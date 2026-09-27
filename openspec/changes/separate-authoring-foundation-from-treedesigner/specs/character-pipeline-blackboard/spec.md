## ADDED Requirements

### Requirement: 黑板公共数据必须与具体图运行接入分离

独立的 scope/lifetime、输入绑定、事实投射数据、变量引用数据及只依赖公共值的规则 MUST 归入 TreeDesigner 之外的公共作者定义。公共定义 MUST 不引用 BaseGraph、BaseExposedProperty、StateMachineExecutionScope 或 Editor。读取具体声明对象的创建与校验职责 MUST 留在所属实现；IPipelineBlackboardRuntimeAccess MUST 继续归属具体图运行接入，不通过改写运行签名或新增通用接口消除依赖。

#### Scenario: 从具体声明创建公共引用

- **WHEN** 领域入口从 BaseExposedProperty 创建 PipelineBlackboardVariableReference
- **THEN** 具体声明侧 MUST 提取现有字段并构造唯一公共引用数据，保持声明身份、owner、显示键和类型匹配语义
- **AND** 公共引用 MUST 不反向读取具体声明，不保留旧耦合构造入口、第二份数据模型或反射转换器

#### Scenario: 拆分混合规则

- **WHEN** 同一 policy 同时包含公共值组合规则和具体声明校验
- **THEN** 两类成员 MUST 按依赖分属公共定义与具体实现，规则结果保持不变
- **AND** MUST 不把整类搬入公共层后继续引用 TreeDesigner

### Requirement: 黑板定义归位必须保留现有声明与运行语义

黑板整理 MUST 保留原生变量和自有声明的稳定关联、默认值、所有权、分域 Provider、scope/lifetime、typed 地址、generation 与 provenance。现行 fact projection 和写入 provenance 要求 MUST 完整保留，包括动作窗口类型、身份、候选隔离和 Model Egress 准入。MUST 只调整迁出的作者定义及直接消费者，不增加重复声明、可写 Provider 镜像或新的运行字典。

#### Scenario: 迁移作用域定义

- **WHEN** 作用域和生命周期类型从混合文件迁入作者目录
- **THEN** 现有声明、编译映射与运行地址 MUST 保持相同语义
- **AND** MUST 不重写变量重置、脏状态或值缓存机制

#### Scenario: 类型仍有有效消费者

- **WHEN** 旧变量类型仍被合法资产或运行调用使用
- **THEN** MUST 保留其有效职责并记录具体引用
- **AND** MUST 不因删除旧黑板面板而删除该变量或强制迁移资产格式
