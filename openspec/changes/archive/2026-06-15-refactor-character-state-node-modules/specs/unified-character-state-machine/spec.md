## ADDED Requirements
### Requirement: 状态节点能力模块模型
系统 MUST 将角色状态节点表达为统一节点关系加能力模块集合。节点核心 MUST 只表达稳定状态 ID、父节点、路径片段、标签和模块列表；Locomotion phase、动作请求、位移、动画、timeline、输入消费、run latch 和特殊 motion policy 等能力 MUST 通过模块或等价模块数据表达。系统 MUST NOT 长期使用一个包含所有能力字段的万能节点作为正式配置模型。

#### Scenario: 节点关系保持统一
- **WHEN** 设计者配置 `FullBody/Locomotion/MoveLoop` 和 `FullBody/Action/Dodge`
- **THEN** 两者 MUST 共享同一种节点关系模型
- **AND** MUST 都能通过 `stateId`、`parentStateId`、`pathSegment` 或等价字段表达树关系
- **AND** MUST NOT 需要不同节点类才能参与同一张状态图 transition

#### Scenario: 能力通过模块表达
- **WHEN** 设计者配置 `Dodge`
- **THEN** Dodge 的动作请求、动作位移、动作动画和输入消费 MUST 来自模块或等价模块数据
- **AND** 普通 `MoveLoop` MUST NOT 暴露无效的 Dodge 动作位移字段
- **AND** 分组节点 MUST NOT 暴露无效的 motion 或 animation 配置字段

#### Scenario: 旧万能字段不得成为双权威
- **WHEN** 默认状态机资产完成模块迁移
- **THEN** 运行时 MUST 只读取模块配置作为状态能力来源
- **AND** 旧 `output`、`animation`、`variants` 或等价万能字段 MUST NOT 与模块配置并行决定同一输出

### Requirement: 输出通道替代互斥 owner 分支
系统 MUST 将状态帧输出表达为 motion、animation、input、latch、timeline、runtime facts 等输出通道或等价纯数据结果。`Locomotion / Action` MAY 作为诊断或兼容事实从模块输出派生，但 MUST NOT 作为决定是否执行 motion、是否播放 animation、是否消费输入的互斥运行时分支权威。

#### Scenario: Action 动画由输出通道驱动
- **WHEN** 当前节点通过模块产出动作动画请求
- **THEN** FullBody pipeline MUST 根据 animation output channel 或等价输出播放动作动画
- **AND** MUST NOT 仅通过 `Owner.IsAction` 判断是否播放动作动画

#### Scenario: Locomotion 动画由模块事实驱动
- **WHEN** 当前节点通过 Locomotion phase 模块产出基础移动表现请求
- **THEN** 动画 adapter MUST 使用 phase 与运行时 gait facts 解析具体基础移动动画
- **AND** 状态节点 MUST NOT 直接配置 Walk/Run 作为逻辑子状态

#### Scenario: 兼容 owner 只读派生
- **WHEN** 诊断或旧测试读取当前 owner
- **THEN** owner MAY 从当前节点模块组合派生
- **AND** 派生 owner MUST NOT 反向决定状态图 transition 或输出系统分支

### Requirement: 模块组合校验
系统 MUST 校验状态节点模块组合的合法性，确保每个模块有明确输出或事实用途，并防止同一职责出现多个权威来源。校验 MUST 覆盖默认状态机资产，并对无效组合报告明确错误。

#### Scenario: Dodge 模块组合合法
- **WHEN** 校验默认 `Dodge` 节点
- **THEN** 节点 MUST 包含动作请求、动作位移、动作动画和输入消费能力
- **AND** 每个 Dodge 变体 MUST 能解析到稳定 animation key
- **AND** 动作位移时长和距离 MUST 只有一个正式配置来源

#### Scenario: TurnBack alias 不重复
- **WHEN** 校验默认 `TurnBack` 节点
- **THEN** timeline binding、motion policy 和 animation alias MUST 共享同一正式 alias 来源或明确映射
- **AND** 状态机资产 MUST NOT 同时要求设计者在两个字段重复填写 `Locomotion.Turn.Back`

#### Scenario: 普通 Locomotion 不携带无效动画模块
- **WHEN** 校验 `Idle`、`MoveStart`、`MoveLoop`、`MoveStop`
- **THEN** 这些节点 MUST NOT 要求配置 action animation key
- **AND** MUST NOT 暴露或读取无效的 action movement 模块

### Requirement: 当前 runner 对模块模型的支撑边界
系统 MUST 在现有自研统一状态图 runner 上实现节点模块模型，而不是新增第二套状态机 runtime。现有 runner MAY 继续负责 active state、state time、variant、transition、pending path 和 restore；模块解析、输出聚合和事实采样 MUST 保持纯数据并位于明确 solver 子职责中。

#### Scenario: 保留单一 runner owner
- **WHEN** 模块化节点配置接入运行时
- **THEN** `PlayerFullBodyActionController` 或等价正式入口 MUST 继续是唯一正式 runner owner
- **AND** 系统 MUST NOT 新增 parallel ECS state runner、per-action runner 或独立 Locomotion runner

#### Scenario: Runner 不知道具体模块副作用
- **WHEN** runner 推进一帧状态
- **THEN** runner MUST NOT 直接播放 Animancer
- **AND** MUST NOT 直接执行 movement
- **AND** MUST NOT 直接消费 Unity 输入对象
- **AND** 模块输出 MUST 通过 FullBody pipeline adapter 执行副作用
