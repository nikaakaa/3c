# IK 效果诊断记录

## 目标

这份文档持续记录 `replace-pose-ik-with-finalik-full-body-solver` 的端到端效果、采样证据、Lyra 对照、根因、修复状态和业务取舍。它不代替 proposal、design、spec 或 tasks。

## 当前验收状态

当前唯一业务目标：让`FootGrounding -> FinalIK FBBIK`纯响应式链达到Lyra `CR_Mannequin_FootPlant`的可见效果。Predictive Modifier只允许作为未接线能力存在；未经用户再次明确授权，不得接入Corin、不得用于解释响应式穿模，也不得用未来查询掩盖Current Grounding偏差。

第25节把Corin正式图接入Predictive Modifier属于实施越权。它虽然通过Document和Character Build发布，但不是用户批准的业务状态，因此该次“闭环”无效。第25节保留为错误历史，不反勾；第26节负责正式撤线并重新完成响应式Lyra对账。

已保留的有效结论：最新`17359`证明第24节把离散高踏面直接写回Swing脚spring Value会制造单帧吸附；FBBIK旧实现提前计算相对position offset，会被后续`LimitBend`改变参考位置。对应源码修复继续保留：鞋底硬安全约束只属于Plant Contact，Foot Placement Position作为绝对effector target交付。它们不能代替对Lyra响应式Current Grounding本身的逐项核验。

第26节的静态Live Snapshot只证明当时抽样帧的两个Fixed Actor处于Anchored且残差为零，不是楼梯运动验收。`410f`慢放采样再次证明运动期存在大面积下陷，因此第26节“构建、编译和静态抽样完成”保留为历史事实，但效果重新打开，不能继续写成已闭环。

正式构建先刷新了19个过期Foot Analysis artifact；构建后的`btsmtl.validate`通过。Document因生成上下文变化进入`Conflict`后按正式流程rebase为`DocumentDirty`，新dry-run只规划5项预期拓扑变更，apply返回`applied=true`、`saved=true`、`syncState=Clean`，source revision为`b323dff2dcdadfb340bce98a469b03b78fb22cb4296d5dc11951e9f4f0efbae6`。最终Float32/Fixed构建后再次checkout为`Clean`并通过validate；已打开Unity强制刷新与脚本编译无项目错误，GameplayLab成功启动，20秒内没有Foot、FullBodyIK或Presentation failure。Editor保持Play Mode，用户可以直接测试；Console中的ShapeProjection `AsyncGPUReadback`断言属于用户已要求暂不处理的独立问题。

用户于 2026-08-08 提供的运行截图显示：

- 平地时身体和骨盆明显下移，双腿长期压缩。
- 脚掌在平地、台阶边缘和斜坡上穿入碰撞面。
- 台阶和斜坡上的膝盖弯曲方向、腿链形状及上身代偿异常。
- 长时间运行后发生 `Foot Placement final Foot Goals have no common pelvis reach interval.`。

已记录的一次错误发生于 `gameplay-lab-player` 第 40887 表现帧，调用链为：

`FootGrounding最终双脚Goal -> Pelvis Reach共同区间 -> 区间为空 -> Presentation Faulted`

## 修复时间线

本表只追加，不用新结论覆盖旧改法。`有效`表示改动仍属于当前正式链；`被替代`表示历史任务保持完成，但对应处理不再是当前实现；`待修`表示本轮已经有数据证据但尚未改源码。

