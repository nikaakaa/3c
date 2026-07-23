# Tasks

## 1. 锁定前置模块

- [ ] 1.1 确认`add-animation-transition-routing-module`已经归档。
- [ ] 1.2 读取已安装Blend Logic合同。
- [ ] 1.3 读取已安装exact rule合同。
- [ ] 1.4 读取已安装Frame Input与Output合同。
- [ ] 1.5 读取已安装typed request合同。
- [ ] 1.6 读取已安装request lifecycle与generation合同。
- [ ] 1.7 读取已安装capture与release permission合同。
- [ ] 1.8 读取已安装snapshot与reason合同。
- [ ] 1.9 禁止在Character Animation目录复制模块类型。
- [ ] 1.10 禁止正式Runtime引用前置Editor Fixture。

## 2. 盘点现有接入点

- [ ] 2.1 盘点`CharacterAnimationBlendPolicy`当前schema。
- [ ] 2.2 盘点`CharacterPoseInertializationPolicy`当前schema。
- [ ] 2.3 盘点Blend Transition Projection payload。
- [ ] 2.4 盘点Inertialization Projection descriptor。
- [ ] 2.5 盘点Pose Plan workspace索引。
- [ ] 2.6 盘点Player Pose Discontinuity输出。
- [ ] 2.7 盘点BlendStack exact pair查找。
- [ ] 2.8 盘点BlendStack source capture与release。
- [ ] 2.9 盘点Inertialization history与rebase。
- [ ] 2.10 盘点Marker relation detach顺序。
- [ ] 2.11 盘点Timeline Preview的独立执行分支。
- [ ] 2.12 盘点Live snapshot的旧direct route字段。

## 3. 升级Blend Policy authoring

- [ ] 3.1 升级`CharacterAnimationBlendPolicy`schema version。
- [ ] 3.2 为Transition Rule引用模块Blend Logic。
- [ ] 3.3 保持Standard Blend Duration由Transition Rule拥有。
- [ ] 3.4 保持Standard Blend Curve由Transition Rule拥有。
- [ ] 3.5 保持Standard Blend Profile由Transition Rule拥有。
- [ ] 3.6 保持Inertialization Duration由Transition Rule拥有。
- [ ] 3.7 保持Inertialization Blend Profile由Transition Rule拥有。
- [ ] 3.8 禁止Inertialization target为Empty。
- [ ] 3.9 禁止Policy声明Custom。
- [ ] 3.10 禁止Policy声明独立HardCut。
- [ ] 3.11 将零时长Standard Blend显示为Hard Cut outcome。
- [ ] 3.12 将MaxActiveSourceEntries显示为Max Active Blends。
- [ ] 3.13 将Stored Pose policy显示为Store Blended Pose。
- [ ] 3.14 禁止Stored Pose进入Blend Logic下拉项。
- [ ] 3.15 保持完整exact pair coverage校验。

## 4. 收敛Inertialization Policy

- [ ] 4.1 升级`CharacterPoseInertializationPolicy`schema version。
- [ ] 4.2 删除直接Player source-target pair table。
- [ ] 4.3 删除Policy级HardCut业务选择。
- [ ] 4.4 保留consumer默认衰减配置。
- [ ] 4.5 保留consumer position residual限制。
- [ ] 4.6 保留consumer rotation residual限制。
- [ ] 4.7 保留consumer linear velocity限制。
- [ ] 4.8 保留consumer angular velocity限制。
- [ ] 4.9 保留consumer参数过滤配置。
- [ ] 4.10 保留consumer reset配置。
- [ ] 4.11 禁止Inertialization Policy重新决定Blend Logic。
- [ ] 4.12 禁止Inertialization Policy读取Selection业务类型。

## 5. 升级Presentation Projection schema

- [ ] 5.1 为Projection增加Routing Plan identity。
- [ ] 5.2 为Projection增加Routing Plan revision。
- [ ] 5.3 为Blend node descriptor增加compiled rule索引。
- [ ] 5.4 为Player descriptor增加routing owner索引。
- [ ] 5.5 为BlendStack descriptor增加routing owner索引。
- [ ] 5.6 为Inertialization descriptor增加consumer route索引。
- [ ] 5.7 为request route定义稳定identity。
- [ ] 5.8 为request route保存producer PoseNodeId。
- [ ] 5.9 为request route保存consumer PoseNodeId。
- [ ] 5.10 为request route保存branch scope。
- [ ] 5.11 为request route保存source map identity。
- [ ] 5.12 提升Projection ABI version。
- [ ] 5.13 拒绝旧ABI读取。
- [ ] 5.14 删除旧direct Player route payload。
- [ ] 5.15 删除重复endpoint matrix payload。

## 6. 编译角色Routing Plan

