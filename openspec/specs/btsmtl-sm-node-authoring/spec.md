# btsmtl-sm-node-authoring Specification

## Purpose
定义 BTSMTL 状态机创作链路：普通行为图通过 `StateMachineNode` 进入 `StateMachineGraph`；创建 `StateMachineNode` 时默认自动拥有并绑定私有 inline `StateMachineGraph` 数据，用户不需要先手动创建或拖拽状态机资产；`StateMachineGraph` 只表达状态关系；`StateNode` 和 Transition edge 是状态机图内联数据；状态具体行为在 `StateNode` resolved `SubTree` 或 `StateBehaviorSubTree` 中编辑。
## Requirements

### Requirement: 状态机层级角色分离

系统 MUST 使用 `StateMachineNode` 表达父级行为图进入状态机图的入口，使用 `StateMachineGraph` 表达同层状态结构，使用 `StateNode` 表达状态机图内普通状态。`StateMachineGraph` MUST NOT 直接包含另一个 `StateMachineNode`；需要嵌套状态机时，作者 MUST 在某个 StateNode 的 resolved `SubTree` 或 `StateBehaviorSubTree` 行为图中创建普通 `StateMachineNode`。每个 StateMachineNode 默认拥有的 StateMachineGraph MUST 是该节点内部的普通 C# inline graph data，只有显式复用时才可提升为 shared asset。

#### Scenario: 状态行为创建嵌套状态机

- **WHEN** 用户在 `Attack` StateNode 的 inline StateBehaviorSubTree Root 流程中创建状态机入口
- **THEN** 创建结果 MUST 是普通 `StateMachineNode`
- **AND** 编辑器 MUST 自动创建并绑定 inline `StateMachineGraph`
- **AND** 用户 MUST 能继续下钻编辑 Attack1、Attack2 与 Exit
- **AND** 系统 MUST NOT 创建 `AttackStateMachineNode` 或一次性 StateMachineGraph asset

#### Scenario: 状态机图拒绝直接嵌套节点

- **WHEN** 用户尝试在 StateMachineGraph 同层创建 StateMachineNode
- **THEN** `CanCreateNodeType` 和 validation MUST 拒绝该结构
- **AND** UI MUST 引导作者从某个 StateNode 的行为图继续下钻

### Requirement: StateMachineGraph 只表达同层状态结构
系统 MUST让`StateMachineGraph`只包含`Enter`、`AnyState`、`Exit`、`StateNode`、状态Transition及其逻辑调度/条件数据。它 MUST NOT保存Animation Layer、strategy、duration、curve、Presentation binding、animation owner或可见动画后代状态。Transition条件继续下钻到resolved `ConditionRuleGraph`。

#### Scenario: 打开状态机图

- **WHEN** 作者打开Locomotion或Action StateMachineGraph
- **THEN** 图 MUST只显示状态拓扑、priority、condition和interruption
- **AND** 图资产 MUST不包含角色动画表现配置

#### Scenario: 非角色状态机运行

- **WHEN** StateMachineGraph在没有Character Animation模块的上下文执行
- **THEN** 状态切换和通用Tree lifecycle MUST正常工作
- **AND** runtime MUST不要求Animation Layer或Driver binding

### Requirement: StateNode 下钻状态行为 SubTree
系统 MUST 允许 `StateNode` 通过正式状态行为 graph reference 拥有默认 inline 状态行为图数据，或显式引用 shared `SubTree` / `StateBehaviorSubTree` asset。默认创建状态行为图时 MUST 创建普通 C# 内联图数据并自动绑定。`StateNode` MUST NOT 在 `StateMachineGraph` 本层暴露 `Behavior` flow port，也 MUST NOT 直接引用子 `StateMachineGraph`。

#### Scenario: State 下钻到行为图
- **WHEN** 用户打开 Idle `StateNode` 的状态行为引用
- **THEN** 编辑器 MUST 打开该 StateNode resolved 状态行为图
- **AND** 用户 MUST 能在该图中创建 Timeline、Action、Composite、Decorator、Tree 引用或嵌套 `StateMachineNode`

