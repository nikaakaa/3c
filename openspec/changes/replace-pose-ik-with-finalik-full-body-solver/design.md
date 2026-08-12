## Context

上游动画在唯一`Prepare -> Evaluate Barrier -> Seal`事务中产生原始Component Pose与动作贡献。Foot Placement是Evaluate Barrier内唯一world-aware脚部owner，输出一个typed FullBodyIK Goal Set；FinalIK只在同一Pending Pose页执行一次FBBIK。

旧实现先生成响应式Goal，再用Predictive Modifier叠加高度。随后一次纠偏又把冻结的世界Foot Route完整XYZ写进Swing Goal。前者没有真正消费预测路径，后者会在角色实际移动与烘焙动作距离不一致时把腿拉向旧世界位置。两种做法都不符合GDC的“地形路径 + 动画净空”分解。

## Decisions

### 1. 一个Foot Placement owner

```text
Simulation locomotion clock + input direction
  -> Pose source / Blend / original Component Pose
  -> Unified Foot Placement
       Baked In-place Foot Step Fact + frozen committed locomotion intent
       Frozen terrain-distance map, Query Route and Landing
       Ground Path and terrain-aware predicted Hip
       Native Foot XZ + Ground Height + Animation Clearance
       Stance / Anchor / Pelvis
       One final Pelvis + Feet Goal Set
  -> FinalIK FBBIK once
  -> Final Pose publication
```

Foot Placement内部只有一个World Query backend、一个左右脚Plan状态、一个Stance状态和一个Pelvis状态。预测是该owner内部的Swing规划能力，不是独立Goal后处理节点。

### 2. In-place动画烘焙的职责

Corin使用键鼠与in-place Pose。正式角色平移由Simulation `LocomotionInputMotion`的作者Move Speed和输入方向产生，不由动画Root或Action Motion Curve驱动。当前工作区把WalkStart、WalkLoop、RunStart、RunLoop从原`4.592 / 6 / 5.1 / 7.36 m/s ConstantSpeed`改成`MoveSpeed=0 + ActionMotionCurve`，属于本change必须撤销的错误运动链。

Foot Analysis只为每个Landing Event原子发布：

- root-local Foot、Ankle与Hip路线；
- 动画鞋底相对参考Ground Path的Clearance；
- Landing姿态、Constraint、Support、Orientation与Body Pivot策略；
- Action Step Clock域。

同脚前后Landing只定义事件identity、duration和phase。Plan可以在PreSwing提前创建，但真正提交给未来查询和执行的Foot Route必须裁成`planStartPhase -> landingPhase`。`constraintReleasePhase`取离散Constraint从`Locked`切到非`Locked`时最近采样规则真正生效的交界，并夹到不晚于LiftOff；`planStartPhase = max(generationPhase, constraintReleasePhase)`。Locked阶段不是Foot Path，但Stance开始释放后Plan必须在同一相位接管，不能留下Current Grounding空窗。计划创建时一次冻结同帧committed Locomotion Intent的请求平面速度、世界Root、方向、Native Sole和脚骨局部姿态；不得把CharacterController碰撞后的Body水平投影当成作者Move Speed。现有Anchor只通过同一Stance Blend连续交接到Executing Plan。不允许用逐帧`FootRouteWorldAlignment`、当前脚位置或新查询改写计划。计划提交后不重读Body速度或输入，不使用动画Root位移曲线或Foot Analysis推导速度形成第二运动模型。

### 3. Simulation拥有动作时钟

Landing Event绑定`SourceSampleIdentity + SourceSampleCycle + EventOrdinal + ContributionContinuityIdentity`。Locomotion Sequence、原动画Pose、Action Step Fact与Clearance必须由Simulation提交的`LocomotionMotionElapsedTicks`投影到同一相位；Presentation只可在相邻Simulation事实间插值，不得按`presentationDeltaSeconds`独立累计动作时间。

Plan不拥有第二时间轴。Contribution消失、身份替换、动作相位回退或动作中断时旧Plan结束。Ground Path执行进度只来自同一`Action Step Clock`在`planStartPhase -> landingPhase`区间的规范化相位；不得用当前脚世界位置、墙钟、Render Frame数或Plan私有Elapsed重新推导进度。

### 4. 冻结的是查询承诺，不是最终脚XYZ

计划创建时冻结committed Locomotion Intent请求速度和生成相位脚骨局部姿态。请求速度乘剩余动作时间只定义角色沿未来地面的路程预算，不直接承诺相同长度的世界水平位移。唯一Future Query owner在同一个Plan创建事务中先用该预算生成覆盖未来地面的发现路线，删除不可通行候选并构造分段Upper Envelope；随后用Envelope三维弧长把动作路程预算反解为不可变的水平Route Progress，再以该映射生成唯一正式Query Route、Ground Path与Landing。只有第二阶段正式结果可提交、诊断和执行，创建后不得再次查询或重映射。

地形映射只改变Root沿运动方向走到哪里，不改变Animation Action Phase。`localFoot(samplePhase) - localFoot(generationPhase)`、Clearance、Constraint与Support仍使用原动作相位；平地上映射严格退化为恒等。Plan开始前脚由Stance保持；Constraint进入非Locked后Plan立即执行，并按同一Anchor Blend连续取得所有权。第一点是预测Constraint Release Sole，最后一点是该事件Landing。计划执行期间不得按当前Body速度、当前输入、当前脚或新查询重建这份路线。

当前上游原动画Component Pose继续拥有Swing脚的最终X/Z与基础旋转。运行时只用同一Simulation Action Step Phase采样已冻结Ground Path；Native Sole不得回头驱动Path Progress。冻结路线不得直接覆盖当前世界Ankle X/Z。

### 5. Ground Path与Animation Clearance合成最终高度

```text
terrainDistanceBudget(phase) = frozenRequestedPlanarSpeed * timeFromGeneration(phase)
terrainRouteFraction(phase) = InverseArcLength(frozenUpperEnvelope, terrainDistanceBudget(phase))
queryXZ(phase) = nativeSoleAtGenerationXZ
    + frozenRequestedPlanarVelocity * mappedTimeFromGeneration(terrainRouteFraction(phase))
    + frozenRootRotation * (localFootXZ(phase) - localFootXZ(generationPhase))
constraintReleasePhase = min(liftOffPhase, firstNearestSampleBoundary(Locked -> non-Locked))
planStartPhase = max(generationPhase, constraintReleasePhase)
pathProgress(t) = Normalize(simulationActionStepPhase(t), planStartPhase, landingPhase)
finalSoleXZ(t) = nativeSoleXZ(t)
finalSoleY(t) = groundPathY(pathProgress(t)) + animationClearanceY(simulationActionPhase(t))
finalAnkle(t) = finalSole(t) + nativeSoleToAnkle(t)
```

