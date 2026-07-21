# Change: 增加角色垂直 Body Motion 闭环

## Why

当前 Character Simulation 已把 Locomotion、Action Timeline MotionCurve 与 GameplayResult 统一仲裁为唯一 `CharacterMotionRequest`，再由 Session 唯一 WorldSolver 处理碰撞。Fixed Deterministic KCC 已支持连续胶囊查询、去穿透、多平面滑动、坡面、台阶、稳定 Grounding 与 Ground Snap，但它只消费调用方给出的 `requestedDisplacement`，没有任何阶段根据上一 Tick 的垂直动力状态计算重力。

成熟 KCC 的通用边界同样把“速度策略”和“几何约束”分开：Character Controller 在稳定地面与空中分支中更新持久速度，KCC Motor 探地后消费该速度执行 Sweep、Slide、Step 和碰撞后约束。其落地判断依赖稳定支撑而不是任意下方接触。本文采用这一职责边界，但不引入第三方 KCC 运行时依赖；公共 Body Motion Integrator 对应速度策略，Unity CharacterController 与 Deterministic KCC 继续只作为 WorldSolver。

现有 `WorldBodyState.Velocity` 是 Solver 实际位移除以 TickDelta 的观察速度。角色沿坡面移动时，该速度的 Y 分量会随实际地形上升或下降，因此它不能同时充当重力积分状态。当前系统又没有独立 `VerticalVelocity`。结果是角色离开稳定支持面后虽然会变为 `Grounded=false`，下一 Tick 仍可能收到 Y 为零的位移并悬空；Ground Snap 只能跟随小范围下坡，不能替代重力、持续下落、天花板响应或回滚中的空中状态。

把重力直接写入 Deterministic KCC 会产生三套不同语义：Unity CharacterController、Fixed KCC 与 DotRecast 各自拥有自己的重力和碰撞后清零规则。把重力伪装成 Locomotion contribution 又会被 Action 的 `Override + ConsumeLowerChannels` 吃掉，使攻击期间错误悬空。正确链路需要在玩法 Motion 仲裁与 WorldSolver 之间增加一个 Target 正式拥有的 Body Motion Integration 阶段，并在 Solver 返回真实碰撞后通过同一个积分合同提交下一 Tick 的垂直状态：

```text
MotionContribution
  -> Channel Resolve
  -> Program Motion Modifier
  -> ResolvedGameplayMotion
  -> BodyMotionIntegrator.Prepare
  -> CharacterMotionRequest
  -> WorldSolver
  -> BodyMotionIntegrator.Finalize
  -> committed WorldBodyState
```

本change先闭合重力、持续下落、落地清零、天花板清零以及动作 Y 位移与重力的组合，不扩张跳跃、空中控制或动作专属重力窗口。

## What Changes

