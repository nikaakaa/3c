## 0. Proposal Context

- [x] 0.1 读取 `proposal.md`，确认本变更先清理过时 specs，再实现代码。
- [x] 0.2 读取 `design.md`，确认目标不是统一层级状态机。
- [x] 0.3 读取 `design.md`，确认 FullBody 只是 body/channel claim 或动画层语义，不是状态根。
- [x] 0.4 读取 `openspec show refactor-locomotion-fullbody-ownership --json --deltas-only`。
- [x] 0.5 读取当前 active changes，标记仍依赖旧 runner 的内容。
- [x] 0.6 读取当前 active changes，标记仍引用旧 FullBody action 路径的内容。

## 1. Current Usage Inventory

- [x] 1.1 搜索生产代码中的 `FullBodyOwnerKind.Locomotion`。
- [x] 1.2 搜索生产代码中的 `FullBodyOwner.Locomotion`。
- [x] 1.3 搜索生产代码中的 `FullBodyStateView`。
- [x] 1.4 搜索生产代码中的 `FullBody/Locomotion`。
- [x] 1.5 搜索生产代码中的 `FullBody/Action`。
- [x] 1.6 搜索配置资产中的 `FullBody/Locomotion`。
- [x] 1.7 搜索配置资产中的 `FullBody/Action`。
- [x] 1.8 搜索测试中的旧路径断言。
- [x] 1.9 记录每个旧引用的迁移目标：删除、兼容转换、诊断保留或测试改写。

## 2. Spec Cleanup Alignment

- [x] 2.1 更新 implementation 前确认 `fullbody-hfsm-state-tree` delta 已退役 FullBody 主树口径。
- [x] 2.2 更新 implementation 前确认 `unified-character-state-machine` delta 已退役统一层级状态机口径。
- [x] 2.3 更新 implementation 前确认 `locomotion-state-graph-config` delta 已把 Locomotion 归为移动领域 module。
- [x] 2.4 更新 implementation 前确认 `dodge-action` delta 已把 Dodge 归为 Action domain claim。
- [x] 2.5 更新 implementation 前确认 `character-frame-pipeline` delta 已声明 pipeline 是唯一合成 module。

## 3. Impact Analysis

- [x] 3.1 对 `CharacterStateIds` 运行 GitNexus upstream impact。
- [x] 3.2 对 `FullBodyOwnerKind` 运行 GitNexus upstream impact。
- [x] 3.3 对 `FullBodyOwner` 运行 GitNexus upstream impact。
- [x] 3.4 对 `FullBodyStateView` 运行 GitNexus upstream impact。
- [x] 3.5 对 `CharacterStateMachineSnapshot` 运行 GitNexus upstream impact。
- [x] 3.6 对 `CharacterFrameSubmitterGraph.CreateDefault` 运行 GitNexus upstream impact；GitNexus 尚未索引该新文件，已用源码检查与编译补充验证。
- [x] 3.7 如任一 impact 为 HIGH 或 CRITICAL，先向用户报告并等待确认。

## 4. Domain Id Migration

- [x] 4.1 读取 `CharacterStateIds` 当前定义。
- [x] 4.2 新增或迁移 Locomotion idle ID 为 `Locomotion.Idle`。
- [x] 4.3 新增或迁移 Locomotion move-start ID 为 `Locomotion.MoveStart`。
- [x] 4.4 新增或迁移 Locomotion move-loop ID 为 `Locomotion.MoveLoop`。
- [x] 4.5 新增或迁移 Locomotion move-stop ID 为 `Locomotion.MoveStop`。
- [x] 4.6 新增或迁移 Locomotion turn-back ID 为 `Locomotion.TurnBack`。
- [x] 4.7 新增或迁移 dodge action ID 为 `Action.Dodge`。
- [x] 4.8 删除或降级旧 FullBody path 常量。
- [x] 4.9 更新状态 ID 单元测试断言。

## 5. Locomotion Ownership Cleanup