等价实现可以把`GroundPathY - BakedReferenceGroundY`作为Component-Up增量加到原动画鞋底，但不得使用`max(AnimationY, GroundY)`，也不得把冻结Route完整XYZ写入Goal。Heel/Toe只对同一Landing支撑面做最后的最小安全修正。

同脚事件覆盖本脚前后两次Landing，期间必然包含一次对侧脚Landing。Ground Path MUST按该对侧接触的权威动作时间切成前后两个Virtual Ground区间，再分别构造连续Upper Hull；不得对完整同脚步幅只做一次全局Hull。分割点只改变Ground Path的地形包络，不把对侧脚位置写成本脚Landing，也不建立双脚共享Plan。对侧接触身份、Phase与Future Support在本脚Plan提交时一并冻结，之后不得逐帧重查或移动分割点。

没有Executable Plan时，Swing保持上游动画并明确报告不可预测；Current Ground Query只提供Stance接触事实，不得作为隐藏的响应式Swing兜底。

### 6. 先建立地形Hip，再判断可达

Future Query在同一个Plan创建事务内按以下顺序执行：

1. 用冻结请求速度形成覆盖路程预算的发现路线，并逐点选出与上一点连通的唯一正式支撑；
2. 对每个相邻正式支撑之间执行连续Sweep，水平候选只有同时可连接前后正式支撑才可进入Ground Envelope，近竖直命中只作为Edge Plane；
3. 支撑高度只来自该地形命中的接触高度，不得用Query Route或无IK脚高度向上钳制；
4. 以正式支撑高度相对当前参考支撑的变化平移同相位预测Root/Hip，再判断Step、Edge与Ankle Reach；
5. 删除不可通行候选后，按权威对侧接触分段并分别构造连续Upper Hull；
6. 用该Upper Envelope的三维弧长反解动作路程预算对应的水平Route Progress，再生成并提交唯一正式Route、Ground Path与Landing；发现阶段不得成为第二Plan、第二Grounding或可执行输出。

这样下楼不会因为Hip仍停在上一级而被提前判为ReachExceeded，上楼也不会先承诺不可执行路线再在最终Goal处截断。预测Hip只作为事实进入现有Stance/Pelvis owner，不形成第二Pelvis输出。

### 7. Stance、Pelvis与FinalIK边界

Current Ground Query只提供当前合法支撑。动画进入接触阶段后，计划Landing与当前接触共同决定捕获；捕获后Stance唯一拥有`Locked / Sliding / Unlocked`、Anchor、移动Surface与连续释放。地形修正后的未来Root/Hip只形成同一个Pelvis目标并进入现有连续Spring；锁定支撑腿的Reach只检查该连续Pelvis当前值是否仍能到达Anchor。不可达时释放同一个Anchor并沿现有Blend连续退出，Reach不得在Spring之后再次钳制Pelvis，也不得把Foot Goal权重单帧清零。

FBBIK只消费最终Goal Set，先应用唯一Pelvis Pre-Solve，再应用Feet与其它不重叠Goals并执行一次solve。它不读取Ground Path、不规划、不锁脚、不做后处理。

### 8. 不可变诊断快照

每个Plan保存完整正式Query Route、Ground Path、Landing、查询请求、接受支撑、拒绝几何与动作身份。每帧保存Simulation Action Phase、空间Path Progress、Native Sole、Ground sample、Clearance、Final Heel/Toe/Ankle、Pelvis与FBBIK结果。Scene、Game和CSV只读同一完成快照，不重新计算；发现路线不得画成可执行Path。

## 2026-08-11 算法纠偏记录

1. `01396a864cc04a4ebcfd5935e9b4577a`证明旧Modifier只加Y，预测Path XZ没有进入Final Goal，FBBIK以近零残差执行了错误Goal。
2. 随后的全XYZ改版把约`3.58m`至`3.60m`的同脚周期冻结路线直接写入Swing Goal；`00214d2c4c164be991ae4b2a382318ca`中同计划预测Root约`3.27m`至`3.49m`，实际角色只移动约`2.81m`至`3.30m`，形成水平拉腿、侧飘与上楼鬼畜。
3. 同一run的重诊断分块中248个Render Frame只推进87个Simulation Tick，Presentation经过`5.86s`而Simulation经过`1.45s`，确认独立Presentation时钟会让烘焙动作、Pose与实际位移脱节。
4. 查询代码在用地形高度修正预测Hip之前执行Reach，解释了“下楼大量Rejected却更稳定、上楼Executable却鬼畜”：下楼由原动画保留形成假好结果，上楼由错误计划接管。
5. 本次正式替代：冻结路线只用于查询；原动画Pose拥有最终Foot XZ；Ground Path与烘焙Clearance只决定Y；Simulation时钟统一Pose与动作事实；地形修正Hip先于Reach。
6. 对照GDC原始幻灯片与最新参考实现后确认：GDC要求逐脚的Distance/Time预测、Foot Forward来自动画、Animation Height表达高于Foot Path的高度。Corin是in-place + Constant Speed，因此距离必须来自计划创建帧冻结的Simulation速度乘剩余时间；Action Motion Curve、Foot Analysis重建Root与当前脚空间投影都会形成额外运动模型，不属于当前项目的GDC合同。

## 2026-08-11 实施记录