#### Scenario: 创建私有状态行为图
- **WHEN** 用户从 `StateNode` 创建状态行为图
- **THEN** 系统 MUST 默认创建 inline `SubTree` 或 `StateBehaviorSubTree` graph data
- **AND** 系统 MUST 自动绑定到该 `StateNode`
- **AND** 用户 MUST NOT 被要求先手动创建、保存或拖拽一个 tree asset

#### Scenario: 显式复用状态行为图
- **WHEN** 多个 `StateNode` 需要复用同一份状态行为
- **THEN** 用户 MUST 显式创建、抽取或分配 shared tree asset
- **AND** 切换到 shared asset 后 MUST 清理该 StateNode 的 inline 状态行为真数据

#### Scenario: 没有状态行为
- **WHEN** active `StateNode` 没有配置状态行为图
- **THEN** 该状态 MUST 保持 `Running`
- **AND** 状态切换 MUST 继续由同层 Transition 决定

### Requirement: 状态机创作 UI 遵守 inline-first 心智
系统 MUST 让 `StateMachineNode`、`StateNode` 和 Transition 的默认 UI 操作与 inline-first 数据模型一致。默认创建必须可立即下钻；左侧 Inspector 负责查看和显式切换复用状态；普通创建路径 MUST NOT 暴露“先创建内部 graph”的旧心智。

#### Scenario: StateMachineNode 默认 UI
- **WHEN** 用户选中 `StateMachineNode`
- **THEN** Inspector MUST 显示状态机引用 ownership
- **AND** 用户 MUST 能通过 `Open` 或双击进入 resolved `StateMachineGraph`
- **AND** 节点画布本体 MUST NOT 因 `Shared Graph` 字段暴露而强制显示配置齿轮

#### Scenario: StateNode 默认 UI
- **WHEN** 用户选中 `StateNode`
- **THEN** Inspector MUST 显示状态行为引用 ownership
- **AND** 用户 MUST 能通过 `Open` 或双击进入 resolved `SubTree` / `StateBehaviorSubTree`
- **AND** shared 状态行为 asset 只能作为显式复用配置

#### Scenario: Transition Rule UI
- **WHEN** 用户选中 StateMachine Transition edge
- **THEN** Inspector MUST 显示 priority、ownership、shared rule asset 和 rule graph 操作
- **AND** 已有 rule graph 时 `Open Rule` MUST 是主操作
- **AND** 合法 Transition MUST NOT 显示 `Create Rule` 或等价创建按钮
- **AND** 新建合法 Transition MUST 在创建事务内已拥有 inline rule graph
- **AND** 已落盘的缺失或 invalid rule graph MUST 显示配置错误，不得由打开、刷新、校验或 `CheckInit()` 自动补齐

### Requirement: StateBehaviorSubTree 提供状态生命周期入口

系统 MUST 让普通 `SubTree` 只表达 `RootNode` 行为入口。`StateBehaviorSubTree` MUST 固定拥有 `OnEnter`、`RootNode` 和 `OnExit` 生命周期入口。`OnEnter` 和 `OnExit` MUST 使用普通 `RunnableNode` flow 链路，MUST NOT 成为 `StateMachineGraph` Transition 端点。State Transition 或父 Tree graceful stop 离开 active State 时 MUST 先停止 Root，再在当前 StateExitContext scope 内运行 OnExit。

#### Scenario: 普通 SubTree

- **WHEN** 用户创建普通 `SubTree`
- **THEN** 新图 MUST 默认包含一个 `RootNode`
- **AND** 新图 MUST NOT 默认包含 `OnEnter` 或 `OnExit`

#### Scenario: StateBehaviorSubTree

