## ADDED Requirements

### Requirement: 相机作者内容必须通过现有 C# 领域适配完整转换

纳入生成范围的 Camera Profile、Sequence、Effect、Curve 以及 Graph/Timeline 相机请求 MUST 通过现有 btsmtl.export_code 与 btsmtl.generate_assets 的领域薄适配表达真实创建、参数、引用、顺序、共享资源和根绑定。系统 MUST 复用正式相机 API，不新增第三个作者工具、目录包、同步器或第二份 Camera 领域模型。无法完整表达的内容必须定位对象/字段并拒绝该范围导出，不得以占位代码报告成功。

#### Scenario: 导出包含共享相机资源的动作

- **WHEN** 明确生成范围包含相机请求及其受支持资源
- **THEN** 导出 MUST 保持请求时点、资源共享、参数和绑定关系
- **AND** 显式生成 MUST 通过正式 API 恢复根消费者绑定，不只创建孤立资源

#### Scenario: 相机领域尚不支持某个字段

- **WHEN** 正式资产包含导出适配无法表达的字段
- **THEN** 工具 MUST 拒绝该范围的完整导出并说明缺口
- **AND** MUST 不恢复旧 Agent 协议或静默省略字段
