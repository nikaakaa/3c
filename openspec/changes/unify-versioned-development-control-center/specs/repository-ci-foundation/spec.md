## ADDED Requirements

### Requirement: 开发实验工具必须使用精确工程允许项且不扩大CI

Repository Policy MUST精确允许 `Tools/ThirdPersonDevelopment/ThirdPerson.Development.RunHost.csproj`、`Tools/ThirdPersonPerformanceCapture/ThirdPersonPerformanceCapture.Worker.csproj` 与 `Tools/ThirdPersonPerformanceCapture/ThirdPersonPerformanceCapture.Analyzer.csproj`，只消费声明的共享合同源。旧 Network Orchestrator/Performance Controller 工程及允许项 MUST随迁移删除，不能保留转发入口或宽泛 Tools 通配规则。正式产物库 MUST不进入 Git。现有 CI MUST不增加 Unity、采样、网络运行、工具发布或上传任务；本机有界 batchmode 构建例外 MUST不改变 CI 禁令。

#### Scenario: 跟踪最终工具工程

- **WHEN** 候选提交包含三个精确工具工程与共享合同依赖
- **THEN** Repository Policy MUST接受明确路径
- **AND** 其他未批准工程 MUST继续失败

#### Scenario: CI遇到本机后台构建能力

- **WHEN** 开发工具具备本机batchmode构建入口
- **THEN** 基础CI MUST不调用该入口或读取Unity授权
- **AND** CI绿色 MUST不能被描述为Player或性能通过
