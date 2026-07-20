# Design: 确定性回滚输入传播与纯 .NET 中继

## Context

现有 Rollback 链路是：

```text
Peer A local input
  -> ActorInputBatch
  -> Unity Canonical Host
  -> Canonical assembler
  -> 等待共同输入前沿领先 4 Tick
  -> Canonical bundle
  -> Peer B 第一次看到 A 的输入
  -> Peer B restore/replay
  -> Body/动画 branch replacement
  -> visual follower 收敛
```

`RollbackCanonicalInputHost.ReceiveInput` 当前只把 frame 提交给 assembler，没有把 frame 转发给其它 Peer。`AssembleDueBundles` 又在每次生成 bundle 前要求 `ExplicitContiguousTick >= NextTick + InputDelayTicks`。因此即使两个 Player 都运行在本机、日志没有丢包，Peer B 仍会长期用 A 的旧连续输入预测。4 Tick 在 60 Hz 下是约 66.7 ms，方向切换和转身会持续产生错误轨迹，再由表现层反复纠偏，看起来就是远端角色“飘”和“卡”。

该问题不能继续通过增大 visual half-life、增加 confirmed render delay 或吞掉 branch replacement 修复。那些方案会让错误轨迹更柔和，但会进一步增加动作延迟，并掩盖输入协议本身制造的陈旧数据。

## Goals

- 远端显式输入一到 Relay 就立即传播，不等待 canonical 或 confirmation。
- 两个 Unity Peer 都在当前预测时间线上执行同一 Fixed Program 和 KCC。
- Canonical 和 Confirmed 保留正式价值，但不再承担实时输入首次投递。
- 连续移动保持即时；进攻离散 request 使用可审查的 2 Tick 固定延迟。
- 删除Unity Host产品，只保留纯.NET Dedicated Relay Server。
- 现有 Presentation 只观察模拟输出，不参与网络同步和 Gameplay 纠偏。
- 协议、运行时、构建和配置最终只有一条正式路径。

## Non-Goals

- 不引入远端位置 snapshot 插值或服务端权威 correction。
- 不让 Relay 执行 Program、KCC、动画、Timeline 或 GameplayEffect。
- 不让动画状态进入 deterministic hash 或网络 payload。
- 不把请求延迟硬编码成 `Attack`、`Dodge` 等字符串判断。
- 不保留旧 Unity Host 作为 fallback 或调试入口。

## Terms

### Network Model与Network Test Product

当前有两个Network Model：`ServerAuthoritativeHybrid`与`DeterministicRollback`。当前有三个可独立Build/Run的Network Test Product：`UnityAuthority`、`DotRecastAuthority`与`DeterministicRollback`。前两个产品共享ServerAuthoritativeHybrid语义但使用不同Authority backend；第三个产品使用DeterministicRollback语义。产品数量不得被当成Network Model数量。

### Runtime Artifact

Network Test Product中的一个可独立校验发布闭包。Artifact Kind只表达`UnityPlayer`或`ManagedExecutable`等启动载体，不表达Fantasy、Authority、Rollback或具体Network Model。

### Dedicated Relay Server

DeterministicRollback产品中的专用服务器进程。它拥有会话、Client roster、输入合法性、canonical排序、confirmation及hash/snapshot路由，但不执行Fixed Program、KCC或Presentation。它不是Listen Host，也不是Gameplay Authority Server。

### Captured Input

Unity Peer 从设备采样得到的连续输入值和离散 request。连续值属于当前 Tick；离散 request 具有稳定 request id、capture sequence 和业务 timing class。

### Relayed Explicit Input

Relay 校验发送 Peer 与 Actor 所有权后，立即广播的同一 `RollbackActorInputFrame`。它是该 Actor 对该 Tick 的显式输入证据，不是另一份 Gameplay command。

### Canonical Input

同一 Tick 的全部 roster 显式输入到齐后，按 stable ActorId 排序得到的不可变 bundle。它确定 replay、hash 和最终历史使用的顺序事实。

### Confirmed Input

已经越过独立 confirmation delay、Relay 不再接受变化的连续 canonical 区间。它负责释放 confirmed-only 副作用和裁剪 history，不负责驱动实时远端表现。

### Predicted Current Timeline

Peer 在当前 SimulationTick 上使用本地显式输入、已到达的远端显式输入或受约束的 last-known 连续值执行出的当前分支。Body 和动画表现都消费该分支。

