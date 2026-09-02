# 执行记录

## 冻结独立package输入与3C迁移清单

独立Owner仓库为`D:/Unity_Project_1/generated-diagnostic-sampling`，消费commit为`57681605d1e9b017bf0bb4a829700eb7db7bb1da`，package为`com.kk.generated-diagnostic-sampling` 0.1.0。正式Analyzer SHA-256为`D73D39C59271BFE6AC0A34C7E45C4CF2D74DE44239106D9A2BB967C8C63D8C73`，MVID为`c91a40d8-7c63-40d8-a342-a60f1efe2cf1`；源码、UPM Analyzer与独立仓库记录一致。迁移前复核发现旧发布DLL与当前Release输出身份不一致，独立Owner已增加确定性构建、CI构建和稳定PathMap，连续两次Rebuild身份一致，Probe全闭包0警告0错误，package与Release的SHA-256和MVID完全相同。

独立package已具备`DiagnosticLifecycleEvent`／`DiagnosticLifecyclePayload`、三个Event生成处理器、Schema-driven CSV／主表／子表／artifact／manifest、Runtime Session／Writer和Host Finalizer。Probe以Started、含Left／Right声明维度的CommittedSample、Stopped三事件闭合生命周期；领域不需要Bridge、Host Adapter、Column或CsvBinding。

3C迁移清单已覆盖Unity manifest／packages lock、Foot using／asmdef／Sampler／Program／三个Event、旧`3cDemo/Shared/UnityPackages/com.thirdperson.generated-diagnostic-sampling`、旧`Tools/ThirdPersonGeneratedDiagnosticSampling`、Repository Policy四项allowlist以及OpenSpec Owner口径。当前Foot字段迁移只修改Foot插件自身，独立package Owner线程明确不修改3C consumer文件；共享index窗口已串行协调。由此完成任务1.1至1.3。

## 3C consumer切换证据

工作区已把manifest与lock改为唯一`file:../../../../../generated-diagnostic-sampling/Packages/com.kk.generated-diagnostic-sampling`，Foot程序集和23个Extractor文件改用`KK.GeneratedDiagnosticSampling`，Full Sampler改为`DiagnosticOutputFormat.Csv`，并声明CaptureStarted、CommittedSample、CaptureStopped三个typed Event及`character-foot-ik/left`、`character-foot-ik/right`两个稳定样本维度。旧embedded package的44个跟踪文件、旧Tools的6个跟踪文件和其134个忽略构建产物已删除；旧package、namespace、Bridge、Host Adapter、Column、CsvBinding、Reader及Session／Writer控制面在Foot消费范围搜索均为零。

Unity MCP重启并恢复`3C_Client@e852139597e42532`后执行全量refresh与脚本编译，`ThirdPersonCharacter.FootIkDiagnosticSampling.dll`和`KK.GeneratedDiagnosticSampling.dll`于2026-09-02 10:46:21重建。Unity生成工程只引用外部KK Analyzer和`KK.GeneratedDiagnosticSampling.csproj`，Analyzer SHA-256为`D73D39C59271BFE6AC0A34C7E45C4CF2D74DE44239106D9A2BB967C8C63D8C73`；Foot程序集包含三个领域Event、`DiagnosticLifecycle`、`CreateDiagnosticLifecycle`、`Handle`及Left／Right维度identity。Console没有C#、package或asmdef错误，唯一Error是既有FinalIK `FBIKChain.reachSmoothing`序列化深度提示。

常规`dotnet build`按规定参数执行并立即关闭build server，但被既有项目图错误阻断：`.NET Framework 4.7.1`的`ThirdPersonGameplay.csproj`引用`netstandard2.1`的`TEngine.Runtime.csproj`；关闭ProjectReference构建后又因对应输出DLL缺失而无法独立编译Foot目标。本迁移不复制DLL、不创建临时引用路径，也不修改无关TEngine目标框架，因此任务4.1仍保持未完成。Repository Policy报告36项既有空meta或其它项目白名单违规，无KK迁移新增项；迁移范围`git diff --check`通过，全局仅有用户Rollback prefab两处既有尾随空格；OpenSpec strict通过。

## 3C核心迁移提交

3C核心consumer切换已提交为`499a24a2fe29efeeb27f7580c7c1325dc5405b54`。提交只包含KK依赖、Foot namespace／Sampler／typed Event、旧embedded package与旧Tools删除、对应Policy删除和本change证据；工作区中的TND package、Performance／Network Policy、Resolved Target／Contact后续字段及其它PoseGraph／Performance修改均未进入该提交。任务4.5仍等待`pik`精确关联commit及完整consumer收口，不因3C核心提交提前勾选。
