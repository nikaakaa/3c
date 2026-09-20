# Timeline 秒制作者时间、时钟域与表现采样设计

## Context

本次根据用户决定更新已有变更：内容用秒记录；既有播放管理者决定游标进度与速率；Timeline 接收推进结果被动求值，不自己维护两个独立计时器。逻辑 tick 与表现帧是两种调用场合，执行权限不同；编辑器可按正式配置的逻辑 tick 率自适应吸附。本文只更新设计，代码仍为下表所述现状。

### 当前代码事实

下列路径以 3cDemo/Client/3C_Client/Assets/GameScripts/Main 为根：

| 链路 | 当前状态 | 缺口 |
|---|---|---|
| Runtime/BTSMTL/Timeline/Runtime/TimelineRuntimePreparation.cs | Logic 保存整数作者帧游标和换算余数，参与快照 | 逻辑快照恢复与表现分支更新的交付仍需接通 |
| Runtime/BTSMTL/Timeline/Runtime/TimelineRuntimeComposition.cs | Presentation driver 独立执行 cursor + deltaSeconds × frameRate | InterpolationAlpha 没有用来派生该游标；同一动作的 Pose 可能使用另一套进度 |
| Runtime/Character/Pipeline/Animation/Contracts/Action/ActionPresentationClockPolicy.cs | 已有 FreeRun、CommittedMovement、CommittedFollow；DriveClock 直接操作动画播放器 | 时间计算没有成为 Timeline Marker、Camera 和动作动画共同消费的结果 |
| Runtime/Character/Pipeline/Animation/Lifecycle/CharacterTimelineHost.cs | Present 产生表现 Marker 后调用 InvokePresentationMarkers | 仍依赖 m_ActiveTreeClipInvoker；该 invoker 只在逻辑 Tick 的 Push / Pop 范围内有效 |
| Runtime/Simulation/Core/Fixed/Execution/FixedAbilityOperationControlRuntime.cs 及 Float32 对应实现 | 在逻辑求值范围注入并释放技能图 invoker | 不能靠永久保存此引用让 Presentation 再次进入逻辑 evaluator |
| TimelineRuntimeComposition.cs 的 CommitStop / TryPresent | CommitStop 写入 StopCommitted，已有表现状态的推进没有读取此标记 | 已停止实例仍可能继续采样、跨 Marker，循环内容尤需收口 |
| Slate / Timeline Inspector / C# authoring | 已有 Marker 创建、图角色、拖动坐标修复及导出字段；现有 Track Inspector 缺少 Domain 编辑 | Marker 与 Clip 最终仍保存整数帧；解除部分 UI 吸附不等于亚帧支持，导出入口存在也不等于完整私有图重建闭环 |

Marker 创建、选中、拖动、独占 OnEnable 图角色等已有改动属于作者链成果。编译通过不能证明表现图可执行、停止可生效或端到端已经完成。tasks.md 的完成状态按此边界修正。

### 三个必须分开的概念

| 概念 | 回答的问题 | 当前口径 |
|---|---|---|
| 作者单位 | 事件写在内容的什么位置 | 目标统一为秒；帧只用于显示、吸附和素材来源定位 |
| 更新域 | 谁在什么时候运行，允许改变什么 | Logic 在 SimulationTick 提交；Presentation 在表现帧采样 |
| 表现进度策略 | 这次应该采样动作的哪个位置 | 正式业务装配选择，并服从该业务的现行 sample / lifecycle 合同 |

现有代码保存了 Logic 与 Presentation 两个进度；目标不是给 Timeline 再装两种自主时钟，而是明确进度计算和内容求值的边界。秒制内容只有一份，逻辑位置与表现位置由各自正式播放管理者提供；Timeline 只处理调用所声明的区间和域。正文的“双域”不替代 LocalLogicTick、ServerTick、source phase 等已有时间身份。

## Goals / Non-Goals

**Goals:**

