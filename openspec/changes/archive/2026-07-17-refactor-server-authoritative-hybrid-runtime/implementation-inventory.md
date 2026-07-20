# ServerAuthoritativeHybrid 实施盘点

## 归档前协议审计

本文件第7至33行保留第一次实现的故障链作为迁移依据；“实施结果”记录修订后的最终单一路径。第一次实现已建立Prediction/Authority Session与Pipeline骨架，但统一`ObservationCadenceTicks`、Room双Actor共同horizon、routine完整Character State baseline和单KCP gameplay stream已全部删除。

## 修订前传输链审计

第一次实现的高频链路是：

```text
Prediction 60Hz Current step
-> 仅在source tick为1或命中ObservationCadenceTicks时发送ClientInputCommandBatch
-> Fantasy KCP
-> Room按两个Player的LastAcceptedSourceTick取最小共同horizon
-> AcceptedInputBatch
-> Authority
-> 按同一ObservationCadenceTicks发送包含完整baseline/body/event的ReplicationFrame
-> Room拆成ObservationFrame
-> Fantasy KCP
-> Prediction history确认与remote presentation
```

该链把Simulation、command、snapshot三个独立时钟错误合并为`ObservationCadenceTicks`，并让Room等待两个独立客户端时钟的最小值。较慢客户端或KCP排队会同时压住两个Actor的Authority输入、ACK和remote body，Authority也无法按自己的60Hz时钟持续推进。routine replication还通过KCP携带完整Character State baseline、body和重复Event，Room保存pending input、replaceable baseline/body和reliable event重发状态，职责已经越过控制面边界。

`Build/Client/NetworkLogs/20260717-002952`的四进程实机日志证明：

- Authority、Client A、Client B已经完成register/join并锁定双人roster，故障不是初始连接失败。
- Client A在`lastAckTick=15;lastBaselineTick=15;confirmedSequence=6`时耗尽256项history；其最早未确认输入为`firstTick=7;firstSequence=7`。
- Client B在`lastAckTick=36;lastBaselineTick=36;confirmedSequence=12`时耗尽256项history；其最早未确认输入为`firstTick=19;firstSequence=214`。
- 两个Prediction都继续生成本地输入，但Authority确认游标长期停留在初始区间，最终由`prediction_source_disposed`触发Room关闭；后续`RosterIncomplete`与worker connection mismatch是连锁错误。

现有日志没有逐通道packet/s、bytes/s、queue depth、Authority独立时钟和每Actor ingress游标，因此不能仅凭消息数量断言瓶颈只在KCP吞吐、Gate共同horizon或Unity帧率中的某一个点。能够确定的是旧合同把三者串成同一个阻塞链，并且ACK没有持续追上Prediction输入；修订实现必须删除该合同并补齐分通道诊断，不能用扩大history、放宽超时或吞掉异常掩盖。

## 已锁定基座

- `refactor-simulation-operation-runtime-modules` 已完成；唯一 `Float32OperationEvaluator` 位于 `ThirdPersonSimulation.Float32`，只由 `SimulationKernel` 的 Actor runtime 创建。
- `refactor-gameplay-session-composition-boundary` 已归档且 strict validation 通过。
- 公共 composition 已支持自定义 Source port、Product slot、SnapshotParticipant Pass、Restore/Replay/Authoritative ExecutionPlan 和 Source/Actor resource ownership。本 change 不修改公共 Pipeline compiler、Standard Local Pipeline、Session Host 或 Actor registration 合同。
- 本 change 不安装 DotRecast、C# KCC、Fixed、rollback 或 combat rewind；Unity Authority Worker 是唯一 gameplay authority。

## 公共合同

| 合同 | 正式 owner | 本 change 的用法 |
|---|---|---|
| Source descriptor/runtime port | `ThirdPersonSimulation.Core` | Prediction/Authority Source 只提供外部输入、观察和发送端口 |
| Pass/Product/Pipeline descriptor | `ThirdPersonSimulation.Core` | 模型声明独立 Product、Ingress/Schedule/Egress Pass 与两个 Pipeline |
| ExecutionPlan/restore/snapshot | `ThirdPersonSimulation.Core` | Prediction Schedule 生成 Restore/Replay/Current，Authority Schedule 生成 Authoritative step |
| Float32 Program Runtime/Backend/Step Pass | `ThirdPersonSimulation.Float32` | Prediction 与 Authority 共用 Evaluate/WorldSolve/Finalize |
| Composer/runtime handle/atomic commit | `Float32SimulationSessionComposer` | 两种模型组合都从同一正式入口创建 |
| Session lifecycle | `SimulationSessionHost` | Preparing/Ready/Failed、Tick target 和销毁顺序不分叉 |
| Actor/Presentation registration | `CharacterPipelineHost` 与正式 registration | owner 注册 simulation；remote 只注册 presentation |

