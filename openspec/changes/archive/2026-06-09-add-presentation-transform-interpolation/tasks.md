## 1. 现状确认
- [x] 1.1 确认 `Sandbox.unity` 中当前本地角色实例使用 simulation tick 驱动基础移动。
- [x] 1.2 确认 `LocomotionTickAdapter` 注册到 `SimulationTickPhase.ExecuteMotion`。
- [x] 1.3 确认 `PlayerLocomotionController.AutoUpdate` 在 tick locomotion 启用时关闭。
- [x] 1.4 确认基础位移最终仍由 `IBasicLocomotionMotionExecutor` 提交。
- [x] 1.5 确认当前 CharacterController 位移入口是 `CharacterController.Move`。
- [x] 1.6 确认 `可琳.prefab` 中 Animator、骨骼和 SkinnedMeshRenderer 的当前父子关系。
- [x] 1.7 确认 `可琳.prefab` 中 `Root`、`Bip001`、`Corin_body` 等可见对象当前是否直接挂在真实根下。
- [x] 1.8 确认 `Third Person Camera Rig.prefab` 中存在 `Third Person Rail CM vcam`。
- [x] 1.9 确认 `Third Person Camera Rig.prefab` 中存在 `CameraFollowTarget`。
- [x] 1.10 确认 `Third Person Camera Rig.prefab` 中存在 `CameraAimTarget`。
- [x] 1.11 搜索所有写入 Cinemachine Follow/LookAt 的代码路径。
- [x] 1.12 搜索所有写入 `CameraFollowTarget` / `CameraAimTarget` 的代码路径。
- [x] 1.13 记录哪些 dirty 文件属于本变更范围，哪些是已有无关变更。

## 2. Tick 插值读数
- [x] 2.1 在 tick core 中提供只读 accumulated ratio / interpolation alpha。
- [x] 2.2 保证 alpha 在 fixed delta 非法或缺失时有安全兜底。
- [x] 2.3 保证 alpha clamp 到 0..1。
- [x] 2.4 保证不足一个 tick 时 alpha 表达剩余时间比例。
- [x] 2.5 保证单帧产生一个 tick 后 alpha 表达追帧后的剩余时间比例。
- [x] 2.6 保证单帧产生多个 tick 后 alpha 表达追帧后的剩余时间比例。
- [x] 2.7 保证达到 max tick 追帧上限时 alpha 仍不越界。
- [x] 2.8 保证 tick core 不引用 Cinemachine。
- [x] 2.9 保证 tick core 不引用 Animancer。
- [x] 2.10 保证 tick core 不引用 CharacterController。
- [x] 2.11 保证 tick core 不引用场景 Transform。

## 3. 表现插值纯逻辑
- [x] 3.1 新增表现 pose 数据结构或等价纯数据输入。
- [x] 3.2 新增表现插值 resolver，输入上一 tick pose、当前 tick pose、alpha 和 snap threshold。
- [x] 3.3 实现 position 线性插值。
- [x] 3.4 实现 rotation 插值。
- [x] 3.5 实现 alpha 小于 0 时 clamp。
- [x] 3.6 实现 alpha 大于 1 时 clamp。
- [x] 3.7 实现首个样本 snap 到当前 pose。
- [x] 3.8 实现缺少上一样本 snap 到当前 pose。
- [x] 3.9 实现缺少 tick driver 时安全退化。
- [x] 3.10 实现 teleport / 超距变化 snap。
- [x] 3.11 保证 resolver 不依赖 MonoBehaviour。
- [x] 3.12 保证 resolver 不读写 Unity 场景对象。

