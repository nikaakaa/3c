## 1. 基线与依赖对齐

- [x] 1.1 重新读取本change的proposal、design、tasks和全部spec delta。
- [x] 1.2 导出当前Corin Agent v11 Full Snapshot并记录source revision、semantic hash、ProgramHash和LayoutHash。
- [x] 1.3 记录Corin Attack/Dodge ActionProfile的当前target requirement。
- [x] 1.4 记录全部Attack `CanActivateAction`节点的stable identity和目标引用。
- [x] 1.5 记录Attack1..Attack5 `ActivateActionInstance`节点的stable identity和目标引用。
- [x] 1.6 记录Attack1..Attack5 Timeline、主MotionCurve和后摇MotionCurve的stable identity与范围。
- [x] 1.7 记录当前MotionWarpTrack、MotionWarpClip和ActionTargetSnapshot declaration数量。
- [x] 1.8 盘点Float32与Fixed CharacterSimulationInput、canonical codec、hash和neutral input全部消费点。
- [x] 1.9 盘点ServerAuthoritative与DeterministicRollback输入复制、编码、历史和replay消费点。
- [x] 1.10 盘点CharacterPipelineHost固定输入、本地相机和Presentation装配字段。
- [x] 1.11 盘点CharacterSimulationActorRegistration对具体Unity input adapter的依赖。
- [x] 1.12 盘点StandaloneGameplay玩家prefab、Session Host、ActorId和World binding。
- [x] 1.13 读取已完成`refactor-deterministic-rollback-input-propagation`的最终input阶段、request timing、codec与identity实现并作为唯一基线。
- [x] 1.14 确认实施可沿唯一Input、Program、Session、WorldSolver和Presentation主链完成。

## 2. Action目标策略

- [x] 2.1 在numeric-neutral语义中增加`OptionalSnapshot` target requirement。
- [x] 2.2 保持`None`和`SnapshotRequired`现有稳定枚举identity。
- [x] 2.3 更新ActionProfile authoring校验接受三种typed策略。
- [x] 2.4 更新Action catalog canonical字段编码三种target requirement。
- [x] 2.5 更新Semantic IR target requirement reader和Inspector。
- [x] 2.6 更新Float32 Program lowerer读取`OptionalSnapshot`。
- [x] 2.7 更新Fixed Program lowerer读取`OptionalSnapshot`。
- [x] 2.8 更新portable admission evaluator的Optional有目标准入结果。
- [x] 2.9 更新portable admission evaluator的Optional无目标准入结果。
- [x] 2.10 保持SnapshotRequired无目标返回typed reject。
- [x] 2.11 保持None动作不接收target candidate。
- [x] 2.12 更新ActionInstance创建逻辑在Optional有目标时冻结snapshot。
- [x] 2.13 更新ActionInstance创建逻辑在Optional无目标时保存None。
- [x] 2.14 更新Action target diagnostics显示requirement、candidate与capture结果。

## 3. Portable目标输入合同

- [x] 3.1 为Float32 `SimulationInputValueKind`增加ActionTargetSnapshot kind。
- [x] 3.2 为Fixed `SimulationInputValueKind`增加同语义ActionTargetSnapshot kind。
- [x] 3.3 为Float32 input value增加typed target payload factory和访问器。
- [x] 3.4 为Fixed input value增加typed target payload factory和访问器。
- [x] 3.5 保持TargetId、position和yaw字段顺序稳定。
- [x] 3.6 更新Float32 CharacterSimulationInput排序与重复input id校验。
- [x] 3.7 更新Fixed CharacterSimulationInput排序与重复input id校验。
- [x] 3.8 更新Float32 canonical input codec写入target payload。
- [x] 3.9 更新Float32 canonical input codec读取target payload。
- [x] 3.10 更新Fixed/Rollback input codec写入target payload。
- [x] 3.11 更新Fixed/Rollback input codec读取target payload。
- [x] 3.12 将target payload纳入Float32 GameplayHash。
- [x] 3.13 将target payload纳入Fixed GameplayHash。
- [x] 3.14 更新ServerAuthoritative command复制target payload。
- [x] 3.15 更新ServerAuthoritative neutral input生成target None。
- [x] 3.16 更新DeterministicRollback exact/canonical/predicted input复制target payload。
- [x] 3.17 规定预测缺失显式输入时target candidate为None而不延续旧目标。
- [x] 3.18 更新portable Reader显示target input摘要。
- [x] 3.19 提升受影响input schema、protocol和ABI identity。
- [x] 3.20 删除旧input reader和未知kind兼容分支。

