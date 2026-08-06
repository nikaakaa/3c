## Context

BTSMTL逻辑层是项目的小蓝图作者系统，现有Tree、AI、Data Catalog、Inspector、StateMachine下钻、Undo与Live Debug体验应保持。PoseGraph是Character Presentation动画拓扑，不是BTSMTL Gameplay Graph；它应复用BTSMTL已经成熟的Graph编辑积木，但需要角色画面、动画输入、运行目标和节点调参这些领域能力。

当前`CharacterPresentationPoseGraphEditorWindow`已经继承共享Graph作者基础，也已经存在Preview Fixture、AnimationPreviewRuntime、RuntimeDebugSession、Tuning Layout和candidate交换代码。失败点在组合层：共享Shell被错误扩张为动画布局；PoseGraph窗口出现重复Preview/Live/Diagnostics表面；全量Tuning Layout被铺成表单；Action Animation承担了错误入口；节点视觉没有稳定消费BTSMTL UXML/USS；角色画面、图selection与运行状态没有闭合。

本设计不重写BTSMTL，不新建PoseGraph数据模型，也不增加第二动画runtime。它把已有能力收敛成一条作者操作链。

## Goals

- 保持BTSMTL逻辑图现有编辑体验与窗口结构。
- 让现有PoseGraph窗口复用BTSMTL Canvas、Node/Port/Edge视觉、selection、Undo、创建菜单、StateMachine表面和Details宿主。
- 从精确Presentation Profile上下文直接打开现有PoseGraph，不经过Action Animation。
- 让Corin Preview运行正式Projection与Pose Plan，并由typed输入驱动现有PoseStateMachine。
- 在同一作者图上显示当前State、Transition与执行节点。
- 让当前selection的作者值、Applied值和生效语义在同一Details中可见和可修改。
- 保留已经实现的完整candidate、帧边界交换和runtime consumer链。
- 保持Compile与Character Build显式触发。

## Non-Goals

- 不实现自由停靠系统。
- 不要求固定Pose Watch、Asset Browser、Trace、Search Results或Preview Scene Settings面板。
- 不修改BTSMTL逻辑图的Data Catalog、Inspector信息架构、节点业务语义或Live Debug模式。
- 不从Action Animation、Timeline或Slot解析PoseGraph工作上下文。
- 不修改FinalIK、Foot Placement、Blend、Inertialization、Motion Matching或Secondary Motion算法。
- 不新增Preview专用Pose Plan、直接Clip播放器、shadow skeleton、第二PlayableGraph或第二solver。
- 不自动Build、自动Foot Analysis或自动Motion Matching Database Build。

## Decision: 共享图编辑内核，不共享整个工作区布局

共享关系收敛为两层：

```text
Graph Authoring Interaction Core
  document adapter
  capability catalog
  GraphAuthoringCanvasView
  node / port / edge visual
  selection / clipboard / undo
  create menu / connection policy
  page stack / breadcrumb
  StateMachine surface
  Details host
        │
        ├── BTSMTL Tree / AI Workspace
        │     保持现有Data Catalog、Graph、Details和Live Debug布局
        │
        └── Character Presentation PoseGraph Workspace
              组合角色画面、PoseGraph Canvas、图导航、Details、typed输入和目标控制
```

PoseGraph复用的是交互与视觉积木，不是BTSMTL Gameplay数据、BaseNode序列化模型、Data Catalog内容或整个BaseTreeWindow布局。`CharacterPresentationPoseGraphEditorWindow`继续是现有唯一窗口；它可以拥有动画领域的区域组合，但必须装配同一个`GraphAuthoringCanvasView`、selection、Undo、breadcrumb和typed Mutation，不得复制这些生命周期。

这项决策修正current spec中“所有domain必须共用同一五区Shell”的过度约束。Tree与AI继续使用`GraphAuthoringEditorShell`现有布局；PoseGraph窗口使用同一交互内核构成领域工作区。代码复用边界比视觉布局复用边界更窄、更稳定。

## Decision: PoseGraph窗口只保留长期必要功能

第一闭环需要六类功能，但不规定它们必须是固定页签或可停靠窗口：

1. 命令与状态：Validate、Compile、Character Build、Dirty/Invalid/Stale/Ready、目标选择、Play/Pause/Step/Restart。
2. 角色结果：当前Preview或Live Actor的最终角色画面。
3. 图导航：Root Graph、root-owned子图、PoseStateMachine和breadcrumb。
4. 图编辑：唯一BTSMTL Canvas与StateMachine表面。
5. 当前selection Details：Node、State或Transition的作者字段、Applied值、引用和错误。
6. Preview typed输入：只在Preview目标下驱动Grounded、Movement Mode、Speed、Direction等正式输入。

