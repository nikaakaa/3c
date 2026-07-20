## Context

当前正式链路是：

```text
Fixed Program
    -> CharacterMotionRequest batch
    -> DeterministicKccWorldSolver.ResolveBatch
    -> 静态世界 candidate
    -> DeterministicActorContactSolver
    -> 静态世界重约束
    -> BodyResult batch
    -> Fixed WorldState / Snapshot / Hash
```

外层所有权是正确的，问题集中在“静态世界 candidate”内部。当前 `DeterministicCapsuleQueries.Cast` 先检查终点 overlap，只有终点重叠时才二分碰撞位置。它不是完整连续 shape cast；外部 substep 只是缩短每次位移，仍无法证明不会穿过薄障碍。当前接触只按 PrimitiveId 排序，grounding 主要依据法线 Y，step 是简化的 raise/forward/down 尝试，多墙面约束和边缘稳定性不足。

本 change 保留外层合同，替换这块内部实现。

## Reference Boundary

### Philippe KCC

Philippe KCC 用来确认成熟角色控制器应具备的行为边界：胶囊 sweep、overlap recovery、稳定 ground report、坡面、台阶、墙面滑动、ledge/denivelation 与显式刚体交互边界。

它是 Unity Asset Store Extension Asset，依赖 Unity Physics，原始算法使用 float，并不满足本项目 Fixed Q32.32、普通 .NET 可运行、snapshot/hash 与 lockstep 确定性的运行约束。因此：

- MUST不引用其 runtime assembly。
- MUST不复制其 Asset Store 源码。
- MUST不让它成为 Fixed KCC 的 package 依赖。
- MAY用它做同场景行为参照，但行为参照不进入正式运行链。

### Rapier/Parry

Rapier KCC 将“期望 shape movement”转换为“受世界约束后的 corrected movement”，并明确处理 slope、step、ground snap 与 contact。Parry 提供 Apache-2.0 的 shape cast、closest-feature 与 penetration 算法结构参考。

本 change 只移植当前静态胶囊查询需要的最小算法集合到 Fixed Q32.32，不引入 Rust FFI、Rapier world、第二套 broadphase 或完整刚体引擎。移植部分必须保留来源和许可证。

### PhysX CCT

PhysX CCT 用来参考 contact offset、overlap recovery、collide-and-slide 与 controller shape 的长期工程取舍。它的 BSD-3-Clause 源码可以作为算法参考，但本 change 不引入 PhysX runtime 或 Unity Physics 查询。

### Quantum KCC

Quantum KCC 用来核对确定性 KCC 的 stage、grounding 与 processor 边界。它是产品绑定实现，只做概念交叉检查，不复制源码。

## Architecture

```text
DeterministicKccWorldSolver.ResolveBatch
    |
    +-- Static candidate loop, stable ActorId order
    |      |
    |      +-- DeterministicKccMotor.Move
    |             |
    |             +-- DeterministicKccQueryPipeline
    |             |      +-- Broadphase candidate gather
    |             |      +-- Plane analytic query
    |             |      +-- Fixed closest-feature distance/cast
    |             |      +-- Fixed penetration solve
    |             |
    |             +-- Penetration recovery
    |             +-- Ground probe/classification
    |             +-- Continuous move and multi-plane slide
    |             +-- Step candidate transaction
    |             +-- Ground snap
    |             +-- Final overlap validation
    |
    +-- DeterministicActorContactSolver, stable pair order
    +-- Static reconstraint through the same Motor/query contract
    +-- Atomic BodyResult batch commit
```

`DeterministicKccMotor` 是纯 Fixed 移动策略，不持有 Unity 对象，不读取 Input、Network、Timeline 或 Presentation。它的输入只有 capsule state、desired displacement、上 Tick support state、KCC configuration 与只读 collision artifact；输出是 applied displacement、下一 support state、canonical contacts、diagnostics 与明确 failure。

## Decisions

### 1. 原位替换，而不是并存两个 Deterministic KCC

保留 `DeterministicKccWorldSolver` 作为唯一 Composition 可选实现，内部替换查询和 Motor。旧 endpoint-overlap cast、旧简化 ground/step/slide 在迁移完成后删除。

业务收益：Rollback Demo 的 Program、Scene、Composition、Snapshot 与网络协议不需要重新理解，也不会出现“某资产用旧 KCC、某资产用新 KCC”的结果分裂。

