# Design: Corin 目标输入、MotionWarp 与训练敌人闭环

## Context

MotionWarp执行层已经完成：合法`MotionWarpClip`会编译为`TimelineMotionWarp` operation，只有显式source MotionCurve成为Action channel resolved owner时才进入Modifier；目标来自对应ActionInstance捕获的不可变`ActionTargetSnapshot`；最终request继续交给唯一WorldSolver。

缺失部分位于Warp之前：

```text
没有目标选择输入
  -> 没有InputDerived ActionTargetSnapshot Blackboard
  -> CanActivate/Activate没有候选目标
  -> Attack Profile只能保持None
  -> Corin Timeline不能合法增加MotionWarpClip
```

同时，Standalone当前只有一个由`CharacterPipelineHost`固定创建Unity设备输入、本地相机和Local Owner Presentation的Corin。要增加一个真正参与Session与WorldSolver的静止敌人，必须让Actor控制来源和表现角色可以显式选择，而不能复制Character runtime或把普通GameObject冒充Simulation Actor。

## Goals

- 让目标候选沿正式Input、Blackboard、Action admission和ActionInstance进入MotionWarp。
- 让有目标和无目标攻击都具有明确可审查的业务语义。
- 在同一个Local Session中放入第二个使用同一Corin Program的静止模拟Actor。
- 保持目标pose来自已提交逻辑Body，不读取表现Transform。
- 保持Float32、Fixed和Network Model只共享portable输入事实，不共享Unity provider。
- 让Agent可以完成全部BTSMTL/Timeline/Blackboard/ActionProfile资产修改。

## Non-Goals

- 不实现完整锁定系统或目标评分。
- 不实现敌人AI和Combat Result。
- 不让MotionWarp追踪动作期间移动后的目标。
- 不让Presentation、Animator或CharacterController反向提供Gameplay目标真相。
- 不建立仅供Demo使用的Target脚本直写Graph状态。

## Terms

### Target Candidate

当前输入帧提供给动作准入的可选目标事实，包含稳定TargetId、position和yaw。它可以每帧变化，但还不属于任何ActionInstance。

### Captured Target Snapshot

Action激活成功时从Target Candidate复制到ActionInstance的不可变pose。当前Action的MotionWarp始终使用这一份值。

### InputDerived Blackboard

由当前`CharacterSimulationInput`在Evaluate开始前覆盖的Character-scope Blackboard declaration。它不是Config、Graph节点写入或网络镜像；Input value与Blackboard slot通过编译后的显式input id绑定。

### Simulated Actor

在当前Session内执行正式Program、拥有CharacterState和World Body，但不拥有玩家设备或相机的Actor。训练敌人使用Neutral Input，后续Bot可以替换为AI Input Source。

## Target Chain

```text
Explicit Session Actor Target Provider
  -> target Actor latest committed Body
  -> Unity player input adapter latch
  -> CharacterSimulationInput.ActionTargetSnapshot value
  -> Network Model input/history/codec when applicable
  -> InputDerived Blackboard binding
  -> CanActivateAction + ActivateActionInstance
  -> immutable ActionInstance.TargetSnapshot
  -> Timeline MotionCurve resolved Action channel
  -> TimelineMotionWarp Modifier
  -> CharacterMotionRequest
  -> WorldSolver
  -> committed Body Result
  -> Presentation
```

## Decisions

### Decision 1: 增加显式 OptionalSnapshot，而不是让缺目标隐式跳过

选择：`ActionTargetRequirement`包含：

```text
None
OptionalSnapshot
SnapshotRequired
```

- `None`：动作不接受目标，配置MotionWarp非法。
- `OptionalSnapshot`：有目标时捕获并运行Warp；无目标时动作仍合法，Warp按定义保持source trajectory。
- `SnapshotRequired`：缺目标时准入拒绝，适用于处决、交互和必须对齐的动作。

业务收益：普通攻击既能对敌修正，也能在没有敌人时挥空；处决等动作仍能强制要求目标。

代价：Action admission、catalog、Program、两个Target和MotionWarp runtime都要识别第三种策略，并提升ABI。

未选择“所有Attack都SnapshotRequired”：会让现有无目标Scene完全不能攻击，也会迫使每个网络测试环境先实现目标系统。

未选择“Catch异常后不Warp”：这会把配置错误与正常无目标混在一起，属于禁止的fallback。

### Decision 2: Target Candidate属于portable input，不属于Scene或Presentation

选择：在Float32与Fixed `SimulationInputValue`中增加`ActionTargetSnapshot` kind，按显式input id传递。Unity provider只负责在设备采样边界构造该值；Program、Network Model和Rollback history消费同一canonical payload。

业务收益：本地玩家、AI、服务端输入和Rollback重放都可以提供同一种目标事实，Graph与MotionWarp不认识来源。

代价：Input codec、hash、neutral value、history和两个Numeric Target都需要版本升级。

未选择在`CharacterPipelineHost.Update`直接写Blackboard：它绕过SimulationTick、Snapshot和网络输入历史。

未选择让MotionWarp按TargetId查Scene Transform：它会把Unity场景对象带进portable runtime，并让回放结果随外部对象变化。

