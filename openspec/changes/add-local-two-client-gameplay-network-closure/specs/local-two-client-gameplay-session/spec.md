## ADDED Requirements

### Requirement: 本地 ServerAuthoritative Session 必须形成真实双客户端 Room

系统 MUST 提供一个运行在 Fantasy Gate Scene 的本地双人 Room。Room MUST 最多接受两条已 Join Session，每条 Session MUST 只拥有一个 Actor，并由服务端分配唯一 PlayerId、ActorId、TeamId 和出生 pose。Room MUST NOT 依赖账号、数据库、匹配、Map server、SubScene 或 Roaming。

#### Scenario: 第一名客户端加入

- **WHEN** 第一条 Fantasy Session 发送 Join request
- **THEN** 服务端 MUST 创建唯一 Actor并返回 Owner descriptor、出生 pose 和 server clock
- **AND** 客户端 MUST 将 inactive Corin 配置为 LocalDevice + LocalSolver 后再激活

#### Scenario: 第二名客户端加入

- **WHEN** 第二条 Session 加入同一 Room
- **THEN** 服务端 MUST 为其创建不同 Actor
- **AND** 两条 Session MUST 各自收到另一个 Actor 的 roster 数据
- **AND** 两个客户端 MUST 各自创建 ExternalFacts + ExternalPose Corin

#### Scenario: 第三名客户端加入

- **WHEN** Room 已有两条 Session
- **THEN** Join response MUST 返回明确 RoomFull 业务错误
- **AND** MUST 不替换或销毁现有 Actor

### Requirement: Session Roster 必须通过模型事件管理 Character 生命周期

Fantasy endpoint MUST 将 JoinCompleted、ActorJoined 和 ActorLeft 转换为 ServerAuthoritative session event。Model-owned roster host MUST 在 inactive root 上先配置 Character host、binding、SubjectActorId、spawn pose 和相机依赖，再激活 pipeline。Common SessionHost 和 Character per-actor queue MUST 不解释 roster event。

#### Scenario: Owner JoinCompleted

- **WHEN** roster 收到 Owner descriptor
- **THEN** MUST 在激活前写入服务端 SubjectActorId 和 spawn pose
- **AND** MUST 不使用临时 LocalActor identity 建立并行 binding

#### Scenario: 远端 ActorLeft

- **WHEN** roster 收到远端 ActorLeft
- **THEN** MUST 释放该 actor binding、snapshot/action buffer、pipeline 和 clone
- **AND** 本地 Owner 与 Fantasy Session MUST 继续运行

### Requirement: 服务端必须维护唯一 canonical Actor pose

Room MUST 通过单一 Scene-owned 30Hz tick 消费每个 Actor 的有界 canonical input/action command queue。客户端 MAY 按现有 logic tick 产生 command，服务端每次 tick MUST 按 sequence 消费积累的全部 command；MUST NOT 只保留最后一条。服务端 MUST 从 Session 解析 Actor ownership，拒绝非法数值、重复/倒序 sequence、配置 identity 不一致和非法 action phase，并调用本纵切唯一 authoritative simulation backend，从当前 canonical body state 独立生成 motion intent 和新的 canonical pose。服务端 MUST 以 15Hz 向非 Owner 广播 snapshot。服务端 MUST NOT 累加客户端 applied displacement、接受 predicted pose 作为 canonical pose，或在 backend 缺失时回退 envelope validation。

#### Scenario: Client A 移动

- **WHEN** Client A 连续提交合法 canonical input/action MotionCommand
- **THEN** authoritative backend MUST 独立推进 Actor A canonical pose 与 accepted sequence
- **AND** Client B MUST 收到 Actor A MotionSnapshot
- **AND** Client A 在存在误差时 MUST 收到 MotionCorrection

#### Scenario: 收到重复 command

- **WHEN** 服务端收到已消费 input sequence 的 MotionCommand
- **THEN** MUST 拒绝该 command并增加 duplicate 计数
- **AND** authoritative backend MUST 不再次推进相同输入

#### Scenario: backend 不可用

