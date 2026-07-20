# character-gameplay-pipeline-closure Specification

## ADDED Requirements

### Requirement: ServerAuthoritative Gameplay必须复用正式Program与Step Pass

Prediction Client与Authority Worker MUST加载同一Corin Float32 Program并复用正式Program Evaluate、World ResolveBatch和Program Finalize Step Pass。Owner/server/remote差异 MUST只存在于Session Source、Ingress/Schedule/Egress Pass和Presentation registration，不得进入Graph、StateMachine、Timeline、Action、GameplayEffect或Motion operation。

#### Scenario: Authority Worker执行Dodge

- **WHEN** Authority Source accepted command包含Actor A Dodge request
- **THEN** Authority Pipeline MUST通过同一compiled Action/Timeline/Motion operation产生WorldRequest
- **AND** MUST不调用model专属Dodge代码

### Requirement: Local与Hybrid必须是显式且互不回退的完整组合

Local gameplay MUST只由Standard Local Pipeline组合运行；Hybrid gameplay MUST由Prediction或Authority Pipeline组合运行。三种Pipeline MAY共享Program Runtime、Execution Backend、标准Step Pass和Solver实现，但 MUST不共享mutable state、Source、History或Endpoint，并 MUST不在失败时互相切换。

#### Scenario: Fantasy连接失败

- **WHEN** Hybrid Prediction Source preparation失败
- **THEN** 当前Session MUST进入Failed
- **AND** MUST不创建Standard Local Pipeline继续Corin gameplay

### Requirement: 网络复制必须只消费正式Finalized Output

Authority Replication Egress MUST只消费finalized Character/World state、typed GameplayFacts、Presentation commands和EventId；MUST不读取Program mutable slot、pending evaluation、Graph authoring、Unity Transform或Animancer state。Prediction command egress MUST只发送canonical input与identity，MUST不发送resolved displacement作为权威真值。

#### Scenario: 复制Action Window

- **WHEN** Authority Timeline生成ActionWindow fact
- **THEN** Replication Egress MUST保留Actor、ActionInstance、Window、Tick和EventId
- **AND** Fantasy Room MUST只路由该事实而不重新解释窗口语义

### Requirement: Remote表现必须属于正式Committer消费链

Remote Body sample、producer command和reliable EventId facts MUST在Prediction Pipeline最终Commit边界进入remote presentation output，并复用既有Presentation interpolation、Animation lifecycle和Projection。Fantasy Handler、Room和Model Source MUST不直接调用Animancer、写visual Transform或决定Animation transition。

#### Scenario: Remote Actor切换到Attack2动画

- **WHEN** RemotePresentationEgress提交Authority producer select command
- **THEN** CharacterAnimationPlaybackRuntime MUST通过Projection播放Attack2 producer
- **AND** 网络层 MUST不发送AnimationClip或直接调用Play

### Requirement: Hybrid Diagnostics必须沿统一Source Map与Session Trace关联

Runtime diagnostics MUST能从authoring identity、Program operation、Prediction/Authority Pipeline Pass、SimulationTick、WorldRequest/Result、baseline、correction decision、EventId disposition和Presentation command形成只读关联。Diagnostics MUST不持有runtime clone、packet queue或mutable state。

#### Scenario: 审查Attack纠偏

- **WHEN** Attack2期间发生RestoreReplay
- **THEN** Debug Session MUST关联Authority baseline、Replay steps、Action operation、EventId suppression和最终动画producer
