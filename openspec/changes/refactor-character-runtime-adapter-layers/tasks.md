## 1. Scope Audit
- [ ] 1.1 读取本变更 `proposal.md`、`design.md` 和全部 spec delta。
- [ ] 1.2 读取 `openspec/changes/refactor-locomotion-adapter-modules/tasks.md`，确认 Movement 局部拆分完成状态。
- [ ] 1.3 读取 `openspec/changes/formalize-animation-playback-rollback-authority/tasks.md`，确认 Presenter playback restore 是否仍在修改中。
- [ ] 1.4 读取 `openspec/changes/add-animation-motion-source-pipeline/tasks.md`，确认 TurnBack motion source 是否仍在修改中。
- [ ] 1.5 读取 `openspec/changes/refactor-fullbody-frame-pipeline/tasks.md`，确认 FullBody pipeline phase order 是否稳定。
- [ ] 1.6 统计 `PlayerLocomotionController`、`BasicLocomotionAnimancerPresenter`、`CharacterControllerBasicMotionExecutor`、`PlayerFullBodyActionController`、`FullBodyFramePipeline` 的当前职责和外部调用点。
- [ ] 1.7 标记与 active rollback/playback 变更重叠的文件；重叠处只做边界测试，不做语义迁移。

## 2. Boundary Tests First
- [ ] 2.1 新增静态测试：`Character/Animation/Solver` 不引用 Animancer runtime、Animator、AnimationClip、CharacterController、Transform 或 InputAction。
- [ ] 2.2 新增静态测试：`Character/Movement/Solver` 不调用 `CharacterController.Move` 或写 Transform。
- [ ] 2.3 新增静态测试：`Character/Action/FullBody/Solver` 不创建 `CharacterStateMachineRunner`，不注册 tick driver。
- [ ] 2.4 新增静态测试：`Diagnostics` 模块不调用状态机 transition API、motion executor 或 Animancer play API。
- [ ] 2.5 新增静态测试：正式 runtime 中只有 `PlayerFullBodyActionController` 创建 `CharacterStateMachineRunner`。
- [ ] 2.6 新增静态测试：拆分后没有新增 `Resources.Load`、全局单例配置读取或代码默认 fallback 配置。
- [ ] 2.7 新增日志 key 测试：Locomotion、Animation、MotionExecutor、FullBody 关键 event id 迁移前后保持。

## 3. Animation Presenter Layering
- [ ] 3.1 审计 `BasicLocomotionAnimancerPresenter` 的 alias 解析、播放提交、进度读取、restore、root motion probe 和日志职责。
- [ ] 3.2 若 `formalize-animation-playback-rollback-authority` 未完成，暂停 playback restore 迁移。
- [ ] 3.3 创建或迁移 `LocomotionAnimationAliasResolver`，只处理 phase/gait/context 到 alias 的纯数据解析。
- [ ] 3.4 保持 `BasicLocomotionAnimancerPresenter` 只调用 resolver 结果，不让 resolver 调用 Animancer。
- [ ] 3.5 创建或迁移 `LocomotionAnimationDiagnostics`，保留现有 animation debug event id。
- [ ] 3.6 创建或迁移 `TurnBackRootMotionProbeDiagnostics`，只记录 probe，不写 movement facts。
- [ ] 3.7 更新 Presenter 静态测试，确认它不调用状态机切换、motion executor 或 Transform 写入。
- [ ] 3.8 运行基础移动动画 Presenter 定向 EditMode 测试。

## 4. Motion Executor Layering
- [ ] 4.1 审计 `CharacterControllerBasicMotionExecutor` 的输入 motion、animation delta、rotation、`CharacterController.Move`、rollback state 和日志职责。
- [ ] 4.2 若 `add-prediction-rollback-authority-scopes` 未完成，暂停 rollback state helper 迁移。
- [ ] 4.3 创建或迁移 `AnimationPlanarDeltaResolver`，只把 motion facts 转为平面 delta/yaw 纯数据结果。
- [ ] 4.4 创建或迁移 `MovementCommandResolution` 或等价 helper，保持输入运动和动画运动合成结果不变。
- [ ] 4.5 保持 `CharacterControllerBasicMotionExecutor` 是唯一调用 `CharacterController.Move` 的基础移动 runtime adapter。
- [ ] 4.6 创建或迁移 `MotionExecutorDiagnostics`，保留 motion consumed / suppressed / rollback 相关日志。
- [ ] 4.7 增加 characterization 测试：相同 `BasicLocomotionFrame` 下拆分前后 movement delta 和 yaw 结果一致。
- [ ] 4.8 运行 motion executor 定向 EditMode 测试。

