# Tasks

- [x] 1.1 定义 `CameraMode`、`CameraStateRequest`、`CameraResponsePolicy`、`CameraCue`、`CameraTargetRequest`、`CameraBasisSnapshot` 和 `CameraPosePlan` 的 runtime model。
- [x] 1.2 在 `PresentationOutput` 增加本地相机请求、cue、response policy、target request 和 pose/debug 输出缓存。
- [x] 1.3 在 `CharacterGraphContext` 增加提交相机请求、提交相机 cue、提交响应策略、提交 target request 和读取 camera basis 的正式接口。
- [x] 1.4 增加 `RequestCameraStateNode`，只向 GraphContext 提交 `CameraStateRequest`。
- [x] 1.5 增加 `EmitCameraCueNode`，只向 GraphContext 提交 `CameraCue`。
- [x] 1.6 增加 `SetCameraResponseNode`，提交 look response 和 manual orbit weight。
- [x] 1.7 增加 `SetCameraTargetNode`，提交 target key、anchor key 或 aim point key。
- [x] 1.8 增加 `ReadCameraBasisNode`，读取上一帧稳定 `CameraBasisSnapshot`。
- [x] 1.9 增加 Timeline camera state sample 类型和 `CameraStateTrack / CameraStateClip`。
- [x] 1.10 增加 Timeline camera cue sample 类型和 `CameraCueTrack / CameraCueClip`。
- [x] 1.11 增加 Timeline camera response sample 类型和 `CameraResponseTrack / CameraResponseClip`。
- [x] 1.12 让 `TimelinePlaybackScheduler` 采样相机轨道并提交到 `PresentationOutput`，不直接控制 Cinemachine。
- [x] 1.13 实现 `CameraStateResolver`，以 FreeLook 为 base state，并按 priority、weight、source lifecycle 仲裁 Aim、LockOn、ActionFocus、SkillCloseup。
- [x] 1.14 实现 `CameraResponsePolicyResolver`，合并 Full、Suppressed、Weighted 响应策略。
- [x] 1.15 实现 `CameraModifierResolver`，第一阶段维护 shake、FOV kick、recoil 和 collision correction 的有限顺序；仅 FOV kick 写入 lens 修正，空间表现交给 Cinemachine adapter/extension。
- [x] 1.16 实现 `CharacterCameraStage`，在 PresentationFrame 消费相机输出并生成 `CameraPosePlan`。
- [x] 1.17 定义 `ICameraRigAdapter`，让 Stage 只依赖 pose plan 输出接口。
- [x] 1.18 将现有 `ThirdPersonCameraController` 收口为 Cinemachine FreeLook adapter，只写 FreeLook axis、Follow/LookAt target、FOV 和 Brain，并输出 Cinemachine basis。
- [x] 1.19 移除相机旧自主 `LateUpdate` 控制权，确保 CharacterPipeline 是相机表现的正式调度入口。
- [x] 1.20 在 `CharacterPipelineHost` 增加显式 camera rig / camera anchor 绑定，并在缺失时报告配置错误。
- [x] 1.21 将 `CharacterPipeline.PresentationFrame` 串入 `CharacterCameraStage`，保持 frame cleanup 顺序统一。
- [x] 1.22 接入 action lifecycle，清理 action-scoped camera request 和 cue。
- [x] 1.23 输出 `CameraDebugSnapshot`，展示当前 mode、active requests、response policy、source action instance、blend progress、basis 和 pose plan 摘要。
- [x] 1.24 搜索并清理新相机链路中的 `Camera.main`、`FindObjectOfType`、直接 Cinemachine 业务调用和隐藏 fallback。
- [x] 1.25 运行 `openspec validate add-character-camera-pipeline --strict --no-interactive`。
- [x] 1.26 运行相关 `rg` 检查，确认 BTSMTL/Timeline 相机节点没有直接依赖 Cinemachine 或场景搜索。
- [x] 1.27 删除 controller-level direct influence stack、旧 resolve result、旧自研相机碰撞 extension 和相机空间解算旁路。

## 2. 相机表现跟随闭环

- [x] 2.1 复查 `CharacterPresentationStage` 的 previous/current logic pose、`InterpolationAlpha` 和 force correction 贴合语义。
- [x] 2.2 定义 `CharacterPresentationRootPose`，表达本表现帧统一的 position、rotation、grounded 和 valid。
- [x] 2.3 在 `PresentationOutput` 增加当帧 `PresentationRootPose`，并在 transient clear 时清理。
- [x] 2.4 让 `CharacterPresentationStage` 只计算一次 `PresentationRootPose`。
- [x] 2.5 让 visual root 从 `PresentationRootPose` 和 visual bind offset 生成 `CharacterVisualPose`。
- [x] 2.6 在表现 debug snapshot 中暴露 `PresentationRootPose`。
- [x] 2.7 将正式 logic root 绑定传入 `CharacterCameraStage`。
- [x] 2.8 在 `CharacterCameraStage` 初始化时计算 camera anchor 相对 logic root 的绑定偏移。
- [x] 2.9 让默认 follow point 使用 `PresentationRootPose` 变换 camera anchor 绑定偏移。
- [x] 2.10 保持显式 `CameraTargetRequest.AnchorKey` 世界点语义不变。
- [x] 2.11 表现根姿态缺失时报告明确错误，不回退读取 logic anchor 世界坐标。
- [x] 2.12 删除默认 follow 路径对 `m_FollowAnchor.position` 的表现帧读取。
- [x] 2.13 搜索确认 CameraStage 没有新增独立插值历史、额外 alpha 计算或 visual root fallback。
- [x] 2.14 刷新 Unity 并确认相关脚本编译无错误。
- [x] 2.15 使用运行时帧探针确认移动期间默认 camera follow point 不再按 logic tick 交替冻结。
- [x] 2.16 运行 `openspec validate add-character-camera-pipeline --strict --no-interactive`。
- [x] 2.17 确认以上任务完成后再将本节更新为 `[x]`。
