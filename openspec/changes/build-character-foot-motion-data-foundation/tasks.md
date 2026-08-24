## 1. 恢复基线并固定数据阶段

- [x] 1.1 对账`bd5780a`架构与`8fc704a74ed3548c3357eff5c2d45f52d8366a4b`行为Oracle，确认唯一Module、Context、Goal、FBBIK和Writer边界
- [x] 1.2 删除失败实验引入的Foot Intent Runtime传播、状态政策、Pair Reach/Pelvis政策和生成产品，不保留开关、fallback或兼容reader
- [x] 1.3 恢复Corin基线产品和AnimationClip数据，清除实验Contact/Support曲线但保留正式`Gait Phase`与`Foot IK`
- [x] 1.4 固定本change不得向Projection发布新Runtime Foot payload，不得修改Foot逐帧行为

## 2. 建立短名Curve Catalog

- [x] 2.1 固定`Clip Curves`接收器及`Gait Phase / Foot IK`可见property，删除长Receiver类型和旧property binding
- [x] 2.2 为左右脚登记Step Time、Step Dist、Foot Height、Toe Height、Toe Speed、Pos Error、Rot Error、Contact、Lock Mode、Lock Weight与Support共22条channel
- [x] 2.3 为每条channel固定秒域、单位、值域、完整coverage、切线与离散边界规则
- [x] 2.4 让Clip identity、Registered Curve Hash、读取、替换、删除、Animation Window和Agent Document只使用唯一完整binding

## 3. 建立规范Raw Sampling与事件

- [x] 3.1 新增固定Raw Sample页，完整保存Root Motion及Hip/Knee/Ankle/Heel/Toe/Sole双空间姿态
- [x] 3.2 统一Root-local、Clip-motion、Calibration Up、Ground Reference与Sole Frame转换
- [x] 3.3 先完成全部位置采样，再用中心差分生成Toe/Sole速度和角速度
- [x] 3.4 从Toe/Heel/Sole与Ground Pose证据生成带滞回和最短持续时间的Contact区间
- [x] 3.5 合并循环首尾Contact段并生成左右脚Landing/LiftOff Event、ordinal与cycle关系

## 4. 生成Prediction与Foot Path数据

- [x] 4.1 为每个Sample生成绑定下一Landing Event的Step Time秒域曲线
- [x] 4.2 为每个Landing生成RootLocalLanding和MotionSpaceLanding，并从相邻同脚Landing生成StepVector/Step Distance
- [x] 4.3 让Step Distance在Event区间保持常量，并为循环跨界、有限Clip终点和零长度Step生成typed诊断
- [x] 4.4 从Animation Sole平面轨迹生成Foot Forward累计距离Progress
- [x] 4.5 生成Landing间Foot Path Baseline与非负Height Above Path，验证端点回零和完整coverage

## 5. 生成Filter、Lock与Support数据

- [x] 5.1 生成Toe Height与Toe Speed完整秒域曲线
- [x] 5.2 在Calibration Ground假设下生成Ground Pose Position/Rotation Error曲线
- [x] 5.3 从Toe/Heel/Sole与Pose Error证据生成Contact曲线，不复用旧PlantConfidence结果
- [x] 5.4 生成`Unlocked / Sliding / Locked`离散Lock Mode与连续Lock Weight，并应用滞回和最短持续时间
- [x] 5.5 从Pelvis投影、双脚Contact与Lock Weight生成独立Support曲线，验证单支撑、双支撑与空中区间

## 6. 候选、Apply与内容迁移

- [x] 6.1 新增包含完整lineage与左右22条Curve的session-local不可变候选
- [x] 6.2 在现有Foot Analysis作者表面同时显示Raw Evidence、Event、Step、Path、Lock和Support候选
- [x] 6.3 实现一个Undo事务内原子Apply全部22条曲线，任一Stale或非法曲线使整体写入失败
- [x] 6.4 为稳定Corin Direct Clip、Blend Space Sample和有限Action Clip显式生成并Apply正式曲线
- [x] 6.5 明确跳过TrainingEnemy，并删除旧长property、旧实验曲线和旧格式reader

## 7. Build与一致性

- [x] 7.1 扩展Projection Build数据质量校验和Registered Curve依赖，但不生成新Runtime payload
- [x] 7.2 更新Agent Document v4 curve catalog与Mutation闭包，保持完整Curve原子替换和owner权限
- [x] 7.3 更新`openspec/project.md`为实际AnimationClip数据链，并声明后续行为change逐项消费
- [x] 7.4 使用规定参数编译Runtime与Editor工程，并在每次构建后立即关闭dotnet build server
- [x] 7.5 执行`git diff --check`、本change严格校验和全量严格OpenSpec校验

