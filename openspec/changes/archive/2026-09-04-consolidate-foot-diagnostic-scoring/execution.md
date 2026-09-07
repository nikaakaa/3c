# 执行记录

## 固定Record新规则基线

固定输入使用`43357ff3cd384e5cba75d2c31175b116`，本次保留结果为`Diagnostics/FootPlacementRuns/20260902-020333-815-08bc7f3b04b44d2daa27b8a4919ce442`，Replay Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-020430-968-69ab7492738548ec83425931c993bc4d.json`。工具对上一保留Proof报告`matched:1044`，无failure，Foot Finalizing完整结束。

唯一格式身份已收口为`character-foot-motion-facts/75`、Analyzer 75、`character-foot-diagnosis-file/44`与`character-foot-quality-score/4`，评分规则为`foot-quality-seven-dimensions/3`。固定Record覆盖1043个表现帧、2086个双脚事实行和67186个Geometry行；总分为84.2，weighted evidence为96.5，最弱维度为`leg-pose`。

七维新规则基线为：`penetration`权重0.20、eligible 84、matched 19、Health 92.7、Evidence 100；`contact-fit`权重0.20、eligible 60、matched 13、Health 85.8、Evidence 100；`stable-swing`权重0.15、eligible 344、matched 143、Health 84.1、Evidence 100；`path-revision`权重0.15、eligible 667、matched 199、Health 87.7、Evidence 100；`contact-transition`权重0.15、eligible 1052、matched 418、Health 81.4、Evidence 100；`leg-pose`权重0.10、eligible 58、matched 26、Health 55.2、Evidence 100；`locked-horizontal`权重0.05、eligible 15、matched 0、Health 100、Evidence 30。该结果只作为同Record、同schema的诊断基线，不把总分解释为视觉通过。

`ThirdPersonClient.Editor.csproj`使用规定参数构建成功，57个警告均来自既有Unity、第三方依赖与未使用字段，0错误；构建后立即关闭build server。`consolidate-foot-diagnostic-scoring`严格校验与限定diff检查通过。至此任务5.3完成，后续Foot Host Adapter迁移必须保持上述字段资格、分母、权重和评分数学，不再并行修改旧Analyzer／Publisher语义。
