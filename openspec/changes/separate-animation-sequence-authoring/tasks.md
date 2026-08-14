## 1. Sequence作者模型

- [x] 1.1 定义`CharacterAnimationSequenceAsset`、稳定Sequence identity、正式AnimationClip/Rig/Loop/Finite/默认倍率引用和唯一校验。
- [x] 1.2 把Time Mapping、Marker Group、Topology、SyncRole与ordered Marker迁入Sequence正式authoring API。
- [x] 1.3 为Sequence注册typed素材Curve Channel，并让Foot Placement Weight使用同一完整curve mutation合同。
- [x] 1.4 定义typed Sequence Notify catalog、稳定Notify identity、frame、payload和presentation-only校验边界。
- [x] 1.5 建立Sequence editor-only layout/view-state与资源直接打开入口，不把布局或播放状态写入资产。

## 2. Sequence编译与消费

- [ ] 2.1 定义不可变Sequence Projection plan，降低Clip、Rig、duration、loop、marker、curve、notify与Analysis引用。
- [x] 2.2 让Sequence Player从Profile Binding引用的Sequence plan采样，不再从Binding读取Clip、Marker或Curve。
- [x] 2.3 让Blend Space sample从Sequence plan解析Clip、Marker、Time Mapping与Foot Analysis绑定。
- [x] 2.4 让Action presentation sampler从Timeline Sequence Segment解析Sequence plan与segment-local ClipIn/Weight/Ease。
- [x] 2.5 保证Sequence Notify只进入表现预览/只读snapshot，不产生Gameplay Timeline、Window、Cue、Motion、Warp或Action lifecycle输出。

## 3. Profile与Blend Space引用迁移

- [x] 3.1 把Profile-owned Sequence Pose Source Binding收敛为Source Slot到Sequence的强类型引用和binding identity。
- [x] 3.2 删除Sequence Binding中的Clip、Loop、PlayRate、Marker、Time Mapping、Curve、Rig与Analysis重复字段及对应mutation。
- [x] 3.3 把Blend Space Dynamic/Stationary sample的裸AnimationClip和Marker改为Sequence引用，同时保留sample-owned位置、角色与Stationary time。
- [x] 3.4 删除Blend Space Details中的Sample Time Authoring和`AnimationTimeField`接线，只保留Sequence摘要与Open Sequence导航。
- [x] 3.5 更新Profile/Blend Space validator、Projection compiler与References，使所有资源关系按精确Sequence对象引用解析。

## 4. Action Timeline编排模型

- [x] 4.1 把Timeline Animation Clip改为强类型Sequence Segment，保存Sequence引用、Start/End、ClipIn、Extrapolation、Weight与Ease。
- [x] 4.2 从Timeline AnimationTrack删除素材Marker Sync、Time Mapping和Point Marker owner。
- [x] 4.3 从Timeline Animation Clip删除裸AnimationClip、素材Foot Placement Curve与其它Sequence素材覆盖字段。
- [x] 4.4 定义稳定`TimelineSection`作者数据、正式Mutation、Undo、导航与唯一校验，不增加执行跳转或Gameplay事实。
- [x] 4.5 更新Timeline compiler、Action producer binding、source map、Preview与Live Debug以解析Sequence Segment和Sequence marker plan。

## 5. 共享主时间编辑器

- [ ] 5.1 从现有`TimelineEditorView/TimelineFieldView`提取不依赖`TimelineData/Track/Clip`的`AnimationTimeCanvas`、frame geometry、viewport、selection、interaction和rendering ports。
- [ ] 5.2 定义typed `IAnimationTimeDocumentAdapter`、lane descriptor、selection descriptor、mutation transaction、Preview binding与Diagnostics合同。
- [ ] 5.3 安装Action Timeline document adapter，保持现有Track/Clip/Window/Cue/Curve交互与正式Timeline Mutation。
- [ ] 5.4 安装Sequence document adapter，显示素材Span、Sync Marker、Notify、typed Curve、Analysis overlay与Sequence Preview。
- [x] 5.5 统一主窗口的播放、暂停、seek、速度、zoom/pan、playhead、Details、Tools、文档tab和breadcrumb状态。
- [x] 5.6 实现Action Segment双击进入精确Sequence、返回Action Timeline并恢复各自window-local selection/view-state。
- [x] 5.7 让Details只编辑选择属性和精确数值，删除全部Inspector内嵌time ruler、Marker lane、Curve lane和独立playhead。

