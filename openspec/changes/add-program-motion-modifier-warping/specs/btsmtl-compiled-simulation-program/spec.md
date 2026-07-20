## ADDED Requirements

### Requirement: Program 必须声明 Motion Modifier descriptor 与固定顺序

Target Program MUST保存按channel索引的canonical Motion Modifier descriptor，包含operation、source Motion operation、Timeline owner、Action Context owner和state slot range。ProgramHash与LayoutHash MUST覆盖descriptor内容和顺序。Runtime MUST不扫描authoring asset、按字符串发现handler或根据Network Model改变顺序。

#### Scenario: 同一 Authoring 编译两个 Target

- **WHEN** 同一Semantic IR分别降低为Float32与Fixed Program
- **THEN** 两个Program MUST包含同语义modifier descriptor和source关系
- **AND** 数值表示差异 MUST不改变modifier eligibility与顺序

### Requirement: MotionWarp 跨 Tick 数据必须进入 Character State Layout

Program MUST为每个MotionWarp operation声明恢复后继续执行所需的typed state，包括active/generation、ActionInstance、窗口起始pose、总position/yaw correction与上一累计progress。同Step raw contribution、resolved channel、modifier output与CharacterMotionRequest MUST保持transient且不得进入committed state。

#### Scenario: 检查 MotionWarp state layout

- **WHEN** Compiler生成包含MotionWarp的Program
- **THEN** Program MUST声明完整Warp state slots和默认值
- **AND** Program MUST不为resolved channel或最终request分配跨Tickslot

### Requirement: MotionWarp 版本变化必须拒绝旧 Artifact

增加MotionWarp operation、descriptor、ActionTargetRequirement或Warp state schema时，Frontend、Operation Set、Target ABI、Program artifact与State codec identity MUST按实际payload变化提升。旧reader、旧state payload、兼容operation分派和字段猜测 MUST删除。

#### Scenario: Session 加载旧 Program

- **WHEN** composition读取MotionWarp版本升级前的Program或State payload
- **THEN** composition MUST在Session启动前明确失败
- **AND** MUST不把缺失descriptor解释为无Modifier
