## MODIFIED Requirements

### Requirement: 动作定义转换为纯 runtime model
系统 MUST 提供 `CharacterActionDefinitionSO` 或等价动作定义 SO，并能将其转换为纯 runtime action definition。runtime definition MUST 只包含动作解析、请求准入和 committed action branch 评估需要的值类型数据、稳定 ID、request binding、优先级、抗性、source input、body claim policy binding、selector definition 和 compiled tick timeline definition。runtime definition MUST NOT 持有 Unity asset、scene object、controller、presenter、input runtime object、AnimationClip 或 Animancer runtime object。Directional / Backstep 旧 variant 字段 MAY 作为迁移输入或 authoring 辅助存在，但正式 runtime motion、animation key、duration ticks、window 和 cue MUST 来自 selected ActionTimeline seconds authoring 经固定 tick interval 量化后的 timeline definition。

#### Scenario: Dodge definition 输出纯数据
- **WHEN** 运行时从 `Action.Dodge` definition 构建 runtime definition
- **THEN** 结果 MUST 包含 `Action.Dodge` action id
- **AND** MUST 包含 `ActionRequestType.Dodge` 或等价 request type
- **AND** MUST 包含 `InputRequestKind.Dodge` 或等价来源输入
- **AND** MUST 包含请求准入所需 priority 和 resistance
- **AND** MUST 包含 Dodge selector、Directional timeline 和 Backstep timeline 的纯数据 runtime definition
- **AND** Directional / Backstep 的 runtime motion、animation key、duration ticks、window 和 cue MUST 来自对应 timeline clip payload 的 compiled tick 数据
- **AND** MUST NOT 包含 `ScriptableObject`、`InputAction`、Animancer runtime 或场景实例引用

#### Scenario: 旧 Variant 不作为 runtime motion 权威
- **GIVEN** 动作定义中仍存在旧 Directional 或 Backstep variant 字段
- **WHEN** runtime 构建 `Action.Dodge`
- **THEN** 这些字段 MAY 被迁移工具或 validator 用于诊断
- **AND** runtime MUST NOT 从这些字段补齐 motion spec、animation key、duration ticks、window 或 cue
- **AND** 缺失对应 timeline payload 时 MUST 报告配置错误

#### Scenario: 非法定义报告错误
- **GIVEN** 动作定义缺失 action id、request type、source input、priority、resistance、selector、Directional timeline 或 Backstep timeline
- **WHEN** 运行配置校验
- **THEN** 校验 MUST 报告错误
- **AND** runtime MUST NOT 使用隐藏默认值、旧 variant 字段、Resources 或 sample asset 补齐定义

#### Scenario: seconds authoring 编译为 tick runtime
- **GIVEN** 动作定义中的 timeline authoring 使用 seconds 表达 Motion clip 范围
- **AND** 调用方从 simulation tick settings 提供 fixed tick interval compile context
- **WHEN** action definition compiler 构建 runtime definition
- **THEN** runtime definition MUST 保存 deterministic tick 区间
- **AND** runtime evaluator MUST NOT 在采样时重新读取 authoring seconds 字段
