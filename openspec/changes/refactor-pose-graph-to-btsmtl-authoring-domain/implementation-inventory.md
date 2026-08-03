# 实施基线清单

## 已安装的共享Shell

`upgrade-character-animation-authoring-workspace`已经交付：

- `GraphAuthoringEditorShell`拥有Toolbar、Navigator、Graph Canvas、Details与Bottom Dock五区装配。
- `GraphAuthoringWorkspaceLayoutState`只保存Editor布局、折叠与活动页。
- `GraphAuthoringDomainAdapters`装配document、node catalog、port policy、mutation、Inspector、diagnostics、Navigator和Bottom Dock。
- Shell已经统一搜索入口、domain clipboard envelope、selection发布、Undo回调、breadcrumb宿主与显式重操作按钮样式。
- Shell没有统一GraphView、Node View、Port View、StateMachine View与Details字段模型。

## BTSMTL作者入口

| 职责 | 当前正式入口 |
|---|---|
| 窗口与页面栈 | `BaseTreeWindow` |
| Canvas与selection | `BaseTreeView` |
| 结构Mutation与clipboard | `TreeGraphMutationService` |
| Node View | `BaseNodeView`及其特化View |
| Flow Port | `BasePortView` |
| Property Port | `PropertyPortView`、`VariablePropertyPortView` |
| Flow Edge | `BaseEdgeView` |
| Property Edge | `PropertyEdgeView` |
| Details | `BaseTreeInspectorView`、`SubTreeInspectorView` |
| Data Catalog | `GraphDataCatalog`及source/view state |
| Domain adapter | `BtsmtlGraphAuthoringAdapters` |
| StateMachine数据 | `StateMachineGraph`、`StateNode`、`BaseEdge.ConditionRuleGraph` |
| StateMachine视觉 | `BaseNodeView`、`BaseEdgeView`和`BaseTreeInspectorView`中的特化分支 |
| StateMachine Mutation | `TreeGraphMutationService`与edge/node正式authoring API |
| StateMachine Compiler | Character Frontend的BTSMTL StateMachine emitter与ConditionRule compiler |

`BaseTreeView`和`BaseNodeView`当前直接依赖`BaseGraph/BaseNode/BaseEdge/PropertyPort`，因此只能作为共享交互内核的提取来源，不能作为Pose正式数据基类。

## BTSMTL UI抽象基线

本change共享UI的实现来源固定为上述BTSMTL正式入口。允许提取domain-neutral合同、移动代码、重命名类型和倒置依赖，但以下能力必须从现有代码迁入同一个共享实现，不得用新写的简化视图替换：

| 现有业务操作 | 提取来源 | 共享后输入 | 共享后输出 |
|---|---|---|---|
| 窗口分区、页面栈与恢复 | `BaseTreeWindow` | document page与显式authoring context | 当前页面、breadcrumb与轻量窗口状态 |
| Graph加载、刷新与selection | `BaseTreeView` | document projection | 唯一GraphView元素与stable selection |
| 节点显示与特化交互 | `BaseNodeView`及特化View | capability、node projection与presenter | 原有节点内容、端口和命令 |
| Flow/Property Port与Edge | `BasePortView`、`PropertyPortView`、对应Edge View | port/edge projection与domain policy | 原有连接手势与typed mutation |
| 黑板变量拖拽 | `GraphDataCatalog`、Blackboard source、拖拽工厂与变量Node View | 正式Blackboard catalog item | BTSMTL变量节点与Property Port mutation |
| 搜索与创建 | 现有Node Search、attribute与Graph role过滤 | capability与当前document role | 本领域正式create mutation |
| 框选、复制粘贴与Undo | `BaseTreeView`现有交互 | stable selection与domain envelope | 本领域mutation与Unity Undo |
| Inspector与子树Details | `BaseTreeInspectorView`、`SubTreeInspectorView` | selection、capability与owner | typed field/command mutation |
| Navigator与下钻 | Data Catalog、页面栈、SubTree/StateMachine/Condition Rule入口 | owner/reference projection | 打开唯一正式owner |
| Live与diagnostics | 现有runtime overlay与diagnostics | 匹配revision的只读trace | 原有Live Debug显示，不产生mutation |

共享化必须先让BTSMTL通过binding继续使用这些提取后的同一实现，再允许Pose接入。任何一项没有明确承接位置时，不得切换`BaseTreeAsset`入口，不得删除对应原类型。

此前新建替代式`GraphAuthoringCanvas/Node/Port/Details/Navigator/StateMachine Surface`并切换BTSMTL入口的做法不符合本基线，不能作为4–7或11的完成依据。可复用的domain-neutral合同需逐项对账保留；替代式视觉实现必须在恢复BTSMTL后删除。

