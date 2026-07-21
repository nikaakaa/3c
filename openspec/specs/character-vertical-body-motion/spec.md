# character-vertical-body-motion Specification

## Purpose

定义角色玩法 Motion 完成后、WorldSolver 前后的唯一垂直动力积分、状态提交、能力校验，以及网络与回滚闭包。

## Requirements

### Requirement: 垂直Body Motion必须有唯一Prepare与Finalize阶段

同一Actor/SimulationTick的玩法Motion在channel与Program Motion Modifier完成后 MUST形成唯一`ResolvedGameplayMotion`。当前Numeric Target的唯一Body Motion Integrator MUST在WorldSolver前通过`Prepare`把committed Body、compiled Body Motion descriptor、TickDelta与ResolvedGameplayMotion转换为唯一`CharacterMotionRequest`和同Step integration plan；WorldSolver返回真实applied displacement、Grounded与Collision后 MUST通过同一Target Integrator的`Finalize`形成committed vertical state。Graph、Timeline、Action、Presentation、Session Source、Network Model与concrete KCC MUST不拥有第二套重力积分或碰撞后垂直状态转换。

#### Scenario: Actor走出稳定支持面

- **WHEN** Actor上一Tick稳定Grounded且当前玩法Motion把它带出支持面
- **THEN** Prepare MUST在当前Tick加入向下重力位移
- **AND** Solver报告Airborne后Finalize MUST保存candidate VerticalVelocity
- **AND** 下一Tick MUST从该速度继续积分

### Requirement: VerticalVelocity必须与实际Velocity分离

`WorldBodyState` MUST分别保存actual `Velocity`与`VerticalVelocity`。Actual Velocity MUST继续表示Solver applied displacement除以TickDelta；VerticalVelocity MUST唯一表示会影响下一Tick重力积分的垂直动力状态。系统 MUST不从actual `Velocity.Y`、坡面法线、Step位移、Ground Snap位移、Animation root motion或Presentation速度推导VerticalVelocity。VerticalVelocity MUST进入WorldState equality、canonical codec、Snapshot、Hash和restore。

#### Scenario: Actor沿坡面上行

- **WHEN** Solver因稳定坡面投影产生正向actual Velocity.Y
- **THEN** actual Velocity MUST记录该几何上升速度
- **AND** VerticalVelocity MUST不因此变为向上动力

### Requirement: 重力必须使用版本化恒加速度语义

Body Motion descriptor MUST显式保存有限负数`GravityAcceleration`、有限正数`MaximumFallSpeed`与semantic version。Prepare MUST使用固定半隐式运算顺序，先计算`candidate = max(previous + gravity * dt, -maximumFallSpeed)`，再计算`gravityDelta = candidate * dt`并把gravityDelta加入玩法位移Y。Float32与Fixed MUST从同一numeric-neutral descriptor降低并执行同一公式和状态转换；运行时 MUST不按Solver、Network Model、Scene或Presentation选择另一积分模式。

#### Scenario: Actor持续自由下落

- **WHEN** Actor连续多个Tick没有稳定Grounded且没有阻挡向上动力的Above碰撞
- **THEN** 每Tick MUST从上一committed VerticalVelocity继续积分
- **AND** 向下速度 MUST不超过MaximumFallSpeed

### Requirement: 玩法Y位移不得消费或关闭重力

ResolvedGameplayMotion的Y位移 MUST表达Timeline、Action或GameplayResult明确产生的作者位移。Prepare MUST将该Y位移与gravityDelta相加。Motion channel priority、`Override`、`ConsumeLowerChannels`与Program Motion Modifier MUST只解析玩法Motion，MUST不消费、覆盖或关闭环境重力。第一版 MUST不提供IgnoreGravity、GravityScale、OverrideVerticalVelocity或从MotionCurve delta推断跳跃动力的隐式规则。

#### Scenario: Grounded攻击包含向上MotionCurve

- **WHEN** Action channel赢得仲裁并产生向上Y delta
- **THEN** Prepare MUST把向上玩法delta与向下gravityDelta合成为唯一request Y
- **AND** Action的ConsumeLowerChannels MUST不删除gravityDelta
- **AND** KCC MUST根据最终request决定Ground Snap与碰撞

### Requirement: 碰撞后VerticalVelocity必须由统一规则提交

