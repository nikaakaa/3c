> 已被 `refactor-unified-character-state-machine` 接管：本变更不再作为后续实现基线，已有运行时/配置产物需要按统一状态机删除、回滚或归并。

## 1. 现状确认
- [x] 1.1 确认 `PlayerLocomotionController` 当前读取 Move/Look/Run 输入的入口。
- [x] 1.2 确认 `PlayerLocomotionController` 当前生成移动意图和世界方向的位置。
- [x] 1.3 确认 `PlayerLocomotionController` 当前推进 `BasicLocomotionStateMachine` 的位置。
- [x] 1.4 确认 `PlayerLocomotionController` 当前提交 `MovementCommand` 的位置。
- [x] 1.5 确认 `PlayerLocomotionController` 当前提交 `MovementAnimationContext` 的位置。
- [x] 1.6 确认 `PlayerDodgeActionController` 当前如何压制基础移动位移。
- [x] 1.7 确认 `PlayerDodgeActionController` 当前如何压制基础移动动画。
- [x] 1.8 确认 `PlayerDodgeActionController` 当前如何写入 Run latch。
- [x] 1.9 确认当前 prefab/scene 中不会同时存在多个会消费 Dodge 请求的启用入口。

## 2. FullBody 上下文模型
- [x] 2.1 定义 FullBody tick 输入上下文，包含 delta time、当前 step、Move/Look 快照或等价事实。
- [x] 2.2 定义 FullBody Locomotion 意图事实，包含 has move intent、world direction、gait 候选和当前 phase。
- [x] 2.3 定义 FullBody 行为选择结果，能表达当前 owner 是 Locomotion 还是某个 Action。
- [x] 2.4 定义 FullBody 输出包，包含可选平面运动命令、可选 base layer 动画命令和相机 Look/Resolve 请求。
- [x] 2.5 保证这些模型不引用 Animancer、Animator、AnimationClip、CharacterController、InputAction 或 Cinemachine 具体类型。

## 3. Locomotion 模块接入
- [x] 3.1 为现有基础 Locomotion 提供 adapter 或等价模块边界。
- [x] 3.2 adapter 能读取现有 input source 并生成移动输入快照。
- [x] 3.3 adapter 能在不提交位移的情况下生成移动意图和世界方向事实。
- [x] 3.4 adapter 能推进 `Idle / MoveStart / MoveLoop / MoveStop` 局部 phase。
- [x] 3.5 adapter 能构建基础移动运动命令，但是否提交由 FullBody coordinator 决定。
- [x] 3.6 adapter 能构建基础移动动画上下文，但是否提交由 FullBody coordinator 决定。
- [x] 3.7 adapter 保持 MoveStop 重新输入立即回 MoveStart 的现有行为。
- [x] 3.8 adapter 不读取 ActionRuntimeStateTracker 来决定 Locomotion phase。

## 4. Action Module 端口
- [x] 4.1 定义 FullBody Action module 标识，使用稳定 action state id。
- [x] 4.2 定义 module 进入检查端口，输入包含请求缓冲、Locomotion 意图事实和 Action tracker facts。
- [x] 4.3 定义 module 进入结果，能表达 accepted、rejected、no request 和要消费的请求。
- [x] 4.4 定义 module active tick 端口，输出动作运动命令、动作动画命令和完成事实。
- [x] 4.5 定义 module exit 端口，允许显式写回 `Action.None` 或等价空 action state。
- [x] 4.6 module 不直接调用 `CharacterController.Move`。
- [x] 4.7 module 不直接调用 Animancer 或 Animator 播放 API。
- [x] 4.8 module 不直接切换 Locomotion phase。

## 5. FullBody Coordinator
- [x] 5.1 新增 FullBody coordinator 或等价主调度入口。
- [x] 5.2 coordinator 每帧先收集输入事实和本地输入请求。
- [x] 5.3 coordinator 再生成 Locomotion 意图事实。
- [x] 5.4 coordinator 再调用 Action module 进入检查和 Action 仲裁。
- [x] 5.5 coordinator 在无 active action 时选择 Locomotion 作为 FullBody owner。
- [x] 5.6 coordinator 在 Dodge active 时选择 Dodge module 作为 FullBody owner。
- [x] 5.7 coordinator 每帧只向 motion executor 提交一个 FullBody owner 的平面运动命令。
- [x] 5.8 coordinator 每帧只向 base layer presenter 提交一个 FullBody owner 的动画命令或上下文。
- [x] 5.9 coordinator 在 action active 期间仍转交 Look 输入到项目侧相机入口。
- [x] 5.10 coordinator 不直接读取 `Camera.main`、`CinemachineFreeLook` 或具体场景相机 Transform。

