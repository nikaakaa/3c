## 1. 收口现状与跨change基线

- [x] 1.1 以active FinalIK change的Rig v4、Calibration v4、Foot Analysis artifact和Projection payload为唯一实现基线，列出本change需要提升的最终format、algorithm与Projection schema。
- [x] 1.2 重基线active Blend Space change的phase policy、plan、runtime与文档，删除把`MarkerSynchronizedPhase`固定等同线性步态同步的冲突口径。
- [x] 1.3 盘点全部Action AnimationTrack、Pose source binding与Blend Space sample的MarkerGroup owner，形成精确的Time Mapping迁移清单。
- [x] 1.4 盘点Locomotion Input Motion、MovingTurn Timeline Motion、Action playback与Sequence source retention的clock owner，删除把Motion channel等同Locomotion operation的冲突口径。
- [x] 1.5 对账active FinalIK change已增加的Start、End与MovingTurn Marker覆盖，删除本change中“这些资源没有Marker”的过期描述。

## 1A. 建立权威tick派生的Movement播放时钟

- [x] 1.6 新增目标数值后端无关的`CommittedMovementPlaybackClock`，只保存owner、generation、authority tick、continuous ticks与tick rate。
- [x] 1.7 让Fixed/Float Locomotion Input Motion在提交本tick motion delta时原子提交自身operation clock。
- [x] 1.8 让Fixed/Float Timeline Motion Curve以Timeline owner、activation generation与连续playhead原子提交Movement clock。
- [x] 1.9 让Motion Accumulator只转发获胜Contribution的clock，删除resolve后反查`LocomotionMotionElapsedTicks`和generation的路径。
- [x] 1.10 将clock随Body、Trajectory Intent与Presentation Fact committed sample传递，保持Action committed sample history独立。
- [x] 1.11 把Sequence clock source迁移为`CommittedMovement`，按精确owner与generation锁定、插值和重基线；retained outgoing source不得改绑新owner。
- [x] 1.12 明确rollback outer transaction只发布最终分支clock，Presentation workspace不进入snapshot/network；删除零值、presentation delta与旧Locomotion字段fallback。

## 2. 增加唯一同步作者合同

- [x] 2.1 在共享Marker Sync authoring合同增加`AnimationSyncTimeMapping`的`Unspecified`、`MarkerSegmentFraction`与`GeneratedFootPhase`。
- [x] 2.2 让`None`原子清空Time Mapping，让MarkerGroup强制保存明确策略，并拒绝两侧group或Time Mapping不一致的relation。
- [x] 2.3 把Timeline AnimationTrack、Profile Pose source、Blend Space authoring与Document v3投影接入同一typed字段和唯一Mutation/Undo链。
- [x] 2.4 更新Inspector、Timeline Marker lane、Pose source editor与Blend Space Details，使作者看到策略、artifact readiness和编译失败原因，不暴露generated warp payload。

## 3. 扩展现有Foot Analysis artifact

- [x] 3.1 从Analyzer已经完成的heel/toe/sole采样帧生成左右脚root-local sole平面位置、高度、局部速度与Plant Confidence同步描述，不重复采样Clip。
- [x] 3.2 为同步描述建立Editor-only不可变数据合同、canonical codec、identity、hash、format与algorithm版本。
- [x] 3.3 更新Artifact Store的Missing、Stale、Corrupt与原子发布校验，删除旧format reader和任何缺字段兼容路径。
- [x] 3.4 保持contact Marker candidate为Editor session瞬时建议，禁止把MarkerId、作者frame或world contact写入同步描述。
- [x] 3.5 更新Definition Build artifact resolver，使普通Runtime Foot Feature和Editor-only同步描述从同一精确artifact读取但进入不同Projection边界。

## 4. 编译确定性Foot Phase Time Warp

- [x] 4.1 新增Editor-only纯数据warp compiler，按精确leader/follower artifact与Marker occurrence构造双脚规范化特征序列。
- [x] 4.2 实现端点固定、索引单调、稳定tie-break和局部斜率约束的确定性序列对齐。
- [x] 4.3 将对齐路径确定性reduction为固定容量严格单调knot table，并在误差或容量超限时失败。
- [x] 4.4 为PoseState Transition的实际source/target与解析后leader方向编译relation-local plan。
- [x] 4.5 为AnimationSlot中明确选择`GeneratedFootPhase`的可达Action source pair编译精确relation plan。
- [x] 4.6 为Blend Space固定Phase Reference到每个DynamicCycle sample编译同格式plan，StationaryPose不进入warp。
- [x] 4.7 把plan identity、algorithm、artifact hash、source identity、marker pair、occurrence与knots编入Projection并加入严格payload validation。

