# TurnBack Root Motion 排查记录

## 2026-06-13 初始事实

### 当前假设

1. TurnBack 动画资源本身有 180 度 RootQ 和平面 RootT，问题不在动画曲线缺失。
2. Sandbox 实际没有转满或没有位移，主要嫌疑是 runtime 没拿到或没应用 `Animator.deltaPosition` / `Animator.deltaRotation`。
3. 同一个 `Animator` 上的 `BasicLocomotionAnimancerPresenter` 和 `ActionAnimationAnimancerPresenter` 都会写 `Animator.applyRootMotion`，存在互相覆盖风险。
4. TurnBack 第一帧可能因为 `OnAnimatorMove` 与 simulation tick 时序错位得到 0 delta，但仍必须保持输入旋转和输入平面位移 suppress。

### 查证方法

1. 静态读取 Sandbox、prefab、Animancer TransitionLibrary 和 TransitionAsset 引用链。
2. 解析 TurnBack `.anim` 中 `RootT.*` 和 `RootQ.*` 曲线。
3. 阅读 root motion 读取、消费、命令构建和执行链路。
4. 增加测试覆盖 root motion 开关仲裁与 TurnBack suppress 行为。

### 观察结果

1. Sandbox 使用 `Assets/Prefabs/Character/可琳.prefab`。
2. 该 prefab 上的 `AnimancerComponent` 使用 Generic `Assets/Configs/3C/Animacer/Corin/Generic/Corin_TransitionLib.asset`。
3. `Locomotion.Turn.Back` 指向 `Assets/Configs/3C/Animacer/Corin/Generic/TransitionAsset/Corin_TurnBack.asset`。
4. `Corin_TurnBack.asset` 的 clip 为 `Assets/Art/Animation/MyDemoNeed/Corin/WithWeaponRootmotion/Corin_TurnBack_WithWeaponRootmotion.anim`。
5. `Corin_TurnBack_WithWeaponRootmotion.anim` 的 `RootQ` yaw 为 `0 -> 180`，净转身 `180` 度。
6. `Corin_TurnBack_WithWeaponRootmotion.anim` 的 `RootT` 平面净位移约 `3.0352`。
7. Unity API 读取该 clip：`hasRootCurves=True`，`averageSpeed=(-1.63, 0.00, 3.83)`，`averageAngularSpeed=2.655`。
8. `BasicLocomotionAnimancerPresenter.OnAnimatorMove()` 只在 `currentPhase == TurnBack` 时采集 `deltaPosition` / `deltaRotation`。
9. `BasicLocomotionAnimancerPresenter.Present()` 在 TurnBack 时会尝试将 `Animator.applyRootMotion` 设为 `true`。
10. `ActionAnimationAnimancerPresenter.ApplyRootMotionPolicy()` 会在动作播放路径将同一个 `Animator.applyRootMotion` 设为 `false`。
11. `PlayerLocomotionController.ResolveTurnBackRootMotionFacts()` 即使 delta 为 0，也会对 TurnBack 输出 `SuppressInputRotation=true` 和 `SuppressInputPlanarMovement=true`。

### 修改内容

1. 新增 `AnimatorRootMotionController`，统一写入同一个 `Animator.applyRootMotion`。
2. `BasicLocomotionAnimancerPresenter` 不再直接写 `Animator.applyRootMotion`，改为向 `AnimatorRootMotionController` 提交 Locomotion 请求。
3. TurnBack 期间 Locomotion 请求强制打开 root motion，优先级高于 Action 的禁用请求。
4. 非 TurnBack 的 locomotion 默认仍保持 root motion 关闭，避免普通移动被动画 root motion 影响。
5. `ActionAnimationAnimancerPresenter` 不再直接写 `Animator.applyRootMotion`，改为提交 Action 禁用请求。
6. 保留并新增诊断日志：
   - `animator-root-motion-policy`：观察 root motion 仲裁结果。
   - `locomotion-root-motion-delta`：观察 `OnAnimatorMove()` 是否收到非零 delta。
   - `turnback-root-motion-consumed`：观察 `PlayerLocomotionController` 是否消费到 root motion delta，并确认 suppress 标志。

### 测试结果

1. `Tests.Editor.UnifiedCharacterStateMachineTests.CorinTurnBackTransitionLibrariesUseRootMotionClips` 通过。
2. `Tests.Editor.UnifiedCharacterStateMachineTests.PlayerLocomotionTurnBackSuppressesInputAndConsumesRootMotion` 通过。
3. `Tests.Editor.UnifiedCharacterStateMachineTests.TurnBackLocomotionRootMotionOverridesActionRootMotionDisable` 通过。
4. `Tests.Editor.UnifiedCharacterStateMachineTests.NonTurnBackLocomotionKeepsAnimatorRootMotionDisabledByDefault` 通过。
5. `Tests.Editor.UnifiedCharacterStateMachineTests.ActionAnimationClearsStaleTurnBackRootMotionForce` 通过。
6. 已新增 `Tests.Editor.UnifiedCharacterStateMachineTests.PlayerLocomotionTurnBackSuppressesInputWhenRootMotionDeltaIsEmpty`，用于覆盖 TurnBack 首帧/空 delta 时仍 suppress 输入旋转和输入平面位移；等待 Unity 会话恢复后执行。
7. `Tests.Editor.UnifiedCharacterStateMachineTests` 所在组共 `55/55` 通过；新增保护测试使用 `Assembly-CSharp-Editor` + 方法名单独执行，`1/1` 通过。
8. `openspec validate add-moving-pivot-turn --strict --no-interactive` 通过。
9. Unity Console error 数为 `0`。

### 下一步判断

1. 这轮修复已经覆盖“Action presenter 覆盖 TurnBack root motion 开关”的强嫌疑。
2. 如果 Sandbox 仍无位移或无旋转，下一步看 `locomotion-root-motion-delta`：
   - 如果没有出现：`OnAnimatorMove()` 没被调用，继续查 Animator/Animancer update 和 root motion 回调条件。
   - 如果出现但 `hasDelta=False`：Unity 没从该 clip 输出 delta，继续查 clip import/Avatar/controller override。
   - 如果出现且 delta 非零，但 `animation-motion-executor` 仍为 0：继续查 `ResolveTurnBackRootMotionFacts()` 的消费时序。
   - 如果 executor 非零但角色没动：继续查 `CharacterController.Move()`、碰撞、root/visual transform 关系。

## 2026-06-13 续查：残留 delta 与 root motion 仲裁边界

### 当前假设

