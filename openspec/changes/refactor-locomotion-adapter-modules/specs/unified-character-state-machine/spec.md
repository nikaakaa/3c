## ADDED Requirements
### Requirement: Locomotion 模块拆分不得恢复状态权威
系统 MUST 保持 `PlayerFullBodyActionController` 作为统一状态机 runner 的唯一正式运行时 owner。Locomotion adapter 拆出的任何模块都只能提供纯数据 facts、构建状态机 context 或转换状态机输出，不得创建、持有或推进第二个 `CharacterStateMachineRunner`。

#### Scenario: 拆出的模块不拥有 runner
- **WHEN** 检查 Locomotion adapter 拆分后的运行时代码
- **THEN** 只有 FullBody 主调度入口 MAY 创建 `CharacterStateMachineRunner`
- **AND** Locomotion facts、TurnBack、motion builder、snapshot 或 diagnostics 模块 MUST NOT 创建 runner
- **AND** 这些模块 MUST NOT 保存一套独立 active state path 作为状态权威

#### Scenario: Locomotion 仍通过 FullBody pipeline 被调用
- **WHEN** FullBody pipeline 推进一帧 gameplay
- **THEN** Locomotion 模块 MAY 提供移动事实和基础移动输出候选
- **AND** FullBody pipeline MUST 继续决定本帧是否提交基础移动 motion 和 base layer animation
- **AND** Locomotion 模块 MUST NOT 在 FullBody pipeline 外提交第二份输出

#### Scenario: 退役直驱入口不恢复
- **WHEN** 旧 `TickFromInputSource`、`TryEvaluateLocomotion` 或 `LocomotionTickAdapter` 仍存在于代码中
- **THEN** 它们 MUST 只作为迁移诊断或测试辅助存在
- **AND** 它们 MUST NOT 推进状态机 runner
- **AND** 它们 MUST NOT 提交 motion executor 或 base layer presenter
