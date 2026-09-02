## 1. 冻结3C迁移输入

- [x] 1.1 记录独立package、3C和`pik`关联change／精确前置commit及统一migration identity，使用`git status --short`确认Foot并行任务没有占用package／Generator／using／asmdef／生命周期文件
- [x] 1.2 盘点3C中旧package id、ThirdPerson namespace／assembly、Tools、Analyzer、manifest、asmdef、Foot Bridge／Host Adapter／Column／CsvBinding、portable工程、Repository Policy和OpenSpec引用，输出完整删除／迁移清单并确认没有未登记消费者
- [x] 1.3 核对独立仓库已提供`com.kk.generated-diagnostic-sampling` 0.1.0、三个typed生命周期Event handler、Schema-driven CSV和hash闭合Analyzer；缺失任一项则停止，不从3C复制旧实现补齐

## 2. 切换3C正式依赖与Foot事件接入

- [x] 2.1 将3C manifest与packages lock切换为`file:../../../../../generated-diagnostic-sampling/Packages/com.kk.generated-diagnostic-sampling`，检查依赖图只含一个KK package与Analyzer
- [x] 2.2 将Foot Capability、Field／Table／Sampler／Program Definition、using、asmdef和受影响portable工程改为`KK.GeneratedDiagnosticSampling*`，构建生成源码确认旧ThirdPerson编译引用为零
- [x] 2.3 定义Foot `CaptureStarted`、`CommittedSample`、`CaptureStopped` typed Event与Left／Right声明式样本维度，检查Generated handler自动Session、租包、Extractor、提交和封存且领域没有手写Bridge循环
- [ ] 2.4 重建Foot Schema、Program、Capability Set和Player manifest identity，确认迁移前Request／packet不能被新Reader或Comparer接受

## 3. 删除3C旧Owner与领域适配层

- [x] 3.1 删除Foot Bridge、每Sampler Host Adapter、`hostAdapterId`消费、手写Column／CsvBinding、Geometry Header、旧Reader与Session／Writer控制面，全文搜索确认Foot只剩Event／Attribute和下游Analyzer／Publisher
- [ ] 3.2 将Foot Analyzer／Publisher切到KK package生成的基础CSV／typed artifact／manifest，确认它们不参与Capture生命周期、不重新声明Schema且失败只产生下游报告身份
- [x] 3.3 删除`3cDemo/Shared/UnityPackages/com.thirdperson.generated-diagnostic-sampling`、`Tools/ThirdPersonGeneratedDiagnosticSampling`和对应Repository Policy allowlist，确认3C不跟踪框架源码、Analyzer镜像或同步脚本
- [ ] 3.4 更新`add-generated-diagnostic-sampling-framework`、Foot、Performance与本change文档的独立Owner／typed Event／内建CSV口径，严格校验相关changes

## 4. 构建、Unity校验与3C提交

- [ ] 4.1 使用规定参数构建3C受影响portable工程并立即执行`dotnet build-server shutdown`，确认Foot生成代码与KK Runtime／Host合同0 error
- [x] 4.2 执行3C Unity refresh与Console编译核对，确认只加载KK package／Analyzer、Foot Generated Event handler与Program且没有package／asmdef错误
- [x] 4.3 执行Repository Policy、`git diff --check`及旧package／namespace／Bridge／Host Adapter／Column／fallback搜索，区分既有违规并确认本迁移新增问题为零
- [ ] 4.4 核对Disabled Player闭包不含Foot Event handler／Program／page／queue／interest，Capture闭包只含匹配KK Event／Program且没有第二Player、Controller或Capability Set
- [ ] 4.5 使用统一migration identity提交3C闭合迁移，记录独立package与`pik`精确关联commit，并检查3C迁移范围`git status --short`为空
- [ ] 4.6 在用户端验收完成后更新3C current specs／project truth并归档本change；完成前保持active，不提前安装外部Owner真相
