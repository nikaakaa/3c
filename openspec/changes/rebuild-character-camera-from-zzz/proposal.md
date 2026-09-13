## Why

当前摄像机已经接入 GameLab Local Fixed 的正式 Character Presentation 链。默认轨道、鼠标偏移相加、同帧 Body 跟随、请求生命周期、Zoom/Stretch 和 Cinemachine 输出已有代码，不应再次按“尚未接入”的旧基线实施。

尚缺的部分是：连续平滑和构图合同收口、锁定与多目标构图、碰撞、Override/Shake/Shot 的真实求值、作者能力与真实动作引用的完整交付，以及旧 Controller/诊断路径清理。旧 change 的执行批次、worktree 指令、Document v4/v5 计划和进度已过时。

2026-09-13 按用户“可以删除重写”授权重写本 change，并合并删除独立的摄像机规划文档。用户随后明确“让实现窗口做吧”，现已授权配套实现窗口按 v3 文档范围实施。当前源码证据见 `evidence/current-implementation.md`；原始来源证据继续由 `evidence/source-baseline.md`、`evidence/source-behavior.md` 保存。

## Scope

保留本 change 的单角色 ZZZ 相机移植目标，不以改写文档缩减未完成范围，也不把来源字段存在当成行为还原。完整目标包括基础构图与手动输入、跟随和平滑、锁定/双点/多点/实体取景、转场、Zoom/Stretch/Override/Shake/Shot、碰撞、正式作者资源与请求接线、诊断和旧路径删除。

已有正确代码保留；来源消费者未闭合的能力继续明确拒绝，直到证据与实现完整。若用户另选 3C 自有行为，须明确改写相应合同并迁移字段，不在旧来源合同下填入猜测公式。

不新增换人、网络相机状态、独立 Camera Preview 会话、自动选敌业务、材质淡出、全设备输入支持或独立 Camera Build。锁定消费业务显式给出的目标；需要选敌操作时单独明确它的业务范围。

## What Changes

- **BREAKING**：收口 Profile、Sequence、Projection 的构图字段与单位，删除确认无消费者或重复的数据；同步 validator、hash、compiler、作者 UI、资源和生成产物，不保留兼容读法。
- 保留默认 Sequence、鼠标角度叠加、同帧 Body pose 与最终 Pose 后执行 Camera 的现有链。修正连续平滑状态，明确输入限幅、Reset、响应裁决与切镜时间语义。
- 在同一 Camera Runtime 内完成锁定/多目标构图、环境查询与碰撞约束，以及尚未闭合的 Override/Shake/Shot，扩展相应有限算法，不另起 Controller。
- 保留已有 Zoom/Stretch，实现范围必须落实到真实动作请求、资源、Program/Projection 和明确退出行为；18 个资源不等于 18 个已接通动作。
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
- `btsmtl-compiled-simulation-program`：相机请求和资源分离、同一 Projection、依赖身份及原子发布。
- `btsmtl-timeline-editor-preview`：沿现有预览 owner 消费相机能力与实际诊断。
- `character-csharp-authoring`：相机内容通过现有两个显式作者入口完整导出/生成。

## Impact

运行范围为 CameraContracts、Character/Camera、内部 CharacterCameraPresentationRuntime，以及它们的正式编译、作者与诊断调用者。Body、动画、Gameplay 和网络的既有正确行为不重写。

当前规范冲突：`character-camera-pipeline` 仍有 Cinemachine 必须负责全部 orbit/damping 的文字，而当前 Planner/History 已负责求值；本 delta 明确由项目求解、Adapter 落地。现行 C# 作者规范已删除旧目录包，本 change 不再要求恢复它。当前 spec 已没有旧 CameraStateResolver/Camera modifier requirement，不再保留针对不存在 requirement 的删除条目。

文档重写未把未来能力写成已实现的 current spec。现已授权实施，全部冲突、依赖、业务取舍见 design；实现前重新核对当前工作区，不恢复旧 worktree 的自动执行指令。
