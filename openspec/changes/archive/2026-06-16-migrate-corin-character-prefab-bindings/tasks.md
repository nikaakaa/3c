## 1. 准备
- [x] 1.1 读取本变更 `proposal.md`。
- [x] 1.2 读取本变更 `design.md`。
- [x] 1.3 读取本变更 `tasks.md`。
- [x] 1.4 读取 `align-character-config-authoring-contracts` 的 proposal/design/tasks。
- [x] 1.5 读取 `migrate-corin-character-config-assets` 的 proposal/design/tasks。
- [x] 1.6 读取 `refactor-unified-animancer-presenter` 的 proposal/design/tasks。
- [x] 1.7 读取 `character-config-root` spec。
- [x] 1.8 读取 `character-runtime-ports` spec。
- [x] 1.9 对 `PlayerLocomotionController` 运行 GitNexus impact analysis，并报告 HIGH blast radius。
- [x] 1.10 对 `PlayerFullBodyActionController` 运行 GitNexus impact analysis，并记录 blast radius。
- [x] 1.11 对 `BasicLocomotionAnimancerPresenter` 运行 GitNexus impact analysis，并记录 blast radius。
- [x] 1.12 对 `ActionAnimationAnimancerPresenter` 运行 GitNexus impact analysis，并记录 blast radius。
- [x] 1.13 读取 `可琳.prefab` 当前 YAML 中角色主链组件片段。
- [x] 1.14 读取 `可琳_Humanoid.prefab` 当前 YAML 中角色主链组件片段。
- [x] 1.15 使用 Unity 只读 SerializedObject 检查两个 prefab 的实际组件引用。
- [x] 1.16 搜索 `Sandbox.unity`、CameraTest 场景中的 Corin 配置和组件引用。
- [x] 1.17 确认本变更与 `refactor-unified-animancer-presenter` 的执行顺序。

## 2. 自动测试先行
- [x] 2.1 增加 prefab 静态测试：`可琳.prefab` 的 Locomotion controller 绑定 `CorinCharacterConfig.asset`。
- [x] 2.2 增加 prefab 静态测试：`可琳.prefab` 的 FullBody controller 绑定 `CorinCharacterConfig.asset`。
- [x] 2.3 增加 prefab 静态测试：`可琳_Humanoid.prefab` 的 Locomotion controller 绑定 `CorinCharacterConfig.asset`。
- [x] 2.4 增加 prefab 静态测试：`可琳_Humanoid.prefab` 的 FullBody controller 绑定 `CorinCharacterConfig.asset`。
- [x] 2.5 增加 prefab 静态测试：正式 controller YAML 不保留 legacy config 字段值作为正式入口。
- [x] 2.6 增加 scene 静态测试：Sandbox 角色实例不覆盖旧配置入口。
- [x] 2.7 增加 scene 静态测试：CameraTest 角色实例不覆盖旧配置入口。
- [x] 2.8 增加测试：FullBody controller 和 Locomotion controller 仍引用同一角色配置根。
- [x] 2.9 增加测试：FullBody controller 和 Locomotion controller 仍连接同一角色 runtime component 链。
- [x] 2.10 增加测试：prefab 不新增第二 pipeline、第二 runner、第二 motion executor。
- [x] 2.11 增加测试：`可琳_Humanoid.prefab` 不再保留重复 `characterConfig` YAML 残留。
- [x] 2.12 增加测试：prefab 上 input buffer 引用仍可解析。
- [x] 2.13 增加测试：prefab 上 motion executor 引用仍可解析。
- [x] 2.14 增加测试：prefab 上 facing provider 引用仍可解析。
- [x] 2.15 增加测试：prefab 上 presenter 引用仍可解析。
- [x] 2.16 增加测试：scene override 不启用独立 Locomotion auto update 作为 FullBody 并行正式路径。
- [x] 2.17 增加测试：scene override 不新增第二 action animation presenter。

## 3. Prefab 迁移
- [x] 3.1 盘点 `可琳.prefab` 当前正式组件和旧字段。
- [x] 3.2 盘点 `可琳_Humanoid.prefab` 当前正式组件和旧字段。
- [x] 3.3 确认 `可琳.prefab` 的 Locomotion controller 指向正式 root config。
- [x] 3.4 确认 `可琳.prefab` 的 FullBody controller 指向正式 root config。
- [x] 3.5 确认 `可琳_Humanoid.prefab` 的 Locomotion controller 指向正式 root config。
- [x] 3.6 确认 `可琳_Humanoid.prefab` 的 FullBody controller 指向正式 root config。
- [x] 3.7 清理 `可琳.prefab` 上已退休或 legacy 配置字段的序列化残留。
- [x] 3.8 清理 `可琳_Humanoid.prefab` 上已退休或 legacy 配置字段的序列化残留。
- [x] 3.9 清理重复 `characterConfig` 或等价 YAML 残留。
- [x] 3.10 保持 input buffer 引用不丢失。
- [x] 3.11 保持 input adapter 引用不丢失。
- [x] 3.12 保持 motion executor 引用不丢失。
- [x] 3.13 保持 facing provider 引用不丢失。
- [x] 3.14 保持 presenter 引用不丢失。
- [x] 3.15 确认不新增 fallback 配置。
- [x] 3.16 确认不新增第二角色控制器路径。

## 4. Scene 迁移
- [x] 4.1 盘点 `Sandbox.unity` 中角色实例配置 override。
- [x] 4.2 清理 `Sandbox.unity` 中旧配置入口 override。
- [x] 4.3 盘点 CameraTest 场景中角色实例配置 override。
- [x] 4.4 清理 CameraTest 场景中旧配置入口 override。
- [x] 4.5 确认正式场景角色仍引用 Corin prefab 或等价正式装配。
- [x] 4.6 确认 scene 不新增并行角色 prefab。
- [x] 4.7 确认 scene 中角色实例仍连接 camera/input/motion runtime 引用。
- [x] 4.8 确认 scene 不覆盖出第二 pipeline、runner、motion executor 或 presenter。
- [x] 4.9 使用 Unity 只读 SerializedObject 复核迁移后的 prefab 和 scene 实例引用。

## 5. 验证
- [x] 5.1 运行 prefab/scene binding 静态测试。
- [x] 5.2 运行 `Tests.Editor.UnifiedCharacterStateMachineTests` 相关定向测试。
- [x] 5.3 运行 `ThirdPersonSimulation.Tests.FullBodyRollbackReplayTests` 相关定向测试。
- [x] 5.4 运行 `Tests.Editor.Simulation.LocalRollbackSynctestFoundationTests` 相关定向测试。
- [x] 5.5 运行 Locomotion config/root 相关定向测试。
- [x] 5.6 运行 Action animation profile 相关定向测试。
- [x] 5.7 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp.csproj --no-restore`。
- [x] 5.8 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp-Editor.csproj --no-restore`。
- [x] 5.9 运行 `openspec validate migrate-corin-character-prefab-bindings --strict --no-interactive`。
- [x] 5.10 运行 GitNexus `detect_changes()`，确认 affected symbols 和 execution flows 与本 change 范围一致。
