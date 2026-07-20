# Change: 增加预测式足部放置表现 Pass

## Why

当前角色表现链已经明确区分 Simulation Body、VisualRoot、动画采样和相机：

```text
Simulation Commit / Model Egress
  -> CharacterBodyPresentationRuntime
  -> CharacterAnimationPlaybackRuntime
  -> Animancer
  -> CharacterCameraPresentationRuntime
```

这条链能够在逻辑 Tick 之间连续采样动画，也能在网络分支替换时对 VisualRoot 做有界纠偏，但最终动画姿势仍完全假定地面平整。角色上楼梯、站在高低差边缘或经过不规则坡面时，脚会穿入表面、悬空或跟随骨盆一起跳动。直接给 prefab 挂一个自行 `LateUpdate` 的 Final IK Grounder 会形成第二个表现驱动时钟，并且无法消费现有 Body reset、动画 producer、网络纠偏和统一 diagnostics。

成熟动作游戏的公开方案也说明，高质量楼梯效果不是“脚下打一条 Ray”本身：

- Ubisoft 的预测式 biomechanical foot placement 会预测落脚位置、构造可达支撑路径、区分 Locked/Sliding/Unlocked，并以支撑腿驱动骨盆。
- Naughty Dog 公开的 Foot Plant IK 使用脚踝速度检测接触并锁定世界落点，证明接触判定可以作为动画后的独立程序化层，不依赖 Motion Matching。
- Unreal Foot Placement 将 Plant、Trace、Pelvis、Interpolation 与 Replant 参数分开配置，并区分摆动期采样和最终落脚约束。
- Final IK 已经提供成熟的 Limb IK、脚旋转、基础预测与 Grounding 工具，但其默认 Grounder 仍按自己的 Unity 组件生命周期运行，不能直接成为本项目的管线 owner。

因此本 change 不重写 IK 数学，也不让 Final IK 接管表现生命周期，而是在唯一 `CharacterSimulationPresentationRuntime` 中增加正式 Foot Placement Pass：项目代码负责接触、预测、地面路径、脚锁、骨盆和重置语义，Final IK adapter 只把最终计划解到 Corin 骨骼。

## What Changes

