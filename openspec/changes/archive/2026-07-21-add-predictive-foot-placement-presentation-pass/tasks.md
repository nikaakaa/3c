# Tasks

## 1. 实施前基线与冲突核对

- [x] 1.1 使用PowerShell UTF-8重新读取本change proposal、design、tasks和全部spec delta
- [x] 1.2 读取current `character-animation-pipeline`、`character-pipeline-runtime`与`character-presentation-interpolation` spec
- [x] 1.3 记录当前 `CharacterSimulationPresentationRuntime` 的Body、Animation、Camera调用顺序
- [x] 1.4 记录当前 `CharacterPresentationRuntimeFactory` 的Local、Simulated、Observed和Preview创建入口
- [x] 1.5 记录当前 `CharacterBodyPresentationFrame` 的velocity、Grounded、ResetSequence与ResetReason字段
- [x] 1.6 记录当前 `CharacterAnimationPlaybackRuntime` 到 `AnimancerPlaybackAdapter.Evaluate` 的唯一链路
- [x] 1.7 记录当前Corin prefab的VisualRoot、Animator、Animancer、Body Profile和World binding
- [x] 1.8 记录Final IK当前位于Assembly-CSharp-firstpass且RootMotion asmdef尚未安装的事实
- [x] 1.9 检查 `add-timeline-animation-marker-sync` 是否保持只选择表现采样时间且未定义foot contact或plant权威
- [x] 1.10 如存在重叠foot contact权威则停止并说明合并tradeoff，不继续双源实现
- [x] 1.11 检查 `add-corin-targeted-motion-warp-demo` 对Host和actor role装配的最新修改
- [x] 1.12 确认本change只沿正式Presentation Factory/runtime闭环，不修改Simulation或Network；Timeline曲线只通过BTSMTL正式Animation Clip API和Agent事务迁移
- [x] 1.13 建立 `implementation-inventory.md` 记录最终类型、程序集、资产、调用点和删除路径

## 2. Final IK正式程序集边界

- [x] 2.1 从插件自带unitypackage核对`RootMotion.Runtime.asmdef`的官方内容和目标路径
- [x] 2.2 从插件自带unitypackage核对`RootMotion.Editor.asmdef`的官方内容和目标路径
- [x] 2.3 安装官方`Assets/Plugins/RootMotion/RootMotion.Runtime.asmdef`
- [x] 2.4 安装官方`Assets/Plugins/RootMotion/Editor/RootMotion.Editor.asmdef`
- [x] 2.5 为两个asmdef建立匹配Unity meta资产
- [x] 2.6 确认Final IK runtime源码只进入命名程序集`RootMotion`
- [x] 2.7 确认Final IK Editor源码只进入`RootMotionEditor`
- [x] 2.8 确认`RootMotionEditor`只在Editor平台并引用`RootMotion`
- [x] 2.9 新建`ThirdPersonCharacter.Presentation.FinalIK`运行时程序集
- [x] 2.10 让Final IK adapter程序集只引用`ThirdPersonClient.Runtime`和`RootMotion`
- [x] 2.11 确认`ThirdPersonClient.Runtime`不引用`RootMotion`
- [x] 2.12 搜索并删除BBB条件包装或其它项目代码对Final IK的旧旁路引用
- [x] 2.13 确认没有项目adapter代码写入`Assets/Plugins/RootMotion`
- [x] 2.14 确认没有修改Final IK vendor C#源码

## 3. Pose Post Process核心合同

- [x] 3.1 在Presentation core定义`ICharacterPosePostProcessPass`
- [x] 3.2 定义不可变`CharacterPosePostProcessFrame`
- [x] 3.3 让Frame携带同帧`CharacterBodyPresentationFrame`
- [x] 3.4 让Frame携带presentation delta和render frame identity
- [x] 3.5 让Frame携带只读animation pose contribution集合
- [x] 3.6 定义`CharacterPosePostProcessReset`与reset identity
- [x] 3.7 定义Pose Post Process创建时的actor和rig上下文
- [x] 3.8 保持合同不引用RootMotion、Simulation Target或Network Model类型
- [x] 3.9 为Pass定义单次Present、Reset和Dispose生命周期
- [x] 3.10 禁止Pass注册priority、动态排序或运行时全局registry
- [x] 3.11 为无动画正式输出定义明确的skip-and-reset语义
- [x] 3.12 为Pass异常定义fail-stop和actor上下文错误信息

## 4. Foot Placement Profile与Timeline曲线所有权

