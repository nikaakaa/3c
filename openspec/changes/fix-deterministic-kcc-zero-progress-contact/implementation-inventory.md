# 实现清单：Deterministic KCC 零进展接触修复

## 故障证据

- 复现位置约为`(-17.62, 0.10, 13.49)`。
- 旧Artifact中的`Primitive 812 / Surface 95`对应`RoughTile_05_07`，`Primitive 705 / Surface 86`对应`RoughTile_04_07`。
- 两个旋转Box在Z方向约穿插`6.5cm`，并被分别降低为封闭三角形。
- 查询连续返回相同内部边`TOI=0`，旧投影的remaining before/after完全相同，最终耗尽八轮movement iteration。
- `primitive=-1 / capacity=0`是iteration non-convergence外层失败信息，不是candidate、contact或pair容量溢出。
- 故障调用链只经过Fixed Collision Query、Motor与World Solver；Foot Placement、LegIK、Animation和Presentation没有进入该事务。

## Runtime唯一链路

| 输入或阶段 | 当前实现 | 输出 |
|---|---|---|
| canonical movement contacts | `DeterministicKccMotor.AddConstraintPlane` | 预分配的完整active plane集合 |
| remaining movement | `ProjectRemaining` | 原始位移、全部单平面、全部非平行双平面交线与零向量中的唯一Fixed可行候选 |
| 等距候选 | `IsConstraintCandidateBefore` | candidate kind、plane index与raw vector显式稳定裁决 |
| 连续零进展 | `SaveZeroProgressSignature`与`MatchesZeroProgressSignature` | 第二轮完全相同事实终止为`BlockedNoProgress` |
| 成功终止 | `DeterministicKccMotorResult` | 最后safe position、已完成位移、Ground Probe与最终validation结果 |
| transient诊断 | `PublishNoProgressDiagnostics` | 确认轮数及Surface/Primitive/Feature/normal raw，不进入Snapshot或StateHash |

Runtime继续只依赖portable Core与Fixed数值模块。没有新增Unity Physics、Float32、CharacterController、第二Motor或fallback。

## Philippe来源对账

- 继续使用已锁定的`com.janooba.kcc 1.0.1`，`KinematicCharacterMotor.cs` SHA-256为`D7FEE8FA2D703A273DFF0CF67A64FF88A65531309A23429CC1A6BBF587440476`。
- Reference `KinematicCharacterMotor.cs:1588-1762`的remaining movement分支仍是“一面保留切向、两面限制到crease、约束封闭则停止”的主要行为来源。
- Fixed实现把reference对少数平面的顺序分支推广为同一三维active-constraint问题，使三个以上canonical plane也按同一行为边界求解；这不是第二套movement policy。
- `BlockedNoProgress`是项目Fixed query在量化内部边上重复返回相同`TOI=0`时的确定收敛出口。它只在reference行为已经无法产生任何新位置、contact或remaining解时终止，不覆盖Step、Ground Probe、Hit Stability或普通slide。

## Baker唯一链路

```text
DeterministicCollisionWorldAuthoring
-> StairTraversalSurfaceValidator
-> 稳定Collider source records
-> walkable Box竞争支撑面校验
-> 原有Box/Mesh/Terrain canonical lowering
-> CorinDeterministicCollisionWorld.asset
```

竞争支撑面校验按稳定pair执行：双方必须均为walkable Box、量化局部Y支撑轴不平行、上表面XZ投影正面积交叠，并通过Fixed 15轴OBB SAT证明超一个quantization cell的正体积穿插。平行支撑实体与Ramp/Top合法边界不误报。失败发生在Artifact替换前，诊断聚合全部非法pair。

## 场景作者迁移

- 108个`RoughTile_*`保留Transform、MeshFilter与MeshRenderer，全部删除BoxCollider。
- `CharacterMovementRoughGroundCollision.obj`包含130个共享顶点与216个顶面三角形，不包含Tile侧面或底面。
- `RoughGroundCollision`是Ground层、无Renderer、非Trigger MeshCollider，由现有`Zone_02_RoughGround`唯一Surface owner拥有。
- 旧`CourseBase`保留视觉组件并删除BoxCollider。
- `CharacterMovementCourseGroundCollision.obj`包含16个顶点与16个顶面三角形，在粗糙区域精确开孔。
- `CourseGroundCollision`是Ground层、无Renderer、非Trigger MeshCollider，由现有`GroundAndRoutes`唯一Surface owner拥有。
- 粗糙Mesh外围顶点全部落在`y=0`，与Course Ground只共享同一孔洞边界。
- LowStairs的31个Gameplay/Foot后代从错误局部`x=-6`归一为`x=0`，因此统一服从课程根`x=12`。
- Gentle Ramp与Top平台整体移到`x=-4.5`；Steep Ramp与Top平台整体移到`x=-10`。
- `Vault_H0.90_Yaw15`从`z=2`移到`z=-1`，移出OverLimit上行Ramp。
- 以上位置修改均作用于Renderer与Collider共同所在的现有Transform，没有碰撞专用副本。

## 发布身份

- Motor semantic：`fixed-philippe-kcc-motor/9`。
- World Solver version：`10`。
- KCC identity schema：`deterministic-kcc/8`。
- Configuration schema保持`deterministic-kcc-configuration/7`。
- Collision Artifact schema保持`deterministic-collision-world/3`。
- CollisionWorldHash：`19d2b81df6204842d2d45b0517125d2d96bc9ff6f9423c5205492d33f98e7097`。
- Collision Artifact文件SHA-256：`1C12B33D90704FE122FD75B1B817ECFFACD5FCE4BAAE693FBEB019DAD6BB7AC6`。
- Gameplay Lab Local Fixed与DeterministicRollback Variant继续引用同一个`CorinDeterministicKcc.asset`和`CorinDeterministicCollisionWorld.asset`；KccId由新Motor/Solver schema、正式配置、TickRate与新CollisionWorldHash共同计算，不保存第二份序列化身份。

## Artifact对账

- `RoughGroundCollision`身份出现1次。
- `CourseGroundCollision`身份出现1次。
- `RoughTile_`碰撞身份出现0次。
- `CourseBase`碰撞身份出现0次。
- Ramp、真实Step、Slope、Vault和其它正式Surface继续保留。
