using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MessageToolkit;

public sealed class ProtocolConfiguration<TProtocol> : IProtocolConfiguration<TProtocol> where TProtocol : struct
{
    public int StartAddress { get; private set; }
    public int Size { get; private set; }
    public bool BooleanTypeFlag { get; }

    private readonly int _boolTypeSize;

    public bool NeedEndianConversion { get; }
    public FrozenDictionary<string, ushort> AddressMapping { get; private set; }

    public FrozenDictionary<string, ushort> BooleanAddressMapping { get; private set; }

    public FrozenDictionary<string, ushort> NotBooleanAddressMapping { get; private set; }

    public ProtocolConfiguration() : this(typeof(short), false)
    {
    }

    public ProtocolConfiguration(Type boolType, bool needEndianConversion)
    {
        BooleanTypeFlag = boolType == typeof(int);
        _boolTypeSize = BooleanTypeFlag ? 4 : 2;
        NeedEndianConversion = needEndianConversion;

        StartAddress = int.MaxValue;
        Size = 0;

        AddressMapping = BuildAddressMapping();
    }

    public ushort GetAddress(string memberName)
    {
        if (AddressMapping.TryGetValue(memberName, out var address))
        {
            return address;
        }
        throw new ArgumentException($"Field '{memberName}' not found in address mapping.");
    }

    public ushort GetAddress<TValue>(Expression<Func<TProtocol, TValue>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            var fieldName = memberExpression.Member.Name;
            return GetAddress(fieldName);
        }
        throw new ArgumentException("Invalid expression. Expected a member expression.");
    }

    [MemberNotNull(nameof(BooleanAddressMapping))]
    [MemberNotNull(nameof(NotBooleanAddressMapping))]
    private FrozenDictionary<string, ushort> BuildAddressMapping()
    {
        var finalAddressSize = 0;
        var maxAddress = 0;
        var mapping = new Dictionary<string, ushort>();
        var boolMapping = new Dictionary<string, ushort>();
        var notBoolMapping = new Dictionary<string, ushort>();

        // 获取字段
        var fields = typeof(TProtocol).GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (var field in fields)
        {
            var attribute = field.GetCustomAttribute<AddressAttribute>();
            if (attribute == null) continue;

            int fieldSize = Marshal.SizeOf(field.FieldType);
            if (field.FieldType == typeof(bool))
            {
                fieldSize = _boolTypeSize;
                boolMapping.Add(field.Name, attribute.Address);
            }
            else
            {
                notBoolMapping.Add(field.Name, attribute.Address);
            }

            CompareAddress(attribute.Address, fieldSize);
            mapping[field.Name] = attribute.Address;
        }

        // 获取属性
        var properties = typeof(TProtocol).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttribute<AddressAttribute>();
            if (attribute == null) continue;

            int fieldSize = Marshal.SizeOf(property.PropertyType);
            if (property.PropertyType == typeof(bool))
            {
                fieldSize = _boolTypeSize;
                boolMapping.Add(property.Name, attribute.Address);
            }
            else
            {
                notBoolMapping.Add(property.Name, attribute.Address);
            }

            CompareAddress(attribute.Address, fieldSize);
            mapping[property.Name] = attribute.Address;
        }

        // 计算总大小
        Size = maxAddress - StartAddress + finalAddressSize;

        BooleanAddressMapping = boolMapping.ToFrozenDictionary();
        NotBooleanAddressMapping = notBoolMapping.ToFrozenDictionary();

        return mapping.ToFrozenDictionary();

        void CompareAddress(int address, int typeSize)
        {
            if (address > maxAddress)
            {
                maxAddress = address;
                finalAddressSize = typeSize;
            }

            if (address < StartAddress)
            {
                StartAddress = address;
            }
        }
    }
}
