# Change: 修复 Deterministic KCC 零进展接触

## Why

Gameplay Lab粗糙地面在约`(-17.62, 0.10, 13.49)`存在稳定复现的Fixed KCC movement failure。`RoughTile_05_07`与`RoughTile_04_07`是相互穿插的旋转`BoxCollider`，Baker把它们分别降低为两套封闭三角形；胶囊到达交叠接缝后连续得到同一`TOI=0`接触，现有双平面投影没有改变剩余位移，Motor仍重复同一轮直到耗尽`MaximumContactIterations`并关闭Session。

这不是Foot Placement、LegIK、动画累计误差或query capacity溢出。把迭代上限调大只会重复相同计算；只移动两个Tile会把相同风险留在其它接缝；只让Runtime吞掉异常会继续接受带内部面和内部边的正式Collision Artifact。

## What Changes

- 在唯一`DeterministicKccMotor`中把现有前两平面特判替换为有界的三维active-constraint求解：从原始剩余位移、一平面投影、两平面交线与零向量中按固定顺序选择满足全部active constraints且与原始位移距离最小的结果。
- 为movement loop增加无分配的零进展状态：只有`TOI=0`、safe position未变化、canonical blocking contact set与上一轮相同、active-constraint结果与本轮输入完全相同同时成立时，才把当前状态认定为已经收敛到“无可行进展”，清零剩余位移并正常结束本次Motor movement。
- 保留真实失败边界：penetration recovery、shape cast、容量、Actor pair/static reconstraint和无法证明为相同零进展接触的迭代耗尽继续使完整batch失败，不回退Unity Physics、Float32或直接位移。
- 在Editor Collision Baker写Artifact前，按稳定Collider identity顺序检查同一World中进入Artifact的walkable闭合`BoxCollider`。先要求双方支撑轴不平行且上表面水平投影存在正面积交叠，再基于量化后的八顶点和Fixed OBB SAT判断正体积穿插；超过一个quantization cell的危险穿插直接失败，平行支撑实体拼装与合法坡面/平台边界相接不误报。
- Baker只校验并拒绝非法作者数据，不自动删除三角形、不按Surface优先级忽略命中、不执行CSG合并，也不修改场景对象。
- 将Gameplay Lab粗糙地面从“每个可见Tile同时提供一个相互穿插的封闭Box”迁移为“可见Tile只表达外观，一个持久化连续Ground MeshCollider表达Gameplay与Foot Placement表面”。将旧`CourseBase`碰撞改为围绕粗糙区域开孔的持久化顶面Mesh，使两者只在同一`y=0`外边界相接；删除旧Tile Collider，不保留运行时选择或旧Artifact兼容。
- 清理由新Baker校验暴露的现有作者错位：LowStairs全部Gameplay/Foot子树统一服从其`x=12`课程根；Gentle/Steep坡道路段及其平台整体移入空闲车道；单个Vault障碍移出OverLimit上行Ramp。视觉与Collider继续位于同一Transform，不建立专用碰撞副本。
- 通过现有显式Collision Bake入口发布唯一新Artifact，更新`CollisionWorldHash`；提升Motor策略身份并更新`KccId`，Local Fixed与DeterministicRollback继续共享同一KCC和Collision身份。
- 不修改Foot Placement、LegIK、Character Animation、Presentation、Camera、Body Motion Integrator、Step/Ramp策略或Network Model算法。

## Impact

- Runtime：`Assets/GameScripts/Main/Runtime/Simulation/DeterministicKcc/Kcc/DeterministicKccMotor.cs`、constraint求解与transient diagnostics。
- Editor：`Assets/GameScripts/Main/Editor/CharacterSimulation/NetworkProducts/DeterministicRollback/DeterministicCollisionWorldBaker.cs`及同目录唯一几何校验模块。
- 场景资产：`Assets/Scenes/Shared/CharacterMovementTestEnvironment.prefab`、持久化粗糙地面与课程地面collision mesh、唯一Fixed Collision Artifact及其引用身份。
- Current spec：`deterministic-kcc-world-solver`。
- Active change协调：`add-discrete-stair-presentation`必须在本change的新KCC/Collision身份之后显式Bake；`close-deterministic-rollback-character-pipeline`只消费刷新后的共享身份，不并行修改Motor或Baker。

## Current Spec 对账

- Current `KCC失败必须终止确定模拟而不回退`把所有iteration limit耗尽统称为non-convergence。此次修改明确：满足完整零进展证明的状态已经确定收敛为“当前剩余位移被约束封闭”，必须正常停止；无法形成该证明的预算耗尽仍然失败。
- Current `Deterministic KCC必须统一处理去穿透和多平面 Collide-And-Slide`只规定一面、两面和约束封闭的结果，没有规定超过两个active plane的统一求解和相同`TOI=0`接触的收敛出口；此次补齐。
- Current Editor Baker合同规定了显式来源、稳定降低和退化三角形拒绝，但没有拒绝两个不同支撑方向、上表面投影正面积重叠的合法来源Collider之间的正体积穿插；此次补上与故障几何一致的walkable闭合Box校验，同时保留平行实体拼装和坡面/平台边界接触。
- Active `add-discrete-stair-presentation`声明KCC算法不变并计划重新Bake同一Artifact。它与本change共享Collision Artifact发布点，必须串行：本change先更新Motor/Baker/粗糙地面和身份，离散楼梯change再在该基线上增加普通Ground台阶。
