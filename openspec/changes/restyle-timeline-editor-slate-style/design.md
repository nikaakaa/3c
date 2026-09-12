## Context

2026-09-13 公共作者基线采用 [remove-agent-authoring-use-native-csharp/design.md r2](../remove-agent-authoring-use-native-csharp/design.md)。TimelineData 是正式业务对象，Slate 是现有编辑投影；显式 C# 导出/生成替代旧 Agent Document/五工具。旧日期的 UI 问题记录仅作历史，已完成交互、布局、选择、Undo、owner、时钟和 Camera 轨道按最新实现保留。本次仅规划 UI JSON 接入退役及公共生成合同的领域边界，不重做已完成 UI 或预览。

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
轨道名称          │ [Clip────────────────────]
  曲线通道        │ 曲线与关键帧
──────────────────────────────────────────────────────
可收起属性区：选中轨道 / Clip / 曲线关键帧的正式属性
```

- 文档名显示一次，ownership 为短标记，路径放 tooltip。删除独立 Source、Timeline Ownership 大行和“作者预览”长说明。
- 文档行、工具行以各 24–28 个编辑器逻辑像素为基准，适应字体/控件高度；不为隐藏的原生工具栏保留空白。
- 搜索区与右侧标尺区共用顶边。缩放条、帧号分区，游标命中不覆盖缩放条。
- 左轨道与右 Clip/Curve 使用同一行高度表和垂直滚动值；展开曲线时两侧同步。
- 属性区在同窗口底部，可调高度、可收起；无选择显示短提示，不为缺失领域工具占空行。
- 支持最小 600×360 逻辑像素；空间不足优先收起属性区。长标题省略、次要工具进入“更多”，主要按钮和帧输入不互相挤压。
- 背景、分隔线、裁剪、控件和命中范围从同一布局结果读取。不能只增加 TOP_MARGIN；当前背景还使用旧坐标的路径必须一并迁移。
- 直接修改 Slate Editor Surface 和现有控件，外层 UI Toolkit 只负责承载、文档绑定及既有属性工具，不重画时间轴。

取舍：新增轨道使用显式按钮，新增 Clip 放对应轨道右键菜单，作者能确定添加位置；空轨道提示“右键添加 Clip”。底部属性区减少横向时间轴占用，代价是展开时减少可见轨道数量。

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

### 3. 模块和桥接

现有打开链：

```text
TimelineEditorWindow 的正式 owner/path
  -> BtsmtlSlateTimelineProjection
  -> 临时 Slate Group/Track/ActionClip/AnimatedParameter
  -> CutsceneEditorSurface
```

临时 GameObject/组件仅满足 Slate UI 的接口，使用 HideAndDontSave；不保存、不进入 C# 导出输入或 Compiler、不绑定角色。SourceAuthoringId 映射到正式对象，不能用显示名、组件 instance id 或数组下标替代。

在现有 Editor 模块内明确五项责任：窗口布局/生命周期、帧映射、创建及字段命令、identity/快照转换、视图状态恢复。使用现有 TimelineEditorSessionContext 调度唯一 Mutation/Undo，不另建 service。各模块输入分别为窗口尺寸、正式帧率、typed 用户输入、正式数据/草稿、稳定 ID 视图状态；输出分别为区域、帧坐标、正式修改、显示投影、恢复后的视图。

已有内容修改：

```text
开始手势记录 source revision 和草稿基线
  -> Slate 临时字段变化
  -> 提交时按正式 ID 求差异并校验
  -> Session.Apply / Timeline.ApplyModify / 正式 owner Undo
  -> 从正式数据更新显示并恢复视图
