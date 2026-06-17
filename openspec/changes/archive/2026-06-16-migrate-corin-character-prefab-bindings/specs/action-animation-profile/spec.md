## ADDED Requirements
### Requirement: Corin Prefab Action 动画绑定迁移
系统 MUST 让 Corin prefab 的 Action 动画表现绑定通过正式 animation presenter 路径解析。Prefab 迁移 MUST NOT 新增第二个 Action animation presenter 或绕过 Character output apply 阶段。

#### Scenario: Action presenter 引用保持唯一
- **WHEN** 自动校验 Corin prefab 上的 `PlayerFullBodyActionController`
- **THEN** `animationPresenterBehaviour` MUST 指向正式 action presenter 或已审批的统一 presenter
- **AND** prefab MUST NOT 同时启用两个正式 action animation presenter
- **AND** Action animation 播放 MUST 仍由 Character frame output 阶段提交

#### Scenario: 与统一 Presenter change 协调
- **WHEN** `refactor-unified-animancer-presenter` 已实施
- **THEN** 本变更 MUST 校验 prefab 不再同时挂载旧 Locomotion Presenter 和旧 Action Presenter 作为正式路径
- **AND** 若统一 Presenter 尚未实施，本变更 MUST NOT 提前删除旧 Presenter 导致当前正式播放路径断裂

