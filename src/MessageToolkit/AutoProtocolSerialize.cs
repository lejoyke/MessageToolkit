using ProjectLibrary.Communication;
using System.Collections.Frozen;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MessageToolkit;

public sealed class AutoProtocolSerialize<TProtocol> : ProtocolSerializeBase<TProtocol> where TProtocol : struct
{
    private FrozenDictionary<string, MemberAccessor>? _memberAccessors;
    private byte[]? _buffer;
    private TProtocol _data;

    public override TProtocol Data => _data;

    public override byte[]? Buffer => _buffer?.DeepClone();

    public AutoProtocolSerialize(IProtocolConfiguration<TProtocol> configuration)
        : base(configuration)
    {
        InitializeMemberAccessors();
    }

    private class MemberAccessor
    {
        public required Type FieldType { get; init; }
        public required int Size { get; init; }
        public required Func<TProtocol, object> Getter { get; init; }
        public required Action<object, object> Setter { get; init; }
    }

    public override byte[] Serialize(TProtocol plcData)
    {
        ArgumentNullException.ThrowIfNull(_memberAccessors);
        var data = CreateBuffer();
        var dataSpan = data.AsSpan();

        foreach (var kvp in Configuration.AddressMapping)
        {
            string memberName = kvp.Key;
            int byteOffset = CalculateOffset(kvp.Value);

            if (!_memberAccessors.TryGetValue(memberName, out var memberAccessor))
                continue;

            var value = memberAccessor.Getter(plcData)
                ?? throw new InvalidOperationException($"Member '{memberName}' is null.");

            var valueBytes = GetBytesFromValue(value, memberAccessor.FieldType);
            valueBytes.CopyTo(dataSpan[byteOffset..]);
        }

        return data;
    }

    public override TProtocol Deserialize(ReadOnlySpan<byte> rawData)
    {
        ValidateBytes(rawData);
        ArgumentNullException.ThrowIfNull(_memberAccessors);

        TProtocol result = new();
        object resultObj = result; // 装箱，以便通过引用修改结构体

        foreach (var kvp in Configuration.AddressMapping)
        {
            string memberName = kvp.Key;
            int byteOffset = CalculateOffset(kvp.Value);

            if (!_memberAccessors.TryGetValue(memberName, out var memberAccessor))
                continue;

            var valueBytes = rawData.Slice(byteOffset, memberAccessor.Size);
            var value = GetValueFromBytes(valueBytes, memberAccessor.FieldType);
            memberAccessor.Setter(resultObj, value);
        }

        _buffer = rawData.ToArray();
        _data = (TProtocol)resultObj;
        return Data;
    }

    private void InitializeMemberAccessors()
    {
        var type = typeof(TProtocol);
        var accessors = new Dictionary<string, MemberAccessor>();

        foreach (var kvp in Configuration.AddressMapping)
        {
            string memberName = kvp.Key;
            var member = type.GetMember(memberName, BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault();

            if (member == null)
                continue;

            accessors[memberName] = CreateMemberAccessor(member)!;
        }

        _memberAccessors = accessors.ToFrozenDictionary();
    }

    private MemberAccessor? CreateMemberAccessor(MemberInfo member)
    {
        return member switch
        {
            FieldInfo fieldInfo => CreateFieldAccessor(fieldInfo),
            PropertyInfo propertyInfo => CreatePropertyAccessor(propertyInfo),
            _ => null
        };
    }

    private MemberAccessor CreateFieldAccessor(FieldInfo fieldInfo)
    {
        return new MemberAccessor
        {
            FieldType = fieldInfo.FieldType,
            Size = GetMemberSize(fieldInfo.FieldType),
            Getter = target => fieldInfo.GetValue(target)!,
            Setter = (target, value) => fieldInfo.SetValue(target, value)
        };
    }

    private MemberAccessor CreatePropertyAccessor(PropertyInfo propertyInfo)
    {
        return new MemberAccessor
        {
            FieldType = propertyInfo.PropertyType,
            Size = GetMemberSize(propertyInfo.PropertyType),
            Getter = target => propertyInfo.GetValue(target)!,
            Setter = (target, value) => propertyInfo.SetValue(target, value)
        };
    }

    private int GetMemberSize(Type type)
    {
        if (type == typeof(bool))
        {
            return Configuration.BooleanTypeFlag ? 4 : 2;
        }
        return Marshal.SizeOf(type);
    }
}
