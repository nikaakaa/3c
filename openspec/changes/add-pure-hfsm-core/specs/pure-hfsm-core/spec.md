## ADDED Requirements
### Requirement: 纯 HFSM Core 边界
系统 MUST 在 `Assets/Scripts/FSM/Core/HFSM.cs` 提供纯 HFSM 框架，并且 MUST NOT 在该框架中依赖角色移动、Animancer、Cinemachine、具体输入系统或相机系统。

#### Scenario: Core 不接业务系统
- **WHEN** 实现 `HFSM.cs`
- **THEN** 文件 MUST NOT 引用 Animancer
- **AND** MUST NOT 引用 Cinemachine
- **AND** MUST NOT 引用 CharacterController
- **AND** MUST NOT 引用当前角色移动、动画或相机命名空间

#### Scenario: 旧 demo 不被强制迁移
- **WHEN** 新 HFSM Core 落地
- **THEN** 系统 MUST 保留 `SimpleHFSM.cs` 的现有 demo 路径
- **AND** MUST NOT 要求 WASD 或 BBB 角色系统立即迁移到新 HFSM Core

### Requirement: 状态树与路径事实
系统 MUST 使用组合状态和叶子状态组成单根状态树，并以 root 到当前叶子的完整路径作为运行时状态事实。

#### Scenario: 初始化进入默认叶子
- **WHEN** 状态机以 root 状态启动
- **THEN** 系统 MUST 从 root 进入每一层 initial 子状态
- **AND** MUST 最终停在一个叶子状态
- **AND** `CurrentPath` MUST 表达 root 到当前叶子的完整路径

#### Scenario: 当前路径可调试
- **WHEN** 当前状态为 `Root/Alive/Locomotion/MoveLoop`
- **THEN** 调试路径输出 MUST 包含完整层级
- **AND** MUST NOT 只输出叶子状态名

### Requirement: LCA 跨层转移
系统 MUST 支持同一棵状态树内的跨层转移，并 MUST 使用最近公共父节点算法执行退出和进入路径。

#### Scenario: 保留公共祖先
- **GIVEN** 当前路径为 `Root/A/B/C`
- **AND** 目标路径为 `Root/A/D/E`
- **WHEN** 执行跨层转移
- **THEN** 系统 MUST 退出 `C` 和 `B`
- **AND** MUST NOT 退出 `Root` 或 `A`
- **AND** MUST 进入 `D` 和 `E`

#### Scenario: 非同树 target 被拒绝
- **WHEN** 转移目标不属于当前状态树
- **THEN** 系统 MUST 拒绝该转移
- **AND** 当前路径 MUST 保持不变
- **AND** MUST 提供可诊断的失败原因

### Requirement: 确定性 Transition 解析
系统 MUST 以确定性规则选择可执行 transition，规则顺序为 priority、更深作用域、注册顺序。

#### Scenario: priority 高者优先
- **GIVEN** 同一帧存在多个 guard 成立的 transition
- **WHEN** 一个 transition 的 priority 更高
- **THEN** 系统 MUST 选择 priority 更高的 transition

#### Scenario: 深层作用域优先
- **GIVEN** 多个 transition priority 相同
- **WHEN** 一个 transition 定义在更接近当前叶子的组合状态上
- **THEN** 系统 MUST 选择更深作用域的 transition

#### Scenario: 注册顺序兜底
- **GIVEN** 多个 transition priority 和作用域深度都相同
- **WHEN** 需要选择唯一 transition
- **THEN** 系统 MUST 选择更早注册的 transition

### Requirement: 转移准入和拒绝
系统 MUST 支持状态级 `CanExit` 与 `CanEnter`，并在准入失败时拒绝转移且不改变当前路径。

#### Scenario: CanExit 拒绝
- **WHEN** 当前路径中需要退出的任一状态拒绝 `CanExit`
- **THEN** 系统 MUST 拒绝该转移
- **AND** 当前路径 MUST 保持不变
- **AND** MUST 发出转移拒绝结果或事件

#### Scenario: CanEnter 拒绝
- **WHEN** 目标路径中需要进入的任一状态拒绝 `CanEnter`
- **THEN** 系统 MUST 拒绝该转移
- **AND** 当前路径 MUST 保持不变
- **AND** MUST 发出转移拒绝结果或事件

