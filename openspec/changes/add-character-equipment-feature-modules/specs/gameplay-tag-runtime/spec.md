## ADDED Requirements

### Requirement: Equipment Tag必须按Slot revision拥有稳定source

Equipment commit授予的Tag MUST使用由ActorId、SlotId与EquipmentRevision构成的稳定source identity，并由唯一Gameplay Tag aggregate保存。替换或卸下 MUST只撤销该source；Character State copy、codec与restore MUST重建相同source。系统 MUST不把装备Tag作为无来源bool或写入第二Blackboard。

#### Scenario: 更换MainWeapon

- **WHEN** MainWeapon从revision 4 Sawblade切换到revision 5 Gun
- **THEN** runtime MUST撤销revision 4 source全部Tag并安装revision 5 source Tag
- **AND** 其它Slot和GE source MUST保持

#### Scenario: Character State恢复到旧revision

- **WHEN** Character State codec恢复到MainWeapon revision 4
- **THEN** Tag aggregate MUST恢复revision 4 equipment source
- **AND** Action Required Query MUST得到相同结果

### Requirement: Equipment Action准入必须复用通用Tag Query

Gameplay Tag query evaluator MUST同时服务Action Required、Block、Cancel与Graph Condition查询，并从同一当前transaction view读取Equipment source。Equipment runtime MUST不增加武器专用Tag evaluator、字符串前缀判断或按FeatureId隐式授予未声明Tag。

#### Scenario: Required与Block同时存在

- **WHEN** Attack Required Query命中Sawblade而Block Query命中Stunned
- **THEN** 通用Action admission MUST拒绝该动作
- **AND** Equipment Route选择 MUST不覆盖Block结果