## Standard Local 基线

Corin Local composition：`Assets/Configs/Character/Corin/Pipeline/Definition/CorinLocalSimulationSessionComposition.asset`。

| 项目 | 值 |
|---|---|
| PipelineId | `thirdperson.simulation.pipeline.standard-local` |
| Revision / Schema | `1 / 1` |
| PipelineHash | `ae6ecdc9adb8d3ed997ffd1ae1ffb79882ffd26ffbffd6ebf740d582d5c7fa5d` |
| DescriptorHash | `0ecb35f1c0744877cb0676fb201c65873457009a21b8452b709387a910968a42` |
| PlanHash | `81c0b27b0d78446ea12a3366b65f92772a6fb1076be4cc262de3e8f53334291c` |

Pass 顺序：

```text
Ingress  thirdperson.simulation.local-input-ingress@1
Schedule thirdperson.simulation.local-single-step-schedule@1
Step     thirdperson.simulation.float32-program-evaluate@1
Step     thirdperson.simulation.float32-world-resolve-batch@1
Step     thirdperson.simulation.float32-program-finalize@1
Egress   thirdperson.simulation.local-immediate-output@1
```

Local 组合不会引用 Fantasy Endpoint、prediction history、baseline、correction 或 remote presentation。

## Corin 与执行组件身份

| 项目 | 值 |
|---|---|
| ProgramId | `character:c7a7c1e3f7e64d81b5a04a90cbeb8d4e` |
| ProgramHash | `5f39ddaeb5b39290657e5e162de75e9e6b130c2de275b64acf2e7b60e22b39aa` |
| LayoutHash | `0618222660eaf877db0331ceee8056060b914614d1a6e1d234bf9c30b4215d6e` |
| Operation Set | `character-gameplay-operations/3` |
| TickRate | `60` |
| NumericProfile / ABI | `float32-ieee754 / 1` |
| Canonical bytes | `1,285,649` |
| Canonical SHA-256 | `3314aedca2d7d253702ea12df65f634598b8b88581d813b168e3755b5827ee9d` |
| Program Runtime | canonical Float32 Program Runtime |
| Execution Backend | canonical Float32 Pass Backend |
| Solver | Unity CharacterController Solver，要求 `Reconstructible` capability |

ProgramAsset 是 Unity Player 的唯一 Program 输入；worker/client 都不得读取 `Library/*.csim` 或运行 authoring compiler。

## 实施前网络与 Fantasy 盘点

- `GameplayNetworkModelDefinition` 目前只是网络模型 Definition 抽象入口，没有 ServerAuthoritative runtime。
- 仓库不存在旧 `ServerAuthoritativeHybridSession`、旧 endpoint、packet、payload、history、queue、binding、LocalLoopback 资产或 scene 引用，因此迁移以新增正式模型模块为主，不保留兼容 facade。
- Fantasy server 当前只有 `Main`、`Entity`、`Hotfix` 三工程和一个 Gate Scene 配置；没有 Room gameplay 代码。
- Unity client 当前只有 Fantasy 初始化、连接 facade 与连接设置；没有 gameplay message handler。
- Outer proto 当前仍是废弃的 FrameSync 协议，生成目录为空。
- Protocol 输入：`3cDemo/Tools/NetworkProtocol/Outer/OuterMessage.proto`。
- Server generated 输出：`3cDemo/Server/Entity/Generate/NetworkProtocol`。
- Client generated 输出：`3cDemo/Client/3C_Client/Assets/Generated/NetworkProtocol`。

## 实施结果

