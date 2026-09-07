## ADDED Requirements

### Requirement: 仓库策略必须精确允许Network Test Orchestrator工程

Repository Policy MUST精确允许`Tools/ThirdPersonNetworkTest/ThirdPerson.NetworkTest.Orchestrator.csproj`作为被跟踪的本地Windows Network Test会话编排工具工程，并允许它显式链接项目唯一Network Test合同源文件。允许项 MUST不宽泛覆盖Tools目录下其它`.csproj`、`.sln`或Unity生成工程。现有GitHub基础CI MUST不因此新增Orchestrator build、Unity Player build、网络集成测试或部署job。

#### Scenario: Network Test Orchestrator工程被跟踪

- **WHEN** 候选提交包含精确Orchestrator project及明确源文件
- **THEN** Repository Policy MUST接受该正式工具工程
- **AND** 现有三个基础CI job及其只读边界 MUST保持不变

#### Scenario: Tools目录出现未批准工程

- **WHEN** Tools下出现除已批准Performance Controller和Network Test Orchestrator之外的其它project或solution
- **THEN** Repository Policy MUST继续列出路径并失败
- **AND** MUST不因本change扩大通配允许范围

