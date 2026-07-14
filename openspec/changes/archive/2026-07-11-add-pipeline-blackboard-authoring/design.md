# Design: Pipeline Blackboard 统一模型

## 目标

这次设计把“图里可调变量”“运行时临时变量”“网络可见事实”拆成同一模型下的不同层级：

- `Pipeline Blackboard Definition`：作者声明变量的地方，提供 key、类型、默认值、scope、lifetime、authority、sync policy。
- `Pipeline Blackboard Runtime`：每个角色 pipeline 运行实例持有的值存储和生命周期管理。
- `SyncFacts`：黑板或节点输出被策略解析后，进入 Motion、Action、GameplayResult、StateEffect、Presentation 同步域的事实集合。

## 概念模型

### Blackboard Variable

每个变量至少包含：

- `Key`：稳定业务 key，不依赖节点 GUID。
- `Type`：Bool、Int、Float、String、Vector2、Vector3、AnimationCurve、ActionContext、业务事件摘要等正式类型。
- `DefaultValue`：运行实例初始化或 scope 重置时使用。
- `Scope`：Graph、State、ActionInstance、Character、Frame 等。
- `Lifetime`：Config、Spawn、StateEnterToExit、ActionInstance、Frame、ManualClear。
- `Authority`：LocalOnly、ClientPredicted、ServerAuthoritative、PresentationOnly。
- `SyncPolicy`：None、ConfigVersion、InputDerived、SyncFact、ReplicatedCue、CorrectionOnly 等。
- `DebugCategory`：调试 UI 和日志分类。

### Authoring Surface

现有 `ExposedProperty` 不作为废弃路径处理，而是成为 Pipeline Blackboard 的作者入口：

- 行为图中读取/写入变量仍可以保留图变量心智。
- 角色 pipeline 下的 exposed 变量必须能解析成 blackboard declaration。
- 旧 `ExposedPropertyNode` 作为 `RunnableNode` 只能留在行为图生命周期里。
- TransitionRuleGraph 需要新的纯 ValueNode 读取入口，不能把 Runnable 节点塞进规则图。

### Runtime Instance

`CharacterGraphContext` 不应长期暴露散字典语义。目标形态是：

- `CharacterGraphContext` 持有或转发到 `PipelineBlackboardRuntime`。
- 所有 `TryGetBlackboardValue<T>()` 和 `SetBlackboardValue()` 都经过 declaration 校验和 scope/lifetime 管理。
- Action window/cue/result 写入 blackboard 时，只是保存最近运行值；是否同步仍由后续 SyncFacts 写入和 policy 决定。
- 帧末、状态退出、动作结束时按 variable lifetime 清理，不靠调用方手动记得删 key。

## 变量分类和网络策略

| 分类 | 示例 | 业务含义 | 网络策略 |
| --- | --- | --- | --- |
| 可调参数 | WalkThreshold、RunThreshold、TurnAngle | 作者调手感的配置 | 不逐帧同步；使用配置版本/hash 或角色配置身份 |
| 输入事实 | MoveAxis、AttackPressed | 本地输入帧事实 | 由 InputFrame/ClientCommand 进入 Motion/Action，同步域已覆盖 |
| 输入派生值 | MoveAxisMagnitude、MoveAngleDelta | 可由输入和上下文确定性计算 | 默认不独立同步，必要时用于本地规则图 |
| 动作运行事实 | ActionContext、LastHitWindow、LastGameplayResult | 动作事务过程中的输出或临时缓存 | 输出事实通过 Action/GameplayResult SyncDomain；缓存本身不复制 |
| 表现事实 | CameraCue、SfxCue、VfxCue | 表现层事件 | local-only 默认不发；需要远端可见时进入 Presentation SyncDomain |
| 状态/效果事实 | Stun、Invulnerable、ResourceDelta | 影响玩法状态的实例变化 | 进入 StateEffect 或 GameplayResult SyncDomain |

## TransitionRuleGraph 接入

TransitionRuleGraph 的业务目标是“纯条件求值”，因此黑板读取必须满足：

- 节点类型继承 `ValueNode`，可被 `TransitionRuleGraph.CanCreateNodeType()` 接受。
- 只读取当前 graph context 的 Pipeline Blackboard、InputFrame 或 SyncDomain 输入，不 tick 行为节点。
- 缺失变量 declaration、类型不匹配或跨 scope 读取必须报告为非法或返回明确失败结果，不走 fallback key。
- 常量、比较、And/Or/Not 等逻辑继续通过已有 ValueNode/PropertyPort 组合表达。

对 Corin 这类 locomotion transition，推荐结构是：

- `MoveAxis Input Value` 读取输入事实。
- `MoveAxisMagnitude ValueNode` 产出派生值。
- `Blackboard Float ValueNode` 读取 `WalkThreshold`、`RunThreshold` 等调参值。
- `CompareNode`、`AndNode`、`OrNode` 拼接条件。

## 网络边界

Pipeline Blackboard 不直接成为网络协议：

- NetworkSendStage 仍只收集 `CharacterPipelineOutput.SyncFacts`。
- 黑板变量只有在 `SyncPolicy` 要求时，才由正式节点、runtime 或 resolver 转成 SyncFacts。
- 配置类变量不逐帧发送，使用配置版本、角色 loadout id 或 pipeline definition hash 对齐。
- 输入派生值不重复发送，远端或服务端应从输入事实和配置确定性计算。
- Action window/cue/result 不因为写入 blackboard 就自动同步，必须进入对应 SyncDomain output。

## 迁移顺序

1. 建立 `PipelineBlackboardVariable` 元数据模型和枚举。
2. 建立 `PipelineBlackboardRuntime`，支持 typed get/set、scope/lifetime 清理和 declaration 校验。
3. 将 `CharacterGraphContext` 的 blackboard API 改为委托到 runtime instance。
4. 将 BTSMTL `ExposedProperty` 映射为 blackboard declaration，补齐缺失元数据。
5. 新增 TransitionRuleGraph 可用的纯 ValueNode 黑板读取节点。
6. 用 `CompareNode`、`AndNode`、`OrNode` 组合替换临时业务条件节点。
7. 迁移 Corin locomotion/action 阈值和临时变量到 blackboard declaration。
8. 将网络可见输出保留在 SyncFacts/domain resolver，不新增 blackboard 直连网络路径。
9. 在 Runtime Debug 中并排展示变量 declaration、当前值、SyncFacts 输出和未发送原因。

## 风险

- 如果直接复用 `BaseGraph.m_ExposedProperties` 扩字段，改动小但会把 BTSMTL 通用图和角色 pipeline 变量策略耦合更深。
- 如果新增 `m_BlackboardVariables`，语义更干净，但需要资产迁移和编辑器 UI 一次性改完整，否则会短期出现双面板。
- 如果先只做 runtime API，不做 TransitionRuleGraph ValueNode，Corin 的阈值条件仍然无法优雅落到 If/And/Or/Compare 组合。
- 如果把变量同步策略做得过细，作者负担会上升；需要用默认模板区分 tunable、input-derived、action-output、presentation-only，而不是让每个变量从零配置。
