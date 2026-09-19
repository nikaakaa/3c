## RENAMED Requirements

- FROM: `### Requirement: Timeline必须使用正式帧率统一编辑时间`
- TO: `### Requirement: Timeline必须使用秒制时间统一编辑与保存`

## MODIFIED Requirements

### Requirement: Timeline必须使用秒制时间统一编辑与保存

Surface MUST以正式 Timeline 秒制作者模型作为唯一时间来源，像素、秒输入、tick／素材帧显示和 Slate 提交 MUST使用同一映射。作者 MUST能选择逻辑 tick、素材帧或关闭吸附：逻辑网格 MUST从当前绑定 pipeline 的 SimulationTickRate 生成 n/R 秒位置，素材网格 MUST读取正式素材帧率与源映射。界面 MUST显示模式与参考来源，缺少绑定时逻辑吸附 MUST不可用，不得猜测频率或另存一份编辑器逻辑 tick 配置。网格 MUST不限制保存精度，不得由 Slate 全局 FPS 或旧 60Hz 网格决定资产精度。Clip、Section、Marker 和 Timeline 自有曲线 MUST保存秒制位置并删除旧帧存储入口。

#### Scenario: 编辑一帧

- **WHEN** 作者在显示帧率 F 下把 Clip 起点从显示第 12 帧移到第 13 帧并提交
- **THEN** 草稿、属性与正式秒制位置 MUST一致为按统一精度表示的 13/F 秒，一次 Undo MUST恢复 12/F 秒
- **AND** 上一帧/下一帧 MUST按正式显示映射移动一帧，不跳到相邻关键帧；连续移动不得因逐次舍入累积漂移

#### Scenario: 秒输入不被帧显示改写

- **WHEN** 作者关闭帧吸附并输入正式存储精度允许、但不落在显示帧边界的秒数
- **THEN** 提交 MUST保存该秒制位置，MUST NOT重新量化到整数显示帧
- **AND** 改变显示帧率或运行 tick 率 MUST不改变该资产位置

#### Scenario: 逻辑网格跟随配置

- **WHEN** 当前绑定 pipeline 从 60Hz 改为 30Hz 且使用逻辑 tick 吸附
- **THEN** 网格 MUST从 n/60 秒更新为 n/30 秒，已有 0.25 秒事件 MUST保持原位置
- **AND** 预计第 8 tick 经过的读数 MUST注明正常速率、起点对齐且无暂停的前提，不得把运行中变速误作网格变化

#### Scenario: 显式重新对齐选中内容

- **WHEN** 作者对选中内容执行按当前网格重新对齐
- **THEN** 系统 MUST按统一舍入规则通过原 mutation 修改其秒数，并提供一次完整 Undo
- **AND** 未选内容 MUST不变，共享 Timeline MUST显示此次参考的具体 pipeline

#### Scenario: 没有逻辑配置绑定

- **WHEN** 独立打开的 Timeline 没有明确 pipeline 来源
- **THEN** UI MUST说明逻辑 tick 吸附不可用，仍允许合法秒输入及具备素材来源的帧吸附
- **AND** MUST NOT用默认 60Hz 或另一窗口的配置代替缺失绑定

#### Scenario: Curve时间换算

- **WHEN** 作者编辑 Timeline-local 曲线 key
- **THEN** adapter MUST按正式 descriptor 处理秒制时间、曲线 domain 与切线换算；源运动 XYZ/Yaw MUST消费曲线 owner 的源区间/映射，MUST NOT在 UI 重造采样公式
- **AND** 未编辑 key 的时间、值、tangent、weight、WeightedMode 和 wrap mode MUST保持；源曲线原生坐标 MUST NOT被误改为第二份 Timeline 作者数据

#### Scenario: 显示全部内容

- **WHEN** 作者打开短 Timeline 或选择显示全部
- **THEN** 内容终点 MUST来自真实秒制范围，视窗 MAY保留像素边距但 MUST不改变文档
- **AND** 空文档显示视窗 MUST不创建一秒内容，终点线 MUST不提供无正式字段对应的长度写入

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

- **AND** 同名重载、partial/helper搬迁或共用类名 MUST NOT被当作已合并；正式来源的独立列表/Clip循环与简化拖动 MUST删除，已正确typed命令/帧/Undo MUST迁入共用原主体
- **AND** 完成记录 MUST明确原主体保留和重复实现删除的具体位置，曲线显示、其它bug修复或编译结果 MUST NOT替代本项完成

#### Scenario: 源区间比Clip短时的显示

- **WHEN** MotionCurveClip使用1秒线性源区间而Clip持续2秒，且正式映射按源秒推进后保持终值
- **THEN** 只读显示 MUST在对应第1秒到达终值并保持到Clip结束，MUST NOT自行拉伸源区间到2秒
- **AND** 区间裁切 MUST沿正式映射/边界求值保留插值，MUST NOT把区间外所有key钳到同一端点；显示副本 MUST NOT回写源或进入运行数据

#### Scenario: Slate UI编辑Clip范围

- **WHEN** 作者在 Slate `CutsceneEditor` 中移动或裁剪一个 projection Clip
- **THEN** adapter MUST按 projection identity 将结果转换成 BTSMTL 秒制时间/value mutation
- **AND** 正式写入 MUST经过 `TimelineEditorSessionContext`、owner 和 BTSMTL Mutation
- **AND** mutation 完成后 MUST从 BTSMTL owner 重新生成 projection

#### Scenario: Timeline owner外部刷新

- **WHEN** BTSMTL Timeline 被 Undo/Redo、显式 generate_assets 或其它正式业务入口修改
- **THEN** adapter MUST销毁旧 projection 的临时状态并从最新 BTSMTL Timeline 重建
- **AND** 临时状态 MUST仅指本窗口编辑草稿与失效引用，MUST NOT创建或销毁代用组件树；有效ID选择、展开和视野 MUST保持
- **AND** MUST不把旧 Slate proxy 的字段覆盖回 BTSMTL
