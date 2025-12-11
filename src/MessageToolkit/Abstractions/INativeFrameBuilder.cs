using System.Linq.Expressions;
using MessageToolkit.Models;

namespace MessageToolkit.Abstractions;

/// <summary>
/// 原生协议帧构建器接口 - 用于原生类型的帧构建
/// </summary>
/// <typeparam name="TProtocol">协议结构体类型</typeparam>
/// <typeparam name="TData">原生数据类型</typeparam>
public interface INativeFrameBuilder<TProtocol, TData>
    where TProtocol : struct
{
    /// <summary>
    /// 协议模式
    /// </summary>
    IProtocolSchema<TProtocol> Schema { get; }

    /// <summary>
    /// 编解码器
    /// </summary>
    INativeProtocolCodec<TProtocol, TData> Codec { get; }

    /// <summary>
    /// 构建写入整个协议的帧
    /// </summary>
    WriteFrame<TData> BuildWriteFrame(TProtocol protocol);

    /// <summary>
    /// 构建单个字段的写入帧
    /// </summary>
    WriteFrame<TData> BuildWriteFrame(
        Expression<Func<TProtocol, TData>> fieldSelector, 
        TData value);

    /// <summary>
    /// 构建指定地址的写入帧
    /// </summary>
    WriteFrame<TData> BuildWriteFrame(ushort address, TData value);

    /// <summary>
    /// 构建多值写入帧
    /// </summary>
    WriteFrame<TData> BuildWriteFrame(ushort startAddress, TData[] values);

    /// <summary>
    /// 构建读取整个协议的请求
    /// </summary>
    ReadFrame BuildReadRequest();

    /// <summary>
    /// 构建单个字段的读取请求
    /// </summary>
    ReadFrame BuildReadRequest(Expression<Func<TProtocol, TData>> fieldSelector);

    /// <summary>
    /// 构建指定地址和数量的读取请求
    /// </summary>
    ReadFrame BuildReadRequest(ushort startAddress, ushort count);

    /// <summary>
    /// 创建数据映射（批量写入构建器）
    /// </summary>
    INativeDataMapping<TProtocol, TData> CreateDataMapping();
}
