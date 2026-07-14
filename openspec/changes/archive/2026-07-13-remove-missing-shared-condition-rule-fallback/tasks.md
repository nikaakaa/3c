## 1. 基线与资产盘点

- [x] 1.1 盘点 `BaseEdge` 的 inline/shared ConditionRuleGraph 序列化字段与 resolved 访问入口
- [x] 1.2 盘点 StateMachine Transition 与 BT Composite child edge 的创建、初始化和校验调用点
- [x] 1.3 盘点 `EnsureConditionRuleGraphs()` 与编辑器 Open Rule 的自动创建路径
- [x] 1.4 扫描现有 Graph asset 的 inline、shared、双持有、缺失与类型错误 ConditionRuleGraph 引用
- [x] 1.5 记录需要迁移的有效 edge 数量与必须作者处理的 invalid edge

## 2. 显式 ownership 数据模型

- [x] 2.1 定义持久化 ConditionRuleGraph ownership 枚举与 invalid 状态
- [x] 2.2 将 ownership 写入 `BaseEdge` 正式序列化数据
- [x] 2.3 让 resolved graph 只在 ownership 与数据来源匹配时可用
- [x] 2.4 让新建合法 StateMachine Transition edge 显式创建 Inline ownership
- [x] 2.5 让新建合法 BT child edge 显式创建或保留无条件 edge 的正式状态
- [x] 2.6 让显式 Set Shared 清理 inline 真数据并写入 Shared ownership
- [x] 2.7 让显式 Use Inline 创建 inline 图并写入 Inline ownership
- [x] 2.8 阻止 inline/shared 双持有数据进入 resolved 状态

## 3. 迁移与校验

- [x] 3.1 实现 editor-only 的 ConditionRuleGraph ownership 迁移入口
- [x] 3.2 将有效 inline 引用迁移为 Inline ownership
- [x] 3.3 将有效 shared ConditionRuleGraph asset 引用迁移为 Shared ownership
- [x] 3.4 对缺失 shared asset、错误类型、双持有或无法判断来源的 edge 报告错误
- [x] 3.5 迁移不得创建 inline 图、复制 shared 图或清除断裂 shared 引用
- [x] 3.6 更新 NestedGraphValidation 以报告 ownership 与 resolved data 不匹配

## 4. 自动修复路径清理

- [x] 4.1 让 `StateMachineGraph.CheckInit()` 只补齐明确的新 Inline edge
- [x] 4.2 删除 Shared 引用缺失时由 `EnsureConditionRuleGraphs()` 自动创建 inline 图的路径
- [x] 4.3 删除编辑器刷新或校验自动清理 shared 引用的路径
- [x] 4.4 让 Open Rule 遇到 Shared invalid edge 时显示错误且不创建 inline 图
- [x] 4.5 保留 Replace Shared 与 Use Inline 作为作者显式 ownership 切换命令

## 5. Runtime 错误闭合

- [x] 5.1 让 StateMachine runtime 对 invalid ConditionRuleGraph edge 记录来源错误并使条件失败
- [x] 5.2 让 BT Composite runtime 对 invalid ConditionRuleGraph edge 记录来源错误并禁止进入该 child
- [x] 5.3 确认 invalid edge 不会退回默认 true、同层规则、BoolPort 或旧 IfNode 条件
- [x] 5.4 在 debug/validator 输出中显示 edge、owner、ownership 与引用错误原因

## 6. Corin 与规格收口

- [x] 6.1 扫描 Corin RootTree 与所有正式角色 Graph，确认不存在 unresolved shared ConditionRuleGraph
- [x] 6.2 确认 Transition 与 BT edge 的有效 inline/shared 条件图只保留一份真数据
- [x] 6.3 更新 `btsmtl-sm-node-authoring` 的 shared 条件图缺失 requirement
- [x] 6.4 运行 `openspec validate remove-missing-shared-condition-rule-fallback --strict --no-interactive`
