## ADDED Requirements

### Requirement: Camera Rig必须由唯一正式Profile与共享Prefab装配

系统 MUST使用唯一`ThirdPersonCameraPresentationProfile`声明look输入策略及`FreeLook`、`Aim`、`LockOn`、`ActionFocus`、`SkillCloseup`的完整mode profile。每个mode profile MUST具有稳定Rig Slot identity、FOV、pitch范围、shoulder、distance、vertical arm、composition、damping、collision、response和transition参数。系统 MUST使用唯一共享Player Camera Rig Prefab显式绑定Main Camera、Manual Cinemachine Brain、Rotation Pivot、Gameplay Rig、Follow/Aim Point和Shot Rig Slot。Profile MUST不保存Scene Transform或Cinemachine组件引用，Prefab MUST不复制第二份mode参数表。缺少Profile、mode、Rig Slot或组件绑定 MUST在composition明确失败，不得搜索、创建或选择默认Camera。

#### Scenario: 多产品装配本地玩家Camera

- **WHEN** Standalone、Local Fixed、Rollback或ServerAuthoritative Client装配本地玩家
- **THEN** 它们 MUST实例化或引用同一个正式Player Camera Rig Prefab
- **AND** 它们 MUST消费同一个正式Camera Profile
- **AND** Network Model MUST不复制Camera配置或创建自己的Camera实现

#### Scenario: AI与远端Actor装配

- **WHEN** Actor不是本地Camera owner
- **THEN** Factory MUST不为该Actor创建Camera Runtime、Rig容器或Profile实例
- **AND** 该Actor MUST继续使用自己的正式Body Presentation策略

#### Scenario: Camera Rig缺少Shot Slot

- **WHEN** Projection包含`SkillCloseup`请求但共享Prefab没有对应Rig Slot
- **THEN** composition MUST报告具体Profile、mode和Rig Slot identity
- **AND** runtime MUST不回退到Gameplay Rig或任意Scene virtual camera

### Requirement: Look输入必须区分Pointer Delta与Rate语义

系统 MUST在一次RenderFrame采集中生成typed `CameraLookInputSample`并锁存同一帧`CameraBasisSnapshot`。Pointer Delta MUST表达本RenderFrame指针位移且不乘presentation delta；Gamepad或Joystick Rate MUST表达归一化持续输入并按Profile角速度、dead zone、response curve和presentation delta换算。Camera Response Policy MUST只修改Camera消费权，不得停止Input Adapter采集。Float32、Fixed与Rollback本地玩家的camera-relative movement MUST只读取同一次锁存的Camera basis，不得在后续Simulation Tick读取live Camera state。

#### Scenario: 鼠标与手柄产生相同转向意图

- **WHEN** Pointer在一帧产生delta且Gamepad在另一帧维持right stick输入
- **THEN** Pointer MUST按degrees-per-pixel处理
- **AND** Gamepad MUST按degrees-per-second乘presentation delta处理
- **AND** 两者 MUST不共享一个无单位倍率

#### Scenario: 同一RenderFrame构造camera-relative移动

- **WHEN** Input Adapter锁存Move、Look和Camera basis后构造一个或多个Simulation Tick输入
- **THEN** 所有camera-relative Move转换 MUST使用该RenderFrame锁存basis
- **AND** 后续Camera Presentation变化 MUST不改变已经锁存的移动方向

## MODIFIED Requirements

### Requirement: CharacterSimulationPresentationRuntime 必须是相机 runtime 唯一边界

系统 MUST使用`CharacterSimulationPresentationRuntime`作为角色相机runtime的唯一公开编排边界。内部`CharacterCameraPresentationRuntime` MUST唯一拥有Camera State/Response/Target/Cue lifecycle、resolver、mode mixer、look input处理、bind offset、modifier和`ICameraRigAdapter`调用。Camera Runtime、Input Adapter和Host MUST只依赖`ICameraRigAdapter`、`ICameraMovementBasisProvider`及typed look input合同，不得依赖具体Cinemachine MonoBehaviour类型。协调器 MUST在`PresentationFrame`中使用同一`CharacterBodyPresentationFrame`最终visible pose推进Animation与Camera，并在完整Camera Rig Plan成功后调用adapter。系统 MUST不保留Camera MonoBehaviour自主Update/LateUpdate、外部resolver调用、Scene Camera搜索或无相机Actor分配Camera容器的路径。

#### Scenario: Local Owner推进相机

- **WHEN** `CharacterPresentationFrameTarget`调用唯一`ICharacterPresentationRuntime.Present`
- **THEN** 协调器 MUST先取得本帧唯一最终Body visible pose
- **AND** 内部Camera Runtime MUST消费已提交camera command、typed look sample、target binding和Profile生成`CameraRigPlan`
- **AND** 唯一adapter MUST应用该plan并发布新的Camera basis