- 同一动作播放实例只有一份本帧表现采样结果；进度来源可替换，消费者不再各自累加。
- 播放管理者负责进度、倍率和暂停；Timeline 负责区间遍历、内容采样及生命周期候选，不自行读取时间源推进。
- 作者只保存一份秒制内容，适配运行 tick 率而不重写内容；编辑器帧显示不限制保存精度。
- 业务通过正式装配选择进度来源、暂停和修正行为，Timeline / Clip 不认识网络模型或游戏类型。
- Marker 从作者数据、图闭包、域能力、正式运行、事件调和到停止有完整归属。
- 明确代码已有能力与待实施能力，保留原 Logic 事务、Pose 帧事务与领域 owner。

**Non-Goals:**

- 本轮只改文档，不改代码和资产、不归档；秒制存储迁移纳入后续实施范围，不为所有业务选一个全局默认策略。
- 不改 Body 插值、locomotion plan、逻辑 Motion / Warp、网络调度或确认边界。
- 不为尚无正式 domain 的 VFX / Audio 建临时事件系统，不实现持续表现 TreeClip 图语言。
- 不把角色全部动画、混合权重、独立音效尾部都合并成一个游标。

## Decisions

### 1. 作者内容统一用秒，运行保留 tick 身份

用户已确定秒制作者时间。Timeline 起点、持续时间、Marker、Section、ClipIn、循环边界及 Timeline 自有曲线的时间坐标统一按秒表达和保存；归一化参数、播放倍率和外部源曲线的原生坐标仍按其正式含义映射。作者资产只有一份时间，不维护可写的秒 / 帧 / tick 三套数据，不在加载时生成 tick 版 Timeline 操作表。

秒是单位，不等于必须用 float 保存。逻辑边界需要精确比较、稳定排序和可恢复的推进状态。下面是同一秒制模型的数值表示选择，不是两套运行模式：

| 数值表示 | 业务收益 | 业务代价 |
|---|---|---|
| 固定精度秒，底层整数保存 | 存储、比较与快照简单，作者仍输入秒 | 必须明确精度、范围和舍入，部分素材帧时间不能精确表示 |
| 有理数秒 | 可精确表达不同素材帧率的事件时间 | 比较、归一化、溢出处理和序列化更复杂 |

实施决定：采用与现有 `FixedScalar` 一致的有符号 Q32.32 秒表示，序列化保存一个 64 位 raw 值，作者 API 和 UI 输入输出仍为秒；不引入另一套有理数算术引擎。时间最小单位为 `2^-32` 秒，正向内容范围为 0 至 `long.MaxValue / 2^32` 秒；非法负内容位置、非有限输入和溢出明确失败。输入与素材帧比值转换使用现有 Fixed 数值合同的 nearest-even 规则，精确比较直接比较 raw。量化误差不超过半个最小单位；旧 60Hz 内容也按同一规则一次性转换，不把误差累计到每一步。

逻辑播放管理者保存整数累计量与对 tick 率的换算余数，从完整累计位置按 nearest-even 得到本步秒制端点；不能先把 `1/R` 秒舍入再反复累加。动作速率沿现有 Fixed 倍率合同表示；变速只改变后续累计增量，既有余数保留，暂停不累加。时间、速率、余数与控制生效 Step 同属正式快照。帧／tick 网格由 n/R 一次换算，连续移动网格不累计逐格舍入误差。Float32 与 Fixed Timeline 内容使用相同秒制位置与排序，领域数值结果仍保持各自数值目标；最终源动画采样仅在播放器边界转换为浮点秒。

业务取舍：Q32.32 复用现有数值精度和舍入规则，以约 0.233 纳秒的有限精度换取无托管分配的固定大小存储与比较；不承诺任意素材帧率的时间在数学上完全无误差，也不引入分母增长、动态大整数或第二套精度配置。上述数值决定完成任务 0.1，字段和运行迁移仍由后续任务实施。

逻辑 tick 率仍来自正式 pipeline 配置。每次 SimulationTick 由既有逻辑播放管理者以固定步长和正式速率控制计算动作推进，再把前后秒数、经过信息和原因交给 Timeline。精确进度、倍率、暂停与必要换算余数属于播放管理者的确定性状态；Timeline 保留活动 Clip、已接受边界、循环遍历及去重等求值状态。两者沿同一正式 Step 提交、丢弃和快照恢复，不各保一份可独立推进的权威游标。暂停与变速改变实际到达 tick，不修改事件作者秒数。

