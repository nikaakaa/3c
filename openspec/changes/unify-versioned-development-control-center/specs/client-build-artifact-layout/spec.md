## RENAMED Requirements

- FROM: `### Requirement: Network Test Product 必须保留现行 Build/Network 合同`
- TO: `### Requirement: 开发测试产物必须使用项目统一产物库`

## MODIFIED Requirements

### Requirement: 客户端正式产物必须收敛到唯一 Build 根

商业客户端正式 Content 与普通 Player MUST只使用客户端 `Build/Content` 与 `Build/Players`，其版本与发布规则保持。Network Test 与 Local Fixed 性能候选 MUST作为开发测试产物发布到唯一显式项目 ArtifactRoot，不能作为商业 Player、Content 或 CDN 源。`Builds`、`Bundles`、`Library`、`HybridCLRData`、`Build/.Workspace` MUST不作为正式商业产物；旧 `Build/Network` 与 `Library/Performance` MUST不再拥有开发正式产物职责。两类产物 MUST不互相复制或双写。

#### Scenario: 查看客户端工程根目录

- **WHEN** 作者查看商业发布版本与开发实验版本
- **THEN** 商业版本 MUST来自客户端 Build 分区，开发实验 MUST来自项目唯一产物库
- **AND** 任一开发候选 MUST不能被解释为可发布商业 Player

#### Scenario: 输出路径逃逸

- **WHEN** 商业构建输出离开客户端 Build 或开发产物库落入已登记 worktree
- **THEN** 对应构建 MUST在写入前拒绝
- **AND** MUST不改写默认目录继续

### Requirement: 开发测试产物必须使用项目统一产物库

Network Test 与 Local Fixed 开发候选 MUST使用 `<ArtifactRoot>/<ProjectId>/Candidates/<ProductId>/<CandidateId>`，运行与采样/分析 MUST分别使用统一 Runs、Captures、Analyses、Comparisons 目录。Library、Temp、外部短路径 Unity 构建区 MUST只作为临时缓存，不能被 Run 消费。跨盘结果 MUST先进入产物库 staging 校验，再原子发布。工作区删除 MUST不删除已发布版本和报告。

#### Scenario: 构建任一 Network Test Product

- **WHEN** 两个工作区属于同一 ProjectId 且发布不同合法候选
- **THEN** 两份候选 MUST在同一库内并存并由所有 Center 实例读取
- **AND** 后一次发布 MUST不修改任何已发布内容

#### Scenario: 两个工作区各自发布候选

- **WHEN** 两个已登记工作区同时发布不同候选
- **THEN** 统一索引 MUST在完整原子发布后显示两者
- **AND** MUST不共享或覆盖工作区的 Library

#### Scenario: 清理旧开发根

- **WHEN** 批准的迁移核对完旧产物所有权与保留证据
- **THEN** 系统 MUST删除确认失效的旧根与活动消费者
- **AND** MUST不保留 Build/Network 或 Library/Performance 的镜像、链接或兼容读取
