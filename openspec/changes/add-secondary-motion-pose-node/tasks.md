## 1. 收敛依赖与实施门禁

- [ ] 1.1 等待`replace-pose-ik-with-finalik-full-body-solver`完成并归档
- [ ] 1.2 基于归档后的Rig v4、FullBodyIK和末段Pose拓扑重对账本change delta
- [ ] 1.3 记录Magica Cloth 2参与manual update的精确源码文件与内容hash
- [ ] 1.4 列出Magica Manager对PlayerLoop、Unity Time、global team和Transform读写的依赖矩阵
- [ ] 1.5 证明manual batch seam不需要修改Simulation、Constraint或Collision核心方程
- [ ] 1.6 证明graph-owned team可完全关闭Before/AfterLateUpdate自动更新
- [ ] 1.7 证明同一RenderFrame全部Actor team只需一次global manual call
- [ ] 1.8 证明manual call可接收显式presentation delta并返回可核对completion
- [ ] 1.9 证明post-secondary完整Rig capture不需要shadow skeleton或第二PlayableGraph
- [ ] 1.10 证明运行时setup可由Profile、Rig和Projection构建且不依赖手工Prefab组件正文
- [ ] 1.11 任一门禁失败时停止实施并记录失败API、文件和事务影响

## 2. 定义Secondary Motion核心合同

- [ ] 2.1 新增稳定`SecondaryMotionProfileId`
- [ ] 2.2 新增稳定`SecondaryMotionGroupId`
- [ ] 2.3 新增稳定`SecondaryMotionColliderId`
- [ ] 2.4 定义group root、controlled bone和连接模式合同
- [ ] 2.5 定义fixed/movable bone范围合同
- [ ] 2.6 定义Animation Follow与Simulation Weight合同
- [ ] 2.7 定义distance、angle、bending、motion、inertia和collision参数合同
- [ ] 2.8 定义Sphere、Capsule和Plane collider shape合同
- [ ] 2.9 定义reset、suspend和resume合同
- [ ] 2.10 定义Profile revision、content hash和Rig lineage
- [ ] 2.11 定义固定group、collider、team和workspace容量
- [ ] 2.12 禁止Profile保存Transform、GameObject、Magica组件引用、backend selector或fallback

## 3. 建立Profile与全局设置资产

- [ ] 3.1 新增`CharacterSecondaryMotionProfile`资产
- [ ] 3.2 新增有序group子数据
- [ ] 3.3 新增有序collider子数据
- [ ] 3.4 新增`CharacterSecondaryMotionRuntimeSettings`唯一全局资产
- [ ] 3.5 让全局设置唯一保存frequency、max substep、time scale和manual update policy
- [ ] 3.6 让Gameplay Presentation装配根显式引用全局设置
- [ ] 3.7 拒绝缺失或重复全局设置
- [ ] 3.8 拒绝未知、重复或空GroupId与ColliderId
- [ ] 3.9 拒绝group之间controlled bone重叠
- [ ] 3.10 拒绝Virtual Bone、跨Rig BoneId和非法root后代
- [ ] 3.11 拒绝collider绑定未知或Virtual Bone
- [ ] 3.12 拒绝非有限数值、非法尺寸、非法权重和空可动集合

## 4. 建立Profile作者表面

- [ ] 4.1 在现有Presentation Profile Inspector增加Secondary Motion入口
- [ ] 4.2 提供Profile对象选择与Open Profile命令
- [ ] 4.3 提供group列表、root chain、connection mode和controlled bone只读推导
- [ ] 4.4 提供Animation Follow、Simulation Weight和约束字段
- [ ] 4.5 提供Collider形状、BoneId、offset和尺寸字段
- [ ] 4.6 提供Rig lineage、revision和stale diagnostics
- [ ] 4.7 默认隐藏GUID、hash、dense index与Magica team id
- [ ] 4.8 让修改只标记Presentation Projection stale
- [ ] 4.9 禁止Inspector repaint、selection变化、Domain Reload和Play Mode自动Build
- [ ] 4.10 禁止作者通过Prefab Magica组件形成第二调参入口

## 5. 新增Pose Graph节点能力

