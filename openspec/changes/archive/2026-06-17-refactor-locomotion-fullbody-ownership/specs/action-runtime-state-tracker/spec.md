## MODIFIED Requirements

### Requirement: Action facts 同步权威

系统 MUST 以 Action domain facts、action instance state 或等价纯数据 action snapshot 作为当前 Action state 的直接事实来源。`ActionRuntimeStateTracker` 或等价 helper MAY 保存当前 Action facts，但 MUST 由 Action request resolver、Action lifecycle 或 Character frame pipeline 明确更新；它不得独立驱动角色帧、消费输入、执行运动、播放动画或决定 Locomotion 状态。

#### Scenario: Locomotion 派生为空 Action
- **GIVEN** 当前没有 active action instance
- **WHEN** FullBody Action 请求门面构建仲裁上下文
- **THEN** 当前 action state MUST 为 `Action.None`
- **AND** current resistance MUST 为 0
- **AND** 该事实 MUST NOT 依赖 `FullBodyOwnerKind.Locomotion`

#### Scenario: Dodge 派生为 Action.Dodge
- **GIVEN** Action domain 当前 active action 为 `Action.Dodge`
- **AND** Dodge 动作配置 resistance 为 40
- **WHEN** FullBody Action 请求门面构建仲裁上下文
- **THEN** 当前 action state MUST 为 `Action.Dodge`
- **AND** current resistance MUST 为 40
- **AND** 该事实 MUST NOT 依赖 `FullBody/Action/Dodge` 诊断路径

#### Scenario: tracker 不成为第二状态机
- **WHEN** 检查 `ActionRuntimeStateTracker` 或等价 helper 的运行时接入
- **THEN** 它 MUST NOT 调用 Locomotion 状态 transition
- **AND** MUST NOT 调用动画播放 API
- **AND** MUST NOT 直接读取或消费输入缓冲
- **AND** MUST NOT 因 duration、动画结束或隐藏规则自动退出当前 action

#### Scenario: Action facts 通过角色帧提交
- **WHEN** Action domain facts 在 tick N 发生变化
- **THEN** Action submitter MUST 将变化作为纯数据提交给 Character frame pipeline
- **AND** runtime facts 写入 MUST 仍发生在角色级 output apply 阶段
- **AND** Action facts MUST NOT 绕过 Character frame pipeline 写入 Unity 场景对象
