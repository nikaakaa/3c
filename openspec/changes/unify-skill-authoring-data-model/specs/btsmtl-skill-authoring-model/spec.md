## Purpose

定义原业务节点与FlowCanvas对应节点共用参数、默认值、校验、逻辑端口和引用规则的作者合同，消除为接入画布而重复维护的业务定义。各节点实例保持独立，图与工作包沿用现有正式格式，人工编辑与编译必须对同义业务给出一致结果。

## ADDED Requirements

### Requirement: 同义节点必须共用业务定义

经行为对照确认为同义的原业务节点与FlowCanvas节点 MUST使用同一参数合同、默认值、字段约束、逻辑端口、引用与适用编译语义。框架适配 MUST只负责明确本地身份、端口对象和事件接入，不得分别维护同义业务规则。仍有正式消费者的原节点 MUST接入共同定义；零消费者旧适配 MUST在迁移后删除。

#### Scenario: 两种节点接收相同移动配置

- **WHEN** 原移动节点与FlowCanvas移动节点提交相同的模式、速度、曲线和时长
- **THEN** 两者 MUST使用同一业务校验并得到一致的合法性和编译解释
- **AND** 已正确的原业务约束 MUST保留，不能以较弱的新适配规则替代

#### Scenario: 修改共同规则

- **WHEN** 共同业务定义修改一个默认值或合法范围
- **THEN** 两侧作者适配及目录 MUST消费该定义，不能分别维护第二份同义规则

### Requirement: 不同实例必须独立保存参数

每个节点实例 MUST只保存自己的一份业务参数，不能同时保留壳字段与参数对象中的同义值。两个节点使用同一种定义 MUST不意味着共享可变参数。现有图集合、连接、UID、Macro接口和默认输入值存储 MUST保持各自唯一正式来源，不得建立第二份可写图。

#### Scenario: 两个移动节点配置不同速度

- **WHEN** 两个实例使用同一移动定义但设置不同速度
- **THEN** 保存重载后各实例 MUST保留自己的数值，修改其中一个不得覆盖另一个

### Requirement: 真实业务差异必须明确保留

节点对照 MUST包含值类型、单位、作用域、输入输出、引用、执行和停止行为。不同业务不能仅凭相同显示名合并。已确认规则冲突 MUST列明影响并在裁决前拒绝覆盖，不能静默选择任一版本；没有对应节点的正式能力不得被裁剪或强行制造另一份节点。

#### Scenario: 同名节点执行含义不同

- **WHEN** 两个同名节点具有不同的值类型或执行生命周期
- **THEN** 系统 MUST保留明确的业务区别，只共用已确认相同的部分

### Requirement: 字段与逻辑端口必须只有一套规则

字段访问、默认值、可写性、条件可见性与逻辑端口形状 MUST来自共同定义。两个作者宿主可以具有不同本地端口identity和控件，但 MUST通过明确映射使用同一规则，不得按名称猜测或插入隐式转换。本次抽取 MUST保持既有公开字段和端点，不因内部类型调整改写已有连线。

#### Scenario: 相同字段从两侧修改

- **WHEN** 原作者入口与FlowCanvas入口修改对应业务字段
- **THEN** 两者 MUST采用同一类型、默认值和合法范围
- **AND** 非法值 MUST在部分修改正式参数前被拒绝

#### Scenario: 重命名而不改变业务身份

- **WHEN** 作者只改变显示名称或重新加载图
- **THEN** 既有端口映射、连线和参数identity MUST保持有效

### Requirement: 参数引用迁移必须保留既有作者行为

受参数抽取影响的资产和子图引用 MUST由共同定义读取和重映射，并使用现有正式owner与Mutation合同。复制私有引用、保留共享引用、删除回收和保存重载 MUST保留既有正确行为；不得因字段存储位置改变漏掉目标或另建事务。

#### Scenario: 复制含私有与共享引用的节点

- **WHEN** 复制已迁入共同参数合同的节点
- **THEN** 其私有内容 MUST按既有所有权规则独立复制，共享内容 MUST保持正式共享引用

### Requirement: 编译必须对同义业务使用同一解释

原节点与FlowCanvas节点的同义业务参数 MUST通过适用的共同编译规则进入现有运行产物。框架的UID、端口及调用路径通过正式映射保留来源；不得构造旧作者节点执行或运行画布委托。必要的宿主来源差异不要求完整产物字节相同，但业务参数、操作与连接语义 MUST一致。

#### Scenario: 编译两侧相同业务输入

- **WHEN** 两侧适配提交同义参数与已映射的输入输出
- **THEN** 编译 MUST使用同一业务规则，来源 MUST能定位到各自真实节点

### Requirement: Document必须消费定义而保持现行形状

Document MUST从共同业务定义读取和提交节点参数，保留实施基线唯一公开合同的kind、typed properties、values、逻辑端点、owner和单一包事务。内部参数类型与文件组织变化 MUST不自行升级schema、增加MCP工具或兼容reader；其他独立变更已正式发布新合同后 MUST消费该唯一合同，不得恢复旧版。无法无损映射的公开合同差异 MUST明确报告，不能静默扩大本次迁移。

#### Scenario: 参数抽取后的Document往返

- **WHEN** 相同业务目标经过现有导出、dry-run、apply和重新导出
- **THEN** 正式节点与包 MUST保持相同业务参数、identity和引用，外部形状仍符合实施基线的唯一正式版本
- **AND** 往返成功不能代替两种节点确实共用业务定义的验证

### Requirement: 参数存储迁移必须完整删除重复来源

参数从节点壳迁入共同参数对象前 MUST核对精确实例的旧值、UID、端点和引用。迁移 MUST通过现有正式资产写入与事务完成，失败恢复已有资产，不覆盖用户未提交改动。成功后重复字段、重复校验、无消费者旧适配和一次性转换代码 MUST删除，正常入口不得依靠双读或双写。

#### Scenario: 迁移已有节点

- **WHEN** 已有节点的参数被移入共同参数对象
- **THEN** 保存重载后参数值、连接和引用 MUST保持不变
- **AND** 正常读取 MUST不再依赖旧字段补读

#### Scenario: 参数迁移失败

- **WHEN** 写入或保存失败
- **THEN** 原参数、身份和引用 MUST由既有事务恢复，不能留下半迁移实例

### Requirement: 状态机转移必须使用唯一Edge Payload

StateMachine的Transfer Edge MUST唯一保存ConditionRule引用、priority、abortPolicy和非负order。Enter、Any和State只能通过固定Transfer输出发起转移，State和Exit只能通过固定StateIn输入接收；状态节点和anchor MUST不保存同义转移Step。

#### Scenario: 多条转移保持作者顺序

- **WHEN** 同一状态有多条合法Transfer Edge
- **THEN** 编译、Document和作者视图 MUST按显式order保持同一顺序
- **AND** 同一source的order重复 MUST被拒绝

#### Scenario: 普通组合节点仍使用Step

- **WHEN** Sequence、Selector或Parallel保存执行分支
- **THEN** 分支 MUST继续位于该节点的`properties.steps`
- **AND** 该Step数据 MUST不被解释为StateMachine Edge转移数据
