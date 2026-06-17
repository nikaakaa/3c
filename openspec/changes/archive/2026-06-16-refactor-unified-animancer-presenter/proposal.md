# Change: 收口统一 Animancer 表现入口

## Why
当前角色主线已经收口到统一 FullBody 状态机，但动画表现层仍保留 `BasicLocomotionAnimancerPresenter` 和 `ActionAnimationAnimancerPresenter` 两个正式 Animancer 播放组件。它们共同持有同一个角色视觉根上的 Animancer 播放状态、播放进度和 root motion policy，容易形成新的表现分裂路径。

## What Changes
- **BREAKING**：当前角色正式运行时只能存在一个 FullBody base layer Animancer Presenter，统一消费 Locomotion 和 Action 的动画播放请求。
- **BREAKING**：`BasicLocomotionAnimancerPresenter` 和 `ActionAnimationAnimancerPresenter` 不再作为两个正式运行时播放组件并存；若保留旧类型，只能作为迁移桥或测试兼容层，不能各自持有正式播放权威。
- 将 Locomotion 的 `phase + gait + alias` 和 Action 的 `animation key` 归一成统一播放请求，由同一个 Presenter 执行 Animancer 播放、clear、restore 和进度暴露。
- 保留现有配置分工：基础移动配置继续提供 Locomotion alias/退出策略/motion profile，动作动画配置继续提供 Action key 到表现资源的映射。
- 保留状态机纯数据边界：状态机只输出动画语义 key / timeline binding key 或等价请求，不直接调用 Animancer。
- 不新增 fallback 配置，不新增第二个 Animator/Animancer 播放路径，不绕过当前 Character frame pipeline / output applier。

## Impact
- Affected specs:
  - `fullbody-action-framework`
  - `basic-locomotion-animation`
  - `action-animation-profile`
- Affected code:
  - `Assets/Scripts/Character/Animation/Runtime/BasicLocomotionAnimancerPresenter.cs`
  - `Assets/Scripts/Character/Animation/Runtime/ActionAnimationAnimancerPresenter.cs`
  - `Assets/Scripts/Character/Animation/Contracts/IActionAnimationPresenter.cs`
  - `Assets/Scripts/Character/Animation/Model/ILocomotionAnimationPlaybackProgressController.cs`
  - `Assets/Scripts/Character/Movement/Runtime/PlayerLocomotionController.cs`
  - `Assets/Scripts/Character/Movement/Runtime/LocomotionRuntimeReferenceResolver.cs`
  - `Assets/Scripts/Character/Action/FullBody/Runtime/PlayerFullBodyActionController.cs`
  - `Assets/Prefabs/Character/可琳.prefab`
  - `Assets/Prefabs/Character/可琳_Humanoid.prefab`
  - 相关 EditMode tests
- Related active changes:
  - `refactor-character-hierarchical-state-runtime`：继续保持状态机 runtime 不调用 animation presenter。
  - `formalize-animation-playback-rollback-authority`：统一 Presenter 必须承接 Locomotion 和 Action 播放进度 restore 语义。
  - `refactor-character-runtime-adapter-layers`：本变更先消除两个正式 Animancer 播放组件并存，再继续拆内部模块。
