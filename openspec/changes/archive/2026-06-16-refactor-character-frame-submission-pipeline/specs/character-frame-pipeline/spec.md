## ADDED Requirements
### Requirement: 唯一 Character Frame Pipeline
系统 MUST 只有一个正式角色帧管线拥有单个角色在一个 simulation tick 或兼容 frame tick 内的 phase 顺序。FullBody、Locomotion、Action、UpperBody、LowerBody 或其它身体域 MUST NOT 拥有独立 phase owner；它们只能在唯一角色帧管线指定的阶段提交请求候选或纯数据帧输出。

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

#### Scenario: 兼容入口不形成第二管线
- **WHEN** 旧 `PlayerFullBodyActionController.Tick`、tick adapter 或 rollback 入口推进角色
- **THEN** 它们 MUST 转发到同一个角色帧管线
- **AND** MUST NOT 维护与角色帧管线不同的 phase 顺序

### Requirement: 请求提交和打断仲裁
系统 MUST 在状态机推进前收集 request submission。外部请求、输入缓冲请求、Dodge、TurnBack、Attack、Jump 或其它动作候选 MUST 通过统一 request submission 进入请求/打断仲裁。request provider MUST 只提交请求候选，不得直接切状态、执行运动、播放动画、消费输入或写 runtime blackboard。

#### Scenario: 外部请求进入统一仲裁
- **WHEN** 外部系统或输入缓冲提交 Dodge、TurnBack、Attack、Jump 或等价请求候选
- **THEN** 该请求 MUST 被转换为 request submission
- **AND** MUST 进入统一请求/打断仲裁入口
- **AND** MUST NOT 直接变成状态机 active state

#### Scenario: accepted request 输入状态机
- **WHEN** 请求/打断仲裁接受一个请求
- **THEN** 系统 MUST 生成 accepted `CharacterInputRequestFact` 或等价事实
- **AND** 统一状态机 MUST 通过该事实评估 transition
- **AND** accepted request 的输入消费 MUST 仍由后续帧输出和角色级 apply 阶段决定

#### Scenario: rejected request 不产生副作用
- **WHEN** 请求/打断仲裁拒绝一个请求
- **THEN** 系统 MUST NOT 消费该请求
- **AND** MUST NOT 切换状态
- **AND** MUST NOT 执行 motion 或提交 animation

### Requirement: Character Frame Submission 模型
系统 MUST 使用 `CharacterFrameSubmission` 或等价 Character 语义提交模型表达各身体域或 adapter 的状态机后本帧结果。提交内容 MUST 是纯数据，MAY 包含状态帧、运动提案、动画提案、输入消费提案、runtime facts 提案、snapshot/events 提案和 diagnostics trace，但 MUST NOT 直接执行副作用。request submission MUST NOT 与 `CharacterFrameSubmission` 混用。

#### Scenario: FullBody 提交当前结果
- **WHEN** 当前 FullBody 行为域完成本帧状态和运动构建
- **THEN** 它 MUST 产出 `CharacterFrameSubmission` 或等价角色级帧提交
- **AND** MUST 提交 `CharacterStateMachineFrame` 或等价状态结果
- **AND** MUST 提交 `BasicLocomotionFrame` 或等价基础移动结果
- **AND** MUST 提交 `ActionMotionResolveResult` 或等价动作运动结果
- **AND** 提交本身 MUST NOT 调用 motion executor 或 animation presenter

#### Scenario: CharacterFrameSubmission 不持有 Unity 场景对象
- **WHEN** 检查 `CharacterFrameSubmission` 或等价角色帧提交模型
- **THEN** 提交模型 MUST NOT 持有 `MonoBehaviour`
- **AND** MUST NOT 持有 `Transform`
- **AND** MUST NOT 持有 `CharacterController`
- **AND** MUST NOT 持有 Animancer runtime object
- **AND** MUST NOT 持有 `InputAction`

#### Scenario: 请求提交不混入帧输出提交
- **WHEN** 检查 `CharacterFrameSubmission` 或等价角色帧提交模型
- **THEN** 它 MUST NOT 表达 request priority、resistance、force 或 timing window 仲裁规则
- **AND** 请求准入 MUST 已经在状态机推进前完成

### Requirement: 输出合成先于输出应用
系统 MUST 在执行任何运动、动画、输入消费、runtime facts 写入或 snapshot/events commit 之前，先由角色级 output composer 合成本帧最终输出。第一版 composer MAY 只接收 FullBody 一个 `CharacterFrameSubmission` 来源，但仍 MUST 是副作用应用前的唯一裁决位置。

#### Scenario: 单一 FullBody 来源仍经过 composer
- **GIVEN** 本帧只有 FullBody 提交
- **WHEN** 角色帧管线进入输出合成阶段
- **THEN** composer MUST 从 FullBody 提交中选择最终 movement 输出
- **AND** MUST 从 FullBody 提交中选择最终 animation 输出
- **AND** MUST 从 FullBody 提交中选择最终 input consume 输出
- **AND** MUST 从 FullBody 提交中选择最终 runtime facts 输出

