# ZZZ 相机行为取证

> 历史来源证据：保留此前取证的函数、字段、调用和未闭合项；未在 2026-09-13 重新提取原数据。页内“当前”指取证时点，不作为今天的执行指令或完成状态。当前实现见本目录 current-implementation.md，范围与执行要求见本 change 的 proposal/design/tasks。

记录时间：2026-09-04

本附件只收录已经从同一构建的 metadata、GameAssembly 反汇编和 Corin 解码配置中确认的行为边界。没有确认消费者的字段不进入正式算法。

## 元数据会话与函数身份

| 类型/函数 | 已确认输入或字段 | 已确认输出/职责 | 来源证据 |
|---|---|---|---|
| `MoleMole.Config.PipelineCameraAvatarConfigData` | `cameraAvatarGroup`、普通目标锁定开关/配置、Boss 锁定开关/配置、默认待机触发间隔、默认球面 key、角色球面组 | 角色相机配置根，向角色相机扩展配置提供装配入口 | metadata session 829 `fields.csv` |
| `MoleMole.CameraModuleAvatarDataConfig` | Near/Far Clip、拖拽俯仰区间、相机定位半径、默认俯仰、默认球面、旋转/换人过渡、默认平滑、上下运动、状态覆盖、静音开关、碰撞算法与碰撞配置 | 角色 Camera Profile 与碰撞规则的运行数据 | metadata session 829 `fields.csv` |
| `MoleMole.CameraModuleAvatarDataConfig.InnerInit` | 配置实例；自定义碰撞配置 | 构建碰撞运行 tag；没有自定义碰撞时不进入该分支 | RVA `0x150DC6C0`，函数 hash `3b2f6d98ee41ab87b4ac00b6c94f3ac22126af7f8a112bb734133c9d80cf2629` |
| `MoleMole.CameraModuleAvatarDataConfigExt.Init` | `PipelineCameraAvatarConfigData` 及扩展数据 | 校验全局配置并枚举/初始化扩展数据 | RVA `0x1279F110`，函数 hash `be9f2a52067c52693cf357364c14c09b1e2f6c26b8ab744166ff6264595f19e0` |
| `MoleMole.Config.CameraSequence.CameraSequence<T>` | 序列时间、输入数据、上下文；序列项、变量和事件集合 | 序列长度、速率、循环、上下文依赖、采样时间、`GetSequenceData`、事件与运行数据路径 | metadata session 829 `methods.csv` |
| `FrameOnePointInCorePolicy_ByHSF.GetData` | EntityHeight、heightRatio、FOV、Vector2 screenOffset、makeContextDependent | 生成单点取景的 `WorldBasicCameraData` | RVA `0xF134030-0xF134852`，函数 hash `81187c1d3f5fe3dc88b2f2ef0682d9c2ed8e3d9ec0cd6add957b266a115fa607` |
| `FrameOnePointInCorePolicy_ByScreenOffset.GetData` | aspectRatio、FOV、screenOffset、radius、makeContextDependent | 生成屏幕偏移单点取景数据 | RVA `0x134F3AB0-0x134F3C1E`，函数 hash `29d5fbbe30e65b2201e70d1df0b5f84233ac404682b9e65a57827c9d68046a9b` |
| `FrameOnePointInCorePolicy_ByTrack.GetData` | cameraOrbits（仅 height/radius）、screenOffset 列表、aspectRatio、FOV、ElevationRatio、PolarAngle、makeContextDependent | 按轨道列表生成单点取景数据 | metadata session 829 `methods.csv`；完整调用分支仍需继续反汇编 |
| `FrameTwoPointsInCorePolicy_Chat.GetData` | 主/副点屏幕偏移、角色/目标高度、FOV、pitch/角度范围、BeginCameraData | 双点构图数据 | RVA `0x12A4B2F0`，metadata session 829 |
| `FrameMultiplePointsInCorePolicy_Chat.GetData` | 半径、高度偏移/比例、点集合、角度范围、FOV、layerMask、fallback 双点策略 | 多点构图数据 | RVA `0x12EF8430`，metadata session 829 |
| `FrameOneEntityInCoreSpace.GetSequenceDataInternal` | EntityId、EntityWorld、实体位置、frame/rotation policy、playLength 与上下文开关 | 单实体序列采样 | RVA `0x143660F0`，metadata session 829 |
| `FrameTwoEntitiesInCoreSpace.GetSequenceDataInternal` | 主/副 EntityId、EntityWorld、位置 policy、frame policy | 双实体序列采样 | RVA `0x11467E40`，metadata session 829 |
| `FrameMultipleEntitiesInCoreSpace.GetSequenceDataInternal` | 主 Entity、多个副 Entity、位置 policy、frame policy | 多实体序列采样 | RVA `0xF1326C0`，metadata session 829 |
| `FixedInCoreSpace.GetSequenceDataInternal` | fixedPolicy、playLength、activeChannel、上下文开关 | 固定核心空间序列采样 | RVA `0x1A4A5320`，metadata session 829 |
| `HandleCameraVolume.GetSequenceDataInternal` | NearClipPlane、CollisionData、LineOfSight 开关 | 相机 volume/collision 阶段数据 | RVA `0x16C35E90`，metadata session 829 |
| `CameraShotData` | cine prefab、近/远裁剪面、duration、时间尺度开关、follow/lookAt 偏移、blend 定义、core-space/delta blend、advanced blend flag、忽略 look-at 绑定 | Shot 资源与运行绑定需要的镜头参数 | metadata session 829 `fields.csv`/`methods.csv` |
| `CameraTrackBlending.GenerateBlender` | 覆盖时长、debug name、`WorldBasicCameraDataDeltaFlag` | 根据原 blend 规则创建 blender | RVA `0x1623AC80`，metadata session 829 |
| `MoleMole.MonoStageEnv.GetVirtualCamera` | virtual camera identity string | 通过 stage 环境查找并返回 `CinemachineVirtualCamera` | RVA `0x129CCBB0-0x129CCC8D`，函数 hash `90f63664bd08882cc70d953c7d9364b0aec48d96f0982a3500d7865289003f49` |
| `MoleMole.MonoStageCamera.Awake` / `ActiveCam` | followName、lookAtName、virtualCamera、active bool | 取得承载实例并切换活动状态 | Awake RVA `0x1909D150-0x1909D1FE`，hash `80ddafb3287b177231289ba4f96ac0b62688b9d67d2cebb6d758b43b9b568f96`；ActiveCam RVA `0x1909D580-0x1909D5DB`，hash `0d25d4a3a9a5f39584f4f0d94edb89ee0a13e2f252089227d49ea070de7e2e90` |

