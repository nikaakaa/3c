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

## 规格状态

- current specs已经统一为AnimationChannel、PoseSlot、Blend Library、Pose Graph与FinalAnimationPoseFrame口径。
- 旧LayerId、TransitionLibrary、Animancer fade、Current/Outgoing和Equipment Required Layer只允许出现在明确禁止恢复旧路径的语境中。
- `character-equipment-presentation`已明确RequiredProducerIds只校验Gameplay route完整性，不声明AnimationChannel、PoseSlot或动画空间拓扑。
- `btsmtl-node-interruption-lifecycle`已明确Program只按AnimationChannelId输出唯一producer command，通用Tree scheduler不产生表现生命周期。

## BTSMTL Graph Editor

- `GraphAuthoringEditorShell`及`IGraphAuthoringDocument`、`IGraphAuthoringNodeCatalog`、`IGraphAuthoringPortPolicy`、`IGraphAuthoringMutationAdapter`、`IGraphAuthoringInspectorAdapter`、`IGraphAuthoringDiagnosticsAdapter`已经安装。
- `BaseTreeWindow`已经继承`GraphAuthoringEditorShell`并通过BTSMTL adapters复用窗口、GraphView、selection、搜索、clipboard、Undo、Inspector与diagnostics宿主。
- `CharacterPresentationPoseGraphEditorWindow`已经继承同一Shell，并使用独立Pose document、node catalog、port policy、mutation、Inspector和diagnostics adapters；Pose数据不继承BTSMTL runtime node/edge语义。
- 跨domain clipboard由domain identity拒绝，Shell不保存第二份node/edge集合。
- Pose diagnostics当前只运行authoring validator并显示Projection摘要，明确显示`Live Snapshot Unavailable`；正式runtime snapshot source-map绑定仍未完成。

## Corin正式迁移清单

- 逻辑通道目标：`BaseLocomotion`与`FullBodyAction`。
- 表现入口目标：`BaseLocomotionSlot/RequireOutput`与`FullBodyActionSlot/AllowEmpty`。
- Locomotion producer：Idle、WalkStart、WalkLoop、RunStart、RunLoop、RunEnd、MovingTurn。
- FullBody producer：Attack1至Attack5、Dodge及其它明确全身Action。
- WalkEnd保持无producer，不创建Timeline或默认Idle。
- 必须通过正式Agent authoring工具迁移RootTree和Timeline；不得直接编辑Unity YAML。
- Pose Graph、Blend Library、Rig、Profile与producer source binding不属于Agent Patch写入口，必须通过各自正式authoring入口和显式Build请求发布。
- 当前`CorinPlayableRootTree.asset`与`CorinAttack1Timeline.asset`仍保存`LayerId: Base`；`CorinAnimationPresentationProfile.asset`仍保存Layer catalog、Animancer layer index和TransitionLibrary；旧generated Projection仍保存Layer与transition payload。

## 并行change边界

- 当前工作区已经把CrossFade/Stored/source release收口到显式BlendStack，把history/residual/rebase收口到SelectedPosePlayer后的局部Inertialization，并由完整Pose Plan拥有composition、world-aware FootPlacement与final publication；不存在per-slot隐藏Stack或Stack Inertial双写。
- `add-character-motion-matching-pose-source`只在Resolved Pose Request之前提供producer source，不得建立私有fade、第二Stack或第二Pose输出。
- Timeline Animation Analysis、Marker Sync、Foot Analysis与target-neutral Projection现有字段均视为已安装合同，本change不得回退。
- 待删除清单固定为旧Layer serialized数据、TransitionLibrary、Animancer fade/layer权威、global compositor、旧snapshot字段、旧generated Projection payload及兼容字段。
