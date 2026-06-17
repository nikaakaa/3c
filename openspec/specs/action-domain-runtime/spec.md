# action-domain-runtime Specification

## Purpose
定义 Action 领域运行时的请求解析、生命周期、body/channel claim、动作运动候选、动作动画意图和角色帧管线接入边界。Action 是 `CharacterFramePipeline` 下的领域模块，不是 FullBody 主树、独立 Unity tick 入口或第二角色控制路径。

## Requirements
### Requirement: Action 领域作为角色帧兄弟提交者
系统 MUST 将 Action 领域建模为 `CharacterFramePipeline` 下的 sibling submitter、runtime module 或等价纯数据提交者。Action 领域 MAY 内部使用 lifecycle、timeline、局部 graph 或策略对象表达动作阶段，但 MUST NOT 拥有角色级 frame phase、Locomotion 状态权威或独立 gameplay tick。

#### Scenario: Action submitter 提交候选
- **GIVEN** 输入缓冲或 AI 决策产生 Action 请求
- **WHEN** 角色帧管线收集领域输出
- **THEN** Action submitter MUST 提交动作请求、动作状态事实、body/channel claim、motion candidate 和 animation candidate 中适用的纯数据结果
- **AND** MUST NOT 直接移动角色、播放动画或写 `CharacterRuntimeBlackboard`

#### Scenario: Action 不拥有 Locomotion
- **GIVEN** Locomotion submitter 已经提交基础移动候选
- **AND** Action submitter 已经提交动作 claim
- **WHEN** `CharacterFramePipeline` 生成 `CharacterFramePlan` 或等价计划
- **THEN** 是否采用 Action 输出 MUST 由角色级计划决定
- **AND** Action submitter MUST NOT 改写 Locomotion 私有状态来表达压制

### Requirement: Action 请求解析与生命周期分离
系统 MUST 将 Action 请求候选构建、请求仲裁、resolved action 解析和 Action lifecycle 推进拆成明确职责。新增 Attack、Jump、HitReact 或 Skill MUST 通过 Action Catalog、provider/resolver strategy 或等价扩展点接入，不得在角色帧主流程里新增具体动作 switch。

#### Scenario: 请求候选由 provider 贡献
- **GIVEN** 本帧存在 Dodge、Attack 或等价输入请求
- **WHEN** Action 请求提交阶段运行
- **THEN** 对应 provider MUST 构建纯数据候选请求
- **AND** 主流程 MUST NOT 手写具体动作的请求构建分支

#### Scenario: lifecycle 只推进 active action
- **GIVEN** Action 仲裁 accepted 一个 resolved action
- **WHEN** Action lifecycle tick
- **THEN** lifecycle MUST 更新 active action、state time、variant、播放实例身份和退出状态
- **AND** lifecycle MUST NOT 重新读取 Unity 输入对象或创建第二角色帧 runner

### Requirement: Body Channel Claim 独立于行为模块
系统 MUST 将 FullBody、UpperBody、LowerBody、Additive 或等价身体输出范围表达为 body/channel claim。Action、Locomotion、Aim 或 HitReact 是行为模块；body/channel claim 只描述输出占用或合成范围，MUST NOT 成为 gameplay owner。

#### Scenario: Dodge 提交 FullBody claim
- **GIVEN** `Action.Dodge` 需要占用全身输出
- **WHEN** Action submitter 构建本帧候选
- **THEN** 它 MUST 提交 FullBody 或等价 body/channel claim
- **AND** FullBody MUST 只表示输出通道占用
- **AND** MUST NOT 表示 Locomotion 的父状态域

#### Scenario: claim policy 缺失时报错
- **GIVEN** Action definition 引用的 body/channel claim policy 缺失
- **WHEN** 正式 gameplay 需要解析该 Action
- **THEN** 系统 MUST 报告明确配置错误
- **AND** MUST NOT 使用隐藏默认 full-body claim 继续运行

### Requirement: Action 输出候选保持纯数据
Action 领域 MUST 输出动作运动意图、动作动画意图、输入消费、runtime facts 请求、cue 请求和诊断数据中的适用纯数据候选。最终副作用 MUST 由 `CharacterFramePipeline` 的 output applier 或批准的等价角色级输出阶段执行。

#### Scenario: 候选不执行副作用
- **WHEN** Action submitter 输出本帧 action candidate
- **THEN** candidate MAY 包含 motion intent、animation key、hitbox/cancel window facts、cue request 和 diagnostics
- **AND** candidate MUST NOT 持有 `Transform`、`CharacterController`、`Animator`、Animancer runtime object、`InputAction` 或 `MonoBehaviour`

