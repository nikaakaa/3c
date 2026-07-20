# deterministic-kcc-world-solver Specification

## Purpose

定义 DeterministicRollback Fixed Target 使用的确定性胶囊角色世界求解合同，包括版本化静态碰撞世界、连续查询、稳定地面、坡面、台阶、墙面滑动、Actor 接触和原子批量提交。

## Requirements
### Requirement: Deterministic KCC 必须实现 Portable World Solver 合同

DeterministicKccWorldSolver MUST通过 ICharacterWorldSolver 接收 portable BodyState、MotionRequest、Tick context 和确定性 world state，并返回 portable BodyResult。Program/Kernel MUST不引用 KCC concrete type 或 Network Model。

#### Scenario: Kernel 执行 KCC Motion

- **WHEN** Kernel 产生 portable MotionRequest
- **THEN** KCC MUST返回确定 BodyResult 并由 Kernel 更新 SimulationState

### Requirement: Collision World 必须使用版本化量化 Artifact

DeterministicCollisionWorldArtifact MUST保存 MapId、quantization、bounds、surface/material catalog、canonical static primitives、stable order 和 content hash。Runtime MUST不读取 Unity Physics scene、Mesh instance id 或动态场景几何。

#### Scenario: 两端加载地图

- **WHEN** 两端加载同一 CollisionWorldHash
- **THEN** MUST获得相同 primitive order 与量化数据

### Requirement: Editor Collision Baker 必须使用显式且唯一的场景作者来源

每个Deterministic Collision World Scene MUST只有一个`DeterministicCollisionWorldAuthoring`。Baker MUST只收集该根下显式`DeterministicCollisionSurfaceAuthoring`标记所拥有的活动Collider子树，MUST按稳定层级与组件身份排序，并拒绝无来源、重复归属、Trigger和不支持的Collider。轴对齐BoxCollider MUST降低为Box primitive；旋转BoxCollider MUST按固定顶点、三角形winding和adjacency规则降低为同一indexed-triangle surface。Baker MUST不创建隐藏临时Scene、代码生成替代几何或运行时Unity Physics读取路径。

#### Scenario: 可见坡面使用旋转 BoxCollider

- **WHEN** 显式surface marker下的旋转BoxCollider进入Bake
- **THEN** Baker MUST生成稳定量化顶点、索引、winding和adjacency
- **AND** 两个Peer MUST从相同CollisionWorldHash读取该坡面

### Requirement: KCC 必须使用确定数值与固定查询顺序

KCC gameplay calculation MUST使用 core fixed/quantized math、stable candidate/contact order、固定 iteration limit 和明确 overflow policy。KCC state/hash MUST不包含 float/double、Unity Vector/Quaternion、无序集合结果 或未保存随机数。

#### Scenario: 同一 Capsule 碰到两个 Surface

- **WHEN** query 同时返回多个 candidate/contact
- **THEN** KCC MUST按 canonical primitive/contact order 处理

### Requirement: KCC 必须完整实现已声明移动能力

KCC 若声明 capsule-static-world capability，MUST完整处理 capsule cast/overlap、ground probing/snap、slope limit、step up/down、wall slide、penetration resolution 和 yaw/motion 顺序。KCC 若声明 `WorldFeature.ActorCollision`，MUST同时完整实现 stable pair order、连续相对 sweep、初始重叠去穿透、接触响应、静态世界重新约束与最终 pair separation validation。未实现的 moving platform 或 dynamic body MUST不出现在 capability manifest。

#### Scenario: Program 需要 Moving Platform

- **WHEN** Program/world profile 要求 moving-platform capability
- **THEN** KCC combination MUST拒绝创建

### Requirement: KCC 必须批量解决 Actor 身体接触

DeterministicKccWorldSolver MUST在同一个 World batch中先为全部 Actor生成静态世界 candidate，再按 stable ActorId pair order解决 fixed capsule身体接触。系统 MUST使用垂直区间过滤与双方相对位移的连续平面 sweep，MUST不按 Actor逐个提交、读取 Unity Collider或在 Presentation中执行第二次碰撞。

#### Scenario: 两个 Actor 相向冲刺

