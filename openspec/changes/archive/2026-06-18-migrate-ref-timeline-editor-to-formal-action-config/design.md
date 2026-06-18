# Design: Ref Timeline Editor 正式迁移

## 问题拆分
这次不是重新造一个 Timeline Editor，而是把 Ref 的成熟编辑交互迁移到本项目正式 action 配置上。当前已有窗口已经证明 `CharacterActionDefinitionSO` 可以作为入口，但实现层仍然缺少 Ref 的核心编辑器逻辑：

- 没有完整 track hierarchy 和 add track/dropdown。
- 没有 rectangle selector、多选、Delete、F 定位、滚轮缩放和中键平移。
- 没有 track handle 拖拽排序、track 删除和 track inspector。
- 没有 clip dropdown、新建 clip、drop 创建 clip、clip invalid feedback、ease-in/ease-out 操作。
- preview 只是 UI 高亮 active clip，没有通过正式 evaluator 输出 animation/motion/window/cue，也没有 editor-only visual preview binding。
- 当前 tests 主要验证“有 RefPorted 名字和资源”，不能证明迁移质量。

## 目标链路
正式链路必须是：

1. `CommittedActionTimelineEditorWindow` 选择 `CharacterActionDefinitionSO`。
2. `CharacterActionDefinitionSO` 暴露 Dodge selector 或通用 committed action branch timeline。
3. Editor adapter 将正式 serialized data 映射成 editor timeline view model。
4. Ref ported UI 操作 view model。
5. Adapter 将修改写回 `DodgeCommittedActionBranchAuthoring` / `CommittedActionBranchTimelineAuthoring` / `ActionTimelineTrackAuthoring` / `ActionTimelineClipAuthoring`。
6. Save 调用 Unity serialized save、dirty、undo 和正式 validator。
7. Preview 调用 `CommittedActionBranchEvaluator` / `ActionTimelineEvaluator` 生成当前 local time / local tick outcome。
8. Runtime 继续只消费 `CharacterActionDefinitionSO.ToDefinition()` 输出的纯数据 definition。

## 迁移边界
### 可以迁移
- `TimelineEditorWindow` 的布局结构、toolbar、track hierarchy、add track dropdown 交互。
- `TimelineFieldView` 的 marker、locator drag、scroll/zoom、rectangle selector、delete、focus current timeline position 交互。
- `TimelineTrackHandle` 与 `TimelineTrackView` 的 track 展示、选择、排序、drop clip 交互。
- `TimelineClipView` 的 move、resize、ease、invalid 状态、context menu 和视觉样式。
- Timeline inspector view 的字段展示思想。
- UXML、USS、图标和 manipulator 实现，复制后必须改命名空间和数据 adapter。

### 不迁移到正式 gameplay
- `TimelinePlayer`
- Taco `Timeline` / `Track` / `Clip` runtime object
- Taco `BaseTree`、`RunnableTree`、`RunnableNode`、`TreeRunner`
- Ref gameplay `PlayableGraph`
- Ref root motion、Animator、Audio、Particle、Cinemachine 直接副作用

Editor-only preview 可以参考 Ref 的 preview 操作方式，但必须封装在 Editor assembly，正式 runtime asmdef 不得引用。

## Adapter 设计
迁移后的 UI 不直接读 `SerializedProperty` 深路径散落在各个 view 里，而是通过 adapter 形成清晰边界：

- `ICommittedActionTimelineEditorModel`：暴露 action id、branch id、Directional / Backstep timeline、track、clip、duration、selection 和 validation result。
- `CommittedActionTimelineSerializedAdapter`：封装 `CharacterActionDefinitionSO` 的 serialized property 路径，负责 add/remove/reorder track、add/remove/move/resize clip、修改 payload。
- `CommittedActionTimelinePreviewAdapter`：把当前 local time / local tick 和当前 selector context 转成 `CommittedActionBranchEvaluationInput`，输出 selected node、animation key、motion spec、active windows、cue requests。
- `CommittedActionTimelineEditorValidator`：聚合 `CharacterActionDefinitionSO.Validate()` 与 timeline editor 级规则，禁止非法 clip kind、负 frame、end < start、缺 payload、重复/空 stable id。

如果实现发现需要新增 schema version、track id 或 clip id，必须作为正式 authoring 数据字段加入，并配套迁移和测试，不能只存在于 view state。

## Preview 设计
Preview 分两层：

1. 数据预览：每个 preview local tick 用正式 evaluator 得到 selected node、animation key、motion、window facts、cue。Timeline UI 必须能显示当前 local tick 哪些 clip 被 runtime 采样，以及 evaluator 结果。
2. 编辑器视觉预览：通过 editor-only preview binding 展示动画 key、motion 轨迹或 cue 状态。没有 preview binding 时显示明确配置错误或未绑定状态，不使用 scene 查找、Resources 或隐式默认对象。

这两层都不能改变正式 gameplay 的 motion executor、Animancer presenter 或 blackboard writer。视觉预览如果使用 Unity Editor API、AnimationMode 或 Playables，只能位于 Editor assembly，且不能被 runtime tests 扫到。

## 与 Graph Editor 的关系
Character Behavior Editor 仍然只表达 root / Locomotion leaf / CommittedAction leaf 的行为提交关系。Dodge selector 和 Directional / Backstep timeline 的主要编辑入口是 Committed Action Timeline Editor。Graph editor 可以打开或定位 action timeline，但不能复制一套 Dodge timeline 数据，也不能把 `FullBody` 当 source、slot 或状态树根。

## 与当前已完成 change 的关系
本 change 是对 `add-character-behavior-editor-adapters` 的纠偏和细化：

- 保留 Editor-only adapter、正式 definition、Ref UI 可移植、runtime 边界不变的要求。
- 收紧“Timeline Editor 默认编辑正式配置”为可操作迁移标准。
- 把“RefPorted 名字存在”升级为“Ref 核心 timeline 编辑交互和 preview 能力可用”。
- 明确当前 `Behavior/Samples` 不再作为正式 timeline editor 入口。

## 测试策略
- 不用字符串存在测试证明迁移完成；字符串测试只能作为边界补充。
- 用 editor model / adapter 测试证明 add/remove/reorder track、add/move/resize/delete clip 会写回正式 action definition。
- 用 compile/evaluator 测试证明保存后的正式 asset 能生成 runtime branch/timeline outcome。
- 用非法数据测试证明缺 track、非法 seconds / tick 区间、缺 payload、重复 id 会报错。
- 用静态边界测试证明 runtime 不引用 UnityEditor、GraphView、Ref runner、TimelinePlayer、PlayableGraph。
- 用 preview adapter 测试证明 Directional / Backstep 的当前 local tick 输出与 runtime evaluator 一致。

## 风险
- GitNexus 索引可能落后于当前磁盘，需要实现前重新运行 impact 并以 `3cDemo/Client/3C_Client` 实际文件为准。
- Ref timeline 原始编辑器依赖 Taco runtime object 和 Resources 加载，需要在迁移时拆成 UI/view-model/adapter，避免把 runtime type 偷带进 gameplay。
- 如果直接复制 Ref preview，会引入 PlayableGraph/root motion 运行路径；必须用 editor-only preview boundary 承接。
