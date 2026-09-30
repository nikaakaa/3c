# 可琳相机完整链路审计

日期：2026-09-29。本文是只读审计记录：只回读 `CorinCharacterCameraProfile`、Corin Timeline/Ability/AttackProperty、当前相机合同与求值器源码，以及既有取证文档。没有修改相机代码，没有刷新 Unity，没有运行 Play、replay 或性能采集。

## 结论

可琳相机还没有做完。当前已经闭合的是默认轨道、输入轴、Delay 的默认/手动/释放模式、屏幕框基础构图、Zoom/Stretch 的主要相位、震动基础信号与时钟通道；仍未闭合的是完整原生模式资格、若干已进入 Profile 的 Delay 字段、基础轨道 Follow/Aim 偏移、效果后构图时序、Zoom/Stretch 的完整生命周期，以及震动静默/保持/取消优先级等业务输入。

“资源在 Profile 里”不等于“当前可琳会用”。当前 Profile 有 21 Shake、18 Zoom、18 Stretch。本文把“存在正式静态引用”和“运行时能进入相机求值器”分开：21 个 Shake 都能在当前正式资源中找到静态引用，但只有 12 个动作帧 Shake 已在正式 Timeline/Ability 图中产生 `CameraEffectRequest`；9 个 A 类命中 Shake 只进入 AttackProperty 数据和 Fixed/Float32 catalog，当前还没有命中几何查询、命中结果生产者或把 `GameplayAttackCameraShakeComponentDefinition` 转成相机请求的运行消费者。另外，Zoom 和 Stretch 各只有 5 项被当前正式资产引用，另外 13 项只是 Profile 常驻资源。

## 当前实际可达资源

| 类型 | Profile 数量 | 存在正式静态引用 | 当前可进入运行求值器 | 资源与运行状态 |
| --- | ---: | ---: | ---: | --- |
| Shake | 21 | 21 | 12 | Branch_02 的 A_01/A_02/E_01/E_02/E_03；Normal_01 A_01；Normal_02 A_01/E_01；Normal_03 A_01；Normal_04 A_01/A_02/E_01/E_02；Normal_05 A_01/E_01/E_02/E_03；Rush A_01/E_01/E_02；Rush Enhance E_01。其中 9 个 A 类停在 AttackProperty/catalog，运行触发链未闭合 |
| Zoom | 18 | 5 | 5 | Branch_02 Zoom_01/02；Normal_03 Zoom_01；Normal_05 Zoom_01/02 |
| Stretch | 18 | 5 | 5 | Branch_02 Stretch_01/02；Normal_03 Stretch_01；Normal_05 Stretch_01/02 |

当前没有正式的 Sequence、Override Track、Shot 和额外 TargetSlot。`CorinCameraDefaultSequence` 是唯一 Sequence；`FixedInCore` 与 `HandleVolume` 也没有当前资产实例，因此这两个未实现分支不会在当前 Corin 默认链中触发。

引用方式有两类，不能只用资源名搜索：命中类 Shake 多数由 AttackProperty 以字符串资源 ID 引用；Timeline 的 `CameraEffectClip` 以 GUID 引用 Zoom/Stretch。本文的“存在正式静态引用”指能在当前正式 Corin 资产中找到正式生产者引用；“当前可进入运行求值器”还要求中间业务链已经把请求送到相机运行时。两者都不等于已经逐项验证触发条件会在实机战斗中命中。

## 当前启用分支

当前可达的 5 个 Zoom 全部是 raw `PlayStackingType=0` 映射后的 Replace、raw `StackingType=Replace`、`FovVariationType=Absolute`。基础/附加输出选择和绝对 FOV 变体会走当前 `CameraZoomEffectEvaluator`；但原作组内起始值继承和独立全局 FOV 栈还没有闭合。

当前可达的 5 个 Stretch 都没有开启特殊仰角、终点仰角、`ApplyAimPointsCameraFollowYOffset` 或 `ApplyRuntimeCamFollowYOffset`。所以特殊仰角到顶端轨道、FollowY 点位解析这两条缺口不影响当前可达资源，但仍是 Profile 内其他 13 项资源未来启用时的正式边界。当前可达 Stretch 会消费 CamOffset、半径比例、滚转、生命周期曲线和 Base/Additive 输出选择。

