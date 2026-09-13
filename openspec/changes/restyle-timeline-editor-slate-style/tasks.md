2026-09-13 源码对账：ce21aec8f/afcb90056已恢复原Slate UI及正式新增/游标，仍创建隐藏组件代理，DirectProjection已删除。第11节按[源码解耦决策](slate-source-decoupling.md)重新展开为未完成实现项。第1–10节保留此前功能记录，但对已由当前源码证明不成立的勾选予以纠正；未重新核对的历史勾选不构成本次端到端声明。正确业务实现不回退。只在主线实施，不向旧worktree双写；不新增测试、手动验证、编译或校验任务。

2026-09-13 领域协调仅PLAN：原UI/数据/Undo实现要求不变；预览联动改用独立技能、原生Pose重建、Camera绑定/Reset及Session领域准备，废除总包Build/ProgramEpoch前置。Motion源XYZ/Yaw归外部源，UI只接曲线owner的typed字段与导航。本次只更新清单含义，不向实现任务下发或扩大授权。

## 1. 已有接入基础

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
- [ ] 2.4 完成diff到Session/正式owner的校验、提交与失败恢复；当前ApplyModify无自动校验/回滚，过期分支仍有静默返回，随11.8收口
- [x] 2.5 收口取消、关闭、Undo/Redo、外部 owner 修改和过期草稿；无效草稿不覆盖正式数据
- [ ] 2.6 Clip、Curve、属性和菜单使用同一正式Undo；当前原DopeSheet/Inspector仍有组件Undo入口，随11.6–11.8改接，不以已有Session提交推定全部隔离
- [ ] 2.7 删除、复制、排序、跨轨道移动直接接正式contract/引用规则，复制新ID、排序保留ID；当前仍有先改组件再diff路径，随11.5收口

## 3. 正式新增

- [x] 3.1 打开请求显式携带 owner contract composition，Add Track 候选来自正式 catalog/type metadata，收集 Track 必填字段
- [x] 3.2 接通各 Track 允许的 Add Clip 输入：Animation 资源、TreeClip ownership/来源、Motion/Camera/Cue/Scene typed binding；基础 Slate UI 只传递资源和 binding identity，Tree contract 由正式 Track 工厂解析来源
- [x] 3.3 在唯一 Session 事务内创建正式对象；合法空 Track 可保存，取消 picker/输入非法/owner 过期不留半成品或 Undo
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
- [x] 6.2 按稳定 identity 保存并恢复选择、展开、当前帧、横向视野和纵向滚动；删除对象不自动改选首个 Clip，不保存自制 Inspector 宽度
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

实现门槛：打开、刷新、新增、选择、编辑、关闭均不创建或依赖 Slate 组件树；原生 Cutscene/Actor/Director 规则不能阻止正式 TimelineData 的合法操作。__BTSMTL_SlateTimelineProjection__ / BuildProjection 残留不作为最终方案或 fallback。本段是实现约束，不新增验证任务。

- [ ] 11.1 D1/D2：在现有Slate Editor模块收窄原编辑输入，原生Cutscene入口与BTSMTL入口共用一套原函数；BTSMTL输入不继承IDirector/IKeyable运行接口，不增加空运行实现或替代绘制分支
- [ ] 11.2 D1/D3：将CutsceneTrack原OnTrackInfoGUI、DoDefaultInfoGUI、DoParamsInfoGUI、OnTrackTimelineGUI、DoClipCurves及原展开/高度状态参数化，必要时搬入现有Editor模块；旧位置不保留第二份函数主体
- [ ] 11.3 D2：把现有Projection中正确的ID映射、typed新增、曲线换算和提交迁移为BtsmtlSlateTimelineBinding；只引用正式对象和必要手势草稿，不恢复EditorModel/DirectProjection两条路径
- [ ] 11.4 D3/D4：原ShowListGroups/ShowListTracks/ShowTimeLines及ActionClipWrapper就地改接该输入；保留Rect、样式、GUI.Window/DragWindow、框选和边缘交互，能力与重叠规则来自正式Capabilities/contract，分离SelfEase与派生OtherEase
- [ ] 11.5 D3/D7：原Track/Clip/Section菜单和排序释放直接提交正式命令；Section不依赖directorGroup，显示边界不保存成Section，删除无正式合同的Actor/循环/任意组件创建命令
- [ ] 11.6 D5：原CurveRenderer/DopeSheetRenderer/参数工具只接正式Timeline-local曲线、编辑时间和事务通知，保留原key/切线/缩放算法；删除proxy假字段，保留局部Weight/Ease；Motion源XYZ/Yaw仅只读/源导航，源区间及映射消费曲线owner的typed接线
- [ ] 11.7 D6：原ActionClipInspector通用控件参数化并通过真实serialized owner接入Unity已有Inspector；普通字段走Read/Configure，选择不写代理context，不增加假Actor、假Unity Object或Timeline右侧自制面板
- [ ] 11.8 D7：在现有Session/TimelineData mutation链收口手势、字段、菜单的一次提交、完整业务校验、source revision反馈与该次owner范围失败恢复；组件Undo退出BTSMTL编辑，选择/滚动不产生事务
- [ ] 11.9 D8：原标尺/游标/步进/局部曲线吸附使用正式帧上下文，编辑不依赖Cutscene/Actor；保留原Runtime/History与Scene Play归属，实际采用读取领域报告，删除BTSMTL的Slate采样副作用，不擅自新增Play或实现领域工厂
- [ ] 11.10 D2/D8：正式入口切到无组件binding并删除BuildProjection/CreateChild/隐藏宿主、BtsmtlSlateGroup/Track/ActionClip及组件字典/扫描/销毁；删除无消费者的EditorModel与过时接口/meta，真实Slate组件与正式Actor/Camera资源不在删除范围
- [ ] 11.11 D5/D7：原选择、曲线缓存与Undo订阅按窗口/正式ID恢复和释放，改为可解除回调，关闭丢弃未提交草稿但不改已保存数据；原native/BTSMTL共用Renderer，不互相清空状态
