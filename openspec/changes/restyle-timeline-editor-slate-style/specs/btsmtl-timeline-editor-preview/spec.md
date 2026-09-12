## MODIFIED Requirements

### Requirement: Timeline Editor必须直接使用Slate CutsceneEditor作为实际编辑表面

正式 Timeline 编辑入口 MUST 打开并使用 Slate `CutsceneEditor` 的真实 IMGUI 编辑 UI，包括时间尺、Group/Track列表、Clip、选择、拖动、缩放、Curve/DopeSheet 和 Slate Inspector。Timeline Editor MUST不再用 UI Toolkit 重新实现一套 Slate 风格视图，也 MUST不把 Slate 图片或 GUI skin 当成自制 UI 的替代品。Slate 播放、暂停、场景绑定和运行控制在 BTSMTL Embedded Surface 中 MUST被隐藏或禁用。

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

Slate 原生 `CutsceneEditor` 对 proxy 的字段修改、`Undo.RecordObject`、`Undo.RegisterFullObjectHierarchyUndo` 和 `EditorUtility.SetDirty` MUST被视为临时 projection 操作，不得直接等同于 BTSMTL 正式写入。Adapter MUST通过 snapshot/diff 或明确 transaction callback 将一次 Slate编辑转换成一次 BTSMTL Mutation。若未能隔离 Slate proxy 的 Undo，系统 MUST明确报告临时 Slate Undo 与 BTSMTL Undo 的边界，不得声称完全无双写。

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

Animation Clip MUST只能选择已存在的原生 AnimationClip；TreeClip MUST选择正式 Graph/Tree 来源；Camera、Motion、Cue 和其它 typed Clip MUST使用对应的 authoring binding。系统 MUST不创建替代 AnimationClip、默认 Tree、空 Track、空 Clip 或 fallback contract。

#### Scenario: 新增合法Track

- **WHEN** 作者在 Timeline Surface 的 Add Track 菜单选择当前 owner 支持的 Track contract
- **THEN** adapter MUST通过正式 Timeline Mutation 创建 Track 并生成正式 identity
- **AND** 新 Track MUST进入 owner revision、Undo 和重建后的 Slate projection

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
- **AND** MUST不留下空 Track、空 Clip、无效 identity 或半成品 Undo

## ADDED Requirements

### Requirement: 旧UI Toolkit仿制Timeline路径必须删除

完成本 change 后，正式 Timeline 编辑入口 MUST不再依赖上一轮新增的 UI Toolkit Slate仿制 UXML/USS、独立 viewport、独立 zoom/pan、独立 Clip hit-test 或独立 rendering path。项目 MUST只保留 Slate `CutsceneEditor` 作为 Timeline 编辑表面和 BTSMTL Mutation 作为正式写入链。

#### Scenario: 检查Timeline编辑入口

- **WHEN** 工程编译并打开正式 Timeline入口
- **THEN** 调用链 MUST能追溯到唯一 `TimelineEditorWindow` 中的 Slate `CutsceneEditor.DrawEmbeddedGUI`
- **AND** MUST不存在并行的旧 UI Toolkit Timeline窗口、仿 Slate皮肤或兼容开关
