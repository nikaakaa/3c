## Why

Timeline 已有可单独编辑的数据和共享资产，但编译入口、运行状态、TreeClip、动作上下文与输出仍依赖 Character 管线；只增加播放方法或传入上下文，无法使它真正独立使用。用户已确认本次规划同时支持 Skill 与非 Skill 的 C# 调用，并保留 TreeClip，因此需要提取共用的内容编译和执行能力，再让不同调用方提供各自的状态、时间和业务绑定。

## What Changes

- **BREAKING**：统一 Timeline 数据、Track/Clip 类型合同、时间推进与停止规则。Timeline 不按技能、机关等业务派生不同类型；行为差异属于片段执行模块，资源和强度差异属于参数。
- **BREAKING**：从 Character 编译与运行代码中提取共用 Timeline 内容编译、时间执行和 TreeClip 调用能力。共用部分不依赖 Character Definition、ActionInstance、角色状态容器、Unity 场景对象或 Graph 父对象；Character 继续调用同一份实现，删除被替代的旧实现和接口。
- 保留数据编译：Track、Clip 与嵌套树经同一能力目录、语义发射、数值降低和产物校验形成只读内容。Skill 引用与独立 Timeline 资产构建复用该链；独立构建不伪造 Character Definition、TimelineNode 或 Skill Root。
- 明确调用参数：作者只配置内容及确实需要外部提供的目标/参数；程序装配领域执行接口。Skill 调用自动沿当前角色和 ActionInstance 取得已有信息；非 Skill 调用显式提供实际目标和参数，TreeClip 复用同次绑定，不通过场景搜索或万能上下文取值。
- Skill 路径保持唯一 Action 准入、ActionInstance/SkillExecutionState、SimulationTick、Decision/Commit、WorldResolve/Finalize 和提交链。多个或嵌套 Timeline 继续是技能内部内容，不新增 SkillInstance 生命周期。
- 动画表现的修正策略由每个 Animation Clip 明确选择：跟随修正后的逻辑进度，或让仍有效的同次播放保持连续。同一 Track 可以包含不同策略；运行时区分 Clip 的每次播放。Skill、伤害窗口和位移等模拟状态仍按角色管线恢复，动画策略不授予跳过模拟恢复的权限。
- 建立正式非 Skill 调用入口，完整提供准备、开始、推进、查询、停止、销毁和只读诊断；同一资产的并发播放各自保存状态。直接调用使用共用执行模块，不恢复作者对象解释器或第二 TreeClip scheduler。
- 非 Skill 可用能力由内容依赖与调用环境共同校验。交付一个有真实目标绑定、曲线片段和条件 TreeClip 的本地场景表现用例；伤害、ActionWindow、角色 MotionWarp、角色动画及相机输出继续要求其正式领域合同。独立播放不授予修改 Character/World 状态的权限，不补齐完整机关碰撞、战斗、Audio/VFX 或网络业务。
- **BREAKING**：Track/Clip 的字段、允许组合、重叠规则、绑定需求和执行能力进入唯一领域合同，供编辑器、Document、编译器与运行装配共同使用。新增片段必须完成整条接入，不能只登记菜单或留下空执行器。
- 在主重构的唯一 Document v5 上增加独立 Timeline 目标闭包，复用五个生命周期工具、整包事务和相同 Timeline/Tree Mutation；不建第二套文档格式，不把运行目标写入作者资产。其他 domain 按各自正式变更保留或退役，本提案不要求保留游戏 AIController；新 AI 提案删除该 domain 时不得被本次增量恢复。
- 作者继续在现有 Timeline 窗口编辑、提取共享资产和下钻 TreeClip；增加清楚的目标/参数要求及缺失绑定提示。预览会话继续由 ScenePlay change 拥有，本变更只提供正式调用与观察合同，不新增窗口播放器或任意 seek 修改 Gameplay。

## Capabilities

### New Capabilities

