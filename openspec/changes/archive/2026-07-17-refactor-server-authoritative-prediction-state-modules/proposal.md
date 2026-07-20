# Change: 重构ServerAuthoritative Prediction State内部模块

## Why

当前`ServerAuthoritativePredictionState`同时拥有pending input request、authority confirmation cursor、prediction history、EventId disposition journal、baseline identity validation、correction decision、restore snapshot合并、三份SnapshotParticipant codec与容量淘汰规则。单个类已接近千行，任何history、ack或correction修改都必须同时理解互相独立的状态集合与三种canonical schema。此前`prediction history capacity cannot discard unconfirmed input`问题也说明这些不变量需要更强的局部所有权。

现有外部链路与数据模型方向正确：Prediction Pipeline Pass通过唯一Prediction State port共享状态，Correction、History与Output Disposition仍是三个正式SnapshotParticipant。本change只拆内部实现并建立一个Prediction aggregate root，不修改Pipeline、协议、history策略或correction行为。

## Dependencies

- `refactor-unity-simulation-assembly-ownership`已完成全部任务并通过strict validation；按用户要求暂不归档，本change作为第二个串行change实施。
- `refactor-server-authoritative-hybrid-runtime` current specs与现有ServerAuthoritative packet/checkpoint bytes是行为基线。
- `add-dotrecast-authoritative-server-backend` MUST继续暂停；本change完成后该change直接复用同一Prediction State aggregate，不新增DotRecast专属history或correction实现。

## What Changes

- 保留`ServerAuthoritativePredictionState`作为唯一aggregate root与Source port暴露对象，但将其职责收敛为跨模块时序编排和原子状态转换。
- 新增内部Confirmation/Request模块，唯一拥有ConfirmedInputSequence、ConfirmedEventHorizon、LastAuthorityAckTick、LastBaselineTick、AuthorityClock与pending input request。
- 新增内部Prediction History模块，唯一拥有按Tick排序的history record、replay查询、journal cursor seal、confirmed pruning与capacity规则。
- 新增内部Disposition Journal模块，唯一拥有EventId entry、journal cursor、confirmation/rejection reconciliation、live-event pruning与capacity规则。
- 新增内部Reconciliation模块，唯一拥有baseline identity validation、state/body误差裁决、hard recovery/restore-replay决策以及World/Pipeline snapshot重建。
- 新增内部Prediction State codec模块，集中保存Correction v3、History v1、Journal当前schema与Pipeline projection canonical读写；迁移前后相同状态必须生成exact-byte相同payload与hash。
- 保持Correction Schedule、History Egress、Output Disposition三个Pass及其StateOwner、StateSchemaId、SchemaVersion、SnapshotParticipant顺序和restore transaction不变。
- 保持Source port、Pipeline产品、Network checkpoint、baseline、ack、EventId disposition、HistoryCapacity与MaximumReplayTicks策略不变。
- 删除旧单体类中的集合、codec、容量helper与重复checkpoint DTO；不保留旧实现委托、新旧双写或兼容reader。

## Non-Goals

- 不修正或改变网络cadence、ack算法、history容量配置、Command Slack或HardRecovery policy。
- 不改变packet、protocol、checkpoint、Program/Layout/Solver compatibility或SimulationTick epoch。
- 不合并三个SnapshotParticipant，也不增加第四份Prediction状态真相。
- 不把模块暴露为可配置策略、ScriptableObject、Source port或公共插件接口。
- 不实现DotRecast、Rollback KCC、remote presentation或动画同步。
- 不新增测试或人工验证task，不运行Unity batchmode。

## Current Spec Comparison

- `server-authoritative-prediction-correction-pipeline`已规定History、Correction与Disposition必须进入正式Pipeline和SnapshotParticipant，但未定义共享Prediction State内部所有权；本change补充aggregate root及模块不变量。
- `gameplay-simulation-pipeline`已要求SnapshotParticipant capture/restore具有事务性；本change保持三个Participant身份与bytes不变，并让跨模块操作先验证后提交。
- `add-dotrecast-authoritative-server-backend`明确禁止DotRecast专属History、Correction与Checkpoint；本change提供可直接复用的唯一模块化实现。

## Impact

- 修改能力：`server-authoritative-prediction-correction-pipeline`。
- 主要代码范围：portable `ThirdPersonSimulation.ServerAuthoritative`程序集内Prediction State、Correction Schedule、History Egress与Output Disposition调用边界。
- 不影响Unity资产、Scene、Fantasy协议、Server Gate/Authority Scene或Presentation。
- 迁移风险集中在Snapshot canonical bytes与跨模块原子性；任何schema或hash变化都视为实现失败，而不是允许升级版本。
