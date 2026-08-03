## 唯一实施顺序

以下编号是稳定追踪ID，不表示实施先后。执行必须严格服从`openspec/character-pipeline-serial-execution.md`：

1. 先完成Agent Document v2可复用基础，以及本change的共享合同、Capability、typed payload、Presentation Mutation、Validator、Pose IR、Document v3、事务和迁移计划代码；对应1–3、8–10、12–15中尚未完成的逻辑任务。
2. 再迁移共享Editor表面；对应4–7、11和17中的旧UI删除任务。
3. 再完整实施`add-action-animation-authoring-workspace`，并完成14A全部能力接入门禁。
4. 前三步全部结束后，才允许执行一次Corin Document v3迁移；对应16.1–16.13，并与`refactor-animation-control-boundaries`的20–23、27.1–27.5、27.13–27.15和28合并为同一事务。
5. canonical reverse export回到Clean后，才允许执行16.14–16.16以及动画职责change的26.6–26.11正式产品发布与旧产物删除。
6. 最后完成17中剩余单链清理和18的文档对账，再进入Rollback产品闭包。

任何UI任务不得反向定义领域schema；任何Corin资产任务不得在Action Workspace与14A完成前开始；任何Build不得在Document v3同hash对账前开始。

## 当前UI纠偏与已协商实施口径

此前“新建`GraphAuthoringCanvas/Node/Port/Details/Navigator/StateMachine Surface`并切换BTSMTL入口”的实现方式被判定为错误：它是替代现有BTSMTL UI，不是从现有BTSMTL UI抽象共享能力，并且已经改变黑板变量拖拽等成熟操作。4–7、11中的相关UI任务重新打开；不得用已有错误实现的文件存在作为完成依据。

接下来严格按以下顺序执行：

1. 先停止共享UI扩展，只恢复被错误删除的BTSMTL作者UI源码与可编译状态；不迁移资产，不Build。
2. 以恢复后的`BaseTreeWindow`、`BaseTreeView`、`BaseNodeView`、`BasePortView`、`PropertyPortView`、Edge View、`BaseTreeInspectorView`与`GraphDataCatalog`建立操作所有权清单。
3. 逐项从上述现有实现原地提取domain-neutral合同和交互代码。抽象可以改变类名、程序集和依赖方向，但不得改变窗口分区、节点信息密度、黑板变量拖拽、Flow/Property Port、搜索与创建、selection、框选、clipboard、Undo、Inspector、子树/StateMachine下钻和Live Debug。
4. BTSMTL必须先通过binding继续使用提取后的同一实现；任一原操作尚未映射，不得切换入口，不得删除原类型。
5. BTSMTL闭合后，Pose再通过独立document/capability/mutation/presenter binding接入同一实现；Pose不得继承`BaseGraph/BaseNode`。
6. Action Workspace最后接入同一作者表面。前三者闭合后才删除被抽空的BTSMTL专用壳和错误的新建替代UI。
7. 若抽象必须改变BTSMTL布局、信息密度、拖拽、菜单或操作入口，立即停止并列出同级业务tradeoff，等待用户决定。

### UI恢复门禁

- [x] 0U.1 对账当前错误新建UI、被删BTSMTL UI、`BaseTreeWindow`切换代码和全部编译错误。
- [x] 0U.2 从受版本控制的原内容恢复`BaseTreeWindow`依赖的BTSMTL Canvas、Node、Port、Edge、Inspector、Data Catalog与拖拽源码。
- [x] 0U.3 只通过Unity MCP刷新恢复文件的`.meta`与AssetDatabase。
- [x] 0U.4 移除`BaseTreeWindow`对替代式新Canvas的装配，恢复原BTSMTL窗口操作链。
- [x] 0U.5 恢复Editor可编译状态，且不修改任何Corin资产或generated产品。
- [x] 0U.6 区分可保留的domain-neutral合同与必须删除的替代式视觉实现。

### BTSMTL现有操作抽象门禁

- [x] 0U.7 固定`BaseTreeWindow`区域布局、页面栈、窗口恢复和显式命令入口。
- [x] 0U.8 固定`BaseTreeView`加载、刷新、GraphView生命周期和selection行为。
- [x] 0U.9 固定`BaseNodeView`及全部特化Node View的标题、内容、端口和交互。
- [x] 0U.10 固定Flow Port、Property Port、Variable Property Port及对应Edge行为。
- [x] 0U.11 固定黑板变量Data Catalog、拖拽工厂、变量节点创建和正式BTSMTL mutation链。
- [x] 0U.12 固定节点搜索、创建菜单、Graph role过滤和节点工厂链。
- [x] 0U.13 固定拖线、断线、删除、移动、layout和端口兼容链。
- [x] 0U.14 固定框选、selection同步、复制、粘贴和跨domain拒绝链。
- [x] 0U.15 固定Unity Undo、dirty owner和Editor保存边界。
- [x] 0U.16 固定`BaseTreeInspectorView`、`SubTreeInspectorView`和特化Details行为。
- [x] 0U.17 固定Data Catalog搜索、分组、Navigator、breadcrumb和owner打开链。
- [x] 0U.18 固定SubTree、StateMachine、Condition Rule与Transition edge下钻链。
- [x] 0U.19 固定Preview、Watch、Live Debug与diagnostics只读链。
- [x] 0U.20 为每项既有操作标明共享实现提取位置、BTSMTL binding输入、Mutation输出与删除条件。

