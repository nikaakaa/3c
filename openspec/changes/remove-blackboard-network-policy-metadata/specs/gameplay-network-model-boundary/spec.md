## MODIFIED Requirements

### Requirement: BTSMTL Authoring 不得拥有 Network Model 配置

Graph、StateMachine、Timeline、TreeClip、Blackboard、Action、Behavior、GameplayEffect 和 CharacterSimulationProgram MUST不保存 ModelId、Endpoint、Transport、history、correction、rollback、WorldSolver implementation selection，或变量级Authority/SyncPolicy/replication策略。Blackboard Input Binding MUST只连接portable input与typed Character State；Fact Projection MUST只生成model-neutral GameplayFact。Program MAY只声明 model-neutral required capabilities。Network Model是否发送事实 MUST只由自己的fact kind/producer coverage决定。

#### Scenario: 复用同一 Program

- **WHEN** 同一 Program被 Local Source与后续 Network Model Source使用
- **THEN** BTSMTL authoring MUST保持不变
- **AND** Blackboard declaration MUST不因Model切换增加或修改网络策略字段

#### Scenario: ActionWindow没有packet mapping

- **WHEN** Program生成ActionWindowFact但当前Model coverage不支持ActionWindow
- **THEN** fact MUST保持model-neutral本地Gameplay输出
- **AND** Model MUST不读取Blackboard key、Input Binding或Fact Projection来推导默认packet

