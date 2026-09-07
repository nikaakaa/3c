## Purpose

定义可被技能及非技能调用方共同使用的 Timeline 内容、轨道和片段规则、预先编译的数据、时间推进与停止行为，使内容复用不依赖 Character 管线，同时保持同一套执行语义和明确的业务能力边界。

## ADDED Requirements

### Requirement: Timeline 必须使用统一内容模型

系统 MUST以同一种 Timeline 内容模型表达轨道、片段、时间、区段、资源引用及外部输入声明。不同业务 MUST通过领域 Track/Clip 表达行为，通过资源和参数表达内容变化，MUST不按技能或非技能调用方建立重复的 Timeline 数据模型。共享与内嵌内容 MUST保持明确且唯一的作者 owner，MUST不保存运行目标、播放状态或调用方对象。

#### Scenario: 两种业务编排不同片段

- **WHEN** 技能作者编排动画/运动片段，场景作者编排表现参数片段
- **THEN** 两者 MUST使用相同 Timeline 时间和轨道结构
- **AND** 可用业务行为 MUST由各自领域片段合同决定

#### Scenario: 同一内容重复调用

- **WHEN** 两个调用引用同一 shared Timeline
- **THEN** 内容 MUST保持共享只读，两个调用不得向资产写入各自目标或进度

### Requirement: Track 和 Clip 必须具有完整一致的类型合同

每种正式 Track/Clip MUST声明稳定类型身份、参数类型、允许组合、目标/通道约束、重叠规则、所需能力和执行阶段。作者界面、Document、构建与执行装配 MUST使用同一合同；缺少任一必需环节的类型 MUST明确拒绝创建或发布。资源变化 MUST不要求新增行为类型。跨轨道的运动合成、动画混合和相机选择 MUST由相应领域合同处理，不得依赖未声明的数组顺序。

#### Scenario: 轻震与重震

- **WHEN** 两个震动片段只在资源或强度上不同
- **THEN** 作者 MUST能使用同一种行为类型配置它们

#### Scenario: 不允许的片段重叠

- **WHEN** 作者让两个片段形成其轨道合同禁止的重叠
- **THEN** 作者校验与正式构建 MUST一致拒绝并定位两个片段

#### Scenario: 新片段只有菜单登记

- **WHEN** 可达内容使用没有完整编译或执行能力的片段类型
- **THEN** 发布 MUST失败并说明缺失能力，MUST不生成空执行内容

### Requirement: Timeline 内容必须先编译为可独立校验的只读数据

Timeline 与可达 TreeClip/子树 MUST经共享内容编译和明确 Numeric Target 生成只读内容单元，包含时间、片段、绑定签名、局部状态布局、来源及完整能力要求。Skill 构建和独立 Timeline 构建 MUST复用同一发射与执行语义。独立构建 MUST接受明确 Timeline 内容根，MUST不要求假 Character、Skill 或 Graph 调用节点。Runtime MUST不读取作者对象、现场编译或回退作者解释器。

#### Scenario: 独立构建共享资产

- **WHEN** 调用者提供精确 shared Timeline 根和受支持的 Numeric Target
- **THEN** 构建 MUST产生可独立校验的内容产物
- **AND** MUST不要求 Character Definition 或 ActionProfile

#### Scenario: 技能和独立调用使用相同片段

- **WHEN** 两种调用方式使用相同内容与兼容能力
- **THEN** 时间、片段参数、树控制与停止规则 MUST来自相同内容语义

### Requirement: 时间推进必须覆盖片段与循环边界

Timeline MUST只使用调用方显式提供的时间推进；一次调用的 Once/Loop、当前进度、cycle 与活动片段 MUST属于实例状态。跨越循环和片段边界时 MUST有序处理尾段、完整 cycle 与头段，按片段合同产生进入、采样、离开和一次事件。无正时长的 Loop MUST在构建或开始前失败，MUST不注入默认时长。核心 MUST不读取 Unity Time、动画播放头或创建独立调度器。

#### Scenario: 一帧跨过完整片段

- **WHEN** 一次合法推进从片段开始之前跨到结束之后
- **THEN** 片段 MUST仍按合同完成进入和离开，不能因最终时间落在区间外而漏执行

#### Scenario: 跨过多个循环

- **WHEN** 一次推进跨过多个完整 cycle
- **THEN** 每个 cycle 的事件和片段生命周期 MUST按顺序处理且不重复