| 顺序 | 用户现象与输入 | 当时证据 | 实际改动 | 随后结果与副作用 | 当前状态 |
| --- | --- | --- | --- | --- | --- |
| 第18节 | 运动时普通Foot Goal被脚速和Plant Confidence压没，锁脚与普通接地混成一个总权重 | 世界脚速包含Actor平移，烘焙Sole Local Velocity又被Body速度重复放大 | 删除普通Goal上的Plant Confidence与世界脚速总闸门，只让烘焙Sole Local Velocity参与contact | 普通Grounding可以持续输出；contact仍是项目后置扩展 | 有效 |
| 第19节 | 用户要求直接对齐真实Lyra，而不是继续修旧自研Foot Placement | 本地`ABP_Mannequin_Base -> CR_Mannequin_FootPlant`导出确认单脚一次Sphere Trace、Foot/Normal/Pelvis SpringInterpV2和ProcessFootOffset顺序 | 建立`CharacterLyraCurrentGroundingSolver`，迁移为唯一Current Grounding；保留项目Stance、Reach和FBBIK作为后置职责 | 建立了后续所有CSV可对账的Lyra基线，但端到端仍不等于Lyra最终Basic IK链 | 有效基线 |
| 第20节 | 平地整体下沉、楼梯和斜坡穿模、Reach异常 | 旧CSV和UE 5.7源码证明项目把SphereCast球心当Hit Location，并把Target Offset算成相对动画Ankle差值 | Location改为Impact Point；Target Offset改为Component绝对竖直坐标；旋转改为上方向到Hit Normal的最短旋转 | 消除约半米系统性下拉和错误朝向；没有解决鞋底几何穿透 | 有效 |
| 第21节 | 斜坡、楼梯立面和鞋底仍穿模 | Current Query的minimum normal dot为`-1`；最终Ankle没有Heel/Toe鞋底约束 | 复用55度坡度门槛拒绝立面；在Lyra/anchor之后按Calibration Heel/Toe立即抬升Goal | 平地和斜坡明显改善，但楼梯支撑面切换把离散高度直接写入Goal，出现吸附和跳变 | 坡度过滤有效；无条件硬抬升被替代 |
| 第22节 | `a619`显示楼梯跳变和A-B-A交接 | 大Goal Y变化与spring后鞋底清障同帧，Lyra候选相对平滑 | 在现有Stance中用AnchorBlend控制spring后清障，不加第二状态 | AnchorBlend为零的Swing脚也失去清障，跳变减轻但运动穿模回归 | 被第23节替代 |
| 第23节 | `09979`显示Swing严重穿模，AnchorBlend为零时实际清障恒为零 | Current Surface有效，Sole Clearance目标存在，但spring后平移被Anchor资格关掉 | 删除spring后AnchorBlend清障，把Sole Clearance Target并入唯一Foot Offset SpringInterpV2 target | 目标连续，但安全Target不等于安全Current；spring追赶期仍穿模 | 有效基础，安全性不足 |
| 第24节 | `0ef04`显示左右最大穿透`0.178420m`和`0.185388m` | Query命中正确踏面、FBBIK近零残差，Current Offset落后安全Offset Target | 所有阶段把向上鞋底缺口写回同一个spring Value，并清除向下Velocity | 不穿模，但`17359`证明Swing单帧被抬高`0.08m`至`0.135m`，视觉吸到上一级 | 被第25节替代 |
| 第25节 | `17359`显示不穿模但明显跳变，另有独立FBBIK大残差 | 全部大于`0.05m`的硬约束都发生在Swing；少数Goal连续但Solved Position偏离`0.42m`至`0.46m` | 硬鞋底约束收窄到Plant Contact；FinalIK改收绝对Foot Position并加残差失败边界；曾越权把Predictor接入Corin | Swing跳变来源被移除，FBBIK绝对目标修正；运动Swing穿模重新暴露；预测接线不符合业务目标 | Plant/FBBIK部分有效；预测接线已撤销 |
| 第26节 | 用户要求纯响应式达到Lyra效果；高台阶静态接触却不锁脚 | `17359`中高台阶最终鞋底已贴面，但旧contact距离仍约`0.289m`，因为它在Lyra spring之前读取动画脚 | Contact距离改为Lyra spring候选Heel/Toe到当前面的距离；Corin删除Predictor并正式重建发布 | 静态高台阶可以按真实候选鞋底进入contact；只完成静态抽样，没有证明运动楼梯穿模已解决 | 有效，但验收不完整 |
| 第27节 | `410f`慢放时走动踩台阶仍下陷和穿模 | 240帧、311列；左右共134个大于`5mm`的穿透脚帧均已有合法Hit且FBBIK残差为零；46帧是释放中的旧anchor，88帧是同surface Swing spring滞后；无A-B-A | 本轮先修正文档和根因归属，尚未改源码 | 已证明不是单纯算法上限，也不是Query或FBBIK；需要分别修复anchor交权和Swing响应边界 | 待修 |

## CSV定量结果

旧CSV文件已由用户删除，不再恢复或读取；旧样本只保留下移约0.5米、Hit Location与Impact Point相差约Sphere半径等已经写入本文的历史结论。

本轮唯一只读采样：

`3cDemo/Client/3C_Client/Assets/Scenes/Standalone/foot-ik-17359cf4a2b94eb2ba70bd24a9a9b290.csv`

该文件SHA-256为`A25893B2C9C09E7C4ACCC0D7887B503F00485FB8740BBB0C2354525ABCD5E7CA`，包含Frame 2054至2293共240个表现帧、311列：

- 240帧均未编译Predictive Modifier，FBBIK backend一致且solver failure为空。
- `Current Offset - Unconstrained Offset`逐帧等于`Sole Constraint Offset`，可直接裁决离散改写发生在现有Stance owner。
- 左脚9帧、右脚10帧的`Sole Constraint Offset`超过`0.05m`，全部处于Swing且没有anchor；最大为左Frame 2234的`0.134998m`和右Frame 2162的`0.128789m`。
- Frame 2234左脚支撑面从`0m`切到`0.24m`：约束前Goal Y只变化`+0.014391m`，硬约束额外写入`+0.134998m`，最终Goal单帧变化`+0.149389m`。
- Frame 2162右脚支撑面从`0.24m`切到`0.48m`：约束前Goal Y变化`-0.007670m`，硬约束写入`+0.128789m`，最终Goal变化`+0.121119m`。Frame 2110的`0.24 -> 0 -> 0.24m` A-B-A也产生`+0.117124m`约束和`+0.109043m`最终跳变。
- 其它同类峰值包括右Frame 2123的`0.107885m`、2174的`0.123962m`，左Frame 2116的`0.080135m`、2167的`0.078676m`。Surface往返会放大个案，但单向交接同样触发，故不是必要条件。
- 跳变帧的Lyra候选相对平滑，Pelvis、Anchor和FBBIK大多只是消费硬改写后的Goal；广泛吸附归属于Swing阶段Stance硬约束，不是FinalIK放大。
- 独立异常出现在左Frame 2228至2230：Goal约为`(0.0547, 0.2101, -0.4264)`，Solved Foot约为`(-0.0448, 0.5544, -0.6463)`，位置残差达到`0.420475m`，随后两帧为`0.460385m`；约束为零、Pelvis平滑且另一只脚准确。右Frame 2258另有`0.068919m`残差。

