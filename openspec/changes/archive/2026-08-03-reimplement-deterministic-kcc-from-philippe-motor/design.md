## Context

当前 Fixed KCC 的查询内核已经具备 upright capsule 对 Plane、Box和one-sided indexed Triangle的Fixed cast、overlap、penetration、canonical contact与Surface/Primitive/Feature identity。失败集中在查询之上的movement policy：现实现把Philippe KCC中的不同查询错误统一成outer/inner capsule landing，并额外要求landing primitive相同或相邻、两次landing高度差不超过两倍`QueryTolerance`。

在正式配置下，outer probe只从safe position向障碍内侧进入`SkinWidth + QueryTolerance = 0.00501m`，inner再进入`MinimumStepDepth = 0.01m`。合法台阶的inner capsule probe仍得到不稳定鼻部接触；增大深度后，两次capsule contact高度又无法满足`0.00002m`高度差。因此同一个参数无法同时满足两项准入。

Philippe KCC并没有使用这套合同。它把以下职责放在同一个Motor行为中：

```text
Ground Probe / Snap
    -> EvaluateHitStability
        -> Inner/Outer Ray Probes
        -> Ledge / Denivelation
        -> DetectSteps
    -> Movement Sweep Loop
        -> Step Commit or Velocity Projection
        -> Continue Remaining Movement
```

本设计以该行为顺序为主基准，只替换Unity Physics与float数值承载。

## Goals

- 正常作者楼梯首先表现为可走地形，不再被自研几何身份规则误拒绝。
- Fixed Motor在相同输入、配置、collision artifact和previous state下保持确定性。
- Philippe reference使用哪种查询，Fixed实现就提供对应查询，不用近似查询替代语义。
- Step、Grounding、Ledge、Denivelation、Snap和remaining movement属于同一Motor策略，不再拆成彼此矛盾的模块。
- 最终只有一个KCC配置、一个Motor、一个query pipeline、一个state codec与一个identity。

## Reference Hierarchy

当参考之间存在冲突时，按以下顺序决定：

1. `KinematicCharacterMotor.cs`锁定版本的movement policy、分支顺序和状态语义。
2. 本项目Fixed numeric、canonical query、collision artifact、rollback state与batch transaction硬约束。
3. Rapier/Parry与PhysX只用于底层shape query、penetration和数值稳健性，不得改写角色movement policy。
4. OpenKCC只用于静态测试课程覆盖，不作为runtime算法来源。

本地参考必须同时匹配package版本与两个SHA-256。哈希变化不是自动升级信号，必须先重新对账本设计。

## Semantic Mapping

| Philippe KCC | Fixed KCC正式映射 | 明确不采用 |
|---|---|---|
| `CharacterCollisionsSweep` | `DeterministicCapsuleQueries.Cast`及canonical同TOI contact set | Unity `Physics.CapsuleCast` |
| `CharacterCollisionsRaycast` | 新增portable Fixed raycast，输出Surface/Primitive/Feature、distance、point、normal | 用capsule cast假装ray probe |
| `CharacterCollisionsOverlap` | `DeterministicCapsuleQueries.Overlap` | Unity overlap或终点猜测 |
| `Collider` identity | collision artifact `SurfaceId` | 强制相同PrimitiveId或triangle adjacency |
| `HitStabilityReport` | Fixed typed stability report | 只返回一个`IsStable`布尔值 |
| `CharacterGroundingReport` | Fixed ground report与snapshot必要子集 | 仅保存grounded和单一normal |
| `BaseVelocity` | Program/Fixed Character State唯一垂直与运动状态 | KCC复制第二份速度 |
| `StepHandling.Extra` | 唯一Standard-first、Extra-second策略 | runtime mode开关 |
| `AllowSteppingWithoutStableGrounding` | 固定为false | 空中自动登台阶 |
| Rigidbody/PhysicsMover | 不在本change范围 | 隐式Unity对象或非快照动态世界 |

## Final Architecture

```text
DeterministicKccWorldSolver.ResolveBatch
    -> DeterministicKccMotor.Move
        -> ResolvePenetration
        -> ProbeGround(previous grounding policy)
            -> EvaluateHitStability
                -> Fixed inner/outer ray probes
                -> ledge/denivelation report
                -> DetectStep when unstable
        -> Project requested displacement on stable ground
        -> Continuous movement sweep loop
            -> EvaluateHitStability
            -> TryCommitStep on valid step + vertical obstruction
            -> otherwise multi-plane projection
            -> continue remaining movement
        -> Final ground report / snap policy
        -> Final overlap validation
    -> DeterministicActorContactSolver
    -> same Motor static reconstraint
    -> atomic batch commit
```

