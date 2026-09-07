## RENAMED Requirements

- FROM: `### Requirement: Document v4必须原子替代v3`
- TO: `### Requirement: Document v5必须原子替代v4`
- FROM: `### Requirement: Document v4失败恢复必须同时覆盖Unity owner与正式package`
- TO: `### Requirement: Document v5失败恢复必须同时覆盖Unity owner与正式package`

## MODIFIED Requirements

### Requirement: Agent Authoring Document必须是按需生成的持久化目录包

系统 MUST在唯一 Document v5 中为已有合法 CharacterController、Timeline 及其他仍由正式规范保留的 domain 根提供确定性目录包。Timeline domain 根 MUST为精确 shared Timeline 资产，无需 Character 或 Skill；CharacterController 的技能/控制配置保持原 owner。其他领域的退役 MUST由其正式变更处理，Timeline 增量不得恢复已退役的游戏 AIController 根、包 reader 或 AI 正文。目录包 MUST位于 Unity 项目内、Assets 外的正式 domain/root 位置，只在显式 checkout 时从 Unity 作者资产创建或刷新，MUST不成为另一份作者真相、Player 内容或 Runtime 输入。

#### Scenario: AI首次编辑现有Character Controller

- **WHEN** Agent 对合法 Character root 显式 checkout
- **THEN** 系统 MUST导出该角色的控制配置、技能、Timeline、表现和允许的可达曲线闭包
- **AND** response MUST返回唯一绝对包路径，MUST不修改或保存 Unity 资产

#### Scenario: 普通人工编辑期间没有AI会话

- **WHEN** 作者修改资产而没有显式 checkout
- **THEN** 系统 MUST不创建/刷新包或触发对账、编译和发布

#### Scenario: 独立 Timeline checkout

- **WHEN** 请求明确选择 Timeline domain 及合法 shared Timeline 路径
- **THEN** MUST导出该内容、输入声明、曲线、TreeClip 与可达子图
- **AND** MUST不搜索或创建关联 Character Definition

#### Scenario: 游戏 AI domain 已正式退役

- **WHEN** 新 AI 变更已移除 AIController domain 后安装 Timeline 增量
- **THEN** 目录包装配 MUST只增加正式 Timeline 根，不得重新接受旧 AI 包或创建插件 AI Document

### Requirement: 文档包必须分离可编辑authoring、只读context与service基线

所有 domain MUST使用同一 service-owned manifest/sync、editable 和只读 context 分区。sync 只保存基线身份/hash，不保存作者业务。Character v5 editable MUST保持控制配置、技能和原 Presentation/允许的原生曲线 owner；Timeline editable MUST只表达其内容、外部输入声明和树依赖，不能写运行目标对象、ActionInstance、播放状态或执行服务。Rig、分析生成数据、运行状态和 Program/Projection 只进入必要的只读摘要或省略。

#### Scenario: AI读取Character文档包

- **WHEN** checkout Character root
- **THEN** editable MUST表达其正式可写业务，context 只读表达类型、资源、依赖和必要能力
- **AND** MUST不暴露 Unity YAML、managed-reference 布局或私有序列化路径

#### Scenario: AI尝试修改只读context

- **WHEN** 只读 context 相比基线出现语义变化
- **THEN** parser 或 Reconciler MUST拒绝并报告 readonly_context_modified，不生成 Mutation

#### Scenario: Timeline 包写入场景实例

- **WHEN** editable 试图记录当前门对象、角色实例或本次播放句柄
- **THEN** strict parser MUST拒绝该运行数据

### Requirement: Timeline结构与Curve payload必须分离

Timeline 目录 MUST通过 timeline.json 表达轨道、片段、ownership、typed 参数、外部输入/绑定声明和资源引用，通过 curves.json 表达完整本地曲线。两种 domain 可引用同一 shared 作者资产，但不能持有两份真数据。Timeline JSON 不得表达 Marker、Sequence、原生 Clip 注册曲线、Foot Analysis 或运行接口。曲线 MUST保留正式语义并省略与 catalog 默认值相同的字段；修改必须提交完整曲线状态。

#### Scenario: AI只修改Timeline weighted curve