布局可以先使用稳定分割区实现，后续自由停靠不会改变上述功能、数据owner或运行链。因此Docking不是第一闭环的依赖。

固定不进入首轮的功能包括Pose Watch管理器、通用动画Asset Browser、Trace浏览器和永久搜索结果。已有Pose Watch runtime能力继续保留，但没有明确作者操作需求前不占据主界面。

## Decision: 现有PoseGraph资产和编译链保持唯一

工作区直接编辑当前`CharacterPresentationPoseGraphAsset`：

- Root Graph与root-owned flat graph catalog保持不变；
- Pose Node、typed Port与Pose Edge保持不变；
- PoseStateMachine、State、Alias、Transition与layout owner保持不变；
- Source Slot仍由PoseGraph拥有，实际资源Binding仍由Presentation Profile拥有；
- 作者修改继续通过typed Presentation Mutation、Validator、Undo和dirty owner；
- Compiler继续降低为Pose IR、Presentation Projection与Native Pose Program。

窗口重构不得创建新的Graph资产、缓存节点集合、临时Transition列表或Preview-only拓扑。GraphView只投影当前正式document。

## Decision: 正式入口从Presentation Profile建立精确上下文

唯一可运行入口是：

```text
CharacterPipelineDefinition
  -> CharacterAnimationPresentationProfile
       -> Open Pose Graph
            -> CharacterPresentationPoseGraphEditorWindow
```

打开请求必须携带或精确解析：

- Character Definition；
- Character Animation Presentation Profile；
- Character Presentation Pose Graph；
- 当前发布Presentation Projection；
- Rig Definition与revision；
- Profile-owned Source Bindings；
- 唯一匹配的Preview Fixture。

缺少精确上下文时，窗口可以显示作者图和明确Unavailable，但不得获得可运行Preview或Live绑定。不得从Action、Timeline call site、Slot、当前Scene、GameObject名称、资产目录或上次窗口状态补全。

Action Animation Workspace继续只处理有限Action关系。它不得成为PoseGraph正式入口、前置步骤或验收界面。

## Decision: PoseGraph必须完整复用BTSMTL图编辑手感

PoseGraph Canvas必须提供与BTSMTL一致的基本操作：

- 空白处右键按Capability搜索并创建节点；
- 从typed端口拖到空白处时只显示兼容节点；
- 节点选择、移动、框选、复制、粘贴、删除和Undo/Redo；
- typed连接、断开与Mutation前connection policy校验；
- NodeGroup和注释继续使用BTSMTL已有视觉能力，但不作为第一运行闭环的阻断项；
- 双击PoseStateMachine进入共享StateMachine表面；
- 双击State或PoseSubgraph进入对应root-owned graph；
- breadcrumb返回上层；
- 打开页面时按现有layout恢复位置，并在首次显示或内容不可见时执行一次Frame All，不持续重排作者位置。

视觉必须直接消费BTSMTL的Node、Port、Edge、selection、running状态与Inspector USS/UXML。若Pose数据不能继承`BaseNode`，应提取不依赖数据模型的Node Visual Chrome；不得以裸Unity GraphView Node或复制USS数值建立近似样式。

PoseGraph节点可提供领域颜色、图标、业务标题和typed port标签，但不能复制selection、port container、node status或运行高亮实现。

## Decision: Preview执行正式Presentation链

Preview输入到最终画面的链固定为：

```text
Preview typed inputs
  -> CharacterPresentationFactFrame / parameter input
  -> current published Projection
  -> compiled Pose Plan
  -> source demand and capture
  -> one Animancer Evaluate
  -> PoseStateMachine / Slot / Blend / IK stages
  -> FinalPublication
  -> Preview Rig physical bones
```

`CharacterAnimationPreviewFixture`只提供精确角色Prefab、Animator/VisualRoot/Rig binding和可选world environment。它不保存Clip、PoseGraph、Source Slot、Foot Placement参数、Full Body IK参数或第二solver配置。

Fixture Session在隔离editor Scene和明确PhysicsScene中运行现有`AnimationPreviewRuntime`。普通Pose预览不依赖world environment；需要world-aware query且环境缺失时发布typed Unavailable，不创建假地面。

Preview transport只拥有Play、Pause、Step、Seek和Restart。Restart只重置Preview clock和该Preview runtime有限状态，不修改作者资产、Live Actor或已发布产物。

## Decision: Preview和Live共享观察表面，但输入所有权不同

目标选择器只包含：

- 当前精确Fixture建立的Preview Instance；
- RuntimeDebugSession已经证明Definition、Profile、PoseGraph、Projection、Rig和Tuning Layout匹配的Live Actor。