Step不是第二个WorldSolver，也不是脱离Motor的几何搜索服务。实现可以保留小型内部纯函数文件，但不能保留拥有另一套candidate语义的`DeterministicKccStepSolver`对象。

## Decisions

### 1. 主要移植行为，不复制Unity运行时

正式实现逐项映射Philippe的movement branch、查询种类、候选选择和状态转换，但使用项目类型与命名重新表达。UnityEngine、Collider、Rigidbody、MonoBehaviour、callback lifecycle与float不进入Fixed程序集。

业务收益：角色手感和常见边界站在成熟Unity KCC经验上，同时保持rollback确定性与普通.NET可运行。

代价：不能机械复制单个方法；每个Unity Physics调用都必须有语义等价的Fixed查询，并显式处理identity和tie-break。

### 2. Sweep、Raycast与Overlap必须一一对应

Philippe Step检测同时使用capsule sweep、point raycast和capsule overlap。Fixed query pipeline必须补齐raycast：

- Plane使用解析ray/plane交点。
- Box使用slab raycast并输出canonical face/edge tie-break。
- one-sided Triangle使用Fixed ray/triangle相交，服从artifact winding。
- 同距离hit按`SurfaceId -> PrimitiveId -> FeatureId -> point`排序。
- 所有距离、平行、边界和退化容差进入query identity。

业务收益：台阶鼻部由细射线检查顶部内外法线，不再让0.35m胶囊的圆角接触替代踏面证据。

代价：query层增加一种正式查询及容量；但它同时服务ground edge、step和ledge，不建立第二world表示。

### 3. `HitStabilityReport`成为唯一中间事实

每个ground hit或movement hit先生成：

```text
IsStable
FoundInnerNormal / InnerNormal
FoundOuterNormal / OuterNormal
ValidStepDetected / SteppedSurfaceId
LedgeDetected
IsOnEmptySideOfLedge
DistanceFromLedge
IsMovingTowardsEmptySideOfLedge
LedgeGroundNormal
```

base稳定性先由坡度与Surface准入决定；inner/outer ray probe再形成ledge信息；ledge/denivelation特殊规则可以取消stable；只有最终仍不stable时才执行step detection。Step detection成功可以把本次hit视为stable obstruction信息，但不会提前移动角色。

业务收益：Grounding、Step和Ledge读取同一份证据，不再各自重新查询并得出不同结论。

代价：报告字段和快照字段增多；只有影响下一Tick分支的数据进入state，临时候选与诊断不进入snapshot。

### 4. Step Detection按Philippe顺序移植

Standard检查：

1. 从当前hit与角色位置计算`stepCheckStartPosition`，位置位于`MaximumStepHeight`上方并沿障碍内侧偏移固定collision offset。
2. 以完整角色胶囊向下cast，收集`MaximumStepHeight + CollisionOffset`内hits。
3. 按向下距离最远优先、canonical identity次序检查候选。
4. 候选角色位置使用`castDistance * TimeOfImpact - CollisionOffset`，再执行capsule overlap，存在重叠即拒绝该hit。
5. 从候选世界hit point向外侧执行短偏移向下raycast，法线必须稳定。
6. 从当前角色位置向上capsule cast本次真实rise，验证头顶净空。
7. 在角色中心对应高度向下raycast；若未得到稳定inner normal，再从hit point沿内侧短偏移向下raycast。
8. inner稳定后记录候选`SurfaceId`为`SteppedSurfaceId`。

Extra检查只在Standard没有候选时执行：从角色中心按`MinimumRequiredStepDepth`进入障碍内侧，在`MaximumStepHeight`上方向下capsule cast，再复用同一个`CheckStepValidity`。`MinimumRequiredStepDepth`不再被解释成两个capsule landing必须同高的距离。

业务收益：普通宽楼梯走Standard路径，窄但具有最小踏面深度的合法台阶才使用Extra路径。

代价：一次不稳定hit可能产生多次查询；全部查询使用预分配buffer与固定budget，超出即明确失败。

### 5. Step Commit按Stepped Surface提交并保留remaining

只有movement sweep的hit满足以下条件才提交Step：

- stability report已发现valid step；
- obstruction normal与Up近似垂直；
- 角色此前稳定grounded；
- 当前请求没有明确向上脱离意图。