1. TurnBack 第一帧没有 root motion delta 是正常时序：状态机先构建/执行 `MovementCommand`，随后 `Present` TurnBack 动画，`OnAnimatorMove()` 产生的 delta 会在后续 tick 被消费。
2. 如果 TurnBack 结束或切出时最后一帧 `OnAnimatorMove()` 产生的 delta 没被消费，它会残留到下一次 TurnBack，造成下一次转身起步瞬间偏移或突转。
3. `AnimatorRootMotionController.SetActionRootMotionDisabled(true)` 如果发现 action 禁用请求本来就是 true 会 early return；这会导致 Action 进入时清不掉之前 TurnBack 留下的 `locomotionForceRequested`。
4. `Corin_TurnBack_WithWeaponRootmotion.anim` 的 yaw 不是整段 1.183 秒匀速转满，而是在约 0.416 秒到达 180 度，后半段基本保持 180 度；因此“动画太长”的手感可能来自后段仍在 TurnBack 状态但 root yaw 已经停止变化。

### 查证方法

1. 静态读取 `BasicLocomotionAnimancerPresenter`、`AnimatorRootMotionController`、`PlayerLocomotionController` 和 `CharacterControllerBasicMotionExecutor`。
2. 静态读取 `DefaultCharacterStateMachine.asset`、`Corin_TurnBack.asset`、`DefaultRunLocomotionAnimationConfig.asset` 和真实 TurnBack clip。
3. 用 `openspec validate add-moving-pivot-turn --strict --no-interactive` 验证 OpenSpec。
4. 用 `git diff --check` 检查本次相关文件格式。
5. 尝试通过 Unity MCP 读取 Console 和跑测试。

### 观察结果

1. `CharacterVisualRoot` 上同时挂有 `Animator`、`AnimancerComponent`、`BasicLocomotionAnimancerPresenter` 和 `ActionAnimationAnimancerPresenter`，`OnAnimatorMove()` 脚本没有挂错对象。
2. `Locomotion.Turn.Back` 的 TransitionAsset fade 为 `0.08`，clip 为 `Corin_TurnBack_WithWeaponRootmotion`。
3. TurnBack 状态机退出条件是 `LocomotionAnimationCanExit`，要求 blackboard 中 `Locomotion.Turn.Back` progress ended，不是固定短秒数。
4. `DefaultRunLocomotionAnimationConfig.asset` 中 TurnBack `exitPolicy=2`，继续指向动画结束策略。
5. `Corin_TurnBack_WithWeaponRootmotion.anim` 静态长度为 `1.1833334` 秒，`m_LoopTime=0`。
6. 该 clip 的 Root/Editor 曲线显示 yaw 在约 `0.4166667` 秒到达 `180`，之后到 `1.1666667` 秒保持 `180`。
7. Unity 进程存在，但 MCP `mcpforunity://instances` 返回 `instance_count=0`，`read_console` 返回 `Unity session not available`，所以本轮暂时不能用 Test Runner/Console 做运行验证。
8. `Editor.log` 显示本轮脚本编译成功，`Assembly-CSharp.dll` / `Assembly-CSharp-Editor.dll` 已生成；未看到 C# error，只看到 KCC sample 的 obsolete warning、Animancer 更新提示，以及 InputSystem remoting 断连日志。

### 修改内容

1. `BasicLocomotionAnimancerPresenter.OnAnimatorMove()` 在非 TurnBack phase 收到 Animator move 回调时清空 `pendingRootMotionDelta`。
2. `BasicLocomotionAnimancerPresenter.Present()` 在非 TurnBack animation context 时清空 `pendingRootMotionDelta`。
3. `BasicLocomotionAnimancerPresenter.ClearPlaybackProgress()` 清空 `pendingRootMotionDelta`。
4. `AnimatorRootMotionController.SetActionRootMotionDisabled(true)` 即使 action disabled 状态已经是 true，也会在存在 stale `locomotionForceRequested` 时继续清掉 locomotion force 并重新应用策略。
5. 新增测试 `NonTurnBackLocomotionClearsStaleTurnBackRootMotionDelta`，覆盖非 TurnBack presentation 清除残留 root motion delta。
6. 新增测试 `ActionAnimationClearsStaleTurnBackRootMotionForceAfterPolicyWasAlreadyDisabled`，覆盖 Action 禁用请求重复时仍清除 stale TurnBack root motion force。
7. 新增测试 `CharacterControllerExecutorAppliesTurnBackAnimationMotionWithoutInputMotion`，覆盖真实 `CharacterControllerBasicMotionExecutor` 在 TurnBack suppress 下只应用 animation yaw/local delta，不叠加输入旋转和输入平面位移。

### 测试结果

1. `openspec validate add-moving-pivot-turn --strict --no-interactive` 通过。
2. `git diff --check` 通过；仅有 LF/CRLF 提示。
3. `Editor.log` 静态检查显示脚本编译成功，无 C# error；当前尾部有 InputSystem remoting 断连日志，需在 Unity Console 中区分它与本次 TurnBack 代码无关。
4. Unity MCP 当前没有可用 Unity session；`read_console` 和 `run_tests` 都返回 `Unity session not available`，尚未运行新增 Unity EditMode 测试，也尚未重新读取 Unity Console。
5. `git diff --check` 覆盖新增 executor 测试文件，通过；仅有 LF/CRLF 提示。

### 下一步判断

1. Unity MCP 恢复后必须先读取 Console error，再运行新增的三条测试和 TurnBack 相关测试集合。
2. 如果 Sandbox 日志显示 `locomotion-root-motion-delta` 在 TurnBack 后续帧有 delta，但视觉仍像瞬切，需要重点看 `animationNormalized`、`animationEnded`、`turnback-root-motion-consumed` 和 `animation-motion-executor` 是否显示 0.42 秒后 yaw 已经停止、但状态仍等到 1.18 秒才退出。
3. 如果用户觉得后半段拖沓，可以考虑后续把 TurnBack 的可退出窗口改成动画事件/normalized threshold，但这属于手感策略调整，不能在未确认日志前直接改。

## 2026-06-13 续查：executor 级输入抑制与 prefab 根节点

### 当前假设

1. 即使 `MovementCommand` 已经带了 suppress 标志，也需要确认真实 `CharacterControllerBasicMotionExecutor` 没有继续叠加输入旋转或输入平面位移。
2. 如果 prefab 的 `CharacterMotionDriver.rotationRoot` 指错到视觉子节点，root motion yaw/位移可能和 presentation 插值对象不一致，造成 Sandbox 里看起来偏移或瞬切。

### 查证方法

1. 静态读取 `CharacterControllerBasicMotionExecutor` 的输入旋转、animation yaw、输入位移和 animation delta 应用顺序。
2. 静态读取 `Assets/Prefabs/Character/可琳.prefab` 中 `CharacterMotionDriver`、`CharacterController`、`PlayerLocomotionController` 和 `CharacterVisualRoot` 的引用关系。
3. 新增 executor 级测试，直接构造 TurnBack command：输入方向向右，animation local delta 向前，animation yaw 为 90 度，且 suppress 输入旋转/位移。

### 观察结果

