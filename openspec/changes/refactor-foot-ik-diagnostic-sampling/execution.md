# 执行记录

## 冻结评分Owner

`consolidate-foot-diagnostic-scoring`任务5.3已由独立提交`df146d35b`完成，唯一格式冻结为`character-foot-motion-facts/75`、Analyzer 75、`character-foot-diagnosis-file/44`、`character-foot-quality-score/4`与`foot-quality-seven-dimensions/3`。固定Record基线为`Diagnostics/FootPlacementRuns/20260902-020333-815-08bc7f3b04b44d2daa27b8a4919ce442`，1043表现帧、2086脚行、67186 Geometry行，总分84.2、weighted evidence 96.5。后续Full Host Adapter只迁移同一字段、资格、分母、规则、权重与Publisher语义，不再并行修改旧Analyzer／Publisher数学。由此完成任务1.3。

## 建立Foot Capability与typed lineage边界

新增独立`ThirdPersonCharacter.FootIkDiagnosticSampling`程序集，依赖方向固定为Foot插件单向引用`ThirdPersonClient.Runtime`与`ThirdPerson.GeneratedDiagnosticSampling`。`CharacterFootIkDiagnosticCapability`通过框架`DiagnosticCapabilityAttribute`声明`character-foot-ik` revision 1，并显式把PoseGraph-owned `CharacterFootIkCommittedCaptureViewLease`登记为Committed View输入；PoseGraph类型及其程序集没有新增框架引用，Foot插件也没有定义第二View、Capture Frame或事实页。

Foot领域lineage identity为`character-foot-ik-lineage/1`。`CreateSampleKey`只在View短租约内读取已经由PoseGraph核对完整Frame／Completion／Program／Projection／Rig／Tuning的`CharacterPoseFrameLineage`，拒绝无效lineage，并把Frame Identity与Completion Identity编码为框架不解释的opaque key；它不访问Module、Bank、Workspace、Vendor、Transform、World Query或写权限。由此完成任务3.1。

Unity导入新程序集并生成正式meta与csproj；`ThirdPersonCharacter.FootIkDiagnosticSampling.csproj`使用规定参数构建成功，27个警告只来自既有Unity／第三方依赖与未使用字段，0错误，构建后立即关闭build server。3C Unity编译没有Foot插件或Generated Sampling错误。同期通用框架Host文件写入曾使SourceAssetDB报告两条修改时间不一致；在对应框架提交`e98d86425`完成后重新force refresh，错误消失，剩余仅既有warning和FinalIK Domain Reload日志并已清空。
