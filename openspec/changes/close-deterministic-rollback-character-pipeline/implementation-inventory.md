# 实施对账

## 2026-08-03 KCC与Collision Artifact当前状态

- 当前源码已经安装Motor `fixed-philippe-kcc-motor/6`、Solver version `8`、KCC identity schema `deterministic-kcc/6`与state codec v3；旧outer/inner capsule Step Solver、独立Step Down和微距Ground Snap已经删除。Gameplay Lab作者场景已分离LowStairs与GentleRamp路线，并补齐独立0.40m超限楼梯；唯一Collision Artifact已显式重新Bake，正式`CollisionWorldHash`为`02512d39104d34b650a5667c276cbc46ce5f6a7e77383f758b2862ff27a66ff5`，资产文件SHA-256为`666793c82954d1e929bfa0986a29695d3c9624b1f476a5e3fde3cd20e1c342fa`。下一份产品KccId必须由Motor `/6`、该CollisionWorldHash、正式配置与tick rate共同派生。
- 下文BuildId `20260801-133626`与KccId `78a2e8538aa8e5a114292a56fc3a1b9796176af523ee095f1537a4a8d918eba7`只记录上一次旧算法产品，不再代表当前源码闭包。
- 旧产品已移出`Build/Network/DeterministicRollback`正式路径，隔离到`Build/.Workspace/RejectedProducts/DeterministicRollback-20260801-133626-old-kcc`，正式Run不会继续启动旧KCC。
- 本change中受KCC与Collision identity变化影响的Prepare、Build、manifest与Run任务保持重新打开；顺序固定为Local Fixed验证、再发布并验证Relay与Peer A/B，新产品只能由作者在Unity Editor中显式触发，不自动构建。

## 输入

- Character Definition：`Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset`
- Definition GUID：`c7a7c1e3f7e64d81b5a04a90cbeb8d4e`
- ProgramId：`character:c7a7c1e3f7e64d81b5a04a90cbeb8d4e`
- SourceRevision：`ed4eb13950a91eb0861b643fb0475aa03cf9df2ae751c2302e8eee2d1ba71b3a`
- SemanticHash：`c9aad75ce4fa7a113260da33b4bc16757d38e95e3b8dff7e5e46a18355b21324`
- Fixed Program：`Assets/Configs/Simulation/DeterministicRollback/Programs/CorinFixedProgram.asset`
- Fixed ProgramHash：`fdb5cdbf9ce175dc55588fff903068611d620a33ca983a135637e5d5dab4866d`
- Fixed LayoutHash：`deafee76fa2e27be5386a6e271e6e36ea4a1b9efc6b76f4bb6acc570ec1051c7`
- Presentation Projection：`Assets/Configs/Character/Corin/Pipeline/Definition/Generated/CorinCharacterPipelineDefinition.PresentationProjection.asset`
- ContractHash：`1fbdc21bacec544e9b7e9ea6b6ee7d7f1d53895cb231cff80549b11ac0acce73`
- ProjectionRevision：`46be7b6a85b7a2571655a7ff318b758c28a203613da53fc2359deffb339d8652`
- KCC：`Assets/Configs/Simulation/DeterministicRollback/World/CorinDeterministicKcc.asset`
- 上一失效Product KccId：`thirdperson.simulation.solver.deterministic-kcc@78a2e8538aa8e5a114292a56fc3a1b9796176af523ee095f1537a4a8d918eba7`
- Collision Artifact：`Assets/Configs/Simulation/DeterministicRollback/World/CorinDeterministicCollisionWorld.asset`
- CollisionWorldHash：`02512d39104d34b650a5667c276cbc46ce5f6a7e77383f758b2862ff27a66ff5`
- Collision Artifact文件SHA-256：`666793c82954d1e929bfa0986a29695d3c9624b1f476a5e3fde3cd20e1c342fa`

## 处理

1. `GameplayLabAssetBuilder`只读取上述已发布产物，拒绝过期Program、Projection以及分裂的KCC与Collision引用，不再调用Character Build或Collision Bake。
2. `GameplayLabSessionVariantDefinition`显式保存Definition GUID、Composition、Fixed Program、Projection、World Solver与Collision Artifact引用。
3. `GameplayLabLocalFixedVariant.asset`与`GameplayLabDeterministicRollbackVariant.asset`共享全部Character、Presentation、KCC与Collision身份，只分别装配Local Fixed Source/Pipeline与Rollback Source/Pipeline。
4. Rollback Composition沿`Composition -> Session Source -> Fixed Program / Pipeline / Solver / Endpoint`形成唯一闭包；Local Fixed与Rollback Composition共享WorldId、MapId、WorldRevision、Program Runtime、Execution Backend与World Solver。
5. `DeterministicRollbackNetworkTestProductAdapter`只读取两个精确Variant和精确Definition路径，在Player Build前校验全部身份，不执行Build、Bake、迁移或资产修复。
6. 唯一`NetworkTestProductBuildWorkflow`使用IL2CPP构建Player，发布纯.NET Relay Server，写入完整身份字段，校验exact file closure后原子替换正式产品。
7. `GameplayLabAssetBuilder`只在`Compositions/Pipelines/Sources/Variants`聚合目录创建和更新资产；Local Fixed Prefab与Gameplay Lab场景引用这些正式GUID，根目录不再保存同名副本。
8. 当前MovingTurn Document事务发布产品后重新执行正式Prepare、IL2CPP Build与Run；产品清单直接锁定当前Fixed Program与Projection，不复用旧Build清单。

