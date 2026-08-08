## ADDED Requirements

### Requirement: Document v3 Blackboard JSON必须分离基础声明、输入绑定和事实投影

Document v3的Blackboard declaration MUST只表达稳定identity、key、valueType、defaultValue、owner、scope、lifetime、categoryPath，以及可选`inputBinding`与可选`factProjection`。`inputBinding` MUST只包含非空`inputValueId`；`factProjection` MUST按kind保存该projection所需的typed payload。没有binding或projection时writer MUST省略对应对象。`authority`、`syncPolicy`、旧平铺`inputId`、`factProjection`字符串和`windowType/windowId/digest` MUST是未知字段。

#### Scenario: checkout ActionTarget declaration

- **WHEN** Character Document checkout包含ActionTargetSnapshot输入绑定
- **THEN** editable Blackboard declaration MUST输出`inputBinding.inputValueId`
- **AND** MUST不输出ClientPredicted、InputDerived或其它策略标签

#### Scenario: checkout ActionWindow declaration

- **WHEN** Character Document checkout包含Attack1Hit projection
- **THEN** editable Blackboard declaration MUST输出独立ActionWindow factProjection payload
- **AND** MUST不输出SyncFact

#### Scenario: package包含旧字段

- **WHEN** strict parser读取包含`authority`或`syncPolicy`的旧v3 package
- **THEN** MUST在Reconciler和Mutation前拒绝整个package
- **AND** 调用方 MUST显式重新checkout，不得转换或删除字段后静默继续

### Requirement: Blackboard schema normalization必须进入唯一Document事务

Document v3 checkout、Snapshot、Reconciler和Application Service MUST识别RootTree正式Blackboard authoring schema revision。旧revision应用新package时，Reconciler MUST生成唯一typed normalization plan，重写基础declaration、Input Binding与Fact Projection，并在同一Undo、Validator、Save、reverse export和package publish事务中更新revision。系统 MUST不直接修改Unity YAML、不通过OnValidate或AssetPostprocessor自动迁移，也 MUST不在已迁移revision重复全量重写。

#### Scenario: 迁移Corin RootTree

- **WHEN** 新Document package应用到旧Blackboard schema revision的Corin RootTree
- **THEN** plan MUST覆盖全部受影响Blackboard declaration和精确owner
- **AND** apply成功后的Unity资产与reverse-export package MUST只包含新schema

#### Scenario: normalization后Validator失败

- **WHEN** typed normalization已经修改部分owner但全域Validator失败
- **THEN** Application Service MUST回滚全部owner与schema revision
- **AND** 正式package MUST保持apply前内容且不得报告Clean

#### Scenario: AI纯schema normalization遇到过期Character Program

- **WHEN** AI RootTree只执行Blackboard schema normalization、AI authoring source revision未变化且受控Character Program已过期
- **THEN** apply MUST验证并保存AI authoring、更新schema revision并反向发布Clean package
- **AND** MUST不加载旧Numeric Target、不自动Build Character或发布AIIntentProgram
- **AND** context MUST继续明确记录Character Program与AIIntentProgram为stale
- **AND** 任一真实AI authoring语义变化仍 MUST要求当前Character Program并通过正式AI Compiler
