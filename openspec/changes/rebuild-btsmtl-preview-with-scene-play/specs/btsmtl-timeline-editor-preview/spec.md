## REMOVED Requirements

### Requirement: Timeline 编辑器预览使用管线预览会话

**Reason**：窗口独立推进表现会话已由场景真实运行替代。
**Migration**：播放、暂停、停止与目标连接迁入统一场景预览操作；Timeline 页面保留作者数据、编辑游标和本地运行观察。

### Requirement: Timeline 编辑器预览目标来自正式管线预览目标

**Reason**：预览目标不再是提供独立动画求值的场景组件。
**Migration**：从本次预览场景声明的正式角色或非 Skill 调用方中精确选择，消费对应实例身份，删除旧预览目标抽象及其窗口字段。

### Requirement: Timeline preview session 必须隔离动画生命周期状态

**Reason**：窗口不再创建自己的 Action、动画工作区和物理输出生命周期。
**Migration**：动画状态由各真实 Actor 独立拥有；多窗口只共享观察数据，受控场景运行统一拥有控制权限。

### Requirement: 预览采样必须复用正式动画Selection与Pose Plan

**Reason**：编辑游标生成 Selection 和独立 seek 重建不再是完整角色预览行为。
**Migration**：通过正式输入运行角色，由实际 Action 和 Pose Plan 产生结果；编辑游标和 Capture 历史不执行角色。

### Requirement: Timeline Editor 必须分离 Authoring Preview 与 Live Debug

**Reason**：表现专用 Authoring Preview 模式被移除。
**Migration**：采用作者编辑、受控场景预览操作和只读真实运行观察；外部 Live 运行不能被预览控制抢占。

### Requirement: Timeline Preview 必须按正式阶段展示 TreeClip

**Reason**：旧合同禁止预览运行 Gameplay，并允许独立纯表现采样，已被场景真实运行替代。
**Migration**：采用“Timeline场景预览必须按正式阶段展示TreeClip”，保留阶段与声明显示，真实执行只来自角色正式 Session 或 Timeline 的正式非 Skill 调用方。

### Requirement: Timeline Field内部交互、几何与渲染必须分属明确模块

**Reason**：旧合同要求保留 Authoring Preview 模式及其窗口 session adapter。
**Migration**：采用“Timeline Field必须隔离作者交互、几何绘制与场景运行观察”，完整保留原有交互、几何、绘制和 identity 责任，迁移运行入口。

## MODIFIED Requirements

### Requirement: Timeline 资产不保存编辑器播放状态

Inline TimelineData、shared TimelineAsset 及其持有的 TimelineData MUST只保存作者数据和正式内容输入声明，不得保存预览场景运行、实际目标/运行服务绑定、Actor、playback generation、当前运行时间或动画资源状态。编辑游标和窗口选择 MUST保存在本地视图；技能运行状态 MUST位于正式 Actor 的 ActionInstance 所有的 SkillExecutionState，非 Skill 运行状态 MUST由 Timeline 正式调用方拥有。窗口 MUST消费对应 owner 的调用/播放 identity、generation、Timeline activation 和 cycle，区分共享模板的每次使用，不把全部 Timeline 状态强制放进技能实例。

#### Scenario: 两个页面预览同一个 shared Timeline

- **WHEN** 两个作者页面打开同一个 shared TimelineAsset
- **THEN** 页面 MUST分别保存自己的编辑游标、选择和运行观察绑定
- **AND** 关闭或移动一个页面的游标 MUST不改写资产、另一页面或真实运行

#### Scenario: 预览 inline Timeline

- **WHEN** 正式角色的技能 Root 经所属 ActionInstance 执行 TimelineNode 的 inline Timeline
- **THEN** 运行时间、Track/TreeClip 状态 MUST只存在于正式运行数据
- **AND** MUST不创建作者 Timeline 工作副本作为另一套运行源

#### Scenario: 同一shared内容供独立调用使用

- **WHEN** 无 Skill 的正式业务 owner 使用已发布 shared Timeline 产生一次播放
- **THEN** 播放头、Track/TreeClip 和停止状态 MUST只归该正式播放状态所有
- **AND** 作者资产和窗口 MUST不保存运行副本或创建假 ActionInstance

### Requirement: 旧 TimelinePlayer 预览路径必须删除

