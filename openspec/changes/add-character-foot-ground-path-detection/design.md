# Design

## 1. 历史基线

本设计只继承三条已经验证的Git边界：

- `6a3e1d0`：删除旧Predictive Planner、Plan、Ground Envelope、Pelvis、自动控制和专用Variant。
- `a59bd0d`：建立`Biomechanical Step -> Raw Landing -> SphereCast -> Accepted/Rejected Landing`最小闭环。
- `dc0b941`：Landing改用Body Target世界速度和KCC Future Body Translation，删除Future Yaw、输入方向曲率和旧Grounding。

`6a3e1d0`之前的代码只用于核对失败原因，不复用类型、配置或执行路径。尤其不恢复可变Plan、Current Grounding、Successor、Anchor、Pelvis、WorldProjection和多层连续性。

## 2. 正式数据链

```text
Current/Incoming Step
+ committed Body Target世界速度与Timeline
-> KCC Future Body Translation
-> Raw Landing
-> Landing SphereCast
-> Accepted Landing
+ Previous Sealed Completed Sole
+ Motion Authority Identity
-> Immutable Ground Path Revision
-> Capsule Ground Detection
-> Raw Ground Contacts
-> Sealed Diagnostics
```

当前`Completed Sole`就是上一成功表现事务实际完成的Sole。由于Goal权重为零，它等于Native Animated Sole；以后Foot Goal生效时仍是同一个合同，不需要把起点改成另一条Grounding路径。

## 3. Revision与状态

每只脚只有一个Committed Page和一个Pending Page。Pending只能在外层Foot Placement Frame中创建，并随同一Presentation事务`Seal`或`Discard`。

Ground Path Revision创建时从上一成功Seal的Frame捕获Completed Sole，并冻结：

- Foot Side。
- Landing Event identity。
- Motion Timeline generation与authority tick。
- Future Body Translation source identity。
- 上一完成帧Sole位置。
- Accepted Landing位置、法线与Surface identity。
- Component Up与Ground Detection配置revision。
- 实际Capsule请求与原始查询结果。

同一Landing Event、运动权威、Accepted Landing几何和配置保持不变时复用Committed Revision，不因后续动画Sole继续移动而逐Render帧重新查询。Landing Event、运动权威、Accepted Landing几何或配置变化时，才从当时的上一完成Frame捕获新起点并创建Revision；Rejected Revision可以在后续新authority tick重新尝试，但不得把旧结果旋转、平移或冒充新Revision。Runtime首次启动且没有上一成功Frame时发布`OriginUnavailable`，不得用当前Transform或固定高度补起点。

本change没有Goal交接，因此Rejected Revision直接发布typed rejection，不让旧Revision继续代表当前地面事实。以后启用Foot Motion时，连续性必须单独建立在上一完成Goal上，不能隐藏在Ground Detection内。

## 4. Capsule查询

Capsule路径端点是上一完成Sole与Accepted Landing。完整查询包络的两个胶囊端点分别为`PathStart + ComponentUp * CastAbove`和`PathEnd + ComponentUp * CastAbove`，查询方向固定为`-ComponentUp`，距离为`CastAbove + CastBelow`。Profile显式声明半径、最大轴段长度、上方余量、下方距离、Ground Layer和固定命中容量，并要求`CastAbove`大于半径以避免合法路径在查询开始时与地面重叠。

Unity Physics的一次Capsule Cast不能保证返回同一MeshCollider上所有接触。World Query Backend MUST按最大轴段长度把完整轴确定性切成首尾连续的短段，并对每段执行实际Capsule Cast；所有短胶囊在轴上的并集等于完整Capsule包络。候选identity包含段索引和Surface identity，保证同一连续网格上的不同位置能够保留。分段数量必须由请求几何和正式配置唯一决定，不得根据首个命中临时切换成射线、Sphere Cast或第二算法。

