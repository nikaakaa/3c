## MODIFIED Requirements

### Requirement: CharacterPipelineHost 只负责装配和注册

系统 MUST 使用 `CharacterPipelineHost` 作为每个角色的 Unity 装配点。Host MUST 只负责序列化唯一 ActorId、角色管线定义、Animancer、visual root、Logic Pose Adapter、按 authority mode 需要的 Motion Executor Adapter 和其它 Unity 组件引用，创建 pipeline，并注册和释放 pipeline。Host MUST NOT 直接序列化 BTSMTL RootTree 或 BTSMTL component 类型，MUST NOT 写入动作状态判断、状态切换、motion 结算或 GameplayResult 裁决逻辑。Host MUST NOT 把 concrete `CharacterController` 直接传入 CharacterPipeline。

#### Scenario: Host 创建 LocalSolver pipeline

- **WHEN** Host 以 LocalSolver 初始化
- **THEN** Host MUST 使用 ActorId、`CharacterPipelineDefinition`、Animancer、显式 Logic Pose Port、显式 Motion Executor 和输入配置创建 `CharacterPipeline`
- **AND** Host MUST NOT 创建 BBB `PlayerBaseState` 或 `PlayerStateRegistry`
- **AND** BTSMTL RootTree MUST 通过 `CharacterPipelineDefinition` 间接进入 pipeline

## ADDED Requirements

### Requirement: Character ActorId 必须由 Host 单点装配

每个可运行 CharacterPipelineHost MUST 持有唯一非空 ActorId，并在创建时传给 CharacterPipeline。Pipeline MUST 将同一 ActorId 提供给 CharacterGraphContext 与角色 Gameplay Effect 适配层。其它 binding MAY 读取 Host.ActorId，但 MUST NOT 保存可独立编辑的重复角色 identity。

#### Scenario: Host 缺少 ActorId

- **WHEN** CharacterPipelineHost 的 ActorId 为空
- **THEN** Host MUST 明确拒绝创建 CharacterPipeline
- **AND** 系统 MUST NOT 从 GameObject 名称、instance id 或网络配置生成 fallback identity

#### Scenario: 角色被模型 binding 注册

- **WHEN** 模型 binding 需要 subject actor identity
- **THEN** binding MUST 读取 CharacterPipelineHost.ActorId
- **AND** CharacterPipeline、Graph 与 GE Self Context MUST 使用同一值
