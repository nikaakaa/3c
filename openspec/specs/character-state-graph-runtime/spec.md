# character-state-graph-runtime Specification

## Purpose
定义项目自研状态图作为领域局部实现的运行时合同。状态图可以服务 Locomotion、复杂 Action 或后续已审批领域，但不再表达旧的角色级统一大层级状态机树；角色级合成、并行领域协调和副作用执行归 `CharacterFramePipeline` 与 Character Graph/NodeTree 合同。

## Requirements
### Requirement: 状态图只作为领域局部实现
系统 MAY 使用自研状态图运行时表达单个领域内部的状态推进，例如 Locomotion 的 `Idle / MoveStart / MoveLoop / MoveStop / TurnBack`。状态图 MUST NOT 成为角色帧 owner、全局行为树、Action/Locomotion 共同父树或第二 gameplay pipeline。

#### Scenario: Locomotion 使用局部状态图
- **WHEN** Locomotion module 推进基础移动阶段
- **THEN** 它 MAY 使用状态图维护 `Locomotion.*` 状态
- **AND** 状态图 MUST 输出纯数据 snapshot、facts 或候选输出
- **AND** MUST NOT 直接执行 movement 或 animation

#### Scenario: Action 可选择非状态图实现
- **WHEN** `Action.Dodge` 或后续 Skill 只需要 lifecycle/timeline 表达
- **THEN** Action MAY 不使用状态图
- **AND** MUST 仍通过 Action runtime、body/channel claim 和 `CharacterFramePipeline` 参与角色输出

### Requirement: 多个领域图可在同一角色帧并行参与
系统 MUST 允许多个领域 runtime、局部 graph、timeline 或节点分支在同一角色帧中独立产出候选结果。并行参与不表示多条副作用路径；最终 motion、animation、input consume 和 facts 写入仍由角色级 frame plan 决定。

#### Scenario: Locomotion 与 Action 同帧提交
- **GIVEN** Locomotion graph 产出基础移动候选
- **AND** Action lifecycle 或局部 graph 产出 action claim
- **WHEN** `CharacterFramePipeline` 收集本帧输出
- **THEN** 两者 MAY 同帧作为 sibling domain 参与计划
- **AND** 最终互斥输出 MUST 由角色级计划选择

#### Scenario: 并行领域不互写状态
- **WHEN** Locomotion graph 与 Action timeline 同帧评估
- **THEN** 它们 MUST 通过只读 facts、候选输出、claim 或 frame result 交互
- **AND** MUST NOT 直接写入彼此 runtime state

### Requirement: Graph Model 只表达图语义
状态图模型 MUST 只表达节点、边、条件 key、初始状态、稳定 ID、可恢复 payload schema 和诊断 metadata。业务解释、motion/animation 副作用、Unity 对象和具体 evaluator implementation MUST 位于领域 metadata、solver、adapter 或输出阶段。

#### Scenario: 节点不持有 Unity 对象
- **WHEN** 加载任意状态图 definition
- **THEN** 节点和边 MUST NOT 持有 `Transform`、`CharacterController`、`Animator`、Animancer runtime object、`InputAction`、`MonoBehaviour` 或 scene instance

#### Scenario: 条件以 key 或数据引用表达
- **WHEN** graph transition 需要判断输入、窗口或领域事实
- **THEN** transition MUST 保存 condition key、fact id 或纯数据引用
- **AND** MUST NOT 直接保存 Action/Locomotion evaluator implementation

### Requirement: Runner 核心职责收窄
状态图 runner MUST 只负责解释图、求值 transition、维护 active state、state time、variant、pending transition、state payload 和纯数据 snapshot/restore。Timeline facts 采样、状态输出解析、运动命令构建、动画请求构建、输入消费、run latch 写入和诊断提交 MUST 位于明确外围模块或子职责。

#### Scenario: Runner 不执行副作用
- **WHEN** 状态图 runner tick 一帧
- **THEN** runner MUST 返回纯数据 frame、snapshot 或 transition trace
- **AND** MUST NOT 调用 motion executor、animation presenter、input buffer consume、RuntimeDiagnosticLog 或 Unity scene mutation API

