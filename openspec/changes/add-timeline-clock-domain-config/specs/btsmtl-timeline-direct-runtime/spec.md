## MODIFIED Requirements

### Requirement: Timeline必须直接执行同一正式只读内容

Timeline Runtime MUST直接消费正式轨道、Clip类型/区间/参数/顺序、Marker、Section、稳定身份及资源引用，MUST NOT把轨道/Clip编为Semantic operation或在加载时生成操作表。Logic 与 Presentation evaluation MUST只是当前 Advance / Present 调用直接遍历的结果分区，MUST NOT成为常驻编译产物、第二份内容或跨播放复用的执行语言。技能调用操作 MAY引用Timeline identity/revision及入参，但 MUST NOT展开其内部执行。技能与真实非Skill调用 MUST使用同一Runtime，MUST NOT恢复旧TimelinePlayer、TimelineRunningTree自主播放器或Slate Runtime。

#### Scenario: 技能与独立调用复用内容

- **WHEN** 技能与非Skill调用方请求相同Timeline内容
- **THEN** 两者 MUST经同一准备/实例/推进接口，仅调用上下文不同，每次播放状态隔离
- **AND** 非Skill调用 MUST有真实调用方、typed能力和播放身份，不得包装成空技能或假角色

#### Scenario: 双域输出不形成第二执行语言

- **WHEN** 同一 Timeline 在逻辑推进或表现推进中需要输出对应域的结果
- **THEN** Runtime MUST直接遍历当前正式只读内容并形成该次调用的 evaluation 输出
- **AND** MUST NOT预编译 Track / Clip 为 Semantic operation、操作表或可复用的第二 Timeline 内容

### Requirement: Timeline必须保留时间边界和TreeClip生命周期

既有逻辑／表现播放管理者 MUST决定动作进度与速率，Timeline MUST接收前后秒数、播放身份、域、完整经过和推进原因被动求值，不得自主读取时间或再次应用动作倍率。Timeline MUST负责 ClipIn、源倍率映射、Section/loop 边界遍历和生命周期候选，复用原稳定遍历顺序；作者秒数 MUST不被永久转换为独立 tick 版内容。每个 playback identity 的表现采样 MUST由原表现播放管理者每帧计算一次，动作动画、同动作 Marker 和 Camera MUST消费该结果，不改变逻辑进度或强制并入 locomotion／独立效果。TreeClip MUST引用正式已编译入口与服务，管理阶段／调用身份／取消，不复制技能状态；Presentation Marker MUST使用正式表现安全上下文。Motion/Warp/Camera 算法与资源映射 MUST复用原领域 owner。

#### Scenario: 一步跨越循环与多个Clip边界

- **WHEN** 正式时间推进穿过多个边界
- **THEN** Runtime MUST按原稳定顺序处理尾段、完整循环、头段及Decision/Commit生命周期，不重复或遗漏Enter/Exit
- **AND** 同一源被多次使用 MUST保持不同Clip/Warp调用身份，源区间末端保持累计终值且后续delta为零，片段占用仍按自身边界

#### Scenario: 推进落点不在被跨过的短Clip中

- **WHEN** 播放从 0.10 秒推进到 0.30 秒，中间存在 0.20 秒进入、0.21 秒退出的 Clip
- **THEN** Runtime MUST按同一次Step的稳定边界顺序处理该Clip的进入、适用阶段及退出，并形成对应候选结果
- **AND** MUST NOT仅按 0.30 秒的活动 Clip 集合决定本次业务；窗口内碰撞采样仍由原战斗领域负责，不能以边界已发出冒充已完成命中处理

#### Scenario: 停止指定播放

- **WHEN** 自然结束、停止、强停或Action context失效
- **THEN** Stop MUST关闭该实例窗口和TreeClip并通过调用方提交/丢弃协议接受终态，旧generation不得再产生Gameplay结果或新的Presentation Marker
- **AND** 已生成的动画、相机、特效或音效尾部 MUST归原领域owner，不得通过继续推进旧Timeline保持尾部

#### Scenario: 停止候选未被调用方接受

- **WHEN** 指定播放产生停止、关闭窗口及结束TreeClip的候选后，同一步调用方失败或丢弃
- **THEN** Timeline MUST保留此前已提交的播放状态与活动调用，丢弃本次停止候选及输出
- **AND** MUST NOT在RequestStop或CompleteStop内绕过正式接受边界提前清空committed活动Clip或安装终态

### Requirement: Timeline推进必须服从调用方Step提交

Advance MUST被动消费调用方播放管理者提供的时间经过，只产生求值私有状态、实际到达边界、领域贡献、窗口／TreeClip 和表现／trace 候选，MUST NOT提前发布副作用。调用方统一 Step MUST一起接受或丢弃播放进度与 Timeline 求值状态；遇到 Decision、阻塞或完成边界时 MUST接受正式处理后的实际位置，不得越过边界直接安装请求终点。Timeline Commit MUST只安装对应求值候选，角色／World／表现发布仍归原 owner。

#### Scenario: 同一步World处理失败

- **WHEN** Timeline 已求值但调用方未接受本 Step
- **THEN** Discard MUST丢弃播放进度、Timeline 及 TreeClip 服务候选，保持上次 committed 状态
- **AND** MUST NOT泄漏窗口、Motion、Cue、结果或成功 trace，也不得新增自动提交路径

#### Scenario: 接受推进结果

- **WHEN** 调用方接受指定 Step 候选
- **THEN** MUST原子接受匹配实例／generation／Step 的播放进度与求值状态，不重新执行技能逻辑
- **AND** 正式结果 MUST通过原领域发布链交付，而非直接操作场景对象

