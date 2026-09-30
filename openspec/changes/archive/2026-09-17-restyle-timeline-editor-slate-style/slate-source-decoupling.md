# Slate 原源码解耦决策

## 1. 结论与本次范围

2026-09-14 最新集中作者审阅见[editor-wiring-audit.md](editor-wiring-audit.md)：原UI逐项接线、资产写入范围、源曲线查看和未完成任务以该文档为当前索引。以下历史源码证据不自动等同当前状态；特别是SelectedClip派生、ApplyModify失败回滚与c167301eb绘图区初始化已有正确改动，不重新列为未改。

2026-09-14 新增Runtime规划已单列[timeline-direct-runtime.md](timeline-direct-runtime.md)。本文继续只约束Slate UI解耦与已确认截图修正；UI不运行角色。新增直接Runtime不使用Slate播放器，不把portable内容视为被禁止的自制Editor Model，也不改变本节各历史源码快照事实。仅登记规划，本轮不下发Runtime实现。

2026-09-13 后续领域协调更新：本文件原UI解耦决策保留；运行依赖以[领域运行方案](../2026-09-17-replace-character-program-with-domain-runtimes/design.md)为准，MotionCurve数据归属以[曲线源迁移](../../../specs/character-root-motion-curves/spec.md)为准。以下源码快照仍是此前阅读事实，不是本轮重跑检查。此次仅PLAN，不改代码或向实现任务发送授权。

可以保留 Slate 原 UI，同时去掉 BTSMTL 对 Slate 组件树和播放器的依赖。依据不是“看起来能适配”，而是下文列出的原函数、字段与调用点。实现需要修改这些函数的参数、数据访问和命令出口，有些原 UI 方法需要从 MonoBehaviour 实例搬到现有 Editor 模块；不需要重新设计轨道、Clip、曲线的绘制或鼠标算法。

本文件是实现设计，不是完成报告。2026-09-13 核对了 `ce21aec8f`、`afcb90056` 以及工作树源码；阅读期间 HEAD 为 `24e63cf3f`，其它任务可能继续提交。未启动 Unity、未执行 UI 操作或编译，不把源码分析当成端到端通过。

上次源码阅读的链路（ce21aec8f/afcb90056快照）：

```text
TimelineEditorWindow
  → BtsmtlSlateTimelineProjection.BuildProjection
  → 隐藏 GameObject + Cutscene + BtsmtlSlateGroup/Track/ActionClip
  → CutsceneEditorSurface 原函数
  → proxy snapshot/diff
  → TimelineEditorSessionContext.Apply → TimelineData.ApplyModify
```

上述快照只能说明当时原绘制入口已接回，不是“恢复完成”或无组件依赖完成。不能再引用旧提交声称当前没有组件树。

### 最新状态对账：01c68fb21及未提交工作树

2026-09-13 实现任务报告到01c68fb21《收口Slate代理差异的正式事务》。本次核对该提交存在，修改BtsmtlSlateTimelineBinding和Projection的事务接线；当前BuildProjection仍创建隐藏GameObject、Cutscene和代理Group/Track/ActionClip。正式binding已有真实消费者，不再是只有接口声明，但它尚未代替组件输入。

报告称两个ShowEmbedded替代绘制入口仍在；本次读取的未提交CutsceneEditor.cs及Slate目录搜索已不含ShowEmbeddedGroupsAndTracksList、ShowEmbeddedTimeLines或OnEmbeddedTimelineGUI。该源码文件仍有未提交修改，不能把此变化归到01c68fb21或当成完整交付。当前ShowGroupsAndTracksList仍读cutscene.currentTime，其他位置仍在代理identity与embeddedTimeline之间转换。

因此当前定性为“原生算法及正式命令逐步接线中，组件/绑定混合输入尚未收口”，不是恢复完成。后续保留已正确左侧Track/Group、正式新增、游标/帧号、曲线/缩放/鼠标代码；在同一原函数里逐项替换输入和命令，清除仍存在的替代分支与组件依赖，不整体回退、不新增第三套UI。以下第2–10节源码细节仍以原阅读快照理解，本段才是这次增量核对，不代表UI实跑或完整复审。

2026-09-14 当前实现更新：上述段落是历史快照，不能继续作为当前代码结论。`BtsmtlSlateTimelineProjection` 已改为只创建 `CutsceneEditorSurface` ScriptableObject，并把 `BtsmtlSlateTimelineBinding` 直接传入原 Slate Surface；`BuildProjection`、`CreateChild`、隐藏宿主、BtsmtlSlateGroup/Track/ActionClip、代理 identity 和差异快照路径已删除。现有 Slate 原入口在 embedded binding 下读取正式 Groups/Tracks/Clips，Clip 仍使用原 `ActionClipWrapper`，曲线仍使用原 `TrackEditorGUI`/`CurveEditor`。

当前仍未闭合的不是代理树，而是：Section 的无 Director 编辑、真实 owner Inspector、曲线展开状态恢复、部分 formal 手势和 Unity 端到端曲线保存验收。当前主工作区还被其他 Simulation Control 编译错误阻断，不能把此次源码收口说成运行通过。

