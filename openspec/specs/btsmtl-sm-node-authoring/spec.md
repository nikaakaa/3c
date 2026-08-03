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

Compiled StateMachine operation MUST在正式 decision、enter、update、exit barrier、complete 和 interruption 边界输出结构化 state lifecycle facts。Fact MUST包含 Program/source identity、ActorId、activation identity、execution path 和 SimulationTick，MUST不依赖 runtime clone reference。

#### Scenario: Transition 成立

- **WHEN** compiled ConditionRuleGraph 选中 Transition
- **THEN** Kernel MUST输出可反查 authoring edge 的 decision/exit/enter facts

### Requirement: ConditionRuleGraph 必须能读取当前状态运行事实

Compiled ConditionRuleGraph operation MUST通过只读 Operation Execution Context 读取当前 State execution path、StateRootCompleted 和 Blackboard state slots。Condition operation MUST保持纯条件求值，MUST不执行 State body、Timeline、Action 或 WorldSolver。

#### Scenario: RunStart 完成后切换 RunLoop

- **WHEN** RunStart StateRootCompleted 且 MoveMagnitude 达到阈值
- **THEN** compiled rule MUST只在两项条件都成立时选择 transition

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

CharacterSimulationState MUST使用由 Program handle、StateId、outer-to-inner execution path 与 ActivationGeneration 构成的稳定 State execution scope，维护 State Blackboard、状态运行事实和 nested path。该 scope MUST不复制成动画 owner、ready 或 presentation lineage。

#### Scenario: 同一 State 再次进入

- **WHEN** StateMachine 离开 Attack1 后再次进入 Attack1
- **THEN** MUST分配新的 ActivationGeneration
- **AND** 旧 State Blackboard slots MUST不泄漏

#### Scenario: ForceStop

- **WHEN** compiled StateMachine 被 ForceStop
- **THEN** lifecycle operation MUST只清理目标 execution scope 的 Character state slots

### Requirement: StateMachine上层停止必须使用普通Runnable release链

上层 Tree interruption、graceful stop 与 ForceStop MUST通过统一 compiled Runnable lifecycle 传播到 StateMachine、State body 与 nested StateMachine。StateMachine operation MUST不维护第二套停止或表现等待生命周期。

#### Scenario: 上层 LowerPriority 打断

- **WHEN** 上层 compiled Tree 要求释放正在运行的 StateMachine
- **THEN** release MUST按 outer-to-inner path 到达 active State body
- **AND** 逻辑退出 MUST不等待动画 fade

### Requirement: 嵌套 StateMachine runtime 必须维护完整 execution path

Program MUST为嵌套 StateMachine 编译稳定 outer-to-inner execution path。CharacterSimulationState MUST按 Actor/Graph activation/path 隔离 State slot 与 Blackboard State frame，不得按 runtime object identity 寻址。

#### Scenario: 内外层同名 State

- **WHEN** 两个层级包含同名 State
- **THEN** Kernel MUST以 compiled path/handle 定位不同 State slot

### Requirement: StateMachine 运行时必须由 Compiled Operation 执行

StateMachineNode、StateMachineGraph、StateNode、TransitionEdge 和 ConditionRuleGraph MUST编译为 CharacterSimulationProgram operation/table。Active、pending、exiting、transition、nested path 和 stop barrier MUST存入 CharacterSimulationState slot，MUST不由 StateMachineGraph runtime clone 持有。

#### Scenario: 进入嵌套状态机

- **WHEN** compiled State body 进入内层 StateMachineNode
- **THEN** Kernel MUST以稳定 execution path 访问内层 state slot
- **AND** MUST不创建 runtime Graph clone

### Requirement: StateMachine作者交互必须复用共享领域表面

BTSMTL StateMachine与Character PoseStateMachine MUST复用Graph Authoring Domain Framework的State、Transition、Entry、selection、Node/Port View、Details与Navigator交互实现。BTSMTL domain policy MUST继续把ConditionRuleGraph、interruption和compiled operation映射到自身typed document；Pose domain policy MUST把Presentation Fact、Pose source、transition routing和Pose IR映射到独立typed document。共享表面 MUST不合并两种数据schema、runtime state或compiler handler，也不得保留BTSMTL旧StateMachine View和Pose专用StateMachine View两套实现。

#### Scenario: 在两个领域创建Transition

- **WHEN** 作者分别在BTSMTL StateMachine与PoseStateMachine中拖出Transition
- **THEN** 两者 MUST使用同一StateMachine交互与Edge View实现
- **AND** mutation MUST分别落到BTSMTL Graph owner与Presentation owner