## 输出

- Local Fixed Variant：`Assets/Configs/Simulation/GameplayLab/Variants/GameplayLabLocalFixedVariant.asset`
- Rollback Variant：`Assets/Configs/Simulation/GameplayLab/Variants/GameplayLabDeterministicRollbackVariant.asset`
- Local Fixed Composition：`Assets/Configs/Simulation/GameplayLab/Compositions/CorinGameplayLabFixedComposition.asset`
- Rollback Composition：`Assets/Configs/Simulation/DeterministicRollback/Compositions/CorinRollbackComposition.asset`
- Rollback Session Source：`Assets/Configs/Simulation/DeterministicRollback/Networking/CorinRollbackSessionSource.asset`
- Gameplay Lab场景：`Assets/Scenes/GameplayLab/GameplayLab.unity`
- 上一失效产品原路径：`Build/Network/DeterministicRollback`
- 上一失效主清单：`Build/Network/DeterministicRollback/NetworkTestProduct.json`
- 上一失效Relay清单：`Build/Network/DeterministicRollback/Server/DeterministicRollbackServerManifest.json`
- 上一失效BuildId：`20260801-133626`
- Artifact角色：`deterministic-relay-server`、`unity-client-player`
- 文件闭包：91项，缺失0项，多余0项，哈希不一致0项
- 主清单SHA-256：`6febd7b84e1f3a95055d37ddc97d56911393286e5847c5117e50e016279fa692`
- Relay清单SHA-256：`898cd1d59ac00a3732933f961bd8065518e7c7f5913d0107b356b63cdd0c6cb0`

同一个Unity Player通过启动参数分别成为Peer A与Peer B；产品中不存在Host Player角色。Relay roster固定为`rollback-peer-a/rollback-player-a/rollback-actor-a`与`rollback-peer-b/rollback-player-b/rollback-actor-b`。

Local Fixed双Actor既有性能基准中，`ThirdPerson.Presentation.Animation`由约`14.8 ms`降至`6.2–6.5 ms`，其中Native Pose Graph求值由约`9.7 ms`降至`1.7–1.9 ms`。上一轮旧KCC产品的双端运行日志位于`Build/Network/RunLogs/DeterministicRollback/20260801-214029`：Relay推进到`canonical=4538`，Peer A/B前沿为`4539/4538`，全程`invalid=0`、`dropped=0`且五份日志中没有Exception、Error、mismatch或Presentation failure；该日志只保留历史证据，不证明当前Motor `/6`与新CollisionWorldHash的产品闭包。

## 删除的旧路径

- 删除旧`GameplayLabLocalFloat32Variant.asset`及其`.meta`。
- 删除旧`GameplayLabLocalFloat32.prefab`及其`.meta`。
- 删除旧`DeterministicRollbackNetworkTestBootstrap.cs`及其`.meta`。
- 删除`Assets/Configs/Simulation/GameplayLab`根目录下同名的Local Fixed Composition、Local Fixed Pipeline、Local Fixed Source、Local Fixed Variant与Rollback Variant及其`.meta`；正式资产只保留在聚合子目录。
- 正式产品目录通过candidate校验与原子替换清除了旧Player、旧Server和stale manifest。
- 工作区不再存在Rollback专用Character Graph、第二Fixed Program、第二Projection、第二KCC配置或第二Collision Artifact装配。

## MovingTurn固定180°短Root Motion收口

### 输入

- 唯一Document v3：`AgentAuthoring/Documents/CharacterController/c7a7c1e3f7e64d81b5a04a90cbeb8d4e-001dd30a08d99da6.btsmtl`
- 正式Root Tree：`Assets/Configs/Character/Corin/Pipeline/Graphs/CorinPlayableRootTree.asset`
- MovingTurn Inline Timeline：`editable/timelines/8a6491b4-93fe-4002-a814-2ac6eb75e567-d13b8cdbe1c0/timeline.json`与同目录`curves.json`
- 当前Document基线SourceRevision：`bc2b4a28de68e42cb6e88abe8dc1d26e70c4ad30deb1f37b909707ffc8d0a974`
- 当前EditableHash：`ede2426df11bcbc13b984fcabc5ce801a56674e988fdb111b797ecebf9a1efe0`
- 当前ContextHash：`427afb467828c049a157c22f4a55b560cf0ba0343a6683c58cf09b8ab599fbb7`
- 当前DocumentHash：`069cf12c241a3766f5498d0d7ffc66fa1e39fadb4122d82d6ef4c3ea646d52fb`

