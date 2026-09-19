## ADDED Requirements

### Requirement: 分域Provider与局部声明必须分开

Pipeline Blackboard MUST 同时展示外部 Provider 数据和图内局部声明，但两者 MUST 保持不同所有权、读写能力和生命周期。Character State、Input/TargetData、Attributes/GameplayTags、Equipment 等外部数据 MUST 通过正式 Provider 查询；Skill Local 才能创建为当前原生图的局部变量。系统 MUST 不把 Provider 真值复制进通用可写字典或假装成局部声明。

#### Scenario: 读取Character State

- **WHEN** 节点读取 Character State 或 Input/TargetData
- **THEN** 编译内容 MUST 通过对应 Provider/typed state 地址读取
- **AND** Catalog MUST 显示 external read-only 语义

#### Scenario: 创建Skill Local

- **WHEN** 作者在允许的图中新增 Skill Local
- **THEN** 系统 MUST 创建原生 Variable 和对应自有 declaration
- **AND** MUST 不生成同义 Provider slot

### Requirement: 原生变量与项目声明必须通过稳定ID对应

原生 Variable MUST 保存名称、类型、默认值和稳定 VariableId；项目声明 MUST 保存现有 scope、lifetime、category、InputBinding 和 fact projection；authority、sync 与 owner MUST 沿用现有正式来源和编译映射，不为本次迁移增设重复字段。二者 MUST 一一对应，类型和 owner 不匹配 MUST 失败。运行时 MUST 使用正式 compiled slot/address，不反射作者对象。

#### Scenario: 声明合法局部变量

- **WHEN** 作者保存一个 Graph、State、ActionInstance、Character 或 Frame 变量
- **THEN** validator MUST 检查 VariableId、类型、owner 和合法 scope/lifetime 组合
- **AND** runtime MUST 为其分配唯一 typed address

#### Scenario: 声明缺少原生变量

- **WHEN** declaration 找不到对应 VariableId
- **THEN** authoring/compile MUST 报告明确错误
- **AND** MUST 不创建 object 字典或默认变量继续运行

## MODIFIED Requirements

### Requirement: Pipeline Blackboard 必须统一图变量和运行时黑板

Blackboard declaration、原生 Variable、Graph Data Catalog 和 scope/lifetime 规则 MUST 是作者合同的唯一来源；运行时只通过 CharacterSimulationState 的 typed slots 读写。外部 Provider 数据、局部变量和 fact projection MUST 保持可识别边界，不能以同一可写容器混合。

#### Scenario: 编译节点读取局部变量

- **WHEN** 条件规则或技能节点读取局部声明
- **THEN** MUST 通过 compiled declaration address 访问 typed slot
- **AND** MUST 不反射作者对象或查询裸字符串字典

#### Scenario: Compiled ValueNode 读取变量

- **WHEN** 当前正式原生条件图 operation 读取 Blackboard declaration
- **THEN** MUST通过 compiled address 访问 CharacterSimulationState
- **AND** MUST不反射 authoring ExposedProperty object

### Requirement: Blackboard Variable 必须声明类型、作用域和生命周期

每个局部 Blackboard variable MUST 声明稳定 identity、owner、类型、默认值、scope、lifetime、authority、sync policy 与 category。Graph、State、ActionInstance、Character、Frame 的生命周期 MUST 由对应正式 activation/generation 管理；非法组合、类型不匹配或 Config 写入 MUST 失败，MUST 不降级到其它 scope 或默认值。

#### Scenario: State变量重入

- **WHEN** 同一 State 再次激活
- **THEN** MUST 建立新的 State owner generation
- **AND** 不得读取上次 activation 的值

#### Scenario: Frame变量结束

- **WHEN** 当前 logic frame 完成
- **THEN** 当前 generation 的 Frame 值和未消费投射 MUST 不可读
- **AND** MUST 不物理清空全部状态组来实现关闭

#### Scenario: 非法声明