### Decision 3: InputDerived declaration显式绑定input id

选择：Blackboard declaration在`SyncPolicy=InputDerived`时必须保存非空`InputValueId`。Compiler验证scope、lifetime、authority和value type，并把binding编入Program Layout。Evaluate开始时Input runtime先完成全部InputDerived写入，再执行Graph control。

首个正式类型是`ActionTargetSnapshot`，实现按通用typed binding建立，不在Blackboard runtime中按`ActionTarget`字符串判断。

业务收益：CanActivate与Activate继续使用用户熟悉的Blackboard变量；输入来源和Graph消费明确解耦。

代价：Blackboard authoring model、Inspector、Agent、Semantic IR、Program layout和runtime写入顺序都需要同步修改。

未选择增加Target专用全局变量：那会恢复黑板之外的第二份角色状态。

### Decision 4: 训练敌人是同Session Simulated Actor

选择：训练敌人复用Corin Definition、Program、Projection、World Body和Animancer表现，使用Neutral Input与Simulated Actor Presentation。Neutral Input根据Program input catalog生成类型正确的零值和空request，不硬编码Move/Attack名称。

业务收益：玩家Warp面对的是WorldSolver实际知道的Actor；碰撞、Body pose和后续AI扩展都沿正式Session边界。

代价：需要把`CharacterPipelineHost`当前固定的输入与表现装配拆成显式策略。

未选择普通胶囊/Transform Dummy：它只能测试吸附视觉，不能证明Session Actor、碰撞和目标pose链路成立，而且后续必须删除重做。

未选择复制Enemy Character Runtime：这会形成玩家/敌人两套Program执行和动画路径。

### Decision 5: 目标provider读取已提交Body，不读取VisualRoot

选择：Actor registration从初始Body开始保存最近一次已发布Body；显式Session Actor provider通过稳定Actor绑定读取该值并生成Target Candidate。provider不扫描roster、不按Tag找对象，也不读取VisualRoot或Animator root。

业务收益：目标与WorldSolver真值一致；表现插值不会反向改变下一次攻击的Gameplay目标。

代价：目标采样最多落后当前尚未完成的一个logic tick，这是稳定且可解释的输入边界。

### Decision 6: Host按Control Source和Presentation Role组合

选择：`CharacterPipelineHost`消费一个显式Unity-facing input source factory，并配置`LocalOwner`或`SimulatedActor`表现角色。

```text
Player Corin
  Control Source: Unity Player Input + target provider
  Presentation: LocalOwner + camera

Training Enemy Corin
  Control Source: Neutral Input
  Presentation: SimulatedActor
```

registration只依赖统一的Unity simulation input adapter合同；look input是LocalOwner的可选窄能力，不再把registration类型绑死到`UnityCharacterSimulationInputAdapter`。

业务收益：后续AI只需要增加正式AI input source，不修改Program、Action、Motion、WorldSolver和Presentation。

代价：Host装配代码需要拆分，现有Corin prefab必须一次迁移到显式player source和LocalOwner role。

### Decision 7: Attack1..Attack5都预置Warp，但只绑定主攻击MotionCurve

选择：五段攻击各自创建一个MotionWarpClip，显式引用本段主Action MotionCurve；后摇MotionCurve不绑定Warp。初始窗口放在合法主MotionCurve范围内，参数使用保守、可编辑的正式值，之后由作者在Timeline中精调。

业务收益：连段每一段都会在自身ActionInstance激活时重新捕获目标，不会只有第一段靠近、后续段完全偏离。

代价：五段窗口和曲线都需要作者按动画命中节奏继续调节；本change只保证合法、可运行和可观察，不声称最终手感数值已经完成。

### Decision 8: Agent补齐目标authoring，不手改Graph YAML

选择：在v11现有typed command链增加：

- `ActionTargetSnapshot` Blackboard value type。
- InputDerived `inputValueId`字段。
- `action_can_activate` condition term的target declaration引用。
- Action activation目标引用的snapshot完整投影。
- `set_action_profile_target_requirement`操作。
- current contract中的MotionWarp四类操作与v11说明。

所有Corin Graph/Timeline修改继续执行：

```text
export_snapshot
  -> dry_run_patch
  -> apply_patch same JSON
  -> export_snapshot
  -> validate
```

代价：Agent schema/validator与技能合同都要更新，但这是保证资产唯一写入口所必需的。

## Runtime Ordering

每个Actor Evaluate顺序固定为：

```text
Input request ingest
  -> InputDerived Blackboard projection
  -> Runnable/StateMachine control
  -> CanActivate/Activate
  -> Timeline evaluation
  -> Motion channel resolve
  -> MotionWarp Modifier
  -> MotionRequest
```

InputDerived写入和Graph执行属于同一个Character State Transaction。Evaluate或WorldSolver失败时，它们与ActionInstance、MotionWarp state一起回滚，不发布半帧target状态。

## Optional Target Semantics

当`OptionalSnapshot`动作没有目标时：

