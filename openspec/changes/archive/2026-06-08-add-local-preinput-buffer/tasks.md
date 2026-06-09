## 1. 准备与边界确认
- [x] 1.1 读取本 change 的 `proposal.md`、`design.md`、`tasks.md` 和 spec delta。
- [x] 1.2 读取 `openspec/project.md`，确认输入、状态、运动、动画、网络边界。
- [x] 1.3 读取 `integrate-unityhfsm-locomotion` 的 proposal/design/spec，确认不重复创建 Locomotion 输入端口。
- [x] 1.4 搜索 `IBasicLocomotionInputSource`、`BasicLocomotionInputSnapshot`、`PlayerLocomotionController`，记录当前 Move/Look 消费路径。
- [x] 1.5 搜索 `ActionArbiter`、`ActionController`、`Attack`、`Dodge`、`Jump`，确认当前动作消费层是否存在。
- [x] 1.6 实施未修改 Fantasy 协议、服务端代码、预测回滚驱动或完整 ActionArbiter。

## 2. 本地输入 step
- [x] 2.1 本地输入 step 第一版使用 `int`。
- [x] 2.2 本地输入 step 只服务输入缓冲窗口，不作为完整 SimulationClock。
- [x] 2.3 EditMode 测试通过显式整数 step 控制推进。
- [x] 2.4 输入缓冲窗口不依赖 `Time.time`。

## 3. 离散按钮种类
- [x] 3.1 定义 Attack 输入种类。
- [x] 3.2 定义 Dodge 输入种类。
- [x] 3.3 定义 Jump 输入种类。
- [x] 3.4 定义 Interact 输入种类。
- [x] 3.5 离散输入种类不绑定具体动画、状态或技能实现。

## 4. 按钮事实模型
- [x] 4.1 定义 `InputButtonState` 纯数据结构。
- [x] 4.2 按钮事实包含 pressed。
- [x] 4.3 按钮事实包含 held。
- [x] 4.4 按钮事实包含 released。
- [x] 4.5 定义 `InputButtonState.FromHeld` 计算 pressed/released。
- [x] 4.6 按钮事实不引用 Unity 场景对象。
- [x] 4.7 按钮事实不引用 Animancer、Cinemachine、CharacterController 或状态类。

## 5. 输入请求模型
- [x] 5.1 定义 `InputRequestKind` 请求种类。
- [x] 5.2 定义 `BufferedInputRequest` 纯数据结构。
- [x] 5.3 请求包含来源 step。
- [x] 5.4 请求包含过期 step。
- [x] 5.5 请求包含来源按钮种类。
- [x] 5.6 请求能标记本次模拟中已消费。
- [x] 5.7 请求不保存“已进入攻击/闪避状态”等动作结果。

## 6. 缓冲窗口配置
- [x] 6.1 定义 `InputBufferSettings` 窗口配置。
- [x] 6.2 Attack 支持独立窗口长度。
- [x] 6.3 Dodge 支持独立窗口长度。
- [x] 6.4 Jump 支持独立窗口长度。
- [x] 6.5 Interact 支持独立窗口长度。
- [x] 6.6 支持窗口长度为 0 的请求只在来源 step 可消费。
- [x] 6.7 负窗口长度 clamp 到 0。

## 7. 输入请求缓冲
- [x] 7.1 定义 `InputRequestBuffer` 容器。
- [x] 7.2 支持从按钮 pressed 事实添加请求。
- [x] 7.3 支持按当前 step 清理过期请求。
- [x] 7.4 支持按请求种类查询当前可消费请求。
- [x] 7.5 支持消费请求后避免同次模拟重复消费。
- [x] 7.6 支持清空缓冲。
- [x] 7.7 支持同一 step 多个不同种类请求并存。
- [x] 7.8 同一 kind 重复按下第一版保留队列顺序。