### 原地抽象与双领域接入门禁

- [x] 0U.21 从现有BTSMTL实现提取唯一domain-neutral Canvas生命周期，不新写替代GraphView。
- [x] 0U.22 从现有BTSMTL实现提取唯一Node、Port与Edge视觉/交互基座。
- [x] 0U.23 从现有BTSMTL实现提取唯一Details、Data Catalog、Navigator与breadcrumb宿主。
- [x] 0U.24 从现有BTSMTL实现提取唯一StateMachine视觉与交互基座。
- [x] 0U.25 让BTSMTL document/capability/mutation/presenter binding重新接入提取后的同一实现。
- [x] 0U.26 让Pose document/capability/Presentation Mutation/presenter binding接入同一实现。
- [x] 0U.27 让Action Workspace接入同一实现且不拥有第二份authoring数据。
- [x] 0U.28 删除替代式新建Canvas、Node、Port、Details、Navigator和StateMachine视觉实现。
- [x] 0U.29 删除已经抽空的错误替代壳；仍承载BTSMTL特化操作与原UI的binding类型不是空壳，不得为删除类名而破坏原操作。
- [x] 0U.30 对账代码中只有一个Graph Canvas、Node/Port交互、selection、clipboard、Undo和Details基础实现。

## 1. 变更基线与依赖收口

- [x] 1.1 记录`upgrade-character-animation-authoring-workspace`已经交付的Shell能力清单。
- [x] 1.2 记录当前BTSMTL Canvas、Node View、Port View、Details与Navigator实现入口。
- [x] 1.3 记录当前Pose Graph Canvas、Node View、Port View、Details与Navigator重复实现入口。
- [x] 1.4 记录当前BTSMTL StateMachine作者表面的数据、UI、Mutation与Compiler入口。
- [x] 1.5 记录当前PoseStateMachine作者表面的数据、UI、Mutation与Compiler入口。
- [x] 1.6 记录`CharacterPoseNodeDefinition`全部node kind与字段占用关系。
- [x] 1.7 记录Pose Compiler全部node kind分支与Runtime operation映射。
- [x] 1.8 记录`AgentAuthoringCapabilityCatalog`的全部生产者和消费者。
- [x] 1.9 记录Document v2 Presentation只读投影的全部生产者和消费者。
- [x] 1.10 记录Presentation Profile、Pose Graph、PoseStateMachine与Policy的全部现有写入口。
- [x] 1.11 记录Corin正式Presentation资产与generated产物的完整依赖闭包。
- [x] 1.12 固定本change与`refactor-agent-authoring-to-synced-json-document`的先后依赖。
- [x] 1.13 固定本change与`refactor-animation-control-boundaries`未完成Presentation迁移任务的接管关系。
- [x] 1.14 固定本change与`add-action-animation-authoring-workspace`的共享UI消费关系。
- [x] 1.15 建立旧BTSMTL View、旧Pose View、旧联合体、旧Document v2与旧Projection删除清单。
- [x] 1.16 建立`openspec/character-pipeline-serial-execution.md`唯一串行执行基线。
- [x] 1.17 明确Virtual Bone与TwoBoneIK算法不在本change重复实现。
- [x] 1.18 明确BlendSpace与Motion Matching独立内容任务不阻塞Corin与Rollback关键路径。
- [x] 1.19 明确Action Animation Workspace在共享UI和Document v3完成后、Corin资产迁移前实施。
- [x] 1.20 明确本change与动画职责change只执行一次Corin资产事务和一次正式产品Build。

## 2. Graph Authoring Domain Framework基础合同

- [x] 2.1 定义稳定Graph authoring domain identity。
- [x] 2.2 定义稳定Graph document role identity。
- [x] 2.3 定义Graph document只读投影接口。
- [x] 2.4 定义Graph page与breadcrumb投影接口。
- [x] 2.5 定义Graph node投影接口。
- [x] 2.6 定义Graph fixed port投影接口。
- [x] 2.7 定义Graph dynamic port投影接口。
- [x] 2.8 定义Graph edge投影接口。
- [x] 2.9 定义Graph selection identity合同。
- [x] 2.10 定义Graph clipboard domain envelope。
- [x] 2.11 定义Graph create command合同。
- [x] 2.12 定义Graph connect command合同。
- [x] 2.13 定义Graph disconnect command合同。
- [x] 2.14 定义Graph delete command合同。
- [x] 2.15 定义Graph move与layout command合同。
- [x] 2.16 定义Graph field mutation command合同。
- [x] 2.17 定义Graph child surface open command合同。
- [x] 2.18 定义Graph diagnostics投影合同。
- [x] 2.19 定义Graph runtime trace投影合同。
- [x] 2.20 把框架合同放入BTSMTL authoring程序集而非Presentation runtime程序集。

## 3. 唯一Authoring Capability Catalog

- [x] 3.1 定义capability稳定identity格式。
- [x] 3.2 定义capability所属domain字段。
- [x] 3.3 定义capability允许document role集合。
- [x] 3.4 定义capability作者显示元数据。
- [x] 3.5 定义capability固定port descriptor。
- [x] 3.6 定义capability dynamic port policy。
- [x] 3.7 定义capability typed field descriptor。
- [x] 3.8 定义field值类型与约束合同。
- [x] 3.9 定义field可见条件。
- [x] 3.10 定义field可写条件。
- [x] 3.11 定义field只读reference条件。
- [x] 3.12 定义capability child surface descriptor。
- [x] 3.13 定义capability authoring command descriptor。
- [x] 3.14 定义capability compiler handler identity。
- [x] 3.15 定义capability Document codec identity。
- [x] 3.16 建立capability重复identity拒绝规则。
- [x] 3.17 建立未知capability拒绝规则。
- [x] 3.18 建立未注册field与port拒绝规则。
- [x] 3.19 把现有Agent capability查询迁移为共享目录查询。
- [x] 3.20 删除Agent专属的重复节点能力映射。

