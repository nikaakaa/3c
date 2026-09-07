## ADDED Requirements

### Requirement: 源码版本必须由干净提交与完整构建输入证明

正式开发候选 MUST记录 SourceCommit、SourceTree、封存 Git bundle 与解析后的构建依赖身份。作者标签、分支和 worktree 路径 MUST只作为显示/定位信息。准备输入产生未提交变化、构建期间输入改变或无法证明监测完整性时 MUST停止发布；系统 MUST不自动提交、补造提交号或读取主工作区未声明文件补齐其他 worktree。

#### Scenario: 新工作区缺少被忽略的正式源码

- **WHEN** 构建依赖指向 Git 提交和声明依赖中均不存在的源码
- **THEN** 构建 MUST报告缺失输入并失败
- **AND** MUST不从另一个工作区复制该文件后静默继续

#### Scenario: 删除来源 worktree

- **WHEN** 作者移除已经发布候选的工作区
- **THEN** 已封存的提交、依赖和候选 MUST仍可由产物库定位
- **AND** 工作区路径 MUST不成为读取历史报告的必要条件

### Requirement: 工具版本必须由模块声明并由发布器记录精确文件

每个自有工具 MUST由自身正式描述声明 ToolId、显示版本、接口版本和输出数据格式；Metric/算法的含义修订 MUST由对应 Owner 维护。发布器 MUST自动记录工具源码来源、精确文件与依赖哈希。Center 和其他模块的发布器 MUST不代管其版本常量。外部 KK 包 MUST继续由独立包 Owner 发布；安装工具 MUST通过明确配置记录实际可执行文件与依赖身份。

#### Scenario: 工具实现变化但显示版本未更新

- **WHEN** 两份工具显示版本相同而文件闭包不同
- **THEN** 它们 MUST具有不同 ToolArtifactId，并显示版本声明冲突供维护者修正
- **AND** 系统 MUST不因版本字符串相同把两者认定为同一测量工具

#### Scenario: 发布器消费织入器

- **WHEN** 候选构建选择一份已发布织入器
- **THEN** 构建 MUST读取该织入器自身版本描述并固定精确包身份
- **AND** MUST不从全局最新版本或发布器中的常量猜测其版本

### Requirement: 候选身份必须区分同源码的不同构建和嵌入工具

CandidateId MUST在构建前由 ProductId、SourceId、构建配方、Build 工具、Unity/平台、业务配置、嵌入采样工具与完整 DiagnosticCapabilitySet 计算。最终文件闭包 MUST由 Candidate manifest/hash 单独封存，Run MUST同时固定二者，禁止 ID 与包含该 ID 的文件形成循环。内容身份 MUST使用排序的规范化相对路径与内容，不包含显示标签、时间、工作区路径或自身 identity 字段。候选发布 MUST不可覆盖，同声明不同实际文件 MUST明确失败。

#### Scenario: 同提交分别构建普通与分配诊断版本

- **WHEN** 同一源码使用不同嵌入采样能力或构建配方
- **THEN** 构建 MUST生成不同 CandidateId 并分别记录测量能力
- **AND** MUST不通过更改作者标签覆盖既有版本

#### Scenario: 两个发布者发布相同身份

- **WHEN** 两个 worktree 同时发布相同内容身份
- **THEN** 发布器 MUST在原子发布边界核对完整闭包后返回同一已存在产物
- **AND** 任一文件差异 MUST报错，不合并目录

### Requirement: 开发产物必须位于唯一显式项目产物库

ProjectId 与 ArtifactRoot MUST通过正式配置显式绑定。ArtifactRoot MUST位于项目所有已登记 worktree、客户端工程及 Library 之外。源码、工具、场景、候选、运行、采样、分析、比较 MUST使用 design 中唯一目录合同；客户端商业 Content/Player MUST保持独立现行目录。缺失配置或路径冲突 MUST失败，不推导默认位置、不双写、不建立链接镜像。

#### Scenario: 为另一个 worktree 打开 Center

- **WHEN** 两个工作区登记到同一 ProjectId
- **THEN** Center MUST读取同一产物库的候选与结果
- **AND** 它们的 Library 与 Editor 会话状态 MUST继续彼此独立

### Requirement: 不可变产物必须使用完整引用关系和原子发布

发布 MUST先写独立 staging，核对文件集合、内容及输入引用后在产物库同卷原子发布。跨盘 Unity 临时输出 MUST先复制到 staging 并重新校验，不能直接成为可运行候选。Catalog MUST只索引正式 manifest。RunRequest MUST在入队时固定，Analysis 和 Comparison MUST引用精确输入 manifest/hash，不能在完成时替换成最新输入。

#### Scenario: 排队期间工具升级

- **WHEN** 一个请求已入队而作者发布同工具的新版本
- **THEN** 请求 MUST继续引用入队时固定的版本
- **AND** 旧版本缺失时 MUST失败，不自动改用新版本

### Requirement: 历史证据不得冒充合法候选且删除必须保护引用

旧无源码/工具身份的产物 MUST不得升级、补写当前提交或进入正式候选/比较目录。需要保留的证据 MAY在显式操作下封存至 History，保持原始内容并标记不可执行、不可作基线。删除工具、候选或原始数据 MUST检查活动任务和保留结果引用；工作区移除 MUST不删除产物。

#### Scenario: 保留旧性能采样

- **WHEN** 旧采样只有 BuildId 而缺少可证明的源码版本
- **THEN** 其原始证据 MAY保留为只读 History
- **AND** MUST不生成一个当前 commit 让它通过新版比较

#### Scenario: 报告仍引用候选

- **WHEN** 作者要求删除一个被保留报告引用的候选
- **THEN** 系统 MUST列出依赖并拒绝孤立删除
- **AND** MUST不留下指向失效文件的成功报告
