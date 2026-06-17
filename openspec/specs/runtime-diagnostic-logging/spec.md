# runtime-diagnostic-logging Specification

## Purpose
定义运行时诊断日志通道、过滤器、格式化输出、Inspector 开关和编译开关边界，确保调试信息可控且不污染正式玩法路径。
## Requirements
### Requirement: 统一运行时诊断日志入口
系统 MUST 提供项目自有的运行时诊断日志入口，用于输出可过滤、可测试、格式稳定的调试信息。业务模块 MUST 通过该入口提交常规诊断日志，不得在新增状态机调试逻辑中直接散落 `Debug.Log` 调用。

#### Scenario: 业务模块提交诊断日志
- **WHEN** FullBody、Locomotion 或 Action 模块需要输出常规诊断信息
- **THEN** 模块 MUST 通过统一日志入口提交日志事件
- **AND** 日志事件 MUST 包含分类、等级、消息和稳定通道 key
- **AND** 日志格式 MUST 能稳定表达状态路径、step/frame 或等价上下文

#### Scenario: 未显式提供通道 key
- **WHEN** 业务模块提交日志事件时未显式提供通道 key
- **THEN** 系统 MUST 使用 `分类.消息` 生成默认通道 key
- **AND** 默认 key MUST 能被 Inspector 开关控制器单独开关

#### Scenario: 现有局部日志保留
- **WHEN** 本变更实施完成
- **THEN** 现有相机或工具里的局部日志 MUST NOT 被删除
- **AND** 是否迁移旧日志 MUST 由后续审批范围决定

### Requirement: 编译宏裁切诊断日志
系统 MUST 使用编译宏控制常规诊断日志输出，使开发者可以通过 Unity 构建符号开启或关闭日志，并在未开启宏时让普通诊断日志可从构建产物中裁切。

#### Scenario: 宏开启时输出诊断日志
- **GIVEN** 构建符号包含 `THIRDPERSON_DIAGNOSTIC_LOGS`
- **AND** 对应日志分类在运行时开关中启用
- **WHEN** 状态机提交诊断日志事件
- **THEN** 系统 MUST 将该事件输出到 Unity Console 或等价 Unity 日志出口

#### Scenario: 宏关闭时裁切普通诊断日志
- **GIVEN** 构建符号不包含 `THIRDPERSON_DIAGNOSTIC_LOGS`
- **WHEN** 运行普通 Player 构建
- **THEN** 常规诊断日志调用 MUST 不产生输出
- **AND** 常规诊断日志 MUST NOT 改变状态机、运动命令或动画播放行为

#### Scenario: 必要错误报告不被误归类
- **WHEN** 系统遇到配置缺失、非法状态或不可恢复错误
- **THEN** 实现 MUST 明确区分必要 warning/error 与可裁切诊断日志
- **AND** 不得为了宏裁切吞掉必须暴露的错误报告

### Requirement: 运行时分类开关
系统 MUST 在宏开启后支持按分类和通道 key 两层过滤日志，第一版至少覆盖 FullBody、Locomotion、Action 和 Camera 分类。分类过滤和通道过滤 MUST 是日志输出层行为，不得改变业务执行。

#### Scenario: 分类关闭时不输出
- **GIVEN** `THIRDPERSON_DIAGNOSTIC_LOGS` 已开启
- **AND** Locomotion 分类被关闭
- **WHEN** Locomotion 状态路径发生变化
- **THEN** 系统 MUST 不输出 Locomotion 分类日志
- **AND** Locomotion 状态机 MUST 仍按原规则切换

#### Scenario: 分类开启时输出
- **GIVEN** `THIRDPERSON_DIAGNOSTIC_LOGS` 已开启
- **AND** FullBody 分类被开启
- **AND** 对应通道 key 被开启
- **WHEN** FullBody active path 从 Locomotion 切到 Action.Dodge
- **THEN** 系统 MUST 输出 FullBody 分类日志
- **AND** 日志 MUST 包含新的 active path

