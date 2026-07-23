# Tasks

## 1. 基线与删除清单

- [x] 1.1 枚举Blend Stack全部Inertial technique、rule、payload与runtime字段。
- [x] 1.2 枚举Inertial residual数学、history、workspace与job调用点。
- [x] 1.3 枚举MM、Timeline、Preview与Replay触发Inertial的入口。
- [x] 1.4 枚举Stack snapshot中的Inertial diagnostics字段。
- [x] 1.5 建立旧Stack Inertial与Projection字段删除清单。
- [x] 1.6 确认迁移期间不允许Stack与Node双写残差。

## 2. Pose Discontinuity合同

- [x] 2.1 定义PoseDiscontinuity schema与version。
- [x] 2.2 定义稳定EventIdentity。
- [x] 2.3 定义PreviousEndpoint与CurrentEndpoint。
- [x] 2.4 定义Previous/Current ContinuityIdentity。
- [x] 2.5 定义source jump原因。
- [x] 2.6 定义Reset reason与ResetSequence。
- [x] 2.7 禁止Discontinuity携带duration、curve、weight或旧Pose。
- [x] 2.8 让连续sample不产生新Discontinuity。

## 3. SelectedPosePlayer输出

- [x] 3.1 让Selection source identity变化发布Discontinuity。
- [x] 3.2 让MM generation jump发布Discontinuity。
- [x] 3.3 让连续Timeline sample保持同一continuity。
- [x] 3.4 让非连续Preview seek发布Reset。
- [x] 3.5 让branch replacement与Presentation Reset发布Reset。
- [x] 3.6 禁止Player计算Inertial residual或clock。
- [x] 3.7 将Pose与Discontinuity绑定到同一Player completion。

## 4. Inertialization作者节点

- [x] 4.1 新增CharacterPoseNodeKind.Inertialization。
- [x] 4.2 定义单Pose输入与单Pose输出。
- [x] 4.3 定义直接Player Discontinuity输入。
- [x] 4.4 定义稳定PoseNodeId。
- [x] 4.5 限制节点位于native Pose阶段。
- [x] 4.6 禁止节点位于FootPlacement之后。
- [x] 4.7 禁止跨Blend、Layered、Additive、ModifyBone或Subgraph隐式传播请求。

## 5. Inertialization Policy

- [x] 5.1 定义CharacterPoseInertializationPolicy identity与schema。
- [x] 5.2 定义HardCut与Inertialize exact rule。
- [x] 5.3 定义duration与canonical curve。
- [x] 5.4 定义dense per-bone Blend Profile。
- [x] 5.5 定义Pose Parameter Inertialize或Snap filter。
- [x] 5.6 枚举直接上游Player全部可达endpoint pair。
- [x] 5.7 将authoring default物化为完整exact table。
- [x] 5.8 拒绝duplicate、orphan、缺失pair与Rig mismatch。
- [x] 5.9 禁止Runtime fallback或按名称猜测。

## 6. History与残差Runtime

- [x] 6.1 定义上一份completed output双页history。
- [x] 6.2 保存dense local TRS与真实Presentation delta。
- [x] 6.3 计算linear、angular与scale velocity。
- [x] 6.4 迁移position与scale residual数学。
- [x] 6.5 迁移Quaternion最短弧Log/Exp数学。
- [x] 6.6 迁移velocity residual与curve derivative修正。
- [x] 6.7 迁移dense per-bone duration multiplier。
- [x] 6.8 定义单一Accumulator与generation。
- [x] 6.9 让首次合法Pose只建立history。
- [x] 6.10 让合法Discontinuity从上一completed output捕获。
- [x] 6.11 让连续中断从上一修正输出原子rebase。
- [x] 6.12 禁止Accumulator stack或恢复旧source。

## 7. Optional Pose、参数与Foot Feature

- [x] 7.1 定义Pose到Pose的合法Inertialize边界。
- [x] 7.2 让Pose到NoPose清理history并执行typed HardCut。
- [x] 7.3 让NoPose到Pose只建立新history。
- [x] 7.4 让Invalid传播并清理未提交Accumulator。
- [x] 7.5 让Reset清理history、clock与残差。
- [x] 7.6 让Inertialize参数按filter传播连续值。
- [x] 7.7 让Snap参数立即使用target值。
- [x] 7.8 按左右脚实际Bone envelope传播Foot Feature。
- [x] 7.9 禁止残差伪装成producer、clip或Gameplay contact。

## 8. Compiler、Job与生命周期

- [x] 8.1 编译Inertialization operation与workspace layout。
- [x] 8.2 校验Pose和Discontinuity来自同一Player identity。
- [x] 8.3 校验节点阶段与下游依赖。
- [x] 8.4 将runtime/job安装进唯一PlayableGraph Evaluate。
- [x] 8.5 原子提交input、history与output completion。
- [x] 8.6 让capture完成后立即释放旧source。
- [x] 8.7 禁止Accumulator持有source retention。
- [x] 8.8 让Dispose等待相关job并释放workspace。

## 9. 删除Stack Inertial

- [x] 9.1 删除AnimationBlendTechnique.Inertial。
- [x] 9.2 删除Blend Policy中的Inertial rule与override。
- [x] 9.3 删除Blend Stack Inertial accumulator。
- [x] 9.4 删除Blend Stack residual workspace与job分支。
- [x] 9.5 删除Stack Inertial contribution与snapshot字段。
- [x] 9.6 将旧数学实现移动到唯一PoseInertializationRuntime/Job。
- [x] 9.7 删除旧Projection Inertial payload与reader。
- [x] 9.8 删除旧serialized Inertial配置与兼容转换。

## 10. Preview、Diagnostics与资产

- [x] 10.1 让Timeline Preview执行同一节点capture/rebase。
- [x] 10.2 让MM Query Fixture执行同一节点实例。
- [x] 10.3 发布PoseNodeId与InputPlayerNodeId。
- [x] 10.4 发布Discontinuity与exact rule identity。
- [x] 10.5 发布Capture、Continue、Rebase、Complete与Reset状态。
- [x] 10.6 发布选定Bone residual与envelope。
- [x] 10.7 禁止Inspector重新采样或重算残差。
- [x] 10.8 创建Corin LocomotionInertialPolicy。
- [x] 10.9 将Corin BaseLocomotion改为SelectedPosePlayer到Inertialization。
- [x] 10.10 保持FullBodyAction按业务选择BlendStack或独立Inertialization。
- [x] 10.11 重建Profile、Pose Graph与Projection。

## 11. Active change与规格收口

- [x] 11.1 更新动画选择边界change的节点集与Corin目标图。
- [x] 11.2 更新Blend Stack change并删除Inertial owner口径。
- [x] 11.3 更新Pose Graph change的Inertialization节点合同。
- [x] 11.4 更新Motion Matching changes的推荐Player链。
- [x] 11.5 更新时间线Preview change的节点执行口径。
- [x] 11.6 更新openspec/project.md的当前动画表现职责。
- [x] 11.7 删除全部残留Stack Inertial正式配置与文档口径。