- 在 `CharacterSimulationPresentationRuntime` 中增加唯一 Pose Post Process 插槽，固定执行顺序为 `Body -> Animation/Animancer Evaluate -> Foot Placement -> Camera`。
- 新增 Presentation-owned `CharacterFootPlacementRuntime`，每个表现帧从最终动画骨骼姿势、同帧 `CharacterBodyPresentationFrame`、显式 Unity 场景查询和正式 Profile 生成双脚与骨盆计划。
- 以脚相对 VisualRoot 的速度、脚底高度、下降趋势、Body Grounded 与迟滞阈值判断接触，不新增 BTSMTL FootPhase、Timeline window、Blackboard variable 或第二份 locomotion gait phase 数据。
- 预测下一落脚点时同时使用可见 Body 速度、当前动画脚局部轨迹与有限 look-ahead；沿当前脚到预测落点的路径执行预分配 Sphere/Capsule 查询，过滤不可站立坡度、不可达高度、跨越台阶和角色自身 Collider，形成每只脚的连续支撑 envelope。
- 让每只脚只拥有 `Free`、`Locked`、`Sliding` 三种正式约束状态，并通过 plant/release 权重连续过渡。锁定位置保存为命中 Collider 的局部锚点，使移动平台上的脚随表面移动；超过 replant 距离、角度、腿长或表面失效时按明确原因释放或滑动。
- 根据当前支撑腿和两腿可达范围计算骨盆垂直偏移；使用临界阻尼、上下限和 ascent/descent 规则抑制楼梯上的身体跳动。Foot Placement 不旋转或移动 VisualRoot，不修改 Camera anchor 和 Gameplay Body。
- 新增 Presentation-owned `CharacterFootPlacementProfile`，只保存 PoseSourceLayerId、Trace、Contact、Prediction、Constraint、Pelvis、Foot Rotation 与 smoothing 等角色级算法参数；Profile 不保存按动画分派的策略表。
- 在 Timeline `AnimationClip` 中只保存一条归一化 `Foot Placement Weight` 曲线，表达该动画时间点允许Foot Placement整体介入多少；Prediction、Pelvis与Rotation继续由角色级Profile和运行时算法负责，不再成为逐Clip重复作者数据。每个AnimationTrack下显示默认折叠的`CURVES`分组，展开后显示一条按Clip帧范围对齐且可直接增删、拖动key的曲线行。曲线随动画片段保存并编译进Presentation Projection，不进入Semantic IR、Gameplay Program、State、Snapshot、Hash或网络协议。
- Animancer adapter 在完成本帧 Evaluate 后提供只读 visible playback contribution：producer identity、playback generation、sample time、cycle与实际state weight。Foot Placement以该正式visual sample time复用同一Projection clip binding采样单一Foot Placement Weight曲线，再按最终视觉权重混合，不读取逻辑priority，不参与动画选择或crossfade。
- 将 Timeline 动画曲线加入 BTSMTL Agent Snapshot、Patch、Validator 与正式事务写入口；Corin 迁移必须使用 stable timeline/track/clip identity，不直接编辑 Graph/Timeline YAML。
- 新增 vendor-neutral `ICharacterFootPlacementSolver` 合同。项目 planner 输出 FootPlacementPlan，Final IK adapter 消费计划并显式更新两个 Limb IK solver；Final IK 组件自主 `LateUpdate` 必须关闭，项目不修改 vendor 源码。
- 安装 Final IK 自带 `Import Assembly Definitions.unitypackage` 中的正式 `RootMotion.Runtime.asmdef` 与 `RootMotion.Editor.asmdef`，并建立独立 `ThirdPersonCharacter.Presentation.FinalIK` adapter 程序集。`ThirdPersonClient.Runtime` 只依赖 solver 合同，不直接引用 RootMotion 类型。
- Body `ResetSequence`、Committed branch replacement、Selected stream reset、Presentation reset、actor dispose、动画尚无正式输出和显式大姿态不连续都会清除旧 surface anchor、脚速历史和骨盆状态，避免网络纠偏后脚锁在旧世界位置。
- LocalOwner、SimulatedActor 与 ObservedActor 继续由同一个 Factory 和 Foot Placement Runtime处理；网络不发送脚目标、骨盆偏移或 IK 状态，Simulation、WorldSolver、Snapshot 与 StateHash 不读取该表现结果。
- 扩展统一 diagnostics 和 Profiler marker，显示每只脚的状态、接触速度、预测点、命中表面、锁定锚点、replant 原因、IK 权重、骨盆偏移、查询次数和 reset identity；不建立独立 IK 调试窗口。
- 为 Corin prefab 显式绑定 VisualRoot、pelvis、左右 hip/knee/ankle/toe、两个 Final IK Limb solver 与唯一 Foot Placement Profile，并配置现有 Ground layer。不得通过 humanoid bone API、名称、层级扫描或默认 LayerMask补全缺失引用。

## Capabilities

### New Capabilities

- `character-foot-placement-presentation`：定义预测式足部放置的表现帧输入、接触判断、支撑 envelope、约束状态、骨盆、solver adapter、重置、配置、角色类型和 diagnostics 合同。

### Modified Capabilities

- `character-animation-pipeline`：在 Animancer 完成最终 pose 后增加唯一 Pose Post Process 消费边界，同时保持 Program、Pipeline 与 Network 不引用 IK 或动画实现。
- `character-animation-presentation-authoring`：将现有“唯一 Presentation 配置入口”收窄为“唯一动画播放配置入口”，允许独立Foot Placement Profile Inspector只编辑pose后处理参数而不复制Layer、Transition或producer播放绑定。
- `character-pipeline-runtime`：将 PresentationFrame 的固定原子顺序扩展为动画生命周期提交、Animancer Evaluate、Pose Post Process、Camera 和 batch acknowledge，不允许 Final IK 自主更新形成第二条帧路径。

## Dependencies And Sequencing

