## 1. 收敛最终Foot IK合同

- [x] 1.1 对账current `character-foot-placement-presentation`、917/d24/df74/8fc行为备份、KKK参考与GDC学习文案，明确保留Animated Sole空间进度、FootPath增量、唯一Correction Owner和Plant时Anchor提交，拒绝Set Mesh、双IK与Current Trace补洞
- [x] 1.2 对账当前实现，列出需要删除的Plant总处理器、旧状态、LandingPreparation、ContactTransition、GoalSet兼容容器和过期CSV字段
- [x] 1.3 复用唯一`refactor-character-pose-constraint-transaction` change，确认不恢复被删除的Predictive/Reactive并行active change或创建第二Foot IK proposal
- [x] 1.4 对比三个current spec与`openspec/project.md`，记录Swing-only、plural GoalSet、LegIK术语和当前实现真相的差异

## 2. 保留并封闭根事务

- [ ] 2.1 让唯一PosePlanExecutionRuntime持有唯一CharacterPoseConstraintRuntime，正式Runtime与Preview复用同一构造路径
- [ ] 2.2 建立Frame/Completion/Rig lineage、双Bank和唯一Committed Bank identity
- [ ] 2.3 将左右CharacterFootStateContext、Resolved Foot、Primary Support、Pelvis Spring、Body Trajectory、Goal Assembly、BendHistory与Diagnostics纳入根Bank
- [ ] 2.4 建立BeginFrame、PrepareFootPlacement、AssembleGoals、SolveFullBodyIk、Validate、Seal/Discard与Invalidate窄接口，删除外部可变Bank访问
- [ ] 2.5 对账Reset、Retarget、Pose discontinuity、Action硬抢占和Dispose，确保左右Foot State Context、Pelvis与Bend状态由根Runtime一次失效

## 3. 重建Swing Path Target与唯一Correction

- [x] 3.1 保留Landing Prediction、Proposal更新死区、Ground Path固定页、Reachability、Envelope和唯一World Query Seam的正式Result链
- [ ] 3.2 在CharacterFootStateContext中新增Path Target、Stable/Rebasing/Unavailable跟踪事实与SettledFrameCount，不创建独立Path状态页或Path Output/Velocity
- [ ] 3.3 新增PathCorrectionFrequency、PathSettledDistance、PathSettledSpeed和PathSettledFrameCount正式配置，删除ContactTransitionSeconds、Ownership HalfLife与LandingPreparation配置
- [ ] 3.4 让同Event Path Target变化保留Context中的唯一Effective Correction/Velocity并只替换Target，Swing状态使用临界阻尼更新且不重启固定Duration
- [ ] 3.5 将Swing Progress改为Animated Sole在LastContact到NextLanding方向上的空间投影比例
- [ ] 3.6 让Baseline与Envelope按同一纵向进度采样，把Raw Swing Correction改为非负`Envelope Sample - Baseline Sample`沿Component Up增量，并让Path Target逐值乘同帧动画Foot Placement Weight
- [ ] 3.7 删除Phase采样、`Envelope - Animated Sole`、实时Path硬地面下限、LandingPreparation和任何CurrentTrace第二Swing高度来源
- [ ] 3.8 只有Swing中的Effective Correction误差、Effective Velocity和连续帧数同时满足门槛时发布Path Stable；Tracking Status不得成为Landing事件准入或第二状态机，Rebasing实时Proposal不得进入Pelvis Stride
- [ ] 3.9 扩展Foot Analysis/Projection Build质量校验，发布唯一LandingStarted、单调LandingHeightProgress、唯一PlantStarted、Support和单调Release Progress；禁止Runtime直接解释Constraint或PlantConfidence

## 4. 重建单脚Constraint状态机

