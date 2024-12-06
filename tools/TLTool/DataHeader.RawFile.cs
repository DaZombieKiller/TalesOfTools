using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

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

        /// <summary>The hash of the file's name (including extension), computed with <see cref="TLHash"/>.</summary>
        public uint Hash;

        /// <summary>The file's extension.</summary>
        public FileExtensionBuffer ExtensionBuffer;

        /// <summary>The length of the file's extension in <see cref="ExtensionBuffer"/>.</summary>
        public byte ExtensionLength;

        /// <summary>Unknown. Most likely padding.</summary>
        public byte Unknown;

        /// <summary>Gets the file's extension as a read-only span of bytes.</summary>
        [UnscopedRef]
        public readonly ReadOnlySpan<byte> Extension
        {
            get
            {
                var length = int.Min(ExtensionLength, MaxExtensionLength);
                var buffer = ((ReadOnlySpan<byte>)ExtensionBuffer)[..length];
                return (length = buffer.IndexOf((byte)0)) >= 0 ? buffer[..length] : buffer;
            }
        }

        /// <summary>Initializes a new <see cref="RawFile"/> instance.</summary>
        public unsafe RawFile(BinaryReader reader)
        {
            Length = reader.ReadUInt64();
            CompressedLength = reader.ReadUInt64();
            Offset = reader.ReadUInt64();
            Hash = reader.ReadUInt32();
            reader.BaseStream.ReadExactly(ExtensionBuffer);
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
            writer.Write(ExtensionBuffer);
            writer.Write(ExtensionLength);
            writer.Write(Unknown);
        }

        /// <summary>Buffer structure for <see cref="ExtensionBuffer"/>.</summary>
        [InlineArray(MaxExtensionLength + 1)]
        public struct FileExtensionBuffer
        {
            private byte _e0;
        }
    }
}
