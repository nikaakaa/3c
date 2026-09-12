## MODIFIED Requirements

### Requirement: Timeline Editor必须直接使用Slate CutsceneEditor作为实际编辑表面

正式 Timeline 编辑入口 MUST使用从 Slate `CutsceneEditor` 抽出的 `CutsceneEditorSurface` 真实 IMGUI 编辑 UI，包括时间尺、Group/Track列表、Clip、选择、拖动、缩放和 Curve/DopeSheet。属性区 MUST使用正式 typed 字段绑定，不把 Slate proxy Inspector 的私有参数作为作者字段。Timeline Editor MUST不再用 UI Toolkit 重新实现一套 Slate 风格时间轴，也 MUST不把 Slate 图片或 GUI skin 当成自制 UI 的替代品。Slate 播放、暂停、场景绑定和运行控制在 BTSMTL Embedded Surface 中 MUST被隐藏或禁用。

BTSMTL `TimelineData`、Track/Clip/Section/TreeClip authoring identity、SerializedOwner、Source Map、Mutation、Undo、Preview、Live Debug 和 Document identity MUST继续由 BTSMTL 拥有。Slate `Cutscene`、Group、Track 和 ActionClip 只能由 Editor-only projection 提供给 Slate UI，不能成为持久化或 runtime 数据源。

BTSMTL Skill、Timeline、Preview 和 Runtime MUST NOT依赖 Slate GameObject Actor、DirectorGroup、Camera/Audio/Director Track、PlayableGraph 或 Slate Preview。Projection 中的 Unity/Slate 对象若为满足 Slate Surface 的临时兼容对象，MUST NOT拥有角色、技能、authoring 数据或 runtime 状态。

Timeline 页面 MUST只拥有作者编辑、正式 Mutation/Undo 和被动 Runtime Trace overlay。Scene Play 的 Start、Pause、Resume、Reset、Stop、Build、Skill request、Live Debug、Capture、History、Restore 和 Replay MUST由 SkillGraph/Graph Shell 调用唯一 Scene Play coordinator；Timeline 不得创建 `TimelinePreviewSession`、独立 evaluator、私有 clock 或同类运行命令。

#### Scenario: 从正式Skill Graph打开Skill Timeline

- **WHEN** 作者从正式 Skill Graph 调用点打开 Timeline
- **THEN** 系统 MUST 为当前 BTSMTL Timeline 创建或刷新 Editor-only Slate projection
- **AND** MUST在唯一 `TimelineEditorWindow` 的嵌入 Surface 中调用 Slate `CutsceneEditor.DrawEmbeddedGUI`
- **AND** 该窗口 MUST显示当前 Timeline 的 Track、Clip、Section和identity映射
- **AND** BTSMTL Timeline入口 MUST NOT 创建独立的 Slate `EditorWindow`
- **AND** 不得同时打开或维护上一轮 UI Toolkit Timeline 作为第二个正式编辑表面

#### Scenario: Embedded Surface不带入Slate默认Director

- **WHEN** BTSMTL Timeline 创建 Embedded Slate Surface
- **THEN** Surface MUST 清理 Slate `Reset/TryReset` 自动创建的 DirectorGroup、CameraTrack、DirectorAudioTrack 和 DirectorActionTrack
- **AND** Surface MUST隐藏或禁用 Actor Group、New Cutscene 和 Slate Runtime Playback入口
- **AND** Surface MUST只显示 BTSMTL projection 提供的 Group、Track、Clip 和 Curve

#### Scenario: Slate UI编辑Clip范围

- **WHEN** 作者在 Slate `CutsceneEditor` 中移动或裁剪一个 projection Clip
- **THEN** adapter MUST按 projection identity 将结果转换成 BTSMTL frame/value mutation
- **AND** 正式写入 MUST经过 `TimelineEditorSessionContext`、owner 和 BTSMTL Mutation
- **AND** mutation 完成后 MUST从 BTSMTL owner 重新生成 projection

#### Scenario: Timeline owner外部刷新

- **WHEN** BTSMTL Timeline 被 Undo/Redo、Agent 或其它正式入口修改
- **THEN** adapter MUST销毁旧 projection 的临时状态并从最新 BTSMTL Timeline 重建
- **AND** MUST不把旧 Slate proxy 的字段覆盖回 BTSMTL

