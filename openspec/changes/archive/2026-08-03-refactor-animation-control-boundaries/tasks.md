# Tasks

## 唯一实施顺序

以下任务编号是稳定追踪ID，不表示实施先后。实施 MUST严格按下列阶段顺序推进，前一阶段未完整结束时不得开始后一阶段：

1. 合同、Compiler、Runtime、Editor与Preview代码：1–19、25、30–36、26.1–26.5。
2. 旧代码、旧ABI与旧运行路径清理：24、27.6–27.12、27.16–27.18。
3. 等待Pose Graph共享逻辑、Document v3、共享UI、Pose IR和Action Workspace全部完成。
4. 唯一Corin Document v3资产迁移：20–23、27.1–27.5、27.13–27.15、28，并与Pose Graph重构的资产任务合并执行。
5. 一次精确Definition正式Build：26.6–26.11。
6. 最终文档对账：29。

20–23、27.1–27.5、27.13–27.15与28在代码schema、Compiler、Runtime、Editor、Preview、旧路径清理、Pose Graph共享逻辑、Document v3、共享UI和Action Workspace全部完成前 MUST保持未完成。资产迁移 MUST通过一次正式BTSMTL Agent Document v3事务执行，不得由代码阶段提前写入最终资产，也不得与Pose Graph重构分别覆盖同一角色资产。26.6–26.11 MUST只在迁移、反向导出和同hash对账完成后执行，不得由选择资产、Inspector刷新或字段修改自动触发。总顺序以`openspec/character-pipeline-serial-execution.md`为准。

## 1. 锁定架构前置与冲突

- [x] 1.1 读取本change全部spec delta。
- [x] 1.2 读取current Selection Runtime合同。
- [x] 1.3 读取current Layer Runtime合同。
- [x] 1.4 读取current Pose Graph合同。
- [x] 1.5 读取current Timeline Animation合同。
- [x] 1.6 读取current Corin State Timeline合同。
- [x] 1.7 读取current Action Motion ownership合同。
- [x] 1.8 读取Transition Routing模块合同。
- [x] 1.9 确认旧`integrate-animation-transition-routing-pipeline`目录已经删除。
- [x] 1.10 盘点Blend Space active change的BaseLocomotion假设。
- [x] 1.11 盘点Motion Matching active change的channel winner假设。
- [x] 1.12 盘点Virtual Bone active change的Pose source假设。
- [x] 1.13 在实施前确认冲突active change的后续关系说明已更新。
- [x] 1.14 禁止恢复旧Routing任务19。

## 2. 盘点Corin Locomotion业务与表现数据

- [x] 2.1 枚举Corin Locomotion Gameplay States。
- [x] 2.2 枚举Locomotion State transitions。
- [x] 2.3 枚举Idle Timeline内容。
- [x] 2.4 枚举WalkStart Timeline内容。
- [x] 2.5 枚举WalkLoop Timeline内容。
- [x] 2.6 枚举RunStart Timeline内容。
- [x] 2.7 枚举RunLoop Timeline内容。
- [x] 2.8 枚举RunEnd Timeline内容。
- [x] 2.9 枚举MovingTurn Timeline内容。
- [x] 2.10 标记每条AnimationTrack。
- [x] 2.11 标记每条MotionCurveTrack。
- [x] 2.12 标记每条Window或TreeClip。
- [x] 2.13 标记每条Cue。
- [x] 2.14 标记每组Marker。
- [x] 2.15 标记每项Foot Analysis binding。
- [x] 2.16 标记`HasActionLocomotionOwnership`全部读写。
- [x] 2.17 标记ActionOverride全部入边和出边。
- [x] 2.18 标记BaseLocomotion全部Program producer。
- [x] 2.19 标记BaseLocomotion全部Projection binding。
- [x] 2.20 为每项旧数据指定唯一迁移owner或删除结论。

## 3. 定义Presentation Fact合同

- [x] 3.1 定义`PresentationFactId`。
- [x] 3.2 定义Fact value kind。
- [x] 3.3 定义Fact schema version。
- [x] 3.4 定义Frame identity。
- [x] 3.5 定义Simulation Tick identity。
- [x] 3.6 定义Presentation time。
- [x] 3.7 定义Grounded fact。
- [x] 3.8 定义HorizontalSpeed fact。
- [x] 3.9 定义HorizontalAcceleration fact。
- [x] 3.10 定义VerticalSpeed fact。
- [x] 3.11 定义MovementDirection fact。
- [x] 3.12 定义DesiredDirection fact。
- [x] 3.13 定义FacingError fact。
- [x] 3.14 定义MotionPhase fact。
- [x] 3.15 定义Body discontinuity generation。
- [x] 3.16 定义只读Fact page。
- [x] 3.17 定义缺失Fact失败原因。
- [x] 3.18 禁止Fact保存Animation资源。
- [x] 3.19 禁止Fact保存PoseState identity。
- [x] 3.20 禁止Fact保存mutable state address。

## 4. 构造Presentation Fact投影

- [x] 4.1 从committed Body读取Grounded。
- [x] 4.2 从committed Body读取velocity。
- [x] 4.3 计算水平速度。
- [x] 4.4 计算表现加速度。
- [x] 4.5 读取垂直速度。
- [x] 4.6 从committed Intent读取期望方向。
- [x] 4.7 从Body朝向计算FacingError。
- [x] 4.8 从唯一Motion结果映射MotionPhase。
- [x] 4.9 传播correction generation。
- [x] 4.10 使用Presentation interpolation构造frame。
- [x] 4.11 保持同Simulation Tick不重复Gameplay操作。
- [x] 4.12 把Fact frame接入Presentation Runtime输入。
- [x] 4.13 删除表现层对Gameplay mutable state的直接读取。

## 5. 增加Pose source binding

- [x] 5.1 定义Graph-owned typed Source Slot与Profile-owned typed Binding。
- [x] 5.2 定义Pose source binding schema。
- [x] 5.3 保存AnimationClip resource binding。
- [x] 5.4 保存Rig identity。
- [x] 5.5 保存loop capability。
- [x] 5.6 保存默认play rate。
- [x] 5.7 保存marker topology。
- [x] 5.8 保存ordered marker sequence。
- [x] 5.9 保存source-local Foot Placement Weight typed curve。
- [x] 5.10 保存Foot Analysis identity。
- [x] 5.11 把binding加入Animation Presentation Profile。
- [x] 5.12 增加重复source id校验。
- [x] 5.13 增加缺失resource校验。
- [x] 5.14 增加Rig mismatch校验。
- [x] 5.15 增加loop与marker topology校验。
- [x] 5.16 增加Foot Placement Weight curve校验。
- [x] 5.17 增加analysis identity校验。
- [x] 5.18 编译Pose source projection payload。
- [x] 5.19 提升Projection ABI version。
- [x] 5.20 拒绝旧ABI读取。
- [x] 5.21 为有限Action producer source binding保存明确Foot Analysis identity。
- [x] 5.22 分离Pose source与Action producer的artifact projection key。
- [x] 5.23 禁止两个source owner共享可写marker或curve对象。

## 6. 增加SequencePlayer作者合同

- [x] 6.1 增加SequencePlayer node kind。
- [x] 6.2 增加SequencePlayer node data。
- [x] 6.3 保存Pose source id。
- [x] 6.4 保存loop设置。
- [x] 6.5 保存play rate。
- [x] 6.6 保存initial time。
- [x] 6.7 保存reset-on-entry。
- [x] 6.8 定义Pose输出port。
- [x] 6.9 定义source discontinuity输出。
- [x] 6.10 增加节点序列化。
- [x] 6.11 增加节点clone。
- [x] 6.12 增加节点validation。
- [x] 6.13 增加Projection operation code。
- [x] 6.14 编译SequencePlayer descriptor。
- [x] 6.15 分配SequencePlayer workspace。

## 7. 实现SequencePlayer运行时

- [x] 7.1 从Projection解析source binding。
- [x] 7.2 创建正式source playable。
- [x] 7.3 初始化sample time。
- [x] 7.4 推进loop sample time。
- [x] 7.5 推进finite sample time。
- [x] 7.6 应用play rate。
- [x] 7.7 执行reset-on-entry。
- [x] 7.8 发布source discontinuity。
- [x] 7.9 发布source usage。
- [x] 7.10 在state release后释放playable。
- [x] 7.11 禁止创建Gameplay playback identity。
- [x] 7.12 禁止执行Timeline track。
- [x] 7.13 接入现有Animancer source backend。

## 8. 定义Pose Transition Rule

