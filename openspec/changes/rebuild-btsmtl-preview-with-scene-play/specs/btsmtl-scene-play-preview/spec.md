## Purpose

定义 BTSMTL 在可配置独立 Unity 场景和 Play Mode 中的预览，统一场景生命周期、准确实例观察、直接作者调参及退出恢复。角色沿正式输入、代码控制与唯一动作/模拟链运行，独立 Timeline 消费其正式非 Skill 调用方；两者使用各自真实内容根、版本和执行来源，预览不实现另一套内容执行。

## ADDED Requirements

### Requirement: 场景预览必须使用明确选择的独立场景

系统 MUST允许作者明确选择一个正式保存的独立预览场景。环境、运行 owner、目标、初始条件及适用的角色、光照和相机 MUST由该场景及其正式 Prefab 引用配置；窗口 MUST不复制正式内容定义、动画拓扑或 Gameplay 配置。场景上下文 MUST显式登记本次场景声明的领域目标与就绪结果；角色引用正式 Session/Actor，非 Skill 引用其正式业务 owner，不要求空角色。未选择场景或场景上下文无效时 MUST保持资产可编辑，并禁用启动、显示缺失原因，不得自动使用当前场景或搜索替代目标。

#### Scenario: 选择斜坡试验场景

- **WHEN** 作者选择一个配置了 Corin 和斜坡的独立场景并开始预览
- **THEN** 系统 MUST运行该精确场景及其角色配置
- **AND** 当前其它编辑场景中的角色 MUST不被接管

#### Scenario: 场景没有唯一正式运行上下文

- **WHEN** 所选场景的角色接入没有合法 Session 引用、非 Skill 接入缺少正式调用方，或登记了多个无法唯一识别的预览上下文
- **THEN** 系统 MUST报告具体配置错误并停止启动
- **AND** MUST不选择第一个找到的 Host 或创建默认配置

### Requirement: 角色预览必须由正式角色运行链产生结果

完整角色预览 MUST进入 Unity Play Mode，由场景明确配置的正式 Session、Numeric Target、输入、世界求解和角色表现产生结果。C# 显式 Locomotion StateMachine/State/Transition、唯一 Action 服务、ActionInstance 内的技能 Root/局部状态机/子图/Timeline/TreeClip，以及 Motion、Window、GameplayEffect、动画、IK 和已安装相机 MUST在同一 Session/Pipeline、Evaluate/WorldResolve/Finalize 与 Commit 边界执行；预览窗口 MUST不创建表现专用替代会话、不直接求值作者对象，也不为缺失能力补造简化结果。

#### Scenario: 动作位移被障碍阻挡

- **WHEN** 场景中的正式动作产生位移请求且正式世界求解检测到障碍
- **THEN** 角色表现与作者观察 MUST使用该次正式求解和提交的结果
- **AND** 预览 MUST不按作者位移曲线另行移动视觉根

#### Scenario: 选择不同Numeric Target

- **WHEN** 作者选择明确配置为 Fixed 的预览场景
- **THEN** 预览 MUST使用该场景的 Fixed Session 和匹配产物
- **AND** MUST不自动切换 Float32 以使预览可用

### Requirement: 独立Timeline必须通过正式非Skill调用方接入场景预览

独立 Timeline 预览 MUST复用唯一场景 Play 操作，消费 Timeline owner 提供的精确 shared 内容根/产物、显式目标/参数绑定、业务 owner identity、实际播放 identity、调用点与 generation，以及正式开始、状态、停止/释放和只读观察合同。时间推进和 TreeClip 执行 MUST由该业务 owner 的正式帧调用共用执行实现；预览 MUST不调用 Advance、运行作者对象或创造窗口播放器。非 Skill 本地表现 MUST不要求 Character Definition、SessionHost、Actor、Skill 或 ActionInstance，也不因此取得 Character/World 写入权限。

#### Scenario: 无角色场景运行独立Timeline

- **WHEN** 所选场景只有合法非 Skill owner、已发布内容和明确表现目标，且正式调用成功产生播放 identity
- **THEN** 预览 MUST在同一次受控 Play 中观察该 owner 的实际播放和输出
- **AND** MUST不要求 Character 构建产物或创建假角色/动作实例

#### Scenario: 独立调用合同尚未交付

- **WHEN** 内容可编辑但正式调用方、能力或目标绑定尚不可用
- **THEN** 对应试验入口 MUST显示具体未就绪原因，作者内容 MUST仍可编辑
- **AND** MUST不由预览补齐非 Skill 执行器或改走角色替代路径

