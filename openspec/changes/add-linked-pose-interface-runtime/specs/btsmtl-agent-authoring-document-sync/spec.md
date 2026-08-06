## ADDED Requirements

### Requirement: Document v3 必须原子表达 Linked Pose 只读接口与可编辑装配

Document v3 MUST 把 Linked Interface 以只读 Presentation context 分片导出，并把 Linked Implementation、Entry Pose Graph、layout、Profile Group 与 selector binding 作为 editable Presentation 分片导出。Interface 分片 MUST 包含稳定 identity、revision、signature、Fact contract、Entry 与 typed ports，但 MUST 不得被 Reconciler 降低为修改命令。Implementation、Group 与 selector binding MUST 使用正式对象引用或新建资产的 `local:*` 计划 identity，并与现有 Pose Graph 分片共同进入 strict manifest、规范 package hash、checkout、rebase、dry-run、apply、Conflict 与 reverse export。

#### Scenario: Agent 修改 Implementation Entry Graph

- **WHEN** editable package 只改变一个 Rifle Entry Graph 节点
- **THEN** dry-run 与 apply MUST 仍锁定完整 Document package 及精确 Implementation/Profile owner
- **AND** apply 后 MUST 从最终 Unity 树反向导出整包基线

#### Scenario: Agent 尝试修改 Interface 端口

- **WHEN** editable 内容或额外文件尝试改变只读 Interface signature
- **THEN** strict codec 或 Reconciler MUST 拒绝该 package
- **AND** MUST 不创建影子 Interface 或在 Implementation 内复制端口定义

#### Scenario: Agent 创建 Implementation asset

- **WHEN** editable package 用 `local:*` 声明新 Implementation 及其 required Entry Graph owners
- **THEN** Reconciler MUST 生成唯一 typed Presentation Mutation plan
- **AND** reverse export MUST 用正式 GUID 与 local file id 替换计划 identity

#### Scenario: Agent 修改 Equipment selector

- **WHEN** editable package 改变 EquipmentId 到 ImplementationId 的精确映射
- **THEN** Reconciler MUST 通过正式 Profile selector Mutation 应用变化
- **AND** reverse export MUST 保留 Group 与 selector 的稳定 identity

### Requirement: Linked Pose 分片必须继续服从 Document v3 单一事务边界

系统 MUST 不提供单个 Implementation 文件 apply、Graph 路径级 patch、缺失 Interface context fallback、直接 runtime switch 或按显示名解析。Linked Pose 文件变化、Editor 选择与 Inspector 绘制 MUST 不自动触发 codec、Reconciler、Build 或 Apply；只有固定生命周期命令 MAY 执行对应重操作。

#### Scenario: Implementation 文件在磁盘被修改

- **WHEN** 外部编辑器保存一个 Implementation JSON
- **THEN** Unity Editor MUST 不自动 apply 或 Build
- **AND** 后续显式 dry-run MUST 按整个 package hash 检查变化
