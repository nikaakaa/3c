## Context

2026-09-13 方向纠正：用户否决将 Slate 改造成另一套纯内存编辑器，实现窗口已通过 0aa52f209 回退这轮改造。本 change 撤销纯内存 Surface、Editor Model、Clip交互和 Curve/DopeSheet 全面改造任务。以用户要求回退到的真实 Slate 功能为实施基础，复用原有绘制、交互、曲线工具，只做正式数据/命令和必要布局适配。回退完成情况由实现窗口记录，代码回退由该提交记录，本次规划更新不代表后续绑定替换已完成。

2026-09-13 公共作者基线采用 [remove-agent-authoring-use-native-csharp/design.md r2](../remove-agent-authoring-use-native-csharp/design.md)。TimelineData 是正式业务对象，Slate 是现有编辑投影；显式 C# 导出/生成替代旧 Agent Document/五工具。旧日期的 UI 问题记录仅作历史，已完成交互、布局、选择、Undo、owner、时钟和 Camera 轨道按最新实现保留。该轮作者协议分工继续有效，本轮仅撤销扩大成重做编辑器的错误规划，既有 UI 行为与预览保持。

2026-09-12 后续授权：用户要求将“预览”任务一起规划、后续随 Timeline 实施。跨窗口布局、场景/目标选择、技能与纯 Timeline 预览、运行标记、编辑后采用和历史操作的统一计划见 [预览联动计划](preview-integration-plan.md)。本文件继续定义 Timeline 编辑表面，两边使用同一排期而保持已有数据/运行归属。

目标是让作者在唯一 Timeline 窗口里完成：新增轨道 → 添加 Clip → 调整帧范围 → 编辑属性/曲线 → 撤销重做 → 保存重开，并通过已有 SkillGraph 场景预览查看实际效果。使用 Slate 的真实 Track/Clip/Curve UI，正式数据仍属于 BTSMTL TimelineData。

2026-09-12 对账：已有临时投影、Clip/部分曲线显示和部分回写代码；新增、帧几何、视图恢复与整体布局未完成。此前编译通过、截图显示和任务勾选不能证明编辑闭环成立。以下是实施目标，不是实现完成报告。

用户已确认单窗口、真实 Slate UI、项目自有数据/运行内核、预览期间可编辑且不更换 Session。用户尚未确认 Timeline 内 Play 的具体意义，不能把此前“只让游标线移动”的建议当成需求。

## Goals / Non-Goals

**Goals:**

- 从空文档完成正式 Track/Clip 新增、字段和曲线编辑、撤销、保存重开。
- 统一 GUI 区域、帧映射和鼠标命中，清理漂浮按钮、重复信息及无关菜单。
- 使用正式 identity、contract、字段绑定和同一 Mutation/Undo，保持编辑状态。
- 观察运行只读，作者内容继续可编辑，版本采用交给已有 Graph Shell 合同。

**Non-Goals:**

- 不创建另一套持久化 Timeline、mutation service 或 UI Toolkit 时间轴。
- 不改模拟频率、技能执行、Pose Graph、相机算法和 Scene Play 执行语义。
- 不新增测试代码，不自动 Build 角色产物、启动 Play 或迁移资产。
- 不把旧 worktree 的代码状态当成主线已交付。

## Decisions

### 1. 作者可见布局

```text
Timeline：资产名                                      共享/私有
[＋轨道] [上一帧] [下一帧] [当前帧：24] [显示全部] [属性]
轨道搜索          │ 缩放范围条
                  │ 0       10       20       30       40
轨道名称          │ [Clip────────────────────] │ Inspector
  曲线通道        │ 曲线与关键帧              │ 选中对象属性
────────────────────────────────────────────────────────────
```

