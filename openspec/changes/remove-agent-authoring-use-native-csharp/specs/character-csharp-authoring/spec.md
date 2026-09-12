## Purpose

定义 AI 编写 C# 与人工编辑共用正式角色作者 API 的业务合同。Unity 资产保存唯一内容，Graph、Timeline、Presentation 与编译器负责各自规则；完整删除 Agent 目录包、专属校验和 MCP 中间层，保留作者编辑、稳定引用与明确保存结果。

## ADDED Requirements

### Requirement: C#与人工编辑必须修改同一正式作者资产

系统 MUST允许通过明确的Editor C#调用创建、配置、连接和删除正式角色作者内容，并与人工Graph、Timeline及Profile编辑器共用同一业务API。Unity作者资产 MUST是唯一内容来源，人工编辑 MUST继续可用；C#文件 MUST不成为需要资产持续匹配的全量内容副本，也不得建立双向代码生成、同步或后台重建。

#### Scenario: AI创建后作者微调

- **WHEN** C#创建并保存一个合法技能及其Timeline，作者随后在编辑器调整片段时间
- **THEN** 两次修改 MUST保存在同一正式资产与owner中
- **AND** 下一次C#操作 MUST以实际资产为输入，不因旧源码未变而还原作者调整

#### Scenario: 删除创建语句

- **WHEN** 开发者从C#删除一条过去执行过的创建语句
- **THEN** 系统 MUST不自动删除已保存资产
- **AND** 内容删除 MUST由后续明确的正式删除操作表达

### Requirement: Agent authoring协议与工具必须完整退役

系统 MUST删除Agent authoring目录包、Snapshot、schema、导出、对账、hash同步、专属Mutation/Session/Validator/Report、Controller Window及五个BTSMTL authoring MCP。正式写入 MUST不依赖该协议，不保留兼容包、工具别名、空facade、转发窗口、Agent专用运行时规则表或改名的同类框架。其他系统和插件内部序列化 MUST不因为本次退役而被删除。

#### Scenario: 调用退役工具

- **WHEN** 调用方请求任一旧BTSMTL authoring生命周期工具
- **THEN** 该工具 MUST不可用
- **AND** 系统 MUST不转成隐藏的C#执行、JSON迁移或备用入口

#### Scenario: 正式API独立工作

- **WHEN** 项目不存在Agent authoring协议实现和工作包
- **THEN** 正式Graph、Timeline和Presentation人工及C#写入 MUST仍能使用各自业务校验与保存能力

### Requirement: 业务校验必须属于正式业务模块

节点和连接规则 MUST由正式Graph能力与写入边界检查；Timeline配置 MUST由正式Timeline规则检查；表现资源与Pose拓扑 MUST由对应表现规则检查；整角色编译合法性 MUST由正式编译链检查。所有调用者 MUST共用这些规则，错误 MUST能定位到实际资产和业务对象。协议专用校验与重复业务校验 MUST随Agent删除；仅在正式模块真实缺少时补齐对应规则，不得建立中央Agent替代Validator。

#### Scenario: 代码连接不兼容端口

- **WHEN** C#与人工分别尝试连接同一组类型不兼容的端口
- **THEN** 正式Graph入口 MUST按同一规则拒绝两次操作
- **AND** MUST不依赖Agent preflight才能发现错误

#### Scenario: Agent代码只转发编译诊断

- **WHEN** 作者请求检查角色能否生成合法运行产物
- **THEN** 诊断入口 MUST直接复用正式编译链并返回对应错误
- **AND** MUST不先创建Document、Agent报告或第二套编译规则

### Requirement: C#输入必须是正式类型和明确对象

作者C# MUST通过正式类型、强类型参数、实际端口、稳定身份和资源对象表达修改。入口 MUST使用精确Definition与实际owner，不通过选择状态、同名资源或目录猜测目标。缺失、类型错误或不属于目标范围的引用 MUST明确报错，不建立默认资产或猜测替代值。JSON字段字典、私有序列化路径和YAML修改 MUST不成为公共作者调用协议。

#### Scenario: Timeline引用错误资源类型

- **WHEN** 调用代码向一个片段提交类型不匹配的资源
- **THEN** 类型系统或该片段正式业务入口 MUST拒绝
- **AND** MUST不转换成无类型JSON后尝试写内部字段

### Requirement: 人工Timeline与C#必须使用同一正式Timeline内容

Timeline的人工界面和C#创建 MUST编辑同一正式Timeline数据、轨道、片段、分节和资源绑定，遵守相同的帧域、类型及owner约束。界面代理 MUST只服务显示和交互，不得作为第二持久数据源或C#生成中转。新增片段的人工参数提交与C#配置 MUST共用强类型业务接口，不得在其中保留authoring JSON适配。

#### Scenario: C#新增合法轨道与片段

- **WHEN** C#在明确Timeline内创建合法轨道和资源片段并保存
- **THEN** 人工Timeline编辑器 MUST显示该正式内容并允许继续编辑
- **AND** MUST不需要先创建或保存界面代理

### Requirement: 更新与复制必须遵守稳定身份语义