已进入运行求值器的 12 个动作帧 Shake 都是 `ShakeType=0`、`DissipationMode=5`、`CustomCurveKey=Camera_ShakeSpatial_Curve_01`。当前求值器已消费余弦半幅信号、独立 NoiseAngle、真实距离曲线、Base/Additive 仲裁、正时长自然结束和 owner 时间倍率通道。9 个 A 类 Shake 尚未到达这条求值链。相机生命周期仍未闭合区域渐静默、Base 保持计数、非正时间切离 `Action3DCamera`、按 `DataPriority` 的取消筛选，以及时钟聚合器内部业务来源。`StandardConfigKey` 在已追通的原作求值链中未消费，不能继续当作运行时模板输入。

## Delay 当前状态

当前默认配置 `AutoChangeMode=true`、`DefaultMode=0`，运行时可达模式是 raw 0、3、2：默认为 0，手动转镜进入 3，释放后按 Idle/移动/闪避和移动输入进入 2。EventGraph 输出的 Idle/Move/Evade 已经进入这条模式切换链；Profile 其余模式 raw 1、4、11、12、20、25 当前没有正式模式请求生产者。

已消费的 Delay 输入包括：模式过渡曲线/时间/稳定时间、`OrbitLerpTime` 的上下分段混合、跟随位置阻尼、FollowDirection、`MinimumDistanceRatio`、屏幕中心、死区、软区、Bias 和构图阻尼。Delay 表内的 `FieldOfView` 保留在混合数据中但不直接覆盖当前镜头 FOV，这是既有消费者证据要求的边界。

当前仍未消费但已经进入 Profile 的字段包括：

| 字段 | 当前值与影响 |
| --- | --- |
| `SpeedSmoothTime` | 0.01，当前无运行消费者。 |
| `OverAxisProtectRadius` | 0.2，配置已进入投影，但 `CameraWorldBasicHistory.Apply` 没有跨轴保护消费。 |
| `FollowRotateCoefficient` | 可达模式 raw 2 为 0.5，当前未消费。 |
| `FollowRotationDamping` | 当前可达轨道全为 0，合同字段已进入投影但无消费者。 |
| `RotateDamping` | raw 1/20 有非零值，但这些模式当前不可达；字段本身无运行消费者。 |
| `LookAtDirection` / `LookAtAnimation` | 可达模式 raw 0/2/3 的 LookAtDirection 均有非零值，当前未消费；动画表为空。 |
| `FollowAnimation.States/Tags` | 当前表为空，因此缺失倍率消费者暂不改变当前数值结果。 |
| `Upward` / `Downward` | Default_Normal 有非默认曲线，但原作升降资格来自邦布变身；普通可琳倍率为 1，不应接入普通跟随。 |

默认轨道的 `FollowOffset` 和 `AimOffset` 均为 `(0,-0.225,0)`，已发布到投影，但 `CharacterCameraFramePlanner.BuildTargetPlan` 没有消费。当前 Body 目标仍使用外部 follow/aim anchor 的绑定位置。这是当前默认构图的一个直接缺口。

## 构图与效果时序

当前帧顺序是：`CharacterCameraSequenceEvaluator` 先做基础轨道、仰角和 Delay 历史；`CameraEffectEvaluator` 再按 Override、Zoom、Stretch、Shake、Shot 修改计划；随后做 pitch clamp、环境约束和 rig 写回。当前碰撞配置 `Enabled=false`，所以环境约束实际返回 `NotEvaluated`。

`CameraWorldBasicData.WithFraming` 已经按 `newRadius * tan(newFov/2)` 换算既有 Offset，避免 Zoom/Stretch 修改 FOV/半径后屏幕锚点被当作米制偏移漂移。但 Delay 的死区/软区越界判断和构图阻尼发生在效果修改前；已有原作证据显示当前 FOV 会先写入 CameraState，再计算构图阻尼。因此“屏幕锚点比例保持”不等于“效果后 FOV/半径参与完整构图求值”的时序已经一致。

## 格挡与支援镜头补充

2026-09-29 追加只读核对，覆盖原作动作事件、状态机、公共相机资源和当前 Corin 资产。原作 `ParryAid_H/L` 是“支援格挡”业务：动作状态带 `LockTargetZone_ParryAid_H/L` 和 `ParryInvicibleZone`，`AttackProperty_01` 带 `AttackAid`、`ParryAid`、`ParryAid_H/L` 标签；不能只把它当成一条普通攻击插播镜头。