- **WHEN** 两个 Actor在同一SimulationTick内高速相向移动
- **THEN** KCC MUST使用同一batch的相对sweep找到接触时刻
- **AND** 两端 MUST按相同pair顺序得到相同BodyResult与KCC hash

### Requirement: Actor 接触必须使用 SolidBodyBlock 语义

Actor contact MUST只实现运动学`SolidBodyBlock`。静止目标 MUST阻挡主动闭合的移动者而不被隐式推行；双方移动时 MUST裁剪相对闭合法向并保留切向移动。系统 MUST不计算质量、冲量、弹性或动量交换，也 MUST不按Action、Team、Animation producer或Network role改变响应。

#### Scenario: 移动 Actor 撞到静止 Actor

- **WHEN** Actor A主动移动并接触静止Actor B
- **THEN** Actor A的闭合法向位移 MUST被裁剪
- **AND** Actor B MUST不因该接触获得隐式推行位移

### Requirement: Actor 接触修正必须重新约束静态世界并原子提交

每轮 Actor pair修正后，KCC MUST重新应用静态世界约束，并在最终提交前同时验证静态 penetration与所有有效pair的最小间距。全部 Actor BodyResult与next world state MUST原子提交；pair容量溢出、固定迭代不收敛或两类约束无法同时满足时，整个Step MUST失败。

#### Scenario: Actor 被另一 Actor 挤向墙面

- **WHEN** Actor pair接触修正会让其中一方进入静态墙体
- **THEN** KCC MUST重新约束墙面并继续固定次数的pair求解
- **AND** 无法同时满足墙面与Actor间距时 MUST拒绝整个Step

### Requirement: Actor Contact 配置必须进入 Solver 身份

Fixed contact radius、height、skin、pair capacity、iteration count与策略版本 MUST进入KccId或WorldConfigurationHash。Solver只有完整实现Actor contact合同后才能声明`WorldFeature.ActorCollision`。任何影响未来Tick的contact cache MUST进入World Snapshot与StateHash；无跨Tick cache时不得伪造snapshot字段。

#### Scenario: 两端 Contact Profile 不同

- **WHEN** 两端的contact radius、iteration count或策略版本不同
- **THEN** Rollback handshake MUST因KCC/World identity不匹配而拒绝Session

### Requirement: KCC State 必须参与 World Snapshot 与 Hash

KCC actor/world state MUST全部进入 Fixed `SimulationWorldStateSet` 并由完整 `SimulationWorldSnapshot` capture/restore/hash，包含 body、velocity、grounded和stable support primitive/feature/normal，以及其它确实会影响未来Tick分支的固定状态。瞬态query candidate、contact manifold、step/ledge诊断和iteration统计 MUST不进入Snapshot或Hash。

#### Scenario: Restore 坡面上的 Actor

- **WHEN** Rollback Pipeline恢复 Actor在坡面 grounded的 world snapshot
- **THEN** KCC ground/slope state MUST与 BodyState 同时恢复

### Requirement: KCC 失败必须终止确定模拟而不回退

Overflow、query capacity、iteration non-convergence、invalid artifact 或 unsupported dynamic state MUST产生精确 deterministic solver failure。系统 MUST不回退 Unity Physics、CharacterController、float solver 或直接应用 request displacement。

#### Scenario: Contact Iteration 不收敛

- **WHEN** KCC 达到固定 iteration limit 仍无法解决 penetration
- **THEN** MUST报告明确 failure 并停止该 simulation session

### Requirement: Deterministic KCC 必须使用连续胶囊查询而不是终点重叠启发式

Deterministic KCC 对静态世界的移动 MUST通过 Fixed Q32.32 translational capsule shape cast 求取最早保守 TOI。保守推进 MUST使用位移相对当前 canonical separating normal 的闭合速度计算时间增量；查询在容差外且没有正闭合速度时 MUST判定该 primitive 不会命中。实现 MUST检测起点和终点均不重叠但位移路径穿过障碍的情况，MUST不把 movement substep 或终点 overlap 后二分当作连续碰撞正确性的来源。

#### Scenario: 冲刺穿过薄墙

- **GIVEN** 胶囊起点与终点都不和薄墙重叠
- **WHEN** 请求位移线段穿过该墙
- **THEN** shape cast MUST返回墙面的最早保守 TOI
- **AND** applied displacement MUST停在 contact offset 前

