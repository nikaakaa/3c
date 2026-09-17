## Context

修订 v4，2026-09-13。协调提案 `camera-preview-timeline-domain-runtime-r1` 的规划更新已完成，用户随后明确“更新完了让实现窗口继续做吧”，现已确认按本 v4 继续原任务。承接 [领域运行时基线](D:/Unity_Project_1/3C/openspec/changes/replace-character-program-with-domain-runtimes/design.md) D1/D5/D6/D8：删除角色总 Program 与整包 Projection、技能独立编译、控制直接 C#、Pose 原生 FlowCanvas Runtime；网络 Pipeline/Pass、Float32/Fixed 和独立资源处理保留。规划窗口仅更新授权记录和发送文档指针，代码实施仍归原实现窗口，写入分工不扩大。

本 change 的 proposal 定义范围，本文定义职责和设计，tasks 只列剩余工作。当前源码状态与动态证据边界见 evidence/current-implementation.md。独立的 docs/character-camera-plan-2026-09-13.md 已合并删除，不维护第二份规划。旧协调文档未在本次修改，其 9 月 6 日版本/接线状态不能直接作为今天的事实。

当前配套规划窗口为 01a098bd-a5c1-7283-8aab-59736bab97f5，实现窗口为 01a098bd-bbdf-7d50-b1cc-95339d3bbf8d。v3 在提交 b2c2baa8f 下发过实施授权；用户现已授权通过一次 DOCUMENT_UPDATED 将同一实现任务接续到 v4，不创建新窗口或重做已完成部分。implementation.md 的旧 Projection/旧域 DLL/构建失败属于真实迁移现场，原记录保留，不将其当成等待恢复全量 Build 的前置条件。

授权包含补齐来源消费者和真实资源接线，不包含放弃来源还原、猜测缺失公式或覆盖已有正确代码。当前产品方向为玩家控制的第三人称环绕相机，默认轨道提供基础构图，动作、锁定和碰撞共用同一求解链。未决项先按来源取证与现行合同解决；无法同时满足真实代码、来源和规划时，在 implementation.md 写明冲突与业务取舍，再发送一次 ACTUAL_CONFLICT。

```text
PLANNING_REVISION
planner_thread_id: 01a098bd-a5c1-7283-8aab-59736bab97f5
implementation_thread_id: 01a098bd-bbdf-7d50-b1cc-95339d3bbf8d
planning_document_paths:
  - D:/Unity_Project_1/3C/openspec/changes/rebuild-character-camera-from-zzz/proposal.md
  - D:/Unity_Project_1/3C/openspec/changes/rebuild-character-camera-from-zzz/design.md
  - D:/Unity_Project_1/3C/openspec/changes/rebuild-character-camera-from-zzz/tasks.md
  - D:/Unity_Project_1/3C/openspec/changes/rebuild-character-camera-from-zzz/specs/
  - D:/Unity_Project_1/3C/openspec/changes/rebuild-character-camera-from-zzz/evidence/
implementation_document_path: D:/Unity_Project_1/3C/openspec/changes/rebuild-character-camera-from-zzz/implementation.md
revision: v4
coordination_proposal: camera-preview-timeline-domain-runtime-r1
action: CONTINUE_IMPLEMENTATION
confirmed_by_user: true
confirmed_revision: v4
implementation_dispatch: DOCUMENT_UPDATED
previous_implementation_revision: v3
```

## Goals / Non-Goals

**Goals:**

- 玩家能用鼠标稳定观察角色、按采样的相机方向移动，基础构图与动作演出共用一条求解链。
- 原 change 纳入的单角色镜头能力都有来源、正式输入输出、资源、消费者与退出行为；没有实现的保持未完成。
- 作者填写的每个字段有明确单位和消费者，改配置能追踪到最终镜头结果。
- 环境约束、镜头效果和平台输出职责分开；删除废弃路径时同步迁移全部真实调用者。

**Non-Goals:**

不做换人、网络相机状态、独立预览/Build、自动材质淡出或无需求的选敌系统；不改现有正确 Body/动画逻辑；不新增测试代码和人工验证任务；不恢复 Agent Document 或旧 C# 之外的作者写入协议。

