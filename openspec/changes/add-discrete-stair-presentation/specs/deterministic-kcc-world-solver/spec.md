## MODIFIED Requirements

### Requirement: Editor Collision Baker 必须使用显式且唯一的场景作者来源

每个Deterministic Collision World Scene MUST只有一个`DeterministicCollisionWorldAuthoring`。Baker MUST只收集该根下显式`DeterministicCollisionSurfaceAuthoring`标记所拥有的活动Collider子树，MUST按稳定层级与组件身份排序，并拒绝无来源、重复归属、Trigger和不支持的Collider。轴对齐BoxCollider MUST降低为Box primitive；旋转BoxCollider MUST按固定顶点、三角形winding和adjacency规则降低为同一indexed-triangle surface。Baker MUST不创建隐藏临时Scene、代码生成替代几何或运行时Unity Physics读取路径。

场景存在`StairTraversalSurfaceAuthoring`时，Baker MUST在生成Artifact前调用唯一楼梯作者validator。每个已注册Ramp绑定的Traversal Ramp MUST位于唯一Deterministic Surface作者子树并进入Artifact；其Foot Placement Surface Collider MUST位于全部Deterministic Surface作者子树之外并从Artifact排除。未注册`StairTraversalSurfaceAuthoring`的离散楼梯 MUST作为普通`Ground` Collider沿同一显式Surface owner规则进入Artifact，Baker MUST不为其增加楼梯特判、要求Ramp、生成替代几何或读取Foot Placement配置。Baker MUST不按Layer自动收集Foot Surface，不得跳过非法Ramp楼梯、临时禁用Collider或回退逐级Gameplay碰撞。

#### Scenario: 可见坡面使用旋转 BoxCollider

- **WHEN** 显式surface marker下的旋转BoxCollider进入Bake
- **THEN** Baker MUST生成稳定量化顶点、索引、winding和adjacency
- **AND** 两个Peer MUST从相同CollisionWorldHash读取该坡面

#### Scenario: Ramp楼梯具有合法双表面作者数据

- **WHEN** 注册绑定的Traversal Ramp被唯一Deterministic Surface作者拥有且Foot Surface位于作者子树之外
- **THEN** Artifact MUST包含Ramp而不包含真实踏面Collider
- **AND** Content Hash MUST覆盖Ramp降低后的canonical geometry与surface identity

#### Scenario: Ramp楼梯Foot Surface会被Fixed Artifact收集

- **WHEN** 任一注册Ramp楼梯真实踏面Collider仍属于Deterministic Surface作者子树
- **THEN** Baker MUST在写入Artifact前失败并报告Stair、Collider和Surface owner
- **AND** 既有Artifact MUST保持未修改

#### Scenario: 普通Ground离散楼梯进入Artifact

- **WHEN** 离散阶梯Collider位于`Ground`并被唯一Deterministic Surface作者拥有且没有Ramp楼梯绑定
- **THEN** Baker MUST按普通显式Surface规则降低并收集其全部canonical geometry
- **AND** MUST不调用Ramp生成、Step专用Bake或Foot Surface排除分支