#### Scenario: 查询达到迭代上限

- **WHEN** Fixed shape cast 无法在锁定 iteration budget 内形成保守结果
- **THEN** ResolveBatch MUST明确失败
- **AND** MUST不返回“无碰撞”或直接应用完整请求位移

#### Scenario: 浅角度接近缓坡

- **GIVEN** 胶囊以浅角度接近一个可站立坡面
- **WHEN** 当前分离距离主要由较小的法向闭合分量消除
- **THEN** shape cast MUST按该法向闭合速度推进到保守 TOI
- **AND** MUST不因使用整个位移速度反复欠推进而耗尽固定迭代预算

### Requirement: Fixed Query Kernel 必须具有稳定 Feature 身份和 canonical contact set

每个静态接触 MUST包含 stable PrimitiveId、FeatureId、TOI、法线、角色/世界见证点和 separation/penetration。候选与接触 MUST按锁定规则排序、去重和合并；结果 MUST不依赖容器遍历顺序、对象实例地址、浮点 epsilon 或运行时随机数。

#### Scenario: 同一时刻命中三角形共享边

- **WHEN** 胶囊在同一 TOI 命中共享一条边的两个 triangle
- **THEN** Query Kernel MUST使用 adjacency 与 stable feature identity 形成 canonical contact set
- **AND** 两个 Peer MUST得到相同接触顺序和法线集合

### Requirement: Deterministic KCC 必须统一处理去穿透和多平面 Collide-And-Slide

单次 Motor movement MUST按固定阶段执行初始 penetration recovery、最早 TOI 移动、contact offset、多平面 remaining displacement 投影和最终 overlap validation。一面接触 MUST保留切向位移，两面独立接触 MUST限制到交线，约束封闭时 MUST停止剩余位移。

#### Scenario: 斜向移动撞墙

- **WHEN** 请求位移同时包含朝向墙面的分量和沿墙分量
- **THEN** KCC MUST阻止法向分量并保留合法切向分量
- **AND** MUST不因最后处理的 contact 不同而改变结果

#### Scenario: 贴地胶囊执行纯切向移动

- **GIVEN** grounded 胶囊与支持面的 separation 只存在不超过 QueryTolerance 的 Fixed 归一化残差
- **WHEN** MotionRequest 相对该支持面只有切向分量
- **THEN** shape cast MUST忽略该容差内接触并保留完整合法切向位移
- **AND** MUST不把支持面报告为 `TOI=0` blocking contact

#### Scenario: 稳定坡面重定向平面位移

- **GIVEN** 角色上一 Tick 位于稳定坡面且本 Tick包含平面请求位移
- **WHEN** KCC把平面位移重定向到支持面切平面
- **THEN** 重定向后的坡面切向位移 MUST保持原平面请求位移的长度
- **AND** 显式请求Y分量 MUST继续独立叠加，MUST不被坡面重定向当作平面速度归一化

#### Scenario: 进入内墙角

- **WHEN** 胶囊在同一次 movement 中受到两个独立墙面约束
- **THEN** KCC MUST只保留两平面交线允许的位移
- **AND** 最终 pose MUST通过 overlap validation

### Requirement: Grounding 必须区分任意地面命中与稳定支持面

Ground query MUST分别输出 `FoundAnyGround` 与 `IsStableOnGround`，并记录 stable support primitive/feature、ground normal、distance 与 ledge state。稳定性 MUST考虑坡度、胶囊底部支持区域、triangle adjacency、边缘类型和 previous support；陡坡 MAY作为 collision contact，但 MUST不成为 stable ground。

#### Scenario: 角色移动到坡顶共享边

- **WHEN** 胶囊从一个可站立 triangle 移到相邻可站立 triangle
- **THEN** KCC MUST使用 adjacency 保持稳定 support 连续性
- **AND** grounded MUST不因 primitive id 改变而无依据闪断

#### Scenario: 角色离开悬崖

- **WHEN** 胶囊底部已没有足够稳定支持区域
- **THEN** `IsStableOnGround` MUST变为 false
- **AND** MUST不跨不相邻 feature 保留 previous support

