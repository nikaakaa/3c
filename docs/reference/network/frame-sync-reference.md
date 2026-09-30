# NKGMobaBasedOnET 帧同步参考评估

## 结论

`Ref/NKGMobaBasedOnET` 只用于参考输入历史、确认、回放和同步诊断，不是 3C 的运行时依赖，也不是要整体移植的网络架构。

可以借鉴：

- 输入 sequence、Tick、确认游标和有界历史的组织方式。
- predicted input 与 canonical input 的关联方式。
- 状态快照、恢复、重演和 hash 诊断的职责拆分。
- 逻辑 Tick 与表现帧分离，回放不重复触发外部副作用。

不能照搬：

- ET Entity、NPBehave Blackboard、MOBA 技能和地图业务类型。
- 参考项目自己的全局帧同步 Runtime、物理和寻路实现。
- 把 BTSMTL authoring object、Unity Graph、Timeline asset 或 Animancer state 放进网络协议。
- 为网络模型复制一套 Character Program、Action、GameplayEffect 或 motion evaluator。

## 历史基线

本节记录最初评估该参考项目时的3C基线，已经不是当前项目状态。当前网络模型、产品闭包和运行组合以`openspec/project.md`与`openspec/specs/`为准。

当时可运行组合只有 Float32 Program Runtime + Float32 Pass Backend + Standard Local Pipeline + Local Source + Unity CharacterController Solver。当时链路是：

```text
UnityCharacterSimulationInputAdapter
-> CharacterSimulationInput
-> Local Session Source
-> compiled Standard Local Pipeline
-> Local Input Ingress Pass
-> Local Single Step Schedule Pass
-> Float32 Program Evaluate Pass
-> World ResolveBatch Pass
-> Program Finalize Pass
-> Local Immediate Output Pass
-> atomic state publish
-> Float32PipelineCommitter
-> CharacterSimulationPresentationRuntime
```

`CharacterSimulationProgram` 负责 gameplay operation，`CharacterSimulationState` 与 `WorldSimulationState` 保存可变状态。网络模型只能通过自己的 Session Source、typed Pipeline product、SnapshotParticipant、ExecutionPlan 和 output disposition 接入，不能进入 Program operation 或 WorldSolver 内部。

旧 `ServerAuthoritativeHybrid` packet、policy、history、LocalLoopback endpoint 和 session facade 当时已经删除，而正式 Prediction/Authority Source、Pipeline、Fantasy 协议和 Unity Authority Worker尚未实现。此后项目已经安装ServerAuthoritative Prediction/Authority、Unity Authority、DotRecast Authority与Deterministic Rollback产品；不得继续把本段当成当前能力清单。

`refactor-gameplay-session-composition-boundary` 已建立三个网络方向共用的唯一 `SimulationSessionHost`、Actor registration、正式 `.csim` artifact、Float32 Composer、Source preparation 和 Pipeline compiler。后续模型只能增加自己的 Source、Pass、Pipeline、协议与 Solver，不得创建模型专用 SessionHost 或复制 composition。

## 可吸收机制

### 输入身份与历史

3C 的正式输入是 `CharacterSimulationInput`：

- `Sequence` 标识输入样本顺序。
- `SimulationInputRequest.Sequence` 标识离散 request 顺序。
- `SimulationInputRequest.SourceTick` 保留请求来源 Tick。
- Action 事实使用 `ActionInstanceId`、`PredictionKey` 和 `InputSequence` 关联预测事务。

当前ServerAuthoritative Prediction Pipeline在自己的SnapshotParticipant中保存owner input/state history，并使用server tick、ack和snapshot对齐；DeterministicRollback Pipeline按Tick与stable ActorId组装canonical input bundle。两者复用输入语义，但不共享history实现或correction policy。

### ServerAuthoritative 预测与校正

ServerAuthoritative 的目标链路应是：

```text
owner CharacterSimulationInput
-> Prediction Source command port
-> Prediction Pipeline input/history products
-> authoritative observation ingress
-> correction schedule restore + replay or hard recovery
-> output disposition
-> atomic Commit
```

校正属于模型 Prediction Pipeline：有状态 Schedule/History Pass 产生完整 snapshot restore directive，并安排未确认输入重放。它不直接改 Transform，不生成 ExternalPose，不调用已删除的 MotionStage correction，也不让 Presentation 反向写 gameplay state。

远端角色的 gameplay 真相来自服务端 observation。remote presentation 可以消费 body/action/effect samples，但不得伪造 owner input 或创建第二套 Character operation runtime。

### Deterministic Rollback

参考项目的canonical input、snapshot ring、restore/replay和state hash已经由本项目独立实现在DeterministicRollback组合中。3C仍必须从同一`.csir`生成独立Fixed Program/State/Kernel ABI，并使用项目自有Deterministic KCC与Fixed Session Composer；参考项目不进入运行依赖。

Fixed backend 仍应保持核心世界所有权形状：

```text
SimulationWorldStateSet
-> WorldSimulationState
-> SimulationWorldSnapshot
```

Rollback 恢复的是完整 Tick、全部 Actor、KCC、RNG 和 command cursor，不额外发明平行 `SimulationWorldState` aggregate，也不只回滚 Transform 或单个 Action。

### 事实与副作用

Program 输出 `SimulationActorTickResult`，其中包含 typed GameplayFacts、PresentationCommands、BodySample 和稳定 EventId。模型 adapter 只消费这些结果，不读取 Blackboard slots、Graph 节点或 GameplayEffect 内部容器。

```text
SimulationActorTickResult
-> model-owned policy
-> packet/history/confirmation
-> SimulationOutputPlan
-> SimulationCommitter
```

ServerAuthoritative 使用 EventId 防止 reconciliation 重复提交；DeterministicRollback 将输出分为可替换的 predicted output 与 confirmed-only output。两者都不能把 packet DTO 变成 Character Core 合同。

## 不采用的路线

- 不新增跨所有模型共享的 packet、peer、history 或 correction Runtime。
- 不把 Local Source/Pipeline 伪装成 Network Model，也不把连接失败回退为 Local。
- 不用 endpoint enum、solver enum 或运行时 backend switch 选择组合。
- 不同步 AnimationClip、Animancer transition、Timeline visual time、Camera、VFX 或 UI state。
- 不上传客户端 resolved displacement 作为服务端 canonical pose。
- 不为 rollback 新增专用 BTSMTL 节点、Timeline evaluator 或第二份业务图。

## 历史实施顺序

1. 公共Session composition已经完成并归档。
2. ServerAuthoritative Prediction/Authority、Fantasy endpoint、Room与Unity Authority Worker已经完成并归档。
3. DotRecast Authority已经通过同一ServerAuthoritative模型接入并归档。
4. Deterministic Rollback、Fixed Target、Deterministic KCC与Relay产品已经完成并安装为current capabilities。

## 当前依据

- `openspec/project.md`
- `openspec/specs/character-simulation-kernel/spec.md`
- `openspec/specs/gameplay-network-model-boundary/spec.md`
- `openspec/specs/server-authoritative-hybrid-sync-model/spec.md`
- `openspec/specs/server-authoritative-prediction-correction-pipeline/spec.md`
- `openspec/specs/dotrecast-authoritative-server-backend/spec.md`
- `openspec/specs/deterministic-rollback-network-model/spec.md`
- `openspec/specs/gameplay-network-test-build-workflow/spec.md`

参考项目只能帮助解释机制。任何机制进入 3C 前，都必须落在现有 Program Runtime、Session Source、Pipeline Pass、WorldSolver、Snapshot、OutputDisposition 与 Presentation 边界内，不能成为第二条运行链路。