## 4. InputDerived Blackboard authoring

- [x] 4.1 为Blackboard declaration增加显式InputValueId metadata。
- [x] 4.2 只在SyncPolicy=InputDerived时显示InputValueId编辑字段。
- [x] 4.3 校验InputDerived必须提供非空稳定input id。
- [x] 4.4 校验InputDerived declaration使用Character scope。
- [x] 4.5 校验InputDerived declaration使用Spawn lifetime。
- [x] 4.6 校验InputDerived declaration不得使用PresentationOnly authority。
- [x] 4.7 校验非InputDerived declaration不得残留InputValueId。
- [x] 4.8 将InputValueId写入Blackboard Semantic catalog。
- [x] 4.9 将InputValueId写入SemanticHash和source revision。
- [x] 4.10 将InputDerived binding降低进Float32 Program Layout。
- [x] 4.11 将InputDerived binding降低进Fixed Program Layout。
- [x] 4.12 在Float32 composition阶段验证input kind与Blackboard value kind一致。
- [x] 4.13 在Fixed composition阶段验证input kind与Blackboard value kind一致。
- [x] 4.14 在Float32 actor Evaluate的Graph control前投影InputDerived值。
- [x] 4.15 在Fixed actor Evaluate的Graph control前投影InputDerived值。
- [x] 4.16 让InputDerived写入使用当前State Transaction和typed write stamp。
- [x] 4.17 让Evaluate失败时InputDerived写入随事务一起回滚。
- [x] 4.18 更新Blackboard Inspector和Graph Data Catalog显示input binding。
- [x] 4.19 更新Runtime diagnostics显示input id、declaration和当前target摘要。
- [x] 4.20 删除任何外部直接写CharacterState Blackboard的试验入口。

## 5. MotionWarp Optional目标执行

- [x] 5.1 更新MotionWarp authoring校验允许OptionalSnapshot Action。
- [x] 5.2 保持MotionWarp authoring拒绝None Action。
- [x] 5.3 保持SnapshotRequired call site必须绑定目标declaration。
- [x] 5.4 要求OptionalSnapshot call site的CanActivate与Activate也引用同一目标declaration。
- [x] 5.5 在portable Motion Modifier eligibility增加NoTargetByOptionalPolicy结果。
- [x] 5.6 Float32 MotionWarp在Optional无目标时保持resolved source不变。
- [x] 5.7 Fixed MotionWarp在Optional无目标时保持resolved source不变。
- [x] 5.8 Optional无目标时不初始化MotionWarp跨Tickstate。
- [x] 5.9 Optional无目标时不产生position或yaw correction。
- [x] 5.10 SnapshotRequired无目标继续fail-stop准入而不进入Warp。
- [x] 5.11 更新Float32 MotionWarp trace区分Applied与NoTargetByOptionalPolicy。
- [x] 5.12 更新Fixed MotionWarp trace区分Applied与NoTargetByOptionalPolicy。
- [x] 5.13 更新Timeline Live Debug显示Optional无目标状态。
- [x] 5.14 更新Program/State/Modifier版本identity。

## 6. Unity输入来源与目标provider