### 纠偏后的保留与删除分类

当前恢复后的正式BTSMTL入口重新使用`GraphAuthoringEditorShell -> BaseTreeView -> BaseNodeView/BasePortView/BaseTreeInspectorView`。新增代码按以下边界处理：

| 分类 | 文件/类型 | 处理 |
|---|---|---|
| 保留并作为抽象输入 | `GraphAuthoringDomainContracts.cs` | 保留stable identity、document projection、selection、mutation request与diagnostics合同 |
| 保留并作为抽象输入 | `GraphAuthoringCapabilityCatalog.cs` | 保留唯一capability语义目录 |
| 保留并作为抽象输入 | `GraphAuthoringStateMachineContracts.cs` | 保留StateMachine projection、policy与mutation合同 |
| 继续审计后归入binding | `BtsmtlSharedAuthoringWorkspaceRegistry.cs` | 不拥有视觉实现；只允许保留document/capability/mutation/presenter装配 |
| 已删除的替代视觉实现 | `GraphAuthoringCanvas.cs`中的`GraphAuthoringCanvas/NodeView/PortView` | 已删除；Pose改为直接使用`GraphAuthoringCanvasView`，projection presenter位于`GraphAuthoringProjectionCanvas.cs` |
| 已删除的替代视觉实现 | `GraphAuthoringDetailsView.cs` | 已删除；typed字段投影由`GraphAuthoringDetailsPresenter`挂载到唯一`GraphAuthoringDetailsHostView` |
| 已删除的替代视觉实现 | `GraphAuthoringNavigatorView.cs` | 已删除；Data Source由`GraphAuthoringNavigatorPresenter`挂载到唯一`GraphAuthoringNavigatorHostView` |
| 已删除的替代视觉实现 | `GraphAuthoringStateMachineSurface.cs`与`GraphAuthoringStateMachineDetailsView.cs` | 已删除；StateMachine通过`BindStateMachine`进入同一个`GraphAuthoringCanvasView`，Details挂载到唯一Details宿主 |
| 已删除的替代视觉实现 | `GraphAuthoringBottomDock.cs` | 已删除；tab/catalog合同由`GraphAuthoringBottomDockPresenter`挂载到共享Bottom Dock区域 |

`CharacterPresentationPoseGraphEditor`已经不再引用上述替代视觉类型。Pose与Pose StateMachine直接使用从`BaseTreeView`原地提取的`GraphAuthoringCanvasView`，并分别提交typed Presentation Mutation；`BaseTreeWindow`继续使用同一Canvas、Node、Port与Edge基础实现。

### 原BTSMTL操作所有权与提取位置