## 8. 纠正In-place运动证据与承重语义

- [x] 8.1 为Analysis Source建立Target与Motion Reference的唯一显式配对，严格拒绝名称猜测、缺失配对和非Root通道差异
- [x] 8.2 把Motion Reference对象身份与Analysis Input Hash纳入Artifact、候选和Stale校验，Apply后Artifact保持Ready
- [x] 8.3 从Motion Reference真实Root骨骼生成可跨循环展开的Clip-motion位置、旋转、速度、Landing、Step Time/Dist与Foot Path
- [x] 8.4 按Loop与实际Root净位移禁止移动循环使用No Step或全0事件数据通过Apply，不预设Walk、Run、Grounded或Flight比例
- [x] 8.5 让Support只从动画脚高、垂直速度、腿姿态和骨盆投影生成，不再由Contact、Lock Mode或Lock Weight门控
- [x] 8.6 为全部Corin Target配置正式Motion Reference，重建并原子Apply22条Curve，确认Run/Walk双脚Event、Step、Contact、Lock与Support不再全0
- [x] 8.7 删除错误Artifact和旧全0曲线结果，保持Runtime Foot逐帧行为与TrainingEnemy不变

## 9. 收口WalkLoop锁定数据

- [x] 9.1 回放WalkLoop完整Raw Artifact并逐帧对账左右Contact、Sole平面速度、Pos/Rot Error、Reach、Lock Mode与Lock Weight
- [x] 9.2 让低速Sole证据建立动画Lock Anchor，并以速度阈值乘最短持续时间得到的累计平面漂移预算维持和退出Locked
- [x] 9.3 让Lock Mode与Lock Weight共享完成最短持续时间过滤后的同一Anchor事实，删除短Mode已移除但Weight仍满值的分裂结果
- [x] 9.4 升级Analyzer算法身份，仅重建并Apply Corin WalkLoop左右22条Curve
- [x] 9.5 对新WalkLoop Artifact与正式Curve执行逐帧闭环、规定参数编译、git diff检查和严格OpenSpec校验

## 10. 建立精确单Clip Bake事务与覆盖保护

- [x] 10.1 新增唯一Bake Session和`Empty / Same / Different / Partial` Diff模型，返回完整changed channel与稳定plan hash
- [x] 10.2 让Apply严格校验expected plan hash，默认拒绝已有差异，仅在显式Replace后原子写入并逐Key验证
- [x] 10.3 把Analysis Source Inspector迁移为Source+Target Analyze、只读Motion Reference、Diff与动作时Replace确认
- [x] 10.4 把Corin批处理迁移到同一Session，全部Analyze完成并一次确认后才允许写入任何Clip
- [x] 10.5 增加精确Assets路径的`character.foot_motion_bake` Unity工具，不读取Selection或猜测Target
- [x] 10.6 使用精确单Clip工具重建并Apply WalkLoop，完成数据闭环、编译、diff与严格OpenSpec校验

## 11. 完成Ground Pose与Support语义收口

- [x] 11.1 允许Motion Reference Root曲线在Unity原生最后一个Source Sample区间Clamp，只让有界收稳尾段延伸到第二个区间，并拒绝更长或仍明显运动的提前结束
- [x] 11.2 从Sole平地目标、Sole-Ankle局部关系、双骨段长度与作者膝弯平面生成确定性Ground Pose目标、Residual与Reach
- [x] 11.3 让Pos Error保存Ankle/Knee RMS修正加Residual，Rot Error保存Sole到Ground-aligned目标的Quaternion角度
- [x] 11.4 从Ground proximity、垂直稳定度、Root-local向下伸展和Hip-Ankle姿态生成绝对Support Candidate
- [x] 11.5 让Support输出`Presence × Share`，禁止单侧弱Candidate固定提升为1，并允许明确腾空左右为0
- [x] 11.6 增加Ground Pose、Landing、Lock与Support跨曲线语义Validator，明确区分Same与业务正确
- [x] 11.7 让有限Clip逐索引读取自身Raw Sample，并验证派生Animation Height不得在末帧绕回下一周期首帧
- [x] 11.8 升级Analyzer身份，使用唯一Bake Session重建并显式Replace全部Corin Target 22条Curve
- [x] 11.9 逐Clip审计Artifact与正式Curve，执行规定参数编译、Unity Console、git diff与全量严格OpenSpec校验
