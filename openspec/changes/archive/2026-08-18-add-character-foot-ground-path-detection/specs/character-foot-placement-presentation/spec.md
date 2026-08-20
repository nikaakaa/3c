## ADDED Requirements

### Requirement: Ground Path必须使用上一已提交落点与下一事件落点

每只脚 MUST按Landing Event identity缓存Accepted Landing。PreSwing或Swing阶段的每个有效表现帧 MUST执行一次且仅一次正式Landing SphereCast；同一事件的后续权威Accepted结果 MUST允许更新NextSwingLanding，不能把首次预测永久冻结。更新距离小于正式Foot Motion Profile的死区时 MUST保留原落点并复用Ground Path，但 MUST不停止下一表现帧的正式Landing预测。该事件实际落地后最新NextSwingLanding MUST晋级为LastLanding，之后才为新的Swing事件建立下一落点。

Ground Path MUST只使用LastLanding与NextSwingLanding构造查询输入。没有LastLanding时 MUST发布`CurrentLandingUnavailable`；不得用Animated Sole、Transform、固定高度或默认地面补起点。

#### Scenario: 同一Landing Event持续多个表现帧

- **WHEN** NextSwingLanding Event identity没有变化且新的Accepted Landing移动超过更新死区
- **THEN** Runtime MUST提交新的NextSwingLanding并重建同一Foot Placement事务中的Ground Path
- **AND** Ground Path重建 MUST消费该表现帧已经产生的唯一SphereCast结果，不得为重建再执行第二次Landing查询

#### Scenario: 同一Landing Event的小幅预测误差

- **WHEN** 新的Accepted Landing与缓存点的距离小于正式更新死区
- **THEN** Runtime MUST复用缓存落点与Committed Ground Path
- **AND** MUST继续执行下一表现帧的唯一Landing预测，但不得因毫米级误差触发新的Capsule Ground Detection

#### Scenario: 下一Swing Event完成

- **WHEN** NextSwingLanding对应的事件成为已完成Swing Event
- **THEN** Runtime MUST把该Accepted Landing晋级为新的LastLanding
- **AND** MUST只为新的PreSwing或Swing Event建立新的NextSwingLanding
### Requirement: Ground Detection必须发布原始Capsule接触集合

Ground Detection MUST沿LastLanding到NextSwingLanding构造唯一Capsule请求。两个轴端点 MUST分别为`LastLanding + ComponentUp * CastAbove`与`NextSwingLanding + ComponentUp * CastAbove`，查询方向 MUST为`-ComponentUp`，距离 MUST为`CastAbove + CastBelow`。请求 MUST显式携带半径、最大轴段长度、Ground Layer和固定命中容量；Capsule只表示路径采集包络，不表示鞋底或最终Ground Envelope。

Unity World Query Backend MUST按最大轴段长度确定性切分Capsule轴并对每段执行真实Capsule Cast。每段Physics命中缓冲容量 MUST使用`SegmentHitCapacity`，整条路径Raw Contact页容量 MUST使用独立的`ContactCapacity`；两者都必须由同一个Ground Detection Profile正式配置并预分配。Backend MUST过滤自身Collider、初始重叠、非法几何和同分段重复命中，并发布分段索引、Surface、位置、法线、查询距离和稳定candidate identity。Backend不得改用Raycast、Sphere Cast或第二种查询算法。

#### Scenario: Capsule命中多个表面

- **WHEN** 分段Capsule Cast命中多个合法表面
- **THEN** Backend MUST在固定容量页中保留各接触的位置和法线
- **AND** MUST不先压成单个落点或中心线

#### Scenario: Capsule没有合法命中

- **WHEN** 查询没有合法接触或固定容量溢出
- **THEN** Runtime MUST发布对应typed rejection
- **AND** MUST不生成默认地面或替代查询

### Requirement: Ground Envelope必须来自可达Edge与上侧凸包

Ground Envelope Builder MUST把Raw Contacts投影到脚步纵向与Component Up组成的二维平面，按Near/Far、Bottom/Top和candidate identity稳定排序。Builder MUST在法线有效时用相邻接触的位置与法线定义地面平面；法线无效不得丢弃有效碰撞位置，只有位于两接触距离和高度范围内的平面交点 MAY成为Edge候选。

同一路径距离 MUST保留最高候选，Path Start与Target Landing MUST作为首尾端点保留。`CastAbove`和`CastBelow` MUST只用于Capsule查询范围，不得作为Reachability限值。

