# AsepriteExtractor - Aseprite 格式转换

AsepriteExtractor 是 ShulkerRDK 的 Aseprite 文件处理扩展, 基于 [AsepriteDotNet](https://www.nuget.org/packages/AsepriteDotNet) 构建

将 `.aseprite` 文件转换为 PNG 格式, 并支持通过图层标签系统导出多个精灵变体, 用于 Minecraft 资源包开发中的像素画纹理流程

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
|----------|------|
| `图层名` (无 `#`) | 基础图层, 所有导出变体中都会包含 |
| `图层名#标签` | 带标签的图层, 仅在对应标签的变体中渲染 |
| `图层名#标签A#标签B` | 同一图层可以属于多个标签 |
| `#disableBase` | 特殊图层名, 禁用基础变体的导出 |

### 导出规则

1. 所有不含 `#` 的可见图层会被归入基础图层组
2. 含 `#` 的图层按标签分组 (取第一个 `#` 后的所有 `#` 分隔片段作为标签)
3. 导出的每个变体 = **基础图层** + **该标签的图层** (同索引的标签图层会覆盖基础图层)
4. 隐藏但名称不含 `#` 的图层会被跳过
5. 瓦片图层 (Tilemap) 会被忽略
6. 若存在 `#disableBase` 图层, 不会导出基础变体

### 输出命名

| 变体 | 输出文件名 |
|------|------------|
| 基础变体 | `{文件名}.png` |
| 标签变体 | `{文件名}#{标签}.png` |

### 示例

**Aseprite 图层结构:**

```
character.aseprite
├── body            (基础图层)
├── eyes            (基础图层)
├── armor#iron      (铁盔甲图层)
├── helmet#iron     (铁头盔图层)
├── armor#gold      (金盔甲图层)
├── helmet#gold     (金头盔图层)
├── #disableBase    (禁用基础变体)
```

**执行:**
```
ase character.aseprite
```

**输出:**
```
character#iron.png    (body + eyes + armor 铁 + helmet 铁)
character#gold.png    (body + eyes + armor 金 + helmet 金)
```

> [!NOTE]
> 由于存在 `#disableBase` 图层, 不会导出 `character.png` (基础变体被禁用)

### 更多示例

**无 `#disableBase` 的情况:**

```
tree.aseprite
├── trunk
├── leaves
├── leaves#autumn     (秋季叶子, 覆盖 leaves)
├── trunk#autumn      (秋季树干, 覆盖 trunk)
```

输出:
```
tree.png              (trunk + leaves)
tree#autumn.png       (trunk + leaves, 其中 leaves/trunk 被 #autumn 图层覆盖)
```

> [!TIP]
> 使用图层标签系统可以在单个 `.aseprite` 文件中管理同一精灵的多个变体 (如不同材质、季节、状态等), 大幅减少文件数量和维护成本

---

## 在 Levitate 脚本中使用

```lvt
# 批量转换目录
ase src/sprites build/sprites

# 转换单个文件
ase src/sprites/character.aseprite build/sprites

# 配合项目变量
ase "%project.src%/sprites" "%project.cache%/sprites"
```

---