## 5. 升级source-local运行时映射

- [x] 5.1 扩展`CharacterPoseStateSourceSyncPlan`与Action relation plan，使其只引用Projection中的显式Time Mapping和dense warp plan。
- [x] 5.2 把`MarkerSegmentTimeMapper`拆成共同segment/occurrence定位与策略化fraction求值，保留唯一relation cursor和cycle展开。
- [x] 5.3 让`MarkerSegmentFraction`继续执行明确线性映射，让`GeneratedFootPhase`只查编译knot table；删除无条件复制leader fraction的路径。
- [x] 5.4 保持Transition target Ready后立即开始Routing与blend，并在共同可见期间每帧持续更新follower effective clock。
- [x] 5.5 保持Finite occurrence选择、source retention、release completion与continuation anchor语义，禁止warp plan创建第二生命周期。
- [ ] 5.6 对missing plan、identity mismatch、missing occurrence、invalid knot与coverage exceeded发布稳定typed failure，不保留上一帧时间或切换策略。
- [x] 5.7 finite leader到达最后marker coverage时只提交一次终点映射，后续共同可见帧让follower从continuation anchor连续推进，避免冻结target sample。

## 6. 收口Blend Space相位计划

- [x] 6.1 将Blend Space phase policy收敛为`SharedNormalizedPhase`、`MarkerSegmentPhase`与`GeneratedFootPhase`，删除旧枚举兼容值。
- [x] 6.2 让固定Reference Sample发布canonical marker occurrence；每个Dynamic Sample按自己明确策略生成effective time。
- [x] 6.3 让`GeneratedFootPhase`复用Projection warp plan与共同查表函数，不在Blend Space runtime复制foot cost或对齐算法。
- [x] 6.4 保持参数权重变化不切换Phase Reference，并让Pose、Foot Feature与source contribution继续使用相同child effective time和weight。

## 7. 统一Preview与Diagnostics

- [x] 7.1 扩展Committed runtime snapshot，分别保存mapping policy、plan identity、leader fraction、warped follower fraction、occurrence与effective time。
- [ ] 7.2 让Pose Graph Preview、Timeline Preview、Blend Space Preview和Live Debug只读取正式Projection plan与Committed snapshot。
- [x] 7.3 更新source map和作者诊断，把artifact、marker topology、relation plan与失败原因定位到精确Track、Pose source、Transition或Blend Space Sample。
- [ ] 7.4 删除只显示单一segment fraction、从Animancer weight反推同步或在Preview现场编译warp的旧诊断路径。

## 8. 原子迁移正式内容

- [x] 8.1 将所有现有MarkerGroup owner显式迁移为`MarkerSegmentFraction`或`GeneratedFootPhase`，不得留下`Unspecified`或序列化默认值。
- [x] 8.2 将Corin Walk Loop与Run Loop的`Locomotion.Gait`迁移为`GeneratedFootPhase`，保留真实Cyclic marker frame、group与role。
- [x] 8.3 保持Corin Idle、Start、End、MovingTurn及Action的当前正式Marker authoring，只把具备精确生成计划的relation迁移为`GeneratedFootPhase`，不按资源类别推断策略。
- [x] 8.4 删除旧Foot Analysis artifact格式、旧Projection payload、旧Blend Space phase enum与无条件线性Locomotion映射代码。
- [x] 8.5 通过精确Corin Character Build发布匹配的新Foot Analysis artifact、Float32/Fixed Program、Presentation Projection与Native Pose产品，不自动全量构建。

## 9. 规范与项目口径收口

- [ ] 9.1 更新current specs与project.md的Marker Sync时间口径，明确Gameplay不等待Marker边界、Generated Foot Phase只决定source effective time。
- [ ] 9.2 对账active FinalIK、Blend Space与Motion Matching changes，删除冲突、重复或过期的线性脚步同步描述。
- [ ] 9.3 保持FootGrounding、PredictiveFootPlacementModifier、FullBodyIK、Transition Routing与Motion Matching owner数量和输入输出不变。