- [x] 4.1 新建Presentation-owned `CharacterFootPlacementProfile` ScriptableObject
- [x] 4.2 定义`FootPlacementTraceSettings`
- [x] 4.3 定义`FootPlacementContactSettings`
- [x] 4.4 定义`FootPlacementPredictionSettings`
- [x] 4.5 定义`FootPlacementConstraintSettings`
- [x] 4.6 定义`FootPlacementPelvisSettings`
- [x] 4.7 定义`FootPlacementRotationSettings`
- [x] 4.8 定义`FootPlacementSmoothingSettings`
- [x] 4.9 在Profile中保存唯一PoseSourceLayerId
- [x] 4.10 在Timeline `AnimationClip`建立按动画时间采样的Foot Placement曲线作者入口
- [x] 4.11 让Timeline Inspector直接编辑单一Foot Placement Weight曲线且dirty owner为Timeline
- [x] 4.12 删除Profile中的producer policy、identity和动画时间曲线
- [x] 4.13 将Profile构建为不可变runtime settings
- [x] 4.14 校验全部数值有限且范围有序
- [x] 4.15 校验单一Timeline Foot Placement Weight曲线存在且全部key时间和值位于`[0,1]`
- [x] 4.16 保持曲线只按stable Timeline/Track/Clip identity编译且不按名称匹配
- [x] 4.17 校验Ground LayerMask非空且不包含明确Character层
- [x] 4.18 禁止代码内置Corin默认参数或缺Profile fallback
- [x] 4.19 保持Profile不进入Definition、Program或Projection，同时让Timeline曲线进入正式Presentation Projection revision

## 5. 显式Rig与solver合同

- [x] 5.1 定义vendor-neutral `CharacterFootPlacementRig`绑定类型
- [x] 5.2 显式保存VisualRoot与pelvis引用
- [x] 5.3 显式保存左hip、knee、ankle和toe引用
- [x] 5.4 显式保存右hip、knee、ankle和toe引用
- [x] 5.5 显式保存左右heel/toe sole offset与foot forward axis
- [x] 5.6 显式保存self-collider root或精确self-collider集合
- [x] 5.7 校验全部骨骼属于同一VisualRoot层级
- [x] 5.8 校验左右链骨骼不重复且顺序合法
- [x] 5.9 校验pelvis是两条腿共同上游而不是任一foot后代
- [x] 5.10 校验sole offset、axis和leg length有限且非退化
- [x] 5.11 禁止Animator Humanoid骨骼映射fallback
- [x] 5.12 禁止骨骼名称和`GetComponentInChildren`扫描fallback
- [x] 5.13 定义`ICharacterFootPlacementSolver`
- [x] 5.14 定义animated rig capture结果
- [x] 5.15 定义solver Apply、Reset和Dispose合同
- [x] 5.16 定义solver同一render frame只允许Apply一次的校验

## 6. 动画可见Contribution投影

- [x] 6.1 定义只读`AnimationPoseContribution`
- [x] 6.2 保存LayerId、producer identity和playback generation
- [x] 6.3 保存visual sample time、normalized time和cycle
- [x] 6.4 保存Animancer实际state weight
- [x] 6.5 从Projection解析PoseSourceLayerId且不做字符串fallback
- [x] 6.6 在`AnimancerPlaybackAdapter.Evaluate`完成后构建当前visible contribution
- [x] 6.7 覆盖Current、Outgoing和重入fade中的合法visible state
- [x] 6.8 不把PendingFirstSample伪装成已经可见的pose contribution
- [x] 6.9 使用预分配buffer输出contribution
- [x] 6.10 禁止按clip名、State名、Action名或Tag推断曲线
- [x] 6.11 将单一Timeline Foot Placement Weight曲线复制到对应Presentation Projection clip binding
- [x] 6.12 先按producer内部clip weight、再按Animancer visible weight混合Foot Placement权重
- [x] 6.13 保持曲线混合不修改Animancer state、layer或fade
- [x] 6.14 保持AnimationPlaybackLifecycle不依赖Foot Placement结果

## 7. Body Frame预测输入补齐

- [x] 7.1 在Body target sample中保留target yaw velocity
- [x] 7.2 在Visual Trajectory Follower结果中保留visible yaw velocity
- [x] 7.3 将target与visible yaw velocity投影到`CharacterBodyPresentationFrame`
- [x] 7.4 保持yaw velocity只属于Presentation值
- [x] 7.5 确认Body frame字段不进入Program、Snapshot、Hash或packet
- [x] 7.6 保持Body Runtime仍是VisualRoot唯一写入者
- [x] 7.7 保持Foot Placement不修改Body frame或VisualRoot
- [x] 7.8 保持Camera继续消费同一Body frame而不维护第二份pose历史

## 8. Foot kinematic采样与Contact classifier

- [x] 8.1 定义每只脚固定容量的运行状态
- [x] 8.2 在Animancer Evaluate后捕获ankle、toe、heel和pelvis动画姿势
- [x] 8.3 将脚位置转换到VisualRoot局部空间
- [x] 8.4 计算剔除Body平移和旋转后的相对ankle速度
- [x] 8.5 计算剔除Body平移和旋转后的相对toe速度
- [x] 8.6 计算平面速度、垂直速度和descending状态
- [x] 8.7 从合法support candidate计算sole distance
- [x] 8.8 将Body Grounded纳入plant eligibility
- [x] 8.9 将producer placement weight纳入plant eligibility
- [x] 8.10 实现plant enter阈值
- [x] 8.11 实现release exit阈值
- [x] 8.12 实现enter/exit迟滞且不使用固定帧数
- [x] 8.13 对第一帧或reset后无速度历史定义重新锚定语义
- [x] 8.14 对非有限骨骼姿势直接报告错误
- [x] 8.15 确认Contact classifier不读取gait phase、Timeline或Gameplay状态

