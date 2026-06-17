## MODIFIED Requirements

### Requirement: FullBody Action Module

系统 MUST 提供 FullBody Action module 或等价 Action domain 端口，使单个全身动作能通过统一请求、仲裁、action instance facts、body/channel claim、运动候选和动画候选接入 Character frame pipeline。Action module MUST NOT 是 FullBody 主树内部模块；它 MUST 是 Character frame owner 下的 sibling submitter 或 submitter 内部职责。

#### Scenario: Module 不是独立角色状态机
- **WHEN** 系统注册或执行 FullBody Action module
- **THEN** module MAY 使用内部 lifecycle、timeline 或小型 FSM/HFSM 表达 action 生命周期
- **AND** MUST NOT 拥有角色级 frame phase
- **AND** MUST NOT 决定 Locomotion domain state
- **AND** MUST NOT 形成独立角色控制路径

#### Scenario: Module 使用 Action 仲裁
- **GIVEN** 输入缓冲存在一个 FullBody Action 请求
- **WHEN** module 尝试进入动作
- **THEN** module MUST 通过 `ActionInterruptArbiter` 或等价 Action 仲裁判断是否允许进入
- **AND** accepted 时 MUST 更新 Action facts 或等价 action instance state
- **AND** rejected 时 MUST 不消费未过期请求

#### Scenario: Module 输出命令而不直接执行
- **WHEN** module active tick 产生动作位移或动作动画
- **THEN** module MUST 输出纯数据 action motion candidate
- **AND** MUST 输出动作动画 key/command 或等价 animation candidate
- **AND** MUST 输出 full-body 或等价 body/channel claim
- **AND** MUST NOT 直接调用 `CharacterController.Move`
- **AND** MUST NOT 直接调用 Animancer 或 Animator 播放 API

#### Scenario: Module 显式退出
- **GIVEN** module 当前 active
- **WHEN** module 达到自身退出条件
- **THEN** module MUST 显式退出到 `Action.None` 或等价空 action facts
- **AND** MUST 停止提交 full-body 或等价 body/channel claim
- **AND** `ActionRuntimeStateTracker` 或等价 facts helper MUST NOT 因隐藏 duration 规则自动退出

### Requirement: FullBody 输出权威

系统 MUST 退役 FullBody 作为 gameplay output authority 的旧口径。FullBody Action 只能提交 body/channel claim 和候选输出；统一 motion executor 是位移执行权威，动画 Presenter 只消费最终动画命令并反馈播放事实。Locomotion 和 Action MAY 同帧提交候选，但最终运动和 base layer 动画选择 MUST 由角色级 plan 决定。

#### Scenario: 单一运动提交
- **WHEN** Character frame output apply 处理本帧运动
- **THEN** 它 MUST 最多向统一 motion executor 提交一个被选中的 base/full-body 平面运动命令来源
- **AND** 该来源 MUST 来自 `CharacterFramePlan` 或等价角色级计划
- **AND** 动画 Presenter、Animancer 回调或 Transform 写入 MUST NOT 成为平面位移权威

#### Scenario: 单一 base layer 动画提交
- **WHEN** Character frame output apply 处理本帧动画输出
- **THEN** 它 MUST 按角色级 plan 选择 base layer 动画请求
- **AND** Locomotion 和 Action MUST NOT 同帧同时执行互斥 base layer 主动画
- **AND** Presenter MUST NOT 决定业务 Action 是否允许进入

#### Scenario: Look 不被 FullBody 动作锁死
- **GIVEN** 当前存在 active full-body action
- **WHEN** 玩家输入 Look
- **THEN** 项目侧相机入口 MUST 继续接收 Look 输入或等价相机意图
- **AND** Action module MUST NOT 直接读取或控制 Cinemachine 具体实例

### Requirement: FullBody 固定调度顺序

系统 MUST 将原 FullBody 固定调度顺序迁移到唯一 Character frame pipeline。输入、Locomotion 意图、Action 仲裁、领域状态推进、body claim、输出合成、运动输出、动画输出和 facts 写入 MUST 按 Character frame phases 保持确定。

