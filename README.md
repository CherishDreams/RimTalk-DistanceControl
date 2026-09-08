# RimTalk Distance Control

[English](README_EN.md) | **中文**

一个 RimWorld 模组，允许自定义 [RimTalk](https://steamcommunity.com/sharedfiles/filedetails/?id=3365145210) 中小人对话的距离和范围限制。

## 功能特性

### 距离与范围控制

- **对话最大距离** — 调整小人之间可对话的最大距离（默认：20，0 = 无限制）
- **同房间要求** — 开关是否要求小人必须在同一房间才能对话（默认：开启）
- **听觉检测范围** — 调整基于听觉的小人选择检测范围（默认：10）
- **视觉检测范围** — 调整基于视觉的小人选择检测范围（默认：20）
- **环境上下文采集距离** — 调整周围建筑、物品、动植物的扫描半径（默认：5）
- **公告/广播听距范围** — 调整公告类对话的听觉检测范围（默认：30）

### 社交效果控制

- **拦截被忽视 debuff** — 可选拦截 RimTalk 施加的 `Slighted`（被忽视/被轻视）负面思想（默认：关闭）

## 前置依赖

- RimWorld 1.5+
- [RimTalk](https://steamcommunity.com/sharedfiles/filedetails/?id=3365145210)（`cj.rimtalk`）
- Harmony（RimWorld 内置）

## 安装方法

1. 在 Steam 创意工坊订阅此模组，或将文件夹复制到 `RimWorld/Mods/`
2. 确保 **RimTalk** 在此模组之前加载
3. 在游戏选项 → Mod 设置 → RimTalk Distance Control 中配置参数

## 构建

```bash
cd Source
dotnet build
```

输出：`1.6/Assemblies/RimTalkDistanceControl.dll`

## 许可证

MIT
