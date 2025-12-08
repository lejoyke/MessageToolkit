using Microsoft.Extensions.DependencyInjection;
using ProjectLibrary.Communication;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MessageToolkit;

public sealed class ModbusMessageBuilder<TProtocol>(IServiceProvider serviceProvider)
    : IMessageBuilder<TProtocol> where TProtocol : struct
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public FluentModbusClient? ModbusClient { get; set; }

    public IProtocolConfiguration<TProtocol> Configuration { get; } =
        serviceProvider.GetRequiredService<IProtocolConfiguration<TProtocol>>();

    public IProtocolSerialize<TProtocol> ProtocolSerialize { get; } =
        serviceProvider.GetRequiredService<IProtocolSerialize<TProtocol>>();

    [MemberNotNull(nameof(ModbusClient))]
    private void CheckModbusClient()
    {
        if (ModbusClient == null)
        {
            throw new InvalidOperationException("ModbusClient is not set");
        }

        if (!ModbusClient.IsConnected)
        {
            throw new InvalidOperationException("ModbusClient is not connected");
        }
    }

    public IProtocolDataMapping<TProtocol> CreateWriteMapping()
    {
        return _serviceProvider.GetRequiredService<IProtocolDataMapping<TProtocol>>();
    }

    public void Commit(Dictionary<ushort, byte[]> datas)
    {
        foreach (var kvp in datas)
        {
            ModbusClient!.WriteMultipleRegisters(1, (ushort)(kvp.Key / 2), kvp.Value);
        }
    }

    public async Task CommitAsync(Dictionary<ushort, byte[]> datas, CancellationToken cancellationToken = default)
    {
        foreach (var kvp in datas)
        {
            await ModbusClient!.WriteMultipleRegistersAsync(1, (ushort)(kvp.Key / 2), kvp.Value, cancellationToken);
        }
    }

    public TValue ReadProtocol<TValue>(Expression<Func<TProtocol, TValue>> propertyExpression)
        where TValue : unmanaged
    {
        CheckModbusClient();

        if (propertyExpression.Body is not MemberExpression memberExpression)
        {
            throw new ArgumentException("Invalid attribute expression");
        }

        string propertyName = memberExpression.Member.Name;
        if (!Configuration.AddressMapping.TryGetValue(propertyName, out ushort address))
        {
            throw new ArgumentException($"Address not found for property {propertyName}");
        }

        return ReadProtocol<TValue>(address);
    }

    public Task<TValue> ReadProtocolAsync<TValue>(Expression<Func<TProtocol, TValue>> propertyExpression, CancellationToken cancellationToken = default)
        where TValue : unmanaged
    {
        CheckModbusClient();

        if (propertyExpression.Body is not MemberExpression memberExpression)
        {
            throw new ArgumentException("Invalid attribute expression");
        }

        string propertyName = memberExpression.Member.Name;
        if (!Configuration.AddressMapping.TryGetValue(propertyName, out ushort address))
        {
            throw new ArgumentException($"Address not found for property {propertyName}");
        }

        return ReadProtocolAsync<TValue>(address, cancellationToken);
    }

    public TValue ReadProtocol<TValue>(ushort address)
        where TValue : unmanaged
    {
        int size = GetTypeSize<TValue>() / 2;

        var bytesData = ModbusClient!.ReadHoldingRegisters(1, (ushort)(address / 2), (ushort)size);
        return ProtocolSerialize.GetValueFromBytes<TValue>(bytesData);
    }

    public async Task<TValue> ReadProtocolAsync<TValue>(ushort address, CancellationToken cancellationToken = default)
        where TValue : unmanaged
    {
        int size = GetTypeSize<TValue>() / 2;

        var bytesData = await ModbusClient!.ReadHoldingRegistersAsync(1, (ushort)(address / 2), (ushort)size, cancellationToken);
        return ProtocolSerialize.GetValueFromBytes<TValue>(bytesData);
    }

    public TProtocol ReadProtocol()
    {
        CheckModbusClient();

        int sizeInWords = Configuration.Size / 2;
        var bytes = ModbusClient.ReadHoldingRegisters(1, (ushort)(Configuration.StartAddress / 2), (ushort)sizeInWords);

        return ProtocolSerialize.Deserialize(bytes);
    }

    public async Task<TProtocol> ReadProtocolAsync(CancellationToken cancellationToken = default)
    {
        CheckModbusClient();

        int sizeInWords = Configuration.Size / 2;
        var bytes = await ModbusClient.ReadHoldingRegistersAsync(1, (ushort)(Configuration.StartAddress / 2), (ushort)sizeInWords, cancellationToken);

        return ProtocolSerialize.Deserialize(bytes);
    }

    public void WriteProtocol<TValue>(Expression<Func<TProtocol, TValue>> propertyExpression, TValue value) where TValue : unmanaged
    {
        CheckModbusClient();
        if (propertyExpression.Body is not MemberExpression memberExpression)
        {
            throw new ArgumentException("Invalid attribute expression");
        }

        string propertyName = memberExpression.Member.Name;
        if (!Configuration.AddressMapping.TryGetValue(propertyName, out ushort address))
        {
            throw new ArgumentException($"Address not found for property {propertyName}");
        }

        WriteProtocol(address, value);
    }

    public async Task WriteProtocolAsync<TValue>(Expression<Func<TProtocol, TValue>> propertyExpression, TValue value, CancellationToken cancellationToken = default) where TValue : unmanaged
    {
        CheckModbusClient();
        if (propertyExpression.Body is not MemberExpression memberExpression)
        {
            throw new ArgumentException("Invalid attribute expression");
        }

        string propertyName = memberExpression.Member.Name;
        if (!Configuration.AddressMapping.TryGetValue(propertyName, out ushort address))
        {
            throw new ArgumentException($"Address not found for property {propertyName}");
        }

        await WriteProtocolAsync(address, value, cancellationToken);
    }

    public void WriteProtocol<TValue>(ushort address, TValue value)
        where TValue : unmanaged
    {
        var bytes = ProtocolSerialize.GetBytes(value);
        ModbusClient!.WriteMultipleRegisters(1, (ushort)(address / 2), bytes);
    }

    public async Task WriteProtocolAsync<TValue>(ushort address, TValue value, CancellationToken cancellationToken = default)
        where TValue : unmanaged
    {
        var bytes = ProtocolSerialize.GetBytes(value);
        await ModbusClient!.WriteMultipleRegistersAsync(1, (ushort)(address / 2), bytes, cancellationToken);
    }

    public void WriteProtocol(TProtocol protocol)
    {
        CheckModbusClient();

        var bytes = ProtocolSerialize.Serialize(protocol);
        ModbusClient.WriteMultipleRegisters(1, (ushort)(Configuration.StartAddress / 2), bytes);
    }

    public async Task WriteProtocolAsync(TProtocol protocol, CancellationToken cancellationToken = default)
    {
        CheckModbusClient();

        var bytes = ProtocolSerialize.Serialize(protocol);
        await ModbusClient.WriteMultipleRegistersAsync(1, (ushort)(Configuration.StartAddress / 2), bytes, cancellationToken);
    }

    private int GetTypeSize<TValue>() where TValue : unmanaged
    {
        if (typeof(TValue) == typeof(bool))
        {
            return Configuration.BooleanTypeFlag ? 4 : 2;
        }

        return Marshal.SizeOf<TValue>();
    }
}