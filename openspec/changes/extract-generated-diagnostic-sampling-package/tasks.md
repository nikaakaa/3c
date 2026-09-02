## 1. 冻结并核对0.4迁移输入

- [x] 1.1 记录独立package、3C和`pik`关联change及统一migration identity，确认各仓库只修改自身Owner文件
- [x] 1.2 盘点3C旧package、Tools、namespace、Analyzer、Event／View／Bridge／Adapter／Getter／Extractor／Column／CsvBinding、Reader、manifest、asmdef、portable工程与Repository Policy引用
- [x] 1.3 核对独立Owner HEAD为`507ccbdb3c780f45d36b21bb044eedb4b53ec4a9`、package为0.4.0、Analyzer SHA-256为`26577FD50C2DED7C7572E97C223777BCEF9C545BAB30379C5B6172C727AC20C1`且MVID为`5152d24c-57cd-4f31-9b50-837bfcb93de5`

## 2. 切换3C依赖与多Fact Root声明

- [x] 2.1 将3C manifest与packages lock切换为唯一`file:../../../../../generated-diagnostic-sampling/Packages/com.kk.generated-diagnostic-sampling`依赖并删除本地Analyzer来源
- [ ] 2.2 将Foot Capability收敛为Metadata与多个既有readonly Fact Root，确认Root ID、类型和Program Dimension顺序唯一稳定
- [ ] 2.3 将普通`DiagnosticField`迁到真实业务成员，使用path-scoped identity；删除普通Getter／Extractor并把剩余Derived限制为真正公式
- [ ] 2.4 迁移固定集合`DiagnosticTable`，确认Generator直接读取既有Count／只读索引器并生成子表，不复制行DTO

## 3. 接入generated Commit与Host

- [ ] 3.1 删除Started／CommittedSample／Stopped Event DTO、Dimension View、Side Metadata和Bridge，在正式同步Commit点直接以`in`传入Left／Right Fact Root与Metadata并调用generated `HandleCommitted`
- [ ] 3.2 检查generated Lifecycle自动完成Start、左右Dimension packet rent／Capture／submit、Stop与结构化失败传播，领域不直接控制Session或Writer
- [ ] 3.3 删除Host Adapter、`hostAdapterId`、Column、CsvBinding、Geometry Header、旧Reader和第二CSV映射
- [ ] 3.4 将Foot Analyzer／Publisher切到Host自动生成的基础CSV、typed artifact和manifest，确认它们只负责评分与报告
- [ ] 3.5 重建Foot Schema、Program、Capability Set和Player manifest identity，确认0.1至0.3 Request／packet／Reader不能进入0.4链

## 4. 删除旧Owner与统一文档

- [x] 4.1 删除`3cDemo/Shared/UnityPackages/com.thirdperson.generated-diagnostic-sampling`、`Tools/ThirdPersonGeneratedDiagnosticSampling`及对应Repository Policy allowlist
- [ ] 4.2 全文搜索并删除旧package／namespace、Event／View／Projection／Bridge／Adapter／Getter／Extractor／Column／CsvBinding、兼容Reader和fallback引用
- [ ] 4.3 将`add-generated-diagnostic-sampling-framework`、Foot、Performance与本change同步到0.4 multi Fact Root、generated Commit、自动Host和Disabled零闭包口径并严格校验

## 5. 构建与产物Gate

- [ ] 5.1 使用规定的无build server参数构建3C受影响portable工程并立即关闭build server，确认0.4 Generator与Runtime／Host合同0 error
- [ ] 5.2 执行3C Unity refresh与Console编译核对，确认多Fact Root直接访问、generated `HandleCommitted`、Dimension布局、条件程序集和Analyzer identity正确
- [ ] 5.3 执行Repository Policy、`git diff --check`及旧Owner／ABI／identity搜索，区分既有问题并确认迁移新增问题为零
- [ ] 5.4 构建真实Capture Player并核对只包含匹配0.4的Foot Generated Program、Runtime、Schema与identity闭包
- [ ] 5.5 构建真实Disabled Player，使用Cecil／IL2CPP证明零Diagnostic Attribute／Sampling AssemblyRef、零采样程序集／生成代码／Runtime状态和零Capability／Field identity
- [ ] 5.6 使用统一migration identity提交3C闭合迁移，记录独立package与`pik`精确关联commit并确认迁移范围工作区干净
- [ ] 5.7 用户端验收完成后更新3C current specs／project truth并归档；完成前保持active