- [x] 6.1 定义Unity-facing Character simulation input adapter生命周期合同。
- [x] 6.2 让现有UnityCharacterSimulationInputAdapter实现统一生命周期合同。
- [x] 6.3 定义窄`ICharacterActionTargetInputProvider`读取合同。
- [x] 6.4 provider输出必须包含稳定TargetId、position、yaw和validity。
- [x] 6.5 Unity input adapter在RenderFrame采样时锁存target candidate。
- [x] 6.6 Unity input adapter在BuildInput时写入typed target input value。
- [x] 6.7 provider不可用时写入显式None，不缓存上一目标。
- [x] 6.8 定义显式Session Actor target provider组件。
- [x] 6.9 provider必须显式引用目标Character host或稳定Actor binding。
- [x] 6.10 provider拒绝目标ActorId与owner相同。
- [x] 6.11 provider拒绝目标属于另一个SimulationSessionHost。
- [x] 6.12 Character registration从InitialBody初始化最近提交Body。
- [x] 6.13 Character registration在每次published result更新最近提交Body。
- [x] 6.14 provider只读取最近提交Body，不读取VisualRoot或Animator Transform。
- [x] 6.15 provider不扫描Scene、Tag、名称或全局registry。
- [x] 6.16 更新输入adapter identity包含target input配置identity。
- [x] 6.17 更新input diagnostics显示provider、target Actor和采样Body tick。

## 7. Neutral Simulated Actor装配

- [x] 7.1 定义Character control source factory合同。
- [x] 7.2 将现有玩家Unity input adapter创建迁入Player control source。
- [x] 7.3 定义Neutral control source。
- [x] 7.4 从Program input catalog建立neutral continuous value模板。
- [x] 7.5 Neutral adapter对Bool输出false。
- [x] 7.6 Neutral adapter对Scalar输出zero。
- [x] 7.7 Neutral adapter对Vector2/Vector3输出zero。
- [x] 7.8 Neutral adapter对Yaw输出zero。
- [x] 7.9 Neutral adapter对ActionTargetSnapshot输出None。
- [x] 7.10 Neutral adapter始终输出空request集合。
- [x] 7.11 Neutral adapter不读取InputAction、Camera、Scene或Character名字。
- [x] 7.12 将CharacterSimulationActorRegistration的具体Unity adapter依赖改为统一生命周期合同。
- [x] 7.13 保持Local Session Source只消费同一ISimulationInputAdapter port。
- [x] 7.14 定义Character Presentation Role的LocalOwner与SimulatedActor。
- [x] 7.15 LocalOwner role继续创建Camera runtime和look input绑定。
- [x] 7.16 SimulatedActor role调用正式CreateSimulatedActor factory。
- [x] 7.17 SimulatedActor role不要求CameraRig、follow anchor、aim anchor或look input id。
- [x] 7.18 CharacterPipelineHost按显式control source和presentation role装配registration。
- [x] 7.19 删除Host内部固定new UnityCharacterSimulationInputAdapter路径。
- [x] 7.20 删除SimulatedActor不需要的camera fallback与自动猜测。
- [x] 7.21 保持Player与Simulated Actor共用同一Program/Projection/diagnostics/output链。
- [x] 7.22 保持Actor dispose和Session stop顺序不产生双释放。

## 8. Agent v11目标authoring闭环

- [x] 8.1 确认Agent代码、current spec、project与技能唯一使用现行v11，并在该合同内扩展目标authoring。
- [x] 8.2 在Agent Snapshot输出Blackboard InputValueId。
- [x] 8.3 在Agent Snapshot输出ActionTargetSnapshot declaration类型。
- [x] 8.4 在Agent Snapshot输出CanActivateAction目标declaration identity与key。
- [x] 8.5 保持Action activation目标declaration完整投影。
- [x] 8.6 在Patch value type parser支持ActionTargetSnapshot。
- [x] 8.7 在ensure/move blackboard declaration command支持InputValueId。
- [x] 8.8 lowerer校验InputDerived与InputValueId组合。
- [x] 8.9 handler通过正式Blackboard authoring API保存InputValueId。
- [x] 8.10 为action_can_activate condition term增加target declaration reference。
- [x] 8.11 condition builder配置CanActivateActionInfoNode目标引用。
- [x] 8.12 增加`set_action_profile_target_requirement`typed operation。
- [x] 8.13 lowerer只接受None、OptionalSnapshot和SnapshotRequired。
- [x] 8.14 handler原子修改当前Definition内明确ActionProfile。
- [x] 8.15 validator检查CanActivate与reachable Activate引用同一declaration。
- [x] 8.16 validator检查Optional/SnapshotRequired与MotionWarp组合。
- [x] 8.17 current contract记录四类MotionWarp typed operation。
- [x] 8.18 current contract记录target declaration、admission和profile操作。
- [x] 8.19 保持dry-run与apply消费同一immutable typed plan。
- [x] 8.20 全局确认目标authoring没有引入旧schema reader、writer、converter或第二份宽DTO解释路径。