### 取景轨道采样的新增证据

对同版本 `GameAssembly.dll` 的只读反汇编补齐了 `FrameOnePointInCorePolicy_ByTrack` 的采样骨架：

- `GetData` 先解析 `aspectRatio`、`ElevationRatio`、`PolarAngle` 以及两个 context-dependent provider；轨道列表和屏幕偏移列表不会直接按整数下标读取。
- `LAFFKCJNBDB`（RVA `0xF136C50`）把 `cameraOrbits` 的前三个 `height/radius` 转成三个键为 `0.0/0.5/1.0` 的轨道采样项；`BHALLONOACA`（RVA `0xF136570`）对 `screenOffset` 列表做同样处理。两个函数都实际写入三项，键值来自 `0x00000000/0x3F000000/0x3F800000`。
- `PipelineCamera.InterpCurveMode` 的四个值为 `Linear`、`Cubic`、`Constant`、`Bezier`。ByTrack 通过 `0x10A1AC60` 把上述列表转换为运行曲线记录，该构造路径把记录的插值模式设为 `Constant`；`0x10328820` 仍会做区间查找并在落入区间时进入 `0x1036ECB0`，但当前这条来源轨道的实际曲线模式不是默认线性插值。采样比值在进入查找前被压到 `[0,1]`，因此现有 `RoundToInt(ElevationRatio * (count - 1))` 既不能代表原采样边界，也不能代表原曲线记录。
- `GetData` 先分别取得轨道结果和屏幕偏移结果，再使用 `PolarAngle` 构造空间方向，计算轨道平面长度，最后进入相机数据构造和有效性/碰撞检查。调用序列对应 `elevation = atan2(height, radius) * Rad2Deg`、`rotation = Quaternion.Euler(elevation, PolarAngle, 0)`、`radius = sqrt(height * height + radius * radius)`，并将屏幕偏移和 FOV 写入 `WorldBasicCameraData.Create`；结果不是只改变当前 FreeLook 轨道组的半径。
- `FrameMultiplePointsInCorePolicy_Chat.GetData` 的实际函数体在 RVA `0x12EF9204` 调用 `FrameTwoPointsInCorePolicy_Chat.GetData`（RVA `0x12A4B2F0`），说明多点策略存在来源明确的双点回退分支，不能把所有多点输入都强行压成单点。