- **WHEN** 作者将 Frame scope 配成 Spawn lifetime 或用错误类型读取变量
- **THEN** validator/runtime MUST 报告错误
- **AND** MUST 不自动转换或猜测规则

#### Scenario: State 作用域变量

- **WHEN** 某个变量声明为 State scope 且生命周期为 StateEnterToExit
- **THEN** 进入正式状态作用域时 runtime MUST 使用该 activation 的独立 typed 地址与 generation
- **AND** 离开该 execution scope 时 runtime MUST 只清理该 activation 的值
- **AND** 其它并行状态机和后续 activation MUST NOT 被清理或读到遗留值

#### Scenario: Graph 局部配置

- **WHEN** 一个状态行为 Graph 需要只在该 Graph 内可见的只读调参值
- **THEN** 作者 MUST 能声明 `Graph + Config` variable
- **AND** declaration MUST 随该私有或共享原生图序列化
- **AND** runtime MUST 拒绝对该 Config variable 的写入

#### Scenario: 非法 scope 和 lifetime

- **WHEN** 作者将 State scope 配成 ManualClear 或将 Frame scope 配成 Spawn
- **THEN** authoring validation MUST 报告非法组合
- **AND** runtime MUST NOT 猜测清理时机或降级成 Character scope

#### Scenario: 类型不匹配

- **WHEN** 节点以 Float 读取声明为 Vector2 的 variable
- **THEN** graph validation 和 runtime MUST 报告类型不匹配
- **AND** 系统 MUST NOT 尝试字符串转换、默认零值或其它 fallback

### Requirement: ExposedProperty 必须成为 Pipeline Blackboard 的 authoring 表面

Pipeline Blackboard 的 authoring 表面 MUST 由当前原生 Variable、项目声明和领域 Catalog 共同组成；旧 BaseExposedProperty 类型 MUST 不再是唯一表面，也 MUST 不被新消费者要求。已有合法默认值、稳定 identity、scope/lifetime 和引用关系 MUST 保留。

#### Scenario: 打开已有技能黑板

- **WHEN** 作者打开已有原生技能图
- **THEN** Catalog MUST 从原生 Variable 与对应 declaration 投影同一条目
- **AND** MUST 不创建 BaseExposedProperty 副本

#### Scenario: 旧类型无消费者

- **WHEN** 旧 BaseExposedProperty 的正式消费者已迁出
- **THEN** 系统 MUST 删除旧类型和其执行链
- **AND** MUST 不保留兼容空壳或 fallback

#### Scenario: State body 创建 Local 变量

- **WHEN** 作者在私有状态行为图创建 State scope declaration
- **THEN** declaration MUST仍归属该 Graph authoring
- **AND** Compiler MUST生成对应 owner/layout entry

### Requirement: Transition Rule 必须通过纯 ValueNode 读取黑板

条件图读取局部变量 MUST 使用显式 declaration reference 的纯 ValueNode；Provider 数据 MUST 通过正式只读查询节点获取。规则图 MUST 不 tick Timeline、Action、RunnableNode 或状态行为图。读取失败、owner 不可见或类型不匹配 MUST 使本次条件求值失败，不得写零值、空值或默认值继续比较。

#### Scenario: 读取输入和局部阈值

- **WHEN** Transition 同时需要 Input/TargetData 和 Skill Local threshold
- **THEN** 条件图 MUST 分别使用 Provider query 与显式 declaration reference
- **AND** MUST 不把输入复制成 Blackboard 变量

#### Scenario: 缺少声明

- **WHEN** 条件引用的 declaration 被删除或当前上下文不可见
- **THEN** validation/runtime MUST 失败并报告 owner
- **AND** MUST 不按 key 搜索其它声明

#### Scenario: 读取移动阈值

- **WHEN** Idle 到 WalkStart 的 Transition 需要比较输入幅度和 `WalkThreshold`
- **THEN** 规则图 MUST 通过显式 reference 读取 Character `WalkThreshold`
- **AND** Compare/And/Or 等纯条件节点 MUST 负责组合最终 Bool
- **AND** 规则图 MUST NOT 使用 Runnable `ExposedPropertyNode` 或裸字符串 key

