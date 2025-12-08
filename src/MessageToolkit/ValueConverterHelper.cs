using ProjectLibrary.Communication;

namespace MessageToolkit;

public sealed class ValueConverterHelper
{
    /// <summary>
    /// 将值转换为字节数组
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="value">要转换的值</param>
    /// <param name="needEndianConversion">是否需要大小端转换</param>
    /// <returns>字节数组</returns>
    /// <exception cref="ArgumentException">不支持的数据类型时抛出</exception>
    public static byte[] Value<T>(T value, bool needEndianConversion = false)
    {
        byte[] datas = null;

        if (value is float floatValue)
        {
            datas = ByteUnit.GetBytes(floatValue, needEndianConversion);
        }
        else if (value is int intValue)
        {
            datas = ByteUnit.GetBytes(intValue, needEndianConversion);
        }
        else if (value is uint uintValue)
        {
            datas = ByteUnit.GetBytes(uintValue, needEndianConversion);
        }
        else if (value is short shortValue)
        {
            datas = ByteUnit.GetBytes(shortValue, needEndianConversion);
        }
        else if (value is ushort ushortValue)
        {
            datas = ByteUnit.GetBytes(ushortValue, needEndianConversion);
        }
        else
        {
            throw new ArgumentException("Unsupported data type");
        }
        return datas;
    }
}
