## 1. 收敛旧change与实施边界

- [x] 1.1 标记`add-character-motion-matching-pose-source`剩余外接Player任务由本change取代
- [x] 1.2 标记旧change剩余显式MM BlendStack任务由本change取代
- [x] 1.3 标记旧change独立validation fixture任务由完整MM角色Prefab取代
- [x] 1.4 保留旧change已完成的Feature Schema合同
- [x] 1.5 保留旧change已完成的SourceSet合同
- [x] 1.6 保留旧change已完成的Database Artifact合同
- [x] 1.7 保留旧change已完成的candidate admission合同
- [x] 1.8 保留旧change已完成的cost与Top-K计划合同
- [x] 1.9 保留旧change已完成的continuity plan合同
- [x] 1.10 保留旧change已完成的显式Analysis Build合同
- [x] 1.11 对账FinalIK change的最终Animation Rig schema
- [x] 1.12 将新增MM角色产物构建排在Rig v4 schema与角色Rig revision确定之后
- [x] 1.13 禁止实施期间提交可同时运行的新旧MM路径

## 2. 建立Pose节点核心合同

- [ ] 2.1 在Pose Node Kind中加入`MotionMatchingPose`
- [ ] 2.2 在Pose Node Kind中加入`PoseHistoryCollector`
- [ ] 2.3 在Pose Node Kind中加入`EntryPoseInput`
- [ ] 2.4 定义`MotionMatchingPose`稳定node identity
- [ ] 2.5 定义`MotionMatchingPose` typed输入端口
- [ ] 2.6 定义`MotionMatchingPose`唯一Local Pose输出端口
- [ ] 2.7 定义MM binding identity字段
- [ ] 2.8 定义MM Jump Blend Policy字段
- [ ] 2.9 定义MM entry graph identity字段
- [ ] 2.10 定义MM relevance reset policy字段
- [ ] 2.11 定义MM search cadence policy字段
- [ ] 2.12 定义`PoseHistoryCollector`稳定history identity
- [ ] 2.13 定义Collector Local Pose输入端口
- [ ] 2.14 定义Collector Local Pose passthrough输出端口
- [ ] 2.15 定义Collector history read value端口
- [ ] 2.16 定义Collector history commit lineage
- [ ] 2.17 定义`EntryPoseInput`固定Local Pose输出端口
- [ ] 2.18 定义entry graph `GraphOutput`约束

## 3. 建立Chooser与Profile装配合同

- [x] 3.1 定义`CharacterMotionMatchingDatabaseChooser`资产identity
- [x] 3.2 定义Chooser有序rule集合
- [x] 3.3 定义Chooser rule priority
- [x] 3.4 定义Chooser exclusive policy
- [x] 3.5 定义Chooser typed fact predicate
- [x] 3.6 定义Chooser database identity输出
- [x] 3.7 定义Chooser`ShouldSearch`输出
- [x] 3.8 定义Chooser`InterruptMode`输出
- [x] 3.9 定义Chooser可选search policy identity输出
- [ ] 3.10 删除MM binding中的裸数据库数组
- [ ] 3.11 从MM binding引用唯一MM Profile
- [ ] 3.12 从MM binding引用唯一Chooser
- [ ] 3.13 从MM binding引用SearchDomain identity
- [ ] 3.14 从MM binding引用正式artifact identities
- [x] 3.15 校验Chooser数据库属于所选MM Profile
- [x] 3.16 校验Chooser policy属于所选MM Profile
- [x] 3.17 校验Chooser重复数据库identity
- [x] 3.18 校验同优先级exclusive规则冲突
- [x] 3.19 把空Chooser结果定义为typed Invalid
- [x] 3.20 删除默认数据库与上一帧集合fallback

## 4. 关闭唯一Rig身份链

