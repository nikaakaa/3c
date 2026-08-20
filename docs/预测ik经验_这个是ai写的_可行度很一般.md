# Foot IK关键经验

本文只保留会改变正式架构、否决方案或决定下一步的结论。历史编号永久保留，但不记录逐轮流水账。

## 不变量

- 唯一链：`Original Component Pose + Step/Body/World Facts -> CharacterFootPlacementRuntime单帧事务 -> 一个Foot Placement Final Goal Set -> 一次FinalIK FBBIK`。
- 每只脚只有一个`CharacterFootExecutionState`；Current Support、Predictive Plan、Query和Pelvis只返回事实或提案，不拥有第二份脚生命周期。
- Ground Envelope只提供feet-only安全下界；最终Swing保留动画XZ与动画净空。
- Plan、Revision、Landing、Anchor、Pelvis都必须有唯一owner；Rejected不得由响应式Swing伪装成功。
- FBBIK只执行最终Goal。Goal先跳而solver residual很小时，首因不在FBBIK。
- 固定验收顺序：Artifact重建 -> Projection事件/时钟 -> Future Body -> Ground Path -> Revision -> Landing/Anchor -> Pelvis -> FBBIK。

## 历史经验索引

1. 当前不是完整GDC数据层：Artifact、Constraint、Support Leg、Orientation、Pivot和平地重建门禁曾长期缺失，Runtime调参不能补齐这些事实。
2. GDC核心不是提前射线：`Final Sole Height = Ground Path Height + Animation Height Above Foot Path`，Convex Hull不是最终脚轨迹。
3. 四条路线必须分开：Animation Foot Route、Future Body Transform、Virtual Ground Query Route、feet-only Ground Envelope不能互相冒充。
4. In-place动画不提供世界位移；输入幅值、Action Motion、Visible速度、Body Yaw和Render差分都不能重建KCC世界运动。
5. 楼梯前先证明Artifact可按同一相位还原Heel/Toe/Sole/Ankle/Knee/Hip位置、旋转、弧长、侧向范围和事件边界。
6. 历史方案曾让冻结Plan每帧按Root做廉价平面刚体投影；该方案会旋转旧命中、Surface和Hull，已由71否决。
7. 曲线内部连续不等于跨owner连续；Active/Revision、Event、Predictive/Grounding、Swing/Anchor、Reach和Pelvis换边都可跳。
8. GDC Ground Path顺序固定为采集位置/法线 -> 排序 -> Edge Plane -> Reachability -> 删除不可达 -> 上侧Hull；顺序颠倒会先承诺错误路线。
9. Foot Lock是数据意图加世界验证；Landing必须原子提交Pose、Surface、Anchor local、Committed Sole和Successor Start。
10. Pelvis必须消费独立Body Support Path与Support Leg事实，不能平均左右Foot Envelope或把spring current当目标。
11. 上坡、下坡、跑步Foot Orientation与Support Foot Pivot是正式求解事实，不是诊断装饰。
12. 已否决：逐Render帧Physics重规划、旧世界Plan直接晋升、Route接管Foot XYZ、双Y owner、全局抛物线、平滑错误Path、响应式fallback、FBBIK后处理和第二套owner；逐帧无Physics执行投影不属于重规划。
13. 编译、Character Build、Console 0 Error和CSV等宽都不是运动效果通过。
14. 自动输入必须真实穿过故障地形；普通Play与自动Variant分开，自动只接管MoveAxis并保留LookAxis。
15. 最小完整A/D事务足以定位owner；扩大运行时间只增加重复数据。
16. Body Yaw与轨迹曲率不是同一事实；Revision C1不能从Render差分、Final Goal历史或额外Hermite猜测。
17. Foot Route起点、Ground Probe起点和Clearance起点是三个事实；混用曾造成约80cm XZ错位与长斜线。
18. Reachability必须在完整地面采集后构造有向链；稀疏端点预拒绝和单点双端直连都会误删正常楼梯。
19. 冻结Plan必须同时冻结Step时长、Future Body范围和phase映射；运行时改时长曾触发轨迹越界。
20. 下一事件必须在Incoming PreSwing预建；等它成为Current再建会错过LiftOff，形成无Plan空窗和迟到托脚。
21. “全收命中”会踏面切换抖动，“全拒命中”会Plan消失踏空；Physics命中距离不等于路线支撑所有权。
22. 单次Cast贪心选最高点只会把错误从一只脚转到另一只脚；必须先保留候选组再求完整有向链。
23. 自动CSV只允许`LiveState -> completed frame -> 流式压缩`；叠加无界Continuous Capture曾造成约10MB/帧分配和严重低帧率。
24. Current与Incoming必须由Analyzer在同一Artifact采样点原子发布完整Step事实，Runtime不得搜索或补建。
25. Marker occurrence由`Source Landing Cycle + Event Ordinal + Foot Side`拥有；不能从连续时间距离反推身份。
26. Projection必须整对选择`Current + Incoming`，不能逐字段择优或从不同source拼装一个不存在的Step。
27. 权威Step不能再乘Pose contribution weight；否则事件身份和Foot Placement所有权会换代两次。
28. Revision起点必须是上一完成帧实际送入FBBIK的Final Sole及其支撑，不是旧Plan理论Target；创建帧Blend保持0。
29. Future Body曲率只来自相邻Simulation committed Intent和Tick时长；Presentation不拥有导数。
30. 原子Step不等于10KB巨型值类型复制；完整事实进入预分配Workspace页，Action Frame只携带lease快照。
31. Incoming事件边界需要离散Source Landing Cycle Offset；连续Clock插值曾让Incoming提前跨到N+2并造成1.6m断点。
32. 弹簧不能修复同一步多次换路；查询必须按正式trajectory authority tick限流。源Plan一生只尝试一次会让单次Rejected封死整步，同一tick至多一次、后续tick可重试才同时满足性能和可恢复性。
33. 接触净空不能逐帧累加进Current spring；删除重复`offset += constraint`后Current Offset从米级降到约0.4m内。
34. Plan在ReleasePhase开始所有权淡入，Ground Path到PathStartPhase才开始推进；用几何起点门控状态会制造LiftOff硬切。
35. 运动偏差必须是提交边界：过期Plan不得输出Landing、Anchor或Successor；run `2755132...`曾在误差0.99m至2.61m时继续执行并产生`+65/-116/+100cm`跳变。
36. Idle Capture不能依赖权重先低于1：run `32d8bff...`静止段两脚`Contact=true`但`HasAnchor=false`；改为进入`GroundedStationary`显式武装后，run `d6ee145...`两脚frame76捕获、frame82权重到1并持续Anchored到frame97，Final XZ不再跟随Idle动画漂移。
37. Revision权重连续不等于旧侧目标连续：run `d6ee145...`中旧Active已偏离0.51m至4.82m仍在Blend期间继续求值。Revision旧侧必须冻结上一完成输出；本Plan从未输出时则没有预测历史可保留，必须先退出再从当前事实重建。
38. Swing交接不能冻结绝对世界Ankle：run `2633c9...`中动画与Pelvis继续运动，绝对冻结导致Reach单帧补高53.9cm、Final Goal跳72.4cm。必须冻结相对Original Animated Ankle/Hip的修正，保留原动画运动；只有Ground Path与Support保持世界事实。
39. 历史方案曾把参考文章4解释成“每帧刚体变换旧Path、跨界才重建”；运行证明它让可视化线旋转但不代表新地形采样，已由71否决。
40. Traditional与Predictive必须在Release前按整步选择并保持粘滞。Traditional可处理急加减速、低速急转、无历史和空中状态，但不能成为Query Rejected后的中途fallback。
41. 旧WorldProjection要求偏差比较也进入投影域，否则正常位移会累计成米级误差；整个WorldProjection已删除，不再作为正式设计。
42. 唯一Revision槽必须有交接截止时间。run `db699e86...`中左脚旧事件在phase 0.944仍创建Intent Revision，直到新事件成为Current仍占槽，随后产生frame148-157共10帧Swing无Path；Intent Revision若不能在`ApproachingContact`前完成就不应启动，交接边界必须让Incoming Successor优先。
43. Planner准备早于同帧Stance提交，若Event Successor硬等Committed Anchor，唯一预建窗口会天然晚一帧。允许用旧Active已验证的Projected Landing预建不参与Goal的geometry，但提升前必须用上一完成帧Committed Anchor验证；候选Rejected后的FadeOut不能封死后续authority tick重试。
44. 自动压力测试的A/D事务不能把“一轮结束”当作“运行结束”：旧实现进入Complete后先提交零输入、再等正式Input Action收敛，既截断CSV又会在锁存输入尚未更新时抛异常。Endurance必须持续循环并按lap分段，只有手动停止Play才负责释放输入和封口数据。
45. Camera-relative的`A 1秒 -> D 2秒 -> A 1秒`只在输入时间上对称；角色朝向和相机Basis持续变化时，世界位移不会闭合。无限重复会把角色漂出Deterministic Collision World。每轮必须用正式MoveAxis回到压力区起点后再递增lap，不能靠自动停测或Transform传送规避边界。
46. Event Successor是否能预建首先取决于Artifact是否提供真实下降窗口。run `d863a1...`中代表性右脚事件从phase `0.0336`推进到`0.1653`仍保持Unsupported，换代后出现22帧Swing无Path；根因是Analyzer把ApproachContact固定成Landing前一个采样点。正式边界必须来自同一参考Foot Path上的动画Clearance峰值，Runtime Planner不能用fallback猜一个窗口。
47. GameplayLab场景碰撞体变化不会自动更新Fixed KCC的Deterministic Collision Artifact。run `e6f5b4...`中Actor本体从`Y=1.04m`跌至`-21.33m`并触发`body left collision world bounds`，证明不是IK视觉下沉；World Bounds足够大也不能代替场景碰撞重新烘焙。Free与Automatic必须共用当前场景生成的同一正式Artifact。
48. Event Successor已经预建不等于换代已经闭环。run `dc44e9d...`中Successor在旧事件下降期存在，但事件成为Current时因没有Committed Anchor被取消；同一分支又把Committed Anchor当作Current PreSwing重建前提，最终左右脚分别出现547/549帧Swing无Executable Path。换代时若没有Landing事务，只能拒绝旧Successor，并在同帧用当前真实Sole、唯一Current Support和committed trajectory创建Current Event Plan；不能等待到Swing后再由响应式Grounding托脚。
49. Reach Clearance出现接近腿长的数值不是参数不足，而是Ground/Body Path身份过期的证据。run `dc44e9d...`中左右最大Reach Clearance为`0.792m/0.910m`，对应Path仍在`Y=0`而身体已到`Y=0.99m/1.14m`；Reach只能验证同一Plan的小范围可达性，不能把低一层的旧Path抬到当前角色附近。
50. Executing Plan单帧求值失败后直接保留Baseline，就是隐式响应式fallback。run `dc44e9d...`右脚frame `1441 -> 1442 -> 1443`在同一Plan内出现`预测有效 -> NonFinite且Path归零 -> 预测恢复`，frame1442物理穿透`12.14cm`。失败帧必须保留上一完成输出相对当前Original动画的修正并连续淡出，同时发布`ReachExceeded`或`NonFinite`等typed reason；不得形成`预测 -> Baseline -> 预测`。
51. 权威事件存在且预测权重为零不等于Stance拥有脚。只有Locked/Sliding动作约束或真实Anchor可以保留Grounding目标；Unlocked Swing没有可执行Plan时必须保持Original动画并明确暴露失败，不能用Current Grounding伪装预测成功。
52. Ground Envelope分段必须同时保存起点与终点Surface。run `ea0e2e2...`中第一段起点坐标来自已提交Landing，但旧段只携带终点Surface，台阶边缘因此把连续Successor误判为Surface不兼容并取消。GDC的连续Hull是feet-only下界，不允许用终点踏面身份冒充整段或FootLock起点。
53. Event Successor不能无条件阻塞当前Swing的Intent Revision。run `451c2adb...`中旧Active的`MotionLandingError`已达`2.59m~2.81m`、正式容差仅`0.08m`，但预建Successor占据唯一Revision槽，旧Path仍以100%权重改写脚；事件换代后Goal单帧跳`60cm`以上。当前Step仍为Unsupported时必须先修正当前Plan，进入ApproachingContact后才由Successor优先；当前Swing无Plan必须原事件重建并从上一完成输出连续接管。
54. Ground Envelope样本是有限高度下界，不是可无限外推的Surface Plane。run `b33c501f...` frame `24103 -> 24104`中Path Y只下降`1.62cm`，但前一Segment法线约为`(0.035,0.585,-0.810)`，Native Sole与采样点相距约`1.13m`；把该局部斜面外推后Heel/Toe平面距离达到`-0.684m/-0.691m`，Required Lift被放大到`1.215m`，下一帧切水平面后Final Goal单帧下降`1.156m`、Pelvis Translation下降`38.7cm`。Surface法线只可在现有SoleSupportRadius局部覆盖内参与坡面净空与方向，超出范围后必须按Ground Envelope高度沿Component Up计算；远距离偏差交给Revision，不能交给净空补偿。
55. 历史方案曾让Plan identity提升后继续保留Sequence Output Continuity，以避免Promotion帧暴露新Goal；后续静态复核证明这会与已存在的Revision Blend重复，形成拉回、滞后和弹簧感，本规则由56替代。
56. Intent Revision只能有一层连续性：旧侧是上一完成Final Ankle相对Original动画的冻结修正，新侧是新Plan完整安全目标，二者只做一次Revision Blend。Support identity随Promotion原子替换；Executable Swing完成Plan净空后不得再被Current Grounding末端顶脚。
57. “一个owner”不能只写在文档里。当前`Prepare -> GetStanceInput -> Grounding Update -> ObserveStance -> baseline Goals -> Predictive Resolve`让Grounding与Predictive互相观察半完成状态，且`FootState`、`FootPlanRuntime`和可变Plan共同持有生命周期。正式结构必须改成一个Frame Input/Result事务、一个每脚执行状态、不可变Plan/Query和一次最终Goal写入。
58. Ground Envelope几何连续不等于Foot Rate采样连续。run `4bded9ad...`左脚同一Plan的权威phase仅从`0.6875`到`0.7222`，旧全局最近段却把progress从`0.3492`跳到`0.8756`，frame `709 -> 710` Path Y下降`46.97cm`、Goal Y下降`57.98cm`。Foot Rate只能在权威phase对应的局部路线段投影，不能跨整条自交或回折路线找最近点。
59. Foot Placement状态必须与Final Pose共用提交边界。若Evaluate阶段直接推进Plan、Anchor、Landing或Current spring，随后FBBIK或Final Pose失败，下一帧会从不存在的半完成历史继续。正式做法是保存完整Committed状态、只在Pending上求值，并在Presentation Seal后保留；Discard与Fault恢复全部左右脚和Pelvis filter状态。
60. Plan事实与Plan执行状态必须物理分离。冻结路线、查询几何和Landing候选属于不可变Plan；当前相位、world projection、运动误差、结束原因和Blend属于每脚执行状态。Intent Revision、Event Successor与Predictive Exit共用一个`CharacterFootPlanTransition`，CSV直接记录kind，禁止再从Plan消失或响应式输出猜换代原因。
61. Event Successor提升不能清空Transition再等Sequence Continuity补洞。run `e31ff918...`中左脚frame `2041 -> 2042 -> 2043`发生`旧预测Goal 0.994m -> 新Plan低权重Grounding 1.849m -> 新预测1.226m`，而Path首帧基本未变、FBBIK残差接近0；另有新Plan先处于`Planned`而Goal单帧下降`46~72cm`。正式交接必须由同一Transition保留上一完成输出相对Original动画的修正，跨过`Planned`空档，再由新Plan自身`Release -> LiftOff`权重接管。
62. Event Successor连续性不能在候选预建时冻结。run `cbf1f30...`右脚frame `19 -> 20 -> 21`中候选提前保存了旧脚仍在空中的修正；事件晋升时动画Ankle只轻微变化，Final Goal却从`0.081m`跳到`0.430m`，单帧回弹`34.86cm`，随后新Plan执行又回到`0.170m`。候选阶段只能保存geometry与时钟；连续性必须在Promotion边界从紧邻上一完成帧捕获。修复后run `b91013d...`的晋升最大跳变降为左`12.71cm`、右`15.23cm`，证明捕获时点是根因之一，但同Plan跳变和穿透仍需继续定位。
63. 同一Plan不能在Pelvis前后执行两次完整Goal求值。run `628412c...`左脚frame 385在Stance/Pelvis输入阶段仍有合法Geometry，随后同帧Goal阶段因应用Pelvis后`ReachExceeded`；旧诊断只留下末次布尔结果，把Geometry、Pelvis和Reach三个owner混在一起。正式链必须封存一次Geometry Candidate，Pelvis后只追加同Plan Sequence的Reach裁决。
64. `PredictiveExit`不是Unsupported Swing中候选失败的合法替代owner。run `628412c...`左右脚在`FutureLandingNoCandidate`或`ReachExceeded`后由Render Delta推进退出，长帧中Final Goal分别出现约`70.9cm`和`47.0cm`下降；这不是Path曲线自身抖动，也不是FBBIK放大，而是尚未完成重建时把脚暴露给Original的所有权错误。修复应保留上一完成输出为待替换事务并继续正式重建，不能调淡出速度或让Current Grounding接管Swing。
65. 自动Input Action的锁存边界属于Input System update sequence，不属于Render Frame。只有`InputSystem.onAfterUpdate`确认正式Action消费了上一提交状态后才能校验；要求每个表现帧都出现新Input update会制造与Foot Placement无关的假失败。
66. Foot与Pelvis必须在同一个Transition边界保留旧完成输出。run `fec7a6b...`证明仅保留Foot而暂时移除Pelvis候选，或在Transition之后再次执行Reach，都会让同一Completion出现两个owner；移除第二Reach后run `79dfec...`的`Pre-Continuity -> Final`最大额外变化降至左`2.192cm`、右`0.982cm`。
67. Anchor部分权重不是Original动画的空档。run `79dfec...`中Anchor Blend约`0.303~0.706`时出现`63.59~82.48cm`Goal下降和最高约`1.330m`物理下陷；根因是先以`1 - AnchorBlend`衰减Predictive，再让Original补足。正式Landing必须只做`Resolved Predictive/Transition -> Committed Anchor`的互补混合，并让Foot与Pelvis消费同一权重。
68. 互补公式不能使用本帧重算目标作为左端点。run `fe5f1816...`右脚frame 231首次提交Anchor时Blend为0、Committed Anchor Y为`2.423m`，但Final已降到`1.981m`；frame 232事件换代后Pre-Continuity降至`1.505m`，约9.3% Anchor混合仍产生`84.48cm`物理下陷。Landing左端点必须在首次Anchor提交时冻结为紧邻上一完成Final，并用`Plan Sequence + Landing Event identity`锁定整笔事务；跨帧连续性还必须比较Goal Owner，且不能在Landing覆盖底层Plan时提前消费释放交接。
69. Landing公式正确后仍可能由Stance事务跳变。run `0bf203fb...`证明Origin、Target、Blend到Final的代数误差低于`0.01mm`，但同identity Handoff会一帧消失再捕获，左右分别30/92次；frame `794 -> 795`旧Anchor遇到Current Event换代时Blend从`0.354`增至`0.919`，产生约`1.205m`三维Goal跳变。Committed Anchor必须持续到Release归零；事件换代不能让旧Anchor重新增权，逐帧`HasAnchor`布尔值不能代替显式事务状态。
70. Landing Handoff开始后不能每帧用Completed Output重新验资格。run `20a2e819...`中Anchor与Committed Goal持续有效，但首帧Handoff输出把Completed Output记成底层Active Plan，下一帧便因Plan不匹配自行退出。Handoff必须由冻结的Plan/Event identity持续拥有，期间Completed Output也记录Anchor Plan；底层Plan只能提供候选，不能取消已提交事务。
71. 运行时有效转向不能刚体旋转旧Foot Route、命中、Surface和Ground Envelope。方向改变必须产生新committed trajectory Revision，重新Landing查询、Capsule采样、Edge、Reachability与Hull；旧Plan只保持不可变交接旧侧。实际脚未沿平地Animation Foot Route时禁止进入地形算法。

