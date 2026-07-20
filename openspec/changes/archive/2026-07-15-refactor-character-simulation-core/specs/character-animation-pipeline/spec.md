# character-animation-pipeline Specification

## ADDED Requirements

### Requirement: Compiled Timeline Operation 必须是 Gameplay Timeline 唯一权威

CharacterSimulationProgram Timeline operations MUST唯一拥有 Timeline request、logic time、loop、TreeClip Decision/Commit、motion、window 与 gameplay cue 采样。Presentation timeline sampler MUST只使用 committed producer identity 与 visual time采样动画，MUST不执行 TreeClip、Motion 或 Gameplay fact。

#### Scenario: Attack Timeline 推进

- **WHEN** Attack Timeline 在一个 SimulationTick 内推进
- **THEN** Program operation MUST产生全部 Gameplay 结果
- **AND** PresentationFrame MUST只重采样动画表现

## MODIFIED Requirements

### Requirement: Timeline 轨道采样输出管线数据

Compiled Timeline gameplay tracks MUST在 Evaluate/Finalize 输出 typed facts、WorldRequest contribution 和 EventId presentation commands。Animation track resource binding MUST留在 CharacterPresentationProjection；Runtime MUST不把 Unity Track/Clip object 写入 SimulationOutput。

#### Scenario: 同一 Attack Timeline 产生 Window 与动画

- **WHEN** 当前 Tick 命中 Attack Window 并选择动画 producer
- **THEN** Tick result MUST分别包含 Gameplay Window fact 与 presentation command

### Requirement: Timeline 动画采样必须和逻辑事实采样分离

Gameplay Timeline sampling MUST只按 SimulationTick/canonical fraction 发生；动画 visual sampling MUST按 PresentationFrame visual time发生。多个 PresentationFrame MUST不重复产生 TreeClip、Motion、ActionWindow、Cue fact 或 Effect mutation。

#### Scenario: 两个逻辑 Tick 之间多次渲染

- **WHEN** PresentationFrame 多次采样同一 producer
- **THEN** 动画 pose MAY连续变化
- **AND** Gameplay state/facts MUST保持不变

### Requirement: CharacterPresentationStage 是 Unity 动画应用边界

SimulationCommitter 与 Character Presentation adapter MUST共同构成 Unity animation application boundary。Committer MUST只提交 Driver OutputPlan 标记为 Publish/Replace/Retire 的 producer command；Presentation adapter MUST通过 Projection、AnimationPlaybackLifecycle 和 Animancer应用。Kernel、WorldSolver 与 Driver MUST不引用 Animancer。

#### Scenario: Commit Attack producer

- **WHEN** Local Driver 将 Attack presentation command 标记为 Publish
- **THEN** Committer MUST将其送入唯一 animation command lifecycle

### Requirement: 不新增 Timeline 播放分裂路径

系统 MUST只有一条 Gameplay Timeline operation 路径和一条纯表现 sampling 路径。它们通过 producer/playback identity 与 EventId连接；不得保留旧 TimelinePlaybackScheduler gameplay runtime、Timeline.Bind/Evaluate/Unbind、自主 TreeClip runtime 或 AnimationClip root motion 路径。

#### Scenario: 搜索 Timeline Runtime

- **WHEN** Corin 完成 compiled migration
- **THEN** Gameplay TreeClip MUST只由 Program operation 执行

### Requirement: Timeline 回绕采样必须覆盖边界两侧

Compiled Timeline operation MUST在一个 SimulationTick 跨越 loop 边界时按尾段、中间 cycle 和头段稳定采样 Gameplay tracks。Presentation sampler MAY按 visual time回绕动画，但 MUST不补发 Gameplay facts。

#### Scenario: 一 Tick 跨越 Loop 终点

- **WHEN** logic time 从 cycle 尾部前进到下一 cycle 头部
- **THEN** Program MUST按正式区间顺序采样两侧 Gameplay segment

### Requirement: TreeClip 运行不得恢复 Timeline 双权威