- [ ] 5.1 在Pose Node Kind加入`SecondaryMotion`
- [ ] 5.2 新增typed `CharacterSecondaryMotionPosePayload`
- [ ] 5.3 让payload只保存强类型Profile引用
- [ ] 5.4 声明必需`pose.local`输入
- [ ] 5.5 声明唯一`pose.local`输出
- [ ] 5.6 新增`ExternalPhysicalPose` execution domain
- [ ] 5.7 在共享Capability Catalog登记节点与Open Profile命令
- [ ] 5.8 只允许节点出现在root Pose Graph
- [ ] 5.9 禁止State Pose Graph、Subgraph、Linked Entry和MM Entry Graph创建节点
- [ ] 5.10 限制每个root Pose Graph最多一个SecondaryMotion节点
- [ ] 5.11 要求输入来自最终`ComponentToLocalPose`
- [ ] 5.12 要求输出直接进入唯一`OutputPose`
- [ ] 5.13 禁止在节点前后隐藏插入Pose转换或第二Output
- [ ] 5.14 让Canvas、clipboard、Details和创建菜单读取同一Capability

## 6. 扩展Frontend、Validator与Projection

- [ ] 6.1 让Pose Graph Validator识别SecondaryMotion typed payload
- [ ] 6.2 校验root-only、唯一性和末段拓扑
- [ ] 6.3 校验Profile对象类型、identity、revision和Rig lineage
- [ ] 6.4 校验全部group root和controlled Physical Bone
- [ ] 6.5 校验collider binding与group骨集互斥
- [ ] 6.6 编译profile/group/collider为dense physical index
- [ ] 6.7 编译Magica setup payload与固定容量
- [ ] 6.8 编译pre-secondary Base Pose output binding
- [ ] 6.9 编译post-secondary Local Pose output和completion slot
- [ ] 6.10 把ExternalPhysicalPose stage编入唯一stage table
- [ ] 6.11 让FinalPublication显式依赖SecondaryMotion completion
- [ ] 6.12 把Profile、Rig、Global Settings与Magica setup artifact纳入Projection依赖hash
- [ ] 6.13 拒绝stale、缺失或容量不一致的generated payload
- [ ] 6.14 保持Float32与Fixed消费同一Presentation Projection payload

## 7. 建立Magica vendor I/O seam

- [ ] 7.1 为Magica Manager新增唯一Manual update location
- [ ] 7.2 为manual update提供显式presentation delta输入
- [ ] 7.3 为manual update提供稳定RenderFrame/completion输入
- [ ] 7.4 为team registration暴露稳定只读team handle
- [ ] 7.5 为manual batch返回完成team集合与global completion
- [ ] 7.6 让manual mode不注册BeforeLateUpdate与AfterLateUpdate cloth update
- [ ] 7.7 让manual mode继续使用现有Transform read、Simulation和Transform write job
- [ ] 7.8 保持Magica核心约束、碰撞、积分和WriteTransform数学不变
- [ ] 7.9 拒绝同帧重复manual call和未知team参与
- [ ] 7.10 记录vendor seam文件、符号与升级对账说明
- [ ] 7.11 让manual mode禁用graph-owned team的自动RestoreBaseTransform回调
- [ ] 7.12 证明manual batch之外不存在graph-owned Transform restore或write

## 8. 实现唯一Magica Secondary Motion backend

- [ ] 8.1 定义`ICharacterSecondaryMotionSolverBackend`批处理合同
- [ ] 8.2 实现唯一`CharacterMagicaCloth2SecondaryMotionBackend`
- [ ] 8.3 从Projection和Rig Binding创建actor team
- [ ] 8.4 从dense root bone构建Bone Cloth proxy topology
- [ ] 8.5 从编译Collider descriptor创建并绑定Magica collider
- [ ] 8.6 把Animation Follow映射到`animationPoseRatio`
- [ ] 8.7 把Simulation Weight映射到`blendWeight`
- [ ] 8.8 映射其余正式group constraint参数
- [ ] 8.9 在preparation阶段完成组件/team/workspace创建与预热
- [ ] 8.10 禁止正常帧扫描Transform层级、创建组件、扩容或创建托管集合
- [ ] 8.11 建立Actor、Group、Team与Projection completion映射
- [ ] 8.12 实现reset、suspend、resume和dispose生命周期
- [ ] 8.13 禁止缺失backend时透传Base Pose
- [ ] 8.14 禁止Magica camera/distance culling静默跳过正式team

