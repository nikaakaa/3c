## ADDED Requirements

### Requirement: 运行期间字段编辑资格必须由领域正式合同提供

领域 MUST消费现行正式 C# authoring API、共享 Capability、typed Mutation、作者 owner 和参数合同，向所有作者入口一致提供运行可调参数、需要 Build 的作者字段、纯编辑布局与只读运行观察的区别。技能/控制配置、Pose 与独立 Timeline MUST保持各自资格和采用规则；窗口 MUST不按字段名、数值类型或 C# 反射猜测可热更新能力。运行可调参数 MUST复用真实作者 Mutation、Validator、Undo 和正式运行端口；其输入范围和生效时机 MUST与领域原有合同一致。

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

### Requirement: 预览字段资格不得把配置可写等同于局内程序替换

控制 binding/参数与 SkillDefinition/技能 Root 内容 MUST使用主重构的唯一作者入口。可写字段没有正式运行更新合同时 MUST要求 Build/发布后由新 Session 采用；代码实现、控制状态 schema、生成 SkillProgram 与 ActionInstance/调用状态 MUST保持只读。C# 控制流程 MUST不被映射为可编辑角色总控 Graph；技能局部状态机与 PoseState MUST分别保留原有领域合同。

独立 Timeline MUST使用其正式作者根、内容构建和采用合同；没有局内更新能力的内容 MUST在正式发布后由新调用环境采用，不原地替换活动播放的 Program。实际目标、播放状态和生成内容 MUST只读，MUST不通过 Actor 参数端口或窗口私有候选应用到非 Skill 实例。

#### Scenario: 修改可编译的技能参数

- **WHEN** 技能作者参数属于 Program 内容且没有正式局内更新接口
- **THEN** 作者工具 MUST明确标记构建采用规则
- **AND** MUST不通过 Actor Pose 调参接口或反射写入当前 SkillExecutionState

#### Scenario: 独立内容字段没有局内更新合同

- **WHEN** shared Timeline 字段可由正式 Mutation 编辑但其领域要求重新构建
- **THEN** 工具 MUST显示独立内容的正式构建和新调用采用规则
- **AND** MUST不补造热更新端口或借用角色调参接口
