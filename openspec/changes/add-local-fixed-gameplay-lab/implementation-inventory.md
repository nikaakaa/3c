# 实施基线清单

## Float Local

- Unity程序集：`ThirdPersonSimulation.Unity`。
- Program runtime：`Float32ProgramRuntimeDefinition`。
- Backend：`Float32PassExecutionBackendDefinition`。
- Source：`LocalSimulationSessionSourceDefinition`。
- Pipeline：`StandardLocalSimulationPipelineDefinition`。
- Composer：`Float32SimulationSessionComposer`。
- Solver：`UnityCharacterControllerWorldSolverDefinition`。
- Actor registration：`Float32SimulationActorRegistration`。

## Fixed公共Core

- Portable程序集：`ThirdPersonSimulation.Fixed`。
- Composer：`ThirdPersonSimulation.Fixed.FixedSimulationSessionComposer`。
- Program runtime：`FixedProgramRuntime`。
- Backend：`FixedPassExecutionBackend`。
- Session request：`FixedSimulationSessionCompositionRequest`。
- Snapshot codec：`FixedSimulationSessionSnapshotCodec`。
- World pass：`FixedWorldResolveBatchPass`。
- Program evaluate/finalize：`FixedProgramEvaluatePass`、`FixedProgramFinalizePass`。

## 当前Rollback Unity装配

- Unity程序集：`ThirdPersonSimulation.DeterministicRollback.Unity`。
- 通用Fixed类型当前仍位于Rollback命名空间：
  - `FixedProgramRuntimeDefinition`
  - `FixedPassExecutionBackendDefinition`
  - `IFixedSimulationActorRegistration`
  - `FixedSimulationOutputAggregate`
  - `FixedSimulationDiagnosticsAggregate`
  - `FixedCharacterSimulationProgramAsset`
  - `FixedCharacterSimulationDiagnosticsAdapter`
  - `FixedCharacterPresentationOutputAdapter`
  - `DeterministicKccWorldSolverDefinition`
  - `UnityFixedSimulationSessionComposer`
- Rollback专属类型：
  - `IDeterministicRollbackSimulationActorRegistration`
  - `IDeterministicRollbackPreparedSource`
  - `DeterministicRollbackSessionSourceDefinition`
  - `DeterministicRollbackPipelineDefinition`
  - `RollbackRuntimeState`
  - `RollbackOutputCommitter`
  - `RollbackHistoryCommitter`
  - `IFixedSimulationRestoreSource`
  - `IRollbackNetworkDiagnosticsSource`
  - `RollbackEndpointDefinition`
  - `DeterministicRollbackCharacterHost`

## 不可变Rollback语义

- Source必须继续显式拥有Endpoint roster、LocalPeerId与Rollback model policy。
- Pipeline必须继续保留exact、predicted、canonical、confirmed input阶段。
- Restore继续来自snapshot history，confirmed output继续由Rollback committer裁决。
- Local Fixed不得创建上述对象，也不得用跳过pass或单Peer配置复用Rollback pipeline。

## 当前生成产物身份

- ProgramId：`character:c7a7c1e3f7e64d81b5a04a90cbeb8d4e`。
- SourceRevision：`0a4d01a6e9c4360c28578d3cf0c3634516527186ffefa44e5b0a46343e211877`。
- Float Numeric Profile：`float32-ieee754`，Target ABI `6`。
- Float ProgramHash：`88a2dca272c5ea87c279c4b21952001fb48fe0c6a0f482a55ab6230658cfcf2b`。
- Float LayoutHash：`92662b682e7ed331e8cda6c911a4f38f3944eecdf49d4dc9e8d4d594d3d08ef7`。
- 旧“Projection绑定Float ProgramHash”口径已删除；正式Projection只绑定ProgramId、SourceRevision、SemanticHash、ordered producer contract生成的ContractHash与独立ProjectionRevision，Float32与Fixed通过各自Adapter复用同一Projection。
- Fixed ProgramHash：`2be1199c43613d488e3fd9b79f7132f83fcacd6fe3d45ed99369362221a2e085`。
- Fixed LayoutHash：`f1c62affc958bfcb82639d4a44639f2c9c765a3be9be7b8552be5c5ea070f71f`。
- 以上是SourceMap格式迁移前的基线；正式重生成后必须整体变化，ProgramId必须不变。

## 当前碰撞与KCC身份

- collision asset：`CorinDeterministicCollisionWorld.asset`。
- MapId：`deterministic-rollback-demo`。
- ContentHash：`e065a37a92a76a160b83ba29161fce7fe9ef3e095232ccf0c04302a868ca7a71`。
- KCC asset：`CorinDeterministicKcc.asset`。
- KCC配置包含capsule、skin、坡度、step、ground snap、query/movement tolerance、candidate/contact/pair capacity与iteration。
- KCC ConfigurationHash由`deterministic-kcc-configuration/5`及全部正式字段计算，不在Gameplay Lab另存一份。

## 当前手感参数边界

- `LocomotionInputMotionNode`当前只保存`MoveSpeed=4`与`TurnSpeedDegrees=720`，Float和Fixed runtime每Tick直接把输入乘以MoveSpeed生成位移，输入归零时立即归零。
- `CharacterBodyMotionProfile`唯一保存`GravityAcceleration=-25`与`MaximumFallSpeed`，两项进入Semantic IR和两个Numeric Target的Program descriptor。
- 当前Program没有加速、减速、急停阈值、空中横向控制或落地速度衔接的正式字段，也没有对应state owner；因此现状只能表现“即时满速/即时停止”，不能把基础KCC碰撞正确误写成手感已经调好。
- 这些缺口必须扩展`LocomotionInputMotionNode -> Semantic constant -> Float/Fixed locomotion runtime`唯一链路，并进入SemanticHash与ProgramHash。不能把倍率放在Gameplay Lab、EditorPrefs、Presentation或KCC Definition中。
- KCC Definition继续只拥有capsule、ground、step、slide、query和capacity等碰撞策略；角色加减速不进入KCC ConfigurationHash。

## 唯一环境作者来源

- 环境Prefab：`Assets/Scenes/Shared/CharacterMovementTestEnvironment.prefab`。
- 运动课程Prefab：`Assets/Scenes/Shared/OpenKCCMovementCourse.prefab`。
- Standalone、DeterministicRollback与ServerAuthoritative场景已经复用该环境。
- Gameplay Lab必须继续实例化这一份Prefab；Fixed只消费它Bake出的现有collision asset。

## 并行边界

- AI Controller change已经安装Float32 Committed Actor Observation；snapshot和read port当前仍位于`Core/Float32/AI`，本change要求的model-neutral最终port尚未出现，因此第10节保持未开始。
- Equipment change正在修改Semantic frontend、catalog、emitter、Program semantics与Float/Fixed execution。
- Local Fixed只消费并行change最终合同，不复制Observation、不覆盖其编译器与Corin资产改动。
