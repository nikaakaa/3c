# character-input-pipeline Specification

## Purpose
定义角色输入管线：`CharacterInputProfile` 将 Unity InputAction映射为 gameplay input value和 action request，`UnityCharacterSimulationInputAdapter`负责表现帧采样并生成 portable `CharacterSimulationInput`，Program state slots负责 request buffer与消费；预测历史只属于需要它的 Network Model SnapshotParticipant。
## Requirements
### Requirement: 动作目标候选必须作为 portable typed input 进入 Simulation

系统 MUST将可选`ActionTargetSnapshot`作为`CharacterSimulationInput.Values`的正式typed value kind，通过稳定InputId表达TargetId、position、yaw和有效性。Float32、Fixed、canonical codec、GameplayHash、ServerAuthoritative input command、DeterministicRollback input history与replay MUST保存同一业务字段和顺序。系统 MUST不创建Target专用packet、第二input buffer或Scene对象引用。

#### Scenario: 当前输入帧没有目标

- **WHEN** provider不可用或明确返回无目标
- **THEN** 当前输入 MUST保存显式None
- **AND** 输入层 MUST不延续上一帧目标或按Scene搜索替代目标

#### Scenario: Rollback 预测缺少精确输入

- **WHEN** DeterministicRollback为缺失输入构造predicted frame
- **THEN** 目标候选 MUST按正式预测规则成为None
- **AND** MUST不把上一个已知目标快照跨帧延续

### Requirement: Neutral Input 必须从 Program 输入目录生成

Neutral Input Source MUST依据已验证Program input catalog为每个continuous input value生成类型正确的neutral值，并始终生成空request集合。它 MUST覆盖Bool、Scalar、Vector2、Vector3、Yaw与`ActionTargetSnapshot`，MUST不按Corin输入名称硬编码，也 MUST不读取Unity InputAction、Camera、Scene或Character名称。

#### Scenario: Neutral Actor 生成一帧输入

- **WHEN** Neutral Actor进入一个Logic Tick
- **THEN** Neutral Input Source MUST生成完整且类型匹配的CharacterSimulationInput
- **AND** target candidate MUST为None，request集合 MUST为空

### Requirement: CharacterInputProfile 映射 InputAction 到 gameplay 输入值和动作请求
系统 MUST 使用 `CharacterInputProfile` 表达角色输入配置。Profile MUST 引用正式 `InputActionAsset`，并将 action 稳定身份映射为 gameplay input value 或 action request。Gameplay 逻辑 MUST 使用稳定 gameplay input id 或 request id，不得直接使用 InputAction 显示名。Profile、Graph 和输入 frame 的正式口径 MUST NOT 使用 `signal` 作为连续输入概念。

#### Scenario: 配置连续输入值
- **WHEN** Profile 将 `Player/Move` action 映射为 `MoveAxis` input value
- **THEN** 输入层 MUST 使用 action identity 读取来源
- **AND** gameplay、Tree 和 Motion 模块 MUST 使用 `MoveAxis` input value id
- **AND** 系统 MUST NOT 将该输入称为 signal 或 network command

#### Scenario: 配置动作请求
- **WHEN** Profile 将 `Player/Fire` action 映射为 `Attack` action request
- **THEN** 输入层 MUST 在该 action 触发时产生 `Attack` action request
- **AND** 后续动作管线 MUST 不直接依赖 `Player/Fire` 名字

#### Scenario: 来源 action 缺失
- **WHEN** Profile 中的 action identity 无法在来源 asset 中解析
- **THEN** 输入层 MUST 报告配置错误
- **AND** 输入层 MUST NOT 回退为按显示名查找

### Requirement: Unity Input Adapter 每 tick 产出 CharacterSimulationInput

`UnityCharacterSimulationInputAdapter` MUST在本地采样边界将 CharacterInputProfile/InputAction、Camera-relative direction和离散 request转换为 portable `CharacterSimulationInput`。Adapter、Source port、Pipeline product、Kernel和 Program之间 MUST只传递 portable input contract，MUST不保留第二个 `CharacterInputFrame`运行合同。

#### Scenario: 采样移动与闪避

- **WHEN** Input Adapter 读取 Move 和 Shift request
- **THEN** MUST按当前 Session NumericProfile 生成带稳定 InputId、target scalar/vector value、request sequence 和 source tick 的 CharacterSimulationInput
- **AND** Local Float32 Adapter MUST不预先量化为DeterministicRollback的FixedQ32.32格式

### Requirement: 连续输入作为 input value 保存且不消费
系统 MUST 将 MoveAxis、LookAxis、AimAxis、SprintHeld 等连续或保持型输入保存为 typed input value。Input value MUST 每 tick 覆盖当前值，MUST NOT 进入 request 消费语义，也 MUST NOT 在 Graph/BTSMTL 中命名为 command。

