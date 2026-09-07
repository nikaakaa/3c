## ADDED Requirements

### Requirement: Rollback GM必须发布完整Tool Bundle身份

Rollback GM Tool Bundle MUST声明稳定ToolId、ToolVersion、ProtocolVersion、CommandCatalogHash、ConfigurationIdentity与BundleHash。CommandCatalogHash MUST覆盖稳定排序后的命令Id、版本、权限、参数和结果合同。Candidate、GM服务和控制台 MUST绑定同一Tool Bundle；不匹配时 MUST拒绝启动或连接，不加载全局最新版、旧协议adapter或兼容命令目录。

#### Scenario: 控制台版本与服务不匹配

- **WHEN** GM Console的ToolVersion、ProtocolVersion或CommandCatalogHash与目标服务不同
- **THEN** 控制台 MUST在提交命令前明确报告ToolVersionMismatch
- **AND** MUST不降级命令版本、隐藏未知命令或继续猜测结果

### Requirement: Rollback GM运行配置必须属于唯一Run

GM Server、GM Console和Relay Query配置 MUST由Orchestrator在Run目录创建，并绑定CandidateId、RunId、SessionId、Slot、Tool Identity、endpoint、token和容量策略。Candidate MUST只携带静态GM Tool/Policy，不携带运行token或固定endpoint。GM请求和响应 MUST关联Candidate、Run、Session及service/relay instance，跨Run或旧实例请求 MUST明确拒绝。Player MUST继续不接收GM配置或凭据。

#### Scenario: GM请求指向另一场并行Session

- **WHEN** 请求中的CandidateId、RunId或SessionId与当前GM/Relay目标不一致
- **THEN** 服务 MUST拒绝并报告目标身份不匹配
- **AND** MUST不转发到同Slot旧实例、同Candidate其它Run或本地执行

