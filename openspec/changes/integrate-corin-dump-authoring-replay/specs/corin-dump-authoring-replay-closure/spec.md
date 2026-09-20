## Purpose

为Corin建立从外部Dump来源、正式Unity作者配置、Graph／领域／表现产物、Session运行到固定输入Replay的可追溯闭环，确保每一步都使用同一组稳定身份、版本和依赖证据。

## ADDED Requirements

### Requirement: Corin Dump Source必须有不可变来源身份

系统 MUST 为每次Corin配置闭包记录Dump根来源、来源版本、文件哈希、资源身份和解析状态。外部绝对路径只能作为导入期证据，运行时和正式作者配置 MUST 不直接依赖Dump路径。

#### Scenario: Dump来源完整

- **WHEN** Corin资源被导入或核对
- **THEN** 系统 MUST 能从来源manifest追溯模型、Rig、AnimationClip、Binding、Foot Analysis和相关快照的来源身份与哈希
- **AND** 缺失、重复或哈希不匹配的来源 MUST 阻止进入正式配置闭包

### Requirement: Corin正式配置必须从唯一Definition形成完整闭包

系统 MUST 从精确Corin CharacterPipelineDefinition形成唯一闭包，覆盖Control/Input、Skill/Action、GameplayEffect/Tag/Attribute、Motion、Presentation/Pose、Foot/IK/Blend、Timeline、Session Composition、Prefab/Scene和生成产品引用。目录扫描、显示名和其他worktree文件 MUST 不能补齐闭包。

#### Scenario: 完整闭包核对

- **WHEN** 系统准备Corin作者闭包或运行产物
- **THEN** 所有正式owner、稳定identity、引用和来源manifest MUST 能从Definition闭包解析
- **AND** 任一跨角色、跨Target或未声明引用 MUST 被拒绝

### Requirement: Dump来源、作者资产和生成产物必须分层

系统 MUST 将Dump归一化为正式作者资产后再参与Definition闭包。作者资产可以引用来源identity，但Runtime MUST不读取Dump文件；Graph artifact、领域／表现binding和Replay结果 MUST不能反向成为作者配置来源。

#### Scenario: 禁止生成产物反向驱动作者配置

- **WHEN** 生成产品或运行Dump存在但作者资产缺失或版本不一致
- **THEN** 系统 MUST 报告缺少正式作者来源并拒绝继续Build或Replay

### Requirement: Corin Numeric Target和Session必须成组固定

每个Corin运行目标 MUST同时固定Numeric Target、Graph artifact、Domain Binding Set、Presentation Binding、Session Composition、Prefab／Scene、Source和Solver identity。Float32、Fixed、Rollback和Server Authority的产物 MUST不能交叉混用。

#### Scenario: Target身份不一致

- **WHEN** Replay或运行配置使用不同Target的Graph artifact、binding或Session
- **THEN** 系统 MUST 在启动前拒绝并报告完整身份差异

### Requirement: Replay必须固定输入与运行版本

每次Corin Replay MUST绑定固定Input Trace、初始Actor／World配置、GraphArtifact／DomainBindingSet／PresentationBinding hash、Session身份、时钟模式和运行构建身份。Replay MUST不创建临时执行器或旁路配置。

#### Scenario: 同条件Replay

- **WHEN** 使用相同Corin闭包和同一Fixed Input Trace执行Replay
- **THEN** 系统 MUST 输出可关联的RunId、版本身份、Action/Timeline生命周期、State/Snapshot hash和Body结果
- **AND** 输入、时钟或运行版本不一致时 MUST 只报告不可比较，不得伪造A/B结论

### Requirement: Corin闭环必须按Gate串行推进

正式流程 MUST按Dump来源核对、作者配置闭包、Graph／领域／表现产物准备、Session／Play、Runtime Dump／Replay和最终比较的顺序执行。前一Gate未成功时 MUST不进入后一Gate。

#### Scenario: 前置Gate失败

- **WHEN** 来源、Authoring Closure、产物准备或Session校验失败
- **THEN** 系统 MUST 停在当前Gate并保留机器诊断
- **AND** MUST 不使用旧产物或其他worktree结果继续Replay

### Requirement: Camera必须作为Corin Presentation闭包的一部分

Corin正式配置 MUST 包含Camera Profile、Default Sequence、Camera Curve、Action/Scene Camera Sequence以及近远裁剪、Locate Radius、Elevation、FOV、Smooth、Rotation/Avatar Transition、Input、Locking、Collision、Zoom、Stretch、Shake和Shot参数。Skill Timeline的Camera/Scene请求 MUST 只能引用这些正式Camera owner，Replay MUST 固定Camera配置与相机输入身份。

#### Scenario: Camera配置参与闭包核对

- **WHEN** Corin配置被导出、Build或Replay
- **THEN** 系统 MUST 核对Camera Profile、Sequence、Curve、数值参数和引用的stable identity/hash
- **AND** 缺少Camera owner或相机输入版本不一致时 MUST 拒绝Build/Replay比较
