# BTSMTL Timeline Editor Preview Specification

## MODIFIED Requirements

### Requirement: 预览采样必须复用正式动画Selection与Pose Plan

Authoring Preview MUST把当前Timeline/Track/Clip时间降低为含raw visual time的正式Animation Selection与Parameter page，并执行匹配Projection的`CharacterPresentationPosePlan`。Preview MUST只在图中存在MarkerSync时显示和应用effective time，并按图中显式transition rule与节点决定Standard Blend、零时长Standard Blend产生的Hard Cut结果、BlendStack Stored Pose容量压缩或由Player/BlendStack发布请求并由下游Inertialization消费的残差衰减，按同一Layered/Additive/ModifyBone拓扑生成Composed Pose；具备正式Body与PhysicsScene上下文时 MAY执行FootPlacement节点，否则 MUST把world-aware阶段标记为Unavailable。Preview MUST使用与运行时相同的typed request route、capture generation、source release顺序和consumer policy，且 MUST不创建隐藏Marker Sync、固定per-slot Stack、隐藏Inertialization、全局request bus、简化PoseGraph、Animancer direct Play或假Foot Physics。

#### Scenario: 当前时间采样

- **WHEN** preview time 位于 AnimationTrack clip 范围
- **THEN** session MUST提交该producer的唯一preview command与Animation Selection
- **AND** AnimationPlaybackLifecycle MUST完成PendingFirstSample/Selected提交
- **AND** 正式native链 MUST应用Projection中的producer source、Player与Pose Plan binding

#### Scenario: 同channel多个producer

- **WHEN** 一次preview evaluation发现多个producer声明同一AnimationChannelId
- **THEN** session MUST 明确拒绝该 evaluation
- **AND** MUST 不按 Priority 或 Track 顺序选择赢家

#### Scenario: 非连续 seek

- **WHEN** preview time 非连续跳转
- **THEN** session MUST retire旧preview EventId并清理对应channel Lifecycle、Player、source与Pose Plan workspace
- **AND** 目标时间 MUST 使用新的 preview playback generation 建立 command/sample

#### Scenario: 连续播放

- **WHEN** session 连续播放
- **THEN** 同一 preview playback generation MUST 持续更新 producer sample time
- **AND** session MUST 不在每个表现帧重新创建隐藏 producer

#### Scenario: 预览BlendStack到Inertialization

- **WHEN** 当前preview切换命中FullBodyAction BlendStack的一条Inertialization规则
- **THEN** Preview MUST由BlendStack发布正式typed request
- **AND** MUST由图中Action Inertialization消费请求并生成残差
- **AND** 预览诊断 MUST显示与运行时一致的producer、consumer、rule identity与capture generation

#### Scenario: 非连续seek清理未完成请求

- **WHEN** preview generation因非连续seek被替换
- **THEN** session MUST清理旧generation的待处理请求、残差与source usage
- **AND** 新generation MUST不继承旧generation的惯性速度
