## Context

当前实现存在六个直接混杂点：

1. `ActionCorrectionPolicy.SmoothCorrection/ForceCorrection` 是空间位姿处理方式，`CancelOnReject` 却是动作生命周期结果。
2. 同一字段被序列化到 Action、Action motion 和通用 Behavior 三个层级，但 MotionStage 不读取任何一处。
3. `BehaviorNetworkPolicyResolver` 使用 correction policy 判断 Ack 是否发送，把“如何应用纠正”和“是否发送网络事实”绑在一起。
4. incoming Correction 与 outgoing Ack 复用同一个运行时对象和 packet payload，Ack 还重复携带 position/rotation。
5. `ActionMotionSample.CorrectionId` 与 `GameplayActionMotionDigest.CorrectionId` 没有正式生产者，当前始终为零。
6. PresentationInterpolator 从 `MotionDebug.Correction.Mode` 读取运行决策，使 debug 数据反向成为表现逻辑输入。

项目现行方向已经区分 ActionSyncDomain、MotionSyncDomain 与 PresentationFrame，但上述数据模型仍沿用早期“一个 correction 字段包办所有层”的口径。

## Goals / Non-Goals

### Goals

- 删除混合的 Action correction authoring，并让旧字段在代码和资产中完全消失。
- 让动作 decision、逻辑位姿 correction、表现采样和 correction acknowledgement 使用独立合同。
- 保持 MotionStage 当前数值行为，但不把当前直接纠偏算法升级成正式作者配置。
- 让 resolver、Inspector、runtime debug 与实际运行链路使用同一语义。
- 保持一条正式收发路径，不保留旧 payload、旧字段、兼容 parser 或双写。

### Non-Goals

- 不实现 owner input history、restore/replay 或 deterministic rollback。
- 不实现 RemoteProxy snapshot buffer/interpolation/extrapolation。
- 不实现新的 VisualRoot correction offset decay 算法。
- 不实现 Fantasy peer、服务端 simulation 或服务端 action/window/result 裁决。
- 不新增“Direct/Replay”“Owner/Remote”等尚未闭环的编辑器模式选项。
- 不新增当前 partial/full 纠偏算法的 PipelineDefinition 参数或 Inspector tuning。

## Decisions

### 1. 删除 ActionCorrectionPolicy，不用多个新枚举替换

ActionProfile 保留动作事务真正拥有的 `PredictionPolicy`、`AuthorityPolicy`、`ReplicationPolicy` 和各输出策略。ActionRuntime 继续通过 lifecycle transition 处理 Confirm、Reject、Correct、Cancel、Interrupt、Abort。

- `Reject` 是 ActionInstance 的 terminal 状态，不是作者可选策略。
- `Correct` 只表示服务端修正了动作事务状态，默认 non-terminal；它不描述角色位置如何移动，也不描述 VisualRoot 如何平滑。
- `ActionContext` 只携带动作实例身份和正式 profile 语义，不复制空间校正方式。

不选择“把旧枚举拆成 ActionRejectPolicy、ActionMotionCorrectionPolicy、BehaviorCorrectionPolicy”方案，因为当前业务没有三套独立可调需求。拆成更多枚举仍会允许无效组合，也会继续让 profile 声称控制它实际不执行的行为。

### 2. Action motion digest 只表达动作来源的运动事实

`ActionMotionPolicy` 保留 `SourceType + PredictionPolicy`。客户端 outgoing resolver 只有在 source 为 `LocalPredicted`，且 ActionProfile 不是 LocalOnly、Replication 不是 None 时，才允许生成 ActionMotionDigest。`ServerConfirmed` motion 对本地客户端是等待服务端结果，不应因为 correction 字段非空而主动发送。

`ActionMotionSourceType.Correction` 被删除。网络位姿 correction 是 actor 级 MotionSyncDomain 输入，不属于某个 ActionInstance 的 motion source。`ActionMotionSample` 与 `GameplayActionMotionDigest` 中无生产者的 `CorrectionId` 同时删除；Action lifecycle transition 上用于关联服务端 Correct decision 的 `CorrectionId` 保留。

### 3. 当前逻辑位姿算法保持内部实现，不建立作者配置

本 change 保持 CharacterMotionStage 现有数值行为：小误差在本 tick 部分应用，大误差在本 tick 完整应用。这个算法只作为当前 MotionStage 的单一路径存在，不再由 ActionProfile、ActionMotionPolicy 或 GameplayBehaviorProfile 假装配置，也不迁入 CharacterPipelineDefinition。

本 change 不新增 `CharacterMotionCorrectionDefinition`、partial fraction、单 tick clamp 或 snap threshold 的 authoring 字段。现有数值是当前算法实现的一部分，不是 fallback：runtime 不会先查 Profile、缺失后再退回常量，而是只有这一条内部执行路径。