正式Ground Path Profile MUST提供米制`MaximumReachableVerticalEdge`。Builder MUST在同路径距离折叠前保留每个Edge的Bottom与Top，并检查全部Edge沿Component Up的垂直距离。任一Edge超过限值时，Ground Path MUST发布`UnreachableEdge`与首个Invalid Segment，不得删除障碍点后继续构造Hull，不得沿用旧Envelope，也不得把KCC Step高度、Cast范围或腿长作为替代限值。

只有全部Edge通过Reachability时，全部合法候选才 MUST形成二维上侧Convex Hull，输出从Path Start到Target Landing的连续折线。该折线 MUST位于全部保留候选的Component Up上侧或与其重合，并且只属于feet-only地面下界；它 MUST不携带Animation Clearance、不改变Foot XZ、不驱动Pelvis。

#### Scenario: 路径经过台阶

- **WHEN** 合法接触与法线定义出台阶边缘且全部Edge不超过正式Reachability限值
- **THEN** Ground Envelope MUST保留上侧Hull关键转折点
- **AND** MUST不退化为LastLanding到NextSwingLanding中心直线

#### Scenario: 路径经过不可达垂直面

- **WHEN** 任一Edge的Bottom到Top垂直距离超过`MaximumReachableVerticalEdge`
- **THEN** Ground Path MUST发布`UnreachableEdge`并记录首个Invalid Segment
- **AND** Accepted Ground Envelope MUST为空
- **AND** Raw Contacts与Edge事实 MUST保留在同一成功Seal的只读诊断页

### Requirement: Ground Path模块必须保持抽象与实现分离

Foot Placement Runtime MUST只依赖World Query合同、Ground Envelope Builder和预分配结果页。纯Builder MUST不引用`PhysicsScene`、`Collider`、`RaycastHit`、Gizmo或Editor类型；Unity Backend MUST不选择Step、构造Hull或写Goal；Gizmo MUST不重新查询或重算算法。

Raw Contacts、Builder workspace和Envelope顶点 MUST预分配。左右脚 MUST各自只有一个Committed Page和一个Pending Page，并随外层Foot Placement事务执行`Seal`或`Discard`。

#### Scenario: 提交Foot Placement Frame

- **WHEN** 外层Frame成功Seal
- **THEN** Raw Contacts与Ground Envelope MUST作为同一Foot Placement结果原子提交
- **AND** Debug读取 MUST不改变下一帧状态

## MODIFIED Requirements

### Requirement: 当前Landing阶段必须保持Pose恒等

当前阶段增加Capsule Ground Detection、Edge、Reachability与feet-only Ground Envelope，但不实现Foot Motion、FootLock、Constraint、Anchor或Pelvis。Pelvis与双脚Goal的位置和旋转权重 MUST全部为零；唯一FullBodyIK MUST在验证Goal lineage后跳过FBBIK求解并保持输入Pose不变。

#### Scenario: Ground Envelope构建完成

- **WHEN** 任一脚提交Accepted Ground Envelope
- **THEN** 该结果 MUST只进入同一成功Seal后的diagnostics
- **AND** 脚、骨盆和其它Physical Bone MUST继续使用原动画Pose

### Requirement: Foot Placement诊断必须只显示当前事实

Scene诊断 MUST保留上一已提交Accepted Landing与下一Landing Event的Cached Accepted Landing，并以左右脚不同颜色绘制最近一次成功Seal的最终Ground Envelope粗折线。`UnreachableEdge`时 MUST只额外绘制首个Invalid Segment的红色细线且不得绘制Accepted Envelope。诊断 MUST不绘制Current到Incoming中心直线、逐帧Animated Sole、Capsule外框、扫掠边线、文字、矩形鞋底或伪Path。

完整请求、Raw Contacts、Edge数量、首个Invalid Segment、Reachability限值与Envelope顶点 MUST来自成功Seal的只读摘要。CSV MUST记录同一Edge与Reachability事实。Gizmo和采样器 MUST不重新采样动画、查询世界、计算Reachability或计算Hull。

#### Scenario: 查看Ground Envelope诊断

- **WHEN** 用户打开Foot Placement Scene诊断
- **THEN** 显示折线 MUST逐点等于最近一次成功Seal的Ground Envelope
- **AND** 读取诊断 MUST不改变下一帧结果

#### Scenario: 查看Invalid Segment诊断

- **WHEN** 最近一次成功Seal的Ground Path状态为`UnreachableEdge`
- **THEN** Scene红线端点 MUST等于只读摘要中的Invalid Segment Bottom与Top
- **AND** CSV中的Vertical Distance与正式限值 MUST来自同一摘要
