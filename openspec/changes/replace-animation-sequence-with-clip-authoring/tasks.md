## 1. 收口被取代的实现基线

- [x] 1.1 盘点`AnimationSequenceAsset`、`CharacterAnimationSequenceAsset`及其全部Runtime、Editor、Projection和Document引用。
- [x] 1.2 盘点`AnimationSyncMode`、`AnimationSyncTimeMapping`、Marker Group、SyncRole、Topology与Point Marker全部引用。
- [x] 1.3 盘点Foot Analysis同步descriptor、pairwise warp compiler、Projection plan与Runtime mapper全部引用。
- [x] 1.4 盘点Sequence Notify authoring、payload、Projection、snapshot、preview与runtime全部引用。
- [x] 1.5 盘点Corin 19个Sequence到AnimationClip、Pose Binding、Blend Space Sample和Timeline Segment的精确引用关系。
- [x] 1.6 盘点active Blend Space中Sequence、Marker phase和GeneratedFootPhase冲突字段与实现。
- [x] 1.7 固定新链的schema、algorithm、Projection与Document版本提升清单。
- [x] 1.8 固定完整Clip dependency、Analysis Input、注册Curve与Artifact validation四类身份边界。

## 2. 建立AnimationClip注册曲线合同

- [x] 2.1 定义唯一注册曲线catalog并登记`presentation.locomotion-phase`。
- [x] 2.2 在同一catalog登记`presentation.foot-placement-weight`。
- [x] 2.3 为每项channel定义唯一Unity EditorCurveBinding、值域、切线限制和必填条件。
- [x] 2.4 把注册Curve key time固定为Clip秒域，并建立canonical读取与有限值校验。
- [x] 2.5 建立AnimationClip注册曲线canonical写入与Undo mutation。
- [x] 2.6 建立AnimationClip对象引用、dependency hash与注册曲线hash身份。
- [x] 2.7 严格拒绝ModelImporter子Clip、只读Clip和非原生`.anim`作者目标。
- [x] 2.8 删除DefaultPlayRate素材字段并把正式默认倍率固定为1。
- [x] 2.9 从AnimationClip正式Loop设置解析Cyclic/Finite事实并删除重复Topology作者字段。
- [x] 2.10 从排除注册表现Curve的骨骼/Root曲线与正式Clip设置计算唯一`SourceDurationSeconds`。
- [x] 2.11 建立只包含骨骼/Root曲线、正式Loop与基础时长的`AnimationClipAnalysisInputHash`。
- [x] 2.12 分离完整Unity dependency baseline、Analysis Input Hash、Registered Curve Hash与Artifact validation identity。
- [x] 2.13 让Direct Clip、Action、Blend Space与Motion Matching resolver只按同一catalog精确binding读取作者Curve。
- [x] 2.14 把`presentation.foot-placement-weight`唯一降低为Runtime `animation.foot-placement-weight`参数并删除第二channel入口。

## 3. 删除Animation Sequence作者模型

- [x] 3.1 删除`AnimationSequenceAsset`基类及其Marker、Curve、Notify mutation。
- [x] 3.2 删除`CharacterAnimationSequenceAsset`和`IAnimationSequenceAnalysisReference`。
- [x] 3.3 删除Sequence AuthoringId、ContentRevision及其identity validator。
- [x] 3.4 删除Character Animation Sequence创建菜单与Inspector。
- [x] 3.5 删除Sequence资源引用resolver、dependency collector和source map字段。
- [x] 3.6 删除Sequence专用Preview target与Preview session。
- [x] 3.7 删除Sequence Notify kind和全部typed payload。
- [x] 3.8 删除Sequence Notify Projection binding、runtime snapshot与Debug View。
- [x] 3.9 删除Sequence旧codec、reader、writer和任何兼容反序列化。

## 4. 迁移Pose Source与Player

