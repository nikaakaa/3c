## MODIFIED Requirements
### Requirement: FullBody 动作配置参数
系统 MUST 通过正式动作逻辑配置资产或批准的等价数据源提供 Directional 和 Backstep 的距离、时长、优先级、抗性和旋转策略。正式 gameplay 路径 MUST NOT 通过代码 fallback、状态机旧 `output` 字段或场景临时字段补齐缺失的动作手感参数。

#### Scenario: 配置提供 Directional 参数
- **WHEN** 设计者配置 Directional 变体
- **THEN** 配置 MUST 能表达 duration、distance、priority、resistance 和 rotateToDirection
- **AND** Directional 默认正式配置 MAY 使用约 0.35s、4m、priority 30、resistance 20、rotateToDirection true
- **AND** 这些参数 MUST 能从 Action 逻辑配置入口追踪

#### Scenario: 配置提供 Backstep 参数
- **WHEN** 设计者配置 Backstep 变体
- **THEN** 配置 MUST 能表达 duration、distance、priority、resistance 和 rotateToDirection
- **AND** Backstep 默认正式配置 MAY 使用约 0.30s、2m-2.5m、priority 30、resistance 20、rotateToDirection false
- **AND** 这些参数 MUST 能从 Action 逻辑配置入口追踪

#### Scenario: 缺失配置不 fallback
- **GIVEN** 正式 Action 逻辑配置缺失 Dodge motion 参数
- **WHEN** 系统尝试构建 Dodge motion 输出
- **THEN** 系统 MUST 报告配置错误或拒绝该动作输出
- **AND** MUST NOT 使用代码内置默认值、状态机旧 `output` 字段、场景临时字段或 Resources 资产继续运行

#### Scenario: 非法配置被校验报告
- **GIVEN** 配置中存在负时长、负距离、负优先级或负抗性
- **WHEN** 系统校验动作配置
- **THEN** 校验 MUST 报告对应问题
- **AND** 正式 gameplay 路径 MUST NOT 静默把非法值改成另一套隐藏默认手感

#### Scenario: 状态机不复制动作手感参数
- **WHEN** 设计者检查 `FullBody/Action/Dodge` 状态节点
- **THEN** 状态机节点 MAY 保存 action state id、variant key、animation key、request/timeline/output module 绑定
- **AND** 状态机节点 MUST NOT 并行保存决定 Directional 或 Backstep motion duration/distance 的第二套正式参数