## 9. Footprint predictor

- [x] 9.1 定义每只脚的`PredictedFootprint`
- [x] 9.2 从foot vertical speed与support距离估算有限time-to-support
- [x] 9.3 使用Profile限制最小和最大look-ahead
- [x] 9.4 使用visible Body线速度预测root position
- [x] 9.5 使用visible Body yaw velocity预测root rotation
- [x] 9.6 使用当前foot局部位置和相对速度预测动画foot local trajectory
- [x] 9.7 合成predicted root pose与predicted foot local position
- [x] 9.8 保持Free脚水平位置继续来自动画
- [x] 9.9 让预测只影响support、clearance和落脚准备
- [x] 9.10 对角速度、距离和可达范围外预测明确拒绝
- [x] 9.11 对Locked脚只用预测判断前方surface和replant
- [x] 9.12 记录预测horizon、clamp与reject reason

## 10. PhysicsScene查询与Support Envelope

- [x] 10.1 定义显式PhysicsScene query context
- [x] 10.2 定义固定容量Ray/Sphere/Capsule hit buffer
- [x] 10.3 定义固定容量support candidate buffer
- [x] 10.4 对当前heel执行NonAlloc support查询
- [x] 10.5 对当前toe执行NonAlloc support查询
- [x] 10.6 对foot path中间采样点执行NonAlloc查询
- [x] 10.7 对predicted footprint执行NonAlloc查询
- [x] 10.8 对当前到预测路径执行Capsule sweep
- [x] 10.9 按Profile Ground LayerMask过滤命中
- [x] 10.10 使用显式self binding过滤角色自身Collider
- [x] 10.11 拒绝NaN、Infinity和退化normal
- [x] 10.12 拒绝超过最大可站立坡度的候选
- [x] 10.13 拒绝超过最大step up/down的候选
- [x] 10.14 拒绝hip到foot超出leg reach的候选
- [x] 10.15 按路径fraction、surface identity和distance稳定排序候选
- [x] 10.16 按相邻高度连续性构建分段support envelope
- [x] 10.17 选择路径末端最近且可达的plant support
- [x] 10.18 计算Free脚不低于envelope的有限swing clearance
- [x] 10.19 禁止退化为单Ray或Default LayerMask
- [x] 10.20 确认查询热路径无LINQ、临时List或每帧分配

## 11. Foot约束和移动Surface生命周期

- [x] 11.1 定义`FootConstraintState.Free`
- [x] 11.2 定义`FootConstraintState.Locked`
- [x] 11.3 定义`FootConstraintState.Sliding`
- [x] 11.4 定义有限`FootConstraintTransitionReason`
- [x] 11.5 实现Free到Locked的plant提交
- [x] 11.6 在Locked时保存surface Transform局部point和normal
- [x] 11.7 每帧从surface局部锚点重建世界lock target
- [x] 11.8 实现Locked到Sliding的距离和角度条件
- [x] 11.9 实现Sliding在同一surface内的受限移动
- [x] 11.10 实现Sliding回到Locked的稳定条件
- [x] 11.11 实现policy release到Free
- [x] 11.12 实现Body airborne到Free
- [x] 11.13 实现surface destroyed/disabled/layer mismatch到Free
- [x] 11.14 实现replant distance/angle超限到Free
- [x] 11.15 实现leg unreachable到Free
- [x] 11.16 使用presentation delta和half-life推进plant/release solve weight
- [x] 11.17 禁止Planting/Releasing隐藏状态或固定帧计时器
- [x] 11.18 确认surface引用不进入Simulation、Snapshot、Hash或Network

## 12. Foot orientation与Pelvis resolver

- [x] 12.1 从support normal和动画foot forward计算合法目标rotation
- [x] 12.2 限制foot pitch和roll最大角度
- [x] 12.3 为ascent配置偏水平orientation规则
- [x] 12.4 为descent配置有限贴坡orientation规则
- [x] 12.5 按producer rotation weight和constraint weight混合脚旋转
- [x] 12.6 计算左右leg length和当前hip到target距离
- [x] 12.7 为每条腿计算pelvis可达垂直区间
- [x] 12.8 求双腿可达区间交集
- [x] 12.9 以plant weight和高度确定主要支撑腿
- [x] 12.10 实现ascent时避免高支撑腿过伸
- [x] 12.11 实现descent时避免低支撑腿悬空
- [x] 12.12 在区间无交集时夹紧pelvis并标记不可达脚
- [x] 12.13 使用显式half-life临界阻尼pelvis offset
- [x] 12.14 分别限制最大上移、最大下移和变化速度
- [x] 12.15 保持pelvis只做局部垂直偏移
- [x] 12.16 禁止Foot Placement旋转spine、pelvis或VisualRoot

## 13. CharacterFootPlacementRuntime与Plan

