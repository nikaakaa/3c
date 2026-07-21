# Change: 重构角色可见轨迹跟随

## Why

Deterministic Rollback 当前已经让两名 Actor 在本机继续消费最新 predicted timeline，也会在 canonical input 到达后原子替换 replay 分支；网络同步和逻辑模拟方向没有问题。现在的“远端角色飘、转身像瞬切”发生在 Simulation Commit 之后的 Presentation：

- `CharacterPresentationBodyState` 只保留 Position 与 Rotation，`WorldBodyState` 已有的 Velocity 与 Grounded 在表现边界被丢弃。
- `CommittedStream` 在分支替换时把旧、新分支的姿态差累计为 offset，并使用硬编码六个 Tick 的指数衰减。连续 canonical 修订会让可见角色长期追赶新 target。
- `SelectedStream` 对每一帧正常移动持续运行另一套 `SmoothDamp`，把正常轨迹和纠偏轨迹一起滤波，天然产生拖尾。
- `CommittedStream / SelectedStream` 同时表达“Body 数据从哪里来”和“可见姿态如何跟随”，导致不同 Network Model 很难只选择策略而不复制实现。

因此这里不是继续增加网络延迟，也不是修改 BTSMTL 或动画资源，而是把 Body target sampling 与 visual correction 正式分开：正常移动直接消费渲染帧重采样轨迹，只有分支替换、流重置或真实不连续才启动有界纠偏。

## What Changes

- 将 `CharacterPresentationBodyState` 扩展为表现所需的完整运动学 sample：Position、Rotation、LinearVelocity 与 Grounded；Float32、Fixed 和 observed Body 边界必须保留这些已有信息，不修改网络协议或 Simulation ABI。
- 将 Body 数据源和视觉响应拆成两个正交合同：
  - Source：`CommittedStream` 或 `SelectedStream`，只决定 target history 与表现时钟。
  - Trajectory：`Direct` 或 `BoundedCorrection`，只决定 target 不连续时 visual pose 如何接管。
- 新建 Presentation 所有的正式 `CharacterBodyPresentationProfile`。Profile 显式声明 Trajectory 模式和有界纠偏参数；缺失或非法配置直接失败，不提供默认 profile 或按 Network Model 类型猜测。
- 以单一 `CharacterVisualTrajectoryFollower` 替换 committed recovery offset 和 selected per-frame SmoothDamp：
  - 连续 append 区间只做 target interpolation，不叠加低通拖尾。
  - 分支替换时从“上一帧可见姿态/速度”与“新分支同一表现时刻 target 姿态/速度”的差开始纠偏。
  - 连续替换重新计算当前误差，不累计新的固定时长尾巴。
  - 水平位置与 yaw 独立收敛；Grounded target 的垂直位置直接跟随，避免悬浮或陷地。
  - 最大位置误差、最大 yaw 误差、half-life 和 settle threshold 全部来自正式 profile。
- Factory 继续是唯一创建入口，但每个创建调用点必须显式提供 Body Presentation Profile。Standard Local 与 Preview 使用 `Direct`；Deterministic Rollback 完整模拟 Actor 使用 `BoundedCorrection`；ServerAuthoritative observed Actor 使用自己的 `BoundedCorrection` profile。
- Body visual correction 不改变 AnimationSampleTick/Alpha。动画继续按 predicted presentation timeline 和现有 EventId/Playback lifecycle采样；同一 playback 的 replay sample替换只更新目标采样，不重启动画。
- 扩展 Presentation diagnostics，暴露 source、trajectory mode、target/visible pose、target/visible velocity、correction error/velocity、Grounded、clamp、settle、branch/reset identity。
- 删除旧 `m_RecoveryPositionOffset`、`m_RecoveryRotationOffset`、固定六 Tick recovery、selected visual velocity/yaw SmoothDamp 状态以及 `CharacterRemotePresentationProfile` 旧命名，收敛为一条正式实现。

## Capabilities

### Modified Capabilities

- `character-presentation-interpolation`：将 Body source 与 visual trajectory response 解耦，补齐运动学 sample，定义只在真实不连续时运行的有界纠偏、配置所有权、动画采样时钟与诊断合同。