| 业务操作 | 当前输入 | 当前处理 | 当前输出 | 共享提取位置 | 原类型删除条件 |
|---|---|---|---|---|---|
| 窗口五区、折叠、恢复与显式命令 | `BaseTreeAsset`、`GraphAuthoringWorkspaceDescriptor`、Editor窗口状态 | `GraphAuthoringEditorShell.CreateGUI/ConfigureWorkspace/InitializeLayout` | Toolbar、Navigator、Canvas、Details、Bottom Dock与Editor-only layout state | `GraphAuthoringEditorShell`保持唯一Shell；`BaseTreeWindow`只提供BTSMTL页面与binding | `BaseTreeWindow`不再拥有通用区域代码且BTSMTL页面行为全部保留 |
| 页面栈、breadcrumb与窗口恢复 | root tree、引用Graph、serialized owner/path/authoring id | `BaseTreeWindow`在Shell初始化前只保留精确root打开请求；空Shell只建立Inspector宿主，不读取尚未存在的document binding；Shell就绪后由`TreeWindowNavigationController.ReplaceRoot/Push/Pop/TryRestore/RefreshToolbar`唯一处理并通过同一次`RebindGraphAuthoringDocument`绑定Canvas、Details与Data Catalog，显式root请求不再被旧窗口恢复覆盖 | 当前`BaseTree`、page stack、breadcrumb和错误状态 | 提取为共享page projection/navigation controller，BTSMTL binding保留Tree解析 | BTSMTL引用解析、restore诊断与页面所有权全部由binding承接 |
| Graph加载、清空与刷新 | `BaseTree` | `BaseTreeView.PopulateView/ClearView`创建Node、Flow Edge、Property Edge、Stack、Group | 单一GraphView元素集合 | 在`BaseTreeView`原实现中把数据遍历改为document projection，保留创建顺序和GraphView生命周期 | 共享Canvas可投影BTSMTL全部元素且`BaseTreeView`只剩空壳 |
| 节点搜索与创建 | 当前document role、`GraphAuthoringNodeCatalogEntry`、鼠标位置 | `GraphAuthoringNodeSearchProvider`与Shell `CreateNode`先按`BaseTree.CanCreateNodeType + Capability.Allows(documentRole)`取交集，BTSMTL adapter再调用`BaseTreeView.CreateNode`；`ValueNode`正式role覆盖原BTSMTL允许的BaseTree、RunnableTree、SubTree、StateBehaviorSubTree与ConditionRuleGraph | `TreeGraphMutationService.CreateNode -> CapabilityCatalog.Require(domain, role) -> BaseTree.ApplyModify -> BaseTree.CreateNode` | Shell搜索宿主保留；capability替换类型目录，BTSMTL create handler保留正式`BaseTree` mutation | 原搜索过滤、NodePath分组和Graph role结果均由capability复现；菜单、Data Catalog与Mutation不得使用分裂的role判断 |
| 节点视觉与特化交互 | `BaseNode`、`BaseTreeWindow`、Node字段与状态 | `BaseNodeView`及特化View生成标题、面板、Flow/Property Port、折叠、状态和context menu | GraphView Node与特化作者命令 | 从`BaseNodeView`提取共享Node基座和presenter插槽；BTSMTL presenter继续调用原特化逻辑 | 全部特化View已有明确presenter或domain view承接 |
| Flow Port与Flow Edge | `NodePort`、`BaseEdge` | Capability的Flow input/output direction与capacity统一从节点正式`GetSupportedFlowPortDeclarations`投影；`BasePortView`拖线；`BaseTreeView.GetCompatiblePorts`调用BTSMTL port policy；`TreeGraphMutationService.Link/Unlink` | `BaseTree.Link/UnLink`与`BaseEdgeView` | 从`BasePortView/BaseEdgeView`提取手势和视觉，BTSMTL policy/mutation保留类型语义 | Flow连接、`StateIn`多连接、Transition/BT condition edge删除确认和rule ownership全部承接 |
| Property/Variable Port与Edge | `PropertyPort`、字段访问器、可接受类型 | `PropertyPortView/VariablePropertyPortView`、`BaseNodeView.GeneratePropertyPorts`、`TreeGraphMutationService.LinkProperty` | `BaseTree.LinkProperty/UnLinkProperty`与变量类型解析 | 共享Port基座提供外壳；BTSMTL presenter/policy保留PropertyPort具体类型和动态列表行为 | Property Port生成、排序、类型推断、连接回调和Edge显示全部承接 |
| 黑板Data Catalog拖出变量节点 | `GraphDataCatalogEntry`中的`BaseExposedProperty`、当前context generation和`BaseTreeView` | `GraphDataCatalogEntryView.PerformDrag -> BlackboardGraphDataCatalogSource.TryCreateNode -> BlackboardGraphDataNodeFactoryRegistry.TryCreate` | `BaseTreeView.CreateNode`后绑定正式declaration，生成`ExposedPropertyNode`或领域工厂节点 | 从现有`GraphDataCatalog`与Entry View提取Data Source/drag request；BTSMTL factory继续拥有变量节点类型 | 原手势、filter、scope/context、factory选择、Property Port和declaration binding全部承接 |
| Character Input Data Catalog投影与拖出 | `CharacterPipelineAuthoringContext.InputProfile`中的Input Value、Action Request稳定身份与当前Graph role | `CharacterInputGraphDataCatalogSource`即时投影只读条目；节点类型只通过`BtsmtlGraphAuthoringCapabilities.TryResolveInputValueCapability/TryResolveActionRequestCapability`解析；条目可拖状态先检查同一Capability domain、document role和`BaseTree.CanCreateNodeType`，拖出再提交`IBtsmtlNodeCreationPayload`到`BaseTreeView.CreateNode`唯一Mutation | 创建当前Capability与Graph共同允许的Input Value或Action Request节点，并在同一Mutation内调用`BindInputValue/BindActionRequest`绑定稳定身份 | Input source继续作为唯一Graph Data Catalog领域provider；节点种类、role、port与Mutation均来自共享Capability，不恢复独立Input面板或旧硬编码节点工厂 | `MoveAxis`、`LookAxis`、`Attack`、`Dodge`在RunnableTree可见且可拖；不适用role只显示不可用原因而不会在drop时抛异常；缺失Definition/Profile有显式状态；旧`CharacterInputInfoNodeFactory`保持删除 |
| InputAction资产拖入 | Unity DragAndDrop对象、当前`BaseTree` role | `InputActionNodeDragFactory.CanCreateFromDrag/CreateFromDrag`按control type选节点 | 创建并绑定Button/Vector2/Float InputAction节点 | 抽成BTSMTL Data Catalog/drag provider，不进入通用Canvas业务switch | 三种control type、批量布局、role拒绝和BindAction全部承接 |
| 框选与selection | GraphView pointer/key事件 | `TreeRectangleSelector`面向共享`GraphView`、GraphView selection、Shell `PublishSelection` | stable选择集合与Inspector选择 | 单一selector服务BTSMTL与Pose画布；不再要求`BaseTreeView`具体类型 | BTSMTL与Pose框选命中、增选和Inspector发布一致 |
| 复制粘贴 | GraphElement selection | Shell `BindClipboard`封装domain envelope，BTSMTL mutation adapter调用`TreeGraphMutationService.Serialize/CanPaste/Paste` | 同领域`CopyPasteHelper`重建节点/边；跨领域拒绝 | Shell envelope保留，Graph元素codec由domain binding提供 | BTSMTL Node/Group/Stack/Edge复制语义与跨domain拒绝全部承接 |
| 删除、移动与layout | `GraphViewChange` | `TreeGraphMutationService.ApplyGraphViewChange`处理edge创建、元素删除确认和moved element转发；Node/Stack/Group `OnMoved`写位置 | `BaseTree.ApplyModify`、owner dirty与editor-only位置 | 提取通用change分类；具体删除、condition rule清理和layout写入由BTSMTL mutation handler负责 | 删除确认、owned rule清理、Stack/Group位置和Undo粒度全部承接 |
| Undo与保存边界 | 人工Graph操作、Unity Undo | `BaseTree.ApplyModify`记录操作；Shell监听`Undo.undoRedoPerformed`并调用adapter reload；`BaseTreeWindow.SetCurrentTreeDirty`只标记当前owner | Unity标准Undo、Graph重载、dirty owner，不自动Build | 共享transaction接口只定义粒度；BTSMTL adapter继续使用正式ApplyModify | 不出现第二Undo service，全部原操作名称与owner闭合 |
| Inspector与Details | Graph selection、authoring context、visible blackboard sources | `BaseTreeInspectorView/TreeSelectionInspectorController/SubTreeInspectorView` | 原选择页、Blackboard、Graph设置与特化字段修改 | 从现有Inspector提取section host；BTSMTL details provider复用原控件和`AuthoringPageOpenRegistry` | 所有Node/Edge/Graph特化字段与命令均由provider承接 |
| SubTree/StateMachine/Condition Rule下钻 | NodeGraphReference、BaseEdge rule graph、inline/shared ownership | `TreeWindowNavigationController.Push`、`AuthoringPageOpenRegistry`、`BaseEdgeView.OpenConditionRuleGraph` | 同窗口页面栈与明确owner页面 | 共享child-surface request只传identity；BTSMTL binding解析实际Graph与ownership | inline-first、shared asset、删除确认、breadcrumb和返回行为全部承接 |
| Live Debug与diagnostics | `RuntimeDebugSession`、Graph/Node/Edge authoring identity | `TreeWindowRuntimeOverlayController`解析binding并写`BaseNodeView/BaseEdgeView`只读状态 | Live模式只读覆盖、follow/filter/status | 抽成共享trace projection与overlay host；BTSMTL presenter保留节点/边显示 | Live模式mutation禁用、revision诊断、overlay清理和原显示全部承接 |

