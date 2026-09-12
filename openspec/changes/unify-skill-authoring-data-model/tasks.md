## 1. 数据来源对账

- [x] 1.1 确认状态机转移同时存在 Step 与 Edge 两份条件、priority、abortPolicy，固定 Edge 为唯一转移来源。
- [x] 1.2 确认普通 Sequence、Selector、Parallel 的 steps 是另一种组合语义，保留在普通节点 properties 中。
- [x] 1.3 固定 Corin 状态机的节点、边、条件图、owner、UID和并列顺序基线，避开其它未提交资产。

## 2. 正式节点与端口模型

- [x] 2.1 建立 `BtsmtlSkillTransferPayload`，统一条件图、priority、abortPolicy和order的类型与写入口。
- [x] 2.2 状态 Enter、Any、State、Exit改为固定逻辑端口；Transfer输出容量和StateIn输入规则由正式Closure校验。
- [x] 2.3 接入共享 `GraphAuthoringCapabilityCatalog` 与唯一 `GraphAuthoringNodePortShapeProjector`，删除anchor动态steps投影。
- [ ] 2.4 完成原业务节点与FlowCanvas同义节点的全量字段、默认值、校验、引用和编译对照；未确认同义的能力不合并。

## 3. 引用、复制与编译消费

- [x] 3.1 Closure、ClosureIndex、GraphCopy、Exporter、Validator、Applier和Occurrence改为读取Edge transfer payload。
- [x] 3.2 ConditionRule owner统一为`kind=edge`、`edgeId`、`referenceKey=condition`，并纳入可达性、循环和owner校验。
- [x] 3.3 删除状态机anchor.steps的DTO、导出、应用、投影、引用闭包和循环检查路径；普通节点properties.steps保留。
- [ ] 3.4 等并行 Native FSM/Timeline authoring闭包稳定后，完成当前正式编译和SourceMap对账，不覆盖其未提交改动。

## 4. Document v8与存量包

- [x] 4.1 将唯一 `AgentAuthoringSchema.Version`、Report、Codec、Store、作者窗口和五个MCP工具说明切换为 v8，并严格拒绝v7及更早包。
- [x] 4.2 删除独立 `BtsmtlSkillTransferConnectionMigrator`；删除前代码由Git提交历史保留，不建立旁路迁移入口。
- [x] 4.3 删除被忽略的旧 v4/v5/v7 package目录，保留正式Unity资产和Git历史。
- [ ] 4.4 在当前 authoring闭包可导出后，通过正式checkout生成v8 package，核对manifest/sync、完整闭包、owner/order和hash。
- [ ] 4.5 对v8执行无修改dry-run、validate、重新checkout；需要改资产时才使用同hash apply，并交付Clean结果。

## 5. 文档与交付

- [x] 5.1 重写本change的proposal、design和implementation，移除与实际Edge/v8实现矛盾的旧口径。
- [ ] 5.2 对照并更新现行 `openspec/specs/`、`openspec/project.md` 与 `btsmtl-agent-authoring` 技能合同，保留非本change场景和历史archive。
- [ ] 5.3 完成原业务/FlowCanvas定义清单、源码路径、删除项和业务行为对照；不以Agent往返成功代替模型统一证明。
- [ ] 5.4 汇总小步提交、正式Unity实例、checkout/dry-run/apply/validate结果与未提交外部改动边界。
