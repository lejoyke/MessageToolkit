using MessageToolkit.Abstractions;

namespace MessageToolkit;

public sealed class PropertyValueSetter<TProtocol, TData>(
    INativeDataMapping<TProtocol,TData> writeMapping,
    ushort address) where TProtocol : struct
{
    private readonly INativeDataMapping<TProtocol, TData> _writeMapping = writeMapping;
    private readonly ushort _address = address;

    public INativeDataMapping<TProtocol, TData> Value(TData value)
    {
        _writeMapping.Write(_address, value);
        return _writeMapping;
    }
}
