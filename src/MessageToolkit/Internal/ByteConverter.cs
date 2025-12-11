using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using MessageToolkit.Models;

namespace MessageToolkit.Internal;

/// <summary>
/// 高性能字节转换工具 - 处理值类型与字节数组之间的转换
/// </summary>
internal static class ByteConverter
{
    #region short/ushort

    /// <summary>
    /// 将 short 转换为字节数组
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] GetBytes(short value, Endianness endianness)
    {
        var bytes = new byte[2];
        WriteBytes(value, bytes, endianness);
        return bytes;
    }

    /// <summary>
    /// 将 short 写入到 Span
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBytes(short value, Span<byte> destination, Endianness endianness)
    {
        if (endianness == Endianness.BigEndian)
            BinaryPrimitives.WriteInt16BigEndian(destination, value);
        else
            BinaryPrimitives.WriteInt16LittleEndian(destination, value);
    }

    /// <summary>
    /// 将 ushort 转换为字节数组
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] GetBytes(ushort value, Endianness endianness)
    {
        var bytes = new byte[2];
        WriteBytes(value, bytes, endianness);
        return bytes;
    }

    /// <summary>
    /// 将 ushort 写入到 Span
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBytes(ushort value, Span<byte> destination, Endianness endianness)
    {
        if (endianness == Endianness.BigEndian)
            BinaryPrimitives.WriteUInt16BigEndian(destination, value);
        else
            BinaryPrimitives.WriteUInt16LittleEndian(destination, value);
    }

    /// <summary>
    /// 从字节读取 short
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short ToInt16(ReadOnlySpan<byte> bytes, Endianness endianness)
    {
        return endianness == Endianness.BigEndian
            ? BinaryPrimitives.ReadInt16BigEndian(bytes)
            : BinaryPrimitives.ReadInt16LittleEndian(bytes);
    }

    /// <summary>
    /// 从字节读取 ushort
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ToUInt16(ReadOnlySpan<byte> bytes, Endianness endianness)
    {
        return endianness == Endianness.BigEndian
            ? BinaryPrimitives.ReadUInt16BigEndian(bytes)
            : BinaryPrimitives.ReadUInt16LittleEndian(bytes);
    }

    #endregion

    #region int/uint

    /// <summary>
    /// 将 int 转换为字节数组（Modbus 字序：高字在前，字内大端）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] GetBytes(int value, Endianness endianness)
    {
        var bytes = new byte[4];
        WriteBytes(value, bytes, endianness);
        return bytes;
    }

    /// <summary>
    /// 将 int 写入到 Span（Modbus 字序）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBytes(int value, Span<byte> destination, Endianness endianness)
    {
        if (endianness == Endianness.BigEndian)
        {
            // Modbus 大端序: Big-Endian Word Swap
            var high = (short)(value >> 16);
            var low = (short)value;
            BinaryPrimitives.WriteInt16BigEndian(destination, low);
            BinaryPrimitives.WriteInt16BigEndian(destination[2..], high);
        }
        else
        {
            BinaryPrimitives.WriteInt32LittleEndian(destination, value);
        }
    }

    /// <summary>
    /// 将 uint 转换为字节数组
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] GetBytes(uint value, Endianness endianness)
    {
        var bytes = new byte[4];
        WriteBytes(value, bytes, endianness);
        return bytes;
    }

    /// <summary>
    /// 将 uint 写入到 Span
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBytes(uint value, Span<byte> destination, Endianness endianness)
    {
        if (endianness == Endianness.BigEndian)
        {
            var high = (ushort)(value >> 16);
            var low = (ushort)value;
            BinaryPrimitives.WriteUInt16BigEndian(destination, low);
            BinaryPrimitives.WriteUInt16BigEndian(destination[2..], high);
        }
        else
        {
            BinaryPrimitives.WriteUInt32LittleEndian(destination, value);
        }
    }

    /// <summary>
    /// 从字节读取 int（Modbus 字序）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToInt32(ReadOnlySpan<byte> bytes, Endianness endianness)
    {
        if (endianness == Endianness.BigEndian)
        {
            var low = BinaryPrimitives.ReadInt16BigEndian(bytes);
            var high = BinaryPrimitives.ReadInt16BigEndian(bytes[2..]);
            return (high << 16) | (ushort)low;
        }
        return BinaryPrimitives.ReadInt32LittleEndian(bytes);
    }

    /// <summary>
    /// 从字节读取 uint（Modbus 字序）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ToUInt32(ReadOnlySpan<byte> bytes, Endianness endianness)
    {
        if (endianness == Endianness.BigEndian)
        {
            var low = BinaryPrimitives.ReadUInt16BigEndian(bytes);
            var high = BinaryPrimitives.ReadUInt16BigEndian(bytes[2..]);
            return ((uint)high << 16) | low;
        }
        return BinaryPrimitives.ReadUInt32LittleEndian(bytes);
    }

    #endregion

    #region long/ulong

    /// <summary>
    /// 将 long 转换为字节数组
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] GetBytes(long value, Endianness endianness)
    {
        var bytes = new byte[8];
        WriteBytes(value, bytes, endianness);
        return bytes;
    }

    /// <summary>
    /// 将 long 写入到 Span（Modbus 字序）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBytes(long value, Span<byte> destination, Endianness endianness)
    {
        if (endianness == Endianness.BigEndian)
        {
            // Modbus 大端序: Word Swap
            var w0 = (ushort)value;
            var w1 = (ushort)(value >> 16);
            var w2 = (ushort)(value >> 32);
            var w3 = (ushort)(value >> 48);
            BinaryPrimitives.WriteUInt16BigEndian(destination, w0);
            BinaryPrimitives.WriteUInt16BigEndian(destination[2..], w1);
            BinaryPrimitives.WriteUInt16BigEndian(destination[4..], w2);
            BinaryPrimitives.WriteUInt16BigEndian(destination[6..], w3);
        }
        else
        {
            BinaryPrimitives.WriteInt64LittleEndian(destination, value);
        }
    }

    /// <summary>
    /// 将 ulong 转换为字节数组
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] GetBytes(ulong value, Endianness endianness)
    {
        var bytes = new byte[8];
        WriteBytes(value, bytes, endianness);
        return bytes;
    }

    /// <summary>
    /// 将 ulong 写入到 Span
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBytes(ulong value, Span<byte> destination, Endianness endianness)
    {
        if (endianness == Endianness.BigEndian)
        {
            var w0 = (ushort)value;
            var w1 = (ushort)(value >> 16);
            var w2 = (ushort)(value >> 32);
            var w3 = (ushort)(value >> 48);
            BinaryPrimitives.WriteUInt16BigEndian(destination, w0);
            BinaryPrimitives.WriteUInt16BigEndian(destination[2..], w1);
            BinaryPrimitives.WriteUInt16BigEndian(destination[4..], w2);
            BinaryPrimitives.WriteUInt16BigEndian(destination[6..], w3);
        }
        else
        {
            BinaryPrimitives.WriteUInt64LittleEndian(destination, value);
        }
    }

    /// <summary>
    /// 从字节读取 long（Modbus 字序）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ToInt64(ReadOnlySpan<byte> bytes, Endianness endianness)
    {
        if (endianness == Endianness.BigEndian)
        {
            var w0 = BinaryPrimitives.ReadUInt16BigEndian(bytes);
            var w1 = BinaryPrimitives.ReadUInt16BigEndian(bytes[2..]);
            var w2 = BinaryPrimitives.ReadUInt16BigEndian(bytes[4..]);
            var w3 = BinaryPrimitives.ReadUInt16BigEndian(bytes[6..]);
            return w0 | ((long)w1 << 16) | ((long)w2 << 32) | ((long)w3 << 48);
        }
        return BinaryPrimitives.ReadInt64LittleEndian(bytes);
    }

    /// <summary>
    /// 从字节读取 ulong（Modbus 字序）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ToUInt64(ReadOnlySpan<byte> bytes, Endianness endianness)
    {
        if (endianness == Endianness.BigEndian)
        {
            var w0 = BinaryPrimitives.ReadUInt16BigEndian(bytes);
            var w1 = BinaryPrimitives.ReadUInt16BigEndian(bytes[2..]);
            var w2 = BinaryPrimitives.ReadUInt16BigEndian(bytes[4..]);
            var w3 = BinaryPrimitives.ReadUInt16BigEndian(bytes[6..]);
            return w0 | ((ulong)w1 << 16) | ((ulong)w2 << 32) | ((ulong)w3 << 48);
        }
        return BinaryPrimitives.ReadUInt64LittleEndian(bytes);
    }

    #endregion

    #region float/double

    /// <summary>
    /// 将 float 转换为字节数组
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] GetBytes(float value, Endianness endianness)
    {
        var bytes = new byte[4];
        WriteBytes(value, bytes, endianness);
        return bytes;
    }

    /// <summary>
    /// 将 float 写入到 Span
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBytes(float value, Span<byte> destination, Endianness endianness)
    {
        var intValue = BitConverter.SingleToInt32Bits(value);
        WriteBytes(intValue, destination, endianness);
    }

    /// <summary>
    /// 从字节读取 float（Modbus 字序）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ToSingle(ReadOnlySpan<byte> bytes, Endianness endianness)
    {
        var intValue = ToInt32(bytes, endianness);
        return BitConverter.Int32BitsToSingle(intValue);
    }

    /// <summary>
    /// 将 double 转换为字节数组
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] GetBytes(double value, Endianness endianness)
    {
        var bytes = new byte[8];
        WriteBytes(value, bytes, endianness);
        return bytes;
    }

    /// <summary>
    /// 将 double 写入到 Span
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBytes(double value, Span<byte> destination, Endianness endianness)
    {
        var longValue = BitConverter.DoubleToInt64Bits(value);
        WriteBytes(longValue, destination, endianness);
    }

    /// <summary>
    /// 从字节读取 double（Modbus 字序）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ToDouble(ReadOnlySpan<byte> bytes, Endianness endianness)
    {
        var longValue = ToInt64(bytes, endianness);
        return BitConverter.Int64BitsToDouble(longValue);
    }

    #endregion

    #region Buffer Pool

    /// <summary>
    /// 从 ArrayPool 租用数组
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] RentBuffer(int minimumLength)
        => ArrayPool<byte>.Shared.Rent(minimumLength);

    /// <summary>
    /// 归还租用的数组
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReturnBuffer(byte[] buffer)
        => ArrayPool<byte>.Shared.Return(buffer);

    #endregion
}
