# 实施盘点

## 实施前基线

- 服务端只有`3cDemo/Server/Main/Main.csproj`一个可执行入口，两个Network Test都发布`Main.exe`。
- 公共`Entity.csproj`同时包含Gate、DotRecast Authority Entity和portable DotRecast引用；公共`Hotfix.csproj`同时包含external Unity Worker与in-process DotRecast Scene handler。
- 旧`ServerAuthoritativeAuthorityHostRouter`按具体route kind分支；内部in-process枚举名称直接包含DotRecast。
- Unity Build在publish后修改共享`Fantasy.config`；Unity与DotRecast输出目录不同，但可执行和程序集闭包不是独立产品。
- 实施开始时两个固定发布目录均没有可供本change复用的完整当前产物，仓库中也不存在`CharacterProgram.csim`、`NavigationSurface.navsurface`或`DotRecastAuthorityScene.manifest`，因此没有可记录的有效旧Authority artifact hash。
- Outer/Inner generated协议、消息字段与route wire数值无需修改。本change只迁移服务端代码所有权与进程装配。

## 最终项目闭包

### 共享Host与Gate

- `ThirdPerson.Server.Host`：产品定义、manifest writer/reader、Hotfix generation loader、NLog与Fantasy Entry。
- `ThirdPerson.Server.Gate.Entity`：generated协议、Room/roster/ticket状态、host-neutral route合同与唯一adapter安装点。
- `ThirdPerson.Server.Gate.Hotfix`：共同join/leave、Room lifecycle、可靠事务与失败传播，只引用Gate Entity。

### Unity Authority产品

- 入口：`ThirdPerson.UnityAuthority.Server.exe`。
- Scene集合：`Gate`。
- Entity模块：`thirdperson.server.gate.entity`、`thirdperson.server.unity-authority.entity`。
- Hotfix模块：`thirdperson.server.gate.hotfix`、`thirdperson.server.unity-authority.hotfix`。
- Authority artifact：空集合。
- 禁止DotRecast Entity/Hotfix与`ThirdPersonSimulation.DotRecast*`依赖。

### DotRecast Authority产品

- 入口：`ThirdPerson.DotRecastAuthority.Server.exe`。
- Scene集合：`Gate`、`DotRecastAuthority`。
- Entity模块：`thirdperson.server.gate.entity`、`thirdperson.server.dotrecast-authority.entity`。
- Hotfix模块：`thirdperson.server.gate.hotfix`、`thirdperson.server.dotrecast-authority.hotfix`。
- required portable依赖：`ThirdPersonSimulation.DotRecast`、`ThirdPersonSimulation.DotRecastAuthority`、`ThirdPersonSimulation.ServerAuthoritative`、`ThirdPersonSimulation.ServerAuthoritative.Transport`。
- Authority artifact：Scene manifest、Character Program、Navigation Surface三项精确文件。
- 禁止Unity Authority Entity/Hotfix模块。

## Build与Run入口

- Unity Build固定发布`Products/UnityAuthority/ThirdPerson.UnityAuthority.Server.csproj`到`Build/Network/UnityAuthority/Server`。
- DotRecast Build固定发布`Products/DotRecastAuthority/ThirdPerson.DotRecastAuthority.Server.csproj`到`Build/Network/DotRecastAuthority/Server`。
- 两个Build都先验证源配置与发布配置exact-byte相同，再调用发布后的产品入口生成`ServerProductBuild.json`。
- DotRecast Build在生成产品manifest前，必须先由正式`DotRecastAuthoritySceneManifestExporter`输出三项Authority artifact。
- Run只读取Network Test manifest引用的精确Server Product manifest和hash，不publish、不编译、不修改配置。

## 静态发布证据

- 隔离验证根：`3cDemo/Server/Build/Verification/ServerProducts/20260718-113950`。
- Unity产品manifest hash：`8029dd5a5b1a59258f4468416dd33fd6812ff078a50564cd68672de16e418258`。
- Unity产品配置与源配置exact-byte相同，Scene仅为`Gate`，包含2个Entity模块、2个Hotfix模块、14个portable依赖和0个Authority artifact。
- DotRecast纯server publish的`.deps.json`与实际DLL闭包精确一致，配置与源配置exact-byte相同，Scene为`DotRecastAuthority,Gate`，required portable程序集全部存在，Unity Authority程序集数量为0。
- 在没有正式Authority artifact时调用DotRecast产品writer会明确失败于缺失`Authority/DotRecastAuthorityScene.manifest`，不会生成不完整manifest或使用fallback。
- 正式Unity Editor Build已生成DotRecast BuildId `20260718-114654`，产品manifest记录3个Authority artifact并通过Run前完整校验；manifest hash为`940562c465e7937097ab6f06eb959b7858853742009d6edb7fb1050144df7862`。
- `DotRecastAuthorityBuild.json`记录的ServerProductId、相对路径与manifest hash均和正式发布目录exact match。

## 不变业务链

本change没有修改Program、Pipeline、checkpoint、correction、UDP codec、Prediction、Authority Simulation或Presentation语义。Gate之后仍消费同一portable ServerAuthoritative control products和数据面；变化只发生在Server产品入口、模块所有权、配置与发布闭包。