## 9. 原子迁移Presentation批调度

- [ ] 9.1 将`IGameplayPresentationFrameTarget`迁移为Prepare/Finalize批接口
- [ ] 9.2 删除旧单方法`PresentationFrame`接口
- [ ] 9.3 定义与Character、Animation、Magica无关的`IGameplayPresentationBatchCoordinator`
- [ ] 9.4 让GameplayTickSystem构造时接收唯一正式Batch Coordinator
- [ ] 9.5 让`CharacterPhysicalPublicationBatchCoordinator`成为产品唯一装配实现
- [ ] 9.6 禁止GameplayTickSystem引用Character、Animation或Magica类型
- [ ] 9.7 迁移Local Float32 Presentation target
- [ ] 9.8 迁移Local Fixed Presentation target
- [ ] 9.9 迁移Deterministic Rollback Presentation target
- [ ] 9.10 迁移Server Authoritative Remote Presentation target
- [ ] 9.11 让GameplayTickSystem按稳定注册顺序Prepare全部target
- [ ] 9.12 在全部Prepare完成后调用唯一Physical Publication Coordinator
- [ ] 9.13 在global barrier完成后按同一顺序Finalize全部target
- [ ] 9.14 让所有target共用同一RenderFrame和presentation delta
- [ ] 9.15 让无Secondary Motion的target走同一批接口且不登记team
- [ ] 9.16 禁止target在Finalize前推进Camera或发布Final Pose
- [ ] 9.17 定义Prepare失败、global失败和Finalize失败的批次传播规则
- [ ] 9.18 删除任何Actor私有Magica manual call

## 10. 重构Animation Physical Publication事务

- [ ] 10.1 把旧Final Writer职责拆为Base Physical Pose Applicator与Final Pose Capture
- [ ] 10.2 删除`AnimationFinalPosePhysicalWriter`旧终态实现与命名
- [ ] 10.3 在Barrier前验证Base apply全部Physical Bone binding
- [ ] 10.4 在Barrier前验证Final capture全部Physical Bone binding
- [ ] 10.5 执行Actor Animancer Evaluate恰好一次
- [ ] 10.6 完成pre-secondary全部Pure/WorldAware Pose stage
- [ ] 10.7 把Base Local Pose应用到完整Physical Rig
- [ ] 10.8 保持Actor Animation transaction Pending并登记team
- [ ] 10.9 在global Magica completion后捕获完整Physical Local Pose
- [ ] 10.10 把capture写入Pending Final Pose页并完成SecondaryMotion输出
- [ ] 10.11 让OutputPose与FinalPublication只消费post-secondary completion
- [ ] 10.12 在Finalize后统一Seal Action、source、Pose与diagnostics状态
- [ ] 10.13 让Camera只在对应Actor成功Seal后推进
- [ ] 10.14 删除pre-secondary Final Pose发布和旧Committed页提升路径

## 11. 完成失败与Reset语义

- [ ] 11.1 保留Evaluate前Discard语义
- [ ] 11.2 Base Physical应用后任一Actor局部失败使该ActorFaulted
- [ ] 11.3 global Magica调用失败使全部参与ActorFaulted
- [ ] 11.4 Final capture或publication失败使对应ActorFaulted
- [ ] 11.5 禁止Barrier后恢复Physical Transform before-image
- [ ] 11.6 禁止Faulted Actor自动重建backend或跳过节点
- [ ] 11.7 在Body stream reset后安排ResetToCurrentAnimationPose
- [ ] 11.8 在branch replacement和teleport后安排reset
- [ ] 11.9 在Projection/Profile/Rig revision变化时使旧team失效
- [ ] 11.10 在Preview scrub、target切换和session restart时reset
- [ ] 11.11 在visibility resume前reset并重新登记team
- [ ] 11.12 保持普通连续帧Magica history

## 12. 同步Preview、Watch与Diagnostics

