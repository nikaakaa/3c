## ADDED Requirements

### Requirement: Agent Authoring Document v3必须把Presentation作者内容纳入Character editable

Character Document MUST升级为唯一`btsmtl-agent-authoring-document.v3`并把CharacterAnimationPresentationProfile、Pose Graph、PoseStateMachine、Pose State、Pose Transition、Pose source binding、AnimationSlot和Presentation Policy纳入`editable/presentation/`。AI可写范围 MUST只包含正式Presentation authoring owner；Rig资源正文、generated Projection、Foot Analysis artifact、Native Program与runtime snapshot MUST保持只读context或省略。

#### Scenario: checkout Character文档

- **WHEN** Agent对合法Character Definition显式checkout
- **THEN** editable MUST包含Profile摘要、Pose Graph与PoseStateMachine的稀疏typed作者数据
- **AND** context MUST只读包含所需Rig、resource capability与generated状态摘要

#### Scenario: 修改generated字段

- **WHEN** AI修改Projection、compiled index、runtime state或Rig资源正文
- **THEN** parser MUST拒绝该变化
- **AND** MUST不把变化降低为Presentation Mutation

### Requirement: Presentation分片必须保持整包同步与稳定owner

Document v3 MUST使用`editable/presentation/profile.json`、`editable/presentation/pose-graphs/<graph-id>/graph.json`、对应`layout.json`和`editable/presentation/pose-state-machines/<state-machine-id>/state-machine.json`表达Presentation目标状态。分片 MUST通过稳定owner identity互相引用，并继续服从整包checkout、hash、dry-run、apply、Conflict与反向导出语义；不得提供文件级apply。新增Pose State Graph或Subgraph MUST使用`local:<meaningful-id>`并同时创建canonical segment目录内的`graph.json`与`layout.json`；Store MUST严格解析完整文件对并形成当前请求的有效manifest，AI不得编辑service-owned manifest。

#### Scenario: AI只修改一个Pose节点

- **WHEN** 仅一个Pose Graph分片发生语义变化
- **THEN** dry-run与apply MUST仍锁定并处理整个Document包
- **AND** 反向导出 MUST更新整包基线与规范文件清单

#### Scenario: AI新增Pose State Graph分片

- **WHEN** AI新增严格合法的canonical `local:*` graph/layout完整文件对
- **THEN** dry-run MUST把服务接纳后的有效manifest闭包纳入exact document hash并生成Create Pose Graph计划
- **AND** 任意缺失配对、非canonical目录、非local graph identity、root role或其它manifest外文件 MUST继续严格失败
- **AND** apply成功后的reverse export MUST发布stable identity分片和service-owned canonical manifest

### Requirement: Presentation JSON必须由共享Capability生成稀疏typed字段

Pose Graph node、port、field、StateMachine页面和Profile binding的JSON合同 MUST来自Graph Authoring Domain Framework的同一Authoring Capability Catalog。每个node MUST只包含当前capability有意义的typed payload与node-local动态port；MUST不输出C#类型、SerializedProperty path、runtime枚举载荷或联合体空字段。

#### Scenario: Sequence Player JSON包含IK字段

- **WHEN** Sequence Player payload包含TwoBoneIK字段或未知property
- **THEN** strict parser MUST在Reconciler前拒绝该分片
- **AND** MUST不忽略字段或保留扩展字典

### Requirement: Presentation Reconciler必须调用唯一Presentation Mutation

Document v3 Reconciler MUST按owner依赖生成类型化Presentation Mutation计划，并与人工编辑共用validator、资产级transaction、identity allocator、dirty owner与诊断。Reconciler MUST不直接写Unity YAML、SerializedObject路径或generated Projection。

#### Scenario: apply新增Pose State

- **WHEN** 文档目标状态新增Pose State、state-local graph与transition
- **THEN** Reconciler MUST按owner、页面、节点、动态端口、edge与引用顺序生成类型化mutation
- **AND** 任一失败 MUST回滚全部Gameplay、Timeline与Presentation变化

### Requirement: Document v3必须原子替代v2

系统 MUST删除v2 schema、reader、writer、manifest识别、文档包兼容与升级器，只接受v3 Document。已有v2工作目录 MUST要求重新checkout生成v3，不得静默迁移、fallback读取或并存两种apply路径。五个生命周期动作及其事务语义 MUST保持不变。

#### Scenario: 读取v2文档包

- **WHEN** service发现schema为`btsmtl-agent-authoring-document.v2`
- **THEN** 状态查询 MUST报告需要重新checkout
- **AND** dry-run与apply MUST拒绝该文档且不修改资产

#### Scenario: 调用现有生命周期动作

- **WHEN** 调用checkout、status、rebase、dry-run或apply
- **THEN** service MUST按v3处理包含Presentation的整个文档包
- **AND** MUST不新增Pose专用生命周期工具

### Requirement: Document v3失败恢复必须同时覆盖Unity owner与正式package

Application Service MUST在首次Mutation前解析并锁定全部Gameplay、Timeline与Presentation serialized owner，并注册一个完整Undo事务。只有Mutation、全域Validator、Unity authoring保存、最终树反向导出、staging重读与hash校验、正式package原子替换全部成功后，apply才可返回`applied=true`、`saved=true`与`Clean`。任一步失败 MUST恢复全部Unity owner并保留上一份正式package；Character apply MUST不发布Program、Projection或Native Pose Program。

#### Scenario: Presentation Validator失败

- **WHEN** Gameplay和Timeline mutation已经执行，但Presentation Validator发现AnimationSlot引用断裂
- **THEN** Application Service MUST回滚同一事务内全部Gameplay、Timeline与Presentation owner
- **AND** 正式Document package MUST保持apply前内容且响应不得报告`Clean`

#### Scenario: 最终Document反向发布失败

- **WHEN** Unity authoring保存后最终v3 staging package导出、重读、hash校验或原子替换失败
- **THEN** Application Service MUST恢复全部Unity owner并重新保存恢复结果
- **AND** MUST保留上一份正式package并返回`applied=false`与`saved=false`
