# btsmtl-timeline-direct-runtime Specification

## Purpose

定义 Timeline 正式内容从准备、实例创建、分步推进到提交、丢弃和停止的直接运行合同。Timeline Runtime 直接消费唯一作者数据及其 portable 表示，在技能与非技能调用中复用同一执行链，并把 Gameplay、表现和诊断结果交给各自领域 owner；不生成第二套 Timeline IR、操作表、播放器或提交路径。

## Requirements

### Requirement: Timeline必须直接执行同一正式只读内容

Timeline Runtime MUST直接消费正式轨道、Clip类型/区间/参数/顺序、Section、稳定身份及资源引用，MUST NOT把轨道/Clip编为Semantic operation或在加载时生成操作表。技能调用操作 MAY引用Timeline identity/revision及入参，但 MUST NOT展开其内部执行。技能与真实非Skill调用 MUST使用同一Runtime，MUST NOT恢复旧TimelinePlayer、TimelineRunningTree自主播放器或Slate Runtime。

#### Scenario: 技能与独立调用复用内容

- **WHEN** 技能与非Skill调用方请求相同Timeline内容
- **THEN** 两者 MUST经同一准备/实例/推进接口，仅调用上下文不同，每次播放状态隔离
- **AND** 非Skill调用 MUST有真实调用方、typed能力和播放身份，不得包装成空技能或假角色

### Requirement: Portable Timeline必须与作者字段一一对应

TimelineData MUST保持唯一可写作者数据，普通.NET使用同字段/类型语义的只读portable表示，Unity引用 MUST转换为稳定资源或技能引用。Portable内容 MUST NOT包含Unity/Slate对象、IR、控制流、操作码、角色状态槽或可变播放状态。Float32/Fixed MUST共用调度含义，只在准备中绑定数值及资源。

#### Scenario: 普通NET加载内容

- **WHEN** 普通.NET为指定NumericTarget准备Timeline
- **THEN** MUST直接解析正式portable内容与资源依赖，不访问Unity作者资产或Character Frontend
- **AND** 内容identity/revision、顺序与字段 MUST保留，不生成另一种Timeline执行语言或第二份可写资产

### Requirement: Timeline必须提供独立准备和播放实例创建

Prepare MUST接收RequestId、精确内容identity/revision、NumericTarget、目标/外部成员及资源合同、唯一运动映射与TreeClip服务需求，返回分型就绪状态及准确Missing/Invalid/Failed原因，只有Ready可以带PreparedBinding。CreatePlayback MUST接收就绪绑定和精确调用/播放identity与generation，返回独立实例及实际绑定版本，不借用Ability Prepare隐藏独立Timeline职责。

#### Scenario: 准备缺少TreeClip服务或资源

- **WHEN** 正式内容声明的技能入口、资源版本或typed成员无法解析
- **THEN** Prepare MUST返回该依赖的精确失败，不回读旧Program、不搜索资源fallback、不用空服务返回Ready
- **AND** 准备结果 MUST NOT被当作实际播放或已采用，实例创建/安装由对应owner报告真实版本

### Requirement: Timeline推进必须服从调用方Step提交

Advance MUST只产生本次候选播放状态、领域贡献、窗口/TreeClip结果及表现/trace候选，MUST NOT提前CommitFrame或发布角色Gameplay副作用。调用方统一Step决定Commit或Discard；Timeline Commit只安装本实例私有候选，角色/World/表现发布 MUST仍归各自owner。

#### Scenario: 同一步World处理失败

- **WHEN** Timeline已Advance但调用方未接受本Step
- **THEN** Discard MUST丢弃Timeline及其TreeClip服务候选与输出，保持上次committed状态
- **AND** MUST NOT泄漏窗口、Motion、Cue、结果或trace成功事实，不得在Timeline内再开启一个自动提交路径

#### Scenario: 接受推进结果

- **WHEN** 调用方接受指定Step的候选结果
- **THEN** Commit MUST只接受对应实例/generation/Step的结果并安装私有状态，不重新执行技能逻辑
- **AND** 正式结果 MUST通过原角色/领域发布链交付，而非直接操作场景对象

### Requirement: Timeline必须保留时间边界和TreeClip生命周期

Timeline唯一时间owner MUST负责帧/秒/Tick、ClipIn、速率、源映射、Section/loop与前后采样边界，复用原稳定遍历顺序。TreeClip MUST引用主实现提供的已独立编译技能入口和执行服务；Timeline管理其阶段/调用身份/取消，MUST NOT复制技能编译器或技能状态真相。Motion/Warp/Camera算法与资源映射 MUST复用原领域owner。

#### Scenario: 一步跨越循环与多个Clip边界

- **WHEN** 正式时间推进穿过多个边界
- **THEN** Runtime MUST按原稳定顺序处理尾段、完整循环、头段及Decision/Commit生命周期，不重复或遗漏Enter/Exit
- **AND** 同一源被多次使用 MUST保持不同Clip/Warp调用身份，源区间末端保持累计终值且后续delta为零，片段占用仍按自身边界

#### Scenario: 推进落点不在被跨过的短Clip中

- **WHEN** 播放从第10帧推进到第30帧，中间存在第20帧进入、第21帧退出的Clip
- **THEN** Runtime MUST按同一次Step的稳定边界顺序处理该Clip的进入、适用阶段及退出，并形成对应候选结果
- **AND** MUST NOT仅按第30帧的活动Clip集合决定本次业务，遗漏短窗口或一次性事件