- **WHEN** 用户创建 `StateBehaviorSubTree`
- **THEN** 新图 MUST 默认包含一个 `OnEnter`、一个 `RootNode` 和一个 `OnExit`
- **AND** 缺失或重复生命周期入口 MUST 被校验报告为非法结构

#### Scenario: State Transition 离开状态

- **WHEN** active StateNode 通过同层 Transition 离开
- **THEN** runtime MUST 先停止 State Root
- **AND** MUST 在 target State 进入前运行 OnExit

#### Scenario: Parent Tree abort StateMachineNode

- **WHEN** StateMachineNode 收到 Self、LowerPriority 或 Parent graceful stop
- **THEN** runtime MUST 先停止 active State Root
- **AND** MUST 在 SMNode StopCompleted 前运行 OnExit
- **AND** OnExit MUST 能读取对应 StateExitContext

### Requirement: Transition 是同层 BaseEdge 语义

系统 MUST 将状态转换表达为 `StateMachineGraph` 内联保存的 `BaseEdge`，MUST NOT 新增 `TransitionNode`，也 MUST NOT 为 Transition 本体创建 asset。Transition MUST 只连接同层 `Enter`、`AnyState`、`StateNode` 和 `Exit`。Transition 条件默认 MUST 是该 edge 内部的 inline `ConditionRuleGraph` 数据；需要复用时才显式绑定 shared `ConditionRuleGraph` asset。每个 edge MUST 保存正式 ConditionRuleGraph ownership，系统 MUST NOT 根据 shared asset 是否可解析来猜测或改写 owner 来源。

#### Scenario: 合法端点

- **WHEN** 用户连接 Transition flow
- **THEN** 合法连接 MUST 是 `Enter -> StateNode`、`AnyState -> StateNode|Exit`、`StateNode -> StateNode|Exit`
- **AND** `RootNode`、`StateMachineNode`、普通 `RunnableNode`、`TimelineNode`、Tree 节点和 `ValueNode` MUST NOT 成为 Transition 端点

#### Scenario: 条件和优先级

- **WHEN** Transition 配置条件
- **THEN** 条件 MUST 通过该 Transition resolved `ConditionRuleGraph` 表达
- **AND** 创建合法 Transition edge 时 MUST 立即创建该 edge 内部的 inline `ConditionRuleGraph`
- **AND** 默认规则图 MUST 是该 edge 内部的 inline graph data
- **AND** 默认规则图在未连接条件时 MUST 允许 Transition 通过
- **AND** `AnyState` Transition MUST 配置规则图条件
- **AND** runtime MUST 按 Transition 优先级选择可通过的边

#### Scenario: Transition 显式复用规则图

- **WHEN** 多条 Transition 需要复用同一套规则
- **THEN** 用户 MUST 显式抽取或分配 shared `ConditionRuleGraph` asset
- **AND** 删除 Transition 时 MUST 只断开 shared 引用，不删除 shared asset
- **AND** 切换到 shared asset 后 MUST 清理该 Transition 的 inline rule graph 真数据

#### Scenario: Shared ConditionRuleGraph asset 被删除

- **WHEN** Transition 配置为 Shared ownership，但其 `ConditionRuleGraph` asset 被删除、类型错误或无法解析
- **THEN** 编辑器、validator 与 runtime MUST 保留该 Shared ownership 错误并报告 edge 与 owner
- **AND** 编辑器 MUST NOT 清理 shared 引用、创建 inline 图或把 Transition 当作无条件边
- **AND** runtime MUST 使该 Transition 条件失败
- **AND** 作者只能显式替换 shared asset 或执行 Use Inline 才能恢复该 Transition

### Requirement: 状态机运行时解释

系统 MUST 让 `StateMachineNode` 驱动自己 resolved `StateMachineGraph` 数据，并让 `StateMachineGraphRuntime` 以 `StateNode` 作为 active state。解释器 MUST 从 `Enter` 读取初始 Transition，并在每帧写入当前运行工作副本的 `BaseGraph.DeltaTime`。运行时 MUST 从 inline 或 shared authoring graph data 创建隔离工作副本。状态 root 完成 MUST 是可查询事实而不是所有 Transition 的隐式前置条件。State Transition 和父 Tree graceful stop MUST 复用统一 source-exit 内核。

