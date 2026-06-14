## 1. 边界确认
- [x] 1.1 读取 `refactor-unified-character-state-machine` 的 proposal、design 和 spec delta。
- [x] 1.2 确认旧 FullBody/HFSM/Dodge 缝合路线不得作为 synctest 实现基线。
- [x] 1.3 列出现有 tick phase 中输入记录和快照记录的接入点。
- [x] 1.4 列出当前 `CharacterStateMachineRunner` 需要恢复的内部事实。
- [x] 1.5 列出当前 `PlayerLocomotionController` 需要恢复或比较的运行时事实。
- [x] 1.6 列出当前 `CharacterMotionDriver` 可能无法恢复的事实。

## 2. 输入帧模型
- [x] 2.1 新增本地预测输入帧模型。
- [x] 2.2 输入帧包含 tick。
- [x] 2.3 输入帧包含 Move。
- [x] 2.4 输入帧包含 Look。
- [x] 2.5 输入帧包含 Run held。
- [x] 2.6 输入帧包含 Dodge pressed/held/released。
- [x] 2.7 输入帧预留 Attack/Jump/Interact 按钮事实字段或扩展点。
- [x] 2.8 输入帧构造时 clamp 非法 Move/Look。
- [x] 2.9 输入帧构造时拒绝或修正负 tick。
- [x] 2.10 输入帧提供到 `BasicLocomotionInputSnapshot` 的转换。

## 3. 输入历史
- [x] 3.1 新增输入历史 ring buffer。
- [x] 3.2 支持按 tick 写入输入帧。
- [x] 3.3 支持同 tick 覆盖输入帧。
- [x] 3.4 支持按 tick 查询输入帧。
- [x] 3.5 支持读取 tick 区间输入帧。
- [x] 3.6 支持容量上限。
- [x] 3.7 支持裁剪已确认 tick 前输入。
- [x] 3.8 缺失输入时返回失败并输出诊断事实。
- [x] 3.9 测试连续写入。
- [x] 3.10 测试覆盖写入。
- [x] 3.11 测试区间读取。
- [x] 3.12 测试容量裁剪。
- [x] 3.13 测试缺失读取。

## 4. 角色模拟快照 v0
- [x] 4.1 新增角色模拟快照模型。
- [x] 4.2 快照包含 tick。
- [x] 4.3 快照包含真实模拟根 position。
- [x] 4.4 快照包含真实模拟根 yaw。
- [x] 4.5 快照包含统一状态机 active state。
- [x] 4.6 快照包含统一状态机 state time。
- [x] 4.7 快照包含统一状态机 variant。
- [x] 4.8 快照包含 pending transition。
- [x] 4.9 快照包含 Run latch。
- [x] 4.10 快照包含 last moving gait。
- [x] 4.11 快照包含 current world direction。
- [x] 4.12 快照包含 locomotion phase/gait。
- [x] 4.13 快照预留 animation key/progress 字段或扩展点。
- [x] 4.14 快照构造时处理 NaN/Infinity。
- [x] 4.15 快照不保存 Unity Object 引用。

## 5. 快照历史
- [x] 5.1 新增快照历史 ring buffer。
- [x] 5.2 支持按 tick 写入快照。
- [x] 5.3 支持同 tick 覆盖快照。
- [x] 5.4 支持按 tick 查询快照。
- [x] 5.5 支持查询最近可恢复 tick。
- [x] 5.6 支持裁剪已确认 tick 前快照。
- [x] 5.7 缺失快照时返回失败并输出诊断事实。
- [x] 5.8 测试写入和查询。
- [x] 5.9 测试覆盖。
- [x] 5.10 测试裁剪。
- [x] 5.11 测试恢复点缺失。

## 6. 快照采集边界
- [x] 6.1 定义快照采集 adapter。
- [x] 6.2 adapter 从真实模拟根读取 position/yaw。
- [x] 6.3 adapter 从统一状态机读取快照。
- [x] 6.4 adapter 从 `PlayerLocomotionController` 读取 Run latch、gait 和 world direction。
- [x] 6.5 adapter 从动画事实源读取只读 progress。
- [x] 6.6 adapter 在 `WriteSnapshotAndEvents` phase 写入快照历史。
- [x] 6.7 测试 adapter 不写真实模拟根。
- [x] 6.8 测试 adapter 不写表现根。
- [x] 6.9 测试 adapter 不播放 Animancer。