Motor先按Philippe `GetObstructionNormal`处理当前hit：previous state稳定且hit normal本身不是稳定地面时，用previous ground normal与Up重算水平障碍法线。垂直障碍判断、Step前进方向与普通constraint plane必须共用该法线，不能读取Step Detection把最终`IsStable`提升后的结果冒充base stability。

Motor从safe position沿有效obstruction内侧前移固定`SteppingForwardDistance`，再上移`MaximumStepHeight`作为向下capsule cast起点。向下hits中只接受`SurfaceId == SteppedSurfaceId`的canonical候选，最终位置使用`MaximumStepHeight * TimeOfImpact - CollisionOffset`。提交后把运动方向投影到水平面，remaining magnitude继续进入同一movement loop；不得清零，也不得另做完整forward事务。

业务收益：角色上一级后仍能处理第二级台阶、墙体与内角，且MeshCollider的多个triangle可以表达同一个可踩Collider表面。

代价：行为会忠于参考中的固定前探距离，而不是按当前自研candidate消费模型；该常量必须进入KccId。

### 6. 下台阶由Ground Probe与Denivelation统一处理

不保留独立Step Down。Ground Probe距离按Philippe语义选择：

- 没有previous stable或last movement ground证据时，只使用`MinimumGroundProbingDistance`。
- previous ground允许snap且previous stable或last movement found ground时，使用`max(Radius, MaximumStepHeight) + GroundDetectionExtraDistance`。
- ground sweep命中非稳定面时，按固定rebound budget沿命中面调整方向继续探测。
- stable hit仍需经过ledge distance、朝空侧运动和denivelation角规则；不满足时设置`SnappingPrevented`并不移动到落点。

项目的明确向上MotionRequest等价于本Tick禁止ground snap，不复制Philippe Controller层的`ForceUnground`计时器。

业务收益：下坡、下楼梯和悬崖共享成熟的一套grounding依据，不会出现Ground Snap和Step Down互相争夺最终位置。

代价：删除当前独立`SteppedDown` candidate诊断；若业务仍需要“发生了一级下台阶”事件，只能由最终ground delta派生只读诊断，不能反向控制求解。

### 7. Snapshot保存reference分支真正读取的前态

`DeterministicKccBodyState`扩展保存：

- previous grounded与found any ground；
- support Surface/Primitive/Feature identity；
- ground、inner与outer normal；
- snapping prevented；
- ledge state；
- last movement iteration found any ground。

Position和Program拥有的`VerticalVelocity`继续由现有World/Character State保存，不在KCC state重复。临时ray hits、step candidates、contact buffers和iteration diagnostics不进snapshot。

Fixed Body Motion Integrator仍然先产生包含重力的完整XYZ位移。previous state稳定且本Tick没有明确向上意图时，Motor在movement sweep前把平面请求重定向到previous ground tangent，并把负Y分量约束为零；角色离地后不执行这条地面约束，Ground Probe与Finalize继续决定position、Grounded与VerticalVelocity。该约束是碰撞响应，不是Motor私有重力积分。

业务收益：restore/replay后会走同一ground snap、denivelation和step资格分支。

代价：state codec与旧snapshot破坏性不兼容，必须升级identity并重新发布产品。

### 8. 正式配置按reference语义改名

保留并重新解释：

- `Radius`、`Height`
- `MaximumStepHeight`
- `MinimumGroundNormalY`
- query与容量配置

删除：

- `GroundSnapDistance`
- 当前错误语义的`MinimumStepDepth`
- outer/inner landing专用容差或算法version

新增并纳入identity：

- `CollisionOffset`
- `GroundDetectionExtraDistance`
- `GroundProbeReboundDistance`
- `MinimumGroundProbingDistance`
- `SecondaryProbeVerticalDistance`
- `SecondaryProbeHorizontalDistance`
- `SteppingForwardDistance`
- `MinimumRequiredStepDepth`
- `MaximumStableDistanceFromLedge`
- `MaximumStableDenivelationAngle`
- `VerticalObstructionCorrelation`

正式行为固定为Step Extra、禁止无稳定ground上台阶、启用ledge/denivelation，不增加serialized mode开关。

### 9. SurfaceId承担Philippe Collider identity

Philippe用`SteppedCollider`保证Detection与Commit属于同一个Collider。Fixed artifact中的对应身份是`SurfaceId`：baker为每个作者Collider生成一个稳定`SurfaceId`，同一MeshCollider或TerrainCollider的全部triangle共享该身份。PrimitiveId与FeatureId只用于canonical contact、最终support和诊断，不再作为Step跨阶段唯一归属。

