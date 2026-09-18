本轮公共输入与C# authoring协议见[对接记录](coordination-r2.md)。对应新增实现任务由[只读Blackboard任务](../refine-pose-graph-readonly-blackboard/tasks.md)唯一维护；下列旧方案其它任务及状态保留，原Document专属完成记录已移至历史表。

## 1. 保留现有基础

- [x] 1.1 保留已接入的FlowCanvas原生图编辑、typed端口及统一作者交互基础。
- [x] 1.2 保留现有原生图执行、Native／Job、Source／Constraint／Final Publication和唯一帧事务。
- [x] 1.3 保留已有真实Actor观察、稳定来源identity和窗口解绑基础，作为新组织的接入点。

## 2. 作者模型与动画层

- [x] 2.1 建立AnimGraph、Animation Layer、State Pose、Transition Rule和Control Rig的正式角色、接口与owner合同。
- [x] 2.2 将Player改为直接动画资源或typed资源参数，提供速率、起始位置、Loop和进入行为。
- [x] 2.3 从作者Player移除Source Slot／Profile重复binding，统一由编译生成source usage与dense资源绑定。
- [x] 2.4 基于既有Linked Pose实现层接口、Implementation／Group与调用状态归属，统一Layer作者命名。
- [ ] 2.5 接通层内状态机、Slot与骨骼混合，明确姿势生产层和接收Base Pose的组合层接口。
- [x] 2.6 接入同帧Cached Pose及调用内值复用，消除重复状态推进与source采样。

## 3. Slot与Action Timeline的Montage职责

- [x] 3.1 在Rig对应目录建立稳定Animation Slot与Slot Group，分离内部AnimationChannel和IK Effector身份。
- [x] 3.2 将Slot改为Source Pose加Slot引用，允许根图、动画层和State Pose使用，移除Action Playback作者输入。
- [x] 3.3 在原Action Timeline动画轨道加入Slot引用及同Group约束，保持原AnimationClip片段和玩法轨道owner。
- [x] 3.4 在原Timeline扩展Sections、后续Section、Play／Stop／跳段控制及统一窗口进入退出规则。
- [x] 3.5 将动作Blend In／Out、Curve、Blend Profile和自动退出设置统一归入该Timeline动画设置，删除Slot级重复时间策略。
- [x] 3.6 接通既有committed Action指令与Slot Group路由，多个Slot消费者共享播放和采样，不建立第二时钟或动作仲裁。

## 4. 骨骼混合曲线与惯性化

- [x] 4.1 统一Rig关联Blend Mask、Branch Filter和Blend Profile作者设置及真实资产owner。
- [x] 4.2 完成Layered Blend Per Bone的层顺序、Alpha、空间选项和dense骨骼混合展开。
- [x] 4.3 将Curve Blend Options归入实际组合节点，提供明确Curve修改能力，删除必接Pose Parameter Resolve作者路径。
- [ ] 4.4 保留显式Inertialization并接入状态／Timeline动画混合请求、有界请求选择与局部history。
- [x] 4.5 将Player有效播放策略接入现有Phase／同步编译，保持Corin原有限与循环用法。

## 5. Control Rig作者与求解展开

- [x] 5.1 建立Control Rig输入Pose、Forwards Solve顺序、typed目标数据和图返回接口。
- [x] 5.2 接入已有骨骼控制、目标Transform／权重、Foot Placement和FBIK Effectors作者能力。
- [x] 5.3 将重复Effector处理、目标混合／选择与空间合同接入统一typed约束。
- [x] 5.4 从FBIK作者请求展开Goal编码、唯一Assembler／Goal Set和身体求解位置，移除Assembler作者节点。
- [x] 5.5 对接原Foot／Pelvis／FBBIK／BendHistory模块和Rig参数，隐藏没有实际后端支持的UE专有选项。

## 6. 编译与运行来源

- [x] 6.1 在唯一Compiler的Closure及Definition展开阶段接入各角色图、直接资源、层与Rig调用。
- [x] 6.2 将内部Action读取、参数处理、接口边界转换和目标组装接入原Topology／Stage／Value／Workspace／Seal链。
- [x] 6.3 扩展作者节点到多operation、端口、内部步骤及Timeline／层／Rig调用的Source Map。
- [x] 6.4 更新必要的动画指令、产物与codec合同，保持原Runtime owner、固定容量和唯一最终写入。
- [x] 6.5 将作者版本、Rig、Implementation、Actor generation和call-site匹配覆盖到新组织全部观察入口。
- [x] 6.6 建立动画侧Animation Input Contract，声明Fact、参数、Slot播放输入与World能力，不从SkillGraphs或旧Gameplay产物反推。
- [x] 6.7 拆出只接收动画根、Rig、资源和输入声明的Pose编译请求与不可变结果，移除对Character前端及Numeric布局的依赖。
- [x] 6.8 将Pose窗口和正式命令接到独立动画编译入口，无Character上下文时仍能编译完整动画输入。
- [x] 6.9 将Character总Build改为复用同一Pose Compiler／结果，在装配层绑定Gameplay输入并原子发布，删除先技能编译才能进入Pose的调用链。

## 7. 作者窗口

- [x] 7.1 按图角色组织目录、节点、端口和连接外观，状态转换采用独立命中、箭头与详情。
- [x] 7.2 统一目录、双击、Details及运行定位的调用路径和面包屑，保持状态、层与控制图往返选择。
- [x] 7.3 提供Player资源、Slot／Group、Mask、转换策略、Rig目标与求解的作者详情及就地失败反馈。
- [x] 7.4 在原Timeline表面展示Slot轨道、Sections和动画混合设置，保持技能窗口及Clip曲线各自既有入口。
- [ ] 7.5 接通新Slot、层、骨骼权重和Rig目标的已完成观察，内部operation只在需要时展开。

## 8. 正式作者入口与规范同步

- [x] 8.4 同步design.md列出的现行spec与project.md冲突条款，删除旧作者显式流水线、Source Slot及旧版本描述。
- [x] 8.5 整理一次性Editor迁移入口，禁止普通窗口、Runtime或Build暗中迁移旧作者资产。

## 9. 最后迁移与发布

- [x] 9.1 生成精确Corin作者目标，保留可保留的图／状态identity、动画资源、时间、过渡与IK配置，列明无法无损表达的旧规则。
- [x] 9.2 通过正式事务把Corin根图与身体控制重组为新层次，直接资源写回Player，Timeline原位补齐Slot／Section／Blend设置。
- [x] 9.4 通过唯一Character Build统一发布Corin所需Float32、Fixed与共享Projection，删除过期产物读取路径。
