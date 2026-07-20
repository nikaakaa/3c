# character-pipeline-blackboard Specification

## MODIFIED Requirements

### Requirement: Pipeline Blackboard 必须统一图变量和运行时黑板

Blackboard declaration、ExposedProperty authoring、Graph Data Catalog 和 scope/lifetime 语义 MUST继续是唯一黑板数据源。Compiler MUST将 declaration/reference 解析为 Program layout，Kernel MUST只通过 CharacterSimulationState Blackboard slots 读写。

#### Scenario: Compiled ValueNode 读取变量

- **WHEN** ConditionRuleGraph operation 读取 Blackboard declaration
- **THEN** MUST通过 compiled address 访问 CharacterSimulationState
- **AND** MUST不反射 authoring ExposedProperty object

### Requirement: Runtime value 必须按 declaration 与 scope owner 共同寻址

Compiler MUST为 declaration identity、Character、Graph activation、State execution path、ActionInstance 和 Frame owner 生成稳定 layout/address rule。Kernel MUST使用 owner generation 隔离实例，MUST不使用 runtime object reference 或 dictionary object identity 作为真值地址。

#### Scenario: 两次 State activation

- **WHEN** 同一 State 第二次进入
- **THEN** 新 owner generation MUST与上一次 State frame 隔离

### Requirement: 嵌套状态机必须按 declaration owner 解析 State activation frame

Nested StateMachine MUST使用 Program 中编译的 declaration owner 和完整 execution path 定位 State frame。Runtime MUST不从 Graph clone 或显示名推断 owner。

#### Scenario: 内层 State 读取自己的 Frame

- **WHEN** 内层 State operation 读取 State-scoped variable
- **THEN** MUST命中完整 outer-to-inner path 对应的 owner bucket

### Requirement: Runtime Fact 和 Blackboard Variable 必须命名分层

Blackboard variable MUST只表达 Program 内运行变量、调参值或当前 scope state；SimulationOutput typed fact MUST表达当前 Tick 已发生、可记录、调试或由模型消费的事实。只有正式 fact projection MAY从当前 Blackboard write provenance 生成 typed fact。Model adapter 与 Committer MUST不直接读取 Blackboard key/value。

#### Scenario: Timeline 产出攻击窗口

- **WHEN** Decision TreeClip 写入合法 ActionWindow-bound Frame variable
- **THEN** Program MUST让后续 operation 读取该 variable
- **AND** projection MUST另外产生带 ActionInstance 与 EventId 的 ActionWindow fact

#### Scenario: 本地调参变量

- **WHEN** RunThreshold 只参与 ConditionRuleGraph
- **THEN** MUST保持 Config Blackboard 语义
- **AND** MUST不自动成为 SimulationOutput fact

### Requirement: ExposedProperty 必须成为 Pipeline Blackboard 的 authoring 表面

BaseExposedProperty MUST继续是 Pipeline Blackboard declaration 的唯一 authoring/serialization 表面。Compiler MUST将 declaration owner、reference、scope、lifetime、default value 与 projection 编译进 Program layout；Runtime MUST不同时维护 CharacterGraphContext dictionary、局部散字段或第二 Blackboard service。

#### Scenario: State body 创建 Local 变量

- **WHEN** 作者在 inline State body 创建 State scope declaration
- **THEN** declaration MUST仍归属该 Graph authoring
- **AND** Compiler MUST生成对应 owner/layout entry

### Requirement: Gameplay Effect 不得存入 Pipeline Blackboard

GameplayTag、Attribute、ActiveEffect、stack、duration、period、inhibition 与 journal MUST只存在于 CharacterSimulationState 的正式 GE slots。Blackboard MAY保存局部计算值或 fact projection source，但 MUST不复制 GE 真值。Value operation MUST通过正式 GE query读取 Attribute/Tag。

#### Scenario: Graph 读取 Health

- **WHEN** compiled Value operation读取当前 Health
- **THEN** MUST通过 GE state query读取
- **AND** MUST不从同名 Blackboard slot读取
