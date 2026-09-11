2026-09-11起本change转入主线单线执行：实现、验证、任务勾选与本清单都落在主线工作树，唯一实现记录为同目录implementation-audit.md。分支`codex/btsmtl-scene-play-preview`（tip `4b7f7544e`）与其worktree同日起停写，只作为尚未复制内容的来源和历史追溯。主线尚未落地的任务由用户指定范围，从该分支按提交或文件逐块复制（cherry-pick / checkout path）；每批复制前确认主线tip与目标文件无未提交冲突，落一批在implementation-audit.md登记「分支提交↔主线提交」对应关系并同步勾选，勾选只反映主线实际进度。

任务依赖按接口推进：场景启动、恢复和共享UI可独立开展；角色实例、独立Timeline调用、Document v7与来源接线只等待各自正式合同的精确提交和发布结果，不要求其它change全量完成。本清单只实施预览消费端：主重构的已提交部分提供控制FSM、SkillProgram/ActionInstance、v7基础与来源schema；Timeline owner提供共用内容编译/执行、独立根构建、非Skill调用/状态/输出与同一v7内domain增量。主线工作树未提交的作者重构不视为可消费接口。场景控制层不要求所有目标具有角色，也不代做非Skill运行。

## 0. 主线基线与集成门禁

- [x] 0.1 以主线当前已提交 HEAD `5eeaf7c2174057059e6dc48267915afb8cd8d88d` 与预览实际消费的稳定提交集合为对账基线，区分已提交接口与主线工作树未提交的作者重构；不得从 dirty main 直接合并预览。预览已摘取 Slate embedded API `622ca053b`（预览 `a3eda34cc`）及单窗口承载语义（预览 `a40bb3c3c`），但未带入主线 `5bbb9d434` 中的独立 `TimelinePreviewSession`。预览同时保留 Pose Resource Slot 核心链 `7a53e62e8`、`e60c2ebfd`、`e5928500a`、`e3b9f81ab`、`12073c826`、`368f71cda`、`0f31c2f33`、`38bf4b42c`，以及动态 Scene Play/Diagnostics adoption 链；`47a219bc0` 独立 Timeline Preview、`775f42bbe` 删除预览 adoption 接口以及主线未提交改动未带入。预览同时消费无冲突的 `cf795cd03`（`f1586da49`）、旧 Pose 迁移数据结构清理 `4b1673e60`（`f087cc1a3`）、正式 Presentation Reset capability（`d07479b0e`）和 StateMachine transition 资源槽引用扫描（主线 `5a2b4adc6`，预览 `ceda942dd`）。
- [ ] 0.2 等待主线 `refactor-agent-authoring-attribute-driven` 的正式 metadata、v7 projection、Mutation adapter 和删除清单落成，预览只消费其公开合同，不复制 Agent 领域实现。
- [x] 0.3 等待 Slate Timeline projection/transaction 与 Pose owner 的正式入口稳定，预览移除对旧 Timeline UI、Pose migration 和一次性 Corin writer 的长期依赖。主线 `622ca053b` 的 embedded API 已由预览 `a3eda34cc` 接入，单窗口 Surface 由 `a40bb3c3c` 完成；`TimelineEditorWindow` 只承载 Slate，不复制其 editor 行为。证据还包括主线 `9cae9a16f`、预览合并 `f76c2dcb1` 及迁移入口清理 `2ace76b69`。
- [x] 0.4 将预览当前 `d07479b0e` 作为过渡审计快照，完成精确合并后的类型/程序集/owner 静态对账，再继续补运行功能。对账结论见 implementation-audit.md。
- [x] 0.5 处理主线后续 `47a219bc0` 的语义冲突：该提交恢复 `TimelinePreviewSession`、`TimelinePreviewTarget` 和窗口级 Editor update/SetTime；本 change 不合入独立 Timeline 播放链，须由主线明确其仅限非技能作者工具的边界后再决定是否拆分消费。主线原生清除，记录见 implementation-audit.md。

## 1. 固定迁移范围与规范对账

