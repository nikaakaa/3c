## ADDED Requirements

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