- 文档名显示一次，ownership 为短标记，路径放 tooltip。删除独立 Source、Timeline Ownership 大行和“作者预览”长说明。
- 文档行、工具行以各 24–28 个编辑器逻辑像素为基准，适应字体/控件高度；不为隐藏的原生工具栏保留空白。
- 搜索区与右侧标尺区共用顶边。缩放条、帧号分区，游标命中不覆盖缩放条。
- 左轨道与右 Clip/Curve 使用同一行高度表和垂直滚动值；展开曲线时两侧同步。
- Inspector 在同一 Timeline 窗口右侧，可拖拽调整宽度、可收起；无选择显示短提示，不为缺失领域工具占空行。
- 支持最小 600×360 逻辑像素；窗口宽度不足约 1080 时优先收起右侧 Inspector，Inspector 展开时保持可读的最小宽度。长标题省略、次要工具进入“更多”，主要按钮和帧输入不互相挤压。
- 背景、分隔线、裁剪、控件和命中范围从同一布局结果读取。不能只增加 TOP_MARGIN；当前背景还使用旧坐标的路径必须一并迁移。
- 直接修改 Slate Editor Surface 和现有控件，外层 UI Toolkit 只负责承载、文档绑定及既有属性工具，不重画时间轴。

取舍：新增轨道使用显式按钮，新增 Clip 放对应轨道右键菜单，作者能确定添加位置；空轨道提示“右键添加 Clip”。右侧 Inspector 占用横向空间，但保持时间轴的垂直轨道区域完整；收起后恢复全部时间轴宽度。

### 2. 唯一帧时间

正式内容已有 StartFrame、EndFrame、ClipInFrame、Section.Frame；当前 TimelineUtility.FrameRate 为 60。Surface 从正式 Session/FrameRate 合同取得帧率，不自行保存另一份 60，也不依赖 Slate 全局 Prefs 的 FPS/秒吸附。

- 标尺、当前帧、Clip 开始/结束/长度、Section 位置和关键帧横轴显示 Timeline 帧；秒只作补充 tooltip。
- 上一帧/下一帧严格加减 1。跳到相邻关键帧若保留必须另名，不能冒充单步。
- 统一“像素 ↔ Timeline 帧 ↔ Slate 秒”。移动/裁剪/新增/游标/key 在草稿阶段吸附，释放后不跳到另一个取整位置。
- 标尺缩小时采用整数主刻度间隔，放大到逐帧，避免重叠标签；修改 Slate 全局偏好不改变 BTSMTL 的显示和保存。
- 曲线时间转换服从正式 descriptor 的 domain；Motion 的 CurveEndFrame 等范围不能统一误用 Clip 总长度。局部秒与 normalized time 互换时同步换算切线。
- 仅编辑的 key 时间按帧吸附，不能打开或重建一次就整体量化原曲线；完整保留 tangent、weight、WeightedMode、wrap mode。
- 内容终点取真实 MaxFrame/Section，不用“至少 1 秒”伪造长度，也不默认多加 1 秒视野。空文档可有编辑视窗，但视窗不成为正式内容。
- “显示全部”取内容范围并留少量像素边距；边距不写数据。终点线和编辑游标颜色/说明不同；没有正式长度字段的终点不能提供假拖动保存。

作者帧与 Runtime Logic Tick 不自动等同。沿用当前作者帧可保持素材时长和资产含义；按模拟 Tick 作者有利于逐 Tick 窗口定位，但会涉及频率、倍率和旧内容迁移。本次只落实既有作者帧，真实 Tick 作为来源明确的运行观察字段。若用户要求合并两者，必须先确定业务变化，不由 UI 偷改。

### 3. 在 Slate 原源码里更换数据绑定

用户最新明确要求：Slate 已有 UI 本身可用，直接修改它的源码，绑定该换的换、无关的该删的删。实施不是另做一个看起来像 Slate 的编辑器，而是沿 Slate 原有绘制/交互函数修改数据入口、出口和必要参数。

#### 3.1 保留哪些现成代码

保留原时间尺、Group/Track 列表布局、Clip 窗口、GUI.Window 命中、鼠标选择/框选、拖动/裁剪、缩放/平移、CurveEditor、DopeSheetEditor、关键帧/切线编辑及样式资源。除明确的布局缺陷或数据依赖外，不改其算法和操作行为。

