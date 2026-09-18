# 设计：BTSMTL Authoring Runtime Workbench

## 1. 产品模型

Authoring Runtime Workbench 是作者和运行观察共用的工作台。Preview 只是其中一种产品形态。工作台不拥有新的业务 Runtime；它只把正式作者入口、ScenePlay 运行和 RuntimeDebug 观察组织到同一工作面。

```text
Authoring Runtime Workbench
├─ Authoring
│  ├─ FlowCanvas / RootTree / 子图
│  └─ Slate Timeline / Track / Clip / Curve
├─ Preview
│  └─ 可编辑作者内容 + 真实 ScenePlay 运行结果
└─ RuntimeDebug
   └─ 只读调用栈 + FlowCanvas / Slate 动态观察

Workbench 唯一运行底座：ScenePlay Session
```

ScenePlay 是 Preview 的运行实现和生命周期 owner，不是用户需要另外学习的第四种产品形态。用户可以在工作台中进入 Preview；是否由 Unity Play Mode、编辑器预热宿主或其它正式承载方式启动，不改变产品语义。

## 2. 三种产品形态

### 2.1 Authoring

Authoring 面直接操作正式作者数据：

- FlowCanvas Graph、RootTree、嵌套子图、StateMachine 和 Graph 连接；
- Timeline、Track、Clip、TreeClip、Section、曲线和资源引用；
- 领域正式参数、作者版本和 Undo。

Authoring 显示完整作者内容，可以写入；它不创建 ScenePlay Session，不推进业务时间，不读取 runtime clone，也不把运行状态写回节点或 Clip。

作者在 Authoring 面关心的是“我设计了什么”。作者游标属于编辑定位，不是 Runtime 游标。

### 2.2 Preview

Preview 是在真实 ScenePlay Session 中查看作者修改结果的工作形态。它必须使用正式 Scene、Actor、Ability、RootTree、Timeline、Pose、Motion、Camera、World 和输入链；不使用 Timeline 私有播放器、CMC MontagePlayer 或 Pose fixture。

Preview 允许作者继续编辑正式数据。修改流程为：

```text
作者 Mutation / Undo
→ 导出作者闭包
→ 后台 Prepare / Build
→ 发布新的内容 revision
→ 正式 adoption barrier
→ 同一 Session 继续运行并采用
```

Preview 的目标是“在真实场景里改了之后会怎样”。因此 UI 必须同时让作者知道：

- 当前 ScenePlay Session、Actor、Ability 和调用目标；
- 作者版本与当前运行采用版本；
- 修改是否只影响下一帧、下一次激活、下一次调用，还是必须重新发布；
- 运行继续使用旧版本时，旧版本和新版本分别是什么。

### 2.3 RuntimeDebug

RuntimeDebug 是同一 Session 的只读运行观察形态。它只读取正式 `RuntimeDebugSession`、SourceMap、Trace、Playback、Snapshot、Capture/History 和领域提交事实。

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

## 3. Session 与工作台关系

一个 Workbench 对应一个当前 Preview Session。多个作者页面、Timeline 页面、FlowCanvas 页面和 RuntimeDebug 页面只持有自己的视图绑定，不能拥有运行状态。

```text
一个 ScenePlay Session
├─ Scene / Actor
├─ Ability / RootTree
├─ Timeline playback instances
├─ Pose / Motion / Camera
├─ RuntimeDebug facts
└─ Workbench 页面绑定
```

切换页面只切换观察目标，不停止 Session；关闭页面只释放本地 interest；明确结束 Preview 才结束 Session。不能按 Timeline、Graph 或窗口创建第二个 Scene、Actor、Session、时钟或执行器。

多次调用同一 Timeline 必须用 playback identity、调用点和 generation 区分；多个 Actor 不能按名称合并。RuntimeDebug 的 Follow/Pin 只改变观察目标，不改变运行。

## 4. 工具表面

工作台只提供能回答当前任务的最小工具：

```text
顶部：Session / Actor / Ability / 当前采用版本 / 准备状态
主区：当前 Authoring、Preview 或 RuntimeDebug 表面
导航：当前 Graph / 子图 / Timeline / TreeClip 调用路径
底部：必要的采用结果、失败原因和当前选中对象详情
```

