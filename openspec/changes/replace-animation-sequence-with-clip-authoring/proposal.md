# Change: 以AnimationClip作者链取代Animation Sequence与生成式脚相同步

## Why

当前动画作者链额外引入了`AnimationSequenceAsset -> CharacterAnimationSequenceAsset -> Sequence Binding/Segment/Sample -> Projection`。这层最初用于统一Clip、Marker、Curve、Notify、Rig和Foot Analysis，但Corin正式内容已经证明它没有形成足够的业务深度：19个Sequence一一包装19个原生`.anim`，默认倍率全部为1，Rig和Foot Analysis全部相同，Notify全部为空，只有5个Locomotion素材真正参与同步。Sequence因此增加了资产、identity、revision、Document分片、Editor adapter和跨模块引用，却没有隐藏相应复杂度。

当前`GeneratedFootPhase`同步又在Sequence Marker之上增加Foot同步描述、relation-local动态规划、pairwise warp plan和Runtime knot映射。它会在实际业务只播放MovingTurn 0-28帧时按完整0-71帧Sequence编译，并且缺少关系质量拒绝门槛；结果是同步计划存在、Runtime也执行，但左右脚仍不匹配。继续修补Sequence Marker、warp knot或混合时长会保留错误的数据层次。

项目当前全部19个正式动画都是可编辑原生`.anim`。Unity Animation Window已经能够编辑骨骼动画与注册Float Curve，项目也已有AnimationMode、AnimationUtility、Foot Analysis和正式Projection Build。因此应删除Sequence与自建Sequence Editor，让AnimationClip成为素材时间真相，让Character Presentation Profile只保存角色装配与Locomotion Sync Group，并把脚步同步收敛为作者可见、Build可拒绝的Locomotion Phase曲线。

## What Changes

- 删除`AnimationSequenceAsset`、`CharacterAnimationSequenceAsset`、Sequence资产、Sequence Editor、Sequence Preview、Sequence Document adapter、Sequence Notify及全部Sequence identity/revision。
- Pose source binding、Blend Space sample与Action Timeline animation segment直接引用精确`AnimationClip`对象；Runtime Projection继续只保存编译后的dense source与Clip计划，不在Player中查找作者资产。
- Unity Animation Window成为骨骼动画、`presentation.locomotion-phase`与`presentation.foot-placement-weight`两项注册曲线的唯一时间编辑表面；项目只提供精确Clip/Preview Target打开、曲线注册、Analysis候选应用和Build诊断，不再实现第二套素材时间轴。
- `CharacterAnimationPresentationProfile`继续唯一拥有Rig、Foot Analysis Source、Pose Source Binding和Action producer binding，并新增角色级Locomotion Sync Group成员装配；Clip不保存Group、Role、Time Mapping或Transition策略。
- 删除`AnimationSyncMode`、`AnimationSyncTimeMapping`、Marker Group、SyncRole、Sequence Topology、Point Marker、`GeneratedFootPhase`同步描述、pairwise DP compiler、warp knot plan与Marker relation Runtime。
- Locomotion Sync Group改为消费Clip内严格单调的展开Phase曲线：`整数相位=右脚接触`，`整数+0.5=左脚接触`。Projection按每条可达PoseState关系、实际播放时钟和实际有限覆盖区间编译forward/inverse phase plan。
- Foot Analysis不再生成时间warp输入，只校验Phase曲线、实际左右脚接触、有限source终点和可达Transition相容性；任一不相容关系阻止Projection发布，不回退normalized time、Marker或旧计划。
- Corin全部Locomotion PoseState Transition改用Standard Blend；删除Locomotion专用Inertialization authoring及未再引用的Policy/Profile。Action、受击或其它明确业务仍可独立选择Inertialization。
- Action Timeline只保留AnimationClip引用、ClipIn、Start/End、Extrapolation、Weight、Ease和Gameplay编排内容；当前没有正式Action Marker Sync或Sequence Notify，相关通用能力直接删除。
- Blend Space Dynamic/Stationary sample直接引用AnimationClip；内部相位策略收敛为`SharedNormalizedPhase | LocomotionPhase`，删除Marker/GeneratedFootPhase策略与Reference pairwise warp。
- Agent Document v4原子取代v3：删除`editable/animation-sequences/**`与Marker字段，新增只覆盖当前Definition可达原生AnimationClip注册曲线的`editable/animation-clips/**/curves.json`；Profile和Timeline使用结构化Clip对象引用。五个BTSMTL生命周期工具、唯一Reconciler、Mutation Plan、Undo事务和显式Build边界保持不变，旧v3 package只能重新checkout。
- 以本change原子取代active `separate-animation-sequence-authoring`与`add-generated-foot-phase-animation-sync`；对active `add-character-presentation-blend-space`的Sequence与Marker Phase口径执行同一重基线，不保留并行active真相。

