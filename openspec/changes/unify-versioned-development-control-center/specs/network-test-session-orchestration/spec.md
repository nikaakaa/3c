## ADDED Requirements

### Requirement: Network候选必须复用统一源码与候选身份

Network Build MUST通过 development-artifact-versioning 固定干净提交、依赖、产品配方和工具，生成构建前 CandidateId 并以最终 manifest/hash 证明文件。作者标签 MUST不作为唯一键；Prepare 改变正式输入时 MUST停止并显示待提交变化。

#### Scenario: 同提交使用不同构建工具

- **WHEN** 两份 Network 候选采用不同工具或构建配方
- **THEN** 它们 MUST分别发布并保留准确身份
- **AND** MUST不因 label 或 commit 相同覆盖另一产物

### Requirement: Network候选目录必须由统一产物库管理

Network Catalog MUST只读取项目共享库中显式 ProductId 的已发布候选。旧 Build/Network 固定根与 schema 1/2/3 MUST不能成为新活动候选，历史需要保留时只能按 History 规则登记。候选删除 MUST检查统一引用关系与活动任务。

#### Scenario: 旧固定根仍存在

- **WHEN** 磁盘上仍有旧 Player/Server/Gm 目录
- **THEN** Center MUST显示旧数据待清理状态而不把它们枚举为合法候选
- **AND** MUST不自动迁移或补写新 manifest

### Requirement: Network工具必须固定来源而不依赖仓库当前版本

候选 MUST固定产品 adapter、GM 和其他运行工具的 ToolArtifactId/接口/配置；RunRequest MUST额外固定所选 Development Run Host。工具 MAY由共享库的精确不可变引用提供，无需每候选复制一份，但依赖集合 MUST全量校验。实例配置 MUST绑定 CandidateId、RunId、SessionId 和工具身份。缺失或不支持时 MUST拒绝，不选最新、不自动更新。

#### Scenario: 仓库工具更新后运行已有候选

- **WHEN** 当前源码中的工具已经更新
- **THEN** Run MUST继续使用请求固定的共享工具产物
- **AND** MUST不执行仓库当前脚本代替

### Requirement: Network槽位必须使用机器级租约

Rollback MUST保留 rollback-a、rollback-b 两个正式不重叠槽位；Authority 产品 MUST保持显式 default。槽位、endpoint 与角色要求 MUST通过统一机器级协调器校验，跨 worktree 或 Host 版本也不能重复占用。系统 MUST不动态找端口、抢占已有会话或回退模型。

#### Scenario: 两份Rollback候选同时运行

- **WHEN** 两个候选分别取得 rollback-a 与 rollback-b
- **THEN** 它们 MAY同时运行各自 Relay、GM 与两个 Client
- **AND** 任一 Run 停止 MUST不影响另一 Run

### Requirement: 每个Network Run必须由统一Host拥有完整实例身份

Development Run Host MUST唯一创建 Network RunManifest、状态、配置和日志，并通过统一角色计划拥有进程。产品 adapter MUST只提供对应产品的配置与 Ready 语义，不拥有第二 Run 生命周期。失败或停止 MUST只回收本 Run；Player MUST不接收 GM 凭据，GM MUST不进入 Gameplay 驱动。

#### Scenario: Peer在Ready前失败

- **WHEN** 某个必需 Peer 在准备阶段退出
- **THEN** Host MUST把本次 Run 标记故障并结束其已启动角色
- **AND** MUST不修改候选或其他会话

### Requirement: Network操作必须进入统一Development Center

Network Candidate、Slot、Start/Stop、GM 和日志 MUST在唯一 Development Center 中显示。Center MUST只列产品实际支持的能力，不能替没有回放合同的网络产品生成 Replay/Capture 通过状态。GUI 与 CLI MUST调用同一服务。

#### Scenario: 查看只支持功能运行的网络产品

- **WHEN** 作者选择未声明性能场景的 Network 候选
- **THEN** Center MUST提供其正式功能运行与 GM/日志操作
- **AND** MUST不显示伪造的性能结果或隐式改用 Local Fixed
