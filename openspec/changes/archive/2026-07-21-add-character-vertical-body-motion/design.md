# Design: 角色垂直 Body Motion

## Context

当前玩法Motion链已经完成统一提交、仲裁、Modifier与WorldSolver批处理：

```text
Locomotion / Timeline / GameplayResult
  -> SimulationMotionContribution
  -> ResolvedMotionChannel
  -> Motion Modifier
  -> CharacterMotionRequest
  -> ICharacterWorldSolver.ResolveBatch
  -> WorldBodyState
```

缺失的是环境动力。现有两个事实不能承担重力状态：

1. `CharacterMotionRequest.RequestedVelocity`由本Tick玩法位移除以TickDelta得到，只描述请求，不跨Tick保存。
2. `WorldBodyState.Velocity`由Solver实际位移除以TickDelta得到。上坡、下坡、Step Up/Down和Ground Snap都会改变其Y分量，因此它不是自由落体速度。

Fixed KCC的`GroundSnapDistance`只处理上一Tick稳定Grounded且目标稳定地面仍在小范围内的向下贴地。走出Snap范围后，KCC会正确报告`Grounded=false`，但没有调用方生成后续向下位移。

Unity `CharacterController.Move`同样只约束传入位移，不自动应用重力。DotRecast `MoveAlongSurface`则只表达导航表面移动，无法证明支持离开表面后的三维空中碰撞。

### 成熟KCC参考边界

本地`ExternalDownloads/PhilippeKccReference`只作为设计参考，不成为运行时依赖。它通过`ICharacterController.UpdateVelocity`让角色控制策略更新持久`BaseVelocity`，Motor先探测`IsStableOnGround`，再以`BaseVelocity * deltaTime`执行Sweep、Slide、Step和碰撞约束，并把Position、BaseVelocity、Grounding与ForceUnground状态一起保存。该模式确认了三个适用于本项目的边界：

1. Gravity、Jump impulse与Air Control属于速度策略，不属于碰撞查询内核。
2. `FoundAnyGround`不等于`IsStableOnGround`；陡坡接触不能结束下落。
3. Rollback必须恢复跨Tick动力状态和稳定支撑状态，不能只恢复Position。

本项目不会直接复制其完整`BaseVelocity`所有权。水平Gameplay Motion仍由Program每Tick解析，`WorldBodyState.Velocity`仍是Solver实际观察速度；当前change只新增缺失的跨Tick`VerticalVelocity`。后续Jump可以向这一正式动力状态提交显式Vertical Impulse，移动平台速度仍需另行建模，不能借此change隐式预埋。

## Goals

- 用一条正式Target语义生成重力位移和持续垂直速度。
- 保持玩法Motion、环境动力和世界碰撞三个职责分离。
- 保持实际Body速度与未来垂直动力状态语义分离。
- 让Float32与Fixed从同一authoring/Profile得到同一规则。
- 让Rollback、Prediction与Authority恢复完整空中状态。
- 让不支持空中移动的Solver在Session Active前明确拒绝。
- 不恢复旧Motion Stage、Graph gravity node或每Solver私有重力。

## Non-Goals

- 不实现跳跃或空中操作策略。
- 不实现动作专属垂直模式、Gravity Scale或Ignore Gravity。
- 不实现移动平台、动态刚体力、浮力或任意重力方向。
- 不实现垂直MotionWarp。
- 不在本change扩展DotRecast为三维碰撞Solver。

## Decision 1: Body Motion Integration位于Program Motion之后、WorldSolver之前

### Decision

Motion accumulator不再直接返回最终`CharacterMotionRequest`，而是返回：

```text
ResolvedGameplayMotion
  Displacement
  RequestedVelocity
  YawDegrees
  HasMotion
  Provenance
```

Target唯一`BodyMotionIntegrator.Prepare`读取：

```text
ResolvedGameplayMotion
Committed WorldBodyState
Compiled CharacterBodyMotionDescriptor
SimulationTickDelta
```

并产生：

```text
BodyMotionIntegrationPlan
CharacterMotionRequest
```

Integration发生在全部Program Motion Modifier之后。重力不是一个Modifier，因为Modifier处理作者玩法轨迹；重力也不是一个Motion channel，因为channel priority与ConsumeLowerChannels只处理玩法来源竞争。

### Tradeoff

