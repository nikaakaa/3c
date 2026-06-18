# Change: 收敛 Character Behavior Graph Source 合同

## Why
当前 `character-behavior-graph-contracts` 正式 spec 仍允许 `CharacterBehaviorGraphDefinition` 携带 branch / timeline authoring 信息，但已完成的 authoring source boundary change 已将 Dodge selector、Directional timeline、Backstep timeline 和 track/clip 数据收敛到正式 `CharacterActionDefinitionSO`。

如果不修正正式 spec，Graph 和 ActionDefinition 会同时像数据源，后续 Timeline Editor、compiler、validator 和测试会继续在两处寻找同一份 Dodge 数据。

## What Changes
- 将 `CharacterBehaviorGraphDefinition` 的正式 Interface 收敛为 source topology、node、port、edge、editor position 和 action source reference。
- 明确 Graph 不保存 Dodge selector、Directional timeline、Backstep timeline、track、clip、motion payload、animation key、window 或 cue。
- 明确 Action/timeline 数据只归 `CharacterActionDefinitionSO`、action catalog 或批准的等价 ActionDefinition。
- 明确 Behavior compiler 只编译 source graph；ActionDefinition compiler/validator 负责编译 committed action branch、selector 和 timeline。
- 增加测试要求：Graph 保存不改 timeline，Graph compiler 不输出 timeline payload，缺 ActionDefinition 报正式错误，不使用 legacy embedded branch fallback。

## Impact
- Affected specs: `character-behavior-graph-contracts`
- Related active changes:
  - `refactor-character-behavior-authoring-source-boundary`
  - `migrate-ref-timeline-editor-to-formal-action-config`
  - `formalize-character-behavior-submission-runtime-chain`
- Affected code:
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Behavior/Authoring/CharacterBehaviorAuthoringAsset.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Action/Config/CharacterActionDefinitionSO.cs`
  - `3cDemo/Client/3C_Client/Assets/Editor/Character/Graph/CharacterBehaviorEditorWindow.cs`
  - `3cDemo/Client/3C_Client/Assets/Editor/Character/Action/Timeline/CommittedActionTimelineEditorWindow.cs`
  - `3cDemo/Client/3C_Client/Assets/Tests/Editor/Character/Behavior/Editor/CharacterBehaviorEditorAdapterTests.cs`