- [x] 13.1 定义不可变`FootPlacementFootPlan`
- [x] 13.2 定义不可变`CharacterFootPlacementPlan`
- [x] 13.3 在Plan中保存双脚target position/rotation/weight/state
- [x] 13.4 在Plan中保存pelvis local vertical offset
- [x] 13.5 在Plan中保存actor、render frame和reset identity
- [x] 13.6 新建唯一`CharacterFootPlacementRuntime`
- [x] 13.7 让Runtime唯一拥有左右foot状态与workspace
- [x] 13.8 让Runtime唯一拥有contact classifier
- [x] 13.9 让Runtime唯一拥有footprint predictor
- [x] 13.10 让Runtime唯一拥有support envelope query
- [x] 13.11 让Runtime唯一拥有constraint resolver
- [x] 13.12 让Runtime唯一拥有pelvis resolver
- [x] 13.13 固定执行capture、classify、predict、query、resolve、plan、solve顺序
- [x] 13.14 保证同一render frame只提交一个Plan
- [x] 13.15 在动画没有正式输出时reset且不solve残留姿势
- [x] 13.16 在Dispose时清理surface引用、buffer和solver状态

## 14. Final IK Limb adapter

- [x] 14.1 在独立adapter程序集新建Final IK solver组件
- [x] 14.2 让组件实现`ICharacterFootPlacementSolver`
- [x] 14.3 显式绑定`CharacterFootPlacementRig`
- [x] 14.4 显式绑定左`LimbIK`
- [x] 14.5 显式绑定右`LimbIK`
- [x] 14.6 校验两个Limb链与rig hip/knee/ankle精确匹配
- [x] 14.7 禁用两个LimbIK的自主Unity lifecycle更新
- [x] 14.8 显式初始化两个底层IK solver
- [x] 14.9 在Apply前恢复Animancer本帧动画pelvis基准
- [x] 14.10 应用planner的pelvis局部垂直offset
- [x] 14.11 设置左脚IK position、rotation和weight
- [x] 14.12 设置右脚IK position、rotation和weight
- [x] 14.13 按固定左右顺序单次更新solver
- [x] 14.14 防止同一render frame重复Apply
- [x] 14.15 Reset时归零IK权重并恢复动画基准
- [x] 14.16 Dispose时解除runtime所有权且不销毁vendor asset
- [x] 14.17 确认adapter不query地面、不做contact或pelvis决策
- [x] 14.18 搜索并确认没有GrounderBipedIK、GrounderFBBIK或GrounderIK运行入口

## 15. Presentation协调器与Factory接入

- [x] 15.1 让`CharacterSimulationPresentationRuntime`拥有唯一Pose Post Process Pass
- [x] 15.2 在构造函数中显式接收该Pass
- [x] 15.3 在Body frame无效时保持现有返回语义并reset非法Pose历史
- [x] 15.4 在Animation Present完成后构建Pose Post Process Frame
- [x] 15.5 在Camera Present之前调用Foot Placement
- [x] 15.6 将ResetSequence变化在同帧传给Foot Placement Reset
- [x] 15.7 将`ICharacterPresentationRuntime.Reset`传播到Foot Placement
- [x] 15.8 将销毁顺序改为Camera、Foot Placement、Animation、Body
- [x] 15.9 扩展Presentation module lifetime helper覆盖Pose Pass
- [x] 15.10 让Factory显式接收Foot Placement Profile
- [x] 15.11 让Factory显式接收solver adapter接口
- [x] 15.12 让Factory显式接收rig/self-collider/PhysicsScene上下文
- [x] 15.13 在Factory创建前统一校验Profile、Projection policy和rig
- [x] 15.14 为LocalOwner创建同一Foot Placement Runtime
- [x] 15.15 为SimulatedActor创建同一Foot Placement Runtime
- [x] 15.16 为ObservedActor创建同一Foot Placement Runtime
- [x] 15.17 保持Camera capability不决定Foot Placement算法
- [x] 15.18 保持Body SourceMode不决定Foot Placement算法
- [x] 15.19 确认Authority无Presentation产品不创建Foot Placement
- [x] 15.20 确认没有另一个Factory或MonoBehaviour创建同类runtime

## 16. Host、Preview与角色Role装配

- [x] 16.1 在最新`CharacterPipelineHost`结构中增加显式Foot Placement Profile引用
- [x] 16.2 在Host中增加显式solver adapter组件引用
- [x] 16.3 在Host中增加显式rig和self-collider binding引用
- [x] 16.4 在Host配置校验中检查Profile、adapter和rig完整性
- [x] 16.5 让Host只把绑定传给Factory而不执行Foot Placement算法
- [x] 16.6 更新LocalOwner registration创建参数
- [x] 16.7 更新无相机SimulatedActor registration创建参数
- [x] 16.8 更新ServerAuthoritative observed actor创建参数
- [x] 16.9 更新DeterministicRollback完整模拟actor创建参数
- [x] 16.10 确认项目已删除PreviewSimulationActorRegistration且不为Foot Placement恢复第二套Preview Simulation
- [x] 16.11 让Play Mode完整Gameplay角色复用正式Foot Placement Pass
- [x] 16.12 保持纯动画PreviewPlaybackEngine不创建假Body或地面
- [x] 16.13 在纯动画Preview状态明确显示Foot Placement不可用
- [x] 16.14 合并`add-corin-targeted-motion-warp-demo`可能已完成的Host role参数
- [x] 16.15 搜索并删除按Camera、Actor名或Network Model猜测IK配置的路径

