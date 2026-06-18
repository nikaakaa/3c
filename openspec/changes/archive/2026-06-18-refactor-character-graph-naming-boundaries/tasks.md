## 0. 范围确认
- [x] 0.1 读取本变更 `proposal.md`、`design.md` 和 spec delta。
- [x] 0.2 列出当前 Graph、Tree、SubmitterGraph、ActionBranch 命名使用点。
- [x] 0.3 确认本变更不新增行为树节点能力。

## 1. 命名映射
- [x] 1.1 固定 `CharacterGraphDefinition -> CharacterBehaviorGraphDefinition`。
- [x] 1.2 固定 `CharacterExecutionNodeTree -> CharacterBehaviorExecutionTree`。
- [x] 1.3 固定 `ActionBranch -> CommittedActionBranch`。
- [x] 1.4 确认 submitter chain 命名已由 `refactor-character-submitter-chain-boundary` 处理。
- [x] 1.5 更新设计文档和测试命名表。

## 2. Character Graph / Tree 收束
- [x] 2.1 重命名 authoring definition。
- [x] 2.2 重命名 runtime execution tree。
- [x] 2.3 更新 runtime tree validator 命名。
- [x] 2.4 更新 tests 路径和测试类名。
- [x] 2.5 增加 authoring graph 不作为 runtime runner 的静态测试。

## 3. CommittedActionBranch 边界
- [x] 3.1 重命名 `ActionBranch` 相关类型为 `CommittedActionBranch`。
- [x] 3.2 更新命名或文档避免 Action 成为顶层行为二分。
- [x] 3.3 更新相关测试名称。
- [x] 3.4 增加抽象层不依赖具体 Dodge 类型的边界测试。

## 4. 验证
- [x] 4.1 运行相关 Unity EditMode 测试。
- [x] 4.2 运行静态边界测试。
- [x] 4.3 运行 `openspec validate refactor-character-graph-naming-boundaries --strict --no-interactive`。