- `ServerAuthoritativeHybridModelDefinition`显式锁定 Fantasy Endpoint、Prediction Pipeline、Authority Pipeline、Float32 Program Runtime、Float32 Pass Backend、Unity Solver与完整模型 policy；全部引用进入稳定 identity/configuration hash。
- Prediction Source在Fantasy join、worker identity、双人roster和一次性data-plane ticket锁定后拥有唯一control/data endpoint；每个60Hz prediction tick生成immutable canonical input sample，30Hz command datagram冗余当前及前三个sample，并携带最后完整重建的snapshot/base ack。
- Client authority clock以HelloAck和snapshot tick建立同一epoch的估计值；Prediction Schedule只在正式ExecutionPlan中生成0/1/2个Current step维持3 tick command slack，不改TimeScale、动画速度或额外Update。
- Authority Source在worker register、完整roster、两个ticket handshake和双方首个canonical input到达后Ready；自己的60Hz clock不等待客户端共同horizon，每Actor独立选择exact、held或neutral input，每Tick只产生一个双Actor Authoritative step与一次World ResolveBatch。
- Fantasy Gate Scene拥有唯一固定Demo Room。Room只持有control connection、worker/client identity、roster、ticket、可靠Event/Full Checkpoint路由和失败传播，不读取command/snapshot datagram、Character state、baseline或body。
- Model-owned UDP endpoint直接承载30Hz command与20Hz snapshot。socket callback只解析header并写有界queue；Source边界消费queue，执行Program、Correction、Solver与Presentation的职责没有进入socket线程。
- Network Checkpoint Layout由validated Program Layout生成dense slot表并覆盖全部committed Character state。Routine snapshot只发送相对最后已确认base的changed-slot bitset/value、owner correction、remote body/producer、hash、input ack和event horizon；完整State codec bytes与逐slot codec字符串不再上网。
- Full与Delta共享单调SnapshotSequence。未收到新ack时worker继续相对最后已确认base发送新delta；Client只在base未知或重建失败时请求Full Checkpoint，单纯丢失一个snapshot不会阻塞后续更新。KCP Full晚于更新UDP Delta到达时不会回退snapshot或ack游标。
- Reliable Action/Effect/Cue通过KCP按EventId和event sequence只发送一次。Remote body、producer command和reliable event都按authority tick进入同一remote presentation缓冲，并在6 tick interpolation horizon到达后发布，不反向写Gameplay state。
- Prediction ACK只推进确认游标，完整baseline消费后才释放不再参与replay的历史；每个Current Tick在output disposition落账后封口journal cursor，restore port只保存并一次性消费当前待恢复快照。
- Outer proto只保留ServerAuthoritative control/reliable协议。Client与Server generated `OuterMessage.cs`由ProtocolExportTool同次导出且字节一致；旧FrameSync和旧KCP input/accepted-input/replication/observation消息、Handler与DTO均已删除。
- Bootstrap只按显式`TestScenarioId + process role`进入Client或Authority Scene，不持有Composition。专用Editor菜单生成固定三Scene Player；仓库启动脚本启动Fantasy Server、Authority、Client A、Client B并轮询control与data-plane就绪。

## 新模块所有权

| 模块 | 唯一职责 |
|---|---|
| Model contracts | identity、product、codec、policy、history、correction 和 diagnostics shape |
| Prediction Source/Pass | local canonical input、authority observation、restore/replay、history、EventId disposition、command egress |
| Authority Source/Pass | accepted input、authority clock、missing-input policy、replication egress |
| Fantasy Endpoint | generated control/reliable message、model datagram与typed Source port的转换、连接 lifecycle |
| Fantasy Room | worker/client identity、固定roster、ticket、可靠事务精确路由和Session失败传播 |
| Unity Authority Worker | 一个双 Actor Authority Session、Unity Solver 和 replication output |
| Client remote presentation | authority body/producer/fact 转正式 Presentation output，不运行 remote Program |
| Demo assets/scenes | Bootstrap 路由、Client A/B launch、Authority launch 与显式 composition |

## 删除边界

- 用正式 ServerAuthoritative Outer 协议替换 FrameSync proto；不保留双协议 DTO。
- 不引入 model-specific Driver、Session Host、Kernel、WorldSolver、Committer 或 replay loop。
- 不发送 client resolved displacement，不做 Transform correction，不同步 AnimationClip/Animancer state。
- 连接、配置、identity、history、reliable queue 或 restore 失败均 fail-stop，不回退 Local。
- Fantasy .NET 进程不得引用 UnityEngine、Float32 runtime、Character state 或 WorldSolver。

