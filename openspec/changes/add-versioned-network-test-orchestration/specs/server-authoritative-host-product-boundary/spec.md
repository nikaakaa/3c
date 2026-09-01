## MODIFIED Requirements

### Requirement: Build与Run必须锁定精确Server Product Identity

每个Authority Network Test Candidate manifest MUST引用匹配的Server Product manifest及其hash。Server Product manifest MUST记录SchemaVersion、CandidateId、ServerProductId、executable、configuration、Entity/Hotfix模块、portable依赖和Authority artifact身份；CandidateId MUST绑定Network Product的SourceCommit/Tree且不得使用时间BuildId。Run MUST在启动前校验全部事实，MUST不触发publish、不接受其它Candidate或Product manifest、不按文件存在性猜测产品。

#### Scenario: DotRecast Run读取另一Candidate的Server Product

- **WHEN** DotRecast Candidate引用不同CandidateId、Unity Authority ServerProductId或错误executable
- **THEN** Run MUST在启动进程前失败并报告产品/候选身份不匹配
- **AND** MUST不改写manifest、切换目录或重新Build

### Requirement: 产品选择必须发生在Build与进程启动边界

Unity Authority与DotRecast Authority MUST保持独立Product根，并在各自根下使用不可变CandidateId目录。同Product不同Candidate MUST并存且不得覆盖；一个Server进程启动后 MUST锁定单一Candidate、单一产品、单一Authority Host route和单一Scene集合，MUST不热切换Candidate/Product或同时安装两种route adapter。运行工具 MUST来自所选Candidate Tool Bundle。

#### Scenario: 两种产品连续Build多个候选

- **WHEN** 作者为Unity Authority和DotRecast Authority分别构建新Candidate
- **THEN** 新目录 MUST只包含对应Candidate的Server、配置、manifest和工具
- **AND** 任何已有Candidate executable、config、manifest与日志 MUST保持不变