1. `CharacterControllerBasicMotionExecutor` 在 `SuppressInputRotation=true` 时不会调用输入 `RotateTowards`，只会应用 `AnimationYawDelta`。
2. `CharacterControllerBasicMotionExecutor` 在 `SuppressInputPlanarMovement=true` 时输入 planar velocity 为 0，但仍会应用 `AnimationLocalPlanarDelta` 转换后的世界位移。
3. `可琳.prefab` 的 `CharacterMotionDriver.rotationRoot` 指向角色根 Transform，`CharacterVisualRoot` 是角色根子节点；root motion 通过 executor 应用到角色根，符合统一运动出口。
4. 静态搜索未发现 `TurnInPlace` / `MovingPivotTurn` 运行链路仍存在于角色 runtime；`MotionProfile` 仍存在于普通 RunStart/RunEnd/Walk 等配置和工具中，但 TurnBack 分支先走 `ResolveTurnBackRootMotionFacts()`，不采样 baked profile。

### 修改内容

1. 新增 `CharacterControllerExecutorAppliesTurnBackAnimationMotionWithoutInputMotion`。
2. 该测试覆盖真实 executor：输入方向/速度存在时，TurnBack suppress 下最终位置只来自 animation local delta，最终 yaw 只来自 animation yaw。

### 测试结果

1. `git diff --check` 通过；仅有 LF/CRLF 提示。
2. `openspec validate add-moving-pivot-turn --strict --no-interactive` 通过。
3. `Editor.log` 仍显示脚本编译成功，无 C# error。
4. Unity MCP `instances` 仍为 0；`read_console` 和 `run_tests` 仍返回 `Unity session not available`。新增 executor 测试尚未由 Unity Test Runner 运行。

### 下一步判断

1. 需要在 Unity Test Runner 中运行：
   - `Tests.Editor.UnifiedCharacterStateMachineTests.CharacterControllerExecutorAppliesTurnBackAnimationMotionWithoutInputMotion`
   - `Tests.Editor.UnifiedCharacterStateMachineTests.NonTurnBackLocomotionClearsStaleTurnBackRootMotionDelta`
   - `Tests.Editor.UnifiedCharacterStateMachineTests.ActionAnimationClearsStaleTurnBackRootMotionForceAfterPolicyWasAlreadyDisabled`
   - `Tests.Editor.UnifiedCharacterStateMachineTests.PlayerLocomotionTurnBackSuppressesInputWhenRootMotionDeltaIsEmpty`
   - `Tests.Editor.UnifiedCharacterStateMachineTests.PlayerLocomotionTurnBackSuppressesInputAndConsumesRootMotion`
   - `Tests.Editor.UnifiedCharacterStateMachineTests.CorinTurnBackTransitionLibrariesUseRootMotionClips`
2. Sandbox 手测如果仍怪，优先复制上述五类日志；不要先调角度或时间。

## Sandbox 手测步骤

1. 打开 `Assets/Scenes/Sandbox.unity`。
2. 运行场景，按住向前移动进入跑/走循环。
3. 立刻按反方向输入触发 TurnBack。
4. 观察 Console 中这些日志：
   - `locomotion-animation-played`：应出现 `phase=TurnBack`、`alias=Locomotion.Turn.Back`、`nextAnimation=Corin_TurnBack_WithWeaponRootmotion`。
   - `animator-root-motion-policy`：TurnBack 帧应出现 `locomotionForceRequested=True`、`applied=True`。
   - `locomotion-root-motion-delta`：TurnBack 后续帧应出现 `applyRootMotion=True`，并逐帧看到 `hasDelta=True`、`worldDelta` 或 `yawDelta` 非零。
   - `turnback-root-motion-consumed`：每个 TurnBack motion build 应出现 `hasSource=True`，第一帧允许 `hasDelta=False`，但必须始终显示 `suppressInputRotation=True`、`suppressInputPlanarMovement=True`。
   - `animation-motion-executor`：TurnBack 期间应出现 `suppressFlag=True`、`suppressInputPlanarMovement=True`、`appliedInputRotation=False`、`appliedInputPlanarMovement=False`。
   - 后续 TurnBack 帧应看到 `commandAnimationYawDelta` 非 0 或 `animationWorldDelta` 非 0，并且 `animationDeltaSpace=World`。
5. 预期表现：TurnBack 期间角色由动画 root motion 旋转约 180 度并产生动画位移，动画结束后恢复普通移动控制。

## 2026-06-13 续查：TurnBack 触发角度来源

### 当前假设

1. 用户反馈前后移动触发不了，且指出 TurnBack 不应该靠相机历史方向，而应该看人物当前朝向与当前输入方向的夹角。
2. 旧触发逻辑用 runtime blackboard 里的上一帧 `Locomotion.WorldDirection` 对比当前 `WorldMoveDirection`，这会把“上一帧 camera-relative 移动方向”误当成人物朝向。
3. 这会导致角色已经被 root motion 或输入旋转改变朝向后，TurnBack 触发仍受上一帧输入方向影响，前后切换容易漏触发或误触发。

### 修改内容

1. `CharacterStateMachineContext` 新增 `FacingForward`。
2. `PlayerLocomotionController` 复用已有 `IFacingDirectionProvider` / `TransformFacingDirectionProvider`，构建 state machine context 时传入人物当前 forward。
3. `CharacterStateTransitionEvaluator.IsMoveTurnBackRequested()` 改为比较 `FacingForward` 与当前 `WorldMoveDirection` 的夹角，不再读取上一帧 locomotion world direction。
4. 新增/调整测试，锁定 TurnBack 触发只由人物朝向与当前输入方向决定。

### 测试结果

1. `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal` 通过，0 error。
2. `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` 通过，0 error。
3. `openspec validate add-moving-pivot-turn --strict --no-interactive` 通过。
4. Unity Test Runner 未运行，原因是本轮没有可用 Unity 会话；Sandbox 手测仍需用户在编辑器中验证。

## 2026-06-13 续查：TurnBack 条件探针

### 当前假设

1. 用户多次尝试只有最后一次触发，现有日志只覆盖成功后的 `TurnBack` root motion，没有覆盖失败时条件为什么没通过。
2. 需要知道每次 `MoveLoop -> TurnBack` 条件评估时的 current state、输入方向、人物朝向、夹角、阈值和 pass/fail。

### 修改内容

1. 在 `CharacterStateMachineRunner` 的 transition 条件评估循环中，对 `MoveTurnBackRequested` 增加 `locomotion-turnback-condition` 日志。
2. 日志包含：`from`、`to`、`hasMove`、`worldMove`、`facing`、`angle`、`threshold`、`passed`、`stateTime`、`phaseCanExit`、blackboard locomotion 摘要。
3. 行为逻辑不变，只增加诊断输出。

### 测试结果

1. `dotnet restore Assembly-CSharp.csproj -v:minimal` 通过，用于恢复 StylizedGrass csproj 缺失的 `project.assets.json`。
2. `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal` 通过，0 error。
3. `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` 通过，0 error。

## 2026-06-13 续查：Animator.deltaPosition 空间语义

### 当前假设

