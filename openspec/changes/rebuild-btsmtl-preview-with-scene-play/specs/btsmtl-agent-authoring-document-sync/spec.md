## ADDED Requirements

### Requirement: 场景预览调参必须保持唯一作者数据与Document同步

人工场景预览调参 MUST通过主重构提供的唯一 Document v5 及各领域正式增量的共享 Capability、Mutation 和唯一作者资产闭包修改合法字段，Document MUST按同一整包基线识别树侧变化和冲突。CharacterController domain 表达控制 binding/参数、SkillDefinition/技能 Root 与既有 Presentation 内容，MUST不恢复角色总控 RootTree 正文；独立 Timeline MUST消费其 owner 在同一 v5 中提供的精确 shared 内容根和分片，不要求 Character 包。预览 MUST不自动 checkout、rebase、apply 或保存 package，不建立另一份作者文档；下一次正式导出 MUST读取最终作者资产，不读取运行采用值作为默认值。

#### Scenario: 已有Document后人工调参

- **WHEN** 存在干净 Document 基线且作者在预览中修改正式参数
- **THEN** 同步检查 MUST显示正常树侧变化
- **AND** package 目标正文 MUST不被预览窗口自动重写

#### Scenario: 两侧都有修改

- **WHEN** 预览人工调参期间 Document editable 也发生变化
- **THEN** 同步服务 MUST按既有规则识别 Conflict
- **AND** MUST不自动合并、rebase 或应用

### Requirement: 预览运行配置与状态不得绕过Document边界

独立场景配置 MUST由其正式 Scene/Prefab 作者入口拥有，实际场景对象、调用方和运行服务绑定 MUST不复制进 Document 的可写业务分片。Timeline 内容声明的外部目标/参数需求 MUST由其正式作者合同表达，与本次实际对象绑定分离。控制 C# 正文/state schema、生成 Skill/Timeline Program、ActionInstance/SkillExecutionState、非 Skill 播放状态、调用 generation、执行来源状态、场景运行和参数采用状态/耗时 MUST不成为 editable 字段；需要向 Agent 展示的字段资格 MUST通过共享 Capability 的只读 context 描述。目标 Document v5、严格 Codec、唯一 Reconciler/Mutation/Validator、反向导出和五个生命周期 MUST保持统一，Play Mode 期间仍 MUST拒绝原本禁止的 Document 写入操作。

#### Scenario: 预览期间请求Document apply

- **WHEN** Unity 在 Play Mode 而 Agent 请求修改作者资产的 Document apply
- **THEN** 服务 MUST继续返回正式 Play Mode 门禁诊断
- **AND** MUST不通过预览调参接口绕过整包事务

#### Scenario: 导出运行参数相关字段

- **WHEN** 正式作者参数已修改但当前 Actor 尚未采用
- **THEN** Document 导出 MUST使用正式作者值
- **AND** MUST不把旧运行值、候选 generation 或生效状态写进作者正文

### Requirement: 预览接入不得重复Document升级或恢复旧包

Document v5 的基础 schema、控制配置/技能分片、严格解析与整包迁移 MUST由主重构的唯一文档服务提供，Timeline domain MUST由 Timeline owner 在同一服务/事务内增加。预览 MUST消费各领域已批准并正式发布的 domain 集合，不写死数量，不恢复已退役 domain，也不新建插件 AI Document。预览 MUST不复制 reader、migrator、Reconciler 或独立保存事务。旧 v4 或更早 package MUST按正式服务拒绝，在作者资产迁移后针对精确根显式 checkout v5，不把旧角色图正文翻译成预览运行配置。

#### Scenario: 预览关联旧Document包

- **WHEN** 预览相关操作遇到旧 v4 package
- **THEN** MUST保留正式服务的版本拒绝结果并指向精确根重新 checkout
- **AND** MUST不兼容读取、自动 rebase 或通过运行调参通道绕过版本检查

#### Scenario: 独立Timeline作者入口已正式发布

- **WHEN** 唯一 v5 服务已安装 Timeline domain 且作者选择合法 shared Timeline 根
- **THEN** 预览相关字段资格与作者修改 MUST消费同一 Capability、Mutation 和整包事务
- **AND** MUST不要求 Character/Skill 根或另建 Timeline 包格式

#### Scenario: 其它领域已正式退役

- **WHEN** 对应领域 change 已从唯一服务移除一个 domain 后合并预览增量
- **THEN** 预览 MUST消费更新后的正式 domain 集合
- **AND** MUST不因旧文档曾列两个 domain 而恢复旧 reader、正文或工具路由

#### Scenario: 两个Document包关联同一shared Timeline

- **WHEN** Character 包与 Timeline 包引用同一正式 shared Timeline，作者通过预览的合法参数入口修改该资产
- **THEN** 两个包 MUST按同一资产 revision 和各自基线识别 TreeDirty 或 Conflict
- **AND** MUST不复制资产、维护第二份作者值或自动覆盖任一 package
