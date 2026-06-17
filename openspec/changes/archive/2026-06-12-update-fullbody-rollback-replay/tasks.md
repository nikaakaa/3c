## 1. 现状复核
- [x] 1.1 读取 `add-local-rollback-synctest-foundation` 的 proposal、design、tasks 和 spec delta。
- [x] 1.2 读取 `add-character-runtime-blackboard` 中与 snapshot/restore 相关的任务和 spec。
- [x] 1.3 读取 `refactor-unified-character-state-machine` 中最终状态机 runner / restore 边界。
- [x] 1.4 读取 `add-turn-in-place-locomotion` 中 runtime blackboard / turn facts 字段变化。
- [x] 1.5 确认当前 Sandbox 的动作入口是 `PlayerFullBodyActionController`。
- [x] 1.6 确认当前 `LocomotionRollbackSimulation` 仍只作为 locomotion-only adapter 保留。
- [x] 1.7 确认本变更不修改 Fantasy proto、协议导出工具或服务端代码。

## 2. FullBody replay 边界模型
- [x] 2.1 定义 full-body replay adapter 的职责。
- [x] 2.2 定义 full-body replay adapter 与 `ILocalRollbackSynctestSimulation` 的关系。
- [x] 2.3 确认 adapter 不直接调用 `BasicLocomotionPipeline`。
- [x] 2.4 确认 adapter 不直接调用 `CharacterController.Move`。
- [x] 2.5 确认 adapter 不保存 Unity Object 到快照。
- [x] 2.6 确认 adapter 不保存 Animancer runtime state。
- [x] 2.7 保留 locomotion-only replay adapter 的现有测试。

## 3. FullBody restore state
- [x] 3.1 列出 `PlayerFullBodyActionController` replay 必须恢复的字段。
- [x] 3.2 新增 full-body action 纯数据 restore state。
- [x] 3.3 restore state 包含当前 full-body snapshot。
- [x] 3.4 restore state 包含 debug path 或等价可诊断字段。
- [x] 3.5 restore state 不包含 `InputRequestBufferComponent` 引用。
- [x] 3.6 restore state 不包含 animation presenter 引用。
- [x] 3.7 restore state 不包含 runtime policy object 引用。
- [x] 3.8 `PlayerFullBodyActionController` 提供 capture 方法。
- [x] 3.9 `PlayerFullBodyActionController` 提供 restore 方法。
- [x] 3.10 restore 后下一 tick 的 active state 与恢复前一致。
- [x] 3.11 restore 后 pending transition 与恢复前一致。
- [x] 3.12 restore 后 action variant 与恢复前一致。

## 4. 输入帧到输入缓冲回灌
- [x] 4.1 定义 `PredictionButtonFrame` 到 `InputButtonState` 的转换。
- [x] 4.2 Dodge pressed 回灌为 Dodge request。
- [x] 4.3 Attack pressed 回灌为 Attack request。
- [x] 4.4 Jump pressed 回灌为 Jump request。
- [x] 4.5 Interact pressed 回灌为 Interact request。
- [x] 4.6 held 不重复生成 pressed request。
- [x] 4.7 released 不生成 pressed request。
- [x] 4.8 replay tick 前调用 `InputRequestBufferComponent.SetStep(input.Tick.Value)`。
- [x] 4.9 replay tick 前移除已过期请求。
- [x] 4.10 replay 不写入动作结果。
- [x] 4.11 replay 不直接调用 action gate 以外的消费路径。

## 5. FullBody replay adapter 实现
- [x] 5.1 新增 full-body rollback simulation adapter。
- [x] 5.2 adapter 引用 `PlayerFullBodyActionController`。
- [x] 5.3 adapter 引用 `PlayerLocomotionController`。
- [x] 5.4 adapter 引用 `InputRequestBufferComponent`。
- [x] 5.5 adapter capture 同时采集 locomotion snapshot 和 full-body restore state。
- [x] 5.6 adapter restore 先恢复 locomotion snapshot。
- [x] 5.7 adapter restore 再恢复 full-body action state。
- [x] 5.8 adapter advance 先按 tick 写入 input buffer。
- [x] 5.9 adapter advance 再调用 `PlayerFullBodyActionController.Tick(...)`。
- [x] 5.10 adapter 使用 `PredictionInputFrame.ToLocomotionInput(fixedDelta)` 构造移动输入。
- [x] 5.11 adapter fixed delta 使用 `SimulationTickRate` 或配置 tick rate。
- [x] 5.12 adapter 缺少必要引用时返回明确失败诊断或不注册。
- [x] 5.13 adapter 不改变 `PlayerFullBodyActionController.AutoUpdate` 的长期状态。
- [x] 5.14 adapter 不改变 `PlayerLocomotionController.AutoUpdate` 的长期状态。

