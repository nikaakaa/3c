# 执行记录

## 当前独立package输入

独立Owner仓库为`D:/Unity_Project_1/generated-diagnostic-sampling`。当前唯一消费输入已经核对为：

- commit：`507ccbdb3c780f45d36b21bb044eedb4b53ec4a9`
- package：`com.kk.generated-diagnostic-sampling` 0.4.0
- Analyzer SHA-256：`26577FD50C2DED7C7572E97C223777BCEF9C545BAB30379C5B6172C727AC20C1`
- Analyzer MVID：`5152d24c-57cd-4f31-9b50-837bfcb93de5`

0.4正式ABI为一个Dimension下多个强类型Fact Root与一个Metadata。Capability通过重复`DiagnosticFactRoot`声明Root；Program声明Dimension；Generator生成multi-`in` `HandleCommitted`、Lifecycle、Schema和packet访问；Host自动生成CSV与manifest。0.1至0.3的单View、Lifecycle Event DTO和旧Analyzer只属于历史，不是本change的有效构建或验收证据。

## 3C已完成部分

3C manifest与packages lock已经切换到唯一外部file dependency。本地旧embedded package、旧Tools及其精确Repository Policy allowlist已经删除。此前3C核心消费者切换提交为`499a24a2fe29efeeb27f7580c7c1325dc5405b54`，它只证明旧Owner删除和外部依赖切换，不证明0.4 multi Fact Root消费闭环。

当前工作区正在把Foot普通Attribute迁到既有readonly业务成员，并把Capability拆为多个Fact Root；旧普通Getter／Extractor正在删除，剩余Derived正在限制为真正公式。这些修改尚未完成统一生成、Commit接线、Host消费和最终提交，因此任务2.2至3.5保持未完成。

## 已失效的旧验收证据

先前针对0.1 Analyzer进行的Unity refresh、三个Lifecycle Event handler、单View Left／Right Dimension和旧Generated Program编译记录不能验证0.4。旧Analyzer SHA-256 `D73D39C59271BFE6AC0A34C7E45C4CF2D74DE44239106D9A2BB967C8C63D8C73`以及相关Event／View生成结果只保留为迁移历史，不得用于勾选当前构建任务。

旧portable构建曾被`.NET Framework 4.7.1`与`netstandard2.1`项目图冲突阻断，该记录同样没有验证0.4。不得通过复制DLL、临时ProjectReference或第二工程绕过正式构建图。

## 尚未完成的验收

以下闭环目前没有完成，不能声明3C采样器已经交付：

- Foot Capability、真实Field／Table、Sampler与Program的完整0.4生成校验。
- 正式同步Commit点对generated `HandleCommitted`的Left／Right多Fact Root调用。
- Event／View／Bridge／Adapter／Getter／Extractor／Column／CsvBinding及旧Reader的零残留搜索。
- Analyzer／Publisher只读Host自动CSV、typed artifact和manifest。
- 真实Capture Player的0.4闭包检查。
- 真实Disabled Player的Cecil／IL2CPP零Attribute、零AssemblyRef、零程序集、零生成代码和零identity检查。
- 统一migration identity最终提交、用户验收、current truth更新与归档。

本change继续保持active。只有上述代码、生成结果和真实Player Gate完成并由用户验收后，才能更新任务状态和归档。