## 17. Corin正式资产配置

- [x] 17.1 盘点Corin VisualRoot下pelvis、左右hip/knee/ankle/toe的准确Transform
- [x] 17.2 确认Corin骨架可建立两条非退化Limb IK链
- [x] 17.3 在Corin prefab添加唯一`CharacterFootPlacementRig`
- [x] 17.4 显式绑定Corin VisualRoot与pelvis
- [x] 17.5 显式绑定左hip/knee/ankle/toe
- [x] 17.6 显式绑定右hip/knee/ankle/toe
- [x] 17.7 配置左右sole heel/toe offset和forward axis
- [x] 17.8 在Corin prefab添加左LimbIK并绑定精确链
- [x] 17.9 在Corin prefab添加右LimbIK并绑定精确链
- [x] 17.10 在Corin prefab添加唯一Final IK adapter并绑定两个solver
- [x] 17.11 确认两个solver不会自主Update/LateUpdate
- [x] 17.12 创建Corin `CharacterFootPlacementProfile`正式资产
- [x] 17.13 配置Corin Ground layer、self filtering和trace参数
- [x] 17.14 配置Corin contact、prediction、constraint和pelvis参数
- [x] 17.15 配置Corin Base PoseSourceLayerId
- [x] 17.16 从Agent Snapshot列出Base layer全部Animation Clip stable identity
- [x] 17.17 为Idle、Walk、Run和MovingTurn片段配置显式Foot Placement Weight曲线
- [x] 17.18 为Dodge、Attack和Recovery片段配置压低与恢复的Timeline曲线
- [x] 17.19 确认全部可达Animation Clip曲线存在且Profile已无producer policy数据
- [x] 17.20 将Corin Host显式绑定Profile、rig和adapter
- [x] 17.21 将Local、网络Client和Rollback Peer使用的Corin角色引用统一迁移
- [x] 17.22 搜索并删除旧prefab变体上的重复Final IK/Grounder组件

## 18. Editor Inspector与配置诊断

- [x] 18.1 为`CharacterFootPlacementProfile`建立分组Inspector
- [x] 18.2 在Inspector显示Trace、Contact、Prediction、Constraint、Pelvis和Rotation设置
- [x] 18.3 在Timeline Animation Clip Inspector显示Foot Placement曲线作者入口
- [x] 18.4 使用Unity正式CurveField编辑normalized key
- [x] 18.5 在Projection编译诊断中标出missing、空或非法曲线
- [x] 18.6 保持Profile Inspector不读取或编辑Projection producer表
- [x] 18.7 保持Profile Inspector不修改AnimationPresentationProfile、Timeline或Graph
- [x] 18.8 在Host Inspector显示Foot Placement Profile、rig和solver binding状态
- [x] 18.9 为无效骨骼层级、LayerMask和自主solver更新显示精确错误
- [x] 18.10 保持Undo和dirty owner属于实际Profile或prefab asset
- [x] 18.11 不创建独立Foot Placement EditorWindow
- [x] 18.12 不向Graph或Timeline页签栈增加IK配置副本

## 18A. BTSMTL Agent Timeline曲线闭环

- [x] 18A.1 将Agent外部Schema提升为v11并删除v10 parser口径
- [x] 18A.2 在Snapshot Timeline Clip输出单一Foot Placement Weight曲线及完整key数据
- [x] 18A.3 新增按stable timeline/track/clip identity配置单一曲线的Patch operation
- [x] 18A.4 同步typed command、lowerer、handler catalog和handler
- [x] 18A.5 通过正式Timeline mutation API写曲线且不直接维护第二套序列化数据
- [x] 18A.6 在Graph Validator校验曲线存在、有限、有序且位于`[0,1]`
- [x] 18A.7 更新MCP bridge schema说明与项目BTSMTL Agent skill合同
- [x] 18A.8 对Corin执行export_snapshot、dry_run_patch、apply_patch、export_snapshot和validate

## 19. Runtime diagnostics与性能边界

- [x] 19.1 定义Foot Placement只读frame snapshot
- [x] 19.2 记录actor、render frame、Body ticks和ResetSequence
- [x] 19.3 记录PoseSourceLayer和visible producer contribution
- [x] 19.4 记录左右foot constraint state和transition reason
- [x] 19.5 记录相对速度、descending和surface distance
- [x] 19.6 记录predicted footprint和look-ahead
- [x] 19.7 记录support candidate/reject计数和最终surface identity
- [x] 19.8 记录lock anchor、slide和replant误差
- [x] 19.9 记录单一作者policy weight、constraint state和solver weight
- [x] 19.10 记录pelvis target/current offset和support foot
- [x] 19.11 记录Final IK Apply结果和同帧重复保护
- [x] 19.12 增加`FootPlacement.Plan` Profiler marker
- [x] 19.13 增加`FootPlacement.Query` Profiler marker
- [x] 19.14 增加`FootPlacement.Solve` Profiler marker
- [x] 19.15 将snapshot接入现有RuntimeDebugSession/Host Live Debug
- [x] 19.16 让Scene gizmo只读取最新snapshot
- [x] 19.17 搜索并删除diagnostics重新query或修改runtime状态的路径
- [x] 19.18 搜索Foot Placement hot path中的LINQ、反射、字符串查找和临时容器
- [x] 19.19 确认query、candidate和snapshot buffer按actor复用