### Requirement: Step Up/Down 必须作为完整候选事务

Step MUST依次验证向上 clearance、前向最小进展、向下稳定落点、最大高度和最终无重叠。只有全部阶段成功时才能接受完整 step candidate；任一阶段失败 MUST放弃该 candidate，不得部分提交上抬或前移结果。

#### Scenario: 走上合法台阶

- **WHEN** 障碍高度不超过 MaxStepHeight、头顶空间充足且前方存在稳定落点
- **THEN** KCC MUST接受完整 step candidate
- **AND** 最终 pose MUST位于稳定地面且无 overlap

#### Scenario: 垂直墙被误判为台阶

- **WHEN** 上抬后无法获得最小水平进展或无法找到稳定落点
- **THEN** KCC MUST拒绝 step candidate
- **AND** MUST按普通阻挡/slide 处理原请求

### Requirement: Ground Snap 必须受上一支持面和当前运动意图约束

Ground snap MUST只在上一 Tick 稳定 grounded、当前没有明确向上位移、目标落点在 SnapDistance 内且为稳定地面时执行。Snap path MUST经过连续查询，MUST不穿过陡坡、断崖或其它阻挡。

#### Scenario: 连续下坡

- **WHEN** 角色上一 Tick 稳定 grounded 且下坡落点在 SnapDistance 内
- **THEN** KCC MAY向稳定落点 snap
- **AND** MUST更新新的 support identity

#### Scenario: 向上攻击位移

- **WHEN** MotionRequest 包含明确向上位移
- **THEN** KCC MUST不执行 ground snap

#### Scenario: Actor接触修正后重新施加静态约束

- **WHEN** Actor接触批处理修改了角色候选位置并要求再次执行静态去穿透与Ground query
- **THEN** 静态重约束 MUST复用原Motor movement依据上一稳定支持面和当前请求Y确定的Ground probe资格与距离
- **AND** MUST不把初始化放置使用的完整SnapDistance无条件应用到Airborne或明确向上移动的角色

### Requirement: Mesh 与 Terrain 静态作者数据必须降低为同一 canonical surface

Editor baker MUST将受支持的 MeshCollider 与 TerrainCollider 数据转换为 Fixed quantized indexed triangles、stable feature identity、one-sided winding、triangle adjacency、canonical bounds 与 content hash。Fixed Runtime MUST只读取该 artifact，MUST不调用 Unity Physics、TerrainData 或 Mesh API。

#### Scenario: 烘焙 Unity Terrain

- **WHEN** 作者场景包含受支持的 TerrainCollider
- **THEN** baker MUST生成与 Mesh 使用同一查询合同的 indexed-triangle surface
- **AND** artifact hash MUST覆盖量化顶点、索引、winding 和 adjacency

#### Scenario: 量化后出现退化 triangle

- **WHEN** triangle 在 Fixed 量化后坍缩
- **THEN** baker MUST按 canonical 规则拒绝该输入并报告 primitive 来源
- **AND** MUST不生成运行时跳过该 triangle 的兼容分支

### Requirement: 静态 KCC 与 Actor Contact 必须保持唯一批处理链路

同一 ResolveBatch MUST先为全部 Actor 通过同一个 DeterministicKccMotor 计算静态 candidate，再按 stable ActorId pair order执行 `SolidBodyBlock`，随后通过同一个静态 query/Motor contract重约束修正后的 pose。任一 Actor 失败 MUST abort完整 batch；只有全部成功后才能原子提交 BodyResult。

#### Scenario: 两名角色在墙边相撞

- **WHEN** Actor contact 修正使任一角色靠近静态墙面
- **THEN** 修正后的两个 pose MUST都经过同一静态重约束
- **AND** MUST不调用旧 KCC helper、Unity Physics 或第二个 Solver

### Requirement: KCC 支持状态和算法身份必须进入 Snapshot 与 Hash

会影响下一 Tick movement 分支的 stable support、ground normal、grounded state 与固定 counters MUST进入 Fixed KCC state、world snapshot 和 state hash。Query kernel/Motor version、容差、迭代上限、容量、capsule/slope/step/snap 配置以及 artifact schema/quantization/adjacency version MUST进入 KccId 或 WorldConfigurationHash。

