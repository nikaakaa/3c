> 已被 `refactor-unified-character-state-machine` 接管：本变更不再作为后续实现基线，HFSM 缝合层产物需要按统一状态机删除、回滚或归并。

## 1. 现状复核
- [x] 1.1 读取 `add-fullbody-action-framework` 当前实现和剩余任务，确认本变更只做状态树可见性，不重复 coordinator 能力。
- [x] 1.2 确认 `PlayerFullBodyActionController` 当前 owner 选择位置。
- [x] 1.3 确认 `BasicLocomotionStateMachine.ActivePath` 当前可用。
- [x] 1.4 确认 `DodgeFullBodyActionModule` active/completed 事实可供 HFSM transition 使用。
- [x] 1.5 确认没有新增 BBB 运行时依赖。

## 2. 状态 ID 和快照模型
- [x] 2.1 定义 FullBody root 状态 ID：`Locomotion`、`Action` 或等价 ID。
- [x] 2.2 定义 Action 子状态 ID，第一版至少表达 `Action.Dodge`。
- [x] 2.3 定义统一状态路径格式，例如 `/FullBody/Locomotion/MoveLoop`。
- [x] 2.4 定义 `FullBodyStateSnapshot` 或等价模型，包含 owner、路径、Locomotion phase、Action state、状态时间。
- [x] 2.5 快照模型不引用 UnityHFSM 内部状态对象、Animancer、Animator、AnimationClip、CharacterController、InputAction 或 Cinemachine。

## 3. FullBody HFSM Builder/Driver
- [x] 3.1 新建 FullBody HFSM builder 或等价装配类。
- [x] 3.2 builder 创建 FullBody root。
- [x] 3.3 builder 创建 `Locomotion` 子状态。
- [x] 3.4 builder 创建 `Action` 子状态。
- [x] 3.5 builder 创建 `Action.Dodge` 子状态。
- [x] 3.6 transition 读取 context 中的最终 Action 仲裁结果或 module active 事实。
- [x] 3.7 transition 不直接读取 Input System、Animancer、CharacterController 或 Camera。
- [x] 3.8 暴露 active hierarchy path 和 pending transition 诊断信息。

## 4. Locomotion 子树接入
- [x] 4.1 将现有 `BasicLocomotionStateMachine` 输出映射到 `FullBody/Locomotion/*` 路径。
- [x] 4.2 不复制 `Idle / MoveStart / MoveLoop / MoveStop` transition 条件。
- [x] 4.3 Walk/Run 仍作为 gait 事实，不进入 FullBody 逻辑状态 ID。
- [x] 4.4 `MoveStop -> MoveStart` 仍由现有 Locomotion 局部状态图处理。
- [x] 4.5 Action active 时 Locomotion 可继续生成事实，但不得提交位移或 base layer 动画。

## 5. Dodge Action 子状态接入
- [x] 5.1 `Action.Dodge` 进入仍通过现有 Action 仲裁。
- [x] 5.2 `Action.Dodge` active tick 仍复用 `DodgeFullBodyActionModule`。
- [x] 5.3 `Action.Dodge` 运动输出仍通过统一 action motion executor。
- [x] 5.4 `Action.Dodge` 动画输出仍通过 action animation presenter。
- [x] 5.5 `Action.Dodge` 完成后回到 `FullBody/Locomotion/*`，并保持 Directional Run latch 行为。
- [x] 5.6 Backstep 完成后不写 Run latch。

## 6. Coordinator 迁移
- [x] 6.1 `PlayerFullBodyActionController.CurrentOwner` 来源改为 HFSM snapshot 或等价状态树结果。
- [x] 6.2 移除长期依赖 `if dodge active else locomotion` 的 owner 权威。
- [x] 6.3 保持 `PlayerFullBodyActionController` 只负责端口连接、输入读取、命令提交和 Unity 引用解析。
- [x] 6.4 保持每帧最多一个平面位移 owner。
- [x] 6.5 保持每帧最多一个 base layer 动画 owner。
- [x] 6.6 保持 Look 输入在 Action active 期间继续进入项目侧相机入口。
- [x] 6.7 删除旧 `PlayerDodgeActionController` MonoBehaviour 调度入口，避免 Dodge 继续存在 per-action controller 分裂路径。
- [x] 6.8 从可琳 Prefab 移除旧 Dodge controller 组件，只保留 `PlayerFullBodyActionController` 作为 FullBody Action 入口。
- [x] 6.9 清理空的 `Assets/Scripts/Character/Action/Runtime` 文件夹，FullBody 运行时代码统一放入 `Action/FullBody/Runtime`。
- [x] 6.10 将 FullBody Action 逻辑配置资产编排到 `Assets/Configs/3C/Action/FullBody`，Dodge 运动和打断子配置编排到 `Action/FullBody/Dodge`。