## 5. FullBody Controller Layering
- [ ] 5.1 审计 `PlayerFullBodyActionController` 的引用解析、配置解析、runner 创建、snapshot restore、pipeline facade 和日志职责。
- [ ] 5.2 创建或迁移 `FullBodyReferenceResolver`，只负责同角色层级内引用解析，不做状态机推进。
- [ ] 5.3 创建或迁移 `FullBodyDiagnostics`，保留 active path、owner、action request 和 restore 相关日志。
- [ ] 5.4 评估 `FullBodyStateMachineFactory` 是否需要独立 Module；如果只有一个实现，保持 internal helper，不新增 public Contract。
- [ ] 5.5 保持 `PlayerFullBodyActionController` 作为唯一正式 runner owner。
- [ ] 5.6 保持缺失正式配置时报错，不引入 fallback 配置。
- [ ] 5.7 增加 characterization 测试：同一输入下 current owner、active path、state time、variant 和 pending transition 不变。
- [ ] 5.8 运行 FullBody controller 定向 EditMode 测试。

## 6. FullBody Pipeline Layering
- [ ] 6.1 审计 `FullBodyFramePipeline` 的 phase order、action request gate input、motion build、presentation bridge 和 snapshot/logging 职责。
- [ ] 6.2 创建或迁移 `FullBodyPipelineActionRequestResolver`，只构建 request gate input 和纯数据 gate result。
- [ ] 6.3 保持 `FullBodyFramePipeline` 仍是 frame order 权威，不改变 phase 顺序。
- [ ] 6.4 将 pipeline snapshot 日志迁移到 `FullBodyDiagnostics` 或等价 Diagnostics Module。
- [ ] 6.5 确认 pipeline helper 不直接调用 `CharacterController.Move`、Animancer play API 或状态机 transition API。
- [ ] 6.6 增加 characterization 测试：同一 `FullBodyFrameInput` 下 pipeline step result 与迁移前一致。
- [ ] 6.7 运行 FullBody frame pipeline 定向 EditMode 测试。

## 7. Cross-Layer Static Verification
- [ ] 7.1 搜索拆出的 Model/Solver/Diagnostics，确认没有 `MonoBehaviour` 继承。
- [ ] 7.2 搜索拆出的 Model/Solver/Diagnostics，确认没有 `InputAction`、`CharacterController`、`Animancer` runtime、`Transform` 或 `UnityEngine.Object` 字段。
- [ ] 7.3 搜索拆出的 Module，确认没有 `new CharacterStateMachineRunner`。
- [ ] 7.4 搜索拆出的 Module，确认没有 `RegisterTick` 或等价 tick driver 注册。
- [ ] 7.5 搜索拆出的 Module，确认没有直接 `Debug.Log` 散落在新增状态机诊断逻辑中。
- [ ] 7.6 搜索正式路径，确认没有新增 fallback config 或 `Resources.Load`。

## 8. Automatic Validation
- [ ] 8.1 运行 `dotnet build .\3cDemo\Client\3C_Client\Assembly-CSharp.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [ ] 8.2 运行 `dotnet build .\3cDemo\Client\3C_Client\Assembly-CSharp-Editor.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [ ] 8.3 使用 Unity Test Runner 运行 `Tests.Editor.UnifiedCharacterStateMachineTests`。
- [ ] 8.4 使用 Unity Test Runner 运行基础移动动画相关 EditMode 测试。
- [ ] 8.5 使用 Unity Test Runner 运行 FullBody frame pipeline 相关 EditMode 测试。
- [ ] 8.6 使用 Unity Test Runner 运行 rollback/replay 定向测试；若 active rollback 变更尚未完成，记录阻塞原因。
- [ ] 8.7 运行 `openspec validate refactor-character-runtime-adapter-layers --strict --no-interactive`。
- [ ] 8.8 不运行 Unity batchmode。

## 9. Manual Verification
- [ ] 9.1 打开 Sandbox 场景。
- [ ] 9.2 确认当前角色仍只有 FullBody 正式 driver active。
- [ ] 9.3 WASD 仍进入 Idle、MoveStart、MoveLoop、MoveStop。
- [ ] 9.4 RunEnd 仍能无输入播完回 Idle，中途输入立即回移动阶段。
- [ ] 9.5 RunLoop 反向输入仍进入 TurnBack，motion/input lock 语义不变。
- [ ] 9.6 Shift Dodge Directional 和 Backstep 仍可触发并恢复 Locomotion。
- [ ] 9.7 开启诊断日志后确认 Locomotion、Animation、MotionExecutor、FullBody 关键日志仍可定位。
- [ ] 9.8 若相关 rollback active change 已完成，运行 F6/F8 验证无新增 first mismatch。

## 10. Completion
- [ ] 10.1 确认 Runtime Adapter 只保留 Unity 装配、生命周期、正式外围调用和少量状态缓存。
- [ ] 10.2 确认新 Module 删除后复杂度会回流到多个调用点，避免浅 helper。
- [ ] 10.3 确认没有新增第二状态路径、第二运动路径、第二动画权威或 fallback 配置。
- [ ] 10.4 确认没有删除用户未明确要求删除的 log。
- [ ] 10.5 更新任务状态前逐项核对全部验收。
- [ ] 10.6 全部真实完成后再将 checklist 标为 `- [x]`。