- `timeline-runtime-core`：统一内容模型、Track/Clip 能力、时间与生命周期、共享编译、可选 TreeClip 执行和内容版本。
- `timeline-caller-integration`：调用参数与领域接口绑定、Skill 接入、正式非 Skill 播放、状态所有权、输出权限及实际独立用例。

### Modified Capabilities

- `btsmtl-runnable-timeline-node`：节点作为 Graph 调用适配，复用共用 Timeline 内容与执行，保留技能阶段及停止语义。
- `btsmtl-gameplay-semantic-ir`：Character 组合与共用 Timeline 内容发射分责，增加明确的独立 Timeline 语义根。
- `btsmtl-compiled-simulation-program`：共享 Timeline 内容单元、独立产物根、相同校验与发布基础，以及 Character 绑定/状态布局的边界。
- `btsmtl-graph-core`：TreeClip 在两类调用方中均使用编译后的树执行，不借用非 Character 作者对象解释器。
- `character-simulation-kernel`：Character 在唯一事务内接入提取后的执行模块，领域输出权限不扩大。
- `character-presentation-interpolation`：按 Clip 区分动画表现的进度修正策略，保留本次播放身份、最终动作分支、取消及既有确认终态规则。
- `btsmtl-timeline-animation-authoring-surface`：通用片段目录、外部目标/参数表面、独立编辑和精确运行实例观察。
- `btsmtl-agent-authoring-document-sync`：在唯一 v5 增加独立 Timeline 整包目标及其依赖闭包，保持共享资产与运行绑定的区别。
- `btsmtl-agent-authoring-mcp-bridge`：原五个生命周期工具接受正式 Timeline domain，精确资产构建仍与 apply 分离。
- `btsmtl-runtime-diagnostics`：区分内容调用点、调用方、本次播放与可选 ActionInstance，保持一个只读诊断链。
- `unity-simulation-assembly-ownership`：Timeline 数据/执行、Tree 扩展、Character 接入与 Unity 非 Skill 接入的单向依赖。

## Impact

- 主要代码范围：`Main/Runtime/BTSMTL/Timeline`、`Main/Runtime/Simulation/Core/Execution`、Float32/Fixed 的 Timeline target 与执行装配、`Main/Editor/CharacterSimulation/Compilation`、Timeline 作者窗口和 Agent Document 模块。
- 真正的拆分点包括 `CharacterAuthoringTimelineRecord` 的 TimelineNode 依赖、`CharacterSemanticEmitter` 的 Character 上下文、`TimelineControlRuntime` 的动作/运动/表现知识、typed 状态端口和 TreeClip 的 Character Program 限制；不能用移目录、改名字或空接口代替。
- 依赖 `refactor-btsmtl-authoring-architecture` 已确认的技能/控制边界与 Document v5 目标。该 change 正在实施，本提案不把其全部目标视为已安装能力，也不覆盖其未提交代码；共享模块接线以实际合同为准。
- 与 `replace-btsmtl-ai-with-behavior-designer` 对账：旧游戏 AI 的图/Program/Document domain 删除由 AI 变更拥有；Timeline 不保留它们，也不让插件 AI 绕过正式 Character 输入直接启动技能或 Timeline。仍被 Skill/Timeline 使用的共用树控制、状态访问和作者基础不得随 AI 目录整体删除。
- `rebuild-btsmtl-preview-with-scene-play` 继续拥有预览运行；相机 change 继续拥有相机数学和已提交命令消费。本次不进入相机 worktree，不改动正确的 Motion/KCC/Pose/IK/相机算法。
- 现行规范中的 Character 唯一编译根、TreeClip 仅 Character 执行、Document 两种 domain 与 v4 表述需按各自适用范围调整；现行角色管线、Action 身份和表现所有权继续保持。设计包含逐项对账、并行 change 的合并规则和删除清单。
- 本次仅生成规划文件与 delta specs，不修改 current specs、实现代码或 Unity 资产。后续实施使用正式 Development Center 编译和已有回放/运行证据，不新增测试代码，不把手动验收列入 tasks。