## 9. Corin目标与Action资产迁移

- [x] 9.1 使用正式Agent export_snapshot取得迁移前v11 identity。
- [x] 9.2 为Corin Root owner创建唯一ActionTargetSnapshot declaration。
- [x] 9.3 将declaration配置为Character scope与Spawn lifetime。
- [x] 9.4 将declaration配置为ClientPredicted authority与InputDerived sync policy。
- [x] 9.5 将declaration绑定正式ActionTarget input id并归类到Combat/Targeting。
- [x] 9.6 将Corin Attack Profile配置为OptionalSnapshot。
- [x] 9.7 保持Corin Dodge Profile为None。
- [x] 9.8 让None到Attack的CanActivate引用目标declaration。
- [x] 9.9 让Dodge到Attack的CanActivate引用目标declaration。
- [x] 9.10 让Attack recovery/cancel中的全部CanActivate Attack引用目标declaration。
- [x] 9.11 让Attack1 Activate引用同一目标declaration。
- [x] 9.12 让Attack2 Activate引用同一目标declaration。
- [x] 9.13 让Attack3 Activate引用同一目标declaration。
- [x] 9.14 让Attack4 Activate引用同一目标declaration。
- [x] 9.15 让Attack5 Activate引用同一目标declaration。
- [x] 9.16 保持所有CanActivate Dodge和Dodge Activate无目标引用。
- [x] 9.17 dry-run同一Patch并检查planned diff只包含目标资产变更。
- [x] 9.18 apply完全相同Patch并保存唯一authoring资产。
- [x] 9.19 重新export_snapshot确认declaration与全部call site一致。

## 10. Corin Attack Timeline MotionWarp迁移

- [x] 10.1 为Attack1 Timeline创建MotionWarpTrack。
- [x] 10.2 为Attack1主MotionCurve创建显式source Warp Clip。
- [x] 10.3 为Attack1配置合法窗口、position/yaw mode、offset、weight和clamp。
- [x] 10.4 为Attack1配置canonical position/yaw累计曲线。
- [x] 10.5 为Attack2 Timeline创建MotionWarpTrack。
- [x] 10.6 为Attack2主MotionCurve创建并配置Warp Clip。
- [x] 10.7 为Attack3 Timeline创建MotionWarpTrack。
- [x] 10.8 为Attack3主MotionCurve创建并配置Warp Clip。
- [x] 10.9 为Attack4 Timeline创建MotionWarpTrack。
- [x] 10.10 为Attack4主MotionCurve创建并配置Warp Clip。
- [x] 10.11 为Attack5 Timeline创建MotionWarpTrack。
- [x] 10.12 为Attack5主MotionCurve创建并配置Warp Clip。
- [x] 10.13 确认五个Warp source都不引用后摇MotionCurve。
- [x] 10.14 确认五个Warp窗口都完全位于各自主MotionCurve范围。
- [x] 10.15 确认同一source没有重叠Warp窗口。
- [x] 10.16 dry-run同一Timeline Patch并检查planned diff。
- [x] 10.17 apply完全相同Timeline Patch。
- [x] 10.18 重新export_snapshot确认Track/Clip/source identity和参数。
- [x] 10.19 运行正式Agent validate和Compiler验证。

## 11. Corin玩家与训练敌人资产

