using System.Buffers.Binary;

namespace AgentContextOS.Services;

/// <summary>
/// Converts between float[] and the little-endian byte[] BLOB format required by sqlite-vec.
/// </summary>
public static class VectorHelper
{
    public static byte[] ToBlob(ReadOnlyMemory<float> vector)
    {
        var span = vector.Span;
        var bytes = new byte[span.Length * sizeof(float)];
        for (var i = 0; i < span.Length; i++)
            BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(i * sizeof(float)), span[i]);
        return bytes;
    }

    public static float[] FromBlob(byte[] blob)
    {
        var count = blob.Length / sizeof(float);
        var result = new float[count];
        for (var i = 0; i < count; i++)
            result[i] = BinaryPrimitives.ReadSingleLittleEndian(blob.AsSpan(i * sizeof(float)));
        return result;
    }
}
