# fullbody-hfsm-tree-data Specification

## Purpose
定义 FullBody HFSM 树数据资产的中心化归属、只读预览、默认树结构和禁止分裂 fallback 的规则。
## Requirements
### Requirement: FullBody HFSM 中心树资产
系统 MUST 提供一个 `FullBodyHfsmTreeDefinitionSO` 或等价中心资产，用于表达 FullBody 主行为域的完整 HFSM 层级树。该资产 MUST 只负责树拓扑和节点绑定，不得接管 Locomotion transition、Dodge 业务、动作动画、动作位移或打断策略配置。

#### Scenario: Action 是 FullBody 子域
- **WHEN** 设计者检查 FullBody HFSM 树资产
- **THEN** `Action` MUST 作为 `FullBody` 根下的子分支存在
- **AND** `Action` 分支 MUST NOT 表达一个与 `FullBody` 并列的独立状态机权威
- **AND** Action 节点 MUST 只绑定稳定 `ActionStateId`
- **AND** Action 节点 MUST NOT 自行提交 base layer 动画或平面位移

#### Scenario: 一个资产表达当前树
- **WHEN** 设计者检查当前 FullBody HFSM 树定义
- **THEN** 系统 MUST 能通过一个中心资产看到 `FullBody` 根节点
- **AND** MUST 能看到 `Locomotion` 分支
- **AND** MUST 能看到 `Action` 分支
- **AND** MUST 能看到 `Locomotion/Idle`、`Locomotion/MoveStart`、`Locomotion/MoveLoop`、`Locomotion/MoveStop`
- **AND** MUST 能看到 `Action/Dodge`

#### Scenario: 树资产不接管业务配置
- **WHEN** 设计者配置 `Action.Dodge`
- **THEN** 树节点 MUST 只绑定稳定 `ActionStateId`
- **AND** Dodge motion config MUST 继续归属 FullBody Action 逻辑配置入口或等价动作逻辑配置
- **AND** Action interrupt policy MUST 继续归属 FullBody Action 逻辑配置入口或等价打断策略配置
- **AND** Action animation profile MUST 通过动作动画绑定集或等价动画配置入口解析
- **AND** 树节点 MUST NOT 直接引用动作动画绑定集或动作动画 Profile
- **AND** Locomotion transition 规则 MUST 继续归属 Locomotion 局部状态图配置

### Requirement: 内嵌 HFSM 节点定义
系统 MUST 提供内嵌可序列化的 `FullBodyHfsmNodeDefinition` 或等价节点数据。节点 MUST 能表达稳定节点 ID、路径段、节点类型、可选 Locomotion phase 绑定、可选 Action state 绑定和子节点列表。

#### Scenario: 节点字段支持树编译
- **WHEN** 编译器读取一个节点
- **THEN** 节点 MUST 提供稳定 node id
- **AND** MUST 提供 path segment
- **AND** MUST 提供 node kind
- **AND** MAY 提供 `BasicMovementPhase` 绑定
- **AND** MAY 提供 `ActionStateId` 绑定
- **AND** MAY 提供子节点列表

#### Scenario: 节点不是独立 state 资产
- **WHEN** 设计者检查 FullBody HFSM 树资产
- **THEN** Locomotion phase 节点 MUST 作为树资产内嵌数据存在
- **AND** Action 节点 MUST 作为树资产内嵌数据存在
- **AND** 系统 MUST NOT 要求每个 HFSM state 都创建一个独立 ScriptableObject 资产

### Requirement: FullBody HFSM 树编译和校验
系统 MUST 提供树定义编译器和校验器，将可序列化树定义转换为运行时只读树描述。校验 MUST 在运行时构建 HFSM 前发现重复 ID、重复路径、非法绑定和当前默认树缺口。

#### Scenario: 校验重复和缺失
- **WHEN** 树定义包含重复 node id、重复完整路径或重复绑定
- **THEN** 校验结果 MUST 报告错误
- **AND** builder MUST NOT 静默使用该非法树

