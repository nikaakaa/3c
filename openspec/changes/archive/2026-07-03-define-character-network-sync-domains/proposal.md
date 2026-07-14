# Proposal: 定义 Graph 主体的网络 SyncDomain 合同

## Why

当前讨论和现有 spec 已经确认几件事：

- Graph/BTSMTL 是角色玩法编排主体，不能同步 Graph 结构本身。
- `ActionInstance` 只适合表达攻击、翻滚、格挡、受击等离散动作事务。
- locomotion 和普通连续运动更像 UE CMC：它们按 tick/input/state 校正，不应该强塞进 action。
- 当前 `NetworkOutput` 已经收集 input command、action activation、window、motion、cue、gameplay result 和 correction，但缺少正式的同步域语义来说明这些输出如何进入网络。
- 当前 `character-action-activation-flow` 允许 Timeline 或非 Timeline 输出从当前 `ActionContext` 关联 action，这对最小闭环有用，但不够明确；后续应收紧为显式 action context，而不是让输出偷读 ambient current active action。

需要一个 proposal 把问题钉住：**Graph 产出 typed output，Pipeline 按 SyncDomain 处理，Network 按 SyncDomain 的稳定身份同步。**

## What Changes

本变更定义 `character-network-sync-domain-contract`：

- `SyncDomain` 是 runtime/pipeline contract，中文口径是“同步域”，不是 port、特殊 node 或 profile 表。
- `MotionSyncDomain` 处理连续运动，稳定同步键是 `EntityId + Tick/InputSequence`。
- `ActionSyncDomain` 处理离散动作事务，稳定同步键是 `ActionInstanceId`。
- `GameplayResultSyncDomain` 处理命中、伤害、格挡结果、objective 结果、PvE aggro/threat 等权威玩法结果，稳定同步键是 `GameplayResultId`，可选关联 `ActionInstanceId`。
- `StateEffectSyncDomain` 处理 buff/debuff/stun/dead/downed/revive/resource/cooldown 等状态实例，稳定同步键是 `StateId` 或 `EffectInstanceId`。
- `PresentationSyncDomain` 处理 VFX/SFX/camera/hit stop 等表现事件，稳定同步键是 `CueEventId`，默认 local/predicted，可按策略复制。
- `NetworkSendStage` 按 SyncDomain 和 policy 打包 outgoing packet，不读取 Graph 节点路径、SubTree membership 或 Timeline 结构。
- `NetworkReceiveStage` 按 SyncDomain 注入 incoming decision/snapshot/correction，不直接修改 Graph 执行结构。

本变更同时修改 `character-action-activation-flow`：

- Action activation 成功后 MUST 产生可传递的 action runtime context/handle。
- Timeline 和输出节点关联 action 时 MUST 使用显式 action context、playback request context 或等价显式参数。
- 没有显式 action context 的 Timeline、Motion、GameplayResult、Cue 输出 MUST NOT 自动继承当前 active action。

本变更还补充清理当前 spec 中的旧输出命名：

- `character-action-instance-runtime` 使用 GameplayResult 表达动作相关权威结果，不再把旧 Combat 作为正式输出类别。
- `character-action-network-policy-authoring` 将 hit/result rewind 表达为窗口策略解析结果，不把 Timeline clip 当成网络策略承载点。
- `character-motion-semantics` 将来自命中、击退、目标点或服务端结果的运动影响统一表述为 gameplay result 来源。

## Non-Goals

- 不实现真实 transport、Fantasy handler 或服务端裁决。
- 不实现完整 rollback，也不要求所有 SyncDomain 都记录 replay history。
- 不把 Graph、SubTree、StateNode、TimelineNode 或 NodeModule 变成网络同步单位。
- 不恢复 AbilityTree、ActionTree、ActionModule、node membership table。
- 不要求所有节点都接特殊 port，也不为每个业务字段新增特殊 port。
- 不把 locomotion 强行塞进 ActionSyncDomain。

## 决策和 Tradeoff

### 方案 A：同步 Graph / SubTree

- 优点：作者看起来容易理解，“这段图就是这个动作/网络单元”。
- 缺点：Graph 是编排结构，不是稳定协议；Timeline、Motion、GameplayResult、Presentation 的结果会跨结构边界，服务端也不会运行客户端 BTSMTL/Timeline/Animancer。
- 业务取舍：面试 demo 会显得网络边界不专业，后续 Fantasy 服务端接入会被客户端 graph 结构绑死。

### 方案 B：所有 gameplay 都变成 ActionInstance

- 优点：统一 id，短期实现简单。
- 缺点：连续运动、状态快照、buff、cue、gameplay result 生命周期不同，全部塞进 action 会让 action 变成新 Ability 大桶。
- 业务取舍：攻击能讲清楚，但 locomotion、远端代理、状态同步和 PvE/objective 会变混。

### 方案 C：Graph 产出 typed output，Pipeline/Network 按 SyncDomain 处理

- 优点：Graph 仍是主体；不同业务生命周期使用不同稳定 id；Motion/Action/GameplayResult/StateEffect/Presentation 可以选择不同同步策略。
- 缺点：需要定义 SyncDomain 合同和 policy resolver；`NetworkOutput` 需要从“列表集合”进化为“按同步域的网络合同”。
- 业务取舍：最符合 `2v2vE / 2v2 + PvE` 混合网络 demo，能同时展示手感、服务端权威压力和清晰工程边界。

本 proposal 选择方案 C。

## 与现有 Spec 的关系

- `character-pipeline-runtime` 已要求 `NetworkReceiveStage` 和 `NetworkSendStage` 是正式边界，本 proposal 补充它们按 SyncDomain 消费/注入数据。
- `character-motion-semantics` 已要求 motion 通过 MotionStage 统一结算，本 proposal 补充 MotionSyncDomain 的网络稳定键和 correction 边界。
- `character-action-activation-flow` 已要求 action 不靠结构身份，本 proposal 收紧 action 输出归属：输出必须有显式 action context，而不是默认读取 ambient current active action。
- `character-action-instance-runtime` 和 `character-action-network-policy-authoring` 已要求动作事务、profile policy 与结构归属分离，本 proposal 补充 GameplayResult 命名并继续保持策略集中解析。
- `add-gameplay-sync-runtime-character-adapter` 正在定义 GameplaySyncRuntime、Character adapter 和 loopback peer，本 proposal 不替代它；它定义 SyncDomain packet 怎么进入通用同步运行时，本 proposal 定义有哪些 SyncDomain 以及它们的稳定身份。

## Open Questions

- `ActionInstanceHandle` 是否作为正式 value type 暴露给 BTSMTL PropertyPort，还是先作为 `ActionRuntimeContext` 显式参数传入 Timeline playback request。
- State/Effect 同步域第一阶段是否只做 spec 和输出容器，还是同步实现最小 `EffectInstanceId`。
- GameplayResult 同步域第一阶段是否只收集 result/digest，还是同时接入 hit query/result resolver。