### Requirement: 同一Unity实例必须只有一个受控预览运行

同一 Unity 实例 MUST只有一个受控场景预览请求拥有启动、暂停、重建和停止权限。各作者窗口 MUST共享该次运行并保存自己的选择、观察目标和 interest。第二次启动 MUST显示已有运行；窗口切页、折叠和关闭 MUST只改变本地视图，不隐式终止预览。未由预览请求启动的 Play MUST只能经原有 Live 入口观察，不得被自动接管。

#### Scenario: 从Timeline下钻TreeClip

- **WHEN** 作者在运行中从 Timeline 打开 TreeClip Graph 并关闭 Timeline 窗口
- **THEN** 同一次预览 MUST继续执行
- **AND** Graph 窗口 MUST观察原正式调用方的结果，不创建或接管业务时钟

#### Scenario: 外部Play已经运行

- **WHEN** Unity 已运行其它任务启动的场景而作者尝试开始预览
- **THEN** 预览 MUST拒绝抢占并显示已有运行
- **AND** MUST不停止、换场景或修改外部运行状态

### Requirement: 启动与退出必须恢复完整编辑环境

预览 MUST保存和恢复原有编辑场景布局及启动场景设置，包括场景加载状态、顺序和 active scene。未保存场景 MUST经过 Unity 正式保存流程，取消时 MUST取消启动。跨 Domain Reload MUST只恢复请求与稳定定位信息，重新等待所声明正式 owner 就绪；角色等待 Session Active，非 Skill 等待正式调用环境准备结果。结束、启动失败或请求失效 MUST通过同一生命周期释放受控运行并恢复编辑环境，不自动重试。

#### Scenario: 多场景编辑后结束预览

- **WHEN** 作者在多个场景加载的编辑环境中启动并结束独立预览
- **THEN** 原编辑场景的加载布局与 active scene MUST恢复
- **AND** 已合法保存的作者参数 MUST保留

#### Scenario: 用户取消保存

- **WHEN** 启动前 Unity 保存流程被取消
- **THEN** 系统 MUST不进入 Play、不更换场景、不留下待执行启动请求

#### Scenario: 进入Play发生Domain Reload

- **WHEN** 脚本域重载使编辑器和窗口对象重新创建
- **THEN** 预览 MUST按请求 identity 和场景 identity 重新等待正式登记
- **AND** MUST不恢复旧 Runtime 对象或重放已经消费的命令

### Requirement: 预览动作必须通过正式输入和准入产生

作者操作 MUST经所选角色的明确输入映射进入正式输入链，同一角色同一阶段 MUST保持唯一输入 owner。C# 控制 MUST选择技能，唯一 Action 服务 MUST负责准入、激活、替换、取消/打断和 ActionInstance 生命周期，再将上下文交给技能 Root。控制状态转换与技能激活 MUST保持独立；SkillExecutionState MUST只归属于 ActionInstance，不新增 SkillInstance 生命周期。AI MUST继续只提供正式 CharacterSimulationInput。缺少输入映射、目标或准入条件时 MUST显示真实原因，不得直接设置 Action winner、当前 State、Grounded、动画 Selection 或相机效果。

#### Scenario: 动作请求被拒绝

- **WHEN** 作者请求攻击但正式 Action admission 拒绝该请求
- **THEN** 预览 MUST显示拒绝结果并保持正式运行状态
- **AND** MUST不绕过条件播放选中的攻击 Clip

#### Scenario: 查看Locomotion变化

- **WHEN** 作者通过正式移动输入让角色加速
- **THEN** PoseState、Blend Space 与 Motion Matching 查询 MUST读取实际提交的角色事实
- **AND** MUST不由预览窗口直接覆盖速度 Fact 或查询结果

### Requirement: 暂停和重建试验必须保持正式生命周期

暂停与继续 MUST使用真实 Play 暂停状态和正式调度。正式产物仍匹配时，重建试验 MUST在同一次 Play 中按各领域正式停止/释放合同结束本轮运行并重新加载所选场景，从保存的初始条件建立新的运行 owner。角色 MUST完整结束原 Session，旧输入、C# 控制状态、ActionInstance 内的技能/调用/嵌套 Timeline/停止状态、物理、表现和事件按其 owner 释放；非 Skill MUST由正式业务 owner 释放播放、TreeClip 状态和本次目标占用。全部旧观察绑定 MUST失效，协调器 MUST不代做内容退出或 Advance。新场景 generation 产生前 MUST不接受对旧目标的命令，失败 MUST停止受控预览并报告原因。调参后正式构造检查要求重新发布时 MUST显示构建并重启，不得使用旧产物加补丁绕过校验。

