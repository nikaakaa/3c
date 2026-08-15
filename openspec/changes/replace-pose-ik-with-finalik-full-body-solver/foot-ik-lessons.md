# Foot IK关键经验

本文只保留会改变正式架构或否决方案的结论，不记录逐轮修补流水账。

## 1. 当前不是完整GDC数据层

现有实现已经具备单次FBBIK、当前支撑、Foot/Ankle/Hip位置路线、Clearance、Ground Envelope和诊断框架，但Artifact v26缺少：

- Sole/Ankle旋转路线；
- 支撑腿长度、压缩余量、膝盖弯曲平面和支撑权重；
- 数据定义的连续Constraint Weight；
- 上坡、下坡、跑步Foot Orientation策略；
- 真正参与求解的Support Foot Body Pivot；
- 可阻断Build的平地原动画重建误差合同。

因此当前能力只能称为“预测脚部清障基础”。在这些数据缺失时继续调runtime，不可能稳定复制GDC整体效果。

## 2. GDC的核心不是提前射线

本地原始幻灯片给出的完整关系是：

```text
Predictive Character Motion through hips
+ Animation Foot Forward Motion
+ Animation Height Above Foot Path
+ Data-defined Foot Constraint
+ Support Leg Hip Stabilization
+ Foot Orientation
+ Rotation near Contact Foot
+ Virtual Ground / Reachability / Feet-only Ground Envelope
```

最终鞋底高度是：

```text
Ground Path Height + Animation Height Above Foot Path
```

不是`max(AnimationWorldY, GroundY)`，也不是把Convex Hull直接当最终脚轨迹。

参考：`tmp/pdfs/Roche_Clifford_Fitting-the-World_GDC2016.pdf`。

## 3. 四条路线必须分开

- `Animation Foot Route`：in-place动画中脚相对角色的局部位置与旋转；
- `Future Body Transform Trajectory`：Simulation/KCC提交的角色世界Position、Facing、线速度和角速度；
- `Ground Query Route / Virtual Ground`：由未来Foot Route和对侧接触形成的查询拓扑；
- `Ground Envelope`：合法地形点形成的连续feet-only上侧包络。

它们不能互相冒充：

- Animation Route不是角色位移；
- Query Route不是最终脚轨迹；
- Ground Envelope不是Pelvis Path；
- Body Facing不是位移曲率；
- Current Grounding不是Rejected Predictive Plan的fallback。

## 4. In-place动画不提供世界位移

Corin世界移动由Simulation/KCC拥有。Artifact只保存骨骼相对Visual Root的动画事实。

已经被数据否决的做法：

- 用输入幅值缩放动画路线；
- 用Action Motion Curve或Plant轨迹重建Root位移；
- 丢弃动画局部X只保留Z；
- 用Visible Velocity、Body Yaw或相邻Render Frame导数猜未来轨迹；
- 固定零曲率后仍声称支持A/D圆周移动。

这些做法会产生第二移动距离、路线约两倍、侧向漂移或计划与角色脱节。

## 5. 必须先证明Artifact能还原原动画

在楼梯上调Path之前，先用同一Action Phase比较：

```text
Original AnimationClip Pose
vs
Artifact Reconstructed Heel/Toe/Sole/Ankle/Knee/Hip Pose
```

必须同时看位置、旋转、路线弧长、侧向范围、Landing端点和事件相位。若平地重建不一致，楼梯上的Ground Query、Foot Rate、Landing和Pelvis都没有可信输入。

当前“平地能踩到，但Path不像原动画”就是这个门禁缺失的直接表现。

## 6. 冻结计划与意图变化并不矛盾

预测必须在一个动作事务内确定，但真实输入可能改变。正确做法不是逐帧自适应，也不是永远锁死旧计划：

```text
Committed trajectory A
  -> immutable Plan A

Simulation提交实质不同的trajectory B
  -> immutable Successor Plan B
  -> 从当前已执行位置、线速度、角速度连续交接
```

每个Plan保持不可变；只有committed世界落点或朝向误差超过鞋底几何边界才创建Revision。摄像机缓动本身不是证据，必须先进入Simulation并改变正式未来身体轨迹。

最新压力采样证明，当前Revision仍有三类错误：

- 大量Executing帧的剩余落点误差已超过阈值但没有有效新计划；
- 事件换代会在后继尚未Executable时删除旧输出；
- generic `NonFinite`把任意未重写Executing帧归为数值错误，掩盖真实owner切换。

## 7. 连续曲线不能保证跨owner连续

一条Path内部连续，只能证明同一Plan的几何连续。以下边界仍可产生跳变：

