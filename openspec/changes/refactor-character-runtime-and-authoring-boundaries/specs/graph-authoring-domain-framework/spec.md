## ADDED Requirements

### Requirement: Pose作者校验报告必须直接支持错误定位

Pose 作者校验 MUST 在其输入责任边界生成统一结果，并为可定位的问题携带所属图、节点、端口或状态机元素身份。作者 UI MUST 消费该报告展示问题并导航，MUST NOT 为取得位置再次执行相同能力或状态机规则校验，也不得把同一问题重复计数。纯图作者合法性与依赖 Profile／Rig 的内容检查 MUST 按正式输入模式表达；缺少后者上下文不得让前者被跳过，也不得将仅通过作者检查显示为完整运行内容可用。

#### Scenario: 状态转移规则非法

- **WHEN** 一次校验发现某状态转移规则非法
- **THEN** 同一报告 MUST 包含其所属图和转移身份，UI MUST 能定位该转移并显示对应原因
- **AND** 定位 MUST NOT 再运行同一状态机规则检查或增加一条重复错误

#### Scenario: 只打开独立Pose作者图

- **WHEN** 作者从正式纯图入口打开资产且未提供 Profile／Rig
- **THEN** 系统 MUST 执行适用于该输入的作者合法性检查并如实标明检查范围
- **AND** MUST NOT 把该结果显示为角色运行资源已准备或已被 Session 采用

### Requirement: Pose保存必须支持正式作者上下文

Pose 保存 MUST 持久化当前正式图资产、所属图内容以及当前上下文实际拥有的待保存作者资源。纯图模式 MUST 不要求不存在的 Profile；带 Profile 模式 MUST 同时处理正式引用的可编辑参数资源。保存 MUST 沿唯一持久化操作执行，不得在窗口与调参入口重复维护保存协议，不得创建默认 Profile、备用资源或通过资产扫描猜测上下文。

C# 作者生成 MUST 将实际写入的各个 owner 在修改前纳入同一正式事务的快照、保存和失败回退集合，包括被修改的独立 Profile。只读引用不等于写入 owner。系统 MUST NOT 只保存输出图与 Definition 后将尚未保存的 Profile 修改报告为 Saved，也不得调用作者窗口的保存操作绕过生成事务。

#### Scenario: 保存纯图资产

- **WHEN** 作者从不带 Profile 的正式入口保存已修改的 Pose 图
- **THEN** 系统 MUST 保存该资产与所属图内容
- **AND** MUST NOT 访问不存在的 Profile、创建替代 Profile 或要求进入其他作者窗口

#### Scenario: 保存带Profile的作者修改

- **WHEN** 作者在正式 Profile 上下文保存图及所引用参数资源的修改
- **THEN** 唯一保存操作 MUST 覆盖这些已知 owner
- **AND** 保存成功 MUST NOT 被显示成运行 Session 已采用新内容

#### Scenario: 生成代码同时修改独立Profile

- **WHEN** 正式 C# 作者生成同时修改输出图和已引用的独立 Profile
- **THEN** 生成事务 MUST 在成功时保存所有实际写入 owner，失败时按同一事务回退它们
- **AND** MUST NOT 遗留未回退的 Profile 内存修改或在 Profile 未保存时报告全部 Saved

### Requirement: Pose作者操作必须如实表达实际结果

原生 Pose 作者界面 MUST 区分校验、保存与正式运行采用结果。只有执行对应操作并得到其正式结果时才能显示成功；仅执行 Validate 的操作 MUST NOT 命名为 Compile 或显示编译完成，也不得要求不存在的 Pose Projection Build。新建 State／Alias MUST 通过现有领域 mutation 与同一 Undo 语义处理状态、子图及布局，UI 只提供操作意图和选择。

#### Scenario: 作者执行校验

- **WHEN** 作者执行 Pose Validate 且检查通过
- **THEN** UI MUST 显示对应范围的校验结果
- **AND** MUST NOT 显示编译、内容发布或运行采用成功

#### Scenario: 新建状态后撤销

- **WHEN** 作者创建一个 State 及默认 Pose 子图后执行 Undo
- **THEN** 状态、子图和布局 MUST 遵循同一正式 mutation 的撤销语义
- **AND** MUST NOT 留下由窗口单独写入的状态副本或悬空作者对象
