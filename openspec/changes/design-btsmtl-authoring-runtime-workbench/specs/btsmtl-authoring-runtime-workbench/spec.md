## Purpose

定义基础内容编排、Ability 执行预览与真实运行诊断的工作职责，在同一作者工具中提供隐藏场景效果查看、随实际执行增长的时间线、历史状态观察与正式变量命令，保持唯一业务执行路径。

## ADDED Requirements

### Requirement: Authoring Runtime Workbench 必须明确三种产品形态

系统 MUST 将 Authoring、Preview 和 RuntimeDebug 定义为同一工作台的三种产品形态。Authoring（基础编排亦称 Assembling）MUST 负责作者数据编辑与纯 Timeline 效果查看；Ability Preview MUST 将正式 Session 中实际节点、决策、Loop 和调用展开为执行时间线；RuntimeDebug MUST 只读观察实际游戏或明确绑定的预览 Session。ScenePlay MUST 是 Preview 的运行底座，不得被定义为第四种产品形态。

#### Scenario: 作者编辑正式内容

- **WHEN** 用户处于 Authoring
- **THEN** 用户 MUST 能编辑正式 FlowCanvas、RootTree、子图、Timeline、Track、Clip、曲线和正式参数
- **AND** Authoring MUST 不创建第二个 Runtime、私有时钟或运行时 Graph clone

#### Scenario: 在真实场景中预览作者修改

- **WHEN** 用户处于 Preview
- **THEN** Preview MUST 连接正式 ScenePlay Session、Scene、Actor、Ability、RootTree、Timeline、Pose、Motion、Camera 和 World
- **AND** 作者修改 MUST 通过正式 Mutation、Export、Prepare、Publish 和 Adopt 进入运行
- **AND** Preview MUST 不使用独立 Timeline 播放器、CMC MontagePlayer 或 Pose fixture 代替正式运行链

#### Scenario: 只读观察运行事实

- **WHEN** 用户处于 RuntimeDebug
- **THEN** RuntimeDebug MUST 只读取 RuntimeDebugSession、SourceMap、Trace、Playback、Snapshot、Capture/History 和领域提交事实
- **AND** RuntimeDebug MUST 不修改作者数据、不创建运行实例、不重算 Graph/Timeline/Pose

### Requirement: Authoring Runtime Workbench 必须只有一个正式 ScenePlay Session

角色预览 MUST 绑定一个当前正式 Preview Session；合法非 Skill 内容 MUST 使用已有领域 owner，不强制角色 Session。RuntimeDebug MUST 只读连接明确目标，不为观察建立隐藏运行实例。页面、Graph、Timeline、FlowCanvas、Slate 和 RuntimeDebug MUST 只拥有本地视图绑定，不得按 Timeline、Graph、Actor 或窗口创建第二个 Scene、Actor、Session、时钟或执行器。普通页面切换或关闭 MUST 不停止 Session；明确结束预览与编辑器宿主销毁 MUST 沿正式生命周期释放。

#### Scenario: 切换 Authoring、Preview 和 RuntimeDebug

- **WHEN** 用户在同一正式目标的三种形态之间切换
- **THEN** Session、Scene、Actor、Playback 和运行版本 MUST 保持不变
- **AND** 切换 MUST 只改变当前工具表面和本地观察 interest

#### Scenario: 多个 Timeline 调用

- **WHEN** 同一 Session 多次调用同一 Timeline
- **THEN** Runtime MUST 通过 playback identity、调用点和 generation 区分实例
- **AND** UI MUST 不按名称合并不同调用或自动选择未声明的实例

### Requirement: Preview 作者修改必须经过发布与采用边界

Preview 中的作者修改 MUST 先写入正式作者数据，再通过 Export、Prepare、Publish 和正式 adoption barrier 进入 Session。Export MUST 冻结作者 Timeline 闭包；Plan MUST 绑定当前 Timeline Host 会话标识与内容代次；Publish 和 Adopt MUST 重新校验作者/content revision。旧 revision 在新 revision 准备期间 MUST 继续运行，活动 playback MUST 保持自己的冻结内容；UI MUST 分别显示作者版本、运行采用版本、导出/准备/发布状态、Session generation、RuntimeDebug target revision 和失败原因。Mutation 成功 MUST NOT 被解释为 Runtime 已生效。