- 依赖已经完成的 `refactor-character-presentation-runtime-modules` 与 `refactor-character-visual-trajectory-following`。实现必须复用现有 `Factory -> Body / Animation / Camera`、`CharacterBodyPresentationFrame.ResetSequence` 与统一 VisualRoot，不得恢复旧 `CharacterPresentationStage`。
- `refactor-deterministic-rollback-input-propagation` 可以并行实施。本 change 只消费已提交的 Body frame和 reset identity，不修改 Fixed Program、rollback input、history、replay、hash或协议。
- `add-corin-targeted-motion-warp-demo` 在 Gameplay 侧修改 target、Program Motion Modifier和 Host角色装配。本 change 不读取 MotionWarp target，也不在 Presentation 补偿 Gameplay 位移；二者只可能在 `CharacterPipelineHost` 参数装配处产生文件级冲突，实施时必须基于最新 Host 合并为一个 Factory 调用。
- `add-timeline-animation-marker-sync` 只负责AnimationTrack表现采样时间映射，不定义脚接触、脚锁或plant runtime真相。Foot Placement继续从最终pose与地面查询判断约束；双方可并行，但不得让Foot Placement消费Marker Sync作为contact输入，也不得复制marker作者数据。
- Final IK 源码已经位于 `Assets/Plugins/RootMotion`，但当前编译到 `Assembly-CSharp-firstpass`，`ThirdPersonClient.Runtime` 没有引用。必须先安装插件自带的正式 asmdef，再建立 adapter 程序集；不得把项目 adapter 写进 Plugins 或 Assembly-CSharp 旁路。
- 实施顺序必须先建立 vendor-neutral Profile、Plan、Query 与 solver 合同，再实现纯项目 Foot Placement Runtime，然后接 Final IK adapter，最后迁移 Corin prefab和全部 Factory调用点。不得先挂 Grounder 组件临时获得效果。

## Current Spec Comparison

- 现行 `character-animation-pipeline` 把正式动画消费链写到 `CharacterAnimationPlaybackRuntime -> AnimationPlaybackLifecycle -> Animancer` 为止，没有定义最终 pose 后处理。本 change 保留 Animancer 的 state、layer和fade权威，只在 Evaluate之后增加只改骨骼的 consumer。
- 现行 `character-pipeline-runtime` 要求 PresentationFrame 原子读取 queue、采样 producer、调用 Animancer、退休 outgoing并 acknowledge。本 change 会修改该顺序，使 Pose Post Process 在 Animancer Evaluate之后、Camera之前执行，并仍处于同一个 PresentationFrame transaction。
- 现行 `character-presentation-interpolation` 已明确 Body Runtime 是 VisualRoot、stream reset和visual correction的唯一 owner，并且 Presentation不能产生同步事实。本 change 只读取其 frame/reset identity，不写 VisualRoot或维护第二份 Body history，因此无需修改该 capability。
- 现行 `character-animation-presentation-authoring` 要求 `CharacterAnimationPresentationProfile` 只保存 Layer、TransitionLibrary和producer transition binding，Runtime不读取该 Profile。本 change 不扩张它；角色级Foot Placement算法仍由Unity表现装配显式引用，单一Foot Placement Weight曲线由Timeline独占并进入Presentation Projection，调曲线只重建表现投影而不重编Gameplay Program。
- 现行 `character-animation-presentation-authoring` 同时把其Inspector标题写成“唯一 Presentation 配置入口”，该表述会与Camera、Body Profile以及本change的Foot Placement Profile冲突。本 change将它精确收窄为唯一动画播放配置入口，并继续禁止第三个Animation Presentation窗口和Graph/Timeline配置副本。
- 现行 `character-pipeline-definition-authoring` 要求 Definition只保存已有正式config与generated artifact引用。本 change 不向 Definition 添加 IK字段，不把场景LayerMask、骨骼或Final IK组件放进Gameplay authoring root。
- 现行 `gameplay-tick-system` 已要求 PresentationFrame使用真实frame delta推进Visual interpolation、Animancer和Camera。本 change沿用同一delta推进接触、脚锁权重和骨盆阻尼，不增加MonoBehaviour自主时钟。
- `openspec/project.md` 当前仍将表现顺序写为 `Body -> Animation -> Camera`。实施完成时必须更新为 `Body -> Animation -> Pose Post Process -> Camera`，并补充Foot Placement完全属于表现层。

## Impact

- Presentation core：
  - `CharacterSimulationPresentationRuntime`
  - `CharacterPresentationRuntimeFactory`
  - `CharacterAnimationPlaybackRuntime`
  - `AnimancerPlaybackAdapter`
  - 新的Foot Placement Profile、Plan、Runtime、query、constraint与diagnostics类型
- Final IK adapter：
  - `Assets/Plugins/RootMotion/RootMotion.Runtime.asmdef`
  - `Assets/Plugins/RootMotion/Editor/RootMotion.Editor.asmdef`
  - 新的独立 `ThirdPersonCharacter.Presentation.FinalIK` 程序集
  - Corin两个显式Limb solver与rig adapter
