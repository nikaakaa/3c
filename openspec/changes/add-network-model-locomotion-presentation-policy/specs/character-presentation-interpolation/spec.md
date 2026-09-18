## MODIFIED Requirements

### Requirement: Locomotion表现策略必须由表现域装配

每个角色表现实例 MUST由 Session Composition 创建唯一的 `CharacterLocomotionPresentationPlan` 与正式 `CharacterBodyPresentationProfile`，并通过 preparation 产生 `PreparedCharacterLocomotionPresentationBinding` 后交给 `CharacterPresentationDomainRuntimeFactory`。Local Composition MUST使用同一入口，但 MUST NOT被声明为 Network Model。Network Model Source MUST只提供正式 capability 和 Presentation Fact，不得创建 plan、直接推进播放或直接写入 VisualRoot。

`CharacterLocomotionPresentationPlan` MUST是唯一的模型无关 plan，不得新增同义 plan 或 adapter。它 MUST不可变地声明 plan identity、`ClockMode` 和 `BodySource`；`ClockMode` 只允许 `FreeRun` 与 `CommittedMovement`，`BodySource` 只允许 `CommittedStream` 与 `SelectedStream`。Graph、Pose Graph、Timeline、Clip、通用 Character runtime 与 Network Model Source MUST不保存 ModelId、Endpoint、Transport、Prediction、Rollback 或 Replay 选择。

`CharacterBodyPresentationProfile` MUST是 `CharacterBodyCorrectionMode` 与全部 correction 参数的唯一 owner。`CharacterBodyCorrectionMode` 只允许 `Direct` 与 `BoundedCorrection`，并替换旧 `CharacterVisualTrajectoryMode` / `TrajectoryMode` 命名。`CharacterBodyPresentationResetReason` MUST继续表示已提交 stream 事件，不得被作为第三种 correction mode 或 plan 字段。正常 reset MUST只重建 Body target anchor；只有同一 committed Fact 的 pose discontinuity identity 改变时，locomotion Pose Player 才 MAY重置。

Locomotion Pose node 只 MAY声明自己是否属于 locomotion。`CharacterClipPlayerClockSource` MUST从 authoring、compiled descriptor 和 runtime 删除；单个 Clip MUST不决定时钟、Body source、correction mode 或当前角色 / Session 的网络策略。全部 locomotion 节点 MUST从同一 prepared binding 接收 policy。

`FreeRun` MUST只使用 `FreeRunPresentationClockPolicy` 并按 PresentationFrame delta 推进。`CommittedMovement` MUST只使用 `CommittedMovementPresentationClockPolicy` 并读取正式 `CharacterPresentationFactFrame.MovementPlaybackClock`。`CommittedFollowPresentationClockPolicy` 只服务有限 Action Timeline 的 committed Action sample 投影，MUST NOT被 locomotion 使用。`CommittedMovementPlaybackClock` MUST是 Simulation Tick 的派生事实，不得成为第三个时间域、独立 Tick 或 history owner。

当 `ClockMode` 是 `CommittedMovement` 时，Presentation Fact MUST同时携带 plan identity、Body source、movement playback clock identity、movement segment identity 和 lineage generation，并且它们 MUST与 prepared binding 相同。Preparation 或事实提交不满足此合同 MUST返回稳定 `LocomotionPresentationFailureCode`：`MissingPlan`、`InvalidPlan`、`MissingBodyProfile`、`InvalidBodyProfile`、`BodySourceUnavailable`、`MissingMovementFact`、`PlanIdentityMismatch`、`BodySourceMismatch`、`LineageGenerationMismatch` 或 `MovementSegmentMismatch`。失败 MUST不回退到 FreeRun、另一条 Body stream 或另一份 Profile。

Session Composition 的正式 builder 映射 MUST如下：

