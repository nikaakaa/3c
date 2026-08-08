# Change: 重构角色摄像机模式与Rig控制

## Why

当前项目已经有完整的Camera authoring到PresentationCommand边界，也已经把Camera保持为local-only表现域；问题不在“有没有Camera管线”，而在中间的控制语义和底层Rig没有真正闭合。

现有实现只有一个`CinemachineFreeLook`。`FreeLook`、`Aim`、`LockOn`、`ActionFocus`和`SkillCloseup`虽然可以被Resolver选中，但最终只改变硬编码FOV、Follow/Aim世界点和同一组FreeLook轴值。`blendInSeconds`只形成一个debug progress，`blendOutSeconds`和`CameraInterruptPolicy`没有参与最终镜头混合；Shake、Recoil、CollisionCorrection和Custom cue进入Modifier后也没有形成adapter可消费的输出。业务上看起来有五种模式，实际仍是一台镜头。

相机调参目前分散在Scene内的FreeLook、`ThirdPersonCameraController`硬编码灵敏度、Runtime硬编码FOV、Gameplay Lab Builder硬编码Orbit/Collider参数，以及没有绑定到Brain的`ThirdPersonCameraBlends.asset`。相同玩家相机在Standalone、ServerAuthoritative、DotRecast和Gameplay Lab分别装配，作者无法确认哪个位置才是正式真相。

当前代码还存在几处与current spec直接不一致的实现：Presentation Runtime和输入适配器依赖具体`ThirdPersonCameraController`而不是Camera合同；DeterministicRollback Host存在`FindObjectOfType`补齐Camera的场景搜索；Float32相机相对移动读取live Camera basis而不是同一RenderFrame锁存值；`CameraDebugSnapshot`定义后没有进入正式发布链。

本change不重做Camera业务入口，而是把现有管线从“请求能到达”收口为“模式真的改变构图、输入、过渡和反馈”的可展示第三人称动作相机。

## What Changes

- 保留唯一业务链：

```text
BTSMTL / Timeline / Action提交Camera request与cue
  -> CharacterCameraPresentationRuntime
  -> CameraStateResolver / CameraModeMixer / CameraModifierResolver
  -> CameraRigPlan
  -> ICameraRigAdapter
  -> Cinemachine
```

- 新增唯一`ThirdPersonCameraPresentationProfile`正式资产：
  - 显式声明Pointer与Stick两类look输入语义、灵敏度、反转、dead zone、俯仰范围和recenter策略。
  - 为`FreeLook`、`Aim`、`LockOn`、`ActionFocus`和`SkillCloseup`声明完整typed mode profile。
  - 每个mode profile保存稳定Rig Slot、FOV、shoulder、distance、vertical arm、screen composition、damping、collision、look response和默认transition参数。
  - Profile不保存Scene Transform、Cinemachine组件引用、运行时fallback或第二套priority状态机。
- 将`CameraPosePlan`迁移为完整`CameraRigPlan`：
  - 保存当前/来源mode、mode profile identity、follow/aim、yaw/pitch、FOV、shoulder、distance、composition、damping、collision policy、look response、transition和有限modifier输出。
  - Presentation层只裁决业务和表现参数，不计算最终Unity Camera世界位置、旋转或Physics obstruction结果。
- 将`CameraStateResolver`从单赢家加debug progress改为正式mode mixer：
  - 使用稳定priority、weight、source identity和producer generation仲裁。
  - 实际执行blend in、blend out、cut和hold-until-source-ends。
  - 过渡期间同时保存来源与目标mode，并混合所有允许连续过渡的Rig参数。
  - Target、离散Rig Slot和不可混合policy在明确handoff点切换，不依赖Dictionary遍历顺序或Cinemachine scene priority自行决定。
- 重做look输入合同：
  - Input Adapter在一次RenderFrame同时锁存look sample和Camera basis。
  - Pointer delta按每RenderFrame位移处理；Stick按每秒角速度乘presentation delta处理，二者不再共用同一个无单位Vector2倍率。
  - Camera response policy只改变本地Camera消费权，不停止输入采集。
  - Float32、Fixed和Rollback本地玩家的camera-relative move都只使用同一次锁存的basis。
- 选择`CinemachineVirtualCamera + Cinemachine3rdPersonFollow`作为唯一Gameplay Rig实现：
  - 使用独立旋转pivot承载yaw/pitch，角色Transform不成为Camera orbit状态。
  - FreeLook、Aim、LockOn和ActionFocus共用同一Gameplay Rig并消费最终混合后的typed参数。
  - SkillCloseup使用显式绑定的Shot Rig Slot；是否进入、何时退出和如何混合仍由`CameraRigPlan`控制。
  - Cinemachine负责最终camera pose、damping、collision和Brain输出，不拥有mode resolver、request lifecycle或target search。
