# Character Runtime Adapter 分层设计

## Context
当前 Character 运行时已经有清晰方向：

- 统一状态机是 FullBody base layer 的状态权威。
- `PlayerFullBodyActionController` 是正式 runner owner。
- `FullBodyFramePipeline` 编排一帧的输入、状态、运动、表现和 snapshot 顺序。
- Locomotion 正在通过 `refactor-locomotion-adapter-modules` 从胖 controller 拆成 facts、motion、TurnBack、snapshot 和 diagnostics 模块。

剩下的架构摩擦在于：Animation Presenter、Motion Executor、FullBody Controller 和 Pipeline 仍是浅 Module。它们的 Interface 包含太多调用方必须知道的细节：何时 resolve 引用、何时 restore playback、何时构建 action gate input、何时提交日志、何时写黑板、哪些方法可以在 rollback 里调用。

本变更不重写主线，只把这些知识归位。

## Goals
- 让 Runtime Adapter 只承担 Unity 装配、生命周期、场景引用和正式外围调用。
- 让 Model 保存纯数据 facts、request、result、restore state 和 frame context。
- 让 Solver 承担纯逻辑决策、转换和采样窗口协作，但不持有 Unity 场景对象。
- 让 Diagnostics 集中日志格式、event id、channel key 和提交逻辑。
- 让 FullBody pipeline 保持一帧顺序权威，同时减少它直接知道的 request gate 和日志细节。
- 通过自动测试和静态边界测试证明拆分没有产生第二状态路径、第二运动路径、第二动画权威或 fallback 配置。

## Non-Goals
- 不改变状态机 transition 条件、state path、owner、variant 或 state time 语义。
- 不改变 TurnBack motion source、animation playback restore 或 rollback snapshot 字段语义。
- 不改变 MotionExecutor 对 `CharacterController.Move` 的唯一正式调用职责。
- 不新增泛型框架、插件式依赖注入或 service locator。
- 不为了抽象而创建只有一个实现且没有真实变化点的 Contracts seam。

## Layer Rules
```text
Assets/Scripts/Character/<Domain>/
  Config/
  Contracts/
  Model/
  Solver/
  Runtime/
  Diagnostics/
  Editor/
```

- `Runtime/` 放 MonoBehaviour、Unity 引用解析、Unity 生命周期、正式 adapter 和 scene/prefab 装配。
- `Model/` 放纯数据输入、facts、request、result、snapshot、restore state 和 frame context。
- `Solver/` 放纯逻辑：alias 解析、motion delta 转换、request gate input 构建、pipeline step 结果转换。
- `Diagnostics/` 放日志提交和格式化。它可以读纯数据 snapshot，但不得重新计算状态、运动或动画权威。
- `Contracts/` 只放真实 seam。已有跨 Runtime Adapter 的接口可以保留；如果只有一个实现且没有第二 adapter，不新增接口。
- `Config/` 只放正式配置类型。不得新增 fallback 配置或硬编码默认配置路线。

## Proposed Module Shape
```text
Character/Animation/
  Runtime/BasicLocomotionAnimancerPresenter.cs
  Solver/LocomotionAnimationAliasResolver.cs
  Solver/LocomotionPlaybackProgressResolver.cs
  Diagnostics/LocomotionAnimationDiagnostics.cs
  Diagnostics/TurnBackRootMotionProbeDiagnostics.cs

Character/Movement/
  Runtime/CharacterControllerBasicMotionExecutor.cs
  Solver/Motion/AnimationPlanarDeltaResolver.cs
  Solver/Motion/MovementCommandResolution.cs
  Diagnostics/MotionExecutorDiagnostics.cs

Character/Action/FullBody/
  Runtime/PlayerFullBodyActionController.cs
  Runtime/FullBodyReferenceResolver.cs
  Solver/FullBodyStateMachineFactory.cs
  Solver/FullBodyPipelineActionRequestResolver.cs
  Diagnostics/FullBodyDiagnostics.cs
```

这些名称是提案级目标，实施时可以采用等价命名，但必须保持职责一致。

## Decisions
- Decision: 先完成 Locomotion 局部拆分，再做跨 Character runtime adapter 拆分。
  - Reason: Locomotion controller 当前仍是最大聚合点，先让它变薄可以降低后续 FullBody/Pipeline 迁移风险。