## Decisions

### 1. 保留现有链路，补齐具体缺口

保留的运行调用次序如下。装配输入改为领域资源和只读运行绑定，不修改这条链的算法及时序；下列类型沿当前源码定位，不表示需要恢复其旧总包构造入口。

```text
InputProfile / InputActions
→ Session.BeginRenderFrame
→ Registration / ControlSource.CaptureRenderFrame
→ 本渲染帧 Look + 采样时的 CameraBasis

正式 Graph 内 TreeClip / 相机特殊 Node → 已提交 PresentationCommand
同帧 Body visible pose + 最终动画提交
→ CharacterSimulationPresentationRuntime.CompletePresentationFrame
→ 内部 CharacterCameraPresentationRuntime.Present
→ Sequence / Response / Target 裁决
→ FramePlanner（基础轨道 + 鼠标偏移）
→ SequenceTransition（切镜）
→ WorldBasicHistory（连续平滑）
→ Effect owners
→ 拟补齐的环境约束
→ ICameraRigAdapter / CinemachineCameraRigAdapter
→ 实际画面、CameraRigResult、CameraBasisSnapshot
```

移动/动作使用采样时固化的 Basis；本帧镜头产生下一份可采样 Basis。不能为降低延迟让 logic 随时读取活动相机，也不能在同一渲染帧多个 logic tick 中重复累加鼠标位移。

默认跟随使用 Body visible pose 加初始绑定偏移；最终 Pose 后调用相机不代表默认跟随每帧读取最终骨骼。只有显式声明的骨骼/目标绑定消费对应 Transform。

### 2. 模块职责与公共合同

| 模块 | 输入 | 输出 | 边界 |
|---|---|---|---|
| ControlSource | 配置、设备输入、已存在的焦点状态 | 渲染帧 Look 与逻辑输入事实 | 不裁决演出抑制 |
| Camera 资源与绑定 | Profile、独立资源、显式 Rig/目标/物理上下文 | 只读运行绑定或精确失败、实际采用身份 | 本领域负责；角色装配调用，不含角色总 Program/整包 Projection |
| Camera Runtime | committed command、visible pose、Look、正式时间、有效绑定 | 当帧计划、生命周期和诊断 | 唯一调度，不能成为所有算法的大类 |
| Resolvers | 强类型请求和目标绑定 | 胜出请求、响应权和目标 | 不依赖 Cinemachine |
| FramePlanner | 目标、Sequence、手动偏移 | 无历史期望构图 | 不查询 Physics，不维护第二份角色插值 |
| Transition / History | 期望构图、时间、Reset | 混合和平滑后的基础计划 | 唯一切镜/连续状态 |
| Effect owners | 计划、资源、请求身份 | 效果贡献及修正计划 | 每种效果负责自己时空语义和结束 |
| 环境约束求解器（新增） | 效果后计划、正式碰撞配置、查询端口 | 安全计划和修正原因 | 不改 follow/aim Transform |
| Unity 查询实现（新增） | 形状、起终点、自身/层/触发器过滤 | 命中事实 | 仅平台查询；不选镜头业务 |
| RigAdapter | 最终计划 | 实际 RigResult/Basis | 落地与回读，不另建状态/阻尼/输入 |
| 诊断和作者视图 | 正式字段与运行快照 | 作者可读信息与导航 | 不复制领域表，不重算另一套结果 |

查询端口名称按实际目录习惯确定；必需字段包括期望/前次位置、pivot、近裁剪保护体或半径、LayerMask、Trigger、自身过滤、delta、Reset，结果区分无命中、修正、起点重叠与无合法空间。接口不得只返回一个“成功”布尔值。

### 3. 基础构图、输入和平滑

本节保留原算法目标与初次调查背景；实现窗口之后已完成的字段清理、平滑和裁决按实际代码保留，不因本次公共版本/装配迁移重新实施。这里只调整资源与运行绑定边界，不能把初次调查缺口当作今天仍未修复的证明。

