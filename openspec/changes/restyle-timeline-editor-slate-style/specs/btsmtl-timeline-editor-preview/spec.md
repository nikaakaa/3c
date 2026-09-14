## MODIFIED Requirements

### Requirement: Timeline Editor必须直接使用Slate CutsceneEditor作为实际编辑表面

正式Timeline编辑入口 MUST使用Slate已有CutsceneEditor的真实IMGUI绘制和交互，包括时间尺、Group/Track列表、Clip、选择、拖动、缩放和Curve/DopeSheet。原Inspector控件 MUST通过正式typed字段接入Unity已有Inspector，MUST NOT在Timeline内部另建右侧Inspector或把proxy私有参数作为作者字段。Timeline Editor MUST NOT用UI Toolkit或另一套IMGUI函数重新实现Slate风格时间轴，也 MUST NOT只复用图片/skin。Slate播放器、场景绑定和运行控制 MUST NOT作为BTSMTL作者编辑依赖。

BTSMTL `TimelineData`、Track/Clip/Section/TreeClip authoring identity、SerializedOwner、Source Map、Mutation、Undo、Preview 和 Live Debug MUST继续由原业务模块拥有。Slate UI MUST通过现有数据适配读写正式内容，MUST不新建替代 Surface/Editor Model 来重做已有功能。Slate 编辑对象 MUST不保存为第二份业务资产。显式导出的 C# MAY作为声明生成范围的重建来源，生成 MUST仍产出正式 TimelineData 并恢复 owner 挂接。

BTSMTL Skill、Timeline、Preview 和 Runtime MUST NOT依赖 Slate GameObject Actor、DirectorGroup、Camera/Audio/Director Track、PlayableGraph 或 Slate Preview。ScenePlay MUST NOT成为本地作者能力的必要依赖。Timeline MUST通过直接修改 Slate 原源码的数据绑定去掉临时代用组件树及无关运行依赖，MUST保留原绘制和交互，不另写替代编辑器。

Timeline 页面 MUST只拥有作者编辑、正式 Mutation/Undo 和被动 Runtime Trace overlay。Scene Play 的 Start、Pause、Resume、Reset、Stop、Build、Skill request、Live Debug、Capture、History、Restore 和 Replay MUST由 SkillGraph/Graph Shell 调用唯一 Scene Play coordinator；Timeline 不得创建 `TimelinePreviewSession`、独立 evaluator、私有 clock 或同类运行命令。

#### Scenario: 从正式Skill Graph打开Skill Timeline

- **WHEN** 作者从正式 Skill Graph 调用点打开 Timeline
- **THEN** 系统 MUST 为当前 BTSMTL Timeline 创建或刷新 Editor-only Slate projection
- **AND** MUST在唯一 `TimelineEditorWindow` 的嵌入 Surface 中调用 Slate 原有编辑 Surface
- **AND** 该窗口 MUST显示当前 Timeline 的 Track、Clip、Section和identity映射
- **AND** BTSMTL Timeline入口 MUST NOT 创建独立的 Slate `EditorWindow`
- **AND** 不得同时打开或维护上一轮 UI Toolkit Timeline 作为第二个正式编辑表面

#### Scenario: 恢复现成Slate编辑能力

- **WHEN** 实现按用户要求回退扩大范围的编辑器重做
- **THEN** Timeline MUST复用 Slate 原有时间尺、轨道/Clip、选择/拖动/缩放和 Curve/DopeSheet
- **AND** 正式 typed 编辑、资源、Undo 和已正确的布局/Inspector MUST保持
- **AND** 后续 MUST在原函数内替换正式数据读写并删除临时代用对象，不能把回退或文档更新当成接线已完成

#### Scenario: 数据来源共用原函数主体

- **WHEN** 原生Cutscene与正式Timeline需要使用同一轨道、Clip或曲线UI
- **THEN** 数据来源 MUST在入口明确绑定，原列表、时间轴、ActionClipWrapper及Renderer MUST共用一份原绘制/事件处理主体
- **AND** MUST NOT在原入口提前返回另一套ShowEmbedded列表、时间轴或独立鼠标分支
- **AND** 原CutsceneTrack中的Editor方法 MAY为解除MonoBehaviour依赖迁入现有Editor模块并参数化，旧位置 MUST NOT继续保留重复绘制主体

#### Scenario: Slate UI编辑Clip范围

- **WHEN** 作者在 Slate `CutsceneEditor` 中移动或裁剪一个 projection Clip
- **THEN** adapter MUST按 projection identity 将结果转换成 BTSMTL frame/value mutation
- **AND** 正式写入 MUST经过 `TimelineEditorSessionContext`、owner 和 BTSMTL Mutation
- **AND** mutation 完成后 MUST从 BTSMTL owner 重新生成 projection