#### Scenario: 调用方暂停推进

- **WHEN** 调用方暂停并未提交新的推进
- **THEN** 时间与树状态 MUST保持，MUST不从其他时钟自行前进

### Requirement: TreeClip 必须作为可选编译执行能力保留

含 TreeClip 的 Timeline MUST支持纯决策及有生命周期的树内容，复用共用编译树控制和停止规则。所有可达节点和子树 MUST声明并满足能力与输入需求；技能专属节点不得因放在非 Skill Timeline 内而获得角色权限。不含 TreeClip 的内容 MUST不要求树执行能力。独立 Timeline 的 TreeClip MUST不运行作者 Graph 工作副本或第二套树解释路径。

#### Scenario: 非 Skill 运行条件树

- **WHEN** 独立调用提供条件树声明的输入和表现输出能力
- **THEN** TreeClip MUST能执行条件与合法分支，MUST不要求 ActionInstance

#### Scenario: 非 Skill 使用角色动作节点

- **WHEN** TreeClip 或嵌套子树包含角色动作节点，而调用环境没有相应正式能力
- **THEN** 构建或绑定 MUST在执行前失败并定位节点
- **AND** MUST不忽略节点、伪造角色或转换成场景对象操作

#### Scenario: 子树递归

- **WHEN** TreeClip 可达子图形成直接或间接递归
- **THEN** 构建 MUST拒绝并报告引用链，显式循环不应被误判为递归

### Requirement: 完成和停止必须关闭完整调用范围

自然完成、取消和 owner 销毁 MUST覆盖本次播放的活动片段、TreeClip 与嵌套调用。自然到达终点 MUST在必要退出完成后才报告完成；必要退出指领域离开/释放义务，不隐式等待表现领域独立拥有的视觉尾段。停止中 MUST禁止新的正常业务输出，只允许正式清理；graceful 停止进度 MUST进入实例状态，force teardown MUST不等待动画或网络。共享内容的其他播放 MUST不受影响。

#### Scenario: 取消正在等待的 TreeClip

- **WHEN** 父 Timeline 停止时子树仍在等待
- **THEN** 子树 MUST沿相同停止流程退出并释放本次调用状态
- **AND** MUST不保留独立继续运行的等待或时钟

#### Scenario: 一个播放停止另一个继续

- **WHEN** 两次播放使用相同内容且其中一次停止
- **THEN** 另一次的进度、局部变量和活动输出 MUST保持自己的生命周期

#### Scenario: 领域释放后仍有合法视觉尾段

- **WHEN** 片段已完成正式释放且相机等表现领域仍有合法退出曲线
- **THEN** Timeline MUST允许本次内容完成，尾段由表现 owner 的正式帧继续处理
- **AND** MUST不隐式延长 Skill/Action 生命周期或另建表现时钟

### Requirement: 片段生命周期必须与权重数值分离

进入、持续样本、一次触发和不同原因的离开/撤销 MUST由领域合同明确表达，不得仅用权重零猜测。跨片段/循环的生命周期 MUST携带本次调用和 cycle 的明确来源，同帧进入/退出不得被无条件合并删除。持续样本更新不得重建同一活动实例的效果时钟。

#### Scenario: 片段从零权重进入

- **WHEN** 持续片段进入时合法权重为零
- **THEN** 领域模块 MUST仍能取得进入身份与后续样本，不能把进入误判成停止

#### Scenario: 同帧穿过完整持续片段

- **WHEN** 一次推进同时跨过片段起止点
- **THEN** 领域模块 MUST按顺序收到必要进入与离开，不能只保留最终零权重

### Requirement: 内容与执行版本必须完整锁定

每次准备调用环境 MUST锁定内容、嵌套依赖、Numeric Target、类型/执行合同、状态布局和输出绑定版本。新内容 MUST经显式构建与正式发布采用；活动调用 MUST不原地换 Program、代码语义或目标身份。缺失能力、过期内容或版本失配 MUST明确失败，MUST不选择旧产物或默认实现。

#### Scenario: 共享子树发布新版本

- **WHEN** 共享子树改变且新产物尚未完整发布
- **THEN** 新调用环境 MUST拒绝不完整依赖组
- **AND** 已运行的旧环境 MUST继续保持自己锁定的合法版本
