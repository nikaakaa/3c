# Design: Motion 语义和 Modifier 链路

## 背景

当前项目已经确定 BTSMTL 是 authoring 主线。状态机负责状态结构，状态行为进入 `SubTree` 或 `StateBehaviorSubTree`，Timeline 通过 `TimelineNode` 请求播放，再由 `CharacterBTSMTLPhase` 内部 `TimelinePlaybackScheduler` 统一推进和采样。

本变更只处理运动层语义，不再定义 ActionModule 或节点 action 身份。Action/Ability 身份由 `replace-action-module-with-ability-runtime` 的 `AbilityAsset + AbilityRuntime` 负责。

## 目标模型

```text
MotionContribution
-> MotionResolver 生成 Raw MotionIntent
-> MotionModifierPipeline 生成 Final MotionIntent
-> CharacterController.Move
-> MotionResult
```

业务含义：

- `MotionContribution` 是来源贡献，不决定最终位移。
- `MotionIntent` 是 Move 前最终运动意图。
- `MotionModifier` 是 Move 前的正式修正层。
- `MotionResult` 是 Move 后的实际结果。

## Timeline 和外部影响

Timeline 适合配置时间编排：

- 动画 clip
- root motion 曲线引用
- motion warp window
- attack/cancel/iframe window
- VFX/SFX/Camera cue

Timeline 不适合配置实时外部事实：

- 当前 target 是谁
- 目标当前在哪里
- server correction 是多少
- 平台速度是多少
- 被击退方向和强度是多少

这些应由 `CharacterGraphContext`、combat context、network input 或 world context 提供，MotionStage 读取这些事实后执行 modifier。

## Motion 命名清理

| 当前语义 | 新语义 | 说明 |
| --- | --- | --- |
| `MotionProposal` | `MotionIntent` | Move 前最终运动意图，不是请求也不是建议 |
| `MotionContribution` | 保留 | 上游来源贡献，例如 root motion、输入移动、击退 |
| `MotionResolver` | 保留 | 将贡献合成 Raw MotionIntent |
| `MotionModifier` | 新增 | Move 前运动修正，例如 motion warp、correction smoothing |
| `MotionResult` | 保留 | CharacterController.Move 后的实际结果 |

`Request` 只用于未被状态接受的输入或动作请求。`Command` 只用于输入/网络命令。`Intent` 用于玩法已经接受、等待 MotionStage 结算的运动意图。

## MotionModifier 顺序

第一阶段不做可任意注册的插件系统。MotionStage 内部使用固定顺序，避免调试时找不到谁改了运动：

```text
Resolve contributions
Apply timeline-scoped modifiers
Apply ability/combat modifiers
Apply world modifiers
Apply network correction modifiers
Move
Write MotionResult
```

第一阶段可以只实现 `MotionWarpModifier`，但数据结构要允许后续接受击位移、平台速度和网络校正。

## MotionWarp 职责

Timeline 输出：

- warp window
- target key
- position/yaw weight
- max correction
- curve 或 normalized window

Runtime context 输出：

- target transform 或 target position
- 当前 authority mode
- server correction 或 combat result

MotionStage 执行：

- 根据当前 MotionIntent、窗口和 target 计算修正
- 输出新的 MotionIntent
- 不直接在 Timeline、Graph 或 Animator 中移动角色

## 实现说明

- 不恢复 BBB 的 `PlayerSO`、`MotionClipData`、`WarpedMotionData` 或代码状态类链路；本实现只吸收 BBB “动画后、Move 前统一修正”的时序思想。
- `MotionWarpTrack` 只输出 motion warp window；目标必须通过 `CharacterGraphContext` 的 fact 按 target key 提供。target 缺失时 `MotionWarpModifier` 正式 no-op，不做场景搜索或隐藏 fallback。
- `MotionResult.RequestedDisplacement` 指向 modifier 后的最终 `MotionIntent.Displacement`，也就是真正提交给 `CharacterController.Move` 的请求位移。