#### Scenario: 通道关闭时不输出
- **GIVEN** `THIRDPERSON_DIAGNOSTIC_LOGS` 已开启
- **AND** Action 分类被开启
- **AND** `Action.interrupt-request-accepted` 通道 key 被关闭
- **WHEN** Action 仲裁接受请求
- **THEN** 系统 MUST 不输出该通道日志
- **AND** Action 仲裁结果 MUST 保持不变

### Requirement: 场景 Inspector 日志开关控制器
系统 MUST 提供可挂载到场景对象上的 Inspector 开关控制器，用于开发期直接开关日志通道 key 并按 key 文本规则批量筛选通道。该控制器 MUST 只操作统一日志过滤器，MUST NOT 新增第二套日志状态、MUST NOT 主动发出测试日志、MUST NOT 读取状态机内部对象。

#### Scenario: Inspector 开关同步到统一过滤器
- **GIVEN** 场景对象挂载了日志开关控制器
- **AND** 开发者在 Inspector 中关闭 `Locomotion.locomotion-phase-changed` 通道 key
- **WHEN** 控制器应用通道设置
- **THEN** `RuntimeDiagnosticLog.Filter` MUST 将该通道 key 标记为关闭
- **AND** 该开关 MUST NOT 改变 Locomotion owner、phase、active path 或运动命令

#### Scenario: Inspector 按文本规则批量筛选通道
- **GIVEN** 场景对象挂载了日志开关控制器
- **AND** 开发者输入通道 key 前缀、后缀或包含文本
- **WHEN** 控制器应用文本筛选
- **THEN** key 匹配的日志通道 MUST 被开启
- **AND** key 不匹配的日志通道 MUST 被关闭
- **AND** 匹配规则 MUST 不区分大小写

#### Scenario: Inspector 通道列表覆盖已知日志 key
- **WHEN** 日志系统观察或注册到新的通道 key
- **THEN** Inspector 开关控制器 MUST 能同步出对应通道项
- **AND** 缺失通道项 MUST NOT 静默导致该日志无法开关

#### Scenario: 编译宏仍是最终关闭开关
- **GIVEN** Player 构建符号不包含 `THIRDPERSON_DIAGNOSTIC_LOGS`
- **WHEN** 场景中保留日志开关控制器
- **THEN** 普通常规诊断日志 MUST 不产生 Unity Console 输出
- **AND** Inspector 通道设置 MUST NOT 绕过宏裁切或改变玩法行为

### Requirement: 状态机日志只读接入
系统 MUST 通过现有状态快照、phase、path、Action tracker snapshot 或仲裁结果输出状态机日志。日志系统 MUST NOT 成为第二状态权威，MUST NOT 读取 UnityHFSM 内部对象或 Unity 表现对象。

#### Scenario: FullBody 状态路径变化日志
- **GIVEN** FullBody 主调度入口已经生成 `FullBodyStateSnapshot` 或等价快照
- **WHEN** active path 发生变化
- **THEN** 日志系统 MUST 读取该快照输出旧 path、新 path、owner、Action state 和状态持续时间
- **AND** 日志系统 MUST NOT 调用状态机 transition API

#### Scenario: Locomotion 阶段变化日志
- **GIVEN** Locomotion 状态机或主调度入口已经输出当前 phase 和 active path
- **WHEN** phase 从 `MoveStart` 切到 `MoveLoop`
- **THEN** 日志系统 MUST 输出 Locomotion 分类日志
- **AND** 日志系统 MUST NOT 重新计算 Locomotion transition 条件

#### Scenario: 运行时对象边界
- **WHEN** 检查日志系统源码
- **THEN** 日志系统 MUST NOT 引用 Animancer state、Animator state、AnimationClip、CharacterController、InputAction、Cinemachine 实例、UnityHFSM 内部 state 对象或 BBB 运行时类型

### Requirement: Action 和 Dodge 诊断日志
系统 MUST 为 Action 第一版提供可读诊断日志，至少覆盖 Dodge 请求接受、拒绝、进行中完成和回到 Locomotion 的关键事件。