例如 Marker 在 0.25 秒、正常速率且从 0 开始播放：120Hz 在第 30 tick 到达；30Hz 在第 8 tick 跨过。两种配置读取相同内容，实际处理时点受逻辑步长约束。确定性承诺限于相同配置、输入和初始状态下重复执行，不保证不同 tick 率下碰撞、输入响应和物理结果完全相同。

正常前进按 `(previous, current]` 遍历点事件，0 秒起点由正式开始经过处理一次；循环按尾段、完整循环、头段遍历，循环身份区别重复经过。同时间事件复用正式稳定排序，Clip 的 Enter / Exit / Decision 生命周期保持原规则。Seek 和修正不走正常跨点补发规则。短窗口的进入与退出均需交给原战斗领域；发出两个边界不等于窗口内已完成碰撞采样，不靠表现事件补做逻辑命中。

编辑器提供逻辑 tick 吸附、素材帧吸附与关闭吸附：逻辑网格从当前明确绑定的 pipeline 读取 SimulationTickRate，位置为 `n / R` 秒；素材网格读取正式素材帧率和源映射。二者都转换为同一正式秒制位置，不限制底层精度。共享 Timeline 必须显示当前参考的 pipeline；没有绑定时逻辑吸附不可用，不能默认猜一个 60Hz 配置。

改变配置只更新网格和参考读数，不移动已保存事件。重新对齐必须是作者显式操作，仅修改选中范围并支持一次完整 Undo。逻辑吸附表示正常速率、动作起点对齐逻辑边界时的参考，不保证变速或暂停后仍在原 tick 生效。旧资产按已知旧 60 作者帧基准迁移，源素材帧号按正式源映射处理，不能对一基帧号盲目除以 60。

### 1.1 播放控制与 Timeline 被动求值

| 职责 | 输入 | 处理与输出 |
|---|---|---|
| 既有逻辑播放管理者 | 固定逻辑步、已接受播放控制、当前播放状态 | 决定本步前后动作秒数、播放身份与推进原因，产生待提交进度 |
| 既有表现播放管理者 | committed samples、正式表现策略与控制 | 每表现帧计算一次前后动作秒数、经过信息与事件资格 |
| Timeline 求值 | 同一秒制内容、播放身份、域、前后位置、循环/分段经过、推进原因与事件资格 | 遍历边界、映射 Clip 源采样、产生领域结果和生命周期候选 |
| 正式领域消费者 | 已接受的 Timeline 结果与共享动作采样 | 执行动画、相机、战斗等各自业务，不再次推进同一动作时间 |

`previous → current` 必须包含正常前进、暂停、Seek、修正或终态的原因；循环或 Section 跳转还必须带完整经过信息，不能只给两个取模位置丢失中间循环。Timeline 可以依据内容处理循环边界和返回 Decision / 阻塞 / 完成候选，但最终采用的位置与控制由原播放管理者在同一事务接受。被内容边界截停时不能仍提交候选区间的远端位置，Discard 也不能留下进度已走、事件未交付的状态。

Timeline 不读取 Unity delta、自行累计 wall time 或注册独立 Update；也不解释“子弹时间”并再次计算倍率。被动不等于无状态，已有边界生命周期、活动调用与去重状态仍归原运行实例。技能与真实非 Skill 调用方均复用这条接口，不新建播放器、影子图或第二 Registry。推进结果与求值结果复用既有预分配存储，逐 tick、逐表现帧和事件热路径不得产生托管 GC 分配。

### 1.2 子弹时间与动画倍率

业务子弹时间通过正式播放控制改变指定角色／动作的有效推进速率，不修改 Unity `Time.timeScale`、`Time.fixedDeltaTime` 或为此改变 SimulationTickRate。假设本步正常动作增量为 0.02 秒，0.1 倍速时播放管理者传给 Timeline 的区间是 `0.20 → 0.202` 秒；暂停时为 `0.20 → 0.20`。Timeline 按区间求值，不需要知道造成该区间的业务名称。

| 作者或业务意图 | 修改位置 | 业务取舍 |
|---|---|---|
| 整个动作加速、减速或 hitstop | 正式动作进度控制 | 窗口、动作动画、动作 Marker 与随动作采样的相机共同快慢；影响实际战斗节奏 |
| 仅调整某个 Clip 的素材播放速度 | Clip 源时间映射 | 可调整视觉与素材覆盖，不自动改变攻击窗口或动作总进度；作者需要保证视觉与命中仍一致 |

