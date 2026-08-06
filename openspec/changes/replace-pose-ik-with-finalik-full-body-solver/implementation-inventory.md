# FinalIK 实施门禁与源码清单

## 唯一正式链

本 change 只允许以下正式链：同一份 Component Pose 分别只读供给 `PredictiveFootPlacement` 与 `PoseBoneIKGoals`；前者用 FinalIK Grounding 当前脚数学、项目 Future Landing 扩展和逐腿 Pelvis Reach Planner 生成 Body/Foot Goals，后者生成 Hand Goals；唯一 `FullBodyIK` 一次消费原始 Component Pose 与全部 Goal Sets，并通过 FinalIK FBBIK 写入 Pending Dense Pose。两个 Goal Source 只是值生产者，不是串行 IK。

正式运行时禁止 shadow skeleton、FinalIK target GameObject、`FullBodyBipedIK`/`GrounderFBBIK` Prefab 组件、`Update`/`LateUpdate`/`OnAnimatorIK` IK、TwoBoneIK/LegIK fallback、重复 Grounding 权威和中间 Physical Transform 写入。全部 stage 成功后仍只经过唯一 Physical Transform final writer。

任一条无法满足时，停止实施，不增加兼容或第二路径。

## FBBIK 参与源码与基线身份

基线文件按仓库相对路径排序后，以 UTF-8 路径、分隔符和各文件 SHA-256 组成 backend source identity。实施前组合身份为 `addc899055f231bc1ba7cccd2103aaf81be34492b5b62d67993749c34dc8724a`。正式身份必须在 backend seam 完成后重新计算并写入 Projection 依赖，不能继续使用基线身份。

backend seam 完成后，FBBIK、Grounding与两个新增backend文件按同一规则重新计算出的正式组合身份为 `7cd67a8e9ca9e22b68e466f60bf27aa29ea653cf3edc619566b0ac6d41ee3cb1`。`CharacterFullBodyIkProfile`与`CharacterFinalIkPoseBufferBackend`均只接受该正式身份。

| 文件 | 基线 SHA-256 | 允许改造边界 |
| --- | --- | --- |
| `IK Solvers/IKSolver.cs` | `c7e7adb4bc3969c08425cc9fad13b18198fe8d226688674ddc07f9602afcec9f` | 初始化、Point/Node identity 与 Pose I/O |
| `IK Solvers/IKSolverFullBody.cs` | `d41975361120b5d5d859af5c6f4b5359587d71a9da42cb6d02c6d4683148b01d` | indexed backend 选择、ReadPose/WritePose 调度 |
| `IK Solvers/IKSolverFullBodyBiped.cs` | `0a108e20c0af551727feb0e4bc8e6fee8f067b4c5fc352f05ad195c404577305` | biped references 初始化、biped mapping I/O |
| `IK Solvers/FBIKChain.cs` | `dcf93fccb6b06fe931e89525007143ccf8215052563e205e1da13794997eccd0` | chain/node 初始化与 ReadPose 输入 |
| `IK Solvers/IKEffector.cs` | `2614c113a0c47d90931f1e98565999889b1a0dfe3d9203071e2ab3b685df38e1` | bone/child/plane identity 与目标输入 |
| `IK Solvers/IKConstraintBend.cs` | `f58398329bf06d88d0ec5090ef669574807e54a6fb8c135f18a5e69a5bbeac0e` | 三骨 identity 与 reference bend 初始化 |
| `IK Solvers/IKMapping.cs` | `eb6bdf3b21f600bd0fd25ece4278a77416396f91a1636463426d523612ba4fa5` | BoneMap identity、Pose 读写与 parent-space 换算 |
| `IK Solvers/IKMappingSpine.cs` | `579735067ad9b1c59f3a7a761cf4d0017d51e0800be837787032862f38faed36` | spine mapping 初始化、ReadPose、WritePose |
| `IK Solvers/IKMappingLimb.cs` | `37302eee32405cdff0277c1be2423d2aa1dcc4ca056126501eec201f908d520f` | limb mapping 初始化、ReadPose、WritePose |
| `IK Solvers/IKMappingBone.cs` | `7882a87c4d74ba144d262b3bd3ed99f5de9ffad7b76124ff69fdb89ba4a5801f` | single-bone mapping 初始化、ReadPose、WritePose |

### Transform 依赖位置

- `IKSolverFullBodyBiped`：`rootNode`、`SetToReferences`、spine/limb/effector数组构造、`ReadPose`、`WritePose`和 clavicle/reference 辅助方法。
- `IKSolverFullBody`：Transform 只用于按骨查找 chain/effector/point，以及间接调度 mapping。核心 `Solve` 只操作 solver node 数值。
- `FBIKChain`：ChildConstraint 的两个骨、node 数组构造、初始化长度、`ReadPose` 当前位置和 bend 三骨解析。`Push`、`Reach`、`Stage1`、`Stage2`与约束求解只操作 solver 数值。
- `IKEffector`：effector bone、可选 target、child bones、三个 plane bones；初始化解析 node，预解读取目标或 Pose。
- `IKConstraintBend`：bone1/2/3 与可选 bend goal；初始化解析 node 和 reference bend，求解阶段操作 solver positions。
- `IKMappingSpine`：spine、upper arm、thigh Transform 只用于 mapping 初始化、读取动画 Pose、写回求解 Pose。
- `IKMappingLimb`：parent、bone1/2/3 Transform 只用于 mapping 初始化、读取动画 Pose、写回求解 Pose。
- `IKMappingBone`与`BoneMap`：Transform identity、默认局部状态、长度/轴/平面初始化、Pose读写和 parent-space 换算。

