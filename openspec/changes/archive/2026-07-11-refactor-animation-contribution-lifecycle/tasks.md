## 1. 合同复查

- [x] 1.1 读取本 change 的 proposal、design 和 tasks，确认只实施统一动画贡献生命周期，不修改 Corin 动画资源。
- [x] 1.2 复查 `AnimationContribution`、`AnimationLayerPlaybackPlan`、`CharacterAnimationLayerRuntime` 和 `AnimancerAnimationPresenter` 当前字段与清理语义。
- [x] 1.3 复查 `TimelinePlaybackHandle`、`TimelinePlaybackRequest`、`TimelineNode` 和 `TimelinePlaybackScheduler` 当前完成、取消和释放顺序。
- [x] 1.4 复查 StateMachine state enter/root/exit、transition event 和 inline state body 的 runtime user 传播链路。
- [x] 1.5 复查 `CharacterPipelineFrame.Begin()`、`CaptureLogicSample()`、`PresentationFrame()` 和 `ClearTransient()` 在 catch-up logic tick 下的清理顺序。
- [x] 1.6 复查 Timeline 编辑器 preview session、preview target、LayerRuntime 和 Animancer adapter 的正式调用链。

## 2. 统一身份与提交合同

- [x] 2.1 定义稳定的 animation playback instance identity，区分同一节点的不同播放实例。
- [x] 2.2 定义稳定的 contribution instance identity，区分同一 playback 内不同 track/clip slot。
- [x] 2.3 定义 animation owner scope identity，能表达 StateMachine runtime、StateNode 和 activation generation。
- [x] 2.4 定义 standalone playback owner scope，不依赖 state name、tree name或场景对象。
- [x] 2.5 定义统一 `Sample` 提交，携带 clip、layer、priority、time、weight、loop context 和三类 identity。
- [x] 2.6 定义统一 `Complete` 提交，只结束 producer 推进，不隐式释放 owner。
- [x] 2.7 定义统一 `Release` 提交，明确 contribution 离开 clip 范围或 owner 正式释放。
- [x] 2.8 定义 owner transition event，携带 source owner、target owner、transition id、duration 和 curve。
- [x] 2.9 为 lifecycle command 增加 local logic tick 和同 tick 内稳定 sequence。
- [x] 2.10 删除或改名被新 identity/lifecycle 合同取代的临时 key、状态和命名，不保留兼容字段。

## 3. StateMachine owner scope

- [x] 3.1 在通用 BTSMTL runtime 层定义不依赖 Character、Timeline 或 Animancer 的 state execution scope 合同。
- [x] 3.2 让每个 StateMachine runtime 实例拥有稳定 runtime identity。
- [x] 3.3 让每次 StateNode enter 生成新的 activation generation 和 owner scope。
- [x] 3.4 在 tick State OnEnter 图时推入并恢复当前 owner scope。
- [x] 3.5 在 tick State Root 图时推入并恢复当前 owner scope。
- [x] 3.6 在 tick State OnExit 图时推入并恢复当前 owner scope。
- [x] 3.7 让 `CharacterGraphContext` 在动画 producer 提交时读取当前 owner scope。
- [x] 3.8 确保并行 Locomotion 和 Action StateMachine 的 owner scope 不互相覆盖。
- [x] 3.9 确保同一 StateNode 再次进入时不会复用上一 activation owner。
- [x] 3.10 禁止通过 `Owner.name`、state display name、场景搜索或静态全局变量补齐 owner scope。

## 4. State transition presentation event

- [x] 4.1 将现有只表达非零 blend 的 transition 输出收口为正式 owner handoff event。
- [x] 4.2 让 duration 为 0 的 Transition 也发布 source/target owner handoff。
- [x] 4.3 保留 Transition edge 的 duration 和 curve 作为唯一 authoring 来源。
- [x] 4.4 让 pending OnExit 完成前 source owner 保持有效，不因 state root 停止提前消失。
- [x] 4.5 transition 完成后只 tick target state，不继续 tick source state 获取动画。
- [x] 4.6 删除旧 `AnimationTransitionBlendRequest` 或等价旧事件的兼容双发路径。
- [x] 4.7 owner transition 到达 Registry 后先保持 pending，不在 target state body 首次执行前释放 source。
- [x] 4.8 target activation 的 OnEnter 或 Root 首次实际 tick 后提交 `OwnerReady`。
- [x] 4.9 `OwnerReady` 不以目标必须产出动画为条件，缺动画时暴露真实空输出。

