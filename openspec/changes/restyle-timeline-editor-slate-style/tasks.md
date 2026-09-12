2026-09-12 审计：以下任务按主线实现证据管理。条目按本次模块重新编号，旧编号通过 Git 历史追溯；已重新打开的条目表示代码基础存在，但未满足完整行为。文档写完、严格校验通过、编译通过均不等于窗口已验收。只在主线实施，不继续向旧预览 worktree 双写。人工验收行为列在 design.md，不作为本清单任务；默认不新增测试代码。

## 1. 已有接入基础

- [x] 1.1 已有 CutsceneEditorSurface 的 InitializeEmbedded/DrawEmbeddedGUI 和 transaction callback；后续布局/播放清理分别由布局和预览边界章节承担
- [x] 1.2 已有 TimelineData 正式 owner 与 Editor-only Slate projection 的身份映射基础，不把临时对象作为保存资产
- [x] 1.3 正式打开入口已切换到 Slate Surface，旧 UI Toolkit 仿 Slate 时间轴不再作为正式入口
- [x] 1.4 不恢复旧 UI Toolkit viewport/interaction/rendering 并行实现；帧和 GUI 改造在 Slate Surface 内完成
- [x] 1.5 已有 HideAndDontSave projection host、Track/Clip 对象；序列化和释放问题由 5.5 继续收口
- [x] 1.6 已有正式 Track/Clip/Section identity 到临时对象映射；刷新恢复由第 6 节收口
- [x] 1.7 Skill/Shared Timeline 正式入口已有唯一 TimelineEditorWindow 承载 Slate
- [x] 1.8 Slate 类型/API 无法加载时已有明确 unavailable 入口，不回退到另一套 UI
- [x] 1.9 已抽取不创建第二个 EditorWindow 的 Surface
- [x] 1.10 已移除 CreateInstance<CutsceneEditor> 伪造窗口路径

## 2. 投影和数据编辑收口

- [x] 2.1 对齐 Clip/Section/Curve 全部显示与 domain 映射，包含 Motion CurveEndFrame、Scene Presentation valueCurve 和真实内容终点；未支持字段明确报告
- [x] 2.2 将正式 selection、属性、TreeClip ownership/下钻和 AnimationClip 资源导航接入同一 adapter，不仅保存在临时名称中
- [x] 2.3 完成有效手势 begin/commit/cancel 与 source revision 校验；选择/游标/缩放不生成 mutation，不因每次 MouseUp 重建
- [x] 2.4 完成 diff -> Session -> 正式 owner 的校验/提交/刷新；拒绝操作给出原因，删除 Unsupported 静默吞修改路径
- [x] 2.5 收口取消、关闭、Undo/Redo、外部 owner 修改和过期草稿；无效草稿不覆盖正式数据
- [x] 2.6 核对 Clip、Curve、属性和菜单路径只产生一个正式 Undo，隔离临时 Slate Undo/dirty，并保留取消和失败事务结果
- [x] 2.7 将删除、复制、排序、跨轨道移动统一接正式 contract/引用校验，复制生成新 identity，排序保留 identity

## 3. 正式新增

- [x] 3.1 打开请求显式携带 owner contract composition，Add Track 候选来自正式 catalog/type metadata，收集 Track 必填字段
- [x] 3.2 接通各 Track 允许的 Add Clip 输入：Animation 资源、TreeClip ownership/来源、Motion/Camera/Cue/Scene typed binding；基础 Slate UI 只传递资源和 binding identity，Tree contract 由正式 Track 工厂解析来源
- [x] 3.3 在唯一 Session 事务内创建正式对象；合法空 Track 可保存，取消 picker/输入非法/owner 过期不留半成品或 Undo
- [x] 3.4 用 Slate Surface 的“＋轨道”和轨道右键“在第 N 帧添加 Clip”替换原生无正式身份创建入口，成功后恢复新对象选择

## 4. 帧几何

- [x] 4.1 由正式 Session FrameRate 提供统一像素/帧/Slate秒换算，删除嵌入路径对 Slate 全局 FPS/timeStepMode/snapInterval 的时间权威依赖
- [x] 4.2 标尺、游标输入、逐帧、Clip/Section 拖动与裁剪使用整数帧；区分一帧移动与关键帧跳转
- [x] 4.3 按 Curve descriptor domain 换算 key time/tangent，保留未编辑 key、weight、WeightedMode 和 wrap，不全量量化资产
- [x] 4.4 内容终点使用真实 MaxFrame，移除最少一秒和额外一秒；显示全部只改变视窗，终点线不提供无正式数据对应的编辑

## 5. GUI 布局和生命周期

