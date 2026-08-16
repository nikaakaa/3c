## 1. 已有基础与文档收口

- [x] 1.1 保持`FootGrounding -> optional PredictiveFootPlacementModifier -> FinalIK FBBIK`唯一正式链，不增加第二Grounding、Heel/Toe Current Query、第二Pelvis、LegIK、TwoBoneIK或FBBIK后处理。
- [x] 1.2 已接入Rig v4、Calibration v4、Heel/Toe/Sole几何、唯一World Query backend、Stance/Anchor/Pelvis owner和FinalIK Pose Buffer FBBIK。
- [x] 1.3 已建立Action Step Fact、基础Foot/Ankle/Hip/Clearance路线、Ground Probe、Ground Envelope、Revision、Gizmo、Runtime Trace与CSV诊断框架。
- [x] 1.4 用最新压力采样和源码证明当前首个错误owner位于Artifact/Plan/所有权交接而非FBBIK；Executing失去Predictive输出、计划陈旧、Revision硬边与不可达Goal均有数据证据。
- [x] 1.5 对照本地GDC 2016原始幻灯片，重写proposal、design、delta spec和经验文档；旧逐轮补丁不再定义目标架构。
- [x] 1.6 对重写后的change执行strict validate并清除全部delta冲突。

## 1A. GameplayLab自动反馈环

- [x] 1.7 在共享Character Movement Test Environment中生成正式宽楼梯课程：30米宽、24级上楼、6米平台、24级下楼，Gameplay使用斜坡碰撞，FootPlacement使用逐级踏面。
- [x] 1.8 保持Local Fixed普通Play为自由输入；唯一Foot IK Automatic Variant只接管MoveAxis，保留LookAxis相机控制并自动启动Diagnostics与流式采样。
- [x] 1.9 自动源只执行一次短单向Camera-relative A/D事务：对齐起点、进入第一段楼梯、正式提交`A 1秒 -> D 2秒 -> A 1秒`，随后输入归零并结束采样；不得用世界空间横向路点冒充键盘转向，不改Transform、速度倍率或Time Scale。
- [x] 1.10 启动前以场景中唯一Course和Start/End为权威验证全部踏面、A/D安全范围、两条Traversal Ramp和Collision World闭包；允许整体移动Course，但路线不覆盖故障地形时拒绝运行。
- [x] 1.11 自动writer只消费`LiveState`完成帧流并后台压缩CSV；删除自动附加`RuntimeDebugSession Continuous`造成的第二份无界内存捕获。
- [x] 1.12 Foot IK Automatic Variant只运行唯一玩家表现，ActionTarget正式提交`None`；普通Local Fixed继续保留中立Target。
- [x] 1.13 将动画Source不可变Clip Catalog的全量校验和索引建立收敛到Source创建/换代，逐帧Prepare只处理动态采样计划与实际激活Clip。
- [x] 1.14 将Foot Feature曲线合法性校验收敛到Sequence Player创建；删除Runtime后继事件搜索与缓存，由Artifact在同一采样域原子烘焙Current与Incoming Step，运行帧只采样并绑定权威source。

## 2. Animation Biomechanical Step Artifact

- [x] 2.1 提升`AnimationFootAnalysisArtifact` format与algorithm identity，删除v26兼容reader、位置-only payload和旧generated产品读取路径。
- [x] 2.2 在同一Action Phase采样域烘焙Heel、Toe、Sole、Ankle、Knee、Hip的root-local位置路线。
- [x] 2.3 烘焙Sole与Ankle的root-local旋转路线，并保存动画脚掌朝向基准。
- [x] 2.4 从同一动作区间生成Animation Foot Planar Route与相对参考Foot Path的Clearance，不保存世界高度或KCC位移。
- [ ] 2.5 生成精确Release、LiftOff、ApproachContact、Landing边界，以及Locked/Sliding/Unlocked区间和连续Constraint Weight。
- [x] 2.6 生成Support Weight、Support Leg Length、Compression Reserve、Knee Bend Plane、Support Foot Pivot位置与权重。
- [x] 2.7 原子保存对侧Landing identity、time、cycle和root-local Sole pose。
- [ ] 2.8 更新artifact codec、hash、store、inspector与analysis source identity；未知字段、非有限值和旧版本必须明确失败。
- [x] 2.8A 在同一Artifact采样点原子保存Current与Incoming Step；Incoming必须携带完整事件、时钟、路线、Biomechanical事实和相对当前采样的Landing时间，不允许Runtime搜索、补建或缓存猜测。
- [x] 2.8B 为Current与Incoming显式烘焙离散Source Landing Cycle Offset，以该identity切分全部事件字段并删除Runtime从TimeToLanding反推cycle；重建Corin Projection后验证每次Current换代的前一完成帧Incoming恰为该事件。
- [ ] 2.9 建立Artifact Flat Reconstruction Gate，逐相位输出Foot/Sole/Ankle/Knee/Hip位置与Sole/Ankle旋转误差，并阻止超过固定容差的artifact进入Projection。
- [x] 2.10 重新生成Corin Start、Loop、Stop、MovingTurn全部可达Foot Analysis artifact，不保留旧资产或运行时补建。

