# Change: 新增角色表现 Blend Space

## Why

当前角色表现链已经能够把一份 `AnimationSelection` 交给 `SelectedPosePlayer` 或 `BlendStack`，并用显式节点完成跨来源过渡、惯性、骨骼分层、脚步修正和最终输出；但它还不能用移动速度、局部移动方向等连续参数，在多个动画样本之间计算同一时刻的姿势贡献。

因此，需要为后续具备完整方向移动素材的演示配置提供连续样本空间能力。当前 Corin 主配置仍使用 Idle、起步、循环、停步和转身等离散 producer；在素材未齐时强行把这些状态压进 Blend Space 会丢掉动作阶段。继续把参数插值问题塞进 `BlendStack` 也会混淆两件业务：

- Blend Space回答“当前参数位于样本空间的什么位置，各样本此刻各占多少”。
- Blend Stack回答“来源已经切换时，旧来源还要保留多久，何时准确释放”。

Unity 的 Animator Controller BlendTree不能直接复用：它会引入另一套状态、时钟、同步、权重和诊断权威，而且运行时算法不以项目可编译的纯数据合同公开。项目内 Animancer 8.2.2 已公开 `LinearMixerState`、`CartesianMixerState` 和 `DirectionalMixerState` 的权重行为，并且现有 `ManualMixerState` / `AnimationMixerPlayable` 已能按计划采样多个 clip。因此本change不重做骨骼混合，只新增项目自有、target-neutral、可编译和可诊断的参数空间权重求解层。

## What Changes

- 新增 `CharacterAnimationBlendSpaceAsset`，保存稳定资产身份、Rig、模式、轴、稳定 SampleId、AnimationClip、样本坐标、时间策略、同步标记绑定、Foot Analysis引用和显式Pose Parameter解析策略。
- 正式模式限定为 `Linear1D`、`FreeformCartesian2D` 与 `FreeformDirectional2D`。三者分别采用项目内Animancer对应Mixer的可见算法语义，但权重计算被抽成不拥有Playable、时钟或Unity对象的纯求解器。
- 新增显式 `BlendSpacePlayer` Pose Graph节点。节点消费一份BlendSpace类型的`AnimationSelection`、一到两个typed Parameter输入，输出普通Pose Value与typed discontinuity；它不拥有跨来源历史。
- `CharacterAnimationPresentationProfile`的producer source binding新增`BlendSpace`来源种类。Profile仍是producer到表现资源的唯一绑定入口，Pose Graph节点不再保存第二份Blend Space资产引用。
- Projection Compiler把Blend Space authoring编译为固定样本表、权重求解数据、canonical phase映射、Foot Analysis绑定、参数策略和有界workspace；Runtime不读取authoring asset、不动态构建三角关系、不搜索资源。
- `BlendSpacePlayer`内部只拥有同一Blend Space来源内的参数插值、canonical phase到各样本时间的映射、多clip加权采样和source-local feature聚合。跨来源Marker Sync仍由显式`MarkerSync`节点拥有，跨来源CrossFade仍由`BlendStack`拥有，单Pose跳变平滑仍由下游`Inertialization`拥有。
- 时间策略必须在资产上显式选择`SharedNormalizedPhase`或`MarkerSynchronizedPhase`，不得在标记缺失时自动退回normalized time。Marker模式使用稳定Phase Reference Sample和统一MarkerId拓扑，参数变化不会动态更换phase leader。
- Foot Analysis按每个真实样本clip生成和绑定；Runtime用与骨骼姿势相同的最终样本权重聚合左右脚feature与source contribution，之后仍由唯一FootPlacement节点消费。
- 在正式Character Animation Authoring Workspace中增加Blend Space资产模式：Navigator、参数空间视图、Details、Preview、编译诊断和引用关系使用现有workspace外壳，不创建旧Workbench或第二个编辑器体系。
- Pose Graph Details与Live Debug显示参数值、落点、有效样本、归一化权重、canonical phase、每样本有效时间、foot contribution和Projection revision；Preview与Runtime执行同一编译计划。
- Agent CharacterController Snapshot只读输出Blend Space资产identity/revision、模式、轴摘要、producer binding、BlendSpacePlayer节点和编译状态；Agent Patch与MCP action不获得Blend Space写入口。
- Corin 当前正式主配置保持`Selection -> MarkerSync -> SelectedPosePlayer -> Inertialization`，BaseLocomotion各producer分别绑定自己的Timeline source。Blend Space只在素材齐备后由独立演示Definition、Profile和Pose Graph使用；不得把演示配置混入Corin主图，也不得同时保留Timeline与BlendSpace双写。

## Capabilities

### Added

- `character-animation-blend-space`：定义Blend Space资产、权重算法、编译、运行时、同步、feature聚合、编辑器、预览与诊断合同。

### Modified

- `character-presentation-pose-graph`：加入显式BlendSpacePlayer节点、typed参数端口和固定执行阶段。
- `character-animation-presentation-authoring`：加入producer到BlendSpace资源的唯一正式绑定和workspace入口。
- `character-animation-foot-analysis-artifact`：把BlendSpace sample加入正式分析源与Projection绑定。
- `agent-character-controller-synthesis`：扩展只读Presentation Snapshot，不扩展Patch写能力。

## Current Spec Comparison