有效动作倍率在播放管理者计算推进时应用一次；跟随 committed sample 的表现消费者直接使用已变速的动作位置，不再乘同一倍率。若多个业务倍率叠加，其组合、作用范围、生效 Step 和恢复规则必须由正式播放控制明确，Timeline 不临时组合。逻辑倍率、暂停和恢复控制进入原确定性输入／状态及快照链；暂停到期不能依赖已冻结的动作游标自行抵达结束点，释放条件由业务控制的正式时间来源决定。

UI、独立效果与非随动作相机保留自身正式时间策略，不强制跟随动作。角色位移、投射物或其它世界行为如需变慢，由对应正式领域接收控制，不能把 Timeline 减速声称为整个世界子弹时间已完成。本 change 不建立全局慢动作服务。

现有 GameplayTickSettings 默认 Scaled，外部修改 Unity 全局时间仍可能影响调度，这是当前状态，不是本方案的子弹时间入口。这里不擅自把全项目设置改成 Unscaled；若要隔离所有外部全局时间影响，需要另行明确正式调度配置范围。Slate 的全局 timeScale 运行片段不得作为本方案实现路径。

### 2. 保留两个更新域，统一每个动作的表现采样

统一范围是同一 playback identity / generation 的动作实例。由既有动作表现播放管理者保管策略状态，每个 PresentationFrame 计算一次结果，再交给 Timeline 被动表现求值和动作动画、Camera 采样；Timeline 内部不拥有独立表现速率积分器。

输入为正式播放身份、已提交的开始 / 暂停 / 速率 / 终止控制、可用 committed samples、当前表现 delta 及准备好的业务策略。输出至少表达前后动作秒数、循环经过、变化原因、是否允许产生新的跨点事件。这里描述业务含义，不另定一套与现有类型同义的公开接口。跟随策略从 committed samples 推导位置，不要求另外维护一个自由累加器；自主推进只用于已明确允许该策略的内容。

动画 Clip 用同一动作位置加自己的起点、ClipIn、源速率映射为源采样时间。多个 Clip 的源时间可以不同，重叠混合的权重仍由原 Pose owner 推进。locomotion 和已独立生成的特效继续使用各自正式生命周期，不纳入动作游标。

取舍：统一结果避免画面在 0.20 秒而相机 / Marker 已按另一个游标走到 0.25 秒；代价是必须把现有直接改播放器的策略拆开。保留消费者独立累加能更自由，但每个暂停、速率变化和修正都要多方协调，不适合共享动作时间线的内容。

### 3. 业务选择进度来源，播放管理者执行策略

“中断后紧跟规则还是平滑接管”是业务要求。单机也会有打断和 hitstop；是否需要网络修正与这些本地控制分别决定。MMO / ACT 只是使用场景，不能成为基础层 if 分支。

| 表现策略 | 适合的业务要求 | 业务取舍 |
|---|---|---|
| 跟随 committed sample | 攻击画面、关键动作事件、回放需要与已提交动作阶段一致 | 依赖有效样本；插值时序必须服从现有 horizon，不能为连续画面擅自预测未来事件 |
| 按表现 delta 自由推进 | 独立展示、装饰播放或正式合同允许自主推进的内容 | 连续且控制直接；与逻辑存在时间差，开始 / 暂停 / 变速 / 终止仍必须显式接入 |
| 向 committed 目标进度有界追赶 | 允许短时不同步，希望逐渐消除时间误差的内容 | 会改变播放速度；需要正式参数限制误差、追赶速率和重基线条件，关键命中时序可能不适用 |

进度来源与视觉衔接也要分开：关键事件可以严格跟随已提交阶段，同时由 Slot / Blend / Inertialization 平滑姿态。平滑姿态不意味着继续发旧动作 Marker；纠正 Body 也不应顺带修改动画时钟。

当前有限 Action 继续遵守 character-presentation-interpolation 的 committed raw sample 投影合同，不因新增策略入口改成自由推进。locomotion 继续使用正式 plan / prepared binding 的 FreeRun 或 CommittedMovement；CommittedMovement 是逻辑派生事实。扩展有限 Action 的自由推进不是本变更隐含承诺，需要先显式修改该现行合同。