禁止重新写一套 DrawTrack/DrawClip/DrawCurve、手势状态机或行渲染器去替代这些代码。不是把 Slate 的皮肤或图片拿过来，也不是保留旧 UI 再另开纯内存 UI 路径。

#### 3.2 原函数逐处改什么

| 原代码区域 | 保留 | 绑定替换/删除 |
|---|---|---|
| CutsceneEditor 的窗口/嵌入绘制入口 | 原绘制调度和单窗口承载 | 输入改接当前正式 Timeline/adapter；不为了满足输入创建临时 Cutscene 和组件树 |
| ShowTimeInfo、SnapTime、DoZoomAndPan | 标尺、缩放条、刻度和手势逻辑 | 正式 FrameRate、内容帧范围、现有窗口视野；编辑游标不写 runtime clock |
| ShowListGroups/ShowListTracks、ShowTimeLines | 列表、行高、背景、滚动和原 Clip 绘制 | 从正式 Track/Clip 集合或现有薄适配读取，移除 Transform/groupsRoot/GetComponentsInChildren 的数据发现 |
| ActionClipWindow/既有 Clip wrapper | 原选择、拖动、裁剪、混合手柄等行为 | 正式元素 ID、Start/End/Ease/ClipIn 和编辑能力，释放时写原 Timeline Mutation |
| 原新增/删除/复制/排序菜单 | 菜单和交互入口 | 调正式 AddTrack/AddClip/AddSection、typed 配置/引用规则；删除原生任意组件创建命令 |
| CurveEditor/DopeSheetEditor/参数列表 | 原曲线绘制、关键帧操作、切线、缩放和工具 | 曲线来自正式 descriptor/AnimationCurve；参数/回调改接现有曲线草稿与提交，不依赖 AnimatedParameter 的场景对象反射或运行采样 |
| CutsceneUtility 的选择/时间/刷新调用点 | 原选中反馈和编辑后的刷新行为 | 接现有 Timeline selection/编辑游标/owner 通知；资源导航才操作 Unity Selection，不选临时 GameObject |
| Undo/SetDirty/Validate | 用户原撤销/重做和合法性行为 | 接现有 Session/owner Undo、Timeline 业务规则；删除组件层级 Undo 和 Cutscene.Validate |
| 初始化/释放/SceneGUI/播放 | 正常 GUI 生命周期 | 删临时 GameObject/Cutscene/组件创建、Actor/Director/场景播放回调及专用销毁，保留原资源/事件的必要释放 |

函数签名、输入接口和绑定代码可以按上述目的修改。需要适配时，只用能提供这些原函数所需字段/动作的薄 adapter；不新造通用 Editor Model、第二套业务对象、序列化格式或绘制框架。不能为了“函数名不变”保留无用组件，也不能以改参数为名重写函数主体的交互算法。

#### 3.3 数据如何来回传

```text
TimelineData 正式轨道/Clip/Section/曲线
  -> 现有 adapter 供应原 Slate UI 所需字段
  -> Slate 原绘制与交互函数
  -> 当前编辑草稿/明确创建命令
  -> 原 Timeline typed API / Session / 正式 owner Undo
  -> 原选择和视图恢复，刷新 Slate UI
```

TimelineData 仍是唯一持久化 Timeline 业务对象，完整曲线、业务 identity 和资源引用保持。原 UI 修改期间的草稿可以在内存中存在，但不作为新 Editor Model 工程，不进入代码导出、Compiler 或 runtime。Curve 引用不可被 UI 在未提交时直接改正式数据。

现有帧换算、右侧 Inspector、ID选择、曲线 descriptor、typed 配置和 Undo 提交已经正确的部分继续用。r2 C# 输出仍从正式 TimelineData 读取，不从 Slate 绘制状态导出。

#### 3.4 无关绑定直接清除

