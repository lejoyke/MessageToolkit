using Microsoft.Extensions.DependencyInjection;
using ProjectLibrary.Communication;

namespace MessageToolkit;

public static class ServiceExtension
{
    public static void AddModbusMessageService(this IServiceCollection services)
    {
        services.AddTransient(typeof(IProtocolSerialize<>), typeof(AutoProtocolSerialize<>));
        services.AddTransient(typeof(IProtocolDataMapping<>), typeof(ProtocolDataMapping<>));
        services.AddSingleton(typeof(IMessageBuilder<>), typeof(ModbusMessageBuilder<>));
    }

    public static void AddMessageConfigurationService<TProtocol>(this IServiceCollection services, Type boolType, bool needEndianConversion= true) where TProtocol : struct
    {
        services.AddSingleton(typeof(IProtocolConfiguration<TProtocol>), provider =>
        {
            return ActivatorUtilities.CreateInstance(provider, typeof(ProtocolConfiguration<TProtocol>), boolType,needEndianConversion);
        });
    }
}
