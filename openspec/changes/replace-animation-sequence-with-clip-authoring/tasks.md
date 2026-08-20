## 1. 收口被取代的实现基线

- [ ] 1.1 盘点`AnimationSequenceAsset`、`CharacterAnimationSequenceAsset`及其全部Runtime、Editor、Projection和Document引用。
- [ ] 1.2 盘点`AnimationSyncMode`、`AnimationSyncTimeMapping`、Marker Group、SyncRole、Topology与Point Marker全部引用。
- [ ] 1.3 盘点Foot Analysis同步descriptor、pairwise warp compiler、Projection plan与Runtime mapper全部引用。
- [ ] 1.4 盘点Sequence Notify authoring、payload、Projection、snapshot、preview与runtime全部引用。
- [ ] 1.5 盘点Corin 19个Sequence到AnimationClip、Pose Binding、Blend Space Sample和Timeline Segment的精确引用关系。
- [ ] 1.6 盘点active Blend Space中Sequence、Marker phase和GeneratedFootPhase冲突字段与实现。
- [ ] 1.7 固定新链的schema、algorithm、Projection与Document版本提升清单。
- [ ] 1.8 固定完整Clip dependency、Analysis Input、注册Curve与Artifact validation四类身份边界。

## 2. 建立AnimationClip注册曲线合同

- [ ] 2.1 定义唯一注册曲线catalog并登记`presentation.locomotion-phase`。
- [ ] 2.2 在同一catalog登记`presentation.foot-placement-weight`。
- [ ] 2.3 为每项channel定义唯一Unity EditorCurveBinding、值域、切线限制和必填条件。
- [ ] 2.4 把注册Curve key time固定为Clip秒域，并建立canonical读取与有限值校验。
- [ ] 2.5 建立AnimationClip注册曲线canonical写入与Undo mutation。
- [ ] 2.6 建立AnimationClip对象引用、dependency hash与注册曲线hash身份。
- [ ] 2.7 严格拒绝ModelImporter子Clip、只读Clip和非原生`.anim`作者目标。
- [ ] 2.8 删除DefaultPlayRate素材字段并把正式默认倍率固定为1。
- [ ] 2.9 从AnimationClip正式Loop设置解析Cyclic/Finite事实并删除重复Topology作者字段。
- [ ] 2.10 从排除注册表现Curve的骨骼/Root曲线与正式Clip设置计算唯一`SourceDurationSeconds`。
- [ ] 2.11 建立只包含骨骼/Root曲线、正式Loop与基础时长的`AnimationClipAnalysisInputHash`。
- [ ] 2.12 分离完整Unity dependency baseline、Analysis Input Hash、Registered Curve Hash与Artifact validation identity。
- [ ] 2.13 让Direct Clip、Action、Blend Space与Motion Matching resolver只按同一catalog精确binding读取作者Curve。
- [ ] 2.14 把`presentation.foot-placement-weight`唯一降低为Runtime `animation.foot-placement-weight`参数并删除第二channel入口。

## 3. 删除Animation Sequence作者模型

- [ ] 3.1 删除`AnimationSequenceAsset`基类及其Marker、Curve、Notify mutation。
- [ ] 3.2 删除`CharacterAnimationSequenceAsset`和`IAnimationSequenceAnalysisReference`。
- [ ] 3.3 删除Sequence AuthoringId、ContentRevision及其identity validator。
- [ ] 3.4 删除Character Animation Sequence创建菜单与Inspector。
- [ ] 3.5 删除Sequence资源引用resolver、dependency collector和source map字段。
- [ ] 3.6 删除Sequence专用Preview target与Preview session。
- [ ] 3.7 删除Sequence Notify kind和全部typed payload。
- [ ] 3.8 删除Sequence Notify Projection binding、runtime snapshot与Debug View。
- [ ] 3.9 删除Sequence旧codec、reader、writer和任何兼容反序列化。

## 4. 迁移Pose Source与Player

