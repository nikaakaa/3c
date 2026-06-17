## ADDED Requirements
### Requirement: Character runtime port 去 FullBody 化
系统 MUST 将正式 Character runtime port Interface 收敛为角色级 frame 能力合同。该 Interface MUST NOT 通过继承 `IFullBodySubmissionRuntimePort`、`IFullBodyOutputRuntimePort` 或等价 FullBody port 来暴露 FullBody 内部操作面板。FullBody port MAY 保留在 FullBody adapter 内部。

#### Scenario: Character port 不继承 FullBody ports
- **WHEN** 检查正式 Character runtime port Interface
- **THEN** Interface MUST 只暴露角色帧 pipeline 所需能力
- **AND** MUST NOT 继承 FullBody submission runtime port
- **AND** MUST NOT 继承 FullBody output runtime port
- **AND** MUST NOT 要求 UpperBody、HitReact 或 Aim submitter 了解 FullBody port 面板

#### Scenario: FullBody adapter 内部保留领域端口
- **GIVEN** FullBody Action 仍需要 runner、snapshot、interrupt policy、action resistance 或 output runtime
- **WHEN** FullBody adapter 构建自己的提交或输出
- **THEN** 它 MAY 使用 FullBody-specific narrow ports
- **AND** 这些 ports MUST NOT 成为 Character-level host 的正式 Interface
- **AND** 这些 ports MUST NOT 被新身体域当作上级 owner

### Requirement: FullBody host 降级为兼容 Adapter
`PlayerFullBodyActionController` 或等价 MonoBehaviour MUST 从正式角色帧 owner 降级为 Unity 装配、配置解析、调试 view 和旧 Tick 兼容 Adapter。它 MAY 转发到 Character-level runtime host，但 MUST NOT 长期直接创建或拥有正式 `CharacterFramePipelineHost`。

#### Scenario: 旧 Tick 入口转发
- **WHEN** 兼容代码调用 `PlayerFullBodyActionController.Tick`
- **THEN** 该入口 MAY 读取输入并转发到 Character-level runtime host
- **AND** MUST NOT 构造第二条 phase 顺序
- **AND** MUST NOT 让 FullBody MonoBehaviour 成为新增身体域的上级 owner

#### Scenario: 正式 host 位于 Character 层
- **WHEN** 生产路径创建角色帧 host
- **THEN** 正式 host MUST 位于 Character pipeline/runtime 语义下
- **AND** MUST 组合角色级 submitters、arbiter、composer 和 applier
- **AND** MUST NOT 由 FullBody controller 私有字段决定哪些身体域参与正式仲裁

### Requirement: 端口退役可测试
系统 MUST 提供自动测试证明 FullBody port 降级不会产生分裂路径。测试 MUST 覆盖 Character pipeline 不直接依赖 FullBody concrete adapter、新身体域不依赖 FullBody controller、旧 Tick 入口只转发到同一 host。

#### Scenario: 静态测试阻止 FullBody 泄漏
- **WHEN** 运行 runtime port 静态测试
- **THEN** 测试 MUST 确认 Character-level port 不继承 FullBody ports
- **AND** MUST 确认 `CharacterFramePipeline` 不引用 `PlayerFullBodyActionController`
- **AND** MUST 确认新增身体域不引用 FullBody 私有 runtime 状态作为仲裁输入权威
