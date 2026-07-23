## MODIFIED Requirements

### Requirement: Animation Clip Foot Placement曲线必须沿正式表现投影采样

每个Timeline Animation Clip MUST继续唯一保存一条可写`Foot Placement Weight`曲线，表达Foot Placement总体介入量。左右脚sole速度、高度、plant confidence与landing feature MUST由Editor-only artifact生成并在Definition Build时嵌入Projection；它们不得成为Timeline Track lane、editable Curve Channel、Undo数据、Blackboard或Agent Patch字段。Player Runtime MUST继续只按visible producer最终采用的raw/effective VisualSampleTime采样Projection feature并与作者Weight组合。

#### Scenario: 编辑Foot Placement Weight

- **WHEN** 作者在Timeline编辑Foot Placement Weight
- **THEN** Timeline MUST只修改该Animation Clip作者曲线
- **AND** generated artifact MUST不被当作可写曲线同步修改

#### Scenario: 查看generated feature

- **WHEN** 作者通过Animation Analysis面板查看Plant metric
- **THEN** 面板 MUST读取精确artifact并保持只读
- **AND** AnimationTrack主行与CURVES分组 MUST不增加generated channel

#### Scenario: Runtime采样显式MarkerSync后的时间

- **WHEN** Pose Graph中的显式MarkerSync节点改变某visible producer的VisualSampleTime
- **THEN** Foot Placement MUST按该时间采样Projection feature
- **AND** MUST不读取MarkerId作为plant/contact真相
