# Change: 修复Foot Placement校准与腿部求解

## Why

当前Corin的Foot Placement不是单纯“参数偏大”，而是正式输入和膝盖约束同时存在结构性错误：

- `CharacterFootPlacementRigCalibration`允许作者独立填写heel/toe offset、forward、up和knee pole，但Inspector只提供裸`Vector3`，没有Sampling Rig上的脚底、鞋尖和膝盖可视化编辑入口。
- 当前validator只验证数值有限、向量非零和forward/up近似正交，不能证明脚底点位于鞋底、左右脚frame手性一致、参考姿势平地修正接近零或pole位于合理膝盖弯曲侧。
- Corin现有forward来自ankle到toe骨的方向，heel/toe参考高度不一致，semantic up与VisualRoot up存在明显偏差；错误frame会把平地法线转换成大幅ankle旋转。
- Runtime若把静态reference-pose knee pole以与脚position相同的权重提交给骨骼solver，脚目标接近腿长极限、转身或骨盆补偿时会覆盖动画原有弯曲平面，导致膝盖翻面、腿部扭曲或伸直奇异。
- Editor Foot Analysis与Runtime共享同一Calibration identity，因此错误校准会被稳定写入artifact和Projection；identity一致只能证明两端使用同一数据，不能证明数据正确。

现行spec要求Calibration统一、显式和可验证，也要求Planner与解析式Limb Pose Solver分离。本change不增加第二套IK、不恢复Final IK或图外Transform写入，也不通过降低全局Foot Placement Weight掩盖问题，而是修正唯一正式Pose workspace链路。

## What Changes

- 将Rig Calibration升级为唯一几何语义合同：
  - 删除可彼此矛盾的独立semantic forward/up字段，改为每脚唯一正交`Sole Frame`旋转。
  - 保留heel与toe接触点，但要求它们在精确Sampling Rig绑定姿势中共同定义有长度、有方向且接近统一地面的鞋底基线。
  - 将静态knee pole位置语义改为`Preferred Bend Direction`，只表达腿部首选弯曲侧，不再等价于每帧满权重IK目标。
  - 升级Calibration schema与content revision；删除旧schema读取、默认axis和兼容fallback。
- 在现有Analysis Source作者入口提供Sampling Rig上的校准工具：
  - `CharacterFootPlacementAnalysisSource` Inspector通过显式命令进入其精确Sampling Rig上下文并编辑所引用Calibration。
  - Analysis Source显式引用一个仅用于校准作者预览的AnimationClip与固定归一化时间；进入校准时以Unity Animation Mode在Sampling Rig上显示该姿势，退出时完整恢复Prefab绑定姿势且不保存骨骼Override。
  - Scene View只允许作者移动左右heel/toe接触点与preferred bend direction；sole frame按heel-to-toe平面投影和VisualRoot up唯一自动派生，不保留手动旋转路径。
  - Scene View显示自动sole frame、hip-knee-ankle弯曲平面、左右手性和预测平地修正；支持Undo和单次提交。
  - Inspector显示只读几何指标与按脚侧归类的结构化错误，不显示GUID或编辑态旧正式Calibration重复错误，不在`OnInspectorGUI`、选中资产或拖动重绘期间执行完整分析、Compile或Build。
- 扩展Calibration正式验证：
  - 验证接触点长度、有限性、frame正交性与手性。
  - 验证左右heel/toe相对统一参考地面的误差、semantic sole up与VisualRoot up夹角、参考姿势平地ankle修正角、preferred bend direction与hip-knee-ankle弯曲平面的一致性。
  - 非法Calibration阻止artifact rebuild、Definition Build和Runtime composition，并定位脚侧、指标、阈值及资产。
- 重构腿部弯曲计划：
  - Planner从最终动画hip/knee/ankle姿势计算当前动画弯曲平面，不再把reference pole直接当作满权重Bend Goal。
  - Plan新增vendor-neutral的leg extension ratio、preferred bend normal和独立bend stabilization weight。
  - Profile显式配置最小/最大腿伸展比例、稳定介入区间和最大稳定权重；脚位置权重与弯曲稳定权重分离。
  - 正常范围优先保留动画弯曲平面；接近过度伸直或过度压缩时，才连续混向Calibration首选方向。越过可解范围时仍按正式约束生命周期释放，不允许solver硬拉。
