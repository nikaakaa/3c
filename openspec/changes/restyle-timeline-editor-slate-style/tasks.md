2026-09-14 最新作者UI状态与具体修正以[editor-wiring-audit.md](editor-wiring-audit.md)为准。本轮只审阅和补文档；已正确代码不回退，与源码不符的整体勾选重开，源码已改不等于真实窗口已通过。

2026-09-13 源码对账：ce21aec8f/afcb90056已恢复原Slate UI及正式新增/游标，仍创建隐藏组件代理，DirectProjection已删除。第11节按[源码解耦决策](slate-source-decoupling.md)重新展开为未完成实现项。第1–10节保留此前功能记录，但对已由当前源码证明不成立的勾选予以纠正；未重新核对的历史勾选不构成本次端到端声明。正确业务实现不回退。只在主线实施，不向旧worktree双写；不新增测试、手动验证、编译或校验任务。

2026-09-14 实现对账：提交 `93b232070` 已删除 `BuildProjection`、隐藏宿主和 BTSMTL 代理组件树；提交 `66e2ebc92` 将正式 binding 接入现有 Slate `ShowGroupsAndTracksList` / `ShowTimeLines` 入口。第11.10 的“无组件入口”已达到源码边界，但真实打开、刷新、曲线编辑和保存仍未端到端验收；其余第11节按实际缺口继续保留未完成。

2026-09-13 领域协调仅PLAN：原UI/数据/Undo实现要求不变；预览联动改用独立技能、原生Pose重建、Camera绑定/Reset及Session领域准备，废除总包Build/ProgramEpoch前置。Motion源XYZ/Yaw归外部源，UI只接曲线owner的typed字段与导航。本次只更新清单含义，不向实现任务下发或扩大授权。

## 1. 已有接入基础

01c68fb21增量对账：正式binding与代理差异事务已部分接入，组件树未删除；当前未提交CutsceneEditor.cs中两个ShowEmbedded替代入口已搜不到，但仍依赖Cutscene/代理identity，不能标为恢复完成。第11节继续表示完整收口，不因局部提交勾选整项；复用已有binding，不新建第三条路径。旧快照里的完成项只用于保留正确业务，不驱动整体回退。

历史上0aa52f209撤销过一次自制UI；最新恢复基线是ce21aec8f/afcb90056。两次回退均不构成组件解耦完成；第11节不得沿用已回退实现的完成勾选。

- [x] 1.1 已有 CutsceneEditorSurface 的 InitializeEmbedded/DrawEmbeddedGUI 和 transaction callback；后续布局/播放清理分别由布局和预览边界章节承担
- [x] 1.2 已有 TimelineData 正式 owner 与 Editor-only Slate projection 的身份映射基础，不把临时对象作为保存资产
- [x] 1.3 正式打开入口已切换到 Slate Surface，旧 UI Toolkit 仿 Slate 时间轴不再作为正式入口
- [x] 1.4 不恢复旧 UI Toolkit viewport/interaction/rendering 并行实现；帧和 GUI 改造在 Slate Surface 内完成
- [x] 1.6 已有正式 Track/Clip/Section identity 到临时对象映射；刷新恢复由第 6 节收口
- [x] 1.7 Skill/Shared Timeline 正式入口已有唯一 TimelineEditorWindow 承载 Slate
- [x] 1.8 Slate 类型/API 无法加载时已有明确 unavailable 入口，不回退到另一套 UI
- [x] 1.9 已抽取不创建第二个 EditorWindow 的 Surface
- [x] 1.10 已移除 CreateInstance<CutsceneEditor> 伪造窗口路径

## 2. 投影和数据编辑收口

