## Context

当前项目已经有本地预测回滚地基，但还没有真实网络输入权威合同。本地 `PredictionInputFrame` 是 replay 输入事实，`LocalLatencyReconciliationRunner` 能用 delayed remote input 模拟预测错误并执行回滚。

接入 Fantasy 之前，必须把本地输入事实提升为网络可传输的稳定合同。这个合同不能直接等同于 Unity 输入事件，也不能直接等同于角色运行结果。

## Goals

- 统一帧同步输入字段。
- 统一 Action request 输入边界。
- 统一服务端 confirmed input set。
- 统一版本握手。
- 保证后续 transport、Fantasy、prediction buffer、rollback reconciliation 使用同一套输入口径。

## Non-Goals

- 不实现真实网络。
- 不实现 Fantasy Handler。
- 不实现服务端角色模拟。
- 不实现 rollback apply。
- 不处理视觉相机同步。

## Decisions

### Decision: 输入同步以 `SimulationTick` 为唯一时间主序

原因：

- 当前 tick system 已经定义 `SimulationTick`。
- rollback/snapshot/input history 都围绕 tick 对齐。
- Fantasy 或 fake transport 只需要传整数 tick，不应传浮点时间。

### Decision: 相机不进入网络同步

原因：

- 真实 camera 是 presentation/control local-only。
- 相机 shake、FreeLook、Cinemachine blending 都不应该影响多端 gameplay 一致性。
- 需要 replay 的 camera-relative 解算由纯数据 camera basis 或 gameplay intent 承担。

### Decision: Action request 只保存输入事实

原因：

- 动作能否进入取决于 Action domain runtime、body claim、slot arbitration、interrupt policy。
- 如果输入历史保存动作结果，replay 就不再验证 gameplay decision。
- 当前 specs 已经要求 Dodge/Attack/Jump/Interact 重新经过输入缓冲和仲裁。

### Decision: 服务端确认输入，不确认角色状态

原因：

- 当前目标是帧同步/预测回滚，不是传统状态同步。
- 服务端第一阶段没有必要也不应该运行角色控制器。
- 确认输入集合已经足够让客户端回滚重放。

### Decision: version handshake 前置于 gameplay input

原因：

- 配置不同会导致同输入不同结果。
- 如果不先阻断，后续 checksum mismatch 会变成难以定位的伪网络问题。
- 缺失配置 hash 不能 fallback。

## Data Boundaries

### Network Allowed

- tick
- player id
- unit id
- local input sequence
- move intent
- look/aim intent
- run held
- button facts
- action request facts
- target stable id
- protocol/config/version hash

### Network Forbidden

- GameObject
- Transform
- Animator
- AnimationClip
- Animancer state
- InputAction
- Cinemachine camera
- Main Camera transform
- action accepted result
- active action state
- body slot result
- motion executor state

## Risks

- 风险：字段设计太接近当前本地 `PredictionInputFrame`，未来多人单位不够用。
  - 缓解：加入 PlayerId、UnitId、LocalInputSequence。
- 风险：Action request 过早绑定具体业务。
  - 缓解：stable action id 来自 action catalog，payload 只表达 request facts。
- 风险：camera basis 被误解为同步真实相机。
  - 缓解：spec 明确它只是 replay 需要的纯数据事实，真实相机仍 local-only。
- 风险：config hash 生成不稳定。
  - 缓解：后续任务要求稳定排序和缺失 hash 失败测试。

## Migration Plan

1. 先添加纯数据合同与测试。
2. 再添加 converters。
3. 再让 prediction buffer 使用该合同。
4. 最后让 Fantasy adapter 映射到同一合同。

## Open Questions

- `TargetIntent` 第一版只需要 stable target id，还是需要 aim direction？
- Action request payload 第一版覆盖 Dodge/Attack 即可，还是同时预留 Jump/Interact？
- Config hash 第一版是否从已有 SO 直接计算，还是先用 manifest 描述？