不把当前算法配置化的原因是，项目目标中的 owner reconciliation 最终需要权威状态恢复、未确认 input replay 和独立 VisualRoot recovery。当前“每 tick 消化部分误差”并不是该模型的参数化版本；把它公开为正式配置会制造下一次重构必须删除的作者合同。

后续网络 change 必须在确定 Owner、RemoteProxy 等角色和历史合同后，再决定真正需要暴露的 history 长度、最大 replay tick、hard reset 条件和 visual recovery 参数。

### 4. MotionCorrectionApplicationResult 是正式运行结果

CharacterMotionStage 在处理 correction 后写入正式 `MotionCorrectionApplicationResult`。该结果至少包含是否应用、application extent、input sequence、server tick、校正前位置、目标位置、实际 delta 和 yaw delta。`ApplicationExtent` 只客观描述本 tick 未应用、部分应用或完整应用，不是作者策略。

- Motion debug 从该结果生成展示记录。
- Presentation logic sample 从该结果读取本 tick 的实际 application extent。
- Presentation 不读取 `MotionResolveDebugFrame`，debug 开关也不能改变显示行为。

为保持当前画面行为，Presentation 对完整应用继续使用 `alpha = 1`，对部分应用继续使用普通 logic sample interpolation。VisualRoot 的长期误差衰减是后续独立能力，本 change 不将其伪装成已经完成。

### 5. Correction 与 Acknowledgement 使用独立合同

正式链路为：

```text
GameplayMotionCorrection packet
  -> CharacterGameplaySyncAdapter
  -> CharacterNetworkReceiveStage
  -> Correction
  -> CharacterMotionStage
  -> MotionCorrectionApplicationResult
  -> MotionCorrectionAcknowledgement SyncFact
  -> CharacterNetworkSendStage
  -> CharacterGameplaySyncAdapter
  -> GameplayMotionCorrectionAcknowledgement packet
```

Ack 只携带确认所需的 `InputSequence` 与 `ServerTick`，不回显 position/rotation。只有 MotionStage 确实应用了 correction 才生成 Ack；没有 CharacterController 或 correction 未应用时不得发送成功 Ack。

`SyncFactBehaviorBinding.MotionCorrectionAck` 继续提供显式 BehaviorId。Resolver 只检查该 profile 是否为 Motion Stream、是否非 LocalOnly 且 Replication 非 None；它不读取 correction application extent。这样“事实是否存在”“事实能否发送”“逻辑如何纠正”是三个独立判断。

### 6. 直接迁移正式资产，不保留旧序列化入口

Corin ActionProfile、ActionMotionPolicy 和 GameplayBehaviorProfile 删除旧 `m_CorrectionPolicy` YAML。Corin PipelineDefinition 不增加 correction 算法配置。代码删除字段后不使用 `FormerlySerializedAs`、legacy enum、一次性 runtime migrator 或 Inspector 兼容读取。

## Tradeoffs

### 保持现有 partial/full 数值行为

优点是本次只清理真实所有权，不把网络算法重写和数据模型迁移混在一起，用户现有本地测试手感不会被无关改变。代价是它仍不是带 input replay 的完整 owner reconciliation；后续实现 replay 时会替换逻辑应用算法，而不是复用一个冒充 replay 的配置选项。

### 不提供当前算法的 authoring tuning

优点是不会把即将被 owner restore/replay 取代的 direct correction 参数固化为资产合同，也不会出现 Action、Behavior、Pipeline 三处可调。代价是当前 partial fraction 和 full threshold 仍是 MotionStage 内部实现，作者要等后续正式 reconciliation change 才能调真正有业务意义的参数。

### Ack 保留独立 BehaviorProfile binding

优点是系统事实仍遵守现有 BehaviorId/SyncFact policy 主链，网络过滤和 debug 可追踪。代价是作者仍会看到一个 Ack behavior profile；它只配置网络可见性，不再假装配置 correction 算法。

## Risks / Migration

- Unity 旧 YAML 字段必须与代码删除在同一 change 内完成，否则 Inspector 会留下不可见旧数据。
- packet 构造函数和 debug 读取点会因新增独立 Ack payload 发生编译断裂，必须串行迁移所有调用点后再编译。
- 旧 `MotionCorrectionApplyMode` 不能继续作为 Presentation 输入；必须通过正式 application result 一次收口，不能同时保留 debug 输入路径。
- 当前 specs 明确要求 ActionProfile 和 Stream behavior 保存 correction policy，与目标设计冲突；本 change 的 delta 会逐项修改这些要求。
