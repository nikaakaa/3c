# Design: 在唯一Foot事务内稳定Path并保证Landing可达

## Context

当前重构已经把Landing Lifecycle、Ground Path、Anchor、Support、Pelvis与Goal收进唯一`CharacterFootPlacementModule -> Resolved Foot Pair`根事务，并完成纯Transition Resolver、唯一Transition Runtime、纯State Target Resolver、统一Interpolation Runtime与Post-Interpolation Constraint阶段拆分。旧`CharacterFootStateMachine`、分散Residual/Progress和兼容入口已经删除；这个唯一生命周期和Owner边界继续保留。Prediction已经以零诊断回归接管唯一KCC Future Translation，当前剩余重点是Landing/Lock垂直接管仍可绕过Interpolation，以及正式Foot Motion、Support、Reach和Contact/Lock旧语义尚未全部迁移。

诊断显示三类问题具有不同边界。Path侧已经证明identity单独触发Residual重置不合理，但删除该触发后用户观察到的整体抖动基本不变；`20260828-184607-138-d23494c9824a42a89c9973d567305442`进一步证明同Event、同RootLocalLanding在720度/秒转向下因瞬时世界速度变化令Future Body Translation单帧移动约36厘米，查询切换Surface，Next Landing移动约39.7厘米并由Post Constraint产生约17.5厘米Correction跳变。`20260829-084258-427-2e96ab5155fd4730a74be4732c90493f`已经让Prediction以零诊断回归进入唯一运行链，因此当前首要断点转为Landing/Lock垂直接管：Acquire进入帧与Ground Constraint都能绕过Interpolation同帧抬脚。Landing直腿则仍是Foot Goal在Pelvis无有效Reach协调时超过腿长，FBBIK只能把膝盖夹到伸展极限。设计必须分别处理Prediction时间连续、Landing提交、垂直视觉连续、允许的Ground穿透预算和腿链可达，不能用一个最终Pose平滑器互相交换问题。

## ZZZ PIK参考口径

`D:\ZZZ_Dump\PIK分析包`作为本change的行为基准。正式证据入口固定为`证据审计与勘误.md`、`代码边界与完整性清单.md`、`PIK精确函数审计表.md`与`平滑层审计.md`；旧重构C++、固定窗口ASM和“完成报告”只作历史材料，与上述四份文件冲突时不得使用。

本change对P0/P1已经确认的控制结构采取“默认照搬”原则：实现顺序、跨帧状态分离和边沿处理不得另造同类算法；只有项目输入合同确实不同，或同一1044帧Replay证明照搬会回归时，才允许在现有Owner内调整，并把差异、业务取舍和证据写回proposal。照搬的是行为，不是地址、混淆字段名、对象内存布局、构造默认值或PIK单体组件。

项目正式Foot Motion lineage、根Bank双页事务、canonical Observation、完整Ground Path/Envelope、分型Transition/Interpolation/Post Constraint、Resolved Foot Pair、唯一Goal Set/FBBIK/Writer和封口诊断继续作为装配外壳。ZZZ行为必须进入这些既有Owner，不得形成第二Prediction、第二Interpolation、第二Goal链、Legacy路径或全局缓存。

### 证据等级与采用规则

ZZZ资料必须按分析包的正式证据等级使用：

| 等级 | 可采信内容 | 本项目使用方式 |
|---|---|---|
| P0 原始证据 | 当前DLL精确函数边界、指令、字段访问、分支、数组步长和直接调用 | 可定位实现和验证数学；不得从地址直接推导业务名 |
| P1 直接结构 | 当前态到目标态线性混合、边沿/驻留/一次性事件、两份独立速率限制、权重基准混合、固定容量与有限值保护 | 本change默认照搬控制顺序和状态职责，并翻译为typed Event、Profile与根Bank合同 |
| P2 结构推断 | 候选位置、接触状态、旋转参考、组件过滤等中性职责 | 只能帮助决定现有Owner，不直接产生业务字段、状态或默认值 |
| P3 业务猜测 | Plant、Lock、Grounded、Pelvis、Heel/Toe、Stride、Moving Platform等具体名字 | 不作为本change实现合同；必须由项目正式输入和Replay重新定义 |
| X 错误或冲突 | 固定步到渲染帧相位、世界速度EMA精确公式、候选稳定tie-break、PIK直连tbik/通用Solver等旧结论 | 从Change删除，不得继续作为采用理由 |

### 成熟结论逐项对账

