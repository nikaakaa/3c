# committed-action-node-selection Specification

## Purpose
定义 Committed Action selector 节点、条件评估、确定性选择顺序和未命中无 timeline 输出的正式行为。
## Requirements
### Requirement: Committed Action Selection Nodes
CommittedActionBranch MUST 支持固定 Branch Root、Selector、Condition 和 Timeline 四类最小节点或批准的等价节点，使一个 accepted committed action 能从固定入口进入内部选择并根据只读上下文选择具体 timeline。Branch Root MUST 只表达单个 committed action branch 的固定入口和唯一 child 转发，MUST NOT 表达 gameplay condition、timeline 输出或跨 Action 跳转。该选择 MUST 发生在 Action lifecycle 已经 accepted action 之后，MUST NOT 替代 Action request / interrupt 仲裁。

#### Scenario: Selector 选择 Timeline
- **GIVEN** CommittedActionBranch root 是固定 Branch Root
- **AND** Branch Root 的唯一 child 是 selector
- **AND** 第一个 child condition 通过并指向 timeline A
- **WHEN** CommittedActionBranchEvaluator 评估 tick N
- **THEN** 它 MUST 只评估 timeline A 的输出
- **AND** MUST 返回 timeline A 对应的 CommittedActionBranchOutcome

#### Scenario: Branch Root 不承载业务语义
- **GIVEN** CommittedActionBranch root 是固定 Branch Root
- **WHEN** compiler 生成 runtime definition
- **THEN** Branch Root MUST 保留为稳定 root node
- **AND** selector、condition 或 timeline node MUST NOT 被提升为 branch root
- **AND** Branch Root MUST 只连接一个 child

#### Scenario: Selection 不决定请求准入
- **WHEN** CommittedActionBranch selector 评估 condition
- **THEN** 它 MUST 只决定当前 accepted action 的内部 timeline
- **AND** MUST NOT 接受或拒绝新的 action request

### Requirement: Condition 只读上下文
Action condition node MUST 只读取纯数据上下文，例如 request facts、movement intent、locomotion facts、runtime blackboard snapshot、active action id、source step 或批准的等价数据。Condition node MUST NOT 写状态、写黑板、消费输入、执行 motion 或播放 animation。

#### Scenario: Directional condition 读取移动意图
- **GIVEN** 当前 accepted action 是 Dodge
- **AND** condition 需要判断是否存在有效移动意图
- **WHEN** condition evaluator 运行
- **THEN** 它 MUST 从只读 movement / locomotion facts 判断
- **AND** MUST NOT 读取 Unity InputAction 或场景对象

#### Scenario: Condition 无副作用
- **WHEN** condition 评估失败
- **THEN** 系统 MUST NOT 因该失败写入 blackboard fact
- **AND** MUST NOT 消费 input buffer
- **AND** MUST NOT 改变 action lifecycle active state

### Requirement: Selector 评估顺序确定
Selector node MUST 按 runtime definition 中稳定 child 顺序评估，并选择第一个条件满足且可输出的 child。Selector MUST NOT 依赖非确定性集合枚举、Unity instance id 顺序或 editor view 顺序。

#### Scenario: 第一个通过 child 获胜
- **GIVEN** selector 有 child A 和 child B
- **AND** child A 与 child B 的 condition 都通过
- **WHEN** selector 评估
- **THEN** child A MUST 被选择
- **AND** child B MUST 不产生 timeline outcome

#### Scenario: 没有 child 通过
- **GIVEN** selector 的所有 child condition 都失败
- **WHEN** selector 评估
- **THEN** CommittedActionBranchOutcome MUST 不包含 timeline 输出
- **AND** MUST 包含明确 diagnostics 或等价 rejected selection result
- **AND** MUST NOT 使用隐藏 fallback timeline

### Requirement: 未选中 Timeline 不输出
未被当前 selector 选择的 TimelineNode MUST NOT 输出 motion、animation、active window fact 或 cue request。CommittedActionBranchOutcome MUST 只反映选中路径。

