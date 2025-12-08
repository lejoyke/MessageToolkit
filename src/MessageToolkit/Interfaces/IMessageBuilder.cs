using ProjectLibrary.Communication;
using System.Linq.Expressions;

namespace MessageToolkit;

public interface IMessageBuilder
{
    public FluentModbusClient? ModbusClient { get; set; }

    /// <summary>
    /// 写入对应的地址和数据
    /// </summary>
    /// <param name="datas">地址和数据的映射表</param>
    /// <returns></returns>
    void Commit(Dictionary<ushort, byte[]> datas);

    /// <summary>
    /// 写入对应的地址和数据（异步）
    /// </summary>
    /// <param name="datas">地址和数据的映射表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task CommitAsync(Dictionary<ushort, byte[]> datas, CancellationToken cancellationToken = default);
}

public interface IMessageBuilder<TProtocol> : IMessageBuilder where TProtocol : struct
{
    public IProtocolConfiguration<TProtocol> Configuration { get; }
    public IProtocolSerialize<TProtocol> ProtocolSerialize { get; }

    /// <summary>
    /// 创建写入映射表
    /// </summary>
    /// <returns></returns>
    IProtocolDataMapping<TProtocol> CreateWriteMapping();

    /// <summary>
    /// 单独读取协议中某段数据
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="propertyExpression"></param>
    /// <returns></returns>
    TValue ReadProtocol<TValue>(Expression<Func<TProtocol, TValue>> propertyExpression) where TValue : unmanaged;

    /// <summary>
    /// 单独读取协议中某段数据（异步）
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="propertyExpression"></param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task<TValue> ReadProtocolAsync<TValue>(Expression<Func<TProtocol, TValue>> propertyExpression, CancellationToken cancellationToken = default) where TValue : unmanaged;

    /// <summary>
    /// 单独写入协议中某段数据
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="propertyExpression"></param>
    /// <returns></returns>
    void WriteProtocol<TValue>(Expression<Func<TProtocol, TValue>> propertyExpression,TValue Value) where TValue : unmanaged;

    /// <summary>
    /// 单独写入协议中某段数据（异步）
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="propertyExpression"></param>
    /// <param name="Value"></param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task WriteProtocolAsync<TValue>(Expression<Func<TProtocol, TValue>> propertyExpression,TValue Value, CancellationToken cancellationToken = default) where TValue : unmanaged;

    /// <summary>
    /// 读取协议
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    TProtocol ReadProtocol();

    /// <summary>
    /// 读取协议（异步）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task<TProtocol> ReadProtocolAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 写入协议
    /// </summary>
    /// <param name="protocol"></param>
    /// <returns></returns>
    void WriteProtocol(TProtocol protocol);

    /// <summary>
    /// 写入协议（异步）
    /// </summary>
    /// <param name="protocol"></param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task WriteProtocolAsync(TProtocol protocol, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写入协议中的某个值
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="address"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    void WriteProtocol<TValue>(ushort address, TValue value) where TValue : unmanaged;

    /// <summary>
    /// 写入协议中的某个值（异步）
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="address"></param>
    /// <param name="value"></param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task WriteProtocolAsync<TValue>(ushort address, TValue value, CancellationToken cancellationToken = default) where TValue : unmanaged;

    /// <summary>
    /// 读取地址的某个值
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="address"></param>
    /// <returns></returns>
    TValue ReadProtocol<TValue>(ushort address) where TValue : unmanaged;

    /// <summary>
    /// 读取地址的某个值（异步）
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="address"></param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task<TValue> ReadProtocolAsync<TValue>(ushort address, CancellationToken cancellationToken = default) where TValue : unmanaged;
}