| ZZZ精确结论 | 本项目采用结论 | 当前归属 |
|---|---|---|
| `f`来自当前/下一状态ID和过渡量，执行`current + (target-current) × clamp01(f)` | 使用正式Contact/Lock或Transition提供的typed权重混合当前脚态与目标脚态；不把`f`解释为固定步到渲染帧相位 | Transition、Interpolation Runtime |
| `0x171D6920`为每脚目标高度保存历史并执行`delta=clamp(target-history, ±rate×dt)`，同时由历史到目标距离、事件位控制更新、冻结与强制刷新 | 每脚唯一Target Height历史保存Accepted Landing沿Up高度；Swing输出为`正式Raw Height + Filtered Landing Height - Current Landing Height`，正常Phase直接通过；Current Landing或Plant Target与历史的累计差大于等于`TargetHeightForceRefreshDistance`时刷新内部目标并由Residual/Correction连续接管，中等变化才按`MaximumVerticalTargetSpeed`限速 | Interpolation Runtime Target Height Stage |
| `0x171D7910`为每脚修正标量保存另一份历史并执行第二次`±rate×dt`限制 | `EffectiveCorrection`必须在状态权重混合后形成独立历史；每帧先把上一Interpolation世界输出按当前Animated Sole重表达，再以`MaximumVerticalCorrectionSpeed`限制其向本帧Desired Correction的沿Up变化，禁止动画基线位移绕过限速；不得用Plant Target历史替代这一层 | Interpolation Runtime Correction Stage |
| `0x171D8A00`把修正结果按`k=clamp01((1-weight)×globalWeight)`与基准混合，同时让缓存量回归 | 项目只在Correction限速后通过现有Foot Goal/Position Weight与动画基线混合一次；不新增第三历史或Goal后处理器 | Resolved Foot、Goal Contribution |
| B边沿、D组合位、驻留计时和一次性事件分开，快速重入事件会绕过普通门强制刷新 | 使用正式Contact Event表达Rising/Falling与Same-Event Reentry Refresh；顶层五态不增加匿名B/D业务状态 | Contact Transition Context、Resolver/Runtime |
| 无有效候选只跳过本次新候选调整，通用输出尾段仍继续 | Rejected Observation不提交新Landing，但不得把整帧Foot状态冻结；既有Accepted Landing、Interpolation和输出仍按正式合同推进 | Observation、Landing Context、Interpolation |
| 候选分支严格选择更高Y，但等高候选没有稳定tie-break证据 | 项目继续使用canonical最近合法Surface和稳定identity；不复制“最高Y”候选政策 | World Query Adapter、Observation |
| 主求解存在标量死区、有界响应和带符号速率限制，但输入不是已证明的世界速度Vector | Prediction Motion继续采用本项目Replay已经证明的`Body Current + Timeline Continuation`合同；不得再声称它是照搬ZZZ世界速度EMA | shared Prediction Motion State |
| 热路径固定缓冲、数组边界和NaN/Inf保护 | 现有根Bank、固定容量Observation/Workspace继续作为更强等价实现；新增Prediction、Landing和Pelvis状态也必须固定布局、无每帧托管分配、入口有限值校验、容量溢出typed失败 | Runtime storage、validator |

本change的ZZZ迁移主链因此固定为：

```text
Accepted Candidate / Verified Anchor
-> typed状态与过渡权重
-> 每脚目标高度历史限速
-> current/target状态混合
-> 每脚Correction独立历史限速
-> 既有Foot Goal权重混回动画基线
-> 唯一Goal Assembler与FBBIK
```

当前实现已经具备持久Plant Target、目标高度限速和PlantBlend权重，但尚未具备“状态混合后的独立Correction历史限速”。这不是参数调优，而是缺失的一层正式Owner，必须在本change内补齐后才能描述为基本迁移完成。Prediction、Pelvis、Knee、旋转、Heel/Toe、Moving Platform和PIK到tbik/通用Solver的连接不属于这条已确认主链；除本change已有项目证据支持的能力外，不用这些未闭合推断扩大范围。

## Decision 1: 唯一正式Foot Motion Runtime Frame

Projection Compiler在`build-character-foot-motion-data-foundation`归档后，从原生AnimationClip Catalog和匹配Foot Analysis lineage降低唯一Foot Motion payload。Source Runtime按与Component Pose相同的Live Contribution、Source、Cycle、Normalized Time与Completion生成左右脚typed Sample；离散Lock Mode不得跨Source混合。

Foot Placement Pose Input只接受这一个Frame。缺失完整Curve、Event table、Source lineage或Contribution归属时整帧typed invalid，不读取旧Artifact字段、旧隐藏Feature、默认值或另一动画Source补全。

Step Event table由正式Step Time边界、Step Distance、匹配Artifact中的RootLocalLanding与稳定source/cycle/side ordinal共同编译。Runtime不读取Library Artifact；Editor Build只把已经严格对账的结果发布进Projection。

## Decision 2: 消费者按依赖顺序迁移

正式顺序固定为：

```text
Path逐阶段归因
-> 首个不连续阶段修复
-> 拆分State / Transition / Interpolation / Post Constraint
-> Step Time/Distance接入Prediction
-> 共享Prediction Motion稳定
-> Approach Plant目标持续准备 / Contact Verification / Anchor冻结
-> Foot Height接入Swing
-> Support接入Primary Support/Pelvis与非对称速度边界
-> Landing Reach闭合
-> Contact/Lock接入Transition、State Target与Interpolation
```

每一步只切换一个业务定义。对应旧字段在同一步删除；未轮到的正式字段可以存在于不可变Frame，但不得影响行为。架构拆分已经完成并继续保持单一Owner。Step Time先成为Prediction唯一时域，再驱动已经连续的Residual截止；共享Prediction Motion只稳定committed Body Target当前速度与移动计划Continuation，不修改正式Step Time、RootLocalLanding、Visible Rotation或KCC世界碰撞。随后让Approach Contact继续Tracking并在唯一Interpolation中准备Plant目标，首次正式Contact Rising再以一次Plant Verification建立Anchor；稳定Plant冻结Anchor。只有该边界准确后才消除Acquire与Ground Constraint绕过Interpolation的同帧抬升并安装穿透/Lock门控，再迁移Foot Height。Contact/Lock最后清理旧语义，因为延长Landing前必须先让Swing接近Verified Anchor并让Pelvis拥有有效Support/Reach输入。

## Decision 3: State、Transition、Interpolation与Post Constraint分层

每只脚继续由一个根事务处理，但帧内顺序固定为：

```text
Immutable Foot Input / Observation
-> Pre-Interpolation Transition
-> State Target
-> Unified Interpolation
-> Post-Interpolation Transition
-> Post-Interpolation Constraint
-> Resolved Foot
```

`CharacterFootDiscreteState`只表达当前业务归属：`Swing / UnlockedSupport / Landing / Locked / Releasing`。Lock的`Sliding / FullAnchor`继续是Locked内部的typed响应模式，不扩展成第二套顶层状态。状态本身不得拥有`Enter/Exit`副作用、计时器、Residual或World Query。