#### Scenario: Timeline owner外部刷新

- **WHEN** BTSMTL Timeline 被 Undo/Redo、显式 generate_assets 或其它正式业务入口修改
- **THEN** adapter MUST销毁旧 projection 的临时状态并从最新 BTSMTL Timeline 重建
- **AND** 临时状态 MUST仅指本窗口编辑草稿与失效引用，MUST NOT创建或销毁代用组件树；有效ID选择、展开和视野 MUST保持
- **AND** MUST不把旧 Slate proxy 的字段覆盖回 BTSMTL

### Requirement: Slate Projection必须是Editor-only桥接而不是第二个正式数据源

现有 adapter MUST保留正式 BTSMTL identity 与 Slate 编辑对象的映射，编辑数据 MUST不保存成另一份 Timeline 或进入 C#导出/Compiler。正式写入和 Undo MUST仍由原业务 owner 及 Session 管理，MUST不为适配新增一套业务对象或编辑器框架。

Track/Clip/Section 和曲线 MUST复用原 Slate 显示、交互与编辑工具，通过现有 adapter 对应正式字段和曲线。MUST保留 CurveEditor/DopeSheet 已有关键帧与切线功能；其参数、曲线访问、编辑时间和提交绑定 MAY按需要修改，MUST不重写绘制/交互算法或增加另一个曲线工具。

编辑输入 MUST只提供原UI需要的字段、曲线、编辑时间与正式命令，MUST NOT要求BTSMTL实现带Play/Sample/Evaluate/Actor的运行IDirector、IDirectable、IKeyable或IAnimatableData。原Editor函数所用编辑子集 MAY拆出供两种真实数据来源共用；MUST NOT使用空运行方法满足类型，也 MUST NOT创建通用Editor Model。正式对象与必要手势草稿 MUST不形成第二份序列化或运行模型。

#### Scenario: 关闭Slate窗口

- **WHEN** Slate Surface 被销毁或切换到其它 Timeline owner
- **THEN** adapter MUST沿现有生命周期清理本窗口持有的临时状态、引用及回调，不改写正式数据
- **AND** MUST保留已提交的 BTSMTL Timeline 修改
- **AND** MUST不产生 Slate Cutscene 资产或残留的可运行 GameObject

#### Scenario: Slate插件不可用

- **WHEN** 当前编辑器无法加载 Slate `CutsceneEditor` 或嵌入 Surface
- **THEN** Timeline UI MUST显示明确的 Unavailable 原因
- **AND** MUST不偷偷切回上一轮 UI Toolkit仿制界面
- **AND** MUST不创建替代数据源或默认 Timeline

### Requirement: Timeline Editor必须明确Slate临时写入与BTSMTL正式写入边界

Slate 对 proxy 的字段修改 MUST仅作为手势草稿。Adapter MUST通过 snapshot/diff 和明确 transaction callback 将一次有效 Slate 编辑转换成一次 BTSMTL Mutation，隔离 proxy 的 Undo/dirty，不允许形成第二套用户可见撤销历史。未能满足隔离要求的命令 MUST报告不可用并继续作为未完成项，不允许通过说明存在双 Undo 来宣称交付。

#### Scenario: 一次Slate拖动提交

- **WHEN** 作者完成一次 Clip 拖动
- **THEN** proxy 可以在拖动期间发生临时字段变化
- **AND** adapter MUST在提交边界只向 BTSMTL owner提交一次正式 mutation
- **AND** MUST不把每一帧 proxy变化分别写入 BTSMTL

#### Scenario: Pointer Cancel或窗口关闭

- **WHEN** Slate拖动被取消、窗口关闭或projection identity过期
- **THEN** adapter MUST丢弃未提交的 proxy变化
- **AND** MUST从 BTSMTL owner重新生成 projection
- **AND** MUST不写入半成品 TimelineData

#### Scenario: 提交与菜单事务

- **WHEN** 作者完成字段输入、菜单命令、曲线编辑或多Clip手势
- **THEN** 操作 MUST在原Session/TimelineData mutation链内完成本次完整改动的业务校验与一次正式Undo；失败 MUST恢复该次owner范围，不保留部分写入
- **AND** 普通选择、滚动、缩放、游标和折叠 MUST NOT建立作者事务；菜单 MUST NOT依赖外层MouseUp才保存
- **AND** MUST NOT把现有ApplyModify注册Undo视为已经实现自动校验或异常回滚，缺项 MUST在原正式链实现而非另建全局事务框架

### Requirement: Preview与Live Debug控制必须归Graph Shell

