## 1. 固定最终闭包输入

- [x] 1.0 确认`openspec/character-pipeline-serial-execution.md`阶段1–7已经完成，且本change只重连现有Rollback与KCC。
- [x] 1.1 记录Corin CharacterPipelineDefinition精确资产路径与identity。
- [x] 1.2 记录当前Semantic IR、Fixed Program与Projection identity。
- [x] 1.3 记录Gameplay Lab Local Fixed与Rollback Variant精确资产路径。
- [x] 1.4 记录两个Variant引用的Composition、Source、Program与Projection。
- [x] 1.5 记录唯一CorinDeterministicKcc资产路径与identity。
- [x] 1.6 记录唯一DeterministicCollisionWorldAuthoring与artifact identity。
- [x] 1.7 记录Rollback Product adapter、manifest与Run入口。
- [x] 1.8 建立旧Host、旧wrapper、重复world authoring与stale product删除清单。

## 2. 收口Character Fixed产品

- [x] 2.1 确认Corin Document v3与正式Unity authoring为Clean。
- [x] 2.2 确认Pose Graph只保存typed authoring数据。
- [x] 2.3 确认Pose IR已经进入正式Native Plan Build。
- [x] 2.4 用精确Definition执行显式Fixed Build Request。
- [x] 2.5 从同一次validated Semantic IR发布Fixed Program。
- [x] 2.6 从同一Presentation Semantic Contract发布target-neutral Projection。
- [x] 2.7 校验SourceRevision、SemanticHash、ContractHash与ordered producer contract。
- [x] 2.8 删除旧Fixed wrapper与旧Projection产物。

## 3. 统一Gameplay Lab Variant

- [x] 3.1 让Local Fixed Variant引用新Fixed Program与Projection。
- [x] 3.2 让Rollback Variant引用同一Fixed Program与Projection。
- [x] 3.3 让两个Variant引用同一CorinDeterministicKcc资产。
- [x] 3.4 让两个Variant引用同一collision artifact。
- [x] 3.5 保持两个Variant只在Session Source与Network Model装配上不同。
- [x] 3.6 删除Variant内重复Program、KCC或world配置。
- [x] 3.7 删除旧Rollback Host与Canonical Host引用。
- [x] 3.8 拒绝按selection、场景名或目录扫描补全Variant。
- [x] 3.9 让Local Fixed生成资产使用`Presentation VisualRoot -> AnimatorRoot`正式层级。
- [x] 3.10 让Local Fixed的本地与模拟Actor都保持Native Animation Job可执行。

## 4. 锁定现有KCC identity

- [x] 4.1 确认Rollback当前只装配唯一正式Deterministic KCC。
- [x] 4.2 让两个Variant引用当前同一KCC identity。
- [x] 4.3 更新collision与Composition闭包中的Solver identity。
- [x] 4.4 删除Variant与Product中的旧Motor/Solver identity引用。
- [x] 4.5 拒绝旧snapshot、replay与endpoint identity。

## 5. 收口Rollback Product Build

- [x] 5.1 让Rollback adapter只读取精确Rollback Variant。
- [x] 5.2 禁止Rollback adapter调用Character Build。
- [x] 5.3 禁止Rollback adapter调用Collision Bake。
- [x] 5.4 禁止Rollback adapter创建或修改Gameplay Lab资产。
- [x] 5.5 把SemanticHash、ProgramHash与ProjectionRevision写入candidate manifest。
- [x] 5.6 把CollisionWorldHash与KccId写入candidate manifest。
- [x] 5.7 校验Relay、Peer A与Peer B使用相同roster和全部identity。
- [x] 5.8 校验candidate exact file closure。
- [ ] 5.9 原子替换正式DeterministicRollback Product。
- [x] 5.10 删除stale manifest、旧Player与旧Server candidate。

## 6. 收口Run与诊断

- [x] 6.1 保持Run只读取正式manifest。
- [x] 6.2 保持Run不触发Build、Bake、迁移或资产修复。
- [x] 6.3 保持Run只启动Relay、Peer A与Peer B。
- [x] 6.4 拒绝Canonical Host Scene与Host Player role。
- [ ] 6.5 对账Peer握手使用相同Program、Projection、World与KCC identity。
- [x] 6.6 对账rollback、replay、world hash、actor hash与presentation replacement诊断入口。
- [ ] 6.6a 用更新后的Gameplay Lab闭包重新发布精确Rollback Product。
- [ ] 6.6b 对账Local Fixed双Actor持续消费同一Fixed Program与Projection。
- [ ] 6.6c 对账Relay与Peer A/B持续推进且无Presentation transaction failure。

## 7. 删除分裂路径并对账文档

- [x] 7.1 删除重复Fixed Program装配。
- [x] 7.2 删除重复Presentation Projection装配。
- [x] 7.3 删除重复KCC配置资产。
- [x] 7.4 删除重复Collision authoring与artifact引用。
- [x] 7.5 删除Rollback专用Character graph、节点或Projection。
- [x] 7.6 删除运行时fallback、自动修复与旧schema reader。
- [x] 7.7 更新openspec/project.md的Rollback闭包状态。
- [x] 7.8 对账deterministic-rollback-two-client-demo delta。
- [x] 7.9 对账gameplay-network-test-build-workflow delta。
- [x] 7.10 运行`openspec validate close-deterministic-rollback-character-pipeline --strict --no-interactive`。

