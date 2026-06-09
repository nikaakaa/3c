## Context
当前项目里相机链路已经有两层职责：

- 项目侧 `ThirdPersonCameraController` 读取 Look 输入，维护 yaw/pitch，输出 `CameraTarget`、`CameraPlanarForward`、`CameraPlanarRight` 和 pitch。
- Cinemachine vcam 使用 `Follow/LookAt`、`3rd Person Follow`、Composer 和自定义 `CameraArmCollisionConstraint` 输出最终画面。

这个结构让最终画面仍依赖 Cinemachine，但基础轨道和视角状态由项目自研逻辑掌控，导致手感不如 FreeLook，也让后续多影响源战斗相机容易出现多个主真相源。

本变更的方向是：Cinemachine FreeLook 成为 Free 模式基础镜头的主真相源；项目侧保留影响源仲裁和对外接口，但不再维护 Free 模式主 yaw/pitch 解算，也不再维护独立的跟随目标和瞄准目标。

## Goals / Non-Goals
- Goals:
  - Free 模式主相机使用 `CinemachineFreeLook`。
  - 项目侧继续掌控影响源决策，不把战斗镜头规则散落到 Cinemachine 组件上。
  - 移动系统继续通过项目接口读取相机平面方向。
  - `CameraArmCollisionConstraint` 保留为 Cinemachine 管线里的平面适配约束。
  - prefab 和场景中只保留一个启用的 Free 模式主相机真相源。
- Non-Goals:
  - 不实现完整战斗相机模式系统。
  - 不实现锁定、瞄准、技能镜头、震屏或 TargetGroup 细节。
  - 不删除现有 log，除非后续单独明确要求。
  - 不引入新第三方相机框架。
  - 不绕过当前 WASD、运动驱动和动画外观层。

## Decisions
- Decision: Free 模式主相机改为 `CinemachineFreeLook`。
  - Reason: FreeLook 已经提供第三人称轨道、上下轴、阻尼和三轨构图，当前需求明确希望直接使用 Cinemachine 自带能力。
  - Alternative: 继续调自研 yaw/pitch target。会继续维护一套不如 FreeLook 的基础相机数学。

- Decision: FreeLook Follow 和 LookAt 使用同一个角色锚点。
  - Reason: 当前 Free 模式不需要双目标结构；保留 `CameraFollowTarget` / `CameraAimTarget` 会让旧相机路径继续存在。
  - Alternative: 让两个目标位置相同。这样仍然保留旧输出路径，后续战斗相机影响源会更难判断真相来源。

- Decision: 项目侧保留相机影响源仲裁层。
  - Reason: 后续战斗会有锁定、瞄准、技能镜头和构图请求，这些属于游戏规则，不应交给单个 Cinemachine 组件直接决定。
  - Alternative: 让每个战斗脚本直接改 FreeLook 参数。短期快，但会产生分裂路径，难以排序、回收和调试。

- Decision: 项目侧相机接口继续服务移动和调试。
  - Reason: 移动系统已经通过 `ICameraMovementBasisProvider` 读取平面方向，这个边界可以避免 `Camera.main`、FreeLook 实例和移动逻辑耦合。
  - Alternative: 移动脚本直接读取 FreeLook 或主相机 Transform。会把移动主路径绑定到具体相机实现。

- Decision: `CameraArmCollisionConstraint` 保留，但只作为 Cinemachine Extension。
  - Reason: 用户明确需要对平面或薄地面做额外适配；该逻辑应继续在 Cinemachine 管线边界修正最终状态，不写进移动或输入。
  - Alternative: 完全退回 Cinemachine 自带碰撞。可以减少代码，但会丢掉当前对平面适配的需求。

- Decision: 现有旧 yaw/pitch 控制器不再作为 Free 模式主驱动。
  - Reason: FreeLook 应成为 Free 模式相机状态源，避免 `ThirdPersonCameraController` 和 FreeLook 同时解释 Look 输入。
  - Alternative: 两者同时保留并靠优先级切换。会让调试时很难判断当前视角是谁驱动。

## Proposed Runtime Shape
1. 输入层读取玩家 Look 输入。
2. FreeLook 输入适配器把 Look 输入写入 `CinemachineFreeLook.m_XAxis` / `m_YAxis` 或等价接口。
3. `CinemachineFreeLook` 负责 Free 模式轨道、阻尼、构图和基础相机位置。
4. 项目相机服务从当前 live Cinemachine 相机或输出相机 Transform 计算 `CameraPlanarForward`、`CameraPlanarRight`、`LookDirection`、`AimPoint`。
5. 影响源仲裁层接收移动、战斗、锁定、瞄准和技能镜头请求，并只通过统一适配器调整 Cinemachine 状态或优先级。
6. `CameraArmCollisionConstraint` 作为 FreeLook/子 rig 可用的 Cinemachine Extension，在管线后段处理平面和薄地面适配。
7. WASD 移动继续读取项目相机接口，再进入现有移动状态和 `CharacterMotionDriver`。

## Risks / Trade-offs
- Risk: FreeLook 子 rig 和自定义 Extension 挂载边界需要验证。
  - Mitigation: 任务中先定位 FreeLook 生成的 rig 和 Extension 生效点，再改 prefab。
- Risk: 旧 `ThirdPersonCameraController` 与 FreeLook 同时启用会双重消费 Look 输入。
  - Mitigation: 任务要求明确禁用或替换旧 Free 模式主驱动，并用 prefab/YAML 检查验证。
- Risk: 移动方向来自输出相机时，镜头阻尼可能让移动方向略有延迟。
  - Mitigation: 通过 EditMode 测试验证方向计算纯逻辑，通过手动验证确认手感；必要时后续 proposal 决定方向使用 FreeLook 轴还是 live camera。
- Risk: 平面碰撞适配比 Cinemachine 默认碰撞更项目化。
  - Mitigation: 保持为可插拔 Extension，禁用后仍可回到纯 Cinemachine 行为。

## Open Questions
- 后续锁定/瞄准模式是否使用独立 vcam、FreeLook 参数覆盖，还是 TargetGroup 构图，需要单独 proposal 决定。