- [x] 8.1 定义Rule graph identity。
- [x] 8.2 定义Fact input operation。
- [x] 8.3 定义Bool literal。
- [x] 8.4 定义Float literal。
- [x] 8.5 定义Enum literal。
- [x] 8.6 定义Not operation。
- [x] 8.7 定义And operation。
- [x] 8.8 定义Or operation。
- [x] 8.9 定义Equal operation。
- [x] 8.10 定义NotEqual operation。
- [x] 8.11 定义Greater operation。
- [x] 8.12 定义GreaterOrEqual operation。
- [x] 8.13 定义Less operation。
- [x] 8.14 定义LessOrEqual operation。
- [x] 8.15 定义TimeInState input。
- [x] 8.16 定义StatePoseRemainingTime input。
- [x] 8.17 禁止副作用operation。
- [x] 8.18 禁止Gameplay state address。
- [x] 8.19 编译固定Rule operation span。
- [x] 8.20 增加type validation。
- [x] 8.21 增加cycle validation。
- [x] 8.22 增加唯一Bool output validation。

## 9. 定义PoseStateMachine作者合同

- [x] 9.1 增加PoseStateMachine node kind。
- [x] 9.2 定义StateMachine stable identity。
- [x] 9.3 定义Entry identity。
- [x] 9.4 定义Pose State identity。
- [x] 9.5 定义Transition identity。
- [x] 9.6 定义State Alias identity。
- [x] 9.7 定义State inline Pose subgraph。
- [x] 9.8 定义State唯一Pose output。
- [x] 9.9 定义Transition source。
- [x] 9.10 定义Transition target。
- [x] 9.11 定义Transition priority。
- [x] 9.12 定义Transition Rule引用。
- [x] 9.13 定义Blend Logic。
- [x] 9.14 定义duration。
- [x] 9.15 定义curve。
- [x] 9.16 定义Blend Profile。
- [x] 9.17 定义target reset policy。
- [x] 9.18 定义MaxTransitionsPerFrame。
- [x] 9.19 定义State Alias source集合。
- [x] 9.20 禁止Alias成为active state。
- [x] 9.21 增加authoring serialization。
- [x] 9.22 增加authoring clone。
- [x] 9.23 增加authoring validation。
- [x] 9.24 定义Transition State Source Sync模式。
- [x] 9.25 定义None模式。
- [x] 9.26 定义MarkerGroup模式。

## 10. 编译PoseStateMachine

- [x] 10.1 发现全部可达State。
- [x] 10.2 校验唯一Entry。
- [x] 10.3 校验Entry唯一target。
- [x] 10.4 校验State stable identity。
- [x] 10.5 编译每个State Pose subgraph。
- [x] 10.6 校验每个State唯一Pose output。
- [x] 10.7 展开State Alias source集合。
- [x] 10.8 校验Alias循环。
- [x] 10.9 建立ordered transition table。
- [x] 10.10 编译Transition Rule span。
- [x] 10.11 编译Transition Blend Logic。
- [x] 10.12 调用唯一Transition Routing compiler。
- [x] 10.13 建立state source usage plan。
- [x] 10.14 建立state relevance plan。
- [x] 10.15 分配fixed state workspace。
- [x] 10.16 分配fixed transition workspace。
- [x] 10.17 编译state source map。
- [x] 10.18 写入Projection descriptor。
- [x] 10.19 转发结构化compile reason。
- [x] 10.20 禁止自动插入State或Player。
- [x] 10.21 从source State解析Pose source marker binding。
- [x] 10.22 从target State解析Pose source marker binding。
- [x] 10.23 编译Transition Source Sync Plan。
- [x] 10.24 校验SyncGroup、topology与role。
- [x] 10.25 禁止复用Action MarkerSync relation state。

## 11. 实现PoseStateMachine运行时

- [x] 11.1 初始化Entry state。
- [x] 11.2 初始化active State workspace。
- [x] 11.3 更新TimeInState。
- [x] 11.4 更新StatePoseRemainingTime。
- [x] 11.5 求值active State有序Transition。
- [x] 11.6 按priority选择唯一Transition。
- [x] 11.7 遵守MaxTransitionsPerFrame。
- [x] 11.8 准备target State首Pose。
- [x] 11.9 提交Routing Frame Facts。
- [x] 11.10 执行Standard Blend。
- [x] 11.11 发布Inertialization request。
- [x] 11.12 处理capture completion。
- [x] 11.13 处理source release permission。
- [x] 11.14 完成active State切换。
- [x] 11.15 执行target reset policy。
- [x] 11.16 释放不再relevant的State source。
- [x] 11.17 处理同帧连续Transition。
- [x] 11.18 处理Fact generation reset。
- [x] 11.19 处理Projection replacement reset。
- [x] 11.20 禁止写回Gameplay。
- [x] 11.21 在State Pose采样前求值source sync。
- [x] 11.22 在共同可见期间持续更新effective time。
- [x] 11.23 transition release时建立continuation anchor。
- [x] 11.24 reset时清理state sync relation。

## 12. 定义Animation Slot合同

- [x] 12.1 增加AnimationSlot node kind。
- [x] 12.2 定义Slot stable identity。
- [x] 12.3 定义Source Pose输入。
- [x] 12.4 定义Action Playback输入。
- [x] 12.5 定义Pose输出。
- [x] 12.6 定义AnimationChannel binding。
- [x] 12.7 定义node-local Blend Policy。
- [x] 12.8 定义Slot允许无Action并透传`SourcePoseEndpoint`的语义。
- [x] 12.9 定义action player source usage。
- [x] 12.10 定义Slot BlendStack capacity。
- [x] 12.11 定义Slot routing owner。
- [x] 12.12 禁止Slot保存Bone Mask。
- [x] 12.13 禁止Slot保存Action admission。
- [x] 12.14 禁止Slot保存Motion policy。
- [x] 12.15 增加authoring serialization。
- [x] 12.16 增加authoring clone。
- [x] 12.17 增加authoring validation。

## 13. 编译Animation Slot

- [x] 13.1 解析Source Pose edge。
- [x] 13.2 解析Action channel binding。
- [x] 13.3 枚举可达Action producer。
- [x] 13.4 加入`SourcePoseEndpoint`并与`NoPose`分离。
- [x] 13.5 物化完整exact rule table。
- [x] 13.6 编译Action player descriptor。
- [x] 13.7 编译BlendStack workspace。
- [x] 13.8 编译Routing Plan。
- [x] 13.9 编译request route。
- [x] 13.10 编译source usage。
- [x] 13.11 编译release plan。
- [x] 13.12 写入Projection descriptor。
- [x] 13.13 拒绝未知Action producer。
- [x] 13.14 拒绝缺失exact rule。
- [x] 13.15 禁止生成隐藏Gameplay selector。

## 14. 实现Animation Slot运行时

- [x] 14.1 无Action时透传Source Pose。
- [x] 14.2 接收Action PendingFirstSample。
- [x] 14.3 保持Source Pose持续更新。
- [x] 14.4 首样本就绪后提交Routing Facts。
- [x] 14.5 执行Source到Action Standard Blend。
- [x] 14.6 发布Source到Action Inertialization request。
- [x] 14.7 采样当前Action Pose。
- [x] 14.8 处理Action到Action切换。
- [x] 14.9 处理Action到`SourcePoseEndpoint`的release。
- [x] 14.10 执行Action到当前Source Pose Standard Blend。
- [x] 14.11 发布Action到Source Pose Inertialization request。
- [x] 14.12 保持Stored Pose只属于capacity策略。
- [x] 14.13 按permission释放Action source。
- [x] 14.14 更新Slot source usage。
- [x] 14.15 更新Slot diagnostics。
- [x] 14.16 禁止Slot推进Timeline。
- [x] 14.17 禁止Slot提交Motion。

## 15. 接入唯一Transition Routing模块

- [x] 15.1 确认Routing模块实现与当前合同一致。
- [x] 15.2 引用前置模块正式Runtime程序集。
- [x] 15.3 禁止在Character Animation程序集复制模块合同。
- [x] 15.4 把PoseState transition降低为模块Frame Facts。
- [x] 15.5 把AnimationSlot handoff降低为模块Frame Facts。
- [x] 15.6 保留typed request合同。
- [x] 15.7 保留capture/release握手。
- [x] 15.8 保留branch-local Inertialization。
- [x] 15.9 删除旧direct Player pair decision。
- [x] 15.10 删除旧BlendStack私有route decision。
- [x] 15.11 接入模块reset与generation。
- [x] 15.12 接入模块snapshot与结构化reason。
- [x] 15.13 禁止正式Runtime引用Editor Fixture。