| Composition | ClockMode | BodySource |
|---|---|---|
| Local | FreeRun | CommittedStream |
| Prediction owner | FreeRun | SelectedStream |
| Server Authority remote | FreeRun | SelectedStream |
| Rollback local | FreeRun | CommittedStream |
| Rollback remote | FreeRun | SelectedStream |
| Replay / 严格重演 | CommittedMovement | Composition 显式指定，并与 Fact lineage 相同 |

Body correction 的具体 mode 和数值 MUST始终来自该 Composition 显式选择的 Body Profile，不得由 ClockMode、Actor 名称、Camera ownership、Network Model 名称或 BodySource 推断。Body correction MUST不修改逻辑 Body、Simulation State、Timeline state 或 locomotion 逻辑事实。

#### Scenario: 同一 locomotion 图复用多个 Network Model

- **WHEN** Local、Prediction owner、Server Authority remote、Rollback 或 Replay Composition 复用同一 Pose Graph
- **THEN** Graph、Clip 和资源内容 MUST保持不变
- **AND** 每个实例 MUST只通过其 prepared binding 取得 locomotion policy
- **AND** PresentationFrame MUST继续只推进表现状态，不创建 SimulationTick 或修改 Network Model 状态

#### Scenario: 连续revision重新定向

- **WHEN** 上一次 visual correction 尚未 settle 又收到新 branch revision
- **THEN** Follower MUST保持当前 visible pose 与 velocity 连续
- **AND** MUST以新 target 重新计算当前相对误差
- **AND** MUST不叠加另一条固定时长 correction 尾巴

#### Scenario: 普通本地角色自由播放

- **WHEN** Local Composition 创建角色
- **THEN** binding MUST使用 `FreeRun`、`CommittedStream` 和显式 Local Body Profile
- **AND** Idle、Walk、Run、Start、Stop 与 Turn MUST只按 PresentationFrame delta 连续播放
- **AND** runtime MUST不读取 movement playback history 或创建第二个播放器

#### Scenario: 严格重演缺少 lineage

- **WHEN** Replay / 严格重演的 binding 选择 `CommittedMovement`，但 Fact 缺少 movement segment identity 或 generation 不匹配
- **THEN** preparation 或事实提交 MUST返回 `MissingMovementFact`、`LineageGenerationMismatch` 或 `MovementSegmentMismatch`
- **AND** runtime MUST不自动改用 FreeRun、CommittedStream、SelectedStream 或另一份 Profile

#### Scenario: Body 平滑纠正但 locomotion 自由播放

- **WHEN** binding 使用 `FreeRun`，Body Profile 显式选择 `BoundedCorrection`
- **THEN** VisualRoot MAY按该 Profile 收敛到新 Body target
- **AND** locomotion 动画 MUST继续按表现时钟推进
- **AND** Body correction MUST不重置 locomotion 播放时间

#### Scenario: Selected Stream显式重置

- **WHEN** Model Egress 提交 `SelectedStreamReset`，且 Fact 的 pose discontinuity identity 未改变
- **THEN** Body Presentation MUST按正式 Profile 重建 target anchor
- **AND** locomotion Clock 与 Pose Player MUST保持连续
- **AND** Network adapter MUST不直接写入 VisualRoot

#### Scenario: 有限 Action 不接管 locomotion 时钟

- **WHEN** 同一表现帧同时存在有限 Action Timeline 和 locomotion Pose node
- **THEN** Action Timeline MAY使用 `CommittedFollowPresentationClockPolicy`
- **AND** locomotion MUST继续只使用 prepared binding 指定的 FreeRun 或 CommittedMovement policy

#### Scenario: 排查远端 locomotion 表现

- **WHEN** 远端角色出现动画阶段、Body target 或纠正状态异常
- **THEN** 诊断 MUST报告 plan identity、ClockMode、BodySource、CorrectionMode、Profile identity、Fact lineage、reset sequence/reason 和 failure code
- **AND** 诊断 MUST只读，不得参与策略选择、逻辑模拟、Timeline 推进或 Network Model 状态修改
