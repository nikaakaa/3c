## 1. 准备
- [x] 1.1 读取本变更 `proposal.md`。
- [x] 1.2 读取本变更 `design.md`。
- [x] 1.3 读取本变更 `tasks.md`。
- [x] 1.4 读取 `character-frame-pipeline` 当前 spec。
- [x] 1.5 读取 `character-runtime-ports` 当前 spec。
- [x] 1.6 读取 `fullbody-action-framework` 当前 spec。
- [x] 1.7 读取 `wasd-locomotion-pipeline` 当前 spec。
- [x] 1.8 读取 `simulation-tick-system` 当前 spec。
- [x] 1.9 读取 `character-config-root` 当前 spec。
- [x] 1.10 搜索当前 `PlayerFullBodyActionController.Update`、`FullBodyActionTickAdapter`、`CharacterFrameRuntimeHost`、`FullBodyIntegratedFrameAdapter` 和 submitter 调用点。
- [x] 1.11 对计划修改的 runtime symbol 执行 GitNexus impact analysis，并记录 blast radius。

## 2. 测试先行
- [x] 2.1 增加静态测试：Corin 正式 runtime 入口必须是 `CharacterFrameRuntimeController` 或等价角色级入口。
- [x] 2.2 增加静态测试：`PlayerFullBodyActionController` 不得作为正式路径创建或拥有 `CharacterFrameRuntimeHost`。
- [x] 2.3 增加静态测试：正式生产 submitter graph 不得只绑定 `FullBodyIntegratedFrameAdapter`。
- [x] 2.4 增加静态测试：`FullBodyActionTickAdapter` 不得作为正式 simulation tick registration owner。
- [x] 2.5 增加静态测试：`PlayerLocomotionController` 的 direct tick 不得成为正式 gameplay driver。
- [x] 2.6 增加资产验证测试：Corin 正式 prefab/scene 绑定角色级 runtime controller。
- [x] 2.7 增加资产验证测试：Corin 正式 prefab/scene 不启用 FullBody 或 Locomotion 的正式 autoUpdate 主线。
- [x] 2.8 增加 EditMode 行为测试：角色级 runtime controller 可推进 Locomotion-only frame。
- [x] 2.9 增加 EditMode 行为测试：角色级 runtime controller 可推进 Dodge accepted frame。
- [x] 2.10 增加 EditMode 行为测试：Dodge active 时 CharacterFramePlan 压制 Locomotion motion/animation。
- [x] 2.11 增加 EditMode 行为测试：Dodge exit 后回到 Locomotion 输出。

## 3. Character Runtime Controller
- [x] 3.1 新增 `CharacterFrameRuntimeController` 或等价角色级 MonoBehaviour。
- [x] 3.2 让该 controller 读取正式 `CharacterConfigSO` 根配置。
- [x] 3.3 让该 controller 装配输入缓冲、Locomotion adapter、FullBody Action adapter、motion executor、animation presenter 和 diagnostics。
- [x] 3.4 让该 controller 创建并持有唯一 `CharacterFrameRuntimeHost`。
- [x] 3.5 让该 controller 提供 frame `Update` 兼容驱动。
- [x] 3.6 让该 controller 提供 runtime tick phase 驱动入口。
- [x] 3.7 增加单驱动校验，防止 frame update 和 simulation tick 同时推进 gameplay。

## 4. Submitter Graph
- [x] 4.1 新增角色级 submitter graph 或等价组合模块。
- [x] 4.2 让 graph 支持多个 sibling request submitter。
- [x] 4.3 让 graph 支持多个 sibling output submitter。
- [x] 4.4 让 graph 输出统一的 request submission 结果。
- [x] 4.5 让 graph 输出可供 `BodyArbiter` 生成 `CharacterFramePlan` 的候选输出。
- [x] 4.6 确认 graph 本身不执行 motion、animation、input consume、runtime facts 或 snapshot/events。

## 5. Locomotion Submitter
- [x] 5.1 新增 Locomotion submitter 或等价 adapter。
- [x] 5.2 让 Locomotion submitter 通过 `ILocomotionFrameRuntimePort` 读取移动意图和 Locomotion facts。
- [x] 5.3 让 Locomotion submitter 提交基础移动 motion candidate。
- [x] 5.4 让 Locomotion submitter 提交基础移动 animation candidate。
- [x] 5.5 确认 Locomotion submitter 不读取 `PlayerFullBodyActionController` 或 FullBody 私有字段。
- [x] 5.6 确认 Locomotion submitter 不创建 runner、不执行 motion、不播放 animation。

## 6. FullBody Action Submitter
- [x] 6.1 新增 FullBody Action submitter 或等价 adapter。
- [x] 6.2 让 FullBody Action submitter 提交 Dodge request 或当前已实现动作请求。
- [x] 6.3 让 FullBody Action submitter 提交 full-body occupancy claim。
- [x] 6.4 让 FullBody Action submitter 提交 action motion candidate。
- [x] 6.5 让 FullBody Action submitter 提交 action animation candidate。
- [x] 6.6 确认 FullBody Action submitter 不直接调用 Locomotion output runtime 执行压制。
- [x] 6.7 确认 FullBody Action submitter 不执行 motion、不播放 animation、不消费输入缓冲。

## 7. Legacy Controller 降级
- [x] 7.1 将 `PlayerFullBodyActionController` 的正式 frame `Update` 主线降级为兼容入口或禁用正式驱动。
- [x] 7.2 移除 `PlayerFullBodyActionController` 直接创建正式 `CharacterFrameRuntimeHost` 的职责。
- [x] 7.3 保留 FullBody 配置解析、状态机 runner owner、diagnostic view 或兼容 API 中仍必要的职责。
- [x] 7.4 将 `FullBodyIntegratedFrameAdapter` 标记为 legacy compatibility path。
- [x] 7.5 确认 Corin 正式生产路径不依赖 `FullBodyIntegratedFrameAdapter`。

## 8. Tick Adapter 和 Prefab/Scene 绑定
- [x] 8.1 新增 `CharacterFrameRuntimeTickAdapter` 或等价角色级 simulation tick adapter。
- [x] 8.2 让角色级 tick adapter 注册 Character frame phases。
- [x] 8.3 让角色级 tick adapter 禁用或接管 frame update 驱动，避免双驱动。
- [x] 8.4 将 `FullBodyActionTickAdapter` 从正式生产路径降级、删除或转发。
- [x] 8.5 迁移 Corin 正式 prefab 绑定到 `CharacterFrameRuntimeController`。
- [x] 8.6 迁移 Corin Humanoid 正式 prefab 绑定到 `CharacterFrameRuntimeController`。
- [x] 8.7 迁移纳入范围的 Corin playable scene override，不恢复 FullBody 或 Locomotion 旧入口。
- [x] 8.8 确认不修改第三方 Ref、Art 示例 prefab 或非 Corin 历史资产。

## 9. 回归和验证
- [x] 9.1 运行 Character frame runtime controller 定向 EditMode 测试。
- [x] 9.2 运行 Corin prefab/scene binding 定向 EditMode 测试。
- [x] 9.3 运行 FullBody rollback replay 相关定向 EditMode 测试。
- [x] 9.4 运行 unified Animancer presenter 相关定向 EditMode 测试。
- [x] 9.5 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp.csproj --no-restore`。
- [x] 9.6 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp-Editor.csproj --no-restore`。
- [x] 9.7 运行 `openspec validate promote-character-frame-runtime-controller --strict --no-interactive`。
- [x] 9.8 运行 GitNexus `detect_changes()`。
- [x] 9.9 不运行 Unity batchmode。
