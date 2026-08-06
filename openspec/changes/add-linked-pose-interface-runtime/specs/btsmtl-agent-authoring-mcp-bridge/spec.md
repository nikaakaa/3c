## ADDED Requirements

### Requirement: Linked Pose 必须复用固定 Document 生命周期工具

`btsmtl.checkout_document`、`btsmtl.rebase_document`、`btsmtl.dry_run_document`、`btsmtl.apply_document` 与 `btsmtl.validate` MUST 通过同一 Document application service 支持 Linked Interface context、Implementation editable 分片、Entry Graph、Profile Group 与 selector binding。MCP MUST 不新增 `create_linked_pose`、`switch_implementation`、`patch_entry_graph` 或其它 Pose 领域 action，也 MUST 不直接 Build Projection、修改活动 Runtime Session 或形成业务 selector 旁路。

#### Scenario: Agent 创建武器 Implementation

- **WHEN** Agent 需要创建 Implementation、两个 Entry Graph 与 Equipment selector 映射
- **THEN** Agent MUST 修改 Document 目标状态并调用既有 dry-run 与 apply
- **AND** MCP discovery MUST 仍只暴露固定生命周期工具集合

#### Scenario: Agent 请求运行时切换实现

- **WHEN** MCP 调用内容尝试直接改变活动 Session Linked handle
- **THEN** Bridge MUST 拒绝该行为不属于 authoring Document 事务
- **AND** MUST 不把 Runtime 调试操作混入 apply
