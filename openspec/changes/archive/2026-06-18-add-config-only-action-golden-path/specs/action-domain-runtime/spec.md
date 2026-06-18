## ADDED Requirements

### Requirement: Config-Only Action 金线路径
系统 MUST 提供纯配置 Action golden path，用于证明普通 Action 可以通过 ActionDefinition、Branch、Timeline、Condition、Transition Policy、BodyClaim 和 AnimationKey 配置完成。该 golden path MUST NOT 要求新增动作专用 MonoBehaviour、角色帧 phase、motion executor、animation presenter、blackboard writer、`CharacterFramePipeline` 分支或具体 action id switch。

#### Scenario: TestHold 纯配置运行
- **GIVEN** 测试 fixture 配置了 `Action.TestHold`
- **AND** `Action.TestHold` branch 包含 Start、Loop 和 End TimelineNode
- **WHEN** 角色帧主线推进 TestHold 请求
- **THEN** Action runtime MUST 通过通用 request、lifecycle、branch、timeline 和 output path 产出动作输出
- **AND** 系统 MUST NOT 依赖 `PlayerTestHoldController` 或等价专用 runtime class

#### Scenario: 普通新动作不修改角色帧主线
- **WHEN** 新增一个只使用既有 condition、timeline、policy、claim 和 animation key 能力的普通 Action
- **THEN** 该动作 MUST 通过资产或测试 fixture 配置完成
- **AND** MUST NOT 修改 `CharacterFramePipeline`、motion executor、animation presenter 或角色控制入口

#### Scenario: Golden path 不进入正式玩法配置
- **WHEN** 系统装配正式 Corin gameplay 配置、prefab 或 scene
- **THEN** `Action.TestHold` 和 `Action.TestCounter` MUST NOT 作为正式可玩能力挂入角色配置
- **AND** 它们 MUST 只存在于测试 fixture、测试资产目录或批准等价测试归属中

### Requirement: Golden Path 使用正式 Compiler
Config-only golden path MUST 使用正式 ActionDefinition、Branch、Timeline、Condition 和 Transition Policy authoring compiler / validator / evaluator。测试 MAY 在内存中构造 authoring fixture 或使用测试资产，但 MUST NOT 直接构造 test-only runtime branch definition、timeline definition、policy runtime 或 action switch 来绕过 compiler。

#### Scenario: Fixture 通过正式 compiler
- **GIVEN** 测试 fixture 构造了 `Action.TestHold` authoring 数据
- **WHEN** golden path 测试运行
- **THEN** fixture MUST 通过正式 action definition compiler 生成 runtime action definition
- **AND** Branch、Timeline、Condition 和 Policy runtime 数据 MUST 来自正式 compiler 输出

#### Scenario: 不手搓 runtime definition
- **WHEN** golden path 测试需要 `Action.TestCounter` policy
- **THEN** 测试 MUST 通过 matrix row authoring 或批准等价 policy authoring 编译得到 runtime policy
- **AND** MUST NOT 直接 new 一个仅供测试使用的 runtime policy 让 arbiter 通过

#### Scenario: compiler error 阻止 golden path
- **GIVEN** TestHold authoring 缺失 required fact declaration 或非法 condition payload
- **WHEN** validator 报告 error
- **THEN** golden path MUST 失败并暴露配置错误
- **AND** MUST NOT 通过代码默认值或 test helper 补齐缺失 runtime 数据

### Requirement: TestHold Start Loop End 配置闭环
系统 MUST 用 `Action.TestHold` 或批准等价测试动作证明 Start -> Loop -> End 可以纯配置完成。Start MUST 能通过 `TimelineComplete` 进入 Loop；Loop MUST 能通过 `RequestHeld` 保持自身；Loop MUST 能通过 `RequestReleased` 进入 End；End MUST 在 timeline 完成后通过正式 Action lifecycle completion 退出 action 或进入空 action。

#### Scenario: Start 完成进入 Loop
- **GIVEN** `Action.TestHold` 当前 TimelineNode 为 Start
- **AND** Start runtime timeline 已完成
- **WHEN** Branch evaluator 评估下一步
- **THEN** selected TimelineNode MUST 变为 Loop