## 16. 重基线Blend Space与Motion Matching

- [x] 16.1 保留BlendSpace source-local混合。
- [x] 16.2 把BlendSpacePlayer接入PoseState subgraph。
- [x] 16.3 删除Blend Space对Gameplay BaseLocomotion winner的依赖。
- [x] 16.4 保留MM trajectory与query。
- [x] 16.5 保留MM search与plan。
- [x] 16.6 保留MM pose history。
- [x] 16.7 把MM demand改为PoseState relevance。
- [x] 16.8 把MM Selection输出接入State内部Player。
- [x] 16.9 删除MM的BTSMTL channel arbitration要求。
- [x] 16.10 保持MM不拥有Motion。
- [x] 16.11 保持MM不拥有transition算法。
- [x] 16.12 严格校验两个更新后的active change。

## 17. 升级Pose Graph工作区

- [x] 17.1 增加PoseStateMachine节点创建入口。
- [x] 17.2 增加SequencePlayer节点创建入口。
- [x] 17.3 增加AnimationSlot节点创建入口。
- [x] 17.4 增加StateMachine下钻页。
- [x] 17.5 增加State inline Pose下钻页。
- [x] 17.6 增加Transition Rule下钻页。
- [x] 17.7 增加State Alias作者UI。
- [x] 17.8 增加Transition priority UI。
- [x] 17.9 增加Blend Logic UI。
- [x] 17.10 增加target reset UI。
- [x] 17.11 增加Sequence source picker。
- [x] 17.12 增加Slot channel picker。
- [x] 17.13 增加Slot Blend Policy导航。
- [x] 17.14 增加compiled active state显示。
- [x] 17.15 增加compiled target state显示。
- [x] 17.16 增加transition progress显示。
- [x] 17.17 增加Slot playback显示。
- [x] 17.18 增加source usage显示。
- [x] 17.19 增加Routing lifecycle显示。
- [x] 17.20 禁止选择资产自动Build。
- [x] 17.21 禁止字段修改自动Build。
- [x] 17.22 增加Transition State Source Sync UI。
- [x] 17.23 显示compiled SyncGroup与leader/follower。

## 18. 升级Profile Inspector

- [x] 18.1 增加Pose source binding列表。
- [x] 18.2 增加stable source id显示。
- [x] 18.3 增加AnimationClip绑定。
- [x] 18.4 增加Rig摘要。
- [x] 18.5 增加loop capability。
- [x] 18.6 增加marker topology编辑。
- [x] 18.7 增加ordered marker编辑。
- [x] 18.8 增加Foot Analysis摘要。
- [x] 18.9 增加PoseState source导航。
- [x] 18.10 保留Action producer导航。
- [x] 18.11 区分Pose source与Action Timeline source。
- [x] 18.12 复用明确Projection Build按钮。
- [x] 18.13 禁止Inspector repaint触发Build。
- [x] 18.14 增加Pose source Foot Placement Weight curve编辑。
- [x] 18.15 保持Action Timeline曲线导航。
- [x] 18.16 禁止Pose source与Timeline Clip双写curve。
- [x] 18.17 增加Pose source Analysis工具入口。
- [x] 18.18 保持Action Clip Analysis导航到Timeline上下文。
- [x] 18.19 禁止Timeline Editor编辑Pose source marker。
- [x] 18.20 禁止Pose Graph Details复制source mutation。

## 19. 升级Preview与Diagnostics

- [x] 19.1 Preview构造正式Presentation Fact frame。
- [x] 19.2 Preview执行正式PoseStateMachine。
- [x] 19.3 Preview执行正式SequencePlayer。
- [x] 19.4 Preview执行正式Animation Slot。
- [x] 19.5 Preview执行正式Transition Routing。
- [x] 19.6 Preview seek resetState workspace。
- [x] 19.7 Preview seek resetSlot workspace。
- [x] 19.8 Live Debug显示active Pose State。
- [x] 19.9 Live Debug显示target Pose State。
- [x] 19.10 Live Debug显示Transition Rule结果。
- [x] 19.11 Live Debug显示Slot playback。
- [x] 19.12 Live Debug关联ActionInstance与playback。
- [x] 19.13 Live Debug区分Motion authority与Pose coverage。
- [x] 19.14 删除BaseLocomotion Selection旧字段。
- [x] 19.15 删除ActionOverride恢复动画旧字段。
- [x] 19.16 禁止Preview建立简化状态机。
- [x] 19.17 把Timeline Preview收窄为有限Action输入。
- [x] 19.18 把Locomotion Preview迁入Pose Graph Workspace。
- [x] 19.19 让两类Preview复用同一Projection与Pose Plan。
- [x] 19.20 Timeline Live Debug只解释Action playback relation。
- [x] 19.21 Pose Graph Live Debug解释PoseState source relation。
- [x] 19.22 增加跨工作区只读导航。
- [x] 19.23 禁止调试视图伪造PlaybackId或PoseState。

## 20. 迁移Corin表现资源

- [x] 20.1 为Idle创建Presentation Pose source binding。
- [x] 20.2 为WalkStart创建Presentation Pose source binding。
- [x] 20.3 为WalkLoop创建Presentation Pose source binding。
- [x] 20.4 为RunStart创建Presentation Pose source binding。
- [x] 20.5 为RunLoop创建Presentation Pose source binding。
- [x] 20.6 为RunEnd创建Presentation Pose source binding。
- [x] 20.7 为MovingTurn创建Presentation Pose source binding。
- [x] 20.8 迁移每个AnimationClip resource。
- [x] 20.9 迁移Locomotion.Gait marker。
- [x] 20.10 迁移finite marker。
- [x] 20.11 迁移Foot Placement Weight曲线。
- [x] 20.12 迁移Foot Analysis identity。
- [x] 20.13 校验全部Rig identity。
- [x] 20.14 删除Profile旧BaseLocomotion producer binding。

## 21. 构建Corin Locomotion PoseStateMachine

- [x] 21.1 创建Locomotion PoseStateMachine节点。
- [x] 21.2 创建Entry。
- [x] 21.3 创建Idle State。
- [x] 21.4 创建Start State。
- [x] 21.5 创建Locomotion State。
- [x] 21.6 创建Stop State。
- [x] 21.7 创建Turn State。
- [x] 21.8 为Idle接SequencePlayer。
- [x] 21.9 为Start接SequencePlayer。
- [x] 21.10 为Locomotion接BlendSpacePlayer或正式Sequence方案。
- [x] 21.11 为Stop接SequencePlayer。
- [x] 21.12 为Turn接SequencePlayer。
- [x] 21.13 配置Idle到Start Rule。
- [x] 21.14 配置Start到Locomotion Rule。
- [x] 21.15 配置Locomotion到Stop Rule。
- [x] 21.16 配置Stop到Idle Rule。
- [x] 21.17 配置Locomotion到Turn Rule。
- [x] 21.18 配置Turn到Locomotion或Idle Rule。
- [x] 21.19 配置Transition priority。
- [x] 21.20 配置每条Blend Logic。
- [x] 21.21 配置target reset policy。
- [x] 21.22 接入Locomotion Inertialization。

## 22. 构建Corin Action Slot拓扑

- [x] 22.1 创建FullBodyAction Playback Input。
- [x] 22.2 创建FullBodyAction Slot。
- [x] 22.3 把Locomotion Pose接入Source Pose。
- [x] 22.4 绑定FullBodyAction channel。
- [x] 22.5 绑定Action Blend Policy。
- [x] 22.6 枚举Attack1至Attack5 endpoint。
- [x] 22.7 枚举Dodge endpoint。
- [x] 22.8 加入当前Corin Slot endpoint配置，并显式包含`SourcePoseEndpoint`。
- [x] 22.9 配置完整exact Blend Logic。
- [x] 22.10 接入Action Inertialization。
- [x] 22.11 接入Pose Parameter Resolve。
- [x] 22.12 保持FootPlacement顺序。
- [x] 22.13 接入唯一Output Pose。

## 23. 迁移Corin Gameplay Locomotion

