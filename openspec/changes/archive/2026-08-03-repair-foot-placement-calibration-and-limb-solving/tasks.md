# Tasks

## 1. 固定当前实现与迁移闭包

- [x] 1.1 枚举`CharacterFootPlacementRigCalibration`全部序列化字段、构造入口和读者。
- [x] 1.2 枚举heel/toe offset在Analyzer、Planner、diagnostics和hash中的全部使用点。
- [x] 1.3 枚举semantic forward/up在rotation target计算中的完整调用链。
- [x] 1.4 枚举preferred bend从Calibration到`CharacterFootPlacementPlan`再到`CharacterComponentPoseLimbSolver`的完整调用链。
- [x] 1.5 枚举Foot Placement Profile中现有reach、twist和weight约束。
- [x] 1.6 枚举Calibration validator在保存、artifact rebuild、Build和Runtime composition的调用边界。
- [x] 1.7 枚举Corin Analysis Source、Sampling Rig、Calibration、Runtime Rig和Profile正式引用。
- [x] 1.8 枚举受Corin Calibration revision影响的全部artifact与生成Projection。
- [x] 1.9 确认Pose Graph中只有一个world-aware FootPlacement节点。
- [x] 1.10 确认旧Final IK生命周期与图外Transform写入已由唯一Pose workspace solver替换。
- [x] 1.11 确认active change未实现另一套Calibration或leg solver。
- [x] 1.12 固定Corin当前参考姿势heel/toe高度、sole axis角度、leg length和bend plane诊断值。

## 2. 建立Calibration v2数据合同

- [x] 2.1 定义Calibration v2稳定schema version。
- [x] 2.2 定义每脚heel contact offset字段。
- [x] 2.3 定义每脚toe contact offset字段。
- [x] 2.4 定义每脚ankle-local sole frame rotation字段。
- [x] 2.5 定义每脚preferred bend direction字段。
- [x] 2.6 定义sole frame的固定前、上、侧轴语义。
- [x] 2.7 定义左右脚统一手性规则。
- [x] 2.8 更新Calibration公开只读API返回v2语义。
- [x] 2.9 更新Calibration内容hash覆盖全部v2字段。
- [x] 2.10 更新Calibration content revision提交规则。
- [x] 2.11 更新Calibration clone/draft模型完整传递v2字段。
- [x] 2.12 删除独立semantic forward序列化字段。
- [x] 2.13 删除独立semantic up序列化字段。
- [x] 2.14 删除静态knee pole目标旧字段。
- [x] 2.15 删除Calibration v1 reader与默认axis补全。
- [x] 2.16 删除旧字段的兼容alias和迁移fallback。

## 3. 建立Sampling Rig几何解析器

- [x] 3.1 定义精确Analysis Source到Sampling Rig与Calibration的解析结果。
- [x] 3.2 验证Analysis Source只引用一个Sampling Rig。
- [x] 3.3 验证Analysis Source只引用一个Calibration。
- [x] 3.4 验证Sampling Rig绑定的Calibration identity与Source一致。
- [x] 3.5 从Sampling Rig精确绑定VisualRoot。
- [x] 3.6 从Sampling Rig精确绑定左右hip、knee、ankle和toe。
- [x] 3.7 禁止名称搜索、Humanoid映射和层级fallback。
- [x] 3.8 计算heel contact到VisualRoot参考空间的位置。
- [x] 3.9 计算toe contact到VisualRoot参考空间的位置。
- [x] 3.10 计算每脚heel-to-toe基线。
- [x] 3.11 计算每脚sole frame世界/VisualRoot参考姿势。
- [x] 3.12 计算hip-knee-ankle参考弯曲平面。
- [x] 3.13 计算preferred bend direction与腿轴夹角。
- [x] 3.14 计算参考平地目标ankle correction。
- [x] 3.15 将几何结果封装为Editor与Runtime validator共享的纯数据。

## 4. 扩展统一Calibration validator

