## 1. 固定依赖与唯一输入

- [ ] 1.1 确认`build-character-foot-motion-data-foundation`已经由用户验收并归档，记录正式Curve Catalog、Artifact format、algorithm version与Corin Registered Curve Hash
- [ ] 1.2 固定Foot模块的typed输入、唯一Result与根事务边界，让`refactor-character-pose-graph-architecture`只消费不透明Constraint合同而不规定Foot内部布局
- [ ] 1.3 固定Corin范围和TrainingEnemy禁区，确认本change内只有一套Recorder、Analyzer与Publisher

## 2. 发布唯一Foot Motion Runtime Frame

- [ ] 2.1 扩展Projection Compiler，从正式AnimationClip Curve组和匹配Artifact事件降低唯一Foot Motion payload与稳定Landing Event table
- [ ] 2.2 让选中Live Animation Source按同Contribution、Cycle、Normalized Time和Completion采样左右正式Foot Motion Sample
- [ ] 2.3 把唯一typed Foot Motion Frame接入Foot Placement Pose Input，并严格校验Source与Contribution lineage
- [ ] 2.4 对缺失、重复、旧binding、Event不一致和非有限值发布typed invalid，不读取旧Artifact或默认值补全

## 3. 收口Path换代与Floor顺序

- [x] 3.1 让Releasing完成先更新为Swing，再执行同帧Swing Ground Envelope保护和最终输出分类
- [x] 3.2 让Path Revision只由Event、可用性、Landing端点或实际Swing目标的有效变化触发，不因identity单独变化每帧重置Residual
- [x] 3.3 在同Frame、Side与Event lineage下补齐Raw Landing/Path Target、Swing Target、Captured Residual、State Output、Safety Floor Output和Encoded Goal逐阶段事实
- [x] 3.4 用最新代表事件定位Correction首次不连续或放大的正式阶段，区分Target换代、Residual Capture、State所有权、Safety Floor与Goal编码责任
- [x] 3.5 拆分Accepted Swing Path Landing与Promoted Contact Landing所有权，消除Event交接帧错误的Path不可用，不用更短HalfLife、Step Time截止、Goal低通或Solver后处理掩盖同帧跳变
- [x] 3.6 把Swing硬Floor收敛为CurrentSwingFloor真实地面点相对Animated Sole的最低安全Correction，未来Path Envelope只服务连续Swing目标，并区分普通目标追踪与Safety Floor Clamp
- [x] 3.7 扩展正式诊断事实，记录Revision原因、逐阶段Correction、Envelope clearance和Releasing到Swing转换结果
- [x] 3.8 把采样包迁移为每Frame/Side唯一主行与独立Ground Path几何表，删除每个Contact/Envelope重复整套阶段列的旧展开行
- [x] 3.9 让停止、队列失败和自动路线统一进入后台Finalizing，排空Writer、封存双表并运行唯一Analyzer/Publisher后再发布结果
- [x] 3.10 为每脚建立根事务所有的Landing Observation Key、Committed/Pending Page与双页Pool，相同Key复用已提交Accepted或Rejected结果
- [x] 3.11 让新Observation Key只执行一次canonical SphereCast并删除PreferredSurfaceIdentity选择行为，保持5毫米Acceptance死区不变
- [x] 3.12 把Observation identity、World revision、cache state、query executed与canonical Raw Landing接入唯一facts/diagnosis链并删除Preferred旧口径

## 4. 拆分State、Transition、Interpolation与Hard Constraint

- [x] 4.1 对账当前每个Foot State、合法Transition边、Anchor命令、目标Correction、Residual/Progress、完成条件与Hard Constraint，固定迁移前业务映射
- [x] 4.2 把根Context拆成离散State、Contact/Anchor、统一Interpolation、Landing与Observation分型数据块，并保持一次Begin、Seal或Discard的唯一根事务
- [x] 4.3 实现纯`CharacterFootTransitionResolver`与固定typed Decision，显式区分输入驱动的Pre-Interpolation边和完成驱动的Post-Interpolation边
- [x] 4.4 实现唯一Transition Runtime，只允许它应用Decision、写离散State、执行Anchor Create/Retain/Release并发布Transition事实
- [x] 4.5 实现纯`CharacterFootStateTargetResolver`，按已确定State生成目标Correction、Reference、Contact/Support/Reach意图与typed Interpolation Request，不推进时间和Context
- [x] 4.6 实现唯一`CharacterFootInterpolationRuntime`，迁移Swing/Acquire/Release Residual、Contact Progress、HalfLife与Effective Correction，只保留一份统一Interpolation State和固定typed Policy
- [x] 4.7 把CurrentSwingFloor与Landing Reach放在Interpolation之后执行，禁止Hard Constraint回写State Target、Residual或Transition
- [x] 4.8 让Resolved Foot只消费Post-Transition、Post-Interpolation和Post-Constraint结果，并补齐Transition、Target、Interpolation与Constraint逐阶段事实
- [x] 4.9 删除旧`CharacterFootStateMachine`、旧分散Residual/Progress字段、重复Advance方法和全部兼容入口，确认State/Anchor与Effective Correction各自只有一个写入者

