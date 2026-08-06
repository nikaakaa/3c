## MODIFIED Requirements

### Requirement: Pose State transition必须显式编译Routing并从source binding派生同步

每条Transition MUST显式配置source、target、priority、Rule、`Standard Blend | Inertialization`、duration、Blend Mode、条件式Custom Curve Asset与强类型Blend Profile。Transition MUST不保存target reset policy或SourceSyncMode。每个State MUST显式保存`AlwaysResetOnEntry`，并成为其内部全部Player进入生命周期的唯一owner。Projection Compiler MUST把Blend Mode或Custom Curve Asset编译为canonical curve index，把Blend Profile编译为匹配Rig的dense per-bone profile index，并根据source/target State中唯一可同步provider的Pose Source Binding共同Sync Group自动生成可选Source Sync Plan。Standard Blend MUST按该curve/profile执行；Inertialization MUST把同一edge的duration/curve/profile交给branch-local执行节点，不得由下游Policy替换。Marker topology和effective sample映射 MUST来自source-local binding，Pose Graph MUST不创建MarkerSync节点。Runtime与Preview MUST只执行匹配Projection revision的计划，不得现场重新编译。

#### Scenario: Walk与Run属于共同MarkerGroup

- **WHEN** 两侧State各有唯一可同步provider且binding共享canonical SyncGroup
- **THEN** Source Sync Plan MUST在共同可见期间持续映射marker segment fraction
- **AND** Transition MUST不保存重复的同步开关

#### Scenario: 目标State要求重新进入

- **WHEN** target State的`AlwaysResetOnEntry=true`且Transition首次准备该target
- **THEN** StateMachine MUST在提交target relevance前重置该State全部source provider
- **AND** Transition与Player MUST不覆盖该State进入策略

#### Scenario: 目标State保留播放状态

- **WHEN** target State的`AlwaysResetOnEntry=false`
- **THEN** StateMachine MUST保留该State provider既有clock与properties
- **AND** MUST不按source edge、Player默认值或当前显示名重置

#### Scenario: Standard Blend使用每骨骼Profile

- **WHEN** Walk到Run选择EaseOut且Blend Profile让腿部duration multiplier小于脊柱
- **THEN** Standard Blend MUST按同一canonical curve分别求值腿部与脊柱的归一化时间
- **AND** MUST不退回统一`elapsed / duration`权重

#### Scenario: Target选择Inertialization

- **WHEN** target Ready且compiled route为Inertialization
- **THEN** transition owner MUST提交绑定edge duration、curve和profile的typed capture/release request
- **AND** branch-local consumer MUST使用该数学配置完成capture、rebase与completion

#### Scenario: Standard Blend Duration为零

- **WHEN** edge选择Standard Blend且Duration为0
- **THEN** Runtime MUST在该帧执行Hard Cut并完成source release
- **AND** MUST不创建Inertialization request或隐式BlendStack
