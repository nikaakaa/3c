# Design: 在唯一Foot事务内稳定Path并保证Landing可达

## Context

当前重构已经把Landing Lifecycle、Ground Path、Anchor、Support、Pelvis与Goal收进唯一`CharacterFootPlacementModule -> Resolved Foot Pair`根事务，并完成纯Transition Resolver、唯一Transition Runtime、纯State Target Resolver、统一Interpolation Runtime与Post-Interpolation Constraint阶段拆分。旧`CharacterFootStateMachine`、分散Residual/Progress和兼容入口已经删除；这个唯一生命周期和Owner边界继续保留。Prediction已经以零诊断回归接管唯一KCC Future Translation，当前剩余重点是Landing/Lock垂直接管仍可绕过Interpolation，以及正式Foot Motion、Support、Reach和Contact/Lock旧语义尚未全部迁移。

诊断显示三类问题具有不同边界。Path侧已经证明identity单独触发Residual重置不合理，但删除该触发后用户观察到的整体抖动基本不变；`20260828-184607-138-d23494c9824a42a89c9973d567305442`进一步证明同Event、同RootLocalLanding在720度/秒转向下因瞬时世界速度变化令Future Body Translation单帧移动约36厘米，查询切换Surface，Next Landing移动约39.7厘米并由Post Constraint产生约17.5厘米Correction跳变。`20260829-084258-427-2e96ab5155fd4730a74be4732c90493f`已经让Prediction以零诊断回归进入唯一运行链，因此当前首要断点转为Landing/Lock垂直接管：Acquire进入帧与Ground Constraint都能绕过Interpolation同帧抬脚。Landing直腿则仍是Foot Goal在Pelvis无有效Reach协调时超过腿长，FBBIK只能把膝盖夹到伸展极限。设计必须分别处理Prediction时间连续、Landing提交、垂直视觉连续、允许的Ground穿透预算和腿链可达，不能用一个最终Pose平滑器互相交换问题。

## ZZZ PIK参考口径

`D:\ZZZ_Dump\PIK分析包`作为本change及后续Foot能力的实现参考。已经由反汇编正文与运行时元数据交叉支持的成熟控制顺序包括：预测输入阈值、EMA与上限，生产侧突变拒绝，无效查询冻结更新，固定容量候选与确定性选择，Plant/Lock阶段不重新定位，可信小变化再做高度与姿态混合，Pelvis PD与非对称速度边界，Knee方向稳定，以及移动平台、Stride与Time Scale同步的独立责任。

实现阶段 MUST直接对照这些已确认职责与数据流，优先复用成熟控制结构，不为同一问题重新发明平行算法。PIK资料不成为源码或Runtime依赖；混淆类名、地址、全局缓存布局、硬编码常量、预测/回退双路径、Legacy Pelvis和并存IK组件不得进入项目。早期call/jmp目标计算错误已经勘误；仍未完成名字映射的函数只能作为行为线索，不得写成精确方法合同。

项目已有能力继续优先：正式Foot Motion lineage、根Bank双页事务、canonical Observation、完整Ground Path/Envelope、分型Transition/Interpolation/Post Constraint、Resolved Foot Pair、唯一Goal Set/FBBIK/Writer和封口诊断不得退化为PIK式单体组件。采用PIK表示把成熟政策翻译进这些现有Owner，不是复制其对象结构。

### 证据等级与采用规则

ZZZ资料的“成熟”必须按证据等级使用，不能把字段名、推测和完整算法混成一个可信度：

| 等级 | 可采信内容 | 本项目使用方式 |
|---|---|---|
| A：算法已确认 | `PIK预测IK_核心算法.md`、`状态机转移表.md`、`ManualWait时序.md`、`查询槽实现.md`、`姿态混合尾段.md`、`走动IK与SmoothKnee.md`与`七项完成报告.md`已经给出阈值/EMA/速度上限、输入拒绝、Plant边沿、驻留计时、回弹强制刷新、固定步目标到渲染帧相位插值、Heel/Toe双点混合、高度变化率Clamp、Plant旋转冻结、Footprint记录与物理组件CrossCheck、Pelvis非对称速度、SmoothKnee开关消费和LateIK通道驱动证据 | 可以直接参考控制顺序和责任分离，但仍要翻译成现有typed Owner、正式Event lineage和根事务，不复制地址、对象布局、双路径或候选政策 |
| B：能力存在但命名或完整公式未闭合 | Stride Wrapping数据流与Moving Platform刚体Delta已经达到结构级，DownStairWeight、部分Bone Adjust和平台完整进入/离开政策仍保留推断或区域外缺口 | 只用于决定后续模块归属；不得把结构推断写成当前规范性算法，也不得先增加未被当前业务使用的参数 |
| C：仍不可作为实现合同 | 状态机外部B/D输入的最终C#语义名、激活角色的真实运行参数、相等比较容差和剩余混淆方法仍未闭合 | 不进入当前公式和默认值；构造函数常量及未激活活体实例只可作为量级参考，不得直接写成Corin正式配置 |

