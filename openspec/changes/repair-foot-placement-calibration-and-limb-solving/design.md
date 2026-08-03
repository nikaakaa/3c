# Design: Foot Placement校准与腿部求解修复

## Context

当前链路是：

```text
Calibration裸Vector3字段
  heel/toe offset + forward/up + knee pole
        |
        +-> Editor Analyzer -> artifact -> Projection foot feature
        |
        +-> Runtime Planner -> ankle target + reference bend goal
                                      |
                                      v
                    CharacterComponentPoseLimbSolver
```

它有两个独立但会叠加的问题：

1. semantic frame未由真实鞋底几何证明，平地也可能产生大旋转。
2. reference bend goal与foot position共用权重，动画膝盖平面会被静态方向强制覆盖。

目标链路是：

```text
Analysis Source + exact Sampling Rig
        |
        v
Scene Calibration Authoring
  heel/toe contact + single sole frame + preferred bend side
        |
        v
Strict Geometric Validation
        |
        +-> Editor Analyzer -> artifact -> Projection
        |
        +-> Runtime Planner
              animated bend plane
              extension ratio
              preferred bend normal + independent weight
                        |
                        v
        CharacterComponentPoseLimbSolver
```

## Goals

- 作者能在精确Sampling Rig上看见并编辑鞋底和膝盖语义，不再手填不可理解的坐标。
- Calibration在保存、分析、Build和Runtime composition四个边界使用同一几何validator。
- 平地参考姿势不会因为rig轴差异产生大幅ankle旋转。
- 正常行走和转身保留动画膝盖弯曲平面；只有接近奇异区间才有限介入。
- Planner只生成world-aware Plan，CharacterComponentPoseLimbSolver只把Plan应用到同帧Component Pose workspace。
- Corin只保留一份正式Calibration和一条Foot Placement运行链。

## Non-Goals

- 不恢复Final IK、vendor adapter、隐藏target或图外Transform写入。
- 不新增FABRIK、CCD、Unity Animation Rigging或另一套腿部solver。
- 不把Foot Placement变成Timeline动作、Montage、Gameplay State或Motion Matching特例。
- 不增加左右脚手写Plant曲线、Foot Phase Blackboard变量或按动画名分支。
- 不自动Compile、Build、重建artifact或保存资产。
- 不新增测试；用户负责Unity端到端验收。

## Decision 1: Calibration使用单一Sole Frame而不是独立forward/up

Calibration v2每只脚只保存：

```text
Heel Contact Offset
Toe Contact Offset
Sole Frame Rotation
Preferred Bend Direction
```

`Sole Frame Rotation`是ankle局部空间中的唯一正交旋转。Editor以参考姿势中heel到toe在VisualRoot up平面上的投影作为前轴，以VisualRoot up作为上轴，并由固定右手性得到侧轴；作者不再直接旋转该frame。Runtime不再分别归一化两个可互相矛盾的向量。

heel offset仍相对ankle，toe offset仍相对toe骨；validator在精确Sampling Rig预览姿势中把两者转换到同一世界语义空间，再证明它们能够形成有限鞋底基线。作者移动任一接触点后，工具立即按`LookRotation(ProjectOnPlane(toe - heel, VisualRoot.up), VisualRoot.up)`重新计算并提交完整frame；退化基线保留非法draft供validator定位，但不得提交。系统删除手动sole frame旋转模式，不允许作者数据与接触基线形成两条配置路径。

### Tradeoff

- 单一旋转：天然保持正交和手性，Runtime输入简单；必须破坏性升级旧schema并重新校准Corin。
- 保留forward/up并加强validator：序列化变化较小，但作者仍需同时维护鞋底点与两根轴，合法正交也可能与真实鞋底方向矛盾，不采用。
- Runtime从动画toe方向即时猜frame：无需校准，但不同动画toe骨姿态会改变rig固定语义，Analyzer与Runtime也无法共享稳定frame，不采用。