- [x] 4.1 用直接AnimationClip字段替换Profile-owned Sequence Pose Source Binding。
- [x] 4.2 删除Binding中的Sequence透传Clip、Loop、PlayRate、Rig和Analysis getter。
- [x] 4.3 让Binding只从Profile解析Rig与Foot Analysis Source。
- [x] 4.4 把`PresentationPoseSourceKind.Sequence`改名为`Clip`并删除旧枚举值。
- [x] 4.5 把Graph Capability中的`SequencePlayer`改名为`ClipPlayer`。
- [x] 4.6 把typed payload、node kind、port、source slot和binding类型同步改为Clip命名。
- [x] 4.7 修改Pose Graph compiler只从精确Clip Binding生成dense source plan。
- [x] 4.8 修改Projection payload的Sequence identity/revision字段为Clip object/dependency/curve identity。
- [x] 4.9 修改Runtime provider、player、source usage、diagnostics和Preview为Clip术语。
- [x] 4.10 删除旧SequencePlayer capability、codec、Mutation和显示名alias。
- [x] 4.11 删除ClipPlayer payload、Capability、Document、Projection descriptor与Runtime中的Loop字段。
- [x] 4.12 让ClipPlayer、source plan和Animancer sample只消费AnimationClip正式Loop事实。

## 5. 收口Unity Animation Window作者入口

- [x] 5.1 建立正式AnimationClip打开请求并要求精确Character Definition、Profile与Preview Target。
- [x] 5.2 建立作者曲线接收器并暴露两项可动画Float字段。
- [x] 5.3 让Preview Target显式安装作者曲线接收器而不成为Runtime数据源。
- [x] 5.4 让打开请求选择精确Preview Target与AnimationClip并打开Unity Animation Window。
- [x] 5.5 在Profile Inspector提供精确`Open Animation Clip`导航。
- [x] 5.6 在Pose Graph ClipPlayer Details提供同一导航。
- [x] 5.7 在Blend Space Sample Details提供同一导航。
- [x] 5.8 在Action Workspace和Timeline Segment提供同一导航。
- [x] 5.9 提供注册曲线Missing、Invalid和Ready摘要，不嵌入第二时间轴。
- [x] 5.10 删除Timeline Editor的Sequence tab、Sequence breadcrumb和Sequence selection状态。
- [x] 5.11 删除`IAnimationTimeDocumentAdapter`及其唯一Sequence实现。
- [x] 5.12 删除Sequence Marker lane、Notify lane、素材Curve lane和Sequence Details。
- [x] 5.13 保留Timeline底层frame geometry、viewport和Action Timeline交互，不让它依赖Clip作者曲线。
- [x] 5.14 校验Production Prefab不安装作者曲线接收器，Runtime与Native Pose不读取接收器字段。
- [x] 5.15 让Curve authoring、Document和Compiler按完整`path + type + property`匹配binding，不接受仅propertyName命中。

## 6. 迁移Foot Placement Weight

- [x] 6.1 建立从旧Sequence归一化Curve到Clip秒域Curve的无损迁移模型，并按`SourceDurationSeconds`缩放key time与切线。
- [x] 6.2 把19个Corin Foot Placement Weight曲线写入对应原生`.anim`并覆盖完整`[0, SourceDurationSeconds]`。
- [x] 6.3 让Projection compiler从Clip注册Curve编译canonical dense curve。
- [x] 6.4 让Runtime继续从Projection curve求Foot Placement Weight。
- [x] 6.5 删除Sequence、Binding、Timeline Segment和Blend Space Sample上的Foot Weight副本。
- [x] 6.6 缺失或值域非法时阻止Projection发布，不生成常量1。
- [x] 6.7 修改Motion Matching Clip parameter resolver复用唯一注册Curve catalog。
- [x] 6.8 删除按`animation.foot-placement-weight`或仅按propertyName直接搜索AnimationClip Curve的旧resolver路径。

## 7. 以Locomotion Phase取代Marker与GeneratedFootPhase