结论：第24节“所有阶段都立即把向上安全缺口写回Value”在运动业务上不成立。Current-only查询无法同时保证Swing同帧零穿透、离散踏面交接连续和无未来信息；Plant Contact可以硬保安全，Swing必须保留spring连续性，并由图中已存在的Predictive Modifier提前抬脚。独立FBBIK异常必须修正绝对目标交付，不能继续用求解前参考Pose计算相对offset。

## Lyra 5.7 已确认事实

参考来源：

- `D:/UE_Project/LyraStarterGame/Content/Characters/Heroes/Mannequin/Animations/ABP_Mannequin_Base.uasset`
- `D:/UE_Project/LyraStarterGame/Content/Characters/Heroes/Mannequin/Rig/CR_Mannequin_FootPlant.uasset`
- `C:/Program Files/Epic Games/UE_5.7/Engine/Source/Runtime/ControlRig/Private/Units/Highlevel/RigUnit_WorldCollision.cpp`
- `C:/Program Files/Epic Games/UE_5.7/Engine/Source/Runtime/ControlRig/Private/Units/Highlevel/RigUnit_AimBone.cpp`

已确认行为：

1. 真实执行 gate 是 `DisableLegIK <= 0 && !UseFootPlacement`。`IsOnGround`、`GroundDistance`和`PelvisBlendSpeed=0.5`没有直接进入该 gate。
2. `ProcessFootTrace`从动画 IK Foot 上方 `0.5m`扫到下方 `0.5m`，Sphere Trace 半径为 `0.05m`。
3. UE 5.7 Control Rig 源码把 `HitResult.ImpactPoint`转换到 VM 空间并写入 `HitLocation`。
4. `TargetFootOffsetZ`使用该 `HitLocation`在 Control Rig Component/VM 空间中的绝对竖直坐标，不执行 `HitLocation - AnimatedAnklePosition`。
5. Pelvis Target 无条件取左右 Target Foot Offset 的最小值，再由资产实际连接的 SpringInterpV2 更新 Current Pelvis Offset。
6. Foot Offset 的目标是 `TargetFootOffset - CurrentPelvisOffset`，最终位置保持 `AnimatedAnkle + CurrentPelvisOffset + CurrentFootOffset` 的顺序。
7. `AimBoneMath`以局部上方向为 Primary Axis，Secondary Weight 为零，使用上方向到平滑 Hit Normal 的最短旋转，再乘回动画 IK Foot 相对 root 的旋转。
8. 平地 Hit Normal 等于角色上方向时，Lyra 保留输入动画脚旋转，不重新投影 forward，也不把动画摆脚压平。
9. Lyra 当前 Foot Plant 没有 Swing、stance、抬脚高度或 locomotion 状态筛选。项目 contact/anchor/reach 属于后置稳定层，不是 Lyra 原生逻辑。

本地楼梯内容进一步确认：已检查的Lyra楼梯Static Mesh使用`Use Complex Collision As Simple`并以真实阶梯几何参与查询；对应测试地图中没有发现替代这些楼梯的隐藏Blocking Volume。Lyra仍只执行每脚一次Sphere Trace，没有楼梯专用斜坡查询、surface迟滞或Heel/Toe Current Query。它的好效果主要依赖原动画摆脚、Mannequin脚部几何、Foot/Pelvis spring和最后的Basic IK共同成立，不是通过把楼梯碰撞偷偷换成斜坡。

## 已确认根因

修复前的正式链同时存在两个空间错误：

- `CharacterFootPlacementQueryHit.Location`保存 SphereCast 球心停止位置，而 Lyra `HitLocation`实际是 `ImpactPoint`。
- `CharacterLyraCurrentGroundingSolver.TraceFoot`使用 `Dot(Location - AnimatedAnkle, Up)`形成 Target Offset，而 Lyra 使用 Impact Point 的 Component 绝对竖直坐标。

这会把约半米的动画脚踝高度差直接写成负 Target Offset，随后把双脚 Goal 和 Pelvis 一起向下拉。FBBIK 残差很小只说明 solver 正确执行了错误目标。

旧脚旋转还通过投影forward和`LookRotation`重建方向，平地会丢失动画脚pitch，斜坡会得到与Lyra `AimBoneMath`不同的旋转。

此前根因依次是Current Grounding未拒绝立面、最终Ankle没有鞋底几何约束、鞋底间隙位于Lyra offset spring之后，以及清障资格错误绑定Anchor。`0ef04`进一步确认安全target不等于安全current；`17359`则反证了把同一硬约束无条件应用到Swing：穿模消失的代价是离散高度直接写入Value。它还把历史大残差定位为FinalIK目标参考顺序错误，而不是Stance输入。

## 本轮源码修复

当前源码修改为：

