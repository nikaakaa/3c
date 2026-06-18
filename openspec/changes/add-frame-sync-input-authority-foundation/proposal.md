# Change: 帧同步输入权威地基

## Why

当前项目已经有本地 `PredictionInputFrame`、输入历史、snapshot history、local latency reconciliation 和 Character frame rollback/replay 地基，但它们仍然偏向本地调试语义。要接入 Fantasy 或等价 transport，必须先把“网络上到底传什么输入事实、服务端确认什么、客户端拿什么回滚”定义成稳定合同。

这个 change 的目标不是实现网络，也不是实现服务端角色模拟，而是把帧同步的输入权威地基一次性说清楚。它把原来拆散的输入帧合同、Action request 合同、confirmed input set、config/version handshake 和路线边界合并成一个大 proposal，因为它们本质上都在回答同一个问题：**哪些事实可以成为多人同步的输入权威，哪些事实必须留在本地或 replay 后重新推导。**

如果这一层不统一，后续 Fantasy adapter、prediction buffer、rollback reconciliation 和 checksum 都会各说各话，最后形成多个输入格式、多个确认口径、多个回滚入口。那会直接违背当前项目“不新增分裂路径”的约束。

## What Changes

- 新增帧同步输入权威地基能力。
- 定义 `FrameSyncInputFrame` 的概念字段，而不是直接开始写运行时代码。
- 定义 player、unit、tick、move intent、look/aim intent、button facts、action request facts 和 target intent 的同步边界。
- 明确相机不同步，网络只同步相机折算后的 gameplay intent 或已进入 snapshot 的 camera basis 派生事实。
- 明确 Action request 只同步输入事实，不同步动作接受结果、动作状态或动画播放结果。
- 定义服务端确认输入集合 `ConfirmedInputSet` 的语义：一个 tick 的多玩家输入集合、排序、缺帧、重复输入、late input 和 confirmed tick。
- 定义进入帧同步前的 config/protocol/version handshake：协议版本、action catalog、locomotion config、state machine config、motion profile、input mapping、checksum schema。
- 明确服务端第一阶段只做 tick/input authority，不做角色控制器、不做玩法状态权威、不生成 Transform correction。
- 明确后续串行实施包必须以当前 `SimulationTick`、`PredictionInputFrame`、`CharacterFrameInput`、`InputRequestBuffer` 和 `CharacterFramePipeline` 为对齐目标。

## Impact

- Affected specs: frame-sync-input-authority-foundation
- Related specs:
  - simulation-tick-system
  - local-rollback-synctest-foundation
  - local-latency-reconciliation
  - character-frame-rollback-replay
  - prediction-rollback-authority-scopes
  - character-action-catalog
  - action-domain-runtime
  - character-config-root
  - cinemachine-third-person-camera
- Affected code later:
  - `Assets/Scripts/Simulation/Rollback`
  - future `Assets/Scripts/Simulation/FrameSync`
  - future input DTO converter
  - future confirmed input resolver
  - future config hash manifest

## Implementation Package

这个 change 是四个正式实施规划包中的第一个，必须先做。它完成后，第二、第三、第四实施包才能安全地落 transport、Fantasy、prediction buffer 和 rollback closed loop。

它不应该拆成十几个小 proposal，因为这些小块彼此无法独立验收。比如：

- 没有输入帧字段，就无法定义 confirmed input set。
- 没有 action request 字段，就无法证明 Dodge/Attack 只同步输入事实。
- 没有 confirmed input set，就无法定义 prediction buffer 的替换边界。
- 没有 config handshake，就无法解释为什么 checksum mismatch 不是版本不一致导致。

所以本 proposal 的正确粒度是“输入权威地基”，内部用非常细的 tasks 串行完成。

## Scope Layer

本 change 触碰以下层：

- Input Contract：定义网络输入事实。
- Action Request Contract：定义动作请求的输入事实边界。
- Server Input Authority Contract：定义服务端确认输入集合。
- Session Admission Contract：定义版本握手。
- Rollback Boundary Contract：定义如何转换到现有 replay 输入。

本 change 不触碰以下层：

- Transport implementation。
- Fantasy handler。
- Rollback apply。
- Motion executor。
- Animancer presenter。
- Cinemachine camera runtime。
- 服务端角色模拟。

## Sync Field Model

`FrameSyncInputFrame` 必须以纯数据表达输入事实。建议概念字段如下：

- `SimulationTick Tick`
- `FrameSyncPlayerId PlayerId`
- `FrameSyncUnitId UnitId`
- `uint LocalInputSequence`
- `Vector2 MoveIntent`
- `Vector2 LookIntent`
- `bool RunHeld`
- `FrameSyncButtonFact Dodge`
- `FrameSyncButtonFact Attack`
- `FrameSyncButtonFact Jump`
- `FrameSyncButtonFact Interact`
- `FrameSyncActionRequestFact[] ActionRequests`
- `FrameSyncTargetIntent TargetIntent`
- `RollbackCameraBasisState CameraBasisState` 或等价派生事实，只有在 replay 需要时进入 simulation snapshot / input mapping，不代表同步真实相机。

字段必须保持稳定排序，后续 transport/proto/checksum 才能稳定。

## Camera Boundary

相机不同步。

这不是因为相机不重要，而是因为相机在本项目中属于 local-only presentation/control aid。第三人称相机、Cinemachine、FreeLook 轴、Main Camera transform、screen effect、camera shake 都不应该成为网络权威。

网络侧真正需要的是“玩家想往哪里走、往哪里看、朝哪个目标发起请求”。这些可以在客户端输入采集阶段折算成 gameplay intent：

