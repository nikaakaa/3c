# Change: 分离楼梯Gameplay斜坡与Foot Placement踏面

## Why

当前Deterministic KCC已经按Philippe St-Amand KCC的Step、Ground Probe与Hit Stability语义完成真实台阶求解，Gameplay Lab也使用0.14m、0.24m与0.40m逐级碰撞验证能力。这证明KCC能够处理离散台阶，但连续楼梯把每一级踏面都作为Gameplay真相，会让角色Body按级改变高度；Body Presentation、骨盆、双脚和相机随后必须共同吸收这组周期性垂直位移。

本项目是求职向第三人称动作客户端Demo。普通静态连续楼梯当前不承载缺级坠落、逐级掩体、单级破坏或精确立面阻挡等玩法，因此“每一级都是Gameplay碰撞”没有形成与表现成本相称的业务收益。更符合当前展示目标的边界是：角色运动沿连续斜坡求解，双脚仍查询并踩到可见踏面。

现有场景不能通过临时增加一块斜坡实现该目标：

- `DeterministicCollisionSurfaceAuthoring`当前拥有整个环境Collider子树，斜坡与真实台阶会同时进入Fixed Collision Artifact，形成双重Gameplay碰撞。
- Foot Placement当前通过统一`GroundLayerMask`查询Unity Physics；若斜坡仍在该Mask中，双脚会踩到斜坡而不是真实踏面。
- current spec与`reimplement-deterministic-kcc-from-philippe-motor`实施清单明确记录连续楼梯“不含隐藏坡道”，与新选择直接冲突。
- 若只删除真实台阶Gameplay碰撞，现有0.14m、0.24m与0.40m路线将不再证明KCC Step能力，需要将该能力迁移到独立的真实台阶障碍课程，而不是把已经完成的Step Motor删除。

因此本change建立一条正式且唯一的楼梯表面所有权：连续楼梯使用持久化Gameplay Traversal Ramp，Foot Placement使用持久化真实踏面查询Collider；二者由同一楼梯作者绑定显式关联，但不得被同一个运行系统同时消费。

## What Changes

- 新增`StairTraversalSurfaceAuthoring`作为连续楼梯的唯一作者绑定：
  - 保存稳定Stair identity。
  - 显式引用唯一Gameplay Traversal Ramp Collider。
  - 显式引用唯一Foot Placement Surface根及其真实踏面Collider子树。
  - 显式引用下端与上端过渡边界，用于证明Ramp与可见楼梯首尾一致。
  - 只保存作者关系与校验输入，不在Runtime生成或替换Collider。
- 固定三类表面角色：
  - `Ground`：普通共享地面，可同时进入Gameplay Collision Artifact与Foot Placement查询。
  - `CharacterTraversal`：连续楼梯Gameplay Ramp，只进入Deterministic Collision Artifact，Foot Placement必须排除。
  - `FootPlacementSurface`：真实楼梯踏面查询Collider，只进入Foot Placement Physics查询，Deterministic Collision Baker必须排除。
- 扩展场景与Bake校验：
  - Traversal Ramp必须是无Renderer、非Trigger、持久化的显式Collider，并且被唯一`DeterministicCollisionSurfaceAuthoring`拥有。
  - Foot Placement踏面必须位于`FootPlacementSurface`层、可由当前PhysicsScene查询，并且不得位于任何Deterministic Collision Surface作者子树内。
  - Ramp和Foot Surface不得引用同一个Collider；下端、上端、宽度、前进方向和包围范围必须在固定容差内一致。
  - 校验失败必须阻止显式Collision Bake，不允许按Layer猜测、忽略错误Collider或回退真实台阶Gameplay碰撞。
- 迁移Gameplay Lab连续楼梯：
  - LowStairs、HighStairs与连续OverLimitStairs的Gameplay碰撞改为正式Traversal Ramp。
  - 保留可见台阶及其真实踏面Collider，仅供Foot Placement查询。
  - 新建独立Step Capability Course，以0.14m、0.24m和0.40m孤立障碍继续表达KCC Step准入与拒绝边界；该课程不伪装成连续楼梯。
  - 顶平台、入口地面与Ramp端点必须形成无重叠、无空隙的唯一Gameplay连续面。
