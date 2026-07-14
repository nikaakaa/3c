# character-gameplay-pipeline-closure Specification

## ADDED Requirements

### Requirement: 角色 Gameplay 管线必须形成 ActionInstance 事实闭环

系统 MUST 让本地输入、动作请求、Graph/StateMachine 决策、ActionRuntime、Timeline 输出、MotionStage、PresentationStage 和 SyncFacts 通过同一 `CharacterPipeline` 主线形成闭环。系统 MUST NOT 新增第二套角色控制器、第二套 Timeline 播放器、第二套网络输出或 demo 专用临时桥接。

#### Scenario: 本地预测攻击闭环

- **WHEN** 本地玩家输入攻击 request
- **THEN** `CharacterInputStage` MUST 生成 `CharacterInputFrame` 和 action request
- **AND** Graph 或 StateMachine MUST 通过正式 request 查询/消费入口创建 `ActionActivationRequest`
- **AND** `ActionRuntime` MUST 生成 `ActionInstance` 和 Action Context
- **AND** Timeline MUST 基于 Action Context 输出 window、motion、cue 或 animation facts
- **AND** Motion、Presentation 和 SyncFacts MUST 消费这些 facts 而不是读取旧动作配置

#### Scenario: 禁止分裂路径

- **WHEN** 实现 demo 攻击、闪避、防守、受击或支援动作
- **THEN** 实现 MUST 位于正式 `CharacterPipeline`、BTSMTL、ActionRuntime、Timeline、MotionStage、PresentationStage 或 GameplaySync 主线
- **AND** 实现 MUST NOT 通过旧 SO/config、旧状态机、旧 Timeline 播放器或场景脚本直接移动/播放/同步角色

### Requirement: Authoring 装配必须从 CharacterPipelineDefinition 汇入 runtime

系统 MUST 使用 `CharacterPipelineDefinition` 作为角色 authoring 装配入口。输入配置、RootTree、ActionProfile、动画 layer 和后续 demo 事实配置 MUST 汇入同一个角色管线定义或它的正式引用链。Host MUST 只负责装配注册，不承担动作决策或运动结算。

#### Scenario: 创建可玩的角色管线

- **WHEN** 场景中的角色启动
- **THEN** `CharacterPipelineHost` MUST 使用 `CharacterPipelineDefinition` 创建 `CharacterPipeline`
- **AND** 该 pipeline MUST 从同一正式定义读取输入配置、RootTree、ActionProfile 和动画 layer
- **AND** Host MUST NOT 直接序列化旧动作 SO、旧 locomotion 配置或业务状态机

#### Scenario: 缺少正式配置

- **WHEN** 角色缺少 RootTree、ActionProfile 或输入配置
- **THEN** 系统 MUST 通过正式配置校验或错误报告暴露问题
- **AND** 系统 MUST NOT 自动扫描场景、目录、同名 asset 或旧配置作为 fallback

### Requirement: Graph 和 Timeline 必须只输出 gameplay facts

系统 MUST 让 Graph、StateMachine 和 Timeline 输出 gameplay facts，而不是直接执行最终 Transform、命中裁决、扣血或网络发送。第一阶段 facts 至少包括 `ActionActivationRequest`、`ActionLifecycleTransition`、`ActionWindowSample`、`ActionMotionSample`、`ActionCueEvent`、`GameplayResultEvent`、`MotionContribution`、`MotionWarpWindow` 和 `AnimationContribution`。

#### Scenario: Timeline 输出攻击窗口

- **WHEN** 动作 Timeline 采样到 HitWindow、IFrameWindow、ParryWindow 或 CancelWindow
- **THEN** Timeline MUST 输出 `ActionWindowSample`
- **AND** sample MUST 关联当前 Action Context 的 `ActionInstanceId`
- **AND** Timeline MUST NOT 直接判定命中、扣血或发送网络包

#### Scenario: Timeline 输出动作位移

