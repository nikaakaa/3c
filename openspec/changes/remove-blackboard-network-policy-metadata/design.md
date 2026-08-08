## Context

Pipeline Blackboard 目前把三类不同问题塞进一个 declaration：

1. 变量是什么：identity、key、类型、默认值、owner、scope、lifetime、category。
2. 变量是否从 portable input 注入：`SyncPolicy=InputDerived + InputValueId`。
3. 写入是否投影 GameplayFact：`SyncPolicy=SyncFact + FactProjection`。

`Authority` 和 `SyncPolicy` 的名字把后两类问题伪装成变量级网络策略，但 Target Runtime 实际只消费 `InputDerived`。ActionWindow 是否发送由 Network Model coverage 决定，其他枚举值没有运行时消费者。结果是 UI、Document 和 Semantic IR 表达的配置比实际系统更强，作者无法从字段判断真实行为。

本次设计删除这个伪抽象，只保留当前系统真正兑现的两个能力：输入绑定和事实投影。

## Goals / Non-Goals

### Goals

- Blackboard declaration 不再保存或展示变量级网络权威、同步、复制、纠正策略。
- 基础变量、输入绑定、事实投影具有独立数据模型、Validator 和 authoring API。
- Character 与 AI Blackboard 共用同一基础 declaration，不要求 AI 变量填写无意义网络默认值。
- Float32 与 Fixed 从同一 Semantic IR 的 `InputValueId` 建立相同 input-to-state binding。
- ActionWindow projection 继续生成相同 `ActionWindowFact`、ActionInstance 和 EventId。
- Agent Document、人工 UI、Semantic Frontend 和 Target Runtime 只存在一套合同。
- 四个正式资产和全部 generated product 一次迁移到新版本，不保留旧 reader 或旧字段。

### Non-Goals

- 不删除或重命名 Action lifecycle、Window Cancel、Tree Interrupt、Tree Abort、Natural Complete。
- 不为 ActionWindow 增加 ServerAuthoritative packet mapping。
- 不改变 Network Model Source、Schedule、History、Correction、Egress 或 Endpoint。
- 不把输入绑定移回 Host、Scene、Presentation 或 Graph key 猜测。
- 不增加 `LocalOnly` 默认解释、兼容枚举、`FormerlySerializedAs`、旧 Document reader 或 runtime fallback。
- 不自动执行 Character Build，不自动运行 Unity Test Runner，不运行 Unity batchmode。

## Decisions

### Decision: Declaration、Input Binding、Fact Projection 使用三个独立合同

基础 declaration 只保存：

```text
Identity + Key + ValueType + DefaultValue
+ Owner + Scope + Lifetime + CategoryPath
```

输入绑定是可选 typed payload：

```text
InputBinding { InputValueId }
```

事实投影是可选 typed payload：

```text
FactProjection {
  Kind
  ActionWindowType
  ActionWindowId
  ActionWindowDigest
}
```

人工 authoring API 分为 `ConfigureDeclaration`、`ConfigureInputBinding` 和 `ConfigureFactProjection`。Mutation command 也按相同边界携带 payload，不能重新引入一个决定三者组合的 mode 或 policy 枚举。

业务取舍：作者看到的字段和运行时真实能力一致，Action target 与 ActionWindow 不再共享一个含糊策略开关。代价是序列化、Document 和 Mutation 都需要破坏性迁移，但迁移后新增输入类型或 fact projection 不需要修改网络策略枚举。

### Decision: 非空 InputValueId 是唯一输入绑定标记

系统不新增 `InputDerived` 的替代枚举。`InputBinding` 存在且 `InputValueId` 非空时，Compiler 必须建立 binding；payload 缺失时该 declaration 不是输入绑定。

Validator 必须检查：

- declaration 属于 Character scope、Spawn lifetime；
- `InputValueId` 稳定、非空且在当前 Definition 的 Input catalog 中唯一；
- Blackboard value kind 与 Program input value kind 精确匹配；
- Input catalog、Semantic IR、Float32 和 Fixed Target 都能解析同一 identity；
- 同一 `InputValueId` 不得被普通 InputProfile 和 Blackboard input binding 重复声明。

业务取舍：运行时不再读取 magic enum value `2`，输入链只看正式 identity。代价是空字符串不能再被当作“配置了但暂时无输入”的中间状态，作者必须完成合法绑定或保持 payload 不存在。

### Decision: ActionWindow 只由 FactProjection 表达

ActionWindow projection 的合法性只取决于：

- Bool value kind；
- Frame scope、Frame lifetime；
- `Kind=ActionWindow`；
- 非空 WindowType、WindowId；
- 稳定 Digest；
- 写入时具有正式 Action Context provenance。