1. `Animator.deltaPosition` 代表 Animator 上一帧求值产生的 root motion 位移，参考工程直接把它用于 `CharacterController.Move()`，不先转成本地空间。
2. 当前实现先在 `BasicLocomotionAnimancerPresenter.OnAnimatorMove()` 中用 `transform.InverseTransformDirection(animator.deltaPosition)` 转成本地，再在 `CharacterControllerBasicMotionExecutor` 中用角色根 `TransformDirection()` 转回世界。
3. 如果 Animator/visual root 与角色运动根存在层级或朝向差异，这个“世界 -> visual local -> character root world”的二次空间转换会让 TurnBack 位移方向偏掉。
4. 该问题可以解释“动画资源确实有 RootT/RootQ，但 Sandbox 中位移和转身体感仍不对”。

### 查证方法

1. 对照参考工程 `CharacterMoveControllerBase.OnAnimatorMove()`：它调用 `ApplyBuiltinRootMotion()` 后直接使用 `characterAnimator.deltaPosition` 推 `CharacterController`。
2. 查 Unity 文档 `Animator.deltaPosition` / `OnAnimatorMove` 示例：手动 root motion 示例直接将 `animator.deltaPosition` 加到 transform 或刚体速度。
3. 静态读取 `可琳.prefab`：`CharacterVisualRoot` 是角色根的子节点，`CharacterMotionDriver.rotationRoot` 指向角色根。
4. 增加 executor 级测试，模拟角色根已有 90 度朝向时，TurnBack 的世界 root motion delta 不应再被根朝向旋转一次。

### 观察结果

1. 之前 `locomotion-root-motion-delta` 记录的是 `localDelta`，它来自 `transform.InverseTransformDirection(animator.deltaPosition)`。
2. executor 又通过 `root.TransformDirection(localDelta)` 得出 `animationWorldDelta`。
3. 这对 baked profile 的本地位移是合理的，但对真实 `Animator.deltaPosition` root motion 不合理。
4. `CharacterVisualRoot` 本地旋转当前为单位四元数，但该实现仍把 root motion 语义绑定到了 presentation transform，而不是 Animator delta 本身；后续换装/重定向/视觉根修正时容易再次错。

### 修改内容

1. 新增 `BasicMovementPlanarDeltaSpace`，区分 animation delta 是 `Local` 还是 `World`。
2. `BasicMovementMotionFacts` 增加 `PlanarDeltaSpace`，默认 `Local`，保持普通 baked motion profile 不变。
3. `MovementCommand` 增加 `AnimationPlanarDeltaSpace`，将空间语义传给 executor。
4. `BasicLocomotionAnimancerPresenter.OnAnimatorMove()` 不再把 `animator.deltaPosition` 转成本地，而是直接记录 `worldDelta`。
5. `LocomotionRootMotionDelta` 将字段改为 `WorldPlanarDelta`，避免继续误读为本地 motion profile delta。
6. `PlayerLocomotionController.ResolveTurnBackRootMotionFacts()` 将 TurnBack root motion facts 标记为 `BasicMovementPlanarDeltaSpace.World`。
7. `CharacterControllerBasicMotionExecutor.ResolveAnimationWorldDelta()` 在 delta space 为 `World` 时直接使用 delta；只有 `Local` 时才用运动根转换。
8. `animation-motion-executor` 日志新增 `animationDeltaSpace`，`locomotion-root-motion-delta` / `turnback-root-motion-consumed` 日志改为输出 `worldDelta`。

### 测试结果

1. `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal` 通过，0 error。
2. `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` 通过，0 error；仅有项目既有 warning。
3. `git diff --check` 覆盖本轮相关文件，通过；仅有 LF/CRLF 提示。
4. `openspec validate add-moving-pivot-turn --strict --no-interactive` 通过。
5. 新增 `CharacterControllerExecutorDoesNotRotateWorldRootMotionDeltaTwice`，但 Unity MCP 当前仍返回 `Unity session not available`，尚未能用 Unity Test Runner 执行。

### 下一步判断

1. Unity MCP 或编辑器可用后，优先跑 `Tests.Editor.UnifiedCharacterStateMachineTests.CharacterControllerExecutorDoesNotRotateWorldRootMotionDeltaTwice` 和 TurnBack 相关测试集合。
2. Sandbox 手测时重点看：
   - `locomotion-root-motion-delta` 是否输出 `worldDelta` 非零。
   - `turnback-root-motion-consumed` 是否输出同方向 `worldDelta`。
   - `animation-motion-executor` 是否显示 `animationDeltaSpace=World`，且 `animationWorldDelta` 不再随角色当前 yaw 被二次旋转。
3. 如果这轮后 TurnBack 仍然像“转完后拖太久”，下一步再看 `animationNormalized` / `animationEnded`，判断是否需要按可退出窗口调整 TurnBack 结束时机，而不是继续修 root motion 位移。

## 2026-06-13 续查：World/Local delta 双路径保护

### 当前假设

1. TurnBack 应该把真实 `Animator.deltaPosition` 当世界 root motion delta 应用。
2. 普通 baked motion profile 仍然是本地空间 delta，不能因为 TurnBack 修复被改成世界空间。
3. 如果没有同时覆盖 World 和 Local 两条路径，后续很容易再次把两种动画运动语义混在一起。

### 查证方法

1. 对 `CharacterControllerBasicMotionExecutor` 增加 World root motion 回归测试：角色根已朝向 90 度时，TurnBack world delta `(0,0,1)` 仍应移动到世界 Z。
2. 对 `CharacterControllerBasicMotionExecutor` 增加 Local baked motion 保护测试：角色根已朝向 90 度时，Local delta `(0,0,1)` 应被转换到世界 X。
3. 用 `.csproj` 编译确认新增 enum/字段/测试没有 C# 编译错误。
4. 继续尝试 Unity MCP Test Runner，确认是否能执行新增测试。

### 观察结果

1. `CharacterControllerExecutorDoesNotRotateWorldRootMotionDeltaTwice` 覆盖 TurnBack World delta 不被二次旋转。
2. 新增 `CharacterControllerExecutorStillRotatesLocalBakedMotionDelta` 覆盖普通 Local baked delta 仍按角色根朝向转换。
3. `AnimationPlanarDeltaSpace` 当前只在 TurnBack root motion facts 中设置为 `World`；baked profile 路径保持默认 `Local`。
4. Unity MCP 仍然能返回 telemetry，但 `read_console`、`manage_scene`、`run_tests` 继续返回 `Unity session not available`。

### 修改内容

1. 在 `UnifiedCharacterStateMachineTests` 增加 `CharacterControllerExecutorStillRotatesLocalBakedMotionDelta`。
2. 没有改状态机、Animancer 配置、动画资源或 Sandbox 场景。

### 测试结果

1. `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` 通过，0 error；仅有既有 `RootMotionExtractorWindow._logEveryNFrames` warning。
2. `git diff --check` 覆盖相关文件，通过；仅有 LF/CRLF 提示。
3. `openspec validate add-moving-pivot-turn --strict --no-interactive` 上一轮通过，本轮未改 OpenSpec 内容。
4. Unity Test Runner 仍未能执行：`run_tests` 返回 `Unity session not available`。

