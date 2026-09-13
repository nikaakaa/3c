## Context

2026-09-13 无场景对象修订：用户明确要求打开 Timeline 不创建临时 Slate GameObject、Cutscene、Group、Track 或 ActionClip 组件树。此前用 HideAndDontSave 包装组件树是适配实现选择错误，不是 Timeline 业务依赖。本次替换该输入模型和关联 UI 调用链；保留现有右侧 Inspector、布局、帧编辑、选择与正式 Mutation/Undo 行为。下面第3节是唯一目标输入设计，旧组件投影条款全部退役，当前代码尚未完成该替换。

2026-09-13 公共作者基线采用 [remove-agent-authoring-use-native-csharp/design.md r2](../remove-agent-authoring-use-native-csharp/design.md)。TimelineData 是正式业务对象，Slate 是现有编辑投影；显式 C# 导出/生成替代旧 Agent Document/五工具。旧日期的 UI 问题记录仅作历史，已完成交互、布局、选择、Undo、owner、时钟和 Camera 轨道按最新实现保留。该轮只涉及作者协议；本轮进一步按用户授权移除组件投影，已正确 UI 行为和预览仍保留。

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

### 3. 纯内存 Slate Editor Model 与 adapter

#### 3.1 当前实现与错误原因

当前 BtsmtlSlateTimelineProjection.BuildProjection 创建 __BTSMTL_SlateTimelineProjection__ GameObject，AddComponent<Cutscene>，再从 groupsRoot 创建 Group/Track 子物体及 BtsmtlSlateActionClip 组件。CutsceneEditorSurface 还是 ScriptableObject，初始化和绘制直接使用 Cutscene.Validate、Transform、GetComponentsInChildren 和组件选择。隐藏对象并没有解除依赖，创建后再删除 Director 仍然属于错误链路。

曲线也存在直接耦合：CurveEditor.DrawCurves 与 DopeSheetEditor.DrawDopeSheet 接收 IAnimatableData/IKeyable；AnimatableParameterEditor 依赖 targetObject、animationData 和 root.currentTime；DopeSheet 把 keyable 转成 UnityEngine.Object 记录 Undo。必须把绘制所需数据与组件求值行为分离，不用纯 C# 假对象实现完整 IDirectable/IKeyable 来保留旧内核。

#### 3.2 正式对象、视图、草稿分别是什么

```text
TimelineData + 正式 serialized owner/path
  -> BTSMTL Editor adapter（只读业务内容，提交调用原 Session）
  -> Slate 纯内存视图 + 窗口局部状态 + 当前手势草稿
  -> Slate 时间尺/轨道/Clip/Curve/DopeSheet
  -> 结束手势按正式 ID 提交
  -> 既有 TimelineEditorSessionContext / Mutation / Undo
  -> 从正式 TimelineData 刷新视图
```

TimelineData 仍是唯一持久化 Timeline 业务模型，r2 显式生成 C# 的重建规则继续有效。Editor Model 不是第二份业务模型或可运行 Timeline：不复制各种 Clip 配置类、不序列化、不导出、不入 Compiler。它只提供绘制需要的读视图、稳定 identity 和当前交互草稿。

| 输入/状态 | 必需内容 | 写入责任 |
|---|---|---|
| 内容读视图 | Timeline ID/revision、FrameRate、真实帧范围、按正式顺序的 Track/Clip/Section；名称、颜色、编辑能力 | adapter 从正式数据读取，Surface 不直接写 |
| Track/Clip 行 | 稳定 ID、父 Track ID、帧范围、显示标记、合法交互能力；可选 UI 分组仅为行分组 | adapter 生成轻量普通 C# 行记录；无 Actor/Transform |
| 曲线读输入 | Clip ID、ChannelId、名称/单位/值域、完整曲线、正式 time-domain 映射 | 由原 descriptor 提供，不反射组件字段 |
| 手势草稿 | 本次变更的帧范围/排序/复制意图/完整曲线和原 revision | Surface 只改草稿，未编辑内容不额外复制 |
| 窗口状态 | 编辑游标、选择、展开、缩放、滚动、Inspector宽度 | 当前窗口独占，不进入 TimelineData |
| 可选运行标记 | 精确调用 ID、版本和已发布位置/状态 | 外部 adapter 被动注入；无数据时本地编辑完整可用 |