- [ ] 1.1 以主重构规划 d99093011、FSM澄清 3bf66c4ea 和控制基础 cbcd7fa42 为追溯起点，记录每项实际使用接口的后续精确提交、合同版本、发布状态及缺失项；不将基础类型存在当作完整迁移已完成。
- [ ] 1.2 盘点技能Tree/局部StateMachine/嵌套Timeline、Pose、Blend Space、MM及工作区的预览调用者，交付迁移/删除清单；对齐主重构、Timeline与Camera条款及共享窗口逐段所有权，不重复实施角色RootTree删除、v5升级、非Skill执行或相机算法。
- [ ] 1.3 明确记录脚部分析生成的既有代码/spec 分歧和本次边界，在迁移表中区分完整角色播放器、离线分析/校准、原生素材编辑与独立模块诊断工具，不把后面三类误删或改成预览补充路径。

## 2. 共享合同与场景上下文

- [x] 2.1 定义场景预览操作、只读状态、请求 identity、场景 generation、ProgramEpoch adoption、历史查看和恢复阶段合同，接口能够表达开始、暂停、继续、Build、采用、重建、结束及每个失败阶段，不持有领域执行状态或提供业务Advance。
- [x] 2.2 在已有客户端Unity边界实现场景上下文声明，角色准确引用Session/Actor及正式组合配置，非Skill消费Timeline已发布的正式owner/目标绑定；登记各自精确内容根、产物和就绪结果，不伪造角色或猜测目标。
- [ ] 2.3 消费唯一v7共享Capability、领域增量与实际已发布参数合同，提供控制配置/技能/Pose及独立Timeline字段的编辑资格和采用规则；生成内容、实际对象绑定和实例状态只读，不补造热更新能力。
- [ ] 2.4 配置 Editor、客户端与公共 Composition 的单向依赖，程序集引用中不出现公共 Simulation 到 Editor、窗口、Animancer 或具体 Network Model 的新增反向依赖。

- [ ] 2.5 实现作者目标和运行目标的独立绑定，角色消费SkillDefinition/Root、Session/Actor/ActionInstance/SkillProgram/调用generation，非Skill消费精确Timeline根、业务owner/播放identity/调用点/generation；两者关联场景generation，不强制补齐另一领域字段。
- [x] 2.6 定义稳定 Session 内 ProgramEpoch、Program compatibility、ExecutionBranch、Tick history 和 checkpoint 的公开合同，并验证它们不进入 Document 可写正文。

## 3. 通用场景启动器

- [ ] 3.1 将现有 `EditorPlayModeSceneLauncher` 收敛为明确启动请求，原有调用者迁入同一入口，删除旧单 active scene 路径假设和并行恢复实现。
- [ ] 3.2 接入指定 Play 启动场景及原设置恢复，保存完整 Scene setup；启动请求结果能够区分成功、用户取消保存与配置失败。
- [ ] 3.3 将必要请求身份和编辑环境信息持久化到 editor-only 会话状态，Domain Reload 后只能重新解析合法请求，数据中不保存旧 Runtime 或 GameObject 实例。
- [ ] 3.4 按 Unity Play 生命周期完成进入、退出、取消和失败清理，终态保留操作诊断并清空待执行命令，不出现自动重启。
- [ ] 3.5 迁移现有 prepare 调用者对场景起始条件的传递，正式场景作者值不再被运行准备临时改写，既有 Launcher 仍按明确配置启动。
- [ ] 3.6 实现受控场景在 Play 内的正式重载操作，返回新的场景 generation，并由同一启动器管理加载失败和最终编辑环境恢复。

## 4. 唯一场景预览协调器

- [ ] 4.1 实现检查、等待 Build、进入 Play、准备、运行、暂停、Program adoption、历史查看、恢复、重建、停止和失败状态，公开状态能够定位当前等待或失败阶段。
- [ ] 4.2 加入实例级运行所有权和请求校验，第二次启动及外部 Play 占用返回明确结果，不自动抢占或选择其它场景。
- [ ] 4.3 从场景上下文按领域核对精确正式根、产物/绑定和owner准备结果，角色核对Character/控制/技能闭包，非Skill消费Timeline正式检查；就绪后才开放请求，缺失项可定位，不猜测目标或套用角色要求。
- [ ] 4.4 将暂停和继续连接到真实 Unity Play 状态，清理旧 Editor 定时推进依赖，公开状态与原生 Pause/Stop 操作一致。
- [ ] 4.5 实现重建前产物检查、旧目标失效及场景重载，角色经Quiesce/Dispose释放，非Skill经其正式owner停止/teardown释放播放与目标占用；保留暂停意图，通过正式调度准备和重新连接，不代做内容退出或Advance。
- [ ] 4.6 处理窗口全部关闭、Unity Stop、请求丢失和运行 Fault，窗口只撤销本地 interest，预览控制资源只由协调器与正式 owner 释放。

