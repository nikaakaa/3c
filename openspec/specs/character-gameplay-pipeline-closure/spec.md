# character-gameplay-pipeline-closure Specification

## Purpose
定义角色 Gameplay 管线闭环：输入、动作请求、Graph/StateMachine 决策、ActionRuntime、Timeline facts、MotionStage、PresentationStage、SyncFacts、GameplaySync 和 Runtime Debug 必须走同一条正式 `CharacterPipeline` 主线，不恢复旧 SO/config、旧播放器、旧网络输出或 demo 临时桥接。
## Requirements
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

系统 MUST让 Graph、StateMachine 和 Timeline 输出 gameplay facts，而不是直接执行最终 Transform、命中裁决、扣血或网络发送。Timeline 时间范围的 Window 作者输入 MUST统一为 Decision TreeClip 写入 scope variable；显式 Blackboard fact projection MUST在统一 phase 将合法写入转换为 `ActionWindowSample`。正式 gameplay facts 至少包括 `ActionActivationRequest`、`ActionLifecycleTransition`、`ActionWindowSample`、`ActionMotionSample`、`GameplayCueFact`、`GameplayResultEvent`、`GameplayEffectLifecycleFact`、`GameplayAttributeValueFact`、`MotionContribution` 与 `MotionWarpWindow`。动画选择与表现采样 MUST通过独立 AnimationLayerSelection/AnimationProducerSample 合同提交，不得伪装成 gameplay fact。系统 MUST不保留 ActionWindowTrack/Clip 或 SubmitActionWindowSampleNode 作为并行事实生产路径。

#### Scenario: Timeline 输出攻击窗口

- **WHEN** 动作 Timeline 的 Hit、IFrame、Parry 或 Cancel Decision TreeClip 在当前 Tick 写入 projected variable=true
- **THEN** 统一 projection MUST输出 `ActionWindowSample`
- **AND** sample MUST关联写入 provenance 中 Action Context 的 ActionInstanceId
- **AND** Timeline MUST不直接判定命中、扣血或发送网络包

#### Scenario: Timeline 输出动作位移

- **WHEN** 动作 Timeline 采样到 root motion 或 motion warp window
- **THEN** Timeline MUST输出 `MotionContribution` 或 `MotionWarpWindow`
- **AND** 最终位移 MUST由 `CharacterMotionStage` 结算
- **AND** Window 作者路径重构 MUST不改变 Motion 轨道权威

#### Scenario: 本地状态时间门

- **WHEN** Decision TreeClip 写入 Projection=None 的 Bool Frame variable
- **THEN** Graph 与 StateMachine MAY将其作为本地条件读取
- **AND** Pipeline MUST不生成 ActionWindowSample 或 outgoing packet

### Requirement: Motion 闭环必须依赖正式仲裁而不是直接移动

系统 MUST 将本闭环的所有 motion 来源接入正式 motion 仲裁链。最终顺序 MUST 是 contribution resolve、motion modifier、network correction phase、Move、MotionResult。所有 gameplay motion 来源 MUST 能被 debug 追踪。

#### Scenario: 输入和攻击 root motion 同帧出现

- **WHEN** 同一逻辑 tick 内存在输入 locomotion 和攻击 root motion
- **THEN** 输入 MUST 作为 locomotion contribution 进入 resolver
- **AND** 攻击 root motion MUST 作为 action contribution 进入 resolver
- **AND** MotionStage MUST 根据正式仲裁规则输出一个 `MotionIntent`

#### Scenario: 网络校正到达

- **WHEN** NetworkReceiveStage 收到 motion correction
- **THEN** correction MUST 在 MotionStage 的 correction phase 处理
- **AND** MotionStage MUST 输出正式 application result，并只在成功应用后通过 `SyncFacts.Motion.CorrectionAcknowledgements` 输出独立 acknowledgement
- **AND** 系统 MUST NOT 在 resolver 前直接硬设 Transform 作为正式路径

### Requirement: Presentation 闭环必须只消费表现事实

动画表现链路 MUST消费每层唯一 AnimationLayerSelection、匹配 generation 的 AnimationProducerSample、Complete 与 Release。PresentationStage MUST原子提交 AnimationPlaybackLifecycle，并由 AnimancerPlaybackAdapter 使用 presentation delta 应用正式 TransitionLibrary、state、mixer 与 fade；它 MUST不消费 PresentationSync cue，不推进 Timeline logic、执行 Action 决策、读取 transport 或产生 gameplay 裁决。Presentation MUST不读取 Tree priority、Tree lifecycle 或 StateMachine edge 来二次选择动画赢家。

#### Scenario: Timeline producer sample

