## Context
现有运行时代码已经从 `CharacterConfigSO` 读取正式子配置，但 prefab YAML 中仍保留旧平铺字段。与此同时，规格中对 Animancer 正式目录存在新旧口径冲突：旧规格还提到 `Animacer`，新配置边界已要求旧拼写不得作为正式入口。

## Goals
- 先统一作者入口、目录命名和校验口径。
- 为后续资产迁移和 prefab 迁移提供稳定验收标准。
- 保持不新增 fallback 配置、不新增第二角色控制器路径。

## Non-Goals
- 不移动或编辑任何 `.asset`。
- 不编辑任何 `.prefab` 或 `.unity`。
- 不合并 Animancer Presenter；该部分由 `refactor-unified-animancer-presenter` 负责。
- 不改变 `CharacterFramePipeline` 运行顺序。

## Decisions
- `CharacterConfigSO` 是唯一正式角色配置根，旧 controller 字段只允许作为迁移遗留数据。
- 正式 Animancer 播放资产目录使用 `Assets/Configs/3C/Animation/<角色>/Animancer/...`。
- 旧 `Animacer`、`Statemachine`、`Pramater` 目录只能被静态校验识别为迁移残留或待删除对象，不能作为正式运行时入口。
- 本变更只建立校验和规格，不在同一 change 中修改 prefab/scene，避免资产改动掩盖规格冲突。

## Boundary Model
```text
CharacterConfigSO
  -> StateMachine config
  -> Movement config
  -> Locomotion animation config
  -> FullBody action/request config
  -> Action animation / Animancer rig variant config
  -> Input config
  -> Camera config

Controller legacy fields
  -> migration residue only
  -> no fallback
  -> no new module expansion
```

## Validation Matrix
| Check | Evidence |
| --- | --- |
| Root config is official entry | Static tests inspect `CharacterConfigSO` and runtime resolution code. |
| Old directories are not official | Static tests scan formal config paths and known legacy spellings. |
| Old controller fields are not fallback | Tests create missing-root cases and expect diagnostics or failure, not fallback. |
| No asset/prefab mutation | Git diff contains only tests/docs/spec deltas for this change. |

## Archive Notes
Archive should merge these deltas into `character-config-root`, `fullbody-config-boundaries` and `project-structure` only after the static tests prove the chosen formal paths. If older requirement text still names `Animacer` as formal, archive should replace that wording with the new `Animation/<角色>/Animancer` path.

## Risks / Mitigations
- 风险：旧场景仍有平铺字段值，清理过早可能丢引用。
  - 缓解：本变更不清理资产，只增加可观察校验。
- 风险：现有规格有冲突，实施时难以判定通过标准。
  - 缓解：先将正式目录和唯一入口写成新增 requirement，后续 archive 时再合并旧 requirement。
- 风险：测试只扫路径文本，无法证明运行时不 fallback。
  - 缓解：同时增加 runtime config resolution 测试，覆盖缺失根配置时不使用旧字段。

## Validation
- 运行配置作者入口静态测试。
- 运行 C# build。
- 运行 `openspec validate align-character-config-authoring-contracts --strict --no-interactive`。
