# Design: Agent Authoring 显式同步 JSON 文档

## Context

当前实现把MCP工具数量压缩为一个，但AI-facing语言仍是低层mutation指令：

```text
LLM
  -> patch_json.operations[]
  -> AgentPatchCommandLowerer
  -> AgentPatchCommandPlan
  -> AgentPatchCompileSession
  -> typed handler
  -> formal BTSMTL authoring API
```

后半段已经解决preflight、identity、ownership、Undo、rollback和validator，应该继续保留。问题位于前半段：AI需要编排大量`ensure_*`、`configure_*`、`link_*`和`delete_*`操作，并直接承担内部施工顺序。

用户要求的双向绑定不是实时数据绑定，而是带基线的显式同步：平时Unity树独立编辑；AI开始工作时才从当前树checkout一份JSON；AI保存JSON只标脏；显式dry-run/apply后再从最终树反向规范化JSON。

## Goals

- 让AI只编辑一份持久化、结构化、可读的JSON文档。
- 保持普通BTSMTL资产为唯一正式authoring真相和人工编辑面。
- 保持现有typed handler、正式authoring API、validator和单Undo事务。
- 准确区分树变化、文档变化和双边冲突。
- 确保任何重操作只由明确action触发。
- 让CharacterController与AIController共享同一Document、Reconciler和事务生命周期。

## Non-Goals

- 不做实时文件watcher或自动apply。
- 不在selection、Inspector、domain reload或AssetDatabase事件中build。
- 不把JSON打进Player或运行时读取。
- 不做自动三方merge。
- 不保留Intent、Macro、外部Patch或bootstrap兼容入口。
- 不扩大Agent对Presentation、Body Motion或generated analysis的写权限。

## Decision 1: JSON是工作文档，不是第二份authoring真相

正式权威保持：

```text
普通BTSMTL / Character / AI Unity assets
```

JSON只表示某次AI编辑会话的目标结构。apply前，Unity资产不受JSON影响；apply成功后，以正式树重新导出的JSON为规范结果。

如果把JSON设为第二份正式真相，就必须让Graph Editor和Timeline Editor所有人工编辑反向写JSON，否则立即形成双主线。本变更选择工作文档，可以保留现有人工工具，也能让AI直接编辑文件。

## Decision 2: 文档位置固定在Unity项目外部资产目录

Document Store拥有唯一确定性路径：

```text
<UnityProject>/AgentAuthoring/Documents/<domain>/<root-key>.btsmtl.json
```

`root-key`由显式domain、规范root asset path和已有root identity确定。调用方不传任意文档路径，不通过文件选择器或当前selection寻找文档。

该目录位于`Assets/`之外：

- 不触发AssetDatabase import。
- 不进入Player或AssetBundle。
- 不成为Unity authoring asset。
- 可以跨Editor重启保留AI未应用修改。

该目录是派生工作区，不进入版本控制；Git历史仍记录最终Unity资产。Document Store不得扫描其它目录寻找替代文件。

## Decision 3: 文档分为同步头、可编辑正文与只读上下文

外部schema使用新的语义版本：

```text
btsmtl-agent-authoring-document.v1
```

结构分为：

```json
{
  "schemaVersion": "btsmtl-agent-authoring-document.v1",
  "domain": "CharacterController",
  "rootIdentity": "...",
  "sync": {
    "baseSourceRevision": "...",
    "baseContentHash": "..."
  },
  "editable": {},
  "context": {}
}
```

- `sync`由service写入，AI不得修改。
- `editable`完整表达Agent正式可写的Graph、StateMachine、Timeline、Blackboard、Perception和Intent实体。
- `context`输出Input、ActionProfile、Capability、Presentation、Body Motion、Foot Analysis与generated product等只读信息；Reconciler拒绝被修改的只读字段。
- 已有实体使用stable authoring identity。
- 新实体使用文档局部identity；apply成功后由反向导出替换为真实stable identity。
- editable集合是完整目标集合，删除已有可写实体通过从集合中移除表达；read-only或Agent不支持的实体不因缺席而删除。

## Decision 4: 使用严格解析和规范化内容hash

Document parser必须拒绝：

- 未知字段。
- 重复JSON属性。
- 非法discriminator。
- 缺失必需字段。
- 修改service-owned同步字段。
- 非有限数值、非法curve、非法identity与domain不匹配。

`baseContentHash`和dry-run返回的`documentHash`都基于规范化语义内容，不基于缩进、换行或属性输入顺序。Canonical writer固定：