## 6. 配置入口
- [x] 6.1 定义 FullBody Action 逻辑集和动作动画绑定集的分离入口。
- [x] 6.2 配置入口能列出角色可用 FullBody actions。
- [x] 6.3 配置入口能定位 `Action.Dodge` 的 Action 定义。
- [x] 6.4 `Action.Dodge` 定义能定位运动参数配置。
- [x] 6.5 `Action.Dodge` 定义能定位打断策略配置。
- [x] 6.6 动作动画绑定集能通过 `Action.Dodge` 定位动作动画 Profile。
- [x] 6.7 配置入口校验缺失 action id。
- [x] 6.8 配置入口校验重复 action id。
- [x] 6.9 配置入口校验缺失运动参数。
- [x] 6.10 配置入口校验缺失打断策略。
- [x] 6.11 动作动画绑定入口校验缺失动作动画 Profile。
- [x] 6.12 配置入口校验 Dodge 缺失 `Action.Dodge.Directional` key。
- [x] 6.13 配置入口校验 Dodge 缺失 `Action.Dodge.Backstep` key。
- [x] 6.14 配置入口不接管 Locomotion Walk/Run 状态图配置。
- [x] 6.15 配置入口不接管 Locomotion TransitionLibrary 或 alias 配置。

## 7. Dodge 迁移
- [x] 7.1 将当前 `DodgeActionRuntime` 接成 FullBody Action module 或等价 adapter。
- [x] 7.2 保留当前 Directional/Backstep 变体语义。
- [x] 7.3 保留当前 Action 仲裁接入。
- [x] 7.4 保留当前动作结束后再次按 Shift 可触发的行为。
- [x] 7.5 保留 Directional 完成后写 Run latch 的行为。
- [x] 7.6 保留 Backstep 完成后不写 Run latch 的行为。
- [x] 7.7 迁移后 `PlayerDodgeActionController` 不再作为长期独立 FullBody 动作调度入口。
- [x] 7.8 prefab/scene 中长期只启用一个 FullBody 动作调度入口。

## 8. 自动测试
- [x] 8.1 测试 FullBody coordinator 无 action 时提交 Locomotion 运动命令。
- [x] 8.2 测试 FullBody coordinator 无 action 时提交 Locomotion 动画上下文。
- [x] 8.3 测试 Dodge active 时不提交 Locomotion 平面运动命令。
- [x] 8.4 测试 Dodge active 时不提交 Locomotion base layer 动画上下文。
- [x] 8.5 测试 Dodge active 时提交 Dodge 动作运动命令。
- [x] 8.6 测试 Dodge accepted 时消费输入请求。
- [x] 8.7 测试 Dodge rejected 时保留未过期输入请求。
- [x] 8.8 测试 Dodge 完成后回到 `Action.None`。
- [x] 8.9 测试 Dodge 完成后新 pressed 请求能再次触发。
- [x] 8.10 测试 Directional 完成后 Run latch 仍生效。
- [x] 8.11 测试回到 Idle 后 Run latch 重置。
- [x] 8.12 测试 FullBody Action 逻辑集能定位 Dodge motion 和 interrupt 配置，动作动画绑定集能定位 animation 配置。
- [x] 8.13 测试 FullBody Action 逻辑集或动作动画绑定集缺失必要子配置时校验失败。
- [x] 8.14 测试 FullBody Action 逻辑集和动作动画绑定集不要求 Locomotion 配置并入 Dodge。
- [x] 8.15 静态测试 FullBody framework 不引用 `BBBNexus`。
- [x] 8.16 静态测试 Action module 不调用 `CharacterController.Move`。
- [x] 8.17 静态测试 Action module 不直接调用 Animancer 播放 API。
- [x] 8.18 静态测试 Locomotion 状态图不引用 Action module。

## 10. 验证记录
- [x] 10.1 运行定向 EditMode 测试并记录结果：`ThirdPersonAction.Tests.FullBodyActionFrameworkTests` 23/23 passed。
- [x] 10.2 运行 `openspec validate add-fullbody-action-framework --strict --no-interactive` 并记录结果：passed。
- [x] 10.3 记录静态边界检查结果：FullBody framework 无 `BBBNexus`/BBB 主控依赖，Dodge module 未直接调用 `CharacterController.Move` 或 Animancer/Animator 播放 API，Locomotion 状态图未引用 Action module，FullBody coordinator 未直接读取 `Camera.main`/Cinemachine。
- [ ] 10.4 记录用户 Play Mode 手动验证结果。
