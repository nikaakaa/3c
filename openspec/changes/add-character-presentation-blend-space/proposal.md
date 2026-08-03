# Change: 新增角色表现 Blend Space

## Why

当前角色表现链已经能够由`CharacterPresentationFactFrame`驱动Locomotion PoseStateMachine，并用显式SequencePlayer、State transition、AnimationSlot、惯性、骨骼分层、脚步修正和最终输出形成唯一Pose；但它还不能在PoseState内部用移动速度、局部移动方向等连续参数，在多个动画样本之间计算同一时刻的姿势贡献。

因此，需要为后续具备完整方向移动素材的演示配置提供连续样本空间能力。当前Corin可以继续用Idle、起步、循环、停步和转身等明确SequencePlayer State；在素材未齐时强行把这些状态压进Blend Space会丢掉动作阶段。继续把参数插值问题塞进PoseState transition或`BlendStack`也会混淆两件业务：

- Blend Space回答“当前参数位于样本空间的什么位置，各样本此刻各占多少”。
- Blend Stack回答“来源已经切换时，旧来源还要保留多久，何时准确释放”。

Unity 的 Animator Controller BlendTree不能直接复用：它会引入另一套状态、时钟、同步、权重和诊断权威，而且运行时算法不以项目可编译的纯数据合同公开。项目内 Animancer 8.2.2 已公开 `LinearMixerState`、`CartesianMixerState` 和 `DirectionalMixerState` 的权重行为，并且现有 `ManualMixerState` / `AnimationMixerPlayable` 已能按计划采样多个 clip。因此本change不重做骨骼混合，只新增项目自有、target-neutral、可编译和可诊断的参数空间权重求解层。

## What Changes

- 新增 `CharacterAnimationBlendSpaceAsset`，保存稳定资产身份、Rig、模式、轴、稳定 SampleId、AnimationClip、样本坐标、时间策略、同步标记绑定、Foot Analysis引用和显式Pose Parameter解析策略。
- 正式模式限定为 `Linear1D`、`FreeformCartesian2D` 与 `FreeformDirectional2D`。三者分别采用项目内Animancer对应Mixer的可见算法语义，但权重计算被抽成不拥有Playable、时钟或Unity对象的纯求解器。
- 新增显式`BlendSpacePlayer` Pose Graph节点。节点只能位于PoseState inline subgraph，消费一条Profile中的BlendSpace Pose source binding和一到两个typed Presentation Fact Parameter输入，输出普通Pose Value与typed discontinuity；它不拥有跨State历史。
- `CharacterAnimationPresentationProfile`的Presentation Pose source binding新增`BlendSpace`来源种类。Profile仍是Pose source到表现资源的唯一绑定入口，Pose Graph节点只保存稳定source identity，不保存第二份Blend Space资产引用。
- Projection Compiler把Blend Space authoring编译为固定样本表、权重求解数据、canonical phase映射、Foot Analysis绑定、参数策略和有界workspace；Runtime不读取authoring asset、不动态构建三角关系、不搜索资源。
- `BlendSpacePlayer`内部只拥有同一Blend Space来源内的参数插值、canonical phase到各样本时间的映射、多clip加权采样和source-local feature聚合。跨State source同步与过渡由PoseState transition edge拥有，Action来源切换由AnimationSlot拥有，单Pose跳变平滑仍由显式`Inertialization`拥有。
- 时间策略必须在资产上显式选择`SharedNormalizedPhase`或`MarkerSynchronizedPhase`，不得在标记缺失时自动退回normalized time。Marker模式使用稳定Phase Reference Sample和统一MarkerId拓扑，参数变化不会动态更换phase leader。
- Foot Analysis按每个真实样本clip生成和绑定；Runtime用与骨骼姿势相同的最终样本权重聚合左右脚feature与source contribution，之后仍由唯一FootPlacement节点消费。
- 在正式Character Animation Authoring Workspace中增加Blend Space资产模式：Navigator、参数空间视图、Details、Preview、编译诊断和引用关系使用现有workspace外壳，不创建旧Workbench或第二个编辑器体系。
- Pose Graph Details与Live Debug显示参数值、落点、有效样本、归一化权重、canonical phase、每样本有效时间、foot contribution和Projection revision；Preview与Runtime执行同一编译计划。
- Character Document v3通过共享Pose capability表达BlendSpacePlayer typed payload与Profile source binding；Blend Space资源正文、generated payload和运行时权重继续只读，且不增加专用MCP action。
- Corin持续Locomotion只走`Presentation Fact -> PoseStateMachine -> SequencePlayer或正式BlendSpacePlayer`。Blend Space只在样本齐备时接入；素材不足时使用明确SequencePlayer State，不得恢复Gameplay BaseLocomotion Selection、Timeline locomotion producer或Timeline/BlendSpace双写。

## 后续动画职责重构关系

本change已经安装`BlendSpacePlayer`的通用source-local参数混合能力。剩余独立演示内容任务 MUST在`refactor-animation-control-boundaries`完成后继续，并直接把BlendSpacePlayer放入PoseState inline subgraph；不得再创建Gameplay BaseLocomotion Selection、Timeline locomotion producer或旧Selection Input接线。已完成任务保留其当时实施记录，不作为剩余任务的目标拓扑。

## Capabilities

### Added

- `character-animation-blend-space`：定义Blend Space资产、权重算法、编译、运行时、同步、feature聚合、编辑器、预览与诊断合同。

### Modified