- [x] 23.1 从Locomotion图删除Idle动画Timeline playback。
- [x] 23.2 删除WalkStart动画Timeline playback。
- [x] 23.3 删除WalkLoop动画Timeline playback。
- [x] 23.4 删除RunStart动画Timeline playback。
- [x] 23.5 删除RunLoop动画Timeline playback。
- [x] 23.6 删除RunEnd动画Timeline playback。
- [x] 23.7 删除MovingTurn动画Timeline playback。
- [x] 23.8 删除ActionOverride State。
- [x] 23.9 删除ActionOverride全部入边。
- [x] 23.10 删除ActionOverride全部出边。
- [x] 23.11 删除`HasActionLocomotionOwnership`表现读取。
- [x] 23.12 保留真正Gameplay movement control。
- [x] 23.13 迁移真实Gameplay Motion curve。
- [x] 23.14 删除无消费Motion curve。
- [x] 23.15 接入正式Action/Motion arbitration。
- [x] 23.16 删除按动作结束恢复RunLoop逻辑。
- [x] 23.17 删除按动作结束恢复Idle逻辑。
- [x] 23.18 保持Action StateMachine与Timeline。
- [x] 23.19 保持Action Window与Cue。
- [x] 23.20 保持Action lifecycle与打断。

## 24. 删除旧BaseLocomotion Selection链

- [x] 24.1 删除BaseLocomotion Program producer contract。
- [x] 24.2 删除BaseLocomotion AnimationChannel binding。
- [x] 24.3 删除BaseLocomotion Selection Input。
- [x] 24.4 删除BaseLocomotion MarkerSync旧入口。
- [x] 24.5 删除BaseLocomotion SelectedPosePlayer旧入口。
- [x] 24.6 删除BaseLocomotion selection lifecycle state。
- [x] 24.7 删除BaseLocomotion playback retention。
- [x] 24.8 删除BaseLocomotion producer source map。
- [x] 24.9 删除BaseLocomotion Projection payload。
- [x] 24.10 删除BaseLocomotion Profile binding。
- [x] 24.11 删除BaseLocomotion runtime dictionary entry。
- [x] 24.12 删除BaseLocomotion diagnostics字段。
- [x] 24.13 删除BaseLocomotion Preview分支。
- [x] 24.14 搜索并删除旧producer显示名判断。
- [x] 24.15 禁止保留旧新配置开关。

## 25. 拆分动画表现协调与Action Playback运行时

- [x] 25.1 定义`CharacterAnimationPresentationRuntime`唯一整帧协调职责。
- [x] 25.2 定义`CharacterActionPlaybackRuntime`有限Action生命周期职责。
- [x] 25.3 定义协调器到Action Playback的ordered command输入。
- [x] 25.4 定义Action Playback到Slot的exact playback输出。
- [x] 25.5 定义Slot到Action Playback的source usage输入。
- [x] 25.6 定义Slot到Action Playback的release permission输入。
- [x] 25.7 定义Action Playback的command acknowledgement输出。
- [x] 25.8 定义Action Playback的exact retirement输出。
- [x] 25.9 把Presentation Fact frame提交迁入动画表现协调器。
- [x] 25.10 把Pose Runtime frame advance迁入动画表现协调器。
- [x] 25.11 把Pose Runtime唯一evaluate迁入动画表现协调器。
- [x] 25.12 把Pose request workspace所有权迁入动画表现协调器。
- [x] 25.13 把final publication事务迁入动画表现协调器。
- [x] 25.14 把有限Action command buffer迁入Action Playback Runtime。
- [x] 25.15 把有限Action lifecycle registry迁入Action Playback Runtime。
- [x] 25.16 把Action committed raw visual sample缓存迁入Action Playback Runtime。
- [x] 25.17 把Action sample demand迁入Action Playback Runtime。
- [x] 25.18 把Action retention与retirement迁入Action Playback Runtime。
- [x] 25.19 把Action lifecycle snapshot迁入Action Playback Runtime。
- [x] 25.20 将`AnimationPlaybackLifecycle`收窄并重命名为`ActionAnimationPlaybackLifecycle`。
- [x] 25.21 将对应Lifecycle command、phase与snapshot命名收窄为Action语义。
- [x] 25.22 禁止Action Playback推进Gameplay Timeline或自行累计权威visual time。
- [x] 25.23 禁止Action Playback调用Pose Runtime advance或evaluate。
- [x] 25.24 禁止Action Playback查询PoseStateMachine relevance。
- [x] 25.25 禁止Action Playback查询Motion Matching frame work。
- [x] 25.26 禁止Motion Matching state-local Selection创建Action playback identity。
- [x] 25.27 把Motion Matching relevance与请求协调迁入PoseState provider链。
- [x] 25.28 保持Action MarkerSync只消费Action exact source usage。
- [x] 25.29 保持PoseState Source Sync只消费state relevance。
- [x] 25.30 更新`CharacterSimulationPresentationRuntime`组合与调用入口。
- [x] 25.31 更新`CharacterPresentationRuntimeFactory`一次性装配。
- [x] 25.32 更新正式Timeline Preview复用动画表现协调器与Action Playback Runtime。
- [x] 25.33 更新Pose Graph Preview使无Action场景不创建虚假playback。
- [x] 25.34 分离Action lifecycle diagnostics与整帧Pose diagnostics。
- [x] 25.35 更新reset、Projection replacement与Dispose逆序清理。
- [x] 25.36 删除`CharacterAnimationPlaybackRuntime`旧类型。
- [x] 25.37 删除旧类型全部构造、字段、属性与调用引用。
- [x] 25.38 删除旧类型Preview转发入口。
- [x] 25.39 删除旧共享playback runtime文档和显示名。
- [x] 25.40 禁止保留旧类型兼容壳或adapter。

## 30. 拆分Action与state-local Pose source ABI

- [x] 30.1 定义`ActionAnimationPlaybackCommandKind`只包含Select、Sample、Complete与Release。
- [x] 30.2 定义`ActionAnimationPlaybackCommand`的EventId。
- [x] 30.3 定义`ActionAnimationPlaybackCommand`的AnimationPlaybackId。
- [x] 30.4 定义`ActionAnimationPlaybackCommand`的ActionInstanceId。
- [x] 30.5 定义`ActionAnimationPlaybackCommand`的AnimationChannelId。
- [x] 30.6 定义`ActionAnimationPlaybackCommand`的ProgramProducerId。
- [x] 30.7 定义`ActionAnimationPlaybackCommand`的generation。
- [x] 30.8 定义`ActionAnimationPlaybackCommand`的committed raw sample payload。
- [x] 30.9 定义`ActionAnimationPlaybackFrame`的exact Action identity。
- [x] 30.10 定义`ActionAnimationPlaybackFrame`的latest committed raw sample。
- [x] 30.11 定义`ActionAnimationPlaybackFrame`的lifecycle phase。
- [x] 30.12 定义`ActionAnimationPlaybackFrame`的source-local clip sample page。
- [x] 30.13 要求有限Action的ActionInstanceId非零。
- [x] 30.14 校验同一Playback的全部command保持相同ActionInstanceId。
- [x] 30.15 定义state-local`PresentationPoseSourceSample`。
- [x] 30.16 为state-local sample保存Projection-local dense source index、PlayerNodeId、generation与lease。
- [x] 30.17 为state-local sample保存provider与player identity。
- [x] 30.18 为state-local sample保存source generation。
- [x] 30.19 为state-local sample保存availability。
- [x] 30.20 为state-local sample保存raw与effective presentation sample。
- [x] 30.21 从state-local sample删除AnimationPlaybackId。
- [x] 30.22 从state-local sample删除AnimationChannelId。
- [x] 30.23 从state-local sample删除ProgramProducerIndex。
- [x] 30.24 把`PoseDiscontinuityEndpoint`改为source-neutral endpoint identity。
- [x] 30.25 删除Sequence PlayerIndex冒充ProgramProducerIndex的写法。
- [x] 30.26 把Action discontinuity endpoint与Presentation Pose source endpoint显式区分。
- [x] 30.27 编译独立`ActionPlaybackInputPlan`。
- [x] 30.28 编译独立`PoseStateSourceProviderPlan`。
- [x] 30.29 从通用Selection Input表移除ActionPlaybackInput。
- [x] 30.30 从通用Selection Input表移除MotionMatchingSelectionInput。
- [x] 30.31 删除旧通用`AnimationSelectionInput`作者类型。
- [x] 30.32 删除旧通用`AnimationSelectionInput`validator分支。
- [x] 30.33 删除旧通用`AnimationSelectionInput`compiler分支。
- [x] 30.34 删除旧通用`AnimationSelectionInput`Editor mutation。
- [x] 30.35 拆分`CharacterAnimationPresentationBindingIndex`。
- [x] 30.36 建立Action-only binding index。
- [x] 30.37 建立Pose source/provider binding index。
- [x] 30.38 让Action lifecycle只读取Action-only binding index。
- [x] 30.39 让PoseState/source runtime只读取Pose source/provider binding index。
- [x] 30.40 删除跨Action与Pose source共用的Selection frame resolver。

