## MODIFIED Requirements

### Requirement: Action.Dodge 配置参数
系统 MUST 通过正式 `CharacterActionDefinitionSO`、Character Action Catalog 或批准的等价数据源提供 `Action.Dodge` 的请求绑定、优先级、抗性、selector、Directional timeline、Backstep timeline 和 body claim policy。Directional 与 Backstep 的正式 runtime motion、animation key、duration ticks、timeline window 和 cue request MUST 来自 selected ActionTimeline 的 seconds authoring 经固定 tick interval 量化后的 clip payload。旧 Directional / Backstep variant 字段 MAY 作为迁移输入或 authoring 诊断存在，但 MUST NOT 作为正式 runtime motion、animation 或 timeline fallback。

#### Scenario: Directional Timeline 提供运行时参数
- **WHEN** 设计者配置 Directional 变体
- **THEN** 正式 Dodge action definition MUST 能定位 Directional timeline
- **AND** Directional timeline MUST 能通过 Motion clip 表达 seconds authoring duration、distance、rotateToDirection 和必要 motion payload
- **AND** Directional timeline MUST 能通过 AnimationKey clip 表达 `Action.Dodge.Directional` 或等价稳定 key
- **AND** runtime definition MUST 能将 Directional timeline 编译为 deterministic duration ticks 和 clip tick 区间
- **AND** 请求 priority 和 resistance MUST 能从 action definition、interrupt policy 或批准的正式请求策略入口追踪

#### Scenario: Backstep Timeline 提供运行时参数
- **WHEN** 设计者配置 Backstep 变体
- **THEN** 正式 Dodge action definition MUST 能定位 Backstep timeline
- **AND** Backstep timeline MUST 能通过 Motion clip 表达 seconds authoring duration、distance、rotateToDirection 和必要 motion payload
- **AND** Backstep timeline MUST 能通过 AnimationKey clip 表达 `Action.Dodge.Backstep` 或等价稳定 key
- **AND** runtime definition MUST 能将 Backstep timeline 编译为 deterministic duration ticks 和 clip tick 区间
- **AND** 请求 priority 和 resistance MUST 能从 action definition、interrupt policy 或批准的正式请求策略入口追踪

#### Scenario: 缺失配置不 fallback
- **GIVEN** 正式 Dodge action definition 缺失 selector、Directional timeline、Backstep timeline、必要 Motion clip 或必要 AnimationKey clip
- **WHEN** 系统尝试构建 Dodge motion 输出
- **THEN** 系统 MUST 报告配置错误或拒绝该动作输出
- **AND** MUST NOT 使用代码内置默认值、状态机旧 `output` 字段、旧 variant 字段、场景临时字段、Behavior Graph 或 Resources 资产继续运行

#### Scenario: 非法配置被校验报告
- **GIVEN** timeline 中存在负 seconds、负距离、非法 seconds 区间、缺失 payload、负优先级或负抗性
- **WHEN** 系统校验动作配置
- **THEN** 校验 MUST 报告对应问题
- **AND** 正式 gameplay 路径 MUST NOT 静默把非法值改成另一套隐藏默认手感

#### Scenario: 状态机不复制动作手感参数
- **WHEN** 设计者检查 `Action.Dodge` 状态节点
- **THEN** 状态机节点 MAY 保存 action state id、variant key、timeline binding key 或 output module binding
- **AND** 状态机节点 MUST NOT 并行保存决定 Directional 或 Backstep motion duration/distance 的第二套正式参数

### Requirement: Dodge Timeline 作为运行时权威
`Action.Dodge` 的正式运行时 motion、animation key、duration ticks、timeline window 和 cue request MUST 来自 selected ActionTimeline 或批准的等价 timeline definition。旧 Directional / Backstep variant 字段 MAY 作为迁移输入存在，但 MUST NOT 作为正式 runtime motion 或 animation 权威。Authoring seconds MUST 通过固定量化规则编译为 runtime tick 数据后再参与采样。

#### Scenario: Directional 内容来自 Timeline
- **GIVEN** Dodge selector 选择 Directional timeline
- **WHEN** CommittedActionBranchEvaluator 在 tick N 评估
- **THEN** Directional Dodge 的 motion spec MUST 来自 Directional timeline 的 motion clip
- **AND** animation key MUST 来自 Directional timeline 的 animation clip
- **AND** resolver MUST NOT 从旧 Directional variant 字段补齐 runtime motion 或 animation

#### Scenario: Backstep 内容来自 Timeline
- **GIVEN** Dodge selector 选择 Backstep timeline
- **WHEN** CommittedActionBranchEvaluator 在 tick N 评估
- **THEN** Backstep Dodge 的 motion spec MUST 来自 Backstep timeline 的 motion clip
- **AND** animation key MUST 来自 Backstep timeline 的 animation clip
- **AND** resolver MUST NOT 从旧 Backstep variant 字段补齐 runtime motion 或 animation

#### Scenario: Timeline 采样使用 local tick
- **GIVEN** Dodge action 已在 source step S 被 accepted
- **WHEN** CommittedActionBranchEvaluator 在 source step `S + 5` 评估
- **THEN** selected timeline MUST 使用 local tick 5 采样
- **AND** MUST NOT 使用 Unity deltaTime、Animator normalized time 或 editor preview position 推导采样位置

### Requirement: Dodge Timeline 使用 Tick 时间权威
`Action.Dodge` 的 timeline runtime MUST 使用 action-local tick、duration ticks 和 window tick range 作为采样权威。Seconds MUST 作为 authoring、editor 和诊断语言存在，并 MUST 在进入 runtime definition 前通过固定 tick interval 编译为 tick。旧 frame 字段 MAY 只作为迁移输入或诊断存在，MUST NOT 作为正式 runtime fallback。

#### Scenario: Runtime 不读取 Seconds 权威
- **GIVEN** Dodge timeline definition 已被编译或加载到 runtime
- **WHEN** CommittedActionBranchEvaluator 在 tick N 评估
- **THEN** duration、window 和 timeline sampling MUST 基于 compiled tick 字段
- **AND** runtime MUST NOT 读取 seconds 字段作为推进 timeline 的权威来源

#### Scenario: 旧 frame 字段不作为 fallback
- **GIVEN** Dodge asset 仍包含 legacy frame 字段
- **WHEN** seconds authoring 字段缺失或非法
- **THEN** 正式 runtime MUST 报告配置错误或拒绝动作输出
- **AND** MUST NOT 从 legacy frame 字段静默补齐正式 runtime timeline

#### Scenario: 旧 frame 只允许按 legacy rate 迁移
- **GIVEN** Dodge asset 仍包含 legacy Directional 或 Backstep frame 字段
- **WHEN** 迁移器读取这些 frame 字段
- **THEN** 迁移器 MUST 使用显式 legacy authoring frame rate 转换为 seconds，默认 60Hz
- **AND** compiler MUST 再按 simulation tick settings 的 fixed tick interval 编译为 runtime ticks
- **AND** 正式 runtime MUST NOT 将 legacy frame 直接解释为 Dodge local tick
