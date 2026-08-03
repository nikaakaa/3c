# Change: 用资产引用替换Pose Source字符串作者关系

## Why

当前持续动画资源关系仍由作者层字符串串接：

- `SequencePlayer`、`BlendSpacePlayer`与`SelectedPosePlayer`保存`PresentationPoseSourceId`或Provider字符串。
- `CharacterAnimationPresentationProfile`使用同一字符串查找内联`PresentationPoseSourceBinding`。
- 通用Details在领域没有提供选项时直接退化为可编辑`TextField`，即使提供选项也把可读名称与内部identity拼在一起显示。
- Profile Inspector、Navigator、breadcrumb与部分动画资产Inspector仍直接显示Stable Source Id、GraphId、Rig identity、revision、GUID或哈希。

这使作者必须理解并维护机器identity，拼写错误只会在后续Validate或Build暴露，也无法获得Unity对象选择、Ping、Open、资源类型约束与直接预览体验。ALS通过AnimGraph节点选择具体动画资产，GASP通过节点选择Chooser或Pose Search Database资产；它们都不会要求作者手填资源identity。

现行`character-animation-presentation-authoring`和`character-presentation-pose-graph`明确要求Player通过`PresentationPoseSourceId`解析Profile binding，已经与本次“不再使用字符串建立动画资源关系”的目标冲突。本change必须正式替换该合同，不能只隐藏字段或增加一个保留旧字符串的选择器。

## What Changes

- 引入对象引用式Pose source authoring：
  - Pose Graph拥有可读命名的`CharacterPresentationPoseSourceSlot`子资产，表达Idle、Run Loop、Moving Turn等语义插槽。
  - Presentation Profile拥有按类型区分的Pose source binding子资产，使用Unity对象引用连接Source Slot与Sequence、Blend Space或Motion Matching资源。
  - `SequencePlayer`、`BlendSpacePlayer`与`SelectedPosePlayer`直接引用类型匹配的Source Slot对象，不再序列化`m_SourceId`或`m_ProviderId`字符串。
  - Source Slot与binding以子资产形式保存在既有Pose Graph和Profile资产内，不为每个状态制造独立顶层`.asset`文件。
- 保持Profile变体能力：
  - shared Pose Graph只声明语义Source Slot，不直接绑定某个角色的AnimationClip。
  - 每个Character Presentation Profile可以为同一Source Slot绑定不同动画资源、Rig、marker、Foot Analysis与参数策略。
  - Profile仍是资源绑定的唯一owner，Pose Graph节点不复制Clip、marker或analysis数据。
- 收口编译与运行身份：
  - Projection Compiler按精确Unity对象引用解析Source Slot和Profile binding。
  - 编译产物为每个可达source分配Projection-local dense source index，并保存明确source map；Runtime sample与Player使用dense index、Player identity、generation和Projection revision，不再依赖作者字符串SourceId。
  - Unity资产GUID/local file id只用于Editor解析、Document同步与构建依赖身份，不成为作者可编辑字段或Runtime查找键。
- 重做作者界面投影：
  - Source字段使用类型受限的对象选择器或精确Profile上下文的可搜索资源选择器。
  - 选中Player时显示Source Slot、实际Animation Clip/Blend Space/MM资源、时长、Rig、marker、analysis和状态，并提供Ping、Open Source与Open Owner。
  - Navigator、breadcrumb、节点副标题与Profile Inspector只显示业务名称和Unity资源名；稳定identity、revision、GUID、哈希与compiled index默认隐藏，只允许在显式Diagnostics区域只读查看。
  - 通用IdentityReference选择器不再把内部值拼进显示标签，也不得在缺少选项目录时退化成可编辑字符串。
- 扩展唯一Document v3链路：
  - Presentation profile与Pose Graph JSON使用结构化资产引用表达Source Slot和binding子资产。
  - 既有资产引用结构扩展`localFileId`，以精确引用同一`.asset`文件内的子资产。
  - 新建Source Slot或binding在editable中使用`local:*`，经同一Reconciler、typed Presentation Mutation和资产级事务创建子资产，apply后反向导出正式对象引用。
  - 不增加Pose专用MCP action、第二Reconciler、直接YAML写入或按名称查找fallback。
- 激进迁移并删除旧路径：
  - Corin现有Idle、Walk、Run、Start、Stop与Moving Turn source一次迁移为正式Source Slot和Profile binding子资产。
  - 删除作者资产中的`PresentationPoseSourceId`、内联`PresentationPoseSourceBinding[]`、Player字符串source/provider字段及其mutation、codec与validator路径。
  - 旧字符串资产在迁移前明确Invalid，不保留兼容reader、双写、自动修复或运行时fallback。
- 所有Compile、Projection Build、Foot Analysis与Motion Matching Database Build继续只由明确命令触发；选择对象、打开窗口、重载Inspector和AssetDatabase refresh都不得触发重操作。

## Impact

- 影响Character Presentation Profile、Pose Graph typed payload、Capability Catalog、Details/Navigator/Profile Inspector、Projection Compiler、Projection payload、Pose source provider/player/runtime sample、Animancer source backend、Foot Analysis source map与diagnostics。
- 影响Document v3 Presentation model、asset catalog、exporter、codec、reconciler、typed Presentation Mutation、validator、reverse export和事务内子资产生命周期。
- 需要重新对账`add-character-presentation-blend-space`、`add-character-motion-matching-pose-source`与`refactor-animation-control-boundaries`中依赖`PresentationPoseSourceId`的未归档设计和任务；不得让这些active change恢复字符串authoring路径。
- 不改变Gameplay StateMachine、Action Timeline、AnimationChannel仲裁、Motion Curve、MotionWarp、Rollback state或网络协议。
- 不运行Unity batchmode，不自动Build，不新增测试。

## 与现行Spec对比

- `character-animation-presentation-authoring`当前要求Profile保存`PresentationPoseSourceId -> resource`映射，且SequencePlayer只引用Source Id；本change改为`Source Slot对象 -> Profile binding子资产 -> resource对象`。
- `character-presentation-pose-graph`当前要求Player按ProviderId、PlayerNodeId与PresentationPoseSourceId解析；本change保留Player结构identity与generation，但把作者source关系改为对象引用，并在Projection内降低为dense source index。
- `graph-authoring-domain-framework`已经要求稳定identity默认隐藏；当前Details的字符串fallback、`DisplayName · identity`标签和空References属于实现违约，本change把该要求扩展为禁止资源关系降级为字符串编辑。
- `btsmtl-agent-authoring-document-sync`当前Presentation JSON仍用字符串typed property表达Pose source；本change要求Document通过结构化子资产引用和`local:*`创建语义进入同一事务。
- `add-character-presentation-blend-space`当前声明BlendSpacePlayer不得保存资产引用且必须依赖`PresentationPoseSourceId`，与本change冲突，实施前必须重写为引用类型匹配的Source Slot对象。
- `add-character-motion-matching-pose-source`与`refactor-animation-control-boundaries`当前把`PresentationPoseSourceId`放入provider/sample/runtime identity，和本change的Projection-local dense source index冲突，实施时必须同步收口，不能保留双身份。
