## RENAMED Requirements

- FROM: `### Requirement: Network Test Candidate必须绑定可证明的源码版本`
- TO: `### Requirement: Network候选必须复用统一源码与候选身份`

- FROM: `### Requirement: Candidate Catalog必须只管理严格合法的不可变候选`
- TO: `### Requirement: Network候选目录必须由统一产物库管理`

- FROM: `### Requirement: Tool Bundle必须与Candidate形成精确版本闭包`
- TO: `### Requirement: Network工具必须固定来源而不依赖仓库当前版本`

- FROM: `### Requirement: Session Slot必须显式声明且互不争用`
- TO: `### Requirement: Network槽位必须使用机器级租约`

- FROM: `### Requirement: 每个Run必须由独立Orchestrator拥有完整实例身份`
- TO: `### Requirement: 每个Network Run必须由统一Host拥有完整实例身份`

- FROM: `### Requirement: Test Control Center必须显式管理Candidate与Run`
- TO: `### Requirement: Network操作必须进入统一Development Center`

## MODIFIED Requirements

### Requirement: Network候选必须复用统一源码与候选身份

Network Build MUST通过 development-artifact-versioning 固定干净提交、依赖、产品配方和工具，生成构建前 CandidateId 并以最终 manifest/hash 证明文件。作者标签 MUST不作为唯一键；Prepare 改变正式输入时 MUST停止并显示待提交变化。

#### Scenario: 同提交使用不同构建工具

- **WHEN** 两份 Network 候选采用不同工具或构建配方
- **THEN** 它们 MUST分别发布并保留准确身份
- **AND** MUST不因 label 或 commit 相同覆盖另一产物

#### Scenario: Prepare更新了Projection资产

- **WHEN** Product Prepare使被跟踪的Program、Projection或其它正式输入发生变化
- **THEN** Candidate Build MUST停止并列出未提交变化
- **AND** MUST不继续构建Player或把变化隐藏进Candidate

#### Scenario: 干净提交构建候选

- **WHEN** 作者从干净worktree以显式CandidateLabel执行Build且Prepare不改变源码输入
- **THEN** manifest MUST记录完整SourceCommit、SourceTreeHash和确定性CandidateId
- **AND** 构建时间 MUST只作为非身份元数据

### Requirement: Network候选目录必须由统一产物库管理

Network Catalog MUST只读取项目共享库中显式 ProductId 的已发布候选。旧 Build/Network 固定根与 schema 1/2/3 MUST不能成为新活动候选，历史需要保留时只能按 History 规则登记。候选删除 MUST检查统一引用关系与活动任务。

#### Scenario: 旧固定根仍存在

- **WHEN** 磁盘上仍有旧 Player/Server/Gm 目录
- **THEN** Center MUST显示旧数据待清理状态而不把它们枚举为合法候选
- **AND** MUST不自动迁移或补写新 manifest

#### Scenario: 同Candidate再次Build

- **WHEN** 项目统一产物库中已经存在相同CandidateId
- **THEN** 发布器 MUST核对既有精确闭包后返回已有产物或报告内容冲突
- **AND** MUST不覆盖、合并或按构建时间替换已有目录

#### Scenario: 显式删除候选

- **WHEN** 作者删除一个合法且没有Active/Starting Run引用的Candidate
- **THEN** 系统 MUST只删除该精确Candidate目录并更新Catalog
- **AND** 自动清理策略、latest链接和相似目录删除 MUST不存在

### Requirement: Network工具必须固定来源而不依赖仓库当前版本

候选 MUST固定产品 adapter、GM 和其他运行工具的 ToolArtifactId/接口/配置；RunRequest MUST额外固定所选 Development Run Host。工具 MAY由共享库的精确不可变引用提供，无需每候选复制一份，但依赖集合 MUST全量校验。实例配置 MUST绑定 CandidateId、RunId、SessionId 和工具身份。缺失或不支持时 MUST拒绝，不选最新、不自动更新。

#### Scenario: 仓库工具更新后运行已有候选

