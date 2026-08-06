## 0. 固定实施门禁

- [x] 0.1 记录本change唯一正式链为FinalIK Grounding-backed PredictiveFootPlacement Goal生成与单次FullBodyIK Pose求解。
- [x] 0.2 禁止创建shadow skeleton GameObject层级。
- [x] 0.3 禁止创建FinalIK target GameObject。
- [x] 0.4 禁止在角色Prefab挂FullBodyBipedIK组件。
- [x] 0.5 禁止在角色Prefab挂GrounderFBBIK组件或复制Grounder配置。
- [x] 0.6 禁止通过Update、LateUpdate或OnAnimatorIK执行正式IK。
- [x] 0.7 禁止保留TwoBoneIK或LegIK作为Runtime fallback。
- [x] 0.8 禁止复制FinalIK核心求解方程到项目自有solver类。
- [x] 0.9 约定任一硬门禁失败时停止后续实施并报告精确依赖。
- [x] 0.10 保持唯一Animancer Evaluate Barrier与唯一Physical Transform final writer。
- [x] 0.11 保持PredictiveFootPlacement为唯一world query与Foot contact owner。
- [x] 0.12 禁止把PredictiveFootPlacement或PoseBoneIKGoals标为IK solver。
- [x] 0.13 禁止把两个Goal Source的generated调度顺序表述为串行IK。
- [x] 0.14 禁止运行时在FinalIK Grounding结果与项目重复Grounding结果之间择优。

## 1. 审计FinalIK Grounding与成熟求解器边界

- [x] 1.1 枚举FBBIK正式参与的RootMotion源码文件。
- [x] 1.2 计算参与文件的稳定内容hash。
- [x] 1.3 生成FBBIK backend source identity。
- [x] 1.4 枚举IKSolverFullBodyBiped中的直接Transform字段。
- [x] 1.5 枚举IKSolverFullBody中的直接Transform读写。
- [x] 1.6 枚举FBIKChain与ChildConstraint中的直接Transform读写。
- [x] 1.7 枚举IKEffector中的直接Transform读写。
- [x] 1.8 枚举IKConstraintBend中的直接Transform读写。
- [x] 1.9 枚举IKMappingSpine中的直接Transform读写。
- [x] 1.10 枚举IKMappingLimb中的直接Transform读写。
- [x] 1.11 枚举IKMappingBone与BoneMap中的直接Transform读写。
- [x] 1.12 区分允许改造的I/O、初始化、mapping方法与禁止改写的核心方程。
- [x] 1.13 确认backend seam不要求修改Push、Reach、Stage1或Stage2数学。
- [x] 1.14 确认backend seam不要求修改FABRIK或trigonometric pass数学。
- [x] 1.15 确认backend seam不要求修改effector或bend constraint数学语义。
- [x] 1.16 确认Pose Buffer backend不需要任何中间Physical Transform写入。
- [x] 1.17 确认Pose Buffer backend正常帧不需要创建managed solver对象。
- [x] 1.18 确认Pose Buffer backend正常帧不需要创建managed集合。
- [x] 1.19 确认Corin Rig可满足FBBIK完整biped references。
- [x] 1.20 在全部审计结论中记录FinalIK不具备UE PBIK完整Bone Settings。
- [x] 1.21 枚举Grounding正式参与的RootMotion源码文件。
- [x] 1.22 把Grounding参与文件纳入稳定内容hash与backend source identity。
- [x] 1.23 枚举Grounding中的Transform读取。
- [x] 1.24 枚举Grounding中的Time.time和Time.deltaTime读取。
- [x] 1.25 枚举Grounding中的Physics.Raycast、SphereCast与CapsuleCast入口。
- [x] 1.26 枚举Grounding.Leg当前脚query组合。
- [x] 1.27 枚举Grounding.Leg velocity prediction数学。
- [x] 1.28 枚举Grounding.Leg命中点和平面到脚高的数学。
- [x] 1.29 枚举Grounding.Leg坡面rotation与maximum angle数学。
- [x] 1.30 枚举Grounding.Leg foot interpolation数学。
- [x] 1.31 枚举Grounding.Pelvis lower/lift offset数学。
- [x] 1.32 枚举Grounding.Pelvis interpolation与damper数学。
- [x] 1.33 枚举GrounderFBBIK pelvis-before-effectors应用顺序。
- [x] 1.34 逐项记录FinalIK Grounding不提供动画Foot Feature与source contribution。
- [x] 1.35 逐项记录FinalIK Grounding不提供相位Future Landing与Ground Envelope。
- [x] 1.36 逐项记录FinalIK Grounding不提供surface identity与moving anchor。
- [x] 1.37 逐项记录FinalIK Grounding不提供Free、Locked与Sliding生命周期。
- [x] 1.38 确认Grounding seam不要求复制脚高、rotation或foot interpolation数学，并确认stock pelvis缺少逐腿可达区间。
- [x] 1.39 确认Grounding seam能够使用精确PhysicsScene、自碰撞排除和fixed hit page。
- [x] 1.40 确认Grounding与Project Predictive Extension不存在重复结果权威。

## 2. 建立FinalIK中立Grounding与Bone Backend