- [ ] 2.1 保留已正确Clip/Section/局部Curve显示与domain映射；按曲线owner新合同接Motion源/区间/映射与导航，源XYZ/Yaw退出Timeline-local写入，Weight/Ease继续编辑，不新增采样公式
- [x] 2.2 将正式 selection、属性、TreeClip ownership/下钻和 AnimationClip 资源导航接入同一 adapter，不仅保存在临时名称中
- [ ] 2.3 完成有效手势begin/commit/cancel与source revision处理；当前恢复基线仍对普通MouseDown抓快照、MouseUp排队重建，随11.8收口，选择/游标/缩放不产生作者事务
- [ ] 2.4 保留已接通的正式ApplyModify失败回滚和validation；完成局部差异提交、准确过期反馈与刷新，不再声称回滚尚未加入，剩余见审阅A07及11.19
- [x] 2.5 收口取消、关闭、Undo/Redo、外部 owner 修改和过期草稿；无效草稿不覆盖正式数据
- [ ] 2.6 Clip、Curve、属性和菜单使用同一正式Undo；当前原DopeSheet/Inspector仍有组件Undo入口，随11.6–11.8改接，不以已有Session提交推定全部隔离
- [ ] 2.7 删除、复制、排序、跨轨道移动直接接正式contract/引用规则，复制新ID、排序保留ID；当前仍有先改组件再diff路径，随11.5收口

## 3. 正式新增

- [x] 3.1 打开请求显式携带 owner contract composition，Add Track 候选来自正式 catalog/type metadata，字段 UI、校验和写入现在都读取 `TimelineAuthoringTrackFieldAttribute`/field sink，不再硬编码 AnimationTrack 字段
- [x] 3.2 接通各 Track 允许的 Add Clip 输入：Animation 资源、TreeClip ownership/来源、Motion/Camera/Cue/Scene typed binding；基础 Slate UI 只传递资源和 binding identity，Tree contract 由正式 Track 工厂解析来源
- [x] 3.3 在唯一 Session 事务内创建正式对象；合法空 Track 可保存，取消 picker/输入非法/owner 过期不留半成品或 Undo；Add Track/Add Clip 的 owner 过期、失效 Track 和缺失 contract 现在通过正式 AuthoringIssue 通知
- [x] 3.4 用 Slate Surface 的“＋轨道”和轨道右键“在第 N 帧添加 Clip”替换原生无正式身份创建入口，成功后恢复新对象选择

## 4. 帧几何

- [ ] 4.1 保留现有正式帧换算，将原Inspector中的Prefs.frameRate/snapInterval及曲线全局吸附读数改接同一Session上下文，随11.6–11.7收口
- [x] 4.2 标尺、游标输入、逐帧、Clip/Section 拖动与裁剪使用整数帧；区分一帧移动与关键帧跳转
- [x] 4.3 按 Curve descriptor domain 换算 key time/tangent，保留未编辑 key、weight、WeightedMode 和 wrap，不全量量化资产
- [x] 4.4 内容终点使用真实 MaxFrame，移除最少一秒和额外一秒；显示全部只改变视窗，终点线不提供无正式数据对应的编辑

## 5. GUI 布局和生命周期

- [x] 5.1 合并文档名/ownership/来源，工具栏统一高度；清掉重复标题和隐藏控件的空白占位
- [x] 5.2 唯一 Timeline 窗口只承载 Slate Surface；原 Slate Surface 自己计算工具栏、搜索、缩放、标尺、左右轨道和命中区域，不新增 UI Toolkit Timeline 壳
- [x] 5.3 左右共同行高度与垂直滚动、曲线展开同步均由原 Slate Surface 保留；不新增右侧自制 Inspector 或第二套属性面板
- [x] 5.4 删除临时 Auto、“作者预览”、无关 Actor/Director/Render 和未映射菜单；曲线入口不再拼入 Track 名
- [x] 5.5 根据完整堆栈修复重复序列化字段、GUI 和 proxy 生命周期异常，销毁时释放临时宿主/选择/回调，不隐藏错误代替处理

## 6. 刷新与属性

- [ ] 6.1 选中正式Track/Clip/key驱动原Slate控件，普通字段接Unity已有Inspector的正式owner；不恢复Timeline右侧自制面板，不再选代理Cutscene，随11.7收口
  - [ ] 6.2 按稳定 identity 保存并恢复选择、展开、当前帧、横向视野和纵向滚动；删除对象不自动改选首个 Clip，不保存自制 Inspector 宽度。当前 view state 已保存 Track/Clip/Section、展开轨道、Track 高度、inspected parameter、当前帧、视野和滚动；真实关闭/重载及曲线缓存验收仍未完成，见审阅A05/A10。