共享Details当前由`GraphAuthoringDetailsPresenter`直接挂载到原`BaseTreeInspectorView`的`selection-inspector-container`，Pose窗口只增加`GraphAuthoringDetailsRegion`作为同一原始VisualTree宿主。Authoring只枚举当前Capability声明的字段；Live与References只读；Diagnostics默认折叠。普通面板不再投影Node/Port identity、ContentRevision、compiler index、runtime handle、generated path或Projection中间数据。BTSMTL Timeline以及StateMachine/State仍保留原特化操作入口；Timeline写入已经typed，StateMachine/State字段与按钮由第6阶段继续收口。

Timeline特化入口的布局与按钮保持不变，但写入已经改为`BaseNodeView.ExecuteAuthoringCommand -> GraphAuthoringMutationRequest.ExecuteCommand -> BtsmtlSharedNodeCommandBinding -> TimelineNode.ConfigureAuthoring/ConfigureSharedAuthoring`。`TimelineAuthoringCommands.UseInline/UseShared`登记在Timeline Capability；需要`TimelineAsset`参数的命令标记为Custom presentation，避免通用Details生成无参数重复按钮。StateMachine/State特化入口继续由第6阶段单独收口。

StateMachine继续使用原BTSMTL节点、端口、Edge和拖线视觉；共享`GraphAuthoringCanvasView`把StateMachine结构事件与Entry、State、Alias移动统一分类为领域Mutation。Entry重连仍是原拖线操作，处理后重投影原画布；普通Graph不进入该分支。Pose StateMachine使用同一`GraphAuthoringCanvasView`与`GraphAuthoringNodeViewBase/GraphAuthoringPortViewBase/GraphAuthoringEdgeViewBase`，只由Presentation policy提供blend、sync、readiness和Pose rule payload。普通Pose、Transition Rule和StateMachine Transition共用同一个`GraphAuthoringDetailsRegion`，不再创建第二棵Details VisualTree。