Preview目标允许提交Preview-only typed输入。Live Actor的Gameplay Fact和Runtime Input只读，作者不能伪造Speed、Grounded、Movement Mode或Gameplay State。

目标变化时，工作区必须释放旧interest、清空Applied值、运行高亮和diagnostics，再绑定新target。多个Live Actor时必须显式选择一个；不得广播到全部Profile消费者。

## Decision: 运行状态叠加在同一作者图

Graph overlay只读取匹配当前document、Projection revision和frame completion的正式snapshot：

- 当前PoseState高亮；
- 当前target State高亮；
- 当前Transition edge与progress高亮；
- 当前执行Pose节点、availability和contribution高亮；
- 未执行节点可以降低视觉权重，但仍保持可选择和可读。

Overlay不得修改Graph asset，不得保存第二份selection，不得打开runtime clone，也不得从Animancer state、作者默认值或旧target推断运行状态。revision失配时立即移除高亮并显示Stale。

## Decision: Details是唯一节点编辑与实时调参表面

Details只投影当前Node、State或Transition真正拥有的字段。字段由Capability或typed Profile tuning descriptor分类：

| 策略 | UI行为 | 运行行为 |
|---|---|---|
| `Structural` | 可在Authoring目标编辑；Live目标下结构只读；显示Build Required | 不发送candidate |
| `TunableDefault` | 修改正式作者owner并进入Undo；显示Authoring与Applied | 向当前精确目标发送完整candidate |
| `RuntimeInput` | Preview输入在会话输入区编辑；节点Details只读显示来源和当前值 | 由Preview Fixture或正式Gameplay每帧提供 |
| `DerivedReadOnly` | 只显示当前selection的结果或错误 | 不可写 |

Details不显示完整Tuning Layout、GUID、hash、dense index、workspace offset和其它内部实现。没有target时Applied显示`No Target`；target失配时清空旧值；没有当前selection关系的输入和参数不显示。

外部Profile正文仍由其唯一typed owner服务写入。Pose节点只保存Profile引用，Details可以嵌入该Profile当前节点实际消费的Tunable字段，但不得复制Profile数据或增加Profile专用runtime update。

## Decision: 保留完整candidate与帧边界参数块

已经实现的调参架构保持：

```text
typed Authoring Mutation + Undo
  -> current complete authoring closure
  -> CharacterPoseTuningParameterBlock candidate
  -> exact target identity validation
  -> Pending page
  -> next PresentationFrame before Prepare
  -> atomic Active page swap
  -> existing Pose runtime consumers
```

Runtime只保留一个Active block和至多一个Pending candidate。Candidate必须匹配Program、Projection、Pose Plan、Rig和TuningLayoutHash；失败时保留旧Active block并发布typed原因。

`NextFrame`字段从下一表现帧读取；`NextActivation`字段只被下一次Transition、BlendStack entry或Inertialization generation捕获。需要重建Rig、source、solver、PlayableGraph、workspace或改变容量的字段必须保持`Structural`。

实时修改同时保存正式作者值。退出Play或更换target后Editor candidate绑定失效，新实例从Character Build发布的default block开始；作者资产中的值仍然存在，并在下一次显式Build发布。

## Decision: Compile与Character Build必须分离

`Validate`只检查当前authoring document与Capability约束。

`Compile`只执行PoseGraph已有的轻量编译/候选生成边界，并把错误映射回Node、Port、State或Transition。它不得发布Character Program、Projection、Foot Analysis或Motion Matching Database。

`Character Build`调用当前Definition唯一正式Build事务，发布Program、Projection、Pose Plan、Tuning Layout和默认参数块。只能由作者明确点击。

打开窗口、恢复窗口、选择对象、切换target、播放Preview、修改字段、Undo、保存资产、AssetDatabase import与domain reload都不得触发Character Build。

## Functional Completion Order

### 1. 清理错误组合

- 从通用BTSMTL Shell移除Pose Preview专属布局要求，保持Tree/AI现有体验。
- 删除Action Animation作为PoseGraph入口。
- 删除重复Preview/Live/Diagnostics表面与全局Tuning表。

### 2. 建立唯一可运行上下文

- 从Presentation Profile打开现有PoseGraph。
- 创建唯一匹配Corin的Preview Fixture Session。
- 对齐当前发布Projection、Rig与Source Bindings；失配只报告，不fallback。

### 3. 恢复图编辑可用性

- 加载BTSMTL Node/Port/Edge/Inspector视觉资产。
- 恢复创建、连接、selection、Undo、StateMachine和子图导航。
- 让现有Root Graph打开后可读并定位内容。

### 4. 跑通正式Preview

- typed输入驱动现有PoseStateMachine。
- 正式Pose Plan写入Preview角色。
- 当前State、Transition和节点高亮。