#### Scenario: 黑板只写确认事实
- **GIVEN** Action candidate 包含 cancel window active 或 motion completed
- **WHEN** 角色帧管线尚未应用最终计划
- **THEN** Action runtime MUST NOT 直接写 `CharacterRuntimeBlackboard`
- **AND** 已确认 facts MUST 在角色级 output/facts 写入阶段提交

### Requirement: Action Motion Resolver 只消费通用规格
Action motion resolver MUST 只消费通用 Action motion spec、timeline facts、delta/tick 信息和必要前帧事实。Dodge、Attack、Jump 或 Skill 的配置解析 MUST 在 Action definition、provider 或 adapter 中完成，resolver MUST NOT 按具体 action id 读取动作专用配置资产。

#### Scenario: Dodge 数值进入通用 spec
- **GIVEN** `Action.Dodge` 已被解析为动作候选
- **WHEN** Action motion resolver 处理本帧 motion spec
- **THEN** duration、distance、rotateToDirection、variant 和 locked direction MUST 已经进入通用 spec
- **AND** resolver MUST NOT 从旧 Dodge runtime config 或 controller 字段读取正式数值

#### Scenario: 新动作不修改 resolver 主流程
- **WHEN** 后续新增 Attack、Jump 或 Skill motion spec
- **THEN** 新动作 MAY 新增 spec payload 或 strategy
- **AND** MUST NOT 要求在 resolver 主流程中新增具体 action id switch 才能运行

### Requirement: Action 动画播放意图身份
Action lifecycle MUST 为每个 accepted Action 实例提供纯数据播放实例身份，并将其传递到动作动画请求。播放实例身份 MUST 在同一 active action 内保持稳定，在新的 accepted action 进入时变化，并且 MUST 可通过 snapshot/restore 重建。

#### Scenario: 连续同 key Action 可重播
- **GIVEN** 当前 active action 使用 animation key `Action.Dodge.Directional`
- **AND** 新的 `Action.Dodge` 请求被 accepted
- **WHEN** lifecycle 输出下一段动作动画请求
- **THEN** 请求 MUST 携带新的播放实例身份
- **AND** 即使 animation key 相同，Presenter 也能识别为新的播放意图

#### Scenario: 输出阶段不生成身份
- **GIVEN** Action lifecycle frame 已包含 animation key 和播放实例身份
- **WHEN** output runtime 执行动画提交
- **THEN** output runtime MUST 原样转交该播放意图
- **AND** MUST NOT 基于当前 Presenter state、Unity frame count 或 normalized time 重新生成身份

### Requirement: Action Runtime Capture/Restore
Action runtime MUST 支持纯数据 capture/restore，用于 rollback、synctest 和调试回放。恢复数据 MUST 包含 active action、state time、variant、播放实例身份、必要 payload 和已确认 facts，MUST NOT 保存 Unity scene object 或表现层 runtime object。

#### Scenario: restore 后继续动作
- **GIVEN** rollback restore 后 Action runtime 仍处于 `Action.Dodge`
- **WHEN** 下一帧 Action lifecycle tick
- **THEN** state time、variant 和播放实例身份 MUST 从 restore state 恢复
- **AND** 输出的 motion/animation candidate MUST 与恢复后的 action 实例对应

#### Scenario: restore 不依赖 Mono 生命周期
- **WHEN** 测试或 rollback 对 Action runtime 执行 restore
- **THEN** restore MUST 作用于 core-owned pure runtime state
- **AND** MUST NOT 要求启用、禁用或重新创建 MonoBehaviour 才能恢复一致状态

### Requirement: 旧 FullBody Host 与旧播放路径退役
系统 MUST 不再使用 `PlayerFullBodyActionController`、FullBody integrated adapter、旧 Action presenter、旧 Dodge 平铺配置或等价兼容 API 作为正式扩展入口。历史类型若短期存在，MUST 只作为迁移残留或只读诊断，不得进入正式 prefab、scene、runtime port、submitter graph 或 rollback replay 主线。

#### Scenario: 正式 runtime 不依赖旧 Host
- **WHEN** 检查正式角色 runtime 装配、prefab、scene 和测试 fixture
- **THEN** 它们 MUST 通过 `CharacterFrameRuntimeController`、Action submitter、Action runtime 和正式 runtime ports 组合
- **AND** MUST NOT 通过 `PlayerFullBodyActionController` 或 FullBody integrated adapter 推进 gameplay

#### Scenario: 新 Action 不复用旧路径
- **WHEN** 后续新增 Attack、Jump、HitReact 或 Skill
- **THEN** 新动作 MUST 通过 Action Catalog、Action provider/resolver、Action runtime 和角色帧管线接入
- **AND** MUST NOT 新增 `PlayerAttackController`、`PlayerJumpController` 或等价 MonoBehaviour gameplay 入口
