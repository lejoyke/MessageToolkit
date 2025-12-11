# MessageToolkit 需求规格说明书

## 1. 概述

### 1.1 核心问题

在工业通信场景中，开发者面临两类数据处理挑战：

**挑战一：字节数据的类型转换**
```
原始数据: [0x00, 0x64, 0x41, 0xC8, 0x00, 0x00, ...]
           ↓ 需要转换
协议对象: { Speed = 100, Temperature = 25.0f, Status = 0 }
```
- 如何将连续的字节流解析为有意义的字段？
- 如何处理不同数据类型（int、float、bool）的字节表示？
- 如何处理大端/小端字节序？
- 如何将抽象的地址（如寄存器地址 100）映射到具体字段？

**挑战二：原生类型的地址映射**
```
原始数据: [true, false, true, true, false, ...]  // bool[]
           ↓ 需要映射
协议对象: { Input1 = true, Input2 = false, Output1 = true, ... }
```
- 数据类型已经是目标类型（bool、byte、int 等），无需转换
- 需要将抽象地址（如 IO 点位地址 0、1、2）映射到具体字段名

### 1.2 解决方案

MessageToolkit 提供两种协议处理模式：

| 模式 | 核心功能 | 适用场景 |
|------|----------|----------|
| **字节协议 (Byte Protocol)** | 类型转换 + 地址映射 + 帧构建 | Modbus 寄存器读写 |
| **原生协议 (Native Protocol)** | 地址映射 + 帧构建 | IO 点位、原生数组 |

### 1.3 设计目标

1. **声明式协议定义** - 使用特性标记地址，自动建立映射关系
2. **类型安全** - 编译时检查，减少运行时错误
3. **零配置编解码** - 自动处理类型转换和字节序
4. **简洁的帧构建** - 通过 Lambda 表达式选择字段，自动计算地址

---

## 2. 核心概念

### 2.1 协议结构体

协议是一个 C# 结构体，通过 `[Address]` 特性将字段与通信地址绑定：

```csharp
public struct DeviceProtocol
{
    [Address(100)] public int Speed { get; set; }        // 地址 100
    [Address(104)] public float Temperature { get; set; } // 地址 104
    [Address(108)] public short Status { get; set; }      // 地址 108
}
```

协议结构体的作用：
- 定义数据的逻辑结构
- 建立 **地址 ↔ 字段名** 的映射关系
- 为编解码器提供类型信息

### 2.2 地址映射

地址映射是 MessageToolkit 的核心功能之一：

```
抽象地址空间                    具体字段
┌─────────────┐              ┌─────────────────┐
│ Address 100 │  ──────────► │ Speed (int)     │
│ Address 104 │  ──────────► │ Temperature     │
│ Address 108 │  ──────────► │ Status (short)  │
└─────────────┘              └─────────────────┘
```

**价值**：开发者使用熟悉的字段名，而不是难以记忆的地址数字。

### 2.3 两种协议模式对比

#### 字节协议 (Byte Protocol)

适用于需要类型转换的场景（如 Modbus 寄存器）：

```
编码流程:
Protocol { Speed=100, Temp=25.0f }
    ↓ 类型转换 (int → 4 bytes, float → 4 bytes)
    ↓ 字节序处理 (Big Endian)
byte[] { 0x00, 0x00, 0x00, 0x64, 0x41, 0xC8, 0x00, 0x00 }

解码流程:
byte[] { 0x00, 0x00, 0x00, 0x64, 0x41, 0xC8, 0x00, 0x00 }
    ↓ 字节序处理
    ↓ 类型转换 (4 bytes → int, 4 bytes → float)
Protocol { Speed=100, Temp=25.0f }
```

#### 原生协议 (Native Protocol)

适用于数据类型已匹配的场景（如 IO 点位）：