- [x] 2.1 定义Grounding显式frame delta输入。
- [x] 2.2 定义Grounding显式root Component Transform输入。
- [x] 2.3 定义Grounding显式heel、toe、ankle和foot Component Transform输入。
- [x] 2.4 定义FinalIK Grounding固定容量current-foot query输入。
- [x] 2.5 为current-foot输入定义heel采样数据。
- [x] 2.6 为current-foot输入定义toe与foot-center采样数据。
- [x] 2.7 定义Project Predictive Extension固定容量Future Landing请求。
- [x] 2.8 定义Project Predictive Extension固定容量Path Sample请求。
- [x] 2.9 定义Grounding world-query backend接口。
- [x] 2.10 让world-query backend显式接收PhysicsScene和LayerMask。
- [x] 2.11 让world-query backend使用self-collider filter。
- [x] 2.12 让world-query backend写固定命中page与stable surface identity。
- [x] 2.12A 让Grounding与Predictive Extension共用同一个world-query backend和命中合同。
- [x] 2.13 让Grounding.Leg通过request而非Transform位置执行既有query数学。
- [x] 2.14 让Grounding.Leg通过frame delta而非Time.time执行既有history数学。
- [x] 2.15 让Grounding.Leg发布current hit、foot height与rotation结果值。
- [x] 2.16 审计Grounding.Pelvis显式root delta与damper行为，确认其不适合作为正式Actor movement compensation。
- [x] 2.17 从Grounding adapter正式结果删除raw与smoothed stock pelvis offset。
- [x] 2.18 为Transform调用链实现vendor Grounding backend适配。
- [x] 2.19 保持FinalIK自带Grounder只使用vendor Transform backend。
- [x] 2.20 为项目PredictiveFootPlacement实现唯一Grounding adapter。
- [x] 2.21 删除项目Grounding adapter中的GameObject、Transform、Time和默认Physics依赖。
- [x] 2.22 为Grounding adapter安装source identity与revision检查。
- [x] 2.23 确认Grounding正常帧不创建delegate、array、list或Grounding对象。

### 2A. 建立FBBIK indexed bone backend

- [x] 2A.1 在RootMotion可见边界定义稳定indexed bone handle。
- [x] 2A.2 定义读取Component Position的backend接口。
- [x] 2A.3 定义读取Component Rotation的backend接口。
- [x] 2A.4 定义写入Component Position的backend接口。
- [x] 2A.5 定义写入Component Rotation的backend接口。
- [x] 2A.6 定义读取Local Position与Rotation的backend接口。
- [x] 2A.7 定义读取Parent handle的backend接口。
- [x] 2A.8 定义读取Reference Pose的backend接口。
- [x] 2A.9 让IKSolver Point和Node使用bone handle而非硬依赖Transform identity。
- [x] 2A.10 让FBIKChain通过backend读取bone pose。
- [x] 2A.11 让ChildConstraint通过backend读取两端位置。
- [x] 2A.12 让IKEffector通过backend读取目标骨骼与plane bones。
- [x] 2A.13 让IKEffector通过backend写solver node结果。
- [x] 2A.14 让IKConstraintBend通过backend读取三骨姿势。
- [x] 2A.15 让IKMappingSpine通过backend读写Pose。
- [x] 2A.16 让IKMappingLimb通过backend读写Pose。
- [x] 2A.17 让IKMappingBone通过backend读写Pose。
- [x] 2A.18 让BoneMap通过backend执行既有mapping数学。
- [x] 2A.19 为Transform调用链实现vendor Transform backend适配。
- [x] 2A.20 保持FinalIK自带组件只使用vendor Transform backend。
- [x] 2A.21 为项目Component Pose实现唯一Pose Buffer backend。
- [x] 2A.22 让项目backend按Rig stable bone index绑定。
- [x] 2A.23 让项目backend拒绝Virtual Bone写入。
- [x] 2A.24 让项目backend在Physical写入后重建受影响Virtual Bone。
- [x] 2A.25 删除项目backend中的Transform、GameObject和Component依赖。
- [x] 2A.26 为backend安装稳定source identity与revision检查。

## 3. 升级Animation Rig v4

- [x] 3.1 将Rig schema唯一升级为`character-animation-rig/v4`。
- [x] 3.2 在Rig v4增加Solver Root BoneId。
- [x] 3.3 在Rig v4增加ordered Spine BoneId列表。
- [x] 3.4 在Rig v4增加Left Arm语义结构。
- [x] 3.5 在Left Arm增加可选Clavicle BoneId。
- [x] 3.6 在Left Arm增加Upper Arm、Forearm与Hand BoneId。
- [x] 3.7 在Rig v4增加Right Arm语义结构。
- [x] 3.8 在Right Arm增加可选Clavicle BoneId。
- [x] 3.9 在Right Arm增加Upper Arm、Forearm与Hand BoneId。
- [x] 3.10 在Rig v4增加可选Head BoneId。
- [x] 3.11 保留Pelvis与左右Hip、Knee、Ankle、Toe语义。
- [x] 3.12 校验Solver Root只属于Pelvis或Spine。
- [x] 3.13 校验Spine BoneId有序且父子连续。
- [x] 3.14 校验左右Arm链Physical且父子关系合法。
- [x] 3.15 校验左右Leg链Physical且父子关系合法。
- [x] 3.16 校验全部biped语义BoneId唯一且属于同一Rig。
- [x] 3.17 校验四条limb reference segment长度有限且为正。
- [x] 3.18 校验四条limb reference bend plane非退化。
- [x] 3.19 删除Rig v3 schema reader和兼容分支。
- [x] 3.20 更新Rig Inspector的Root、Spine、Arms、Legs与Head分组。
- [x] 3.21 让Rig Inspector只从Physical Bone catalog选择语义。
- [x] 3.22 增加显式FinalIK FBBIK Rig validation命令。
- [x] 3.23 禁止Rig validation在OnInspectorGUI、selection或repaint执行重操作。

## 4. 收敛Foot Calibration v4

- [x] 4.1 将Foot Calibration schema唯一升级为v4。
- [x] 4.2 保留左右Heel Contact字段。
- [x] 4.3 保留左右Toe Contact字段。
- [x] 4.4 保留由Heel、Toe与Visual Up派生的Sole Frame。
- [x] 4.5 删除Left Preferred Bend Direction字段。
- [x] 4.6 删除Right Preferred Bend Direction字段。
- [x] 4.7 删除Calibration content hash中的bend direction。
- [x] 4.8 删除Geometry Validation中的Reference Bend Direction。
- [x] 4.9 删除Geometry Validation中的Preferred Bend Direction。
- [x] 4.10 删除Knee Bend Scene gizmo和只读箭头。
- [x] 4.11 删除Knee Bend Inspector说明与错误字段。
- [x] 4.12 保持Calibration Preview只服务Heel、Toe与Sole几何。
- [x] 4.13 升级Geometry Validation identity以锁定Rig v4与Calibration v4。
- [x] 4.14 删除Calibration v3 reader和旧schema兼容。