#### Scenario: 父级 tick 状态机入口

- **WHEN** 父级行为图 tick 到 `StateMachineNode`
- **THEN** `StateMachineNode` MUST 进入 resolved `StateMachineGraph` 运行工作副本
- **AND** 父级行为图 MUST 只看到该节点的 `Running/Success/Failure`

#### Scenario: active state tick

- **WHEN** 状态机已有 active state
- **THEN** 本帧 MUST 只 tick active StateNode 状态行为工作副本
- **AND** 其它状态 MUST NOT 被 tick，除非 Transition 切换 active state

#### Scenario: AnyState 和 Exit

- **WHEN** 状态机已有 active state
- **THEN** runtime MUST 在 tick active state 前检查 `AnyState` Transition
- **AND** 命中 `Exit` MUST 让本层状态机返回 `Success`

#### Scenario: 未完成状态被 Transition 抢占

- **WHEN** active State root 仍为 Running
- **AND** 某条出边 ConditionRuleGraph 返回 true
- **THEN** runtime MUST 按 priority 和 flow order 选择 Transition
- **AND** MUST NOT 隐式等待 StateRootCompleted

#### Scenario: 父 Tree graceful stop

- **WHEN** StateMachineNode 收到 graceful stop request
- **THEN** runtime MUST 请求没有 target 的 active State exit
- **AND** OnExit Running 时 SMNode stop status MUST 保持 Running
- **AND** OnExit 完成后 MUST 发布 owner release 并 StopCompleted

#### Scenario: ForceStop

- **WHEN** StateMachineNode 因 Shutdown、Dispose 或强制 Reset 被 ForceStop
- **THEN** runtime MUST 立即停止 active State 和释放 owner
- **AND** MUST NOT 运行 gameplay OnExit 或进入 target State

### Requirement: Timeline 和输入通过正式状态行为链路接入
系统 MUST NOT 将 `TimelineNode` 或 InputAction 节点作为 `StateMachineGraph` 同层状态或 Transition flow 端点。Timeline MUST 通过 `StateNode` 下钻的状态行为 `SubTree` 接入；InputAction 值 MUST 在 `ConditionRuleGraph` 中作为条件输入接入。

#### Scenario: Idle 播放 Timeline
- **WHEN** Idle 状态需要播放 Timeline
- **THEN** 用户 MUST 在 Idle 引用的状态行为 `SubTree` 中创建 `TimelineNode`
- **AND** `TimelineNode` MUST 从所在运行 Graph 获得 `Owner.DeltaTime`

#### Scenario: 输入驱动 Transition
- **WHEN** 用户需要用 InputAction Bool 驱动 Transition
- **THEN** 用户 MUST 在该 Transition 的 `ConditionRuleGraph` 中创建 InputAction Bool 输入节点
- **AND** 该输入节点 MUST NOT 出现在 `StateMachineGraph` 本层合法 Transition flow 端点候选中

### Requirement: 不保留旧特化数据路径
系统 MUST 不依赖旧 Locomotion、Action、FootPhase、BodyClaim 或 AnimationPresentationPolicy SO/config 数据来表达状态机创作语义。

#### Scenario: 旧数据存在
- **WHEN** 项目中发现旧状态、动作、FootPhase 或表现配置数据
- **THEN** 当前状态机能力 MUST NOT 读取该数据
- **AND** 该数据 MUST 迁移到 Graph、NodeModule 或 Timeline 轨道后删除

### Requirement: Transition 调度元数据留在边上
Transition 的优先级和同优先级稳定排序 MUST 属于边调度数据。规则图 MUST NOT 负责选择其它 Transition，也 MUST NOT 保存 priority、tag 或 trigger 的调度排序逻辑。

