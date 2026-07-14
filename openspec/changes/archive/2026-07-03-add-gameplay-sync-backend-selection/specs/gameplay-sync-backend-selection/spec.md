# gameplay-sync-backend-selection Specification

## ADDED Requirements

### Requirement: Gameplay Sync 后端选择必须是正式装配语义

系统 MUST 提供 `CharacterGameplaySyncDriver` 或等价正式 Unity 装配组件，用于为角色选择 gameplay sync backend。该组件 MUST 持有 `GameplaySyncRuntime`、actor identity、backend mode 和 backend-specific settings。它 MUST NOT 使用 loopback 专用命名作为角色同步主入口。

#### Scenario: 场景作者选择本地模拟
- **WHEN** 作者在角色对象上启用 gameplay sync 并选择 `LocalLoopback`
- **THEN** 系统 MUST 创建 `GameplaySyncRuntime`
- **AND** 系统 MUST 创建 `LocalGameplaySyncLoopbackPeer` 并注册到 runtime
- **AND** 组件名称和 Inspector 语义 MUST 表达这是 gameplay sync backend selection，而不是 loopback 专用管线

#### Scenario: 场景作者关闭同步后端
- **WHEN** 作者选择 `None`
- **THEN** 系统 MUST 创建或保留 `GameplaySyncRuntime`
- **AND** 系统 MUST 将 peer 设置为空
- **AND** CharacterPipeline MUST 继续正常运行本地输入、Graph、Motion 和 Presentation

### Requirement: 第一阶段后端必须只包含 None 和 LocalLoopback

第一阶段 backend mode MUST 只包含 `None` 和 `LocalLoopback`。系统 MUST NOT 暴露 `Fantasy`、`Server`、`Online` 或其它不可用真实网络选项。未来真实 Fantasy 后端 MUST 通过独立 change 增加。

#### Scenario: Inspector 查看后端选项
- **WHEN** 作者展开 gameplay sync driver 的 backend mode
- **THEN** 可选项 MUST 只包含 `None` 和 `LocalLoopback`
- **AND** 系统 MUST NOT 显示尚未实现的 Fantasy mode

#### Scenario: 后续接入 Fantasy
- **WHEN** 未来实现真实 Fantasy peer
- **THEN** 该实现 MUST 复用 `IGameplaySyncPeer` 和 `GameplaySyncRuntime.SetPeer`
- **AND** 该实现 MUST 不要求 CharacterPipeline、Graph 或 Timeline 改走第二条网络路径

### Requirement: Backend driver 必须服从 GameplayTickSystem

Gameplay sync driver MUST 作为 `IGameplayTickHook` 或等价 tick hook 接入 `GameplayTickSystem`。它 MUST 在角色 logic tick 前 pump incoming 并注入 `CharacterNetworkReceiveStage`，在角色 logic tick 后收集 `CharacterNetworkSendStage` 输出并 flush 给当前 backend。

#### Scenario: Tick 前注入 incoming
- **WHEN** `GameplayTickSystem` 进入某个 local logic tick
- **THEN** gameplay sync driver MUST 先调用 `GameplaySyncRuntime.Pump(localLogicTick)`
- **AND** 必须通过 `CharacterGameplaySyncAdapter.DrainIncoming` 注入正式 receive stage
- **AND** driver MUST NOT 直接调用 ActionRuntime、Graph 或 MotionStage

#### Scenario: Tick 后发送 outgoing
- **WHEN** CharacterPipeline 完成本 tick 并产生 SyncFacts
- **THEN** gameplay sync driver MUST 通过 `CharacterGameplaySyncAdapter.CollectOutgoing` 写入 runtime outgoing queue
- **AND** 必须通过 `GameplaySyncRuntime.FlushOutgoingToPeer` 交给当前 peer
- **AND** driver MUST NOT 直接构造 Fantasy 消息或访问 transport

### Requirement: None 后端必须是正式关闭同步模式

`None` backend MUST 表达“当前角色不连接任何 gameplay sync peer”。它 MUST NOT 作为 fallback、异常恢复或隐式缺省补救。选择 `None` 时，系统 MAY 继续产生 `SyncFacts`，但这些 facts MUST 不被发送到外部 peer。

#### Scenario: 单机角色运行
- **WHEN** 角色 backend mode 是 `None`
- **THEN** CharacterPipeline MUST 正常执行本地输入、Graph、Timeline、Motion 和 Presentation
- **AND** gameplay sync driver MUST 不创建 loopback peer
- **AND** outgoing queue MUST 不发送到外部 backend

#### Scenario: 调试面板查看 None
- **WHEN** backend mode 是 `None`
- **THEN** Runtime Debug MUST 能显示 backend 关闭状态
- **AND** Debug MUST NOT 假装存在 pending peer packet

### Requirement: LocalLoopback 后端必须只创建本地调试 peer

`LocalLoopback` backend MUST 使用 `LocalGameplaySyncLoopbackPeer` 或等价 peer 实现本地延迟、确认、拒绝、校正、快照和 debug。Loopback settings MUST 归属 gameplay sync driver 的本地调试配置，MUST NOT 写入 Graph、Timeline、ActionProfile 或正式服务端策略数据。

#### Scenario: 本地预测动作确认
- **WHEN** backend mode 是 `LocalLoopback` 且 loopback 配置为 confirm
- **THEN** outgoing action activation MUST 通过 peer 延迟生成 incoming action decision
- **AND** incoming decision MUST 通过 GameplaySyncRuntime 和 CharacterGameplaySyncAdapter 回到 CharacterNetworkReceiveStage

#### Scenario: 切换 loopback 配置
- **WHEN** 作者修改 loopback 延迟、拒绝、校正或快照配置
- **THEN** 这些设置 MUST 只影响本地 loopback peer
- **AND** 系统 MUST NOT 修改 ActionProfile、Graph 或 Timeline 数据

### Requirement: 旧 LoopbackDriver 入口必须清理

实现阶段 MUST 删除或重命名旧 `CharacterGameplaySyncLoopbackDriver` 主入口。系统 MUST NOT 长期同时保留 loopback 专用 driver 和 backend-agnostic driver 两个角色同步入口。Loopback 命名 MAY 保留在 peer、settings 和本地调试 UI 中。

#### Scenario: 搜索旧 driver
- **WHEN** 实现完成后搜索 `CharacterGameplaySyncLoopbackDriver`
- **THEN** 系统 MUST 不再存在该类型作为正式 Unity 组件
- **AND** `CharacterPipelineHostEditor` MUST 不再依赖该类型查找网络 debug

#### Scenario: 保留 loopback peer
- **WHEN** 实现完成后搜索 `LocalGameplaySyncLoopbackPeer`
- **THEN** 该 peer MAY 继续存在
- **AND** 它 MUST 只由正式 backend selection 在 `LocalLoopback` mode 下创建
