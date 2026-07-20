# 实施清单

## 当前模型与产品

- Network Model `ServerAuthoritativeHybrid`：Unity Authority 与 DotRecast Authority 两个 Network Test Product 共用其输入、baseline、reconciliation 与 authority 语义。
- Network Model `DeterministicRollback`：DeterministicRollback Network Test Product 使用 fixed Program、Deterministic KCC、Peer rollback/replay 与 Relay 输入协议。
- Unity Authority 产品目录固定为 `Build/Network/UnityAuthority`，当前进程为 Fantasy Gate、Unity Authority Worker、Client A、Client B。
- DotRecast Authority 产品目录固定为 `Build/Network/DotRecastAuthority`，当前进程为 Fantasy Gate + DotRecast Authority、Client A、Client B。
- DeterministicRollback 产品目录固定为 `Build/Network/DeterministicRollback`，迁移前进程为 Unity Canonical Host、Client A、Client B；目标进程为纯 .NET Dedicated Relay Server、Client A、Client B。

## Unity Host 删除清单

- Runtime：`RollbackCanonicalInputHost.cs`、`DeterministicRollbackCanonicalHostBehaviour.cs`。
- Scene：`DeterministicRollbackCanonicalHost.unity`及其`.meta`。
- Bootstrap：`CanonicalHost` role、canonical host scene字段与`--deterministic-rollback-role=host`。
- Endpoint authoring：`m_HostPeerId`与Host handshake命名。
- Editor：`HostScenePath`、`BuildCanonicalHostScene`与Host Scene生成逻辑。
- Script：第三个Unity Host Player启动、停止、存活检查与host日志。
- Asset：`CorinRollbackEndpoint.asset`中的`m_HostPeerId`和Bootstrap Scene中的Host配置。
- Product manifest：Rollback的`NoServer`、顶层`hostIdentity`与Player closure中的Host Scene。

## InputDelayTicks 删除清单

- `DeterministicRollbackModelPolicy.InputDelayTicks`及policy hash。
- `RollbackCanonicalInputHost`的canonical lead与epoch调度。
- `RollbackRuntimeState`的predicted target tick计算。
- `DeterministicRollbackPipelineDefinition.m_InputDelayTicks`。
- `DeterministicRollbackDemoStatusOverlay`的单一input delay显示。
- `CorinDeterministicRollbackPipeline.asset`中的`m_InputDelayTicks: 4`。
- current `deterministic-rollback-network-model` spec中的全局input delay描述。

## 当前协议身份

- 原始输入：`RollbackActorInputBatch`，Actor单一、Tick连续、带冗余帧。
- canonical：`RollbackCanonicalInputBundle`，按Tick与stable ActorId排序。
- confirmation：`RollbackCanonicalConfirmation`，可靠携带完整最终bundle区间。
- hash：`RollbackStateHashReport`，由Peer生成并经中继路由。
- snapshot：`RollbackSnapshotRequest`与`RollbackSnapshotResponse`，Relay只路由，WorldState仍由选定Peer提供。
- 迁移前协议为version 3；目标协议增加Relayed Explicit消息与输入阶段身份并提升版本，旧decoder不保留。

## schema v1 删除清单

- `NetworkTestServerProductShape`、`NetworkTestServerProductResult`和`NoServer`。
- `NetworkTestProductBuildManifest.player`、`.server`与顶层`hostIdentity`。
- `INetworkTestProductBuildAdapter.BuildServer`。
- `NetworkTestProductAdapterUtility`和三个产品adapter中的固定server shape判断。
- 三个启动脚本对schema v1固定字段的读取。
- `Assert-NetworkTestProductBuild.ps1`中的schema v1断言。

## 正式主链结论

目标修改继续沿唯一链路完成：`Unity Input Adapter -> Rollback Source -> Endpoint/Relay -> Pipeline Ingress -> Schedule/Replay -> Fixed Program/KCC -> Committer -> Presentation`。Relay不执行Gameplay，Presentation不读取网络packet，不需要新增第二套Gameplay或动画同步路径。