- **WHEN** 动作 Timeline 采样到 root motion 或 motion warp window
- **THEN** Timeline MUST 输出 `MotionContribution` 或 `MotionWarpWindow`
- **AND** 最终位移 MUST 由 `CharacterMotionStage` 结算
- **AND** Timeline MUST NOT 直接调用 `CharacterController.Move` 或修改 Transform

### Requirement: Motion 闭环必须依赖正式仲裁而不是直接移动

系统 MUST 在 `refactor-character-motion-arbitration` 完成后，将本闭环的所有 motion 来源接入正式 motion 仲裁链。最终顺序 MUST 是 contribution resolve、motion modifier、network correction phase、Move、MotionResult。所有 gameplay motion 来源 MUST 能被 debug 追踪。

#### Scenario: 输入和攻击 root motion 同帧出现

- **WHEN** 同一逻辑 tick 内存在输入 locomotion 和攻击 root motion
- **THEN** 输入 MUST 作为 locomotion contribution 进入 resolver
- **AND** 攻击 root motion MUST 作为 action contribution 进入 resolver
- **AND** MotionStage MUST 根据正式仲裁规则输出一个 `MotionIntent`

#### Scenario: 网络校正到达

- **WHEN** NetworkReceiveStage 收到 motion correction
- **THEN** correction MUST 在 MotionStage 的 correction phase 处理
- **AND** correction acknowledgement MUST 通过 `SyncFacts.Motion.CorrectionAcknowledgements` 输出
- **AND** 系统 MUST NOT 在 resolver 前直接硬设 Transform 作为正式路径

### Requirement: Presentation 闭环必须只消费表现事实

系统 MUST 让表现链路消费 `AnimationContribution`、`AnimationLayerPlaybackPlan` 和 `PresentationCue`。PresentationStage MUST NOT 自主推进 Timeline、执行动作决策、读取 transport 或输出 gameplay 裁决。

#### Scenario: Timeline 输出动画贡献

- **WHEN** Timeline 动画轨道采样出 `AnimationContribution`
- **THEN** `CharacterAnimationLayerRuntime` MUST 将贡献解析为 `AnimationLayerPlaybackPlan`
- **AND** Animancer adapter MUST 只应用该播放计划
- **AND** 表现层 MUST NOT 重新解释 ActionProfile 或重复推进 Timeline

#### Scenario: 本地表现 cue

- **WHEN** Timeline 或 Graph 输出 VFX、SFX、camera cue 或 hit stop
- **THEN** cue MUST 进入正式 presentation output 或 presentation sync facts
- **AND** 是否同步 MUST 由策略解析决定
- **AND** cue 实现 MUST NOT 直接绕过 SyncFacts 伪装成网络事件

### Requirement: SyncFacts 必须成为 demo 同步和 debug 的唯一事实出口

系统 MUST 使用 `SyncFacts` 作为本 tick 已发生 gameplay facts 的唯一出口。`CharacterNetworkSendStage`、`CharacterGameplaySyncAdapter`、`ActionNetworkPolicyResolver` 和 `GameplaySyncRuntime` MUST 从 `SyncFacts` 收集、解析、记录和发送 facts。系统 MUST NOT 恢复旧 `NetworkOutput` 或让节点/Timeline 直接发送 transport 消息。

#### Scenario: 收集动作同步事实

- **WHEN** 本 tick 产生 activation、lifecycle、window、motion、cue、gameplay result 或 correction ack
- **THEN** 这些 facts MUST 写入 `CharacterPipelineOutput.SyncFacts`
- **AND** `CharacterNetworkSendStage` MUST 收集这些 facts
- **AND** `CharacterGameplaySyncAdapter` MUST 通过 action profile 或正式 policy resolver 决定是否发送

#### Scenario: 没有网络后端

- **WHEN** 后端配置为 `None`
- **THEN** pipeline MAY 继续产生 `SyncFacts`
- **AND** gameplay 本地表现 MUST 继续运行
- **AND** 系统 MUST NOT 因没有 peer 而切换到第二套 gameplay 输出路径