- `CharacterFootPlacementQueryHit.Location`改为 Unity `RaycastHit.point`，即当前命中的 Impact Point。
- `TargetOffset`改为 `PoseRoot.InverseTransformPoint(hit.Location).y`，表达 Impact Point 的 Component 绝对竖直坐标。
- Foot Goal rotation 改为 `FromToRotation(ComponentUp, CurrentHitNormal) * AnimatedAnkleRotation`。
- 删除 Current Grounding 中基于 forward 投影和 `LookRotation`的旋转重建。
- Current Grounding直接使用现有55度最大坡度换算的minimum ground normal dot，在同一次SphereCast命中页中拒绝立面和锐边。
- 唯一Current Surface、Lyra Target Offset、目标Hit Normal与Calibration Heel/Toe生成非负`Sole Clearance Target`。
- `Target Offset + Sole Clearance Target - Current Pelvis Offset`成为既有Foot Offset `SpringInterpV2`的唯一target；没有第二spring、参数或查询。
- Stance使用当前平滑Ankle Rotation和同一Current Surface复核spring候选Heel/Toe；Plant Contact仍穿透，或非Plant脚在同一surface上从上一帧非穿透连续进入本帧穿透时，才把沿Component Up的最小正修正累加回同一个Foot Offset spring Value，并把该状态的向下Velocity归零。
- Swing仍计算并消费完整`Sole Clearance Target`；新surface首次命中的大缺口`Sole Constraint Offset`为零，不直接改写Value或Velocity。同surface连续跨面只消除本帧越界；当前Corin不接预测，必须继续证明响应式Current Grounding的目标、平滑和鞋底语义与Lyra一致。
- 向下支撑面交接不夹紧，继续由原SpringInterpV2释放；短暂A-B-A因此保留上一帧安全高度记忆，不新增surface迟滞状态。
- Anchor只捕获约束后的surface-local稳定位置，不再拥有鞋底清障资格。
- Anchor释放或不可达后继续使用当前surface驱动的同一Foot Offset spring，不创建clearance blend。
- Diagnostics、Runtime Trace、Inspector与CSV发布`Target Offset`、`Sole Clearance Target`、合成`Offset Target`、`Unconstrained Offset`、`Sole Constraint Offset`、约束后`Current Offset`与`Residual Sole Penetration`。
- FullBodyIK先对Foot应用pre-rotation，再把`FootPlacementEffectorTarget.ComponentPosition`直接交给FinalIK绝对`effector.position`；不再在`LimitBend`修改参考腿链之前计算一次性`positionOffset`。
- 满位置权重Foot Placement Goal求解后残差超过`0.001m`时返回`FootEffectorResidualExceeded`，保留目标与结果诊断并阻断FinalPublication。
- Reach 区间失败日志补充 Render Frame、Lyra Target/Current、全局升降范围、左右 Hip、最终 Ankle Goal、Goal Weight、腿长、左右可达区间与最终交集。

本轮没有增加第二查询、固定高度补偿、第二Pelvis或第二IK，也没有修改FBBIK参数、Goal权重或预测策略。

## 剩余假设与验证顺序

### H1：鞋底间隙绕过Lyra spring

状态：`17359`反证“所有阶段写回同一Value即可兼得连续与安全”。超过`0.05m`的写回全部发生在Swing并直接形成Goal跳变。本轮收口为Plant Contact单向硬约束；Swing只走spring与显式Predictive Modifier。

### H2：合法上下踏面反复切换

状态：只解释局部放大。`17359`右Frame 2110出现A-B-A，但Frame 2162、左Frame 2234等单向跨级也产生同量级吸附。不得据此增加第二查询或独立surface迟滞状态。

### H3：Anchor捕获、释放或surface交接

状态：未成立。0ef04超过`0.01m`帧中左46/46、右46/47没有有效anchor；修复只保留Anchor锁点交接职责。

### H4：Pelvis Reach放大单脚台阶突变

状态：未成立。Pelvis变化远小于`0.17–0.18m`穿透，仍只消费Lyra target和最终脚可达区间。

### H5：FinalIK放大输入

状态：未成立为广泛楼梯跳变根因，但独立异常成立。`17359`多数跳变由FBBIK准确跟随硬Goal；左Frame 2228至2230却出现`0.42–0.46m`残差。根因是旧offset在FinalIK内部`LimitBend`改变参考Foot位置前计算，修复为绝对effector position并增加`0.001m`失败边界。

## 本轮沉淀的经验

- 安全Target不等于安全Current，但“把Current强制安全”也不等于视觉正确。必须先按Plant Contact与Swing区分业务阶段。
- Plant Contact需要非穿透优先，允许同一Value单向写回；Swing需要轨迹连续，不能把刚进入Current Query的离散高踏面直接写进Value。
- Current-only无法同时知道未来落点并保证Swing不穿、不跳。提前跨级属于已有Predictive Modifier职责，不能塞进Stance或增加第二查询。
- FBBIK residual用于区分输入与solver。Goal已穿模而residual近零时，调solver只会更准确地执行错误输入。
- FinalIK绝对目标不能先降成依赖旧Foot参考Pose的offset；`ReadPose/LimitBend`可能改变该参考。目标要在内部预处理后仍保持绝对语义。
- Surface切换、Anchor和Pelvis必须以“是否为广泛穿模必要条件”裁决，不能因为个别帧同时变化就先加状态或参数。
- 用户反馈的静态高踏面不锁脚与运动期穿模不是同一个owner。本轮只修复已由0ef04证明的运动穿模；静态contact资格仍应单独采样后检查surface distance与Plant Contact，不能顺手改阈值。
- 当前采样没有Predictive Modifier。单向约束解决已查询踏面的物理穿透，但“踏空”和提前抬脚弧线仍需区分动画输入与显式Predictive Modifier，不能让Current Grounding猜未来。

## 业务 Tradeoff

### 严格复刻 Lyra Current Grounding vs 增加鞋底最终防穿透

- 严格停在Lyra Ankle Goal：便于逐项对账，但Corin有实际鞋底长度，无法保证Heel/Toe不穿过斜坡。
- 同一Stance Stabilization增加鞋底间隙：当前选择。Lyra current仍单独可观察，最终Baseline明确记录额外平移；代价是最终Ankle Goal不再逐值等于Lyra。

