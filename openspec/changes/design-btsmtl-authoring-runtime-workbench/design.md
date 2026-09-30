# 设计：BTSMTL Authoring Runtime Workbench

## 1. 产品模型

Authoring Runtime Workbench 是作者和运行观察共用的工作台。Preview 只是其中一种产品形态。工作台不拥有新的业务 Runtime；它只把正式作者入口、ScenePlay 运行和 RuntimeDebug 观察组织到同一工作面。

```text
Authoring Runtime Workbench
├─ Authoring
│  ├─ FlowCanvas / RootTree / 子图
│  └─ Timeline 编排（Assembling）+ 正式内容效果预览
├─ Preview
│  └─ Ability 节点图 + 随实际执行增长的时间线 + 角色视口
└─ RuntimeDebug
   └─ 实际游戏或已绑定 Session 的只读调用栈与运行观察

Workbench 唯一运行底座：ScenePlay Session
```

ScenePlay 在本文指正式 Session 运行链，不等于 Unity Play Mode。Preview 采用 CMC 式隐藏场景承载：打开预览区时自动创建隔离的编辑器预览 Scene，装配正式 Session 和 Actor，不启动 Unity Play，不切换用户正在编辑的场景。编辑器宿主负责场景资源和驱动接入，正式 Session 继续拥有业务运行生命周期。

此方向已确认；下文标为“待对齐”的交互和销毁策略是本次设计建议，不代表已确认或已实现。

## 2. 三种产品形态

### 2.1 Authoring / Assembling：基础编排

Authoring 面直接操作正式作者数据：

- FlowCanvas Graph、RootTree、嵌套子图、StateMachine 和 Graph 连接；
- Timeline、Track、Clip、TreeClip、Section、曲线和资源引用；
- 领域正式参数、作者版本和 Undo。

Authoring 显示并修改完整作者内容。Assembling 在本文指其中的基础编排任务，不新增第四种产品形态，也不据此改名现有代码 API。纯 Timeline 的片段、曲线、动画、特效和镜头调整，以及播放和拖动查看效果，都属于这一层；查看效果不要求进入 Ability Preview。

纯编辑页面不运行内容；打开效果预览区后，由共享隐藏场景宿主自动建立正式运行绑定。角色相关内容使用正式 Session；合法非 Skill 内容使用 Timeline 领域已有正式 owner 和目标绑定，不要求用户先搭 Ability 图或伪造角色。页面只提交播放、暂停和时间定位请求，实际执行与定位仍属于正式 owner，不增加本地采样器。

作者在 Authoring 面关心的是“内容怎样安排、调整后效果如何”。作者游标表示请求查看的位置，实际运行标记表示正式 owner 已到达的位置；两者不混为一份状态。

### 2.2 Ability Preview：执行过程时间线

Preview 在隐藏预览 Scene 内运行正式 ScenePlay Session，查看作者修改结果。Profile 的 AssemblyPrefab 提供正式场景装配来源，预览实例与作者场景隔离，不保存临时运行对象。Actor、Ability、RootTree、Timeline、Pose、Motion、Camera、World 和输入消费仍复用正式管线；不使用 Timeline 私有播放器、CMC MontagePlayer 或 Pose fixture。隐藏场景可以执行完整节点图及其子调用，不限于单个蒙太奇。

打开 Ability 后显示其节点图，并将本次正式运行展开成持续增长的执行时间线。它不是单个作者 Timeline 的播放器，也不是把作者 Clip 长度全部改成动态。时间线显示的是已发生的节点调用、决策、Loop 迭代、子图和 Timeline 调用。

| 执行事实 | 时间线投影 |
| --- | --- |
| 节点进入、持续执行、退出 | 以片段显示实际持续区间，活动片段随已提交进度增长，退出后固定 |
| 瞬时决策 | 以决策标记或瞬时片段显示发生位置、实际条件值与选中分支，不伪造持续时长 |
| Loop 迭代 | 每次迭代独立显示，保留迭代身份及内部调用，不能重复覆盖同一片段 |
| 子图或作者 Timeline 调用 | 显示可展开的调用区间，点击定位到来源节点或原作者 Timeline |
| 并行调用 | 分开显示并发区间；全部记录可见，自动聚焦仍遵守唯一调用或显式 Pin |