#### Scenario: 未选中 cue 不触发
- **GIVEN** timeline A 被选中
- **AND** timeline B 在同一 frame 有 cue clip
- **WHEN** selector 评估
- **THEN** output MUST 只包含 timeline A 的 cue request
- **AND** timeline B 的 cue request MUST NOT 出现在 outcome 中

### Requirement: Action Selection Nodes 可测试和可验证
系统 MUST 提供自动测试和静态边界验证，证明 Action selection nodes 是纯数据、确定性且不绕过角色帧管线。

#### Scenario: 自动测试覆盖选择语义
- **WHEN** 运行 Action selection EditMode 测试
- **THEN** 测试 MUST 覆盖 selector 顺序、condition true/false、未选中 timeline 不输出和无 fallback 行为

#### Scenario: 静态边界验证
- **WHEN** 检查 Action selection runtime 源码
- **THEN** 静态测试 MUST 确认它不引用 `MonoBehaviour`、`Transform`、`Animator`、`InputAction` 或 GraphView
- **AND** MUST 确认它不直接写 `CharacterRuntimeBlackboard`

### Requirement: Action Condition 可配置模型
CommittedActionBranch condition node MUST 使用可配置 typed condition model 表达条件。condition model MUST 至少支持 `Always`、`RequestHeld`、`RequestReleased`、`RequiredFactActive`、`TimelineComplete`、`HasMoveIntent` 和 `ActionVariantEquals`。condition authoring MUST 编译为纯 runtime condition 数据，且 MUST NOT 保存 Unity scene object、Animator、Animancer runtime object、InputAction、GraphView 或 MonoBehaviour。

#### Scenario: Condition 编译为 runtime 数据
- **GIVEN** Branch authoring 中存在 `RequiredFactActive` condition
- **WHEN** action definition compiler 编译 branch
- **THEN** runtime branch definition MUST 保存 condition kind 和 required fact id
- **AND** runtime definition MUST NOT 保存 editor view object 或 Unity scene reference

#### Scenario: 普通动作不新增专用 condition class
- **GIVEN** 设计者需要配置 Start -> Loop -> End 这类普通动作节点
- **WHEN** 该动作只需要 held、released 和 timeline complete 条件
- **THEN** 系统 MUST 能用通用 condition kind 表达该流转
- **AND** MUST NOT 要求新增动作专用 evaluator switch 才能表达该普通流转

#### Scenario: Condition kind 不使用具体动作语义命名
- **WHEN** 新增 condition kind
- **THEN** condition kind MUST 表达通用事实或通用判断
- **AND** MUST NOT 命名为 BlockOnly、AttackCombo、GuardCounterReady、DodgeBackstepOnly 或等价具体动作专用语义

### Requirement: Condition Payload 按 Kind 校验
condition payload MUST 按 condition kind 进行校验和编译。未被当前 kind 使用的 payload MUST NOT 影响 runtime evaluator 结果。非法 payload MUST 产生 validator error 或 warning；compiler MUST NOT 通过隐藏默认值、字符串猜测或动作专用分支补齐 condition。

#### Scenario: RequestHeld 需要 request kind 或批准等价默认请求语义
- **GIVEN** condition kind 为 `RequestHeld`
- **WHEN** validator 检查该 condition
- **THEN** validator MUST 能解析该 condition 对应的 request kind 或批准等价 current action request
- **AND** MUST NOT 从 Unity InputAction 名称猜测 request kind

#### Scenario: ActionVariantEquals 需要稳定 variant id
- **GIVEN** condition kind 为 `ActionVariantEquals`
- **WHEN** compiler 编译该 condition
- **THEN** runtime definition MUST 保存稳定 variant id
- **AND** MUST NOT 保存 editor display name 作为正式判断依据

#### Scenario: 未使用 payload 不改变结果
- **GIVEN** condition kind 为 `Always`
- **AND** authoring payload 中错误保留了 required fact id
- **WHEN** compiler 或 validator 处理该 condition
- **THEN** 系统 MAY 输出 warning
- **AND** evaluator 结果 MUST NOT 被该未使用 required fact id 改变

