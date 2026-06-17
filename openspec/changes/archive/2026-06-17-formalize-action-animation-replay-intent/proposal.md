# Change: 正式化 Action 动画重播意图

## Why

当前连续 Dodge 如果进入同一个 `Action.Dodge.Directional` 或 `Action.Dodge.Backstep` 动画 key，动作生命周期已经接受了新的动作实例，但 Animancer Presenter 只根据相同 key 判断为同一次播放，导致第二次 Dodge 不重播动画。

现有 `action-animation-profile` 已经要求“连续 Dodge 仍重播同 key”，但现有协议没有表达“这是新的动作播放意图”与“这是 restore 后同一次播放继续提交”的差异。该变更补齐 Action 动画请求和 Presenter 之间的播放实例语义。

## What Changes

- 将 Action 动画稳定语义 key 与 Action 动画播放意图身份分离。
- 新的 accepted Action 实例即使输出相同动画 key，也必须生成新的播放意图，使 Presenter 重播动画。
- 同一个 active Action 在后续帧重复提交相同播放意图时，Presenter 必须保持幂等，不反复重启。
- rollback restore 或等价恢复路径必须能表达“恢复同一次播放”，不得因为同 key 请求再次到达而归零。
- Presenter 继续只消费纯数据播放请求，不参与 Action 仲裁、输入消费、位移或配置 fallback。

## Non-Goals

- 不修改 Dodge 动作配置、Action Catalog 或动作动画 Profile 资产格式。
- 不新增第二套 Action animation presenter、第二条 frame pipeline 或专用 Dodge 播放路径。
- 不改变 Locomotion `TickSampledMotion` 的回滚播放时钟设计；该内容仍归属 `formalize-animation-playback-rollback-authority`。
- 不接入 Attack combo、Jump、HitReact 或多层 UpperBody 表现。
- 不引入 Animator/Animancer runtime object 作为可回滚状态。

## Impact

- Affected specs:
  - `action-animation-profile`
  - `fullbody-action-framework`
- Affected code:
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Action/Model/ActionLifecycleFrame.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/StateMachine/Model/CharacterStateMachineRuntimeTypes.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Animation/Model/CharacterAnimationPlaybackModels.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Animation/Runtime/CharacterAnimancerPresenter.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Animation/Contracts/*.cs`
  - `3cDemo/Client/3C_Client/Assets/Tests/Editor/CharacterAnimancerPresenterTests.cs`
  - `3cDemo/Client/3C_Client/Assets/Tests/Editor/UnifiedCharacterStateMachineTests.cs`
  - `3cDemo/Client/3C_Client/Assets/Tests/Editor/Simulation/FullBodyRollbackReplayTests.cs`
- Related active changes:
  - `formalize-animation-playback-rollback-authority`：处理 Locomotion/profile-driven playback clock 的 restore 权威，本变更只处理 Action 动画播放意图身份。
  - `add-character-action-catalog`：提供正式 Action 定义入口，本变更不改变 catalog 配置形态。
  - `add-light-attack-combo-action`：后续 Attack 应复用本变更的播放意图语义，不新增独立播放分支。

## Current Gaps

- `CharacterStateAnimationRequest` 当前只有 `ActionAnimationKey` 和 `SourceStep`，没有稳定的 Action 播放实例身份。
- `ActionLifecycleRuntime` 在 accepted action 到来时会重置动作生命周期，但输出动画请求时没有把“新动作实例”传给 Presenter。
- `CharacterAnimancerPresenter` 的 Action same-playback 判断只比较 key 和当前 state，无法区分连续同 key 新动作与 restore 后同一次播放。
- 现有测试覆盖了 restore 后同 key 不重启，但缺少“同 key 不同动作实例必须重播”的回归测试。

## Clarifications

- 暂无阻塞性需求歧义。本提案按“所有 Action 动画通用，Dodge 是第一验收场景”规划。
