## ADDED Requirements

### Requirement: BaseTreeWindow 页面栈
系统 MUST 在 Taco 编辑器窗口中维护 editor-only 页面栈，用于表达当前窗口这次编辑会话的 Graph 访问路径。页面栈 MUST 从当前打开的 `BaseTree` 开始，而不是从项目全局根节点反推。

#### Scenario: 直接打开 Graph 资产
- **WHEN** 用户直接打开一个 `BaseTree` 或其子类资产
- **THEN** 编辑器 MUST 将该 Graph 作为当前窗口页面栈根
- **AND** 页面栈 MUST NOT 保留之前无关的下钻路径

#### Scenario: 从节点下钻
- **WHEN** 用户从节点的 Graph 引用打开子 Graph
- **THEN** 编辑器 MUST 将子 Graph push 到当前窗口页面栈
- **AND** 页面栈条目 MUST 记录来源图、来源节点 GUID 和引用 key
- **AND** 编辑器 MUST 显示被引用 Graph

#### Scenario: 复用 Graph
- **WHEN** 多个节点引用同一个 Graph
- **THEN** 页面栈 MUST 记录用户本次从哪个节点进入
- **AND** 系统 MUST NOT 尝试把该 Graph 固定为某个唯一父节点的子图

### Requirement: 页面栈返回与 breadcrumb
系统 MUST 在 Taco 编辑器窗口中提供返回按钮和 breadcrumb，使用户能从当前下钻页面返回上层页面或跳回任一已打开层级。

#### Scenario: 返回上一页
- **WHEN** 页面栈中存在上一页
- **AND** 用户点击 Back
- **THEN** 编辑器 MUST 返回上一页 Graph
- **AND** 当前页 MUST 从页面栈中移除

#### Scenario: 根页面不能返回
- **WHEN** 页面栈只有根页面
- **THEN** Back 按钮 MUST 处于禁用状态

#### Scenario: 点击 breadcrumb 中间层
- **WHEN** 用户点击 breadcrumb 中的非当前页面
- **THEN** 编辑器 MUST 切换到该页面对应的 Graph
- **AND** 被点击页面之后的页面 MUST 从页面栈移除

#### Scenario: breadcrumb 显示名
- **WHEN** 页面是直接打开的根 Graph
- **THEN** breadcrumb MUST 显示该 Graph 资产名
- **WHEN** 页面是从节点下钻进入的 Graph
- **THEN** breadcrumb SHOULD 优先显示来源节点的显示名
- **AND** 来源节点不可用时 MUST 显示 Graph 资产名

### Requirement: Graph 下钻不污染资产数据
系统 MUST 将页面栈视为编辑器会话状态。页面栈 MUST NOT 序列化到 `BaseTree`、`BaseNode`、`NodeModule` 或 Graph 引用字段中，也 MUST NOT 改变 runtime 解释链路。

#### Scenario: 保存 Graph
- **WHEN** 用户保存或 dirty 当前 Graph
- **THEN** 页面栈数据 MUST NOT 写入 Graph 资产
- **AND** Graph 的节点、边、属性边和模块字段 MUST 保持原有正式数据模型

#### Scenario: 关闭窗口
- **WHEN** 用户关闭 Taco 编辑器窗口
- **THEN** 页面栈 MAY 被丢弃
- **AND** 下次直接打开 Graph 时 MUST 从该 Graph 重新作为栈根开始

#### Scenario: 运行时
- **WHEN** 游戏运行时 tick `RunnableTree`、`StateMachineGraph` 或 `TimelineNode`
- **THEN** 页面栈 MUST NOT 参与 runtime 决策
- **AND** 系统 MUST NOT 因页面栈新增 runtime 字段

### Requirement: 节点 Graph 引用打开命令
系统 MUST 让节点双击和右键 `Open Reference` 命令使用当前窗口页面栈打开 Graph 引用。直接打开资产的行为 MUST 保持为打开该资产本身。

#### Scenario: 双击节点标题
- **WHEN** 用户双击一个拥有 Graph 引用的节点标题
- **THEN** 编辑器 MUST 打开第一个可用 Graph 引用
- **AND** 该打开操作 MUST push 到当前窗口页面栈

#### Scenario: 右键打开指定引用
- **WHEN** 用户通过右键菜单选择 `Open Reference/<label>`
- **THEN** 编辑器 MUST 打开该菜单项对应的 Graph 引用
- **AND** 该打开操作 MUST push 到当前窗口页面栈

#### Scenario: 引用为空
- **WHEN** 节点的 Graph 引用为空
- **THEN** 对应打开命令 MUST 处于禁用状态或不执行页面跳转
