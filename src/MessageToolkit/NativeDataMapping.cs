using System.Linq.Expressions;
using MessageToolkit.Abstractions;
using MessageToolkit.Models;

namespace MessageToolkit;

/// <summary>
/// 原生协议数据映射 - 用于原生类型的批量写入构建
/// </summary>
/// <remarks>
/// 适用于 bool、byte、int 等原生类型的批量操作，
/// 支持链式 API 和地址优化合并。
/// </remarks>
/// <typeparam name="TProtocol">协议结构体类型</typeparam>
/// <typeparam name="TData">原生数据类型</typeparam>
public sealed class NativeDataMapping<TProtocol, TData> : IDataMapping<TProtocol, TData>
    where TProtocol : struct
{
    private readonly IProtocolSchema<TProtocol> _schema;
    private readonly Dictionary<int, TData> _data = [];

    /// <summary>
    /// 已添加的数据项数量
    /// </summary>
    public int Count => _data.Count;

    /// <summary>
    /// 创建原生协议数据映射
    /// </summary>
    /// <param name="schema">协议模式</param>
    public NativeDataMapping(IProtocolSchema<TProtocol> schema)
    {
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
    }

    /// <summary>
    /// 添加字段写入（链式调用）
    /// </summary>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="fieldSelector">字段选择器</param>
    /// <param name="value">要写入的值</param>
    /// <returns>当前映射实例，支持链式调用</returns>
    public IDataMapping<TProtocol, TData> Property<TValue>(
        Expression<Func<TProtocol, TValue>> fieldSelector,
        TValue value) where TValue : unmanaged
    {
        var address = _schema.GetAddress(fieldSelector);
        if (value is not TData typedValue)
        {
            throw new ArgumentException(
                $"值类型 {typeof(TValue).Name} 与原生数据类型 {typeof(TData).Name} 不匹配。" +
                $"原生协议不进行类型转换，请确保值类型与协议数据类型一致。",
                nameof(value));
        }
        _data[address] = typedValue;
        return this;
    }

    /// <summary>
    /// 添加地址写入（链式调用）
    /// </summary>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="address">目标地址</param>
    /// <param name="value">要写入的值</param>
    /// <returns>当前映射实例，支持链式调用</returns>
    public IDataMapping<TProtocol, TData> Property<TValue>(ushort address, TValue value) where TValue : unmanaged
    {
        if (value is not TData typedValue)
        {
            throw new ArgumentException(
                $"值类型 {typeof(TValue).Name} 与原生数据类型 {typeof(TData).Name} 不匹配。" +
                $"原生协议不进行类型转换，请确保值类型与协议数据类型一致。",
                nameof(value));
        }
        _data[address] = typedValue;
        return this;
    }

    /// <summary>
    /// 构建帧集合（每个写入操作生成独立帧）
    /// </summary>
    public IEnumerable<IFrame<TData>> Build()
    {
        foreach (var kvp in _data)
        {
            yield return new NativeWriteFrame<TData>(kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// 构建并优化（合并连续地址）
    /// </summary>
    /// <remarks>
    /// 优化规则：
    /// - 按地址排序所有写入操作
    /// - 合并地址连续的写入为单个帧
    /// - 地址不连续的写入生成独立的帧
    /// </remarks>
    public IEnumerable<IFrame<TData>> BuildOptimized()
    {
        if (_data.Count == 0)
        {
            yield break;
        }

        var sortedEntries = _data.OrderBy(kvp => kvp.Key).ToArray();
        var index = 0;

        while (index < sortedEntries.Length)
        {
            var startAddr = sortedEntries[index].Key;
            var mergeCount = 1;

            // 查找连续地址
            for (var i = index + 1; i < sortedEntries.Length; i++)
            {
                if (sortedEntries[i].Key != startAddr + mergeCount)
                    break;
                mergeCount++;
            }

            // 创建帧
            var data = new TData[mergeCount];
            for (var i = 0; i < mergeCount; i++)
            {
                data[i] = sortedEntries[index + i].Value;
            }

            yield return new NativeWriteFrame<TData>(startAddr, data);
            index += mergeCount;
        }
    }

    /// <summary>
    /// 清空已添加的数据
    /// </summary>
    public void Clear()
    {
        _data.Clear();
    }
}
