## Context
状态输出解析现在已经从 runner 中拆出，这是进步。但输出解析仍计算动作位移：根据 duration、distance、deltaTime 和 stateTime 算本帧距离，同时推导 action completed。这个计算属于 Action motion 语义，不属于状态图输出解析本身。

## Goals
- 状态输出解析只做状态配置到纯数据输出意图的转换。
- Action motion 计算集中在独立 resolver。
- Character frame pipeline 仍负责 phase 顺序；FullBodySubmissionBuilder 负责生成 Action motion submission，最终 motion executor 调用由 Character output applier 触发。
- Dodge 行为保持。
- 后续轻攻击、跳跃、受击可以新增 motion spec 或 resolver 分支，不改状态机输出核心。

## Non-Goals
- 不拆 motion executor。
- 不改变 locomotion motion resolver。
- 不把 action motion 计算放进 Animancer Presenter。
- 不新增物理碰撞或 hitbox 逻辑。

## Decisions
### Decision: State output emits spec, not command
状态输出层输出 `ActionMotionSpec` 或等价纯数据规格，包含 variant、distance、duration、rotateToDirection、setRunLatchOnComplete、locked direction、state time 和 source step。

### Decision: ActionMotionResolver owns command math
`ActionMotionResolver` 根据 spec、deltaTime、stateTime 和 timeline facts 计算本帧 `ActionMovementCommand`、has movement、completed、run latch 派生结果。

### Decision: Character frame pipeline owns ordering
Character frame pipeline 先推进请求仲裁和状态机，再让 FullBodySubmissionBuilder 调用 ActionMotionResolver 生成 Action motion result，最后由 Character output applier 提交 motion executor 和 runtime facts。

### Decision: Resolver is pure logic
ActionMotionResolver 不读取 Transform、CharacterController、Animator、Animancer 或 InputAction，不执行运动。

## Interface Shape
### ActionMotionSpec
- Owner: 状态输出解析。
- Contains: action state id、variant、distance、duration、rotateToDirection、setRunLatchOnComplete、locked world direction、state time、source step。
- Does not contain: `ActionMovementCommand`、Transform、CharacterController、Animator、Animancer state。

### ActionMotionResolveInput
- Owner: FullBodySubmissionBuilder。
- Contains: spec、deltaTime、current timeline facts、previous action facts 或必要 rollback facts。
- Does not contain: motion executor 或场景对象。

### ActionMotionResolveResult
- Owner: Action motion resolver。
- Contains: `ActionMovementCommand`、hasActionMovement、actionCompleted、setRunLatch、source step、diagnostic summary。
- Consumers: Character output applier、runtime blackboard、rollback snapshot comparison。

## Rejected Alternatives
### Alternative: 只把计算方法从 OutputResolver 抽成 private helper
拒绝原因：helper 仍属于 output resolver implementation，新增动作运动数学仍会回到同一模块，没有获得 Locality。

### Alternative: 让每个 Action module 自己计算 command
拒绝原因：当前项目没有独立 Action module 状态权威，拆到每个动作会重新制造分裂路径。第一版应集中到一个 resolver。

### Alternative: 让 motion executor 根据 spec 计算距离
拒绝原因：executor 是执行 adapter，应该消费命令而不是解释 gameplay duration/distance。否则执行端会知道动作语义。

## Test Surface
- `CharacterStateOutputResolver` 静态测试证明它只输出 spec。
- `ActionMotionResolver` 单元测试覆盖距离、完成、run latch。
- Character frame pipeline / FullBodySubmissionBuilder 测试覆盖 spec -> result -> executor 顺序。
- `CharacterRuntimeBlackboard` 测试覆盖 action facts 来源。
- `FullBodyRollbackReplayTests` 覆盖 resolver result 在 replay 中稳定。

## Risks / Trade-offs
- Risk: 多一个 resolver 让调用链变长。
  - Mitigation: resolver 承载真实 motion 数学和完成判断，删除它会让复杂度回流到 output resolver 和 pipeline。
- Risk: run latch 完成时机迁移引入回归。
  - Mitigation: 先加 Directional/Backstep characterization 测试。
- Risk: spec 与 command 字段重复。
  - Mitigation: spec 表示配置和状态时间，command 表示本帧执行结果。

## Migration Plan
1. 加 characterization 测试锁定 Dodge Directional/Backstep 输出。
2. 定义 action motion spec。
3. 修改 output resolver 输出 spec。
4. 新增 ActionMotionResolver。
5. FullBodySubmissionBuilder 调用 resolver。
6. blackboard action facts 改读 resolver result。
7. 删除 output resolver 中帧距离计算。
8. 保持 motion executor 入口不变。
