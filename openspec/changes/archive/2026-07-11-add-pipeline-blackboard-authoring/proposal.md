# Proposal: 统一 Pipeline Blackboard 创作、运行时和同步策略

## Why

Corin 状态机重构后，输入事实、Transition 条件、Action 输出、Timeline window/cue 和网络同步事实已经进入同一条角色 pipeline 主链路。但当前变量口径仍然分裂：

- BTSMTL `BaseGraph.m_ExposedProperties` 是图内可暴露变量，当前主要服务 graph-local Get/Set。
- `CharacterGraphContext.m_Blackboard` 是运行时 `Dictionary<string, object>`，Action window/cue/result 会写入它，同时也写入 `SyncFacts`。
- `CharacterPipelineOutput.SyncFacts` 是网络/回放/debug 可消费事实，但不是任意变量同步通道。
- `TransitionRuleGraph` 已经被 spec 限定为纯 ValueNode 求值图，现有 `ExposedPropertyNode` 是 `RunnableNode`，不能直接进入 TransitionRuleGraph。

这导致作者会自然地问：阈值、输入派生值、动作临时变量、网络可见值到底是不是同一个“黑板”？如果不先统一语义，后续很容易出现两条配置路径：一条用 ExposedProperty，一条用 CharacterGraphContext blackboard，再额外开一条网络变量复制路径。

## What Changes

- 新增 `Pipeline Blackboard` 作为角色管线内部统一变量模型，覆盖图变量声明、运行时读写、作用域、生命周期和同步策略。
- 将 BTSMTL `ExposedProperty` 定义为 Pipeline Blackboard 的 authoring 表面，而不是独立于运行时 blackboard 的第二套变量系统。
- 将 `CharacterGraphContext` 的 blackboard 从散 `Dictionary<string, object>` 语义收敛为 Pipeline Blackboard runtime instance 的访问入口。
- 引入黑板变量元数据：稳定 key、类型、默认值、作用域、生命周期、写入权限、复制/回滚策略和 debug 分类。
- 规定 TransitionRuleGraph 只能通过纯 `ValueNode` 兼容节点读取 blackboard/exposed 值，不能把 `RunnableNode` 形态的 `ExposedPropertyNode` 放进规则图。
- 规定网络层不自动同步 blackboard。只有被显式策略映射为 SyncFacts 的变量或事件，才进入 Motion、Action、GameplayResult、StateEffect、Presentation 等同步域。
- 规定可调参数、输入事实、输入派生事实、动作运行事实、表现事实使用不同同步策略，避免把所有变量都变成网络包字段。

## Out of Scope

- 不在 proposal 阶段修改 Unity 代码、资产或 Corin RootTree。
- 不恢复旧 BBB registry、旧 locomotion/action SO 或旧分裂数据源。
- 不新增 fallback 配置、兼容路径或临时桥接路径。
- 不把所有 blackboard key/value 自动复制到网络层。
- 不实现真实 transport、Fantasy handler 或服务端裁决。
- 不新增测试，除非后续明确要求。

## Current Behavior

- `CharacterGraphContext` 直接持有 `Dictionary<string, object> m_Blackboard`，并暴露 `TryGetBlackboardValue<T>()`、`SetBlackboardValue()`。
- Action window、motion sample、cue、gameplay result 的提交方法会同时写入 runtime blackboard 和 `CharacterPipelineOutput.SyncFacts` 的对应 SyncDomain。
- `CharacterPipelineOutput.SyncFacts` 当前包含 Motion、Action、GameplayResult、StateEffect、Presentation 五类 domain output。
- `CharacterNetworkSendStage` 只从 `frame.Output.SyncFacts` 收集同步事实，不读取 graph `ExposedProperty` 或 runtime blackboard。
- `ExposedPropertyNode` 是 `RunnableNode`，用于行为图 Get/Set `BaseExposedProperty`。
- `TransitionRuleGraph.CanCreateNodeType()` 和嵌套图校验禁止 `RunnableNode`，只允许纯 ValueNode 和结果节点。

## Decisions and Tradeoffs

### 方案 A：保持 ExposedProperty 和 runtime blackboard 分离

业务取舍：实现改动最少，但作者会继续面对两套变量心智。调参值放 ExposedProperty、动作临时值放 blackboard、网络事实放 SyncFacts，短期能跑，长期会让 Corin 这种状态机资产越来越难解释。

### 方案 B：把 ExposedProperty 只定义为静态调参

业务取舍：适合走跑阈值、转身角度、冷却常量这类配置，但解决不了 ActionContext、最近命中窗口、目标 key、运行时事件缓存等需要生命周期管理的值。它会把“配置”和“运行时变量”分干净，但作者仍然没有统一黑板。

### 方案 C：把 ExposedProperty 升级为 Pipeline Blackboard authoring 表面

业务取舍：作者仍然使用熟悉的图变量入口，但变量声明会补齐 scope、lifetime、authority、sync policy。运行时统一从 Pipeline Blackboard 读写，网络只消费显式映射后的 SyncFacts。这是本 proposal 选择的方向，改动比方案 B 大，但能让图、状态机、Timeline、Action 和网络边界使用同一套业务语言。

### 方案 D：自动同步整个 blackboard

业务取舍：看起来最省配置，但会把调参、输入派生值、本地表现变量、临时缓存都推向网络层，增加带宽、回滚和 authority 风险，也破坏当前 SyncDomain 合同。因此不采用。

## Spec Alignment Notes

- `character-pipeline-runtime` 已要求 `CharacterGraphContext` 提供 gameplay blackboard，并要求 `SyncFacts` 表达可记录、调试、回放、loopback 或网络 backend 消费的事实。本 proposal 与其一致，但补齐 blackboard 的声明、作用域和生命周期。
- `character-network-sync-domain-contract` 已要求 NetworkSendStage 按 SyncDomain 和 policy 打包，不同步 Graph 执行路径、SubTree membership、Timeline 结构或节点身份。本 proposal 延续该边界，明确 blackboard 不默认网络同步。
- `btsmtl-sm-node-authoring` 已要求 TransitionRuleGraph 是纯 Bool 条件求值图。本 proposal 明确 blackboard/exposed 读取必须以纯 ValueNode 进入规则图，不能复用现有 Runnable `ExposedPropertyNode`。
- `character-action-authoring-closure` 已要求 Graph 内部临时读写命名为 blackboard，不得命名为 fact。本 proposal 延续“blackboard 是内部变量，fact 是已发生输出”的命名边界。

## Open Questions

- 作者 UI 上是否继续显示 `ExposedProperty` 名称，还是在角色管线图中改名为 `Blackboard Variable`？
- 序列化上是否复用 `BaseGraph.m_ExposedProperties` 并逐步扩字段，还是新增正式 `m_BlackboardVariables` 后迁移旧字段？
- 角色配置版本/hash 的正式来源是 `CharacterPipelineDefinition`、动作库组合，还是后续独立的 pipeline config manifest？