目标只有一条编辑链：

```text
正式 TimelineData / Track / Clip / Section / Curve descriptor
  → 原适配文件迁移后的非组件 binding（引用正式对象与必要手势草稿）
  → 同一套 Slate 原函数、ActionClipWrapper、CurveRenderer、DopeSheetRenderer
  → 正式创建命令或本次编辑的差异
  → 同一 Session.Apply / TimelineData.ApplyModify / serialized owner Undo
```

不启动新的 OpenSpec change，不改运行算法、C# 输出生成协议或资产，不重新造 UI。旧实现记录保留历史；本文件取代旧“DirectProjection 已完成解耦”的当前状态说明。

## 2. 原 UI 实际分布与耦合证据

下列路径均以 `3cDemo/Client/3C_Client/Assets/` 为根。函数名是长期定位点，行号仅对应本次阅读快照。

| 文件与定位点 | 原来提供的作者功能 | 与组件/运行绑定在一起的代码 | 处理决策 |
|---|---|---|---|
| `ParadoxNotion/SLATE Cinematic Sequencer/Design/Editor/Windows/CutsceneEditor.cs`，`OnGUI`，约1320行 | 同一布局、滚动、输入调度和绘制顺序 | `cutscene == null` 阻断；Prefab门禁；`cutscene.Validate()`；层级 Undo；Layout 阶段重新赋值 `track.clips`；场景重采样 | 原调度不换，输入是否存在、能否编辑、排序视图、事务和刷新改接绑定 |
| 同文件，`ShowGroupsAndTracksList` / `ShowListGroups` / `ShowListTracks`，约2235/2306/2449行 | 搜索、折叠、图标、选中背景、轨道拖动排序、分隔宽度、菜单 | `CutsceneGroup`/`CutsceneTrack` 实例；`GUI.enabled = cutscene.currentTime <= 0`；Actor字段；Director特殊类型；组件类型菜单与删除 | 原函数主体就地接可读集合和正式命令，不能 `if (formal) ShowEmbedded…; return` |
| 同文件，`ShowTimeLines` / `ShowGroupSections`，约2546/2933行 | Clip布局、折叠缩略图、时间网格、Section、GUI.Window、多选 | `cutscene.groups/directorGroup`；组件列表；Section循环退出语义；`0.1f`间距；原生同类型交叉混合规则 | 原布局和手势保留，业务间距/重叠规则、Section数据和集合改接正式合同 |
| 同文件，`ActionClipWrapper.OnClipGUI` / `DoEdgeControls` / `StartClipAdjust` / `EndClipAdjust`，约3308行起 | 点击、拖动窗口、左右裁剪、Blend手柄、DopeSheet、缩放关键帧 | `ActionClip`字段；反射判定CanScale/CanBlend；双击读取actor；绘制期间给相邻Clip写blend | 同一个wrapper只接一种编辑接口，保留鼠标分支；能力读正式Capabilities/合同；派生混合不写作者SelfEase |
| `…/Framework/CutsceneTrack.cs`，`OnTrackInfoGUI` / `DoDefaultInfoGUI` / `DoParamsInfoGUI` / `OnTrackTimelineGUI` / `DoClipCurves`，约359/373/439/552/613行 | 左侧名称与曲线按钮、参数行、展开高度、关键帧小按钮，右侧逐通道DopeSheet和曲线 | UI方法藏在MonoBehaviour中；选中Clip强转ActionClip；`IKeyable.animationData.animatedParameters`；场景Add Property；原生AddAction | 搬迁这些原Editor方法及原高度状态到现有Editor模块并参数化，两种来源共用这一份；不是另写Track绘制器 |
| `…/Design/Partial Editor/CurveEditor.cs`，`DrawCurves` / `CurveRenderer.Init` / `Draw`，约21/101/321行 | Unity原生CurveEditor的关键帧、切线、框选、缩放和平移 | `IAnimatableData/IKeyable`输入、全局吸附、全局cache、匿名Undo订阅 | 保留反射构造`UnityEditor.CurveEditor`及原onGUI；修改数据、帧吸附、事件和释放接线 |
| `…/Design/Partial Editor/DopeSheetEditor.cs`，`DrawDopeSheet` / `RecordUndo`，约19/565行 | key聚合、选择、移动、复制粘贴、缩放、切线模式 | `keyable.root.currentTime`读写、把keyable强转Unity Object记录Undo、原参数通知 | 不换Renderer；时间操作接编辑游标，写入接同一草稿事务 |
| `…/Design/Partial Editor/AnimatableParameterEditor.cs`，`ShowParameter` / `ShowMiniParameterKeyControls` / `DoParamGearContextMenu` | 参数折叠、Value、前后key、加删key、齿轮菜单 | `RootTimeToLocalTime`、`IsRootTimeWithinClip`、`GetCurrentValue/SetCurrentValue`、录制/表达式/场景属性反射 | 原布局和控件保留，值读曲线或输入草稿；只保留正式通道支持的编辑命令 |
| `…/Design/Editor/Inspectors/ActionClipInspector.cs`，`ShowErrors`，约79行 | 原IN/OUT、Blend、子片段和参数Inspector | Actor校验、ActionClip target、base序列化Inspector、`action.Validate()` | 将原可复用GUI方法参数化，合法性与typed字段改接正式Clip；不创建假Actor |
| `…/Design/Partial Editor/CutsceneUtility.cs`，`selectedObject`，约54行；`…/Design/Editor/Inspectors/CutsceneInspector.cs`，`DoSelectionInspector`，约182行 | 选择反馈与Unity Inspector联动 | 选择写`value.root.context`；选中元素强转Unity Object；`Editor.CreateEditor`；Cutscene Inspector延迟ReSample | 选中正式ID，使用真实serialized owner作为Inspector宿主；不选代理GameObject，不清空全局Selection冒充修复 |

