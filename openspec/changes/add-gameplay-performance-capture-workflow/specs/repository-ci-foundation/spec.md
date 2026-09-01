## ADDED Requirements

### Requirement: 仓库策略必须精确允许Performance Controller工程

Repository Policy MUST精确允许`Tools/ThirdPersonPerformanceCapture/ThirdPersonPerformanceCapture.Controller.csproj`作为被跟踪的本地Windows性能采集控制器工程。允许项 MUST只覆盖该项目及其明确源文件，不得宽泛允许Tools目录下其它`.csproj`或Unity客户端生成工程。GitHub基础CI MUST不因此新增Controller build、Player build、WPR、WPA或性能测试job。

#### Scenario: Performance Controller工程被跟踪

- **WHEN** 候选提交包含精确路径的Controller `.csproj`和普通源文件
- **THEN** Repository Policy MUST接受该正式工具工程
- **AND** 现有三个基础CI job及其只读边界 MUST保持不变

#### Scenario: Tools目录出现另一个工程

- **WHEN** 候选提交包含未被current spec精确批准的其它Tools `.csproj`或`.sln`
- **THEN** Repository Policy MUST继续列出该路径并失败
- **AND** MUST不因Performance Controller的允许项扩大匹配范围