#### Scenario: Dodge 请求接受
- **GIVEN** 输入缓冲存在有效 Dodge 请求
- **AND** Action 仲裁接受该请求
- **WHEN** FullBody 主调度入口进入 Action.Dodge
- **THEN** 系统 MUST 输出 Action 或 FullBody 分类日志
- **AND** 日志 MUST 包含请求 step、目标 Action state、当前 owner 和 active path

#### Scenario: Dodge 请求拒绝
- **GIVEN** 输入缓冲存在 Dodge 请求
- **AND** Action 仲裁拒绝该请求
- **WHEN** FullBody 主调度入口保留 Locomotion owner
- **THEN** 系统 MUST 输出 Action 分类日志
- **AND** 日志 MUST 包含 reject reason、当前 Action state 和请求 step

#### Scenario: Dodge 完成回到 Locomotion
- **GIVEN** 当前 FullBody owner 为 Action.Dodge
- **WHEN** Dodge module 报告完成并回到 Locomotion
- **THEN** 系统 MUST 输出状态回退日志
- **AND** 日志 MUST 包含新 active path 和 Action state 清空结果

### Requirement: 日志系统测试和验证
系统 MUST 为运行时诊断日志提供自动测试、静态边界检查和手动验证步骤，证明日志系统可开关、可裁切、不会改变状态机行为。

#### Scenario: 自动测试覆盖过滤和格式
- **WHEN** 运行日志相关 EditMode 测试
- **THEN** 测试 MUST 覆盖分类过滤
- **AND** MUST 覆盖状态路径变化日志格式
- **AND** MUST 覆盖同一状态路径不会重复刷屏

#### Scenario: 自动测试覆盖状态机日志
- **WHEN** 运行 FullBody 和 Locomotion 日志接入测试
- **THEN** 测试 MUST 覆盖 Locomotion path 变化日志
- **AND** MUST 覆盖 Dodge accepted 日志
- **AND** MUST 覆盖 Dodge rejected 日志
- **AND** MUST 证明日志开关不改变 owner、phase 或 active path

#### Scenario: 手动验证方式
- **WHEN** 开发者在 Unity 中开启 `THIRDPERSON_DIAGNOSTIC_LOGS` 并进入 Play Mode
- **THEN** 普通 WASD MUST 能在 Console 中看到 Locomotion path 变化
- **AND** Dodge 输入 MUST 能看到 Action.Dodge 进入和退出日志
- **AND** 关闭宏并重新编译后，普通诊断日志 MUST 不再输出

#### Scenario: 手动验证 Inspector 通道开关
- **WHEN** 开发者在场景对象上挂载日志开关控制器并进入 Play Mode
- **THEN** Inspector MUST 能显示不同日志通道 key 的开关
- **AND** 关闭某个通道后，该通道的业务日志 MUST 不再输出
- **AND** 通过前缀、后缀或包含文本筛选后，只有匹配通道的业务日志恢复输出

### Requirement: 角色诊断 Adapter 边界
系统 MUST 将角色运行时核心产生的诊断事实和日志提交分离。状态机 runner、transition evaluator、timeline sampler、character frame pipeline 和 output runtime SHOULD 产出纯数据 trace 或调用窄 diagnostic port；实际 `RuntimeDiagnosticLogEvent` 格式化和 `RuntimeDiagnosticLog.Submit` MUST 由 diagnostic adapter 或等价外围模块承担。

#### Scenario: Core 只产出 trace
- **WHEN** 状态机 runner、condition evaluator 或 timeline sampler 需要诊断信息
- **THEN** 它 MUST 产出纯数据 trace 或填充 frame result trace
- **AND** MUST NOT 直接提交 `RuntimeDiagnosticLog`
- **AND** trace MUST NOT 包含 MonoBehaviour、Transform、CharacterController、Animancer state 或 InputAction