## 4. 表现插值运行时组件
- [x] 4.1 新增表现层 Transform 插值组件，放在表现/运行时模块，不放在相机专属命名空间。
- [x] 4.2 组件序列化真实 source Transform。
- [x] 4.3 组件序列化 visual target Transform。
- [x] 4.4 组件序列化 tick driver 引用。
- [x] 4.5 组件序列化 snap threshold。
- [x] 4.6 组件在启用时捕获初始真实 pose。
- [x] 4.7 组件在 simulation tick 后捕获真实 pose 样本。
- [x] 4.8 组件在渲染帧写入 visual target pose。
- [x] 4.9 组件写 visual target 时不写 source Transform。
- [x] 4.10 组件不调用 `CharacterController.Move`。
- [x] 4.11 组件不调用 `PlayerLocomotionController.Tick`。
- [x] 4.12 组件缺少 visual target 时安全退化。
- [x] 4.13 组件缺少 source 时安全退化。
- [x] 4.14 组件缺少 tick driver 时直接跟随当前真实 pose。
- [x] 4.15 组件执行顺序早于相机目标代理刷新。
- [x] 4.16 组件执行顺序早于 CinemachineBrain 采样。
- [x] 4.17 保留现有 debug log，不删除未审批日志。

## 5. 角色 prefab 迁移
- [x] 5.1 为本地角色 prefab 明确真实模拟根。
- [x] 5.2 为本地角色 prefab 新增或指定表现根。
- [x] 5.3 保持 `CharacterController` 在真实模拟根。
- [x] 5.4 保持 locomotion 输入适配在真实模拟根或明确 gameplay 子模块。
- [x] 5.5 保持 locomotion tick adapter 在真实模拟根或明确 gameplay 子模块。
- [x] 5.6 保持 motion executor 在真实模拟根或明确 gameplay 子模块。
- [x] 5.7 将 Animator 归入表现根或表现根子树。
- [x] 5.8 将 Animancer 外观层归入表现根或保持可由表现根引用。
- [x] 5.9 将骨骼层级归入表现根或表现根子树。
- [x] 5.10 将 SkinnedMeshRenderer 归入表现根或表现根子树。
- [x] 5.11 保持动画配置 ScriptableObject 引用不丢失。
- [x] 5.12 保持 Animator avatar 引用不丢失。
- [x] 5.13 保持材质和 mesh 引用不丢失。
- [x] 5.14 确认真正移动的 Transform 仍是真实模拟根。
- [x] 5.15 确认最终渲染位置来自表现根。
- [x] 5.16 避免新增第二套角色控制器。
- [x] 5.17 避免新增绕过现有 locomotion 主线的移动路径。

## 6. 相机接入
- [x] 6.1 让相机 follow anchor 来源指向表现根或表现层派生锚点。
- [x] 6.2 保持 `CameraFollowTarget` 作为相机主路径输出代理。
- [x] 6.3 保持 `CameraAimTarget` 作为相机主路径输出代理。
- [x] 6.4 保持 FreeLook 或当前 live vcam 使用统一目标代理。
- [x] 6.5 保持 `Third Person Rail CM vcam` 使用统一目标代理或等价输出。
- [x] 6.6 确保 `ThirdPersonCameraController` 在 AutoTick 关闭时仍每帧刷新目标代理。
- [x] 6.7 确保相机碰撞约束读取表现层锚点。
- [x] 6.8 确保相机碰撞忽略 root 使用真实模拟根，避免把表现根当碰撞权威。
- [x] 6.9 避免在其他业务系统中直接写 `CameraFollowTarget`。
- [x] 6.10 避免在其他业务系统中直接写 `CameraAimTarget`。