## 当前证据与下一owner

- 首轮换代修复后的run `ea0e2e2...`共2021行、1221列且逐行等宽；左右Swing无Executable Path从`547/549`降到`85/8`，同一Executing Plan的单帧`rewritten=false`降为0。
- 同一run左脚frame `197 -> 198`在Anchor开始接管时，预测Fade仍覆盖Baseline，单帧产生`55.54cm`物理下陷；Fade必须使用Predictive Output与Anchor的互补权重，开始帧不得先推进Render Delta。
- Event Successor起点坐标正确但Surface取自Envelope第一段终点，解释了台阶边缘Promotion取消与剩余无Path窗口；修复必须进入Envelope数据合同，不能放宽身份阈值。
- Reach Clearance仍达左`0.678m`、右`0.780m`，说明低层旧Path身份仍未完全退出；端点Surface和换代闭环后必须继续以该指标验收。
- FinalIK历史残差仍远小于Goal跳变；在Goal连续性闭环前不修改FBBIK。
- 4A重构后的run `e31ff918...`共2767行、1223列且Header唯一、逐行等宽。最大换代异常不是Ground Envelope或FBBIK：Event Successor提升会清空Transition并禁止Sequence连续性，左右脚分别有`75/78`次大跳发生在Plan换代；大量同Plan最大Path Y变化约`1.6m`实际是`Planned -> Executing`诊断空档。修复口径是保留唯一Successor交接并让已过Release的Plan当帧Executing，不增加第二平滑层。
- 第二轮run `451c2adb...`共2278行、1221列且逐行等宽；互补Fade使最大物理下陷从左`55.54cm`/右`59.14cm`降到左`18.71cm`/右`7.02cm`，证明Fade修复有效但尚未闭环。
- 同一run左右Swing无Executable Path为`136/48`帧，左frame `820 -> 821`由旧Path `Y=0`、100%预测权重切到新Plan `Y=0.893m`、预测权重`0.0055`，Final Goal单帧跳`64.40cm`；这不是FBBIK放大，而是旧Plan过期、Revision槽被Successor占用和换代输出不连续叠加。
- 下一次数据回归先验证四项：过期Active不再在米级偏差下100%输出、当前Swing无Plan可原事件重建、Plan换代首帧Goal连续、Successor起点不再被Current Query覆盖；通过后再进入Landing事务与Pelvis支撑切换。
- run `b33c501f...`共27915行、1221列且逐行等宽；左右最大Goal Y单帧变化为`1.156m/0.660m`，最大Pelvis Y单帧变化为`0.387m`。最坏帧发生在Executing、rewritten且预测权重为1时，证明局部斜面无限外推是预测owner自身错误；左右另有`232/110`帧Swing无Executable Plan，分别产生最大`15.29cm/8.25cm`物理穿透，仍需独立验收Plan空窗。
