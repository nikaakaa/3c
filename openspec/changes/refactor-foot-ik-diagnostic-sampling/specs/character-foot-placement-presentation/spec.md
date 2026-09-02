## MODIFIED Requirements

### Requirement: Foot Placement诊断必须只显示正式结果

Runtime Result MUST与Diagnostics严格分型。Foot、Landing、Ground Path、Pelvis、Goal、FBBIK与Final Publication MUST只按业务管线产生正式结果；PoseGraph MUST只拥有自己的Pending／Committed事务与Seal，并在成功Seal后的同步Commit调用栈内暴露现有只读事实，不得为了采样新增事实结构、Dimension类型、领域生命周期Event、Side metadata、packet、Session、Host或第二发布路径。

需要采样的现有真实readonly成员 MAY增加Conditional DiagnosticField。业务 MUST只在成功Seal后的既有PostCommit调用栈中调用一行带`DiagnosticEvent`的partial方法，并以in参数传入现有Left／Right与公共事实根；Generator生成typed dispatcher和Program handler。该接点 MUST不改变成员值、对象布局、业务执行顺序或无采样构建的运行闭包，也不得构造采样DTO、逐字段复制、按Metadata Side选择、重新执行World Query、坐标变换、Goal Assembly、FBBIK或Physical读取。

Gizmo、Trace与Pose Watch MUST继续只读取各自允许的Committed事实。Generated Program MUST只读取本次PostCommit提供的根并写framework-owned packet；后台 MUST不持有业务page。通用Host与Editor workflow MUST只消费sealed packet与生成artifact，不得访问Pending Workspace、Vendor对象、场景Transform或可写Runtime Target。旧Foot单体Analyzer／Publisher、评分报告与Diagnosis Store MUST从采样链删除；独立Host-only Foot Analysis MAY按`add-schema-driven-diagnostic-analysis`只读Completed artifact恢复领域算法，不得反向读取Runtime或改变正式结果。

#### Scenario: 捕获正式Foot事实

- **WHEN** Foot、Pelvis、Goal、FBBIK与Final Publication在同一Frame和Completion成功Seal
- **THEN** 既有PostCommit Owner MUST只调用一行Foot `DiagnosticEvent` partial方法，把现有左右脚与公共Fact Root交给同一个generated typed dispatcher
- **AND** PoseGraph MUST不发布采样专用Event或第二事实页，Foot业务结果 MUST不因Sampler数量变化

#### Scenario: 当前没有Foot采样

- **WHEN** 构建未包含Foot Diagnostics或Session没有选择Foot Sampler
- **THEN** Foot、Ground Path、Goal、FBBIK与Final Publication MUST只执行原业务链
- **AND** MUST不构造采样事实、不执行额外坐标变换、不创建packet或保留采样interest

#### Scenario: Writer失败

- **WHEN** 当前帧在Seal前Discard或Final Publication失败
- **THEN** Foot采样 MUST不提交该Frame的packet
- **AND** MUST不借用上一帧、Pending结果、Pose Watch或当前Transform补成记录

#### Scenario: 多个Sampler共享同一帧

- **WHEN** 同一个Program组合多个Foot Sampler
- **THEN** Generated Program MUST从同一PostCommit根集合求字段并集
- **AND** Foot查询、Ground page、Goal Assembly、FBBIK与Final Publication执行次数 MUST保持不变
