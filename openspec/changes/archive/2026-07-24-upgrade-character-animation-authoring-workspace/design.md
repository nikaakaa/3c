# Design: 角色动画作者工作区

## Context

当前共享Shell通过`IGraphAuthoringDocument`、Node Catalog、Port Policy、Mutation、Inspector和Diagnostics adapter隔离BTSMTL与Pose Graph领域。`BaseTreeWindow.uxml`仍只提供固定左栏和右栏；Pose Graph Inspector已经能编辑Selection、Parameter、Blend Policy、Inertialization、Bone Mask、Additive、Modify Bone、Foot Placement与Subgraph，Diagnostics adapter也能按PoseNodeId读取匹配ProjectionRevision的正式runtime snapshot。

因此缺口不是另一套Graph runtime，而是工作区的信息组织、可达producer解释、Preview控制和中间Pose观察。设计必须保留以下唯一真相：

- Graph mutation只进入真实domain owner。
- Animation producer、Marker、Clip和Curve只由Timeline/AnimationTrack拥有。
- Profile、Rig、Policy与Analysis Source只由各自正式Inspector拥有。
- Program、Projection、Foot Analysis和Motion Matching Database只由显式Build发布。
- Runtime/Preview只执行匹配revision的正式Pose Plan。

## Goals

- 让熟悉UE AnimGraph的作者能够用已有概念理解布局、Details、Preview、Sync Group与Pose Watch。
- 让作者在同一个窗口追踪`Animation Selection -> Player -> composition -> world-aware -> OutputPose`。
- 让BTSMTL Tree、AI Graph和Pose Graph复用同一工作区骨架，同时保持domain数据、mutation和compiler完全独立。
- 让所有跨资产信息只读投影并精确导航，不产生第二写入口。
- 所有重构只保留一条正式布局和一套view-state，不保留旧互斥页签模式。

## Non-Goals

- 不复制UE Animation Blueprint runtime、Event Graph、Anim State Machine、Montage、Slot或Post Process Anim Blueprint。
- 不让UI名称改变serialized identity、Projection schema或runtime operation code。
- 不把Preview或Pose Watch变成新的动画播放器、Pose缓存权威或Player发布路径。
- 不让打开窗口、选中资产、修改图或切换Preview目标触发重Build。

## Decision 1: 升级唯一共享Shell而不是创建动画专用窗口框架

共享Shell固定提供五个区域：

```text
Toolbar
Navigator | Graph Canvas | Details
          | Bottom Dock   |
```

区域只表达UI职责：

- `Toolbar`：导航、模式、显式命令和状态。
- `Navigator`：当前domain的图结构、正式数据目录或只读来源目录。
- `Graph Canvas`：唯一GraphView和breadcrumb。
- `Details`：当前selection的authoring、live与reference投影。
- `Bottom Dock`：可折叠的preview、diagnostics或domain工具。

Shell继续复用现有document、catalog、port、mutation、Inspector和diagnostics adapter。新增区域通过可选窄adapter提供内容；Shell不得按节点类型或domain名称构造面板。

### Tradeoff

- 优点：所有Graph作者工具保持同一交互规则，Pose Graph不形成第二套GraphView/Undo/selection。
- 代价：BTSMTL Tree与AI窗口也需要一次性迁移布局，不能只给Pose Graph打局部补丁。

## Decision 2: 使用左侧Navigator、中间Graph、右侧Details、底部结果

默认桌面布局：

```text
┌─────────────────────────────────────────────────────────────────────┐
│ Breadcrumb      Authoring/Live      Compile      Preview      Status │
├──────────────┬──────────────────────────────────┬───────────────────┤
│ Navigator    │ Graph Canvas                     │ Details           │
│              │                                  │ Authoring         │
│ Graphs       │ Selection -> Player -> Pose      │ Live              │
│ Data         │                  -> Output        │ References        │
│ Sources      │                                  │                   │
├──────────────┴────────────────────┬─────────────┴───────────────────┤
│ Preview / Pose Watch              │ Diagnostics / Sync              │
└───────────────────────────────────┴─────────────────────────────────┘
```

窄窗口允许折叠Navigator、Details或Bottom Dock，但折叠状态只保存为editor-only view-state。折叠不得改变Graph、Profile、Timeline或Projection revision。

旧Data/Inspector互斥页签直接删除。Data Catalog与Details可以同时可见，但两者职责不重叠：Navigator用于查找和引用，Details用于编辑当前selection。

### Tradeoff

- 优点：符合UE、Unity Graph Tools和常见DCC的方向习惯，Inspector获得足够宽度。
- 代价：现有BTSMTL用户需要适应Inspector从左侧移动到右侧；不提供旧布局开关，避免长期维护两套路径。

