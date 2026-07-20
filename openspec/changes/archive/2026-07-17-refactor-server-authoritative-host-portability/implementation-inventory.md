# Authority Host Portability 实施盘点

## 前置状态

- `refactor-gameplay-session-composition-boundary`与`refactor-server-authoritative-hybrid-runtime`已归档并进入current specs。
- 本change保持现有Outer协议、generated代码、Room Handler、WorldSolver、WorldBodyBinding与Actor registration合同不变。
- 当前Corin Authority仍使用两个固定Actor、Float32 Program Runtime、Float32 Pass Backend和Unity CharacterController Solver；Solver迁出Network Model由后续`add-dotrecast-authoritative-server-backend`负责。

## 迁移前身份基线

- Authority Pipeline Id：`thirdperson.simulation.pipeline.server-authoritative-authority`。
- Authority Pipeline Revision：`1`。
- Authority Pipeline Schema：`1`。
- Authority Pipeline Descriptor Hash由相同Model Policy、Replication Policy、固定Pass顺序和相同Pass configuration canonical lowering产生；迁移不得修改任一输入。
- Corin Model Policy：60Hz simulation、30Hz command、20Hz snapshot、3 tick slack、6 tick remote interpolation、1200-byte datagram、256 history、8 tick lead/lag、64 replay、0.05 position tolerance、2 degree yaw tolerance、reuse-last-input。
- Corin Authority Source Policy：command queue 256、reliable queue 1024、full checkpoint queue 1024、command liveness 600、heartbeat 120、catch-up 4、clock lag 120。
- Source Policy Hash唯一由`ServerAuthoritativeAuthoritySourcePolicyCodec`的schema 1 canonical bytes产生。
- Backend identity保持`Float32PassExecutionBackend.Descriptor.Identity`。
- Command、snapshot与hello继续使用`ServerAuthoritativeGameplayDatagramCodec`和`ServerAuthoritativeDatagramPayloadCodec`。
- Full checkpoint继续使用`NetworkCheckpointCodec`；reliable event与replication继续使用`ServerAuthoritativeEgressCodec`。

## 正式所有权

- `ServerAuthoritativeAuthorityPipelineCatalog`：唯一构造Authority descriptor、Pass factory catalog、runtime factory catalog和product runtime catalog。
- `ServerAuthoritativeAuthoritySourcePolicy`：唯一保存Authority queue、clock和rate policy，并提供canonical codec/hash。
- `ServerAuthoritativeAuthoritySourceRuntime`：唯一拥有locked route、command queue、authority tick、checkpoint baseline、snapshot sequence、ack cursor、reliable/full-checkpoint output queue和Source ports。
- `IServerAuthoritativeAuthorityControlTransport`：只交换register、roster、ticket、heartbeat、reliable event、full checkpoint、leave和failure。
- `IServerAuthoritativeAuthorityDataTransport`：只承载routine hello、command和snapshot datagram。
- `ServerAuthoritativeAuthorityHostLaunchRequest`：校验完整portable组合并调用唯一`Float32SimulationSessionComposer`。
- Unity Authority Definition、Source preparation和Fantasy connection只负责authoring lowering、Unity/Fantasy adapter与lifecycle装配。

## 删除清单

- Unity侧Authority descriptor与runtime factory拼装。
- Unity侧Authority command queue、clock、checkpoint baseline、snapshot sequence和replication lowering。
- 只为镜像固定Authority Pass顺序存在的Unity Authority Pass Definition与资产。
- 重复command/snapshot/full-checkpoint codec或packet mapper。
- Host launch中的默认Backend、Pipeline、Source、Solver或roster选择。
- 任何Unity/portable双写queue、兼容Source和旧factory fallback。
