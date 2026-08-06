# 实施清单

## 参考锁定

- 本地参考package：`com.janooba.kcc` `1.0.1`。
- 原作者：Philippe St-Amand；本地package标记Gawidev/Janooba维护。
- `KinematicCharacterMotor.cs` SHA-256：`D7FEE8FA2D703A273DFF0CF67A64FF88A65531309A23429CC1A6BBF587440476`。
- `package.json` SHA-256：`29752D03559951B9241EE9C900C092B281362A527D2D44FA9403E1375CB3A74F`。
- 正式Runtime、asmdef、Unity manifest与Player闭包不引用`com.janooba.kcc`、`KinematicCharacterMotor`或第三方KCC assembly。

## Philippe方法到Fixed实现映射

| Philippe方法/阶段 | 锁定reference行 | Fixed实现 | 语义边界 |
|---|---:|---|---|
| `CharacterCollisionsSweep` | 1464、1501、2033、2047、2096 | `DeterministicCapsuleQueries.Cast/CastAll` | movement、Step Commit、Standard、Extra与upward clearance分别保留原查询种类 |
| `CharacterGroundSweep` / `ProbeGround` | 940、1294、2414 | `DeterministicKccMotor.ProbeGround` + capsule `Cast` | short/extended probe、最多3轮rebound与沿面投影 |
| `CharacterCollisionsRaycast` | 1968、1978、2088、2110、2122 | `DeterministicCapsuleQueries.Raycast` | inner/outer ledge probe与Step inner/outer probe不再用capsule landing近似 |
| `CharacterCollisionsOverlap` | 874、1106、1213、1431、2084 | `DeterministicCapsuleQueries.Overlap` | Fixed只迁移初始去穿透、movement pose与Step candidate验证；Rigidbody/callback用途排除 |
| `EvaluateHitStability` | 1299、1486、1938 | `DeterministicKccMotor.EvaluateHitStability` | movement hit与ground hit共用唯一报告 |
| `GetObstructionNormal` | 1528、1584 | `DeterministicKccMotor.GetObstructionNormal` | previous stable且hit本身不稳定时，用previous ground normal与Up重算水平障碍法线 |
| `DetectSteps` | 2013、2024 | `DeterministicKccMotor.TryDetectStep` | Standard-first，失败后才进入Extra |
| `CheckStepValidity` | 2037、2051、2058 | `DeterministicKccMotor.CheckStepValidity` | farthest-first、canonical tie、扣除CollisionOffset、overlap、outer ray、clearance、inner ray |
| movement step commit | 1493–1518 | `DeterministicKccMotor.Move` + `TryCommitStep` | 有效障碍法线、previous stable、无向上意图、Detection与Commit相同SurfaceId、扣除CollisionOffset、remaining继续同一loop |
| movement projection | 1588–1762 | `ProjectRemaining`与constraint planes | 一面切向、两面交线、三面封闭，Step不建立第二Motor |
| Grounding前态 | 931–942、1258–1269 | `DeterministicKccBodyState` + codec v3 | FoundAnyGround、Stable、support、Ground/Inner/Outer normal、SnappingPrevented、ledge与LastMovementGround进入snapshot/hash |

## 上一帧Grounding读取点

- 931–932：`SnappingPrevented`、`IsStableOnGround`与`LastMovementIterationFoundAnyGround`选择短或扩展Ground Probe。
- 942：previous unstable到current stable的landing分支；Fixed KCC不拥有velocity，因此只保留ground结果，不复制速度事务。
- 1258–1269：previous `FoundAnyGround`与`InnerGroundNormal`参与denivelation稳定性。
- 1388–1409、1528–1600：current stable ground参与初始overlap、障碍法线和remaining投影；Fixed实现读取上一Tick support normal重定向平面请求、约束stable ground上的负Y已积分位移，并用`GetObstructionNormal`处理非稳定hit。
- 729–751：reference motor state capture/restore；Fixed对应全部未来分支状态进入`DeterministicKccStateCodec`，但不复制Position或VerticalVelocity。

## 明确排除的reference分支