- [x] 4.1 定义结构化Calibration诊断代码与severity。
- [x] 4.2 让诊断携带资产identity和脚侧。
- [x] 4.3 让诊断携带指标实测值与允许边界。
- [x] 4.4 拒绝非有限contact offset。
- [x] 4.5 拒绝非有限或不可归一sole frame rotation。
- [x] 4.6 拒绝非有限或近零preferred bend direction。
- [x] 4.7 拒绝过短heel-to-toe基线。
- [x] 4.8 拒绝sole frame前轴与heel-to-toe方向不一致。
- [x] 4.9 拒绝sole frame up与VisualRoot up偏差超限。
- [x] 4.10 拒绝heel与toe统一地面高度误差超限。
- [x] 4.11 拒绝左右脚参考地面误差超限。
- [x] 4.12 拒绝左右sole frame手性不一致。
- [x] 4.13 拒绝左右sole frame前后语义相反。
- [x] 4.14 拒绝退化hip-knee-ankle弯曲平面。
- [x] 4.15 拒绝preferred bend direction接近腿轴。
- [x] 4.16 拒绝preferred bend direction位于参考膝盖反侧。
- [x] 4.17 拒绝参考平地ankle correction超限。
- [x] 4.18 让Calibration正式提交前执行完整validator。
- [x] 4.19 让artifact rebuild前执行同一validator。
- [x] 4.20 让Definition Build前执行同一validator。
- [x] 4.21 让Runtime composition执行等价无Editor依赖validator。
- [x] 4.22 删除旧的仅finite/nonzero/orthogonal验证路径。

## 5. 实现Analysis Source校准作者入口

- [x] 5.1 在Analysis Source Inspector增加`Edit Rig Calibration`显式命令。
- [x] 5.2 让命令精确解析Source、Sampling Rig和Calibration。
- [x] 5.3 让缺失或冲突引用以结构化错误停止进入。
- [x] 5.4 打开或聚焦精确Sampling Rig Prefab Stage。
- [x] 5.5 创建不序列化的Editor校准session上下文。
- [x] 5.6 让session只允许写入Source引用的Calibration。
- [x] 5.7 在Scene View绘制统一参考地面。
- [x] 5.8 在Scene View绘制左右heel contact。
- [x] 5.9 在Scene View绘制左右toe contact。
- [x] 5.10 提供heel contact position handle。
- [x] 5.11 提供toe contact position handle。
- [x] 5.12 绘制每脚sole forward/up/side frame。
- [x] 5.13 提供sole normal与frame rotation handle。
- [x] 5.14 绘制hip-knee-ankle参考骨链。
- [x] 5.15 绘制动画参考bend plane normal。
- [x] 5.16 提供preferred bend direction handle。
- [x] 5.17 绘制参考平地ankle correction预览。
- [x] 5.18 显示sole length与heel/toe ground error。
- [x] 5.19 显示sole up夹角与左右手性。
- [x] 5.20 显示bend direction与腿轴夹角。
- [x] 5.21 将handle编辑保存在session draft中。
- [x] 5.22 让`Apply Calibration`先验证完整draft。
- [x] 5.23 让合法Apply形成单次Undo。
- [x] 5.24 让合法Apply更新Calibration revision和dirty。
- [x] 5.25 让非法Apply保留旧正式数据并显示全部诊断。
- [x] 5.26 让Cancel丢弃session draft且不修改资产。
- [x] 5.27 让Prefab Stage关闭时释放session对象与Scene回调。
- [x] 5.28 确认Inspector repaint不实例化Playable或扫描clip。
- [x] 5.29 确认handle拖动不触发artifact rebuild、Compile或Build。
- [x] 5.30 删除Calibration默认Inspector中的裸几何字段编辑入口。
- [x] 5.31 为Analysis Source增加显式Calibration Preview Clip引用。
- [x] 5.32 为Analysis Source增加有限的Preview normalized time。
- [x] 5.33 让Analysis Source校验Preview Clip存在、持久化且时长有效。
- [x] 5.34 在Analysis Source Inspector显示Preview Clip与固定时间。
- [x] 5.35 为校准session创建独立AnimationModeDriver。
- [x] 5.36 为Preview Clip创建Editor-only PlayableGraph。
- [x] 5.37 在进入Sampling Rig校准时采样固定Preview姿势。
- [x] 5.38 让session Inspector显式显示当前Preview Clip与时间。
- [x] 5.39 让关闭session或Prefab Stage时恢复全部采样属性并销毁Graph。
- [x] 5.40 确认Preview不写入Prefab Override、不修改AnimationClip且不进入Runtime。
- [x] 5.41 将Corin Analysis Source配置为正式Locomotion Idle固定帧。
- [x] 5.42 从heel-to-toe平面投影与VisualRoot up建立唯一sole frame派生函数。
- [x] 5.43 在加载Calibration draft时为左右脚自动重新派生sole frame。
- [x] 5.44 在移动任一heel/toe接触点后立即重新派生对应sole frame。
- [x] 5.45 删除Sole Frame手动编辑模式、rotation handle与相关分支。
- [x] 5.46 将draft诊断改为按脚侧显示的简短作者信息并删除GUID原始串。
- [x] 5.47 在校准session期间删除旧正式Calibration的重复Runtime binding错误展示。
- [x] 5.48 让Inspector明确显示sole frame为自动结果及其前轴、上轴误差边界。
- [x] 5.49 搜索确认不存在第二个sole frame手动作者入口。

