## 1. 固定依赖与唯一输入

- [ ] 1.1 确认`build-character-foot-motion-data-foundation`已经由用户验收并归档，记录正式Curve Catalog、Artifact format、algorithm version与Corin Registered Curve Hash
- [ ] 1.2 对账`refactor-character-pose-graph-architecture`最终Program Operation、Constraint Bank与Final Publication lineage，保持唯一Foot Module和根事务
- [ ] 1.3 固定Corin范围和TrainingEnemy禁区，确认本change不接管独立诊断change的Analyzer/Reporter所有权

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

## 4. 单独接入Step Time与Step Distance

- [ ] 4.1 用正式Step Time替换Landing Prediction时域、Current/Incoming选择和Future Body Translation请求时长
- [ ] 4.2 在Path瞬时Correction链连续后，用正式Step Time、LandingUpdateDistance和基础HalfLife计算唯一Swing Residual的Landing截止收敛
- [ ] 4.3 用正式Step Distance与Event table校验RootLocalLanding的同脚相邻事件和水平步长，不改变世界速度或地形查询数学
- [ ] 4.4 删除旧隐藏Step Time/Distance/Event消费者及其Projection字段，不保留双读或fallback
- [ ] 4.5 对账Raw Landing、Future Translation、Landing Event和Surface lineage诊断，阻止事件边界造成水平偏移

## 5. 单独接入Foot Height

- [ ] 5.1 用`Runtime Ground Envelope + Formal Foot Height`生成Swing沿Up目标，保持Foot XZ来自动画骨骼
- [ ] 5.2 删除旧`LandingConstraintWeight * BaselineHeightError`高度政策和对应旧输入
- [ ] 5.3 保持真实Envelope安全Clamp、Path Residual和Landing截止收敛只由State Context唯一拥有
- [ ] 5.4 发布Formal Foot Height、目标高度、最终Correction和Envelope clearance诊断事实

## 6. 单独接入Support与Pelvis

- [ ] 6.1 把正式Support写入Resolved Foot的Support Intent，并建立与Lock分离的Pelvis Reach Reference
- [ ] 6.2 让Primary Support按Support Intent、Event lineage和Reach Reference获取/保留，不读取Foot State或Lock Mode
- [ ] 6.3 让Pelvis消费正式Support Presence/Share并保持双脚都无Support时的typed Release
- [ ] 6.4 删除由旧Lock状态推导Support Weight、Intent和Eligibility的消费者，不把弱单侧Support归一成1

## 7. 闭合Landing腿可达

- [ ] 7.1 在Foot Motion Profile新增米制最小Landing腿压缩余量并纳入Profile revision和严格校验
- [ ] 7.2 让Resolved Foot发布Landing Reach Request，包含Event、世界Reference、腿长与最小压缩余量
- [ ] 7.3 让Pelvis Builder求Primary Support腿与Landing腿Reach区间交集，并限制Target与Spring Output
- [ ] 7.4 在Reach无交集时保持支撑腿安全、夹紧Landing Foot Goal、发布`LandingReachUnavailable`并禁止Full Lock
- [ ] 7.5 发布Target/Solved Extension Ratio、Compression Reserve、Reach区间、交集和Goal夹紧量诊断事实

## 8. 单独接入Contact与Lock

- [ ] 8.1 用正式Contact与Lock Mode建立同Event唯一Anchor并启动现有Landing状态
- [ ] 8.2 用正式Lock Weight驱动Acquire Residual消退，用Locked/Sliding Mode选择现有FullAnchor或Sliding Response
- [ ] 8.3 用正式Contact退出、Lock Mode与Weight驱动Releasing和完成，不建立第二状态机或第二Anchor
- [ ] 8.4 删除旧PlantConfidence、PlantCycleConsumed和Constraint Weight状态准入消费者及其Projection字段

## 9. 清理、构建与严格校验

- [ ] 9.1 删除全部旧Foot Motion Runtime payload、旧隐藏Feature reader、旧配置字段和失去消费者的诊断列
- [ ] 9.2 使用精确Corin Definition显式重建Presentation Projection、Float32 Program与Fixed Program，不修改TrainingEnemy
- [ ] 9.3 使用规定参数编译Runtime与Editor工程，并在每次构建后立即关闭dotnet build server
- [ ] 9.4 对封口诊断包重新生成facts/diagnosis，对账Path、Envelope、Landing Reach、Support、Goal、Solved和Physical阶段责任
- [ ] 9.5 执行`git diff --check`、本change严格校验和全量严格OpenSpec校验，清除旧spec冲突和失效任务引用
