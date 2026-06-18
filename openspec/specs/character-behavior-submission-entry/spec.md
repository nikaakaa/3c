# character-behavior-submission-entry Specification

## Purpose
定义 CharacterBehaviorSubmissionRunner 作为正式 request/output 提交入口的装配规则、默认配置要求和旧 submitter chain 迁移边界。
## Requirements
### Requirement: Behavior Submission Entry 成为默认提交入口
系统 MUST 在前置合同、chain boundary 和 Dodge golden line 完成后，将 `CharacterRuntimeCore` 默认 frame submitter 入口替换为 behavior submission entry。该入口 MUST 仍由 `CharacterFramePipeline` 调用，MUST NOT 成为第二个 frame pipeline 或 Unity tick owner。

#### Scenario: RuntimeCore 使用新入口
- **WHEN** `CharacterRuntimeCore` 创建默认 runtime host
- **THEN** 它 MUST 使用 behavior submission entry 或批准的等价 submitter
- **AND** MUST NOT 使用旧 submitter chain 作为默认生产入口

#### Scenario: Pipeline phase 不变
- **WHEN** behavior submission entry 参与 tick N
- **THEN** `CharacterFramePipeline` phase 顺序 MUST 保持不变
- **AND** ExecuteMotion、PresentationBridge 和 WriteSnapshotAndEvents 仍由 pipeline phase 驱动

### Requirement: Behavior Entry 配置必须正式存在
Behavior submission entry MUST 来自明确的 runtime definition、配置或批准的等价装配数据。系统 MUST NOT 在缺失配置时生成隐藏 fallback tree、Resources 默认树或代码默认角色行为树。

#### Scenario: 缺失 Entry Definition 报错
- **GIVEN** 角色配置缺失 behavior entry root 或 required leaf
- **WHEN** `CharacterRuntimeCore` 创建默认 runtime host
- **THEN** 系统 MUST 报告配置错误或拒绝创建该 entry
- **AND** MUST NOT 自动生成隐藏 root / Locomotion / Action fallback tree

### Requirement: RequestPass 顺序保持
Behavior submission entry MUST 在 RequestPass 中先运行 Locomotion leaf，准备 movement facts、timeline facts 或等价 decision context，再运行 Committed Action leaf 消费这些 context 完成 request resolution / interrupt arbitration。

#### Scenario: Locomotion 先准备 request context
- **WHEN** RequestPass 运行
- **THEN** Locomotion leaf MUST 先产生本帧 movement / locomotion context
- **AND** Action leaf MUST 后消费该 context

#### Scenario: Action 不在 OutputPass 重新仲裁
- **WHEN** OutputPass 运行
- **THEN** Action leaf MUST 使用 RequestPass 已确定的 accepted / rejected request 结果
- **AND** MUST NOT 重新读取输入缓冲决定 Dodge 是否 accepted

### Requirement: OutputPass 顺序保持
Behavior submission entry MUST 在 OutputPass 中先运行 Locomotion leaf，构建 state frame、locomotion frame 或等价基础移动候选，再运行 Committed Action leaf 消费这些 context 生成 action output submission。

#### Scenario: Locomotion 先写 output context
- **WHEN** OutputPass 运行
- **THEN** Locomotion leaf MUST 先写入 state frame / locomotion frame context
- **AND** Action leaf MUST 后消费该 context 构建 action output

#### Scenario: Action 不改 Locomotion 私有状态
- **WHEN** Action leaf 提交 full-body claim
- **THEN** 它 MUST 通过 behavior output submission 表达 claim
- **AND** MUST NOT 直接修改 Locomotion runtime private state

### Requirement: Composer 复用现有 FramePlan
Behavior submission composer MUST 将 typed submissions 转换为现有 `CharacterFrameSubmission`、`CharacterFrameArbitrationInput`、`BodyArbiter` 或 `CharacterFramePlan` 可消费的输入。Composer MUST NOT 新增第二套 body arbiter 或第二套 output applier。

#### Scenario: Action 与 Locomotion 进入同一计划
- **GIVEN** Locomotion leaf 提交基础移动候选
- **AND** Action leaf 提交 FullBody claim 和 action candidate
- **WHEN** composer 生成 plan input
- **THEN** 两者 MUST 进入同一个 `CharacterFramePlan` 或批准的等价角色级计划
- **AND** 最终选择 MUST 由现有 BodyArbiter 或等价角色级策略决定

#### Scenario: Composer 不执行副作用
- **WHEN** composer 转换 submissions
- **THEN** composer MUST NOT 调用 motion executor
- **AND** MUST NOT 调用 animation presenter
- **AND** MUST NOT 写 runtime blackboard

#### Scenario: Unsupported Submission 不静默丢弃
- **GIVEN** composer 收到无法映射到 frame plan input 的 required submission
- **WHEN** composer 处理该 submission
- **THEN** 系统 MUST 产生 diagnostic 或配置错误
- **AND** MUST NOT 静默忽略该 required submission 后继续表现为成功

### Requirement: 旧 Submitter Chain 退为迁移对象
旧 submitter chain 在本变更后 MUST NOT 作为默认生产入口。若短期保留，MUST 标注为迁移 adapter、测试 baseline 或兼容对象，并 MUST 有明确删除条件。

#### Scenario: 旧链路不是默认入口
- **WHEN** 检查 default runtime host
- **THEN** 旧 submitter chain MUST NOT 是默认 submitter
- **AND** 新行为不得继续扩展旧 chain 作为主线

#### Scenario: 保留时有删除条件
- **GIVEN** 旧 submitter chain 仍存在
- **WHEN** 检查其注释、测试或任务记录
- **THEN** 系统 MUST 能说明它的迁移用途
- **AND** MUST 能说明删除条件

### Requirement: Entry 替换保持 Golden Line
Behavior submission entry 替换默认入口后，Directional Dodge、Backstep Dodge、rejected Dodge、基础 Locomotion 和 restore 行为 MUST 与 Dodge golden line 和现有 frame pipeline 语义一致。

#### Scenario: Directional Dodge 等价
- **WHEN** 玩家有移动输入并触发 Dodge
- **THEN** behavior submission entry 产生的 final frame plan MUST 与 Dodge golden line 中 Directional 输出等价

#### Scenario: Backstep Dodge 等价
- **WHEN** 玩家无移动输入并触发 Dodge
- **THEN** behavior submission entry 产生的 final frame plan MUST 与 Dodge golden line 中 Backstep 输出等价

#### Scenario: Restore 等价
- **WHEN** rollback restore 到 Dodge 中间帧后继续 tick
- **THEN** behavior submission entry 输出 MUST 与 golden line restore 输出一致

### Requirement: Behavior Submission Entry 可测试和可验证
系统 MUST 提供自动测试和静态边界验证，证明默认入口替换没有引入第二 pipeline、第二 arbiter、第二 side-effect path 或不确定顺序。

#### Scenario: 自动测试覆盖入口替换
- **WHEN** 运行 behavior submission entry EditMode 测试
- **THEN** 测试 MUST 覆盖 RequestPass 顺序、OutputPass 顺序、default entry、Dodge golden line 等价和基础 Locomotion

#### Scenario: 静态边界验证
- **WHEN** 检查 behavior submission entry 源码
- **THEN** 静态测试 MUST 确认 wrappers 不直接调用 motion executor、animation presenter 或 blackboard writer
- **AND** MUST 确认没有新增第二 `CharacterFramePipeline`
