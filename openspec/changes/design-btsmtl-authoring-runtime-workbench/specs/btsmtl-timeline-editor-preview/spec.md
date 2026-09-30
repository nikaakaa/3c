## MODIFIED Requirements

### Requirement: ScenePlay 必须拥有预览生命周期

Preview MUST 采用 CMC 式隔离隐藏 Scene，在 Edit Mode 内装配正式 ScenePlay Session，MUST NOT 启动 Unity Play。打开预览区 MUST 自动创建并准备，正常使用 MUST NOT 要求作者执行 Start Session 或 Prepare；仅在实际耗时等待或失败时显示原因。编辑器宿主 MUST 接入已有 GameplayTickSystem 驱动正式逻辑和表现，MUST NOT 建立 CMC 动作块回调或第二套节点、Timeline 求值链。ScenePlay 在此表示正式 Session，不表示 Unity Play Mode。

ScenePlay 的正式 Session、Actor 和 RuntimeDebug owner MUST 统一拥有 Start、Pause、Resume、Stop、Ability 输入、Live Debug、Capture 和 History。作者编排和 RuntimeDebug MUST 复用原 Timeline 工作面；独立可停靠 Preview MUST 承载共享视口、黑板与执行投影，并允许从 Ability 节点图联动进入；基础交互与 Session 菜单只提交正式请求。Profile MUST 以 AssemblyPrefab 创建隐藏 Preview Scene，再以 ContextId 匹配实例内 Composition SessionId，最后在该 Session 内匹配 DefaultActorId；缺少或重复目标 MUST 明确拒绝。实际游戏观察 MUST 精确匹配该装配 Prefab 的实例与 Context，不得接管其它场景的同名目标。

#### Scenario: 从编辑器开始一次 Ability 预览

- **WHEN** 作者选择有效 Profile 并打开独立 Preview 窗口
- **THEN** ScenePlay MUST 在隔离隐藏 Scene 中实例化声明的正式装配 Prefab，装配唯一 Session/Actor
- **AND** Ability MUST 通过正式输入和请求入口启动；窗口不得直接创建 Timeline playback
- **AND** 未运行、准备中、准备失败、目标缺失和已连接 MUST 根据正式状态区分，不以点击成功冒充准备完成

#### Scenario: 暂停与停止

- **WHEN** 作者点击 Pause、Resume 或 Stop
- **THEN** 请求 MUST 作用于精确解析的正式 Session
- **AND** 请求 MUST 只影响本入口绑定的目标；隐藏预览不启动或停止 Unity Play，也不得操作无关 Session
- **AND** Capture、History 和 Resume Live MUST 只调用 RuntimeDebugSession，不创建恢复或回放实现

### Requirement: Timeline UI 不得拥有运行时执行状态

TimelineEditorWindow MUST 不拥有 evaluator、独立时钟、playback command source、TimelinePreviewSession、AnimationPreviewRuntime、Preview Player、隐藏 Action runtime 或独立 PlayableGraph。窗口本地只保存 authoring selection、view state、观察绑定和显示过滤。

#### Scenario: 打开同一 Timeline

- **WHEN** 作者切换页面或重复打开同一 TimelineData
- **THEN** 页面 MUST 复用原 Timeline 面板，不按调用创建窗口
- **AND** TimelineData MUST 不保存窗口时间、目标、generation、播放状态或 GUI 游标
- **AND** 关闭窗口 MUST 释放该工具面的运行观察 interest；仍有预览承载窗口时 Session MUST 保持，最后一个承载预览的窗口关闭时隐藏宿主 MUST 结束并释放 Preview Session；实际游戏 Session MUST NOT 被观察窗口关闭所终止

### Requirement: Timeline 预览必须消费正式 Runtime 事实

Timeline UI MUST 只读取正式 Timeline Runtime、Ability lifecycle、Action playback、Pose 结果和 ScenePlay diagnostics 发布的 binding、playback identity、generation、content revision、active Clip、窗口、TreeClip 阶段、Motion/Cue 结果和 completion trace。UI MUST 不从 Animancer weight、当前 authoring 游标或场景对象推断运行事实。运行投影 MUST 取自对应 playback 的冻结内容和该观察位置的实际事件，不以当前作者内容补齐尚未执行的 Track/Clip。

#### Scenario: 当前 Ability 没有执行该 Timeline

- **WHEN** 正式 Session 的 playback summary 不包含当前 Timeline identity
- **THEN** UI MUST 显示未执行或未绑定
- **AND** MUST 不调用预览求值器、不重采样 TimelineData、不猜测其它 Actor