## 5. 更新Foot Analysis Artifact合同

- [x] 5.1 把Analyzer输入升级为Rig v4与Calibration v4。
- [x] 5.2 从artifact identity删除Preferred Bend字段。
- [x] 5.3 把Rig v4 full-biped revision与content hash写入artifact identity。
- [x] 5.4 把Calibration v4 geometry validation identity写入artifact identity。
- [x] 5.5 升级Foot Analysis algorithm identity。
- [x] 5.6 升级Foot Analysis artifact format。
- [x] 5.7 让旧Rig v3或Calibration v3 artifact直接报告Stale。
- [x] 5.8 保持Heel、Toe、Sole、contact marker与Foot Feature算法不变。
- [x] 5.9 保持Analyzer不读取FullBodyIK Profile。
- [x] 5.10 保持Analyzer不创建FinalIK solver或执行Pose求解。

## 6. 定义Full Body IK作者与Runtime合同

- [x] 6.1 新增`CharacterFullBodyIkProfile`资产。
- [x] 6.2 为Profile增加稳定schema、identity与revision。
- [x] 6.3 为Profile增加Iterations。
- [x] 6.4 为Profile增加FABRIK Pass。
- [x] 6.5 为Profile增加Spine Stiffness。
- [x] 6.6 为Profile增加Pull Body Vertical与Horizontal。
- [x] 6.7 为每条chain增加Pin。
- [x] 6.8 为每条chain增加Pull。
- [x] 6.9 为每条chain增加Push。
- [x] 6.10 为每条chain增加Push Parent。
- [x] 6.11 为每条chain增加Reach与Smoothing。
- [x] 6.12 为每条limb mapping增加Weight。
- [x] 6.13 为每条limb mapping增加Maintain Rotation Weight。
- [x] 6.14 为每条limb增加Bend Constraint Weight与Clamp。
- [x] 6.15 为Profile增加全局Node Weight。
- [x] 6.16 校验全部Profile值有限且位于FinalIK真实支持范围。
- [x] 6.17 禁止Profile序列化UE PBIK Stiffness字段。
- [x] 6.18 禁止Profile序列化UE PBIK Rotation Limit字段。
- [x] 6.19 禁止Profile序列化UE Preferred Angle字段。
- [x] 6.20 禁止Profile序列化runtime fallback solver枚举。
- [x] 6.21 在Presentation Profile增加唯一FullBodyIK Profile引用。
- [x] 6.22 让Profile revision参与Projection source revision。

### 6A. 收敛Foot Placement Profile

- [x] 6A.1 保持现有CharacterFootPlacementProfile为唯一Foot Placement配置资产。
- [x] 6A.2 在作者表面增加FinalIK Grounding设置分组。
- [x] 6A.3 在作者表面增加Project Predictive Extension设置分组。
- [x] 6A.3A 显式暴露FinalIK Grounding Quality。
- [x] 6A.4 把Ground Layer与Max Step映射到FinalIK Grounding语义。
- [x] 6A.5 把Foot Radius与velocity prediction基线映射到FinalIK Grounding语义。
- [x] 6A.6 把Foot Height Speed映射到FinalIK Grounding语义。
- [x] 6A.7 把Foot Rotation Weight、Speed与Maximum Angle映射到FinalIK Grounding语义。
- [x] 6A.8 删除FinalIK stock Pelvis Speed、Damper、Lower与Lift作者字段，并增加逐腿Pelvis Reach Planner配置。
- [x] 6A.9 把Root Cast Radius与Overstep policy映射到FinalIK Grounding语义。
- [x] 6A.10 保留动画相位look-ahead与future distance为Predictive Extension。
- [x] 6A.11 保留path sample与surface continuity为Predictive Extension。
- [x] 6A.12 保留contact、lock、slide与moving anchor为Predictive Extension。
- [x] 6A.13 保留leg reach与source contribution为Predictive Extension。
- [x] 6A.14 删除与FinalIK重复的旧Foot rotation计算配置命名。
- [x] 6A.15 删除与FinalIK重复的旧Foot height smoothing配置命名。
- [x] 6A.16 删除stock pelvis smoothing配置命名，保留唯一Planner插值速度、dead zone与最大升降范围。
- [x] 6A.17 禁止Profile保存Grounder组件副本或backend选择。
- [x] 6A.18 让Grounding与Predictive Extension共同进入一个Profile identity。
- [x] 6A.19 让Foot Placement Profile revision参与Projection source revision。
- [x] 6A.20 正式声明AllLegs、AllPlantedFeet与DirectionalSlopeSupport为Pelvis Height Mode。
- [x] 6A.21 正式声明FollowBody与HoldWorldDuringInterpolation为Actor Movement Compensation Mode。

## 7. 建立typed Full Body IK Goals

- [x] 7.1 定义`CharacterFullBodyIkEffectorSlot`稳定枚举。
- [x] 7.2 定义固定容量`CharacterFullBodyIkGoal`值。
- [x] 7.3 为Goal增加目标Component Position。
- [x] 7.4 为Goal增加目标Component Rotation。
- [x] 7.5 为Goal增加Position Weight。
- [x] 7.6 为Goal增加Rotation Weight。
- [x] 7.6A 为Goal增加显式Application，区分绝对effector target、Grounding effector target与pelvis pre-solve translation。
- [x] 7.7 为Goal增加Source Kind。
- [x] 7.8 为Goal增加只读diagnostic metadata index。
- [x] 7.9 定义`CharacterFullBodyIkGoalSet`header与slice。
- [x] 7.10 为Goal Set增加Frame Sequence。
- [x] 7.11 为Goal Set增加Completion Identity。
- [x] 7.12 为Goal Set增加Rig Id与Revision。
- [x] 7.13 为Goal Set增加Producer Node与Call Site lineage。
- [x] 7.14 定义稳定port type `component.full-body-ik-goals`。
- [x] 7.15 让Goal Set只存在于同帧固定workspace。
- [x] 7.16 禁止Goal Set进入作者资产、Gameplay状态或Network状态。
- [x] 7.17 在Build阶段拒绝同一FullBodyIK的重复Effector Slot。
- [x] 7.18 在Build阶段拒绝超过固定容量的Goal数量。
- [x] 7.19 在Runtime拒绝跨帧或跨Rig Goal Set。
- [x] 7.20 删除`CharacterBipedLegTargets`及其availability合同。