## 已有四进程运行证据

正式构建菜单为`Tools/3C/Build/Server Authoritative Network Test Player`，启动入口为`3cDemo/Tools/ServerAuthoritative/Start-ServerAuthoritativeDemo.ps1 -StopExisting`。本次证据目录为`3cDemo/Client/3C_Client/Build/Client/NetworkLogs/20260717-043807`。

- Fantasy Server、Authority、Client A、Client B持续运行超过120秒；当前Session在主动结束前保持Active，固定roster没有解锁。
- Authority推进到Tick 8400以上，Actor A/B的confirmed input都推进到8417；A/B客户端最后ack tick都为8409，snapshot sequence都为2804。
- A/B客户端各接收约2800个remote body sample，最新remote body tick均为8409；snapshot稳定约20pps，command稳定约30pps。
- 两端都显示一个本地预测Actor和一个remote presentation Actor。分别在Client A与Client B执行正式动作/位移输入后，另一端remote body持续更新；Authority最终位置由A `(1.5,-5.27)`、B `(-1.5,-5.27)`变为A `(1.313,1.775)`、B `(-1.25,-14.663)`。
- 本次A/B/Authority日志均未出现`prediction history capacity`、`prediction_source_disposed`、`pipeline_pass_failed`、`RosterIncomplete`、`worker connection mismatch`、ack回退、baseline miss、reconstruction failure、datagram oversize或queue overflow。

该次成功运行不能替代重复稳定性验证。`20260717-112515`复测在Client B一帧积累多个已重建owner baseline时暴露`AuthoritativeObservationBatch`重复ActorId：旧`DrainObservations`把队列内全部同Actor baseline提交给只允许每Actor一条记录的产品，导致Observation Ingress失败，随后才出现`prediction_source_disposed`和固定roster关闭。修订后每次Ingress只提交最新owner baseline，并保留同期全部remote body、producer和reliable event；该修订需要重新构建Player后复测。

`20260717-113607`使用新构建复测后，重复Actor baseline异常已消失，Client A在Correction重建预测分支时暴露下一处首错：新的较大input sequence回到旧Command历史中的target authority tick，旧发送层无条件头插后构造出target tick非严格递减的datagram。修订后Command冗余历史只表示当前预测分支；新样本复用或回退target tick时，先删除该边界及之后的旧分支样本，再保留更早冗余样本并插入新样本。该修订仍需重新构建Player后复测，Server日志中的`prediction_source_disposed`仅是Client Commit失败后的连锁结果。

后续四进程已能持续运行并同步双Actor，人工观察暴露两个独立配置/时钟缺口。Client Scene显示的`wall.prefab`没有进入Authority Worker Scene，因此唯一Unity Authority Solver只看见地面和两个CharacterController，角色间会碰撞但可穿过客户端独占墙体；修订后两个场景引用同一`wall.prefab`及相同Transform。Remote Presentation旧实现又在首个snapshot到达时立即从Tick 1启动表现时钟，配置的6 tick缓冲从未形成，20Hz到达抖动直接表现为远端卡顿；修订后先积满6 tick horizon，再从`LastTick - 6`按presentation delta推进。两项修订需要重新构建Player后复测。

最新人工观察进一步区分出Owner表现卡顿、Remote循环动画不同步和动作请求偶发失效。Owner旧实现直接把GameplayTickSystem的outer interpolation alpha应用到最后一个body pair，但Prediction Schedule合法产生0/1/2个Current step：零步会把同一body区间从头重播，双步会覆盖中间区间；restore/replay又会直接替换可见采样。修订后Owner Presentation缓存Committer提交的simulation body历史，以presentation delta推进独立sample时钟，并在预测分支被替换时用6 tick视觉恢复收敛到新canonical body。Remote旧实现只发布到`floor(authority presentation time)`的SampleProducer，且动画仅允许相邻tick插值；20Hz采样因此长期走自由运行。修订后SampleProducer预取到当前Body区间右端，可靠生命周期仍守住presentation horizon，动画允许按稀疏authority tick插值。动作侧确认`UnityCharacterSimulationInputAdapter`在BuildInput后清空请求，而Schedule可能生成零Current step；修订后请求由Correction Schedule持有、进入schema 3 SnapshotParticipant，并只在下一次首个Current step携带。