#### Scenario: 攻击中途重建试验

- **WHEN** 角色仍有攻击、动画淡出和相机状态时作者点击重建试验
- **THEN** 旧 Session 与场景状态 MUST完整结束，新场景 MUST按配置重新建立
- **AND** MUST不只清空游标或移动角色到出生点

#### Scenario: 暂停期间修改参数

- **WHEN** 作者在暂停期间修改一个合法运行参数
- **THEN** 作者数据 MUST正常保存且显示待生效
- **AND** 窗口绘制 MUST不主动执行额外逻辑或表现帧

#### Scenario: 独立播放期间重建试验

- **WHEN** 非 Skill owner 仍有活动 Timeline/TreeClip 和目标占用时作者重建场景
- **THEN** 正式 owner MUST完成本轮停止/释放，新场景按配置重新准备，旧播放句柄和观察绑定 MUST失效
- **AND** 窗口 MUST不以清空游标代替退出或把旧命令发给新实例

#### Scenario: 调参后重建要求重新发布

- **WHEN** 作者要求重建试验而正式产物检查发现作者数据尚未发布
- **THEN** 系统 MUST显示需要明确构建并重启
- **AND** MUST不拿旧产物创建新 Actor 后补参数跳过构造验证

#### Scenario: 暂停中重建试验

- **WHEN** 作者在暂停状态点击重建试验且产物仍匹配
- **THEN** 系统 MUST通过受控的正常运行阶段完成加载和准备，并在完成后恢复暂停
- **AND** 准备期间 MUST不接收试验输入或由 Editor 直接调用业务帧

### Requirement: 运行中调参必须直接保存正式作者数据

运行中允许的作者参数修改 MUST消费唯一 Document v5 的共享 Capability、领域验证、正式 Mutation 与 Undo，写入并保留真实作者资产。控制配置、SkillDefinition/技能正文、Presentation 与独立 Timeline MUST保持各自唯一 owner；C# 实现、控制 state schema、生成 Program 和实例状态 MUST不可作为作者参数写入。系统 MUST不创建预览试用资产、作者值镜像或第二个保存入口。运行观察字段 MUST保持只读，运行状态 MUST不反写成作者默认值。

#### Scenario: 调参后退出Play

- **WHEN** 作者合法修改混合时长并退出预览
- **THEN** 正式作者参数 MUST保留修改且能够通过正式 Undo 撤回
- **AND** 本轮位置、当前状态和活动 Action MUST不被保存为作者数据

#### Scenario: 作者修改验证失败

- **WHEN** 输入值违反领域值域或引用规则
- **THEN** Mutation MUST拒绝修改并显示具体原因
- **AND** MUST不向运行实例提交该非法候选

### Requirement: 作者保存与运行采用必须分别确认

只有领域明确支持的参数 MUST能够更新当前精确运行目标，保持原有生效时机与原子更新规则；角色使用其 Actor 参数端口，非 Skill 只消费其领域已提供的更新合同，没有局内更新能力的字段 MUST要求构建采用。作者修改成功后，系统 MUST分别显示待生效、已采用或运行应用失败；已采用 MUST以运行端确认而不是提交成功为依据。运行拒绝候选时 MUST保留作者修改和上一份正式运行参数，不自动回退作者资产。Undo/Redo MUST通过同一规则提交新候选。

#### Scenario: 参数在下一次激活生效

- **WHEN** 作者修改一个只允许下一次激活生效的参数
- **THEN** UI MUST显示作者已修改且运行待激活
- **AND** 只有正式激活并确认采用后 MUST显示已生效

#### Scenario: 运行应用失败

- **WHEN** 作者 Mutation 成功但运行实例拒绝候选
- **THEN** 作者修改 MUST保留，运行 MUST保持上一份已提交参数
- **AND** UI MUST同时显示保存状态、失败原因与实际采用状态

#### Scenario: 两个角色引用共享Profile

- **WHEN** 作者为明确选中的角色 A 调整共享 Profile 字段
- **THEN** 资产默认值 MUST更新，运行候选 MUST只提交到角色 A
- **AND** 角色 B MUST不被暗中更新，其运行采用状态 MUST独立显示

