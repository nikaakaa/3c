## MODIFIED Requirements

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
