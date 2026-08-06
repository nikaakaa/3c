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

场景存在`StairTraversalSurfaceAuthoring`时，Baker MUST在生成Artifact前调用唯一楼梯作者validator。每段连续梯段的Traversal Ramp MUST位于唯一Deterministic Surface作者子树并进入Artifact；其Foot Placement Surface Collider MUST位于全部Deterministic Surface作者子树之外并从Artifact排除。Baker MUST不按Layer自动收集Foot Surface，不得跳过非法楼梯、临时禁用Collider或回退逐级Gameplay碰撞。

#### Scenario: 可见坡面使用旋转 BoxCollider

- **WHEN** 显式surface marker下的旋转BoxCollider进入Bake
- **THEN** Baker MUST生成稳定量化顶点、索引、winding和adjacency
- **AND** 两个Peer MUST从相同CollisionWorldHash读取该坡面

#### Scenario: 连续楼梯具有合法双表面作者数据

- **WHEN** Traversal Ramp被唯一Deterministic Surface作者拥有且Foot Surface位于作者子树之外
- **THEN** Artifact MUST包含Ramp而不包含真实踏面Collider
- **AND** Content Hash MUST覆盖Ramp降低后的canonical geometry与surface identity

#### Scenario: Foot Surface会被Fixed Artifact收集

- **WHEN** 任一连续楼梯真实踏面Collider仍属于Deterministic Surface作者子树
- **THEN** Baker MUST在写入Artifact前失败并报告Stair、Collider和Surface owner
- **AND** 既有Artifact MUST保持未修改

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

Grounding MUST以锁定版本Philippe KCC `EvaluateHitStability`与`ProbeGround`的行为顺序作为主要语义基准，并使用Fixed query重新表达。每个ground或movement hit MUST先按坡度得到base stability，再使用与reference一致的inner/outer向下ray probe形成`FoundInnerNormal`、`InnerNormal`、`FoundOuterNormal`、`OuterNormal`、ledge side、ledge distance与朝空侧运动状态；实现 MUST不使用第二个capsule landing代替reference ray probe。

Ground report MUST分别输出`FoundAnyGround`、`IsStableOnGround`、`SnappingPrevented`、support Surface/Primitive/Feature identity、Ground/Inner/Outer normal、distance与ledge state。陡坡 MAY成为collision contact但 MUST不成为stable ground。previous grounding、previous inner normal、当前inner/outer normal、ledge distance、movement direction与MaximumStableDenivelationAngle MUST共同决定是否允许继续snap；任何诊断不得反向覆盖该结果。

#### Scenario: 胶囊位于普通稳定踏面

- **WHEN** ground capsule sweep命中可站立表面且inner/outer probe没有形成禁止稳定的ledge或denivelation条件
- **THEN** Grounding MUST报告FoundAnyGround与IsStableOnGround
- **AND** MUST提交该surface的canonical support identity与Ground/Inner/Outer normal

#### Scenario: 台阶鼻部需要内外法线

- **WHEN** capsule contact本身位于台阶鼻部，但inner ray命中稳定踏面、outer ray命中空侧或非稳定面
- **THEN** Hit Stability MUST按Philippe ledge规则记录inner/outer证据
- **AND** MUST不因capsule contact法线不稳定直接丢弃合法step detection

#### Scenario: 角色越过允许站立的ledge距离

- **WHEN** 角色位于空侧、到ledge距离超过MaximumStableDistanceFromLedge或朝空侧运动触发正式限制
- **THEN** Grounding MUST取消stable snap或设置SnappingPrevented
- **AND** MUST不跨空侧保留previous support

#### Scenario: 下坡法线变化超过denivelation限制

- **WHEN** 当前inner与outer normal或previous inner与当前outer normal的下降夹角超过MaximumStableDenivelationAngle
- **THEN** Grounding MUST设置SnappingPrevented并保留movement结果
- **AND** MUST不通过独立Step Down重新吸附到该落点

### Requirement: Step Up必须由当前movement contact触发完整候选事务，下降必须由Ground Probe统一处理

Step Up MUST只由当前movement sweep的真实非稳定contact触发，并以锁定版本Philippe KCC的`DetectSteps`、`CheckStepValidity`与movement step commit顺序作为主要语义基准。Standard detection MUST使用MaximumStepHeight上方的capsule down sweep、候选最终位置capsule overlap、outer stable ray、实际rise upward capsule clearance和inner stable ray；Extra detection MUST只在Standard失败后按MinimumRequiredStepDepth建立补充capsule down sweep，并复用同一个validity流程。`MinimumRequiredStepDepth` MUST不再表达两个capsule landing必须同高。

Philippe `Collider`身份 MUST映射为collision artifact `SurfaceId`。Baker MUST为每个作者Collider生成一个稳定`SurfaceId`，同一MeshCollider或TerrainCollider生成的全部Primitive MUST共享该身份。Step detection与commit MUST选择相同`SteppedSurfaceId`，但movement blocker MAY属于另一个SurfaceId，且实现 MUST不要求相同PrimitiveId、triangle adjacency或outer/inner高度差落在QueryTolerance内。Detection候选位置与Commit最终位置 MUST分别按对应cast distance扣除`CollisionOffset`。

