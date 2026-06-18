# Change: 增加 Behavior Source 到 Committed Branch 的节点导航

## Why
当前 `Character Behavior Editor` 已经同时承载 Behavior Source mode 和 Committed Branch mode，但两张图之间仍靠 toolbar 手动切换。设计者在 Behavior Source 图中看到 `Committed Action Leaf` 后，不能直接从该 source 节点进入对应的 Committed Action Branch 图，容易误解为两张图没有编辑关系，或者误以为需要新建第二个窗口。

## What Changes
- 在同一个 `CharacterBehaviorEditorWindow` 内增加 Behavior Source 图到 Committed Branch 图的导航合同。
- 双击或批准等价 open gesture 作用在 `CommittedActionLeaf` 时，切换到 Committed Branch mode 并定位正式 `CharacterActionDefinitionSO` 的 branch。
- 第一版不在 Behavior Source authoring asset 中新增 ActionDefinition 引用字段；导航目标使用当前已选 action definition 或正式默认 Dodge action definition。
- 单击仍只负责选中节点，不触发 mode 切换。
- Behavior Source 图和 Branch 图继续使用同一个 Ref-port GraphView shell，但通过不同 adapter 读写不同正式数据源。
- 保存 Behavior Source 图不得写入 action branch/timeline；保存 Branch 图不得写入 behavior source topology。

## Impact
- Affected specs:
  - `character-behavior-editor-adapters`
  - `character-behavior-authoring-source-boundary`
- Affected code:
  - `Assets/Editor/Character/Graph/CharacterBehaviorEditorWindow.cs`
  - `Assets/Editor/Character/Graph/CharacterBehaviorRefPortedGraphView.cs`
  - `Assets/Tests/Editor/Character/Behavior/Editor/CharacterBehaviorEditorAdapterTests.cs`
  - `Assets/Tests/Editor/Character/Behavior/CharacterBehaviorAuthoringSourceBoundaryTests.cs`

## Dependencies / Conflicts
- 基于 current specs 中已经归档合入的口径：`Character Behavior Editor` 是唯一节点树窗口入口，Branch 使用 Committed Branch mode，Timeline 仍是独立窗口。
- 与 active change `add-committed-action-timeline-scene-preview-binding` 不冲突；该 active change 只扩展独立 Timeline Window 的 scene preview，不改变 Behavior Source 到 Branch 的导航。

## Out of Scope
- 不新增 `Tools/3C/Committed Action Branch Editor` 或第二个 Branch 专用窗口。
- 不把 Branch 图嵌进 Behavior Source 节点内部。
- 不在 Behavior Source asset 中保存 selector、condition、TimelineNode、track、clip 或 payload。
- 不新增隐藏 fallback branch、Resources 查找、sample asset 查找或代码默认 branch。
- 不实现多 Action catalog 选择器；多 Action 到具体 ActionDefinition 的显式选择另开 proposal。
