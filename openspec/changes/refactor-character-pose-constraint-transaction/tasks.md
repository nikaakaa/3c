## 1. 固定8fc行为Oracle与重解释合同

- [x] 1.1 固定行为基准为`8fc704a74ed3548c3357eff5c2d45f52d8366a4b`，确认它只定义逐帧状态效果、公式、阈值、Anchor、Support、Pelvis与Goal，不定义新架构命名
- [x] 1.2 对账Landing Lifecycle、Swing Builder、Effective Constraint、Primary Support、Pelvis、Goal与FBBIK实际代码顺序和跨帧字段
- [x] 1.3 定义旧状态到`Swing/Landing/Locked/Releasing/UnlockedSupport`的确定映射，并把Sliding降为Locked内部Lock Response
- [x] 1.4 把新的Contact Plan、空间Swing、纯非负FootPath、SupportDomain、Sliding删除和Landing腿提前约束拆入后续`improve-character-foot-placement-behavior`
- [x] 1.5 对比current specs、`openspec/project.md`和两个active change，确认本change不修改Foot Analysis Artifact或动画事实

## 2. 建立唯一Pose Constraint根事务

- [ ] 2.1 让唯一PosePlanExecutionRuntime持有唯一CharacterPoseConstraintRuntime，正式Runtime与Preview复用同一构造路径
- [ ] 2.2 建立Frame/Completion/Rig lineage、双Bank和唯一Committed Bank identity
- [ ] 2.3 将左右Foot Context、Observation/Ground Path、Resolved Foot Pair、Primary Support/Pelvis、Goal Contribution/Goal Set、BendHistory/Solver Outcome和Diagnostics拆为预分配引用页并纳入根Bank
- [ ] 2.4 禁止运行方法按值传递完整Bank、Ground Path FixedList payload或Diagnostics聚合体，避免巨型参数ABI
- [ ] 2.5 建立BeginFrame、PrepareFootPlacement、AssembleGoals、SolveFullBodyIk、Validate、Seal/Discard与Invalidate窄Interface，删除外部可变Bank访问和逐模块Seal
- [ ] 2.6 对账Reset、Retarget、Pose discontinuity、Action硬抢占和Dispose，确保整个根Bank一次失效
- [ ] 2.7 限制根Runtime只实现阶段顺序、lineage、页所有权和事务生命周期，禁止Foot、Pelvis、Goal与Solver数学进入根Runtime

## 3. 建立Foot Context与基线状态映射

- [ ] 3.1 新增固定布局CharacterFootStateContext，集中保存Landing Lifecycle、PlantCycleConsumed、Path identity/point/residual、Contact Event/Anchor/Progress、Effective Correction、Acquire/Release Residual和Lock Response
- [ ] 3.2 新增CharacterFootStateMachine并成为Context唯一写入者；一次Evaluate最多转换一次顶层状态
- [ ] 3.3 实现None未消费到Swing、None已消费到UnlockedSupport、Acquiring到Landing、Locked/Sliding到Locked、Releasing到Releasing的确定映射
- [ ] 3.4 将8fc Sliding进入首帧保留Output与后续HalfLife追踪保存为Locked内部Lock Response事实，不建立第二状态机
- [ ] 3.5 保持PlantConfidence 0.5/0.75、LockDistance、SlideDistance、LandingUpdateDistance和EffectiveCorrectionHalfLife的原转换条件与比较顺序
- [ ] 3.6 保持Action硬失去所有权、PlantCycleConsumed、Release完成和Event消费的逐帧结果

## 4. 深化Foot Placement Module且保持8fc数学

- [ ] 4.1 建立唯一深CharacterFootPlacementModule Interface，外部只提交不可变Frame Input并读取一个Result
- [ ] 4.2 把Landing Prediction、Landing Lifecycle事实、Ground Path、Swing、Constraint、Support和Pelvis收为Implementation内部纯函数或内部Module，禁止独立Pending/Committed和Output Owner；World Query Adapter只发布不可变Observation，State Machine不得直接访问Unity查询对象
- [ ] 4.3 保持8fc Landing Prediction、同Event Landing更新死区、Previous/Next Landing晋升和Ground Path重建行为
- [ ] 4.4 原顺序迁入Phase SmoothStep、Baseline、Envelope按弧长采样、非负Envelope Floor与`LandingConstraintWeight * BaselineHeightError`公式
- [ ] 4.5 原顺序迁入Path Revision时Swing Residual捕获、HalfLife衰减、Output合成和RaiseToFloor
- [ ] 4.6 原顺序迁入Landing入口Anchor、Acquire Residual、Contact Progress、Locked FullAnchor、Sliding Response和Release移动Target残差
- [ ] 4.7 生成紧凑CharacterResolvedFootResult和Resolved Foot Pair，发布Final Sole/Ankle、Correction、Goal Weight、Contact Reference/Ownership、Support Eligibility/Weight/Intent/Error/Event、Pelvis Reach Reference与typed Outcome；State、Lock Response、Path和Residual只进入Diagnostics
- [ ] 4.8 删除CharacterFootLandingLifecycle、CharacterFootEffectiveConstraint和对外Route/Reducer/Builder浅链，不保留兼容调用路径