#### Scenario: Runner 不拥有角色级并行
- **WHEN** 多个领域图或节点分支需要同帧评估
- **THEN** runner MUST 只推进自己所属领域的图
- **AND** MUST NOT 直接调度其它领域 graph 或决定角色级输出顺序

### Requirement: Timeline Facts 与输出解析在外围
状态图运行时 MAY 消费由外部 sampler 提供的 current/projected/target timeline facts，也 MAY 产出需要后续解析的状态输出意图；但 runner 自身 MUST NOT 采样动画播放进度、计算动作位移、播放动画或写 runtime blackboard。

#### Scenario: 请求仲裁和 transition 使用同一 current facts
- **GIVEN** 本帧角色上下文已有 current `StateTimelineWindowFacts`
- **WHEN** Action 请求仲裁和 Locomotion transition 都需要窗口事实
- **THEN** 它们 MUST 消费同一帧 current facts
- **AND** 状态图 runner MUST NOT 自行伪造第二份窗口事实

#### Scenario: 输出解析不成为 motion solver
- **WHEN** 状态图 active state 产出 motion intent 或 animation intent
- **THEN** 输出解析 MUST 只生成纯数据意图
- **AND** 本帧位移数学和执行 MUST 由 motion resolver/output applier 完成

### Requirement: Snapshot 与诊断 View 分离
状态图 snapshot MUST 只保存恢复所需的 active id、state time、variant、payload、pending transition 和必要 trace。FullBody owner、旧 path、Action/Locomotion 兼容视图或调试标签 MAY 从领域 metadata 和 frame plan 派生，但 MUST NOT 成为 snapshot 核心权威。

#### Scenario: Snapshot 保持纯状态事实
- **WHEN** 捕获 Locomotion graph snapshot
- **THEN** snapshot MUST 保存恢复所需的状态推进事实
- **AND** MUST NOT 保存 Unity 对象、动画 runtime object 或 FullBody owner

#### Scenario: View 不反向决定仲裁
- **WHEN** 诊断面板显示当前 Locomotion state、Action state 或 body claim
- **THEN** view MAY 从 snapshot、Action facts 和 frame plan 派生
- **AND** view MUST NOT 写回 transition、Action 仲裁或 body arbitration

### Requirement: 领域 ID 替代旧跨领域路径
正式状态或动作身份 MUST 使用稳定领域 ID。Locomotion 状态使用 `Locomotion.*`，Action 状态或 resolved action 使用 `Action.*`。系统 MUST NOT 要求这些 ID 位于同一棵角色级层级状态机树；旧 `FullBody/Locomotion/*` 和 `FullBody/Action/*` 只能作为 legacy 迁移输入或历史断言出现。

#### Scenario: 新配置使用领域 ID
- **WHEN** 创建新的 Locomotion graph、Action definition 或测试 fixture
- **THEN** 配置 MUST 使用 `Locomotion.Idle`、`Locomotion.MoveLoop`、`Locomotion.TurnBack` 或 `Action.Dodge` 等领域 ID
- **AND** MUST NOT 把 `FullBody/Locomotion` 或 `FullBody/Action` 作为正式路径

#### Scenario: 旧路径只做迁移
- **WHEN** 系统加载旧配置或旧测试 fixture
- **THEN** MAY 将旧 path 转换为领域 ID
- **AND** runtime MUST NOT 将旧 path 作为正式状态权威

### Requirement: 状态图运行时可测试和可验证
系统 MUST 为状态图运行时提供自动测试和静态边界验证，证明 graph model、runner、snapshot、timeline facts 输入和输出解析边界不会恢复旧统一大层级状态机或第二执行路径。

#### Scenario: 自动测试覆盖 runner 边界
- **WHEN** 运行状态图 EditMode 测试
- **THEN** 测试 MUST 覆盖 transition、state time、payload、snapshot/restore 和无效配置
- **AND** MUST 覆盖 runner 不直接执行 movement、animation 或 blackboard write

#### Scenario: 静态验证旧口径退役
- **WHEN** 检查当前 specs、生产源码和测试断言
- **THEN** 验证 MUST 确认新增正式配置不依赖旧 FullBody path
- **AND** MUST 确认状态图运行时不作为角色级统一大树或独立 gameplay pipeline