- [x] 6.3 Add Track/Add Clip 弹窗在正式提交失败或 owner 过期时保留当前输入并报告原因；DopeSheet 仅按像素密度减少显示 key，不修改正式曲线

## 7. 预览边界

- [x] 7.1 移除误接的嵌入 Slate Play/Sample/ReSample/Stop 和私有时钟，保留静态编辑游标、逐帧与被动运行标记
- [ ] 7.2 删除BTSMTL隐藏组件树与默认Director/Actor前提，清理编辑更新、保存、Inspector、delayCall和释放中的Slate运行调用，随11.9–11.10收口；原正式预览保留
- [x] 7.3 Timeline 通过 `RuntimeDebugSession` 的正式 Timeline playback summary 接入场景/技能观察，Preview 返回 Graph Shell；运行事实只读、作者仍可编辑，多调用不猜选，Graph Shell 继续拥有运行控制
- [x] 7.4 作者游标、Runtime overlay 和 History overlay 使用三个独立时间状态；Graph Shell Segment 选择进入历史观察，Timeline 不提供未经批准的本地 Play

## 8. 文档与交付

联合预览实施的 P1–P5、原任务映射与行为标准见 [preview-integration-plan.md](preview-integration-plan.md)。以下联动任务负责跨窗口集成；场景预览原 tasks 继续记录其 owner 内部实现，不复制一套协调器待办。

- [x] 8.1 已形成 proposal/design/delta 的编辑和预览边界说明；r2 追加两个显式作者操作、文件分工和协议退役范围
- [x] 8.2 implementation.md 已交付模块输入/输出、实际代码链、删除范围及未完成项；旧验证记录保留为历史，不作为本轮任务
- [x] 8.3 Scene valueCurve 已接入正式曲线 descriptor 和领域规则，保留实现；旧 Skill Document 对账不作为新依赖，JSON 退役接线另列第10节

## 9. 与预览窗口联合实施

- [x] 9.1 P1：将共享预览 presenter 接入实际 SkillGraph/Graph Shell 宿主，按场景控制、试验/采用、观察、折叠历史分组，消除旧树窗口专属接入和重复工具条
- [ ] 9.2 P2：接入场景资产/context、角色或真实非Skill调用方、内容及播放身份，消费各领域就绪/失败报告；区分场景开始与技能请求，不要求Character全量Build或整包Projection就绪
- [x] 9.3 P3：Timeline 打开请求携带作者 locator 和可选准确 runtime binding，连接运行 overlay、Follow/Pin、双向导航及多调用选择，编辑帧与运行/历史位置隔离
- [ ] 9.4 P4：Timeline修改/Undo后消费原预览owner的领域就绪、配置版本、实际采用版本/实例和失败报告；技能启动版本固定，Pose重建重置历史，Camera正式绑定/Reset，控制/网络按Session准备；删除统一ProgramEpoch采用前提，不用UI hash推断生效
- [ ] 9.5 P5：历史面板区分诊断采集与输入录制，按选定 Tick/区间和正式 capability 校验恢复/回放，刷新不覆盖输入，命令接受与完成分开显示
- [ ] 9.6 跨宿主布局、切页、关闭、重载和绑定释放统一收口；同步预览原任务/审计的主线证据，记录缺失能力，不以按钮存在代替联合交付

## 10. C# authoring r2 强类型接线

- [x] 10.1 在 C# authoring 提供 TimelineAuthoringClipBinding 正式强类型配置合同后，将 projection 现有创建输入及当前 Clip 值接入该入口，保持原字段/默认值/引用/合法范围与失败反馈
- [x] 10.2 删除 BtsmtlSlateTimelineProjection.cs 的 BuildClipProperties、Export/JObject/JSON Apply 中转及专用依赖；不建立 UI 配置模型，不修改公共任务拥有的 TimelineAuthoringClipBinding.cs
- [x] 10.3 如公共输出接入需要补充读取或根挂接能力，仅在既有 Timeline 领域 API 暴露正式内容/布局/owner 能力；公共遍历、C#输出、生成及两个MCP由 C# authoring 负责，不在 Slate 复制
- [x] 10.4 从本任务剩余 UI/导航消费者移除旧 Agent 文件协议调用与无用依赖，保留 AddTrack/AddClip/AddSection、正式规则/Undo/Session和已有刷新；人工编辑不写源码，生成不自动 Build/Play

