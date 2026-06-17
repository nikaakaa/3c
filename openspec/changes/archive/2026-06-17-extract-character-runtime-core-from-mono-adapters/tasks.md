## 1. Baseline and Impact Review
- [x] 1.1 读取本变更 `proposal.md`、`design.md` 和全部 spec deltas。
- [x] 1.2 读取 `formalize-character-frame-module-architecture` 的相关 delta，确认不重复实现 frame plan、submitter graph 或 config root。
- [x] 1.3 读取 `separate-rollback-debug-rig-from-character-runtime` 的相关 delta，确认本变更只消费显式 replay target 合同。
- [x] 1.4 使用 GitNexus impact 分析 `CharacterFrameRuntimeController` 的上游影响并记录风险。
- [x] 1.5 使用 GitNexus impact 分析 `PlayerLocomotionController` 的上游影响并记录风险。
- [x] 1.6 使用 GitNexus impact 分析 `FullBodyActionRuntime` 的上游影响并记录风险。
- [x] 1.7 列出当前正式 prefab/scene 中角色 runtime 相关 MonoBehaviour 装配，区分正式 adapter 与 debug tooling。

## 2. Characterization Tests
- [x] 2.1 新增 EditMode 测试锁定 `CharacterFramePipeline` phase 顺序。
- [x] 2.2 新增 EditMode 测试锁定一帧内只存在一个正式 `CharacterFramePipeline` owner。
- [x] 2.3 新增 EditMode 测试锁定 motion executor 仍为唯一正式运动出口。
- [x] 2.4 新增 EditMode 测试锁定 animation presenter 仍为唯一正式动画出口。
- [x] 2.5 新增 EditMode 测试覆盖 Locomotion snapshot capture/restore 当前行为。
- [x] 2.6 新增 EditMode 测试覆盖 Action state machine snapshot capture/restore 当前行为。
- [x] 2.7 新增静态测试扫描正式 runtime 代码，不允许新增隐藏 fallback 配置路径。

## 3. Core Shell
- [x] 3.1 在 Character/Pipeline/Runtime 或批准目录新增 pure C# runtime core 类型。
- [x] 3.2 为 core 定义显式 dependencies 数据结构或构造参数。
- [x] 3.3 让 core 持有单个 `CharacterFrameRuntimeHost`。
- [x] 3.4 让 core 暴露正式 tick 入口。
- [x] 3.5 让 core 暴露必要 phase run 入口。
- [x] 3.6 让 core 暴露 capture/restore 入口。
- [x] 3.7 为 core 增加无 GameObject 的构造测试。
- [x] 3.8 为 core 增加无 GameObject 的 tick 测试。
- [x] 3.9 为 core 增加无 GameObject 的 capture/restore 测试。

## 4. Character Controller Adapter
- [x] 4.1 将 `CharacterFrameRuntimeController` 的正式 runtime host ownership 迁到 core。
- [x] 4.2 保留 `CharacterFrameRuntimeController` 的 Unity Update/tick adapter 职责。
- [x] 4.3 将 `CharacterFrameRuntimeController` 的 config 注入改为注入 core dependencies。
- [x] 4.4 将 `CharacterFrameRuntimeController` 的 input 注入改为注入 core dependencies。
- [x] 4.5 将 `CharacterFrameRuntimeController` 的 motion/animation adapter 引用作为 core dependencies 传入。
- [x] 4.6 移除或隔离 controller-backed 正式 runtime port。
- [x] 4.7 增加测试证明 controller 不直接创建第二 pipeline。
- [x] 4.8 增加测试证明 controller 只持有一个 core。

