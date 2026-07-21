## ADDED Requirements

### Requirement: TEngine Editor 构建能力必须作为无项目业务的底层服务

TEngine Editor MUST以显式 request 和结构化 result 提供 HotFix DLL、YooAsset Content 与 Unity Player 构建能力。request MUST要求调用方提供 BuildTarget、PackageVersion、BuildOutputRoot 以及构建 Player 时的 PlayerOutputPath；TEngine MUST不引用 ProductStartupProfile、ClientBuildVersion、MinimumClientBuildVersion、商业产品目录或项目 release manifest。项目层 MUST拥有产品版本、固定路径、staging、闭包校验和正式发布。

#### Scenario: 项目工作流调用 TEngine Content 服务

- **WHEN** CommercialClientBuildWorkflow 提交完整显式 Content request
- **THEN** TEngine MUST先执行配置要求的 HotFix DLL 编译与复制并运行一次 YooAsset 构建
- **AND** result MUST返回真实输出目录、BuildReport 和结构化失败
- **AND** TEngine MUST不自行改写 PackageVersion 或 OutputRoot

#### Scenario: TEngine 构建请求缺少正式参数

- **WHEN** request 缺少 PackageVersion、BuildOutputRoot 或 PlayerOutputPath
- **THEN** TEngine MUST在调用对应构建器前失败
- **AND** MUST不使用 `Builds`、`Bundles`、当前时间或平台默认目录补齐

#### Scenario: 通用 TEngine 菜单尝试产生产品产物

- **WHEN** 开发者查看可用构建菜单
- **THEN** 项目 MUST只公开项目拥有的商业客户端正式构建入口
- **AND** TEngine 通用菜单 MUST不直接写入正式 Content 或 Players 分区