### 下一步判断

1. Unity MCP 恢复后，必须先跑：
   - `Tests.Editor.UnifiedCharacterStateMachineTests.CharacterControllerExecutorDoesNotRotateWorldRootMotionDeltaTwice`
   - `Tests.Editor.UnifiedCharacterStateMachineTests.CharacterControllerExecutorStillRotatesLocalBakedMotionDelta`
   - `Tests.Editor.UnifiedCharacterStateMachineTests.PlayerLocomotionTurnBackSuppressesInputAndConsumesRootMotion`
   - `Tests.Editor.UnifiedCharacterStateMachineTests.TurnBackLocomotionRootMotionOverridesActionRootMotionDisable`
2. Sandbox 手测如果 `worldDelta`、`animationDeltaSpace=World`、`appliedInputRotation=False`、`appliedInputPlanarMovement=False` 都正确但体感仍拖，下一步才进入 TurnBack 退出窗口/动画归一化时机排查。

## 2026-06-13 续查：Sandbox tick 驱动路径

### 当前假设

1. 如果 `PlayerLocomotionController` 同时被自身 `AutoUpdate`、`LocomotionTickAdapter` 和 `FullBodyActionTickAdapter` 驱动，TurnBack 会在同一帧被多次 evaluate/execute/present，表现会非常像 root motion 或转身动画错误。
2. 如果只有 `FullBodyActionTickAdapter` 驱动，则当前统一链路是 `FullBodyActionTickAdapter -> PlayerFullBodyActionController -> PlayerLocomotionController`，不应再单独启用 `LocomotionTickAdapter`。

### 查证方法

1. 静态读取 `Assets/Prefabs/Character/可琳.prefab` 的 `PlayerLocomotionController`、`PlayerFullBodyActionController`、`LocomotionTickAdapter` 序列化状态。
2. 静态读取 `Assets/Scenes/Sandbox.unity` 对可琳 prefab 的 override。
3. 阅读 `LocomotionTickAdapter`、`FullBodyActionTickAdapter` 和 `SimulationTickRunner` 注册/执行逻辑。

### 观察结果

1. `可琳.prefab` 中 `PlayerLocomotionController.autoUpdate=0`。
2. `可琳.prefab` 当时的 `LocomotionTickAdapter.m_Enabled=0`；截至 2026-06-14，该组件已从正式角色 prefab 移除，因此 prefab 默认不会通过单独 locomotion adapter tick。
3. `可琳.prefab` 中 `PlayerFullBodyActionController.autoUpdate=1`，但 Sandbox 里存在启用的 `FullBodyActionTickAdapter`，该 adapter `Register()` 时会把 `fullBodyActionController.AutoUpdate=false`。
4. Sandbox 中 `FullBodyActionTickAdapter` 引用同一个 `UnitySimulationTickDriver` 和 `PlayerFullBodyActionController`。
5. 目前静态证据不支持“双 tick 同时驱动 locomotion”作为首要原因。

### 修改内容

1. 未修改 tick 驱动代码或场景配置。
2. 仅记录排查结论，避免误把 TurnBack 表现问题归因到双 tick。

### 测试结果

1. 本轮未新增代码。
2. Unity MCP `read_console`、`manage_scene` 仍返回 `Unity session not available`，无法从运行时确认 adapter 注册顺序。

### 下一步判断

1. Sandbox 运行时如果日志每个 simulation step 只出现一次 `fullbody-tick-snapshot` 和一次 `animation-motion-executor`，则可继续排除双 tick。
2. 如果同一 step 出现两次 `animation-motion-executor` 或两次 `locomotion-phase-changed`，再回头检查 adapter 注册顺序和是否有隐藏启用的 `LocomotionTickAdapter`。

## 2026-06-13 续查：Root Motion delta 被覆盖而不是累加

### 当前假设

1. `OnAnimatorMove()` 跟当前 simulation tick 不一定一一对应；Animator 按渲染帧输出 `deltaPosition/deltaRotation`，`UnitySimulationTickDriver` 用 60Hz accumulator 在 `Update()` 中跑 tick。
2. 旧实现每次 `OnAnimatorMove()` 都用当前帧 delta 覆盖 `pendingRootMotionDelta`，如果两次 Animator move 之间没有 locomotion tick 消费，就会丢掉前一段 root motion。
3. 这会直接导致“动画资源确实转了 180 度并有位移，但角色实际只吃到一部分旋转/位移”，表现为没转满、位移不足或后段瞬切。

### 查证方法

1. 阅读 `UnitySimulationTickDriver`：默认 tick rate 为 60Hz，tick 在 `Update()` 中 accumulator 推进。
2. 对照 Unity/Animancer root motion 示例：`OnAnimatorMove()` 中读取 `Animator.deltaPosition/deltaRotation` 并手动转发是正确用法。
3. 检查 `BasicLocomotionAnimancerPresenter.OnAnimatorMove()`：旧代码只保存最后一次 delta，没有累计。
4. 增加纯数据测试，验证多次 root motion delta 在消费前能累加。

### 观察结果

1. `OnAnimatorMove()` 旧逻辑每次执行都会直接写 `pendingRootMotionDelta = new LocomotionRootMotionDelta(...)`。
2. `ConsumeRootMotionDelta()` 只有在 locomotion build TurnBack motion facts 时才读取并清空 pending delta。
3. 因为两个阶段不同步，pending 单槽覆盖会丢失未消费的 TurnBack root motion。
4. Unity MCP server 存在，但 `read_console` / `run_tests` 仍返回 `Unity session not available`，本轮无法用 Test Runner 验证运行时。

### 修改内容

1. `LocomotionRootMotionDelta` 新增 `Accumulate()`，累加世界平面位移和 yaw delta，并保持 y 轴位移为 0。
2. `BasicLocomotionAnimancerPresenter.OnAnimatorMove()` 改为把当前 frame delta 累加到 `pendingRootMotionDelta`，直到 `ConsumeRootMotionDelta()` 清空。
3. `locomotion-root-motion-delta` 日志增加 `pendingWorldDelta` 和 `pendingYawDelta`，方便 Sandbox 里确认是否有多帧 delta 被合并。
4. `UnifiedCharacterStateMachineTests` 新增 `LocomotionRootMotionDeltaAccumulatesUntilConsumed`。

### 测试结果

1. `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal` 在 `3cDemo/Client/3C_Client` 通过，0 error；有既有 `KinematicCharacterControllerSamples` 和参考脚本 warning。
2. `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` 串行重跑通过，0 error；有既有 `RootMotionExtractorWindow._logEveryNFrames` warning。
3. 并行跑 runtime/editor build 时曾出现 `CS2012`，原因是 `VBCSCompiler` 锁住 `Temp/obj/Assembly-CSharp/Assembly-CSharp.dll`；串行重跑通过，判断为构建锁竞争，不是代码错误。
4. `openspec validate add-moving-pivot-turn --strict --no-interactive` 通过。
5. `git diff --check` 覆盖本轮 TurnBack 相关文件通过；仅有 LF/CRLF 提示。
6. Unity MCP `read_console` 和 `run_tests` 仍返回 `Unity session not available`；Unity Console 和 EditMode Test Runner 本轮未验证。

