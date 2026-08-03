# Implementation Inventory

## 唯一运行链

- `AnimationSelectedPosePlayerRuntime`只采样当前source，并发布与同一Player completion绑定的`PoseDiscontinuity`；连续sample不创建新事件。
- `PoseInertializationNativeProgram`与`CharacterPoseGraphNativeJob`是唯一history、TRS/velocity residual、Quaternion Log/Exp、curve derivative、dense profile、parameter filter、Foot Feature envelope与Accumulator实现。
- `AnimationBlendStackRuntime`只保留多source entry、CrossFade、Stored Pose、Per-Bone Blend Profile与exact release；`AnimationBlendTechnique`及Stack内全部Inertial字段已删除。
- Timeline Preview与MM Query Fixture都构造正式`CharacterAnimationPlaybackRuntime`并执行Projection中的同一`CharacterPresentationPosePlan`。

## Pose Discontinuity与Reset入口

- `PoseDiscontinuity`使用`pose-discontinuity/v1`，包含稳定EventIdentity、Previous/Current Endpoint、Previous/Current ContinuityIdentity、typed reason与ResetSequence，不携带duration、curve、weight或旧Pose。
- Selection source变化与MM generation jump由SelectedPosePlayer发布普通Discontinuity。
- Timeline连续sample保持source continuity；`PreviewPlaybackEngine.RetireAndReset`使用`PreviewSeek`。
- Presentation Reset与branch replacement分别通过`PresentationReset`和`BranchReplacement`清理正式Pose runtime。

## Authoring、Compiler与加载校验

- `CharacterPoseNodeKind.Inertialization`只有一个Pose输入和一个Pose输出，只允许直接连接`SelectedPosePlayer`。
- `CharacterPoseInertializationPolicy`使用`character-pose-inertialization-policy/v2`，定义HardCut/Inertialize、canonical curve、dense per-bone profile和完整Pose Parameter filter。
- `CharacterPresentationInertializationPlanCompiler`枚举直接Player全部可达endpoint，生成完整N×N exact rule table并拒绝duplicate、orphan与缺失配置。
- `CharacterPresentationPosePlan.RequireValid`校验Pose value生产顺序和Selection/native/world-aware/final phase单向依赖；`RequireInertializationValid`校验operation/descriptor/player identity、完整endpoint矩阵与参数过滤。
- Projection build和Runtime Native Program构造都会执行Inertialization专用校验，不提供fallback或旧Projection转换。

## Runtime、生命周期与诊断

- 每节点预分配双页completed output history、dense local TRS、真实Presentation delta、linear/angular/scale velocity、parameter与左右脚feature。
- 首次Pose只建立history；合法Discontinuity从上一completed output捕获；连续中断从上一修正输出原子rebase并提升唯一Accumulator generation。
- Reset、NoPose与Invalid清理history/clock/residual；Accumulator不持有source，不伪装成producer、clip或Gameplay contact。
- Snapshot发布PoseNodeId、InputPlayerNodeId、policy identity、exact rule identity、Discontinuity、ResetSequence、Capture/Continue/Rebase/Complete/Reset状态、TRS residual与per-bone envelope；Inspector只读正式snapshot。

## Corin正式资产

- Profile：`Assets/Configs/Character/Corin/Pipeline/Definition/CorinAnimationPresentationProfile.asset`。
- Pose Graph：`Assets/Configs/Character/Corin/Pipeline/Presentation/CorinPresentationPoseGraph.asset`。
- Locomotion Policy：`Assets/Configs/Character/Corin/Pipeline/Presentation/CorinLocomotionInertializationPolicy.asset`。
- Locomotion Blend Profile：`Assets/Configs/Character/Corin/Pipeline/Presentation/CorinLocomotionInertialBlendProfile.asset`。
- BaseLocomotion：`MarkerSync -> SelectedPosePlayer -> Inertialization`；FullBodyAction：独立`BlendStack`；两者经LayeredBoneBlend后进入唯一FootPlacement与OutputPose。
- 14个动画producer严格分为7个BaseLocomotion和7个FullBodyAction；Locomotion descriptor生成49条exact rule，每条包含完整2项Pose Parameter filter。
- Rig共201骨骼；左右脚分别绑定`animation-bone/Bip001/Bip001_Pelvis/Bip001_L_Thigh/Bip001_L_Calf/Bip001_L_Foot`与`animation-bone/Bip001/Bip001_Pelvis/Bip001_R_Thigh/Bip001_R_Calf/Bip001_R_Foot`。
- 当前Pose Plan hash为`7cc5f01233906af6bdfe058e2c61739b63db52e7ae3bfc17e16b8ac5d4a5d6bc`，Projection revision为`846f8d0ccdab23ea19ce9a02712c6e16ab7cb55ec1372148f78a8b318c420ba6`，Fixed program hash为`aa8068d033b1ec6be4bf80e283277a79429268daa441c4f151b6b6cca0fad0ec`。

## 验证事实

- `ThirdPersonClient.Runtime.csproj`：0 errors。
- `ThirdPersonClient.Editor.csproj`：0 errors。
- Unity强制刷新与脚本重载完成，Console为0 errors。
- 正式Presentation工具完成Corin inspect，并通过重建链发布Float32 Projection与Fixed wrapper；重建触发Unity domain reconnect后，生成资产可重新加载且Console无错误。