Timeline 编辑不需要 Actor、Director、ScenePlay 或 Slate runtime 执行。删除 __BTSMTL_SlateTimelineProjection__ 宿主、临时 Cutscene/Group/Track/ActionClip 组件承载及仅为它们存在的创建/扫描/销毁路径；依赖它们的现成 UI 函数按上表改接正式数据，而不是另建 UI。

实际角色预览由原 Graph Shell/Session 运行，Timeline 只接外部观察和导航。已有正式 Camera Track、角色、资源或用户真实 Cutscene 不属于临时代理删除范围。若插件自身仍有真实 Cutscene 窗口消费者，复用相同原 UI 函数并将其绑定隔离在插件入口；BTSMTL 不走该绑定，不保留失败时回到组件代理的 fallback。

某个原函数耦合比预计深时，记录该函数的具体绑定并在原源码内处理；不能自动扩成替代编辑器工程，不能再声称整体重写只是轻量适配。

#### 3.5 回退与继续

实现窗口已提交 0aa52f209《回退Timeline纯内存自制UI链》，回退 7bc392cd3 及后续自制 Surface/Curve/DopeSheet/缩放/手势改造，保留同期无关相机等业务提交。正式入口恢复为 TimelineEditorWindow → BtsmtlSlateTimelineProjection → Slate CutsceneEditorSurface。实现窗口报告 BTSMTL.Timeline.Tree.Editor 编译 0 errors/0 warnings；本规划未重复执行编译。原 projection 仍有临时组件树，这是下一步绑定适配要处理的问题，不是最终方案。

原第11节的“新建 SlateTimelineEditorSurface/纯内存 Editor Model、替换整套交互和曲线工具”已撤销。新的第11节只记录原源码的数据绑定替换和无关代码清理，全部未勾选，不沿用被回退实现的完成状态。

### 4. 新增与字段编辑

打开请求显式携带当前 owner 的正式 contract composition。菜单消费现有 TimelineContractCatalog、TimelineAuthoringTypeCatalog 和字段 binding，不在 UI 复制类型名单。

```text
＋轨道 / 轨道空白处右键“在第 N 帧添加 Clip”
  -> 合法候选与 typed 创建表单
  -> 完整输入校验
  -> Session 的同一事务内创建正式对象及 identity
  -> 从正式对象生成 Slate 显示并选中新对象
```

| 类型 | 必须明确的输入 |
|---|---|
| Track | contract、名称及必填字段；Animation Channel/Slot 使用正式绑定 |
| Animation Clip | 已有原生 AnimationClip、插入帧；长度按正式资源规则计算 |
| TreeClip | 现有正式 inline/shared ownership、Graph/Tree 来源和阶段；inline 创建只走已有正式 API |
| MotionCurve | 合法帧范围、CurveEndFrame、空间/通道等必填字段；初值遵循正式类型规则 |
| MotionWarp | 当前 Timeline 中合法源 Motion Clip、窗口及 required target binding |
| Camera/Cue/Scene 参数 | 该类型现有资源、参数或目标声明，不用 Slate Actor/Camera 替代 |

主动创建无 Clip 的合法空 Track 是正常操作。“不得留下空对象”只针对取消/失败残留。取消 picker、输入非法、owner 过期时不写入、不产生 Undo；跨对象校验先验证完整草稿，提交失败在同一事务回滚。

属性区编辑正式字段，资源为精确引用，时间字段按帧，TreeClip 保留正式下钻。曲线仅来自已注册 Timeline-local channel，不纳入素材骨骼/Foot Analysis。

删除、复制、排序和跨轨道移动都必须走正式命令和 contract：复制生成新 ID，排序保留 ID；删除前检查 MotionWarp/Section 等引用，按既有 validator 接受或拒绝。未接通的原生命令不能作为可用入口暴露，已支持的类型缺创建绑定时补正式绑定，不靠隐藏类型宣称完整。

### 5. 曲线与刷新状态

