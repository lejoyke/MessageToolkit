using System.Linq.Expressions;
using MessageToolkit.Abstractions;
using MessageToolkit.Models;

namespace MessageToolkit;

/// <summary>
/// Modbus 数据映射 - 用于字节协议的批量写入构建
/// </summary>
/// <remarks>
/// <para>核心功能：</para>
/// <list type="bullet">
///   <item><description>链式 API 批量添加写入操作</description></item>
///   <item><description>自动将值类型转换为字节数组</description></item>
///   <item><description>支持连续地址的帧合并优化</description></item>
/// </list>
/// </remarks>
/// <typeparam name="TProtocol">协议结构体类型</typeparam>
public sealed class ModbusDataMapping<TProtocol> : IDataMapping<TProtocol, byte>
    where TProtocol : struct
{
    private readonly IProtocolSchema<TProtocol> _schema;
    private readonly ModbusProtocolCodec<TProtocol> _codec;
    private readonly List<WriteEntry> _pendingWrites;

    private const int DefaultCapacity = 16;

    /// <summary>
    /// 已添加的数据项数量
    /// </summary>
    public int Count => _pendingWrites.Count;

    /// <summary>
    /// 创建 Modbus 数据映射
    /// </summary>
    /// <param name="schema">协议模式</param>
    /// <param name="codec">字节编解码器</param>
    public ModbusDataMapping(IProtocolSchema<TProtocol> schema, ModbusProtocolCodec<TProtocol> codec)
    {
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        _codec = codec ?? throw new ArgumentNullException(nameof(codec));
        _pendingWrites = new List<WriteEntry>(DefaultCapacity);
    }

    /// <summary>
    /// 添加原始字节数据
    /// </summary>
    /// <param name="address">目标地址</param>
    /// <param name="data">字节数据</param>
    public void AddData(int address, byte data)
    {
        _pendingWrites.Add(new WriteEntry((ushort)address, [data]));
    }

    /// <summary>
    /// 添加字段写入（链式调用）
    /// </summary>
    /// <remarks>
    /// 执行流程：
    /// <list type="number">
    ///   <item><description>通过 Lambda 表达式解析字段地址</description></item>
    ///   <item><description>将值转换为字节数组</description></item>
    ///   <item><description>添加到待写入列表</description></item>
    /// </list>
    /// </remarks>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="fieldSelector">字段选择器</param>
    /// <param name="value">要写入的值</param>
    /// <returns>当前映射实例，支持链式调用</returns>
    public IDataMapping<TProtocol, byte> Property<TValue>(
        Expression<Func<TProtocol, TValue>> fieldSelector,
        TValue value) where TValue : unmanaged
    {
        var address = _schema.GetAddress(fieldSelector);
        var data = _codec.EncodeValue(value);
        _pendingWrites.Add(new WriteEntry(address, data));
        return this;
    }

    /// <summary>
    /// 添加地址写入（链式调用）
    /// </summary>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="address">目标地址</param>
    /// <param name="value">要写入的值</param>
    /// <returns>当前映射实例，支持链式调用</returns>
    public IDataMapping<TProtocol, byte> Property<TValue>(ushort address, TValue value) where TValue : unmanaged
    {
        var data = _codec.EncodeValue(value);
        _pendingWrites.Add(new WriteEntry(address, data));
        return this;
    }

    /// <summary>
    /// 构建帧集合（每个写入操作生成独立帧）
    /// </summary>
    public IEnumerable<IFrame<byte>> Build()
    {
        foreach (var entry in _pendingWrites)
        {
            yield return new ModbusWriteFrame(entry.Address, entry.Data);
        }
    }

    /// <summary>
    /// 构建并优化（合并连续地址）
    /// </summary>
    /// <remarks>
    /// <para>优化规则：</para>
    /// <list type="bullet">
    ///   <item><description>按地址排序所有写入操作</description></item>
    ///   <item><description>合并地址连续的写入为单个帧</description></item>
    ///   <item><description>地址不连续的写入生成独立的帧</description></item>
    /// </list>
    /// <para>示例：</para>
    /// <code>
    /// 输入：地址 100 (4字节), 地址 104 (4字节), 地址 200 (2字节)
    /// 输出：帧1 (地址 100, 8字节), 帧2 (地址 200, 2字节)
    /// </code>
    /// </remarks>
    public IEnumerable<IFrame<byte>> BuildOptimized()
    {
        if (_pendingWrites.Count == 0)
        {
            yield break;
        }

        var entries = _pendingWrites.ToArray();
        Array.Sort(entries, static (a, b) => a.Address.CompareTo(b.Address));

        var index = 0;
        while (index < entries.Length)
        {
            var startEntry = entries[index];
            var startAddr = startEntry.Address;

            var totalLength = startEntry.Data.Length;
            var endAddr = startAddr + startEntry.Data.Length;
            var mergeCount = 1;

            // 查找可合并的连续地址
            for (var i = index + 1; i < entries.Length; i++)
            {
                var nextEntry = entries[i];
                if (nextEntry.Address != endAddr)
                    break;

                totalLength += nextEntry.Data.Length;
                endAddr = nextEntry.Address + nextEntry.Data.Length;
                mergeCount++;
            }

            if (mergeCount == 1)
            {
                // 单个写入，直接返回
                yield return new ModbusWriteFrame(startAddr, startEntry.Data);
            }
            else
            {
                // 合并多个写入
                var mergedData = new byte[totalLength];
                var offset = 0;

                for (var i = index; i < index + mergeCount; i++)
                {
                    var data = entries[i].Data;
                    data.CopyTo(mergedData, offset);
                    offset += data.Length;
                }

                yield return new ModbusWriteFrame(startAddr, mergedData);
            }

            index += mergeCount;
        }
    }

    /// <summary>
    /// 清空已添加的数据
    /// </summary>
    public void Clear()
    {
        _pendingWrites.Clear();
    }

    private readonly struct WriteEntry(ushort address, byte[] data)
    {
        public ushort Address { get; } = address;
        public byte[] Data { get; } = data;
    }
}