#### Scenario: 兼容内容修改

- **WHEN** 作者修改参数、曲线、Clip 时间或正式合同允许的内容
- **THEN** 当前 Scene 和 Session MUST 保持运行
- **AND** 新 revision MUST 在正式安全边界采用
- **AND** Preview MUST 显示 `作者已修改`、`准备中`、`待采用` 或 `已采用` 的真实状态

#### Scenario: 作者在导出后继续修改

- **WHEN** 作者在 Export、Prepare 或 Publish 后继续修改同一 Timeline 闭包
- **THEN** 旧 Export、Plan 和 Publication MUST 被拒绝或作废
- **AND** 系统 MUST 为最新作者版本重新组织内容导出与采用，旧内容 MUST NOT 覆盖新的作者版本或当前采用版本

#### Scenario: 不兼容结构修改

- **WHEN** 作者修改状态布局、Composition、Scene、Actor roster、C# 代码或不兼容运行拓扑
- **THEN** 系统 MUST 明确显示需要重建、重新发布或新 Session
- **AND** MUST NOT 把新旧调用栈、Snapshot、SourceMap 或状态布局混合到同一个活动实例

### Requirement: RuntimeDebug 必须按真实调用栈切换 FlowCanvas 与 Slate

RuntimeDebug MUST 以正式调用栈为导航主线。RootTree 或子图执行时 MUST 显示 source-mapped FlowCanvas 只读状态；Timeline 执行时 MUST 显示对应 playback 的 Slate Timeline 只读状态；TreeClip 进入子图时 MUST 能导航到对应子图；调用返回时 MUST 回到父调用方。RuntimeDebug MUST 不为每个 Graph 或 Timeline 创建窗口。

#### Scenario: RootTree 调用子图和 Timeline

- **WHEN** 运行从 RootTree 进入子图、Timeline 和 TreeClip
- **THEN** RuntimeDebug MUST 按实际调用顺序更新路径
- **AND** 当前表面 MUST 在 FlowCanvas 与 Slate 之间切换
- **AND** 未执行的 Graph、Track 或 Clip MUST 不被当作本次运行内容提前显示

#### Scenario: 复用原 Timeline 面板进行导航

- **WHEN** RuntimeDebug 在 Graph、Timeline 和父调用方之间跟随
- **THEN** 作者 Timeline MUST 使用原 Slate 面板，独立 Preview MUST 复用 Slate 执行投影并允许从 Ability 图联动进入
- **AND** Graph MUST 使用已有 FlowCanvas 面板，导航 MUST NOT 按调用创建新窗口或将 FlowCanvas 嵌入 Slate
- **AND** 并行调用 MUST 要求显式 Pin，不得按事件列表顺序选择赢家

#### Scenario: 观察历史运行

- **WHEN** 用户查看 Capture 或 History
- **THEN** RuntimeDebug MUST 显示记录时的 SourceMap、调用 identity、时间和提交事实
- **AND** MUST 不使用当前作者数据重新求值历史结果

- **AND** 切换历史位置 MUST 按该位置重新建立已执行内容集合，较晚历史出现的 Track 或 Clip 不得残留到较早位置

### Requirement: Slate 和 FlowCanvas 必须只是工具表面

Workbench MAY 复用 Slate 的时间尺、Track、Clip、缩放、滚动和绘制能力，以及 FlowCanvas 的 Graph 画布和导航能力；但这些表面 MUST 不拥有业务时间、Runtime 状态、作者第二数据源或独立执行器。Authoring projection、Preview projection 和 RuntimeDebug projection 的数据来源 MUST 分开。

#### Scenario: Slate 显示 Runtime Timeline

- **WHEN** RuntimeDebug 聚焦 Timeline
- **THEN** Slate MUST 使用 Runtime observation projection 显示真实 playback、Track、Clip、游标和生命周期
- **AND** Slate MUST NOT 通过完整作者 TimelineData 补齐未执行内容或自行估算时间

