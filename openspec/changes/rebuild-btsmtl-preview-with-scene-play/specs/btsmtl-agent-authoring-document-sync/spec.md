## ADDED Requirements

### Requirement: 场景预览调参必须保持唯一作者数据与Document同步

人工场景预览调参 MUST经既有领域 Mutation 修改正式作者资产，Document MUST按原有整包基线识别树侧变化和冲突。预览 MUST不自动 checkout、rebase、apply 或保存 package，不建立另一份角色作者文档；下一次正式导出 MUST读取最终作者资产，不读取运行采用值作为默认值。

#### Scenario: 已有Document后人工调参

- **WHEN** 存在干净 Document 基线且作者在预览中修改正式参数
- **THEN** 同步检查 MUST显示正常树侧变化
- **AND** package 目标正文 MUST不被预览窗口自动重写

#### Scenario: 两侧都有修改

- **WHEN** 预览人工调参期间 Document editable 也发生变化
- **THEN** 同步服务 MUST按既有规则识别 Conflict
- **AND** MUST不自动合并、rebase 或应用

### Requirement: 预览运行配置与状态不得绕过Document边界

独立场景配置 MUST由其正式 Scene/Prefab 作者入口拥有，不复制进 Character/AI Document 的可写业务分片。运行身份、场景 generation、当前状态、参数采用状态和耗时 MUST不成为 editable 字段；需要向 Agent 展示的字段资格 MUST通过共享 Capability 的只读 context 描述。Document v4、严格 Codec、唯一 Reconciler/Mutation/Validator、反向导出和五个生命周期 MUST保持统一，Play Mode 期间仍 MUST拒绝原本禁止的 Document 写入操作。

#### Scenario: 预览期间请求Document apply

- **WHEN** Unity 在 Play Mode 而 Agent 请求修改作者资产的 Document apply
- **THEN** 服务 MUST继续返回正式 Play Mode 门禁诊断
- **AND** MUST不通过预览调参接口绕过整包事务

#### Scenario: 导出运行参数相关字段

- **WHEN** 正式作者参数已修改但当前 Actor 尚未采用
- **THEN** Document 导出 MUST使用正式作者值
- **AND** MUST不把旧运行值、候选 generation 或生效状态写进作者正文
