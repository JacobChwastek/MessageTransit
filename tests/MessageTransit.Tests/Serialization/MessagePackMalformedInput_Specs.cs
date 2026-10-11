namespace MessageTransit.Tests.Serialization;

using System;
using System.Buffers;
using MessagePack;
using MessageTransit.Serialization;
using NUnit.Framework;


[TestFixture]
public class Deserializing_malformed_MessagePack_input
{
    const int NestingBeyondMaximumDepth = 2_000;
    const int NestingBeyondStackDepth = 1_000_000;

    readonly MessagePackMessageSerializer _serializer = new();

    [Test]
    public void Should_read_a_well_formed_interface_message()
    {
        var context = DeserializeEnvelope(WriteEnvelope(message: WriteComplaint()));

        Assert.That(context.TryGetMessage(out ComplaintAdded message), Is.True);
        Assert.That(message.Body, Is.EqualTo("Body"));
    }

    [Test]
    public void Should_reject_a_truncated_envelope()
    {
        var envelope = WriteEnvelope(message: WriteComplaint());

        Assert.That(() => DeserializeEnvelope(envelope.AsSpan(0, envelope.Length / 2).ToArray()),
            Throws.InstanceOf<MessagePackSerializationException>());
    }

    [Test]
    public void Should_reject_deeply_nested_header_values()
    {
        var envelope = WriteEnvelope(message: WriteComplaint(), writeExtraField: (ref MessagePackWriter writer) =>
        {
            writer.Write("Headers");
            writer.WriteMapHeader(1);
            writer.Write("Nested");
            WriteNestedArrays(ref writer, NestingBeyondMaximumDepth);
        });

        Assert.That(() => DeserializeEnvelope(envelope), Throws.InstanceOf<MessagePackSerializationException>());
    }

    [Test]
    public void Should_skip_deeply_nested_unknown_envelope_fields()
    {
        var envelope = WriteEnvelope(message: WriteComplaint(), writeExtraField: (ref MessagePackWriter writer) =>
        {
            writer.Write("Unknown");
            WriteNestedArrays(ref writer, NestingBeyondStackDepth);
        });

        Assert.That(DeserializeEnvelope(envelope).TryGetMessage(out ComplaintAdded _), Is.True);
    }

    [Test]
    public void Should_reject_a_date_time_extension_with_an_invalid_length()
    {
        var envelope = WriteEnvelope(message: WriteComplaint(), writeExtraField: (ref MessagePackWriter writer) =>
        {
            writer.Write("SentTime");
            // ext 32 header declaring 0x7FFFFFF0 bytes of DateTime (-1) data, followed by only four bytes
            writer.WriteRaw([MessagePackCode.Ext32, 0x7F, 0xFF, 0xFF, 0xF0, 0xFF, 1, 2, 3, 4]);
        });

        Assert.That(() => DeserializeEnvelope(envelope), Throws.InstanceOf<MessagePackSerializationException>());
    }

    [Test]
    public void Should_skip_deeply_nested_unknown_fields_in_an_interface_message()
    {
        var context = DeserializeEnvelope(WriteEnvelope(message: WriteComplaint(writeExtraField: (ref MessagePackWriter writer) =>
        {
            writer.Write("Unknown");
            WriteNestedArrays(ref writer, NestingBeyondStackDepth);
        })));

        Assert.That(context.TryGetMessage(out ComplaintAdded message), Is.True);
        Assert.That(message.Body, Is.EqualTo("Body"));
    }

    [Test]
    public void Should_not_return_an_interface_message_from_a_truncated_body()
    {
        var message = WriteComplaint();

        var context = DeserializeEnvelope(WriteEnvelope(message: message.AsSpan(0, message.Length - 2).ToArray()));

        Assert.That(context.TryGetMessage(out ComplaintAdded _), Is.False);
    }

    SerializerContext DeserializeEnvelope(byte[] envelope)
    {
        return _serializer.Deserialize(new BytesMessageBody(envelope), EmptyHeaders.Instance);
    }

    static byte[] WriteEnvelope(byte[] message, WriteField writeExtraField = null)
    {
        return Write((ref MessagePackWriter writer) =>
        {
            writer.WriteMapHeader(writeExtraField is null ? 4 : 5);
            writer.Write(nameof(MessagePackEnvelope.MessageId));
            writer.Write(NewId.NextGuid().ToString());
            writer.Write(nameof(MessagePackEnvelope.MessageType));
            writer.WriteArrayHeader(1);
            writer.Write(MessageUrn.ForTypeString<ComplaintAdded>());
            writer.Write(nameof(MessagePackEnvelope.IsMessageNativeMessagePackSerialized));
            writer.Write(true);
            writer.Write(nameof(MessagePackEnvelope.Message));
            writer.Write(message);

            writeExtraField?.Invoke(ref writer);
        });
    }

    static byte[] WriteComplaint(WriteField writeExtraField = null)
    {
        return Write((ref MessagePackWriter writer) =>
        {
            writer.WriteMapHeader(writeExtraField is null ? 1 : 2);
            writer.Write(nameof(ComplaintAdded.Body));
            writer.Write("Body");

            writeExtraField?.Invoke(ref writer);
        });
    }

    static void WriteNestedArrays(ref MessagePackWriter writer, int depth)
    {
        for (var i = 0; i < depth; i++)
            writer.WriteArrayHeader(1);

        writer.WriteNil();
    }

    static byte[] Write(WriteField write)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);

        write(ref writer);
        writer.Flush();

        return buffer.WrittenSpan.ToArray();
    }


    delegate void WriteField(ref MessagePackWriter writer);
}
