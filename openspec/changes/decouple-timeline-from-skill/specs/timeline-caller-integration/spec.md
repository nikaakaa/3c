## Purpose

定义 Skill 和非 Skill 调用 Timeline 时如何提供实际目标、参数、状态和执行能力，以及时间、停止和输出归属，使 C# 直接使用 Timeline 成为完整的正式能力，并避免复制角色管线或让作者配置运行服务。

## ADDED Requirements

### Requirement: 调用输入必须按内容声明显式绑定

内容 MUST声明真正需要的外部目标与 typed 参数，调用方 MUST在开始前提供符合声明的值或身份。程序装配 MUST提供片段所需的领域执行能力，不得要求作者逐片段配置运行服务。构建和绑定 MUST检查全部可达内容的需求，缺失或类型错误必须定位到声明与使用位置。系统 MUST不按场景名称、Tag、selection、反射属性或全局服务查找补全绑定。

#### Scenario: 同一内容绑定不同目标

- **WHEN** C# 分别将同一展开 Timeline 绑定到两个合法表现目标
- **THEN** 每次调用 MUST作用于自己明确提供的目标
- **AND** 动画/曲线等已配置内容不需要重复作为调用参数传入

#### Scenario: 缺少目标

- **WHEN** 内容需要一个目标而调用方没有提供
- **THEN** 开始 MUST失败并指出需要目标的片段，不能自动搜索场景

### Requirement: 调用绑定必须与本 Tick 访问分离

固定调用值与目标身份 MUST按声明在开始时绑定，运行中变化的事实 MUST通过明确的当前帧读取合同获取。本 Tick 的状态事务、输出缓冲和时间访问 MUST只在本次推进有效，不得作为跨 Tick 对象保存在片段或子树中。运行接口和对象绑定 MUST不写回共享作者资产。

#### Scenario: 机关可用状态变化

- **WHEN** 独立 TreeClip 在后续帧读取已经改变的可用事实
- **THEN** MUST读取对应正式当前帧事实，不能沿用启动时偶然复制的旧值

#### Scenario: 请求替换目标

- **WHEN** 调用方需要将活动播放从一个目标改到另一个目标
- **THEN** MUST明确停止旧调用并建立新调用，不能静默替换现有目标

### Requirement: TreeClip 必须继承同次调用的声明绑定

TreeClip 与嵌套子树 MUST通过各自签名映射到本次 Timeline 已有绑定；值输入按声明捕获，动态事实通过声明读取，完成输出按签名返回。子树 MUST拥有独立局部状态和调用身份，不得重新寻找 Skill、隐式获取未声明服务或共享另一播放的变量。中止调用 MUST不提交未完成返回值。

#### Scenario: 两个并行子树读取不同参数

- **WHEN** 两个调用点引用相同子树并提供不同参数
- **THEN** 两者 MUST分别读取各自输入并保持各自变量和等待状态

### Requirement: Skill 必须在原有动作与模拟链中调用共用执行

Skill 调用 MUST由当前角色及唯一 ActionInstance 的技能执行范围提供已有信息，Timeline 和子树状态 MUST属于其 SkillExecutionState。准入、替换、打断、目标快照及动作生命周期 MUST继续属于原 Action 合同；Timeline MUST不建立第二 SkillInstance 或动作上下文镜像。Skill 的时间与结果 MUST继续走现有 SimulationTick、Evaluate/WorldResolve/Finalize 和唯一提交链。

#### Scenario: 作者在技能中加入 Timeline

- **WHEN** 技能执行到已绑定的 Timeline 调用点
- **THEN** 系统 MUST自动带入当前角色和本次释放的正式信息
- **AND** 作者 MUST不额外配置一份角色对象、播放服务或动作身份

#### Scenario: 同 Tick 取消窗口

- **WHEN** 活动技能 Timeline 的 Decision 内容在本 Tick 提供窗口候选
- **THEN** 角色代码决策 MUST在同 Tick 读取该候选，Commit 内容按正式技能阶段执行

#### Scenario: 恢复停止中的技能

- **WHEN** 正式快照包含尚未完成停止的 Timeline 和 TreeClip
- **THEN** 恢复 MUST继续相应实例的停止进度，不能重新激活或遗漏子树

### Requirement: 非 Skill 调用必须具备完整独立生命周期

系统 MUST提供由业务 C# 使用的正式准备、开始、推进、状态查询、停止和销毁入口，所需信息来自精确内容、调用方、目标/参数和已装配能力。该入口 MUST无需 Character Definition、角色组件、Skill、ActionProfile 或 ActionInstance；MUST复用共用编译内容、时间及树执行，而不是伪造角色或运行作者对象。调用方 MUST唯一拥有本次播放状态与推进入口，播放身份和 generation MUST区分并发与重复调用。

#### Scenario: 没有角色的场景运行 Timeline

- **WHEN** 合法场景调用方提供已发布内容及全部绑定
- **THEN** Timeline MUST能完成开始、推进、查询、停止与销毁
- **AND** MUST不创建角色运行包、角色状态或空技能

#### Scenario: 旧播放句柄被再次使用

- **WHEN** 已结束调用的句柄尝试控制后续新调用
- **THEN** 系统 MUST拒绝该访问，不能影响复用存储的新实例

### Requirement: 技能相机片段必须按正式领域合同传递来源与停止

