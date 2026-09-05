## 1. 核对实际接线与保护范围

- [ ] 1.1 核对主重构已提供的 SkillProgram、ActionInstance/SkillExecutionState、树控制和 Document v5 合同，交付精确代码入口、提交/工作区状态及缺口记录；不得以未接通类型作为完成依据。
- [ ] 1.2 在正式 Development Center 改动记录中登记本变更、精确 Unity 项目与角色内容基线，关联已有输入回放和源码/产物身份；以可追溯记录作为交付物。
- [ ] 1.3 登记现有 Track/Clip、TreeClip 节点、重叠和停止语义，以及所有 Character 依赖，交付“现有入口—新 owner—保留行为—删除项”映射。
- [ ] 1.4 列出受影响 shared/inline 资产、managed-reference 类型、asmdef、portable source set 和产物组；通过正式引用解析记录完整迁移闭包，实际冲突报告用户。
- [ ] 1.5 按设计中的接口交接表，与主重构对齐共享控制、Skill/ActionInstance、generation、两数值目标、SemanticEmitter 和 v5，并与 Camera/ScenePlay 对齐领域输出/消费合同；交付提供提交、签名、负责方及逐项接管记录，未交接的同文件改动不得用副本或临时适配绕过。

## 2. 统一内容与领域类型合同

- [ ] 2.1 在现有 Timeline 数据模型中加入稳定 typed 外部目标/参数声明，保持真实 inline/shared owner；以作者投影和正式 Validator 结果确认无运行对象或状态写入资产。
- [ ] 2.2 将 Track/Clip kind、字段、允许组合、重叠规则和能力需求收敛到领域合同，以 UI/Document/构建使用相同声明的代码接线和目录输出验证。
- [ ] 2.3 按领域迁移现有动画、MotionCurve/MotionWarp、Cue 和相机片段的定义与执行登记，交付前后字段/语义映射；不得改变正确的领域数学和输出规则。
- [ ] 2.4 完成内容全闭包的依赖汇总与 typed 绑定校验，覆盖 TreeClip 和嵌套子树；以正式 Validator 对缺失目标、类型错误和缺失能力的精确诊断验证。
- [ ] 2.5 分开固定调用值、领域绑定与本 Tick 执行视图，预解析运行使用的 typed index/handle；以状态/输入合同和调用链证明不存在对象字典、场景搜索或跨 Tick 事务引用。

## 3. 提取共用编译与产物发布

- [ ] 3.1 将 Timeline 内容发现记录与 Graph/C# 调用点来源分离，去掉内容编译必需的 TimelineNode；以无 Graph 调用节点的独立内容发现产物验证。
- [ ] 3.2 将 Track/Clip/TreeClip 内容发射迁入共用领域模块，Character 前端改为调用该链；以登记清单和编译来源报告确认每种类型只有一个 emitter 语义。
- [ ] 3.3 建立只读 Timeline 内容单元、局部状态布局和外部绑定签名，链接 Skill 内容与角色组合；以正式产物读取报告验证没有场景引用或某 Actor 的可变全局地址。
- [ ] 3.4 在现有 artifact/store/codec/原子发布基础增加明确 Timeline 根类型与精确 Build 入口，完成内容、资源映射和依赖组；以实际独立产物及重读/hash 校验报告验证。
- [ ] 3.5 同步 Float32/Fixed lowering、Operation Set、受影响 ABI 和依赖版本检查；以两个正式目标构建结果及明确的未知/过期能力诊断验证，不保留旧 reader 或默认目标路径。

## 4. 提取时间执行并接回 Skill

- [ ] 4.1 从现有控制模块提取时间分段、循环和片段生命周期，使其通过 typed 状态视图执行；以正式程序集依赖及调用点检查确认核心不再依赖 Character/Action 业务。
- [ ] 4.2 将 Action 有效性、角色 Blackboard、Motion 和 Presentation 处理留在 Character 接入，删除时间核心对应知识；以原领域输出链和精确缺失能力诊断验证。
- [ ] 4.3 将 Float32/Fixed 的 Skill Timeline 状态映射到现有 ActionInstance 内的技能状态，同步 codec/hash/restore 接线；以正式状态布局、产物读取及恢复诊断验证无独立状态镜像。
- [ ] 4.4 保持活动 Timeline Decision、C# 控制决策和技能 Commit 的原顺序，以及唯一 Evaluate/WorldResolve/Finalize/Commit；用正式已有回放报告比较窗口、动作阶段和输出。
- [ ] 4.5 接管自然完成、graceful/force stop、实例失效和嵌套清理，移除被替代的时间/停止实现；以已有回放中的停止来源和状态诊断确认完整 owner 范围。
- [ ] 4.6 按相机提供的正式版本接通持续/瞬时片段的真实调用身份、sample/cycle、单次权重采样和显式停止，覆盖零权重进入、跨完整短片段、逐实例 force 及视觉尾段归属；以相机领域诊断和已提交事件对账证明未重建同实例时钟、未延长 Action 生命周期。

## 5. 完成共用 TreeClip 与非 Skill 调用