#### Scenario: 多条 Transition 同时成立
- **WHEN** 同一来源节点存在多条规则图返回 true 的 Transition
- **THEN** runtime MUST 先按 Transition 优先级选择
- **AND** 优先级相同 MUST 再按 flow order 保持稳定顺序

#### Scenario: tag 或 fact 条件
- **WHEN** Transition 需要 tag、fact、输入或黑板变量参与判断
- **THEN** 这些数据 MUST 通过规则图内的读取或谓词节点表达
- **AND** Transition 边 MUST NOT 为每类业务数据新增专用条件字段

### Requirement: Transition Rule 编辑入口属于 Transition 边

编辑器 MUST 允许用户从 Transition 边打开和查看条件图。边视图 MUST 显示优先级、持久化 ownership、resolved 状态和规则摘要。默认私有规则图 MUST 作为 Transition edge 内部 inline graph data 保存，需要复用时才显式抽取或分配 shared `ConditionRuleGraph` asset。打开、刷新和校验 MUST NOT 改写已落盘 edge 的 ownership 或生成替代规则图。

#### Scenario: 打开 resolved 规则图

- **WHEN** 用户双击 Transition 边或点击边 Inspector 的 `Open Rule`
- **AND** 该 Transition 边拥有与 ownership 匹配的 resolved `ConditionRuleGraph`
- **THEN** 编辑器 MUST 打开该 resolved `ConditionRuleGraph`
- **AND** 页面栈 MAY 记录来源边，但 MUST NOT 将页面栈写入图数据

#### Scenario: 打开 invalid Transition rule

- **WHEN** 用户尝试打开 ownership 为 Unspecified、Shared asset 缺失、类型错误、Inline 数据缺失或 inline/shared 双持有的 Transition
- **THEN** 编辑器 MUST 显示 edge、owner、ownership 和错误原因
- **AND** 编辑器 MUST NOT 创建 inline `ConditionRuleGraph`、清理 shared 引用或把该 Transition 当作无条件边

#### Scenario: 作者显式切换到 Inline

- **WHEN** 作者在 invalid 或 Shared Transition 上执行 `Use Inline Rule`
- **THEN** 编辑器 MUST 创建新的 edge 内部 inline `ConditionRuleGraph`
- **AND** edge MUST 写入 Inline ownership 并清理 shared 真数据
- **AND** 默认规则图 MUST 包含默认通过的规则输出入口

#### Scenario: 作者显式替换 Shared

- **WHEN** 作者为 Shared Transition 选择另一份有效 `ConditionRuleGraph` asset
- **THEN** edge MUST 保持 Shared ownership 并保存新的 shared 引用
- **AND** edge MUST NOT 保留 inline 规则图真数据

#### Scenario: 删除带规则图的 Transition

- **WHEN** 用户删除拥有 inline 规则图的 Transition 边
- **THEN** inline 规则图 MUST 随 Transition 边序列化数据一起删除
- **AND** 系统 MUST NOT 执行 subasset 删除

#### Scenario: 删除引用 shared 规则图的 Transition

- **WHEN** 用户删除引用 shared asset 规则图的 Transition 边
- **THEN** 系统 MUST 只删除 Transition 边并断开引用
- **AND** 系统 MUST NOT 删除 shared asset

### Requirement: 旧 BoolPort 条件链路必须删除
系统 MUST 删除旧 `TransitionConditionNodeGuid/PortId` 条件链路和同图 Bool port 条件菜单。可迁移的旧条件 MUST 迁移为规则图；不可迁移的旧条件 MUST 被报告为非法结构，不得 fallback。

#### Scenario: 旧条件字段存在
- **WHEN** 旧资产或代码路径仍保存 Transition BoolPort 条件引用
- **THEN** 迁移或清理路径 MUST 将其移除
- **AND** runtime MUST NOT 再读取该旧字段决定 Transition

