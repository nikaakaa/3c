# 3C 项目 Agent 指令

根目录只保留执行规则和入口。项目业务、架构方向、技术取舍写在 `openspec/project.md`；OpenSpec 工具规则写在 `openspec/AGENTS.md`。

## 必读规则

- 读取文档必须显式使用 UTF-8：PowerShell 用 `Get-Content -Encoding UTF8`。
- 修改代码用系统文件工具，不通过 Unity MCP 写文件。
- 永远不要运行 Unity batchmode。
- 搜索优先 `rg`；仓库有 `.codegraph/` 时，理解代码优先用 CodeGraph。
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
- Taco 是 authoring 基座和参考，不是必须照搬的 runtime。
- 旧 Workbench、旧 locomotion/action/footphase/bodyclaim 等分裂数据源应迁移进节点、模块、Timeline 或删除。

## OpenSpec

- 涉及新能力、破坏性变更、架构调整、计划、proposal、spec 或含糊的大改动时，先读 `openspec/AGENTS.md` 和 `openspec/project.md`。
- OpenSpec 内容除固定格式关键字外使用中文。
- proposal 阶段只写设计文档，不写代码。
- 不把手动验证写进 OpenSpec `tasks.md`。
- 用户说已经 archive，视为用户已经测试过，直接归档。
- 当前架构真相以 `openspec/specs/` 和 `openspec/project.md` 为准；archive 只作为历史追溯。

## 回答要求

- 用户问“做了啥”“说说代码”时，要沿代码链路讲清楚，不只报文件名。
- 每个技术决策都要从业务角度说明取舍，尤其要比较不用其它方案的原因。

## CodeGraph

仓库根目录存在 `.codegraph/` 时，理解代码、定位符号、评估影响面优先使用 CodeGraph。

- 问“怎么工作”“在哪里”“影响面”时优先 `codegraph_explore`。
- 读取具体符号或文件时优先 `codegraph_node`。
- 如果工具不可用，可用 shell：`codegraph explore "<问题或符号>"`、`codegraph node <符号或文件>`。
- CodeGraph 是代码理解入口，不代替编译器、Unity 控制台和用户端到端验证。
