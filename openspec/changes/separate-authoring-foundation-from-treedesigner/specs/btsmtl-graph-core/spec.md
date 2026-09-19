## ADDED Requirements

### Requirement: 原生图资产必须是唯一可写结构

技能、技能 FSM 和 Pose MUST 继续保存和消费当前正式原生图的节点、连接、参数与引用。人工编辑、C# 创建、粘贴和编译准备 MUST 经所属领域正式接口操作同一结构，不再生成旧图模型或第二份可写文档。原生端口对象 MUST 使用领域声明的逻辑身份和约束，业务值 MUST 只保存于真实领域 owner。

#### Scenario: 修改技能连接

- **WHEN** 作者修改原生技能图的一条连接并保存
- **THEN** 正式编译 MUST 直接读取该图的同一端点与连接
- **AND** MUST 不生成旧节点/边集合供编译或窗口同步

#### Scenario: 从资源入口打开图

- **WHEN** 作者从 Project、Inspector 或业务定义打开当前正式图资产
- **THEN** MUST 到达该领域唯一原生编辑入口
- **AND** MUST 不打开旧图窗口或转换成另一份可写结构

### Requirement: 下钻内容必须保留私有与共享所有权

需要下钻内容的节点、状态、转移和 TreeClip MUST 默认拥有可立即编辑的私有内容，沿用所属领域正式资产表示。作者 MUST 无需手工先创建共享资产。Shared 必须通过显式创建、分配或提取；同一引用 MUST 只有一个真实来源。删除 owner MUST 同时删除其专有内容，MUST 不删除共享资产。物理内联或原生子资产由领域序列化合同决定，MUST 不强制恢复旧内联图类型。

#### Scenario: 创建带规则的转移或 TreeClip

- **WHEN** 作者创建需要私有下钻图的 owner
- **THEN** 系统 MUST 在同一作者操作中建立私有内容，并提供 Open 或双击入口
- **AND** MUST 不要求先点击 Create Inline 或手工分配共享资源

#### Scenario: 显式提取共享图

- **WHEN** 作者把合法私有图提取为共享资源
- **THEN** 原 owner MUST 切换到该共享引用，清理原私有真数据
- **AND** 后续删除原 owner MUST 保留共享资源

#### Scenario: 查看下钻引用

- **WHEN** 作者选择拥有下钻内容的节点或边
- **THEN** 编辑器 MUST 明确显示私有、共享或缺失状态并提供对应操作
- **AND** 节点标题、Details 和转移端点 MUST 使用同一业务显示名；未自定义时使用节点类型显示名
- **AND** 不适用的共享字段 MUST 不以 None 配置项占用默认节点表面

### Requirement: 编辑上下文与导航必须独立于业务数据

页面、选择恢复、业务入口上下文和窗口绑定 MUST 只属于 Editor；下钻保持准确 owner 和业务上下文，MUST 不写入图、节点、连接或 Timeline 内容。Timeline MUST 通过独立正式 Timeline 编辑入口打开；图导航只表达图及 TreeClip 来源，不把 Timeline 页面混入图 breadcrumb。dirty 与 Undo MUST 作用于当前内容真实 serialized owner。

#### Scenario: 从技能图打开 Timeline 再进入 TreeClip

- **WHEN** 作者打开技能引用的 Timeline 并进入其中的 TreeClip 图
- **THEN** Timeline MUST 保持可见，图窗口 MUST 打开对应作者图并保留来源上下文
- **AND** 页面位置和选择 MUST 不写入业务数据，也不改变播放绑定

#### Scenario: 孤立资源缺少上下文

- **WHEN** 作者直接打开缺少业务定义上下文的共享图
- **THEN** 图本身 MUST 可由正式资产入口打开
- **AND** 依赖外部上下文的字段 MUST 明确不可用，不写入 fallback 配置

### Requirement: 图声明与运行访问必须保留真实所有权

每张图 MUST 只保存自己拥有的局部声明；外层可见变量 MUST 通过稳定声明引用访问，MUST 不复制到子图。执行访问 MUST 使用正式 layout 和调用上下文提供的 Graph、State activation、ActionInstance 与 Frame owner，不从显示名、路径或运行对象地址推断。

#### Scenario: 子图读取外层变量

- **WHEN** 状态行为图读取根技能图的可见声明
- **THEN** 子图 MUST 只保存引用，并解析原 declaration owner
- **AND** 共享图的不同调用实例 MUST 使用各自运行 owner 隔离可变值

#### Scenario: 缺少必需运行 owner

