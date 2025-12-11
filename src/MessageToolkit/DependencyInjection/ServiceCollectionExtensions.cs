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
    /// 添加 Modbus 字节协议支持
    /// </summary>
    public static IServiceCollection AddModbusProtocol<TProtocol>(
        this IServiceCollection services,
        BooleanRepresentation booleanType = BooleanRepresentation.Int16,
        Endianness endianness = Endianness.BigEndian) where TProtocol : struct
    {
        services.AddSingleton<IProtocolSchema<TProtocol>>(_ => new ProtocolSchema<TProtocol>(booleanType, endianness));

        services.AddTransient<ModbusProtocolCodec<TProtocol>>();
        services.AddTransient<ModbusFrameBuilder<TProtocol>>();

        // 注册接口映射
        services.AddTransient<IModbusProtocolCodec<TProtocol>>(sp => sp.GetRequiredService<ModbusProtocolCodec<TProtocol>>());
        services.AddTransient<IModbusFrameBuilder<TProtocol>>(sp => sp.GetRequiredService<ModbusFrameBuilder<TProtocol>>());

        return services;
    }

    /// <summary>
    /// 添加原生协议支持（IO 点位等）
    /// </summary>
    public static IServiceCollection AddNativeProtocol<TProtocol, TData>(this IServiceCollection services) where TProtocol : struct
    {
        services.AddSingleton<IProtocolSchema<TProtocol>>(_ => new ProtocolSchema<TProtocol>(BooleanRepresentation.Boolean));

        services.AddTransient<NativeProtocolCodec<TProtocol, TData>>();
        services.AddTransient<NativeFrameBuilder<TProtocol, TData>>();

        // 注册接口映射
        services.AddTransient<INativeProtocolCodec<TProtocol, TData>>(sp => sp.GetRequiredService<NativeProtocolCodec<TProtocol, TData>>());
        services.AddTransient<INativeFrameBuilder<TProtocol, TData>>(sp => sp.GetRequiredService<NativeFrameBuilder<TProtocol, TData>>());

        return services;
    }
}