Gameplay policy在binding时要求Gameplay semantic和`BtsmtlGameplayTransitionPayload`，并验证StateMachine Node、State Behavior和Condition Rule的inline-first/shared ownership闭包。Pose policy要求Pose semantic和`CharacterPoseTransitionPayload`，验证唯一root owner、flat graph catalog与state-local `PoseGraphId`；两侧payload不能互换。

Layout写链已经收敛为唯一typed入口。GraphView的普通拖动、右键对齐、Group移动、Stack移动与Stack子节点重排统一进入`BaseTreeView.CommitMovedElements -> TreeGraphMutationService.ForwardMovedElements -> GraphAuthoringMutationRequest.MoveElement -> BtsmtlSharedGraphMutation.MoveElement`；`BaseNodeView/NodeGroupView/StackNodeView`不再直接调用`ApplyModify`写位置。Pose Graph由`GraphAuthoringProjectionCanvas.ApplyGraphViewChange -> CharacterTypedPoseGraphMutationAdapter -> MovePoseNodeMutation`写`CharacterTypedPoseGraph.Layout`；Pose StateMachine由共享移动分类进入`CharacterPoseStateMachineMutationAdapter -> SetPoseStateMachineLayoutElementMutation`，写根Pose资产的独立稀疏layout catalog。三类layout都只影响作者投影，Compiler与runtime不读取位置。原View直写位置路径已删除，Undo仍由同一领域mutation transaction记录。

BTSMTL迁移后的输入是原`BaseTreeAsset`打开入口、原Node Search、原黑板拖拽、原GraphView selection与原Live会话；处理依次经过`BaseTreeWindow -> GraphAuthoringEditorShell`、`BtsmtlGraphAuthoringNodeCatalogAdapter`的Capability过滤、`BaseTreeNavigatorView/GraphDataCatalogController`、共享clipboard envelope、typed mutation以及`BtsmtlSharedGraphDiagnostics`；输出仍是原窗口布局、节点/端口/Edge、Details、Navigator和Live覆盖。共享`GraphAuthoring*`类型中已无BTSMTL/Pose业务类型switch；`BaseTreeWindow/BaseTreeView/BaseNodeView`仍承接未抽空的BTSMTL特化行为，因此7.18–7.20与17.10–17.11继续保持未完成，不提前删除。

Pose工作区的输入是当前typed Pose document、Capability与Presentation owner；创建菜单、Node/Port/Edge渲染、拖线和clipboard全部由同一`GraphAuthoringCanvasView`处理，写入`CharacterTypedPoseGraphMutationAdapter`。Navigator与Producer Catalog使用从原`BaseTreeNavigator` VisualTree提取的`GraphAuthoringNavigatorPresenter`，Pose Graph、Transition Rule、Pose Source、Motion Matching Provider和Action Producer由同一data source投影。PoseStateMachine使用同一Canvas实现与StateMachine policy；Preview、Pose Watch和Live Debug只通过`GraphAuthoringBottomDockCatalog`注册。revision不匹配由`CharacterPoseRuntimeTraceProjection`与Preview/Watch panel输出`Stale`，Live开关同时把Pose、StateMachine和Transition Rule mutation设为只读。

Action Animation Workspace重新对账后的输入是精确Definition与Action identity；`ActionAnimationAuthoringWorkspaceResolver`只解析ActionProfile、Gameplay call site、Timeline、Presentation producer和AnimationSlot正式owner。窗口复用原`BaseTreeWindow` VisualTree区域、`GraphAuthoringDetailsHostView`和`GraphAuthoringBreadcrumbHost`，中央只嵌入正式`TimelineEditorView`，不建立第二Graph或Timeline数据。Track、Clip、TreeClip、Marker与Curve写入仍由Timeline adapter和Timeline owner Undo处理；Action/Profile/Pose信息在Workspace中只读并导航到正式owner。`BaseNodeView.ExecuteAuthoringCommand -> TimelineAuthoringCommands -> BtsmtlSharedGraphMutation -> TimelineNode.ConfigureAuthoring/ConfigureSharedAuthoring`保持唯一typed owner mutation；Preview、Live、selection、打开窗口和owner解析都不触发Build。