- [ ] 4.7 接通SkillDefinition作者选择与正式激活结果关联，明确绑定ActionInstance/调用generation和Timeline activation；拒绝、并发、替换或实例失效时不取首个同模板实例，绑定状态有准确来源。
- [x] 4.8 接通稳定 Session 的 Program adoption barrier，兼容 Program 在正式 Logic Tick 采用新 Epoch，不兼容 State Layout/调用拓扑保留旧 Action 并报告下一次 Action 采用原因。
- [x] 4.9 接通同 Session restore/replay 请求和 ExecutionBranch，验证恢复只消费正式 checkpoint、输入和外部结果，不由协调器写 Simulation State 或推进业务帧。

## 5. 独立场景与正式输入

- [ ] 5.1 使用主重构后有效的Corin Prefab、控制binding/参数、技能目录及正式Session/Composition创建 `Assets/Scenes/Authoring/BtsmtlPreview.unity`，接齐启动/Tick、物理和已安装相机依赖，资源引用通过既有校验。
- [ ] 5.2 接入场景/Actor和技能目录选择，场景保存环境与初始条件，窗口保存作者页面和观察选择；普通移动/默认相机不要求技能，缺少关联有可定位结果。
- [x] 5.3 将技能试验输入接入唯一Control Source/Ingress，由C#控制选择技能、Action服务准入建立ActionInstance并交给Skill Root；接线保持控制Transition与技能激活独立，不直接改State或实例。
- [ ] 5.4 投影真实准入/拒绝/替换结果、SkillDefinition/作者调用路径和控制代码来源；Tree-only技能合法，多Timeline按路径选择，窗口不创建替代producer或第二SkillInstance。

## 6. 作者窗口与真实运行观察

- [x] 6.0 将 Scene Play Start/Pause/Resume/Reset/Stop、Build、Skill、Live Debug、Capture、History、Restore 和 Replay 从 Timeline toolbar 移入共享 Graph Shell/SkillGraph 控制面；Timeline 只保留 Slate authoring Surface、Mutation/Undo 和被动 Runtime Trace overlay，并在同一 Session 内保持可编辑。代码提交 `ab2498864`。
- [ ] 6.1 在共享Graph Shell/技能工作区装配领域提供的场景操作与目标表面，角色控制显示配置与代码来源、技能显示Root/子图，独立内容消费其作者入口；保留现有交互组件，不在外壳新增角色必需检查或恢复已退役领域。
- [x] 6.2 将inline/shared Timeline的运行观察改为被动消费 Graph Shell 建立的正式 binding；技能观察ActionInstance/调用路径/generation/playback/cycle，独立内容消费正式owner/播放identity/调用点/generation与来源；Timeline 不提交 Scene Play 命令、不拥有窗口 evaluator 或独立 clock，内容与独立根入口仍归Timeline owner。代码提交 `ab2498864`。
- [ ] 6.3 迁移Tree/局部StateMachine/参数化子图及TreeClip下钻绑定；技能保留同Tick Decision候选与技能Commit事实，非Skill消费其正式帧/声明/已提交结果，不伪造ActionWindow；导航不执行业务，多个调用保持明确Follow/Pin。
- [ ] 6.4 迁移 Pose Graph Bottom Dock、Pose Watch 和目标选择，观察来自真实 Actor 的 committed snapshot，删除私有 Fact Preview 装配。
- [ ] 6.5 迁移 Blend Space 与 MM 的完整角色预览入口，实际 Fact/Query 来自正式角色；保留采样点、曲线和几何等作者数据绘制。
- [ ] 6.6 在SkillDefinition工作区显示Tree-only和多个/嵌套Timeline的真实结构，绑定精确ActionInstance/调用generation；仅在实际有动画时显示logic/visual sample与Slot，移除Base Pose/Action fixture。
- [ ] 6.7 迁移原生 Animation Window 的 typed navigation 签名和显式素材编辑目标，Production Prefab 不安装素材接收器，运行中的物理输出不被素材采样接管。

