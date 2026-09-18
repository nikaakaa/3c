## Context

现有 locomotion Pose 资产、PoseStateMachine 和 Body Presentation 已经分别存在。`CharacterLocomotionPresentationPlan` 也已经存在，但它把 `CharacterVisualTrajectoryMode` 复制进 plan，而同名 mode 实际表达的是 `Direct` / `BoundedCorrection`；角色工厂还读取 `CharacterClipPlayerClockSource` 选择 locomotion 时钟。当前不是缺一个新 plan，而是已有 plan、Body Profile、Clip descriptor 和 host builder 的责任没有收口。

`add-timeline-clock-domain-config` 已经定义了三种播放器策略。locomotion 只消费其中的 `FreeRunPresentationClockPolicy` 与 `CommittedMovementPresentationClockPolicy`；`CommittedFollowPresentationClockPolicy` 是有限 Action Timeline 的 committed sample 投影器，不属于 locomotion。

当前必须保持的事实：运行时只有 Simulation Tick 与 Presentation Frame 两个时间域；逻辑移动进度是 Simulation Tick 产生的派生事实，不是第三个时钟；Body Presentation 不修改 Simulation State；locomotion Clip 不保存 Network Model 配置；严格模式缺事实时不能静默退回 FreeRun。

## Goals / Non-Goals

**Goals:**

- 原地扩展 `CharacterLocomotionPresentationPlan`，不再引入 `LocomotionPresentationPlan` 同义类型或 adapter。
- 让 Session Composition 先得到唯一 prepared binding，再由 `CharacterPresentationDomainRuntimeFactory` 装配 Body Presentation、locomotion clock policy 和 Pose Clip Player。
- 让普通 Local、Prediction、Server Authority 和普通 Rollback 保持 `FreeRun`；Replay 或业务显式严格重演才使用 `CommittedMovement`。
- 让 `CharacterBodyPresentationProfile` 唯一拥有 `Direct` / `BoundedCorrection` 和数值；Reset 是 Body stream 更新事件，不改变 locomotion 时间，除非事实明确带来 pose discontinuity。
- 把 plan、Body source、correction mode、事实 lineage、reset 和失败原因发布到只读诊断。

**Non-Goals:**

- 不重写 Locomotion Simulation、KCC、PoseStateMachine、AnimationClip、Blend Space 或 Motion Matching。
- 不修改 Timeline、TreeClip、Gameplay Window、Action lifecycle 或任何网络协议字段。
- 不让 Character runtime 识别具体 Network Model 类型，不在 Clip、Graph 或 Profile 内保存 Endpoint、Transport、Prediction 或 Rollback 选择。
- 不创建第二个 Pose Player、第二个 Body history、第三个时钟或 fallback 配置。

## Decisions

### 1. 原地扩展既有 plan

保留唯一 `CharacterLocomotionPresentationPlan`，将其收敛为不可变的：

- `PlanIdentity`；
- `ClockMode`：`FreeRun` 或 `CommittedMovement`；
- `BodySource`：`CommittedStream` 或 `SelectedStream`。

`ClockMode == CommittedMovement` 表示必须消费正式 `CharacterPresentationFactFrame` 的 locomotion lineage；`FreeRun` 明确不需要该事实。plan 不再保存 `TrajectoryMode`、`CorrectionMode`、Endpoint 或 ModelId。已有 `CharacterLocomotionPresentationPlans` 从 Presentation 程序集迁出并删除，由 Session Composition 的 builder 生成 plan。

业务收益是角色级策略只有一个输入，替换 Walk、Run 或 Turn 资源不会改变网络表现。代价是 Composition 必须明确给出 plan，但这正是业务模型选择发生的位置。

### 2. Session Composition 唯一拥有 builder

Session Composition 创建 plan、选择正式 `CharacterBodyPresentationProfile`，并通过 `CharacterLocomotionPresentationPreparation` 得到 `PreparedCharacterLocomotionPresentationBinding`。Factory 只接收这个已准备 binding，不再分别猜测 Network Model、Body source、Profile 或 Clip clock source。

Network Model Source 只通过正式 capability 和 Presentation Fact 提供它能支持的 Body stream、movement fact 和 lineage。它不直接创建 plan，不直接更新 VisualRoot，也不拥有 Presentation playback。Local 使用相同 Composition 边界，但绝不被写成 Network Model。

业务收益是所有角色创建路径都有相同的错误和装配入口。代价是每种 Composition 需要声明自己的绑定，但不会再产生 Host 内隐藏分支。

### 3. 时钟只由 plan 注入

`FreeRun` 只注入 `FreeRunPresentationClockPolicy`，按 PresentationFrame delta 推进；`CommittedMovement` 只注入 `CommittedMovementPresentationClockPolicy`，读取 `CharacterPresentationFactFrame.MovementPlaybackClock`。`CommittedMovementPlaybackClock` 是 Simulation Tick 的派生事实，不拥有 Tick、history 或独立循环。

`CommittedFollowPresentationClockPolicy` 只服务有限 Action Timeline 的 committed Action sample 投影，不能被任一 locomotion Pose node、Clip Player 或 plan 使用。严格 locomotion 不得借用它的 FreeRun 分支。