1. 先前实现移除了输入幅值缩放，却继续让`CharacterPredictiveFootRootTrajectory`消费烘焙Action Root；最新审计确认Corin in-place不应使用该位移曲线，因此这一步只作为被证伪的中间实现记录。
2. Swing输出撤销冻结世界XYZ接管：冻结Route只生成Ground Path；每帧最终Sole XZ来自原始Component Pose，Y由`GroundPathY + AnimationClearanceY`生成，再用当前Sole-to-Ankle几何恢复Ankle。
3. 已实现的“当前原动画Sole投影到冻结Route”仍会让冻结计划受当前Pose逐帧驱动，现已判定为错误方向；它只作为一次失败实施记录保留，必须由同一Simulation Action Step Phase的确定性采样替代。
4. Future Query顺序改为先地形后Reach：每个合法支撑先按相对参考支撑高度平移同相位预测Root/Hip，再执行Ankle Reach、Step和Edge判断。
5. 该change曾让Float32与Fixed Simulation从Motion resolve后的获胜operation反查`LocomotionMotionElapsedTicks`、Tick Rate和Runnable Generation，并把Walk/Run节点切到`SimulationLocomotion`。`add-generated-foot-phase-animation-sync`已将此实现重基线为producer随Motion原子提交`CommittedMovementPlaybackClock`，节点统一迁移为`CommittedMovement`；Locomotion Input与MovingTurn Timeline分别拥有自己的clock，Action不进入Movement clock。
6. 先前为支持预测而把Simulation每Tick位移改成采样Action Motion X/Z，改变了Corin原Move Speed并造成速度不匹配；该修改必须通过正式Document撤销。
7. 当前验证只完成Unity脚本编译0 Error与Runtime工程编译0 Error。`tasks.md`仅将已经落地并经过该编译检查的3.1、4.1至4.3、5.1与5.2标为完成；独立Predictive Modifier清理、Gizmo/CSV、Float32/Fixed Character Build和GameplayLab效果回归仍保持未完成。
8. 当前代码审计确认IK迭代把Corin四个Locomotion节点从git基线的Constant Speed改成Action Motion Curve，并给Foot Analysis增加了21份Pose/Action Motion绑定。下一步先通过正式Document恢复原Move Speed与Constant Speed，再删除Foot Analysis的Action Motion依赖；预测只冻结同帧Simulation速度和Landing剩余时间，同时删除`FootRouteWorldAlignment`与空间投影进度。
9. 正式Corin Document已恢复`WalkStart=4.592`、`WalkLoop=6`、`RunStart=5.1`、`RunLoop=7.36`的`ConstantSpeed`，四个节点不再引用Action Motion Curve；Document apply后保持`syncState=Clean`。
10. Foot Analysis已升级到v48：删除Action Motion Clip/Curve绑定、Action Root产物和21份旧曲线资产，只从in-place Pose烘焙root-local Foot/Ankle/Hip、动画净空、接触事实和统一步时钟。
11. 运行时Query Route现为`创建帧Native Sole + 冻结committed Simulation平面速度 * 生成后动作时间 + 同一in-place Pose脚骨局部姿态差`。它不读取动画Root位移曲线；冻结路线只用于未来地面查询，最终Swing XZ继续来自当前原动画Pose。
12. 旧独立Predictive Modifier源码与产品合同已删除；预测规划、Current Grounding、Stance、Pelvis在同一个Foot Placement owner内合成一个Goal Set，并共享同一个World Query backend。
13. Runtime与Editor工程已在关闭build server的条件下完成0 error编译；Runtime存在1个与本change无关的`CharacterInputValueNodes`未使用字段warning，Editor为0 warning。Unity刷新后项目Console为0个项目错误。Float32产品已按精确Corin Definition重新发布；Fixed工具已导入精确Corin wrapper与同一Projection，效果门禁仍以新自动采样为准。
14. 自动run `0ca7e9f31d0a420db3d8f3351ec8f39b`生成214个gzip分块与manifest，双向楼梯Traversal共2280帧，Header与每行均为928列。同计划Route Hash唯一、Progress单调且没有Surface A-B-A，证明Action Motion与逐帧重建已从现行链删除；该run当时把首点强制等于创建帧Native Sole，后续第17条已证明此约束对PreSwing计划错误。
15. 同一run仍检出左脚25.79cm、右脚31.24cm物理下陷。异常帧FBBIK位置残差为0，预测Path自身Heel/Toe距离为0，但Plan在权威Swing开始后进度约0.16至0.36即以`StanceCaptured`结束，后续`rewritten=false`，说明错误Goal来自Foot Placement所有权而非Solver。根因是7点离散Constraint/Support用最近样本读取，刚越过精确LiftOff仍可读到前一`Sliding/Releasing`样本，Current Grounding因而提前夺取Swing。修复口径是精确Action Step Clock决定PreSwing/Swing，离散事实只保留锁定强度与Approaching Contact；Executing Swing只允许在Approaching Contact向现有Stance交接。
16. 修复后短run `3cd52acdba8e41f6adabb7f51675a043`中，Executing且Rewritten的Swing左脚最大物理下陷从25.79cm降至0，右脚从31.24cm降至0.4cm，`phase<0.8`的StanceCaptured由双脚各6次降至0。新首要异常是PlanExecutionStarted帧Goal Y跳高47cm至54cm：PreSwing生成计划把锁脚阶段也纳入Ground Path，LiftOff首帧直接从中段开始消费。现行修复把Foot Route起点改为`max(generationPhase, liftOffPhase)`，保留提前计划，但不把锁脚阶段当作摆脚路径。
17. 第二个短run `6e75aa54978e451d8370ec3baa8724ff`包含8段双向Traversal、461行，Header与每行均为928列。PlanExecutionStarted的Goal Y跳变中位数从左20.68cm/右30.92cm降至左8.94cm/右20.95cm，Executing且Rewritten的最大物理下陷为左0、右1.4cm，提前StanceCaptured仍为0；但右脚Rejected增至19个唯一计划。所有Rejected均为`FutureLandingReachExceeded`，其中8个计划只执行首个Query便失败。数据重建证明这些右脚计划从生成到LiftOff的Simulation位移与脚骨局部姿态差合计为60.5cm至66.1cm，现实现却把未来LiftOff起点强制钉回生成帧Native Sole，再用未来Hip判断旧脚点可达性。修复为保留`LiftOff -> Landing`查询域，但所有未来坐标从生成相位累计Simulation位移和脚骨局部姿态差；这不是动画位移曲线。
18. 修复导入后的首次自动启动生成run `f95e1583aa2c4561885283c5b1ad922f`，但仅46行便因脚本Asset导入触发Assembly Reload，manifest明确记录`status=assembly-reload`。该run不属于效果回归，也不用于判断第17条修复；Runtime、Editor编译与OpenSpec strict validate已通过，新的干净Editor run仍待执行。
19. 继续只读分析`6e75aa54978e451d8370ec3baa8724ff`后确认，Planner使用的`Body.TargetVelocity`来自committed `WorldBodyState.Velocity`，不是键鼠输入意图，不需要更换速度owner。主楼梯直线段右脚在PreSwing生成后由Anchored进入Swing时，原动画Native Sole相对生成帧已移动47.7cm至69.1cm，角色Root同期移动95.6cm至118.2cm；这与修正后的`Simulation位移 + root-local Foot差`预测一致。现有Stance却在权威Swing开始时直接`ClearAnchor`，同时预测输出丢弃Baseline Anchor Goal，形成确定的所有权跳变。修复为保留同一Anchor及其现有Blend衰减，并在统一Foot Placement owner内按`1 - AnchorBlendWeight`把最终Goal连续交接到预测Swing；不增加配置、第二Anchor或后处理。该修复仍需新的干净run证明。
20. 对同一run的62个双向PreSwing计划离线回放生成相位公式，并与首个越过LiftOff的实际Native Sole对账：左脚误差中位2.3cm、P90 10.9cm，右脚中位4.9cm、P90 7.8cm；离散采样最大误差分别14.7cm与14.4cm。对应帧Anchor Blend均已为0，证明60cm级变化是PreSwing期间角色位移与in-place局部脚差的真实累计，不是LiftOff单帧所有权跳变。曾尝试把Route第一点重新固定到生成时Sole，但这会恢复`FutureLandingReachExceeded`并缩短正确未来路线，已在实现中撤销。现行Route保留从生成相位累计到预测LiftOff与Landing的唯一Simulation运动模型；后续新run必须对账更精确的同相位插值误差和最终Goal交接。
21. 本轮最终代码完成Runtime 0 error编译（仅保留一个与本change无关的未使用字段warning）、Editor 0 warning/0 error编译、单一路径静态搜索、补丁格式检查、OpenSpec strict validate和.NET build server清理。Unity MCP连接层能发现并选中唯一Editor实例，但Editor主线程持续不回ping，当前不能把编译通过冒充GameplayLab效果通过；第4.7、5.6与Unity回归任务保持未完成。
22. 继续只读分析run `6e75aa54978e451d8370ec3baa8724ff`后发现新的确定性所有权空窗：旧实现只在LiftOff开始Executing，但离散Constraint按最近采样会更早从`Locked`进入非`Locked`并驱动Anchor释放。左脚42个计划中12个出现`Plan=Planned && AnchorBlend=0`，共27帧，中位2帧/0.107秒，最大物理下陷18.0cm；右脚40个计划中10个出现，共14帧，中位1帧/0.060秒，最大物理下陷20.6cm。修复把`constraintReleasePhase`定义为首个非Locked离散样本的最近采样交界且不晚于LiftOff，并令`planStartPhase=max(generationPhase, constraintReleasePhase)`；Query Route仍只使用冻结committed Simulation速度与in-place脚局部姿态差，不引入动画位移曲线。新run必须证明该空窗消失，才能把本条判为闭环。
23. 同一run进一步证明不能只提前Plan时钟：左脚有13个Planned帧、覆盖9个计划，同时满足`PlantContact=true`且Anchor正在衰减，`AnchorBlend=0.132..0.726`。旧预测交接仅在`PlantContact=false`时消费Anchor Blend；若Plan在Constraint Release开始Executing，这13类帧会直接从Stance Goal跳到预测Goal。现行交接删除该门控：只要Plan正在Executing且尚未进入Approaching Contact，就始终按同一个`1 - AnchorBlendWeight`从Stance Baseline连续混到预测Swing；PlantContact不再绕过既有Anchor连续状态。
24. 自动run `13c18c3f871044fb8c3fcce49cf05157`包含19个gzip分块与2514行，Header及每行均为928列。上楼Executing Swing的最大Goal Y单帧变化为左39.86cm、右43.98cm，FBBIK residual接近0；典型左脚Plan把Ground Path从0.24m拉到1.68m，右脚Plan从0.96m拉到1.92m，证明鬼畜输入在Foot Path而非Solver。源码审计确认`BuildUpperEnvelope`虽然存在，但只对本脚完整事件做一次全局Hull，没有实现GDC第30页的`Split path on opposing contact`。Corin WalkLoop资产时长0.6秒、正式MoveSpeed为6m/s；锁定段root-local Sole约0.1秒后移0.606m，反推动画参考速度约6.06m/s，排除速度被简单重复计算。3.6m是同脚完整步幅，对侧接触位于约1.8m；下一修复必须用权威对侧Landing时间与Future Support冻结Virtual Ground分割，不能截短Landing、调速度或让响应式路径兜底。
25. 首次Virtual Ground分割实现的自动run `f66a77ec019b454182aa1488ac19de4c`在停止前完成2217行，Header及每行仍为928列。左右带分割的Executable计划均在精确fraction处形成包络边界，但Goal最大单帧下坠扩大到左115.88cm、右162.42cm，明确判定该实现失败。最大异常不是FBBIK放大：旧Plan在`0.9818 -> 1`仅约6.5cm的水平末段把Ground Path从1.1786m降到0m，下一帧新Plan从真实支撑0.009m开始。根因是`QuerySupport`会检查相邻支撑的Step/Height连续性，`AddSegmentHits`却把同一胶囊Sweep的其它合法法线命中只按腿长加入凸包，未执行相邻支撑可通行性过滤；这违反GDC的`Remove any unpassable surfaces -> Convex Hull`顺序。对侧分割不能在污染后的候选集上单独解决问题；下一实现先在Sweep收集入口删除不可连续命中，再验证分割前后包络与计划交接。
26. 源码级复核进一步找到同一末端断崖的第二个确定原因：`AddSegmentHits`把地形命中高度写成`max(Query Route高度, Cast命中地面高度)`。Query Route是无IK脚的平面查询母线，不是Ground Path；下楼时该写法会把所有中间地面强行维持在起始台阶高度，只让最终Landing Query落到低处，必然形成单段巨幅下坠。现行实现已改为先选定每段前后正式支撑，再只接纳同时可连接两端的水平Sweep候选；近竖直命中仍只形成Edge Plane；Ground高度只取真实Cast接触高度。Runtime与Editor工程已0 error编译，Unity脚本刷新完成；效果任务保持未完成，必须由下一自动run证明上下楼Path连续性，而不是用编译结果判定成功。
27. 自动往返测试资产的速度合同已用manifest对账：`WalkStart`实际平面速度为`4.592m/s`，`WalkLoop`为`6m/s`，坡段约`4.10/5.36m/s`只是同一世界速度的水平投影。控制源通过真实Input System写入归一化MoveAxis，没有Transform写入、Time Scale或额外速度倍率，因此后续Path差异不能再归因于测试器速度模型。
28. Pelvis Spring旧实现把离散目标变化作为Target Velocity再次注入Spring，语义上重复驱动。删除`PelvisOffsetTargetVelocityAmount`及Runtime、Editor、Profile正式配置后，run `52cb240cde3b41bab7e274e1ffd12a8f`的上楼/下楼Pelvis最大逐帧变化仍为Target `14.59/16.99cm`、Current `6.50/7.06cm`、Resolved `10.87/7.06cm`。该配置删除是正式清理，但数据证伪它是主要跳变来源。
29. Reach曾使用Anchor与动画目标混合后的瞬时位置作为支撑几何。改为有Anchor时只使用稳定`AnchorWorldPosition`后，run `22e7769d7d3a49e3a81600c23155ed21`把上楼Resolved最大逐帧变化从`10.87cm`降到`6.52cm`，但Target仍为`14.88cm`，说明支撑几何修正有效而Pelvis目标本身仍不连续。
30. 曾把预测Pelvis权重从`4p(1-p)`改为单调`p`，试图消除计划末端权重归零。run `75a9a787a5a643c98c959b3e68f9b141`反而把Target最大逐帧变化扩大到上楼`24.69cm`、下楼`24.33cm`，因为左右脚各自Path Root不是统一Body Path，不能用单脚Progress直接接管全身；该实验已撤销，现行仍为`4p(1-p)`。
31. run `ad82140d7ab4474b9a9dd1a79b5d1457`证明真正的预测Swing穿透发生在安全Target与Stance Baseline混合之后：旧帧会出现Required Lift `11.78cm`、Applied Lift `6.52cm`、剩余穿透`4.74cm`。现行实现对混合后的同一个Ankle Goal在同一冻结Current Path支撑面上重新计算Heel/Toe，并只沿Component Up补足剩余距离；不增加查询或第二Grounding。该run中Executing Swing最终物理穿透左右均降到`0cm`，FBBIK活跃目标残差接近零。诊断同时修正为直接测量Solver Heel/Toe到实际支撑平面，不再从结果中错误减去已经应用过的`SoleClearanceTarget`。
32. Reach输出随后尝试按最大Support Weight在`Lyra Current`与硬钳制结果之间Lerp。run `20568c8059504bdfbb467932d4b54065`仍出现上楼`10.97cm`、下楼`7.29cm`的Resolved跳变；典型Frame 823中Spring Current只变化`-3.99cm`，Reach却把最终Pelvis从`5.51cm`改成`-1.46cm`。该实验被证伪：给离散硬区间乘连续权重仍会把Reach变成第二个Pelvis运动源。
33. 现行替代删除Spring后的Pelvis钳制：预测Root/Hip只驱动唯一Pelvis目标与Spring；Reach只接收真实`PlantContact + Anchor`支撑，检查Spring当前值是否可达。不可达时只调用现有Release，保留Anchor及Goal并由既有Anchor Blend连续退出；不再立即Clear Anchor或把Foot Goal权重归零。首次run `4f764d8f22a8426499dc33979cc739bd`因启动后又触发强制脚本刷新，仅2帧便以`assembly-reload`结束，不能作为效果回归；这2行仍确认944列合同成立且`Pelvis Resolved == Pelvis Current`。必须重新完成干净双向短run后才能接受本条。
34. 对run `ad82140d7ab4474b9a9dd1a79b5d1457`重新按同一Plan身份对账后，推翻第19条“碰撞后Body速度可直接作为路线速度”的判断。Frame 478中旧右脚Plan在平地冻结`5.990958m/s`，进入坡面后角色实际水平投影约`5.359497m/s`；同一动作时钟下预测Root Z=`10.84319`、实际Root Z=`10.44758`，提前`39.56cm`。同帧另一只在坡面新建的Plan仅差`6.6cm`。这证明测试器与Move Speed正确，错误在于把“沿地形的6m/s路程预算”当成“恒定6m/s水平位移”。现行修复从committed Locomotion Intent冻结正式请求速度，在同一Plan创建事务内先发现Upper Envelope，再按其三维弧长建立不可变的动作进度到水平Route Progress映射；动画局部Foot与Clearance仍按原动作相位，执行期不重查、不自适应。Runtime编译已通过，效果仍须干净双向run证明。
35. 用第34条同一Plan的冻结输入和Ground Envelope离线重放现行弧长反解：Frame 478旧预测Root相对实际提前`39.56cm`，新映射预测Root相对实际落后`6.70cm`，主误差缩小`32.86cm`。该结果只证明空间映射方向正确，不替代Unity干净双向run。期间曾把CSV的`plan_swing_duration=0.5s`误当成完整Action Step周期；源码确认它是`(1-liftOffPhase)*ActionStepDuration`，该事件完整周期为`0.6s`，且`TimeToLanding=(1-EventPhase)*0.6s`严格成立，因此不修改Simulation Action Step Clock，也不引入第二时钟。
36. 当前代码再次完成Runtime `0 error / 1个无关warning`与Editor `0 warning / 0 error`编译，build server已立即关闭；单一路径禁用项搜索无匹配，OpenSpec strict validate通过。Unity MCP Server、Editor插件与本机manifest均为`9.7.1`，但精确Float32 Build触发`ForceDomainReload`后，Editor插件只完成WebSocket注册，未再完成主线程的36项工具注册；Server日志随后稳定表现为每约40秒因无pong断开、约5秒后重新注册，`get_editor_state/read_console`均在应用级ping前置检查失败。同一次重载还记录`SimulationSessionHost`已释放后`FixedCharacterHost.OnEnable`重新注册的异常，原GameplayLab Play会话不能继续作为有效测试。重启MCP helper不能恢复该Unity进程的主线程命令分发；该状态不能视为Character Build或效果回归完成，第4.10及Unity效果任务继续保持未完成。
37. 重启Unity后的独立复核纠正第36条的过度归因。旧进程中的`SimulationSessionHost`释放与`FixedCharacterHost.OnEnable`异常，确实来自我没有先可靠停止GameplayLab运行态便进入Build/Reload流程；这属于执行顺序错误。新进程PID `27976`在明确非Play状态执行精确Float32 Build时没有Domain Reload，生成的Float32 Wrapper与Projection于`2026-08-12 09:17:12`实际发布成功；但现有MCP入口把完整Build同步运行在Unity主线程，超过Server固定30秒命令等待后误报超时并暂时阻塞应用级ping。随后Fixed Build调用在等待期间断连，精确Fixed Wrapper修改时间未变化，不能算发布成功。正式修复只给现有`CharacterSimulationBuildOrchestrator`增加可轮询作业生命周期：start在下一次Editor update执行原Build，status从线程安全快照返回进度，最长等待600秒；Play Mode仍由原入口拒绝，Domain Reload丢失job必须报告`job_lost`且不得自动重放。该层只解决MCP运输和可观察性，不建立第二Build实现，也不改变Definition或Wrapper业务参数。
38. 轮询Build入口已完成闭环。首次脚本刷新先编译了已修改入口、尚未导入新调度文件，产生4条`CS0103`；随后强制完整刷新导入同目录脚本，Unity Console恢复0 Error，Runtime与Editor本地工程均0 Error并关闭全部.NET Build Server。精确Float32作业`629d2363576542d3976d0e733f0f2f6d`与Fixed作业`ece9b61ecb984567a75c69cd7468bc26`都能在Unity主线程Build期间由后台status持续返回`running`，最终分别发布Float32 Program Hash `781c80537be18903c357bd73ce1149818aaccd489a2383044f7eac599da0ef74`与Fixed Program Hash `c33f258fa660bab6cea89358453f63ff830c8eec58ecc6ced04c5f2424d807fb`，共享Source Revision `d647f38bb0539d50cd73fa4b5d8f37d50a763a8f7eb6795a36f87ca0769438d5`和Projection Revision `366a1edebdce0100edca842a4c68a7fdf7ba4b2ff5faf83f117b082253917fe5`。Build结束后仅出现一次MCP WebSocket重连自身异常，清空并等待5秒未复现，项目Console为0 Error；该记录只完成产品与工具门禁，不代表IK效果通过。
39. 统一Simulation动作时钟的首次实现又在`CharacterSimulationPresentationRuntime`内用`LocalLogicTick + InterpolationAlpha`计算动画Delta，形成了Body Sample Cursor之外的第二时钟。切换0.25倍Rate Playback时`GameplayTickSystem`会重置Accumulator，同一Logic Tick的Interpolation Alpha可从例如`0.8`回到`0`，第二时钟因此倒退`0.8 Tick`并抛出`Animation logic clock cannot move backward`；正式Committed Body Sample Cursor并未倒退。现行修复直接用`bodyFrame.PreviousTick + (CurrentTick - PreviousTick) * SampleAlpha`计算同帧已呈现游标及其Delta；Body branch replacement继续通过既有Reset重新初始化，未经过Reset的真实Sample倒退仍然失败，不使用`max(0, delta)`、异常吞噬或fallback。Runtime与Editor本地工程均0 Error，Unity中的1倍/0.25倍/暂停/单步切换仍需Editor回归。
40. 第39次修复进入GameplayLab后在`Frame=582, BodyTick=605->605@1`暴露第二个边界：Committed Body表现游标已经追到最新Simulation样本时，相邻Render Frame可以合法重复同一Sample，样本Delta为0；首次实现仍把0传入Animation和Foot Grounding，触发`Foot Grounding requires valid body and delta inputs`并使未关闭的Pose frame在Session清理时继续报错。现行语义改为：Body Sample Cursor前进时才执行Fact、Animation、Foot Placement与唯一FBBIK；游标未前进时保持上一帧已提交Physical Pose，只保留Body VisualRoot、Equipment和Camera的Presentation更新。系统不放宽Grounding的正Delta合同，不复用上一Delta，不注入最小Delta，也不创建兼容路径。
41. 第40次修复后Runtime与Editor本地工程均0 Error并关闭.NET Build Server；单一路径静态搜索确认旧`LocalLogicTick + InterpolationAlpha`动画时钟已删除，OpenSpec strict validate通过。Unity刷新编译后GameplayLab连续运行超过原`Frame=582`故障窗口且Console为0 Error；同轮精确Float32作业`7e4927f1ae654d609630f9b2375a838b`与Fixed作业`592ddfd5b4484e0dbb4f7afc926c222a`分别发布Program Hash `781c80537be18903c357bd73ce1149818aaccd489a2383044f7eac599da0ef74`与`c33f258fa660bab6cea89358453f63ff830c8eec58ecc6ced04c5f2424d807fb`。MCP内存代码执行因插件CodeDom命令行过长失败，未向项目增加临时测试入口；0.25倍Rate Playback切换仍需Game View中的正式`O`输入确认，不能把1倍运行结果冒充慢放结果。
40. 干净自动run `ba8c606d71ff49d3a453d393dd921a56`包含55个gzip分块与1683行，Schema为`foot-ik-944-full-plan-v65`，Header及每行均为944列。左右Plan的Route、Ground Envelope、Clearance Path、Query与Landing在同一Plan内均无变化，Progress无回退，楼梯Executing无Path缺失或Rejected；分类楼梯帧最终Heel/Toe物理穿透最大为0，FBBIK无失败且大跳帧位置残差接近0。剩余失败是同一Plan内上楼Goal Y最大单帧变化左30.03cm、右30.91cm，下楼左35.92cm、右40.14cm。逐帧对账发现右脚42次出现角色继续前进而Landing Event Phase不变，最坏帧实际Root与冻结Path Root相差85.16cm。源码根因是Foot Feature多Contribution选择器在候选与当前拥有同一Landing Identity时，即使新候选得分更高并被`Select`选中，Accumulator仍只按Identity判断而拒绝拷贝它的新Phase、TimeToLanding与路线事实。现行修复让`Select`显式返回候选是否胜出；所有调用者始终写回被选中的完整事实，并只在候选胜出时更新对应Score。GDC第30至36页的Virtual Ground分段与分段Convex Hull本身得到保留；本轮不再把连续凸包斜线误判为算法错误，也不调查询或IK参数。该修复必须由新run证明同Plan时钟冻结、Root脱节与Goal正负往返下降后，任务3.7才可完成。
41. 修复后自动run `23515fadd2bf4c52ae905d78d599aeab`包含29个gzip分块、5776行与944列，完成3次双向路线且Console为0 Error。左右最大Ground Path单帧变化从旧run的`29.80/32.13cm`降至`5.78/8.78cm`，最大Final Goal变化从`35.92/40.14cm`降至`14.00/13.54cm`，Pelvis Current最大变化从`9.02cm`降至`1.59cm`，说明第40条修复移除了部分旧事实。但右脚仍有56个真实Swing帧在角色继续移动时Phase与Plan Progress冻结；典型Plan 16的Phase固定为`0.7595993`，角色继续前进约`42.9cm`，Current Path Root完全不动。Source Sample Identity `4833222869170760285`可由正式identity算法还原为`5/Sequence/2`，对应非循环`Walk Start`；下一事件`16604100843132520217`还原为`1/Sequence/2`，对应循环`Walk Loop`。源码确认Blend Stack把退出动画捕获为`Stored Pose`后仍将其Foot Prediction与Live Pose等价选择；Stored骨骼可合法淡出，但其Sample Time已冻结，不能拥有Simulation Action Clock。现行修复保留Stored Pose的姿势、速度、Sole Height和Plant混合，只禁止它提交Predicted Step；Landing Event、Phase、TimeToLanding和路线事实只从Live Contribution选择。该修复不增加Plan私有时钟、逐帧重规划或响应式前置，必须由新run证明Walk Start到Walk Loop交接不再冻结后才能完成任务3.7和3.8。
42. 第41条只按`Stored/Live`区分所有权的实现已被自动run `f5adb92b27e84a468f31c50e6bb10251`证伪：3356行与944列均有效且FBBIK无失败，但右脚仍有44个真实Swing时钟冻结，最大实际Root与Current Path Root偏差`69.03cm`；左右下楼Goal最大单帧变化分别回升到`27.56cm/31.24cm`，Pelvis最大变化回升到`7.40cm`。原因是退出的非循环`Walk Start`仍可作为`Live` StateMachine源或Inertial History参与姿势过渡，`Live`只说明物理Pose仍存在，不说明它仍由当前Simulation Locomotion Clock拥有。Foot速度、高度与Plant是连续表现值，可以混合；Landing Event、Phase、TimeToLanding和完整路线是离散动作事实，不能按Pose权重或Confidence选赢家。现行替代让StateMachine目标、Inertialization当前输入和Slot中最新Live目标分别拥有该离散事实，退出源和历史只混合连续表现值；任务3.7与3.8仍须新run证明后才可完成。
43. 自动入口复现出两次`FixedCharacterHost.OnEnable -> SimulationSessionHost.RegisterActor`向已释放Host注册的`ObjectDisposedException`。根因是关闭Scene Reload时同一Authoring Host会经历`OnDisable -> OnEnable`，旧`OnDisable`永久Dispose会话，而Actor在下一次Enable仍引用同一组件。正式修复让`SimulationSessionHost`以早于Actor Host的执行顺序在Enable时开始新的空生命周期，不复用旧Runtime、Registration或Diagnostics。Runtime与Editor本地编译通过；Unity连续两次停止后重新启动Foot IK Endurance并各运行12秒，项目Console均为0 Error。该结果只恢复诊断环境可信度，不代表IK效果通过。
44. 自动run `04370dd4b3bc47ebb0d8c41173f82d2a`证明退出状态不再冻结Action Step Clock，但左脚仍有24个计划在LiftOff之后才创建。典型`WalkStart -> WalkLoop`交接中旧计划完成后出现5帧无Landing Event，新WalkLoop左脚首次事件Phase约`0.58`而LiftOff约`0.1667`；鞋底安全边界修复后的run `d12c3e51b73640f6bdac304197ab4a04`把晚计划首帧Applied Lift P95从`20.03cm`降到近0，却仍在下一帧出现约`28.9cm`抬升，证明边界连续性只能消除第一帧突变，不能修复上游步态相位。
45. 源码与正式资产对账确认，WalkLoop资产已经以同一`Locomotion.Gait`周期保存`RightFootContact@0`与`LeftFootContact@18/36`。原实现让`SimulationLocomotion` Sequence每帧直接消费一条通道级elapsed；Motion owner切到MovingTurn Timeline或operation重入时，这条elapsed无法表达真实producer lifecycle。后续重基线让每个Movement producer随motion提交owner、generation、authority tick与continuous ticks，Sequence relevant occurrence按完整identity建立时钟原点，retained outgoing source不改绑incoming owner；Generated Foot Phase只在该raw clock上解析effective time。首版校验错误地把推断型攻击、闪避和转向也当作循环步态，Character Build据此拒绝了非Locomotion资产；该范围已撤销为只检查明确作者化的循环步态。run `5a926acf482d45f49cb5bfa5cb7526a1`只证明循环内Marker间隔稳定，不能证明跨producer切换或Start到Loop的双脚轨迹已经匹配。
46. run `5a926acf482d45f49cb5bfa5cb7526a1`包含1528行、44个完整gzip分块且每行944列，左右脚在全部1068个共享有效帧中来自相同Source与Contribution；晚建左脚计划精确表现为目标WalkLoop首次事件`Phase=0.5`、`LiftOff=0.1666667`，前一帧事件无效。根因不是FinalIK或左右Loop相位漂移，而是两项上游合同缺失：运行时Virtual Ground分别选本脚与对侧脚事实再比较，配对可随Contribution赢家改变；WalkStart、RunStart、RunEnd与MovingTurn没有`Locomotion.Gait` Marker，目标Loop只能从0开始。Foot Analysis v51与Artifact v22现把对侧Landing Delay、Ordinal及Cycle Offset在同一次分析中写入本脚事件，Planner只消费该原子配对；Corin正式资产按同一分析器采样结果作者化为WalkStart `R0-L13-R41-L57-R75`、RunStart `R0-L13-R33-L48-R63`、RunEnd `R0-L4-R64`、MovingTurn `R1-L10-R71`，并与WalkLoop `R0-L18`、RunLoop `R0-L15`共享唯一`Locomotion.Gait`。有限Marker时间在首尾之外保持最近真实接触段，Marker Sync每帧重建目标Locomotion Clock offset，使过渡完成后同一相位继续前进而不会被状态入口时钟再次覆盖。Runtime与Editor本地编译已通过；该实现仍必须由精确Character Build和新自动run证明左右事件、Virtual Ground split与Goal连续性后才能完成任务3.9和3.10。