原作状态机里 `PlaceHolder -> Attack_ParryAid_H/L_Start` 没有Animator条件，入口由外部技能或战斗流程指定。`Attack_ParryAid_H/L` 与 `_End` 的 battle event pattern 为空；`_Start` 的唯一显式相机事件是在 frame 124 播 `Corin_Attack_Normal_02_CamShake_E_01`，同时在 transition out 移除 `AidAttack_Parry_Avatar_Multi` 标签。用户指认的“格挡镜头”对应公共 `Avatar_Common_ParryAid_H/L_CameraOverrideTrack_01`：同一套三点轨道 `(2,2.5)`、`(0,3.75)`、`(-1,2.2)`，`TopOrbit=(2,0.2)`，`TopCurvature=0.25`，FOV 60，blendIn 0.25，blendOut 1.5，`clearTracks=true`；L duration 0.5，H duration 0.75，忽略 world/owner 时间缩放。

| 原作业务 | 原作镜头证据 | 当前资源 | 当前正式生产者 | 主要缺口 |
| --- | --- | --- | --- | --- |
| `ParryAid_H/L` | 公共 Override、Zoom、Stretch、Shake 均存在；动作只显式发 Normal_02 E_01 Shake | 只有 H/L 与 Auto 的 AttackProperty；没有 Override、Zoom、Stretch 或 Parry Shake 资产 | 无 | 缺支援格挡业务入口、状态与无敌/锁敌窗口；公共 Override 资产未迁入 Profile |
| `Attack_Counter` | 完美闪避后从 `Evade_Back` frame 56 或 `Evade_Front` frame 24，配合 `Trigger_PerfectEvade + Trigger_PressAttackA` 进入；Timeline 发 Zoom/Stretch 和 E 系列 Shake | Zoom/Stretch 资产和 4 个 AttackProperty 存在；Counter Override 和 Shake 资产不在 Profile | 无 | 缺完美闪避反击业务；命中链未闭合；Profile 缺 Counter Shake，且 Zoom/Stretch 无 Profile 引用和生产者 |
| `AssaultAid` | Timeline 发 `OverrideTrack_01`、`Zoom_01`、`Stretch_01` 和 E_01-E_06 Shake；AttackProperty 引用 A 系列 Shake | Zoom_01/02 与 Stretch_01/02 资产存在；Override 和 E/A Shake 资产不在 Profile | 无 | 缺连携突击业务和 Timeline；Profile 缺 Override/Shake，已有 Zoom/Stretch 无生产者 |
| `BeHitAid` | Timeline 发 `Zoom_01`、`Stretch_01` 和 E_01-E_03 Shake；AttackProperty 引用 A 系列 Shake | Zoom_01 与 Stretch_01 资产存在；Shake 资产不在 Profile | 无 | 缺受援/受击响应业务；命中链未闭合；已有 Zoom/Stretch 无生产者 |

当前相机运行时已经有 `CameraOverrideEffectEvaluator`、`CharacterCameraProjectionPayload.TryGetOverride` 和 Timeline 到 `CameraEffectKind.Override` 的正式桥，但没有资源和生产者。Override 合同支持 duration、tag、clearTracks/clearTags、blendIn/out、world/owner/local 时间域；不过 `CameraOverrideTrackPayload` 没有 `TopCurvature` 字段，`CameraOverrideEffectEvaluator` 也未消费 `TopOrbit`，只按 pitch 在中间轨道里取最近点。因此即使先补资源，格挡镜头的顶部弧线仍无法按原作求值。

这条链路的实现顺序不应从相机先开始：先定支援/反击业务入口和状态，再让 Timeline/Ability 发出正式相机请求；相机侧补 Override 的 `TopCurvature/TopOrbit` 求值，最后接命中类 Shake。命中链窗口未闭合前，AttackProperty 里的 A 系列 Shake 不应被表述为已可用。

## Zoom/Stretch 剩余边界

Zoom 当前已完成 Base/Additive 两组内按绝对 FOV 改变量选择。尚未闭合的是独立全局 FOV 覆盖栈、原作资格/接纳规则，以及实例建立时按所属组继承上一帧改变量的完整衔接。当前 5 个可达资源都是 Absolute/Replace，缺口不会让它们立即表现为 Additive，但连续技能切换时的起始值仍不能称为完整复刻。

Stretch 当前已消费分通道 Base/Additive、半径/偏移/滚转选择、`StackingType=0` 的组内起点、特殊仰角实例的进入/退出时钟，以及 `WithFraming` 半径换算。尚未完全闭合的是原作 `anchorRadius`/`anchorRadiusAppend`/动态 `cameraLocateRatio` 的全部中间量、特殊仰角到轨道重采样的完整链、FollowY 点位解析，以及 `IgnorePriorityInEndTime` 这类当前值为 false 的生命周期规则。当前 `RuntimeCamFollowYPoints` 全为空，两个 FollowY 开关全为 false，因此该分支没有当前数值影响。

## 原作业务复刻缺口

### 命中震动

