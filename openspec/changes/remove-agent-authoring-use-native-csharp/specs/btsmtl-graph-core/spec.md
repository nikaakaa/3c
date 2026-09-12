## MODIFIED Requirements

### Requirement: Graph 必须拥有统一稳定 authoring identity

每个 `BaseGraph` MUST 持有稳定 `GraphAuthoringId`，Node 和 Edge MUST 继续持有各自稳定 authoring GUID。Graph runtime clone MUST 保留这些 source identities，但 MUST 使用独立 runtime instance identity。Pipeline Blackboard declaration owner、C#资产编辑、Debug Source Map 和 editor navigation MUST 引用同一个 Graph authoring identity。

#### Scenario: 创建 inline Graph

- **WHEN** owner 创建新的 inline Graph
- **THEN** Graph MUST 获得新的稳定 `GraphAuthoringId`
- **AND** Graph 内 Node/Edge MUST 获得各自稳定 identity

#### Scenario: 创建 runtime clone

- **WHEN** runtime 从 authoring Graph 创建工作副本
- **THEN** clone MUST 保留 Graph/Node/Edge authoring identity
- **AND** clone MUST 获得新的 runtime instance identity

#### Scenario: 迁移 Blackboard owner identity

- **WHEN** 实现将旧 `BlackboardOwnerId` 提升为 `GraphAuthoringId`
- **THEN** 现有 declaration owner reference MUST 一次性迁移到同一 identity value
- **AND** 旧字段、旧 API 和第二份 debug Graph id MUST 删除

### Requirement: Graph节点兼容性必须由稳定Authoring Capability裁决

每个可进入受限Graph的节点类型 MUST声明稳定authoring capability。Graph Role MUST通过唯一policy定义允许的capability；`CanCreateNodeType`、Node Search、拖拽、粘贴、脚本创建与Compiler Validator MUST复用该policy。系统 MUST为后续自动authoring暴露同一只读policy查询，且 MUST不建立自动authoring专用规则副本。系统 MUST NOT按NodePath字符串、显示名、继承层次或窗口类型猜测节点兼容性。

#### Scenario: 已退役AI图尝试进入BTSMTL

- **WHEN** 搜索、粘贴、脚本或Compiler尝试打开旧AIControllerTree或创建旧AI节点
- **THEN** 统一Graph policy MUST拒绝该图和节点
- **AND** Graph数据 MUST不发生修改

#### Scenario: Behavior Designer图不注册为BTSMTL Graph

- **WHEN** 作者从BTSMTL Graph入口选择Behavior Designer行为资源
- **THEN** 入口 MUST 明确说明该资源由插件编辑器拥有
- **AND** BTSMTL MUST 不为其创建Graph role、节点或编译镜像

#### Scenario: 退役AI节点缺少能力声明

- **WHEN** 未声明authoring capability的旧AI节点尝试进入任一BTSMTL Graph
- **THEN** 创建与发布 MUST失败并报告节点类型和Graph Role
- **AND** 系统 MUST不按默认Base节点处理
