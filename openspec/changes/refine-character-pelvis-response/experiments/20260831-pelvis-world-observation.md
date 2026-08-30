# 持续Goal第一轮：真实骨盆观测闭合

## 裁决与固定对照

保留本轮观测修复。020243相对010821在这条Record上没有运动变化，不把它记作骨盆效果改善。193957仍是固定质量对照，010821及其本次零行为继承版仍有骨盆和Knee代价，持续Goal保持未完成。

Goal继续使用现有OpenSpec、唯一Replay和唯一Diagnostics：一次可证伪小步、规定flags构建、正式加载、同Record回放、独立质量核对、证据保存与中文独立提交。没有另建后台回放器或并发操作Unity，也没有新增测试框架。

本轮没有修改Foot目标/状态/Anchor/查询/旋转、Spring数学或频率、Reach安全余量、Bend、Body/KCC、TrainingEnemy或ZZZ。没有用新评分、阈值或样本分母放行。

## 先纠正测量对象

第三步记录的30个超过50毫米的骨盆步，指相邻帧加权Correction的差，并非30次世界骨盆下降超过50毫米。两者都需要保留，不能互相代替。

历史包在本次已核对的Up、单位比例窗口中，可以用同帧Root与Physical Component点作辅助世界重建。193957、235033、010821的1042个连续帧对，世界Y绝对步长超过50毫米分别45、43、33；P90分别44.673、43.959、42.948毫米，最大均80.210毫米，位于420。

这个历史重建有明确限制：CSV没有Root scale或完整矩阵；部分世界脚字段同样由Sampler变换产生，不能拿它们当独立原生测量；Sampler的Root快照也不是最终Writer冻结值。Hip/Ankle的跨阶段几何复算在数微米内只是这批Up/单位比例记录的佐证，不证明一般非均匀尺度或跨阶段变化。

266是容易误判的例子：010821相对修正下降50.122毫米，但Root同时上升32.314毫米，动画骨盆Component上升约14.289毫米，所以世界骨盆实际只下降3.519毫米。它相对235033的世界上升22.515毫米确实改变了轨迹，但不能叫世界突然下陷5厘米。

| 共享帧 | 235033世界Y步 | 010821世界Y步 |
| --- | ---: | ---: |
| 231 | -3.921 mm | -21.135 mm |
| 266 | +22.515 mm | -3.519 mm |
| 267 | -5.039 mm | -22.319 mm |
| 303 | -5.285 mm | -22.518 mm |
| 591 | +2.160 mm | -15.779 mm |
| 675 | -69.959 mm | -7.466 mm |
| 711 | -32.934 mm | +17.329 mm |
| 789 | -30.048 mm | -40.811 mm |

322/466仍有89.362/79.742毫米Correction下降，世界Y步分别约-45.877/-46.606毫米。世界运动、相对修正和原动画运动分别列出，不删掉对候选不利的一项。

## 本轮代码链路

Runtime提交d98db03577e7bad0dda45fca8ee47a44c83081a8只改三个Runtime文件：

1. AnimationFinalPosePhysicalWriter在原骨骼写回完成后，读取同一次Pelvis Transform世界点；用这个同点同时构造World与Component观测，不增加Writer或改Pose。
2. AnimationPresentationRuntimeSnapshot在原Physical Write页发布PelvisWorldPosition，沿用Frame/Completion有效性，不用之后的Root重建替代。
3. CharacterFootStrideHipsBuilder补齐合法Releasing/LandingReach输出的原动画Pelvis、Root和Component点，增加PoseInputAvailable；Rejected无源Pose时明确不可用。Spring、Reach和Goal原值不动。

Diagnostics提交928a7be18c4a12ac7a22993d5c9d7503b4ab13c4由指定诊断任务独立完成，只改五个Editor Diagnostics文件。唯一版本facts62/Analyzer62/diagnosis31，CSV1221列，在1216列基础上增加精确五列：

- PelvisPoseInputAvailable。
- FinalPhysicalPelvisWorldPositionX/Y/Z。
- FinalPhysicalPelvisGoalResidualAvailable。

Goal残差只在同Completion Physical写回、源Pose有效且PositionWeight大于零时可测。期望Component点等于原动画Component点加加权Goal偏移；合法零偏移加正权重仍有约束意义，不能按偏移幅度判不可用。没有有效Goal的占位0不作为JSON零误差测量。

原pelvisFrames和landing-leg-extension报告分别发布World、Component、加权Correction及相邻差；首帧没有前样本则明确不可用。没有第二Reporter，没有改变原37项质量Target或七维评分。

## 自动回放与独立零行为验收

本轮Record为43357ff3cd384e5cba75d2c31175b116，原文件SHA256为24D97232F35246C0B85A003B5980AC8F199D6FF63E9F74A0001B082F57EB89A6。新包20260831-020243-204-ff9828180a164a88bfcdfe4c59a37215，1043输出帧、2086脚行；不能把1044输入Proof帧写成输出采样帧。