### Plant Contact硬安全 vs Swing连续轨迹

- 所有阶段硬安全：当前帧不穿，但`17359`证明Swing会被单帧吸到高一级，破坏步态。
- 所有阶段只走spring：轨迹连续，但`0ef04`证明Plant Contact仍可在追赶期明显穿入踏面。
- 按接触阶段分工：当前选择。Plant Contact对同一Value执行单向硬约束；Swing保留spring连续性并由显式Predictive Modifier提前抬脚。代价是未发布或无有效预测时，Swing不承诺Current-only同帧零穿透；这是保留唯一查询和唯一状态后的明确业务边界。

### 沿Component Up vs 沿Surface Normal

- 沿Surface Normal位移最短，但会改变脚步X/Z和楼梯前后落点。
- 沿Component Up保留动画水平步幅与anchor落点；代价是坡越陡抬升越大。55度门槛保证分母有限，因此选择Component Up。

### 单次Ankle SphereCast vs Heel/Toe双查询

- 双查询更能描述跨台阶边缘的两个支撑面，但会增加第二current support owner和更多边缘切换。
- 单次查询把命中面作为当前鞋底支撑面，边缘可能略保守，但保持Lyra查询数量和唯一支撑权威，因此继续使用单次查询。

### Reach 区间硬失败 vs 自动释放或降低某只脚

- 硬失败：当前合同。能保留错误现场，避免系统悄悄牺牲一只脚并把骨骼问题伪装成可运行。
- 自动释放或降低某只脚：运行连续性更好，但必须定义业务上哪只脚可以被牺牲、何时交权以及如何避免脚滑；这会改变现有 spec，不应作为临时容错。

本轮保留硬失败，只增强错误数据。先确认修正后的 Goal 是否自然消除冲突。

### Lyra Two Bone IK vs 项目唯一 FBBIK

- Lyra Two Bone IK：单腿行为更直接，但会恢复第二 solver，并与手部和全身目标形成分裂路径。
- 唯一 FBBIK：继续满足项目统一全身目标和单次写 Pose 的架构，但腿弯曲更依赖 Rig reference plane、Pelvis Goal 和 Profile。

项目继续使用唯一 FBBIK。Goal 正确后若膝盖仍异常，修 Rig/Profile，不恢复 LegIK 或 TwoBoneIK。

## 下一次 Unity 采样要求

下一份新CSV至少检查：

- 平地左右 `Hit Location`与`Impact Point`应一致。
- Current Grounding的minimum ground normal dot应约为`0.5735764`，楼梯立面不应再成为选中Hit。
- `Target Offset + Sole Clearance Target - Pelvis Current`应等于`Offset Target`；Swing无anchor时Sole Clearance Target不得被归零。
- 新surface首次命中的Swing帧`Sole Constraint Offset`必须为零；同surface连续跨面时`continuous_sole_contact=true`且只消除本帧越界。所有帧仍满足`Unconstrained Offset + Sole Constraint Offset = Current Offset`，约束大于零时Heel/Toe不得低于唯一支撑面。
- Corin必须显示`has_modifier=false`；Baseline Goal直接进入唯一FBBIK，不得出现Future Landing query或Modifier rewrite。
- 支撑面降低时Sole Constraint Offset应回零，Current Offset继续由原spring向下释放；不得出现独立的spring状态外鞋底平移列。
- 平地左右Target Offset、Pelvis Target、Pelvis Current和Reach Resolved不应稳定接近`-0.5m`。
- 左右 Baseline/Final Goal与FBBIK Solved Pose residual；满权重Foot residual应不超过`0.001m`，否则必须出现`FootEffectorResidualExceeded`且该帧不发布。
- 平地、斜坡、台阶上的最终脚掌视觉位置。
- 长时间运行是否仍出现共同 Reach 区间错误；若出现，直接保留新增的完整错误文本。
- Goal 正确但膝盖异常时，再采集 Hip/Knee/Ankle 求解前后位置和 bend constraint。

验证对象按顺序为 Local Float32 Corin、Local Fixed Corin、TrainingEnemy。禁止 Unity batchmode；Character Build 和发布只由用户明确触发。

## 第26节纠正：高台阶锁脚读错处理阶段

### 输入

- 唯一CSV：`foot-ik-17359cf4a2b94eb2ba70bd24a9a9b290.csv`，240个数据帧、311列、`has_modifier=false`。
- 本地Lyra导出：`ABP_Mannequin_Base -> CR_Mannequin_FootPlant`。每脚从动画IK Foot上方`0.5m`到下方`0.5m`执行半径`0.05m`的Sphere Trace；Hit Normal使用`8/1`spring，Foot和Pelvis使用`2.5/1/0.2`spring；资产内没有项目式contact、anchor或脚锁状态机。
- 用户现象：高台阶上的脚视觉已接触踏面但没有锁脚，运动期则在“不穿模”和“跳变”之间反复回归。

### 数据证据

左脚有45帧同时满足：Plant Confidence为`1`、动画鞋底速度约`0.02m/s`至`0.14m/s`、合法Current Surface存在、最终Heel/Toe平面距离约为`0m`至`0.002m`；但旧`surface distance`约为`0.289m`，Contact State始终为Swing且`HasAnchor=false`。Frame 2054至2065是连续样本，Target Offset约为`0.287979m`，旧距离随高踏面高度保持约`0.289m`，而最终鞋底已经贴面。

