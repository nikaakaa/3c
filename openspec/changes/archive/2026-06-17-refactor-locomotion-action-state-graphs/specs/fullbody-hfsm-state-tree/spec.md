# fullbody-hfsm-state-tree Delta

## MODIFIED Requirements
### Requirement: FullBody 分层 HFSM 状态树
系统 MUST 将旧 FullBody 分层 HFSM 状态树视为已退役语义。默认 Corin 运行时 MUST NOT 要求一棵 FullBody HFSM 主树表达 `FullBody/Locomotion` 与 `FullBody/Action/Dodge`，正式状态与输出 MUST 由 Character frame pipeline 下的 Locomotion local graph、Action lifecycle 和 frame plan 共同表达。

#### Scenario: 不再使用 FullBody HFSM 主树作为权威
- **WHEN** 本变更归档后查看正式角色状态权威
- **THEN** 系统 MUST NOT 要求 FullBody HFSM 包含 `FullBody/Action/Dodge`
- **AND** Locomotion phase MUST 来自 Movement module 的 Locomotion graph
- **AND** Action active state MUST 来自 Action lifecycle

## REMOVED Requirements
### Requirement: Locomotion 子树映射
该要求被 `Locomotion graph 归属 Movement module` 取代。Locomotion phase 不再需要映射进同一棵 FullBody HFSM 主树。

#### Scenario: Locomotion 不再作为 FullBody HFSM 子树
- **WHEN** 正式角色推进 Locomotion
- **THEN** Locomotion graph MUST 作为 Movement module 的局部 implementation 推进
- **AND** MUST NOT 要求 `/FullBody/Locomotion/*` 路径作为 gameplay 权威

### Requirement: Action.Dodge 子状态映射
该要求被 Action lifecycle 与 Dodge request/resolver 路径取代。`Action.Dodge` 保留为稳定 Action id，但不得作为默认 Locomotion graph 或 FullBody HFSM 主树叶子。

#### Scenario: Dodge 不再进入 FullBody HFSM Action 路径
- **GIVEN** 输入缓冲存在有效 Dodge 请求
- **AND** Action 仲裁接受该请求
- **WHEN** 正式角色推进本帧
- **THEN** Action lifecycle MUST active `Action.Dodge`
- **AND** 默认 graph path MUST NOT 进入 `/FullBody/Action/Dodge`

### Requirement: FullBody 状态快照
该要求被 Character frame runtime snapshot、Movement runtime facts、Action lifecycle restore state 和诊断 view 取代。兼容 view MAY 派生 FullBody 可读信息，但不得成为权威状态。

#### Scenario: 快照权威分散到正式模块
- **WHEN** 系统 capture 或 debug 当前角色状态
- **THEN** Locomotion facts MUST 来自 Movement runtime
- **AND** Action facts MUST 来自 Action lifecycle 或 action output
- **AND** 兼容 FullBody view MUST NOT 反向决定 transition 或输出应用

### Requirement: HFSM 与输出权威分离
该要求被 Character frame pipeline 的提交、计划、输出应用边界取代。不再维护 FullBody HFSM 作为状态路径权威。

#### Scenario: 输出权威归属 Character frame pipeline
- **WHEN** Action 或 Locomotion 产生运动、动画、输入消费或 Run latch 输出
- **THEN** 输出 MUST 通过 Character frame pipeline 的 composer/applier 应用
- **AND** FullBody HFSM MUST NOT 作为正式输出权威存在

### Requirement: 可测试和可验证
该要求的验证目标被 Locomotion graph、Action lifecycle、BodyArbiter、Run latch output 和 rollback replay 测试取代。

#### Scenario: 测试目标迁移
- **WHEN** 自动测试覆盖本变更
- **THEN** 测试 MUST 覆盖 Locomotion graph 不含 Action 节点
- **AND** MUST 覆盖 Action lifecycle active Dodge
- **AND** MUST 覆盖 Directional Dodge 完成写 Run latch 与 Backstep 不写 Run latch
- **AND** MUST NOT 要求 Play Mode debug 显示 `/FullBody/Action/Dodge`
