## ADDED Requirements

### Requirement: ExposedProperty 节点必须原子维护模式与端口方向

Runnable `ExposedPropertyNode` MUST以唯一领域入口原子维护`NodeType`与`m_Value.Direction`：Get对应Output，Set对应Input。UI、Timeline authoring、Agent Mutation与其它调用者 MUST只提交目标mode和declaration binding，不得分别写端口方向。正式Validator MUST拒绝mode与方向不一致的节点，系统 MUST不在打开窗口、导出Document或编译时静默修复资产。

#### Scenario: Timeline Decision创建Set节点

- **WHEN** Timeline Decision TreeClip创建写入Frame Blackboard declaration的ExposedProperty节点
- **THEN** 正式节点入口 MUST同时设置Set mode和Input方向
- **AND** Timeline authoring MUST不再额外修改`m_Value.Direction`

#### Scenario: Set节点资产方向损坏

- **WHEN** Validator发现`NodeType=Set`但`m_Value.Direction=Output`
- **THEN** Validator MUST报告节点identity、mode、实际方向与期望方向
- **AND** Canvas、Exporter与Compiler MUST停止处理该节点
- **AND** 系统 MUST不按任一字段作为fallback继续运行