- WASD 可以折算成 camera-relative 后的 move intent。
- 瞄准可以折算成 target intent、aim direction 或 gameplay look intent。
- replay 如果要重建 camera-relative 解算，只使用 `RollbackCameraBasisState` 这种纯数据事实，不恢复真实相机。

这与当前 `cinemachine-third-person-camera` 和 `character-frame-rollback-replay` 的 current spec 一致。

## Action Request Boundary

Action request 只同步输入事实。

网络输入可以表达：

- 这一 tick 是否按下 Dodge。
- Dodge 是否 held。
- Dodge 是否 released。
- Attack request 的 stable action id。
- request sequence。
- target id。
- aim intent。

网络输入不能表达：

- Dodge 已经被接受。
- Attack 已进入 active state。
- Action lifecycle state time。
- cancel window 是否打开。
- hit window 是否已经命中。
- Animancer clip 播放到哪里。

这些结果必须在 replay 时重新经过：

- `InputRequestBuffer`
- `CharacterActionRequestSubmissionArbiter`
- Action domain runtime
- body claim / slot arbitration
- `CharacterFramePipeline`

输入历史继续保存输入事实，不保存“动作结果”。这与 `character-frame-rollback-replay` 中的输入回灌要求一致。

## Confirmed Input Set Model

`ConfirmedInputSet` 是服务端或 fake room 对某个 tick 的输入确认结果。

它不是状态快照。

它建议包含：

- `SimulationTick Tick`
- `uint ServerSequence`
- `uint ProtocolVersion`
- `uint ConfigHash`
- `ConfirmedPlayerInput[] Inputs`
- `ConfirmedInputDiagnostic[] Diagnostics`
- `SimulationTick ConfirmedTick`

其中 `ConfirmedPlayerInput` 必须按稳定键排序：

1. `PlayerId`
2. `UnitId`
3. `LocalInputSequence`

如果同一个 tick、player、unit 到达多份输入：

- 第一份合法输入进入 confirmed set。
- 后续重复输入进入 duplicate diagnostic。
- 不允许静默覆盖已确认输入。

如果 tick 已经小于 confirmed tick：

- 该输入进入 late diagnostic。
- 不允许回写已经裁剪的历史。

如果 expected player/unit 缺失：

- 进入 missing diagnostic。
- 不允许假装成合法空输入，除非后续有正式 input prediction policy 明确声明。

## Version Handshake Model

进入帧同步前必须先完成版本握手。

第一版建议至少比较：

- `ProtocolVersion`
- `ChecksumSchemaVersion`
- `ActionCatalogHash`
- `LocomotionConfigHash`
- `StateMachineConfigHash`
- `MotionProfileHash`
- `InputMappingVersion`
- `FrameSyncInputSchemaVersion`

不一致时：

- 拒绝进入同步。
- 输出差异类别。
- 不发送 gameplay input。
- 不进入 checksum/correction 流程。

不能用 fallback 配置继续运行。配置缺失就是握手失败。

## Non-Goals

- 不实现 Fantasy。
- 不修改 proto。
- 不实现 socket、KCP、WebSocket 或任何真实 transport。
- 不新增服务端角色控制器。
- 不同步相机。
- 不同步 Unity Object。
- 不同步 Animancer / Animator runtime。
- 不把 Action accepted result 写入输入历史。
- 不把 `FullBody` 当作 source、slot 或 graph owner。
- 不新增 fallback 配置。

## First Vertical Slice

第一条实施纵切应该是纯数据：

1. 定义 `FrameSyncInputFrame` 概念模型。
2. 定义 `FrameSyncButtonFact`。
3. 定义 `FrameSyncActionRequestFact`。
4. 定义 `ConfirmedInputSet` 概念模型。
5. 定义 version handshake manifest。
6. 写转换器测试计划：`PredictionInputFrame -> FrameSyncInputFrame -> PredictionInputFrame`。
7. 写 confirmed set 排序、duplicate、missing、late 测试计划。
8. 写 no camera sync / no Unity Object 静态边界测试计划。

这一纵切不需要 Fantasy，也不需要真实 rollback apply。

## Acceptance Criteria

- 输入合同能覆盖 Move、Look、Run、Dodge、Attack、Jump、Interact。
- Action request 保存输入事实，不保存动作结果。
- confirmed input set 的排序稳定。
- duplicate/missing/late/wrong tick 都有诊断。
- handshake 不一致时拒绝进入同步。
- 所有 DTO 都是纯数据。
- 不引入 Unity runtime 对象引用。
- 不引入 Fantasy 类型引用。
- 不改现有 Character gameplay 主线。

## Conflict Check Against Current Specs

### `simulation-tick-system`

本 proposal 与它一致：tick 是唯一主序，不用浮点时间表达网络输入。

### `local-rollback-synctest-foundation`

本 proposal 与它一致：输入历史只保存输入事实，不保存动作结果。

### `local-latency-reconciliation`

本 proposal 是它的真实网络输入前置：confirmed input set 后续会替代本地 delayed remote input，但 reconciliation 分类不变。

### `character-frame-rollback-replay`

本 proposal 与它一致：Action 请求 replay 时必须重新进入 `InputRequestBuffer` 和 `CharacterFramePipeline`。

### `prediction-rollback-authority-scopes`

本 proposal 与它一致：presentation drift 不成为 strict gameplay 输入。

### `cinemachine-third-person-camera`

本 proposal 与它一致：真实相机 local-only，网络只同步 gameplay intent。

## Review Notes

明天 review 这个 proposal 时，重点看三个问题：

1. 同步字段是不是太多，是否混入了表现层状态。
2. confirmed input set 是否足够表达服务端输入权威。
3. handshake 是否能阻止“配置不同但还继续同步”的错误。

如果这三个问题成立，后续 Fantasy 和 rollback 才不会被迫补救输入层设计错误。