## 31. 重写Action command与逐Playback生命周期

- [x] 31.1 建立`ActionPlaybackCommandInbox`。
- [x] 31.2 让Inbox跨帧保存未提交Select command。
- [x] 31.3 让Inbox跨帧保存未提交Sample command。
- [x] 31.4 让Inbox跨帧保存未提交Complete command。
- [x] 31.5 让Inbox跨帧保存未提交Release command。
- [x] 31.6 按EventId校验command顺序。
- [x] 31.7 按producer与generation校验command归属。
- [x] 31.8 让Publish只写入Inbox。
- [x] 31.9 让Replace只替换Inbox中的目标command。
- [x] 31.10 让Retire只写入Inbox terminal command。
- [x] 31.11 禁止外部Publish直接修改live lifecycle。
- [x] 31.12 从Action command kind删除PoseRequest。
- [x] 31.13 从Action command kind删除PoseUnavailable。
- [x] 31.14 删除`DiscardFrameLocalPoseCommands`。
- [x] 31.15 把Pose request迁入`PresentationFrameWorkspace`。
- [x] 31.16 把Pose unavailable结果迁入`PresentationFrameWorkspace`。
- [x] 31.17 拆分旧`IAnimationPlaybackCommandSink`。
- [x] 31.18 拆分旧`IAnimationPlaybackBatchSource`。
- [x] 31.19 定义Inbox begin-read lease。
- [x] 31.20 定义Inbox commit acknowledgement。
- [x] 31.21 定义Inbox rollback。
- [x] 31.22 建立`ActionAnimationPlaybackLifecycleRegistry`。
- [x] 31.23 让Registry按完整AnimationPlaybackId保存entry。
- [x] 31.24 在entry保存ActionInstanceId。
- [x] 31.25 在entry保存producer、channel与generation。
- [x] 31.26 在entry保存latest EventId。
- [x] 31.27 在entry保存first-sample readiness。
- [x] 31.28 在entry保存logic terminal。
- [x] 31.29 在entry保存Slot usage set。
- [x] 31.30 在entry保存retirement permission。
- [x] 31.31 在entry保存backend release request。
- [x] 31.32 在entry保存backend release completion。
- [x] 31.33 实现PendingFirstSample到Selected转换。
- [x] 31.34 实现Selected到Retained转换。
- [x] 31.35 实现Retained到RetirementPermitted转换。
- [x] 31.36 实现backend completion后到Retired转换。
- [x] 31.37 让Complete建立terminal watermark。
- [x] 31.38 让Release建立terminal tombstone。
- [x] 31.39 让Replace服从EventId与terminal顺序。
- [x] 31.40 让Lifecycle snapshot枚举全部registry entry。
- [x] 31.41 删除按channel只枚举当前winner的snapshot。
- [x] 31.42 删除Lifecycle对`AnimationPosePlayableGraphRuntime`类型的依赖。
- [x] 31.43 删除Lifecycle调用Pose Runtime publish selection。
- [x] 31.44 删除Lifecycle调用Pose Runtime retention查询。
- [x] 31.45 删除Lifecycle调用Pose Runtime playback status查询。
- [x] 31.46 删除Lifecycle调用Pose Runtime handoff source查询。

## 32. 建立Action usage与精确释放握手

- [x] 32.1 定义Action-only Slot usage entry。
- [x] 32.2 在usage entry保存SlotId。
- [x] 32.3 在usage entry保存ActionPlaybackId。
- [x] 32.4 在usage entry保存usage kind。
- [x] 32.5 在usage entry保存completion identity。
- [x] 32.6 从Action usage batch排除Sequence source。
- [x] 32.7 从Action usage batch排除BlendSpace source。
- [x] 32.8 从Action usage batch排除Motion Matching source。
- [x] 32.9 聚合同一Action的多个exact consumer。
- [x] 32.10 只有全部Action usage消失后才能提交retirement permission。
- [x] 32.11 定义Action backend release request identity。
- [x] 32.12 在release request保存完整source set。
- [x] 32.13 让Physical Pose Source Registry消费release request。
- [x] 32.14 定义逐source backend release completion。
- [x] 32.15 聚合同一request的全部source completion。
- [x] 32.16 禁止任意单个source release触发Action Retired。
- [x] 32.17 让Action registry只在完整completion后提交Retired。
- [x] 32.18 删除Pose Runtime反查RetainsPlayback路径。
- [x] 32.19 删除按channel扫描retained player路径。
- [x] 32.20 删除旧`PruneUnreferencedSampling`退休推断。
- [x] 32.21 把Action source物理释放发起权迁入动画表现协调器。
- [x] 32.22 把Action source物理释放完成回执提交给Action Runtime。
- [x] 32.23 让Stored Pose capture资源加入对应release request。
- [x] 32.24 让Action Marker relation等待对应source release completion。
- [x] 32.25 让Action到Action handoff使用Slot exact outgoing与incoming usage。
- [x] 32.26 让Action到Source Pose handoff使用`SourcePoseEndpoint`。
- [x] 32.27 从release snapshot删除模糊的`releasedAny`结果。
- [x] 32.28 发布带request identity的release diagnostics。

## 33. 拆分Action committed时间与表现采样时间

- [x] 33.1 从旧`AnimationSamplingState`提取`ActionCommittedSampleHistory`。
- [x] 33.2 让History只保存committed Timeline sample。
- [x] 33.3 让History按EventId替换目标sample。
- [x] 33.4 让History按playback identity分区。
- [x] 33.5 让History拥有确定的sample pruning规则。
- [x] 33.6 从History删除Marker effective cursor。
- [x] 33.7 从History删除render-frame advance。
- [x] 33.8 建立`ActionPresentationSampleProjector`。
- [x] 33.9 实现两个committed sample之间的表现插值。
- [x] 33.10 保存`ProjectedPresentationSampleTime`独立字段。
- [x] 33.11 禁止projected time覆盖`CommittedRawVisualTime`。
- [x] 33.12 实现Retained Action的animation-only时间投影。
- [x] 33.13 让finite source投影钳制在合法coverage。
- [x] 33.13a 把finite source最后合法采样时间编译进Projection binding并提升ABI。
- [x] 33.13b 让Action与Retained Action只消费编译后的合法采样边界，删除运行时尾帧重试。
- [x] 33.13c 让Action到`SourcePoseEndpoint`只保留blend-out source，不生成Action marker relation。
- [x] 33.14 让cyclic source投影保持展开cycle。
- [x] 33.15 禁止表现投影推进Gameplay Timeline。
- [x] 33.16 禁止表现投影产生Window、Motion或Cue。
- [x] 33.17 建立`ActionMarkerEffectiveSampleState`。
- [x] 33.18 把Marker raw-to-effective映射迁入Marker state。
- [x] 33.19 把Marker continuation anchor迁入Marker state。
- [x] 33.20 把Marker rebase迁入Marker state。
- [x] 33.21 分离Action Marker registry与PoseState Source Sync registry。
- [x] 33.22 提取两类Marker registry共用的marker segment数学。
- [x] 33.23 禁止PoseState Source Sync依赖Action lifecycle phase。
- [x] 33.24 更新diagnostics区分committed raw、projected raw与effective time。

## 34. 建立统一动画表现帧事务

- [x] 34.1 定义有界`AnimationPresentationFrameTransaction`。
- [x] 34.2 为Action inbox读取建立staged state。
- [x] 34.3 为Action registry mutation建立staged state。
- [x] 34.4 为Action sample projector建立staged cursor。
- [x] 34.5 为Action Marker state建立staged cursor。
- [x] 34.6 为PoseState workspace建立staged state。
- [x] 34.7 为source provider demand与result建立staged page。
- [x] 34.8 为AnimationSlot workspace建立staged state。
- [x] 34.9 为Transition Routing workspace建立staged state。
- [x] 34.10 为source usage建立staged batch。
- [x] 34.11 为release request与completion建立staged batch。
- [x] 34.12 在唯一Pose Plan成功后提交Action inbox acknowledgement。
- [x] 34.13 在唯一Pose Plan成功后提交Action lifecycle。
- [x] 34.14 在唯一Pose Plan成功后提交sample与Marker cursor。
- [x] 34.15 在唯一Pose Plan成功后提交Slot与Transition state。
- [x] 34.16 在唯一Pose Plan成功后提交retirement。
- [x] 34.17 在唯一Pose Plan成功后发布diagnostics。
- [x] 34.18 在唯一Pose Plan成功后发布FinalAnimationPoseFrame。
- [x] 34.19 Evaluate失败时回滚Action inbox读取。
- [x] 34.20 Evaluate失败时回滚Action lifecycle mutation。
- [x] 34.21 Evaluate失败时回滚sample与Marker cursor。
- [x] 34.22 Evaluate失败时回滚PoseState、Slot与Transition state。
- [x] 34.23 Evaluate失败时禁止发布source release completion。
- [x] 34.24 Evaluate失败时禁止发布部分Final Pose。
- [x] 34.25 为Selection sequence定义独立identity domain。
- [x] 34.26 为Pose request定义独立identity domain。
- [x] 34.27 为source continuity定义独立identity domain。
- [x] 34.28 为workspace completion定义独立identity domain。
- [x] 34.29 为player usage completion定义独立identity domain。
- [x] 34.30 把每个allocator迁入对应Module。
- [x] 34.31 禁止跨identity domain比较裸序号。