- 将 Motion accumulator 的最终输出从可直接交给 Solver 的 `CharacterMotionRequest`改为只表达玩法仲裁结果的 `ResolvedGameplayMotion`；删除旧的“仲裁后立即构造最终Request”路径。
- 增加版本化、numeric-neutral 的 Body Motion Integration语义，并为Float32与Fixed Target各自实现同一公式、同一状态转换和同一拒绝规则。
- 在 `WorldBodyState` 中增加独立 `VerticalVelocity`。现有 `Velocity` 保持实际位移速度语义，禁止从 `Velocity.Y` 推导重力状态。
- 增加 `CharacterBodyMotionProfile`，显式保存负向 `GravityAcceleration` 与正向 `MaximumFallSpeed`。`CharacterPipelineDefinition`必须引用该Profile，不提供默认值或缺失fallback。
- 将Profile值和Body Motion semantic version编入Semantic IR、Float32/Fixed Program、ProgramHash、source revision及Required World Capability。
- 固定使用半隐式恒加速度积分：先根据上一Tick `VerticalVelocity`、Gravity和TickDelta得到受`-MaximumFallSpeed`限制的新速度，再以新速度生成环境垂直位移。
- Grounded角色也生成本Tick的向下重力位移；地面仍存在时Solver碰撞后清零，走出悬崖时同Tick开始下落，不额外悬空一Tick。
- 现有Timeline/Action MotionCurve的Y delta继续作为作者明确位移，和重力位移相加。Action Override与ConsumeLowerChannels只仲裁玩法channel，不得关闭或消费重力。
- 第一版不提供动作忽略重力、重力缩放或垂直动力覆盖模式；需要这些业务时必须另行增加显式Program语义。
- 增加两阶段Body Motion合同：`Prepare`在Solver前产生唯一integration plan与最终request；`Finalize`只根据同一plan、applied displacement、稳定`Grounded`和方向性Above/Below碰撞形成下一Tick `VerticalVelocity`。只有稳定支撑可以清除向下速度，`Below`本身不能代替稳定Grounded。
- Unity CharacterController Solver与Deterministic KCC Solver声明并实现`AirborneVerticalMotion`通用World Capability，复用Target唯一Body Motion finalizer，不在concrete Solver复制重力公式。
- 当前DotRecast Solver继续只表达Navigation Surface约束，不声明`AirborneVerticalMotion`。绑定需要该能力的Corin Program时，Composition必须在Session Active前明确拒绝；不得把NavMesh投影、边界clamp或保持Grounded伪装成空中移动。
- 提升Float32/Fixed WorldState、Snapshot、Prediction、Authority Baseline、Rollback History与相关canonical codec identity；删除旧payload reader、兼容解析和缺字段默认值。
- 将`VerticalVelocity`纳入WorldStateHash、Rollback Snapshot/Hash、Prediction History、Authority Baseline、HardRecovery与网络比较，确保restore/replay从相同空中动力继续。
- 扩展Structured Trace，关联玩法Y位移、上一垂直速度、重力位移、candidate速度、最终request、Solver碰撞、实际位移和提交后的垂直速度。
- 为Corin创建并绑定唯一正式Body Motion Profile，重新生成Semantic IR、Float32/Fixed Program、Projection及受身份影响的产品manifest。
- 更新current specs与`openspec/project.md`，删除“Motion accumulator直接生成最终Request”和“现有Ground Snap等价于完整垂直运动”的过时表述。

## Capabilities

### New Capabilities

- `character-vertical-body-motion`：定义垂直动力状态、重力积分、玩法Y位移组合、碰撞后状态转换、配置、能力校验、快照与诊断语义。

### Modified Capabilities

- `character-motion-semantics`：在玩法Motion仲裁与最终`CharacterMotionRequest`之间增加唯一Body Motion Integration阶段。
- `character-motion-simulation-boundary`：要求WorldSolver消费已积分Request，并通过Target统一finalizer提交垂直状态。
- `character-pipeline-definition-authoring`：要求Definition引用唯一正式`CharacterBodyMotionProfile`。
- `btsmtl-compiled-simulation-program`：将Body Motion参数、语义版本和Required World Capability编入Program身份。
- `character-simulation-kernel`：将独立`VerticalVelocity`纳入WorldState、codec、Snapshot与Hash。
- `deterministic-kcc-world-solver`：明确KCC只约束已积分位移并报告真实Ground/Above/Below，不私有实现重力。
- `dotrecast-navigation-world-solver`：明确当前Solver缺少空中垂直能力并在Composition拒绝。
- `gameplay-simulation-session-composition`：在Session Active前校验Program的`AirborneVerticalMotion`要求与Solver真实能力。
- `server-authoritative-prediction-correction-pipeline`：让Baseline、History、Correction与HardRecovery覆盖垂直动力状态。
- `deterministic-rollback-network-model`：让Fixed World Snapshot、Hash与Replay保存并恢复垂直动力状态。
- `agent-character-controller-synthesis`：让Agent Snapshot只读投影Body Motion Profile身份、参数与编译状态，不增加第二个Profile写入口。

## Dependencies And Sequencing

