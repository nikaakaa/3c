## 1. Spec And Design
- [x] 1.1 读取 `character-behavior-graph-contracts` 当前 spec。
- [x] 1.2 读取 `refactor-character-behavior-authoring-source-boundary` 的 proposal/design/delta。
- [x] 1.3 确认本 change 不新增平行 capability。
- [x] 1.4 更新 `character-behavior-graph-contracts` delta，收敛 Graph Interface。
- [x] 1.5 运行 `openspec validate refactor-character-behavior-graph-source-contract --strict --no-interactive`。

## 2. Graph Authoring Contract
- [x] 2.1 检查 `CharacterBehaviorAuthoringAsset` 当前正式字段。
- [x] 2.2 确认正式 graph authoring 只包含 schema、stable id、nodes、ports、edges、editor position 和 source reference。
- [x] 2.3 确认 legacy timeline clip id 与 legacy Dodge branch 字段不被正式 compiler 消费。
- [x] 2.4 若发现正式 schema 仍暴露 timeline/track/clip payload，删除或迁到 legacy/diagnostic 口径。

## 3. Compiler Split
- [x] 3.1 检查 Behavior compiler 输出。
- [x] 3.2 确认 Behavior compiler 只输出 source topology/runtime definition。
- [x] 3.3 确认 Behavior compiler 不输出 `ActionTimelineDefinition`。
- [x] 3.4 确认 Behavior compiler 不通过 `Action.Dodge` 字符串决定 timeline 结构。
- [x] 3.5 确认 ActionDefinition 编译路径使用 `CharacterActionDefinitionSO.ToDefinition()` 或等价正式入口。

## 4. Editor Data Ownership
- [x] 4.1 检查 Character Behavior Editor 保存逻辑。
- [x] 4.2 确认移动节点只更新 editor position。
- [x] 4.3 确认修改 edge 只更新 source topology。
- [x] 4.4 确认打开 Timeline Editor 时选择或传递正式 `CharacterActionDefinitionSO`。
- [x] 4.5 确认 Graph Editor 不创建第二份 Dodge timeline。

## 5. Tests
- [x] 5.1 增加 Graph compiler 不输出 timeline payload 的 EditMode 测试。
- [x] 5.2 增加 Graph schema 不包含正式 Dodge selector/timeline 字段的 EditMode 或静态边界测试。
- [x] 5.3 增加 Graph Editor 保存不修改 `ActionTimelineTrackAuthoring` / `ActionTimelineClipAuthoring` 的测试。
- [x] 5.4 增加缺少正式 ActionDefinition 时报告配置错误的测试。
- [x] 5.5 增加正式 ActionDefinition 编译 Dodge selector/timeline 的行为测试。

## 6. Validation
- [x] 6.1 运行相关 EditMode 测试。
- [x] 6.2 运行 `openspec validate refactor-character-behavior-graph-source-contract --strict --no-interactive`。
- [x] 6.3 运行 `openspec validate --changes --strict --no-interactive`。
- [x] 6.4 运行旧语义静态扫描，确认 active specs 和新 change 不把 graph 写成 timeline/action 数据源。
