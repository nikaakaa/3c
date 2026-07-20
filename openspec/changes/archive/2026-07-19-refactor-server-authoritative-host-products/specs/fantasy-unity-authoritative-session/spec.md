## MODIFIED Requirements

### Requirement: 四进程Demo必须具有可验证的正式启动入口

Network Test Player MUST通过显式构建入口固定Bootstrap、Client和Authority Worker场景顺序，且Network Test Bootstrap MUST是第一场景。Unity Authority Build MUST发布独立`ThirdPerson.UnityAuthority.Server`产品；该产品 MUST只包含Gate Scene、共享Gate模块与external Unity Worker route模块，MUST不包含DotRecast runtime、DotRecast Authority Scene模块或Authority Scene artifact。四进程启动入口 MUST从匹配ServerProductId和manifest启动Fantasy Server、Authority Worker、Client A和Client B，并在报告成功前验证三个Unity角色均存活且已建立网络endpoint。旧Player、旧通用`Main.exe`、错误Server Product、漏启角色或未进入网络场景 MUST fail-fast，不能作为成功的双客户端测试环境。

#### Scenario: Client B启动语句未执行

- **WHEN** 启动入口未能创建Client B进程或Client B在检查前退出
- **THEN** 启动入口 MUST报告Client B缺失并终止本次新进程

#### Scenario: Unity Demo混入DotRecast Server模块

- **WHEN** Unity Authority Build或Run发现server product manifest包含DotRecast模块、DotRecast Authority Scene或错误ServerProductId
- **THEN** Build或Run MUST在启动四进程前失败并报告具体产品闭包错误
- **AND** MUST不删除文件后继续运行或回退旧`Main.exe`