### Requirement: Slate Projection必须是Editor-only桥接而不是第二个正式数据源

Projection MUST建立明确的 BTSMTL identity 到 Slate object 的双向映射。Projection 对象 MUST使用临时生命周期，不得绑定 BTSMTL SerializedObject，不得保存为资产，不得进入 Document manifest、Timeline compiler、runtime player 或 runtime clock。Projection 只负责满足 Slate UI 的对象模型和交互要求。

Projection MUST将每个可编辑 Clip 挂载到 Slate `CutsceneTrack` 能发现的 ActionClip 集合中。Slate `Validate`、Clip wrapper 或 DopeSheet 不得因为临时 GameObject 层级错误而丢失 Clip；BTSMTL 已拥有的 Curve Channel MUST只能通过该 Clip 的 Slate AnimatedParameter 显示和编辑。

#### Scenario: 关闭Slate窗口

- **WHEN** Slate Surface 被销毁或切换到其它 Timeline owner
- **THEN** adapter MUST丢弃临时 Cutscene、Group、Track 和 ActionClip 对象
- **AND** MUST保留已提交的 BTSMTL Timeline 修改
- **AND** MUST不产生 Slate Cutscene 资产或残留的可运行 GameObject

#### Scenario: Slate插件不可用

- **WHEN** 当前编辑器无法加载 Slate `CutsceneEditor` 或嵌入 Surface
- **THEN** Timeline UI MUST显示明确的 Unavailable 原因
- **AND** MUST不偷偷切回上一轮 UI Toolkit仿制界面
- **AND** MUST不创建替代数据源或默认 Timeline

### Requirement: Timeline Editor必须明确Slate临时写入与BTSMTL正式写入边界

Slate 对 proxy 的字段修改 MUST仅作为手势草稿。Adapter MUST通过 snapshot/diff 和明确 transaction callback 将一次有效 Slate 编辑转换成一次 BTSMTL Mutation，隔离 proxy 的 Undo/dirty，不允许形成第二套用户可见撤销历史。未能满足隔离要求的命令 MUST报告不可用并继续作为未完成项，不允许通过说明存在双 Undo 来宣称交付。

#### Scenario: 一次Slate拖动提交

- **WHEN** 作者完成一次 Clip 拖动
- **THEN** proxy 可以在拖动期间发生临时字段变化
- **AND** adapter MUST在提交边界只向 BTSMTL owner提交一次正式 mutation
- **AND** MUST不把每一帧 proxy变化分别写入 BTSMTL

#### Scenario: Pointer Cancel或窗口关闭

- **WHEN** Slate拖动被取消、窗口关闭或projection identity过期
- **THEN** adapter MUST丢弃未提交的 proxy变化
- **AND** MUST从 BTSMTL owner重新生成 projection
- **AND** MUST不写入半成品 TimelineData

### Requirement: Preview与Live Debug控制必须归Graph Shell

Scene Play Session、Runtime Trace、Follow/Pin、TreeClip ownership、Character/Actor target、Build、Skill request、Capture、History、Restore 和 Replay MUST由 Graph Shell/SkillGraph 与唯一 Scene Play coordinator 管理。Timeline 只读取正式 binding，将当前实际运行标记作为只读 overlay 显示；Timeline 不得控制运行对象或复制这些状态机。

Embedded Slate Surface MUST NOT调用 Slate `Sample`、`Play`、`PlayableGraph` 或 Actor binding 来执行 Preview。Scene Play Session 与领域 owner 是唯一时间推进和输出 owner；Slate current time 只允许作为编辑游标和被动 overlay 的显示输入。

#### Scenario: Graph Shell启动Scene Play

- **WHEN** 作者在 Graph Shell 点击 Scene Play、Build 或 Skill request
- **THEN** 命令 MUST进入唯一 Scene Play coordinator 和正式 Session
- **AND** Timeline MUST只接收正式 runtime binding/overlay，不创建本地播放器
- **AND** Timeline 的作者编辑能力 MUST不因打开运行观察而复制或切换到另一个窗口

#### Scenario: Scene Play期间编辑Timeline