### 处理

1. `RunLoop`只在存在移动输入、`MovingTurnAngleThreshold=135°`成立且Attack与Dodge Action Context均未激活时进入MovingTurn；Walk、Start、End与Idle不直接进入Gameplay Turn。RunEnd重新收到输入时先进入规范RunLoop，再由唯一门禁判断。
2. MovingTurn Graph只保留一个有限Timeline节点。MotionCurve Clip固定为60Hz的0–28帧，前25帧完成180° yaw，后3帧保持180°；X/Z和切线保持Root Motion Baker输出的Unity米制原值，29个贡献累计为`(-0.9001478, 0, 0.4623734)`，不按`0.01`二次缩放，也不按实际目标角缩放。
3. Timeline以`Local / Locomotion / Override / Priority 100 / ConsumeLowerChannels`提交Gameplay Motion，状态只在`state_root_completed`后释放。
4. Turn Pose Graph删除`RootOrientationWarp`和LocalYaw输入，只保留Sequence到Output；Presentation的RunStart、RunEnd可以在观察到已提交Turn事实时进入同一Turn Pose。RunStart、RunLoop、RunEnd进入Turn使用0.12秒Inertialization；Turn退出到RunLoop、WalkLoop或Idle使用0.30秒。Idle、WalkLoop与RunLoop的`AlwaysResetOnEntry=false`，有限状态保持true。
5. Document Store、strict codec、Snapshot exporter、Reconciler、planner、typed Mutation与handler共同保存Inline Timeline文件对、MotionCurve元数据和registered curve payload；同一事务内由`EnsureInlineTimeline`解析planned Timeline identity。
6. 唯一Document v3事务以document hash `4834eaa3a01940b0a3bc707a0a485b4a2544ed6fbc9f31704259d685c34bdc33`和plan hash `41672e29f45810275b344c537eec373c8c4598b240aff6a195532e427aa1f59e`原子提交Gameplay与Presentation，canonical reverse export后重新checkout；当前`.sync.json`记录的SourceRevision、EditableHash、ContextHash与DocumentHash和上方输入一致，状态为`Clean`。最终只读Validator的Graph编译与语义校验均通过。
7. Local Fixed与DeterministicRollback Host现在都注册同一`AnimationPresentationRuntimeTarget`并在释放时注销；这只补齐正式Pose运行诊断，不把Pose状态写入Rollback snapshot或网络协议。

### 输出

- Float32 ProgramHash：`c0555c1f0c859b037df320ec10a8dfd59c014be5b53bfe24dd7f49c6c4716012`
- Fixed ProgramHash：`fdb5cdbf9ce175dc55588fff903068611d620a33ca983a135637e5d5dab4866d`
- SourceRevision：`ed4eb13950a91eb0861b643fb0475aa03cf9df2ae751c2302e8eee2d1ba71b3a`
- SemanticHash：`c9aad75ce4fa7a113260da33b4bc16757d38e95e3b8dff7e5e46a18355b21324`
- ContractHash：`1fbdc21bacec544e9b7e9ea6b6ee7d7f1d53895cb231cff80549b11ac0acce73`
- ProjectionRevision：`46be7b6a85b7a2571655a7ff318b758c28a203613da53fc2359deffb339d8652`
- Projection中的`m_RootOrientationWarps`为空，所有Pose State的RootOrientationWarp index为`-1`。
- 三个相互隔离的Local Fixed真实Input System样本均记录`TurnEntries=1`、`TurnExits=1`；运行时读取到Turn进入0.12秒、退出0.30秒。
- Rollback产品BuildId：`20260801-133626`，manifest精确锁定上方当前Fixed Program、Projection、SourceRevision与SemanticHash，闭包为91项，角色只包含Relay与Unity Client。

### 删除的旧路径

- 删除`Assets/Configs/Character/Corin/Pipeline/Motion/Turning/CorinMovingTurnOrientationWarpCurve.asset`及其`.meta`。
- 删除MovingTurn Graph中的输入运动节点、MoveAxis投影与并行包装。
- 删除Pose Turn Graph中的RootOrientationWarp节点和LocalYaw属性；删除RunEnd私有Gameplay Turn条件，Presentation只保留消费已提交Turn事实所需的过渡。
- 删除将Root Motion Baker米制曲线再次乘`0.01`的错误资产数据。
- Rollback Product adapter删除旧扁平目录常量，只显式读取`Programs`、`Pipelines`、`World`、`Networking`与`Compositions`下的正式资产。
