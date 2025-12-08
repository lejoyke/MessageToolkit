namespace MessageToolkit;

/// <summary>
/// 协议序列化接口
/// </summary>
/// <typeparam name="TProtocol">协议类型</typeparam>
public interface IProtocolSerialize<TProtocol> where TProtocol : struct
{
    /// <summary>
    /// 获取协议配置
    /// </summary>
    IProtocolConfiguration<TProtocol> Configuration { get; }

    /// <summary>
    /// 最新的协议数据
    /// </summary>
    public TProtocol Data { get; }

    /// <summary>
    /// 最新协议数据
    /// </summary>
    public byte[]? Buffer { get; }

    /// <summary>
    /// 序列化协议数据
    /// </summary>
    /// <param name="protocol">要序列化的协议数据</param>
    /// <returns>序列化后的字节数组</returns>
    byte[] Serialize(TProtocol protocol);

    /// <summary>
    /// 反序列化协议数据
    /// </summary>
    /// <param name="bytes">要反序列化的字节数据</param>
    /// <returns>反序列化后的协议对象</returns>
    TProtocol Deserialize(ReadOnlySpan<byte> bytes);

    /// <summary>
    /// 从字节数组中解析值
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="bytes"></param>
    /// <returns></returns>
    TValue GetValueFromBytes<TValue>(ReadOnlySpan<byte> bytes);


    /// <summary>
    /// 将数据转换为字节数组
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    byte[] GetBytes<TValue>(TValue value) where TValue : unmanaged;
}