- 迁移Foot Placement正式配置：
  - Corin查询Mask只包含`Ground`与`FootPlacementSurface`。
  - `CharacterTraversal`不得出现在Foot Placement Mask中。
  - 现有heel/toe、Ground Envelope、Free/Locked/Sliding、Heel Lift、Directional Pelvis与Actor Movement Compensation算法继续复用，不增加楼梯专用状态或按对象名分支。
- 保留Deterministic KCC完整Step能力：
  - 不修改Fixed Motor、Step Detection、Step Commit、Ground Probe、Snapshot或Hash语义。
  - 连续楼梯主要走稳定坡面；真实Step能力继续服务路沿、孤立矮障碍与Step Capability Course。
- 通过现有显式Unity菜单重新Bake唯一Collision Artifact并更新CollisionWorldHash；不在Inspector、导入、场景打开或运行时自动Bake，不自动发布Network Player。
- 更新current specs、`openspec/project.md`与KCC实施清单，删除Gameplay Lab连续楼梯“不含隐藏坡道”和“逐级楼梯重放就是正式楼梯表现方案”的旧口径。

## Impact

- 影响Gameplay Lab共享环境Prefab、Unity Layer定义、Deterministic Collision Surface作者层级、Collision Baker校验、Corin Foot Placement Profile、唯一Deterministic Collision Artifact及其Hash。
- 新CollisionWorldHash会使既有DeterministicRollback产品manifest失效；后续产品发布必须通过既有显式workflow重新执行，本change不自动构建或启动产品。
- 不改变Deterministic KCC Motor算法、KCC配置、Gameplay Body状态、Rollback Snapshot、Program ABI、World Solver批处理或Actor Contact。
- 不改变Pose Graph FootPlacement节点位置、Foot Analysis Artifact、动画作者Weight、Foot Constraint生命周期或Component Pose Solver。
- 不让Foot Placement读取Gameplay Ramp、KCC Step阶段、Collision Artifact或Snapshot；也不让KCC读取Unity Foot Surface、脚目标、骨盆偏移或动画事务。
- 不为同一连续楼梯保留“Ramp失败后改走真实台阶”的fallback。作者配置非法时必须阻止Bake或运行装配。
- 不将Gameplay Ramp生成为临时Scene对象或运行时Collider；正式Artifact只消费场景中持久化且可审计的Ramp Collider。

## 与现行Spec及Active Change对比

- `deterministic-kcc-world-solver`当前要求Collision Baker只收集显式Surface作者拥有的Collider子树。本change保留该原则，并要求连续楼梯Foot Surface明确位于这些子树之外；Baker仍不按Layer自动补收或排除普通Collider。
- `deterministic-kcc-world-solver`当前要求KCC完整实现Step Up/Down相关能力。本change不删除或弱化该能力，只把连续视觉楼梯从Step能力课程中分离，新增孤立Step Capability Course继续表达真实边界。
- `character-foot-placement-presentation`当前要求virtual ground来自合法有限命中且不能是隐藏Collider。本change继续让Foot Placement命中真实踏面Collider；无Renderer的Gameplay Ramp被Foot查询明确排除，因此不会成为virtual ground。
- current `character-foot-placement-presentation`只要求一个非空`GroundLayerMask`，没有定义Gameplay专用Ramp与表现专用踏面的排他所有权。本change补充`Ground + FootPlacementSurface`查询口径及`CharacterTraversal`排除规则。
- `reimplement-deterministic-kcc-from-philippe-motor`已完成且不归档；其Motor、配置、状态和portable重放结论继续成立，但其中Gameplay Lab连续楼梯“不含隐藏坡道”、逐级楼梯高度结果与当前CollisionWorldHash将在本change实施后被新场景事实取代。
- `repair-foot-placement-calibration-and-limb-solving`已经修复Rig v3 Calibration与解析式腿部求解，本change复用该唯一表现链，不新增第二IK、Final IK Grounder或图外Transform写入。

