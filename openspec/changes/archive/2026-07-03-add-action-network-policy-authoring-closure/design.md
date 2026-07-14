# Design: Action 网络策略作者闭环

## 问题关键

这里要解决的不是“动作是不是一个特殊 tree”，也不是“节点是不是属于 Ability”。真正的问题是：一次动作在网络里是一段有生命周期的事务，作者必须能配置并检查这段事务的同步契约。

这条事务需要回答：

- 谁可以预测启动？
- 启动、取消、完成、打断如何同步？
- 哪些 window 只本地表现，哪些进入 combat history 或服务器裁决？
- motion 是 owner predicted、server correctable，还是纯远端插值？
- cue 是只本地播，还是服务器确认后广播？
- 命中、伤害、目标归属等 gameplay result 是否只能由服务器确认？
- Debug 时能不能从一次输入一路追到 outgoing packet 和 incoming correction？

## 作者心智模型

最终作者只需要理解五个层次：

1. `ActionProfile` 是动作的同步契约。
2. Graph 是逻辑编排层，负责提交 action request、持有 Action Context、提交 lifecycle transition。
3. Timeline 和普通 Graph 输出只产出窗口、运动、表现和结果事实，不保存完整网络策略。
4. Resolver 把 `ActionProfile + 输出事实` 解析成只读 effective policy。
5. Adapter 把符合策略的 SyncFacts 映射到 GameplaySync packet。

`ActionProfile` 不表示一棵树的结构归属。Graph 也不因为某段 subtree 被标记就自动变成 Ability body。动作身份来自 profile，运行时归属来自 Action Context，网络输出来自 SyncFacts。

## 数据流

```text
Input / AI / Network
-> Graph request submit
-> ActionRuntime creates ActionInstance + Action Context
-> Graph / Timeline / Stage emits windows, motion, cues, gameplay results
-> ActionNetworkPolicyResolver resolves effective policy
-> Character SyncFacts
-> CharacterGameplaySyncAdapter
-> GameplaySync packets
```

接收方向：

```text
GameplaySync incoming packets
-> CharacterGameplaySyncAdapter
-> CharacterNetworkReceiveStage
-> action decision / correction / result inputs
-> ActionRuntime / Motion / Presentation stages consume
```

## 核心组件

### ActionProfile Inspector

`ActionProfile` Inspector 是唯一完整策略编辑入口。它应该展示：

- Identity：action id、display name、debug category。
- Network Overview：prediction、authority、replication、correction。
- Windows：WindowType 到 authority/history/replication 的映射。
- Motion：MotionSourceType 到 prediction/correction/replication 的映射。
- Cues：CueType 或 CueId 到 playback/replication 的映射。
- Gameplay Result：result proposal、server confirmation、replication 的策略。
- Preview：本动作的 expected SyncFacts 和 packet domain。
- Debug：运行时实例入口和最近错误。

策略模板只用于创建时把字段显式写入 profile，例如“本地预测近战攻击”“服务端确认格挡”“只本地表现动作”。创建后 profile 字段就是正式配置，运行时不读模板，不做隐藏默认值。

### Effective Policy Resolver

Resolver 读取 `ActionProfile` 和输出事实，生成只读结果，例如：

- action activation 是否本地预测、是否需要服务器确认、如何复制。
- lifecycle transition 是否发送、发送到哪个 domain、是否进入历史。
- window 是否进入 ActionSyncDomain、是否只发送 digest、是否进入 combat rewind history。
- motion 是否进入 MotionSyncDomain、是否携带 ActionInstance 来源、是否允许 correction。
- cue 是否本地播放、预测播放、服务器确认播放或广播。
- gameplay result 是否允许客户端 proposal、是否只接受服务器 confirmed result。

Resolver 不修改 Graph、Timeline 或输出事实。缺少配置时应该给出配置错误或明确 warning，不得静默套用 fallback。

### Graph 和 Timeline 边界

Graph 节点可以做这些事：

- 选择 `ActionProfile` 提交 activation request。
- 输出 Action Context。
- 把 Action Context 传给 Timeline、窗口、motion、cue 或 result 输出。
- 在明确离开点提交 Complete、Cancel、Interrupt、Abort。

Graph 节点不应该做这些事：

- 配置 HitWindow authority。
- 配置 RootMotion correction。
- 配置 cue replication。
- 通过 subtree membership 表示网络同步范围。

Timeline clip 可以编辑窗口类型、窗口 id、时间和业务参数。非 Timeline 节点也可以产出同类 window sample。两者都通过 Action Context 归属到 ActionInstance，然后由 resolver 查策略。

### Adapter 边界

`CharacterGameplaySyncAdapter` 不拥有作者配置，也不直接读 Graph 或 Timeline。它应该消费本帧 `SyncFacts` 和 resolved policy，把事实映射到 GameplaySync packet。

这样 adapter 只关心网络协议和 SyncDomain：

- MotionSyncDomain：owner command、motion digest、correction。
- ActionSyncDomain：activation、decision、lifecycle、window digest/history。
- PresentationSyncDomain：需要同步的 cue。
- GameplayResultSyncDomain：命中、伤害、目标、PvE/objective result。

## 方案对比

### 方案 A：保留现状，只在 ActionProfile 里放 raw enum

优点：实现最少，字段已经存在。

缺点：作者看不出“这个动作到底会发什么”，调试时也无法解释 request、window、motion、packet 的关系。业务上不利于展示网络意识，后续接服务端时容易继续靠硬编码。

### 方案 B：在每个 Graph 节点和 Timeline clip 上配置网络策略

优点：所见即所得，单个 clip 很直观。

缺点：动作复用会产生重复配置，Timeline 被多个 ActionProfile 复用时会冲突，Graph 结构和网络语义重新耦合。业务上会让作者觉得“节点属于某个 Ability”，又回到之前混乱点。

### 方案 C：新增独立 NetworkPolicyAsset，再由 ActionProfile 引用

优点：网络策略和动作身份在资产层分离，长线可复用。

缺点：当前 demo 阶段会多一层资产跳转，作者需要在 ActionProfile、NetworkPolicyAsset、Timeline 三处来回查。业务上会让闭环更难展示，且现有字段迁移成本更高。

### 方案 D：ActionProfile 集中配置 + Resolver + Preview/Debug

优点：不恢复旧 ActionModule，不污染 Graph/Timeline，同时让作者能看到最终同步结果。业务上最贴合当前求职 demo：一个动作资产能解释预测、确认、回滚/纠正、远端复制和调试链路。

缺点：需要补 resolver、preview 和 debug UI。ActionProfile 会变成较重的策略中心，后续如果动作数量很多，Inspector 需要良好的折叠、过滤和校验。

本 change 选择方案 D。

## 和现有 spec 的关系

- 延续 `character-action-network-policy-authoring`：ActionProfile 仍是策略中心。
- 延续 `character-action-authoring-closure`：Graph 只提交 request 和 lifecycle，不保存完整网络策略。
- 延续 `character-gameplay-sync-adapter`：CharacterPipeline 只输出 SyncFacts，adapter 才映射 packet。
- 不替代 `refactor-character-motion-arbitration`：motion 仲裁怎么融合是另一个 change；这里只定义 motion fact 的同步策略如何配置和预览。

## 需要清理的误导点

- 不新增 `AbilityNodeTree`。
- 不把 subtree、StateNode、Timeline asset 当作动作身份。
- 不让 Timeline clip 保存完整网络策略。
- 不让 adapter 直接硬编码每种 action 输出的网络决策。
- 不让作者手敲 `attack.handle`、`ActionHandleSlot` 或等价内部 key。
