## 1. 范围确认
- [x] 1.1 确认 `THIRDPERSON_DIAGNOSTIC_LOGS` 是否作为最终日志宏名。
- [x] 1.2 确认 FullBody HFSM 活跃变更中 `FullBodyStateSnapshot` 的最终字段名。
- [x] 1.3 确认 Locomotion 状态变化日志读取 `BasicLocomotionStateMachine.ActivePath` 或控制器已有 frame/snapshot，不新增状态路径来源。
- [x] 1.4 确认现有相机 `debugLog` 本次只保留不迁移。

## 2. 日志模型和门面
- [x] 2.1 新建诊断日志分类枚举，至少包含 FullBody、Locomotion、Action、Camera。
- [x] 2.2 新建诊断日志等级枚举，至少包含 Trace、Info、Warning、Error。
- [x] 2.3 新建日志事件数据模型，包含分类、等级、消息、状态路径、step/frame 和可选上下文。
- [x] 2.4 新建日志过滤配置，支持按分类开启关闭。
- [x] 2.5 新建统一日志门面，常规诊断输出受 `THIRDPERSON_DIAGNOSTIC_LOGS` 宏控制。
- [x] 2.6 保证 Error/Warning 的必要错误报告不被误裁切为普通诊断日志。
- [x] 2.7 新增日志通道 key，支持显式 key 和 `分类.消息` 默认 key。
- [x] 2.8 日志过滤配置支持按通道 key 开启关闭。
- [x] 2.9 日志格式包含通道 key，便于从 Console 反查 Inspector 开关。

## 3. 状态机日志接入
- [x] 3.1 在 FullBody 主调度入口记录 active path 变化。
- [x] 3.2 在 FullBody 主调度入口记录 pending transition 变化。
- [x] 3.3 在 Dodge 请求被接受时记录 Action state、owner、step 和动画命令 key。
- [x] 3.4 在 Dodge 请求被拒绝时记录 reject reason、当前 Action state 和请求 step。
- [x] 3.5 在 Dodge 完成并回到 Locomotion 时记录 owner/path 变化。
- [x] 3.6 在 Locomotion phase/path 变化时记录旧 phase、新 phase、gait 和 phase time。
- [x] 3.7 避免每帧重复输出同一状态路径，除非显式开启连续帧诊断。

## 4. 边界约束
- [x] 4.1 日志系统不得引用 Animancer runtime 类型。
- [x] 4.2 日志系统不得引用 Animator state、AnimationClip 或 TransitionAsset。
- [x] 4.3 日志系统不得引用 CharacterController、KCC 或直接移动角色。
- [x] 4.4 日志系统不得引用 Unity Input System action 类型。
- [x] 4.5 日志系统不得调用状态机 transition API 或修改状态事实。

## 5. 自动测试
- [x] 5.1 EditMode 测试覆盖日志分类过滤。
- [x] 5.2 EditMode 测试覆盖状态路径变化时生成日志事件。
- [x] 5.3 EditMode 测试覆盖相同状态路径不会重复刷屏。
- [x] 5.4 EditMode 测试覆盖 Dodge accepted/rejected 日志内容。
- [x] 5.5 EditMode 测试覆盖 Locomotion phase/path 变化日志内容。
- [x] 5.6 静态测试验证日志门面源码包含 `THIRDPERSON_DIAGNOSTIC_LOGS` 宏裁切。
- [x] 5.7 静态测试验证日志系统不引用 Animancer、CharacterController、KCC、InputAction、Cinemachine 或 BBB 运行时。

## 6. 验证
- [x] 6.1 运行定向 EditMode 测试：日志模型、FullBody/Locomotion 日志接入、静态边界检查。
- [x] 6.2 运行 `openspec validate add-runtime-diagnostic-logging --strict --no-interactive`。
- [x] 6.3 手动验证：在 Unity Scripting Define Symbols 中开启 `THIRDPERSON_DIAGNOSTIC_LOGS`，进入 Play Mode，普通 WASD 时 Console 显示 Locomotion path 变化。
- [x] 6.4 手动验证：按 Dodge 输入时 Console 显示 Action.Dodge accepted、`/FullBody/Action/Dodge` 和回到 Locomotion 的日志。
- [x] 6.5 手动验证：移除 `THIRDPERSON_DIAGNOSTIC_LOGS` 后重新编译，普通诊断日志不再输出，玩法行为不变。

## 7. Inspector 开关控制器
- [x] 7.1 确认 Inspector 控制器只操作 `RuntimeDiagnosticLog.Filter`，不主动发出测试日志。
- [x] 7.2 新建可序列化的日志通道开关数据，字段只包含通道 key 和启用状态。
- [x] 7.3 新建场景组件 `RuntimeDiagnosticLogInspectorController`。
- [x] 7.4 在 `Reset` 中按已知日志通道 key 初始化通道列表。
- [x] 7.5 在 `OnEnable` 中把 Inspector 通道设置应用到统一过滤器。
- [x] 7.6 在 `OnValidate` 中补齐缺失通道 key 并避免重复 key。
- [x] 7.7 暴露 `ApplyChannels` 方法用于手动应用当前 Inspector 设置。
- [x] 7.8 暴露 `EnableAllChannels` 方法用于一键开启所有通道。
- [x] 7.9 暴露 `DisableAllChannels` 方法用于一键关闭所有通道。
- [x] 7.10 暴露按通道 key 包含文本批量启用通道的方法。
- [x] 7.11 暴露按通道 key 前缀批量启用通道的方法。
- [x] 7.12 暴露按通道 key 后缀批量启用通道的方法。
- [x] 7.13 新建 Editor 检视器，在 Inspector 中显示应用、全开、全关、包含、前缀和后缀筛选。
- [x] 7.14 Editor 检视器不得直接调用 `Debug.Log` 输出诊断日志。
- [x] 7.15 EditMode 测试覆盖 Inspector 通道应用到统一过滤器。
- [x] 7.16 EditMode 测试覆盖前缀筛选只开启匹配通道 key。
- [x] 7.17 EditMode 测试覆盖后缀筛选只开启匹配通道 key。
- [x] 7.18 EditMode 测试覆盖包含文本筛选只开启匹配通道 key。
- [x] 7.19 EditMode 测试覆盖通道列表会补齐已知日志通道 key。
- [x] 7.20 静态测试验证 Inspector 控制器不引用状态机、Animancer、CharacterController、InputAction 或 Cinemachine。
- [x] 7.21 静态测试验证 Inspector 控制器和 Editor 检视器不调用 `RuntimeDiagnosticLog.Submit` 或 `Debug.Log` 发测试日志。
- [x] 7.22 运行包含 Inspector 控制器的新定向 EditMode 测试。
- [x] 7.23 手动验证：在场景中新建空对象并挂载日志开关控制器。
- [x] 7.24 手动验证：Play Mode 中关闭 Action 通道后，Action 业务日志不再输出。
- [x] 7.25 手动验证：通过包含文本 `Action` 筛选后，仅 key 包含 `Action` 的通道开启。
- [x] 7.26 手动验证：通过前缀或后缀筛选后，仅 key 匹配的通道开启。
- [x] 7.27 手动验证：移除 `THIRDPERSON_DIAGNOSTIC_LOGS` 后重新编译，Player 普通诊断日志不输出，玩法行为不变。