顶层五态保持不变，不增加`Rebound`、`Blocked`或另一套Grounded状态。根Bank内部新增分型`Contact Transition Context`，只保存上一帧正式Lock请求、距最近Contact边沿的秒数、最近Contact Event identity和最近释放的Contact Event identity。`Contact Rising / Contact Falling / Same-Event Reentry Refresh`只属于本帧不可变Transition事实或Reason，不成为持久顶层State。相比PIK依赖匿名输入与驻留阈值，本项目还拥有正式Event和Anchor lineage：同Event在Releasing期间重新请求Lock时必须强制刷新Transition与Target，但只可复用仍保留的Verified Anchor；Release完成、Anchor已清除后不得复活旧Event。紧接着到来的新Event必须执行自己的首次Plant Verification，不受上一Event回弹事实影响。

`CharacterFootTransitionResolver`是纯决策器，只读取不可变Frame、Observation与上一Committed typed Context，返回固定`CharacterFootTransitionDecision`。Decision至少包含Source、Target、Reason、Event lineage、执行Phase、Anchor Command与Interpolation Policy identity。允许边固定为：

```text
Swing -> Landing | UnlockedSupport
UnlockedSupport -> Landing | Swing
Landing -> Locked | Releasing
Locked -> Releasing
Releasing -> Landing | Swing
```

输入驱动的边在Pre-Interpolation阶段执行；只有依赖本帧插值完成事实的边在Post-Interpolation阶段执行。`Releasing -> Landing`只允许同Event Contact Rising、原Verified Anchor仍保留且Reach/Lock准入继续合法时在Pre阶段执行；它必须Retain原Anchor并从当前Effective Correction连续重入，不得Create Anchor、重查Landing或把Interpolation清零。`Releasing -> Swing`属于Post阶段，完成后必须用Swing输出分类执行同帧Post Constraint。系统不得循环求Transition直到稳定，也不得让状态目标或插值器暗中改State。唯一Transition Runtime负责应用Decision、改写离散State、执行Anchor Create/Retain/Release命令并记录原因；其他模块不得写这些字段。

Contact边沿事实 MUST由同一Resolver按上一Committed Contact Transition Context与本帧正式Contact、Lock Mode和Event identity纯计算。唯一Transition Runtime在应用Decision时同时更新该Context；State Target、Interpolation、Post Constraint与Diagnostics不得写回边沿历史。Reset、Retarget、Source lineage变化和根Bank Discard分别按现有事务语义清空或保留Committed Context，不得在失败帧推进边沿秒数或消费Event。

`CharacterFootStateTargetResolver`按已经确定的State纯计算本帧目标，输出目标Correction、Reference、Contact/Support/Reach意图和固定typed Interpolation Request。它不得读取或推进Delta Time、Residual、Progress与上一输出，也不得查询世界。Swing/UnlockedSupport目标只来自Ground Path、Envelope与正式Foot Height；Landing/Locked目标来自唯一Anchor和正式Contact/Lock；Releasing只回到原始动画Swing目标。

`CharacterFootInterpolationRuntime`是Foot连续链的唯一所有者。它只接受`Previous Interpolation State + State Target + typed Policy + Delta Time`，持有一份统一Interpolation State并发布Output、Residual和Completion。现有Swing Residual、Acquire Residual、Release Residual、Contact Progress与散落的HalfLife推进必须迁入这里；政策固定为直接跟随、Residual Half-Life与正式Weight接管等有业务含义的typed策略，不提供string key、字典注册、任意曲线回调或项目级通用Tween。

Interpolation State内部必须把每脚唯一`Target Height History`与`Effective Correction History`分开。Target Height History保存Accepted Landing沿Component Up的世界高度，不保存包含动画Phase的完整Swing Raw Height。Swing Raw Height仍由`Ground Envelope + Formal Foot Height`产生，过滤后输出固定为`Raw Height + Filtered Landing Height - Current Landing Height`；因此同一Ground Path只因动画Phase推进时直接通过正式曲线。Foot Motion Profile新增必须显式序列化的`TargetHeightForceRefreshDistance`并纳入Revision；它必须为有限正值且大于`PathRevisionDistance`，Corin首个候选为`0.30m`。同Event Ground Path Input换代时，Current Landing与Filtered Landing Height的累计沿Up差达到该值必须刷新内部历史并让既有Swing Residual捕获可见连续性；累计差小于该值但本次换代超过`PathRevisionDistance`才以`MaximumVerticalTargetSpeed × Delta Time`追赶。Event换代同样直接刷新Landing Height并由Residual接管。Approach/Plant取得Interpolation所有权后，Swing Target Height更新必须发布typed Held，Held期间Next Swing Event只能提供本帧Raw Swing Target，不得改写或解释Current Plant拥有的Target Height identity/value；Plant Target沿同Event继续这份Landing高度历史，Plant Target与历史的累计差达到该值时刷新内部高度并由Plant Residual与独立Correction历史连续接管，禁止同帧双重限速。随后以typed状态/Contact权重混合当前脚态和目标脚态；Effective Correction历史在写回前必须先把上一Interpolation世界输出按当前Animated Sole沿Component Up重表达，抵消动画基线自身位移，再以`MaximumVerticalCorrectionSpeed × Delta Time`限制它向本帧Desired Correction的沿Up变化。最后的动画基准混合只由既有Foot Goal/Position Weight执行一次，不再建立有历史的第三平滑层。两份历史只可由各自明确的Reset、Retarget、Event失效或Policy退出规则清理；同Event Prediction换点、Contact Verification和Same-Event Reentry不得同时清零。Swing/UnlockedSupport继续以Accepted Ground Envelope作为不可延迟的硬下界，Release继续使用统一Residual。`AcquireByWeight`进入帧不得对Contact Anchor调用`RaiseToMinimum`，Lock Weight达到1也不得把未收敛Residual清零。

