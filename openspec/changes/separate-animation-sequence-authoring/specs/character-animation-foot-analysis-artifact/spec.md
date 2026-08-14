## MODIFIED Requirements

### Requirement: Animation Foot Analysis必须拥有Editor-only规范产物

Animation Foot Analysis MUST为`Animation Sequence identity + AnimationClip imported content + Rig + Sampling Rig prefab + Rig Calibration + Geometry Validation Result + Analysis Settings + Analyzer Version`生成不可变Editor-only规范Artifact。Artifact identity MUST覆盖Sequence identity以及精确Clip、Rig、Analysis Source与全部生成依赖。Artifact MUST写入固定Library存储根，不得进入Assets、Player、Addressables、Program、Snapshot或Network，也不得写回Sequence、Timeline、Profile或Blend Space。相同Sequence输入 MUST产生相同artifact identity与规范payload；两个Sequence即使引用同一AnimationClip，只要Rig、Analysis Source或素材语义不同就不得按Clip名称合并authoring identity。

#### Scenario: Sequence更换AnimationClip

- **WHEN** Run Sequence改为另一精确AnimationClip
- **THEN** 旧artifact MUST变为Stale
- **AND** 系统 MUST不因Profile Binding或Blend Space sample仍引用Run Sequence而继续使用旧Clip数据

#### Scenario: 多个消费者引用同一Sequence

- **WHEN** Pose Source、Blend Space与Action Segment引用同一Run Sequence
- **THEN** Build MAY复用该Sequence同一Ready artifact payload
- **AND** 三个消费者 MUST不创建自己的Marker候选或分析配置副本

#### Scenario: Calibration几何改变

- **WHEN** Heel、Toe、Sole Frame、Preferred Bend或Geometry Validation identity改变
- **THEN** 旧Sequence artifact MUST变为Stale
- **AND** Analyzer MUST不因Sequence identity仍相同继续使用旧feature

#### Scenario: 同一合法输入重复分析

- **WHEN** 相同Sequence、Clip、Analysis Source与Geometry Validation输入重复构建
- **THEN** 系统 MUST产生相同canonical payload与artifact hash
- **AND** Store MUST解析到同一规范identity

#### Scenario: AnimationClip重新导入

- **WHEN** Sequence引用的AnimationClip GUID不变但import dependency改变
- **THEN** expected artifact identity MUST改变并把旧artifact判为Stale
- **AND** MUST不因Sequence引用未变继续使用旧数据

#### Scenario: Rig腿链改变

- **WHEN** Sequence Rig的腿链、revision或content hash改变
- **THEN** 旧artifact MUST变为Stale
- **AND** Analyzer MUST不使用Sampling Rig旧Transform映射继续发布

### Requirement: Foot Analysis必须生成可校验的接触Marker候选

Ready Artifact MAY从左右脚feature推导离散接触Marker候选。候选 MUST携带Sequence identity、artifact identity/content hash、AnimationClip dependency、脚侧、源动画时间、目标Sequence frame与置信值，并保持Editor session瞬时只读。作者显式Apply MUST重新校验完整Sequence与artifact输入，并通过Sequence正式Marker mutation提交；不得写入Profile Binding、Blend Space sample、Action Timeline Track/Segment或generated Projection。

#### Scenario: 应用Sequence候选

- **WHEN** 作者在Run Sequence文档确认当前Ready候选
- **THEN** Apply MUST只替换Sequence中的对应Left/Right contact Marker集合并保留其它Marker
- **AND** 所有引用Run Sequence的消费者 MUST通过同一Sequence读取结果

#### Scenario: 循环步态生成左右脚候选

- **WHEN** Ready Sequence artifact的左右脚Plant Confidence跨循环边界进入稳定接触
- **THEN** 系统 MUST按实际采样点生成LeftFootContact与RightFootContact候选
- **AND** MUST按Sequence素材时间映射frame，不得假设frame 0或半周期

#### Scenario: 候选输入过期

- **WHEN** Sequence、Clip dependency、Analysis Source、Rig、Calibration、采样参数或artifact hash在候选显示后改变
- **THEN** Apply MUST重新解析并把旧候选判为Stale
- **AND** MUST不按旧frame、名称或缓存曲线继续写入Marker
