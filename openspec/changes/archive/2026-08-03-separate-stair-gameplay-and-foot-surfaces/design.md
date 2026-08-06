# Design: 楼梯Gameplay斜坡与Foot Placement踏面分离

## Context

当前连续楼梯只有一套Collider，同时承担两种彼此冲突的职责：

```text
Visible Stair Collider
        +-> Deterministic Collision Baker -> Fixed KCC逐级Step
        +-> Unity PhysicsScene -> Foot Placement踏面查询
```

这让碰撞与画面完全一致，但也把逐级Body高度变化变成Gameplay事实。目标链路改为：

```text
StairTraversalSurfaceAuthoring
        |
        +-> Gameplay Traversal Ramp
        |      -> DeterministicCollisionSurfaceAuthoring
        |      -> Collision Artifact
        |      -> Fixed KCC稳定坡面运动
        |
        +-> Visible Foot Surface Treads
               -> Unity PhysicsScene
               -> Foot Placement heel/toe/envelope
               -> Component Pose双脚与骨盆
```

两套几何由同一个作者绑定关联，但每个消费者只看其中一套。一个绑定只表示Lower到Upper的单调上升梯段；一条包含上行、顶平台和下行的往返路线必须拆成上行与下行两个绑定，顶平台继续属于普通`Ground`。分离是正式数据模型，不是运行时fallback。

## Goals

- 连续楼梯上的Gameplay Body、速度和相机锚点沿稳定斜坡连续变化。
- 双脚仍然踩到可见踏面，并继续使用现有Foot Placement状态、预测、Ground Envelope与骨盆求解。
- Deterministic KCC保留真实Step能力，用于路沿、孤立矮障碍和明确Step课程。
- 楼梯两套几何具有稳定身份、明确所有者和可阻断的结构校验。
- Collision Artifact继续只从显式持久化场景Collider生成，Runtime不读取Unity Physics。
- Local Fixed与DeterministicRollback继续引用同一Collision Artifact与Foot Placement Projection。

## Non-Goals

- 不删除、简化或替换Philippe语义的Fixed Step Motor。
- 不让Gameplay Ramp模拟破损楼梯、缺级坠落或每级台阶Gameplay交互。
- 不增加Foot Placement楼梯专用动画状态、Timeline lane、Marker真相或动作名分支。
- 不增加运行时Collider生成、自动坡道fallback或按Layer猜测作者意图。
- 不在`OnInspectorGUI`、`OnValidate`、资源导入、场景打开或Play Mode入口执行Collision Bake。
- 不自动构建、发布或启动DeterministicRollback Network Product。
- 不新增测试；用户负责Unity端到端验收。

## Decision 1: 连续楼梯的Gameplay真相是Ramp，真实踏面只属于Presentation

所有标记为连续楼梯的单调梯段必须拥有唯一Traversal Ramp。Fixed KCC只消费该Ramp在Collision Artifact中的量化表面，不再消费楼梯每一级的Box/Mesh Collider。Foot Placement继续消费真实踏面Collider，并完全排除Ramp。现有Low、High与OverLimit路线各自拆成ascent与descent两个梯段作者，共六个Ramp；descent作者仍按实际上坡方向把低端声明为Lower、顶平台端声明为Upper。

Ramp不是对KCC失败的修复路径，也不是第二份候选。对同一楼梯而言不存在“先试Ramp，失败再Step”的运行分支；Collision Artifact中只有Ramp。KCC Step仍然存在，但只能在真实进入Artifact的路沿、孤立障碍和Step Capability Course上触发。

### Tradeoff

- Ramp作为Gameplay真相：Body与Camera连续，连续楼梯不再产生逐级垂直脉冲；代价是角色可以在视觉两级之间的斜坡位置停止，Gameplay不再认识单独立面。
- 真实踏面作为Gameplay真相：碰撞与画面完全一致；代价是连续楼梯的每一级都需要Body、骨盆、脚锁和Camera共同平滑。本change不采用该方案表达普通连续楼梯。
- 同时Bake Ramp和真实踏面：看似保留两边能力，但会形成重叠碰撞、候选顺序依赖和无法解释的Grounding结果，不采用。