## Decision 3: 只借用语义准确的UE概念

UI采用以下对应：

| 项目语义 | UI术语 | UE对应说明 |
|---|---|---|
| Character Presentation Pose Graph | Anim Graph / Pose Graph | 窗口标题可显示Anim Graph，资产和技术详情保留Pose Graph |
| LayeredBoneBlend | Layered Blend Per Bone | 语义直接一致 |
| Inertialization | Inertialization | 语义直接一致 |
| Bone Mask | Blend Mask | Details可说明UE对应，但保留正式类型名 |
| MarkerGroup | Sync Group | 语义一致，Marker仍由AnimationTrack拥有 |
| ProgramParameterInput | Animation Parameter | 类似AnimBP变量/Property Access |
| PoseSubgraph | Anim Layer/Subgraph | 只在静态subgraph语义下说明对应 |
| OutputPose | Output Pose | 语义直接一致 |

以下名称不得直接替换：

- `AnimationChannel`不是UE Montage Slot；UI必须显示Animation Channel，可在帮助文字说明其承担类似命名表现入口的部分职责。
- `BTSMTL Timeline`不是UE Montage；不得改名或伪造Section/Slot运行语义。
- `WorldAwarePostProcess`是主Pose Plan阶段，不是独立UE Post Process Anim Blueprint。
- Gameplay StateMachine不是UE Animation State Machine；Pose Graph Navigator不得显示不存在的Anim State Machine目录。

serialized node kind、port kind、PoseNodeId与compiler operation不因UI名称改变。

## Decision 4: Navigator只投影正式来源

Pose Graph Navigator包含：

- 当前root graph和shared/inline subgraph。
- Pose Graph声明的Animation Selection Input与Program Parameter。
- 显式Definition上下文下可达的AnimationChannel和producer。
- 当前图引用的Rig、Mask、Blend Policy、Inertialization Policy、Foot Placement配置的只读定位入口。

producer目录必须调用唯一`CharacterAnimationPresentationAuthoringService`，从精确`CharacterPipelineDefinition`的composition roots递归发现Timeline和AnimationTrack stable identity。不得：

- 扫描Assets目录。
- 反读generated Program或Projection来bootstrap。
- 按显示名、文件夹、列表index或旧PoseSlot猜测binding。
- 把producer flow、Marker或Profile字段复制到Pose Graph资产。

producer条目只提供查看、筛选、定位Timeline/Track/Clip和显示可达关系。它不通过拖拽直接修改Timeline或创建隐式Pose节点。

## Decision 5: Details分离Authoring、Live和References

### Authoring

只显示当前Pose节点正式拥有的字段，并通过现有Pose mutation adapter写入真实Pose Graph owner。字段按节点类型组织，不再显示无关serialized默认字段。

### Live

只读取与当前PoseGraphId、PoseGraphRevision和ProjectionRevision完全匹配的`AnimationPresentationRuntimeSnapshot`。显示availability、source、weight、raw/effective time、Marker segment、Player usage、discontinuity、阶段completion和invalid reason。缺少匹配target时显示Unavailable或Stale。

### References

只读显示node source map、call site、reachable producer、Profile/Rig/Policy owner和精确导航操作。导航到Timeline后，Marker与Clip仍由Timeline Inspector唯一修改。

Details不得重新执行validator之外的runtime算法，不得从Animancer state、scene Transform或authoring object推断Live值。

## Decision 6: Preview必须显式启动并执行正式Pose Plan

Pose Preview有两类显式上下文：

- `Authoring Preview`：作者明确选择CharacterPipelineDefinition和合法Preview Rig/Target，使用已发布且revision匹配的Projection与Pose Plan。
- `Live Target`：作者通过共享RuntimeDebugSession明确选择场景runtime target，只观察该target已经发布的结果。

只有点击Play、Pause、Step或Seek才推进Authoring Preview。以下事件均不得自动执行Preview或Build：

- 选择资产或节点。
- Inspector focus变化。
- Graph mutation。
- 窗口创建、恢复或domain reload。
- AssetDatabase import/refresh。
- Preview target下拉变化。

Graph或依赖修改后，Preview立即进入Stale；作者必须显式Compile/Build后才能继续。系统不临时编译、不读取旧Projection、不创建默认角色或场景fallback。

## Decision 7: Pose Watch是有界只读诊断兴趣

Pose Watch selection以`PoseGraphId + PoseGraphRevision + PoseNodeId + call-site`标识，保存在Editor view-state，不进入Pose Graph或Projection。