## 6. Preview与Analysis

- [x] 6.1 建立Sequence Preview typed session adapter，只执行Sequence表现采样、正式Rig/Pose预览和只读Marker/Notify overlay。
- [x] 6.2 保持Action Timeline Preview沿Action Playback、AnimationSlot、Routing与Pose Plan执行，并从Sequence Segment读取素材。
- [x] 6.3 让Foot Analysis工具以Sequence和精确Clip/Rig/Analysis identity检查artifact并显式应用候选到Sequence Marker。
- [x] 6.4 禁止两个文档模式因打开、selection、seek、Preview或Analysis自动运行Character Build、Projection Build或Foot Analysis Build。

## 7. Agent Document v3

- [x] 7.1 为`editable/animation-sequences/<stable-segment>/{sequence.json,curves.json}`定义strict模型、canonical codec、manifest闭包与hash。
- [x] 7.2 更新Asset Catalog、Dependencies与Presentation context，使Profile、Blend Space和Timeline可引用现有或`local:*`Sequence。
- [x] 7.3 更新Exporter与reverse exporter，把Sequence素材数据只输出到Sequence分片，并让其它分片只输出稳定Sequence引用。
- [x] 7.4 更新Reconciler、planning symbol、typed Mutation handler与preflight，使Sequence创建/修改/删除和跨owner引用进入同一immutable plan。
- [x] 7.5 更新Agent Validator，拒绝Timeline Track、Profile Binding或Blend Space sample残留素材Marker/Curve/Notify字段。
- [x] 7.6 删除旧Document Timeline/Profile素材字段、parser、writer、reconciler与mutation，不保留兼容schema或局部Sequence MCP工具。

## 8. 正式内容迁移与清理

- [ ] 8.1 实现按完整内容签名迁移Profile Sequence Binding、Blend Space sample与Action Timeline Animation数据的显式迁移命令。
- [ ] 8.2 对无法从Track级Marker唯一映射到Sequence的Action Timeline返回精确Track/Clip/coverage诊断并停止迁移。
- [x] 8.3 迁移Corin Run、TurnBack、Stop、Start与全部有限Action动画引用到正式Sequence资产。
- [x] 8.4 重基线active generated foot phase数据，使Time Mapping、Marker occurrence与warp输入只从Sequence owner解析。
- [x] 8.5 重基线active Blend Space数据，使每个sample引用Sequence且不保存Marker副本。
- [x] 8.6 删除`CharacterPoseSourceEditorWindow`、`AnimationTimeFieldAuthoring`、Blend Space Inspector内嵌时间轴及其菜单、adapter、session和样式资源。
- [x] 8.7 删除Timeline Track素材Marker、Timeline Clip素材Curve、Profile Binding素材字段与全部旧runtime/compiler/editor reader。

## 9. 构建与静态对账

- [ ] 9.1 更新相关Runtime/Editor程序集引用，使Timeline Editor Core不反向依赖Character领域，而Character Sequence adapter通过显式Editor composition安装。
- [x] 9.2 使用禁用共享编译服务的参数构建受影响Runtime与Editor工程，并在构建后立即关闭.NET build server。
- [x] 9.3 运行OpenSpec strict validation并确认current specs、active generated foot phase与active Blend Space不再声明旧Marker/Curve owner。
- [x] 9.4 搜索并清除`CharacterPoseSourceEditorWindow`、`AnimationTimeField`、Track-owned material Marker、Binding-owned material Curve和sample-owned Marker的正式入口。
