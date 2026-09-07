## MODIFIED Requirements

### Requirement: 玩家与AI必须产出同一CharacterSimulationInput合同

Character Control Source MAY来自Unity玩家设备、Neutral source或已显式绑定的插件行为，但进入Session Source、Character Program与Network Model的正式合同 MUST始终是匹配Numeric Target的`CharacterSimulationInput`。AI source MUST使用Character Program input/request catalog构造typed values与requests，MUST NOT增加AICommand、BotAction、第二request buffer或Character专用AI节点。

Local Session Preparation MUST显式锁定每个Actor的Control Source identity、Numeric ABI、所需capability与Character Program binding。唯一Local Control Input Ingress MUST通过正式Committed Observation read port为需要World观察的source提供上一轮已提交Actor Body，并一次生成完整CanonicalInputBatch；`ISimulationInputAdapter`、AI source或CharacterPipelineHost MUST不自行查询Session、Scene或Presentation状态补齐观察。

输入生产 MUST与消费区分：已冻结的本端输入、捕获身份和生产 frontier 不因角色回滚或外层事务恢复而倒退；模型重复请求同一输入 Tick MUST取得同一输入身份与内容。插件 MUST只为新输入 Tick 执行。观察中新增的请求/Action 结果 MUST只读、具有明确能力声明并来自原有状态所有者。

#### Scenario: AI输出移动与攻击

- **WHEN** 已显式绑定的插件行为决定向目标移动并提交Attack
- **THEN** MoveAxis MUST进入CharacterSimulationInput.Values
- **AND** Attack MUST进入CharacterSimulationInput.Requests
- **AND** Character Program MUST按与玩家输入相同的正式输入合同读取它们

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

#### Scenario: 回滚后再次读取已生产Bot输入

- **WHEN** 模拟恢复后需要已经冻结的 Bot Tick T 输入
- **THEN** Source MUST交付原 Tick、原输入/request sequence 和原 GameplayHash
- **AND** MUST不重新执行插件或再次捕获攻击

### Requirement: 输入历史只属于需要预测重放的 Model Source 或 Pass

Input history MUST不再由公共CharacterInputStage、Program Runtime或标准Pipeline默认拥有。Local Session Source与Standard Local Pipeline MUST不创建replay history；ServerAuthoritative Prediction与DeterministicRollback MUST在自己的Source或明确有状态Pipeline Pass中保存匹配Numeric ABI的input history，并声明ExternalSource或SnapshotParticipant所有权。

Source 为避免重复生产而保留的当前未完成输入事实 MUST明确声明 ExternalSource 所有权；Local MUST只保留当前消费/重试所需的有界记录，不因此创建世界 snapshot 或长期 replay history。网络输入事实 MUST复用其唯一模型历史，MUST不另建 Bot 历史镜像。

#### Scenario: Local Pipeline 提交输入

- **WHEN** Standard Local Pipeline完成本次 SimulationStep
- **THEN** Core MUST不创建 model history或假 rollback buffer
- **AND** Local Source MUST不保留未声明的 replay state

#### Scenario: Local模拟失败后保留已冻结输入

- **WHEN** 当前输入已冻结而本轮模拟事务失败
- **THEN** Source MUST保留该输入直到原失败策略决定消费或终止
- **AND** MUST不借此安装 Local 回滚 history

### Requirement: 离散 Request 调度必须保持捕获顺序

需要选择性延迟的Model Source MUST以request capture sequence维护有界pending schedule。后捕获request MUST不越过尚未eligible的前序request；request到期后 MUST保留原始request id与sequence并进入正式input history。Pending schedule影响未来模拟时 MUST进入该Model Source的checkpoint/restore合同，不得藏在Unity UI状态或建立第二个Gameplay request buffer。

捕获事件的身份与已经写入正式输入帧的结果 MUST作为不可改写输入事实保存。恢复 pending schedule MUST依据原捕获事实和原模型 timing policy，不得重新 Tick 插件、重新读取设备、回退已发布序号或改写已冻结帧。一个真实端点拥有多个 Actor 时，pending schedule MUST保持每 Actor 的身份隔离。

#### Scenario: Attack 后立即输入 Dodge

- **WHEN** Offensive Attack仍在等待eligible tick且之后捕获Dodge request
- **THEN** Dodge MUST不越过Attack写入更早SimulationTick
- **AND** 两个request MUST保留各自capture sequence

#### Scenario: Restore 到 Request 尚未 Eligible 的 Tick

- **WHEN** Rollback Source恢复到pending request尚未写入input frame的历史点
- **THEN** Source MUST从checkpoint恢复相同pending schedule
- **AND** MUST不重新读取InputAction生成重复request

#### Scenario: Bot攻击等待eligible期间发生恢复

- **WHEN** Bot 已捕获攻击且模型请求延迟尚未结束，输入消费状态发生恢复
- **THEN** MUST从原捕获事实恢复相同请求顺序与 eligible Tick
- **AND** MUST不让插件再次产生该请求或影响另一 Actor 的序号

## ADDED Requirements

### Requirement: 请求与动作结果必须从正式所有者提供只读关联

系统 MUST从正式输入请求状态与 Action 已提交结果提供按 ActorId、RequestId、capture sequence 关联的只读观察，区分缓冲中、过期、拒绝及对应 ActionInstance 的开始、完成、中断。结果保留 MUST由正式有界状态/结果所有者承担，不创建第二请求 buffer 或 Action 生命周期，MUST不通过动作名称、动画时间或私有地址猜测。

#### Scenario: 同一角色连续两次攻击

- **WHEN** 两个不同捕获序号的攻击先后提交
- **THEN** 观察 MUST将它们分别关联到自己的请求状态和实际释放
- **AND** 第一次动作完成 MUST不结束等待第二次请求的任务
