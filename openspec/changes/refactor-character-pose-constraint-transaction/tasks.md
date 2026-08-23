## 1. 收敛最终Foot IK合同

- [x] 1.1 对账current `character-foot-placement-presentation`、917基线、KKK参考与GDC学习文案，明确保留Animated Sole空间进度、FootPath增量和开始落脚时锁点，拒绝Set Mesh、双IK与Current Trace补洞
- [x] 1.2 对账当前实现，列出需要删除的Plant总处理器、旧状态、LandingPreparation、ContactTransition、GoalSet兼容容器和过期CSV字段
- [x] 1.3 复用唯一`refactor-character-pose-constraint-transaction` change，确认不恢复被删除的Predictive/Reactive并行active change或创建第二Foot IK proposal
- [x] 1.4 对比三个current spec与`openspec/project.md`，记录Swing-only、plural GoalSet、LegIK术语和当前实现真相的差异

## 2. 保留并封闭根事务

- [ ] 2.1 让唯一PosePlanExecutionRuntime持有唯一CharacterPoseConstraintRuntime，正式Runtime与Preview复用同一构造路径
- [ ] 2.2 建立Frame/Completion/Rig lineage、双Bank和唯一Committed Bank identity
- [ ] 2.3 将Foot、Primary Support、Pelvis Spring、Body Trajectory、Goal Assembly、BendHistory与Diagnostics纳入根Bank
- [ ] 2.4 建立BeginFrame、PrepareFootPlacement、AssembleGoals、SolveFullBodyIk、Validate、Seal/Discard与Invalidate窄接口，删除外部可变Bank访问
- [ ] 2.5 对账Reset、Retarget、Pose discontinuity、Action硬抢占和Dispose，确保整个Route/Constraint/Pelvis/Bend状态由根Runtime一次失效

## 3. 重建Route与Swing

- [x] 3.1 保留Landing Prediction、Proposal更新死区、Ground Path固定页、Reachability、Envelope和唯一World Query Seam的正式Result链
- [ ] 3.2 新增Path Stable/Rebasing状态页，保存Target/Output/Velocity/SettledFrameCount并接入根Bank
- [ ] 3.3 新增PathCorrectionFrequency、PathSettledDistance、PathSettledSpeed和PathSettledFrameCount正式配置，删除ContactTransitionSeconds、Ownership HalfLife与LandingPreparation配置
- [ ] 3.4 让同Event Path Target变化保留当前Output/Velocity并只替换Target，使用临界阻尼更新且不重启固定Duration
- [ ] 3.5 将Swing Progress改为Animated Sole在LastContact到NextLanding方向上的空间投影比例
- [ ] 3.6 让Baseline与Envelope按同一纵向进度采样，并把Swing Target改为非负`Envelope Sample - Baseline Sample`沿Component Up增量
- [ ] 3.7 删除Phase采样、`Envelope - Animated Sole`、实时Landing Height下限、LandingPreparation和任何CurrentTrace第二Swing高度来源
- [ ] 3.8 只有连续满足Settled距离、速度和帧数门槛时发布Path Stable；Rebasing结果允许连续Swing但不得取得锁脚资格或进入Pelvis Stride
- [ ] 3.9 扩展Foot Analysis/Projection Build质量校验，证明每个Predictive循环Event具有完整Constraint锁入、Support和Release实际coverage，删除Runtime固定Duration补偿

## 4. 重建单脚Constraint状态机

- [ ] 4.1 新增`Swing/Landing/Locked/Releasing/UnlockedSupport`五状态、Active/Consumed Event、FrozenPatch、TransitionCause、Residual、Progress与ReleaseTargetState正式合同
- [ ] 4.2 删除`Tracking/Acquiring/Committed/Closed`旧状态、ContactOwnership主控制字段、Sliding水平削弱和旧Plant命名
- [ ] 4.3 新增纯Constraint Trigger Resolver，按Hard Invalid、Action、Invalid、Grounded/Reachability、超距、开始抬脚、开始落脚、Path Revision固定优先级归一同帧事实
- [ ] 4.4 新增纯Constraint Reducer，只执行状态转换、Event消费、Patch冻结与Transition入口事实，不查询世界、采样Path、生成Goal或计算Pelvis
- [ ] 4.5 实现Swing到Landing：动画开始落脚时，只有Path Stable、Proposal/Event匹配、Grounded、Action未占用和目标可达才冻结完整Patch；否则进入UnlockedSupport并消费Event
- [ ] 4.6 实现Landing锁入：入口捕获一次AcquireResidual，按动画ConstraintWeight单调上升，完成后进入Locked
- [ ] 4.7 实现Locked：Effective Correction严格等于FrozenAnchor减Animated Sole，非零Goal权重1，LockDistance只发布NearRelease，ReleaseDistance触发Releasing
- [ ] 4.8 实现正常Releasing：入口捕获一次ReleaseResidual，只按动画Constraint下降回到原生动画脚，期间Path只更新Next Route
- [ ] 4.9 实现Grounded丢失、Contact超距与不可达的正式Safety Release，使用ContactLossReleaseSeconds且与正常Release互斥
- [ ] 4.10 实现Action硬抢占和Reset/Retarget硬失效；Action消费当前Event但不运行Foot Release，lineage失效清空Consumed Event

