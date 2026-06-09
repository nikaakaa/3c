# Change: 增加基础 Locomotion 状态图配置

## Why
当前 `BasicLocomotionStateMachine` 已经使用 UnityHFSM，但 `Idle / MoveStart / MoveLoop / MoveStop` 状态和转移仍然写死在代码里。继续加入起跳、落地、闪避、KCC 或飞檐走壁之前，需要先把“状态怎么装配、转移怎么判断、动画表现怎么映射”收敛成可配置且可验证的边界，否则后续状态会像 BBB 一样逐渐变成状态类之间互相 `ChangeState`。

本变更只推进基础移动四阶段的配置化，不扩展完整角色状态机。目标是让当前 `PlayerLocomotionController -> BasicLocomotionPipeline -> UnityHFSM -> motion executor -> animation presenter` 主链保持不变，同时把状态图和动画映射从硬编码迁移到 ScriptableObject 配置资产。

## What Changes
- 新增基础移动状态图配置资产，用于描述初始状态、启用状态、转移、条件、优先级和最小时长门槛。
- 新增状态图 builder/validator，由配置构建当前 UnityHFSM 四阶段状态机。
- 保留当前四阶段语义：`Idle`、`MoveStart`、`MoveLoop`、`MoveStop`。
- 将 `MoveStartMinTime`、`MoveStopMinTime` 从“代码内固定转移语义”迁移为状态图条件可读取的配置事实。
- 新增基础移动动画集配置资产，用抽象动画意图映射 Animancer transition key。
- 明确 `RunEnd`、`WalkEnd` 等是动画变体，不是逻辑状态。
- 明确状态图只消费调用方传入的 delta/facts，保持后续 simulation tick 可调度，但不拥有 tick accumulator、tick runner 或 Unity tick driver。
- 保持 `PlayerLocomotionController` 不直接持有 `InputActionReference`，不直接依赖具体 `CharacterController` 或 KCC。
- 保持 `CharacterMotionDriver` 仅作为当前 Unity 组件适配器，不把状态图配置写进 motion adapter。
- 增加 EditMode 测试覆盖配置构建、配置校验、转移语义、动画映射和缺失配置兜底。
- 使用 Unity MCP 跑定向 EditMode 测试，并运行 OpenSpec strict 校验。

## Non-Goals
- 不新增跳跃、落地、闪避、翻滚、翻越、攻击、锁定、瞄准、飞檐走壁或完整角色主状态机。
- 不接入 KCC 包或 KCC sample，只为后续 motion executor 替换保留边界。
- 不复制 BBB 的 `PlayerBrainSO`、`PlayerStateRegistry`、`PlayerBaseState` 或状态内部互相 `ChangeState(GetState<T>())` 的写法。
- 不新增第二套玩家控制器、第二套输入路线或第二套动画播放入口。
- 不在本变更中把 Locomotion 接入 `SimulationTickRunner` 或 `UnitySimulationTickDriver`，也不新增 tick 专用控制器。
- 不让状态图配置采样 Move/Look 输入；输入采样仍属于输入端口、输入缓冲或 pipeline 上游。
- 不让动画配置反向驱动逻辑状态图；动画层只消费逻辑输出。
- 不启用 Root Motion 作为基础移动位移权威。
- 不删除现有 log，除非后续用户明确要求。

## Impact
- Affected specs:
  - `locomotion-state-graph-config`
  - `locomotion-animation-set-config`
- Related changes:
  - `integrate-unityhfsm-locomotion`
  - `refactor-wasd-to-locomotion-pipeline`
  - `standardize-basic-locomotion-animation-config`
- Affected code/assets:
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Solver/BasicLocomotionStateMachine.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Runtime/PlayerLocomotionController.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Config/BasicMovementConfigSO.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Animation/Runtime/BasicLocomotionAnimancerPresenter.cs`
  - 可能新增 `LocomotionStateGraphConfigSO`、`LocomotionStateGraphBuilder`、`LocomotionStateGraphValidator`
  - 可能新增 `LocomotionAnimationSetSO` 或等价动画映射资产
  - `3cDemo/Client/3C_Client/Assets/Tests/Editor/PlayerLocomotionControllerTests.cs` 或新增定向 EditMode 测试文件
- Validation:
  - `openspec validate add-locomotion-state-graph-config --strict --no-interactive`
  - Unity MCP 编译/刷新
  - Unity MCP 定向 EditMode 测试
  - 静态搜索确认没有新增 BBB 运行时依赖、KCC 运行时依赖或第二控制器路径
  - 静态搜索确认状态图 builder/state machine 不引用 simulation tick driver、runner 或 accumulator
  - 场景/Prefab 绑定检查，确认当前演示场景仍由 `PlayerLocomotionController` 驱动
