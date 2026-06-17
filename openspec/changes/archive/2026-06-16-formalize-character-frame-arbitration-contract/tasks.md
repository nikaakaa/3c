## 1. 准备
- [x] 1.1 读取本变更 `proposal.md`。
- [x] 1.2 读取本变更 `design.md`。
- [x] 1.3 读取本变更 `tasks.md`。
- [x] 1.4 读取 `character-frame-pipeline` 当前 spec。
- [x] 1.5 读取 `character-runtime-ports` 当前 spec。
- [x] 1.6 读取 `fullbody-action-framework` 当前 spec。
- [x] 1.7 读取 `wasd-locomotion-pipeline` 当前 spec。
- [x] 1.8 搜索规格中 `Locomotion 作为 FullBody`、`FullBody 主调度入口`、`FullBody 子职责` 等目标口径冲突描述。
- [x] 1.9 搜索运行时代码中 `FullBodySubmissionBuilder`、`CharacterFramePipelineHost`、`PlayerFullBodyActionController` 的当前调用链。
- [x] 1.10 对准备修改的运行时 symbol 运行 GitNexus impact analysis，并记录 blast radius：`CharacterFramePipeline` LOW / direct callers 1 / affected processes 0；`CharacterFrameOutputComposer` LOW / direct callers 1 / affected processes 0；`CharacterFrameContext` LOW / direct callers 0；`CharacterFrameOutput` LOW / direct callers 0；`FakeActionAnimationPresenter` LOW / direct callers 0。

## 2. 契约和模型
- [x] 2.1 定义 Character frame owner 的正式职责。
- [x] 2.2 定义 sibling frame submitter 的正式职责。
- [x] 2.3 定义 BodyArbiter 的纯逻辑职责。
- [x] 2.4 定义 CharacterFramePlan 的纯数据职责。
- [x] 2.5 定义 BodyOccupancyDecision 的表达范围。
- [x] 2.6 定义 FullBody occupancy claim 的语义。
- [x] 2.7 定义 Locomotion candidate output 的语义。
- [x] 2.8 定义 UpperBody future submitter 的接入前置条件。
- [x] 2.9 定义 current FullBody integrated submitter 的迁移期边界。
- [x] 2.10 确认契约不新增 fallback 配置。

## 3. 测试先行
- [x] 3.1 增加静态测试：正式规格不得把目标架构描述为 FullBody 拥有 Locomotion。
- [x] 3.2 增加静态测试：保留旧口径时必须出现迁移期、兼容入口或 legacy 标记。
- [x] 3.3 增加静态测试：新增 UpperBody runtime 前必须存在 BodyArbiter 或 CharacterFramePlan contract。
- [x] 3.4 增加静态测试：`CharacterFramePipeline` 不直接包含具体 UpperBody/FullBody/Locomotion 优先级硬编码。
- [x] 3.5 增加静态测试：BodyArbiter 不引用 Unity scene object、Animancer、Animator 或 CharacterController。
- [x] 3.6 增加 EditMode 测试：FullBody occupancy claim 可以压制 Locomotion output。
- [x] 3.7 增加 EditMode 测试：无 FullBody occupancy claim 时 Locomotion output 可成为 base layer 输出。
- [x] 3.8 增加 EditMode 测试：UpperBody claim 不得隐式压制 base Locomotion output。
- [x] 3.9 增加 EditMode 测试：BodyArbiter result 可被 output composer 消费为纯数据 plan。
- [x] 3.10 增加边界测试：Presenter、motion executor、runner 不参与 BodyArbiter 决策。

## 4. 规格更新
- [x] 4.1 更新 `character-frame-pipeline` spec delta。
- [x] 4.2 更新 `character-runtime-ports` spec delta。
- [x] 4.3 更新 `fullbody-action-framework` spec delta。
- [x] 4.4 更新 `wasd-locomotion-pipeline` spec delta。
- [x] 4.5 确认规格不要求本 change 修改 `.asset`、`.prefab` 或 `.unity`。
- [x] 4.6 确认规格不要求新增第二 pipeline。
- [x] 4.7 确认规格不要求新增第二 runner。
- [x] 4.8 确认规格不要求新增第二 presenter。
- [x] 4.9 确认规格不要求绕过 output applier。

