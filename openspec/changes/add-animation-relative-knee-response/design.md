# 动画相对膝角响应设计

## 单变量与调用链

同帧Component输入Pose → 记录左右动画膝角 → 原Goal与FBBIK → 动画相对膝角响应 → 原Component输出与唯一Physical Writer。

仅新尾段改变姿态。Foot目标、Pelvis响应、Bend方向／权重、Vendor求解步骤和Clip不变。角差响应可能改变Foot实际位置，这是测量对象，不能靠重解IK、恢复脚位置、清Contact残差或降Goal权重遮盖。

## ZZZ普通数学

膝角为大腿向量K−H与小腿向量A−K的夹角，直腿为0，单位弧度。

```
desiredExtra = solvedAngle - animationAngle
rate = lerp(upRate, downRate, clamp01(downStairWeight))
currentExtra = MoveTowards(previousExtra, desiredExtra, rate * deltaSeconds)
compensation = currentExtra - desiredExtra
thigh.localRotation *= AngleAxis(-0.5 * compensation, thighLocalBendAxis)
calf.localRotation *= AngleAxis(compensation, calfLocalBendAxis)
foot.worldRotation = savedFootWorldRotation
```

历史保存currentExtra，不保存世界Knee位置。角差历史从零开始；不将初始化伪装为已经到达desired。零时间不推进角差或移动速度历史。

本步正式策略只声明Disabled与Forced；Forced对应已恢复的Force分支及override速率，不发布虚构kneeState。Corin配置Forced、7／4rad/s；启停区段属于后续有正式输入的独立工作。

## 坐标与移动输入

原ZZZ局部Z是其Rig坐标。项目使用同一Rig的引用H/K/A和引用旋转，得到同一弯曲法线在大腿、小腿各自局部坐标的轴，构造时严格验证非退化，运行时不按历史翻号或重选轴。这是静态坐标适配，不是之前的目标腿轴方向运输算法。

输入动画膝角来自FBBIK前的同一Component Pose。当前膝角来自FBBIK后H/K/A经同一PoseRoot矩阵转换的世界点；保存和恢复Component脚旋转在该冻结Root旋转下等价于保留世界脚旋转。非均匀缩放不因此获得整套骨骼正确性的承诺。

移动输入使用当前PoseRoot位置与旋转及正式PresentationDeltaSeconds。已有历史时v=(p−previousP)/dt；首次输入只建立位置基准，速度历史从0开始。前向速度取dot(v, rootRotation*Forward)，下降速度取dot(v, WorldDown)，各按0.25追踪，downStairWeight=clamp01(3*downSpeed/forwardSpeed)。精确forwardSpeed=0时，downSpeed>0为1，否则0；不以默认PoseRoot／默认Body补缺失绑定。

## 所有权

独立模块拥有角差计算与骨骼缓冲区补偿；根Runtime只传时间／Root输入和管理固定大小历史。历史属于同一Pending Bank，开始从Committed复制，成功后随根Bank切换，Discard不发布，既有Solver Reset同时清除。不存在第二Commit或Vendor私有持久角差。

Profile内容参与正式revision与Projection依赖。Disabled是作者策略，不是错误恢复；非法参数、绑定或非有限结果按现有Fault合同拒绝。

## 诊断

保留FBBIK原始Solved H/K/A及既有权重测量；新增响应是否执行、动画／输入／输出膝角、历史前后、目标角差、限速步长、补偿角、下楼权重与移动速度，以及响应后Knee／Ankle和脚旋转恢复误差。

Analyzer检查弧度角差与MoveTowards、历史连续、旋转恢复和阶段身份。实际Final Heel／Toe及现有质量规则继续使用真实Writer结果。不能把响应后位置覆盖到同名FBBIK测量，也不能让诊断interest改变算法是否执行。
