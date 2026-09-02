## 1. 冻结三仓迁移输入与唯一Owner边界

- [ ] 1.1 记录3C、`pik`与待建独立仓库的精确迁移identity、前置commit和受影响路径，使用`git status --short`与全文搜索确认Foot并行任务没有占用package／Generator／using／asmdef迁移文件
- [ ] 1.2 盘点`com.thirdperson.generated-diagnostic-sampling`、`ThirdPerson.GeneratedDiagnosticSampling*`、旧Tools路径、Unity manifest、asmdef、portable工程、Repository Policy及OpenSpec引用，输出完整旧→新identity迁移表并确认没有未登记消费者
- [ ] 1.3 固定`D:\Unity_Project_1\generated-diagnostic-sampling`、`com.kk.generated-diagnostic-sampling`、`KK.GeneratedDiagnosticSampling*`、版本`0.1.0`、作者`KK`和MIT许可证，核对proposal／spec／design确认实施期间不再产生占位命名或第二身份

## 2. 建立独立Git与Package唯一源码

- [ ] 2.1 在`D:\Unity_Project_1\generated-diagnostic-sampling`初始化独立Git main仓库、`.gitignore`、README、MIT LICENSE与OpenSpec根，检查`.git`存在且3C／`pik`都没有把该目录作为子模块或嵌套工作树跟踪
- [ ] 2.2 建立`Packages/com.kk.generated-diagnostic-sampling`与`Tools/KK.GeneratedDiagnosticSampling/{Generator,Host,Probe}`正式目录，将3C当前已验证框架迁入并记录来源commit，全文搜索确认独立仓库不包含Foot、FinalIK、PoseGraph、Character、Performance Player或`pik`算法
- [ ] 2.3 将package manifest、namespace、asmdef、assembly name、root namespace、portable csproj、Generator硬编码metadata name与Probe全部改为KK正式identity，全文搜索确认独立仓库中的`ThirdPerson.GeneratedDiagnosticSampling`和旧package id为零
- [ ] 2.4 在独立仓库建立框架与package distribution的OpenSpec／project truth、安装文档和发布闭包说明，严格校验独立仓库OpenSpec并确认文档不把3C或`pik`声明为框架Owner

## 3. 闭合Generator源码与Analyzer发布原子

- [ ] 3.1 使用`--disable-build-servers /nr:false /p:UseSharedCompilation=false`分别构建Runtime、Generator、Host与Probe Release工程，并在结束后立即执行`dotnet build-server shutdown`，确认四个工程0 error
- [ ] 3.2 把唯一Generator Release DLL写入package `RoslynAnalyzers`，核对package与bin DLL的SHA-256、MVID、assembly name和Generator identity完全一致
- [ ] 3.3 检查Probe生成源码，确认具体View／Metadata双输入、固定容量Table、dense handle、Schema、Capability descriptor和AOT Capture函数全部只引用`KK.GeneratedDiagnosticSampling*`
- [ ] 3.4 执行独立仓库`git diff --check`、禁止Expression／Reflection／DynamicInvoke／`object[]`与领域分支搜索，确认package不存在旧Analyzer、兼容wrapper、type forwarder或第二生成路径

## 4. 原子迁移3C消费者并删除旧Owner

- [ ] 4.1 在冻结窗口把3C Unity manifest与packages lock切换为`file:../../../../../generated-diagnostic-sampling/Packages/com.kk.generated-diagnostic-sampling`，检查Unity只解析一个KK package且不再解析旧ThirdPerson package
- [ ] 4.2 将3C Foot Capability、Field／Table／Sampler／Program Definition、using、asmdef和受影响portable工程一次改为KK identity，构建生成程序并确认所有Schema／Program／Capability Set identity按新Generator闭合
- [ ] 4.3 更新3C Repository Policy与相关active change文档的Owner／路径／程序集口径，全文搜索确认Performance、PoseGraph和Foot文档都只把独立仓库视为框架Owner
- [ ] 4.4 删除3C的`3cDemo/Shared/UnityPackages/com.thirdperson.generated-diagnostic-sampling`、`Tools/ThirdPersonGeneratedDiagnosticSampling`及其精确allowlist，不保留同步脚本、vendor snapshot、本地镜像、旧namespace或旧package lock记录
- [ ] 4.5 按规定参数构建3C受影响portable工程并关闭build server，执行3C Unity refresh与Console编译核对，确认Foot双输入Extractor由KK Analyzer生成且不存在package／asmdef／Analyzer错误

## 5. 接入pik但不复制框架实现

- [ ] 5.1 将`pik`项目manifest接入`file:../../generated-diagnostic-sampling/Packages/com.kk.generated-diagnostic-sampling`，并在`com.kk.pik/package.json`声明`com.kk.generated-diagnostic-sampling: 0.1.0`正式依赖
- [ ] 5.2 检查`pik`的package、asmdef和源码树，确认它只消费KK框架并且没有复制Runtime、Generator、Session、Writer、Reader、codec或Host Finalizer
- [ ] 5.3 执行`pik` Unity refresh与Console编译核对，确认stock FinalIK Foot Placement现有链不因新增package依赖改变算法Owner、求解顺序或生成第二诊断路径
- [ ] 5.4 更新`pik` OpenSpec／README的依赖与领域插件边界，严格校验`pik` change并确认它只拥有未来领域Definition／Bridge／Host Adapter扩展点

## 6. 三仓对账与迁移提交

- [ ] 6.1 跨三个仓库搜索package id、namespace、assembly、Tools路径与Analyzer，确认独立仓库是唯一框架源码，3C／`pik`只含正式依赖和领域消费者代码，旧ThirdPerson采样identity为零
- [ ] 6.2 执行独立仓库、3C与`pik`各自的Repository Policy／等价仓库检查、`git diff --check`和OpenSpec严格校验，记录所有既有非本迁移失败并确认本迁移没有新增违规
- [ ] 6.3 核对3C Foot Generated Program、Player manifest、Runtime／Capability manifest和Comparer不再接受迁移前identity，确认系统没有旧packet兼容Reader、运行时fallback或双Capability Set
- [ ] 6.4 使用同一migration identity分别提交独立仓库、3C和`pik`的闭合状态，检查三个仓库`git status --short`在各自迁移范围内为空且提交信息记录相互依赖的精确commit
- [ ] 6.5 在实现与用户端验收完成后更新三个仓库的current specs／project truth并归档对应change；完成前保持active，不提前把独立Owner安装为current truth
