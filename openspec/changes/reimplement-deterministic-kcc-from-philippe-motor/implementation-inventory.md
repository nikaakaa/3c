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
- Gameplay Lab作者环境已把LowStairs与GentleRamp_12deg分离，保留0.14m与0.24m楼梯尺寸，并把LowStairs与HighStairs顶平台Collider起点接到最后一级末端，删除会让down sweep提前选中另一Collider的末级重叠。独立`OverLimitStairs_Rise0.40_Run0.48`路线位于`x=24`，使用项目自有Box Collider表达连续0.40m rise的上行、平台与下行拒绝边界，不含隐藏坡道或第三方Prefab。

## 当前闭包状态

- 唯一`CorinDeterministicCollisionWorld.asset`已于2026-08-03 10:29:44通过Unity Editor正式菜单重新Bake；当前`CollisionWorldHash`为`74151f2e4e54794af94b505e6af3377aa95bceab71a52593a18bdcee65a56436`，资产文件SHA-256为`4c79a5aa6e0a132c8a494acd0915b3756426f6d940d5547b447ae1fcefb3b691`。该v3 artifact包含0.14m、0.24m与0.40m三条独立楼梯路线，并保存逐Collider Surface identity。
- 工作区只保留这一份collision artifact，Local Fixed与Rollback Variant继续引用同一KCC配置和collision identity，不存在旧artifact reader或镜像。
- 使用正式artifact、真实每Tick重力位移与同一Fixed Motor重放时，0.14m路线到达`Y=1.69`、0.24m路线到达`Y=1.93`，0.40m超限路线保持`Y=0.01`且不提交Step。
- 资产、Bake与portable Motor闭包已完成；Local Fixed Play目前先被既有Foot Placement Rig Calibration异常阻断Actor注册，必须在独立Presentation链路修复后再按Local Fixed、Rollback Relay与Peer A/B顺序端到端确认，KCC不增加绕过路径。
- 本change不归档。