- current specs中不存在Blend Space能力；当前`AnimationPoseSourceKind`只有Timeline与MotionMatching，不能表达稳定BlendSpace source identity。
- current `character-animation-presentation-authoring`规定Profile是表现资源唯一入口，但目前只绑定Timeline/Animancer transition或Motion Matching。本change沿用这个入口并新增正式source kind，不在节点、Timeline或Agent Patch中复制资产引用。
- current `character-animation-foot-analysis-artifact`按Timeline/Track/Clip绑定分析产物。本change为BlendSpaceAsset/SampleId/Clip增加并列正式source identity；同一个BlendSpace sample不得同时从Timeline与BlendSpace读取两份feature。
- current `agent-character-controller-synthesis`已规定Pose Graph、Blend Library、Rig和Presentation binding只读。本change只扩展只读摘要，继续禁止Presentation mutation。
- active `add-character-presentation-pose-graph`的最终节点目录尚不含BlendSpacePlayer；本change在该显式拓扑上增加一个正式Player，不回到隐藏per-slot装配。
- active `refactor-animation-selection-pose-graph-boundary`规定只有Player可以把Selection降低为Pose。本change把BlendSpacePlayer加入该有限Player目录，并保持Selection不携带最终样本权重。
- active `refactor-animation-playback-to-blend-stack`规定BlendStack独占多source过渡。本change不把参数空间插值塞入BlendStack，也不让BlendSpacePlayer保留旧来源。
- active `upgrade-character-animation-authoring-workspace`明确禁止显示尚未安装的Blend Space入口。本change完成后才安装该入口，并沿用其Navigator、Canvas、Details、Bottom Dock、Pose Watch与显式Compile边界。
- active `refactor-timeline-animation-authoring-boundary`把Timeline和独立领域工具分开。本change使用独立Blend Space资产模式，不把二维样本空间伪装成Timeline lane。

## Dependencies And Sequencing

1. `refactor-animation-selection-pose-graph-boundary`先固定Selection、MarkerSync和Player边界。
2. `add-character-presentation-pose-graph`与`upgrade-character-animation-authoring-workspace`先提供最终节点编译合同和正式workspace外壳。
3. `refactor-animation-playback-to-blend-stack`与`refactor-inertial-blending-to-local-pose-node`先保证跨来源过渡和单Pose平滑不再隐藏在采样器中。
4. 本change在上述目标合同上实施；不得先向旧PoseSlot Stack或旧Workbench安装临时Blend Space路径。
5. 当前先收口Corin纯Timeline主图。后续独立Blend Space演示只在完整样本、Projection、Profile、Pose Graph和全部可达producer能一次配置时创建；不得把未完成样本或混合source kind接进Corin主图。

## Deliberate Scope

- 不嵌入Unity AnimatorController、BlendTree或AnimatorControllerPlayable。
- 不把Animancer MixerState作为权重、时钟或同步权威；只复用其可见算法语义和现有采样后端。
- 不提供`Direct`模式。显式权重组合已经由BlendPose、LayeredBoneBlend和AdditivePose表达，新增Direct会形成重复权威。
- 不提供`SimpleDirectional2D`、嵌套Blend Space、任意子图或动态运行时样本增删。当前正式目录只覆盖项目移动业务需要且能与Animancer公开算法逐一对应的三种模式。
- 不在Blend Space里执行Gameplay状态选择、Motion Matching查询、Root Motion移动决策、跨来源CrossFade、惯性、FootPlacement或最终Animator输出。
- 不增加Agent Patch operation、MCP action、任意SerializedProperty写入或第二个Presentation authoring服务。

## Breaking Changes

- Pose Graph node kind、Projection schema、Presentation ContractHash和ProjectionRevision提升；旧generated Projection直接失效并重建。
- 任何进入BlendSpacePlayer的可达Selection endpoint都必须解析为正式BlendSpace source binding，并拥有一致的轴接口与Rig；Timeline或MotionMatching来源混入时编译失败。
- MarkerSynchronizedPhase缺少Phase Reference、统一MarkerId拓扑或样本绑定时编译失败，不回退到normalized time。
- 缺失SampleId、重复坐标、非法方向、退化求解区域、Foot Analysis artifact或Pose Parameter policy时编译失败，不跳过样本、不补默认样本。
- Corin主图中已产生的临时Blend Space资产、速度轴节点、参数声明和producer binding直接删除；重新发布generated Projection，不保留兼容reader、双写或fallback。

## Success Criteria

- 作者能在正式workspace创建、编辑、编译和预览三种正式Blend Space资产，并在Pose Graph中通过显式BlendSpacePlayer使用它们。
- 参数落点在Preview与Runtime得到相同的稳定SampleId集合、归一化权重、canonical phase、样本时间、Pose Parameter和Foot feature贡献。
- BlendSpacePlayer、BlendStack、MarkerSync、Inertialization和FootPlacement各自只有一项清晰职责，代码链中没有第二个权重、过渡、同步或IK权威。
- Corin正式Profile、Projection和Pose Graph只走纯Timeline状态链；后续Blend Space演示拥有独立Definition、Profile和Pose Graph，不污染当前双端帧同步验证配置。
- Agent Snapshot能解释Blend Space配置和诊断，但Agent Patch/MCP仍不能修改Presentation资产。
