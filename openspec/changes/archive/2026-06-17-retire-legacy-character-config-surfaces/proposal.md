# 退役旧配置面与兼容字段风险

## 背景

Locomotion 与 FullBody Action 的正式路径已经收敛到角色根配置、角色专属 Action 配置目录、Locomotion 状态图与统一帧运行时。但是当前仓库里仍存在几类风险：

- 基础规格仍有旧 FullBody 主树、旧目录、旧 Host Adapter 的描述，容易误导后续实现。
- 正式运行时脚本仍保留若干 `[Obsolete]` 序列化字段、兼容属性或退役 Adapter，虽然测试已经阻止 fallback，但这些字段仍可能被未来配置或自动化重新使用。
- Corin prefab/scene 中存在旧字段的空序列化痕迹，虽然值为 null，但会让“字段是否仍可配置”变得不清楚。
- 新旧配置迁移依赖 GUID 复用与目录移动，需要明确哪些是正式配置，哪些只是历史迁移痕迹。

本变更只处理“废弃配置/兼容字段风险清理”。它不新增动作、不改变跑步/冲刺/Dodge 行为、不引入新的分裂运行路径。

## 变更内容

- 建立废弃面清单，将旧字段、旧目录、旧 Adapter/Presenter、兼容视图分为：必须删除、允许只读保留、仅测试/迁移可见。
- 退役正式运行时中的旧平铺配置入口，使 `CharacterConfigSO` 及其子配置成为唯一正式来源。
- 清理 Corin prefab/scene 中旧字段的空序列化痕迹，并用测试阻止旧字段值或旧字段键回流。
- 明确正式目录只允许角色专属 Action 配置、Locomotion 状态图、统一 Animancer Presenter 与 CharacterFrame runtime。
- 更新规格，避免后续提案继续引用 `Action/FullBody`、`StateMachine/FullBody`、`PlayerFullBodyActionController`、旧 tick adapter 或旧 presenter 作为正式路径。
- 保留必要的只读兼容面，例如 `FullBodyStateView` 只能作为诊断/观察视图，不能参与动作仲裁、生命周期推进或配置解析。

## 非目标

- 不实现 LightAttack、Jump、UpperBody 或新的动作模板。
- 不修改当前 Shift 点按冲刺后进入 Run 的主线行为。
- 不修改后撤 Dodge 的输入保留与完整播放规则。
- 不切换状态机框架，不新增 fallback 配置。
- 不归档历史 OpenSpec 变更；归档应在用户确认对应变更已测试后单独执行。

## 影响范围

- 规格：`character-config-root`、`project-structure`、`character-runtime-ports`、`fullbody-action-framework`。
- 代码清理候选：`PlayerLocomotionController`、`FullBodyActionRuntime`、退役 tick adapter、旧 Animancer presenter、旧目录下脚本或配置残留。
- 资产清理候选：Corin prefab/scene 的旧序列化键、旧 FullBody 配置目录、旧 FullBody 状态机资产引用。
- 测试：配置根测试、prefab/scene 绑定测试、作者ing 布局测试、运行时端口/Presenter 静态约束测试。

## 依赖与顺序

- `refactor-locomotion-action-state-graphs` 已完成但尚未归档；本变更实现前应以它的当前结果为基线，并在归档或合并时避免规格冲突。
- `retire-player-fullbody-action-controller` 已完成；本变更不恢复该 Controller 的任何职责。
- `formalize-character-frame-module-architecture` 仍有未完成任务；本变更只清理已确定废弃的配置面，不抢先实现未审批的新模块路径。

## 待确认默认值

- 默认不在本变更中重命名 `CharacterConfigSO.StateMachine` 字段；本变更只确保它指向正式 Locomotion 图，避免序列化迁移风险扩大。
- 默认删除或硬隔离退役 Adapter/Presenter 的正式使用面；若发现仍有测试或迁移工具依赖，则只允许作为 Editor/test-only 诊断面保留。
