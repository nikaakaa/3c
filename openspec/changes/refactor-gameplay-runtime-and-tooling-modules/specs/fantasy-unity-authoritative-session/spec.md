## ADDED Requirements

### Requirement: Unity Fantasy Endpoint必须由唯一Connection Coordinator拥有生命周期

Unity侧Fantasy endpoint MUST保持一个正式endpoint interface和唯一control/datagram网络路径，但其内部control session、datagram channel、checkpoint reconstruction、prediction evidence/metrics MUST由职责独立的内部模块实现。唯一Connection Coordinator MUST拥有endpoint state transition、共享资源、failure和dispose顺序；内部模块 MUST只接收窄输入并返回typed result/event，MUST不独立启动Simulation、切换endpoint state、释放共享session/socket或建立第二transport。

#### Scenario: Data-plane handshake完成

- **WHEN** Control Session取得ticket且Datagram Channel完成handshake
- **THEN** 两个模块 MUST向Connection Coordinator提交typed result
- **AND** 只有Coordinator MAY将endpoint推进到可接收Gameplay datagram的状态

#### Scenario: Delta checkpoint缺少baseline

- **WHEN** Checkpoint Reconstruction收到无法应用的delta checkpoint
- **THEN** 模块 MUST返回包含sequence/baseline identity的明确失败结果
- **AND** Coordinator MUST按正式Source/Model失败策略处理
- **AND** 模块 MUST不自行切换KCP gameplay stream或创建近似baseline

#### Scenario: Endpoint释放

- **WHEN** Player离开、Worker失败或Host dispose当前endpoint
- **THEN** Coordinator MUST按固定顺序停止callback ingress、释放datagram/control资源并完成Source failure
- **AND** 任一内部模块 MUST不保留独立heartbeat、socket或后台发送循环

### Requirement: Endpoint内部拆分不得改变Source Callback边界

Fantasy handler与network callback MUST继续只校验消息外壳并写入正式Source receive queue。Checkpoint merge、ack/horizon推进、Program、Pipeline、WorldSolver和Presentation MUST继续由GameplayTickSystem驱动的Session runtime消费。内部模块拆分 MUST不新增callback simulation、Transform写入或Animancer调用。

#### Scenario: Client收到routine snapshot datagram

- **WHEN** Datagram Channel收到合法routine snapshot
- **THEN** 它 MUST将typed packet/result写入Prediction Source边界队列
- **AND** Prediction Schedule与Remote Presentation MUST在后续正式Tick处理