## 20. Reset、错误和单向边界清理

- [x] 20.1 在Initialization时清除双脚与pelvis历史
- [x] 20.2 在CommittedBranchReplacement时清除旧surface anchor
- [x] 20.3 在SelectedStreamReset时清除旧surface anchor
- [x] 20.4 在Presentation Reset时清除Foot Placement全部状态
- [x] 20.5 在Dispose时清除surface和solver引用
- [x] 20.6 在动画无正式输出时清除残留IK权重
- [x] 20.7 对超限pose/root不连续提供显式reset reason
- [x] 20.8 保持正常producer crossfade不硬reset
- [x] 20.9 为Profile缺失和非法参数提供fail-fast错误
- [x] 20.10 为rig缺失、重复或跨Actor提供fail-fast错误
- [x] 20.11 为Ground LayerMask和PhysicsScene缺失提供fail-fast错误
- [x] 20.12 为Final IK未初始化或自主更新提供fail-fast错误
- [x] 20.13 搜索并确认Foot Placement不写VisualRoot
- [x] 20.14 搜索并确认Foot Placement不写Character/World state
- [x] 20.15 搜索并确认Foot Placement不产生GameplayFact或PresentationCommand
- [x] 20.16 搜索并确认Network、Solver和Program不引用Foot Placement类型
- [x] 20.17 搜索并确认没有按名称、Tag、Action或State硬编码IK策略
- [x] 20.18 搜索并确认没有单Ray fallback或默认LayerMask

## 21. 文档、编译与严格校验

- [x] 21.1 更新`openspec/project.md`的Presentation运行顺序为Body、Animation、Pose Post Process、Camera
- [x] 21.2 更新`openspec/project.md`说明Foot Placement属于纯Unity表现且不进入网络或Simulation
- [x] 21.3 更新current `character-animation-pipeline` spec为最终实现真相
- [x] 21.4 更新current `character-pipeline-runtime` spec为最终实现真相
- [x] 21.5 更新current `character-animation-presentation-authoring` spec以区分动画播放Profile与Foot Placement Profile
- [x] 21.6 安装new `character-foot-placement-presentation` current spec内容
- [x] 21.7 更新implementation inventory记录最终程序集、类型、资产和参数owner
- [x] 21.8 记录`add-timeline-animation-marker-sync`最终未向Foot Placement提供contact/plant输入的核对结果
- [x] 21.9 记录Final IK vendor源码未修改且自主Grounder路径未使用
- [x] 21.10 使用规定参数编译`RootMotion.csproj`

- [x] 21.11 编译后立即执行`dotnet build-server shutdown`
- [x] 21.12 使用规定参数编译`ThirdPersonCharacter.Presentation.FinalIK.csproj`
- [x] 21.13 编译后立即执行`dotnet build-server shutdown`
- [x] 21.14 使用规定参数编译`ThirdPersonClient.Runtime.csproj`
- [x] 21.15 编译后立即执行`dotnet build-server shutdown`
- [x] 21.16 使用规定参数编译`ThirdPersonClient.Editor.csproj`
- [x] 21.17 编译后立即执行`dotnet build-server shutdown`
- [x] 21.18 编译受影响的ServerAuthoritative与DeterministicRollback Unity程序集
- [x] 21.19 每次编译后立即执行`dotnet build-server shutdown`
- [x] 21.20 运行`openspec validate add-predictive-foot-placement-presentation-pass --strict --no-interactive`
- [x] 21.21 确认全部任务真实完成后再将本文件所有任务标记为`[x]`

## 22. Timeline Animation Clip曲线可视化

- [x] 22.1 记录当前曲线只在选中Clip后的内部Inspector可见且时间轴无法核对的缺口。
- [x] 22.2 在proposal、design、delta spec和current spec定义单一Foot Placement Weight曲线子轨。
- [x] 22.3 将AnimationTrack组合行布局扩展为Clip、Marker Sync和默认折叠的Curves分组。
- [x] 22.4 为Foot Placement表现权重定义稳定曲线通道描述。
- [x] 22.5 按Animation Clip的StartFrame与EndFrame绘制归一化曲线。
- [x] 22.6 让多个Clip分别绘制自身曲线且不生成预混合作者数据。
- [x] 22.7 让点击曲线段选择其唯一Animation Clip并打开现有内部Inspector。
- [x] 22.8 让曲线编辑、Clip移动、缩放和Timeline刷新重绘曲线子轨。
- [x] 22.9 让Track Handle显示与单一Foot Placement Weight曲线子轨对齐的只读通道标签。
- [x] 22.10 保持曲线子轨不进入TimelineData.Tracks、不获得AuthoringId且不执行Tick。
- [x] 22.11 通过正式Agent Snapshot核对Corin全部可达Animation Clip拥有显式Foot Placement Weight曲线。
- [x] 22.12 更新implementation inventory记录曲线子轨与单一数据所有权。
- [x] 22.13 编译BTSMTL Timeline Runtime与Editor程序集并立即shutdown build server。
- [x] 22.14 编译ThirdPersonClient Runtime与Editor程序集并立即shutdown build server。
- [x] 22.15 编译Assembly-CSharp与Assembly-CSharp-Editor并立即shutdown build server。
- [x] 22.16 运行两个受影响change的OpenSpec strict validate。
- [x] 22.17 确认未运行Unity batchmode、未新增测试且未形成第二份曲线数据。