它不再检查 `SyncFact`，也不产生 packet policy。Program 继续在 EndFrame projection stage 生成 `ActionWindowFact`。Network Model Egress 仍只按自己的 fact-kind/producer coverage 决定是否消费。

业务取舍：动作窗口仍能被 Transition、Debug 和未来 Network Model 使用，但 Blackboard 作者不再承诺当前模型一定发送。代价是需要在 Model Debug 中继续明确区分“fact 已生成”和“model 未覆盖”。

### Decision: Semantic IR 使用可选字段，不保存 authoring 网络标签

Blackboard catalog entry 只输出基础字段、可选 `InputValueId` 和可选 projection payload。`Authority`、`SyncPolicy` 不进入 discovered model、Semantic IR、SourceMap、Reader text/JSON 或 Target Program。

Target lowering 将 `BuildInputDerivedBindings` 与 `InputDerivedStateBinding` 改为中性命名，只遍历具有合法 `InputValueId` 的 declaration。Float32/Fixed Evaluate 仍在 Timeline Decision 和 Graph control 前把当前 Tick portable input 写入同一 typed Character State transaction。

业务取舍：SemanticHash 只覆盖会改变程序行为的数据，删除无消费者字段后，同一 Program 在 Local、Authority 和 Rollback 下复用时不再携带虚假的网络差异。代价是 SemanticHash、ProgramHash 和 LayoutHash 全部变化，必须重新发布产物。

### Decision: 删除 catalog field 并提升完整 artifact 边界

`ProgramCatalogFieldId.SyncPolicy` 直接删除，剩余 field id 和 runtime field count 按新 schema 收敛。实现必须提升：

- Character Semantic Frontend compiler version；
- Semantic IR artifact/payload format；
- Float32/Fixed Program artifact、program/layout format 中受影响的版本；
- Float32/Fixed Target ABI；
- 任何把上述 identity 写入 generated wrapper、catalog 或 handshake 的正式 manifest。

不提升与本次 payload 无关的 packet protocol、State codec 或 Pipeline product schema。旧 `.csir`、Target Program 和 wrapper 必须在 preparation/build 门禁明确失败，不能按字段缺失补默认值。

业务取舍：版本提升让旧产物无法混入新 runtime，错误会在 Session 启动前暴露。代价是两个角色、两个 Numeric Target 和依赖它们的 Network Test product 都需要重新构建。

### Decision: Agent Document v3 使用稀疏嵌套 payload

Blackboard declaration JSON 采用以下目标形状：

```json
{
  "id": "...",
  "key": "ActionTarget",
  "valueType": "ActionTargetSnapshot",
  "scope": "Character",
  "lifetime": "Spawn",
  "categoryPath": "Action/Target",
  "inputBinding": {
    "inputValueId": "ActionTarget"
  }
}
```

ActionWindow 使用独立 payload：

```json
{
  "id": "...",
  "key": "Attack1Hit",
  "valueType": "Bool",
  "scope": "Frame",
  "lifetime": "Frame",
  "factProjection": {
    "kind": "ActionWindow",
    "windowType": "Hit",
    "windowId": "Attack1Hit",
    "digest": 0
  }
}
```

没有绑定或投影时省略对应 payload，不输出 `None`、空对象或空字符串。`authority`、`syncPolicy`、旧平铺 `inputId/factProjection/window*` 都是未知字段。Package Mapper 必须在 Reconciler 前拒绝旧 package，并要求重新 checkout。

业务取舍：JSON 直接反映三个独立模块，AI 不需要记枚举组合矩阵。代价是现有 v3 package 不能原地继续 apply，但 Document 本来就是可重新 checkout 的同步工作副本，不值得保留兼容 reader。

### Decision: AI Blackboard 只按 AI scope/lifetime 和 typed value 校验

AIController/Graph/AITick declaration 不再强制 `LocalOnly + None`。AI Compiler 和 Validator 只检查 AI scope/lifetime、owner、类型、默认值、读写能力和是否越过 Character Blackboard 边界。当前 AI Blackboard 不允许 Input Binding 或 Character Fact Projection；发现对应 payload 时明确失败。

业务取舍：AI 不再为了表示“本地”保存无运行时意义的字段，同时继续保持 AIControllerState 与 CharacterSimulationState 分离。未来如果 AI 需要网络观察，必须通过正式 Observation/Intent 边界规划，不能复活变量级 authority。

### Decision: 资产迁移必须由 Document v3 typed transaction 完成

受影响 Unity authoring 只有四个 RootTree。迁移通过新 Document schema checkout 目标状态，由 Reconciler 生成基础 declaration、Input Binding 和 Fact Projection 的 typed Mutation，并在一个 owner lock、Undo、Validator、Save、reverse export、package publish 事务中完成。

