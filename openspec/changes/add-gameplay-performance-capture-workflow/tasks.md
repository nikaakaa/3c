## 1. 合同与现有链路收口

- [x] 1.1 定义Performance Scenario、Capture Profile、Budget Profile、Toolchain Definition、Capture Request、Manifest、Status与比较身份合同。
- [x] 1.2 为Gameplay、Session、Simulation和重构后Presentation Owner建立分域Metric Catalog，统一MetricId、Parent、Unit、SampleScope与Aggregation。
- [x] 1.3 迁移现有ProfilerMarker与`ISimulationPerformanceSink`到Catalog名称来源，删除采集器手写Marker数组、三个失效Foot Marker和错误统计命名。
- [x] 1.4 明确Capture Agent、Scenario Runner、Runtime Diagnostics和Gameplay/Pipeline边界，保证采集不注册第二Tick target、不写Gameplay state或Trace Store。

## 2. 可复现场景与Performance Player

- [x] 2.1 建立Performance Scenario作者与发布入口，把选定Fixed Input Trace、local camera input、Variant、Ready条件和捕获范围冻结为版本化Artifact。
- [x] 2.2 实现Scenario Runner，角色输入复用正式trace/drive端口，相机输入复用local camera采样端口，并完成Warmup暂停、Replay Gate与Capture后缀的唯一协议，不回放Camera Transform或创建第二Input writer。
- [x] 2.3 实现Windows x64 IL2CPP Development Performance Player显式Build工作流，关闭Deep Profiling、Script Debugging与Autoconnect，并输出到`Library/Performance/Players` staging。
- [x] 2.4 生成并校验Performance Player exact manifest、PDB/Burst/Native symbols、Scenario catalog和全部文件hash，Build与Capture入口严格分离。

## 3. Player Agent与独立Controller

- [x] 3.1 实现Player Capture Agent的HELLO、Ready、Smoke Stop、Warmup、Replay Continue、Capture Record与Fault状态机和非阻塞`loopback-tcp/1`协议，取消由Controller统一拥有并立即清理，删除Named Pipe传输。
- [x] 3.2 收口`Tools/ThirdPersonPerformanceCapture`的.NET 8 Windows Run Controller，分别拥有Smoke Gate、Replay Gate与Capture的Player、超时、退出码、日志和staging生命周期。
- [x] 3.3 实现显式本机Toolchain配置与preflight，校验WPR、xperf、WPA Exporter、WPA、Profiler打开方式、权限、磁盘、架构和精确版本，不提供工具fallback。
- [x] 3.4 更新Repository Policy精确允许Controller `.csproj`，不宽泛允许其它Tools工程，也不改变现有CI job和Unity禁区。

## 4. Unity与Windows采集

- [x] 4.1 实现只在Completed Replay Gate准入后的Catalog驱动Recorder预分配、Unity binary Profiler log和按RenderFrame/LogicTick对齐的原始Metric采集。
- [x] 4.2 建立`windows-wpr-cpu/1` WPR profile，采集Sampled Profile、Process/Thread、Context Switch和必要File IO，不启用PMC或全量Heap。
- [x] 4.3 Controller只在Smoke与Replay Gate通过且Warmup到达精确边界后使用唯一WPR instance、Capture起止Marker与owned cleanup；不得停止未知WPR session。
- [x] 4.4 收口xperf按Capture Marker区间、目标PID和匹配PDB生成函数Inclusive/Exclusive栈报告，以及WPA Exporter按同区间导出目标Player Context Switch与线程栈CSV的完整链路。
- [x] 4.5 对符号缺失、ETW event丢失、Marker断裂、Player多义PID和Controller/Player异常发布结构化Fault，不产生半成功热点报告。

## 5. 产物、分析与比较

- [x] 5.1 收口`Library/Performance/Gates`与`Captures/.staging`到Completed/Faulted目录的原子发布和全部文件hash闭包。
- [x] 5.2 收口RenderFrame Aggregate、LogicTick Aggregate、Invocation Mean与Parent/Child Inclusive统计，计算P50/P95/P99/Max和超预算数量。
- [x] 5.3 收口xperf函数表与WPA Context Switch表归一化、函数Inclusive/Exclusive样本排序、线程等待证据摘要，不解析ETL或PDB。
- [x] 5.4 收口Budget结果与作者显式Baseline比较，身份不一致、Gate无效或Capture Faulted时明确拒绝。
- [x] 5.5 收口summary、metric samples、process结果、日志、Unity raw、ETL、xperf栈报告、WPA CSV与comparison的版本化manifest引用。

## 6. Launcher、MCP迁移与清理

- [x] 6.1 在唯一`Tools/3C/Launcher`收口Toolchain、Scenario、Performance Player、Smoke Gate、Replay Gate、Budget、Run状态和产物入口，不在Inspector执行重操作。
- [x] 6.2 接入Build、Smoke、Replay、Capture、Cancel Owned Run、Open Summary、Open Unity Standalone Profiler、Open WPA、Select Baseline与Compare命令。
- [x] 6.3 删除旧`Capture Simulation Performance (10s)`菜单、快捷键、SessionState、固定1024容量、单JSON schema和旧读取假设，不保留转发或兼容路径。
- [x] 6.4 同步`openspec/project.md`当前Gate/Capture入口与性能采集边界，核对active Pose Graph重构后的Marker Owner，不修改archive历史报告。
- [x] 6.5 收口`performance.prepare`、`performance.build_player`、`performance.smoke`、`performance.replay`、`performance.capture`与`performance.report` MCP薄入口，长任务按`job_id`轮询并复用唯一workflow。

## 7. 构建与规范收口

- [x] 7.1 使用规定参数构建Controller工程并立即执行`dotnet build-server shutdown`，修复编译与Repository Policy问题。
- [x] 7.2 执行OpenSpec change/all strict validation和工作区diff核对，确保没有Unity batchmode、性能CI job、RenderDoc/PIX自动化或第二采集实现。

## 8. 串行接入通用Generated Diagnostic Sampling Framework

- [ ] 8.1 扩展唯一Performance Build Request只消费canonical、稳定排序`DiagnosticCapabilitySet`，每项保存CapabilityId、Mode、Event Set、Sampler Set、Schema、Program、维度、packet capacity与transport identity，并通过Player专属编译输入选择Disabled或Capture；删除并禁止领域专属Build字段、全局配置隐式重编译和运行时fallback
- [ ] 8.2 扩展BuildIdentity与Player manifest保存完整`DiagnosticCapabilitySet`，由Program identity闭合AOT Generated Event Handler／Program、Generator与packet layout revision；Disabled排除对应领域Definition／Handler，Capture只包含匹配闭包
- [ ] 8.3 扩展Run Request、Player握手与Capture manifest核对同一Set identity；让Controller发布Generated Started／Stopped Event、领域Owner发布CommittedSample Event，使每个Capture Capability独立拥有Session、cadence、lineage、packet流与子manifest，并在Player停止后按稳定CapabilityId运行框架唯一Schema-driven Host Finalizer，不调用领域Host Adapter
- [ ] 8.4 确认Performance只编排顶层Build／Player／Controller／Gate／Capture／Comparer，不解释lineage、不对齐跨Capability sample、不合并packet流或重写子manifest；Comparer拒绝任一Capability的Mode、Program、Sampler Set、Schema、capacity或transport差异，Launcher与MCP仍只复用现有入口且不新增领域按钮
