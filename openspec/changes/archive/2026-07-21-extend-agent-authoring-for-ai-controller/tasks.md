## 1. 基线与依赖

- [x] 1.1 读取本change的proposal、design、tasks和全部spec delta。
- [x] 1.2 确认add-btsmtl-ai-controller-authoring已经完成且AI authoring API稳定。
- [x] 1.3 记录Agent v14 Snapshot、Patch、Intent、Validator与Report根合同。
- [x] 1.4 记录v14 Marker、Curve、MotionWarp、Action与Character Graph operation catalog。
- [x] 1.5 记录Patch DTO到immutable typed command plan的唯一lowering链。
- [x] 1.6 记录handler preflight、asset transaction、apply与report链。
- [x] 1.7 记录MCP bridge与EditorWindow共用的AgentPatchAuthoringService。
- [x] 1.8 盘点全部v14版本常量、reader、错误分支、文档和技能引用。
- [x] 1.9 确认不存在AI专用Agent工具、YAML writer或临时菜单。

## 2. Agent v15根合同

- [x] 2.1 将唯一Agent schema常量提升为agent-character-controller-synthesis.v15。
- [x] 2.2 定义CharacterController与AIController根domain discriminator。
- [x] 2.3 让Snapshot请求显式携带domain和root identity。
- [x] 2.4 让Patch根显式携带domain、source revision和root identity。
- [x] 2.5 让Intent根显式携带domain且不按内容推断。
- [x] 2.6 让Validation请求显式携带domain。
- [x] 2.7 保持Character domain现有v14字段和typed语义不变。
- [x] 2.8 拒绝未知domain、缺失domain和domain/root不匹配。
- [x] 2.9 删除v14及更早schema接受路径。

## 3. AI Snapshot

- [x] 3.1 输出AIControllerDefinition stable identity和source revision。
- [x] 3.2 输出AIControllerTree root、Graph、Node、Edge与PropertyPort identity。
- [x] 3.3 输出每个节点的authoring capability。
- [x] 3.4 输出AI Blackboard declaration、owner、scope、type与default value。
- [x] 3.5 输出AIPerceptionProfile和显式候选Actor binding。
- [x] 3.6 输出受控Character Definition、Program与input/request catalog binding。
- [x] 3.7 输出Observation、Memory和Intent节点typed字段。
- [x] 3.8 输出generated AI Program identity、source revision和stale状态。
- [x] 3.9 保持compact/full Snapshot使用同一identity。
- [x] 3.10 禁止输出AI candidate state、Perception缓存或Character mutable state。

## 4. AI Patch与Typed Command

- [x] 4.1 增加ensure AIControllerDefinition operation。
- [x] 4.2 增加ensure AIControllerTree operation。
- [x] 4.3 增加绑定Character Definition与Perception Profile operation。
- [x] 4.4 增加AI Blackboard declaration operation。
- [x] 4.5 增加Configured Candidate binding operation。
- [x] 4.6 增加Observation node typed operation。
- [x] 4.7 增加Memory node typed operation。
- [x] 4.8 增加Continuous Input intent operation。
- [x] 4.9 增加ActionTargetSnapshot intent operation。
- [x] 4.10 增加Action Request intent operation。
- [x] 4.11 复用现有Graph node、flow edge与property edge operation。
- [x] 4.12 为全部AI operation定义immutable typed command。
- [x] 4.13 让lowerer验证domain、owner、identity和operation顺序。
- [x] 4.14 让lowerer拒绝Character operation进入AI domain。
- [x] 4.15 让lowerer拒绝自由字符串InputId、RequestId和节点类型。
- [x] 4.16 保持dry-run与apply消费同一command plan。

## 5. Handler与事务

- [x] 5.1 注册AI domain handler catalog。
- [x] 5.2 让Definition handler只调用正式AI Definition authoring API。
- [x] 5.3 让Graph handler只调用BaseGraph和统一Graph policy。
- [x] 5.4 让Blackboard handler只调用正式AI declaration API。
- [x] 5.5 让Perception handler只调用正式Profile mutation API。
- [x] 5.6 让Intent handler只调用正式typed binding API。
- [x] 5.7 将AI Definition、Tree与Profile owner纳入同一资产事务。
- [x] 5.8 在preflight失败时保持全部owner未修改。
- [x] 5.9 在apply异常时恢复全部owner序列化状态。
- [x] 5.10 保持Undo、dirty与source revision属于实际owner。
- [x] 5.11 禁止SerializedProperty、反射、YAML和path/index写入。

## 6. Validator与Report

- [x] 6.1 让Validator按显式domain选择正式校验组合。
- [x] 6.2 复用AI Definition完整性校验。
- [x] 6.3 复用AI Graph capability policy。
- [x] 6.4 复用AI Blackboard scope与type校验。
- [x] 6.5 复用Perception binding校验。
- [x] 6.6 复用Intent与Character input/request catalog校验。
- [x] 6.7 复用AI Compiler publish校验。
- [x] 6.8 拒绝AI Graph中的Character、Timeline、Motion和Transform副作用节点。
- [x] 6.9 拒绝Team、Tag、名称和ActorId前缀敌我推断字段。
- [x] 6.10 报告domain、ControllerId、Graph identity、operation和正式错误来源。
- [x] 6.11 保持Validator只读且不修复资产。

## 7. MCP、窗口与技能

- [x] 7.1 更新Agent EditorWindow为v15并显示根domain。
- [x] 7.2 让EditorWindow继续调用唯一AgentPatchAuthoringService。
- [x] 7.3 更新MCP bridge接受和返回v15 generic request。
- [x] 7.4 保持MCP action集合不增加AI专用action。
- [x] 7.5 更新bridge schema说明AI domain和typed operation。
- [x] 7.6 更新btsmtl-agent-authoring技能的AI Snapshot流程。
- [x] 7.7 更新技能的AI dry-run、apply、re-export和validate流程。
- [x] 7.8 更新技能列出AI Graph禁止节点和资产事务边界。
- [x] 7.9 保持Character Controller v15工作流与现有v14能力一致。

## 8. 清理、编译与校验

- [x] 8.1 删除v14及更早reader、converter、alias与双写输出。
- [x] 8.2 删除按资产类型、路径或显示名推断domain的分支。
- [x] 8.3 删除重复AI node whitelist和Validator业务副本。
- [x] 8.4 搜索确认只有一个Agent schema常量和Patch application service。
- [x] 8.5 更新openspec/project.md的Agent版本与AI authoring边界。
- [x] 8.6 更新受影响current spec为v15唯一合同。
- [x] 8.7 使用规定参数构建Character Editor与相关BTSMTL Editor程序集。
- [x] 8.8 编译后立即执行dotnet build-server shutdown。
- [x] 8.9 运行openspec validate extend-agent-authoring-for-ai-controller --strict --no-interactive。
- [x] 8.10 核对tasks勾选与唯一Agent链一致。