## 8. 新增三种Pose Graph Capability

- [x] 8.1 将旧`FootPlacement` kind替换为`PredictiveFootPlacement`。
- [x] 8.2 让PredictiveFootPlacement接收Component Pose。
- [x] 8.3 让PredictiveFootPlacement接收可选Weight参数。
- [x] 8.4 让PredictiveFootPlacement只输出Full Body IK Goals。
- [x] 8.5 删除PredictiveFootPlacement的Component Pose输出。
- [x] 8.6 新增`PoseBoneIKGoals` kind。
- [x] 8.7 让PoseBoneIKGoals接收Component Pose。
- [x] 8.8 让PoseBoneIKGoals输出Full Body IK Goals。
- [x] 8.9 为PoseBoneIKGoals定义可重排effector binding列表。
- [x] 8.10 为binding定义Effector Slot。
- [x] 8.11 为binding定义目标Pose Bone与position/rotation offset。
- [x] 8.12 为binding定义position/rotation weight。
- [x] 8.13 新增`FullBodyIK` kind。
- [x] 8.14 让FullBodyIK接收Component Pose。
- [x] 8.15 让FullBodyIK拥有稳定动态Goals输入port。
- [x] 8.16 让FullBodyIK输出Solved Component Pose。
- [x] 8.17 让FullBodyIK显式引用FullBodyIK Profile。
- [x] 8.18 从Capability Catalog删除TwoBoneIK。
- [x] 8.19 从Capability Catalog删除LegIK。
- [x] 8.20 从Capability Catalog删除`component.biped-leg-targets`。
- [x] 8.21 同步Canvas创建菜单、搜索、clipboard与typed connection policy。
- [x] 8.22 同步Details字段与References投影。
- [x] 8.23 同步Document v3 exporter、strict parser与canonical writer。
- [x] 8.24 同步Document Reconciler与typed Presentation Mutation。
- [x] 8.25 禁止旧node kind或旧target type被Document兼容读取。
- [x] 8.26 让PredictiveFootPlacement Capability声明Goal Source而非IK solver角色。
- [x] 8.27 让PredictiveFootPlacement节点显示FinalIK Grounding backend badge。
- [x] 8.28 让Canvas把Component Pose分别扇出到两个Goal Sources与FullBodyIK。
- [x] 8.29 禁止Canvas自动把两个Goal Sources排列成Pose backbone。
- [x] 8.30 让Document往返保留Pose分支与Goal value分支。

## 9. 重构Predictive Foot Placement

- [x] 9.1 保留动画Foot Feature与source contribution输入。
- [ ] 9.2 在普通Grounding基线稳定后重新接入未来落点delay与local offset预测。
- [x] 9.3 从Component Pose和Calibration构造Current Heel Grounding request。
- [x] 9.4 从Component Pose和Calibration构造Current Toe Grounding request。
- [ ] 9.5 从动画Foot Feature构造Project Predictive Extension Future Landing request并接回正式Goal输出。
- [ ] 9.6 从动画脚路径构造固定数量Project Predictive Extension Path Sample request并接回正式Goal输出。
- [x] 9.7 只通过FinalIK Grounding adapter执行current-foot Grounding输入。
- [ ] 9.7A 通过共享world-query backend执行Future Landing与Path Sample请求。
- [x] 9.7B 禁止Predictive Extension请求覆盖FinalIK Grounding当前脚高或rotation结果。
- [x] 9.8 让FinalIK Grounding提供当前脚高基础结果。
- [x] 9.9 让FinalIK Grounding提供坡面rotation基础结果。
- [x] 9.10 让FinalIK Grounding提供foot interpolation基础结果。
- [x] 9.11 禁用FinalIK stock pelvis输出，让逐腿Pelvis Reach Planner提供target与resolved pelvis结果。
- [ ] 9.12 让Project Predictive Extension构造Current Support与Future Landing Support。
- [ ] 9.13 让Project Predictive Extension构造Ground Envelope与surface continuity。
- [x] 9.14 让Project Predictive Extension维护Free、Locked与Sliding状态机。
- [x] 9.15 让Project Predictive Extension维护moving surface anchor。
- [x] 9.16 让Project Predictive Extension计算逐腿可达区间，并在目标不可满足或区间冲突时显式释放对应Foot Goal。
- [x] 9.17 删除项目重复的当前脚高计算。
- [x] 9.18 删除项目重复的坡面rotation计算。
- [x] 9.19 删除项目重复的foot interpolation计算。
- [x] 9.20 删除FinalIK stock pelvis正式输出与旧lower/lift/damper字段。
- [x] 9.20A 用逐腿可达区间统一AllLegs、AllPlantedFeet与DirectionalSlopeSupport选择。
- [x] 9.20B 用唯一Planner状态实现pelvis interpolation与显式Actor Movement Compensation。
- [x] 9.20C 让Calibration semantic sole frame只转换FinalIK rotation offset的输入输出表达。
- [x] 9.21 把pelvis结果改为PelvisPreSolveTranslation goal数据。
- [x] 9.22 把左右Ankle目标改为LeftFoot与RightFoot effectors。
- [x] 9.23 删除Animated Bend Plane输出。
- [x] 9.24 删除Preferred Bend Plane输出。
- [x] 9.25 删除Knee Direction或Bend Stabilization Weight输出。
- [x] 9.26 删除FootPlacement operation中的TranslateSubtree调用。
- [x] 9.27 禁止PredictiveFootPlacement写任意Physical或Virtual Pose Bone。
- [x] 9.28 禁止PredictiveFootPlacement创建或调用IK solver。
- [x] 9.29 禁止PredictiveFootPlacement调用GrounderFBBIK组件生命周期。
- [x] 9.30 让PredictiveFootPlacement发布匹配Pose completion的Goal Set。
- [x] 9.31 预测扩展重新接入后，为受其改变的Goal标记Project Predictive Extension来源。
- [ ] 9.32 预测扩展重新接入后，Reset清除contact、anchor和prediction历史；普通基线继续清除Grounding历史。

