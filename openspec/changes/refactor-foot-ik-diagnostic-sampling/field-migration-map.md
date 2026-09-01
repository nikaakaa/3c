# Foot IK字段迁移清单

逐列真相见[current-field-migration-inventory.csv](current-field-migration-inventory.csv)。该清单以固定Record `20260902-020333-815-08bc7f3b04b44d2daa27b8a4919ce442`的正式Header为顺序真相，以现行`CharacterFootSampleColumns.Schema.Columns`为组、类型、单位和availability真相，并记录每列的当前Owner、迁移分类、Analyzer要求与七维评分消费者。

清单包含主表1222列与Ground Geometry 27列，共1249列、29个业务组，列名和表内ordinal零遗漏、零重复。按新框架基础类型折叠XYZ／XYZW组件后得到879个typed字段：701个标量、164个Vector3、14个Quaternion。151个typed字段的231个展开列受34个availability typed字段控制，所有引用都能在同表解析；879个Field identity均满足框架lowercase identity规则且唯一。

现行Analyzer 75通过`CharacterFootSampleColumns.Schema.Bind(indices)`绑定完整主表，Geometry Reader绑定完整27列，因此清单把每列标为`required:facts/75`，不得以“当前某份报告没读取”为由静默删除。`ScoringConsumers`记录七维评分可能消费该组形成的facts；`all:identity`表示全部报告用于连接或证据身份，不表示该字段直接扣分。

| 组 | 列数 | 当前来源Owner | 迁移分类 |
| --- | ---: | --- | --- |
| Identity | 46 | Capture metadata + Constraint Committed Result | capture metadata与运行身份混合 |
| SelectedStep | 5 | Constraint Committed Result | 选择派生 |
| CurrentStep | 19 | Constraint Committed Result | 正式运行字段 |
| IncomingStep | 19 | Constraint Committed Result | 正式运行字段 |
| FormalOutput | 15 | Program Committed Result | 正式Program观察字段 |
| FormalEvents | 20 | Program Committed Result | 正式事件格式化 |
| FormalInput | 20 | Constraint Committed Result | 正式Constraint输入 |
| InputFormalEvents | 20 | Constraint Committed Result | 正式事件格式化 |
| RootLanding | 3 | Constraint Committed Result | 正式运行字段 |
| Timing | 17 | Constraint Committed Result | 正式运行字段 |
| PredictionMotion | 29 | Constraint Committed Result | 正式运行字段 |
| Action | 6 | Constraint Committed Result | 正式运行字段 |
| PrimarySupport | 4 | Constraint Committed Result | 正式运行字段 |
| RootHierarchy | 35 | 旧场景Transform | 必须由Final Publication替换 |
| BodyCorrection | 35 | Constraint Committed Result | 正式运行字段 |
| LandingObservation | 69 | Constraint Committed Result | 正式字段与格式化混合 |
| GroundPath | 61 | Constraint Committed Result | 正式运行字段 |
| MotionCore | 89 | Constraint Committed Result | Sampler纯派生 |
| PathContinuity | 51 | Constraint Committed Result | 正式运行字段 |
| Lifecycle | 64 | Constraint Committed Result | 正式运行字段 |
| OutputStages | 44 | Constraint Committed Result | 正式运行字段 |
| FootMotionSelectedSupportTarget | 22 | Constraint Committed Result | 正式运行字段 |
| ResponseContact | 81 | Constraint Committed Result | 正式运行字段 |
| CurrentSupport | 102 | Constraint Committed Result | 正式运行字段 |
| ResolvedFoot | 98 | Constraint Committed Result | 正式运行字段 |
| Goal | 15 | Constraint Committed Result | 正式字段与纯派生混合 |
| Pelvis | 140 | Constraint + Final Publication Committed Result | 正式字段与纯派生混合 |
| SolverPhysical | 93 | Constraint + Final Publication + 旧场景Transform | 正式字段、纯派生与旧Transform混合 |
| GroundGeometry | 27 | Constraint Committed Result | 固定容量子表投影 |

## 必须先补的上游合同

- `RootHierarchy`的35列不能迁入新Sampler。旧链从`CharacterRootHierarchyBinding`读取场景Transform；新链必须由Final Publication Committed Result直接提供所需Root／Physical世界空间事实，随后删除整组旧Transform输入。
- `SolverPhysical`的世界Ankle旋转／位置、Heel／Toe接触点与部分残差目前由旧Sampler把Component事实经Root Transform换算。Final Publication必须直接发布对应世界空间Physical结果；纯残差和Contact Pose再由Foot Extractor只读同一View计算。
- `Identity`中的Sample identity、开始UTC、目标Runtime／Host identity属于Capture metadata；Program／Projection／PoseGraph／Rig／Tuning与Frame／Completion属于View lineage。两类输入必须在Generated Program调用时显式分开，不能重新查询Runtime Target。
- `MotionCore`、`SelectedStep`以及Goal／Pelvis／SolverPhysical中的派生项必须成为Foot插件AOT-safe纯Extractor；它们只读同一View及固定Capture metadata，不回写Runtime，也不进入下一帧。
- `GroundGeometry`保持一个固定容量子表。行数仍取Surface、Contact与Envelope三类正式计数的最大值，每行用各自availability／index表达缺项，不把27列重新塞回主表。

## 删除与保留边界

新Schema必须以CSV中的`TypedFieldId`、`TypedValueKind`、`Component`、`AvailabilityFieldId`和表内ordinal生成。旧`ColumnName`只用于Host输出字段名与迁移对账，不得继续成为Runtime字符串路径。旧封存目录保持不可变；完成新Reader／Host Adapter对账后，现行`CharacterFootCsvColumn/Group`、`RootHierarchyCapture`、手写Geometry Header与旧Reader绑定整体删除，不保留兼容分支。