Authoring 和 Preview 可以使用 FlowCanvas 与 Slate 的可编辑表面；RuntimeDebug 复用同一视觉表面但切换为只读 projection。可以复用 Slate 的时间尺、Track、Clip、缩放、滚动和绘制算法，但 Runtime projection 不得直接复用 Authoring projection 作为数据源。

不新增以下产品区域：

- 独立 Dashboard、事件中心和性能面板；
- 每个子图或 Timeline 的独立窗口；
- 第二套 Timeline Renderer、曲线编辑器或播放器；
- Runtime 资产保存页、运行状态 Inspector 副本；
- CMC 兼容入口、fallback Preview 或本地隐藏时钟。

## 5. Preview 更新边界

### 5.1 可以轻量采用的修改

参数、曲线、Clip 时间、窗口值和正式合同允许的内容变化，可以在同一 Session 内后台导出和准备。旧版本在准备期间继续运行；新版本只能在正式安全边界采用。UI 显示 `作者已修改`、`准备中`、`待采用`、`已采用` 或 `应用失败`，不得把 Mutation 成功直接画成 Runtime 已生效。

### 5.2 需要明确边界的修改

节点/连接/Track/Clip 拓扑、Graph 依赖、状态布局、Composition、Scene、Actor roster、C# 代码和运行模块变化，不承诺当前活动实例原地无感替换。兼容的内容 revision 可以延迟到下一次调用或正式 adoption barrier；不兼容的版本必须显示需要重新发布、重建或新 Session。

不能出现一半旧调用栈、一半新 Graph、旧 Snapshot 对新 SourceMap 或旧 Playback 对新状态布局的混合状态。

### 5.3 首次进入成本

产品入口不要求用户手动点击 Unity Play 按钮。首次准备可能产生场景、Session、资源或领域绑定等待，必须显示真实阶段和失败原因；不能为了“无感”创建 Edit Mode 假 Runtime。进入后，正常作者修改应保持 Scene、Session、Actor 和 RuntimeDebug 绑定不变。

## 6. RuntimeDebug 的动态切换

RuntimeDebug 只显示当前真实执行焦点：

1. RootTree 当前节点变化，FlowCanvas 高亮对应节点或边；
2. 调用子图，导航路径增加一层并打开对应 Graph；
3. 调用 Timeline，Slate 显示该 playback 的真实 Track/Clip；
4. 进入 TreeClip，Slate 可导航到对应 TreeClip 子图；
5. Timeline 结束或返回，焦点回到父调用方；
6. Capture/History 时，显示历史事实，不拿当前作者数据重新求值。

RuntimeDebug 的时间、活跃集合、Clip 生长、退出原因和历史位置全部来自正式运行事实。开放时长 TreeClip 的可视 End 是 `实际退出 ?? Runtime 游标`；UI 不估算、不补长。

## 7. CMC 参考边界

仓库内 CwcMontage 的 Editor UI 和 MontagePlayer 可作为交互体验参考：

- 打开时预热编辑面和表现资源；
- 编辑后快速同步时间线和局部显示缓存；
- 使用手动刷新降低作者等待；
- 把作者数据与当前显示对象分开。

但 CwcMontage 的独立 PlayableGraph、MontagePlayer、动作块生命周期和局部时钟不能成为 BTSMTL Preview 的运行真相。BTSMTL Preview 必须继续由唯一 ScenePlay Session 驱动正式 Graph、Timeline、Pose、Motion、Camera 和 World。

## 8. 关键不变量

- Authoring 可以写；RuntimeDebug 不能写；Preview 的写入必须走正式作者 Mutation。
- Preview 与 RuntimeDebug 使用同一 ScenePlay Session；不创建第二 Runtime。
- Slate 和 FlowCanvas 是工具表面，不是业务状态所有者。
- ScenePlay 拥有运行生命周期；Workbench 不推进业务帧。
- RuntimeDebug 只消费 SourceMap 和提交事实；不从作者资产猜运行结果。
- 当前运行版本、作者版本和待采用版本必须分开显示。
- CMC 只能提供体验参考，不能形成平行执行路径。
