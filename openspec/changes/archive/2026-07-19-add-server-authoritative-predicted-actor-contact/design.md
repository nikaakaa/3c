# Design: ServerAuthoritative预测中的观察Actor接触

## Context

当前Authority与Prediction对同一业务事实使用了不同世界输入：

```text
Authority
  Actor A Program request + Actor B Program request
    -> DotRecast ResolveBatch
    -> ActorContactSolver(A active, B active)
    -> A/B FinalBody

Client A Prediction
  Actor A Program request
    -> DotRecast ResolveBatch
    -> ActorContactSolver(A only)
    -> A FinalBody

Remote Actor B authority Body
    -> Remote Presentation only
```

网络校正只能告诉Client A“刚才结果不对”，却不能让下一次预测拥有缺失的Actor B。因此相同错误会持续重演。

## Goals

- 让ServerAuthoritative Prediction在不运行远端Gameplay的前提下预测普通Actor硬接触。
- 保持唯一WorldSolver、唯一ActorContactSolver和唯一FinalBody提交入口。
- 让Current、Restore、Replay使用可证明的远端Body frame。
- 让远端可见Body与碰撞Body来自同一选择流，避免看得见的位置和挡住玩家的位置分裂。
- 保持Authority完整roster对称求解和Fixed Rollback实现不变。

## Non-Goals

- 不获得或推测远端输入，不复制远端Program执行。
- 不把网络误差变成表现层Transform或动画问题。
- 不提供绝对零纠偏承诺。ServerAuthoritative客户端无法知道远端未来输入，短时外推错误只能在新权威样本到达后纠正。

## Decision 1: 远端Actor是ObservedKinematic，不是第二个Simulation Actor

公共Float32 Step新增`ObservedWorldConstraintFrame`。其中每个参与者包含：

```text
ActorId
PreviousBody
CurrentBody
PreviousAuthorityTick
CurrentAuthorityTick
TargetSimulationTick
SamplingKind
SourceHash
ContactShapeConfigurationHash
Participation = ObservedKinematic
```

本地Program actor仍由`CharacterWorldSolveRequest`表达，参与语义为`ActiveSimulated`。Observed参与者不出现在Program roster、Character state、World committed state和Result roster中。

### 为什么不把远端加入Client roster

加入roster就需要Program、初始Character state、canonical input、Gameplay output disposition和动画事件身份。客户端没有远端输入，只能伪造neutral input或运行错误业务，这会从碰撞问题扩成第二套Gameplay真值。

### 为什么不做Unity Collider proxy

Collider proxy会让Unity Physics成为第二个接触裁决者。DotRecast预测、Authority和表现Transform会产生三份位置，World batch的request/result hash也无法证明使用了哪个碰撞体。

## Decision 2: RemoteBodyTimeline归Prediction History模块所有

Observation Ingress解码并验证远端`CharacterBodySample`后，只提交给`ServerAuthoritativeRemoteBodyTimeline`。该时间线按ActorId与authority tick稳定保存：

- `BeforeBody`与`FinalBody`连续性。
- Solver/World/Actor identity。
- 收到顺序无关的canonical tick排序。
- 有界容量和明确淘汰条件。
- 当前事务checkpoint、capture、restore与hash。

Remote Presentation不得再保存一份可以独立决定Body tick的原始权威样本时间线。它只缓存Schedule已经选择并提交的Body frame，用于渲染帧插值。

RemoteBodyTimeline放在现有History模块内部，而不是增加第四个Prediction状态模块。这样History record、Replay查询和远端采样输入由同一owner维护。

### 启动预热

当前Prediction Endpoint在data plane Ready后即可完成Source preparation，首个remote Body样本不属于握手内容。因此不能把“尚未收到remote Body”一律定义为运行故障。

Schedule增加正式`RemoteObservationPriming`状态：

- 从locked handshake roster确定全部非owner ActorId。
- 每个remote Actor至少拥有可形成合法anchor的权威Body sample前，只产生零Current step。
- 既有pending request规则继续保存离散输入，不能丢弃或重复消费。
- Priming期间不构造空观察frame冒充完整预测，也不发布remote selected Body。
- 首个完整观察集合到达后一次性进入正常Current调度。
- 已进入正常调度后发生超上限缺口属于正式失败或HardRecovery，不重新退回Priming。

## Decision 3: Current按目标tick选择，Replay复用历史frame

### Current step

Schedule以当前`Float32SimulationStep.Tick`为目标：

