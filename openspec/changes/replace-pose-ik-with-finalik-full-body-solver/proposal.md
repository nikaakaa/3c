# Change: 以动画生物力学数据重建GDC风格Foot Placement

## Why

当前工作区已经完成单次FinalIK FBBIK、当前支撑查询、鞋底几何、Stance/Anchor、基础预测Path和完整诊断框架，但仍未形成GDC 2016《Fitting the World: A Biomechanical Approach to Foot IK》所要求的完整数据链。

本轮对代码与最新压力采样的对账证明，问题不是楼梯阈值不足：

- `AnimationFootAnalysisArtifact`仍为format v26，主要发布Foot、Ankle和Hip位置路线；没有同相位Sole/Ankle旋转路线、支撑腿压缩事实、连续约束权重和身体支点事实；
- `Foot Orientation`只有`PreserveAnimation / LandingSurface`二选一，不能表达上坡脚掌趋于水平、下坡脚掌贴坡、跑步关闭坡面旋转；
- `Body Rotation Pivot`主要作为由LiftOff推导的枚举和诊断存在，没有实际围绕接触脚组织身体旋转；
- 当前Pelvis planner最终仍转发已有spring current，没有建立“上一次支撑到预测Landing”的身体坡线、支撑腿选择和预测髋部路径；
- Future Body trajectory的转向曲率仍固定为`0`，A/D圆周运动没有进入同一步的正式预测；
- 最新`foot-ik-2979d902bbc64705b95da4c9dbae2340.csv`中，Executing计划会在输入仍存在时失去Predictive输出并切回Grounding，左右脚出现约`1.23m`和`0.52m`单帧Goal Y变化；大量计划的剩余落点误差超过正式几何阈值，事件换代又会清除未完成Revision；FBBIK多数帧只是准确执行了不连续Goal。

因此，现实现只能称为“带未来地面查询的脚部清障基础”，不能称为完整GDC风格预测身体系统。继续在当前runtime上调Cast、Reach、Blend或Spring，只会把错误数据延迟或放大。

同时，当前Foot Placement运行结构本身也没有兑现“一个owner”：

- `CharacterFootGroundingPlanner.Plan`先调用Predictive `Prepare`和`GetStanceInput`，再由Grounding推进Stance，随后把Anchor通过`ObserveStance`回传Predictive，最后由Predictive `Resolve`改写Grounding已经生成的baseline Goals；
- 每只脚的可变所有权分散在Grounding `FootState`、Predictive `FootPlanRuntime`、可变`CharacterPredictiveFootPlacementPlan`和Current Grounding spring中；
- Landing、Anchor、Plan换代、输出连续性和Pelvis拒绝发生在不同对象与不同调用阶段，任一阶段都可能观察到半完成状态；
- `CharacterFootPlacementRuntime -> CharacterFootGroundingGoalSource -> CharacterFootGroundingPlanner`存在无业务决策的转发层，但真正的调用顺序依赖没有进入显式合同。

这使得代码即使拥有正确的Artifact和Ground Path，也仍可能在owner交接处产生鬼畜。因此，本change必须先把运行时收敛为一笔单向Foot Placement帧事务，再继续实现后续GDC身体层。

## What Changes

本change改为按依赖顺序建立唯一前向链：

```text
In-place AnimationClip + Rig/Calibration
  -> Editor-only Biomechanical Step Artifact
  -> authoritative Action Step Fact and Clock
  -> committed Future Body Transform Trajectory
  -> unified FootPlacement frame transaction
       -> Current Support facts
       -> optional Predictive Plan facts
       -> per-foot Execution State
       -> Stance / Anchor / Landing / Support Leg
       -> Pelvis / Body Pivot arbitration
  -> one Final Goal Set
  -> one FinalIK FBBIK solve
```

正式对外链仍保持：

```text
Original Component Pose + Step Facts + Body Trajectory + World Context
  -> CharacterFootPlacementRuntime.EvaluateFrame
  -> one FootPlacement Final Goal Set
  -> one FinalIK FBBIK
```

`Current Support`与`Predictive`只是统一Foot Placement内部的事实生产模块，不再形成相互回调的两个状态owner。Current Support不先生成一套响应式Swing Goal供Predictive覆盖；Predictive只创建或求值不可变Plan。没有Executable计划时，Swing保持原动画并明确报告Unavailable或Rejected，不把响应式结果伪装成预测成功。

### 0. 先收口Foot Placement运行事务

