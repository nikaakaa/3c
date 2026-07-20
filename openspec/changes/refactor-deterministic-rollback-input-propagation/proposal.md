# Change: 重构确定性回滚DS输入传播与网络测试产品边界

## Why

当前 DeterministicRollback Demo 的远端角色并不是被真实网络抖动拖慢，而是被现有实现主动拖慢：Peer 的 `ActorInputBatch` 只提交给 `RollbackCanonicalInputHost` 的 assembler，其他 Peer 直到 canonical bundle 生成后才第一次看到该输入；而 canonical clock 又要求共同显式输入前沿领先 `InputDelayTicks=4`。在 60 Hz 下，远端连续移动至少长期落后约 66.7 ms，Peer 只能先用旧连续输入预测，再持续 restore/replay。表现层的有界纠偏只能柔化这些反复替换，无法消除输入链路制造的漂移。

当前 Demo 还把 portable 的输入排序逻辑包装成 `DeterministicRollbackCanonicalHostBehaviour + Unity Scene + 第三个 Unity Player`。这个 Unity 进程不执行角色 Gameplay，却承担网络中继，导致构建、启动、资源和进程拓扑都被无意义地绑定 Unity。第三种产品实际采用DS拓扑：一个Dedicated Relay Server加两个Unity Client；Server只拥有会话、输入排序、确认与hash/snapshot路由，不拥有角色WorldState。把它叫Host并包装成Player是当前产品装配错误。

本变更采用《For Honor》公开分享中可以确认的原则，而不猜测其未公开实现细节：网络传播输入、各参与端执行同一确定性模拟、Gameplay 区分即时预测与最终确认、动画和视觉观察不进入确定性状态；连续移动不统一增加输入延迟，只对进攻类离散请求使用显式固定延迟。项目仍使用现有 BTSMTL Semantic IR、Fixed Program、Deterministic KCC、Rollback Session 和 Presentation 主链。

## What Changes

- 将本地Demo收敛为`纯.NET Deterministic Rollback Dedicated Relay Server + Unity Client A + Unity Client B`三个进程；删除Unity Canonical Host Player、Host Scene、Host Bootstrap role和`MonoBehaviour` Host。
- 在同一输入身份的生命周期内区分 `Captured -> Relayed Explicit -> Canonical -> Confirmed` 四个阶段。它们不是四份 Gameplay 输入，也不是双写路径。
- Relay 校验 Actor 所有权后立即转发收到的原始输入批次；不得等待 canonical lead、confirmation delay 或完整 Tick bundle 才让另一 Peer 看见输入。
- Peer 为每个远端 Actor 保存有界的显式输入历史。当前 Tick 优先使用该 Tick 的 relayed explicit input；缺失时才使用上一份连续值并清空离散 request；不得预测不存在的离散动作。
- Canonical assembler 只在同一 Tick 的完整 roster 显式输入到齐后，按 `SimulationTick + stable ActorId` 生成不可变 canonical bundle。普通 canonical bundle 不再承担原始输入首次传播，也不再产生同一 Tick 的可变 revision。
- Confirmed horizon 与原始输入传播、canonical 生成分离。Relay 在 canonical 连续前沿超过独立 confirmation delay 后可靠发送最终区间；Peer 不得用 confirmed horizon 作为表现延迟缓冲。
- 删除全局 `InputDelayTicks`。连续输入和普通即时 request 不增加模型延迟；Corin Rollback Demo 的 `Offensive` request 使用 2 Tick，即 60 Hz 下约 33.3 ms 的显式延迟。
- 为 `CharacterActionRequestDefinition` 增加通用 timing class，由角色输入配置表达 request 的业务类别；具体 Tick 延迟由 Network Model policy 解释，BTSMTL、Program ABI 和普通 Local Session 不绑定 Rollback policy。
- Rollback request scheduler 在同一离散请求序列中保持捕获顺序。后捕获的 request 不得越过尚未到期的进攻 request；连续输入不进入该排序队列。
- 远端 Body 和动画继续消费 predicted current timeline。Relayed explicit input 引起的分支替换通过现有原子 Body/动画事务提交；visual follower 只处理真实迟到、丢包或恢复造成的剩余误差，不再长期掩盖固定四 Tick 陈旧输入。
- 新增受版本控制的`ThirdPerson.DeterministicRollback.Server`纯.NET executable与portable server manifest。Server内部唯一`RollbackInputRelayRuntime`不加载Unity Scene、Unity Asset、Character Program、KCC、Animancer或Fantasy ServerAuthoritative产品。
- 将公共Network Test Product manifest升级为schema v2，使用`NetworkModelIdentity + RuntimeTopologyIdentity + artifacts[]`准确描述三个测试产品；删除固定`player/server`、`ServerShape`、`NoServer`和顶层`hostIdentity`。
- Unity Authority与DotRecast Authority adapter同步迁移到schema v2，但保持现有Server Product、四进程/三进程拓扑和业务行为不变。
- Rollback Build输出`Player + Server + portable manifest`的exact closure；Run只启动既有Dedicated Relay Server和两个Client，不编译、不发布、不临时生成配置。
- 删除旧`RollbackCanonicalInputHost`、`HostPeerId`、`m_HostHandshake`命名和Unity Host专属资产/参数/诊断；保留的canonical assembler只表达最终排序，不再表达网络主机或Gameplay authority。

## Public Reference Boundary

本提案只采用以下公开资料明确支持的原则：

