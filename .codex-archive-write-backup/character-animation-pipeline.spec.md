# character-animation-pipeline Specification

## Purpose
定义角色管线中的Timeline和动画输出链路：Compiler将`TimelineNode`降低为Program operation，SimulationTick推进Gameplay Timeline并提交AnimationChannel winner，PresentationFrame按raw visual time生成Animation Selection，最终由CharacterSimulationPresentationRuntime在同一PlayableGraph执行唯一编译Pose Plan并发布最终姿势。
## Requirements
### Requirement: Timeline轨道采样必须输出Animation Selection数据

Compiler MUST将Timeline Animation Track降低为source-neutral selection binding和marker binding，并将唯一可达Timeline调用点声明的`PlaybackMode`编入对应producer。SimulationTick MUST只推进Gameplay Timeline与提交AnimationChannel winner；PresentationFrame sampler MUST按raw visual time、cycle、编译PlaybackMode和source-local clip权重生成Animation Selection与typed Parameter page。Presentation MUST不通过BlendSpace类型、Marker topology、clip名称或其它表现侧启发式规则推断Once或Loop。Timeline MUST不解析Marker Sync effective time，也 MUST不创建Pose、Blend entry、transition identity、Bone Mask或IK plan。

#### Scenario: 同一Attack Timeline产生Window与动画

- **WHEN** Attack Timeline在一个逻辑Tick内推进Gameplay Window并选择Attack animation producer
- **THEN** Gameplay Window MUST进入Program事实链
- **AND** Presentation MUST独立生成Attack Animation Selection供Pose Graph消费

#### Scenario: Loop与Once使用正式Timeline声明

- **WHEN** Idle、WalkLoop或RunLoop Timeline调用点声明`Loop`
- **THEN** Presentation sampler MUST按对应producer的编译PlaybackMode持续循环采样
- **AND** RunStart、MovingTurn、Attack或Dodge调用点声明`Once`时 MUST保持单次采样语义

#### Scenario: 同一producer存在冲突PlaybackMode

- **WHEN** 同一Timeline producer同时被`Once`与`Loop`调用点引用
- **THEN** Compiler MUST拒绝生成Presentation Projection
- **AND** Runtime MUST不选择任一模式作为fallback

### Requirement: Timeline 动画采样必须和逻辑事实采样分离

Gameplay Timeline sampling MUST只按 SimulationTick/canonical fraction 发生；动画 visual sampling MUST按 PresentationFrame visual time发生。多个 PresentationFrame MUST不重复产生 TreeClip、Motion、ActionWindow、Cue fact 或 Effect mutation。

#### Scenario: 两个逻辑 Tick 之间多次渲染

- **WHEN** PresentationFrame 多次采样同一 producer
- **THEN** 动画 pose MAY连续变化
- **AND** Gameplay state/facts MUST保持不变

### Requirement: CharacterSimulationPresentationRuntime必须执行唯一编译Pose Plan

SimulationCommitter与唯一`CharacterSimulationPresentationRuntime` MUST共同构成Unity animation application boundary。Presentation Runtime MUST消费committed Animation Selection与参数，执行Projection编译的Selection、Player、native pose composition、world-aware postprocess和final publication阶段，并在IK/Solver exact completion后发布唯一`FinalAnimationPoseFrame`。Runtime MUST不自动创建图外Stack、图外Foot Placement、第二Pose Graph或第二final writer。

#### Scenario: Commit Attack producer

- **WHEN** Program提交FullBodyAction channel的Attack Selection
- **THEN** Runtime MUST把Selection送入Pose Graph中绑定该channel的输入节点
- **AND** 最终是否经过BlendStack、如何覆盖Base以及是否执行FootPlacement MUST只由编译Pose Plan决定

#### Scenario: Selection经过MarkerSync

- **WHEN** 编译Pose Plan包含`AnimationSelectionInput -> MarkerSync -> BlendStack`
- **THEN** Runtime MUST先生成Player source usage，再由MarkerSync解析effective sample page，最后采样与混合source
- **AND** Timeline sampler MUST保持只提交raw visual time

#### Scenario: SelectedPosePlayer切换复用物理source槽位

- **WHEN** SelectedPosePlayer完成旧source到新source的Marker时间映射并声明旧source release
- **THEN** Runtime MUST在注册和采样新source前断开并释放旧source的CapturePlayable
- **AND** 旧CaptureJob与新CaptureJob MUST不在同一图评估中写入同一复用workspace槽位

### Requirement: 动画预览只读取正式调试Snapshot

系统 MUST从正式AnimationPlaybackLifecycle、Player、source backend与Pose Graph导出只读AnimationPlaybackFrameSnapshot或等价数据。Snapshot MAY包含AnimationChannelId、PoseNodeId、selection/source map、raw/effective sample time、Player source usage、PendingFirstSample、Selected、Retained、Retired、Stack entry/clock/Stored、Inertialization residual、Pose operation contribution以及Composed/PostProcess/Final completion，MUST不参与gameplay决策或最终播放。Timeline编辑器预览 MUST执行与正式链路相同的Selection、Lifecycle、Player、source backend与Pose Plan。

#### Scenario: 生成每帧预览数据

- **WHEN** 正式或 preview session 更新动画
- **THEN** 系统 MAY导出当前channel/player/playback lifecycle snapshot
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

