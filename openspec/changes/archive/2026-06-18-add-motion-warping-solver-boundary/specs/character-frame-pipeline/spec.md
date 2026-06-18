## ADDED Requirements

### Requirement: Motion Warping 结果作为候选输出参与 Plan
Motion Warping result MUST 在角色级输出应用前被转换为 motion candidate 或等价 frame submission 数据，并参与 `CharacterFramePlan` 或批准的等价角色级计划。Character frame output applier MUST 只执行计划选择后的 warped motion，不得在 output apply 阶段临时解析 warp target 或运行 solver。

#### Scenario: Action warped motion 进入提交
- **GIVEN** Action Motion clip 通过 Motion Warping solver 生成 action motion result
- **WHEN** Action submitter 构建本帧 `CharacterFrameSubmission`
- **THEN** submission MUST 携带该 action motion candidate
- **AND** BodyArbiter 或等价 plan builder MUST 能决定该 candidate 是否成为本帧最终 motion
- **AND** output applier MUST 不重新运行 solver

#### Scenario: Action 使用共享 solver result 但保留 Action command
- **GIVEN** MotionWarpSolver 为 Action 攻击吸附或转向修正输出 MotionWarpResult
- **WHEN** Action motion resolve 构建本帧提交
- **THEN** 该 result MUST 被适配为 `ActionMovementCommand` 或批准的等价 Action motion candidate
- **AND** 系统 MUST NOT 要求 `MovementCommand` 与 `ActionMovementCommand` 在本变更中合并

#### Scenario: Locomotion warped motion 进入提交
- **GIVEN** Locomotion 状态通过动画运动源或 Motion Warping solver 生成 movement facts
- **WHEN** Locomotion submitter 构建本帧候选输出
- **THEN** movement facts MUST 进入 Locomotion motion candidate 或等价 frame data
- **AND** 最终是否执行 MUST 服从 `CharacterFramePlan`

#### Scenario: Output apply 不解析 target
- **WHEN** output applier 执行本帧 motion
- **THEN** 它 MUST 只消费已经求解好的 command 或 motion result
- **AND** MUST NOT 解析 warp target binding
- **AND** MUST NOT 查询场景目标
- **AND** MUST NOT 读取 ActionTimeline clip payload 来补算 motion

### Requirement: Motion Warping 不改变角色帧 phase 顺序
引入 Motion Warping MUST 不改变唯一 `CharacterFramePipeline` 的 phase owner 或输出副作用顺序。request submission、state/lifecycle 推进、motion resolve、plan 合成和 output apply 的职责 MUST 保持分离。

#### Scenario: Motion resolve 在 output apply 前完成
- **GIVEN** 本帧存在需要 Motion Warping 的 Action 或 Locomotion motion intent
- **WHEN** 角色帧管线进入 output compose / plan 阶段
- **THEN** warp result MUST 已经作为候选纯数据存在
- **AND** output apply 阶段 MUST 只应用最终计划选择的结果

#### Scenario: 不新增第二帧循环
- **WHEN** 新增 Motion Warping runtime 代码
- **THEN** 系统 MUST NOT 新增 MonoBehaviour Update、独立 tick adapter、第二 `CharacterFramePipeline` 或第二 output applier 来驱动 warped motion
- **AND** 正式推进 MUST 继续从现有角色帧主线进入