本变更实现共享策略入口与当前合法绑定，不要求凭空增加全部策略或配置 profile。追赶方案作为可扩展选择，参数与使用方未定，不列为本次必须交付的实现。

### 4. 从已有策略抽出采样结果，删除重复推进

已有 IActionPresentationClockPolicy 把进度计算和 AnimationClipPlayerRuntime 写入绑在 DriveClock 内。沿此正式策略链拆出中立时间结果，继续复用 ActionCommittedSampleHistory、Projector、Registry 与既有播放身份。Player 只消费已算出的结果；Timeline Presentation driver 改为接收外部结果的求值入口。逻辑入口同样接收正式逻辑播放管理者的推进区间，不因是 Logic 域就保留一套自主时间源。

不能在旁边新增 ITimelineClockPolicy 并保留旧播放器时钟，否则只是把两份进度换了名字。拆分应只影响动作共享采样边界，保留已正确的 locomotion 策略、Phase、混合、source sampling 和 Pose 帧事务。

### 5. 图执行能力由域约束，表现 Marker 不借用逻辑调用栈

作者仍编辑现有正式 TimelineTrigger 图，图角色继续只允许 OnEnable。正式编译 / preparation 根据有效执行域校验节点、读写能力、目标资源和图身份；Logic 图走原技能执行服务，Presentation 图经既有图服务边界绑定表现安全执行上下文。Timeline 只提交精确图身份与事件，不加载或解释图。

Presentation 上下文只接收该表现帧可用的只读事实与正式表现输出能力，禁止读写 SimulationState、写 Gameplay fact / canonical input，禁止调用 Kernel Evaluate / Finalize。不能延长逻辑 invoker 寿命、造空 Actor 或私有 Simulation context 来绕过边界。能力不满足时必须在准备或作者提交处报告具体图 / 节点错误。

表现图执行的中间结果进入原表现帧的 Prepare / Validate / Commit / Discard 边界；事件只有在该帧被正式接受后交付下游。复用图模型与正式编译服务，不复制一个表现专用影子图系统。

Track 是执行域的唯一声明者，只允许 Logic 或 Presentation；Clip 与 Marker 全部继承所在 Track，不保留 Clip 覆盖字段或 DualProjection。内容类型只校验能否放入该域的轨道。两类轨道消费播放管理者提供的动作进度；逻辑结果通过原已提交结果链传给表现，不再次执行逻辑内容。本变更不补造持续表现 TreeClip，已有 Logic TreeClip 的 Root / Enable / Disable / Destroy 生命周期保持原义。

### 6. 区分正常经过、修正采样与已接受终态

| 输入变化 | 新 Marker 产生规则 | 既有表现处理 |
|---|---|---|
| 正常前进 / 正常循环 | 按真实经过产生；循环使用新的 traversal 身份 | 下游按稳定 EventId 调和 |
| 暂停或同位置重复采样 | 不产生新经过 | 保持已接受结果，是否继续尾部由原 owner 决定 |
| Seek / 分支修正 / 重采样 | 不因位置变化自动补发越过区间事件；真实事件修订走原身份调和 | 从当前可见结果接管，不能把修正误当自然循环 |
| 业务 Stop / Cancel 已按原协议接受 | 旧 playback / generation 立即失去新事件资格 | 已生成特效、音效和动画尾部按正式结束策略取消或完成 |
| 预测分支被撤销 | 消费最终分支更新，旧分支不再产生新事件 | 不能合成已确认 Complete / Release，不重置整个 Body / locomotion |

稳定 Marker 身份继续由 PlaybackHandle、Generation、MarkerId、TraversalIndex 组成；跨回旧位置再次重采样不能随意增加 TraversalIndex 逃避去重。事件记账与本帧结果一起提交，Discard 不得把尚未交付事件标成已消费。

逻辑 Restore 只恢复逻辑私有状态。Presentation 消费正式最终分支 revision / reset 信息，不读取回滚中间状态，不把旧表现页作为确定性快照还原。停止时点遵守原确认 / horizon 合同，本变更不把未确认 rollback terminal 提前当成最终释放。

### 7. 作者操作必须对应可保存、可执行的内容

