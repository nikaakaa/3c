## 1. 冻结并核对0.4迁移输入

- [x] 1.1 记录独立package、3C和`pik`关联change及统一migration identity，确认各仓库只修改自身Owner文件
- [x] 1.2 盘点3C旧package、Tools、namespace、Analyzer、Event／View／Bridge／Adapter／Getter／Extractor／Column／CsvBinding、Reader、manifest、asmdef、portable工程与Repository Policy引用
- [x] 1.3 核对独立Owner发布提交为`e43af24`、package为0.5.2、Analyzer SHA-256为`47EE5F876377EBE453E98009E5AEA6D95FECC8DC4491B1A9EBE881812C55AA87`且MVID为`9b3d5a64-d7e9-46c4-a687-52e87a5bc84a`

## 2. 切换3C依赖与多Fact Root声明

- [x] 2.1 将3C manifest与packages lock切换为唯一`file:../../../../../generated-diagnostic-sampling/Packages/com.kk.generated-diagnostic-sampling`依赖并删除本地Analyzer来源
- [x] 2.2 将Foot Capability收敛为Metadata与多个既有readonly Fact Root，确认Root ID、类型和Program Dimension顺序唯一稳定
- [x] 2.3 将普通`DiagnosticField`迁到真实业务成员，使用path-scoped identity；删除普通Getter／Extractor与Runtime Derived
- [x] 2.4 迁移固定集合`DiagnosticTable`，确认Generator直接读取既有Count／只读索引器并生成子表，不复制行DTO

## 3. 接入generated Event handler与Host

- [x] 3.1 删除Started／CommittedSample／Stopped Event DTO、Dimension View、Side Metadata、Consumer／Binding、Bridge和手写Capture转发，在正式同步Commit点只调用一行Foot `DiagnosticEvent` partial方法并以`in`传入目标、真实lineage与Left／Right Fact Root，Metadata只在Start冻结
- [x] 3.2 检查generated target-scoped dispatcher／Program handler自动完成左右Dimension packet rent／Capture／submit／Fault，Host workflow唯一完成Session Start／Stop、订阅／退订和封存，领域不直接控制Session或Writer
- [x] 3.3 删除Host Adapter、`hostAdapterId`、Column、CsvBinding、Geometry Header、旧Reader和第二CSV映射
- [x] 3.4 从采样链删除旧单体Analyzer／Publisher、评分报告和Diagnosis Store，以Host自动生成的CSV、typed artifact和manifest作为唯一基础采样产物；领域诊断迁移交由`add-schema-driven-diagnostic-analysis`
- [x] 3.5 重建Foot Schema、Program与Capability identity，确认0.1至0.5.1的旧Request／直接HandleCommitted／重分配评分输出不能进入0.5.2链

## 4. 删除旧Owner与统一文档

- [x] 4.1 删除`3cDemo/Shared/UnityPackages/com.thirdperson.generated-diagnostic-sampling`、`Tools/ThirdPersonGeneratedDiagnosticSampling`及对应Repository Policy allowlist
- [x] 4.2 全文搜索并删除采样链旧package／namespace、生命周期Event DTO／Dimension View／Consumer／Binding／Projection／Bridge／Adapter／Getter／Extractor／Column／CsvBinding、兼容Reader和fallback引用；Live／Trace／Gizmo专用View不作为采样输入
- [x] 4.3 将`add-generated-diagnostic-sampling-framework`、Foot与本change同步到multi Fact Root、target-scoped `DiagnosticEvent` typed handler、可选partial Query、自动Host和Disabled零闭包口径并严格校验

## 5. 构建与产物Gate

- [ ] 5.1 使用规定的无build server参数构建3C受影响portable工程并立即关闭build server，确认0.4 Generator与Runtime／Host合同0 error
- [ ] 5.2 执行3C Unity refresh与Console编译核对，确认多Fact Root直接访问、generated `DiagnosticEvent` dispatcher／Program handler、Dimension布局、条件程序集和Analyzer identity正确
- [ ] 5.3 执行Repository Policy、`git diff --check`及旧Owner／ABI／identity搜索，区分既有问题并确认迁移新增问题为零
- [ ] 5.4 构建真实Capture Player并核对只包含匹配0.4的Foot Generated Program、Runtime、Schema与identity闭包
- [ ] 5.5 构建真实Disabled Player，使用Cecil／IL2CPP证明零Diagnostic Attribute／Sampling AssemblyRef、零采样程序集／生成代码／Runtime状态和零Capability／Field identity
- [ ] 5.6 使用统一migration identity提交3C闭合迁移，记录独立package与`pik`精确关联commit并确认迁移范围工作区干净
- [ ] 5.7 用户端验收完成后更新3C current specs／project truth并归档；完成前保持active
