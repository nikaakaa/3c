## 1. 基线与生成边界

- [ ] 1.1 核对当前源码、资产、规范和Git差异，按design D0登记共享文件归属与排除删除范围，交付已有正确实现、不覆盖文件及真实合同冲突清单。
- [ ] 1.2 清点旧Agent目录与目录外消费者，交付协议删除、重复校验删除、正式能力复用、缺失业务补齐四类清单。
- [ ] 1.3 固定支持的Graph/Timeline根、内部owner闭包与外部资源界限，交付输入/输出/根绑定表；不把一个图导出扩张为全角色素材重建。
- [ ] 1.4 核对旧工作包及待保留人工内容，交付明确删除范围和未应用内容处置记录，不自动丢弃用户改动。

## 2. 正式业务API与校验

- [ ] 2.1 核对Agent协议校验和重复规则的删除去向，真实业务缺口按D0交所属领域并记录依赖，交付逐规则唯一实现与调用关系；旧协议实删须满足6.1，不接管领域校验或新建中央Validator。
- [ ] 2.2 负责BtsmtlSkillNodeAuthoringBinding.cs的JSON退役，复用已有正式Configure/Set和字段读取，交付直接C#接口对照；不修改数据统一任务的Capability或正确规则。
- [ ] 2.3 负责TimelineAuthoringClipBinding.cs的JSON退役并交付typed接口；核对Timeline任务完成BtsmtlSlateTimelineProjection.cs的UI接入结果，不由本任务修改该共享文件。
- [ ] 2.4 将Agent报告/Validator调用改为现有正式诊断，核对Curve组、MotionWarp、Action target、Animation channel和FSM Edge约束，交付保留规则与无Agent依赖结果。

## 3. 完整图到C#输出

- [ ] 3.1 提供design D3.1公共领域扩展合同及通用闭包遍历，消费领域提供的正式对象读取/依赖信息，交付支持判定、创建/配置/连线/根挂接责任与访问去重规则，不建第二领域模型。
- [ ] 3.2 实现对象到局部变量映射与确定性命名，按创建、配置、引用和连线阶段输出，交付前向引用、动态端口及共享对象顺序说明。
- [ ] 3.3 实现通用C#值/语句输出，覆盖字符串转义、精确数值、枚举、向量、数组和完整Curve，交付真实输出文件及字段覆盖检查。
- [ ] 3.4 接入Skill/FSM/Timeline等领域提供的薄输出适配，交付参数、接口、转移、绑定、布局和顺序覆盖；BtsmtlSkillGraphAuthoringApplier.cs内有效FSM操作迁出由FSM任务负责。
- [ ] 3.5 接入Pose与EventGraph领域薄适配，交付正式图/节点/变量及引用覆盖；不修改Pose adapter/Mutation/输入消费或事件图三个共享文件，未支持内容必须拒绝完整导出。
- [ ] 3.6 实现未知类型/字段/引用的完整性错误和完成后写文件，交付精确诊断与失败不覆盖已有源码的路径；不新增JSON、源码解析或操作日志。

## 4. C#到资产生成与重建

- [ ] 4.1 定义供导出代码和AI手写代码共用的最小Editor创建入口合同，交付明确生成上下文、根输出和现有业务API调用；不引入DSL、Agent Session或任意eval。
- [ ] 4.2 通过领域正式API接通完整创建/替换，保持图/节点/变量identity，内部引用使用新对象并恢复Profile/Definition根挂接，交付无旧生成子资产GUID依赖及删除重建代码链。
- [ ] 4.3 完成范围内旧生成内容清理与外部资源隔离，交付共享资产、原始AnimationClip/Rig及范围外消费者不误删的引用说明。
- [ ] 4.4 接入正式保存和实际错误返回，交付创建/保存结果、失败恢复限度与受影响对象；人工Undo沿原入口，不增加源码Undo或保存监听。

## 5. 两个显式作者MCP

- [ ] 5.1 实现btsmtl.export_code薄桥，接受精确资产/Definition/输出代码路径并返回代码入口、依赖与诊断，交付独立调用结果；不修改输入资产。
- [ ] 5.2 实现btsmtl.generate_assets薄桥，接受精确源码/已编译入口/Definition/输出资产路径并执行保存，交付成功与代码未就绪/编译失败结果，不运行旧同名编译产物。
- [ ] 5.3 确认人工修改、资产保存、源码编译、selection和Inspector刷新均不调用导出或生成，交付触发入口引用检查；两个工具显式unity_instance并拒绝不安全Editor状态。

## 6. 激进删除旧Agent

- [ ] 6.1 按design D7.1核对作者调用者脱离Agent、正式编辑/生成/保存可用及领域有效操作迁出，交付旧协议删除门槛证据；固定motor参数桥与动画变量运行闭环另列外部依赖。
- [ ] 6.2 在6.1成立后删除AgentAuthoring公共Document/Snapshot/Codec/Store/Exporter/Reconciler/Mutation/Session/Validator/Report及support，交付删除清单和无反向引用结果。
- [ ] 6.3 按D0共享文件例外清理SkillDocument/PresentationDocument无消费者协议，核对FSM业务迁出交付后仅删剩余协议；不得整目录删除共享文件或搬迁一套改名框架。
- [ ] 6.4 删除旧checkout/rebase/dry-run/apply/validate五工具和专属scheduler，交付仅两个新作者MCP的注册对照；独立Build与非Agent工具不被误删。
- [ ] 6.5 删除Agent窗口、失效导航、协议专属测试/依赖、已确认无用工作包和本任务拥有的技能说明，交付消费者与meta配对检查；不新增测试或删除其他系统JSON。

## 7. 往返与交付

- [ ] 7.1 按design Evidence Contract归集完整导出、删除输出后重建、图/节点/变量identity、根引用恢复、确定性、错误不覆盖源码与人工编辑不导出的证据；无法实际执行则明确保留未验证，不新增测试代码或手动验收任务。
- [ ] 7.2 执行适用编译、引用和差异检查，交付真实命令与结果；dotnet/msbuild禁用build server且结束后shutdown，不以静态检查冒充Unity运行。
- [ ] 7.3 对账本change所有delta与最新现行spec及active change，清理Agent专属合同、迁移独立Build条款，更新project/skill唯一入口并通过严格OpenSpec校验；具体业务冲突不自行覆盖。
- [ ] 7.4 在execution.md汇总代码链、公共扩展/两个MCP输入输出、领域交付依赖、完整生成覆盖、删除门槛、小步中文提交与未完成项；运行时motor桥闭环单独登记，不把本任务完成当作其完成。