#### Scenario: 调度顺序固定
- **WHEN** Character frame pipeline 处理一帧
- **THEN** 系统 MUST 先收集输入事实和本地输入请求
- **AND** MUST 再生成 Locomotion 意图、空间事实和 Locomotion 状态事实
- **AND** MUST 再处理 Action 请求解析与 Action 仲裁
- **AND** MUST 再收集 body/channel claim 和候选输出
- **AND** MUST 再生成 CharacterFramePlan
- **AND** MUST 再应用最终 motion、animation、input consume 和 runtime facts

#### Scenario: 同输入序列结果稳定
- **WHEN** 使用相同输入序列、相同配置和相同 delta/tick 序列运行 Character frame pipeline
- **THEN** action request 消费结果 MUST 一致
- **AND** Locomotion 状态事实序列 MUST 一致
- **AND** CharacterFramePlan 的输出选择 MUST 一致

### Requirement: FullBody 框架可测试和可验证

系统 MUST 为 FullBody Action framework 的归属迁移提供 EditMode 测试和静态边界验证，证明 FullBody Action 不拥有 Locomotion，也没有破坏现有基础移动和 Dodge 行为。

#### Scenario: 自动测试覆盖 claim 输出选择
- **WHEN** 运行 FullBody Action framework EditMode 测试
- **THEN** 测试 MUST 覆盖无 Action claim 时 Locomotion candidate 被选中
- **AND** MUST 覆盖 Dodge active 时 full-body claim 被提交
- **AND** MUST 覆盖 Dodge claim 参与 FramePlan 输出选择
- **AND** MUST 覆盖 Dodge 结束后 Locomotion candidate 恢复

#### Scenario: 静态边界验证
- **WHEN** 检查 FullBody framework 新增或迁移源码
- **THEN** 静态搜索 MUST 能确认 FullBody Action submitter 不引用 Locomotion output side effects 作为输出选择权威
- **AND** MUST 能确认新增源码不通过 `FullBodyOwnerKind.Locomotion` 选择输出
- **AND** MUST 能确认 Action module 不直接调用 Animancer 或 Animator 播放 API

## ADDED Requirements

### Requirement: FullBody Action 作为 Action domain submitter

FullBody Action framework MUST 在目标架构中作为 Character frame owner 下的 Action domain submitter 存在。它 MUST 提交动作请求、动作状态事实、body/channel claim、action motion candidate 和 action animation candidate。它 MUST NOT 作为正式 Unity tick 入口、Character runtime host owner 或 Locomotion 上级 owner。

#### Scenario: Dodge 通过 Action submitter 提交
- **GIVEN** 输入缓冲中存在有效 Dodge 请求
- **WHEN** Character frame pipeline 收集 Action submitter 输出
- **THEN** submitter MUST 提交 `Action.Dodge` request 或 resolved action candidate
- **AND** MUST 提交 full-body 或等价 body/channel claim
- **AND** MUST NOT 直接执行 Dodge movement
- **AND** MUST NOT 直接播放 Dodge animation

#### Scenario: Action claim 不拥有 Locomotion
- **GIVEN** Locomotion submitter 已提交基础移动候选输出
- **AND** Action submitter 已提交 full-body 或等价 body/channel claim
- **WHEN** CharacterFramePlan 选择 Action 输出并未采用 Locomotion 输出
- **THEN** 选择 MUST 来自角色级计划
- **AND** FullBody Action framework MUST NOT 写 Locomotion runtime 私有状态来表达该选择
- **AND** FullBody Action framework MUST NOT 调用 Locomotion output runtime 直接执行输出屏蔽

#### Scenario: Future Action 不新增入口
- **WHEN** 后续新增 Attack、Jump 或 HitReact
- **THEN** 新动作 MUST 通过 Action submitter、action provider/resolver 或等价 sibling submitter 接入
- **AND** MUST NOT 新增 `PlayerAttackController`、`PlayerJumpController` 或等价 MonoBehaviour 作为正式 gameplay tick 入口