### 双点取景的新增证据

对 `FrameTwoPointsInCorePolicy_Chat.GetData`（RVA `0x12A4B2F0`）的函数体反汇编确认了以下链路：

- 函数直接读取实例内的 `aspectRatio`、`heightRatio`、玩家/目标高度比例上下限、FOV、固定 pitch、主/副水平偏移、主垂直偏移、目标垂直偏移上下限、pitch 上下限、玩家高度、目标高度和 `BeginCameraData`；这些读取位置与 metadata 的字段布局一致。
- `BeginCameraData` 先通过 `WorldBasicCameraData.get_Location`（RVA `0x1E88D780`）派生相机世界位置，再把两个输入点分别转换成相对该位置的水平方向并做长度归一化；归一化带有接近零长度的固定分支，不是直接对两个点取平均。
- 两个方向随后进入角度比较、主/副点选择和水平/垂直构图计算，结果通过辅助构造函数 `0x12A50210` 写回 `WorldBasicCameraData`；代码同时保留无效或缺失依赖时的失败跳转。
- 这条函数的最终角度夹取、相机半径计算和各偏移字段的精确组合仍需继续对 `0x12A50210` 及其调用者解码；当前证据足以拒绝“多点/双点统一取中点”的近似实现，但不足以发布双点 evaluator。

以上闭合了 ByTrack 的采样骨架、曲线模式、角度换算和一个回退调用关系；其有效性检查的完整失败回退、碰撞阶段的世界输入以及结果如何在所有活动 Cinemachine 实例之间切换仍未闭合。

### WorldBasicCameraData 字段布局

同一 829 运行元堆中的 `PipelineCamera.WorldBasicCameraData` 类型索引为 `38261`，是 `60` 字节的值类型。字段记录的值类型负载偏移为：

| 字段 | 类型 | 值类型负载偏移 | 记录偏移 |
|---|---|---:|---:|
| `_pivotLocation` | `Vector3` | `0x00` | `0x10` |
| `_rotation` | `Quaternion` | `0x0C` | `0x1C` |
| `_radius` | `float` | `0x1C` | `0x2C` |
| `_offset` | `Vector2` | `0x20` | `0x30` |
| `_fieldOfView` | `float` | `0x28` | `0x38` |

同类型还存在一个静态 `Fallback` 字段，不能当作实例输出。上述值类型负载可解释 `ByTrack.GetData` 最终写回的 `44` 字节相机核心数据；工程侧 `CameraFramePlan` 已改为直接持有这五个实例字段，旧的 `FollowPoint/AimPoint/Orbit` 平台字段不再作为计划输出。

工程侧以 `CameraWorldBasicData` 表达这五个实例字段，不携带静态 `Fallback`，并由 `CameraFramePlan`、转场、Zoom、Stretch 和 Rig Adapter 共用同一份值。