## 3. Projection与Action Step事实

- [ ] 3.1 扩展`AnimationPredictedFootStepSample`和Projection payload，使完整Biomechanical Step Event以不可拆分值发布。
- [x] 3.2 Pose字段可按正式Blend混合；Landing identity、Clock、路线、Constraint、Support Leg、Orientation与Pivot必须从一个权威source原子选择。
- [x] 3.3 删除Stored Pose、退出source、Inertial History和逐脚Pose Weight复活旧事件或拆开混合字段的路径。
- [ ] 3.4 统一Start、Loop、Stop、MovingTurn的左右脚Marker Epoch、Occurrence、Cycle与Phase，使每个当前事件在LiftOff前成为PreSwing事实。
- [ ] 3.5 更新Definition Build校验和Projection schema；缺少新字段、Flat Reconstruction失败或event不连续必须阻止Float32/Fixed产品发布。

## 4. Committed Future Body Transform Trajectory

- [ ] 4.1 由Simulation/KCC发布覆盖剩余Action Step的Position、Facing、Linear Velocity、Angular Velocity和trajectory identity；Foot Placement不得自行解释输入或Visible导数。
- [ ] 4.2 删除Predictive planner中固定`trajectoryCurvatureDegreesPerSecond = 0`与Body Yaw猜曲率语义，使位移与有限Facing来自同一committed trajectory。
- [ ] 4.3 用Future Body Transform与Artifact root-local Sole/Ankle/Hip路线建立唯一未来世界路线，保留动画局部X、Z和旋转。
- [ ] 4.4 Plan创建帧只允许一次刚性重基，使`PathStartPhase`的Artifact Foot Route与已提交接触点或已执行Sole正下方重合；执行Sole另行保存净空连续性，重基整步冻结。
- [ ] 4.5 A/D、W/S或camera-relative意图改变时，只在committed Landing位置或朝向误差超过鞋底几何边界后创建离散后继Revision。
- [x] 4.5A 历史实验已证明“每个权威Landing Event至多一笔Intent Revision”能阻止同一源Plan反复换路；自动A/D run `35d23e6892f24566808e0276a3ec28be`中，每个事件最多只出现原Plan加一个Revision。后续run证明该事件级限制会让再次偏离的后继Plan继续过期执行，本规则由4.5B替代，不反勾历史完成项。
- [ ] 4.5B Revision资格改为每个不可变源Plan至多一次；Revision提升后新的Active Plan可在committed轨迹再次越过几何边界时继续离散修订，但每只脚同一时刻仍只有一个Active与一个过渡槽。
- [ ] 4.6 后继Revision从当前已执行Sole位置、线速度与Body角速度连续重基；Ground Probe起点必须是该Sole投影到旧计划当前支撑面的点，不得使用旧Envelope的异位XZ；新计划未Executing前保留旧输出，Rejected后继连续退回原动画。
- [ ] 4.7 删除事件换代、Plan状态或generic`NonFinite`导致Executing输出当帧归零的路径，并为真实失败分别发布typed reason。
- [ ] 4.8 Plan创建时原子冻结Action Step时长、Future Body时间范围与路线时间映射；运行中只同步权威相位，时长变化必须进入离散Revision，禁止用新时长采样旧轨迹。
- [ ] 4.9 当前Plan进入`ApproachingContact`、Intent Revision结束且现有Revision槽空闲时，`IncomingPredictedStep`必须预建唯一Event Successor，并从Stance上一完成帧真实提交的Anchor Sole与Surface连续重基；Intent Revision与Event Successor只允许顺序复用同一槽。
- [ ] 4.9A Event Successor成为Current前不得参与Goal或Blend；事件identity换代时必须原子提升为Active，并只由新Plan自身`Release -> LiftOff`权重与旧Landing Stance/Anchor完成交接，禁止在新Swing中按Render Delta混合旧Landing目标。
- [ ] 4.9B Plan必须在`ReleasePhase`进入Executing并执行`Release -> LiftOff`连续输出权重；Ground Path进度独立保持到`PathStartPhase`才开始，禁止用几何起点截断所有权淡入。
- [ ] 4.9C Event Successor成为Current前必须按当前身体位置和朝向重新验证冻结轨迹；过期Successor不得提升、提交Landing/Anchor或产生下一Successor，必须以`MotionDeviationExceeded`退出并从当前真实Sole与Support创建Current Event计划。

