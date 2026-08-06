# character-motion-matching-presentation-module Specification

## REMOVED Requirements

### Requirement: Motion Matching表现状态必须由唯一深Module拥有

系统 MUST不再由actor级深Module聚合Trajectory、查询、选择、Pose History、Player和Reset状态。可变选择与播放状态 MUST属于具体`MotionMatchingPose`节点实例，共享能力 MUST收敛到不可变Frame Context与无状态Search Kernel。

#### Scenario: 删除旧Module owner

- **WHEN** 本change实施完成
- **THEN** Runtime MUST不存在保存当前MM selection或history的`CharacterMotionMatchingPresentationModule`
- **AND** 两个MM节点 MUST不通过actor级current selection互相覆盖

### Requirement: Trajectory Adapter具体类型必须隐藏在Module内部

Trajectory具体适配不再由旧深Module拥有。Trajectory MUST在`CharacterMotionMatchingFrameContext`边界解析为正式typed query。

#### Scenario: 解析Trajectory

- **WHEN** Presentation Stage建立MM Frame Context
- **THEN** adapter MUST只发布规范化Trajectory query
- **AND** MM节点 MUST不依赖旧Module具体类型

### Requirement: MM表现帧必须是固定Resolve与Complete事务

旧Module Resolve/Complete事务 MUST删除。MM阶段 MUST并入唯一Pose Plan的Frame Context resolve、History read、Search、Node pose、History commit和FinalPublication阶段。

#### Scenario: 执行表现帧

- **WHEN** Pose Plan包含MM节点
- **THEN** executor MUST按Pose Plan依赖执行MM阶段
- **AND** MUST不额外调用旧Module Complete

### Requirement: Player continuity与source usage权威不得进入MM Module

旧Module边界不再存在。Player continuity与source usage MUST直接进入`MotionMatchingPose`节点内部owner，而不是进入共享Frame Context或Search Kernel。

#### Scenario: Jump发生

- **WHEN** Search Kernel返回Jump
- **THEN** 调用节点 MUST原子更新entry与source usage
- **AND** 共享runtime MUST不保存该Jump的active player

### Requirement: MM Reset与Lifetime必须在Module内原子收敛

Reset与Lifetime MUST不再由actor级Module统一拥有。每个MM节点 MUST按自己的relevance、Rig revision、binding revision和明确reset policy原子重置。

#### Scenario: 一个State失去relevance

- **WHEN** 仅其中一个MM节点离开relevance
- **THEN** Runtime MUST只应用该节点的reset policy
- **AND** MUST不清空同Actor其它MM节点状态

### Requirement: MM Diagnostics与Replay必须通过同一Module帧合同

Diagnostics与Replay MUST不再依赖旧Module帧合同。它们 MUST读取正式Frame Context、Search Plan、MM node state、Blend pages和History pages。

#### Scenario: 回放MM查询

- **WHEN** Replay恢复一个已记录表现帧
- **THEN** 查询解释 MUST使用记录的typed输入与node identity
- **AND** MUST不构造旧Module或shadow selection

### Requirement: 第三方Motion Matching运行时不得形成第二动画路径

该边界由`character-motion-matching-runtime-kernel`接管。旧Module adapter和其动画路径 MUST删除。

#### Scenario: 清理旧第三方入口

- **WHEN** 本change完成迁移
- **THEN** 第三方能力 MUST只可作为Search Kernel内部纯查询实现
- **AND** MUST不存在Module级player或Transform写入

### Requirement: Query Fixture Preview必须复用正式MM Module与唯一Pose链

独立Query Fixture Preview MUST不再作为正式内容入口。Preview MUST从Presentation Profile和Pose Graph运行正式Chooser、MM node和Pose Plan。

#### Scenario: 预览新增MM角色

- **WHEN** 作者从`MotionMatchingDemoCharacter` Presentation Profile打开MM Preview
- **THEN** Preview MUST使用该Prefab正式Definition引用的binding与生成物
- **AND** MUST不要求独立validation profile或fixture专用selection链