- **WHEN** 作者在同一 Scene Play Session 中拖动 Clip、修改 Curve 或 Section
- **THEN** Timeline MUST通过正式 BTSMTL Mutation/Undo 写入作者 Timeline
- **AND** Graph Shell MUST负责显示 dirty、Build 和 Program adoption 状态
- **AND** Build 成功后 MUST在同一 Session 的 adoption barrier 采用兼容的新 ProgramEpoch；当前 Action 不兼容时保留旧 Epoch，下一次 Action 才采用

#### Scenario: Timeline只读观察运行

- **WHEN** Scene Play coordinator 已产生正式 Runtime Trace
- **THEN** Timeline MAY显示 active Track/Clip、logic/visual time、TreeClip phase 和 playback identity overlay
- **AND** overlay MUST只读，Timeline 不得暂停、恢复、重置、恢复历史或回放运行对象

### Requirement: Timeline新增Track与Clip必须使用正式typed authoring contract

Timeline Editor MUST提供正式的 Add Track/Add Clip 作者入口；候选类型 MUST来自当前 Timeline owner 的 `TimelineContractCatalog` 和 Track contract 的 allowed clip kinds。新增操作 MUST调用正式 `TimelineData.AddTrack`、`TimelineData.AddClip` 或其等价的唯一 typed Mutation API，并进入同一个 `TimelineEditorSessionContext`、Undo 和 owner revision。

Slate 原生创建的临时 `CutsceneTrack`、`ActionClip`、GameObject、组件 instance id、显示名称和 proxy local state MUST NOT直接成为 BTSMTL authoring 数据。新增对象的正式 identity、ContractKind、Track/Clip relationship、typed properties 和外部资源引用 MUST由 Timeline owner/API生成并校验；新增完成后 MUST从 owner 重建 Slate projection。

Animation Clip MUST只能选择已存在的原生 AnimationClip；TreeClip MUST使用正式支持的 inline/shared ownership 和 Graph/Tree 来源，inline 创建 MUST走既有正式 authoring API；Camera、Motion、Cue 和其它 typed Clip MUST使用对应的 authoring binding。系统 MUST允许作者主动创建不含 Clip 的合法 Track；MUST不因取消或失败留下半成品，不创建替代 AnimationClip、默认 Tree 或 fallback contract。

#### Scenario: 新增合法Track

- **WHEN** 作者在 Timeline Surface 的 Add Track 菜单选择当前 owner 支持的 Track contract
- **THEN** adapter MUST通过正式 Timeline Mutation 创建 Track 并生成正式 identity
- **AND** 新 Track MUST进入 owner revision、Undo 和重建后的 Slate projection
- **AND** 完成必填字段的无 Clip Track MUST允许保存，后续可独立添加 Clip

#### Scenario: 新增不允许的Clip

- **WHEN** 作者在一个 Track 上打开 Add Clip 菜单
- **THEN** 菜单 MUST只显示该 Track contract 允许的 Clip kind
- **AND** 不允许的 Clip MUST无法通过 Slate 原生菜单创建或写入 authoring

#### Scenario: 新增Animation或TreeClip

- **WHEN** 作者选择一个已有 AnimationClip 或正式 Graph/Tree 作为新增内容来源
- **THEN** adapter MUST通过对应 typed binding 创建 Animation Segment 或 TreeClip
- **AND** proxy对象、GameObject层级和显示名称 MUST不进入正式 Timeline 数据

#### Scenario: 新增取消或校验失败

- **WHEN** 作者取消资源选择或新增输入未通过 contract validation
- **THEN** 系统 MUST丢弃临时菜单状态
- **AND** MUST不新增半成品 Track/Clip、无效 identity 或 Undo 项，MUST保留作者此前已创建的合法空 Track

#### Scenario: 创建位置与字段

- **WHEN** 作者在轨道空白处右键选择在第 N 帧添加 Clip
- **THEN** 创建表单 MUST携带准确 Track identity、插入帧、contract 和 required typed fields
- **AND** 资源选择、全部字段和 owner revision 校验成功后 MUST在同一正式事务创建对象，再投影为 Slate Clip
- **AND** 成功后 MUST选中新对象；取消或失败 MUST不修改已有内容

## ADDED Requirements

