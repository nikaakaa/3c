# 自研分层角色状态机运行时设计

## Context
当前角色状态机已经是同一棵分层状态树：`FullBody` 是根，`Locomotion` 和 `Action` 是子域，`Idle / MoveStart / MoveLoop / MoveStop / TurnBack / Dodge` 是路径上的具体节点。这里的“统一”表示单一状态权威，“分层”表示状态路径和父子域表达方式，两者不是替代关系。

真正的摩擦在运行时边界：`CharacterStateMachineRunner` 现在既解释状态图，也采样 timeline，又生成 motion / animation / run latch / TurnBack 输出。调用方因此需要理解太多状态机内部细节，Action request resolver 还会反向调用 runner 采样 timeline，这让职责不够集中。

UnityHFSM 不是预测回滚绝对不能用，但它默认没有项目需要的一等 snapshot/restore、固定 tick 时间源约束和副作用隔离接口。当前选择继续自研，是为了把回滚、配置、状态路径和纯数据输出做成项目正式边界，而不是把这些约束塞进第三方库 adapter。

## Goals
- 明确角色主线继续使用项目自研分层状态图运行时。
- 让状态图运行时成为通用模块，不绑定 Dodge、TurnBack、Animancer、CharacterController 或 FullBody pipeline 细节。
- 用经典 `Enter / Tick / Exit` 生命周期接口组织状态节点运行时。
- 保持 `Tick(context) -> CharacterStateMachineFrame` 作为唯一对外推进结果。
- 让状态节点生命周期只产出纯数据，不执行 Unity 副作用。
- 让 runner 只维护状态推进所需的最小可恢复状态。
- 让 timeline、state output、motion command、animation request、diagnostics 成为 runner 外围明确模块。
- 让状态机节点只保存动画语义 key / timeline binding key，具体播放配置回到 Animancer 和动画配置资产。
- 保持 `PlayerFullBodyActionController` 作为唯一正式 runner owner。
- 保持 FullBody frame pipeline 作为一帧顺序权威。
- 保持输入、运动、动画、相机为状态机外围 adapter。
- 用测试证明职责拆分没有改变现有 Locomotion、TurnBack、Dodge 和 rollback 行为。

## Non-Goals
- 不把当前角色主线迁移到 UnityHFSM。
- 不引入第二套角色控制器、第二个 runner owner、第二条 motion executor 或第二套状态图配置。
- 不实现完整通用游戏状态机框架，不做并行层、编辑器图编辑器或 arbitrary callback 插件系统。
- 不让每个状态变成 MonoBehaviour。
- 不让状态节点直接调用 Animancer、Animator、CharacterController、InputAction 或 Transform。
- 不改变当前玩法语义：Idle、MoveStart、MoveLoop、MoveStop、TurnBack、Dodge 的状态路径和行为保持。
- 不删除日志。
- 不新增 fallback 配置。
- 不运行 Unity batchmode。

## Terms
- 统一分层状态机：同一棵状态树既提供唯一权威，又通过路径表达父子域。
- 状态图运行时：解释节点、transition、条件、active state、state time、variant、pending transition 和 snapshot/restore 的纯运行时。
- 状态输出解析：把 active state 和配置解析成 motion、animation、input consume、run latch、TurnBack policy 等纯数据输出的模块。
- Timeline facts：根据当前状态、播放进度和配置采样出的输入窗口、退出窗口、motion window 等纯数据事实。
- Adapter：输入、运动、动画、相机、Unity 生命周期和场景引用所在的外围模块。

## Proposed Runtime Shape
```text
CharacterStateGraphRuntime
  - 输入: CharacterStateGraphDefinition, CharacterStateGraphContext
  - 输出: CharacterStateGraphStepResult
  - 持有: active state, state time, variant, transition path, restore flags
  - 不持有: Animancer state, CharacterController, InputAction, Transform

CharacterStateTransitionEvaluator
  - 只读 context facts
  - 不调用 action arbiter
  - 不采样 timeline

CharacterStateTimelineFactSampler
  - 输入: active state snapshot, runtime animation facts, timeline policy
  - 输出: StateTimelineWindowFacts
  - 不切换状态

CharacterStateOutputResolver
  - 输入: active state snapshot, node output config, context facts
  - 输出: motion / animation / input consume / run latch / TurnBack 纯数据输出
  - 不执行运动、不播放动画

FullBodyFramePipeline
  - 编排输入、request gate、timeline facts、状态推进、输出解析、执行和表现提交
```

实施时命名可以等价，但职责必须一致。

## Classic Lifecycle Shape
经典接口是内部组织方式，不是新的外部执行管线。概念接口如下，实际命名可在实现阶段按代码库风格调整：

```text
ICharacterStateLifecycle
  - Enter(context, frameBuilder)
  - Tick(context, frameBuilder)
  - Exit(context, frameBuilder)
```

推进顺序：

```text
1. 从 context 和 runtime blackboard 准备 facts
2. 采样当前状态 timeline facts
3. 解析 transition 候选并选择最高优先级目标
4. 如发生 transition:
   - 调用旧 active state 的 Exit
   - 切换 active state、state time、variant、payload
   - 调用新 active state 的 Enter
5. 调用当前 active state 的 Tick
6. 产出单个 CharacterStateMachineFrame
7. FullBody pipeline 执行输入消费、运动、动画、黑板写入和诊断
```

生命周期方法只能向 frame builder 或等价纯数据输出写入结果。它们不得播放动画、移动角色、读取 Unity 输入对象或写场景对象。

