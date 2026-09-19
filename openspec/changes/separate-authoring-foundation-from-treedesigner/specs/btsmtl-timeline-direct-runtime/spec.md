## ADDED Requirements

### Requirement: Timeline播放调用必须使用独立的调用身份

Timeline 服务 MUST 沿已有阶段接收必要身份：Prepare 读取正式内容 identity/revision 与依赖；Playback 接收真实调用方和所需 owner context，并由既有播放 owner 建立播放 identity/generation；TreeClip 执行时携带对应 Clip/cycle 与精确图 identity/revision。接口 MUST 不把整张旧 BaseGraph 当作通用上下文，也 MUST 不通过图对象推断 Action、Character 或 Timeline owner。Skill、非 Skill 和独立 Timeline 调用 MUST 继续使用各自真实的 typed 调用来源，不新增全字段通用上下文。

#### Scenario: 技能调用Timeline

- **WHEN** 技能请求一个带 TreeClip 的 Timeline
- **THEN** 播放请求 MUST 使用正式 Ability 调用来源及其所需 Action 上下文；进入 TreeClip 时 MUST 使用该 Clip 与精确图 revision identity
- **AND** Timeline MUST 不接收或解析旧 BaseGraph

#### Scenario: 独立调用Timeline

- **WHEN** 真实的非 Skill owner 请求 Timeline
- **THEN** 调用 MUST 使用该 owner 的 typed invocation source 和独立 playback identity
- **AND** MUST 不包装成空技能、假角色或默认图上下文

#### Scenario: 缺少调用身份

- **WHEN** 某阶段缺少其正式合同要求的调用 owner、播放 generation 或 TreeClip 图版本
- **THEN** 对应阶段 MUST 返回明确失败；Prepare MUST 不要求尚未创建的播放或 Clip 身份
- **AND** MUST 不从当前资产、最后调用或默认值补齐
