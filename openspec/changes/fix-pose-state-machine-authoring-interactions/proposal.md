# Change: 修复Pose StateMachine作者交互与布局闭环

## Why

当前Pose StateMachine虽然显示在共享`GraphAuthoringCanvasView`中，但作者交互没有达到现行共享Graph合同：

- `TreeRectangleSelector`仍把目标写死为`BaseTreeView`。Pose画布实际是`GraphAuthoringCanvasView`，所以左键框选在入口处直接返回。
- `CharacterPoseStateMachinePolicy.PersistsLayout`固定为`false`，共享State View因此主动删除`Capabilities.Movable`。
- `CharacterPoseStateMachineDocument`按数组序号临时生成Entry、State和Alias位置；`CharacterPoseStateMachineDefinition`与Document v3均没有Pose StateMachine layout owner。即使只开放拖动，位置也无法经过Undo、保存、checkout与apply闭合。
- StateMachine子画布此前还依赖Root画布专用尺寸样式，说明同一Graph区域对同级画布的布局责任没有统一收口。

现行`graph-authoring-domain-framework`已经要求BTSMTL与Pose复用selection、框选和StateMachine表面，`refactor-pose-graph-to-btsmtl-authoring-domain`的任务与实施清单也把这些操作标记为已承接；当前代码与这些声明不一致。该问题不能通过增加Pose专用选择器、EditorPrefs位置缓存或只在当前窗口开放拖动解决，否则会产生第二交互路径或不可保存的假编辑。

## What Changes

- 修正唯一共享框选器：
  - 只依赖共享GraphView选择能力，不再要求`BaseTreeView`具体类型。
  - BTSMTL Graph、Pose Graph、Gameplay StateMachine与Pose StateMachine继续使用同一框选实现。
  - 保留普通框选、Shift/Action增选、仅命中Selectable元素和Inspector单选投影语义。
- 完成共享StateMachine移动交互：
  - Entry、State与Alias移动都通过`GraphAuthoringMutationKind.MoveElement`进入领域Mutation。
  - BTSMTL继续写现有Graph节点位置；Pose写新的Pose StateMachine layout owner。
  - 只读或Live Debug状态继续禁止Mutation，但不复制另一套View或手势。
- 为Pose StateMachine增加独立稀疏layout：
  - `CharacterPresentationPoseGraphAsset`保存按`PoseStateMachineId`索引的Entry、State与Alias显式位置，不把位置写入State、Transition或运行语义。
  - 缺少显式位置时使用按稳定identity排序的唯一确定性布局；拖动后只保存被移动元素的位置。
  - layout变化进入Undo、dirty和保存，但不修改Pose StateMachine `ContentRevision`，不使Presentation Projection变为Stale，也不触发Compile或Build。
- 扩展Document v3 Presentation闭包：
  - 每个`editable/presentation/pose-state-machines/<stable-segment>/`同时包含`state-machine.json`与`layout.json`。
  - `state-machine.json`只保存Entry、State、Alias、Transition、Rule与blend/sync语义；`layout.json`只保存稳定元素identity与有限坐标。
  - codec、manifest闭包、canonical writer、hash、exporter、reconciler、typed Presentation Mutation、validator与reverse export同步支持该分片。
  - 工具升级前已checkout的旧闭包必须显式重新checkout；不增加旧闭包reader、缺失文件fallback或双写。
- 收口现有资产与文档：
  - Corin和其它正式Pose StateMachine在未设置显式位置时继续由同一确定性规则显示；作者首次拖动后写入正式layout。
  - 修正旧实施清单中“框选与StateMachine layout已经完全承接”的失实描述。
  - 更新`btsmtl-agent-authoring`当前合同和技能说明，不新增Pose专用MCP action或自动Build。

## Impact

- 影响共享Graph作者交互、Pose StateMachine作者模型、Character Presentation资产、Document v3 Presentation分片、Exporter、Codec、Store、Reconciler、Mutation、Validator与相关文档。
- 不改变Gameplay StateMachine、Pose StateMachine运行选择、Transition Routing、Pose Plan、Animation Runtime、Simulation Program、Rollback状态或网络协议。
- 不自动运行Unity Build、Character Build、Projection Build、Motion Matching Database Build或AI Program Build。
- `BaseTreeView`现有BTSMTL框选、节点拖动、黑板拖拽、Flow/Property Port和Undo行为必须保持不变。

## 与现行Spec对比

- 与`graph-authoring-domain-framework`“复用同一selection、框选和StateMachine表面”的要求一致；本change修复当前实现违约，并把StateMachine移动与layout owner写得更精确。
- `graph-authoring-editor-shell`所称window-local布局只包括区域宽度、折叠、页签和Preview面板状态；本change的节点位置属于Graph作者layout，不与该要求冲突。
- `btsmtl-agent-authoring-document-sync`当前只列出Pose StateMachine的`state-machine.json`，与可持久化拖动目标不一致；本change必须修改为`state-machine.json + layout.json`正式文件对。
- `character-presentation-pose-graph`当前没有Pose StateMachine layout要求；本change新增纯作者layout合同，并明确Compiler与Runtime不读取位置。