#### Scenario: 停止指定播放

- **WHEN** 自然结束、停止、强停或Action context失效
- **THEN** Stop MUST关闭该实例窗口和TreeClip并通过调用方提交/丢弃协议接受终态，旧generation不得再产生Gameplay结果
- **AND** 有限动画尾部 MUST归原Slot/ActionPlayback，不因等待淡出延长Gameplay窗口，不绕过角色事务

#### Scenario: 停止候选未被调用方接受

- **WHEN** 指定播放产生停止、关闭窗口及结束TreeClip的候选后，同一步调用方失败或丢弃
- **THEN** Timeline MUST保留此前已提交的播放状态与活动调用，丢弃本次停止候选及输出
- **AND** MUST NOT在RequestStop或CompleteStop内绕过正式接受边界提前清空committed活动Clip或安装终态

### Requirement: Timeline快照必须仅拥有分型播放私有状态

Timeline MUST提供Float32/Fixed分型Capture/Restore，保存已提交cursor、loop/Section、活动Clip、窗口、TreeClip调用关联、终态、内容版本和generation等影响未来Tick的私有状态。Pending、派生缓存、Unity对象与GUI状态 MUST NOT进入快照。角色核心 MUST组合技能/Timeline/World分区并决定原子安装，Timeline MUST NOT另建角色codec或复制技能服务私有状态。

#### Scenario: 恢复内容不兼容

- **WHEN** snapshot的内容revision、NumericTarget、schema或服务关联不匹配
- **THEN** Restore MUST在安装前明确拒绝，MUST NOT从当前资产猜测旧内容或读取旧Program兼容包
- **AND** 失败 MUST保持当前已提交状态，成功恢复也 MUST NOT重发已提交副作用或推进Pose图

### Requirement: TreeClip必须通过精确图身份调用技能服务

Timeline Runtime MUST为每个TreeClip候选携带`TreeGraphId`和`TreeGraphRevision`。`TreeGraphId` MUST是只读内容闭包声明的`tree:<GraphAuthoringId>`，`TreeGraphRevision` MUST是同一`timeline.tree` dependency的`ContentHash`。Timeline MUST NOT解析、加载、缓存、执行或改写目标图。TreeClip service MUST只解析完全匹配的正式图身份；图、依赖或revision缺失/不匹配 MUST产生精确失败。系统 MUST NOT使用fallback、自动最新版、默认图、兼容映射或静默替换。

#### Scenario: 准备缺少tree contract

- **WHEN** Timeline内容包含TreeClip，但只读闭包缺少对应`timeline.tree` dependency或`ContentHash`
- **THEN** Prepare MUST返回精确的tree contract缺失失败，MUST NOT返回Ready或空服务
- **AND** Runtime MUST NOT改用默认图、最新图或兼容身份

#### Scenario: 图版本不匹配

- **WHEN** TreeClip service收到的`TreeGraphRevision`与正式图身份不一致
- **THEN** 本次TreeClip调用 MUST失败，调用方Step MUST按Commit/Discard边界处理候选
- **AND** Runtime MUST NOT隐式替换revision、重编译成另一版本或绕过失败

#### Scenario: 精确身份通过服务执行

- **WHEN** 只读闭包中的tree dependency与正式图身份完全匹配
- **THEN** Timeline MUST把同一`TreeGraphId`和`TreeGraphRevision`传给TreeClip service
- **AND** Timeline MUST NOT在服务外解析或执行图，服务结果 MUST回到同一Step提交边界

### Requirement: ActionCue必须只发布committed领域事件

`ActionCueTrack` MUST 只把 Logic 执行域的跨点转成 committed 领域事件；`EventName` MUST 使用作者配置的 `CueType`，业务键 MUST 使用 `CueId`。事件 MUST 在 SimulationTick Advance 被接受后通过 `CharacterTimelineHost.ActionCueCommitted` 发布，payload MUST 包含稳定 `EventId`、playback handle、generation、`LogicTick`、frame/cycle、execution identity、content revision、source/track/clip authoring id、`EventName` 和 `CueId`。Timeline runtime MUST NOT 在 ActionCue 内解析领域 payload、直接驱动 Camera/VFX/Audio、写 Gameplay fact 或改由 PresentationFrame 重发。

#### Scenario: Corin攻击属性cue被提交

- **WHEN** Logic Timeline 跨过 `CueType=AttackProperty` 的 ActionCue
- **THEN** runtime MUST 只发布事件名为 `AttackProperty` 的 committed ActionCue
- **AND** `CueId` MUST 保留原始 `Corin_Attack_*_AttackProperty_*` key，MUST NOT被 Timeline 重命名、截断或重编码
- **AND** Ability/Attack 领域 MUST 按该 `CueId` 解析正式 GameplayEffect Profile / Ability 执行域内容

#### Scenario: 攻击属性payload到达领域

- **WHEN** `AttackProperty` ActionCue 被提交
- **THEN** Timeline payload MUST只包含播放、内容、身份和 `CueId` 字段
- **AND** 命中效果编号、碰撞形状、属性数值和目标语义 MUST由 GameplayEffect Profile / Ability 执行域消费，MUST NOT由 Timeline runtime 解释

#### Scenario: 领域消费方未装配

- **WHEN** 某个 `CueType` 没有领域订阅者
- **THEN** Timeline MUST 保持事件为已提交事实和 trace
- **AND** MUST NOT伪造 Camera、VFX、Audio 或 Gameplay 结果，也不得宣称事件已被业务消费
