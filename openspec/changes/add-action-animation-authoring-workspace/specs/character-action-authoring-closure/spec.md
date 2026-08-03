# character-action-authoring-closure Specification

## ADDED Requirements

### Requirement: Action作者闭环必须能导航到统一动画工作面

ActionProfile Inspector、Gameplay Action Context call site、有限Action Timeline和Pose Graph AnimationSlot MAY在精确Character Definition context下打开同一个`Action Animation Workspace`。打开请求 MUST携带稳定Action、Timeline、producer和Slot identity；Workspace MUST不按显示名或当前selection推断关系。

#### Scenario: 从ActionProfile打开Workspace

- **WHEN** 作者在Attack ActionProfile选择Open Action Animation Workspace
- **THEN** 请求 MUST携带精确Definition与Action identity
- **AND** Workspace MUST解析并显示唯一Gameplay、Timeline和Presentation owner

#### Scenario: 从AnimationSlot打开Workspace

- **WHEN** 作者从FullBodyAction Slot选择一个可达Attack producer
- **THEN** 请求 MUST携带Slot、AnimationChannel和producer identity
- **AND** Workspace MUST定位对应Action Timeline而不复制Slot配置
