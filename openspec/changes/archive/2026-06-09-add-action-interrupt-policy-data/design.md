# Design: 动作打断策略数据源

## Context

现有 `ActionInterruptArbiter` 的输入是：

```text
ActionInterruptContext
IReadOnlyList<ActionInterruptRequest>
IReadOnlyList<ActionInterruptPolicy>
```

这说明仲裁器已经足够纯：它不关心策略从哪里来，也不切状态、不播放动画。现在缺的是“策略表从哪里配置、怎么校验、怎么稳定转换成 runtime policy”。如果直接在攻击状态、闪避状态或 Presenter 里构造策略，会让规则散落，后续编辑器和预测回滚都难以复用。

## Goals

- 给现有仲裁器提供最小可序列化策略集合数据源。
- 保持 runtime 仲裁输入仍然是纯 `ActionInterruptPolicy`。
- 让策略集合可以在 Unity Inspector 中创建和编辑。
- 对策略集合做统一校验。
- 保持模块化：配置层负责序列化，模型层负责纯数据，solver 层负责转换和校验。

## Non-Goals

- 不定义完整动作状态。
- 不定义动画 alias、clip、fade、speed、root motion。
- 不实现动作状态机。
- 不接输入缓冲和 tick runner。
- 不实现编辑器窗口或 Timeline 轨道。
- 不修改现有 Locomotion 状态图。

## Proposed Model

```text
ActionInterruptPolicyDefinition
  fromStateId: string
  targetStateId: string
  minPriority: int
  timingRule: ActionInterruptTimingRule
  windowStart: float
  windowEnd: float
  force: bool
  note/debugName: string

ActionInterruptPolicySet
  policies: IReadOnlyList<ActionInterruptPolicyDefinition>

ActionInterruptPolicySetSO
  serialized policies
  ToPolicySet()
  Validate()

ActionInterruptPolicySetCompiler
  Compile(policySet) -> IReadOnlyList<ActionInterruptPolicy>
```

`ActionInterruptPolicyDefinition` 是序列化友好的定义；`ActionInterruptPolicy` 继续是仲裁器消费的纯 runtime 数据。

## Decisions

### Decision: 不把 SO 直接喂给仲裁器

仲裁器继续只接收 `IReadOnlyList<ActionInterruptPolicy>`。

Reason: SO 属于 Unity 配置入口，不应该污染纯逻辑 solver。之后服务端、预测回滚、测试或生成数据都可以绕过 SO，直接传入 runtime policies。

### Decision: 第一版只做策略集合，不做状态 catalog

本变更只关心“从状态 A 到状态 B 的打断规则”。状态是否存在、属于哪一层、用什么动画，后续用 `ActionStateDefinition` 或 catalog proposal 单独处理。

Reason: 当前最直接的缺口是策略来源，不是完整状态系统。先把策略数据跑通，能让下一步接入和编辑器更清楚。

### Decision: 状态 ID 先用字符串字段

策略定义中的 from/target 用字符串保存，再编译成 `ActionStateId`。

Reason: 这和现有 `ActionStateId` 对齐，也避免提前引入生成器、GUID catalog 或枚举分裂。

### Decision: 自定义编辑器窗口不在本变更

第一版只保证可通过 Inspector 编辑 ScriptableObject，并通过测试和校验证明数据有效。

Reason: 在规则语义稳定前做可视化编辑器容易固化错误模型。Timeline 和图形化编辑后续再做。

## Risks / Trade-offs

- Risk: 字符串 ID 容易手填错误。
  - Mitigation: 第一版 validator 报错；后续 state catalog 可提供下拉和引用校验。
- Risk: SO 和纯数据模型重复。
  - Mitigation: SO 只负责序列化，runtime 统一消费编译后的 `ActionInterruptPolicy`。
- Risk: 过早接入运行时造成第二条动作路径。
  - Mitigation: 本变更只新增数据和测试，不修改 `PlayerLocomotionController` 或状态图。

## Validation

- OpenSpec strict 校验。
- Unity EditMode 测试覆盖：
  - 空策略集合合法。
  - 单条策略定义能编译成 `ActionInterruptPolicy`。
  - 多条策略保持顺序。
  - 非法 from/target ID 报错。
  - 负优先级报错。
  - 非法窗口报错。
  - 重复策略报告 warning。
  - 编译后的策略能被 `ActionInterruptArbiter` 接受。
  - Action 配置/模型/solver 不引用 Animancer、AnimationClip、Animator、CharacterController、Cinemachine、Input System 或 BBB。

## Future Extensions

- `ActionStateDefinition` / `ActionStateCatalog`。
- Inspector 下拉选择 state ID。
- Timeline cancel/combo window 采样。
- FullBody / UpperBody / LowerBody 多层策略集合。
- 运行时接入输入请求缓冲和动作状态机。
