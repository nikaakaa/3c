## MODIFIED Requirements

### Requirement: Rollback Model 必须严格校验 Deterministic Capability

Model MUST在创建前校验SemanticHash、Fixed GameplayContentHash、Fixed ABI、角色玩法deterministic capability、TickRate、CollisionWorldHash、KccId／capabilities、protocol version和actor roster规则。任一项不满足 MUST拒绝创建，MUST不跳过operation、加载Float32领域运行或回退其他Solver／Model。

确定性要求 MUST覆盖进入Fixed角色／世界执行的全部状态和操作。已明确位于模型输入生产边界之外的插件决策 MUST不被当作Fixed operation，也不能借该身份直接修改模拟；它只能交付已经转换成正式Fixed数值的输入。会话兼容身份 MUST同时锁定Actor输入所有权和输入格式。

#### Scenario: Program 包含 Nondeterministic Operation

- **WHEN** Fixed领域运行capability manifest不满足deterministic-compatible
- **THEN** Rollback model option MUST不可创建

#### Scenario: Graph包含Nondeterministic Operation

- **WHEN** Fixed Graph／domain capability manifest不满足deterministic-compatible
- **THEN** Rollback model option MUST不可创建

#### Scenario: 插件在指定端产生Fixed输入

- **WHEN** 已声明的 Bot 生产端将本次决策转换为正式 Fixed 输入
- **THEN** 其它端 MUST消费相同编码的输入并执行确定性角色模拟
- **AND** MUST不要求其它端运行插件或在Fixed领域运行中装载插件任务

### Requirement: Gameplay 输入必须沿单一 Raw-to-Canonical 生命周期传播

Endpoint/Dedicated Relay Server MUST将每个输入事实按`ActorId + SimulationTick + InputSequence + GameplayHash`唯一识别，并沿`Local Explicit -> Relayed Explicit -> Canonical -> Confirmed`单向晋升。Peer MUST将固定Tick冗余编码为连续`ActorInputBatch`；配置的冗余帧数 MUST是历史上限，发送端 MUST从当前Tick向前选择可完整放入单个unreliable datagram的最大连续后缀。当前Tick单帧仍超过payload预算时 MUST明确失败，MUST不调大MTU、分片unreliable input或静默丢字段。Relay Server MUST校验发送Peer与Actor所有权、去重同一输入身份，并在接收后立即向其它Peer转发Relayed Explicit frame，同时把同一frame提交给canonical assembler。立即转发 MUST不等待同Tick其它Actor、canonical lead或confirmation delay。Canonical assembler MUST只在`NextCanonicalTick`的完整roster显式输入齐备后，按stable ActorId顺序生成不可变canonical bundle。Canonical bundle MUST是最终Gameplay排序的唯一bundle表示，但 MUST不再承担原始输入首次投递。相同GameplayHash的阶段晋升 MUST不触发replay；同一Actor/Tick/Sequence出现不同GameplayHash MUST视为协议冲突。Program/Kernel MUST不读取endpoint packet。

Peer/连接身份与 Actor 输入所有权 MUST分别锁定；真实 Peer 可以拥有玩家 Actor 和多个 Bot Actor 的输入权。Relay MUST逐 Actor 验证发送权，ready/timeout 按真实 Peer 统计，缺失输入、frontier、容量和 canonical 完整性按 Actor 统计。每个 Actor 的帧保持原有独立连续冗余、单帧 payload 上限和不可改写身份，不为 Bot 伪造连接或 PlayerId。

#### Scenario: Relay Server收到一个合法Actor Input Batch

- **WHEN** Peer A提交其Actor的连续冗余输入批次
- **THEN** Relay Server MUST先校验和去重frame，再立即向Peer B转发Relayed Explicit input
- **AND** MUST不等待Peer B同Tick输入或canonical bundle生成

#### Scenario: 配置的输入冗余超过单包预算

- **WHEN** 当前Tick加全部历史冗余无法装入一个unreliable datagram
- **THEN** Peer MUST发送包含当前Tick的最大连续历史后缀
- **AND** MUST不发送超过预算的数据报或丢弃当前Tick

#### Scenario: 当前 Tick 的完整 roster 输入齐备

- **WHEN** canonical assembler已经持有NextCanonicalTick的全部Actor显式输入
- **THEN** MUST按stable ActorId生成一个不可变canonical bundle
- **AND** 后续相同GameplayHash的冗余输入 MUST不产生普通revision

#### Scenario: Canonical 只提升输入阶段