- [x] 7.1 定义检查Hermite段内部导数与切线过冲的连续严格单调展开Phase validator。
- [x] 7.2 校验整数Phase对应右脚Landing/Plant onset、半整数Phase对应左脚Landing/Plant onset，并核对对侧脚状态与接触顺序。
- [x] 7.3 校验Cyclic Clip秒域coverage为`[0, SourceDurationSeconds]`、首尾Phase差为正整数且半开循环边界模1连续。
- [x] 7.4 校验Finite Clip首尾key定义的秒域coverage完整覆盖实际业务秒域，不把Curve外推视为coverage。
- [x] 7.5 编译固定容量`time -> unwrapped phase` forward knots。
- [x] 7.6 编译固定容量`unwrapped phase -> time` inverse knots。
- [x] 7.7 对reduction误差、非单调、容量超限和非法斜率返回typed Build failure。
- [x] 7.8 在Profile建立Locomotion Sync Group作者数据与唯一Mutation。
- [x] 7.9 让Group只保存GroupId与精确AnimationClip成员引用。
- [x] 7.10 校验一个Clip最多属于一个Group且全部成员具有Phase曲线。
- [x] 7.11 把Direct Clip source降低为以自身Clip plan承载时钟的`AnimationSourcePhasePlan`。
- [x] 7.12 把Blend Space source降低为显式Reference承载时钟、Dynamic Sample共享unwrapped Phase的`AnimationSourcePhasePlan`。
- [x] 7.13 从PoseState实际可达edge建立source-to-source relation inventory。
- [x] 7.14 按`CommittedMovement`优先、同authority时outgoing source优先和完整Blend窗口coverage解析唯一relation leader。
- [x] 7.15 从Gameplay committed clock、正式Timeline frame rate和Player使用方式解析两侧实际秒域coverage。
- [x] 7.16 为每条relation生成只引用source endpoint与per-clip plan的Phase relation plan。
- [x] 7.17 删除`AnimationSyncMode`和`AnimationSyncTimeMapping`枚举。
- [x] 7.18 删除Marker Group、SyncRole、Topology和Point Marker作者合同。
- [x] 7.19 用Editor-only `AnimationFootPhaseValidationDescriptor`原子取代`AnimationFootSynchronizationDescriptor`及其artifact codec字段。
- [x] 7.20 删除`AnimationFootPhaseTimeWarpCompiler`、pairwise plan和warp knot payload。
- [x] 7.21 让Phase relation plan保存TransitionId、固定leader、两侧秒域coverage和validation identity。
- [x] 7.22 固定relation runtime identity为`RelationIdentity + TransitionId + TransitionGeneration`。

## 8. 增加Phase与脚部关系质量门槛

- [x] 8.1 让Foot Analysis按Phase整数和半整数inverse时间核对左右脚Landing/Plant onset。
- [x] 8.2 拒绝接触脚、onset时间、对侧脚状态或左右接触顺序与Phase语义不一致的Clip。
- [x] 8.3 计算有限source终点与target候选Phase的双脚接触差异。
- [x] 8.4 计算有限source终点与target候选Phase的脚底位置、高度和速度差异。
- [x] 8.5 计算整个Transition可见窗口的Phase coverage与inverse斜率质量。
- [x] 8.6 把质量门槛固定在versioned compiler algorithm而不是Transition配置。
- [x] 8.7 为Coverage、ContactSide、TerminalPose、WarpSlope和QualityLimit建立typed Build failure。
- [x] 8.8 删除只服务pairwise warp的计划字段，并保留质量门槛所需的最小位置、高度、速度、Plant与Landing onset采样payload。
- [x] 8.9 保持Foot Analysis普通Foot Feature、Landing与Foot Placement输入不变。
- [x] 8.10 把Foot Analysis Artifact identity从完整Clip dependency改为`AnimationClipAnalysisInputHash`。
- [x] 8.11 确认注册Curve Mutation只使Projection stale，骨骼/Root/Loop/基础时长变化才使Artifact stale。

## 9. 收口Phase Runtime

- [x] 9.1 建立只消费Projection forward/inverse knots的Phase evaluator。
- [x] 9.2 让leader source raw time只通过clock carrier forward plan得到unwrapped phase。
- [x] 9.3 让follower source按自己的raw continuation选择最近合法cycle。
- [x] 9.4 让Direct Clip follower只通过自身inverse plan得到effective Clip time。
- [x] 9.5 让Blend Space follower把同一unwrapped Phase分发给全部正权重Dynamic Sample inverse plan。
- [x] 9.6 保持effective time不写回producer raw clock。
- [x] 9.7 为finite follower coverage耗尽建立typed invalid。
- [x] 9.8 为plan identity、curve hash、无序knots和非有限求值建立typed invalid。
- [x] 9.9 删除`MarkerSegmentTimeMapper`和Marker relation cursor。
- [x] 9.10 删除occurrence ordinal、marker pair、warp fraction和Marker continuation diagnostics。
- [x] 9.11 增加source endpoint、Clip、Phase、actual coverage、target effective time和typed failure diagnostics。
- [x] 9.12 保持Runtime不读取AnimationClip Editor曲线、Profile或Foot Analysis artifact。
- [x] 9.13 在relation建立时锁定compiled leader，同一generation内禁止按weight、sample或clock进度换leader。
- [x] 9.14 在正常release时把最后effective time写成follower continuation anchor并删除relation generation。
- [x] 9.15 在Transition replacement、反向边、AlwaysResetOnEntry、Projection replacement、Presentation Reset与Dispose时按正式顺序清理relation generation。
- [x] 9.16 禁止旧relation或continuation跨reset、branch replacement与Projection identity复用。