movement blocker不要求与`SteppedSurfaceId`相同；`CheckStepValidity`可以从down sweep选择另一个Collider作为合法踏面，最终Commit只要求重新命中该`SteppedSurfaceId`。这对应reference把`SteppedCollider`写成validity candidate collider，而不是初始`hitCollider`。

业务收益：Box、MeshCollider和Terrain lowering都可以在不保留Unity对象的前提下表达来源表面。

代价：baker必须保证同一作者Collider稳定产生同一SurfaceId；跨Surface拼接即使几何相邻也不会被当成同一个step candidate。

### 10. 场景与算法分别收口

Gameplay Lab低楼梯与12°坡道必须拆开，保证一条测试路线只表达一种几何意图。0.14m与0.24m楼梯继续使用项目自有Box作者数据，顶平台Collider从最后一级踏面末端开始，不得与多个末级Box重叠；0.40m楼梯表达超过正式`MaximumStepHeight`的拒绝边界。OpenKCC prefab中缺失Mesh/Collider的样例不得作为KCC已工作的证据，也不得用隐藏ramp补楼梯。

业务收益：失败可以归因于算法或场景，而不是两套碰撞几何叠在一起。

代价：需要重建正式collision artifact并升级WorldHash；旧rollback产品随之失效。

### 11. 第三方边界

本地Philippe参考是用户合法持有且未跟踪的参考副本，不进入Git、Unity manifest、asmdef或Player。项目文档记录版本、作者、来源与哈希；正式实现采用项目结构和命名逐分支重写。OpenKCC notice继续只覆盖静态测试课程。

若未来决定直接分发第三方源码或assembly，必须单独确认许可与发布边界；本change不授权该动作。

## Failure Policy

- reference文件版本或哈希不匹配：停止实施，不猜测新版本语义。
- Fixed raycast无法为Plane、Box或Triangle提供canonical hit：停止实施，不以capsule probe替代。
- query/contact容量不足或iteration不收敛：当前ResolveBatch失败，不跳过probe。
- Step Detection任一阶段失败：保留safe position，用原movement contact执行普通multi-plane projection。
- Step Commit找不到相同SurfaceId landing：不提交任何step位置。
- Ground Probe被ledge/denivelation禁止：保持movement结果并报告Airborne/非稳定，不执行独立Step Down。
- identity或state schema不匹配：Session准备失败，不兼容读取旧snapshot、replay或产品manifest。

## Migration

1. 锁定reference manifest与Fixed策略常量。
2. 为collision query补齐canonical Fixed raycast。
3. 建立Fixed stability、grounding、step和ledge报告合同。
4. 在Motor内实现Philippe顺序的EvaluateHitStability与DetectStep。
5. 在movement loop内实现Step Commit与remaining continuation。
6. 用Ground Probe/SnappingPrevented替换独立Step Down与微距Ground Snap分裂路径。
7. 扩展body state、snapshot codec与hash。
8. 迁移Unity Solver Definition和正式KCC asset。
9. 删除当前Step Solver模块、旧字段、旧诊断与兼容路径。
10. 清理测试场景重叠并重建唯一collision artifact。
11. 升级Motor、Solver、KccId与WorldHash，重新打开受影响的产品发布任务。
12. 实现完成后更新current spec、`project.md`与第三方说明为已安装事实。

迁移最终结果必须一次收敛；中途不得提交可运行的双算法选择。

## Alternatives And Tradeoffs

### 继续修补当前outer/inner capsule算法

优点：改动文件少。

代价：继续偏离主要参考的查询类型和分支语义；调深度只能在不稳定inner contact与严格高度差之间切换失败，无法给后续Grounding、Ledge和下降建立统一依据。该方案不采用。

### 改用OpenKCC SnapUp作为主要算法

优点：源码公开，算法较短。

代价：不是用户指定的Unity生态主要KCC，也不具备Philippe同一套Hit Stability、Ledge/Denivelation和Ground Probe关系。OpenKCC继续只做测试课程参考，该方案不采用。

### 直接引用Philippe Unity runtime

优点：浮点Unity场景中最接近原始行为。

代价：Unity Physics、float、MonoBehaviour与Collider状态不能进入portable Fixed rollback内核，且会形成第二WorldSolver与第二状态真相。该方案不采用。