## 5. 实现
- [x] 5.1 添加 `CharacterFramePlan` 纯数据模型。
- [x] 5.2 添加 `BodyOccupancyDecision` 纯数据模型。
- [x] 5.3 添加 `IBodyArbiter` Interface。
- [x] 5.4 添加 `DefaultBodyArbiter` 默认实现。
- [x] 5.5 添加 Locomotion candidate output contract。
- [x] 5.6 添加 FullBody Action occupancy claim contract。
- [x] 5.7 将当前 integrated submitter 标记为迁移 adapter。
- [x] 5.8 将 `CharacterFramePipeline` 调整为通过 output composer 消费 plan。
- [x] 5.9 保持现有 Corin playable 行为不变。
- [x] 5.10 不新增 UpperBody runtime，直到 arbitration contract 完成。

## 6. 验证
- [x] 6.1 运行 BodyArbiter 相关 EditMode 测试。
- [x] 6.2 运行 CharacterFramePlan 相关 EditMode 测试。
- [x] 6.3 运行 CharacterFramePipeline 相关 EditMode 测试。
- [x] 6.4 运行 FullBody/Locomotion 仲裁相关 EditMode 测试。
- [x] 6.5 运行静态边界测试。
- [x] 6.6 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp.csproj --no-restore`。
- [x] 6.7 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp-Editor.csproj --no-restore`。
- [x] 6.8 运行 `openspec validate formalize-character-frame-arbitration-contract --strict --no-interactive`。
- [x] 6.9 运行 GitNexus `detect_changes()`：全量 dirty worktree 返回 HIGH，包含多个既有 Prefab/Scene/Presenter/Spec 变更；本 change 修改前逐 symbol impact 为 LOW。
- [x] 6.10 不运行 Unity batchmode。

## 7. 输出 Runtime Port 收口
- [x] 7.1 将 `ICharacterFrameOutputRuntimePort` 的 motion 输出节点改为接收 resolved `CharacterFrameMovementSubmission`。
- [x] 7.2 将 `ICharacterFrameOutputRuntimePort` 的 animation 输出节点改为接收 resolved `CharacterFrameAnimationSubmission`。
- [x] 7.3 将 `CharacterFrameOutputApplier` 调整为只把 `CharacterFrameOutput` 的 resolved 输出切片传给 runtime port。
- [x] 7.4 将 `FullBodyRuntimePortAdapter` 和 `FullBodyOutputRuntime` 调整为不再通过 `stateFrame.ExecuteBasicMovement` 或 `stateFrame.PresentLocomotionAnimation` 执行输出。
- [x] 7.5 增加静态边界测试：output runtime 消费 resolved output slices。
- [x] 7.6 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp.csproj --no-restore`。
- [x] 7.7 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp-Editor.csproj --no-restore`。
- [x] 7.8 运行 `CharacterFrameArbitrationTests` 和 CharacterFramePipeline 相关 EditMode 定向测试。

## 8. 废弃路径清理
- [x] 8.1 删除空壳 `IFullBodySubmissionRuntimePort` / `IFullBodyOutputRuntimePort` 文件。
- [x] 8.2 从 `Assembly-CSharp.csproj` 移除已删除 FullBody runtime ports include。
- [x] 8.3 删除 `CharacterFrameOutput(CharacterFrameSubmission)` legacy 构造器。
- [x] 8.4 删除 `CharacterFrameOutputComposer.Compose(CharacterFrameSubmission)` legacy overload。
- [x] 8.5 更新静态测试，保护 Character runtime port 作为唯一正式 frame runtime port。
- [x] 8.6 确认 `FullBodyActionTickAdapter` 暂不删除：Sandbox scene 仍引用该脚本 GUID，需在 Prefab/Scene 迁移 change 中清理。