- 复用 Slate Curve/DopeSheet；通道名称、单位、颜色来自正式 descriptor，Track 名不再拼 “[Curves: N]”，使用明确曲线展开入口。
- 展开仅显示当前 Clip 的有效通道；无曲线 Clip 不出现大片“No Clip Selected”参数区域。
- 密集 key 根据缩放优化显示，不减少正式 key；选中 key 支持时间、值和切线精确编辑。
- 按稳定 ID 保存选择、选中曲线、展开状态、编辑帧、横向范围、纵向滚动、Inspector 宽度。提交、Undo/Redo、外部刷新恢复仍有效的状态。
- 删除所选对象时清空对应选择，不自动改选首个 Clip；另一个文档才采用其初始视图。
- 重绘不得抢走文本输入和合法草稿；外部变更使草稿过期时明确提示。
- 截图中的 GUI、序列化重复字段和 proxy 生命周期错误需要完整堆栈定位，不以隐藏 Console 或泛化 null 检查代替修复。

### 6. 游标和角色预览

已确认：Timeline 保留编辑游标；Scene Play、暂停、Build、Skill request、LiveDebug/Capture/History/Restore/Replay 由 SkillGraph/Graph Shell 和正式 owner 控制。运行事实只读，作者内容可编辑；编辑后是否生效由现有 Build/adoption 合同报告，不自动改当前运行。

本次工具栏提供帧定位、逐帧和“预览”导航。精确来源存在时返回对应 SkillGraph/共享预览区；独立内容使用其正式调用方上下文，无绑定给出缺项说明，不猜测 Actor。导航不启动或重建 Session。联合计划已纳入该导航与真实观察 binding，不能只提供一条不可操作的说明文字。

Timeline 本地自动播放游标，以及 Timeline 中直接控制 Scene Play 的 Play 快捷按钮，都尚未确认，不作为已批准功能。后续若要求 Timeline 内 Play，应接已有正式命令并明确目标，同时修订控制入口约定，不能用 Slate Play/Sample 代替。

清理嵌入路径的 Slate Play/Sample/ReSample/Stop、AutoKey 和场景副作用，覆盖按钮、快捷键、EditorUpdate、初始化、释放、保存和 delayCall。静态曲线值显示直接读取正式曲线，不调用 Cutscene 采样。独立 Slate 插件窗口保留其自身行为。

### 7. 保留、删除和实施边界

保留真实 Slate Clip/Track/Curve UI、正式 identity/数据、Mutation/Undo、Graph/AnimationClip 导航、已有场景预览。

撤销替代编辑器改造，清理因此新增且被用户要求回退的专用代码由实现窗口按实际 diff 负责；现有 Slate 功能、正确的 typed 配置/帧/右侧 Inspector 与正式 Camera Track 保留。无用菜单和真实缺陷继续局部处理，不列整套 Surface/曲线输入迁移。

只在主线执行。预览 change 的 2026-09-11 约定要求旧 worktree 停写，后者仅供历史追溯和未集成内容参考，不自动双写。相同文件存在其它未提交改动时报告冲突，不覆盖。


### 8. r2 强类型配置与共享文件分工

| 文件/能力 | 唯一负责方 | 本任务边界 |
|---|---|---|
| TimelineAuthoringClipBinding.cs | C# authoring | 等待其正式强类型读取/配置接口；该任务删除 JSON，保留原 Configure/Set 和字段规则 |
| 现有 BTSMTL Slate 数据适配文件 | Timeline | 本任务维护正式配置和 UI 接线，不再要求纯内存模型或强制改名；公共任务不并行修改 |
| 两个作者 MCP、公共输出/生成 | C# authoring | 不在 Timeline/Slate 新建工具、输出器、源码 Undo 或导出界面 |
| TimelineData.AddTrack/AddClip/AddSection、Curve/引用规则 | 原 Timeline 模块 | 人工编辑和代码生成继续使用同一正式业务 API |
| Scene Play、Build/adoption | 原预览/运行 owner | 保留现有 Session、owner、时钟、已正确 UI 和 Camera Track |