- **WHEN** 孤立图调用读取 ActionInstance 变量但没有 ActionInstance 身份
- **THEN** 正式解析 MUST 报告缺失上下文
- **AND** MUST 不使用 Character、Graph、最后活动实例或默认值代替

### Requirement: 图运行必须服从所属领域并隔离作者资产

Skill 与技能 FSM、TreeClip MUST 使用正式编译执行内容与调用状态；Pose MUST 使用其现有角色独立原生实例和领域运行合同。原生实例初始化、编译准备及资源绑定分别归原领域 owner。系统 MUST 不恢复旧通用图解释器或为整角色创建第二运行链，也 MUST 不把可编辑资产当作可变运行状态。

#### Scenario: 多实例使用同一技能或 TreeClip

- **WHEN** 两个技能调用或播放实例使用同一作者内容
- **THEN** 它们 MUST 共享不可变内容并拥有独立的调用和变量状态
- **AND** MUST 不创建旧树工作副本或由 Timeline 执行旧节点

#### Scenario: 多角色使用同一 Pose 资源

- **WHEN** 两个角色使用同一 Pose 作者图
- **THEN** 各自 MUST 使用独立原生运行实例
- **AND** 执行状态 MUST 不污染另一角色或作者资产

### Requirement: 图运行观察必须来自唯一只读诊断来源

编辑器 MUST 绑定作者图，通过正式 diagnostics 和 source identity 显示精确目标实例的节点、连接、状态机及生命周期状态。作者图 MUST 不因观察而被替换为运行实例，导航 MUST 不持有运行对象作为业务数据。旧节点字段直读高亮 MUST 随旧视图删除。

#### Scenario: 在调试中下钻

- **WHEN** 作者查看已提交且图身份/revision 匹配的诊断并进入子图
- **THEN** 页面 MUST 仍展示作者内容，overlay MUST 定位选中调用的对应子实例
- **AND** MUST 不修改作者参数或同时显示另一套直接读取旧节点状态的结果

## MODIFIED Requirements

### Requirement: 节点创建尊重图类型规则

所有搜索、拖拽、粘贴、人工和 C# 创建 MUST 复用当前领域 Graph Role 的能力规则。技能 FSM MUST 只接收正式状态结构；条件规则 MUST 只接收该领域允许的纯读取、值和条件能力。创建失败 MUST 不修改图，正式编译/准备 MUST 消费同一规则。

#### Scenario: ConditionRuleGraph 拒绝行为节点

- **WHEN** 作者或代码尝试向条件图加入 Timeline、动作生命周期或其它副作用节点
- **THEN** 正式领域规则 MUST 拒绝创建并报告能力与图角色
- **AND** MUST 不以普通节点或旧基类身份绕过限制

#### Scenario: ConditionRuleGraph 接受条件节点

- **WHEN** 作者添加合法 Input、黑板读取、Compare 或 Logic 节点
- **THEN** MUST 允许其使用正式 typed 字段、端口与声明引用
- **AND** MUST 不因此启动节点原生运行或产生行为副作用

#### Scenario: StateMachineGraph 拒绝非法节点

- **WHEN** 创建、粘贴或 C# authoring 尝试向原生技能 FSM 加入普通技能执行节点或条件值节点
- **THEN** 正式状态机能力规则 MUST 在写入前拒绝
- **AND** 条件内容 MUST 通过转移所属的正式原生规则图编辑，不恢复旧 StateMachineGraph 或 ConditionRuleGraph 类型

### Requirement: 不新增 Graph 分裂路径

每个领域 MUST 保持一份正式作者结构、一套业务定义与一条正式运行链。当前原生图与领域专有数据 MUST 保留其边界，共享作者合同和编辑集成 MUST 不变成新的图模型、同步器或通用执行器。Pose MUST 不继承技能节点语义；系统 MUST 不保留旧图 fallback、双份序列化集合或旧窗口写入口。

#### Scenario: 复用节点编辑交互

- **WHEN** 两个领域使用 Details、选择、复制粘贴和 Undo
- **THEN** MUST 复用原生编辑能力与共享业务集成，并只修改各自真实 owner
- **AND** MUST 不生成旧 BaseGraph 或第二份通用图来实现共享

#### Scenario: BTSMTL新增规则图能力

- **WHEN** 技能状态转移需要新的条件求值能力
- **THEN** MUST 使用当前正式原生规则图及所属领域的字段、端口与能力声明
- **AND** MUST 不恢复 ConditionRuleGraph、PropertyPort 或 BaseTree 旧入口；Behavior Designer 的条件继续归插件拥有

