## ADDED Requirements

### Requirement: 相机配置必须由唯一正式 owner 装配

Character Definition MUST 装配正式 Camera Profile；Profile MUST 注册 Sequence、效果、曲线和全局输入/平滑/环境配置；各参数只能由一个正式 owner 定义。所有有效字段 MUST 有明确单位、数值域和运行消费者。重复轨道、确认无消费者字段和旧控制器配置 MUST 在迁移后完整删除，不保留兼容读法。

#### Scenario: 作者调整默认轨道

- **WHEN** 作者修改默认 Sequence 的轨道
- **THEN** Build MUST 从该唯一配置生成运行数据
- **AND** MUST 不存在另一份可编辑轨道覆盖其结果

### Requirement: 相机作者编辑必须复用现有领域 API

Graph、Timeline、资源 Inspector 与 C# 作者入口 MUST 调用相同正式业务 API，保持稳定身份、引用、顺序、Undo/Redo 和保存语义。相机作者能力 MUST 不恢复旧 Agent Document、目录包、同步器或第二份字段/校验模型。OnInspectorGUI MUST 不执行编译、批量导入或运行求值等重操作。

#### Scenario: 编辑相机资源

- **WHEN** 作者改变正式 Profile 或 Effect 参数
- **THEN** 编辑 MUST 只修改真实 owner 并产生正确 dirty/Undo 状态
- **AND** MUST 不自动导出 C# 或直接修改正在运行的相机

### Requirement: 相机曲线与依赖必须具有真实导航

Timeline-local 权重及 Ease 曲线 MUST 由对应 Clip 拥有并进入现有 typed Curve Channel；共享 Effect 曲线 MUST 保留资源 owner，引用处只能导航，不复制为另一可编辑来源。资源选择 MUST 使用正式引用与支持范围，缺失或不支持能力必须显示原因。

#### Scenario: Clip 引用共享效果曲线

- **WHEN** 作者从 Clip 查看共享曲线
- **THEN** 编辑器 MUST 导航到实际资源 owner
- **AND** MUST 不在 Clip 中保存第二份曲线

### Requirement: 作者必须能区分配置保存和运行发布

作者入口 MUST 显示明确 Build 状态、当前 Projection 身份与不可用原因；生成作者资产和发布运行产物必须沿各自正式入口。Runtime/Preview MUST 只消费已发布计划，不能逐字段热读作者对象形成混合版本。

#### Scenario: 保存 Profile 后尚未 Build

- **WHEN** 当前运行实例仍使用旧 Projection
- **THEN** 作者界面 MUST 明确显示配置与运行版本的关系
- **AND** MUST 不宣称新参数已经作用于当前画面
