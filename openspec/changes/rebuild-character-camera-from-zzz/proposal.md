## Why

当前摄像机已经接入 GameLab Local Fixed 的正式 Character Presentation 链。默认轨道、鼠标偏移相加、同帧 Body 跟随、请求生命周期、Zoom/Stretch 和 Cinemachine 输出已有代码，不应再次按“尚未接入”的旧基线实施。

原剩余范围包括连续平滑和构图合同收口、锁定与多目标构图、碰撞、Override/Shake/Shot、作者和真实动作引用、旧路径清理；其中实施窗口已经做过部分代码，实际状态保留在 implementation.md，不按旧任务列表重新实现。本次协调只迁移被取消总包的资源/绑定边界并明确来源映射与单一写入分工。旧执行批次、worktree 指令、Agent Document 计划均不恢复。

修订 v4，2026-09-13，协调提案 `camera-preview-timeline-domain-runtime-r1`。规划更新完成后，用户明确要求实现窗口继续，现按本修订接续原实施任务，承接 `replace-character-program-with-domain-runtimes/design.md` 已批准的领域运行时基线；保持协调后的资源、角色装配及同批 Timeline/生成源码写入分工。当前源码和旧发布失败的历史记录见 `evidence/current-implementation.md` 与实现记录，不能用旧 Projection 或旧 DLL 的结果证明新绑定已经可用。

## Scope

保留本 change 的单角色 ZZZ 相机移植目标，不以改写文档缩减未完成范围，也不把来源字段存在当成行为还原。完整目标包括基础构图与手动输入、跟随和平滑、锁定/双点/多点/实体取景、转场、Zoom/Stretch/Override/Shake/Shot、碰撞、正式作者资源与请求接线、诊断和旧路径删除。

已有正确代码保留；来源消费者未闭合的能力继续明确拒绝，直到证据与实现完整。若用户另选 3C 自有行为，须明确改写相应合同并迁移字段，不在旧来源合同下填入猜测公式。

不新增换人、网络相机状态、独立 Camera Preview 会话、自动选敌业务、材质淡出、全设备输入支持或独立 Camera Build。锁定消费业务显式给出的目标；需要选敌操作时单独明确它的业务范围。

## What Changes

- **BREAKING**：收口 Profile、Sequence 与相机领域资源/只读运行绑定的字段和单位，删除重复数据及对角色总 Program、整包 Projection 的依赖。相机提供必要转换和引用检查，角色装配由领域运行时迁移任务调用；不另建 Camera-only 发布入口或换名总包。
- 保留默认 Sequence、鼠标角度叠加、同帧 Body pose 与最终 Pose 后执行 Camera 的现有链。修正连续平滑状态，明确输入限幅、Reset、响应裁决与切镜时间语义。
- 在同一 Camera Runtime 内完成锁定/多目标构图、环境查询与碰撞约束，以及尚未闭合的 Override/Shake/Shot，扩展相应有限算法，不另起 Controller。
- 保留已有 Zoom/Stretch，完整接线以源动作/事件→工程 TreeClip/相机特殊 Node→效果类型/ResourceId→时间/持续/取消的精确映射为输入。相机提供映射；同批 Corin TreeClip/生成源码由曲线迁移任务统一写入，18 个资源不等于 18 个已接通动作。
- 动作相机请求统一由技能 Graph 内的 TreeClip 特殊 Node 表达。Node 只提交带 ActionContext、ResourceId 和生命周期的正式相机请求，不直接写 Camera、Cinemachine 或虚拟相机；Camera Runtime 继续唯一负责求解、叠加、碰撞和最终输出。动作链不再维护并行的 `CameraStateTrack`/`CameraResponseTrack`/`CameraCueTrack`/`CameraCueClip` 或 `ActionCueClip(CueType: Camera)` 路径。
- **BREAKING**：Timeline 相机表达按触发与排布划界。瞬态触发一律走 TreeClip Node，持续效果窗口收敛为唯一效果轨道（Clip 只含窗口、资源引用与曲线，效果类型由资源自描述）；迁移后删除 CameraState/Response/Cue 三组触发型轨道与按类型拆分的四条效果轨道及其曲线 channel。瞬态请求统一走 Node 同时作为通用合同：音效、特效等其它域的帧触发表达按同一边界迁往 Node 后删除其宿主轨道，本 change 不扩大到其它域的具体迁移。
- 作者层消费现有正式 Graph/Timeline/资源 API 及 `btsmtl.export_code`、`btsmtl.generate_assets`，删除已废弃的 Agent Document v4/v5 接入 delta。相机 C# 领域覆盖尚需实现对账，不宣称已经完整支持。
- **BREAKING**：将记录/回放的相机初始状态与采样迁到正式 Presentation 合同，再删除旧 ThirdPersonCameraController、旧引用与孤立配置。保护既有诊断和 prefab 改动。
- 统一输出前求解与 Cinemachine 落地的职责，扩展现有诊断快照解释输入、请求来源、混合、效果、碰撞与实际输出。
- 清除旧批次和旧授权路由；本 change 的 proposal/design/tasks 是唯一当前方案，源码/运行证据放 evidence，既有 Git 历史保留追溯。

## Capabilities

### New Capabilities

- `character-camera-source-parity`：来源字段、消费者行为、依赖、项目实现与当前交付范围逐项对应。
- `character-camera-authoring`：Profile/Sequence/Effect/Curve 的唯一正式作者流程与可解释性。

### Modified Capabilities

- `character-camera-pipeline`：唯一相机求解和平台输出边界、连续平滑、输入、目标、效果与碰撞。
- `btsmtl-timeline-editor-preview`：沿现有预览 owner 消费相机能力与实际诊断。
- `character-csharp-authoring`：相机内容通过现有两个显式作者入口完整导出/生成。

## Impact

相机任务拥有 Camera 资源、Builder/payload、只读运行绑定、TreeClip 相机请求合同/编译出口和自身诊断/算法。角色工厂与编译收窄由领域运行时迁移任务接入；同批 Corin TreeClip 资产及生成源码由 `unify-timeline-motion-curve-source` 统一写入。仅公共版本/绑定迁移不重新打开已正确算法。网络 Pipeline/Pass、Float32/Fixed 和独立资源处理保留。

本修订不把 `CameraCueClip`/`ActionCueClip` 的历史状态写成已迁移结果；当前只有已确认映射的动作迁到 TreeClip Node，其余历史 Cue 必须等映射和消费者闭合后删除。正式目标是单一 TreeClip Node 请求链，Timeline/TreeClip 只作为正式相机请求消费入口，迁移完成后删除旧动作 Cue 路径，不保留兼容双轨。

现行规范已与主链一致：`character-camera-pipeline` 由项目内部求解 CameraFramePlan，Cinemachine 只由 Adapter 落地；C# 作者规范已删除旧目录包；相机 spec 也没有旧 CameraStateResolver/Camera modifier requirement。后续提案只需维护剩余消费者、映射和清理合同，不恢复总包 Projection、旧触发轨道或第二套 Timeline Camera domain。

早期相机 delta 曾要求相机进入整包 Projection 并等待 Character 全量 Build，该口径已经退役；仍必要的资源转换、依赖检查与身份要求改归相机领域绑定 delta。当前规范不再保留整角色 Program 索引，不能借历史文字恢复总包。本次只落规划和 delta，不改写其它任务文件或宣称代码迁移已完成。
