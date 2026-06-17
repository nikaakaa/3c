## ADDED Requirements
### Requirement: CharacterFrameRuntimeController 持有正式 Runtime Host
系统 MUST 提供 `CharacterFrameRuntimeController` 或等价角色级 MonoBehaviour 作为正式 runtime host owner。该 controller MUST 位于 Character/Pipeline 或等价角色级目录语义下，MUST 持有唯一 `CharacterFrameRuntimeHost`，并且 MUST 通过角色级 runtime port 和 sibling submitters 推进 `CharacterFramePipeline`。

#### Scenario: Runtime host 不由 FullBody controller 创建
- **WHEN** 生产路径创建 `CharacterFrameRuntimeHost`
- **THEN** 创建职责 MUST 位于 `CharacterFrameRuntimeController` 或等价角色级 owner
- **AND** `PlayerFullBodyActionController` MUST NOT 直接 `new CharacterFrameRuntimeHost`
- **AND** `PlayerFullBodyActionController` MUST NOT 决定正式 submitter graph 中有哪些 sibling submitter

#### Scenario: Controller 不泄漏 Unity 对象进 Pipeline
- **WHEN** `CharacterFrameRuntimeController` 推进 `CharacterFramePipeline`
- **THEN** pipeline MUST 只接收 `ICharacterFrameRuntimePort` 或等价角色级 runtime port
- **AND** submitter graph MUST 只通过 Interface 访问 runtime 能力
- **AND** pipeline、submitter graph 和 frame model MUST NOT 保存 `MonoBehaviour`、`Transform`、`CharacterController`、Animator、Animancer runtime 或 InputAction

#### Scenario: Host 不是第二权威
- **WHEN** `CharacterFrameRuntimeController` 持有 runtime host
- **THEN** 状态权威 MUST 仍来自当前角色唯一 `CharacterStateMachineRunner`
- **AND** motion executor MUST 仍只有一个正式出口
- **AND** animation presenter MUST 仍只有一个正式出口

### Requirement: Character Runtime Port 组合兄弟 Adapter
角色级 runtime port MUST 能组合 Locomotion adapter、FullBody Action adapter、input buffer adapter、output runtime adapter 和 diagnostics adapter。该 port MUST NOT 通过 FullBody controller 的大操作面板表达所有能力，也 MUST NOT 要求新增身体域了解 FullBody-specific port。

#### Scenario: Locomotion 能力通过角色级 port 暴露
- **WHEN** Locomotion submitter 需要移动意图、Locomotion facts 或候选输出
- **THEN** 它 MUST 通过角色级 runtime port 的 Locomotion 能力或窄 `ILocomotionFrameRuntimePort` 获取
- **AND** MUST NOT 读取 `PlayerFullBodyActionController` 私有字段
- **AND** MUST NOT 通过 FullBody output runtime 执行压制

#### Scenario: FullBody Action 能力通过角色级 port 暴露
- **WHEN** FullBody Action submitter 需要 action request、state snapshot、policy 或 action motion facts
- **THEN** 它 MUST 通过角色级 runtime port 的 FullBody Action 能力或窄 action port 获取
- **AND** MUST NOT 要求 Locomotion submitter 成为其内部子模块
- **AND** MUST NOT 调用 Locomotion motion 或 animation output side effects

### Requirement: Character 级 Tick Adapter
系统 MUST 提供 `CharacterFrameRuntimeTickAdapter` 或等价角色级 tick adapter 作为 simulation tick 的正式 phase handler。旧 FullBody tick adapter MAY 作为迁移兼容转发存在，但 MUST NOT 作为 Corin 正式 runtime tick registration owner。

#### Scenario: Tick adapter 注册 Character phases
- **WHEN** simulation tick driver 启用
- **THEN** 角色级 tick adapter MUST 注册 `ReadInput`、`UpdateInputBuffer`、`GameplayDecision`、`BuildMotion`、`ExecuteMotion`、`PresentationBridge` 和 `WriteSnapshotAndEvents`
- **AND** 每个 phase MUST 调用同一个 `CharacterFrameRuntimeController`
- **AND** MUST NOT 为 FullBody 和 Locomotion 分别注册独立 gameplay phase owner

#### Scenario: Tick adapter 防止双驱动
- **GIVEN** frame update 和 simulation tick driver 都存在
- **WHEN** 角色级 tick adapter 接管角色
- **THEN** `CharacterFrameRuntimeController` 的 frame auto update MUST 被关闭或跳过
- **AND** FullBody controller 与 Locomotion controller 的 direct auto update MUST 不作为正式 gameplay driver
- **AND** 冲突 MUST 可通过自动测试或装配校验发现