### Requirement: 重入安全与请求队列
系统 MUST 在 Enter、Exit 或 transition action 执行期间防止递归切换，并 MUST 将期间产生的新切换请求排队到当前切换之后处理。

#### Scenario: Enter 中请求切换
- **WHEN** 状态的 Enter 回调中请求另一次转移
- **THEN** 系统 MUST NOT 递归执行第二次转移
- **AND** MUST 在当前转移完成后再处理该请求

#### Scenario: 队列请求保持顺序
- **WHEN** 同一执行阶段产生多个转移请求
- **THEN** 系统 MUST 按请求入队顺序处理
- **AND** MUST 为防止无限循环提供最大处理步数保护

### Requirement: 历史恢复
系统 MUST 支持组合状态的浅历史和深历史恢复策略。

#### Scenario: 浅历史恢复直接子状态
- **GIVEN** 组合状态启用 shallow history
- **AND** 退出前直接子状态为 `Move`
- **WHEN** 再次进入该组合状态
- **THEN** 系统 MUST 恢复到直接子状态 `Move`
- **AND** `Move` 内部 MUST 按自身 initial 或 history 策略继续进入

#### Scenario: 深历史恢复完整叶子路径
- **GIVEN** 组合状态启用 deep history
- **AND** 退出前叶子路径为 `Locomotion/Move/Run`
- **WHEN** 再次进入 `Locomotion`
- **THEN** 系统 MUST 恢复到 `Locomotion/Move/Run`

### Requirement: 下推栈 Push/Pop
系统 MUST 支持 pushdown stack，用于保存当前路径、切入临时目标状态，并在 pop 时恢复先前路径。

#### Scenario: Push 保存当前路径
- **GIVEN** 当前路径为 `Root/Alive/Locomotion/MoveLoop`
- **WHEN** Push 到 `Root/Menu/Pause`
- **THEN** 系统 MUST 保存原当前路径到栈
- **AND** MUST 切换到 `Root/Menu/Pause`

#### Scenario: Pop 恢复路径
- **GIVEN** push stack 顶部保存 `Root/Alive/Locomotion/MoveLoop`
- **WHEN** 执行 Pop
- **THEN** 系统 MUST 切回 `Root/Alive/Locomotion/MoveLoop`
- **AND** MUST 移除该栈顶记录

#### Scenario: 空栈 Pop 失败
- **WHEN** push stack 为空并执行 Pop
- **THEN** 系统 MUST 返回失败结果
- **AND** 当前路径 MUST 保持不变

### Requirement: 事件队列驱动
系统 MUST 支持框架级事件队列，使外部可以发送事件并由 transition guard 在 Tick 中按顺序消费。

#### Scenario: 事件按顺序消费
- **WHEN** 外部依次发送事件 `A` 和 `B`
- **THEN** 状态机 Tick MUST 先处理 `A`
- **AND** MUST 后处理 `B`

#### Scenario: guard 可读取当前事件
- **WHEN** transition guard 需要判断当前事件名或 payload
- **THEN** 系统 MUST 向 guard 提供当前事件上下文
- **AND** MUST NOT 要求 guard 直接读取 Unity 输入系统

### Requirement: 构建校验
系统 MUST 在构建状态树时校验结构合法性，并在发现非法状态树时提供清晰错误。

#### Scenario: 缺失 initial
- **WHEN** 组合状态包含子状态但没有 initial
- **THEN** 构建 MUST 失败或返回明确错误

#### Scenario: 重复挂载
- **WHEN** 同一个状态实例被挂载到多个父节点
- **THEN** 构建 MUST 失败或返回明确错误

#### Scenario: transition target 非法
- **WHEN** transition 的 from 或 to 不属于当前状态树
- **THEN** 构建 MUST 失败或返回明确错误

### Requirement: 调试快照
系统 MUST 提供运行时调试信息和可恢复快照，用于定位状态路径、history 和 push stack。

#### Scenario: 快照包含核心运行时信息
- **WHEN** 请求状态机快照
- **THEN** 快照 MUST 包含当前路径
- **AND** MUST 包含上一条转移结果
- **AND** MUST 包含 history 信息
- **AND** MUST 包含 push stack 信息

#### Scenario: 非法快照恢复失败
- **WHEN** 从快照恢复的路径不存在于当前状态树
- **THEN** 恢复 MUST 失败
- **AND** 当前路径 MUST 保持不变