### Requirement: Condition Fact Id 校验
系统 MUST 对 condition 引用的 fact id 提供校验。`RequiredFactActive` 或批准等价 condition 引用的 fact id MUST 能从 action/timeline authoring、runtime fact registry、测试 fixture 或批准等价 fact source 中解析。缺失 fact id MUST 报告错误，MUST NOT 通过隐藏默认事实、字符串猜测或 fallback 继续编译为正式 runtime branch。

#### Scenario: 缺失 fact id 报错
- **GIVEN** Branch condition 引用 `window.counter.open`
- **AND** 当前 action definition、timeline authoring 和 runtime fact registry 都没有声明该 fact id
- **WHEN** 运行 branch validator
- **THEN** validator MUST 报告缺失 fact id
- **AND** compiler MUST NOT 生成可被正式 runtime 消费的半成品 branch

#### Scenario: Timeline window fact 可被引用
- **GIVEN** TimelineNode 中声明了 `window.block.active`
- **AND** Branch condition 引用 `window.block.active`
- **WHEN** validator 检查该 action definition
- **THEN** condition MUST 被视为引用已知 fact id

#### Scenario: Fact id 不通过前缀猜测解析
- **GIVEN** TimelineNode 只声明了 `window.block.active`
- **AND** Branch condition 引用 `window.block`
- **WHEN** validator 检查该 action definition
- **THEN** validator MUST 报告缺失或不匹配 fact id
- **AND** MUST NOT 因字符串前缀相似而接受该引用

### Requirement: Condition 共享 Fact Compile Context
Condition compiler 和 validator MUST 使用共享 Action fact compile context、fact id resolver 或批准等价解析入口。该解析入口 MUST 聚合当前 action definition、timeline authoring、request fact source、runtime fact registry、locomotion fact source 和测试 fixture 中声明的 fact id。Condition 与 Action transition policy matrix 对同一个 fact id MUST 得到一致解析结果，MUST NOT 各自维护互相独立的隐藏 fact registry。

#### Scenario: Condition 和 Policy 解析同一窗口事实
- **GIVEN** TimelineNode 声明了 `window.test.counter.open`
- **AND** Branch condition 引用 `window.test.counter.open`
- **AND** transition policy row 也引用 `window.test.counter.open`
- **WHEN** 运行共享 fact resolver
- **THEN** condition validator 和 policy validator MUST 都解析到同一个 fact declaration
- **AND** MUST NOT 出现 condition 合法但 policy 缺失或反向不一致的结果

#### Scenario: 冲突声明报错
- **GIVEN** 同一 compile context 中两个 fact source 声明了相同 fact id
- **AND** 两者的窗口、来源或 payload 语义冲突
- **WHEN** validator 检查 fact declarations
- **THEN** validator MUST 报告冲突错误
- **AND** compiler MUST NOT 选择其中一个作为隐藏默认声明

#### Scenario: Resolver 不读取运行时对象
- **WHEN** fact resolver 收集可引用 fact id
- **THEN** resolver MUST 只读取 authoring、compiled definition、registry 或测试 fixture 数据
- **AND** MUST NOT 读取 scene object、MonoBehaviour、runtime blackboard、Animator、Animancer 或 InputAction

### Requirement: Condition Evaluator 只读纯数据
condition evaluator MUST 只读取纯数据 evaluation context。该 context MAY 包含 request facts、active window facts、locomotion facts、runtime blackboard snapshot、active action variant、timeline local tick 和 source step。condition evaluator MUST NOT 写黑板、消费输入、接受 action request、切换 action、执行 motion、播放 animation 或访问 Unity scene object。

#### Scenario: RequestHeld 只读请求事实
- **GIVEN** condition kind 为 `RequestHeld`
- **WHEN** evaluator 运行
- **THEN** evaluator MUST 只读取输入请求事实或批准等价纯数据
- **AND** MUST NOT 读取 Unity `InputAction`
- **AND** MUST NOT 消费 input buffer

#### Scenario: TimelineComplete 不读取表现层时间
- **GIVEN** condition kind 为 `TimelineComplete`
- **WHEN** evaluator 判断当前 TimelineNode 是否完成
- **THEN** evaluator MUST 使用 action-local tick 和 runtime timeline duration
- **AND** MUST NOT 使用 Animancer normalized time、Animator state time、Unity render delta 或 editor preview time