Scene Play Session、Runtime Trace、Follow/Pin、TreeClip ownership、Character/Actor target、Build、Skill request、Capture、History、Restore 和 Replay MUST由 Graph Shell/SkillGraph 与唯一 Scene Play coordinator 管理。Timeline 只读取正式 binding，将当前实际运行标记作为只读 overlay 显示；Timeline 不得控制运行对象或复制这些状态机。

Embedded Slate Surface MUST NOT调用 Slate `Sample`、`Play`、`PlayableGraph` 或 Actor binding 来执行 Preview。Scene Play Session 与领域 owner 是唯一时间推进和输出 owner；Slate current time 只允许作为编辑游标和被动 overlay 的显示输入。

#### Scenario: Graph Shell启动Scene Play

- **WHEN** 作者在Graph Shell点击Scene Play、构建当前技能或Skill request
- **THEN** 命令 MUST进入唯一 Scene Play coordinator 和正式 Session
- **AND** Timeline MUST只接收正式 runtime binding/overlay，不创建本地播放器
- **AND** Timeline 的作者编辑能力 MUST不因打开运行观察而复制或切换到另一个窗口

#### Scenario: Scene Play期间编辑Timeline

- **WHEN** 作者在同一 Scene Play Session 中拖动 Clip、修改 Curve 或 Section
- **THEN** Timeline MUST通过正式 BTSMTL Mutation/Undo 写入作者 Timeline
- **AND** Graph Shell MUST显示对应领域的配置版本、就绪结果、实际采用版本/实例及失败原因，MUST NOT恢复Character全量Build或统一ProgramEpoch采用
- **AND** 活动技能实例 MUST保持启动时的不可变技能版本，新版本只用于后续实例；影响Session玩法identity的变化 MUST按正式规则重新准备，普通编辑本身 MUST NOT更换Session

#### Scenario: Timeline只读观察运行

- **WHEN** Scene Play coordinator 已产生正式 Runtime Trace
- **THEN** Timeline MAY显示 active Track/Clip、logic/visual time、TreeClip phase 和 playback identity overlay
- **AND** overlay MUST只读，Timeline 不得暂停、恢复、重置、恢复历史或回放运行对象

### Requirement: Timeline新增Track与Clip必须使用正式typed authoring contract

Timeline Editor MUST提供正式的 Add Track/Add Clip 作者入口；候选类型 MUST来自当前 Timeline owner 的 `TimelineContractCatalog` 和 Track contract 的 allowed clip kinds。新增操作 MUST调用正式 `TimelineData.AddTrack`、`TimelineData.AddClip` 或其等价的唯一 typed Mutation API，并进入同一个 `TimelineEditorSessionContext`、Undo 和 owner revision。

Slate 编辑对象、GUI整数ID和显示名称 MUST NOT成为 BTSMTL authoring identity 或第二业务数据。新增对象的正式 identity、ContractKind、Track/Clip relationship、typed properties 和外部资源引用 MUST由 Timeline owner/API生成并校验；新增完成后 MUST从 owner 重建 Slate projection。

Animation Clip MUST只能选择已存在的原生 AnimationClip；TreeClip MUST使用正式支持的 inline/shared ownership 和 Graph/Tree 来源，inline 创建 MUST走既有正式 authoring API；Camera、Motion、Cue 和其它 typed Clip MUST使用对应的 authoring binding。系统 MUST允许作者主动创建不含 Clip 的合法 Track；MUST不因取消或失败留下半成品，不创建替代 AnimationClip、默认 Tree 或 fallback contract。

#### Scenario: 新增合法Track

- **WHEN** 作者在 Timeline Surface 的 Add Track 菜单选择当前 owner 支持的 Track contract
- **THEN** adapter MUST通过正式 Timeline Mutation 创建 Track 并生成正式 identity
- **AND** 新 Track MUST进入 owner revision、Undo 和重建后的 Slate projection
- **AND** 完成必填字段的无 Clip Track MUST允许保存，后续可独立添加 Clip

#### Scenario: 新增不允许的Clip

- **WHEN** 作者在一个 Track 上打开 Add Clip 菜单
- **THEN** 菜单 MUST只显示该 Track contract 允许的 Clip kind
- **AND** 不允许的 Clip MUST无法通过 Slate 原生菜单创建或写入 authoring

#### Scenario: 新增Animation或TreeClip

- **WHEN** 作者选择一个已有 AnimationClip 或正式 Graph/Tree 作为新增内容来源
- **THEN** adapter MUST通过对应 typed binding 创建 Animation Segment 或 TreeClip
- **AND** proxy对象、GameObject层级和显示名称 MUST不进入正式 Timeline 数据