r2 曾有新增片段 JSON 中转；最新 tasks 已记录强类型接线完成，应以当前实现保留该成果。输入仍为“现有创建输入与当前 Clip 值 → 同一正式强类型配置入口 → 原 Configure/Set/领域规则 → 既有 Session/Undo”。删掉 UI JSON 中转，不用另一份 UI DTO 或按 kind 复制业务规则顶替。

只改变参数传递方式，保留未覆盖字段、既有默认值、资源、CurveEndFrame、合法范围、取消/错误反馈、选择和刷新。字段/签名以公共任务已交付的正式文档和代码为准；未交付只等待该接线项，不新增兼容入口，也不重做无关 UI。

公共完整导出直接读取 TimelineData：
- Timeline/Track/Clip/Section 的 identity、字段、帧、业务顺序和 Loop/Scale 等正式配置。
- 外部 binding 声明及使用、Channel/Slot、TreeClip 的来源/阶段/嵌套图和 owner 关系。
- 完整原始曲线的 key、tangent、weight、WeightedMode、wrap 与 time domain，不能读取 Slate 秒域草稿或显示降采样结果。
- 正式 layout owner 的布局。窗口局部选择/滚动/运行标记不成为生成内容，不另存布局镜像。

生成范围内对象使用本次创建引用；范围外共享图、原始 AnimationClip、Rig/Profile 等作为精确外部输入，不因可达而复制或删除。仅替换明确范围内物理对象，保持业务 identity/引用关系，通过正式 API 恢复明确的 Definition/Profile/Timeline 根挂接并保存；不能用旧生成子资产 GUID 寻找内部对象，不能扫描全项目猜消费者。

r2 仅 export_code 显式写指定源码、generate_assets 显式执行当前已编译入口并保存生成范围；导出从当前资产完整输出，不读取旧源码增量合并。未导出人工修改只在资产中，重新生成不自动合并；源码编译不触发生成，两作者操作不自动 Character Build/Play。代码未编译或版本不匹配由公共入口拒绝，不执行旧程序集。

现有正式局部校验和编辑 Undo 保留；旧 Agent Document/五工具/同步协议退役不新增中央 Validator、新整包事务、rebase 或源码状态机。生成后沿已有 owner/identity 失效和刷新规则处理，不新增源码同步/重载恢复系统。

### 9. r2 行为条件

以下是交付行为，不添加测试或验证 tasks：
- 人工拖动、字段/曲线修改、保存和 Undo/Redo 不写源码；编译源码不生成资产。
- UI 和 C# 创建使用同一 typed 配置规则，projection 无 BuildClipProperties/Export/JObject/JSON Apply 中转。
- 完整导出/重建保持帧、顺序、字段、曲线 tangent/weight/WeightedMode/wrap、业务身份和资源关系，生成根已挂回明确 owner 并保存。
- 只替换声明范围，范围外资源不变；未导出修改不自动合并。
- 已正确 Slate 编辑、布局、选择、Undo、Session、时钟、预览采用和 Camera 轨道继续工作。

## Risks / Trade-offs

- C# 是显式生成范围的可重建来源，人工编辑保留在资产；需要把人工修改带入下次生成时显式完整 export_code。代价是未导出修改不会自动合并，收益是没有源码同步/解析/冲突状态机。

- 修改 Slate 源码需要维护插件升级差异，换来复用真实 UI；补丁集中于 Editor Surface，领域规则留在 BTSMTL。
- 临时对象是内存草稿，正式保存只有 TimelineData；新增先建正式对象，编辑只提交一次事务。
- 右侧 Inspector 占用横向空间，可收起以释放时间轴宽度；属性编辑不再放在底部制造第二块时间区。
- 作者帧沿用已有语义，逐 Tick 运行定位消费真实 trace；本次不做时钟迁移。

## 文档对账