## Decision 2: 校准入口属于Analysis Source的精确Sampling Rig上下文

视觉校准从`CharacterFootPlacementAnalysisSource` Inspector显式进入，因为该资产已经唯一引用Sampling Rig与Calibration。入口打开或聚焦精确Sampling Rig Prefab Stage，并创建仅Editor session存在的校准上下文；它只写回Analysis Source引用的Calibration，不新增持久化rig引用。

Scene View提供：

- heel与toe接触点position handle；
- 按heel/toe与VisualRoot up自动派生的只读sole frame前、上、侧轴；
- hip-knee-ankle当前弯曲平面与preferred bend direction handle；
- 左右脚统一地面、sole长度、ground error和参考平地ankle correction预览；
- 单次`Apply Calibration`、Undo和dirty。

Inspector只显示当前draft的作者可行动指标与按脚侧归类的诊断。校准session期间不再同时执行并显示旧正式Calibration的Runtime binding错误；GUID、revision和原始诊断串只保留在正式诊断数据中，不直接占据作者操作面板。

Analysis Source同时显式引用一个`Calibration Preview Clip`与固定归一化时间。这个引用只属于Editor作者体验：校准session通过独立`AnimationModeDriver`与PlayableGraph预览该固定姿势，关闭session或Prefab Stage时停止Animation Mode并恢复全部被采样属性。预览不得记录Prefab骨骼Override，不得修改动画资产，也不得在Inspector重绘时持续采样。

预览姿势不是第二份Calibration几何数据。heel/toe仍保存为ankle/toe局部offset，sole frame仍保存为ankle局部rotation，preferred bend仍保存为VisualRoot局部direction；因此作者可以在Idle姿势中定位这些bone-local语义，而Analyzer和Runtime继续消费同一Calibration。Runtime不得读取Preview Clip，也不得为了构建IK绑定采样Editor动画。

Calibration资产Inspector只显示schema、identity、revision、引用它的Analysis Source和最近一次正式验证结果。没有精确Analysis Source上下文时，不允许在空白坐标系里编辑几何字段。

拖动期间只计算当前Sampling Rig绑定姿势的轻量矩阵和指标。完整clip analysis、artifact rebuild、Projection Compile与Build只能通过现有显式命令触发；`OnInspectorGUI`不得遍历clip、实例化Playable或执行资产级扫描。

### Tradeoff

- 从Analysis Source进入：Sampling Rig和Calibration关系唯一，作者看到的就是Analyzer输入；同一Calibration被多个Source引用时必须明确选择一个上下文。
- 显式Preview Clip：Generic Rig无需Humanoid T-Pose或名称猜测，作者能使用项目正式Idle观察脚底；代价是Analysis Source多一个Editor-only资源引用，且预览只负责可视化，不改变Runtime绑定姿势。
- 独立Calibration EditorWindow：可以集中显示，但会产生第二个导航和selection生命周期，且容易在未绑定rig时编辑裸坐标，不采用。
- 继续使用默认Inspector：实现最少，但作者无法判断点和轴在角色上代表什么，不采用。

## Decision 3: 几何正确性是Build条件，不是调参建议

统一validator接收`Analysis Source + Sampling Rig + Calibration`并输出结构化结果。它至少检查：

- 所有offset、rotation和direction有限；
- sole frame可归一、固定手性且左右脚语义一致；
- heel到toe基线长度大于骨架尺度相关的最小值；
- heel与toe在统一参考地面上的高度误差在边界内；
- sole up与VisualRoot up在参考姿势中的夹角在边界内；
- 将参考姿势放到平地时，计算出的ankle correction在边界内；
- hip-knee-ankle可以形成有限弯曲平面；
- preferred bend direction位于该平面的同侧且不接近腿轴；
- 左右脚sole frame不会出现镜像手性或前后颠倒。