- 收益：动作、重力和碰撞各有唯一owner，新增Solver不会新增玩法规则。
- 代价：现有Motion accumulator返回类型和Evaluate到WorldRequest的合同需要迁移。
- 不选择Graph Gravity Node：每棵Graph都要重复配置，并可能被打断或不执行。
- 不选择MotionContribution：Action Override可能错误消费重力。
- 不选择KCC私有重力：Unity、Fixed与后续Solver会形成分裂语义。

## Decision 2: 独立保存VerticalVelocity

### Decision

`WorldBodyState`增加Target数值类型的`VerticalVelocity`标量：

```text
Position
Yaw
Velocity             实际AppliedDisplacement / TickDelta
VerticalVelocity     影响下一Tick重力积分的动力状态
Grounded
Collision
```

`Velocity.Y`不得用于初始化、恢复或修正`VerticalVelocity`。坡面、Step、Snap与Actor Contact造成的几何Y速度只进入实际Velocity。

`VerticalVelocity`属于World body状态而不是Character Program local state，因为它由环境接触结果决定，并与Position、Grounded、Collision一起参与World Snapshot、WorldHash和Solver恢复。

### Tradeoff

- 收益：坡面移动不会被误判为跳跃，空中恢复有完整动力状态。
- 代价：WorldState和全部网络/历史codec必须破坏性升级。
- 不选择Character State slot：碰撞结果需要再跨Character/World owner同步，容易形成双写。
- 不重解释旧Velocity：会破坏Presentation、网络外推和现有诊断的实际速度语义。

## Decision 3: 使用固定半隐式恒加速度公式

### Decision

世界Y轴固定为向上。Profile要求：

```text
GravityAcceleration < 0
MaximumFallSpeed > 0
```

每Tick Prepare使用相同语义：

```text
v0 = before.VerticalVelocity
v1 = max(v0 + gravity * dt, -maximumFallSpeed)
gravityDelta = v1 * dt
finalDisplacement = gameplayDisplacement + (0, gravityDelta, 0)
```

`v1`作为candidate垂直速度进入IntegrationPlan。Float32执行IEEE Float32边界规则；Fixed执行Q32.32规则。两者不要求bit相同，但必须来自同一Semantic descriptor和同一运算顺序。

Grounded角色也以`v0=0`执行重力并产生小向下位移。若支持面仍在，Finalize清零；若支持面消失，该Tick立即获得第一段下落位移。

### Tradeoff

- 收益：积分顺序稳定、可回放，并与成熟KCC的“先更新持久速度、再由Motor消费速度移动”生命周期一致；后续Jump impulse与碰撞后速度提交可以复用同一状态。
- 代价：Grounded时每Tick都有很小的向下查询，但这正好让Solver确认真实支持面。
- 代价：相较解析式`v0 * dt + 0.5 * gravity * dt * dt`，半隐式在第一Tick产生更大的向下位移，轨迹也更依赖固定Tick率；本项目Session已经锁定Simulation Tick，Profile按该Tick语义调参。
- 不选择解析式：它能减少恒定重力下的Tick率轨迹误差，但会让速度冲量、碰撞后速度与位移使用不同积分直觉，不利于后续Jump和统一动力生命周期。
- 不提供可选积分模式：运行时模式切换会扩大身份和调试面，当前没有业务需求。

## Decision 4: 动作Y位移与重力相加

### Decision

`ResolvedGameplayMotion.Displacement.Y`继续表达Timeline/Action明确制作的本TickY位移。Prepare固定执行：

```text
requestY = gameplayY + gravityDelta
```

Gameplay Motion channel和Modifier不能读取、覆盖或消费gravityDelta。第一版没有`IgnoreGravity`、`GravityScale`或`OverrideVerticalVelocity`字段。

向上的Root Motion若大于向下gravityDelta，最终Request仍向上，KCC现有规则会禁止Ground Snap。动作结束后，若角色处于空中，保存的重力速度继续产生下落；系统不从Root Motion delta反推抛射速度。

### Tradeoff

- 收益：规则简单且不会因Action占权悬空。
- 代价：某些未来动作可能希望完全按动画Y轨迹运行，需要另行增加显式Body Motion policy。
- 不自动把动作Y速度写入VerticalVelocity：Root Motion是每Tick作者轨迹，不等于跳跃冲量。
- 不在本change预埋模式枚举：没有实际业务的字段只会变成未消费配置。

## Decision 5: Prepare与Finalize共享唯一Target实现

### Decision

每个Numeric Target提供一个正式Body Motion模块，包含：