```

有效手势一个正式 Undo；选择、游标、缩放、搜索、运行观察不产生 authoring Undo，不因每次 MouseUp 无条件重建。取消/关闭/owner 过期丢弃草稿；Unsupported 必须给出原因，不能静默吞操作。

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
- 按稳定 ID 保存选择、选中曲线、展开状态、编辑帧、横向范围、纵向滚动、属性高度。提交、Undo/Redo、外部刷新恢复仍有效的状态。
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

替换临时 ShowEmbeddedPlaybackControls、重复 Source/Ownership、“作者预览”标签、无关 Actor/Director/Render 菜单、未映射创建/复制命令、Slate 私有播放、假长度编辑和无变化也重建的路径。不删除项目自己的 Camera Track。

只在主线执行。预览 change 的 2026-09-11 约定要求旧 worktree 停写，后者仅供历史追溯和未集成内容参考，不自动双写。相同文件存在其它未提交改动时报告冲突，不覆盖。


### 8. r2 强类型配置与共享文件分工

| 文件/能力 | 唯一负责方 | 本任务边界 |
|---|---|---|
| TimelineAuthoringClipBinding.cs | C# authoring | 等待其正式强类型读取/配置接口；该任务删除 JSON，保留原 Configure/Set 和字段规则 |
| BtsmtlSlateTimelineProjection.cs | Timeline | 本任务独占 UI 接入，公共任务不并行修改此文件 |
| 两个作者 MCP、公共输出/生成 | C# authoring | 不在 Timeline/Slate 新建工具、输出器、源码 Undo 或导出界面 |
| TimelineData.AddTrack/AddClip/AddSection、Curve/引用规则 | 原 Timeline 模块 | 人工编辑和代码生成继续使用同一正式业务 API |
| Scene Play、Build/adoption | 原预览/运行 owner | 保留现有 Session、owner、时钟、已正确 UI 和 Camera Track |

当前新增片段调用 TimelineAuthoringClipBinding.Apply(...BuildClipProperties(...))，后者调用 Export(clip).Properties 得到 JObject 再覆盖表单字段。目标改为“现有创建输入与当前 Clip 值 → 同一正式强类型配置入口 → 原 Configure/Set/领域规则 → 既有 Session/Undo”。删掉 UI JSON 中转，不用另一份 UI DTO 或按 kind 复制业务规则顶替。

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
- 底部属性区减少横向占用，可收起以释放垂直空间。
- 作者帧沿用已有语义，逐 Tick 运行定位消费真实 trace；本次不做时钟迁移。

## 文档对账

2026-09-13 r2 补充：current Agent 专属规范中的目录包、五工具和中央 Agent Validator 与 r2 冲突，删除/替换 delta 由 C# authoring 任务拥有。本任务只清理自己规划里的协议依赖；正式 Timeline 校验、编辑 Session 和预览 Session 不属于旧 Agent 协议。原“资产永久为唯一来源”被明确生成范围的 C# 重建规则替代，TimelineData 作为正式对象、Slate 作为临时投影不变。implementation.md 中旧 Document 对账属于历史实现记录，不作为新接线前提。

| 来源 | 处理 |
|---|---|
| current btsmtl-timeline-editor-preview | 仍描述 TimelinePreviewSession、互斥 Preview/LiveDebug 和只读；由 rebuild 场景预览 change 的 REMOVED/MODIFIED delta 替换。尚未归档，不把目标直接写成 current 已交付。 |
| restyle 本 change | 负责 Slate GUI、帧、新增、正式编辑及桥接，旧完成勾选按代码证据纠正。 |
| rebuild 场景预览 change | 同步改正“观察后结构只读”为“观察只读、作者可编辑”；场景控制和采用实现仍归该 change。 |
| current timeline-animation-authoring-surface | 保留独立作者能力、typed context、按需工具和不占空行规则。 |
| 当前代码 | f1287b44c 接回 Slate Play/Sample；639ba8253 只增加垂直偏移；两者均未完成本设计，需要替换。 |

## Migration Plan

已完成的前序 UI 工作保持，不重复实施；r2 增量在正式强类型配置合同交付后只修改本任务拥有的 projection 调用段。公共代码输出/生成由 C# authoring 任务负责，下节列出需要表达的完整 Timeline 内容，不在本任务复制输出器。

1. 接通正式新增、字段编辑及取消/失败/Undo，替换无正式回写的菜单。
2. 统一帧几何、曲线 domain 转换、逐帧和视图状态恢复。
3. 整体整理 GUI，审计嵌入生命周期，移除 Slate 内核和无关控件。
4. 运行已有编译/validator，记录真实窗口证据与未完成项，分模块中文小步提交；不新增测试代码。

5. 与 preview-integration-plan.md 的 P1–P5 联合排期和交付：共享预览区、实际 SkillGraph 宿主接入、场景/目标、Timeline 调用观察、编辑后采用与历史交互一起核对。原预览任务按该计划映射执行，不另建重复任务/播放器。

## 完成标准

以下是交付行为标准，人工操作不写入 tasks.md：

- 从空文档新增合法空 Track、有资源 Clip，保存重开后 ID/资源/帧不变；取消选择不留对象或 Undo。
- Clip 从第 12 帧移动到 13，草稿、属性、正式保存和重开均为 13；一次 Undo 返回 12。
- 曲线新增/删除/移动/数值/切线完整往返，未编辑 key/tangent/weight/wrap 不变。
- 连续编辑、撤销、刷新保留有效选择、展开、滚动和缩放。
- 600×360、截图近似尺寸和宽窗口及不同编辑器缩放下没有遮挡，命中与显示一致。
- Scene Play 观察期间可编辑，游标不运行角色；运行时间来自真实绑定，采用状态由 Graph Shell 报告。
- 没有新增 GUI/序列化/生命周期错误；完整堆栈和处理结果可追溯。编译和文档结构校验不能替代上述行为证明。