投影片段是只读显示对象，不生成作者 Clip/Track 资产，不作为执行输入，也不能拖拽改写已发生的调用。事实至少能区分 Session generation、Ability 调用实例、父子调用、节点来源与本次执行发生；Loop 迭代和 Timeline playback/cycle 必须保持各自正式身份。UI 不通过重跑条件或当前作者图猜测分支。

作者可以拨回执行游标查看当时的节点、变量和角色情况，也可以修改领域正式允许的运行变量，继续观察后续决策。历史查看与从历史继续执行分别定义于第 3.2 节；运行变量修改与作者资产修改分别定义于第 5.4 节。

Preview 允许作者继续编辑正式数据；点击执行投影中的 Timeline 调用可进入基础编排层。作者资产修改流程为：

```text
作者 Mutation / Undo
→ Export 冻结作者 Timeline 闭包
→ Prepare 校验当前 Host、内容代次与兼容拓扑
→ Publish 封存本次 Export
→ 正式 adoption barrier
→ 同一 Session 的后续 playback 采用新内容
```

Preview 的目标是“在真实场景里改了之后会怎样”。因此 UI 必须同时让作者知道：

- 当前 ScenePlay Session、Actor、Ability 和调用目标；
- 作者版本、已采用版本、已导出/准备/发布版本；
- Session generation 与 RuntimeDebug target revision；
- 修改是否只影响下一次正式调用，还是必须重新导出或新 Session。

### 2.3 RuntimeDebug

RuntimeDebug 面向实际游戏运行，也可只读观察当前已绑定的隐藏预览 Session。它连接一个明确目标，不为观察创建或接管第二运行实例。它只读取正式 `RuntimeDebugSession`、SourceMap、Trace、Playback、Snapshot、Capture/History 和领域提交事实。

RuntimeDebug 不编辑作者数据，不启动第二个运行实例，不重算 Graph、Timeline 或 Pose，不用当前作者游标猜运行状态。

RuntimeDebug 以真实调用栈为导航主线：

```text
Ability
→ RootTree
→ 子图
→ Timeline 调用
→ TreeClip
→ TreeClip 子图
→ 返回父 Timeline / 父子图
```

当前焦点在 Graph 时使用 FlowCanvas 作者画布显示 source-mapped 只读状态；当前焦点在 Timeline 时使用 Slate Timeline 表面显示真实 playback、Track、Clip、游标和生命周期；调用栈变化时自动切换表面，不为每个子图或 Timeline 新建窗口。

这里的切换是导航到已有 FlowCanvas 面板或原 Timeline 面板，不替换 Timeline 内的 Slate，也不创建新的工作台。Authoring 与 RuntimeDebug 继续使用原 Timeline/FlowCanvas 工作面；独立 Preview 窗口承载视口、运行黑板和执行时间线，并允许从 Ability 节点图联动进入。只有唯一明确的活动调用时自动跟随；并行调用要求显式 Pin，不能按列表顺序挑选。历史位置变化只更新记录时的观察投影，不能把较晚位置已经显示的 Clip 带入较早位置。

## 3. Session 与工作台关系

角色预览时，一个 Workbench 对应一个当前 Preview Session。基础编排中的合法非 Skill 内容由已有正式 Timeline owner 管理，不强制 Character Session。RuntimeDebug 连接实际游戏时只观察选定目标，不创建另一个隐藏 Session。多个作者页面、Timeline 页面、FlowCanvas 页面和 RuntimeDebug 页面只持有自己的视图绑定，不能拥有运行状态。

```text
一个 ScenePlay Session
├─ Scene / Actor
├─ Ability / RootTree
├─ Timeline playback instances
├─ Pose / Motion / Camera
├─ RuntimeDebug facts
└─ Workbench 页面绑定
```

切换页面只切换观察目标，不停止 Session；仍有预览承载窗口时关闭普通页面只释放本地 interest；关闭最后一个承载预览的窗口或明确结束 Preview 时，由隐藏宿主结束并释放 Session。编辑器重载、退出和转入 Unity Play 的宿主销毁边界见第 3.1 节。不能按 Timeline、Graph 或窗口创建第二个 Scene、Actor、Session、时钟或执行器。基础编排和 Ability Preview 的运行内容不同，但共享宿主与领域执行合同；目标不兼容时沿正式生命周期切换，不承诺旧 playback 跨目标存活。