### 9A. 先闭合普通Foot Placement基线

- [x] 9A.1 让FinalIK Grounding当前帧结果生成LeftFoot与RightFoot Goals，让Pelvis Reach Planner生成唯一Pelvis Goal。
- [x] 9A.2 让Grounding root来自Rig物理Root的Component Pose，而不是假设Presentation VisualRoot等于骨架根。
- [x] 9A.3 在普通基线闭合阶段暂停Future Landing与Path Sample对正式Goal输出的影响；Current Support的Foot Lock、Slide与moving surface anchor只约束FinalIK Grounding当帧目标。
- [x] 9A.4 只在逐腿目标不可达或双腿区间冲突时清零不可满足Foot Goal，合法Current Grounding目标保持连续Placement Weight。
- [x] 9A.5 保持一次FinalIK FBBIK求解，不恢复TwoBoneIK、LegIK或第二求解器。
- [x] 9A.6 用有限Replant角度与Ankle Twist阈值释放失效锁点，并在同帧返回FinalIK Grounding目标。
- [x] 9A.7 在有限腿长范围内保持左右Foot Goal最小水平分离；双脚均受约束时释放次要支撑脚。
- [x] 9A.8 让不可达或过度压缩目标按PelvisRangeConflictReleased释放，避免FBBIK拉伸或骨盆无限下蹲。
- [x] 9A.9 把Replant角度、最小脚距与最大Ankle Twist接入同一Foot Placement Profile、Live Tuning与typed diagnostics。
- [x] 9A.10 在同一FinalIK Grounding owner中发布typed secondary Toe plant hit，并以同一个Foot Goal和唯一FBBIK实现有限Heel Lift与Toe Pivot；不新增第二当前地面查询权威、不复制stock Grounding数学。
- [x] 9A.11 定义`Unlocked`、`PivotAroundToe`、`PivotAroundAnkle`与`LockRotation`四种plant policy；普通基线阶段让Corin正式选择`Unlocked`，不创建anchor或toe pivot。
- [x] 9A.12 让`AdjustHeelBeforePlanting`和`HeelLiftRatio`进入唯一Foot Placement Profile、Projection identity与Pose Watch诊断，并只让连续量`HeelLiftRatio`进入Live Tuning。
- [x] 9A.13 让Foot Goal携带完整ankle目标、toe plant point与pivot weight，并在一次FBBIK中完成部分权重下的toe-preserving offset。
- [x] 9A.14 让作者Foot Placement Weight唯一控制合法Current Grounding Goal；Plant Confidence与动画Foot Speed只参与接触意图迟滞，Contact Weight只控制anchor、lock与slide。
- [x] 9A.14A 让FinalIK Grounding用root-relative height保留动画摆脚高度，禁止用actor世界脚速关闭普通跑动Foot Goal。
- [x] 9A.14B 让最小脚距修正只在左右脚都有有效Contact Weight时运行，禁止连续Position Goal把自由摆脚横向推开。
- [x] 9A.15 让Grounding leg与Pelvis Reach Planner插值全部使用调用方显式frame delta，禁止同帧混用`Time.deltaTime`。
- [x] 9A.16 让Corin普通基线关闭velocity prediction、plant anchor与提前Heel Lift，选择AllPlantedFeet与FollowBody，并以显式Character Build重建正式Projection。

## 10. 实现Pose Bone Goal Source

- [x] 10.1 编译PoseBoneIKGoals binding为稳定descriptor。
- [x] 10.2 从Rig v4解析每个目标Physical或Virtual Pose Bone index。
- [x] 10.3 在Component Pose中读取目标Bone Transform。
- [x] 10.4 应用binding position offset。
- [x] 10.5 应用binding rotation offset。
- [x] 10.6 应用position/rotation weight。
- [x] 10.7 把全部binding写入一个固定Goal Set slice。
- [x] 10.8 保持Goal Source不修改Pose。
- [x] 10.9 拒绝重复Effector Slot。
- [x] 10.10 拒绝跨Rig目标Bone。
- [x] 10.11 拒绝非法Virtual Bone依赖。
- [x] 10.12 发布Goal Source completion与diagnostics。

## 11. 编译Full Body IK Pose Plan

- [x] 11.1 新增PredictiveFootPlacement operation code。
- [x] 11.2 新增PoseBoneIKGoals operation code。
- [x] 11.3 新增FullBodyIK operation code。
- [x] 11.4 删除FootPlacement旧operation code。
- [x] 11.5 删除TwoBoneIK operation code。
- [x] 11.6 删除LegIK operation code。
- [x] 11.7 在共享execution domain合同新增WorldAwareValue与PureValue。
- [x] 11.8 编译PredictiveFootPlacement为WorldAwareValue stage。
- [x] 11.8A 编译PoseBoneIKGoals为PureValue stage。
- [x] 11.9 编译FullBodyIK为PurePose stage。
- [x] 11.9A 禁止Value stage分配Pose输出page或声明Pose write set。
- [x] 11.10 让FullBodyIK同时依赖Pose输入和全部Goal Set输入。
- [x] 11.11 编译稳定goal value index与slice capacity。
- [x] 11.12 编译动态Goal port的稳定输入顺序。
- [x] 11.13 编译FullBodyIK Profile descriptor与revision。
- [x] 11.14 编译Rig v4 full-biped dense indexes。
- [x] 11.15 编译FinalIK backend identity。
- [x] 11.16 更新Projection semantic hash与contract hash。
- [x] 11.17 更新Native Pose Program operation layout。
- [x] 11.18 更新Native workspace capacity与Pending/Committed page。
- [x] 11.19 删除BipedLegTargets workspace。
- [x] 11.20 删除TwoBoneIK和LegIK descriptor arrays。
- [x] 11.21 让Validator拒绝Predictor Goal没有FullBodyIK消费方。
- [x] 11.22 让Validator拒绝FullBodyIK缺少Pose输入。
- [x] 11.23 让Validator拒绝FullBodyIK没有任何Goal输入。
- [x] 11.24 让Validator拒绝FullBodyIK出现在Local Pose段。
- [x] 11.25 让Compiler不插入隐藏空间转换或隐藏solver。
- [x] 11.26 让PredictiveFootPlacement从原始Component Pose生成只读value分支。
- [x] 11.27 让PoseBoneIKGoals从同一Component Pose生成只读value分支。
- [x] 11.28 让FullBodyIK的Pose输入直接来自原始Component Pose backbone。
- [x] 11.29 让FullBodyIK等待全部Goal Source completion后执行一次。
- [x] 11.30 禁止Goal Source生成中间Pose completion。
- [x] 11.31 禁止Compiler建立PredictiveFootPlacement到PoseBoneIKGoals的执行数据edge。
- [x] 11.32 让compiled diagnostics把Goal Source顺序标记为调度而非IK串联。
- [x] 11.33 编译FinalIK Grounding backend identity与Foot Placement Profile revision。
- [x] 11.34 编译Grounding fixed request、hit与state workspace容量。
- [x] 11.35 编译Project Predictive Extension fixed state容量。