- [x] 4.1 从Presentation Profile读取唯一Rig identity
- [x] 4.2 校验MM FeatureSchema Rig等于Presentation Rig
- [x] 4.3 校验Database TargetRig等于Presentation Rig
- [x] 4.4 校验SourceSet TargetRig等于Presentation Rig
- [ ] 4.5 校验Database Artifact binding等于Presentation Rig
- [ ] 4.6 校验Foot Analysis binding等于Presentation Rig
- [ ] 4.7 校验Presentation Projection binding等于Presentation Rig
- [ ] 4.8 校验Native Pose Program binding等于Presentation Rig
- [x] 4.9 同时比较RigId与Revision
- [ ] 4.10 删除按Humanoid类型推断Rig兼容的入口
- [ ] 4.11 删除按骨骼名称接受旧revision的入口
- [ ] 4.12 在Rig revision变化时使旧MM node state失效
- [ ] 4.13 在Rig revision变化时使旧数据库artifact stale
- [ ] 4.14 在Rig revision变化时使旧Foot Analysis stale
- [ ] 4.15 在Rig revision变化时使旧Projection和Pose Program stale

## 5. 统一Capability与作者创建入口

- [ ] 5.1 在共享Capability Catalog登记`MotionMatchingPose`
- [ ] 5.2 在共享Capability Catalog登记`PoseHistoryCollector`
- [ ] 5.3 在共享Capability Catalog登记`EntryPoseInput`
- [ ] 5.4 声明MotionMatchingPose允许出现的graph context
- [ ] 5.5 声明PoseHistoryCollector允许出现的graph context
- [ ] 5.6 声明EntryPoseInput只允许出现在MM entry graph
- [ ] 5.7 声明MM entry graph允许的node capability集合
- [ ] 5.8 禁止entry graph包含StateMachine
- [ ] 5.9 禁止entry graph包含MotionMatchingPose
- [ ] 5.10 禁止entry graph包含PoseHistoryCollector
- [ ] 5.11 禁止entry graph包含AnimationSlot
- [ ] 5.12 禁止entry graph包含外部source Player
- [ ] 5.13 禁止entry graph包含world-aware节点
- [ ] 5.14 禁止entry graph包含Component Pose IK
- [ ] 5.15 从Capability Catalog移除`SelectedPosePlayer`
- [ ] 5.16 从Capability Catalog移除显式MM BlendStack入口
- [ ] 5.17 让创建菜单读取共享Capability声明
- [ ] 5.18 让Canvas端口读取共享Capability声明
- [ ] 5.19 让Inspector字段读取共享Capability声明

## 6. 完成Document与Mutation闭环

- [ ] 6.1 扩展唯一Agent Authoring Document的Pose node schema
- [ ] 6.2 扩展Document exporter输出MotionMatchingPose payload
- [ ] 6.3 扩展Document exporter输出PoseHistoryCollector payload
- [ ] 6.4 扩展Document exporter输出MM entry graph catalog
- [ ] 6.5 扩展Document reconciler读取MotionMatchingPose
- [ ] 6.6 扩展Document reconciler读取PoseHistoryCollector
- [ ] 6.7 扩展Document reconciler读取entry graph owner identity
- [ ] 6.8 新增创建MotionMatchingPose的typed Mutation
- [ ] 6.9 在同一Mutation创建entry graph identity
- [ ] 6.10 在同一Mutation创建EntryPoseInput
- [ ] 6.11 在同一Mutation创建GraphOutput
- [ ] 6.12 在同一Mutation连接identity Pose edge
- [ ] 6.13 新增创建PoseHistoryCollector的typed Mutation
- [ ] 6.14 新增配置MM binding的typed Mutation
- [ ] 6.15 新增配置MM Blend Policy的typed Mutation
- [ ] 6.16 新增配置MM reset policy的typed Mutation
- [ ] 6.17 新增配置MM search cadence的typed Mutation
- [ ] 6.18 新增配置Collector绑定的typed Mutation
- [ ] 6.19 复制MM节点时复制entry graph并生成新identity
- [ ] 6.20 删除MM节点时按引用计数删除entry graph
- [ ] 6.21 拒绝多个MM节点共享可变entry graph identity
- [ ] 6.22 更新节点复制与粘贴的identity重写
- [ ] 6.23 更新Undo/Redo覆盖节点与entry graph同一事务

## 7. 完成Validator与IR编译