#### Scenario: Held 保持 Loop
- **GIVEN** `Action.TestHold` 当前 TimelineNode 为 Loop
- **AND** TestHold request held fact 为 active
- **WHEN** Branch evaluator 评估下一步
- **THEN** selected TimelineNode MUST 保持 Loop

#### Scenario: Released 进入 End
- **GIVEN** `Action.TestHold` 当前 TimelineNode 为 Loop
- **AND** TestHold request released fact 为 active
- **WHEN** Branch evaluator 评估下一步
- **THEN** selected TimelineNode MUST 变为 End

#### Scenario: End 完成退出
- **GIVEN** `Action.TestHold` 当前 TimelineNode 为 End
- **AND** End runtime timeline 已完成
- **AND** 正式 Action motion/lifecycle completion 收到完成结果
- **WHEN** Action lifecycle 推进
- **THEN** active action MUST 退出到空 action、Locomotion 可接管状态或批准等价退出状态

### Requirement: Config-Only 跨 Action 跳转金线
系统 MUST 用纯配置 TestCounter 或批准等价测试动作证明跨 Action 跳转通过 request provider、transition policy、interrupt arbiter 和 action lifecycle 完成。Branch graph MUST NOT 直接持有跨 Action 边。

#### Scenario: TestHold 到 TestCounter 走 policy
- **GIVEN** `Action.TestHold` 输出 `window.test.counter.open`
- **AND** policy 配置允许 `Action.TestHold -> Action.TestCounter`
- **WHEN** 对应 request 在窗口内提交
- **THEN** interrupt arbiter MUST 接受 `Action.TestCounter`
- **AND** `Action.TestHold` Branch MUST NOT 直接跳到 `Action.TestCounter` node

#### Scenario: 缺少 required fact 时拒绝跳转
- **GIVEN** policy 需要 `window.test.counter.open`
- **AND** 当前 tick 没有该 active fact
- **WHEN** TestCounter request 提交
- **THEN** interrupt arbiter MUST 拒绝或输出明确诊断
- **AND** 系统 MUST NOT 使用隐藏 fallback 允许跳转

#### Scenario: TestCounter 由 lifecycle 进入
- **GIVEN** interrupt arbiter accepted `Action.TestCounter`
- **WHEN** Action runtime 推进本帧 lifecycle
- **THEN** Action lifecycle MUST 将 active action 切换为 `Action.TestCounter`
- **AND** policy matrix、Branch evaluator 和 Timeline evaluator MUST NOT 直接创建 active action state

### Requirement: Config-Only Timeline Fact 金线
`Action.TestHold` MUST 能通过通用 timeline fact 输出产生 `window.test.counter.open` 或批准等价测试窗口事实。该 fact MUST 被 transition policy 作为 required fact 引用。窗口激活与否 MUST 由 runtime timeline tick 和已编译 timeline duration/window 数据决定，MUST NOT 由 Animancer normalized time、Animator state time、Unity render delta 或 editor preview time 决定。

#### Scenario: Loop 输出 counter window fact
- **GIVEN** `Action.TestHold` 当前 TimelineNode 为 Loop
- **AND** runtime local tick 位于 `window.test.counter.open` 窗口内
- **WHEN** timeline evaluator 运行
- **THEN** Action candidate MUST 包含 active fact `window.test.counter.open`

#### Scenario: 窗口外不输出 counter fact
- **GIVEN** `Action.TestHold` 当前 TimelineNode 为 Loop
- **AND** runtime local tick 不在 `window.test.counter.open` 窗口内
- **WHEN** timeline evaluator 运行
- **THEN** Action candidate MUST NOT 包含 active fact `window.test.counter.open`

### Requirement: Config-Only Output 金线
Config-only Action golden path MUST 通过正式 `CharacterFramePipeline` output plan、OutputApplier 或批准等价角色级输出阶段提交 motion、animation 和 facts。Action runtime、Branch evaluator、Timeline evaluator 和 policy matrix MUST NOT 直接调用 motion executor、animation presenter、CharacterController、Animator、Animancer 或 runtime blackboard writer。

