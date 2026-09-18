## ADDED Requirements

### Requirement: Authoring Runtime Workbench 必须明确三种产品形态

系统 MUST 将 Authoring、Preview 和 RuntimeDebug 定义为同一工作台的三种产品形态。Authoring MUST 负责正式作者数据编辑；Preview MUST 由唯一正式 ScenePlay Session 驱动并允许作者修改后重新导出和采用；RuntimeDebug MUST 只读观察同一 Session 的真实运行事实。ScenePlay MUST 是 Preview 的运行底座，不得被定义为第四种产品形态。

#### Scenario: 作者编辑正式内容

- **WHEN** 用户处于 Authoring
- **THEN** 用户 MUST 能编辑正式 FlowCanvas、RootTree、子图、Timeline、Track、Clip、曲线和正式参数
- **AND** Authoring MUST 不创建第二个 Runtime、私有时钟或运行时 Graph clone

#### Scenario: 在真实场景中预览作者修改

- **WHEN** 用户处于 Preview
- **THEN** Preview MUST 连接正式 ScenePlay Session、Scene、Actor、Ability、RootTree、Timeline、Pose、Motion、Camera 和 World
- **AND** 作者修改 MUST 通过正式 Mutation、Export、Prepare 和 Adopt 进入运行
- **AND** Preview MUST 不使用独立 Timeline 播放器、CMC MontagePlayer 或 Pose fixture 代替正式运行链

#### Scenario: 只读观察运行事实

- **WHEN** 用户处于 RuntimeDebug
- **THEN** RuntimeDebug MUST 只读取 RuntimeDebugSession、SourceMap、Trace、Playback、Snapshot、Capture/History 和领域提交事实
- **AND** RuntimeDebug MUST 不修改作者数据、不创建运行实例、不重算 Graph/Timeline/Pose

### Requirement: Authoring Runtime Workbench 必须只有一个正式 ScenePlay Session

一个 Workbench MUST 绑定一个当前 Preview Session。页面、Graph、Timeline、FlowCanvas、Slate 和 RuntimeDebug MUST 只拥有本地视图绑定，不得按 Timeline、Graph、Actor 或窗口创建第二个 Scene、Actor、Session、时钟或执行器。切换页面或关闭页面 MUST 不停止 Session；只有明确结束 Preview 才能结束 Session。

#### Scenario: 切换 Authoring、Preview 和 RuntimeDebug

- **WHEN** 用户在三种形态之间切换
- **THEN** Session、Scene、Actor、Playback 和运行版本 MUST 保持不变
- **AND** 切换 MUST 只改变当前工具表面和本地观察 interest

#### Scenario: 多个 Timeline 调用

- **WHEN** 同一 Session 多次调用同一 Timeline
- **THEN** Runtime MUST 通过 playback identity、调用点和 generation 区分实例
- **AND** UI MUST 不按名称合并不同调用或自动选择未声明的实例

### Requirement: Preview 作者修改必须经过发布与采用边界

Preview 中的作者修改 MUST 先写入正式作者数据，再通过 Export、Prepare、发布 revision 和正式 adoption barrier 进入 Session。旧 revision 在新 revision 准备期间 MUST 继续运行；UI MUST 分别显示作者版本、运行采用版本、准备状态和失败原因。Mutation 成功 MUST NOT 被解释为 Runtime 已生效。

#### Scenario: 兼容内容修改

- **WHEN** 作者修改参数、曲线、Clip 时间或正式合同允许的内容
- **THEN** 当前 Scene 和 Session MUST 保持运行
- **AND** 新 revision MUST 在正式安全边界采用
- **AND** Preview MUST 显示 `作者已修改`、`准备中`、`待采用` 或 `已采用` 的真实状态

#### Scenario: 不兼容结构修改

- **WHEN** 作者修改状态布局、Composition、Scene、Actor roster、C# 代码或不兼容运行拓扑
- **THEN** 系统 MUST 明确显示需要重建、重新发布或新 Session
- **AND** MUST NOT 把新旧调用栈、Snapshot、SourceMap 或状态布局混合到同一个活动实例

### Requirement: RuntimeDebug 必须按真实调用栈切换 FlowCanvas 与 Slate

RuntimeDebug MUST 以正式调用栈为导航主线。RootTree 或子图执行时 MUST 显示 source-mapped FlowCanvas 只读状态；Timeline 执行时 MUST 显示对应 playback 的 Slate Timeline 只读状态；TreeClip 进入子图时 MUST 能导航到对应子图；调用返回时 MUST 回到父调用方。RuntimeDebug MUST 不为每个 Graph 或 Timeline 创建窗口。

#### Scenario: RootTree 调用子图和 Timeline

- **WHEN** 运行从 RootTree 进入子图、Timeline 和 TreeClip
- **THEN** RuntimeDebug MUST 按实际调用顺序更新路径
- **AND** 当前表面 MUST 在 FlowCanvas 与 Slate 之间切换
- **AND** 未执行的 Graph、Track 或 Clip MUST 不被当作本次运行内容提前显示

#### Scenario: 观察历史运行

- **WHEN** 用户查看 Capture 或 History
- **THEN** RuntimeDebug MUST 显示记录时的 SourceMap、调用 identity、时间和提交事实
- **AND** MUST 不使用当前作者数据重新求值历史结果

### Requirement: Slate 和 FlowCanvas 必须只是工具表面

Workbench MAY 复用 Slate 的时间尺、Track、Clip、缩放、滚动和绘制能力，以及 FlowCanvas 的 Graph 画布和导航能力；但这些表面 MUST 不拥有业务时间、Runtime 状态、作者第二数据源或独立执行器。Authoring projection、Preview projection 和 RuntimeDebug projection 的数据来源 MUST 分开。

#### Scenario: Slate 显示 Runtime Timeline

- **WHEN** RuntimeDebug 聚焦 Timeline
- **THEN** Slate MUST 使用 Runtime observation projection 显示真实 playback、Track、Clip、游标和生命周期
- **AND** Slate MUST NOT 通过完整作者 TimelineData 补齐未执行内容或自行估算时间

### Requirement: 首次进入 Preview 必须显示真实准备阶段

Workbench MUST 不要求用户手动操作 Unity Play 按钮；首次进入 Preview 可以存在场景、Session、资源和领域 Prepare 等待，但 MUST 显示真实阶段、失败原因和采用状态。系统 MUST NOT 为了隐藏等待创建 Edit Mode 假 Runtime 或 CMC 平行播放器。进入 Preview 后，正常作者修改 MUST 尽量保持 Scene、Session、Actor 和 RuntimeDebug 绑定不变。

#### Scenario: Preview 首次准备

- **WHEN** 用户从 Authoring 进入 Preview
- **THEN** 系统 MUST 创建或连接正式 ScenePlay Session，并显示准备阶段
- **AND** 准备完成前 MUST 不显示伪造的运行结果

### Requirement: CMC 只能作为交互参考

系统 MAY 参考 CwcMontage 的预热、手动刷新和局部缓存重建方式，但 BTSMTL Preview MUST NOT 使用 CwcMontage 的独立 PlayableGraph、MontagePlayer、动作块生命周期或局部时钟作为运行真相。正式结果 MUST 由唯一 ScenePlay Session 发布。

#### Scenario: CMC 编辑体验参考

- **WHEN** 设计 Preview 的编辑响应和局部刷新
- **THEN** 允许复用 CMC 的交互经验
- **AND** 不得产生第二套 Graph、Timeline、Pose 或 RuntimeDebug 执行路径