- [ ] 6.1 从Blend Policy降低模块Routing Definition。
- [ ] 6.2 从可达producer建立ordered endpoint catalog。
- [ ] 6.3 传入全部exact Transition Rule。
- [ ] 6.4 调用前置模块唯一Compiler。
- [ ] 6.5 把compiled Plan嵌入Projection。
- [ ] 6.6 校验Plan revision与Projection revision。
- [ ] 6.7 校验Plan endpoint与producer source map一致。
- [ ] 6.8 校验Empty endpoint覆盖。
- [ ] 6.9 校验Blend Profile identity可解析。
- [ ] 6.10 转发模块结构化compile reason。
- [ ] 6.11 禁止Projection Compiler复制exact rule算法。
- [ ] 6.12 禁止缺失rule时生成默认Standard Blend。

## 7. 编译静态request route

- [ ] 7.1 发现选择Inertialization的SelectedPosePlayer规则。
- [ ] 7.2 发现选择Inertialization的BlendSpacePlayer规则。
- [ ] 7.3 发现选择Inertialization的BlendStack规则。
- [ ] 7.4 从每个producer沿分支搜索显式consumer。
- [ ] 7.5 允许直接下游Inertialization。
- [ ] 7.6 定义允许透明经过的非Pose控制edge。
- [ ] 7.7 禁止跨Layered Blend Per Bone传播request。
- [ ] 7.8 禁止跨Apply Additive传播request。
- [ ] 7.9 禁止跨ModifyBone传播request。
- [ ] 7.10 禁止跨FootPlacement传播request。
- [ ] 7.11 禁止跨Output Pose传播request。
- [ ] 7.12 拒绝零consumer。
- [ ] 7.13 拒绝多个consumer。
- [ ] 7.14 拒绝route循环。
- [ ] 7.15 拒绝orphan Inertialization rule。
- [ ] 7.16 禁止运行时名称查找。
- [ ] 7.17 禁止全局request bus。
- [ ] 7.18 禁止Compiler自动插入consumer。

## 8. 扩展Pose Plan workspace

- [ ] 8.1 为每个routing owner分配模块workspace。
- [ ] 8.2 为每个route分配固定索引。
- [ ] 8.3 建立Frame Facts page。
- [ ] 8.4 建立Frame Output page。
- [ ] 8.5 建立request event page。
- [ ] 8.6 建立capture completion page。
- [ ] 8.7 建立release completion page。
- [ ] 8.8 建立reset reason page。
- [ ] 8.9 保持workspace容量由compiled Plan决定。
- [ ] 8.10 禁止每帧动态注册route。
- [ ] 8.11 禁止workspace保存第二份Pose history。
- [ ] 8.12 禁止workspace保存BlendStack entry副本。

## 9. 接入SelectedPosePlayer

- [ ] 9.1 从Selection变化构造current endpoint fact。
- [ ] 9.2 从Selection变化构造requested endpoint fact。
- [ ] 9.3 提交selection generation。
- [ ] 9.4 提交target readiness。
- [ ] 9.5 提交owner node identity。
- [ ] 9.6 调用模块Frame API。
- [ ] 9.7 对Standard Blend零时长结果执行现有直接切换。
- [ ] 9.8 对Inertialization结果发布route request。
- [ ] 9.9 保持Player不保存旧source entry。
- [ ] 9.10 保持Player不执行residual。
- [ ] 9.11 删除旧Player私有pair decision。

## 10. 接入BlendSpacePlayer

- [ ] 10.1 从source identity变化构造endpoint facts。
- [ ] 10.2 从BlendSpace selection generation构造generation fact。
- [ ] 10.3 保持source-local sample weight不进入Routing模块。
- [ ] 10.4 保持参数轴不进入Routing模块。
- [ ] 10.5 调用模块Frame API。
- [ ] 10.6 对Inertialization结果发布route request。
- [ ] 10.7 保持BlendSpacePlayer不拥有CrossFade entry。
- [ ] 10.8 保持BlendSpacePlayer不拥有Stored Pose。
- [ ] 10.9 保持BlendSpacePlayer不执行residual。
- [ ] 10.10 删除Discontinuity无条件触发惯性化的旧判断。

## 11. 接入BlendStack Standard Blend

- [ ] 11.1 用Stack current target构造current endpoint fact。
- [ ] 11.2 用新Selection构造requested endpoint fact。
- [ ] 11.3 调用模块Frame API。
- [ ] 11.4 对Standard Blend command查找现有entry。
- [ ] 11.5 对Standard Blend command创建新entry。
- [ ] 11.6 使用rule Duration。
- [ ] 11.7 使用rule Curve。
- [ ] 11.8 使用rule Blend Profile。
- [ ] 11.9 保持旧source持续采样。
- [ ] 11.10 保持Stored Pose容量压缩。
- [ ] 11.11 保持Per-Bone weight。
- [ ] 11.12 保持exact source usage。
- [ ] 11.13 对零时长结果执行统一entry release。
- [ ] 11.14 禁止Standard Blend发布request。

