## ADDED Requirements
### Requirement: 角色级帧仲裁权威
系统 MUST 将正式目标架构定义为 Character 级 frame owner 驱动一帧。Locomotion、FullBody Action、UpperBody 或等价行为域 MUST 作为 sibling frame submitters 提交请求、事实、占用声明或候选输出；它们 MUST NOT 互相成为目标架构中的上级 owner。

#### Scenario: Character owner 汇集兄弟提交者
- **WHEN** 正式角色运行时处理一帧
- **THEN** Character frame owner MUST 汇集 Locomotion submitter 的请求或候选输出
- **AND** MUST 汇集 FullBody Action submitter 的请求或候选输出
- **AND** MAY 汇集 UpperBody submitter 的请求或候选输出
- **AND** MUST NOT 要求 Locomotion 作为 FullBody submitter 的长期子模块才能参与正式主线

#### Scenario: FullBody 通过占用声明压制 Locomotion
- **GIVEN** FullBody Action submitter 提交 full-body occupancy claim
- **AND** Locomotion submitter 提交基础移动候选输出
- **WHEN** BodyArbiter 或等价仲裁模块生成本帧计划
- **THEN** 计划 MAY 压制 Locomotion 的 base layer motion 或 animation output
- **AND** 该压制 MUST 来自角色级仲裁结果
- **AND** MUST NOT 表达为 FullBody 直接拥有 Locomotion runtime

#### Scenario: Pipeline 不保存业务优先级
- **WHEN** `CharacterFramePipeline` 执行本帧
- **THEN** pipeline MUST 消费 `CharacterFramePlan` 或等价纯数据计划
- **AND** pipeline MUST NOT 在自身核心逻辑中硬编码 UpperBody、FullBody Action、Locomotion 的具体优先级树
- **AND** body domain 互斥和叠加规则 MUST 位于 BodyArbiter 或等价策略模块

#### Scenario: 当前集成提交者是迁移期形态
- **WHEN** 当前实现仍通过 `FullBodySubmissionBuilder` 或等价 integrated submitter 收集 Locomotion 与 Action 数据
- **THEN** 该路径 MUST 被视为迁移期兼容实现
- **AND** 后续新增正式 UpperBody、HitReact 或 Aim runtime MUST NOT 继续塞入该 integrated submitter 作为目标架构

### Requirement: CharacterFramePlan 先于新身体层
系统 MUST 在新增正式 UpperBody、HitReact、Aim 或等价身体层 runtime 前，先提供角色级 `CharacterFramePlan` 或等价一帧计划契约。新身体层 MUST 通过 plan 参与 output composer/applier，不能直接绕过角色级管线。

#### Scenario: 新 UpperBody 需要计划契约
- **WHEN** 后续 proposal 准备新增正式 UpperBody runtime
- **THEN** proposal MUST 依赖已定义的 CharacterFramePlan 或等价 frame plan contract
- **AND** UpperBody MUST 作为 sibling submitter 接入
- **AND** UpperBody MUST NOT 直接读取 FullBody controller 或 builder 内部状态作为上级权威

#### Scenario: Plan 是纯数据
- **WHEN** BodyArbiter 产出 CharacterFramePlan
- **THEN** plan MUST 只包含决策、输出选择、压制关系、source step 和诊断事实
- **AND** plan MUST NOT 持有 `Transform`、`CharacterController`、Animator、Animancer runtime object 或 Unity input object

#### Scenario: Output applier 仍唯一执行副作用
- **WHEN** CharacterFramePlan 选中本帧 motion 或 animation output
- **THEN** 最终副作用 MUST 仍通过 Character output applier 提交给正式 motion executor、Presenter、runtime facts writer 或 camera adapter
- **AND** submitter 和 arbiter MUST NOT 直接执行 movement 或播放 animation
