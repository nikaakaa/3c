## 1. Candidate 与工具合同

- [ ] 1.1 定义schema v3 Network Test Candidate、CandidateId、SourceCommit/Tree、Tool Bundle、Session Plan、Slot Catalog、RunManifest与RunStatus合同。
- [ ] 1.2 实现CandidateLabel和Git worktree身份读取，Prepare前后拒绝脏输入、HEAD变化和生成物未提交状态。
- [ ] 1.3 将公共Network Product、Server Product和工具身份从时间BuildId迁移为CandidateId，保留BuiltAtUtc为非身份元数据。
- [ ] 1.4 建立Tool Bundle canonical identity与文件闭包，覆盖公共Orchestrator、产品启动adapter和Rollback GM Tool Identity。

## 2. 版本化Build与目录迁移

- [ ] 2.1 将三个Product输出迁移到`Build/Network/<Product>/<CandidateId>`，候选staging成功后原子发布且同Candidate拒绝覆盖。
- [ ] 2.2 升级Network Test Product manifest到schema v3，写入源码、Program/Projection/Pipeline/World、runtime artifacts、tool bundles、session plan和exact closure。
- [ ] 2.3 迁移UnityAuthority、DotRecastAuthority与DeterministicRollback adapter，使公共workflow不按产品分支处理Candidate或Tool Bundle。
- [ ] 2.4 删除schema v2 reader、固定ProductRoot消费、同产品backup替换、时间BuildId目录语义与旧固定根引用。
- [ ] 2.5 实现严格Candidate Catalog和显式Remove Candidate，禁止latest选择、递归搜索、自动迁移和删除Active Run引用。

## 3. Session Orchestrator

- [ ] 3.1 新增`Tools/ThirdPersonNetworkTest/ThirdPerson.NetworkTest.Orchestrator.csproj`及Unity无关共享合同链接，建立普通.NET 8进程入口。
- [ ] 3.2 实现Candidate、Tool Bundle、Session Plan与Slot preflight，创建Run目录、RunManifest、RunStatus和Run-owned配置。
- [ ] 3.3 实现有界进程组、产品启动adapter调用、ready/fault/stop生命周期和只回收本Run进程的所有权校验。
- [ ] 3.4 建立正式Session Slot Profile和portable catalog，为Rollback提供至少两个互不重叠槽位并拒绝动态端口fallback。
- [ ] 3.5 将三个现有Run脚本迁为Candidate-owned启动adapter或等价正式入口，删除默认ProductRoot、StopExisting和仓库当前脚本依赖。

## 4. Rollback Run与GM版本

- [ ] 4.1 将Rollback Candidate静态Model/Program/World/roster身份与RunId/SessionId/endpoint/token实例配置分离。
- [ ] 4.2 迁移Relay、Peer、GM Server和GM Console，使它们只消费精确Candidate与Run配置并校验CandidateId、RunId、SessionId和实例身份。
- [ ] 4.3 建立`GmToolIdentity`、ToolVersion、ProtocolVersion、CommandCatalogHash与BundleHash生成和握手校验。
- [ ] 4.4 把`RollbackGmBuildProfile`迁移为无端口和token的静态Tool Policy，删除Candidate中的旧GM/Relay查询运行manifest。
- [ ] 4.5 保持四个只读命令、Relay线程查询桥、Player无GM凭据和GM/Gameplay故障隔离，不增加玩法命令或采样路径。

## 5. Authority Product收口

- [ ] 5.1 将UnityAuthority Server Product与Network Product切换为CandidateId、版本目录和candidate-owned工具入口，保留单一显式默认Slot。
- [ ] 5.2 将DotRecastAuthority Server Product、Authority artifact与Network Product切换为CandidateId、版本目录和candidate-owned工具入口，保留单一显式默认Slot。
- [ ] 5.3 删除两个Authority产品的时间BuildId、固定当前Product替换和仓库Run脚本hash依赖，不修改其Gameplay、Room、Worker或Authority Scene语义。

## 6. Test Control Center

- [ ] 6.1 将Launcher Network Test区迁移为CandidateLabel、Product Build、Candidate Catalog、版本/工具身份和严格状态显示。
- [ ] 6.2 接入显式Candidate＋Slot Start、Run列表、Open GM、Open Logs、Stop Owned Session和Remove Candidate。
- [ ] 6.3 让Launcher只启动Orchestrator和读取小型状态，不在OnGUI、OnInspectorUpdate或Unity主线程执行进程等待、日志解析或大文件hash。
- [ ] 6.4 删除旧固定三行Run、默认当前Product、StopExisting和按时间选择语义，不保留转发按钮。

## 7. 仓库与规范收口

- [ ] 7.1 更新Repository Policy精确允许Network Test Orchestrator工程，不宽泛允许其它Tools项目，也不新增CI job。
- [ ] 7.2 同步`openspec/project.md`、Network Test和GM使用文档到Candidate、Tool Bundle、Slot、Run与Control Center唯一链路。
- [ ] 7.3 使用规定参数构建Orchestrator与受影响普通.NET产品并立即执行`dotnet build-server shutdown`，清理构建服务残留。
- [ ] 7.4 执行本change与全量OpenSpec strict validation，核对旧schema v2、固定ProductRoot、构建期token/端口、外部Run脚本和兼容入口已经删除。