保留当前轨道角度与手动偏移相加、InputAction 的 Y 反转、默认 Sequence 资产及同帧 visible pose。不得把已修复的鼠标输入再次作为重建工作。

当前 ElevationRatio=0.5 固定采样中间轨道，鼠标改变角度偏移，不改变轨道采样参数。是否映射到三轨道必须由消费者证据或明确业务决策确定，不凭“看起来更像”修改。

Offset 当前参与 Rotation * (Offset.x, Offset.y, Radius) 的长度计算；ScreenOffset 名称与 AspectRatio 字段未代表已完成屏幕投影求解。先定单位，再连同所有资源与 UI 迁移。Profile.DefaultOrbitGroup 与 Sequence.CameraOrbits 重复；DefaultSphere/DefaultFOV 仍有部分 stage 初始化消费者，补齐 stage 输入后才能删。

修正 SmoothDamp 后每帧 SetCurrent 清零速度的问题。只有 Reset/首次绑定等明确边界清空历史；跟随平滑、手动响应与作者切镜时长需各自有确定职责，不能继续用第二层平滑任意延长动作过渡。

Reset 区分首次绑定、瞬移、回放恢复、会话退出，分别定义手动角度与速度保留规则。输入快照携带可追踪帧号；Full/Suppressed/Weighted 改消费权，不停止采集。当前 Keyboard&Mouse 不自动扩大为手柄；如果后续纳入手柄，必须区分鼠标位移与摇杆角速度。

### 4. 目标、效果与碰撞

锁定由业务传入明确目标。现有 Key→Transform 只能提供点，不能代替角色与敌人同时入镜的解法。需闭合锁定/双点/多点/实体取景的选择、单位、限幅、插值和丢失目标处理；正常目标销毁由业务结束请求，漏绑必需目标继续报配置错误。

保留 Zoom/Stretch 的实现及原事件生命周期；优先级相同时统一 source/generation/action/cycle 的稳定裁决，不依赖字典插入顺序。自然结束、取消、事件撤销和 Owner 销毁均保留真实身份，不能把每个请求终止都变成 Weight=0。

Override、Shake、Shot 按已有来源逐个补齐资源及消费者；先恢复正式语义，再移除 compiler/runtime 的不可用错误。Shot 需要实际 prefab、绑定和切镜资源，不能只创建名称。不得摆动角色骨骼或目标物体伪造镜头 Shake。

环境约束放在所有影响位姿的效果之后、Adapter 之前，处理近裁剪保护、急转扫墙、薄墙、起点重叠、低顶和无合法空间。缩回及时，恢复距离有明确连续历史，平滑后的结果仍需合法。自身和触发器按正式过滤规则排除，不全场景搜索。无法满足期望构图应返回明确受限结果，不静默回到穿墙位置。

原消费者的效果顺序、时钟和组合规则仍需来源对账；当前注册顺序 Override→Zoom→Stretch→Shake→Shot 只是项目现状，不能作为完整原行为已证明。碰撞与演出请求不在同一个 priority 数字域中竞争。

### 5. 领域资源、只读绑定、作者和诊断

Profile 装配相机资源，技能 Graph 内的 TreeClip 特殊 Node 表达动作相机请求。技能内容仍独立编译；C# 控制与 Pose 原生图不被塞回相机或角色总包。相机的资源转换/引用解析只覆盖实际领域输入，实例创建时形成只读运行绑定，不每帧遍历作者资产，也不形成另一份可编辑配置。实例绑定不可序列化成新的发布总包；已有独立资源产品保持自己的处理与身份。相机内容变化更新自己的内容/绑定身份，不要求重建角色所有技能、Pose 和非相机资源。

当前 `CharacterCameraProjectionBuilder.Build(CharacterCameraProfile)` 只接收 Profile，其必要字段转换、资源引用检查和 payload 可以按领域保留并改成合适名称，但旧名称和类型不代表必须保留整包 Projection。相机任务提供正式资源和绑定入口，领域运行时迁移任务从角色装配调用。Builder 位于 Editor 编译目录，不能因取消总包就直接搬到 Player：纯运行绑定逻辑与 Editor-only 的资源转换/导入边界要分开，真正资源处理继续走原有独立资源流程，不新增 Camera-only 临时发布入口、运行时补构建或另一个总包。