代价：迁移是破坏性的，旧 artifact/KccId 必须一次性重建，旧回放与新回放不能互换。

### 2. 使用真正的连续 shape cast，不把 substep 当 CCD

查询层为 upright capsule 对静态 convex primitive 求最早保守 TOI：

- Plane 使用解析距离和 TOI。
- Triangle 使用胶囊轴到 face/edge/vertex 的精确 Fixed 最近特征查询，Box 使用胶囊轴到 AABB 的精确 Fixed 最近特征查询。
- 每次距离查询输出分离距离、见证点、法线与 stable feature identity。
- 保守推进使用位移长度作为距离函数变化上界，计算 translational shape cast 的最早保守 TOI。
- 初始重叠进入固定迭代的 penetration solver，不伪造 TOI 0 普通命中。

外部 substep MAY作为移动距离预算和接触数量上界，但 MUST不再承担“避免穿墙”的正确性责任。

业务收益：冲刺和攻击位移穿过薄墙的结果不再取决于 substep 恰好切在哪里。

代价：每个受支持 Primitive 都必须提供完整 closest-feature 语义，并严格规定迭代、退化形状和 tie-break；新增任意凸形状时需要扩展同一查询合同，不会自动获得通用 GJK 支持。不收敛时只能 fail-closed。

### 3. Contact 使用稳定 Feature 身份，而不只使用 PrimitiveId

每个 query contact 至少包含：

```text
PrimitiveId
FeatureType
FeatureIndex
TimeOfImpact
Normal
WitnessPointOnCharacter
WitnessPointOnWorld
SeparationOrDepth
```

同一 TOI 内按 `PrimitiveId -> FeatureType -> FeatureIndex -> quantized witness` 排序。重复 feature 在固定容差内合并，不能因为容器遍历顺序改变接触流形。

业务收益：墙角、三角形共享边和地形坡面不会因“先碰到哪一片 primitive”随机改变滑动方向。

代价：artifact 必须生成稳定 feature/adjacency，snapshot identity 也会改变。

### 4. 多平面 collide-and-slide 是一个统一循环

Motor 对 remaining displacement 反复执行最早 TOI cast：

1. 移动到 contact offset 前的安全位置。
2. 收集同一 TOI 的 canonical contact plane set。
3. 一面接触时把 remaining 投影到该面切向。
4. 两个独立平面时只保留交线方向。
5. 三个独立平面或相反约束封闭自由度时停止。
6. 每轮验证 progress，达到固定迭代或无进展时明确失败或正常阻挡，二者使用不同结果码。

业务收益：角色沿墙移动、进入内角、冲刺斜撞墙面时不再依赖最后一次法线覆盖。

代价：需要明确 contact normal 合并容差和自由度判定，配置必须进入 KccId。

### 5. Grounding 输出稳定支持面，不等同于一次向下命中

Ground probe 产生明确报告：

```text
FoundAnyGround
IsStableOnGround
SupportPrimitiveId
SupportFeatureId
GroundNormal
GroundDistance
LedgeState
```

稳定性由坡度、胶囊底部支持区域、triangle adjacency、内外边法线、移动方向和 previous support 共同决定。陡坡可以是 collision contact，但不能成为 stable ground。地形边缘只允许在真实支持范围内继续保持 ground。

业务收益：下坡、坡顶、台阶边缘和网格三角形接缝不再频繁切换 grounded，表现层不会因逻辑 ground flag 抖动而反复切动画。

代价：KCC world state 要保存最小 previous support 信息，并纳入 snapshot/hash。

### 6. Step 是候选事务

遇到非稳定阻挡面时，只有满足 step eligibility 才尝试：

1. 上抬 sweep 验证头顶空间。
2. 前向 sweep 验证可获得最小水平进展。
3. 向下 probe 寻找不高于 MaxStepHeight 的稳定落点。
4. 验证最终 capsule 无 overlap、坡度可站立、落点不是仅靠墙面法线伪造。
5. 全部成立才接受完整 step candidate。

业务收益：能上合法台阶，同时不会把垂直墙、尖角或悬空边缘当台阶攀爬。

代价：一次 step 需要额外查询；通过显式容量和只在阻挡时尝试控制成本。