#### Scenario: TestHold 输出经角色级出口
- **GIVEN** `Action.TestHold` timeline 输出 animation key 和 body claim
- **WHEN** 角色帧主线生成最终 plan
- **THEN** fake animation output port 或批准等价测试端口 MUST 从 OutputApplier 收到 animation key
- **AND** Action runtime MUST NOT 直接调用 animation presenter

#### Scenario: TestCounter 输出经角色级出口
- **GIVEN** `Action.TestCounter` 已被 lifecycle 接受
- **WHEN** 角色帧主线推进 TestCounter
- **THEN** fake output port MUST 收到 TestCounter 的 animation key、claim 或批准等价输出
- **AND** TestCounter runtime MUST NOT 绕过 CharacterFramePipeline 直接执行表现副作用

### Requirement: Config-Only Slot Contract 金线
Config-only Action golden path MUST 验证 body claim 与 slot owner 分离。`Action.TestHold` 和 `Action.TestCounter` MAY 使用 FullBody claim，但 FullBody MUST 只作为 claim kind。最终 frame plan MUST 使用 `BaseSlot`、`UpperBodySlot` 或批准等价 slot contract 表达仲裁结果，MUST NOT 将 `FullBody` 作为 slot owner。

#### Scenario: FullBody claim 映射到 BaseSlot owner
- **GIVEN** `Action.TestHold` 输出 FullBody claim
- **AND** 该 claim 被角色级仲裁采纳
- **WHEN** `CharacterFramePlan` 生成
- **THEN** `BaseSlot` owner MUST 是 Action-side owner、CommittedAction 或批准等价 owner
- **AND** `FullBody` MUST NOT 作为 slot owner 输出

#### Scenario: UpperBodySlot 被压制
- **GIVEN** `Action.TestCounter` 输出 FullBody claim
- **AND** 本帧存在 UpperBodySlot 或批准等价扩展位
- **WHEN** 角色级 plan 合成完成
- **THEN** plan MUST 能表达 UpperBodySlot 被 FullBody claim 压制
- **AND** 表现层 MUST 只消费该 slot contract，不得反向决定 claim 是否采纳

#### Scenario: 测试断言不用旧 layer 口径
- **WHEN** golden path 测试检查身体仲裁结果
- **THEN** 测试 MUST 使用 `BaseSlotOwner`、`UpperBodySlotOwner`、`UpperBodySlotSuppressed` 或批准等价 slot contract
- **AND** MUST NOT 使用 `BaseLayerOwner`、`FullBody owner` 或表现层 layer 名称作为 gameplay 断言

### Requirement: Config-Only Action 静态边界
系统 MUST 提供静态边界测试，证明 config-only golden path 没有新增第二运行路径。测试 MUST 确认不存在 TestAction 专用 controller、专用角色帧入口、专用 motion executor、专用 animation presenter 或绕过 Action Catalog / ActionDefinition 的 runtime branch。

#### Scenario: 静态检查无专用 Controller
- **WHEN** 运行 config-only action 静态边界测试
- **THEN** 测试 MUST 确认 runtime 源码不存在 `PlayerTestActionController` 或批准命名之外的 TestAction 专用 MonoBehaviour gameplay 入口
- **AND** MUST 确认 TestAction 不通过代码默认 action definition、Resources 或 sample asset fallback 进入正式 runtime

#### Scenario: 静态检查无角色帧分支
- **WHEN** 运行 config-only action 静态边界测试
- **THEN** 测试 MUST 确认 `CharacterFramePipeline` 或批准等价角色帧主入口不存在 TestHold/TestCounter 专用分支
- **AND** MUST 确认 TestHold/TestCounter 通过 Action Catalog / ActionDefinition 或批准等价正式配置进入 runtime

#### Scenario: 静态检查无表现层专用分支
- **WHEN** 运行 config-only action 静态边界测试
- **THEN** 测试 MUST 确认 motion executor 和 animation presenter 不包含 TestHold/TestCounter 专用 action id switch
- **AND** MUST 确认 TestAction 没有专用 motion executor 或专用 animation presenter