`WorldBasicCameraData.get_Location`（源码段 RVA `0xA3F750`）不是存储字段读取：它把值类型负载交给 `0x1E88D780` 计算 `cameraToPivot`，再用 `pivotLocation - cameraToPivot` 得到位置。该计算读取 `_rotation`、`_radius` 和 `_offset`，其中局部向量由 `offset.x/offset.y/radius` 组成；因此 `Location` 不能用当前 FreeLook 的中心半径属性直接替代。工程侧合同提供同名的 `CameraToPivot` 与 `Location` 派生值。

同一来源的 `MoleMole.CameraOrbit`（类型索引 `50865`）是只有 `m_Height float`（记录偏移 `+0x10`）和 `m_Radius float`（记录偏移 `+0x14`）的值类型；它没有 `ScreenY` 或其它屏幕构图字段。屏幕构图由独立的 `MoleMole.Config.ConfigCameraComposer`（类型索引 `57503`）承载，字段为 `BiasY`、`ScreenX`、`ScreenY`、`BiasX`（记录偏移 `+0x10/+0x14/+0x18/+0x1C`）。工程侧 `CameraOrbitPayload` 现已只保留 `Height/Radius` 与源轨道几何对齐；`CameraOverrideTrackSettings` 中的 `ScreenY` 仍属于尚未闭合的 Override 构图输入，不能重新写回轨道 payload 或旧 FreeLook 轨道数组。

### WorldBasicCameraData 的序列消费者与活动承载

新增的类型/字段证据把 `WorldBasicCameraData` 的归属进一步固定在相机数据管线，而不是 FreeLook 轨道：

- `CameraSequenceCollectionPlayer<WorldBasicCameraData>` 作为相机序列集合播放器的成员，`ICameraSubModule<WorldBasicCameraData>` 作为子模块输入；这证明序列结果以该值类型在相机模块内部流转，不是直接写 `CinemachineFreeLook`。
- `CameraSequence.FixedInCoreSpace` 的 `activeChannel` 类型是 `WorldBasicCameraDataChannel`；`CameraShotData` 的进入/退出高级混合标记类型是 `WorldBasicCameraDataDeltaFlag`。这两处都以 WorldBasic 字段通道表达阶段选择或混合差异。
- `MonoStageEnv` 的 `OPKELHFPFNI` 字段是 `Dictionary<string, CinemachineVirtualCamera>`，位于实例偏移 `0x90`。`GetVirtualCamera(string)` 从该字典按身份查找并返回 `CinemachineVirtualCamera`，不是返回 `CinemachineFreeLook`。
- `MonoStageCamera` 的字段为 `followName`（`+0x20`）、`lookAtName`（`+0x28`）、`virtualCamera`（`+0x30`）和活动标记（`+0x38`）。`Awake`、`ActiveCam` 及其绑定辅助函数围绕这一个 `virtualCamera` 实例运行；目前只闭合了字段和承载身份，follow/look-at 名称最终解析到哪一个场景对象、WorldBasic 数据由哪一层写入该实例仍未闭合。
- 默认第三人称相机 `MoleMole.Cameras.ScopedOverShoulderCamera` 持有 `CameraVariableSubModule<WorldBasicCameraData>`（字段 `+0x270`），其 `HJOEFCAMAD(float)` 与 `MCOJPGDGMPI(CameraVariableFetchContext<WorldBasicCameraData>)` 都返回 `WorldBasicCameraData`；配置侧同时包含 follow/camera offset、FOV、roll、位置阻尼、输入灵敏度以及 Shake/Zoom/Stretch 开关。由此可以确认默认相机的核心结果也经过 WorldBasic 数据子模块，而不是把相机轨道交给 FreeLook 自己求值。

### Pipeline VCam 到 CameraState 的新增证据

同一 829 运行元数据中的 `MoleMole.Cameras.NapVirtualPipelineCamera`（类型索引 `69714`）是 `CinemachineVirtualCameraBase` 的子类，不是 `CinemachineFreeLook`。它的实例字段把相机管线和 Cinemachine 状态放在同一个活动承载上：