根`CharacterFootStateContext`收敛为一组分型数据块：`Discrete State Context`只存当前State与最近Transition，`Contact Context`只存Anchor和Lock响应，`Contact Transition Context`只存边沿与已消费Event历史，`Interpolation State`只存上一目标、Effective Correction、统一Residual与完成事实，Landing与Observation继续使用各自typed Page。所有数据块仍由同一个Pending/Committed根事务一次Seal或Discard，不建立独立生命周期。

Post Constraint只在插值后消费结果，但按状态承担两种明确责任。Swing/UnlockedSupport必须继续把Accepted Ground Path Envelope作为硬最低约束，防止一个仍可到达的Swing因为Residual或目标限速落到地形下；这项约束不参与Landing/Lock垂直接管。Landing/Locked的Verified Anchor部分只测量连续输出的穿透深度、判断是否位于`GroundPenetrationTolerance`内并发布`GroundCatchup`与Full Lock门控，不得调用`RaiseToMinimum`、修改Effective Correction或写回Interpolation历史。Contact接管允许不超过容差的轻微穿透；若状态交接继承超预算Contact误差，输出必须继续受Contact竖直速率限制并向同一Verified Anchor收敛，期间不得Full Lock。Landing Reach和有限值边界仍可硬夹紧不可达Goal。Ground测量与Reach夹紧都不得回写State Target、Residual或Transition。

## Decision 4: 先定位Path同帧放大，再分离连续目标与Envelope安全

FutureLanding世界事实固定拆成`Committed Body Target Current + 移动计划Continuation -> shared Prediction Motion State -> KCC Future Body Translation -> Raw Landing Candidate -> Query Admission -> canonical Landing Observation -> Landing Tracking -> Approach Plant Target Preparation -> Contact Verification`。Prediction Motion State属于Foot根Bank且左右脚共享一份；它不得进入Gameplay、World State、rollback或网络packet。状态至少保存初始化标志、稳定当前速度、稳定Continuation速度、移动计划Generation、Body Reset Sequence与Prediction Source identity，并随根事务Seal或Discard。

Prediction使用本项目Replay已经证明的正式控制律，不把ZZZ主求解的未命名标量响应解释成世界速度算法。当前目标取committed Body Target世界速度，Continuation目标取committed移动计划下一段世界速度；两者分别计算`TargetVelocity - StableVelocity`。差值不超过Profile显式`PredictionVelocityDeltaThreshold`时保持稳定速度，超过时按`PredictionVelocitySmoothSpeed * PresentationDelta`执行有界EMA响应，再把结果限制到`PredictionMaximumSpeed`。三个配置必须为有限正值、纳入Profile Revision且由Corin正式序列化；缺失或非法时整项typed unavailable，不提供默认值。首次合法输入直接以对应正式速度初始化，避免从零产生启动滞后；Body Reset、Retarget、移动计划Generation或Prediction Source变化清空状态，普通Landing Event、Animation Source和左右脚Step换代不得重置角色级Prediction Motion。移动计划Current Velocity只作为诊断对照，不得替换KCC当前运动起点。停止边界缺失移动计划时保持上一Committed Prediction状态且不生成本帧Future Translation；显式静止计划的生产侧闭合不在本change实现，Foot Placement不得补零。

唯一KCC Future Body Translation继续负责真实世界碰撞，只是请求中的当前与Continuation平面速度改为稳定速度。左右脚按各自正式Step Time读取同一Pending Workspace；RootLocalLanding仍只乘本帧Visible Rotation，不预测Future Yaw。Prediction不得复制KCC、创建低速普通路径或在KCC结果后另做位置低通。

Raw Landing在`Tracking`阶段仍从每帧不可变Frame Input重新投影；Committed Observation Page保存上次真实查询使用的Side、Landing Event、Source Sample、Source Cycle、按1毫米量化的Raw Landing、按`1e-4`量化的Component Up、Profile Revision与World Revision。当前Candidate与该查询快照的世界位移累计不超过`PredictionInputAccumulationDistance`且Up夹角不超过`ComponentUpChangeAngleDegrees`时复用同一Committed Observation Page，不更新累计基准也不查询。

Corin显式使用5厘米累计位移和1度Up角度。距离配置必须为正且不得超过Landing Sphere半径；因此本change不采用10厘米。Event、Source Sample、Source Cycle、Profile Revision或World Revision变化不受阈值限制，必须执行一次新查询。正式Foot Lock Mode处于`Sliding`、正在准备接触准入时，只要canonical预测输入identity变化也必须刷新Observation，避免缓存落点误差与动画脚底残差相加后越过8厘米Lock准入；输入identity未变时仍复用，不重复查询。超过任一阈值时同样恰好查询一次；SphereCast使用当前Candidate生成的新canonical Key反量化几何，并只选择canonical最近合法Surface。Accepted与Rejected查询结果都属于不可变Observation；Pending根事务失败时不得提交新Page。

上一Committed Surface、Frame、Authority Tick、Trajectory Generation、Future Translation Source、Foot State、Residual与查询输出不得进入Query Admission、Observation Key或候选选择。上一Surface不得传入World Query或改变候选选择。`Tracking`阶段新查询命中不同Surface时只换代NextSwingLanding，不得覆盖Current Anchor；同Surface才通过独立`LandingAcceptanceDistance`决定是否替换点位。Accepted与Rejected Observation必须保持各自Key和结果；新Rejected Observation不得冒充上一Accepted结果，但同Event Landing Tracking可以继续持有此前已经Accepted的NextSwingLanding，诊断必须同时记录当前Rejected Observation和被保留Landing的原始lineage。查询前累计阈值、1毫米Key量化和查询后Landing接受距离是三个独立定义，不得合并。