## 6. 重构Calibration Inspector与只读引用信息

- [x] 6.1 为Calibration资产增加轻量Custom Editor。
- [x] 6.2 显示Calibration identity、schema和revision。
- [x] 6.3 显示引用该Calibration的精确Analysis Source列表。
- [x] 6.4 显示最近一次正式几何验证摘要。
- [x] 6.5 用业务名称显示Sampling Rig与脚侧，不显示GUID正文。
- [x] 6.6 为每个Source提供跳转而不提供无上下文几何编辑。
- [x] 6.7 确认选中Calibration不执行资产扫描重操作。
- [x] 6.8 确认Custom Editor不在`OnInspectorGUI`中分析AnimationClip。

## 7. 扩展Foot Placement Profile腿部约束

- [x] 7.1 增加`MinimumLegExtensionRatio`正式字段。
- [x] 7.2 统一并明确现有maximum reach字段的`MaximumLegExtensionRatio`语义。
- [x] 7.3 增加`BendStabilizationStartRatio`正式字段。
- [x] 7.4 增加`BendStabilizationFullRatio`正式字段。
- [x] 7.5 增加`MaximumBendStabilizationWeight`正式字段。
- [x] 7.6 更新Profile公开只读settings结构。
- [x] 7.7 更新Profile revision/hash覆盖新增字段。
- [x] 7.8 验证minimum ratio为有限正数。
- [x] 7.9 验证maximum ratio有限且小于1。
- [x] 7.10 验证stabilization start/full严格位于可解区间。
- [x] 7.11 验证start严格小于full。
- [x] 7.12 验证maximum bend weight位于`[0,1]`。
- [x] 7.13 删除solver内隐藏的bend权重常量。
- [x] 7.14 删除按动作名称或状态覆盖腿部约束的入口。

## 8. 扩展vendor-neutral Foot Placement Plan

- [x] 8.1 为每脚Plan增加leg extension ratio。
- [x] 8.2 为每脚Plan增加animated bend normal。
- [x] 8.3 为每脚Plan增加preferred bend normal。
- [x] 8.4 为每脚Plan增加bend stabilization weight。
- [x] 8.5 为每脚Plan增加typed bend decision reason。
- [x] 8.6 保持Plan不引用Transform、IKSolver或vendor类型。
- [x] 8.7 更新Plan reset/invalid工厂清零新增字段。
- [x] 8.8 更新Plan finite validation覆盖新增字段。
- [x] 8.9 更新固定容量runtime snapshot复制新增字段。
- [x] 8.10 删除Plan中静态reference Bend Goal位置语义。

## 9. 重构Planner腿部弯曲求解