#### Scenario: 新增取消或校验失败

- **WHEN** 作者取消资源选择或新增输入未通过 contract validation
- **THEN** 系统 MUST丢弃临时菜单状态
- **AND** MUST不新增半成品 Track/Clip、无效 identity 或 Undo 项，MUST保留作者此前已创建的合法空 Track

#### Scenario: 创建位置与字段

- **WHEN** 作者在轨道空白处右键选择在第 N 帧添加 Clip
- **THEN** 创建表单 MUST携带准确 Track identity、插入帧、contract 和 required typed fields
- **AND** 资源选择、全部字段和 owner revision 校验成功后 MUST在同一正式事务创建对象，再投影为 Slate Clip
- **AND** 成功后 MUST选中新对象；取消或失败 MUST不修改已有内容

## ADDED Requirements

### Requirement: Timeline预览联动必须消费领域准备和实际采用报告

Timeline与Graph Shell的公共接入 MUST遵循replace-character-program-with-domain-runtimes领域装配合同。角色总Program、整包Projection、统一ProgramEpoch、旧Document/v7 MUST NOT成为作者编辑、场景准备或运行采用的前置；MUST NOT换名创建新的角色总包。Slate原UI、TimelineData、现有编辑Session/Undo及typed接线 MUST保留。

原rebuild-btsmtl-preview-with-scene-play协调器与正式运行模块 MUST继续拥有预览生命周期和领域采用。共同接口 MUST表达场景/context/目标/Session generation、领域内容与配置版本、就绪结果、实际采用版本或实例、失败阶段和原因。Timeline UI MUST仅消费对应报告与导航，不代写角色工厂、CameraBuilder或技能编译器。

#### Scenario: 各领域处理作者变化

- **WHEN** 技能、Pose、Camera、控制或网络配置发生变化并收到对应显式操作
- **THEN** 技能 MUST仅构建技能及真实依赖；Pose MUST经正式运行共用的原生Factory重建实例并重置历史；Camera MUST经正式绑定和Reset
- **AND** 控制与网络内容变化 MUST按Session规则准备，MUST保留网络Pipeline/Pass、Float32/Fixed和独立资源处理，不要求Character全量Build

#### Scenario: 就绪不等于实际采用

- **WHEN** UI收到准备成功、请求接受或作者配置hash变化
- **THEN** UI MUST分别显示配置、候选/准备、实际采用状态，只有正式领域报告能够确认生效
- **AND** 缺失实际版本 MUST显示未知或待报告，旧请求/目标/generation的晚到结果 MUST NOT覆盖新实例；MUST NOT由UI hash推断采用

#### Scenario: 原预览接口尚未迁移

- **WHEN** 原预览owner尚未提供某领域的就绪、版本或失败报告
- **THEN** 联动 MUST明确报告该领域待接入并保留本地编辑，MUST NOT等待已删除总包、代写其它owner实现或建立第二个协调器
- **AND** 纯Timeline MUST仍要求真实非Skill调用方、内容与播放身份，MUST NOT创建假角色或空技能替代

### Requirement: Timeline运动源与局部作者曲线必须分开

MotionCurve源、源区间、播放映射及MotionWarp源配置 MUST由unify-timeline-motion-curve-source的正式合同拥有；Timeline UI MUST消费其typed字段和打开源导航。RootMotionCurveAsset拥有的XYZ/Yaw MUST NOT注册成Timeline-local可写channel；Weight/Ease及Warp progress等真正局部曲线 MUST继续复用原Slate曲线编辑。

#### Scenario: 编辑一次运动使用

- **WHEN** 作者修改MotionCurveClip的源、区间、播放配置或局部Weight/Ease
- **THEN** UI MUST通过相应正式typed入口修改一次使用，MUST NOT修改共享源XYZ/Yaw、自动复制源或保留旧嵌入曲线双读
- **AND** 源时间裁切、末端保持和求值映射 MUST来自曲线owner唯一正式定义，不在Timeline预览另写采样公式

#### Scenario: 查看或修改共享运动源

- **WHEN** 作者从Timeline查看XYZ/Yaw或请求修改源
- **THEN** Timeline MAY只读显示源及其版本，修改 MUST导航真实源owner并由其负责Undo/保存/依赖失效
- **AND** 普通Timeline C#输出 MUST只保留明确源引用与使用配置，不复制外部源关键帧；局部曲线仍完整输出

### Requirement: Timeline必须直接复用现成Slate编辑功能