## 6. Debug runner 接入
- [x] 6.1 让 `LocalRollbackSynctestDebugRunner` 支持 full-body adapter。
- [x] 6.2 保持默认安全探针：F6 后恢复触发前现场。
- [x] 6.3 保持可选可见 correction：仅显式开启时应用 replay result。
- [x] 6.4 FAIL 日志继续输出 reason 和 differences。
- [x] 6.5 PASS 日志继续输出 restore/end tick。
- [x] 6.6 缺少 full-body adapter 引用时输出 missing simulation 诊断。
- [x] 6.7 不新增 Fantasy 调试入口。

## 7. 自动测试：输入回灌
- [x] 7.1 测试 Dodge pressed 生成 Dodge request。
- [x] 7.2 测试 Dodge held 不重复生成 request。
- [x] 7.3 测试 Dodge released 不生成 request。
- [x] 7.4 测试 Attack/Jump/Interact pressed 可生成对应 request。
- [x] 7.5 测试 replay step 使用 input tick。
- [x] 7.6 测试 replay 会裁剪过期请求。

## 8. 自动测试：FullBody restore
- [x] 8.1 测试 capture/restore 保持 current owner。
- [x] 8.2 测试 capture/restore 保持 action state。
- [x] 8.3 测试 capture/restore 保持 locomotion phase。
- [x] 8.4 测试 capture/restore 保持 pending transition。
- [x] 8.5 测试 restore 后下一 tick 不重复消费旧请求。
- [x] 8.6 测试 restore 后下一 tick action facts 来源 step 稳定。
- [x] 8.7 测试 restore state 不包含 Unity Object 字段。

## 9. 自动测试：FullBody replay
- [x] 9.1 测试 Move 输入 replay 后 position/yaw 收敛。
- [x] 9.2 测试 Run 输入 replay 后 run latch / gait 收敛。
- [x] 9.3 测试 Dodge pressed replay 后进入相同 Dodge state。
- [x] 9.4 测试 Dodge replay 后 action facts 收敛。
- [x] 9.5 测试 Dodge replay 后 blackboard action sourceStep 收敛。
- [x] 9.6 测试 animation facts 使用 fake presenter 时收敛。
- [x] 9.7 测试 locomotion-only adapter 仍可独立通过原有测试。
- [x] 9.8 测试 full-body adapter 不新增第二 movement controller。

## 10. 静态边界验证
- [x] 10.1 搜索 full-body replay core 不引用 Fantasy。
- [x] 10.2 搜索 full-body replay core 不引用协议 DTO。
- [x] 10.3 搜索 full-body replay core 不引用 Animancer runtime object。
- [x] 10.4 搜索 full-body replay core 不引用 Cinemachine。
- [x] 10.5 搜索 full-body replay core 不直接调用 `CharacterController.Move`。
- [x] 10.6 搜索未新增第二套 player controller。
- [x] 10.7 搜索未修改 `NetworkProtocol/*.proto`。

## 11. 手动验证
- [x] 11.1 打开 `Assets/Scenes/Sandbox.unity`。
- [x] 11.2 进入 Play Mode。
- [x] 11.3 不启用 synctest 时验证 WASD/Look/Run 正常。
- [x] 11.4 不启用 synctest 时验证 Dodge 正常进入和退出。
- [x] 11.5 启用 full-body replay adapter 的记录和快照组件。
- [x] 11.6 先移动、Run、Dodge 几秒积累历史。
- [x] 11.7 按 F6 运行 full-body synctest。
- [x] 11.8 验证 Console 输出 PASS 或 FAIL differences 明显少于 locomotion-only replay。
- [x] 11.9 验证 position/yaw 不回退为 mismatch。
- [x] 11.10 验证 `blackboard.action.sourceStep` 不再持续 mismatch。
- [x] 11.11 验证 `blackboard.animation.*` 若仍 mismatch，日志能定位到 animation fact 而非 action replay 缺失。
- [x] 11.12 打开可见 correction 模式，注入位置差异时表现根平滑追上逻辑根。

## 12. 验证命令
- [x] 12.1 运行 `openspec validate update-fullbody-rollback-replay --strict --no-interactive`。
- [x] 12.2 运行 Unity Editor 编译，确认 Console 无编译错误。
- [x] 12.3 运行定向 EditMode 测试 `ThirdPersonSimulation.Tests.LocalRollbackSynctestFoundationTests`。
- [x] 12.4 运行定向 EditMode 测试 `Tests.Editor.UnifiedCharacterStateMachineTests`。
- [x] 12.5 运行定向 EditMode 测试 `Tests.Editor.InputRequestBufferTests`。
- [x] 12.6 运行定向 EditMode 测试 `ThirdPersonPresentation.Tests.PresentationTransformInterpolatorTests`。
- [x] 12.7 不使用 Unity batchmode。

## 13. 后续交接
- [x] 13.1 在文档中记录 full-body replay 已覆盖和未覆盖动作。
- [x] 13.2 明确本变更完成后再规划 `add-local-latency-reconciliation-simulator`。
- [x] 13.3 明确 Fantasy proto 接入必须晚于本地 latency simulator 验收。