## 8. 修复MovingTurn闭环回归

- [x] 8.1 确认Corin Locomotion StateMachine缺少`RunLoop -> MovingTurn`正式业务边。
- [x] 8.2 通过Document v3新增`RunLoop -> MovingTurn`的`move_has + turn_facing_angle`条件。
- [x] 8.3 为`MovingTurn -> WalkEnd`补齐`state_root_completed`门禁。
- [x] 8.4 从同一精确Definition重新发布Float32 Program、Presentation Projection与Fixed Program。
- [x] 8.5 确认正式Validator通过且Document重新回到Clean。

## 9. 收口MovingTurn原地转身语义

- [x] 9.1 对账`720°/s + 同一135°进入/退出阈值`导致MovingTurn约4 Tick提前释放的根因。
- [x] 9.2 将`moveSpeed`、`turnSpeedDegrees`、`cameraRelative`和`continuous`注册为唯一Capability typed payload。
- [x] 9.3 让Document v3导出、规划、Mutation和反向导出完整保存Locomotion输入运动参数。
- [x] 9.4 让Blackboard声明的typed默认值进入Document v3并由唯一Mutation落地。
- [x] 9.5 让`turn_facing_angle`显式引用Blackboard key，并让`negate`经共享条件构建器、Mutation与canonical reverse export稳定往返，删除硬编码阈值名称。
- [x] 9.6 新增`MovingTurnReleaseAngleThreshold=15°`并保留`MovingTurnAngleThreshold=135°`作为进入阈值。
- [x] 9.7 将MovingTurn配置为`moveSpeed=0`、`turnSpeedDegrees=360`、`cameraRelative=true`、`continuous=true`，让朝向逐Tick提交到明确释放条件。
- [x] 9.8 删除RunLoop进入时过早清除Run意图的节点，并用该意图互斥分流MovingTurn到WalkLoop或RunLoop。
- [x] 9.9 让停止输入只通过`move_stop + state_root_completed`进入WalkEnd。
- [x] 9.10 保持MovingTurn的X/Z不进入Gameplay Motion，LocalYaw只由Pose Graph RootOrientationWarp与Inertialization消费。
- [x] 9.11 通过Document v3执行dry-run、exact-hash apply和canonical reverse export。
- [x] 9.12 从同一精确Definition显式发布Float32 Program、Presentation Projection与Fixed Program。
- [x] 9.13 确认Document为Clean，正式Validator的Graph编译与语义校验通过。

## 10. 将MovingTurn替换为固定180°短Root Motion