多次调用同一 Timeline 必须用 playback identity、调用点和 generation 区分；多个 Actor 不能按名称合并。RuntimeDebug 的 Follow/Pin 只改变观察目标，不改变运行。

### 3.1 隐藏场景宿主与生命周期

已确认装配来源为正式 Prefab。Unity 2022 使用公开 NewPreviewScene 与 Prefab 实例化入口，不通过私有 OpenPreviewScene 反射，也不改用出现在 Hierarchy 的普通附加 Scene。Profile 不再保存 .unity 来源；正式游戏场景与预览共用装配 Prefab。

职责按资源和运行事实划分，不把预览生命周期放进 Timeline 或 Graph 页面：

| 责任方 | 输入 | 状态与输出 |
| --- | --- | --- |
| 编辑器预览宿主 | Profile、开始/结束请求、编辑器生命周期 | 创建隔离 Scene、装配正式 Session、连接驱动和视口、释放自己创建的资源 |
| GameplayTickSystem | 编辑器提供的帧间隔及正式 DriveCommand | 正式逻辑 Tick、表现帧、暂停/单步/倍速状态 |
| SimulationSessionHost 与领域 owner | 正式 Tick、Actor 输入、内容采用请求 | 准备与运行状态、节点调用、Timeline、Pose/Motion/Camera 输出及诊断事实 |
| Slate / FlowCanvas / 预览视口 | 正式状态、运行结果、用户交互 | 作者编辑、运行观察、输入请求与图像显示，不保存另一份业务运行状态 |

现有 GameplayTickSystem 已支持 Realtime、Paused、ManualStep、RatePlayback 和 ScriptedPresentationFrame。手动与连续推进都进入 AdvanceLogicTick；表现通过 FrameLateUpdate 推进。Preview 只接入编辑器驱动，不新增 Tick 算法。一个 Session 同时只有一个驱动来源，不能同时被 Editor update 和 Unity PlayerLoop 推进。暂停与单步要显式接入现有表现时钟策略，不能让表现继续按墙钟漂移。

编辑器可操作性是实施约束：暂停且没有新命令时不执行逻辑或表现求值，不因墙钟刷新运行投影；关闭预览时撤销驱动订阅。资源加载和绑定仅在正式准备边界执行，不放入 Inspector/重绘。开发验证采用短时开启、完成后释放，源码按完整批次编译，不在后台持续播放。后台失焦时的 Editor update 频率不能冒充用户交互帧率，响应性判断须区分运行负载、重绘与编译停顿。

编辑器性能属于 Workbench 完成条件，不以 Player 帧率替代。比较相同输入、目标和窗口布局下的关闭观察、静止预览、连续播放、调试跟随与技能切换，记录 Editor 主线程耗时、托管分配、输入和导航响应；编译与资源首次加载单独记录。观察层不能用持续降低刷新频率替代对重复求值、全量重建或无变化重绘的修复。当前目标是恢复正常编辑操作；未获得同条件证据前，不宣称已经解决个位数 FPS。另一任务拥有的 Profiler 录制和数据不得擅自停止、覆盖或清理。

以下是待对齐的宿主生命周期建议，Session 内部状态继续由现有正式合同拥有，不复制一套状态机：

```text
未创建 → 创建隐藏 Scene / 装配 → 正式 Prepare → 可交互
                                                 ↕
                                           连续播放 / 单步
任一已创建阶段 → 结束 / 准备失败 → 停止驱动并释放 → 未创建 / 失败原因
```

