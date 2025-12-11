# MessageToolkit 3.0.0

协议帧构建与编解码库，支持 **Modbus 字节协议** 和 **Native 原生类型协议**。聚焦协议层，零通信耦合，可与任意通信客户端搭配使用。

## 主要特性

- **双协议支持**：Modbus（字节转换）和 Native（原生类型直接映射）
- **零通信耦合**：只负责协议序列化、反序列化与帧构建
- **声明式映射**：通过 `[Address]` 特性标注地址，自动计算起始地址与总长度
- **灵活配置**：支持布尔以 `Int16`/`Int32`/`Boolean` 表示，可配置大小端
- **批量优化**：批量写入自动合并连续地址，减少通信次数
- **工厂模式**：`FrameBuilderFactory` 快速创建构建器
- **依赖注入**：提供 DI 扩展，快速集成

## 安装

```bash
dotnet add package MessageToolkit --version 3.0.0
```

## 协议类型对比

| 特性 | Modbus 字节协议 | Native 原生协议 |
|------|----------------|-----------------|
| 数据转换 | 值类型 ↔ byte[] | 无转换，直接映射 |
| 适用场景 | Modbus 寄存器读写 | PLC IO 点位、原生数组 |
| 数据类型 | int, float, bool → bytes | bool[], int[] 等原生类型 |
| 编解码器 | `IModbusProtocolCodec` | `INativeProtocolCodec` |
| 帧构建器 | `IModbusFrameBuilder` | `INativeFrameBuilder` |

---

## 快速开始

### 1. Modbus 字节协议

适用于 Modbus 寄存器读写场景，自动处理类型转换和字节序。

#### 定义协议结构体

```csharp
using MessageToolkit.Attributes;

public struct DeviceProtocol
{
    [Address(100)] public int Speed { get; set; }        // 4 bytes
    [Address(104)] public float Temperature { get; set; } // 4 bytes
    [Address(108)] public bool IsRunning { get; set; }    // 2 bytes (Int16)
    [Address(110)] public short Status { get; set; }      // 2 bytes
}
```

#### 使用工厂创建构建器

```csharp
using MessageToolkit;
using MessageToolkit.Models;

// 使用默认配置（Int16 布尔表示、大端字节序）
var builder = FrameBuilderFactory.CreateModbus<DeviceProtocol>();

// 或自定义配置
var builder = FrameBuilderFactory.CreateModbus<DeviceProtocol>(
    BooleanRepresentation.Int16, 
    Endianness.BigEndian);
```

#### 构建写入帧

```csharp
// 写入整个协议
var protocol = new DeviceProtocol 
{ 
    Speed = 1000, 
    Temperature = 25.5f, 
    IsRunning = true, 
    Status = 1 
};
var writeFrame = builder.BuildWriteFrame(protocol);
// writeFrame.StartAddress = 100, writeFrame.Data = byte[12]

// 写入单个字段
var speedFrame = builder.BuildWriteFrame(p => p.Speed, 2000);

// 写入指定地址
var addrFrame = builder.BuildWriteFrame<int>(100, 3000);
```

#### 构建读取请求

```csharp
// 读取整个协议
var readRequest = builder.BuildReadRequest();
// readRequest.StartAddress = 100, readRequest.RegisterCount = 6

// 读取单个字段
var fieldRequest = builder.BuildReadRequest(p => p.Temperature);
```

#### 批量写入（自动合并连续地址）

```csharp
var frames = builder.CreateDataMapping()
    .Property(p => p.Speed, 1500)
    .Property(p => p.Temperature, 30.0f)
    .Property(p => p.IsRunning, false)
    .Property(p => p.Status, (short)2)
    .BuildOptimized()  // 合并连续地址
    .ToArray();

// 结果：1 个帧（地址连续，自动合并）
```

#### 编解码

```csharp
// 编码：结构体 → 字节数组
byte[] data = builder.Codec.Encode(protocol);

// 解码：字节数组 → 结构体
DeviceProtocol decoded = builder.Codec.Decode(receivedBytes);

// 单值编解码
byte[] speedBytes = builder.Codec.EncodeValue(1000);
int speed = builder.Codec.DecodeValue<int>(speedBytes);
```

---

### 2. Native 原生协议

适用于 PLC IO 点位等场景，数据类型与协议字段类型一致，无需类型转换。

#### 定义 IO 协议

```csharp
public struct IOProtocol
{
    [Address(0)] public bool DI0 { get; set; }  // 数字输入 0
    [Address(1)] public bool DI1 { get; set; }  // 数字输入 1
    [Address(8)] public bool DO0 { get; set; }  // 数字输出 0
    [Address(9)] public bool DO1 { get; set; }  // 数字输出 1
}
```