依赖仅阻止第10节对应接线，无关 UI 继续原范围。领域规则是业务实现，不是中央 Agent Validator；不新增验证任务。按中文小步提交，命令执行遵守 AGENTS。

原第11节的替代编辑器方案已撤销，不是将其标为完成。回退由实现窗口按实际 diff 执行，保护已正确的 typed 数据、布局和预览修改。以下是按用户最新指令重写的原源码接线任务，不沿用被回退实现的勾选。

## 11. 直接修改Slate原源码的数据绑定

2026-09-14 用户追加授权：修正最新截图中的轨道局部坐标、重复Clip标题与DopeSheet占位，详见源码决策第13节及下方11.12–11.14。沿现有实现goal，不新增验证tasks、不回退已正确改动。

实现门槛：打开、刷新、新增、选择、编辑、关闭均不创建或依赖 Slate 组件树；原生 Cutscene/Actor/Director 规则不能阻止正式 TimelineData 的合法操作。__BTSMTL_SlateTimelineProjection__ / BuildProjection 残留不作为最终方案或 fallback。本段是实现约束，不新增验证任务。

- [ ] 11.1 D1/D2：在现有Slate Editor模块收窄原编辑输入，原生Cutscene入口与BTSMTL入口共用一套原函数；BTSMTL输入不继承IDirector/IKeyable运行接口，不增加空运行实现或替代绘制分支。提交 `6d56e7efd`、`6445b7490`、`3736dc802`、`5d8548a8e`、`85203d7b3`、`24fa2d629`、`bfa830f34` 已合并 Track/Group 行、左侧列表、右侧 Track 行和时间轴 Group 外层循环，剩余为真实窗口与来源谓词验收。
- [ ] 11.2 D1/D3：将CutsceneTrack原OnTrackInfoGUI、DoDefaultInfoGUI、DoParamsInfoGUI、OnTrackTimelineGUI、DoClipCurves及原展开/高度状态参数化，必要时搬入现有Editor模块；旧位置不保留第二份函数主体。`ec59bd5a1` 已接回 formal Track 的原生纵向调高手势，native/formal 参数行 header、命中逻辑和值域 tooltip 现在共用 Slate 函数；`09ce7262d` 已让 formal 参数区和曲线区共用原生的 inspected parameter 高度规则，`8a9fb1f6e` 按原 `TrackEditorGUI` 的 proposedHeight 和 Mini 控件公式修正 formal 展开高度；参数 provider 的来源字段和真实窗口高度验收仍需继续收口。
- [x] 11.3 D2：把现有Projection中正确的ID映射、typed新增、曲线换算和提交迁移为BtsmtlSlateTimelineBinding；只引用正式对象和必要手势草稿，不恢复EditorModel/DirectProjection两条路径。真实窗口验收仍未完成。
- [ ] 11.4 D3/D4：原ShowListGroups/ShowListTracks/ShowTimeLines及ActionClipWrapper就地改接该输入；保留Rect、样式、GUI.Window/DragWindow、框选和边缘交互，能力与重叠规则来自正式Capabilities/contract，分离SelfEase与派生OtherEase。`e3fd0b691` 已合并Clip主体，`32aee7b96`/`1bdc420f8`/`a3c4ca3ce`/`001a6ce99` 已合并Track/Group外壳和Group/Track输入状态机，`6d56e7efd`、`6445b7490`、`3736dc802`、`5d8548a8e`、`85203d7b3`、`24fa2d629`、`bfa830f34` 已合并两侧 Track/Group 左侧列表和右侧 Group/Track 循环，剩余为参数提供者、来源命令和真实窗口验收。
- [x] 11.5 D3/D7：原Track/Clip/Section菜单和排序释放直接提交正式命令；Section不依赖directorGroup，显示边界不保存成Section，删除无正式合同的Actor/循环/任意组件创建命令。真实窗口验收仍未完成。
- [x] 11.6 D5：原CurveRenderer/DopeSheetRenderer/参数工具只接正式Timeline-local曲线、编辑时间和事务通知，保留原key/切线/缩放算法；删除proxy假字段，保留局部Weight/Ease；Motion源XYZ/Yaw以同一参数行/DopeSheet/CurveEditor作只读Reference显示，只有source字段可配置并沿正式入口导航。提交 `081ba862b` 已接通Reference参数和只读原Renderer。
- [x] 11.7 D6：原ActionClipInspector通用控件参数化并通过真实serialized owner接入Unity已有Inspector；普通字段走Read/Configure，选择不写代理context，不增加假Actor、假Unity Object或Timeline右侧自制面板。提交 `a25a05d10` 已将TimelineAsset Inspector改为typed字段和正式Configure；真实窗口验收仍未完成。
- [ ] 11.8 D7：在现有Session/TimelineData mutation链收口手势、字段、菜单的一次提交、完整业务校验、source revision反馈与该次owner范围失败恢复；组件Undo退出BTSMTL编辑，选择/滚动不产生事务。真实窗口验收仍未完成。原ApplyModify回滚、差异提交和Track正式命令已保留；提交失败现在在 Slate 回调内完成重建和正式通知，不再把已回滚异常重新抛穿 IMGUI；选择合同现在携带当前 Timeline fingerprint revision；组合手势和owner范围失败恢复仍见审阅A07/A08。
- [x] 11.9 D8：原标尺/游标/步进/局部曲线吸附使用正式帧上下文，编辑不依赖Cutscene/Actor；保留原Runtime/History与Scene Play归属，实际采用读取领域报告，删除BTSMTL的Slate采样副作用，不擅自新增Play或实现领域工厂。真实窗口验收仍未完成。
- [x] 11.10 D2/D8：正式入口切到无组件binding并删除BuildProjection/CreateChild/隐藏宿主、BtsmtlSlateGroup/Track/ActionClip及组件字典/扫描/销毁；删除无消费者的EditorModel与过时接口/meta，真实Slate组件与正式Actor/Camera资源不在删除范围
- [x] 11.11 D5/D7：原选择、曲线缓存与Undo订阅按窗口/正式ID恢复和释放，改为可解除回调，关闭丢弃未提交草稿但不改已保存数据；原native/BTSMTL共用Renderer，不互相清空状态。提交 `bf07cf6e6` 已将Formal缓存清理收窄到Surface scope，提交 `8b7a0e2a3` 已恢复全部展开曲线轨道；真实关闭和未提交手势仍待主 Unity Editor 验收。