- UTF-8无BOM。
- 稳定属性顺序。
- 稳定entity排序。
- 明确数值格式。
- 不输出默认别名或兼容字段。

因此纯格式调整不会制造业务dirty，语义修改一定改变hash。

## Decision 5: live authoring revision独立于generated Program

同步revision必须在不build的情况下从当前authoring内容计算：

```text
Character root
  + Definition正式可写配置
  + 全部可达Graph/StateMachine/ConditionRuleGraph
  + inline/shared Timeline及其Track/Clip/Marker/Curve
  + Agent可写Input/ActionProfile/Blackboard依赖

AI root
  + AIControllerDefinition
  + AIControllerTree与全部可达Graph
  + AI Blackboard
  + Perception Profile
  + 受控Character的只读input/request contract identity
```

generated Program、Projection和AIIntentProgram revision只进入`context`诊断，不参与判断作者是否改过树。Revision calculator必须只读，不调用任何build或publish入口。

## Decision 6: 四态同步由revision和hash推导

设：

```text
treeChanged = currentSourceRevision != sync.baseSourceRevision
documentChanged = canonical(editable + context contract) != sync.baseContentHash
```

状态为：

| treeChanged | documentChanged | 状态 | 允许动作 |
|---|---|---|---|
| false | false | `Clean` | checkout、validate |
| true | false | `TreeDirty` | 显式checkout刷新文档 |
| false | true | `DocumentDirty` | dry-run、apply |
| true | true | `Conflict` | inspect、显式rebase；拒绝apply |

状态不保存为可编辑bool，每次action都从当前树和当前文件重新计算。

## Decision 7: checkout只在明确请求时写文档

`checkout_document`固定行为：

1. 显式加载domain与root path。
2. 计算live authoring revision。
3. 读取确定性文档路径。
4. 无文档时从当前树生成完整规范Document。
5. `Clean`或`TreeDirty`且Document未改时，从当前树刷新Document。
6. `DocumentDirty`时保留现有文件并返回路径和状态，不覆盖AI工作。
7. `Conflict`时保留双方并返回结构化冲突，不重写文件。

checkout不build、不validate generated product、不保存Unity资产。

## Decision 8: Reconciler只生成内部Mutation Plan

正式降低链改为：

```text
AgentAuthoringDocument
  -> strict parse
  -> sync/root/context validation
  -> current canonical Snapshot
  -> AgentDocumentReconciler
  -> immutable AgentMutationPlan
  -> existing preflight/session/handler
```

Reconciler按stable identity比较实体：

- 现有identity且内容相同：不生成命令。
- 现有identity且可写字段变化：生成对应typed update command。
- 新local identity：建立planning symbol并生成typed create command。
- 现有可写identity从完整目标集合消失：生成typed delete command。
- read-only或unsupported identity变化：拒绝，不生成命令。

创建顺序、引用绑定、edge重接、owner收集和删除顺序由Reconciler和typed plan决定，不暴露给AI。

现有Patch Command/Compiler类型直接迁移为`AgentMutation*`命名，不保留Patch alias。Handler继续调用相同正式BTSMTL/Timeline/AI authoring API。

## Decision 9: dry-run和apply以documentHash锁定同一语义输入

`dry_run_document`：

1. 重新读取文档。
2. 重新计算live source revision和sync状态。
3. 只接受`DocumentDirty`或明确允许的无变化`Clean`。
4. 严格解析、reconcile和preflight。
5. 返回canonical `documentHash`、planned diff、诊断和metrics。

`apply_document`：

1. 要求`expected_document_hash`。
2. 重新计算文档hash和live source revision。
3. 任一身份、revision、hash或状态变化时在mutation前失败。
4. 重新建立与dry-run相同语义的immutable plan并校验其plan hash。
5. 在唯一Undo事务内apply、validate、dirty与save。
6. 只有显式apply成功后才调用正式generated product发布。

dry-run不缓存Unity对象或跨MCP调用保存plan。hash锁定语义输入，apply在当前Editor状态重新构建等价plan，避免跨domain reload持有失效对象。

## Decision 10: apply成功后以正式树反向规范化Document

正式顺序：

```text
apply mutation plan
  -> validate formal tree
  -> save formal owners
  -> explicit generated product publish
  -> export final canonical document
  -> atomic replace document file
  -> update baseSourceRevision/baseContentHash
  -> Clean
```