## Decision 2: 表面角色同时由作者绑定、Layer和Baker所有权证明

新增`StairTraversalSurfaceAuthoring`，其正式输入为：

```text
StairId
TraversalRampCollider
FootSurfaceRoot
LowerTransition
UpperTransition
```

它不拥有运行时逻辑，只为Editor校验和显式Bake提供唯一关系。三类表面角色固定为：

| 角色 | Unity Layer | Deterministic Surface所有权 | Foot Placement Mask |
|---|---|---|---|
| 普通共享地面 | `Ground` | 必须拥有 | 包含 |
| 连续楼梯Ramp | `CharacterTraversal` | 必须拥有 | 排除 |
| 可见楼梯踏面 | `FootPlacementSurface` | 必须排除 | 包含 |

Layer只决定Unity Physics查询范围，不能替代Deterministic Baker的显式作者所有权。Baker仍只收集`DeterministicCollisionSurfaceAuthoring`拥有的Collider；楼梯绑定validator额外证明Ramp在其内、Foot Surface在其外。

### Tradeoff

- Layer与作者所有权双重证明：运行消费者边界明确，错误可以在Bake前阻断；代价是场景层级和Layer必须一起维护。
- 只依赖Layer：配置简单，但当前Deterministic Baker并不按Layer收集，修改Layer就会悄悄改变另一系统语义，不采用。
- 只依赖层级：Fixed Artifact正确，但Foot Placement仍可能命中Ramp，不能形成完整排他边界，不采用。

## Decision 3: Ramp是持久化显式Collider，不由Collision Baker临时生成

Traversal Ramp使用场景或Prefab中持久化的BoxCollider。作者可以通过明确的楼梯作者命令创建或更新该Collider，但命令结果必须保存到资产，之后由现有Collision Baker读取。Collision Bake本身不根据踏面猜坡、不创建临时Scene对象，也不在Runtime补齐缺失Ramp。

Validator必须证明：

- Ramp存在、启用、非Trigger、没有Renderer且Layer为`CharacterTraversal`。
- Ramp被且只被一个Deterministic Surface作者拥有。
- Foot Surface根至少包含一个启用、非Trigger的合法Collider，全部位于`FootPlacementSurface`层。
- Foot Surface Collider不被任何Deterministic Surface作者拥有。
- Ramp与Foot Surface没有重复Collider引用。
- Ramp上表面的下端与上端分别匹配Lower/Upper Transition高度和水平边界。
- Ramp宽度覆盖可行走踏面宽度，方向与楼梯上行方向一致。
- Ramp与入口地面、顶平台在固定容差内连续，不能同时重叠出第二支持面或留下空隙。

几何容差属于唯一Editor校验合同，不成为每角色Profile或运行时宽松参数。

### Tradeoff

- 持久化Collider：可在Scene View中检查、能进入版本控制和稳定Bake；代价是资产中真实存在一套无Renderer几何。
- Collision Bake即时生成：减少Prefab对象，但生成结果不可直接审计，容易让Unity Physics与Fixed Artifact看到不同世界，不采用。
- Runtime生成：可以动态适配，但破坏Fixed Artifact和网络身份，不采用。

## Decision 4: Foot Placement查询真实踏面，不读取Ramp或KCC Step状态

Corin `GroundLayerMask`迁移为`Ground | FootPlacementSurface`。`CharacterTraversal`必须被排除，Support Query继续对heel、toe、当前路径和Future Landing执行现有NonAlloc查询。

Body Grounded和Body位姿仍来自Fixed KCC Ramp；Foot support来自Unity真实踏面。Planner继续执行：

```text
Final Component Pose
  -> heel/toe Current Support
  -> Future Landing + Ground Envelope
  -> Free/Locked/Sliding
  -> Heel Lift + Foot Rotation
  -> Directional Pelvis Reach
  -> Component Pose Limb Solve
```

