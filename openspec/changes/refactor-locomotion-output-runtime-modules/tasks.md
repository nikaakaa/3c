## 1. Context Lock
- [x] 1.1 读取 `ILocomotionOutputRuntimePort` 当前契约。
- [x] 1.2 读取 `PlayerLocomotionController` output/apply 方法区域。
- [x] 1.3 读取 `CharacterMotionDriver` 和 motion executor contracts。
- [x] 1.4 读取 `BasicLocomotionAnimancerPresenter` 调用边界。
- [x] 1.5 读取 runtime blackboard facts 写入测试。
- [x] 1.6 读取 rollback camera basis tests。
- [x] 1.7 对 `PlayerLocomotionController` output 方法运行 GitNexus impact analysis。
- [x] 1.8 记录 HIGH/CRITICAL 风险并等待用户确认后再实施。

## 2. Characterization Tests
- [x] 2.1 静态测试：Locomotion output modules 不创建 runner。
- [x] 2.2 静态测试：Locomotion output modules 不调用 `CharacterController.Move`。
- [x] 2.3 静态测试：Locomotion output modules 不直接读取 InputAction。
- [x] 2.4 行为测试：基础移动 motion executor 调用次数不变。
- [x] 2.5 行为测试：locomotion animation presenter 调用时机不变。
- [x] 2.6 行为测试：action facts 写入 source step 不变。
- [x] 2.7 行为测试：animation facts 写入 source step 不变。
- [x] 2.8 行为测试：camera basis sync restore 不变。
- [x] 2.9 行为测试：idle 后 run latch reset 不变。

## 3. Output Modules
- [x] 3.1 创建 Locomotion motion output applier。
- [x] 3.2 创建 Locomotion animation output presenter。
- [x] 3.3 创建 Locomotion runtime blackboard writer。
- [x] 3.4 创建 Locomotion output completion module。
- [x] 3.5 创建 Locomotion output runtime adapter。
- [x] 3.6 将 `ExecuteLocomotionMotion` 委托到 motion output applier。
- [x] 3.7 将 `PresentLocomotionAnimation` 委托到 animation output presenter。
- [x] 3.8 将 `WriteActionFacts` 委托到 blackboard writer。
- [x] 3.9 将 `WriteAnimationFacts` 委托到 blackboard writer。
- [x] 3.10 将 `CompleteLocomotionTick` 委托到 completion module。

## 4. Controller Narrowing
- [x] 4.1 保留 controller 的 serialized references。
- [x] 4.2 保留 input source 装配。
- [x] 4.3 保留 motion executor 装配。
- [x] 4.4 保留 animation presenter 装配。
- [x] 4.5 保留 camera controller 装配。
- [x] 4.6 保留 rollback camera basis provider 语义。
- [x] 4.7 确认 controller 不恢复 direct gameplay tick。
- [x] 4.8 确认 FullBody output 仍只通过 Locomotion output port 访问。

## 5. Validation
- [x] 5.1 运行相关 Unity EditMode 定向测试。
- [x] 5.2 运行 `dotnet build 3cDemo\Client\3C_Client\Assembly-CSharp.csproj --no-restore`。
- [x] 5.3 运行 `dotnet build 3cDemo\Client\3C_Client\Assembly-CSharp-Editor.csproj --no-restore`。
- [x] 5.4 运行 `openspec validate refactor-locomotion-output-runtime-modules --strict --no-interactive`。
- [x] 5.5 运行 GitNexus `detect-changes` 并记录影响范围。

## 6. Scope Gates
- [x] 6.1 搜索 output module，确认没有调用 `TryPrepareDecisionFrame`。
- [x] 6.2 搜索 output module，确认没有调用 `TryEvaluatePreparedGameplayDecision`。
- [x] 6.3 搜索 output module，确认没有调用 `TryBuildMotionFromStateDecision`。
- [x] 6.4 搜索 output module，确认没有直接引用 raw InputAction。
- [x] 6.5 搜索 output module，确认没有直接调用 `CharacterController.Move`。
- [x] 6.6 搜索 output module，确认没有直接设置 `Transform.position`。
- [x] 6.7 搜索 output module，确认没有创建或推进状态机 runner。
- [x] 6.8 搜索 FullBody output，确认只通过 `ILocomotionOutputRuntimePort` 访问 Locomotion output。

## 7. Fine-Grained Completion Checks
- [x] 7.1 motion applier 只依赖 formal motion executor Interface。
- [x] 7.2 animation presenter wrapper 只依赖 formal presenter Interface。
- [x] 7.3 blackboard writer 使用 upstream frame/result source step。
- [x] 7.4 blackboard writer 不读取未来帧输入。
- [x] 7.5 completion module 将 camera sync 和 latch reset 顺序写入测试。
- [x] 7.6 adapter 方法体只有参数整理和委托。
- [x] 7.7 controller output methods 不再包含业务分支。
- [x] 7.8 rollback camera basis restore 和 idle run latch reset 都有测试覆盖。
- [x] 7.9 direct tick 入口仍被测试标记为非 authoritative path。
- [x] 7.10 删除迁移期间产生的未使用 wrapper 或重复 helper。
