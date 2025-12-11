using System.Linq.Expressions;
using MessageToolkit.Models;

namespace MessageToolkit.Abstractions;

/// <summary>
/// Modbus 帧构建器接口 - 用于字节协议的帧构建
/// </summary>
/// <typeparam name="TProtocol">协议结构体类型</typeparam>
public interface IModbusFrameBuilder<TProtocol>
    where TProtocol : struct
{
    /// <summary>
    /// 协议模式
    /// </summary>
    IProtocolSchema<TProtocol> Schema { get; }

    /// <summary>
    /// 编解码器
    /// </summary>
    IModbusProtocolCodec<TProtocol> Codec { get; }

    /// <summary>
    /// 构建写入整个协议的帧
    /// </summary>
    ModbusWriteFrame BuildWriteFrame(TProtocol protocol);

    /// <summary>
    /// 构建单个字段的写入帧
    /// </summary>
    ModbusWriteFrame BuildWriteFrame<TValue>(Expression<Func<TProtocol, TValue>> fieldSelector, TValue value) where TValue : unmanaged;

    /// <summary>
    /// 构建指定地址的写入帧
    /// </summary>
    ModbusWriteFrame BuildWriteFrame<TValue>(ushort address, TValue value) where TValue : unmanaged;

    /// <summary>
    /// 构建读取整个协议的请求
    /// </summary>
    ModbusReadRequest BuildReadRequest();

    /// <summary>
    /// 构建单个字段的读取请求
    /// </summary>
    ModbusReadRequest BuildReadRequest<TValue>(Expression<Func<TProtocol, TValue>> fieldSelector) where TValue : unmanaged;

    /// <summary>
    /// 构建指定地址和数量的读取请求
    /// </summary>
    ModbusReadRequest BuildReadRequest(ushort startAddress, ushort registerCount);

    /// <summary>
    /// 创建数据映射（批量写入构建器）
    /// </summary>
    IModbusDataMapping<TProtocol> CreateDataMapping();
}