## 5. Runtime Port Migration
- [x] 5.1 将正式 `ICharacterFrameRuntimePort` 实现改为 core-backed。
- [x] 5.2 将 Locomotion frame runtime port 暴露从 Mono owner 迁到 core-owned module。
- [x] 5.3 将 Locomotion output runtime port 暴露从 Mono owner 迁到 core-owned module。
- [x] 5.4 将 Action state machine/runtime port 暴露从 Mono owner 迁到 core-owned module。
- [x] 5.5 将 output runtime 调用改为通过 core-owned modules。
- [x] 5.6 增加静态测试阻止正式 port 依赖 `CharacterFrameRuntimeController` 查找 runtime state。
- [x] 5.7 增加行为测试证明 port phase 调用顺序不变。

## 6. Locomotion Runtime Ownership
- [x] 6.1 将 `LocomotionRuntimeStateStore` 正式 owner 从 `PlayerLocomotionController` 迁出。
- [x] 6.2 将 `CharacterRuntimeBlackboard` 正式 owner 从 `PlayerLocomotionController` 迁出。
- [x] 6.3 将 Locomotion frame runtime host 从 `PlayerLocomotionController` 嵌套类型迁出或替换为 pure host。
- [x] 6.4 将 Locomotion output runtime host 从 `PlayerLocomotionController` 嵌套类型迁出或替换为 pure host。
- [x] 6.5 将 `PlayerLocomotionController` 降级为 Unity adapter 或兼容 facade。
- [x] 6.6 保持 `PlayerLocomotionController.AutoUpdate` 不作为正式主线。
- [x] 6.7 增加测试证明 Locomotion state store 只有一个正式 owner。
- [x] 6.8 增加测试证明 direct tick 不恢复为正式主线。
- [x] 6.9 增加测试覆盖 Locomotion 被 Action claim 压制时不执行副作用。

## 7. Action Runtime Ownership
- [x] 7.1 将 `CharacterStateMachineRuntime` 正式 owner 从 `FullBodyActionRuntime` 迁出。
- [x] 7.2 将 `ActionLifecycleRuntime` 正式 owner 从 `FullBodyActionRuntime` 迁出。
- [x] 7.3 将 Action output runtime host 从 `FullBodyActionRuntime` 迁出或替换为 pure host。
- [x] 7.4 将 `FullBodyActionRuntime` 降级为 Unity adapter 或兼容 facade。
- [x] 7.5 保持 Action request、interrupt、lifecycle 和 body claim 仍通过 Action module 进入 frame plan。
- [x] 7.6 增加测试证明 Action state machine runner 只有一个正式 owner。
- [x] 7.7 增加测试证明 Action lifecycle restore 不依赖 MonoBehaviour 生命周期。
- [x] 7.8 增加测试覆盖 Dodge 完整播放与结束释放 claim 的现有语义。

## 8. Prefab and Tooling Boundary
- [x] 8.1 更新正式 Corin prefab/scene 装配，使正式 gameplay 只依赖角色 runtime adapter 和 core dependencies。
- [x] 8.2 增加 prefab/scene 静态测试，阻止正式角色挂载 rollback debug runner、history recorder 或 replay adapter。
- [x] 8.3 增加 prefab/scene 静态测试，阻止正式角色保留第二 runtime host、第二 runner、第二 motion executor 或第二 animation presenter。
- [x] 8.4 确认 rollback debug rig 通过显式目标引用连接角色 runtime，不创建第二 core。
- [x] 8.5 删除实施过程中产生的临时兼容字段或记录保留原因和删除条件。

## 9. Validation
- [x] 9.1 运行 `openspec validate extract-character-runtime-core-from-mono-adapters --strict --no-interactive`。
- [x] 9.2 运行与 Character Frame Pipeline 相关的 EditMode 测试。
- [x] 9.3 运行与 Locomotion runtime 相关的 EditMode 测试。
- [x] 9.4 运行与 Action runtime 相关的 EditMode 测试。
- [x] 9.5 运行与 prefab/scene boundary 相关的 EditMode 静态测试。
- [x] 9.6 运行 GitNexus `detect_changes()` 确认影响范围符合本变更。
- [x] 9.7 确认所有任务完成后，再将本清单全部更新为 `- [x]`。