- [x] 9.1 从最终动画姿势读取每脚hip、knee和ankle位置。
- [x] 9.2 计算upper leg与lower leg有限长度。
- [x] 9.3 计算目标脚位置对应leg extension ratio。
- [x] 9.4 计算最终动画hip-knee-ankle bend normal。
- [x] 9.5 检测动画bend plane退化程度。
- [x] 9.6 将Calibration preferred bend direction转换到当前求解空间。
- [x] 9.7 将preferred direction投影为合法bend normal。
- [x] 9.8 保证preferred normal与动画参考膝盖同侧。
- [x] 9.9 在安全伸展区间输出零stabilization weight。
- [x] 9.10 在start到full区间连续增加stabilization weight。
- [x] 9.11 将stabilization weight限制到Profile最大值。
- [x] 9.12 在动画bend plane退化时输出明确decision reason。
- [x] 9.13 在超过maximum extension时沿现有生命周期当帧释放。
- [x] 9.14 在低于minimum extension时拒绝不可解压缩目标。
- [x] 9.15 确保不可解Plan输出零position/rotation/bend权重。
- [x] 9.16 确保旧constraint release half-life不反向维持bend权重。
- [x] 9.17 确保Foot Placement Weight只在最终求解链应用一次。
- [x] 9.18 保持contact、support和pelvis生命周期所有权不进入Limb Pose Solver。
- [x] 9.19 删除reference-pose pole每帧满权重驱动路径。

## 10. 收口解析式Limb Pose Solver

- [x] 10.1 让solver读取Plan的preferred bend normal与独立稳定权重。
- [x] 10.2 从同帧hip、knee、目标ankle和preferred normal建立有限bend plane。
- [x] 10.3 让bend稳定范围按Rig v3当前leg length确定。
- [x] 10.4 让bend weight只读取Plan stabilization weight。
- [x] 10.5 删除`BendGoalWeight = PositionWeight`旧赋值路径。
- [x] 10.6 保持零bend weight时使用动画当前bend normal。
- [x] 10.7 保持position与rotation target读取现有Plan字段。
- [x] 10.8 保持pelvis先于左右Physical chain求解。
- [x] 10.9 让solver只写节点output Component Pose workspace。
- [x] 10.10 删除临时Bend Goal Transform。
- [x] 10.11 确保solver不query PhysicsScene。
- [x] 10.12 确保solver不修改constraint lifecycle或Profile。
- [x] 10.13 确保`ThirdPersonClient.Runtime`不引用RootMotion类型。
- [x] 10.14 删除Final IK adapter与vendor运行依赖。

## 11. 升级Foot Analyzer与artifact合同

- [x] 11.1 在Analyzer采样前解析精确Calibration v2。
- [x] 11.2 在创建Playable前执行完整几何validator。
- [x] 11.3 从合法左右heel/toe接触几何建立统一参考地面。
- [x] 11.4 禁止用全部接触点最低值掩盖单脚误差。
- [x] 11.5 让每脚height继续基于其heel/toe最低接触点。
- [x] 11.6 让sole轨迹继续使用合法heel/toe几何。
- [x] 11.7 保持plant classifier只使用垂直速度和高度。
- [x] 11.8 保持水平速度进入生成轨迹供Runtime合成。
- [x] 11.9 升级Foot Analysis algorithm version。
- [x] 11.10 升级artifact format version。
- [x] 11.11 让artifact identity覆盖Calibration v2 revision。
- [x] 11.12 删除旧artifact format reader。
- [x] 11.13 让旧algorithm artifact明确报告Stale或Unknown。
- [x] 11.14 更新Analysis面板显示Calibration几何错误。
- [x] 11.15 保持generated feature只读且不进入Timeline曲线编辑。

## 12. 扩展Projection与Runtime composition验证