#### Scenario: 移动输入进入预测
- **WHEN** `MoveAxis` input value 在当前 tick 读取到 Vector2 值
- **THEN** `CharacterSimulationInput.Values` MUST 保存该 `MoveAxis` input value
- **AND** Locomotion 或 Motion 模块 MAY 使用该 input value 立即驱动本地表现

#### Scenario: 按住输入不被消费
- **WHEN** `SprintHeld` 在多帧中保持 true
- **THEN** 每个 tick 的 `CharacterSimulationInput` MUST 能读取该 input value
- **AND** 读取该 input value MUST NOT 将其标记为 consumed

### Requirement: 离散动作输入进入 request buffer

系统 MUST将Attack、Dodge、Jump、Interact等离散动作输入编译进`CharacterSimulationInput.Requests`，并由Program声明的typed `InputRequestBuffer` state address维护可查询、可消费的committed状态。每个request MUST保存sequence、source tick、expire simulation tick、priority与consumed状态；request id MUST由Program Layout稳定绑定。写入、查询、过期与消费 MUST通过当前Character State Transaction的Input state port完成，不得创建第二个request buffer、opaque bytes镜像或每Tickrequest codec。

#### Scenario: 硬直中预输入攻击

- **WHEN** 玩家在当前状态不可攻击时触发`Attack`
- **THEN** Input Adapter MUST将`Attack`写入`CharacterSimulationInput.Requests`，Program MUST将其写入对应typed request state
- **AND** 该request MUST在配置的buffer时间内保持可查询

#### Scenario: 请求过期

- **WHEN** `Attack` request超过配置的buffer时间仍未被消费
- **THEN** request buffer MUST将该typed request视为不可用
- **AND** 后续查询 MUST NOT返回该过期request

#### Scenario: 请求被消费

- **WHEN** 状态行为或动作管线正式接受`Dodge` request
- **THEN** request buffer MUST在当前State Transaction中将该request标记为consumed
- **AND** 同一request MUST NOT被第二次消费

### Requirement: GraphContext 读取同一输入帧和请求缓存

Compiled input operation MUST从当前Actor input与CharacterSimulationState typed request buffer读取连续值和离散请求，并通过Program级request index访问预验证地址。Operation MUST不读取CharacterGraphContext、Unity InputAction、Camera、mutable CharacterPipelineFrame或执行runtime bytes decode。

#### Scenario: Attack request 被消费

- **WHEN** compiled Action operation 消费当前 Attack request
- **THEN** MUST通过 request identity 更新 CharacterSimulationState buffer

### Requirement: Network Model 必须从正式输入与 Tick Result构造自己的命令

Network Model Source与Pass MUST只从CharacterSimulationInput、SimulationTickResult、SimulationWorldSnapshot和正式Source products构造自己的packet/history。Program、Kernel和Unity Input Adapter MUST不保存packet、model policy或correction metadata。

#### Scenario: ServerAuthoritative 构造命令

- **WHEN** Model Source或 Egress Pass需要生成 canonical input command
- **THEN** MUST从 portable Actor input 与 Tick identity 映射
- **AND** MUST不读取 authoring node 或 InputAction

### Requirement: 输入历史只属于需要预测重放的 Model Source 或 Pass

Input history MUST不再由公共CharacterInputStage、Program Runtime或标准Pipeline默认拥有。Local Session Source与Standard Local Pipeline MUST不创建replay history；ServerAuthoritative Prediction与DeterministicRollback MUST在自己的Source或明确有状态Pipeline Pass中保存匹配Numeric ABI的input history，并声明ExternalSource或SnapshotParticipant所有权。

#### Scenario: Local Pipeline 提交输入

- **WHEN** Standard Local Pipeline完成本次 SimulationStep
- **THEN** Core MUST不创建 model history或假 rollback buffer
- **AND** Local Source MUST不保留未声明的 replay state

### Requirement: 动作 Request 必须由 Authoring 声明业务 Timing Class

`CharacterActionRequestDefinition` MUST为每个离散request保存稳定timing class，当前正式值为`Immediate`与`Offensive`。Timing class MUST表达request的业务类别，不得保存具体Network Model、Tick延迟或packet policy。CharacterInputProfile Inspector与Agent authoring MUST读写同一字段；缺失或非法值 MUST作为配置错误，MUST不按request id、InputAction显示名或字符串前缀推断类别。

#### Scenario: 作者配置攻击请求

- **WHEN** 作者把Corin Attack request标记为Offensive
- **THEN** CharacterInputProfile MUST保存该timing class
- **AND** Agent snapshot与Inspector MUST读取同一配置

#### Scenario: 请求没有合法 Timing Class

- **WHEN** CharacterInputProfile包含未定义的timing class值
- **THEN** 配置校验 MUST失败
- **AND** Runtime MUST不回退为Immediate

