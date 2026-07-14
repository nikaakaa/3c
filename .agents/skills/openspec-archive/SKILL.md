---
name: openspec-archive
description: 归档完成的本项目 OpenSpec change，并将 delta 合并进 current specs。Use when 用户明确要求 archive、归档某个 change，或表示某项 OpenSpec 工作已测试完成。
---

# OpenSpec Archive

1. 确认唯一的 change-id；若用户未指定，运行 `openspec list`，只在无法从上下文确定时询问。
2. 用户要求 archive 即表示其已完成端到端测试，不额外要求或记录人工验证。
3. 执行 `openspec archive <change-id> --yes`，确认 change 已移动到 `openspec/changes/archive/`，并核对 current specs 的更新结果。
4. 执行 `openspec validate --all --strict --no-interactive`。如出现校验错误，定位并修正归档后 spec 的一致性问题。
5. 汇报归档路径、更新的 current spec 和校验结果；不保留旧 change 的兼容入口。