## 10. 重基线Blend Space

- [x] 10.1 把Blend Space Sample的Sequence引用改为AnimationClip引用。
- [x] 10.2 删除Sample上的Sequence identity、Marker和Foot Analysis透传字段。
- [x] 10.3 把Phase Policy收敛为`SharedNormalizedPhase`与`LocomotionPhase`。
- [x] 10.4 删除`MarkerSynchronizedPhase`、`MarkerSegmentPhase`与`GeneratedFootPhase`枚举值。
- [x] 10.5 让LocomotionPhase sample解析同一Profile Sync Group和per-clip phase plan。
- [x] 10.6 让LocomotionPhase显式Reference Sample作为唯一source raw clock carrier。
- [x] 10.7 让Stationary sample继续使用固定normalized time且不参与Phase inverse。
- [x] 10.8 删除Reference-to-sample pairwise warp、Marker topology和动态leader代码。
- [x] 10.9 修改Blend Space Projection、Runtime、Preview与Diagnostics使用source endpoint和直接Clip phase plan。
- [x] 10.10 对账active Blend Space已经采用的直接Clip与LocomotionPhase文档口径，并迁移仍保存Sequence/旧phase的实现与剩余任务引用。

## 11. 简化Action Timeline与Workspace

- [x] 11.1 把Timeline Animation Segment的Sequence引用改为AnimationClip引用。
- [x] 11.2 保留Start/End、ClipIn、Extrapolation、Weight和Ease编排字段。
- [x] 11.3 修改Action presentation sampler直接解析AnimationClip plan。
- [x] 11.4 修改Action producer binding和source map使用Clip object identity。
- [x] 11.5 删除Action producer binding中的Foot Analysis identity副本，统一从Profile Analysis Source与Clip identity解析Artifact。
- [x] 11.6 删除Action Marker Sync relation编译与Runtime mapping。
- [x] 11.7 删除AnimationTrack Marker Sync、Time Mapping和Marker diagnostics。
- [x] 11.8 删除Sequence Notify到Action Projection与Snapshot的全部路径。
- [x] 11.9 让Action Workspace双击Segment打开Unity Animation Window。
- [x] 11.10 保持Action Window、Cue、Motion、MotionWarp、Decision和Gameplay lifecycle不变。
- [x] 11.11 删除Action Workspace中的Sequence导航、摘要和引用类型。

## 12. 同步Agent Document v4

- [x] 12.1 把唯一schema提升为`btsmtl-agent-authoring-document.v4`并删除v3 reader、writer、识别与升级器。
- [x] 12.2 删除`editable/animation-sequences/**`manifest discovery与文件闭包。
- [x] 12.3 删除Sequence Document model、strict codec与canonical writer。
- [x] 12.4 删除Sequence exporter、reconciler、planning symbol和Mutation handler。
- [x] 12.5 定义`editable/animation-clips/<stable-segment>/curves.json`秒域严格schema。
- [x] 12.6 让Clip curve fragment只接受现有原生AnimationClip对象引用与注册Curve payload。
- [x] 12.7 禁止Document创建AnimationClip、修改骨骼曲线、AnimationEvent或import设置。
- [x] 12.8 把Profile Pose Source Binding导出为直接AnimationClip结构化引用。
- [x] 12.9 把Profile Locomotion Sync Group加入Presentation目标、codec和canonical排序。
- [x] 12.10 把Timeline Animation Segment导出为直接AnimationClip结构化引用。
- [x] 12.11 更新Asset Catalog与Dependencies区分可写原生Clip和只读导入子Clip。
- [x] 12.12 更新Exporter从正式Clip读取两项注册曲线。
- [x] 12.13 更新Reconciler生成Clip curve、Profile Group与Timeline引用的同一immutable Mutation Plan。
- [x] 12.14 更新Mutation preflight校验完整Clip dependency baseline、Analysis Input Hash、精确Curve binding、秒域和值域。
- [x] 12.15 更新Application Service事务owner收集，使Clip、Profile和Timeline进入同一Undo group。
- [x] 12.16 更新reverse export从最终Unity Clip与Profile发布canonical package。
- [x] 12.17 更新Agent Validator拒绝旧v3、Sequence、Marker和GeneratedFootPhase字段。
- [x] 12.18 保持apply不Build并让曲线或Group语义变化只标记Projection stale，不把注册Curve变化误判为Foot Analysis stale。
- [x] 12.19 更新AI domain strict schema，拒绝Character Clip分片而保持统一v4生命周期。
- [x] 12.20 更新五个MCP生命周期工具说明与机器诊断，不新增Clip局部工具。
- [x] 12.21 更新`btsmtl-agent-authoring`的SKILL、current contract、Document v4字段和代码所有权地图。

