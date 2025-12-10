using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using MessageToolkit;
using MessageToolkit.Abstractions;
using MessageToolkit.Attributes;
using MessageToolkit.Models;

namespace MessageToolkit.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ProtocolCodecBenchmarks
{
    private IProtocolSchema<TestProtocol> _schema = null!;
    private OldProtocolCodec<TestProtocol> _oldCodec = null!;
    private ProtocolCodec<TestProtocol> _newCodec = null!;
    private TestProtocol _testData;
    private byte[] _encodedData = null!;

    [GlobalSetup]
    public void Setup()
    {
        _schema = new ProtocolSchema<TestProtocol>(BooleanRepresentation.Int16, Endianness.BigEndian);
        _oldCodec = new OldProtocolCodec<TestProtocol>(_schema);
        _newCodec = new ProtocolCodec<TestProtocol>(_schema);

        _testData = new TestProtocol
        {
            Field01 = 1001, Field02 = 1002, Field03 = 1003, Field04 = 1004, Field05 = 1005,
            Field06 = 1006, Field07 = 1007, Field08 = 1008, Field09 = 1009, Field10 = 1010,
            Field11 = 11.1f, Field12 = 12.2f, Field13 = 13.3f, Field14 = 14.4f, Field15 = 15.5f,
            Field16 = 16.6f, Field17 = 17.7f, Field18 = 18.8f, Field19 = 19.9f, Field20 = 20.0f,
            Field21 = true, Field22 = false, Field23 = true, Field24 = false, Field25 = true,
            Field26 = false, Field27 = true, Field28 = false, Field29 = true, Field30 = false,
            Field31 = 31, Field32 = 32, Field33 = 33, Field34 = 34, Field35 = 35,
            Field36 = 36, Field37 = 37, Field38 = 38, Field39 = 39, Field40 = 40,
            Field41 = DeviceStatus.Running, Field42 = DeviceStatus.Starting, Field43 = DeviceStatus.Stopped,
            Field44 = DeviceStatus.Stopping, Field45 = DeviceStatus.Error,
            Field46 = 461, Field47 = 472, Field48 = 483, Field49 = 494, Field50 = 505
        };

        _encodedData = _newCodec.Encode(_testData);
    }

    // ==================== 编码性能测试 ====================

    [Benchmark(Baseline = true, Description = "优化前 - 编码")]
    public byte[] Old_Encode()
    {
        return _oldCodec.Encode(_testData);
    }

    [Benchmark(Description = "优化后 - 编码")]
    public byte[] New_Encode()
    {
        return _newCodec.Encode(_testData);
    }

    // ==================== 解码性能测试 ====================

    [Benchmark(Description = "优化前 - 解码")]
    public TestProtocol Old_Decode()
    {
        return _oldCodec.Decode(_encodedData);
    }

    [Benchmark(Description = "优化后 - 解码")]
    public TestProtocol New_Decode()
    {
        return _newCodec.Decode(_encodedData);
    }

    // ==================== 单值编码性能测试 ====================

    [Benchmark(Description = "优化前 - 编码单值")]
    public byte[] Old_EncodeValue()
    {
        return _oldCodec.EncodeValue(1234);
    }

    [Benchmark(Description = "优化后 - 编码单值")]
    public byte[] New_EncodeValue()
    {
        return _newCodec.EncodeValue(1234);
    }

    // ==================== 单值解码性能测试 ====================

    [Benchmark(Description = "优化前 - 解码单值")]
    public int Old_DecodeValue()
    {
        return _oldCodec.DecodeValue<int>(_encodedData.AsSpan(0, 4));
    }

    [Benchmark(Description = "优化后 - 解码单值")]
    public int New_DecodeValue()
    {
        return _newCodec.DecodeValue<int>(_encodedData.AsSpan(0, 4));
    }

    // ==================== 提取布尔值性能测试 ====================

    [Benchmark(Description = "优化前 - 提取布尔值")]
    public Dictionary<int, bool> Old_ExtractBooleanValues()
    {
        return _oldCodec.ExtractBooleanValues(_testData);
    }

    [Benchmark(Description = "优化后 - 提取布尔值")]
    public Dictionary<int, bool> New_ExtractBooleanValues()
    {
        return _newCodec.ExtractBooleanValues(_testData);
    }
}

// 测试协议定义 - 50 个字段
public struct TestProtocol
{
    // Int 字段 (10 个) - 40 字节
    [Address(100)] public int Field01 { get; set; }
    [Address(104)] public int Field02 { get; set; }
    [Address(108)] public int Field03 { get; set; }
    [Address(112)] public int Field04 { get; set; }
    [Address(116)] public int Field05 { get; set; }
    [Address(120)] public int Field06 { get; set; }
    [Address(124)] public int Field07 { get; set; }
    [Address(128)] public int Field08 { get; set; }
    [Address(132)] public int Field09 { get; set; }
    [Address(136)] public int Field10 { get; set; }
    
    // Float 字段 (10 个) - 40 字节
    [Address(140)] public float Field11 { get; set; }
    [Address(144)] public float Field12 { get; set; }
    [Address(148)] public float Field13 { get; set; }
    [Address(152)] public float Field14 { get; set; }
    [Address(156)] public float Field15 { get; set; }
    [Address(160)] public float Field16 { get; set; }
    [Address(164)] public float Field17 { get; set; }
    [Address(168)] public float Field18 { get; set; }
    [Address(172)] public float Field19 { get; set; }
    [Address(176)] public float Field20 { get; set; }
    
    // Bool 字段 (10 个) - 20 字节
    [Address(180)] public bool Field21 { get; set; }
    [Address(182)] public bool Field22 { get; set; }
    [Address(184)] public bool Field23 { get; set; }
    [Address(186)] public bool Field24 { get; set; }
    [Address(188)] public bool Field25 { get; set; }
    [Address(190)] public bool Field26 { get; set; }
    [Address(192)] public bool Field27 { get; set; }
    [Address(194)] public bool Field28 { get; set; }
    [Address(196)] public bool Field29 { get; set; }
    [Address(198)] public bool Field30 { get; set; }
    
    // Short 字段 (10 个) - 20 字节
    [Address(200)] public short Field31 { get; set; }
    [Address(202)] public short Field32 { get; set; }
    [Address(204)] public short Field33 { get; set; }
    [Address(206)] public short Field34 { get; set; }
    [Address(208)] public short Field35 { get; set; }
    [Address(210)] public short Field36 { get; set; }
    [Address(212)] public short Field37 { get; set; }
    [Address(214)] public short Field38 { get; set; }
    [Address(216)] public short Field39 { get; set; }
    [Address(218)] public short Field40 { get; set; }
    
    // Enum 字段 (5 个) - 10 字节
    [Address(220)] public DeviceStatus Field41 { get; set; }
    [Address(222)] public DeviceStatus Field42 { get; set; }
    [Address(224)] public DeviceStatus Field43 { get; set; }
    [Address(226)] public DeviceStatus Field44 { get; set; }
    [Address(228)] public DeviceStatus Field45 { get; set; }
    
    // UShort 字段 (5 个) - 10 字节
    [Address(230)] public ushort Field46 { get; set; }
    [Address(232)] public ushort Field47 { get; set; }
    [Address(234)] public ushort Field48 { get; set; }
    [Address(236)] public ushort Field49 { get; set; }
    [Address(238)] public ushort Field50 { get; set; }
    
    // 总计: 140 字节
}

public enum DeviceStatus : short
{
    Stopped = 0,
    Starting = 1,
    Running = 2,
    Stopping = 3,
    Error = 4
}

