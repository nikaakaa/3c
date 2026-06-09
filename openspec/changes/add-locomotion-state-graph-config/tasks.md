## 1. 准备与边界确认
- [x] 1.1 读取本 change 的 `proposal.md`、`design.md`、`tasks.md` 和 spec delta。
- [x] 1.2 读取 `docs/agents/unityhfsm-usage-guide.md`，确认 UnityHFSM builder 使用约束。
- [x] 1.3 读取 `integrate-unityhfsm-locomotion` 的 proposal/design/tasks，确认不回退已完成的端口边界。
- [x] 1.4 搜索 `BasicLocomotionStateMachine`、`BasicMovementPhase`、`BasicMovementSettings` 引用，记录状态图接入点。
- [x] 1.5 搜索 `BasicLocomotionAnimancerPresenter`、`RunEnd`、`WalkEnd`、`TransitionLibrary` 引用，记录动画映射接入点。
- [x] 1.6 搜索 `InputActionReference`、`CharacterController.Move`、`KinematicCharacterMotor`，确认本变更不扩大输入或运动实现边界。
- [x] 1.7 读取 BBB 的 `PlayerBrainSO`、`PlayerStateRegistry`、`PlayerMoveStartState`、`PlayerStopState`，只记录可参考配置点和需要避免的互跳耦合。
- [x] 1.8 读取 `add-simulation-tick-system` 的 proposal/design/spec，确认 tick 只调度现有 Locomotion 主线。
- [x] 1.9 若实施需要新增第二玩家控制器、复制 BBB 主线、接入 KCC、接入 tick driver、或让动画 key 参与逻辑状态判断，停止并回到 OpenSpec。

## 2. 状态图配置数据模型
- [x] 2.1 新增 `LocomotionStateGraphConfigSO` 或等价 ScriptableObject。
- [x] 2.2 配置资产包含初始状态字段。
- [x] 2.3 配置资产包含启用状态列表。
- [x] 2.4 配置资产包含转移列表。
- [x] 2.5 转移项包含 `from` 状态。
- [x] 2.6 转移项包含 `to` 状态。
- [x] 2.7 转移项包含显式优先级。
- [x] 2.8 转移项包含条件列表。
- [x] 2.9 条件第一版支持 `HasMoveIntent`。
- [x] 2.10 条件第一版支持 `NoMoveIntent`。
- [x] 2.11 条件第一版支持 `MoveStartMinTimeReached`。
- [x] 2.12 条件第一版支持 `MoveStopMinTimeReached`。
- [x] 2.13 条件第一版支持 `Always` 或等价无条件。
- [x] 2.14 提供当前四阶段默认图创建方法或默认 asset。
- [x] 2.15 默认图包含 `Idle`、`MoveStart`、`MoveLoop`、`MoveStop`。
- [x] 2.16 默认图初始状态为 `Idle`。
- [x] 2.17 默认图包含当前六条基础转移。

## 3. 状态图上下文与条件解析
- [x] 3.1 新增 `LocomotionStateGraphContext` 或等价运行时事实容器。
- [x] 3.2 上下文包含当前是否有移动意图。
- [x] 3.3 上下文包含当前阶段已运行时间。
- [x] 3.4 上下文包含调用方传入的 deltaTime 或 fixed delta 事实。
- [x] 3.5 上下文包含 `BasicMovementSettings`。
- [x] 3.6 上下文不持有 `SimulationTickRunner`、`UnitySimulationTickDriver` 或 tick accumulator。
- [x] 3.7 实现条件解析器，不依赖 UnityEngine 表现对象。
- [x] 3.8 条件解析器正确处理负 deltaTime 已被钳制后的阶段时间。
- [x] 3.9 条件解析器不读取 Animancer、CharacterController、Camera、Cinemachine、InputAction 或 KCC。
- [x] 3.10 条件解析器不采样 Move/Look action、输入缓冲或设备状态。