Landing Context在现有唯一生命周期内保存三个可并存的typed槽位：`NextSwing Empty / Tracking`、`Plant Target Tracking / Verified`与`Verified LastLanding`。NextSwing属于Prediction Event，Plant Target是项目对ZZZ独立目标历史结构的typed映射，LastLanding属于已经真实接触的Plant事实；三者不是互斥状态，也不构成第二状态机。PreSwing、Swing和Approach Contact都保持NextSwing Tracking；首次Accepted Observation建立NextSwingLanding，后续可信Observation继续按Query Admission和Landing接受规则换代并重建Ground Path。`ApproachContactToLanding`只声明Plant目标准备区：每次Accepted Prediction可以更新Plant Target的Desired Point与诊断Ground Path，但不得直接把Path Revision写入可见Correction。

唯一Interpolation直接迁移ZZZ已确认的两份历史、门控与后续基准混合顺序。PreSwing/Swing用正式Envelope与Foot Height生成Raw Height，以Accepted Landing沿Up高度作为唯一Target Height历史，并按`Raw Height + Filtered Landing Height - Current Landing Height`形成Swing目标；正常Phase不积分历史，同Event Ground Path Input换代时以Current Landing到Filtered Landing Height的累计沿Up差判断强制刷新，累计差达到`TargetHeightForceRefreshDistance`时刷新内部Landing高度并由Residual连续接管，累计差小于该值但本次换代超过`PathRevisionDistance`才进入`±MaximumVerticalTargetSpeed × Presentation Delta`限速。Approach进入时冻结Swing对该历史的更新，并沿同Event由持久Plant Target继续推进；Plant Target到历史的累计差使用同一强制刷新门并由Plant Correction保持可见连续，水平分量直接采用同Event目标，Contact Verification与Same-Event Reentry不得重置。Runtime随后用单调不回退的正式Contact/Lock权重在Swing输出与过滤后Plant Target之间形成Desired Correction，再以独立`Effective Correction History`把其Component Up变化限制为`±MaximumVerticalCorrectionSpeed × Presentation Delta`。Resolved Foot之后只通过既有Foot Goal/Position Weight把该结果与动画基线混合一次。Landing与Locked继续使用同一`PlantBlend` Policy，稳定Plant冻结Desired Point。普通PreSwing/Swing仍由Ground Envelope硬保护；Approach Plant、Landing和Locked只测量过滤目标或Anchor穿透，不用Post Constraint抬升可见输出。

该Event首次产生正式Contact Rising且Lock Mode请求Sliding或Locked时，Runtime必须执行一次typed Current Contact Plant Verification。Verification使用当前Animated Sole生成唯一Current Contact查询输入，但查询结果只可在同一Transition事务中建立一次LastLanding、Promoted Contact Landing与Anchor；它不得作为第二Prediction、第二Anchor或逐帧Ground路径。稳定Plant期间Anchor冻结，普通Contact、速度、Surface候选和Prediction变化不得再次查询或重定位。Verification缺失、拒绝、与Event lineage不一致或超过Lock准入时保持没有Anchor并发布typed unavailable，不使用早期Prediction点冒充真实Plant。

Approach Contact到达时没有同Event Accepted Landing必须发布typed准备不可用，但仍保持Tracking并等待后续合法Observation；不得使用Animated Sole、默认地面、另一Event或旧Surface建立预测目标。Reset、Retarget、World Revision变化或Backend重建必须使Tracking与Verified Contact失效。正式`Sliding`接触准入刷新在Tracking中仍按canonical输入执行；实际Contact Rising只执行上述一次Plant Verification，稳定Plant不得以误差修正为由重复查询移动Anchor。

当前FootPlacementSurface在World Query Backend生命周期内视为静态，Backend发布固定非零World Revision；Reset、Retarget或Backend重建必须清空每脚Observation Page。移动平台和运行时Surface变更不在本change范围。

Ground Path Input identity只表示查询输入lineage，不单独触发Residual重置。Path Revision只由Event、Path可用性或Accepted Landing端点变化产生；同一Event、同一Landing与同一Envelope内的Phase目标变化不得发布Path Revision。正式Swing目标变化超过独立`PathRevisionDistance`时，Interpolation Runtime可以发布分型`TargetTrackingApplied`并捕获`PreviousOutput - NewTarget`，但不得把它记录为Path Residual重建。原始Builder目标与State Target继续分列诊断，不得互相改名覆盖。`PathRevisionDistance`不得控制Landing接受、Residual截止或Release完成；后二者分别只读取`SwingResidualTolerance`与`ReleaseCompletionTolerance`。

Accepted Swing Motion必须携带与同一Ground Path Event匹配的typed Swing Path Landing Reference。Verified Plant Landing只属于Contact/Anchor准入，不得门控Swing Path可用性或提供Swing Residual的Landing Point。同帧旧Event完成Plant Verification、下一Swing Event已经Accepted时，Foot根事务必须同时保留旧Contact Landing和新Swing Path Landing，不得把Path发布为一帧不可用。

Path诊断必须先在同Frame、Side与Event lineage下记录`Raw Landing/Path Target -> Swing Target -> Captured Residual -> State Output -> Vertical Rate Limit -> Ground Penetration/Post Constraint -> Encoded Goal`。任一后继阶段的单帧Correction变化明显大于直接输入变化时，必须先修复第一个产生不连续或放大的阶段；不得通过Goal低通或Step Time截止把该跳变藏到无Owner的后处理器。统一Interpolation的正式限速属于目标接管政策，必须同时记录追赶欠账和穿透代价。

在上述Correction链已经连续后，普通Swing目标使用统一Interpolation State中的Residual。基础半衰期仍来自Profile；当Residual大于`SwingResidualTolerance`时，Interpolation Runtime按剩余Step Time计算保证在Landing前收敛到容差所需的半衰期，并取它与基础半衰期的较小值。没有有效Step Time时不得猜测截止时间，只能发布明确输入不可用。Step Time只解决Landing前仍有Residual欠账，不负责改变Raw Target、重选State Output或修正同帧放大。Releasing完成只使用独立`ReleaseCompletionTolerance`，不得因调整Swing截止精度而改变Release退出时机。

