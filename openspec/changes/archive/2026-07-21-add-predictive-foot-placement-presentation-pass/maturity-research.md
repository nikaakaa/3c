# Foot Placement成熟方案调研与现状审计

## 结论

当前实现已经建立唯一表现链：

```text
Body -> Animation/Animancer -> Foot Placement Planner -> Final IK Limb Solver -> Camera
```

这条所有权边界是正确的。Final IK只负责把最终脚目标解到骨骼，不拥有接触判断、地面查询、预测、脚锁、骨盆或独立Unity更新时钟。

当前闭环已经具备预测式Foot Placement的技术底座，但不等于成熟动作游戏最终质量。结构修正完成后，仍需由作者按Corin真实鞋底几何校准sole offset，并通过楼梯、斜坡、转身和CrossFade实机观察Profile边界；完整三维凸Ground Envelope、水平Pelvis重平衡和Stride Warping不在当前范围。

## 2026-07-21 实施后状态

本文件后续“当前实现”章节保留为`add-predictive-foot-placement-presentation-pass`完成时的实施前审计。`refactor-foot-placement-animation-informed-runtime`已经沿同一Presentation Pass安装以下能力：

| 原缺口 | 当前处理 |
|---|---|
| Prefab重复sole offset与forward axis | 已迁移为全部Corin Runtime Prefab共享的`CharacterFootPlacementRigCalibration` |
| 缺少semantic up与固定旋转差 | Calibration保存每脚forward/up；Planner以`DesiredSemantic * inverse(AnimatedSemantic) * AnimatedAnkle`生成脚踝目标 |
| Final pose差分独占contact | 正式Definition Build生成每脚速度、高度、plant与landing特征；Runtime按visible producer自身时间和实际权重合成，姿势差分只诊断非法跳变 |
| 当前脚与未来落面混用 | Query显式拆分`CurrentSupport`、`FutureLandingSupport`和有限Ground Envelope；未来落面不替换Free脚X/Z |
| Ground Envelope缺少边界与拒绝原因 | segment保存有限区间、surface plane、edge midpoint、minimum sole height，并诊断height/edge/slope/step/reach等拒绝原因 |
| 缺少转身与可达约束 | 保持Free/Locked/Sliding三态，增加最小脚距、heel lift、ankle twist reduction、速度响应与同帧不可达清零 |
| Pelvis缺少支撑腿与Body垂直补偿 | 主要支撑腿、heel lift后的可达区间、Body visible vertical movement和有界临界阻尼已进入唯一pelvis solver |
| `MaintainBend()`不稳定 | Final IK adapter改为每脚显式bend goal，仍禁用LimbIK自主Update且每表现帧只Apply一次 |
| Timeline看不到生成事实 | 同一Timeline窗口增加只读`FOOT ANALYSIS`分组；唯一可编辑作者曲线仍是`Foot Placement Weight` |
| InPlace与播放倍率污染Plant速度 | Analyzer改用统一地面参考与垂直脚速；Runtime按Marker Sync后的视觉时间倍率推进生成特征 |
| Current heel/toe被单点折叠 | heel/toe独立查询并构造唯一virtual support plane，路径Envelope不再覆盖Current Support |
| Replant同帧重新锁定 | 旧constraint solve weight必须在Free中释放到零，后续表现帧才允许提交新锚点 |
| 作者Weight重复相乘 | clearance与rotation先生成完整几何目标，Foot/Pelvis最终权重只消费一次作者Weight |

仍然明确保留的边界：

- 不做pelvis水平重平衡、spine/VisualRoot旋转、Stride Warping或Motion Matching。
- 不让Foot Placement回写Gameplay Body、WorldSolver、Snapshot、Hash或网络。
- Corin鞋底offset仍需要在Unity中按实际鞋底网格做最终视觉微调；这属于Calibration数值，不改变代码链路。
- 完整效果仍必须由具有正式Body、PhysicsScene和Final IK Rig的Play Mode角色观察，纯Timeline Preview不伪造地面或IK世界。

## 公开成熟方案

### Ubisoft：预测式生物力学Foot IK

参考：

