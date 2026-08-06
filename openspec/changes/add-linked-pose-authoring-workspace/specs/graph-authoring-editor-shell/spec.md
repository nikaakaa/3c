# graph-authoring-editor-shell Specification

## ADDED Requirements

### Requirement: Shell必须承载Graph关联对象页而不复制Canvas状态

`GraphAuthoringEditorShell` MUST允许domain adapter把Profile-owned Group、selector、Interface与Implementation等Graph关联对象投影为Navigator selection和Details page。选择非Graph对象时 MUST保留当前唯一Graph Canvas与page stack，只更新同一selection context、Details与可用命令；打开其Graph page时 MUST继续通过同一Canvas与breadcrumb导航。Shell MUST不为这些对象创建第二GraphView、独立Workbench或Inspector selection缓存。

#### Scenario: 在Entry Graph中选择所属Group

- **WHEN** 作者从breadcrumb或Navigator选择当前Entry所属Group
- **THEN** 同一Details MUST显示Group作者页且Canvas MUST保持当前Entry Graph上下文
- **AND** 返回root或打开另一Entry MUST继续使用同一page stack

### Requirement: Shell必须统一呈现领域命令状态与跨对象导航

Shell MUST为domain command显示可执行状态与不可执行原因，并允许Details、Navigator、Canvas node与diagnostic之间按稳定真实引用双向定位。Shell MUST不执行领域依赖扫描、资产创建或删除逻辑；这些命令 MUST由domain application service预检并返回结果。

#### Scenario: 删除命令被引用阻塞

- **WHEN** domain application service报告目标仍被两个Call和一个selector mapping引用
- **THEN** Details MUST禁用删除并显示三个可跳转依赖
- **AND** Shell MUST不按显示名重新扫描或自行修改引用
