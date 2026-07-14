# Change: 增加 Root Motion 曲线烘焙链路

## Why

当前角色管线已经有 `MotionIntent`、`MotionContribution` 和 `CharacterMotionStage`，Timeline 动画采样已经收口到 BT/SM/TL 逻辑阶段内部的 `TimelinePlaybackScheduler`。但动画自带 root motion 还没有正式数据链路：如果直接用 Animator/Animancer 的 root motion 驱动 Transform，就会绕过 `CharacterMotionStage`；如果恢复 BBB 的 `PlayerSO -> MotionClipData/WarpedMotionData` 递归写回链路，又会重新产生旧配置数据源。

本变更要补的是一个干净的派生数据工具：从 `AnimationClip` 离线采样 root 本地位移和朝向，生成可持久化的曲线资产；运行时按当前动画时间求出本帧 delta，提交给角色 motion 管线仲裁和应用。

## What Changes

- 新增 `character-root-motion-curves` 能力，定义 Root Motion 曲线资产、编辑器烘焙器、运行时求值器和管线提交边界。
- 生成独立 `RootMotionCurveAsset`，保存源动画、时长、采样率、累计本地位置 XYZ 曲线和累计 yaw 曲线。
- 新增编辑器烘焙工具，参考 BBB 的 Animator 采样思路，但不复制 BBB 的旧 `PlayerSO` 扫描和旧数据写回方式。
- 新增运行时求值器，从累计曲线按 `previousTime -> currentTime` 求 delta position / delta yaw。
- Timeline 或 Action 创作数据显式引用 Root Motion 曲线资产，并在播放时提交正式 motion 数据。
- 角色位移仍由 `CharacterMotionStage` 或同一 motion resolver 应用，禁止 root motion 采样器、Timeline 轨道或动画表现层直接改 Transform。

## Chosen Approach

采用“离线烘焙累计曲线，运行时求 delta 后提交管线”的方案：

```text
AnimationClip + 采样 Prefab
-> RootMotionCurveBaker
-> RootMotionCurveAsset
-> Timeline/Action 显式引用
-> RootMotionCurveEvaluator 按播放时间求 delta
-> MotionContribution / MotionIntent
-> CharacterMotionStage 应用 CharacterController.Move
```

业务取舍：

- 不直接依赖 Animator/Animancer root motion，因为它会绕过动作状态、网络预测、碰撞修正和调试输出。
- 不把 root motion 写回旧 BBB `MotionClipData`，因为那会恢复旧 locomotion/action 配置路径。
- 不使用命名约定自动查找曲线资产，因为这会形成隐式 fallback。Timeline/Action 必须显式引用正式资产。
- 第一版保存累计本地位移和累计 yaw，而不是速度曲线。累计曲线是更接近源数据的表达，seek、拖时间轴预览、低帧率和变速播放时更不容易产生积分漂移。
- 第一版不保存完整 quaternion 曲线。第三人称动作管线当前主要需要水平位移和朝向旋转；pitch/roll 不应默认驱动 CharacterController。

## Non-Goals

- 不恢复 BBB 的 `PlayerSO`、`MotionClipData`、`WarpedMotionData`、`FootPhase` 或旧批量配置扫描。
- 不实现 foot phase、攻击窗口、取消窗口、warped motion 点位或 IK 权重。
- 不实现完整 Animancer 表现层接入。
- 不做网络同步、服务端裁决或 rollback。
- 不新增测试。

## Current Reality

- `CharacterMotionStage` 当前消费 `frame.Output.StrictGameplay.MotionIntent` 并调用 `CharacterController.Move`。
- `MotionIntent` 表达 Move 前最终运动意图。
- `TimelinePlaybackScheduler` 当前已经采样 `AnimationTrack`，并把动画贡献写入 `frame.Output.Presentation.AnimationContributions`。
- BBB 参考代码在 `Assets/Ref/BBB/Editor/RootMotionExtractor.cs` 和 `Assets/Ref/BBB/Editor/WarpedMotionExtractor.cs`，其中旧工具会扫描 `PlayerSO` 并写回旧数据，本项目不能沿用这条数据源。
- `openspec/specs/btsmtl-runnable-timeline-node` 当前仍保留旧 TimelineNode 直接播放语义；`refactor-timeline-animation-pipeline-authority` 已完成但未归档到 current spec。实现本变更前应确保 Timeline 播放权以角色管线为准，不能按旧 current spec 恢复直接播放。

## Impact

- 需要新增正式 runtime 类型和 editor baker 类型。
- 需要扩展 Timeline/Action 的动画片段数据，让 root motion 曲线资产成为显式引用。
- 需要让 `TimelinePlaybackScheduler` 在采样动画时间时同步采样 root motion 曲线。
- `StrictGameplayOutput` 使用 motion contribution 收集，再由 resolver 生成最终 `MotionIntent`。
- 需要清理任何试图从 BBB 旧配置、命名约定或 Animator root motion 直接应用位移的路径。

## Spec Comparison

- 与 `btsmtl-runnable-timeline-node` 当前 spec 的潜在冲突：当前 spec 仍描述 `TimelineNode` 直接实例化和评估 Timeline。本变更依赖管线权威版本，不能让 root motion 采样挂在旧的节点直播路径上。
- 与 `btsmtl-componentized-node-authoring` 一致：曲线资产应通过节点模块或 Timeline clip 字段显式引用，不新增 Workbench 端口协议。
- 与 `btsmtl-sm-node-authoring` 一致：root motion 不是新的状态节点；状态机只通过状态行为播放 Timeline/Action，再由管线采样。
- 与项目清理规则一致：旧 BBB 数据结构只作为参考，不进入正式运行时。