```
编码流程:
Protocol { Input1=true, Input2=false, Output1=true }
    ↓ 地址映射 (无类型转换)
bool[] { true, false, true }

解码流程:
bool[] { true, false, true }
    ↓ 地址映射 (无类型转换)  
Protocol { Input1=true, Input2=false, Output1=true }
```

---

## 3. 功能规格

### 3.1 字节协议功能

#### 3.1.1 类型转换

将 unmanaged 值类型与字节数组相互转换：

| 类型 | 大小 | 说明 |
|------|------|------|
| `short/ushort` | 2 字节 | 16 位整数 |
| `int/uint` | 4 字节 | 32 位整数 |
| `long/ulong` | 8 字节 | 64 位整数 |
| `float` | 4 字节 | 单精度浮点 |
| `double` | 8 字节 | 双精度浮点 |
| `bool` | 2/4 字节 | 可配置表示方式 |
| `enum` | 取决于基础类型 | 自动处理 |

#### 3.1.2 字节序处理

```csharp
// 配置字节序
var schema = new ProtocolSchema<DeviceProtocol>(
    endianness: Endianness.BigEndian  // Modbus 标准
);

// int 值 100 的编码结果:
// BigEndian:    [0x00, 0x00, 0x00, 0x64]
// LittleEndian: [0x64, 0x00, 0x00, 0x00]
```

#### 3.1.3 布尔类型表示

Modbus 协议中布尔值通常用整数表示：

```csharp
var schema = new ProtocolSchema<DeviceProtocol>(
    booleanType: BooleanRepresentation.Int16  // 2 字节
);

// true  → [0x00, 0x01]
// false → [0x00, 0x00]
```

### 3.2 原生协议功能

#### 3.2.1 直接映射

原生协议不进行类型转换，直接按地址映射：

```csharp
public struct IOProtocol
{
    [Address(0)] public bool Input1 { get; set; }
    [Address(1)] public bool Input2 { get; set; }
    [Address(2)] public bool Output1 { get; set; }
}

// 编码: Protocol → bool[]
// 解码: bool[] → Protocol
// 无类型转换，仅地址对应
```

#### 3.2.2 支持的原生类型

| 原生类型 | 使用场景 |
|----------|----------|
| `bool` | 数字 IO 点位 |
| `byte` | 字节级 IO |
| `int` | 整数寄存器组 |

### 3.3 通用功能：地址映射与帧构建

两种协议模式共享的核心功能：

#### 3.3.1 地址查询

```csharp
// 通过字段名
ushort addr1 = schema.GetAddress("Speed");

// 通过 Lambda 表达式 (类型安全)
ushort addr2 = schema.GetAddress(p => p.Speed);
```

#### 3.3.2 帧构建

**写入帧构建**：
```csharp
// 写入整个协议
var frame = builder.BuildWriteFrame(protocol);
// 结果: { StartAddress, Data[], DataLength }

// 写入单个字段
var frame = builder.BuildWriteFrame(p => p.Speed, 1500);
// 自动解析地址，编码值
```

**读取请求构建**：
```csharp
// 读取整个协议
var request = builder.BuildReadRequest();
// 结果: { StartAddress, Count }

// 读取单个字段
var request = builder.BuildReadRequest(p => p.Speed);
```

#### 3.3.3 批量数据映射

```csharp
// 批量设置多个字段，自动合并连续地址
var frames = builder.CreateDataMapping()
    .Property(p => p.Speed, 1500)
    .Property(p => p.Temperature, 25.5f)
    .Property(p => p.Status, (short)1)
    .BuildOptimized();

// 优化结果: 连续地址合并为单帧，减少通信次数
```

---

## 4. API 设计

### 4.1 核心接口

