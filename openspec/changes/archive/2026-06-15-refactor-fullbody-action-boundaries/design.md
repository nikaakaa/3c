## Context
当前链路是 `CharacterFramePipeline -> FullBodySubmissionBuilder -> CharacterActionRequestSubmissionArbiter -> CharacterStateMachineRunner -> state output -> ActionMotionResolver/Locomotion motion -> output applier`。

上一轮重构已经完成了三件事的主体：
- current/projected/target timeline facts 有了帧内语义。
- transition condition 已迁到 evaluator collection。
- state output 已不直接构造 `ActionMovementCommand`。

但代码仍有四个残留分裂点：
- 旧 request gate 手写 TurnBack 与 Dodge 候选；当前 `CharacterActionRequestSubmissionArbiter` 必须只收集 request submission provider 产物。
- `ActionMotionResolver` 读取 `DodgeActionConfig` 并判断 `ActionStateIds.Dodge`。
- `CharacterStateMachineRunner` 保存 `actionWorldDirection`、`turnBackWorldDirection`、`turnBackEntryBasisForward`。
- `CharacterStateMachineSnapshot` 直接派生 `Owner`、`ActionState`、`LocomotionPhase`。

## Goals
- 新增 Attack/Jump 请求时不需要修改 request submission arbiter 主流程。
- 新增动作运动类型时不需要修改 Action motion resolver 的业务分支。
- runner 不再保存 TurnBack/Dodge 等具体状态 payload。
- snapshot 只表达状态机身份，FullBody 解释由外围 view/adapter 负责。
- 现有 Dodge、TurnBack、WASD 行为保持。

## Non-Goals
- 不实现新动作。
- 不迁移到 UnityHFSM。
- 不改变 motion executor 接口。
- 不改变现有配置数值。
- 不把 lifecycle 模块化纳入第一批。

## Decisions
### Decision: gate 只编排请求候选
`CharacterActionRequestSubmissionArbiter` MUST 从一组 request submission provider 收集候选请求。每个 provider 负责读取自己的输入 facts 和配置，产出 0..N 个候选。arbiter 不再手写 TurnBack/Dodge 分支。

### Decision: 仲裁仍是唯一准入裁决
request candidate builder MAY 构造候选请求，但 priority、resistance、force、policy、timeline window 裁决仍由 `ActionInterruptArbiter` 或等价仲裁入口完成。状态机只消费 accepted request fact。

### Decision: motion resolver 消费完整 motion spec
`ActionMotionResolver` MUST 只消费状态机 frame 提供的 motion spec、state time、delta time 和 timeline facts。Dodge 的 duration、distance、rotate 等具体数值必须在 spec 构建前由配置解析完成，或以通用 motion profile 数据进入 spec。

### Decision: runner payload 是通用可恢复状态 payload
runner 可以保存 active state、state time、variant、pending transition 和通用 state payload，但不能以字段形式知道 TurnBack/Dodge/Attack 等具体状态。TurnBack 锁定方向和 Action 锁定方向应来自 state payload 或状态输出数据。

### Decision: snapshot 与 FullBody view 分离
`CharacterStateMachineSnapshot` 只保留 active state、active path、state time、variant、pending transition、tags 等纯状态机事实。`Owner`、`ActionState`、`LocomotionPhase` 迁到 `FullBodyStateView` 或等价外围 adapter。

## Sequencing
1. 先完成现有三条架构 change 的验证，确认 current facts、condition trace、action motion result 稳定。
2. 先改 request candidate seam，因为它最直接阻塞 Attack/Jump 请求接入。
3. 再改 action motion spec/resolver seam，移除 Dodge config 反向读取。
4. 再改 runner state payload，避免 motion/request 改动同时触碰 runner 过深。
5. 最后改 snapshot/view，让外围调用迁到新解释入口。

## Risks / Trade-offs
- Risk: 一次改动覆盖 request、motion、runner、snapshot，影响面较大。
  - Mitigation: tasks 按阶段拆分，每阶段都有静态测试和行为回归。
- Risk: request candidate seam 只有 TurnBack/Dodge 两个 adapter，抽象可能过早。
  - Mitigation: seam 只表达候选收集，不引入脚本回调或场景对象引用。
- Risk: snapshot view 迁移会触碰大量调用点。
  - Mitigation: 先加 view，再迁调用点，最后删除 snapshot 派生解释。

## Stop Conditions
- 如果需要保留旧 gate 分支和新 candidate 分支同时作为正式路径，停止。
- 如果 `ActionMotionResolver` 仍需要读取 `DodgeActionConfig` 才能保持行为，停止并先修正 motion spec 形状。
- 如果 runner 仍需要 `CharacterStateIds.TurnBack` 特判才能恢复行为，停止并先设计 state payload。
- 如果 snapshot view 迁移导致 FullBody 与 Locomotion 各自维护 owner 解释，停止并统一 view。