## 5. GDC Foot Path与Ground Envelope

- [ ] 5.1 由本脚动画Foot Path和权威对侧接触构造Virtual Ground分段路线；对侧接触只提供空间拓扑，不强迫本脚按对侧phase经过。
- [ ] 5.2 沿`当前本脚支撑 -> 对侧接触 -> 本脚预测落点`的Virtual Ground各分段执行唯一Sphere/Capsule检测，并在可达性判断前保存全部位置、法线与query identity。
- [ ] 5.3 按前后和高低排序命中，验证法线并建立Edge Plane。
- [ ] 5.4 在Convex Hull前对排序后的正式支撑链验证Step Up/Down、gap、坡度、鞋底范围和Support Leg Reach；Height Discontinuity只判断真实Edge Plane，不得在中间踏面尚未收集前用稀疏端点提前拒绝整条Plan。
- [ ] 5.5 对剩余点构造连续二维上侧Convex Hull；Ground Envelope保持feet-only，不驱动Pelvis。
- [ ] 5.6 最终Swing保持Native Sole XZ，唯一Y为`GroundEnvelopeHeight + AnimationClearance`；禁止冻结Path XYZ拉脚和Native/Predicted Y双owner。
- [ ] 5.7 使用Calibration Heel/Toe验证唯一支撑平面物理净空，不增加Heel/Toe Current Query、固定高度或默认地面。
- [ ] 5.8 台阶边缘Sphere多命中必须以前一支撑做有向可达选择；同Foot Rate合并保留正式支撑身份，Landing、Body Support终点与Ground Envelope终点从验证后的同一链末端原子提交。

## 6. Constraint、Landing与GDC身体层

- [ ] 6.1 在现有Stance owner中用Artifact事实收口`Locked -> Sliding/Releasing -> Unlocked Swing -> Approaching -> LandingBlend -> Locked`，连续Constraint Weight不得在事件边界硬切。
- [ ] 6.2 Landing提交同一Plan Landing Pose、Surface identity、Anchor local point/normal、Committed Sole Pose与Successor Step Start，不允许Current Surface替换预测支撑。
- [ ] 6.3 Locked保持完整世界Goal；Sliding只允许支撑面内有限移动；Unlocked不消费旧Anchor；Idle继续使用现有单一Stance owner归位和锁脚。
- [ ] 6.4 构造独立Body Support Path：`last support -> opposing support -> predicted landing`，不得复制Foot Ground Envelope或离散KCC台阶Y。
- [ ] 6.5 用Animation Hip relative path、Support Leg Weight、Length、Compression Reserve和Knee Bend Plane生成预测Hip与可达区间。
- [ ] 6.6 直接应用Body Support Path位移，临界spring只增加support-leg pull并消除bounce；输出一个Pelvis Pre-Solve Transform。
- [ ] 6.7 实现上坡脚掌趋于水平、下坡脚掌贴坡、跑步保留动画的Foot Orientation策略，并受同一reach约束。
- [ ] 6.8 临近接触时按Artifact pivot weight围绕Locked Support Foot应用有限body/pelvis rotation，不移动锁定Foot Goal或创建第二body owner。

## 7. 集成、诊断与发布

- [ ] 7.1 更新统一Foot Placement输入、Plan、Query、Stance、Pelvis与Final Goal合同；FinalIK继续只执行一次FBBIK。
- [ ] 7.2 更新Scene/Game Gizmo：完整绘制Artifact Route、Future Body Transform、Virtual Ground、Capsule Query、Ground Envelope、实际消费点、Revision和Body Support Path，不显示文字或伪Path。
- [ ] 7.3 更新Runtime Trace、Inspector和CSV，覆盖Artifact重建误差、事件所有权、Future Body Position/Facing/速度、当前与Plan冻结Trajectory Curvature及availability、query/reach、Goal、Support Leg、Pelvis、Pivot和FBBIK两层残差。
- [ ] 7.4 保证CSV Header/Value等宽、列名唯一、左右脚字段对称，更新流式耐久writer与manifest合同。
- [ ] 7.5 删除旧artifact、旧Projection、旧Plan字段、旧诊断列、旧配置和失效命名，不保留fallback或兼容路径。
- [ ] 7.6 完成单一路径静态搜索、OpenSpec strict validate、Runtime/Editor编译和精确Float32/Fixed Character Build；构建服务器按项目规则关闭。