#### Scenario: 同一 Timeline 有多个播放实例

- **WHEN** 正式 Runtime 同时存在多个 playback identity
- **THEN** UI MUST 要求作者显式 Pin；Follow 仅在调用关系能确定唯一当前目标时导航
- **AND** 不得自动选择第一个实例或按名称匹配

#### Scenario: 返回较早历史位置

- **WHEN** 作者从较晚 Capture Segment 返回较早 Segment
- **THEN** 显示 MUST 使用较早位置的 SourceMap、事件和 playback 冻结内容重新建立观察集合
- **AND** 较晚位置出现的 Track/Clip MUST NOT 残留，当前 Runtime MUST NOT 被重算或改写

#### Scenario: 开放时长 TreeClip 已退出

- **WHEN** 对应 cycle 已记录 TreeClip 退出事实
- **THEN** 只读 Clip 的结束位置 MUST 使用实际退出时间
- **AND** 已退出 Clip MUST NOT 继续显示 open，也不得由旧 Enter/Active 事件覆盖较新的 Exit 状态

### Requirement: Timeline必须与共享预览区完成跨窗口联动

作者编排 MUST 复用原 TimelineEditorWindow；独立 Preview MUST 复用 Slate 显示执行投影，打开 Ability MUST 能联动其节点图；作者 Timeline 与执行记录 MUST 使用各自正式数据源。RuntimeDebug MUST 根据正式调用关系导航已有 FlowCanvas 与原 Timeline 面板；窗口只改变观察绑定，不能按调用创建新面板、替换 ScenePlay Session 或把 FlowCanvas 嵌入 Slate。

#### Scenario: 技能产生多个Timeline调用

- **WHEN** 同一运行调用从 RootTree 进入子图、Timeline 或 TreeClip 子图
- **THEN** 系统 MUST 根据实际 ActionInstance、完整调用路径、generation 和 playback 选择对应来源
- **AND** 子调用返回后 MUST 导航仍在执行的父调用方；并行分支无法唯一决定时 MUST 请求显式 Pin

#### Scenario: 编辑后返回预览

- **WHEN** 作者修改 Timeline 或执行 Undo
- **THEN** 作者数据 MUST 保持，状态 MUST 区分作者 revision、实际采用 revision 和导出/准备/发布 revision
- **AND** 旧 Export、Plan、Publication 与当前内容不匹配时 MUST 作废，活动 playback MUST 保持原冻结内容
- **AND** 切页、折叠和关闭 Timeline MUST NOT 停止 Session

#### Scenario: 独立内容预览

- **WHEN** 当前内容无法对应所选场景的正式调用方
- **THEN** 窗口 MUST 保留作者编辑并显示缺少绑定
- **AND** MUST NOT 创建假 Actor、Skill 或私有播放器

### Requirement: 编辑控件不能冒充真实角色预览

Timeline MUST 使用秒制编辑游标和时间输入，保留独立的秒／帧显示切换与显式吸附网格；编辑游标、真实运行标记和 Capture 历史位置 MUST 分别保存。已明确的 Profile、三态切换和 Session 菜单 MUST 通过正式 ScenePlay owner 操作运行。普通来源导航 MUST NOT 重建 Session；打开效果预览区 MUST 自动装配唯一正式宿主。基础编排的拖动查看效果 MUST 通过正式 owner 提交定位请求，角色表现只显示实际到达结果；Ability 执行游标 MUST 使用所选位置的正式记录。嵌入按钮、快捷键、EditorUpdate、初始化/释放、保存和 delayCall MUST NOT 调用 Slate Play/Sample/ReSample/Stop 执行预览，MUST 清理 AutoKey 与临时作者播放器。

#### Scenario: 没有运行绑定

- **WHEN** 作者独立打开 shared Timeline
- **THEN** 轨道、Clip、曲线和编辑帧 MUST 完整可编辑
- **AND** MUST NOT 创建本地播放器或猜测角色目标，不把游标移动显示成角色已运行

#### Scenario: 运行时继续编辑

- **WHEN** 作者在 Preview 中编辑正式 Timeline
- **THEN** 作者数据 MUST 经同一 Mutation/Undo 修改，运行标记 MUST 只读消费真实绑定
- **AND** 新版本是否采用 MUST 由正式内容 owner 的实际报告决定；没有实际目标、版本未知或准备失败时 MUST NOT 显示已采用

### Requirement: 动态 TreeClip 长度必须来自对应执行域的实例事实