## 8. Unity Input System adapter 边界
- [x] 8.1 已评估：本次只做纯逻辑层，暂不新增离散按钮 adapter。
- [x] 8.2 本次未新增 adapter，因此没有新增 `InputActionReference` 持有点。
- [x] 8.3 本次实现不直接调用状态机。
- [x] 8.4 本次实现不直接调用 ActionArbiter。
- [x] 8.5 本次实现不直接调用 motion executor。
- [x] 8.6 本次实现不直接调用 Presenter。
- [x] 8.7 当前 Locomotion Move/Look 运行时代码未改动。

## 9. 消费边界
- [x] 9.1 使用 `TryConsume` 和 EditMode 测试作为 fake consumer/policy。
- [x] 9.2 fake consumer 能在窗口内消费 Attack 请求。
- [x] 9.3 fake consumer 不能消费过期请求。
- [x] 9.4 fake consumer 消费后同次模拟不能重复消费同一请求。
- [x] 9.5 真实 ActionArbiter 不在本 change 中实现。
- [x] 9.6 真实攻击/闪避/跳跃状态不在本 change 中实现。

## 10. EditMode 自动测试
- [x] 10.1 测试 false->true 产生 pressed 和 held。
- [x] 10.2 测试 true->true 只产生 held。
- [x] 10.3 测试 true->false 产生 released。
- [x] 10.4 测试 false->false 不产生 pressed/held/released。
- [x] 10.5 测试 pressed 生成输入请求。
- [x] 10.6 测试不同请求种类使用不同窗口。
- [x] 10.7 测试窗口长度为 0 的请求只在来源 step 可消费。
- [x] 10.8 测试请求在窗口内可消费。
- [x] 10.9 测试请求超过过期 step 不可消费。
- [x] 10.10 测试已消费请求不会在同次模拟重复消费。
- [x] 10.11 测试清空缓冲后没有可消费请求。
- [x] 10.12 测试同一按钮事实序列构建两次得到一致请求结果。
- [x] 10.13 测试输入缓冲不需要 Unity 场景对象。

## 11. 静态验证
- [x] 11.1 静态搜索确认输入缓冲模型不引用 Animancer。
- [x] 11.2 静态搜索确认输入缓冲模型不引用 `CharacterController`。
- [x] 11.3 静态搜索确认输入缓冲模型不引用 Cinemachine。
- [x] 11.4 静态搜索确认输入缓冲不调用 `CharacterController.Move`。
- [x] 11.5 静态搜索确认输入缓冲不调用动画播放 API。
- [x] 11.6 静态搜索确认没有新增第二套角色控制器入口。
- [x] 11.7 静态搜索确认没有新增 Fantasy 协议文件或服务端输入实现。
- [x] 11.8 静态搜索确认没有新增预测回滚驱动或状态快照历史。

## 12. 自动验证
- [x] 12.1 使用 Unity MCP 运行 `ThirdPersonInput.Tests.InputRequestBufferTests` EditMode 测试。
- [x] 12.2 运行 `openspec validate add-local-preinput-buffer --strict --no-interactive`。
- [x] 12.3 记录验证输出中的既有 warning 和 missing script error。

## 13. Unity 手动验证
- [x] 13.1 本变更未接入运行时输入路径，不需要新增 Play Mode 操作作为完成条件。
- [x] 13.2 提供 WASD Move 手动验证步骤：打开 `Sandbox`，进入 Play Mode，按 W/A/S/D 确认移动行为不变。
- [x] 13.3 提供 Look 手动验证步骤：移动鼠标或摇杆视角，确认 Look 行为不变。
- [x] 13.4 提供动画手动验证步骤：确认 Idle、MoveStart、MoveLoop、MoveStop 表现不回退。
- [x] 13.5 本次未接入离散输入调试输出，因此没有新增按钮调试手动验收项。

## 14. 收尾
- [x] 14.1 更新任务清单为真实完成状态。
- [x] 14.2 向用户说明实际改动文件。
- [x] 14.3 向用户说明自动测试和 OpenSpec 验证结果。
- [x] 14.4 向用户说明 Unity 手动验证步骤和结果边界。
