# Timeline 与预览窗口联动实施计划

2026-09-13 补充：Timeline原UI的具体解除组件/Actor依赖方案见[源码解耦决策](slate-source-decoupling.md)，其中D8约束编辑游标和外部运行观察。ce21aec8f/afcb90056当前仍使用隐藏组件代理，不能视为解耦完成；本次只更新编辑侧接线决策，不重写已正确的预览运行链。下文早期实现观察按其日期理解，不是今天的逐项完成报告。

## 目标与归属

2026-09-13 用户纠正：撤销另做 Slate Surface/Editor Model/曲线交互的方案，实现窗口已提交 0aa52f209 回退。改为直接修改 Slate 原源码：原绘制和操作保留，数据绑定换为正式 Timeline/薄 adapter，Actor/Director和临时组件树相关无关绑定清除。本地编辑不依赖启动 ScenePlay，预览联动使用原正式运行链。

2026-09-13 r2：作者输入采用 [公共 C# authoring 方案](../remove-agent-authoring-use-native-csharp/design.md)，不再依赖 Agent Document 版本或五工具生命周期。现有 Timeline 编辑 Session、运行 Session、owner、Undo、时钟、Build/adoption 和已正确界面保持原归属，不按 Document/JSON 名字整目录删除业务代码。

2026-09-12 用户要求将“预览”任务的窗口一起规划，写入 Timeline change，后续随 Timeline 一起实施。本文件是两边 UI、导航、binding 和编辑后采用的统一实施入口。这里的“一起”是一次完整作者操作交付，不是把所有场景运行按钮塞进 Timeline。

- SkillGraph/共享 Graph Shell 的预览区：配置场景、选择正式目标、开始/暂停/继续/结束、请求技能或合法独立调用、Build/采用、运行观察与历史。
- Timeline：新增和编辑 Track/Clip/Curve/Section、作者帧、属性；显示精确调用的运行标记，并提供返回预览入口。
- Unity Game/Scene 视图：显示实际角色、地形、相机和场景结果，不新增一个有自己相机/播放器的 Timeline 预览窗口。
- 原场景预览 change 继续拥有协调器、启动器、Session/adoption/restore 等运行合同。联合排期不复制其状态机，不重写已正确的运行算法。

已有真实预览场景、角色和相机由原正式 owner 管理，Session、Build/采用与三种时间状态保持。不得为恢复 Slate UI 或去依赖重做预览，具体代码回退范围由实现窗口核对。

## 已核对证据与可靠性

引用任务：[预览](thread://01a08127-0be6-7f52-b555-3d36121c7a73?hostId=local)。已调用 read_thread；最近若干回合返回空 items，能读到的历史正文包含“不整支合并 dirty 主线”“Presentation restore 缺 owner/端到端证据”的旧结论。因此不能声称已完整读到该任务最新 MR 状态。

主线 implementation-audit.md 的 2026-09-11 批次1/2已记录场景合同、协调器、Graph Shell 控制面和诊断/模拟合同进入主线，并记录当时 Unity 编译结果；历史 worktree 阻塞不直接当成今天主线缺代码。

本轮只读代码证据：

| 代码 | 现状和需要处理的点 |
|---|---|
| BtsmtlScenePlayGraphShellToolbar.cs | Scene Play Context 对象框、Paused、五个运行按钮、Build/Skill、录输入、Restore/Replay 字段都直接加入同一 VisualElement；缺少按任务分组的完整布局 |
| 同文件 Create(shell) | 仅为 BaseTreeWindow 创建扩展；不能据此宣称实际 FlowCanvas SkillGraph 已有相同控制入口 |
| BtsmtlSkillObservationToolbar.cs | FlowCanvas 已有采集端口值、执行实例、返回父调用；需要在实际 SkillGraph 宿主接入共享预览 presenter，不能再开旧树窗口顶替 |
| RefreshHistoryControls | 每次刷新重写 Restore/Replay 的 Tick 输入，可能覆盖作者选择；存在 checkpoint 不等于用户选定 Tick 可恢复 |
| BtsmtlScenePlayPreviewCoordinator.cs | 已有 Start/Pause/Resume/Reset/Stop/Build/RequestSkill/ResumeFromTick/ReplayInputRange；Reset 实际重载场景，不能叫“回到第一帧” |
| Timeline projection | ApplyRuntimeOverlay 存在，当前搜索未找到正式调用方；方法存在不能视为跨窗口观察已完成 |
| CharacterSimulationActorRegistration / SessionHost | 已有 Presentation checkpoint 委派及能力检查；实际目标是否可完整恢复仍需运行 owner 和证据，不能根据接口名判完成 |

## 作者最终看到的预览区

复用当前 SkillGraph 宿主和共享控制实现，预览区可展开/收起。内容按如下层次布局，不把恢复和录制字段铺在主工具栏：

```text
预览场景：[场景资产]  目标：[角色或正式独立调用方]  [设置]
[开始预览]  [暂停/继续]  [结束预览]  [更多]
状态：未开始 / 准备中 / 运行中 / 已暂停 / 失败原因

试验：[技能或该调用方允许的内容] [执行] [打开对应 Timeline]
作者已修改 → [构建并采用] → 等待采用 / 已采用 / 下一次激活采用

观察：[实例与调用路径] [跟随/固定] [实时/历史]
  活动节点、Timeline、TreeClip、逻辑 Tick、实际结果
历史与录制（默认折叠）
  诊断采集 | 输入录制 | 历史位置 | 返回实时
  从此 Tick 恢复并继续 | 按输入区间回放（能力就绪才可操作）
```

- 场景通过 SceneAsset 选择；场景内 Context 保存正式声明。不能要求作者先打开该场景拖一个 GameObject 才能启动。进入后根据本次请求与登记 context 精确绑定。
- 开始前场景选择有效；运行中固定场景，切换观察目标不等于重启。只有显式结束/重建才改变运行生命周期。
- 暂停和继续共用固定宽度按钮；更多中放“重建场景试验”和“启动后暂停”，避免 Reset/Stop 含义不清。
- 常用按钮使用清楚中文和 tooltip；长 identity/hash 放可展开诊断。窄窗口每组自然换行，次要操作折叠，主图不被预览工具占满。
- 采集端口值、诊断 Capture、输入录制分别解释用途，不用一个“录制”开关冒充三者。
- 不强制增加每个窗口一套控制栏。共享 presenter 管理命令/能力/状态，不同原生宿主只承担绘制；角色执行只有唯一 coordinator/Session。

## 两种正式入口

| 类型 | 作者怎么用 | 不能混淆的边界 |
|---|---|---|
| 技能预览 | 选场景和 Actor，选其正式技能/调用点，开始预览后执行技能；从实际调用打开 Timeline | RequestSkill 经正式输入/C#控制/Action 准入；收到请求不等于技能已经执行成功 |
| 纯 Timeline 预览 | 选 shared 内容和场景声明的非 Skill 调用方，通过其正式业务入口开始 | 必须有内容产物、目标/参数绑定与实际 playback identity；没有合法调用方时保持编辑并说明缺失，不伪造 Actor/Skill |

Tree-only 技能可正常试验，不要求必须打开 Timeline。控制用 MotionCurve 载体也不能仅因是 TimelineAsset 就直接当作可执行技能。普通移动和默认相机可在 Actor 层观察，不需要空技能。

## Timeline 与预览如何连起来

本地作者硬边界：打开/刷新/新增/选择/编辑/关闭不创建或依赖 Slate GameObject/Cutscene/Director/Group/Track/ActionClip 组件树，也不因没有 Slate Actor/Director 而拒绝正式 TimelineData 合法操作。回退后的 BuildProjection 组件残留必须清理，不能以预览联动为由保留；真实预览场景对象继续由原运行 owner 拥有。

Timeline 继续通过现有适配层接正式内容、编辑状态与外部运行观察，复用 Slate 已有 UI。没有运行绑定时仍可编辑；不新建 Editor Model/Surface，也不把预览接线当成改造曲线框架的理由。

```text
SkillGraph 选定作者技能/调用点
  -> 共享预览命令进入正式场景运行
  -> 实际准入结果给出 ActionInstance 或非Skill playback identity
  -> 诊断 binding 发布准确来源、调用路径、generation、版本
  -> Timeline 显示该实例的进度，作者游标保持自己的帧位置

Timeline 修改并保存
  -> 正式 Mutation/Undo 产生作者 revision
  -> 预览区显示“作者已修改，运行尚未采用”
  -> 显式 Build/参数更新，经正式 owner 确认采用
  -> 同一 Session 中按兼容边界生效；Timeline 编辑状态保留
```

- 共享模板被多次调用时列出实例和路径，Follow/Pin 语义明确，不按首项选择。结束的实例保留终态，不能静默换到新调用。
- Timeline 打开请求携带明确作者 locator 与可选观察 binding。没有绑定不妨碍本地编辑。
- Timeline 的编辑帧、真实运行位置和历史位置分别保存；点击标尺仅定位编辑，不能调用 Slate Sample 或恢复真实状态。
- 从 Timeline 返回预览、TreeClip 下钻、关闭 Timeline、折叠面板均不改变 Session。只有明确的场景生命周期命令才能停止/重建。
- “打开对应 Timeline”在多个真实调用时先提供列表；没有 Timeline 时明确显示该技能无 Timeline，不报配置错误。
- 场景重建后旧 generation 失效，按正式身份重新连接；失败时显示未绑定，不按同名对象恢复。
- 自动刷新只更新状态与观察缓存，不覆盖焦点、未提交文本、Tick 选择、缩放和滚动；历史范围只在首次进入或明确“使用最新范围”时初始化。
- Timeline toolbar 本次增加“预览”导航和短只读关联状态，不增加第二个本地 Play。用户本次授权联合规划不等于已经选择 Timeline 内 Scene Play 快捷控制。

## 命令状态与失败反馈

| 情况 | UI 与正式行为 |
|---|---|
| 未选择场景/缺少 Context | 禁用开始，给出具体缺项；作者内容仍可编辑 |
| 启动中/准备中 | 显示真实阶段，防止重复请求；取消沿启动器清理 |
| Running/Paused | 暂停/继续与 Unity 实际状态一致；暂停不由 Editor 主动推进业务帧 |
| 请求技能被拒绝 | 显示实际拒绝原因；不产生假的“播放中”标记 |
| 作者修改但未采用 | 分别显示作者 revision 与运行采用状态，允许继续编辑 |
| Build 成功、候选待采用 | 显示等待边界；收到正式采用报告后才显示已采用 |
| 当前 Action 不兼容 | 保留当前运行版本，显示下一次 Action 采用；重启只在正式合同要求时显式执行 |
| Build/采用失败 | 保留作者修改与旧运行结果，给出失败阶段，不自动回退资产 |
| 外部 Unity Play | 只允许正式观察；预览控制不能自动抢占 |
| 结束/故障 | 清理受控资源并恢复编辑环境，保留最终原因；不撤销已保存作者数据 |

同 Session 编辑与 Build/adoption 保持原正式合同。C# 编译/Domain Reload、明确的场景重建、底层布局不兼容要求重启，必须单独说明，不能承诺任何修改都可不停机采用。

## 历史、恢复、回放的边界

看历史只读诊断记录，不倒放场景。只有记录包含正式画面/表现数据时才显示其记录结果；缺记录不能按当前资产补算。

“从此 Tick 恢复并继续”会改变真实运行，必须检查目标的正式 checkpoint、Simulation 和 Presentation 恢复能力、版本、分支与目标 Tick。操作接受不等于已恢复到目标；展示正式完成/失败结果和新 ExecutionBranch。

输入回放沿既有录制格式、输入 owner 和区间命令，校验起止 Tick、checkpoint、版本和外部结果的正式支持范围；不把任意曲线拖动变成 Gameplay seek。能力不足时给出精确原因，对应运行能力继续归原 change 未完成项；不能因为 UI 可禁用就宣称完整恢复已交付。

## 联合实施批次与输入输出

Timeline 原第11节替代编辑器方案已撤销，重写后的第11节只负责原 Slate 源码的数据绑定替换和无关对象清理。实现窗口恢复原功能后按该清单就地改接，P1–P5 保留已正确联动，不再等待一个新 Editor Model。

| 批次 | 输入 | 输出 | 原任务归属 |
|---|---|---|---|
| P1 统一预览表面 | 当前 SkillGraph/Graph Shell 实际宿主、operations/status、场景定位合同 | 分组布局、明确按钮、能力/状态 presenter，删除重复控制入口 | 预览 6.1/9.4 |
| P2 场景与目标接线 | SceneAsset/context、正式技能或独立调用方、启动器 | 精确准备/绑定/请求及原因，技能和纯Timeline都按自己的领域显示 | 预览 2.5/3.x/4.7/5.2–5.4 |
| P3 Timeline 联动 | 作者 ID、实际调用身份、版本、diagnostics | 双向导航、运行标记、作者帧隔离、选择/草稿恢复 | Timeline 第7节；预览 6.2/6.3/6.8 |
| P4 编辑后采用 | 正式作者修改、Build 报告、adoption/参数确认 | 已修改/待生效/已采用/失败状态，同 Session 更新与合法重启原因 | 预览 7.x/8.x |
| P5 历史与清理 | 真实历史、checkpoint、目标能力与生命周期 | 历史面板、恢复/回放正确门禁、无输入被刷新覆盖、完整释放 | 预览 4.9/6.10/6.11/9.x |

P1/P3 与 Timeline 布局、帧、新增一起排期；P2/P4/P5 逐项核对主线正式合同和真实证据。接口缺失只记录具体依赖，不复制旧分支整份实现，不增加默认播放器。Pose/MM/相机算法和 Presentation checkpoint 实现不因联合 UI 规划而改由窗口拥有。

## 联合完成标准

本轮行为条件：Timeline 使用 Slate 原有绘制/交互/曲线代码，数据绑定接正式 Timeline，打开/编辑不创建临时代用组件树；没有新写的替代编辑器。现有预览场景对象保留，不能把删除UI代理误扩为删除真实运行对象。

人工行为标准放本文件，不加入 tasks 的手动测试项：

1. 作者选场景/Actor/技能，能清楚区分开始场景与执行技能；实际结果在 Game/Scene 中出现。
2. 技能产生 Timeline 调用后，打开准确内容并看到准确实例进度；无 Timeline、多调用、重复 shared 模板均有明确结果。
3. 修改 Clip/曲线、Undo/Redo 后作者数据正确，预览区报告真实采用状态；普通编辑/导航不更换 Session，不丢视图。
4. 暂停/继续/结束、原生 Unity Stop 和场景重建后，所有面板状态与真实运行一致，无残留绑定和重复推进。
5. 非 Skill shared Timeline 经正式调用方运行；无调用方时能编辑且缺项清楚，不伪造技能预览。
6. 历史选择和 Tick 文本不被新数据刷新覆盖；浏览不修改现场，恢复/回放只在正式能力/记录满足时执行并有完成结果。
7. 窄/宽窗口、展开/收起、频繁切页无遮挡或漂浮控件。只通过编译或显示按钮不算上述行为完成。

本次仅写规划，不运行 Build/Play，不创建实现窗口。完整联动未满足上述标准时必须列出剩余问题；不能以 Timeline UI 通过替代真实预览验收。

## 文档衔接

本文件拥有跨窗口体验、联动批次和共同验收。Timeline design/tasks 继续拥有编辑器实现细节；rebuild-btsmtl-preview-with-scene-play 保留场景/运行合同和原实现任务，并以链接引用本计划，避免复制两套待办。

旧“结构必须在 Edit Mode”“结构变更一律换 Session”“禁止一切恢复”与已批准的同 Session adoption/restore 合同冲突，联合接线使用现有精确能力与正式采用规则。旧 Agent 文件协议/版本不再是预览配置、编辑资格或采用前置条件；已有领域 Capability 和业务规则保留。

## 显式作者操作与预览

- 人工修改/拖动/保存 Timeline 只写正式 TimelineData，经原 Undo，不生成 C#，不增加源码同步状态、源码 Undo 或导出 UI。
- export_code 从当前资产完整输出 C#；generate_assets 执行当前已编译入口，重建并保存明确生成范围和根挂接。重新生成不会合并未导出修改，保留修改需先显式导出。
- 源码编译不自动 generate_assets；两个作者 MCP 不自动 Character Build 或 Play。预览仍使用原显式 Build/采用入口。
- TimelineAuthoringClipBinding.cs的JSON退役、typed配置和公共输出/生成归C# authoring；当前BtsmtlSlateTimelineProjection.cs及其向非组件BtsmtlSlateTimelineBinding的迁移由Timeline任务维护，DirectProjection已回退删除，不复制业务模型或中央Validator。
- 生成内部引用使用新建对象，范围外共享图和原始资源作为精确外部输入。仅生成范围内物理对象可替换，业务 identity/引用和根 owner 挂接须恢复并保存。预览按已有 binding/revision 规则刷新，不把旧对象或 Slate proxy 当成生成输入。
- 本轮只更新 Timeline 目录内规划，不修改其它预览任务文档或实现；不因协议退役重做已正确 UI/预览，也不将新接线列为完成。