## 4. 共享Canvas、Node与Port作者表面

- [x] 4.1 从BTSMTL现有Canvas原地提取通用Graph Canvas生命周期，不新写替代GraphView。
- [x] 4.2 把document加载与刷新改为domain adapter驱动，并保持原BTSMTL刷新行为。
- [x] 4.3 把node创建菜单改为capability驱动，并保持原菜单、Graph role过滤与工厂能力。
- [x] 4.4 把搜索索引改为capability与document projection驱动，并保持原搜索结果与操作入口。
- [x] 4.5 从现有Node View提取descriptor驱动的标题、图标和颜色，不降低特化Node信息。
- [x] 4.6 从现有Node View提取diagnostics projection驱动的状态badge。
- [x] 4.7 从现有Flow/Property Port View提取capability驱动的固定Port创建。
- [x] 4.8 从现有动态Port实现提取node-local identity驱动的Port创建。
- [x] 4.9 从现有Flow/Property Edge View提取document edge投影。
- [x] 4.10 把端口兼容查询改为domain port policy驱动，并保持BTSMTL Property Port规则。
- [x] 4.11 把原拖线操作提交改为typed connect mutation。
- [x] 4.12 把原断线操作提交改为typed disconnect mutation。
- [x] 4.13 从现有框选与selection代码提取唯一实现。
- [x] 4.14 从现有复制行为提取domain clipboard envelope。
- [x] 4.15 从现有粘贴行为提取domain capability与mutation。
- [x] 4.16 在保持本领域粘贴行为的同时拒绝跨domain payload。
- [x] 4.17 把原删除操作收敛为领域typed delete mutation。
- [x] 4.18 把原节点位置与layout写入独立editor-only owner。
- [x] 4.19 把原Undo粒度绑定到领域mutation transaction。
- [x] 4.20 仅在BTSMTL特化行为已由presenter或binding承接后删除共享View中的领域switch。

## 5. 共享Details、Navigator与Data Catalog

- [x] 5.1 定义Details section稳定identity。
- [x] 5.2 从现有Inspector原地提取Authoring section的capability field投影。
- [x] 5.3 从现有Inspector原地提取References section的只读引用投影。
- [x] 5.4 从现有Inspector原地提取Live section的只读runtime projection。
- [x] 5.5 从现有Inspector原地提取Diagnostics section的默认折叠投影。
- [x] 5.6 默认隐藏stable identity与revision内部字段。
- [x] 5.7 默认隐藏compiled index与runtime handle。
- [x] 5.8 默认隐藏generated path与Projection中间字段。
- [x] 5.9 默认隐藏当前capability不适用的nullable字段。
- [x] 5.10 把原Details字段提交降低为typed field mutation。
- [x] 5.11 把原Details命令提交降低为typed authoring command。
- [x] 5.12 定义Navigator item稳定identity。
- [x] 5.13 定义Navigator item owner与reference关系。
- [x] 5.14 定义Navigator Open Owner命令。
- [x] 5.15 定义Data Catalog分组与搜索合同。
- [x] 5.16 从现有breadcrumb与page stack原地提取共享Navigator宿主。
- [x] 5.17 禁止Navigator保存第二份业务binding。
- [x] 5.18 禁止References区域直接修改外部owner。
- [x] 5.19 从现有Bottom Dock行为提取domain descriptor驱动的tab注册。
- [x] 5.20 保持Preview、Watch与Live Debug原有入口且只读取正式结果。

## 6. 共享StateMachine作者表面

- [x] 6.1 定义StateMachine surface role合同。
- [x] 6.2 定义Entry投影合同。
- [x] 6.3 定义State投影合同。
- [x] 6.4 定义State Alias投影合同。
- [x] 6.5 定义Transition投影合同。
- [x] 6.6 定义State child graph下钻合同。
- [x] 6.7 定义Transition rule下钻合同。
- [x] 6.8 从BTSMTL现有StateMachine实现提取共享Entry View。
- [x] 6.9 从BTSMTL现有StateMachine实现提取共享State View。
- [x] 6.10 从BTSMTL现有StateMachine实现提取共享Alias View。
- [x] 6.11 从BTSMTL现有Transition实现提取共享Transition Edge View。
- [x] 6.12 从BTSMTL现有页面栈提取共享StateMachine breadcrumb。
- [x] 6.13 从BTSMTL现有创建入口提取共享StateMachine菜单宿主。
- [x] 6.14 从BTSMTL现有Inspector提取共享Transition Details宿主。
- [x] 6.15 让BTSMTL adapter只提供Condition、priority与interruption语义，并保持原操作。
- [x] 6.16 让Presentation adapter只提供blend、sync、readiness与Pose rule语义。
- [x] 6.17 拒绝Gameplay transition payload进入PoseStateMachine。
- [x] 6.18 拒绝Pose transition payload进入BTSMTL StateMachine。
- [x] 6.19 保持BTSMTL inline-first ownership语义。
- [x] 6.20 保持Pose state-local graph的root-owned flat catalog语义。

