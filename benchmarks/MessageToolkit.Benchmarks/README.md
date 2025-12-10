# MessageToolkit 性能基准测试

这是一个用于测试 MessageToolkit 库性能优化效果的 BenchmarkDotNet 项目。

## 快速开始

### 运行基准测试

```bash
cd MessageToolkit/benchmarks/MessageToolkit.Benchmarks
dotnet run -c Release
```

### 查看结果

测试完成后，结果会保存在 `BenchmarkDotNet.Artifacts/results/` 目录下：

- **HTML 报告**: `MessageToolkit.Benchmarks.ProtocolCodecBenchmarks-report.html` (推荐查看)
- **CSV 数据**: `MessageToolkit.Benchmarks.ProtocolCodecBenchmarks-report.csv`
- **Markdown 报告**: `MessageToolkit.Benchmarks.ProtocolCodecBenchmarks-report-github.md`

## 测试内容

### 对比版本

- **优化前版本** (`OldProtocolCodec`): 使用反射直接访问属性
- **优化后版本** (`ProtocolCodec`): 使用编译的表达式树委托

### 测试场景

1. **编码完整协议** - 将协议结构编码为字节数组
2. **解码完整协议** - 从字节数组解码为协议结构
3. **编码单值** - 编码单个基础类型值
4. **解码单值** - 解码单个基础类型值
5. **提取布尔值** - 从协议中提取所有布尔字段

### 测试协议

使用包含 8 个字段的测试协议：
- 2 个 `int` 字段
- 2 个 `float` 字段
- 2 个 `bool` 字段
- 1 个 `enum` 字段
- 1 个 `short` 字段

总计 24 字节的协议数据。

## 性能结果概览

### 📊 测试报告

- **[8 字段协议测试](./BENCHMARK_RESULTS.md)** - 基础性能对比
- **[50 字段协议测试](./BENCHMARK_RESULTS_50_FIELDS.md)** - 大规模协议性能验证
- **[8 vs 50 字段对比分析](./COMPARISON_8_VS_50_FIELDS.md)** - 深度对比分析

### 🎯 核心发现

**8 字段协议 (24 字节):**
- ✅ 编码性能提升 **68.6%**
- ✅ 提取布尔值性能提升 **46.6%**
- ✅ 内存分配优化（编码减少 32.4%）
- ⚠️ 解码性能有所下降（-25.1%）

**50 字段协议 (140 字节):**
- ⭐ 编码性能提升 **90.6%** (接近 2 倍速度)
- ⭐ 提取布尔值性能提升 **69.1%**
- ⭐ 内存分配优化（编码减少 68.9%）
- ⚠️ 解码性能有所下降（-26.7%）

**结论**: 字段越多，优化效果越显著！

## 项目结构

```
MessageToolkit.Benchmarks/
├── Program.cs                      # 入口程序
├── ProtocolCodecBenchmarks.cs      # 基准测试定义
├── OldProtocolCodec.cs             # 优化前的实现
├── README.md                       # 本文件
└── BENCHMARK_RESULTS.md            # 详细性能报告
```

## 自定义测试

如果想测试自己的协议，可以修改 `ProtocolCodecBenchmarks.cs` 中的 `TestProtocol` 结构。

## 要求

- .NET 8.0 SDK
- BenchmarkDotNet 0.14.0

## 注意事项

- 始终使用 **Release** 配置运行基准测试
- 关闭其他占用 CPU 的应用程序
- 首次运行可能需要一些时间进行预热
- 完整测试需要约 7-8 分钟

