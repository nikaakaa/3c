## ADDED Requirements
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
系统 MUST 为 FullBody Action 第一版提供可读诊断日志，至少覆盖 Dodge 请求接受、拒绝、进行中完成和回到 Locomotion 的关键事件。

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