## 7. BTSMTL现有作者UI迁移

- [x] 7.1 为BaseGraph实现共享document adapter。
- [x] 7.2 为BaseNode实现共享node projection adapter。
- [x] 7.3 为PropertyPort实现共享port projection adapter。
- [x] 7.4 为BaseEdge与PropertyEdge实现共享edge projection adapter。
- [x] 7.5 为BTSMTL Graph role注册正式capability。
- [x] 7.6 把BTSMTL原Node Search迁移到共享catalog且保持搜索、过滤与创建行为。
- [x] 7.7 从BTSMTL原Node View原地抽象共享Node View。
- [x] 7.8 从BTSMTL原Flow/Property Port View原地抽象共享Port View。
- [x] 7.9 从BTSMTL原Flow/Property Edge View原地抽象共享Edge View。
- [x] 7.10 从BTSMTL原Inspector原地抽象共享Details宿主。
- [x] 7.11 从BTSMTL原Data Catalog原地抽象共享Navigator宿主并保持黑板拖拽。
- [x] 7.12 从BTSMTL原breadcrumb与页面栈原地抽象共享页面导航。
- [x] 7.13 从BTSMTL原clipboard原地抽象共享domain envelope。
- [x] 7.14 把BTSMTL原mutation调用适配到共享typed command合同。
- [x] 7.15 把BTSMTL原diagnostics适配到共享只读投影。
- [x] 7.16 把BTSMTL原runtime trace适配到共享Live投影。
- [x] 7.17 仅在0U.7至0U.25闭合后把BaseTreeAsset正式打开入口切到共享Shell。
- [x] 7.18 原窗口通用布局、页面栈、恢复和命令入口已由`GraphAuthoringEditorShell`与共享导航承接；保留仍负责BTSMTL页面解析和原UI装配的`BaseTreeWindow`领域binding。
- [x] 7.19 原Canvas通用生命周期、selection、clipboard、Undo和typed change分类已由`GraphAuthoringCanvasView`承接；保留仍负责BTSMTL节点、Stack、Group和黑板拖拽的`BaseTreeView`领域binding。
- [x] 7.20 原Node/Port/Edge基础视觉与只读交互已原地提取；保留仍负责BTSMTL字段、Flow/Property Port和特化Node内容的`BaseNodeView`领域binding。

## 8. Pose typed authoring数据模型

- [x] 8.1 固定全部正式Pose node capability identity。
- [x] 8.2 为Graph Input定义独立typed payload。
- [x] 8.3 为Graph Output定义独立typed payload。
- [x] 8.4 为Program Parameter Input定义独立typed payload。
- [x] 8.5 为Sequence Player定义独立typed payload。
- [x] 8.6 为Blend Space Player定义独立typed payload。
- [x] 8.7 为Selected Pose Player定义独立typed payload。
- [x] 8.8 为PoseStateMachine定义独立typed payload。
- [x] 8.9 为AnimationSlot定义独立typed payload。
- [x] 8.10 为Blend Pose定义独立typed payload。
- [x] 8.11 为Blend Stack定义独立typed payload。
- [x] 8.12 为Inertialization定义独立typed payload。
- [x] 8.13 为Layered Bone Blend定义独立typed payload。
- [x] 8.14 为Additive Pose定义独立typed payload。
- [x] 8.15 为Pose Parameter Resolve定义独立typed payload。
- [x] 8.16 为Modify Bone定义独立typed payload。
- [x] 8.17 为Two Bone IK定义独立typed payload。
- [x] 8.18 为Foot Placement定义独立typed payload。
- [x] 8.19 为Pose Subgraph定义独立typed payload。
- [x] 8.20 为Action Playback Input定义独立typed payload。
- [x] 8.21 为每个Pose capability注册固定port descriptor。
- [x] 8.22 为需要可变输入的Pose capability定义dynamic port数据。
- [x] 8.23 定义Pose node identity到typed payload的封闭映射。
- [x] 8.24 删除node kind原地转换语义。
- [x] 8.25 删除`CharacterPoseNodeDefinition`联合体字段模型。

## 9. Presentation Mutation与Validator

- [x] 9.1 定义Presentation mutation transaction合同。
- [x] 9.2 定义Create Pose Node命令。
- [x] 9.3 定义Delete Pose Node命令。
- [x] 9.4 定义Set Pose Node Field命令。
- [x] 9.5 定义Add Dynamic Pose Port命令。
- [x] 9.6 定义Remove Dynamic Pose Port命令。
- [x] 9.7 定义Connect Pose Port命令。
- [x] 9.8 定义Disconnect Pose Port命令。
- [x] 9.9 定义Create PoseStateMachine命令。
- [x] 9.10 定义Create Pose State命令。
- [x] 9.11 定义Delete Pose State命令。
- [x] 9.12 定义Create Pose Transition命令。
- [x] 9.13 定义Delete Pose Transition命令。
- [x] 9.14 定义Set Pose Transition Field命令。
- [x] 9.15 定义Profile source binding mutation。
- [x] 9.16 定义Profile Policy mutation。
- [x] 9.17 定义AnimationSlot binding mutation。
- [x] 9.18 统一identity分配器与冲突检测。
- [x] 9.19 统一dirty owner与Undo记录。
- [x] 9.20 统一跨Pose Graph与Profile的资产级回滚。
- [x] 9.21 让Validator从共享capability读取字段与port约束。
- [x] 9.22 让Validator检查Pose拓扑与Output唯一性。
- [x] 9.23 让Validator检查StateMachine页面所有权。
- [x] 9.24 让Validator检查Pose source与AnimationChannel引用。
- [x] 9.25 删除直接SerializedProperty字段写入路径。