### 7. Ground snap 必须受 previous support 和运动意图约束

只有上 Tick 稳定 grounded、当前没有明确向上运动、向下距离在 SnapDistance 内、落点稳定且路径无阻挡时才 snap。离开悬崖、跳跃/上冲或越过陡坡时不得吸回地面。

业务收益：下坡连续，但攻击上挑、跳跃或离开平台时不会被错误拉地。

代价：Program 必须继续用正式 MotionRequest 表达向上意图；KCC 不猜测动作类型。

### 8. Mesh 与 Terrain 降低到同一个 canonical triangle surface

Editor baker 将 MeshCollider 与 TerrainCollider 的静态表面转换为：

- Fixed quantized vertices。
- stable indexed triangles。
- one-sided winding。
- triangle adjacency/shared-edge identity。
- canonical bounds 与 content hash。

运行时 broadphase 和 narrowphase 只认识这个 artifact，不调用 `Physics.*`、`TerrainData` 或 Mesh API。Terrain 不增加第二套 runtime primitive 语义。

业务收益：Demo 可以使用 Unity 地形和网格关卡，但每个 Peer 仍执行同一个 Fixed 查询实现。

代价：大 Terrain 的 artifact 体积高于专用 heightfield；当前 Demo 优先统一正确性，压缩/分块作为后续独立优化。

### 9. Actor contact 保持独立 batch 阶段

静态 KCC Motor 不把其他 Actor 当普通 artifact primitive。全部 Actor 先得到静态 candidate，再由现有 `DeterministicActorContactSolver` 按 stable ActorId pair order 执行 `SolidBodyBlock`，随后所有修正结果通过同一个静态 Motor/query contract 重约束。

业务收益：角色碰撞仍是 Session batch 的共同结果，静止目标不会因为单 Actor 查询顺序被隐式推行。

代价：静态 contact 与 Actor contact 是两个求解阶段，但只有一个正式 WorldSolver 和一个原子 commit，并非分裂运行路径。

### 10. 只快照会影响未来结果的状态

KCC snapshot 保存 capsule body、velocity、stable support identity/normal、grounded 状态和会改变下一 Tick 分支的 counters。Broadphase candidate、GJK simplex、临时 contact set 与 diagnostics 不进入 snapshot，也不跨 Tick warm start。

业务收益：恢复和重演完整，又不把查询缓存膨胀成网络状态。

代价：不使用跨 Tick warm start，换取更简单且可验证的确定性状态边界。

### 11. 所有容量、容差与算法版本进入身份

以下数据进入 KccId/WorldConfigurationHash：

- Fixed numeric/profile version。
- Query kernel 与 Motor semantic version。
- Contact offset、normal merge、TOI、progress 和 penetration tolerance。
- GJK/cast/penetration/move/step iteration caps。
- Candidate/contact/manifold capacity。
- Capsule、slope、step、snap 与 actor contact 配置。
- Artifact schema、quantization 与 adjacency version。

任一端不匹配时 Session 在首 Tick 前拒绝启动，不允许运行时协商成“较低功能模式”。

### 12. Tick 热路径使用预分配 layout

每个 Solver runtime 在 Session 创建时根据锁定配置分配 query scratch、candidate、contact、manifold 与 pair buffers。Tick 中不得按 Actor/Primitive 创建 List、LINQ 集合或字符串；buffer overflow 返回带 ActorId、query stage、required/capacity 的正式 failure。

业务收益：Rollback replay 重复执行相同 KCC 时不会把 GC 抖动放大。

代价：场景和 Actor 上限必须显式配置，超出即失败，而不是临时扩容。

### 13. 第三方参考的正式依赖边界

`com.janooba.kcc` 当前没有被产品源代码引用，却作为 embedded package 出现在 manifest，且 package asmdef 为 auto referenced。正式实现移除该 manifest/lock 依赖，Fixed KCC 不引用它。

若移植 Rapier/Parry 或 PhysX 的算法片段，仓库增加正式第三方声明，记录来源文件、版本/commit、许可证和本项目修改。用户本地未跟踪的参考副本不作为迁移资产处理。

## Failure Policy

