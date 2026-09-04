## MODIFIED Requirements

### Requirement: Source identity 与 runtime instance identity 必须分离

Diagnostics MUST继续使用稳定authoring source与独立runtime instance identity。通用Tree execution core MUST拥有`TreeAuthoringElementKey`；Diagnostics Source Map MUST从该core key单向生成`RuntimeSourceElementKey`。通用Runnable event MUST额外暴露stable containment route与activation generation，以区分shared Graph多引用和同一Node多次进入。Editor MUST不把route display path、asset path或runtime clone引用当authoring identity，正式Tree lifecycle与Presentation binding MUST不依赖Diagnostics类型。

#### Scenario: 同一状态重复进入

- **WHEN** 同一个 State source 在不同 activation generation 中执行
- **THEN** 两次执行 MUST 使用相同 source identity
- **AND** 两次执行 MUST 使用不同 runtime instance identity

#### Scenario: 两个角色运行同一 Definition

- **WHEN** 两个 Character runtime 使用同一 authoring source
- **THEN** Trace MUST 能按 Character runtime session 分离
- **AND** Debug Session MUST NOT 合并两个角色的 Node、Timeline 或 Blackboard 状态

#### Scenario: shared Graph双引用

- **WHEN** 两个owner运行同一个shared Graph中的同一Node
- **THEN** Trace source element MAY相同
- **AND** authoring route、parent activation与runtime instance MUST不同
#### Scenario: 并发技能及代码控制来源

- **WHEN** 同Actor两个释放引用同一技能且由C#模块选择
- **THEN** Trace MUST区分控制模块／代码位置与技能模板／ActionInstance／调用点／generation
- **AND** 代码来源 MUST不伪造Graph节点，模板相同不得合并实例状态


### Requirement: Debug Source Map 必须严格映射执行元素到 authoring source

Compiler MUST为 operation、state slot、scope、Timeline segment、TreeClip、Skill／Action／Effect definition 和 presentation producer 生成严格 Source Map。断裂、歧义或 duplicate identity MUST使 Program build 失败。

#### Scenario: 定位 Timeline Window

- **WHEN** Trace 包含 ActionWindow EventId
- **THEN** Source Map MUST唯一定位原 Timeline/TreeClip/declaration
#### Scenario: 定位代码控制决策

- **WHEN** 动作来源是已登记控制代码位置
- **THEN** Diagnostics MUST能关联稳定模块／代码来源与后续ActionInstance
- **AND** MUST不要求不存在的角色图节点
