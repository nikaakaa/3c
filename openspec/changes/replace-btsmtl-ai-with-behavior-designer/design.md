## Context

动机和范围见 [proposal.md](proposal.md)。本设计依据 2026-09-05 的实际工作区和 current specs；当前主重构、Pose、Timeline 独立化等工作并行进行，未提交文件不等于已安装规范。

已核对的事实：

| 位置 | 当前行为 | 接入含义 |
|---|---|---|
| `Packages/com.opsive.behaviordesigner/package.json` | Behavior Designer 3.0.3，依赖 Entities 1.3.8；Graph Designer 3.0.2、Opsive Shared 2.2.4 已导入 | 本次使用该插件；不沿用此前 NodeCanvas 的能力判断 |
| `BehaviorTree.Tick` | 只启用 EvaluateFlag，实际执行发生在插件系统组更新 | 不能在每个 Actor 的 BuildInput 中逐个 Tick 后立即读取结果 |
| `TaskObjectSystem` | 调用普通 Task.OnStart/OnUpdate/OnEnd，并进行 ECS/托管变量同步 | 可以直接接现有 C# 游戏能力；不要求整个角色改成 ECS |
| `SaveManager` 与 `Wait` | 保存树进度和任务数据；Load 会停止/启动树；Wait 执行与保存使用的时钟不同，任务保存未覆盖全部随机状态 | 本次不把原生 SaveData 声明为项目逐 Tick 回滚快照，不修改插件内核补齐该能力 |
| `AICharacterControlSource` 与 `Float32AIControlSourceRuntime` | 装配自研 AI Program、观察、状态、诊断，并产生角色输入 | 游戏输入职责可保留，旧图和执行链需要完整退出 |
| `Float32LocalInputSourcePort.Read` | 对完整 roster 逐个 BuildInput | 增加正式批量准备能力，随后仍从唯一输入源取得同一批角色输入 |
| `RollbackEndpointInputSourcePort` | 一个本地 Actor/输入适配器；checkpoint 包含控制源状态、输入序号和本地 pending frame | 必须正式拆清外部输入事实和可恢复消费状态，不能只删 CaptureState 调用 |
| `RollbackRoster` 与 Relay | PeerId、PlayerId、ActorId 当前均唯一；Relay 按一个 Peer 对应的 Actor 校验发送权 | 多 Bot 接入需要修改名单、身份、握手、权限和历史容量，不能伪造玩家 |
| `AuthorityTickSchedule` | 从 accepted 输入为所有 Actor 形成权威 step；任何 Actor 尚无输入时 pending | 权威 Bot 必须有正式本地输入来源，不得等待一个不存在的 Bot 客户端 |