#### Scenario: 副作用只在 apply 阶段发生
- **WHEN** 角色帧管线应用 composer 结果
- **THEN** motion executor 调用 MUST 只发生在角色级 output applier 或等价提交阶段
- **AND** animation presenter 调用 MUST 只发生在角色级 output applier 或等价提交阶段
- **AND** input buffer consume MUST 只发生在角色级 output applier 或等价提交阶段
- **AND** runtime blackboard 写入 MUST 只发生在角色级 output applier 或等价提交阶段

### Requirement: FullBody 兼容迁移行为保持
系统 MUST 在迁移到唯一角色帧管线时保持当前 FullBody-only 行为输出一致。Idle、MoveStart、MoveLoop、MoveStop、TurnBack、Directional Dodge 和 Backstep Dodge 的状态路径、输入消费、运动执行、动画提交、runtime facts 和诊断 trace MUST 与迁移前等价。

#### Scenario: 基础移动行为保持
- **WHEN** 使用相同 WASD 输入、相同配置和相同 tick 序列运行迁移前后路径
- **THEN** Idle、MoveStart、MoveLoop 和 MoveStop 的状态序列 MUST 等价
- **AND** 基础移动运动命令来源 MUST 等价
- **AND** base layer animation 提交语义 MUST 等价

#### Scenario: Dodge 行为保持
- **WHEN** 使用相同 Dodge 输入、相同配置和相同 tick 序列运行迁移前后路径
- **THEN** Directional Dodge 和 Backstep Dodge 的 accepted/rejected 结果 MUST 等价
- **AND** 动作运动结果 MUST 等价
- **AND** Dodge active 时基础移动输出 MUST 不被重复提交

#### Scenario: TurnBack 行为保持
- **WHEN** 使用相同 RunLoop 反向输入、相同配置和相同 tick 序列运行迁移前后路径
- **THEN** TurnBack 进入、输入抑制、运动源策略和退出结果 MUST 等价
- **AND** TurnBack 运动不得新增第二运动出口

### Requirement: 不引入并行身体域
本变更 MUST 只建立唯一角色帧管线和提交模型，不得实现 UpperBody、LowerBody、Facial、IK、Additive、AvatarMask layer 或并行状态机。后续并行身体域 MUST 另开 OpenSpec 定义身体域职责、动画合成、权威压制和验证方式。

#### Scenario: 不创建 UpperBody 或 LowerBody runtime
- **WHEN** 实施本变更
- **THEN** 系统 MUST NOT 新增正式 UpperBody runtime
- **AND** MUST NOT 新增正式 LowerBody runtime
- **AND** MUST NOT 新增并行状态机调度规则

#### Scenario: 只预留提交接口
- **WHEN** 角色帧管线定义提交模型
- **THEN** 提交模型 MAY 预留来源标识和合成扩展点
- **AND** MUST NOT 在本变更中实现多身体域合成策略

### Requirement: 局部 Pipeline 直接改名
系统 MUST 在唯一角色帧管线中移除 FullBody 和 Locomotion 的正式 pipeline 命名。FullBody 侧正式入口 MUST 是 `FullBodySubmissionBuilder` 或等价提交构建器；Locomotion 侧正式入口 MUST 是 `LocomotionFrameBuilder` 或等价局部帧构建器。正式路径 MUST NOT 保留 obsolete pipeline 外壳作为 phase owner。

#### Scenario: FullBody 不再叫 Pipeline
- **WHEN** 实施唯一角色帧管线迁移
- **THEN** FullBody 侧正式职责 MUST 由 `FullBodySubmissionBuilder` 或等价提交构建器承担
- **AND** 该构建器 MUST NOT 拥有 phase switch
- **AND** 该构建器 MUST NOT 执行输出副作用

#### Scenario: Locomotion 不再叫 Pipeline
- **WHEN** 实施唯一角色帧管线迁移
- **THEN** Locomotion 侧正式职责 MUST 由 `LocomotionFrameBuilder` 或等价局部帧构建器承担
- **AND** 该构建器 MUST NOT 注册 tick handler
- **AND** 该构建器 MUST NOT 拥有角色级 phase 顺序

#### Scenario: Character Pipeline 不归属 FullBody 目录
- **WHEN** 检查角色级帧管线源码归属
- **THEN** `CharacterFramePipeline`、角色帧模型和 `ICharacterFrameRuntimePort` MUST 位于 `Assets/Scripts/Character/Pipeline/...` 或等价角色级目录
- **AND** `Assets/Scripts/Character/Action/FullBody/...` MUST NOT 保留 `CharacterFramePipeline`、`CharacterFramePipelineTypes` 或 `ICharacterFrameRuntimePort` 的正式文件