1. 目标tick命中权威样本时使用Exact。
2. 目标tick位于两个权威样本之间时使用Interpolation。
3. 目标tick晚于最新样本时，最多在`MaximumRemoteBodyExtrapolationTicks`内使用最新权威Body velocity做ConstantVelocityExtrapolation。
4. 缺少连续样本、Actor identity不匹配、超过外推上限或Body不合法时，Schedule拒绝当前outer transaction。

外推上限进入Model policy、PipelineHash和handshake compatibility。系统不使用未序列化默认值，也不在失败时退化为无远端碰撞。

### Replay step

每条History record保存当时实际使用的完整`ObservedWorldConstraintFrame`和hash。Replay只读取该frame，不根据当前RemoteBodyTimeline重新计算过去。这样同一输入、同一owner state和同一观察frame会进入同一World request hash。

新权威远端样本只影响后续Current step。Owner baseline纠偏时，Reconciler保留当前合法RemoteBodyTimeline，并从旧History record取得Replay frame；不能把最新远端Body倒灌进过去。

Replay frame只负责重建过去的World request，不能作为新的实时Remote Presentation sample发布。一个outer transaction包含Restore、多个Replay和Current时，Egress只提交成功Current step产生的selected frame；没有Current step时，表现层完成当前已提交插值区间并保持终点。HardRecovery必须提交显式Body stream reset，后续成功Current step再提交新anchor，不能让Presentation自行猜测清空时机。

## Decision 4: 同一选择流驱动碰撞与远端Body表现

Schedule先从唯一RemoteBodyTimeline产生model-neutral selected Body frame。该frame始终进入Remote Presentation；只有Solver声明且Composition要求观察接触能力时，Schedule才把同一选择转换成`ObservedWorldConstraintFrame`：

```text
RemoteBodyTimeline
  -> selected Body frame
     -> Remote Presentation Egress
     -> [ObservedKinematicActorContact]
        -> Float32SimulationStep
        -> WorldSolveBatchRequest
        -> DotRecast contact

Final committed model output
  -> Remote Presentation Egress
  -> selected Body sample buffer
  -> render interpolation
```

未声明该能力的Unity CharacterController Prediction仍消费同一selected Body frame完成远端表现，但向World提交带tick的正式空观察frame。它不会获得预测硬接触，也不会伪装支持；这不是运行时fallback，而是Composition在准备期锁定的Solver能力差异。

Presentation可以在两个selected frame之间按渲染delta插值，也可以在frame被新权威信息替换后从当前可见pose平滑收敛，但不能自行选择另一个authority tick，更不能把visual root写回World。

这项选择会让远端Body更接近客户端用于接触预测的短时估计，而不是固定落后多个tick。优点是玩家看到的角色与挡住自己的角色一致；代价是远端突然改向时，Body表现会随新权威样本发生一次视觉收敛。该代价比持续的隐形碰撞或周期性穿透拉回更符合当前硬碰撞业务。

现有`RemoteInterpolationDelayTicks`同时承担Body缓冲和可靠事件horizon，职责已经混合。本change删除该字段：

- Body使用`MaximumRemoteBodyExtrapolationTicks`控制可预测范围。
- 可靠Select/Complete/Release仍按已收到的authority tick和EventId发布，并不得早于同tick selected Body已进入Presentation。
- 不再配置第二个Body delay。

## Decision 5: World batch包含Active requests与Observed constraints

`WorldSolveBatchRequest`继续以`BeforeWorldState`和active `CharacterWorldSolveRequest`为可提交世界。新增的观察frame进入canonical request bytes和`RequestHash`，但不进入`BeforeWorldState`。

合同约束：

- Active request数量必须与`BeforeWorldState.Bodies`完全一致。
- Observed ActorId必须有效、稳定排序、不得与active ActorId重复。
- Observed frame tick必须匹配batch tick。
- Observed frame没有Result slot，也不得出现在`NextWorldState`。
- Observed frame必须携带接触形状configuration hash，并与Solver锁定的canonical shape一致。
- 空观察frame也是带batch tick的正式值，不允许`null`代表“碰撞关闭”。

这使Local、Authority、Preview和Prediction仍使用同一Evaluate/WorldSolve Pass。差异只在Schedule依据已锁定WorldFeature提供完整或空观察frame，不在Pass内按Network Model类型分支。

## Decision 6: ActorContactSolver按Mobility执行单侧修正

`ActorContactCandidate`新增`ActorContactMobility`：

