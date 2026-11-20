# AsepriteExtractor - Aseprite 格式转换

AsepriteExtractor (`shulker.ase`) 是 ShulkerRDK 的 Aseprite 文件处理扩展, 基于 [AsepriteDotNet](https://www.nuget.org/packages/AsepriteDotNet) 构建

将 `.aseprite` 文件转换为 PNG 格式 *(导出第 0 帧)*, 并支持通过图层标签系统导出多个精灵变体, 用于 Minecraft 资源包开发中的像素画纹理流程

## 命令 / 方法一览

```lvt
ase <路径> [输出路径]    # 将 .aseprite 文件(批量)转换为 PNG
```

`ase` 同时注册为交互命令和 Levitate 方法, 可在命令行直接执行, 也可在 Levitate 脚本中调用

---

## 详情

### ase - Aseprite 转 PNG

将 `.aseprite` 文件(批量)转换为 PNG 格式:

```lvt
ase <路径> [输出路径]
```

**参数:**
- `路径` - 单个 `.aseprite` 文件路径, 或包含 `.aseprite` 文件的目录路径
- `输出路径` (可选) - 输出目录。不指定时默认输出到源目录

**工作原理:**
- **目录模式**: 递归扫描所有 `*.aseprite` 文件, 逐个转换并保留目录结构
- **单文件模式**: 转换单个 `.aseprite` 为 PNG

**示例:**
```
>ase src/sprites
正在转换[src/sprites]>[src/sprites]
完成!

>ase character.aseprite out/
正在转换[character.aseprite]>[out/]
完成!
```

---

## 图层标签系统

通过在 Aseprite 图层名称中使用 `#` 作为标签分隔符, 可以实现从单个 `.aseprite` 文件导出多个精灵变体:

### 标签语法

| 图层命名 | 说明 |
| :--- | :--- |
| `图层名` (无 `#`) | 基础图层, 所有导出变体中都会包含 |
| `图层名#标签` | 带标签的图层, 仅在对应标签的变体中渲染 |
| `图层名#标签A#标签B` | 同一图层可以属于多个标签 |
| `#disableBase` | 特殊图层名, 禁用基础变体的导出 |

### 导出规则

1. 所有不含 `#` 的可见图层会被归入基础图层组 (隐藏且不含 `#` 的图层会被跳过)
2. 含 `#` 的图层按标签分组 — **即使在 Aseprite 里隐藏也会参与对应标签的导出**, 这样编辑多个变体时不用担心图层互相遮挡
3. 导出的每个变体 = **基础图层** + **该标签的图层** (同索引的标签图层会覆盖基础图层)
4. 瓦片图层 (Tilemap) 会被忽略
5. 若存在 `#disableBase` 图层, 不会导出基础变体

### 输出命名

标签还支持用 `*` 决定文件名放在前缀还是后缀:

| 变体 | 输出文件名 (`{文件名}` 为原文件不含扩展名) |
| :--- | :--- |
| 基础变体 | `{文件名}.png` |
| 不带 `*` 的标签 (如 `#_iron`) | `{文件名}{标签}.png` -> `character_iron.png` |
| 带 `*` 的标签 (如 `#iron_*` 或 `#pre_*_suf`) | `*` 前作为前缀, `*` 后作为后缀 -> `iron_character.png` / `pre_character_suf.png` |

### 示例

**Aseprite 图层结构:**

```text
character.aseprite
├── body            (基础图层)
├── eyes            (基础图层)
├── armor#_iron     (铁盔甲图层)
├── helmet#_iron    (铁头盔图层)
├── armor#gold_*    (金盔甲图层, 前缀用法)
├── helmet#gold_*   (金头盔图层, 前缀用法)
├── #disableBase    (禁用基础变体)
```

**执行:**
```lvt
ase character.aseprite
```

**输出:**
```text
character_iron.png    (body + eyes + armor 铁 + helmet 铁)
gold_character.png    (body + eyes + armor 金 + helmet 金)
```

> [!NOTE]
> 由于存在 `#disableBase` 图层, 不会导出 `character.png` (基础变体被禁用)

**无 `#disableBase` 的情况:**

```text
tree.aseprite
├── trunk
├── leaves
├── leaves#_autumn     (秋季叶子, 覆盖 leaves)
├── trunk#_autumn      (秋季树干, 覆盖 trunk)
```

输出:
```text
tree.png              (trunk + leaves)
tree_autumn.png       (trunk + leaves, 其中 leaves/trunk 被 #autumn 图层覆盖)
```

> [!TIP]
> 使用图层标签系统可以在单个 `.aseprite` 文件中管理同一精灵的多个变体 (如不同材质、季节、状态等), 大幅减少文件数量和维护成本

---

## 在 Levitate 脚本中使用

```lvt
import shulker.ase

# 批量转换目录
ase src/sprites build/sprites

# 转换单个文件
ase src/sprites/character.aseprite build/sprites

# 配合项目变量
ase "%project.src%/sprites" "%project.cache%/sprites"
```