- Active Plan变成Revision；
- 当前Landing Event变成下一Event；
- Predictive输出变成Grounding或原动画；
- Swing变成Landing Anchor；
- Foot Goal被Reach拒绝；
- Pelvis owner换腿；
- Body Pivot或Foot Orientation策略切换。

`foot-ik-2979d902bbc64705b95da4c9dbae2340.csv`中，左脚Executing输出曾在相邻帧出现约`-1.23m / +1.21m / -1.03m` Goal Y往返，右脚也有约`-0.52m`变化；FBBIK多数帧准确跟随Goal。问题发生在Plan/Goal owner，不是曲线绘制或solver平滑。

## 8. Ground Envelope的顺序不能颠倒

GDC顺序是：

1. Capsule检测Foot Path，取得位置与法线；
2. 按前后与高低排序；
3. 验证法线并建立Edge Plane；
4. 检查边的垂直高差和可达性；
5. 删除不可通行点；
6. 对剩余点构造连续Convex Hull；
7. Ground Envelope只服务feet。

先把不可达点放进Hull，再在最终Goal处Reach clamp，会先承诺错误路线，再让脚在最后一刻跳变或悬空。

台阶边缘必须在鞋底配置空间中处理：墙面`hit.Point`不是鞋底中心开始碰撞的位置。Edge Plane必须考虑鞋底Heel/Toe范围与Capsule半径，但不能用固定高度补偿。

## 9. Foot Lock是数据意图加世界验证

GDC的Locked、Sliding、Unlocked由动画数据定义；世界查询只验证它能否成立。

- Locked：完整世界位置锁定；
- Sliding：保持同一支撑面垂直接触，允许有限面内移动；
- Unlocked：完整释放世界锁，不让旧Anchor继续拉Swing脚。

Landing必须同时提交Plan Landing Pose、Surface identity、Anchor局部点/法线、Committed Sole Pose和后继Step起点。不能用预测Ankle配Current Query的另一踏面，也不能用已经贴地的预测Goal自证接触。

静止归位与Idle锁脚仍属于同一个Stance owner：旧运动Anchor先退到同帧原动画Sole经Current Support约束后的安全Baseline，再捕获Idle Anchor。不能捕获仍在收敛的spring current，也不能永久禁用Idle Anchor。

## 10. Pelvis必须拥有身体坡线而不是脚部包络

GDC明确要求：预测角色运动主要通过hips发生；支撑腿决定Hip高度；上坡与下坡不同；直接使用位移，spring只增加pull并消除bounce。

因此：

- Future Body Trajectory的KCC Y不是Body Support Path；
- Foot Ground Envelope不是Body Support Path；
- 左右脚各自Path不能平均成第三个Pelvis；
- 权威Root竖直位移不能再从Pelvis spring中反向扣除；
- spring current不能成为预测Pelvis目标本身。

正式身体输入应是`last support -> opposing support -> predicted landing`，再叠加动画Hip相对Body Path的运动，并通过一个支撑腿和一个Pelvis owner做reach与临界spring。

## 11. Foot Orientation与Body Pivot不是装饰字段

GDC要求：

- 上坡脚掌趋于水平；
- 下坡脚掌趋于贴坡；
- 跑步不使用该坡面orientation；
- 身体旋转支点靠近接触脚，Locked脚保持锁定且允许有限旋转。

当前binary orientation和由LiftOff推导的pivot枚举不足。若它们只进入diagnostics，转向时腿仍会被身体中心旋转拉扭，坡面脚掌也会把错误力传给hips。

## 12. 被否决的补丁方向

- 每帧重规划Landing或重投影当前脚；
- 用下一动作的旧世界计划直接晋升；
- 完整世界Route接管Foot XYZ；
- `max(CurrentAnimatedY, PredictedY)`双高度owner；
- 全局抛物线或样条替代动画脚轮廓；
- Spring、Blend、Reach或Pelvis平滑错误Path；
- 用响应式Current Goal兜底Rejected计划；
- 用FBBIK后处理追回不连续Goal；
- 增加第二Grounding、第二Pelvis或第二IK solver。

这些方案最多改变症状，不改变错误数据和所有权。

## 13. 固定验收顺序

```text
Artifact重建原动画
-> Projection事件与时钟连续
-> 平地Future Foot Route匹配Native Pose
-> A/D Future Body Transform与Revision连续
-> Ground Query与Reachability正确
-> Ground Envelope与Animation Clearance正确
-> Landing与Anchor原子交接
-> Support Leg / Pelvis / Orientation / Pivot正确
-> FBBIK residual正确
```

前一层未通过时，不修改后一层参数。编译、Character Build、Console 0 Error和CSV列数正确都不能代替运动效果闭环。
