# OpenSpec 项目规则

这个文件只保留本项目需要的 OpenSpec 规则，不使用默认长模板。

## 什么时候写 proposal

- 新能力、破坏性变更、架构调整、跨模块重构、数据结构变化，必须先写 proposal。
- 小 bug 修复、明显拼写、文档瘦身、已批准 change 的实现，不需要新 proposal。
- 如果需求含糊，先查 `openspec/project.md`、active changes 和相关代码，再指出冲突或缺口。

## 必读入口

- 先读 `openspec/project.md`。
- 再跑 `openspec list` 和 `openspec list --specs`。
- 当前 `openspec/specs/` 可能为空；为空时不要假装有 current spec，按 `project.md` 和 active changes 判断。
- archive 只作为历史，不作为当前架构依据。

## 写法

- 除固定标题关键字外，proposal、design、tasks、spec delta 都用中文。
- `change-id` 使用 kebab-case、动词开头，例如 `add-`、`refactor-`、`remove-`。
- 每个 change 至少包含 `proposal.md`、`tasks.md` 和一个 `specs/<capability>/spec.md`。
- 架构跨多个系统、引入新模型或存在 tradeoff 时写 `design.md`。
- spec delta 使用 `## ADDED|MODIFIED|REMOVED|RENAMED Requirements`。
- 每个 requirement 必须至少有一个 `#### Scenario:`。

## tasks.md

- 任务颗粒度要细，按可验证的小闭环拆。
- 不把手动验证写进 `tasks.md`。
- 不默认写测试任务，除非用户明确要求测试。
- 实现完成后才能把任务勾成 `- [x]`。

## 项目禁区

- 不新增 fallback 配置。
- 不新增并行旧路径或临时桥接路径。
- 迁移时旧数据、旧路径、旧命名确认不用就删除。
- 不恢复旧 Workbench、旧 locomotion/action/footphase/bodyclaim 分裂数据源。
- 不跑 Unity batchmode。
- 不用 MCP 写文件。

## 命令

```powershell
openspec list
openspec list --specs
openspec validate <change-id> --strict --no-interactive
openspec validate --all --strict --no-interactive
```

读取文档必须显式 UTF-8：

```powershell
Get-Content -Encoding UTF8 openspec\project.md
```