- [ ] 4.1 用直接AnimationClip字段替换Profile-owned Sequence Pose Source Binding。
- [ ] 4.2 删除Binding中的Sequence透传Clip、Loop、PlayRate、Rig和Analysis getter。
- [ ] 4.3 让Binding只从Profile解析Rig与Foot Analysis Source。
- [ ] 4.4 把`PresentationPoseSourceKind.Sequence`改名为`Clip`并删除旧枚举值。
- [ ] 4.5 把Graph Capability中的`SequencePlayer`改名为`ClipPlayer`。
- [ ] 4.6 把typed payload、node kind、port、source slot和binding类型同步改为Clip命名。
- [ ] 4.7 修改Pose Graph compiler只从精确Clip Binding生成dense source plan。
- [ ] 4.8 修改Projection payload的Sequence identity/revision字段为Clip object/dependency/curve identity。
- [ ] 4.9 修改Runtime provider、player、source usage、diagnostics和Preview为Clip术语。
- [ ] 4.10 删除旧SequencePlayer capability、codec、Mutation和显示名alias。
- [ ] 4.11 删除ClipPlayer payload、Capability、Document、Projection descriptor与Runtime中的Loop字段。
- [ ] 4.12 让ClipPlayer、source plan和Animancer sample只消费AnimationClip正式Loop事实。

## 5. 收口Unity Animation Window作者入口

- [ ] 5.1 建立正式AnimationClip打开请求并要求精确Character Definition、Profile与Preview Target。
- [ ] 5.2 建立作者曲线接收器并暴露两项可动画Float字段。
- [ ] 5.3 让Preview Target显式安装作者曲线接收器而不成为Runtime数据源。
- [ ] 5.4 让打开请求选择精确Preview Target与AnimationClip并打开Unity Animation Window。
- [ ] 5.5 在Profile Inspector提供精确`Open Animation Clip`导航。
- [ ] 5.6 在Pose Graph ClipPlayer Details提供同一导航。
- [ ] 5.7 在Blend Space Sample Details提供同一导航。
- [ ] 5.8 在Action Workspace和Timeline Segment提供同一导航。
- [ ] 5.9 提供注册曲线Missing、Invalid和Ready摘要，不嵌入第二时间轴。
- [ ] 5.10 删除Timeline Editor的Sequence tab、Sequence breadcrumb和Sequence selection状态。
- [ ] 5.11 删除`IAnimationTimeDocumentAdapter`及其唯一Sequence实现。
- [ ] 5.12 删除Sequence Marker lane、Notify lane、素材Curve lane和Sequence Details。
- [ ] 5.13 保留Timeline底层frame geometry、viewport和Action Timeline交互，不让它依赖Clip作者曲线。
- [ ] 5.14 校验Production Prefab不安装作者曲线接收器，Runtime与Native Pose不读取接收器字段。
- [ ] 5.15 让Curve authoring、Document和Compiler按完整`path + type + property`匹配binding，不接受仅propertyName命中。

## 6. 迁移Foot Placement Weight

- [ ] 6.1 建立从旧Sequence归一化Curve到Clip秒域Curve的无损迁移模型，并按`SourceDurationSeconds`缩放key time与切线。
- [ ] 6.2 把19个Corin Foot Placement Weight曲线写入对应原生`.anim`并覆盖完整`[0, SourceDurationSeconds]`。
- [ ] 6.3 让Projection compiler从Clip注册Curve编译canonical dense curve。
- [ ] 6.4 让Runtime继续从Projection curve求Foot Placement Weight。
- [ ] 6.5 删除Sequence、Binding、Timeline Segment和Blend Space Sample上的Foot Weight副本。
- [ ] 6.6 缺失或值域非法时阻止Projection发布，不生成常量1。
- [ ] 6.7 修改Motion Matching Clip parameter resolver复用唯一注册Curve catalog。
- [ ] 6.8 删除按`animation.foot-placement-weight`或仅按propertyName直接搜索AnimationClip Curve的旧resolver路径。

## 7. 以Locomotion Phase取代Marker与GeneratedFootPhase