Logic 与 Presentation 的 TreeClip MUST 支持显式 TreeDecision 结束来源，图内“结束片段”只结束当前调用实例。Presentation 的 FrameBoundary MUST 保留为显式固定区间模式。正式 TreeClip 运行事件 MUST 携带本次实例的 `ExitSource`，RuntimeDebug MUST 使用记录中的 `ExitSource` 区分动态和固定片段，不能从当前作者资产补推。动态长度 MUST NOT 取作者 End，也 MUST NOT 反写作者资产；Presentation 的作者 End 只作为可编辑布局上界，Logic 的作者 End 跟随 Timeline 终点；表现域退出 MUST NOT 修改 Gameplay 时钟或逻辑生命周期。

#### Scenario: 动态片段仍在执行

- **WHEN** 所观察实例尚无已接受的退出或销毁事实
- **THEN** 可视 End MUST 使用对应执行域的已提交游标；Presentation 使用表现时间，Logic 使用逻辑时间
- **AND** 身份 MUST 包含 playback、generation、cycle 和 Clip；不同调用不能共享实际长度

#### Scenario: 表现图主动结束片段

- **WHEN** 当前表现 TreeClip 在 OnEnable 或 Root 请求退出
- **THEN** 同一候选帧 MUST 执行 OnDisable、撤下该实例的活跃状态和持续输出
- **AND** 只有整帧接受后 MUST 保存退出并发布实际退出时间；Discard MUST 保留此前接受状态

#### Scenario: 回看退出之前的历史

- **WHEN** 当前历史位置尚未包含本次实例的退出事实
- **THEN** Clip MUST 显示 open 并截取到该历史位置的对应域游标
- **AND** MUST NOT 读取未来事件或使用当前作者结束时间

#### Scenario: 父播放结束或被撤销

- **WHEN** 正式父 Timeline 结束、离开 cycle、停止、撤销或修正
- **THEN** 动态子片段 MUST 由原生命周期链路退出或销毁，不留下孤立输出
- **AND** MUST NOT 为延长子片段创建独立时间源或继续执行已结束父播放

## ADDED Requirements

### Requirement: 作者 Timeline 与 Ability 执行投影必须分开

作者 Timeline MUST 表达可编辑的内容安排；Ability 执行时间线 MUST 表达实际节点、决策、循环和调用记录，两者 MUST 复用原绘制交互基础而不共享可写数据。基础编排 MUST 支持正式内容效果查看，执行投影片段 MUST 可定位来源但不可编辑成作者 Clip。运行变量命令与作者 Mutation MUST 分别显示实际采用状态。

#### Scenario: 从调用记录进入内容编排

- **WHEN** 用户点击执行时间线中的一次 Timeline 调用并修改其作者内容
- **THEN** 系统 MUST 定位对应来源并使用原作者编辑入口，修改按正式内容采用规则进入后续运行
- **AND** 该次已发生的执行记录 MUST 保持原版本及调用身份，不随作者修改被重写

### Requirement: Timeline 内容刷新必须与图编译区分

Timeline 的时间、长度、曲线、资源与普通 Track/Clip 编排 MUST 走正式内容刷新，MUST NOT 无条件 Build Ability 图。Track/Clip 结构变化 MUST 由正式 owner 重建对应绑定与播放生命周期；TreeClip 内部图或编译依赖变化 MUST Build 受影响的图。常用编辑提交 MUST 自动组织正式内容采用步骤；活动 Ability playback MUST 保持其原版本，新调用采用新内容。纯 Timeline 编排 MUST 通过正式定位显示更新后的当前游标结果。

#### Scenario: 调整 Clip 时间

- **WHEN** 作者提交 Clip 时间变化，图结构和编译依赖未改变
- **THEN** 系统 MUST 更新 Timeline 内容并显示采用状态，不触发 Ability 图 Build
- **AND** UI MUST 区分当前活动 playback 与新采用内容的版本，不改写已记录历史

#### Scenario: 新旧内容的播放同时存在

- **WHEN** 活动 playback 继续使用旧内容，而新调用已采用更新后的 Timeline
- **THEN** 两者的运行事件 MUST 分别携带对应正式内容的 SourceMap 版本，Live 观察、执行时间线和来源跳转 MUST 按事件版本解析
- **AND** Capture 与 History MUST 保留这些映射，MUST NOT 使用当前作者资产或角色初始来源表替代记录版本
- **AND** 重复调用 MUST 复用正式准备的内容指纹和映射，不为观察重新遍历作者 Timeline 计算版本