## Capabilities

### Added

- `character-animation-clip-authoring`：定义原生AnimationClip、注册表现曲线、Unity Animation Window、Preview Target和Profile装配的唯一作者合同。

### Modified

- `character-animation-presentation-authoring`：Profile Binding、控制曲线和同步组从Sequence/Marker模型迁移到直接Clip与Locomotion Phase Group。
- `character-presentation-pose-graph`：`SequencePlayer`收敛为`ClipPlayer`，State source sync消费编译Phase计划与实际播放覆盖区间。
- `character-animation-layer-runtime`：删除Marker relation Runtime，只保留Clip raw/effective time与编译Phase映射。
- `character-animation-selection-runtime`：把source-local Marker映射替换为Direct Clip/Blend Space统一Phase endpoint。
- `character-animation-blend-stack`：Blend Stack只消费endpoint解析后的sample，不再连接MarkerSync。
- `character-presentation-interpolation`：表现帧顺序、Player时钟与诊断改为Clip/Blend Space Phase endpoint术语。
- `character-pipeline-runtime`：唯一PresentationFrame执行顺序以Phase resolve取代Marker resolve。
- `character-animation-pipeline`：Action Timeline直接提交Clip sample，删除Sequence Notify与Marker effective time链。
- `character-animation-foot-analysis-artifact`：Artifact继续以AnimationClip为输入，只提供Phase与Transition相容性Build校验，不再生成warp descriptor。
- `character-pipeline-definition-authoring`：Profile成为Rig、Analysis、Clip binding和Locomotion Sync Group唯一装配根。
- `character-state-timeline-authoring-loop`：Corin迁移为直接Clip、Phase曲线与Standard Blend Locomotion。
- `btsmtl-timeline-editor-preview`：主Timeline Editor只编辑Action Timeline；素材时间编辑回到Unity Animation Window。
- `btsmtl-timeline-animation-authoring-surface`：Timeline Animation表面只拥有Clip Segment和Action编排，不拥有素材Marker/Curve编辑器。
- `character-action-animation-authoring-workspace`：Workspace直接打开精确AnimationClip，不导航Sequence文档。
- `agent-character-controller-synthesis`：Agent读写注册Clip曲线、Profile Sync Group和直接Clip引用，不创建Sequence。
- `agent-ai-controller-synthesis`：AI业务schema不扩张，但与Character共同原子升级到Document v4。
- `btsmtl-agent-authoring-document-sync`：Document以Clip curve fragment取代Sequence fragment，并保持同一整包事务。
- `btsmtl-agent-authoring-mcp-bridge`：五个生命周期工具透传Clip curve与Profile/Timeline同一Character事务。
- `graph-authoring-domain-framework`：人工入口与Document v4继续复用同一typed Mutation和Undo边界。

## Current Spec Comparison

