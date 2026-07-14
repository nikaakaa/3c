# Design: TimelineNode Inline/Shared 所有权与 Graph/Timeline 双窗口协作

## Context

当前 Timeline 同时承担两件事：

1. `ScriptableObject` 资产身份；
2. tracks、clips、scale、事件和运行采样所需的数据模型。

这导致 TimelineNode 无法像 StateMachineNode、StateNode 和 TreeClip 一样默认拥有普通 C# inline data。Unity `SerializeReference` 不能把 `ScriptableObject` 当作普通 managed-reference 数据可靠嵌入节点，因此仅在 TimelineNode 增加一个 `Timeline` 字段不能形成安全的 inline 模型。

编辑器也按数据类型分成两个窗口。Graph 的 `m_NavigationStack` 只保存 `BaseTree`，TimelineEditorWindow 只保存一个 Timeline asset；TreeClip 的 Open registry 只能另开 Tree 窗口。这使真实存在于 Timeline 的 TreeClip 在作者心智上像一条隐藏分支。

## Goals

- TimelineNode 默认私有 Timeline，不要求创建一次性资产。
- shared Timeline 是显式复用选择，不是默认保存方式。
- inline 与 shared 始终只有一个真数据来源。
- 作者可沿 State body、TimelineNode、Timeline、TreeClip 连续下钻和返回。
- runtime、preview、Agent 和 Debug 只消费同一 resolved TimelineData。
- 保持 TreeClip、Blackboard、Action Context、动画与网络事实合同不变。

## Non-Goals

- 不修改 Track/Clip 的业务种类和采样语义。
- 不恢复 ActionWindowTrack、ActionWindowClip 或专用 Window reader。
- 不修改 Timeline logic tick、presentation interpolation 或动画 transition 策略。
- 不让 Timeline 或 TreeClip 自动推导 ActionInstance。
- 不新增测试或 Unity batchmode 流程。

## Decisions

### 1. Timeline 数据与 asset 外壳分离

引入普通可序列化 `TimelineData` 作为 tracks、clips、scale、duration authoring 和 runtime clone 的唯一数据模型。`TimelineAsset : ScriptableObject` 只持有一份 `TimelineData`，负责 Project 文件、直接打开和显式复用。

业务取舍：私有 Timeline 可以真正跟随状态 body 生命周期，删除状态或 TimelineNode 时数据自然删除；代价是 Timeline editor 的 SerializedObject 绑定必须从“对象本身”改为“serialized owner + property path”。

### 2. TimelineNode 保存互斥的 inline/shared source

Timeline ownership module 保存：

- `[SerializeReference] TimelineData inlineTimeline`
- `TimelineAsset sharedTimelineAsset`

resolved source 只按 shared 优先或 inline 返回，但 validator 必须拒绝两者同时非空；正式编辑操作必须在切换时清理另一份数据，因此正常资产不会依赖优先级掩盖冲突。

新建 TimelineNode 自动创建命名后的空 inline TimelineData。`Extract Shared` 将 inline data 移入新 TimelineAsset 并清空 inline；选择已有 shared asset 清空 inline；`Use Inline` 克隆当前 shared data 到节点并清空 shared 引用。

业务取舍：默认创作不再制造 11 个一次性 Timeline 文件；代价是需要复用时多一次明确的 Extract Shared 操作，但复用意图与生命周期因此可见。

### 3. Runtime 只消费 resolved TimelineData 工作副本

TimelineNode 请求携带 resolved TimelineData 和稳定 source identity。TimelinePlaybackScheduler 为每个 request 深克隆独立工作副本，runtime time、Track binding、TreeClip runtime graph 和事件不得写回 authoring data。Preview session 使用相同 clone 服务。

旧 `Object.Instantiate(Timeline ScriptableObject)` 删除。clone 服务必须覆盖 Track/Clip managed references 和 nested TimelineRunningTree，不通过 JSON、字符串 YAML 或资产重载实现。

业务取舍：inline 与 shared 在 runtime 完全同构，同一 shared asset 的多个播放请求继续隔离；代价是需要正式 managed-reference clone 边界，不能再借 Unity Object.Instantiate 偷带资产身份。

