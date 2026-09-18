## Why

当前 locomotion 的基础移动与 PoseStateMachine 已经可用。现行 `character-presentation-interpolation` 已经声明了高层装配边界，但 `CharacterLocomotionPresentationPlan` 只保存时钟、Body source 和被误命名的 trajectory mode；角色工厂仍让 `CharacterClipPlayerClockSource` 参与时钟选择。这样同一个角色的 Session 装配、Body Profile 和 Clip 仍可能各自表达策略。

本 change 要扩展既有 plan，而不是新增第二种 plan；将时钟、Body source、事实 lineage、视觉纠正和失败语义收口为一条 Session Composition 到表现域的链路。这样同一份 locomotion 资产可以被 Local、Prediction、Server Authority、Rollback 与 Replay 复用，同时不把 Network Model 配置写入 Graph、Timeline 或单个 Clip。

## What Changes

- 原地扩展现有 `CharacterLocomotionPresentationPlan`，不新增同义类型、不保留 plan adapter；plan 只拥有 identity、`ClockMode` 和 `BodySource`，严格模式的 lineage 由正式 Presentation Fact 提供。
- Session Composition 唯一创建已准备的 locomotion presentation binding。Network Model 只提供正式 source capability 和事实，不拥有 Presentation playback；Local 也不被描述为 Network Model。
- Locomotion 只允许 `FreeRun` 和 `CommittedMovement` 两种时钟模式。`CommittedFollowPresentationClockPolicy` 只服务有限 Action Timeline，禁止进入 locomotion。
- 将 `CharacterVisualTrajectoryMode` 重命名为 `CharacterBodyCorrectionMode`，由 `CharacterBodyPresentationProfile` 唯一拥有 `Direct` / `BoundedCorrection` 及其数值。Reset 继续是 stream 事件和 reset reason，不是第三种 correction mode。
- 删除 `CharacterClipPlayerClockSource` 的时钟选择职责；locomotion 参与关系由 Pose node 表达，具体 policy 只由已准备 plan 注入。
- 为 Local、Prediction、Server Authority、Rollback、Replay 明确正式 `CommittedStream` / `SelectedStream` 映射。缺少所需事实、Profile 或 lineage 时 preparation 返回稳定错误码，不回退到另一种 source、profile 或时钟。
- 补充只读诊断，能够看到 plan identity、时钟、Body source、correction mode、事实 lineage、reset 和失败原因。
- 不修改 Timeline Runtime、TreeClip 生命周期、Gameplay Timeline 逻辑推进、locomotion 模拟规则或战斗窗口语义。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `character-presentation-interpolation`: 将已有的高层 locomotion 装配要求扩展为唯一 plan、Body correction、严格事实 lineage、Composition builder、诊断和失败合同。

## Impact

- 角色表现装配：`CharacterPresentationDomainRuntimeFactory`、`CharacterBodyPresentationRuntime`、`CharacterBodyPresentationProfile`、Pose Clip Player policy factory 和 Presentation preparation。
- Composition 边界：Local、Fixed、Rollback 与 Server Authority 的 Session Composition 创建同一类已准备 binding；公共 Character runtime 不识别具体 Network Model 类型。
- Authoring 与资产：现有 locomotion Pose Graph、Locomotion Group 与 Clip 资源保持复用；删除 `CharacterClipPlayerClockSource` 的策略字段，并把 `CharacterVisualTrajectoryMode` 迁移为 `CharacterBodyCorrectionMode`。
- Diagnostics：增加 plan identity、Body source、correction mode、logic fact lineage、reset 和稳定 failure code 的只读观察字段。
- 不涉及 Timeline、TreeClip、Gameplay ability execution、Movement solver、combat window 或新的网络协议字段。