- 准备：打开预览区自动触发，由正式准备流程加载资源、锁定 roster、绑定各领域和输入端口。现有 Prepare 依赖 Tick 推进；不能因尚未点击播放而在准备完成前切断所有推进。就绪状态必须对应实际可运行的 Actor 和表现绑定。
- 播放、暂停、单步、倍速：提交既有 GameplayTickDriveCommand。单步明确是 Logic Tick，不把 Timeline 作者帧误当作 Logic Tick。
- 页面切换：Session 和隐藏 Scene 保持；Graph/Timeline 页面只更换本地绑定。仍有预览承载窗口时，普通作者页面关闭不销毁 Session；最后一个承载预览的窗口关闭则结束并释放预览，不留下后台运行。
- 暂停保留运行现场。Stop 是复位并保留场景还是结束并释放，尚未确认，不写成已定合同。建议区分停止运行与关闭预览，停止后保留场景供继续编辑；重新开始必须走正式复位或重建，不能只把 Timeline 时间归零就声称节点图已重置。
- 销毁顺序：建议先停止编辑器驱动和输入接入，撤销观察绑定，再沿现有 Session 的 Quiesce、资源释放与 Dispose 合同退出，最后释放预览视口资源和隐藏 Scene。不依赖 Edit Mode 中 MonoBehaviour 自动获得完整 Play 生命周期。
- 编辑器边界：建议程序集重载、编辑器退出、用户进入 Unity Play 前结束隐藏 Preview，避免旧回调和新 Runtime 同时推进。重载后保留 Profile 与作者视图，不冒充旧 Session 仍然存活。
- 输入与视口：预览交互经正式输入端口交给角色；编辑字段、拖动节点等作者手势不作为角色输入。视口消费正式角色表现和 Camera 输出；编辑观察相机不能修改正式相机运行事实。焦点与输入释放由宿主接入统一处理。

### 3.2 时间定位、历史查看与继续执行

打开效果预览区自动创建和准备，用户不需要 Start Session 或 Prepare；正常直接看到内容与操作入口，耗时或失败才提示原因。首次是否自动连续播放仍未确定。

基础编排中，拖动作者时间尺可请求正式内容 owner 定位并显示效果。Ability Preview 中，拨回执行游标查看所选位置的节点、变量和角色情况：节点与变量读取记录时事实；角色画面需要记录的正式表现状态，或由正式 Session 的 Restore/Replay 恢复到目标位置。UI 不能只跳高亮却把实时角色画面当成历史结果；缺少记录或恢复能力时显示缺失部分。Capture/History 的只读浏览本身不修改正在运行的 Session；需要恢复当前预览 Session 才能显示角色时，必须暂停并明确进入正式恢复流程，不暗中修改实际游戏目标。

运行区间按正式事件 Sequence 归并，逻辑 Tick 与表现 Frame 不按数值混排，也不合并为同一个开放区间。Lifecycle 记录使用正式诊断上下文的逻辑 Tick。Sequence 只表示记录顺序，不能替代时间尺坐标。

Timeline 局部时间结合 playback、cycle 和调用来源映射到正式 Session 时间。作者游标、实时执行头、历史观察位置与正式已恢复位置分别保存。回看较早位置不借用未来 Exit、Loop 次数、变量或当前作者图；回到实时位置继续读取最新事实。既有 TryRestoreToTick 与 TryReplayInputRange 是可复用入口，不代表历史角色定位已经实现。

“回到过去修改变量并从那里继续”会改变后续执行，不能作为普通历史查看的隐式副作用。新旧轨迹是否保留并列比较、继续运行时如何处理旧未来记录，尚未获确认；本轮不将历史分叉列为已确认实施任务。当前已确认的是运行过程可回看、可通过正式运行变量入口调整并观察后续变化。

## 4. 工具表面

工作台分为三个可停靠的工具面：原 FlowCanvas 负责节点图，原 TimelineEditorWindow/Slate 负责作者内容编排，独立 Preview 窗口负责角色画面、运行黑板与执行记录。Preview 复用同一个 Slate 执行投影，不复制作者 Timeline、业务执行器或 Renderer。打开 Preview 不要求先打开被调用的作者 Timeline。

```text
FlowCanvas：作者节点图 / 运行高亮
Timeline：作者 Track / Clip / Curve
Preview：
  顶部：Profile、运行控制与高级菜单
  上区：角色视口 | 当前实例黑板
  下区：实际执行时间线
  上下与上区左右分隔可拖动
```

打开 Preview 自动连接唯一隐藏宿主；重复打开聚焦既有窗口。关闭普通作者窗口不停止预览；关闭最后一个 Preview 承载窗口沿正式生命周期释放隐藏宿主。Unity Play 的实际游戏 Session 不受影响。ScenePlay 保留为正式运行底座的代码概念，常用 UI 不要求用户操作 Start ScenePlay。

常用内容更新自动组织 Export、Prepare、Publish、Adopt；高级菜单保留这些阶段及 Capture、History、Resume Live，便于观察失败和采用状态。图配置变化提供一次“应用并重新预览”；纯 Timeline 内容刷新不要求用户 Build Ability 图。Profile 仅选择装配配置资产，其字段仍在原 Inspector 编辑。

