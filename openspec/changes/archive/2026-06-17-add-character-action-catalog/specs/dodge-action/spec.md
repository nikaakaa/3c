## ADDED Requirements

### Requirement: Dodge 通过 Action Catalog 配置
`Action.Dodge` 的正式动作逻辑配置 MUST 通过 Character Action Catalog 或批准的等价 ActionSet 进入运行时。Dodge 的 Directional/Backstep variant、duration、distance、priority、resistance、rotateToDirection 和动作动画 key seed MUST 能从该 catalog entry 或其正式子配置追踪。`CharacterConfigSO.DodgeAction` MUST NOT 作为正式 gameplay 解析入口或缺失 catalog 时的 fallback。

#### Scenario: Dodge definition 包含两个变体
- **WHEN** 设计者检查 `Action.Dodge` definition
- **THEN** definition MUST 包含 Directional variant 配置
- **AND** MUST 包含 Backstep variant 配置
- **AND** 两个 variant MUST 都能配置 duration、distance、priority、resistance 和 rotateToDirection
- **AND** 缺失任一必要字段 MUST 被配置校验报告

#### Scenario: Directional Dodge 行为保持
- **GIVEN** Action Catalog 包含有效 `Action.Dodge` definition
- **AND** 输入缓冲中存在 Dodge 输入且当前移动事实支持 directional dodge
- **WHEN** 通用 provider/resolver 路径处理该请求
- **THEN** Dodge resolver MUST 输出 directional dodge resolved action
- **AND** accepted 后进入的 target state、request fact、motion seed 和 animation key seed MUST 与迁移前一致

#### Scenario: Backstep Dodge 行为保持
- **GIVEN** Action Catalog 包含有效 `Action.Dodge` definition
- **AND** 输入缓冲中存在 Dodge 输入且当前移动事实支持 backstep
- **WHEN** 通用 provider/resolver 路径处理该请求
- **THEN** Dodge resolver MUST 输出 backstep dodge resolved action
- **AND** accepted 后进入的 target state、request fact、motion seed 和 animation key seed MUST 与迁移前一致

#### Scenario: 缺失 catalog 不使用旧 Dodge 字段
- **GIVEN** `CharacterConfigSO` 缺失 Action Catalog
- **OR** Action Catalog 缺失 `Action.Dodge` definition
- **WHEN** 正式 gameplay 路径尝试处理 Dodge 输入
- **THEN** 系统 MUST 报告配置错误或拒绝动作输出
- **AND** MUST NOT 从 `CharacterConfigSO.DodgeAction`、Resources、全局单例或代码默认值继续运行