`CharacterFootPlacementRuntime`成为Pose Plan唯一可见的深模块。每帧只接收一个不可变`CharacterFootPlacementFrameInput`，并只返回一个`CharacterFootPlacementFrameResult`。内部固定顺序为：

1. 捕获Original Component Pose与同源Step事实；
2. 取得左右脚Current Support查询事实；
3. 创建或求值不可变Predictive Plan；
4. 从上一完成帧的唯一`CharacterFootExecutionState`生成左右脚约束提案；
5. 由唯一Pelvis owner同时仲裁双脚可达性；
6. 原子提交Landing与Anchor，形成左右脚最终结果；
7. 一次写入Pelvis、Left Foot、Right Foot三个最终Goals；
8. 发布与上述结果同completion的诊断快照。

每只脚只有一个可变`CharacterFootExecutionState`，统一拥有Constraint Phase、Current Support filter、Anchor、Active Plan引用、唯一Transition槽、上一完成输出、Landing Commit和Query Attempt identity。Predictive Plan提交后只保存不可变事件、时钟映射、路线、Query、Ground Envelope、Landing和Body Path事实；Plan不再拥有Active/Revision/Fade、Anchor观察、Action Clock推进或输出连续性。

Foot Placement读取Committed状态并生成Pending左右脚状态。只有本帧Goal Set与后续Pose Plan完成时，Pending状态才随表现帧Seal成为下一帧Committed状态；失败或重置不得留下只推进了一只脚、只提交了Landing一半或已经换Plan但未发布Goal的状态。

### 1. 先升级动画生物力学Artifact

现有Artifact原地升级，不建立第二资产或兼容reader。每个Landing Event必须原子保存：

- Heel、Toe、Sole、Ankle、Knee、Hip的root-local位置路线；
- Sole与Ankle的root-local旋转路线和动画脚掌朝向基准；
- 动画脚相对参考Foot Path的Clearance；
- Release、LiftOff、ApproachContact、Landing及Locked/Sliding/Unlocked约束事实；
- 支撑腿长度、压缩余量、膝盖弯曲平面和支撑权重；
- 身体围绕支撑脚旋转所需的pivot位置与phase/weight；
- 对侧Landing身份、时间和root-local接触姿态；
- source、cycle、event、clock、artifact和projection identity。

Corin是in-place动画。Artifact不得保存或推导角色世界位移、运行速度、Action Motion Curve或KCC路径。

### 2. 在接触地形前证明Artifact能重建原动画

Editor必须使用同一采样时钟，从Artifact重建Foot、Sole、Ankle、Knee、Hip位置与旋转，并与原AnimationClip逐相位对账。误差超过schema固定容差时，Build必须失败。Start、Loop、Stop、MovingTurn的左右脚事件、cycle和phase也必须连续。

这一步没有通过前，不允许继续用楼梯观感判断Ground Path、Landing、Pelvis或FBBIK。

### 3. 统一Action Step事实与Projection所有权

Pose可以连续混合，但每只脚的Landing Event、Clock、路线、约束、支撑腿和pivot事实必须从一个权威source原子选择。Stored Pose、退出源、Inertial History和逐脚Pose权重不得复活旧事件或拆开混合事件字段。

### 4. 由Simulation发布未来身体Transform轨迹

Foot Placement不得从输入幅值、Visible导数、Body Yaw或动画步幅猜角色位移。Simulation/KCC必须为剩余步时钟发布同源的未来Position、Facing、Linear Velocity和Angular Velocity轨迹。初始Plan与每个Revision冻结该轨迹。

A/D、W/S或camera-relative世界意图发生实质改变时，Simulation提交新的trajectory identity；Predictive只在落点或朝向误差超过鞋底几何边界时创建离散后继Revision。旧、新计划从当前已执行输出的位置、线速度和角速度连续交接；不得逐帧改写同一个Plan，也不得在后继尚未Executable时删除旧输出。

### 5. 按GDC顺序构造Feet-only Ground Envelope

Future Query沿动画预测Foot Path与对侧接触形成的Virtual Ground路线执行Capsule检测，保留位置与法线，按前后和高低排序，验证法线、建立Edge Plane，先删除不可达点，再对剩余点构造连续二维上侧凸包。

Ground Envelope只是脚不能穿过的feet-only下界，不是最终脚轨迹，也不得驱动Pelvis。最终Swing高度固定为：

```text
FinalSoleHeight = GroundEnvelopeHeight + AnimationClearance
```

脚的前向与侧向轮廓来自同相位动画，不能丢弃局部X，也不能用Ground Probe或凸包XYZ直接拉脚。

