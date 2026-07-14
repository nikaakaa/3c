## 1. 现状梳理

- [x] 1.1 读取 `ExposedProperty`、`ExposedPropertyNode`、`BaseGraph` 的序列化和初始化链路，确认现有字段可迁移范围。
- [x] 1.2 读取 `TransitionRuleGraph`、`NestedGraphValidation` 和现有 ValueNode/PropertyPort 组合能力，确认黑板读取节点的合法边界。
- [x] 1.3 读取 `CharacterGraphContext` blackboard API、Action 输出提交 API 和 frame cleanup 链路，确认 runtime 生命周期缺口。
- [x] 1.4 读取 `CharacterPipelineOutput.SyncFacts`、`CharacterNetworkSendStage`、ActionProfile policy 相关代码，确认网络映射只能走 SyncDomain。

## 2. Blackboard 模型

- [x] 2.1 定义 blackboard variable 的 key、type、default、scope、lifetime、authority、sync policy、debug category 数据模型。
- [x] 2.2 定义 scope 和 lifetime 枚举，覆盖 Graph、State、ActionInstance、Character、Frame 等运行边界。
- [x] 2.3 定义 sync policy 枚举，区分 None、ConfigVersion、InputDerived、SyncFact、ReplicatedCue、CorrectionOnly。
- [x] 2.4 定义变量类型映射规则，覆盖现有 ExposedProperty 类型和角色 pipeline 需要的 ActionContext/业务事件摘要。

## 3. Runtime 接入

- [x] 3.1 实现 Pipeline Blackboard runtime instance，支持 typed get/set、默认值初始化、缺失 declaration 报错。
- [x] 3.2 将 `CharacterGraphContext` 的 blackboard API 改为委托 runtime instance。
- [x] 3.3 在 BeginFrame、状态退出、动作结束、Dispose 等生命周期点执行 scope/lifetime 清理。
- [x] 3.4 保持 Action window/cue/result 提交继续写入 `SyncFacts`，不得改成 blackboard 直连网络。

## 4. BTSMTL Authoring 接入

- [x] 4.1 将角色 pipeline 图内的 ExposedProperty 解析为 blackboard declaration。
- [x] 4.2 为缺少 scope/lifetime/sync policy 的旧 ExposedProperty 设计一次性迁移规则，不保留兼容分支。
- [x] 4.3 明确通用 BTSMTL 图和角色 pipeline 图的 UI 命名：`ExposedProperty` 或 `Blackboard Variable` 只能保留一个作者主入口。
- [x] 4.4 更新 graph 校验，发现 pipeline 图变量缺少必要元数据时报告配置错误。

## 5. Transition Rule 接入

- [x] 5.1 新增 TransitionRuleGraph 合法的纯 ValueNode 黑板读取节点。
- [x] 5.2 让黑板读取节点支持 Bool、Int、Float、String、Vector2、Vector3 等类型输出。
- [x] 5.3 缺失变量、类型不匹配、跨 scope 读取时按规则图错误处理，不走 fallback key。
- [x] 5.4 使用现有 Compare、And、Or、Not 等节点表达 Corin locomotion 阈值条件，删除临时业务条件节点。

## 6. 网络和 Debug

- [x] 6.1 实现 blackboard variable sync policy 到 SyncFacts 的显式 resolver，不新增 key/value 通用网络包。
- [x] 6.2 配置类变量使用 pipeline 配置版本/hash 或角色 loadout identity，不逐帧发送。
- [x] 6.3 输入派生变量保持本地确定性计算，不写入独立 SyncFacts。
- [x] 6.4 Runtime Debug 展示 declaration、当前值、生命周期、是否产生 SyncFacts、未发送原因。

## 7. Corin 迁移

- [x] 7.1 将 Corin locomotion 阈值、转身角度、动作临时 key 声明为 Pipeline Blackboard variables。
- [x] 7.2 将 Locomotion StateMachine 的 TransitionRuleGraph 改为 input value + blackboard value + compare/logic 节点组合。
- [x] 7.3 将 Action StateMachine 的 ActionContext、window/cue/result 临时读写统一到 blackboard declaration。
- [x] 7.4 删除由临时条件节点、散字符串 key 或旧 ExposedProperty 路径产生的分裂配置。

## 8. 验证

- [x] 8.1 运行 C# 编译验证，不运行 Unity batchmode。
- [x] 8.2 运行 `openspec validate add-pipeline-blackboard-authoring --strict --no-interactive`。