### Requirement: 结构变化必须经过明确构建和重新启动

技能 Root/子图/Timeline、独立 Timeline 内容、控制 binding/语义版本、资源装配或参数/状态布局变化 MUST服从对应领域的正式 Build 与资源/代码发布合同，不把 C# 控制翻译成角色 RootTree。普通开始预览、参数修改、选择和窗口刷新 MUST不自动构建。需要构建的字段 MUST在 Edit Mode 编辑；外部修改或 Undo 导致运行拓扑过期时 MUST使预览失效。明确的构建并开始／重启操作 MUST在 Edit Mode 针对精确正式根和 Target 构建：角色使用 Character Definition，独立内容使用 Timeline owner 提供的 shared TimelineAsset 根及精确发布目标。构建成功后才进入预览，失败保持编辑状态；MUST不为独立根伪造 Character Definition、TimelineNode 或 Skill Root。

#### Scenario: 产物过期时开始预览

- **WHEN** 作者点击开始而所选角色的已发布产物过期
- **THEN** 系统 MUST显示缺失或过期的精确目标及构建入口
- **AND** MUST不使用旧产物、临时编译产物或偷偷启动 Build

#### Scenario: 构建并重启失败

- **WHEN** 作者明确要求构建并重启而正式 Build 失败
- **THEN** 系统 MUST保留正式失败诊断并停留 Edit Mode
- **AND** MUST不进入另一场景或使用其它 Numeric Target

#### Scenario: 加载后才发现产物缺失

- **WHEN** 实际场景登记后才能确定某个正式角色缺少产物
- **THEN** 系统 MUST结束受控 Play 并在 Edit Mode 显示该精确目标的构建入口
- **AND** MUST不把窗口当前文档猜作所有角色的构建目标

#### Scenario: 独立Timeline产物过期

- **WHEN** 场景中的非 Skill 调用方报告精确 shared Timeline 根的产物过期
- **THEN** 构建入口 MUST调用 Timeline owner 的正式独立根构建和发布，并核对内容/绑定/数值目标版本
- **AND** MUST不把当前 Character 文档作为替代构建根或要求无关 Projection

### Requirement: 编辑游标与历史浏览必须不改变真实运行

编辑游标 MUST只定位作者内容；Capture 历史位置 MUST只选择既有历史事实；实时播放标记 MUST来自真实运行。系统 MUST明确区分三者，不得把任意游标变化解释为运行 seek、重置、额外采样或已重建到该时刻。本次完整角色预览 MUST不提供未实现状态恢复的任意时间跳转。

#### Scenario: 查看较早的Capture帧

- **WHEN** 作者浏览历史攻击帧而当前角色仍在运行
- **THEN** 视图 MUST标明历史观察，当前角色 MUST继续按原正式调度执行
- **AND** MUST不重新触发该帧的攻击、伤害、动画或相机事件

### Requirement: 预览等待和失败必须显示真实阶段

系统 MUST按实际领域报告内容依赖检查、编译/确定性检查、Numeric Target lowering、组合发布、进入 Play、正式 owner 准备、目标连接和试验重建的状态与耗时。角色路径 MUST另显示实际发生的控制 binding/版本/参数检查、表现计划、分析生成或复用、Session 准备中的 PipelineCompiler 工作；独立 Timeline MUST消费其正式构建/准备报告，不填入未发生的角色阶段。没有测量值 MUST显示未测量。编译轮询等待、C# 重载和场景启动 MUST不合并伪装成图编译耗时；失败 MUST定位实际阶段，不自动重跑构建或更改 Unity 重载设置。

#### Scenario: 分析生成与数据构建分别计时

- **WHEN** 作者通过明确的正式操作生成分析产物并构建角色数据
- **THEN** 输出 MUST区分已经实际执行的分析生成、图与表现数据编译及资产发布耗时
- **AND** 后续复用分析时 MUST显示复用状态而不是报告一次未发生的生成

### Requirement: 迁移后完整角色预览必须只有场景运行入口

迁移 MUST删除旧完整角色预览的窗口播放器、独立 Fact/Action/Query 输入、私有预览场景、时间推进、目标接管和仅供这些路径使用的资源。全部正式作者入口 MUST接入同一场景运行操作，不能保留兼容开关或重命名后的替代播放器。原生素材、曲线和几何编辑 MUST继续使用各自正式作者工具。