- [x] 12.1 更新Projection Calibration dependency读取v2 schema。
- [x] 12.2 更新Projection hash覆盖v2 content revision。
- [x] 12.3 让Definition Build拒绝几何非法Calibration。
- [x] 12.4 让Definition Build定位Analysis Source、Sampling Rig和脚侧。
- [x] 12.5 让Runtime Rig读取v2 sole frame与preferred bend direction。
- [x] 12.6 让Runtime composition精确匹配v2 identity/revision。
- [x] 12.7 让Runtime composition拒绝非法Profile腿部区间。
- [x] 12.8 删除Runtime默认forward/up/pole补全。
- [x] 12.9 保持Runtime不读取Library artifact或Editor Analysis Source。
- [x] 12.10 保持BTSMTL Document只投影Calibration引用与只读context。
- [x] 12.11 确认不新增Foot Placement Document mutation或第二authoring owner。

## 13. 扩展运行诊断与Scene gizmo

- [x] 13.1 在runtime snapshot显示每脚sole frame identity/revision。
- [x] 13.2 显示animated bend normal。
- [x] 13.3 显示preferred bend normal。
- [x] 13.4 显示final bend normal或Bend Goal方向。
- [x] 13.5 显示leg extension ratio。
- [x] 13.6 显示bend stabilization weight。
- [x] 13.7 显示bend decision reason。
- [x] 13.8 分别显示position、rotation和bend最终权重。
- [x] 13.9 在Scene gizmo绘制动画bend plane。
- [x] 13.10 在Scene gizmo绘制preferred/final bend方向。
- [x] 13.11 在Scene gizmo绘制minimum/maximum extension边界。
- [x] 13.12 保持gizmo只读取正式Plan和snapshot。
- [x] 13.13 确保diagnostics不重新query或分析AnimationClip。
- [x] 13.14 确保热路径不新增LINQ、反射、临时List或每帧托管分配。

## 14. 迁移Corin正式配置与生成产物

剩余Calibration、artifact与Character Build发布闭包已转记到`complete-composable-pose-graph-editor-workflow`的10.1与10.11-10.14；本change不再维护旧图外solver发布边界。

- [x] 14.1 将Corin Calibration资产升级到v2 schema。
- [x] 14.3 在绑定姿势中重新定位左脚heel contact。
- [x] 14.4 在绑定姿势中重新定位左脚toe contact。
- [x] 14.5 在绑定姿势中重新生成左脚sole frame。
- [x] 14.6 在绑定姿势中重新提交左腿preferred bend direction。
- [x] 14.7 在绑定姿势中重新定位右脚heel contact。
- [x] 14.8 在绑定姿势中重新定位右脚toe contact。
- [x] 14.9 在绑定姿势中重新生成右脚sole frame。
- [x] 14.10 在绑定姿势中重新提交右腿preferred bend direction。
- [x] 14.12 更新Corin Foot Placement Profile最小伸展比例。
- [x] 14.13 更新Corin Foot Placement Profile最大伸展比例。
- [x] 14.14 更新Corin bend stabilization start/full区间。
- [x] 14.15 更新Corin maximum bend stabilization weight。
- [x] 14.16 删除Corin资产中的旧forward/up/pole序列化数据。
- [x] 14.23 对账Corin只有一个FootPlacement节点；后续解析式solver替换由新change统一实施。

## 15. 文档与静态收口

- [x] 15.1 更新`openspec/project.md`中的Foot Placement当前合同。
- [x] 15.2 更新相关current spec与本change任务状态。
- [x] 15.3 更新BTSMTL只读Presentation context中的Calibration字段说明。
- [x] 15.4 搜索确认不存在Calibration v1 reader。
- [x] 15.5 搜索确认不存在独立semantic forward/up旧字段。
- [x] 15.6 搜索确认不存在静态reference pole满权重路径。
- [x] 15.7 搜索确认不存在`BendGoalWeight = PositionWeight`。
- [x] 15.8 搜索确认不存在运行时default axis或Humanoid fallback。
- [x] 15.9 搜索确认不存在第二Foot Placement solver、Final IK Grounder或图外Transform写入。
- [x] 15.10 搜索确认没有selection、Inspector repaint或field change自动Build入口。
- [x] 15.11 对账current specs与实现没有旧schema矛盾。
- [x] 15.12 执行严格OpenSpec校验并修复全部错误。