这组数据排除了“锁脚阈值太小”和“FBBIK没有执行Goal”：contact输入在Lyra spring之前由动画鞋底计算，把高踏面相对动画脚的高度当成未接触距离。它等价于把锁脚条件绑定在角色附近的旧动画高度，而不是当前IK鞋底是否已经接触唯一支撑面。于是高台阶脚无法进入Plant Contact和anchor，后续Goal继续混入未锁定的动画脚变化；此前为消除穿模而对Swing硬写spring Value时，又把离散踏面高度直接变成单帧吸附。问题反复的共同根因是处理顺序错误，不是两个独立参数问题。

### 修复后的唯一处理顺序

1. 唯一Current SphereCast选择合法支撑面并形成Lyra Target Offset与Hit Normal。
2. 唯一Foot/Pelvis SpringInterpV2生成当前候选Ankle和Rotation。
3. Stance用该候选重建Calibration Heel/Toe，取两点到同一支撑面的最大绝对平面距离作为contact的`surface distance`。
4. 只有候选鞋底真实靠近支撑面且Plant Confidence、sole speed满足原滞回条件时进入Plant Contact并捕获anchor。
5. 第26节当时只允许Plant Contact的候选鞋底残余穿透写回同一Foot Offset spring Value；第27节由`410f`反证该门禁过宽后，补充为同surface连续跨面也可只消除本帧越界，新surface首次命中的Swing仍不硬吸附。
6. Anchor、Pelvis Reach与唯一FinalIK FBBIK消费上述同一Baseline Goal。

输入是动画Pose、Foot Feature和唯一Current Surface；处理是Lyra连续状态后接现有Stance contact/anchor；输出仍是一个Pelvis Goal和左右Foot Goal。没有增加查询、配置、状态、Pelvis owner或solver。

### 业务取舍

- 修复前：高台阶脚即使已经被IK抬到踏面，contact仍看动画脚，无法锁定；为了防穿而让Swing立即安全，会重新引入跳变。
- 修复后：锁脚依据是当前响应式IK候选鞋底是否真的接触当前面。高台阶可以正常进入Plant Contact；尚未靠近踏面的Swing不会提前锁定，因此保留Lyra spring连续性。
- 明确边界：纯响应式Current Query不能在看到未来踏面之前提前规划抬脚。当前目标是把已查询到的响应式接地做正确，Predictive Modifier继续保持未接线，不能用于掩盖Current或contact错误。

### 发布状态

用户明确授权Character Build后，正式Foot Placement菜单重建了2个geometry validation资产并消除了19个Foot Analysis binding的过期identity。Document随后把输入edge identity正式迁为`corin.pose.local-to-component-foot-grounding`，apply返回`applied=true`、`saved=true`、`syncState=Clean`，source revision为`3b1e74baca51290ab2901ff42fb309880a57865770159a0785af336dd338520f`。Float32与Fixed产品已发布；最终checkout为Clean，`btsmtl.validate` compile/semantic成功。Source、Document、Generated Projection均不存在Predictive Modifier。GameplayLab Live Snapshot中两个Fixed Actor均报告`ModifierNotCompiled`、左右脚Anchored、Sole Residual与FBBIK Residual为零，持续运行Console为0 error。Editor保持GameplayLab Play Mode供直接测试。

## 第27节：慢放楼梯运动穿模重新打开验收

### 输入与慢放有效性

本轮只读分析：

`3cDemo/Client/3C_Client/Assets/Scenes/Standalone/foot-ik-410fd03f556044948c4e1665e9aca95f.csv`

文件SHA-256为`E8195763A0FC57A986F1F58CAA8C6E8D599D587809DAC901DAADE6F11DFABF6D`，共240个数据帧、311列，覆盖Frame 706至945。`presentation_position`、`frame_sequence`和`trace_sequence`都逐行严格加一，没有掉帧、重复帧或中途Reset；240帧均为`has_modifier=false`、`placement_alpha=1`、FBBIK backend一致且solver failure为`None`。

慢放样本可以用于以下结论：

- 同一帧中Current Query、spring target/current、contact、anchor、鞋底平面距离、Baseline Goal和FBBIK Result之间的所有权归因；
- 以米为单位的穿透深度、surface identity、支撑面高度和FBBIK空间残差；
- 连续帧内是否发生surface A-B-A、anchor交权或Goal往返。

当前CSV没有导出`PresentationDeltaSeconds`、Gameplay presentation clock mode和rate multiplier，因此不能用它精确比较慢放与正常速度下“经过多少真实毫秒收敛”。Runtime的Live Presentation使用scaled `Time.deltaTime`，Rate Playback在Simulation Locked模式下使用scaled delta乘rate；慢放不会把同帧的空间穿透或owner关系伪造出来，但下一轮若要裁决spring时间常数，必须把这三个时间字段加入CSV。

### 定量证据

把`Residual Sole Penetration > 0.005m`定义为本轮显著穿透：