旧Window/View通用交互路径已删除。`GraphAuthoringEditorShell`唯一拥有VisualTree区域、搜索宿主、clipboard envelope、selection发布、Undo回调、breadcrumb与layout state；`GraphAuthoringCanvasView`唯一拥有GraphView生命周期、通用selection/read-only和Projection/StateMachine交互；`GraphAuthoringNodeViewBase/GraphAuthoringPortViewBase/GraphAuthoringEdgeViewBase`唯一拥有基础视觉与只读能力。保留的`BaseTreeWindow/BaseTreeView/BaseNodeView`只处理BTSMTL页面解析、黑板/Stack/Group、Flow/Property Port、字段面板和原特化节点，不再复制共享Shell、Pose Canvas或Details基础实现。

以上每行均以“共享实现承接同一输入并产生同一业务输出”为删除条件。只建立接口、只显示相似节点或只让代码编译不算承接。

## Pose作者入口与重复实现

`CharacterPresentationPoseGraphEditor.cs`当前集中拥有：

- `PoseGraphNodeView`、`PoseGraphView`。
- `PoseStateMachineNodeView`、`PoseStateAliasNodeView`、`PoseStateEntryNodeView`、`PoseTransitionRuleOperationView`。
- `PoseGraphDocumentAdapter`、`PoseGraphNodeCatalogAdapter`、`PoseGraphPortPolicyAdapter`、`PoseGraphMutationAdapter`。
- `PoseGraphNavigatorAdapter`、`PoseGraphBottomDockAdapter`、`PoseGraphInspectorAdapter`、`PoseGraphDiagnosticsAdapter`。
- Pose clipboard DTO、port value marker、preview viewport、Pose Watch viewport和compiled product cache。

Pose数据由`CharacterPresentationPoseGraphAsset -> CharacterPoseGraphData -> CharacterPoseNodeDefinition[] + CharacterPoseEdge[]`进入Editor。`CharacterPresentationPoseGraphAuthoringService`是现有Pose结构写入口；Profile、Policy、Pose source和StateMachine还分别存在于Profile Inspector与专项authoring/migration service。

## Pose联合体字段占用

| 节点能力 | 联合体中实际使用的主要字段 |
|---|---|
| ProgramParameterInput | ParameterId |
| SelectedPosePlayer | ProviderId、PoseSourceId、SelectionAvailability |
| BlendSpacePlayer | ProviderId、PoseSourceId、InputRangePolicy、Player状态 |
| SequencePlayer | ProviderId、PoseSourceId、Loop、PlayRate、InitialTime、ResetOnEntry |
| PoseStateMachine | PoseStateMachineDefinition |
| ActionPlaybackInput | AnimationChannelId |
| AnimationSlot | AnimationChannelId、AnimationSlotId、BlendPolicy、SelectionAvailability |
| BlendStack | BlendPolicy |
| Inertialization | InertializationPolicy |
| BlendPose | Weight及动态Pose输入 |
| LayeredBoneBlend | BoneMask、Weight |
| AdditivePose | ReferencePoseId、ReferenceSpace、ScalePolicy、Weight |
| PoseParameterResolve | ParameterPolicies |
| ModifyBone | BoneId、ReferenceSpace、OperationMask、Position、Rotation、Scale |
| TwoBoneIK | physical end bone、effector、joint target、offset、end rotation mode、Weight |
| FootPlacement | Profile、Calibration |
| PoseSubgraph | Subgraph reference |
| GraphInput/GraphOutput | interface port集合 |
| OutputPose | Pose输入 |

所有节点还重复携带`NodeId`、`Kind`、`DisplayName`、`Position`和固定`Ports`。联合体总是序列化其它节点不使用的字段，是本change必须删除的数据源。

## Pose Compiler与Runtime映射

`CharacterPresentationPoseGraphCompiler`当前负责拓扑排序、incoming edge索引、节点kind分派、StateMachine展开、source/parameter/Rig binding、workspace规划和Native operation生成。直接Runtime operation映射为：

- `ProgramParameterInput -> ProgramParameterInput`
- `SelectedPosePlayer -> SelectedPosePlayer`
- `BlendSpacePlayer -> BlendSpacePlayer`
- `SequencePlayer -> SequencePlayer`
- `PoseStateMachine -> PoseStateMachine`
- `ActionPlaybackInput -> ActionPlaybackInput`
- `AnimationSlot -> AnimationSlot`
- `BlendStack -> BlendStack`
- `Inertialization -> Inertialization`
- `BlendPose -> BlendPose`
- `LayeredBoneBlend -> LayeredBoneBlend`
- `AdditivePose -> AdditivePose`
- `PoseParameterResolve -> PoseParameterResolve`
- `ModifyBone -> ModifyBone`
- `TwoBoneIK -> TwoBoneIK`
- `FootPlacement -> FootPlacement`
- `OutputPose -> OutputPose/StatePoseOutput`

`GraphInput`、`GraphOutput`和`PoseSubgraph`在静态Graph展开阶段消失。Runtime operation enum与dispatch可以保留，但节点校验和降低必须迁入独立handler。