- [ ] 7.1 定义检查Hermite段内部导数与切线过冲的连续严格单调展开Phase validator。
- [ ] 7.2 校验整数Phase对应右脚Landing/Plant onset、半整数Phase对应左脚Landing/Plant onset，并核对对侧脚状态与接触顺序。
- [ ] 7.3 校验Cyclic Clip秒域coverage为`[0, SourceDurationSeconds]`、首尾Phase差为正整数且半开循环边界模1连续。
- [ ] 7.4 校验Finite Clip首尾key定义的秒域coverage完整覆盖实际业务秒域，不把Curve外推视为coverage。
- [ ] 7.5 编译固定容量`time -> unwrapped phase` forward knots。
- [ ] 7.6 编译固定容量`unwrapped phase -> time` inverse knots。
- [ ] 7.7 对reduction误差、非单调、容量超限和非法斜率返回typed Build failure。
- [ ] 7.8 在Profile建立Locomotion Sync Group作者数据与唯一Mutation。
- [ ] 7.9 让Group只保存GroupId与精确AnimationClip成员引用。
- [ ] 7.10 校验一个Clip最多属于一个Group且全部成员具有Phase曲线。
- [ ] 7.11 把Direct Clip source降低为以自身Clip plan承载时钟的`AnimationSourcePhasePlan`。
- [ ] 7.12 把Blend Space source降低为显式Reference承载时钟、Dynamic Sample共享unwrapped Phase的`AnimationSourcePhasePlan`。
- [ ] 7.13 从PoseState实际可达edge建立source-to-source relation inventory。
- [ ] 7.14 按`CommittedMovement`优先、同authority时outgoing source优先和完整Blend窗口coverage解析唯一relation leader。
- [ ] 7.15 从Gameplay committed clock、正式Timeline frame rate和Player使用方式解析两侧实际秒域coverage。
- [ ] 7.16 为每条relation生成只引用source endpoint与per-clip plan的Phase relation plan。
- [ ] 7.17 删除`AnimationSyncMode`和`AnimationSyncTimeMapping`枚举。
- [ ] 7.18 删除Marker Group、SyncRole、Topology和Point Marker作者合同。
- [ ] 7.19 用Editor-only `AnimationFootPhaseValidationDescriptor`原子取代`AnimationFootSynchronizationDescriptor`及其artifact codec字段。
- [ ] 7.20 删除`AnimationFootPhaseTimeWarpCompiler`、pairwise plan和warp knot payload。
- [ ] 7.21 让Phase relation plan保存TransitionId、固定leader、两侧秒域coverage和validation identity。
- [ ] 7.22 固定relation runtime identity为`RelationIdentity + TransitionId + TransitionGeneration`。

## 8. 增加Phase与脚部关系质量门槛

- [ ] 8.1 让Foot Analysis按Phase整数和半整数inverse时间核对左右脚Landing/Plant onset。
- [ ] 8.2 拒绝接触脚、onset时间、对侧脚状态或左右接触顺序与Phase语义不一致的Clip。
- [ ] 8.3 计算有限source终点与target候选Phase的双脚接触差异。
- [ ] 8.4 计算有限source终点与target候选Phase的脚底位置、高度和速度差异。
- [ ] 8.5 计算整个Transition可见窗口的Phase coverage与inverse斜率质量。
- [ ] 8.6 把质量门槛固定在versioned compiler algorithm而不是Transition配置。
- [ ] 8.7 为Coverage、ContactSide、TerminalPose、WarpSlope和QualityLimit建立typed Build failure。
- [ ] 8.8 删除只服务pairwise warp的计划字段，并保留质量门槛所需的最小位置、高度、速度、Plant与Landing onset采样payload。
- [ ] 8.9 保持Foot Analysis普通Foot Feature、Landing与Foot Placement输入不变。
- [ ] 8.10 把Foot Analysis Artifact identity从完整Clip dependency改为`AnimationClipAnalysisInputHash`。
- [ ] 8.11 确认注册Curve Mutation只使Projection stale，骨骼/Root/Loop/基础时长变化才使Artifact stale。

## 9. 收口Phase Runtime

- [ ] 9.1 建立只消费Projection forward/inverse knots的Phase evaluator。
- [ ] 9.2 让leader source raw time只通过clock carrier forward plan得到unwrapped phase。
- [ ] 9.3 让follower source按自己的raw continuation选择最近合法cycle。
- [ ] 9.4 让Direct Clip follower只通过自身inverse plan得到effective Clip time。
- [ ] 9.5 让Blend Space follower把同一unwrapped Phase分发给全部正权重Dynamic Sample inverse plan。
- [ ] 9.6 保持effective time不写回producer raw clock。
- [ ] 9.7 为finite follower coverage耗尽建立typed invalid。
- [ ] 9.8 为plan identity、curve hash、无序knots和非有限求值建立typed invalid。
- [ ] 9.9 删除`MarkerSegmentTimeMapper`和Marker relation cursor。
- [ ] 9.10 删除occurrence ordinal、marker pair、warp fraction和Marker continuation diagnostics。
- [ ] 9.11 增加source endpoint、Clip、Phase、actual coverage、target effective time和typed failure diagnostics。
- [ ] 9.12 保持Runtime不读取AnimationClip Editor曲线、Profile或Foot Analysis artifact。
- [ ] 9.13 在relation建立时锁定compiled leader，同一generation内禁止按weight、sample或clock进度换leader。
- [ ] 9.14 在正常release时把最后effective time写成follower continuation anchor并删除relation generation。
- [ ] 9.15 在Transition replacement、反向边、AlwaysResetOnEntry、Projection replacement、Presentation Reset与Dispose时按正式顺序清理relation generation。
- [ ] 9.16 禁止旧relation或continuation跨reset、branch replacement与Projection identity复用。