## 10. Pose Compiler模块化与Pose IR

- [x] 10.1 定义Pose IR graph identity。
- [x] 10.2 定义Pose IR node identity。
- [x] 10.3 定义Pose IR PoseLink identity。
- [x] 10.4 定义Pose IR value input identity。
- [x] 10.5 定义Pose IR source binding。
- [x] 10.6 定义Pose IR state machine引用。
- [x] 10.7 定义Pose IR slot引用。
- [x] 10.8 定义Pose IR diagnostics source path。
- [x] 10.9 定义Pose compiler handler接口。
- [x] 10.10 为每个正式Pose capability注册唯一handler。
- [x] 10.11 把node-local字段校验移入对应handler。
- [x] 10.12 把node降低逻辑移入对应handler。
- [x] 10.13 把顶层拓扑排序收敛到Compiler coordinator。
- [x] 10.14 把PoseLink依赖解析收敛到Compiler coordinator。
- [x] 10.15 把cycle诊断收敛到Compiler coordinator。
- [x] 10.16 把buffer lifetime规划收敛到Native plan builder。
- [x] 10.17 把IR线性化收敛到Native plan builder。
- [x] 10.18 保持Runtime operation enum只属于compiled层。
- [x] 10.19 保持Runtime dispatch switch只属于执行层。
- [x] 10.20 删除顶层Compiler中的node kind业务字段switch。
- [x] 10.21 删除缺失handler时的passthrough或默认operation。
- [x] 10.22 让Preview从同一Pose IR与Native plan构建。
- [x] 10.23 让Live trace引用同一compiled node identity。
- [x] 10.24 保持普通编辑不自动触发Projection Build。
- [x] 10.25 保持Build为显式作者命令。

## 11. Pose Graph工作区迁移

- [x] 11.1 为Pose Graph实现共享document adapter。
- [x] 11.2 为typed Pose node实现共享node projection adapter。
- [x] 11.3 为typed Pose port实现共享port projection adapter。
- [x] 11.4 为Pose edge实现共享edge projection adapter。
- [x] 11.5 注册Pose Graph role capability集合。
- [x] 11.6 注册PoseStateMachine role capability集合。
- [x] 11.7 注册Pose state-local graph role capability集合。
- [x] 11.8 注册Pose transition rule role capability集合。
- [x] 11.9 把Pose创建菜单迁移到从BTSMTL原地抽象的共享catalog宿主。
- [x] 11.10 把Pose节点渲染迁移到从BTSMTL原地抽象的共享Node View。
- [x] 11.11 把Pose端口渲染迁移到从BTSMTL原地抽象的共享Port View。
- [x] 11.12 把Pose拖线迁移到同一Canvas和Presentation Mutation。
- [x] 11.13 把Pose clipboard迁移到同一Canvas的共享domain envelope。
- [x] 11.14 把Pose Details迁移到从BTSMTL原地抽象的Details宿主与capability field投影。
- [x] 11.15 把Pose References迁移到共享只读section。
- [x] 11.16 把Pose Navigator迁移到从BTSMTL原地抽象的Navigator宿主。
- [x] 11.17 把Pose Producer Catalog迁移到从BTSMTL原地抽象的Data Catalog宿主。
- [x] 11.18 把PoseStateMachine迁移到从BTSMTL原地抽象的StateMachine表面。
- [x] 11.19 把Pose Watch迁移到共享Bottom Dock注册。
- [x] 11.20 把Pose Preview迁移到共享Bottom Dock注册。
- [x] 11.21 把Live Debug迁移到共享模式与trace投影。
- [x] 11.22 保持revision不匹配时Live显示Stale。
- [x] 11.23 保持Live Debug模式禁用mutation。
- [x] 11.24 删除`PoseGraphNodeView`。
- [x] 11.25 删除`PoseGraphView`。
- [x] 11.26 删除Pose专用Node Catalog adapter中的重复能力表。
- [x] 11.27 删除Pose专用Port Policy adapter中的重复端口表。
- [x] 11.28 删除Pose专用Inspector中的node kind字段switch。

## 12. Agent Authoring Document v3模型与Codec

- [x] 12.1 把唯一schema常量升级为`btsmtl-agent-authoring-document.v3`。
- [x] 12.2 更新manifest允许的规范文件清单规则。
- [x] 12.3 定义`editable/presentation/profile.json`模型。
- [x] 12.4 定义Presentation Profile稳定owner identity。
- [x] 12.5 定义Pose source binding JSON模型。
- [x] 12.6 定义Presentation Policy JSON模型。
- [x] 12.7 定义`editable/presentation/pose-graphs/<graph-id>/graph.json`模型。
- [x] 12.8 定义Pose Graph typed node JSON模型。
- [x] 12.9 定义Pose fixed port引用格式。
- [x] 12.10 定义Pose dynamic port JSON模型。
- [x] 12.11 定义Pose edge JSON模型。
- [x] 12.12 定义Pose Graph subgraph引用格式。
- [x] 12.13 定义Pose Graph `layout.json`模型。
- [x] 12.14 定义`editable/presentation/pose-state-machines/<id>/state-machine.json`模型。
- [x] 12.15 定义Pose State JSON模型。
- [x] 12.16 定义Pose State Alias JSON模型。
- [x] 12.17 定义Pose Transition JSON模型。
- [x] 12.18 定义state-local graph owner引用格式。
- [x] 12.19 定义AnimationSlot binding JSON模型。
- [x] 12.20 从共享capability生成Presentation strict property合同。
- [x] 12.21 拒绝未知Presentation文件。
- [x] 12.22 拒绝未知Pose node kind。
- [x] 12.23 拒绝当前node不适用字段。
- [x] 12.24 拒绝C#类型名与SerializedProperty path。
- [x] 12.25 拒绝runtime、Projection与generated字段。
- [x] 12.26 保持Rig资源正文只读或省略。
- [x] 12.27 保持整包semantic hash与Conflict语义。
- [x] 12.28 保持layout hash与authoring semantic hash分离。
- [x] 12.29 删除Document v2 schema常量。
- [x] 12.30 删除Document v2 reader与writer。

