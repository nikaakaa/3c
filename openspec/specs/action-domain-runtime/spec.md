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
系统 MUST 将 FullBody、UpperBody 或经批准的等价身体输出范围表达为 body/channel claim。Action、Locomotion、Aim、HitReact 或未来 UpperBodyAction 是行为模块或 source；body/channel claim 只描述请求占用范围，MUST NOT 成为 gameplay owner、behavior graph leaf、runtime source、slot owner 或 animation presentation layer。

FullBody claim MUST 表示提交方在本帧请求全身占用。该 claim 被采纳后的正式输出 MUST 是 CommittedAction / Action-side owner 接管 `BaseSlot`，并压制冲突的 `UpperBodySlot`。系统 MUST NOT 把 `FullBody` 当作 slot owner 输出。UpperBody claim MAY 表示提交方请求占用 `UpperBodySlot`，但本要求本身不实现 UpperBody runtime source。

Body claim policy MUST 是正式配置、正式校验或正式错误；系统 MUST NOT 为缺失 claim policy 引入 fallback 配置。

#### Scenario: Dodge 提交 FullBody claim
- **WHEN** Action domain 接受 `Action.Dodge`
- **THEN** Dodge source MUST 输出 FullBody claim、动作 motion candidate、动作 animation candidate 和必要的 action facts
- **AND** 身体仲裁 MUST 将该 claim 解释为 Action-side owner 对 `BaseSlot` 的接管
- **AND** 系统 MUST NOT 要求存在 `FullBody` behavior node 才能执行 Dodge

#### Scenario: Locomotion 不提交 FullBody claim
- **WHEN** Locomotion source 提交基础移动候选
- **THEN** Locomotion MUST 以 movement source 参与 `BaseSlot` 候选
- **AND** Locomotion MUST NOT 通过 FullBody claim 把自己伪装为 Action 或全身动作

#### Scenario: 缺失 claim policy
- **WHEN** 某个 source 输出了当前正式配置无法识别的 body/channel claim
- **THEN** 校验或运行时构建 MUST 报告正式错误
- **AND** 系统 MUST NOT 自动降级到默认 FullBody、默认 UpperBody 或临时 fallback claim

#### Scenario: claim 和 slot owner 不混用
- **WHEN** 测试、compiler 或 editor adapter 检查身体仲裁结果
- **THEN** 结果 MUST 记录 `BaseSlot` owner、`UpperBodySlot` owner 或批准的等价 slot owner
- **AND** 结果 MUST NOT 把 `FullBody` 当作 slot owner 名称

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
系统 MUST 不再使用旧 FullBody action controller、旧集成 adapter、旧 Action presenter、旧 Dodge 平铺配置或等价兼容 API 作为正式扩展入口。历史类型若短期存在，MUST 只作为迁移残留或只读诊断，不得进入正式 prefab、scene、runtime port、submitter graph 或 rollback replay 主线。

#### Scenario: 正式 runtime 不依赖旧 Host
- **WHEN** 检查正式角色 runtime 装配、prefab、scene 和测试 fixture
- **THEN** 它们 MUST 通过 `CharacterFrameRuntimeController`、Action submitter、Action runtime 和正式 runtime ports 组合
- **AND** MUST NOT 通过旧 FullBody action controller 或旧集成 adapter 推进 gameplay

#### Scenario: 新 Action 不复用旧路径
- **WHEN** 后续新增 Attack、Jump、HitReact 或 Skill
- **THEN** 新动作 MUST 通过 Action Catalog、Action provider/resolver、Action runtime 和角色帧管线接入
- **AND** MUST NOT 新增 `PlayerAttackController`、`PlayerJumpController` 或等价 MonoBehaviour gameplay 入口

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

