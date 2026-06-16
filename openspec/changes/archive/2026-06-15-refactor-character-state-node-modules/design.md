# 角色状态节点能力模块设计

## Context
当前状态机已经满足“一棵状态图、一个 runner owner”的方向，但数据结构仍把所有节点压成同一个万能序列化结构：

```text
CharacterStateNodeDefinition
  - stateId
  - parentStateId
  - pathSegment
  - tags
  - variants
  - output
  - animation
```

这导致三个问题：

1. 分组节点、普通移动节点、TurnBack、Dodge 都暴露相同字段，配置者难以判断字段是否有效。
2. `Locomotion / Action` 被当作互斥 owner，运行时在多个地方写 `Owner.IsAction`、`HasTag(Action)`、`IsLocomotion` 分支。
3. 状态输出是大结构，而不是能力组合；新增 Attack、Jump、HitReact、Aim、Equip 时会继续往万能字段里塞。

更合适的模型是 ECS 思路：`StateNode` 是状态图里的实体，模块是能力数据，系统读取模块并产出输出。

## Goals
- 保持节点关系统一：所有状态仍是同一种节点。
- 把节点能力从万能字段拆为可组合模块。
- 让 `Locomotion / Action` 不再是互斥 owner 分支权威。
- 让输出按通道聚合，而不是按 owner 二选一。
- 让普通 Locomotion 节点不再携带无效 animation binding。
- 让 TurnBack 的 alias / motion policy 只保留一个正式来源。
- 让 Dodge、Attack 等动作通过动作请求、动作位移、动作动画等模块组合表达。
- 保持 gait 为运行时事实，不把 Walk/Run 写入状态节点。
- 保持 FullBody pipeline 顺序和唯一 runner owner。
- 迁移后不保留旧字段作为 fallback。

## Non-Goals
- 不引入 Unity ECS / DOTS。
- 不把每个状态变成 MonoBehaviour 或 ScriptableObject 组件实例散落路径。
- 不新增第二套状态机、第二个 Locomotion runtime 或第二个 Action runtime。
- 不改变现有 WASD、TurnBack、Dodge 的玩家可见行为。
- 不在本 proposal 实现 Attack、Jump 或完整动作系统。
- 不让状态模块直接调用 Animancer、Animator、CharacterController、InputAction 或 Transform。
- 不删除日志。
- 不运行 Unity batchmode。

## Proposed Model
概念模型：

```text
CharacterStateNodeDefinition
  - stateId
  - parentStateId
  - pathSegment
  - tags
  - modules[]

StateModuleDefinition
  - moduleType
  - payload
```

第一阶段模块集合：

```text
LocomotionPhaseModule
  - phase: Idle | MoveStart | MoveLoop | MoveStop | TurnBack

InputDrivenMotionModule
  - uses locomotion facts

ConfiguredActionMotionModule
  - requestKind
  - variant -> duration / distance / rotate / latch policy

ActionAnimationModule
  - variant -> animationKey / timelineBindingKey

LocomotionAnimationAliasModule
  - aliasKey / timelineBindingKey

TurnBackMotionPolicyModule
  - aliasKey
  - yawSource / translationSource / input lock / exit normalized time
  - bakedMotionProfileId

InputConsumeModule
  - requestKind

RunLatchModule
  - resetOnEnter
  - setOnExit
  - setOnComplete

TimelineWindowModule
  - windows[]
```

实际实现可以使用强类型数组、managed reference、模块表或显式字段集合，但外部语义必须是“节点核心 + 可组合能力模块”，不得继续让所有节点暴露所有能力字段。

## Runtime Shape
当前 runner 可以支撑这个方向，但需要升级输出解释层。

可保留：
- `CharacterStateMachineRunner` 维护 active state、state time、variant、pending transition、restore。
- `CharacterStateTransitionDefinition` 继续表达 from/to/priority/conditions。
- `CharacterStateTimelineFactSampler` 继续作为独立 sampler，但输入改为模块解析结果。
- `CharacterStateOutputResolver` 继续作为纯数据 resolver，但从“读 node.Output 和 node.Animation”改为“遍历模块产出输出通道”。

需要改变：
- `CharacterStateNodeDefinition` 从万能字段迁移到模块集合。
- `CharacterStateMachineSnapshot.Owner` 不再驱动逻辑分支；它可以作为兼容诊断事实从模块输出派生。
- `FullBodyFramePipeline.PresentStateFrameAnimationForPipeline` 不应只看 `Owner.IsAction` 决定播放 action 动画，而应消费 `AnimationOutputChannel`。
- `CharacterStateTimelineFactSampler` 不应通过 `HasTag(Locomotion/Action)` 判定读哪个播放进度，而应通过模块声明的 fact source 读取。
- `CharacterStateLifecycle` 不应通过 `targetNode.HasTag(Action)` 判断 action enter，而应通过目标节点是否具备动作请求/动作位移模块判断进入 payload。
- `CharacterStateMachineValidator` 不应校验“action movement 必须 tagged Action”，而应校验模块组合是否合法。

