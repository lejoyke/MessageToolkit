using MessageToolkit;
using MessageToolkit.Abstractions;
using MessageToolkit.Attributes;
using MessageToolkit.Models;
using Xunit;

namespace MessageToolkit.Tests;

/// <summary>
/// 原生协议测试 - 测试 NativeProtocolCodec 和 NativeFrameBuilder
/// </summary>
public sealed class NativeProtocolTests
{
    private readonly IProtocolSchema<IOProtocol> _schema;
    private readonly NativeProtocolCodec<IOProtocol, bool> _codec;
    private readonly NativeFrameBuilder<IOProtocol, bool> _builder;

    public NativeProtocolTests()
    {
        _schema = new ProtocolSchema<IOProtocol>(BooleanRepresentation.Boolean, Endianness.BigEndian);
        _codec = new NativeProtocolCodec<IOProtocol, bool>(_schema);
        _builder = new NativeFrameBuilder<IOProtocol, bool>(_schema, _codec);
    }

    [Fact]
    public void Schema_Should_Parse_Boolean_Fields()
    {
        Assert.Equal(0, _schema.StartAddress);
        Assert.Equal(10, _schema.TotalSize);
        Assert.Equal(4, _schema.Properties.Count);

        var field = _schema.GetFieldInfo(nameof(IOProtocol.DO0));
        Assert.Equal(8, field.Address);
        Assert.Equal(typeof(bool), field.FieldType);
    }

    [Fact]
    public void Codec_Should_Encode_Without_TypeConversion()
    {
        var protocol = new IOProtocol
        {
            DI0 = true,
            DI1 = false,
            DO0 = true,
            DO1 = false
        };

        var data = _codec.Encode(protocol);
        
        // 原生协议直接映射，无类型转换
        Assert.Equal(10, data.Length);
        Assert.True(data[0]);   // DI0 at address 0
        Assert.False(data[1]);  // DI1 at address 1
        Assert.True(data[8]);   // DO0 at address 8
        Assert.False(data[9]);  // DO1 at address 9
    }

    [Fact]
    public void Codec_Should_Decode_Without_TypeConversion()
    {
        var data = new bool[10];
        data[0] = true;   // DI0
        data[1] = true;   // DI1
        data[8] = false;  // DO0
        data[9] = true;   // DO1

        var protocol = _codec.Decode(data);
        
        Assert.True(protocol.DI0);
        Assert.True(protocol.DI1);
        Assert.False(protocol.DO0);
        Assert.True(protocol.DO1);
    }

    [Fact]
    public void FrameBuilder_Should_Build_WriteFrame_For_Protocol()
    {
        var protocol = new IOProtocol { DI0 = true, DI1 = false, DO0 = true, DO1 = true };

        var frame = _builder.BuildWriteFrame(protocol);
        
        Assert.Equal(0, frame.StartAddress);
        Assert.Equal(10, frame.DataLength);
    }

    [Fact]
    public void FrameBuilder_Should_Build_WriteFrame_For_SingleField()
    {
        var frame = _builder.BuildWriteFrame(p => p.DO0, true);
        
        Assert.Equal(8, frame.StartAddress);
        Assert.Equal(1, frame.DataLength);
        Assert.True(frame.ToArray()[0]);
    }

    [Fact]
    public void FrameBuilder_Should_Build_ReadRequest()
    {
        var request = _builder.BuildReadRequest();
        
        Assert.Equal(0, request.StartAddress);
        Assert.Equal(10, request.Count);
    }

    [Fact]
    public void FrameBuilder_Should_Build_ReadRequest_For_Field()
    {
        var request = _builder.BuildReadRequest(p => p.DO1);
        
        Assert.Equal(9, request.StartAddress);
        Assert.Equal(1, request.Count);
    }

    [Fact]
    public void DataMapping_Should_Build_Frames()
    {
        var mapping = _builder.CreateDataMapping();
        
        var frames = mapping
            .Property(p => p.DO0, true)
            .Property(p => p.DO1, false)
            .Build()
            .ToArray();

        Assert.Equal(2, frames.Length);
    }

    [Fact]
    public void DataMapping_Should_Optimize_ContiguousAddresses()
    {
        var mapping = _builder.CreateDataMapping();
        
        var frames = mapping
            .Property(p => p.DO0, true)   // address 8
            .Property(p => p.DO1, false)  // address 9
            .BuildOptimized()
            .ToArray();

        // 连续地址应合并为单个帧
        Assert.Single(frames);
        Assert.Equal(8, frames[0].StartAddress);
        Assert.Equal(2, frames[0].DataLength);
    }

    [Fact]
    public void DataMapping_Should_Not_Optimize_NonContiguousAddresses()
    {
        var mapping = _builder.CreateDataMapping();
        
        var frames = mapping
            .Property(p => p.DI0, true)   // address 0
            .Property(p => p.DO0, true)   // address 8 (not contiguous)
            .BuildOptimized()
            .ToArray();

        Assert.Equal(2, frames.Length);
        Assert.Equal(0, frames[0].StartAddress);
        Assert.Equal(8, frames[1].StartAddress);
    }

    [Fact]
    public void NativeWriteFrame_Should_Support_SingleValue()
    {
        var frame = new NativeWriteFrame<bool>(5, true);
        
        Assert.Equal(5, frame.StartAddress);
        Assert.Equal(1, frame.DataLength);
        Assert.True(frame.ToArray()[0]);
    }

    [Fact]
    public void NativeWriteFrame_Should_Support_ArrayValue()
    {
        var frame = new NativeWriteFrame<bool>(0, new[] { true, false, true });
        
        Assert.Equal(0, frame.StartAddress);
        Assert.Equal(3, frame.DataLength);
        var data = frame.ToArray();
        Assert.True(data[0]);
        Assert.False(data[1]);
        Assert.True(data[2]);
    }

    private struct IOProtocol
    {
        [Address(0)] public bool DI0 { get; set; }
        [Address(1)] public bool DI1 { get; set; }
        [Address(8)] public bool DO0 { get; set; }
        [Address(9)] public bool DO1 { get; set; }
    }
}