## Capability生产与消费

`AgentAuthoringCapabilityCatalog`当前是唯一相对完整的BTSMTL稀疏节点目录。直接消费者包括Package Mapper、Document Reconciler、Graph/Node/State Mutation handler、Mutation Session、Graph Validator、Character/AI exporter。

人工BTSMTL创建菜单仍主要依赖C#类型、attribute和Graph role；Pose创建菜单、port和Details在Pose Editor内另有kind switch。目标是把`AgentAuthoringCapabilityCatalog`改成共享`GraphAuthoringCapabilityCatalog`的Document投影，不能继续保存第二份节点能力表。

## 迁移前Document v2只读Presentation链（已删除）

| 层 | 迁移前行为 |
|---|---|
| Schema | `AgentAuthoringSchema.Version = btsmtl-agent-authoring-document.v2` |
| Exporter | Character Snapshot递归读取Profile、Pose Graph、StateMachine和source binding |
| Package Mapper | 只把Presentation投影到`context/dependencies.json` |
| Codec/Store | manifest闭包没有`editable/presentation/**` |
| Reconciler | 不为Presentation生成Mutation |
| Validator | 只验证Presentation引用和generated状态 |
| MCP | checkout/dry-run/apply说明明确写Presentation只读 |
| Skill | 主流程和current-contract都禁止Presentation Mutation |

该表只保留迁移输入证据。正式实现已经删除v2 schema、codec、manifest/service分支、旧operation入口和Presentation只读限制；v3直接复用唯一Store、strict package、Application Service与五工具生命周期，没有复制基础设施。

## Presentation正式写入口

- Pose topology：`CharacterPresentationPoseGraphAuthoringService`。
- PoseStateMachine：`CharacterPoseStateMachineAuthoringService`及Pose Editor mutation。
- Profile source与Rig/Analysis引用：`CharacterAnimationPresentationProfileEditor`及对应authoring service。
- Blend/Inertialization Policy：Profile Editor、Policy asset Editor和现有迁移service。
- AnimationSlot：Pose Graph authoring service与Inspector分支。
- Document：v3 Reconciler通过同一typed Presentation Mutation写入正式owner。

最终只保留领域typed Presentation Mutation；人工UI和Document Reconciler均调用它。

## Corin正式依赖闭包

根为`CorinCharacterPipelineDefinition`，Presentation闭包包括：

- `Pipeline/Presentation/Profiles/CorinAnimationPresentationProfile.asset`
- `Pipeline/Presentation/PoseGraphs/CorinPresentationPoseGraph.asset`
- `Pipeline/Presentation/Rig/CorinAnimationRigDefinition.asset`
- `Pipeline/Presentation/Blend/Action/CorinActionBlendPolicy.asset`
- `Pipeline/Presentation/Blend/Action/CorinActionBlendProfile.asset`
- `Pipeline/Presentation/Blend/Locomotion/CorinLocomotionInertializationPolicy.asset`
- `Pipeline/Presentation/Blend/Locomotion/CorinLocomotionInertialBlendProfile.asset`
- `Pipeline/Presentation/FootPlacement/`下的正式Foot Placement Profile、Analysis Source、Analysis Rig与Rig Calibration
- `Pipeline/Presentation/Sources/Actions/`下由Action Timeline引用的Attack与Dodge source
- Profile引用的Pose source、有限Action Timeline/Track/AnimationChannel与Foot Analysis输入
- generated Presentation Projection、Float32 Simulation Program和Definition wrapper引用

迁移顺序固定为Profile/Pose资产事务、显式Projection/Program Build、Definition generated引用更新、旧产物删除、Document v3重新checkout。

## 文件聚合与残留资产收口（2026-08-01）

### 输入

- 平铺在`Editor/CharacterPipeline`根下的44个Editor实现。
- 平铺在`Editor/CharacterSimulation`根下的46个Editor实现。
- 平铺在`Runtime/Character/Pipeline/Animation/Contracts`根下的62个合同实现。
- 分散在Corin Character、Gameplay Lab、Deterministic Rollback和Character Runtime Profile根目录的正式资产。
- 全项目GUID与精确路径扫描确认不可达的14个旧Action/Dodge/Rush RootMotion Curve、7个旧顶层Locomotion source和1个旧FullBody Mask。

### 处理

- 源文件与对应`.meta`作为一个单元移动，保持Unity GUID不变。
- Character Pipeline Editor按Authoring、Inspector、Analysis与Diagnostics职责聚合；Character Simulation Editor按Compilation、Build、Analysis、Navigation、Network Product与Inspector聚合；Animation Contracts按业务合同聚合。
- Corin资产按AI、Motion、Presentation和Simulation业务owner聚合；产品资产按Composition、Pipeline、Program、World、Network与运行用途聚合。
- 全部硬编码AssetDatabase路径、产品启动路径和资源收集路径直接改为新路径，不保留旧路径查找或兼容分支。
- Unity刷新编译后，通过正式Document v3 checkout重新生成canonical目录包，并执行同一Definition的语义验证。