TreeClip Decision/Commit lifecycle MUST只存在于 Program operation 与 CharacterSimulationState。Editor preview 只有通过隔离 Preview Simulation Session 执行同一 Program operation 时才 MAY 运行 TreeClip；正式 Character runtime 与 Preview MUST不创建 TimelineRunningTree clone、调用旧 scheduler 或维护第二份 TreeClip 解释逻辑。

#### Scenario: Runtime 与 Preview 并存

- **WHEN** Editor preview 打开同一 Timeline 且游戏运行 Corin Program
- **THEN** preview state MUST不影响 CharacterSimulationState
- **AND** preview 与 live runtime MUST执行相同 Program operation

### Requirement: 动画生命周期通道必须分离事实写入与批次消费权限

Kernel Finalize MUST只写 EventId producer selection/sample/complete/release command；SimulationCommitter MUST只按已校验 OutputPlan 写 presentation-owned command queue；PresentationFrame MUST原子消费并 acknowledge。任何阶段 MUST不双写同一 command。

#### Scenario: 一个 RenderFrame 前发生多个 SimulationTick

- **WHEN** queue 收到多个 generation 的 complete/release
- **THEN** PresentationFrame MUST按 Tick/Event sequence消费

### Requirement: Animation 与 Presentation 模块必须保持单向依赖

Portable Core MUST只定义 model-neutral presentation command，不引用 Animation/Presentation module。Projection/Committer MAY引用 Core identity/output；Animation adapter MAY引用 Projection/Presentation lifecycle，MUST不反向修改 Program 或 Character state。

#### Scenario: 普通 DotNet 编译 Core

- **WHEN** server project 编译 Program/Kernel
- **THEN** MUST不需要 Animancer 或 Unity Presentation assembly

### Requirement: 逻辑层必须为每个动画层提交唯一播放选择

Program Finalize MUST根据 State/Action ownership 为每个 LayerId 最多输出一个 selected producer/playback command。Committer 与 Presentation MUST不重新仲裁两个逻辑候选；冲突 MUST作为 Tick failure/diagnostic暴露。

#### Scenario: Action 与 Locomotion 同时声称 Base

- **WHEN** Program ownership 规则无法产生唯一选择
- **THEN** Finalize MUST报告冲突
- **AND** Presentation MUST不自行选择赢家

### Requirement: 目标播放准备就绪必须来自第一份合法 Sample

Presentation-owned AnimationPlaybackLifecycle MUST继续只以所选 producer 的第一份合法 visual sample 作为 readiness。Runnable completion、Kernel Finalize 或 Driver commit MUST不伪造 ready。

#### Scenario: 新 producer 尚无 Sample

- **WHEN** selection 已提交但 visual sample 未到
- **THEN** lifecycle MUST保持 PendingFirstSample 与现有 Current

## REMOVED Requirements

### Requirement: BTSMTL 内部 TimelinePlaybackScheduler 是 Timeline 播放权威

**Reason**：该 scheduler 同时持有 gameplay Timeline、TreeClip 与表现采样状态，无法进入 portable Program/State snapshot。

**Migration**：Gameplay 权威迁入 compiled Timeline operations，纯动画 sampling 留在 Presentation。

#### Scenario: 删除旧 Scheduler Gameplay 路径

- **WHEN** Corin 切换到 Program
- **THEN** MUST不创建旧 gameplay TimelinePlaybackScheduler

### Requirement: TimelinePlaybackScheduler 必须支持回绕稳定的循环播放

**Reason**：循环 Gameplay 语义迁入 Program operation；保留旧 Scheduler 循环会形成双时间源。

**Migration**：由 compiled Timeline operation 按 canonical logic time处理回绕。

#### Scenario: Loop Timeline

- **WHEN** compiled Timeline 回绕
- **THEN** MUST不调用旧 Scheduler gameplay loop

### Requirement: TimelinePlaybackScheduler 必须分阶段推进 TreeClip

**Reason**：TreeClip Decision/Commit 已成为 Program operation 阶段，旧 Scheduler 不再是正式 Character runtime。

**Migration**：Evaluate 内按固定顺序执行 Decision、Root/State 与 Commit operation。

#### Scenario: Decision TreeClip

- **WHEN** 当前 Tick 进入 Decision segment
- **THEN** Program operation MUST在 Graph decision 前写 Frame Blackboard