## 7. 状态恢复边界
- [x] 7.1 定义统一状态机恢复输入模型。
- [x] 7.2 恢复 active state。
- [x] 7.3 恢复 state time。
- [x] 7.4 恢复 variant。
- [x] 7.5 恢复 action world direction。
- [x] 7.6 恢复 pending transition。
- [x] 7.7 恢复 animation requested 标记或等价事实。
- [x] 7.8 恢复输入消费标记或等价事实。
- [x] 7.9 恢复 Run latch。
- [x] 7.10 恢复 last moving gait。
- [x] 7.11 恢复 current world direction。
- [x] 7.12 恢复真实模拟根 position/yaw。
- [x] 7.13 记录 motion driver 无法恢复的事实。

## 8. 快照比较
- [x] 8.1 定义快照比较结果。
- [x] 8.2 比较 tick。
- [x] 8.3 比较 position 容差。
- [x] 8.4 比较 yaw 容差。
- [x] 8.5 比较 active state。
- [x] 8.6 比较 state time 容差。
- [x] 8.7 比较 variant。
- [x] 8.8 比较 Run latch。
- [x] 8.9 比较 gait。
- [x] 8.10 输出差异字段列表。
- [x] 8.11 测试相同快照通过。
- [x] 8.12 测试位置差异失败。
- [x] 8.13 测试状态差异失败。

## 9. 本地 Synctest 编排
- [x] 9.1 新增本地 synctest runner。
- [x] 9.2 支持记录起始 tick。
- [x] 9.3 支持记录结束 tick。
- [x] 9.4 支持正常运行并保存每 tick 输入和快照。
- [x] 9.5 支持选择恢复 tick。
- [x] 9.6 支持加载恢复 tick 快照。
- [x] 9.7 支持用恢复 tick 后的输入历史逐 tick 重放。
- [x] 9.8 支持重放后比较最终快照。
- [x] 9.9 支持输出通过/失败诊断。
- [x] 9.10 输入历史缺失时停止。
- [x] 9.11 快照历史缺失时停止。
- [x] 9.12 新增 Play Mode 本地 synctest debug 入口，支持按键触发恢复和重放。

## 10. 自动测试
- [x] 10.1 新增输入帧测试。
- [x] 10.2 新增输入历史测试。
- [x] 10.3 新增角色模拟快照测试。
- [x] 10.4 新增快照历史测试。
- [x] 10.5 新增快照比较测试。
- [x] 10.6 新增状态恢复测试。
- [x] 10.7 新增本地 synctest runner 测试。
- [x] 10.8 新增静态边界测试。
- [x] 10.9 新增 debug runner 恢复历史 tick 并重放记录输入的测试。
- [ ] 10.10 复跑包含 debug runner 测试的定向 EditMode 测试，不使用 Unity batchmode。

## 11. 静态边界验证
- [x] 11.1 验证 synctest core 不引用 Animancer。
- [x] 11.2 验证 synctest core 不引用 Cinemachine。
- [x] 11.3 验证 synctest core 不引用 Input System adapter。
- [x] 11.4 验证 synctest core 不引用 `CharacterController`。
- [x] 11.5 验证 runtime adapter 不新增第二套 movement controller。
- [x] 11.6 验证快照模型不保存 Unity Object 字段。

## 12. 手动验证
- [x] 12.1 打开当前 Sandbox 或演示场景。
- [ ] 12.2 未启用 synctest 时验证 WASD/Look/Run 行为不变。
- [ ] 12.3 未启用 synctest 时验证 Dodge 行为不变。
- [ ] 12.4 启用仅记录输入和快照模式，验证本地行为不变。
- [ ] 12.5 按 F6 运行本地 synctest，验证通过/失败诊断可见。
- [x] 12.6 记录用户如何复现验证。

## 13. 文档和验收
- [x] 13.1 更新 `docs/agents/action-fighting-prediction-rollback-guide.md` 的阶段状态。
- [x] 13.2 确认本阶段保留 spec delta，归档阶段再由 `openspec archive` 合入正式 specs。
- [x] 13.3 执行 `openspec validate add-local-rollback-synctest-foundation --strict --no-interactive`。
- [x] 13.4 明确告诉用户如何运行定向测试。
- [x] 13.5 明确告诉用户如何手动验证。