#### Scenario: 从不同作者页面开始预览

- **WHEN** 作者分别从 Timeline、Pose Graph、Blend Space 或 Action Workspace 请求完整角色预览
- **THEN** 各入口 MUST使用同一场景预览合同和正式运行链
- **AND** MUST不因页面不同而选择不同的角色执行器

### Requirement: 作者技能与运行释放必须分别精确绑定

角色技能的作者上下文 MUST按 Character Definition、SkillDefinition、技能 Root 和稳定作者调用路径定位；运行上下文 MUST按场景 generation、Session、Actor、ActionInstance、SkillProgram identity/版本和运行调用 generation 定位。技能 Timeline 观察 MUST进一步明确 playback/activation 与 cycle。ActionProfile 被多个技能引用、同技能并发释放或 shared 子图多调用时 MUST显式区分；失效绑定 MUST显示原目标失效，不选择首个同模板实例替代。该合同 MUST不把角色身份要求扩展到非 Skill 调用，独立内容使用其正式根与调用方/播放身份。

#### Scenario: 同一个技能并发释放

- **WHEN** 正式准入允许同 Actor 同时运行两次相同技能
- **THEN** 两次释放 MUST共享只读技能模板并使用各自 ActionInstance/调用状态，窗口 MUST只显示明确绑定的实例
- **AND** 一个实例结束 MUST不将窗口静默切到另一个实例

#### Scenario: 重建场景后恢复作者页面

- **WHEN** 场景重建后同一 SkillDefinition 仍存在但旧释放已销毁
- **THEN** 作者页面和稳定调用路径 MUST可以恢复，旧 ActionInstance 观察绑定 MUST失效
- **AND** MUST不重新发起旧请求或创建替代 SkillInstance

### Requirement: 技能内容结构不得受唯一Timeline限制

技能 MUST可以是 Tree-only，也可以包含多个或嵌套 Timeline、TreeClip、技能局部状态机和参数化子图。工作区 MUST以 SkillDefinition/Root 为内容入口，显示真实作者调用路径；只有需要编辑或观察一个 Timeline 时才要求明确选择。技能是否可试验 MUST由有效技能/输入/准入及正式产物决定，不以唯一 Timeline 为条件。

#### Scenario: Tree-only技能试验

- **WHEN** 作者选择一个没有 Timeline 的合法技能并经正式输入成功激活
- **THEN** 工作区 MUST观察该 ActionInstance 的 Tree、等待和完成/停止进度
- **AND** MUST不报告缺少唯一 Timeline 或补建空 Timeline

#### Scenario: 技能调用多个嵌套Timeline

- **WHEN** 技能 Root 经子图和 TreeClip 调用多个 Timeline，且 shared Timeline 被重复使用
- **THEN** 作者目录 MUST按调用路径列出内容，运行观察 MUST按 ActionInstance/调用 generation/playback 区分
- **AND** 选择其中一个 Timeline MUST不改变其它调用或整个技能的执行

### Requirement: 代码控制观察不得恢复角色总控图

C# 控制 MUST只通过正式 binding/参数配置和已登记的代码来源被编辑或观察，不提供角色总控 RootTree、角色外层 Action/连招 FSM 或代码/图双拓扑。普通 Locomotion、整体 Pose 和默认相机观察 MUST可以只绑定 Actor；技能未激活 MUST是正常状态，不能创建空技能或 ActionInstance 来取得观察身份。控制 State 转换、技能阶段与 PoseState MUST明确区分。

#### Scenario: 只观察普通移动

- **WHEN** Actor 没有活动技能但正在执行 C# Locomotion 控制
- **THEN** 预览 MUST显示真实控制 State/Transition、代码来源和角色表现
- **AND** MUST不要求技能或 Timeline，不生成可编辑角色图

#### Scenario: 移动状态中攻击

- **WHEN** 正式控制在不改变移动模式时接受一次攻击技能
- **THEN** 观察 MUST分别显示原控制 State 与新 ActionInstance 的技能进度
- **AND** MUST不构造移动加攻击的镜像状态或将技能后摇写成控制状态

### Requirement: 预览必须消费统一发布与执行来源合同

