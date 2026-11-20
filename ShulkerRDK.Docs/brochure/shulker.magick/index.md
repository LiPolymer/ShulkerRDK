# ResourceMagick - 图像处理与资源包迁移

ResourceMagick (`shulker.magick`) 是 ShulkerRDK 的图像处理扩展, 基于 [Magick.NET](https://github.com/dlemstra/Magick.NET) 构建

提供 PNG / PSD 格式互转、PBR 贴图提取以及 Java 版资源包跨版本迁移 (`mrvc`) 功能

> [!WARNING]
> 图像转换相关的指令与方法在将来可能会被大幅重构, 本文档仅供参考

## 命令与方法一览

```lvt
# 交互指令
png2psd <路径> [输出路径]                  # 将 PNG 文件(批量)转换为 PSD
mrvc    <源包> <目标包> [原版本] <目标版本> # 迁移 Java 资源包版本

# Levitate 方法 (脚本中需先 import shulker.magick)
psdcvt  <路径> [输出路径]                  # 将 PSD 文件(批量)转换为 PNG
pbrex   <路径> [输出路径] [导出基础贴图]    # 从 PSD 中提取 PBR 贴图
mrvc    <源包> <目标包> [原版本] <目标版本> # 迁移 Java 资源包版本
```

### 快速别名

| 别名 | 展开 |
| :--- | :--- |
| `psdCvt` | `psdcvt "%project.src%" "%project.cache%"` |
| `pbrEx` | `pbrex "%project.src%" "%project.cache%"` |

---

## png2psd - PNG 转 PSD

将 PNG 文件(批量)转换为 PSD 格式:

```lvt
png2psd <路径> [输出路径]
```

- **目录模式**: 递归扫描目录下所有 `*.png` *(自动跳过 `*_n.png` 和 `*_s.png`)*, 保留目录结构输出为同名 `.psd`。不指定输出路径时默认输出到 `%project.src%`
- **单文件模式**: 转换单个 PNG；若指定输出路径, 需传入输出 `.psd` 文件的完整路径

```
>png2psd src/textures
>png2psd src/textures/blocks/stone.png out/stone.psd
```

---

## psdcvt - PSD 转 PNG

*仅作为 Levitate 方法提供*

```lvt
psdcvt <路径> [输出路径]
```

- **目录模式**: 递归将目录下所有 `*.psd` 转换为同名 `.png`, 不指定输出目录时默认输出到源目录
- **单文件模式**: 转换单个 PSD；若指定输出路径, 需传入输出 `.png` 文件路径

```lvt
psdcvt src/textures build/cache
```

---

## pbrex - PBR 贴图提取

*仅作为 Levitate 方法提供*

```lvt
pbrex <路径> [输出路径] [导出基础贴图]
```

按 PSD 图层名称前缀分类合成并导出为独立 PNG:

| 图层前缀 | 导出文件 | 说明 |
| :--- | :--- | :--- |
| `n_` | `{name}_n.png` | 法线贴图 |
| `s_` | `{name}_s.png` | 高光 / 粗糙度贴图 |
| 其他 | `{name}.png` | 基础颜色贴图 *(仅第 3 参数为 `"true"` 时导出)* |

> [!TIP]
> `pbrex` 会无视图层的隐藏状态, 只要名字匹配都会提取。所以平时可以在 PSD 里把 `n_` / `s_` 图层隐藏掉, 这样普通 `psdcvt` 不会导出它们, 而需要 PBR 时再用 `pbrex` 单独提取
>
> 另外在单文件模式下, `[输出路径]` 填的是**不含 `.png` 后缀的输出前缀** (比如 `out/stone`)

```lvt
import shulker.magick

# 单文件提取 (输出 out/stone.png, out/stone_n.png, out/stone_s.png)
pbrex stone.psd out/stone true

# 目录批量提取 (仅提取法线和高光)
pbrex src/textures build/cache
```

---

## mrvc - Java 资源包版本迁移

`mrvc` 同时注册为交互指令和 Levitate 方法, 用于在不同版本的 Minecraft Java 版资源包之间迁移贴图路径、模型引用、图集切片与 `pack.mcmeta` 元数据 *(纯本地转换, 不需要安装 Java)*

```lvt
mrvc <源包> <目标包> [原版本] <目标版本>
```

### 参数说明

- **`<源包>`**: 可以是完整资源包目录 (含 `pack.mcmeta`)、纯 `assets` 内容目录, 或 `.zip` 压缩包
- **`<目标包>`**: 路径以 `.zip` 结尾时直接输出压缩包, 否则输出为目录 *(目标路径不能已经存在, 也不会修改原包)*
- **`[原版本]`**: 可选的**源包格式编号**。若源包根目录有 `pack.mcmeta` 且写明了唯一格式号, 可省略；如果源包没有 `pack.mcmeta` (例如只给了一层纯 `assets` 目录), 或 `pack.mcmeta` 里写的是跨版本范围 (`supported_formats`), 就必须手动填上原版本
- **`<目标版本>`**: 必填的目标**资源包格式编号** *(注意是 `pack_format`, 不是游戏版本号, 如 `15`、`34`、`46`、`75`)*

```lvt
# 目录 -> zip (自动读取 pack.mcmeta 里的原版本)
mrvc "src/resource pack" "build/converted.zip" 46

# zip -> 目录 (手动指定原版本 3, 迁移到版本 4)
mrvc "legacy.zip" "build/modern" 3 4

# 纯 assets 目录 -> 纯 assets 目录 (无 pack.mcmeta 时保持纯 assets 结构, 需指定原版本)
mrvc "src/assets" "build/converted" 15 75
```

### 支持的版本与转换范围

目前支持格式 `1` 至 `75` *(最高对应 Java 1.21.11)*：

::: details **查看格式编号与游戏版本对照表**

| `pack_format` | Minecraft 版本 | `pack_format` | Minecraft 版本 |
| :--- | :--- | :--- | :--- |
| `1` | 1.7.2 – 1.8.9 | `18` | 1.20.2 |
| `2` | 1.9 – 1.10.2 | `22` | 1.20.3 – 1.20.4 |
| `3` | 1.11 – 1.12.2 | `32` | 1.20.5 – 1.20.6 |
| `4` | 1.13 – 1.14.4 | `34` | 1.21 – 1.21.1 |
| `5` | 1.15 – 1.16.1 | `42` | 1.21.2 – 1.21.3 |
| `6` | 1.16.2 – 1.16.5 | `46` | 1.21.4 |
| `7` | 1.17 – 1.17.1 | `55` | 1.21.5 |
| `8` | 1.18 – 1.18.2 | `63` | 1.21.6 |
| `9` | 1.19 – 1.19.2 | `64` | 1.21.7 – 1.21.8 |
| `12` | 1.19.3 | `69` | 1.21.10 |
| `13` | 1.19.4 | `75` | 1.21.11 |
| `15` | 1.20 – 1.20.1 | | |

:::

转换过程涵盖以下内容:
- 纹理、模型、blockstate、语言文件 (`.lang` <-> `.json`)、OptiFine/MCPatcher 属性、动画与 atlas 的重命名和引用更新
- 原版图集切片与重排 (指南针、箱子、粒子、画、状态效果、GUI、地图图标、月相等) 及 `pack.mcmeta` 更新

> [!NOTE]
> - 转换规则主要面向 `minecraft` 原版命名空间, Mod 资源与音频文件会原样保留
> - 高版本向低版本降级时, 部分新特性 (如拆散后的 GUI、地图图标、月相或部分新物品模型定义) 无法自动逆向重建, 遇到时会直接报错并指出具体文件
> - `pack.mcmeta` 里的 `overlays` 不会自动转换, 遇到时会保留原文件并给出警告

---

*`mrvc` 转换规则与映射数据基于 [agentdid127/ResourcePackConverter](https://github.com/agentdid127/ResourcePackConverter) (MIT 许可) 用 C# 改写, 完整许可声明位于扩展附带的 `VersionConverter/ResourcePackConverter.LICENSE.txt`*
