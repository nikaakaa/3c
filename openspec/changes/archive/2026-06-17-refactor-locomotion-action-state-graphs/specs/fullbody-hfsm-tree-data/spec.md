# fullbody-hfsm-tree-data Delta

## MODIFIED Requirements
### Requirement: FullBody HFSM 中心树资产
系统 MUST 将旧 FullBody HFSM 中心树资产视为已退役语义。默认 Corin 正式 gameplay MUST NOT 依赖中心 FullBody HFSM 树资产表达 `Action.Dodge`，正式配置 MUST 通过 Locomotion graph 资产、Action 配置资产、BodyClaimPolicy 和 Character frame pipeline 模块边界追踪。

#### Scenario: 中心树资产不再是正式配置入口
- **WHEN** 设计者追踪默认 Corin 正式配置
- **THEN** Locomotion graph MUST 来自 Movement 配置目录
- **AND** Dodge action config MUST 来自 Action 配置目录
- **AND** MUST NOT 要求 `FullBodyHfsmTreeDefinitionSO` 表达当前 gameplay 权威

## REMOVED Requirements
### Requirement: 内嵌 HFSM 节点定义
该要求随 FullBody HFSM 中心树资产退役。默认 gameplay 不再依赖内嵌 FullBody HFSM 节点定义。

#### Scenario: 节点数据不再参与正式构建
- **WHEN** 正式角色 runtime 初始化
- **THEN** 系统 MUST NOT 要求读取 `FullBodyHfsmNodeDefinition`
- **AND** MUST NOT 从该节点数据恢复 Locomotion 或 Action 状态

### Requirement: FullBody HFSM 树编译和校验
该要求随中心树资产退役。默认 gameplay 的配置校验 MUST 分别归属 Locomotion graph validator、Action config validator 和 Character config root validator。

#### Scenario: 校验职责迁移
- **WHEN** 自动校验 Corin gameplay 配置
- **THEN** Locomotion graph validator MUST 校验 `Locomotion.*`
- **AND** Action config validator MUST 校验 Dodge action、animation 和 claim 配置
- **AND** 系统 MUST NOT 要求中心 FullBody HFSM 树校验 `Action.Dodge` 叶子

### Requirement: 路径从节点树推导
该要求被模块事实和兼容诊断 view 取代。正式 runtime 不再要求从 FullBody HFSM 节点树推导 `/FullBody/Action/Dodge`。

#### Scenario: 路径不是状态权威
- **WHEN** 诊断需要展示当前角色状态
- **THEN** 系统 MAY 从 Locomotion facts、Action lifecycle facts 和 frame plan 派生可读路径
- **AND** 该路径 MUST NOT 成为 graph transition 或 output application 的权威输入

### Requirement: Builder 消费中心树数据
该要求随 FullBody HFSM builder 退役。正式 runtime 不再通过中心树数据构建 FullBody HFSM。

#### Scenario: Builder 不参与正式主线
- **WHEN** 正式 Corin playable 初始化
- **THEN** 系统 MUST 通过 Character frame runtime、Movement module 和 Action module 建立主线
- **AND** MUST NOT 要求 `FullBodyHfsmStateTreeBuilder` 创建 gameplay 状态树

### Requirement: Owner 从编译节点绑定推导
该要求被 BodyArbiter、CharacterFramePlan、Movement facts 和 Action lifecycle facts 取代。

#### Scenario: Owner 从 frame plan 或 facts 推导
- **WHEN** 需要判断当前输出 owner
- **THEN** owner MUST 来自 BodyArbiter/CharacterFramePlan 或等价 frame facts
- **AND** MUST NOT 来自 FullBody HFSM compiled node binding

### Requirement: 运行时接入中心树资产
该要求随中心树资产退役。缺失中心树资产不应阻止正式 Character frame pipeline 运行；但缺失 Locomotion graph、Action config 或 BodyClaimPolicy 仍必须作为正式配置错误报告。

#### Scenario: 不使用中心树 fallback
- **WHEN** prefab 或角色配置不再引用 FullBody HFSM 树资产
- **THEN** 系统 MUST NOT 通过隐藏硬编码树补回旧 FullBody HFSM 主线
- **AND** 系统 MUST 继续校验正式 Locomotion 和 Action 配置

### Requirement: 只读树形编辑器预览
该要求随中心树资产退役。后续如需要诊断可视化，必须另开基于 Character frame pipeline facts 的只读调试视图。

#### Scenario: 不保留旧树预览验收
- **WHEN** 本变更归档后查看编辑器验收
- **THEN** 系统 MUST NOT 要求显示 `/FullBody/Action/Dodge` 树节点
- **AND** MAY 另行提供 Locomotion graph 或 Action lifecycle 的只读调试视图

### Requirement: 可测试和可验证
该要求的测试目标被 Locomotion graph 配置、Action lifecycle、frame arbitration、Run latch output 和 rollback replay 测试取代。

#### Scenario: 测试目标迁移
- **WHEN** 自动测试覆盖本变更
- **THEN** 测试 MUST 覆盖默认 Locomotion graph 不含 Action 节点
- **AND** MUST 覆盖 Action lifecycle active Dodge
- **AND** MUST 覆盖 Run latch capture/restore
- **AND** MUST NOT 覆盖 `/FullBody/Action/Dodge` 路径解析作为正式验收