previous state稳定且hit normal本身不稳定时，Motor MUST按Philippe `GetObstructionNormal`从previous ground normal、hit normal与Up重算有效障碍法线。只有该有效obstruction近似垂直、previous state稳定grounded、当前请求没有明确向上脱离意图时才可commit。垂直障碍判断、Commit前进方向与普通constraint plane MUST共用该有效法线。Commit MUST从safe position沿障碍内侧加入SteppingForwardDistance，从MaximumStepHeight上方向下capsule cast到相同SurfaceId landing，验证最终无overlap并保留remaining movement进入同一Motor loop。

下降 MUST不再使用独立Step Down candidate。合法下坡与下台阶 MUST由previous grounding控制的Ground Probe、SnappingPrevented和ledge/denivelation规则统一处理。Step与Ground Probe只改变position、applied displacement与support结果，MUST不写入或推导VerticalVelocity。

#### Scenario: Standard路径走上普通楼梯

- **WHEN** previous state稳定grounded、当前近似垂直obstruction存在合法outer capsule候选、outer与inner ray法线稳定、实际rise净空充足且commit找到相同SurfaceId landing
- **THEN** KCC MUST提交Step Up
- **AND** MUST把remaining movement继续送入同一movement loop

#### Scenario: Extra路径走上窄但合格踏面

- **WHEN** Standard detection没有合法候选，但在MinimumRequiredStepDepth位置的Extra capsule sweep通过同一validity流程
- **THEN** KCC MUST记录Extra来源的ValidStepDetected与SteppedSurfaceId
- **AND** Commit MUST继续遵守相同SurfaceId、垂直obstruction、最终overlap与remaining规则

#### Scenario: 胶囊鼻部接触不能替代ray probe

- **WHEN** outer或inner位置的capsule contact法线不稳定，但reference位置的point ray命中稳定顶部
- **THEN** Step Detection MUST使用ray probe结果判断踏面
- **AND** MUST不返回当前outer/inner capsule landing拒绝原因

#### Scenario: Mesh表面的不同triangle属于同一作者Collider

- **WHEN** blocker、validity landing与commit landing具有相同SurfaceId但PrimitiveId不同
- **THEN** KCC MAY接受该Step candidate
- **AND** MUST按canonical hit顺序选择最终PrimitiveId与FeatureId作为support

#### Scenario: movement blocker与合法踏面属于不同Collider

- **WHEN** 当前movement blocker与CheckStepValidity选中的踏面具有不同SurfaceId，但commit重新命中该SteppedSurfaceId
- **THEN** KCC MAY接受该Step candidate
- **AND** MUST不把blocker SurfaceId强加为SteppedSurfaceId

#### Scenario: 垂直墙没有合法顶部

- **WHEN** capsule down sweep、outer/inner ray、upward clearance、相同SurfaceId commit或最终overlap任一阶段失败
- **THEN** KCC MUST不提交任何Step位置
- **AND** MUST从原safe position与原contact继续普通multi-plane projection

#### Scenario: 连续走下合法楼梯

- **WHEN** previous grounding允许snap且扩展Ground Probe在正式距离内找到未被ledge或denivelation禁止的stable surface
- **THEN** KCC MUST通过Ground Probe提交下降后的稳定位置
- **AND** MUST不建立第二个Step Down事务

#### Scenario: 离开平台

- **WHEN** 扩展Ground Probe没有stable surface或当前ledge/denivelation规则设置SnappingPrevented
- **THEN** KCC MUST保留movement位置并报告非稳定ground
- **AND** MUST不跨悬崖吸附到MaximumStepHeight内的任意表面

### Requirement: Ground Probe必须受上一支持面和当前运动意图约束

Ground Snap MUST收敛为Philippe KCC Ground Probe的一部分，不得与独立Step Down并存。没有previous stable或last movement ground证据时，probe MUST只使用MinimumGroundProbingDistance；previous snap未被禁止且previous stable或LastMovementIterationFoundAnyGround成立时，probe距离 MUST为`max(Radius, MaximumStepHeight) + GroundDetectionExtraDistance`。非稳定hit MAY在固定GroundProbeReboundDistance与迭代预算内沿命中面继续探测；stable hit只有在`SnappingPrevented == false`且当前MotionRequest没有明确向上分量时才可提交位置。

#### Scenario: 上一帧稳定地面上的连续下坡

- **WHEN** previous stable成立且扩展Ground Probe找到合法stable surface
- **THEN** KCC MUST允许连续snap并更新完整ground report
- **AND** MUST保存下一Tick需要的inner/outer normal与SnappingPrevented状态

#### Scenario: 明确向上位移

- **WHEN** MotionRequest包含明确向上分量
- **THEN** KCC MUST不提交扩展ground snap
- **AND** MUST不通过Step Down或previous support覆盖该运动意图

