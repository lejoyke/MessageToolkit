using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using MessageToolkit.Abstractions;
using MessageToolkit.Internal;
using MessageToolkit.Models;

namespace MessageToolkit;

/// <summary>
/// 字节协议编解码器 - 类型转换 + 地址映射
/// </summary>
/// <remarks>
public sealed class ModbusProtocolCodec<TProtocol> : IProtocolCodec<TProtocol, byte>
    where TProtocol : struct
{
    /// <summary>
    /// 协议模式
    /// </summary>
    public IProtocolSchema<TProtocol> Schema { get; }

    private readonly Dictionary<string, PropertyInfo> _propertyMap;
    private readonly (ProtocolFieldInfo Info, PropertyInfo Property)[] _orderedProperties;

    /// <summary>
    /// 创建字节协议编解码器
    /// </summary>
    /// <param name="schema">协议模式</param>
    public ModbusProtocolCodec(IProtocolSchema<TProtocol> schema)
    {
        Schema = schema ?? throw new ArgumentNullException(nameof(schema));

        _propertyMap = typeof(TProtocol)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(p => p.Name, p => p, StringComparer.Ordinal);

        _orderedProperties = BuildPropertyAccessors(Schema.Properties.Values);
    }

    #region 协议编解码

    /// <summary>
    /// 编码：协议结构体 → 字节数组
    /// </summary>
    /// <remarks>
    /// 执行流程：
    /// <list type="number">
    ///   <item><description>遍历所有标记了地址的属性</description></item>
    ///   <item><description>根据属性类型执行类型转换（值类型 → 字节）</description></item>
    ///   <item><description>根据字节序配置处理字节顺序</description></item>
    ///   <item><description>将字节写入到对应地址偏移位置</description></item>
    /// </list>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] Encode(TProtocol protocol)
    {
        var buffer = new byte[Schema.TotalSize];

        foreach (var (fieldInfo, property) in _orderedProperties)
        {
            var value = property.GetValue(protocol)
                        ?? throw new InvalidOperationException($"属性 {fieldInfo.Name} 的值为 null");
            var bytes = EncodeValueInternal(value, fieldInfo.FieldType);
            var offset = fieldInfo.Address - Schema.StartAddress;
            bytes.CopyTo(buffer.AsSpan(offset));
        }

        return buffer;
    }

    /// <summary>
    /// 解码：字节数组 → 协议结构体
    /// </summary>
    /// <remarks>
    /// 执行流程：
    /// <list type="number">
    ///   <item><description>遍历所有标记了地址的属性</description></item>
    ///   <item><description>从对应地址偏移位置读取字节</description></item>
    ///   <item><description>根据字节序配置处理字节顺序</description></item>
    ///   <item><description>根据属性类型执行类型转换（字节 → 值类型）</description></item>
    /// </list>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TProtocol Decode(ReadOnlySpan<byte> data)
    {
        if (data.Length < Schema.TotalSize)
        {
            throw new ArgumentException(
                $"数据长度不足: 需要 {Schema.TotalSize} 字节, 实际 {data.Length} 字节",
                nameof(data));
        }

        var result = new TProtocol();
        object boxed = result;

        foreach (var (fieldInfo, property) in _orderedProperties)
        {
            var offset = fieldInfo.Address - Schema.StartAddress;
            var valueBytes = data.Slice(offset, fieldInfo.Size);
            var value = DecodeValueInternal(valueBytes, fieldInfo.FieldType);
            if (property.SetMethod == null)
            {
                throw new InvalidOperationException($"属性 {fieldInfo.Name} 不可写");
            }
            property.SetValue(boxed, value);
        }

        return (TProtocol)boxed;
    }

    #endregion

    #region 单值编解码

    /// <summary>
    /// 编码单个值：值类型 → 字节数组
    /// </summary>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="value">要编码的值</param>
    /// <returns>编码后的字节数组</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] EncodeValue<TValue>(TValue value) where TValue : unmanaged
    {
        var type = typeof(TValue);
        return EncodeValueInternal(value, type);
    }

    /// <summary>
    /// 解码单个值：字节数组 → 值类型
    /// </summary>
    /// <typeparam name="TValue">目标值类型</typeparam>
    /// <param name="data">字节数据</param>
    /// <returns>解码后的值</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue DecodeValue<TValue>(ReadOnlySpan<byte> data) where TValue : unmanaged
    {
        var expectedSize = GetValueSize(typeof(TValue));
        if (data.Length < expectedSize)
        {
            throw new ArgumentException(
                $"数据长度不足: 需要 {expectedSize} 字节, 实际 {data.Length} 字节",
                nameof(data));
        }

        var value = DecodeValueInternal(data[..expectedSize], typeof(TValue));
        return (TValue)value;
    }

    /// <summary>
    /// 编码协议中的指定字段
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] EncodeField<TValue>(TProtocol protocol, Expression<Func<TProtocol, TValue>> fieldSelector)
    {
        var memberName = GetMemberName(fieldSelector);
        var property = GetProperty(memberName);
        var value = property.GetValue(protocol)
                    ?? throw new InvalidOperationException($"属性 {memberName} 的值为 null");
        var fieldInfo = Schema.GetFieldInfo(memberName);
        return EncodeValueInternal(value, fieldInfo.FieldType);
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 提取协议中所有布尔字段的值
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Dictionary<int, bool> ExtractBooleanValues(TProtocol protocol)
    {
        var result = new Dictionary<int, bool>();
        foreach (var (info, property) in _orderedProperties)
        {
            if (info.FieldType == typeof(bool))
            {
                var value = property.GetValue(protocol);
                if (value is bool b)
                {
                    result[info.Address] = b;
                }
            }
        }
        return result;
    }

    #endregion

    #region 私有方法

    private int GetValueSize(Type type)
    {
        if (type == typeof(bool))
        {
            return Schema.BooleanType == BooleanRepresentation.Int32 ? 4 : 2;
        }

        if (type.IsEnum)
        {
            return Marshal.SizeOf(Enum.GetUnderlyingType(type));
        }

        return Marshal.SizeOf(type);
    }

    private static string GetMemberName<TValue>(Expression<Func<TProtocol, TValue>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        if (expression.Body is UnaryExpression { Operand: MemberExpression unaryMember })
        {
            return unaryMember.Member.Name;
        }

        throw new ArgumentException("无效的字段访问表达式", nameof(expression));
    }

    private (ProtocolFieldInfo Info, PropertyInfo Property)[] BuildPropertyAccessors(
        IEnumerable<ProtocolFieldInfo> fields)
    {
        var orderedInfos = fields.OrderBy(f => f.Address).ToArray();
        var result = new (ProtocolFieldInfo Info, PropertyInfo Property)[orderedInfos.Length];

        for (var i = 0; i < orderedInfos.Length; i++)
        {
            var info = orderedInfos[i];
            var property = GetProperty(info.Name);
            result[i] = (info, property);
        }

        return result;
    }

    private PropertyInfo GetProperty(string name)
    {
        if (_propertyMap.TryGetValue(name, out var property))
        {
            return property;
        }

        throw new ArgumentException(
            $"找不到属性 {name}，请确认协议模型仅包含带 Address 特性的公共属性。");
    }

    /// <summary>
    /// 内部类型转换：值 → 字节数组
    /// </summary>
    private byte[] EncodeValueInternal(object? value, Type fieldType)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return fieldType switch
        {
            var t when t == typeof(byte) => [(byte)value],
            var t when t == typeof(sbyte) => [(byte)(sbyte)value],
            var t when t == typeof(short) => ByteConverter.GetBytes((short)value, Schema.Endianness),
            var t when t == typeof(ushort) => ByteConverter.GetBytes((ushort)value, Schema.Endianness),
            var t when t == typeof(int) => ByteConverter.GetBytes((int)value, Schema.Endianness),
            var t when t == typeof(uint) => ByteConverter.GetBytes((uint)value, Schema.Endianness),
            var t when t == typeof(long) => ByteConverter.GetBytes((long)value, Schema.Endianness),
            var t when t == typeof(ulong) => ByteConverter.GetBytes((ulong)value, Schema.Endianness),
            var t when t == typeof(float) => ByteConverter.GetBytes((float)value, Schema.Endianness),
            var t when t == typeof(double) => ByteConverter.GetBytes((double)value, Schema.Endianness),
            var t when t == typeof(bool) => EncodeBooleanValue((bool)value),
            _ when fieldType.IsEnum => EncodeValueInternal(
                Convert.ChangeType(value, Enum.GetUnderlyingType(fieldType))!,
                Enum.GetUnderlyingType(fieldType)),
            _ => throw new NotSupportedException($"不支持的类型 {fieldType.Name}")
        };
    }

    /// <summary>
    /// 编码布尔值（根据配置的表示方式）
    /// </summary>
    private byte[] EncodeBooleanValue(bool value)
    {
        return Schema.BooleanType switch
        {
            BooleanRepresentation.Boolean => [value ? (byte)1 : (byte)0],
            BooleanRepresentation.Int16 => ByteConverter.GetBytes((short)(value ? 1 : 0), Schema.Endianness),
            BooleanRepresentation.Int32 => ByteConverter.GetBytes(value ? 1 : 0, Schema.Endianness),
            _ => throw new NotSupportedException($"不支持的布尔表示方式: {Schema.BooleanType}")
        };
    }

    /// <summary>
    /// 内部类型转换：字节数组 → 值
    /// </summary>
    private object DecodeValueInternal(ReadOnlySpan<byte> data, Type fieldType)
    {
        return fieldType switch
        {
            var t when t == typeof(byte) => data[0],
            var t when t == typeof(sbyte) => (sbyte)data[0],
            var t when t == typeof(short) => ByteConverter.ToInt16(data, Schema.Endianness),
            var t when t == typeof(ushort) => ByteConverter.ToUInt16(data, Schema.Endianness),
            var t when t == typeof(int) => ByteConverter.ToInt32(data, Schema.Endianness),
            var t when t == typeof(uint) => ByteConverter.ToUInt32(data, Schema.Endianness),
            var t when t == typeof(long) => ByteConverter.ToInt64(data, Schema.Endianness),
            var t when t == typeof(ulong) => ByteConverter.ToUInt64(data, Schema.Endianness),
            var t when t == typeof(float) => ByteConverter.ToSingle(data, Schema.Endianness),
            var t when t == typeof(double) => ByteConverter.ToDouble(data, Schema.Endianness),
            var t when t == typeof(bool) => DecodeBooleanValue(data),
            _ when fieldType.IsEnum => Enum.ToObject(
                fieldType,
                DecodeValueInternal(data, Enum.GetUnderlyingType(fieldType))),
            _ => throw new NotSupportedException($"不支持的类型 {fieldType.Name}")
        };
    }

    /// <summary>
    /// 解码布尔值（根据配置的表示方式）
    /// </summary>
    private bool DecodeBooleanValue(ReadOnlySpan<byte> data)
    {
        return Schema.BooleanType switch
        {
            BooleanRepresentation.Boolean => data[0] != 0,
            BooleanRepresentation.Int16 => ByteConverter.ToInt16(data, Schema.Endianness) != 0,
            BooleanRepresentation.Int32 => ByteConverter.ToInt32(data, Schema.Endianness) != 0,
            _ => throw new NotSupportedException($"不支持的布尔表示方式: {Schema.BooleanType}")
        };
    }

    #endregion
}

