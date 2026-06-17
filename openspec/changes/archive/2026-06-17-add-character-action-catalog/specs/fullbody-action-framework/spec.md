## MODIFIED Requirements

### Requirement: FullBody Action 逻辑配置和动画绑定
系统 MUST 提供 Character Action Catalog 或批准的等价 ActionSet 作为角色可用 Action 的正式逻辑配置入口，聚合稳定 action id、request binding、运动参数、打断输入、lifecycle seed 和 variant 数据。动作动画表现 MUST 通过独立的动作动画绑定入口或等价边界按稳定 action id 解析。FullBody Action submitter MUST 显式引用动作逻辑配置和动作动画绑定配置，不得把动画 Profile 塞回动作逻辑定义。

#### Scenario: 角色级 Action Catalog
- **WHEN** 设计者检查角色 Action 逻辑配置
- **THEN** 系统 MUST 提供一个角色级 Character Action Catalog、ActionSet 或等价逻辑入口
- **AND** 该入口 MUST 能列出当前角色可用的 Action
- **AND** action id MUST 使用稳定 ID
- **AND** 该入口 MUST 通过 `CharacterConfigSO` 或等价角色配置根追踪

#### Scenario: Action 定义聚合逻辑子配置
- **WHEN** 设计者检查 `Action.Dodge` 或等价 Action 定义
- **THEN** 该定义 MUST 能定位动作运动参数配置
- **AND** MUST 能定位 request type、source input、priority 和 resistance
- **AND** MAY 定位打断策略集合或等价策略引用
- **AND** MUST NOT 直接持有动作动画 Profile

#### Scenario: 缺失配置可校验
- **GIVEN** Action Catalog 缺失必要 action id、request binding、运动参数或打断策略
- **WHEN** 运行配置校验
- **THEN** 校验结果 MUST 报告错误
- **AND** MUST 不要求设计者进入多个游离动作逻辑资产才能发现逻辑配置缺口
- **AND** runtime MUST NOT 使用旧 `DodgeAction` 平铺字段或代码默认值补齐缺口

#### Scenario: 动作动画绑定独立解析
- **WHEN** FullBody Action submitter 准备提交 `Action.Dodge` 动画命令
- **THEN** 系统 MUST 通过动作动画绑定集或等价动画配置入口解析 `Action.Dodge` 的动作动画 Profile
- **AND** 该绑定入口 MUST 能校验缺失 Profile 或必要动作动画 key
- **AND** 动作动画绑定入口 MUST NOT 定义 FullBody 状态树拓扑、动作进入条件或动作位移权威

#### Scenario: Locomotion 配置不并入 Action
- **WHEN** 设计者配置基础 Locomotion 状态图、Walk/Run alias 或 TransitionLibrary
- **THEN** 这些配置 MUST 仍属于 Locomotion 配置入口
- **AND** Action Catalog 和 Action 定义 MUST NOT 接管 `Idle / MoveStart / MoveLoop / MoveStop` 的 Locomotion 状态图规则

## ADDED Requirements

### Requirement: Action resolver 消费 Catalog 定义
FullBody Action request resolver MUST 从正式 Action Catalog 或等价 runtime action catalog 读取动作定义，再输出 `CharacterResolvedAction` 或等价动作结果。resolver MAY 使用动作专用策略处理 Dodge 方向、Attack 连段阶段或 Jump 起跳条件，但动作数值和 action id MUST 来自正式动作定义。

#### Scenario: Dodge resolver 使用 catalog 数值
- **GIVEN** Dodge 输入请求有效
- **AND** Action Catalog 包含 `Action.Dodge` definition
- **WHEN** Dodge resolver 解析该请求
- **THEN** Directional 与 Backstep 的 duration、distance、priority、resistance 和 rotateToDirection MUST 来自 catalog definition
- **AND** resolver MUST 保留现有 Directional/Backstep 行为语义
- **AND** resolver MUST NOT 从 `CharacterConfigSO.DodgeAction` 读取正式配置

#### Scenario: 新动作不修改仲裁主流程
- **WHEN** 后续新增 Attack、Jump 或 Skill 动作定义
- **THEN** 新动作 MUST 通过 Action Catalog definition 和对应 provider/resolver strategy 接入
- **AND** `CharacterActionRequestSubmissionArbiter` 或等价主流程 MUST NOT 新增直接面向具体动作的 target-state switch
- **AND** 新动作 MUST NOT 新增独立 MonoBehaviour gameplay 入口