Runtime规定flags构建27个既有警告、0错误；Diagnostics Editor构建57个既有警告、0错误，结束均立即shutdown。Unity在Edit正式Refresh后执行唯一同Record Replay；采样与Finalizing完成、failure为空、Console0错误，随后本任务结束自己启动的Play并回到Edit。没有Unity batchmode。

Proof020417对直接前驱Proof011000官方matched1044、aggregate mismatches为空、divergent0。独立核对1044完整frames、Runtime身份、输入/Body/start hash一致，samples SHA与Proof发布值一致。没有伪造193957的直接官方Proof。

原1216共同列只有24个采样/实例/Surface/Path身份列以及8个预期观测列变化；其它1184列逐字符串相同。所有身份按字符串建立双向映射无冲突，不把64位ID转浮点做差。50195行Geometry仅四个身份列变化，映射同样无冲突。

预期观测变化为StridePoseRootPositionXYZ、StrideAnimatedPelvisXYZ、StrideAnimatedPelvisComponentPositionY在1176个Releasing脚行补齐，以及FinalPhysicalPelvisGoalResidual在1168行修正。实际Foot、Reach、Spring、Knee、Body、Goal和原Physical Component输出全部不变。

PoseInputAvailable为2086/2086：455 Accepted加588 Releasing共享帧。ResidualAvailable为2078行；8行不可用精确对应共享帧778/974/975/976的零Goal权重。旧Releasing假残差中位719.922毫米、最大747.068毫米来自原动画点默认零；新有效残差最大0.298微米。这个误差的消失不是把骨盆移动了约75厘米。

新同Writer World点与历史单位T辅助重建最大差0.431微米，新值来自Writer而非重建。直接WorldY超过50毫米的相邻步仍33个，最大80.210毫米；它正式补齐后续质量对照的测量来源。

原37项rules、eligible/matched/rate、scorePolicy、完整score、occurrence和measurements逐值相同；七维分项相同，总分61.9、Evidence86.9不变。独立诊断任务的验收与本任务共同CSV/Proof对账一致。

无动态覆盖：合法零位移加正权重、Rejected/PoseUnavailable、仅LandingReach、缺Physical写回、非单位/变化Root变换。没有把静态分支合法性冒称这条Replay都已覆盖。

## 排除一个盲调方向

只读冻结010821的Foot/Pose/Reach输入，对原3Hz Critical Spring递推复算，最大Output差约5.043e-8米；再将同一模型频率换9Hz，仅作为数学反事实，不是Unity Replay或正式质量报告。

这个模型预测Correction超过50毫米30→87、WorldY超过50毫米33→127，WorldY最大步80.210→163.412毫米。因此没有把提高频率作为候选加载或改配置。Corin及TrainingEnemy的正式3Hz参数都未动，不能声称上述反事实证明了最终Foot/Knee结果。

## R826的实际几何边界

本轮确认不能靠“稍微抬高骨盆”承诺解决626.929毫米Solved Knee峰值。826左腿为PrimarySupport-only角色，必须读PelvisReach或实际Solver点，不能拿该脚未请求的LandingReach默认零向量计算。

腿长约695.434毫米，正式硬半径675.434毫米。826左Hip到目标Ankle约675.435毫米，实际Pelvis修正-30.237毫米已位于共同硬区间上界；在不改两脚目标与安全余量的前提下没有额外向上空间。右Hip只高于目标Ankle7.823毫米，横向43.139毫米，距离43.843毫米，即腿长6.304%。

这不是“所有数学方向都无解”：只限制最大腿长允许向下移动到Hip低于脚的位置，但那不能直接当作合理站立姿态。即便只读反事实把20毫米余量去掉，能多抬25.548毫米，右腿距离仍仅54.540毫米；本轮没有实施这个参数改动。

R826 BendWeight为0，位置目标、响应与Bend之间仍有业务取舍。以上只界定简单骨盆抬高方案的范围，不授权顺手修改Bend、Foot目标、Body或硬边界。

## 持久证据与后续

本地原包、所有前序/失败包、Record及其Proof均未覆盖、删除或清理。本轮12文件ZIP与独立Proof已保存在Diagnostics/FootPlacementReplayArchives/20260830-pelvis-response-refinement/step-4-world-pelvis-observation；每个ZIP条目与原文件SHA256相同，完整列表见manifest.json。

- ZIP SHA256：C07778263250B9C13909B156AACC2EE99210AE177AED2D8D8A67C254DCA552A1。
- Proof SHA256：BB0F44A9D139471D3E0D04DCE6483CAFD74CB87DE29F739383022E7BCC713983。
- samples SHA256：AFD739C194E446EBDD9E842F164E80366674EDB7034ABD0D5CBE089BD3BDF5CF。

下一轮只依据真实世界运动与相对修正分解，继续检查共同目标、已有响应和移动硬边界；保留675/711收益及231/266/267/303/591/789变化作为并列对照。R826膝盖外溢仍需检查，不能通过改变质量定义或忽略零权重约束风险宣布完成。需要更改未批准的业务合同或碰到他人改动时先明确边界，不扩展本Goal授权。