为避免无业务 diff 时旧序列化字段残留，RootTree 的 Blackboard authoring schema revision 必须成为 Reconciler 的正式迁移门禁。旧 revision 进入新 package apply 时，Reconciler 计划一次完整的 typed Blackboard normalization，并只在事务成功后写入新 revision。它不是 runtime fallback，不读取或解释旧网络枚举；四个正式资产迁移完成后，正常 apply 只按目标 diff 工作。

禁止直接文本替换 Unity YAML、`FormerlySerializedAs`、OnValidate 自动迁移、AssetPostprocessor 自动迁移或窗口打开时重写。

业务取舍：资产保存、Undo、Validator 和 Document package 始终一致，失败可以完整回滚。代价是实现和内容迁移必须按固定顺序完成，不能先删字段再随手修 YAML。

## Alternatives Considered

### 方案一：只隐藏 Inspector 字段，保留序列化枚举

收益是 UI 改动最小，现有资产和 artifact 不需要立刻迁移。代价是 Agent Document、Semantic IR、Program catalog 和代码 API 仍继续传播假策略；后续作者或工具仍可能重新使用这些字段，逻辑层与网络层没有真正分开。本变更不采用。

### 方案二：把旧 SyncPolicy 拆成 InputPolicy 与 FactPolicy 两个新枚举

收益是比当前枚举更细，迁移时容易一一映射。代价是输入绑定和事实投影仍由 mode 控制，每增加一种 input source 或 fact kind 都要扩枚举；Network Model 语义仍容易重新混入 declaration。本变更不采用，直接使用 typed payload 的存在性和内容表达能力。

### 方案三：把 Input Binding 和 Fact Projection 移到独立资产

收益是物理分离最强，也能独立复用。代价是一个 Blackboard declaration 会被三个资产共同定义，删除、移动、Document 对账和作者导航都需要跨资产保持 identity，形成新的第二数据源风险。当前 binding/projection 都只属于单个 declaration，因此保留为 declaration-owned 独立 payload。

### 方案四：升级为新的 Document v4 package

收益是 package 顶层版本能直接区分旧新形状。代价是五个固定生命周期工具、Character/AI domain、同步状态和Presentation分片都要一起升级，而实际变化只在 Blackboard capability。当前 Document v3 已有 strict parser、context hash 和 capability/schema revision，足以拒绝旧package并要求重新checkout，因此保留v3顶层合同，只提升Blackboard authoring schema revision。

## Unified Flow

```text
Input Profile value
  -> CharacterSimulationInput
  -> Blackboard InputBinding(InputValueId)
  -> typed Character State address
  -> Graph / Timeline / Action read

Frame Blackboard write + Action Context
  -> FactProjection(ActionWindow)
  -> ActionWindowFact
  -> SimulationActorTickResult
  -> Network Model fact/producer coverage
  -> optional packet
```

基础 Blackboard declaration 不出现在第二条链的 packet 决策中；Network Model 也不反向读取 Blackboard。

## Migration Order

1. 安装新的 typed declaration/InputBinding/FactProjection 模型和 Validator，删除 enum 依赖。
2. 同步人工 UI、Timeline Decision、AI Compiler 和 Graph Data Catalog。
3. 更新 Document v3 strict schema、Exporter、Mapper、Reconciler、Mutation、Validator 和技能合同。
4. 更新 Semantic Frontend、IR codec、Reader、Float32/Fixed lowering 和 ABI 门禁。
5. 通过 Document v3 新 package 对四个 RootTree 执行原子 schema normalization，确认 reverse export 为新形状。
6. 删除所有旧字段、旧参数、旧诊断文本和旧 schema 解析代码。
7. 显式发布两个 Character Definition 的 Semantic IR 与 Float32/Fixed Program，并发布两个 AIIntentProgram。
8. 运行新增自动化测试和静态残留扫描；Character Build 和 Unity 测试只由明确操作触发。

## Risks

- 若先删除序列化字段再迁移资产，Unity YAML 可能保留未知字段。必须按 schema revision 的 Document transaction 顺序执行。
- 若只改 Float32，Fixed 仍会按 magic `SyncPolicy` 建 binding。两个 Target 必须在同一 change 中收口。
- 若 Document 只删字段但 Mutation command 仍接收旧参数，AI 和人工 UI 会形成两套合同。Exporter、Mapper、Reconciler、Mutation、Validator 必须一起更新。
- 若只提升 compiler version而不提升 artifact/ABI，旧 Program 仍可能通过 composition identity。所有实际受影响边界必须一起提升。
- `ConfigVersion` 删除后不能用另一个 Blackboard 布尔或版本字符串替代。配置变化继续由 canonical content hash 和 build identity 表达。
- ActionWindow 没有当前 ServerAuthoritative mapping。验收时必须把“没有发送”识别为 model coverage 现状，而不是本次清理回归。
