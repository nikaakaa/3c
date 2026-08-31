## ADDED Requirements

### Requirement: 动画相对膝角响应必须属于唯一Pose事务

FullBodyIK Profile MAY显式声明Disabled或Forced动画相对膝角响应。启用时，同一FullBodyIK阶段 MUST先记录输入动画膝角、执行原FBBIK，再执行角差响应，最后进入既有唯一Physical Writer。响应 MUST拥有独立分型角差历史并归属于既有根Bank，不得创建第二Solver、图外MonoBehaviour、LateUpdate骨骼写入或独立提交。

#### Scenario: 成功帧更新角差历史

- **WHEN** 正式Profile启用响应且当前Pose、Goal和时钟合法
- **THEN** 响应 MUST只修改当前Pending Component Pose并保存Pending角差历史
- **AND** Writer成功后 MUST与Foot、Pelvis及BendHistory共同提交

#### Scenario: 后续阶段失败

- **WHEN** 角差响应完成后当前Pose事务失败
- **THEN** Committed角差与移动输入历史 MUST保持上一成功帧
- **AND** MUST不继续写Physical Bones或采用默认历史恢复

### Requirement: 膝角响应必须保留ZZZ额外角差与旋转补偿顺序

响应 MUST以直腿为0的弧度膝角计算solvedAngle−animationAngle；Forced策略 MUST按正式上下楼速率和下楼权重推进唯一额外角差历史。历史与目标的差 MUST分别以−0.5和+1系数右乘大腿、小腿局部弯曲轴旋转，并恢复脚的原世界旋转，不恢复脚位置。静态弯曲轴 MUST由同一Rig的正式引用姿态解析，不得从Contact、旧Applied方向或默认Up制造。

#### Scenario: 当前额外弯曲变化超过预算

- **WHEN** 额外角差变化大于本帧rate×deltaSeconds
- **THEN** 历史 MUST只朝目标推进该预算并用剩余补偿修改骨骼
- **AND** 当前动画本身的膝角 MUST不作为被低通的持久世界Pose

#### Scenario: 脚位置因旋转补偿改变

- **WHEN** 大腿与小腿补偿改变脚的实际位置
- **THEN** Diagnostics MUST记录真实响应后位置和最终Heel／Toe
- **AND** MUST不重解IK、恢复脚位置或修改Goal Weight来隐藏差异

### Requirement: 膝角响应必须分离Solver证据与响应后证据

正式采样 MUST分别记录FBBIK结果、动画相对膝角响应输入／历史／输出与最终Physical Writer事实。缺少新合同字段的旧采样 MUST保持旧版本或拒绝新分析，不得补零伪造覆盖。质量评分 MUST继续消费实际输出，不能以角差收敛代替膝侧、Foot间隙／穿透或Pelvis质量。

#### Scenario: Solver膝侧反转但角差变化很小

- **WHEN** 实际Knee侧向反转而无符号膝角差变化很小
- **THEN** 分析 MUST保留真实膝侧与位移证据
- **AND** MUST不因为角差响应未限速而宣布无反弯