外部能力依据：[任务运行方式](https://opsive.com/support/documentation/behavior-designer-pro/getting-started/gameobject-or-entity-tasks/)、[性能边界](https://opsive.com/support/documentation/behavior-designer-pro/performance/)、[存档接口](https://opsive.com/support/documentation/behavior-designer-pro/save-load/)、[联网建议](https://opsive.com/support/documentation/behavior-designer-pro/networking/)。本轮仅做源码/文档核对，没有运行插件场景、回滚实验或性能采样。

## Goals / Non-Goals

**Goals:**

- 一个敌人的行为只有一份插件作者资产和一份运行状态，项目不再拥有 AI 图、编译器和解释器。
- 玩家、Bot 与中立 Actor 进入同一 CharacterSimulationInput、C# 控制、Action、技能、World 与结果链。
- 将非确定性的决策隔离在输入生产端；Fixed 角色和世界继续按完全相同的正式输入重演。
- 一次性输入生产、重发、事务失败、角色回滚和会话终止有可检查的状态归属。
- 交付现有稳定角色能实际配置和运行的插件任务与产品配置，不只新增未接线类型。

**Non-Goals:**

- 不维护通用 BT 内核、插件图到 AIProgram 的编译器、自定义通用黑板、第二个 AI 编辑器或插件 AI Document。
- 不保证 BT 对修正后的历史重新决策，不提供 AI 状态在 Peer 间无损接管、任意存档续跑或运行中换图/换代码。
- 不改写 KCC、MotionWarp、角色控制规则、技能解释器、Pose、IK、Camera 或正确的非 AI 网络语义。
- 不引入新的 AI Unity 服务进程，不让纯 .NET Relay/DotRecast Authority 装载插件。
- 不实现完整 Team/Faction、仇恨、寻路、命中、格挡伤害、生命/死亡闭环；不修复 TrainingEnemy 的动画和表现资产。
- 不以 DOTS 为理由重写所有游戏任务，不新增性能测试或将性能采样作为普通接入的前置条件。

## Decisions

### 1. 插件直接拥有行为，项目只提供游戏任务

采用 Behavior Designer 原生 BehaviorTree/Subtree、SharedVariable、节点生命周期和调试。现有角色 Control Source 接入组件直接引用插件行为组件/资产及正式角色输入绑定，不再创建一层持有另一份图正文的 AI Definition。

项目任务代码放在独立 Unity 插件接入程序集。目标筛选、输入目录、请求时序和已提交结果查询使用项目已有或按实际业务扩展的合同；这些合同不引用 Opsive、Entity 或插件 TaskStatus。TaskStatus 仅在插件任务中根据项目结果转换。

| 方案 | 作者/业务收益 | 维护成本与本次选择 |
|---|---|---|
| 插件编辑并执行 | 作者直接获得子树、条件打断、变量与调试，修改行为无需修改编译器 | 本次采用；维护窄游戏任务和输入接入 |
| 插件编辑、自研 Program 执行 | 可以延续 portable AI 执行与自有状态格式 | 仍需节点语义转换、运行器和调试映射，不符合本次减少维护的目标 |
| 自研 C# 决策模块执行 | 规则适合文本协作，脱离 Unity 的能力更容易控制 | AI 运行及作者体验仍由项目维护，本次不保留并行路径 |

内置 Transform、Animator、NavMesh 等直接控制任务不进入正式角色行为闭包。项目校验的是会改变角色/世界的输出权限，以及实际使用任务的依赖；不复制插件的 Sequence/Selector/abort 规则。需要新增玩法输出时，先通过正式角色请求能力接入，再提供插件任务。

### 2. 作者配置和维护范围

作者在插件窗口编辑“选择目标 → 接近 → 请求攻击 → 等待结果”等行为，通过插件子树复用。角色输入字段从该角色正式 input/request catalog 选择稳定身份，不能靠显示名、任意字符串或默认攻击绑定。

本次最小项目任务集合：

| 任务 | 输入 | 输出/结束语义 |
|---|---|---|
| 选择显式候选目标 | 当前 Actor、冻结观察、配置的候选 ActorId | 插件 Graph 变量中的目标 ActorId；无合法目标时明确失败 |
| 接近并朝向目标 | 逻辑位置/朝向、目标、停止距离 | 本 Tick 的移动/朝向值；到达距离后成功 |
| 提交角色请求 | 正式 RequestId、目标快照、输入目录 | 一次带 capture sequence 的请求身份；提交成功不代表技能完成 |
| 等待请求/动作结果 | 自己提交的请求身份、只读正式结果 | 等待、接受并关联 ActionInstance、拒绝/过期、完成/中断 |
| 表达取消或替换意图 | 角色目录中已存在的取消/替换请求能力 | 普通正式请求；角色没有该能力时配置不合法 |

同一次任务激活只提交一次离散请求，Running 更新只查询结果。另一轮合法激活可以产生新请求。输出在每次输入生产开始时按 Character catalog 初始化完整的中性值；合法树未写某个持续值时采用该值的正式中性语义，避免退出追击后残留方向。树执行失败不能以中性输入代替。

BT 停止、条件打断和 Load 可能触发 OnEnd，因此 OnEnd 只处理任务自己的等待/资源，不直接终止角色 Action。作者需要取消时必须通过明确的角色请求表达，最终由唯一 Action 服务裁决。

观察第一层继续从正式已提交 Actor Body/roster 取得。当前禁止读取任意 Gameplay fact 的规则调整为允许明确声明的只读请求/Action 结果投影；投影由正式 Input/Action 状态及其已提交结果所有者产生，不复制一份 Action 生命周期或请求 buffer。结果至少能关联 ActorId、RequestId、capture sequence、所关联的 ActionInstance 和终态；缓冲中、过期、准入拒绝与实际动作完成不能混用。只读观察不包含 Character/技能私有状态地址。

此处不增加 Team/Faction 推断。最小样例使用显式候选 ActorId；中立 Actor 的身份与候选名单不等于已经安装中立怪战斗规则。

### 3. 由正式 Source 批量调度插件

批量准备是现有 Source 输入生命周期的一部分，由 Source 显式声明能力并由 Unity 接入实现。公共 Host/Composer/Ingress 只消费能力合同，不识别 BehaviorTree，不增加 AI 专用 CanonicalInputBatch writer、Logic target 或私有 MonoBehaviour.Update。

每次需要生产新的输入 Tick T：

1. 从本轮开始时最近已提交的世界冻结观察，记录 ObservationTick 和角色名单身份；Local/Authority 使用已提交模拟结果，Rollback 允许该结果属于尚未确认的预测分支。
2. 验证本端拥有输入权的 Actor、行为资源、输入目录和观察能力，准备各 Actor 的输出。
3. 标记这一批行为树需要执行，再统一推进已显式绑定的插件系统组，完成本次任务执行与结果同步。
4. 校验全部待生产输入、应用当前模型已有的请求 timing policy、转换到对应 Numeric Target，冻结本端这一批输入。
5. 正式 Source 向 Local/Authority/Rollback 原有输入装配交付结果，角色 Evaluate 在完整 batch 形成以后开始。

插件使用显式 Manual 更新与受控启动。自动 PlayerLoop 更新和 Source 主动推进不能同时执行同一批 AI；不为每个 Actor 创建 World，也不逐 Actor 更新整组。插件 World/系统组属于 Unity 接入资源，Source 通过注册与生命周期管理它；它不保存第二份 Character Body 或运行第二次 WorldSolver。多 Session 的批次必须按显式注册隔离，不能通过切换全局 World 或静态“当前 Session”选择目标。

本次使用插件原生计时与随机语义，不承诺插件等待秒数等价于 Fixed Tick 计数，也不修改 Wait/Random 实现。时间/随机只影响本次生产出来的输入，后续重演消费已经冻结的结果。会话暂停、恢复和销毁通过插件公开生命周期接入；正式采样记录实际生产/观察 Tick，不把 RenderFrame 当成 SimulationTick。

### 4. 一次性生产记录替换旧 AI 候选状态回滚

这是正式状态合同变更，不是跳过旧 CaptureState 的特例。复用并整理现有 Source 的本地 explicit/pending/history 存储，明确唯一输入事实与消费位置；不再建立平行的 Bot 输入历史或第二请求 buffer。

| 状态 | 唯一所有者 | 恢复规则 |
|---|---|---|
| BT 游标、战术变量、插件计时/随机 | 本端插件运行实例 | 角色回滚不恢复；实例故障后不继续同一 Session |
| 已冻结的本端输入、请求捕获身份、生产 frontier | 正式 Source 的一次性输入事实 | 在同一 Session 内不可改写、不可因角色/外层事务恢复而倒退 |
| 请求 eligible Tick 与有界 pending timing | 当前模型原有输入时序所有者 | 按输入事实和原有时序合同恢复；不能重新捕获同一请求或修改已冻结帧 |
| 发送队列、消费位置、预测/确认推进 | 对应网络 Source/Pass | 按原有网络事务恢复，重发仍引用相同输入身份 |
| Character、Action、技能、Body/World、输出处置 | 既有模拟及模型状态所有者 | 原有 snapshot、hash、restore/replay 和 Commit 保持 |

生产键以 Session、ActorId、输入 Tick 为准，输出保留原有 InputSequence/GameplayHash。重复请求已生产 Tick 时直接读取原帧；不得再次 Tick、增加 request sequence 或替换 payload。一个 Tick 中多 Actor 的插件执行和输入校验全部成功后才冻结该本端批次，不能发送半批新 Bot 决策。

失败边界：

- 插件尚未执行时的装配错误在 Active 前拒绝。
- 插件开始执行后，任一任务异常、非法输出或批次校验失败，使生产者和 Session Faulted；不尝试用自研快照修复插件，也不丢弃错误后重跑同一生成键。
- 完整输入已经冻结，后续模拟事务失败时，模拟状态/输出按原有合同撤销；输入事实保留。原有失败策略若允许重试，只能复用该输入；若要求终止，则正常终止，不能因此新增自动重试。
- 输入已经发送以后，重发、丢包补发和外层事务恢复仍使用原身份。接收端的同 Actor/Tick 内容冲突仍是协议错误。
- Local 只保留当前未完成消费所需的有界生产记录，不新增 Replay history；网络复用模型既有的有界历史和确认清理规则。

这种分责使插件状态无需进入每 Tick 的保存/恢复协议。代价是不能承诺“从任意旧存档恢复后继续生成完全相同的未来 AI 决策”。完整已有输入区间可以作为正式输入回放；缺少后续输入时不能静默启动一棵新树接续历史。

### 5. Rollback 只重演角色，不重算历史 Bot 操作

连接名单和 Actor 名单分开保存。真实 Peer 继续拥有连接、身份认证、endpoint、ready/timeout 与诊断；每个 Actor 具有唯一输入来源。玩家 Actor 绑定真实玩家/Peer，Bot 绑定显式生产 Peer 与行为内容，不拥有伪造的 PlayerId 或连接。

首个正式样例仍启动两个 Unity Peer 和既有纯 .NET Relay/只读 GM；Candidate 显式指定其中一个 Peer 生产两名玩家角色 Bot 与一个中立 Actor 的输入。该选择属于会话配置，不通过谁先连接、对象名或“第一个 Peer”推断。两个真实玩家继续只由各自设备产生输入。

Peer 发送自身拥有的每个 Actor 的连续冗余输入；Relay 按发送 Peer 对该 Actor 的所有权逐帧校验。一个 Actor 的当前帧仍必须满足单 datagram 预算，多 Actor 不通过放大 MTU 或把独立 frame 硬塞进一份超限消息解决。Relay 在收到显式帧后立即转发，最终 canonical bundle 仍等待完整 Actor roster、按 ActorId 排序；ready、连接数量、超时按 Peer 统计，缺失输入/frontier/历史容量按 Actor 统计。

插件输出只在生产端经现有数值转换规则成为正式 Fixed 输入；其它端读取相同编码值。BT 浮点计算、托管对象、Unity 时钟和随机不进入各端确定性计算。Fixed 角色控制、Action/技能、世界求解、状态 hash 和输出处置完整覆盖所有 Actor。

示例：T100 Bot 根据当时的预测观察提交攻击；迟到的玩家 T98 输入表明玩家更早闪开。T98 起角色模拟重演仍消费原来的 Bot T100 攻击，结果可以变为未命中；不能重算 BT 并把 T100 攻击改成追击。后续超过本端生产 frontier 的新输入使用新的已提交观察。角色回滚期间即使 SimulationTick 变小，也不重复生产旧输入。

World snapshot recovery 只恢复模拟状态，不重建或倒退存活的插件实例和输入事实。恢复点到当前生产 frontier 的必需输入超出有界历史时，按既有不可恢复错误结束 Session；不临时补造 Bot 历史。指定生产 Peer 失联继续触发既有 Session 失败语义，本次没有自动接管或主机迁移。

替代方案是让 BT 与世界一起恢复并重决策，它可以更准确响应修正后的历史，但需要维护所有使用节点的状态、随机、计时和回调恢复。本次明确不选择该业务语义。

### 6. Unity Authority 合并玩家与本地 Bot 输入

复用现有 Unity Authority Worker、Authority Source、Pipeline 和 WorldSolver。玩家输入来自已认证客户端路由；Bot 输入由该 Worker 内的插件实例读取权威已提交观察产生。唯一 Authority 输入装配为完整 Actor roster 形成同一 Tick 的输入，随后走现有 Evaluate/ResolveBatch/Finalize/Commit。

连接票据只为真实玩家建立输入权限，Bot 不等待连接、不创建 ticket、不发送 owner 输入 ACK。客户端不能为权威 Bot 提交 command。玩家丢包继续使用现有有界连续值保持和离散请求不重复规则；Bot 执行异常不属于网络丢包，不能套用该策略掩盖失败。

Authority Replication 对完整 Actor roster 产生既有状态/动作/结果。收件人是客户端连接集合；客户端对自己拥有的角色做预测纠正，对其它玩家和 Bot 走既有远端观察/表现。角色状态与动作结果需要什么字段仍由正式模型合同决定，网络不传 BT 游标/黑板，也不逐帧同步骨骼或 Animator 参数。

纯 .NET DotRecast Authority 不支持本次插件 AI capability，启动前明确拒绝这类 Actor binding；其既有无插件业务继续使用原 Host。选择 Unity Worker 的业务代价是 Unity 产品部署与资源闭包，收益是直接复用插件且不再维护 portable AI。拆出 Unity AI 服务再回传 .NET 的方案会增加观察/输入通信和进程维护，本次不建立。

### 7. 内容版本、调试和 DOTS

正式资源目录只保存插件原生行为资产和项目任务配置；构建沿现有资源/Network Product workflow 发布。Session 锁定行为稳定身份、图/子树依赖内容 revision、插件/任务版本、Character input catalog 与输入所有权配置。网络合同只携带这些普通身份/哈希，不携带 Unity 对象或类型句柄。

已安装任务的图连接/参数变化作为内容更新，在新 Session 采用；新增 C# 任务沿现有代码发布/热更链，新 ECS/Burst 实现需要对应编译产物。改变正式输入、动作结果或状态 ABI 时，由其领域合同和产品发布统一升级。本次不扩展 HybridCLR、AOT stripping 或网络热更新基础设施。

正式运行使用启动时锁定的图闭包。插件 Play Mode Live Edit 用于调试，不保证成为原作者资产的正式事务写回；改动内容需重新创建匹配版本的 Session 才形成可对比证据。BTSMTL ScenePlay 继续拥有整体暂停/单步/重建，插件图单步不代替完整 Session 单步。

节点颜色、路径和变量由插件窗口显示。项目诊断只补充生产 Peer/Actor、ObservationTick、InputTick、请求身份、冻结/重发/消费状态与正式动作结果的关联，不把插件节点伪装成 AIProgram operation，也不创建第二套图诊断仓库。

树遍历确实使用 Burst/Jobs；GameObject Task 仍执行普通 C#，存在变量同步与调度成本。本次默认使用能直接调用角色能力的 GameObject Task，只有独立的实际性能需求才考虑 ECS Task。没有本项目 Player 数据前，不声明提帧比例、承载数量或相对旧 AI 的速度结论。

### 8. 删除与职责迁移

| 原内容 | 最终去向 |
|---|---|
| AIControllerDefinition、AIControllerTree、AI RootTree、AI 专用节点/端口值与窗口 | 删除；行为与变量使用插件原生资产/窗口 |
| AI Frontend、AIIntentSemanticIr、AIIntentProgram/Asset、AIControllerState/codec、AI 专用 Operation 与运行诊断 | 删除；不保留空壳、旧 reader、兼容类型或 AIProgram 导出 |
| AIIntentOutputBuilder 内仍被需要的角色输入目录/typed 输入构造 | 移到正式角色输入职责并按实际用途命名，复用一份实现 |
| Committed Actor 观察、候选筛选、稳定目标与请求规则 | 保留业务内容，移出对旧 AI Definition/Program 的依赖 |
| AIController domain、DTO、exporter/reconciler/Mutation/validator 分支、MCP 描述与技能说明 | 删除游戏 AI 领域；保留 Character/技能/Presentation 和独立 Timeline 已正式声明的能力 |
| Corin 训练 AI 的旧图、Definition、generated Program 和引用 | 改成插件行为及正式 Source；旧资产和 meta 清除 |
| TrainingEnemy 旧 AI 资产及其旧组件引用 | 仅清理/迁移 AI 绑定，不迁移 Pose、动画曲线、Projection、Rig 或修复角色本身 |
| 仅服务旧 AI 内核的测试/fixture、诊断与构建引用 | 随删除能力移除；不新增测试代码，不删除技能/网络仍使用的公共基础 |

共享 Runnable/Tree/Timeline/TreeClip、Graph Authoring Framework、Source Map、Character input catalog 等仍被技能或其它领域使用的实现不能按目录整块删除。移除 AI opcode 时保留非 AI operation 的既有显式身份，不因清理重排技能编号；确实受影响的 schema/ABI 再按正式发布规则升级。

### 9. 与 current specs 和并行 change 对账

| 当前约束/位置 | 与本提案的差异 | 本提案的处理 |
|---|---|---|
| btsmtl-ai-controller-authoring：独立 Definition/RootTree、BTSMTL AI 窗口、AI Blackboard | 用户已选择停止维护这套作者框架 | 全部要求移除，旧 spec 能力在安装时退役 |
| gameplay-ai-control-source：AI 必须编译为 portable Program；失败恢复 AI 候选状态 | 与插件直接执行及一次性输入事实冲突 | 替换为外部输入生产/消费分责；插件故障终止，角色回滚复用输入 |
| character-input-pipeline：AI 来源固定 AIIntentProgram；pending 请求恢复 | 来源改为插件；已发布输入不得再由恢复改变 | 保留请求捕获顺序和 timing class，明确 pending 与已冻结事实的边界 |
| gameplay-simulation-session-composition：AI Program/ControllerId/ABI binding | 已删除 Program/Definition | 改锁定行为内容、游戏任务/input catalog、观察、所有权与 Source 能力 |
| btsmtl-graph-core、editor shell、domain framework 的 AI 示例/领域装配 | 插件 AI 不再复用 BTSMTL 图框架 | 删除 AI 注册与承诺，保留其它领域的共享行为 |
| Agent Document/MCP/Character synthesis 的 CharacterController 与 AIController 两域及 AI 事务内 Build | AI 正文和 AIProgram 发布不再存在 | 从唯一协议中移除 AI domain；其它领域继续整包事务、精确 root、显式 Build |
| Rollback one Peer/Player/Actor 与完整输入确认 | 一个真实端点将控制多个 Actor | 分离名单和所有权；确认仍按完整 Actor 集合，不改变 Relay-only |
| Rollback world snapshot/history 包含所有未来模拟状态 | 插件是已明确外部输入生产者 | 保留全部 Fixed 模拟状态；外部决策、生产 frontier 不进入世界 hash/restore |
| two-client demo 的双 Actor SolidBodyBlock 描述 | 样例扩展到两玩家、两 Bot 和一中立 Actor | 复用现有多 Actor batch/碰撞能力，更新样例覆盖；不改 KCC 算法 |
| Authority 所有 Actor 依赖客户端 accepted input 和 owner route | Bot 没有客户端连接 | 正式合并本地/远端输入并按真实连接分发，保持一个 Source/Endpoint/Pipeline |
| Network Product/Candidate 的 roster 与资源闭包 | 新增行为资源与一端多 Actor 归属 | 由模型 adapter 发布精确身份，公共 workflow 不识别插件/模型具体类型 |
| project.md 与 2v2vE 文档的 AIProgram、两域、Authority AI 尚未安装表述 | 本次接入完成后不再真实 | 在实施收口时按实际能力更新；完整命中/伤害和 Team/Faction 仍不得宣称完成 |

并行变更的合并规则：

- `refactor-btsmtl-authoring-architecture`：其 design 中保留 AI RootTree/AIIntentProgram、Document 两域以及 task 5.6 由本提案的删除目标替代；技能/C# 控制/Action/状态布局仍归原变更。本次不要求整份主重构完成才开展独立工作，但接线必须使用实际已形成的正式输入与动作合同，不能镜像另一套接口。
- Document 继续对齐主重构正在形成的唯一 v5 目标；本变更移除该目标中的 AIController domain，不另起插件 schema。当前 installed v4 与目标 v5 的迁移仍只有一条正式发布链，旧 AI package 不被转换为插件图。其它非 AI domain 的新增只来自其正式变更，不能借删除 AI 顺带撤销。
- `decouple-timeline-from-skill`：保留其独立 Timeline 目标和共享 TreeClip/编译/执行提取。插件 AI 不直接调用技能/非技能 Timeline，不决定独立 Timeline 的生命周期；涉及 Document 的共同文件合并为“已有非 AI domains + 该变更正式 Timeline domain”。
- `rebuild-btsmtl-preview-with-scene-play`：场景预览继续拥有正式 Session 控制；只替换旧 AI 作者/诊断引用，不恢复窗口播放器或插件图直接改角色的预览路径。
- Pose、Foot、Camera、Performance、Development Center 等变更只复用其已声明合同。本次不覆盖其未提交修改、不修复无关 baseline、不发送自动实施指令。

主重构精确交接清单：

| 主重构文件/条款 | 本提案负责替换的部分 | 原 owner 保留的部分 |
|---|---|---|
| `design.md:69`、`tasks.md:42` 的 5.6 | 保留 AI RootTree/AIIntentProgram 的目标改为插件 AI 输入接入；不再新增旧 AI 功能 | CharacterSimulationInput、角色目录和对现存调用者必要的可编译适配 |
| `design.md:190`、`:192` | AIController domain、AI body 的持续维护目标删除 | 技能/控制配置 v5 基础、Presentation 所有权、唯一 schema 发布 |
| `specs/btsmtl-ai-controller-authoring/spec.md` 的 AI 窗口、AI Intent 绑定 | 旧窗口/节点要求移除，游戏任务接入归本提案 | 稳定 InputId/RequestId、value kind、TimingClass 与角色输入目录 |
| `specs/agent-ai-controller-synthesis/spec.md` | v5 AI Definition/Graph/Blackboard/Perception/Intent 合同移除 | 其它领域的共享对账/事务/校验基础 |
| `specs/btsmtl-agent-authoring-document-sync/spec.md` 的目录包根、v5 原子替代条款 | 删除 AI 根和“两旧 Controller 域都必须保留”的承诺 | 单一 v5 迁移，保留技能与独立 Timeline 正式目标 |
| `specs/btsmtl-agent-authoring-mcp-bridge/spec.md` 的 AI 事务 | 删除 AI 正文/路由，不新增插件 domain | 五个生命周期工具及精确 root/hash/整包事务 |
| `specs/agent-character-controller-synthesis/spec.md` 的声明式控制器结构 | 删除 AI editable 正文和旧 AI handler 依赖 | C# 控制配置、技能局部结构、Timeline/Presentation 的正式作者合同 |

Input/Action 的共同输出仍是 ActorId、RequestId、capture sequence 到排队/过期/拒绝、实际 ActionInstance 与完成/中断的只读关联；本提案负责尚缺的观察接入，不要求主重构先为旧 AI 实现一套。插件不读取 Action 私有地址。共享的角色输入目录和 Tree/Timeline 编译基础不得随旧 AI 一并删除。

Document 的规范安装必须与唯一 schema owner 合并 v5 基础及 Timeline 增量，不能单独安装本提案的局部 v5 条款而留下 current spec 中的 v4-only 要求，也不能重复应用已完成的 requirement rename。合并依据实际已安装条款，最终删除 AI、保留其余正式领域；这项文档发布顺序不要求其它无关主重构任务全部完成。

2026-09-05 已按主规划 owner 的明确要求，将以上接口交接发送给主实现任务 `01a06b30-aa8c-7cf3-8e05-cedfbfbbee2f`，限定为规划协调，不授权本提案实施或删除代码。Timeline 的 AIController 保留文字由其规划 owner 对齐。本轮不修改这些 current/active 文件；上表指出的是安装本提案时必须合并的语义，不把仍然存在的旧文本当作已完成迁移。

## Risks / Trade-offs

- [Bot 使用预测观察，历史决定可能变得不合时宜] → 将输入不可改写写入玩法口径；诊断保留观察 Tick，下一次新决策读取新的已提交结果。
- [插件中途失败后无法精确撤销隐藏状态] → 生产者/Session 明确 Faulted；不恢复后偷偷继续、不用中性值掩盖错误。
- [外层事务恢复可能错误回退生产序号或重复发攻击] → Source 明确区分不可倒退的输入事实与可恢复消费状态，并检查原 Tick/序号/GameplayHash 重用。
- [一端多 Actor 改动遗漏握手、权限或容量] → 名单、输入认证、ready/timeout、frontier、确认、GM 只读投影和 Candidate identity 同步迁移；不增加伪 Peer。
- [对局依赖指定的 Bot 生产端] → 生产端在会话开始前明确锁定；失联沿现有 Session 终止语义，本次不承诺接管。
- [插件 DOTS 调度开销高于少量敌人的遍历收益] → GameObject Task 保持窄调用，批量推进；普通接入只报告功能证据，性能结论由独立实际采样给出。
- [插件更新或图数据变化破坏运行绑定] → 使用精确内容/任务版本与新 Session 采用边界，旧运行内容不能混用新图或新节点程序集。
- [删除 AI 作者链误伤技能/Timeline/Agent 编辑] → 按调用者和能力归属清理，保留共享基础；全部剩余 domain 做既有 Validator/编译引用核对。

## Migration Plan

1. 从实际工作区建立受影响清单，确认共享输入、动作结果、Document 与两个网络模型的接口及上述冲突；不覆盖并行任务改动。
2. 实现插件隔离、游戏任务和正式输入生产/消费合同，并在稳定 Corin 的 Local Float32/Fixed 入口形成完整行为链。
3. 将旧 AI 资产、组件引用、Program/State/编译器、作者 domain 和诊断一次迁入新正式链，删除旧实现和仅供旧能力使用的数据。保留与技能/Timeline/表现共享的实现。
4. 迁移网络 Peer/Actor 所有权、Source 输入记录与两个模型的正式输入装配，发布锁定新身份的配置；旧协议/配置不建立兼容读取。
5. 在 Unity Authority 与两个 Peer 的 Rollback Candidate 中交付明确 Bot 所有权及同角色样例；更新只读诊断、资源闭包与文档。
6. 通过现有编译、配置/架构 Validator、可追溯输入回放与正式网络运行证据核对结果。已有证据未覆盖的行为如实列明，不编写新增测试，不把手动操作列入 tasks。

每个职责闭合形成中文小步提交。若需撤回实现，回退本变更自己的提交与配套资产/协议版本，并从对应版本重新创建 Session；不回退用户或其它任务的修改，不保留同时可运行的旧 AI 配置作为撤回机制。
