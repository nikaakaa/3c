## 1. GM 入口与正式目标

- [ ] 1.1 为现有运行时目标补充明确的进程、业务 Session、Peer/角色、Actor 与内容身份，区分现有 diagnostics Guid 和业务 SessionId。
- [ ] 1.2 关联 Gameplay/Animation 已注册目标，提供只读目标目录与终止通知，禁止新增角色状态镜像或场景扫描选择。
- [ ] 1.3 建立显式命令描述、参数/结果合同和模块注册执行器，区分查询、诊断控制和玩法控制。
- [ ] 1.4 安装目标列表、目标详情和诊断频道订阅命令，正确释放各调用方自己的 interest。
- [ ] 1.5 制作共用开发版 GM UI、Input System 配置与显式 Gameplay Lab 装配，显示当前进程、角色、能力和结果。
- [ ] 1.6 在现有设备适配边界实现共用 GM 焦点策略，阻止 UI 输入穿透，不改历史或已提交请求，不暂停网络模拟。
- [ ] 1.7 将开发入口/配置纳入正式构建验证，拒绝非 Development Player 误装；同步开发工具装配说明。

## 2. 共用运行时采样

- [ ] 2.1 建立多目标采样运行身份、状态机、频道集合、覆盖和结束原因合同，锁定开始时的目标集合。
- [ ] 2.2 提取现有 Foot 帧关联与字段构造，改为消费正式已提交 provider 数据，保留 Frame/Completion/Rig/Bank 一致性。
- [ ] 2.3 迁移唯一 CSV/geometry Writer、后台有界队列、封口和完整性发布，移除运行时路径中的 Editor 依赖。
- [ ] 2.4 接入正式 Gameplay Trace、Body、Action 命令/Inbox/生命周期、Pose/Foot/Goal/FBBIK/Final Pose 记录频道，禁止重新求值或二次物理查询。
- [ ] 2.5 在现有失败边界发布有身份的首个失败上下文与重复次数，区分 Attempted、Committed、无输出和丢失区间。
- [ ] 2.6 将 GM 和 Editor 的采样操作接到同一控制服务，删除 `gameplay-lab-player` 扫描、旧类型分支、重复字段定义与重复 Writer。
- [ ] 2.7 为 Network Test 客户端显式传入本次启动关联身份，为单机开发装配建立对应身份；新采样写入唯一正式可写根。
- [ ] 2.8 处理正常退出、target 结束、后台写入失败与非完整包；保留已收到证据，明确覆盖，不静默补齐。
- [ ] 2.9 同步 Launcher、现有录制/回放工作流和 MCP 的采样路径消费者，保留输入、回放时钟和 Proof 数学。

## 3. 诊断查询与结果展示

- [ ] 3.1 增加封存包身份与完整性入口校验，将各目标 Foot 原始文件交给已有唯一 Finalizer。
- [ ] 3.2 复用现有 Foot Analyzer、Publisher、紧凑 Store 和七维评分，不复制算法或重写历史包。
- [ ] 3.3 提供共用 `summary/events/detail/frame` 查询合同与 Foot 适配，GM/Editor/MCP 不重复分析。
- [ ] 3.4 提供 Action 边界事件与生命周期时间线查询，关联 PlaybackId/EventId/tick/sequence 和首个失败，不补造 Select 或业务状态。
- [ ] 3.5 提供单帧 Source/Goal/IK/Final 证据定位及跨端身份对照，显示采样时钟、分支与覆盖差异，不把本地骨骼差异当作同步错误。
- [ ] 3.6 更新 current spec/project 的最终已安装口径及正式工具说明，对尚未完成的阶段保持未勾选。
