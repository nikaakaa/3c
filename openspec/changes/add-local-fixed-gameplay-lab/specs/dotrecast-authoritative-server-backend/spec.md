## MODIFIED Requirements

### Requirement: Build与Run必须分离且产物必须按模型隔离

Unity Authority与DotRecast Authority MUST分别拥有模型专属`Build`与`Run` Editor入口、Player、Fantasy Server、build manifest和日志目录。两个产品的Editor操作 MUST进入唯一`Tools/3C/Launcher`的`Network Test Products`分组，不再注册分散菜单。Build MUST锁定该模型的Player target/options与Server configuration，只替换同模型的当前Player、Server、manifest与Authority artifacts，并保留日志；Build MUST不启动进程。Run MUST只校验和消费该模型当前正式manifest与产物，MUST不触发编译。manifest MUST记录`BuildId=yyyyMMdd-HHmmss`作为当前产物身份，但BuildId MUST不参与目录寻址。Unity Authority与DotRecast Authority的固定目录 MUST不重叠且不得互相覆盖。Unity Authority发布的`Fantasy.config` MUST只包含Gate Scene；DotRecast Authority发布的`Fantasy.config` MUST只包含Gate Scene与DotRecast Authority Scene。DotRecast Authority Scene manifest、Program和Navigation artifact MUST随DotRecast Server以正式相对路径发布。Unity脚本 MUST启动Fantasy Server、Unity Authority Worker、Client A和Client B；DotRecast脚本 MUST只启动Fantasy Server、Client A和Client B，并先构造完整PowerShell参数数组再调用`Start-Process`。每次Run MUST按模型与RunId建立日志目录。未形成完整可运行闭环的模型 MUST不注册占位测试入口。

#### Scenario: 分别Build和Run Unity Authority

- **WHEN** 作者点击统一Launcher中Unity Authority的`Build`
- **THEN** 系统 MUST按`StandaloneWindows64 + IL2CPP + Development + StrictMode`替换Unity Authority当前Player，并以`Debug`替换该模型Fantasy Server且写入实际编译选项
- **AND** MUST不启动任何测试进程
- **WHEN** 作者随后点击同一产品的`Run`
- **THEN** 系统 MUST只从Unity Authority固定目录启动Fantasy Server、Unity Authority Worker、Client A与Client B
- **AND** MUST不重新构建Player或Server

#### Scenario: 连续发布两次DotRecast环境

- **WHEN** 仓库中已存在前一次DotRecast Authority当前产物
- **THEN** 新Build MUST替换DotRecast Authority自己的Player、Server、manifest与Authority artifacts，并保留既有日志
- **AND** MUST不修改Unity Authority的Player、Server、manifest或日志
