# Change: 移植动画相对膝角差响应

## 当前裁决

2026-08-31：183002 Replay出现新的整脚离面、穿透和锁脚漂移，用户已否决并要求撤销。本change保留为失败实验记录，下面的方案及delta不安装为现行合同；Runtime、Corin Profile和Diagnostics恢复160901保留链路。正式产品重建及恢复Replay结果见experiment.md。

## Why

用户要求以160901保留版进入反弯修复，并尽量采用已经恢复的ZZZ算法。新元数据和磁盘PE确认BoneAdjust.SmoothKnee入口为0x165C51A0：记录本帧动画膝角，追踪求解姿态相对动画的额外膝角，再右乘大腿／小腿局部旋转补偿并恢复脚旋转。它不是此前的有符号膝向运输候选，也不保证脚位置不变。

## What Changes

- 在现有FullBodyIK阶段内增加由正式Profile声明的动画相对膝角响应尾段；不增加MonoBehaviour、第二Solver、Goal或Physical Writer。
- 保留160901的Foot、Pelvis、Reach撤除决定、Bend方向及权重算法。
- 沿用ZZZ的弧度角差、有限步长、−0.5／+1旋转系数、右乘顺序和只恢复脚旋转的语义。
- 首轮明确采用ZZZ Force路径，普通kneeState启停不在本步；正式Profile配置强制路径的上／下楼速率7／4。不得把Contact、Lock或Approach映射成kneeState。
- 下楼权重按同一PoseRoot的位置差、朝向、0.25输入速度追踪和3倍下降／前向速度比计算。精确静止的0／0定义为无下降权重，零时间不推进历史。
- Rig引用姿态提供每腿静态弯曲轴的坐标适配；动画膝角使用同帧输入Component Pose，求解膝角使用同帧PoseRoot变换后的世界几何。
- 新角差与移动输入历史随同一根Bank提交／丢弃。正式诊断分开保留FBBIK输出、角差响应及响应后骨骼位置，不修改现有质量评分。

## Impact

涉及FullBodyIK Profile、唯一Solver适配器、根Bank与阶段时间输入，以及现有Diagnostics采样／分析／发布。仅更新Corin正式资产和产品，不处理TrainingEnemy。

## 现行合同对照

- 保持character-animation-pipeline与character-presentation-pose-graph的一次FBBIK、一次Writer和根Bank事务；新增尾段由已有FullBodyIK节点及其唯一Profile显式声明，不在图外添加常驻Pose低通。
- 现行BendHistory只描述方向稳定；本步增加独立的角差历史，不替换或混称同一状态。
- 活跃stabilize-character-foot-path-and-landing的8.6／8.7方向候选仍未实施，本步不勾选它们。
- openspec/project.md仍有用户正在修改的旧Reach／响应描述，本步不覆盖该文件；160901已接受的Reach撤除以当前代码、refine-character-pelvis-response与用户决定为准，不能借移植恢复硬夹紧。
- 旧研究中“SmoothKnee仅方法名、消费点未知”不再适用于新BoneAdjust证据；ZZZ仍未提供该算法单独消除反弯或保脚位置的动态证明。

## 验证边界

以20260831-160901-709-3e0df68f9d3640aaa82f4fbd2ec7c42f为基线，使用43357ff3cd384e5cba75d2c31175b116正式输入自动Replay。分别比较膝角变化、实际响应后膝侧／位移、Foot间隙／穿透／漂移和Pelvis，不以角差公式成立或总分上升宣称全部反弯修复。原样保留失败数据，普通结论只写一份实验Markdown。
