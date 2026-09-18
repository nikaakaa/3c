## MODIFIED Requirements

### Requirement: Timeline必须直接执行同一正式只读内容

Timeline Runtime MUST直接消费正式轨道、Marker、Clip类型/区间/参数/顺序、Section、稳定身份及资源引用，MUST NOT把轨道/Clip编为Semantic operation或在加载时生成操作表。执行域只允许在当前 `Advance` 或 `Present` 调用中把直接遍历的结果分成 Logic Evaluation 与 Presentation Evaluation；该结果 MUST NOT成为常驻编译产物、第二份内容或跨播放复用的执行语言。技能调用操作 MAY引用Timeline identity/revision及入参，但 MUST NOT展开其内部执行。技能与真实非Skill调用 MUST使用同一Runtime，MUST NOT恢复旧TimelinePlayer、TimelineRunningTree自主播放器或Slate Runtime。

#### Scenario: 技能与独立调用复用内容

- **WHEN** 技能与非Skill调用方请求相同Timeline内容
- **THEN** 两者 MUST经同一准备/实例/推进接口，仅调用上下文不同，每次播放状态隔离
- **AND** 非Skill调用 MUST有真实调用方、typed能力和播放身份，不得包装成空技能或假角色

#### Scenario: 双域输出不形成第二执行语言

- **WHEN** 同一 Timeline 在逻辑推进或表现推进中需要输出对应域的结果
- **THEN** Runtime MUST直接遍历当前正式只读内容并形成该次调用的 evaluation 输出
- **AND** MUST NOT预编译 Track / Clip 为 Semantic operation、操作表或可复用的第二 Timeline 内容

### Requirement: 状态本地ActionCue必须绑定状态分段和分支

`Attack_Normal_03_Explode` 与 `Attack_Normal_05_End / End_2` 的 ActionCue MUST 绑定各自状态分段或独立 Timeline；MUST NOT 把状态本地 cue 压平成无状态全局帧。分支边界 MUST 在进入 End / End_2 时明确选择其中一个分支，两侧 cue MUST NOT同时发布。状态本地 cue 事件 MUST 保留原始 `CueId`，MUST 携带状态 id 与状态本地帧；Attack5 分支还 MUST 携带分支身份用于稳定调和。攻击碰撞与属性 payload MUST 留在 GameplayEffect / Ability 执行域。

#### Scenario: Attack3 Explode cue

- **WHEN** `Attack_Normal_03_Explode` 的 frame=1 cue 被收口
- **THEN** 事件 MUST携带 `StateId=Attack_Normal_03_Explode`、`LocalFrame=1`
- **AND** MUST NOT通过主段无状态全局帧替代

#### Scenario: Attack5 End与End2分支

- **WHEN** `Attack_Normal_05` 到达 frame=47 分支点
- **THEN** playback MUST只进入 End 或 End_2 其中一个正式 Timeline / 状态分段
- **AND** End_2 的 15 个状态本地 cue MUST只在 End_2 分支发布
