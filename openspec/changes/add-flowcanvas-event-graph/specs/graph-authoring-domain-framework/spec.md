## MODIFIED Requirements

### Requirement: Authoring节点与Runtime执行描述必须分离

Graph Authoring Domain Framework MUST只理解稳定作者identity、typed payload、port与mutation。编译执行领域 MUST不要求authoring node继承runtime node，并 MUST继续把authoring graph编译为领域自己的中间表示和runtime program；Runtime性能枚举、线性index与switch可以继续存在于compiled层，但 MUST不反向成为创建菜单、Details或Document schema。

明确采用原生执行的通用 EventGraph MUST复用其原生图实例与节点行为，通过宿主合同规定事件、输入输出、类型和生命周期，不要求再生成项目专用事件IR或解释器。原生实例 MUST与可编辑资产隔离。该执行边界 MUST不自动扩展到现有Skill、FSM或PoseGraph，不放开它们的作者图直接运行。

#### Scenario: Runtime增加优化字段

- **WHEN** Pose Runtime为执行计划增加内部offset或buffer index
- **THEN** Authoring capability、Details与Document MUST不自动暴露该字段
- **AND** Compiler MUST负责从Pose IR生成该内部值

#### Scenario: 原生事件图调用变量节点

- **WHEN** 已明确采用原生执行的事件图使用宿主准入的 Get/Set
- **THEN** 系统 MUST执行原生节点并遵守唯一变量合同
- **AND** MUST不要求建立同义项目指令或启动另一个备用执行器

#### Scenario: Pose作者图被尝试直接启动

- **WHEN** 调用方试图因新增事件图能力而直接启动现有Pose作者图
- **THEN** 系统 MUST继续拒绝，Pose MUST沿其唯一编译程序执行