## 23. Unreal风格Timeline曲线可读性修正

- [x] 23.1 更新proposal、design、delta spec与current spec，明确Curves分组、参考线、插值曲线、key和Clip遮挡约束。
- [x] 23.2 将Timeline Clip视图限制在Clip行高度，删除覆盖Marker与Curves区域的100%高度。
- [x] 23.3 将Animation Track名称与图标限制在主行，避免Track标题覆盖曲线标签。
- [x] 23.4 在Animation Track左右两侧增加对齐的Curves分组标题。
- [x] 23.5 为单一曲线行设置稳定高度并绘制0、0.5与1参考线。
- [x] 23.6 按每个Animation Clip的原始AnimationCurve绘制插值曲线和全部key。
- [x] 23.7 保持点击曲线行按帧选择唯一Clip并定位同一Inspector字段。
- [x] 23.8 统一Track View、Track Handle、滚动范围和拖动重排的组合高度。
- [x] 23.9 确认Curves UI只读取AnimationClip曲线且不生成第二份数据。
- [x] 23.10 使用规定参数编译BTSMTL Timeline Runtime与Editor程序集并立即shutdown build server。
- [x] 23.11 使用规定参数编译ThirdPersonClient Runtime与Editor程序集并立即shutdown build server。
- [x] 23.12 使用规定参数编译Assembly-CSharp与Assembly-CSharp-Editor并立即shutdown build server。
- [x] 23.13 运行两个受影响change的OpenSpec strict validate。
- [x] 23.14 确认未运行Unity batchmode且未新增测试。

## 24. Foot Placement单曲线与折叠编辑收敛

- [x] 24.1 审计Corin全部可达Animation Clip的四条旧曲线是否逐项一致。
- [x] 24.2 更新proposal、design、delta spec与current spec为单一Foot Placement Weight曲线。
- [x] 24.3 删除Animation Clip的Prediction、Pelvis与Rotation曲线字段和mutation API。
- [x] 24.4 将Presentation Projection采样收敛为单一Foot Placement Weight。
- [x] 24.5 让Prediction、Pelvis与Rotation继续消费Profile算法参数和同一作者权重。
- [x] 24.6 将Foot Placement diagnostics收敛为单一作者权重。
- [x] 24.7 将Agent Snapshot/Patch合同升级为v13并删除旧四曲线operation。
- [x] 24.8 同步Agent exporter、lowerer、typed command、handler与validator。
- [x] 24.9 更新BTSMTL Agent skill和current contract为v13单曲线合同。
- [x] 24.10 为AnimationTrack Curves建立默认折叠的editor-only状态。
- [x] 24.11 统一Track View、Track Handle、滚动范围与重排的折叠高度。
- [x] 24.12 将Curves分组收敛为单一Foot Placement Weight曲线行。
- [x] 24.13 支持直接拖动现有key并提交同一Timeline Undo事务。
- [x] 24.14 支持双击曲线段增加key和右键删除非唯一key。
- [x] 24.15 使用正式Agent Snapshot核对Corin旧四曲线，并原位保留唯一`FootPlacementCurve`序列化数据。
- [x] 24.16 使用正式Agent v13 dry-run与重新导出的Snapshot验证单曲线合同。
- [x] 24.17 编译BTSMTL Timeline Runtime与Editor程序集并立即shutdown build server。
- [x] 24.18 编译ThirdPersonClient Runtime与Editor程序集并立即shutdown build server。
- [x] 24.19 编译Assembly-CSharp与Assembly-CSharp-Editor并立即shutdown build server。
- [x] 24.20 运行受影响change的OpenSpec strict validate。
- [x] 24.21 确认未运行Unity batchmode、未新增测试且未保留旧schema或四曲线路径。
- [x] 24.22 复核曲线key拖动的pointer capture、Inspector选择与视图重建时序。
- [x] 24.23 将拖动期间的正式Clip写入改为曲线行本地预览。
- [x] 24.24 在Pointer Up或Capture Out时以单一Undo事务提交最后预览值。
- [x] 24.25 在Pointer Cancel时丢弃预览且不修改Timeline资产。
- [x] 24.26 编译Timeline Editor程序集并立即shutdown build server。
- [x] 24.27 重新运行受影响change的OpenSpec strict validate。

## 25. 预测接触事实与脚底支撑修正

