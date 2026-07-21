# Design: Agent v15 AI Controller Authoring

## Context

当前Agent v14是Character Controller authoring的唯一自动写入口。AI核心change新增另一种authoring根，但不应复制Snapshot、Patch编译器、事务或MCP工具。版本升级必须发生在AI核心API稳定之后，并且一次完成，不能让v14和v15并存。

## Decision 1: 一个Schema，两个显式Domain

v15根保存显式domain：

```text
CharacterController
AIController
```

domain决定允许的根引用、Snapshot section和typed operation catalog。它不改变Graph、Node、Edge或PropertyPort identity，也不按文件路径、ScriptableObject类型或显示名猜测。

## Decision 2: 复用同一Command Pipeline

```text
v15 JSON Patch
  -> domain-aware lowerer
  -> immutable typed command plan
  -> shared preflight and asset transaction
  -> domain handler
  -> formal authoring API
  -> shared report
```

AI domain只增加新的typed command和handler，不建立AI Patch compiler、AI transaction或AI MCP action。Character与AI handler都必须通过同一Graph policy创建节点。

## Decision 3: Snapshot只投影Authoring真相

AI Snapshot读取Definition、Tree、Blackboard、Perception、Intent binding和generated identity。它不读取AI candidate state、当前Perception frame、Character mutable state或运行时node状态。Live Debug继续属于运行诊断，不进入Patch上下文。

## Decision 4: Validator复用领域校验

Agent Validator只组合以下正式结果：

- AI Definition完整性。
- AI Graph capability policy。
- AI Blackboard scope和类型。
- Perception binding。
- Intent与Character input/request catalog匹配。
- AI Compiler发布校验。

它不复制节点白名单、InputId规则或敌我推断规则。

## Decision 5: 原子替换v14

v15安装时同时更新Snapshot、Patch、Intent、Validator、MCP bridge、Editor窗口和技能。随后删除v14 reader、converter、alias和版本错误兼容分支。历史v14 JSON不是运行资产，不做迁移。

## Asset Boundary

本change不创建Corin训练AI资产。它只证明通用Agent链能作用于已存在或作者创建的合法AI Definition。具体Corin Patch和资产迁移属于`add-corin-training-ai-demo`，避免工具实现与业务样例任务互相掩盖。

## Risks And Tradeoffs

### Schema版本再次提升

代价是现有v14外部调用必须更新；收益是不会让v14在不同机器上同时表示Character-only和Character+AI两种含义。

### 独立change延后自动资产生成

AI核心完成后会短暂存在“可人工编辑但Agent尚不可写”的正式状态。该状态没有旁路：v14明确拒绝AI domain，Corin demo必须等待v15，而不是使用YAML或迁移器抢跑。