## 35. 拆分Pose运行Module并安装readiness barrier

- [x] 35.1 定义`PoseStateAndSourceRuntime`职责。
- [x] 35.2 定义`AnimationSlotRuntime`职责。
- [x] 35.3 定义`PosePlanExecutionRuntime`职责。
- [x] 35.4 定义`PhysicalPoseSourceRegistry`职责。
- [x] 35.5 从`AnimationPosePlayableGraphRuntime`迁出PoseState控制状态。
- [x] 35.6 从`AnimationPosePlayableGraphRuntime`迁出source provider relevance。
- [x] 35.7 从`AnimationPosePlayableGraphRuntime`迁出AnimationSlot route状态。
- [x] 35.8 从`AnimationPosePlayableGraphRuntime`迁出physical source lifecycle。
- [x] 35.9 让Pose Plan执行Module只装载native plan与workspace。
- [x] 35.10 删除Pose Runtime的`CollectRetainedPlaybackDemand`。
- [x] 35.11 删除Pose Runtime的`CollectRetainedSourceUsages` Action路径。
- [x] 35.12 删除Pose Runtime的`RetainsPlayback`。
- [x] 35.13 删除Pose Runtime的`TryGetPlaybackStatus`。
- [x] 35.14 删除Pose Runtime的`TryGetHandoffSource`。
- [x] 35.15 删除Pose Runtime按channel的`PublishSelection`。
- [x] 35.16 删除Pose Runtime按channel的`PublishEmptySelection`。
- [x] 35.17 删除Pose Runtime按channel的`PublishUnavailableSelection`。
- [x] 35.18 删除协调器按channel扫描Player的路由。
- [x] 35.19 让Action frame按compiled ActionPlaybackInput精确路由到Slot Action Player。
- [x] 35.20 让PoseState发布active source provider demand。
- [x] 35.21 让PoseState发布target source provider demand。
- [x] 35.22 定义provider `Pending`结果。
- [x] 35.23 定义provider `Ready`结果。
- [x] 35.24 定义provider `Invalid`结果。
- [x] 35.25 Entry required source Pending时阻止Final Pose发布。
- [x] 35.26 target source Pending时保持当前合法source。
- [x] 35.27 target source Pending时禁止提交transition generation。
- [x] 35.28 target source Ready后提交Transition Routing。
- [x] 35.29 source Invalid时发布typed failure。
- [x] 35.30 source Invalid时禁止恢复历史Selection或bind pose。
- [x] 35.31 把完整Transition Routing plan编入Projection。
- [x] 35.32 把Slot exact endpoint matrix编入Projection。
- [x] 35.33 把capture/release request layout编入Projection。
- [x] 35.34 让Runtime只装载并校验Routing PlanId与Revision。
- [x] 35.35 删除角色Runtime调用`TransitionRoutingCompiler.Compile`。
- [x] 35.36 删除Slot Runtime调用`TransitionRoutingCompiler.Compile`。
- [x] 35.37 把Slot无Action占用序列化为`SourcePoseEndpoint`。
- [x] 35.38 定义`NoPose`并与`SourcePoseEndpoint`分离。
- [x] 35.39 更新Slot route snapshot删除旧`Empty`显示。

## 36. 收口启动、Reset、Authoring、Preview与Diagnostics

- [x] 36.1 删除`RequireCommittedSelection`启动策略。
- [x] 36.2 删除`AwaitCommittedSelection`启动策略。
- [x] 36.3 删除Action Selection语义的`HasRequiredOutput`。
- [x] 36.4 定义Pose Plan startup readiness。
- [x] 36.5 让startup readiness检查committed Body与Fact。
- [x] 36.6 让startup readiness检查Projection与Pose Plan。
- [x] 36.7 让startup readiness检查Entry PoseState。
- [x] 36.8 让startup readiness检查Required Pose source readiness。
- [x] 36.9 更新Factory本地角色启动装配。
- [x] 36.10 更新Factory模拟角色启动装配。
- [x] 36.11 更新Factory观察角色启动装配。
- [x] 36.12 禁止无Action启动创建空playback。
- [x] 36.13 定义PresentationReset清理矩阵。
- [x] 36.14 定义BodyDiscontinuity重基线矩阵。
- [x] 36.15 定义ActionCommandReplace局部替换矩阵。
- [x] 36.16 定义PreviewSeek清理矩阵。
- [x] 36.17 定义ProjectionReplacement逆序销毁矩阵。
- [x] 36.18 把PoseState inline graph存储改为root-owned graph catalog。
- [x] 36.19 定义stable PoseGraphId。
- [x] 36.20 让PoseState只保存PoseGraphId引用。
- [x] 36.21 删除PoseState递归内联`CharacterPoseGraphData`序列化。
- [x] 36.22 更新Pose Graph Editor按GraphId导航。
- [x] 36.23 更新Pose Graph Undo按GraphId记录。
- [x] 36.24 更新validator遍历root-owned catalog。
- [x] 36.25 更新compiler遍历root-owned catalog。
- [x] 36.26 更新source map记录GraphId。
- [x] 36.27 校验Pose subgraph递归调用。
- [x] 36.28 把Action producer schema收窄为有限Timeline Action。
- [x] 36.29 删除Motion Matching producer authoring kind。
- [x] 36.30 删除Blend Space producer authoring kind。
- [x] 36.31 删除authoring service创建Gameplay MM producer的mutation。
- [x] 36.32 删除authoring service创建Gameplay Blend Space producer的mutation。
- [x] 36.33 更新Profile Inspector只编辑有限Action producer binding。
- [x] 36.34 让MM与Blend Space只使用Pose source/provider binding。
- [x] 36.35 建立统一`AnimationPreviewRuntime`。
- [x] 36.36 建立`TimelineActionPreviewAdapter`。
- [x] 36.37 让Timeline Preview创建session-scoped非零ActionInstance。
- [x] 36.38 让Timeline Preview通过Action inbox提交command。
- [x] 36.39 建立`PoseGraphFactPreviewAdapter`。
- [x] 36.40 禁止Pose Graph Preview创建虚假Action entry。
- [x] 36.41 建立`MotionMatchingQueryPreviewAdapter`。
- [x] 36.42 禁止MM Query Fixture创建Gameplay producer或PlaybackId。
- [x] 36.43 把`BuildBindings`迁入正式binding factory。
- [x] 36.44 删除旧Playback类型上的`BuildBindings`静态入口。
- [x] 36.45 定义Action-only lifecycle snapshot。
- [x] 36.46 从Action snapshot删除PoseNodeId、output weight与Pose availability。
- [x] 36.47 定义独立Pose Plan runtime snapshot。
- [x] 36.48 定义独立AnimationSlot runtime snapshot。
- [x] 36.49 分离Action Marker relation与PoseState Source Sync snapshot。
- [x] 36.50 让动画表现协调器在成功commit后组合Debug View。
- [x] 36.51 删除Pose Runtime diagnostics对Action lifecycle snapshot的依赖。
- [x] 36.52 更新CharacterPipelineHost diagnostics provider。
- [x] 36.53 更新Preview diagnostics provider。
- [x] 36.54 更新RuntimeDebugSession trace publisher。

## 26. 重建正式产物

