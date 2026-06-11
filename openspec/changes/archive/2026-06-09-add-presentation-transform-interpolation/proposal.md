# Change: 增加角色表现层 Transform 插值

## Why
当前 `Sandbox` 的基础移动已经接入 60Hz simulation tick。角色真实 Transform 只在 tick 输出时变化，而可见模型、骨骼和相机跟随目标仍直接或间接消费 tick 阶梯化结果；高刷新率下会看到角色表现和相机跟随以 60Hz 节奏跳变，256 tick 只是把阶梯变密，并没有解决表现层边界问题。

当前 `可琳.prefab` 中可见骨骼和 SkinnedMeshRenderer 仍是角色真实根的直接子级，现有相机锚点插值只能让相机目标连续，不能让画面中的角色本体连续。本变更把插值提升为通用表现层 Transform 能力：模拟根保持 tick 权威，表现根按渲染帧消费 tick 样本并输出连续 pose，相机跟随同一个表现结果。

## What Changes
- 增加通用表现层 Transform 插值：表现层组件 SHALL 读取 tick 后的真实 pose 样本，并在渲染帧输出插值后的 visual pose。
- 拆分角色真实模拟根和可见表现根：`CharacterController`、tick adapter、locomotion 主线保持在真实根；Animator、骨骼和渲染器归入表现根或其子树。
- 让相机目标代理消费表现根或其派生锚点：`CameraFollowTarget` / `CameraAimTarget` 继续是相机主路径输出，但输入来源 MUST 是表现层结果。
- 增加 tick 系统只读插值信息：simulation driver SHALL 暴露表现层可读的 tick 余量或 alpha，但表现层 MUST NOT 反向修改 tick accumulator、角色真实 Transform 或运动权威。
- 增加自动测试、静态边界检查和手动高刷新率验证步骤。

## Impact
- Affected specs:
  - `presentation-transform-interpolation`
  - `cinemachine-third-person-camera`
  - `simulation-tick-system`
- Affected code:
  - `Assets/Scripts/Presentation/*`
  - `Assets/Scripts/Camera/Runtime/ThirdPersonCameraController.cs`
  - `Assets/Scripts/Camera/Runtime/CinemachineResolvedTargetAdapter.cs`
  - `Assets/Scripts/Camera/Model/CameraFollowAnchor.cs`
  - `Assets/Scripts/Simulation/Runtime/UnitySimulationTickDriver.cs`
  - `Assets/Scripts/Simulation/Core/SimulationTickAccumulator.cs`
  - `Assets/Prefabs/Character/可琳.prefab`
  - `Assets/Prefabs/Camera/Third Person Camera Rig.prefab`
  - `Assets/Scenes/Sandbox.unity`
  - `Assets/Tests/Editor/*`
