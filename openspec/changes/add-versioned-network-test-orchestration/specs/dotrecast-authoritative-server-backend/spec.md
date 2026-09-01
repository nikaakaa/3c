## MODIFIED Requirements

### Requirement: Build与Run必须分离且产物必须按模型隔离

Unity Authority与DotRecast Authority MUST分别拥有显式Build入口、Product根、Player、Fantasy Server、Candidate manifest、candidate-owned启动adapter和日志目录。Build MUST从干净Git提交锁定CandidateId、Player target/options、Server configuration和Authority artifacts，只创建新的不可变Candidate目录且不启动进程。Unity Player MUST继续使用`StandaloneWindows64 + IL2CPP + Development + StrictMode`，Fantasy Server MUST继续使用Debug配置并记录实际编译选项。Unity Authority发布的`Fantasy.config` MUST只包含Gate Scene；DotRecast Authority发布的`Fantasy.config` MUST只包含Gate Scene与DotRecast Authority Scene，且Authority Scene manifest、Program和Navigation artifact MUST随Server以正式相对路径发布。Run MUST显式选择并校验一个Candidate及其Tool Bundle，不触发编译、publish、目录修复或latest选择。Unity Authority Session Plan MUST启动Fantasy Server、Unity Authority Worker、Client A和Client B；DotRecast Session Plan MUST只启动Fantasy Server、Client A和Client B。Candidate manifest MUST记录SourceCommit/Tree和BuiltAtUtc，MUST不使用`BuildId=yyyyMMdd-HHmmss`作为版本。两个Product根和全部Candidate MUST互不覆盖；每次Run MUST按Product与RunId建立RunManifest、状态和日志目录。未形成完整可运行闭环的Product MUST不注册占位Candidate或Run入口。

#### Scenario: 构建新的DotRecast Candidate

- **WHEN** DotRecast Authority已有其它合法Candidate并从新提交Build
- **THEN** 新Build MUST发布到新的CandidateId目录并保留旧Candidate
- **AND** MUST不修改Unity Authority或旧DotRecast Candidate的Player、Server、manifest、工具和日志

#### Scenario: 分别Build和Run Unity Authority

- **WHEN** 作者从干净提交Build一个Unity Authority Candidate
- **THEN** 系统 MUST按正式Player与Server配置发布新Candidate且不启动测试进程
- **AND** 作者随后Run该Candidate时 MUST只启动其Fantasy Server、Unity Authority Worker、Client A与Client B且不重新构建

#### Scenario: 运行指定DotRecast Candidate

- **WHEN** 作者选择合法Candidate和其正式默认Slot执行Run
- **THEN** Orchestrator MUST只消费该Candidate的Player、Server、Authority artifacts和启动adapter
- **AND** MUST不使用仓库当前PowerShell脚本或另一Candidate配置