Authoring 的作者 Timeline 与 Preview 关联的作者节点图可以编辑；Ability 执行时间线和 RuntimeDebug 事实投影始终只读。可以复用 Slate 的时间尺、Track、Clip、缩放、滚动和绘制算法，但 Runtime projection 不得直接复用 Authoring projection 作为数据源。

不新增以下产品区域：

- 独立 Dashboard、事件中心和性能面板；
- 每个子图或 Timeline 的独立窗口；
- 第二套 Timeline Renderer、曲线编辑器或播放器；
- Runtime 资产保存页、运行状态 Inspector 副本；
- CMC 运行兼容入口、fallback Preview 或独立业务时钟；编辑器宿主向正式 Tick 提供帧间隔不属于第二时钟。

### 4.1 ScenePlay Profile

`BtsmtlScenePlayProfile` 是唯一预览入口配置，字段只包含：

- `AssemblyPrefab`：正式场景装配 Prefab，实例化到隐藏 Preview Scene；正式游戏场景复用该 Prefab，不复制角色与场景配置；
- `ContextId`：该场景中正式 `SimulationSessionCompositionDefinition.SessionId`；
- `DefaultActorId`：该 Session roster 中默认绑定的 Actor。

Profile 不保存 Session、Actor runtime identity、当前 revision、运行时间、暂停状态、Capture、History 或窗口选择。这些状态仍由 ScenePlay、正式 Actor host 和 RuntimeDebugSession 拥有。连接时先按 `ContextId` 找唯一正式 Session，再仅在该 Session 内找 `DefaultActorId`；缺失或不唯一都拒绝连接，不能退回全场景名称匹配。

Preview 顶部选择 Profile，原 Timeline 保留打开 Preview 与 RuntimeDebug 入口，共享同一配置和状态。AssemblyPrefab、Context 与 Actor 的详细编辑只出现在 Profile Inspector。Profile 无效时只显示一条错误和定位 Profile 的入口，不展开配置表单；当前窗口不重复显示 AssemblyPrefab、Context 或 Actor 字段。

## 5. Preview 更新边界

### 5.1 可以轻量采用的修改

参数、曲线、Clip 时间、窗口值和正式合同允许的内容变化，可以在同一 Session 内 Export 和 Prepare。Export 冻结作者闭包；Plan 记录当前 Timeline Host 会话标识与内容代次；Publish 和 Adopt 都重新核对作者/content revision。旧版本在准备和发布期间继续运行；新版本只能在正式安全边界采用，且只影响后续 playback。UI 显示 `作者已修改`、`已导出`、`已准备`、`待采用`、`已采用` 或 `已拒绝/失败`，不得把 Mutation 成功直接画成 Runtime 已生效。

### 5.2 需要明确边界的修改

节点/连接/Track/Clip 拓扑、Graph 依赖、状态布局、Composition、Scene、Actor roster、C# 代码和运行模块变化，不承诺当前活动实例原地无感替换。普通 Track/Clip identity 或编排结构变化由 Timeline owner 更新内容与播放绑定，不无条件编译图或新建 Session；编译依赖、状态布局或其它 Session 结构变化按各自正式合同重建。兼容的内容 revision 可以延迟到下一次调用或正式 adoption barrier；不兼容的版本必须显示需要重新发布、重建或新 Session。

不能出现一半旧调用栈、一半新 Graph、旧 Snapshot 对新 SourceMap 或旧 Playback 对新状态布局的混合状态。

### 5.3 首次进入成本

Preview 始终在 Edit Mode 内创建隐藏 Scene 并装配正式 Runtime，不自动或手动要求进入 Unity Play。打开预览区时自动完成场景、Session、资源与领域绑定；正常操作不要求作者感知准备阶段或点击 Prepare。确实耗时或失败时才显示真实等待与原因。正式 Session 在 Edit Mode 下运行不属于假 Runtime；使用另一套节点、Timeline 或动画求值替代正式管线才属于分裂实现。进入后，正常兼容作者修改应保持 Scene、Session、Actor 和 RuntimeDebug 绑定不变。

### 5.4 运行变量与作者资产分别提交

