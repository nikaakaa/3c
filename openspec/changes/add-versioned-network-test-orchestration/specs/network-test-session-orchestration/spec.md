## ADDED Requirements

### Requirement: Network Test Candidate必须绑定可证明的源码版本

每次Network Test Build MUST要求作者显式提供CandidateLabel，并从干净Git worktree读取SourceCommit与SourceTreeHash，建立稳定CandidateId。Build MUST在Product Prepare前后校验同一HEAD和干净状态；Prepare产生未提交正式输入时 MUST在Player Build前失败。CandidateId MUST不由时间、目录mtime、EditorPrefs或当前分支名生成。

#### Scenario: Prepare更新了Projection资产

- **WHEN** Product Prepare使被跟踪的Program、Projection或其它正式输入发生变化
- **THEN** Candidate Build MUST停止并列出未提交变化
- **AND** MUST不继续构建Player或把变化隐藏进Candidate

#### Scenario: 干净提交构建候选

- **WHEN** 作者从干净worktree以显式CandidateLabel执行Build且Prepare不改变源码输入
- **THEN** manifest MUST记录完整SourceCommit、SourceTreeHash和确定性CandidateId
- **AND** 构建时间 MUST只作为非身份元数据

### Requirement: Candidate Catalog必须只管理严格合法的不可变候选

系统 MUST只在三个显式Product根的一层Candidate目录中发现schema v3 manifest，并校验目录CandidateId、ProductId、源码身份、Tool Bundle与exact closure。已发布Candidate MUST不可覆盖、合并或按时间替换；Catalog MUST不选择latest、不递归搜索其它目录、不读取schema v2或修复损坏产物。

#### Scenario: 同Candidate再次Build

- **WHEN**目标Product下已经存在相同CandidateId
- **THEN** Build MUST在替换任何文件前失败
- **AND** MUST不比较构建时间或覆盖旧Candidate

#### Scenario: 显式删除候选

- **WHEN** 作者删除一个合法且没有Active/Starting Run引用的Candidate
- **THEN** 系统 MUST只删除该精确Candidate目录并更新Catalog
- **AND** 自动清理策略、latest链接和相似目录删除 MUST不存在

### Requirement: Tool Bundle必须与Candidate形成精确版本闭包

Candidate manifest MUST显式声明全部Tool Bundle的ToolId、ToolVersion、ContractVersion、ArtifactRoleId、entry point、configuration identity和BundleHash。Launcher与Orchestrator MUST只使用Candidate内部匹配工具；全局最新版、仓库当前脚本、下载工具、旧协议adapter或文件名猜测 MUST不作为运行输入。

#### Scenario: 当前仓库工具已经更新

- **WHEN** 作者运行一份仍合法的旧Candidate而仓库中的Orchestrator或GM源码已经变化
- **THEN** Run MUST继续使用Candidate exact closure中的匹配Tool Bundle
- **AND** MUST不拿仓库当前工具替换后继续

#### Scenario: Tool Bundle文件被修改

- **WHEN** Candidate中的工具文件hash不再匹配BundleHash
- **THEN** Run MUST在启动任何业务进程前失败
- **AND** MUST不下载、重建或选择另一工具版本

### Requirement: Session Slot必须显式声明且互不争用

系统 MUST提供版本化Session Slot Catalog，Slot MUST声明稳定SlotId、逻辑endpoint端口和窗口资源。Orchestrator MUST在Start前校验Product Session Plan所需全部key、端口占用和Slot owner。Slot被占用或身份不明时 MUST明确失败，不停止未知进程、不动态找空闲端口、不切换Slot。

#### Scenario: 两个Rollback Candidate使用不同Slot

- **WHEN** 作者分别以Slot A和Slot B启动两份合法Rollback Candidate
- **THEN** 两个Session MUST使用不重叠的Relay、Peer、GM和查询endpoint并同时运行
- **AND** 任一Session停止 MUST不影响另一Session

#### Scenario: 重复占用同一Slot

- **WHEN** Slot A仍由一个Active Run拥有而作者再次选择Slot A
- **THEN** 新Start MUST在创建业务进程前失败并报告现有RunId
- **AND** MUST不通过StopExisting抢占Slot

### Requirement: 每个Run必须由独立Orchestrator拥有完整实例身份

每次Run MUST由Candidate携带的普通.NET Orchestrator创建RunManifest、RunStatus、运行配置和日志目录。RunManifest MUST绑定Candidate manifest/hash、Product、Tool Bundle、Slot、RunId、SessionId、resolved endpoint与role配置hash。Orchestrator MUST以有界进程组拥有本次进程，Start失败或Stop时只回收身份匹配的本Run进程；不得编译、修改Candidate或杀死其它Session。

#### Scenario: 一个Peer启动失败

- **WHEN** Session Plan中的一个Peer在ready前退出
- **THEN** Orchestrator MUST把本Run标记为Faulted并回收本Run已经启动的角色
- **AND** Candidate、其它Run和其它Slot MUST保持不变

#### Scenario: Unity域重载

- **WHEN** 启动Session后Unity Editor发生域重载
- **THEN** 独立Orchestrator MUST继续拥有Session进程和状态
- **AND** Launcher恢复后 MUST只根据RunManifest与RunStatus重新显示，不重新启动Session

### Requirement: Test Control Center必须显式管理Candidate与Run

唯一`Tools/3C/Launcher` MUST提供Network Test Candidate Build、Catalog、Candidate/Slot选择、Start、Run状态、Open GM、Open Logs、Stop Owned Session与Remove Candidate。界面 MUST不在选择、恢复或刷新时自动Build/Run，不在GUI回调执行进程等待、大文件hash或日志分析，也 MUST不把单场GM命令业务搬入Launcher。

#### Scenario: 作者启动指定候选

- **WHEN** 作者在Control Center选择精确Candidate和Slot并执行Start
- **THEN** Launcher MUST启动该Candidate的匹配Orchestrator并显示其RunId与状态
- **AND** MUST不自动选择最新Candidate、替换Slot或停止其它Run

