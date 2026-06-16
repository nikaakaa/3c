## 1. Context Lock
- [x] 1.1 读取 `refactor-character-runtime-ports` 文档，确认 Locomotion frame/output 端口当前形态。
- [x] 1.2 读取 `PlayerLocomotionController` 的 prepare/evaluate/build 方法。
- [x] 1.3 读取 `LocomotionFrameBuilder`，确认纯 solver 边界。
- [x] 1.4 读取 Locomotion snapshot/rollback tests。
- [x] 1.5 读取 TurnBack motion facts 相关测试。
- [x] 1.6 对 `PlayerLocomotionController` 运行 GitNexus upstream impact analysis。
- [x] 1.7 对 `LocomotionFrameBuilder` 运行 GitNexus upstream impact analysis。
- [x] 1.8 记录 HIGH/CRITICAL 风险并等待用户确认后再实施。

## 2. Characterization Tests
- [x] 2.1 静态测试：`LocomotionFrameBuilder` 不调用 motion executor。
- [x] 2.2 静态测试：`LocomotionFrameBuilder` 不调用 animation presenter。
- [x] 2.3 静态测试：`LocomotionFrameBuilder` 不引用 `MonoBehaviour`、`Transform`、`CharacterController`。
- [x] 2.4 行为测试：prepare decision facts 与当前实现一致。
- [x] 2.5 行为测试：prepared gameplay decision 与当前实现一致。
- [x] 2.6 行为测试：state decision 到 motion frame 与当前实现一致。
- [x] 2.7 行为测试：RunLatch restore 与当前实现一致。
- [x] 2.8 行为测试：TurnBack pending intent restore 与当前实现一致。
- [x] 2.9 行为测试：camera basis rollback override 与当前实现一致。

## 3. Runtime State Store
- [x] 3.1 创建 Locomotion runtime state store。
- [x] 3.2 迁移 current intent 存取。
- [x] 3.3 迁移 current frame 和 current phase time 存取。
- [x] 3.4 迁移 run latch 存取。
- [x] 3.5 迁移 last moving gait 存取。
- [x] 3.6 迁移 previous world direction 存取。
- [x] 3.7 迁移 pending TurnBack intent 存取。
- [x] 3.8 保持 rollback snapshot capture/restore 字段一致。

## 4. Frame Runtime Providers
- [x] 4.1 创建 prepare facts provider。
- [x] 4.2 创建 spatial facts provider。
- [x] 4.3 创建 phase facts/settings provider。
- [x] 4.4 创建 motion facts provider。
- [x] 4.5 将 camera/facing resolve 作为 provider 输入，不进入 pure builder。
- [x] 4.6 将 playback progress/window 读取限制在 runtime provider。
- [x] 4.7 保持 `RuntimeBlackboardSnapshot` read-only 输入语义。

## 5. Port Implementation Migration
- [x] 5.1 创建 `LocomotionFrameRuntime` 或等价编排模块。
- [x] 5.2 创建 `LocomotionFrameRuntimeAdapter` 或等价生产 adapter。
- [x] 5.3 迁移 `TryPrepareDecisionFrame` 委托。
- [x] 5.4 迁移 `TryEvaluatePreparedGameplayDecision` 委托。
- [x] 5.5 迁移 `TryBuildMotionFromStateDecision` 委托。
- [x] 5.6 保持 direct tick retired diagnostic。
- [x] 5.7 确认 controller 不创建 runner。
- [x] 5.8 确认 FullBody submission builder 不引用 `PlayerLocomotionController`。

## 6. Validation
- [x] 6.1 运行相关 Unity EditMode 定向测试。
- [x] 6.2 运行 `dotnet build 3cDemo\Client\3C_Client\Assembly-CSharp.csproj --no-restore`。
- [x] 6.3 运行 `dotnet build 3cDemo\Client\3C_Client\Assembly-CSharp-Editor.csproj --no-restore`。
- [x] 6.4 运行 `openspec validate refactor-locomotion-frame-runtime-modules --strict --no-interactive`。
- [x] 6.5 运行 GitNexus `detect-changes` 并记录影响范围。

## 7. Scope Gates
- [x] 7.1 搜索 `LocomotionFrameBuilder`，确认没有新增 `MonoBehaviour` 引用。
- [x] 7.2 搜索 `LocomotionFrameBuilder`，确认没有新增 `Transform`、`Camera` 或 `CharacterController` 引用。
- [x] 7.3 搜索 frame runtime module，确认没有调用 motion executor。
- [x] 7.4 搜索 frame runtime module，确认没有调用 animation presenter。
- [x] 7.5 搜索 frame runtime module，确认没有创建或推进状态机 runner。
- [x] 7.6 搜索 `FullBodySubmissionBuilder`，确认没有 concrete controller 引用。
- [x] 7.7 搜索 `PlayerLocomotionController`，确认 prepare/evaluate/build 不再各自复制实现。
- [x] 7.8 搜索正式代码，确认 Locomotion direct tick 没有重新成为正式主线。

## 8. Fine-Grained Completion Checks
- [x] 8.1 `LocomotionRuntimeStateStore` 覆盖所有 restorable Locomotion local state。
- [x] 8.2 state store 不保存 Unity scene object。
- [x] 8.3 prepare facts provider 不消费 one-shot action request。
- [x] 8.4 spatial facts provider 输出 plain vector facts。
- [x] 8.5 motion facts provider 不执行 motion apply。
- [x] 8.6 `LocomotionFrameRuntime` 明确编排 prepare/evaluate/build 顺序。
- [x] 8.7 adapter 不包含 gameplay 决策分支。
- [x] 8.8 controller debug properties 可以从 state store 或 last result 读取。
- [x] 8.9 rollback snapshot 字段名和语义保持兼容。
- [x] 8.10 迁移后删除未被生产路径和测试使用的临时 wrapper。