绑定在采用前完整解析候选：必需资源、类型、目标/Shot/Rig/物理上下文不满足时，由 Camera 返回带资源/请求来源的失败，不伪造新绑定已采用。成功采用后发布实际资源身份、内容版本、绑定实例/代际和状态；Reset、替换时停止旧调用、处理 History/Transition/Effect 的明确重置与资源释放，旧实例结果不得写入新实例。具体已有接口名字在迁移中统一，不为诊断再建独立状态来源。

UI 显示真实单位、支持范围、资源处理/技能编译各自状态及 Camera 实际采用身份；不在 OnInspectorGUI 编译或求值。通过现有 C# API 表达相机作者字段，不恢复目录包/同步器。Preview/ScenePlay 只调用领域绑定、Reset/替换及正式相机求值入口，并读取实际结果，不能自己解释 Profile、求解镜头或把“按钮操作成功”当成新绑定已采用。没有资源/绑定就由领域报告具体缺失，不等待旧全量 Build 恢复。

### 5.3 动作相机请求统一走 TreeClip 特殊 Node

动作相关的 Camera State、Effect、Response、Target 请求统一在技能 Graph 的 TreeClip 内通过相机特殊 Node 表达。当前已有的 `RequestCameraStateNode`、`EmitCameraCueNode`、`SetCameraResponseNode`、`SetCameraTargetNode` 和 `ReadCameraBasisNode` 是这条正式表达的基础；Node 只产生带稳定 ActionContext、来源身份、优先级、ResourceId 和生命周期的 typed Camera request，不直接写 Camera、Cinemachine 或虚拟相机。

正式调用次序收窄为：

```text
Ability / TreeClip
→ Camera 特殊 Node
→ Camera operation / PresentationCommand
→ CharacterCameraDomainRuntime
→ Sequence / Response / Target / Effect / Environment
→ CameraRigAdapter
```

Camera Domain Runtime 继续拥有默认轨道、鼠标输入、同帧 Body visible pose、平滑、效果叠加、碰撞和最终输出。TreeClip Node 只是动作时点的请求入口，不能变成第二套相机求解器。Node 的一次性触发、循环、取消、自然结束和 seek/replay 必须由正式 TreeClip 执行身份处理，不能在每个逻辑 tick 重复提交同一个请求。

动作链不再维护并行的 `CameraCueTrack`/`CameraCueClip` 或 `ActionCueClip` 携带 `CueType: Camera` 的旧表达。来源映射的工程落点改为 TreeClip/Node 稳定身份；ResourceId、来源事件、时间和退出规则仍必须逐项取证，改用 Node 不会消除映射要求。Camera 资源继续作为独立资源存在，Node 只引用它，不复制资源参数或创建 Camera-only 发布路径。

### 5.1 本批写入责任与输入输出

| 责任方 | 拥有和写入 | 提供给另一方的输入/输出 |
|---|---|---|
| Camera 本任务 | Camera 资源、Builder/payload、运行绑定、TreeClip 相机请求合同/编译出口；相机算法和诊断 | 资源身份与精确 TreeClip/Node 映射；绑定成功/失败、Reset/替换、实际采用身份 |
| replace-character-program-with-domain-runtimes | 角色实例装配、总 Program/整包 Projection 退役、领域模块接线 | 调用 Camera 的正式资源/只读绑定；不重写相机算法 |
| unify-timeline-motion-curve-source | 本批 Corin TreeClip 资产及其生成 C# 的唯一写入 | 消费 Camera 精确映射，写入具体 TreeClip/Node 请求；不反推来源效果公式 |
| Preview/ScenePlay 任务 | 现有会话、交互、观察、暂停/推进 | 调用 Camera 提供的绑定/Reset/替换，展示 Camera 返回的真实采用身份 |

本分工不改变未完成算法仍归相机的事实；公共版本或绑定调整不能成为重开正确 FramePlanner、轨道+鼠标偏移、History/Transition、Effect、Collision、Adapter、同帧 Body 及诊断行为的理由。同批 Timeline 资产和生成源码不能由 Camera 与曲线迁移各自重建。

