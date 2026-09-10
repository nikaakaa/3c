## MODIFIED Requirements

### Requirement: Timeline Editor必须直接使用Slate CutsceneEditor作为实际编辑表面

正式 Timeline 编辑入口 MUST 打开并使用 Slate `CutsceneEditor` 的真实 IMGUI UI，包括时间尺、Group/Track列表、Clip、选择、拖动、缩放、播放控制和 Slate Inspector。Timeline Editor MUST不再用 UI Toolkit 重新实现一套 Slate 风格视图，也 MUST不把 Slate 图片或 GUI skin 当成自制 UI 的替代品。

BTSMTL `TimelineData`、Track/Clip/Section/TreeClip authoring identity、SerializedOwner、Source Map、Mutation、Undo、Preview、Live Debug 和 Document identity MUST继续由 BTSMTL 拥有。Slate `Cutscene`、Group、Track 和 ActionClip 只能由 Editor-only projection 提供给 Slate UI，不能成为持久化或 runtime 数据源。

#### Scenario: 从正式Skill Graph打开Skill Timeline

- **WHEN** 作者从正式 Skill Graph 调用点打开 Timeline
- **THEN** 系统 MUST 为当前 BTSMTL Timeline 创建或刷新 Editor-only Slate projection
- **AND** MUST调用 Slate `CutsceneEditor.ShowWindow(projection)` 显示真实 Slate UI
- **AND** Slate 窗口 MUST显示当前 Timeline 的 Track、Clip、Section和identity映射
- **AND** 不得同时打开或维护上一轮 UI Toolkit Timeline 作为第二个正式编辑表面

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

#### Scenario: 关闭Slate窗口

- **WHEN** Slate `CutsceneEditor` 关闭或切换到其它 Timeline owner
- **THEN** adapter MUST丢弃临时 Cutscene、Group、Track 和 ActionClip 对象
- **AND** MUST保留已提交的 BTSMTL Timeline 修改
- **AND** MUST不产生 Slate Cutscene 资产或残留的可运行 GameObject

#### Scenario: Slate插件不可用

- **WHEN** 当前编辑器无法加载 Slate `CutsceneEditor`
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

### Requirement: Preview与Live Debug必须继续由BTSMTL拥有

Slate `CutsceneEditor` 的播放和时间控件 MUST不创建第二个 BTSMTL Timeline player、PlayableGraph、时钟或 runtime binding。Authoring Preview、Live Debug、Follow/Pin overlay、TreeClip ownership 和 Character Preview Target MUST继续经 BTSMTL 的 session adapter 管理；Slate UI只显示或驱动经过适配的编辑时间状态。

#### Scenario: Authoring Preview切换Live Debug

- **WHEN** 作者从 Authoring Preview切换到 Live Debug
- **THEN** BTSMTL session MUST停止 authoring preview 并建立本地 runtime binding
- **AND** Slate projection MUST进入只读或由 adapter禁止编辑的状态
- **AND** MUST不启动第二个 Slate runtime playback

## ADDED Requirements

### Requirement: 旧UI Toolkit仿制Timeline路径必须删除

完成本 change 后，正式 Timeline 编辑入口 MUST不再依赖上一轮新增的 UI Toolkit Slate仿制 UXML/USS、独立 viewport、独立 zoom/pan、独立 Clip hit-test 或独立 rendering path。项目 MUST只保留 Slate `CutsceneEditor` 作为 Timeline 编辑表面和 BTSMTL Mutation 作为正式写入链。

#### Scenario: 检查Timeline编辑入口

- **WHEN** 工程编译并打开正式 Timeline入口
- **THEN** 调用链 MUST能追溯到 Slate `CutsceneEditor.ShowWindow`
- **AND** MUST不存在并行的旧 UI Toolkit Timeline窗口、仿 Slate皮肤或兼容开关