- **WHEN** Peer已经用Relayed Explicit frame执行Tick T且后续canonical bundle包含相同GameplayHash
- **THEN** Source MUST只推进canonical provenance/frontier
- **AND** MUST不产生restore、replay或表现分支替换

#### Scenario: 同一输入身份内容冲突

- **WHEN** Relay Server收到同一Actor、Tick和Sequence但GameplayHash不同的frame
- **THEN** MUST报告协议冲突并结束该Session
- **AND** MUST不选择任一版本继续模拟

#### Scenario: 同一渲染帧采集相机相对移动

- **WHEN** Unity Peer在RenderFrame采集camera-relative Vector2输入并在后续SimulationTick构造ActorInputBatch
- **THEN** 移动方向 MUST使用该RenderFrame锁存的CameraBasisSnapshot转换
- **AND** 输入值与可选CameraBasis字段 MUST来自同一次采样
- **AND** Program未声明CameraBasis输入时 MUST不把basis字段加入网络payload

#### Scenario: 一个Peer提交玩家和两个Bot输入

- **WHEN** 该真实 Peer 拥有三个 Actor 的输入权并提交各自合法输入批次
- **THEN** Relay MUST逐 Actor 验证并转发，canonical 仍等待完整 Actor roster
- **AND** MUST不等待三个不同连接或复用同一 Actor 的序号

#### Scenario: 非所有者提交Bot输入

- **WHEN** Peer 提交未授予自己输入权的 Bot Actor 帧
- **THEN** Relay MUST拒绝该帧且不写入 canonical assembler
- **AND** MUST不依据角色类型、队伍或行为名称放宽权限

### Requirement: Rollback History 必须保存完整 Fixed SimulationWorldSnapshot

Rollback History Pass MUST保存有界 canonical input history与Fixed world snapshot history。Fixed Target MUST复用typed state schema和`Begin -> Evaluate -> Finalize -> Commit|Abort`事务生命周期形状，但 MUST实现自己的Fixed partition、numeric value、canonical codec与transaction specialization。World snapshot MUST包含SimulationTick、Fixed Program/Layout/codec identity、stable actor table、所有Actor committed SimulationState canonical bytes、Deterministic KCC actor/world state、模拟内RNG、Event/Command cursor和模型必要状态，MUST不保存active transaction、mutable typed partition或Float32 State/Snapshot，也 MUST不新增平行总世界状态aggregate。

Peer的predicted completed frontier MUST不超过本地canonical contiguous frontier加`MaximumPredictionLeadTicks`。达到上限时Ingress MAY继续接收canonical并重发同一待执行Tick输入，但Schedule MUST不新增predicted history；canonical差异触发的restore/replay仍 MUST执行。`MaximumRollbackDepthTicks` MUST只用于restore/replay深度、history保护和deep recovery判定。

完整世界 snapshot MUST覆盖玩家、Bot 和中立 Actor 的全部 Fixed 模拟状态。外部插件游标、战术变量、随机/时钟以及不可倒退的本端输入生产事实 MUST不进入该 snapshot/hash；必要输入仍由唯一模型输入历史保存。恢复消费状态 MUST不回退生产 frontier、改写已冻结的本端帧或重新捕获请求。

#### Scenario: Capture Tick T

- **WHEN** History Pass保存 Tick T snapshot
- **THEN** MUST原子 capture 全部 Actor 和 KCC/world state
- **AND** MUST不只保存 Transform 或单个 Actor

#### Scenario: 快 Peer 达到最大预测领先

- **WHEN** 下一个predicted Tick会超过canonical contiguous frontier加MaximumPredictionLeadTicks
- **THEN** Schedule MUST返回NoStep并等待canonical推进
- **AND** input history MUST不因两个进程运行速度不同而无限增长

#### Scenario: Bot已发布输入后发生角色回滚

- **WHEN** 世界恢复到 Bot 已发布输入之前的模拟 Tick
- **THEN** 角色、技能和世界 MUST按 snapshot 恢复
- **AND** 原 Bot 输入及其身份 MUST保留供重演使用，插件实例 MUST不倒退

### Requirement: Late Input 必须触发原子 Restore 与 Replay

当Relayed Explicit或canonical input的GameplayHash改变已经执行的Tick T时，Rollback Schedule Pass MUST产生恢复T前最近完整world snapshot的restore directive，并按Tick和stable ActorId order产生replay/current steps；同一outer transaction内多个晚到输入 MUST合并到最早受影响Tick。Deterministic Backend MUST在同一outer transaction使用同一Fixed Program、Fixed Kernel和Deterministic KCC执行全部步骤。只改变provenance而GameplayHash不变的输入 MUST不触发restore/replay。