### Requirement: Timeline必须与共享预览区完成跨窗口联动

Timeline 与 SkillGraph/Graph Shell MUST按同一联动计划交付导航、运行 binding、作者/运行版本状态及生命周期。Timeline MUST提供“预览”导航与精确关联说明；共享预览区 MUST支持从实际调用打开对应 Timeline。角色结果 MUST来自正式 Scene Play，Timeline 编辑游标 MUST不执行角色或 Slate 内核。

#### Scenario: 技能产生多个Timeline调用

- **WHEN** 作者从正在预览的技能打开 Timeline
- **THEN** 系统 MUST根据真实 ActionInstance、完整调用路径、generation 和内容版本关联 Timeline
- **AND** 多个合法调用 MUST明确选择，不按列表首项猜测；Tree-only 技能 MUST正常显示无 Timeline

#### Scenario: 编辑后返回预览

- **WHEN** 作者修改 Timeline 并返回共享预览区
- **THEN** 作者数据 MUST保持，预览区 MUST显示正式待采用/已采用/下一次激活/失败状态
- **AND** 切页、折叠和关闭 Timeline MUST不停止或更换 Session；runtime overlay 和编辑帧 MUST分别保存

#### Scenario: 独立内容预览

- **WHEN** 作者从 shared Timeline 请求查看预览
- **THEN** 系统 MUST使用其精确正式非 Skill 调用方或已明确关联的技能来源
- **AND** 无合法绑定时 MUST保留编辑并解释缺项，不伪造 Actor、Skill 或 Timeline 播放器

### Requirement: Timeline布局必须统一计算并适应窗口尺寸

嵌入 Surface MUST以单一布局结果提供背景、分隔线、控件、裁剪和命中范围。窗口 MUST包含紧凑文档行、编辑工具栏、左侧搜索/轨道、右侧缩放/帧标尺/Clip/Curve 和可收起底部属性区。左右内容 MUST共享行高度和垂直滚动；MUST不为隐藏的 Slate 控件或不存在的工具保留空白。文档名称只显示一次，ownership 为短标记，长路径在 tooltip。

#### Scenario: 窄窗口与曲线展开

- **WHEN** 窗口缩到 600×360 逻辑像素或作者展开曲线
- **THEN** 左轨道和右内容 MUST保持对齐，属性区 MAY收起，次要工具 MUST折叠或省略文字
- **AND** 主要按钮、数值输入、标尺 MUST不重叠，命中位置 MUST与显示一致

#### Scenario: 缩放条与游标

- **WHEN** 作者拖动顶部缩放范围条
- **THEN** 操作 MUST只改变视窗，游标的命中区域 MUST不占用缩放条
- **AND** 隐藏工具栏 MUST不留下额外高度，背景 MUST使用最新布局边界

### Requirement: Timeline必须使用正式帧率统一编辑时间

Surface MUST消费正式 Timeline Session 的 FrameRate，统一像素、整数作者帧和 Slate 秒的转换；MUST不以 Slate 全局 FPS 或秒吸附偏好决定 BTSMTL 显示与保存。标尺、帧输入、Clip/Section 边界与关键帧编辑 MUST以作者帧为主。作者帧 MUST不自动解释为 Runtime Logic Tick。现有资产未编辑的数据 MUST不被整体量化。

#### Scenario: 编辑一帧

- **WHEN** 作者把 Clip 起点从第 12 帧移到第 13 帧并提交
- **THEN** 草稿、属性和正式 StartFrame MUST一致为 13，一次 Undo MUST恢复为 12
- **AND** 上一帧/下一帧 MUST严格移动 1 帧，不跳到相邻关键帧

#### Scenario: Curve时间换算

- **WHEN** 作者编辑 Timeline-local 曲线 key
- **THEN** adapter MUST按正式 descriptor 的 domain 转换局部秒与 normalized time，正确处理 CurveEndFrame 等领域边界和切线缩放
- **AND** 未编辑 key 的时间、值、tangent、weight、WeightedMode 和 wrap mode MUST保持

#### Scenario: 显示全部内容

- **WHEN** 作者打开短 Timeline 或选择显示全部
- **THEN** 内容终点 MUST来自真实帧范围，视窗 MAY保留像素边距但 MUST不改变文档
- **AND** 空文档显示视窗 MUST不创建一秒内容，终点线 MUST不提供无正式字段对应的长度写入

