## MODIFIED Requirements
### Requirement: FullBody Frame Pipeline
系统 MUST 将当前 FullBody 一帧编排降级为角色帧管线下的 FullBody 提交职责。FullBody MAY 继续复用现有统一状态机、Locomotion facts、Action 仲裁、motion spec resolver 和 Animancer presenter adapter，但正式最高 phase 顺序 MUST 归属唯一 Character frame pipeline。FullBody 提交职责 MUST NOT 自行执行运动、播放动画、消费输入缓冲或写 runtime blackboard。

#### Scenario: 一帧步骤由 Character 管线显式
- **WHEN** 当前角色推进 tick N
- **THEN** 系统 MUST 由唯一 Character frame pipeline 依次处理输入事实、输入请求缓冲、Locomotion facts、FullBody Action 请求仲裁、统一状态机推进、运动命令构建、输出合成、输出应用、runtime facts 写入和 snapshot/events commit
- **AND** FullBody MUST 在该顺序内提交状态、运动、动画、输入消费和诊断结果
- **AND** 每个步骤 MUST 能通过测试观察到输入输出

#### Scenario: FullBody 提交不拥有状态权威
- **WHEN** FullBody 提交职责处理 GameplayDecision
- **THEN** 当前 FullBody 状态路径 MUST 仍由统一角色状态机决定
- **AND** FullBody 提交职责 MUST NOT 创建独立 Action 状态机
- **AND** FullBody 提交职责 MUST NOT 创建独立 Locomotion 状态机

#### Scenario: FullBody 提交不绕过运动出口
- **WHEN** FullBody 提交职责产生 movement submission
- **THEN** 它 MUST 只输出纯数据运动结果或运动提案
- **AND** MUST NOT 直接调用 `CharacterController.Move`
- **AND** MUST NOT 直接写角色 `Transform.position`
- **AND** MUST NOT 直接调用 motion executor

#### Scenario: FullBody 提交不让动画决定业务
- **WHEN** FullBody 提交职责产生 animation submission
- **THEN** Animancer presenter MUST 只在角色级 output applier 阶段消费最终动画命令并回传播放事实
- **AND** presenter MUST NOT 决定动作请求是否 accepted
- **AND** presenter MUST NOT 直接切换统一状态机状态

### Requirement: FullBody 主入口降级为装配 Adapter
系统 MUST 将 `PlayerFullBodyActionController` 或等价 MonoBehaviour 保持为角色帧管线的 FullBody 装配和调试 adapter，而不是把完整一帧顺序继续隐藏在 MonoBehaviour Tick 实现或 FullBody 局部管线中。兼容入口 MAY 保留，但 MUST 调用同一条 Character frame pipeline。

#### Scenario: Tick 兼容入口复用 Character 管线
- **WHEN** 旧兼容入口调用 `PlayerFullBodyActionController.Tick`
- **THEN** 该入口 MUST 通过 Character frame pipeline 推进一帧
- **AND** MUST NOT 维护一套与 tick phase adapter 不同的状态推进顺序

#### Scenario: 主入口不硬编码单个动作
- **WHEN** FullBody 主入口处理 Action 请求
- **THEN** 它 MUST 通过通用 FullBody Action request submission 或等价请求入口提交候选请求，并进入统一请求/打断仲裁
- **AND** MUST NOT 在主入口中直接硬编码 `BuildDodgeRequestFact` 作为唯一动作请求路径
- **AND** 后续 Attack、Jump 或其它 Action MUST 能复用同一请求入口

#### Scenario: Locomotion controller 作为 adapter
- **WHEN** Character frame pipeline 需要 Locomotion 输入、相机 facts、运动执行或动画提交
- **THEN** `PlayerLocomotionController` MAY 作为外围 adapter 提供这些能力
- **AND** 它 MUST NOT 成为 winning submission 选择或状态切换的第二权威

### Requirement: FullBody 单驱动装配
系统 MUST 防止同一角色在同一运行模式下同时由 frame `Update`、`LocomotionTickAdapter`、`FullBodyActionTickAdapter` 或 Character frame pipeline 之外的其它 handler 推进 gameplay。迁移兼容入口可以存在，但必须通过装配校验或显式配置保证每帧只有一个 gameplay driver active，且该 driver 必须进入唯一 Character frame pipeline。

#### Scenario: Tick 接管时关闭 frame 自动驱动
- **WHEN** FullBody tick adapter 接管某个角色
- **THEN** `PlayerFullBodyActionController` 的 frame auto update MUST 被关闭或跳过
- **AND** 同一角色的 locomotion-only tick adapter MUST NOT 同时推进 gameplay

#### Scenario: 单驱动校验报告冲突
- **GIVEN** 同一角色同时配置了 FullBody tick adapter 和 active locomotion-only tick adapter
- **WHEN** 运行装配校验或进入 Play Mode
- **THEN** 系统 MUST 报告配置冲突
- **AND** MUST 不依赖运行时互相压制来长期维持正确性

#### Scenario: 无 tick driver 时兼容运行
- **GIVEN** 场景未启用 `UnitySimulationTickDriver`
- **WHEN** 角色使用 frame Update 兼容入口
- **THEN** 系统 MAY 通过同一 Character frame pipeline 推进
- **AND** MUST NOT 同时启用另一条 gameplay 推进路径