## 5. 单独接入Step Time与Step Distance

- [ ] 5.1 用正式Step Time替换Landing Prediction时域、Current/Incoming选择和Future Body Translation请求时长
- [ ] 5.2 在Path瞬时Correction链连续后，用正式Step Time、LandingUpdateDistance和基础HalfLife计算统一Interpolation State中Swing政策的Landing截止收敛
- [ ] 5.3 用正式Step Distance与Event table校验RootLocalLanding的同脚相邻事件和水平步长，不改变世界速度或地形查询数学
- [ ] 5.4 删除旧隐藏Step Time/Distance/Event消费者及其Projection字段，不保留双读或fallback
- [ ] 5.5 对账Raw Landing、Future Translation、Landing Event和Surface lineage诊断，阻止事件边界造成水平偏移

## 6. 单独接入Foot Height

- [ ] 6.1 用`Runtime Ground Envelope + Formal Foot Height`生成Swing沿Up目标，保持Foot XZ来自动画骨骼
- [ ] 6.2 删除由`LandingConstraintWeight`乘`BaselineHeightError`或`FormalTargetCorrection`的旧高度/目标政策和对应旧输入
- [ ] 6.3 保持真实Envelope安全Clamp只由Hard Constraint负责，Path Residual和Landing截止收敛只由Interpolation Runtime负责
- [ ] 6.4 发布Formal Foot Height、目标高度、最终Correction和Envelope clearance诊断事实

## 7. 单独接入Support与Pelvis

- [ ] 7.1 把正式Support写入Resolved Foot的Support Intent，并建立与Lock分离的Pelvis Reach Reference
- [ ] 7.2 让Primary Support按Support Intent、Event lineage和Reach Reference获取/保留，不读取Foot State或Lock Mode
- [ ] 7.3 让Pelvis消费正式Support Presence/Share并保持双脚都无Support时的typed Release
- [ ] 7.4 删除由旧Lock状态推导Support Weight、Intent和Eligibility的消费者，不把弱单侧Support归一成1

## 8. 闭合Landing腿可达

- [ ] 8.1 在Foot Motion Profile新增必须显式序列化的米制最小Landing腿压缩余量，纳入Profile revision和严格校验，缺失时typed invalid且不提供默认值
- [ ] 8.2 让State Target与Resolved Foot发布Landing Reach Request，包含Event、世界Reference、腿长与最小压缩余量
- [ ] 8.3 让Pelvis Builder求Primary Support腿与Landing腿Reach区间交集，并限制Target与Spring Output
- [ ] 8.4 在Reach无交集时保持支撑腿安全、夹紧Landing Foot Goal、发布`LandingReachUnavailable`并禁止Full Lock
- [ ] 8.5 发布Target/Solved Extension Ratio、Compression Reserve、Reach区间、交集和Goal夹紧量诊断事实

## 9. 单独接入Contact与Lock

- [ ] 9.1 用正式Contact与Lock Mode通过Pre-Interpolation Transition建立同Event唯一Anchor并进入Landing
- [ ] 9.2 用正式Lock Weight选择typed接管政策并驱动统一Interpolation State，用Locked/Sliding Mode选择FullAnchor或Sliding State Target
- [ ] 9.3 用正式Contact退出、Lock Mode与Weight产生Releasing Transition，并由Interpolation Completion产生Post-Interpolation `Releasing -> Swing`
- [ ] 9.4 删除旧PlantConfidence、PlantCycleConsumed和Constraint Weight状态准入消费者及其Projection字段

## 10. 清理、构建与严格校验

- [ ] 10.1 删除全部旧Foot Motion Runtime payload、旧隐藏Feature reader、旧配置字段和失去消费者的诊断列
- [ ] 10.2 使用精确Corin Definition显式重建Presentation Projection、Float32 Program与Fixed Program，不修改TrainingEnemy
- [ ] 10.3 使用规定参数编译Runtime与Editor工程，并在每次构建后立即关闭dotnet build server
- [ ] 10.4 对封口诊断包重新生成facts/diagnosis，对账Transition、Interpolation、Path、Envelope、Landing Reach、Support、Goal、Solved和Physical阶段责任
- [ ] 10.5 执行`git diff --check`、本change严格校验和全量严格OpenSpec校验，清除旧spec冲突和失效任务引用
