## MODIFIED Requirements

### Requirement: Timeline必须直接执行同一正式只读内容

Timeline Runtime MUST直接消费正式轨道、Clip类型/区间/参数/顺序、Section、稳定身份及资源引用，MUST NOT把轨道/Clip编为Semantic operation或在加载时生成操作表。执行域只允许在当前 `Advance` 或 `Present` 调用中把直接遍历的结果分成 Logic Evaluation 与 Presentation Evaluation；该结果 MUST NOT成为常驻编译产物、第二份内容或跨播放复用的执行语言。技能调用操作 MAY引用Timeline identity/revision及入参，但 MUST NOT展开其内部执行。技能与真实非Skill调用 MUST使用同一Runtime，MUST NOT恢复旧TimelinePlayer、TimelineRunningTree自主播放器或Slate Runtime。

#### Scenario: 技能与独立调用复用内容

- **WHEN** 技能与非Skill调用方请求相同Timeline内容
- **THEN** 两者 MUST经同一准备/实例/推进接口，仅调用上下文不同，每次播放状态隔离
- **AND** 非Skill调用 MUST有真实调用方、typed能力和播放身份，不得包装成空技能或假角色

#### Scenario: 双域输出不形成第二执行语言

- **WHEN** 同一 Timeline 在逻辑推进或表现推进中需要输出对应域的结果
- **THEN** Runtime MUST直接遍历当前正式只读内容并形成该次调用的 evaluation 输出
- **AND** MUST NOT预编译 Track / Clip 为 Semantic operation、操作表或可复用的第二 Timeline 内容