### 下一步判断

1. Sandbox 手测时先看 `locomotion-root-motion-delta` 的 `pendingWorldDelta` / `pendingYawDelta`：如果它们逐帧累积，然后 `turnback-root-motion-consumed` 一次消费相同量，说明 delta 覆盖问题已被修掉。
2. 如果 `pendingYawDelta` 累计接近 180，但 `animation-motion-executor` 的 `commandAnimationYawDelta` 明显更小，继续查 `ConsumeRootMotionDelta()` 的时序。
3. 如果 `pendingYawDelta` 本身始终很小或为 0，继续查 `Animator.applyRootMotion`、clip import root motion 和 Animancer layer 权重。
4. 如果累计和 executor 都正确但体感仍拖，下一步才调整 TurnBack 退出窗口/动画 normalized time，而不是再改 root motion 采样。

## 2026-06-13 续查：不要在运行时切换 Animator.applyRootMotion

### 当前假设

1. 当前 TurnBack root motion 链路已经通过 `OnAnimatorMove()` 手动读取 `Animator.deltaPosition` / `Animator.deltaRotation`，所以运行时反复写 `Animator.applyRootMotion` 不是必要条件。
2. Unity 官方文档说明：脚本实现 `OnAnimatorMove()` 时，`Animator.applyRootMotion` 对 root motion 应用没有效果；运行时修改 `applyRootMotion` 会重新初始化 Animator。
3. 因此旧的仲裁实现虽然解决了 Locomotion/Action 抢写同一个 bool 的问题，但仍然会在 TurnBack 进入/退出或 Action 切入时重写 `Animator.applyRootMotion`，这可能造成动画状态重置、瞬切、前几帧 root motion 丢失或表现不连续。
4. 如果 Action 打断 TurnBack，`BasicLocomotionAnimancerPresenter.currentPhase` 可能仍停在 TurnBack；此时 `OnAnimatorMove()` 不应继续把后续 Action 动画 delta 缓存成 TurnBack root motion。

### 查证方法

1. 读取 Unity 官方 `Animator.applyRootMotion` 文档，确认 `OnAnimatorMove()` 与运行时写 `applyRootMotion` 的语义。
2. 对照当前代码：`AnimatorRootMotionController.Apply()` 旧实现会写 `resolvedAnimator.applyRootMotion = next`。
3. 对照参考工程：它在 `OnAnimatorMove()` 中统一读取 Animator root motion 并由代码决定怎么推角色，而不是把移动转身当 TurnInPlace。
4. 串行运行 `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal` 与 `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`。
5. 尝试通过 Unity MCP 读取 Console error。

### 观察结果

1. Unity 官方文档确认：实现 `OnAnimatorMove()` 后 `applyRootMotion` 对应用 root motion 无效；运行时改变该值会 re-initialize Animator。
2. `BasicLocomotionAnimancerPresenter.OnAnimatorMove()` 已经手动读取 `animator.deltaPosition` / `animator.deltaRotation`，并通过 motion facts/executor 应用，不依赖 Unity 自动应用 root motion。
3. 旧 `AnimatorRootMotionController` 日志里的 `applied=True/False` 实际是写入 `Animator.applyRootMotion`，容易把“谁拥有手动 root motion 消费权”和“Unity 是否自动应用 root motion”混成一个开关。
4. Unity MCP 仍返回 `Unity session not available`，因此本轮不能用 Unity Console/Test Runner/Sandbox 直接验证运行时效果。

### 修改内容

1. `AnimatorRootMotionController` 重新承担运行时 `Animator.applyRootMotion` 评价开关：TurnBack 手动 root motion 活跃时写 true，Action 或普通 locomotion 时写 false。
2. `AnimatorRootMotionController` 同时仲裁 `ManualRootMotionActive`：TurnBack Locomotion force 时为 true，Action 禁用或普通 locomotion 时为 false。
3. `animator-root-motion-policy` 日志记录 `manualRootMotionActive`、`animatorApplyRootMotionBefore` 和 `animatorApplyRootMotionAfter`，用于确认 Animator 是否正在产出可被 `OnAnimatorMove()` 采集的 root motion delta。
4. `BasicLocomotionAnimancerPresenter.OnAnimatorMove()` 只有在 `currentPhase == TurnBack` 且 `ManualRootMotionActive == true` 时才累加 root motion delta；否则清空 pending delta 并记录 `locomotion-root-motion-skipped`。
5. 更新测试：TurnBack 必须把 `Animator.applyRootMotion` 打开，Action 和非 TurnBack locomotion 必须关闭它。
6. 新增/更新测试 `RootMotionPolicyEnablesAnimatorApplyRootMotionForManualTurnBack`，防止 TurnBack 再次出现动画播放但没有 root motion delta 的状态。

### 测试结果

1. `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal` 通过，0 error；仅有既有 KCC/参考脚本 warning。
2. 并行跑 runtime/editor build 时再次出现 `CS2012` 文件锁竞争；随后串行重跑 editor build 通过，判断不是代码错误。
3. `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` 串行通过，0 error；仅有既有 warning。
4. Unity MCP `read_console` 仍返回 `Unity session not available`，尚未读取 Unity Console。
5. Unity Test Runner 尚未执行新增/更新测试，不能宣称 EditMode 测试通过。

### 下一步判断

1. Sandbox 手测时，`animator-root-motion-policy` 应显示 `manualRootMotionActive=True`、`animatorApplyRootMotionAfter=True`，表示 TurnBack 已打开 Animator root motion 评价入口。
2. `locomotion-root-motion-delta` 应显示 `manualRootMotionActive=True`，且后续帧 `pendingYawDelta` / `pendingWorldDelta` 逐步累计。
3. 如果 `locomotion-root-motion-skipped` 在 TurnBack 期间连续出现且 `manualRootMotionActive=False`，继续查 presenter 调用顺序或 Action 是否误清了 TurnBack force。
4. 如果 `manualRootMotionActive=True` 但仍没有 `locomotion-root-motion-delta`，再回到 Animator/Animancer 是否触发 `OnAnimatorMove()` 和 clip import/layer 权重。
5. 如果 delta 正常但体感仍拖，再查 TurnBack 退出窗口，不再回到 baked profile/yaw 修补。

## 2026-06-13 续查：可琳 prefab 静态启用 Animator root motion

### 当前假设

