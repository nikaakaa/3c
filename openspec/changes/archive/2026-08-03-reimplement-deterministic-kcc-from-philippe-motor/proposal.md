# Change: 以 Philippe KCC 语义重构 Deterministic KCC

## Why

当前 Fixed KCC 已经进入 `GameplayTickSystem -> SimulationSessionHost -> Fixed Pipeline -> DeterministicKccWorldSolver -> DeterministicKccMotor` 正式运行链，但已接入的台阶实现没有按用户要求以 Unity 生态中 Philippe St-Amand 的 Kinematic Character Controller 为主要依据。现实现重新设计了 blocker-linked outer/inner capsule landing、primitive adjacency 和近乎零高度差约束；Gameplay Lab 中高度为 0.14m 与 0.24m 的合法楼梯都会在 `InnerLanding` 阶段被拒绝，增大 `MinimumStepDepth` 只会把拒绝原因从 `InnerLandingUnrelated` 切换为 `MinimumStepDepthAbsent`。

这不是资产参数微调问题，而是 movement policy 与主要参考实现的查询类型、分支顺序和身份语义不一致。Philippe KCC 的台阶处理以 `KinematicCharacterMotor` 的 Hit Stability、Step Detection、Step Commit、Ground Probe、Ledge/Denivelation 和 remaining movement 循环共同构成；不能只摘取“outer/inner”名词后重新发明一套独立 Step Solver。

本 change 原位重构唯一 Fixed KCC Motor 的运动策略。角色运动语义以本地固定版本 Philippe KCC 为主基准；现有 Fixed Q32.32 查询、collision artifact、rollback state 与 batch ownership 继续作为确定性承载。此前未验收的旧 Step 求解设计与任务由本 change 整体取代，不归档为已完成能力，也不保留为第二实现路径。

## What Changes

- 将 Philippe KCC `KinematicCharacterMotor` 的 movement policy 设为唯一主要行为基准，锁定本地参考包版本、源文件哈希和允许移植的边界。
- 把 Philippe 的 `CharacterCollisionsSweep`、`CharacterCollisionsRaycast` 与 `CharacterCollisionsOverlap` 分别映射到 Fixed capsule cast、Fixed raycast 与 Fixed capsule overlap；禁止用第二次 capsule landing 代替 reference 中的 inner/outer ray probe。
- 在 Fixed Motor 内建立与 `HitStabilityReport` 对应的 typed report，明确 stable normal、inner/outer normal、valid step、stepped surface、ledge side、distance 与 movement direction。
- 按 Philippe 的顺序实现 Step Detection：当前非稳定 contact、outer capsule down sweep、无 overlap、outer stable ray、实际上抬 clearance、inner stable ray；锁定 Extra stepping 作为唯一正式模式，并只在 Standard 检查失败时使用 `MinimumRequiredStepDepth` 分支。
- 按 Philippe 的顺序实现 Step Commit：只接受近似垂直 obstruction；从 safe position 沿 obstruction 内侧前移固定 stepping distance并从 `MaximumStepHeight` 向下 capsule sweep；只选择与 `SteppedSurfaceId` 相同的 landing；提交落点后保留 remaining movement进入同一 collide-and-slide 循环。
- 删除当前 `DeterministicKccStepSolver`、`DeterministicKccStepGeometry`、`DeterministicKccStepSupportEvaluator`、outer/inner capsule landing identity、primitive adjacency准入和独立 Step Down事务；不保留开关、adapter或兼容诊断。
- 按 Philippe 的 Ground Probe 与 `SnappingPrevented` 语义统一普通贴地、下坡和下台阶；下降不再由独立 Step Down算法处理，ledge/denivelation判定负责阻止跨悬崖或不合法落差吸地。
- 扩展会影响下一 Tick 分支的 Fixed KCC state：previous grounding、inner/outer normal、snapping prevented、ledge state与last movement found ground；继续不复制 Program拥有的垂直速度。
- 将 reference 常量、正式 KCC配置、state codec、Motor/Solver semantic version、KccId 与 world configuration hash 同批升级。
- 清理 Gameplay Lab 低楼梯与 12°坡道的碰撞重叠；正式楼梯继续由项目自有 primitive 场景表达，不把未跟踪 Philippe样例或OpenKCC runtime变成产品依赖。
- 更新 current spec、`project.md`、第三方说明和产品闭包清单，但只在代码与资产实现完成后把目标能力写成已安装事实。

## Primary Reference

主要参考是本地未跟踪目录中的 Philippe KCC 派生包：

