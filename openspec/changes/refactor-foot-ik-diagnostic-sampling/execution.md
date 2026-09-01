# 执行记录

## 冻结评分Owner

`consolidate-foot-diagnostic-scoring`任务5.3已由独立提交`df146d35b`完成，唯一格式冻结为`character-foot-motion-facts/75`、Analyzer 75、`character-foot-diagnosis-file/44`、`character-foot-quality-score/4`与`foot-quality-seven-dimensions/3`。固定Record基线为`Diagnostics/FootPlacementRuns/20260902-020333-815-08bc7f3b04b44d2daa27b8a4919ce442`，1043表现帧、2086脚行、67186 Geometry行，总分84.2、weighted evidence 96.5。后续Full Host Adapter只迁移同一字段、资格、分母、规则、权重与Publisher语义，不再并行修改旧Analyzer／Publisher数学。由此完成任务1.3。

## 建立Foot Capability与typed lineage边界

新增独立`ThirdPersonCharacter.FootIkDiagnosticSampling`程序集，依赖方向固定为Foot插件单向引用`ThirdPersonClient.Runtime`与`ThirdPerson.GeneratedDiagnosticSampling`。`CharacterFootIkDiagnosticCapability`通过框架`DiagnosticCapabilityAttribute`声明`character-foot-ik` revision 1，并显式把PoseGraph-owned `CharacterFootIkCommittedCaptureViewLease`登记为Committed View输入；PoseGraph类型及其程序集没有新增框架引用，Foot插件也没有定义第二View、Capture Frame或事实页。

Foot领域lineage identity为`character-foot-ik-lineage/1`。`CreateSampleKey`只在View短租约内读取已经由PoseGraph核对完整Frame／Completion／Program／Projection／Rig／Tuning的`CharacterPoseFrameLineage`，拒绝无效lineage，并把Frame Identity与Completion Identity编码为框架不解释的opaque key；它不访问Module、Bank、Workspace、Vendor、Transform、World Query或写权限。由此完成任务3.1。

Unity导入新程序集并生成正式meta与csproj；`ThirdPersonCharacter.FootIkDiagnosticSampling.csproj`使用规定参数构建成功，27个警告只来自既有Unity／第三方依赖与未使用字段，0错误，构建后立即关闭build server。3C Unity编译没有Foot插件或Generated Sampling错误。同期通用框架Host文件写入曾使SourceAssetDB报告两条修改时间不一致；在对应框架提交`e98d86425`完成后重新force refresh，错误消失，剩余仅既有warning和FinalIK Domain Reload日志并已清空。

## 固结现行字段迁移清单

从固定Record正式Header与现行`CharacterFootSampleColumns.Schema.Columns`逐字段对账，生成`current-field-migration-inventory.csv`与`field-migration-map.md`。清单包含主表1222列、Ground Geometry 27列、共1249列和29组；折叠CSV组件后为879个typed字段，其中701标量、164 Vector3、14 Quaternion。151个typed字段的231个展开列引用34个availability typed字段，表内ordinal、Column、typed Field identity与availability引用检查均为0错误。

现行Analyzer 75仍通过唯一Schema绑定完整主表与Geometry，因此1249列全部登记为`required:facts/75`；每行同时登记当前来源Owner、运行／派生／格式化／旧Transform分类和七维评分消费者。清单明确发现35个`RootHierarchy`列与93个`SolverPhysical`混合列仍经过旧场景Transform，不能直接接入新Sampler；它们先由PoseGraph Final Publication补齐世界空间Physical事实，随后旧Transform输入整体删除。由此完成任务1.4，并给任务2.4、3.2、3.6、3.7、5.1至5.4提供同一零遗漏迁移真相。

PoseGraph提交候选已把`PoseGraphId`与`PoseGraphRevision`补入唯一`CharacterPoseFrameLineage`，具体Foot View因此能随同Frame／Completion／Program／PlanHash／Projection／Rig／Tuning读取完整上游身份，不再从Runtime Target补查。固定Record `024336`相对`020333`的Proof为`matched:1044`，Foot与Geometry业务列0冲突、评分不变。任务2.1仍等待specific `CharacterFootIkCaptureInterest`冻结后统一验收，不在此提前勾选。

## 建立View与Capture metadata双输入字段边界

`CharacterFootIkDiagnosticCapability`已登记PoseGraph-owned `CharacterFootIkCommittedCaptureViewLease`与Foot-owned `CharacterFootIkCaptureMetadata`两个不同输入类型。metadata只在Capture开始构造一次，保存Sample identity、UTC ticks、Target Runtime identity、Host identity及组合Program identity；它不保存View、Module、Workspace、Vendor或Transform。首批11个Field Extractor覆盖上述metadata以及View lineage中的Projection、PoseGraph、PlanHash、Frame和Completion，全部使用框架Attribute与`(in View, in Metadata)`普通静态签名。