系统 MUST删除旧 TimelinePlayer、旧对象式自主 Timeline Bind/Evaluate/Unbind 和完整角色的窗口级预览播放器依赖。Timeline 作者入口 MUST统一使用独立场景 Play 运行操作，不得保留旧 target、独立动画时钟或兼容播放分支。Timeline change 提供的共用编译执行及非 Skill 正式绑定/生命周期 MUST继续归其业务 owner，不因旧播放器清理而被删除或迁入窗口。

#### Scenario: 打开旧Timeline资产

- **WHEN** 作者打开已有 inline 或 shared Timeline 并请求预览
- **THEN** 播放入口 MUST只进入统一场景预览合同
- **AND** MUST不根据资产年代或窗口类型恢复旧播放器

#### Scenario: 搜索旧播放器入口

- **WHEN** 迁移完成后检查正式 Timeline 播放入口及其调用链
- **THEN** MUST不存在旧 TimelinePlayer、旧对象式自主 Timeline 绑定或窗口级完整角色预览播放器
- **AND** 所有完整角色播放入口 MUST指向统一场景运行操作

### Requirement: Timeline Live Debug 必须显示真实 runtime membership

Timeline Live Debug MUST从正式 provider 显示实际播放 identity/generation、内容/产物来源、完整调用路径、active Track/Clip、TreeClip phase/runtime 和 terminal state。技能调用 MUST进一步消费 current Action playback summary 中的 SkillDefinition/SkillProgram、Actor/ActionInstance、技能 Graph/Node 与关联控制代码来源，以及实际存在的 Action Selection、PendingFirstSample/Selected/Retained/Retired、AnimationSlot/PoseNode。非 Skill MUST消费业务 owner/播放 identity、正式调用点/generation 和 Timeline 内容来源，不要求技能、角色动画或 Action 字段。

来源 MUST消费统一代码/operation合同，不伪造角色总控Graph节点。同模板不得合并实例；Tree-only技能没有Timeline必须正常显示未执行，而非缺少唯一Timeline错误。PoseState source usage属于Pose Graph Live Debug；Timeline MAY提供只读导航但不得伪装成Timeline playback。停止Capture后，它 MUST在共享Capture history position显示对应历史事实，不得根据当前authoring time重新采样来猜测membership。

#### Scenario: Decision TreeClip active

- **WHEN** 所属ActionInstance的SkillProgram在某SimulationTick按正式阶段评估Decision TreeClip operation
- **THEN** Timeline Live Debug MUST在对应 Clip 上显示该 tick 的 Decision evaluation
- **AND** UI MUST能关联写入的 Blackboard declaration identity

#### Scenario: visual time 位于两个 logic tick 之间

- **WHEN** PresentationFrame 以 interpolation alpha 计算 visual Timeline time
- **THEN** Timeline Live Debug MUST分别显示 logic time 与 visual time
- **AND** animation playhead MUST使用 visual time
- **AND** gameplay window/TreeClip decision 标记 MUST使用 logic tick

#### Scenario: 多个 playback 使用同一 Timeline source

- **WHEN** 同一 Timeline source 同时存在多个 playback instances
- **THEN** Timeline Editor MUST 为每个 playback 显示其领域的正式调用方/实例身份、完整调用路径、playback id、内容/产物与代码/operation 来源、activation generation 及 terminal/lifecycle 摘要；技能包括 SkillDefinition/SkillProgram 和 ActionInstance，非 Skill 包括业务 owner 和播放 identity
- **AND** Timeline 窗口 MUST 要求作者在本地 binding 中 Pin 其中一个，或显式保持 Follow
- **AND** 系统 MUST NOT 按列表顺序静默选择赢家

#### Scenario: 当前 Timeline 未执行

- **WHEN** 已附着 target 的共享 current state 不包含当前 Timeline 的 playback
- **THEN** Timeline Editor MUST 显示当前正式调用方未执行该 Timeline 的状态
- **AND** MUST NOT 调用 TimelinePreviewSession、preview evaluator 或 authoring time 重采样

#### Scenario: 同一子图中Timeline被两次调用

- **WHEN** 同一技能释放的两个合法调用复用 shared Timeline
- **THEN** Timeline 观察 MUST按完整调用路径/运行 generation 和 playback activation 区分进度
- **AND** MUST不因 Timeline source 相同而合并两份状态

