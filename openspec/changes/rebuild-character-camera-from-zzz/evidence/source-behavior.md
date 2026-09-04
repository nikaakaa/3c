# ZZZ 相机行为取证

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
| `FrameOnePointInCorePolicy_ByTrack.GetData` | cameraOrbits、screenOffset、aspectRatio、FOV、ElevationRatio、PolarAngle、makeContextDependent | 按轨道列表生成单点取景数据 | metadata session 829 `methods.csv`；完整调用分支仍需继续反汇编 |
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

metadata 只证明类型、字段、方法身份和地址，不证明函数体之外的完整演出规则。上表把这种边界保留下来，不能把 metadata 名称当成公式或阶段顺序。

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

这些时点只确认命令发生的位置；同帧各阶段的写入先后、效果叠加和退出仍由消费者取证闭合。

## 未闭合项

1. `CameraModuleAvatarDataConfig` 的完整 Profile 数值和球面/轨道组的实际资源闭包尚未从来源中完整解析。
2. 单点/双点/多点函数的数学分支、位置/旋转阻尼历史、`BlendFromCurrent`/`MoveByBlending` 的起终点和取消规则尚未闭合。
3. `CameraCutscenes` 的 5 个 block 内容不同，4 个 typetree 只有 identity，另 1 个 block 未找到 source block；Shot、公共曲线、Timeline 绑定不能按名字补齐。
4. `AvatarTrack` 175 份副本有 52 个内容 hash，角色/场景消费者尚未逐项对账。
5. `WorldBasicCameraData` 到活动 Cinemachine 实例的每阶段写入、Brain 推进和最终 basis 回读顺序尚未闭合。

在上述缺口闭合前，正式实现只能建立数据合同和失败诊断，不能发布声称完整移植的默认算法或 Corin 运行资源。