## 4. 状态图校验器
- [x] 4.1 新增 `LocomotionStateGraphValidator` 或等价校验入口。
- [x] 4.2 校验空配置或空状态列表。
- [x] 4.3 校验初始状态必须存在于启用状态列表。
- [x] 4.4 校验每条转移的 `from` 状态存在。
- [x] 4.5 校验每条转移的 `to` 状态存在。
- [x] 4.6 校验每条转移至少有一个条件，或显式允许 `Always`。
- [x] 4.7 校验同一来源状态下优先级冲突时给出可读诊断。
- [x] 4.8 校验完全重复的转移时给出可读诊断。
- [x] 4.9 校验从初始状态不可达的启用状态。
- [x] 4.10 校验默认四阶段图必要转移齐全。
- [x] 4.11 校验结果包含错误列表和警告列表，便于测试和后续编辑器显示。

## 5. UnityHFSM builder 接入
- [x] 5.1 新增 `LocomotionStateGraphBuilder` 或等价 builder。
- [x] 5.2 builder 接收状态图配置和运行时 context。
- [x] 5.3 builder 先运行 validator，错误配置不得静默构建。
- [x] 5.4 builder 注册配置中的全部状态。
- [x] 5.5 builder 显式设置配置中的初始状态。
- [x] 5.6 builder 按优先级处理同来源状态转移。
- [x] 5.7 builder 将配置条件转换为 UnityHFSM transition predicate。
- [x] 5.8 builder 保留 `onTransition` 重置阶段计时的现有语义。
- [x] 5.9 builder 不引用 Animancer、CharacterController、KCC、Camera、Cinemachine 或 InputAction。
- [x] 5.10 builder 不引用 `SimulationTickRunner`、`UnitySimulationTickDriver`、tick accumulator 或服务端 tick driver。

## 6. `BasicLocomotionStateMachine` 改造
- [x] 6.1 为 `BasicLocomotionStateMachine` 增加接收状态图配置的构造路径。
- [x] 6.2 保留无参构造，使用默认四阶段图并暴露诊断。
- [x] 6.3 保留 `Phase` 只读属性。
- [x] 6.4 保留 `PhaseTime` 只读属性。
- [x] 6.5 保留 `ActivePath` 诊断输出。
- [x] 6.6 保留 `Reset()` 回到初始状态并清零计时。
- [x] 6.7 保留 `Tick(bool hasMoveIntent, float deltaTime, in BasicMovementSettings settings)` 或等价 API，降低 pipeline 改动范围。
- [x] 6.8 确认当前四阶段转移行为与现有测试一致。
- [x] 6.9 确认状态机核心不依赖 MonoBehaviour。
- [x] 6.10 确认状态机核心可以由固定 delta 调用方推进，且不需要知道 tick id。

## 7. `PlayerLocomotionController` 配置接入
- [x] 7.1 在 `PlayerLocomotionController` 上增加状态图配置引用。
- [x] 7.2 初始化状态机时优先使用绑定配置。
- [x] 7.3 未绑定状态图配置时使用默认图兜底。
- [x] 7.4 未绑定状态图配置时输出可诊断信息，不静默隐藏。
- [x] 7.5 保持 `PlayerLocomotionController` 不引用 `InputActionReference`。
- [x] 7.6 保持 `PlayerLocomotionController` 不直接调用 `CharacterController.Move` 或 KCC API。
- [x] 7.7 保持 tick 顺序：输入、意图、相机方向、状态、运动命令、动画、相机 Resolve。
- [x] 7.8 迁移当前演示场景或 prefab，绑定默认状态图资产。
- [x] 7.9 本变更不接入 `UnitySimulationTickDriver` 或新增 tick 专用控制器。
- [x] 7.10 若后续需要 tick adapter，只记录为后续 change，不在本变更创建分裂路径。

## 8. 动画集配置数据模型
- [x] 8.1 新增 `LocomotionAnimationSetSO` 或等价 ScriptableObject。
- [x] 8.2 动画集包含 `Idle` 映射。
- [x] 8.3 动画集包含 `MoveStart + Walk` 映射。
- [x] 8.4 动画集包含 `MoveStart + Run` 映射。
- [x] 8.5 动画集包含 `MoveLoop + Walk` 映射。
- [x] 8.6 动画集包含 `MoveLoop + Run` 映射。
- [x] 8.7 动画集包含 `MoveStop + Walk` 映射。
- [x] 8.8 动画集包含 `MoveStop + Run` 映射。
- [x] 8.9 动画集第一版输出 Animancer transition key，不直接持有逻辑状态实例。
- [x] 8.10 动画集不写入角色位置、速度或状态机。
- [x] 8.11 提供当前 `Idle / WalkStart / RunStart / WalkLoop / RunLoop / WalkEnd / RunEnd` 默认映射 asset 或创建方法。