#### Scenario: 打开Presentation Pose Graph

- **WHEN** 作者打开正式 Pose 图
- **THEN** MUST 装配 Pose 原生编辑入口、领域端口约束和自研业务面板
- **AND** MUST 不创建旧 BaseGraph、技能黑板副本或运行求值上下文

### Requirement: Graph 必须拥有统一稳定 authoring identity

每个正式作者 Graph MUST 持有稳定 `GraphAuthoringId`，Node 和 Edge MUST 继续持有各自稳定 authoring GUID。编译内容的 SourceMap 与原生运行实例 MUST 保留这些 source identities，运行实例 MUST 使用独立 runtime instance identity。Pipeline Blackboard declaration owner、C#作者导出上下文、Debug Source Map 和 editor navigation MUST 引用同一个 Graph authoring identity。

#### Scenario: 创建 inline Graph

- **WHEN** owner 创建新的私有 Graph
- **THEN** Graph MUST 获得新的稳定 `GraphAuthoringId`
- **AND** Graph 内 Node/Edge MUST 获得各自稳定 identity

#### Scenario: 创建 runtime clone

- **WHEN** Pose 领域从作者图创建角色独立原生实例
- **THEN** 实例 MUST 保留 Graph/Node/Connection 的作者身份用于定位
- **AND** MUST 获得独立运行实例身份；技能仍通过编译内容和调用状态运行，不复制旧图

#### Scenario: 迁移 Blackboard owner identity

- **WHEN** 实现将旧 `BlackboardOwnerId` 提升为 `GraphAuthoringId`
- **THEN** 现有 declaration owner reference MUST 一次性迁移到同一 identity value
- **AND** 旧字段、旧 API 和第二份 debug Graph id MUST 删除

### Requirement: Graph节点兼容性必须由稳定Authoring Capability裁决

每个可进入受限Graph的节点类型 MUST声明稳定authoring capability。Graph Role MUST通过唯一policy定义允许的capability；正式节点创建 API、Node Search、拖拽、粘贴、脚本创建与Compiler Validator MUST复用该policy。系统 MUST为后续C# authoring暴露同一只读policy查询，且 MUST NOT复制另一份作者合同。系统 MUST NOT按NodePath字符串、显示名、继承层次或窗口类型猜测节点兼容性。

#### Scenario: 已退役AI图尝试进入BTSMTL

- **WHEN** 搜索、粘贴、脚本或Compiler尝试打开旧AIControllerTree或创建旧AI节点
- **THEN** 统一Graph policy MUST拒绝该图和节点
- **AND** Graph数据 MUST不发生修改

#### Scenario: Behavior Designer图不注册为BTSMTL Graph

- **WHEN** 作者从BTSMTL Graph入口选择Behavior Designer行为资源
- **THEN** 入口 MUST 明确说明该资源由插件编辑器拥有
- **AND** BTSMTL MUST 不为其创建Graph role、节点或编译镜像

#### Scenario: 退役AI节点缺少能力声明

- **WHEN** 未声明authoring capability的旧AI节点尝试进入任一BTSMTL Graph
- **THEN** 创建与发布 MUST失败并报告节点类型和Graph Role
- **AND** 系统 MUST不按默认Base节点处理

## REMOVED Requirements

### Requirement: BaseGraph 承载唯一图结构数据

**Reason**: 此条款将有效作者能力绑定到已由原生图替代的 TreeDesigner 模型、窗口或执行器。

**Migration**: 迁入本规范的原生图结构、私有/共享所有权、编辑上下文、局部声明、领域运行隔离与只读观察要求；保留合法内容和稳定身份后删除旧实现。

### Requirement: BaseGraph 承载结构编辑操作

**Reason**: 此条款将有效作者能力绑定到已由原生图替代的 TreeDesigner 模型、窗口或执行器。

**Migration**: 迁入本规范的原生图结构、私有/共享所有权、编辑上下文、局部声明、领域运行隔离与只读观察要求；保留合法内容和稳定身份后删除旧实现。

### Requirement: BaseTreeAsset 保持资产和编辑器入口

**Reason**: 此条款将有效作者能力绑定到已由原生图替代的 TreeDesigner 模型、窗口或执行器。

**Migration**: 迁入本规范的原生图结构、私有/共享所有权、编辑上下文、局部声明、领域运行隔离与只读观察要求；保留合法内容和稳定身份后删除旧实现。

### Requirement: Graph 引用和页面栈保持 editor-only

**Reason**: 此条款将有效作者能力绑定到已由原生图替代的 TreeDesigner 模型、窗口或执行器。