## 12. 接入BlendStack Inertialization

- [ ] 12.1 为新target建立Pending ownership。
- [ ] 12.2 在首份target sample前提交TargetReady为false。
- [ ] 12.3 target sample合法后提交TargetReady为true。
- [ ] 12.4 consumer capture plan准备后提交CapturePlanReady。
- [ ] 12.5 调用模块取得typed request。
- [ ] 12.6 把request写入compiled route page。
- [ ] 12.7 保持旧entry到capture completion。
- [ ] 12.8 capture completion后读取module release permission。
- [ ] 12.9 按release permission释放旧live entry。
- [ ] 12.10 按release permission释放旧Stored entry。
- [ ] 12.11 按release permission结束source retention。
- [ ] 12.12 按release permissiondetach Marker relation。
- [ ] 12.13 把release completion回报模块。
- [ ] 12.14 禁止BlendStack执行Pose residual。
- [ ] 12.15 禁止准备失败时退回Standard Blend。
- [ ] 12.16 禁止capture前释放旧source。

## 13. 接入Inertialization consumer

- [ ] 13.1 从route page读取typed request。
- [ ] 13.2 校验request owner identity。
- [ ] 13.3 校验request route identity。
- [ ] 13.4 校验request selection generation。
- [ ] 13.5 校验request generation。
- [ ] 13.6 读取上一份completed corrected Pose。
- [ ] 13.7 读取新target raw Pose。
- [ ] 13.8 使用现有速度估计。
- [ ] 13.9 使用现有TRS residual算法。
- [ ] 13.10 使用request Duration。
- [ ] 13.11 使用request Blend Profile identity。
- [ ] 13.12 应用consumer残差限制。
- [ ] 13.13 应用consumer参数过滤。
- [ ] 13.14 提交capture completion。
- [ ] 13.15 无request时只透传并更新history。
- [ ] 13.16 repeated request时执行现有rebase。
- [ ] 13.17 保持单一accumulator。
- [ ] 13.18 拒绝Invalid target Pose。
- [ ] 13.19 禁止读取BlendStack私有entry。
- [ ] 13.20 禁止根据endpoint变化猜测request。

## 14. 收口PlayableGraph completion顺序

- [ ] 14.1 固定selection与source plan阶段。
- [ ] 14.2 固定target playable准备阶段。
- [ ] 14.3 固定raw source Pose capture阶段。
- [ ] 14.4 固定BlendStack完成输出阶段。
- [ ] 14.5 固定Routing Frame Facts提交阶段。
- [ ] 14.6 固定request消费阶段。
- [ ] 14.7 固定Inertialization residual capture阶段。
- [ ] 14.8 固定Pose composition阶段。
- [ ] 14.9 固定FootPlacement阶段。
- [ ] 14.10 固定final publication阶段。
- [ ] 14.11 固定capture completion回报阶段。
- [ ] 14.12 固定source release阶段。
- [ ] 14.13 固定release completion回报阶段。
- [ ] 14.14 任一前置失败时阻止后续状态提交。
- [ ] 14.15 禁止第二次PlayableGraph evaluate。

## 15. 接入reset与seek

- [ ] 15.1 在Projection replacement时reset模块workspace。
- [ ] 15.2 在Pose Graph revision变化时reset模块workspace。
- [ ] 15.3 在preview seek时提交seek reset。
- [ ] 15.4 在playback generation变化时提交generation reset。
- [ ] 15.5 在source Retired时清理对应routing owner。
- [ ] 15.6 清理旧request route page。
- [ ] 15.7 清理旧capture completion。
- [ ] 15.8 清理旧release completion。
- [ ] 15.9 禁止reset后消费旧generation request。
- [ ] 15.10 禁止reset后恢复已release source。

## 16. 升级Authoring工作区

- [ ] 16.1 在Blend Policy Details显示Blend Logic。
- [ ] 16.2 显示Standard Blend。
- [ ] 16.3 显示Inertialization。
- [ ] 16.4 显示零时长Hard Cut outcome。
- [ ] 16.5 隐藏Custom。
- [ ] 16.6 把Stored Pose放入Stack容量区。
- [ ] 16.7 把Inertialization数学参数放入consumer节点Details。
- [ ] 16.8 显示exact rule coverage。
- [ ] 16.9 显示request producer与consumer。
- [ ] 16.10 显示route compile reason。
- [ ] 16.11 禁止字段修改自动Build。
- [ ] 16.12 禁止选中资产自动编译Projection。
- [ ] 16.13 保持所有重操作使用明确按钮。

## 17. 升级Pose Graph工作区

