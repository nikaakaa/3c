## 1. 前置对齐
- [x] 1.1 确认 `refactor-action-timeline-time-authority` 已归档或实现代码按其 seconds authoring / runtime tick delta 执行。
- [x] 1.2 复核 `committed-action-node-selection`、`committed-action-timeline-editor`、`character-action-catalog`、`dodge-action` 和 `character-behavior-editor-adapters` 的现行要求。
- [x] 1.3 对将要修改的核心符号运行 GitNexus impact，并记录高风险项。

## 2. Runtime Authoring Model
- [x] 2.1 新增通用 Committed Action branch authoring 数据结构，包含 schema version、branch id、root node id、节点列表和 editor layout。
- [x] 2.2 新增通用 node authoring，支持 Selector、Condition、Timeline 三类节点和稳定 node id。
- [x] 2.3 新增 condition authoring，第一版只映射现有 `RequestVariantEquals` 和 `HasMoveIntent`。
- [x] 2.4 新增 timeline node authoring，复用正式 timeline authoring track / clip / payload 数据。
- [x] 2.5 新增 branch authoring compiler，输出现有 `CommittedActionBranchDefinition`，并保持稳定 child 顺序。
- [x] 2.6 新增 branch authoring validator，覆盖缺 root、重复 node id、悬空 child、循环、非法 condition、空 timeline 和缺 body claim。

## 3. Action Definition 迁移
- [x] 3.1 将 `CharacterActionDefinitionSO` 的正式 branch 来源切到通用 branch authoring。
- [x] 3.2 迁移 Corin Dodge action asset 到通用 branch authoring。
- [x] 3.3 删除或停止使用 `DodgeCommittedActionBranchAuthoring` 的正式 runtime 解析路径。
- [x] 3.4 删除 Dodge branch 缺失时从旧 Directional / Backstep variant、single timeline 或代码默认值补齐的路径。
- [x] 3.5 保留旧字段仅作为一次性迁移输入或诊断输入；迁移完成后不得参与 `ToDefinition()`。

## 4. Branch Editor Adapter
- [x] 4.1 新增 Editor-only serialized adapter，以 `CharacterActionDefinitionSO` 读写通用 branch authoring。
- [x] 4.2 adapter 支持节点新增、删除、重命名、重排 child、设置 root 和布局保存。
- [x] 4.3 adapter 支持选中 TimelineNode 后暴露对应 timeline serialized property。
- [x] 4.4 adapter 保存后必须能立即通过 action definition compiler 生成同一份 runtime branch。
- [x] 4.5 adapter 不引用 runtime scene object、motion executor、animation presenter 或 blackboard writer。

## 5. Branch Editor UI
- [x] 5.1 新增或正式化 `Tools/3C/Committed Action Branch Editor`。
- [x] 5.2 窗口 ObjectField 限定为 `CharacterActionDefinitionSO`，默认打开正式 Corin Dodge action definition。
- [x] 5.3 图视图展示 selector、condition、timeline 节点和稳定 child 顺序。
- [x] 5.4 inspector 支持编辑 branch id、root node、body claim、condition payload 和 node id。
- [x] 5.5 选中 TimelineNode 时显示嵌入 timeline panel，并复用现有 track / clip / payload 操作。
- [x] 5.6 保留的 Timeline Editor 菜单只能作为快捷入口定位到 branch editor 的选中 TimelineNode。
- [x] 5.7 Character Behavior Editor 文案和入口只描述 source topology，不再让用户误以为它编辑 action branch。

## 6. 自动测试
- [x] 6.1 添加 branch authoring compiler EditMode 测试，覆盖 selector / condition / timeline 输出和 child 顺序。
- [x] 6.2 添加 validator EditMode 测试，覆盖缺 root、重复 id、循环、悬空 child、非法 condition 和空 timeline。
- [x] 6.3 添加 Dodge 迁移回归测试，断言 Directional / Backstep 选择、motion、animation key、window、cue 和 Run latch 语义不回退。
- [x] 6.4 添加 editor adapter EditMode 测试，覆盖节点 CRUD、timeline node writeback、保存后重新加载和 `ToDefinition()` 编译。
- [x] 6.5 添加 timeline panel 适配测试，覆盖选中不同 TimelineNode 时 track / clip 操作写回正确 node。
- [x] 6.6 添加静态边界测试，确认 runtime 不引用 UnityEditor、GraphView、TimelinePlayer、PlayableGraph、Taco runner 或 scene object binding。
- [x] 6.7 添加命名边界测试，确认菜单、窗口和文档不把本阶段称为通用 Skill Editor。

## 7. 工具验证
- [x] 7.1 运行 `openspec validate formalize-committed-action-authoring-toolchain --strict --no-interactive`。
- [x] 7.2 通过 Unity MCP 运行新增的定向 EditMode 测试。
- [x] 7.3 运行相关现有 Action Timeline、Action Catalog、Dodge 和 Editor Adapter EditMode 测试。
- [x] 7.4 运行 `detect_changes()`；当前工作区已有大量非本 change 改动，结果为 CRITICAL，需按交付说明中的 caveat 解读。