## 5. Timeline contribution lifecycle

- [x] 5.1 将 `TimelinePlaybackHandle` 映射为统一 playback instance identity。
- [x] 5.2 将请求时捕获的 state activation scope写入 `TimelinePlaybackRequest` 和 active record。
- [x] 5.3 为 Timeline AnimationTrack 的 track/clip slot 生成 playback 内稳定 contribution identity。
- [x] 5.4 在表现帧对当前有效 clip 提交或更新 `Sample`。
- [x] 5.5 记录每个 playback 上一表现采样仍有效的 contribution identities。
- [x] 5.6 clip 离开 `ExtraPolationMode=None` 范围时提交明确 `Release`。
- [x] 5.7 `ExtraPolationMode=Hold` 时继续由 AnimationTrack 提交正式 Hold sample。
- [x] 5.8 保持重叠 clip 各自提交 contribution，并继续进入统一 layer/priority/weight 仲裁。
- [x] 5.9 Once Timeline logic 完成时立即写回 `Succeeded`，同时创建最小 terminal pending record。
- [x] 5.10 terminal animation 继续在 PresentationFrame 通过 AnimationTrack 正式采样，不在 logic tick 烘焙 pose。
- [x] 5.11 state-owned playback 完成时对仍有效 contributions 提交 `Complete`，不恢复已 Release 的 clip。
- [x] 5.12 state root 停止 loop playback 时保留当前合法 sample 到 owner handoff，不继续采样 gameplay facts。
- [x] 5.13 standalone playback 完成、取消或失败时按 standalone owner 规则释放。
- [x] 5.14 pipeline deactivate/dispose 时释放所有 Timeline playback 和未交付 terminal records。
- [x] 5.15 Registry 确认 terminal handoff 后销毁对应 runtime Timeline clone。
- [x] 5.16 保持 Loop Timeline 的 handle、cycle 和表现帧连续采样语义不变。
- [x] 5.17 让 TimelineNode 的 Success/Failure 只映射逻辑播放请求状态，不直接删除 owner-scoped Registry entries。
- [x] 5.18 让 TimelineNode stop/reset 取消未完成逻辑请求，同时把 state-owned 表现退场交给 owner handoff。
- [x] 5.19 让不属于 State activation 的 TimelineNode 在停止、重置或释放时显式释放 standalone owner。
- [x] 5.20 terminal sample 被 Registry 接受后清理短期 completed-playback metadata，但保留 owner-held entries。

## 6. 统一 Contribution Registry

- [x] 6.1 新增来源无关的 `CharacterAnimationContributionRegistry` 或等价正式类型。
- [x] 6.2 使用 playback、contribution 和 owner identity 作为 registry entry 主合同。
- [x] 6.3 实现 `Sample` 对 Active entry 的创建和更新。
- [x] 6.4 实现 `Complete` 将当前有效 entry 转为 `CompletedHeld`。
- [x] 6.5 实现 contribution `Release` 并确保释放后不会因 Timeline 完成被恢复。
- [x] 6.6 实现非零 duration owner transition，将 source entries 交给 outgoing session。
- [x] 6.7 实现零 duration owner transition，在同一表现批次中原子 retire source entries。
- [x] 6.8 保持未参与 transition 的其它 owner entries 不变。
- [x] 6.9 导出供 LayerRuntime 消费的 active/incoming/outgoing registry snapshot。
- [x] 6.10 导出包含 identity、owner、状态、clip time 和 weight 的只读 debug snapshot。
- [x] 6.11 owner release、pipeline dispose 和 registry dispose 时确定清理全部 entries。
- [x] 6.12 对重复 identity、未知 owner 和非法 lifecycle 顺序报告明确错误，不做超时或默认 owner fallback。

## 7. Logic-to-presentation 命令队列

- [x] 7.1 新增 presentation-owned lifecycle command queue，不复用 transient `CharacterPipelineOutput` 列表作为持久存储。
- [x] 7.2 在每个 logic tick 结束前捕获该 tick 的 Complete、Release 和 owner transition events。
- [x] 7.3 保留单个 render frame 内所有 catch-up logic tick 的 lifecycle commands。
- [x] 7.4 按 local logic tick 和 sequence 保持 command 顺序稳定。
- [x] 7.5 让 `CharacterPipelineFrame.Begin()` 不清理尚未消费的 lifecycle commands。
- [x] 7.6 让 `ClearTransient()` 不清理尚未消费的 lifecycle commands。
- [x] 7.7 PresentationFrame 消费成功后只移除已确认 commands。
- [x] 7.8 pipeline deactivate/dispose 时清理 pending commands。
- [x] 7.9 先复制 pending commands，Registry 完整应用成功后再 acknowledge，失败时不提前清空队列。