#### Scenario: 规则图引用缺失 declaration

- **WHEN** 当前正式原生条件图 引用的 declaration 已删除、不可见或无法解析 owner
- **THEN** 校验 MUST 报告非法结构
- **AND** runtime MUST 让本次规则求值失败
- **AND** runtime MUST NOT 用硬编码默认值让 Transition 继续求值

### Requirement: Decision TreeClip 必须通过声明式 Frame Blackboard 输出决策

Decision TreeClip 写入 MUST 只针对属于当前 owner 的 Frame scope/Frame lifetime declaration，并携带当前 Action、Clip、cycle 和 generation provenance。Frame 开始、重新求值、统一投射与结束失效沿用现有提交边界；Projection=None 保持本地，ActionWindow 等正式 projection 生成 typed fact。无写入时读取 declaration default，不通过 OnDisable 写回 false，也不遍历全部 Frame group 物理清零。

#### Scenario: 当前帧产生窗口

- **WHEN** Decision TreeClip 在 active frame 写入合法 ActionWindow declaration
- **THEN** 系统 MUST 暂存带 ActionInstance 的 candidate
- **AND** 唯一 projection stage MUST 按现有提交顺序保留当前动作 candidate 与窗口可见性，并在正式 frame 结束生成 fact

#### Scenario: 当前帧没有写入

- **WHEN** 本帧没有 Decision 写入
- **THEN** 读取 MUST 返回 declaration default
- **AND** MUST 不生成 provenance、candidate 或 dirty 状态

#### Scenario: 失效owner

- **WHEN** TreeClip 写入的 owner generation 与当前不匹配
- **THEN** runtime MUST 使其不可读并报告错误
- **AND** MUST 不投射到其它 Action 或 Character owner

#### Scenario: Dodge恢复段开放动作切换

- **WHEN** Dodge Timeline的`RecoveryOpen` Decision TreeClip在当前Tick active
- **THEN** Tree MUST写入owner-local Bool Frame declaration
- **AND** 唯一projection stage MUST暂存当前ActionInstance的ActionWindow candidate
- **AND** Dodge Transition MUST能在同一Tick通过`ActionWindowActiveInfoNode`读取该WindowType

#### Scenario: Decision clip不再active

- **WHEN** 新logic frame中Decision TreeClip不在active时间范围
- **THEN** Frame Blackboard MUST把上一generation的true表现为declaration default
- **AND** Runtime MUST NOT依赖OnDisable写false或EndFrame物理清零才能关闭gate

#### Scenario: 当前Frame没有Decision写入

- **WHEN** 当前Tick没有任何Decision TreeClip写入某个Frame declaration
- **THEN** 读取 MUST返回declaration default且不能生成write provenance或projection
- **AND** 该declaration的value、owner、provenance和candidate state MUST不因Frame begin/end被标记dirty

#### Scenario: 声明策略冲突

- **WHEN** TreeClip 或状态行为图引用同一 declaration identity，但类型、owner、scope 或 lifecycle 等与真实声明不一致
- **THEN** validator/runtime MUST 报告冲突并定位声明来源
- **AND** MUST 不按同名 key 合并声明；不同 owner 的合法同名声明继续独立存在

### Requirement: Pipeline Blackboard declaration 必须作为 Graph Data Catalog 的正式来源

Data Catalog MUST 从原生 Variable、项目 declaration 和 Provider catalog 形成唯一只读投影，保留真实 owner、identity、类型、scope、lifetime、authority、sync、category 与 projection。继承声明只能读和定位原 owner；当前图不得复制 declaration，也不得用裸 key 合并同名项。

#### Scenario: 显示本地和继承变量

- **WHEN** 当前图同时可见本地 Skill Local 与外层 Character declaration
- **THEN** Catalog MUST 分别显示 local/inherited 和真实 owner
- **AND** MUST 不创建第二份序列化变量

#### Scenario: 显示 inline Timeline 本地 declaration