- [ ] 7.1 校验MM节点四类typed输入完整
- [ ] 7.2 校验MM节点唯一Local Pose输出
- [ ] 7.3 校验MM binding完整
- [ ] 7.4 校验MM Blend Policy完整
- [ ] 7.5 校验entry graph引用存在
- [ ] 7.6 校验entry graph owner identity一致
- [ ] 7.7 校验entry graph唯一EntryPoseInput
- [ ] 7.8 校验entry graph唯一到达GraphOutput的Pose路径
- [ ] 7.9 校验entry graph禁止节点集合
- [ ] 7.10 校验每个MM节点只有一个兼容Collector
- [ ] 7.11 校验Collector与MM节点Rig一致
- [ ] 7.12 校验History Read与Commit无同帧环
- [ ] 7.13 校验同一Collector没有竞争writer
- [ ] 7.14 校验MM Jump没有第二continuity owner
- [ ] 7.15 定义MotionMatchingPose IR record
- [ ] 7.16 定义PoseHistoryRead IR record
- [ ] 7.17 定义PoseHistoryCommit IR record
- [ ] 7.18 定义Chooser Resolve IR record
- [ ] 7.19 定义Entry Source Capture IR record
- [ ] 7.20 定义Entry Processing IR subprogram
- [ ] 7.21 定义Internal Blend IR record
- [ ] 7.22 从IR删除SelectedPosePlayer opcode
- [ ] 7.23 从IR删除MM Slot输入
- [ ] 7.24 从IR删除显式MM BlendStack opcode

## 8. 重构MM共享Runtime

- [ ] 8.1 定义不可变`CharacterMotionMatchingFrameContext`
- [ ] 8.2 将Trajectory规范化结果写入Frame Context
- [ ] 8.3 将typed presentation facts页写入Frame Context
- [ ] 8.4 将delta time写入Frame Context
- [ ] 8.5 将frame identity写入Frame Context
- [ ] 8.6 将Rig lineage写入Frame Context
- [ ] 8.7 在Presentation Stage每帧只resolve一次Frame Context
- [ ] 8.8 删除节点直接读取Input组件的路径
- [ ] 8.9 删除节点直接读取KCC组件的路径
- [ ] 8.10 删除节点直接读取Transform的路径
- [ ] 8.11 删除节点直接读取Unity Time的路径
- [ ] 8.12 提取无状态`CharacterMotionMatchingSearchKernel`
- [ ] 8.13 把candidate admission迁入Search Kernel
- [ ] 8.14 把lower-bound pruning迁入Search Kernel
- [ ] 8.15 把Top-K plan评估迁入Search Kernel
- [ ] 8.16 把Continue/Jump判定迁入Search Kernel
- [ ] 8.17 让Search Kernel返回完整typed plan
- [ ] 8.18 从Search Kernel删除动画采样
- [ ] 8.19 从Search Kernel删除source time推进
- [ ] 8.20 从Search Kernel删除Blend entry分配
- [ ] 8.21 从Search Kernel删除Pose History写入
- [ ] 8.22 删除旧`CharacterMotionMatchingPresentationModule`运行实例

## 9. 实现节点级History与选择生命周期

- [ ] 9.1 为每个编译MM节点分配独立runtime state
- [ ] 9.2 保存节点当前selection identity
- [ ] 9.3 保存节点当前selection generation
- [ ] 9.4 保存节点当前source time
- [ ] 9.5 保存节点query cadence状态
- [ ] 9.6 保存节点active entry状态
- [ ] 9.7 保存节点Stored Pose状态
- [ ] 9.8 保存节点source usage token
- [ ] 9.9 实现Collector固定容量history ring
- [ ] 9.10 实现Collector previous-page read view
- [ ] 9.11 实现Collector completed-page commit
- [ ] 9.12 在commit保存root kinematics
- [ ] 9.13 在commit保存source lineage
- [ ] 9.14 在commit保存frame与Rig lineage
- [ ] 9.15 实现Unseeded首帧状态
- [ ] 9.16 实现relevance reset
- [ ] 9.17 实现binding revision reset
- [ ] 9.18 实现Rig revision reset
- [ ] 9.19 实现Preview seek reset
- [ ] 9.20 阻止同帧读取未完成history page
- [ ] 9.21 阻止跨节点selection generation输入
- [ ] 9.22 阻止跨Rig entry进入当前节点