### Requirement: Preview 必须使用独立可停靠窗口

原 FlowCanvas MUST 负责节点图编辑与运行高亮，原 TimelineEditorWindow MUST 负责作者 Track/Clip/Curve 编排。Preview MUST 使用独立可停靠窗口，上区承载角色视口和当前实例黑板，下区承载执行时间线，分区尺寸 MUST 可拖动调整。执行时间线 MUST 复用 Slate 及正式记录，MUST NOT 新建另一套 Timeline Renderer、Session Dashboard 或业务执行链。打开 Preview MUST 自动装配唯一隐藏宿主，重复打开 MUST 聚焦既有窗口；常用 UI MUST 不要求操作 ScenePlay 生命周期术语。

#### Scenario: 并排编辑节点图与预览

- **WHEN** 作者从 Timeline 或菜单打开 Preview 并将其与 FlowCanvas 并排停靠
- **THEN** 原作者画布 MUST 保留完整编辑区域，Preview MUST 显示共享角色结果和执行过程
- **AND** 双击执行片段 MUST 导航原 FlowCanvas 或作者 Timeline，不按调用创建窗口
- **AND** 所有窗口 MUST 共享同一隐藏 Scene、Session、时钟和 Renderer

#### Scenario: 关闭作者窗口与预览窗口

- **WHEN** 用户关闭原 Timeline 或 FlowCanvas，Preview 仍打开
- **THEN** 隐藏预览 MUST 继续由原宿主拥有
- **WHEN** 最后一个 Preview 承载窗口关闭
- **THEN** 隐藏宿主 MUST 停止驱动并释放运行资源，实际游戏 Session MUST 不受影响

### Requirement: Scene 与 Actor 必须由单一 Profile 选择

系统 MUST 使用 `BtsmtlScenePlayProfile` ScriptableObject 保存正式装配 AssemblyPrefab、ContextId 和 DefaultActorId。Preview 窗口与 Timeline 入口 MUST 共享同一 Profile 选择，不得展开装配、Context、Actor、Composition、World 或 Camera 的重复配置。正式游戏场景与隐藏预览 MUST 复用同一装配 Prefab。Profile MUST NOT 保存 Session、runtime identity、revision、播放时间、暂停或诊断状态。

#### Scenario: 使用 Profile 进入 Preview

- **WHEN** 作者从 Timeline 或菜单打开 Preview
- **THEN** 系统 MUST 从当前 Profile 读取 AssemblyPrefab、ContextId 和 DefaultActorId，并实例化到隐藏 Preview Scene
- **AND** ScenePlay MUST 先将 ContextId 精确匹配正式 Composition 的 SessionId，再在该 Session roster 中校验唯一 Actor
- **AND** Profile 无效时 MUST 只显示失败原因并拒绝伪造运行目标

### Requirement: 打开预览必须自动装配并仅在等待或失败时提示

Workbench Preview MUST 在 Edit Mode 内采用 CMC 式隔离隐藏 Scene，MUST NOT 启动 Unity Play。打开预览区 MUST 自动创建和准备正式 Session，正常使用 MUST NOT 暴露准备操作步骤；实际耗时或失败时 MUST 显示等待原因、失败原因和采用状态。系统 MUST NOT 为了隐藏等待创建 Edit Mode 假 Runtime 或 CMC 平行播放器。进入 Preview 后，正常作者修改 MUST 尽量保持 Scene、Session、Actor 和 RuntimeDebug 绑定不变。

#### Scenario: Preview 首次准备

- **WHEN** 用户从 Authoring 进入 Preview
- **THEN** 系统 MUST 自动在隐藏 Scene 内装配正式 Session，不要求作者执行准备步骤，仅在实际耗时等待或失败时提示原因
- **AND** 准备完成前 MUST 不显示伪造的运行结果

### Requirement: 隐藏场景承载必须复用正式业务执行