### Requirement: 状态机默认图数据初始化
系统 MUST 在创建 `StateMachineNode` 时初始化一份可立即下钻编辑的 inline `StateMachineGraph` 数据。默认图必须提供状态机闭环所需的最小结构。

#### Scenario: 创建默认状态机图
- **WHEN** 用户创建 `StateMachineNode`
- **THEN** inline `StateMachineGraph` MUST 默认包含一个 `Enter`、一个 `AnyState`、一个 `Exit` 和一个 `StateNode`
- **AND** `Enter` MUST 默认连接到该 `StateNode`
- **AND** 这些控制节点和状态节点 MUST 保存为 inline graph data

#### Scenario: 不依赖已保存 asset
- **WHEN** owner graph asset 尚未保存到磁盘
- **THEN** `StateMachineNode` 创建仍 MUST 能生成 inline `StateMachineGraph` 数据
- **AND** 创建流程 MUST NOT 调用 subasset 创建 API

### Requirement: Transition Rule 默认图数据初始化

系统 MUST 只在创建合法 Transition 或作者显式执行 `Use Inline Rule` 时初始化一份可立即下钻编辑的 inline `ConditionRuleGraph` 数据。规则图是条件求值图，不是普通行为树。编辑器 MUST NOT 通过打开、刷新、校验或 `CheckInit()` 修复已落盘的 invalid edge。

#### Scenario: 创建默认 Transition rule

- **WHEN** 用户连接合法 Transition
- **THEN** 系统 MUST 创建 inline `ConditionRuleGraph` 数据并写入 Inline ownership
- **AND** 默认规则图 MUST 包含规则输出入口
- **AND** 默认规则图在未连接条件时 MUST 允许 Transition 通过
- **AND** 用户 MUST 能立即下钻编辑条件节点和属性连线
- **AND** 创建流程 MUST NOT 创建 subasset

#### Scenario: 已落盘规则图缺失

- **WHEN** 编辑器打开、刷新或校验一个 ownership 与实际数据不匹配的 Transition
- **THEN** 系统 MUST 保留该 invalid 状态并报告错误
- **AND** 系统 MUST NOT 自动生成 inline 图、复制 shared 图或清除断裂引用

### Requirement: StateMachine runtime 必须暴露状态运行事实

系统 MUST 在 `StateMachineGraphRuntime` 的运行工作副本中维护当前 active state 的运行事实。运行事实至少 MUST 包含 active state identity、进入状态后的 elapsed ticks、elapsed seconds、状态 root 上次返回状态和状态 root 是否完成。运行事实 MUST 属于 runtime working copy，MUST NOT 写回 authoring graph data。

#### Scenario: RunStart 状态运行中

- **WHEN** `RunStart` 是当前 active `StateNode`
- **THEN** runtime MUST 能报告 `RunStart` 的 elapsed seconds 和 elapsed ticks
- **AND** runtime MUST 能报告该状态行为 root 最近一次返回 `Running`、`Success` 或 `Failure`
- **AND** authoring graph asset MUST NOT 因这些 runtime 值变脏

#### Scenario: 状态切换

- **WHEN** 状态机从 `WalkStart` 切换到 `WalkLoop`
- **THEN** runtime MUST 重置 active state elapsed 计数
- **AND** runtime MUST 将 active state identity 更新为 `WalkLoop`
- **AND** 旧状态的运行事实 MUST NOT 被新状态 transition rule 当作当前状态事实读取

### Requirement: ConditionRuleGraph 必须能读取当前状态运行事实
系统 MUST 提供正式 value node 或等价只读接口，让状态机 Transition 使用的 `ConditionRuleGraph` 能读取当前 `StateMachineGraphRuntime` 的状态运行事实。该能力 MUST 保持条件图的纯条件求值语义，MUST NOT tick 状态行为 SubTree、Timeline 或 Action 节点。

