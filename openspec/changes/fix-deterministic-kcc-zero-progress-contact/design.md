# Design: Deterministic KCC 零进展接触收敛与碰撞作者约束

## 现状

当前movement loop执行：

```text
Cast earliest TOI
-> position = safePosition
-> remaining *= 1 - TOI
-> 收集constraint planes
-> ProjectRemaining
-> 下一轮Cast
```

`ProjectRemaining`只显式处理第一个平面、前两个平面的交线和第三平面的封闭判断。它没有把全部active constraints作为同一个几何问题求解，也没有记录上一轮零时刻接触。当query在量化边界上再次报告同一`TOI=0`阻挡，而投影结果不变时，循环既没有获得新位置，也没有获得新约束，仍只能等待固定预算耗尽。

粗糙地面当前由相邻旋转Box组成。`RoughTile_05_07`和`RoughTile_04_07`的Z区间约正体积重叠`6.5cm`。旋转Box不能降低为轴对齐Box primitive，因此每个Collider输出六个面的十二个三角形；两个闭合体相互穿插后，原本只为视觉拼缝准备的内部侧面和边也成为正式query feature。

## 目标

- 合法的复杂静态接触最多让角色停止，不让正常场景移动关闭Session。
- 只有可以由当前Fixed输入完全证明的零进展模式才正常停止，不能把真实query failure或penetration failure吞掉。
- Runtime对量化边、相邻三角形和外部导入Mesh保持稳健，不依赖场景名称或特殊SurfaceId。
- Baker拒绝当前已知的非法walkable闭合Box交叠，使正式Artifact不再包含粗糙地面的内部封闭面。
- 视觉粗糙度、Gameplay Collision和Foot Placement继续使用明确的作者数据，不在Runtime建立Surface优先级或fallback。

## Decision 1: 使用完整active-constraint投影

设原始剩余位移为`d`，全部canonical约束法线为`n[i]`，合法结果满足每个`dot(v, n[i]) >= -MinimumMovementDistance`。三维中欧氏投影的候选只可能位于：

1. 约束内部的原始`d`；
2. 任意一个约束平面上的单平面投影；
3. 任意两个非平行约束平面的交线投影；
4. 全部方向被封闭时的零向量。

Motor在预分配constraint数组上按canonical plane顺序枚举以上候选，丢弃违反任一active constraint的候选，选择与`d`平方距离最小者。距离相同则按候选类型、plane index pair和fixed raw vector字典序选择，保证两个Peer结果相同。该求解不使用LINQ、临时集合、float或动态扩容。

不采用继续只看前两个或前三个平面：碰撞结果会依赖最早进入数组的少数法线，后续真实约束不能统一参与。

不采用重复顺序clip：后一个平面的clip可能重新违反前一个平面，结果依赖遍历次数和停止阈值。

## Decision 2: 零进展必须由连续两轮相同事实证明

单轮`TOI=0`是贴墙、贴地和沿边移动的正常输入，不能直接停止。Motor只在连续两轮同时满足以下条件时认定`BlockedNoProgress`：

- 两轮earliest TOI均为Fixed零；
- `safePosition`与Cast前position完全相同；
- canonical contact count相同；
- 每项contact的`PrimitiveId`、`FeatureId`和canonical normal raw值相同；
- active-constraint求解前后的remaining raw值相同；
- 两轮没有新增或更新会改变解的constraint plane。

第一次满足时把contact signature复制到Session创建时预分配的scratch页并继续一次；第二次完全相同则清零remaining、记录transient termination reason并退出movement loop。退出位置仍是最后一个safe position，collision summary与output contact使用已经形成的canonical结果；Move开头已经完成的initial penetration recovery保持不变，随后照常执行结束阶段penetration recovery、Ground Probe和最终validation。

如果第二轮contact、normal、position或projected remaining发生变化，旧signature立即失效并继续正常求解。预算耗尽但没有形成上述证明时仍抛确定性failure。

不采用“任何投影不变立即break”：第一轮可能刚获得新的plane，下一轮仍能沿新的crease继续。

不采用把角色沿法线推出epsilon：它会额外改变Gameplay位移、坡面速度和Rollback hash，并把错误容差变成隐式配置。

## Decision 3: 零进展停止属于成功的受阻移动

`BlockedNoProgress`表示当前requested displacement在现有canonical约束下没有新的合法进展，不是query或容量故障。Motor返回正常`BodyResult`：

- position保持safe；
- applied displacement只包含已经完成的部分；
- remaining为零；
- collision summary保留Below/Sides/Above分类；
- Ground Probe仍决定最终stable support；
- transient diagnostics报告终止原因、重复次数和最后contact身份，但不进入Snapshot或StateHash。

算法策略版本进入`KccId`。因此旧客户端与新客户端不能在同一Rollback Session中静默混用。

## Decision 4: Baker拒绝会形成竞争支撑面的walkable闭合Box穿插