- GDC 2016 `Fitting the World: A Biomechanical Approach to Foot IK`
- https://media.gdcvault.com/gdc2016/Presentations/Roche_Clifford_Fitting%20the%20World.pdf

公开方案的关键点：

- 每只脚独立预测，离线自动获得每一步的落地延迟和距离。
- 接触判断同时使用脚趾速度、脚趾高度和IK姿势误差。
- 预测角色运动主要来自髋部，并结合动画前进速度和当前到预测位置的坡度。
- 保留动画原始脚轨迹，将动画脚高解释为相对预测地面路径的高度，禁止脚低于该路径。
- 使用Locked、Sliding、Unlocked多种约束，不把脚永久钉死。
- 骨盆高度由主要支撑腿决定，上坡和下坡使用不同规则，并以临界弹簧抑制上下跳动。
- 上坡时脚尽量保持水平，下坡时允许有限贴坡；跑步时关闭或显著减弱脚面方向修正。
- 沿脚的未来路径执行Capsule查询，排序并过滤表面，构造连续Ground Envelope；楼梯、尖峰和不可通过高度不能退化为单点Ray命中。
- 角色转向时考虑支撑脚附近的旋转关系，避免围绕角色原点旋转后把支撑腿扭坏。

### Unreal Engine：Foot Placement与Speed Planting

参考：

- `AnimNode_FootPlacement`
- https://dev.epicgames.com/documentation/en-us/unreal-engine/python-api/class/AnimNode_FootPlacement?application_version=5.5
- `FootPlacementPlantSettings`
- https://dev.epicgames.com/documentation/en-us/unreal-engine/python-api/class/FootPlacementPlantSettings?application_version=5.6
- `FootPlacementPelvisSettings`
- https://dev.epicgames.com/documentation/en-us/unreal-engine/python-api/class/FootPlacementPelvisSettings?application_version=5.6
- `FootPlacementInterpolationSettings`
- https://dev.epicgames.com/documentation/en-us/unreal-engine/python-api/class/FootPlacementInterpolationSettings?application_version=5.5
- Speed Planting
- https://dev.epicgames.com/documentation/unreal-engine/fix-foot-sliding-with-ik-retargeter-in-unreal-engine

公开合同把问题拆成明确模块：

- 每只脚使用动画图计算的速度，或使用离线生成的脚骨速度曲线判断Plant。
- Plant、Unplant、Replant分别拥有距离、角度和速度阈值。
- 腿链拥有最小/最大伸展比例，避免膝盖打直或脚目标不可达。
- Heel Lift、Ankle Twist Reduction和双脚最小分离距离分别处理脚掌滚动、脚踝扭转和转身夹腿。
- Floor、Unplant和Separation使用不同插值参数，不以一个统一half-life覆盖全部行为。
- Pelvis拥有Heel Lift优先级、水平重平衡、最大偏移、移动补偿和独立插值。
- 2-Bone IK链使用明确Hint或Preferred Angle稳定膝盖弯曲平面。

### Naughty Dog：动画后Foot Plant

参考：

- GDC 2021 `Motion Matching in The Last of Us Part II`
- https://media.gdcvault.com/GDC%2B2021/Motion_Matching_In_TLOU2.pdf

其公开Foot Plant Pass在最终动画姿势之后运行：脚速低于阈值时把脚固定在稳定世界位置，进入和离开约束时使用连续混合，避免锁定瞬间跳变。这个思路不依赖Motion Matching；关键仍是每脚接触数据、稳定世界锚点和连续释放。

## 当前实现与成熟方案对照