## 9. 动画 presenter 接入
- [x] 9.1 `BasicLocomotionAnimancerPresenter` 增加动画集配置引用。
- [x] 9.2 presenter 用动画集解析 key，替代硬编码 key 选择。
- [x] 9.3 presenter 保留 `runInputThreshold` 或等价步态解析策略。
- [x] 9.4 presenter 保留 `lastMovingGait`，用于 `MoveStop` 动画变体选择。
- [x] 9.5 presenter 确认 `RunEnd` 映射发生在动画层，逻辑状态仍是 `MoveStop`。
- [x] 9.6 缺失动画映射时输出可读错误。
- [x] 9.7 presenter 不调用状态机 `ChangeState`、`RequestStateChange` 或等价逻辑切换 API。
- [x] 9.8 presenter 不调用 `CharacterController.Move` 或 KCC API。
- [x] 9.9 迁移当前演示场景或 prefab，绑定默认动画集资产。

## 10. EditMode 测试：状态图配置
- [x] 10.1 测试默认图 validator 无错误。
- [x] 10.2 测试默认图初始状态为 `Idle`。
- [x] 10.3 测试默认图包含四个基础状态。
- [x] 10.4 测试默认图包含六条基础转移。
- [x] 10.5 测试缺初始状态会返回错误。
- [x] 10.6 测试转移来源状态缺失会返回错误。
- [x] 10.7 测试转移目标状态缺失会返回错误。
- [x] 10.8 测试重复转移会返回错误或警告。
- [x] 10.9 测试不可达状态会返回错误或警告。
- [x] 10.10 测试优先级冲突会返回可读诊断。

## 11. EditMode 测试：状态机行为
- [x] 11.1 使用配置图构建状态机，验证 start state 为 `Idle`。
- [x] 11.2 使用配置图测试 `Idle -> MoveStart`。
- [x] 11.3 使用配置图测试 `MoveStart -> MoveLoop` 最小时长门槛。
- [x] 11.4 使用配置图测试 `MoveStart -> MoveStop`。
- [x] 11.5 使用配置图测试 `MoveLoop -> MoveStop`。
- [x] 11.6 使用配置图测试 `MoveStop -> Idle` 最小时长门槛。
- [x] 11.7 使用配置图测试 `MoveStop -> MoveStart`。
- [x] 11.8 测试 `Reset()` 回到配置的初始状态并清零计时。
- [x] 11.9 测试负 deltaTime 不推进阶段时间。
- [x] 11.10 测试 `ActivePath` 仍输出可诊断路径。
- [x] 11.11 测试使用固定 delta 推进时，`MoveStartMinTime` 和 `MoveStopMinTime` 语义与当前入口一致。

## 12. EditMode 测试：动画集
- [x] 12.1 测试默认动画集能解析 `Idle`。
- [x] 12.2 测试默认动画集能解析 `MoveStart + Walk` 为 `WalkStart`。
- [x] 12.3 测试默认动画集能解析 `MoveStart + Run` 为 `RunStart`。
- [x] 12.4 测试默认动画集能解析 `MoveLoop + Walk` 为 `WalkLoop`。
- [x] 12.5 测试默认动画集能解析 `MoveLoop + Run` 为 `RunLoop`。
- [x] 12.6 测试默认动画集能解析 `MoveStop + Walk` 为 `WalkEnd`。
- [x] 12.7 测试默认动画集能解析 `MoveStop + Run` 为 `RunEnd`。
- [x] 12.8 测试 `RunEnd` 解析不改变逻辑阶段，阶段仍为 `MoveStop`。
- [x] 12.9 测试缺失必要动画映射会返回可读错误。