- Rigidbody、InteractiveRigidbody、质量、冲量、弹性、推动与velocity transfer不进入本change；Actor碰撞继续由既有Fixed `SolidBodyBlock` batch负责。
- PhysicsMover、moving platform、attached Rigidbody与任意动态world不进入capability。
- 任意`CharacterUp`、旋转胶囊与Quaternion运动不进入本change；正式角色保持Fixed upright Y轴胶囊。
- `BeforeCharacterUpdate`、`UpdateRotation`、`UpdateVelocity`、`ProcessHitStabilityReport`、`OnGroundHit`、`OnMovementHit`、`PostGroundingUpdate`、`AfterCharacterUpdate`与collision filter callback不进入Fixed Motor。
- KCC不进入动画、Presentation、Timeline、Action或GameplayEffect事务；它只约束Fixed Body Motion Integrator已经产生的XYZ位移并返回Gameplay body/ground/collision结果。

## 实施后事实

- Query semantic为`fixed-capsule-ray-conservative-cast/4`；Motor semantic为`fixed-philippe-kcc-motor/7`；WorldSolver version为`9`；KCC identity schema为`deterministic-kcc/7`；collision artifact schema为`deterministic-collision-world/3`；configuration schema为`deterministic-kcc-configuration/7`；state codec为v3。
- 正式配置只保留`CollisionOffset`，并安装Ground Detection、Ground Probe Rebound、Minimum Ground Probe、Secondary Probe、Step Forward、Minimum Required Step Depth、Ledge、Denivelation与Vertical Obstruction字段。`SkinWidth`、`GroundSnapDistance`与旧`MinimumStepDepth`已删除。
- 唯一Motor拥有penetration recovery、movement cast、multi-plane projection、Hit Stability、Standard/Extra Step Detection/Commit与Ground Probe；旧`DeterministicKccStepSolver`、`DeterministicKccStepGeometry`、`DeterministicKccStepSupportEvaluator`和独立Step Down合同已删除。
- Hit Stability明确区分base stability与最终`IsStable`，并输出inner/outer ray、ledge、denivelation、step与SnappingPrevented结果；`SnappingPrevented`只保留movement位置并强制业务`IsStableOnGround=false`，矛盾的stable snapshot/state会被拒绝；诊断不进入snapshot/hash。Step Detection提升后的`IsStable`不会覆盖`GetObstructionNormal`所需的base normal判断。
- Baker为每个作者Collider生成一个稳定`SurfaceId`；同一MeshCollider或TerrainCollider的全部Primitive共享该身份。Step只约束Detection选中的`SteppedSurfaceId`与Commit一致，不把movement blocker强制成同一SurfaceId。
- Gameplay Lab作者环境继续保留LowStairs、HighStairs与`OverLimitStairs_Rise0.40_Run0.48`的可见踏面尺寸，但连续路线的Gameplay真相已经由`separate-stair-gameplay-and-foot-surfaces`迁移为六条持久化Traversal Ramp：三条路线的上行与下行分别拥有稳定`StairTraversalSurfaceAuthoring`身份。真实踏面位于`FootPlacementSurface`且不进入Fixed Artifact，顶平台继续作为共享`Ground`。独立`StepCapabilityCourse`通过0.14m、0.24m与0.40m真实孤立障碍继续表达Step准入和拒绝边界，不使用Ramp或第三方Prefab。

## 当前闭包状态

- 唯一`CorinDeterministicCollisionWorld.asset`已于2026-08-03 13:30:42通过Unity Editor正式菜单重新Bake；当前`CollisionWorldHash`为`d2921f1e50a1a7722f8a9d8762eef8038f7f9cad66185ac32998788ed3c1d7c3`，资产文件SHA-256为`9b5e6350d0dc262e710afd4ce19f9806b5645dc88805970d28c1e4f0c78428b3`。该v3 artifact包含`low/high/over-limit-stairs-{ascent,descent}`六条Ramp和0.14m、0.24m、0.40m三个独立Step障碍，明确不包含连续路线的真实踏面Collider。
- 工作区只保留这一份collision artifact；Local Fixed与Rollback Variant以及唯一KCC配置都引用GUID `d0b6f91c641a9a94aaeabe7bb97263e6`，不存在旧artifact reader、hash快照或镜像。
- 旧artifact上0.14m路线`Y=1.69`、0.24m路线`Y=1.93`与0.40m路线`Y=0.01`的逐级重放只保留为历史追溯，不再证明当前连续楼梯表现。当前真实Step边界由独立`StepCapabilityCourse`表达，连续楼梯只证明Ramp Gameplay与真实Foot Surface的分离。
- portable Motor、正式作者资产与Collision Bake闭包已完成；本轮没有自动重建Character产品或发布Network Product。新Collision身份的端到端确认仍按Local Fixed、Rollback Relay与Peer A/B顺序由用户执行，KCC不增加Presentation绕过路径。
- 本change不归档。