## Target Topology

```text
                         +----------------------------+
                         | .NET Dedicated Relay DS    |
                         | handshake / roster         |
                         | immediate input fanout     |
                         | canonical ordering         |
                         | confirmation               |
                         | hash/snapshot routing      |
                         +-------------+--------------+
                                       |
                         same protocol | same input identity
                                       |
              +------------------------+------------------------+
              |                                                 |
+-------------v-------------+                     +-------------v-------------+
| Unity Client A            |                     | Unity Client B            |
| local device input        |                     | local device input        |
| Fixed Program + KCC       |                     | Fixed Program + KCC       |
| rollback history/replay   |                     | rollback history/replay   |
| Body + animation observer |                     | Body + animation observer |
+---------------------------+                     +---------------------------+
```

运行时一共三个进程：一个纯.NET Dedicated Relay Server和两个Unity Gameplay Client。Server是DS产品，不是第三个玩家、Listen Host、Gameplay Authority或表现进程。

## Input Lifecycle

```text
Device sample
  -> request timing scheduler
  -> local explicit frame
  -> local predicted current step
  -> unreliable redundant ActorInputBatch
  -> Relay ownership validation
  -> immediate explicit frame fanout
  -> remote exact-input history
  -> remote predicted current step or earliest-tick replay
  -> complete-roster canonical bundle
  -> reliable confirmation range
  -> confirmed-only output release / history trim
```

Raw、Canonical 和 Confirmed 是同一输入的阶段升级。每个 frame 用 `ActorId + SimulationTick + InputSequence + GameplayHash` 唯一识别。相同身份只能从较早阶段晋升到较晚阶段；不得在另一个 request buffer、packet model 或 Gameplay command 中双写。

## Decisions

### Decision 1: 使用纯.NET Dedicated Relay Server，不使用Unity Host

选择：新增受版本控制的`ThirdPerson.DeterministicRollback.Server` .NET 8 executable，唯一职责是承载portable Rollback protocol与`RollbackInputRelayRuntime`。Unity Build只包含Bootstrap与Peer Scene。

业务收益：

- Demo真实表达“两个Client各自模拟，Dedicated Server只传输入和确认”。
- Relay 不加载模型、动画或场景，启动更快，资源闭包更小。
- 后续替换 UDP、部署到远端或加入房间服务时，不需要改 Gameplay Program。

代价：

- 本地 Demo 仍有一个轻量中继进程，不是纯两进程 P2P。
- Build workflow 需要正式支持 model-owned external runtime artifact。

未选择Client-host：虽然可减少一个进程，但会从DS变成Listen Host/P2P拓扑，引入host优势、host生命周期和迁移问题，并让其中一个Gameplay Client承担产品外职责。

未选择 full mesh P2P：它能减少一次中继跳转，但 roster、可靠确认、snapshot routing 和 N 个 Peer 的发送复杂度更高，不适合当前双客户端作品 Demo 的范围。

### Decision 2: 原始输入立即转发，Canonical 只负责不可变排序

Relay 收到合法 `ActorInputBatch` 后，先按 actor/tick/sequence/hash 去重，再立即转发给其它 Peer，同时将同一 frame 提交给 canonical assembler。转发不得等待同 Tick 另一个 Actor、canonical clock 或 confirmation。

`InputRedundancyCount` 是批次历史上限，不是突破 UDP payload 预算的强制帧数。Peer 每 Tick 从最新显式输入向前选择仍可完整放入一个 unreliable datagram 的最大连续后缀；当前 Tick 单帧若仍超过预算则明确失败。unreliable input 不分片，可靠的 canonical、confirmation 与 snapshot 消息继续使用既有有界分片合同。

Assembler 只有在 `NextCanonicalTick` 的完整 roster 显式输入都存在时才生成 bundle。生成后 bundle 不再 revision；同一 actor/tick 后到达但 GameplayHash 不同的 frame 是协议错误，不是正常修订。

业务收益：远端连续移动通常在目标 Tick 执行前已经可用；真实迟到时也能在 canonical bundle 之前尽早 restore/replay。

代价：协议需要同时编码 explicit frame delivery 和 canonical/confirmation phase，但三者共享同一 frame identity，不形成第二套 Gameplay 输入。