Skill 相机片段 MUST经相机领域正式编译绑定和已提交输出传递内容调用来源、ActionInstance 关联、producer generation、cycle 和具有明确含义的采样时间。不同释放/调用/cycle 必须可区分，MUST不以零值或资源 identity 代替缺失来源。Clip 权重/缓动采样必须有唯一 owner，相机资源自身包络与表现时间由相机领域处理。自然完成、取消/中断、事件撤回/替换和 force teardown MUST按同一领域合同传递，不能仅以零权重表达全部停止。非 Skill 缺少正式相机能力时必须在开始前拒绝。

模拟纠正后，上游 MUST沿唯一表现输出链提供当前有效请求和失效来源的停止。Camera MUST使用既有本地播放、更新、混合和淡出处理结果，MUST不保存或恢复网络历史镜头；角色及动画既有恢复链 MUST保留。

#### Scenario: 同一相机片段由两个技能释放使用

- **WHEN** 两个合法释放或调用同时使用同一资源
- **THEN** 已提交输出 MUST保持各自完整来源，一个退出不得撤销另一个

#### Scenario: 重复更新同一持续相机效果

- **WHEN** 同一播放来源产生下一次合法持续样本
- **THEN** MUST更新该实例而不是重新进入或重启相机效果时钟

#### Scenario: 强制停止单个相机来源

- **WHEN** 一个 Timeline 来源被 force teardown
- **THEN** MUST精确通知相机清理该来源的活动与待发内容，不能重置整个相机或影响其他释放

#### Scenario: 预测来源在模拟纠正后失效

- **WHEN** 已开始的相机效果对应来源被最终模拟结果撤销
- **THEN** 上游 MUST提交该来源的正常停止，相机按既有规则从当前画面过渡
- **AND** 相机 MUST不倒回或重放历史镜头

### Requirement: 非 Skill 帧与领域输出必须具有明确权限

本地非 Skill 场景表现调用 MUST使用其业务 owner 的正式帧输入，先准备事实与待提交状态，执行纯 Decision，再执行 Commit 内容，成功后发布本帧状态和受限输出。Decision MUST不返回 Running 或产生副作用。独立调用 MUST不直接修改 Character/World 状态、角色骨骼或碰撞控制；内容要求模拟能力时必须由正式模拟环境提供，否则构建/绑定 MUST拒绝，不得自动添加空 Pass 或降级为场景写入。

#### Scenario: 本地帧执行失败

- **WHEN** 某个必要片段执行失败
- **THEN** 调用 MUST报告失败并拒绝发布本帧待提交内容，不得继续作为正常成功帧运行

#### Scenario: 独立内容需要 MotionWarp

- **WHEN** 内容声明角色 MotionWarp，而调用环境只有场景表现参数能力
- **THEN** 绑定 MUST在开始前拒绝并指出缺失的角色/运动合同

### Requirement: 独立能力必须交付真实目标与 TreeClip 用例

系统 MUST交付一个通过正式非 Skill 入口运行的场景表现参数用例：同一 Timeline 包含输出标量展开程度的曲线片段和读取当前帧可用事实、输出布尔显示状态的条件 TreeClip，绑定两个明确的非碰撞表现目标，各自拥有播放状态。字段、编译、执行、输出接收、配置和诊断 MUST全部接通，MUST不以空接口、日志片段、伪造角色或临时 fixture 代替。参数写入 MUST限定为领域公开的 typed 参数，不允许任意对象反射写入；同一目标同一参数在同一采样范围存在多个写入来源时 MUST明确拒绝，不能按轨道或调用顺序覆盖。

#### Scenario: 两个面板分别展开

- **WHEN** 同一内容通过 C# 分别绑定两个正式表现目标，并为两次调用提供不同输入
- **THEN** 两次播放 MUST按各自输入产生可观察表现，停止一个不得停止另一个
- **AND** 条件 TreeClip 与曲线片段 MUST通过同一领域输出合同作用于目标

#### Scenario: 两次调用争用同一表现参数

- **WHEN** 一个参数已被活动调用占用，另一调用尝试写入同一目标的该参数
- **THEN** 绑定 MUST明确拒绝并保留原调用，原调用停止后才能释放该占用

### Requirement: 独立播放观察必须复用正式诊断与场景所有权

Timeline 编辑器 MUST通过明确调用方与播放身份观察正式执行。运行目标、作者页面和历史游标 MUST分别表示其真实含义。场景预览 MUST由既有 ScenePlay owner 编排，Timeline 窗口不得创建第二播放会话或通过任意 seek 修改 Gameplay。

#### Scenario: 同一资产存在两个活动播放

- **WHEN** 作者选择查看其中一个播放
- **THEN** 时间、TreeClip 和变量观察 MUST只来自所选实例
- **AND** 窗口关闭或切换页面不得自行停止业务 owner 的运行

### Requirement: Timeline 接入不得改变游戏 AI 的角色输入边界

游戏 AI MUST继续只通过正式 Character 输入请求控制角色，不得利用独立 Timeline 入口直接建立技能释放、播放角色片段或修改角色/世界状态。插件 AI 的树、变量和调试属于其正式 AI 方案，Timeline 不把插件图编译为 TreeClip，不新增插件 AI Document。删除旧 AI 专用实现时 MUST保留仍被 Skill/Timeline 使用的共用树控制、作者和状态访问能力。

#### Scenario: AI 请求角色施放技能

- **WHEN** 游戏 AI 需要角色执行一个技能
- **THEN** 请求 MUST进入正式角色输入与唯一 Action 准入，不能直接调用技能或独立 Timeline 播放入口

#### Scenario: 清理旧 AI 树执行代码

- **WHEN** AI 迁移删除旧专用调用者
- **THEN** 仍有 Skill/Timeline 消费的共用控制和停止实现 MUST保持可用，不能整目录删除或另起副本
