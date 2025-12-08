using ProjectLibrary.Communication;
using System.Runtime.InteropServices;

namespace MessageToolkit;

/// <summary>
/// 协议序列化基类，提供基本的配置管理和验证功能
/// </summary>
/// <typeparam name="TProtocol">协议类型</typeparam>
public abstract class ProtocolSerializeBase<TProtocol> : IProtocolSerialize<TProtocol>
    where TProtocol : struct
{
    /// <summary>
    /// 获取协议配置
    /// </summary>
    public IProtocolConfiguration<TProtocol> Configuration { get; }

    public abstract TProtocol Data { get; }

    public abstract byte[]? Buffer { get; }

    /// <summary>
    /// 创建协议序列化基类的新实例
    /// </summary>
    /// <param name="configuration">协议配置</param>
    /// <exception cref="ArgumentNullException">配置为空时抛出</exception>
    protected ProtocolSerializeBase(IProtocolConfiguration<TProtocol> configuration)
    {
        Configuration = configuration;
        ValidateConfiguration();
    }

    /// <summary>
    /// 验证配置的有效性
    /// </summary>
    /// <exception cref="InvalidOperationException">配置无效时抛出</exception>
    protected virtual void ValidateConfiguration()
    {
        if (Configuration.AddressMapping.Count == 0)
        {
            throw new InvalidOperationException("地址映射不能为空");
        }

        if (Configuration.Size <= 0)
        {
            throw new InvalidOperationException("数据大小必须大于0");
        }

        if (Configuration.StartAddress < 0)
        {
            throw new InvalidOperationException("起始地址不能为负数");
        }
    }

    /// <summary>
    /// 序列化协议数据
    /// </summary>
    /// <param name="protocol">要序列化的协议数据</param>
    /// <returns>序列化后的字节数组</returns>
    public abstract byte[] Serialize(TProtocol protocol);

    /// <summary>
    /// 反序列化协议数据
    /// </summary>
    /// <param name="bytes">要反序列化的字节数据</param>
    /// <returns>反序列化后的协议对象</returns>
    public abstract TProtocol Deserialize(ReadOnlySpan<byte> bytes);

    /// <summary>
    /// 创建字节缓冲区
    /// </summary>
    /// <returns>指定大小的字节数组</returns>
    protected byte[] CreateBuffer() => new byte[Configuration.Size];

    /// <summary>
    /// 验证字节数据的有效性
    /// </summary>
    /// <param name="bytes">要验证的字节数据</param>
    /// <exception cref="ArgumentException">数据无效时抛出</exception>
    protected void ValidateBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < Configuration.Size)
        {
            throw new ArgumentException(
                $"数据长度不足。需要 {Configuration.Size} 字节，实际接收到 {bytes.Length} 字节",
                nameof(bytes));
        }
    }

    /// <summary>
    /// 计算字节偏移量
    /// </summary>
    /// <param name="address">地址</param>
    /// <returns>相对于起始地址的偏移量</returns>
    protected int CalculateOffset(ushort address) => address - Configuration.StartAddress;

    public virtual TValue GetValueFromBytes<TValue>(ReadOnlySpan<byte> bytes)
    {
        return (TValue)GetValueFromBytes(bytes, typeof(TValue));
    }

    public virtual byte[] GetBytes<TValue>(TValue value) where TValue : unmanaged
    {
        if (value is bool boolValue)
        {
            if (Configuration.BooleanTypeFlag)
            {
                return ValueConverterHelper.Value(boolValue ? 1 : 0, Configuration.NeedEndianConversion);
            }
            return ValueConverterHelper.Value((short)(boolValue ? 1 : 0), Configuration.NeedEndianConversion);
        }

        return ValueConverterHelper.Value(value, Configuration.NeedEndianConversion);
    }

    protected byte[] GetBytesFromValue(object value, Type fieldType)
    {
        return fieldType switch
        {
            var t when t == typeof(int) => ByteUnit.GetBytes((int)value, Configuration.NeedEndianConversion),
            var t when t == typeof(short) => ByteUnit.GetBytes((short)value, Configuration.NeedEndianConversion),
            var t when t == typeof(float) => ByteUnit.GetBytes((float)value, Configuration.NeedEndianConversion),
            var t when t == typeof(ushort) => ByteUnit.GetBytes((ushort)value, Configuration.NeedEndianConversion),
            var t when t == typeof(uint) => ByteUnit.GetBytes((uint)value, Configuration.NeedEndianConversion),
            var t when t == typeof(bool) => Configuration.BooleanTypeFlag
                ? ByteUnit.GetBytes((bool)value ? 1 : 0, Configuration.NeedEndianConversion)
                : ByteUnit.GetBytes((short)((bool)value ? 1 : 0), Configuration.NeedEndianConversion),
            _ => throw new NotSupportedException($"不支持的类型 {fieldType.Name}")
        };
    }

    protected object GetValueFromBytes(ReadOnlySpan<byte> bytes, Type fieldType)
    {
        return fieldType switch
        {
            var t when t == typeof(int) => bytes.GetInt32(needConversion: Configuration.NeedEndianConversion),
            var t when t == typeof(short) => bytes.GetInt16(needConversion: Configuration.NeedEndianConversion),
            var t when t == typeof(float) => bytes.GetFloat(needConversion: Configuration.NeedEndianConversion),
            var t when t == typeof(ushort) => bytes.GetUInt16(needConversion: Configuration.NeedEndianConversion),
            var t when t == typeof(uint) => bytes.GetUInt32(needConversion: Configuration.NeedEndianConversion),
            var t when t == typeof(bool) => Configuration.BooleanTypeFlag
                ? bytes.GetInt32(needConversion: Configuration.NeedEndianConversion) == 1
                : bytes.GetInt16(needConversion: Configuration.NeedEndianConversion) == 1,
            _ => throw new NotSupportedException($"不支持的类型 {fieldType.Name}")
        };
    }
}
