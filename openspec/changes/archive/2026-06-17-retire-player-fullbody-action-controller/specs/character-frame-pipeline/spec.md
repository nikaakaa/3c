## MODIFIED Requirements
### Requirement: 唯一 Character Frame Pipeline
系统 MUST 只有一个正式角色帧管线拥有单个角色在一个 simulation tick 或兼容 frame tick 内的 phase 顺序。FullBody、Locomotion、Action、UpperBody、LowerBody 或其它身体域 MUST NOT 拥有独立 phase owner；它们只能在唯一角色帧管线指定的阶段提交请求候选或纯数据帧输出。`PlayerFullBodyActionController` MUST NOT 作为兼容入口、转发入口或旧 Tick owner 保留在正式运行时。

#### Scenario: FullBody-only 也通过唯一管线
- **GIVEN** 当前角色仍然只有 FullBody 行为域
- **WHEN** 角色推进 tick N
- **THEN** 系统 MUST 通过 `CharacterFramePipeline` 或等价唯一角色帧管线推进
- **AND** FullBody MUST 作为提交来源参与该管线
- **AND** FullBody MUST NOT 自行拥有正式最高 phase 顺序

#### Scenario: 后续身体域只提交
- **GIVEN** 后续新增 UpperBody、LowerBody 或其它身体域
- **WHEN** 这些身体域参与 tick N
- **THEN** 它们 MUST 只向角色帧管线提交纯数据结果
- **AND** MUST NOT 自行执行 motion、播放动画、消费输入或写 runtime blackboard

#### Scenario: 旧 FullBody controller 不再作为兼容入口
- **WHEN** 旧 `PlayerFullBodyActionController.Tick`、旧 FullBody tick adapter 或旧 rollback 入口仍被代码、测试、prefab 或 scene 引用
- **THEN** 实施 MUST 删除或迁移该引用
- **AND** 正式推进 MUST 进入 `CharacterFrameRuntimeController -> CharacterFrameRuntimeHost -> CharacterFramePipeline`
- **AND** 系统 MUST NOT 通过保留 controller 转发来延长第二入口寿命

## ADDED Requirements
### Requirement: Sibling Submitter 边界
角色帧管线 MUST 将 Locomotion 与 FullBody Action 建模为兄弟提交者。Locomotion submitter MUST 只提交 Locomotion motion、animation、facing、camera 或 locomotion facts 候选；FullBody Action submitter MUST 只提交 action request、action motion、action animation、occupancy 或 resolved action facts 候选。系统 MUST NOT 通过单个 FullBody 命名 builder 同时构建 Locomotion 与 FullBody Action 的正式输出。

#### Scenario: Locomotion 与 FullBody Action 独立提交
- **GIVEN** tick N 同时存在 Locomotion 输入和 Dodge 请求
- **WHEN** `CharacterFrameSubmitterGraph` 构建提交
- **THEN** Locomotion submitter MUST 提交 Locomotion 候选
- **AND** FullBody Action submitter MUST 提交 Dodge 或等价 action 候选
- **AND** 两者 MUST 由 `CharacterFramePipeline` 仲裁
- **AND** 任一 submitter MUST NOT 通过共享 FullBody 集成 builder 替另一个 submitter 决定 winning output

### Requirement: Frame Output Source 不表达旧 FullBody 权威
角色帧输出来源 MUST 表达角色级候选、仲裁结果或具体提交域，而不是表达旧 FullBody 集成路径权威。正式路径 MUST NOT 继续使用 `LegacyFullBodyIntegrated` 作为 winning frame output source、diagnostic authority 或测试断言的正式身份。

#### Scenario: Winning frame source 来自角色级仲裁
- **WHEN** `CharacterFramePipeline` 产出 tick N 的 `CharacterFramePlan`
- **THEN** plan MUST 能说明 winning motion、animation 和 facts 来自角色级仲裁后的候选
- **AND** 输出来源 MUST NOT 被标记为 `LegacyFullBodyIntegrated`
- **AND** diagnostics MAY 显示具体 submitter 名称，但 MUST NOT 把旧 FullBody 集成路径标记为正式来源