- [x] 25.1 将动画分析的Plant速度语义从脚底局部总速度收敛为不受InPlace水平运动污染的垂直速度。
- [x] 25.2 从同一Rig Calibration绑定姿势生成稳定脚底地面参考高度，删除“每个clip最低点就是地面”的误判路径。
- [x] 25.3 让Editor Analyzer分别采样heel/toe，以实际最低接触点生成高度事实，保留中点轨迹用于速度与landing offset。
- [x] 25.4 提升Foot Analysis algorithm identity并使旧artifact自然stale，不保留旧算法读取兼容。
- [x] 25.5 在Animation Pose Contribution中传递Marker Sync后的连续视觉时间倍率。
- [x] 25.6 按每个visible producer的视觉时间倍率修正sole velocity和landing delay，暂停或时间重定位时不伪造未来落地。
- [x] 25.7 在Runtime Contact中将生成局部脚速与Body可见线速度、yaw角速度合成世界接触点速度。
- [x] 25.8 让Sliding稳定判断复用同一世界接触速度，删除InPlace局部水平速度分裂口径。
- [x] 25.9 分别保留heel和toe的Current Support，删除相同PathFraction将两者折叠为单点的实现。
- [x] 25.10 从heel/toe有限support构造唯一脚底virtual support plane，并显式选择其移动surface owner。
- [x] 25.11 让Foot Rotation与Heel Lift消费heel/toe高差和合法support normal，不再从单一命中点猜脚掌姿态。
- [x] 25.12 禁止Replant超限释放后在同一表现帧满权重重新锁定，沿现有Free与solve weight完成连续释放再提交。
- [x] 25.13 保证单一Foot Placement Weight对位置、旋转、摆脚净空和Pelvis只应用一次。
- [x] 25.14 同步Runtime diagnostics显示visual time scale、合成世界脚速与heel/toe support identity。
- [x] 25.15 更新proposal、design、delta spec、current spec和maturity research为修正后唯一语义。
- [x] 25.16 重建Corin Foot Analysis artifact、Presentation Projection和相关正式产物，不直接编辑generated asset。
- [x] 25.17 使用规定参数依次编译受影响Runtime、FinalIK与Editor程序集，每次后立即shutdown build server。
- [x] 25.18 执行正式v15 CharacterController export_snapshot与validate，确认Agent只读Foot Analysis合同没有分裂。
- [x] 25.19 运行`openspec validate add-predictive-foot-placement-presentation-pass --strict --no-interactive`。
- [x] 25.20 确认未运行Unity batchmode、未新增测试且未保留旧Foot Analysis算法或单点support路径。

## 26. Pelvis组件空间补偿修正

- [x] 26.1 对照UE Foot Placement的Actor Movement Compensation与Pelvis组件空间语义。
- [x] 26.2 核对Corin `Bip001 Pelvis`父骨预旋转并确认local Y不等于VisualRoot up轴。
- [x] 26.3 将计划字段从`PelvisLocalVerticalOffset`收敛为`PelvisComponentVerticalOffset`。
- [x] 26.4 在Final IK adapter应用前把组件空间竖直向量转换到pelvis父骨空间。
- [x] 26.5 对缺失pelvis parent与非有限转换结果保持fail-fast。
- [x] 26.6 更新proposal、design、implementation inventory、maturity research、delta spec与current spec的坐标空间合同。
- [x] 26.7 使用规定参数编译Runtime与Final IK程序集并立即shutdown build server。
- [x] 26.8 运行`openspec validate add-predictive-foot-placement-presentation-pass --strict --no-interactive`。

## 27. 最终姿态帧输入与方向化Pelvis收口

- [x] 27.1 将Pose Post Process输入替换为带有效lease的同帧`FinalAnimationPoseFrame`。
- [x] 27.2 从Projection Pose Program一次绑定`animation.foot-placement-weight`的identity、dense index与ProgramHash。
- [x] 27.3 删除Foot Profile的`PoseSourceLayerId`、producer binding和Projection二次采样入口。
- [x] 27.4 让Foot Runtime直接读取最终Foot Features与最终归一化Foot Placement Weight。
- [x] 27.5 将Diagnostics、Formatter、Feature Frame和Inspector迁到最终姿态帧合同。
- [x] 27.6 为Pelvis Profile增加`AllPlantedFeet`与`DirectionalSlopeSupport`模式及显式方向证据阈值。
- [x] 27.7 将Corin Profile迁到`DirectionalSlopeSupport`并删除旧Base Layer配置。
- [ ] 27.8 将Runtime snapshot和trace迁到最终Pose Program、Completion、Continuity、参数与source contribution身份。
- [ ] 27.9 实现上坡选择前方plant脚、下坡选择较低plant脚的方向化Pelvis决策。
- [ ] 27.10 对缺少方向、脚前后顺序或坡面高度差的情况输出typed Neutral或Unavailable原因。
- [ ] 27.11 将Pelvis mode、decision、reason与support foot接入统一diagnostics。
- [ ] 27.12 更新proposal、design、delta spec与current spec，删除visible Layer重采样和隐式双脚平均描述。
- [ ] 27.13 使用规定参数编译受影响Runtime与Editor程序集并立即shutdown build server。
- [ ] 27.14 运行受影响OpenSpec strict validate并确认没有旧Layer、二次采样或兼容路径。