### 输出

- `Assets/GameScripts/Main/Editor/CharacterPipeline/{Authoring,Inspectors,Analysis,Diagnostics,AgentAuthoring,MotionMatching,RootMotion}`。
- `Assets/GameScripts/Main/Editor/CharacterSimulation/{Compilation,Build,Analysis,Navigation,NetworkProducts,Inspectors,Fixed}`。
- `Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/Contracts/{Action,Blend,Rig,Pose,Sources,Projection,Workspace,Common}`。
- `Assets/Configs/Character/Corin/Pipeline/Presentation/{Profiles,Blend,Rig,FootPlacement,PoseGraphs,Sources}`及`Motion/{Profiles,Turning}`。
- `Assets/Configs/Simulation/GameplayLab/{Compositions,Pipelines,Sources,Variants}`。
- `Assets/Configs/Simulation/DeterministicRollback/{Compositions,Networking,Pipelines,Programs,World}`。
- `Assets/Prefabs/Characters/RuntimeProfiles/{Local,AI,Rollback,ServerAuthoritative}`。

### 删除的旧路径

- 删除`Pipeline/Motion/Curves`中的Attack1至Attack5、DodgeBack、DodgeForward、RushAttack及对应End旧RootMotion Curve，共14个资产。
- 删除`Pipeline/Presentation`根下Idle、WalkStart、WalkLoop、MovingTurn、RunStart、RunEnd、RunLoop旧Locomotion source，共7个资产。
- 删除旧`CorinFullBodyActionMask.asset`；FullBody Action由Pose Graph的AnimationSlot和正式Rig/Blend合同处理。
- 删除确认空且不再拥有业务的旧`Pipeline/Animation`、`Pipeline/Motion/Curves`、`Presentation/BlendCurves`、Runtime `Animation/Presentation/Animancer`与Editor `CharacterSimulation/DeterministicRollback`目录。

### 闭合结果

- Unity刷新后脚本编译无错误。
- 通过正式Gameplay Lab Local Fixed入口启动运行时，依次注入W、S、鼠标左键与D+LeftShift；角色位置从`(2.96,0.00,-5.27)`依次变化到`(2.96,0.00,-2.41)`、`(2.96,0.00,-2.65)`、`(2.26,0.00,-2.24)`和`(7.74,0.00,-2.90)`，证明移动、后向转身输入、攻击和闪避均经过正式输入链。运行期间产品Error与Warning均为0，验证结束后临时探针程序集和`.meta`已删除。
- Definition checkout回到`Clean`，Document schema为v3，canonical反向导出不再包含旧资产路径。
- Definition validator通过，Float32 Program、Presentation Projection与Pose依赖仍由同一Definition闭包解析。

## Active change关系

- `refactor-agent-authoring-to-synced-json-document`提供Store、strict package、Reconciler、Application Service与五工具；本change升级v3并删除v2。
- `refactor-animation-control-boundaries`的Presentation代码合同保留，其Corin资产迁移与发布任务由本change的新Pose authoring和Document v3承接。
- `add-action-animation-authoring-workspace`必须消费共享Canvas、Details、Data Catalog和Presentation Mutation。
- Blend Space、Virtual Bone和Motion Matching的Pose能力必须注册进唯一catalog，不得再扩展Pose Editor kind switch。

## 删除清单

- `PoseGraphView`、`PoseGraphNodeView`及Pose专用基础port/selection/clipboard/Undo实现。
- Pose专用Node Catalog、固定Port Policy和Inspector大switch。
- `BaseTreeWindow/BaseTreeView/BaseNodeView`中已经从原代码提取到唯一共享实现、由BTSMTL binding继续承接且不再拥有任何特化行为的空壳；禁止直接删除成熟交互实现。
- 此前错误新建、用于替换BTSMTL而不是从BTSMTL原地提取的Canvas、Node、Port、Details、Navigator与StateMachine视觉实现。
- `CharacterPoseNodeDefinition`联合体及逐实例固定port镜像。
- 顶层Pose Compiler的node kind业务字段switch。
- `AgentAuthoringCapabilityCatalog`内部重复语义表。
- Document v2 schema、reader、writer、manifest/service分支和Presentation只读限制。
- 旧Pose Snapshot第二字段模型、旧Patch/Macro/Workbench/兼容菜单和全部fallback。
- 迁移后的旧Pose资产字段、legacy inline carrier与旧generated Projection/Program。
