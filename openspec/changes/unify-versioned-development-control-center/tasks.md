## 1. 规范所有权与版本合同

- [ ] 1.1 按 spec-audit 完成批准范围内重叠 active delta 的所有权交接，移除重复 ADDED 与旧工程允许项；保留独立框架、GM 与 Gameplay 要求
- [ ] 1.2 在既有 tooling-contracts 按模块定义 Project、Workspace、Source、Tool、Candidate、Run、Capture、Analysis、Comparison 合同，分离 Player Runtime DTO 与 Host-only 合同
- [ ] 1.3 为 Run Host、产品 adapter、织入器、采集模块、基础转换器与分析器建立模块自有版本描述，删除发布器代管的版本常量
- [ ] 1.4 实现规范化内容身份及不可变引用规则，区分源码/产物身份、数据格式、Metric 语义与测量身份，消除 manifest 自包含哈希

## 2. 正式产物库与工作区登记

- [ ] 2.1 实现显式 ProjectId、ArtifactRoot、MachineControlRoot 与 Workspace/Unity instance 登记及路径约束，缺失配置明确失败
- [ ] 2.2 实现 Sources、Tools、Scenarios、Candidates、Runs、Captures、Analyses、Comparisons 的 staging、精确闭包校验、原子发布和并发索引
- [ ] 2.3 实现干净源码封存、Git bundle、解析依赖身份与输入变化检测，拒绝构建期间混合 HEAD 或修改后的输入
- [ ] 2.4 实现跨 worktree 只读目录、来源跳转与引用关系，删除产物前检查活动运行及保留记录依赖，区分工作区移除与产物删除
- [ ] 2.5 对接并行开发任务已提交的 ProjectEditorPreferences/SessionState 隔离，记录新 worktree 所需正式源码与本地依赖闭包，避免依赖未跟踪文件或共享缓存

## 3. 统一候选构建

- [ ] 3.1 将 NetworkTestProductBuildWorkflow 提升为 DevelopmentCandidateBuildWorkflow，抽取公共 Unity/外部构建与发布能力，同时切换全部网络产品消费者
- [ ] 3.2 将既有 Performance Player 构建作为 Local Fixed 产品 adapter 接入，保留 IL2CPP、同盘短路径临时输出、符号、Scenario 与全部编译期诊断能力合同
- [ ] 3.3 实现同提交多构建配方/多工具的 CandidateId，固定业务与工具闭包；作者标签不再作为候选唯一键
- [ ] 3.4 接入每工作区独占构建与机器负载租约，保持规定 dotnet 参数和完成/失败后立即 shutdown
- [ ] 3.5 删除旧独立 Network/Performance 构建身份、正式发布目录配置和直接构建入口，保留现有产品适配业务
- [ ] 3.6 将并行开发任务的命令行构建接缝接入同一 Build Workflow，后台 Unity 按精确 projectPath/请求启动并在完成或失败后退出，不要求第二个常驻 Editor

## 4. 唯一运行 Host 与机器资源

- [ ] 4.1 将 Network Orchestrator 迁为 Development Run Host，统一不可变请求、状态、心跳、Job Object、角色启动身份与取消处理
- [ ] 4.2 将网络 adapter 的进程启动/停止切到 Host 公共进程能力，保留正式 Slot、GM/Relay 协议与产品 Ready 语义
- [ ] 4.3 将 Performance Controller 采集逻辑迁为 Worker，统一 Player/工具进程所有权并保留 Agent 协议、Smoke/Replay Gate、WPR owned cleanup
- [ ] 4.4 实现跨项目/worktree/工具版本的机器资源命名空间、Slot/endpoint 租约及所有权恢复，拒绝未知占用与不支持的租约合同
- [ ] 4.5 实现受管理重负载共享租约、正式性能独占队列、公平等待与取消状态，记录外部负载污染和无法判断的环境信息
- [ ] 4.6 实现全机单个 Unity 构建租约、正式内存准入/持续监测配置、MemoryBudgetExceeded 状态及本次进程终止，保留合法导入缓存且不自动重试

## 5. 采样、分析与比较

- [ ] 5.1 将基础 Capture 与下游 Analysis/Comparison 分开发布，保存各阶段工具身份、原始证据及结构化成功/失败状态
- [ ] 5.2 固定完整 DiagnosticCapabilitySet 与 KK 包/Generator/Program/Schema 身份，通过现有框架生命周期封存子 manifest，不复制领域 Session、Writer 或分析规则
- [ ] 5.3 将统计、预算和热点分析提取为独立版本化 Analyzer，显式读取支持合同的 Completed 数据，重新分析产生新结果而不回写 Capture
- [ ] 5.4 分离插桩产物身份、稳定 PointId、SourceMap 与 MeasurementIdentity，删除物理路径/行号/业务 BuildId 相等准入条件
- [ ] 5.5 实现工作负载、测量工具、完整诊断能力、环境和分析合同的比较准入及结构化冲突列表；预算评价单独绑定，行为一致性单独显示
- [ ] 5.6 接入 Unity 原生托管分配调用栈诊断 Profile 与明确的数据读取能力，发布字节数/次数/调用路径/未解析覆盖信息并区分 MissingEvidence

## 6. Center、CLI 与结果展示

- [ ] 6.1 将 Launcher 中既有 Center 升级为统一工作区/候选/运行/报告视图，显示产品支持的操作与准备输入造成的待提交变化
- [ ] 6.2 建立唯一 development.* 命令服务与 CLI/MCP 薄适配，所有 Unity 操作显式绑定 unity_instance，长任务使用持久化 OperationId
- [ ] 6.3 接入构建、Smoke、Replay、Capture、分析、选择基线、比较、取消及日志/报告打开动作，分别显示运行完整性、业务结果、预算与证据缺失
- [ ] 6.4 将哈希、目录扫描、日志分析、进程等待和构建调度移出 GUI 回调，域重载后按精确请求恢复显示，不自动重跑
- [ ] 6.5 删除旧 Network/Performance 顶层窗口分区、菜单、CLI/MCP 别名和各自的全局当前选择状态，所有控制面使用同一命令链

## 7. 旧数据、工程与文档收口

- [ ] 7.1 实现旧产物所有权/引用盘点和显式清理入口，将需要保留的原始证据登记为不可执行 History，删除确认失效的旧正式目录和配置
- [ ] 7.2 删除旧 Orchestrator/Controller 工程并精确登记 RunHost、Worker、Analyzer 三个工具工程，保持现有 CI 范围和生成产物禁止跟踪规则
- [ ] 7.3 更新工具 README、模块输入输出与版本维护说明，记录各控制面唯一入口及工作区/机器配置职责
- [ ] 7.4 对完成实现执行受影响普通 .NET 工程构建与立即 shutdown、仓库策略及固定 OpenSpec strict 校验，处理本 change 引入的问题
- [ ] 7.5 按最终实现同步 current specs 与 project.md，清除旧路径/旧 schema/双重生命周期要求，保留真实未完成状态与运行证据引用
- [ ] 7.6 将批准后的本机显式后台构建例外同步到执行规则，保留 CI 禁用 Unity、禁止 Computer Use 和禁止常驻第二 Editor 的边界