BTSMTL Timeline 打开、刷新、新增 Track/Clip、选择、编辑和关闭全链路 MUST NOT创建或依赖 Slate GameObject/Cutscene/Director/Group/Track/ActionClip 组件树。隐藏、不保存或退出销毁 MUST NOT代替此约束。__BTSMTL_SlateTimelineProjection__ 与 BuildProjection 组件树 MUST作为回退后的未完成残留删除，不得成为最终方案或 fallback。全部读写和 Undo MUST使用现有 BTSMTL TimelineData/Session。

Timeline MUST保留 Slate 已提供的绘制、交互、Curve/DopeSheet 和切线编辑，直接在原源码修改正式数据/命令、帧显示、必要布局和具体缺陷。函数签名和数据绑定 MAY更换，Actor/Director/组件扫描/运行采样等无关绑定 MUST删除。MUST不新建替代 Surface、通用 Editor Model、选择/拖动框架或曲线渲染器，再以相似外观称为复用 Slate。此前整套替换输入体系和强制拆分程序集的方案 MUST撤销。

#### Scenario: 正式合法操作不受Slate对象规则阻止

- **WHEN** TimelineData contract 允许作者新增 Track/Clip、选择或编辑正式内容
- **THEN** UI MUST根据 BTSMTL 正式规则处理，不因缺少 Slate Actor/Director、attachable type、Cutscene 有效性或其播放状态拒绝
- **AND** 操作及后续刷新/关闭 MUST不创建代用组件树，MUST继续复用原 Slate UI/交互算法

#### Scenario: 后续适配需要解除依赖

- **WHEN** 一项现成 Slate 功能依赖其原对象结构
- **THEN** 实施 MUST在对应原函数修改数据访问或必要参数，将正式数据和编辑命令接入
- **AND** 临时GameObject/Cutscene/Group/Track/ActionClip组件承载及专属生命周期 MUST删除，MUST不自动转为整体 UI/模型重写

#### Scenario: 原曲线工具改接正式通道

- **WHEN** Timeline显示或编辑已注册的Curve channel
- **THEN** 原DoParamsInfoGUI、DoClipCurves、AnimatableParameterEditor、CurveRenderer与DopeSheetRenderer MUST复用原控件/关键帧/切线实现，数据来自正式descriptor及隔离曲线草稿
- **AND** MUST NOT为每个channel新建proxy Animatable浮点字段、反射场景属性或要求IKeyable运行root；前后key跳转 MUST只改编辑游标
- **AND** 静态读取草稿AnimationCurve值 MAY用于Value显示，MUST NOT调用Cutscene采样或写Actor属性

#### Scenario: 原手势消费正式重叠规则

- **WHEN** 作者拖动、裁剪或调整Blend，或者原UI仅执行Repaint
- **THEN** 原手势与图形 MUST保留，能力和合法放置 MUST来自正式Capabilities与Reject/Parallel/Blend合同，而非proxy CLR类型或组件反射
- **AND** Repaint MUST NOT将相邻重叠写入SelfEase；派生OtherEase MUST由原正式混合规则处理

#### Scenario: 无Actor的原属性控件

- **WHEN** 作者选中正式Track或Clip并修改属性
- **THEN** 原Inspector的IN/OUT、Blend和参数控件 MUST接真实serialized owner、正式选择和typed配置，MUST NOT依赖ActionClip组件target、base proxy字段序列化或CutsceneInspector重采样
- **AND** MUST NOT创建Dummy Actor、代用Unity对象或清空全局Selection隐藏问题；仅跳过Actor报错 MUST NOT被记作解耦完成
- **AND** 删除Timeline内部右侧自制面板 MUST NOT删除已有Clip的正式字段编辑能力

#### Scenario: 原有功能恢复

- **WHEN** 编辑器重做代码回退完成
- **THEN** 实施记录 MUST区分原有 Slate 功能恢复、正式数据适配和剩余问题
- **AND** 绑定替换 MUST同时达到原UI功能保留、正式数据写入和无代用组件树，MUST不抹掉同期正确的业务修改

### Requirement: Timeline作者输入必须遵守两个显式C#操作边界

Timeline MUST消费公共 C# authoring r2 的 export_code 与 generate_assets，不新增 Timeline MCP、源码 Undo、自动同步、导出界面、中央 Validator 或新整包事务。人工拖动/修改/保存/撤销 MUST只走现有正式 TimelineData 编辑与 Undo；旧 Agent Document/五工具 MUST不再作为作者或预览依赖。两个公共工具的实现和旧协议删除由 C# authoring owner 负责，本任务 MUST保留正确的 Slate UI、Session、owner、时钟、Camera Track 和预览 adoption。

#### Scenario: 人工编辑与源码编译

