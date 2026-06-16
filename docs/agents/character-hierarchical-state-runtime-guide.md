# 自研统一分层状态机运行时指南

本文是当前角色 FullBody base layer 的状态机主线指南。UnityHFSM 只作为第三方库参考；未经新的 OpenSpec 审批，不得把 UnityHFSM 接入为正式角色状态机 engine。

## 当前主线

```text
PlayerFullBodyActionController
  -> FullBodyFramePipeline
  -> CharacterStateTimelineFactSampler
  -> FullBodyPipelineActionRequestResolver
  -> CharacterStateMachineRunner
  -> CharacterStateOutputResolver
  -> CharacterStateMachineFrame
  -> ActionMotionResolver
  -> motion executor / Animancer presenter / diagnostics
```

`FullBody/Locomotion/...` 和 `FullBody/Action/...` 属于同一棵统一分层状态机。统一表示单一状态权威，分层表示路径和父子域表达方式，两者不冲突。

## 目录规划

- `Assets/Scripts/Character/StateMachine/Model/`：纯数据模型、snapshot、frame、binding 和 validation result。
- `Assets/Scripts/Character/StateMachine/Config/`：状态机 ScriptableObject 包装和转换入口。
- `Assets/Scripts/Character/StateMachine/Solver/Runtime/`：runner 和内部状态生命周期。
- `Assets/Scripts/Character/StateMachine/Solver/Timeline/`：timeline window 和 animation facts 采样。
- `Assets/Scripts/Character/StateMachine/Solver/Transition/`：状态图 transition 条件评估。
- `Assets/Scripts/Character/StateMachine/Solver/Output/`：state output 到 frame 的纯数据解析。
- `Assets/Scripts/Character/StateMachine/Solver/Validation/`：配置校验。
- `Assets/Configs/3C/StateMachine/`：中心状态机配置资产。旧 `Assets/Configs/3C/Statemachine/` 不作为并行入口保留。

## Runtime 边界

- `CharacterStateMachineRunner` 只解释状态图、选择 transition、维护 active state、state time、variant、direction payload、pending transition 和 snapshot/restore。
- `FullBodyFramePipeline` 在 Action request gate 前采样 current `StateTimelineWindowFacts`，并把同一个 facts trace 传给 request gate、runner 和 output resolver。
- `CharacterStateTimelineFactSampler` 根据 active snapshot、runtime animation facts 和 timeline policy 采样 `StateTimelineWindowFacts`，不切换状态、不提交日志、不理解请求准入。
- `CharacterStateNodeLifecycle` 提供内部 `Enter / Tick / Exit`，只向 `CharacterStateMachineFrameBuilder` 写纯数据输出。
- `CharacterStateOutputResolver` 把当前 state output 解析成 animation、input consume、run latch、TurnBack policy 和 Action motion spec 等纯数据 frame 输出，不构造 `ActionMovementCommand`，也不计算动作本帧距离。
- `ActionMotionResolver` 把 Action motion spec、deltaTime、state time 和 timeline facts 解析成本帧 `ActionMovementCommand`、completed、run latch 派生和诊断摘要；它不执行移动。
- `FullBodyFramePipeline` 负责输入消费、BuildMotion 阶段调用 Action motion resolver、ExecuteMotion 阶段提交 motion executor、动画提交、黑板写入和诊断提交。

状态机节点只保存 `animationKey` 和 `timelineBindingKey`。具体 clip、transition asset、fade、speed、start time 归 Animancer TransitionLibrary、`RunLocomotionAnimationConfigSO` 或等价动画配置入口。

## Timeline facts 术语

- current facts：FullBody 帧管线在 `GameplayDecision` 中、Action request gate 前生成的当前状态事实包；request gate、transition evaluator 和 output resolver 必须能观察同一个 current facts trace。
- projected facts：runner 为 transition evaluation 采样的 `StateTime + DeltaTime` 视角，只用于 transition 判断，不写回 current facts，也不传给 request gate。
- target facts：发生 transition 后，runner 用目标状态和目标 state time 采样的进入帧事实，只作为 target trace 和目标状态 Enter/Tick 的局部输入。
- `StateTimelinePolicy` 仍是窗口数据权威；上述三类 facts 只描述一帧内采样和传递归属。
- `state-timeline-window-facts` 日志由 FullBody diagnostics 外围提交，message 必须标明 `source=current`、`source=projected` 或 `source=target`。

## Transition 条件职责

- `CharacterStateTransitionCondition` 只保存受控 `condition kind`、请求类型、数值阈值和 tag 等纯数据参数，不保存 `MonoBehaviour`、`ScriptableObject` evaluator 或可执行回调。
- `CharacterStateTransitionEvaluatorCollection` 是正式运行时求值入口，默认装配顺序为 Core、Locomotion、Animation、Action。
- Core evaluator 处理移动意图、无移动意图、状态可退出、已 accepted input fact、elapsed time 和当前状态 tag。
- Locomotion evaluator 处理 `MoveTurnBackRequested` 等 Locomotion facts；它只读取已派生的 `LocomotionDecisionFacts`，不重新读取输入或相机。
- Animation evaluator 处理 Locomotion 播放进度可退出事实；Action evaluator 处理 Action 播放进度可退出事实。两者只读取 runtime blackboard / timeline facts，不读取 Animancer state；需要预判自然退出窗口时读取 projected facts，不覆盖 current facts。
- `CharacterStateMachineValidator` 必须发现缺失 evaluator key 和重复 evaluator key。缺失 key 不允许在运行时静默当作 false。
- `CharacterStateMachineRunner` 只把 condition 交给 collection，不包含 TurnBack、Dodge、Attack、Jump、HitReact 等业务条件分支，也不直接提交业务条件诊断日志。
- Condition trace 是 `CharacterStateMachineFrame` 的纯数据输出，`FullBodyDiagnostics` 负责把需要输出的 trace 转成 `RuntimeDiagnosticLog`。