### 4. TimelineData 绑定真实 serialized owner

TimelineData 保存非序列化的 owner object 与 property path。节点 inline Timeline 的 owner 是 RootTree 所在 BaseTreeAsset，路径落到 node module；shared Timeline 的 owner 是 TimelineAsset，路径落到其 data 字段。Track/Clip 初始化继续回指 TimelineData，TreeClip 再从 TimelineData owner path 派生 nested TimelineRunningTree path。

Undo、SerializedProperty、dirty 和 Save 都作用于 owner object。TimelineData 本身不获得 UnityEngine.Object 身份。

业务取舍：编辑操作仍能使用 Unity 原生 Undo/序列化，但所有路径必须稳定维护；路径断裂直接报错，不能把修改同时写到临时对象和 owner。

### 5. Graph 页面栈只保存 Graph 页面

`BaseTreeWindow` 的 editor-only page entry 只保存 Graph page 与 TreeClip resolved Graph page。TimelineData 不进入该栈，也不出现在 Graph breadcrumb。entry 继续保存显示名、来源 node/clip identity、serialized owner/path、page kind 和 authoring context，不序列化到业务数据。

业务取舍：Graph 的 Back、breadcrumb、selection 与 Blackboard 可见来源保持单一图语义；作者能把 Timeline 窗口与 Graph 窗口同时摆在屏幕上。代价是两个窗口需要正式传递来源 context，而不是依靠同一栈隐式继承。

### 6. TimelineNode 打开独立 TimelineEditorWindow

从 Graph 打开 TimelineNode：

```text
Graph Window: State Body Graph 保持不变
Timeline Window: 绑定 TimelineNode resolved TimelineData
```

TimelineEditorWindow 接收 TimelineData、serialized owner/path、ownership、来源 TimelineNode identity、来源 BaseTreeWindow 和 authoring context。TimelineEditorView 继续承载 field、track hierarchy、clip inspector 与 preview session；窗口重绑时必须释放旧 preview owner，不创建第二个隐式 session。

直接打开 shared TimelineAsset 时进入同一个 TimelineEditorWindow，但不伪造 Character context。依赖角色 declaration 的 TreeClip 在缺少来源 context 时必须显示缺口，不按 key 搜索或创建 fallback declaration。

业务取舍：Inline 与 Shared 使用同一个 Timeline 编辑宿主，Graph 和 Timeline 可同时观察；代价是窗口重绑、domain reload 和来源窗口关闭需要明确生命周期。

### 7. TreeClip 从 Timeline 窗口请求 Graph 下钻

从 TimelineEditorWindow 打开 TreeClip：

```text
Timeline Window: 保持当前 Timeline 与时间位置
Graph Window: push TreeClip resolved TimelineRunningTree
```

优先使用打开 TimelineNode 时记录的来源 BaseTreeWindow；直接打开 shared TimelineAsset 或来源窗口已关闭时，使用正式 TreeWindowUtility 获取 Graph 窗口。Graph 窗口接收 Timeline 窗口保存的 authoring context，使 TreeClip 看见 Character Root declarations；它只引用 declaration identity，不复制 declaration。

业务取舍：两个窗口形成显式协作而不是共享 breadcrumb；Timeline preview 生命周期归 Timeline 窗口，Graph page 生命周期归 BaseTreeWindow，互不替代。

### 8. Agent authoring 显式表达 ownership

Agent Patch 的 TimelineNode 操作增加 ownership：默认 `Inline`，可显式 `Shared`。Inline 创建可以从 template TimelineAsset 克隆初始内容，但 template path 只是编译期输入，结果不保留 runtime asset 引用。Shared 模式才保存 shared asset identity。

Snapshot 输出 node path、ownership、resolved timeline name、tracks/clips 和 shared asset path（仅 Shared）。TreeClip summary 归属到对应 TimelineNode path，不再依赖全局 timeline asset 列表定位。

业务取舍：Agent 生成结果与人工 UI 使用同一 ownership 模型；代价是旧 timelineAssetPath patch 不能继续表示默认绑定，必须迁移 schema，不保留兼容解释。

### 9. Corin 11 个 Timeline 全部迁入节点