- **WHEN** 作者拖动或保存 Timeline，或导出的 C# 经正常编译
- **THEN** 人工编辑 MUST不自动 export_code，源码编译 MUST不自动 generate_assets
- **AND** 只有显式公共操作才更新指定源码或资产；两操作 MUST NOT自动触发领域准备或Play，Character全量Build MUST NOT作为前置

#### Scenario: 显式重新生成

- **WHEN** 作者明确以当前已编译入口生成声明范围
- **THEN** generate_assets MUST重建并保存该范围、恢复根挂接，MUST不自动合并未导出人工修改
- **AND** 当前编译结果不匹配时 MUST由公共入口拒绝，不调用旧程序集或增加源码同步流程

### Requirement: Timeline UI配置必须直接使用共同强类型入口

Timeline 任务 MUST维护现有 BTSMTL-to-Slate 数据适配文件，不要求强制改名或新建纯内存 adapter；C# authoring 任务 MUST提供 TimelineAuthoringClipBinding.cs 的 JSON 退役与正式 typed 读取/配置合同。UI MUST移除 BuildClipProperties -> Export -> JObject -> Apply 中转，直接调用同一配置入口，保留原字段、值、引用、校验、取消/失败和编辑行为。MUST不建立 UI DTO/第二业务规则或并行修改公共文件。

#### Scenario: 强类型合同接通

- **WHEN** 公共任务交付正式字段和接口
- **THEN** projection MUST把当前 Clip 值与现有创建输入交给共同 typed 配置入口
- **AND** AddTrack/AddClip/AddSection、Curve/引用约束、Session/Undo MUST继续来自既有 Timeline 规则

#### Scenario: 合同尚未交付

- **WHEN** projection 所需强类型入口尚未可用
- **THEN** 对应接线 MUST保持未完成，无关 UI 工作保持原范围
- **AND** MUST不复制实现、添加兼容 JSON 路径或把规划修改称为代码完成

### Requirement: Timeline完整代码输出必须保留正式内容与生成边界

公共输出 MUST直接读取 TimelineData 的 Track、Clip、Section、外部 binding、TreeClip、全部正式配置、完整曲线及正式布局，保持业务顺序和 identity；MUST不读取 Slate 草稿或显示降采样数据。生成 MUST沿 AddTrack/AddClip/AddSection 和共同 typed 配置/原 owner API，只有本次生成范围内物理对象可替换。

#### Scenario: 完整曲线与引用重建

- **WHEN** 公共操作导出并生成包含 Curve、TreeClip 和共享引用的 Timeline
- **THEN** key/time/tangent/weight/WeightedMode/wrap、业务顺序、identity 和全部正式配置 MUST保持
- **AND** 内部对象 MUST使用本次创建引用，范围外共享图和原始资源 MUST为精确外部输入，不靠旧生成子资产 GUID 解析内部引用
- **AND** 生成根 MUST经正式 API 挂回明确 owner 并保存，不扫描全项目猜消费者

#### Scenario: 临时窗口状态

- **WHEN** 输出正式 Timeline 布局
- **THEN** MUST读取已有正式 layout owner
- **AND** MUST不将选择、滚动、运行 Session、编辑游标或 Slate proxy 私有状态作为生成内容，不新增布局镜像

### Requirement: Timeline必须与共享预览区完成跨窗口联动

Timeline 与 SkillGraph/Graph Shell MUST按同一联动计划交付导航、运行 binding、作者/运行版本状态及生命周期。Timeline MUST提供“预览”导航与精确关联说明；共享预览区 MUST支持从实际调用打开对应 Timeline。角色结果 MUST来自正式 Scene Play，Timeline 编辑游标 MUST不执行角色或 Slate 内核。

#### Scenario: 技能产生多个Timeline调用

- **WHEN** 作者从正在预览的技能打开 Timeline
- **THEN** 系统 MUST根据真实 ActionInstance、完整调用路径、generation 和内容版本关联 Timeline
- **AND** 多个合法调用 MUST明确选择，不按列表首项猜测；Tree-only 技能 MUST正常显示无 Timeline

#### Scenario: 编辑后返回预览

- **WHEN** 作者修改 Timeline 并返回共享预览区
- **THEN** 作者数据 MUST保持，预览区 MUST显示正式待采用/已采用/下一次激活/失败状态
- **AND** 切页、折叠和关闭 Timeline MUST不停止或更换 Session；runtime overlay 和编辑帧 MUST分别保存

#### Scenario: 独立内容预览

- **WHEN** 作者从 shared Timeline 请求查看预览
- **THEN** 系统 MUST使用其精确正式非 Skill 调用方或已明确关联的技能来源
- **AND** 无合法绑定时 MUST保留编辑并解释缺项，不伪造 Actor、Skill 或 Timeline 播放器