- [x] 11.1 创建正式Player control source配置并迁移现有Corin prefab。
- [x] 11.2 将现有Corin prefab表现角色配置为LocalOwner。
- [x] 11.3 将玩家target input id与InputDerived declaration保持一致。
- [x] 11.4 创建复用Corin Definition的训练敌人prefab variant或正式prefab。
- [x] 11.5 将训练敌人control source配置为Neutral。
- [x] 11.6 将训练敌人表现角色配置为SimulatedActor。
- [x] 11.7 为训练敌人配置唯一ActorId和World body binding identity。
- [x] 11.8 训练敌人保留正式Animancer、VisualRoot和Body presentation profile。
- [x] 11.9 训练敌人不配置CameraRig或玩家look input。
- [x] 11.10 在StandaloneGameplay中加入训练敌人。
- [x] 11.11 将训练敌人与玩家绑定到同一SimulationSessionHost。
- [x] 11.12 在玩家配置显式Session Actor target provider。
- [x] 11.13 provider精确绑定训练敌人Actor而不使用场景搜索。
- [x] 11.14 配置玩家与训练敌人的初始位置和朝向便于观察Warp。
- [x] 11.15 保持Bootstrap和Standalone Scene入口不新增第二启动路径。
- [x] 11.16 确认Scene与prefab没有旧固定input装配字段残留。

## 12. Generated products与诊断

- [x] 12.1 提升Semantic operation set和input schema版本。
- [x] 12.2 提升Float32 Program ABI与State codec identity。
- [x] 12.3 提升Fixed Program ABI与State codec identity。
- [x] 12.4 更新ServerAuthoritative protocol/schema identity。
- [x] 12.5 更新DeterministicRollback protocol/schema identity。
- [x] 12.6 重新生成Corin Semantic IR。
- [x] 12.7 重新生成Corin Float32 Program asset与canonical artifact。
- [x] 12.8 重新生成Corin Fixed Program artifact。
- [x] 12.9 重新生成Corin Presentation Projection。
- [x] 12.10 更新受影响的Local与Network Test Product manifest identity来源。
- [x] 12.11 在Motion trace显示candidate target、captured target和Optional no-target结果。
- [x] 12.12 在Timeline Live Debug显示五段Warp的source、progress和修正。
- [x] 12.13 确认diagnostics只读且不重新计算目标或Warp。

## 13. 废弃路径删除与文档统一

- [x] 13.1 删除CharacterPipelineHost固定创建Unity input adapter的旧路径。
- [x] 13.2 删除CharacterSimulationActorRegistration对具体Unity adapter类型的旧依赖。
- [x] 13.3 删除旧Host字段的序列化残留和兼容读取。
- [x] 13.4 删除任何Scene/Tag/name target lookup代码。
- [x] 13.5 删除任何VisualRoot/Animator Transform作为目标Gameplay pose的读取。
- [x] 13.6 删除任何Target专用packet、第二input buffer或Blackboard直写入口。
- [x] 13.7 更新Agent v11技能合同，补齐MotionWarp与Action target正式操作并删除目标authoring过时描述。
- [x] 13.8 更新`openspec/project.md`的Agent v11与MotionWarp实际Corin接入状态。
- [x] 13.9 更新current specs中的严格目标语义为三种typed requirement。
- [x] 13.10 更新current specs中的InputDerived正式runtime含义。
- [x] 13.11 记录训练敌人是Neutral Simulated Actor而非完整AI/Combat closure。
- [x] 13.12 全局搜索确认没有旧input ABI reader或fallback分支。

## 14. 编译与规范校验

- [x] 14.1 构建portable Core、Float32和Fixed工程并禁用build server/shared compilation。
- [x] 14.2 构建DeterministicRollback与Endpoint工程并使用相同参数。
- [x] 14.3 构建受影响Unity runtime/editor工程并使用相同参数。
- [x] 14.4 每次编译后立即执行`dotnet build-server shutdown`。
- [x] 14.5 运行Corin正式Agent v11 validate。
- [x] 14.6 确认Corin Program报告包含五个Motion Modifier且无authoring错误。
- [x] 14.7 运行`openspec validate add-corin-targeted-motion-warp-demo --strict --no-interactive`。
- [x] 14.8 核对tasks勾选与最终统一链路一致。