| 字段 | 类型 | 记录偏移 |
|---|---|---:|
| `HHFPFIIMAIC` | `CameraSequenceCollectionPlayer<WorldBasicCameraData>[]` | `+0x80` |
| `FFMLIFECBEN` | `Nullable<WorldBasicCameraData>` | `+0x9C` |
| `FCOLECGOBJM` | `Nullable<WorldBasicCameraData>` | `+0xCC` |
| `lookAt` | `Transform` | `+0x100` |
| `follow` | `Transform` | `+0x108` |
| `PNKMKMKAMED` | `Cinemachine.CameraState` | `+0x110` |

该类型的运行入口和数据转换入口为：

- `InternalUpdateCameraState(Vector3, float)`，RVA `0x1623B8F0`：活动 VCam 的 Cinemachine 更新入口，函数体会更新 Pipeline 数据并继续处理 CameraState。
- `HAHPGHPCIKB(WorldBasicCameraData&, WorldBasicCameraData&, CameraState&)`，RVA `0x1623EA00`：接收两份核心相机数据和一个 `CameraState` 输出引用。
- `IOPJLAMFIPJ(CameraState&)`，RVA `0x1623ECF0`：对 `CameraState` 做进一步输出处理。
- `HKDKONGDANJ(CameraState&)`，RVA `0x1623F260`：从 `CameraState` 取得两份 `WorldBasicCameraData` 结果。
- `GetLastCameraData()` / `GetLatestCameraData()`，RVA `0x1623DB50` / `0x1623DCB0`：暴露当前和最新的两份 `WorldBasicCameraData` 结果。

`PipelineCamera.CameraContext<,>` 还保存 `_lastCameraData`、`_lastReferenceCameraData` 和 `_mCamera Camera`，并提供 `GetLastFinalCameraData()`；`PipelineCamera.FinalCameraData` 是 `48` 字节值类型，实例字段只有 `location Vector3`、`rotation Quaternion`、`fieldOfView float`。这把源侧阶段关系固定为“核心 WorldBasic 数据 → Pipeline VCam/CameraState → FinalCameraData”，但当前快照还没有证明 `FinalCameraData` 写回 Unity `Camera` 属性的唯一函数和同帧回读点。

因此标准 `CinemachineVirtualCamera`（`MonoStageEnv` 的字典返回类型）与默认 `NapVirtualPipelineCamera` 是两个已确认但职责不同的承载类型：前者是 stage 资源的 virtual-camera 身份返回值，后者是默认相机管线的活动计算载体。工程侧在唯一写入 owner 和最终回读顺序闭合前，不能把二者合并成一个适配入口，也不能在现有适配器旁边再加一条并行输出路径。

工程侧当前已将 `CinemachineCameraRigAdapter` 和六个当前产品资源切换到标准 `CinemachineVirtualCamera`，并由 Adapter 直接应用 `WorldBasicCameraData.Location/Rotation`、镜头参数和一次手动 Brain 推进；这只闭合了当前平台绑定，不等于已经还原源侧 `NapVirtualPipelineCamera` 的阶段组件、最终 `FinalCameraData` 写回和同帧回读顺序。后续仍必须沿唯一 Adapter/Brain 输出链补齐这些来源证据，不能恢复 FreeLook 或增加第二条运行输出路径。

metadata 只证明类型、字段、方法身份和地址，不证明函数体之外的完整演出规则。上表把这种边界保留下来，不能把 metadata 名称当成公式或阶段顺序。

## 补充闭合的 Profile、阻尼、锁定和曲线数据

