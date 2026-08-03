# Design: Pose StateMachine作者交互与布局闭环

## Context

共享Canvas已经存在两类StateMachine document：

```text
BTSMTL StateMachineGraph
  -> BaseTreeView / GraphAuthoringCanvasView
  -> BtsmtlStateMachinePolicy(PersistsLayout=true)
  -> MoveElement
  -> 正式Graph节点Position

Pose StateMachineDefinition
  -> GraphAuthoringCanvasView
  -> CharacterPoseStateMachinePolicy(PersistsLayout=false)
  -> 移除Movable
  -> 按数组序号临时排布
```

框选器同样处于半抽象状态：Manipulator挂在`GraphAuthoringCanvasView`构造函数中，却只接受`BaseTreeView`作为实际目标。BTSMTL可进入框选逻辑，Pose画布无法进入。

## Goals

- BTSMTL与Pose使用同一个框选器、StateMachine View和移动事件分类。
- Pose StateMachine的Entry、State与Alias可以拖动、Undo、保存、重新打开并保持位置。
- 人工UI与Document apply写入同一Pose StateMachine layout owner和同一种typed Presentation Mutation。
- layout不进入Pose运行语义、ContentRevision、Projection stale判断或Build链。
- 不增加Pose专用GraphView、EditorPrefs第二真相、自动迁移保存或自动Build。

## Non-Goals

- 不改变State、Transition、Rule、blend、sync或Runtime状态选择。
- 不增加Transition edge控制点、Comment、Group或State颜色能力。
- 不改变BTSMTL Gameplay StateMachine的正式位置数据。
- 不为Action Animation Workspace或普通Timeline新增画布能力。
- 不新增测试；Unity端到端结果由用户在实现完成后自行验收。

## Decision 1: 框选器面向GraphView而不是BaseTreeView

`TreeRectangleSelector`继续作为唯一框选实现，但其目标合同改为共享GraphView能力：

```text
MouseDown
  -> target必须是GraphView
  -> 检查事件是否来自空白画布
  -> 记录GraphView.selection
  -> 遍历GraphView.graphElements
  -> 只选择具备Selectable的可见元素
```

不新增第二个`RectangleSelector`或Pose专用Manipulator。`BaseTreeView`继承同一GraphView合同，因此原BTSMTL行为继续经过相同代码。

### Tradeoff

- 面向`GraphView`：改动小，直接表达真正使用的selection与graphElements能力，BTSMTL和Pose完全共用。
- 新建项目自定义选择接口：抽象层更厚，但当前只有GraphView实现者，没有额外业务价值。
- Pose单独使用Unity `RectangleSelector`：能快速恢复框选，但形成两套手势、增选和过滤语义，不采用。

## Decision 2: Pose StateMachine layout由根Pose资产独立拥有

在`CharacterPresentationPoseGraphAsset`的root-owned authoring数据中增加按`PoseStateMachineId`索引的layout catalog：

```text
PoseStateMachineLayout
  stateMachineId
  elements[]
    elementId
    position.x
    position.y
```

合法`elementId`集合是当前StateMachine的Entry、全部State和全部Alias。Transition位置由端点推导，不进入layout。

layout与`CharacterPoseStateMachineDefinition`分离，原因是：

- 拖动节点不改变动画状态选择语义。
- 不应调用`CharacterPoseStateMachineDefinition.Touch()`。
- Projection Compiler和Runtime不需要也不得读取位置。
- 删除State或Alias时，结构Mutation和layout清理仍可进入同一资产级事务。

### 稀疏位置

layout只保存作者显式移动过的元素。缺失位置不是错误，使用唯一确定性规则：

1. Entry使用固定入口列。
2. State按稳定StateId排序后分配网格位置。
3. Alias按稳定AliasId排序后分配独立入口列。
4. 显式位置覆盖对应元素的生成位置。
5. 未受影响的显式位置不得因新增或删除其它元素而改变。

重复identity、未知identity、NaN或Infinity必须失败；缺失显式位置使用上述正式规则，不属于fallback。

### Tradeoff