- **WHEN** selected Timeline AnimationTrack 在表现帧产生合法 sample
- **THEN** command queue MUST保存对应 playback generation 的 AnimationProducerSample
- **AND** lifecycle MUST在首个 sample 后将 PendingFirstSample 原子切换为 Current
- **AND** AnimancerPlaybackAdapter MUST应用该 producer 的正式 transition

#### Scenario: 逻辑所有权变化

- **WHEN** State/Action 逻辑为 Base 提交新的唯一 AnimationLayerSelection
- **THEN** Presentation MUST等待目标首个合法 sample
- **AND** MUST不从 Tree edge、Priority 或历史 sample 推断另一个目标

#### Scenario: Presentation Cue

- **WHEN** Timeline/Graph 输出 VFX、SFX、camera cue 或 hit stop
- **THEN** cue MUST进入正式 presentation output
- **AND** cue MUST不绕过 SyncFacts 伪装成网络事件

#### Scenario: AllowEmpty

- **WHEN** AllowEmpty layer 收到正式 Empty selection
- **THEN** Animancer MAY淡出该 layer 到空
- **AND** PresentationStage MUST不创建隐藏 producer

### Requirement: SyncFacts 必须成为 demo 同步和 debug 的唯一事实出口

系统 MUST 使用 `SyncFacts` 作为本 tick 已发生 gameplay facts 的唯一模型外输出。Character fact stage MUST 收集 facts；model-owned adapter/resolver MUST 按当前 ModelId 解析、记录并构造 model packets。CharacterPipeline、Graph 和 Timeline MUST 不引用 ServerAuthoritative runtime、packet、endpoint 或 policy，也 MUST 不恢复旧 NetworkOutput。

#### Scenario: ActionWindow 进入当前模型

- **WHEN** Timeline projection 产生 ActionWindow fact
- **THEN** fact MUST 先进入 SyncFacts
- **AND** ServerAuthoritative adapter MUST 从 model profile 解析 packet/history policy

### Requirement: 第一阶段网络后端只覆盖 None 和 LocalLoopback

第一阶段唯一完整 Network Model MUST 是 `ServerAuthoritativeHybrid`。未引用 EndpointDefinition MUST 表达明确断开；当前唯一可创建的 endpoint definition MUST 是 LocalLoopback。断开/LocalLoopback MUST 不再称为两个 Network Model，且系统 MUST 不显示未实现的 Fantasy 或 Rollback。

#### Scenario: Sandbox 使用 LocalLoopback

- **WHEN** SessionHost model 是 ServerAuthoritativeHybrid 且 endpoint 是 LocalLoopback
- **THEN** Character gameplay MUST 通过 model-owned adapter 和 endpoint 闭环
- **AND** MUST 不存在 per-character backend ownership

### Requirement: Runtime Debug 必须按 ActionInstance 展示完整链路

系统 MUST 能按 ActionInstance 或本地输入序列追踪一次动作从输入到输出的完整链路。Debug 至少 MUST 展示 input sequence、action request、activation、lifecycle、Timeline window、motion source、motion resolve、cue、gameplay result、policy decision、network packet 和 correction。

#### Scenario: 查看一次本地攻击

- **WHEN** 开发者查看本地预测攻击 debug
- **THEN** debug MUST 能从 input sequence 追到 ActionInstanceId
- **AND** debug MUST 展示该 ActionInstance 的 activation、Timeline outputs、motion outputs、window samples、cue events 和 sync policy decisions
- **AND** 若发生 correction，debug MUST 展示 correction 来源、application extent、实际 delta 和 acknowledgement

#### Scenario: 查看服务端拒绝

- **WHEN** LocalLoopback 或后续服务端拒绝一个 action instance
- **THEN** debug MUST 展示本地预测事实和服务端拒绝事实的差异
- **AND** pipeline MUST 通过 action lifecycle 或 correction 处理该拒绝
- **AND** 系统 MUST NOT 靠隐藏状态重置掩盖拒绝原因

### Requirement: 2v2vE demo 第一阶段只实现最小业务压力事实

第一阶段 MUST 继续只实现输入、动作事务、motion、window、result、Gameplay Effect 生命周期、Attribute、cue 和本地 ServerAuthoritative Loopback 压力事实。本 change 只隔离模型边界，不实现真实双客户端、PvE、Objective、完整 Rollback、命中伤害裁决或 Fantasy server slice。

#### Scenario: 查看当前网络能力

- **WHEN** 作者查看 Runtime Debug
- **THEN** MUST 能识别当前 ServerAuthoritativeHybrid + LocalLoopback
- **AND** MUST 不宣称已经实现 RemoteProxy 或真实服务端权威
