## 1. 基线与生成边界

- [x] 1.1 按design D0确定本任务源码、生成范围及共享文件归属，保留已有正确实现，不覆盖其他任务文件。
- [x] 1.2 清点旧Agent公共协议及调用者，区分可删除协议、重复规则和需要保留的正式业务能力。
- [x] 1.3 确定Graph/Timeline内部owner与外部资源边界，定义输入、根输出和根挂接，不扩张成全角色素材重建。
- [x] 1.4 明确旧工作包删除范围，保留尚未处置的用户内容，不自动丢弃人工改动。

## 2. 正式业务API与校验

- [x] 2.1 将作者调用者接入正式领域API，真实业务缺口按D0由所属领域补齐，不新增中央Validator。
- [x] 2.2 删除BtsmtlSkillNodeAuthoringBinding.cs的JSON适配，复用正式Configure/Set及字段读取，不改写共享Capability和正确业务规则。
- [x] 2.3 删除TimelineAuthoringClipBinding.cs的JSON适配并提供typed接口；Slate UI接入由Timeline任务维护。
- [x] 2.4 将Agent报告/Validator调用改为现有正式诊断入口，保留Curve组、MotionWarp、Action target、Animation channel和FSM Edge业务约束。

## 3. 完整图到C#输出

- [x] 3.1 实现公共领域扩展合同与通用闭包遍历，消费正式对象读取和依赖信息，不建立第二领域模型。
- [x] 3.2 实现对象变量映射、确定性命名及创建/配置/引用/连线输出顺序，处理前向引用、动态端口和共享对象。
- [x] 3.3 实现通用C#值与语句输出，完整表达字符串、数值、枚举、向量、数组和Curve。
- [x] 3.4 接入Skill/FSM/Timeline等领域薄适配，覆盖参数、接口、转移、绑定、布局和业务顺序；有效FSM操作迁出由FSM任务负责。
- [x] 3.5 接入Pose与EventGraph领域薄适配，保持正式图/节点/变量和引用；不修改D0归他人所有的共享文件。
- [x] 3.6 实现未知类型/字段/引用的明确错误与完整输出后写文件，失败不覆盖已有源码；不新增JSON、源码解析或操作日志。

## 4. C#到资产生成与重建

- [x] 4.1 实现最小Editor生成入口合同，明确生成上下文和根输出，直接调用正式业务API，不引入DSL或Agent Session。
- [x] 4.2 接通生成范围的完整创建/替换，保持图/节点/变量identity，内部引用新对象并恢复Profile/Definition根挂接。
- [x] 4.3 实现旧生成资产清理与外部资源隔离，不误删共享资产、原始AnimationClip/Rig及范围外内容。
- [x] 4.4 接入正式保存与真实错误返回，保留原人工Undo，不新增源码Undo或保存监听。
- [x] 4.5 保留并提交正式导出的CorinAttackSkillAuthoringCode.cs、LocomotionFullBodyPoseGraphAuthoringCode.cs及meta；不按Generated目录或曾用于运行而删除正式创建源码。

## 5. 两个显式作者MCP

- [x] 5.1 实现btsmtl.export_code薄桥，接受精确资产、Definition和输出代码路径，输出完整源码、入口与依赖诊断，不修改输入资产。
- [x] 5.2 实现btsmtl.generate_assets薄桥，执行当前已编译正式入口并保存明确生成范围，代码未就绪时不运行旧结果。
- [x] 5.3 实现严格显式触发边界，人工修改/保存、源码编译、selection和Inspector重绘不导出或生成；MCP显式传unity_instance并拒绝不安全Editor状态。

## 6. 激进删除旧Agent

- [x] 6.1 迁移受影响作者调用者并接通正式编辑/生成/保存，承接共享文件中的有效业务操作；不把独立motor桥运行闭环作为本任务前置工作。
- [x] 6.2 在作者调用关系迁移后删除AgentAuthoring公共Document/Snapshot/Codec/Store/Exporter/Reconciler/Mutation/Session/Validator/Report及support。
- [x] 6.3 按D0共享文件例外删除SkillDocument/PresentationDocument无消费者协议，不整目录删除有效领域实现或搬迁改名框架。
- [x] 6.4 删除旧五工具和专属scheduler，只保留两个新作者MCP；独立Build与其他业务工具不被误删。
- [x] 6.5 删除Agent窗口、旧导航、协议专属测试/依赖、无用工作包，以及design D7.2明确的旧skill三份文件和空目录；不从HEAD恢复旧协议，不新建替代skill，不删除正式Generated C#。

## 7. 文档收口

原7.1往返/证据归集任务、7.2编译/引用验证任务按用户最新指令取消，不作为剩余工作；已有真实记录保留，不删除日志。

- [x] 7.3 清理本change对应的现行Agent合同及项目入口，迁移独立Build条款；新两工具说明归正式文档，有效Pose等规则留所属spec/API，不保留旧skill兼容入口。
- [x] 7.4 更新execution.md中的实现范围、代码链、文件去向、小步提交和真实未完成项；功能由用户验收，不继续列验证任务或以证据缺失卡住实施。