- package：`com.janooba.kcc` `1.0.1`
- 原作者：Philippe St-Amand
- 本地维护来源：Gawidev / Janooba refactor
- 主要源文件：`ExternalDownloads/PhilippeKccReference/Package/Core/Runtime/KinematicCharacterMotor.cs`
- `KinematicCharacterMotor.cs` SHA-256：`D7FEE8FA2D703A273DFF0CF67A64FF88A65531309A23429CC1A6BBF587440476`
- `package.json` SHA-256：`29752D03559951B9241EE9C900C092B281362A527D2D44FA9403E1375CB3A74F`

“主要基于”表示 movement policy、查询种类、分支次序、候选选择、状态字段和剩余运动处理逐项映射；不表示把 UnityEngine、Unity Physics、MonoBehaviour、Collider、Rigidbody 或第三方 assembly引入 portable Fixed Runtime。

## Non-Goals

- 不引入 Philippe KCC runtime assembly、Unity Physics 查询或 `KinematicCharacterMotor` MonoBehaviour。
- 不逐字复制 Asset Store源码；正式代码使用项目命名、Fixed类型和collision artifact重新表达同一行为语义。
- 不实现 moving platform、PhysicsMover、动态 Rigidbody 推动、任意重力方向或旋转胶囊。
- 不修改 Program 对 `VerticalVelocity`、重力、跳跃、攻击位移和 MotionRequest 的所有权。
- 不增加 Standard/Extra运行时开关；唯一正式 Fixed行为锁定为 Standard-first、Extra-second。
- 不恢复旧 `TryStep`，不保留当前 outer/inner capsule算法，不增加OpenKCC运行时路径。
- 不修改 DeterministicRollback网络协议、Session composition或Actor batch ownership。
- 不新增自动化测试；用户继续负责Unity端到端验收，手动验证不写入 `tasks.md`。

## Dependencies

- 依赖当前唯一 `DeterministicKccWorldSolver`、`DeterministicKccMotor`、Fixed collision artifact v2 与 `DeterministicCapsuleQueries`。
- 依赖 `character-vertical-body-motion` 已安装的垂直速度所有权，KCC不得建立第二份速度状态。
- 依赖 `close-deterministic-rollback-character-pipeline` 继续只消费既有正式KCC身份；本 change 完成 identity升级后，该产品闭包必须重新打开受影响的显式Prepare/Build任务。
- 本地 Philippe参考目录不是Git输入、Unity package依赖或Player构建输入；若文件哈希不匹配，实施必须停止并先更新本 change 的参考版本。

## Current Spec Comparison

- current `deterministic-kcc-world-solver` 把台阶准入写成 outer/inner capsule landing、同 primitive或triangle adjacency与严格高度一致；这与 Philippe的 capsule sweep、ray probe和同 Collider身份不一致，本 change修改该 requirement。
- current spec把下楼梯写成独立 Step Down candidate，并把Ground Snap限制为微小距离；Philippe通过 previous grounding、扩展Ground Probe、SnappingPrevented和ledge/denivelation统一处理，本 change修改该 requirement。
- current Grounding只快照稳定support primitive/feature/normal，缺少下一 Tick snap与denivelation所需的inner/outer normal、snapping prevented和last movement found ground，本 change修改state requirement。
- current第三方边界只称Philippe KCC为行为比较来源，没有规定它是movement policy主要基准，也没有锁定本地版本与哈希，本 change修改该 requirement。
- `project.md` 当前描述的是已安装但未通过楼梯验收的自研Step Solver；在代码完成前必须继续如实记录该失败状态，不能提前宣称Philippe语义已安装。
- OpenKCC只继续提供静态课程覆盖参考；其runtime不是本 change 的运动算法来源。

## Impact

- Runtime：`Simulation/DeterministicKcc/Kcc` 的Motor、grounding、step contracts、state codec与diagnostics；`Collision` 增加canonical Fixed raycast。
- Unity authoring：KCC Solver Definition、正式 `CorinDeterministicKcc.asset` 字段与identity破坏性升级。
- Collision artifact：继续使用SurfaceId、PrimitiveId、FeatureId与adjacency，但把SurfaceId正式收敛为单个作者Collider身份；`SteppedCollider`语义映射为`SurfaceId`，同一MeshCollider或TerrainCollider的Primitive共享身份，不新增Unity对象身份。
- Scene：修正Gameplay Lab低楼梯与坡道重叠，并重新生成唯一正式collision artifact。
- Rollback：KCC state schema、snapshot/hash、endpoint identity与既有产品manifest失效，必须显式重新发布。
- 文档：删除两个未验收旧change，新增本change作为唯一KCC实施入口；实现完成后再更新current spec和项目Current State。