### 6. 补齐GDC身体层

- 脚锁采用数据定义的Locked、Sliding、Unlocked和连续权重，世界接触只负责验证；
- 身体坡线使用上一次支撑与预测Landing，不复制Foot Ground Envelope；
- 支撑腿根据上坡、下坡和接触身份驱动Hip高度；
- 直接应用身体支撑位移，临界spring只增加可达拉力并消除bounce；
- 上坡、下坡和跑步使用不同Foot Orientation策略；
- 临近接触时围绕锁定支撑脚组织有限body/pelvis rotation；
- 全部结果仍由唯一Stance/Pelvis owner发布一个Goal Set，FinalIK只求解一次。

## Scope

### 保留

- Rig v4与Calibration v4；
- Heel/Toe/Sole几何与唯一World Query backend；
- 当前合法支撑与鞋底安全平面；
- Current Support、Stance/Anchor与Pelvis已有算法基础；
- Predictive Query、Ground Envelope和World Query backend现有几何能力；
- FinalIK Pose Buffer FBBIK；
- Runtime Trace、Inspector、Gizmo、CSV和自动往返框架。

### 原地替换

- Artifact v26及其codec、projection payload和generated产品；
- 位置-only步事件数据；
- Plan私有或表现导数时钟；
- 固定零曲率Future Body路线；
- Active/Revision事件硬取消；
- `Prepare -> GetStanceInput -> ObserveStance -> Resolve`跨模块调用协议；
- Grounding baseline Goals再由Predictive覆盖的双重Goal决策；
- Grounding `FootState`、Predictive `FootPlanRuntime`和可变Plan并列持有脚所有权的结构；
- 无业务决策的`CharacterFootGroundingGoalSource`转发层与`CharacterFootGroundingPlan`中间结果；
- binary Foot Orientation和diagnostics-only Body Pivot；
- 只转发spring current的预测Pelvis语义。

### 禁止

- 第二套Grounding或Heel/Toe Current Query；
- 响应式Swing fallback；
- 第二Pelvis、LegIK、TwoBoneIK或FBBIK后处理；
- 默认地面、固定高度补偿、兼容reader或旧Projection运行；
- Unity batchmode。

## Acceptance

完成必须按以下顺序成立：

1. Pose Plan每帧只调用一次`CharacterFootPlacementRuntime.EvaluateFrame`，不存在Predictive与Grounding的跨阶段回调；
2. 每只脚只有一个可变`CharacterFootExecutionState`，Plan与Query结果提交后保持不可变；
3. Landing、Anchor、Plan换代和上一完成输出在同一Pending状态中原子提交，失败帧不留下部分状态；
4. Pelvis只消费左右脚约束提案并返回一次仲裁结果，Foot Placement Goal workspace只写入一次最终Pelvis、Left Foot、Right Foot；
5. 新Artifact在平地逐相位重建原动画Foot/Sole/Ankle/Knee/Hip位置与旋转，并通过固定误差门禁；
6. Start、Loop、Stop、MovingTurn左右脚Action Step身份、cycle和phase连续；
7. 平地Runtime预测Foot Route与同相位Native Pose匹配，前向距离和侧向轮廓不再出现约两倍或横向漂移；
8. A/D连续转向由committed Future Body Transform轨迹解释，Revision交接的位置、线速度和角速度连续；
9. 楼梯Executable Plan完整显示Virtual Ground、Capsule查询、合法支撑、Reachability、Ground Envelope和实际消费点；
10. 同一Plan内Path、Landing和Goal没有未解释跳变，Rejected不会切换到伪预测结果；
11. 上楼支撑腿与Hip高度连续，下楼Heel/Toe没有未解释下陷或浮空；
12. Locked脚世界Goal稳定，Sliding只在支撑面内移动，Landing与后继Plan共享同一支撑事务；
13. Foot Orientation和Body Pivot对上坡、下坡、跑步与转向使用明确策略；
14. Final Goal连续安全时，FBBIK solver与physical residual保持正式容差；
15. OpenSpec strict validate、单一路径静态搜索、Runtime/Editor编译和精确Float32/Fixed Character Build通过；
16. 编译、Build或Console 0 Error不得代替IK效果与数据验收。

## Reference

- `tmp/pdfs/Roche_Clifford_Fitting-the-World_GDC2016.pdf`
- 关键页：Predictive Character Motion、Foot Motion、Foot Locking、Stabilizing the Hips、Foot Orientation、Rotation near contact foot、Virtual Ground、Ground Detection、Ground Path、Reachability与Ground Envelope。
