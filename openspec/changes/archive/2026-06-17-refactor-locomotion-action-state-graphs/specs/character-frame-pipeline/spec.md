# character-frame-pipeline Delta

## MODIFIED Requirements
### Requirement: 请求提交和打断仲裁
系统 MUST 在 Locomotion graph 和 Action lifecycle 推进前收集 request submission。外部请求、输入缓冲请求、Dodge、TurnBack、Attack、Jump 或其它动作候选 MUST 通过统一 request submission 进入请求/打断仲裁。request provider MUST 只提交请求候选，不得直接切 Locomotion graph、执行运动、播放动画、消费输入或写 runtime blackboard。accepted Action request MUST 进入 Action lifecycle submission，而不是要求默认 Locomotion graph 进入 Action state。

#### Scenario: 外部请求进入统一仲裁
- **WHEN** 外部系统或输入缓冲提交 Dodge、TurnBack、Attack、Jump 或等价请求候选
- **THEN** 该请求 MUST 被转换为 request submission
- **AND** MUST 进入统一请求/打断仲裁入口
- **AND** MUST NOT 直接变成 graph active state

#### Scenario: accepted Action request 输入 Action lifecycle
- **WHEN** 请求/打断仲裁接受一个 Dodge 或等价 Action 请求
- **THEN** 系统 MUST 生成 accepted resolved action、Action lifecycle seed 或等价纯数据 submission
- **AND** Action lifecycle MUST 通过该 submission active 对应 action
- **AND** accepted request 的输入消费 MUST 仍由后续帧输出和角色级 apply 阶段决定
- **AND** 默认 Locomotion graph MUST NOT 通过该 request 进入 `Action.Dodge`

#### Scenario: accepted Locomotion request 输入 Locomotion graph
- **WHEN** 请求/打断仲裁接受 TurnBack 或等价 Locomotion request
- **THEN** 系统 MAY 生成 Locomotion request fact
- **AND** Locomotion graph MAY 通过该 fact 评估 Locomotion transition
- **AND** 该 fact MUST NOT 表达 Action lifecycle active state

#### Scenario: rejected request 不产生副作用
- **WHEN** 请求/打断仲裁拒绝一个请求
- **THEN** 系统 MUST NOT 消费该请求
- **AND** MUST NOT 切换 graph 或 lifecycle state
- **AND** MUST NOT 执行 motion 或提交 animation

### Requirement: FullBody 兼容迁移行为保持
系统 MUST 在迁移到唯一角色帧管线时保持当前 FullBody-only 行为输出一致，同时采用新的 Locomotion graph 与 Action lifecycle 分离口径。Idle、MoveStart、MoveLoop、MoveStop、TurnBack、Directional Dodge 和 Backstep Dodge 的输入消费、运动执行、动画提交、runtime facts 和诊断 trace MUST 可测试；Dodge active state MUST 由 Action lifecycle 表达，不再要求默认 graph active path 为 `/FullBody/Action/Dodge`。

#### Scenario: 基础移动行为保持
- **WHEN** 使用相同 WASD 输入、相同配置和相同 tick 序列运行迁移前后路径
- **THEN** Idle、MoveStart、MoveLoop 和 MoveStop 的 Locomotion phase 序列 MUST 等价
- **AND** 基础移动运动命令来源 MUST 等价
- **AND** base layer animation 提交语义 MUST 等价

#### Scenario: Dodge 行为保持
- **WHEN** 使用相同 Dodge 输入、相同配置和相同 tick 序列运行迁移前后路径
- **THEN** Directional Dodge 和 Backstep Dodge 的 accepted/rejected 结果 MUST 等价
- **AND** 动作运动结果 MUST 等价
- **AND** Dodge active 时基础移动输出 MUST 不被重复提交
- **AND** Action lifecycle MUST 表达 active `Action.Dodge`
- **AND** 默认 Locomotion graph MUST NOT active `Action.Dodge`

#### Scenario: Directional 后续 Run 行为保持
- **GIVEN** 玩家有移动输入并按下 Shift 进入 Directional Dodge
- **AND** 动作完成帧仍有移动输入
- **WHEN** 输出应用完成该帧
- **THEN** pipeline MUST 将 Run latch frame output 写入 Locomotion output runtime
- **AND** 后续保持移动输入但松开 Shift 时 MUST 继续 Run