- 依赖已经安装的`add-program-motion-modifier-warping`最终链路。Body Motion Integration必须发生在全部Program Motion Modifier之后，不能让重力进入Modifier或channel仲裁。
- 依赖已经安装的`refactor-deterministic-kcc-movement-runtime`最终KCC合同。本change不修改其连续查询算法，只增加已积分位移输入和统一碰撞后垂直状态提交。
- 与未完成的`add-character-equipment-feature-modules`都将修改Semantic IR、Target ABI、State codec和生成产物，不能并行apply同一工作树；后apply的change必须以本change最终identity为基线。
- 与`add-btsmtl-ai-controller-authoring`没有业务依赖，但两者都可能修改Program与Session composition文件；若并行实施必须隔离工作树后串行合并，不能保留双版本codec。
- 当前DotRecast Authority产品使用同一Corin Program。Corin增加`AirborneVerticalMotion`要求后，该产品在DotRecast补齐空中World能力前会在正式Composition阶段拒绝启动。这是明确业务代价，不提供Grounded-only fallback或按Network Model关闭重力。

## Current Spec Comparison

- `character-motion-semantics`当前规定Motion accumulator在channel合成后直接生成`CharacterMotionRequest`。本change将中间结果明确为`ResolvedGameplayMotion`，由Body Motion Integrator生成唯一最终Request。
- `character-motion-simulation-boundary`当前把Request直接交给Solver，但没有表达环境动力如何进入Request，也没有碰撞后动力状态提交。本change补齐Prepare/Finalize两阶段，不允许Graph、Timeline或concrete KCC拥有重力。
- `deterministic-kcc-world-solver`当前完整定义Grounding与Ground Snap，但Ground Snap只允许上一Tick稳定Grounded且落点在SnapDistance内。它与重力没有冲突，也不能继续被描述为离地后的下落方案。
- `character-simulation-kernel`当前WorldState保存Position、Yaw、实际Velocity、Grounded和Collision。新增`VerticalVelocity`后必须提升codec并进入完整World Snapshot；不能复用或重解释旧Velocity字段。
- `btsmtl-compiled-simulation-program`当前Program身份覆盖Operation、State Layout和Motion Modifier，但不包含角色级Body Motion配置。本change将Profile作为Definition正式编译输入，不新增运行时SO读取。
- `character-pipeline-definition-authoring`当前Definition已是Input、GameplayEffect、Animation、Action与Behavior配置装配根。本change增加Body Motion Profile引用，Inspector仍只显示引用和生成状态，不内联一组重力字段。
- `dotrecast-navigation-world-solver`当前只承诺nearest-poly、MoveAlongSurface、height projection、Surface reconstraint与Actor contact，没有空中胶囊世界、天花板或离开NavMesh后的下落。新spec不会把这些不存在的能力写成已完成。
- `server-authoritative-prediction-correction-pipeline`与`deterministic-rollback-network-model`已要求完整World状态恢复，但现有payload没有独立垂直动力字段。本change明确升级单路schema，不保留旧reader。
- `add-program-motion-modifier-warping`的design把“垂直运动”列为该change的Non-Goal，只表示MotionWarp change不实现重力，并非current spec禁止后续能力。本change严格发生在Modifier之后，也继续不实现垂直MotionWarp，因此两者不冲突。
- 现行spec没有要求重力是MotionContribution，因此不存在需要保留的旧Gravity node、Blackboard字段或Timeline窗口。

## Impact

- Authoring：新增`CharacterBodyMotionProfile`、Definition引用、Inspector校验与Corin正式资产。
- Frontend：Definition discovery、source revision、Semantic IR body motion descriptor与diagnostics。
- Target Program：Float32/Fixed body motion descriptor、Required World Capability、Program codec/hash、ABI与Reader。
- Runtime：Float32/Fixed Motion accumulator输出边界、Body Motion Prepare/Finalize、World request、World body state和Solver adapter。
- WorldSolver：Unity CharacterController与Fixed KCC声明完整能力；DotRecast显式不声明并在组合阶段拒绝。
- State：Float32/Fixed WorldState codec、Snapshot、Hash、History与restore。
- Network：ServerAuthoritative Prediction State/Baseline/Checkpoint/Canonical Egress以及Rollback snapshot/hash身份。
- Presentation：继续只消费实际Body Position、Velocity和Grounded；不读取`VerticalVelocity`决定Gameplay，也不产生重力。
- Agent：现行v13 Snapshot增加Body Motion Profile只读摘要与Validator/Compiler诊断；Patch schema和MCP action不新增Profile写操作。
- Generated products：Corin Semantic IR、Float32 Program/Projection、Fixed Program与相关manifest全部重新生成。
- Breaking changes：旧Program ABI、旧WorldState/Snapshot/Prediction/Baseline payload全部拒绝，不提供兼容reader、默认字段或一次性runtime migrator。