### Requirement: Timeline快照必须仅拥有分型播放私有状态

Timeline MUST提供 Float32/Fixed 分型 Capture/Restore，保存已接受求值位置、loop/Section、活动 Clip、窗口、TreeClip 调用关联、终态、内容版本和 generation 等求值私有状态；已接受位置 MUST不构成第二个自主推进源。精确动作进度、倍率、暂停和换算余数 MUST由既有逻辑播放管理者保存，原角色快照组合 MUST原子恢复两者与技能／World 状态。Pending、表现采样缓存、Unity 对象和 GUI 状态 MUST NOT进入逻辑快照；MUST NOT新增角色 codec 或复制技能服务状态。

#### Scenario: 恢复内容不兼容

- **WHEN** snapshot 的 revision、NumericTarget、schema 或服务关联不匹配
- **THEN** Restore MUST在安装前拒绝，不得猜测旧内容或读取兼容包
- **AND** 失败 MUST保持已提交状态，成功恢复 MUST不重发已提交副作用或推进 Pose 图

### Requirement: ActionCue必须只发布committed领域事件

`ActionCueTrack` MUST 只把 Logic 执行域的跨点转成 committed 领域事件；`EventName` MUST 使用作者配置的 `CueType`，业务键 MUST 使用 `CueId`。事件 MUST 在 SimulationTick Advance 被接受后通过 `CharacterTimelineHost.ActionCueCommitted` 发布，payload MUST 包含稳定 `EventId`、playback handle、generation、`LogicTick`、秒制内容位置、cycle、execution identity、content revision、source/track/clip authoring id、`EventName` 和 `CueId`。MUST删除原调度 frame 字段，保留的素材帧身份 MUST不承担时间推进。Timeline runtime MUST NOT 在 ActionCue 内解析领域 payload、直接驱动 Camera/VFX/Audio、写 Gameplay fact 或改由 PresentationFrame 重发。

#### Scenario: Corin攻击属性cue被提交

- **WHEN** Logic Timeline 跨过 `CueType=AttackProperty` 的 ActionCue
- **THEN** runtime MUST 发布事件名为 `AttackProperty` 的 committed ActionCue，携带秒制位置及实际 LogicTick
- **AND** CueId MUST保留原始 key，Ability/Attack 领域 MUST按该 key 解析正式 GameplayEffect Profile / Ability 执行域内容

#### Scenario: 攻击属性payload到达领域

- **WHEN** `AttackProperty` ActionCue 被提交
- **THEN** Timeline payload MUST只包含播放、内容、身份和 CueId 字段
- **AND** 命中效果编号、碰撞形状、属性数值和目标语义 MUST由 GameplayEffect Profile / Ability 执行域消费

#### Scenario: 领域消费方未装配

- **WHEN** 某个 CueType 没有领域订阅者
- **THEN** Timeline MUST保持事件为已提交事实和 trace
- **AND** MUST NOT伪造 Camera、VFX、Audio 或 Gameplay 结果，也不得宣称事件已被业务消费

## ADDED Requirements

### Requirement: 状态本地ActionCue必须绑定状态分段和分支

`Attack_Normal_03_Explode` 与 `Attack_Normal_05_End / End_2` 的 ActionCue MUST 绑定各自状态分段或独立 Timeline；MUST NOT 把状态本地 cue 压平成无状态全局位置。分支边界 MUST 在进入 End / End_2 时明确选择其中一个分支，两侧 cue MUST NOT同时发布。事件 MUST保留原始 CueId、状态 id 与原始素材 LocalFrame 来源身份；Attack5 分支还 MUST携带分支身份。稳定 EventId MUST包含状态 id、来源 LocalFrame 和分支身份，避免同一 CueId 互相覆盖。唯一运行位置 MUST为正式映射后的秒数，LocalFrame MUST NOT成为第二份可写调度时间。攻击碰撞与属性 payload MUST留在 GameplayEffect / Ability 执行域。

#### Scenario: Attack3 Explode cue

- **WHEN** `Attack_Normal_03_Explode` 的 frame=1 cue 被收口
- **THEN** 事件 MUST携带 `StateId=Attack_Normal_03_Explode`、`LocalFrame=1`
- **AND** 唯一秒制位置 MUST由正式状态源映射得到，MUST NOT把一基 LocalFrame=1 直接解释为 1/60 秒偏移

#### Scenario: Attack5 End与End2分支

- **WHEN** `Attack_Normal_05` 到达由旧作者 frame=47 换算得到的秒制分支点
- **THEN** playback MUST只进入 End 或 End_2 其中一个正式 Timeline / 状态分段
- **AND** End_2 的 15 个状态本地 cue MUST只在 End_2 分支发布
- **AND** End_2 事件 MUST携带 `StateId=Attack_Normal_05_End_2`、`BranchId=End_2` 和状态本地帧

### Requirement: 表现修正不得把采样重算当成新的事件经过

同一 playback generation 的 Presentation 重采样、Seek、分支替换或位置回退 MUST沿正式表现 EventId 调和。修正不得自动增加 Marker traversal，也不得从逻辑 Restore 的中间状态发布表现事件。正常循环重新经过才 MAY产生新的 traversal。

#### Scenario: 回退后重新采样

- **WHEN** 表现位置因最终分支修正回退并再次落到已交付 Marker 附近
- **THEN** 系统 MUST保持原经过身份并避免重复事件
- **AND** MUST从当前可见结果接管，不重置整个角色表现事务
