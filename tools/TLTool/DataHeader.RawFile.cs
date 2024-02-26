using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TLTool;

public sealed partial class DataHeader
{
    /// <summary>Data structure describing the raw layout of a file entry in a <see cref="DataHeader"/>.</summary>
    private struct RawFile
    {
        /// <summary>The maximum length of a file extension.</summary>
        public const ushort MaxExtensionLength = 9;

        /// <summary>The uncompressed length of the file, in bytes.</summary>
        public ulong Length;

        /// <summary>The compressed length of the file, in bytes.</summary>
        public ulong CompressedLength;

        /// <summary>The offset of the file within the TLFILE data.</summary>
        public ulong Offset;

        /// <summary>The hash of the file's name (including extension), computed with <see cref="NameHash"/>.</summary>
        public uint Hash;

        /// <summary>The file's extension.</summary>
        public unsafe fixed byte ExtensionBuffer[MaxExtensionLength + 1];

        /// <summary>The length of the file's extension in <see cref="ExtensionBuffer"/>.</summary>
        public byte ExtensionLength;

        /// <summary>Unknown. Most likely padding.</summary>
        public byte Unknown;

        /// <summary>Gets the file's extension as a read-only span of bytes.</summary>
        public unsafe readonly ReadOnlySpan<byte> Extension
        {
            get
            {
                ref var r0 = ref Unsafe.AsRef(in ExtensionBuffer[0]);
                var length = int.Min(ExtensionLength, MaxExtensionLength);
                return MemoryMarshal.CreateReadOnlySpan(ref r0, length);
            }
        }

        /// <summary>Initializes a new <see cref="RawFile"/> instance.</summary>
        public unsafe RawFile(BinaryReader reader)
        {
            Length = reader.ReadUInt64();
            CompressedLength = reader.ReadUInt64();
            Offset = reader.ReadUInt64();
            Hash = reader.ReadUInt32();

            fixed (byte* pExtension = ExtensionBuffer)
                reader.BaseStream.ReadExactly(new Span<byte>(pExtension, MaxExtensionLength + 1));

            ExtensionLength = reader.ReadByte();
            Unknown = reader.ReadByte();
        }

        /// <summary>Writes this <see cref="RawFile"/> to the specified writer.</summary>
        public unsafe readonly void Write(BinaryWriter writer)
        {
            writer.Write(Length);
            writer.Write(CompressedLength);
            writer.Write(Offset);
            writer.Write(Hash);

            fixed (byte* pExtension = ExtensionBuffer)
                writer.Write(new ReadOnlySpan<byte>(pExtension, MaxExtensionLength + 1));

            writer.Write(ExtensionLength);
            writer.Write(Unknown);
        }
    }
}
