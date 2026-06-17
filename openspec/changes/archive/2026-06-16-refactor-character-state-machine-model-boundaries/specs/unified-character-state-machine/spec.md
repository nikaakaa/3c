## ADDED Requirements
### Requirement: 状态机通用模型与角色业务模型分层
系统 MUST 将自研统一分层状态机的通用图模型与角色 FullBody 业务模型分层。通用图模型 MUST 只表达 state id、层级关系、path、transition、runtime active state、state time、variant、pending transition 和纯数据 snapshot/restore。Locomotion phase、Action state、Dodge、TurnBack、RunLatch、animation binding、motion spec、timeline policy 和 condition domain 等角色业务能力 MUST 位于 character metadata、capability module 或等价业务层模型中。

#### Scenario: Generic graph 不知道角色业务词
- **WHEN** 静态检查 generic graph model 和 runner core
- **THEN** 它 MUST NOT 引用 `Dodge`
- **AND** MUST NOT 引用 `TurnBack`
- **AND** MUST NOT 引用 `BasicMovementGait`
- **AND** MUST NOT 引用 `ActionMovementCommand`
- **AND** MUST NOT 引用 Unity scene object 或 Animancer runtime object

#### Scenario: Character metadata 派生 FullBody view
- **WHEN** 运行时需要读取当前 owner、Locomotion phase 或 Action state
- **THEN** 系统 MUST 从 character metadata/capability module 和 snapshot 派生 `FullBodyStateView` 或等价 view
- **AND** 派生 view MUST NOT 反向决定 transition 或成为第二状态权威

#### Scenario: 默认资产迁移保持行为
- **WHEN** 默认角色状态机资产迁移到分层模型
- **THEN** 节点层级、transition、timeline binding、输出能力和 snapshot/restore 语义 MUST 与迁移前等价
- **AND** EditMode tests MUST 覆盖 Idle、MoveStart、MoveLoop、MoveStop、TurnBack 和 Dodge 的关键路径

#### Scenario: Runner 保持单一权威
- **WHEN** 分层模型接入 runner
- **THEN** 正式运行时 MUST 仍只有 FullBody host 创建和推进一个 `CharacterStateMachineRunner`
- **AND** runner MUST NOT 执行 motion、animation、input consume 或 diagnostic submit 副作用
- **AND** Locomotion adapter、Action module 和 Presenter MUST NOT 创建第二 runner

### Requirement: Generic graph Interface 必须只表达图语义
系统 MUST 将 generic graph Interface 收窄到图拓扑、transition edge、active identity、state time、variant 和 pending transition 等通用语义。任何角色玩法解释 MUST 通过 character metadata、capability module 或 derived view 表达。

#### Scenario: Transition edge 不保存 domain evaluator
- **WHEN** generic transition model 表达条件
- **THEN** 它 MUST 保存 condition key/reference
- **AND** MUST NOT 保存 Action/Locomotion evaluator implementation
- **AND** MUST NOT 判断 `ActionCanExit` 或 `LocomotionAnimationCanExit`

#### Scenario: Snapshot 不保存业务解释作为权威
- **WHEN** runner 产出 generic snapshot
- **THEN** snapshot MUST 保存 active id/path、state time、pending transition 和 variant
- **AND** MUST NOT 将 owner、locomotion phase 或 action state 作为 generic authority
- **AND** character view MUST 从 metadata 派生这些解释

#### Scenario: Graph node 不持有 output module implementation
- **WHEN** generic node model 被加载
- **THEN** node MUST NOT 持有 motion executor、animation presenter 或 input consumer
- **AND** output module binding MUST 位于 character metadata/capability layer

### Requirement: Character capability metadata 必须是正式配置
系统 MUST 将角色状态能力建模为正式 metadata/capability module。需要的 timeline、output、condition domain、locomotion phase 和 action state 配置 MUST 明确存在并通过 validation 检查，不得通过 fallback 或命名约定偷偷推断。

#### Scenario: 缺失 required capability 会失败
- **WHEN** 默认状态机资产中某个节点需要 timeline、output 或 condition capability
- **AND** 对应 metadata 缺失
- **THEN** validation MUST 明确失败
- **AND** runtime MUST NOT 通过 fallback 配置继续运行

#### Scenario: FullBody view 是派生 Interface
- **WHEN** runtime 或测试读取 FullBody owner/phase/action view
- **THEN** view MUST 从 generic snapshot 和 character metadata 派生
- **AND** view MUST NOT 修改 snapshot
- **AND** view MUST NOT 选择 transition target

#### Scenario: 不为未知 layer 预设空模块
- **WHEN** 当前没有实现 UpperBody、HitReaction 或 Aim layer
- **THEN** capability metadata MUST NOT 添加未使用 placeholder module
- **AND** future layer MUST 通过独立 proposal 增加正式 metadata/result contract
