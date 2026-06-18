## 1. 准备与规格对齐
- [x] 1.1 读取本 change 的 `proposal.md`、`design.md`、`tasks.md` 和 spec delta。
- [x] 1.2 确认 current specs 保持独立 Timeline Window 定位 TimelineNode 口径。
- [x] 1.3 读取 `openspec/specs/committed-action-timeline-editor/spec.md`。
- [x] 1.4 读取 `openspec/specs/action-timeline-framework/spec.md`。
- [x] 1.5 读取 `openspec/specs/action-animation-profile/spec.md`。
- [x] 1.6 查找当前 `CommittedActionTimelinePreviewAdapter` 数据预览入口。
- [x] 1.7 查找当前 Timeline window / view 的 preview locator、play、pause 和 summary 更新入口。
- [x] 1.8 查找正式动作动画 key 解析路径和绑定角色上的 Animancer TransitionLibrary 使用方式。
- [x] 1.9 查找 Ref `wly970123` 中 PlayableGraph / AnimationClipPlayable 采样实现并记录可移植片段。
- [x] 1.10 对将修改的核心 editor symbol 运行 GitNexus impact。
- [x] 1.11 若 impact 为 HIGH 或 CRITICAL，先拆分风险方案后再实现。

## 2. Preview Target 绑定模型
- [x] 2.1 定义 Editor-only scene preview binding 数据结构，保存 target GameObject、Animator、binding status 和诊断文本。
- [x] 2.2 在 Timeline window 顶部增加 preview target ObjectField。
- [x] 2.3 限制 preview target 只能接受 scene object 或批准等价实际预览目标。
- [x] 2.4 target 为空时保持现有数据预览并显示未绑定状态。
- [x] 2.5 target 缺失 Animator 时显示明确错误状态。
- [x] 2.6 PlayMode 下禁用视觉采样或降级为数据预览，并显示明确状态。
- [x] 2.7 target 变化时释放旧 preview session。

## 3. Animation Key 解析
- [x] 3.1 定义 Editor-only `ActionAnimationKey` 到 preview clip/transition 的 resolver 接口。
- [x] 3.2 从绑定角色解析正式动画表现入口或 Animancer TransitionLibrary。
- [x] 3.3 支持将 `ActionAnimationKey.Value` 解析为 `ClipTransition` 或直接可采样 `AnimationClip`。
- [x] 3.4 非 `ClipTransition` 或未知 transition 类型必须返回 unsupported diagnostic。
- [x] 3.5 缺失 key、缺失 library、缺失 transition 和无效 clip 都必须返回明确 diagnostic。
- [x] 3.6 resolver 不调用 runtime presenter 的播放方法。
- [x] 3.7 resolver 不写 runtime blackboard、motion executor 或角色帧管线。

## 4. Editor-only PlayableGraph 采样
- [x] 4.1 新增 Editor-only preview session，负责创建、持有和销毁 PlayableGraph。
- [x] 4.2 session 绑定 target Animator 并创建 AnimationPlayableOutput。
- [x] 4.3 session 能用解析出的 AnimationClip 创建 AnimationClipPlayable。
- [x] 4.4 session 能按 preview local time 设置 clip time。
- [x] 4.5 session 在 scrub 时调用 graph Evaluate(0) 采样姿态。
- [x] 4.6 session 在 play 时按 Timeline preview tick 推进采样，但不改变 runtime tick 权威。
- [x] 4.7 切换 animation key 或 clip 时重建必要 playable 输入。
- [x] 4.8 停止 preview、解绑、窗口关闭和 domain reload 时销毁 graph。
- [x] 4.9 进入 preview 前记录必要角色状态，退出时恢复 transform / animator controller / enabled 状态或批准等价状态。

## 5. Timeline Preview 集成
- [x] 5.1 扩展 preview result，使其包含 scene binding status、resolved clip name 和 visual preview status。
- [x] 5.2 `SetPreviewFrame` 继续先调用正式 preview adapter 取得 outcome。
- [x] 5.3 将 active `ActionAnimationKey` 交给 resolver 和 preview session。
- [x] 5.4 Timeline summary 显示数据 preview 和视觉 preview 两层状态。
- [x] 5.5 active clip 高亮继续使用正式 tick 命中结果。
- [x] 5.6 Motion clip 第一版只显示 motion spec 摘要和 editor-only ghost/path 诊断，不执行 motion executor。
- [x] 5.7 Window / Cue 第一版只显示 active facts 和 cue ids，不触发表现事件。
- [x] 5.8 preview session 错误不得阻止 timeline authoring 编辑和保存。

## 6. 边界清理
- [x] 6.1 确认 runtime 源码不引用新增 preview session / preview binding。
- [x] 6.2 确认 runtime 源码不引用 `PlayableGraph`、`AnimationPlayableOutput`、`AnimationClipPlayable` 作为 ActionTimeline 执行路径。
- [x] 6.3 确认 runtime 源码不引用 Ref/Taco `TimelinePlayer`、`TimelineRunningTree` 或 `TreeClip`。
- [x] 6.4 确认 ActionTimeline definition / outcome 不保存 scene target、Animator、AnimationClip 或 Unity object。
- [x] 6.5 确认 preview binding 不写入正式 `CharacterActionDefinitionSO` runtime definition。

## 7. 自动测试
- [x] 7.1 添加 preview binding 空 target 状态测试。
- [x] 7.2 添加 preview binding 缺失 Animator 状态测试。
- [x] 7.3 添加 ActionAnimationKey resolver 成功解析 clip 的 EditMode 测试。
- [x] 7.4 添加 resolver 缺失 library / transition / clip 的诊断测试。
- [x] 7.5 添加 preview session 创建和销毁 PlayableGraph 的 EditMode 测试或批准等价 seam 测试。
- [x] 7.6 添加 scrub local tick 到 clip time 映射测试。
- [x] 7.7 添加 `SetPreviewFrame` 仍使用正式 evaluator outcome 的回归测试。
- [x] 7.8 添加 PlayMode 禁用视觉采样或只读数据预览的边界测试。
- [x] 7.9 添加 runtime 静态边界测试，确认新增 preview 类型不进入 `Assets/Scripts/Character` runtime 路径。
- [x] 7.10 添加静态测试确认没有复制 Ref/Taco runner 作为正式 runtime。

## 8. 验证
- [x] 8.1 运行 `openspec validate add-committed-action-timeline-scene-preview-binding --strict --no-interactive`。
- [x] 8.2 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp-Editor.csproj --no-restore`。
- [x] 8.3 通过 Unity MCP 运行 `Tests.Editor.Character.Action.Timeline.CommittedActionTimelineEditorAdapterTests`。
- [x] 8.4 通过 Unity MCP 运行新增 preview binding / resolver / session 定向 EditMode 测试。
- [x] 8.5 运行相关 runtime 边界静态测试。