- GDC 2019《Back to the Future: Working with Deterministic Simulation in 'For Honor'》：只发送输入、每个参与端模拟结果、无 Gameplay authority、即时与最终状态分离、动画与观察系统不属于确定性核心。
- Ubisoft《Input Delay in For Honor》：取消随机 0/33/66 ms Time Snap，改为只对 offensive inputs 使用固定 33 ms；移动和 stance 不统一延迟。
- Ubisoft dedicated server 公告只用于说明网络承载进程可以独立于客户端产品；本提案不根据该公告推断《For Honor》后续 dedicated server 的内部 Gameplay authority 或表现平滑算法。

参考：

- https://media.gdcvault.com/gdc2019/presentations/Henry_Jennifer_BackToTheFuture.pdf
- https://www.gdcvault.com/play/1026077/Back-to-the-Future-Working
- https://www.ubisoft.com/en-us/game/for-honor/news-updates/4qgcoZf3m61lpWD8GlN4JO/input-delay-in-for-honor
- https://news.ubisoft.com/en-us/article/5vpVACm004BZquaodiArA9/for-honor-dedicated-servers-launching-february-19-on-pc

## Scope

### In Scope

- DeterministicRollback protocol、Endpoint、Source、history、canonical/confirmation 生命周期。
- Rollback 专属离散 request timing scheduler 与 CharacterInputProfile timing class。
- 纯.NET Dedicated Relay Server产品、portable manifest、Rollback Build/Run adapter和启动脚本。
- 公共Network Test Product schema v2、runtime artifact合同与三个产品adapter迁移。
- Peer 远端输入预测、restore/replay 触发条件、Body/动画原子表现替换和对应 diagnostics。
- Corin Rollback Demo 正式配置迁移与旧 Host 路径删除。

### Out of Scope

- 不修改 BTSMTL Graph、StateMachine、Timeline、Action combo 或 GameplayEffect 业务语义。
- 不修改 Fixed Program、Deterministic KCC、碰撞世界或数值 ABI。
- 不新增 Unity Gameplay Authority、Fantasy Room、ServerAuthoritative packet 或 correction 路径。
- 不实现匹配、反作弊、掉线重连、Server migration、观战或完整PvP产品。
- 不复制或声称掌握《For Honor》未公开的 visual smoothing、动画图或生产网络拓扑细节。
- 不为远端角色增加固定 confirmed render buffer；如果即时输入传播完成后仍存在残余抖动，继续由正式 diagnostics 定位真实迟到和纠偏来源。

## Impact

- Affected specs:
  - `deterministic-rollback-network-model`
  - `deterministic-rollback-two-client-demo`
  - `character-input-pipeline`
  - `character-presentation-interpolation`
  - `agent-character-controller-synthesis`
  - 新增 `deterministic-rollback-relay-product`
  - 新增 `network-test-runtime-product-boundary`
- Affected runtime:
  - `ThirdPersonSimulation.DeterministicRollback`
  - `ThirdPersonSimulation.DeterministicRollback.Endpoint`
  - `ThirdPersonSimulation.DeterministicRollback.Unity`
  - Rollback Presentation adapter 和 diagnostics projection
- Affected tooling/product:
  - `DeterministicRollbackNetworkTestBuildAndRun`
  - `NetworkTestProductBuildWorkflow` 的schema v2 model-neutral runtime artifact合同
  - `3cDemo/Tools/DeterministicRollback/Start-DeterministicRollbackDemo.ps1`
  - 新增`3cDemo/Server/Products/DeterministicRollback/ThirdPerson.DeterministicRollback.Server.csproj`
- Deleted paths:
  - `DeterministicRollbackCanonicalHostBehaviour`
  - `DeterministicRollbackCanonicalHost.unity`
  - `DeterministicRollbackProcessRole.CanonicalHost`
  - Unity Player 的 `--deterministic-rollback-role=host`
  - 旧`RollbackCanonicalInputHost`、`HostPeerId`、`m_HostHandshake`产品与协议命名
  - `NetworkTestServerProductShape`、`NetworkTestServerProductResult`与`NoServer`
  - schema v1固定`player/server`、顶层`hostIdentity`和旧parser
  - 旧全局 `InputDelayTicks` 配置、identity 字段和诊断口径

## Current Spec Comparison

- 当前 `deterministic-rollback-network-model` 规定 canonical bundle 是唯一 Gameplay 网络输入，并要求共同输入前沿持续领先 `InputDelayTicks`。这与“原始输入立即传播、canonical 只负责最终排序”直接冲突，本 change 必须修改该 requirement。
- 当前`deterministic-rollback-two-client-demo`固定两个客户端加Canonical Input Host，并固定4 Tick canonical input delay。该描述必须改为两个Unity Client加纯.NET Dedicated Relay Server，并拆除全局延迟。
- 当前 `character-presentation-interpolation` 已正确要求远端使用 predicted current timeline、不得把 confirmed horizon 当表现缓冲。本 change 保留该方向，并补充 exact relayed input 优先于 last-known prediction，follower 只处理剩余误差。
- 当前 `character-input-pipeline` 没有 request timing class。若在 Rollback 代码中按 `Attack` 字符串硬编码 2 Tick，会把业务分类隐藏在模型实现中，因此必须补充正式 authoring 语义。
- 当前公共Network Test Product manifest固定为`Player + Server`并让Rollback声明`NoServer`，同时又从Player启动第三个进程。本change必须把三个产品一次迁移到显式artifact schema v2，不能只给Rollback增加特例字段。
- `refactor-character-visual-trajectory-following` 明确把 input protocol 排除在当时 scope 外。本 change 在其后修复真正的输入传播原因，不回写或并行修改该 active change。

## Parallel Work Constraint

实施时必须先检查并行 Agent 对目标文件的最新改动，只在当前实现之上完成本 change。不得回退并行改动。若并行工作已经移动类型或模块，按本提案的所有权和行为要求迁移到新位置；不得为了套用旧路径恢复 Unity Host、旧字段或兼容 wrapper。
