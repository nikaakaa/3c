# Corin Presentation 身份对账

对账日期：2026-08-03

## 结论

Definition、Presentation Profile、Rig v3、Sampling Rig、Calibration、Foot Analysis、Pose Graph与generated Presentation Projection使用同一组显式身份。未发现名称推断、默认Rig、旧Rig revision或第二份腿链。

## 身份链

| 边界 | 身份 | 对账结果 |
|---|---|---|
| Rig Definition | `character-animation-rig/v3` / `corin.animation-rig` / `5f8593de08c64ed0b88dfab51d4cbb72` | Calibration、Pose Plan和所有内嵌Pose Source payload使用同一RigId与revision |
| Rig资产GUID | `6f792f666e4405d4b813f79b53714aea` | Presentation Profile与Foot Analysis Source直接引用该GUID |
| Sampling Rig GUID | `846373e25bc0479cb06bd295c0103817` | Analysis Source的`m_SamplingRigAssetGuid`与Prefab meta一致 |
| Calibration | `Corin.FootPlacementRig` / schema `3` / `9faf04328b19cd32e16350f1def5e99a031c0fc9c7a502f5540fec26e4ebdda9` | Calibration资产、Projection Foot Analysis和FootPlacement operation payload一致 |
| Calibration资产GUID | `471a3432ddd640c187b4ebfdf8c94e69` | Analysis Source与Projection operation直接引用该GUID |
| Foot Analysis | `Corin.FootPlacementAnalysis` / `animation-foot-analysis/v6` | Presentation Profile binding与Projection Foot Analysis一致 |
| Foot Analysis artifact | `01d1ad39a33b5cd2391aab08f4a744bdfba867e8c543a2e3c614045f2a624588` | Projection保存明确artifact content hash，不接受无身份结果 |
| Pose Graph | `ed8ff472330e4057a900af3eae5dfb8f` / `4b13c14457214e17b47a7ad0466cdb6c` | 作者Pose Graph与generated Pose Plan identity/revision一致 |
| Pose Plan | schema `character-presentation-pose-plan/v17` / runtime ABI `character-presentation-pose-runtime/v20` | Rig revision一致，`PoseBoneCount = 203`，空间转换、双臂IK、FootPlacement与Output处于唯一链路 |
| Projection | ABI `character-presentation-projection/v9` / revision `13f73b9db4cfcdb3a7a60d4e6b5e02f489bbb75b01d39e33ec222fbb45e8753c` | Definition直接引用该generated Projection |

## Rig v3语义链

- Pelvis：`animation-bone/Bip001/Bip001_Pelvis`
- Left：`Bip001_L_Thigh -> Bip001_L_Calf -> Bip001_L_Foot -> Bip001_L_Toe0`
- Right：`Bip001_R_Thigh -> Bip001_R_Calf -> Bip001_R_Foot -> Bip001_R_Toe0`

这九个语义槽全部解析为Rig v3 Physical Bone；Calibration不保存第二份Transform映射，Sampling Rig只通过精确Physical BoneId绑定。

## Pose拓扑

作者图与generated Pose Plan共同表达：

```text
PoseStateMachine
  -> Inertialization
  -> AnimationSlot
  -> PoseParameterResolve
  -> LocalToComponentPose
  -> Left Arm TwoBoneIK
  -> Right Arm TwoBoneIK
  -> FootPlacement
  -> ComponentToLocalPose
  -> OutputPose
```

对账只证明已发布资产身份一致；FootPlacement同帧world query与staged executor完成状态以`tasks.md`为准。
