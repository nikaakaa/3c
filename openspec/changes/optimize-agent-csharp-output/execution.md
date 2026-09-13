# 实施记录

## 范围

本次只改 Agent C# authoring 的输出上下文、源码组织、写盘、响应和薄适配；正式 Graph、FSM、Timeline、Pose、Motion、Camera 模型与运行规则不变。

## 代码链

- BtsmtlAuthoringCodeExportContext 记录正式对象变量、局部文件边界、类型化外部引用和阶段语句。
- BtsmtlAuthoringCodeSourceBuilder 输出一个入口、同目录 partial 局部文件和必要的资源加载方法；入口按原阶段顺序调用局部方法，跨文件对象只通过一次性执行状态传递。
- BtsmtlAuthoringCodeFileWriter 逐文件比较 UTF-8 内容；不变文件不写盘，修改文件不触碰已有 .meta，只在入口所属专属目录清理退役 .cs 与 .meta。
- btsmtl.export_code 返回入口、文件集、分区、字节数和创建/修改/未变/删除清单；诊断附带输出文件路径。
- generate_assets 仍只执行一个精确已编译入口，辅助 partial 文件由当前 Unity 编译关联接续，不新增 manifest 或第二入口。

## 已采用输出迁移

已通过正式 btsmtl.export_code 重导并迁移到 Generated/<Root>/<Root>.cs：

- Corin Attack Ability：入口加 5 个 Stage、10 个 Condition、5 个 Timeline 局部文件，共 21 个文件。
- Corin Dodge Back / Dodge Forward Ability：各 4 个文件。
- Corin Attack / Dodge Admission Profile：各 1 个入口文件。
- Locomotion Pose Graph：入口、局部 Slot 文件和 Graph 局部文件，共 3 个文件。
- Corin Animation EventGraph：1 个入口文件。

旧平铺入口及其 .meta 已删除；各专属目录的源码和 .meta 成对保留。MotionCurve 仍只使用正式 RootMotionCurveAsset 引用，没有恢复逐帧曲线或 CurveEndFrame。

## 编译边界

使用：

dotnet build 3cDemo/Client/3C_Client/ThirdPersonClient.Editor.csproj --disable-build-servers /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false -clp:ErrorsOnly

结果：0 个错误、32 个警告；随后执行 dotnet build-server shutdown。当前 Unity Console 的剩余错误来自并行模拟模块的编译索引/类型变更，不属于本次 authoring 输出代码。

本次未新增测试代码、回放任务或自动端到端验收。