`replication-guide/analysis/基础镜头.md` 已从同一构建的原始对象读出 4 组角色镜头配置和 36 组球面/轨道配置。`Default_Normal` 的一组已确认值为：NearClipPlane `0.1`、FarClipPlane `2000`、CameraLocateRadius `3.75`、Default FOV `50`、Default elevation `0.60000002`、Default smooth time `0.15000001`；默认轨道为 `(height, radius) = (2.2249999, 2.5)`、`(0.22499999, 3.75)`、`(-0.77499998, 2.2)`，ScreenY 为 `0.34999999/0.5/0.5`。同一资料还记录了 `ROTATE_STATETRANSITION_TIME=3`、`CHANGEAVATAR_STATETRANSITION_TIME=0.30000001`、默认延迟组和普通目标/Boss 锁定曲线。

`replication-guide/analysis/公共曲线.md` 已确认 6 条公共曲线的关键帧、切线、权重和边界模式，`Camera_Default_Curve_01/02/04` 与两条 Shake 衰减曲线及 `Camera_ShakeSpatial_Curve_01` 的三份来源数值一致。六个 `CamShake_A_01..03/E_01..03` 标准配置键仍没有定位到正文，不能当成已导入运行资源。

`replication-guide/analysis/镜头原生参数.md` 已确认 5 份 CameraCutscenes、4 份 CameraLockDatas 和 3 份基础镜头对象均消费到原生数据末尾；其中 3 份 Shot 使用 compact-blend-words 布局，每条记录有 5 个未命名值。五份 CameraCutscenes 均直接包含两个 Corin Shot key：`Avatar_Corin_SwitchIn_Attack_Ex_Start_Cam_01` 使用 `Assets/NapResources/CameraAnim/Combat/Avatar_Female_Size01_Corin_Cam_SwitchIn_Attack_Ex_Start.prefab`，近/远裁剪面为 `0.01/6000`、duration 为 `-1`、进入/退出时长为 `0.5/1.5`；`QuestStart_Avatar_Corin_01` 使用 `Assets/NapResources/CameraAnim/Combat/Avatar_Female_Size01_Corin_Cam_QuestStart.prefab`，近/远裁剪面为 `0.1/6000`、duration 为 `-1`、进入/退出时长为 `0.1/1.0`。这确认了原生表中的资源身份和值，但 prefab 本体、Timeline 绑定和 compact-blend-words 的未命名字段仍未形成当前工程可执行的引用闭包；不能按名字直接生成 Shot 资产。`replication-guide/analysis/镜头Shot参数.md` 中的其它候选仍未形成真实引用闭包。

上述资料闭合的是来源字段和值，不等于已经闭合当前 Corin 实例的选择、完整算法分支或最终输出 owner。

## 已确认的 Corin 效果字段

### Shake

81 项资源都包含可独立落地的方向、频率、幅度、衰减和生命周期字段：`ShakeType`、`CameraShakePropConfigEnum`、`AngleVertical`、`NoiseAngle`、`RadiusLength`、`DistanceToPlane`、`NoiseRatio`、`ShakeTotalTime`、`Frequency`、`RollAmplitude`、`PitchAmplitude`、`YawAmplitude`、`ShakeCenterAttachPoint`、`RealtimeVibration`、`DissipationMode`、`ImpactRadius`、`DissipationDistance`、`CustomCurveKey`、`FadeInDuration`、`FadeInCurve`、`FadeOutDuration`、`FadeOutCurve`、`CurveKey`、原字段拼写 `IngoreTimeScale`、`PlayStackingType`、`PlayPriority`、`DataPriority` 和 `StandardConfigKey`。

已经观察到的数值不能合并成一条通用模板：普通攻击 A 项常见 `RadiusLength=0.025`、`Frequency=12`、`IngoreTimeScale=true`、总时长 `0.6`；E 项出现 `RadiusLength=0.1..0.25`、`Frequency=20` 且时间尺度开关为 false；Rush E 还出现 `ShakeType=0`、半径 `0.04`、时长 `0.3` 或 `1.5`，使用不同衰减曲线。方向和幅度需要按资源与原消费者分别保留。

### Zoom