#### Scenario: HasMoveIntent 不读取场景相机
- **GIVEN** condition kind 为 `HasMoveIntent`
- **WHEN** evaluator 判断是否存在移动意图
- **THEN** evaluator MUST 读取 locomotion facts、movement intent facts 或批准等价纯数据
- **AND** MUST NOT 读取 Camera、Transform、CharacterController 或 scene object

### Requirement: Request Condition 帧语义
`RequestHeld` 与 `RequestReleased` MUST 基于 condition evaluation context 中预采样的纯 request facts。对同一 request kind，release tick 上 `RequestReleased` MUST 为 true，`RequestHeld` MUST 为 false 或被 evaluator 以 release 语义压制。request facts MUST 携带 source tick、logic tick 或批准等价新鲜度信息，防止 release fact 跨 tick 重复触发。

#### Scenario: Release tick 进入 End
- **GIVEN** `Action.TestHold` 当前 TimelineNode 为 Loop
- **AND** Loop 有 `RequestHeld` self edge
- **AND** Loop 也有 `RequestReleased` 到 End 的 edge
- **AND** 当前 evaluation context 包含同一 request kind 的 released fact
- **WHEN** Branch evaluator 评估 Loop 的 children
- **THEN** `RequestReleased` MUST 能让 Branch 选择 End
- **AND** `RequestHeld` MUST NOT 因上一帧按住状态继续让 Loop self edge 获胜

#### Scenario: Released fact 不跨 tick 重复触发
- **GIVEN** request kind 在 tick N 产生 released fact
- **WHEN** evaluator 在 tick N+1 使用新的 request fact context
- **THEN** `RequestReleased` MUST 不再因为 tick N 的 release 返回 true
- **AND** evaluator MUST NOT 从输入设备重新推导 release 状态

### Requirement: TimelineComplete 边界确定
`TimelineComplete` condition MUST 使用 compiled runtime timeline duration ticks 和 action-local tick 判断完成。完成边界 MUST 使用 `localTick >= durationTicks` 或批准等价确定性规则，并且 compiler、runtime evaluator、editor preview 和测试 MUST 使用同一边界口径。`TimelineComplete` MUST NOT 自行读取或换算 seconds authoring、Animancer normalized time、Animator state time、Unity render delta 或 editor preview time。

#### Scenario: duration tick 到达时完成
- **GIVEN** runtime timeline duration ticks 为 5
- **AND** action-local tick 为 5
- **WHEN** evaluator 评估 `TimelineComplete`
- **THEN** condition MUST 返回 true

#### Scenario: duration tick 前不完成
- **GIVEN** runtime timeline duration ticks 为 5
- **AND** action-local tick 为 4
- **WHEN** evaluator 评估 `TimelineComplete`
- **THEN** condition MUST 返回 false

### Requirement: Condition Editor Adapter 写回正式 Authoring
Branch Editor MUST 通过 serialized adapter 读写 condition authoring。Editor UI MAY 展示 node panel、kind selector、payload field 和 fact id diagnostics，但保存结果 MUST 写回正式 `CharacterActionDefinitionSO` branch authoring 或批准等价 action definition 数据源。Editor preview MUST 使用 compiler/evaluator 的正式路径，MUST NOT 维护第二套 preview-only condition runtime。

#### Scenario: 编辑 RequiredFactActive 写回 Action Definition
- **GIVEN** Branch Editor 选中一个 Condition node
- **WHEN** 设计者将 kind 设置为 `RequiredFactActive` 并填写 `window.counter.open`
- **THEN** serialized adapter MUST 将 kind 和 fact id 写回 action definition 的 branch authoring
- **AND** 下一次编译 MUST 从同一份 authoring 生成 runtime condition definition

#### Scenario: Preview 不绕过 compiler
- **WHEN** Branch Editor preview condition selection
- **THEN** preview MUST 使用 action definition compiler 和 condition evaluator 的正式或批准等价路径
- **AND** MUST NOT 直接从 GraphView edge、EditorWindow state 或 preview-only object 决定 runtime branch outcome
