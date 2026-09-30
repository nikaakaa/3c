# btsmtl-flowcanvas-authoring Specification

## Purpose

定义当前 BTSMTL 技能的原生 FlowCanvas / NodeCanvas 作者入口、实际资产所有权与正式编译边界。Timeline 时间归 btsmtl-timeline-clock-domain，通用作者字段与事务归 btsmtl-skill-authoring-model 和 graph-authoring-domain-framework，状态生命周期归 btsmtl-sm-node-authoring。

## Requirements

### Requirement: 技能作者必须使用唯一原生拓扑和编辑表面

Skill 根、StateBody、ConditionRule、Macro 和 TimelineBody MUST 使用正式 FlowCanvas 图；技能 FSM MUST 使用 NodeCanvas 原生 FSM、FSMState 和 FSMConnection。编辑、保存、C# 导出、生成和语义编译 MUST 读取同一正式资产拓扑。原生 GraphEditor MUST 提供画布、Blackboard、Inspector、菜单、变量拖拽、selection、clipboard、Undo 和下钻；领域 adapter MUST 不创建第二个画布、变量表或替换原生面板。Timeline MUST 使用现行 Slate 作者表面，不恢复旧 TreeDesigner 窗口或 GraphView。

#### Scenario: 修改攻击连段转移
- **WHEN** 作者从 Attack 的正式 Ability 入口修改 FSM 连接
- **THEN** 修改 MUST 落入该连接的正式配置和真实 owner Undo
- **AND** 保存、导出与编译 MUST 消费同一内容，不转换成另一张旧作者图

### Requirement: 私有图与共享调用必须拥有明确资产归属

StateBody MUST 由 State 唯一拥有，私有 ConditionRule MUST 由转移 Edge 唯一拥有，FSM MUST 归其正式调用 owner 和实际资产文件。参数化子图 MUST 使用原生接口和调用表达。复制私有闭包 MUST 重映射其稳定引用；共享内容 MUST 保持显式引用，不因复制生成第二份共享规则。

#### Scenario: 复制含私有 FSM 的技能
- **WHEN** 作者复制包含 FSM、StateBody 和转移条件的 Ability
- **THEN** 私有内容 MUST 随新 owner 独立重建，共享资产 MUST 继续引用明确目标
- **AND** 实际修改的资产 MUST 进入同一正式复制和保存事务

### Requirement: 原生作者图必须进入正式技能执行器

FSM Entry/Prime、Any、Exit MUST 表达入口、任意状态和当前 FSM 调用出口，MUST 不被视为 Ability 终态。转移条件、priority、abortPolicy 和同源唯一 order MUST 归原生连接。图 MUST 经语义编译进入正式 Ability 执行数据，MUST 不启动插件 FSM runtime、GraphOwner、ActionTask.Execute、Condition.Check 或协程作为备用执行器；未登记任务 MUST 在正式能力边界拒绝。

#### Scenario: 节点缺少正式编译映射
- **WHEN** 创建、粘贴或生成的原生任务没有正式能力声明和编译映射
- **THEN** 作者或编译边界 MUST 定位并拒绝该实体
- **AND** MUST 不借用插件运行时补足执行

### Requirement: 完整技能作者入口必须包含 Ability 和共享准入来源

GameplayAbilityDefinition MUST 是完整技能作者入口；AbilityGraph MUST 是其内部执行页。准入、阻断、取消、并发分组和目标规则 MUST 由精确引用的 GameplayAbilityAdmissionProfile 唯一持有，不复制进 Ability。授予参数 MUST 归角色或装备，一次执行状态 MUST 归运行上下文。两个 Dodge MUST 保持同一共享准入资产及准入分组身份，不恢复 ActionProfile 兼容别名或内联副本。

#### Scenario: 两个闪避能力共享规则
- **WHEN** 作者查看或导出 DodgeBack 与 DodgeForward
- **THEN** 两者 MUST 指向同一正式 AdmissionProfile
- **AND** 范围内规则 MUST 只生成一次，范围外规则 MUST 保留明确资源引用

### Requirement: Ability 结束与 StateBody 系统清理必须各自归属

Ability 生命周期 MUST 决定执行结束结果。StateBody MUST 固定包含 OnEnter、Root、OnExit 三个系统锚点；OnExit MUST 不可删除且不进入作者创建菜单，仅由运行代码在 State stop barrier 调用。停止子内容和资源回收 MUST 由原模块完成，MUST 不要求作者配置 Action Exit Selector、Submit、终态图或 OnExit 清理图。内部状态接替 MUST 不产生第二个 Ability 结束决定。

#### Scenario: 连段内部切换状态
- **WHEN** 连段从一个 StateBody 转移到下一状态
- **THEN** 原运行模块 MUST 在 stop barrier 完成退出与回收，再进入目标状态
- **AND** 已确定的 Ability 结果 MUST 不被 OnExit 改写

### Requirement: 技能参数必须通过正式 provider 与类型合同访问

Skill Local、Ability Attribute、GameplayTag、Character State、Input、Target 和 Frame provider MUST 保留真实 owner、稳定声明 ID、精确类型和生命周期。原生 Get/Set、变量创建和外部节点 MUST 使用同一 Capability、Mutation 与 Undo。只读 Character State、Input、Target 和 Frame provider MUST 不允许写入，MUST 不按名称复制成技能私有状态。

#### Scenario: 从 Blackboard 引用角色事实
- **WHEN** 作者把角色移动事实拖入 Skill 图
- **THEN** 节点 MUST 保存正式 provider、声明身份和类型
- **AND** MUST 不创建同名可写变量或以默认值代替缺失引用

### Requirement: 完整 C# 生成必须恢复业务闭包和明确根绑定

完整 Ability 导出和生成 MUST 包含 Definition、执行图、FSM、私有条件、StateBody、Timeline 与范围内共享准入规则，并恢复明确根引用、业务 identity、order、参数和布局。范围外依赖 MUST 使用正式精确资源引用，MUST 不通过私有字段反射、旧 Document/JSON 协议或原私有子资产路径补足缺口。人工编辑、显式导出与显式生成 MUST 分开；窗口保存、Undo、刷新 MUST 不自动导出、同步源码或构建。

#### Scenario: 删除生成输出后重建
- **WHEN** 作者删除明确生成范围的输出并运行同一正式生成入口
- **THEN** MUST 重建其业务闭包和根挂接，不依赖原私有子资产仍存在
- **AND** MUST 不合并尚未显式导出的人工修改，也不创建第二套作者模型