- [x] 26.1 提升Presentation Projection schema。
- [x] 26.2 更新Projection compiler。
- [x] 26.3 更新cross-artifact validator。
- [x] 26.4 更新source map validator。
- [x] 26.5 更新Pose Plan workspace layout。
- [x] 26.6 用`character.build_float32_products`和精确Corin Definition路径原子重建Presentation Projection。
- [x] 26.7 使用同一次精确Float32 Build原子重建Definition正式wrapper。
- [x] 26.8 用`character.build_fixed_products`、精确Corin Definition路径与精确Fixed destination原子重建wrapper。
- [x] 26.9 校验Program不含BaseLocomotion animation producer。
- [x] 26.10 校验Projection包含PoseStateMachine与Slot。
- [x] 26.11 校验Program与Projection identity一致。

## 27. 清理旧代码与资产

- [x] 27.1 删除旧Locomotion Timeline inline data。
- [x] 27.2 删除旧Locomotion AnimationTrack identity。
- [x] 27.3 删除旧Locomotion marker owner。
- [x] 27.4 删除旧ActionOverride authoring data。
- [x] 27.5 删除旧ownership Blackboard declaration。
- [x] 27.6 删除旧ownership compiled field。
- [x] 27.7 删除旧ownership runtime state。
- [x] 27.8 删除旧恢复动画route。
- [x] 27.9 删除旧Selection schema字段。
- [x] 27.10 删除旧Projection ABI字段。
- [x] 27.11 删除旧runtime branch。
- [x] 27.12 删除旧Editor Details。
- [x] 27.13 删除旧Agent schema字段。
- [x] 27.14 删除旧Agent validator规则。
- [x] 27.15 删除旧Agent Document内容。
- [x] 27.16 确认没有fallback配置。
- [x] 27.17 确认没有兼容adapter。
- [x] 27.18 确认没有第二PlayableGraph。

## 28. 同步Agent authoring

- [x] 28.1 在Pose Graph重构提供的Document v3目标状态中声明PoseStateMachine editable owner。
- [x] 28.2 在同一目标状态中声明state-local Pose source binding。
- [x] 28.3 在同一目标状态中声明AnimationSlot与有限Action binding。
- [x] 28.4 在同一目标状态中删除BaseLocomotion producer。
- [x] 28.5 在同一目标状态中删除ActionOverride与旧ownership数据。
- [x] 28.6 消费Pose Graph重构唯一拥有的Document v3 schema，不在本change复制schema version分支。
- [x] 28.7 消费Pose Graph重构唯一拥有的v3 exporter并对账动画业务字段。
- [x] 28.8 消费Pose Graph重构唯一拥有的v3 reconciler与Presentation Mutation。
- [x] 28.9 消费Pose Graph重构唯一拥有的v3 validator并补齐动画业务约束输入。
- [x] 28.10 消费五个Document lifecycle工具，不增加动画专属MCP字段或工具。
- [x] 28.11 对账`btsmtl-agent-authoring`技能已经使用唯一v3 capability命名，不在本change维护第二份技能口径。
- [x] 28.12 用精确Corin Definition显式checkout唯一Document v3迁移包，并通过正式迁移规划器生成旧inline Pose State Graph到flat catalog的typed目标状态；不得先修改Unity资产。
- [x] 28.13 以导出hash创建唯一迁移checkout。
- [x] 28.14 在同一Document mutation中完成20–23与27的全部资产变更。
- [x] 28.15 对最终Document执行dry-run并修复全部typed validation failure。
- [x] 28.16 使用dry-run返回的exact hash执行正式`apply_document`。
- [x] 28.17 反向导出应用后的Corin Document。
- [x] 28.18 对账反向导出与目标Document的canonical hash。
- [x] 28.19 确认迁移未保留旧BaseLocomotion、ActionOverride或旧Selection字段。
- [x] 28.20 确认迁移未创建fallback、兼容配置或双写资产。

## 29. 准备归档文档对账

- [x] 29.1 更新`openspec/project.md`的active实施状态，不把未归档能力写成current truth。
- [x] 29.2 核对Selection Runtime delta覆盖最终实现。
- [x] 29.3 核对Layer Runtime delta覆盖最终实现。
- [x] 29.4 核对Pose Graph delta覆盖最终实现。
- [x] 29.5 核对Presentation Authoring delta覆盖最终实现。
- [x] 29.6 核对Animation Pipeline delta覆盖最终实现。
- [x] 29.7 核对Corin State Timeline delta覆盖最终实现。
- [x] 29.8 核对Action ownership delta覆盖最终实现。
- [x] 29.9 核对Motion semantics delta覆盖最终实现。
- [x] 29.10 删除“基础姿态必须来自Timeline producer”结论。
- [x] 29.11 删除“BTSMTL唯一选择BaseLocomotion动画”结论。
- [x] 29.12 记录Action Timeline继续拥有有限动作权威时间。
- [x] 29.13 核对Pipeline Definition Authoring delta。
- [x] 29.14 核对Pipeline Runtime delta。
- [x] 29.15 核对Presentation Interpolation delta。
- [x] 29.16 核对State Interruption delta。
- [x] 29.17 核对Foot Analysis Artifact delta。
- [x] 29.18 核对Timeline Preview delta。
- [x] 29.19 核对Agent Character Controller delta。
- [x] 29.20 核对Transition Routing Module delta。
- [x] 29.21 删除旧Routing接入change引用。
- [x] 29.22 对账全部active animation change后续关系。
- [x] 29.23 核对Equipment Presentation delta。
- [x] 29.24 核对Foot Placement Presentation delta。
- [x] 29.25 记录动画表现协调器与Action Playback Runtime的最终边界。
- [x] 29.26 删除current specs与project中的旧共享Playback Runtime口径。

## 37. 修正提交移动身份与Corin运行时回归

- [x] 37.1 定义稳定的`presentation.movement-mode` Identity Fact。
- [x] 37.2 从已提交Motion的Locomotion owner或Gameplay Result owner提取所属Gameplay State作者身份。
- [x] 37.3 禁止Action owner进入基础Locomotion Pose选择。
- [x] 37.4 把Movement Mode身份接入Float32 Presentation Intent。
- [x] 37.5 把Movement Mode身份接入Fixed Presentation Intent。
- [x] 37.6 把Movement Mode身份接入Deterministic Rollback Presentation Intent。
- [x] 37.7 在Presentation Fact重采样中保持Movement Mode离散身份。
- [x] 37.8 为共享Transition Rule增加Identity值类型与Identity Literal。
- [x] 37.9 在唯一Transition Rule compiler中编译Identity相等与不相等判断。
- [x] 37.10 在唯一PoseState runtime中求值Identity规则。
- [x] 37.11 在共享Capability、Details与Document v3 payload中开放Identity Literal。
- [x] 37.12 删除Corin Pose StateMachine按速度阈值猜测Walk与Run的规则。
- [x] 37.13 删除Corin Pose StateMachine按Facing Error猜测MovingTurn的规则。
- [x] 37.14 用同一Document v3事务把Corin Pose Transition改为已提交Movement Mode身份。
- [x] 37.15 用同一Document v3事务恢复DodgeForward到RunLoop的正式Gameplay链。
- [x] 37.16 用同一Document v3事务恢复MovingTurn到RunLoop的正式Gameplay链。
- [x] 37.17 用同一Document v3事务把MovingTurn曲线恢复为完整Root Motion平移与旋转。
- [x] 37.18 对Document目标状态执行dry-run并使用exact hash apply。
- [x] 37.19 canonical reverse export并确认Document回到Clean。
- [x] 37.20 通过精确Corin Definition重建Float32 Program、Fixed Program与Presentation Projection。
- [x] 37.21 对账本地产品的Idle、Walk、DodgeForward、RunLoop、MovingTurn业务链。
- [x] 37.22 对账Peer A/B产品使用同一Program、Projection与Rollback Composition。
- [x] 37.23 定位并清除本次重构引入的Presentation每帧分配或重复工作。
- [x] 37.24 修复Animation Slot从无Stack Pose的SourcePose端点快速切入下一Action时误请求Stored Pose捕获。

## 38. 修正Pose连续Transition抢占

- [x] 38.1 沿Gameplay committed movement-mode到PoseStateMachine定位WalkEnd无法及时退出的阻塞点。
- [x] 38.2 确认MovingTurn RootMotion曲线与in-place动画时长、采样率和Root排除策略一致。
- [x] 38.3 确认Locomotion Override仲裁不会把普通移动位移叠加到MovingTurn曲线。
- [x] 38.4 在Active Transition期间继续消费当帧正式Presentation Fact。
- [x] 38.5 当新Transition不同于Active Transition时使用唯一Transition Routing执行替换。
- [x] 38.6 为替换后的Transition分配新的selection generation与native control generation。
- [x] 38.7 复用Transition Routing既有request generation与RebaseRequired语义，不新增惯性混合算法。
- [x] 38.8 清除被替换Transition的本地capture与release完成状态。
- [x] 38.9 仅释放不再属于新Transition source或target的Pose source。
- [x] 38.10 防止Active Transition被同一条规则逐帧重新启动。
- [x] 38.11 处理零时长Standard Blend替换后Active Transition已经完成的边界。
- [x] 38.12 保持Gameplay StateMachine、MovingTurn RootMotion、IK与FootPlacement不变。
- [x] 38.13 同步Character Pipeline运行时行为基线中的连续Transition约束。