### Decision 3: 删除全局四 Tick 延迟，拆成两个独立策略

```text
Explicit relay delay:             0 Tick，网络到达即传播
Continuous input model delay:     0 Tick
Immediate request model delay:    0 Tick
Offensive request model delay:    Corin Demo 为 2 Tick
Canonical assembly lead:          0 Tick，只等待当前 Tick roster 齐备
Confirmation delay:               独立配置，当前可保持 4 Tick
```

原 `InputDelayTicks` 同时承担公平性、canonical lead 和远端可见延迟，语义混杂，必须删除。新的 `OffensiveRequestDelayTicks` 只影响被分类为 Offensive 的本地离散请求；`ConfirmationDelayTicks` 只影响最终确认。

业务收益：移动和转身没有人为 66.7 ms 陈旧；进攻动作仍具有双方一致的 33.3 ms 输入窗口，更接近《For Honor》公开说明的选择性延迟思路。

代价：Rollback Demo 的本地攻击会比单机模式晚 2 Tick 开始。这是明确的网络玩法取舍，不应伪装成渲染平滑。

### Decision 4: Timing class 属于输入 authoring，Tick 数属于模型 policy

`CharacterActionRequestDefinition` 增加稳定 timing class：

```text
Immediate
Offensive
```

它只表达 request 的业务类别。Standard Local、Preview 和其它模型可以映射为 0 Tick；DeterministicRollback policy 将 Offensive 映射为 2 Tick。BTSMTL 和 Program 仍只消费最终 `CharacterSimulationInput.Requests`，不读取 Network Model。

Rollback Unity input adapter 维护模型专属的有界 pending request schedule。request 捕获时确定 eligible tick；到期后才写入正式 Fixed input frame。该队列属于 Rollback Source 的模型状态，必须参与 Source checkpoint/恢复合同，不能藏在无快照的 UI 状态中。

为防止输入反转，同一离散请求序列严格保持 capture sequence。后捕获的 Immediate request 不得越过前面尚未到期的 Offensive request。连续 Move/Look 值不进入该队列，仍每 Tick 立即采样。

业务收益：作者能在 CharacterInputProfile 中看见哪些 request 是进攻输入；模型不按字符串猜业务。

代价：如果未来业务需要“尚未生效的攻击可被闪避直接取消”，必须把它定义成正式 request scheduling/cancel 语义，而不能偷偷允许 sequence 越序。本 change 先采用顺序稳定的公开方案。

### Decision 5: Peer 优先使用 exact relayed input

每个 Peer 的 input source 为每个远端 Actor 保存有界 explicit history：

```text
目标 Tick 有 relayed explicit frame
  -> 使用 exact values + exact requests
否则
  -> 使用最近连续 values
  -> requests 为空
```

显式 input 晚于本地已执行 Tick 到达且 GameplayHash 不同时，Source 记录 earliest affected tick，由现有 Schedule 产生一次原子 restore/replay。相同 GameplayHash 的 provenance 晋升只更新输入证据，不触发 replay 或表现 replacement。

Canonical bundle 到达时，如果其 actor frames 与 explicit history 的 GameplayHash 完全一致，只推进 canonical contiguous frontier；不得再次 replay。同一事实不能因为从 Relayed 晋升为 Canonical而造成视觉跳变。

### Decision 6: Presentation 保持预测当前时间线

Presentation 不接收网络位置或动画状态。链路保持：

```text
Fixed Program/KCC result
  -> Rollback output disposition
  -> atomic Body + animation branch commit
  -> CharacterBodyPresentationRuntime
  -> CharacterAnimationPlaybackRuntime
  -> Animancer
```

当 explicit input 在目标 Tick 前到达时，两端会直接模拟相同轨迹，不需要表现纠偏。真正晚到造成 replay 时，Body 与动画必须在同一 outer transaction 提交最终净分支；`CharacterVisualTrajectoryFollower` 从当前 visible pose 接管剩余误差。Canonical provenance 晋升不得创建第二次 correction。

动画不通过网络同步 clip/time。Gameplay Program 在每个 Peer 上从同一输入产生同一 producer lifecycle；Animancer 只负责本地高帧率采样和 fade。confirmed-only cue 继续等 confirmation，移动循环和可撤销动作动画消费 predicted current。

### Decision 7: Dedicated Relay Server使用portable manifest，不读取Unity Asset

