# Foot Placement重做交接摘要

## 工作范围

- 仓库：`D:/Unity_Project_1/3C`
- Active change：`openspec/changes/replace-pose-ik-with-finalik-full-body-solver`
- 正式链保持：`FootPlacement -> optional Predictive Modifier -> FinalIK FBBIK`
- 禁止第二Grounding、第二Pelvis、LegIK/TwoBoneIK、默认地面、固定高度、fallback、兼容路径和FBBIK后处理。

## 当前实现

- 旧Predictive、Plan、Ground Envelope、Revision、Anchor/Pelvis、CSV、自动Foot IK控制和专用Variant已删除。
- 当前只实现Landing Prediction最小闭环：`Current/Incoming Step + committed Body trajectory -> Raw Landing -> SphereCast -> Accepted/Rejected Landing`。
- FootPlacement仍输出Pelvis/左右脚三个Goal，但权重全部为0；当前只验证落点，不修改脚和骨盆。
- Gizmo：白色Native Sole、青色Raw Landing、黄色真实查询、绿色Accepted Landing、红色Rejected；无文字、无伪Path。
- 关键文档：
  - `openspec/changes/replace-pose-ik-with-finalik-full-body-solver/design.md`
  - `openspec/changes/replace-pose-ik-with-finalik-full-body-solver/tasks.md`
  - `openspec/changes/replace-pose-ik-with-finalik-full-body-solver/atomic-data-chain.md`

## 本轮已闭环错误

- GameplayLab场景曾保留已删除Variant GUID，导致`Variant at index 2 is missing`。现已删除专用Variant、Prefab、菜单、Builder分支和场景空槽，只保留Local Fixed、Local Float32、Rollback。
- `AnimationFootFeatureSample`约10KB，managed Source Sample和Dictionary按值传递时导致Mono `InvalidProgramException: Passing an argument of size '10000'`。
- `AnimationResolvedPoseSourceSample`与`PresentationPoseSourceSample`已改为引用对象；左右Foot Feature通过`ref readonly`读取。
- GameplayLab普通Play已运行约20秒，Console 0 Error，Editor保持Play状态供用户测试。

## 验证

- Landing单元测试3/3通过。
- Runtime与Editor编译0 Error。
- Float32和Fixed精确Character Build成功。
- OpenSpec strict validate通过。

## 关键提交

- `6a3e1d0` 删除失败预测Foot IK实现准备重做
- `a59bd0d` 重建预测落地点最小闭环
- `1a9f49b` 发布预测落地点角色产物
- `3087c73` 修复GameplayLab空Variant启动失败
- `8cb410e` 修复大Foot Step按值传递异常

## 当前架构判断

- 外层Goal链已统一，managed物理复制耦合已降低。
- 语义耦合仍高：`AnimationFootFeatureSample`同时包含Plant、Current/Incoming Step、Foot/Ankle/Hip Route、Clearance和约束事实；Landing只需少数字段却收到整份数据。
- 当前引用对象解决正确性，但会产生少量managed分配；最终应使用预分配Committed/Pending页。

## 推荐下一步顺序

1. 拆分`FootKinematicsSample`、`BiomechanicalStepHeader`、`BiomechanicalRoutePage`和原子`BiomechanicalStepReadPage`。
2. 深化唯一`CharacterFootPlacementRuntime` Module；外部只认识Frame Input、Result、Reset/Seal/Discard。
3. 建立不可变Plan事务，输入变化创建新Revision并重新查询，禁止旋转旧地形结果。
4. 深化World Query Module，集中Slope、Edge、Reachability、Hull和typed rejection；Unity Physics与测试实现作为两个Adapter。
5. 统一FootPlacementState，唯一拥有左右Constraint、Active/Incoming Plan、Landing事务和Pelvis。
6. FinalIK只做GoalSet到Pose的Adapter。
7. Profile删除Lyra/旧Predictive命名，只保留领域配置。
8. 所有热路径迁入预分配双页，消除managed分配。

## 推荐技能

- `openspec-apply`
- `diagnose`
- `improve-codebase-architecture`
- `unity-mcp-orchestrator`，只控制Editor
- 后续有CSV时再用`spreadsheets`只读分析

## 工作区注意

- 工作区仍有大量用户修改、第三方导入、CSV和`tmp` junction变化，不得整理、回退或提交无关文件。
- 永远不运行Unity batchmode；代码修改使用仓库根相对路径和`apply_patch`；文档读取使用`Get-Content -Encoding UTF8`。