- [x] 11.12 恢复原轨道行内GUI.BeginGroup/EndGroup和局部坐标裁剪，名称/图标/曲线按钮/参数只在本行绘制，背景与滚动使用原列表坐标；局部坐标修复保留，不用固定偏移遮盖问题；两份列表主体合并仍归11.1。真实窗口验收仍未完成。
- [x] 11.13 Clip标题统一由原ActionClipWrapper绘制，保留已删除binding重复Label的正确改动；真实运行状态不冒充或覆盖标题，不重复显示Info。真实窗口验收仍未完成。
- [x] 11.14 将正式局部曲线接同一原DopeSheet的真实key操作与正式事务，删除FormalClip画Info后return的占位；无曲线时同时消除假底栏高度和对应拖动区域扣减，不恢复源XYZ/Yaw的局部写入。提交 `48a1d2a72` 已让FormalClip底部复用原DopeSheet并走正式事务；真实窗口验收仍未完成。

2026-09-14 后续截图修正见源码决策第14节；保留已正确修复，以下仅为新增实现项，不是验证任务。

- [x] 11.15 修正Clip选择、Track.SelectedClip、Inspector路径与Session之间的实际失配，刷新/Undo后按同一owner及AuthoringId恢复对象/通道，真实删除时同步清空，不新增选择真相或自动改选首项。提交 `6fad5d20a` 收口Formal通道选择身份，`a25a05d10` 接通同一正式选中对象，`a0842565f` 让嵌入 Surface 每帧重置 `formalSelectionHandled`，避免一次选中污染后续空白选择清理；`3006f5c89` 让选择变化主动请求原Slate Surface重绘，避免选中框停留在旧对象；真实窗口验收仍未完成。
- [x] 11.16 接通选中MotionCurveClip的Weight/Ease局部参数到原Timeline曲线区，修正descriptor收集/过滤/参数生成的实际缺项；区分未选择、无局部曲线与绑定失败，不以Inspector曲线框代替原CurveEditor/DopeSheet。提交 `081ba862b` 增加源Reference项并保留Weight/Ease原曲线链；真实窗口验收仍未完成。
- [x] 11.17 按实际调用栈修正Event.Use对Layout/Repaint的错误消费，只处理明确输入事件，保留现有鼠标交互，不屏蔽警告或将其未经证明归为曲线缺失唯一原因。原参数区、曲线区和锁定轨道现在只消费左键MouseDown/Drag/Up或ContextClick，不再用Event.isMouse吞掉鼠标移动。