Body Motion Finalize MUST只读取matching integration plan、Solver actual displacement、稳定`Grounded`与方向性portable Collision。Portable `Grounded` MUST只表示已确认的可站立稳定支撑，`Below` MUST只表示下方方向发生接触且 MUST不单独证明稳定支撑。只有稳定Grounded MUST把向下VerticalVelocity清零；Above碰撞 MUST把向上的VerticalVelocity清零；仍Airborne且没有对应阻挡时 MUST保存candidate VerticalVelocity。Actual Velocity MUST独立按applied displacement计算。Concrete Solver MUST调用Target唯一Finalizer，MUST不复制、跳过或改写上述状态转换。

#### Scenario: 下落角色落地

- **WHEN** request包含向下位移且Solver确认稳定Grounded
- **THEN** committed VerticalVelocity MUST为零
- **AND** actual Velocity MUST继续反映该Tick真实applied displacement

#### Scenario: 下落角色接触非稳定陡坡

- **WHEN** request包含向下位移且Solver报告方向性下方接触但`Grounded=false`
- **THEN** committed VerticalVelocity MUST继续保存candidate向下速度
- **AND** Finalize MUST不因`Below`或其它非稳定接触结束下落

#### Scenario: 向上动作撞到天花板

- **WHEN** request包含向上位移且Solver报告Above
- **THEN** 向上的VerticalVelocity MUST被清零
- **AND** Finalize MUST不在Solver之后补偿被阻挡的Y位移

### Requirement: Body Motion配置必须是Program编译身份

`CharacterPipelineDefinition` MUST显式引用唯一`CharacterBodyMotionProfile`。Frontend MUST把Profile identity、content revision、GravityAcceleration、MaximumFallSpeed与semantic version编入Semantic IR；Float32/Fixed Program MUST编入对应Target descriptor、ProgramHash、source revision与required world capability。Runtime MUST只读取compiled descriptor，MUST不读取Profile ScriptableObject、Blackboard、Scene字段或缺失默认。

#### Scenario: 两端使用不同Gravity配置

- **WHEN** 两端Profile的GravityAcceleration或MaximumFallSpeed不同
- **THEN** Program identity MUST不同
- **AND** Session组合或网络握手 MUST在模拟前拒绝不匹配

### Requirement: AirborneVerticalMotion必须由Solver真实声明

包含Body Motion descriptor的Program MUST要求`AirborneVerticalMotion`通用World Capability。Composition MUST在Session Active前验证Solver descriptor真实支持该能力。Unity CharacterController与Deterministic KCC只有在完整消费XYZ request、分别报告稳定Grounded与方向性Above/Below并调用统一Finalize后才能声明该能力。当前DotRecast Navigation Surface Solver MUST不声明该能力，也 MUST不通过丢弃Y、NavMesh投影、假Grounded或隐藏fallback继续运行。

#### Scenario: Corin Program选择DotRecast Solver

- **WHEN** Corin Program要求AirborneVerticalMotion但DotRecast descriptor不支持
- **THEN** Composition MUST在创建Session runtime前失败
- **AND** 错误 MUST明确列出缺失capability

### Requirement: 垂直动力必须完整进入网络与回滚状态

ServerAuthoritative Prediction History、Authority Baseline、Checkpoint、Correction与HardRecovery，以及Deterministic Rollback Snapshot、History、Hash与Recovery MUST保存并恢复每个Actor的VerticalVelocity。受影响schema MUST单路升级并拒绝旧payload；系统 MUST不以零默认、actual Velocity.Y或当前Grounded重建缺失状态。

#### Scenario: Rollback恢复到下落中间

- **WHEN** Peer恢复一个Actor正在下落的历史Snapshot
- **THEN** VerticalVelocity MUST与Position、Grounded和KCC support state一起恢复
- **AND** replay下一Tick MUST从恢复速度继续同一Fixed积分

### Requirement: 垂直Body Motion必须可追踪但不进入Presentation权威

Structured Trace MUST关联Profile/source identity、gameplay Y、previous VerticalVelocity、gravity delta、candidate VerticalVelocity、final request Y、Solver applied Y、Grounded、Collision与committed VerticalVelocity。Presentation MAY只读committed VerticalVelocity用于诊断，但 MUST继续以actual Position、Velocity和Grounded驱动VisualRoot、动画与Foot Placement，MUST不通过VerticalVelocity反写Gameplay或补偿Transform。

#### Scenario: 排查角色离崖后悬空

- **WHEN** Diagnostics查看离崖Tick
- **THEN** Trace MUST能区分Prepare未产生gravity delta、Solver错误保持Grounded与Finalize未保存candidate速度
- **AND** Diagnostics MUST不读取mutable integration plan决定后续Gameplay
