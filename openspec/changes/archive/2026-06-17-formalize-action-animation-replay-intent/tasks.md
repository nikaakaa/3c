## 1. Scope Review
- [x] 1.1 读取本 proposal、design 和 spec deltas，确认只实现 Action 动画播放意图身份。
- [x] 1.2 对照 `action-animation-profile` 当前规格，确认现有“连续 Dodge 仍重播同 key”场景被本变更补齐协议语义。
- [x] 1.3 对照 `formalize-animation-playback-rollback-authority`，确认不修改 Locomotion/profile playback clock 设计。
- [x] 1.4 对照 `add-light-attack-combo-action`，确认本变更不提前实现 Attack combo。
- [x] 1.5 使用 GitNexus 对计划修改的符号运行 impact 分析，并记录风险级别。

## 2. Playback Intent Model
- [x] 2.1 审计 `CharacterStateAnimationRequest` 当前字段和构造入口。
- [x] 2.2 定义纯数据 Action playback intent identity 或等价字段。
- [x] 2.3 将 playback intent 接入 `CharacterStateAnimationRequest`。
- [x] 2.4 将 playback intent 接入 `CharacterAnimationPlaybackRequest.FromAction`。
- [x] 2.5 确认新增字段不引用 Unity object、Animancer runtime object、AnimationClip、TransitionAsset 或 Animator。
- [x] 2.6 确认默认无效 intent 不会形成隐藏 fallback 配置。

## 3. Action Lifecycle Integration
- [x] 3.1 审计 `ActionLifecycleRuntime.Tick` 对 accepted action 和 active action 的处理。
- [x] 3.2 在新 accepted action 进入生命周期时创建或确定新的 playback intent。
- [x] 3.3 在同一个 active action 后续帧复用 playback intent。
- [x] 3.4 确认 playback intent 不只依赖当前 frame `SourceStep`。
- [x] 3.5 在 action restore state 中保留或可确定性重建 active playback intent。
- [x] 3.6 确认 action 退出或 clear 时清除 active playback intent。

## 4. Presenter Semantics
- [x] 4.1 审计 `CharacterAnimancerPresenter.PresentAction` 的 same-playback 判断。
- [x] 4.2 将 same-playback 条件改为同 key 且同 playback intent。
- [x] 4.3 确认同 key 不同 playback intent 会重新 `TryPlay` 并重置 normalized time。
- [x] 4.4 确认同 key 同 playback intent 不会重复重启。
- [x] 4.5 确认 `RestorePlaybackProgress` 后同 intent 提交不重启。
- [x] 4.6 确认 `ClearActionPlayback` 会清理 action key、state 和 playback intent。
- [x] 4.7 确认 Presenter 不读取 Action 打断策略、不消费输入、不执行位移。

## 5. Output and Adapter Updates
- [x] 5.1 更新 `IActionAnimationPresenter` 和相关 fake presenter 的请求消费逻辑。
- [x] 5.2 更新 FullBody output runtime 中 action animation request 的转交路径。
- [x] 5.3 更新 runtime blackboard 或 animation snapshot 读取，确保诊断能看到必要的播放身份或 source 信息。
- [x] 5.4 更新现有测试辅助构造器，避免测试默认构造意外表示新播放。
- [x] 5.5 静态检查没有新增 Dodge 专用 presenter、fallback presenter 或第二 animation output path。

## 6. Automated Tests
- [x] 6.1 增加 Presenter 测试：同 key 不同 playback intent 必须重播并归零。
- [x] 6.2 增加 Presenter 测试：同 key 同 playback intent 不重复重启。
- [x] 6.3 增加 Presenter 测试：restore 后同 playback intent 不重启。
- [x] 6.4 增加 Action lifecycle 测试：新 accepted action 产生新 playback intent。
- [x] 6.5 增加 Action lifecycle 测试：active action 后续帧复用 playback intent。
- [x] 6.6 增加 Action lifecycle restore 测试：恢复后 playback intent 保持或可重建。
- [x] 6.7 增加 FullBody/Character frame 测试：连续 Directional Dodge 输出同 key 但不同 playback intent。
- [x] 6.8 增加 FullBody/Character frame 测试：连续 Backstep Dodge 输出同 key 但不同 playback intent。
- [x] 6.9 更新现有 restore 不重启测试，确保它验证的是同一次播放意图。
- [x] 6.10 更新静态边界测试，确认播放意图模型没有 Unity/Animancer runtime 依赖。

## 7. Validation
- [x] 7.1 运行 `dotnet build .\3cDemo\Client\3C_Client\Assembly-CSharp.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [x] 7.2 运行 `dotnet build .\3cDemo\Client\3C_Client\Assembly-CSharp-Editor.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [x] 7.3 运行定向 EditMode 测试：`CharacterAnimancerPresenterTests`。
- [x] 7.4 运行定向 EditMode 测试：`UnifiedCharacterStateMachineTests` 中 Action animation 相关用例。
- [x] 7.5 运行定向 EditMode 测试：新增连续 Dodge playback intent 用例。
- [x] 7.6 运行 OpenSpec 校验：`openspec validate formalize-action-animation-replay-intent --strict --no-interactive`。

## 8. Completion
- [x] 8.1 确认没有修改 Dodge 配置资产、Action Catalog 资产或动画 Profile 资产格式。
- [x] 8.2 确认没有新增未审批 fallback 配置、fallback presenter 或平行播放路径。
- [x] 8.3 确认所有任务完成后再统一将 checklist 标记为已完成。