运行黑板调值是附加调试能力：编辑器定位当前黑板实例和稳定变量 ID，提交一次写入，在正式求值边界采用，后续节点读取新值。作用域与生命周期由原黑板 owner 处理；实例结束后解除绑定，旧请求不能落到新实例。沿用现有可写性、Config 只读及输入绑定规则，不新增逐变量“可调”勾选，也不建立永久覆盖层或另一套生命周期。写入仍经过原业务投射，UI 不直接改诊断 snapshot 或裸状态槽。记录实际采用值与位置；本次调参不附带新增历史分叉或调参重放协议。

作者默认值、节点配置与图连接走 Mutation/Undo 和图 Build；构建成功后由正式入口重建对应预览执行，不隐式保留不兼容的调用栈。运行黑板值不自动保存成作者默认值。

Float32 表现 Marker / TreeClip 的当前执行合同只允许单次求值内的宏参数，每次调用从默认状态开始，并禁止访问 Gameplay 黑板。它的节点、分支与参数属于只读执行观察，不接入角色黑板命令，不为外部调参新增持续变量生命周期。

Timeline 的时间、长度、曲线、资源引用及普通 Track/Clip 编排属于内容更新，不因发生修改就 Build Ability 图；增删 Track/Clip 由正式 Timeline owner 更新绑定和 playback 生命周期。TreeClip 内部图或其它编译依赖变化才 Build 受影响的图。纯 Timeline 编排更新后通过正式重置/定位显示当前游标效果；Ability 中的活动 playback 保持原内容，新调用采用新内容，需要立即观察完整效果时重新预览。当前代码的拓扑拒绝限制是待改实现，不能作为产品规则继续要求所有 Timeline 结构编辑重建整张图。

### 5.1 内容版本与来源映射

每个 Timeline playback 保持创建时正式准备的内容与 SourceMap。映射版本使用该 Timeline 的内容身份、作者指纹与依赖闭包版本；运行事件携带自身映射版本，不能统一套用角色最初装配时的来源表。RuntimeDebug 的实时观察、执行投影、来源跳转、Capture 与 History 必须按事件版本解析。新内容不能改写旧 playback 或历史的来源，Capture 导出需要包含记录涉及的映射目录。

映射首次建立属于内容准备工作；重复播放复用正式准备结果中的指纹与已有映射，不额外遍历作者 Timeline 计算哈希。来源映射就绪不等于动画资源已经就绪：新增动画 Track 的 producer 绑定、新动画资源与 TreeClip 编译调用绑定仍由各自正式 owner 准备，不以移除拓扑拒绝条件代替资源生命周期。

## 6. RuntimeDebug 的动态切换

RuntimeDebug 只显示当前真实执行焦点：

1. RootTree 当前节点变化，FlowCanvas 高亮对应节点或边；
2. 调用子图，导航路径增加一层并打开对应 Graph；
3. 调用 Timeline，Slate 显示该 playback 的真实 Track/Clip；
4. 进入 TreeClip，Slate 可导航到对应 TreeClip 子图；
5. Timeline 结束或返回，焦点回到父调用方；
6. Capture/History 时，显示历史事实，不拿当前作者数据重新求值。

RuntimeDebug 的时间、活跃集合、Clip 生长、退出原因和历史位置全部来自正式运行事实。开放时长 TreeClip 的可视 End 是 `实际退出 ?? Runtime 游标`；UI 不估算、不补长。

### 6.1 动态长度 TreeClip

逻辑域和表现域的 TreeClip 都可以声明 `TreeDecision`：作者设置开始时间，节点图通过正式“结束片段”节点决定本次实例的退出。节点图一次 Root 求值成功不等于请求退出。表现域保留明确的 `FrameBoundary` 固定区间模式；选择固定区间时按作者结束时间退出，不混用两种退出规则。