普通 C# 对象不继承 MonoBehaviour/ScriptableObject，也不通过 ScriptableObject 或隐藏场景伪装组件。可以引用 UnityEngine.AnimationCurve、GUIStyle、Texture 以及已有资源，但这些不是为 UI 新建的场景/角色对象。正式 owner 的 UnityEngine.Object 只由 BTSMTL Mutation/资源导航持有，不下放给 Surface 做 proxy Undo。

内容读视图按 revision 更新；AnimationCurve 是可变对象，UI 不得直接修改正式曲线引用。开始手势只复制要编辑的完整曲线，取消丢弃、提交交正式 owner；不在每次 OnGUI 全量克隆 Timeline 或发现对象。

#### 3.3 Surface API 与单向依赖

下列为设计接口职责，实施时在现有 Editor 模块内采用清楚命名并迁移全部调用者，不保留旧签名 fallback：

| 当前入口/依赖 | 目标 API/责任 |
|---|---|
| CutsceneEditorSurface : ScriptableObject | 改为普通可释放的 SlateTimelineEditorSurface；构造、Bind、DrawGUI、Refresh、Dispose 由窗口管理 |
| InitializeEmbedded(Cutscene, ...) | Bind(只读 Editor Model, 编辑命令 port, 窗口 UI host)；不接受 Cutscene/ScenePlay 对象 |
| 静态 Action<Cutscene> transaction events | 实例级 BeginEdit/CommitEdit/CancelEdit，传正式 ID、revision 和 UI 差异，最终调用既有 Session |
| ActionClipWrapper/Dictionary<ActionClip,...> | 按正式元素 ID 的 Clip 交互状态；GUI.Window 如继续复用，只用当次绘制整数 ID 映射，不保存组件 ID |
| CutsceneUtility.selectedObject/current 全局状态 | 窗口局部 selection port，使用元素 kind/ID、ChannelId、key选择；原 Unity Selection 只在显式资源导航使用 |
| cutscene.currentTime、length、viewTimeMin/Max | 窗口游标/视野和内容模型帧范围；真实运行/历史标记单独输入 |
| group.tracks/track.clips 和 Cutscene.Validate | 读有序行列表；正式 mutation 后按 revision 重读，禁止 Transform 遍历和组件 Validate |
| Curve/DopeSheet 的 IKeyable/AnimatedParameter 输入 | 曲线编辑输入、帧映射和草稿/提交 port；保留原绘制、关键帧/切线算法 |
| BeginWindows/EndWindows、Repaint、通知 | 注入 UI host delegate，不传完整 EditorWindow/角色上下文；只属于 GUI |
| Undo.RecordObject(proxy)、SetDirty(proxy) | 从 UI 删除；唯一正式 owner Undo 仍在原 Timeline Session |

Slate 共享 UI 代码只依赖 Unity 编辑器绘制和上述纯 Editor 合同，不引用 BTSMTL 业务、ScenePlay、Cutscene/Actor/Director runtime。UI 合同和 adapter 按程序集单向依赖放置；必要的 Editor 程序集拆分只为隔离这一边界，不新增 runtime ABI。

BtsmtlSlateTimelineProjection.cs 当前由本任务独占维护；迁移后改为准确命名的 BtsmtlSlateTimelineEditorAdapter，移除组件 projection 类与旧打开签名，并更新调用者/meta/程序集引用。公共 typed binding 文件继续由 C# authoring 任务负责；本次不重复修改已接好的强类型业务入口。

若工程仍实际使用 Slate 自身 Cutscene 编辑窗口，它只能由插件自身边界 adapter 读取其已经存在的真实 Cutscene，调用同一个纯内存 Surface；这不是 BTSMTL 的运行路径，不允许创建代用 Cutscene，也不保留第二套 UI。Cutscene 的 Play/SceneGUI 等由插件自身窗口隔离，不进入共享 Surface。无消费者的旧 Editor 封装直接删除。禁止保留“纯内存失败转组件树”或开关双轨。

#### 3.4 选择、手势和正式提交

保留现有 layout/frame/command 行为，替换其对象来源。选择、框选、拖动、裁剪、复制、排序、跨轨道、Section、曲线快捷键全部通过 UI ID/port；合法性仍由已有 Timeline contract 决定。

一次手势开始记录 source revision；PointerMove 只更新本次草稿；提交通过原 Session 产生一次正式 Undo，成功后恢复稳定 ID 选择/视图。取消、关闭、失效 revision 丢弃草稿；按已存在 PointerUp/CaptureOut 规则完成或取消，不为迁移偷偷改交互语义。没有正式数据变化不记录 Undo，不重建内容。