#### Scenario: Start 状态完成后切换 Loop
- **WHEN** 作者配置 `RunStart -> RunLoop`
- **THEN** `ConditionRuleGraph` MUST 能读取 `StateRootCompleted`
- **AND** `ConditionRuleGraph` MUST 能组合 `MoveMagnitude >= RunThreshold`
- **AND** runtime MUST 只在两者都成立时允许 transition

#### Scenario: End 状态完成后回 Idle
- **WHEN** 作者配置 `RunEnd -> Idle`
- **THEN** `ConditionRuleGraph` MUST 能读取 `StateRootCompleted`
- **AND** transition rule MUST NOT 通过 Timeline asset membership 或节点路径判断完成

### Requirement: StateBehaviorSubTree root 完成不自动退出状态

系统 MUST 区分状态行为 root 完成和状态离开。`StateBehaviorSubTree` 的 Root 返回 `Success` MAY 被记录为 `StateRootCompleted`，但 `StateNode` MUST 继续保持 active state，直到同层 Transition 明确切换到其它状态或 Exit。

#### Scenario: RunStart Timeline 播放完成

- **WHEN** `RunStart` 状态行为中的 TimelineNode 返回 `Success`
- **THEN** runtime MUST 将当前状态 root 标记为 completed
- **AND** `RunStart` MUST NOT 因 root completed 自动离开
- **AND** 只有 `RunStart -> RunLoop` transition 条件成立时才切换状态

#### Scenario: Idle 状态没有离开条件

- **WHEN** `Idle` 状态 root 行为返回 `Success`
- **THEN** `Idle` MUST 保持 active
- **AND** 状态机 MUST 等待 transition rule 决定是否离开

### Requirement: Transition Rule 黑板读取必须保持纯条件图语义

系统 MUST 让 ConditionRuleGraph 中的 blackboard/exposed 读取保持纯 ValueNode 语义。读取节点 MUST 只输出值，不拥有 RunnableNode 生命周期、flow 输入、Timeline 播放、Action 提交或状态行为 graph 引用。

#### Scenario: 创建黑板读取节点

- **WHEN** 作者在 ConditionRuleGraph 中创建 blackboard float 读取节点
- **THEN** 该节点 MUST 被 `ConditionRuleGraph.CanCreateNodeType()` 接受
- **AND** 节点 MUST 能通过 PropertyPort 连接到 Compare、And、Or 或 ConditionRuleResultNode
- **AND** 节点 MUST NOT 创建 flow edge

#### Scenario: 拒绝 Runnable ExposedPropertyNode

- **WHEN** 作者或脚本尝试把 Runnable `ExposedPropertyNode` 放入 ConditionRuleGraph
- **THEN** graph creation 或 validation MUST 拒绝该节点
- **AND** 系统 MUST 提示使用纯 ValueNode blackboard 读取节点

### Requirement: Transition Rule 条件必须由输入、黑板值和逻辑节点组合表达

状态机 Transition 的业务条件 MUST 通过输入 ValueNode、blackboard ValueNode、Compare、And、Or、Not 和 ConditionRuleResultNode 等纯节点组合表达。系统 MUST NOT 为每个 Corin locomotion 分支长期保留业务特化条件节点。

#### Scenario: Idle 到 WalkStart

- **WHEN** Idle 到 WalkStart 需要判断移动输入超过走路阈值
- **THEN** 规则图 MUST 读取 MoveAxis 派生幅度和 `WalkThreshold`
- **AND** CompareNode MUST 输出是否超过阈值
- **AND** ConditionRuleResultNode MUST 接收最终 Bool

#### Scenario: WalkLoop 到 RunStart

- **WHEN** WalkLoop 到 RunStart 需要判断输入超过跑步阈值
- **THEN** 规则图 MUST 读取同一套输入派生值和 `RunThreshold`
- **AND** 条件组合 MUST 不依赖专用 `IsRunInput` 业务节点

### Requirement: BTSMTL StateMachine Editor 必须排除动画表现 authoring

