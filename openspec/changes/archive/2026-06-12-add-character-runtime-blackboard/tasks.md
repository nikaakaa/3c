## 1. 事实分类和边界
- [x] 1.1 列出现有跨帧事实来源：locomotion gait memory、state machine restore state、action frame facts、animation playback progress。
- [x] 1.2 定义第一版黑板 facts 分类：Locomotion facts、Action facts、Animation facts、Debug facts。
- [x] 1.3 为每类 facts 写明唯一写入权威和允许读取方。
- [x] 1.4 明确第一版不迁移的内容：输入快照、完整动画播放表、Unity 对象引用。

## 2. 黑板纯数据模型
- [x] 2.1 新增角色运行时黑板纯 C# 模型。
- [x] 2.2 新增只读 snapshot 类型，供状态机 context、诊断和测试读取。
- [x] 2.3 新增 restore state 类型，支持保存和恢复黑板跨帧事实。
- [x] 2.4 确保黑板类型不引用 Animancer runtime、Transform、Camera、InputAction 或其它场景对象。

## 3. 写入权威接入
- [x] 3.1 由 Locomotion runtime 写入 last moving gait 和 MoveStop entry gait facts。
- [x] 3.2 由 Action runtime 写入 action active、action completed 和 action exit facts。
- [x] 3.3 由 Animation facts adapter 写入只读播放进度摘要，不让 Presenter 直接改逻辑状态。
- [x] 3.4 为每个写入入口添加最小诊断字段，保留现有 log。

## 4. 状态机 context 接入
- [x] 4.1 扩展 `CharacterStateMachineContext` 或等价 context，使其读取黑板 snapshot 中的纯数据 facts。
- [x] 4.2 保持 `CharacterStateMachineRunner` 不直接依赖 MonoBehaviour 或 Presenter。
- [x] 4.3 保持 transition evaluator 只读取纯数据 context。
- [x] 4.4 保持现有 `MoveStop`、Dodge、RunEnd 行为不回退。

## 5. Snapshot / Restore
- [x] 5.1 将黑板 snapshot 合入当前角色 simulation snapshot 或等价恢复结构。
- [x] 5.2 Restore 时恢复黑板 facts、状态机 restore state 和 locomotion frame 的一致性。
- [x] 5.3 添加重复 restore 的幂等测试。

## 6. 自动验证
- [x] 6.1 添加黑板默认值和写入权威单元测试。
- [x] 6.2 添加状态机 context 读取黑板 snapshot 的测试。
- [x] 6.3 添加 snapshot/restore 测试。
- [x] 6.4 添加静态边界测试，确认黑板模型不引用 Animancer、UnityEngine.Object、Transform、Camera、InputAction。
- [x] 6.5 添加回归测试，确认 RunEnd、Dodge 返回 Idle/MoveLoop 的现有行为保持。
- [x] 6.6 运行 `dotnet build .\Assembly-CSharp.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [x] 6.7 运行 `dotnet build .\Assembly-CSharp-Editor.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。

## 7. 手动验证
- [ ] 7.1 Unity Play Mode 下验证 WASD Idle、MoveStart、MoveLoop、RunEnd、WalkEnd 表现不回退。
- [ ] 7.2 验证 Shift 跑步后松开移动仍播放 RunEnd 并回 Idle。
- [ ] 7.3 验证 Dodge 后无输入回 Idle，Dodge 后有输入回 MoveLoop，连续 Dodge 后 RunLoop 能恢复。
- [ ] 7.4 在日志中确认黑板 snapshot/facts 可见，且没有新增第二状态机入口。
- [x] 7.5 不运行 Unity batchmode。

## 8. 文档和归档准备
- [x] 8.1 更新相关 Path/架构文档，说明黑板是 typed facts blackboard，不是 BBB RuntimeData。
- [x] 8.2 在完成实现后运行 `openspec validate add-character-runtime-blackboard --strict --no-interactive`。
- [ ] 8.3 用户手动验证通过后再归档。
