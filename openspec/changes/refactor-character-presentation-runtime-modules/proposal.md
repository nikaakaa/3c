# Change: 重构角色 Presentation Runtime 内部模块

## Why

当前 `CharacterSimulationPresentationRuntime` 已增长到约 1052 行，并同时拥有以下职责：

- Program/Projection identity 校验与 PresentationCommand 分发。
- 动画 playback lifecycle 推进。
- committed Body 历史、本地表现时钟、插值、预测分支替换与 visual recovery。
- visual root 绑定偏移与 Transform 应用。
- Camera state/response/target/cue 容器、resolver、look input 与 rig 输出。
- Cue/VFX/UI 临时信号、diagnostics、Reset 与 Dispose 顺序。

这些职责共享一个生命周期，但不应共享同一个实现类。当前实现还存在三个明确的所有权问题：

1. `ownsCamera` 同时决定是否使用本地 Body 表现时钟，导致“是否拥有相机”和“Body stream 如何采样”被错误绑定。Deterministic Rollback 的无相机 simulated actor 与 ServerAuthoritative 的 observed remote actor因此只能走同一个模糊的 remote 构造路径。
2. `ServerAuthoritativeRemoteVisualPoseFilter`、收敛参数和 visual pose diagnostics 位于具体 Network Model 中。网络模块先计算 presentation-only visual body，再把相同前后姿态反喂给通用 Presentation Runtime，形成 Network 与 Presentation 两个视觉姿态 owner。
3. `ServerAuthoritativeRemotePresentationRegistration` 读取 `HasRequiredAnimationOutput`，并在 `Present` 与 `PresentBody` 之间选择。外部调用方因此知道并控制 Body、Animation 的内部推进顺序，唯一 Presentation Runtime 只剩名义上的边界。

这不是要重写动画系统，也不是增加新的表现框架。本 change 保留 Animancer、`CharacterAnimationPlaybackRuntime`、Camera resolver、现有 visual recovery 和 selected Body 语义，只把职责迁回正确模块并收敛成一条调用链。

## What Changes

- 保留 `CharacterSimulationPresentationRuntime` 作为唯一公开 `ICharacterPresentationRuntime` 和唯一 PresentationFrame 协调入口；外部调用方不能直接取得 Body、Camera 或 Animation 子模块。
- 新增内部 `CharacterBodyPresentationRuntime`，唯一拥有 Body interval、表现时钟、stream reset/replacement、插值、visual recovery/convergence、visual root 应用及对应 diagnostics。
- 修正 committed branch replacement 的恢复语义：只以旧分支与新分支在同一表现采样时刻的姿态差生成 recovery offset，不把相邻表现帧之间的正常移动误判为纠偏。
- 将 Body 输入收敛为带 previous/current tick、pose 和显式 stream update kind 的正式 interval；Network adapter只提交 canonical selected Body interval，不再构造 visual body。
- 在创建 runtime 时显式选择 `CommittedStream` 或 `SelectedStream` Body 策略，不再从 camera ownership、Network Model 类型或调用方 class 推断。
- 新增内部 `CharacterCameraPresentationRuntime`，只在显式提供 camera binding 时创建，唯一拥有 Camera command lifecycle、resolver、look input、target binding、bind offset 和 `ICameraRigAdapter` 输出。
- 保留 `CharacterAnimationPlaybackRuntime` 作为唯一动画 playback owner；协调器只负责 producer 校验、命令路由和每帧调用。
- 为动画启动建立显式 `RequireCommittedSelection` 与 `AwaitCommittedSelection` 策略。Owner/simulated actor 缺少 required output 时继续报错；observed actor在可靠 selection 到达前只推进 Body，selection 到达后由同一协调器推进动画。
- 将 `ServerAuthoritativeRemotePresentationProfile` 迁移并重命名为 Character Presentation 所有的正式 remote visual profile；保留唯一资产与字段，删除旧 Network Model 类型和旧路径。
- 删除 `ServerAuthoritativeRemoteVisualPoseFilter`、外部 `PresentBody`、外部 `HasRequiredAnimationOutput` 分支、无行为的 `BeginTick` 以及不再被调用的直接构造重载。
- 让 `CharacterPresentationRuntimeFactory` 成为唯一创建入口，明确区分 local owner、simulated actor 和 observed actor 组合，同时复用同一个 runtime。
- 更新 local Host、Deterministic Rollback registration 和 ServerAuthoritative remote site，使它们只提交各自正式输入，不复制 Presentation 处理。
- 保持 Projection schema、Program ABI、PresentationCommand schema、Animancer fade、Camera authoring、Network protocol、selected Body horizon 与 gameplay state 不变。