#### Scenario: 禁止具体实现跨模块传播

- **WHEN** Input Adapter、Factory或Host装配Camera capability
- **THEN** 它们 MUST通过Camera合同传递rig apply与basis读取能力
- **AND** 它们 MUST不把`CinemachineFreeLook`、`CinemachineVirtualCamera`或具体Camera Controller作为portable runtime依赖

#### Scenario: 无Camera组合收到Camera命令

- **WHEN** observed或simulated无Camera Actor收到Camera PresentationCommand
- **THEN** 唯一协调器 MUST报告明确配置错误
- **AND** MUST不搜索Scene Camera、创建默认Rig或忽略该命令

### Requirement: CameraStateResolver 必须使用有限状态仲裁并产生真实模式混合

系统 MUST使用有限camera mode和稳定仲裁规则决定目标相机状态。mode MUST覆盖`FreeLook`、`Aim`、`LockOn`、`ActionFocus`和`SkillCloseup`。Resolver MUST按priority、weight、source identity、producer generation和action lifecycle稳定选择目标state，并实现BlendOut、Cut和HoldUntilSourceEnds interrupt policy。独立`CameraModeMixer` MUST维护来源mode、目标mode、blend duration、blend function和progress，并把FOV、pitch、shoulder、distance、vertical arm、composition、damping与look response混合成实际`CameraRigPlan`。系统 MUST NOT只计算debug progress而让最终Rig保持不变，也 MUST NOT使用Dictionary遍历顺序、Scene object priority或Cinemachine内部active state作为业务真相。

#### Scenario: Aim覆盖FreeLook

- **WHEN** Aim request以BlendIn进入且FreeLook是当前base mode
- **THEN** Mode Mixer MUST在正式duration内从FreeLook profile过渡到Aim profile
- **AND** FOV、shoulder、distance、composition与response MUST随同一transition推进

#### Scenario: SkillCloseup Cut打断Aim

- **WHEN** 更高优先级SkillCloseup request使用Cut policy进入
- **THEN** Resolver MUST立即结束当前连续blend并切换目标Shot Rig Slot
- **AND** debug MUST记录被打断来源、获胜request和cut原因

#### Scenario: 同优先级请求稳定仲裁

- **WHEN** 两个active request具有相同priority和weight
- **THEN** Resolver MUST使用稳定source identity与producer generation决定唯一结果
- **AND** 多次运行 MUST不因容器枚举顺序改变winner

### Requirement: 相机响应策略必须和输入采集分离

系统 MUST将输入采集、输入解释和相机响应分离。Input Adapter MUST始终采集typed look sample；Camera Runtime MUST先按Profile把Pointer Delta或Rate转换为yaw/pitch意图，再根据`CameraResponsePolicy`决定消费权。系统 MUST使用`Full`、`Suppressed`、`Weighted`或等价有限响应模式，且过渡期间response weight MUST由同一个Mode Mixer连续混合。Camera response MUST不修改Input Action enable状态、输入历史或已锁存sample。

#### Scenario: 技能特写不响应look

- **WHEN** 当前mode为`SkillCloseup`且response为`Suppressed`
- **THEN** Input Adapter MUST仍然采集Pointer或Stick sample
- **AND** Camera Runtime MUST不把该sample应用到yaw/pitch
- **AND** sample MUST继续可用于输入history或其它正式消费者

#### Scenario: 从FreeLook混合到Aim

- **WHEN** FreeLook的Full response正在过渡到Aim的Weighted response
- **THEN** Mode Mixer MUST连续混合manual orbit、yaw和pitch response权重
- **AND** Camera不得在transition handoff帧突然丢失或放大look输入

### Requirement: Camera modifier 必须按有限顺序产生可消费输出

系统 MUST将Recoil、FOV Kick、Shake、Collision Correction和已注册Custom效果作为有限typed modifier进行生命周期和顺序裁决。Modifier MUST在Mode Mixer产生基础Rig参数后按固定顺序输出到`CameraRigPlan`。Recoil MUST输出有界yaw/pitch impulse；FOV Kick MUST输出lens offset；Shake MUST输出Cinemachine Impulse或Noise可映射的typed request；Collision Correction MUST输出collision response policy。Presentation Runtime MUST不执行Camera obstruction Physics查询，也 MUST不允许未知Custom cue无效果通过。Adapter MUST不持有第二modifier stack或重新决定cue lifecycle。

#### Scenario: 命中帧震屏与后坐同时发生

- **WHEN** 同一帧存在Shake与Recoil cue
- **THEN** Modifier Resolver MUST按固定顺序生成两个typed channel
- **AND** adapter MUST将它们映射到正式Rig能力
- **AND** debug MUST分别显示来源、强度和剩余生命周期