```csharp
// 协议模式 - 描述协议结构，提供地址映射
interface IProtocolSchema<TProtocol>
{
    int StartAddress { get; }
    int TotalSize { get; }
    ushort GetAddress(string fieldName);
    ushort GetAddress<TValue>(Expression<Func<TProtocol, TValue>> selector);
}

// 编解码器 - 协议与数据数组的转换
interface IProtocolCodec<TProtocol, TData>
{
    TData[] Encode(TProtocol protocol);
    TProtocol Decode(ReadOnlySpan<TData> data);
}

// 帧构建器 - 构建读写帧
interface IFrameBuilder<TProtocol, TData>
{
    IFrame<TData> BuildWriteFrame(TProtocol protocol);
    IReadFrame BuildReadRequest();
    IDataMapping<TProtocol, TData> CreateDataMapping();
}
```

### 4.2 实现类

| 类 | 用途 |
|----|------|
| `ProtocolSchema<T>` | 协议结构解析，地址映射 |
| `ByteProtocolCodec<T>` | 字节协议编解码（含类型转换） |
| `BitProtocolCodec<T, TData>` | 原生协议编解码（无类型转换） |
| `ModbusFrameBuilder<T>` | Modbus 帧构建 |
| `BitFrameBuilder<T>` | IO 位帧构建 |

---

## 5. 使用示例

### 5.1 字节协议示例（Modbus）

```csharp
// 1. 定义协议
public struct MotorProtocol
{
    [Address(100)] public int Speed { get; set; }
    [Address(104)] public float Temperature { get; set; }
    [Address(108)] public bool IsRunning { get; set; }
}

// 2. 创建构建器
var schema = new ProtocolSchema<MotorProtocol>(
    booleanType: BooleanRepresentation.Int16,
    endianness: Endianness.BigEndian
);
var builder = new ModbusFrameBuilder<MotorProtocol>(schema);

// 3. 构建写入帧
var writeFrame = builder.BuildWriteFrame(p => p.Speed, 1500);
// writeFrame.StartAddress = 100
// writeFrame.Data = [0x00, 0x00, 0x05, 0xDC]  // 1500 的大端字节

// 4. 构建读取请求
var readRequest = builder.BuildReadRequest();
// 发送读取命令，获取 byte[] 响应

// 5. 解码响应
MotorProtocol motor = builder.Codec.Decode(responseBytes);
Console.WriteLine($"Speed: {motor.Speed}, Temp: {motor.Temperature}");
```

### 5.2 原生协议示例（IO 点位）

```csharp
// 1. 定义协议
public struct IOModule
{
    [Address(0)] public bool DI0 { get; set; }
    [Address(1)] public bool DI1 { get; set; }
    [Address(8)] public bool DO0 { get; set; }
    [Address(9)] public bool DO1 { get; set; }
}

// 2. 创建构建器
var schema = new ProtocolSchema<IOModule>();
var builder = new BitFrameBuilder<IOModule>(schema);

// 3. 写入单个点位
var frame = builder.BuildWriteFrame(p => p.DO0, true);
// frame.StartAddress = 8
// frame.Data = [true]

// 4. 解码 IO 状态
bool[] ioStatus = ReadFromDevice();  // 从设备读取
IOModule module = builder.Codec.Decode(ioStatus);
Console.WriteLine($"DI0: {module.DI0}, DO0: {module.DO0}");
```

---

## 6. 约束与限制

1. **结构体约束**：协议类型必须是 `struct`
2. **类型约束**：字段必须是 `unmanaged` 类型
3. **地址显式标记**：必须使用 `[Address]` 特性标记字段
4. **通信层分离**：本库仅负责帧构建和编解码，不包含实际通信

---

## 7. 术语表

| 术语 | 定义 |
|------|------|
| 地址映射 | 将抽象地址（数字）与具体字段名建立对应关系 |
| 类型转换 | 将值类型（int、float 等）与字节数组相互转换 |
| 帧 | 一个完整的通信数据单元，包含地址和数据 |
| 字节序 | 多字节数据的存储顺序（大端/小端） |

---

## 版本历史

| 版本 | 日期 | 说明 |
|------|------|------|
| 1.0 | 2025-12 | 初始版本 |
