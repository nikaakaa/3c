# character-animation-pipeline Specification

## Purpose
定义角色管线中的Timeline和动画输出链路：Compiler将`TimelineNode`降低为Program operation，SimulationTick推进Gameplay Timeline，PresentationFrame按visual time生成Pose Request，最终由CharacterSimulationPresentationRuntime在同一PlayableGraph完成source capture、PoseSlot Blend、Pose Graph与最终姿势发布。
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

SimulationCommitter与唯一`CharacterSimulationPresentationRuntime`协调器 MUST共同构成Unity animation application boundary。Presentation Egress MUST把纠偏结果表达为当前最终producer selection、pose request、complete或release command，并以Publish disposition提交；MUST不要求Presentation撤回已经显示的历史command。协调器 MUST通过Projection校验producer，并将playback command唯一转发给`CharacterAnimationPlaybackRuntime -> AnimationPlaybackLifecycle -> PoseSlot Blend Stack -> source capture -> Pose Graph -> FinalAnimationPoseFrame`。每个外部PresentationFrame target MUST只调用一次协调器`Present`，MUST不读取animation readiness、不在`Present`与body-only入口之间分支，也 MUST不直接决定Body、Animation和Camera的推进顺序。Program Runtime、Execution Backend、Pipeline Pass、WorldSolver、Session Source和Network adapter MUST不引用Animancer、Blend Stack或Pose Graph实现，也 MUST不直接播放或合成动画。

Runtime创建时 MUST显式锁定animation启动策略。Local owner与完整simulated actor MUST使用`RequireCommittedSelection`，RequireOutput PoseSlot缺少逻辑selection时保持明确错误；只消费外部可靠表现流的observed actor MUST使用`AwaitCommittedSelection`，允许Body在第一份可靠selection到达前推进，但 MUST不伪造Idle、默认producer或隐藏selection。第一份合法selection到达后，observed actor MUST复用同一PendingFirstSample、Selected、Retained、Retired和PoseSlot transition生命周期。

上述 Egress Publish 约束适用于 Standard Float32 与 ServerAuthoritative。Deterministic Rollback adapter MAY在 rollback 原子提交完成后，依据有界 EventId state journal 对已经应用的表现状态调用唯一 Runtime 的 Replace 或 Retire；该对账 MUST不建立第二套 Timeline、crossfade 或 Gameplay state。

#### Scenario: Commit Attack producer

- **WHEN** LocalImmediateOutputPass将Attack presentation command标记为Publish
- **THEN** Committer MUST将其送入唯一Presentation协调器
- **AND** 协调器 MUST将其转发到现有animation playback lifecycle
- **AND** Pipeline Runtime MUST不直接调用Animancer或自行合成Pose

#### Scenario: Observed Actor等待可靠Selection

- **WHEN** selected Body horizon已推进但对应可靠animation selection尚未发布
- **THEN** 协调器 MUST继续推进Body表现
- **AND** MUST不调用外部body-only分支或伪造animation output

#### Scenario: Observed Actor收到首个Selection

- **WHEN** 第一份可靠selection及合法sample进入协调器
- **THEN** AnimationPlaybackLifecycle MUST从PendingFirstSample进入正式Selected生命周期
- **AND** 后续Stack transition与request MUST继续按Body frame提供的同一presentation clock推进

#### Scenario: Simulated Actor缺少Required Output

- **WHEN** Local owner或Deterministic Rollback simulated actor的RequireOutput PoseSlot没有逻辑selection
- **THEN** RequireCommittedSelection策略 MUST报告明确错误
- **AND** MUST不因该Actor无相机或被称为remote而静默等待

#### Scenario: 纠偏改变当前可见 producer

- **WHEN** ServerAuthoritative Egress确认预测producer不再是当前最终selection
- **THEN** Egress MUST生成新的release与最终selection command并以Publish提交
- **AND** 协调器 MUST由正式PoseSlot Stack接管而不建立第二套transition

#### Scenario: Fixed Rollback对账已应用的表现事件

- **WHEN** Fixed rollback原子提交后EventId journal判定既有表现事件被替换或退出有效历史
- **THEN** rollback presentation adapter MAY调用唯一Runtime的Replace或Retire
- **AND** Runtime MUST只修正表现生命周期，不修改Character/World state或重新执行Gameplay operation

### Requirement: 动画预览只读取正式调试Snapshot

系统 MUST从正式AnimationPlaybackLifecycle、PoseSlot Blend Stack、source backend与Pose Graph导出只读AnimationPlaybackFrameSnapshot或等价数据。Snapshot MAY包含AnimationChannelId、PoseSlotId、selection、sample time、PendingFirstSample、Selected、Retained、Retired、Stack entry、source identity与final pose completion，MUST不参与gameplay决策或最终播放。Timeline编辑器预览 MUST使用与正式链路相同的sampling、Lifecycle、Stack、source backend与Pose Graph。

#### Scenario: 生成每帧预览数据

- **WHEN** 正式或 preview session 更新动画
- **THEN** 系统 MAY导出当前channel/slot/playback lifecycle snapshot
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
- **AND** Authoring Preview MUST只复用表现sampling与playback lifecycle，不执行任何Program operation
- **AND** live runtime MUST独占执行该Timeline编译出的Program operation

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

### Requirement: 逻辑层必须为每个动画通道提交唯一播放选择

Program Finalize MUST根据State/Action ownership为每个AnimationChannelId最多输出一个selected producer/playback command。Committer与Presentation MUST不重新仲裁两个逻辑候选；冲突 MUST作为Tick failure/diagnostic暴露。

#### Scenario: Action 与 Locomotion 同时声称 Base

- **WHEN** Program ownership 规则无法产生唯一选择
- **THEN** Finalize MUST报告冲突
- **AND** Presentation MUST不自行选择赢家

### Requirement: 目标播放准备就绪必须来自第一份合法 Sample

Presentation-owned AnimationPlaybackLifecycle MUST继续只以所选 producer的第一份合法 visual sample作为 readiness。Runnable completion、Kernel Finalize或 Pipeline Commit MUST不伪造 ready。

#### Scenario: 新 producer 尚无 Sample

- **WHEN** selection 已提交但 visual sample 未到
- **THEN** Lifecycle MUST保持PendingFirstSample与现有Retained Stack输出

### Requirement: Compiled Timeline Operation 必须是 Gameplay Timeline 唯一权威

CharacterSimulationProgram Timeline operations MUST唯一拥有 Timeline request、logic time、loop、TreeClip Decision/Commit、motion、window 与 gameplay cue 采样。Presentation timeline sampler MUST只使用 committed producer identity 与 visual time采样动画，MUST不执行 TreeClip、Motion 或 Gameplay fact。

#### Scenario: Attack Timeline 推进

- **WHEN** Attack Timeline 在一个 SimulationTick 内推进
- **THEN** Program operation MUST产生全部 Gameplay 结果
- **AND** PresentationFrame MUST只重采样动画表现