### 禁止改写的数学

backend seam 不修改 `FBIKChain.Push`、`Reach`、`Stage1`、`Stage2`、FABRIK iteration、trigonometric pass、`IKEffector.Update`的权重语义和 `IKConstraintBend`的 bend 约束数学。允许把这些数学读取的骨身份与 Pose 数值改由 indexed backend 提供，但不复制方程到项目 solver。

indexed backend 在 Actor preparation 一次创建 solver、chain、mapping、handle 数组和固定 workspace；正常帧只重绑预分配 Pose page，不创建 solver、GameObject、Transform、managed 集合，也不写中间 Physical Transform。插件原有 Transform 调用链继续服务 FinalIK 自带示例，不作为项目 fallback。

## Grounding 参与源码与基线身份

| 文件 | 基线 SHA-256 | 允许改造边界 |
| --- | --- | --- |
| `Grounder/Grounding.cs` | `a7e2eef8269bce7a1aeb25fddd6a77146ec0d2edd5c5392d42178e0888b998f7` | 显式帧输入、query backend、vendor Transform adapter |
| `Grounder/GroundingLeg.cs` | `bf9336d353b1c74b9f1dc66635f1845eb5b568e1d88c66d06938cd5efd02e6a7` | 脚输入与 typed query request |
| `Grounder/GroundingPelvis.cs` | `da066cea8229170cbcd8e9243d82429dddecb232b3201f7c1c18b4560dfc140b` | root/pelvis 显式输入与帧时间 |
| `Grounder/GrounderFBBIK.cs` | `5a6e2522c44abfea6fcec22db0330c436458fe6da3783b0bfdff4660c99fdea7` | 只保留 vendor pelvis-before-effectors 顺序 |

### 当前输入和查询

- Transform：`Grounding.root`、每条 `Leg.transform`、pelvis root；用于 root up/right、脚位置/旋转、pelvis位置与 root local-space 速度。
- 时间：`Grounding.Leg`和`Grounding.Pelvis`直接读取 `Time.time`、`Time.deltaTime`。
- 查询：`Grounding.Raycast`、`SphereCast`、`CapsuleCast` delegates 默认绑定 Unity `Physics` 静态入口。
- Fastest：heel 单 ray；Simple：heel、toe、side rays；Best：heel ray 加 foot capsule。

正式 seam 把 root、heel、toe、ankle、foot component transform、frame time/delta 和 stable foot slot 放入显式 frame input；把 ray/sphere/capsule 参数放入 typed request；项目 adapter 使用精确 `PhysicsScene`、自碰撞排除和预分配 fixed hit page，返回命中点、法线、距离与稳定 surface identity。FinalIK 自带 Grounder 仍通过 vendor adapter 使用 Transform、Time和默认 Physics。

### 保留的成熟数学

- velocity prediction：当前脚位移除以 delta time，再按 `prediction` 权重偏移下一次脚查询。
- hit/plane 到脚高：`SetFootToPoint`、`SetFootToPlane`及 heel/toe/side/capsule 组合保持原式。
- 坡面旋转：命中法线、foot rotation offset、`maxFootRotationAngle`限制保持原式。
- foot interpolation：速度、加速度、平滑、抬起/降低与 IK position/rotation 插值保持原式。
- pelvis：stock最低/最高腿offset、lower/lift、speed与damper仅保留为vendor审计范围，正式adapter权重固定为零，不发布stock pelvis结果。
- `GrounderFBBIK`现有顺序只作为依据：先应用 pelvis，再写 effectors。

项目 Predictive Extension只补FinalIK Grounding没有的动画Foot Feature/source contribution、相位Future Landing、Current/Future Support、Ground Envelope、surface identity/moving anchor、Free/Locked/Sliding生命周期和逐腿Pelvis Reach Planner。Planner只消费最终Foot Goals、Rig腿长与extension ratio并发布一个pelvis pre-solve Goal；当前脚grounding与未来预测在同一`PredictiveFootPlacement`内汇合，不存在两份结果择优。

## Corin 完整 biped reference 门禁

Corin `corin.animation-rig` 当前 physical catalog 已包含以下唯一候选，足以迁移为 Rig v4 显式 binding：

| 语义 | Physical dense index |
| --- | --- |
| Solver Root / Pelvis | 2 |
| Spine | 22, 23, 24 |
| Left Clavicle / Arm | 25 / 26, 27, 30 |
| Right Clavicle / Arm | 110 / 111, 112, 115 |
| Left Leg | 3, 4, 6, 7 |
| Right Leg | 12, 13, 15, 16 |
| Neck / Head | 49 / 50 |

这些候选具有正确父子链。Rig v4 Build 仍必须从 reference pose 验证有限正 segment length 与左右肘、膝非退化 reference bend plane；失败时拒绝 Build，不能用世界前方、角色前方、旧 calibration preferred direction 或上一帧方向补值。

## 能力边界

FinalIK FBBIK没有 UE PBIK 完整逐骨 Bone Settings：没有逐骨 position/rotation stiffness、任意 XYZ rotation limit、Preferred Angle、Excluded Bone、Stretch 或 Root Behavior。`CharacterFullBodyIkProfile`只能暴露 FinalIK 实际拥有的 iterations、FABRIK pass、spine stiffness、body pull、chain pin/pull/push/push-parent/reach、limb mapping、maintain rotation、bend weight/clamp和全局 node weight。需要 UE PBIK 独有行为时必须单独报告能力缺口，不能伪造字段。
