## MODIFIED Requirements

### Requirement: AI Tree编辑必须复用BTSMTL窗口核心

系统 MUST提供`AIControllerTreeWindow : BaseTreeWindow`或等价薄领域窗口，使AI Tree与技能Tree可以作为两个Unity dockable窗口同时打开。AI窗口 MAY增加Controller、Perception、Intent和AI Program信息，但 MUST复用BaseTreeView、Inspector基础、Graph Data Catalog、page stack、breadcrumb、Undo、selection、dirty和Live Debug基础。系统 MUST NOT在Character窗口内嵌第二GraphView或创建AI Workbench。

#### Scenario: 并排编辑AI与Character

- **WHEN** 作者从AIControllerDefinition打开AI Tree并保持Character RootTree窗口打开
- **THEN** Unity MUST同时显示两个可停靠窗口
- **AND** 两者选择、页栈和authoring context MUST互相隔离
- **AND** 两者Graph mutation MUST调用同一BTSMTL authoring API

#### Scenario: 直接打开孤立AI Tree

- **WHEN** 作者直接双击AI RootTreeAsset而没有Definition context
- **THEN** AI窗口 MAY显示Graph结构
- **AND** 依赖Character或Perception的目录项 MUST显示缺失context
- **AND** 系统 MUST不搜索项目补齐Definition
#### Scenario: 角色控制迁入代码

- **WHEN** AI仍绑定同一正式Character输入合同
- **THEN** AI MUST继续经CharacterSimulationInput驱动角色，不进入技能内部state或控制对象


### Requirement: AI Intent节点必须绑定受控Character输入目录

AI Intent authoring MUST从Definition绑定的由代码控制合同和角色Program共同锁定的Input／Request catalog选择稳定InputId、RequestId与value kind。连续输入、ActionTargetSnapshot和离散request MUST使用typed字段；自由字符串、InputAction显示名、Graph node名称或默认request MUST NOT成为绑定来源。

#### Scenario: 配置移动Intent

- **WHEN** 作者配置WriteContinuousInput节点输出MoveAxis
- **THEN** Inspector MUST从受控Character input catalog选择Vector2 InputId
- **AND** Compiler MUST验证value kind一致

#### Scenario: 配置攻击Intent

- **WHEN** 作者配置SubmitActionRequest节点输出Attack
- **THEN** Inspector MUST选择正式RequestId并显示timing class
- **AND** AI节点 MUST不直接引用ActionProfile或ActivateActionInstance
