## MODIFIED Requirements

### Requirement: 物理分片不得改变Document整包同步语义

文档包 MUST通过service-owned manifest声明唯一规范文件清单，并 MAY按Graph、Timeline、Curve、领域配置和只读context拆分JSON。AI MAY只读取和修改相关文件，但checkout、rebase、dry-run、apply、Conflict、hash锁定与反向导出 MUST始终以整个文档包为唯一提交单元。新增Pose State Graph或Subgraph时，AI MUST同时创建canonical segment目录内的`graph.json`与`layout.json`；新增Graph-owned Inline Timeline时，AI MUST同时创建canonical segment目录内的`timeline.json`与`curves.json`。两类新增分片都 MUST使用`local:<meaningful-id>`，由Store严格解析完整文件对后形成当前生命周期请求的有效manifest；AI MUST不直接修改manifest。apply成功后的反向导出或显式rebase MUST由service发布新的canonical manifest。

#### Scenario: AI新增Inline Timeline分片

- **WHEN** AI在timeline local identity对应的canonical segment目录中同时新增严格合法的`timeline.json`与`curves.json`
- **THEN** Store MUST把该完整创建对加入当前请求的有效manifest并纳入editable hash与document hash
- **AND** dry-run MUST把它与拥有它的local TimelineNode降低为同一Timeline Reconciler计划
- **AND** apply成功后的reverse export MUST用stable identity发布规范分片与service-owned manifest

### Requirement: 文档包codec必须严格解析并计算整包规范hash

系统 MUST对manifest、sync及每类JSON分片使用唯一strict parser与canonical writer。Parser MUST拒绝重复属性、未知字段、非法kind、非法identity、缺失manifest文件、非有限数值，以及既不在manifest中也未被Store按完整canonical `local:*`创建合同接纳的文件。Store MUST只接纳同目录完整Pose Graph `graph.json + layout.json`创建对，或Graph-owned Inline Timeline `timeline.json + curves.json`创建对；Timeline创建对 MUST具有匹配的local timeline identity、canonical segment、唯一local TimelineNode调用点、local Track/Clip与一致的curves timelineId，不得按目录前缀放宽其它文件。Writer MUST使用UTF-8无BOM、稳定字段顺序、稳定entity顺序与明确数值格式。`editableHash`与`contextHash` MUST由有效manifest中的规范相对路径和逐文件semantic hash计算，`documentHash` MUST锁定schema、domain、root identity及两项内容hash。

#### Scenario: manifest外Timeline文件对不完整

- **WHEN** AI只新增`timeline.json`、使用非canonical目录或让Timeline调用点不指向同一事务中的local TimelineNode
- **THEN** parser MUST拒绝整包
- **AND** MUST不修改manifest、Unity authoring或generated product

## ADDED Requirements

### Requirement: 新增Inline Timeline必须由typed Mutation链一次落地

Reconciler MUST先由controller摘要创建local TimelineNode，再通过`EnsureInlineTimeline`输出Timeline planned identity，并依次创建typed MotionCurve Track、MotionCurve Clip、MotionCurve配置和registered Curve payload。MotionCurve配置 MUST显式保存`CurveId`、`CurveEndFrame`、Contribution Space、Motion Channel、Blend Mode、Priority与ConsumeLowerChannels。Planner MUST按typed planned identity验证依赖顺序；handler MUST只调用正式Timeline authoring API。系统 MUST不手工写Unity YAML、不分成两次apply、不增加Timeline专用MCP入口或第二Reconciler。

#### Scenario: 同一事务创建MovingTurn MotionCurve

- **WHEN** MovingTurn目标同时新增TimelineNode、Inline Timeline、MotionCurve Track、MotionCurve Clip与完整曲线
- **THEN** dry-run MUST生成Node到Timeline、Track、Clip、配置与Curve的有序typed Mutation计划
- **AND** apply MUST在同一资产事务中完成全部修改并反向导出stable identity
- **AND** 任一步失败 MUST完整回滚且不得发布新manifest
