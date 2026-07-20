## ADDED Requirements

### Requirement: 每种Authority Host必须拥有独立Server Product

Unity Authority与DotRecast Authority MUST分别拥有唯一入口项目、ServerProductId、可执行文件、精确模块闭包、源`Fantasy.config`和server product manifest。不同产品 MUST不由同一个通用可执行项目加运行时分支或发布后配置修改产生。共享库 MUST不能作为第三个可运行的通用ServerAuthoritative产品。

#### Scenario: 分别发布两个Authority产品

- **WHEN** 作者分别执行Unity Authority Build与DotRecast Authority Build
- **THEN** MUST生成`ThirdPerson.UnityAuthority.Server.exe`与`ThirdPerson.DotRecastAuthority.Server.exe`
- **AND** 两个可执行文件 MUST分别来自自己的入口项目和产品定义
- **AND** 系统 MUST不发布或启动旧通用`Main.exe`

### Requirement: 共享Gate必须与具体Authority Host实现解耦

共享Gate模块 MUST只拥有Room、fixed roster、client control、generated protocol、可靠事务、失败传播和host-neutral route port。共享Gate MUST不引用DotRecast、DotRecastAuthority、Unity CharacterController、具体Authority Scene runtime或具体Worker route实现。新增Authority Host产品 MUST通过自己的模块实现route adapter，MUST不修改共享Room的消息分派分支。

#### Scenario: DotRecast产品安装InProcess Route

- **WHEN** DotRecast Authority Server启动
- **THEN** 产品 MUST在Fantasy Entry前安装唯一in-process Authority Scene route adapter
- **AND** 共享Room MUST只调用host-neutral route port
- **AND** 共享代码 MUST不按DotRecast类型、名称或enum分支发送消息

### Requirement: Server Product必须显式声明精确模块集合

每个Server Product MUST以编译期产品定义声明有序Entity与Hotfix模块集合、ModuleId、load order、required与forbidden模块。Entity模块 MUST通过显式marker装载；Hotfix模块 MUST在同一个product-owned可卸载上下文中按清单原子装载。系统 MUST不扫描发布目录、不按程序集名称猜测模块角色、不装载未声明DLL，也 MUST不回退硬编码单一`Hotfix.dll`。

#### Scenario: Unity产品目录混入DotRecast模块

- **WHEN** Unity Authority发布目录包含未在产品定义中声明的DotRecast Authority模块
- **THEN** Build校验或Server启动 MUST明确失败并报告多余ModuleId
- **AND** MUST不忽略该DLL后继续运行

#### Scenario: DotRecast Hotfix模块缺失

- **WHEN** DotRecast产品清单声明的Authority Scene Hotfix文件缺失或hash不匹配
- **THEN** Server MUST在创建Gate/Room前失败
- **AND** MUST不只加载共享Gate Hotfix形成半个产品

### Requirement: Server Product配置必须是源码中的部署真相

Unity Authority MUST拥有只包含Gate Scene的正式源`Fantasy.config`；DotRecast Authority MUST拥有只包含Gate与DotRecast Authority Scene的正式源`Fantasy.config`。产品项目 MUST原样发布自己的配置并验证hash。Build MUST不从共享配置删除、增加或改写Scene，也 MUST不根据输出目录推断产品配置。

#### Scenario: 发布Unity Authority配置

- **WHEN** Unity Authority Server publish完成
- **THEN** 输出`Fantasy.config` MUST与Unity产品源配置exact match
- **AND** MUST不存在DotRecast Authority Scene
- **AND** Build MUST未执行XML裁剪

### Requirement: Unity Authority产品不得携带DotRecast实现

Unity Authority Server产品 MUST只包含共享Host、共享Gate、Unity Authority产品模块和共同ServerAuthoritative依赖。它 MUST不包含DotRecast/DotRecastAuthority portable程序集、DotRecast Authority Entity/Hotfix模块、Authority Scene artifact或DotRecast Scene配置。

#### Scenario: 审查Unity Authority发布闭包

- **WHEN** Build生成Unity Authority server product manifest
- **THEN** manifest与实际目录 MUST不包含任何forbidden DotRecast模块或artifact
- **AND** 发现任一禁止文件 MUST使Build失败

### Requirement: DotRecast Authority产品必须拥有完整InProcess Host闭包

DotRecast Authority Server产品 MUST包含共享Host、共享Gate、DotRecast Authority Entity/Hotfix、portable DotRecast/DotRecastAuthority依赖、Gate + Authority Scene配置及精确Authority manifest、Program和Navigation artifact。产品 MUST不引用UnityEngine、CharacterController或外部Unity Worker runtime实现。

#### Scenario: 审查DotRecast Authority发布闭包

- **WHEN** Build生成DotRecast Authority server product manifest
- **THEN** manifest MUST记录全部required模块和Authority artifact的精确路径与hash
- **AND** 缺失任一项 MUST使Build失败

### Requirement: Build与Run必须锁定精确Server Product Identity

每个模型的network test build manifest MUST引用匹配的server product manifest及其hash。server product manifest MUST记录SchemaVersion、BuildId、ServerProductId、executable、configuration、Entity/Hotfix模块、portable依赖和Authority artifact身份。Run MUST在启动前校验全部事实，MUST不触发publish、不接受其它产品manifest、不按文件存在性猜测产品。

#### Scenario: DotRecast Run读取Unity Server Product

- **WHEN** DotRecast network build manifest指向Unity Authority ServerProductId或executable
- **THEN** Run MUST在启动进程前失败并报告产品身份不匹配
- **AND** MUST不改写manifest、切换目录或重新Build

### Requirement: 产品选择必须发生在Build与进程启动边界

Unity Authority与DotRecast Authority MUST保持独立Build/Run入口和固定不重叠目录；同产品Build MAY替换自己的当前Player、Server、manifest与Authority artifact并保留日志，不同产品 MUST不互相覆盖。一个Server进程启动后 MUST锁定单一产品、单一Authority Host route和单一Scene集合，MUST不热切换产品或同时安装两种route adapter。

#### Scenario: 两种产品连续Build

- **WHEN** 作者先Build Unity Authority再Build DotRecast Authority
- **THEN** DotRecast Build MUST只替换DotRecast固定目录
- **AND** Unity Authority executable、config、manifest与日志 MUST保持不变