### Requirement: Network Model 必须独立解释 Request Timing Class

Input Adapter MUST先捕获带稳定request identity、capture sequence和timing class的动作事实；具体eligible Tick MUST由当前Session Source或Network Model timing policy决定。Standard Local与Preview MAY将全部类别映射为0 Tick；DeterministicRollback MAY为Offensive配置固定Tick延迟。BTSMTL、Program、Kernel和CharacterSimulationState MUST只消费已经eligible并写入`CharacterSimulationInput.Requests`的正式request，MUST不读取Network Model policy。

#### Scenario: 单机调试同一 Corin 输入配置

- **WHEN** Standard Local Session使用标记为Offensive的Attack request
- **THEN** Local input adapter MAY在当前Tick立即写入该request
- **AND** BTSMTL与Program MUST不需要Rollback专用节点

#### Scenario: Rollback 调度 Offensive Request

- **WHEN** DeterministicRollback policy把Offensive映射为2 Tick
- **THEN** Rollback Source MUST在capture tick记录request并在eligible tick写入正式Fixed input
- **AND** 远端收到的仍是普通portable input request

#### Scenario: 连续输入与延迟请求并存

- **WHEN** MoveAxis持续更新且一个Offensive request仍在等待eligible tick
- **THEN** MoveAxis MUST每Tick立即进入CharacterSimulationInput.Values
- **AND** pending request MUST不阻塞连续输入传播

### Requirement: 离散 Request 调度必须保持捕获顺序

需要选择性延迟的Model Source MUST以request capture sequence维护有界pending schedule。后捕获request MUST不越过尚未eligible的前序request；request到期后 MUST保留原始request id与sequence并进入正式input history。Pending schedule影响未来模拟时 MUST进入该Model Source的checkpoint/restore合同，不得藏在Unity UI状态或建立第二个Gameplay request buffer。

#### Scenario: Attack 后立即输入 Dodge

- **WHEN** Offensive Attack仍在等待eligible tick且之后捕获Dodge request
- **THEN** Dodge MUST不越过Attack写入更早SimulationTick
- **AND** 两个request MUST保留各自capture sequence

#### Scenario: Restore 到 Request 尚未 Eligible 的 Tick

- **WHEN** Rollback Source恢复到pending request尚未写入input frame的历史点
- **THEN** Source MUST从checkpoint恢复相同pending schedule
- **AND** MUST不重新读取InputAction生成重复request

### Requirement: 玩家与AI必须产出同一CharacterSimulationInput合同

Character Control Source MAY来自Unity玩家设备、Neutral source或AIIntentProgram，但进入Session Source、Character Program与Network Model的正式合同 MUST始终是匹配Numeric Target的`CharacterSimulationInput`。AI source MUST使用Character Program input/request catalog构造typed values与requests，MUST NOT增加AICommand、BotAction、第二request buffer或Character专用AI节点。

Local Session Preparation MUST显式锁定每个Actor的Control Source identity、Numeric ABI、所需capability与Character Program binding。唯一Local Control Input Ingress MUST通过正式Committed Observation read port为需要World观察的source提供上一轮已提交Actor Body，并一次生成完整CanonicalInputBatch；`ISimulationInputAdapter`、AI source或CharacterPipelineHost MUST不自行查询Session、Scene或Presentation状态补齐观察。

#### Scenario: AI输出移动与攻击

- **WHEN** AIIntentProgram决定向目标移动并提交Attack
- **THEN** MoveAxis MUST进入CharacterSimulationInput.Values
- **AND** Attack MUST进入CharacterSimulationInput.Requests
- **AND** Character Program MUST按与玩家输入相同的operation读取它们

#### Scenario: 同一AI节点持续Running

- **WHEN** SubmitActionRequest节点在多个Tick属于同一activation
- **THEN** AI source MUST只生成一次离散request
- **AND** 新request MUST只由新的activation或显式repeat策略产生

#### Scenario: AI未写连续输入

- **WHEN** 当前AI Tick没有写某个Program声明的continuous input
- **THEN** source MUST按该typed input catalog生成neutral值
- **AND** MUST不延续上一Tick的MoveAxis或ActionTargetSnapshot

#### Scenario: AI需要读取目标Body

- **WHEN** AI Control Source准备当前Tick输入
- **THEN** MUST消费Local Control Input Ingress提供的CommittedActorObservationSnapshot
- **AND** MUST不从普通Input Adapter、Scene Transform或Actor presentation查询目标

#### Scenario: 玩家显式目标迁移到正式观察端口

- **WHEN** Corin玩家target selector读取显式绑定的目标Actor
- **THEN** 它 MUST从Local Control Input Ingress提供的CommittedActorObservationSnapshot解析Logic Body
- **AND** 玩家provider MUST不继续读取Actor registration Body缓存形成第二条观察路径

