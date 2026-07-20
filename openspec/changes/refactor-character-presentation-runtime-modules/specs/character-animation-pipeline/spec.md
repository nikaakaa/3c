## MODIFIED Requirements

### Requirement: CharacterSimulationPresentationRuntime 是 Unity 动画应用边界

SimulationCommitter与唯一 `CharacterSimulationPresentationRuntime`协调器 MUST共同构成Unity animation application boundary。Presentation Egress MUST把纠偏结果表达为当前最终producer selection、sample、complete或release command，并以Publish disposition提交；MUST不要求Presentation撤回已经显示的历史command。协调器 MUST通过Projection校验producer，并将playback command唯一转发给`CharacterAnimationPlaybackRuntime -> AnimationPlaybackLifecycle -> Animancer`。每个外部PresentationFrame target MUST只调用一次协调器`Present`，MUST不读取animation readiness、不在`Present`与body-only入口之间分支，也 MUST不直接决定Body、Animation和Camera的推进顺序。Program Runtime、Execution Backend、Pipeline Pass、WorldSolver、Session Source和Network adapter MUST不引用Animancer或直接播放动画。

Runtime创建时 MUST显式锁定animation启动策略。Local owner与完整simulated actor MUST使用`RequireCommittedSelection`，required layer缺少逻辑selection时保持明确错误；只消费外部可靠表现流的observed actor MUST使用`AwaitCommittedSelection`，允许Body在第一份可靠selection到达前推进，但 MUST不伪造Idle、默认producer或隐藏selection。第一份合法selection到达后，observed actor MUST复用同一PendingFirstSample、Current、Outgoing、Retired和Animancer fade生命周期。

上述 Egress Publish 约束适用于 Standard Float32 与 ServerAuthoritative。Deterministic Rollback adapter MAY在 rollback 原子提交完成后，依据有界 EventId state journal 对已经应用的表现状态调用唯一 Runtime 的 Replace 或 Retire；该对账 MUST不建立第二套 Timeline、crossfade 或 Gameplay state。

#### Scenario: Commit Attack producer

- **WHEN** LocalImmediateOutputPass将Attack presentation command标记为Publish
- **THEN** Committer MUST将其送入唯一Presentation协调器
- **AND** 协调器 MUST将其转发到现有animation playback lifecycle
- **AND** Pipeline Runtime MUST不直接调用Animancer

#### Scenario: Observed Actor等待可靠Selection

- **WHEN** selected Body horizon已推进但对应可靠animation selection尚未发布
- **THEN** 协调器 MUST继续推进Body表现
- **AND** MUST不调用外部body-only分支或伪造animation output

#### Scenario: Observed Actor收到首个Selection

- **WHEN** 第一份可靠selection及合法sample进入协调器
- **THEN** AnimationPlaybackLifecycle MUST从PendingFirstSample进入正式Current生命周期
- **AND** 后续fade与sample MUST继续按Body frame提供的同一presentation clock推进

#### Scenario: Simulated Actor缺少Required Output

- **WHEN** Local owner或Deterministic Rollback simulated actor的required layer没有逻辑selection
- **THEN** RequireCommittedSelection策略 MUST报告明确错误
- **AND** MUST不因该Actor无相机或被称为remote而静默等待

#### Scenario: 纠偏改变当前可见 producer

- **WHEN** ServerAuthoritative Egress确认预测producer不再是当前最终selection
- **THEN** Egress MUST生成新的release与最终selection command并以Publish提交
- **AND** 协调器 MUST从Animancer当前视觉状态接管而不建立第二套fade

#### Scenario: Fixed Rollback对账已应用的表现事件

- **WHEN** Fixed rollback原子提交后EventId journal判定既有表现事件被替换或退出有效历史
- **THEN** rollback presentation adapter MAY调用唯一Runtime的Replace或Retire
- **AND** Runtime MUST只修正表现生命周期，不修改Character/World state或重新执行Gameplay operation