- [ ] 4.1 新增固定布局CharacterFootStateContext，集中保存`Swing/Landing/Locked/Releasing/UnlockedSupport`五状态、Path事实、Active/Consumed Event、FrozenContactPatch、CommittedAnchor、唯一Effective Correction/Velocity、TransitionCause、Landing/Release Residual与Progress
- [ ] 4.2 删除`Tracking/Acquiring/Committed/Closed`旧状态、ContactOwnership主控制字段、Sliding水平削弱和旧Plant命名
- [ ] 4.3 在CharacterFootStateMachine内部新增纯Trigger Resolver，按Hard Invalid、Action、Invalid、Grounded/Reachability、Anchor超距、ReleaseStarted、PlantStarted、LandingStarted固定优先级归一同帧事实；Path Observation先更新但不作为状态Trigger
- [ ] 4.4 实现CharacterFootStateMachine唯一Context写入路径，一次Evaluate最多转换一次状态并同时生成Pending Context与Resolved Foot；内部纯计算不得保存第二状态页或Correction
- [ ] 4.5 实现Swing到Landing：Projection发布LandingStarted时只要求合法匹配Patch、Grounded、Action未占用和Patch可达；冻结Surface/Plane/Normal而非Prediction XYZ，Path Settled不得成为准入条件
- [ ] 4.6 实现Landing垂直交接：保留动画脚XZ，入口捕获一次LandingResidual，只按Projection的单调LandingHeightProgress投影FrozenPatch；实时Path不执行硬地面下限
- [ ] 4.7 实现PlantStarted原子提交：用当帧Effective Sole投影FrozenPatch生成CommittedAnchor；Anchor XZ不得来自Prediction Point，准入失败进入UnlockedSupport，不新增Planting/Acquiring状态
- [ ] 4.8 实现Locked：Effective Correction严格等于CommittedAnchor减Animated Sole，非零Goal权重1，LockDistance只发布NearRelease，ReleaseDistance触发Releasing；删除Sliding
- [ ] 4.9 实现正常Releasing：入口捕获一次ReleaseResidual，只按权威Release进度回到原生动画脚，期间只更新下一Event Path事实；完成后同一个Effective Correction/Velocity进入Swing
- [ ] 4.10 实现Grounded丢失、Anchor超距与不可达的正式Safety Release，使用ContactLossReleaseSeconds且与正常Release互斥
- [ ] 4.11 实现Action硬抢占和Reset/Retarget硬失效；Action消费当前Event但不运行Foot Release，lineage失效清空Consumed Event

## 5. 深化Foot Placement Module与Pelvis数据流

- [ ] 5.1 删除CharacterFootPlantTransaction、旧CharacterFootPlantModule和对外Route/Reducer/Resolver/Builder浅链，建立唯一深CharacterFootPlacementModule Interface
- [ ] 5.2 建立左右CharacterFootStateMachine；把Landing Prediction、Ground Path、Swing Target、Trigger、Constraint数学和Resolved Foot Builder收为Implementation内部纯函数或内部Module，禁止它们拥有Pending/Committed与跨帧输出
- [ ] 5.3 生成左右Resolved Foot与唯一Resolved Foot Pair，聚合Path状态、Constraint状态、FrozenPatch、CommittedAnchor、Correction、Final Sole/Ankle、Contact Reference和Support Intent
- [ ] 5.4 让Support Intent直接来自Biomechanical Support并与Contact Ownership分离；Landing尚未Locked时也能发布正式承重意图
- [ ] 5.5 让Primary Support与Pelvis只消费Resolved Foot Pair；Rebasing实时Proposal不得改变Stride终点，Landing Patch不得伪造Anchor
- [ ] 5.6 让Pelvis Target同时通过上一支撑腿与正在Landing腿的可达区间，再由唯一临界阻尼Spring输出；不得等Locked帧才突然接入Landing腿

## 6. 完成唯一Goal贡献ABI