## Output Channels
状态帧建议变成通道集合或等价结构：

```text
CharacterStateFrame
  - snapshot
  - motionOutputs[]
  - animationOutputs[]
  - inputOutputs[]
  - latchOutputs[]
  - timelineFacts
  - runtimeFacts
```

第一阶段可以保留现有单 motion / 单 animation 字段作为兼容外壳，但内部必须从模块输出聚合生成。长期目标是调用方只消费通道，而不是先判断 `Owner`。

## Can Current State Machine Support This?
结论：可以支撑，但不是靠现有万能节点直接支撑。

当前状态机已经具备可保留基础：
- 单一 runner owner 已经存在。
- 状态图 transition 解释和 output resolver 已经分离。
- timeline sampler 已经独立。
- 动画播放和运动执行已经是外围 adapter。
- snapshot/restore 已经是纯数据。

当前阻碍：
- 节点模型仍是万能字段，不是模块集合。
- `Locomotion / Action` tag 和 owner 还参与运行时分支。
- animation binding 在所有节点上出现，普通 locomotion 节点无意义。
- TurnBack alias 同时存在于 animation binding 和 motion policy。
- Dodge action movement 与 DodgeActionConfigSO 有双权威风险。

因此实现路径应是“先在现有 runner 上替换节点数据和输出解析”，而不是再写一个 ECS runtime。

## Decisions
- Decision: Node 关系保持统一，不拆成 `LocomotionNode` / `ActionNode` 等不同节点类。
  - Reason: 状态图关系是同一种东西，差异来自能力模块。
- Decision: 不采用互斥 owner 作为运行时主分支。
  - Reason: TurnBack、Dodge、Attack、Aim 等行为会组合多个能力，互斥 owner 会不断制造特殊路径。
- Decision: `gait` 不进入状态节点模块。
  - Reason: gait 是输入、run latch、速度和移动配置共同产生的运行时事实；写入状态配置会导致 Walk/Run 状态爆炸。
- Decision: 第一阶段保留兼容 snapshot 字段。
  - Reason: 诊断、测试、rollback 现有代码依赖 active path、locomotion phase、action state；迁移时可从模块输出派生。
- Decision: 不新增 fallback 配置。
  - Reason: 旧万能字段只可作为迁移输入；迁移完成后同一资产不能同时维护旧字段和模块字段。

## Migration Plan
1. 增加 characterization 测试，锁定默认状态机资产当前节点、transition、输出、timeline 和动画 key。
2. 增加模块模型类型，先只覆盖当前状态需要的模块集合。
3. 增加模块组合 validator。
4. 增加从旧节点字段到模块集合的一次性迁移工具或编辑器菜单。
5. 迁移默认状态机资产到模块集合。
6. 让 output resolver 从模块集合产出当前等价 frame。
7. 让 timeline sampler 从模块声明的 playback fact source 采样。
8. 让 lifecycle payload 判断从 tag/owner 分支改为模块查询。
9. 让 FullBody pipeline 消费 animation/motion/input 输出通道。
10. 移除旧万能字段运行时读取路径。
11. 更新测试和手动验证说明。

## Risks / Trade-offs
- Risk: 模块系统过度抽象，变成浅层 indirection。
  - Mitigation: 只引入当前状态实际需要的模块；每个模块必须对应明确输出、validator 或迁移收益。
- Risk: Unity 序列化 managed reference 让配置难维护。
  - Mitigation: 实现阶段先评估强类型数组或显式模块字段，不强制使用 managed reference。
- Risk: 迁移资产时产生双权威。
  - Mitigation: 迁移后旧字段必须停止读取，验证默认资产不同时包含旧字段和新模块来源。
- Risk: 输出通道改造影响 rollback。
  - Mitigation: 先保持外层 frame 兼容，再逐步把 rollback snapshot 从通道事实读取。
- Risk: 与活跃 proposal 冲突。
  - Mitigation: 本变更必须在 `refactor-character-hierarchical-state-runtime` 归档或明确冻结后实施。

## Verification Strategy
- 自动测试：
  - 模块化节点配置序列化测试。
  - 默认状态机迁移等价测试。
  - 模块组合 validator 测试。
  - `MoveLoop` 不携带 action animation 模块测试。
  - `Dodge` 通过 action request / action motion / action animation 模块产出等价输出测试。
  - `TurnBack` 只从一个 alias 模块读取 timeline binding 测试。
  - output resolver 不使用 `Owner.IsAction` 分支测试。
  - timeline sampler 不使用 `HasTag(Action/Locomotion)` 决定播放事实来源测试。
  - FullBody pipeline 消费输出通道测试。
  - rollback replay characterization 测试。
- 手动验证：
  - Inspector 中分组节点只显示关系和标签。
  - 普通 MoveLoop 不显示无效 animation binding。
  - TurnBack 只显示一个 alias / motion policy 来源。
  - Dodge 显示动作请求、位移和动画模块。
  - Play Mode 验证 WASD、TurnBack、Dodge Directional、Dodge Backstep 行为不变。
