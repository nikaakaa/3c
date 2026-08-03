## ADDED Requirements

### Requirement: Character Document Apply必须覆盖唯一Presentation authoring owner

现有Character Document生命周期bridge MUST让checkout、status、rebase、dry-run与apply处理Document v3中的Gameplay、Timeline和Presentation完整闭包。apply MUST把Presentation差异交给正式Presentation Reconciler与Mutation，并与其它领域共享一个资产级事务。Bridge MUST不新增Pose专用action、工具或资产写路径。

#### Scenario: dry-run Presentation变化

- **WHEN** Document v3只修改Pose Graph与Profile binding
- **THEN** dry-run MUST返回Presentation mutation计划、诊断与受影响owner
- **AND** MUST不修改Unity资产或generated Projection

#### Scenario: dry-run接纳新增Pose Graph分片

- **WHEN** Store发现严格合法的canonical `local:*` graph/layout完整文件对
- **THEN** bridge MUST返回锁定有效manifest闭包的exact document hash与Create Pose Graph计划
- **AND** apply MUST复用同一Store发现规则并在reverse export后发布canonical manifest
- **AND** Bridge MUST不暴露manifest编辑参数或Pose专用生命周期工具

#### Scenario: apply跨领域变化

- **WHEN** Document v3同时修改Gameplay Graph、Timeline与Pose Graph
- **THEN** bridge MUST调用唯一Document application service完成一个事务
- **AND** 任一Presentation mutation失败 MUST回滚整个事务

### Requirement: MCP生命周期工具集不得因Pose authoring扩张

Bridge MUST继续只暴露Document v3规定的五个生命周期动作，并通过同一参数、状态机和机器可读诊断承载Presentation authoring。系统 MUST删除旧v2识别和任何Pose-specific patch/tool注册，不得以便利为由建立第二入口。

#### Scenario: 调用Pose专用工具名

- **WHEN** 调用方请求不存在的Pose create、patch或apply action
- **THEN** bridge MUST拒绝未知action
- **AND** MUST引导调用方使用Character Document checkout、编辑、dry-run与apply闭环

### Requirement: Character Document bridge必须准确报告事务结果

Bridge MUST只在Application Service已经保存全部authoring并原子发布最终Document v3 package后报告`applied=true`、`saved=true`与`Clean`。Mutation、Validator、保存、反向导出或package发布失败时，Bridge MUST返回机器可读失败阶段、回滚结果与apply前同步状态，不得把已回滚请求报告为部分成功。

#### Scenario: package发布阶段失败

- **WHEN** apply已执行Mutation但最终package原子发布失败
- **THEN** bridge MUST等待Application Service完成Unity owner回滚
- **AND** 返回结果 MUST为`applied=false`、`saved=false`且不包含`Clean`