- `character-presentation-pose-graph`：加入显式BlendSpacePlayer节点、typed参数端口和固定执行阶段。
- `character-animation-presentation-authoring`：加入Presentation Pose source到BlendSpace资源的唯一正式绑定和workspace入口。
- `character-animation-foot-analysis-artifact`：把BlendSpace sample加入正式分析源与Projection绑定。

## Current Spec Comparison

- current specs中不存在Blend Space能力；Presentation Pose source binding目前只能表达Sequence clip，不能表达稳定BlendSpace source identity。
- current `character-animation-presentation-authoring`规定Profile是持续Pose source资源的唯一入口。本change沿用这个入口并新增BlendSpace variant，不在节点、Timeline或Agent Patch中复制资产引用。
- current `character-animation-foot-analysis-artifact`按Timeline/Track/Clip绑定分析产物。本change为BlendSpaceAsset/SampleId/Clip增加并列正式source identity；同一个BlendSpace sample不得同时从Timeline与BlendSpace读取两份feature。
- `refactor-pose-graph-to-btsmtl-authoring-domain`已经接管Character Document v3的Presentation editable。本change只提供Blend Space领域能力与独立内容，不再维护旧Snapshot/Patch只读边界。
- current Pose Graph合同已经包含显式Player与线性Native Pose Plan；本change在该拓扑上增加正式BlendSpacePlayer，不回到隐藏per-slot装配。
- active `refactor-animation-control-boundaries`规定持续Locomotion source只能位于PoseState inline subgraph。本change把BlendSpacePlayer加入该有限Player目录，并保持Presentation Fact只携带参数、不携带最终样本权重。
- current BlendStack合同规定BlendStack独占跨source过渡。本change不把参数空间插值塞入BlendStack，也不让BlendSpacePlayer保留旧来源。
- `refactor-pose-graph-to-btsmtl-authoring-domain`提供唯一Navigator、Canvas、Details、Bottom Dock、Pose Watch、Capability和显式Compile边界；本change不得保留独立BlendSpace节点UI或字段switch。
- current Timeline authoring合同把Timeline和独立领域工具分开。本change使用独立Blend Space资产模式，不把二维样本空间伪装成Timeline lane。

## Dependencies And Sequencing

1. `refactor-animation-control-boundaries`已经固定Presentation Fact、PoseStateMachine、state-local source、Player、BlendStack与局部Inertialization边界。
2. `refactor-pose-graph-to-btsmtl-authoring-domain`负责把BlendSpacePlayer接入唯一Capability、typed payload、Document v3、共享UI和Pose IR handler；本change不得建立独立GraphView、Inspector switch或第二Presentation Mutation。
3. Virtual Bone change要求BlendSpace每个sample通过统一source capture输出完整PoseBoneCount；BlendSpace不得自行派生第二份Virtual Bone。
4. 剩余任务只创建独立Blend Space内容演示，不修改Corin主图，也不阻塞Character authoring、Fixed产品或DeterministicRollback闭环。演示只在完整样本、Projection、Profile、PoseState和全部source binding能一次配置时创建。
5. 唯一跨change串行顺序见`openspec/character-pipeline-serial-execution.md`。

## Deliberate Scope

- 不嵌入Unity AnimatorController、BlendTree或AnimatorControllerPlayable。
- 不把Animancer MixerState作为权重、时钟或同步权威；只复用其可见算法语义和现有采样后端。
- 不提供`Direct`模式。显式权重组合已经由BlendPose、LayeredBoneBlend和AdditivePose表达，新增Direct会形成重复权威。
- 不提供`SimpleDirectional2D`、嵌套Blend Space、任意子图或动态运行时样本增删。当前正式目录只覆盖项目移动业务需要且能与Animancer公开算法逐一对应的三种模式。
- 不在Blend Space里执行Gameplay状态选择、Motion Matching查询、Root Motion移动决策、跨来源CrossFade、惯性、FootPlacement或最终Animator输出。
- 不增加Pose专用MCP action、任意SerializedProperty写入或第二个Presentation authoring服务。

## Breaking Changes

- Pose Graph node kind、Projection schema、Presentation ContractHash和ProjectionRevision提升；旧generated Projection直接失效并重建。
- 每个BlendSpacePlayer必须位于PoseState inline subgraph并解析唯一正式BlendSpace Pose source binding；节点位于根图、绑定Sequence/MM资源或轴接口与Rig不一致时编译失败。
- MarkerSynchronizedPhase缺少Phase Reference、统一MarkerId拓扑或样本绑定时编译失败，不回退到normalized time。
- 缺失SampleId、重复坐标、非法方向、退化求解区域、Foot Analysis artifact或Pose Parameter policy时编译失败，不跳过样本、不补默认样本。
- Corin主图中已产生的Gameplay Selection接线、临时Blend Space资产、重复速度轴节点和producer binding直接删除；重新发布generated Projection，不保留兼容reader、双写或fallback。

## Success Criteria

- 作者能在正式workspace创建、编辑、编译和预览三种正式Blend Space资产，并在Pose Graph中通过显式BlendSpacePlayer使用它们。
- 参数落点在Preview与Runtime得到相同的稳定SampleId集合、归一化权重、canonical phase、样本时间、Pose Parameter和Foot feature贡献。
- BlendSpacePlayer、BlendStack、MarkerSync、Inertialization和FootPlacement各自只有一项清晰职责，代码链中没有第二个权重、过渡、同步或IK权威。
- Corin正式Profile、Projection和Pose Graph只走PoseState持续Locomotion链；后续Blend Space演示拥有独立Definition、Profile和Pose Graph，不污染当前双端帧同步验证配置。
- Character Document v3能往返BlendSpacePlayer与Profile binding；Blend Space资源正文和generated诊断保持只读。
