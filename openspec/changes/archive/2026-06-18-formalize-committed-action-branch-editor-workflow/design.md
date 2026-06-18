## Context
`Ref/wly970123` 的 TreeDesigner 编辑体验有几个明确特征：

- 资产驱动入口：双击或选择 tree asset 后打开对应 tree window。
- 固定 root：`OneRootTree` 创建时生成 `RootNode`，root 不能删除、复制、分组或进入 stack。
- 单一节点树窗口：`BaseTreeWindow` 承载左侧 inspector 和右侧 GraphView。
- SearchWindow 分组创建节点：通过可接受 node path 控制可创建节点集合。
- 节点内/侧边属性编辑：节点不只是卡片，还能展开字段和 property port。
- Timeline 独立窗口：Timeline 有自己的 track、clip、locator 和 inspector，不塞进 tree window。

本项目已经迁移了 GraphView shell 和 Timeline 独立窗口，但还需要把固定 root、属性面板和模板化初始化补成正式规格。

## Goals
- Committed Branch mode 像 Ref TreeDesigner 一样有固定 root 和明确入口。
- 设计者不需要手动编辑 serialized array 就能配置 condition 和 timeline node 摘要字段。
- Timeline 保持独立窗口，但能从选中的 TimelineNode 精确打开和写回。
- 所有编辑都通过本项目 `CharacterActionDefinitionSO` 的 serialized adapter 写回。
- 自动测试覆盖 root/template/property/writeback 边界。

## Non-Goals
- 不复制 Ref runtime tree、runner、TimelinePlayer 或 PlayableGraph 执行路径。
- 不新增第二套 action branch authoring asset。
- 不把 Behavior Source graph 和 Committed Action branch graph 合并成同一 runtime graph。
- 不在本变更内实现视觉动画预览、scene binding 或 gameplay runner。

## Decisions
- Decision: 使用现有 `CharacterBehaviorEditorWindow` 的 Committed Branch mode 作为唯一节点树入口。
  - Rationale: 避免 Branch 专用窗口和 Character Behavior Editor 分裂。
- Decision: root 是 action branch authoring 的正式节点身份，而不是 GraphView 临时装饰。
  - Rationale: runtime compiler 已依赖 `rootNodeId`，编辑器必须保护这个数据合同。
- Decision: 空 branch 初始化必须是显式命令或显式 repair，而不是隐藏 fallback。
  - Rationale: 缺失 root 是配置错误；工具可以帮助创建正式配置，但不能在 compile/runtime 隐式补齐。
- Decision: Timeline 使用独立 `CommittedActionTimelineEditorWindow`。
  - Rationale: Timeline 和节点树交互密度高，分窗更符合当前编辑体验要求，也避免在 Branch graph 内形成第二 Timeline 面板权威。
- Decision: Ref UI 只迁移交互形态和 Editor-only 资源。
  - Rationale: 本项目 runtime 只能消费 compiler 输出，不能消费 Taco tree 或 TimelinePlayer。

## Risks / Trade-offs
- Risk: root 保护如果只在 UI 层实现，serialized adapter 仍可能删除 root。
  - Mitigation: adapter 层和 GraphView 层都要拒绝 root 删除，并补 EditMode 测试。
- Risk: 初始化模板如果自动执行，会被误解为 fallback。
  - Mitigation: 只在 editor 明确按钮/命令里创建正式配置；validator 对缺失 root 继续报错。
- Risk: 节点属性面板直接绑定 serialized property 时容易因数组删除导致路径失效。
  - Mitigation: selection 使用 stable node id，每次结构变化后重新解析 property path。
- Risk: `committed-action-timeline-editor` 旧规格仍说 panel 嵌入。
  - Mitigation: 本变更重命名并修改该 requirement，统一为独立 Timeline window + selected TimelineNode adapter。

## Migration Plan
1. 先补 editor adapter 的 root 判定、模板初始化和 root 删除保护。
2. 再补 GraphView snapshot 的 root 标记和 UI capabilities。
3. 再补节点属性面板，按 stable node id 解析 serialized property。
4. 最后收紧 Timeline 独立窗口定位和静态边界测试。

## Manual Verification Guidance
实现完成后，设计者可在 Unity 内打开 `Tools/3C/Character Behavior Editor`，切到 `Committed Branch`，选择 `CorinDodgeActionDefinition.asset`。预期看到固定 root/selector、Directional/Backstep condition 和两个 timeline leaf；root 不可删除；选中 condition 可直接编辑 condition payload；选中 timeline 可打开独立 Timeline Editor。