- [x] 5.1 删除正式 `FullBodyOwnerKind.Locomotion` 分支。
- [x] 5.2 删除正式 `FullBodyOwner.Locomotion` 构造入口。
- [x] 5.3 删除 `FullBodyStateView` 中非 action 默认映射到 Locomotion owner 的逻辑。
- [x] 5.4 新增 Locomotion domain snapshot 或复用现有纯数据 snapshot。
- [x] 5.5 确认 Locomotion submitter 只提交移动 facts 和移动候选输出。
- [x] 5.6 确认 Locomotion submitter 不写 FullBody owner。
- [x] 5.7 更新相关测试 fixture。

## 6. Action Claim Cleanup

- [x] 6.1 确认 Action runtime 输出 body/channel claim。
- [x] 6.2 确认 Dodge 使用 `Action.Dodge` 或等价 action ID。
- [x] 6.3 确认 Action submitter 通过 pipeline 提交候选输出。
- [x] 6.4 确认 Action submitter 不依赖 `FullBody/Action` 前缀判断。
- [x] 6.5 更新 Dodge tests 覆盖 claim、打断和 frame plan 输出。

## 7. Pipeline Authority

- [x] 7.1 读取 `CharacterFrameSubmitterGraph.CreateDefault`。
- [x] 7.2 确认默认 graph 中 Locomotion submitter 和 FullBody Action submitter 是 sibling。
- [x] 7.3 确认没有 submitter 绕过 `CharacterFramePipeline` 直接提交最终帧输出。
- [x] 7.4 如需要新增 submitter interface，先检查是否已有两个 adapter；没有则不新增 seam。
- [x] 7.5 更新 pipeline tests 覆盖 Locomotion 与 Action 同帧提交时的仲裁结果。

## 8. Compatibility And Diagnostics

- [x] 8.1 决定 `FullBodyStateView` 是删除还是保留为只读诊断 view。
- [x] 8.2 如保留，确保它从领域 snapshot 和 frame plan 派生。
- [x] 8.3 如保留，确保它不反向决定 Action 或 Locomotion 仲裁。
- [x] 8.4 如需要迁移旧配置，新增只读转换路径并配套测试。
- [x] 8.5 确认没有新增 fallback 配置。

## 9. Automated Tests

- [x] 9.1 运行角色 runtime dotnet build。
- [x] 9.2 运行 editor tests dotnet build。
- [x] 9.3 运行 `UnifiedCharacterStateMachineTests`。
- [x] 9.4 运行 `CharacterFramePipelineTests`。
- [x] 9.5 运行 `FullBodyActionFrameworkTests`。
- [x] 9.6 运行 `LocomotionStateGraphConfigTests`。
- [x] 9.7 增加或更新静态测试确认正式配置不再包含 `FullBody/Locomotion`。
- [x] 9.8 增加或更新静态测试确认正式配置不再包含 `FullBody/Action`。
- [x] 9.9 增加或更新测试确认 `Action.Dodge` 不需要成为统一树叶子即可提交 claim。
- [x] 9.10 增加或更新测试确认 `Locomotion.MoveLoop` 由 Locomotion module 管理。

## 10. Documentation And OpenSpec

- [x] 10.1 更新相关 docs 中“FullBody 主树”表述。
- [x] 10.2 更新诊断文本中的旧 FullBody path。
- [x] 10.3 更新 OpenSpec project 约束中的状态树旧口径。
- [x] 10.4 运行 `openspec validate refactor-locomotion-fullbody-ownership --strict --no-interactive`。
- [x] 10.5 确认所有任务完成后再把本 checklist 全部改为 `- [x]`。

## 11. Final Review

- [x] 11.1 运行 `rg "FullBodyOwnerKind.Locomotion|FullBodyOwner.Locomotion|FullBody/Locomotion|FullBody/Action" 3cDemo/Client/3C_Client/Assets/Scripts 3cDemo/Client/3C_Client/Assets/Tests`。
- [x] 11.2 运行 GitNexus `detect_changes(scope: "all")`。
- [x] 11.3 向用户报告仍需确认的重构意图和未覆盖范围。

## 12. Action Input Config Closure