Swing的Ground Path Envelope同时服务连续轨迹目标和插值后的Ground安全约束。Post Constraint MUST消费本帧Accepted Swing Motion已经采样的同一Envelope Point和Path identity，不得重新Raycast、SphereCast或读取另一Surface。Envelope随Swing Progress连续采样；只有正式Path Revision才能改变其几何。Swing/UnlockedSupport的Interpolation Output低于Envelope时必须立即执行硬最低约束并记录Clamp事实，但不得把Clamp写回Interpolation历史。Landing/Locked只以冻结Contact Anchor测量穿透深度、容差内/外、竖直限速和预计追赶时间，不得立即抬升；Full Lock只有在正式Weight完成、位置残差不超过`LandingLockCompletionTolerance`且穿透不超过`GroundPenetrationTolerance`时成立。

`Releasing -> Swing`完成必须由Post-Interpolation Transition先更新顶层State，再按新State执行Ground Floor和最终输出分类，避免同一帧发布Swing却跳过Swing Envelope保护。

## Decision 5: Foot Height只定义Swing动画高度

Swing的世界目标沿Component Up固定为：

```text
DesiredSoleHeight = RuntimeGroundEnvelopeHeight + FormalFootHeight
DesiredCorrection = DesiredSoleHeight - AnimatedSoleHeight
```

Formal Foot Height只表达动画脚高于动画Foot Path的高度。它不包含Runtime Landing、Anchor、Pelvis或世界修正。Runtime Ground Envelope只表达地面下界。两者组合后删除由`LandingConstraintWeight`乘`BaselineHeightError`或`FormalTargetCorrection`的旧高度/目标政策；Foot XZ继续来自动画骨骼，不创建Foot Forward曲线或空间位置双写。

## Decision 6: Support与Lock解耦并先进入Pelvis

Resolved Foot把正式Support写入`SupportIntentWeight`。Primary Support的Acquire/Retain资格由Support Intent、稳定Event lineage和有效`PelvisReachReference`共同决定，不要求Lock Mode为Locked，也不把Support反写为Foot Goal约束。

`ContactReference`继续只属于脚锁；`PelvisReachReference`可以来自同Event已经Accepted的Landing/Ground事实。这样Sliding或暂时不可锁的承重脚可以协调Pelvis，但不能因此把脚固定到世界Anchor。

Primary Support只消费Resolved字段，不读取Foot State、Lock Mode或Context。Support的Pelvis Reach Reference按同Event冻结Anchor、Verified Landing、Prepared Plant Landing、Accepted Swing Landing的正式优先级取得，Contact Anchor不可用时不得把Formal Support强制清零。Support曲线为0时不得由相对大小归一成1；双脚都无正式Support时Pelvis进入现有typed Release，而不是猜一只脚承重。

## Decision 7: Landing Reach先协调Pelvis，再限制Foot Goal

Foot Motion Profile新增必须显式序列化的米制`MinimumLandingLegCompressionReserve`并纳入Profile Revision。缺失、非有限或越界时整项typed invalid，不提供代码默认值或旧配置补全。State Target Resolver与Resolved Foot为预测Landing脚，以及仍持有同Event Contact Goal的Landing、Locked、Releasing脚发布typed Reach Request：Hip、目标Ankle、Leg Length、最小压缩余量、Landing Event和有效世界Reference。Releasing必须继续参与直到其Goal权重归零，避免Pelvis在释放期间单独上提并把接触腿拉到近伸直奇异区。它不是第二Support、第二Anchor或第二状态机。

Pelvis Builder同时计算Primary Support腿和Landing腿允许的Pelvis沿Up区间：

```text
FeasiblePelvisInterval = SupportReachInterval ∩ LandingReachInterval
```

交集存在时，Pelvis Target与Spring必须限制在交集内。现有Critical Spring继续是唯一Pelvis连续状态，并增加Profile必须显式序列化的`PelvisMaximumUpVelocity`与`PelvisMaximumDownVelocity`。Spring积分后先把Velocity限制在`[-MaximumDownVelocity, MaximumUpVelocity]`，再推进Output并限制在Reach交集；Output撞到上/下边界且Velocity继续朝外时必须清除对应方向速度。只要非零Pelvis Output是满足最小压缩余量所必需，即使小于现有5毫米Endpoint Tolerance也必须以正式Goal Weight写出，不得一边发布Reach Available一边把实际Pelvis Goal权重清零。Support换代、坡度变化和Target跨越Output继续使用现有显式Handoff与Velocity Reset，不增加第二Pelvis平滑器。

交集不存在时，系统先保持Primary Support安全，再把Landing Foot Goal夹紧到保留最小压缩余量的最大可达点，发布`LandingReachUnavailable`，并禁止该脚进入Full Lock。它可以保持Landing、Sliding或进入Releasing，但不得把超长目标交给FBBIK后仅靠腿伸直夹紧。

该政策的业务取舍是：不可同时满足双腿时允许短暂未完全踩实，换取不出现明显直腿、骨盆瞬移或关节奇异。

## Decision 8: 正式Contact与Lock驱动Transition与统一插值

正式Contact有效且同Event Lock Mode首次从Unlocked进入Sliding或Locked时，Pre-Interpolation阶段先执行一次Plant Verification；只有Verified Landing合法且该Event尚未消费，Transition Resolver才发布`Swing/UnlockedSupport -> Landing`与Create Anchor命令。Transition Runtime只建立一次Anchor，并把本次Contact Rising与Event写入同一Contact Transition Context；State Target Resolver以该Anchor生成Landing目标，Interpolation Runtime保存当前Output到Anchor的Residual，并按正式Lock Weight推进接管。Mode、Weight、Event或Verification不一致时发布typed invalid，不按早期Prediction或旧PlantConfidence继续。