Baker在降低primitive之前建立稳定source record，包含Surface identity、Collider hierarchy identity、Walkable和量化后的Box八顶点。只对双方均为walkable的闭合`BoxCollider`建立稳定pair，并依次证明三个条件：

1. 双方量化后的局部Y支撑轴不平行；
2. 双方上表面四边形在水平XZ平面具有超过一个quantization cell的正面积交叠；
3. 双方八顶点通过15轴OBB SAT形成超过一个quantization cell的正体积穿插。

前两项把校验边界限定为会让不同支撑法线在同一可行走区域竞争的作者几何。水平顶面的交叉脊、墙与顶板等平行支撑实体可以继续按现有拼装进入Artifact；正确的Traversal Ramp与Top平台只在上表面投影边界相接，也必须通过。第三项使用双方三个面轴和九个叉积轴；退化轴跳过，全部投影和比较使用项目Fixed数值。

- 任一上表面水平分离轴只有边界接触：允许。
- 双方支撑轴平行：不属于本校验的竞争支撑面。
- OBB任一有效轴存在大于一个quantization cell的分离：不相交。
- 三项均成立且所有OBB有效轴都有超容差正穿透：拒绝Bake。

诊断聚合全部失败pair，并对每项包含两个稳定Collider identity、两个Surface identity和最小穿透轴/深度。pair顺序使用Baker已经锁定的Collider顺序。

本change不自动对任意Mesh执行CSG或封闭体布尔合并。自动删除内部triangle会改变winding、adjacency和外轮廓；若没有完整solid union合同，结果比拒绝非法作者数据更难审查。

## Decision 5: 粗糙地面使用一个连续作者碰撞面

保留现有`RoughTile_*`可见Mesh及Transform作为美术外观，但删除每个Tile的`BoxCollider`和其进入Deterministic Surface的资格。新增一个持久化、非运行时生成的粗糙地面Mesh资产：

- 顶面按现有Tile上表面形成连续共享边界；
- 不包含相互穿插的封闭Tile侧面和底面；
- 由一个启用、非Trigger、`Ground`层的`MeshCollider`引用；
- 由唯一`DeterministicCollisionSurfaceAuthoring`拥有并进入Fixed Artifact；
- 同一个Unity Collider供Foot Placement普通`Ground`查询；
- Collider对象无Renderer，视觉仍由原Tile负责。

旧`CourseBase`继续保留可见Cube，但删除其覆盖全部课程的BoxCollider。新增无Renderer的`CourseGroundCollision`顶面Mesh，在粗糙地面边界处精确开孔；粗糙Mesh全部外围顶点落在`y=0`，因此两份持久化Mesh只共享同一边界，不在粗糙区域上下叠出第二层Ground。

同一轮校验暴露出的旧场景错位直接在现有作者层级修正：LowStairs的Gameplay与Foot子树统一使用课程根`x=12`；Gentle/Steep坡体和各自Top平台作为完整路段移到左侧空闲车道；`Vault_H0.90_Yaw15`移出OverLimit上行Ramp。所有修改同时作用于Renderer与Collider所在Transform，不建立碰撞专用副本。

不保留旧Tile Collider作为Foot Placement专用副本，因为那会让Gameplay和脚查询看到不同的粗糙地形几何。也不把粗糙地面改成完全水平大平面，因为该课程需要继续表达连续的小坡度变化。

## 调用链

```text
持久化连续Rough Ground MeshCollider
-> DeterministicCollisionSurfaceAuthoring
-> Baker Box overlap validation / canonical mesh lowering
-> 唯一Collision Artifact + CollisionWorldHash
-> DeterministicCapsuleQueries canonical contacts
-> DeterministicKccMotor active-constraint solve
-> 普通移动或BlockedNoProgress安全停止
-> Ground Probe / final overlap
-> BodyResult
```

Foot Placement只通过Unity Physics查询同一个`Ground` MeshCollider，不读取Motor termination reason、Artifact primitive或SurfaceId。

## 失败边界

- 初始或最终penetration超过正式恢复能力：失败。
- shape cast无法在固定预算形成保守TOI：失败。
- candidate/contact/constraint/pair容量不足：失败。
- contact iteration耗尽且没有形成完整相同零进展证明：失败。
- Actor pair与静态世界无法共同满足：失败。
- walkable闭合Box同时满足支撑轴不平行、上表面水平投影正面积交叠和超容差正体积穿插：Bake失败且旧Artifact不修改。

## Active Change 协调

- 本change首先拥有Motor、Baker、粗糙地面Collision和两项身份更新。
- `add-discrete-stair-presentation`不得在旧Baker/Artifact基线上发布离散楼梯；合并本change后再执行其显式Bake任务。
- `close-deterministic-rollback-character-pipeline`不修改Motor或Baker，只把新`KccId`与`CollisionWorldHash`带入Local Fixed、Rollback Variant及产品闭包。
- 不修改或归档以上active changes。
