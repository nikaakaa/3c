# 实施清单

## 旧 Layer 身份与调用链

- Timeline authoring 的正式字段已经由 `AnimationTrack.AnimationChannelId` 承担，Timeline copy/paste继续复制稳定channel引用。
- Semantic IR、Float32/Fixed Program producer、Presentation command、queue、Lifecycle、Marker Sync、Preview、Trace、Agent Snapshot与Projection合同均已使用`AnimationChannelId`。
- `Assets/GameScripts/Main`中的CSharp代码已不存在`LayerId`和`CharacterAnimationLayerDefinition`。
- 旧serialized数据仍只存在于Corin资产：`CorinPlayableRootTree.asset`、`CorinAttack1Timeline.asset`、旧`CorinAnimationPresentationProfile.asset`与旧generated Projection。

## Profile、Projection与生成资产

- `CharacterAnimationPresentationProfile`正式引用Pose Graph、Blend Library与Rig Definition；旧Layer类型源码已删除。
- target-neutral Projection边界继续保持`Semantic IR -> Presentation Contract -> Projection`与Numeric Target独立分支；Float32、Fixed和Remote adapter均复用同一contract builder。
- 当前待删除旧资产数据包括Profile中的`m_AnimancerLayerIndex`、`m_TransitionLibrary`，generated Projection中的`m_LayerId`、旧transition/layer payload，以及已删除的Corin Transition Library资产引用。
- `refactor-presentation-projection-target-boundary`已经归档；本change只在其target-neutral边界上升级Animation Channel、Pose Slot、Rig、Blend Stack与Pose Program payload，不修改归档历史。

## Animancer、Blend Stack与最终Pose

- 项目主代码已不存在`AnimancerLayer.Play`、`StartFade`、`FadeGroup`、Animancer layer weight写入或TransitionLibrary查询。
- `AnimancerPoseSamplingBackend`只管理完整`AnimationPoseSourceId`对应的source playable、采样时间、ManualMixer child weight与寿命。
- `AnimationBlendStackRuntime`已经收窄为每Pose Slot的时间混合，并通过native source workspace、双页frame plan与`AnimationSlotBlendJob`发布slot结果。
- `AnimationPosePlayableGraphRuntime`在同一PlayableGraph中按source capture、全部slot job、Pose Graph native job和唯一final writer安装固定拓扑。
- 旧managed `AnimationSlotBlendPoseEvaluator`、`CharacterPoseGraphEvaluator`、`AnimancerPlaybackAdapter`和旧Presentation runtime合同文件已经删除。

## Foot Placement

- Foot Placement正式输入已经是`FinalAnimationPoseFrame`。
- 左右脚feature来自Pose Graph最终输出；Pose Graph为Invalid或NoPose时Foot Placement走正式reset/不可用路径。
- 旧Animancer state weight、Layer scalar或单slot scalar不再作为最终脚贡献事实。

## BTSMTL Graph Editor

- 现有窗口生命周期、breadcrumb和runtime diagnostics集中在`BaseTreeWindow`。
- GraphView画布、selection、搜索、创建、clipboard与连接协调集中在`BaseTreeView`及其Node/Port/Edge View。
- Undo、dirty owner与窗口选择协调分布在`TreeWindowUtility`和`TreeDesignerUtility`。
- Inspector宿主为`BaseTreeInspectorView`，搜索入口为`NodeSearchWindow`，Data Catalog为`GraphDataCatalog`。
- BTSMTL领域特判仍存在于`BaseTreeView`、`BaseNodeView`、`BaseEdgeView`和Input Action drag factory，包括BaseNode subtype、ConditionRule、BTAbortPolicy、PropertyPort与InputAction规则。
- 当前尚不存在`GraphAuthoringEditorShell`及其六个domain adapter接口，因此Graph Shell章节仍未完成。

## Corin正式迁移清单

- 逻辑通道目标：`BaseLocomotion`与`FullBodyAction`。
- 表现入口目标：`BaseLocomotionSlot/RequireOutput`与`FullBodyActionSlot/AllowEmpty`。
- Locomotion producer：Idle、WalkStart、WalkLoop、RunStart、RunLoop、RunEnd、MovingTurn。
- FullBody producer：Attack1至Attack5、Dodge及其它明确全身Action。
- WalkEnd保持无producer，不创建Timeline或默认Idle。
- 必须通过正式Agent authoring工具迁移RootTree和Timeline；不得直接编辑Unity YAML。
- Pose Graph、Blend Library、Rig、Profile与producer source binding不属于Agent Patch写入口，必须通过各自正式authoring入口和显式Build请求发布。

## 并行change边界

- `refactor-animation-playback-to-blend-stack`拥有per-slot Stack、Stored Pose、Inertial、native slot job与source retirement；本change拥有跨slot Pose Graph、final pose与Post Process接缝。
- `add-character-motion-matching-pose-source`只在Resolved Pose Request之前提供producer source，不得建立私有fade、第二Stack或第二Pose输出。
- Timeline Animation Analysis、Marker Sync、Foot Analysis与target-neutral Projection现有字段均视为已安装合同，本change不得回退。
- 待删除清单固定为旧Layer serialized数据、TransitionLibrary、Animancer fade/layer权威、global compositor、旧snapshot字段、旧generated Projection payload及兼容字段。