- [x] 11.18 MotionCurveClip保持与其它Clip相同的参数行、DopeSheet和CurveEditor布局；Position X/Y/Z/Yaw作为带`[Ref]`标记的只读源曲线显示，source字段通过正式typed Inspector配置，Timeline不写源资产。提交 `081ba862b` 改为同一Renderer引用项，提交 `a25a05d10` 接通source typed字段，提交 `dfa8a2db3` 将源区间显示映射移回MotionCurveClip正式定义，提交 `f2df9c64e` 修正Keyframe显示副本的值类型拷贝，提交 `7545f0b22` 保留源区间边界切线；Formal 裁剪/缩放草稿期间 `[Ref]` 曲线现在按正式源映射实时刷新；真实窗口验收仍未完成。
- [x] 11.19 在现有binding/Session内只提交实际变化字段/曲线，空手势不记Undo；分离作者SelfEase与派生OtherEase，删除每次遍历所有Clip回写曲线/字段的路径，保留已有失败回滚（A07）。提交 `48a1d2a72` 已加入HasChanges差异门、SelfEase初始化和曲线等价判断；真实窗口验收仍未完成。
- [x] 11.20 将Track Muted等正式状态改为同一正式命令提交，禁止先写Source再验revision；Track锁定贯穿本轨Clip的原手势与菜单，锁状态只按已有编辑语义保存（A08）。提交 `48a1d2a72`、`ff70519c2` 已接正式Muted命令、可见锁定按钮和本轨Clip锁定阻断，`e7b22ab74` 让binding重建按AuthoringId恢复锁定视图状态，`4a95e1cb2` 补齐Add/Delete/Split/Reorder/Paste/Section等正式命令入口的锁定阻断；真实窗口验收仍未完成。
- [ ] 11.21 原移动/裁剪/缩放/切分准确接入ClipIn、局部曲线及正式源区间含义，Copy即时捕获正式内容，Paste新身份；缺失源操作由原Motion/Warp owner提供，不只改Start/End冒充完整操作（A01/A09）。`0f9448bd9` 和 `adc097652` 已接入ClipIn、Split source range与Formal裁剪草稿；`e3833c11` 已让native/formal共用原生Clip移动事件算法；formal binding 现在移动 Start 时保持原长度，曲线提交按当前 Clip 时长归一化，切分时按 descriptor 的归一化时间域生成两段曲线，裁剪提交会补齐当前时长边界 key 并清理范围外 key；`b7534ec31` 让曲线边界、当前值和Key导航跟随未提交的Clip起止帧草稿；移动提交、完整缩放重定时和局部曲线左右段映射仍未完成。
- [x] 11.22 用原Section编辑控件接名称与整数帧配置，保持已接通新增/删除；精确展示创建过期/必填字段错误并保留输入，不新增另一套表单/校验规则（A12）。现有Timeline Inspector typed Section字段与正式ConfigureSection、AddSection失败反馈已接通；真实窗口验收仍未完成。
- [x] 11.23 接通数据/选择/视野/真实观察变化的重绘通知，移除空RequestRepaint和无条件窗口循环重绘依赖；与11.11一起收口当前Surface范围缓存释放，不新增轮询服务（A10）。提交 `bf07cf6e6`、`8b7a0e2a3` 和现有RepaintRequested链已接通；真实窗口验收仍未完成。

- [x] 11.24 将Clip/曲线/菜单的既有Begin/Commit/Cancel状态关联统一，覆盖明确取消、失效目标和关闭；不让直接binding.BeginEdit绕开Surface取消入口，不新增事务管理器（A13）。提交 `d245e545f` 已统一Surface事务入口并接Escape/关闭取消；真实窗口验收仍未完成。

