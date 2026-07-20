## MODIFIED Requirements

### Requirement: Agent Intent 必须表达角色动作业务意图

系统 MUST 提供面向 Agent 的 `AgentControllerIntent` schema，用于表达角色动作控制器业务意图。Intent SHOULD 使用 input request、action category、nested state machine、state、ActionProfile、Timeline、direction branch、cancel、hit reaction 等业务概念。Intent MUST 能表达“外层 Attack category 拥有可变段数的内层 combo StateMachine”以及“外层 Dodge category 拥有按正式移动输入选择 leaf 的内层 direction StateMachine”，但 MUST NOT 要求作者或 Agent 直接填写 BTSMTL 内部字段、Unity YAML 路径、节点 GUID 或私有序列化字段。

#### Scenario: 描述五段普通连击

- **WHEN** Agent 需要表达五段普通攻击连击
- **THEN** Intent MUST 能描述外层 Attack category、内层 Attack1..5、各自 ActionProfile、各自 Timeline 和相邻 combo 条件
- **AND** Intent MUST NOT 把 Attack1..5 强制平铺到外层 Action StateMachine
- **AND** Intent MUST 能显式声明末段是否回到首段，未声明时 MUST NOT 自动推导循环

#### Scenario: 描述方向闪避

- **WHEN** Agent 需要表达无方向输入与有方向输入两种 Dodge leaf
- **THEN** Intent MUST 能描述外层 Dodge category、内层 DodgeBack/DodgeForward、各自 ActionProfile 与 Timeline
- **AND** 方向选择 MUST 位于内层 Entry transition
- **AND** 目标 leaf MUST 继续作为 Dodge request 的唯一消费点

### Requirement: Macro 必须将业务意图展开为受限 Patch IR

系统 MUST 提供 Agent Macro 层，将受限业务意图展开为 Patch IR。`action_combo` Macro MUST 使用普通 `StateMachineNode`、inline `StateMachineGraph`、`StateNode`、Transition edge 和 ConditionRuleGraph 表达外层 Attack category 与可变段数的内层 combo 状态机，并能显式展开 loop 与 MoveCancel。`directional_dodge` Macro MUST 使用相同通用节点表达外层 Dodge category、内层 direction 状态机、后摇重入和 RushAttack category handoff。Macro MUST NOT 新增 Attack/Dodge 专用 opcode、直接修改 BTSMTL asset、生成平铺 leaf，或为未声明循环的末段生成隐式 loopback。

#### Scenario: 展开五段连击

- **WHEN** Macro 接收包含 Attack1..5 的 `action_combo` intent
- **THEN** Macro MUST 产出外层 Attack State、Attack state body 内的 StateMachineNode、内层 Attack1..5/Exit、四条相邻 combo transition 和显式 Attack5-to-Attack1 loop transition 的 Patch IR
- **AND** 每个 combo request 查询 MUST 与 source Cancel window 位于同一内层 ConditionRuleGraph
- **AND** 每个具体攻击 state MUST 继续产出 Action Context、Timeline 和 lifecycle 节点
- **AND** 每个 leaf MUST 能表达独立 MoveCancel 条件，且 Attack request transition MUST 比同 source MoveCancel transition 优先

#### Scenario: 展开方向闪避

- **WHEN** Macro 接收包含 DodgeBack 与 DodgeForward 的 `directional_dodge` intent
- **THEN** Macro MUST 产出外层 Dodge State、Dodge state body 内的 StateMachineNode和两个 direction leaf
- **AND** 内层 Entry MUST 使用正式 MoveAxis threshold 条件分别选择两个 leaf
- **AND** 内层 Entry MUST NOT 重复查询已由外层接受的 Dodge request
- **AND** 外层 entry MUST 只查询 Dodge request，目标 leaf activation MUST 消费该 request

#### Scenario: 展开 Dodge 后摇与 RushAttack

- **WHEN** `directional_dodge` intent 声明 recovery cancel 与 RushAttack
- **THEN** Macro MUST 为两个 Dodge leaf 生成后摇内的 Attack、再次 Dodge、移动与自然完成路径
- **AND** RushAttack MUST 位于 Attack 内层而非外层 Action StateMachine
- **AND** 同 Tick 优先级 MUST 为 Attack、Dodge、移动、自然完成

### Requirement: 通用Agent Validator与业务样例覆盖必须分层

`AgentGraphValidator` MUST只检查对任意Character Definition成立的Graph kind、Condition纯度、Timeline ownership、serialized owner/path、TreeClip ownership、Action Context、Input/ActionProfile引用、authoring identity和正式Compiler语义。它 MUST不读取Definition名称，不得硬编码Corin、状态display name、连招数量、cancel key或具体transition集合。具体Macro和指定角色作者结构的业务覆盖 MUST由Synthesis/Macro coverage evaluator在对应样例范围内检查typed command plan或只读 Snapshot，MUST不进入普通`validate` action。

#### Scenario: 验证非Corin角色

- **WHEN** 作者验证一个使用不同Action状态名和不同连招层数的合法角色
- **THEN** 通用Validator MUST只按正式authoring语义判断
- **AND** MUST不要求`None/Attack/Dodge`、`Attack1..5`、`DodgeBack/DodgeForward`或Corin ownership key

#### Scenario: 评估action_combo Macro

- **WHEN** Synthesis Evaluator评估五段`action_combo`
- **THEN** Macro coverage evaluator MUST检查typed plan包含外层Attack、内层combo、五个普通攻击leaf、五个Timeline、四条相邻combo、显式末段回首段和每段MoveCancel
- **AND** 未启用loop的样例 MUST检查不存在末段回首段的额外combo transition
- **AND** 该检查 MUST只影响当前样例coverage report

#### Scenario: 评估Corin当前作者结构

- **WHEN** Synthesis Evaluator以Corin业务样例检查当前只读Snapshot
- **THEN** 业务coverage MUST检查outer Action只含None/Attack/Dodge、Attack含五个普通攻击leaf与RushAttack、Dodge含两个direction leaf
- **AND** MUST检查Attack5-to-Attack1、Dodge recovery重入、Dodge-to-RushAttack和MoveCancel职责
- **AND** MUST检查root-owned `HasActionLocomotionOwnership`、`ResumeLocomotionThroughRunEnd`和ActionOverride返回职责
- **AND** MUST拒绝样例Snapshot中仍存在`IsDodging`
- **AND** 普通Graph validate MUST不执行这些Corin业务规则