- [ ] 12.1 让Animation Preview使用同一三段批事务
- [ ] 12.2 让Preview从正式Projection和Rig Binding创建Magica team
- [ ] 12.3 缺少完整setup时返回typed Unavailable并阻止FinalPublication
- [ ] 12.4 为SecondaryMotion新增Pose Watch target
- [ ] 12.5 发布Base Pose与post-secondary Pose
- [ ] 12.6 发布group、team、reset generation和completion
- [ ] 12.7 发布Collider binding与碰撞统计
- [ ] 12.8 发布每骨修正量的固定容量diagnostics页
- [ ] 12.9 没有diagnostics interest时跳过逐骨复制
- [ ] 12.10 禁止Diagnostics从Transform反推或第二次执行Magica
- [ ] 12.11 让Live Debug只读取成功Seal的Committed post-secondary页
- [ ] 12.12 把Profile/Projection revision mismatch显示为Stale

## 13. 同步Agent Document v3与Mutation

- [ ] 13.1 扩展唯一Capability Catalog输出SecondaryMotion descriptor
- [ ] 13.2 在Asset Catalog只读输出可引用Secondary Motion Profile
- [ ] 13.3 输出Profile identity、类型、Rig lineage与revision
- [ ] 13.4 让Document exporter输出SecondaryMotion typed payload
- [ ] 13.5 让strict codec只接受结构化Profile对象引用
- [ ] 13.6 让Reconciler读取新增节点与Profile引用
- [ ] 13.7 新增创建SecondaryMotion节点的typed Presentation Mutation
- [ ] 13.8 新增配置Profile引用的typed Presentation Mutation
- [ ] 13.9 新增删除SecondaryMotion节点的typed Presentation Mutation
- [ ] 13.10 让Mutation复用人工Details的validator、Undo与资产事务
- [ ] 13.11 让reverse export保持stable identity和canonical字段
- [ ] 13.12 拒绝Document修改Profile正文、Bone、Collider Transform或Magica组件
- [ ] 13.13 保持五个BTSMTL lifecycle MCP工具不变
- [ ] 13.14 更新BTSMTL Agent Authoring当前合同与技能说明

## 14. 配置Corin正式内容

- [ ] 14.1 创建唯一Corin Secondary Motion Profile
- [ ] 14.2 配置`Skirt_01`至`Skirt_08`八条root chain
- [ ] 14.3 按腰围实际顺序配置`SequentialLoopMesh`
- [ ] 14.4 锁定裙摆24根controlled Physical Bone
- [ ] 14.5 配置Pelvis与左右Upper Leg裙摆Collider
- [ ] 14.6 按正式几何决定并显式配置Lower Leg Collider
- [ ] 14.7 配置Side Hair root group
- [ ] 14.8 配置Front Hair root group
- [ ] 14.9 配置左右Back Hair root group
- [ ] 14.10 配置Head、Neck、Shoulder与Upper Back头发Collider
- [ ] 14.11 配置`Spring_L/R`挂件group
- [ ] 14.12 配置`S_ChainF/B`挂件group
- [ ] 14.13 明确排除`Weapon_Lever_*`、`Weapon_saw*`和主要`Weapon_Etc_*`
- [ ] 14.14 在Corin root Pose Graph插入唯一SecondaryMotion节点
- [ ] 14.15 连接`ComponentToLocalPose -> SecondaryMotion -> OutputPose`
- [ ] 14.16 保持Corin模型、Renderer和Clip资产不拆分

## 15. 发布generated产品并清理旧路径

- [ ] 15.1 显式生成Corin Magica setup payload或PreBuild artifact
- [ ] 15.2 显式重建Corin Presentation Projection与Native Pose Program
- [ ] 15.3 显式重建Corin Float32产品
- [ ] 15.4 显式重建Corin Fixed产品
- [ ] 15.5 删除项目角色Prefab上的自动Magica更新配置
- [ ] 15.6 删除旧单调用Presentation target实现
- [ ] 15.7 删除旧Final Writer终态代码和diagnostics
- [ ] 15.8 删除临时secondary wiring、Transform名称查找和backend switch
- [ ] 15.9 更新`openspec/project.md`当前链路与能力状态
- [ ] 15.10 对账全部受影响current spec与仍在active的动画change