角色预览 MUST消费主重构拥有的角色运行包、控制模块 binding/语义/参数/状态版本、SkillProgram 目录与完整依赖、策略/资源目录、状态布局、Numeric Target、Projection 和统一代码/operation 来源。角色 Active 前 MUST经正式 Composition 校验完整组合，不只核对旧 Graph hash 或角色外壳 ProgramId。运行观察 MUST关联已登记代码位置或准确技能 operation/作者调用点，并绑定对应 ActionInstance/调用 generation；不匹配 MUST报告具体来源而不伪造 Graph 节点。

Session 和已激活技能 MUST保持锁定的代码、Program 与状态版本；新技能内容和控制实现 MUST在正式发布后由新的 Session 采用。已有合法运行参数更新继续按专属协议执行，不能当作局内替换技能模板或代码的通道。

独立 Timeline MUST消费同一正式产物与来源体系内的内容根/依赖、状态/绑定合同、Numeric Target、能力和代码/operation 来源增量，按业务 owner identity、播放 identity、调用点/generation 和场景 generation 绑定；MUST不伪造 Actor/ActionInstance/技能来源或新增来源 schema。新内容 MUST由正式发布后的新调用环境采用，活动播放 MUST不原地替换 Program。共享内容变化 MUST按正式发布组处理受影响产物。

#### Scenario: 两个非Skill调用使用同一内容

- **WHEN** 同一 shared Timeline 由两个正式调用方或两次播放使用
- **THEN** 窗口 MUST分别核对 owner、播放 identity、调用点/generation 和来源版本，明确选择实际观察目标
- **AND** 一个播放结束或场景重建 MUST不静默改绑另一个实例

#### Scenario: 角色包相同但控制实现混版

- **WHEN** 角色外壳 ProgramId 相同而实际控制模块版本或技能目录不匹配
- **THEN** 预览准备 MUST拒绝 Active 并显示精确模块或技能依赖
- **AND** MUST不回退旧 RootTree、旧 ABI 或其它模块实现

#### Scenario: 控制代码发起技能

- **WHEN** 正式诊断记录控制模块代码来源及其接受的 ActionInstance
- **THEN** 窗口 MUST能导航代码来源，再观察该释放的技能 Root/operation/Timeline
- **AND** MUST不要求角色 Graph 节点才能建立关系

#### Scenario: 技能数据重新发布

- **WHEN** 新构建改变了当前技能或其共享子图
- **THEN** 当前释放 MUST不被原地换成新 Program；预览采用新版本 MUST明确结束并建立新 Session
- **AND** MUST不通过调参候选写入生成技能数据

### Requirement: 相机必须作为场景正式表现提供者接入

场景预览 MUST唯一拥有受控 Play 的启动、暂停、重建和结束。Camera MUST只提供正式 Runtime、Projection、Rig/目标/物理绑定、正式重置/释放与只读诊断，其内部效果生命周期仍由正式相机 Runtime 拥有。技能相机请求 MUST来自唯一正式技能/Action 输出，默认跟随 MUST读取 Actor 和正式视角输入。可复用 fixture MUST只作为场景条件或既有输入源，不创建另一套命令容器、播放器、时钟或状态 seek。

#### Scenario: 预览技能镜头

- **WHEN** 正式技能输出相机请求
- **THEN** 镜头 MUST使用同次运行提交的 Body/最终动画和相机计划
- **AND** Timeline 或 Camera 窗口 MUST不再注入独立预览命令

#### Scenario: 相机观察窗口关闭

- **WHEN** 作者关闭相机观察区域而场景继续运行
- **THEN** 只读观察 interest MUST释放，场景与正式相机 MUST继续由其 owner 管理
- **AND** Camera 观察窗口 MUST不停止整个 Play 或清空角色状态

### Requirement: 跨重构比较必须区分身份变化与业务变化

同版本的产物和运行重复性 MUST继续按正式 identity 与状态规则核对。跨主重构版本的 ProgramHash、LayoutHash、EventId 和 source identity 改变时，既有比较工具 MUST明确记录来源/版本映射并比较语义输入、Body、动作阶段、窗口与输出；不同 hash 不能单独证明回归，也不能用 identity 已变为理由忽略业务差异。历史视图 MUST使用记录对应的来源版本，不用新 Source Map 解释旧事件。

#### Scenario: 主重构前后业务比较

- **WHEN** 两份已有记录跨越角色图到代码控制/技能运行包的迁移
- **THEN** 比较 MUST标明 schema/identity 变化并按可映射业务事实报告差异
- **AND** 无法映射的项 MUST明确不可比较，不能计为通过或业务失败
