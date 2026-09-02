## Why

通用采样框架已经由独立仓库发布0.5.2 multi Fact Root／DiagnosticEvent／固定权重分析版本，3C仍需完成从旧单View／Consumer／Getter链到纯消费者链的破坏性迁移。最终接入必须直接采样已有业务事实，不能为了采样构造View、复制字段或保留第二套Host映射。

## What Changes

- 本change只实施3C仓库内的消费者迁移；独立Owner固定为`D:/Unity_Project_1/generated-diagnostic-sampling`的`e43af24`，package版本为0.5.2，Analyzer SHA-256为`47EE5F876377EBE453E98009E5AEA6D95FECC8DC4491B1A9EBE881812C55AA87`，MVID为`9b3d5a64-d7e9-46c4-a687-52e87a5bc84a`。
- 3C Unity manifest与packages lock只解析独立`com.kk.generated-diagnostic-sampling` file dependency，不保存框架源码、Analyzer镜像或同步脚本。
- **BREAKING**：Foot Capability使用多个`DiagnosticFactRoot(rootId, type)`声明一个维度所需的既有readonly事实根；普通字段Attribute直接位于真实业务成员，默认Field identity由Capability、Table、Fact Root与成员路径生成。
- **BREAKING**：删除Started／CommittedSample／Stopped Event DTO、Dimension View、Consumer／Binding、Bridge、Adapter、Column、CsvBinding及普通Getter／Extractor。Program声明Event和左右Dimension，业务Commit点只调用一行带`DiagnosticEvent`的partial方法并以`in`传入目标、真实lineage与左右既有事实；Generator生成typed dispatcher和匹配Program handler，Metadata在Lifecycle Start冻结。
- Generator负责直接成员访问、类型和codec推断、统一Schema、左右Dimension参数布局、packet rent／Capture／submit及Lifecycle；Host按Schema自动生成主表、子表、CSV和manifest。
- 只有真正计算公式可以保留`DiagnosticDerivedField`；公式只声明实际读取的Fact Root参数，最后接收`in Metadata`，不能用Derived包装普通成员。
- Disabled Player不定义`KK_DIAGNOSTIC_SAMPLING`／`KK_DIAGNOSTIC_FOOT`，并以Cecil／IL2CPP产物Gate证明零Attribute、零Sampling AssemblyRef、零采样程序集、零生成代码和零identity。
- **BREAKING**：删除3C本地旧package、Tools及Repository Policy allowlist，不保留兼容Reader、type forwarder、fallback或旧packet路径。

## Capabilities

### New Capabilities

- `generated-diagnostic-sampling-external-consumption`: 定义3C如何只消费独立0.5.2 KK package，以多Fact Root和generated `DiagnosticEvent` typed handler接入Foot，并彻底删除本地框架Owner与旧映射链。

### Modified Capabilities

无。Generated Diagnostic Sampling Framework由独立仓库拥有，本change只安装3C消费者边界。

## Impact

- 依赖：`3cDemo/Client/3C_Client/Packages/manifest.json`与`packages-lock.json`。
- 领域声明：Foot真实readonly事实成员、Capability、Sampler、Program和少量Derived公式。
- Commit接点：正式同步Commit成功后只调用一行`DiagnosticEvent` partial方法；它不构造或发布采样Event DTO，Start／Stop继续由Host workflow控制。
- Host消费：Foot把自动CSV、typed artifact和manifest作为唯一基础采样产物；本change不迁移旧单体Analyzer／Publisher与评分报告，后续`add-schema-driven-diagnostic-analysis`只从Completed基础产物恢复独立Foot离线诊断。
- 删除范围：旧embedded package、Tools、Event／View／Bridge／Adapter／Column／CsvBinding／Getter／Extractor与兼容身份。
- 验收边界：3C真实Capture与Disabled Player Gate尚未完成；完成前本change保持active且不得归档。
