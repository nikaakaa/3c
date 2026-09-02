## Why

`add-generated-diagnostic-sampling-framework`已经证明采样框架不依赖Character、PoseGraph、Foot或Performance业务，并开始同时服务3C Foot诊断与独立`pik`插件。继续把唯一源码放在3C私有目录会让`pik`只能复制实现或反向依赖3C，因此现在必须把它迁成独立KK package，并让两个项目只消费同一份正式源码。

## What Changes

- 新建独立仓库`D:\Unity_Project_1\generated-diagnostic-sampling`，由它唯一拥有Runtime、Host、Roslyn Source Generator、Analyzer binary、portable build工具、Probe、OpenSpec和发布文档。
- 将正式身份一次改为`com.kk.generated-diagnostic-sampling`、`KK.GeneratedDiagnosticSampling`、`KK.GeneratedDiagnosticSampling.Host`与`KK.GeneratedDiagnosticSampling.Generator`，作者与许可证采用已经确定的`KK`／MIT口径。
- **BREAKING**：删除`com.thirdperson.generated-diagnostic-sampling` package id、`ThirdPerson.GeneratedDiagnosticSampling*` namespace／assembly／tool路径和旧Generator binary identity，不提供wrapper、type forwarder、双package id或兼容Analyzer。
- **BREAKING**：Generated Program的Generator、assembly binding、Schema与Program identity会随正式重命名变化；3C Foot所有Program Definition、Player manifest与历史请求必须显式重建，旧identity不得兼容解释。
- 3C与`pik`的Unity项目manifest都通过正式file dependency消费独立仓库中的同一个UPM package；`com.kk.pik`只声明版本化package依赖，不把框架源码复制进自身package。
- 在同一次迁移中更新3C Foot领域插件的using、asmdef、Program生成与构建闭包，确认它仍只拥有领域Definition／Bridge／Host Adapter，不接管Generator、Session、Writer或Host Finalizer。
- 在同一次迁移中从3C删除`3cDemo/Shared/UnityPackages/com.thirdperson.generated-diagnostic-sampling`、`Tools/ThirdPersonGeneratedDiagnosticSampling`及对应Repository Policy allowlist，不保留同步脚本、vendor snapshot或本地镜像。
- 独立仓库只发布通用诊断采样能力，不包含Foot、FinalIK、PoseGraph、Performance Player、3C配置、回放档案或`pik`算法。

## Capabilities

### New Capabilities

- `generated-diagnostic-sampling-package-distribution`: 定义独立KK UPM package的唯一源码所有权、正式公开身份、Analyzer／Runtime发布闭包、消费方式与禁止双份实现的迁移要求。

### Modified Capabilities

无。现行current specs尚未安装Generated Diagnostic Sampling Framework；本change与active `add-generated-diagnostic-sampling-framework`、`refactor-foot-ik-diagnostic-sampling`和`add-gameplay-performance-capture-workflow`对账，不伪造current spec修改。

## Impact

- 新仓库：`D:\Unity_Project_1\generated-diagnostic-sampling`。
- 3C删除路径：`3cDemo/Shared/UnityPackages/com.thirdperson.generated-diagnostic-sampling`、`Tools/ThirdPersonGeneratedDiagnosticSampling`。
- 3C消费路径：Unity `Packages/manifest.json`／`packages-lock.json`、Foot诊断using／asmdef、Repository Policy和相关active change文档。
- `pik`消费路径：项目`Packages/manifest.json`与`Packages/com.kk.pik/package.json`，不修改Foot Placement算法Owner。
- 构建身份：package id、namespace、assembly name、Generator MVID／SHA-256、Schema identity、Program identity与Capability Set identity全部发生一次明确迁移。
- 外部依赖：Unity 2022.3、.NET Standard 2.0兼容Roslyn 3.8、MIT许可证；不新增FinalIK或3C业务依赖。
