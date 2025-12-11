using MessageToolkit.Abstractions;
using MessageToolkit.Models;
using Microsoft.Extensions.DependencyInjection;

namespace MessageToolkit.DependencyInjection;

/// <summary>
/// MessageToolkit 服务注册扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加字节协议支持（Modbus 寄存器读写）
    /// </summary>
    /// <remarks>
    /// 字节协议提供完整的类型转换功能：
    /// - 值类型 ↔ 字节数组转换
    /// - 字节序处理
    /// - 布尔值表示配置
    /// </remarks>
    /// <typeparam name="TProtocol">协议结构体类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="booleanType">布尔类型表示方式</param>
    /// <param name="endianness">字节序</param>
    public static IServiceCollection AddByteProtocol<TProtocol>(
        this IServiceCollection services,
        BooleanRepresentation booleanType = BooleanRepresentation.Int16,
        Endianness endianness = Endianness.BigEndian) where TProtocol : struct
    {
        services.AddSingleton<IProtocolSchema<TProtocol>>(
            _ => new ProtocolSchema<TProtocol>(booleanType, endianness));

        services.AddTransient<ModbusProtocolCodec<TProtocol>>();
        services.AddTransient<ModbusFrameBuilder<TProtocol>>();

        services.AddTransient<IProtocolCodec<TProtocol, byte>>(
            sp => sp.GetRequiredService<ModbusProtocolCodec<TProtocol>>());
        services.AddTransient<IFrameBuilder<TProtocol, byte>>(
            sp => sp.GetRequiredService<ModbusFrameBuilder<TProtocol>>());

        return services;
    }

    /// <summary>
    /// 添加原生协议支持（IO 点位等）
    /// </summary>
    /// <remarks>
    /// 原生协议仅提供地址映射功能，不进行类型转换。
    /// 适用于数据类型已匹配的场景（如 bool[] IO 点位）。
    /// </remarks>
    /// <typeparam name="TProtocol">协议结构体类型</typeparam>
    /// <typeparam name="TData">原生数据类型（bool、byte、int 等）</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="booleanType">布尔类型表示方式（仅用于计算字段大小）</param>
    /// <param name="endianness">字节序（仅用于 Schema 配置）</param>
    public static IServiceCollection AddNativeProtocol<TProtocol, TData>(
        this IServiceCollection services,
        BooleanRepresentation booleanType = BooleanRepresentation.Boolean,
        Endianness endianness = Endianness.BigEndian) where TProtocol : struct
    {
        services.AddSingleton<IProtocolSchema<TProtocol>>(
            _ => new ProtocolSchema<TProtocol>(booleanType, endianness));

        services.AddTransient<NativeProtocolCodec<TProtocol, TData>>();
        services.AddTransient<NativeFrameBuilder<TProtocol, TData>>();

        services.AddTransient<IProtocolCodec<TProtocol, TData>>(
            sp => sp.GetRequiredService<NativeProtocolCodec<TProtocol, TData>>());
        services.AddTransient<IFrameBuilder<TProtocol, TData>>(
            sp => sp.GetRequiredService<NativeFrameBuilder<TProtocol, TData>>());

        return services;
    }
}

