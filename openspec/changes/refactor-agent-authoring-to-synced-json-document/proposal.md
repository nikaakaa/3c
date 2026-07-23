# Change: 将 Agent Authoring 重构为显式同步 JSON 文档

## Why

当前 Agent authoring 对外虽然只有一个 `manage_btsmtl_agent_authoring` MCP 工具，但 AI 实际提交的是包含大量 `op` 变体的宽 `AgentPatchOperation` JSON。AI仍需理解节点创建、边连接、stable identity输出引用、Timeline ownership和修改顺序，只是这些内部命令被包进了一个字符串参数，并没有形成用户最初要求的“AI直接编辑一份角色控制器JSON”。

当前链路还缺少稳定的双向编辑闭环：

- Unity Graph与Timeline是正式authoring真相，但没有一份只在AI开始工作时生成的持久化、可直接编辑JSON工作副本。
- Character Snapshot的`sourceRevision`当前可来自已生成Program；作者修改树但尚未显式编译时，Agent可能无法准确识别`TreeDirty`。
- `AgentControllerIntent`仍留在schema和窗口中，但`AgentMacroLibrary`已经明确拒绝展开，current spec要求的Intent/Macro链与实际正式路径矛盾。
- current MCP spec固定四个v16 Patch action，工作区却已经出现v17常量和未安装的`bootstrap_ai_controller`第五个action，外部合同与实现继续漂移。
- active `add-corin-training-ai-demo`仍依赖v16 Patch与`bootstrap_ai_controller`，若不重基线会在本变更后保留第二条资产写入路径。

本变更把Agent对外合同改为一份按需checkout的规范JSON文档。AI只编辑文档中的完整、类型化authoring结构；系统显式dry-run时比较当前树和目标文档，确定性降低为内部mutation plan；显式apply时继续复用现有preflight、Undo、正式BTSMTL authoring API、Validator、Save与generated product发布链。保存JSON、选择资产、导入文件或普通Graph编辑都不得自动触发编译。

## What Changes

- 新增`btsmtl-agent-authoring-document-sync`能力，定义Unity树与Agent JSON工作文档之间的显式双向同步、四态脏标记、冲突阻断和规范化反向导出。
- 为每个已有合法`CharacterPipelineDefinition`或`AIControllerDefinition`建立确定性文档位置；只有显式`checkout_document`才从当前树生成或刷新JSON。
- JSON文档使用新的`btsmtl-agent-authoring-document.v1`外部合同，分离service-owned同步头、AI可编辑authoring body和只读capability/catalog上下文。
- 文档保存只产生`DocumentDirty`，不触发Graph mutation、Program build、Projection build、AI Program build、AssetDatabase导入回调或文件监视器。
- 使用live authoring内容计算`sourceRevision`，覆盖Definition、可达Graph、StateMachine、Timeline及domain正式可写依赖，不读取generated Program revision充当树版本。
- 通过`baseSourceRevision`与canonical `baseContentHash`推导`Clean`、`TreeDirty`、`DocumentDirty`和`Conflict`，不保存可由AI伪造的`dirty: true`布尔值。
- 新增确定性Document Reconciler，把完整目标文档与当前Snapshot比较并降低为唯一内部typed mutation plan；AI不再生成`operations[]`、前序operation output或编辑顺序。
- dry-run返回规范`documentHash`、同步状态、planned diff和机器诊断；apply必须携带同一hash并重新确认树revision与文档hash均未变化。
- apply成功后从最终正式树反向导出规范JSON，写回真实stable identity、规范顺序和新基线，使树与文档回到`Clean`。
- 双边同时变化时进入`Conflict`并拒绝apply；只有显式rebase可以把当前树确认为新基线，同时保留AI文档body供继续修订。
- 外部删除`AgentControllerIntent`、`AgentMacroLibrary`、`AgentMacroCoverageEvaluator`、`AgentPatchIR`、宽`AgentPatchOperation`及v16/v17 Patch parser；现有typed command、handler和transaction能力迁移并统一命名为内部`AgentMutationPlan`链。
- MCP继续只有一个`manage_btsmtl_agent_authoring`工具；正式action收敛为`checkout_document`、`rebase_document`、`dry_run_document`、`apply_document`和`validate`。
- 删除`export_snapshot`、`dry_run_patch`、`apply_patch`、`patch_json`和未被current spec授权的`bootstrap_ai_controller`，不保留alias、converter、剪贴板、Patch inbox、文件watcher或临时桥接。
- Editor Window与MCP继续共用唯一application service；窗口只显示文档路径、同步状态、诊断与显式操作，不因selection或focus触发checkout、dry-run、apply或build。

## Scope

### In Scope

- CharacterController与AIController已有合法Definition的按需JSON checkout。
- Agent当前正式可写Graph、StateMachine、Condition、Timeline、Blackboard、Perception和Intent结构的规范文档表达。
- 当前Agent只读Presentation、Body Motion、Foot Analysis与generated product上下文的只读文档投影。
- live authoring source revision、canonical content hash、四态同步状态和显式rebase。
- Document到内部typed mutation plan的确定性reconcile。
- 现有MCP、Editor Window、transaction、validator、report和技能合同迁移。
- active `add-corin-training-ai-demo`从v16 Patch工作流重基线到Document工作流。

### Out of Scope