## 13. Document v3 Exporter、Reconciler与事务

- [x] 13.1 为Profile实现v3 exporter。
- [x] 13.2 为Pose source binding实现v3 exporter。
- [x] 13.3 为Presentation Policy实现v3 exporter。
- [x] 13.4 为Pose Graph实现v3 exporter。
- [x] 13.5 为typed Pose node实现capability驱动exporter。
- [x] 13.6 为dynamic port实现v3 exporter。
- [x] 13.7 为Pose edge实现v3 exporter。
- [x] 13.8 为Pose Graph layout实现v3 exporter。
- [x] 13.9 为PoseStateMachine实现v3 exporter。
- [x] 13.10 为Pose State与Alias实现v3 exporter。
- [x] 13.11 为Pose Transition实现v3 exporter。
- [x] 13.12 为Presentation read-only context实现紧凑exporter。
- [x] 13.13 定义Presentation target-state diff顺序。
- [x] 13.14 先解析全部Presentation owner与引用。
- [x] 13.15 再生成Profile与Graph owner mutation。
- [x] 13.16 再生成StateMachine页面mutation。
- [x] 13.17 再生成typed node与dynamic port mutation。
- [x] 13.18 再生成Pose edge mutation。
- [x] 13.19 最后生成跨owner binding mutation。
- [x] 13.20 把所有Presentation差异降低为正式Presentation Mutation。
- [x] 13.21 把Presentation plan加入Document dry-run结果。
- [x] 13.22 把Presentation diagnostics加入机器可读source path。
- [x] 13.23 把Presentation受影响owner加入apply报告。
- [x] 13.24 把Presentation mutation纳入同一资产级事务。
- [x] 13.24a 在preflight阶段解析全部Presentation命令、引用与owner。
- [x] 13.24b 把Definition与全部Gameplay Graph owner加入事务闭包。
- [x] 13.24c 把全部Timeline与Action/Profile owner加入事务闭包。
- [x] 13.24d 把Presentation Profile与Pose Graph asset加入事务闭包。
- [x] 13.24e 把PoseStateMachine页面、state-local graph与transition rule owner加入事务闭包。
- [x] 13.24f 把editor-only layout owner加入事务闭包。
- [x] 13.24g 在首次写入前拒绝缺失、非持久化或事务外owner。
- [x] 13.24h 在apply报告中输出完整touched owner identity。
- [x] 13.25 把Presentation revision纳入source revision计算。
- [x] 13.26 把Presentation editable纳入反向导出。
- [x] 13.27 把Presentation editable纳入rebase冲突检测。
- [x] 13.28 删除Presentation只读Snapshot专用映射。
- [x] 13.29 删除Reconciler对Presentation的unsupported分支。
- [x] 13.30 禁止Reconciler直接写YAML或SerializedProperty path。

## 14. Application Service、MCP与Editor入口

- [x] 14.1 扩展唯一Document application service以接收v3 Presentation plan。
- [x] 14.2 保持checkout动作名与事务边界不变。
- [x] 14.3 保持validate动作名与只读边界不变。
- [x] 14.4 保持rebase动作名与Conflict边界不变。
- [x] 14.5 保持dry-run动作名与只读边界不变。
- [x] 14.6 保持apply动作名与资产级事务边界不变。
- [x] 14.6a 在首次mutation前注册包含全部owner的唯一Undo group。
- [x] 14.6b 在全部mutation后运行Gameplay、Timeline与Presentation Validator。
- [x] 14.6c 只在全部Validator成功后标记dirty并保存authoring。
- [x] 14.6d 保存后从最终Unity树生成完整v3 staging package。
- [x] 14.6e staging导出、重读或hash校验失败时恢复全部Unity owner。
- [x] 14.6f package原子替换失败时恢复全部Unity owner并保留上一正式package。
- [x] 14.6g 失败响应固定返回`applied=false`、`saved=false`且不报告`Clean`。
- [x] 14.6h Character apply成功后保持generated product stale且不自动Build。
- [x] 14.7 更新MCP schema identity为Document v3。
- [x] 14.8 更新MCP返回的Presentation mutation摘要。
- [x] 14.9 更新MCP返回的Presentation diagnostics路径。
- [x] 14.10 更新EditorWindow状态显示为Document v3。
- [x] 14.11 更新EditorWindow的Presentation dirty摘要。
- [x] 14.12 更新Document authoring skill合同为v3。
- [x] 14.13 删除MCP bridge的v2 schema识别。
- [x] 14.14 删除EditorWindow的v2 schema识别。
- [x] 14.15 删除Document service的v2兼容提示与转换逻辑。
- [x] 14.16 拒绝旧v2文档并要求显式重新checkout。
- [x] 14.17 拒绝Pose专用MCP action。
- [x] 14.18 拒绝旧Patch与Macro Presentation写入口。