正式Locked Mode和完成的Lock Weight触发`Landing -> Locked`，并使用`FullAnchor Response`目标。已锁脚回到Sliding Mode时保持同一顶层Locked生命周期和同一Anchor，只切换内部Sliding Response目标。Mode回到Unlocked或Contact正式退出时触发`Landing/Locked -> Releasing`，记录Contact Falling和最近释放Event；Release仍由Interpolation Runtime这个唯一Effective Correction Owner处理。

Releasing期间同Event再次出现Sliding或Locked请求，且原Verified Anchor仍保留、Lock距离与Reach仍合法时，Resolver必须发布typed `SameEventContactReentryRefresh`并执行`Releasing -> Landing`。Transition Runtime只Retain原Anchor；State Target立即重新计算同Anchor目标；Interpolation Runtime从当前Effective Correction连续接管，不得重置为零或重新查询世界。若Release已经完成、Anchor已清除，旧Event不得复活；新Event即使紧接上一边沿也必须执行自己的首次Plant Verification。任何State Target都不得直接写Anchor、State、Contact Transition Context或插值进度。

迁移完成后删除旧`PlantCycleConsumed`布尔、旧PlantConfidence状态准入、旧Constraint Weight接触政策及相应Projection字段；重入资格必须由明确Contact Event、Releasing生命周期和Retained Verified Anchor共同表达。Foot Placement Weight继续只表达整个Foot IK作者权重，不替代Contact、Lock或Support。

## Decision 9: 诊断证明阶段责任，不决定行为

封口诊断必须继续按同Frame、Completion、Program、Projection、Rig、Event和Surface lineage组合Source、Path、Context、Goal、Solved和Physical结果，并至少发布：

```text
Path Revision原因与前后目标
Raw Landing/Path Target、Swing Target、Captured Residual、State Output、Vertical Rate Limit、Ground Penetration/Post Constraint与Encoded Goal的逐阶段Correction
Transition Decision、State Target、Interpolation Request/Output/Completion与Post Constraint前后值
Residual基础/截止半衰期与剩余距离
Ground Path identity、Envelope/Anchor穿透、容差、Ground Catchup与Full Lock门控
Formal Step/Foot Height/Contact/Lock/Support输入
Support与Landing Reach区间及交集
Foot Goal夹紧量与LandingReachUnavailable
Target/Solved Extension Ratio与Compression Reserve
```

Diagnostics不得创建Anchor、选择Support、改变Reach、Clamp Goal或执行第二次Query。

Prediction诊断必须补齐`Raw Body Target Current + Raw移动计划Continuation -> Stable Prediction Velocity -> KCC Future Translation -> Raw Landing -> Observation -> Tracking -> Approach Plant Target Preparation -> Contact Verification`，并把移动计划Current作为对照列记录，连同速度差、阈值、EMA响应、最大速度Clamp、状态初始化/重置原因、Tracking状态、Verification Frame/Reason和稳定Plant候选忽略事实。这样实现阶段必须先证明Prediction稳定，再判断Interpolation或Post Constraint，不得把所有抖动归到最终Pose。

采样包固定由同一Recorder发布`每Frame/Side一行的samples.csv + 只保存Ground Contact/Envelope数组项的ground-path-geometry.csv`。几何表必须按Sample、Frame、Completion、Side与Ground Path identity连接主表，不得为每个几何项重复整套Source、State、Goal和Solver列。

每次采样固定写入项目本地持久目录`Diagnostics/FootPlacementRuns/<run-id>/`，不得写入Unity会清理的`Temp`。该目录只承载本地原始诊断，不自动复制、晋升或加入版本控制；需要对账的基线由作者明确选择后再单独归档。

停止录制必须进入唯一`Finalizing`生命周期。Unity主线程只停止捕获并冻结最后一批不可变Frame；后台Finalizer继续排空同一Writer、先封存几何表再以`samples.csv`作为包完成标志、运行同一C# Analyzer与Publisher，最后把Completed或Failed状态发布回Editor。不得增加Python Reporter、同步停止分析路径或仅扩大队列掩盖持续吞吐不足。

## 后续能力的ZZZ补证边界

本change不实施下列能力。后续正式change必须先按上述证据等级确认能采信到什么程度，再把同类责任翻译进项目现有Owner：

| 后续能力 | ZZZ当前证据状态 | 项目唯一归属与限制 |
|---|---|---|
| Foot Pitch/Roll与Ground Normal | 大型姿态/几何合成函数存在，具体Normal、Plant冻结和角速度政策仍是P2/P3 | 动态补证后才扩展现有State Target、Resolved Foot与同一Foot Goal Rotation Weight；不得先增加第二Rotation Writer |
| Heel/Toe双点与Sole几何 | 只确认两组三维量按同一状态权重混合，Heel/Toe业务名未证实 | 动态骨骼绑定补证后进入现有Rig Calibration与唯一Foot Goal，不新增LegIK或Toe Writer |
| Knee方向稳定 | tbik批处理、通道和权重组合存在，但没有直接call证明PIK当前帧调用它 | 若后续补出运行连接，只进入现有FBBIK Bend History和唯一Solver，不做Solver后骨骼修正 |
| Pelvis进阶响应与旋转 | 存在Pelvis词汇与主标量响应，字段级PD、上下速度和旋转参数映射未闭合 | 继续保留项目自己的Pelvis Reach与Critical Spring；除非动态证据闭合，不以ZZZ字段改写现有控制器 |
| Stride Wrapping | min/max变体和若干缓存存在，Stride业务映射仍为P3 | 后续先补证，再决定是否进入正式Foot Motion/Stride意图与Pelvis |
| 楼梯专用表现 | DownStair、Stair与Knee速度的具体调用政策未闭合 | 后续只能由Ground Path坡度与阶沿事实驱动同一Foot/Knee Owner，不建立楼梯IK旁路 |
| Moving Platform | 存在位置差和缓存结构，平台进入/离开业务语义未闭合 | 后续由World Query Backend发布稳定Surface Frame/Delta与World Revision，Foot不直接读Rigidbody |
| Time Scale同步 | `TrySync`复制配置与状态成立，完整Time Scale恢复政策未证明 | 继续使用项目既有Presentation事务；不复制对象内存布局或推断的恢复时序 |
| 状态过渡混合时钟 | 只确认`f`来自当前/下一状态和过渡量，ManualWait实际调用频率、暂停与掉帧行为未闭合 | 继续使用项目既有Presentation Frame、统一Foot Interpolation和Evaluate Barrier，不增加第二更新循环 |
| 动画后Bone Adjust | 相关方法簇存在，具体业务调用链和最终Pose贡献未闭合 | 补证后归Pose Graph正式节点或Constraint Operation，不塞进Foot Runtime或LateUpdate直写骨骼 |