## 13. EditMode 测试：主链边界
- [x] 13.1 使用 fake input source 和 fake motion executor 测试 controller 可被配置状态图驱动。
- [x] 13.2 测试 pipeline 仍输出正确 `MovementCommand`。
- [x] 13.3 测试 animation context 仍只包含抽象逻辑事实。
- [x] 13.4 反射测试 `PlayerLocomotionController` 不持有 `InputActionReference` 字段。
- [x] 13.5 反射测试 `BasicLocomotionStateMachine` 不持有 Animancer、CharacterController、KCC、Camera、Cinemachine 或 InputAction 字段。
- [x] 13.6 静态或反射测试 presenter 不引用状态图 builder 或 motion executor。
- [x] 13.7 静态或反射测试状态图 builder/state machine 不持有 `SimulationTickRunner`、`UnitySimulationTickDriver`、tick accumulator 或服务端 tick driver 字段。
- [x] 13.8 静态或反射测试状态图配置和 builder 不读取 Move/Look action 或输入缓冲。

## 14. 资产和场景迁移
- [x] 14.1 创建基础移动默认状态图资产。
- [x] 14.2 创建基础移动默认动画集资产。
- [x] 14.3 将当前玩家 prefab 或演示场景绑定默认状态图资产。
- [x] 14.4 将当前动画 presenter 绑定默认动画集资产。
- [x] 14.5 检查 `Sandbox.unity` 中 `PlayerLocomotionController` 绑定有效。
- [x] 14.6 检查 `CameraTest.unity` 中 `PlayerLocomotionController` 绑定有效。
- [x] 14.7 检查 `CinemachineTest.unity` 中 `PlayerLocomotionController` 绑定有效。

## 15. 自动验证
- [x] 15.1 使用 Unity MCP refresh/compile，确认无编译错误。
- [x] 15.2 使用 Unity MCP 运行状态图配置相关 EditMode 测试。
- [x] 15.3 使用 Unity MCP 运行动画集相关 EditMode 测试。
- [x] 15.4 使用 Unity MCP 运行 `PlayerLocomotionControllerTests` 或新增合并测试集。
- [x] 15.5 使用 Unity MCP 检查三个当前演示场景的关键绑定。
- [x] 15.6 运行 `openspec validate add-locomotion-state-graph-config --strict --no-interactive`。
- [x] 15.7 静态搜索确认没有新增 `BBBNexus` 运行时依赖。
- [x] 15.8 静态搜索确认没有新增 `KinematicCharacterMotor` 运行时依赖。
- [x] 15.9 静态搜索确认没有新增第二玩家移动控制器路径。
- [x] 15.10 记录 Unity Console 错误、警告和已知历史日志。
- [x] 15.11 静态搜索确认状态图 builder/state machine 没有新增 `SimulationTickRunner`、`UnitySimulationTickDriver` 或 tick accumulator 依赖。
- [x] 15.12 静态搜索确认本变更没有把 `UnitySimulationTickDriver` 绑定到当前玩家场景或 prefab。

## 16. 手动验证
- [ ] 16.1 打开当前演示场景并进入 Play Mode。
- [ ] 16.2 按 W/A/S/D，确认角色仍按相机平面方向移动。
- [ ] 16.3 转动 Look 输入，确认相机和移动方向仍正常。
- [ ] 16.4 确认 Idle、MoveStart、MoveLoop、MoveStop 表现仍触发。
- [ ] 16.5 确认停止跑步时播放 RunEnd 映射，但逻辑阶段显示为 MoveStop。
- [ ] 16.6 临时移除状态图绑定，确认默认图兜底有诊断信息且行为不崩溃。
- [ ] 16.7 临时移除动画集映射，确认 presenter 报可读错误且不影响位移主链。

## 17. 收尾
- [x] 17.1 确认所有任务完成后再将 checklist 标为完成。
- [x] 17.2 向用户说明新增配置资产路径和 Inspector 配置方式。
- [x] 17.3 向用户说明如何验证状态图转移。
- [x] 17.4 向用户说明如何配置 `RunEnd` 这类动画变体。
- [x] 17.5 向用户说明 Unity MCP 测试结果和任何剩余风险。
