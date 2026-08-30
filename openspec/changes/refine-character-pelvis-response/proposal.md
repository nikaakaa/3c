# 双脚需求驱动的骨盆目标与统一响应

## Why

用户已明确批准三步实施：先让有效Foot目标不因残差趋零撤销，再用双脚目标与动画脚的最低高度差替换骨盆目标，最后把真实可达硬边界与原动画弯曲偏好分开并收进唯一响应。

固定193957（Runtime eb5fb05、Diagnostics 5d858bc）为效果对照，221050为同输入逐值恢复包。212054卸载实验已撤销，只保留失败证据。其L339/L515/R611已证明0.1毫米Correction门会撤销仍有效的Foot位置约束，让Pelvis平移直接带动脚；骨盆原目标与后置Reach冲突也已由逐帧公式复算确认。

本change采用ZZZ“候选先形成共同高度，再统一响应”的结构参考。用户给定最低高度差是借鉴KKK的项目适配公式，不声称ZZZ采用该公式，也不声称完整复刻任一实现。本轮不做未来髋部前视规划。

## What Changes

1. 合法Ready Foot目标的PositionWeight只取正式FootPlacementWeight，不以Correction长度或是否恰好为零决定有效性。Unavailable、Suppress和作者零权重保留原语义；旋转政策不变。
2. 在现有Pelvis生产资格内，以同帧原动画双脚Sole与Resolved有效目标Sole形成有符号期望偏移：`min(targetL,targetR)-min(animatedL,animatedR)`，高度均沿同一Component Up。替换旧地形相对高度加正向脚补偿，不叠加两套公式。
3. 真实腿长及正式安全余量形成统一硬区间；原动画弯曲程度只影响目标偏好，不再构成另一份输出硬夹紧。现有单一骨盆响应消费完整边界，外部不再二次改写其输出。不可达与Foot安全继续通过现有typed Reach与Goal链表达。

## 与现有规格的关系

- current `character-foot-placement-presentation`规定唯一根事务、Resolved Pair下游边界和typed Reach；本change遵守这些限制，不新增Goal Set、Solver、查询或状态源。
- `stabilize-character-foot-path-and-landing`中已通过的ContactWorldResidual、Swing/FootHeight/Ground Path、Anchor、Rotation和作者曲线均保持。
- 当前Runtime的Correction幅度权重门及Analyzer对应不变量由第1步正式替换，不保留旧开关。
- 旧active设计仍有未实现的PelvisMaximumUpVelocity/DownVelocity草案，与当前配置和本次保持既有响应的范围不同。本change不新增该组参数；相关active delta/design需同步成当前单一响应合同，不把旧草案混入本轮。用户原有proposal.md/project.md未提交内容不打包进本change。
- 第3步替换两处输出夹紧的编排，但不把“位置绝对连续”升级为压倒真实腿长的保证。几何冲突必须显式产生必要下蹲或既有不可达结果。

## 范围与验证

只面向Corin，TrainingEnemy、KCC/Body、动画曲线、骨盆前视、卸载规则、膝盖策略、额外滤波和匿名ZZZ参数均不在范围。

三步独立提交、加载、同Record Replay，分别对上一小步和固定193957比较；不能一次改完再归因。原始采样、Proof与失败证据不删除、不覆盖。真实回归先停在该步裁决，不通过改评分、阈值或分母隐藏。Diagnostics仍由既有专门任务负责，Unity构建/回放只由主任务顺序执行。
