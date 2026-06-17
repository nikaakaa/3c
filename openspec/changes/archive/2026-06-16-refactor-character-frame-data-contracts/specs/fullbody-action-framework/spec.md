## ADDED Requirements
### Requirement: Character Frame 数据契约拆分
系统 MUST 将角色帧 pipeline 的数据契约拆分为 focused pure data types。`CharacterFrameInput`、`CharacterFrameContext`、`CharacterFrameSubmission`、`CharacterFrameOutput`、`CharacterFrameResult` 和 pipeline step MUST 有明确职责边界。frame data types MUST NOT 持有 Unity scene object、runtime executor、animation presenter 或 input runtime object。

#### Scenario: 数据类型职责清晰
- **WHEN** 开发者查看 Character frame model 文件
- **THEN** input、context、submission、output、result MUST 能在文件和类型层面区分
- **AND** 每个类型 MUST 有单一主要变化原因
- **AND** `CharacterFramePipelineTypes` MUST NOT 继续作为无限增长的总线文件

#### Scenario: Frame data 归属角色级 Pipeline
- **WHEN** 开发者查看角色帧数据契约文件
- **THEN** `CharacterFrameInput`、`CharacterFrameContext`、`CharacterFrameSubmission`、`CharacterFrameOutput`、`CharacterFrameResult` 和 pipeline step MUST 位于 `Assets/Scripts/Character/Pipeline/Model/`
- **AND** `CharacterFramePipeline` MUST 位于 `Assets/Scripts/Character/Pipeline/Runtime/`
- **AND** `Action/FullBody` 目录 MUST NOT 承载角色级 pipeline data 或 runtime 文件

#### Scenario: Frame data 保持纯数据
- **WHEN** 静态检查 frame data model
- **THEN** 类型 MUST NOT 引用 `MonoBehaviour`
- **AND** MUST NOT 引用 `Transform`
- **AND** MUST NOT 引用 `CharacterController`
- **AND** MUST NOT 引用 Animancer runtime type
- **AND** MUST NOT 引用 InputAction

#### Scenario: Submission 不执行副作用
- **WHEN** `CharacterFrameSubmission` 从 builder 传给 output composer
- **THEN** 它 MUST 只包含 frame decision、state frame、locomotion frame、action motion result、request facts 和 trace 等纯数据
- **AND** MUST NOT 保存 motion executor、animation presenter、diagnostic sink 或 input buffer component

#### Scenario: Result 作为观测合同
- **WHEN** Tick 或 RunPhase 返回 `CharacterFrameResult`
- **THEN** result MUST 可用于测试和诊断观察
- **AND** MUST NOT 触发日志提交、运动执行或动画播放
- **AND** rollback/replay tests MUST 能比较关键 result 字段

### Requirement: Character Frame 数据类型必须有唯一变化原因
系统 MUST 为每个 Character frame data type 定义唯一主要变化原因。Input、Context、Submission、Output、Result 和 DiagnosticsSummary MUST NOT 互相代替职责，也不得把 future layer 的未实现字段塞进现有 FullBody 数据合同。

#### Scenario: Input 不承担 Submission 职责
- **WHEN** frame input 被构建
- **THEN** 它 MUST 表示外部输入快照和 prediction facts
- **AND** MUST NOT 包含 state frame、locomotion frame 或 action motion result
- **AND** MUST NOT 消费 input buffer

#### Scenario: Context 只在 pipeline 内部可变
- **WHEN** pipeline phases 聚合中间结果
- **THEN** mutation MUST 发生在 `CharacterFramePipeline` 或等价 pipeline runtime 内部
- **AND** external domain modules MUST NOT 把 context 当作公共总线长期保存
- **AND** context MUST NOT 存放 executor、presenter 或 sink

#### Scenario: Submission 和 Output 分离
- **WHEN** submission builder 产出 `CharacterFrameSubmission`
- **THEN** submission MUST 表示决策和待组合数据
- **AND** output composer MUST 产出 `CharacterFrameOutput`
- **AND** output apply side effects MUST NOT 写进 submission 类型

#### Scenario: Future layer 需要独立提案
- **WHEN** 后续要加入 UpperBody、HitReaction、Aim 或其他并行 layer
- **THEN** 系统 MUST 新增或修改正式 layer submission/result contract
- **AND** MUST NOT 在本 change 中提前加入未使用 placeholder 字段

### Requirement: Character Frame 数据合同不得隐藏运行时依赖
系统 MUST 保持 Character frame model 为纯数据合同。任何需要 Unity runtime object、executor、presenter、diagnostic sink 或 input runtime object 的行为 MUST 留在 runtime module 或 adapter 中。

#### Scenario: 禁止 runtime dependency 字段
- **WHEN** 静态检查 Character frame model 文件
- **THEN** model MUST NOT 引用 motion executor Interface
- **AND** MUST NOT 引用 animation presenter Interface
- **AND** MUST NOT 引用 diagnostic sink
- **AND** MUST NOT 引用 input runtime component

#### Scenario: 数据合同不提供 fallback 配置
- **WHEN** output apply 缺少正式配置或 runtime dependency
- **THEN** frame data type MUST NOT 提供 fallback behavior
- **AND** runtime module MUST 通过正式配置或明确错误处理该问题