- Unity装配：
  - `CharacterPipelineHost`
  - Local/Simulated/Observed Presentation创建入口
  - Corin prefab与新的CharacterFootPlacementProfile资产
- Editor与diagnostics：
  - 只编辑全局算法参数的Foot Placement Profile Inspector
  - Timeline Animation Clip单一Foot Placement Weight曲线
  - Agent Snapshot/Patch/Validator的Timeline曲线合同
  - Host配置校验
  - RuntimeDebugSession/Host Live Debug的Foot Placement只读投影
- 不影响：Semantic IR、Float32/Fixed Program ABI、CharacterState、WorldState、WorldSolver、Network Model、packet、Snapshot、StateHash、BTSMTL Graph、Timeline与Agent schema。

## Out Of Scope

- 不实现 Motion Matching、Stride Warping、Slope Warping、Hand IK、Aim IK、LookAt、weapon grip或全身受击IK。
- 不让 IK 改变 CharacterController、Deterministic KCC、DotRecast或任何Gameplay碰撞结果。
- 不根据脚底视觉位置生成Grounded、台阶、命中或移动事实。
- 不新增Timeline Foot Window、FootPhase Track、Blackboard foot变量、StateMachine条件或Gameplay Tag；Timeline只保存动画相对的连续影响曲线，不保存脚接触真相。
- 不在第一版实现Ubisoft完整三维凸包 Ground Envelope、攀爬、翻越、任意动态刚体接触或复杂四足支持多边形。
- 不使用 FullBodyBipedIK 改写上半身动作；Corin第一版使用两条Limb IK与项目拥有的pelvis offset，保持攻击和武器动画轮廓。
- 不让纯动画Timeline Preview伪造Body、地面或Scene Physics。Foot Placement只在Play Mode中具备显式Body、rig和PhysicsScene的正式Local/Simulated/Observed Presentation执行。
- 不新增测试或人工验证task，不运行Unity batchmode。

## Stop Conditions

- 如果 Final IK 官方asmdef导入后不能让独立adapter程序集稳定引用，停止并说明程序集tradeoff，不把adapter放入Assembly-CSharp或Plugins旁路。
- 如果 Corin骨架不能以显式hip/knee/ankle/toe引用建立两个Limb solver，停止并说明rig缺口，不用名称扫描或Humanoid fallback。
- 如果必须修改Gameplay Body、WorldSolver、MotionWarp、Timeline逻辑事实或网络数据才能让脚贴地，停止并说明业务tradeoff；修改Timeline Animation Clip的表现曲线属于本change正式作者入口。
- 如果 `add-timeline-animation-marker-sync` 在apply前被扩张为foot contact/plant权威，停止并先恢复“marker只选择表现采样时间”的边界，不双算。
- 如果 Final IK solver无法关闭自主Update/LateUpdate并由Presentation Pass精确单次驱动，停止，不接受双求解。
- 如果正式运行场景没有可查询的Ground surface或Collider层配置，停止说明资产缺口，不自动使用Default layer或无限Raycast。

## Success Criteria

- 正式表现链唯一为 `Body -> Animation/Animancer -> Foot Placement -> Camera`，不存在Final IK自主LateUpdate或第二个IK manager。
- LocalOwner、SimulatedActor与ObservedActor共用同一个Foot Placement Runtime和Profile语义。
- 每只脚只有Free、Locked、Sliding三种约束状态，移动平台锚点、replant、腿长限制和surface失效都有明确生命周期。
- 楼梯与坡面落脚使用预测点和连续support envelope，不退化为当前脚下一条Ray。
- 支撑腿与骨盆调整不会写VisualRoot，不会改变Gameplay Body、碰撞、Snapshot、Hash或网络输出。
- Timeline Animation Clip的单一Foot Placement Weight曲线只按stable clip identity进入Projection，Animancer再按片段内部权重与实际visible producer weight连续混合；没有Action名、State名、clip名、Tag或priority硬编码。
- Body reset、rollback branch replacement和selected stream reset会在同一PresentationFrame清除旧脚锁与骨盆历史。
- Final IK只负责消费FootPlacementPlan并解算骨骼；vendor源码不被修改，adapter位于独立命名程序集。
- Corin所有必需骨骼、profile、solver和Ground mask均为显式正式配置，缺失配置直接失败。
- diagnostics能够解释脚为什么锁定、滑动、释放、重落脚，以及本帧预测点、表面、权重和骨盆偏移。
