# 双脚需求驱动的骨盆目标与统一响应

## Why

用户已明确批准三步实施：先让有效Foot目标不因残差趋零撤销，再用双脚目标与动画脚的最低高度差替换骨盆目标，最后把真实可达硬边界与原动画弯曲偏好分开并收进唯一响应。

固定193957（Runtime eb5fb05、Diagnostics 5d858bc）为效果对照，221050为同输入逐值恢复包。212054卸载实验已撤销，只保留失败证据。其L339/L515/R611已证明0.1毫米Correction门会撤销仍有效的Foot位置约束，让Pelvis平移直接带动脚；骨盆原目标与后置Reach冲突也已由逐帧公式复算确认。

本change采用ZZZ“候选先形成共同高度，再统一响应”的结构参考。用户给定最低高度差是借鉴KKK的项目适配公式，不声称ZZZ采用该公式，也不声称完整复刻任一实现。本轮不做未来髋部前视规划。

## What Changes

1. 合法Ready Foot目标的PositionWeight只取正式FootPlacementWeight，不以Correction长度或是否恰好为零决定有效性。Unavailable、Suppress和作者零权重保留原语义；旋转政策不变。
2. 共同目标先前已由双脚最低Sole高度差取代旧地形相对高度加项。用户随后批准几何候选适配实验：在原生产资格内，以同帧原动画Ankle、预Reach有效Goal Ankle、原动画Pelvis、Component Up与正式PelvisFootProximityRadius选择共同高度。完整条件选择替换旧两个min公式，不保留并行开关；这是ZZZ候选数学的项目输入适配，不声明post-g/k脚标量已完整复刻。
3. 真实腿长及正式安全余量形成统一硬区间；原动画弯曲程度只影响目标偏好，不再构成另一份输出硬夹紧。现有单一骨盆响应消费完整边界，外部不再二次改写其输出。不可达与Foot安全继续通过现有typed Reach与Goal链表达。

## 与现有规格的关系

- current `character-foot-placement-presentation`规定唯一根事务、Resolved Pair下游边界和typed Reach；本change遵守这些限制，不新增Goal Set、Solver、查询或状态源。
- `stabilize-character-foot-path-and-landing`中已通过的ContactWorldResidual、Swing/FootHeight/Ground Path、Anchor、Rotation和作者曲线均保持。
- 当前Runtime的Correction幅度权重门及Analyzer对应不变量由第1步正式替换，不保留旧开关。
- 旧active设计仍有未实现的PelvisMaximumUpVelocity/DownVelocity草案，与当前配置和本次保持既有响应的范围不同。本change不新增该组参数；相关active delta/design需同步成当前单一响应合同，不把旧草案混入本轮。用户原有proposal.md/project.md未提交内容不打包进本change。
- 第3步替换两处输出夹紧的编排，但不把“位置绝对连续”升级为压倒真实腿长的保证。几何冲突必须显式产生必要下蹲或既有不可达结果。

## 范围与验证

只面向Corin，TrainingEnemy、KCC/Body、动画曲线、骨盆前视、卸载规则、膝盖策略、额外滤波和未定义输入的匿名ZZZ字段映射均不在范围。新增半径明确为项目世界米制候选选择范围，不冒称腿长或旧余量。

本轮与现行规格的冲突显式收口：旧“分别取最低Sole后相减”的增量要求由唯一几何候选替换；current spec的Pelvis下游输入列表补入有效Ankle和同帧只读原动画Pose，不开放Foot内部状态或诊断反读。3Hz、20毫米硬Reach和Foot/Goal政策本步不变，只为隔离目标变量；不把它们认定为未来不可替换。

三步独立提交、加载、同Record Replay，分别对上一小步和固定193957比较；不能一次改完再归因。原始采样、Proof与失败证据不删除、不覆盖。真实回归先停在该步裁决，不通过改评分、阈值或分母隐藏。Diagnostics仍由既有专门任务负责，Unity构建/回放只由主任务顺序执行。

## 实验历史入口

全部已实施步骤、失败撤销、恢复关系和只读未实施筛选统一索引在[experiments/README.md](experiments/README.md)。各步的最终处置优先于其记录中的早期“待回放”段；保留局部改进不代表全身质量通过。101451只证明20毫米候选撤销后恢复085223行为，不作为新骨盆修复，也不覆盖其后的并行Ground Path提交。