| 证据 | 左脚 | 右脚 | 裁决 |
| --- | ---: | ---: | --- |
| 显著穿透脚帧 | 73 | 61 | 共134帧，不是单个偶发帧 |
| 释放中旧anchor仍拥有支撑面 | 23 | 23 | 共46帧，证明交权/诊断语义不一致；旧踏面无限平面残差不等于每帧都有可见碰撞 |
| 同一Current Surface上的Swing spring滞后 | 50 | 38 | 共88帧，目标已知但Current未到 |
| 显著穿透帧的Sole Constraint Offset | 全部0 | 全部0 | 第25节Plant Contact gate关闭了所有运动期安全修正 |
| 显著穿透帧的FBBIK Position Residual | 全部0 | 全部0 | FBBIK准确执行了已经下陷的Goal |
| Surface A-B-A | 0 | 0 | 本样本不支持查询面抖动根因 |

左脚同surface Swing穿透中位数为`0.048624m`、P95为`0.121366m`、最大`0.124862m`；对应`Offset Target - Unconstrained Offset`中位数为`0.108822m`。右脚穿透中位数为`0.068304m`、P95为`0.129382m`、最大`0.135886m`；对应target lag中位数为`0.074414m`。这些帧全部已有合法Current Hit、当前支撑面和Sole Clearance Target输入，不属于“查询尚未看见台阶”。

排除旧anchor退混合尾帧后，主要同surface事件不是在surface切换当帧突然出现：左脚Frame 822、836、867的踏面分别已经稳定10、12、10帧，右脚Frame 828、916、930分别稳定10、14、8帧。上一帧穿透为`0m`或至多`0.003733m`，事件首帧只有`0.006327m`至`0.022438m`，随后才在Plant Contact gate持续关闭时扩大到`0.039651m`至`0.135886m`。因此大下陷可以在同一踏面第一次连续越界时用小修正阻止，不需要等到大缺口后再整段吸附。

释放中旧anchor的平面残差更大：左脚平均`0.157293m`、最大`0.318329m`；右脚平均`0.128810m`、最大`0.300422m`。左Frame 809的Current Surface为`-98098`、高度`1.68m`，但Sole Support仍是旧surface `-98108`、高度`1.92m`；此时`PlantContact=false`、`HasAnchor=true`、`AnchorBlend=0.031151`、Transition连续报告`AnchorDistanceExceeded`、Constraint为0、FBBIK残差为0。右Frame 816是同类状态，AnchorBlend为`0.052799`。由于该残差针对旧collider踏面的无限延伸平面，而CSV没有证明Heel/Toe投影仍位于旧踏面几何范围内，所以它足以证明owner和诊断不一致，但不能把46帧全部直接计为肉眼可见穿模。

### 源码归属

当前Stance顺序产生了一个不一致组合：

1. `UpdateContact`因surface distance、动画鞋底速度或Plant Confidence释放`PlantContact`；
2. `AnchorDistanceExceeded`只调用`Release`，没有清除旧anchor；
3. `AnchorBlendWeight`继续向零退混合，所以`HasAnchor=true`仍让`ContactState`显示Anchored；
4. `StabilizeFoot`继续把旧anchor作为pose blend和Sole Support；
5. 硬鞋底约束只检查`PlantContact`，因此释放中的旧anchor和所有Swing都得到零约束；
6. 唯一FBBIK以零残差执行该Baseline Goal。

第1至5步属于项目新增Stance逻辑，不存在于Lyra `CR_Mannequin_FootPlant`。46个旧anchor脚帧是明确的实现/诊断语义缺陷，不是Lyra算法上限；它们与用户可见穿模的重合程度仍需在修复support authority后重新采样，不能用当前无限平面残差夸大。

另外88个同surface Swing帧来自第25节的明确取舍：`Sole Clearance Target`已经进入Lyra Foot Offset spring，但为了撤销第24节的单帧吸附，Swing不再把残余穿透写回spring Value。这个取舍避免离散高踏面瞬移，却允许Current Value在目标后方追赶。它不是FBBIK、Pelvis或Query错误；它也不能简单归为“算法看不到未来”，因为这些帧中Current Query已经连续看见同一踏面。

### 与Lyra可见效果的差异

本地Lyra资产仍确认：每脚一次垂直Sphere Trace、Impact Point形成Component/VM空间绝对Target Foot Offset、Foot与Pelvis使用`2.5/1/0.2` SpringInterpV2、Hit Normal使用`8/1` spring；没有项目式contact、anchor、鞋底硬约束或锁脚状态机。

因此“Current Grounding数学逐项来自Lyra”不等于“端到端已经和Lyra一样”。Lyra最终效果还同时依赖Mannequin输入动画的IK Foot摆动高度、Control Rig骨骼空间、鞋底几何以及最后的Basic IK/Two Bone腿链；项目输入是Corin动画与Calibration鞋底，输出是唯一FBBIK，并额外串有Stance anchor和鞋底安全扩展。第27节已经证明其中至少一个额外Stance交权实现错误，不能用Lyra本身也有spring来为当前结果免责。

### 实现问题与算法上限裁决

- 当前主要问题是实现与集成，不是单纯算法上限。134个显著平面残差脚帧全部发生在合法Hit之后，FBBIK残差为零；其中46帧包含旧anchor无限平面测量，88帧是同一Current Surface上项目为避免跳变而关闭Swing安全约束后的真实候选下陷。
- 纯Current Query的真实上限只发生在高踏面第一次进入单次脚踝垂直查询之前，或新踏面第一次出现时已经高于当前连续脚轨迹。此时不使用未来信息就不能同时保证“同帧零穿透”和“Goal不跳”。
- `410f`不能证明这134帧都属于该上限。相反，主要同surface事件在surface稳定8至14帧后从小越界开始，再持续扩大，说明目标出现后仍有项目可以改进的处理阶段。
- Lyra可见效果好，说明“响应式方案可以在合适动画、骨骼空间和最终腿链下工作得很好”；它不证明当前Corin输入、Stance扩展和FBBIK集成已经等价。

