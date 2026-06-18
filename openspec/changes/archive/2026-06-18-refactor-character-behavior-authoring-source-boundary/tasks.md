# Tasks

## 0. 前置核对
- [x] 0.1 读取 `AGENTS.md`、`openspec/AGENTS.md`、`openspec/project.md`。
- [x] 0.2 运行 `openspec list` 和 `openspec list --specs`。
- [x] 0.3 读取 `refactor-character-behavior-authoring-source-boundary/proposal.md`、`design.md`、`tasks.md` 和 spec delta。
- [x] 0.4 读取相关 active changes：`formalize-character-behavior-submission-runtime-chain`、`migrate-ref-timeline-editor-to-formal-action-config`。
- [x] 0.5 读取当前 `CharacterBehaviorAuthoringAsset`、`CharacterBehaviorAuthoringCompiler`、`CharacterActionDefinitionSO`、`CharacterBehaviorEditorWindow`。
- [x] 0.6 对将要修改的函数、类、方法运行 GitNexus `impact`。
- [x] 0.7 记录 HIGH / CRITICAL impact 的风险、直接调用方和受影响流程。

## 1. 现状盘点
- [x] 1.1 列出 behavior authoring asset 中所有 source graph 字段。
- [x] 1.2 列出 behavior authoring asset 中所有 action / Dodge / timeline 字段。
- [x] 1.3 列出 compiler 当前输出：execution tree、runtime definition、Dodge branch。
- [x] 1.4 列出 Graph Editor 当前读写字段。
- [x] 1.5 列出 Timeline Editor 当前正式 action definition 入口。
- [x] 1.6 明确需要删除、迁移、保留诊断的 legacy 字段。

## 2. Behavior Authoring Source Graph
- [x] 2.1 定义 behavior source graph 的正式字段集合。
- [x] 2.2 保留 root node authoring。
- [x] 2.3 保留 ordered composite / parallel node authoring。
- [x] 2.4 保留 Locomotion leaf authoring。
- [x] 2.5 保留 CommittedAction leaf authoring。
- [x] 2.6 保留 editor position 和 edge authoring。
- [x] 2.7 移除或停止正式消费 behavior asset 内嵌 Dodge branch/timeline。
- [x] 2.8 缺 root、重复 root、缺 leaf、leaf 顺序错误时报告正式错误。

## 3. Action Definition 数据源
- [x] 3.1 确认 Dodge selector/timeline 的唯一正式来源是 `CharacterActionDefinitionSO`。
- [x] 3.2 确认 Directional timeline 来自正式 Dodge action definition。
- [x] 3.3 确认 Backstep timeline 来自正式 Dodge action definition。
- [x] 3.4 确认 track / clip / payload 不再从 behavior graph 读取。
- [x] 3.5 缺正式 action definition 时报告正式错误，不生成默认配置。
- [x] 3.6 缺 Dodge branch 或 timeline 时报告正式错误，不读取 legacy fallback。

## 4. Compiler 拆分
- [x] 4.1 Behavior compiler 只编译 source graph。
- [x] 4.2 Behavior compiler 输出 `CharacterBehaviorRuntimeDefinition`。
- [x] 4.3 Behavior compiler 输出或保留 execution tree 时不得包含 Dodge branch/timeline payload。
- [x] 4.4 Action definition compile 使用 `CharacterActionDefinitionSO.ToDefinition()`。
- [x] 4.5 Action definition validator 覆盖 Dodge selector/timeline 必填字段。
- [x] 4.6 组合层显式传入 behavior runtime definition 和 action catalog/definition。
- [x] 4.7 删除 sample-only compiled runtime definition 的正式编译路径。

## 5. Editor 边界
- [x] 5.1 Graph Editor 只保存 source graph topology 和 node position。
- [x] 5.2 Graph Editor 显示 Locomotion leaf 和 CommittedAction leaf。
- [x] 5.3 Graph Editor 不显示或编辑 Dodge timeline clip payload。
- [x] 5.4 Graph Editor 可通过正式 action definition reference 打开 Timeline Editor。
- [x] 5.5 Timeline Editor 继续以 `CharacterActionDefinitionSO` 为 ObjectField 类型。
- [x] 5.6 Editor 菜单、窗口标题、文档不使用通用 Skill Editor 命名。

## 6. 迁移与清理
- [x] 6.1 检测 legacy behavior asset 中的 Dodge branch/timeline 数据。
- [x] 6.2 为 legacy 数据提供一次性迁移或明确诊断。
- [x] 6.3 删除正式入口对 `Behavior/Samples` 的依赖。
- [x] 6.4 删除正式入口对 sample-only runtime definition 的依赖。
- [x] 6.5 更新 OpenSpec 或项目文档中的双数据源描述。

## 7. 自动测试
- [x] 7.1 增加 behavior compiler 只输出 source runtime definition 测试。
- [x] 7.2 增加 behavior compiler 不读取 embedded Dodge branch 测试。
- [x] 7.3 增加正式 `CharacterActionDefinitionSO` 编译 Dodge branch 测试。
- [x] 7.4 增加缺 action definition reference 报错测试。
- [x] 7.5 增加 Graph Editor 保存不修改 Dodge timeline 测试。
- [x] 7.6 增加 Timeline Editor 保存修改正式 action definition 测试。
- [x] 7.7 增加静态边界测试：runtime 不引用 Editor、GraphView、sample-only compiled definition。
- [x] 7.8 增加命名边界测试：FullBody 不作为 source、slot 或 graph node。

## 8. 验证
- [x] 8.1 运行 `openspec validate refactor-character-behavior-authoring-source-boundary --strict --no-interactive`。
- [x] 8.2 通过 Unity MCP 尽量运行相关 EditMode 测试。
- [x] 8.3 Unity MCP 不可用时记录未执行测试名和原因。
- [x] 8.4 运行 `detect_changes({scope:"all"})` 并记录影响范围。

## 9. Spec 口径收敛
- [x] 9.1 确认 `character-behavior-graph-contracts` 的 source topology 修正由 `refactor-character-behavior-graph-source-contract` 承接，避免两个 active change 重复修改同一 requirement。
- [x] 9.2 将 `character-behavior-editor-adapters` 中 behavior compiler 可编译 Action timeline 的旧口径改为 compiler 职责拆分。
- [x] 9.3 明确 Graph Editor 只打开或定位正式 action definition，不复制 Dodge timeline 数据。