```text
Prepare(beforeBody, gameplayMotion, descriptor, tickDelta)
Finalize(beforeBody, plan, appliedDisplacement, grounded, collision, tickDelta)
```

Finalize规则固定为：

```text
if plan candidate velocity < 0 and grounded:
    nextVerticalVelocity = 0
else if plan candidate velocity > 0 and collision contains Above:
    nextVerticalVelocity = 0
else:
    nextVerticalVelocity = plan candidate velocity
```

Portable结果语义固定为：

```text
Grounded   已确认存在可站立的稳定支撑
Below      下方方向发生接触，不单独证明稳定支撑
Above      上方方向发生阻挡
```

Solver adapter负责产生真实`appliedDisplacement`、稳定`Grounded`和方向性`Collision`，但必须调用Target唯一Finalize构造最终Body动力字段。Unity、Fixed KCC或其它Solver不得复制这段状态机。非稳定陡坡即使产生下方接触，只要`Grounded=false`就必须保存candidate向下速度并继续受重力；墙面只约束实际位移，不清除垂直动力。

`BodyMotionIntegrationPlan`只存在当前Step的Pending/WorldRequest边界，不进入committed Character State、Snapshot或packet。只有Finalize后的`VerticalVelocity`进入WorldState。

### Tradeoff

- 收益：碰撞后状态转换只有一个实现，Solver仍保持真实碰撞权威。
- 代价：World request需要携带可校验的integration plan identity/candidate状态，Solver adapter必须使用统一finalizer。
- 不用`Below`代替`Grounded`：方向性接触不足以证明角色可以站立，否则陡坡会错误停止下落。
- 不在Solver之后由Presentation修正：Presentation不能改变Gameplay真值。
- 不让Solver自己重新积分：会重复公式并可能使用不同TickDelta。

## Decision 6: Profile属于Definition编译输入

### Decision

新增`CharacterBodyMotionProfile` ScriptableObject，第一版只保存：

```text
GravityAcceleration
MaximumFallSpeed
```

`CharacterPipelineDefinition`必须显式引用。Definition Inspector只显示Profile引用、配置错误与生成状态；Profile Inspector编辑具体值。

Frontend把Profile GUID、content revision、两个数值和Body Motion semantic version写入numeric-neutral Semantic IR descriptor。Float32/Fixed lowering分别生成Target descriptor。ProgramHash、source revision、artifact identity与required world capability覆盖完整descriptor。

Runtime只读取compiled Program descriptor，不读取ScriptableObject。缺Profile、非有限数、`GravityAcceleration >= 0`或`MaximumFallSpeed <= 0`直接阻止编译。

### Tradeoff

- 收益：角色行为配置可审查，网络两端通过Program identity锁定，不依赖场景默认值。
- 代价：Corin与所有正式Definition都需要显式资产迁移和产物重建。
- 不把字段内联进Definition：Definition继续作为Config引用装配根。
- 不放Blackboard：重力是角色基础模拟配置，不是Graph临时变量。

## Decision 7: AirborneVerticalMotion是通用World Capability

### Decision

Program包含Body Motion descriptor时要求`WorldCapability.AirborneVerticalMotion`。Composition在Session Active前验证Solver真实声明该能力。

- Unity CharacterController Solver可处理上下位移、Grounded及Above/Below碰撞，完成接入后声明该能力。
- Deterministic KCC具备连续三维胶囊约束、Grounding和碰撞分类，完成接入后声明该能力。
- 当前DotRecast Solver只处理NavMesh surface，不声明该能力。

DotRecast绑定该Program时必须报告缺失Capability并拒绝Session。不得通过以下方式继续：

- 丢弃request Y。
- 每Tick把Body投影回NavMesh并报告Grounded。
- 按Network Model关闭Body Motion Integrator。
- 使用Unity Physics或Fixed KCC作为隐藏fallback。

### Tradeoff

- 收益：Solver能力与真实实现一致，不会在不同网络模型下悄悄改变同一Corin行为。
- 代价：当前DotRecast Authority Corin产品暂时不能启动。
- 不选择Grounded-only Profile：同一角色会按backend改变玩法语义。
- 后续正确方向是为DotRecast组合增加正式空中碰撞后端，而不是扩张NavMesh查询含义。

## Decision 8: 单路升级World与网络schema

### Decision

受影响的Float32/Fixed schema全部提升正式identity，包括但不限于：

