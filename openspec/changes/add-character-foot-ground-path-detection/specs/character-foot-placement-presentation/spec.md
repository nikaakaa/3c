## ADDED Requirements

### Requirement: Ground Path必须从两次Accepted Landing重新查询

每只脚 MUST在同一Foot Placement事务内得到Current Accepted Landing和Incoming Accepted Landing。Ground Path MUST只使用这两次落点、同一帧的Future Body Translation、Component Up和Ground Detection配置作为查询输入。输入变化时 MUST重新执行对应的Landing和Ground Detection；不得旋转、平移、补全或沿用旧地形命中。

#### Scenario: 两次落点有效

- **WHEN** Current与Incoming Landing都通过真实SphereCast
- **THEN** Runtime MUST使用两次Accepted Landing构造唯一Capsule Ground Detection请求
- **AND** MUST把查询结果交给Ground Envelope Builder

#### Scenario: 任一落点无效

- **WHEN** 任一Landing被拒绝或Future Body不可用
- **THEN** Runtime MUST发布对应typed rejection
- **AND** MUST不构造替代地面或使用旧查询结果补端点

### Requirement: Ground Detection必须发布原始Capsule接触集合

Ground Detection MUST沿Current Accepted Landing到Incoming Accepted Landing构造唯一Capsule请求。两个轴端点 MUST分别为`CurrentLanding + ComponentUp * CastAbove`与`IncomingLanding + ComponentUp * CastAbove`，查询方向 MUST为`-ComponentUp`，距离 MUST为`CastAbove + CastBelow`。请求 MUST显式携带半径、最大轴段长度、Ground Layer和固定命中容量；Capsule只表示路径采集包络，不表示鞋底或最终Ground Envelope。

Unity World Query Backend MUST按最大轴段长度确定性切分Capsule轴并对每段执行真实Capsule Cast。它 MUST过滤自身Collider、初始重叠、非法几何和同分段重复命中，并发布分段索引、Surface、位置、法线、查询距离和稳定candidate identity。Backend不得改用Raycast、Sphere Cast或第二种查询算法。

#### Scenario: Capsule命中多个表面

- **WHEN** 分段Capsule Cast命中多个合法表面
- **THEN** Backend MUST在固定容量页中保留各接触的位置和法线
- **AND** MUST不先压成单个落点或中心线

#### Scenario: Capsule没有合法命中

- **WHEN** 查询没有合法接触或固定容量溢出
- **THEN** Runtime MUST发布对应typed rejection
- **AND** MUST不生成默认地面或替代查询

### Requirement: Ground Envelope必须来自排序边缘与上侧凸包

Ground Envelope Builder MUST把Raw Contacts投影到脚步纵向与Component Up组成的二维平面，按Near/Far、Bottom/Top和candidate identity稳定排序。Builder MUST验证投影法线，并用相邻接触的位置与法线定义地面平面；只有位于两接触距离和高度范围内的平面交点 MAY成为Edge候选。

同一路径距离 MUST保留最高候选，Current与Incoming Landing MUST作为首尾端点保留。Builder MUST使用Ground Detection的`CastAbove`和`CastBelow`检查相邻边缘高度变化及候选相对两端线性高度的偏差；超出范围 MUST发布`UnreachableEnvelope`，不得删除障碍后继续或沿用旧包络。

可达候选 MUST形成二维上侧Convex Hull，输出从Current Landing到Incoming Landing的连续折线。该折线 MUST位于全部保留候选的Component Up上侧或与其重合，并且只属于feet-only地面下界；它 MUST不携带Animation Clearance、不改变Foot XZ、不驱动Pelvis。

#### Scenario: 路径经过台阶

- **WHEN** 合法接触与法线定义出台阶边缘
- **THEN** Ground Envelope MUST保留上侧Hull关键转折点
- **AND** MUST不退化为Current到Incoming中心直线

#### Scenario: 地形高度不可达

- **WHEN** 任一边缘或路径偏差超过CastAbove或CastBelow
- **THEN** Runtime MUST发布`UnreachableEnvelope`
- **AND** MUST不输出替代Envelope

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

Scene诊断 MUST保留绿色Current Accepted Landing与黄色Incoming Accepted Landing，并以左右脚不同颜色绘制最近一次成功Seal的最终Ground Envelope粗折线。诊断 MUST不绘制Current到Incoming中心直线、Current Animated Sole、Capsule外框、扫掠边线、文字、矩形鞋底或伪Path。

完整请求、Raw Contacts与Envelope顶点 MUST来自成功Seal的只读摘要。Gizmo MUST不重新采样动画、查询世界或计算Hull。

#### Scenario: 查看Ground Envelope诊断

- **WHEN** 用户打开Foot Placement Scene诊断
- **THEN** 显示折线 MUST逐点等于最近一次成功Seal的Ground Envelope
- **AND** 读取诊断 MUST不改变下一帧结果