| 领域 | 当前实现 | 成熟方案缺口 |
|---|---|---|
| 管线所有权 | 唯一Presentation Pass，Final IK仅解算 | 已正确，无需改回Grounder或独立LateUpdate |
| Rig绑定 | 显式hip/knee/ankle/toe与forward axis | 鞋底偏移仍为零；缺少鞋底up axis、脚踝骨到语义鞋底坐标系的固定旋转；缺少明确膝盖Pole/Hint |
| 动画作者数据 | 每个Animation Clip一条全局Foot Placement Weight | 缺少左右脚独立的自动速度、高度、接触候选、落地时间和落地距离数据 |
| Contact | 生成每脚特征按视觉时间倍率推进，并与Body线速度和yaw角速度合成世界脚速 | 仍需实机校准不同动作下的Profile阈值 |
| Prediction | 使用生成landing delay/offset、Body可见速度和yaw速度 | 保留有限分段Envelope，不是完整三维生物力学轨迹包络 |
| Ground Envelope | heel/toe独立Current Support、virtual support plane、路径segment与Capsule净空 | 未实现完整凸Ground Envelope与复杂多层动态支撑 |
| Constraint | Free、Locked、Sliding、连续Replant释放、最小脚距、heel lift与twist reduction | 不增加动作名专用规则；极端转身仍需资产与Profile实机验收 |
| Foot Rotation | 应用Calibration semantic frame差值并按速度衰减 | Corin鞋底offset未按可量化网格完成最终人工校准 |
| Pelvis | 主要支撑腿、Heel Lift优先、Body垂直补偿与独立临界阻尼；组件竖直量在solver端转换到pelvis父骨空间 | 未实现水平重平衡、spine或VisualRoot旋转 |
| Limb Solver | Final IK LimbIK、显式bend goal、显式单次Update | 最终效果依赖正确Calibration和Profile，不让Final IK Grounder接管生命周期 |

## 当前已确认的具体问题

### 0. 已修复：组件竖直偏移不能直接写入Pelvis local Y

Corin的`Bip001 Pelvis`父骨带有固定预旋转。旧adapter把VisualRoot组件空间竖直标量直接按`Vector3.up`叠加到`pelvis.localPosition`，等价于沿父骨local Y移动；该方向并不等于角色竖直方向，因此台阶上的Actor Movement Compensation大部分变成横向位移，无法抵消VisualRoot突然上升。

正式合同改为`PelvisComponentVerticalOffset`。Planner只输出沿VisualRoot up轴的有限标量，Final IK adapter使用pelvis parent的`InverseTransformVector`换算为父空间local position delta后再写骨骼。该修正不移动VisualRoot、不改变Gameplay Body，也不新增Root Offset路径。

### 1. 鞋底参考点没有完成资产校准

当前Standalone、Rollback和网络角色Prefab中的左右heel/toe sole offset均为零。运行时因此把ankle与toe骨骼枢轴直接当成鞋底点，再取两者中点作为sole。骨骼枢轴通常不位于鞋底接触面，这会造成脚悬空、下陷、腿长误差和坡面旋转中心错误。

这不是Profile平滑参数能够修复的问题。必须基于Corin实际鞋底几何或明确的sole marker配置四个偏移。

### 2. 已修复：脚踝目标旋转应用Rig固定旋转差

Planner现在从Calibration的semantic forward/up构造Animated Semantic Foot Frame，再将Desired Semantic与Animated Semantic的差值乘到动画ankle rotation。Final IK不再直接接收语义`LookRotation`作为骨旋转。

正式实现必须保存或计算：

```text
Animated semantic foot frame
Desired semantic foot frame
Delta = Desired * inverse(Animated)
Target ankle rotation = Delta * Animated ankle rotation
```

不能直接把`LookRotation`结果写入脚踝。

### 3. 已修复：作者总权重与生成每脚语义分工

单一Foot Placement Weight继续只表达“该动画允许Foot Placement整体介入多少”。以下每脚事实由Editor-only Foot Analysis生成，不由作者重复维护：

- 左脚Plant事实。
- 右脚Plant事实。
- Prediction强度。
- Foot Rotation强度。
- Pelvis强度。

Runtime按visible producer和clip权重合成生成事实，Profile提供速度相关算法响应；旧四条手工策略曲线不会恢复。作者Weight在位置、旋转、clearance与Pelvis链路只应用一次。

### 4. 已修复：CrossFade与播放倍率不再伪造脚速

Contact优先消费各visible Animation Clip在自身Marker Sync有效时间上的生成速度与接触特征，再按visible contribution组合。局部生成速度会乘视觉时间倍率，并与Body线速度、yaw角速度合成世界接触点速度；最终姿势差分只用于非法跳变诊断。

### 5. 已修复：Current Support与未来表面分离

Current heel/toe现在分别查询并构造唯一virtual support plane。未来路径只生成Ground Envelope与Future Landing Support，不再覆盖当前脚目标或提前替换Free脚X/Z。