## 14A. 工作区已有动画能力接入门禁

- [x] 14A.1 确认Rig v3与Virtual Bone catalog继续只有Rig owner可写，Document v3只读投影其Bone identity。
- [x] 14A.2 确认TwoBoneIK只通过共享Capability、typed payload、Presentation Mutation与Pose IR handler进入既有Native operation。
- [x] 14A.3 确认TwoBoneIK Details使用Rig v3 Physical/Virtual Bone picker且不保存第二份Bone定义。
- [x] 14A.4 确认FootPlacement只通过typed payload和Profile/Calibration引用进入唯一world-aware阶段。
- [x] 14A.5 确认BlendSpacePlayer使用共享Capability、state-local source binding与既有BlendSpace solver。
- [x] 14A.6 确认Motion Matching只作为PoseState provider进入共享Catalog且不产生Gameplay channel或Action playback identity。
- [x] 14A.7 确认AnimationSlot只消费有限Action channel并通过typed policy进入已有Slot runtime。
- [x] 14A.8 确认BlendStack、Inertialization、Layer、Additive与Mask全部按PoseBoneCount进入共享authoring和Pose IR。
- [x] 14A.9 确认Transition Routing只通过Transition/Slot policy和只读diagnostics接入，不拥有PoseState、Player或最终Pose。
- [x] 14A.10 在0U全部门禁闭合后，重新对账`add-action-animation-authoring-workspace`接入从BTSMTL原地抽象的共享Canvas、Details、Navigator、Timeline adapter和typed owner mutation。
- [x] 14A.11 删除各能力遗留的Pose专用GraphView、Inspector switch、Agent专属catalog与顶层Compiler业务kind分支。
- [x] 14A.12 在0U、4–7、11与14A.1至14A.11全部完成前禁止执行正式资产迁移器。

## 15. 正式资产迁移器

- [x] 15.1 定义旧Pose联合体node到typed payload的完整映射表。
- [x] 15.2 定义旧固定port到capability port identity的完整映射表。
- [x] 15.3 定义旧动态输入到node-local port identity的映射规则。
- [x] 15.4 定义旧Pose edge到typed edge的映射规则。
- [x] 15.5 定义旧PoseStateMachine页面到共享surface数据的映射规则。
- [x] 15.6 定义旧State payload到typed State payload的映射规则。
- [x] 15.7 定义旧Transition payload到typed Transition payload的映射规则。
- [x] 15.8 定义旧layout到editor-only layout的映射规则。
- [x] 15.9 定义Profile binding迁移顺序。
- [x] 15.10 定义AnimationSlot binding迁移顺序。
- [x] 15.11 实现只生成typed目标状态与Mutation Plan的显式Presentation迁移规划器。
- [x] 15.12 在迁移前拒绝未知node kind与未知字段组合。
- [x] 15.13 在迁移前拒绝断裂owner与引用。
- [x] 15.14 在迁移计划内分配稳定typed payload identity。
- [x] 15.15 在迁移计划内重建dynamic port identity。
- [x] 15.16 在迁移计划内重写所有Pose edge endpoint。
- [x] 15.17 在迁移计划内重写StateMachine页面引用。
- [x] 15.18 在迁移计划内重写Profile与Slot binding。
- [x] 15.19 在迁移目标状态内清除旧联合体字段。
- [x] 15.20 迁移规划失败时不修改任何Unity资产或Document package。
- [x] 15.21 Document apply完成后拒绝旧资产schema加载。
- [x] 15.22 不提供旧资产runtime fallback。

## 16. Corin正式数据与产品构建

- [x] 16.1 用精确Corin Definition显式checkout唯一Document v3迁移包，不修改Unity资产。
- [x] 16.2 在同一目标Document中迁移Corin Presentation Profile与根Pose Graph typed payload。
- [x] 16.3 在同一目标Document中迁移Corin Locomotion PoseStateMachine、state-local Pose Graph与全部Pose Transition。
- [x] 16.4 在同一目标Document中迁移Pose source与FullBodyAction AnimationSlot binding。
- [x] 16.5 在同一目标Document中迁移Rig v3、Virtual Bone、Layered Bone Blend、Bone Mask与Policy引用。
- [x] 16.6 在同一目标Document中迁移TwoBoneIK、FootPlacement typed payload与editor-only layout。
- [x] 16.7 在同一目标Document中合并`refactor-animation-control-boundaries`的Corin Gameplay、Timeline与Presentation目标状态。
- [x] 16.8 对完整目标Document执行dry-run并修复全部typed failure。
- [x] 16.9 使用dry-run返回的exact Document hash执行唯一apply。
- [x] 16.10 确认Application Service在一个Undo group内提交Gameplay、Timeline与Presentation owner。
- [x] 16.11 反向canonical export并确认目标hash、live revision与Document package回到Clean。
- [x] 16.12 确认最终资产只保留PoseStateMachine、Slot、Rig v3、Virtual Bone、TwoBoneIK、FootPlacement与唯一OutputPose链。
- [x] 16.13 确认旧BaseLocomotion表现数据、ActionOverride、旧Pose联合体与旧Document正文已经从正式资产删除。
- [x] 16.14 从同一validated Semantic IR显式发布Float32/Fixed Program、Presentation Projection与Native Pose Program。
- [x] 16.15 更新Corin Definition到新revision产物并删除旧Projection、旧Native Program与旧wrapper。
- [x] 16.16 删除Corin Document v2工作目录，只保留canonical v3 package。