## 10. 收敛内部Blend Stack Kernel

- [ ] 10.1 从现有BlendStack代码提取无owner状态的数值Kernel
- [ ] 10.2 保留独立blend clock计算
- [ ] 10.3 保留curve采样
- [ ] 10.4 保留per-bone权重规范化
- [ ] 10.5 保留Stored Pose压缩
- [ ] 10.6 保留固定Animation Job输出
- [ ] 10.7 把owner workspace显式传入Kernel
- [ ] 10.8 把MM Continue映射为当前entry推进
- [ ] 10.9 把MM Jump映射为新entry压入
- [ ] 10.10 把selection generation写入entry identity
- [ ] 10.11 把entry source time写入采样计划
- [ ] 10.12 把entry graph program identity写入entry计划
- [ ] 10.13 为每个live entry独立执行entry program
- [ ] 10.14 为有状态inner node组合entry-local state key
- [ ] 10.15 在容量满时生成Stored Pose
- [ ] 10.16 在权重归零时发布精确release
- [ ] 10.17 拒绝部分entry失败后的剩余权重重归一化
- [ ] 10.18 删除Animancer transition处理MM Jump的入口
- [ ] 10.19 删除外接Inertialization处理MM Jump的入口
- [ ] 10.20 删除显式作者BlendStack中的MM Slot字段

## 11. 编译Projection与固定工作区

- [ ] 11.1 把Frame Context Resolve编入Projection stage table
- [ ] 11.2 把History Read编入Projection stage table
- [ ] 11.3 把Chooser Resolve编入Projection stage table
- [ ] 11.4 把Search编入Projection stage table
- [ ] 11.5 把Entry Source Capture编入Projection stage table
- [ ] 11.6 把Entry Processing编入Projection stage table
- [ ] 11.7 把Internal Blend编入Projection stage table
- [ ] 11.8 把History Commit编入Projection stage table
- [ ] 11.9 强制History Commit位于AnimationSlot之前
- [ ] 11.10 强制History Commit位于world-aware Pose节点之前
- [ ] 11.11 计算每节点candidate page容量
- [ ] 11.12 计算每节点feature page容量
- [ ] 11.13 计算每Collector history page容量
- [ ] 11.14 计算每节点live entry容量
- [ ] 11.15 计算每节点Stored Pose容量
- [ ] 11.16 计算每entry processing program状态容量
- [ ] 11.17 计算MM diagnostics page容量
- [ ] 11.18 把MM node records写入Float32 Program
- [ ] 11.19 把MM node records写入Fixed Program
- [ ] 11.20 删除Projection中的旧MM Pose Source payload
- [ ] 11.21 删除Projection中的SelectedPosePlayer record
- [ ] 11.22 删除Projection中的显式MM BlendStack record
- [ ] 11.23 更新Native Pose Program opcode与布局
- [ ] 11.24 更新Program stale identity计算

## 12. 接通作者工作区与诊断

- [ ] 12.1 在Profile Inspector显示MM Profile引用
- [ ] 12.2 在Profile Inspector显示Chooser引用
- [ ] 12.3 在Profile Inspector显示SearchDomain identity
- [ ] 12.4 在Profile Inspector显示Rig闭包状态
- [ ] 12.5 在Profile Inspector显示Database Artifact状态
- [ ] 12.6 在Profile Inspector显示Foot Analysis状态
- [ ] 12.7 在Profile Inspector显示Projection与Pose Program状态
- [ ] 12.8 在Canvas显示MM typed端口
- [ ] 12.9 在Canvas显示Collector history关系
- [ ] 12.10 双击MM节点打开entry graph
- [ ] 12.11 在Navigator保留Definition到entry graph面包屑
- [ ] 12.12 在References面板显示MM跨资产identity
- [ ] 12.13 在Pose Watch显示Chooser命中规则
- [ ] 12.14 在Pose Watch显示数据库集合
- [ ] 12.15 在Pose Watch显示query cadence
- [ ] 12.16 在Pose Watch显示admission与cost breakdown
- [ ] 12.17 在Pose Watch显示Continue/Jump原因
- [ ] 12.18 在Pose Watch显示active entries与权重
- [ ] 12.19 在Pose Watch显示entry graph program
- [ ] 12.20 在Pose Watch显示Stored Pose
- [ ] 12.21 在Pose Watch显示history read与commit frame
- [ ] 12.22 在Pose Watch显示完整Rig lineage
- [ ] 12.23 让Preview注入typed facts与Trajectory
- [ ] 12.24 让Preview运行正式Chooser与MM node program
- [ ] 12.25 删除独立MM validation fixture入口