- Query candidate/contact/manifold 容量不足：当前 ResolveBatch 失败。
- GJK、shape cast、penetration 或 move loop 达到迭代上限且无法形成保守结果：当前 ResolveBatch 失败。
- Artifact feature/adjacency 非 canonical、退化 triangle 未按 baker 规则剔除、hash/schema 不匹配：Session 创建失败。
- 初始 penetration 无法在固定预算内恢复：当前 ResolveBatch 失败。
- Actor contact 后静态重约束失败：完整 batch abort，不部分提交其它 Actor。
- 所有失败 MUST保留精确 stage、ActorId、Primitive/Feature 与 iteration/capacity 信息；MUST不回退 CharacterController、Unity Physics、旧查询或直接应用 desired displacement。

## Migration

1. 先引入新的 feature/contact/query 数据合同和 Fixed 查询内核，不改变 `ICharacterWorldSolver`。
2. 将现有 KCC Motor 内部调用切到新 query pipeline，并完成 grounding/slide/step/snap。
3. 将 Actor contact 后的 static reconstraint 切到同一 Motor/query contract。
4. 升级 collision artifact schema/baker，加入 Terrain lowering、indexed triangle 和 adjacency。
5. 升级 KCC state、snapshot codec、KccId 和 world hash，并重建正式 Rollback Demo 资产。
6. 删除旧 endpoint-overlap cast、旧简化 movement helpers、旧 artifact reader/schema 与 package 正式依赖。
7. 更新项目文档和实现清单；先归档基础 Rollback change，再归档本 change。

迁移期间不保留新旧 runtime switch。一个中间提交 MAY暂时编译失败，但最终仓库只有新链路。

## Alternatives And Tradeoffs

### 直接使用 Philippe KCC

优点：Unity 场景行为成熟，moving platform 和 Rigidbody 交互丰富，调试体验好。

代价：依赖 Unity Physics 与 float，源码受 Asset Store EULA 约束，不是普通 .NET Fixed Runtime，也不能直接进入 rollback state hash。它适合作为 Float32 Solver 候选和行为参照，不适合作为本 change 的 Fixed 内核。

### 直接嵌入完整 Rapier/Parry

优点：碰撞算法成熟、Apache-2.0、KCC 功能完整。

代价：需要 Rust/FFI 或移植完整 world/query 系统，会引入第二套 broadphase、world owner 和数值 ABI。当前只移植合法、必要的查询算法，继续使用本项目唯一 collision artifact 和 world state。

### 继续使用 substep + endpoint overlap

优点：代码少、成本低，普通走路可能看起来可用。

代价：它只降低 tunneling 概率，无法保证高速冲刺、薄墙和斜角行为；substep 数还会直接放大 rollback 成本。该方案不满足本 change。

### 引入 BEPUphysics1int 等完整定点物理 fork

优点：已有 Fixed broadphase/narrowphase/constraint 系统。

代价：版本老、范围远超当前无通用物理玩法的 Demo，会替换 world ownership、artifact、actor contact 和 snapshot 结构。当前业务只需要成熟 KCC，不需要完整刚体世界。

## Open Questions Resolved

- 是否做 moving platform：不做，需另行设计动态 support 的 snapshot 与速度继承。
- 是否做通用动态刚体：不做，Actor contact 继续使用 `SolidBodyBlock`。
- 是否把 Philippe KCC 加到 Fixed asmdef：不加。
- 是否保留旧 KCC 供切换：不保留。
- 是否为 Terrain 增加独立 runtime solver：不增加，统一编译为 indexed triangle surface。

## References

- [Unity Asset Store: Kinematic Character Controller](https://assetstore.unity.com/packages/tools/physics/kinematic-character-controller-99131)
- [Philippe KCC release discussion](https://discussions.unity.com/t/released-kinematic-character-controller/678434)
- [Rapier Kinematic Character Controller](https://rapier.rs/docs/user_guides/templates/character_controller/)
- [Rapier character controller source](https://github.com/dimforge/rapier/blob/master/src/control/character_controller.rs)
- [Rapier repository and Apache-2.0 license](https://github.com/dimforge/rapier)
- [PhysX Character Controllers](https://nvidia-omniverse.github.io/PhysX/physx/5.3.0/docs/CharacterControllers.html)
- [PhysX BSD-3-Clause license](https://github.com/NVIDIA-Omniverse/PhysX/blob/main/LICENSE.md)
- [Photon Quantum KCC overview](https://doc.photonengine.com/quantum/current/addons/kcc/overview)