### Requirement: Timeline刷新必须保留有效作者状态

编辑视图状态 MUST按正式 identity 保存选择、曲线通道、展开、当前编辑帧、缩放、滚动和属性高度。提交、Undo/Redo、外部更新 MUST恢复仍有效状态，不自动改选首个 Clip。选择、游标、缩放和搜索 MUST不产生 authoring Undo 或无条件重建。属性区 MUST显示正式 typed 字段，不编辑 proxy 私有参数。

#### Scenario: 修改后继续编辑

- **WHEN** 作者在已缩放并展开曲线的视图提交字段或曲线修改
- **THEN** 选择、曲线展开和视野 MUST保持，正式属性 MUST反映提交结果
- **AND** 原对象被删除时 MUST清空对应选择，不静默选中其它内容

#### Scenario: 不支持的命令或过期草稿

- **WHEN** 命令没有正式映射或 source revision 已变化
- **THEN** adapter MUST明确报告原因并丢弃无效草稿，MUST不静默吞修改或覆盖最新 owner
- **AND** 合法未提交文本 MUST不被普通重绘清空

### Requirement: 编辑控件不能冒充真实角色预览

Timeline MUST保留编辑游标、整数帧输入和逐帧操作；编辑游标、真实运行标记和 Capture 历史位置 MUST分别保存。默认交付 MUST不包含未确认的本地自动播放游标或 Timeline 内 Scene Play 快捷控制。导航 MAY返回精确来源 Graph Shell，但 MUST不启动/重建 Session。嵌入按钮、快捷键、EditorUpdate、初始化/释放、保存和 delayCall MUST不调用 Slate Play/Sample/ReSample/Stop 执行预览，MUST清理 AutoKey 与临时作者播放器。

#### Scenario: 没有运行绑定

- **WHEN** 作者独立打开 shared Timeline
- **THEN** 轨道、Clip、曲线和编辑帧 MUST完整可编辑
- **AND** MUST不创建本地播放器或猜测角色目标，不把游标移动显示成角色已运行

#### Scenario: 运行时继续编辑

- **WHEN** 作者在 Scene Play 中编辑正式 Timeline
- **THEN** 作者数据 MUST经同一 Mutation/Undo 修改，运行标记 MUST只读消费真实绑定
- **AND** 新版本是否采用 MUST由 Graph Shell 的既有 Build/adoption 合同报告，不更换 Session 或直接写运行状态

### Requirement: 临时投影必须正确释放且没有无关编辑入口

临时 Surface MUST清理选择、回调与宿主对象，MUST不产生重复序列化字段或失效对象调用。嵌入路径 MUST移除 Actor/Director/Render 等无业务入口及未映射原生命令；MUST保留项目正式 Camera Track。曲线密集显示优化 MUST不改写正式 key，无曲线内容 MUST不创建空参数面板。

#### Scenario: 关闭与重新打开

- **WHEN** 作者保存后关闭并重新打开 Timeline
- **THEN** 正式 Track/Clip identity、资源和帧范围 MUST保持，旧临时对象 MUST释放
- **AND** MUST无新增 GUI、序列化或生命周期异常，异常处理 MUST基于完整堆栈而不是隐藏 Console

### Requirement: 旧UI Toolkit仿制Timeline路径必须删除

完成本 change 后，正式 Timeline 编辑入口 MUST不再依赖上一轮新增的 UI Toolkit Slate仿制 UXML/USS、独立 viewport、独立 zoom/pan、独立 Clip hit-test 或独立 rendering path。项目 MUST只保留 Slate `CutsceneEditor` 作为 Timeline 编辑表面和 BTSMTL Mutation 作为正式写入链。

#### Scenario: 检查Timeline编辑入口

- **WHEN** 工程编译并打开正式 Timeline入口
- **THEN** 调用链 MUST能追溯到唯一 `TimelineEditorWindow` 中的 Slate `CutsceneEditor.DrawEmbeddedGUI`
- **AND** MUST不存在并行的旧 UI Toolkit Timeline窗口、仿 Slate皮肤或兼容开关