## 8. Layer 仲裁与表现 handoff

- [x] 8.1 让 `CharacterAnimationLayerRuntime` 消费 registry snapshot，而不是直接把当帧 submission list 当作完整生命周期。
- [x] 8.2 保持 LayerRuntime 的 layer、priority、override normalization、additive、mask 和 snapshot 规则。
- [x] 8.3 让 `CharacterPresentationStage` 从 registry outgoing/incoming snapshot 创建 transition session。
- [x] 8.4 删除“上一表现帧 plans 是唯一 outgoing 真相”的旧依赖。
- [x] 8.5 非零 duration transition 按 edge curve 淡出 source owner plans 并淡入 target owner plans。
- [x] 8.6 零 duration transition 不生成中间空计划帧。
- [x] 8.7 transition 期间不改变 Timeline status、window、cue、motion 或 SyncFacts。
- [x] 8.8 transition 完成后向 registry 确认可 retire 的 outgoing entries。
- [x] 8.9 让 `AnimancerAnimationPresenter` 只根据最终 visual plans 创建、更新时间和权重。
- [x] 8.10 只有 registry 和 transition 已正式移除的 plan 才允许 Presenter Stop 对应 state。
- [x] 8.11 删除旧 plan cache、隐式 inactive 推断或重复 lifecycle 状态，不保留双主线。

## 9. PresentationFrame 处理顺序

- [x] 9.1 在 PresentationFrame 收集 active Timeline 表现采样。
- [x] 9.2 在 PresentationFrame 收集 terminal pending Timeline 表现采样。
- [x] 9.3 应用按序的 Sample、Complete 和 contribution Release。
- [x] 9.4 应用 owner transition 与 `OwnerReady` events，并在 ready 前保持 handoff pending。
- [x] 9.5 从 registry 生成统一 snapshot。
- [x] 9.6 由 LayerRuntime 生成 layer playback plans。
- [x] 9.7 由 PresentationStage 生成最终 transition visual plans。
- [x] 9.8 由 Animancer adapter 应用最终计划。
- [x] 9.9 完成 registry 与 scheduler 的 handoff/retire 确认。
- [x] 9.10 保持 CameraStage、visual pose 和 frame cleanup 的现有正式顺序。

## 10. Timeline 编辑器预览

- [x] 10.1 为每个 `TimelinePreviewSession` 创建隔离的 contribution registry。
- [x] 10.2 连续预览播放时复用正式 Sample/Release 和 LayerRuntime 规则。
- [x] 10.3 非连续 seek 时重置 preview registry 后从目标时间重建有效 contributions。
- [x] 10.4 preview clip 离开 None 范围时显式 Release，不隐式 Hold。
- [x] 10.5 切换 preview target 时释放旧 session owner 和 Animancer plans。
- [x] 10.6 停止预览或关闭窗口时释放 preview registry。
- [x] 10.7 确认 preview 不读取 runtime registry，不把 lifecycle state 写回 Timeline asset。
- [x] 10.8 删除被 registry 替代的 preview 临时列表生命周期假设，不创建第二套仲裁规则。

## 11. 清理与校验

- [x] 11.1 搜索并删除 Timeline 完成即直接丢弃 presentation record 的旧路径。
- [x] 11.2 搜索并删除 state transition 只在 blend duration 大于 0 时才发布 handoff 的旧路径。
- [x] 11.3 搜索并确认没有新增 TimelinePlayer autonomous playback、Animator fallback、旧 locomotion/action SO 或 Presenter 隐式 Idle。
- [x] 11.4 确认 `ExtraPolationMode=None` 的 clip 空段不会被 registry 自动保持。
- [x] 11.5 确认 Corin 动画资产没有被本 change 自动改写。
- [x] 11.6 清理实现中出现的临时 lifecycle、owner、queue 和 debug 类型命名。
- [x] 11.7 使用 `--disable-build-servers /nr:false /p:UseSharedCompilation=false` 运行 C# 编译校验。
- [x] 11.8 编译结束后立即运行 `dotnet build-server shutdown`。
- [x] 11.9 运行 `openspec validate refactor-animation-contribution-lifecycle --strict --no-interactive`。
- [x] 11.10 确认全部任务真实完成后再将 tasks 更新为 `[x]`。