#### Scenario: 校验当前默认结构
- **WHEN** 校验当前默认 FullBody 树资产
- **THEN** 校验 MUST 确认 Root 存在
- **AND** MUST 确认 Locomotion 分支存在
- **AND** MUST 确认 Action 分支存在
- **AND** MUST 确认四个 `BasicMovementPhase` 叶子存在且不重复
- **AND** MUST 确认 `Action.Dodge` 叶子存在且位于 `FullBody/Action` 分支

#### Scenario: 编译结果可查询
- **WHEN** 树定义编译成功
- **THEN** 编译结果 MUST 能按 node id 查询节点
- **AND** MUST 能按完整路径查询节点
- **AND** MUST 能按 `BasicMovementPhase` 查询 Locomotion 叶子
- **AND** MUST 能按 `ActionStateId` 查询 Action 叶子

### Requirement: 路径从节点树推导
系统 MUST 从编译后的父子节点关系推导 FullBody 状态路径。运行时 snapshot、pending transition 诊断和测试期望 MUST 共享该路径来源，不得在多个类中手写拼接 `/FullBody/Action/Dodge` 等字符串。

#### Scenario: Locomotion 路径由节点计算
- **GIVEN** 编译树包含 `FullBody/Locomotion/MoveLoop`
- **WHEN** active leaf 绑定到 `BasicMovementPhase.MoveLoop`
- **THEN** snapshot active path MUST 为 `/FullBody/Locomotion/MoveLoop`
- **AND** 该路径 MUST 来自 compiled node path

#### Scenario: Action 路径由节点计算
- **GIVEN** 编译树包含绑定 `ActionStateIds.Dodge` 的 Dodge 节点
- **WHEN** active Action state 为 `Action.Dodge`
- **THEN** snapshot active path MUST 为 `/FullBody/Action/Dodge`
- **AND** 该路径 MUST 来自 compiled node path

### Requirement: Builder 消费中心树数据
系统 MUST 让 `FullBodyHfsmStateTreeBuilder` 或等价 builder 消费编译后的 FullBody 树定义来创建 HFSM。builder MUST NOT 长期硬编码 Locomotion、Action 或 Dodge 节点结构。

#### Scenario: 从树定义构建当前 HFSM
- **GIVEN** 当前默认树资产编译成功
- **WHEN** builder 创建 FullBody HFSM
- **THEN** 创建出的 HFSM MUST 包含 Root、Locomotion 和 Action 层级
- **AND** Locomotion 子状态 MUST 来自树定义中的 phase 节点
- **AND** Action 子状态 MUST 来自树定义中的 action 节点

#### Scenario: 不保留分裂 fallback
- **WHEN** prefab 配置了有效 FullBody HFSM 树资产
- **THEN** 运行时 MUST 使用该资产编译结果构建状态树
- **AND** MUST NOT 同时维护另一套硬编码 FullBody/Locomotion/Action/Dodge 结构作为长期运行路径

### Requirement: Owner 从编译节点绑定推导
系统 MUST 从 active compiled node 的节点类型和绑定推导 FullBody owner。Action owner MUST 来自 active Action 节点绑定的 `ActionStateId`，Locomotion owner MUST 来自 active Locomotion phase 节点或 Locomotion 分支。

#### Scenario: Locomotion owner 推导
- **WHEN** active leaf 是绑定 `BasicMovementPhase.MoveStart` 的 Locomotion phase 节点
- **THEN** FullBody owner MUST 为 Locomotion
- **AND** snapshot Locomotion phase MUST 为 `MoveStart`

#### Scenario: Action owner 推导
- **WHEN** active leaf 是绑定 `ActionStateIds.Dodge` 的 Action 节点
- **THEN** FullBody owner MUST 为 Action
- **AND** snapshot Action state MUST 为 `Action.Dodge`
- **AND** owner 推导 MUST NOT 依赖 `if Action branch then Dodge` 这类固定假设