阈值属于唯一代码级校准合同，不暴露成每角色“调到通过”的配置。错误包含资产、脚侧、指标、实测值和允许边界，并阻止保存正式Calibration提交、artifact rebuild、Definition Build与Runtime composition。

### Tradeoff

- 严格阻断：错误不会流入生成产物或运行时，代价是旧Corin配置在迁移完成前会明确报错。
- 只显示warning：不影响旧内容，但错误frame仍会稳定发布并扭曲腿，不采用。
- Runtime自动修正axis或offset：能够继续运行，但Analyzer与Runtime会使用不同真相并形成隐藏fallback，不采用。

## Decision 4: Planner优先保留动画弯曲平面

Planner每帧从最终动画hip、knee、ankle位置计算动画弯曲平面：

```text
upper = normalize(knee - hip)
lower = normalize(ankle - knee)
animatedBendNormal = normalize(cross(upper, lower))
```

Calibration的`Preferred Bend Direction`在当前VisualRoot/腿部空间中转换为preferred normal。它只在动画平面接近退化或腿长进入稳定区间时提供方向，不能直接替换动画knee位置。

`CharacterFootPlacementPlan`每脚新增：

```text
LegExtensionRatio
AnimatedBendNormal
PreferredBendNormal
BendStabilizationWeight
BendDecisionReason
```

位置/旋转target和weight继续按现有约束生命周期产生。弯曲稳定权重独立计算：

- 伸展比例处于安全区间时为0，完整保留动画平面；
- 接近最大伸展或最小压缩边界时连续上升；
- 动画弯曲平面退化时，按明确原因使用有限preferred weight；
- 越过可解最小/最大伸展范围时，Planner释放或拒绝该脚，不能把weight交给solver硬解；
- Foot Placement作者Weight为0时，position、rotation、pelvis和bend stabilization全部为0，但同一个Weight不在多层重复相乘。

### Tradeoff

- 动画平面优先：保持动画师设计的膝盖方向，只在数值危险区稳定；需要Planner读取最终动画三关节姿势。
- 静态pole始终满权重：方向稳定但会覆盖每个动作自己的膝盖平面，正是当前扭曲来源，不采用。
- 完全不提供preferred方向：正常动作自然，但接近完全伸直时cross product不稳定，膝盖仍可能翻面，不采用。

## Decision 5: Profile显式拥有有限腿长与稳定介入策略

`CharacterFootPlacementProfile`增加每条腿共同使用的正式设置：

```text
Minimum Leg Extension Ratio
Maximum Leg Extension Ratio
Bend Stabilization Start Ratio
Bend Stabilization Full Ratio
Maximum Bend Stabilization Weight
```

所有比例相对校准/绑定姿势得到的有限leg length。validator要求：

```text
0 < minimum < stabilizationStart < stabilizationFull < maximum < 1
0 <= maximumBendWeight <= 1
```

过度压缩使用minimum边界，接近伸直使用stabilization区间与maximum边界。设置是Profile正式业务策略，不藏在Limb Pose Solver或magic constant中。Corin只配置一份Profile值，不按Idle、Walk、Run、Turn分叉。

### Tradeoff

- Profile统一策略：不同角色比例可以显式调整，所有Locomotion source共享；需要Profile schema迁移。
- 每动画配置pole/伸展：能细调动作，但会把骨架约束复制进Timeline并增加大量内容负担，不采用。
- solver固定常量：代码少，但作者无法从正式配置理解角色可解范围，不采用。

## Decision 6: CharacterComponentPoseLimbSolver只应用Plan

解析式solver保持固定调用顺序：

```text
copy upstream Component Pose
apply pelvis component translation
solve left Physical chain
solve right Physical chain
publish output workspace
```

solver根据同帧动画bend plane与Plan的preferred bend normal构造有限最终bend normal，稳定权重只取`BendStabilizationWeight`，不复制`PositionWeight`。当稳定权重为0时继续使用同帧动画弯曲平面；solver不切换实现或猜默认方向。