通用Generator Probe已验证生成ABI为`Capture(in View, in Metadata, ref Packet)`；Foot插件用同一Generator实际编译成功，27个既有warning、0错误并关闭build server。3C Unity force refresh确认新Analyzer、`DiagnosticCompilationClosure`、Foot metadata type与双输入Field签名均无编译错误。字段清单同时把103个Category文本列收紧为领域枚举Int32、把`SampleStartedUtc`收紧为UTC ticks Int64；Host后续负责恢复稳定Category文本与`O`格式，不让Player逐帧格式化字符串。任务3.2仍等待其余868个typed字段及Geometry表全部声明后统一勾选。

## 验收独立Post-Seal Foot View Projector

PoseGraph已新增唯一`CharacterFootIkCommittedCaptureViewProjector`并把Foot页、独立Lease、Foot／Solver／Physical组合与Foot Step Observation解析全部移出万能Snapshot Publisher。Projector只在成功Seal后的Committed diagnostics入口运行，逐项核对根Execution、Actor、Constraint与Final Publication lineage；Discard／Reset／Fault发布失败和双页复用都会使对应Lease失效。万能Snapshot只引用同一Lease供旧UI读取，不再保存或复制Foot事实页。由此完成任务2.3。

固定Record `030456`相对`024336`的Proof为`matched:1044`；主表1198业务列和Geometry 22业务列逐值一致，其余identity列一一映射且0冲突，评分84.2、weighted evidence 96.5不变。旧Foot Sampler仍等待Foot事件并读取Snapshot外层metadata，任务2.4、4.2与7.1继续待迁。

PoseGraph已建立`CharacterFootIkCaptureInterest`、单一owner consumer登记和`CharacterPoseFrameTransaction`帧开始冻结合同；Foot插件后续只需把已编译Program descriptor归一化为容量1的typed interest，不把Sampler或packet传给PoseGraph。默认未登记固定回放`031403`相对`030456`为`matched:1044`，Foot／Geometry业务列0冲突、评分不变。任务2.1、2.2与2.5仍等待specific interest真正传播到Constraint、Final Publication与独立Projector后统一验收。

specific Foot interest现已传播到Actor foot observation、Constraint Foot／FBBIK诊断页与独立Foot Projector；Foot-only不创建Source、Program、Linked、Operation、Pose Watch或万能Snapshot页面，并在成功Seal后把唯一租约同步交给冻结consumer。结合完整lineage、容量1 interest、独立View Lease及Foot插件只有`typeof(CharacterFootIkCommittedCaptureViewLease)`引用且没有第二View，完成任务2.1与PoseGraph任务13.2。任务2.2仍等待Final Publication世界空间扩展事实改为specific-interest gated后勾选，任务2.5仍等待正式Foot Program Definition。

默认未登记固定回放`032312`相对`031403`为`matched:1044`；主表与Geometry业务列0冲突，评分84.2、weighted evidence 96.5保持。该证据确认新增specific路径没有改变Disabled／旧general链的Foot、Goal、FBBIK、Final Pose或Physical结果。

## 封存Root世界事实并迁移RootHierarchy字段

Final Publication Pending页现保存根Frame冻结的specific Foot interest。唯一Physical Writer只在该interest开启时，于最终骨骼写入完成后一次封存LogicRoot世界姿态、VisualRoot局部／世界姿态、PoseRoot局部／世界姿态及双脚踝最终世界姿态；具体Foot View从Committed页公开这些只读值。Foot Projector与Foot插件目录搜索均不含`Transform`、`RootHierarchy`、`PhysicalBones`或Animator骨骼查询，因此完成任务2.2与2.4。固定回放`033516`相对`032312`为`matched:1044`，主表1198业务列与Geometry 22业务列逐值一致，identity一一映射且0冲突，总分84.2、weighted evidence 96.5不变。

`CharacterFootIkDiagnosticFields`已按字段迁移清单新增10个RootHierarchy typed Extractor：LogicRoot世界位置／旋转、VisualRoot局部与世界位置／旋转、PoseRoot局部与世界位置／旋转。Extractor只把View中的Unity值投影为框架`DiagnosticVector3`／`DiagnosticQuaternion`，不接触场景对象，也不重新计算空间变换。Foot插件工程通过实际Source Generator构建，27个warning均来自既有Unity／第三方依赖，0错误并已关闭build server。当前共21个已迁移typed字段；任务3.2仍等待其余858个typed字段和Geometry表完成后统一勾选。