- LLM模型训练、运行时LLM、运行时读取JSON或运行时Graph解释。
- 文件保存、资产选中、Inspector变化或AssetDatabase事件触发自动编译。
- 自动三方合并、静默覆盖Tree或Document、后台同步进程。
- 把JSON升级为Gameplay运行时或BTSMTL正式authoring真相。
- 为不存在的Definition创建新的Character或AI根资产；本变更只编辑已有合法root。若后续需要Agent创建root，必须单独定义正式root creation能力，不能保留`bootstrap_ai_controller`旁路。
- Presentation Profile、Pose Graph、Blend、Rig、Foot Analysis generated data和Body Motion Profile写入能力。

## Impact

### Specs

- 新增`btsmtl-agent-authoring-document-sync`。
- 修改`agent-character-controller-synthesis`。
- 修改`agent-ai-controller-synthesis`。
- 修改`btsmtl-agent-authoring-mcp-bridge`。
- 实施完成后同步更新`openspec/project.md`中的`v16 Snapshot -> Patch IR`现状描述。

### Code

- `AgentAuthoringModels`外部schema与domain文档模型。
- Character/AI Snapshot exporter与新的canonical document exporter。
- live authoring source revision计算。
- document store、strict parser、canonical writer与content hash。
- document sync state evaluator与rebase服务。
- document reconciler与内部typed mutation plan。
- `AgentPatchAuthoringService`、Compiler、CompileSession、Command、Lowerer和handler命名与所有权。
- Character/AI Validator与Compile Report路径。
- `AgentCharacterControllerSynthesisWindow`。
- `ManageBtsmtlAgentAuthoringMcpTool`。
- `btsmtl-agent-authoring`技能及current-contract文档。

### Active Change关系

- `add-corin-training-ai-demo`当前未完成的Agent资产任务仍写死v16 Snapshot/Patch、`bootstrap_ai_controller`和同Patch apply。实施本变更前必须停止这些旧工作流任务；本变更安装Document链后，在同一迁移中把该change的proposal、design、spec与tasks重基线为checkout、编辑文档、dry-run、同hash apply、反向规范化与validate。
- 工作区`AgentAuthoringSchema.Version`已经出现v17，但current specs仍是v16；该v17仍是宽Patch合同，不是本变更目标。实施时直接删除v16/v17外部reader并安装`btsmtl-agent-authoring-document.v1`，不继续用数字递增掩盖语义更换。
- 工作区`bootstrap_ai_controller`不是current MCP spec允许的正式action。本变更删除它，不把该漂移安装进新合同。

## Breaking Changes

- AI不再传入`patch_json`，也不再提交`AgentPatchIR.operations[]`。
- 删除`AgentControllerIntent`与Macro入口，不提供intent-to-document转换器。
- 删除v16/v17 Snapshot/Patch外部兼容、operation alias和旧action alias。
- `sourceRevision`改为live authoring revision；generated Program是否过期成为只读诊断，不再决定TreeDirty。
- apply输入从JSON字符串改为确定性工作文档与dry-run返回的`documentHash`。
- 文档冲突时必须显式rebase，不能自动重导出覆盖AI修改。
- `bootstrap_ai_controller`被删除；不存在合法root时document checkout明确失败。

## Current Spec Comparison

- current `agent-character-controller-synthesis`要求`Snapshot -> Intent -> Macro -> Patch IR`，同时代码中的Macro已经拒绝所有业务展开。本变更删除Intent/Macro外部路径，以规范Document和确定性Reconciler替代。
- current spec把宽`AgentPatchOperation`定义为editor-only JSON边界。本变更将typed mutation plan保留为内部实现，AI-facing边界只剩结构化Document。
- current Snapshot requirement正确规定Snapshot不是正式配置来源。本变更保留这一权威：Document也是工作副本，apply成功前不影响正式树，运行时永不读取它。
- current human-tuning requirement要求作者可以继续编辑普通Graph、Timeline与Profile。本变更保留人工编辑，并通过`TreeDirty`和显式checkout重新建立AI工作副本，而不是把Unity编辑器改成JSON只读视图。
- current MCP bridge要求单一工具、统一service、preflight、单Undo事务、正式authoring API、回滚和机器诊断。本变更全部保留，只替换外部载荷和action生命周期。
- current MCP bridge禁止临时Patch文件和watcher。本变更不恢复fallback或watcher；新的JSON是唯一正式Agent工作文档，并且只有显式action读取或写回。
- current project明确禁止选中资产或普通编辑触发自动构建。本变更把该要求提升为document sync正式合同。
- current `agent-ai-controller-synthesis`仍写v15，而project/current bridge写v16、工作区常量为v17，已经互相矛盾。本变更用同一个Document v1合同原子覆盖Character与AI，不保留多版本解释。

## Success Criteria

- AI开始编辑已有root时，显式checkout从当前正式树生成唯一规范JSON路径。
- AI保存JSON后Unity树、Program和Projection都不变化，状态只变为`DocumentDirty`。
- 人工只修改树时状态为`TreeDirty`，下一次显式checkout从当前树刷新JSON。
- 双方都修改时状态为`Conflict`，dry-run/apply拒绝且不覆盖任何一边。
- dry-run把Document确定性降低为内部typed mutation plan，并返回`documentHash`与planned diff。
- apply只接受同一document hash和同一live authoring revision，失败完整回滚。
- apply成功后从最终树反向规范化JSON，stable identity和基线更新，状态回到`Clean`。
- JSON保存、AssetDatabase import、selection、Inspector focus和domain reload均不自动build或apply。
- Character与AI继续复用唯一transaction、handler、validator和MCP bridge，不存在Patch、Macro、bootstrap或文件watcher并行路径。