solver不创建Transform、target或MonoBehaviour，不query地面、不计算extension、不选择constraint生命周期，只写FootPlacement节点output Component Pose workspace。

### Tradeoff

- vendor-neutral normal/weight：Planner不依赖骨骼实现，solver直接在Pose workspace确定性求解。
- Plan保存Transform/target：会把核心合同绑定到场景对象并形成第二姿势链，不采用。
- 恢复Final IK adapter：会绕开节点output和final writer，不采用。

## Decision 7: Analyzer只接受通过几何证明的Calibration

Analyzer在创建采样Playable前调用同一validator。统一地面从Sampling Rig绑定姿势中两脚合法heel/toe接触点建立；不再通过“所有点取最低值”掩盖单脚悬空或heel/toe高度错误。

每脚sole轨迹仍来自heel/toe接触几何，plant classifier仍使用垂直速度与高度，水平速度仍用于Runtime合成。artifact identity增加Calibration schema v2和新algorithm version。旧format没有reader，状态只能是Stale/Unknown并要求显式重建。

### Tradeoff

- 先验证再分析：生成特征与Runtime共享同一物理含义，坏配置不能生成“内部一致但外部错误”的缓存；需要一次重建全部受影响clip。
- 保留旧最低点算法：旧artifact可继续用，但它会掩盖左右脚和heel/toe参考高度差，不采用。

## Decision 8: 诊断显示原因，不新增控制入口

Live Debug与Scene gizmo只读暴露：

- semantic sole frame、heel/toe contact和参考平地误差；
- animated/preferred/final bend normal；
- leg extension ratio、stabilization weight与decision reason；
- position/rotation/bend三个最终权重；
- Planner release原因和解析式solver执行结果。

诊断不修改Profile、Calibration或Plan，不重新query、不实例化Sampling Rig、不分析clip。运行时snapshot保持固定容量和零每帧托管分配。

## Data Migration

迁移只保留唯一新链路：

1. 将Calibration schema升级到v2并删除旧forward/up/pole序列化字段和reader。
2. 更新所有构造、codec、hash、validator和Document只读context使用新字段。
3. 通过Corin Analysis Source进入新Scene校准工具，重新提交唯一Corin Calibration。
4. 升级Foot Analysis algorithm/artifact format并删除旧缓存可读路径。
5. 显式重建Corin全部受影响AnimationClip artifact。
6. 显式发布Corin Float32与Fixed Presentation Projection及Native Pose Program。
7. 更新Corin Foot Placement Profile的有限腿长与bend stabilization设置。
8. 删除旧solver中`BendGoalWeight = PositionWeight`和reference pole直接驱动路径。

不保留schema v1反序列化、axis默认值、旧artifact reader、双份Calibration或运行时fallback。

## Validation

实现完成后必须能从代码和正式资产证明：

- Calibration不存在独立forward/up和静态pole目标旧字段。
- Calibration几何只能在精确Analysis Source/Sampling Rig上下文提交。
- Inspector重绘、selection和handle拖动不会触发clip analysis、Compile或Build。
- Corin heel/toe、sole frame、左右手性、参考平地修正和preferred bend direction全部通过统一validator。
- Analyzer、Definition Build和Runtime composition调用同一Calibration几何合同。
- Plan显式保存extension ratio与独立bend stabilization weight。
- solver bend稳定权重不再复制foot position weight。
- 安全伸展区间中动画bend plane不被preferred方向覆盖。
- 不可解压缩或伸展在Planner边界释放，不进入solver硬拉。
- Pose Graph每条最终Output路径最多一个有状态FootPlacement节点，不存在Final IK或图外自主Update。
- 旧artifact格式和旧Calibration schema不存在兼容reader。
- Corin生成产物全部引用Calibration v2同一identity和最新revision。