### 5. 接通selection Details与调参

- 当前节点字段投影Authoring、Applied和应用状态。
- Tunable走现有candidate链；Structural要求Build；Runtime Input只读。
- Undo/Redo与target更换正确更新或清空Applied状态。

### 6. 收口显式编译发布

- Compile错误定位Graph元素。
- Character Build保持唯一显式发布入口。
- 删除旧UI与兼容代码。

## Failure Semantics

- 打开上下文不完整：显示Unavailable；不得绑定场景Host或Action call site。
- Preview Fixture不匹配：不创建Preview Session；不得选择近似Fixture。
- Projection、Rig或Source Binding不匹配：停止Preview并显示精确Stale/Invalid原因。
- Authoring Mutation失败：不保存数据，不生成candidate。
- Candidate编译或identity校验失败：保留作者值为Unpublished或Invalid，runtime继续使用旧Active block。
- Live target结束或revision变化：清空Applied与高亮，不自动附着其它Actor。
- Pose stage失败：继续遵循现有Presentation事务；Barrier前Discard，Barrier后Faulted。
- BTSMTL视觉资产缺失：阻断PoseGraph UI完成，必须修复正式资源路径，不允许回退Unity默认节点。

## Migration And Cleanup

1. 保留现有PoseGraph资产和已实现runtime tuning基础。
2. 把通用Graph交互从工作区布局中分离，恢复BTSMTL Tree/AI原体验。
3. 在现有PoseGraph窗口组合动画领域功能，不新建窗口。
4. 把Pose节点、State、Alias与Entry接到BTSMTL Node Visual Chrome和typed Port视觉。
5. 让Profile入口建立精确工作上下文并删除Action Animation入口。
6. 把全局Preview表单拆回Preview typed输入和selection Details；删除完整Tuning Layout列表。
7. 接通Preview Fixture、正式Pose Plan、运行高亮和Live target。
8. 删除旧Bottom Dock、重复Live Debug、裸ObjectField、窗口私有Graph装配和兼容开关。

迁移不保留旧PoseGraph UI模式、第二Preview窗口、Action跳转入口、全局调参表或Unity默认GraphView节点路径。

## Alternatives Rejected

### 把UE动画布局直接改进BaseTreeWindow

会破坏已经可用的BTSMTL逻辑图，让Tree和AI承担角色画面、动画输入和Pose字段。共享应发生在图交互内核，不是整个工作区布局。

### 新建一个PoseGraph动画窗口并保留旧窗口

会产生两套入口、selection、Undo和维护路径。现有`CharacterPresentationPoseGraphEditorWindow`必须原地收口。

### 通过Action Animation打开PoseGraph

Action需要解析有限Timeline和call site，PoseGraph需要Definition/Profile/Projection上下文。把两者串联会让无关Action歧义阻断PoseGraph，因此拒绝。

### 先做完整停靠和所有辅助页签

无法证明Preview、运行高亮和实时调参闭环，反而增加大量空面板。第一阶段只实现长期必要功能；布局自由度后置。

### 全局展示全部Tuning参数

作者无法判断参数属于哪个节点，也会把内部Layout暴露成业务界面。调参必须跟随当前selection。

### Runtime直接读取Profile或修改FinalIK组件

会绕过Projection、Pose Plan、唯一solver与帧事务。继续使用固定Parameter Block和typed consumer。

### 每次修改自动Character Build

Build是重操作，会卡住Unity并混淆作者修改与正式发布，因此只允许明确按钮。

## Dependency And Coordination

- `replace-pose-ik-with-finalik-full-body-solver`继续拥有IK数据、solver和diagnostics；本change只消费其当前正式Projection/Runtime合同，不修改IK Runtime。
- Blend Space、Motion Matching、Linked Pose和Secondary Motion继续拥有各自算法与资产；本change不复制其Preview或创建专用热调入口。
- Action Animation Workspace继续拥有有限Action作者闭环，但不再承担PoseGraph入口。
- 用户端到端验收负责确认画面、状态切换和运行时参数效果；这些人工步骤不写入tasks。

## Hard Stop Gates

出现以下任一情况必须停止对应实施：

- 需要改变BTSMTL逻辑图现有布局或功能才能完成PoseGraph；
- 需要复制GraphView、selection、Undo、StateMachine或Mutation实现；
- 需要从Action、Scene名称、对象顺序或last-used状态猜PoseGraph上下文；
- Preview需要第二Pose Plan、第二PlayableGraph、第二solver、假地面或直接Clip播放；
- Live tuning需要直接写组件、逐帧读ScriptableObject或重建运行对象；
- 需要自动Build或自动修复Stale产物；
- 无法把运行高亮和Applied值绑定到同一正式revision与frame lineage。