- [x] 5.1 合并文档名/ownership/来源，工具栏统一高度；清掉重复标题和隐藏控件的空白占位
- [x] 5.2 Slate Surface 通过单一 SurfaceLayout 计算工具栏、搜索、缩放、标尺、左右轨道和命中区域；属性区由同一 Timeline 窗口的 UI Toolkit 宿主管理
- [x] 5.3 左右共同行高度与垂直滚动，曲线展开同步；属性区可收起/调高，窄窗口自动收起属性区
- [x] 5.4 删除临时 Auto、“作者预览”、无关 Actor/Director/Render 和未映射菜单；曲线入口不再拼入 Track 名
- [x] 5.5 根据完整堆栈修复重复序列化字段、GUI 和 proxy 生命周期异常，销毁时释放临时宿主/选择/回调，不隐藏错误代替处理

## 6. 刷新与属性

- [x] 6.1 正式 Track/Clip/key selection 驱动属性区，时间按帧、资源精确引用、Curve 只显示当前注册 channel
- [x] 6.2 按稳定 identity 保存并恢复选择、展开、当前帧、横向视野、纵向滚动及属性高度；删除对象不自动改选首个 Clip
- [x] 6.3 Add Track/Add Clip 弹窗在正式提交失败或 owner 过期时保留当前输入并报告原因；DopeSheet 仅按像素密度减少显示 key，不修改正式曲线

## 7. 预览边界

- [x] 7.1 移除误接的嵌入 Slate Play/Sample/ReSample/Stop 和私有时钟，保留静态编辑游标、逐帧与被动运行标记
- [x] 7.2 完整审计默认 Director/Actor 清理和嵌入 EditorUpdate、快捷键、初始化/释放、保存、delayCall 的 Slate 内核调用；原勾选因后续接回播放而重新打开
- [x] 7.3 Timeline 通过 `RuntimeDebugSession` 的正式 Timeline playback summary 接入场景/技能观察，Preview 返回 Graph Shell；运行事实只读、作者仍可编辑，多调用不猜选，Graph Shell 继续拥有运行控制
- [x] 7.4 作者游标、Runtime overlay 和 History overlay 使用三个独立时间状态；Graph Shell Segment 选择进入历史观察，Timeline 不提供未经批准的本地 Play

## 8. 文档与交付

联合预览实施的 P1–P5、原任务映射与行为标准见 [preview-integration-plan.md](preview-integration-plan.md)。以下联动任务负责跨窗口集成；场景预览原 tasks 继续记录其 owner 内部实现，不复制一套协调器待办。

- [x] 8.1 2026-09-12 完成 proposal/design/delta 对账，记录 current spec 待替换条款与待确认 Play 语义，并同步修正场景预览 delta 的结构只读冲突
- [x] 8.2 2026-09-12 对 restyle-timeline-editor-slate-style 和 rebuild-btsmtl-preview-with-scene-play 执行 openspec validate --type change --strict，均返回 is valid；结构合法不代表实现完成
- [x] 8.3 `implementation.md` 已交付模块输入/输出、实际代码链、编译与严格校验结果、删除范围及未完成项，不用旧交付说明代替
- [x] 8.4 Scene valueCurve 与 typed binding 已对照正式 catalog/binding、Skill Document exporter/applier/validator；UI 与 Document 共用正式能力，不复制 schema
- [x] 8.5 分模块中文小步提交，保留其它任务改动；dotnet build 按 AGENTS 禁用构建服务器并立即 shutdown，不新增测试代码

## 9. 与预览窗口联合实施

- [ ] 9.1 P1：将共享预览 presenter 接入实际 SkillGraph/Graph Shell 宿主，按场景控制、试验/采用、观察、折叠历史分组，消除旧树窗口专属接入和重复工具条
- [ ] 9.2 P2：接入场景资产/精确 context 定位和角色/非 Skill 正式目标；区分开始场景与请求技能/业务调用，状态与拒绝原因来自正式 owner
- [ ] 9.3 P3：Timeline 打开请求携带作者 locator 和可选准确 runtime binding，连接运行 overlay、Follow/Pin、双向导航及多调用选择，编辑帧与运行/历史位置隔离
- [ ] 9.4 P4：Timeline 修改/Undo 后把 authoring revision 与真实 Build/adoption 报告接入预览状态区，显示待采用/已采用/下次激活/失败，同 Session 生效不由窗口伪造
- [ ] 9.5 P5：历史面板区分诊断采集与输入录制，按选定 Tick/区间和正式 capability 校验恢复/回放，刷新不覆盖输入，命令接受与完成分开显示
- [ ] 9.6 跨宿主布局、切页、关闭、重载和绑定释放统一收口；同步预览原任务/审计的主线证据，记录缺失能力，不以按钮存在代替联合交付