## 13. 迁移Corin正式内容

- [x] 13.1 按`AnimationClipAnalysisInputHash`与新Phase Validation Descriptor显式重建Corin Foot Analysis Artifact。
- [x] 13.2 把19个Pose/Action/Blend引用从Sequence原子改为对应AnimationClip。
- [x] 13.3 在Corin Profile建立唯一`Locomotion.Gait` Phase Group。
- [x] 13.4 把19个Sequence归一化Foot Placement Weight按各自`SourceDurationSeconds`无损迁入对应Clip秒域channel。
- [x] 13.5 把WalkLoop与RunLoop正式Phase曲线写入对应Clip。
- [x] 13.6 把合法WalkStart与RunStart实际秒域coverage正式Phase曲线写入对应Clip。
- [x] 13.7 不迁移MovingTurn当前0-71 Marker plan。
- [x] 13.8 按正式Timeline frame rate把Gameplay 0-28帧换算为秒域coverage，作者MovingTurn Phase并接入质量Build。
- [x] 13.9 当前MovingTurn内容不相容时保持Character Build失败并删除旧generated Projection引用。
- [x] 13.10 把全部Corin Locomotion Transition迁移为Standard Blend。
- [x] 13.11 删除未再引用的Corin Locomotion Inertialization Policy与配置资产。
- [x] 13.12 删除Corin全部19个Sequence资产及`.meta`。
- [x] 13.13 删除Profile、Pose Graph、Timeline和Blend Space中的旧Sequence对象引用。
- [x] 13.14 删除Corin ClipPlayer Loop与Action producer Foot Analysis identity副本。
- [x] 13.15 通过唯一BTSMTL Document v4事务发布Clip Curve、Profile、Pose Graph与Timeline新authoring，并确认Artifact保持Ready。
- [x] 13.16 显式重建Corin Presentation Projection。
- [x] 13.17 显式重建Corin Float32与Fixed Character产品。

## 14. 删除旧代码与收口规范

- [x] 14.1 删除Sequence Runtime、Editor、Projection和Document文件及asmdef引用。
- [x] 14.2 删除Marker Sync Runtime、Editor、Projection和Document文件及asmdef引用。
- [x] 14.3 删除GeneratedFootPhase compiler、payload、diagnostics和旧warp artifact字段，只保留新Phase Validation Descriptor。
- [x] 14.4 删除Sequence与Marker旧serialized字段、codec、migration和fallback。
- [x] 14.5 清除代码、资产和文档中的`AnimationSequenceAsset`正式引用。
- [x] 14.6 清除代码、资产和文档中的`SequencePlayer`正式术语并统一为`ClipPlayer`。
- [x] 14.7 清除代码、资产和文档中的`GeneratedFootPhase`、Marker Group与SyncRole正式引用。
- [x] 14.8 更新`openspec/project.md`的动画作者链、同步链与Corin Locomotion口径。
- [x] 14.9 更新全部受影响current specs并删除已不成立的Marker/Sequence requirement。
- [x] 14.10 对账active Blend Space直接Clip实现、Motion Matching唯一Curve resolver和Foot Placement不读取Phase的交叉口径。
- [x] 14.11 更新`btsmtl-agent-authoring`的SKILL、Document v4当前合同与代码所有权地图。
- [x] 14.12 使用禁用共享编译服务的参数构建全部受影响Runtime与Editor工程。
- [x] 14.13 构建结束后立即执行`dotnet build-server shutdown`。
- [x] 14.14 运行本change及全量OpenSpec strict validation并清除冲突口径。
