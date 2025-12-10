using System.Buffers;

namespace MessageToolkit.Models;

/// <summary>
/// Modbus 写入帧 - 包含要写入的数据
/// </summary>
public readonly struct ModbusWriteFrame
{
    /// <summary>
    /// 起始地址（字节地址）
    /// </summary>
    public ushort StartAddress { get; }

    /// <summary>
    /// 寄存器地址（StartAddress / 2）
    /// </summary>
    public ushort RegisterAddress => (ushort)(StartAddress / 2);

    /// <summary>
    /// 数据载荷
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; }

    /// <summary>
    /// 数据长度（字节）
    /// </summary>
    public int DataLength => Data.Length;

    /// <summary>
    /// 寄存器数量
    /// </summary>
    public ushort RegisterCount => (ushort)(Data.Length / 2);

    /// <summary>
    /// 创建写入帧（使用已有数据）
    /// </summary>
    public ModbusWriteFrame(ushort startAddress, byte[] data)
    {
        StartAddress = startAddress;
        Data = data;
    }

    /// <summary>
    /// 创建写入帧
    /// </summary>
    public ModbusWriteFrame(ushort startAddress, ReadOnlyMemory<byte> data)
    {
        StartAddress = startAddress;
        Data = data;
    }

    /// <summary>
    /// 获取数据的副本
    /// </summary>
    public byte[] ToArray() => Data.ToArray();
}

/// <summary>
/// Modbus 读取请求 - 只包含地址和数量信息
/// </summary>
public readonly struct ModbusReadRequest
{

    /// <summary>
    /// 起始地址（字节地址）
    /// </summary>
    public ushort StartAddress { get; }

    /// <summary>
    /// 寄存器地址（StartAddress / 2）
    /// </summary>
    public ushort RegisterAddress => (ushort)(StartAddress / 2);

    /// <summary>
    /// 要读取的寄存器数量
    /// </summary>
    public ushort RegisterCount { get; }

    /// <summary>
    /// 要读取的字节数
    /// </summary>
    public int ByteCount => RegisterCount * 2;

    /// <summary>
    /// 创建读取请求
    /// </summary>
    public ModbusReadRequest(ushort startAddress, ushort registerCount)
    {
        StartAddress = startAddress;
        RegisterCount = registerCount;
    }

    /// <summary>
    /// 创建读取保持寄存器请求
    /// </summary>
    public static ModbusReadRequest ReadHoldingRegisters(ushort startAddress, ushort registerCount)
        => new(startAddress, registerCount);

    /// <summary>
    /// 创建读取输入寄存器请求
    /// </summary>
    public static ModbusReadRequest ReadInputRegisters(ushort startAddress, ushort registerCount)
        => new(startAddress, registerCount);
}