1. `OnAnimatorMove()` 手动 root motion 链路需要 Animator 有稳定的 root motion 处理入口。
2. 可琳 prefab 的 `Animator.m_ApplyRootMotion` 原本为 `0`，而旧代码试图在 TurnBack 进入时运行时切到 true。
3. 当前 Generic 修正采用运行时显式策略：TurnBack 手动 root motion 活跃时写 `Animator.applyRootMotion=true`，Action 或普通 locomotion 时写 false，再由 `ManualRootMotionActive` 决定是否采集/消费 TurnBack delta。

### 查证方法

1. 静态读取 `Assets/Prefabs/Character/可琳.prefab` 中 `CharacterVisualRoot` 的 `Animator` 配置。
2. 确认 `m_ApplyRootMotion` 当前值。
3. 增加资产级测试，锁定可琳 prefab 的 Animator root motion 静态启用。
4. 串行运行 runtime/editor `.csproj` 编译。

### 观察结果

1. `Assets/Prefabs/Character/可琳.prefab` 中 `CharacterVisualRoot` 的 `Animator.m_ApplyRootMotion` 原本为 `0`。
2. 这会迫使旧 runtime 代码通过切 `Animator.applyRootMotion` 来打开 root motion，而这正好触发 Unity 文档所说的 Animator re-initialize 风险。
3. TurnBack 的实际位移/旋转仍不会交给 Unity 自动推 Transform，而是在 `OnAnimatorMove()` 读取 delta 后通过 `CharacterControllerBasicMotionExecutor` 手动应用。

### 修改内容

1. 将 `Assets/Prefabs/Character/可琳.prefab` 的 `CharacterVisualRoot` Animator `m_ApplyRootMotion` 静态改为 `1`。
2. 新增 `CorinPrefabAnimatorKeepsRootMotionEnabledForManualOnAnimatorMove`，验证可琳 prefab 的 Animator root motion 静态启用。
3. runtime 在 TurnBack 进入时显式写 `Animator.applyRootMotion=true`，确保 `OnAnimatorMove()` 能拿到 `deltaPosition` / `deltaRotation`；Action 或非 TurnBack locomotion 写 false，避免误采集其它动画 root motion。

### 测试结果

1. `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal` 通过，0 error。
2. `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` 通过，0 error；仅有既有 `RootMotionExtractorWindow._logEveryNFrames` warning。
3. Unity Test Runner 仍未运行，原因是 Unity MCP session 不可用。

### 下一步判断

1. Sandbox 手测时，如果 `locomotion-root-motion-delta` 开始稳定出现，说明之前确实被 Animator root motion 入口/运行时切换影响。
2. 如果依然没有 `locomotion-root-motion-delta`，继续查 `OnAnimatorMove()` 是否因为 Animancer layer/Animator Controller/脚本挂载对象没有触发。
3. 如果 delta 出现但 pending 累计仍不足 180，继续查 blend 权重和 clip import。

## 2026-06-13 续查：统一 Locomotion 决策管线

### 当前假设

1. TurnBack 难触发的根因不是 root motion 本身，而是触发事实生成时序太晚：旧条件在状态机 evaluator 内即时比较 `FacingForward` 与 `WorldMoveDirection`，容易被普通旋转、空输入帧和当前 phase 打断。
2. TurnBack 应先在 Locomotion 主链中作为纯数据事实派生，再进入统一状态机 context；状态机 transition 只消费这个事实，不重新解析空间关系。
3. W/S 切换可能存在一帧空输入，需要短 step 窗口保留已经捕获的 TurnBack intent。

### 修改内容

1. 新增 `LocomotionSpatialFacts`、`LocomotionDecisionFacts` 和 `LocomotionTurnBackIntent`。
2. `PlayerLocomotionController.TryEvaluateWithStateMachine()` 拆出 `ResolveMovementIntent`、`ResolveSpatialFacts`、`DeriveLocomotionDecisionFacts`、`BuildStateMachineContext`。
3. TurnBack intent 在 `DeriveLocomotionDecisionFacts()` 中由当前移动意图、当前世界移动方向、人物当前平面朝向、step、角度阈值和短窗口派生。
4. `CharacterStateMachineContext` 携带 `LocomotionDecisionFacts`，旧构造器仍兼容但默认没有 TurnBack intent。
5. `MoveTurnBackRequested` 只读取 `context.LocomotionFacts.TurnBackIntent`，不再现场计算夹角作为触发来源。
6. 默认状态机补齐 `MoveStart -> TurnBack`、`MoveLoop -> TurnBack`、`MoveStop -> TurnBack`，不增加 `Idle -> TurnBack`。
7. `BasicLocomotionPipeline` 改为消费同一份 `LocomotionDecisionFacts`，不再从 raw input 和 camera basis 重新计算 intent/world direction。
8. TurnBack root motion facts 不再优先采样 motion profile；TurnBack 只消费 `ILocomotionRootMotionSource`，并始终 suppress 输入旋转和输入平面位移。
9. `LocomotionRuntimeRollbackState` 保存 pending TurnBack intent，避免 rollback/replay 丢失短窗口事实。
10. 动画播放进度推进收进 `TryPrepareDecisionFrame()`，普通 Locomotion 入口和 FullBody 入口共用同一 prepare 时序，避免 FullBody 直接 prepare 时漏掉 phase/animation progress 更新。

## 2026-06-13 续查：Generic TurnBack root motion 接管

### 当前结论

1. 本轮只处理 Generic `可琳.prefab` 路线，暂不处理 Humanoid。
2. Generic `Locomotion.Turn.Back` 仍指向 `WithWeaponRootmotion/Corin_TurnBack_WithWeaponRootmotion.anim`，没有换成 inplace。
3. 表现对象是子物体，所以 root motion 不能直接让视觉物体驱动世界位置；正确链路是 Animator 产出 `deltaPosition` / `deltaRotation`，`BasicLocomotionAnimancerPresenter.OnAnimatorMove()` 采集，再由 `CharacterControllerBasicMotionExecutor` 应用到角色运动根。
4. 旧 runtime 约束“不写 `Animator.applyRootMotion`”会导致某些场景/时序下 TurnBack 动画播放但没有 root motion delta；本轮已改为 TurnBack 手动 root motion 活跃时显式写 true，Action 和非 TurnBack locomotion 写 false。
5. `animator-root-motion-policy` 日志现在看 `animatorApplyRootMotionAfter`，不再看旧的 `animatorApplyRootMotionWriteSkipped`。

### 修改内容

1. `AnimatorRootMotionController.Apply()` 写入 `Animator.applyRootMotion`，并记录 before/after。
2. `SetLocomotionRootMotion()` 在请求没变但 Animator bool 被外部改掉时也会重新应用策略。
3. 更新测试 `RootMotionPolicyEnablesAnimatorApplyRootMotionForManualTurnBack`，覆盖 TurnBack 打开 Animator root motion 评价、Action 关闭评价。
4. 更新 TurnBack/Action root motion 仲裁测试，补充 `Animator.applyRootMotion` 断言。

### 验证结果