- **WHEN** Room 没有配置已批准的 authoritative motion backend 或 backend 执行失败
- **THEN** 对应 actor 权威推进 MUST 明确失败并记录 health fault
- **AND** Room MUST 不使用客户端 resolved motion 或 predicted pose 继续更新 canonical state

### Requirement: 双客户端必须闭合动作事务与远端动作

Owner ActionActivation MUST 进入服务端事务检查。接受后服务端 MUST 向 Owner 返回 ActionDecision，并向其它 Session 广播 ActionReplication；拒绝时 MUST 只向 Owner 返回 Reject。远端 Character MUST 通过 ExternalActionActivation 与 ActionLifecycleTransition 驱动同一 Corin Action Graph、Timeline 和动画链。

#### Scenario: Client A 攻击

- **WHEN** Client A 本地预测启动 Attack 且服务端接受 activation
- **THEN** Client A ActionRuntime MUST 收到 Confirm lifecycle
- **AND** Client B 的 Actor A Character MUST 启动同一 Attack authoring
- **AND** packet MUST 不携带 AnimationClip、TimelineId 或 Animancer transition

#### Scenario: Client A 闪避

- **WHEN** Client A 启动 Dodge 且服务端接受 activation
- **THEN** Client B MUST 看见 Actor A 的 Dodge Timeline 和动画
- **AND** 远端 logic pose MUST 继续由 external pose 驱动，不由 Dodge MotionCurve 移动

#### Scenario: 攻击继续连段

- **WHEN** Owner 在现有 combo window 内产生下一次合法 Attack activation
- **THEN** 服务端 MUST 按新的 action instance 确认并广播
- **AND** 远端 MUST 通过现有 Attack1/Attack2 StateMachine authoring 进入下一段

### Requirement: 远端 pose 必须按 server tick 平滑表现

每个远端 binding MUST 拥有固定 32 条容量的 snapshot buffer，并使用 Join 返回的 server tick rate、4 tick interpolation delay 和 30 tick stale timeout。Model sampler MUST 输出 Character external logic pose 与 model-neutral external visual pose；Character MUST 不读取 server packet 或 clock。

#### Scenario: 两个 snapshot 间渲染

- **WHEN** buffer 中存在目标 server time 前后样本
- **THEN** visual root MUST 使用插值 pose
- **AND** logic root MUST 不被 PresentationFrame 反写

### Requirement: 长时间运行状态必须保持有界且可观察

Fantasy endpoint、model session、session event、per-actor queue、snapshot buffer、history/debug 与服务端 command/action queue MUST 使用固定容量。系统 MUST 记录连接时长、最后 server tick、最近收发、队列水位、duplicate、stale、overflow、correction 和 disconnect reason。系统 MUST 使用 heartbeat 保持空闲连接，MUST NOT 通过自动重连、自动重启或切换 Loopback 隐藏故障。

#### Scenario: 两个客户端持续空闲

- **WHEN** 两个客户端保持连接且没有玩家输入
- **THEN** heartbeat 和定期 snapshot MUST 保持 Session 活跃
- **AND** 所有历史与队列 MUST 保持在固定容量内

#### Scenario: 可靠队列达到容量

- **WHEN** Action、Ack、Result 或 roster event 队列达到容量
- **THEN** endpoint/session MUST 产生明确 Faulted/overflow 记录
- **AND** MUST 不静默丢弃后继续宣称健康

### Requirement: 本地双客户端启动入口必须使用正式运行模式

项目 MUST 提供本地 server 启动入口和双客户端启动入口。双客户端入口 MUST 接受显式 client executable，启动两个正常可见窗口并使用不同日志文件；MUST NOT 使用 Unity batchmode，也 MUST NOT 自动重启失败进程。

#### Scenario: 启动本地纵切

- **WHEN** 用户提供已构建 client executable 并运行双客户端入口
- **THEN** 系统 MUST 启动一个本地 Fantasy server 和两个 client process
- **AND** 两个客户端 MUST 连接同一 KCP endpoint并由服务端分配不同身份