- [ ] 6.8 消费统一代码/operation来源及Timeline正式调用来源增量，分别导航Actor控制、技能和独立内容实例；历史按记录来源版本显示，旧Source Map不能解释新identity，失效目标不静默换绑。
- [ ] 6.9 接入Camera的正式Runtime/Projection、Rig/目标/物理及只读诊断，技能请求来自正式Action输出；场景启动/暂停/重建/结束归本协调器，清理独立Camera fixture命令源和seek计划对应消费代码。
- [x] 6.10 建立动态技能执行时间轴，把真实 Tick、等待、分支、循环、Timeline、TreeClip cycle 和 PresentationFrame 按 SourceMap/RuntimeInstanceKey 映射到作者节点；验证同一 TreeClip 多次调用不合并。
- [x] 6.11 接入历史视图与实际表现帧，历史位置只读已记录事实；验证缺少表现记录时显示不可用，不按当前 authoring time 重新求值。

## 7. 直接作者调参与运行采用

- [ ] 7.1 将各页面合法运行参数入口接到唯一v7共享Capability、正式Mutation/Validator/Undo，写入准确控制配置/技能/Presentation或Timeline作者owner；只开放有正式运行合同的字段，非法值在提交前拒绝。
- [ ] 7.2 复用精确 Actor 的现有参数候选编译和原子提交协议，保留 Program/Projection/布局身份、NextFrame/NextActivation 与 `resetOwnerState` 语义，不修改共享不可变产物。
- [ ] 7.3 实现作者已修改、运行待生效、已采用、需要 Build 和应用失败的分别显示，以正式运行确认更新状态，不用提交成功代替生效。
- [ ] 7.4 接通 Undo/Redo 的同一路径候选更新，运行应用失败时保留作者修改、Undo 和上一份运行参数，诊断能够指出两者差异。
- [ ] 7.5 处理暂停、共享 Profile 与多 Actor 的采用状态，只向明确选中的 Actor 提交，暂停期间不主动执行帧，其它 Actor 不被暗中修改。
- [ ] 7.6 处理技能Root/子图/Timeline、独立内容、控制模块合同与参数/状态布局变化，消费领域stale和发布规则；兼容角色新版本由稳定Session的ProgramEpoch采用，不兼容内容由下一次Action采用；不通过调参原地改写State或借用Actor端口。

## 8. 明确构建与阶段耗时

- [x] 8.1 接通 Build 并开始／Build 并采用：角色消费 Character Definition/Target 和完整角色闭包，独立内容消费 Timeline owner 的精确 shared 根/Target/发布目标；Build 期间保持旧 Program 运行，成功后经 ProgramEpoch adoption，失败不改变当前运行。
- [ ] 8.2 消费各领域正式构建报告，记录实际内容检查、前端/确定性检查、Numeric Target lowering及发布；角色另记录控制/表现计划与已有PipelineCompiler准备工作，独立内容不填未发生的角色阶段，不重造编译入口。
- [ ] 8.3 为已有明确分析操作记录生成或复用状态，使报告能够区分分析工作与图数据编译；不因预览新增分析生成触发或改变现有产物所有权。
- [ ] 8.4 在预览请求中记录检查、进入Play、对应正式owner准备、目标连接和重建耗时，未测量和失败阶段可见，轮询总等待不作为编译耗时。
- [ ] 8.5 将角色或独立内容产物版本和实际阶段接入共享UI与原构建任务结果，C#编译/重载与内容数据编译分开；不在Inspector绘制、selection或刷新中执行重操作。
- [ ] 8.6 拆分不可变 authoring snapshot、后台 Semantic/Program 编译和主线程 publication；验证 AssetDatabase/ScriptableObject 不进入后台线程，SourceRevision 过期时 Build 结果被丢弃。
- [x] 8.7 发布 Program、Projection、SourceMap 和 revision 后提交 adoption report；验证 ProgramHash、LayoutHash、Numeric Target、Projection contract 和 SourceMap coverage 不匹配时保留旧 Epoch。
- [ ] 8.8 记录真实 Build、发布、adoption、owner preparation 和失败阶段耗时，验证未发生的阶段不被填报，轮询等待不冒充编译耗时。