以下内容明确不照搬：预测/普通fallback、Legacy/Advance/Current多套Pelvis并存、全局Footprint缓存、PIK硬编码`cos30°`、固定“每帧两次查询”预算、半径内最高点候选、混淆bitfield布局、多个IK组件和常驻最终Pose低通。项目坡度、查询数量与候选政策由正式Profile、完整Ground Path和World Query合同决定，不能把另一个产品的常量当成普适结论。

## Rejected Alternatives

- 恢复旧Goal Transition或在FBBIK之后加全局平滑。
- 只把传统State Machine拆成多个State类，但继续让State自己执行Enter/Exit、改Anchor和推进插值；文件变多但责任没有分开，新Transition仍需改多个State。
- 建立项目级通用Tween Manager、string channel或字典注册插值；Foot的Event lineage、Anchor接管、Landing截止和Release完成无法由无业务语义的Tween可靠表达。
- 复用Animation Pose Graph的Transition Routing；Pose Source权重换代与Foot接触所有权是两种业务事务，共用路由会让Foot状态依赖动画图内部生命周期。
- 保留旧`CharacterFootStateMachine`作为新模块外的转发或兼容入口；这会让离散状态和Effective Correction继续存在两个可写位置。
- Path identity每帧变化就无条件重置Residual。
- 在KCC Future Body Translation之后平滑世界位置，或在Final Writer前增加常驻全Pose低通；前者会把碰撞结果揉成不存在的轨迹，后者会破坏Contact、Reach与动作响应。
- 为了模仿PIK新增独立Predictive Modifier、普通/预测双路径、全局Footprint缓存、GrounderIK、LegIK或第二骨骼写入。
- 把每帧CurrentSwingFloor命中接进Swing State Target并以`ContinuousTargetChanged`重建Residual；这会让预测输入稳定时悬空脚仍追逐实时地面查询。
- 把完整Swing目标当作Ground Floor，或允许没有Profile预算、没有收敛事实、仍可进入Full Lock的无界穿透。
- 把当前脚水平投影到一维Ground Path上包络并取同距离最高点；该做法无法区分竖直边两侧的真实Surface。
- 直接给膝盖设置最小角度而不处理Foot Goal与Pelvis可达。
- 只降低Foot Goal Weight掩盖超长目标。
- 在Support和Foot Height未迁移前单独延长Landing或接入Lock Weight。
- 让Primary Support/Pelvis读取Foot State、Lock Mode或可变Context。
- 同时保留旧PlantConfidence和正式Contact/Lock并择优输出。
- 增加第二Landing状态、第二Anchor、第二Goal Set或第二FBBIK。

## Migration

1. Foot Motion数据change已经完成用户验收并归档，记录当前Curve/Event identity。
2. 保留已经完成的Releasing到Swing顺序修正和identity触发清理，记录它们没有明显改善整体Path抖动。
3. 对最新代表事件逐阶段发布Raw Target、Residual、State Output、Floor与Goal事实，修复第一个已证明的不连续阶段。
4. 固定当前State、Transition边、目标、Residual和Post Constraint映射；建立分型Context、纯Transition Resolver、State Target Resolver与唯一Interpolation Runtime，在根事务内逐项迁移等价行为。
5. 切换固定帧内管线并删除旧`CharacterFootStateMachine`、三套Residual、Contact Progress、分散HalfLife推进与所有兼容入口；从此只有Transition Runtime写离散State/Anchor，只有Interpolation Runtime写Effective Correction。
6. 发布唯一Foot Motion Runtime Frame，用正式Step Time/Distance替换旧Prediction时域并删除旧Step消费者。
7. 在同一根Bank增加左右脚共享Prediction Motion State，以本项目Replay证明的阈值、EMA和最大速度分别稳定committed Body Target当前速度与移动计划Continuation；只把稳定速度交给唯一KCC Future Body Translation，并补齐Raw/Stable/Translation及移动计划Current对照诊断。
8. 让PreSwing、Swing与Approach Contact保持Landing Tracking，并由统一Interpolation持续准备Plant目标；首次正式Contact Rising执行一次Plant Verification并建立冻结Anchor。
9. 在唯一Interpolation中按ZZZ精确顺序分离Plant Target高度历史与Effective Correction历史，增加`MaximumVerticalTargetSpeed`并让现有`MaximumVerticalCorrectionSpeed`只服务第二层Correction限速；删除Acquire进入帧和Post Constraint的立即抬升，确保既有Goal权重只在两层限速后混回动画基线。安装Ground穿透预算与Landing Lock完成门控后，再让Foot Height进入Swing并删除旧Baseline Height Error政策。
10. 让Support进入Resolved Foot、Primary Support和Pelvis，但保持Lock生命周期不变；为唯一Pelvis Spring增加非对称速度边界。
11. 增加双腿Reach交集、最小Landing压缩余量、Goal夹紧与typed拒绝。
12. 用Contact、Lock Mode与Lock Weight替换旧PlantConfidence生命周期并删除旧字段。
13. 显式重建Corin Projection、Float32与Fixed产品，完成编译、诊断重放和严格OpenSpec校验。