Build 生成 Relay runtime manifest，至少包含：

- SchemaVersion、BuildId、ProductId。
- SessionId、listen endpoint、expected peer/actor roster。
- ModelId、ProtocolId/Version、TickRate。
- SemanticHash、Fixed ProgramHash、LayoutHash。
- CollisionWorldHash、Kcc identity/capabilities。
- ConfirmationDelayTicks、capacity、snapshot authority。

Relay Server只用这些事实做handshake、容量和路由校验，不加载`.asset`、`.scene`、`.csim`或角色prefab。Manifest与Server executable都进入network test product exact closure和hash。

`NetworkTestProductBuildWorkflow`升级为schema v2，顶层使用：

```text
ProductId
NetworkModelIdentity
RuntimeTopologyIdentity
ProgramIdentity
PipelineIdentity
Artifacts[]
Launch
Fields
Files
```

它不再保存固定`player/server`、`ServerShape`或含糊的`hostIdentity`。每个artifact显式声明RoleId、Kind、ProductId、root、entry point、configuration identity与可选manifest path/hash。公共workflow只拥有Unity Player build、staging、candidate validation、exact closure和原子替换；adapter发布附加artifact。公共workflow不引用Rollback Server、Fantasy Server Product或任何具体adapter类型。

三个产品一次迁移到schema v2：

```text
UnityAuthority
  unity-player
  unity-authority-gate-server

DotRecastAuthority
  unity-client-player
  dotrecast-authority-server

DeterministicRollback
  unity-client-player
  deterministic-relay-server
```

Unity Authority与DotRecast Authority的运行行为不变。Run只读取schema v2，不保留schema v1 parser或自动迁移。

### Decision 8: Snapshot source仍是Peer，不是Relay Server

Relay 继续路由 hash report、snapshot request/response。完整 world snapshot 由模型 policy 指定的 Peer 提供，当前仍为 lowest stable PeerId。Relay 不拥有 WorldState，也不因“中继”身份变成 Gameplay authority。

## State And Identity Changes

### Policy

删除：

- `InputDelayTicks`
- 由 input delay 派生的 canonical epoch lead identity

新增：

- `OffensiveRequestDelayTicks`
- `ConfirmationDelayTicks` 保持独立
- explicit history/redundancy/capacity identity

Model、Protocol、Endpoint、Pipeline revision 必须随 schema 和 policy identity 更新。旧版本直接拒绝握手，不提供兼容解码。

### Input Provenance

现有 provenance 需要收敛为能表达以下状态的正式集合：

```text
LocalExplicit
RelayedExplicit
PredictedContinuous
PredictedNeutral
CanonicalExplicit
ConfirmedExplicit
```

具体枚举数量可以在实现时按不重复职责收敛，但必须区分“显式内容”和“预测内容”，并允许同 GameplayHash 的阶段晋升不触发 replay。

### Source Snapshot

Rollback Source 的模型状态至少保存：

- pending offensive request schedule 与 next request sequence。
- 每 Actor 有界 explicit input history。
- applied input/provenance frontier。
- earliest affected tick。
- canonical contiguous 和 confirmed frontier。

这些状态不得进入 CharacterSimulationState，也不得由 Presentation 保存。

## Product Layout

```text
Build/Network/DeterministicRollback/
  NetworkTestProduct.json
  Player/
    3C_Client.exe
    ...exact Unity Player closure
  Server/
    ThirdPerson.DeterministicRollback.Server.exe
    ...exact .NET runtime closure
    DeterministicRollbackServerManifest.json
```

Unity Player scene closure：

```text
Assets/Scenes/DeterministicRollback/DeterministicRollbackBootstrap.unity
Assets/Scenes/DeterministicRollback/DeterministicRollbackPeer.unity
```

旧 `DeterministicRollbackCanonicalHost.unity` 不得进入 Assets、Build Settings、manifest 或启动脚本。

Run 顺序：

```text
1. 校验 ProductManifest 和全部 exact hashes
2. 启动Dedicated Relay Server executable
3. 等待Server endpoint ready
4. 启动 Peer A Player
5. 启动 Peer B Player
```

Run 不编译、不 publish、不复制文件、不重写 manifest。

## Diagnostics

Peer diagnostics 必须区分：