47. 首次v51自动run `8d31fce58ef94ec9a6013b3ef638de01`在第37帧以`Predicted opposing landing pair is incomplete`主动失败，只形成1个19行完整分块和1个空partial，不能用于效果判断。生成Projection直接证明`OpposingEventOrdinal`在对侧Landing前最后一个烘焙采样区间从1线性插值到0，而`OpposingLandingDelaySeconds`仍线性倒计时；运行时四舍五入ordinal后得到`ordinal=0, delay>0`的半套配对。Foot Analysis v52把Opposing Event Ordinal和Cycle Offset烘焙为严格阶跃离散曲线，Delay继续保持连续倒计时；运行时完整性检查保持严格，不吞异常、不补身份、不建立兼容路径。该修复必须重新发布产品并由新的干净run验证。

48. v52自动run `a9bcc697db724261b74bd011d7ec4db3`包含2063行、54个完整gzip分块且每行944列，左右脚共享有效帧的相位中位间隔为`0.5`，证明Loop内左右脚顺序成立；但左脚晚建计划`21/105=20.00%`、右脚`22/127=17.32%`，左脚移动中无有效事件105帧，且`EventReplaced`约为左42次、右21次。Start到Loop交接中同一个物理Landing被各自`SourceSampleIdentity + EventOrdinal`编码成不同事件，目标Loop又以线性“同脚Landing到下一次同脚Landing”相位进入约`0.32~0.40`，所以Marker段虽然同步，计划仍被当作新事件替换。现行Foot Analysis v53先在同一次烘焙中解析左右Landing，再以相邻对侧Landing作为半周期边界重建Event Phase、LiftOff Phase和Route采样；Runtime为同一`LocomotionMotionGeneration + Locomotion.Gait`建立Marker Epoch，并把每个未来L/R Landing绑定为跨Start、Loop、End、MovingTurn连续的Marker Ordinal。Marker Sync交接同时对齐目标Ordinal offset；Plan只要规范事件身份连续且Phase不回退，就允许目标片段更新剩余Step Duration，不再因不同片段的局部时长或LiftOff数值结束旧Plan。Float32作业`2c45a6bfd63e4831aaf9c50539a0ad46`与Fixed作业`c0e6e0d742144620808bf058d241bd20`已发布共享Source Revision `2b2d918c63886a15fc45422dad6df753e7eade73dc2795f0b3219052aed0e46b`、Projection Revision `d1e6f2ab50bf1f3c1ec561e004bd3d3e4df8aa3bde3ad61da3e96a24f02c50f6`，生成Projection确认算法为`animation-foot-analysis/v53`，Unity Console为0 Error。该记录只确认资产与执行合同已更新；任务3.9、3.10仍须新的固定双向run证明Start、Loop、End、MovingTurn交接不再晚建或替换后才可勾选。