## Animation Ownership
状态机节点允许保存：
- `AnimationKey` 或等价语义 ID。
- `TimelineBindingKey` 或等价播放事实匹配 ID。
- 变体到语义 key 的映射。

状态机节点不得长期保存：
- `AnimationClip`
- `TransitionAsset`
- `TransitionLibraryKey`
- fade duration
- playback speed
- start time
- Animancer runtime state

Locomotion 表现解析归属：
- `RunLocomotionAnimationConfigSO`
- `BasicLocomotionAnimancerPresenter`
- Animancer TransitionLibrary

Action 表现解析归属：
- `ActionAnimationProfileSO` 或等价动作动画配置入口
- `ActionAnimationAnimancerPresenter`
- Animancer TransitionLibrary

## Decisions
- Decision: 继续自研角色分层状态图运行时。
  - Reason: 当前预测回滚和配置边界需要可控 snapshot/restore、固定输入 facts 和副作用隔离；自研运行时更容易把这些约束做成正式接口。
- Decision: 不把 UnityHFSM 作为主线优先库。
  - Reason: UnityHFSM 默认状态和 pending transition 封装在内部对象中，默认 timer 使用 `Time.time`，状态 callback 容易承载副作用。裸用会和预测回滚、纯数据快照和状态输出分层冲突。
- Decision: runner 不再长期承担状态输出解析。
  - Reason: 状态推进和输出解释是两个不同职责。runner 的深度应该来自“可靠推进状态图和可恢复”，而不是知道所有动作、动画和运动输出细节。
- Decision: 状态节点运行时使用经典 `Enter / Tick / Exit`。
  - Reason: 经典接口能明确进入一次性输出、持续输出和离开一次性输出的边界；但必须保持纯数据输出，避免回到副作用回调式状态机。
- Decision: 状态机不保存具体动画播放配置。
  - Reason: 具体 clip、transition、fade、speed、start time 是表现层配置；状态机只需要稳定 key 来请求表现和匹配播放事实。
- Decision: timeline facts 不再由 Action request resolver 通过 runner 反向采样。
  - Reason: request gate 需要事实，但不应该知道 runner 的实现。pipeline 应在状态推进前准备所需 timeline facts，或通过独立 sampler 提供纯数据结果。
- Decision: 文档先收口，再实施代码。
  - Reason: 当前文档仍有 UnityHFSM 优先、BBB 旧聚合点和 Locomotion 局部图口径；先统一术语能避免后续实现继续分裂。

## Migration Plan
1. 更新文档，明确当前角色主线是项目自研统一分层状态机。
2. 增加 characterization 测试，锁定当前状态路径、transition 选择、snapshot/restore、timeline facts 和 state frame 输出。
3. 引入经典 `Enter / Tick / Exit` 生命周期接口和 frame builder 数据边界，先保持行为不变。
4. 引入或重命名通用 state graph runtime 类型。
5. 把 timeline facts 采样抽到独立模块，并让 Action request gate 读取 sampler 输出而不是 runner 实现。
6. 把状态输出解析抽到独立模块，并保持 motion executor / presenter 仍由 pipeline 调用。
7. 将状态机动画字段收敛为语义 key / timeline binding key，并把具体播放配置迁移到 Locomotion 动画配置和 Action 动画 Profile。
8. 收窄 runner restore state，只保存状态推进所需纯数据。
9. 删除或降级不再需要的 runner 方法，但不删除日志。
10. 更新测试和手动验证步骤。

## Risks / Trade-offs
- Risk: 抽太细变成浅 helper。
  - Mitigation: 每个模块必须通过职责归属测试；删除它时复杂度会回流到多个调用点，才允许保留。
- Risk: 与活跃 pipeline 变更冲突。
  - Mitigation: 本变更不改变 FullBody phase order，只移动状态机内部职责。
- Risk: Timeline 抽出后请求仲裁时序改变。
  - Mitigation: 先用 characterization 测试锁定 Dodge、TurnBack、Backstep 和 replay 行为。
- Risk: 经典接口被误用成副作用回调。
  - Mitigation: 静态测试禁止状态生命周期实现引用 Animancer、Animator、CharacterController、InputAction、Transform。
- Risk: 动画配置迁移影响现有资产。
  - Mitigation: 先加迁移测试和校验；实现阶段只做正式配置迁移，不加 fallback。
- Risk: 文档更新后 UnityHFSM 包仍在 manifest 中造成误解。
  - Mitigation: 文档明确包存在不代表角色主线使用；是否删除依赖另开审批。

## Verification Strategy
- 自动测试：
  - 状态图 runtime transition 选择测试。
  - restore round-trip 测试。
  - Enter 一次性输出、Tick 持续输出、Exit 一次性输出测试。
  - transition 发生帧只产出一个 `CharacterStateMachineFrame` 测试。
  - timeline sampler 与 runner 解耦测试。
  - state output resolver 不执行 Unity 副作用测试。
  - animation key 由状态输出产生、具体 clip/transition 由动画配置解析测试。
  - 静态边界测试：state graph runtime 不引用 Animancer runtime、CharacterController、InputAction、Transform、UnityHFSM。
  - FullBody rollback replay characterization 测试。
- 手动验证：
  - Sandbox 中验证 WASD、Run、TurnBack、Dodge Directional、Dodge Backstep 状态路径和表现不变。
  - Console 诊断能继续显示 active path、pending transition、timeline window 和 rollback first mismatch。
  - F6/F8 rollback 工具在对应 active change 完成后继续可用。
