## MODIFIED Requirements
### Requirement: FullBody 分层 HFSM 状态树
系统 MUST 提供一个 FullBody 主行为域的显式分层 HFSM 状态树，用于表达角色 base layer 的主状态路径。该状态树 MUST 建立在现有 FullBody Action 框架之上，MUST NOT 新增第二套角色控制器、第二套基础移动状态机或 BBB 运行时依赖。`FullBody/Locomotion` 和 `FullBody/Action` MAY 作为可读路径分组存在，但它们 MUST NOT 被实现为互斥 owner 权威；状态节点能力 MUST 由模块组合决定。

#### Scenario: 状态树包含可读分组但不形成双权威
- **WHEN** FullBody HFSM 初始化
- **THEN** 状态树 MUST 包含 `FullBody/Locomotion` 分支
- **AND** 状态树 MUST 包含 `FullBody/Action` 分支
- **AND** 第一版 Action 分支 MUST 至少能表达 `Action.Dodge`
- **AND** `FullBody/Action` MUST 是同一状态树内的可读路径分组，不得成为与 Locomotion 并列的独立状态机权威

#### Scenario: 不新增第二控制路径
- **WHEN** FullBody HFSM 状态树接入运行时
- **THEN** 系统 MUST 继续通过现有 FullBody 主调度入口或等价 coordinator 提交运动和动画命令
- **AND** MUST NOT 新增绕过该入口的 per-action controller
- **AND** MUST NOT 复制 `BBBCharacterController`、`PlayerStateRegistry` 或 `PlayerBaseState`

#### Scenario: 节点能力来自模块
- **WHEN** 设计者查看状态树节点
- **THEN** 分组节点 MUST 只表达路径、标签和子节点关系
- **AND** MoveLoop、TurnBack、Dodge 等叶子节点 MUST 通过各自模块表达能力
- **AND** Inspector 或等价配置入口 MUST NOT 强迫每个节点填写同一套 motion / animation / action 字段

### Requirement: Action.Dodge 子状态映射
系统 MUST 将现有 `Action.Dodge` 接入 FullBody HFSM 的 Action 路径，并通过状态节点模块表达 Dodge 的请求、位移、动画、输入消费和连续 Dodge 规则。`Action.Dodge` 的进入许可 MUST 继续由 Action 仲裁或等价请求准入层决定；状态树 transition MUST 读取准入后的纯数据请求事实。

#### Scenario: Dodge accepted 进入 Action 路径
- **GIVEN** 输入缓冲存在有效 Dodge 请求
- **AND** Action 仲裁接受该请求
- **WHEN** FullBody HFSM 处理本帧
- **THEN** FullBody 状态路径 MUST 进入 `/FullBody/Action/Dodge` 或等价层级路径
- **AND** 当前节点 MUST 具备 Dodge 动作请求模块或等价能力
- **AND** 当前节点 MUST 具备动作位移和动作动画模块或等价能力

#### Scenario: Dodge active 期间由输出通道压制基础移动
- **GIVEN** 当前 FullBody 状态路径为 `/FullBody/Action/Dodge`
- **WHEN** 本帧处理运动和动画输出
- **THEN** 系统 MUST 提交 Dodge 动作运动或动画命令
- **AND** 基础移动平面位移输出 MUST 由输出通道优先级、占用策略或等价模块规则压制
- **AND** 系统 MUST NOT 仅依赖 `Owner.IsAction` 分支压制 Locomotion 输出

#### Scenario: Dodge 完成回到 Locomotion 路径
- **GIVEN** 当前 FullBody 状态路径为 `/FullBody/Action/Dodge`
- **WHEN** Dodge 模块输出完成事实且 transition 条件满足
- **THEN** FullBody HFSM MUST 退出 Action.Dodge
- **AND** 状态路径 MUST 回到 `FullBody/Locomotion` 子树
- **AND** action runtime facts MUST 从模块输出或状态快照派生为空 action state