18 项资源字段闭包为 `StartCurveKey`、`EndCurveKey`、`DataPriority`、`IngorePriorityInEndTime`、`IgnoreWorldTimeScale`、`IgnoreLocalAvatar`、`IgnoreOwnerTimeScale`、`LastTime`、`StartTime`、`StackingType`、`Fov`、`FovVariationType`、`DelayTime`、`EndTime`、`PlayStackingType`。`LastTime` 可为 `-1`，不能在导入时统一改成零或 Clamp。

### Stretch

18 项资源字段闭包为 `EndCurveKey`、`StartCurveKey`、`RuntimeCamFollowYPoints`、`RotationZ`、`IgnoreLocalAvatar`、`IsAppliedEleRatio`、`IsAppliedEndEleAngle`、`PlayStackingType`、`RuntimeCamFollowYOffsetRatio`、`IsEleAngleAbsoluted`、`IngorePriorityInEndTime`、`IgnoreWorldTimeScale`、`IsEndEleAngleAbsoluted`、`ElevationAngleMin/Max`、`RecoilTime`、`DataPriority`、`EndElevationAngleMin/Max`、`ApplyAthPtsCamFollowYOffset`、`ApplyRuntimeCamFollowYOffset`、`CamOffsetLocalCoords`、`IgnoreOwnerTimeScale`、`HoldTime`、`DelayTime`、`PosOffsetX/Y/Z`、`RadiusRatio`、`StretchTime` 和 `FovVariationType`。已观察到半径、局部位置和绝对/相对俯仰均会改变，不能折算为 FOV Kick。

### OverrideTrack

4 项资源都包含轨道/构图设置、优先级和进入/退出生命周期。代表项确认了顶部 orbit、多个 `height/radius` orbit、screenY 列表、Follow/LookAt 偏移、FOV、blend in/out 时长和曲线、tag、clearTracks/clearTags、duration、IgnoreWorldTimeScale、IgnoreOwnerTimeScale、IgnoreLocalAvatar 等字段。`duration=-1` 与 tag 清理语义必须保留。

## 事件时点

来自 `20260904_corin_attack_event_index_v3.json` 的 108 条事件必须以原帧号进入正式 Timeline/Graph 请求链，不能在运行时再读取该索引文件。

| 动作 | 已确认时点 |
|---|---|
| AssaultAid | 第 0 帧 Override、Zoom、Stretch；第 8、19、32、52、55 帧 Shake |
| ParryAid H/L | 共用第 124 帧 Shake |
| SwitchInAttack | 第 0、32 帧构图/效果变化；第 42、54、67、71 帧 Shake |

`SwitchInAttack` 在本附件中只表示来源事件索引身份和已观察时点，不表示当前 BTSMTL 已拥有换人、主控切换或跨角色相机消费者；其是否属于单角色攻击演出，待后续设计确认。

这些时点只确认命令发生的位置；同帧各阶段的写入先后、效果叠加和退出仍由消费者取证闭合。

## 未闭合项

1. `CameraModuleAvatarDataConfig` 的多组 Profile 数值已解析，但当前 Corin 实例实际选择的配置键、区域变体和完整资源闭包尚未从消费者中确认。
2. 单点/双点/多点函数的数学分支、位置/旋转阻尼历史、`BlendFromCurrent`/`MoveByBlending` 的起终点和取消规则尚未闭合。
3. `CameraCutscenes` 的 5 个 block 内容不同，4 个 typetree 只有 identity，另 1 个 block 未找到 source block；Shot、公共曲线、Timeline 绑定不能按名字补齐。
4. `AvatarTrack` 175 份副本有 52 个内容 hash，角色/场景消费者尚未逐项对账。
5. `WorldBasicCameraData` 到活动 Cinemachine 实例的每阶段写入、Brain 推进和最终 basis 回读顺序尚未闭合。

在上述缺口闭合前，正式实现只能建立数据合同和失败诊断，不能发布声称完整移植的默认算法或 Corin 运行资源。
