using System.Linq.Expressions;

namespace MessageToolkit;

public sealed class ProtocolDataMapping<TProtocol>(
    IProtocolConfiguration<TProtocol> configuration,
    IMessageBuilder<TProtocol> messageBuilder) : IProtocolDataMapping<TProtocol> where TProtocol : struct
{
    private readonly IProtocolConfiguration<TProtocol> _configuration = configuration;
    private readonly IMessageBuilder<TProtocol> _messageBuilder = messageBuilder;
    private readonly Dictionary<ushort, byte[]> _writeBuffer = [];

    public PropertyValueSetter<TProtocol, TValue> Property<TValue>(Expression<Func<TProtocol, TValue>> propertyExpression)
    {
        ArgumentNullException.ThrowIfNull(propertyExpression);

        if (propertyExpression.Body is not MemberExpression memberExpression)
        {
            throw new ArgumentException("无效的属性表达式", nameof(propertyExpression));
        }

        string propertyName = memberExpression.Member.Name;
        if (!_configuration.AddressMapping.TryGetValue(propertyName, out ushort address))
        {
            throw new ArgumentException($"找不到属性 {propertyName} 的地址映射");
        }

        return new PropertyValueSetter<TProtocol, TValue>(
            this,
            _configuration,
            address);
    }

    public PropertyValueSetter<TProtocol, TValue> Property<TValue>(ushort address)
    {
        return new PropertyValueSetter<TProtocol, TValue>(
            this,
            _configuration,
            address);
    }

    public void AddData(ushort address, byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        _writeBuffer.Add(address, data);
    }

    public void Commit()
    {
        if (_writeBuffer.Count == 0)
            throw new InvalidOperationException("没有要写入的数据");

        var combinedDatas = CombineBufferData();
        _messageBuilder.Commit(combinedDatas);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_writeBuffer.Count == 0)
            throw new InvalidOperationException("没有要写入的数据");

        var combinedDatas = CombineBufferData();
        await _messageBuilder.CommitAsync(combinedDatas, cancellationToken);
    }

    private Dictionary<ushort, byte[]> CombineBufferData()
    {
        // 按地址排序
        var orderedAddresses = _writeBuffer.Keys.OrderBy(address => address).ToList();
        var combinedDatas = new Dictionary<ushort, byte[]>();
        int i = 0;

        while (i < orderedAddresses.Count)
        {
            ushort startAddress = orderedAddresses[i];
            byte[] combinedData = _writeBuffer[startAddress];
            int endAddress = startAddress + combinedData.Length;
            int j = i + 1;

            while (j < orderedAddresses.Count)
            {
                ushort nextAddress = orderedAddresses[j];

                if (endAddress == nextAddress)
                {
                    byte[] nextData = _writeBuffer[nextAddress];
                    combinedData = [.. combinedData, .. nextData];
                    endAddress = nextAddress + nextData.Length;
                    j++;
                }
                else
                {
                    break;
                }
            }

            combinedDatas[startAddress] = combinedData;
            i = j;
        }

        return combinedDatas;
    }
}
