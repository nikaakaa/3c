## MODIFIED Requirements

### Requirement: ServerAuthoritative Session 必须拥有精确 Actor 路由

Fantasy Room MUST使用RoomId、SessionId、PlayerId、SubjectActorId与owner connection精确路由authority host register、ticket、roster、full checkpoint与可靠facts。Authority Host和Model Source MUST使用ticket锁定的Room/Session/Player/SubjectActorId/remote endpoint精确路由command与snapshot。External Unity Worker register MUST绑定精确worker Session；InProcess DotRecast register MUST绑定精确Authority Scene Address。TargetActorId、TeamId与其它业务metadata MUST不得替代SubjectActorId进行queue drain。Unknown、duplicate、stale、role-mismatched或owner-mismatched route MUST明确拒绝，MUST不广播到猜测Actor。

真实客户端连接与完整 Actor roster MUST分离。玩家输入继续使用上述 owner ticket 路由；权威本地 Bot MUST绑定唯一 Authority 输入生产权，不创建客户端 ticket、连接或 PlayerId。Replica/可靠结果 MUST按 Actor 身份向真实收件人连接分发，不能将 TargetActorId 当作发送者权限，也不能要求每个展示 Actor 都有输入连接。

#### Scenario: Client A 提交 Actor B 输入

- **WHEN** Client A data endpoint发送SubjectActorId为Actor B的command datagram
- **THEN** 当前Authority Host MUST按ticket owner route拒绝该消息
- **AND** MUST不写入Actor B command queue

#### Scenario: 客户端提交权威Bot输入

- **WHEN** 客户端 command 指向由 Authority 本地 AI 控制的 Actor
- **THEN** Authority MUST拒绝写入该 Actor 输入
- **AND** MUST不把它当作有效玩家 owner route

#### Scenario: 同一客户端显示多个Bot

- **WHEN** Authority 发布完整 roster 的角色和动作结果
- **THEN** 客户端 MUST通过其既有 Source 路由接收多个非 owner Actor
- **AND** MUST不为每个 Bot 创建连接或额外 Gameplay Session

## ADDED Requirements

### Requirement: 插件AI必须只由声明支持的Unity权威宿主生产

Unity Authority 产品 MUST在唯一 Worker/Session Source 内生产本地 Bot 输入，并与玩家输入共同进入原有权威角色模拟。客户端 MUST只接收对应权威结果而不运行这些 Bot 的决策。纯 .NET DotRecast Authority 未声明插件运行能力时 MUST在创建会话前拒绝该 binding；MUST不增加隐藏 Unity AI 服务、客户端代算或 portable AI 兼容实现。

#### Scenario: Unity Authority运行Bot

- **WHEN** Worker 启动合法的玩家与 Bot roster
- **THEN** Bot MUST读取权威已提交观察并只输出角色输入
- **AND** Actor/Action/World 的权威 MUST仍属于原 Session 运行链

#### Scenario: DotRecast产品含插件行为

- **WHEN** 不支持该能力的 Authority 产品装配插件 AI Actor
- **THEN** 产品或会话准备 MUST明确拒绝
- **AND** 已有无插件 Authority 功能 MUST不被替换为另一宿主