原位更新、改名、排序 MUST保留对象的稳定作者身份与正式资产引用。复制 MUST生成新身份。直接新增操作 MUST明确返回新对象，不保证任意C#代码重复执行幂等；针对固定身份的创建操作遇到已存在目标 MUST明确处理或报告，不因同名查找建立重复或绑定错误对象。系统 MUST不通过全量清空重建、显示名、数组索引或持久local identity映射实现重试。

#### Scenario: 修改并重排片段

- **WHEN** C#配置同一片段后改变其顺序
- **THEN** 片段身份及合法消费者引用 MUST保持不变

#### Scenario: 重复执行固定身份创建

- **WHEN** 某创建操作的明确owner和稳定身份已存在
- **THEN** 操作 MUST明确报告已存在或只执行其声明的更新行为
- **AND** MUST不再随机新增同一业务目标

### Requirement: 删除必须明确范围并维护正式引用

删除 MUST显式指定对象及授权修改范围，通过正式领域API完成。正式引用、私有owner、系统节点和共享资产约束 MUST继续生效；不能删除仍被范围外内容引用的共享对象。旧期望与当前作者改动冲突时 MUST报告冲突，不自动覆盖。无消费者的私有对象 MUST沿所属领域的正式清理链删除。

#### Scenario: 删除一个共享Macro引用

- **WHEN** 某技能解除共享Macro引用但其他技能仍使用该资产
- **THEN** 正式删除操作 MUST仅移除本次明确的引用
- **AND** MUST保留共享资产及其他技能关系

### Requirement: 编辑事务必须按实际业务操作确定范围

单领域修改 MUST使用既有领域Undo、dirty、序列化和保存边界；一次跨owner业务修改 MUST明确其相关对象并组合已有编辑事务，避免内层提前保存部分结果。普通单对象操作 MUST不强制进入全角色扫描、快照或同步事务。执行与保存失败 MUST提供实际影响和恢复结果，不得把内存Undo宣称为多文件磁盘原子提交，也不得为任意C#宣称无副作用dry-run。

#### Scenario: 单图增加节点

- **WHEN** 作者只在一个明确Graph内增加合法节点
- **THEN** 操作 MUST进入该Graph正式编辑边界
- **AND** MUST不要求导出整个角色、计算目录包hash或收集无关资源

#### Scenario: 跨owner修改失败

- **WHEN** 同一次业务修改涉及多个owner且其中一步失败
- **THEN** 正式编辑入口 MUST按其事务恢复已登记的修改并报告实际结果
- **AND** 若磁盘保存或恢复未完成 MUST明确指出受影响资产，不得报告完整成功

### Requirement: 删除协议不得扩大作者业务权限

已有Skill、FSM、Macro、Timeline、Blackboard、Action target、Profile、Pose、Linked Pose与注册Curve的正式编辑能力 MUST继续由对应业务入口提供。删除协议 MUST不自动授权编辑生成的Foot Analysis、Rig/Body Motion底层合同、创建素材或改写Program/Projection。Foot Motion的22条曲线整组规则、完整曲线key/tangent/weight/wrap语义、Clip与Timeline各自owner及时间域 MUST保留，未知channel或不完整组 MUST由正式曲线入口拒绝。

#### Scenario: 只替换一条Foot Motion曲线

- **WHEN** C#尝试只提交要求整组更新的Foot Motion曲线中的一条
- **THEN** 正式曲线业务入口 MUST拒绝不完整数据组
- **AND** MUST不依赖Document Reconciler或写入部分资产

#### Scenario: 修改有限Action动画通道

- **WHEN** C#显式修改一个有限Action轨道的通道配置
- **THEN** 正式Timeline入口 MUST验证该配置与引用闭包
- **AND** MUST不自动修改Pose、Rig、持续Locomotion源或未授权owner来凑出合法结果

### Requirement: 状态转移与动作目标规则必须由正式模型继续承载

状态转移参数 MUST仅由正式Edge保存，优先级、条件、退出策略、顺序与owner规则 MUST继续由正式图模型检查，节点或步骤不得恢复同义副本。动作目标的输入声明、Blackboard引用、准入、激活与动作目标要求 MUST继续通过正式强类型引用闭合；删除Agent后不得靠显示名匹配或省略这些关系。

#### Scenario: C#配置状态转移

- **WHEN** 同一source包含多条转移且提交了重复order或非法端点
- **THEN** 正式Graph规则 MUST拒绝该配置
- **AND** MUST不把冲突参数存到状态节点或组合步骤中

### Requirement: 执行与Build必须显式且相互独立

作者C# MUST经明确Editor入口执行，编译、导入、selection、Inspector重绘或文件保存 MUST不自动应用修改。Unity处于Play、切换Play、编译或资产更新期间 MUST拒绝作者写入，不依赖后台排队重试。作者操作 MUST只修改资产，运行产物由既有精确目标Build独立显式生成；其他Build/Preview/MCP能力不因Agent退役而一并删除。

#### Scenario: C#文件编译完成

- **WHEN** Unity完成新的作者C#代码编译
- **THEN** 系统 MUST不自动修改角色资产或发布运行产物

#### Scenario: 作者保存后请求Build

- **WHEN** 作者已保存合法资产并明确请求指定Definition的Build
- **THEN** 正式Build MUST按原依赖与发布规则生成产物
- **AND** MUST不要求旧Document处于Clean或存在任何Agent工作包