### Requirement: 运行时接入中心树资产
系统 MUST 让当前角色 FullBody 主调度入口引用有效 FullBody HFSM 树资产。缺失或非法树资产 MUST 产生明确校验错误或禁用 FullBody HFSM 构建，不得静默退回未审批的第二状态路径。

#### Scenario: Prefab 引用默认树资产
- **WHEN** 检查当前主角色 prefab 或等价角色组装入口
- **THEN** FullBody 主调度入口 MUST 引用默认 FullBody HFSM 树资产
- **AND** 该资产 MUST 表达当前已实现的 Locomotion 和 Dodge 树结构

#### Scenario: 非法资产不静默运行
- **GIVEN** FullBody HFSM 树资产缺失、校验失败或无法编译
- **WHEN** FullBody 主调度入口初始化
- **THEN** 系统 MUST 报告明确错误
- **AND** MUST NOT 通过隐藏硬编码树继续运行为另一条长期路径

### Requirement: 只读树形编辑器预览
系统 MUST 提供只读 Inspector 或 EditorWindow 预览当前 FullBody HFSM 树资产。该预览 MUST 展示节点层级、完整路径、节点类型和绑定，并复用同一套校验结果。

#### Scenario: 只读预览当前树
- **WHEN** 设计者打开 FullBody HFSM 树预览
- **THEN** 预览 MUST 以树形结构显示 Root、Locomotion、Action 和 Dodge
- **AND** MUST 显示每个节点的完整路径
- **AND** MUST 显示 Locomotion phase 或 Action state 绑定
- **AND** MUST 显示校验错误和 warning

#### Scenario: 第一版不写树
- **WHEN** 设计者使用本 change 提供的编辑器能力
- **THEN** 系统 MUST NOT 提供拖拽改树、图形连线、节点模板或动作 timeline 写入能力
- **AND** 可写编辑能力 MUST 另开 OpenSpec 后再实现

### Requirement: 可测试和可验证
系统 MUST 为 FullBody HFSM 中心树数据提供 EditMode 自动测试、静态边界检查和用户手动验证步骤。测试 MUST 覆盖树结构、重复 id、重复路径、phase/action 绑定、路径解析、builder 接入和只读预览的数据来源。

#### Scenario: 自动测试覆盖数据和路径
- **WHEN** 运行 FullBody HFSM tree data EditMode 测试
- **THEN** 测试 MUST 覆盖默认树结构
- **AND** MUST 覆盖重复 node id 报错
- **AND** MUST 覆盖重复路径报错
- **AND** MUST 覆盖重复 Locomotion phase 绑定报错
- **AND** MUST 覆盖重复 Action state 绑定报错
- **AND** MUST 覆盖 `/FullBody/Locomotion/MoveLoop` 路径解析
- **AND** MUST 覆盖 `/FullBody/Action/Dodge` 路径解析

#### Scenario: 自动测试覆盖运行时接入
- **WHEN** 运行 FullBody HFSM runtime EditMode 测试
- **THEN** 测试 MUST 覆盖 builder 从默认树资产或等价测试树构建 HFSM
- **AND** MUST 覆盖 Action.Dodge active 时 owner 来自 compiled Action 节点绑定
- **AND** MUST 覆盖 Locomotion phase active 时路径来自 compiled Locomotion 节点绑定

#### Scenario: 手动验证树可见
- **WHEN** 用户在 Unity Editor 中选择默认 FullBody HFSM 树资产
- **THEN** 用户 MUST 能看到只读树形预览
- **AND** 预览 MUST 显示 `/FullBody/Locomotion/Idle`
- **AND** 预览 MUST 显示 `/FullBody/Locomotion/MoveLoop`
- **AND** 预览 MUST 显示 `/FullBody/Action/Dodge`
