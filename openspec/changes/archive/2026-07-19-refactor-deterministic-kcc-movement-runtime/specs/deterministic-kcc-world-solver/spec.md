## ADDED Requirements

### Requirement: Deterministic KCC 必须使用连续胶囊查询而不是终点重叠启发式

Deterministic KCC 对静态世界的移动 MUST通过 Fixed Q32.32 translational capsule shape cast 求取最早保守 TOI。实现 MUST检测起点和终点均不重叠但位移路径穿过障碍的情况，MUST不把 movement substep 或终点 overlap 后二分当作连续碰撞正确性的来源。

#### Scenario: 冲刺穿过薄墙

- **GIVEN** 胶囊起点与终点都不和薄墙重叠
- **WHEN** 请求位移线段穿过该墙
- **THEN** shape cast MUST返回墙面的最早保守 TOI
- **AND** applied displacement MUST停在 contact offset 前

#### Scenario: 查询达到迭代上限

- **WHEN** Fixed shape cast 无法在锁定 iteration budget 内形成保守结果
- **THEN** ResolveBatch MUST明确失败
- **AND** MUST不返回“无碰撞”或直接应用完整请求位移

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