- 根资产独立layout catalog：位置可版本控制、可Undo、可由人工与Document共同编辑，且不污染运行语义；需要完整扩展Presentation authoring链。
- EditorPrefs或window-local缓存：不污染资产，但不能随资产和Document共享，换机器或重开工作区会丢失，不采用。
- 把Position直接放入State/Entry/Alias：实现较短，但每次拖动都会污染StateMachine语义revision和Projection stale判断，不采用。

## Decision 3: StateMachine移动统一降低为MoveElement

共享StateMachine surface必须识别被移动的Entry View、State View与Alias View，并为每个稳定identity生成`MoveElement`请求。

```text
GraphView SelectionDragger
  -> GraphViewChange.movedElements
  -> 共享StateMachine移动分类
  -> domain policy确认layout可写
  -> GraphAuthoringMutationKind.MoveElement
      -> BTSMTL StateMachine Mutation
      -> Pose StateMachine Presentation Mutation
```

Pose Mutation只更新layout catalog。结构Mutation承担以下原子约束：

- 新建State或Alias时可以同时接受初始位置。
- 删除State或Alias时删除对应显式位置。
- Entry不可删除，但可以移动。
- Undo恢复结构和layout的同一修改前状态。
- Live Debug只读模式阻止全部Mutation。

## Decision 4: Document v3为Pose StateMachine增加layout.json

规范目录改为：

```text
editable/presentation/pose-state-machines/<stable-segment>/
  state-machine.json
  layout.json
```

`layout.json`使用稀疏结构：

```json
{
  "stateMachineId": "corin.locomotion",
  "elements": [
    { "id": "corin.locomotion.entry", "x": -360, "y": 0 },
    { "id": "corin.locomotion.idle", "x": 0, "y": 0 }
  ]
}
```

约束：

- 文件必须与`state-machine.json`同目录并具有相同StateMachine identity。
- `elements`允许为空或只覆盖部分合法元素。
- unknown、duplicate、非有限坐标严格失败。
- canonical writer按稳定identity排序。
- layout文件进入editable hash、document hash和Conflict判断。
- 纯layout diff降低为typed layout Mutation，不改变StateMachine `ContentRevision`，不发布Projection。

Document schema名称继续使用`btsmtl-agent-authoring-document.v3`。原因是该目录包是本地显式checkout工作副本，manifest本来就锁定精确文件闭包；升级后的旧闭包必须重新checkout生成新规范闭包，codec不兼容读取缺失文件，也不增加v3旧形状reader。为这一项作者布局升级整个Gameplay、Timeline、AI和Presentation包到v4会扩大无业务收益的迁移面。

## Data Flow

### 人工拖动

```text
Pose StateMachine GraphView
  -> MoveElement(elementId, position)
  -> CharacterPoseStateMachineMutationAdapter
  -> typed Pose StateMachine layout Mutation
  -> CharacterPresentationPoseGraphAsset layout catalog
  -> Undo + dirty + save
  -> 不Touch ContentRevision
  -> 不Build
```

### Document apply

```text
state-machine.json + layout.json
  -> strict Presentation codec
  -> AgentAuthoringPresentationReconciler
  -> typed Pose StateMachine layout Mutation
  -> 同一layout catalog
  -> 全域Validator
  -> save + canonical reverse export
```

## Migration

- 新字段为空的现有Pose资产按正式确定性规则显示，不需要隐式保存或自动Build。
- 升级后的首次显式checkout为每个Pose StateMachine发布对应`layout.json`，空显式布局写为`elements: []`。
- 已存在的旧Document闭包在重新checkout前不允许dry-run/apply；返回明确缺失layout文件诊断，不做兼容读取。
- 作者拖动后，新位置通过正常人工Mutation或Document apply写入正式layout。
- 不创建一次性菜单、selection触发迁移、AssetDatabase watcher或窗口打开自动保存。

## Validation

完成实现必须能够从代码链证明：

- `TreeRectangleSelector`不再引用`BaseTreeView`具体类型。
- `CharacterPoseStateMachinePolicy`允许正式layout写入。
- Entry、State、Alias移动都进入同一移动分类。
- Pose layout Mutation不调用StateMachine `Touch()`，不调用Compile或Build。
- Presentation package对每个StateMachine严格包含两个文件。
- Exporter、Reconciler和人工UI引用同一layout owner。
- 旧实施清单不再宣称尚未闭合的交互已经完成。

