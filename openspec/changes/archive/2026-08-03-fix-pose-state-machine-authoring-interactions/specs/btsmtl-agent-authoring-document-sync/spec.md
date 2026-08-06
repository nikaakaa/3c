## MODIFIED Requirements

### Requirement: Presentation分片必须保持整包同步与稳定owner

Document v3 MUST使用`editable/presentation/profile.json`、`editable/presentation/pose-graphs/<graph-id>/graph.json`、对应`layout.json`，以及`editable/presentation/pose-state-machines/<state-machine-id>/state-machine.json`与对应`layout.json`表达Presentation目标状态。Pose StateMachine的`state-machine.json` MUST只表达Entry、State、Alias、Transition、Rule与blend/sync语义；同目录`layout.json` MUST只稀疏表达合法Entry、State与Alias的稳定identity和有限二维位置。分片 MUST通过稳定owner identity互相引用，并继续服从整包checkout、hash、dry-run、apply、Conflict与反向导出语义；不得提供文件级apply、旧单文件闭包reader或缺失layout fallback。

#### Scenario: AI只修改一个Pose节点

- **WHEN** 仅一个Pose Graph分片发生语义变化
- **THEN** dry-run与apply MUST仍锁定并处理整个Document包
- **AND** 反向导出 MUST更新整包基线与规范文件清单

#### Scenario: checkout导出Pose StateMachine

- **WHEN** Character Document显式checkout包含一个正式Pose StateMachine
- **THEN** 规范包 MUST在同一stable segment目录输出`state-machine.json`与`layout.json`
- **AND** 两个文件 MUST使用相同StateMachine identity并共同进入manifest与document hash

#### Scenario: AI只移动一个Pose State

- **WHEN** AI只修改Pose StateMachine `layout.json`中一个合法State的位置
- **THEN** Reconciler MUST生成同一正式layout owner的typed Presentation Mutation
- **AND** apply MUST更新Undo、资产dirty与canonical package基线
- **AND** MUST不修改StateMachine `ContentRevision`或发布Program、Projection与Native Pose Program

#### Scenario: 旧闭包缺少StateMachine layout文件

- **WHEN** 工具升级前的Document v3 manifest只包含Pose StateMachine `state-machine.json`
- **THEN** dry-run与apply MUST拒绝该旧闭包并要求显式重新checkout
- **AND** MUST不补写文件、兼容读取旧形状或建立两种apply路径

