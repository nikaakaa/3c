# Change: 对齐角色配置作者入口规格

## Why
当前代码和资产已经开始以 `CharacterConfigSO` 作为角色配置根，但现有规格中仍存在目录命名与正式入口口径不一致的问题，例如 `project-structure` 仍描述旧 `Animacer` 目录，而较新的 FullBody 配置规格已要求旧拼写不得作为正式入口。

本变更先收敛规格和验证口径，避免后续改 `.asset`、prefab、scene override 时同时承担规格冲突、资产迁移和运行时装配三种风险。

## What Changes
- 明确 `CharacterConfigSO` 是角色作者总入口，controller 上旧平铺字段只能作为迁移残留，不能作为正式入口或 fallback。
- 明确正式 Animancer 配置目录为 `Assets/Configs/3C/Animation/<角色>/Animancer/...`，旧 `Assets/Configs/3C/Animacer/...` 不再作为正式入口。
- 增加配置作者入口的静态校验要求：检查旧目录、旧字段、测试命名资产和重复正式入口。
- 将本变更限定为规格、校验和测试口径，不迁移具体 Corin 资产，不修改 prefab 或 scene。

## Current Findings
- `CharacterConfigSO` 已包含 `stateMachine`、`movement`、`locomotionAnimation`、`fullBodyStateRequestPolicy`、`dodgeAction`、`fullBodyActionAnimation`、`animancerRigVariant`、`inputActions`、`moveAction`、`runAction`、`lookAction` 和 `cameraConfig`。
- `PlayerLocomotionController` 和 `PlayerFullBodyActionController` 已从 `characterConfig` 读取正式配置，但代码中仍保留迁移用旧平铺字段。
- `project-structure` 现有规格仍描述 `Assets/Configs/3C/Animacer/<角色>/`，而 `fullbody-config-boundaries` 已将旧 `Animacer` 拼写标记为不得作为正式入口。
- prefab/scene 中还存在旧字段序列化值，因此必须先明确“旧字段有数据不等于正式 fallback”。

## Detailed Scope
| Area | This change owns | This change must not own |
| --- | --- | --- |
| 规格口径 | 统一根配置、正式目录、旧目录迁移残留的验收语言。 | 移动资产或修改 prefab。 |
| 静态校验 | 增加测试报告旧目录、旧平铺字段、第二入口和测试命名资产。 | 自动清理旧字段值。 |
| Runtime 读取 | 证明正式 runtime 不从旧字段 fallback。 | 重写 controller 装配或新建 runtime host。 |
| 后续衔接 | 为 asset/prefab 迁移提供稳定标准。 | 把 asset/prefab 迁移并入本 change。 |

## Impact
- Affected specs:
  - `character-config-root`
  - `fullbody-config-boundaries`
  - `project-structure`
- Affected code/tests:
  - `Assets/Scripts/Character/Config/CharacterConfigSO.cs`
  - `Assets/Scripts/Character/Movement/Runtime/PlayerLocomotionController.cs`
  - `Assets/Scripts/Character/Action/FullBody/Runtime/PlayerFullBodyActionController.cs`
  - `Assets/Tests/Editor/CharacterConfigRootTests.cs`
  - `Assets/Tests/Editor/FullBodyConfigAuthoringLayoutTests.cs`
- Does not modify:
  - `Assets/Configs/3C/**/*.asset`
  - `Assets/Prefabs/Character/*.prefab`
  - `Assets/Scenes/**/*.unity`

## Dependencies
- SHOULD run after the current completed character frame pipeline changes are verified.
- SHOULD run before `migrate-corin-character-config-assets`.
- SHOULD run before prefab/scene binding migration.

## Stop Conditions
- 如果需要编辑 `.asset`、`.prefab` 或 `.unity` 才能让本 change 通过，必须停止并转入后续迁移 change。
- 如果现有 runtime 需要旧平铺字段作为 fallback 才能通过测试，必须停止并先修正正式配置根解析。
- 如果发现需要新增第二个配置根、Resources 加载或硬编码默认路径，必须停止并重新评审设计。
- 如果统一 Animancer Presenter 的组件形态成为阻塞，必须停止并等待 `refactor-unified-animancer-presenter`。

## User Verification
用户可以通过以下方式确认本 change 完成：

- 搜索 `Animacer`、`Statemachine`、`Pramater`，确认它们不再被测试或规格视为正式入口。
- 打开 `CharacterConfigSO` 和 controller 相关测试，确认旧平铺字段不会作为 fallback。
- 运行本 change 的定向 EditMode 测试、C# build 和 OpenSpec strict validate。