## 5. 深化Foot模块与Pelvis数据流

- [ ] 5.1 删除CharacterFootPlantTransaction和CharacterFootPlantModule，建立Route Module、Swing Resolver、Constraint Reducer、Constraint Resolver、Resolved Foot Builder、Support Module和Pelvis Module固定调用链
- [ ] 5.2 拆分PlantTransactionContracts杂糅文件，把Route、Constraint、ResolvedFoot、Support和Pelvis类型放回对应模块
- [ ] 5.3 生成左右Resolved Foot与唯一Resolved Foot Pair，聚合Path状态、Constraint状态、Patch、Correction、Final Sole/Ankle、Contact Reference和Support Intent
- [ ] 5.4 让Primary Support只消费Resolved Support Intent，并保留上一Committed选择直到另一侧正式取得更高支撑意图
- [ ] 5.5 让Pelvis只消费Resolved Foot Pair、Support选择和腿可达事实；Rebasing中的不稳定Path不得改变Stride终点
- [ ] 5.6 让Pelvis Target先通过支撑腿可达区间限制，再由唯一临界阻尼Spring输出；脚与盆骨必须保存相同Patch identity

## 6. 完成唯一Goal贡献ABI

- [ ] 6.1 将Foot Placement与PoseBone Goal来源改为真正独立的固定typed Goal Contribution，不再复用GoalSet Header
- [ ] 6.2 新增唯一CharacterFullBodyIkGoalAssembler和固定Slot/lineage/重复贡献验证
- [ ] 6.3 修改Projection Compiler与拓扑Validator，要求一个Assembler、一个Goal Set、一个FBBIK、一个Output和一个Writer
- [ ] 6.4 拆分Contribution Workspace与唯一GoalSet Workspace，删除plural GoalSet input、passthrough/copy、Empty Goal兼容路径和旧端口字段
- [ ] 6.5 让Foot Goal Encoder只编码AnimationAnkle加EffectiveCorrection，非零权重1，并禁止读取Route、Constraint State、Residual或再次平滑

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
- [ ] 8.4 重写CSV为Path State/Target/Output/Velocity、Constraint State/Trigger/Cause、Active/Consumed Event、Patch、Residual/Progress、Final Sole、Pelvis Reference、Goal/Solved/Physical Residual
- [ ] 8.5 删除LandingPreparation、OwnershipHalfLife、CurrentTrace、旧Plant State、SupportLock、GoalTransition和其它兼容CSV/Gizmo字段
- [ ] 8.6 让Gizmo、CSV、Trace和Pose Watch只读取同Frame/Completion/Rig/Bank identity的Committed深冻结页，无interest时不复制Ground Path大页或逐腿Pose

## 9. 激进清理与最终一致性

- [x] 9.1 保持被取代Predictive/Reactive active change目录删除状态，并保持`add-discrete-stair-presentation`只剩非Foot Placement范围
- [ ] 9.2 删除旧Plant类、状态、配置、Profile字段、Debug Registry路径、GoalSet兼容容器、临时junction和重复文件路径
- [ ] 9.3 搜索并消除第二Goal Set、第二FBBIK、第二Physical Writer、Pelvis Route旁路、外部Bank写入、CurrentTrace和运行Diagnostics依赖
- [ ] 9.4 更新`openspec/project.md`为实际实现状态，并精确指出Reactive、Heel/Toe、旋转和移动平台仍未实现
- [ ] 9.5 使用规定参数编译Runtime与Editor工程，并在每次构建后立即关闭dotnet build server
- [ ] 9.6 执行`git diff --check`、本change严格校验和全量严格OpenSpec校验，修复全部格式、交叉引用和规范错误