- **WHEN** 当前源码中的工具已经更新
- **THEN** Run MUST继续使用请求固定的共享工具产物
- **AND** MUST不执行仓库当前脚本代替

#### Scenario: 当前仓库工具已经更新

- **WHEN** 作者运行一份仍合法的旧Candidate而仓库中的Development Run Host或GM源码已经变化
- **THEN** Run MUST继续使用共享库中请求固定的精确工具产物
- **AND** MUST不拿仓库当前工具替换后继续

#### Scenario: Tool Bundle文件被修改

- **WHEN** 所引用封存工具的文件hash不再匹配其工具manifest
- **THEN** Run MUST在启动任何业务进程前失败
- **AND** MUST不下载、重建或选择另一工具版本

### Requirement: Network槽位必须使用机器级租约

Rollback MUST保留 rollback-a、rollback-b 两个正式不重叠槽位；Authority 产品 MUST保持显式 default。槽位、endpoint 与角色要求 MUST通过统一机器级协调器校验，跨 worktree 或 Host 版本也不能重复占用。系统 MUST不动态找端口、抢占已有会话或回退模型。

#### Scenario: 两份Rollback候选同时运行

- **WHEN** 两个候选分别取得 rollback-a 与 rollback-b
- **THEN** 它们 MAY同时运行各自 Relay、GM 与两个 Client
- **AND** 任一 Run 停止 MUST不影响另一 Run

#### Scenario: 两个Rollback Candidate使用不同Slot

- **WHEN** 作者分别以Slot A和Slot B启动两份合法Rollback Candidate
- **THEN** 两个Session MUST使用不重叠的Relay、Peer、GM和查询endpoint并同时运行
- **AND** 任一Session停止 MUST不影响另一Session

#### Scenario: 重复占用同一Slot

- **WHEN** Slot A仍由一个Active Run拥有而作者再次选择Slot A
- **THEN** 新Start MUST在创建业务进程前失败并报告现有RunId
- **AND** MUST不通过StopExisting抢占Slot

### Requirement: 每个Network Run必须由统一Host拥有完整实例身份

Development Run Host MUST唯一创建 Network RunManifest、状态、配置和日志，并通过统一角色计划拥有进程。产品 adapter MUST只提供对应产品的配置与 Ready 语义，不拥有第二 Run 生命周期。失败或停止 MUST只回收本 Run；Player MUST不接收 GM 凭据，GM MUST不进入 Gameplay 驱动。

#### Scenario: Peer在Ready前失败

- **WHEN** 某个必需 Peer 在准备阶段退出
- **THEN** Host MUST把本次 Run 标记故障并结束其已启动角色
- **AND** MUST不修改候选或其他会话

#### Scenario: 一个Peer启动失败

- **WHEN** Session Plan中的一个Peer在ready前退出
- **THEN** Development Run Host MUST把本Run标记为Faulted并回收本Run已经启动的角色
- **AND** Candidate、其它Run和其它Slot MUST保持不变

#### Scenario: Unity域重载

- **WHEN** 启动Session后Unity Editor发生域重载
- **THEN** 独立Development Run Host MUST继续拥有Session进程和状态
- **AND** Launcher恢复后 MUST只根据RunManifest与RunStatus重新显示，不重新启动Session

### Requirement: Network操作必须进入统一Development Center

Network Candidate、Slot、Start/Stop、GM 和日志 MUST在唯一 Development Center 中显示。Center MUST只列产品实际支持的能力，不能替没有回放合同的网络产品生成 Replay/Capture 通过状态。GUI 与 CLI MUST调用同一服务。

#### Scenario: 查看只支持功能运行的网络产品

- **WHEN** 作者选择未声明性能场景的 Network 候选
- **THEN** Center MUST提供其正式功能运行与 GM/日志操作
- **AND** MUST不显示伪造的性能结果或隐式改用 Local Fixed

#### Scenario: 作者启动指定候选

- **WHEN** 作者在Control Center选择精确Candidate和Slot并执行Start
- **THEN** Launcher MUST启动该Candidate的匹配Development Run Host并显示其RunId与状态
- **AND** MUST不自动选择最新Candidate、替换Slot或停止其它Run
