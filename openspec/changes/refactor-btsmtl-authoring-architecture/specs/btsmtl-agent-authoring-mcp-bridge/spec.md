## MODIFIED Requirements

### Requirement: MCP bridge必须透传同一Document Character与AI事务

五个BTSMTL lifecycle tool MUST接受并返回`btsmtl-agent-authoring-document.v5`同步与validation结果，并通过显式domain透传CharacterController或AIController generic事务。Character package MUST覆盖控制binding／参数、SkillDefinition、技能局部State、ActionProfile、Timeline、Timeline-local Curve、AnimationClip注册Curve、Node、Edge、direct Clip Binding、Locomotion Sync Group与Presentation owner可写语义；AI package MUST继续覆盖Definition、Graph、Blackboard、Perception、Observation、Memory与Character input/request intent binding。Bridge MUST只调用统一Store、Reconciler、Mutation、transaction和Validator，不得新增domain专用action、Node级tool、Clip级tool、Pose专用tool、Patch JSON、YAML、反射、任意字段写入或旧schema转换。

#### Scenario: dry-run发现Clip Curve分片

- **WHEN** Character Document包含manifest声明且引用现有原生Clip的Curve分片
- **THEN** dry-run MUST返回锁定完整manifest的exact document hash与Clip Curve Mutation计划
- **AND** apply MUST只接受该exact hash，并在成功reverse export后发布canonical package
- **AND** Bridge MUST不增加Clip路径或curve key参数

#### Scenario: AI Document修改Intent binding

- **WHEN** AI package增加合法Character input/request binding
- **THEN** bridge MUST把同一v5整包交给统一service
- **AND** response MUST返回Mutation Plan、事务与Validator机器报告

#### Scenario: bridge收到旧schema

- **WHEN** 调用方提交v1/v2/v3/v4、v15-v17 Snapshot/Patch、operation或`patch_json`
- **THEN** bridge MUST返回unsupported schema或unsupported parameter
- **AND** MUST不转换为v5文档包
