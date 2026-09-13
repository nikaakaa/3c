## Context

修订 v3，2026-09-13。用户明确“让实现窗口做吧”，已确认当前规划范围并授权配套实现窗口实施。当前工作区为 D:/Unity_Project_1/3C；旧 camera-zzz worktree 中的执行批次和审批记录仅属于历史，不作为本修订的调度入口。

本 change 的 proposal 定义范围，本文定义职责和设计，tasks 只列剩余工作。当前源码状态与动态证据边界见 evidence/current-implementation.md。独立的 docs/character-camera-plan-2026-09-13.md 已合并删除，不维护第二份规划。旧协调文档未在本次修改，其 9 月 6 日版本/接线状态不能直接作为今天的事实。

当前配套规划窗口为 01a098bd-a5c1-7283-8aab-59736bab97f5，实现窗口为 01a098bd-bbdf-7d50-b1cc-95339d3bbf8d。实现窗口执行 tasks 的全部剩余范围，创建并维护 implementation.md，记录各任务实际完成状态、修改、提交、证据和冲突；不自行改写规划合同。独立且已授权的部分持续推进，不等待普通进度回执。

授权包含补齐来源消费者和真实资源接线，不包含放弃来源还原、猜测缺失公式或覆盖已有正确代码。当前产品方向为玩家控制的第三人称环绕相机，默认轨道提供基础构图，动作、锁定和碰撞共用同一求解链。未决项先按来源取证与现行合同解决；无法同时满足真实代码、来源和规划时，在 implementation.md 写明冲突与业务取舍，再发送一次 ACTUAL_CONFLICT。

```text
PLANNING_DOCUMENT
planner_thread_id: 01a098bd-a5c1-7283-8aab-59736bab97f5
implementation_thread_id: 01a098bd-bbdf-7d50-b1cc-95339d3bbf8d
planning_document_paths:
  - D:/Unity_Project_1/3C/openspec/changes/rebuild-character-camera-from-zzz/proposal.md
  - D:/Unity_Project_1/3C/openspec/changes/rebuild-character-camera-from-zzz/design.md
  - D:/Unity_Project_1/3C/openspec/changes/rebuild-character-camera-from-zzz/tasks.md
  - D:/Unity_Project_1/3C/openspec/changes/rebuild-character-camera-from-zzz/specs/
  - D:/Unity_Project_1/3C/openspec/changes/rebuild-character-camera-from-zzz/evidence/
implementation_document_path: D:/Unity_Project_1/3C/openspec/changes/rebuild-character-camera-from-zzz/implementation.md
confirmed_by_user: true
confirmed_revision: v3
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

实际调用次序：

```text
InputProfile / InputActions
→ Session.BeginRenderFrame
→ Registration / ControlSource.CaptureRenderFrame
→ 本渲染帧 Look + 采样时的 CameraBasis

正式 Graph / Timeline → 已提交 PresentationCommand
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
| Camera Runtime | committed command、visible pose、Look、正式时间 | 当帧计划、生命周期和诊断 | 唯一调度，不能成为所有算法的大类 |
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

### 5. 作者、编译、运行与诊断的统一

Profile 装配资源；Graph/Timeline 表达请求；资源参数和请求时点各有真实 owner。通过现有 C# 作者入口补齐相机领域适配，不恢复目录包、MutationPlan、同步器或第三个 MCP。人工编辑不自动导出源码，生成资产不等于生成可运行 Program/Projection。

Compiler 必须发布同一正式 Projection，保留完整非相机字段，版本、hash、依赖和加载校验一起迁移；禁止手改大型 Generated。旧 schema 明确拒绝，不写兼容解释器。不固定旧文档中的公共 ABI 数字，使用当前实际 owner 的正式版本规则。

UI 显示真实单位、支持范围、资源导航和明确 Build 状态；不在 OnInspectorGUI 中编译或重求值。Timeline 曲线继续由 Clip 或引用资源真实 owner 拥有，不因新增相机功能改写其它领域曲线。

预览复用当前 ScenePlay/Preview 正式 owner，不复活旧会话。需要求值状态的重建走该 owner 的正式 Reset/重建合同；若当前入口未提供能力，作为依赖写出，不自建执行器。Runtime 与 Preview 消费同一相机模块和已发布计划。

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

依赖顺序：字段/时间/来源合同 → 基础求值与状态 → 目标/效果/碰撞 → 作者与真实请求 → 正式生成产物 → 旧链删除。互不依赖的域内工作不因一个效果缺证据而全部停止。

所有迁移都覆盖正式 authoring、validator/hash、compiler、payload、evaluator、资产引用和输出产物。删除 ThirdPersonCameraController 前必须迁移输入记录/回放调用；该诊断文件与角色 prefab 已有用户修改，实施前重读差异，冲突交用户决定，不能覆盖。

旧 Agent delta 和独立规划本次删除；原来源 evidence 保留为历史证据，当前实现状态另有唯一证据页。旧 worktree 的文件不在本次写入范围，不能把本 change 的文档重写当作跨 worktree 同步或归档。

## 与现行 spec 的对照

| 正式规范 | 现状/冲突 | 本 change 的处理 |
|---|---|---|
| character-camera-pipeline：唯一 Runtime、local-only、同帧 Body | 与当前主链一致 | 保留；不为新效果重写 Body/网络 |
| Cinemachine 必须是 CameraRigAdapter 实现细节 | 内含 Cinemachine 必须负责 orbit/damping 的旧场景，与 Planner/History 实现冲突 | MODIFIED 明确项目求解，Adapter 应用与回读 |
| Camera Sequence / Camera Effect owner | 已有有限算法和固定 owner，不是旧 StateResolver/Modifier | 修改现有 requirement，不删除不存在的旧 requirement |
| Camera debug | 当前快照未覆盖全部期望原因 | 补齐现有合同，不宣称现状已满足 |
| character-csharp-authoring | 已删除旧 Agent 目录包 | 删除废弃 capability delta，添加相机领域覆盖要求 |
| btsmtl-compiled-simulation-program | 已有唯一 Projection 和 Build Transaction | 只补相机依赖/语义要求，不建立第二套发布协议 |
| btsmtl-timeline-editor-preview：Continuous Curve | 已包含相机曲线和其它领域完整要求 | 不重复改写该 requirement；只添加相机状态接入与诊断约束 |
| source-parity（本 change 新能力） | 原行为证据与项目已写代码不能互相替代 | 保留全范围和缺口，禁止以新增现状页宣布完整移植 |

本次不修改 current spec 的完成事实；delta 是目标约束。用户后续授权实施或同步时，按实际交付更新对应规范，不把剩余能力提前写成完成。

## 验收边界

用户端到端观察应覆盖：原地鼠标与限幅、走跑急停和不同渲染帧率、暂停/慢动作/失焦、瞬移/回放复位、动作取消与受击、墙角低顶窄通道、目标切换/丢失、不同宽高比。诊断需能解释帧号、输入、Body pose、混合、来源、碰撞和最终输出。

这些条件不列入 tasks，也不新增自动化测试。当前只取得源码/资产证据；此前 Unity MCP 在实例路径核对前断线，没有本次动态验收。构建成功、资源存在、单帧截图、历史任务勾选都不等于端到端完成。

## Open Questions

仍需补齐来源或明确行为选择：Offset 单位与宽高比处理、实际默认键/俯仰映射、Reset 朝向、平滑/转场时间分工、效果组合顺序与 Shake 对业务瞄准 Basis 的影响。是否改变原移植目标由用户决定；已确认模块边界和已有正确行为不重新争论。
