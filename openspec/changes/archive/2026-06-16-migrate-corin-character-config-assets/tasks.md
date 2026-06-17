## 1. 准备
- [x] 1.1 读取本变更 `proposal.md`。
- [x] 1.2 读取本变更 `design.md`。
- [x] 1.3 读取本变更 `tasks.md`。
- [x] 1.4 读取 `align-character-config-authoring-contracts` 的 proposal/design/tasks。
- [x] 1.5 读取 `character-config-root` spec。
- [x] 1.6 读取 `fullbody-config-boundaries` spec。
- [x] 1.7 读取 `project-structure` spec。
- [x] 1.8 对 `CharacterConfigSO` 运行 GitNexus impact analysis，并记录 blast radius。
  - 记录：LOW，1 个 direct caller，1 条 affected process。
- [x] 1.9 读取 `CorinCharacterConfig.asset`。
- [x] 1.10 读取正式子资产的 `.meta` GUID。
- [x] 1.11 搜索正式根引用链中的旧目录和测试命名资产。
- [x] 1.12 确认本 change 不需要编辑 prefab 或 scene。

## 2. 自动测试先行
- [x] 2.1 增加测试：`CorinCharacterConfig.asset` 可加载。
- [x] 2.2 增加测试：StateMachine 引用非空且位于正式目录。
- [x] 2.3 增加测试：Movement 引用非空且位于正式目录。
- [x] 2.4 增加测试：LocomotionAnimation 引用非空且位于正式目录。
- [x] 2.5 增加测试：FullBody request policy 引用非空且位于正式目录。
- [x] 2.6 增加测试：Dodge action config 引用非空且位于正式目录。
- [x] 2.7 增加测试：Animancer rig variant 引用非空且位于正式 `Animation/Corin/Animancer` 目录。
- [x] 2.8 增加测试：InputActions 与 InputActionReference 引用非空且位于正式目录。
- [x] 2.9 增加测试：Camera config 引用非空且位于正式目录。
- [x] 2.10 增加测试：正式引用链不包含旧 `Animacer`、`Statemachine`、`Pramater` 目录。
- [x] 2.11 增加测试：正式引用链不包含 `TestTurnback`、`turnback`、`testTurn` 命名资产。
- [x] 2.12 增加测试：正式根引用链不存在 dangling GUID。
- [x] 2.13 增加测试：默认 Corin 根不要求 Humanoid rig variant。
- [x] 2.14 增加测试：状态机资产不引用 Animancer TransitionAsset。
- [x] 2.15 增加测试：Dodge motion 参数只来自正式 Dodge action config。
- [x] 2.16 增加测试：InputActionReference 的目标 action 与 `CharacterInput.inputactions` 一致。

## 3. 资产迁移
- [x] 3.1 盘点 `CorinCharacterConfig.asset` 当前全部子引用。
- [x] 3.2 盘点正式子资产 `.meta` GUID。
- [x] 3.3 修正缺失或错误的根配置子引用。
  - 记录：自动校验确认无需修改资产。
- [x] 3.4 修正正式配置中指向旧目录的引用。
  - 记录：自动校验确认正式引用链不含旧目录。
- [x] 3.5 修正正式配置中指向测试命名资产的引用。
  - 记录：自动校验确认正式引用链不含测试命名资产。
- [x] 3.6 修正 dangling GUID 或空正式引用。
  - 记录：自动校验确认无 dangling GUID 或空正式引用。
- [x] 3.7 确认状态机资产不保存动作动画 Profile 或 Animancer TransitionAsset。
- [x] 3.8 确认动作逻辑资产不保存角色具体 AnimationClip。
- [x] 3.9 确认 Locomotion 动画配置不保存状态机拓扑。
- [x] 3.10 确认 InputActionReference 与 InputActionAsset 匹配。
- [x] 3.11 确认 Camera config 位于正式目录。
- [x] 3.12 确认不编辑 prefab 或 scene。
- [x] 3.13 确认不新增 fallback 配置。
- [x] 3.14 若移动资产，确认 `.meta` GUID 保持或所有正式引用已更新。
  - 记录：本 change 未移动资产。

## 4. 验证
- [x] 4.1 运行 Corin 配置闭环相关 EditMode 测试。
- [x] 4.2 运行 `Tests.Editor.CharacterConfigRootTests` 相关定向测试。
- [x] 4.3 运行 `Tests.Editor.FullBodyConfigAuthoringLayoutTests` 相关定向测试。
- [x] 4.4 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp.csproj --no-restore`。
- [x] 4.5 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp-Editor.csproj --no-restore`。
- [x] 4.6 运行 `openspec validate migrate-corin-character-config-assets --strict --no-interactive`。
- [x] 4.7 运行 GitNexus `detect_changes()`，确认 affected symbols 和 execution flows 与本 change 范围一致。