系统 MUST 采用 CMC 式隐藏场景和无需进入 Play 的交互方式，但 BTSMTL Preview MUST NOT 使用 CwcMontage 的独立 PlayableGraph、MontagePlayer、动作块生命周期或局部时钟作为运行真相。正式结果 MUST 由唯一 ScenePlay Session 发布。

#### Scenario: CMC 编辑体验参考

- **WHEN** 设计 Preview 的编辑响应和局部刷新
- **THEN** 允许复用 CMC 的交互经验
- **AND** 不得产生第二套 Graph、Timeline、Pose 或 RuntimeDebug 执行路径

### Requirement: 基础编排必须直接提供纯 Timeline 效果预览

Authoring MUST 支持片段、曲线、动画、特效和镜头的编排以及播放、暂停和拖动查看效果，MUST NOT 要求用户先进入 Ability Preview 或构造 Ability 图。效果 MUST 由正式 Timeline owner 与内容所需的明确目标绑定产生；角色内容使用正式 Session，合法非 Skill 内容保持领域归属，MUST NOT 伪造角色或私有播放器。

#### Scenario: 调整纯 Timeline 片段并查看效果

- **WHEN** 作者在基础编排中移动片段并拖动时间尺
- **THEN** 修改 MUST 进入正式作者数据，时间定位 MUST 请求正式 owner 并显示实际完成结果
- **AND** MUST NOT 自动切换成 Ability 执行时间线或把请求游标冒充实际运行位置

### Requirement: Ability Preview 必须按实际执行展开时间线

打开 Ability MUST 能查看节点图及其执行投影。正式运行产生的持续节点、决策、Loop 每次迭代、子图与 Timeline 调用 MUST 按实际发生位置显示为片段或瞬时标记；执行时间线 MUST 随提交进度增长。活动区间 MUST 在退出前开放，退出后固定；瞬时决策 MUST 显示实际条件值与分支，不伪造持续时间。记录 MUST 区分运行代次、Ability 实例、父子调用、节点发生与 Loop 迭代，并关联 Timeline playback/cycle。投影 MUST NOT 保存为作者 Clip、反写作者资产或通过拖拽改写历史。

#### Scenario: 循环两次后选择不同分支

- **WHEN** 同一节点在 Loop 两次迭代中被执行并选择不同分支
- **THEN** 时间线 MUST 分别显示两次迭代、各自条件值、分支和子调用
- **AND** 后一次 MUST NOT 覆盖前一次，未发生的分支 MUST NOT 显示为已执行

#### Scenario: 子调用与并发区间

- **WHEN** Ability 调用子图和 Timeline，或存在并发调用
- **THEN** 时间线 MUST 保留父子归属并分开显示不同实例，可展开及定位正式来源
- **AND** 展示所有实际记录 MUST NOT 依赖选择一个 Pin，自动聚焦存在歧义时才要求显式选择

#### Scenario: 投影增长不改变作者固定区间

- **WHEN** 执行时间线增长并显示 FrameBoundary Clip 的调用
- **THEN** 作者固定区间 MUST 保持其正式边界
- **AND** 只有声明 TreeDecision 的作者 TreeClip MUST 使用动态退出规则，普通节点投影片段 MUST NOT 变成 TreeClip 资产

### Requirement: Preview 执行游标必须显示对应历史状态

用户 MUST 能拨回执行游标查看当时的节点位置、变量和角色情况。历史事实 MUST 使用记录时的来源与版本，MUST NOT 借用未来退出、后续迭代或当前作者图补推。角色结果 MUST 来自记录的正式表现状态或正式恢复/重放；缺少数据或恢复能力 MUST 明确显示，不把实时角色当成历史画面。只读 Capture 浏览 MUST NOT 修改运行目标；需要恢复当前预览 Session 时 MUST 暂停并明确采用正式恢复流程，不另造执行器或隐式影响实际游戏。

#### Scenario: 从第二轮循环回到第一轮中途