- [ ] 17.1 绘制typed request route。
- [ ] 17.2 区分request route与Pose edge。
- [ ] 17.3 高亮producer PoseNodeId。
- [ ] 17.4 高亮consumer PoseNodeId。
- [ ] 17.5 显示route scope。
- [ ] 17.6 显示compiled rule identity。
- [ ] 17.7 显示module lifecycle。
- [ ] 17.8 显示request generation。
- [ ] 17.9 显示capture completion。
- [ ] 17.10 显示release completion。
- [ ] 17.11 显示rebase count。
- [ ] 17.12 显示reset reason。
- [ ] 17.13 Stale revision时清空Live值。
- [ ] 17.14 禁止工作区伪造Live request。

## 18. 升级Timeline Preview

- [ ] 18.1 Preview加载正式compiled Routing Plan。
- [ ] 18.2 Preview分配正式模块workspace。
- [ ] 18.3 Preview复用Player Frame Facts。
- [ ] 18.4 Preview复用BlendStack Frame Facts。
- [ ] 18.5 Preview复用typed route。
- [ ] 18.6 Preview复用consumer capture。
- [ ] 18.7 Preview复用source release permission。
- [ ] 18.8 Preview复用request rebase。
- [ ] 18.9 非连续seek reset模块workspace。
- [ ] 18.10 preview generation替换时清理旧request。
- [ ] 18.11 禁止Preview创建简化dispatcher。
- [ ] 18.12 禁止Preview读取前置Editor Fixture。

## 19. 迁移Corin正式资产

- [ ] 19.1 锁定BaseLocomotion Selection NodeId。
- [ ] 19.2 锁定MarkerSync NodeId。
- [ ] 19.3 锁定BlendSpacePlayer NodeId。
- [ ] 19.4 锁定Locomotion Inertialization NodeId。
- [ ] 19.5 保持BaseLocomotion现有Pose路径。
- [ ] 19.6 为BaseLocomotion规则物化Routing Plan。
- [ ] 19.7 锁定FullBodyAction Selection NodeId。
- [ ] 19.8 锁定Action BlendStack NodeId。
- [ ] 19.9 新增Action Inertialization NodeId。
- [ ] 19.10 连接Action BlendStack Pose到Action Inertialization。
- [ ] 19.11 编译Action request route。
- [ ] 19.12 枚举全部当前Action producer identity。
- [ ] 19.13 枚举Empty endpoint。
- [ ] 19.14 物化完整Action exact pair matrix。
- [ ] 19.15 为每个pair显式选择Blend Logic。
- [ ] 19.16 禁止按Attack、Dodge显示名推断规则。
- [ ] 19.17 禁止创建不存在的Hit或Death endpoint。
- [ ] 19.18 连接Action consumer到Layered Blend Per Bone。
- [ ] 19.19 保持Pose Parameter Resolve顺序。
- [ ] 19.20 保持FootPlacement顺序。
- [ ] 19.21 原子重建Corin Projection。

## 20. 清理旧链

- [ ] 20.1 删除直接Player Inertialization pair matrix schema。
- [ ] 20.2 删除旧HardCut policy mode。
- [ ] 20.3 删除旧direct route descriptor。
- [ ] 20.4 删除旧route workspace。
- [ ] 20.5 删除旧endpoint decision代码。
- [ ] 20.6 删除旧diagnostic字段。
- [ ] 20.7 删除旧Preview decision分支。
- [ ] 20.8 删除无法进入Projection的重复Stored Pose字段。
- [ ] 20.9 删除旧Policy asset内容。
- [ ] 20.10 删除旧Projection资产内容。
- [ ] 20.11 搜索并删除全局request bus尝试。
- [ ] 20.12 搜索并删除运行时consumer查找。
- [ ] 20.13 搜索并删除自动consumer插入。
- [ ] 20.14 搜索并删除新旧Routing双写。
- [ ] 20.15 确认只有前置模块拥有Routing状态机。

## 21. 同步架构真相

- [ ] 21.1 更新`openspec/project.md`动画当前口径。
- [ ] 21.2 更新Selection Runtime术语。
- [ ] 21.3 更新Layer Runtime职责。
- [ ] 21.4 更新Presentation Authoring规则。
- [ ] 21.5 更新Pose Graph节点与route说明。
- [ ] 21.6 更新Timeline Preview说明。
- [ ] 21.7 更新BlendSpace active change的旧二选一口径。
- [ ] 21.8 更新Motion Matching active change的旧二选一口径。
- [ ] 21.9 更新Virtual Bone对capture/release的说明。
- [ ] 21.10 删除“BlendStack只支持CrossFade”的过期结论。
- [ ] 21.11 删除“Inertialization只能直接接单Player”的过期结论。
- [ ] 21.12 记录Fixture仍是模块诊断入口而非角色运行链。