1. `dotnet build 3cDemo\Client\3C_Client\Assembly-CSharp.csproj --no-restore -v:minimal` 通过，0 warning，0 error。
2. `dotnet build 3cDemo\Client\3C_Client\Assembly-CSharp-Editor.csproj --no-restore -v:minimal` 通过，0 warning，0 error。
3. Unity Test Runner 与 Sandbox 手测仍需要在 Unity Editor 内执行；本轮没有跑 Unity batchmode。
11. FullBody/Dodge request 也改为消费同一份 `LocomotionDecisionFacts`：Dodge 按钮仍来自 `InputRequestBuffer`，directional dodge 使用 `SpatialFacts.WorldMoveDirection`，backstep 使用 `SpatialFacts.FacingForward`，Action gate 不再自行解析 raw input、camera basis 或 facing provider。
12. 新增日志：
   - `locomotion-decision-pipeline`：每帧输出输入、意图、空间事实、phase facts、TurnBack intent 摘要。
   - `locomotion-turnback-intent`：输出捕获、空输入窗口保持、清理、消费原因。
   - `locomotion-turnback-condition`：改为输出派生 intent 的 valid/origin/expire/angle，而不是误导性的现场重算角度。

### 测试结果

1. `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal` 通过，0 error；仅有项目既有 warning。
2. `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` 通过，0 error；仅有项目既有 warning。
3. `openspec validate refactor-locomotion-decision-pipeline --strict --no-interactive` 通过。
4. Unity Test Runner 未运行：当前可用工具不能直接连接 Unity Editor/Test Runner，本轮没有运行 batchmode。
5. 新增静态保护：`BasicLocomotionPipeline` 和 Action/Dodge gate 均不得重新调用 raw input、camera basis 或 facing provider 解析方向。

### Sandbox 验证重点

1. 前进切后退时，应先看到 `locomotion-turnback-intent reason=captured`，再看到 `locomotion-turnback-condition passed=True`，随后状态进入 `FullBody/Locomotion/TurnBack`。
2. 如果中间有一帧无输入，应看到 `locomotion-turnback-intent reason=hold-empty-input-window`，随后仍能在 `MoveStart` 或 `MoveStop` 消费 intent。
3. TurnBack 期间继续确认 `animation-motion-executor` 中 `appliedInputRotation=False`、`appliedInputPlanarMovement=False`。
4. 横向 A/D 切换不应该出现有效 TurnBack intent；如果出现，复制 `locomotion-decision-pipeline` 与 `locomotion-turnback-intent` 同 step 日志继续查。
5. Dodge 验证时，先看同 step 的 `locomotion-decision-pipeline worldMove/facing`，再看 `action-dodge-request-fact-probe requestWorld`；后者应该来自前者，而不是 raw Move 或相机重新计算。

## 2026-06-13 续查：正式化 TurnBack Locomotion 状态

### 当前结论

1. TurnBack 不再作为 `MoveLoop` 内的临时 root motion/yaw 特判，而是 `FullBody/Locomotion/TurnBack` 正式 Locomotion 状态。
2. 默认只允许 `MoveLoop + Run` 消费 TurnBack intent；`Idle`、`MoveStart`、`MoveStop`、`MoveLoop + Walk` 不直接触发 `Locomotion.Turn.Back`。
3. 进入 TurnBack 时状态机锁定本次目标方向，后续相机基准、输入方向或普通移动解析不能覆盖该锁定方向。
4. TurnBack 状态输出携带 `TurnBackMotionPolicy`，默认 alias 为 `Locomotion.Turn.Back`，yaw source 为 `AnimationYawWindow`，translation source 为 `None`，普通输入旋转和平面位移均 suppress。
5. 第一版默认忽略 TurnBack 动画平移尾巴；转完点后退出到 `MoveLoop` 或 `Idle`，普通移动重新接管速度和位移。
6. 烘焙数据没有废弃，但只能作为后续 `bakedMotionProfileId` 等纯数据入口；运行时不重新走旧 `MotionProfile`/`AnimationMotionProfileSampler` TurnBack 补丁。

### 当前实现链路

1. `PlayerLocomotionController` 先派生 `LocomotionDecisionFacts.TurnBackIntent`。
2. `CharacterStateMachineRunner` 只在 `MoveLoop + Run` 且 intent 有效时进入 `FullBody/Locomotion/TurnBack`。
3. `CharacterStateMachineFrame` 输出 TurnBack policy 和 locked world direction。
4. `PlayerLocomotionController.ResolveTurnBackRootMotionFacts()` 按 policy 消费 root motion/authored yaw facts，translation source 为 `None` 时输出零平面动画位移。
5. `MovementCommand` 携带 suppress 标志、animation yaw 和 policy。
6. `CharacterControllerBasicMotionExecutor` 是唯一运动出口，TurnBack 期间不叠加普通输入旋转和平面位移。
7. `BasicLocomotionAnimancerPresenter` 只播放 alias、暴露进度和采样 root motion facts，不直接切状态或写角色根。

### 新日志判断

1. `locomotion-turnback-state-policy`：确认 state path、alias、locked yaw/direction、yaw source、translation source、turn complete、suppress flags、progress 和 canExit。
2. `turnback-root-motion-consumed`：确认 raw root delta、authored yaw、applied yaw、applied planar delta、translation source 和 baked profile id。
3. `animation-motion-executor`：确认 `appliedInputRotation=False`、`appliedInputPlanarMovement=False`，并确认 animation yaw 由 executor 应用。
4. `locomotion-animation-played`：确认 `phase=TurnBack` 且 alias 为 `Locomotion.Turn.Back`。

### 最新自动验证

1. `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal` 通过。
2. `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal` 通过。
3. `Tests.Editor.UnifiedCharacterStateMachineTests` 共 `89/89` 通过。
4. `ThirdPersonSimulation.Tests.FullBodyRollbackReplayTests`、`ThirdPersonSimulation.Tests.LocalLatencyReconciliationTests`、`ThirdPersonSimulation.Tests.LocalRollbackSynctestFoundationTests` 共 `77/77` 通过。
5. `Tests.Editor.CorinTurnBackTurnOnlyClipBuilderTests`、`LocomotionMotionProfileBakeUtilityTests`、`CorinAnimationSplitterTests` 共 `23/23` 通过。
6. Unity Console error 为 `0`。
7. `openspec validate formalize-turnback-locomotion-state --strict --no-interactive` 通过。

### Sandbox 验证重点

1. 先进入 RunLoop，再 W/S 前后反向切换；Walk、MoveStart、MoveStop、Idle 不应直接触发 TurnBack。
2. TurnBack 期间应看到 `locomotion-turnback-state-policy suppressInputRotation=True suppressInputPlanarMovement=True translationSource=None`。
3. 转完点后应很快回到 `MoveLoop` 或 `Idle`，不继续用 TurnBack 动画后半段跑步尾巴拖慢角色。
4. 如果仍然倒走或慢，优先复制 `locomotion-turnback-state-policy|turnback-root-motion-consumed|animation-motion-executor|locomotion-animation-played`。