#### 使用工厂创建构建器

```csharp
// 创建 bool 类型的 Native 构建器
var builder = FrameBuilderFactory.CreateNative<IOProtocol, bool>();

// 或自定义配置
var builder = FrameBuilderFactory.CreateNative<IOProtocol, bool>(
    BooleanRepresentation.Boolean,
    Endianness.BigEndian);
```

#### 构建写入帧

```csharp
// 写入整个协议
var io = new IOProtocol { DI0 = true, DI1 = false, DO0 = true, DO1 = false };
var writeFrame = builder.BuildWriteFrame(io);
// writeFrame.StartAddress = 0, writeFrame.Data = bool[10]

// 写入单个字段
var doFrame = builder.BuildWriteFrame(p => p.DO0, true);

// 写入多个值
var multiFrame = builder.BuildWriteFrame(0, new[] { true, false, true });
```

#### 编解码

```csharp
// 编码：结构体 → 原生数组
bool[] data = builder.Codec.Encode(io);

// 解码：原生数组 → 结构体
IOProtocol decoded = builder.Codec.Decode(receivedBools);
```

---

## 依赖注入

```csharp
using MessageToolkit.DependencyInjection;

// 注册 Modbus 协议
services.AddModbusProtocol<DeviceProtocol>(
    BooleanRepresentation.Int16,
    Endianness.BigEndian);

// 注册 Native 协议
services.AddNativeProtocol<IOProtocol, bool>(
    BooleanRepresentation.Boolean,
    Endianness.BigEndian);
```
```csharp
// 使用
public class MyService
{
    private readonly IModbusFrameBuilder<DeviceProtocol> _modbusBuilder;
    private readonly INativeFrameBuilder<IOProtocol, bool> _nativeBuilder;

    public MyService(
        IModbusFrameBuilder<DeviceProtocol> modbusBuilder,
        INativeFrameBuilder<IOProtocol, bool> nativeBuilder)
    {
        _modbusBuilder = modbusBuilder;
        _nativeBuilder = nativeBuilder;
    }
}
```

---

## 与通信层集成示例

```csharp
// Modbus 写入
var frame = builder.BuildWriteFrame(protocol);
await modbusClient.WriteMultipleRegistersAsync(
    unitId: 1, 
    startAddress: (ushort)frame.StartAddress, 
    data: frame.ToArray());

// Modbus 读取
var request = builder.BuildReadRequest();
var rawBytes = await modbusClient.ReadHoldingRegistersAsync(
    unitId: 1,
    startAddress: (ushort)request.StartAddress,
    count: request.RegisterCount);
var decoded = builder.Codec.Decode(rawBytes);
```

---

## API 速览

### 核心接口

| 接口 | 说明 |
|------|------|
| `IProtocolSchema<T>` | 协议模式（字段信息、地址映射） |
| `IModbusProtocolCodec<T>` | Modbus 编解码器 |
| `INativeProtocolCodec<T, TData>` | Native 编解码器 |
| `IModbusFrameBuilder<T>` | Modbus 帧构建器 |
| `INativeFrameBuilder<T, TData>` | Native 帧构建器 |
| `IModbusDataMapping<T>` | Modbus 批量写入构建 |
| `INativeDataMapping<T, TData>` | Native 批量写入构建 |

### 模型类

| 类型 | 说明 |
|------|------|
| `ModbusWriteFrame` | Modbus 写入帧 |
| `ModbusReadRequest` | Modbus 读取请求 |
| `WriteFrame<TData>` | 泛型写入帧 |
| `ReadFrame` | 读取请求 |
| `BooleanRepresentation` | 布尔表示方式 |
| `Endianness` | 字节序 |

### 工厂方法

```csharp
// Modbus
FrameBuilderFactory.CreateModbus<TProtocol>();
FrameBuilderFactory.CreateModbus<TProtocol>(booleanType, endianness);
FrameBuilderFactory.CreateModbus<TProtocol>(schema);

// Native
FrameBuilderFactory.CreateNative<TProtocol, TData>();
FrameBuilderFactory.CreateNative<TProtocol, TData>(booleanType, endianness);
FrameBuilderFactory.CreateNative<TProtocol, TData>(schema);
```

---

## 设计要点

- **地址驱动**：按 `[Address]` 特性序列化，字段声明顺序无关
- **布尔表示**：`Boolean`（1元素）、`Int16`（2字节）、`Int32`（4字节）
- **协议分离**：Modbus 和 Native 接口完全独立，职责清晰
- **零通信耦合**：仅输出帧数据，实际通信由上层处理
- **地址合并**：`BuildOptimized()` 自动合并连续地址，减少通信次数

---

## 开发与测试

```bash
dotnet build
dotnet test
```

## 许可

MIT