#### Scenario: 在坡面上恢复历史

- **WHEN** Rollback 恢复一个稳定站在坡面上的 Tick
- **THEN** support primitive/feature、ground normal 与 grounded state MUST和 body pose一起恢复
- **AND** replay 的下一 Tick MUST使用恢复后的 support state

#### Scenario: Peer 使用不同 contact tolerance

- **WHEN** 两个 Peer 的 contact tolerance 或 query version 不同
- **THEN** Session MUST在首 Tick 前因 KccId/WorldConfigurationHash 不匹配而拒绝启动

### Requirement: Deterministic KCC 热路径必须有界且无隐式扩容

Solver runtime MUST在 Session 创建时按锁定配置预分配 candidate、contact、simplex、manifold、pair 与 scratch buffer。ResolveBatch 热路径 MUST不使用 LINQ、按查询创建集合、字符串或自动扩容；容量不足 MUST产生包含 stage、ActorId 和 required/capacity 的确定性失败。

#### Scenario: Contact 数量超过锁定容量

- **WHEN** 当前 query 需要的 canonical contact 数量超过配置容量
- **THEN** ResolveBatch MUST明确失败并报告 required/capacity
- **AND** MUST不截断 contact、扩大容量或跳过剩余 primitive

### Requirement: Fixed KCC 必须保持可移植并遵守第三方来源边界

Fixed KCC runtime MUST只依赖 portable Core 与 Fixed 数值模块，MUST不引用 UnityEngine、Unity Physics、CharacterController、DotRecast 或第三方 Unity KCC assembly。Philippe KCC 源码 MUST不被复制到 Fixed Runtime；移植自 Apache-2.0/BSD-3-Clause 来源的算法 MUST保留正式第三方声明。

#### Scenario: 构建 Fixed Runtime 程序集

- **WHEN** 只编译 portable Fixed/Core/DeterministicKcc source set
- **THEN** 编译依赖图 MUST不包含 `Gawidev.KCC`、`KinematicCharacterMotor` 或 Unity Physics

### Requirement: KCC 失败必须关闭当前批次且不得回退

Invalid artifact、initial penetration 无法恢复、query non-convergence、capacity overflow、static reconstraint failure 或 unsupported dynamic world state MUST产生精确 solver failure。系统 MUST不回退旧查询、Unity Physics、CharacterController、float solver、直接位移或部分 Actor commit。

#### Scenario: Actor Contact 后重约束失败

- **WHEN** 任一 Actor 在 pair contact 修正后无法通过固定预算完成静态重约束
- **THEN** 完整 ResolveBatch MUST失败
- **AND** 所有 Actor 的 committed world state MUST保持未修改

### Requirement: Deterministic KCC必须约束已积分位移而不私有拥有重力

Deterministic KCC MUST消费Fixed Body Motion Integrator产生的完整XYZ `CharacterMotionRequest`，继续以唯一Motor执行continuous cast、slide、step、Grounding与Ground Snap，并准确返回applied displacement、稳定Grounded及方向性Above/Below。只有现有`IsStableOnGround`语义可以映射为portable Grounded；`FoundAnyGround`、非稳定陡坡或普通下方接触 MUST不冒充稳定Grounded。KCC Motor、query kernel、Solver Definition与collision artifact MUST不保存GravityAcceleration、MaximumFallSpeed或私有VerticalVelocity积分规则。Deterministic KCC只有在调用Fixed唯一Body Motion Finalize提交VerticalVelocity后才能声明`AirborneVerticalMotion`。

#### Scenario: Fixed Actor离开悬崖

- **WHEN** Prepare产生向下gravity delta且KCC找不到稳定支持面
- **THEN** KCC MUST报告Airborne而不执行跨断崖Ground Snap
- **AND** Fixed Finalize MUST保存candidate VerticalVelocity
- **AND** KCC MUST不自行再次应用Gravity

#### Scenario: Fixed Actor向上撞顶

- **WHEN** 最终request向上且continuous capsule query命中上方阻挡
- **THEN** KCC MUST报告Above并返回受约束的applied displacement
- **AND** Fixed Finalize MUST按统一规则清零向上VerticalVelocity