### 下一步修复边界

第一步把“锁脚”和“非穿透”重新分责，但仍留在同一Stance owner：Anchor捕获继续要求Plant Contact；鞋底单向约束不能再由Plant Contact这一把总闸门控制，也不能恢复第24节对任意Swing大缺口的整段瞬移。正式实现必须用同一surface identity、上一帧签名鞋底距离和既有Foot Offset状态定义连续接触边界，使候选第一次从面上越到面下时只消除本帧小越界；`410f`证明主要事件具备这个连续入口。新surface第一帧若已经形成大缺口则继续由spring处理，不能伪装成无跳变硬修复。

第二步修现有Stance owner的anchor交权：`AnchorDistanceExceeded`只能触发一次释放，旧anchor在退混合期只作为pose blend来源，不能继续冒充当前鞋底支撑权威；`PlantContact=false`后鞋底诊断与安全面必须回到唯一Current Surface。不得增加第二Grounding、第二查询、第二Pelvis、第二solver或fallback。

第三步补齐`PresentationDeltaSeconds`、动画Ankle Component Y、PoseRoot竖直delta、上一帧surface identity、上一帧Heel/Toe对当前平面距离和连续跨面判定，再用正常速度与慢放各一份CSV裁决剩余滞后来自时间步、Actor/root竖直移动还是Corin动画摆脚轨迹。presentation clock/rate属于Session调试控制，不属于FootGrounding输入；若后续必须横比调试时钟，应由Session capture直接发布，不能在Foot owner反推。若Lyra spring输入输出逐帧等价而大缺口只在Corin动画出现，则剩余项属于动画/Calibration/FBBIK集成，不应继续在Grounding中堆状态。

### 第27节实施结果

Stance现已在每脚原`FootState`保存上一帧约束后Heel/Toe世界位置和唯一支撑surface identity。当前spring候选形成后，只有满足以下全部条件才把向上缺口写回原Foot Offset Value：上一帧样本有效、surface identity相同、上一帧两点对当前平面均不低于`-0.0001m`、本帧候选存在正穿透。Plant Contact原有持续约束保持不变。该判断的输入是唯一Current Surface与同一spring候选，输出仍是同一个Current Offset；没有新增查询、配置、spring、Goal或solver。

这与第24节无条件Swing约束的区别是：新高踏面首次出现时，previous surface不同，或者上一帧鞋底对该高平面本来就在下方，所以不会触发整段上吸；同一踏面已知后，鞋底从面上连续进入面下时，第一次小越界被消除并作为下一帧约束后历史保存，因此不会继续积累为`410f`里的`0.124862m/0.135886m`深下陷。按`410f`事件入口做反事实判断，左Frame 822/836/867和右Frame 828/916/930会在首个`0.006327m`至`0.022438m`越界帧进入连续约束，而不是等残差继续扩大。该反事实只证明门禁覆盖已录事件，实际视觉结果仍以修复后新采样为准。

Anchor交权同时修正：`AnchorDistanceExceeded`只在Plant Contact仍成立且旧anchor可解析时触发；释放后旧anchor可继续提供原有pose退混合，但鞋底支撑面和Residual Sole Penetration立即使用Current Surface。这样`410f`中连续重复释放原因和“当前脚已下台阶、诊断仍拿旧高踏面无限平面当支撑”的不一致不会继续存在。

新增CSV字段包括`presentation_delta_seconds`、`pose_root_vertical_delta`、每脚`animated_ankle_component_y`、`has_previous_sole_sample`、`previous_sole_surface`、`previous_sole_heel_plane_distance`、`previous_sole_toe_plane_distance`和`continuous_sole_contact`。下一份慢放采样可以直接对账每帧时间步、动画输入、root移动、连续边界和约束输出，不再只靠Frame序号猜测。

实施后单一路径搜索确认：Current Grounding仍由`CharacterLyraCurrentGroundingSolver`每脚构造一次Sphere请求；World Query中的Capsule分支只由未接线Predictive查询消费；Corin生成Projection保持`m_PredictiveFootPlacementModifiers: []`且所有Modifier索引为`-1`；FootPlacement目录不存在FinalIK Grounding、LegIK或TwoBoneIK。`ThirdPersonClient.Runtime.csproj`与`ThirdPersonClient.Editor.csproj`本地编译均为0 error并已关闭.NET build server，OpenSpec strict validate通过。已打开Unity Editor完成停止Play、强制AssetDatabase刷新、编译与GameplayLab重新启动；第二次全量刷新后SourceAssetDB错误消失，GameplayLab短时运行Console为0 error。Domain reload期间MCP插件自身记录过WebSocket重连异常，连接自动恢复；该插件异常不计为项目C#、AssetDatabase或Gameplay运行错误。未运行batchmode或Character Build。

业务取舍保持明确：纯响应式链可以修正已看见支撑后的连续接触和项目额外owner错误，但在离散高踏面首次出现时，没有未来信息就必须在短时穿透、Goal跳变或上游动画自然清障三者中选择。当前业务已禁止预测，所以目标应是Lyra式连续响应加正确动画/骨骼集成，而不是再次恢复Swing硬吸附。