## 7. 自动测试
- [x] 7.1 测试初始路径为 `/FullBody/Locomotion/Idle` 或等价路径。
- [x] 7.2 测试移动输入后路径进入 `/FullBody/Locomotion/MoveStart`。
- [x] 7.3 测试持续移动后路径进入 `/FullBody/Locomotion/MoveLoop`。
- [x] 7.4 测试停止输入后路径进入 `/FullBody/Locomotion/MoveStop`。
- [x] 7.5 测试 Dodge accepted 后路径进入 `/FullBody/Action/Dodge`。
- [x] 7.6 测试 Dodge active 时 Locomotion 不提交位移和 base layer 动画。
- [x] 7.7 测试 Dodge completed 后路径回到 Locomotion 子树。
- [x] 7.8 测试 `FullBodyStateSnapshot` 中 owner、path、phase、action state 一致。
- [x] 7.9 测试相同输入序列产生稳定 owner/path 序列。
- [x] 7.10 静态测试 FullBody HFSM 新增源码不引用 `BBBNexus`。
- [x] 7.11 静态测试 FullBody HFSM 新增源码不直接调用 `CharacterController.Move`。
- [x] 7.12 静态测试 FullBody HFSM 新增源码不直接调用 Animancer 播放 API。
- [x] 7.13 测试可琳 Prefab 不再引用旧 `PlayerDodgeActionController` 脚本 guid。
- [x] 7.14 测试未显式配置 Action executor 时，`PlayerFullBodyActionController` 复用 Locomotion motion executor 提交 Dodge 位移。
- [x] 7.15 测试 FullBody Action 逻辑配置资产不再直接散落在 `Assets/Configs/3C/Action` 根目录。

## 8. 文档和手动验证
- [x] 8.1 更新 `docs/agents/character-animation-state-roadmap.md`，记录 FullBody HFSM 状态树口径。
- [x] 8.2 记录后续 Roll/Jump/Attack 必须从 `FullBody/Action/*` 扩展，不新增 per-action controller。
- [ ] 8.3 Play Mode 手动验证普通 WASD 状态路径随 Idle/MoveStart/MoveLoop/MoveStop 变化。
- [ ] 8.4 Play Mode 手动验证按 Shift 时状态路径显示 Action.Dodge。
- [ ] 8.5 Play Mode 手动验证 Dodge active 时没有基础移动叠加位移或 base layer 动画。
- [ ] 8.6 Play Mode 手动验证 Dodge 结束后继续方向输入可回到 Locomotion 并保持现有 Run latch 语义。

## 9. 验证记录
- [x] 9.1 运行 `openspec validate add-fullbody-hfsm-state-tree --strict --no-interactive`：passed。
- [ ] 9.2 在 Unity Test Runner 运行定向 EditMode 测试并记录结果：`ThirdPersonAction.Tests.FullBodyActionFrameworkTests` 和 `ThirdPersonAction.Tests.DodgeActionProfileTests`。
- [x] 9.3 记录静态边界检查结果：新增 FullBody HFSM 源码无 `BBBNexus`、未直接调用 `CharacterController.Move`、未直接调用 Animancer/Animator 播放 API、未直接读取 Input System 或 Cinemachine。
- [ ] 9.4 记录用户 Play Mode 手动验证结果。
- [x] 9.5 运行非 batchmode C# 编译验证：`dotnet build .\Assembly-CSharp-Editor.csproj --no-restore -v:minimal` passed，剩余 6 个既有参考代码 warning。