- [ ] 5.1 将 TreeClip 接到共用编译树控制，分开纯 Decision 校验与 Character ActionWindow 投影；以合法非角色条件树产物及非法角色节点诊断验证。
- [ ] 5.2 完成 TreeClip/子树签名绑定、局部状态、等待、调用 generation 和父子停止，保留非递归及现有局部树能力；交付实际调用/停止 Trace 和对应状态布局。
- [ ] 5.3 建立非 Skill 调用方拥有的 typed 状态与 Prepare/Start/Advance/Status/Stop/Dispose 接线；以完整生命周期报告确认无需 Character Definition、组件、Skill 或 ActionInstance。
- [ ] 5.4 完成非 Skill 正式帧的事实读取、Decision/Commit、待提交状态及受限输出发布，失败不发布本帧待提交内容；以正式运行错误状态和输出报告验证。
- [ ] 5.5 完成并发播放、旧句柄/generation 拒绝、明确目标绑定及 owner teardown；以同内容双调用运行诊断验证状态隔离和独立停止。

## 6. 接通有实际结果的独立场景内容

- [ ] 6.1 建立场景表现参数领域的 typed 输入、曲线片段、树输出节点和接收模块，限制为声明的标量/布尔参数，并完整处理同目标参数的写入占用/停止释放；以能力目录、冲突诊断和输出接线验证，禁止任意对象/属性反射写入。
- [ ] 6.2 配置同一“面板展开”Timeline 的两个明确非碰撞表现目标，通过业务 C# 使用正式非 Skill 入口；交付正式资源、配置和绑定校验结果，不创建伪角色或临时 fixture。
- [ ] 6.3 让条件 TreeClip 与曲线片段通过同一领域模块产生实际展开表现，完成停止/退出接收；以正式运行中的参数结果、目标 identity 和生命周期 Trace 验证。
- [ ] 6.4 完成对角色骨骼、Gameplay Body、碰撞控制及未安装角色输出能力的绑定拒绝；以正式 Validator/准备报告确认独立入口不能绕过已有模拟边界。

## 7. 同步作者界面与唯一 Document v5

- [ ] 7.1 在现有 Timeline 窗口接入领域片段目录和外部目标/参数表面，保留字段草稿、Undo、inline/shared 与本地编辑；以共享 Mutation 接线和作者校验结果确认无服务配置字段。
- [ ] 7.2 使无来源 Graph 的 shared Timeline 可以经现有 Graph Shell 编辑 TreeClip，传递真实 owner/声明输入；交付打开请求和可达作者引用检查结果，不创建假父 Graph 或新窗口体系。
- [ ] 7.3 在主重构提供的同一 v5 中增加 Timeline domain 根与完整分片闭包，同步 schema、Exporter、strict parser、Reconciler、Mutation 和 Validator；以正式 checkout/dry-run 报告验证。
- [ ] 7.4 完成 shared 资产跨 Character/Timeline 包的 revision/hash 冲突检测、全 owner Undo 与失败恢复；以正式事务报告确认部分保存被拒绝且原包保持一致。
- [ ] 7.5 扩展原五个生命周期工具和人工入口的 domain 分派，接入独立精确 Timeline Build，更新 btsmtl-agent-authoring 技能及合同地图；以工具 schema 和实际返回报告确认 apply 不自动 Build。

## 8. 接通诊断与迁移程序集

- [ ] 8.1 扩展正式 SourceMap/Trace，使 Graph 与 C# 调用点、Skill 释放与非 Skill 播放均能精确定位；以同内容双播放诊断报告验证不伪造或合并实例。
- [ ] 8.2 在 Timeline/Graph 只读观察中接入新播放来源，向 ScenePlay owner 提供带精确提交的正式根/产物、owner、identity/callsite/generation、绑定和生命周期/观察合同；生命周期按钮与运行绑定归预览、内容与独立根入口归 Timeline，以代码接线证明窗口不拥有时间执行、会话或 Gameplay seek。
- [ ] 8.3 按实际职责迁移 Timeline 核心、Tree 扩展、Character/Scene 接入与 Editor 程序集，同步项目既有 portable 构建清单；以正式依赖图和构建输出确认单向依赖。
- [ ] 8.4 通过正式 Editor 迁移更新受影响 serialized/managed-reference 类型、shared/inline 引用和内容版本；以完整资产引用/Validator 报告确认旧类型引用清零且作者 identity 保持。

## 9. 清理、对账和交付证据

- [ ] 9.1 删除接管完成后的旧 Timeline 发射、动作耦合控制、播放接口、TreeClip 对象执行和废弃配置，保留无关通用树用途；以引用搜索和正式编译确认没有兼容转发或重复执行路径。
- [ ] 9.2 经正式 Development Center 完成精确项目编译、受影响内容构建及已有角色回放比较，记录源码/产物版本、结果和缺口；比较实际窗口、动作、运动和输出，不以跨结构 hash 差异代替结论。
- [ ] 9.3 经正式运行入口收集无 Character 配置的独立双目标样例证据，覆盖条件 TreeClip、曲线、并发、停止和错误绑定；记录真实目标与播放 identity，不新增测试代码或自造编译工程。
- [ ] 9.4 对账主重构、ScenePlay 和新 AI 提案的实际 delta Requirement，按基础目标加 Timeline 范围扩展、移除已退役 AI 域合并规范，更新已安装后的 Purpose/project 口径；以严格 OpenSpec 校验和逐项差异表证明没有恢复 AI 入口、删掉共享树执行或用旧正文覆盖新合同。
- [ ] 9.5 按合同/编译、执行接管、独立调用、作者同步和清理形成中文小步提交，并提交整轮实现报告；报告附实际代码链、输入输出、删除项、正式证据及工作区状态，等待规划窗口实际 review 后再报告是否通过。
