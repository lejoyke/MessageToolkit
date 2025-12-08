using System.Collections.Frozen;
using System.Linq.Expressions;

namespace MessageToolkit;

public interface IProtocolConfiguration<TProtocol> where TProtocol : struct
{
    /// <summary>
    /// 协议起始地址
    /// </summary>
    int StartAddress { get; }

    /// <summary>
    /// 协议数据大小(byte)
    /// </summary>
    int Size { get; }

    /// <summary>
    /// 协议中的布尔类型是否为int类型
    /// </summary>
    bool BooleanTypeFlag { get; }

    /// <summary>
    /// 是否需要大小端转换
    /// </summary>
    bool NeedEndianConversion { get; }

    /// <summary>
    /// 地址映射表(字段名/属性名 -> 地址)
    /// </summary>
    FrozenDictionary<string, ushort> AddressMapping { get; }


    /// <summary>
    /// Boolean类型地址映射表(字段名/属性名 -> 地址)
    /// </summary>
    FrozenDictionary<string, ushort> BooleanAddressMapping { get; }

    /// <summary>
    /// NotBoolean类型地址映射表(字段名/属性名 -> 地址)
    /// </summary>
    FrozenDictionary<string, ushort> NotBooleanAddressMapping { get; }

    /// <summary>
    /// 获取字段的地址
    /// </summary>
    /// <param name="memberName"></param>
    /// <returns></returns>
    public ushort GetAddress(string memberName);

    /// <summary>
    /// 获取字段的地址
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public ushort GetAddress<TValue>(Expression<Func<TProtocol, TValue>> expression);
}