- local captured tick 与 eligible tick。
- offensive request pending count/oldest age。
- relayed explicit arrival lead/late ticks。
- exact remote input hit count/rate。
- predicted continuous/neutral count。
- explicit correction count 与 earliest affected tick。
- canonical provenance-only promotion count。
- replay count/depth/replayed ticks。
- Body/animation branch replacement count。
- follower correction magnitude 与 hard snap reason。

Relay diagnostics 必须区分：

- received/forwarded/deduplicated/invalid input batch。
- per-peer latest explicit contiguous tick。
- canonical contiguous tick。
- confirmed tick。
- reliable resend/drop 与 snapshot routing。

不得只显示一个含糊的 `input delay` 数字。

## Failure Rules

- 同一 Actor/Tick/InputSequence 出现不同 GameplayHash：关闭 session，报告协议冲突。
- 发送 Peer 提交其它 Actor input：关闭该连接，不转发。
- Relay manifest 与 Peer handshake identity 不一致：SimulationTick 不得开始。
- exact history/capacity 耗尽：明确失败，不扩大到无界容器，不回退 last-known 作为 canonical。
- canonical Tick 缺少某 Actor explicit input：canonical frontier 停止；Peer 可在 MaximumRollbackDepth 内继续预测。
- Peer 超过最大预测深度：Schedule 返回 NoStep，等待输入前沿，不丢弃 history。
- Relay 退出：Peer 结束当前 Demo session，不切换为 Peer-host 或 Local 模式。

## Migration And Deletion

迁移顺序：

1. 先建立新 protocol identity、explicit history 和 portable Relay runtime。
2. 再让 Peer Source 消费 relayed explicit input，并确认 canonical 晋升不重复 replay。
3. 再迁移 timing policy 和 Corin CharacterInputProfile。
4. 再将三个Network Test Product迁移到schema v2并接入Server executable和新启动脚本。
5. 最后删除Unity Host code、scene、role、旧字段、旧诊断、旧产品schema和旧命名。

最终不得保留：

- 运行时选择Unity Host或.NET Relay Server的开关。
- schema v1 parser、固定`player/server`字段或顶层`hostIdentity`。
- 旧协议 decoder 或旧 `ProtocolVersion` 兼容分支。
- `InputDelayTicks` 到新字段的 fallback。
- canonical-only input path。
- remote snapshot/Transform correction path。
- ServerAuthoritative/Fantasy adapter 复用。

## Risks And Tradeoffs

### Relay 立即转发仍可能迟到

UDP 仍可能丢包或乱序。批次冗余和 reliable canonical/confirmation 提供恢复；真正迟到仍会 replay。目标不是消灭网络物理延迟，而是删除当前固定制造的四 Tick 陈旧。

输入 schema 增长会减少同一 datagram 内可携带的历史帧数。该取舍优先保持标准 MTU 与低时延单包输入；如果单个当前帧已经装不下，必须优化正式 wire schema 或拒绝该组合，不能调大 datagram、静默丢字段或改用可靠输入。

### 进攻延迟会改变本地手感

2 Tick 是明确业务选择。它提高双方看到进攻开始时间的一致性，但本地攻击不再是零延迟。该值只属于 Rollback model profile；单机调动画和手感不受影响。

### Relay 不是 Gameplay authority

中心进程让 roster 和最终确认更简单，但不能用它的存在偷渡服务端权威世界。如果未来需要 authoritative anti-cheat，那是另一 Network Model，不应修改本 change 的 Relay 职责。

### 表现层可能仍有小幅残余纠偏

只传输入的模型无法保证包总在目标 Tick 前到达。即时转发会大幅减少持续漂移，但偶发晚包仍需要 branch replacement 和 follower。若业务以后接受固定本地呈现延迟，可以另提 change 设计 render buffer；本 change 不提前加入。

## Open Questions Resolved

- 是否需要第三个 Unity 进程：不需要。
- 是否完全不要第三进程：不采用；保留纯 .NET Relay 负责 roster、转发、排序、确认和 snapshot routing。
- 是否同步动画：不同步动画状态；同步输入并在两端模拟相同 Gameplay producer。
- 是否把 4 Tick 全部改成 0：删除全局 4 Tick；移动为 0，进攻 request 为 2，confirmation 仍独立配置。
- 是否给远端增加固定表现延迟：不增加，继续 predicted current。
- 是否修改 BTSMTL：不修改。