49. v53后的自动run `8da0b16db98a4c7ba62d89db8f9a93bc`包含52个完整gzip分块、7633行与944列，所有行同宽且FBBIK失败为0。每次双向路线首次`Walk Start -> Walk Loop`都稳定出现左脚约`0.158~0.162s`无Landing Event，Path随Plan以`EventReplaced`结束后消失；目标Loop首次左脚事件约在Phase `0.263~0.269`才出现，而LiftOff为`0.1667`。正式资产对账证明Walk Start最后左脚Marker为`0.95s`、Clip末尾右脚Marker为`1.25s`，旧过渡只在剩余`0.15s`即`1.10s`开始；因此左脚事实先结束约`0.15s`，目标Loop后进入。Run Start同样为最后左脚`0.80s`、末尾右脚`1.05s`而旧交接提前量仅`0.15s`。正式修复让Start到同速Loop的Marker同步过渡按最后完整左右脚Marker段提前：Walk为`0.30s`、Run为`0.25s`；视觉Blend仍为`0.15s`，目标Loop从同一`Locomotion.Gait`段继续剩余Pose与下一Landing事实，不保留旧Plan、不增加预测私有时钟或响应式前置。该run还证明同一Plan最大Goal跳变与Ground Path变化高度相关（左`0.837`、右`0.811`），29个计划把至少`10cm`台阶变化压进不超过`20ms`的Envelope区间；事件交接修复须先由新run验证，随后Ground Path按GDC Virtual Ground、不可通行点删除与分段连续Hull继续修复。