## 10. 重基线Blend Space

- [ ] 10.1 把Blend Space Sample的Sequence引用改为AnimationClip引用。
- [ ] 10.2 删除Sample上的Sequence identity、Marker和Foot Analysis透传字段。
- [ ] 10.3 把Phase Policy收敛为`SharedNormalizedPhase`与`LocomotionPhase`。
- [ ] 10.4 删除`MarkerSynchronizedPhase`、`MarkerSegmentPhase`与`GeneratedFootPhase`枚举值。
- [ ] 10.5 让LocomotionPhase sample解析同一Profile Sync Group和per-clip phase plan。
- [ ] 10.6 让LocomotionPhase显式Reference Sample作为唯一source raw clock carrier。
- [ ] 10.7 让Stationary sample继续使用固定normalized time且不参与Phase inverse。
- [ ] 10.8 删除Reference-to-sample pairwise warp、Marker topology和动态leader代码。
- [ ] 10.9 修改Blend Space Projection、Runtime、Preview与Diagnostics使用source endpoint和直接Clip phase plan。
- [ ] 10.10 对账active Blend Space已经采用的直接Clip与LocomotionPhase文档口径，并迁移仍保存Sequence/旧phase的实现与剩余任务引用。

## 11. 简化Action Timeline与Workspace

- [ ] 11.1 把Timeline Animation Segment的Sequence引用改为AnimationClip引用。
- [ ] 11.2 保留Start/End、ClipIn、Extrapolation、Weight和Ease编排字段。
- [ ] 11.3 修改Action presentation sampler直接解析AnimationClip plan。
- [ ] 11.4 修改Action producer binding和source map使用Clip object identity。
- [ ] 11.5 删除Action producer binding中的Foot Analysis identity副本，统一从Profile Analysis Source与Clip identity解析Artifact。
- [ ] 11.6 删除Action Marker Sync relation编译与Runtime mapping。
- [ ] 11.7 删除AnimationTrack Marker Sync、Time Mapping和Marker diagnostics。
- [ ] 11.8 删除Sequence Notify到Action Projection与Snapshot的全部路径。
- [ ] 11.9 让Action Workspace双击Segment打开Unity Animation Window。
- [ ] 11.10 保持Action Window、Cue、Motion、MotionWarp、Decision和Gameplay lifecycle不变。
- [ ] 11.11 删除Action Workspace中的Sequence导航、摘要和引用类型。

## 12. 同步Agent Document v4

- [ ] 12.1 把唯一schema提升为`btsmtl-agent-authoring-document.v4`并删除v3 reader、writer、识别与升级器。
- [ ] 12.2 删除`editable/animation-sequences/**`manifest discovery与文件闭包。
- [ ] 12.3 删除Sequence Document model、strict codec与canonical writer。
- [ ] 12.4 删除Sequence exporter、reconciler、planning symbol和Mutation handler。
- [ ] 12.5 定义`editable/animation-clips/<stable-segment>/curves.json`秒域严格schema。
- [ ] 12.6 让Clip curve fragment只接受现有原生AnimationClip对象引用与注册Curve payload。
- [ ] 12.7 禁止Document创建AnimationClip、修改骨骼曲线、AnimationEvent或import设置。
- [ ] 12.8 把Profile Pose Source Binding导出为直接AnimationClip结构化引用。
- [ ] 12.9 把Profile Locomotion Sync Group加入Presentation目标、codec和canonical排序。
- [ ] 12.10 把Timeline Animation Segment导出为直接AnimationClip结构化引用。
- [ ] 12.11 更新Asset Catalog与Dependencies区分可写原生Clip和只读导入子Clip。
- [ ] 12.12 更新Exporter从正式Clip读取两项注册曲线。
- [ ] 12.13 更新Reconciler生成Clip curve、Profile Group与Timeline引用的同一immutable Mutation Plan。
- [ ] 12.14 更新Mutation preflight校验完整Clip dependency baseline、Analysis Input Hash、精确Curve binding、秒域和值域。
- [ ] 12.15 更新Application Service事务owner收集，使Clip、Profile和Timeline进入同一Undo group。
- [ ] 12.16 更新reverse export从最终Unity Clip与Profile发布canonical package。
- [ ] 12.17 更新Agent Validator拒绝旧v3、Sequence、Marker和GeneratedFootPhase字段。
- [ ] 12.18 保持apply不Build并让曲线或Group语义变化只标记Projection stale，不把注册Curve变化误判为Foot Analysis stale。
- [ ] 12.19 更新AI domain strict schema，拒绝Character Clip分片而保持统一v4生命周期。
- [ ] 12.20 更新五个MCP生命周期工具说明与机器诊断，不新增Clip局部工具。
- [ ] 12.21 更新`btsmtl-agent-authoring`的SKILL、current contract、Document v4字段和代码所有权地图。