`SuddenMotionOnly` Actor Movement Compensation继续保留，因为路沿和孤立Step仍可能产生离散Body高度变化；连续Ramp通常不会越过其Sudden阈值。系统不得以Ramp法线替代真实踏面法线，也不得从KCC support identity推断Foot Surface。

### Tradeoff

- 查询真实踏面：脚掌和骨盆仍匹配画面；代价是Gameplay Body可能停在两个踏面之间，Foot Planner必须用已有可达性和约束释放处理。
- 查询Ramp：脚目标稳定且与Body一致，但会在视觉台阶之间悬空，不采用。
- 同时查询并给真实踏面优先级：会建立运行时surface fallback和重叠候选顺序，不采用。

## Decision 5: 连续楼梯与Step能力课程必须分开

Gameplay Lab中的LowStairs、HighStairs与连续OverLimitStairs全部迁移为Ramp Gameplay Collision；每条往返路线使用上行和下行两个单调Ramp，顶平台保持共享Ground。原0.14m、0.24m、0.40m Step能力边界迁移到独立`StepCapabilityCourse`：

- 0.14m孤立台阶表达普通Step准入。
- 0.24m孤立台阶表达接近正式上限的Step准入。
- 0.40m孤立障碍表达超过`MaximumStepHeight`的拒绝。

该课程的真实障碍Collider进入Collision Artifact，且不伪装成连续楼梯。这样Ramp方案不会删除已经实现的KCC能力，也不会继续让普通楼梯承担算法测试职责。

### Tradeoff

- 分离课程：楼梯观感目标和KCC能力边界都可被明确解释；代价是Gameplay Lab增加一组灰盒障碍。
- 删除Step课程：场景更简单，但完成的Step Motor失去正式内容覆盖，不采用。
- 保留一条逐级楼梯作为隐含测试：会让“连续楼梯统一使用Ramp”的产品口径出现例外，不采用。

## Decision 6: Bake与发布保持显式

实施完成后，作者必须通过现有Unity菜单显式生成唯一Deterministic Collision Artifact。Bake读取持久化Ramp与普通Gameplay Surface，生成新的CollisionWorldHash。Local Fixed与Rollback Variant继续引用同一Asset，不复制第二份Artifact。

既有Network Product因CollisionWorldHash变化而失效，这是正式身份保护。产品重新发布仍由用户明确触发现有workflow；本change不在Inspector、场景保存、Play或代码编译时自动Bake或Build。

## Failure Policy

以下任一情况必须阻止楼梯作者校验或Collision Bake：

- Stair identity为空或重复。
- Ramp或Foot Surface引用缺失。
- Ramp被Foot Placement Mask包含。
- Foot Surface被Deterministic Surface作者拥有。
- Ramp未被Deterministic Surface作者拥有。
- Layer、Trigger、启用状态或Renderer角色错误。
- Ramp端点、宽度、方向或过渡连续性不合法。
- 同一Collider同时承担Ramp与Foot Surface职责。

系统不得跳过错误楼梯、自动改Layer、临时禁用Collider、回退逐级Gameplay碰撞或在Runtime修复。

## Migration

1. 增加正式Layer名称与楼梯作者绑定类型。
2. 将共享环境根上的宽泛Deterministic Surface所有权拆成明确Gameplay Surface子树。
3. 为三条往返路线的上行、下行梯段分别建立持久化Traversal Ramp作者绑定，并为每条路线建立Foot Surface根。
4. 将真实踏面迁移到`FootPlacementSurface`并移出Deterministic Surface作者子树。
5. 将Ramp迁移到`CharacterTraversal`并纳入唯一Deterministic Surface作者子树。
6. 建立独立Step Capability Course并迁移0.14m、0.24m、0.40m边界。
7. 更新Corin Foot Placement Mask与结构化诊断。
8. 通过显式命令重新Bake唯一Collision Artifact并更新文档身份。
9. 删除旧场景层级、旧连续楼梯逐级Gameplay Collider所有权和“不含隐藏坡道”口径。
