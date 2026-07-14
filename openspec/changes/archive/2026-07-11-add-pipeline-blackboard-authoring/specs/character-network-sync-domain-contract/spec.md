# character-network-sync-domain-contract Specification

## ADDED Requirements

### Requirement: Blackboard 变量不得默认网络同步

系统 MUST NOT 默认同步 Pipeline Blackboard 的所有变量。Blackboard variable 只有在声明了明确 sync policy，并由正式 resolver 映射成 SyncFacts 后，才 MAY 被 NetworkSendStage 消费。系统 MUST NOT 引入通用 blackboard key/value 网络包作为角色 pipeline 的默认同步路径。

#### Scenario: 本地调试变量

- **WHEN** 某个 blackboard variable 只用于本地 debug 或状态内部判断
- **THEN** 该变量 MUST 保持 local-only
- **AND** NetworkSendStage MUST 不读取该变量
- **AND** outgoing packet MUST 不包含该变量 key/value

#### Scenario: 变量声明为 SyncFact

- **WHEN** 某个变量声明的 sync policy 要求输出为同步事实
- **THEN** resolver MUST 将该变量或事件转换为对应 SyncDomain output
- **AND** NetworkSendStage MUST 只读取转换后的 SyncFacts

### Requirement: 可调参数必须通过配置身份对齐

可调参数类 blackboard variable MUST 通过 pipeline 配置版本、角色 loadout identity、ActionProfile identity 或等价配置 hash 对齐。系统 MUST NOT 将 WalkThreshold、RunThreshold、TurnAngle 等可调参数作为每帧同步事实发送，除非后续 spec 明确要求热更新配置同步。

#### Scenario: 本地预测移动阈值

- **WHEN** 本地和服务端需要使用同一套 locomotion 阈值
- **THEN** 它们 MUST 通过角色配置身份或配置 hash 确认一致
- **AND** 输入帧同步 MUST 不携带每个阈值的逐帧值

#### Scenario: 配置版本不一致

- **WHEN** 接收端发现角色 pipeline 配置版本不一致
- **THEN** 系统 MUST 将其作为配置不一致问题报告
- **AND** 系统 MUST NOT 用网络包中的临时阈值覆盖本地正式配置

### Requirement: 输入派生变量不得作为独立同步事实

由输入帧、tick context、角色配置和当前状态确定性计算出的 Pipeline Blackboard variable SHOULD NOT 作为独立 SyncFacts 输出。系统 MUST 优先同步输入事实和必要的权威 correction，而不是重复同步每个派生值。

#### Scenario: MoveAxisMagnitude

- **WHEN** 规则图从 MoveAxis 计算移动输入幅度
- **THEN** 该派生值 MAY 写入 blackboard 供本地图读取
- **AND** 它 MUST NOT 默认写入 MotionSyncDomain output
- **AND** 远端或服务端 SHOULD 通过输入事实和配置重新计算该值

#### Scenario: 服务端运动校正

- **WHEN** 服务端需要纠正移动结果
- **THEN** 系统 MUST 通过 MotionSyncDomain 的 snapshot 或 correction 表达结果差异
- **AND** 系统 MUST NOT 通过同步输入派生 blackboard 值替代 correction

### Requirement: Action 和 Presentation 输出必须继续归属同步域

动作窗口、动作生命周期、玩法结果、状态效果和表现 cue 的网络可见输出 MUST 继续归属 Action、GameplayResult、StateEffect 或 Presentation SyncDomain。Blackboard MAY 缓存最近输出，但缓存身份 MUST NOT 成为网络同步单位。

#### Scenario: 攻击窗口输出

- **WHEN** `Attack1Hit` window 产生
- **THEN** 可同步事实 MUST 进入 ActionSyncDomain
- **AND** blackboard 中的最近 window 缓存 MUST NOT 替代 `ActionInstanceId`、window id 和 tick 等同步身份

#### Scenario: 本地表现 cue

- **WHEN** Timeline 触发 local-only camera cue
- **THEN** 该 cue MAY 写入 blackboard 或 presentation output
- **AND** 只有 policy 标记为 replicated 的 cue 才 MAY 进入 PresentationSyncDomain outgoing