## 39. 统一MovingTurn位移曲线与Pose采样相位

- [x] 39.1 用临时诊断记录MovingTurn的committed movement mode、Body位移、Body朝向与PoseState播放时间。
- [x] 39.2 确认最终Visual Root没有额外写入朝向偏移。
- [x] 39.3 确认MovingTurn RootMotion曲线和Pose clip正文未相对已闭环版本发生变化。
- [x] 39.4 定位Gameplay Timeline先推进而Pose SequencePlayer后从零开始的首帧相位差。
- [x] 39.5 在Float32 Motion contribution携带winner的curve-local sample time与duration。
- [x] 39.6 在Fixed Motion contribution携带相同语义的curve-local sample time与duration。
- [x] 39.7 只从最终Locomotion Override winner投影Gameplay曲线相位。
- [x] 39.8 把Float32 winner相位接入Character Motion Request与Presentation Intent。
- [x] 39.9 把Fixed winner相位接入Character Motion Request与Presentation Intent。
- [x] 39.10 把Rollback Fixed结果接入同一Presentation Intent，不增加snapshot或网络字段。
- [x] 39.11 在Presentation Fact插值中处理进入新MovementMode时的零到当前sample相位。
- [x] 39.12 为SequencePlayer增加显式`PresentationDelta`与`GameplayLocomotionTimeline` clock source。
- [x] 39.13 要求Gameplay clock同时满足非循环、play rate为1和精确MovementMode identity。
- [x] 39.14 在共享Capability、typed payload、Presentation Mutation与Pose IR中登记唯一clock binding。
- [x] 39.15 在Projection descriptor、compiler fingerprint与runtime ABI中编译clock binding。
- [x] 39.16 在PoseState transition reset之后使用同一Fact frame同步Sequence clock。
- [x] 39.17 让同一节点的多字段Presentation Mutation在一次payload构造中原子提交。
- [x] 39.18 让Document v3明确区分可选Capability字段、缺失required字段与未知字段。
- [x] 39.19 通过Document v3 rebase清除Capability context冲突并保留editable目标正文。
- [x] 39.20 dry-run确认Corin目标只包含MovingTurn clock source与MovementMode binding两条业务变更。
- [x] 39.21 使用exact document hash执行Corin apply并完成canonical reverse export回到Clean。
- [x] 39.22 用精确Definition发布Float32 Program、Fixed Program与Presentation Projection。
- [x] 39.23 重新checkout并通过CharacterController正式validator。
- [x] 39.24 删除MovingTurn临时诊断日志。

## 40. 把MovingTurn收口为Body输入运动与Presentation根朝向偏移

- [x] 40.1 用运行基准固定MovingTurn的Body权威、无X/Z RootMotion与Pose-only根朝向偏移边界，并对账本地GASP与ALS V4职责。
- [x] 40.2 在唯一Pose Capability Catalog登记`RootOrientationWarp`及其业务字段和Pose端口。
- [x] 40.3 新增`RootOrientationWarp` typed payload并纳入通用Document codec。
- [x] 40.4 在唯一Presentation Mutation按整份payload原子支持根朝向偏移字段。
- [x] 40.5 在正式Validator校验Yaw曲线、总角度、时长与上游有限Sequence Player。
- [x] 40.6 新增独立`RootOrientationWarp` compiler handler并降低为Pose IR。
- [x] 40.7 编译根朝向偏移descriptor、Yaw曲线正文、上游Sequence索引与Rig root bone索引。
- [x] 40.8 把根朝向偏移descriptor纳入Projection schema、严格校验、canonical hash和编译fingerprint。
- [x] 40.9 把根朝向偏移operation纳入Native Pose Program ABI与布局校验。
- [x] 40.10 在Presentation事务中按Pose relevance捕获带符号目标角、Body discontinuity和Sequence sample time。
- [x] 40.11 在Native Pose Job只旋转Rig root local pose并透传Pose参数、脚部特征、贡献与continuity。
- [x] 40.12 把根朝向偏移状态纳入Presentation frame capture、rollback和reset，退出时严格归零。
- [x] 40.13 在diagnostics发布节点是否活跃、目标角、作者Yaw、Body角差与最终根偏移。
- [x] 40.14 删除MovingTurn对Gameplay Locomotion Timeline clock的依赖并恢复Turn Sequence的PresentationDelta时钟。
- [x] 40.15 通过唯一Document v3事务把MovingTurn state body替换为正式`locomotion-input-motion`。
- [x] 40.16 通过同一事务把MovingTurn退出边改为`move_has + !turn_facing_angle`和`move_stop`。
- [x] 40.17 通过同一事务删除`CorinMovingTurnTimeline`及其X/Z/Yaw Gameplay MotionCurve可达关系。
- [x] 40.18 通过同一事务在Turn state graph接入`SequencePlayer -> RootOrientationWarp -> StatePoseOutput`。
- [x] 40.19 dry-run确认事务只触及MovingTurn Gameplay Graph、Transition、Timeline和Turn Pose Graph。
- [x] 40.20 使用exact document hash apply并canonical reverse export回到Clean。
- [x] 40.21 通过精确Definition显式发布Float32 Program、Fixed Program、Presentation Projection与Native Pose Program。
- [x] 40.22 删除旧Gameplay Locomotion Timeline相位投影字段、compiler/runtime分支和临时MovingTurn诊断日志。
- [x] 40.23 对账Local Fixed与DeterministicRollback Relay、Peer A/B继续装配同一Program、Projection、KCC和Collision Artifact。

## 41. 修正MovingTurn相反方向交接

- [x] 41.1 沿InputAction composite、表现帧锁存、MoveAxis、MoveFacingAngle和RunLoop转场确认W/S与A/D重叠的零输入链路。
- [x] 41.2 区分相反键相消、真实90度方向变化、135度边界、Body转向消耗和Action Context阻断五类触发条件。
- [x] 41.3 为Vector2输入定义显式数字方向冲突策略。
- [x] 41.4 校验最近激活方向策略只用于带完整上下左右part的唯一Dpad composite。
- [x] 41.5 实现共享的最近激活数字方向解析器。
- [x] 41.6 把解析器接入Float32 Unity Input Adapter的表现帧采样边界。
- [x] 41.7 把同一解析器接入Fixed Unity Input Adapter的表现帧采样边界。
- [x] 41.8 让停用与释放输入适配器时解绑回调并清空物理输入历史。
- [x] 41.9 为Corin MoveAxis显式选择最近激活方向策略，LookAxis保持Unity原始输入。
- [x] 41.10 确认解析后的值进入现有portable input、Rollback history与网络codec，不增加snapshot或协议字段。
- [x] 41.11 完成非batchmode脚本编译并清理新增错误。
- [x] 41.12 同步Character Pipeline运行时行为基线与project active状态。

## 42. 修正MovingTurn连续CrossFade抢占

- [x] 42.1 对账MovingTurn Gameplay Timeline的28帧根运动窗口、71帧Pose Clip与0.3秒退出混合各自职责。
- [x] 42.2 确认Gameplay只在根Timeline完成后退出MovingTurn，不新增输入冷却或重复Gameplay状态。
- [x] 42.3 定位Standard Blend期间Pose StateMachine继续把source当作逻辑active state的阻塞点。
- [x] 42.4 让Standard Blend开始后target立即成为逻辑active state。
- [x] 42.5 在混合期间从target State的正式Transition Rule继续消费最新Presentation Fact。
- [x] 42.6 保持Standard Blend的source与target Pose source共同ready并持续采样到混合完成或被替换。
- [x] 42.7 让连续MovingTurn复用既有active transition替换、selection generation与native control generation。
- [x] 42.8 让Run或Walk到Turn的既有Inertialization从当前最终混合Pose重捕获，不增加第二混合算法。
- [x] 42.9 保持MovingTurn Timeline、Root Motion曲线、Pose Graph资产、IK、FootPlacement和Rollback协议不变。
- [x] 42.10 完成非batchmode运行程序集编译并同步连续Transition运行时基线。