50. 第49条的首次交接提前修复已由精确Float32与Fixed Build发布，并生成自动run `212097aa20f54adbbbc9d50292e76ff4`：34个完整gzip分块、1363行、944列且每行同宽。左脚移动中无权威Landing Event由旧run的215行降至26行，最长无事件Render跨度由34行降至5行，证明Marker段提前量命中了Start到Loop交接；但每次双向启动仍稳定存在`8~11 Simulation Tick = 0.133~0.183s`空窗，Path同时消失且首帧为`PlanEnded/EventReplaced`，右脚仍为0。逐帧边界显示旧事件Phase到1后，目标Loop约在Phase `0.197~0.231`才取得权威身份；该间隔与PoseGraph视觉Blend `0.15s`一致。源码确认离散Predicted Step只由当前权威Live目标Contribution发布，过渡目标在视觉Blend完成前不能接管Action Step Fact。因此第49条只覆盖Marker段、没有为Blend预留时间，是被数据证伪的不完整修复。正式交接提前量改为`最后完整左右脚Marker段 + 视觉Blend`：Walk `0.30 + 0.15 = 0.45s`，Run `0.25 + 0.15 = 0.40s`；Blend本身仍为`0.15s`，不混合两个事件、不保留旧Plan、不增加预测私有时钟。任务3.11保持未完成，必须由下一run证明左脚空窗、Path缺失和`EventReplaced`归零。