- CanActivate与Activate都返回allowed。
- ActionInstance保存`None` target。
- MotionWarp eligibility返回`NoTargetByOptionalPolicy`。
- source MotionCurve保持原值，不产生position/yaw correction。
- 不记录runtime error，不修改Timeline窗口，不选择其它目标。
- diagnostics显示无目标是明确策略结果。

当`SnapshotRequired`动作没有目标时：

- CanActivate与Activate都返回`TargetSnapshotRequired`。
- 不创建ActionInstance，不启动Timeline。

## Training Enemy Scene

`StandaloneGameplay`保留为普通产品场景。新增训练敌人必须：

- 使用显式`ActorId=corin-training-enemy`。
- 与玩家注册到同一`SimulationSessionHost`。
- 使用相同Corin Definition和generated Program/Projection。
- 使用Neutral Input和Simulated Actor Presentation。
- 拥有正式World Body binding和CharacterController collider。
- 玩家target provider显式引用它，不按名称查找。

训练敌人初版保持Idle，不产生request。它不是命中测试替身；以后加入AI时替换Control Source即可。

## Network Boundary

Target Candidate进入同一CharacterSimulationInput canonical payload：

- Local直接消费。
- ServerAuthoritative Prediction将它写入owner input history与command；Authority读取同一payload。
- DeterministicRollback把它作为Actor input identity的一部分中继、canonicalize和replay。
- Network不发送AnimationClip、MotionWarp progress或最终Warp位置作为第二种命令。

本change不实现服务端目标合法性验证。MotionWarp已有最大修正clamp且命中/伤害尚未闭环；未来Combat authority必须基于TargetId和World state验证目标，不得信任客户端pose决定命中。该限制会在diagnostics和文档中明确，不伪装成完整反作弊方案。

## Failure Policy

以下情况必须在发布或composition前失败：

- InputDerived declaration缺少input id或类型不匹配。
- CanActivate与reachable Activate引用不同target declaration。
- `None` Action包含MotionWarp。
- `SnapshotRequired` Action入口没有target declaration。
- Warp source、窗口、curve、Action Context不合法。
- 玩家target provider绑定到自己、无效ActorId或另一个Session。
- Simulated Actor配置LocalOwner camera或Neutral Actor缺Program input defaults。
- Input codec收到未知target payload版本。

运行时目标暂时不可用仅在`OptionalSnapshot`策略下是合法业务结果，不触发目标搜索或默认pose。

## Migration And Deletion

1. 先升级typed target policy、input payload、Blackboard binding和两个Numeric Target。
2. 再拆Host input/presentation装配并迁移现有玩家prefab。
3. 补齐Agent v11目标操作后，通过正式Patch迁移Corin Graph/Timeline。
4. 创建训练敌人prefab/scene实例并绑定provider。
5. 重新生成全部Program/Projection/product identity。
6. 删除旧Host固定Unity input字段、旧registration具体adapter依赖、Agent target authoring缺口和任何临时scene target脚本。

不提供旧input codec、旧Host字段、旧Agent schema或无target runtime exception的兼容路径。

## Risks And Tradeoffs

### Scope比单个Warp Clip大

原因是单个Clip没有目标输入就不能合法执行；普通GameObject敌人又无法验证WorldSolver与Session链。缩小到手改目标pose只会把必要工作推迟并制造第二路径。

### OptionalSnapshot会改变已批准的严格目标规则

它放宽的是业务能力，不是错误处理。`None + Warp`仍被拒绝，只有作者明确选择Optional时才允许source-preserving行为。该语义必须进入catalog、hash和diagnostics，不能只写一个`if target == null`。

### 第二个完整Corin Actor增加Standalone成本

同Session多Actor会增加Program Evaluate、Animation和CharacterController成本，但它正是后续Bot/怪物需要的真实压力。训练敌人不应通过关闭Program或动画来隐藏这部分成本。

### 必须扩展已完成的Rollback输入基线

`refactor-deterministic-rollback-input-propagation`已经完成。实现必须直接沿其最终Captured、Relayed Explicit、Predicted、Canonical与Confirmed输入阶段增加target payload，并保留request timing、exact-input与空离散请求规则。不得按本proposal形成时的旧结构恢复input delay、canonical host或旧codec。

### 与Presentation并行change存在装配文件交点

Foot Placement已经成为当前Presentation runtime与Host装配基线，本change必须保留。Animation Marker Sync与本change共享Corin Timeline和Agent v11 source revision，必须串行落资产；AI Controller可并行实现独立输入模块，但训练敌人Control Source的最终替换必须基于本change装配。

## Open Questions Resolved

- 第一个测试目标是否只是Transform：不是，使用正式Session Actor。
- 是否需要完整敌人AI：不需要，使用Neutral Input；未来AI替换Input Source。
- 没有敌人是否还能攻击：可以，由显式OptionalSnapshot策略保证。
- 目标动作期间是否持续追踪：不追踪；每段ActionInstance激活时重新捕获。
- 是否先只做Attack1：不采用；五段都预置Warp，避免连段后续段无目标修正。
- 是否同步动画或Warp状态：不同步；网络只携带typed input，两个端各自执行Program。