`CharacterClipPlayerClockSource` 从 compiled descriptor 和 authoring 资产删除。Pose node 只声明自己是否属于 locomotion；Factory 按 prepared plan 为全部 locomotion 节点注入同一 policy。

业务收益是普通角色不为严格重演付出 history/projector 成本，严格重演也不会悄悄变成自由播放。代价是旧资产要一次性迁移，换来没有 Clip 级策略残留。

### 4. Body correction 只有 Profile 一个 owner

`CharacterVisualTrajectoryMode` 重命名为 `CharacterBodyCorrectionMode`，值只有 `Direct` 和 `BoundedCorrection`。`CharacterBodyPresentationProfile` 是 correction mode 与半衰期、误差上限、收敛阈值的唯一 owner；plan 不复制该字段，Factory 不再比较两份 mode。

`CharacterBodyPresentationResetReason` 继续表示 Initialization、CommittedBranchReplacement 或 SelectedStreamReset 等已提交 stream 事件。它不是 correction mode：正常 reset 只重建 Body target anchor；只有同一 committed Fact 的 pose discontinuity identity 改变时，Pose Player 才重置。

业务收益是 Body 平滑和动画时间可以独立调节，策划调整纠正参数不会意外改动重演时钟。代价是 Profile 选择必须在 Composition 中显式完成，不能依赖 plan 默认值。

### 5. 严格事实必须与 plan 同 lineage

当 `ClockMode` 是 `CommittedMovement` 时，Presentation Fact 必须同时提供 `PlanIdentity`、`BodySource`、`MovementPlaybackClock` identity、movement segment identity 和 lineage generation。Preparation 和每次事实提交都验证它们与 prepared binding 相同。

Preparation 返回稳定的 `LocomotionPresentationFailureCode`：`MissingPlan`、`InvalidPlan`、`MissingBodyProfile`、`InvalidBodyProfile`、`BodySourceUnavailable`、`MissingMovementFact`、`PlanIdentityMismatch`、`BodySourceMismatch`、`LineageGenerationMismatch`、`MovementSegmentMismatch`。失败不会换用 FreeRun、另一条 stream 或另一份 Profile；Factory 只接收 Preparation 成功的 binding。

业务收益是 Replay、观战和回滚重演要么可复现，要么明确失败。代价是严格模式的输入合同更严格，但不把错误伪装成可播放的普通角色。

### 6. Composition 映射是正式配置

| Composition | ClockMode | BodySource | Correction owner |
|---|---|---|---|
| Local | FreeRun | CommittedStream | Local Body Profile，正式选择 Direct 或 BoundedCorrection |
| Prediction owner | FreeRun | SelectedStream | Prediction Body Profile，只有显式选择才使用 BoundedCorrection |
| Server Authority remote | FreeRun | SelectedStream | Remote Body Profile |
| Rollback local | FreeRun | CommittedStream | Rollback local Body Profile |
| Rollback remote | FreeRun | SelectedStream | Rollback remote Body Profile |
| Replay / 严格重演 | CommittedMovement | Composition 显式指定 | Replay Body Profile，且必须与 movement fact 同 lineage |

这张表是 Session Composition 的唯一 builder 合同，不写入 Pose Graph、Clip 或 Network Model 枚举。Replay 的 BodySource 不设默认值，因为不同 replay source 的事实 provenance 不同。

### 7. 诊断只读发布，不参与选择

表现域在 preparation、创建和每次提交后发布 plan identity、ClockMode、Body source、correction mode、Profile identity、logic fact lineage、movement segment identity、reset sequence/reason 和 failure code。诊断只读取这些已提交事实，不推断策略、不重新采样 Body、不触发纠正。

## Risks / Trade-offs

- [普通角色误用严格跟随] → Composition 必须显式选 `CommittedMovement`，默认 builder 只选 `FreeRun`。
- [远端 Body 纠正造成动画抖动] → correction 只由 Profile 收敛 VisualRoot；reset 不等于 pose discontinuity。
- [严格模式缺少 movement fact] → Preparation 返回稳定错误码，不选择任何替代 mode 或 source。
- [Host 各自继续组装 plan] → 删除 Presentation 程序集内的静态 plan builder，统一迁到 Session Composition。
- [旧 Clip 字段保留隐藏选择] → 一次性迁移并删除 `CharacterClipPlayerClockSource`，不保留兼容读取。

## Migration Plan

1. 扩展既有 `CharacterLocomotionPresentationPlan`，引入 preparation result、binding 和稳定 failure code。
2. 将 `CharacterVisualTrajectoryMode` 迁移为 `CharacterBodyCorrectionMode`，删除 plan 内的重复 correction 字段与 Factory 比对。
3. 让 Factory 只消费 prepared binding，并让 locomotion 只在 FreeRun 与 CommittedMovement 之间选择。
4. 删除 `CharacterClipPlayerClockSource`，迁移所有 Pose Graph、authoring 和编译 descriptor。
5. 将 Local、Prediction、Server Authority、Rollback 和 Replay builder 迁入对应 Session Composition，并接通严格模式所需的 Fact lineage。
6. 接通诊断，删除旧 builder、旧字段和隐式 fallback，再更新 current spec。

不保留兼容 fallback；迁移失败的组合必须在表现域创建阶段明确失败并修正装配配置。
