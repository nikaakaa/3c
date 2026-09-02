## Why

通用采样框架已经由独立仓库发布0.4.0 multi Fact Root版本，3C仍需完成从旧单View／Event／Getter链到纯消费者链的破坏性迁移。最终接入必须直接采样已有业务事实，不能为了采样构造View、复制字段或保留第二套Host映射。

## What Changes

- 本change只实施3C仓库内的消费者迁移；独立Owner固定为`D:/Unity_Project_1/generated-diagnostic-sampling`的`507ccbdb3c780f45d36b21bb044eedb4b53ec4a9`，package版本为0.4.0，Analyzer SHA-256为`26577FD50C2DED7C7572E97C223777BCEF9C545BAB30379C5B6172C727AC20C1`。
- 3C Unity manifest与packages lock只解析独立`com.kk.generated-diagnostic-sampling` file dependency，不保存框架源码、Analyzer镜像或同步脚本。
- **BREAKING**：Foot Capability使用多个`DiagnosticFactRoot(rootId, type)`声明一个维度所需的既有readonly事实根；普通字段Attribute直接位于真实业务成员，默认Field identity由Capability、Table、Fact Root与成员路径生成。
- **BREAKING**：删除Started／CommittedSample／Stopped Event DTO、Dimension View、Bridge、Adapter、Column、CsvBinding及普通Getter／Extractor。Program声明左右Dimension，Commit点把左右既有事实与Metadata按`in`传给Generator生成的`HandleCommitted`。
- Generator负责直接成员访问、类型和codec推断、统一Schema、左右Dimension参数布局、packet rent／Capture／submit及Lifecycle；Host按Schema自动生成主表、子表、CSV和manifest。
- 只有真正计算公式可以保留`DiagnosticDerivedField`；公式只声明实际读取的Fact Root参数，最后接收`in Metadata`，不能用Derived包装普通成员。
- Disabled Player不定义`KK_DIAGNOSTIC_SAMPLING`／`KK_DIAGNOSTIC_FOOT`，并以Cecil／IL2CPP产物Gate证明零Attribute、零Sampling AssemblyRef、零采样程序集、零生成代码和零identity。
- **BREAKING**：删除3C本地旧package、Tools及Repository Policy allowlist，不保留兼容Reader、type forwarder、fallback或旧packet路径。

## Capabilities

### New Capabilities

- `generated-diagnostic-sampling-external-consumption`: 定义3C如何只消费独立0.4.0 KK package，以多Fact Root和generated `HandleCommitted`接入Foot，并彻底删除本地框架Owner与旧映射链。

### Modified Capabilities

无。Generated Diagnostic Sampling Framework由独立仓库拥有，本change只安装3C消费者边界。

## Impact

- 依赖：`3cDemo/Client/3C_Client/Packages/manifest.json`与`packages-lock.json`。
- 领域声明：Foot真实readonly事实成员、Capability、Sampler、Program和少量Derived公式。
- Commit接点：正式同步Commit成功后直接调用generated `HandleCommitted`，不发布采样Event DTO。
- Host消费：Foot把自动CSV、typed artifact和manifest作为最终诊断产物，不迁移旧Analyzer／Publisher与评分报告。
- 删除范围：旧embedded package、Tools、Event／View／Bridge／Adapter／Column／CsvBinding／Getter／Extractor与兼容身份。
- 验收边界：3C真实Capture与Disabled Player Gate尚未完成；完成前本change保持active且不得归档。
