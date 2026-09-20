## MODIFIED Requirements

### Requirement: ScenePlay 必须拥有预览生命周期

ScenePlay 的正式 Session、Actor 和 RuntimeDebug owner MUST 统一拥有 Start、Pause、Resume、Stop、Ability 输入、Live Debug、Capture 和 History。Authoring、Preview、RuntimeDebug 的入口 MUST 位于原 TimelineEditorWindow；Session 菜单只提交正式请求。Profile MUST 先精确匹配 Scene，再以 ContextId 匹配 Composition SessionId，最后在该 Session 内匹配 DefaultActorId；缺少或重复目标 MUST 明确拒绝。窗口 MUST NOT 因其它场景恰好具有相同 ContextId 而接管它。

#### Scenario: 从编辑器开始一次 Ability 预览

- **WHEN** 作者在原 Timeline 窗口选择有效 Profile 并进入 Preview
- **THEN** ScenePlay MUST 加载声明的正式场景并创建或连接唯一 Session/Actor
- **AND** Ability MUST 通过正式输入和请求入口启动；窗口不得直接创建 Timeline playback
- **AND** 未运行、准备中、准备失败、目标缺失和已连接 MUST 根据正式状态区分，不以点击成功冒充准备完成

#### Scenario: 暂停与停止

- **WHEN** 作者点击 Pause、Resume 或 Stop
- **THEN** 请求 MUST 作用于精确解析的正式 Session
- **AND** 只有由本入口启动且请求身份匹配的 Play 才能随 Stop 退出，不得因目标缺失而停止无关 Play
- **AND** Capture、History 和 Resume Live MUST 只调用 RuntimeDebugSession，不创建恢复或回放实现

### Requirement: Timeline UI 不得拥有运行时执行状态

TimelineEditorWindow MUST 不拥有 evaluator、独立时钟、playback command source、TimelinePreviewSession、AnimationPreviewRuntime、Preview Player、隐藏 Action runtime 或独立 PlayableGraph。窗口本地只保存 authoring selection、view state、观察绑定和显示过滤。

#### Scenario: 打开同一 Timeline

- **WHEN** 作者切换页面或重复打开同一 TimelineData
- **THEN** 页面 MUST 复用原 Timeline 面板，不按调用创建窗口
- **AND** TimelineData MUST 不保存窗口时间、目标、generation、播放状态或 GUI 游标
- **AND** 关闭最后一个 Timeline 窗口 MUST 释放该工具面的运行观察 interest，正式 Session MUST 继续运行

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

三种形态的入口 MUST 统一位于原 TimelineEditorWindow，Timeline 面板 MUST 始终使用原 Slate。RuntimeDebug MUST 根据正式调用关系导航已有 FlowCanvas 与原 Timeline 面板；窗口只改变观察绑定，不能按调用创建新面板、替换 ScenePlay Session 或把 FlowCanvas 嵌入 Slate。

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

Timeline MUST 保留编辑游标、整数帧输入和逐帧操作；编辑游标、真实运行标记和 Capture 历史位置 MUST 分别保存。已明确的 Profile、三态切换和 Session 菜单 MUST 通过正式 ScenePlay owner 操作运行。普通来源导航 MUST NOT 启动或重建 Session。嵌入按钮、快捷键、EditorUpdate、初始化/释放、保存和 delayCall MUST NOT 调用 Slate Play/Sample/ReSample/Stop 执行预览，MUST 清理 AutoKey 与临时作者播放器。

#### Scenario: 没有运行绑定

- **WHEN** 作者独立打开 shared Timeline
- **THEN** 轨道、Clip、曲线和编辑帧 MUST 完整可编辑
- **AND** MUST NOT 创建本地播放器或猜测角色目标，不把游标移动显示成角色已运行

#### Scenario: 运行时继续编辑

- **WHEN** 作者在 Preview 中编辑正式 Timeline
- **THEN** 作者数据 MUST 经同一 Mutation/Undo 修改，运行标记 MUST 只读消费真实绑定
- **AND** 新版本是否采用 MUST 由正式内容 owner 的实际报告决定；没有实际目标、版本未知或准备失败时 MUST NOT 显示已采用