## 13. 建立MotionMatchingDemo角色正式内容

- [ ] 13.1 定义`MotionMatchingDemoCharacter`稳定角色identity
- [ ] 13.2 建立`Assets/Configs/Character/MotionMatchingDemo/`正式配置根
- [ ] 13.3 选择新增Prefab使用的GASP角色模型
- [ ] 13.4 选择新增Prefab使用的Animator Avatar
- [ ] 13.5 在Rig v4 schema上创建该角色唯一Presentation Rig
- [ ] 13.6 配置该角色Physical Bone catalog
- [ ] 13.7 配置该角色Virtual Bone catalog
- [ ] 13.8 配置该角色Motion Root与骨骼采样binding
- [ ] 13.9 冻结该角色正式RigId与Revision
- [ ] 13.10 清点GASP Idle候选clip identity
- [ ] 13.11 清点GASP Walk候选clip identity
- [ ] 13.12 清点GASP Run候选clip identity
- [ ] 13.13 清点GASP Sprint候选clip identity
- [ ] 13.14 显式排除GASP Crouch素材
- [ ] 13.15 显式排除GASP Airborne素材
- [ ] 13.16 显式排除GASP Slide素材
- [ ] 13.17 显式排除GASP Traversal素材
- [ ] 13.18 创建MotionMatchingDemo Grounded Idle SourceSet
- [ ] 13.19 创建MotionMatchingDemo Grounded Locomotion SourceSet
- [ ] 13.20 创建MotionMatchingDemo Grounded Sprint SourceSet
- [ ] 13.21 为每个SourceSet绑定该角色唯一Rig
- [ ] 13.22 为每个SourceSet登记明确clip与segment
- [ ] 13.23 配置Grounded Coverage Requirements
- [ ] 13.24 创建MotionMatchingDemo MM FeatureSchema
- [ ] 13.25 将FeatureSchema绑定该角色唯一Rig
- [ ] 13.26 创建Idle、WalkRun与Sprint Database定义
- [ ] 13.27 创建MotionMatchingDemo Motion Matching Profile
- [ ] 13.28 将三个数据库登记到该MM Profile
- [ ] 13.29 创建MotionMatchingDemo Grounded Database Chooser
- [ ] 13.30 配置Grounded、WalkRun与Sprint typed事实规则
- [ ] 13.31 配置明确ShouldSearch与InterruptMode
- [ ] 13.32 显式构建MotionMatchingDemo Database Artifacts
- [ ] 13.33 显式构建MotionMatchingDemo Foot Analysis Artifacts

## 14. 装配MotionMatchingDemo角色Prefab与Pose Graph

