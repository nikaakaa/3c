## ADDED Requirements

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
