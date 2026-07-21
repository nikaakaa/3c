# 实现清单

## 既有唯一链路

- Graph 数据由 `BaseTreeAsset` 持有，`BaseTreeWindow`、`BaseTreeView`、page stack、Graph Data Catalog、Undo 与 selection 是唯一编辑器核心。
- `BaseGraph.CanCreateNodeType` 是节点搜索、拖拽、粘贴和脚本创建共同使用的创建门禁。
- Character authoring 通过 `CharacterPipelineAuthoringContext`提供 Blackboard 与 Character Program catalog。
- Local Session 由 `LocalSimulationSessionSourceDefinition`锁定 Actor roster，并由 `Float32LocalInputSourcePort`准备输入。
- `LocalInputIngressPass` 是 `CanonicalInputBatch`与 `TypedIngress`的唯一 writer。
- Execution Backend 在 outer transaction 开始前捕获所有 `SnapshotParticipant`，失败时恢复 checkpoint，成功后随 Session state 一起发布。
- Character input 的正式输出类型是 `CharacterSimulationInput`；continuous value、request sequence、source tick 与 Numeric Profile 均由该合同约束。
- portable `OperationControlRuntime<TTarget>` 已唯一实现 Root、Sequence、Selector、Parallel、Loop、Running、activation、abort 与 stop 生命周期。

## 已完成的边界迁移

- 玩家、Neutral 与 AI 均实现正式`ICharacterControlSourceRuntime`；输入构建上下文显式携带 Actor、Tick、Numeric Profile、sequence、tick rate 与 committed observation。
- `LocalInputIngressPass`同时绑定 Control Source、Program Runtime 与 committed observation read port，并作为唯一 Local `CanonicalInputBatch` writer及正式 state participant。
- `Float32LocalInputSourcePort`锁定完整 Actor/Control Source roster，在执行任何 source 前核对 observation roster；任一 source 或后续 pass 失败时由 outer transaction 恢复全部有状态 source。
- `SessionActorActionTargetInputProvider`只提供稳定 ActorId，玩家`ActionTargetSnapshot`由同一 committed observation解析；Actor registration Body缓存路径已经删除。
- AI Semantic IR与Character Program分别声明自己的operation set，并共同复用portable `OperationControlRuntime<TTarget>`，没有第二套control evaluator。
- Character InputRequest catalog revision 2正式发布`TimingClass`；AI Inspector、compiler与runtime只读取Character Program catalog，不读取第二份request配置。
- AI Blackboard的ActorId与ActionTargetSnapshot默认值、controller/tick state、canonical codec和hash均由AI Program正式保存；ActionTarget完整保留ActorId、Position与Yaw。
- Network Source Requirements显式保存允许的Local Control Source capability并纳入requirements hash；ServerAuthoritative当前只允许`CommittedObservation`，统一Preparation在Active前拒绝带`TransactionalState`的AI Source。

## 已确认的前置

- `add-corin-targeted-motion-warp-demo`已经提供 Control Source factory、typed `ActionTargetSnapshot` input 与训练敌人 Actor。
- 当前 Session 没有正式 Team/Faction 或公开 Gameplay observation fact 所有者；第一版 perception 只能使用显式 ActorId 候选。
- 实施前没有既有AI Controller或AI Program；实施后全局搜索仍未发现MonoBehaviour Bot、AI Command、第二request buffer或AI专用CanonicalInputBatch writer。

## 当前验证状态

- `BTSMTL.TreeDesigner`与`BTSMTL.TreeDesigner.Editor`已独立编译通过，均为0警告、0错误。
- AI Semantic IR、Float32 Program/runtime和Character AI Runtime完整编译通过；用于覆盖Unity旧项目清单的临时MSBuild清单已删除，没有进入正式工程。
- `ThirdPersonClient.Runtime`全量重跑通过，0错误；本程序集唯一警告来自既有`PipelineBlackboardValueInfoNode`未使用字段，其余警告来自Unity包依赖。
- `ThirdPersonClient.Editor`全量构建通过，0警告、0错误。
- 本change不创建Corin AI资产；artifact正式Compiler包含双编译exact-byte、IR round-trip、Program round-trip与资产Load metadata校验。

## 本 Change 明确不做

- 不修改 Agent v14 Snapshot、Patch、Validator、MCP bridge 或技能合同；该范围属于 `extend-agent-authoring-for-ai-controller`。
- 不创建 Corin AI Definition、AI Tree、Perception Profile 或 Demo 资产，不迁移训练敌人的 Neutral binding；该范围属于 `add-corin-training-ai-demo`。
- 不安装 Authority、Rollback 或 Fixed AI target；非 Local Float32 composition 遇到 AI Control Source 必须在 Active 前拒绝。
- 不增加 Team、Faction、Tag、名称、ActorId 前缀、Scene Transform、Camera 或全局 registry 感知路径。

## 完成后的统一链路

```text
AIControllerDefinition / AIControllerTree
  -> AI Semantic IR
  -> Float32 AIIntentProgram
  -> ICharacterControlSourceRuntime

committed SimulationWorldStateSet T-1
  -> CommittedActorObservationSnapshot
  -> Local Control Input Ingress
       -> Player / Neutral / AI Control Source
  -> one CanonicalInputBatch
  -> Character Program / ResolveBatch / Commit / Presentation
```
