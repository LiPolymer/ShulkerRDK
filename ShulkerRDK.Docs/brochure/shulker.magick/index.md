# ResourceMagick - ImageMagick 图像处理集成

ResourceMagick 是 ShulkerRDK 的图像处理扩展, 基于 [Magick.NET](https://github.com/dlemstra/Magick.NET) (ImageMagick 的 .NET 封装) 构建

提供 PNG/PSD 格式互转、PBR 贴图提取与 Java 资源包版本迁移功能。

> [!WARNING]
> 该扩展在将来可能会被大幅重构, 本文档仅供参考

## 命令一览

```lvt
png2psd <路径> [输出路径]    # 将 PNG 文件(批量)转换为 PSD
mrvc <源包> <目标包> [原版本] <目标版本> # Java 资源包版本迁移
```

## Levitate 方法一览

```lvt
psdcvt <路径> [输出路径]                          # 将 PSD 文件(批量)转换为 PNG
pbrex  <路径> [输出路径] [导出基础贴图]             # 从 PSD 中提取 PBR 贴图
mrvc <源包> <目标包> [原版本] <目标版本> # 与命令共用转换核心
```

### 快速别名

| 别名 | 展开 |
|------|------|
| `psdCvt` | `psdcvt "%project.src%" "%project.cache%"` |
| `pbrEx` | `pbrex "%project.src%" "%project.cache%"` |

> [!TIP]
> 使用别名可以快速将项目源目录中的所有资源转换到构建缓存目录, 无需手动指定路径

---

## 命令详情

### png2psd - PNG 转 PSD

将 PNG 文件(批量)转换为 PSD 格式:

```lvt
png2psd <路径> [输出路径]
```

**参数:**
- `路径` - 单个 PNG 文件路径, 或包含 PNG 文件的目录路径
- `输出路径` (可选) - 输出目录。不指定时使用项目根路径 (目录模式) 或同目录 (单文件模式)

**工作原理:**
- **目录模式**: 递归扫描所有 `*.png` 文件, 自动跳过以 `_s.png` / `_n.png` 结尾的贴图 (PBR 法线/高光贴图, 由 `pbrex` 产生), 转换为同名的 `.psd` 文件并保留目录结构
- **单文件模式**: 转换单个 PNG 为 PSD, 输出到同目录

**示例:**
```
>png2psd src/textures
正在转换[src/textures]>[project.root]
正在转换[stone.png]
正在转换[dirt.png]
完成!

>png2psd src/textures/blocks/stone.png out/
正在转换[src/textures/blocks/stone.png]>[out/stone.psd]
完成!
```

---

## Levitate 方法详情

### psdcvt - PSD 转 PNG

将 PSD 文件(批量)转换为 PNG 格式:

```lvt
psdcvt <路径> [输出路径]
```

**参数:**
- `路径` - 单个 PSD 文件路径, 或包含 PSD 文件的目录路径
- `输出路径` (可选) - 输出目录。不指定时默认输出到源目录

**工作原理:**
- **目录模式**: 递归扫描所有 `*.psd` 文件, 转换为同名的 `.png` 文件并保留目录结构
- **单文件模式**: 转换单个 PSD 为 PNG

**示例:**
```
psdcvt src/textures build/cache
```

> [!NOTE]
> 此方法在 Levitate 脚本中执行, 不可在交互式命令行直接调用。如需在命令行使用, 请参考 [Levitate 方法调用](../core/levitate.md)

---

### pbrex - PBR 贴图提取

从 PSD 文件中提取 PBR (Physically-Based Rendering) 贴图:

```lvt
pbrex <路径> [输出路径] [导出基础贴图]
```

**参数:**
- `路径` - 单个 PSD 文件路径, 或包含 PSD 文件的目录路径
- `输出路径` (可选) - 输出目录。不指定时默认输出到源目录
- `导出基础贴图` (可选) - `"true"` 或 `"false"`, 是否同时导出基础颜色贴图。默认 `false`

**PSD 图层命名规范:**

| 图层前缀 | 导出产物 | 说明 |
|----------|----------|------|
| `n_` | `{name}_n.png` | 法线贴图 (Normal Map) |
| `s_` | `{name}_s.png` | 高光/粗糙度贴图 (Specular/Roughness Map) |
| 其他 (非 n_/s_ 开头) | `{name}.png` | 基础颜色贴图 (仅在开启时导出) |

**工作原理:**
1. 打开 PSD 文件并读取所有图层 (保留透明度蒙版)
2. 按图层标签 (Layer Label) 匹配命名前缀, 将匹配的图层归类
3. 将同类图层合成到一张透明画布上, 保持图层相对位置
4. 分别导出为独立的 PNG 文件

**示例:**

PSD 结构:
```
stone.psd
├── n_normal_detail     (法线细节图层)
├── n_height            (高度图层)
├── s_roughness         (粗糙度图层)
├── s_metal             (金属度图层)
└── base_color          (基础颜色图层)
```

执行:
```
pbrex stone.psd "" true
```

输出:
```
stone.png      (base_color 图层合成)
stone_n.png    (n_normal_detail + n_height 合成)
stone_s.png    (s_roughness + s_metal 合成)
```

**目录批量处理:**
```
pbrex src/textures build/cache
```

会递归处理所有 `*.psd` 文件, 保留目录结构输出到 `build/cache`

---

## 在 Levitate 脚本中使用

```lvt
# PNG 转 PSD
png2psd src/textures

# PSD 转 PNG
psdcvt src/textures build/converted

# 提取 PBR 贴图 (仅法线和高光)
pbrex src/pbr_textures build/pbr

# 提取 PBR 贴图 (含基础颜色)
pbrex src/pbr_textures build/pbr true

# 使用快速别名
psdCvt    # 等同于 psdcvt "%project.src%" "%project.cache%"
pbrEx     # 等同于 pbrex "%project.src%" "%project.cache%"
```

---

## mrvc - Java 资源包版本迁移

命令和 Levitate 方法使用相同语法：

```lvt
mrvc <源包> <目标包> [原版本] <目标版本>
```

- `源包`：完整资源包目录、`assets` 内容目录或 `.zip` 文件；`目标包`：扩展名为 `.zip`（不区分大小写）时输出压缩包，否则输出目录，包括带其他扩展名的路径。输出形式不受输入形式影响。
- `原版本`：省略时读取 `pack.mcmeta`；显式指定时优先于元数据，冲突会警告。无元数据时必须提供原版本，不推测版本号。
- `目标版本`：必填的**资源包格式编号**，不是游戏版本；例如 `15`、`34`、`46`、`75`。`N` 与 `N.0` 等价。仅接受新上游有正式版记录的格式，不再接受 `97.1` 或将未知版本近似映射。
- 相对路径：Command 和 Method 均相对于当前项目工作目录解析，与 `copy`、`psdcvt` 一致。`%project.cache%` 不再相对于脚本目录二次拼接；带空格路径需加引号。Method 成功返回 `null`，失败抛出异常供执行器中断脚本；交互命令显示错误。
- 目标必须不存在；禁止源目标相同或互相嵌套。不覆盖输入，先写独占暂存，全部成功后发布结果。

目录输入仅检查根部的 `pack.mcmeta`；缺失时将整个输入目录视为纯 `assets` 内容（目录名称不限），不向下寻找其他 `pack.mcmeta`。此模式的目录和 ZIP 输出均保持纯 `assets` 结构，根部直接为各命名空间，不额外套 `assets/` 层，也不生成 `pack.mcmeta`；仍需显式提供原版本。完整资源包和 ZIP 输入的包根识别与输出规则不变。

```lvt
# 目录 -> ZIP，自动读取原版本
mrvc "src/resource pack" "build/converted.zip" 46

# ZIP -> 目录，显式指定原版本 3，目标版本 4
mrvc "legacy.zip" "build/modern" 3 4

# 迁移到上游覆盖的资源包格式 75
mrvc "src/pack" "build/latest.zip" 75

# 纯 assets 内容目录 -> 纯 assets 内容目录，显式指定原版本
mrvc "src/assets" "build/converted" 3 4
```

### 转换规则与覆盖范围

核心参考 [agentdid127/ResourcePackConverter](https://github.com/agentdid127/ResourcePackConverter)，固定提交 `63ba2cd40294be9436a09c22ad49fb9f1b31e1e0`，在 C# 中原生实现；运行时不需要 Java 或网络。旧 PackBridge 规则已移除。

接受的包格式为：`1`、`2`、`3`、`4`、`5`、`6`、`7`、`8`、`9`、`12`、`13`、`15`、`18`、`22`、`32`、`34`、`42`、`46`、`55`、`63`、`64`、`69`、`75`。上游源码覆盖到 Java 1.21.11（格式 `75`），不是 README 中较旧的 1.21.4。这些编号是可选择的源/目标范围，**不表示任意资源都能无损互转**。

1. 按版本边界执行路径/纹理/模型名称映射，同时处理模型纹理与 parent 引用、blockstate 引用、旧语言文件、OptiFine/MCPatcher 属性、动画和 atlas 定义。
2. 图像规则包含指南针帧、箱子、粒子、画、状态效果、物品栏、创造模式标签、标题、GUI sprites、地图图标及月相等实际切片/重排，并保留映射指定的 GUI 九宫格元数据。比如 `15 -> 34` 会拆分旧 GUI 图集和地图图标，不再只更新包格式字段。
3. 包格式不总能唯一确定游戏版本，例如格式 `4` 同时覆盖 1.13 和 1.14。能由明确遗留文件识别的规则按文件存在性执行；涉及不明确的染料名称、物品栏布局或切片条件时直接报错，不猜游戏版本。
4. 上游没有完整的反向 GUI/地图/标题/月相/装备/后处理重建规则；遇到相关跨版本资源会拒绝转换并指明受影响文件。复杂物品模型谓词或定义中未实现的语义同样明确失败，不生成伪兼容结果。
5. 上游专用规则主要作用于 `minecraft` 命名空间。Mod 命名空间和音频原样保留，不宣称能自动迁移其自定义 GUI、JSON 或资源语义。

输出包元数据保留描述和未知节点，按目标格式写旧 `pack_format` 或新 `min_format/max_format`；纯 assets 输入不生成元数据。输入与输出均拒绝危险路径、符号链接和不明确的文件冲突；在独占暂存中转换，全部成功后才发布结果，不覆盖已有目标。`overlays` 条件不会自动迁移，遇到时警告。

### 来源、许可与验证

转换代码和映射数据依据 ResourcePackConverter 的 MIT 许可改写，并保留 agentdid127 与其 Hypixel 上游的许可通知。完整通知位于 `VersionConverter/ResourcePackConverter.LICENSE.txt`，随构建/发布复制。

回归测试包括 Command/Method 路径基准、目录/ZIP 与纯 assets 结构、实际 GUI/地图像素和九宫格元数据、语言/模型/blockstate 联动，以及未知版本、歧义布局和缺失反向规则的失败行为。GUI 与地图对照样本由上述固定提交的 Java 实现实际生成，记录在测试项目 `Fixtures/rpc-15-to-34.json`；比较解码后的 RGBA，而不是 PNG 压缩字节。测试不代替 Minecraft 内渲染验证。