#### Scenario: Adapter 提交统一日志
- **WHEN** diagnostic adapter 接收到 frame、timeline、condition 或 snapshot trace
- **THEN** 它 MUST 格式化为稳定 `RuntimeDiagnosticLogEvent`
- **AND** MUST 通过统一 `RuntimeDiagnosticLog` 出口提交
- **AND** MUST 保留已有 event id 和 channel key 语义

#### Scenario: 日志不改变玩法
- **GIVEN** runtime diagnostic filter 关闭某个分类或通道
- **WHEN** 角色 frame pipeline 处理同一输入序列
- **THEN** active path、owner、input consume、motion execution 和 animation presentation MUST 与开启日志时一致
- **AND** diagnostics adapter MUST NOT 成为状态权威或控制流条件

#### Scenario: 测试可替换 sink
- **WHEN** EditMode 测试验证诊断链路
- **THEN** 测试 MUST 能使用 fake diagnostic sink 观察 trace/event
- **AND** MUST 不依赖 Unity Console 文本作为唯一断言来源

### Requirement: 诊断 trace 必须是纯观测数据
系统 MUST 将 runtime core 产生的诊断数据建模为纯观测 trace。Trace MUST 描述已经发生或已经计算出的事实，不得持有 Unity runtime object，不得拥有状态权威，也不得影响下一帧控制流。

#### Scenario: Trace 不保存 Unity 对象
- **WHEN** runner、pipeline、timeline sampler 或 evaluator 产出 diagnostic trace
- **THEN** trace MUST NOT 保存 `MonoBehaviour`
- **AND** MUST NOT 保存 `Transform`
- **AND** MUST NOT 保存 `CharacterController`
- **AND** MUST NOT 保存 Animancer runtime state 或 InputAction

#### Scenario: Trace 不反向驱动玩法
- **WHEN** diagnostic trace 被生成、过滤、丢弃或提交失败
- **THEN** 状态机 active path MUST 不受影响
- **AND** input consume MUST 不受影响
- **AND** motion execution 和 animation presentation MUST 不受影响

### Requirement: 诊断事件所有权必须唯一
系统 MUST 为每个角色 runtime diagnostic event family 指定唯一 adapter/formatter owner。迁移后同一个 event family MUST NOT 同时从 runtime core 和 diagnostic adapter 两处提交，避免重复日志和顺序歧义。

#### Scenario: Event family 只有一个 submit owner
- **WHEN** FullBody path、action accepted、timeline facts、condition probe 或 Locomotion phase event 被提交
- **THEN** 该 event family MUST 有唯一 adapter/formatter owner
- **AND** runtime core MUST NOT 提交同名 event
- **AND** tests MUST 能通过 fake sink 观察该 event family

#### Scenario: 旧 key 保持可搜索
- **WHEN** diagnostic adapter 格式化迁移后的 event
- **THEN** 旧 event id 和 channel key MUST 保持可搜索
- **AND** 若 payload shape 有必要调整，MUST 在 proposal 或 spec 中记录兼容映射

### Requirement: Timeline Facts 诊断由外围提交
Timeline facts、projected facts、target facts 和 transition facts trace 的日志提交 MUST 由 Character diagnostics adapter 或等价外围模块负责。状态机 runner MUST NOT 直接提交 runtime diagnostic log。

#### Scenario: 外围提交 current facts 日志
- **GIVEN** Character frame context 生成 current timeline facts
- **WHEN** diagnostics adapter 处理本帧 trace
- **THEN** 日志 MUST 包含 current facts 的 state id、source step、elapsed seconds、active window ids 和 active fact ids
- **AND** 日志 MUST 标识该 facts 来源为 current

#### Scenario: 外围提交 projected 和 target facts 日志
- **GIVEN** runner 在 transition evaluation 中生成 projected 或 target facts trace
- **WHEN** diagnostics adapter 处理 runner trace
- **THEN** 日志 MUST 能区分 projected facts 和 target facts
- **AND** 日志 MUST NOT 要求 runner 直接调用 `RuntimeDiagnosticLog.Submit`

