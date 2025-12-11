using System.Linq.Expressions;
using MessageToolkit.Models;

namespace MessageToolkit.Abstractions;

/// <summary>
/// Modbus 数据映射接口 - 用于字节协议的批量写入
/// </summary>
/// <typeparam name="TProtocol">协议结构体类型</typeparam>
public interface IModbusDataMapping<TProtocol>
    where TProtocol : struct
{
    /// <summary>
    /// 已添加的数据项数量
    /// </summary>
    int Count { get; }

    /// <summary>
    /// 添加字段写入
    /// </summary>
    IModbusDataMapping<TProtocol> Write<TValue>(Expression<Func<TProtocol, TValue>> fieldSelector, TValue value) where TValue : unmanaged;

    /// <summary>
    /// 添加地址写入
    /// </summary>
    IModbusDataMapping<TProtocol> Write<TValue>(ushort address, TValue value) where TValue : unmanaged;


    IModbusDataMapping<TProtocol> WriteRaw(ushort address, byte[] value);

    /// <summary>
    /// 构建帧集合（每个写入操作生成独立帧）
    /// </summary>
    IEnumerable<ModbusWriteFrame> Build();

    /// <summary>
    /// 构建并优化（合并连续地址）
    /// </summary>
    IEnumerable<ModbusWriteFrame> BuildOptimized();

    /// <summary>
    /// 清空已添加的数据
    /// </summary>
    void Clear();
}
