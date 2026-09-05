## MODIFIED Requirements

### Requirement: Runtime diagnostics 必须与执行对象布局解耦

Runtime diagnostics MUST只依赖稳定作者来源、内容版本、执行位置、正式调用方/播放身份、相应时间域和结构化 Trace。Skill MUST保留 Actor/ActionInstance/SimulationTick 来源，非 Skill MUST使用真实 owner/播放 identity 和帧序列，不得伪造角色字段。Editor MUST不持有或轮询可变状态视图、pending evaluation、作者运行 clone 或世界求解器对象。

#### Scenario: Graph Editor 跟随 Runtime

- **WHEN** Editor 显示编译操作的当前状态
- **THEN** MUST通过 SourceMap 与 Trace 反查作者元素，不依赖执行对象布局

#### Scenario: 独立 Timeline 输出诊断

- **WHEN** 非 Skill owner 推进已绑定 Timeline
- **THEN** Trace MUST能定位内容、调用点、播放实例和所属帧
- **AND** MUST不为了诊断而创建 Actor 或 ActionInstance

### Requirement: Source identity 与 runtime instance identity 必须分离

Diagnostics MUST区分稳定作者来源、调用路径与运行实例。树内容继续由树核心稳定 key 单向生成诊断 key，事件带 containment route 和 generation；Timeline 内容同时保持资产/轨道/片段及真实 Graph 或 C# 调用来源。Skill 使用 Actor/ActionInstance，非 Skill 使用明确 owner/播放实例，不得按模板、显示名或路径合并状态。正式树生命周期、Timeline 绑定和表现消费不得依赖 Diagnostics 类型。

#### Scenario: 同一状态重复进入

- **WHEN** 同一来源在不同 activation generation 执行
- **THEN** 作者来源 MUST相同，运行实例 identity 必须不同

#### Scenario: 两个角色运行同一 Definition

- **WHEN** 两个角色使用相同作者内容
- **THEN** Trace 和 Debug Session MUST分离其 Node、Timeline 和变量状态

#### Scenario: shared Graph双引用

- **WHEN** 两个 owner 调用相同 shared Graph 节点
- **THEN** 来源元素可以相同，但调用路径、父 activation 和实例 MUST不同

#### Scenario: 同一 Timeline 两个独立播放

- **WHEN** 两个非 Skill 调用复用同一 Timeline
- **THEN** 观察 MUST按 owner、播放 identity 和 generation 分离
- **AND** C# 调用不得伪造 Graph 节点作为来源