## 13. 迁移Corin正式内容

- [ ] 13.1 按`AnimationClipAnalysisInputHash`与新Phase Validation Descriptor显式重建Corin Foot Analysis Artifact。
- [ ] 13.2 把19个Pose/Action/Blend引用从Sequence原子改为对应AnimationClip。
- [ ] 13.3 在Corin Profile建立唯一`Locomotion.Gait` Phase Group。
- [ ] 13.4 把19个Sequence归一化Foot Placement Weight按各自`SourceDurationSeconds`无损迁入对应Clip秒域channel。
- [ ] 13.5 把WalkLoop与RunLoop正式Phase曲线写入对应Clip。
- [ ] 13.6 把合法WalkStart与RunStart实际秒域coverage正式Phase曲线写入对应Clip。
- [ ] 13.7 不迁移MovingTurn当前0-71 Marker plan。
- [ ] 13.8 按正式Timeline frame rate把Gameplay 0-28帧换算为秒域coverage，作者MovingTurn Phase并接入质量Build。
- [ ] 13.9 当前MovingTurn内容不相容时保持Character Build失败并删除旧generated Projection引用。
- [ ] 13.10 把全部Corin Locomotion Transition迁移为Standard Blend。
- [ ] 13.11 删除未再引用的Corin Locomotion Inertialization Policy与配置资产。
- [ ] 13.12 删除Corin全部19个Sequence资产及`.meta`。
- [ ] 13.13 删除Profile、Pose Graph、Timeline和Blend Space中的旧Sequence对象引用。
- [ ] 13.14 删除Corin ClipPlayer Loop与Action producer Foot Analysis identity副本。
- [ ] 13.15 通过唯一BTSMTL Document v4事务发布Clip Curve、Profile、Pose Graph与Timeline新authoring，并确认Artifact保持Ready。
- [ ] 13.16 显式重建Corin Presentation Projection。
- [ ] 13.17 显式重建Corin Float32与Fixed Character产品。

## 14. 删除旧代码与收口规范

- [ ] 14.1 删除Sequence Runtime、Editor、Projection和Document文件及asmdef引用。
- [ ] 14.2 删除Marker Sync Runtime、Editor、Projection和Document文件及asmdef引用。
- [ ] 14.3 删除GeneratedFootPhase compiler、payload、diagnostics和旧warp artifact字段，只保留新Phase Validation Descriptor。
- [ ] 14.4 删除Sequence与Marker旧serialized字段、codec、migration和fallback。
- [ ] 14.5 清除代码、资产和文档中的`AnimationSequenceAsset`正式引用。
- [ ] 14.6 清除代码、资产和文档中的`SequencePlayer`正式术语并统一为`ClipPlayer`。
- [ ] 14.7 清除代码、资产和文档中的`GeneratedFootPhase`、Marker Group与SyncRole正式引用。
- [ ] 14.8 更新`openspec/project.md`的动画作者链、同步链与Corin Locomotion口径。
- [ ] 14.9 更新全部受影响current specs并删除已不成立的Marker/Sequence requirement。
- [ ] 14.10 对账active Blend Space直接Clip实现、Motion Matching唯一Curve resolver和Foot Placement不读取Phase的交叉口径。
- [ ] 14.11 更新`btsmtl-agent-authoring`的SKILL、Document v4当前合同与代码所有权地图。
- [ ] 14.12 使用禁用共享编译服务的参数构建全部受影响Runtime与Editor工程。
- [ ] 14.13 构建结束后立即执行`dotnet build-server shutdown`。
- [ ] 14.14 运行本change及全量OpenSpec strict validation并清除冲突口径。