原源码不是“整套都不能用”：曲线Renderer甚至已经支持直接以`AnimationCurve[]`构造。也不是“换个根对象就全好了”：`IKeyable : IDirectable`、`IDirectable.root : IDirector`、`IAnimatableData`里仍有Validate、Evaluate、Snapshot、Transform和AutoKey等运行职责。用空实现凑这些接口，只会继续保留假的运行依赖。

## 3. 决策D1：同一套原函数，不按数据来源分成两套UI

2026-09-14 用户对反复未合并提出质疑，具体收口差异见[审阅A01](editor-wiring-audit.md#a01-两份绘制主体和简化拖动仍存在)。只能保留一份原列表/时间轴循环和鼠标算法；原入口提前返回同名binding重载仍是两套实现。主体合并与其它功能补丁分开记账，后者不能证明前者完成。RootMotion显示与正式求值时间不一致的补充见审阅A04，不能以只读曲线已经出现作为完整交付。

保留 `OnGUI → ShowGroupsAndTracksList → ShowListGroups/ShowListTracks` 与 `ShowTimeLines → ActionClipWindow → ActionClipWrapper.OnClipGUI`。修改原函数所读集合、属性和命令。不增加 `ShowEmbeddedGroupsAndTracksList`、`ShowEmbeddedTimeLines`、另一份Clip wrapper事件分支或自行绘制曲线的函数。

为解除实例依赖，允许把 `CutsceneTrack` 的原Editor方法和状态原样搬迁到现有Slate Editor模块，例如同一 `CutsceneEditorSurface` 的partial文件。迁移后旧方法只在仍有真实原生调用时作为薄转发，不能留下两份函数主体。参数名/类型和业务判定可以变；Rect计算、样式、事件消费顺序、GUI.Window、GUI.DragWindow和曲线Renderer调用链必须能追溯到原实现。

真实Slate Cutscene编辑入口仍有消费者：`CutsceneEditor.ShowWindow`和`CutsceneInspector`。它可以用面向真实Cutscene的数据适配读取自己的组件；BTSMTL用正式Timeline适配。两者在入口选择明确的数据来源，调用同一套原UI；不是BTSMTL失败时退回Cutscene，也不是保留两套轨道绘制。原生专有Actor/Render/场景录制只能由真实Cutscene入口提供，BTSMTL接口不携带这些能力。

业务取舍：保留两种真实数据消费者的一份UI，代价是维护原插件源码补丁；删除原生独立窗口会影响用户其它真实Cutscene用途，当前没有授权，因此不删除。不能为了避免接口修改而保留BTSMTL代用组件。

## 4. 决策D2：接口只描述编辑需要的数据，不伪装播放器

在现有EmbeddedTimelineBinding.cs的位置收窄编辑接口，删除确认无消费者的通用EditorModel。迁移Projection中正确的映射、typed创建、曲线换算和提交；最终只由BtsmtlSlateTimelineBinding接正式数据。01c68fb21已包含该binding类，但Projection组件输入仍存在，不能仅凭类已建立声称解耦完成，也不重新创建另一份binding。

| 编辑接口提供什么 | 数据/行为实际属于谁 | 不允许放进去什么 |
|---|---|---|
| 文档名称、作者帧率、内容范围、已有轨道/Section访问、命令资格 | TimelineData与现有Session、ContractCatalog | Cutscene、context GameObject、Play/Sample、假IDirector |
| 元素ID、显示信息、当前时间范围、相邻元素、支持的裁剪/混合/资源操作 | 正式对象、Capabilities和contract；手势期间读本次草稿覆盖值 | 每个Clip种类另一套序列化类、代理组件类型作为业务kind |
| 原曲线工具所需的曲线数组、通道名/颜色/值域、局部时间、key命令、修改通知 | TimelineCurveChannelDescriptor、原曲线操作算法、正式Curve mutation | Transform、Actor、任意场景属性路径、空实现的Initialize/Enter/Evaluate |
| 选中ID、折叠、曲线展开、行高、搜索、滚动、视野、GUI交互状态 | 当前窗口及原wrapper/Renderer的Editor状态 | 持久化为另一个Timeline、进入Compiler/C#导出、改变运行Session |

集合只是正式对象的访问视图；显示适配可缓存正式引用和ID，但不得复制整个Timeline成为可独立保存/编译/运行的模型。正式对象直接getter会受影响的手势字段，以及不能直接修改正式引用的曲线，才保留草稿。

不让BTSMTL binding实现`IDirector`，也不为了获得曲线UI实现带运行方法的`IKeyable`/`IAnimatableData`。在原Editor函数参数处拆出实际使用的编辑子集；原生AnimatedParameter也接入这一子集，原运行接口留在真实Slate运行端。这里确实需要接口改动，但没有新绘制算法、新业务模型或新运行模块。

原UI从组件虚方法获得的专有绘制扩展，只保留有真实原生消费者的扩展口；不能把整个轨道绘制委托给BTSMTL binding。BTSMTL binding负责数据和命令，Slate模块负责画与交互。

## 5. 决策D3：Track、Group、Section不是Actor或Director的别名

- 左侧继续使用原Group标题行和Track行。BTSMTL只有一个对应当前文档的显示根，不为此向TimelineData新增业务Group，不创建CutsceneGroup，不显示Actor字段。根折叠是窗口状态。
- Track集合与业务顺序来自正式Timeline；原拖动排序标记、鼠标拾取保留，释放后提交正式顺序。按开始帧排序以供显示/寻找邻居的是临时索引，不能在Layout重写正式`Clips`集合。GUI整数ID不是AuthoringId。
- 原`OnTrackInfoGUI/DoParamsInfoGUI/DoClipCurves`共用一份原高度状态与曲线选择，不能左边按新公式、右边继续按旧组件高度。
- `PersistentMuted`是已有作者数据；运行active状态只作叠加显示，不能经`track.isActive`混进muted保存。锁定/折叠若没有正式保存字段，就只作为窗口编辑状态，不发明业务字段。
- Section在正式Timeline上，不挂到默认DirectorGroup。原`ShowGroupSections`改为直接接Section集合，保留标记、拖动和双击聚焦。Intro/Outro边界只作画图哨兵；不得保存成正式Section，也不得跳过位于第0帧的真实Section。
- 原Section的循环次数、ExitMode、currentLoopIteration属于Slate运行含义，未有正式字段就删除对应BTSMTL菜单；原`0.1f`间距改由正式帧/Section规则提供。

业务取舍：保持作者熟悉的Slate轨道组织和交互，不把它的镜头/Actor分组强加给技能。正式数据将来增加能力应由原domain定义，不能因为原UI有按钮就造业务配置。

## 6. 决策D4：保留手势算法，业务重叠与混合不能照抄

`IDirectableExtensions.CanScale/CanBlendIn/CanBlendOut`通过组件属性反射判定能力，`CanCrossBlend`比较两个CLR类型。BTSMTL已有`Clip.Capabilities`、`TimelineTrackOverlapPolicy`（Reject/Parallel/Blend）及正式contract；替换的是这些业务谓词，不是鼠标算法。

| 原操作 | 保留的UI行为 | 正式写入 |
|---|---|---|
| 移动Clip、多选移动、Ripple | GUI.DragWindow、吸附候选、引导线、整组偏移计算 | 一次操作的所有Start/End改动，一次Session提交；不得逐Repaint写入 |
| 裁剪、子片段偏移、缩放key | 左右边缘命中、控制键语义、原关键帧缩放算法 | 按Clip能力写Start/End/ClipIn；不支持的正式能力明确禁用 |
| Blend手柄 | 原三角/边缘图形与拖动 | 只提交作者明确修改的SelfEaseIn/Out；不把UI推导的重叠当成SelfEase |
| 重叠时的显示 | 原颜色、相邻关系与Blend图形 | Reject限制放置；Parallel允许叠放但不自动混合；Blend按原正式规则派生 |

源码里`ActionClipWrapper.OnClipGUI`会在没有拖动时给`action.blendIn`和`previousClip.blendOut`赋重叠值。这不是纯绘制，必须去掉BTSMTL路径的这类作者写入。正式`Clip.UpdateMix`已区分`OtherEase*`和`SelfEase*`，应使用同一正式规则计算派生结果，不在Slate复制第二套混合规则。缺少可供编辑草稿使用的领域入口时，在原Timeline模块补该入口，不在UI自己解释OverlapPolicy。

不能把“保留原算法”误解为保留Slate不适用于本项目的半长度交叉混合限制。原鼠标选择/拖动算法保持；合法放置范围、能力、派生混合归正式业务。这是业务适配，不是另一套UI。

## 7. 决策D5：曲线编辑器不换，去掉假Animatable字段

当前proxy为每个正式Channel声明`[AnimatableParameter] public float …`，再用`ParameterNameFor`映射；随后反射创建AnimatedParameter、拷贝曲线。这要求每增加通道都增加一个假的组件字段，且把场景字段值与曲线值混在一起。最终删除这层重复字段与字符串映射，直接按正式descriptor提供参数行。

具体接线：

1. `DoParamsInfoGUI`仍画原通道行、齿轮和选择；枚举对象从`animationData.animatedParameters`换成正式descriptor对应的编辑参数。名称、颜色、值域、单位和可用性只从descriptor读。
2. `DoClipCurves`仍计算原裁剪区域并调用同一个`CurveEditor.DrawCurves`/`DopeSheetEditor.DrawDopeSheet`。参数改接编辑曲线和局部时间，不要求ActionClip类型；按clip范围判断裁剪，不使用`is ActionClip`分支辨认身份。
3. `CurveRenderer.Init/CreateDelegates/GetCurveWrapperArray/Draw`继续使用原Unity CurveEditor反射和onGUI，不自绘折线、key、切线手柄，不用简化CurveField替代。
4. `DopeSheetRenderer`保留key聚合、框选、复制粘贴、重定时和切线方法；`keyable.root.currentTime`换成当前窗口编辑游标接口；`RecordUndo`换成现有手势事务入口。
5. `ShowParameter/ShowMiniParameterKeyControls`保留原按钮和布局。Value来自当前草稿曲线在编辑帧的值或作者正在输入的值；加key写该值，前后key只定位编辑游标。静态`AnimationCurve.Evaluate`读取曲线数值不等于执行`Cutscene.Sample`，允许前者，禁止后者。
6. 原key/切线数学操作继续共用`CurveUtility`等原代码。需要从AnimatedParameter拆出纯key操作时迁移原实现，不在适配层再抄一份。正式channel若无“禁用参数/表达式/添加场景属性”合同，不显示这些菜单，也不造相应配置。

真正Timeline-local的Weight/Ease、Warp progress等曲线继续沿正式descriptor换算：normalized域时`t秒 = u × D`、`tangent秒 = tangent归一化 / D`，写回反向换算，weight/WeightedMode/wrap保持。只对作者移动/新增的key按帧吸附。

Motion源XYZ/Yaw不再参与这条局部可写链：由RootMotionCurveAsset拥有，但在Timeline中作为带Reference标记的只读参数复用同一套DopeSheet/CurveEditor显示；source字段通过正式Inspector配置并沿已有入口打开源owner。源XYZ/Yaw不经Timeline mutation改源；旧CurveEndFrame等价迁移和源秒映射归曲线owner，不能在Slate适配里保留另一套可编辑源资产或另一套采样公式。

修改原Renderer的cache释放与通知：缓存身份归当前窗口/通道，不因每次重建对象丢失曲线视野；关闭解除所属订阅并清理对应条目。当前匿名`Undo.undoRedoPerformed`订阅无法逐实例解除，需要改成可解除的原处理函数。不能只销毁隐藏GameObject就认为曲线缓存已释放。

业务取舍：直接descriptor接线使新增正式通道不再修改Slate假字段；代价是参数工具的接口需要改。继续伪造带float字段的对象虽然能少改参数签名，却保留重复定义与运行协议，因此不是最终方案。

## 8. 决策D6：Inspector去掉对象前提，不是去掉属性编辑

`ActionClipInspector.ShowErrors`在Actor为空时只退出ShowErrors；`ShowCommonInspector`随后仍画IN/OUT、Blend等控件。不能说这一个return导致整个参数面板不绘制。Actor报错本身仍不适用于正式Timeline。

仅增加`RequiresActor=false`覆写点可以消掉这条报错，但`target as ActionClip`、`base.OnInspectorGUI()`、组件序列化、`action.Validate()`、root时间与CutsceneInspector的场景选择/重采样仍存在。因此不以这个补丁代替本轮解耦，也不绑定真实/假Actor让报错消失。

复用原`ShowInOutControls/ShowBlendingControls/ShowSubClipGUI/ShowAnimatableParameters`的控件实现，把需要的数据/命令作为参数；原生Unity Editor外壳只负责真实target生命周期。BTSMTL使用已有真实`SerializedOwner`和`SerializedPropertyPath`，选中元素按AuthoringId解析，接同一份原GUI。正式类型专有字段用既有`TimelineAuthoringClipBinding.Read/Configure`和正式字段定义接入，不展示proxy私有字段或整套Cutscene Settings。

属性放在Unity现有Inspector，不在Timeline内部恢复右侧自制Inspector、SplitView或第二套属性面板。`TimelineInspector`当前只有完整`m_Data` PropertyField，并不等于已经接好了选中Clip的原Slate Inspector；这是明确的待实现接线。Inline内容使用其真实Graph serialized owner，不创建ScriptableObject假Clip作为Editor target。若其现有Inspector由其它任务修改，只扩展共享选中内容入口并先处理实际文件冲突，不覆写其它图编辑功能。

删除自制右侧面板，不等于只能新增时填参数、以后不能修改。普通字段编辑也必须有正式入口；资源/Graph导航与作者字段写入分别处理。不在OnInspectorGUI扫描全项目、重建projection、编译或采样；只画已解析字段并提交明确编辑，刷新沿现有通知路径执行。

## 9. 决策D7：草稿不是第二套数据，Undo也不能双写

现有正式写入链必须保留：`TimelineEditorSessionContext.Apply → TimelineData.ApplyModify → SerializedOwner Undo/dirty`。源码中的ApplyModify只是注册Undo、调用action、标dirty，没有自动校验或异常回滚；不能仅因调用它就声称事务完整。

- 手势开始：记录本次涉及元素的正式ID、来源revision和修改前字段；曲线编辑获取隔离的曲线草稿。搜索、选择、游标、缩放和折叠不开始作者事务。
- 拖动过程：原wrapper/Renderer改草稿；正式对象不逐帧写入，也不单独记录Slate组件Undo。原生Unity曲线工具自己的key选择历史与正式业务Undo需区分，不能再注册另一份资产撤销。
- 手势结束：只有实际变化才转换为正式字段/完整曲线，以原contract和Curve validator检查完整改动，再一次Session提交。多个Clip、Ripple、Section带动Clip属于同一修改集合。
- 菜单与字段：在实际菜单回调、字段确认处建立一次事务，不依赖窗口已经发生过MouseDown；返回前提交或报告错误。避免外层MouseUp再提交一次。
- 取消/过期：丢弃草稿并恢复视图，不向正式资产写回旧值；revision不同明确提示，不静默吞输入。关闭只丢弃未提交手势，不撤销已提交内容。
- 提交失败：在现有正式mutation实现内完成该次owner范围的恢复，不能留下部分Track/Clip修改；不新增一套全项目事务框架，不把Unity Undo存在等同于自动回滚已实现。
- 结构操作：直接调用正式新增/删除/排序/复制命令，不先AddComponent/DestroyImmediate再从组件差异推测用户意图。复制保留完整正式配置、生成新ID，不走Slate JsonUtility复制代理。

取舍：直接把可变AnimationCurve交给原UI虽更短，却会绕过取消与Undo；保留一次手势草稿有必要。禁止的是可持久化/可运行的第二份模型，不是必要的编辑中间值。正确的强类型配置、曲线换算和Session保存代码迁移复用，不重新实现。

## 10. 决策D8：编辑时间、运行观察与场景生命周期分开

原`DoScrubControls/ShowTimeInfo/DoZoomAndPan/StepForward/StepBackward`继续使用；读写现有编辑帧和视野，不通过Cutscene.currentTime。帧率从Session取得，不写Slate全局Prefs来同步；曲线吸附也从同一编辑上下文取得。

`GUI.enabled = cutscene.currentTime <= 0`换成正式编辑资格，不能让游标离开0帧就无法新增Track，不能因Scene Play正在运行就禁止作者编辑。`TimelineEditorSessionContext`当前只读异常文案仍称Scene Play/Live Debug，文案不能代替真正资格判断，需与实际owner条件一起收口。

运行/历史overlay仍消费已有`TimelineRuntimeObservationBridge`和正式runtime binding，既不写muted/StartFrame，也不复用编辑游标保存其位置。Scene Play Actor只在原运行上下文存在，不注入Slate Actor接口；关闭Timeline不停止Session。

播放语义不在本次解耦中擅自改动：帧定位和逐帧已确认；Timeline内Play是否作为正式Scene Play快捷控制，与纯游标自动前进不是一回事。保留已有正确入口，不加假播放器、不以Slate Play/Sample替代正式预览；若需新增或改变Play行为，先明确其正式命令目标。本文件不把实现消息中的“播放已补回”当作运行语义已确认。

### D8补充：预览接入领域装配而不是总包采用

- 原Scene Play协调器及领域模块继续是运行owner。技能只构建技能，活动实例固定启动版本；Pose通过同一原生Factory显式重建并重置历史；Camera通过正式绑定/Reset；控制和网络变化按Session规则重新准备。
- UI消费场景目标、领域就绪结果、配置版本、实际采用版本/实例generation及失败原因；请求接受、准备成功、实际采用分开报告，不根据UI hash判断生效。
- 撤掉角色全量Build、整包Projection、统一ProgramEpoch与Document/v7前提，不换名造总包。保留Float32/Fixed、网络Pipeline/Pass和独立资源处理。
- 纯Timeline仍需真实非Skill调用方、内容及播放身份。Slate原曲线/手势不承担角色工厂、CameraBuilder、技能编译或运行采样。
- Slate编辑与UI适配归Timeline；MotionCurve/MotionWarp源配置binding归曲线迁移；Timeline.Camera.cs归摄像机；装配归领域运行迁移。完整接入信息见[联动计划](preview-integration-plan.md)。本次仅规划，不新增实现授权。

## 11. 实施顺序与明确删除范围

这是依赖顺序，不是让用户重新选择目标。每步按职责中文提交，不能因“小步提交”而把半条接线称为可交付；也不能回退已正确功能。允许内部源码迁移尚未完成，不添加临时fallback。

1. 从恢复基线保留原函数主体；将隐藏在Track组件里的Editor方法参数化并迁入同一Editor模块，原生消费者继续共用。原wrapper改成单一编辑输入，保持原鼠标事件结构。
2. 在原参数工具、Curve/DopeSheet入口接入编辑数据子集和时间/事务回调，去掉BTSMTL的IKeyable/IDirector运行前提；Inspector的原控件同步接正式owner。
3. 迁移现有BTSMTL映射为非组件binding，复用typed创建、descriptor换算与Session提交；原列表/时间轴/菜单/Section均直接消费它。原生业务门禁改接正式contract，不能靠旁路绘制实现。
4. BTSMTL正式打开入口一次切到该绑定，并删除`BuildProjection`、`CreateChild`、`__BTSMTL_SlateTimelineProjection__`、`BtsmtlSlateGroup/Track/ActionClip`、组件字典与专属扫描/销毁；删除已无消费者的EditorModel、过时接口和meta。保留真实Slate组件给原生消费者，不删除正式Actor/Camera资产。
5. 收口原选择、Undo、菜单回调、曲线cache和生命周期，移除BTSMTL的CutsceneUtility选择/JSON/采样调用及无意义能力；保留正式runtime observation、Session和版本采用路径。

每步交付说明列出原函数与当前函数的对应、改了哪项数据/命令以及还剩什么，不能只报类名。若diff出现新轨道绘制循环、新独立时间轴事件链，或BTSMTL重新实现完整Renderer，就已经偏离，不能继续补丁扩展。

## 12. 文档冲突与状态纠正

最新截图问题与本次授权修正见第13节。第1节及本节早期状态均按对应快照理解，不覆盖后续正确实现或任务进度。

- 旧design第3.5节和tasks第11节把DirectProjection及无组件链写成已完成，与`ce21aec8f/afcb90056`后的源码冲突；本次纠正为恢复原UI但仍有组件代理，解耦任务重新列为未完成。
- proposal、delta中的“保留右侧Inspector”、design旧布局图，与后续明确“不新增右侧自制Inspector”冲突；统一为复用原Slate控件，属性使用Unity已有Inspector，不能因此删除正式属性编辑能力。
- 原tasks“只从原入口分派”不足以约束复用：原入口提前return到另一套绘制不算。本次明确要求同一个函数主体、同一个wrapper/Renderer，允许只为脱离MonoBehaviour迁移原方法。
- current `openspec/specs/btsmtl-timeline-editor-preview/spec.md`仍要求TimelinePreviewSession与互斥LiveDebug/只读，和已决定的Graph Shell场景预览不同；仍由场景预览change的delta处理，不在本次安装未实现规范。
- 早期文档误用了不带btsmtl前缀的路径；当前已定位`openspec/specs/btsmtl-timeline-animation-authoring-surface/spec.md`。源曲线owner和局部Curve要求在current规范已更新，不再照抄旧XYZ/Yaw局部写入条款。
- `implementation.md`包含旧回退前状态，仅保留历史，本次不改其所属任务记录；不能拿旧的“无组件、无Actor、已编译”当当前结论。没有新增测试、验证或编译任务。

## 13. 2026-09-14 截图回归：轨道错位、重复文字与DopeSheet

用户截图为CorinDodgeForwardTimeline：轨道名称/曲线按钮挤到搜索框附近，轨道背景行空白；Clip名称重复，底部条带也显示名称。用户明确要求更新文档并让现有实现任务修改。本节只授权修正这些原UI接线问题，不改变外观目标，不恢复组件树，不扩大为运行/预览重构。

源码依据来自本轮截图诊断，不是Unity实跑验证：

| 症状 | 已读到的调用点 | 原因与修正要求 |
|---|---|---|
| 左侧多条轨道标题堆到列表顶部 | CutsceneEditor的binding列表循环直接调用TrackEditorGUI.DrawParametersInfoGUI；DrawDefaultInfoGUI内部用y=0的Rect画名称/按钮 | 原版在轨道内层GUI.BeginGroup(trackRect)后画局部内容，当前调用缺少轨道坐标转换。恢复原嵌套分组/裁剪：行背景在列表坐标，轨道内容在本行局部坐标，Begin/End成对；不能只改字体或增加固定Y偏移 |
| Clip标题重叠 | ActionClipWrapper.OnClipGUI末尾画editorBinding.Info；FormalClipEditorBinding.DrawClipGUI此前也画Info | 标题由原wrapper唯一负责，binding不再重复画相同标题。本轮已看到未提交差异删除binding中的GUI.Label，必须保留这项正确改动，不再重复修复或回退 |
| 底部条带重复Clip名、无真正key | ShowClipDopesheet的FormalClip分支画Info后return，未调用原DopeSheet | 删除文字替代路径，正式局部曲线/参数必须接同一DopeSheetRenderer的真实关键帧操作与正式事务。无局部曲线时不占用假DopeSheet高度；不能用标题、假key或只读装饰条顶替 |

### 坐标与绘制归属

- 左侧列表区域建立一次列表坐标；每条轨道建立本行局部坐标，内部名称、图标、曲线按钮、通道与小控件相对本行计算。Track参数GUI不重复建立同一偏移。事件命中与图形必须处于同一坐标空间；滚动、曲线展开后仍使用原行高与裁剪规则。
- 原wrapper在GUI.Window局部坐标画Clip标题一次；DopeSheet仅画真实key。外部运行状态仅在有真实报告且文本非空时显示独立状态，不把Clip名称或无状态占位再次覆盖标题。
- 无曲线Clip的HasParameters/HasActiveParameters、底部高度与dragRect必须一致。不能只隐藏底部文字却仍保留空条带或挤占拖动命中区；有曲线Clip则保留原DopeSheet命中与手势。
- 修正落在原列表/Track参数GUI/ActionClipWrapper与既有binding接线。仍然必须合并重复绘制主体，不能给两条分支分别补坐标后就称为最终共用UI。
- 只使用真正Timeline-local的Weight/Ease、Warp progress等参数；源运动XYZ/Yaw沿曲线owner只读/导航规则，不能为了让DopeSheet有key而恢复旧源曲线写入。

### 实现边界

沿现有goal和小步提交继续：保留已正确左侧Track/Group、正式新增、帧/游标、曲线、缩放与鼠标行为；不整体回退共享工作树。当前未提交改动先按实际diff接续，遇到实际冲突交用户决定。代码/文档/编译通过不代表截图回归已消失，未在真实窗口确认时必须明确说明，不新增测试或验证tasks。

## 14. 2026-09-14 曲线区选择与参数接线回归

用户后续截图为CorinAttack2Timeline：Unity Inspector显示Selected Timeline Element及MotionCurveClip的Weight/Ease In/Ease Out曲线，但Timeline左侧展开区显示No Clip Selected，右侧曲线区提示选择Clip；Console底栏出现Event.Use不应处理Layout事件的警告。用户要求更新文档并由现有timeline任务修正。

### 已确认事实与尚未定位的原因

- Timeline.CurveAuthoring.cs已注册MotionWeight、MotionEaseIn、MotionEaseOut，分别读取MotionCurveClip的WeightCurve/EaseInCurve/EaseOutCurve。这三条是正式Timeline-local曲线，应在原Slate时间轴内编辑，不因源XYZ/Yaw迁移而消失。
- BtsmtlSlateTimelineBinding.Select同时设置Inspector的serialized property路径、各Track.SelectedClip与Session selection；Rebuild会重建binding并按选中ID恢复。这是需要追踪的实际接线，不等于运行时所有引用必然一致。
- TrackEditorGUI.DrawParametersInfoGUI与DrawClipCurves把SelectedClip为空、Parameters为空、Parameters.Count为0合并为同一“未选中”提示。截图不能区分这几种情况，不能直接断言一定是选择丢失或曲线Catalog没注册。
- TimelineInspector目前通过独立选择路径显示普通PropertyField。它显示选中字段只能证明该路径能解析，不证明当前Surface/Track的SelectedClip和参数已同步；右侧普通曲线框不等于Timeline曲线编辑完成。
- Layout警告确实出现在截图中，但尚未确认其调用栈与曲线不显示的因果关系。不得未经定位就把它说成唯一原因或屏蔽Console。

### 修正合同

1. 原Clip点击后，唯一正式选择以owner、Clip AuthoringId为依据，Inspector、当前Surface、所属Track.SelectedClip和曲线通道均解析同一个有效对象。不能额外建立第三份选择源。选轨道/点空白的行为应准确区分，不误清刚选中的Clip。
2. 提交、Undo/Redo和binding刷新后，重新解析该ID及其所属Track，并恢复仍存在的选中通道。无效引用不得残留为“Inspector选中旧路径、Timeline没有当前Clip”；真实删除对象时同步清空相关选择，不自动选首个Clip掩盖问题。
3. 追踪本次Clip的descriptor收集、Supports过滤、Parameters生成及Track关联，在实际失败点修正。未选Clip、Clip确无局部曲线、已选Clip但绑定失败应显示不同且准确的提示；缺失正式绑定属于未完成，不冒充合法空曲线。
4. 作者应能在Timeline中点击Clip、展开轨道、选Weight/Ease通道，进入同一原Slate CurveEditor/DopeSheet，完成key时间/值/切线编辑并通过现有Session/Undo保存。Inspector可同步展示字段，但不能代替上述时间轴编辑入口。
5. Event.Use只消费对应的实际输入事件，不在Layout/Repaint路径消费。沿真实触发调用栈修正事件类型与处理时机，不加吞异常或全局屏蔽；不声称修掉警告就等于修好曲线选择。

本节不恢复源XYZ/Yaw的局部写入，不新建曲线渲染器/选择服务/Inspector，不改运行时与预览算法。第13节已正确坐标和重复标题修复保留。以前勾选代表对应实现记录，不作为本截图功能正常的证明；本次剩余项单列tasks 11.15–11.17，不新增测试或验证任务。