#### Scenario: 独立播放没有角色动画来源

- **WHEN** 正式非 Skill Timeline 只输出受限场景表现参数
- **THEN** 观察 MUST显示该调用方的实际内容、TreeClip 与输出状态
- **AND** MUST不把缺少 ActionInstance、AnimationSlot 或角色 visual sample 视为错误，也不生成假字段


## ADDED Requirements

### Requirement: Timeline必须区分作者编辑与真实运行观察

Timeline 页面 MUST明确区分编辑游标、当前正式运行标记和 Capture 历史位置。场景预览和外部 Live MUST复用正式增量诊断与窗口本地运行绑定；运行观察内容 MUST只读。领域允许的作者调参 MUST位于明确的作者字段，通过共享 Mutation 和该领域精确运行目标的正式参数端口执行，不能修改观察字段或其它窗口绑定。非 Skill 没有局内更新合同时 MUST要求正式构建采用，不借用 Actor Pose 参数端口。

#### Scenario: 多个playback使用同一Timeline

- **WHEN** 当前 Actor 的多个 ActionInstance 或技能调用指向同一 Timeline source
- **THEN** 窗口 MUST明确选择 playback instance 并显示其实际来源
- **AND** MUST不按当前编辑游标推断正在执行哪个实例

#### Scenario: 重载后恢复Timeline页面

- **WHEN** 页面经历 Domain Reload 或预览场景重建
- **THEN** 页面 MUST从原正式作者根和稳定调用路径恢复文档；技能按新 Session/Actor 和明确 ActionInstance/调用generation 绑定，独立 Timeline 按新场景声明的业务 owner、实际播放 identity/调用generation 绑定
- **AND** 定位无效时 MUST显示不可用，不猜测另一个 Timeline 或恢复旧播放器

### Requirement: Inline与Shared Timeline必须复用同一场景预览入口

Inline Timeline、shared Timeline 与 TreeClip 下钻页面 MUST复用同一场景运行操作，页面导航 MUST不启动、接管或销毁正式运行。只有能关联所选 Actor 的 SkillDefinition/技能调用点与合法输入的内容才提供技能试验入口；入口 MUST经过 C# 控制与唯一 Action 服务启动技能，再观察实际产生的 Timeline，不能直接播放该 Timeline。Tree-only 技能仍可从技能 Root 试验，多个/嵌套 Timeline 只要求明确选择编辑和观察目标。

shared Timeline 作为独立正式根时，MUST允许消费已声明的非 Skill 调用方及目标/参数绑定，经其公开业务入口开始并观察实际播放；MUST不要求角色技能引用。内容/输入声明/独立根作者入口由 Timeline owner 提供，预览只连接同一场景操作和本地运行绑定。没有任一合法正式调用方时 MUST保留编辑并显示未绑定，不创建默认调用方或播放器。

#### Scenario: shared Timeline没有任何正式调用绑定

- **WHEN** shared Timeline 既没有可定位的角色技能调用点，也没有场景已声明的合法非 Skill 调用方
- **THEN** 作者 MUST能够编辑该资产，但试验入口 MUST显示缺少正式调用绑定
- **AND** MUST不临时创建 producer、Action 或图节点

#### Scenario: 未被角色引用但存在正式非Skill调用方

- **WHEN** shared Timeline 未被任何角色技能引用，但所选场景具有匹配正式产物和目标绑定的非 Skill 调用方
- **THEN** 作者 MUST能通过同一场景操作请求其正式调用并绑定实际播放
- **AND** MUST不要求添加 Character Definition、技能目录项或假 TimelineNode

### Requirement: Timeline场景预览必须按正式阶段展示TreeClip

Timeline Editor MUST继续显示 TreeClip 的 Decision/Commit 阶段、inline/shared ownership 和实际声明的输入/事实摘要。技能预览 MUST保留 Tree → Timeline → TreeClip → 子树，TreeClip 状态归准确 ActionInstance 及调用 frame；Decision 在控制决策前只准备本 Tick 合法窗口候选，Commit 内容在正式技能阶段执行，父级停止覆盖嵌套内容。C#控制、技能Program、Action服务、Blackboard、GameplayEffect、Motion与世界求解 MUST只由同一正式Session/Pipeline推进。

