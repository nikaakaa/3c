## ADDED Requirements

### Requirement: 运行期间字段编辑资格必须由领域正式合同提供

领域 MUST根据共享 Capability 和正式参数合同，向所有作者入口一致提供运行可调参数、需要 Build 的作者字段、纯编辑布局与只读运行观察的区别。窗口 MUST不按字段名、数值类型或 C# 反射猜测可热更新能力。运行可调参数 MUST复用真实作者 Mutation、Validator、Undo 和正式运行端口；其输入范围和生效时机 MUST与领域原有合同一致。

#### Scenario: 同一字段从两个页面修改

- **WHEN** 作者分别从 Pose Graph 和动作工作区修改同一个合法参数
- **THEN** 两处 MUST使用同一编辑资格、Mutation 和运行应用规则
- **AND** MUST不形成页面专用字段白名单或保存副本

#### Scenario: 数值字段需要重新构建

- **WHEN** 一个数值字段会改变编译布局且没有正式运行更新合同
- **THEN** UI MUST将它明确标记为需要 Build 的作者字段
- **AND** MUST不因为它是数值就直接写入 Runtime

### Requirement: 作者参数编辑必须与运行观察保持独立

运行观察字段 MUST只读。场景预览期间合法作者参数 MUST能够在明确的作者区域修改并保留，但不能通过编辑观察值反写运行状态。需要 Build 的结构编辑 MUST在 Edit Mode 完成；只读 Graph/StateMachine 表面 MUST继续拒绝拖动、连线等 Mutation，不得为调参换成第二套 View。

#### Scenario: 调整参数并查看实际值

- **WHEN** 作者在运行中修改合法作者参数
- **THEN** UI MUST分别显示作者修改状态和运行采用状态
- **AND** 当前状态、速度 Fact、运行句柄和诊断 snapshot MUST仍不可编辑
