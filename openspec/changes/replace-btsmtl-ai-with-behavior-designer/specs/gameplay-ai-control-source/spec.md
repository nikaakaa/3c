## REMOVED Requirements

### Requirement: AI Program必须只消费Perception并产出Character Input

**Reason**: 自研 AI Program、Semantic IR 和专用执行链整体退役，插件直接执行属于新的输入生产能力。

**Migration**: 删除旧编译/运行合同，以“AI决策必须只消费正式观察并产出Character Input”规定插件的正式输出边界；共享技能执行基础继续保留。

### Requirement: AI State必须拥有独立生命周期和身份

**Reason**: 自研 AI 候选状态与逐 Tick 状态回滚被一次性外部输入生产取代，不属于原 AI State 的命名调整。

**Migration**: 删除旧 AIControllerState/codec，按“AI输入生产与角色模拟必须分离状态所有权”迁移为插件实例状态、不可改写输入事实和正式消费事务。

## RENAMED Requirements

- FROM: `### Requirement: Local Session必须在Character Evaluate前冻结全部AI输入`
- TO: `### Requirement: Session必须在Character Evaluate前冻结全部角色输入`

## MODIFIED Requirements

### Requirement: AI Perception必须来自冻结的Committed Session观察

每次生产新输入时，Source MUST通过正式只读端口提供最近已提交的 Actor/World 观察及明确声明的请求/Action 结果投影。观察 MUST包含 ObservationTick、锁定名单身份和稳定 ActorId，同一本端输入批次中的 AI MUST读取同一版本。候选 MUST以显式 ActorId 或已正式声明的业务规则选择；本能力不提供 Team/Faction 推断。观察 MUST不读取当前 Tick 部分结果、角色私有状态、VisualRoot、Animator、Camera、Scene 扫描、Tag、名称或身份前缀。

Rollback 生产端 MUST能标明观察来自尚未确认的预测分支；随后发生世界纠正 MUST不改写已经生产的输入，新决策再读取新的已提交观察。

#### Scenario: 两个AI按不同顺序注册

- **WHEN** 同一输入批次的两个 AI Actor 注册顺序变化
- **THEN** 它们 MUST看到相同的观察版本
- **AND** MUST不读取其它 Actor 当前 Tick 的部分执行结果

#### Scenario: 目标正在视觉插值

- **WHEN** 目标 VisualRoot 位于两个逻辑 Body sample 之间
- **THEN** 观察 MUST继续使用最近已提交逻辑 Body
- **AND** 视觉插值 MUST不改变 AI 的观察数据

#### Scenario: AI Profile配置显式目标

- **WHEN** 作者为 Bot 配置候选 Actor
- **THEN** 配置 MUST保存稳定 ActorId 并从所属会话观察解析
- **AND** MUST不扫描场景或按队伍名称猜测目标

#### Scenario: AI等待攻击结果

- **WHEN** 行为查询自己提交的请求
- **THEN** 观察 MUST提供由正式 Input/Action 所有者产生、按请求身份关联的只读结果
- **AND** MUST不提供角色状态地址或可变 Action 对象

### Requirement: Session必须在Character Evaluate前冻结全部角色输入

Local、Unity Authority 与 Rollback 各自正式 Source MUST在其输入准备边界组织批量决策与角色输入装配，并保持本模型唯一 CanonicalInputBatch/step 输入 writer。已冻结的玩家、Neutral 和 Bot 输入 MUST按稳定 ActorId 形成完整模拟批次以后，才允许 Character Evaluate/World ResolveBatch。公共 Host、Composer 和 Ingress MUST按显式能力合同调用，MUST不根据插件类型、名称、Tag 或兜底策略选择输入源。

#### Scenario: AI与玩家同一Tick输入

- **WHEN** 一个可支持的会话包含玩家和 Bot
- **THEN** 进入该模拟 Tick 的全部正式角色输入 MUST先完成冻结
- **AND** 所有角色 MUST继续进入同一 World ResolveBatch

#### Scenario: AI Evaluate失败

- **WHEN** 插件因任务、观察或输入合同错误失败
- **THEN** 当前批次 MUST不发布部分玩法结果
- **AND** 诊断 MUST报告行为、Actor、输入 Tick 和失败任务

#### Scenario: 玩家与AI共享Local Ingress

- **WHEN** Local roster 包含 Player、Neutral 与插件控制源
- **THEN** 三者 MUST通过同一正式输入收集形成唯一 CanonicalInputBatch
- **AND** MUST不安装第二个 AI Ingress 或直接调用角色执行入口

