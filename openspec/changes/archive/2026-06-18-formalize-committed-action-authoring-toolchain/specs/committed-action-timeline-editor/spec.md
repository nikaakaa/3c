## ADDED Requirements
### Requirement: Timeline Editor 嵌入 Branch TimelineNode
Committed Action Timeline Editor MUST 支持作为 Committed Action Branch Editor 的 TimelineNode panel 使用。该 panel MUST 通过 selected TimelineNode 的 serialized adapter 读写 timeline authoring track、clip 和 payload，并 MUST 使用 action definition compiler / evaluator 进行 preview。独立 Timeline Editor 窗口 MAY 保留为快捷入口，但 MUST NOT 继续以 Dodge 专用 Directional / Backstep 字段作为正式数据权威。

#### Scenario: Timeline Panel 编辑选中节点
- **GIVEN** Branch Editor 选中了 TimelineNode A
- **WHEN** 设计者在 timeline panel 中添加、删除、移动或缩放 clip
- **THEN** 修改 MUST 写入 TimelineNode A 的 timeline authoring 数据
- **AND** TimelineNode B 的 timeline authoring 数据 MUST 保持不变
- **AND** preview outcome MUST 使用同一 action definition 编译后的 runtime branch

#### Scenario: 独立窗口只作为快捷入口
- **WHEN** 设计者打开 `Tools/3C/Committed Action Timeline Editor`
- **THEN** 工具 MAY 打开 Branch Editor 并定位默认 action definition 与默认 TimelineNode
- **AND** 保存 MUST 写回正式 branch authoring 内的 TimelineNode
- **AND** MUST NOT 创建独立于 branch authoring 的 runtime timeline definition