## 12. 执行唯一FullBodyIK Runtime

- [x] 12.1 在Actor preparation创建唯一FullBodyIK runtime workspace。
- [x] 12.1A 在Actor preparation创建唯一FinalIK Grounding runtime state。
- [x] 12.1B 在Actor preparation创建唯一Project Predictive Extension state。
- [x] 12.2 从Rig v4建立indexed biped binding。
- [x] 12.3 从Rig v4建立solver root与spine mapping。
- [x] 12.4 从Rig v4建立左右arm chains与mappings。
- [x] 12.5 从Rig v4建立左右leg chains与mappings。
- [x] 12.6 从Rig v4参考Pose初始化四肢bend constraints。
- [x] 12.7 从FullBodyIK Profile初始化全部solver settings。
- [x] 12.8 每帧把输入Component Pose绑定为只读backend page。
- [x] 12.9 每帧把输出Component Pose绑定为独立Pending page。
- [x] 12.10 在输出page应用唯一PelvisPreSolveTranslation。
- [x] 12.11 按编译顺序合并全部Goal Set。
- [x] 12.12 把Body goal写入FinalIK body effector。
- [x] 12.13 把左右手goal写入hand effectors。
- [x] 12.14 把左右脚goal写入foot effectors。
- [x] 12.14A 按GrounderFBBIK成熟语义把Foot目标写为positionOffset与FBBIK ReadPose前rotation，保持Foot effector position/rotation weight为零。
- [x] 12.15 把未提供的effectors权重明确归零。
- [x] 12.16 调用一次FinalIK FBBIK ReadPose。
- [x] 12.17 调用一次FinalIK FBBIK Solve。
- [x] 12.18 调用一次FinalIK FBBIK WritePose到Pending page。
- [x] 12.19 重建受Physical写入影响的Virtual Bone。
- [x] 12.20 发布Solved Component Pose completion。
- [x] 12.21 将非法goal、mapping、non-finite或solver失败降低为typed failure。
- [x] 12.22 在失败时阻断ComponentToLocal与FinalPublication。
- [x] 12.23 在Barrier后失败时把Actor Animation Runtime置为Faulted。
- [x] 12.24 保持失败时不恢复Physical Bone快照。
- [x] 12.25 保持正常帧零GameObject与零Transform创建。
- [x] 12.26 保持正常帧零managed集合分配。
- [x] 12.27 在Reset时清空solver帧状态而不重建solver。
- [x] 12.28 在dispose时按唯一owner释放workspace。
- [x] 12.29 让FullBodyIK只消费PredictiveFootPlacement最终Goals而不调用world query。
- [x] 12.30 让FullBodyIK不调用Grounding或GrounderFBBIK第二次生成脚目标。

## 13. 更新Preview与Diagnostics

- [x] 13.1 让Preview使用同一Rig v4与FullBodyIK Profile。
- [x] 13.2 让Preview使用同一FinalIK Pose Buffer backend。
- [x] 13.3 在缺少world context时只让PredictiveFootPlacement报告Unavailable。
- [x] 13.4 禁止Preview创建假地面或shadow skeleton。
- [x] 13.5 为Grounding Watch发布backend identity、request、hit和stock velocity prediction。
- [x] 13.6 为Grounding Watch发布脚高与坡面rotation。
- [x] 13.6A 为Predictive Extension Watch发布Foot Feature与Current/Future Support。
- [x] 13.6B 为Predictive Extension Watch发布Ground Envelope、surface anchor与constraint。
- [x] 13.6C 为Predictive Extension Watch发布左右腿允许区间、target/resolved pelvis pre-solve、冲突释放与Foot goals。
- [x] 13.7 为PoseBoneIKGoals Watch发布Virtual targets与Hand goals。
- [x] 13.8 为FullBodyIK Watch发布输入与输出Component Pose。
- [x] 13.9 为FullBodyIK Watch发布每个effector目标、权重与残差。
- [x] 13.10 为FullBodyIK Watch发布chain reach与bend constraint状态。
- [x] 13.11 为FullBodyIK Watch发布backend identity与iterations。
- [x] 13.12 删除LegIK KneeDirection、BendPlane与解析式长度诊断。
- [x] 13.13 删除TwoBoneIK旧solver diagnostic名称。
- [x] 13.14 更新Performance Capture marker为FullBodyIK单次solve。
- [x] 13.15 保持Diagnostics只复制完成workspace。
- [x] 13.16 禁止Diagnostics第二次调用FinalIK或读取Transform反推。
- [x] 13.17 让Canvas和Trace明确显示Goal Source不是IK solver。
- [x] 13.18 让Trace区分FinalIK Grounding结果与Project Predictive Extension结果。
- [x] 13.19 让统一Trace以typed payload连续发布左右脚Plant Confidence、Plant Contact迟滞、Animation Foot Speed、surface distance、Placement/Plant Support/Contact权重、Body Grounded三项来源、grounding、constraint、pelvis与FullBodyIK residual，并提供Capture CSV导出。
- [x] 13.20 让CharacterPipelineHost与FixedCharacterHost复用同一Runtime Diagnostics入口、Attach目标、连续采集与CSV导出实现。
- [x] 13.21 让FixedCharacterHost脚IK诊断只订阅Foot Placement通道，以10Hz刷新Inspector预览但保留逐PresentationFrame采集，并在240个segment后自动结束。