### 成熟结论逐项对账

| ZZZ结论 | 本项目采用结论 | 当前归属 |
|---|---|---|
| 预测先做死区、EMA、速度上限，再外推 | 本change直接采用同型控制顺序；当前输入是committed Body Target世界速度，Continuation输入是committed移动计划下一段世界速度，KCC仍是唯一Future Translation | shared Prediction Motion State |
| 生产侧异常记录先拒绝，不让滤波器吞坏数据 | 本项目先由Foot Motion/移动计划typed lineage、有限值和范围合同接纳输入；非法输入发布typed unavailable且不得推进稳定状态。停止边界缺失移动计划不在Foot Placement猜成零速度，生产侧显式静止计划后续单独闭合。合法但急剧转向是真实运动，只进入EMA，不套用语义尚未确认的PIK相对值公式 | Frame Input验证、Prediction Motion State |
| 查询无效时不更新目标 | Accepted与Rejected Observation保持各自Key；Rejected不生成新Landing。Tracking只可继续持有同Event较早Accepted Landing及其原始lineage，Committed则继续使用承诺值 | Observation Page、Landing Context |
| Plant期间不响应普通目标重定位 | Approach Contact把Landing从Tracking提交为Committed；Current Contact再Promote，Anchor建立后只由正式Contact/Lock迁移 | Landing Context、Transition Runtime |
| Contact/Lock使用上一帧输入检测边沿，并以驻留计时和回弹事件识别临界点反复进入 | 顶层`Swing / UnlockedSupport / Landing / Locked / Releasing`不增加状态；同一根Bank内部增加Contact边沿Context。PIK回弹会绕过普通速率门强制更新，而不是丢弃重入；本项目将其翻译为Releasing期间同Event的受限Refresh | Contact Transition Context、Transition Resolver/Runtime |
| 候选必须固定容量且确定性选择 | 直接保留固定容量、稳定identity和边界保护；候选政策使用项目的canonical最近合法Surface，不复制PIK“半径内最高点”，因为项目还要构造Last-to-Next完整Ground Path | World Query Adapter、Observation |
| `FootLockCrossCheck`是Footprint记录与物理组件的交叉验证，不是左右脚互锁 | 对应现有World Query候选的Collider、组件、Surface identity与合法性过滤；继续由Observation Adapter一次验证，不建立Resolved Foot Pair的左右脚可变互读 | World Query Adapter、Observation |
| 脚锁进入/离开、Grounded进入/离开必须有独立持久事实 | 本项目用正式Contact、Lock Mode/Weight、Approach Contact边界和typed Transition表达，不再从脚高或速度推导第二套Grounded/Lock状态机 | Foot Motion Frame、Transition Resolver |
| Lock Damping/Stiffness与目标接管分开 | PIK参数对和目标相位混合已经确认，但其产品数值不直接移植；本change由正式Lock Weight选择统一Interpolation Policy，沿用唯一Interpolation State，不再增加平行Lock弹簧 | State Target、Interpolation Runtime |
| `currentFootprint / nextFootprint`及对应Normal分开保存 | 直接对应LastLanding、NextSwingLanding、Surface与Normal；Contact Landing和Swing Path Landing继续分权 | Landing Context、Ground Path |
| Ground Point、Air Point与`distFraction`分开 | 对应Ground Path两端、连续Envelope与正式Swing Progress；Foot Height只叠加在Envelope上，不再从Transform猜脚步阶段 | Ground Path、State Target |
| PIK热循环消费缓存足迹，所谓地面采样槽实际是TransformPoint与组件检查 | 项目不把正式Sphere/Capsule World Query改写成TransformPoint；只吸收“世界事实由唯一上游生产、Foot热循环消费不可变结果”的分层，继续保持一条Prediction Query与一条Ground Path Query责任，不增加Ordinary fallback | World Query contract、Observation、Ground Path |
| 每脚事件位、持久位、计数器和阈值分开 | 采用其职责分离，不复制bitfield布局；一次性事实进入Transition/Diagnostics，跨帧事实进入typed Context，时间连续量只进入Interpolation State | 根Bank分型Context |
| ManualWait每渲染帧把固定步当前态向目标态按相位`f`混合 | 对应现有唯一`CharacterFootInterpolationRuntime`的表现帧连续化责任；不新建第三插值器。正式Step Time决定截止，typed Policy决定Swing/Acquire/Release响应，高度变化率需要时也必须成为同一Interpolation Policy | Interpolation Runtime |
| 可信小变化最后做位置和姿态混合 | 本change覆盖位置链：Prediction Motion稳定输入，Foot Interpolation唯一连续化输出并按ZZZ同型`±高度变化率 × dt`限制Component Up变化；Ground Envelope/Anchor只发布目标、穿透与Lock门控，允许Profile显式的小范围穿透。Reach不可达仍最后硬限制。Foot Rotation尚未成为正式Goal合同，姿态混合留到后续 | Prediction、Interpolation、Post Constraint |
| Pelvis用独立连续控制并限制上下速度 | 保留现有唯一Critical Spring，只吸收已确认的非对称最大上/下速度和边界外速度清除；字段级Stiffness/Damping/Advance不另建第二Pelvis控制器 | Pelvis Builder/Spring |
| Knee单独稳定，不由Foot位置滤波代替 | 方向归唯一FBBIK Bend History；本change只先消除超长Goal与零压缩余量，不做Solver后骨骼覆盖 | Landing Reach、FBBIK |
| 热路径固定缓冲、数组边界和NaN/Inf保护 | 现有根Bank、固定容量Observation/Workspace继续作为更强等价实现；新增Prediction、Landing和Pelvis状态也必须固定布局、无每帧托管分配、入口有限值校验、容量溢出typed失败 | Runtime storage、validator |
| 调试按预测、普通命中、最终命中、目标、动画分层 | 现有正规化诊断比Gizmo开关更强，继续记录每阶段正式事实并保持只读，不复制PIK的全局Debug开关 | Diagnostics Recorder/Projector |

