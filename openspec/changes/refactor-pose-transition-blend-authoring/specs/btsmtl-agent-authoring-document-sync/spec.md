## ADDED Requirements

### Requirement: Pose Transition混合JSON必须表达可解析业务配置

Pose StateMachine `state-machine.json`中的每条Transition MUST使用`blendLogic`、`durationSeconds`、`blendMode`、条件式`customBlendCurveAssetId`与`blendProfileAssetId`表达混合配置。`customBlendCurveAssetId`只允许在`blendMode=Custom`时出现；其它模式禁止该字段。Curve/Profile identity MUST解析到只读Asset Catalog中类型、identity与revision都匹配的正式资产，并由Reconciler降低为与人工UI相同的typed Presentation Mutation。旧`blendCurveId`、旧`blendProfileId`、Unity GUID、资产路径、compiled curve/profile index与曲线key正文 MUST不进入Transition JSON。

每个State JSON MUST包含必填布尔字段`alwaysResetOnEntry`。Transition JSON MUST不包含`targetResetPolicy`或`sourceSyncMode`，Pose Graph Sequence Player property MUST不包含`reset-on-entry`。Reconciler MUST把State字段降低为State级typed Presentation Mutation；同步计划 MUST只由Projection根据Profile Pose Source Binding编译，不进入Document Transition目标。

#### Scenario: AI配置Custom transition curve

- **WHEN** AI把一条Pose Transition设为Custom并引用Catalog中的Curve Asset identity
- **THEN** dry-run MUST解析强类型Curve资产并显示业务级planned diff
- **AND** apply MUST通过唯一Presentation Mutation修改Transition owner

#### Scenario: AI提交旧字符串字段

- **WHEN** Transition JSON包含`blendCurveId`或旧`blendProfileId`
- **THEN** strict parser MUST在Reconciler前拒绝该文件
- **AND** MUST不按名称、GUID或默认资源迁移该值

#### Scenario: Profile与当前Rig不匹配

- **WHEN** `blendProfileAssetId`解析到不同RigId或revision的Profile
- **THEN** dry-run或全域Validator MUST定位Transition与Profile identity并失败
- **AND** MUST不使用Uniform、null或其它Profile继续apply

#### Scenario: Curve资产内容被人工修改

- **WHEN** 已引用Curve Asset的revision或canonical内容相对checkout context发生变化
- **THEN** Document同步状态 MUST反映context变化并要求重新checkout或显式rebase
- **AND** MUST不让旧Document hash覆盖新曲线内容

#### Scenario: AI修改State重新进入策略

- **WHEN** AI修改State的`alwaysResetOnEntry`
- **THEN** dry-run MUST显示该State进入生命周期的业务级diff
- **AND** apply MUST通过唯一State typed Mutation更新StateMachine revision

#### Scenario: AI提交旧Reset或Sync字段

- **WHEN** Transition包含`targetResetPolicy`、`sourceSyncMode`或Sequence Player包含`reset-on-entry`
- **THEN** strict parser MUST在Reconciler前拒绝该分片
- **AND** MUST不忽略、迁移或按旧值推导State配置