- [x] 12.1 确认 Shift 是 Action/Dodge request 输入，不是 Locomotion Run 输入。
- [x] 12.2 新增 `Player_Dodge` InputActionReference，使 Dodge 输入进入正式根配置。
- [x] 12.3 在 `CharacterConfigSO` 暴露 `DodgeInputAction`，与 Move/Run/Look 输入引用并列。
- [x] 12.4 让 `UnityInputSystemRequestBufferAdapter` 从 `CharacterConfigSO.DodgeInputAction` 接收正式 action asset、action map 和 action name。
- [x] 12.5 让 `CharacterFrameRuntimeController` 在应用根配置时同步配置 request buffer adapter。
- [x] 12.6 让 Locomotion 和 request input adapter 在正式根配置下发后启用对应 InputAction。
- [x] 12.7 清理 Corin prefabs 上的本地 input action asset、map 和 action 名称配置。
- [x] 12.8 增加根配置测试，确认 request buffer adapter 不依赖 prefab-local Dodge 字符串作为正式配置来源。
- [x] 12.9 增加 authoring 测试，确认 Shift 只绑定到 Dodge request，Run 不绑定 Shift。
- [x] 12.10 增加 Corin prefab 静态测试，确认 prefab 不序列化本地 input action 配置。
- [x] 12.11 增加 Corin prefab 集成测试，覆盖 live Shift 输入写入同一个 Dodge request buffer。
- [x] 12.12 运行 `dotnet build 3cDemo\Client\3C_Client\Assembly-CSharp.csproj`。
- [x] 12.13 运行 `dotnet build 3cDemo\Client\3C_Client\Assembly-CSharp-Editor.csproj`。
- [x] 12.14 运行 `openspec validate refactor-locomotion-fullbody-ownership --strict --no-interactive`。

## 13. Shift Dodge Runtime Chain Closure

- [x] 13.1 排查 Shift/Dodge 全链路，确认 `CharacterInput.inputactions` 中 Shift 绑定到 `Dodge` 而不是 `Run`。
- [x] 13.2 排查 prefab 绑定，确认 request adapter 和 FullBody runtime 读取同一个 `InputRequestBufferComponent`。
- [x] 13.3 排查 `CorinStateMachine.asset`，确认正式配置仍残留 `FullBody -> Locomotion/Action` 旧层级。
- [x] 13.4 排查 Dodge 入口 transition，确认资产使用 `fromStateId: Locomotion.*`。
- [x] 13.5 排查 `StateGraphTransition.MatchesSource`，确认 runtime 只识别 `/*`，没有识别 validator 已允许的 `.*`。
- [x] 13.6 让 `StateGraphTransition.MatchesSource` 支持点号领域 wildcard，使 `Locomotion.*` 能匹配 `Locomotion.Idle`、`Locomotion.MoveLoop` 等领域状态。
- [x] 13.7 扁平化 `CorinStateMachine.asset`，删除 `FullBody` 根节点并清空 `Locomotion`、`Action` domain parent。
- [x] 13.8 清理 `CameraTest.unity` 和 `CinemachineTest.unity` 中残留的本地 Locomotion input action asset、map 和 action 名称配置。
- [x] 13.9 增加 `StateGraphTransitionWildcardTests.DotWildcardMatchesDomainStateIds` 覆盖 `Locomotion.* -> Action.Dodge` 的 runtime 匹配语义。
- [x] 13.10 增加 `StateGraphTransitionWildcardTests.CorinStateMachineAssetUsesFlatDomainNodes` 覆盖 Corin 正式状态机资产不再序列化 FullBody 根或 domain parent。
- [x] 13.11 运行静态检查，确认 Corin 正式状态机资产不再包含 `stateId: FullBody`、`parentStateId: FullBody`、`parentStateId: Locomotion` 或 `parentStateId: Action`。
- [x] 13.12 运行静态检查，确认 Corin prefabs 与相关测试场景不再序列化本地 input action asset、map 和 action 名称。
- [x] 13.13 运行 `dotnet build 3cDemo\Client\3C_Client\Assembly-CSharp.csproj`。
- [x] 13.14 运行 `dotnet build 3cDemo\Client\3C_Client\Assembly-CSharp-Editor.csproj`。
- [x] 13.15 运行 `openspec validate refactor-locomotion-fullbody-ownership --strict --no-interactive`。