## Dependencies And Sequencing

- 本 change 以 `refactor-character-presentation-runtime-modules` 已完成的 `Factory -> Body / Animation / Camera` 分层为实现基线。该 change 虽尚未归档，但 `openspec list` 已显示 Complete；实施时不得重开或复制其旧 runtime 路径。
- `refactor-simulation-tick-hot-path`、`refactor-simulation-operation-runtime-modules` 和 DeterministicRollback Fixed/KCC 基座保持不变。本 change 只扩展 Simulation 到 Presentation 的 Unity 投影值，不修改 Program、Snapshot、Hash、Solver 或 canonical input。
- ServerAuthoritative observed stream 继续由 Prediction Schedule 选择 tick。本 change 只迁移其 Presentation convergence，不改变 selected horizon、可靠事件或接触约束。
- 实施顺序必须先建立新 Body sample/profile/follower，再迁移所有调用点，最后删除旧 profile 与两套 recovery 状态；不得在中间保留运行时 fallback 或双滤波。

## Current Spec Comparison

- 现行 `character-presentation-interpolation` 已明确 Rollback remote 必须继续消费 predicted current timeline，confirmed horizon 不得作为表现延迟缓冲。本 change 保留该要求，不引入四 Tick 延迟或 Confirmed cursor。
- 现行 spec 把 `CommittedStream / SelectedStream` 称为 Body“策略”，但它们实际只应表达 target source；视觉响应目前被隐式绑定在 source 分支内。本 change 会修改该要求，将 Source 与 Trajectory Profile 分开。
- 现行 spec 允许 visual recovery/convergence，却没有规定正常连续区间不得持续滤波，也没有要求保留 target velocity。当前固定六 Tick offset 和 SelectedStream SmoothDamp 都满足文字表面但产生业务上的漂移；本 change 补齐可验收语义。
- 现行 `character-animation-pipeline` 已要求 replay sample替换不得建立第二套动画时间轴，且 Animancer 必须从当前视觉图接管。本 change 不改该能力，只保证 Body correction 不篡改 AnimationSampleTick/Alpha。
- 现行 `deterministic-rollback-network-model` 的 canonical input、history、restore/replay、hash 和 output disposition 与本 change 不冲突，不需要修改。
- `openspec/project.md` 仍把已完成的 `refactor-character-presentation-runtime-modules` 记录为 `122/125`，并把 Body source 与视觉响应写成一个策略；实施完成时必须更新这些过时描述。

## Impact

- 主要运行时代码：
  - `Runtime/Character/Pipeline/Presentation/CharacterPresentationRuntimeContracts.cs`
  - `Runtime/Character/Pipeline/Presentation/CharacterBodyPresentationRuntime.cs`
  - 新的 Presentation-owned visual trajectory follower/profile
  - Float32 与 Fixed Unity Presentation boundary
- 装配调用点：
  - `CharacterPipelineHost`
  - `PreviewSimulationActorRegistration`
  - `DeterministicRollbackCharacterHost`
  - `ServerAuthoritativeRemotePresentationSite`
- 资产：
  - 将现有 remote profile 单路迁移为通用 Body Presentation Profile并保留引用 identity。
  - 为 Standard/Preview 与 DeterministicRollback 建立显式 profile 引用；同一产品内相同角色可复用同一 profile资产。
- 文档：更新 `character-presentation-interpolation` current truth 与 `openspec/project.md` 的 Presentation 方向和 active change 状态。

## Out Of Scope

- 不修改 InputDelay、ConfirmationDelay、Rollback history、canonical input、replay调度或 Peer协议。
- 不切换到 confirmed/延迟表现，也不增加网络 snapshot interpolation buffer。
- 不修改 BTSMTL、StateMachine、Timeline、Action、GameplayTag、GameplayEffect、Motion Warp或Corin动作窗口。
- 不实现 Motion Matching、Stride Warping、Foot IK或新的动画选择规则。
- 不让 Presentation velocity、visual root 或 follower state进入Snapshot、Hash、WorldSolver或Gameplay state。
- 不重做 ServerAuthoritative remote tick选择与可靠动画事件同步。
- 不新增测试或人工验证任务。