- **WHEN** 作者打开拥有 local `RecoveryOpen` declaration 的 Timeline 私有 TreeClip 图
- **THEN** Catalog MUST 显示该 TreeClip 图真实 declaration owner 的 local editable 条目
- **AND** 显示 ActionWindow projection、WindowType 与稳定 identity

#### Scenario: 显示 RootTree declaration

- **WHEN** 私有状态行为图 可见 根技能图 的 `RunThreshold`
- **THEN** Catalog MUST 显示 inherited read-only 条目和真实 owner

#### Scenario: 同 key 不同 owner

- **WHEN** 两个 owner 有同名但不同 identity 的 declaration
- **THEN** Catalog MUST 按 identity 和 owner 区分，MUST NOT 合并

### Requirement: Blackboard Catalog source 必须按 declaration 所有权限制写操作

Catalog 只允许通过正式 API 编辑或删除当前 owner 的本地 declaration。继承变量和 Provider 数据 MUST 为只读或转到其正式 owner；新增局部变量 MUST 同步创建原生 Variable 与 declaration。当前 context 不可见、类型不匹配或 owner 已失效时，拖拽创建 MUST 失败，不写默认值继续 authoring。

#### Scenario: 编辑局部默认值

- **WHEN** 作者修改当前图持有的 Skill Local 默认值
- **THEN** mutation MUST 更新原生 Variable 的唯一默认值，并保持 declaration 的稳定关联
- **AND** MUST 保持单一 Undo 边界

#### Scenario: 删除外层声明

- **WHEN** 作者选中 inherited Character declaration
- **THEN** 当前图 MUST 不提供删除命令
- **AND** MUST 提供定位真实 owner 的入口

#### Scenario: 编辑本地默认值

- **WHEN** 作者修改当前 owner 的 Config 变量默认值
- **THEN** mutation MUST 更新对应原生 Variable 的唯一默认值并保持 declaration 的稳定关联
- **AND** Config 在运行期间 MUST 继续只读，不能把作者修改作为运行写入口

#### Scenario: 删除继承 declaration

- **WHEN** 作者查看从 根技能图 继承的 declaration
- **THEN** 目录 MUST 不提供针对当前私有图 的删除命令

#### Scenario: 新建 State variable

- **WHEN** 当前 Graph owner 支持 State scope 且作者通过目录创建 State variable
- **THEN** 系统 MUST 创建属于当前 owner 的合法 declaration

### Requirement: Blackboard Catalog source 必须复用上下文化可见性和节点引用链路

Catalog、Provider picker 与条件节点创建 MUST 复用同一 context 可见性、owner 解析和显式 reference 工厂。MUST 不按裸 key、运行字典或显示名重新查找。引用在写入前失效时 MUST 报告明确原因且不创建默认节点。

#### Scenario: 拖入Skill Local

- **WHEN** 作者从当前 Catalog 拖入可见 Skill Local
- **THEN** 系统 MUST 创建带稳定 declaration reference 的纯 ValueNode
- **AND** MUST 使用当前领域 mutation owner

#### Scenario: Provider不可用

- **WHEN** 当前上下文无法提供某个 Input/TargetData 或 Attribute query
- **THEN** picker MUST 显示不可用原因
- **AND** MUST 不写空值、零值或上一帧值继续编辑

#### Scenario: Transition 读取阈值

- **WHEN** 作者把可见的 `RunThreshold` 从目录拖入 当前正式原生条件图
- **THEN** 系统 MUST 创建保存显式 declaration reference 的当前正式纯变量读取节点

#### Scenario: declaration 在当前 context 不可见

- **WHEN** 某 declaration 不属于当前 Graph 的 local/inherited 可见集合
- **THEN** 目录 MUST 不展示该条目，也 MUST NOT 通过裸 key 搜索把它加入结果

#### Scenario: 引用目标已失效

- **WHEN** 条目对应 declaration 在拖拽完成前被删除或 owner context 已切换
- **THEN** 节点创建 MUST 失败并报告失效引用，MUST NOT 创建绑定默认值的节点
