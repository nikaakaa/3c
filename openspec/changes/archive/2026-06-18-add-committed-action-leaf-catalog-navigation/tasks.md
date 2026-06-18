## 1. 规格与边界确认

- [x] 1.1 读取本 change 的 `proposal.md`、`design.md` 和 spec deltas，确认实现只覆盖 catalog 导航。
- [x] 1.2 对照 current specs 确认 Behavior Source 主图不保存 branch 数据。
- [x] 1.3 对照 active `refactor-ref-editor-source-port` 确认本 change 不重复实现 Ref UI shell。
- [x] 1.4 在修改核心 symbol 前运行 GitNexus upstream impact，并记录风险。

## 2. Catalog 导航模型

- [x] 2.1 增加 Editor-only action catalog navigation snapshot model。
- [x] 2.2 从 `CharacterConfigSO.ActionCatalog` 建立 entry 列表。
- [x] 2.3 为每个 entry 保留 stable action id、display label、definition reference 和 diagnostic。
- [x] 2.4 检测缺失 catalog。
- [x] 2.5 检测空 catalog。
- [x] 2.6 检测缺失 action definition。
- [x] 2.7 检测重复 action id。
- [x] 2.8 确认 snapshot model 不引用 GraphView 或 runtime runner。

## 3. Character Behavior Editor 接入

- [x] 3.1 在 Character Behavior Editor 中增加正式角色配置或 catalog 导航源。
- [x] 3.2 移除 `CommittedActionLeaf` 打开时对 Dodge asset 的硬编码 fallback。
- [x] 3.3 当 catalog 只有一个有效 action 时直接进入 Committed Branch mode。
- [x] 3.4 当 catalog 有多个有效 action 时显示同窗口 action 选择入口。
- [x] 3.5 选择 action 后绑定对应 `CharacterActionDefinitionSO`。
- [x] 3.6 选择 action 后只切换到现有 Committed Branch mode，不打开第二窗口。
- [x] 3.7 缺少 catalog 或 action definition 时显示诊断并停止。
- [x] 3.8 确认 Behavior Source 主图不创建 Branch Root、Selector、Condition 或 TimelineNode。

## 4. 保存与数据边界

- [x] 4.1 确认 action 选择不写入 Behavior Source authoring asset。
- [x] 4.2 确认保存 Behavior Source graph 不修改 ActionDefinition。
- [x] 4.3 确认保存 Branch graph 只修改当前选中的 ActionDefinition。
- [x] 4.4 确认新增 action 只需注册进 Action Catalog 即可在导航入口出现。

## 5. 自动化测试

- [x] 5.1 增加 catalog snapshot EditMode 测试：有效 catalog 生成 action entry。
- [x] 5.2 增加 catalog snapshot EditMode 测试：重复 action id 报诊断。
- [x] 5.3 增加 catalog snapshot EditMode 测试：缺失 definition 报诊断。
- [x] 5.4 增加 editor adapter 测试：单 action catalog 直接打开该 action。
- [x] 5.5 增加 editor adapter 测试：多 action catalog 选择指定 action。
- [x] 5.6 增加 editor adapter 测试：缺失 catalog 不 fallback 到 Dodge。
- [x] 5.7 增加边界测试：Behavior Source 保存不写入 action branch 数据。
- [x] 5.8 增加静态边界测试：runtime 不引用 editor catalog navigation 类型。

## 6. 验证

- [x] 6.1 运行相关 EditMode 测试。
- [x] 6.2 运行 `openspec validate add-committed-action-leaf-catalog-navigation --strict --no-interactive`。
- [x] 6.3 运行 `openspec validate --all --strict --no-interactive`。
- [x] 6.4 确认任务全部真实完成后将 checklist 改为 `[x]`。