Undo/Redo 监听由 adapter 单点管理，重读正式数据并使对应 UI 缓存失效，不给每个 curve renderer 注册匿名永久回调。属性修改仍在现有右侧 Inspector 调原 typed API，同一 selection 定位正式对象。

#### 3.5 曲线与生命周期

CurveEditor/DopeSheetEditor 保留 Slate 真实绘制、关键帧选择/移动、切线和缩放算法，改为直接消费完整 AnimationCurve 草稿。AnimatedParameter 的组件值反射、自动录制、生命周期采样和 root.currentTime 写入从该路径删除；参数列表取正式 descriptor，当前值以曲线 Evaluate 读取，不执行 Cutscene。

Curve key/time/tangent/weight/WeightedMode/wrap、CurveEndFrame domain 和帧吸附保持现有规则。草稿提交使用既有 TimelineCurveAuthoring/typed 配置，不把 normalized 曲线换成第二份持久化秒域曲线。

窗口 Bind 只分配 managed 内容视图/状态，重绑/关闭 Dispose 释放事件订阅、GUI capture、curve/DopeSheet 缓存和命令引用；不创建或销毁 GameObject、Component、Cutscene、Scene/PhysicsScene。删除 ClearEmbedded/ClearCutscene 中仅为临时对象存在的选择清理和 DestroyImmediate 路径；不删除用户真实资源。

曲线 renderer 的静态缓存改为实例拥有或可明确移除的缓存，键不持有旧组件/窗口。Domain Reload 后从已有正式 owner/path 重建内存视图，不序列化 Editor Model、不建立新的恢复服务。

Timeline 能在没有任何 ScenePlay/Context/Actor/Director 的情况下打开和编辑。预览联动是可选外部观察/导航输入；缺失它不阻塞创建、曲线、Undo 或保存。

#### 3.6 剩余实现决策

纯内存输入、无组件树、唯一正式数据与 Undo、右侧 Inspector 和 Slate UI 复用均已确定，不再作为待选路线。实施需按真实引用确定原生 Cutscene 窗口的有效消费者、可直接保留的曲线绘制辅助和 Editor 合同程序集落点。这些是调用图/接口落点，不允许恢复 runtime 模型或另建 UI。同段文件有其它任务修改时报告具体冲突，不整体覆盖。

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

删除组件 projection host、BtsmtlSlateGroup/Track/ActionClip、Cutscene 创建/Validate、Transform/组件扫描和相应 Undo/Selection/销毁路径；迁移必要的绘制代码为第3节纯内存输入。此前已清理的播放控件和无关菜单保持删除，不重做已正确右侧 Inspector/布局，不删除项目自己的 Camera Track。

只在主线执行。预览 change 的 2026-09-11 约定要求旧 worktree 停写，后者仅供历史追溯和未集成内容参考，不自动双写。相同文件存在其它未提交改动时报告冲突，不覆盖。


### 8. r2 强类型配置与共享文件分工

| 文件/能力 | 唯一负责方 | 本任务边界 |
|---|---|---|
| TimelineAuthoringClipBinding.cs | C# authoring | 等待其正式强类型读取/配置接口；该任务删除 JSON，保留原 Configure/Set 和字段规则 |
| BtsmtlSlateTimelineProjection.cs → BtsmtlSlateTimelineEditorAdapter | Timeline | 本任务独占纯内存迁移及调用者，公共任务不并行修改 |
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

无场景对象修订的冲突与迁移：本 change 原设计/spec 明确允许 HideAndDontSave 组件树、要求通过 Cutscene.Validate/AnimatedParameter 发现 Clip，这些是已否决的实现方案，本次全部替换。current timeline-animation-authoring-surface 要求独立作者能力、typed Session 和可选运行输入，与纯内存模型一致；current timeline-editor-preview 的旧 PreviewSession/target/runtime 依赖仍由场景预览 delta 退役，不能因旧 current 条款恢复组件树。本轮仅更新本任务规划，不安装未实现 delta 或修改其它 owner 的 current specs。