- 建立唯一共享Player Camera Rig Prefab，由Standalone、ServerAuthoritative、DotRecast、Local Fixed和Rollback产品显式引用；Gameplay Lab Builder只装配该正式Prefab，不再生成第二套FreeLook参数。
- 让Shake、FovKick、Recoil和CollisionCorrection全部形成typed modifier plan：
  - FOVKick修改lens channel。
  - Recoil修改有界yaw/pitch impulse channel。
  - Shake输出Cinemachine Impulse/Noise请求。
  - CollisionCorrection只表达collision response policy，不在Presentation层做Physics查询。
  - 未知Custom cue在Projection build或composition明确失败，不再运行时无效果通过。
- 将`CameraDebugSnapshot`接入唯一structured presentation diagnostics，发布mode stack、winner、来源、transition、look sample、response、target、modifier、basis和最终Rig Plan。
- 激进清理旧链：
  - 删除`ThirdPersonCameraController`具体类型在Runtime/输入/Host合同中的传播，Unity装配只绑定`ICameraRigAdapter`与`ICameraMovementBasisProvider`实现。
  - 删除旧FreeLook Scene对象、Gameplay Lab程序化FreeLook创建、硬编码FOV/灵敏度、未使用的`ThirdPersonCameraBlends.asset`和Camera场景搜索。
  - 不保留FreeLook兼容adapter、双Rig更新、默认Camera或缺配置后自动补齐。

## Impact

- 修改capability：`character-camera-pipeline`。
- 影响Camera runtime model、Resolver、Modifier、Presentation Runtime、Unity Input Adapter、Fixed Input Adapter、Host/Factory装配、Cinemachine adapter、Gameplay Lab Builder、Camera共享Prefab与产品Scene引用。
- 修改`CameraPosePlan`及`ThirdPersonCameraController`相关公开合同与序列化配置，是破坏性迁移。
- 不修改CharacterSimulationState、WorldSimulationState、Gameplay Program数值ABI、Rollback snapshot、Network payload或WorldSolver。
- 不新增完整目标搜索/切换系统。`LockOn`只消费正式Target Context或Camera Target Binding；目标Registry和战斗命中closure仍由独立change负责。
- 不自动构建Character Program、Presentation Projection、Prefab或Scene；所有重操作继续由显式命令触发。

## 与Current Spec及Active Change对比

- current `character-camera-pipeline`已经规定五种有限mode、请求仲裁、响应策略、modifier、debug和`ICameraRigAdapter`边界；本change不新增第二套Camera模型，而是把这些要求从名义字段补成实际Rig行为。
- current spec禁止Presentation resolver计算orbit radius、shoulder offset或collision distance，导致mode-specific构图只能被藏进Cinemachine adapter。这样adapter会重新拥有mode业务判断。本change有意修改该边界：Presentation允许输出typed Rig参数，仍禁止计算最终Camera世界Pose和执行Physics obstruction。
- current spec允许adapter使用FreeLook或专用virtual camera。本change收敛为唯一Gameplay `3rd Person Follow` Rig加显式Skill Shot Rig，不再保留FreeLook实现分支。
- current spec要求同一RenderFrame锁存Camera basis。Fixed Adapter符合该要求，Float32 Adapter仍读取live basis；本change统一修正并删除差异。
- current spec要求Camera debug可解释状态和输出，现有`CameraDebugSnapshot`没有正式消费者；本change将其并入structured presentation diagnostics。
- active `add-discrete-stair-presentation`正在修改最终visible Body Y与Camera follow共享关系。本change必须消费其最终`CharacterBodyPresentationFrame.VisiblePosition`，不得读取KCC Step diagnostics、logic Body Y或建立第二个Camera竖直filter。
- 当前没有active change拥有Camera mode、Rig Prefab、look输入或Cinemachine adapter。本change不与现有Animation、IK、Motion Matching和KCC实现争夺同一代码owner。

## Hard Stop Gates

1. Gameplay Rig必须能由单一显式rotation pivot驱动`Cinemachine3rdPersonFollow`，并在Manual Brain Update后提供稳定Camera basis；如果需要角色Transform承担yaw/pitch，停止实施。
2. FreeLook、Aim、LockOn和ActionFocus必须能通过同一个typed Rig Plan表达，不得为某个mode新增自主MonoBehaviour或第二resolver。
3. Skill Shot Rig必须由显式Rig Slot绑定并由同一transition plan控制；如果只能依赖Scene priority脚本或Timeline直控Cinemachine，停止实施。
4. Pointer delta与Stick rate必须能在RenderFrame采集时被明确区分；不得继续用一个无单位灵敏度凑两类设备。
5. 所有产品Scene和Gameplay Lab必须能迁移到同一共享Camera Rig Prefab；缺失绑定直接失败，不保留Scene搜索或旧FreeLook fallback。