当前项目只有 11 个 Timeline 资产，且全部属于 Corin。Agent snapshot 显示每个状态 TimelineNode 都引用唯一资产，没有共享引用。因此全部迁入对应节点 inline data：

- Locomotion：Idle、WalkStart、WalkLoop、RunStart、RunLoop、RunEnd、MovingTurn；
- Action：Attack1、Attack2、DodgeForward、DodgeBack。

迁移保持 8 个现有 Decision TreeClip 及其 inline TimelineRunningTree、Blackboard references、owner identity、frame range 和 phase。确认无剩余引用后删除 11 个外部 Timeline 资产和空目录。

业务取舍：Corin 资产层级直接表达真实 ownership，RootTree 成为完整角色作者入口；代价是 RootTree asset 体积增大，但这是私有状态 body 数据集中后的预期结果，不是重复数据。

## Alternatives Considered

### 方案 A：只修 TimelineEditor 显示，不改变 TimelineNode asset 引用

优点是代码和资产迁移最少。缺点是每个状态仍需要一次性 Timeline asset，作者链路仍在节点与外部资产之间跳转，不能满足 inline-first ownership，因此不采用。

### 方案 B：把每个私有 Timeline 保存为 Unity sub-asset

优点是可以继续使用 ScriptableObject 和 Object.Instantiate。缺点是 private data 生命周期、Undo、删除和复制重新依赖隐藏 asset identity，与 Graph 已完成的普通 C# inline data 方向相反，因此不采用。

### 方案 C：把 Timeline page 放入 BaseTreeWindow 异构页面栈

优点是来源路径可以放进同一 breadcrumb。缺点是 Graph 与 Timeline 互相替换，作者无法同时观察状态结构和时间结构；Timeline preview 与 Graph navigation 也被错误耦合，因此不采用。

### 方案 D：TimelineNode 同时保存 inline data 和 asset，运行时按优先级选择

优点是迁移期间容易兼容旧资产。缺点是两个真数据来源会产生隐蔽覆盖和修改丢失，违反项目禁止 fallback/兼容路径的规则，因此不采用。

## Risks And Mitigations

- **RootTree managed-reference 体积扩大**：只迁移一对一私有 Timeline；真正复用的数据显式 Extract Shared。
- **nested TreeClip 序列化路径断裂**：统一由 TimelineData owner binder 计算路径，validator 校验每个 TreeClip inline graph 的 owner/path；无法安全绑定时停止 apply。
- **Undo 修改错误 owner**：TimelineEditorWindow 绑定时必须保存 TimelineData 的 serialized owner/path，所有修改继续直接作用于真实 owner，不允许窗口创建镜像 asset。
- **shared Timeline 的角色变量断链**：保留 declaration owner identity 严格校验；不按 key fallback。
- **runtime clone 污染 authoring data**：scheduler 与 preview 只持有 clone，validator/debug 显示 source ownership 与 runtime identity。
- **active change 归档覆盖新口径**：先归档依赖 changes，再归档本 change；实施时同步检查相关 delta 最终合并文本。

## Migration

1. 固定 Timeline、TimelineNode、TreeClip、preview、Agent 和页面栈现状。
2. 建立 TimelineData 与 TimelineAsset 单一数据模型。
3. 建立 TimelineNode inline/shared ownership 和正式编辑操作。
4. 将 scheduler、preview 和 Track/Clip 改为消费 TimelineData clone。
5. 建立 TimelineData serialized owner/path 绑定。
6. 保持 Graph/TreeClip 页面栈并建立独立 TimelineEditorWindow 宿主。
7. 接通 TimelineNode -> TimelineEditorWindow 与 TimelineEditorWindow -> TreeClip Graph page。
8. 更新 Inspector、Agent、snapshot 和 validator。
9. 原子迁移 Corin 11 个 Timeline 并删除外部资产。
10. 删除旧 TimelineReferenceModule、Timeline external page 和 asset-only authoring 路径。
11. 更新 project context 与冲突 specs。

## Open Questions

无阻塞业务决策。shared TimelineAsset 继续允许直接打开和复用，但角色黑板可见性只来自实际打开来源或显式业务 authoring context。