## Capabilities

### Modified Capabilities

- `character-presentation-interpolation`：明确 Body interval、表现时钟、visual recovery/convergence 与 visual root 的唯一 Presentation owner，并将采样策略与 camera ownership 解耦。
- `character-camera-pipeline`：保留 `CharacterSimulationPresentationRuntime` 唯一公开边界，同时允许其通过不可外部访问的 Camera 内部模块完成解析和 rig 输出。
- `character-animation-pipeline`：禁止外部按动画 readiness 分支调用 Presentation；由唯一协调器按显式启动策略推进 Body 与 animation lifecycle。

## Dependencies And Sequencing

- 本 change 以 `refactor-gameplay-runtime-and-tooling-modules` 已完成的 selected Body visual convergence 和 Camera operation 闭环为行为基线，并修正其实现仍留在 `ServerAuthoritative` 模块的所有权问题。两者不得同时编辑 `ServerAuthoritativeRemotePresentationSite.cs`；应先确认前者实现稳定，再 apply 本 change。
- Action eligibility 已进入current specs。本 change 不修改 Action、GameplayTag、GameplayEffect、Graph/StateMachine/Timeline authoring、Agent schema、Program operation 或 Corin动作资产，不再存在对应active change的并行编辑冲突。
- 本 change 不依赖 `refactor-simulation-tick-hot-path` 的 output lease实现；若两者同时进行，只允许该 change修改 Simulation result生产，本 change只消费最终 committed result，不改变 result ABI。

## Current Spec Comparison

- `character-camera-pipeline` 已要求 `CharacterSimulationPresentationRuntime` 是相机唯一边界。本 change 不删除该边界，而是把 resolver容器封装为协调器私有模块；因此需要修改文字，避免把“唯一边界”误读成“所有代码必须写在一个类里”。
- `character-presentation-interpolation` 已要求 visual pose只属于 Presentation，且 Remote Body不得维护第二份 authority timeline。当前 Network Model内的 visual filter不改变 authority选择，但仍错误拥有 visual pose计算；本 change 将实现迁回 Presentation，并保持 selected Body stream唯一。
- `character-animation-pipeline` 已要求 `CharacterSimulationPresentationRuntime` 与 Animancer链是唯一动画应用边界。当前 external `HasRequiredAnimationOutput -> Present/PresentBody` 分支违反了该意图；本 change补齐而不改变动画选择语义。
- 没有现行 spec要求 `ownsCamera` 决定 Body clock，也没有 spec要求 ServerAuthoritative拥有 visual convergence。删除这两项不会与现行能力冲突。

## Impact

- 主要运行时代码：
  - `Runtime/Character/Pipeline/Presentation/CharacterSimulationPresentationRuntime.cs`
  - `Runtime/Character/Pipeline/Presentation/CharacterPresentationRuntimeContracts.cs`
  - 新的 Body 与 Camera 内部模块文件
  - `Runtime/Networking/GameplayNetwork/ServerAuthoritative/ServerAuthoritativeRemotePresentationSite.cs`
  - 删除 `ServerAuthoritativeRemoteVisualPoseFilter.cs` 的旧 Network Model实现
- 装配入口：
  - `CharacterPipelineHost`
  - Deterministic Rollback Character Host/Registration
  - ServerAuthoritative Remote Presentation Site/Registration
- 资产：迁移并重命名唯一 remote visual profile，保留 `.meta` identity和现有场景引用，不创建兼容 ScriptableObject或双配置。
- 不影响：Semantic IR、Float32/Fixed Program、Simulation state、WorldSolver、Action/Tag/GE、Animation Projection、Timeline资源、Fantasy协议和服务端代码。

## Out Of Scope

- 不实现新的 VFX、Audio、UI 或 GameplayCue consumer。
- 不修改动画 TransitionLibrary、producer binding、Animancer fade算法或 root motion。
- 不改变 Camera mode、priority、response、target和 modifier业务规则。
- 不改变 Network Model selected Body、可靠事件 horizon、prediction/correction或 rollback策略。
- 不重构 `CharacterPresentationProjection` 编译与运行查询；该边界可作为后续独立 change处理。
- 不拆分程序集，不新增测试或人工验证任务。