stable identity、实际owner和规范顺序只能从最终树获得。若Document写回失败，Unity事务不得被报告为完整成功；service必须在可回滚边界内处理文档原子替换，不能留下“树已保存但JSON仍声称待应用”的假状态。

## Decision 11: Conflict只允许显式rebase

发生`Conflict`时，系统不自动选择任何一边。`rebase_document`：

1. 返回当前树的规范投影与Document差异诊断。
2. 要求AI先把当前人工变化合入editable body。
3. 显式确认后，只把当前树revision和当前树canonical content hash写为Document新基线。
4. 保留AI编辑后的目标body，使状态回到`DocumentDirty`。
5. 后续必须重新dry-run。

rebase不修改Unity树、不build、不apply，也不静默删除AI或人工内容。

## Decision 12: 所有重操作都必须显式触发

以下事件只允许更新可见状态或延迟到下一次显式查询计算：

- Graph/Timeline/Inspector编辑。
- JSON文件保存。
- selection变化。
- 窗口focus。
- AssetDatabase refresh。
- domain reload。

它们不得触发checkout、reconcile、dry-run、apply、Program build或Projection build。只有`apply_document`允许在事务成功后显式发布generated product；独立`validate`只做只读正式校验。

## MCP Contract

继续只有一个工具：

```text
manage_btsmtl_agent_authoring
```

正式action：

```text
checkout_document
rebase_document
dry_run_document
apply_document
validate
```

共同参数保留显式`domain + root_asset_path`。`apply_document`额外要求`expected_document_hash`。Document路径由service计算并返回，调用方不能传入任意文件路径。

删除：

```text
bootstrap_ai_controller
export_snapshot
dry_run_patch
apply_patch
patch_json
```

## Editor Window

窗口只负责：

- 显示明确root上下文。
- 显示Document确定性路径。
- 显示`Clean/TreeDirty/DocumentDirty/Conflict`。
- 提供显式checkout、rebase、dry-run、apply和validate按钮。
- 显示planned diff、applied diff和机器诊断。

窗口不内嵌第二个JSON编辑器；AI使用普通文件工具编辑Document。窗口不因selection自动切root或执行任何重操作。

## Failure Semantics

- root缺失或类型不符：失败，不扫描替代资产。
- Document缺失：只有checkout可创建。
- schema不匹配：失败，不转换v16/v17 Patch。
- sync头被修改：失败并指出service-owned字段。
- TreeDirty或Conflict：apply前失败。
- document hash变化：apply前失败。
- unknown entity、field、node capability或reference：reconcile前失败。
- transaction owner不完整：mutation前失败。
- apply或validator失败：回滚全部Unity owner。
- generated product发布失败：回滚并保持DocumentDirty。
- final Document原子写回失败：不得报告Clean或完整成功。

## Migration

1. 冻结旧v16/v17 Patch外部schema，不再增加operation。
2. 建立Document模型、canonical codec、store、revision和sync evaluator。
3. 建立Document Reconciler并复用现有typed handler验证能力覆盖。
4. 将内部Patch类型原子改名为Mutation类型。
5. 把Service、Window和MCP切换到Document action。
6. 删除Intent、Macro、Patch parser、旧action和bootstrap。
7. 重基线`add-corin-training-ai-demo`未完成任务。
8. 更新skill、current-contract与project口径。

迁移结束时只能存在Document到Mutation Plan这一条Agent写入链。不得保留隐藏旧菜单、兼容reader、临时Patch文件或双写response。

## Tradeoffs

### 完整目标文档而不是操作数组

AI更容易理解状态、状态机和Timeline结构，系统也能自己决定最小修改顺序。代价是必须实现严格、稳定的Reconciler，并明确unsupported/read-only实体的保留规则。

### 显式同步而不是实时绑定

避免每次文件保存或Graph编辑卡住Unity Editor，也允许AI在文档中多轮修改。代价是必须显示脏状态并处理冲突，用户不能假设文件和树始终同步。

### Unity树继续是正式真相

保留Graph Editor、Timeline Editor和现有资产编译链，不需要把全部人工工具改成JSON前端。代价是JSON只能作为工作文档，不能在未apply时被其它系统消费。

### 语义hash而不是原始文件hash

格式化不会制造无意义冲突，AI可以自由调整缩进。代价是必须拥有唯一canonical parser/writer，不能继续依赖会忽略未知字段的宽松JSON解析。

### 冲突失败而不是自动merge

不会静默覆盖人工Graph改动或AI工作。代价是双边同时修改后需要一次显式rebase和新的dry-run。