- 收口解析式Limb Pose Solver：
  - solver只消费Plan、同帧上游Component Pose、Rig v3腿链与Calibration。
  - bend稳定权重不再等于foot position weight；零稳定权重时保留同帧动画弯曲平面。
  - 不恢复Final IK、图外MonoBehaviour、自主Update、隐藏target或第二planner。
- 更新Foot Analysis和生成产物：
  - Analyzer在采样前执行完整几何校验，并从合法sole frame与heel/toe接触基线建立统一参考地面。
  - 升级algorithm/artifact format，旧artifact直接Stale；显式重建后再发布Float32/Fixed Presentation Projection和Native Pose Program。
  - Corin Calibration通过新作者工具重新校准，旧序列化字段和旧生成产物直接删除或替换，不保留双读。
- 扩展只读诊断：显示动画弯曲平面、preferred bend normal、bend stabilization weight、leg extension ratio、sole frame误差和最终solver bend结果，Scene gizmo只读取正式Plan。

## Impact

- 影响`CharacterFootPlacementRigCalibration` schema、Analysis Source Inspector与Scene authoring、Calibration validator、Foot Analyzer、artifact identity、Definition Projection dependency、Foot Placement Profile、Planner、Plan、runtime snapshot、CharacterLimbPoseSolver和Corin正式资产。
- Corin的Foot Analysis artifact、Presentation Projection与Native Pose Program必须显式重建；旧artifact不兼容读取。
- 不改变Pose Graph中`FootPlacement`节点的位置，不改变BTSMTL StateMachine、Timeline locomotion职责、Motion Matching搭建顺序、Gameplay State或Network/Simulation边界。
- 不把左右脚接触改成手工Timeline lane；Animation Clip仍只有一条作者可写`Foot Placement Weight`，生成foot feature仍是只读artifact。
- 不自动降低Corin全部Weight曲线。Weight表达“允许Foot Placement介入”，不能用来补偿错误rig frame或错误膝盖方向。
- 不增加运行时fallback、Humanoid自动找骨、名称扫描、默认轴或第二套solver。
- 校准预览姿势不进入Player、Runtime Projection或IK求解；它只改变作者观察Sampling Rig的姿势，正式Calibration仍只保存bone-local contact、sole frame与VisualRoot-local preferred bend direction。

## 与现行Spec及Active Change对比

- `character-foot-placement-presentation`当前要求Calibration“显式且可验证”，但现有场景只覆盖forward/up退化；本change补足Sampling Rig几何、参考平地、左右手性和膝盖弯曲侧验证。
- 现行spec把semantic forward/up作为两个独立作者向量；这允许两者与heel/toe鞋底线互相矛盾。本change将其修改为单一Sole Frame旋转，并让作者工具从真实鞋底几何生成。
- 现行spec只规定最大leg extension时释放，没有定义过度压缩与接近奇异点时如何处理膝盖方向；本change增加有限伸展区间和独立弯曲稳定权重。
- `character-animation-foot-analysis-artifact`当前从Calibration绑定姿势取统一地面，但没有先证明左右接触点和frame是合法鞋底。本change要求Analyzer先通过同一几何validator，避免稳定缓存错误输入。
- `refactor-pose-transition-blend-authoring`只处理StateMachine Transition的Standard Blend/Inertialization曲线与Profile，不改变Foot Placement的骨骼求解；本change不会回退或复制该混合链。
- `add-character-motion-matching-pose-source`尚未成为Corin当前Foot Placement的前置条件；本change以`FinalAnimationPoseFrame`为唯一输入，因此未来Motion Matching接入后直接复用同一校准和腿部求解。
- `add-corin-training-ai-demo`中的角色若启用Foot Placement，也只能消费本change修正后的统一合同；本change不为AI角色创建另一套配置或solver。