#### Scenario: 无移动或 Backstep 回 Idle
- **GIVEN** 玩家无方向按 Shift 进入 Backstep，或 Directional Dodge 完成帧没有移动输入
- **WHEN** Action lifecycle 等到匹配动作动画播放完成并完成动作
- **THEN** pipeline MUST NOT 写 Run latch
- **AND** Locomotion MUST 能回到 Idle

#### Scenario: TurnBack 行为保持
- **WHEN** 使用相同 RunLoop 反向输入、相同配置和相同 tick 序列运行迁移前后路径
- **THEN** TurnBack 进入、输入抑制、运动源策略和退出结果 MUST 等价
- **AND** TurnBack 运动不得新增第二运动出口

### Requirement: Character Frame Pipeline 只消费动作请求解析结果
`CharacterFramePipeline` MUST 只消费 request submission 阶段输出的纯数据结果。动作请求的收集、解析和准入 MUST 在 pipeline 的 request submission 边界内完成；pipeline 主体 MUST NOT 直接读取 Attack、Dodge、Jump 或 HitReact 配置，也 MUST NOT 直接决定这些动作的 target graph state、动画 key 或 motion spec。

#### Scenario: Pipeline 不认识具体动作解析
- **GIVEN** 本帧存在 Attack、Dodge 或 Jump 输入请求
- **WHEN** `CharacterFramePipeline` 执行 GameplayDecision 或等价 request submission phase
- **THEN** 具体动作解析 MUST 已由 provider/resolver 与 action arbiter 完成
- **AND** pipeline MUST 只接收 accepted resolved action、interrupt decision 或等价 pure data submission
- **AND** pipeline MUST NOT 新增具体动作解析分支

#### Scenario: 输出阶段不反推动作请求
- **GIVEN** request submission 已输出 accepted resolved action
- **WHEN** pipeline 进入 BuildMotion、ExecuteMotion、PresentationBridge 或 WriteSnapshotAndEvents
- **THEN** 输出阶段 MUST 只消费 lifecycle frame、motion result、animation request 和 runtime facts
- **AND** 输出阶段 MUST NOT 重新读取输入缓冲来决定 Attack、Dodge 或 Jump

#### Scenario: 没有第二条 action 入口
- **WHEN** 新动作通过通用 request provider/resolver 接入
- **THEN** 它 MUST 继续进入唯一 CharacterFramePipeline
- **AND** MUST NOT 新增第二 pipeline、第二 runner、第二 motion executor 或第二 animation presenter

### Requirement: 输出合成先于输出应用
系统 MUST 在执行任何运动、动画、输入消费、Run latch 写入、runtime facts 写入或 snapshot/events commit 之前，先由角色级 output composer 合成本帧最终输出。第一版 composer MAY 只接收 FullBody 一个 `CharacterFrameSubmission` 来源，但仍 MUST 是副作用应用前的唯一裁决位置。

#### Scenario: 单一 FullBody 来源仍经过 composer
- **GIVEN** 本帧只有 FullBody 提交
- **WHEN** 角色帧管线进入输出合成阶段
- **THEN** composer MUST 从 FullBody 提交中选择最终 movement 输出
- **AND** MUST 从 FullBody 提交中选择最终 animation 输出
- **AND** MUST 从 FullBody 提交中选择最终 input consume 输出
- **AND** MUST 从 FullBody 提交中选择最终 runtime facts 输出
- **AND** MUST 从 FullBody 提交中选择最终 Run latch 输出

#### Scenario: 副作用只在 apply 阶段发生
- **WHEN** 角色帧管线应用 composer 结果
- **THEN** motion executor 调用 MUST 只发生在角色级 output applier 或等价提交阶段
- **AND** animation presenter 调用 MUST 只发生在角色级 output applier 或等价提交阶段
- **AND** input buffer consume MUST 只发生在角色级 output applier 或等价提交阶段
- **AND** Run latch 写入 MUST 只发生在角色级 output applier 或等价提交阶段
- **AND** runtime blackboard 写入 MUST 只发生在角色级 output applier 或等价提交阶段
