# ShulkerFFmpeg - 音频格式转换

ShulkerFFmpeg 是 ShulkerRDK 的 FFmpeg 集成扩展, 基于 [FFMpegCore](https://www.nuget.org/packages/FFMpegCore) 构建

提供音频文件到 OGG Vorbis 格式的批量转换功能, 用于 Minecraft 资源包开发中的音频资源处理

> [!NOTE]
> FFMpegCore 是对 FFmpeg 可执行程序的 .NET 封装。**使用前需确保运行环境中已安装 FFmpeg**

## Levitate 方法一览

```lvt
a2ogg <路径> [输出路径] [编码选项]    # 将音频文件(批量)转换为 OGG
```

> [!NOTE]
> 此扩展仅提供 Levitate 方法, 无交互式命令。需在 Levitate 脚本中调用

---

## 详情

### a2ogg - 音频转 OGG

将音频文件(批量)转换为 OGG Vorbis 格式:

```lvt
a2ogg <路径> [输出路径] [编码选项]
```

**参数:**
- `路径` - 单个音频文件路径, 或包含音频文件的目录路径
- `输出路径` (可选) - 输出目录。不指定时默认输出到源目录
- `编码选项` (可选) - `"mono"` 将音频下混为单声道, 不指定则保持原始声道数

**支持的输入格式:**

| 格式 | 扩展名 |
|------|--------|
| MP3 | `.mp3` `.MP3` |
| WAV | `.wav` `.WAV` |
| FLAC | `.flac` `.FLAC` |

**工作原理:**
- **目录模式**: 递归扫描目录中所有音频文件, **并行转换**为同名的 `.ogg` 文件并保留目录结构
- **单文件模式**: 转换单个音频文件为 OGG
- **源文件处理**: 当输入路径与输出路径相同时, 转换完成后会**删除原始文件**

**示例:**

```
# 目录批量转换 (保留源文件, 输出到另一目录)
a2ogg src/sounds build/sounds

# 目录原地转换 (转换后删除源文件)
a2ogg src/sounds

# 单文件转换
a2ogg src/sounds/ambient.wav build/sounds/ambient.ogg

# 转换为单声道
a2ogg src/sounds build/sounds mono
```

**单声道模式:**

在 Minecraft 中, 部分音频 (如方块音效、脚步声) 需要使用单声道以正确计算声音衰减。传入 `"mono"` 参数会对所有音频使用 `-ac 1` 参数下混:

```
a2ogg src/sounds "" mono
```

---

## 在 Levitate 脚本中使用

```lvt
# 批量转换音效到 OGG (输出到构建缓存)
a2ogg "%project.src%/sounds" "%project.cache%/sounds"

# 转换为单声道 OGG 并删除源文件
a2ogg "%project.src%/sounds" "" mono

# 转换单个文件
a2ogg "%project.src%/sounds/music/intro.wav" "%project.cache%/sounds/music/intro.ogg"
```

---
