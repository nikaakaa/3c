# character-presentation-interpolation Specification

## ADDED Requirements

### Requirement: Prediction变步调度与Owner表现时钟必须解耦

Owner Presentation MUST从Committer提交的simulation body sample历史维护独立表现时钟，并按presentation delta推进。它 MUST不假设每个outer logic tick固定完成一个simulation step，也 MUST不把outer interpolation alpha重复套用到未更新的body pair。Prediction restore/replay替换旧body历史时，Presentation MAY保留上一帧可见姿态并在visual root上收敛到新canonical body，但 MUST不修改World body、Prediction state或Solver输入。

#### Scenario: Prediction outer tick产生零步

- **WHEN** 当前outer logic tick没有提交新的simulation body sample
- **THEN** Owner visual root MUST继续到达当前body区间终点并保持
- **AND** MUST不从alpha零重新播放同一body区间

#### Scenario: Prediction outer tick产生双步

- **WHEN** 当前outer logic tick提交两个连续simulation body sample
- **THEN** Owner Presentation MUST按sample tick顺序消费两个区间
- **AND** MUST不覆盖或跳过中间body sample

#### Scenario: Restore替换旧预测分支

- **WHEN** Committed body历史回退到更早tick或同tick canonical pose被替换
- **THEN** Presentation MUST从上一帧可见姿态收敛到新canonical body
- **AND** canonical body MUST立即保持restore/replay后的权威结果

### Requirement: 稀疏网络动画Sample必须按authority tick区间重采样

Remote Presentation MUST允许当前Body插值区间右端tick的SampleProducer提前进入动画采样缓存，并按前后authority sample tick插值Timeline动画时间。可靠Select、Complete、Release、GameplayFact和Cue MUST仍只在authority presentation horizon到达后生效。存在合法右端SampleProducer时，Animation sampling MUST不把20Hz sample间隔作为过期条件转为无约束自由运行。

#### Scenario: 20Hz Snapshot驱动循环移动动画

- **WHEN** Remote body与SampleProducer分别在Tick 300和303形成当前插值区间
- **THEN** Presentation MUST按当前authority presentation time在两个Timeline sample之间插值
- **AND** Animancer MUST在渲染帧连续采样同一producer generation

#### Scenario: 新Producer样本早于可靠Selection到达horizon

- **WHEN** Tick 303的新Producer Sample已缓存但可靠Select尚未到达presentation horizon
- **THEN** Sample MUST只进入采样缓存
- **AND** 当前可见Selection MUST保持不变直到可靠Select正式发布