### 5.2 精确来源映射，禁止按动作简称猜接线

本任务交付 `evidence/source-cue-mapping.md`，逐行记录源文件/对象/事件、工程 TreeClip/Node 精确路径和稳定身份、Node 请求身份、效果类型、ResourceId、原时钟/帧率、时间/持续/权重/取消规则及缺口。未知项按具体资源和具体证据列出，由任务继续取证，不要求用户凭空填整张表。

协调输入确认 Attack_Counter 与 Attack_Normal_05 的部分 Zoom key 和正式资源 m_ZoomId 一致；本轮磁盘核对可见 Corin_Attack_Counter_CamZoom_01、Corin_Attack_Normal_05_CamZoom_01/02。key 一致不证明工程 TreeClip/Node 映射或触发时刻。Attack_Normal_01 资料当前只列 Corin_Attack_Normal_01_CamShake_A_01，不能按 Attack1 的名字认定工程对应，更不能用 Zoom 替代缺失 Shake。映射确认后由曲线迁移任务统一写本批 TreeClip 与生成源码，Camera 仅提供精确请求映射和资源/合同。

诊断沿现有快照和采样算子扩展：原始/消费 Look、基准角/手动偏移/限幅、请求胜出原因、来源身份、时间域、blend、Reset、效果贡献、碰撞前后及 RigResult。记录/回放迁到正式相机初始状态合同后删除旧 Controller。纯 logic 输入回放不自动证明相机重放；需要记录相机初始状态、渲染帧 Look 和时间信息。

### 6. 并列业务取舍

这些是行为选择和依赖，不是优先级排序。本修订不把未选择方案当作实施授权。

| 决策 | 方案 A 的收益与代价 | 方案 B 的收益与代价 | 当前约束 |
|---|---|---|---|
| 求解归属 | 现有 Planner/History 求解，完整计划可解释，需维护算法 | Cinemachine 负责 orbit/damping，组件调参方便，但要迁移已有数据和删除 History 求值 | 保留当前 A，delta 修正冲突文字；B 需明确改变现有架构 |
| 俯仰控制 | 固定轨道采样+偏移，距离稳定、沿用已修行为 | 俯仰映射轨道参数，远近和取景随视角变化，需整体标定 | 保持 A；来源证明需要 B 时明确迁移 |
| Offset | 长度单位并改名，保留当前画面，但不同 FOV 下屏幕位置变化 | viewport 坐标求解，作者直控屏幕位置，但需宽高比输入和全资源迁移 | 先补来源单位，不自行二选一 |
| 平滑 | 一个时间常数，配置少但鼠标/跟随/切镜互相影响 | 按跟随、手动旋转、构图过渡分职责，输入更可控但增加参数 | 补来源与状态合同，不能用调参掩盖速度清零 |
| 出生/Reset 朝向 | 世界基准，固定镜头可预期但可能从角色侧面起镜 | 角色朝向建立基准，起镜在身后但需明确何时重新建立 | 保留现状，改行为前明确决定 |
| 来源还原 | 补齐 ZZZ 消费者，保持本 change 的还原目标，进度依赖取证 | 正式改为 3C 自有有限合同，可按 demo 需求实现，但不再宣称相应能力来源一致 | 保留 A；B 必须由用户明确改范围 |
| 遮挡 | 收缩镜头距离，单一位置约束易解释，近墙构图受限 | 额外材质淡出保住距离，但增加渲染所有权与排序成本 | 本 change 做 A；B 不自动纳入 |
| 锁定目标 | 消费已有业务目标，职责清楚但不提供选敌操作 | 扩展目标选择业务，能切敌/离开范围解锁，但跨出相机模块 | 本 change 做取景；新增选敌需明确业务范围 |

## Migration Plan

依赖顺序：领域资源/绑定合同与 TreeClip Node 请求合同及来源映射 → 相机必要转换和角色装配调用 → 曲线迁移统一写入 TreeClip/生成源码 → 各领域实际采用/Reset/诊断 → 旧总包绑定与 Camera Cue 无消费者路径删除。原未完成算法继续按其来源依赖处理，已经正确的求值不重写。本轮只更新这些规划依赖，不执行此迁移序列。

