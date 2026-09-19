## ADDED Requirements

### Requirement: 原生端口对象只能作为领域适配

FlowCanvas/NodeCanvas 的 Port、Connection 和回调对象 MUST 由所属领域的稳定字段、逻辑端口与连接规则生成。它们 MAY 承担框架生命周期和序列化端点，但 MUST 不拥有第二份业务参数、变量声明、执行规则或运行真值。节点定义、人工编辑、C# authoring 和 Skill compiler MUST 从同一领域合同读取端点和字段。

#### Scenario: 重建技能节点端口

- **WHEN** 技能图重新打开或节点参数改变导致动态端口更新
- **THEN** 适配 MUST 从正式节点定义重建相同的稳定逻辑端口
- **AND** MUST 不从旧 Edge、显示标题或运行 getter 推断业务端口

#### Scenario: 无消费者的旧节点适配

- **WHEN** 旧 TreeDesigner 节点或适配已没有正式资产和调用者
- **THEN** 迁移完成后 MUST 删除该适配、执行入口和重载
- **AND** MUST 不保留兼容节点或第二编译路径
