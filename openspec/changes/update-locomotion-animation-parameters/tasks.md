## 1. 数据模型

- [ ] 1.1 定义基础移动动画 entry 数据，包含 key、fade duration、speed、normalized start time、exit duration mode、exit duration override。
- [ ] 1.2 定义解析后的基础移动动画 timing 数据，至少包含 exit duration。
- [ ] 1.3 为 entry 提供默认值，保持现有 `Idle / WalkStart / RunStart / WalkLoop / RunLoop / WalkEnd / RunEnd` key 语义。
- [ ] 1.4 为负数 fade、speed、normalized start time、exit duration 约定钳制或 fallback 规则。

## 2. 动画配置资产

- [ ] 2.1 扩展 `LocomotionAnimationSetSO`，让每个移动动画映射返回 entry 或等价解析结果。
- [ ] 2.2 保留现有 key 字段或提供兼容迁移路径，避免当前资产立刻失效。
- [ ] 2.3 增加 `ResolveEntry(phase, gait, lastMovingGait)` 或等价 API。
- [ ] 2.4 增加 `ResolveTiming(phase, gait, lastMovingGait, fallback)` 或等价 API。
- [ ] 2.5 扩展配置校验，发现空 key、非法 speed、缺失 stop exit duration 时返回可读诊断。

## 3. 状态机时长输入

- [ ] 3.1 扩展纯数据 settings 或 state graph context，使状态机能读取当前 `MoveStop` 退出时长。
- [ ] 3.2 保留 `MoveStopMinTime` 作为无动画 timing 时的 fallback。
- [ ] 3.3 保持 `MoveStartMinTimeReached` 仍读取移动配置，不被本次动画时长改动影响。
- [ ] 3.4 调整 `MoveStopMinTimeReached` 或新增等价条件，使其读取当前停止动画退出时长。
- [ ] 3.5 确认 `MoveStop -> MoveStart` 的 `HasMoveIntent` 转移优先级仍高于 `MoveStop -> Idle`。

## 4. Pipeline 与 Presenter

- [ ] 4.1 在 `PlayerLocomotionController` 或 pipeline 主链中解析当前停止动画 timing，并传入状态机所需的纯数据。
- [ ] 4.2 确认解析 timing 不要求状态机引用 Animancer 或 Unity 场景对象。
- [ ] 4.3 调整 `BasicLocomotionAnimancerPresenter` 使用 entry 中的 key、fade、speed、normalized start time 播放动画。
- [ ] 4.4 保持 Presenter 对相同 phase 和 key 的重复提交不从头重播。
- [ ] 4.5 保持 Presenter 不调用状态机切换 API、不调用运动执行 API、不写 Transform。

## 5. 自动测试

- [ ] 5.1 测试默认动画 entry 能解析旧 key 语义。
- [ ] 5.2 测试 `RunEnd` exit duration override 生效。
- [ ] 5.3 测试 `WalkEnd` exit duration override 生效。
- [ ] 5.4 测试 `MoveStop` 无输入时未达到 stop exit duration 保持 `MoveStop`。
- [ ] 5.5 测试 `MoveStop` 无输入时达到 `RunEnd` exit duration 后进入 `Idle`。
- [ ] 5.6 测试 `MoveStop` 无输入时达到 `WalkEnd` exit duration 后进入 `Idle`。
- [ ] 5.7 测试 `MoveStop` 中重新出现移动输入时立即进入 `MoveStart`。
- [ ] 5.8 测试缺少 stop exit duration 且无 fallback 时 validator 返回错误。
- [ ] 5.9 测试状态机、状态图 builder 和条件 evaluator 不引用 Animancer、CharacterController、KCC、Camera、Input System。
- [ ] 5.10 测试 Presenter 不引用状态图 builder 或运动执行具体实现。

## 6. 资产与场景验证

- [ ] 6.1 更新或确认当前默认基础移动动画配置资产具备 entry 默认值。
- [ ] 6.2 确认当前角色 prefab 或场景仍引用同一基础移动动画配置路径。
- [ ] 6.3 确认没有新增第二套角色控制器、第二套移动入口或 BBB 运行时依赖。
- [ ] 6.4 确认现有 log 未被删除。

## 7. 验证命令

- [ ] 7.1 运行 `openspec validate update-locomotion-animation-parameters --strict --no-interactive`。
- [ ] 7.2 运行定向 Unity EditMode 测试 `PlayerLocomotionControllerTests`。
- [ ] 7.3 如果 Unity MCP 或 Unity 测试不可用，记录未执行原因和手动验证步骤，不伪造测试结果。

## 8. 手动验证步骤

- [ ] 8.1 打开当前演示场景，确认角色基础移动可运行。
- [ ] 8.2 持续移动后松开输入，确认进入 `MoveStop` 并播放 `RunEnd` 或 `WalkEnd`。
- [ ] 8.3 不再输入，确认停止动画按配置 exit duration 完成后进入 `Idle`。
- [ ] 8.4 在停止动画中途重新输入，确认立即进入 `MoveStart` 并播放起步动画。
- [ ] 8.5 修改 `RunEnd` exit duration，重复验证回 `Idle` 的等待时间发生对应变化。
