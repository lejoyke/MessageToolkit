using MessageToolkit.Abstractions;
using MessageToolkit.Models;

namespace MessageToolkit;

/// <summary>
/// 帧构建器工厂 - 提供便捷的构建器创建方法
/// </summary>
public static class FrameBuilderFactory
{
    #region Modbus 协议

    /// <summary>
    /// 创建 Modbus 帧构建器（使用默认配置：Int16 布尔表示、大端字节序）
    /// </summary>
    /// <typeparam name="TProtocol">协议结构体类型</typeparam>
    /// <returns>Modbus 帧构建器</returns>
    public static IModbusFrameBuilder<TProtocol> CreateModbus<TProtocol>()
        where TProtocol : struct
    {
        return CreateModbus<TProtocol>(BooleanRepresentation.Int16, Endianness.BigEndian);
    }

    /// <summary>
    /// 创建 Modbus 帧构建器
    /// </summary>
    /// <typeparam name="TProtocol">协议结构体类型</typeparam>
    /// <param name="booleanType">布尔类型表示方式</param>
    /// <param name="endianness">字节序</param>
    /// <returns>Modbus 帧构建器</returns>
    public static IModbusFrameBuilder<TProtocol> CreateModbus<TProtocol>(
        BooleanRepresentation booleanType,
        Endianness endianness = Endianness.BigEndian)
        where TProtocol : struct
    {
        var schema = new ProtocolSchema<TProtocol>(booleanType, endianness);
        return new ModbusFrameBuilder<TProtocol>(schema);
    }

    /// <summary>
    /// 创建 Modbus 帧构建器（使用已有的 Schema）
    /// </summary>
    /// <typeparam name="TProtocol">协议结构体类型</typeparam>
    /// <param name="schema">协议模式</param>
    /// <returns>Modbus 帧构建器</returns>
    public static IModbusFrameBuilder<TProtocol> CreateModbus<TProtocol>(
        IProtocolSchema<TProtocol> schema)
        where TProtocol : struct
    {
        return new ModbusFrameBuilder<TProtocol>(schema);
    }

    #endregion

    #region Native 协议

    /// <summary>
    /// 创建 Native 帧构建器（使用默认配置：Boolean 布尔表示、大端字节序）
    /// </summary>
    /// <typeparam name="TProtocol">协议结构体类型</typeparam>
    /// <typeparam name="TData">原生数据类型</typeparam>
    /// <returns>Native 帧构建器</returns>
    public static INativeFrameBuilder<TProtocol, TData> CreateNative<TProtocol, TData>()
        where TProtocol : struct
    {
        return CreateNative<TProtocol, TData>(BooleanRepresentation.Boolean, Endianness.BigEndian);
    }

    /// <summary>
    /// 创建 Native 帧构建器
    /// </summary>
    /// <typeparam name="TProtocol">协议结构体类型</typeparam>
    /// <typeparam name="TData">原生数据类型</typeparam>
    /// <param name="booleanType">布尔类型表示方式</param>
    /// <param name="endianness">字节序</param>
    /// <returns>Native 帧构建器</returns>
    public static INativeFrameBuilder<TProtocol, TData> CreateNative<TProtocol, TData>(
        BooleanRepresentation booleanType,
        Endianness endianness = Endianness.BigEndian)
        where TProtocol : struct
    {
        var schema = new ProtocolSchema<TProtocol>(booleanType, endianness);
        return new NativeFrameBuilder<TProtocol, TData>(schema);
    }

    /// <summary>
    /// 创建 Native 帧构建器（使用已有的 Schema）
    /// </summary>
    /// <typeparam name="TProtocol">协议结构体类型</typeparam>
    /// <typeparam name="TData">原生数据类型</typeparam>
    /// <param name="schema">协议模式</param>
    /// <returns>Native 帧构建器</returns>
    public static INativeFrameBuilder<TProtocol, TData> CreateNative<TProtocol, TData>(
        IProtocolSchema<TProtocol> schema)
        where TProtocol : struct
    {
        return new NativeFrameBuilder<TProtocol, TData>(schema);
    }

    #endregion
}
