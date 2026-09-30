## RENAMED Requirements

- FROM: `### Requirement: Pose必须提供供角色外壳调用的阶段结果`
- TO: `### Requirement: Pose必须提供供角色外壳调用的完整帧入口`

## MODIFIED Requirements

### Requirement: Pose必须提供供角色外壳调用的完整帧入口

Pose领域 MUST独立提供原生图／资源准备、实例创建、完整帧执行、停止和已完成观察接口。角色表现外壳 MUST调用完整帧入口并消费最终typed结果，保持同一表现时钟、唯一Animancer Barrier和最终输出边界。source demand准备、姿态求值、Pending检查、提交与丢弃 MUST由Pose内部唯一帧协调点组织；外壳不得重新排列阶段、解释Pose内部操作或复制图状态。实际采用的图版本、实例与ResetGeneration MUST由Pose owner确认，核心只汇集。

#### Scenario: 核心装配已准备的Pose实例
- **WHEN** Pose owner返回合法实例及其实际内容版本
- **THEN** 角色外壳 MUST调用正式完整帧入口并消费typed结果，不创建另一份图或Image
- **AND** Barrier前失败按原规则Discard，Barrier内或之后失败按同一Actor故障归属阻止后续帧；Pose成功后的外围业务提交失败也必须进入该故障归属