迁移按实际受影响的 authoring、validator/hash、领域转换、payload/绑定和资源引用处理，取消整角色生成产物联动；资源加工需要的独立输出仍保留。旧 Controller/回放和 prefab 的已正确迁移保留，后续按当前差异接续，不重复删除或回退。本任务不修改 implementation.md 的真实旧域构建失败记录，也不以该记录证明新绑定已经采用。

旧 Agent delta 和独立规划本次删除；原来源 evidence 保留为历史证据，当前实现状态另有唯一证据页。旧 worktree 的文件不在本次写入范围，不能把本 change 的文档重写当作跨 worktree 同步或归档。

## 与现行 spec 的对照

| 正式规范 | 现状/冲突 | 本 change 的处理 |
|---|---|---|
| character-camera-pipeline：唯一 Runtime、local-only、同帧 Body | 与当前主链一致 | 保留；不为新效果重写 Body/网络 |
| Cinemachine 必须是 CameraRigAdapter 实现细节 | 内含 Cinemachine 必须负责 orbit/damping 的旧场景，与 Planner/History 实现冲突 | MODIFIED 明确项目求解，Adapter 应用与回读 |
| Camera Sequence / Camera Effect owner | 已有有限算法和固定 owner，不是旧 StateResolver/Modifier | 修改现有 requirement，不删除不存在的旧 requirement |
| Camera debug | 当前快照未覆盖全部期望原因 | 补齐现有合同，不宣称现状已满足 |
| character-csharp-authoring | 已删除旧 Agent 目录包 | 删除废弃 capability delta，添加相机领域覆盖要求 |
| btsmtl-compiled-simulation-program / 旧 Camera delta | 总 Program、整包 Projection、全量 Build 与已批准领域基线冲突 | 删除本 change 的旧 compiled-simulation delta；总包退役由领域迁移 owner 处理，相机只提供资源与绑定 |
| replace-character-program-with-domain-runtimes D1/D5/D6/D8 | 技能独立编译，C# 控制、Pose 原生运行，表现按领域绑定 | Camera 接资源/只读绑定和实际采用身份，不创建总包或 Editor 逻辑运行时搬运 |
| btsmtl-timeline-editor-preview：Continuous Curve | 已包含相机曲线和其它领域完整要求 | 不重复改写该 requirement；动作请求统一由 TreeClip Node 提交，Preview 只观察 Camera Runtime 结果 |
| source-parity（本 change 新能力） | 原行为证据与项目已写代码不能互相替代 | 保留全范围和缺口，禁止以新增现状页宣布完整移植 |

规划修订没有修改 current spec 或其它任务文档；delta 是协调后的目标约束。旧 current spec 尚存 Program/Projection 用语不撤销用户已批准的新基线，双方按上述 ownership 迁移。用户已授权原实现窗口继续，但文档修订不代表代码完成。后续如操作 Unity，编译期间禁止修改代码或反复刷新，Play 时不得 Build/Refresh，且每次工具显式指定实例。

## 验收边界

用户端到端观察应覆盖：原地鼠标与限幅、走跑急停和不同渲染帧率、暂停/慢动作/失焦、瞬移/回放复位、动作取消与受击、墙角低顶窄通道、目标切换/丢失、不同宽高比。诊断需能解释帧号、输入、Body pose、混合、来源、碰撞和最终输出。

这些条件不列入 tasks，也不新增自动化测试。当前只取得源码/资产证据；此前 Unity MCP 在实例路径核对前断线，没有本次动态验收。构建成功、资源存在、单帧截图、历史任务勾选都不等于端到端完成。

## Open Questions

仍需补齐来源或明确行为选择：Offset 单位与宽高比处理、实际默认键/俯仰映射、Reset 朝向、平滑/转场时间分工、效果组合顺序与 Shake 对业务瞄准 Basis 的影响。是否改变原移植目标由用户决定；已确认模块边界和已有正确行为不重新争论。