### Requirement: 不支持AI的Session Composition必须明确拒绝

支持矩阵 MUST由正式装配能力声明：Local Float32/Fixed 可由本机生产，ServerAuthoritative Unity Authority 可由 Worker 生产，Rollback 可由显式指定的 Unity Peer 生产；其它网络客户端只消费对应正式输入或权威结果。纯 .NET DotRecast Authority 与 Relay MUST不承载插件运行。未声明能力、生产者、观察或行为资源的装配 MUST在 Active 前拒绝，MUST不自动改变宿主、创建 AI 服务进程或替换为其它控制源。

#### Scenario: 网络客户端Scene配置AI Actor

- **WHEN** 客户端没有该 Bot 的输入生产权却配置可运行行为
- **THEN** 会话准备 MUST拒绝该配置并定位归属冲突
- **AND** 客户端 MUST不私自计算该 Bot 的正式输入

#### Scenario: DotRecast Authority配置插件AI

- **WHEN** 纯 .NET Authority 产品包含需要插件运行的 Actor binding
- **THEN** 产品/会话校验 MUST明确报告不支持
- **AND** MUST不增加 Unity AI 旁路或保留 portable AI 兼容实现

## ADDED Requirements

### Requirement: AI决策必须只消费正式观察并产出Character Input

游戏 AI MUST使用正式绑定的插件行为，根据冻结观察产生匹配角色 Numeric Target 和 input/request catalog 的 CharacterSimulationInput。AI MUST不执行角色 Action、技能、Timeline、Motion、GameplayEffect、WorldSolver 或 Presentation，不再生成自研 AI Semantic IR/Program 或维护第二行为树执行器。

#### Scenario: AI决定攻击

- **WHEN** 插件行为根据观察选择攻击
- **THEN** 输出 MUST是带稳定 RequestId、sequence 和 source tick 的正式角色请求
- **AND** C# 角色规则与唯一 Action 服务 MUST自行判断准入并创建释放

#### Scenario: 行为包含直接执行技能的任务

- **WHEN** 正式 AI 行为尝试直接激活 Action 或推进 Timeline
- **THEN** 任务权限校验 MUST拒绝该行为
- **AND** MUST不运行旧 AI Program 或跳过非法任务继续

### Requirement: AI输入生产与角色模拟必须分离状态所有权

插件行为状态 MUST由生产端插件实例持有，MUST不进入 Character 状态、Fixed 世界 snapshot 或角色历史重演。正式 Source MUST以 Session、ActorId、输入 Tick 唯一标识一次生产，并保存不可改写的已冻结输入、原请求身份和生产 frontier。角色回滚、发送重试和外层事务恢复 MUST不撤销这些输入事实或重新推进相同生产键；消费位置和模型 pending timing MUST继续遵守各自正式状态合同。

开始执行插件后，任务异常或输出校验失败 MUST使生产者与会话明确失败，MUST不声称已恢复插件、重跑同 Tick 或改用 Neutral。整批输入冻结以后发生模拟失败时，Character/World/输出 MUST按原事务撤销，输入事实保留；既有失败策略允许重试时 MUST仅复用原帧。该能力 MUST不提供任意旧存档恢复后重新生成相同未来 AI 的承诺。

#### Scenario: AI节点跨Tick运行

- **WHEN** 一个任务跨多个新输入 Tick 保持 Running
- **THEN** 插件实例 MUST保存其任务进度和战术变量
- **AND** CharacterSimulationState MUST不保存该节点状态

#### Scenario: 行为内容版本不匹配

- **WHEN** 行为版本、任务版本或角色输入目录不匹配正式绑定
- **THEN** 会话准备 MUST失败
- **AND** MUST不装载旧 AI Program 或另一行为版本

#### Scenario: 输入已冻结但WorldSolver失败

- **WHEN** 当前 Tick 的 Bot 攻击输入已冻结，而随后世界求解失败
- **THEN** 本轮 Character/World 结果 MUST不发布
- **AND** 原输入、序号与 GameplayHash MUST保持；允许的重试 MUST不再次执行 BT

#### Scenario: 插件在产生半批结果后异常

- **WHEN** 一个 Bot 任务失败且本端其它任务已改变插件状态
- **THEN** 该新输入批次 MUST不部分发布，生产者与 Session MUST失败
- **AND** MUST不通过恢复旧 AI 状态或补造中性帧继续