已有 Track 的 Inspector 提供 Domain 编辑，Marker 显示继承的 Track 域；Domain 控制执行与输出权限，不切换全角色时钟策略。操作通过原 Timeline authoring mutation 校验受影响 Clip 和 Marker 图，更新同一 owner 下的域声明、闭包与 dirty / Undo 状态。存在不兼容节点或 Clip 时拒绝整次修改并定位内容，不只改 Track enum 留下旧 Clip 域。

目标 UI 统一按正式秒制时间保存：拖动反馈、秒输入、tick / 素材帧吸附与 CommitSource 使用同一转换和舍入规则。作者能看到当前吸附模式、参考 pipeline / 素材和频率；逻辑 tick 网格自动跟随正式配置，不另填一份易失配的编辑器逻辑频率。配置变化不改内容，显式重新对齐走原 mutation / Undo；不再把整数 StartFrame 作为正式保存目标。预计逻辑生效 tick 必须注明速率、暂停及起点假设，实际运行 tick 来自正式诊断。

Marker 的私有图归正式 owner 闭包；复制、删除、导出与 generate_assets 重建必须保留图角色、内容、身份与引用。不能只导出旧资产路径 / localFileId 后称为可独立重建，也不能自动生成一张空图代替原内容。

## Risks / Trade-offs

- [把共享采样扩展到所有角色表现] → 限定动作 playback；保留 locomotion、source phase、混合和独立效果原 owner。
- [平滑使关键攻击事件变晚] → 业务选择进度策略，事件与姿态衔接分别定义；有限 Action 继续遵守现行 committed sample 合同。
- [自由推进在逻辑零步或暂停时继续走] → 控制输入显式说明该动作是否暂停；无新逻辑样本不自动等于暂停，也不自动授权无限外推。
- [保留旧 DriveClock 与新 Timeline 策略造成两份进度] → 同一动作迁移时删除重复积分和同义配置，不留并行入口。
- [旧任务勾选被当成运行已完成] → 已完成项限定实际成果；表现执行、停止与共享采样保持未完成。
- [换秒被当成确定性或双时钟问题已解决] → 单位、数值表示、更新域和进度来源分别规定；保留 tick 身份，不承诺跨 tick 率的业务结果完全相同。
- [短窗口边界已发出但命中仍漏采样] → 原战斗领域必须按自身区间消费合同处理，不能把 Timeline 边界遍历当成连续碰撞保证。
- [参考项目内容格式被推断成完整运行架构] → ZZZ 整数帧 dump、HoMiyabi 导出秒数和 UE 动画函数只能证明对应数据 / 局部实现，不能据此断言其完整回滚能力或反证本项目必须采用某个时钟方案。

## Migration Plan

1. 明确秒制数值表示、精度和舍入，梳理正式字段及消费者；保留 Marker 同级模型、Logic 事务、Slate 坐标修复等已正确成果。
2. 同步迁移作者模型、资产、闭包 / 指纹、C# 导出重建、播放管理者的时间状态与 Timeline 求值状态、编辑器 mutation；旧时间位置按正式映射转为秒后删除旧帧字段和双写路径，保留 tick 身份和必要来源身份。
3. 在既有表现策略与播放 owner 中产出一次共享采样；同时接入暂停、Stop / Cancel、分支更新、采样原因与事件资格，再让动作 Player、Timeline Marker / Camera 消费，删除重复推进。
4. 在正式图编译与服务边界接入表现安全能力、帧事务、精确身份，删除临时 Logic invoker 的表现依赖；事件输出与去重记账一起接受或丢弃。
5. 完成配置驱动的逻辑 tick 吸附、素材帧吸附、显式重对齐 Undo、Track Domain 正式 mutation 和 Marker 私有图重建。无效转换显式报错，不保留旧兼容路径。

每步是可独立提交的模块改动，但不能将一个已接通接口当成整条运行链已经完成。代码层尚未实施的内容见 tasks.md；本轮只更新规划。

## 与现行 spec 的差异处理

