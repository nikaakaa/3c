## 1. 准备
- [x] 1.1 读取本变更 `proposal.md`。
- [x] 1.2 读取本变更 `design.md`。
- [x] 1.3 读取本变更 `tasks.md`。
- [x] 1.4 读取 `character-config-root` spec。
- [x] 1.5 读取 `fullbody-config-boundaries` spec。
- [x] 1.6 读取 `project-structure` spec。
- [x] 1.7 对 `CharacterConfigSO` 运行 GitNexus impact analysis，并记录 blast radius。
  - 记录：LOW，1 个 direct caller，1 条 affected process。
- [x] 1.8 对 `PlayerLocomotionController` 运行 GitNexus impact analysis，并记录 blast radius。
  - 记录：HIGH，7 个 direct callers，2 条 rollback/debug affected processes。
- [x] 1.9 对 `PlayerFullBodyActionController` 运行 GitNexus impact analysis，并记录 blast radius。
  - 记录：LOW，0 个 direct callers，0 条 affected processes。
- [x] 1.10 读取 `refactor-unified-animancer-presenter` 的 proposal/design/tasks，确认 presenter 目录与本变更不冲突。
- [x] 1.11 搜索 `Animacer`、`Statemachine`、`Pramater` 在 specs、changes、assets 中的现有出现点。
- [x] 1.12 搜索 `runAnimationConfig`、`config`、`stateMachineDefinition`、`interruptPolicySet`、`dodgeActionConfig` 的正式 runtime 读取点。

## 2. 自动测试先行
- [x] 2.1 增加静态测试：正式 Animancer 目录不使用旧 `Animacer` 拼写。
- [x] 2.2 增加静态测试：正式角色根位于 `Assets/Configs/3C/Character/Corin/CorinCharacterConfig.asset`。
- [x] 2.3 增加静态测试：旧 `CharacterConfig.asset` 不作为第二正式入口。
- [x] 2.4 增加静态测试：controller 旧平铺字段不得被正式运行时解析为 fallback。
- [x] 2.5 增加静态测试：正式配置不得引用 `TestTurnback`、`turnback` 或 `testTurn` 命名资产。
- [x] 2.6 增加静态测试：新增配置 Module 必须从 `CharacterConfigSO` 增加命名子模块入口。
- [x] 2.7 增加测试：缺失 `CharacterConfigSO.Movement` 时不会读取 `PlayerLocomotionController.config`。
- [x] 2.8 增加测试：缺失 `CharacterConfigSO.LocomotionAnimation` 时不会读取 `PlayerLocomotionController.runAnimationConfig`。
- [x] 2.9 增加测试：缺失 `CharacterConfigSO.StateMachine` 时不会读取旧 `stateMachineDefinition`。
- [x] 2.10 增加测试：FullBody request policy 缺失时不会从旧平铺字段静默补齐。
- [x] 2.11 增加测试：Dodge action config 缺失时不会从旧平铺字段静默补齐。
- [x] 2.12 增加测试：OpenSpec 规格中旧目录若出现，必须标记为 legacy 或迁移残留。

## 3. 实现
- [x] 3.1 调整配置作者入口静态校验用例。
- [x] 3.2 调整项目结构静态校验用例。
- [x] 3.3 若现有 spec 文案与正式目录冲突，更新本 change 的 spec delta，不直接改已归档规格。
- [x] 3.4 确认不编辑 `.asset`、`.prefab` 或 `.unity`。
- [x] 3.5 确认不新增 fallback 配置。
- [x] 3.6 将正式 Animancer 目录判断集中在测试 helper 或等价静态校验工具。
- [x] 3.7 将旧目录命名判断集中在测试 helper 或等价静态校验工具。
- [x] 3.8 将旧平铺字段 fallback 断言接入现有 CharacterConfigRoot 测试。
- [x] 3.9 将 project-structure 目录冲突断言接入现有 authoring layout 测试。
- [x] 3.10 确认实现不修改 `CharacterFramePipeline`、runner、motion executor 或 presenter。

## 4. 验证
- [x] 4.1 运行配置作者入口相关 EditMode 测试。
- [x] 4.2 运行 `Tests.Editor.CharacterConfigRootTests` 相关定向测试。
- [x] 4.3 运行 `Tests.Editor.FullBodyConfigAuthoringLayoutTests` 相关定向测试。
- [x] 4.4 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp.csproj --no-restore`。
- [x] 4.5 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp-Editor.csproj --no-restore`。
- [x] 4.6 运行 `openspec validate align-character-config-authoring-contracts --strict --no-interactive`。
- [x] 4.7 运行 GitNexus `detect_changes()`，确认 affected symbols 和 execution flows 与本 change 范围一致。