- `ActiveSimulated`：候选位置可以被接触求解修正，最终由调用方提交。
- `ObservedKinematic`：轨迹是外部已选择约束，求解器不得移动或提交。

pair规则：

| Pair | 处理 |
|---|---|
| Active / Active | 保持现有对称连续扫掠与去穿透 |
| Active / Observed | 使用双方相对轨迹求TOI，只修正Active闭合法向分量 |
| Observed / Observed | 不生成修正；只在需要时记录诊断 |

`ObservedKinematic`不是优先级、阵营或Gameplay权威枚举。它只描述本次World batch内“哪一侧允许当前Solver改写”。攻击推人、霸体或ghost不能复用该枚举硬编码。

DotRecast WorldSolver对active执行Surface candidate和接触后的surface reconstraint；observed轨迹不重新投影，因为它已经来自匹配World identity的权威Solver。最终验证必须检查active与observed最小间距。无法同时满足Surface和接触时整个batch失败。

当前DotRecast World configuration要求locked roster使用一份canonical contact shape。Observed constraint只携带该shape的configuration hash，具体Radius、Height与SkinWidth继续由客户端Solver Definition拥有；网络样本、Remote Presentation和默认值都不能提供第二份形状数据。握手中的World identity与frame shape hash任一不匹配时，Prediction在WorldSolve前失败。后续若业务需要不同角色形状，必须先扩展正式World binding/manifest合同，不能在本change中按ActorId特判。

## Decision 7: 能力与身份必须在Session准备期锁定

新增`WorldFeature.ObservedKinematicActorContact`。DotRecast Solver只有在实现上述合同后才能声明该feature。Corin DotRecast Prediction Composition显式要求：

```text
NavigationSurface
ActorCollision
ObservedKinematicActorContact
```

World request codec version、History v2、Model policy version、PipelineHash、Solver definition identity与WorldConfigurationHash必须同步更新。要求观察接触feature的Composition若装配不支持该能力的Solver，必须在Prediction Active前失败。未要求该feature的Composition提交正式空观察frame。

## Schema Migration

Prediction State保持三个既有participant及其顺序：

```text
Correction v3
History v2
Journal v2
```

History v2一次性替换v1，新增：

- RemoteBodyTimeline canonical bytes。
- 每个History record的ObservedWorldConstraintFrame bytes。
- frame hash、sampling kind与source authority tick。

旧History v1 reader、schema常量和exact-byte说明直接删除。没有兼容reader、双写payload或运行时migrator。现有开发资产由正式Editor构建流程重新生成。

## Failure Semantics

以下情况必须使当前outer transaction失败或进入既有formal HardRecovery，不得继续无接触预测：

- 远端Actor缺少合法Body样本。
- Body样本不连续或Actor/World identity不匹配。
- Current目标超过外推上限。
- Replay record缺少已保存观察frame。
- Solver未声明观察接触feature。
- Active/Observed最终仍穿透。
- History v2 capture、restore或hash失败。

## Alternatives And Tradeoffs

### 方案A：只依赖Authority碰撞

改动最小，但客户端每tick仍会预测穿透，网络校正会重复发生。适合远端无硬碰撞的游戏，不适合当前业务。

### 方案B：ObservedKinematic短时预测

本change选择。它复用同一WorldSolver，远端不运行Gameplay，普通接近、站立阻挡和沿角色滑动可以在客户端先得到接近Authority的结果。远端突然改变输入时仍会出现一次纠偏，因为未来输入未知。

### 方案C：完整预测远端Program

需要同步或猜测远端输入，并为远端复制Character state、Timeline、GameplayEffect和事件处置。预测可能仍错，复杂度和作弊面显著增加，不符合ServerAuthoritative模型。

### 方案D：取消或软化玩家硬碰撞

网络手感最稳定，也是很多动作网络游戏的常见选择，但它改变了当前“Actor必须实体阻挡”的业务规则。本change不替用户改玩法。

### 方案E：表现层Collider或位置补偿

看起来容易，但会制造第二个碰撞裁决和不可回放的Transform输入，直接违反现有Session/World边界。

## Implementation Order

1. 先安装公共Float32观察约束与identity合同。
2. 再扩展DotRecast active/observed接触语义。
3. 再把RemoteBodyTimeline接入Prediction aggregate与History v2。
4. 再让Schedule为Current/Replay生成唯一frame。
5. 再迁移Remote Presentation只消费selected frame。
6. 最后迁移Corin配置、诊断、文档并清理旧字段和旧reader。