- gameplay-tick-system 的固定步长、可配置 Scaled / Unscaled 时间来源和更新职责不变。本方案业务变速不写 Unity 全局时间，也不暗改默认 TimeSource；过去用 P Marker 调逻辑 invoker 的路径必须修正。
- btsmtl-timeline-editor-preview 仍要求整数作者帧、StartFrame 和 frame/value mutation，与新秒制目标直接冲突。本 change 增加对应 MODIFIED requirements，保留 Slate 单一入口与正式 mutation，改成秒保存、帧显示；主 spec 归档前仍是现状。
- character-presentation-interpolation 对有限 Action、locomotion 与 Body correction 已有不同合同；本设计服从它们。将自由推进普遍用于有限 Action 或因 Body 修正重置 Player 都超出当前方案。
- btsmtl-timeline-direct-runtime 的“Timeline 唯一时间 owner”旧表述与被动求值边界不一致。本 change 明确外部既有播放管理者拥有进度／速率，Timeline 拥有内容映射及求值状态；在同一 Step 和快照链提交恢复，不新建第二 Runtime。
- character-animation-pipeline 的 Clip 子 Marker 表述通过 delta 改成同级 Marker，保留原 requirement 名字以便准确归档。
- btsmtl-runnable-timeline-node 的 scale 描述通过 delta 删除。direct-runtime 的 ActionCue frame/cycle 改为秒制时间与 cycle；StateId / LocalFrame / BranchId 中的原始 LocalFrame 仅保留来源身份，不再构成独立调度位置。状态本地 ActionCue 在当前主 spec 中无同名 requirement，列为 ADDED。
- 本轮不改 openspec/project.md 与主 specs；待实现合同仍属于当前 change，不以文档更新宣称已经归档或完成。

## 实施前仍需明确

- 秒制表示已选 Q32.32；后续实施需将既有作者模型和运行消费者一次性迁移，不能因数值决定完成而声称资产已迁移。
- 既有哪个具体播放对象持有逻辑进度和表现策略、在帧事务哪一步计算和失效：职责已确定为 Timeline 求值之外的原播放管理者；沿现有 Action 历史 / Projector / Registry 核对具体类与调用点，不新建并列 owner。
- 正式控制中倍率叠加、生效 Step、暂停解除时间来源的具体业务规则；不得将 Timeline 内部自主计时或修改 Unity 全局时间作为补缺方式。
- 表现图通过现有图服务接入的实际能力：明确具体上下文、合法节点和输出入口；如果原服务必须修改，明确同一正式服务的修改范围，不另搭影子运行器。

停止与修正必须作为共享采样的输入同时设计，不能在消费者接通后才补事件资格。本设计记录已确定方向和约束，不把上述未决项写成已经完成的实施细节。

## 已有内容与编辑器成果记录

以下保留本 change 已有成果，不以它们证明新的表现 Marker 链已完成：

- Timeline 顶栏由 TimelineEditorBindingState、TimelineEditorToolbarView 与 TimelineEditorWindow 分别拥有只读模型、视图和控制；Slate 是唯一 Timeline 编辑面，FlowCanvas 负责正式图可视化。
- Corin AttackProperty 由 Ability / Attack 领域转换并解释；ActionCue 只在 Logic commit 后发布原始 CueType / CueId、播放身份与事件身份，碰撞、属性和命中效果 payload 留在 GameplayEffect / Ability。既有记录包含 108 个效果 key 与 uint 编号收口；没有领域订阅者时，trace 不等于业务已消费。
- Attack3 的 Attack_Normal_03_Explode 使用 CorinAttack3Timeline 中起点 frame=75 的独立 Section，源本地 frame=1 对应全局 frame=75，事件携带 StateId / LocalFrame。
- Attack5 在 frame=47 通过 Attack5EndBoundary 决策进入 End 或 End_2；两个状态分别绑定 CorinAttack5EndTimeline / CorinAttack5End2Timeline，后者播放 Corin_Attack_Normal_05_B。End_2 的源本地帧 1、10、12、14、16、18、20、22、24、26、28、30、32、34、36 共 15 个 cue 映射至从 0 开始的 Timeline，BranchId=End_2。
- StateId / LocalFrame / BranchId 已进入 ActionCue sample、committed event 与稳定 EventId；Attack5 frame=64 多余 _01_02 cue 已清理。旧记录中的 1121 帧回放结果只对应当时内容改动，不作为本轮共享表现采样或图执行的证据。Branch / Rush 内容不在本次设计扩展范围。