## 12. Timeline直接内容Runtime（新增规划范围）

DOMAIN-BOUNDARIES-20260914-03执行补充见timeline-direct-runtime.md第9节。12.3完成整区间Enter/采样/Exit及循环，12.4接真实领域候选；12.5将Stop与未决Advance的关系纳入同一Step，禁止提前清空已提交活动状态或Abort角色；12.6接已提交私有状态恢复；12.9交付实际采用/运行结果。核心负责独立TreeClip服务和角色结果汇集，本任务不共写其Host/codec/技能编译。保留后续已有正确代码，不新增或复制checkbox；只通知本实现，不要求回执。

2026-09-14职责复审（主方案D19）：当前工作区已有显式NumericTarget／资源依赖解析、游标Advance候选、Commit／Discard及停止状态入口，不能再报告为只有Prepare；但nextFrame活动Clip列表不等同区间调度。12.3必须覆盖跨过的短Clip与完整循环，12.4／12.5必须把实际业务输出和停止也纳入调用方Step接受边界。现有UI成果保持，以下完整事项仍未勾选，主方案只负责共享技能／角色接口集成。

2026-09-14依据PARALLEL-20260914-DOMAIN-01登记；具体合同、当前代码对账和文件 owner 见[timeline-direct-runtime.md](timeline-direct-runtime.md)。用户已把这条 Runtime 线纳入当前 goal；不新建窗口，也不向其它窗口派工。当前源码已有 direct-runtime 合同、Skill `ITimelinePlaybackService` 接线和直接内容 Composition；旧的无调用 `TimelineControlRuntime` 已删除，旧合同中仍被并行 Simulation 诊断使用的类型保持不动。第1–11节原UI进度保持；此处是主方案1.5–1.7、3.8与8.2中Timeline域内部分的唯一执行清单，主方案保留公共集成和责任指针，不复制勾选项。

2026-09-15 Runtime 实施对账：`TimelineRuntimePreparation` 已接通完整区间边界、循环分段、逐 Clip 样本、TreeClip Enter/Update/Exit、首帧/循环头 ActionCue 与 CameraCue、正式 Animation/Motion/Camera/Cue/Scene 曲线采样、四类 Camera 资源 Clip typed sample，以及 Step 的 Commit/Discard 回调；`TimelineRuntimeService` 已接通正式播放实例表、Skill `ITimelinePlaybackService` 请求适配、Graph activation/authoring route 校验、`TimelineRuntimePlaybackRequestFactory`、Stop/Shutdown 传播、直接 Scene Presentation 候选提交和 Float32/Fixed 无精度转换的 Capture/Restore；Restore 现在校验 RequestId、Section 和活动 Clip 的内容边界。`TimelineRuntimeEvaluationBuffer` 已在同一 Commit 边界发布不可变 committed evaluation；新增 `TimelineRuntimeCompositionHost` 将 Composition、观察 Buffer、外部 typed sinks 和 TreeClip service 收口为一次正式装配。当前仍未勾选的项目只代表主实现尚未提供具体 domain binding/call source、TreeClip 技能服务、角色/World 汇集器、旧 operation-backed 入口迁移和 Preview/Diagnostics 实际创建/消费；主工程仍没有创建并持有该 Host。Timeline Runtime 不伪造这些服务，也不以接口存在替代实际接线。

