## Context

当前Camera已经拥有正确的上游边界：BTSMTL、Timeline和Action只提交typed request，Camera保持local-only，默认follow消费最终`CharacterBodyPresentationFrame`。需要重构的是从request到Cinemachine之间的控制层。

现有实现链路是：

```text
LookAxis
  -> Unity Input Adapter锁存Vector2
  -> CharacterCameraPresentationRuntime
  -> CameraStateResolver选择单个mode
  -> 硬编码FOV + Follow/Aim点
  -> ThirdPersonCameraController
  -> 单个CinemachineFreeLook
```

这条链的主要缺口：

- mode没有自己的构图参数，五个mode只有FOV差异。
- transition只计算progress，没有混合Rig输出。
- blend out和interrupt policy没有生效。
- Pointer delta与Stick rate被当成同一种输入。
- 非FOV cue没有输出到adapter。
- Presentation、Input和Host依赖具体MonoBehaviour类型。
- Scene、Builder和Runtime同时保存Camera参数。
- Camera diagnostics类型存在，但没有进入正式Trace。

## Goals

- 让普通跑动、瞄准、锁定、动作聚焦和技能特写具有真实不同的构图与输入手感。
- 让作者只在一个Profile和一个共享Rig Prefab中调参。
- 让Camera request、mode mixing、Rig plan、Cinemachine实现保持清楚分层。
- 让鼠标和手柄获得稳定、可解释、与帧率无关的输入语义。
- 让所有本地玩家产品复用同一Camera实现，不随网络模型复制配置。
- 保持Camera不进入Gameplay state、Rollback snapshot和网络同步。

## Non-Goals

- 不实现完整敌人搜索、目标评分、目标切换或目标UI。
- 不实现自由摄影机、观战相机、Photo Mode或关卡Rail Camera系统。
- 不自研最终Camera碰撞和世界Pose solver。
- 不让Timeline直接控制Cinemachine priority、Transform或Brain。
- 不保留FreeLook、3rd Person Follow和自研solver三种并行实现。

## Selected Architecture

```text
RenderFrame Capture
  -> CameraLookInputSample + CameraBasisSnapshot

Committed Camera Producer
  -> State / Response / Target / Cue lifecycle
  -> CameraStateResolver
  -> CameraModeMixer
  -> CameraTargetResolver
  -> CameraModifierResolver
  -> CameraRigPlan
  -> ICameraRigAdapter
  -> CinemachineGameplayRig / CinemachineShotRig
  -> CameraBasisSnapshot + CameraDebugSnapshot
```

`CharacterCameraPresentationRuntime`继续是唯一编排边界。它拥有request生命周期和resolver，但只依赖`ICameraRigAdapter`、`ICameraMovementBasisProvider`和look input合同，不依赖Cinemachine类型。

## Decision: 借Lyra的Mode Stack语义，不照搬UE Camera Component

Lyra的关键价值不是C++或GameplayTag，而是以下边界：

- 每个Camera Mode输出完整View数据，而不是只输出一个枚举。
- Mode拥有blend time、blend function、blend exponent和reset interpolation。
- Mode Stack维护多个活跃mode的权重并生成最终View。
- Gameplay只决定需要哪个mode，不直接计算最终相机。

本项目沿用现有priority request模型，不引入真正LIFO栈。原因是Camera请求可能同时来自StateMachine、Timeline、Action和Target policy，priority + stable source arbitration更符合现有producer生命周期。借用的是“完整mode输出 + 正式混合”，不是Push/Pop API和GameplayTag依赖。

参考：

