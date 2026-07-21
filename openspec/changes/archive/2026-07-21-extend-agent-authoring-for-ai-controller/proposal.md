# Change: 扩展 Agent Authoring 支持 AI Controller

## Why

`add-btsmtl-ai-controller-authoring`将建立AI Definition、AIControllerTree、Graph capability、AI Blackboard、Perception与Intent的正式authoring API，但刻意不修改当前`agent-character-controller-synthesis.v14`。如果在AI核心尚未稳定时同步扩展Agent，Timeline Marker/Curve与AI会同时修改Snapshot、Patch、lowerer、handler、validator、MCP和技能，无法并行实施，也容易复制尚未稳定的AI规则。

本change在AI核心完成后单独把唯一Agent schema从v14原子提升为v15。v15使用显式domain discriminator支持Character Controller与AI Controller，复用同一typed command plan、事务、Graph mutation API和报告合同；不引入AI专用Agent工具，也不生成具体Corin训练资产。

## What Changes

- 将唯一Agent Snapshot、Patch、Intent与Validation根schema从v14提升为v15，并删除v14及更早reader、converter与alias。
- 增加Character Controller与AI Controller根domain discriminator，禁止按资产类型猜测domain。
- Snapshot输出AIControllerDefinition、AIControllerTree、Graph capability、AI Blackboard、Perception binding、Character input/request binding和generated AI Program identity。
- Patch增加创建与配置AI Definition、AI Tree、AI Blackboard、Configured Candidate、Observation、Memory和Intent节点的typed operation。
- Lowerer继续生成唯一immutable command plan；dry-run与apply消费同一plan。
- Handler只调用AI核心正式Definition、Graph、Blackboard、Perception与Intent authoring API。
- Validator复用AI Graph policy、AI Compiler和Character input catalog校验，不复制节点白名单或业务规则。
- MCP bridge与BTSMTL Agent技能只透传同一v15 generic transaction，不增加AI专用action。

## Scope

### In Scope

- Agent v15 schema、domain discriminator和版本删除。
- AI Controller Snapshot、Patch、typed command、handler、validator与report。
- MCP bridge和项目BTSMTL Agent技能更新。
- Character Controller现有v14能力无损迁移到v15。

### Out of Scope

- AI runtime、Perception、AIIntentProgram、Control Source或Session实现。
- Corin Training AI Definition、Tree、Program资产与训练敌人配置。
- Team、Faction、寻路、Combat、Authority AI或Fixed AI。
- YAML、SerializedProperty、反射、临时菜单或第二个Agent工具。

## Impact

- Affected specs:
  - `agent-character-controller-synthesis`
  - `btsmtl-agent-authoring-mcp-bridge`
  - 新增`agent-ai-controller-synthesis`
- Affected Editor:
  - Agent Snapshot exporter、Patch DTO、lowerer、handler catalog、validator、report与窗口。
  - MCP bridge与`.codex/skills/btsmtl-agent-authoring`。
- Breaking changes:
  - 只接受v15；v14及更早输入明确失败。
  - AI Controller只能通过v15 AI domain操作，不允许Character domain访问。

## Current Spec Comparison

- 当前`agent-character-controller-synthesis`和MCP bridge已经统一为v14，完整支持Character、MotionWarp、Marker与typed Curve Channel，但没有AI domain。
- `add-btsmtl-ai-controller-authoring`明确保持v14不变，并提供本change需要的唯一AI authoring与validation API。
- 本change不修改Runtime或Character Program，因此不会形成Agent驱动Gameplay的第二条执行链。

## Dependencies And Sequencing

- 硬依赖`add-btsmtl-ai-controller-authoring`完成，不能在AI Definition、Graph policy和Intent API尚未稳定时复制临时合同。
- 依赖已经完成的`add-timeline-animation-marker-sync`提供完整v14基线。
- 完成本change后，`add-corin-training-ai-demo`才能通过正式v15 Agent事务生成AI资产。

## Success Criteria

- 唯一Agent schema为v15，Character与AI根由显式domain区分。
- Character v14已有能力在v15中保持同一正式authoring语义。
- Agent可以export、dry-run、apply、re-export和validate任意合法AI Controller Definition与Tree。
- AI节点创建和连接只调用AI核心正式authoring API并复用统一Graph policy。
- v14及更早reader、converter、alias、双写输出和AI专用工具均不存在。