#### Scenario: 非稳定hit后找到稳定地面

- **WHEN** 第一次ground sweep命中非稳定面，但固定rebound范围内沿命中面继续探测得到稳定地面
- **THEN** Ground Probe MAY提交该稳定地面
- **AND** 查询次数与方向变化 MUST服从锁定budget和canonical顺序

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

Fixed KCC runtime MUST只依赖portable Core与Fixed数值模块，MUST不引用UnityEngine、Unity Physics、CharacterController、DotRecast或第三方Unity KCC assembly。角色movement policy MUST主要基于proposal锁定版本的Philippe St-Amand Kinematic Character Controller，并逐项记录Sweep、Raycast、Overlap、Hit Stability、Step、Ground Probe、Ledge/Denivelation和remaining movement的Fixed语义映射。正式实现 MUST使用项目命名、Fixed类型、canonical identity和collision artifact重写，不得把未跟踪reference源码、Unity对象或third-party assembly复制进正式Runtime。

Rapier/Parry与PhysX MAY继续作为底层shape query、penetration和数值稳健性参考，但 MUST不覆盖Philippe movement policy。OpenKCC MAY继续作为静态测试课程参考，但 MUST不成为runtime movement算法或fallback。

#### Scenario: 构建portable Fixed KCC

- **WHEN** 编译Deterministic KCC程序集及其portable依赖
- **THEN** 依赖图 MUST不包含`com.janooba.kcc`、`KinematicCharacterMotor`、Unity Physics或OpenKCC runtime
- **AND** movement policy文档 MUST能追溯到锁定package版本和源文件哈希

#### Scenario: reference查询类型被映射

- **WHEN** Philippe branch调用CharacterCollisionsSweep、CharacterCollisionsRaycast或CharacterCollisionsOverlap
- **THEN** Fixed实现 MUST分别调用语义对应的capsule cast、raycast或capsule overlap
- **AND** MUST不为减少查询类型而改变该branch的准入意义

#### Scenario: 本地reference版本变化

- **WHEN** 本地package版本或锁定SHA-256与proposal不一致
- **THEN** 实施 MUST停止并先更新reference对账文档
- **AND** MUST不静默按新文件继续实现或保留旧行为fallback

### Requirement: KCC 失败必须关闭当前批次且不得回退

Invalid artifact、initial penetration 无法恢复、query non-convergence、capacity overflow、static reconstraint failure 或 unsupported dynamic world state MUST产生精确 solver failure。系统 MUST不回退旧查询、Unity Physics、CharacterController、float solver、直接位移或部分 Actor commit。

#### Scenario: Actor Contact 后重约束失败

- **WHEN** 任一 Actor 在 pair contact 修正后无法通过固定预算完成静态重约束
- **THEN** 完整 ResolveBatch MUST失败
- **AND** 所有 Actor 的 committed world state MUST保持未修改

### Requirement: Deterministic KCC必须约束已积分位移而不私有拥有重力

Deterministic KCC MUST消费Fixed Body Motion Integrator产生的完整XYZ `CharacterMotionRequest`，继续以唯一Motor执行continuous cast、slide、step、Grounding与Ground Snap，并准确返回applied displacement、稳定Grounded及方向性Above/Below。previous state稳定且本Tick没有明确向上意图时，Motor MUST在movement sweep前把平面请求重定向到previous ground tangent，并约束已积分位移中的负Y分量；角色不稳定或离地时 MUST继续消费原始负Y位移。只有现有`IsStableOnGround`语义可以映射为portable Grounded；`FoundAnyGround`、非稳定陡坡或普通下方接触 MUST不冒充稳定Grounded。KCC Motor、query kernel、Solver Definition与collision artifact MUST不保存GravityAcceleration、MaximumFallSpeed或私有VerticalVelocity积分规则。Deterministic KCC只有在调用Fixed唯一Body Motion Finalize提交VerticalVelocity后才能声明`AirborneVerticalMotion`。

#### Scenario: 稳定地面上的已积分重力位移

- **WHEN** previous state稳定、MotionRequest包含平面移动与负Y重力位移且没有明确向上意图
- **THEN** Motor MUST把平面移动重定向到previous ground tangent并约束负Y分量
- **AND** MUST继续由Ground Probe更新最终support，而不是让ground TOI零接触抢占台阶obstruction

#### Scenario: Fixed Actor离开悬崖

- **WHEN** Prepare产生向下gravity delta且KCC找不到稳定支持面
- **THEN** KCC MUST报告Airborne而不执行跨断崖Ground Snap
- **AND** Fixed Finalize MUST保存candidate VerticalVelocity
- **AND** KCC MUST不自行再次应用Gravity

#### Scenario: Fixed Actor向上撞顶

- **WHEN** 最终request向上且continuous capsule query命中上方阻挡
- **THEN** KCC MUST报告Above并返回受约束的applied displacement
- **AND** Fixed Finalize MUST按统一规则清零向上VerticalVelocity