#### Scenario: 未注册Custom cue

- **WHEN** Camera producer引用未知Custom modifier identity
- **THEN** Projection build或composition MUST明确失败
- **AND** runtime MUST不把该cue当成成功但无表现

### Requirement: Cinemachine 必须是 CameraRigAdapter 实现细节

系统 MUST通过唯一`CinemachineThirdPersonCameraRigAdapter`将`CameraRigPlan`应用到Unity相机系统。正式Gameplay Rig MUST使用显式Rotation Pivot和`CinemachineVirtualCamera + Cinemachine3rdPersonFollow`；FreeLook、Aim、LockOn和ActionFocus MUST共用该Gameplay Rig并消费混合后的typed参数。SkillCloseup MAY使用显式Shot Rig Slot。Adapter MUST负责把shoulder、vertical arm、distance、composition、damping、collision、FOV、look yaw/pitch和modifier映射到Cinemachine，并执行唯一Manual Brain Update。Adapter MUST NOT拥有mode resolver、request lifecycle、target search、Scene priority业务判断或Profile fallback。旧`CinemachineFreeLook`正式路径 MUST被删除。

#### Scenario: FreeLook输出到Gameplay Rig

- **WHEN** `CameraRigPlan`表达FreeLook构图和处理后的yaw/pitch
- **THEN** adapter MUST旋转独立Rotation Pivot并更新3rd Person Follow参数
- **AND** Cinemachine MUST负责最终Camera位置、旋转、damping与collision
- **AND** 角色Transform MUST不作为Camera orbit状态被写入

#### Scenario: Aim使用肩后构图

- **WHEN** `CameraRigPlan`从FreeLook混合到Aim
- **THEN** adapter MUST应用混合后的shoulder、distance、vertical arm、composition与FOV
- **AND** MUST不激活第二个自主Aim Camera Controller

#### Scenario: SkillCloseup使用Shot Rig

- **WHEN** `CameraRigPlan`选择有效SkillCloseup Rig Slot
- **THEN** adapter MUST按正式transition激活该Slot
- **AND** Shot Rig priority和生命周期 MUST完全由plan控制

### Requirement: Camera debug 必须解释请求、混合、输入和Rig输出

系统 MUST将`CameraDebugSnapshot`接入唯一structured presentation diagnostics。Debug MUST至少包含active requests、winner、来源mode、目标mode、source identity、producer generation、action instance、priority、weight、interrupt policy、blend function、duration、progress、look source kind、raw sample、processed yaw/pitch、response、target来源、active modifier、Camera basis和最终`CameraRigPlan`。Debug MUST从成功应用后的正式Camera状态发布，不得遍历Cinemachine对象或重新推导第二份结果。

#### Scenario: 排查Aim没有改变构图

- **WHEN** Aim request已经获胜但画面仍像FreeLook
- **THEN** debug MUST同时显示Aim mode profile、transition progress和最终shoulder、distance、composition与FOV
- **AND** 开发者 MUST能区分Resolver、Mode Mixer与Adapter映射哪一段失败

#### Scenario: 排查鼠标与手柄速度差异

- **WHEN** 两类设备产生look输入
- **THEN** debug MUST显示Pointer Delta或Rate来源
- **AND** MUST显示raw sample、Profile换算结果与response后的最终yaw/pitch delta

### Requirement: 默认相机跟随必须使用统一最终表现根姿态

系统 MUST让默认Camera follow基于`CharacterBodyPresentationRuntime`产生的最终`CharacterBodyPresentationFrame.VisiblePosition`与`VisibleRotation`计算。Camera Profile、Mode Mixer、3rd Person Follow和collision MUST只在该最终follow point之后工作。接地竖直不连续修正 MUST先在Body Runtime形成最终visible Y，再沿同一个Body Frame同时驱动VisualRoot与Camera。Camera Runtime MUST不读取logic Body、KCC Step diagnostics、camera anchor离散世界坐标或维护第二份插值、竖直filter和step correction。

#### Scenario: 离散台阶产生最终visible Y

- **WHEN** Body Runtime正在收敛接地竖直不连续offset
- **THEN** VisualRoot与Camera默认follow point MUST使用同一个最终visible Y
- **AND** 3rd Person Follow collision MUST只在该follow point基础上求解最终Camera位置
- **AND** Camera Runtime MUST不从logic Body Y建立第二条台阶修正

#### Scenario: Body tracking reset

- **WHEN** Body Frame的ResetSequence变化
- **THEN** Camera Mode Mixer与adapter MUST一次性重置旧tracking interpolation
- **AND** 下一帧 MUST继续使用当前正式mode和最终visible Body pose推进