### Requirement: 第一阶段网络后端只覆盖 None 和 LocalLoopback

系统 MUST 在本闭环第一阶段只要求 `None` 和 `LocalLoopback` 后端。LocalLoopback MUST 能覆盖动作确认、动作拒绝、motion correction、gameplay result 和 debug history 的最小压力链路。真实 Fantasy 后端 MUST 留作后续正式 change，不得以 fake 配置进入当前 Inspector。

#### Scenario: LocalLoopback 模拟服务端压力

- **WHEN** 本地预测角色发送 action activation、window digest、motion digest 或 gameplay result proposal
- **THEN** LocalLoopback MAY 延迟确认、拒绝动作、注入 motion correction 或返回 gameplay result
- **AND** 这些 incoming facts MUST 通过 `CharacterGameplaySyncAdapter.DrainIncoming` 进入 `CharacterNetworkReceiveStage`
- **AND** pipeline MUST 通过正式 receive/action/motion/presentation 链路处理

#### Scenario: 后续接入 Fantasy

- **WHEN** 后续实现真实 Fantasy peer
- **THEN** Fantasy peer MUST 替换 GameplaySync peer adapter
- **AND** `CharacterPipeline`、`SyncFacts`、ActionRuntime、MotionStage 和 Timeline facts 的语义 MUST 保持不变
- **AND** 当前 change MUST NOT 预先增加 fake Fantasy 后端选项

### Requirement: Runtime Debug 必须按 ActionInstance 展示完整链路

系统 MUST 能按 ActionInstance 或本地输入序列追踪一次动作从输入到输出的完整链路。Debug 至少 MUST 展示 input sequence、action request、activation、lifecycle、Timeline window、motion source、motion resolve、cue、gameplay result、policy decision、network packet 和 correction。

#### Scenario: 查看一次本地攻击

- **WHEN** 开发者查看本地预测攻击 debug
- **THEN** debug MUST 能从 input sequence 追到 ActionInstanceId
- **AND** debug MUST 展示该 ActionInstance 的 activation、Timeline outputs、motion outputs、window samples、cue events 和 sync policy decisions
- **AND** 若发生 correction，debug MUST 展示 correction 来源、应用方式和 acknowledgement

#### Scenario: 查看服务端拒绝

- **WHEN** LocalLoopback 或后续服务端拒绝一个 action instance
- **THEN** debug MUST 展示本地预测事实和服务端拒绝事实的差异
- **AND** pipeline MUST 通过 action lifecycle 或 correction 处理该拒绝
- **AND** 系统 MUST NOT 靠隐藏状态重置掩盖拒绝原因

### Requirement: 2v2vE demo 第一阶段只实现最小业务压力事实

系统 MUST 将 2v2vE demo 第一阶段限制为角色动作压力闭环和最小目标/结果事件。最小事实包括 actor identity、team identity、action facts、window facts、gameplay result facts 和 objective event facts。系统 MUST NOT 在本阶段实现完整匹配、账号、背包、大地图、多职业、完整怪物 AI 或完整反作弊。

#### Scenario: 队伍动作交互

- **WHEN** 两队 actor 在同一场景内进行攻击、防守、闪避或支援动作
- **THEN** 每个 actor MUST 使用正式 actor identity 和 team identity
- **AND** 交互 MUST 通过 action/window/result facts 表达
- **AND** 客户端 MUST 能展示本地预测、远端确认和结果修正的差异

#### Scenario: 最小目标点事件

- **WHEN** demo 中存在目标点争夺或 PvE 目标事件
- **THEN** objective 变化 MUST 作为 server event replication 事实进入 GameplaySyncRuntime
- **AND** 角色 pipeline MAY 消费该结果用于表现或状态
- **AND** 系统 MUST NOT 因目标点事件新增完整 PvPvE 产品系统
