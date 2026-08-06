# graph-authoring-domain-framework Specification

## ADDED Requirements

### Requirement: 跨资产作者命令必须由领域应用服务形成原子Mutation闭包

需要创建、复制、配置或删除多个serialized owner的作者命令 MUST由当前domain application service降低为typed Mutation closure，收集精确owner，在一个Undo/rollback边界内按依赖顺序应用，并运行同一Capability与Validator。UI presenter、GraphView与Custom Inspector MUST不直接创建资产、写SerializedObject path、修改owner数组或保存第二份待提交状态。Document Reconciler MUST复用同一种Mutation与handler语义，但保留其整包dry-run/apply事务。

#### Scenario: UI创建Implementation及全部Entry Graph

- **WHEN** 作者从Interface执行Create Implementation
- **THEN** application service MUST原子创建Implementation、required Entry binding、Graph owner、Graph与边界节点
- **AND** 任一mutation或validation失败 MUST回滚全部owner

#### Scenario: UI与Document创建等价Implementation

- **WHEN** UI命令与Document目标状态表达同一Interface和Entry Graph闭包
- **THEN** 两者 MUST使用相同typed mutation kind、identity allocator、handler与validator
- **AND** 最终Unity authoring结构、revision与诊断语义 MUST一致
