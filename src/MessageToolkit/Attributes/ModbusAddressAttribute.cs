namespace MessageToolkit;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class AddressAttribute(ushort address) : Attribute
{
    /// <summary>
    /// 字节地址(寄存器地址*2)
    /// </summary>
    public ushort Address { get; } = address;
}