- [ ] 12.1 以正式 `TimelineData`/`TimelineContentClosure` 为来源建立只读内容闭包或 portable 表示，保留轨道、Clip、Section、曲线 descriptor、资源引用和稳定 identity；清除 direct-runtime 对 operation/IR/state slot 的前置，不新增第二份可编辑资产、不改公共 C# 生成器。`TimelineContentDiscovery` 继续生成正式闭包，Prepare 现在复制并初始化活动播放的只读 Timeline 快照，且该快照已从公开运行结果隐藏；资源/成员的最终 typed 消费仍需接入正式 domain owner。
- [x] 12.2 在 binding 准备之上提供独立分型 `Prepare/CreatePlayback`，明确 RequestId、内容 revision、NumericTarget、真实调用身份、播放模式、资源/成员/TreeClip 服务依赖、Ready/失败结果，并在创建后报告实际 Playback 版本和 generation。`TimelineRuntimeComposition` 已提供 Skill 与非 Skill 两条正式入口，`TimelineRuntimeService.LastFailure` 提供准备失败原因。
- [x] 12.3 将时间、边界、循环和Section规则迁到直接内容调度；直接 Playback 已处理首帧Enter、跨区间短 Clip、循环尾段/整段/头段、逐 Clip样本及Enter/Update/Exit，并由同一Step Commit/Discard安装或丢弃。旧无调用 `TimelineControlRuntime` 已删除。
- [ ] 12.4 在已有cursor／cycle／section／活动Clip候选及Commit／Discard基础上，接入实际窗口、Motion／Camera／Cue、TreeClip和trace待提交结果，由调用方统一Step接受；候选列表不等同已经调度业务。`TimelineRuntimeExecutionConsumer`、`TimelineRuntimeEvaluationFanout` 已把同一Step分发到多个typed sink并共同Commit/Discard，`TimelineRuntimeEvaluationBuffer` 已发布 committed evaluation，`TimelineRuntimeCompositionHost` 已保证该Buffer不会在装配时漏接，`ScenePresentationPanelTarget` 已接入真实Scene参数写入；角色/World、Motion/Camera/Cue和TreeClip正式汇集器仍未由主实现注入。
- [ ] 12.5 接主实现提供的独立TreeClip服务，保留Decision／Commit、调用实例和取消传播；Stop／ForceStop／ActionContextEnded只关闭精确generation的窗口和调用。`RequestStop`、Stop Commit/Discard、Shutdown 和精确 playback request 已接通；实际 TreeClip 技能服务仍需主实现注入。
- [ ] 12.6 提供 Float32/Fixed 分型 Capture/Restore 候选，保存已提交 Timeline 私有状态、调用关联、内容/资源 revision 和 schema；由主实现组合总快照并原子安装，不捕获 Pending、Unity对象、缓存索引或重发副作用。Timeline侧现在还保存 PlaybackMode、首帧边界状态、committed cursor/cycle/section/active clips、identity、generation、NumericTarget、revision 和 schema；外层主实现组合快照尚未接入。
- [ ] 12.7 消费现行 `RootMotionCurveAsset` 唯一源区间/时间映射及 Camera/Motion/Warp typed 领域服务；不重开曲线迁移、不修改共享算法或字段，保证 UI Reference 显示和 Runtime 求值共用同一源定义。direct evaluator 已复用现有 Animation/Motion/Camera/Cue/Scene sample API，Camera Override/Zoom/Stretch/Shot 通过原 CameraTimelineSampling 输出正式资源 identity/priority/weight，并输出携带正式配置的 MotionWarp request；MotionWarp/角色 Camera 领域服务仍待正式 owner 消费。
- [x] 12.8 迁出 Timeline 专属轨道/Clip 发射和 ProgramPlan 消费者，把共享 `Evaluator/Host/codec` 的改动交由唯一主实现 owner；不得整文件删除混合的 Camera/Motion 资源处理。当前无调用的 `TimelineControlRuntime` 已删除；`TimelineControlContracts` 中仍被并行 Simulation 诊断/运动代码使用的公共类型保持原 owner，不作为第二个执行器。
- [ ] 12.9 向预览 owner 提供独立准备结果、实际创建版本、playback identity 和已提交运行观察；Skill 与非 Skill 共用 Runtime，作者导航/游标不推进运行，不创建预览专用播放器。`TimelineRuntimeCompositionHost` 已提供一次性非Skill/Skill组合入口，`TimelineRuntimeService.PlaybackChanged`、`TimelineRuntimeEvaluationBuffer.CommittedEvaluation` 和停止事件已提供运行观察；现有Timeline窗口已接入direct playback的活动轨道/Clip overlay，但主工程仍没有创建并持有该Host，Graph Shell实际采用版本展示仍未接入。

本清单不包含测试、编译、验证或资产生成任务。不存在旧Program/Slate fallback；接口缺失按明确owner记录，不新增空服务或第二套执行清单。本次登记不代表上述能力已实现，也不改变原任务的已完成事实。
