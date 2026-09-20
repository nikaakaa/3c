## MODIFIED Requirements

### Requirement: Product Manifest必须证明精确产物闭包

每个Network Test Candidate manifest MUST使用schema v3记录CandidateId、CandidateLabel、SourceCommit、SourceTreeHash、Product/Model/Topology、Program/Pipeline/Projection/World身份、runtime artifacts、Tool Bundles、Session Plan、Player配置和exact file closure。每个runtime artifact MUST声明唯一RoleId、Kind、ProductId、受约束相对root、entry point、configuration identity及可选manifest path／hash；每个Tool Bundle MUST引用明确artifact、版本、合同和BundleHash。公共系统不得用固定Player／Server字段、目录存在性、文件名或仓库脚本猜测闭包。Build完成后workflow MUST从最终Candidate目录重新读取并严格核对全部身份。schema v2、路径逃逸、时间BuildId、未声明文件、缺失文件、混合Product或工具hash不匹配 MUST失败，系统 MUST不提供兼容reader。

含 Bot 的产品 adapter MUST将真实端点名单、完整 Actor 输入所有权、行为/子树内容版本、任务/插件版本、角色输入目录和对应运行资源纳入其正式配置 artifact 与 exact closure。公共 workflow MUST只处理已声明 artifact/identity，不引用插件或按具体模型建立发布分支；本次不因插件增加而另起内容根或 Run 时 Build。

#### Scenario: Candidate混入另一版GM

- **WHEN** Rollback Candidate中的GM Tool Bundle来自另一Candidate或CommandCatalogHash不匹配
- **THEN** Candidate validation MUST拒绝正式发布或Run
- **AND** MUST不只校验Player/Relay后忽略工具差异

#### Scenario: DotRecast目录混入Unity Authority Worker

- **WHEN** DotRecast Candidate包含未声明的Unity Authority Worker文件、artifact或工具身份
- **THEN** exact closure validation MUST拒绝发布
- **AND** MUST不通过忽略额外文件或修改manifest掩盖混合产物

#### Scenario: Runtime artifact路径逃逸

- **WHEN** artifact或Tool Bundle路径规范化后离开Candidate Root
- **THEN** Build与Run MUST在启动前拒绝
- **AND** MUST不搜索仓库目录、修复路径或复制外部文件

#### Scenario: Bot图已更新但产品仍引用旧行为闭包

- **WHEN** 待发布/运行产品的行为或任务版本与其声明配置不符
- **THEN** exact closure validation MUST拒绝发布或 Run
- **AND** MUST不从项目 Assets、另一 Candidate 或缓存补齐

#### Scenario: Rollback产品增加Bot角色

- **WHEN** adapter 发布两个真实 Peer 控制多个 Actor 的配置
- **THEN** Candidate MUST锁定新的所有权和输入协议身份，并包含生产端所需行为内容
- **AND** Relay artifact MUST仍保持纯 .NET 闭包而不携带插件运行程序集