## 14. 迁移Corin作者资产

- [x] 14.1 将Corin Animation Rig迁移为v4。
- [x] 14.2 显式写入Corin Solver Root BoneId。
- [x] 14.3 显式写入Corin ordered Spine BoneIds。
- [x] 14.4 显式写入Corin Left Arm BoneIds。
- [x] 14.5 显式写入Corin Right Arm BoneIds。
- [x] 14.6 保留并校验Corin左右Leg BoneIds。
- [x] 14.7 显式写入Corin可选Head与Clavicle BoneIds。
- [x] 14.8 创建唯一Corin FullBodyIK Profile资产。
- [x] 14.9 在Corin Presentation Profile引用FullBodyIK Profile。
- [x] 14.9A 把Corin Foot Placement Profile迁移为FinalIK Grounding与Predictive Extension分组。
- [x] 14.9AA 把Corin FinalIK Grounding Quality显式配置为Best。
- [x] 14.9AB 让Corin普通基线关闭FinalIK Overstep Falls Down，并把Foot Radius约束到Calibration鞋底半长以内，避免无命中脚驱动pelvis下沉或胶囊越过台阶边缘取得伪支撑。
- [x] 14.9AC 让Corin普通基线显式选择AllPlantedFeet、FollowBody与有限最大升降范围，禁止stock lower/lift输出。
- [x] 14.9B 删除Corin重复Foot rotation、Foot smoothing与stock Pelvis Speed/Damper/Lower/Lift旧字段。
- [x] 14.10 将Corin Foot Calibration迁移为v4。
- [x] 14.11 删除Corin Calibration preferred bend数据。
- [x] 14.12 从Corin普通Foot Placement基线删除与当前手骨同位、会反向钉住骨盆位移的PoseBoneIKGoals节点。
- [ ] 14.13 定义不等于当前LeftHand Pose的真实武器目标后再接入LeftHand Goal。
- [ ] 14.14 定义不等于当前RightHand Pose的真实武器目标后再接入RightHand Goal。
- [x] 14.15 将FootPlacement节点迁为PredictiveFootPlacement。
- [x] 14.16 删除Corin LegIK节点。
- [x] 14.17 新增唯一Corin FullBodyIK节点。
- [x] 14.18 普通基线只把LocalToComponent Pose连接到Foot Placement与FullBodyIK。
- [x] 14.19 普通基线只把Foot Goals连接到FullBodyIK动态port。
- [x] 14.20 连接FullBodyIK结果到ComponentToLocalPose。
- [x] 14.21 更新Corin Pose Graph layout与stable node identity。
- [x] 14.22 重建Corin Foot Analysis artifact。
- [x] 14.23 通过显式Character Build发布普通Foot Placement基线的Corin Presentation Projection。
- [x] 14.24 通过显式Character Build发布普通Foot Placement基线的Float32 Target Program。
- [x] 14.25 通过显式Character Build发布普通Foot Placement基线的Fixed Target Program。
- [x] 14.26 通过显式Character Build发布普通Foot Placement基线的Native Pose Program。

## 15. 删除旧IK实现和数据

- [x] 15.1 删除`CharacterComponentPoseLimbSolver`。
- [x] 15.2 删除`CharacterTwoBoneIkPoseSolver`。
- [x] 15.3 删除`CharacterLegIkDiagnostics`。
- [x] 15.4 删除TwoBoneIK payload与descriptor。
- [x] 15.5 删除LegIK payload与descriptor。
- [x] 15.6 删除TwoBoneIK compiler handler。
- [x] 15.7 删除LegIK compiler handler。
- [x] 15.8 删除TwoBoneIK runtime operation。
- [x] 15.9 删除LegIK runtime operation。
- [x] 15.10 删除TwoBoneIK reference-pose validation。
- [x] 15.11 删除LegIK bend-plane ABI。
- [x] 15.12 删除BipedLegTargets contract与workspace。
- [x] 15.13 删除旧solver failure与reach enum。
- [x] 15.14 删除旧UI field、label、color与tooltip。
- [x] 15.15 删除旧Document node kind与port type。
- [x] 15.16 删除Corin旧TwoBoneIK与LegIK serialized payload。
- [x] 15.17 删除Prefab中的任何历史FinalIK组件引用。
- [x] 15.18 删除Runtime assembly中的旧自研solver引用。
- [x] 15.19 删除项目旧Support Query中与FinalIK Grounding重复的当前脚目标数学。
- [x] 15.20 删除项目旧Runtime中与FinalIK Grounding重复的坡面rotation数学。
- [x] 15.21 删除项目旧Runtime中与FinalIK Grounding重复的Foot interpolation数学。
- [x] 15.22 删除FinalIK stock pelvis正式输出，建立只消费Foot Goals与Rig腿长的逐腿Pelvis Reach Planner。
- [x] 15.22A 删除旧Directional Pelvis resolver，统一成Pelvis Height Mode与Actor Movement Compensation Mode。
- [x] 15.23 删除项目正式链中的GrounderFBBIK组件引用和配置副本。
- [x] 15.24 保留FinalIK插件自带Grounder示例走vendor Transform backend。
- [x] 15.25 确认正式Runtime只有一个Grounding adapter、一个Predictive Extension owner和一个FBBIK solver。
- [x] 15.26 确认旧schema、旧reader、兼容枚举和fallback开关均不存在。

## 16. 同步架构真相与重叠change

