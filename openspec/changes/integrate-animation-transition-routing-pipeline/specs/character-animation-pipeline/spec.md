# Character Animation Pipeline Specification

## MODIFIED Requirements

### Requirement: CharacterSimulationPresentationRuntime必须执行唯一编译Pose Plan

SimulationCommitter与唯一`CharacterSimulationPresentationRuntime` MUST共同构成Unity animation application boundary。Presentation Egress MUST把纠偏结果表达为当前最终producer selection、sample、complete或release command，并以Publish disposition提交。Presentation Runtime MUST消费committed Animation Selection与参数，执行Projection编译的Selection、Player、native pose composition、world-aware postprocess和final publication阶段，并在IK/Solver exact completion后发布唯一`FinalAnimationPoseFrame`。Runtime MUST不自动创建图外Stack、图外Foot Placement、第二Pose Graph或第二final writer。每个外部PresentationFrame target MUST只调用一次协调器`Present`；Program Runtime、Execution Backend、Pipeline Pass、WorldSolver、Session Source和Network adapter MUST不引用Animancer、Blend Stack或Pose Graph实现，也 MUST不直接播放或合成动画。

当编译后的Player或BlendStack为某次source切换选择`Inertialization`时，该operation MUST向编译期绑定的下游Inertialization operation发布typed request。请求 MUST包含request route、source identity、target identity、transition rule identity、capture generation与生效时刻。唯一Pose Plan MUST先完成请求生产者的source切换与当前/上一表现帧采样，再由绑定consumer捕获当前最终姿势和速度残差，并在同一表现帧开始对新target pose施加衰减残差。Runtime MUST不通过全局事件、字符串查找、图外默认consumer或第二套transition service转发请求。

Runtime创建时 MUST显式锁定animation启动策略。Local owner与完整simulated actor MUST使用`RequireCommittedSelection`，Required Selection Input缺少逻辑selection时保持明确错误；只消费外部可靠表现流的observed actor MUST使用`AwaitCommittedSelection`，允许Body在第一份可靠selection到达前推进，但 MUST不伪造Idle、默认producer或隐藏selection。第一份合法selection到达后，observed actor MUST复用同一PendingFirstSample、Selected、Retained、Retired与Player source usage生命周期。

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
- **AND** 后续Player、MarkerSync、BlendStack与Pose operation MUST继续按Body frame提供的同一presentation clock推进

#### Scenario: Simulated Actor缺少Required Output

- **WHEN** Local owner或Deterministic Rollback simulated actor的Required Selection Input没有逻辑selection
- **THEN** RequireCommittedSelection策略 MUST报告明确错误
- **AND** MUST不因该Actor无相机或被称为remote而静默等待

#### Scenario: 纠偏改变当前可见 producer

- **WHEN** ServerAuthoritative Egress确认预测producer不再是当前最终selection
- **THEN** Egress MUST生成新的release与最终selection command并以Publish提交
- **AND** 协调器 MUST由图中显式Player节点接管而不建立第二套transition

#### Scenario: Fixed Rollback对账已应用的表现事件

- **WHEN** Fixed rollback原子提交后EventId journal判定既有表现事件被替换或退出有效历史
- **THEN** rollback presentation adapter MAY调用唯一Runtime的Replace或Retire
- **AND** Runtime MUST只修正表现生命周期，不修改Character/World state或重新执行Gameplay operation

#### Scenario: BlendStack发布惯性化请求

- **WHEN** FullBodyAction BlendStack提交一条选择Inertialization的精确source-target规则
- **THEN** Pose Plan MUST把请求只送入编译绑定的Action Inertialization operation
- **AND** Action Inertialization operation MUST基于BlendStack切换前最终输出与目标输出捕获姿势和速度残差
- **AND** BlendStack MUST在捕获完成后释放不再需要的旧source

#### Scenario: 请求路由没有合法consumer

- **WHEN** Projection包含会发布Inertialization request的Player或BlendStack但其分支没有唯一合法consumer
- **THEN** Pose Plan编译 MUST失败并指出producer node、transition rule与缺失consumer
- **AND** Runtime MUST不在执行时寻找或自动创建consumer