- [x] 10.1 对账Gameplay输入转向、Pose RootOrientationWarp与源Root Motion同时影响转向的重复所有权。
- [x] 10.2 将MovingTurn入口收窄为唯一`RunLoop + move_has + turn_facing_angle`业务边，删除Walk、Start、End和Idle进入Turn的业务边；该阶段采用的150°门槛已由10.31正式替换为135°。
- [x] 10.3 让Document Store只接纳canonical `local:*` Inline Timeline的完整`timeline.json + curves.json`文件对，并把有效文件闭包纳入同一document hash与service-owned manifest。
- [x] 10.4 新增`EnsureInlineTimeline` typed Mutation，让同一事务中更早创建的TimelineNode输出唯一Timeline planned identity。
- [x] 10.5 新增MotionCurve Track、Clip与配置typed Mutation，严格保存`CurveId`、`CurveEndFrame`、Space、Channel、BlendMode、Priority和ConsumeLowerChannels。
- [x] 10.6 让Snapshot exporter、strict codec、Reconciler、planner、handler与canonical reverse export完整往返MotionCurve元数据和registered curve payload。
- [x] 10.7 同步Document技能合同与五个MCP生命周期工具说明，不新增第二入口、manifest手工编辑或两阶段资产迁移。
- [x] 10.8 删除MovingTurn Graph中的`locomotion-input-motion`、MoveAxis投影与并行包装，改为唯一有限Timeline节点。
- [x] 10.9 在60Hz Timeline中保留源曲线前28帧，并让前25帧完成固定180° yaw、后3帧保持180°收束。
- [x] 10.10 确认Root Motion Baker输出已经是Unity米制值，将同一源曲线0–28帧的X/Z与切线原值写入Document，删除错误的`0.01`二次缩放与任意目标角缩放。
- [x] 10.11 让MovingTurn只以`state_root_completed`释放；保持输入时按Run意图进入RunLoop或WalkLoop，停止输入时进入WalkEnd。
- [x] 10.12 从Turn Pose Graph删除`RootOrientationWarp`和LocalYaw属性，只保留Sequence到Output的唯一姿态链。
- [x] 10.13 保持Gameplay只有`RunLoop -> MovingTurn`入口，并为Presentation的RunStart、RunEnd补齐观察已提交Turn事实的过渡；该阶段统一0.25秒的转场已由10.32至10.33替换为循环相位保留、0.12秒进入与0.30秒退出。
- [x] 10.14 通过Document v3执行checkout、dry-run、exact-hash apply与canonical reverse export。
- [x] 10.15 从同一精确Definition显式发布Float32 Program、Presentation Projection与Fixed Program。
- [x] 10.16 确认Document回到Clean，正式Validator和OpenSpec严格校验通过。
- [x] 10.17 将`RunEnd + move_has`重定向到规范RunLoop，让唯一`RunLoop -> MovingTurn`门禁决定反向输入，删除RunEnd私有Turn条件。
- [x] 10.18 让Local Fixed与DeterministicRollback Host注册和释放同一`AnimationPresentationRuntimeTarget`，补齐Pose运行诊断而不改变Rollback snapshot或网络协议。
- [x] 10.19 从同一精确Definition重新发布Float32 Program、Fixed Program、Presentation Projection与Native Pose Program，并刷新Gameplay Lab共享闭包。
- [x] 10.20 将`GameplayLabAssetBuilder`的Composition、Pipeline、Source与Variant输出统一到正式聚合目录，重建Prefab与场景引用并删除根目录同名重复资产。
- [ ] 10.21 重新执行正式Prepare与IL2CPP Product Build，以当前Program、Projection、KCC和Collision身份原子发布Relay与Unity Player精确闭包。
- [ ] 10.22 通过正式Run入口启动Relay与Peer A/B，确认三端持续推进、`invalid=0`、`dropped=0`后精确释放三端进程与UDP端口。
- [x] 10.23 复现Dodge Action Context存续期间`RunLoop -> MovingTurn`被隐藏选中的触发冲突，并确认其与输入采样或Root Motion曲线无关。
- [x] 10.24 通过Document v3把Attack与Dodge Action Context未激活条件并入唯一`RunLoop -> MovingTurn`门禁，保留既有`move_has + turn_facing_angle`业务条件；该阶段的150°参数已由10.31正式替换为135°。
- [x] 10.25 对该Document执行dry-run、exact-hash apply、canonical reverse export与正式Validator，并确认状态回到`Clean`。
- [x] 10.26 通过精确Definition分别显式发布Float32产品与精确`CorinFixedProgram.asset` Fixed产品，确认两目标共享SourceRevision与SemanticHash。
- [x] 10.27 让Gameplay Lab直接以Fixed Program生成Presentation Contract并校验Projection，删除Float32产物元数据对Fixed闭包的错误门禁。
- [x] 10.28 用正式Input System、Fixed Adapter和Fixed Program诊断动作互斥后的Turn选择，确认Action存续时零次进入、退出后恰好一次进入和一次释放，并删除临时探针。
- [ ] 10.29 重新执行Prepare、IL2CPP Product Build和manifest-only Run，以当前Program、Projection、KCC、Collision身份完成Relay与Peer A/B产品再闭合。
- [x] 10.30 从正式Input System诊断确认Action Context结束后反向输入仍会进入唯一Turn边，但150°窄门槛和统一0.25秒转场分别放大漏触发感与短动作衔接停顿。
- [x] 10.31 通过Document v3把唯一`RunLoop -> MovingTurn`的`MovingTurnAngleThreshold`从150°调整为135°，不增加第二Gameplay入口或释放条件。
- [x] 10.32 将Idle、WalkLoop与RunLoop的`AlwaysResetOnEntry`设为false，保持循环Pose连续相位；有限的WalkStart、RunStart、RunEnd与Turn继续在进入时重置。
- [x] 10.33 将RunStart、RunLoop、RunEnd进入Turn的Inertialization设为0.12秒，将Turn到RunLoop、WalkLoop、Idle的Inertialization设为0.30秒。
- [x] 10.34 完成checkout、dry-run、exact-hash apply、canonical reverse export与重新checkout，确认Document v3回到`Clean`。
- [x] 10.35 从同一精确Definition显式发布Float32 Program、Presentation Projection、Native Pose Program与Fixed Program，确认共同SourceRevision和SemanticHash。
- [x] 10.36 用正式Input System进行三个相互隔离的Local Fixed反向输入样本，确认每个样本恰好进入和退出Turn一次，运行时读取到0.12秒进入与0.30秒退出。
- [x] 10.37 删除临时运行探针并同步Project、串行执行记录、运行基准、Design、Spec与实施清单；执行OpenSpec严格校验。
- [ ] 10.38 重新执行正式Prepare，让Local Fixed与Rollback Variant共享当前Fixed Program、Projection、KCC和Collision精确身份。
- [ ] 10.39 重新执行正式IL2CPP Product Build，以当前ProgramHash、ProjectionRevision、SourceRevision和SemanticHash原子替换Rollback产品清单与91项文件闭包。
- [ ] 10.40 通过正式Run入口启动Relay与Peer A/B，确认两端前沿持续推进且`invalid=0`、`dropped=0`，随后精确释放三端进程与UDP端口。