- [LyraCameraMode.h](https://github.com/LeNidViolet/Lyra/blob/main/Source/LyraGame/Camera/LyraCameraMode.h)

## Decision: Gameplay Rig使用Cinemachine 3rd Person Follow

项目当前使用Cinemachine 2.10.7。官方`3rd Person Follow`已经提供第三人称mini-rig、shoulder offset、vertical arm、camera distance、分轴damping和内建collision resolution。它要求控制脚本旋转独立Follow Target，适合把yaw/pitch保留在Camera本地状态，而不是写入角色Transform。

正式Gameplay Rig结构：

```text
PlayerCameraRigRoot
  MainCamera + CinemachineBrain + CinemachineCameraRigAdapter
  FollowPoint
  AimPoint
  RotationPivot
    GameplayVirtualCamera
      Cinemachine3rdPersonFollow
      Composer / 3rdPersonAim（按正式Aim需求）
  ShotRigSlots
    SkillCloseupSlot...
```

FreeLook、Aim、LockOn和ActionFocus不各建一台长期并行vcam。它们共用GameplayVirtualCamera，并由`CameraRigPlan`连续混合shoulder、distance、vertical arm、composition、damping、pitch range和FOV。这样能保持orbit连续，也避免mode切换时四份Cinemachine内部状态互相跳变。

SkillCloseup属于镜头语言完全不同的shot，允许使用独立显式Rig Slot。Shot Rig不自己抢priority，adapter只按`CameraRigPlan`激活当前slot并应用transition。

参考：

- [Cinemachine 2.10 3rd Person Follow](https://docs.unity.cn/Packages/com.unity.cinemachine@2.10/manual/Cinemachine3rdPersonFollow.html)

## Decision: Profile拥有业务可解释参数，Prefab只拥有组件绑定

`ThirdPersonCameraPresentationProfile`是唯一调参真相，包含：

```text
LookInputPolicy
  PointerSensitivityDegreesPerPixel
  StickYawDegreesPerSecond
  StickPitchDegreesPerSecond
  StickDeadZone
  InvertX / InvertY

CameraModeProfile
  Mode
  RigSlotId
  FieldOfView
  PitchMin / PitchMax
  ShoulderOffset
  VerticalArmLength
  CameraDistance
  ScreenPosition
  PositionDamping
  RotationDamping
  CollisionPolicy
  RecenterPolicy
  DefaultResponsePolicy
  DefaultTransition
```

共享Prefab只保存Main Camera、Brain、Gameplay vcam、Shot Rig和target Transform的显式引用。Prefab不保存另一份mode参数表。Adapter preparation把Profile参数映射到组件；缺少Slot、组件或参数直接失败。

业务收益是Standalone、Fixed、Rollback和ServerAuthoritative本地玩家看到同一种手感。代价是调整Camera需要改Profile而不是直接改某个Scene vcam；这是有意选择，避免产品Scene漂移。

## Decision: CameraRigPlan输出Rig参数，不输出最终世界Pose

current spec把shoulder、distance和collision distance都留给adapter，造成adapter必须按mode重新查表。重构后边界改为：

- Presentation Runtime可以输出最终typed Rig参数和transition结果。
- Adapter只把参数写入明确Cinemachine组件并执行Manual Brain Update。
- Cinemachine计算最终Camera世界位置、旋转、damping和collision obstruction。
- Presentation Runtime不执行Physics查询，不读取Cinemachine内部State反推业务mode。

这样“为什么Aim更近、为什么LockOn偏右、为什么SkillCloseup不响应输入”都能由Rig Plan解释，同时没有自研第二套Camera solver。

## Decision: 输入按设备语义分开

当前`LookAxis`同时绑定`<Pointer>/delta`和`<Gamepad>/rightStick`。两者数值含义不同：

- Pointer是本RenderFrame发生的位移，不乘delta time。
- Stick是持续速率，需要乘presentation delta，且需要dead zone与响应曲线。

新增`CameraLookInputSample`保存value、source kind和render frame。Input Adapter在`CaptureRenderFrame`读取`InputAction.activeControl`确定Pointer或Rate来源，并和Camera basis同时锁存。Camera Runtime按Profile处理输入，再应用当前response policy。

同一RenderFrame的camera-relative move也只读锁存basis。Float32 Adapter当前读取live basis的路径删除，与Fixed Adapter统一。

## Decision: Mode仲裁和参数混合分离

`CameraStateResolver`只负责：

- 过滤失效producer与terminal action。
- 按priority、weight、source identity和producer generation选定目标state。
- 处理HoldUntilSourceEnds、Cut和BlendOut进入/退出规则。

`CameraModeMixer`负责：

- 维护来源mode、目标mode、elapsed、duration和curve。
- 混合FOV、pitch limit、shoulder、distance、vertical arm、composition、damping和look response weight。
- 在显式handoff点切换target binding、Rig Slot与离散collision policy。
- reset tracking时一次性清除旧Cinemachine interpolation，不跨帧保留隐藏过渡。

稳定tie-break必须包含source identity与producer generation，不能依赖Dictionary枚举顺序。

## Decision: Modifier输出有限typed channel

Modifier顺序固定为：

```text
Base Mode Plan
  -> Target Framing
  -> Recoil
  -> FOV Kick
  -> Shake
  -> Collision Response Policy
  -> Final Rig Plan
```

- Recoil输出yaw/pitch impulse和衰减，不直接写FreeLook axis。
- FOV Kick输出lens offset和衰减。
- Shake输出ImpulseId、amplitude、frequency/duration，adapter映射到Cinemachine Impulse或Noise。
- CollisionCorrection只修改正式collision response policy；实际障碍求解仍由3rd Person Follow完成。
- Custom必须在Projection中映射到有限已注册modifier kind，否则build失败。

## Decision: 一个共享Player Camera Rig Prefab

当前Standalone、两种ServerAuthoritative Scene和Gameplay Lab分别保存相机对象，Gameplay Lab Builder还会程序化创建FreeLook。重构后新增一个共享Prefab作为唯一Unity装配真相：

```text
Assets/Prefabs/Camera/ThirdPersonGameplayCameraRig.prefab
Assets/Configs/Camera/ThirdPersonGameplayCameraProfile.asset
```

各产品Scene只实例化Prefab并把adapter显式绑定给本地player host。Gameplay Lab Builder只实例化同一Prefab。AI和remote actor不创建Camera runtime或Camera容器。

`FindObjectOfType`、`Camera.main`、缺引用时创建默认Camera和旧Scene内FreeLook对象全部删除。

## 可抄与不可抄

### Philippe KCC ExampleCharacterCamera

本地参考：`ExternalDownloads/PhilippeKccReference/Samples/ExampleCharacter/Scripts/ExampleCharacterCamera.cs`。

可以借：

- yaw/pitch作为Camera自己的稳定状态。
- pitch clamp。
- `1 - exp(-sharpness * dt)`形式的帧率无关收敛。
- 遮挡进入和离开使用不同响应速度的思路。
- follow point与Camera orbit状态分离。

不直接搬：

- `UpdateWithInput`自主更新。
- 直接写Camera Transform。
- 自己做SphereCast obstruction。
- public字段直接作为正式配置。

这些实现会绕过PresentationFrame和Cinemachine，形成第二条运行链。

### Lyra CameraModeStack

可以借完整mode view、blend function、blend weight、activation/deactivation和reset interpolation语义。

不搬真正栈式业务入口、UE Actor/Component、GameplayTag和直接输出最终世界Camera Pose。项目已有多producer priority模型，应保留现有业务语言。

### Cinemachine 3rd Person Follow

可以直接使用其Gameplay Rig、shoulder、distance、vertical arm、damping和collision能力。它是正式Unity实现，不需要复制数学源码。

不让Cinemachine priority、Scene vcam名称或Timeline Cinemachine Track成为mode真相。

### RootMotion CameraController

只适合看最基础yaw/pitch、zoom和sphere cast流程，不作为正式参考实现。它依赖旧Input API、自主LateUpdate、直接Transform写入和公开MonoBehaviour字段，几乎每个边界都与当前项目冲突。

## Mode业务配置

### FreeLook

- 目标：跑图、观察环境、camera-relative移动。
- 完整手动orbit。
- 中距离、角色略偏画面中心下方。
- 可选移动中延迟recenter，但只由Profile声明，不从角色速度临时推断第二模式。

### Aim

- 目标：稳定瞄准和展示肩后构图。
- 更短distance、更明显shoulder、较低FOV、更窄pitch范围。
- Camera basis可以在Action激活时固化为射击/突进事实。
- Camera本身不直接改角色yaw；角色朝向继续由Gameplay Program决定。

### LockOn

- 目标：同时看清本地角色和正式目标。
- aim point来自正式Target Context。
- 玩家look只提供有限orbit bias或目标切换请求，不在Camera里搜索最近敌人。
- 目标丢失时该request失效并按blend out退出，不自动换目标。

### ActionFocus

- 目标：攻击、受击或特殊移动时短时间强调动作。
- 默认仍使用Gameplay Rig，只调整构图、FOV和look response。
- 不默认抢占target；需要目标时必须有显式CameraTargetRequest。

### SkillCloseup

- 目标：明确的技能镜头或处决镜头。
- 使用显式Shot Rig Slot。
- 默认Suppressed look，按Action/Timeline producer生命周期退出。
- 缺少Shot Slot时composition失败，不回退到FreeLook伪装成功。

## Runtime Order

```text
CaptureRenderFrame
  -> Look sample + Camera basis latch

PresentationFrame
  -> Body final visible frame
  -> Animation final publication
  -> Camera producer lifecycle
  -> State resolve
  -> Mode mix
  -> Target resolve
  -> Modifier resolve
  -> CameraRigPlan
  -> Cinemachine adapter Apply
  -> Manual Brain Update
  -> Camera basis/debug publication
```

Camera继续在完整Animation/Body表现事务成功后推进。`add-discrete-stair-presentation`形成的最终visible Y直接进入follow point，不增加Camera私有竖直滤波。

## Migration

1. 先建立新Profile、look sample、resolved mode和Rig Plan合同。
2. 将Presentation Runtime、Input Adapter和Factory依赖切换为Camera接口。
3. 实现唯一Cinemachine 3rd Person Follow adapter和共享Rig Prefab。
4. 迁移Standalone、ServerAuthoritative、DotRecast、Local Fixed、Rollback与Gameplay Lab引用。
5. 接入mode transition、modifier和debug。
6. 迁移Corin正式Camera profile与必要Camera Timeline内容。
7. 删除旧FreeLook对象、旧Controller、旧Builder创建代码、旧blend资产、硬编码参数和所有Scene搜索。

迁移期间不保留旧新adapter并行开关。每个产品Scene在同一个提交中切到新Prefab；缺失配置允许明确报错，不允许走回旧镜头。

## Risks

- 3rd Person Follow与现有FreeLook构图差异较大，Corin初始手感需要重新调参；通过唯一Profile解决，不保留旧FreeLook做对照运行。
- Pointer与Stick分离后旧灵敏度数值不能直接迁移，需要按角度语义重新标定。
- LockOn没有完整目标Registry，Camera mode实现可以完成，但正式内容只能使用现有显式Action Target或绑定Target。
- 多Scene迁移如果遗漏会直接报配置错误；这是为了暴露产品装配缺口，而不是回退搜索Camera。
