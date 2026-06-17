# Change: 拆分 Rollback Debug Rig 与角色正式 Runtime

## Why
当前本地 rollback / synctest / latency 工具以多个 MonoBehaviour 形式靠近角色装配，DebugRunner、Recorder、PredictionSource 和 Replay Adapter 都倾向于从角色自身或父子层级自动解析依赖。这让“角色正式运行时能力”和“验证工具支架”边界变模糊，也会让正式 prefab 看起来像承载了多套回滚/测试能力。

本变更用于把 rollback debug tooling 收敛为独立 `RollbackDebugRig` prefab：工具可以引用角色、驱动同一条正式 `CharacterFrameRuntimeController` 主线，但不得成为角色正式 runtime 组件集合的一部分。

## What Changes
- 定义独立 `RollbackDebugRig` prefab 的装配边界，用于承载 F6/F7/F8 runner、输入历史 recorder、快照 recorder、prediction source 和 replay adapter。
- 要求正式角色 prefab / 正式场景角色实例只承载角色运行时主线组件，不常驻 rollback debug runner、soak runner、latency runner 或历史 recorder。
- 将 `FullBodyRollbackSimulation` 或等价 `ILocalRollbackSynctestSimulation` Unity adapter 定位为 Debug Rig / 测试 adapter，通过显式引用接入角色主线，而不是角色正式能力。
- 修正 latency debug runner 的规格描述：Debug runner 挂在 Debug Rig 上并引用目标角色，不再要求挂载在角色上。
- 保持 replay / synctest / reconciliation 仍通过 `CharacterFrameRuntimeController`、`CharacterFramePipelineHost` 和现有 snapshot/restore 接口推进，不新增第二角色控制器或 fallback 路径。

## Out of Scope
- 不实现真实网络 rollback 或 Fantasy transport。
- 不修改 rollback snapshot 字段语义、比较域或 TurnBack 抢占生命周期。
- 不新增独立角色控制器、第二 motion executor、第二 animation presenter 或第二 state machine runner。
- 不删除本地 rollback/synctest 工具本身；本变更只规划归属和装配边界。

## Impact
- Affected specs:
  - `local-rollback-synctest-foundation`
  - `fullbody-rollback-replay`
  - `local-latency-reconciliation`
- Affected code:
  - `Assets/Scripts/Simulation/Rollback/*DebugRunner.cs`
  - `Assets/Scripts/Simulation/Rollback/*Recorder.cs`
  - `Assets/Scripts/Simulation/Rollback/FullBodyRollbackSimulation.cs`
  - `Assets/Scripts/Simulation/Rollback/LocomotionPredictionInputFrameSource.cs`
  - Corin 正式 prefab / 场景装配校验
- Tests:
  - Unity EditMode 定向测试覆盖独立 `RollbackDebugRig` prefab 显式引用、正式角色 prefab 不挂 rollback debug tooling、runner 仍能通过同一角色帧主线执行。
  - 静态边界测试覆盖 Debug tooling 不成为正式 gameplay runtime 组件或 fallback 路径。

## 用户验收方式
在 Play Mode 中使用独立 `RollbackDebugRig` prefab 实例触发 F6/F7/F8。正式 Corin 角色对象上不应再看到 rollback debug runner、history recorder 或 replay adapter 作为常驻组件；触发工具后仍应输出原有 PASS/FAIL 诊断，并且未启用 apply result 时角色现场会恢复。