## 配置权威

- path：由 `CharacterStateId` / `CharacterStateMachineDefinitionSO` 表达状态树层级和 transition source / target。
- tag：只做状态分类和通用条件匹配，不承载行为实现。
- module type：声明节点输出和 facts 权威，如 motion、animation、input consume、run latch、timeline window。
- condition key：声明 transition 的纯数据输入契约；新增业务条件必须新增或扩展正式 evaluator adapter。
- 不新增 fallback 字段；缺配置、缺 adapter、重复 key 都走配置校验或构造错误。

## 目录规划

- 纯数据模型：`Assets/Scripts/Character/StateMachine/Model/`
- ScriptableObject 类型：`Assets/Scripts/Character/StateMachine/Config/`
- Runner 和节点生命周期：`Assets/Scripts/Character/StateMachine/Solver/Runtime/`
- Timeline facts 采样：`Assets/Scripts/Character/StateMachine/Solver/Timeline/`
- Transition 判断：`Assets/Scripts/Character/StateMachine/Solver/Transition/`
- Frame 输出解析：`Assets/Scripts/Character/StateMachine/Solver/Output/`
- 配置校验：`Assets/Scripts/Character/StateMachine/Solver/Validation/`
- 状态机资产：`Assets/Configs/3C/StateMachine/`

## 禁止路径

- 不新增第二个 runner owner。
- 不恢复 `BasicLocomotionStateMachine` 或 `LocomotionStateGraphConfigSO` 作为正式权威。
- 不让状态生命周期直接调用 Animancer、Animator、CharacterController、InputAction 或 Transform。
- 不让 `CharacterStateOutputResolver` 重新承载 Action 位移数学；新增 Attack、Jump、HitReact 等动作位移时扩展 motion spec / ActionMotionResolver。
- 不通过 Resources、旧字段或代码默认状态图隐式恢复缺失配置。
- 不让 transition condition 引用场景对象、运行时组件或 ScriptableObject 回调。
- 不删除现有 log，除非用户明确要求。

## 自动验证

优先运行以下 EditMode 测试：

```text
Tests.Editor.UnifiedCharacterStateMachineTests
Tests.Editor.Simulation.FullBodyRollbackReplayTests
Tests.Editor.Simulation.LocalRollbackSynctestFoundationTests
Tests.Editor.ActionInterruptArbiterTests
Tests.Editor.ActionInterruptPolicyDataTests
Tests.Editor.LocomotionFootPhaseMatchingTests
```

命令行验证：

```text
dotnet build .\Assembly-CSharp.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly
dotnet build .\Assembly-CSharp-Editor.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly
openspec validate refactor-character-hierarchical-state-runtime --strict --no-interactive
```

如果 `dotnet build --no-restore` 因 `Temp/obj/.../project.assets.json` 缺失而失败，先以 Unity Editor 编译和 EditMode 测试为准，并在交付说明中记录该环境问题。

## Sandbox 手动验证

在 `Assets/Scenes/Sandbox.unity` 中进入 Play Mode：

1. WASD：观察 active path 在 `FullBody/Locomotion/Idle -> MoveStart -> MoveLoop -> MoveStop -> Idle` 间切换。
2. Run/MoveLoop：Directional Dodge 完成后继续按移动，验证 Run latch 进入 Run gait，松开后 MoveStop 使用最后 moving gait。
3. TurnBack：Run MoveLoop 中快速反向输入，Console 搜索 `locomotion-turnback-condition`、`state-timeline-window-facts`、`turnback-root-motion-consumed`，并确认 timeline facts 日志能区分 `source=current`、`source=projected` 和 transition 后的 `source=target`。
4. Dodge Directional：有移动输入时按 Shift，验证进入 `FullBody/Action/Dodge` 的 `Directional` variant，动作位移进入 motion executor，动画请求 key 为 `Action.Dodge.Directional`。
5. Dodge Backstep：无移动输入时按 Shift，验证进入 `Backstep` variant，完成后不写 Run latch。
6. 状态路径诊断：Console 搜索 `fullbody-path-changed`、`fullbody-pending-transition-changed`、`fullbody-tick-snapshot`。
7. Rollback：已挂载对应 rollback debug runner 时按 F6/F8，Console 搜索 `rollback-synctest`、`first-mismatch`、`ROLLBACK_SOAK_RESULT`。
8. 动画替换：只替换 Animancer TransitionLibrary 或动作动画配置入口中的 `Action.Dodge.Directional` / `Action.Dodge.Backstep` 资源，状态机资产不应需要修改。