- current `character-animation-presentation-authoring`仍要求Binding保存resource、Marker、Foot Placement Weight与Analysis，并要求独立Pose Source时间编辑表面；本change改为Binding直接引用Clip，Profile统一提供Rig/Analysis，素材曲线由Animation Window写入Clip。
- current `character-animation-presentation-authoring`、`character-animation-layer-runtime`、`character-animation-pipeline`与`character-presentation-pose-graph`把Marker Group、SyncRole、segment mapping和source-local Marker计划作为正式同步合同；本change完整删除这些要求，以Clip Phase曲线和Build质量门槛取代。
- current `btsmtl-timeline-editor-preview`要求Timeline Editor编辑AnimationTrack Marker、Continuous Curve和Marker Sync Preview；本change删除素材Marker lane，Timeline只编辑Action本地曲线，Clip注册曲线在Unity Animation Window编辑。
- current Agent Character、Agent AI、Document Sync、MCP Bridge和Graph Authoring规范共同锁定Document v3；本change因删除字段和新增Clip分片提升为v4，旧v3只拒绝并要求重新checkout。
- current `character-state-timeline-authoring-loop`仍要求Corin Marker策略、Locomotion.Gait marker sequence和Marker source mapping；本change删除这些内容，并要求可达Locomotion Clip具有正式Phase曲线。
- active `separate-animation-sequence-authoring`与本change在素材owner、Editor、Document和引用模型上完全冲突，必须由本change取代并删除。
- active `add-generated-foot-phase-animation-sync`与本change在同步输入、编译算法、Projection payload和Runtime mapping上完全冲突，必须由本change取代并删除。
- active `add-character-presentation-blend-space`的`Sequence sample`、`MarkerSynchronizedPhase`与Marker source sync必须改为直接Clip、`LocomotionPhase`和per-clip phase plan。
- active Motion Matching不创建Marker follower或Locomotion Phase relation，继续只消费自己的Database、Query、Plan和Selected Pose，不受本change扩张。

## Deliberate Scope

- 不修改AnimationClip骨骼曲线、Root Motion内容或动画制作工具本身；本change只规定项目注册表现曲线与装配链。
- 不扩展Unity内部Animation Window UI，不使用反射、内部类型或自定义注入lane。
- 不恢复Action Marker Sync、Sequence Notify、通用Montage Notify、运行时Foot search或第二动画播放器。
- 不让Foot Analysis、IK、Foot Placement或最终混合Pose反向修改Phase曲线或source clock。
- 不自动Build Foot Analysis、Projection、Float32/Fixed Program或Native Pose产品。
- 不在proposal阶段修改实现代码或生成Unity资产。

## Breaking Changes

- 全部`CharacterAnimationSequenceAsset`引用失效，19个Corin Sequence资产删除。
- Pose Graph节点和Projection术语从`SequencePlayer`改为`ClipPlayer`，不保留旧node kind、codec或显示名alias。
- Timeline Animation Segment与Blend Space Sample从Sequence引用改为AnimationClip引用。
- Sequence AuthoringId/ContentRevision被AnimationClip结构化对象引用、dependency hash和注册曲线hash取代。
- Marker Group、SyncRole、Time Mapping、Topology、Point Marker与GeneratedFootPhase payload全部删除。
- Sequence Notify及其payload、Projection、Snapshot和Preview支持全部删除。
- Document v3及其Sequence/Marker分片、codec、Reconciler、Mutation和manifest闭包删除；旧package不兼容读取，只能显式重新checkout为v4。
- 只有可写原生`.anim`可以进入注册曲线作者链；ModelImporter子Clip必须先经过正式归一化产出原生`.anim`，Build不创建临时副本。

## Success Criteria

- 作者双击或从Profile、Blend Space、Action Workspace导航AnimationClip时，进入Unity Animation Window并在正式Preview Target上编辑骨骼与注册表现曲线。
- Profile、Pose Source Binding、Blend Space Sample与Action Timeline只引用AnimationClip或Profile Sync Group，不存在Sequence对象、Marker副本或素材Curve副本。
- Corin可达Locomotion Clip具有可见、严格校验的Locomotion Phase曲线；MovingTurn只按Gameplay实际0-28帧覆盖参与关系编译。
- 不相容的MovingTurn到RunLoop关系在Projection Build明确失败，不能发布一条数学合法但腿部错误的映射。
- Runtime只消费Projection中的per-clip phase plan和relation引用，不读取AnimationClip Editor曲线、Foot Analysis artifact或Profile现场搜索。
- Corin Locomotion不再使用Inertialization；正确脚相之后由Standard Blend完成可预测混合。
- Agent Document、人工Animation Window、Profile Inspector和Character Build观察到同一Clip曲线与Sync Group真相，没有Sequence或Marker旧路径。