- Authoring 展示开始位置和结束来源。Logic 动态片段的 End 跟随 Timeline 终点；Presentation 动态片段的 End 是可编辑布局上界。两者都不把作者 End 当作实际退出时间，拖动布局边界也不会伪造运行长度。
- RuntimeDebug 按 playback、generation、cycle 和 Clip identity 区分调用。逻辑片段使用已提交逻辑时间，表现片段使用已接受表现时间。尚未退出时，可视 End 跟随对应域游标；退出后固定为该次实例的实际退出时间。
- 表现域 Root 只读取当前表现事实并输出表现请求。“结束片段”只请求结束当前表现 TreeClip，不调用逻辑域退出服务，不修改 Simulation state，不延长或提前结束父 Timeline。
- 表现域进入或更新期间提出的退出请求，必须在同一候选表现帧内执行 OnDisable、移除活跃片段并收回它的持续输出；只有整帧接受才保存退出状态和发布退出事实。整帧丢弃后，下一次候选仍从原接受状态开始。
- 暂停时不更新 Root，也不靠墙钟增加长度。正常父播放结束或离开当前 cycle 时收束子片段；停止、撤销和时间修正沿现有销毁规则处理旧实例。修正后的实例按修正位置重新建立，不能挪用旧实例的退出记录。
- Capture/History 只读取所选历史位置已经存在的事实。回到退出之前必须重新显示 open；不能借用未来退出时间，也不能把表现时间替换成逻辑时间。
- 运行投影只更新只读显示模型，不替换作者 TimelineData，也不把实际长度写回作者资产。同一作者 Clip 的不同播放可以具有不同长度。

这项能力只改变特定 TreeDecision TreeClip 的实际结束语义。Ability 执行时间线的持续增长是另一层投影能力，不改变 FrameBoundary 作者 Clip 的固定区间。两者均不增加窗口播放器、独立时钟或另一套图执行器；游标联动通过正式 owner 的定位请求完成。片段是否共享 Track 与片段如何决定退出分别由正式 Track 合同和 TreeClip 结束来源决定。

## 7. CMC 参考边界

采用仓库内 CwcMontage 的隐藏场景承载和编辑交互方式：

- 创建隔离预览 Scene 和视口，不进入 Unity Play；
- 打开时预热编辑面和表现资源；
- 编辑后快速同步时间线和局部显示缓存；
- 使用手动刷新降低作者等待；
- 把作者数据与当前显示对象分开。

CMC 使用编辑器自己的 PlayableGraph 和动作块 OnPreview 回调。Workbench 复用其承载与交互方式，不复制这套求值路径。节点图、Timeline、Pose、Motion、Camera 和 World 仍由唯一正式 Session 执行；编辑器宿主仅负责准备、帧驱动、输入和清理。

## 8. 关键不变量

- 作者资产修改走 Mutation/Undo；Preview 运行变量修改走正式运行命令；执行记录与 RuntimeDebug 观察均只读。
- 基础内容预览与 Ability Preview 复用正式领域执行；RuntimeDebug 只观察明确绑定的 Session，不为观察创建第二 Runtime。
- Slate 和 FlowCanvas 是工具表面，不是业务状态所有者。
- 正式 Session 拥有业务生命周期；编辑器宿主驱动既有 Tick 并拥有隐藏 Scene 资源，页面不直接执行业务。
- RuntimeDebug 只消费 SourceMap 和提交事实；不从作者资产猜运行结果。
- 作者版本、已采用版本、已导出/准备/发布版本、Session generation 和 RuntimeDebug target revision 必须分开显示。
- Preview 使用 CMC 式隐藏 Scene，不进入 Unity Play；CMC 动作块与播放器不进入正式执行链。

## 9. 现行规范差异与文档验证

本次更新描述目标设计，不表示代码已实现，不执行归档或将新能力标为完成。需由本 change 的 delta 统一替换：

- 现行 Timeline 规范中“编辑游标不得执行角色”收敛为“页面不得自行执行；内容效果预览可以提交正式定位请求”。
- 旧“Preview 只是作者 Timeline 加 overlay”改为完整 Ability 执行投影；禁止第二播放器不等于禁止执行记录时间线。
- 原固定三态必须从 Timeline 打开的限制放宽为 Ability 图入口与原 Timeline 工作面联动，不另开总控工作台。
- Graph Shell 的 Unity Play/Unity Stop 生命周期描述改为隐藏场景宿主，保留正式 owner 和页面 interest 边界。
- 运行变量命令与作者参数 Mutation 分开；未确认的历史分叉、Stop 行为和最终布局不写成已定实现。

文档验证采用 OpenSpec strict 与 diff 检查。后续用户可按以下场景查看实现结果：纯 Timeline 编排并拖动查看效果；打开 Ability 运行含决策、重复 Loop 与嵌套 Timeline 的图；回看迭代前后时刻核对节点、变量和角色；修改正式可调变量核对采用时刻和后续分支；确认普通 FrameBoundary Clip 未被动态延长。上述说明不作为 tasks 中的测试或验收任务，本轮不声称已执行运行验证。