### Requirement: Timeline布局必须统一计算并适应窗口尺寸

嵌入Surface MUST以原Slate单一布局结果提供背景、分隔线、控件、裁剪和命中范围。窗口 MUST包含紧凑文档信息、原编辑工具、左侧搜索/轨道与时间尺/Clip/Curve，MUST NOT新增右侧自制Inspector或SplitView。选中属性 MUST复用原控件接Unity已有Inspector。左右内容 MUST共用原行高、展开和垂直滚动；隐藏控件 MUST NOT留空白，文档名称只显示一次。

#### Scenario: 窄窗口与曲线展开

- **WHEN** 窗口缩到 600×360 逻辑像素或作者展开曲线
- **THEN** 左轨道和中间内容 MUST保持对齐，不因新增内部Inspector挤压时间轴，次要工具 MUST折叠或省略文字
- **AND** 主要按钮、数值输入、标尺 MUST不重叠，命中位置 MUST与显示一致

#### Scenario: 缩放条与游标

- **WHEN** 作者拖动顶部缩放范围条
- **THEN** 操作 MUST只改变视窗，游标的命中区域 MUST不占用缩放条
- **AND** 隐藏工具栏 MUST不留下额外高度，背景 MUST使用最新布局边界

#### Scenario: 轨道行局部坐标

- **WHEN** 原Track参数GUI被调用或作者滚动/展开曲线
- **THEN** 每行内容 MUST在对应轨道局部坐标和裁剪范围内绘制，名称/图标/曲线按钮 MUST NOT叠在列表原点或搜索框
- **AND** 行背景、局部内容与鼠标事件 MUST采用一致且不重复施加的坐标转换，MUST复用同一原绘制主体

#### Scenario: Clip标题与真实DopeSheet

- **WHEN** 作者查看正式Clip及其底部关键帧区域
- **THEN** 原wrapper MUST只绘制一次Clip标题，binding MUST NOT重复绘制Info，真实状态 MUST与标题分开
- **AND** 有局部曲线的Clip MUST接原DopeSheet关键帧与编辑命令，MUST NOT以Info文字占位并提前返回
- **AND** 无局部曲线的Clip MUST不预留假DopeSheet条带或扣减对应拖动区域，源运动曲线 MUST NOT被恢复为局部可写通道

### Requirement: Timeline必须使用正式帧率统一编辑时间

Surface MUST消费正式 Timeline Session 的 FrameRate，统一像素、整数作者帧和 Slate 秒的转换；MUST不以 Slate 全局 FPS 或秒吸附偏好决定 BTSMTL 显示与保存。标尺、帧输入、Clip/Section 边界与关键帧编辑 MUST以作者帧为主。作者帧 MUST不自动解释为 Runtime Logic Tick。现有资产未编辑的数据 MUST不被整体量化。

#### Scenario: 编辑一帧

- **WHEN** 作者把 Clip 起点从第 12 帧移到第 13 帧并提交
- **THEN** 草稿、属性和正式 StartFrame MUST一致为 13，一次 Undo MUST恢复为 12
- **AND** 上一帧/下一帧 MUST严格移动 1 帧，不跳到相邻关键帧

#### Scenario: Curve时间换算

- **WHEN** 作者编辑 Timeline-local 曲线 key
- **THEN** adapter MUST按正式descriptor处理Timeline-local曲线的domain与切线换算；源运动XYZ/Yaw MUST消费曲线owner的源区间/映射而非旧嵌入CurveEndFrame读法，MUST NOT在UI重造采样公式
- **AND** 未编辑 key 的时间、值、tangent、weight、WeightedMode 和 wrap mode MUST保持

#### Scenario: 显示全部内容

- **WHEN** 作者打开短 Timeline 或选择显示全部
- **THEN** 内容终点 MUST来自真实帧范围，视窗 MAY保留像素边距但 MUST不改变文档
- **AND** 空文档显示视窗 MUST不创建一秒内容，终点线 MUST不提供无正式字段对应的长度写入

### Requirement: Timeline刷新必须保留有效作者状态

编辑视图状态 MUST按正式identity保存选择、曲线通道、展开、当前编辑帧、缩放和滚动，不新增Timeline内部Inspector宽度状态。提交、Undo/Redo和外部更新 MUST恢复有效状态，不自动改选首个Clip。选择、游标、缩放和搜索 MUST NOT产生authoring Undo或无条件重建。原Inspector控件 MUST显示正式typed字段，不编辑proxy私有参数。窗口关闭 MUST释放所属曲线cache与可解除订阅，不清空其它窗口状态。

#### Scenario: 修改后继续编辑

