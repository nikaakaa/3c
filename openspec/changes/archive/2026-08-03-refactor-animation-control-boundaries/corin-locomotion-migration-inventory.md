# Corin Locomotion 迁移清单

## 1. Gameplay Locomotion 状态

Corin Locomotion StateMachine 当前包含 8 个 Gameplay 状态：

1. `Idle`
2. `WalkStart`
3. `WalkLoop`
4. `WalkEnd`
5. `RunStart`
6. `RunLoop`
7. `RunEnd`
8. `MovingTurn`

`ActionOverride` 不是 Gameplay locomotion 语义，而是用来在 FullBody Action 期间接管 BaseLocomotion 表现路由的旧表现状态，迁移后删除。

## 2. Gameplay Locomotion 状态边

以下边保留在 Gameplay：

| 起点 | 终点 | 条件 |
|---|---|---|
| Enter | Idle | `locomotion/Enter_to_Idle_Rule` |
| Idle | WalkStart | `Idle To WalkStart Condition` |
| WalkStart | WalkLoop | `WalkStart To WalkLoop Condition` |
| WalkStart | WalkEnd | `State_To_State_Rule 3` |
| WalkLoop | WalkEnd | `locomotion/WalkLoop_to_WalkEnd_Rule` |
| WalkLoop | MovingTurn | `move_has + turn_facing_angle` |
| WalkEnd | Idle | `locomotion/WalkEnd_to_Idle_Rule` |
| WalkEnd | WalkStart | `WalkEnd To WalkStart Condition` |
| WalkEnd | MovingTurn | `move_has + turn_facing_angle` |
| RunStart | RunLoop | `RunStart To RunLoop Condition` |
| RunStart | RunEnd | `State_To_State_Rule 10` |
| RunLoop | RunEnd | `locomotion/RunLoop_to_RunEnd_Rule` |
| RunEnd | Idle | `locomotion/RunEnd_to_Idle_Rule` |
| RunEnd | WalkStart | `RunEnd To WalkStart Condition` |
| MovingTurn | WalkLoop | `move_has + state_root_completed` |
| MovingTurn | WalkEnd | `move_stop` |

以下 11 条边只服务旧 Action 表现接管，迁移后全部删除：

- `Idle`、`WalkStart`、`WalkLoop`、`WalkEnd`、`RunStart`、`RunLoop`、`RunEnd`、`MovingTurn` 到 `ActionOverride` 的 8 条入边。
- `ActionOverride` 到 `RunLoop`、`Idle`、`WalkStart` 的 3 条出边。

## 3. Locomotion Timeline 内容

| Timeline | 播放 | AnimationTrack | AnimationClip | 帧范围 | 其它保留内容 |
|---|---|---|---|---|---|
| `CorinIdleTimeline` | Loop | `dc73675d-ae72-48f0-9e57-edcbd0bb55ad` | `7dbd59f643bcfbe4495990d45ad359ef` | 0..161 | 无 |
| `CorinWalkStartTimeline` | Finite | `9f166e87-b3bc-48ca-a90a-7ef476cfe3bd` | `90d528fc935ce2e43b4b301c8c2b98db` | 0..75 | 无 |
| `CorinWalkLoopTimeline` | Loop | `762b4d39-92af-42f3-b8c0-7ac9be15afb7` | `a42a3b1b15a10b44f9892c728d72b74f` | 0..36 | 无 |
| `CorinRunStartTimeline` | Finite | `7e3c2795-f922-44d1-bb5b-b1dbe5baf99a` | `c3f6deae23e56064fb4b8938601e35e5` | 0..63 | 无 |
| `CorinRunLoopTimeline` | Loop | `e9e8b58c-4813-4b9f-9e32-8b9f2b57d3f9` | `3747a185f0711e842b4df7c03fa2cfac` | 0..30 | 无 |
| `CorinRunEndTimeline` | Finite | `681cd134-2253-4956-8ffb-55a893974192` | `9654782e7ebbcd14bab93c76dc248019` | 0..136 | 无 |
| `CorinMovingTurnTimeline` | Finite | `e1e9df79-5033-4856-857e-0060a28517f2` | `e569e81bd2858154b9bf4f2e660cf981` | 0..71 | `MotionCurveTrack` `76fe3f2c-9807-4d2a-9aa7-3f3fde9ce616`，`MovingTurn` 曲线，0..71 |

7 条 Timeline 都没有 Window、TreeClip 或 Cue。7 条 AnimationTrack 迁入 Pose Graph 后删除；`MovingTurn` 的 MotionCurveTrack 留在 Gameplay Timeline，继续负责真实运动。该 Timeline 没有 Animation producer，因此运行时不要求 Action context；只有实际包含 Animation producer 的有限 Action Timeline 才保留并校验 Action context。

## 4. Marker 与 Foot Analysis