## 9. 删除被替代的完整角色预览路径

- [x] 9.1 删除窗口级 `TimelinePreviewSession` 和 `TimelinePreviewTarget` 依赖，迁移 `CharacterPipelineHost` 继承与调用者，保留已有组件资产 identity 及正式运行端口。
- [ ] 9.2 删除只服务旧完整角色预览的Controller/Runtime、worker adapter、PreviewSession/preview program及Action/Fact/Query adapters，先按提供提交核对已改调用者，不误删技能Root、ActionInstance服务或Timeline正式共用执行/非Skill接入。
- [ ] 9.3 删除旧 Pose/动画私有场景 fixture、视觉根接管与恢复、预览动画时钟、独立 MotionCurve 求值以及仅为它们存在的配置和资源引用。
- [ ] 9.4 清理旧窗口字段、UXML 控件、菜单、目标选择、playback/seek 分支和失效 editor state key，正式入口不保留旧路径开关。
- [ ] 9.5 删除完整角色预览专用编辑 seek/reset 输入，同时保留正式 Actor reset、Fault、Dispose、History View、Session restore/replay 和已验证惯性/Foot 初始化；恢复不得由窗口直接建立第二条执行路径。

## 10. Document与规范同步

- [ ] 10.1 消费主重构已提交的唯一Document v7基础与各领域正式增量，Timeline domain和分片由其owner提供；预览字段资格接入已有只读context/Capability及一致的Exporter/Codec/Reconciler/Mutation/Validator，不复制迁移器或v4分支。
- [ ] 10.2 保持v7 TreeDirty/Conflict、整包事务、五生命周期与Play Mode门禁，消费主线已批准并发布的domain集合，不写死数量或恢复已退役领域；shared资产跨包使用同一revision，导出真实作者值，不写场景对象绑定或实例状态。
- [ ] 10.3 在主重构v7技能/规范基线上合并本change预览说明，清理旧Preview Purpose与项目入口描述；安装时保留共享控制/技能/来源新合同及素材/模块诊断工具，不用旧全文覆盖。
- [ ] 10.4 核对主重构工作区、Timeline独立根/调用、domain增删和Camera预览条款的owner修订结果，确认角色限制不误扩到独立内容、唯一场景owner/v7与来源身份一致；不以其它change整份完成作为所有步骤门槛。

## 11. 集成门禁与交付

- [x] 11.1 通过现有编辑器编译与程序集门禁，区分既有错误和本次错误；遵循最新AGENTS的明确项目路径本机CLI/executeMethod规则及CI限制，保留主验收Editor，不新增测试代码。
- [ ] 11.2 通过已有场景、作者、角色包/技能闭包、Document v7和来源校验器的适用检查，交付有效Corin场景；消费Timeline owner已交付样例的目标/调用校验，记录预览接入与缺失合同，不代建独立执行样例、不修补TrainingEnemy或替换Target。
- [x] 11.3 对迁移清单执行定向源码与资源引用检查，完整角色播放入口只剩统一场景运行，原生素材编辑和离线分析入口保持完整。
- [ ] 11.4 完成OpenSpec严格校验与共享delta组合对账，按模块独立中文提交，交付接口提交/版本、实际完成范围及外部冲突；规划文件齐全不等于共享接口或运行已可用。
- [ ] 11.5 引用已有Replay/Proof或比较结果时分别核对同版本重复性与跨重构业务差异，明确Program/Layout/Event/source映射，比较语义输入、Body、动作阶段、窗口和输出；不因hash不同直接判回归或忽略差异，不新增录制格式。
- [ ] 11.6 对 Corin 旧 RootTree 的 23 个 TreeClip 与 7 条 Native Skill Timeline 的 23 个目标 TreeClip 逐项对账，验证时间范围、Decision/Commit、Blackboard 投射、生命周期子图、SourceMap 和正式 Build 闭包。
