## 1. 准备与现状确认
- [x] 1.1 读取 `proposal.md`、`design.md` 和本任务清单，确认只实现本次 WASD pipeline 重构。
- [x] 1.2 搜索 `BasicWASDMovementController` 的引用，确认当前主调度入口和 prefab/scene 绑定范围。
- [x] 1.3 搜索 `CharacterController.Move`，记录当前位移权威位置。
- [x] 1.4 搜索 `Camera.main`、`CinemachineFreeLook`、`FreeLook` 在移动目录下的使用，确认移动逻辑没有直接依赖具体相机。
- [x] 1.5 搜索 `BasicLocomotionAnimationConfig`，确认不会恢复旧动画表路径。

## 2. 输入快照整理
- [x] 2.1 为本帧 Move/Look/deltaTime 定义最小输入快照数据结构或等价边界。
- [x] 2.2 将 `ReadMove()` 和 `ReadLook()` 的结果先进入输入快照，再进入后续步骤。
- [x] 2.3 保持 `enableInputOnEnable` 的现有行为。
- [x] 2.4 保持现有 debug log，不主动删除。

## 3. 移动意图与相机相对方向
- [x] 3.1 将 `MovementInputIntent.FromRaw` 的调用收敛到明确的意图处理步骤。
- [x] 3.2 保留输入死区和斜向归一化行为。
- [x] 3.3 保留 `CameraRelativeMovementResolver.Resolve` 作为相机相对方向计算入口。
- [x] 3.4 确认该步骤只消费 `ICameraMovementBasisProvider`。
- [x] 3.5 确认零输入时世界移动方向仍为 `Vector3.zero`。

## 4. 阶段与命令构建
- [x] 4.1 保留 `BasicMovementStateMachine` 作为 `Idle / MoveStart / MoveLoop / MoveStop` 阶段真相。
- [x] 4.2 保留 `BasicMovementSettings.FromConfig(config)` 的配置读取路径。
- [x] 4.3 保留 `MovementCommandBuilder.Build` 构建运动命令。
- [x] 4.4 确认命令中的速度仍来自 `settings.MaxPlanarSpeed * intent.Strength`。
- [x] 4.5 确认命令中的旋转速度仍来自 movement settings。

## 5. 运动权威
- [x] 5.1 保留 `CharacterMotionDriver.ExecuteBasicMovement` 作为命令执行入口。
- [x] 5.2 确认 `CharacterMotionDriver` 仍是唯一调用 `CharacterController.Move` 的角色移动类。
- [x] 5.3 确认 `BasicWASDMovementController` 不直接写 `transform.position`。
- [x] 5.4 确认动画 presenter 不直接写 `transform.position`。
- [x] 5.5 若发现必须新增其它位移入口，停止实施并回到 OpenSpec。

## 6. 动画表现
- [x] 6.1 保留 `MovementAnimationContext` 作为移动结果到动画外观层的数据边界。
- [x] 6.2 保留 `BasicLocomotionAnimancerPresenter.Present` 作为动画提交入口。
- [x] 6.3 确认 Presenter 继续直接使用 Animancer 序列化 transition/状态配置，不恢复旧 `BasicLocomotionAnimationConfigSO`。
- [x] 6.4 确认 Presenter 只在阶段变化时切换动画，避免每帧重播。
- [x] 6.5 确认 Presenter 不拥有移动阶段真相。

## 7. 相机协作
- [x] 7.1 保留 Look 输入经项目相机入口进入 FreeLook 适配链路。
- [x] 7.2 保留移动后触发相机 Resolve 的现有闭环。
- [x] 7.3 确认本次不在运行时覆盖 FreeLook 轨道、Follow、LookAt、轴范围或阻尼配置。
- [x] 7.4 确认 WASD 移动方向仍跟随 FreeLook 输出的平面方向。

## 8. 清理与一致性
- [x] 8.1 删除实施过程中产生但未被主链使用的临时类型。
- [x] 8.2 确认没有新增第二套 WASD controller 或独立角色控制器路径。
- [x] 8.3 确认命名表达 pipeline 边界，而不是 BBB 原类名复制。
- [x] 8.4 检查 asmdef/namespace 不产生新的分裂模块。

## 9. 自动验证
- [x] 9.1 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp.csproj --no-restore -v:minimal`。
- [x] 9.2 运行静态搜索确认 `CharacterController.Move` 只在 `CharacterMotionDriver` 中出现。
- [x] 9.3 运行静态搜索确认移动目录不直接依赖 `Camera.main` 或 `CinemachineFreeLook`。
- [x] 9.4 运行静态搜索确认 `BasicLocomotionAnimationConfig` 没有恢复。
- [x] 9.5 记录所有验证输出中的错误、警告和已知历史警告。

## 10. Unity 手动验证
- [ ] 10.1 打开当前演示场景并进入 Play Mode。
- [ ] 10.2 按 W，确认角色沿 FreeLook 当前平面 forward 移动。
- [ ] 10.3 按 A/S/D，确认角色沿相机相对方向移动。
- [ ] 10.4 移动时转动鼠标，确认 WASD 方向跟随新的 FreeLook 平面方向。
- [ ] 10.5 确认角色朝移动方向旋转。
- [ ] 10.6 确认 Idle、MoveStart、MoveLoop、MoveStop 表现仍能触发。
- [ ] 10.7 松开输入，确认角色回到 Idle。
- [ ] 10.8 检查 Inspector 中手动配置的 FreeLook 轨道、Follow、LookAt 和轴配置没有被代码覆盖。
- [ ] 10.9 若发现动画、相机或位移必须新增未审批路径才能通过，停止实施并回到 OpenSpec。

## 11. 收尾
- [x] 11.1 更新本任务清单为真实完成状态。
- [x] 11.2 向用户说明实际改动文件。
- [x] 11.3 向用户说明自动验证结果。
- [x] 11.4 向用户说明 Unity 手动验证步骤和结果。