成熟实现应把两件事分开：

- `Future Landing Support`只提供预计落点与未来地面路径。
- `Current Foot Target`保留动画水平轨迹，并只用Ground Envelope修正最低高度。

### 6. 已修复：不可达与Replant不保留单帧旧约束

不可达脚会在同一表现帧输出零约束Plan。Replant超限进入Free后，旧constraint solve weight必须先连续释放到零，才允许在后续表现帧提交新锚点，避免同帧满权重换脚锁。

## 推荐的正式演进边界

后续应单独建立OpenSpec change，不在本已完成change中追加未批准代码。推荐顺序如下。

### 阶段一：修正Rig与Solver语义

- 为Corin全部正式Runtime Profile配置真实heel/toe sole offset。
- 增加显式semantic foot up axis或完整foot frame校准。
- 以语义foot frame delta修正脚踝目标旋转。
- 为左右Limb solver增加明确Pole/Hint或等价稳定弯曲平面。
- 不可达脚在同帧清零或重解对应IK权重。

这一阶段解决平地也歪、脚底高度错误、膝盖跳向和单帧拉扯。

### 阶段二：增加自动每脚动画分析

```text
Animation Clip
  -> Editor Foot Analysis
  -> Left/Right speed, height, contact candidate, landing delay/distance
  -> Presentation Projection
  -> CharacterFootPlacementRuntime
```

- 数据只进入Presentation Projection，不进入Semantic IR、Gameplay Program、Snapshot、Hash或网络协议。
- Timeline继续保存唯一手工Foot Placement Weight。
- 自动数据可以在Timeline Curves中只读显示并允许显式重新烘焙，不建立第二份手工作者真相。
- CrossFade按每个visible producer自己的采样时间读取特征，再以实际可见权重组合。

这一阶段解决混合时误锁、误释放和不同播放速度下阈值失真。

### 阶段三：完成地面路径与支撑腿响应

- 将Future Landing Support与Current Foot Target拆开。
- 保留动画原始水平脚轨迹和相对脚高，只用连续Ground Envelope提供最低允许高度。
- 增加边缘平面、Virtual Ground和连续路径过滤；实现可以使用凸包或等价确定算法，但不能回退单Ray。
- 增加支撑腿稳定、Heel Lift优先、转身双脚分离、ankle twist reduction和速度相关脚面旋转衰减。
- 继续只改最终骨骼，不移动VisualRoot，不回写Gameplay。

## 方案Tradeoff

### 方案A：只调现有Profile和Timeline单曲线

- 收益：改动最小，可以减弱当前异常。
- 代价：无法修复鞋底坐标、脚踝旋转、CrossFade假速度、膝盖稳定和未来表面提前牵引；只能把问题藏轻，不能形成成熟闭环。

### 方案B：按三个阶段补齐现有正式Pass

- 收益：保留当前正确的Presentation所有权和Final IK叶子adapter，同时把成熟方案真正缺失的输入补齐；Local、Rollback和网络观察角色继续共用一条路径。
- 代价：需要新增动画分析产物、Projection合同和Rig校准字段，必须通过独立proposal实施并迁移全部Corin Runtime Profile。

### 方案C：改用Final IK Grounder或另一个自主MonoBehaviour

- 收益：短期较快获得基础射线贴地。
- 代价：恢复第二个Unity更新时钟，无法统一消费Animancer可见贡献、Body Reset和网络Presentation identity；与项目当前管线冲突，不采用。

## 验收观察顺序

后续实机观察应按以下顺序定位，不先盲调全部参数：

1. 平地Idle：检查鞋底高度、脚掌方向和膝盖弯曲方向。
2. 平地Walk/Run：检查Plant稳定、CrossFade释放和跑步脚面旋转。
3. 原地与移动转身：检查双脚交叉、支撑脚扭转和膝盖跳向。
4. 单级台阶：检查未来脚是否提前被拉向台阶。
5. 连续楼梯上行/下行：检查原动画抬脚弧线、支撑腿切换和骨盆弹簧。
6. 平台边缘与移动表面：检查Ground Envelope、Replant和surface-local anchor。