51. 第50条的第二次资产提前量改动由新run `c4937f63f7ce4356b6102b1fcf3ad1cd`再次证伪：41个完整gzip分块、1882行、944列且每行同宽；左脚仍有8次空窗，权威事件边界仍为`8~11 Simulation Tick`，Path缺失42行、`EventReplaced`8次，右脚仍全部连续。目标Loop首次事件Phase由约`0.21`前移到约`0.27`，证明`gait-handoff-lead`规则真实生效，但物理空窗不受该参数控制。生成Projection中RunLoop左脚`Confidence=1`、`EventOrdinal=1`从normalized time 0起全程有效，排除烘焙曲线缺事件。源码最终定位到`AnimationSlotBlendJob.BlendFootFeatures`：离散Predicted Step虽然指定最新Live Contribution为唯一owner，却只在该脚`FootWeight > 0`的连续Pose混合分支中读取；Start到Loop的Blend Profile暂时令目标左脚权重为0，因此目标左脚事实被跳过，旧源又因不再是owner而不能发布，形成左脚独有空窗。正式修复把离散Landing Event、Phase与Route从逐脚Pose权重分支中分离，始终从最新Live目标读取；Sole速度、高度和Plant继续按逐脚权重混合，Stored/退出源仍不拥有预测时钟。无效的Walk `0.45`/Run `0.40`撤销为Marker段本身的`0.30`/`0.25`，任务3.11继续等待新run证明空窗归零。

