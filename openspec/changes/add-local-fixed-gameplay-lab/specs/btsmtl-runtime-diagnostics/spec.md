## ADDED Requirements

### Requirement: Debug Source Map必须保存作者容器内容版本

Compiler MUST为每个Graph与Timeline来源的Program SourceMap entry保存对应作者容器的content hash。Graph、Node、Edge与Blackboard declaration MUST使用所属Graph fingerprint；Timeline、Track、Clip与TreeClip MUST使用所属Timeline fingerprint。Runtime Debug MUST以Timeline、Track或Clip identity优先于其owner Graph/Node identity建立Source Map，并 MUST让同一作者容器出现不同content hash时创建失败。Runtime Debug MUST不使用整个ProgramHash替代Graph或Timeline content hash。

#### Scenario: Timeline operation同时带有owner Node identity

- **WHEN**一个compiled Timeline operation同时记录owner Graph、owner Node、Timeline、Track与Clip identity
- **THEN**Runtime Debug Source Map MUST把该operation映射到Clip并建立Track与Timeline父容器
- **AND**Timeline父容器content hash MUST等于该Timeline的作者fingerprint
- **AND**MUST不因owner Node优先级而报告Timeline source missing

#### Scenario: Timeline已修改但Program尚未重建

- **WHEN**Timeline Authoring fingerprint与运行Program SourceMap中的content hash不同
- **THEN**Live Debug MUST报告revision mismatch
- **AND**MUST不按AuthoringId、名称、路径或ProgramHash继续附着