非 Skill TreeClip MUST消费 Timeline owner 提供的共用执行和显式能力/输入声明，状态归该业务 owner 的实际播放；观察其正式帧中的纯 Decision、Commit 和已提交受限输出，不伪造角色 Blackboard、ActionWindow 或技能阶段。所需 Character/World 能力缺失 MUST报告正式不可用，不通过本地表现写 Gameplay。窗口 MUST只观察正式输出，不创建临时图上下文、运行树副本、额外 Advance 或第二解释器；编辑游标 MUST不执行 TreeClip，也不写作者默认值。

#### Scenario: 场景预览执行Decision TreeClip

- **WHEN** 角色正式运行到一个 Decision TreeClip
- **THEN** Timeline MUST显示同一逻辑 Tick 的正式执行和输出事实
- **AND** MUST不由窗口另行执行一次 Decision

#### Scenario: 没有正式运行目标

- **WHEN** 作者打开含 TreeClip 的 Timeline 但未建立合法场景运行绑定
- **THEN** 页面 MUST继续显示作者内容和缺失上下文原因
- **AND** MUST不创建替代 Session 或假运行结果

#### Scenario: 非Skill条件TreeClip执行

- **WHEN** 正式独立调用方在自己的业务帧执行合法条件 TreeClip
- **THEN** 观察 MUST按该次播放身份显示实际 Decision/Commit 和已提交结果
- **AND** MUST不重复求值、不创建 ActionWindow 或套用角色 Blackboard 声明

#### Scenario: 打开动画素材

- **WHEN** 作者从 Animation Segment 打开原生 AnimationClip
- **THEN** 页面 MUST导航到该素材的正式编辑工具
- **AND** 素材编辑 MUST不被标记为 Timeline Gameplay 已执行

### Requirement: Timeline Field必须隔离作者交互、几何绘制与场景运行观察

Timeline Editor MUST保留现有窗口、Timeline Field、Inspector 和真实运行观察入口；selection/drag/move/resize、time/frame/clip geometry 与 hit-test、track/clip/playhead/overlay rendering、窗口本地运行绑定 MUST由职责独立的模块拥有。selection MUST只读暴露并通过命令修改；interaction MUST依赖窄 host port；rendering MUST显式消费 geometry、viewport、playhead 和 overlay 输入，不反向读取完整 Timeline Field。作者修改 MUST通过唯一 Mutation/Undo；geometry 和 rendering MUST不写资产。场景运行控制 MUST交给共享场景操作，本地 binding MUST不拥有播放器。运行只读刷新 MUST保留焦点、未提交作者文本、选择、滚动和合法草稿，外部owner变化使草稿失效时 MUST明确报告，不写回过期数据。迁移 MUST保持 Timeline/Track/Clip identity、Source Map、Inspector selection 和多窗口页签行为。

#### Scenario: Resize一个Animation Clip

- **WHEN** 作者在 Edit Mode 拖动 Clip 边缘改变范围
- **THEN** interaction MUST使用 geometry 的 frame 结果生成一个正式 Mutation
- **AND** 同一个 Undo 边界 MUST更新原 Clip identity，rendering 只根据新数据重绘

#### Scenario: 点击右侧Inspector设置

- **WHEN** 作者选择 Clip 后修改允许编辑的作者字段
- **THEN** selection MUST保持同一 Clip authoring identity
- **AND** 重绘 MUST不清空或切换选择

#### Scenario: 作者页面连接场景运行

- **WHEN** 页面开始观察受控预览场景的真实 playback
- **THEN** 本地 adapter MUST建立准确运行绑定，运行事实和 overlay MUST只读；Timeline 作者结构、Clip、Section 和曲线 MUST继续经正式 Mutation/Undo 编辑
- **AND** 作者修改 MUST不直接改写当前运行状态；新内容由既有 Build/adoption 合同在同一 Session 采用，缺少即时采用能力不能被解释为禁止作者编辑
- **AND** geometry 与 rendering MUST复用同一作者 identity 显示真实 overlay，不创建预览 evaluator

#### Scenario: 多个playback overlay

- **WHEN** 同一 Timeline source 存在多个正式 playback
- **THEN** overlay MUST呈现各实例并服从 Follow/Pin 选择
- **AND** rendering MUST不按列表顺序选择赢家或重新执行角色