最新PIK证据显示它不是笼统“两次平滑”，而是按责任串联：Prediction速度死区/EMA/上限、固定步当前态到目标态的渲染帧相位混合、Heel/Toe双点混合、高度Delta变化率Clamp、Plant进入帧旋转对齐与稳定Plant冻结，以及独立Knee权重混合。它的Plant目标冻结与高度限速同时存在，没有“最终脚位无条件瞬移到地面”的证据。本项目把前者翻译进共享Prediction Motion State，把Foot位置连续化和`MaximumVerticalCorrectionSpeed`收进唯一`CharacterFootInterpolationRuntime`；`GroundPenetrationTolerance`允许普通帧存在有限视觉误差，`LandingLockCompletionTolerance`阻止尚未收敛的脚进入Full Lock。Post Constraint只测量Ground误差并执行Reach硬夹紧，不再成为第二个可见Correction写入者。Pelvis Spring与FBBIK Bend History继续只稳定各自拥有的量。

PIK已经确认Foot Rotation在Plant进入帧执行一次地面对齐、稳定Plant期间冻结、离地后恢复，并没有持续角速度低通；`OnLateAnimationIK`也已定位为通道范围驱动TwoBoneIK批处理编排。它仍不证明“无论什么情况，每帧把最终全身Pose重新覆盖平滑”。当前`CharacterFootInterpolationRuntime`只承担Foot Effective Correction连续化，不承担Animation Source切换、全身骨骼覆盖、Pelvis或Knee平滑；Animation Pose换代继续由Pose Graph正式Transition/Inertialization处理。

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
-> Landing Tracking / Committed / Promoted收口
-> Foot Height接入Swing
-> Support接入Primary Support/Pelvis与非对称速度边界
-> Landing Reach闭合
-> Contact/Lock接入Transition、State Target与Interpolation
```

每一步只切换一个业务定义。对应旧字段在同一步删除；未轮到的正式字段可以存在于不可变Frame，但不得影响行为。架构拆分已经完成并继续保持单一Owner。Step Time先成为Prediction唯一时域，再驱动已经连续的Residual截止；共享Prediction Motion只稳定committed Body Target当前速度与移动计划Continuation，不修改正式Step Time、RootLocalLanding、Visible Rotation或KCC世界碰撞。随后先消除Acquire与Ground Constraint绕过Interpolation的同帧抬升并安装穿透/Lock门控，再迁移Foot Height。Landing提交必须在Contact/Lock迁移前建立，因为Approach Contact后的Next Landing要先成为不可再被普通预测改写的正式承诺。Contact/Lock最后迁移，因为延长Landing前必须先让Swing接近Anchor并让Pelvis拥有有效Support/Reach输入。

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

顶层五态保持不变，不增加`Rebound`、`Blocked`或另一套Grounded状态。根Bank内部新增分型`Contact Transition Context`，只保存上一帧正式Lock请求、距最近Contact边沿的秒数、最近Contact Event identity和最近释放的Contact Event identity。`Contact Rising / Contact Falling / Same-Event Reentry Refresh`只属于本帧不可变Transition事实或Reason，不成为持久顶层State。相比PIK依赖匿名输入与驻留阈值，本项目还拥有正式Event和Anchor lineage：同Event在Releasing期间重新请求Lock时必须强制刷新Transition与Target，但只可复用仍保留的Anchor和Committed Landing；Release完成、Anchor已清除后不得复活旧Event。紧接着到来的新Event必须按自己的Committed Landing正常准入，不受上一Event回弹事实影响。

`CharacterFootTransitionResolver`是纯决策器，只读取不可变Frame、Observation与上一Committed typed Context，返回固定`CharacterFootTransitionDecision`。Decision至少包含Source、Target、Reason、Event lineage、执行Phase、Anchor Command与Interpolation Policy identity。允许边固定为：

```text
Swing -> Landing | UnlockedSupport
UnlockedSupport -> Landing | Swing
Landing -> Locked | Releasing
Locked -> Releasing
Releasing -> Landing | Swing
```

输入驱动的边在Pre-Interpolation阶段执行；只有依赖本帧插值完成事实的边在Post-Interpolation阶段执行。`Releasing -> Landing`只允许同Event Contact Rising、原Anchor仍保留、Committed Landing与Reach/Lock准入继续合法时在Pre阶段执行；它必须Retain原Anchor并从当前Effective Correction连续重入，不得Create Anchor、重查Landing或把Interpolation清零。`Releasing -> Swing`属于Post阶段，完成后必须用Swing输出分类执行同帧Post Constraint。系统不得循环求Transition直到稳定，也不得让状态目标或插值器暗中改State。唯一Transition Runtime负责应用Decision、改写离散State、执行Anchor Create/Retain/Release命令并记录原因；其他模块不得写这些字段。

Contact边沿事实 MUST由同一Resolver按上一Committed Contact Transition Context与本帧正式Contact、Lock Mode和Event identity纯计算。唯一Transition Runtime在应用Decision时同时更新该Context；State Target、Interpolation、Post Constraint与Diagnostics不得写回边沿历史。Reset、Retarget、Source lineage变化和根Bank Discard分别按现有事务语义清空或保留Committed Context，不得在失败帧推进边沿秒数或消费Event。

`CharacterFootStateTargetResolver`按已经确定的State纯计算本帧目标，输出目标Correction、Reference、Contact/Support/Reach意图和固定typed Interpolation Request。它不得读取或推进Delta Time、Residual、Progress与上一输出，也不得查询世界。Swing/UnlockedSupport目标只来自Ground Path、Envelope与正式Foot Height；Landing/Locked目标来自唯一Anchor和正式Contact/Lock；Releasing只回到原始动画Swing目标。

`CharacterFootInterpolationRuntime`是Effective Correction连续性的唯一所有者。它只接受`Previous Effective Correction + State Target + typed Policy + Delta Time`，持有一份统一Interpolation State并发布Output、Residual和Completion。现有Swing Residual、Acquire Residual、Release Residual、Contact Progress与散落的HalfLife推进必须迁入这里；政策固定为直接跟随、Residual Half-Life与正式Weight接管等有业务含义的typed策略，不提供string key、字典注册、任意曲线回调或项目级通用Tween。所有Policy在写回Effective Correction前都必须把Component Up变化限制到`MaximumVerticalCorrectionSpeed × Delta Time`；`AcquireByWeight`进入帧不得调用`RaiseToMinimum`，Lock Weight达到1也不得把未收敛Residual清零。统一的是执行生命周期、状态所有权和垂直变化率边界，不是强迫Swing、Landing与Release使用同一条曲线。

根`CharacterFootStateContext`收敛为一组分型数据块：`Discrete State Context`只存当前State与最近Transition，`Contact Context`只存Anchor和Lock响应，`Contact Transition Context`只存边沿与已消费Event历史，`Interpolation State`只存上一目标、Effective Correction、统一Residual与完成事实，Landing与Observation继续使用各自typed Page。所有数据块仍由同一个Pending/Committed根事务一次Seal或Discard，不建立独立生命周期。

Post Constraint只在插值后消费结果。Ground Path Envelope或Contact Anchor部分只测量连续输出的穿透深度、判断是否位于`GroundPenetrationTolerance`内并发布`GroundCatchup`与Full Lock门控；它不得调用`RaiseToMinimum`、不得修改Effective Correction、不得写回Interpolation历史。普通帧允许不超过容差的轻微穿透；若某次状态交接继承了超预算误差，输出必须继续受同一竖直速率限制并向可信目标收敛，期间不得Full Lock，也不得用同帧抬升掩盖。Landing Reach和有限值边界仍可硬夹紧不可达Goal。Ground测量与Reach夹紧都不得回写State Target、Residual或Transition。

## Decision 4: 先定位Path同帧放大，再分离连续目标与Envelope安全

FutureLanding世界事实固定拆成`Committed Body Target Current + 移动计划Continuation -> shared Prediction Motion State -> KCC Future Body Translation -> Raw Landing Candidate -> Query Admission -> canonical Landing Observation -> Landing Tracking/Commit`。Prediction Motion State属于Foot根Bank且左右脚共享一份；它不得进入Gameplay、World State、rollback或网络packet。状态至少保存初始化标志、稳定当前速度、稳定Continuation速度、移动计划Generation、Body Reset Sequence与Prediction Source identity，并随根事务Seal或Discard。

Prediction使用ZZZ同型控制律，不重新设计第二Trajectory算法。当前目标取committed Body Target世界速度，Continuation目标取committed移动计划下一段世界速度；两者分别计算`TargetVelocity - StableVelocity`。差值不超过Profile显式`PredictionVelocityDeltaThreshold`时保持稳定速度，超过时按`PredictionVelocitySmoothSpeed * PresentationDelta`执行有界EMA响应，再把结果限制到`PredictionMaximumSpeed`。三个配置必须为有限正值、纳入Profile Revision且由Corin正式序列化；缺失或非法时整项typed unavailable，不提供默认值。首次合法输入直接以对应正式速度初始化，避免从零产生启动滞后；Body Reset、Retarget、移动计划Generation或Prediction Source变化清空状态，普通Landing Event、Animation Source和左右脚Step换代不得重置角色级Prediction Motion。移动计划Current Velocity只作为诊断对照，不得替换KCC当前运动起点。停止边界缺失移动计划时保持上一Committed Prediction状态且不生成本帧Future Translation；显式静止计划的生产侧闭合不在本change实现，Foot Placement不得补零。

唯一KCC Future Body Translation继续负责真实世界碰撞，只是请求中的当前与Continuation平面速度改为稳定速度。左右脚按各自正式Step Time读取同一Pending Workspace；RootLocalLanding仍只乘本帧Visible Rotation，不预测Future Yaw。Prediction不得复制KCC、创建低速普通路径或在KCC结果后另做位置低通。

Raw Landing在`Tracking`阶段仍从每帧不可变Frame Input重新投影；Committed Observation Page保存上次真实查询使用的Side、Landing Event、Source Sample、Source Cycle、按1毫米量化的Raw Landing、按`1e-4`量化的Component Up、Profile Revision与World Revision。当前Candidate与该查询快照的世界位移累计不超过`PredictionInputAccumulationDistance`且Up夹角不超过`ComponentUpChangeAngleDegrees`时复用同一Committed Observation Page，不更新累计基准也不查询。

Corin显式使用5厘米累计位移和1度Up角度。距离配置必须为正且不得超过Landing Sphere半径；因此本change不采用10厘米。Event、Source Sample、Source Cycle、Profile Revision或World Revision变化不受阈值限制，必须执行一次新查询。正式Foot Lock Mode处于`Sliding`、正在准备接触准入时，只要canonical预测输入identity变化也必须刷新Observation，避免缓存落点误差与动画脚底残差相加后越过8厘米Lock准入；输入identity未变时仍复用，不重复查询。超过任一阈值时同样恰好查询一次；SphereCast使用当前Candidate生成的新canonical Key反量化几何，并只选择canonical最近合法Surface。Accepted与Rejected查询结果都属于不可变Observation；Pending根事务失败时不得提交新Page。

上一Committed Surface、Frame、Authority Tick、Trajectory Generation、Future Translation Source、Foot State、Residual与查询输出不得进入Query Admission、Observation Key或候选选择。上一Surface不得传入World Query或改变候选选择。`Tracking`阶段新查询命中不同Surface时只换代NextSwingLanding，不得覆盖Current Anchor；同Surface才通过独立`LandingAcceptanceDistance`决定是否替换点位。Accepted与Rejected Observation必须保持各自Key和结果；新Rejected Observation不得冒充上一Accepted结果，但同Event Landing Tracking可以继续持有此前已经Accepted的NextSwingLanding，诊断必须同时记录当前Rejected Observation和被保留Landing的原始lineage。查询前累计阈值、1毫米Key量化和查询后Landing接受距离是三个独立定义，不得合并。

Landing Context在现有唯一生命周期内表达`Empty / Tracking / Committed`，Promotion继续作为当帧输出事实而不是第二状态机。PreSwing和早期Swing进入Tracking；首次Accepted Observation建立NextSwingLanding，后续可信Observation可按上述规则换代。正式Foot Motion进入`ApproachContactToLanding`且已经存在同Event Accepted Landing时，NextSwingLanding原子进入Committed；进入Committed后仍可为诊断重投影Raw Candidate，但普通速度、角度、Source采样和Surface变化不得再创建Observation Key、执行SphereCast、换点或重建Ground Path。该Event成为Current Contact Event时，Committed Landing晋级LastLanding并发布Promoted Contact Landing。

Approach Contact到达时没有同Event Accepted Landing必须发布typed unavailable，不使用Animated Sole、默认地面、另一Event或旧Surface建立承诺。Reset、Retarget、World Revision变化或Backend重建必须使Committed Landing失效；普通移动计划速度变化不得。正式`Sliding`接触准入刷新只允许发生在Tracking，Committed阶段使用已经承诺的Landing执行Lock距离和Reach准入，误差超限就拒绝Lock，不以晚期重查移动脚点。

当前FootPlacementSurface在World Query Backend生命周期内视为静态，Backend发布固定非零World Revision；Reset、Retarget或Backend重建必须清空每脚Observation Page。移动平台和运行时Surface变更不在本change范围。

Ground Path Input identity只表示查询输入lineage，不单独触发Residual重置。Path Revision只由Event、Path可用性或Accepted Landing端点变化产生；同一Event、同一Landing与同一Envelope内的Phase目标变化不得发布Path Revision。正式Swing目标变化超过独立`PathRevisionDistance`时，Interpolation Runtime可以发布分型`TargetTrackingApplied`并捕获`PreviousOutput - NewTarget`，但不得把它记录为Path Residual重建。原始Builder目标与State Target继续分列诊断，不得互相改名覆盖。`PathRevisionDistance`不得控制Landing接受、Residual截止或Release完成；后二者分别只读取`SwingResidualTolerance`与`ReleaseCompletionTolerance`。

Accepted Swing Motion必须携带与同一Ground Path Event匹配的typed Swing Path Landing Reference。Promoted Landing与按当前Step解析的Landing只属于Contact/Anchor准入，不得门控Swing Path可用性或提供Swing Residual的Landing Point。同帧旧Event完成并Promote、下一Swing Event已经Accepted时，Foot根事务必须同时保留旧Contact Landing和新Swing Path Landing，不得把Path发布为一帧不可用。

Path诊断必须先在同Frame、Side与Event lineage下记录`Raw Landing/Path Target -> Swing Target -> Captured Residual -> State Output -> Vertical Rate Limit -> Ground Penetration/Post Constraint -> Encoded Goal`。任一后继阶段的单帧Correction变化明显大于直接输入变化时，必须先修复第一个产生不连续或放大的阶段；不得通过Goal低通或Step Time截止把该跳变藏到无Owner的后处理器。统一Interpolation的正式限速属于目标接管政策，必须同时记录追赶欠账和穿透代价。

在上述Correction链已经连续后，普通Swing目标使用统一Interpolation State中的Residual。基础半衰期仍来自Profile；当Residual大于`SwingResidualTolerance`时，Interpolation Runtime按剩余Step Time计算保证在Landing前收敛到容差所需的半衰期，并取它与基础半衰期的较小值。没有有效Step Time时不得猜测截止时间，只能发布明确输入不可用。Step Time只解决Landing前仍有Residual欠账，不负责改变Raw Target、重选State Output或修正同帧放大。Releasing完成只使用独立`ReleaseCompletionTolerance`，不得因调整Swing截止精度而改变Release退出时机。

Swing的Ground Path Envelope同时服务连续轨迹目标和插值后的Ground误差测量。Post Constraint MUST消费本帧Accepted Swing Motion已经采样的同一Envelope Point和Path identity，不得重新Raycast、SphereCast或读取另一Surface。Envelope随Swing Progress连续采样；只有正式Path Revision才能改变其几何。Interpolation Output低于Envelope时，系统必须记录穿透深度、容差内/外分类、竖直限速和预计追赶时间，不得立即抬升或写回Interpolation历史。Landing/Locked同样以冻结Contact Anchor测量；Full Lock只有在正式Weight完成、位置残差不超过`LandingLockCompletionTolerance`且穿透不超过`GroundPenetrationTolerance`时成立。

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

Primary Support只消费Resolved字段，不读取Foot State、Lock Mode或Context。Support曲线为0时不得由相对大小归一成1；双脚都无正式Support时Pelvis进入现有typed Release，而不是猜一只脚承重。

## Decision 7: Landing Reach先协调Pelvis，再限制Foot Goal

Foot Motion Profile新增必须显式序列化的米制`MinimumLandingLegCompressionReserve`并纳入Profile Revision。缺失、非有限或越界时整项typed invalid，不提供代码默认值或旧配置补全。State Target Resolver与Resolved Foot为Landing脚发布typed Reach Request：Hip、目标Ankle、Leg Length、最小压缩余量、Landing Event和有效世界Reference。它不是第二Support、第二Anchor或第二状态机。

Pelvis Builder同时计算Primary Support腿和Landing腿允许的Pelvis沿Up区间：

```text
FeasiblePelvisInterval = SupportReachInterval ∩ LandingReachInterval
```

交集存在时，Pelvis Target与Spring必须限制在交集内。现有Critical Spring继续是唯一Pelvis连续状态，并增加Profile必须显式序列化的`PelvisMaximumUpVelocity`与`PelvisMaximumDownVelocity`。Spring积分后先把Velocity限制在`[-MaximumDownVelocity, MaximumUpVelocity]`，再推进Output并限制在Reach交集；Output撞到上/下边界且Velocity继续朝外时必须清除对应方向速度。Support换代、坡度变化和Target跨越Output继续使用现有显式Handoff与Velocity Reset，不增加第二Pelvis平滑器。

交集不存在时，系统先保持Primary Support安全，再把Landing Foot Goal夹紧到保留最小压缩余量的最大可达点，发布`LandingReachUnavailable`，并禁止该脚进入Full Lock。它可以保持Landing、Sliding或进入Releasing，但不得把超长目标交给FBBIK后仅靠腿伸直夹紧。

该政策的业务取舍是：不可同时满足双腿时允许短暂未完全踩实，换取不出现明显直腿、骨盆瞬移或关节奇异。

## Decision 8: 正式Contact与Lock驱动Transition与统一插值

正式Contact有效且同Event Lock Mode首次从Unlocked进入Sliding或Locked、Committed Landing合法且该Event尚未消费时，Pre-Interpolation Transition Resolver发布`Swing/UnlockedSupport -> Landing`与Create Anchor命令。Transition Runtime只建立一次Anchor，并把本次Contact Rising与Event写入同一Contact Transition Context；State Target Resolver以该Anchor生成Landing目标，Interpolation Runtime保存当前Output到Anchor的Residual，并按正式Lock Weight推进接管。Mode、Weight和Event不一致时发布typed invalid，不按旧PlantConfidence继续。

正式Locked Mode和完成的Lock Weight触发`Landing -> Locked`，并使用`FullAnchor Response`目标。已锁脚回到Sliding Mode时保持同一顶层Locked生命周期和同一Anchor，只切换内部Sliding Response目标。Mode回到Unlocked或Contact正式退出时触发`Landing/Locked -> Releasing`，记录Contact Falling和最近释放Event；Release仍由Interpolation Runtime这个唯一Effective Correction Owner处理。

Releasing期间同Event再次出现Sliding或Locked请求，且原Anchor仍保留、Committed Landing、Lock距离与Reach仍合法时，Resolver必须发布typed `SameEventContactReentryRefresh`并执行`Releasing -> Landing`。Transition Runtime只Retain原Anchor；State Target立即重新计算同Anchor目标；Interpolation Runtime从当前Effective Correction连续接管，不得重置为零或重新捕获不存在的世界目标。若Release已经完成、Anchor已清除，旧Event不得复活；新Event即使紧接上一边沿也按自己的Committed Landing正常准入。任何State Target都不得直接写Anchor、State、Contact Transition Context或插值进度。

迁移完成后删除旧`PlantCycleConsumed`布尔、旧PlantConfidence状态准入、旧Constraint Weight接触政策及相应Projection字段；重入资格必须由明确Contact Event、Releasing生命周期、Retained Anchor和Committed Landing共同表达。Foot Placement Weight继续只表达整个Foot IK作者权重，不替代Contact、Lock或Support。

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

Prediction诊断必须补齐`Raw Body Target Current + Raw移动计划Continuation -> Stable Prediction Velocity -> KCC Future Translation -> Raw Landing -> Observation -> Tracking/Committed Landing`，并把移动计划Current作为对照列记录，连同速度差、阈值、EMA响应、最大速度Clamp、状态初始化/重置原因、Landing Tracking状态、Commit Frame/Reason、晚期Candidate与是否被忽略。这样实现阶段必须先证明Prediction稳定，再判断Interpolation或Post Constraint，不得把所有抖动归到最终Pose。

采样包固定由同一Recorder发布`每Frame/Side一行的samples.csv + 只保存Ground Contact/Envelope数组项的ground-path-geometry.csv`。几何表必须按Sample、Frame、Completion、Side与Ground Path identity连接主表，不得为每个几何项重复整套Source、State、Goal和Solver列。

每次采样固定写入项目本地持久目录`Diagnostics/FootPlacementRuns/<run-id>/`，不得写入Unity会清理的`Temp`。该目录只承载本地原始诊断，不自动复制、晋升或加入版本控制；需要对账的基线由作者明确选择后再单独归档。

停止录制必须进入唯一`Finalizing`生命周期。Unity主线程只停止捕获并冻结最后一批不可变Frame；后台Finalizer继续排空同一Writer、先封存几何表再以`samples.csv`作为包完成标志、运行同一C# Analyzer与Publisher，最后把Completed或Failed状态发布回Editor。不得增加Python Reporter、同步停止分析路径或仅扩大队列掩盖持续吞吐不足。

## 后续能力继续参照ZZZ的边界

本change不实施下列能力。后续正式change必须先按上述证据等级确认能采信到什么程度，再把同类责任翻译进项目现有Owner：

| 后续能力 | ZZZ成熟参考 | 项目唯一归属与限制 |
|---|---|---|
| Foot Pitch/Roll与Ground Normal | 已确认Normal×参考帧、Plant进入帧对齐、稳定Plant冻结、离地恢复，旋转本身没有角速度低通 | 扩展现有State Target、Resolved Foot与同一Foot Goal Rotation Weight；不得增加持续旋转低通或第二Rotation Writer |
| Heel/Toe双点与Sole几何 | 已确认当前态与目标态都保存Heel/Toe双位置并按同一相位分别混合 | 现有Rig Calibration与唯一Foot Goal；双点只形成同一Sole约束，不新增LegIK或Toe Writer |
| Knee方向稳定 | 已确认TwoBoneIK批处理、全局相位权重、`clamp(f × weight × d1 + d2)`形式及Force开关对批处理全局权重的门控 | 现有FBBIK Bend History和唯一Solver；以Extension Ratio与Ground坡度选择政策，不做Solver后骨骼修正 |
| Pelvis进阶响应与旋转 | Max Stiffness/Damping、Adjustment Advance、Foot Min Distance、Pelvis IK Rotation/Weight | 现有Pelvis Target、唯一Spring与同一Goal Contribution；先证明现有Critical Spring缺口，再决定是否替换参数化，不并存两套控制器 |
| Stride Wrapping | `EnableStrideWrapping`、`MinStrideScale` | 正式Foot Motion/Stride意图与Pelvis；只调整表现步幅，不修改Gameplay Body或另建Trajectory |
| 楼梯专用表现 | Down Stair Weight、Stair状态、上下行Knee速度 | Ground Path坡度与阶沿事实驱动同一Foot/Knee政策，不增加楼梯专用IK或动画旁路 |
| Moving Platform | 平台进入/离开状态、离台时脚在空中/地面、Rigidbody-to-Transform Delta | World Query Backend发布稳定Surface Frame/Delta与World Revision，Landing/Anchor按统一Surface lineage消费，Foot不直接读Rigidbody |
| Time Scale同步 | `TrySyncTimeScale`逐骨骼快照与备份槽 | Presentation事务统一重置或重投影Prediction、Interpolation、Pelvis、Bend与Pose Inertialization；不复制对象内存布局 |
| 固定/渲染帧相位 | 已确认固定步当前态到目标态由每渲染帧相位`f`混合，暂停冻结、掉帧趋近1、Time Scale通过备份状态限制瞬变 | 继续使用项目既有Simulation Tick、Presentation Frame、统一Foot Interpolation和Evaluate Barrier；不增加协程、第二更新循环或PIK式对象内固定步副本 |
| 动画后Bone Adjust | `LiftBonesAccordingIK`、Velocity、Scatter2D、OneBoneLayer与2Target Pose方法簇 | 归Pose Graph正式节点或Constraint Operation，不塞进Foot Runtime，也不通过LateUpdate直接写骨骼 |

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
7. 在同一根Bank增加左右脚共享Prediction Motion State，以ZZZ同型阈值、EMA和最大速度分别稳定committed Body Target当前速度与移动计划Continuation；只把稳定速度交给唯一KCC Future Body Translation，并补齐Raw/Stable/Translation及移动计划Current对照诊断。
8. 把Landing Context收敛为Tracking与Committed所有权；Approach Contact提交Next Landing，晚期普通预测不再查询或换点，Current Contact只Promote已提交Landing。
9. 在唯一Interpolation中增加正式竖直Correction速率限制，删除Acquire进入帧和Post Constraint的立即抬升；安装Ground穿透预算与Landing Lock完成门控后，再让Foot Height进入Swing并删除旧Baseline Height Error政策。
10. 让Support进入Resolved Foot、Primary Support和Pelvis，但保持Lock生命周期不变；为唯一Pelvis Spring增加非对称速度边界。
11. 增加双腿Reach交集、最小Landing压缩余量、Goal夹紧与typed拒绝。
12. 用Contact、Lock Mode与Lock Weight替换旧PlantConfidence生命周期并删除旧字段。
13. 显式重建Corin Projection、Float32与Fixed产品，完成编译、诊断重放和严格OpenSpec校验。