- Decision: 不以行数作为验收标准。
  - Reason: 深 Module 可能内部行数不少，但 Interface 小、Locality 强；浅 helper 即使很短也会让调用方继续背负复杂度。
- Decision: Runtime Adapter 允许持有 Unity 对象，Solver 和 Model 不允许。
  - Reason: 这样测试可以直接穿过 Solver Interface，不需要场景对象、Animancer runtime 或 Input System。
- Decision: FullBody pipeline 保留 frame order 权威。
  - Reason: `refactor-fullbody-frame-pipeline` 已经把一帧顺序收口，本变更只减少 pipeline 对 helper 细节的直接知识。
- Decision: Presenter 拆分不得改变 playback rollback authority。
  - Reason: playback restore 和 sampling window 正在由 `formalize-animation-playback-rollback-authority` 定义，本变更只能移动可安全移动的 alias、诊断和 adapter 辅助逻辑。
- Decision: Motion Executor 拆分不得新增 motion output。
  - Reason: `CharacterControllerBasicMotionExecutor` 是正式 Unity motion adapter；纯逻辑 resolver 可以计算 delta 或 command，但执行根运动仍必须在正式 adapter 内完成。
- Decision: Diagnostics Module 只移动日志语义，不删除日志。
  - Reason: 项目规则要求 log 等用户明确删除再删，且当前 rollback/TurnBack 调试仍依赖日志定位。

## Sequencing
1. 完成或确认 `refactor-locomotion-adapter-modules` 的 Movement 局部拆分。
2. 为 Animation Presenter 增加静态边界和日志 key 测试，再迁移 alias/diagnostics。
3. 为 Motion Executor 增加 motion command characterization 测试，再迁移 planar delta/helper 逻辑。
4. 为 FullBody Controller 增加 runner owner、reference resolution 和 config source 静态测试，再迁移 resolver/diagnostics。
5. 为 FullBodyFramePipeline 增加 phase order characterization 测试，再迁移 action request resolver 和 pipeline diagnostics。
6. 最后统一检查所有拆出 Module 不引用被禁止的 Unity runtime 类型，不创建 runner，不注册 tick driver，不新增 fallback 配置。

## Risks / Trade-offs
- Risk: 与 `formalize-animation-playback-rollback-authority` 同时修改 Presenter restore 语义。
  - Mitigation: 本变更不得修改 restore 语义；若必须修改，停止并转到该 active change。
- Risk: 与 `add-animation-motion-source-pipeline` 同时修改 TurnBack motion source。
  - Mitigation: 本变更不得恢复 `OnAnimatorMove` pending delta 或新增 Animator runtime direct route。
- Risk: FullBody pipeline helper 过度拆分导致浅 Module。
  - Mitigation: 每个新 Module 必须通过 deletion test；删除它时复杂度会回流到多个调用点，才说明它值得存在。
- Risk: Runtime Adapter 变薄后调试入口分散。
  - Mitigation: Diagnostics Module 必须保留稳定 event id 和 channel key，并增加日志格式测试。

## Verification Strategy
- 自动测试：
  - 静态边界测试：拆出的 Solver/Model/Diagnostics 不引用 `MonoBehaviour`、`CharacterController`、`Animancer` runtime、`InputAction`、`Transform` 或 `UnityEngine.Object`。
  - 权威测试：只有 `PlayerFullBodyActionController` 在正式 runtime 创建 `CharacterStateMachineRunner`。
  - 行为 characterization：同一输入下 state path、motion command、animation request、snapshot facts 和关键日志 event id 不变。
  - Pipeline 测试：phase order、request gate result 和 presentation write-back 顺序不变。
- 手动验证：
  - Sandbox 中验证 WASD、RunEnd、TurnBack、Dodge Directional、Dodge Backstep 语义不变。
  - 开启诊断日志验证 Locomotion、Animation、MotionExecutor、FullBody 关键日志仍可定位。
  - F6/F8 rollback/synctest 如相关 active change 已完成，则验证没有因拆分产生 first mismatch。

## Open Questions
- `BasicLocomotionAnimancerPresenter` 的 playback progress resolver 是否等 `formalize-animation-playback-rollback-authority` 完成后再迁移。
- `FullBodyStateMachineFactory` 是否有真实第二 adapter 或测试 seam；如果没有，实施时应保持为 internal helper，而不是新增 public Contract。
- Motion Executor 的 rollback state helper 是否等 `add-prediction-rollback-authority-scopes` 完成后再迁移。