## Non-Goals

- 不实现Jump、VerticalImpulse、AirControl、Coyote Time、二段跳、浮空、飞行、游泳或移动平台继承速度。
- 不实现动作专属Gravity Scale、Ignore Gravity、Override Vertical Velocity或按Timeline窗口切换垂直模式。
- 不从MotionCurve某一帧Y delta推断跳跃速度。
- 不修改MotionWarp的平面目标语义，不增加垂直MotionWarp。
- 不把重力做成Graph节点、Blackboard变量、MotionContribution、GameplayEffect周期任务或Timeline Clip。
- 不让KCC、Unity adapter、DotRecast、Presentation或Network Model私有决定重力常量。
- 不在本change补齐DotRecast空中碰撞；它只负责准确声明不支持并拒绝组合。
- 不新增测试或人工验证task，不运行Unity batchmode。

## Stop Conditions

- 如果无法在不复用`WorldBodyState.Velocity.Y`的情况下保存独立垂直动力状态，停止并重新评估WorldState合同。
- 如果Body Motion finalization必须在Unity、Fixed KCC和其它Solver中复制不同碰撞后清零公式，停止并先建立唯一Target finalizer。
- 如果DotRecast产品只能通过关闭重力、保持假Grounded、NavMesh吸附或运行时fallback继续启动，停止并保留正式Composition拒绝。
- 如果旧Snapshot、Prediction或Baseline payload必须以缺字段默认值继续读取，停止；必须完成单路schema升级和受影响产物重建。
- 如果Float32与Fixed不能从同一Semantic descriptor得到同一半隐式恒加速度规则与状态转换，停止，不交付Target分裂实现。
- 如果Corin缺少正式Body Motion Profile或生成产物无法安全重建，停止并报告资产缺口，不在运行时创建默认配置。

## Success Criteria

- 正式链路唯一为`Contribution -> Channel Resolve -> Motion Modifier -> ResolvedGameplayMotion -> Body Motion Prepare -> CharacterMotionRequest -> WorldSolver -> Body Motion Finalize -> committed WorldBodyState`。
- `WorldBodyState.Velocity`继续表示实际位移速度，独立`VerticalVelocity`唯一保存影响未来Tick的垂直动力。
- 角色走出Fixed KCC或Unity CC稳定支持面后立即按相同配置持续下落；稳定落地和撞顶按统一规则清零相应速度，陡坡或其它非稳定下方接触不得结束下落。
- Action/Timeline Y位移与重力相加，Action channel占权不能关闭或消费重力。
- Float32与Fixed使用同一Profile、半隐式积分语义、稳定支撑判定、碰撞后状态转换和失败规则；Fixed restore/replay保持相同空中轨迹与Hash。
- Unity CharacterController与Deterministic KCC声明并真实满足`AirborneVerticalMotion`；DotRecast不声明且在Session Active前拒绝需要该能力的Program。
- ServerAuthoritative Baseline/Correction与Rollback Snapshot/Hash完整覆盖`VerticalVelocity`，没有旧codec、缺字段默认或双写。
- Presentation继续显示Solver实际Body，不读取积分私有状态修正Transform。
- Agent v13 Snapshot能够只读说明Definition绑定的Body Motion Profile、参数、identity与配置错误，Patch仍不能形成第二个Profile写入口。
- Corin具有显式正式Profile，全部受影响生成产物与identity重新生成。
- current specs与`openspec/project.md`不再把Ground Snap、实际Velocity.Y或NavMesh吸附描述为完整重力闭环。