## 5. 迁移Primary Support与Pelvis但保持结果

- [ ] 5.1 让State Machine发布`None/RetainOnly/AcquireAndRetain` Support Eligibility，映射Swing/Landing/UnlockedSupport、Releasing与Locked的8fc支撑资格；重构阶段Support Intent等于Support Weight，Pelvis Reach Reference只复现现有Contact引用
- [ ] 5.2 让Primary Support只读取Eligibility、Support Weight、Horizontal Error、Event identity和Contact Reference，禁止读取Foot State、Lock Response或Context，并保持同权重选择顺序
- [ ] 5.3 让Stride与Pelvis只读取Primary Support Result和Resolved Pair的Final Sole、Pelvis Reach Reference与lineage，并保持8fc Primary Support缺失、Stride端点、支持腿可达区间、Target、Handoff和Spring公式
- [ ] 5.4 禁止Rebasing Proposal、Landing腿新约束或其它后续行为进入本change

## 6. 完成唯一Goal、FBBIK与Writer闭包

- [ ] 6.1 将Foot Placement与PoseBone来源改为独立固定typed Goal Contribution，不再复用GoalSet Header
- [ ] 6.2 新增唯一CharacterFullBodyIkGoalAssembler和固定Slot/lineage/重复贡献验证
- [ ] 6.3 修改Projection Compiler与拓扑Validator，要求一个Assembler、一个Goal Set、一个FBBIK、一个Output和一个Writer
- [ ] 6.4 拆分Contribution Workspace与唯一GoalSet Workspace，删除plural GoalSet input、passthrough/copy、Empty Goal兼容路径和旧端口字段
- [ ] 6.5 让Foot Goal Encoder只读取Resolved Effective Correction/Goal Weight，Pelvis Encoder只读取Pelvis Result；逐值编码8fc Goal并保持位置、权重、Slot和执行顺序
- [ ] 6.6 枚举Vendor FBBIK全部跨帧状态，将左右BendHistory、紧凑Solver Outcome和lineage迁入根Bank，并证明Committed历史可精确重建同一Stable/Applied Bend、初始化顺序与Update结果；无法证明时停止迁移
- [ ] 6.7 将Physical Writer拆成完整binding/pose预验证与唯一Apply，Apply成功后只允许no-throw根Bank发布

## 7. 深冻结Diagnostics并证明基线映射

- [ ] 7.1 在BeginFrame冻结Foot/FBBIK/Physical diagnostics interest与固定容量
- [ ] 7.2 从Pending Context、Observation、Resolved Result和后续Result单向深冻结Phase Progress、Baseline、Envelope、Swing Correction、Residual、Anchor、Contact Progress、Ownership、Support Eligibility/Weight、Pelvis与Goal/Solved/Physical结果
- [ ] 7.3 发布新五状态与Lock Response，同时确保每个新事实都能映射回8fc状态效果，不保留旧Runtime对象引用
- [ ] 7.4 让Physical Writer成功Apply时把实际Write Completion和Physical Bone位置写入同一Pending Diagnostics页
- [ ] 7.5 让Gizmo、CSV、Trace和Pose Watch只读取同Frame/Completion/Rig/Bank identity的Committed页，Diagnostics不得参与正式计算

## 8. 激进清理与最终一致性

- [ ] 8.1 删除旧Lifecycle、Effective Constraint、分散Pending/Committed、逐模块Seal、GoalSet兼容容器、外部Bank写入和运行Diagnostics依赖
- [ ] 8.2 搜索并消除第二Goal Set、第二FBBIK、第二Physical Writer、第二Foot状态写入者、第二Correction Owner和Pelvis旁路
- [ ] 8.3 搜索并消除Primary Support、Pelvis和Goal对Foot State、Lock Response、Context、Path或Residual的读取
- [ ] 8.4 确认代码、配置和Projection中不存在Contact Plan、Swing Origin、SupportDomain、纯非负FootPath或其它后续行为change内容
- [ ] 8.5 更新`openspec/project.md`为实际重构状态，并保留后续行为change尚未实施的准确说明
- [ ] 8.6 使用规定参数编译Runtime与Editor工程，并在每次构建后立即关闭dotnet build server
- [ ] 8.7 执行`git diff --check`、本change严格校验和全量严格OpenSpec校验
