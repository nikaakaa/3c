## ADDED Requirements

### Requirement: 原生Pose观察不得要求Semantic产物

Pose 观察 MUST直接使用原生图节点／端口／调用实例与已完成结果，Semantic Inspector MUST只负责技能及原有独立内容的编译数据。取消 Character 根后检查入口 MUST显式选择 Ability，旧角色 Program 检查不得伪造为仍可运行。

#### Scenario: 查看没有编译Image的Pose节点
- **WHEN** 作者观察原生 Pose 图的当前结果
- **THEN** 工具 MUST定位真实图实例，不生成隐藏 IR 或 ProgramImage
