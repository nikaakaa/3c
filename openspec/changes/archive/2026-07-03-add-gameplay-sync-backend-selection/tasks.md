# Tasks

- [x] 1. 统一现有 loopback driver 入口
- [x] 1.1 确认 `CharacterGameplaySyncLoopbackDriver` 的当前职责和所有引用点
- [x] 1.2 新增正式 backend mode 类型，第一阶段只包含 `None` 和 `LocalLoopback`
- [x] 1.3 将 loopback 专用 driver 改名或替换为 `CharacterGameplaySyncDriver`
- [x] 1.4 保留现有 actor identity 字段在正式 driver 中
- [x] 1.5 将 loopback settings 收进 `LocalLoopback` 后端配置区域
- [x] 1.6 删除旧 `CharacterGameplaySyncLoopbackDriver` 类型入口

- [x] 2. 实现 backend 装配
- [x] 2.1 让 `None` mode 创建 `GameplaySyncRuntime` 并设置 null peer
- [x] 2.2 让 `LocalLoopback` mode 创建 `LocalGameplaySyncLoopbackPeer`
- [x] 2.3 让 backend 切换时重建 runtime peer 并清理旧 peer 调试状态
- [x] 2.4 保持 `BeforeLogicTick` 只执行 runtime pump 和 incoming 注入
- [x] 2.5 保持 `AfterLogicTick` 只执行 outgoing 收集和 flush
- [x] 2.6 确认 driver 不直接访问 Graph、ActionRuntime、MotionStage 或 Fantasy Session

- [x] 3. 更新 editor debug 入口
- [x] 3.1 将 `CharacterPipelineHostEditor` 查找对象从 loopback driver 改为正式 sync driver
- [x] 3.2 在 debug 区显示当前 backend mode
- [x] 3.3 在 `None` mode 下显示 backend 关闭状态并跳过 peer pending/incoming 细节
- [x] 3.4 在 `LocalLoopback` mode 下复用现有 outgoing、pending、incoming、dropped 展示

- [x] 4. 清理命名和引用
- [x] 4.1 搜索并删除旧 loopback driver 类型引用
- [x] 4.2 确认 loopback 命名只保留在 `LocalGameplaySyncLoopbackPeer` 和 settings
- [x] 4.3 确认没有新增 Fantasy 假配置、空实现或不可用选项
- [x] 4.4 确认 CharacterPipeline、NetworkSendStage、NetworkReceiveStage 和 adapter 没有新增 peer/transport 依赖

- [x] 5. 验证
- [x] 5.1 运行 C# 编译检查
- [x] 5.2 运行 `openspec validate add-gameplay-sync-backend-selection --strict --no-interactive`
- [x] 5.3 运行 `openspec validate --all --strict --no-interactive`