Replay MUST只消费模型中已有的正式输入，不得再次执行插件生成历史 Bot 决策。Bot 生产时读取的预测观察可被后续迟到输入纠正，但已发布 Bot 输入仍不可改写；新的生产 Tick 再读取当时最近已提交的观察。

#### Scenario: Tick T 的 Attack Request 迟到

- **WHEN** Tick T已使用空request预测且后续Relayed Explicit input包含Attack request
- **THEN** Rollback Pipeline MUST恢复完整world并重演T到当前Tick
- **AND** MUST在该outer transaction结束后只发布最终Body与动画分支

#### Scenario: Canonical 内容与 Relayed Explicit 相同

- **WHEN** Tick T的canonical GameplayHash与已应用Relayed Explicit input一致
- **THEN** MUST只推进canonical frontier
- **AND** MUST不增加rollback count

#### Scenario: 玩家迟到闪避使Bot攻击落空

- **WHEN** Bot 在 T100 发布攻击，而随后玩家 T98 显式输入纠正了当时位置
- **THEN** 两端 MUST重演原 Bot T100 攻击并由正式角色/世界规则产生新结果
- **AND** MUST不重新执行 BT 将 T100 攻击改成追击

#### Scenario: 预测上限导致同一Tick停留

- **WHEN** 当前输入已生产但 Schedule 因最大预测领先量返回 NoStep
- **THEN** Source MUST仅继续收包和重发原帧
- **AND** MUST不重复推进 BT 或增加原请求的捕获序号

### Requirement: 严重 Desync 必须通过正式 World Snapshot 恢复

若 history不足、replay后 hash仍不同或 roster/world发生无法自愈的差异，Rollback Source MUST向模型指定 snapshot authority请求完整 Fixed `SimulationWorldSnapshot`，Schedule Pass校验并生成正式 restore directive，校验后原子恢复到唯一 Fixed `SimulationWorldStateSet`。MUST不改用 ServerAuthoritative correction或 Transform teleport。

World snapshot recovery MUST不恢复或重新创建存活的 Bot 决策实例。恢复后到本端生产 frontier 所需的输入 MUST由正式有界历史提供；超出可恢复输入范围时 MUST按模型不可恢复错误终止会话，不能补造过去的 Bot 输入。Bot 输入生产 Peer 与 snapshot authority MUST分别明确声明，不能默认互相替代。

#### Scenario: Late Input 早于 History Floor

- **WHEN** 受影响 Tick 已不在本地 snapshot history
- **THEN** Rollback Source MUST请求正式 world snapshot

#### Scenario: 恢复点缺少必需的已生产Bot历史

- **WHEN** 完整世界恢复需要的某段 Bot 输入已不在正式有界历史
- **THEN** 模型 MUST明确报告恢复所需输入缺失并结束该会话
- **AND** MUST不启动新行为树猜测历史操作

## ADDED Requirements

### Requirement: Bot输入生产者必须由会话显式指定且不在重演中迁移

会话 MUST为每个 Bot 锁定一个真实 Unity Peer 作为输入生产者。生产者 MUST只为新的输入 Tick 推进插件并冻结该 Actor 输入；其它 Peer 只接收输入，Relay 只做网络处理。会话 MUST不支持通过连接先后选择生产者、生产端失联后静默接管或从 world snapshot 重建插件状态；生产端失联按既有会话终止规则处理。

#### Scenario: Bot生产端失联

- **WHEN** 已锁定的 Bot 生产 Peer 离开或达到连接失效条件
- **THEN** 会话 MUST按既有失败语义终止
- **AND** 其它 Peer MUST不开始运行同一 Bot 行为来继续原会话

### Requirement: 多Actor请求时序必须保持角色原有业务规则

Bot 请求 MUST与玩家请求使用相同正式 Timing Class 和模型 eligible Tick 规则，按每 Actor 的原捕获序号保持顺序。生产、pending timing 与已冻结帧 MUST具有明确恢复边界，MUST不在远端再次延迟同一已排期请求，不因多个 Actor 共用 Peer 而合并捕获序号或阻塞其它 Actor 的持续输入。

#### Scenario: Bot提交Offensive请求

- **WHEN** 正式示例的 Bot 在 T 捕获 Offensive 请求且模型配置延迟为两 Tick
- **THEN** 请求 MUST保留原身份并按相同规则在 eligible Tick 进入 Fixed 输入
- **AND** 远端重演 MUST消费收到的正式请求而不再次增加两 Tick