Editor向Preview或RuntimeDebugSession注册显式watch interest。正式Pose Plan完成后，diagnostics publisher从已经完成的对应Pose Value workspace复制所需骨骼Pose和contribution；不得：

- 重新求值节点。
- 第二次采样Animation source。
- 改变Player、Blend Stack或Inertialization history。
- 改变FinalAnimationPoseFrame。
- 无界保留历史帧。

每个窗口拥有自己的watch集合；共享runtime provider合并interest并按固定容量发布。停止观察或关闭窗口时释放interest。

## Decision 8: Bottom Dock只承载结果与精确导航

Pose Graph Bottom Dock提供：

- `Preview`：角色、骨骼、Root轨迹、Foot Placement goal和阶段状态。
- `Pose Watch`：多个中间Pose的颜色、显隐和目标节点。
- `Diagnostics`：validation、compile、runtime和source-map错误。
- `Sync`：当前source与handoff source的只读Marker时间尺、raw/effective time和segment fraction。

Sync面板不允许拖动Marker。`Open Source Timeline`必须使用stable Timeline/Track identity打开唯一Timeline Editor并选择精确对象。

## Decision 9: Compile和Build必须是明确重操作

Toolbar的`Compile`或`Build`只由明确按钮、菜单命令或既有显式Agent apply事务触发。轻量Graph shape validation可以在authoring mutation后刷新，但不得发布Program、Projection、Foot Analysis artifact或Motion Matching Database。

按钮状态必须区分：

- `Dirty`：authoring有未保存变化。
- `Invalid`：轻量validator失败。
- `Stale`：已发布Projection与当前authoring revision不匹配。
- `Ready`：当前已发布产物匹配。
- `Building`：仅在显式命令执行期间。

selection、Inspector、Preview和diagnostics不得把`Stale`自动修复为`Ready`。

## Decision 10: Agent authoring合同保持不变

本change只改变Editor workspace、editor-only view-state和只读diagnostics interest，不增加Pose Graph、Profile、Rig、Policy或Timeline的新mutation语义。Agent Document/Snapshot可以继续只读输出既有Presentation上下文，Patch/MCP不得获得工作区布局、Pose Watch、Preview或Details写入口。

实施时仍需扫描Agent Snapshot、schema、validator和MCP bridge，确认没有代码通过旧窗口类型或左侧页签定位authoring语义；若只是UI宿主引用，迁移到共享Shell合同，不提升Agent schema。

## Migration

1. 先完成依赖active change的显式Pose Plan与最终runtime snapshot布局。
2. 为共享Shell增加通用region与editor-only layout state。
3. 原子迁移BTSMTL Tree、AI Graph和Pose Graph到新region装配。
4. 删除旧左栏Data/Inspector页签、旧固定宽度UXML和重复selection projection。
5. 接入Pose Graph Navigator、Details和只读References。
6. 接入显式Preview与Pose Watch interest。
7. 接入Bottom Dock和精确Timeline导航。
8. 更新current specs、项目文档和Agent影响说明。

迁移期间不得同时保留旧窗口模式或Pose Graph专用第二Shell。

## Risks

- 共享Shell迁移会同时影响Tree、AI和Pose Graph。通过domain adapters保持内容独立，并一次性删除旧装配降低长期分裂风险。
- Pose Watch可能增加Editor/Development诊断复制成本。通过显式interest、固定节点/骨骼/历史容量和关闭即释放限制成本。
- active animation changes仍在修改节点和snapshot。通过明确依赖最终Pose Plan，不为临时PoseSlot、旧Stack或图外FootPlacement建立UI合同。
- UE术语可能造成错误等价。通过术语映射表和禁止伪称规则，只在语义一致时使用公开名称。

## Rejected Alternatives

### 只把现有左侧Inspector加宽

改动较小，但仍无法同时展示Graph结构、Data Catalog、Details和Preview，也继续违背UE常见心智。拒绝。

### 为Pose Graph复制一套UE式窗口

能够快速获得动画专用布局，但会复制GraphView、selection、Undo、Inspector和diagnostics，形成第二套作者链。拒绝。

### 每个Timeline producer都成为Pose Graph节点

来源可见性最高，但会复制producer flow并把Gameplay选择与表现组合混成两份真相。拒绝；使用只读Definition-scoped source目录和Selection Input解释边界。

### 在Pose Graph Details直接编辑Marker和Clip

操作方便，但会建立Timeline authoring的第二写入口。拒绝；只读显示并精确导航到Timeline Editor。

### 资产选中或Graph修改后自动Build

能减少按钮操作，但Program、Projection、Foot Analysis和MM Database构建会阻塞Editor并掩盖Stale边界。拒绝。

