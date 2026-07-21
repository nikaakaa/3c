## MODIFIED Requirements

### Requirement: Runtime value必须按declaration与scope owner共同寻址

Compiler MUST为declaration identity、Character、Graph activation、State execution path、ActionInstance和Frame owner生成稳定compiled address rule。Program layout MUST为每个scope owner分配稳定CompiledOwnerIndex；Kernel MUST使用`ScopeKind + CompiledOwnerIndex + Generation`的typed owner token隔离实例，MUST不使用runtime object reference、dictionary object identity、拼接字符串或显示路径作为真值地址。Character与Graph Config owner MUST在初始State建立；Graph、State和Action generation MUST来自各自正式lifecycle；Frame generation MUST来自当前SimulationTick。需要fact projection的真实写入 MUST保存typed write stamp，人类可读owner/provenance只能由diagnostics按需格式化。

#### Scenario: 两次State activation

- **WHEN** 同一State第二次进入
- **THEN** 新owner generation MUST与上一次State activation隔离
- **AND** Runtime MUST不通过字符串execution path比较或旧value清零来建立隔离

#### Scenario: 两个ActionInstance使用同一declaration

- **WHEN** 同一Action-scoped declaration先后由两个ActionInstance写入
- **THEN** typed owner token MUST使用各自ActionInstance generation
- **AND** 后一个instance MUST不读取或投影前一个instance的值

#### Scenario: Diagnostics显示State owner

- **WHEN** diagnostics实际请求Blackboard owner或write provenance
- **THEN** formatter MAY通过Program SourceMap和typed token生成可读路径
- **AND** 关闭diagnostics的正常Tick MUST不构造该字符串

### Requirement: Decision TreeClip必须通过声明式Frame Blackboard输出决策

Decision TreeClip写入的变量 MUST来自ExposedProperty对应的Pipeline Blackboard declaration，并且 MUST使用`Frame` scope和`Frame` lifetime。Runtime MUST在Frame开始推进当前Frame generation，在当前clip active时重新求值并写入，并在State.OnExit完成后的Frame结束统一flush当前generation的projection candidate。Frame value读取发现owner generation不匹配时 MUST表现为declaration default且不得物理写入State；只有当前Frame第一次真实写入才可materialize value、typed owner token与write stamp。Frame结束 MUST使该generation后续不可读、不可投影，但 MUST不通过遍历全部Frame group写默认值或清空State实现。Projection=None的写入 MUST保持本地；显式ActionWindow projection MUST继续通过唯一projection stage暂存candidate并在EndFrame生成正式fact。

#### Scenario: Dodge恢复段开放动作切换

- **WHEN** Dodge Timeline的`RecoveryOpen` Decision TreeClip在当前Tick active
- **THEN** Tree MUST写入owner-local Bool Frame declaration
- **AND** 唯一projection stage MUST暂存当前ActionInstance的ActionWindow candidate
- **AND** Dodge Transition MUST能在同一Tick通过`ActionWindowActiveInfoNode`读取该WindowType

#### Scenario: Decision clip不再active

- **WHEN** 新logic frame中Decision TreeClip不在active时间范围
- **THEN** Frame Blackboard MUST把上一generation的true表现为declaration default
- **AND** Runtime MUST NOT依赖OnDisable写false或EndFrame物理清零才能关闭gate

#### Scenario: 当前Frame没有Decision写入

- **WHEN** 当前Tick没有任何Decision TreeClip写入某个Frame declaration
- **THEN** 读取 MUST返回declaration default且不能生成write provenance或projection
- **AND** 该declaration的value、owner、provenance和candidate state MUST不因Frame begin/end被标记dirty

#### Scenario: 声明策略冲突

- **WHEN** Timeline inline Tree与RootTree对同一Blackboard key声明不同类型、scope、lifetime、authority或sync policy
- **THEN** validator或runtime MUST报告配置错误
- **AND** 系统 MUST NOT选择任一声明作为fallback
