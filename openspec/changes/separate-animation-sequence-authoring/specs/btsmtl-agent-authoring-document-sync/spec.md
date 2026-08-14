## MODIFIED Requirements

### Requirement: 物理分片不得改变Document整包同步语义

文档包 MUST通过service-owned manifest声明Graph、Timeline、Animation Sequence、Curve、领域配置与只读context的唯一规范文件清单。AI MAY只读取和修改相关editable文件，但checkout、rebase、dry-run、apply、Conflict、hash锁定与反向导出 MUST始终以整个Document包为唯一提交单元。系统 MUST不提供文件级baseline、dirty、apply或Conflict。新增Pose Graph时 MUST继续使用完整`graph.json + layout.json`创建对；新增Animation Sequence时 MUST使用完整`sequence.json + curves.json`创建对。两类创建都必须使用canonical segment和`local:*`计划identity，由Store形成当前请求有效manifest；AI不得修改manifest。apply或显式rebase成功后只能由service发布新的canonical manifest。

#### Scenario: 新建Sequence并被多个owner引用

- **WHEN** AI创建完整`local:*`Sequence文件对并让Profile Binding与Timeline Segment引用它
- **THEN** Store MUST把文件对和全部引用纳入同一有效manifest与document hash
- **AND** apply MUST在一个Undo事务中创建Sequence并替换全部local引用

#### Scenario: AI只修改一个Graph文件

- **WHEN** AI只改动一个`graph.json`
- **THEN** 整个Document MUST进入DocumentDirty且dry-run MUST重新读取完整包
- **AND** apply MUST不允许只提交该Graph文件

#### Scenario: AI新增Pose State Graph分片

- **WHEN** AI在canonical segment中提交严格合法的`local:*` graph/layout完整创建对
- **THEN** Store MUST继续把该创建对纳入同一editable/document hash与Presentation Reconciler
- **AND** Sequence文件对能力 MUST不改变Pose Graph原有创建语义

## ADDED Requirements

### Requirement: Sequence结构与Curve payload必须分离

每个Animation Sequence MUST以同目录`sequence.json`表达identity、owner、AnimationClip/Rig引用、Loop/Finite、默认倍率、Marker Sync、Notify与Analysis Source，并以`curves.json`表达registered素材Curve完整payload。两文件 MUST共享同一Sequence identity并进入同一manifest；缺失、重复、跨目录identity不匹配或未知channel MUST严格失败。Profile、Blend Space和Timeline分片 MUST只保存Sequence对象引用，不得复制Sequence正文。

#### Scenario: Sequence缺少curves文件

- **WHEN** manifest声明的Sequence目录只有`sequence.json`
- **THEN** strict parser MUST拒绝整个Document
- **AND** MUST不以空曲线、Profile旧字段或Timeline旧字段补齐

#### Scenario: Timeline引用未知Sequence

- **WHEN** Sequence Segment引用既不在正式asset catalog也不在当前Document `local:*`计划中的Sequence
- **THEN** Reconciler MUST在preflight前报告精确引用错误
- **AND** MUST不按AnimationClip或资源名解析替代Sequence

## MODIFIED Requirements

### Requirement: Timeline结构与Curve payload必须分离

每个Timeline目录 MUST使用`timeline.json`表达Timeline、Track、Sequence Segment、Section、Window、Cue、Motion、Warp、Decision、TreeClip、ownership和Sequence引用，并使用`curves.json`表达Timeline-local registered Curve完整payload。Timeline分片 MUST不保存Sequence Marker、Notify、素材Curve或Analysis Source。Curve MUST只保存对应Timeline owner正式语义；AI修改Curve MUST提交完整目标状态，不得依赖key级MCP操作。

#### Scenario: Action Timeline包含Sequence Segment

- **WHEN** checkout导出引用Attack Sequence的Action Segment
- **THEN** `timeline.json` MUST输出Segment identity、范围、ClipIn与Sequence对象引用
- **AND** Attack素材Marker与Curve MUST只出现在Sequence文件对

#### Scenario: Timeline提交Sequence素材字段

- **WHEN** `timeline.json`包含Segment Marker或Sequence Notify正文
- **THEN** strict parser MUST拒绝未知字段
- **AND** MUST不把它们迁入Timeline Curve或Point集合

#### Scenario: AI只修改weighted curve

- **WHEN** AI只替换Timeline Segment或其它Timeline owner的registered weighted curve
- **THEN** `curves.json` MUST保存完整wrap、key、tangent与weight语义
- **AND** Reconciler MUST通过该Timeline owner正式Curve Mutation提交而不修改Sequence curve

## ADDED Requirements

### Requirement: Document v3必须原子替代旧Sequence素材字段

Sequence文件对安装后，旧Timeline Track Marker素材字段、旧Timeline Clip裸AnimationClip/素材Curve字段、旧Profile Sequence Binding素材字段和旧Blend Space sample Marker字段 MUST从strict schema、canonical writer、Exporter、Reconciler、Mutation与Validator中删除。系统 MUST不保留兼容parser、双写、默认迁移或按旧字段优先级选择owner。

#### Scenario: 旧package提交Binding Marker

- **WHEN** Document包含已删除的Profile Sequence Binding Marker字段
- **THEN** strict parser MUST以未知字段拒绝整个package
- **AND** report MUST要求先checkout新的Sequence schema

### Requirement: Sequence引用必须进入完整Mutation Plan

Reconciler MUST把Animation Sequence、Profile Binding、Blend Space sample与Timeline Segment完整目标集合确定性降低为一个immutable Mutation Plan。Sequence创建必须先建立planning symbol，再允许同一plan中的Binding/sample/Segment引用；Mutation handler MUST分别调用正式Sequence、Presentation、Blend Space与Timeline authoring API。任一Sequence内容或引用失败 MUST拒绝整个plan，不得只提交已成功owner。

#### Scenario: Sequence合法但Segment范围非法

- **WHEN** Document新建Sequence同时提交引用它的非法Timeline Segment
- **THEN** dry-run MUST拒绝整个Mutation Plan
- **AND** apply MUST不创建孤立Sequence资产

### Requirement: Presentation分片必须只保存Sequence引用

Presentation Profile分片 MUST把Sequence Binding表达为稳定Source Slot与精确Sequence对象引用。Blend Space sample目标 MUST只保存Sequence引用和sample-owned字段。Sequence Marker、Curve、Notify与Analysis Source正文 MUST只存在于Sequence文件对；Presentation Exporter、Reconciler与typed Mutation MUST不投影或接受旧Binding/sample素材字段。

#### Scenario: checkout已有Run Sequence Binding

- **WHEN** Character Document导出Profile中的Run Source Slot
- **THEN** `profile.json` MUST输出Sequence对象引用，Sequence文件对 MUST输出其素材正文
- **AND** 两处 MUST不重复Marker、Curve或Notify
