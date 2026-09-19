## ADDED Requirements

### Requirement: Timeline在作者代码归位中只更新直接依赖

Timeline 的内容、Prepare/Playback/TreeClip 调用身份、调度、Marker、快照及提交语义 MUST 保持。此次整理 MUST 只更新迁出类型的直接引用，不强制删除仍有真实消费者的 BaseGraph 参数或创建替代上下文。无消费者且无资产引用的旧 UI 附属入口才可删除。

#### Scenario: 调用参数仍有正式消费者

- **WHEN** Timeline 接口仍通过图参数表达真实调用上下文
- **THEN** MUST 保留现有行为并列明消费者
- **AND** 如果解除该依赖需要改变运行合同，MUST 报告具体缺口交用户决定

#### Scenario: 作者类型位置改变

- **WHEN** Timeline 作者集成引用的类型原样移入独立文件
- **THEN** MUST 同步直接引用并保持内容与调用身份
- **AND** MUST 不新增播放服务、独立求值路径或通用上下文