本次冲突纠正：上一版本 change 要求全面替换 Slate 的模型、交互与曲线输入，违背用户直接使用现成 UI 的要求，现已撤销。current 独立作者能力、typed Session、合法字段和预览边界保持；current 旧 PreviewSession 描述仍由原预览 change 处理，本轮不改其它任务文档。数据绑定替换和无关组件清理按第3节在原 Slate 源码内完成，不能改成另一套 UI。

2026-09-13 r2 补充：current Agent 专属规范中的目录包、五工具和中央 Agent Validator 与 r2 冲突，删除/替换 delta 由 C# authoring 任务拥有。本任务只清理自己规划里的协议依赖；正式 Timeline 校验、编辑 Session 和预览 Session 不属于旧 Agent 协议。原“资产永久为唯一来源”被明确生成范围的 C# 重建规则替代，TimelineData 作为正式对象、Slate 作为临时投影不变。implementation.md 中旧 Document 对账属于历史实现记录，不作为新接线前提。

| 来源 | 处理 |
|---|---|
| current btsmtl-timeline-editor-preview | 仍描述 TimelinePreviewSession、互斥 Preview/LiveDebug 和只读；由 rebuild 场景预览 change 的 REMOVED/MODIFIED delta 替换。尚未归档，不把目标直接写成 current 已交付。 |
| restyle 本 change | 负责 Slate GUI、帧、新增、正式编辑及桥接，旧完成勾选按代码证据纠正。 |
| rebuild 场景预览 change | 同步改正“观察后结构只读”为“观察只读、作者可编辑”；场景控制和采用实现仍归该 change。 |
| current timeline-animation-authoring-surface | 保留独立作者能力、typed context、按需工具和不占空行规则。 |
| 当前代码 | 0aa52f209 已恢复原 Slate 入口；旧完成记录不证明新的绑定替换已完成，组件树仍待处理。 |

## Migration Plan

1. 实现窗口按用户授权回退替代编辑器改造，恢复真实 Slate 原有绘制、交互和曲线功能；本任务只同步文档，不操作代码或资产。
2. 在恢复后的实际基线上继续原有正式数据、typed 新增/字段/曲线回写、帧显示和布局修复；已有正确功能不重做。
3. 按第3节逐处替换原 Slate 函数的数据读写，删除临时对象树和无关运行绑定；保留原绘制/交互/曲线算法，不建立替代框架。
4. 继续原预览联动和 r2 作者协议接线。运行归原正式 owner，不增加第二播放器、源码同步或编辑器实现。
5. 实施记录区分“回退已完成”“原功能恢复”“剩余接线”，不以编译或文档更新代替功能状态，不新增验证任务。

## 完成标准

以下是交付行为标准，人工操作不写入 tasks.md：

- 使用 Slate 原有时间尺、Clip/Track、选择、拖动、缩放和 Curve/DopeSheet 功能，无另写的替代编辑器；不得仅以相似外观称为 Slate。
- 原 Slate UI 函数已改接正式 Timeline 数据，打开/编辑不创建代用组件树；没有 Actor/Director/ScenePlay 也可编辑。删除组件通过修改绑定达成，不以新 Surface/渲染器替代原功能。


- 从空文档新增合法空 Track、有资源 Clip，保存重开后 ID/资源/帧不变；取消选择不留对象或 Undo。
- Clip 从第 12 帧移动到 13，草稿、属性、正式保存和重开均为 13；一次 Undo 返回 12。
- 曲线新增/删除/移动/数值/切线完整往返，未编辑 key/tangent/weight/wrap 不变。
- 连续编辑、撤销、刷新保留有效选择、展开、滚动和缩放。
- 600×360、截图近似尺寸和宽窗口及不同编辑器缩放下没有遮挡，命中与显示一致。
- Scene Play 观察期间可编辑，游标不运行角色；运行时间来自真实绑定，采用状态由 Graph Shell 报告。
- 没有新增 GUI/序列化/生命周期错误；完整堆栈和处理结果可追溯。编译和文档结构校验不能替代上述行为证明。