Tree Inspector、EdgeView、StateMachineNode Inspector和Graph context menu MUST只编辑StateMachine逻辑结构、priority、ConditionRuleGraph、interruption和ownership。它们 MUST NOT显示或写入animation strategy、duration、curve、external animation exit、Driver binding或Animation Layer。

#### Scenario: 选择Transition edge

- **WHEN** 作者选择StateMachine Transition edge
- **THEN** Inspector MUST显示From、To、priority、rule ownership和condition摘要
- **AND** Inspector MUST不显示动画表现字段

#### Scenario: 选择StateMachineNode

- **WHEN** 作者选择StateMachineNode
- **THEN** Inspector MUST只显示Graph ownership与逻辑authoring内容
- **AND** Inspector MUST不显示External Exit Animation配置

### Requirement: StateMachine runtime 必须向作用域服务发布完整状态激活身份

StateMachineGraphRuntime MUST使用完整 StateMachineExecutionScope(RuntimeId, StateId, ActivationGeneration) 维护 State Blackboard、状态运行事实与 nested execution path。该 scope 只属于状态逻辑，不得复制成动画 owner、animation ready、Tree animation activation 或 presentation lineage。

#### Scenario: target 首次执行

- **WHEN** target StateNode 首次获得正式 state body update
- **THEN** State 事实 MUST记录该 State execution scope 已执行
- **AND** 系统 MUST不从该 executed 事实推导动画已采样或动画可切换

#### Scenario: 同一状态再次进入

- **WHEN** StateMachine 离开 Attack1 后再次进入 Attack1
- **THEN** StateMachineExecutionScope MUST使用新的 activation generation
- **AND** 旧 activation 的 State Blackboard 数据 MUST不泄漏到新 activation

#### Scenario: Transition rule 求值

- **WHEN** runtime 求值当前 active State 的 ConditionRuleGraph
- **THEN** evaluation context MUST携带当前完整 execution scope
- **AND** rule 中的 State scope variable MUST解析到该 activation

#### Scenario: ForceStop

- **WHEN** StateMachine runtime 被强制停止
- **THEN** scope service MUST收到目标 execution scope 的释放通知
- **AND** scope service MUST只清理该 execution scope 的逻辑 runtime data

### Requirement: StateMachine上层停止必须使用普通Runnable release链

parent Tree的Self、LowerPriority或ExplicitParentStop停止StateMachineNode时，StateMachineNode和全部active State descendants MUST复用通用graceful stop与Runnable Released事实。NodeStopContext MUST只携带结构cause和replacement provenance。系统 MUST不发布StateMachine专用external animation transition或读取external-exit Driver definition。

#### Scenario: parent replacement

- **WHEN** Selector LowerPriority replacement停止StateMachineNode
- **THEN** stop cause MUST沿StateMachineNode、active State和State body descendants保持不变
- **AND** 每层activation MUST按barrier顺序发布Released
- **AND** 动画策略 MUST由上层Presentation Adapter基于通用sites解析

### Requirement: 嵌套 StateMachine runtime 必须维护完整 execution path

系统 MUST为嵌套StateMachine维护outer-to-inner State execution path，同时让StateNode、StateBehaviorSubTree和内部Runnable descendants进入通用Tree activation parent chain。状态body update、OnExit、ConditionRuleGraph、Timeline request和Blackboard access MUST使用同一State path；Animation lineage MUST来自通用Runnable facts，BTSMTL path MUST不维护可见动画后代状态。

#### Scenario: Attack1 在 Attack 状态中运行

- **WHEN** 外层Action StateMachine的Attack active且内层Attack1 active
- **THEN** State path MUST包含outer Attack和inner Attack1 scope
- **AND** Runnable parent chain MUST连接外层State body、内层StateMachineNode、Attack1及其TimelineNode

#### Scenario: 嵌套 scope 栈不匹配

- **WHEN** runtime pop的State或Runnable frame不是当前最内层frame
- **THEN** runtime MUST报告明确stack mismatch
- **AND** MUST不静默删除其它active frame
