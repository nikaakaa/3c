# 3C 项目 Agent 指令

根目录只保留执行规则和入口。项目业务、架构方向、技术取舍写在 `openspec/project.md`；OpenSpec 工作流由 `.agents/skills/openspec-*` 与 `openspec/config.yaml` 定义。

## 必读规则

- 读取文档必须显式使用 UTF-8：PowerShell 用 `Get-Content -Encoding UTF8`。
- 修改代码用系统文件工具，不通过 Unity MCP 写文件。
- 允许通过正式 CLI/executeMethod 按明确项目路径运行本机 Unity batchmode，任务结束后退出；保留主验收 Editor，CI 的 Unity 禁令不变。
- 搜索优先 `rg`。
- 不回退用户改动，不使用破坏性 git 命令。
- 生成代码尽量少写注释，只在关键复杂边界写少量注释。
- 默认不新增测试，除非用户明确要求；用户会自己做端到端验证。

## 当前项目口径

- 项目是求职向 Gameplay 客户端程序 demo。
- 重点是第三人称动作客户端：输入、角色控制、相机、动作状态、动画表现、战斗窗口、受击反馈、调试可视化。
- 网络只作为业务压力场景，不是主展示方向。
- 不做完整 PvPvE、MMO、纯网络框架、完整匹配、账号、背包、大地图、多职业、完整反作弊或完整断线重连，除非用户明确改目标。

## 架构和清理原则

- 不做 fallback 配置、兼容路径、临时桥接路径或分裂实现。
- 迁移和重构采取激进清理：旧数据、旧路径、旧命名、旧配置确认不用就直接删除。
- 需要绕过当前系统时必须停下来说明 tradeoff。
- BTSMTL 是 authoring 基座和参考，不是必须照搬的 runtime。
- 旧 Workbench、旧 locomotion/action/footphase/bodyclaim 等分裂数据源应迁移进节点、模块、Timeline 或删除。

## 文档随实施维护

- 实施改变已记录的业务行为、生成诊断产物、沉淀已确认经验或整理记录时，自动使用 `update-path-docs`，在当前任务内同步内容、业务分类和读取入口。
- 分类沿用 `docs/README.md` 的职责约定并核对实际文件；没有新增事实时不制造记录，普通咨询和只读审查不写文档。

## OpenSpec

- 涉及新能力、破坏性变更、架构调整、计划、proposal、spec 或含糊的大改动时，只有在你明确要求我执行 OpenSpec workflow（例如你明确指定使用 openspec skill）时，才读取 `openspec/project.md` 并使用对应 OpenSpec skill。
- OpenSpec 内容除固定格式关键字外使用中文。
- proposal 阶段只写设计文档，不写代码。
- 不把手动验证写进 OpenSpec `tasks.md`。
- 用户说已经 archive，视为用户已经测试过，直接归档。
- 当前架构真相以 `openspec/specs/` 和 `openspec/project.md` 为准；archive 只作为历史追溯。

## 回答要求

- 用户问“做了啥”“说说代码”时，要沿代码链路讲清楚，不只报文件名。
- 每个技术决策都要从业务角度说明取舍，尤其要比较不用其它方案的原因。
