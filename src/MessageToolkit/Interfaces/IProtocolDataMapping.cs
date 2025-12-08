using System.Linq.Expressions;

namespace MessageToolkit;

public interface IProtocolDataMapping
{
    /// <summary>
    /// 向映射表中添加数据
    /// </summary>
    /// <param name="address"></param>
    /// <param name="data"></param>
    void AddData(ushort address, byte[] data);

    /// <summary>
    /// 提交数据
    /// </summary>
    /// <returns></returns>
    void Commit();

    /// <summary>
    /// 提交数据（异步）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task CommitAsync(CancellationToken cancellationToken = default);
}

public interface IProtocolDataMapping<TProtocol> : IProtocolDataMapping where TProtocol : struct
{
    /// <summary>
    /// Set the value of the property
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="propertyExpression"></param>
    /// <returns></returns>
    PropertyValueSetter<TProtocol, TValue> Property<TValue>(Expression<Func<TProtocol, TValue>> propertyExpression);


    PropertyValueSetter<TProtocol, TValue> Property<TValue>(ushort address);
}