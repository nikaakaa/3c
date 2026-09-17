## MODIFIED Requirements

### Requirement: Authoring节点与Runtime执行描述必须分离

领域框架 MUST以唯一作者 identity、业务字段、typed ports 和 Mutation 提供 UI 与 C# 作者 API；执行方式 MUST由领域决定。Skill 作者图继续生成独立技能数据，Pose 与 EventGraph 使用原生运行实例。原生运行节点可以与作者节点同型，但可变状态只属于实例。运行时 offset、缓冲索引、缓存和 resource handle MUST不成为作者参数，也不得为原生 Pose 另编 IR。

#### Scenario: Runtime增加优化字段

- **WHEN** Pose Runtime为执行计划增加内部offset或buffer index
- **THEN** Authoring capability、Details与C#作者参数 MUST不自动暴露该字段
- **AND** 对应节点或资源模块 MUST在实例初始化时准备该内部值，不生成Pose IR