- WorldState codec。
- WorldSnapshot codec/hash。
- WorldSolve request/result hash。
- ServerAuthoritative Prediction State与History。
- Authority Baseline、Checkpoint、Canonical Egress与HardRecovery payload。
- Deterministic Rollback full snapshot、history和layered hash。
- 产品runtime manifest与handshake中引用的Program/World identity。

旧payload reader、字段缺失默认、双写和兼容分支全部删除。`VerticalVelocity`按ActorId稳定顺序进入canonical bytes。

### Tradeoff

- 收益：恢复和重放不会丢失空中动力，网络差异能在握手或codec边界立即暴露。
- 代价：所有生成产物和运行产品必须统一重建，旧构建不能混用。
- 不提供migrator：这些都是可重新生成的Program与测试产品，保留旧reader只会扩大分裂面。

## Decision 9: Presentation只消费实际Body结果

### Decision

Presentation继续使用committed Position、Yaw、实际Velocity与Grounded驱动visual trajectory、动画和Foot Placement。`VerticalVelocity`可进入Diagnostics，但不能被Presentation用于修改VisualRoot或反写Gameplay。

Body Motion Trace至少包含：

```text
GameplayY
PreviousVerticalVelocity
GravityAcceleration
GravityDelta
CandidateVerticalVelocity
RequestedY
AppliedY
Grounded
Collision
CommittedVerticalVelocity
```

### Tradeoff

- 收益：画面严格跟随Solver实际结果，碰撞阻挡和网络纠偏不会被隐藏动力二次覆盖。
- 代价：动画若需要区分上升和下降，应从committed Body/diagnostic projection获得信息，不能直接读取Integrator mutable plan。

## Decision 10: Agent只读理解Profile而不新增写入口

### Decision

Agent v13 compact/full Snapshot增加Body Motion Profile identity、content revision、GravityAcceleration、MaximumFallSpeed、semantic version、required capability与Compiler状态。Agent Validator复用Definition/Profile正式校验。

Body Motion Profile继续通过自己的Inspector编辑。Agent v13 Patch不增加Profile mutation operation，MCP bridge不增加专用action，也不提供任意SerializedProperty写入。Snapshot不得输出runtime VerticalVelocity或pending integration plan。

### Tradeoff

- 收益：Agent能审查Definition为什么需要某个Solver能力，不会留下Unity Inspector可见但Agent完全未知的配置。
- 代价：第一版Agent不能自动调整重力参数。
- 不增加Patch写能力：Profile是独立角色基础配置，本change没有定义其自动批量编辑业务；先保持单一Inspector写入口比仓促增加第二写入口更干净。
- 后续若需要Agent修改Profile，必须通过独立批准capability增加typed command、handler与事务所有权，不能复用任意字段写入。

## Migration Sequence

1. 记录当前Program ABI、WorldState/Snapshot、Prediction/Baseline和Rollback codec identity。
2. 增加Profile authoring与Definition引用，但在完整编译链接入前保持配置错误，不能运行时忽略。
3. 增加Semantic descriptor、Target lowering、Program identity与Required Capability。
4. 增加`VerticalVelocity`并一次性升级Float32/Fixed WorldState与canonical codec。
5. 将Motion accumulator输出迁为`ResolvedGameplayMotion`，接入Prepare并删除旧直接Request构造。
6. 接入Unity与Fixed Solver的唯一Finalize，声明真实Capability。
7. 让DotRecast保持不声明并验证Composition正式拒绝。
8. 升级ServerAuthoritative与Rollback所有状态、history、baseline、hash和manifest。
9. 创建Corin Profile，绑定Definition并重新生成全部受影响产物。
10. 删除旧ABI/codec reader、缺字段默认、重复公式和任何按Solver关闭重力的路径。
11. 更新Agent Snapshot只读Profile投影与统一Validator，不增加Patch写入口。
12. 更新current specs、project context、Reader、Inspector和Diagnostics。

## Open Questions Resolved

- **重力是否属于KCC？** 不属于。KCC只负责约束已经积分的位移并报告接触。
- **是否复用实际Velocity.Y？** 不复用。坡面和Step会污染其动力含义。
- **攻击Override能否关闭重力？** 不能。第一版重力始终叠加。
- **Grounded时是否计算重力？** 计算一小段向下位移，Solver确认地面后清零；这样离崖同Tick开始下落。
- **是否同时实现Jump？** 不实现。未来Jump通过显式VerticalImpulse语义进入同一Body Motion模块。
- **DotRecast怎么办？** 当前明确不支持并拒绝组合，不做假闭环。