- **WHEN** Agent 替换一条本地曲线
- **THEN** Reconciler MUST保留时间、值、切线、必要权重、模式和 wrap 语义
- **AND** MUST不要求 key 级 MCP 操作

#### Scenario: Timeline提交Marker字段

- **WHEN** timeline.json 包含旧 SyncMode、SyncGroup、Topology、Role 或 Marker 字段
- **THEN** parser MUST拒绝整包，不能忽略旧字段

### Requirement: 可编辑能力必须由唯一authoring capability catalog闭合

同一领域能力合同 MUST驱动 exporter、strict parser、Reconciler、Mutation preflight、Validator 和只读目录。正式 Node/Track/Clip kind MUST声明 typed 字段、允许 owner/组合、引用、创建/配置/删除及反向导出。可编辑实体没有完整能力时 checkout MUST以 authoring_capability_incomplete 失败，不能输出假可编辑内容或按 C# 类型名绕过。各 domain 只投影合法内容，不新增独立 Timeline 能力表。

#### Scenario: Exporter发现未登记Node类型

- **WHEN** 可写节点缺少完整合同
- **THEN** checkout MUST报告精确节点、owner 和缺失能力，不能把 C# 类型名写进 editable

#### Scenario: Timeline 片段缺少删除能力

- **WHEN** 新片段只能导出和创建而不能正式删除
- **THEN** checkout MUST拒绝不完整作者能力，不能只让 UI 成功操作

### Requirement: Document v5必须原子替代v4

系统 MUST复用主重构的唯一 v5 文档合同，删除 v4 及更早 reader/writer/schema 和兼容 apply 分支。Timeline domain MUST作为 v5 的正式目标装配，不得临时接 v4、引入另一种包格式或再维护第二迁移器。旧包必须显式重新 checkout，五个生命周期工具及事务语义保持。

#### Scenario: 读取v3文档包

- **WHEN** service 发现 v3 或 v4 旧包
- **THEN** dry-run/apply MUST拒绝且不修改资产，要求显式重新 checkout

### Requirement: Document v5失败恢复必须同时覆盖Unity owner与正式package

Application Service MUST在 Mutation 前解析并锁定整包所有正式 serialized owner，并使用一个 Undo 事务。只有 Mutation、全域校验、作者保存、反向导出、staging 重读/hash 校验和包原子替换全部成功，才能返回 applied、saved 和 Clean；任一步失败 MUST恢复全部 owner 并保留上一份正式包。Character 和 Timeline apply 均不得发布 Program/Projection/分析产物。Character 原生 Clip 注册曲线修改 MUST保持原分析输入 hash 边界，不错误使匹配分析产物过期。

#### Scenario: Clip Curve Validator失败

- **WHEN** Character 内容已发生 Mutation 而 Clip 曲线校验失败
- **THEN** MUST回滚同一事务的 Gameplay、Timeline、Clip 和 Presentation owner
- **AND** 正式包保持原内容，响应不得报告 Clean

#### Scenario: 独立 Timeline 子树保存失败

- **WHEN** Timeline 和共享子树 Mutation 后任一必要保存/反向发布失败
- **THEN** MUST回滚全部相关 owner 与包，不能只保存时间轴或子树中的一部分

## ADDED Requirements

### Requirement: 独立 Timeline 闭包必须复用整包事务与共享冲突检测

Timeline domain MUST使用相同严格文件族、manifest 发现、local identity、分片配对和完整闭包规则。内联 Timeline 只能由真实 owner 的包修改；独立根只能引用明确 shared 资产。Character 包和 Timeline 包共享资产时，任一提交 MUST更新同一 live revision；另一包必须按是否有本地编辑呈现 TreeDirty 或 Conflict，不得自动覆盖、建立文件级 apply 或复制资产。输入/片段/子树修改必须经同一正式 Mutation。

#### Scenario: 两个包编辑同一共享内容

- **WHEN** Timeline 包有未提交修改，而 Character 包先提交相同 shared 资产
- **THEN** Timeline 包下一次生命周期操作 MUST报告 Conflict 并拒绝覆盖

#### Scenario: 独立根缺失必要子树文件

- **WHEN** 包缺少 manifest 要求的 TreeClip/子树配对分片
- **THEN** strict parser MUST拒绝整包，不能回读 Unity 数据补齐后继续 apply
