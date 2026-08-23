## 1. 前置与合同

- [ ] 1.1 确认`refactor-character-pose-constraint-transaction`已经完成、由用户验收并归档，当前Runtime只存在唯一Foot Placement Module、根Bank、Goal Assembler、FBBIK和Writer
- [ ] 1.2 对账归档后的current spec、Foot Placement Module Interface与Resolved Foot Result，确认本change不修改外部所有权和调用拓扑
- [ ] 1.3 固定本change唯一行为范围：Foot Contact Plan、空间Swing、非负Path、有限SupportDomain、Landing/Plant/Release与Landing腿Pelvis可达

## 2. 发布权威Foot Contact Plan

- [ ] 2.1 新增固定typed `AnimationFootContactPlanSample`及Artifact、Projection、Read Page与codec字段
- [ ] 2.2 让Started字段成为与Event绑定的保持事实，并让format/algorithm version覆盖onset与进度曲线
- [ ] 2.3 保留现有LandingPhase作为第一版Plant onset，把ReleasePhase到LiftOffPhase编译为单调Release计划
- [ ] 2.4 从完整sole下降轨迹生成独立LandingStarted与单调Landing Height曲线，Constraint和PlantConfidence只作为Editor分析证据
- [ ] 2.5 让Blend Space从同一Step/Event/Route的authoritative source原子选择整组Plan，禁止跨Event平均Trigger或Progress
- [ ] 2.6 扩展Projection Build质量校验，覆盖可达Clip、Dynamic Sample、同步关系、实际coverage、事件顺序、Landing窗口、Plant、Support与Release完整性

## 3. 替换Swing与Patch政策

- [ ] 3.1 新Swing Event入口从上一帧Final Sole捕获Swing Origin Sole
- [ ] 3.2 将Swing Progress改为Animated Sole在Swing Origin到Next Landing方向上的空间投影，Baseline与Envelope按同一进度采样
- [ ] 3.3 将Raw Swing Correction改为非负`Envelope - Baseline`，删除baseline height error与未来Landing高度分量
- [ ] 3.4 让Path Target变化只替换Target，唯一Effective Correction/Velocity使用临界阻尼追踪且不重启Duration
- [ ] 3.5 发布Stable/Rebasing/Unavailable跟踪事实，但禁止其决定Landing准入或拥有第二Output
- [ ] 3.6 从同Surface连续接触段生成有限平面胶囊SupportDomain及DomainIdentity
- [ ] 3.7 删除实时Path硬地面下限、Current Trace、第二高度来源和边界Clamp

## 4. 替换Landing、Locked、Release与Pelvis政策

- [ ] 4.1 LandingStarted时冻结Event/Path/Surface/Plane/Normal/SupportDomain并捕获一次LandingResidual
- [ ] 4.2 只按同Event LandingHeightProgress在SupportDomain内完成垂直交接，投影离域时判Patch失效
- [ ] 4.3 PlantStarted当帧从Current Effective Sole投影生成Anchor，准入失败进入UnlockedSupport并消费Event
- [ ] 4.4 Locked严格输出Anchor修正，删除Sliding水平削弱与8fc PlantConfidence Ownership
- [ ] 4.5 正常Release只按ReleaseStarted/ReleaseProgress衰减，Grounded丢失、Anchor超距与不可达只走Safety Release
- [ ] 4.6 让State Machine在Resolved Foot发布Landing Support Intent与Pelvis Reach Reference，使Pelvis从Landing开始同时约束上一支撑腿和Landing腿可达；Pelvis继续只读取Resolved Pair，不读取Foot State、Lock Response或Context
- [ ] 4.7 删除旧行为配置、Context字段和状态政策，不保留8fc开关或并行实现

## 5. Diagnostics与一致性

- [ ] 5.1 重写Diagnostics为Contact Plan、Swing Origin/Progress、Path Target/Tracking、SupportDomain、唯一Correction/Velocity、状态Trigger、Anchor、Pelvis与Goal/Solved/Physical残差
- [ ] 5.2 删除Constraint/PlantConfidence Runtime Ownership、实时硬地面下限、Sliding和兼容CSV/Gizmo字段
- [ ] 5.3 更新`openspec/project.md`为实际行为，并精确保留Heel/Toe、旋转、Reactive与移动平台未实现范围
- [ ] 5.4 使用规定参数编译Runtime与Editor工程，并在每次构建后立即关闭dotnet build server
- [ ] 5.5 执行`git diff --check`、本change严格校验和全量严格OpenSpec校验