- `WalkLoop` 使用 `Locomotion.Gait`、循环拓扑、leader：`RightFootContact@0`、`LeftFootContact@18`。
- `RunLoop` 使用 `Locomotion.Gait`、循环拓扑、leader：`RightFootContact@0`、`LeftFootContact@15`。
- 其余 5 个源没有 marker。
- 7 个旧 AnimationClip 都有常量 1 的 `FootPlacementCurve`。迁移后曲线归各自只读 Pose source binding，不再归 Timeline clip。
- 当前 Profile 的 Foot Analysis 是唯一全局 binding：
  - analysis id：`Corin.FootPlacementAnalysis`
  - analysis version：`1`
  - algorithm：`animation-foot-analysis/v4`
  - calibration id：`Corin.FootPlacementRig`
  - calibration revision：`57d707ff8f86229addb0b61f4684bfefd64a2d25eee2d175e2ecd4c40d7c2e92`
  - artifact hash：`6aa73e203604bbe96a58e9fd73a7e15887531d666e1245eff980cf1d90bb8930`

## 5. `HasActionLocomotionOwnership`

旧声明 id 是 `84a5c3f0-04e7-41cb-8898-515f2ebd3a7f`。

- 8 个读取点分别位于 8 条普通 Locomotion 状态到 `ActionOverride` 的入边条件。
- 3 个读取点分别位于 `ActionOverride` 到 `RunLoop`、`Idle`、`WalkStart` 的出边条件，并经过 `NotNode`。
- `Attack State Body` 在 Enter 写 `true`、Exit 写 `false`。
- `Dodge State Body` 在 Enter 写 `true`、Exit 写 `false`。

迁移后 11 个读取点、4 个写入点、声明和只服务这些读写的条件节点全部删除。Action admission、Action StateMachine、有限 Action Timeline 和中断语义保留。

## 6. BaseLocomotion producer 与投影

旧 Program 中共有 7 个 `BaseLocomotion` Animation producer：

| Program index | Timeline | Track | 迁移前旧source asset（已删除） |
|---|---|---|---|
| 18 | WalkStart | `9f166e87-b3bc-48ca-a90a-7ef476cfe3bd` | `CorinWalkStartAnimationSource.asset` |
| 19 | WalkLoop | `762b4d39-92af-42f3-b8c0-7ac9be15afb7` | `CorinWalkLoopAnimationSource.asset` |
| 20 | MovingTurn | `e1e9df79-5033-4856-857e-0060a28517f2` | `CorinMovingTurnAnimationSource.asset` |
| 21 | RunStart | `7e3c2795-f922-44d1-bb5b-b1dbe5baf99a` | `CorinRunStartAnimationSource.asset` |
| 22 | RunEnd | `681cd134-2253-4956-8ffb-55a893974192` | `CorinRunEndAnimationSource.asset` |
| 23 | RunLoop | `e9e8b58c-4813-4b9f-9e32-8b9f2b57d3f9` | `CorinRunLoopAnimationSource.asset` |
| 24 | Idle | `dc73675d-ae72-48f0-9e57-edcbd0bb55ad` | `CorinIdleAnimationSource.asset` |

上述7个迁移前顶层source asset已在2026-08-01确认全项目不可达后删除；当前持续Pose source只存在于Pose Graph/Profile子资产。

旧投影绑定有三层，迁移后都删除：

1. `CorinAnimationPresentationProfile.m_ProducerBindings` 中 7 条 Timeline/Track 到 source asset 的绑定。
2. `CorinCharacterPipelineDefinition.PresentationProjection.m_Producers` 中 index 18..24 的 7 个 Program producer。
3. Pose Plan 中 `BaseLocomotion` SelectionInput 及其 source map、MarkerSync、SelectedPosePlayer、lifecycle、retention 和诊断数据。

## 7. 唯一迁移 owner

| 旧数据 | 唯一 owner 或结论 |
|---|---|
| Locomotion 输入、移动控制、8 个 Gameplay 状态与 16 条业务边 | BTSMTL Gameplay |
| `MovingTurn` MotionCurveTrack | BTSMTL Gameplay Timeline |
| 7 个 AnimationClip resource | Presentation Pose source binding |
| WalkLoop、RunLoop marker topology | 对应 Presentation Pose source binding |
| 7 个 FootPlacementCurve | 对应 Presentation Pose source binding |
| Locomotion pose 选择与连续状态切换 | Pose Graph `PoseStateMachine` |
| Clip 采样、循环、时间连续性 | Pose Graph `SequencePlayer` |
| Locomotion 分支转场 | 分支内 `Inertialization` |
| FullBody Action 插入与回落 | Pose Graph `AnimationSlot` |
| Action admission、Action StateMachine、有限 Action Timeline、Motion、MotionWarp、Window、Cue、中断和生命周期 | BTSMTL Gameplay |
| 7 条 Locomotion AnimationTrack、7 个 BaseLocomotion producer、旧 Profile producer binding、旧 Selection/MarkerSync/SelectedPosePlayer 链 | 删除 |
| `ActionOverride`、11 条关联边、`HasActionLocomotionOwnership` 声明与全部读写 | 删除 |