- **WHEN** 用户拨回第一轮尚未退出的位置
- **THEN** 节点、变量与片段 MUST 对应第一轮当时的事实，尚未退出区间 MUST 截止该观察位置
- **AND** 角色画面 MUST 对应该历史位置，或明确显示未能恢复，MUST NOT 使用第二轮结果冒充

### Requirement: Preview 运行变量修改必须通过正式命令采用

Ability Preview MUST 按既有黑板作用域、生命周期和读写规则为当前实例提供正式写入入口，不新增逐变量可调勾选或永久覆盖层；实例结束后的请求 MUST NOT 写到后继实例。入口 MUST 记录请求值、采用值和采用时间，由后续正式节点执行消费。诊断事实、历史值和节点私有状态 MUST 只读；运行变量修改 MUST NOT 自动保存成作者默认值。作者资产修改 MUST 使用 Mutation/Undo 与发布采用合同。无运行修改合同的字段 MUST 显示实际限制，不使用反射或临时变量副本绕过执行链。

#### Scenario: 调整变量改变下一次决策

- **WHEN** 用户提交一个正式允许修改的预览变量
- **THEN** 当前预览实例 MUST 在正式边界采用，后续决策 MUST 读取实际采用值
- **AND** 时间线 MUST 显示真实变化位置，既有历史 MUST 不被重写，作者资产 MUST 不被自动修改

#### Scenario: 实时刷新期间编辑变量草稿

- **WHEN** 用户选择当前实例的变量并输入新值，期间其它变量产生新记录或列表复用行
- **THEN** 草稿 MUST 保持绑定本次选择的实例、作用域代次和编译版本，不被实时值覆盖，也不随列表行换绑目标
- **AND** 提交 MUST 使用正式精确值和领域类型；历史、其它运行目标或已结束预览 MUST NOT 接收该草稿写入
- **AND** UI MUST 区分请求待采用、已采用位置与拒绝原因；暂停时 MUST 等待用户播放或单步

#### Scenario: 调值候选未提交

- **WHEN** 变量写入在候选求值中被采用，但该角色与世界候选尚未原子发布或已被丢弃
- **THEN** 请求 MUST NOT 被标记为已采用或提前移出待处理队列
- **AND** 只有正式状态发布后的结果 MUST 向工具报告采用值和 Tick；同一外层事务内 MUST NOT 重复消费请求

#### Scenario: 调值目标已结束或换代

- **WHEN** 命令目标的技能释放实例、Graph 或 State 作用域已结束或换代
- **THEN** 正式黑板模块 MUST 拒绝对后继实例写入，并返回实际失效原因
- **AND** Frame 变量 MUST 在正式采用 Tick 生效并继续遵守 Frame 生命周期，不形成永久覆盖

#### Scenario: 查看尚未执行写入的变量

- **WHEN** 用户查看一个已创建运行实例的黑板，该变量尚未执行写节点
- **THEN** 系统 MUST 从正式已提交状态和编译默认值提供只读快照，并显示作用域有效性及 Config 只读限制
- **AND** MUST NOT 通过显示动作创建运行实例、Materialize 状态或把当前作者默认值冒充运行值
- **AND** Capture 首次记录与 Live 重新订阅 MUST 补齐快照；历史游标 MUST 按已有快照和写入事件的顺序读取，缺少记录时显示缺失

### Requirement: 暂停与关闭预览不得持续占用编辑器执行

暂停且没有新命令时，Preview MUST 保留现场并停止逻辑与表现求值，MUST NOT 按墙钟重复提交零增量表现帧或触发持续重绘。关闭预览 MUST 释放其正式运行对象并撤销驱动订阅。准备、扫描与资源构建 MUST NOT 在 Inspector 或重绘中重复执行。用户 MUST 能继续操作作者界面，不能被强制等待 Timeline 播放结束。

#### Scenario: 暂停后继续编辑

- **WHEN** 用户暂停 Preview 并编辑作者字段，没有提交播放或单步命令
- **THEN** 正式逻辑位置与表现帧 MUST 保持不变，编辑交互 MUST 不被播放循环阻塞
- **AND** 用户提交一次单步后 MUST 只执行所请求 Tick，再返回暂停