2026-09-13 r2 补充：current Agent 专属规范中的目录包、五工具和中央 Agent Validator 与 r2 冲突，删除/替换 delta 由 C# authoring 任务拥有。本任务只清理自己规划里的协议依赖；正式 Timeline 校验、编辑 Session 和预览 Session 不属于旧 Agent 协议。原“资产永久为唯一来源”被明确生成范围的 C# 重建规则替代，TimelineData 作为正式对象、Slate 作为临时投影不变。implementation.md 中旧 Document 对账属于历史实现记录，不作为新接线前提。

| 来源 | 处理 |
|---|---|
| current btsmtl-timeline-editor-preview | 仍描述 TimelinePreviewSession、互斥 Preview/LiveDebug 和只读；由 rebuild 场景预览 change 的 REMOVED/MODIFIED delta 替换。尚未归档，不把目标直接写成 current 已交付。 |
| restyle 本 change | 负责 Slate GUI、帧、新增、正式编辑及桥接，旧完成勾选按代码证据纠正。 |
| rebuild 场景预览 change | 同步改正“观察后结构只读”为“观察只读、作者可编辑”；场景控制和采用实现仍归该 change。 |
| current timeline-animation-authoring-surface | 保留独立作者能力、typed context、按需工具和不占空行规则。 |
| 当前代码 | 新 UI/帧/强类型输入已有完成记录；BuildProjection 仍创建组件树，Surface/Curve 输入仍耦合 Slate runtime，这部分纯内存迁移未实施。 |

## Migration Plan

1. 在 Slate Editor 内把已用的绘制输入、选择、帧与命令参数提炼为普通内存合同，确定组件/运行类型只留在各自边界；保留现有可用帧和 SurfaceLayout。
2. 同步迁移 Surface、Clip交互包装器、Section、CurveEditor/DopeSheet/参数列表到内存输入及实例命令/选择 port。不能只迁移 BuildProjection 留下曲线隐式依赖。
3. 在现有 BTSMTL adapter 中接 TimelineData、正式 typed 新增/字段/曲线 Mutation 和 owner Undo；右侧 Inspector 与新 ID selection 连通，复用现有视图恢复。
4. 单次切换所有正式打开入口到内存 Surface，删除旧组件 projection 和兼容签名/分支。中间提交可以尚未接通，但不发布可选双路径，不以 fallback 保持旧实现。
5. 清掉缓存/事件/初始化/关闭/Undo 生命周期中的组件及内核依赖；仍有真实 Slate 原生窗口消费者时只通过插件边界 adapter 复用同一 Surface。
6. 预览联动仅接可选运行标记和导航，保留原 Session/adoption，不让本地作者依赖场景。r2 C# 导出仍直接读取正式 TimelineData，不读取新 Editor Model。
7. 更新实施记录和实际删除范围，未实现条目保持未勾选。不新增测试或验证 tasks，行为验收由用户完成。

## 完成标准

以下是交付行为标准，人工操作不写入 tasks.md：

- 打开空/已有 Timeline、曲线展开、添加/编辑、Undo/Redo、重绑和关闭全程不新建任何临时 GameObject、Cutscene、Group/Track/ActionClip 组件或场景，Hierarchy/场景 dirty 不因 UI 打开变化。
- 无 ScenePlay/Actor/Director 时本地新增、曲线、选择、帧和保存完整可用；ScenePlay 不进入 Surface 必需输入。
- 主线不再存在旧 projection 创建链、类型/签名兼容入口、失败后回组件树分支；曲线与选择也不经 IKeyable runtime root 或组件 Undo。
- 仅替换 UI 输入不改变原 Track/Clip/Section/Curve/typed binding 的业务内容、identity、右侧 Inspector、正式 Undo 和 r2 代码输出。

- 从空文档新增合法空 Track、有资源 Clip，保存重开后 ID/资源/帧不变；取消选择不留对象或 Undo。
- Clip 从第 12 帧移动到 13，草稿、属性、正式保存和重开均为 13；一次 Undo 返回 12。
- 曲线新增/删除/移动/数值/切线完整往返，未编辑 key/tangent/weight/wrap 不变。
- 连续编辑、撤销、刷新保留有效选择、展开、滚动和缩放。
- 600×360、截图近似尺寸和宽窗口及不同编辑器缩放下没有遮挡，命中与显示一致。
- Scene Play 观察期间可编辑，游标不运行角色；运行时间来自真实绑定，采用状态由 Graph Shell 报告。
- 没有新增 GUI/序列化/生命周期错误；完整堆栈和处理结果可追溯。编译和文档结构校验不能替代上述行为证明。