## 17. 激进删除与单链收口

- [x] 17.1 删除Pose专用GraphView生命周期实现。
- [x] 17.2 删除Pose专用Node View实现。
- [x] 17.3 删除Pose专用Port View实现。
- [x] 17.4 删除Pose专用selection实现。
- [x] 17.5 删除Pose专用clipboard实现。
- [x] 17.6 删除Pose专用Undo编排实现。
- [x] 17.7 删除Pose专用Node Catalog重复表。
- [x] 17.8 删除Pose专用Port Policy重复表。
- [x] 17.9 删除Pose专用Inspector大switch。
- [x] 17.10 删除BTSMTL旧Window通用交互实现。
- [x] 17.11 删除BTSMTL旧View通用交互实现。
- [x] 17.12 删除BTSMTL与Agent重复capability目录。
- [x] 17.13 删除`CharacterPoseNodeDefinition`联合体类型。
- [x] 17.14 删除旧Pose node kind到字段的重复映射。
- [x] 17.15 删除旧Pose Compiler顶层kind switch业务分支。
- [x] 17.16 删除Document v2全部模型与codec。
- [x] 17.17 删除Document v2全部manifest与service分支。
- [x] 17.18 删除Presentation只读Document限制。
- [x] 17.19 删除旧Pose Snapshot第二字段模型。
- [x] 17.20 删除旧Patch、Macro与bootstrap Presentation入口。
- [x] 17.21 删除旧Workbench与兼容菜单入口。
- [x] 17.22 删除所有旧schema fallback配置。
- [x] 17.23 删除确认不可达的旧资产与generated产物。
- [x] 17.24 搜索并清除旧类型、旧菜单、旧action与旧schema引用。

## 18. 文档与变更对账

- [x] 18.1 更新`graph-authoring-domain-framework`正式能力文档。
- [x] 18.2 更新`graph-authoring-editor-shell`正式能力文档。
- [x] 18.3 更新`btsmtl-graph-core`的跨领域复用边界。

- [x] 18.4 更新`btsmtl-sm-node-authoring`的共享StateMachine表面边界。
- [x] 18.5 更新`character-presentation-pose-graph`的typed authoring与Pose IR口径。
- [x] 18.6 更新`character-animation-presentation-authoring`的唯一写入口口径。
- [x] 18.7 更新`btsmtl-agent-authoring-document-sync`为v3口径。
- [x] 18.8 更新`agent-character-controller-synthesis`的Presentation可写口径。
- [x] 18.9 更新`btsmtl-agent-authoring-mcp-bridge`的v3闭包口径。
- [x] 18.10 更新`openspec/project.md`中的Graph Authoring与Document边界。
- [x] 18.11 对账`refactor-animation-control-boundaries`已被本change接管的资产迁移任务。
- [x] 18.12 对账`add-action-animation-authoring-workspace`应消费的共享UI合同。
- [x] 18.13 对账所有active change中Presentation只读描述。
- [x] 18.14 对账所有active change中Pose只复用Shell的描述。
- [x] 18.15 对账代码、正式spec与Document skill使用同一capability命名。
- [x] 18.16 对账代码中只剩唯一Presentation Mutation写链。
- [x] 18.17 对账代码中只剩唯一Graph Canvas与Node/Port View实现。
- [x] 18.18 对账代码中不再存在Document v2消费点。

## 19. 文件聚合与残留资产收口

- [x] 19.1 建立Character Pipeline代码与资产的精确移动清单，并确认全部源、目标与`.meta`位于项目根内。
- [x] 19.2 将`Editor/CharacterPipeline`按Action Workspace、共享Graph、Pose Graph、Animation Authoring、Definition、Inspector、Analysis与Diagnostics聚合。
- [x] 19.3 将`Editor/CharacterSimulation`按Semantic、Presentation、Program、Build、Analysis、Navigation、Network Product与Inspector聚合。
- [x] 19.4 将Animation Contracts按Action、Blend、Rig、Pose、Sources、Projection、Workspace与Common聚合。
- [x] 19.5 将Corin Character authoring资产按AI、Motion、Presentation Profile、Blend、Rig、Foot Placement、Pose Graph与Sources聚合。
- [x] 19.6 将Gameplay Lab、Deterministic Rollback与Character Runtime Profile资产按Composition、Pipeline、Program、World、Network与产品用途聚合。
- [x] 19.7 删除全项目不可达的旧Action/Dodge RootMotion Curve资产。
- [x] 19.8 删除全项目不可达的旧顶层Locomotion Pose Source资产与旧FullBody Mask资产。
- [x] 19.9 删除空的旧Animation、BlendCurves与Presentation/Animancer目录，并清除本轮诊断临时资产。
- [x] 19.10 更新全部精确AssetDatabase路径、产品入口路径和文档路径，不保留旧路径兼容查找。
- [x] 19.11 刷新Unity并重新生成canonical Document v3目录包，使资产新路径成为唯一反向导出结果。
- [x] 19.12 更新项目Code Organization与implementation inventory，记录输入、处理、输出和删除的旧路径。
