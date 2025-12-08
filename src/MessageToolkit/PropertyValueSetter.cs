namespace MessageToolkit;

public sealed class PropertyValueSetter<TProtocol, TValue>(
    IProtocolDataMapping<TProtocol> writeMapping,
    IProtocolConfiguration<TProtocol> configuration,
    ushort address) where TProtocol : struct
{
    private readonly IProtocolDataMapping<TProtocol> _writeMapping = writeMapping;
    private readonly IProtocolConfiguration<TProtocol> _configuration = configuration;
    private readonly ushort _address = address;

    public IProtocolDataMapping<TProtocol> Value(TValue value)
    {
        byte[] datas = value switch
        {
            bool boolValue => ConvertBooleanValue(boolValue),
            _ => ValueConverterHelper.Value(value, _configuration.NeedEndianConversion)
        };

        _writeMapping.AddData(_address, datas);
        return _writeMapping;
    }

    public IProtocolDataMapping<TProtocol> Value(byte[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        _writeMapping.AddData(_address, values);
        return _writeMapping;
    }

    private byte[] ConvertBooleanValue(bool value)
    {
        if (_configuration.BooleanTypeFlag)
        {
            return ValueConverterHelper.Value(value ? 1 : 0, _configuration.NeedEndianConversion);
        }
        return ValueConverterHelper.Value((short)(value ? 1 : 0), _configuration.NeedEndianConversion);
    }
}