## Rejected Designs

- 响应式Baseline之后只叠加预测Lift：没有真正的动作级Foot Path。
- 把冻结Query Route完整XYZ写入Swing Goal：实际角色与计划稍有偏差就水平拉腿。
- 用键鼠输入向量幅值缩放烘焙动作路线：项目没有摇杆速度域，会产生第二运动模型。
- 丢弃root-local Foot局部X：会破坏原动画真实侧向脚路线。
- 通过提高Rejected比例获得稳定效果：只是让原动画或响应式路径隐藏预测失败。
- Presentation私有时钟、碰撞后Body速度外推、逐帧重规划、最终Reach clamp、固定高度和FBBIK后处理。
- 用Action Motion Curve驱动Corin in-place Locomotion、从Plant或Foot Analysis重建第二Action Root、把完整同脚步幅误画成一次左右脚接触、`FootRouteWorldAlignment`渐退对齐、按当前脚世界位置推进已冻结计划。

## Tradeoffs

- 最终Foot XZ跟随当前原动画Pose，不会被未来计划水平拉走；代价是急转向时冻结Query Route可能与当前脚偏离，此时必须结束旧动作计划，而不是逐帧改写计划。
- Ground Path只改高度，能稳定解决楼梯清障；身体重心效果依赖同一Pelvis owner及时消费地形Hip，不能只做脚部抬升。
- 无Executable Plan时显式保留原动画可能暴露穿模，但不会把响应式结果冒充预测成功，规划覆盖率因此可被真实诊断。

## Hard Stops

- 不增加第二Grounding、Heel/Toe Current Query、第二Pelvis、第二Anchor、LegIK、TwoBoneIK、默认地面、固定高度、fallback、兼容reader或FBBIK后处理。
- 不通过MCP写代码，不运行Unity batchmode。
