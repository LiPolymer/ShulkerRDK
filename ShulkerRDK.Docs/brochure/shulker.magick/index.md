# ResourceMagick - ImageMagick 图像处理集成

ResourceMagick 是 ShulkerRDK 的图像处理扩展, 基于 [Magick.NET](https://github.com/dlemstra/Magick.NET) (ImageMagick 的 .NET 封装) 构建

提供 PNG/PSD 格式互转与 PBR 贴图提取功能, 用于 Minecraft 资源包开发中的纹理处理流程

> [!WARNING]
> 该扩展在将来可能会被大幅重构, 本文档仅供参考

## 命令一览

```lvt
png2psd <路径> [输出路径]    # 将 PNG 文件(批量)转换为 PSD
```

## Levitate 方法一览

```lvt
psdcvt <路径> [输出路径]                          # 将 PSD 文件(批量)转换为 PNG
pbrex  <路径> [输出路径] [导出基础贴图]             # 从 PSD 中提取 PBR 贴图
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