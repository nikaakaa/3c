# character-animation-pipeline Specification

## Purpose
定义角色管线中的 Timeline 和动画输出链路：Compiler 将 `TimelineNode` 降低为 Program operation，SimulationTick 推进 Gameplay Timeline，PresentationFrame 按 visual time 采样动画，最终由 CharacterSimulationPresentationRuntime 和 Animancer 播放链应用。
## Requirements
### Requirement: Timeline 轨道采样输出管线数据

Compiled Timeline gameplay tracks MUST在 Evaluate/Finalize 输出 typed facts、WorldRequest contribution 和 EventId presentation commands。Animation track resource binding MUST留在 CharacterPresentationProjection；Runtime MUST不把 Unity Track/Clip object 写入 `SimulationActorTickResult`。

#### Scenario: 同一 Attack Timeline 产生 Window 与动画

- **WHEN** 当前 Tick 命中 Attack Window 并选择动画 producer
- **THEN** Tick result MUST分别包含 Gameplay Window fact 与 presentation command

### Requirement: Timeline 动画采样必须和逻辑事实采样分离

Gameplay Timeline sampling MUST只按 SimulationTick/canonical fraction 发生；动画 visual sampling MUST按 PresentationFrame visual time发生。多个 PresentationFrame MUST不重复产生 TreeClip、Motion、ActionWindow、Cue fact 或 Effect mutation。

#### Scenario: 两个逻辑 Tick 之间多次渲染

- **WHEN** PresentationFrame 多次采样同一 producer
- **THEN** 动画 pose MAY连续变化
- **AND** Gameplay state/facts MUST保持不变

### Requirement: CharacterSimulationPresentationRuntime 是 Unity 动画应用边界

SimulationCommitter 与唯一 `CharacterSimulationPresentationRuntime` 协调器 MUST共同构成 Unity animation application boundary。Standard Float32 与 ServerAuthoritative Egress MUST把最终 producer selection、sample、complete 或 release command 以 Publish disposition 提交，Float32 SimulationCommitter MUST拒绝 Presentation command 的 Replace 或 Retire disposition。Deterministic Rollback adapter MAY在 rollback 原子提交完成后，依据有界 EventId state journal 对已经应用的表现状态调用 `ICharacterPresentationRuntime.Replace` 或 `Retire`；该对账 MUST不建立第二套 Timeline、crossfade 或 Gameplay state。协调器 MUST通过 Projection 校验 producer，并将 playback command 唯一转发给 `CharacterAnimationPlaybackRuntime -> AnimationPlaybackLifecycle -> Animancer`。Animancer完成本帧最终pose后，协调器 MAY把该pose和只读visible playback contribution交给唯一注册的Presentation Pose Post Process Pass；该Pass MUST不选择producer、播放动画、修改Animancer state/layer/fade或生成Gameplay output。Program Runtime、Execution Backend、Pipeline Pass、WorldSolver、Session Source 与 Network adapter MUST不引用 Animancer、Final IK、Pose Post Process实现或直接播放/修改动画姿势。

#### Scenario: Commit Attack producer

- **WHEN** LocalImmediateOutputPass将 Attack presentation command标记为 Publish
- **THEN** Committer MUST将其送入唯一 animation command lifecycle
- **AND** Pipeline Runtime MUST不直接调用 Animancer

#### Scenario: 纠偏改变当前可见 producer

- **WHEN** ServerAuthoritative Egress确认预测 producer不再是当前最终选择
- **THEN** Egress MUST生成新的 release与最终 selection command并以 Publish提交
- **AND** MUST不向 Presentation Port提交历史 command的 Replace或 Retire

#### Scenario: Fixed Rollback 对账已应用的表现事件

- **WHEN** Fixed rollback 原子提交后 EventId state journal 判定既有表现事件被替换或退出有效历史
- **THEN** rollback presentation adapter MAY调用唯一 `ICharacterPresentationRuntime` 的 Replace 或 Retire
- **AND** Runtime MUST只修正表现生命周期，不修改 Character/World state 或重新执行 Gameplay operation

#### Scenario: Animancer完成最终pose

- **WHEN** AnimationPlaybackLifecycle已经提交本帧sample并调用Animancer Evaluate
- **THEN** 唯一Pose Post Process Pass MAY消费最终骨骼姿势和只读visible playback contribution
- **AND** MUST不建立另一份animation selection或crossfade权威

### Requirement: 动画层预览只读取调试 Snapshot

系统 MUST从正式 AnimationPlaybackLifecycle 与 Animancer adapter 导出只读 AnimationPlaybackFrameSnapshot 或等价数据。Snapshot MAY包含每层 selection、sample time、PendingFirstSample、Current、Outgoing、Retired、Animancer state key 与 fade progress，MUST不参与 gameplay 决策或最终播放。Timeline 编辑器预览 MUST使用与正式链路相同的 sampling、lifecycle 和 Animancer adapter。

#### Scenario: 生成每帧预览数据

- **WHEN** 正式或 preview session 更新动画
- **THEN** 系统 MAY导出当前 layer/playback lifecycle snapshot
- **AND** 编辑器 MUST只读取该 snapshot

#### Scenario: 运行时禁用调试历史

- **WHEN** 项目关闭动画历史采集
- **THEN** 系统 MAY不保存历史 snapshot
- **AND** 正式播放 MUST不依赖 snapshot

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

TreeClip Decision/Commit lifecycle MUST只存在于 Program operation 与 CharacterSimulationState。Timeline Authoring Preview MUST不运行 TreeClip、Program operation或 Preview Simulation Session；正式 Character runtime 与 Preview MUST不创建 TimelineRunningTree clone、调用旧 scheduler 或维护第二份 TreeClip 解释逻辑。真实 Decision/Commit 与输出只通过正式运行 Session和 Live Debug观察。

#### Scenario: Runtime 与 Preview 并存

- **WHEN** Editor preview 打开同一 Timeline 且游戏运行 Corin Program
- **THEN** preview state MUST不影响 CharacterSimulationState
- **AND** preview 与 live runtime MUST执行相同 Program operation

### Requirement: 动画生命周期通道必须分离事实写入与批次消费权限

Kernel Finalize MUST只写 EventId producer selection/sample/complete/release command；SimulationCommitter MUST只按已校验 OutputDisposition写 presentation-owned command queue；PresentationFrame MUST原子消费并 acknowledge。任何阶段 MUST不双写同一 command。

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

Presentation-owned AnimationPlaybackLifecycle MUST继续只以所选 producer的第一份合法 visual sample作为 readiness。Runnable completion、Kernel Finalize或 Pipeline Commit MUST不伪造 ready。

#### Scenario: 新 producer 尚无 Sample

- **WHEN** selection 已提交但 visual sample 未到
- **THEN** lifecycle MUST保持 PendingFirstSample 与现有 Current

### Requirement: Compiled Timeline Operation 必须是 Gameplay Timeline 唯一权威

CharacterSimulationProgram Timeline operations MUST唯一拥有 Timeline request、logic time、loop、TreeClip Decision/Commit、motion、window 与 gameplay cue 采样。Presentation timeline sampler MUST只使用 committed producer identity 与 visual time采样动画，MUST不执行 TreeClip、Motion 或 Gameplay fact。

#### Scenario: Attack Timeline 推进

- **WHEN** Attack Timeline 在一个 SimulationTick 内推进
- **THEN** Program operation MUST产生全部 Gameplay 结果
- **AND** PresentationFrame MUST只重采样动画表现