- **WHEN** 作者在已缩放并展开曲线的视图提交字段或曲线修改
- **THEN** 选择、曲线展开和视野 MUST保持，正式属性 MUST反映提交结果
- **AND** 原对象被删除时 MUST清空对应选择，不静默选中其它内容

#### Scenario: Inspector与轨道曲线共享选择

- **WHEN** 作者选中MotionCurveClip或提交/Undo后刷新该选择
- **THEN** Inspector、原Surface与Track.SelectedClip MUST按同一正式owner/AuthoringId解析当前Clip，保留有效通道选择，不使用旧引用或额外选择真相
- **AND** Weight/Ease In/Ease Out MUST可在Timeline原曲线区进入CurveEditor/DopeSheet编辑，不以Inspector中的普通曲线字段替代
- **AND** 未选择、确无局部曲线与绑定失败 MUST分别说明，MUST NOT统一显示No Clip Selected掩盖缺项

#### Scenario: GUI事件消费符合输入类型

- **WHEN** 曲线或Clip界面处理Layout/Repaint与鼠标/键盘事件
- **THEN** Event.Use MUST仅用于已处理的有效输入事件，MUST NOT消费Layout/Repaint或屏蔽相关警告
- **AND** 事件缺陷与曲线不显示的关系 MUST依据实际调用链确认，不将消除警告当作曲线编辑已完成

#### Scenario: 不支持的命令或过期草稿

- **WHEN** 命令没有正式映射或 source revision 已变化
- **THEN** adapter MUST明确报告原因并丢弃无效草稿，MUST不静默吞修改或覆盖最新 owner
- **AND** 合法未提交文本 MUST不被普通重绘清空

### Requirement: 编辑控件不能冒充真实角色预览

Timeline MUST保留编辑游标、整数帧输入和逐帧操作；编辑游标、真实运行标记和 Capture 历史位置 MUST分别保存。默认交付 MUST不包含未确认的本地自动播放游标或 Timeline 内 Scene Play 快捷控制。导航 MAY返回精确来源 Graph Shell，但 MUST不启动/重建 Session。嵌入按钮、快捷键、EditorUpdate、初始化/释放、保存和 delayCall MUST不调用 Slate Play/Sample/ReSample/Stop 执行预览，MUST清理 AutoKey 与临时作者播放器。

#### Scenario: 没有运行绑定

- **WHEN** 作者独立打开 shared Timeline
- **THEN** 轨道、Clip、曲线和编辑帧 MUST完整可编辑
- **AND** MUST不创建本地播放器或猜测角色目标，不把游标移动显示成角色已运行

#### Scenario: 运行时继续编辑

- **WHEN** 作者在 Scene Play 中编辑正式 Timeline
- **THEN** 作者数据 MUST经同一 Mutation/Undo 修改，运行标记 MUST只读消费真实绑定
- **AND** 新版本是否采用 MUST由原预览协调器和对应领域的实际报告决定，UI MUST NOT推断生效或直接写运行状态；Pose显式重建 MUST重置历史，控制/网络变化 MUST按Session准备规则处理

### Requirement: 临时投影必须正确释放且没有无关编辑入口

现有 Slate 窗口适配 MUST正确释放所属选择、回调、GUI capture和临时资源，MUST不泄漏引用或删除用户正式资产。嵌入路径 MUST移除 Actor/Director/Render 等无业务入口及未映射原生命令；MUST保留项目正式 Camera Track。曲线密集显示优化 MUST不改写正式 key，无曲线内容 MUST不创建空参数面板。

#### Scenario: 关闭与重新打开

- **WHEN** 作者保存后关闭并重新打开 Timeline
- **THEN** 正式 Track/Clip identity、资源和帧范围 MUST保持，旧临时对象 MUST释放
- **AND** MUST无新增 GUI、序列化或生命周期异常，异常处理 MUST基于完整堆栈而不是隐藏 Console

### Requirement: 旧UI Toolkit仿制Timeline路径必须删除

完成本 change 后，正式 Timeline 编辑入口 MUST不再依赖上一轮新增的 UI Toolkit Slate仿制 UXML/USS、独立 viewport、独立 zoom/pan、独立 Clip hit-test 或独立 rendering path。项目 MUST只保留 Slate `CutsceneEditor` 作为 Timeline 编辑表面和 BTSMTL Mutation 作为正式写入链。

#### Scenario: 检查Timeline编辑入口

- **WHEN** 工程编译并打开正式 Timeline入口
- **THEN** 调用链 MUST能追溯到唯一 `TimelineEditorWindow` 中的 Slate 原有编辑 Surface
- **AND** MUST不存在并行的旧 UI Toolkit Timeline窗口、仿 Slate皮肤或兼容开关