- [x] 16.1 同步`openspec/project.md`唯一Pose链。
- [x] 16.2 同步Rig v4与Calibration v4当前口径。
- [x] 16.3 同步FullBodyIK backend与FinalIK能力边界。
- [x] 16.3A 同步FinalIK Grounding成熟基线与Project Predictive Extension边界。
- [x] 16.4 同步`add-discrete-stair-presentation`中的LegIK旧任务口径。
- [x] 16.4A 同步`add-discrete-stair-presentation`中的当前heel/toe query为FinalIK Grounding owner。
- [x] 16.4B 同步`add-discrete-stair-presentation`中的Future Landing与Ground Envelope为Predictive Extension owner。
- [x] 16.4C 把`add-discrete-stair-presentation`中的旧Directional Pelvis与隐式movement compensation统一为逐腿Planner正式模式。
- [x] 16.5 同步`add-discrete-stair-presentation`对已归档change的错误active引用。
- [x] 16.6 同步`add-character-presentation-blend-space`的Foot Placement阶段名称。
- [x] 16.7 同步`add-character-motion-matching-pose-source`的Foot Placement阶段名称。
- [x] 16.8 保持MM Pose History排除FullBodyIK结果。
- [x] 16.9 保持离散楼梯Surface与Body vertical discontinuity合同不变。
- [x] 16.10 保持KCC、Simulation、Network与Camera不读取IK目标或结果。
- [x] 16.11 同步重叠change中的Foot Placement阶段为FinalIK Grounding-backed Goal Source。
- [x] 16.12 保持MM Pose History排除Grounding与FullBodyIK结果。
- [x] 16.13 按当前普通Foot Placement阶段的真实实现逐项更新本tasks状态。
- [x] 16.14 不归档本change。

## 17. 用逐腿可达区间替换stock pelvis

- [x] 17.1 从Grounding settings、Live Tuning与Corin资产删除Pelvis Speed、Damper、Lower与Lift，并在adapter中固定stock pelvis不输出。
- [x] 17.2 从左右Hip、动画Ankle、最终Foot Goal、Goal Weight、Rig腿长与extension ratio计算逐腿允许pelvis区间。
- [x] 17.3 让Pelvis Reach Planner只使用`max(PlantSupportWeight, ContactWeight)`作为支撑权重，不直接读取或重映射Plant Confidence，也不把普通Placement Weight自动视为支撑。
- [x] 17.4 在单腿不可达或双腿区间冲突时稳定保留主要支撑脚，并以PelvisRangeConflictReleased释放不可满足Goal。
- [x] 17.5 以最大升降范围、dead zone、显式frame delta和Actor Movement Compensation Mode维护唯一pelvis插值状态。
- [x] 17.6 在正式PredictiveFootPlacement diagnostics发布左右区间、target/resolved offset与左右Goal拒绝结果。
- [x] 17.7 将Corin Profile迁移到schema v9，并配置AllPlantedFeet、FollowBody和正式Planner参数。
- [x] 17.8 刷新Unity工程并通过Runtime与Editor编译。
- [x] 17.9 以显式Character Build发布匹配新Profile revision的Corin Float32/Fixed Program与Presentation Projection。
- [x] 17.10 对修改后的change执行strict OpenSpec validate并按真实结果收口任务状态。
- [x] 17.11 让Pelvis Reach Planner从贡献脚相对动画Ankle的竖直Goal变化求支撑权重首选高度，并夹入逐腿共同可达区间。
- [x] 17.12 同步Pelvis设计与spec，删除“区间内永远选择最接近0”导致共同高平台下蹲的旧口径。
- [x] 17.13 通过Runtime与Editor编译并显式发布匹配修正后Profile与Pose Plan的Corin Float32、Fixed和Presentation Projection。
- [x] 17.14 对修正后的change执行strict OpenSpec validate并按真实结果更新任务状态。

## 18. 拆除脚速对普通Foot Goal的总闸门

- [x] 18.1 从`GroundingFootInput`与FinalIK Grounding adapter删除`PlantWeight`输入，让Grounding先生成不受动画Plant Confidence缩放的唯一Current Grounding结果。
- [x] 18.2 删除`InverseLerp(0.5f, 1f, PlantConfidence)`及Plant Confidence对普通Foot Goal和Pelvis的连续乘法。
- [x] 18.3 通过运行时快照确认烘焙Sole Local Velocity与Body可见速度、yaw点速度重组会产生虚高脚速，并删除该重组。
- [x] 18.4 通过持续输入/松开输入现象确认相邻sole世界位置差包含actor平移，删除逐脚世界速度历史和`GroundAlignmentWeight`总闸门。
- [x] 18.5 让合法Current Grounding Goal只由`PlacementWeight`控制，并保留FinalIK `rootYOffset`表达的动画脚离地高度。
- [x] 18.6 只使用最终Pose contribution混合后的烘焙`SoleLocalVelocity.magnitude`维护Plant Contact与Contact约束渐退，不拼接Body或actor世界运动。
- [x] 18.7 将Profile迁移为严格有序的`PlantSpeedThreshold`与`UnalignmentSpeedThreshold`，删除Alignment planar/vertical、descending tolerance与旧Live Tuning key。
- [x] 18.8 新增`PlantSupportWeight`作为Pelvis普通支撑选择，并让Planner使用`max(PlantSupportWeight, ContactWeight)`。
- [x] 18.9 让`Unlocked`固定不产生Contact Weight；其它Plant Policy只让Contact Weight控制anchor、lock与slide。
- [x] 18.10 将runtime plan、pelvis input、typed diagnostics、snapshot、Inspector与CSV统一迁移到Placement/Plant Support/Contact职责，不保留旧字段或双写列。
- [x] 18.11 迁移Corin与TrainingEnemy Foot Placement Profile到schema v11和`0.6m/s -> 2.0m/s`正式Plant阈值。
- [x] 18.12 新增并维护IK诊断文档，记录现象、运行证据、UE对照、踩坑和固定排查链路。
- [x] 18.13 在用户明确触发Character Build后发布匹配新Profile revision的Corin Float32/Fixed Program、Presentation Projection与Native Pose Program，以及TrainingEnemy Float32 Program、Presentation Projection与Native Pose Program。
- [x] 18.14 对最终修订执行静态搜索与strict OpenSpec validate并按真实结果更新任务状态。
