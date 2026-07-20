# unity-authoritative-two-client-demo Specification

## Purpose
定义 Fantasy Gate、Unity Authority Worker、Client A 与 Client B 四进程 Demo 的显式装配、启动、双角色同步和诊断边界。
## Requirements
### Requirement: Demo必须提供四进程显式启动组合

系统 MUST提供一个Fantasy Gate/Room launch配置、一个Unity Authority Worker launch配置、Client A launch配置和Client B launch配置。四个process role、RoomId、PlayerId、ActorId、Endpoint和Composition Definition MUST显式配置，MUST不通过场景搜索、启动顺序、默认值或连接失败fallback决定角色。

#### Scenario: 查看Demo配置

- **WHEN** 作者检查ServerAuthoritative Demo资产
- **THEN** MUST能分别定位Fantasy Server、Authority Worker、Client A与Client B配置
- **AND** 每个Unity角色 MUST引用完整Session composition

### Requirement: Demo必须使用一个Authority Session连接两个Prediction Clients

Authority Worker MUST运行一个包含Actor A/B的canonical Authority Session。Client A MUST只预测Actor A并显示Actor B remote；Client B MUST只预测Actor B并显示Actor A remote。两端 MUST连接同一Fantasy Room并共享相同Model/protocol/Program identity。

#### Scenario: 完整Roster进入Active

- **WHEN** worker和两个clients完成register/join
- **THEN** Room MUST锁定两个唯一Actor ownership
- **AND** 三个Unity Session MUST使用匹配的Authority/Prediction Pipeline pair

### Requirement: Demo三端必须加载同一Corin Program

Authority Worker、Client A和Client B MUST从ProgramAsset exact-byte wrapper加载相同Corin Float32 canonical bytes，并在handshake校验ProgramHash、LayoutHash、operation-set和TickRate。任一identity不匹配 MUST阻止Session Active，MUST不运行时编译或选择旧Program。

#### Scenario: Client B Program过期

- **WHEN** Client B ProgramHash与worker不同
- **THEN** join MUST失败并返回Program identity诊断

### Requirement: Demo必须覆盖Owner Prediction与Server Correction

每个Client owner MUST即时执行Prediction Pipeline，并在收到不一致Authority Baseline时通过正式restore/replay或HardRecovery纠偏。Owner Presentation MUST以独立sample时钟消费零步、单步和双步结果；restore/replay替换旧body历史时 MUST从上一帧可见姿态收敛到新canonical body。Demo MUST能产生和记录ack推进、state/body误差、restore tick、replayed ticks、EventId duplicate suppression和visual recovery。Correction MUST不直接写Transform或切换Local Pipeline。

#### Scenario: Client A预测位置偏离

- **WHEN** Actor A Authority Baseline与本地prediction不一致且history覆盖
- **THEN** Client A MUST在一个outer transaction中restore并replay未确认inputs
- **AND** 最终Body sample MUST通过Presentation recovery显示

### Requirement: Demo必须覆盖Corin Gameplay纵切

Authority Worker MUST独立推进两个Corin的移动、转身、闪避、Run、Attack1/Attack2、连段、打断、Timeline TreeClip Window、motion curve、GameplayEffect、Attribute和Cue。Client owner MUST显示预测结果，remote client MUST从authority replication显示相同业务producer/fact identity。Demo MUST不恢复Graph runtime、Character network stage或动画状态同步。

#### Scenario: Client A输入Attack连段

- **WHEN** Actor A canonical input包含合法Attack1到Attack2请求
- **THEN** Authority Program MUST独立推进Action/Timeline/Window事实
- **AND** Client B MUST通过remote presentation output显示对应producer和可靠EventId

### Requirement: Demo必须保持Local与Hybrid为两个完整显式组合

Standard Local composition MUST继续使用Local Source和Standard Local Pipeline；ServerAuthoritative Demo MUST使用Fantasy Source以及Prediction/Authority Pipeline。两者 MAY共享Corin Program Runtime、Float32 Backend、标准Step Pass和Unity Solver，但 MUST不共享mutable Session、History、Endpoint或Pipeline state，也 MUST不互为fallback。

#### Scenario: 选择Local Demo

- **WHEN** 场景显式引用Corin Local composition
- **THEN** MUST不创建Fantasy Endpoint、Prediction history或Authority Worker

### Requirement: Demo必须提供有界只读模型诊断

Demo diagnostics MUST显示process role、Room/Session/Player/Actor、Program/Layout、Prediction/Authority Pipeline、Control/Data Endpoint、各通道packet/s与bytes/s、payload bytes、control heartbeat outstanding、应用层可靠/full checkpoint队列压力、UDP丢包/乱序、RTT、jitter、command lead、snapshot age、baseline命中、interpolation occupancy、prediction error、correction decision、restore tick、replayed ticks、hard recovery、ack cursor和EventId disposition。Diagnostics MUST不修改Source、Pipeline、History、Solver或Presentation。

#### Scenario: Owner发生Replay

- **WHEN** Prediction Schedule执行多个Replay steps
- **THEN** diagnostics MUST关联authority baseline、input sequence、step range和最终disposition

### Requirement: Demo边界必须明确不是完整对战服务

Demo MUST标记为双角色ServerAuthoritative Gameplay纵切，MUST不宣称命中伤害、combat rewind、lag compensation、2v2vE、PvE、Objective、匹配、数据库、断线续局、动态join或商业反作弊能力。

#### Scenario: 显示Demo能力说明

- **WHEN** Debug/Inspector显示当前网络Demo范围
- **THEN** MUST区分已闭环prediction/correction/remote presentation与未实现combat authority能力
