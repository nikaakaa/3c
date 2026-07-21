# btsmtl-ai-controller-authoring Specification

## Purpose
定义 AI Controller 独立 Definition、Tree、Graph capability、Blackboard、Perception 与 Intent authoring 的唯一正式边界。
## Requirements
### Requirement: AI Controller必须拥有独立Definition与RootTree

系统 MUST使用`AIControllerDefinition`作为AI authoring装配根，显式保存稳定ControllerId、AI RootTreeAsset、受控CharacterPipelineDefinition、AIPerceptionProfile与generated AIIntentProgram identity。AI RootTree数据 MUST是`AIControllerTree`并继续由`BaseTreeAsset`持有；系统 MUST NOT新增AI专用Graph asset shell，也 MUST NOT把AI RootTree内联进CharacterPipelineDefinition。

#### Scenario: 创建AI Controller

- **WHEN** 作者创建新的AIControllerDefinition
- **THEN** Definition MUST获得稳定ControllerId并显式绑定AI RootTree
- **AND** RootTree MUST使用AIControllerTree Graph Role

#### Scenario: 绑定普通BaseTree

- **WHEN** Definition引用的RootTree不是AIControllerTree
- **THEN** Inspector与Compiler MUST拒绝该配置
- **AND** MUST NOT按普通BaseTree解释执行

### Requirement: AI Tree编辑必须复用BTSMTL窗口核心

系统 MUST提供`AIControllerTreeWindow : BaseTreeWindow`或等价薄领域窗口，使AI Tree与Character Tree可以作为两个Unity dockable窗口同时打开。AI窗口 MAY增加Controller、Perception、Intent和AI Program信息，但 MUST复用BaseTreeView、Inspector基础、Graph Data Catalog、page stack、breadcrumb、Undo、selection、dirty和Live Debug基础。系统 MUST NOT在Character窗口内嵌第二GraphView或创建AI Workbench。

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

### Requirement: AI Controller Tree必须限制节点领域

AIControllerTree MUST只允许SharedFlow、SharedPureValue、AI Blackboard、AIObservation、AIMemory、AIIntent与无副作用Editor Debug capability。它 MUST拒绝Character Action、StateMachine执行、Timeline、Motion、Animation、GameplayEffect、Unity InputAction、WorldSolver和Transform副作用节点。Node Search与所有mutation入口 MUST得到相同结果。

#### Scenario: 作者搜索AI节点

- **WHEN** 当前Graph为AIControllerTree
- **THEN** Search MUST显示共享Flow/Value和AI专用节点
- **AND** MUST不显示ActivateActionInstance、TimelineNode或Motion节点

#### Scenario: 粘贴Character分支

- **WHEN** 作者把包含CharacterExecution节点的分支粘贴进AIControllerTree
- **THEN** 整次不合法mutation MUST被拒绝
- **AND** 系统 MUST不创建部分节点或占位节点

### Requirement: AI Blackboard必须与Character Blackboard分离

AI Controller MUST使用独立AI Blackboard与AIControllerState。Controller scope MUST跨AI Logic Tick保存记忆；Tick scope MUST只存在于一次AI Evaluate；Graph scope MUST保持Graph局部owner。AI Blackboard MUST NOT解析Character、State、ActionInstance scope，也 MUST NOT直接访问CharacterSimulationState。AI与Character之间唯一可写边界 MUST是最终CharacterSimulationInput。

#### Scenario: AI保存当前目标

- **WHEN** AI Tree把选中Actor写入Controller-scope CurrentTarget
- **THEN** 值 MUST进入AIControllerState并在下一AI Tick可读
- **AND** Character Pipeline Blackboard MUST不增加同一变量副本

#### Scenario: AI读取Character动作变量

- **WHEN** AI节点尝试引用Character或ActionInstance scope declaration
- **THEN** Data Catalog与Compiler MUST拒绝该引用
- **AND** Runtime MUST不回退到同名AI变量

### Requirement: AI Intent节点必须绑定受控Character输入目录

AI Intent authoring MUST从Definition绑定的Character Program/Input catalog选择稳定InputId、RequestId与value kind。连续输入、ActionTargetSnapshot和离散request MUST使用typed字段；自由字符串、InputAction显示名、Graph node名称或默认request MUST NOT成为绑定来源。

#### Scenario: 配置移动Intent

- **WHEN** 作者配置WriteContinuousInput节点输出MoveAxis
- **THEN** Inspector MUST从受控Character input catalog选择Vector2 InputId
- **AND** Compiler MUST验证value kind一致

#### Scenario: 配置攻击Intent

- **WHEN** 作者配置SubmitActionRequest节点输出Attack
- **THEN** Inspector MUST选择正式RequestId并显示timing class
- **AND** AI节点 MUST不直接引用ActionProfile或ActivateActionInstance