- [ ] 6.1 将Foot Placement与PoseBone Goal来源改为真正独立的固定typed Goal Contribution，不再复用GoalSet Header
- [ ] 6.2 新增唯一CharacterFullBodyIkGoalAssembler和固定Slot/lineage/重复贡献验证
- [ ] 6.3 修改Projection Compiler与拓扑Validator，要求一个Assembler、一个Goal Set、一个FBBIK、一个Output和一个Writer
- [ ] 6.4 拆分Contribution Workspace与唯一GoalSet Workspace，删除plural GoalSet input、passthrough/copy、Empty Goal兼容路径和旧端口字段
- [ ] 6.5 让Foot Goal Encoder只编码AnimationAnkle加Resolved Foot中的EffectiveCorrection，非零权重1，并禁止读取Foot State Context、Constraint State、Residual或再次平滑

## 7. 完成FBBIK与Physical提交闭包

- [ ] 7.1 新增左右BendHistory Pending/Committed页并接入根Bank
- [ ] 7.2 将Stable/Applied Bend历史移出Solver对象并改为Profile显式腿稳定策略
- [ ] 7.3 新增BendHistory SourceCompletionIdentity与Revision，并从Committed历史初始化Pending
- [ ] 7.4 把紧凑CharacterFullBodyIkResult、Goal lineage、Solver Outcome和BendHistory完整写入Pending Bank，失败不得改变Committed历史
- [ ] 7.5 将Physical Writer拆成完整binding/pose预验证与唯一Apply，Apply成功后只允许no-throw根Bank发布
- [ ] 7.6 对账Locked脚的Target、Solved与Physical Sole残差；超过正式容差时阻止根BankSeal，不得静默提交穿模Locked Pose

## 8. 深冻结Diagnostics

- [ ] 8.1 在BeginFrame冻结并预验证Foot/FBBIK/Physical diagnostics interest与固定容量
- [ ] 8.2 新增Pending Runtime Result到Pending Diagnostics页的单向no-throw深冻结Projector，删除运行方法中的`*Diagnostics`/`*Snapshot`依赖和可变页浅引用
- [ ] 8.3 让Physical Writer成功Apply时把实际Write Completion和最终Physical Bone位置写入同一Pending Diagnostics页
- [ ] 8.4 重写CSV为Path Tracking Status/Target、唯一Effective Correction/Velocity、Constraint State/Trigger/Cause、LandingStarted/LandingHeightProgress/PlantStarted/ReleaseProgress、Active/Consumed Event、FrozenPatch、CommittedAnchor、Residual、SupportIntent、Final Sole、Pelvis Reference、Goal/Solved/Physical Residual
- [ ] 8.5 删除LandingPreparation、OwnershipHalfLife、CurrentTrace、PlantConfidence Ownership、Sliding、SupportLock、GoalTransition和其它兼容CSV/Gizmo字段
- [ ] 8.6 让Gizmo、CSV、Trace和Pose Watch只读取同Frame/Completion/Rig/Bank identity的Committed深冻结页，无interest时不复制Ground Path大页或逐腿Pose

## 9. 激进清理与最终一致性

- [x] 9.1 保持被取代Predictive/Reactive active change目录删除状态，并保持`add-discrete-stair-presentation`只剩非Foot Placement范围
- [ ] 9.2 删除旧Plant类、状态、配置、Profile字段、Debug Registry路径、GoalSet兼容容器、临时junction和重复文件路径
- [ ] 9.3 搜索并消除第二Goal Set、第二FBBIK、第二Physical Writer、第二Foot状态写入者、独立Path Output/Velocity、Pelvis Path旁路、外部Bank写入、CurrentTrace和运行Diagnostics依赖
- [ ] 9.4 更新`openspec/project.md`为实际实现状态，并精确指出Reactive、Heel/Toe、旋转和移动平台仍未实现
- [ ] 9.5 使用规定参数编译Runtime与Editor工程，并在每次构建后立即关闭dotnet build server
- [ ] 9.6 执行`git diff --check`、本change严格校验和全量严格OpenSpec校验，修复全部格式、交叉引用和规范错误
