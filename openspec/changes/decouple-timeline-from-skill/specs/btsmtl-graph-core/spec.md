## MODIFIED Requirements

### Requirement: Graph 引用和页面栈保持 editor-only

节点、边、模块和 Timeline MUST通过正式作者引用表达下钻内容。私有内容默认 inline，明确复用时才使用 shared 资产。Graph 窗口页面栈 MUST只承载 Graph 和 TreeClip 子图，Timeline MUST继续在现有独立 Timeline 窗口编辑，不进入 Graph breadcrumb。来源 owner、输入声明和允许能力 MUST显式传递；独立 Timeline 的 TreeClip 不得要求一个虚构父 Graph。页面栈、窗口绑定、选择与返回位置 MUST只属于 Editor，不进入运行内容。

#### Scenario: 节点下钻到内联 Graph

- **WHEN** 作者打开节点内联图
- **THEN** Graph 页面栈 MUST打开该 owner 的数据并保留来源节点和引用位置

#### Scenario: 节点下钻到 shared Graph

- **WHEN** 作者打开节点引用的共享图
- **THEN** 页面 MUST绑定真实 shared 资产并显示 Shared Asset

#### Scenario: TimelineNode 下钻到 inline Timeline

- **WHEN** 作者打开 TimelineNode 的内联内容
- **THEN** 来源 Graph 页面 MUST保持，Timeline 窗口绑定真实 serialized owner 与本地数据
- **AND** Timeline MUST不进入 Graph 页面栈

#### Scenario: TimelineNode 下钻到 shared Timeline

- **WHEN** 作者打开 TimelineNode 的共享内容
- **THEN** Timeline 窗口 MUST绑定 shared 资产并显示其归属
- **AND** 来源调用点的合法输入绑定 MUST能用于 TreeClip 作者检查

#### Scenario: Timeline 下钻到 TreeClip

- **WHEN** 作者从 Timeline 打开 TreeClip
- **THEN** 现有 Graph Shell MUST打开该片段的 resolved 子图并显示真实来源
- **AND** Timeline 窗口 MUST保持可见，Graph breadcrumb 不得加入 Timeline 页面
- **AND** 子图 MUST只看到自身声明及正式绑定允许的输入/变量

#### Scenario: 独立 Timeline 没有来源 Graph 窗口

- **WHEN** 作者直接打开 shared Timeline 并进入 TreeClip
- **THEN** 系统 MUST通过现有 Graph Shell 打开真实片段子图
- **AND** MUST不搜索父角色、创建假 Graph 或要求 Skill

#### Scenario: 保存双窗口内容

- **WHEN** 作者在 Graph、TreeClip 或 Timeline 窗口修改内容
- **THEN** dirty、Mutation 和 Undo MUST作用于真实作者 owner
- **AND** 窗口状态、页面栈和播放观察绑定 MUST不保存到业务资产

### Requirement: BaseGraph 承载运行上下文但不承担执行生命周期

BaseGraph MUST允许明确保留的通用对象解释器保存非序列化 User、DeltaTime 与 typed 上下文，但本身不得拥有执行生命周期。Character/Skill 及本次独立 Timeline 的 TreeClip MUST使用编译定义和各自实例状态，MUST不通过 RunnableTree、StateMachineGraphRuntime 或 Graph clone 执行。其他明确保留的非 Character 通用树用途可以按原合同使用隔离工作副本，但不得成为 Timeline 的替代执行路径或共享其运行状态。

#### Scenario: Character 正式运行

- **WHEN** 角色构建产物有效且正式 Session 处于 Active
- **THEN** 代码控制与编译技能内容 MUST在原 Evaluate/Finalize 事务中执行
- **AND** MUST不创建作者 Graph 工作副本

#### Scenario: 非角色通用 RunnableTree tick

- **WHEN** 与正式 Timeline 无关的既有通用对象树组合显式推进
- **THEN** 它 MUST只更新自身隔离上下文，不读写 Character 或 Timeline 实例状态

#### Scenario: 非 Skill Timeline 请求树执行

- **WHEN** 独立 Timeline 到达 TreeClip
- **THEN** MUST调用共用编译树执行能力，而不是使用通用对象树例外

### Requirement: Graph 运行时初始化必须收敛到统一非虚入口

明确保留的通用对象解释器 MUST通过其统一入口完成父子 route、identity 与图结构初始化。Character/Skill 与独立 Timeline TreeClip MUST不调用该入口，它们的树内容必须经正式编译形成只读操作与状态声明。TreeClip MUST不提供任何基于作者工作副本的专用运行初始化路径。

#### Scenario: 初始化非 Character 嵌套 Graph

- **WHEN** 与正式 Timeline 无关的既有通用解释器初始化子图
- **THEN** MUST先建立父级/route，再在核心索引完整后解析节点引用
- **AND** 工作副本 MUST与 Character 及正式 Timeline 状态隔离

#### Scenario: 编译 Character Timeline TreeClip

- **WHEN** 编译器解析 TreeClip 作者内容
- **THEN** MUST校验 owner、片段 identity、声明绑定和节点执行能力
- **AND** MUST不调用作者图运行初始化

#### Scenario: 尝试运行时初始化 Character TreeClip

- **WHEN** 任一正式 Timeline 调用试图创建 TreeClip 作者工作副本执行
- **THEN** 组合或执行入口 MUST明确拒绝，不能创建半初始化对象或默认上下文