## 7. 自动测试
- [x] 7.1 为 tick alpha 不足一个 tick 增加 EditMode 测试。
- [x] 7.2 为 tick alpha 单 tick 后余量增加 EditMode 测试。
- [x] 7.3 为 tick alpha 多 tick 后余量增加 EditMode 测试。
- [x] 7.4 为 tick alpha 追帧上限 clamp 增加 EditMode 测试。
- [x] 7.5 为 position 中间插值增加 EditMode 测试。
- [x] 7.6 为 rotation 中间插值增加 EditMode 测试。
- [x] 7.7 为 alpha 小于 0 clamp 增加 EditMode 测试。
- [x] 7.8 为 alpha 大于 1 clamp 增加 EditMode 测试。
- [x] 7.9 为首帧 snap 增加 EditMode 测试。
- [x] 7.10 为样本缺失 snap 增加 EditMode 测试。
- [x] 7.11 为 teleport / 超距 snap 增加 EditMode 测试。
- [x] 7.12 为运行时组件只写 visual target 增加 EditMode 测试。
- [x] 7.13 为运行时组件不写 source Transform 增加 EditMode 测试。
- [x] 7.14 为缺少 tick driver 的安全退化增加 EditMode 测试。
- [x] 7.15 为相机 AutoTick 关闭时每帧刷新目标代理增加 EditMode 测试。
- [x] 7.16 为角色 prefab 真实根与表现根分离增加结构测试。
- [x] 7.17 为相机跟随来源接入表现层输出增加结构测试。
- [ ] 7.18 运行表现插值相关定向 EditMode 测试。
- [ ] 7.19 运行 simulation tick alpha 相关定向 EditMode 测试。
- [ ] 7.20 运行现有 `SimulationTickSystemTests`。
- [ ] 7.21 运行现有 `PlayerLocomotionControllerTests` 中 tick locomotion 相关测试。
- [ ] 7.22 运行相机目标代理相关定向 EditMode 测试。

## 8. 静态验证
- [x] 8.1 搜索确认表现插值模块不在 movement 目录中实现。
- [x] 8.2 搜索确认 movement 目录不引用表现插值实现细节。
- [x] 8.3 搜索确认 simulation core 不引用表现 runtime。
- [x] 8.4 搜索确认 simulation core 不引用 Cinemachine。
- [x] 8.5 搜索确认没有新增 `CharacterController.Move` 旁路。
- [x] 8.6 搜索确认没有新增绕过 `PlayerLocomotionController` 的移动入口。
- [x] 8.7 搜索确认没有业务系统直接写相机目标代理。
- [ ] 8.8 检查 prefab diff 只包含本变更需要的表现根和引用迁移。
- [ ] 8.9 检查 scene diff 只包含本变更需要的实例引用迁移。

## 9. 手动验证
- [ ] 9.1 打开 `Sandbox.unity`。
- [ ] 9.2 进入 Play Mode。
- [ ] 9.3 设置或确认 tick rate 为 60。
- [ ] 9.4 在高刷新率或解除 VSync 下持续按 W 直线移动。
- [ ] 9.5 观察角色可见模型是否不再出现 60Hz 阶梯抖动。
- [ ] 9.6 观察相机跟随是否不再出现 60Hz 阶梯抖动。
- [ ] 9.7 同时移动和 Look，确认移动方向仍相机相对正确。
- [ ] 9.8 从静止到起步，确认 MoveStart 动画正常。
- [ ] 9.9 持续移动，确认 MoveLoop 动画正常。
- [ ] 9.10 松开移动，确认 MoveStop 动画正常。
- [ ] 9.11 靠近墙体测试相机碰撞约束仍工作。
- [ ] 9.12 临时禁用表现插值组件，对比 60 tick 阶梯抖动是否复现。
- [ ] 9.13 恢复表现插值组件，确认抖动再次消失。
- [ ] 9.14 将 tick rate 提高到 256 作为对照，确认 60 tick 下也不再依赖高 tick rate 才平滑。

## 10. OpenSpec 和收尾
- [x] 10.1 更新本变更的 tasks 完成状态，只有真实完成后才勾选。
- [x] 10.2 运行 `openspec validate add-presentation-transform-interpolation --strict --no-interactive`。
- [x] 10.3 修复所有 OpenSpec 校验问题。
- [x] 10.4 向用户说明自动测试命令和手动验证步骤。
- [ ] 10.5 等用户确认验证通过后再归档。