当前普通、Rush、Branch 的 9 个 A 类资源已经在正式 AttackProperty 中保存 `ResourceId` 和 `ShakeOnNotHit`；Fixed/Float32 catalog 能解析成 `PortableAttackCameraShakeComponent` 并通过组件校验。但这条链在 catalog 后断开：没有攻击碰撞查询、命中结果 Fact、`ShakeStrength` 实体变量查询、命中条件判断，也没有把该组件转换成 `PresentationCommandKind.Camera` 或 `CameraEffectRequest` 的运行消费者。因此动作帧 E 类会随 Timeline/Ability 请求触发，而命中 A 类当前不应被描述为可触发。

### Override Track

原作 `battle-0.json` 与 `battle-1.json` 的直接事件一致：`Corin_Attack_AssaultAid` 在 frame 0 启用 `Corin_Attack_AssaultAid_CameraOverrideTrack_01`；`Corin_SwitchIn_Attack` 在 frame 0 和 frame 32 分别启用 `Corin_SwitchIn_Attack_CameraOverrideTrack_01` 与 `02`。`Corin_Attack_AssaultAid_CameraOverrideTrack_02` 存在于 override 数据和 curve 依赖中，但这两个 battle 变体没有找到直接事件引用，不能据此把它当作当前战斗运行链的确定输入。

这 4 个原作 Override 的 TrackSetting 均为 TopOrbit `(2.0,0.2)`、三段轨道、三段 ScreenY、FOV 60、相同 tag 和 `clearTracks=true`；`AssaultAid_01` 的进入/退出是 0.35s/1.5s，`SwitchIn_01` 是 0.25s/1.5s，`SwitchIn_02` 是 0.4s/1.5s，`AssaultAid_02` 是 0.15s/1.5s 且显式 0.5s。当前 `CorinCharacterCameraProfile.m_OverrideTracks` 为空，当前生成能力也只有 Normal/Rush/Branch/Dodge，没有 AssaultAid 或 SwitchIn 业务模块和事件生产者。所以这不是只缺相机资产，而是缺上层的技能/切人业务入口。

### Shot

原作战斗动画事件里有两个可琳 Shot：`Corin_QuestStart` 在 frame 0 启动 `QuestStart_Avatar_Corin_01`；`Corin_SwitchIn_Attack_Ex_Start` 在 frame 0 启动并在 frame 84 结束 `Avatar_Corin_SwitchIn_Attack_Ex_Start_Cam_01`。前者是开场/任务启动演出，后者绑定切人强化 E 起手。

当前 Profile 的 `m_Shots` 为空，两个场景 rig 的 `shotRigs` 也为空。更关键的是，当前通用 Shot 求值虽然保存 `CinePrefabPath`，但求值器不加载或绑定该 prefab，只依赖手工配置的 `CameraShotRigBinding`；原作 Shot 的 prefab 播放、Banner、video type、光照方向曲线、QTE 通过、切人中断、根跟随和 Follow/LookAt 点位规则也没有正式业务消费者。`QuestStart` 与 `SwitchIn_Attack_Ex_Start` 两个上层业务模块同样未接入当前角色能力链。

### 当前运行帧的其余差异

状态输入已把 Idle/Move/Evade 接入 raw 0/3/2 的 Delay 切换，但 raw 1、4、11、12、20、25 仍没有模式请求生产者。已入投影但未消费的 Delay 项包括 `SpeedSmoothTime`、`OverAxisProtectRadius`、`FollowRotateCoefficient`、`FollowRotationDamping`、`RotateDamping`、LookAt 通道和 States/Tags 倍率；其中 States/Tags 当前表为空，暂无数值结果差异。默认轨道 `FollowOffset`/`AimOffset=(0,-0.225,0)` 也还未进入目标构图。

构图时序的差别仍集中在效果修改 FOV/半径之后：当前 Delay 死区、软区和构图阻尼先于 Zoom/Stretch 执行，`WithFraming` 只保持屏幕锚点比例；已有原作证据显示当前 FOV 会先进入构图链。因此连续画面仍可能出现锚点比例正确但阻尼输入时序不同的情况。Zoom/Stretch 的连续切换继承、全局 FOV 栈、特殊仰角轨道重采样和 FollowY 解析仍是正式边界；当前可达资源暂时不触发其中特殊仰角和 FollowY 分支。

## 验证边界

本审计没有执行 Unity 编译、Editor 检查、Play、replay、实机画面对照或 GC 采集。上述“已消费/未消费”是当前源码与正式资产静态回读结果；已有文档中的历史编译和编辑器函数检查只覆盖其当时声明的小函数，不能扩大为完整相机行为验收。