Capsule是“脚步经过区域”的查询包络，不是鞋底形状。半径是路径采集宽度，不从Heel/Toe长度伪造脚掌宽度；Scene Gizmo必须直接绘制请求中的两个端点、半径、方向与距离。短胶囊首尾连续且并集等于完整包络，Gizmo只显示简单外包络，不重复绘制内部接缝。

Ground Detection只做以下工作：

- 过滤自身Collider、初始重叠、非法点、非法法线和重复命中。
- 保存位置、法线、Surface identity、Cast距离与稳定候选identity。
- 对结果执行只为稳定存储的canonical排序，不提前承诺Ground Path语义顺序。

它不按坡度、台阶高度或腿长删除候选。陡坡、墙面和边缘法线是后续Edge与Reachability判断所需的原始事实。

## 5. 抽象与实现

```text
CharacterFootGroundPathRevisionBuilder
    输入: immutable frame facts
    输出: query request或typed rejection

ICharacterFootGroundPathWorldQuery
    输入: capsule request + preallocated result page
    输出: raw contact count或typed rejection

CharacterFootPlacementWorldQueryBackend
    实现: Unity PhysicsScene adapter
    负责: Capsule轴分段、Collider过滤、命中归一化、固定容量写入

CharacterFootPlacementRuntime
    负责: per-foot Pending/Committed生命周期与唯一GoalSet
```

纯Revision Builder不引用`PhysicsScene`、`Collider`、Gizmo或Editor类型。Unity适配器不选择Step、不创建Revision、不写Goal。Gizmo只读取Seal后的只读摘要。

## 6. 为什么现在不做Hull

GDC顺序是：

```text
Ground Detection
-> Near/Far与Bottom/Top排序
-> Normal验证与Edge Plane
-> Reachability
-> 删除不可达候选
-> Convex Hull
-> Continuous Ground Envelope
```

当前只有Accepted Landing，没有路径候选集合。直接做Hull只能对单点或未经Edge/Reachability过滤的数据求包络，业务上无法区分可走楼梯、墙面和不可跨越高差。这个change先把Hull真正依赖的原始世界事实做成稳定边界。

## 7. 业务取舍

### 一次真实Capsule与多条射线

真实Capsule更接近GDC的路径包络，能覆盖脚步线两侧的小障碍，Gizmo也能直接表达查询范围；代价是Unity对同一MeshCollider的命中能力有限，适配器必须把轴确定性切成连续短胶囊，并处理固定容量。多条射线容易实现，但采样间距会决定是否漏掉台阶边缘，并且会把线采样错误地固化为Ground Path事实。本change选择分段Capsule作为唯一正式实现，每个子查询仍是Capsule Cast。

### 上一完成Sole与上一Landing Anchor

上一完成Sole在当前零权重阶段来自Native Pose，以后来自同一Foot Placement最终输出，能覆盖首次启动和中途Revision；代价是它本身不声明支撑Surface，必须由Capsule结果验证。上一Landing Anchor带Surface，但当前系统还没有Anchor事务，提前引入会恢复已删除的第二状态链。本change选择上一完成Sole。

### 原始候选闭环与一次做完Ground Envelope

原始候选闭环能单独确认查询形状、位置和法线是否可信，问题定位成本低；代价是本阶段仍不产生可用Foot Path。一次做完Envelope能更快看到IK目标，但会同时引入排序、Edge、Reachability、Hull和Foot Motion，任何抖动都难以定位。本change选择先完成可独立验收的Ground Detection模块。

## 8. 非目标

- 不做Ground Path排序、Edge Plane、Reachability、Convex Hull或Ground Envelope。
- 不消费Animation Clearance，不计算Foot Rate，不写Foot或Pelvis Goal。
- 不增加当前脚实时Grounding、Traditional/Predictive切换或查询失败fallback。
- 不修改FinalIK，不在FBBIK后处理Pose。
- 不为TrainingEnemy建立第二套配置或运行路径。
