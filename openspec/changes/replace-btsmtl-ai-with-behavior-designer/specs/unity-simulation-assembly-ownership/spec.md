## ADDED Requirements

### Requirement: AI插件依赖必须隔离在Unity接入程序集

插件任务、插件运行实例及其资源引用 MUST由独立 Unity 接入程序集拥有。正式观察、角色输入、输入所有权与网络身份合同 MUST不引用 Opsive、Unity Entities 运行对象或 Editor 类型。公共 Host/Composer 与 portable Float32/Fixed、Relay、普通 .NET Authority MUST通过正式能力合同装配，MUST不根据插件具体类型分支或反射查找实现。

#### Scenario: 构建纯.NET网络产品

- **WHEN** 构建既有 Relay 或不支持插件 AI 的普通 .NET Authority
- **THEN** 产品 MUST不引入 Opsive/Entities/Unity 插件运行依赖
- **AND** 网络协议 MUST只保存普通身份、输入和正式结果

#### Scenario: Unity角色使用插件控制源

- **WHEN** Unity 装配具有合法插件输入能力的角色
- **THEN** 插件程序集 MUST单向消费正式角色/输入合同
- **AND** Character/技能模拟程序集 MUST不反向调用插件任务或解释插件图