- [ ] 14.1 创建MotionMatchingDemo Character Pipeline Definition
- [ ] 14.2 创建MotionMatchingDemo Animation Presentation Profile
- [ ] 14.3 从Definition引用唯一Presentation Profile
- [ ] 14.4 从Presentation Profile引用该角色唯一Rig
- [ ] 14.5 从Presentation Profile绑定MM Profile与Chooser
- [ ] 14.6 创建MotionMatchingDemo root Pose Graph Document
- [ ] 14.7 创建Grounded Pose State与inline graph
- [ ] 14.8 创建Grounded MotionMatchingPose节点
- [ ] 14.9 创建该节点的identity entry graph
- [ ] 14.10 创建PoseHistoryCollector节点
- [ ] 14.11 连接Collector previous history到MM节点
- [ ] 14.12 连接Frame Context Trajectory到MM节点
- [ ] 14.13 连接typed facts到MM节点
- [ ] 14.14 连接正式MM binding到MM节点
- [ ] 14.15 连接MM Local Pose到正式Animation Slot链
- [ ] 14.16 把History Commit固定在Animation Slot之前
- [ ] 14.17 按最终Pose Graph spec连接下游Root与IK拓扑
- [ ] 14.18 创建`MotionMatchingDemoCharacter.prefab`
- [ ] 14.19 在Prefab装配标准`CharacterPipelineHost`
- [ ] 14.20 从Prefab引用MotionMatchingDemo Definition
- [ ] 14.21 在Prefab绑定选定模型、Animator与Avatar
- [ ] 14.22 从Prefab移除Animator Controller动画路径
- [ ] 14.23 拒绝Prefab挂载MxMAnimator或自主MM Player
- [ ] 14.24 拒绝Prefab挂载第二Rig、shadow skeleton或runtime Retarget组件
- [ ] 14.25 在Producer Navigator登记该Prefab的Definition上下文
- [ ] 14.26 显式构建MotionMatchingDemo Presentation Projection
- [ ] 14.27 显式构建MotionMatchingDemo Float32 Pose Program
- [ ] 14.28 显式构建MotionMatchingDemo Fixed Pose Program
- [ ] 14.29 将生成物identity回绑到该角色唯一Presentation Profile
- [ ] 14.30 清除新增Prefab中的旧MM Slot、SelectedPosePlayer和外接MM BlendStack序列化数据

## 15. 激进清理旧实现

- [ ] 15.1 删除`CharacterMotionMatchingPoseSourceSlot`合同
- [ ] 15.2 删除`CharacterMotionMatchingPoseSourceBinding`旧数据库数组字段
- [ ] 15.3 删除`CharacterSelectedPosePlayerPayload`
- [ ] 15.4 删除SelectedPosePlayer Mutation
- [ ] 15.5 删除SelectedPosePlayer Capability
- [ ] 15.6 删除SelectedPosePlayer Validator分支
- [ ] 15.7 删除SelectedPosePlayer IR compiler分支
- [ ] 15.8 删除SelectedPosePlayer Runtime state
- [ ] 15.9 删除SelectedPosePlayer Workspace页
- [ ] 15.10 删除显式BlendStack的MM payload
- [ ] 15.11 删除显式MM BlendStack Mutation
- [ ] 15.12 删除显式MM BlendStack Compiler分支
- [ ] 15.13 删除旧Module selection state
- [ ] 15.14 删除旧Module Pose History state
- [ ] 15.15 删除旧Module reset state
- [ ] 15.16 删除旧MM PresentationPoseSourceSample转换
- [ ] 15.17 删除旧MM source usage桥接
- [ ] 15.18 删除旧validation profile资产与引用
- [ ] 15.19 删除旧fixture专用数据库binding
- [ ] 15.20 删除旧路径的diagnostic字段
- [ ] 15.21 删除旧路径的Document schema字段
- [ ] 15.22 删除旧路径的exporter输出
- [ ] 15.23 删除旧路径的reconciler读取
- [ ] 15.24 删除旧路径的serialized配置资产

## 16. 同步项目真相文档

- [ ] 16.1 更新`openspec/project.md`中的MM正式链路
- [ ] 16.2 删除project文档中的MM provider到显式Player口径
- [ ] 16.3 更新Pose Graph节点目录
- [ ] 16.4 更新Animation layer职责顺序
- [ ] 16.5 更新MM authoring入口说明
- [ ] 16.6 更新MM显式Build入口说明
- [ ] 16.7 更新Rig identity闭包说明
- [ ] 16.8 更新MotionMatchingDemo角色Prefab与Grounded内容范围说明
- [ ] 16.9 更新Preview、Pose Watch和Trace字段说明
- [ ] 16.10 删除旧独立fixture发布说明