**Migration**: 迁入本规范的原生图结构、私有/共享所有权、编辑上下文、局部声明、领域运行隔离与只读观察要求；保留合法内容和稳定身份后删除旧实现。

### Requirement: 私有下钻 Graph 默认 inline data

**Reason**: 此条款将有效作者能力绑定到已由原生图替代的 TreeDesigner 模型、窗口或执行器。

**Migration**: 迁入本规范的原生图结构、私有/共享所有权、编辑上下文、局部声明、领域运行隔离与只读观察要求；保留合法内容和稳定身份后删除旧实现。

### Requirement: 下钻引用 UI 表达编辑意图

**Reason**: 此条款将有效作者能力绑定到已由原生图替代的 TreeDesigner 模型、窗口或执行器。

**Migration**: 迁入本规范的原生图结构、私有/共享所有权、编辑上下文、局部声明、领域运行隔离与只读观察要求；保留合法内容和稳定身份后删除旧实现。

### Requirement: BaseGraph 承载运行上下文但不承担执行生命周期

**Reason**: 此条款将有效作者能力绑定到已由原生图替代的 TreeDesigner 模型、窗口或执行器。

**Migration**: 迁入本规范的原生图结构、私有/共享所有权、编辑上下文、局部声明、领域运行隔离与只读观察要求；保留合法内容和稳定身份后删除旧实现。

### Requirement: Shared Graph Asset 只是复用外壳

**Reason**: 此条款将有效作者能力绑定到已由原生图替代的 TreeDesigner 模型、窗口或执行器。

**Migration**: 迁入本规范的原生图结构、私有/共享所有权、编辑上下文、局部声明、领域运行隔离与只读观察要求；保留合法内容和稳定身份后删除旧实现。

### Requirement: Graph 运行工作副本来自数据克隆

**Reason**: 此条款将有效作者能力绑定到已由原生图替代的 TreeDesigner 模型、窗口或执行器。

**Migration**: 迁入本规范的原生图结构、私有/共享所有权、编辑上下文、局部声明、领域运行隔离与只读观察要求；保留合法内容和稳定身份后删除旧实现。

### Requirement: TreeWindow 支持 editor-only authoring context

**Reason**: 此条款将有效作者能力绑定到已由原生图替代的 TreeDesigner 模型、窗口或执行器。

**Migration**: 迁入本规范的原生图结构、私有/共享所有权、编辑上下文、局部声明、领域运行隔离与只读观察要求；保留合法内容和稳定身份后删除旧实现。

### Requirement: BaseGraph declaration 必须保持局部所有权并支持显式外层引用

**Reason**: 此条款将有效作者能力绑定到已由原生图替代的 TreeDesigner 模型、窗口或执行器。

**Migration**: 迁入本规范的原生图结构、私有/共享所有权、编辑上下文、局部声明、领域运行隔离与只读观察要求；保留合法内容和稳定身份后删除旧实现。

### Requirement: Graph evaluation context 必须携带变量访问所有权

**Reason**: 此条款将有效作者能力绑定到已由原生图替代的 TreeDesigner 模型、窗口或执行器。

**Migration**: 迁入本规范的原生图结构、私有/共享所有权、编辑上下文、局部声明、领域运行隔离与只读观察要求；保留合法内容和稳定身份后删除旧实现。

### Requirement: TreeClip 私有下钻 Graph 必须默认 inline

**Reason**: 此条款将有效作者能力绑定到已由原生图替代的 TreeDesigner 模型、窗口或执行器。

**Migration**: 迁入本规范的原生图结构、私有/共享所有权、编辑上下文、局部声明、领域运行隔离与只读观察要求；保留合法内容和稳定身份后删除旧实现。

### Requirement: Graph 运行时初始化必须收敛到统一非虚入口

**Reason**: 此条款将有效作者能力绑定到已由原生图替代的 TreeDesigner 模型、窗口或执行器。

**Migration**: 迁入本规范的原生图结构、私有/共享所有权、编辑上下文、局部声明、领域运行隔离与只读观察要求；保留合法内容和稳定身份后删除旧实现。

### Requirement: TreeWindow runtime 状态必须通过只读 diagnostics overlay 表达

**Reason**: 此条款将有效作者能力绑定到已由原生图替代的 TreeDesigner 模型、窗口或执行器。

**Migration**: 迁入本规范的原生图结构、私有/共享所有权、编辑上下文、局部声明、领域运行隔离与只读观察要求；保留合法内容和稳定身份后删除旧实现。
