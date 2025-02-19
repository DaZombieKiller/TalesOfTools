import type.base;
import type.size;
import type.magic;

bitfield u40 { v : 40; } [[static, format("u40_v")]];
fn u40_v(u40 v) { return v.v; };

enum ZArcStringEncodeType : u32
{
    Ascii = 1,
};

enum ZArcStringCaseType : u32
{
    None,
    Lower,
    Upper,
};

struct ZArchiveContent
{
    type::Hex<u64> HashId;
    u40 UncSize;
    type::Hex<u32>;
    u24 LzmaBlockIndex;
    u32 OffsetInContentAlignBlocks;
} [[static]];

struct ZArchiveHeader
{
    type::Magic<"ZARC">;
    u32 Version;
    u32 EndOfHeader;
    type::Size32 ContentSize;
    u32 ContentCount;
    u32;
    ZArcStringEncodeType PathEncoding;
    type::Size32 LzmaBlockAlign;
    type::Size32 ContentAlign;
    ZArcStringCaseType PathCaseConversion;
    ZArchiveContent Content[ContentCount];
    
    if (BlockAlign > 16777216) {
        u32 LzmaBlockSizes[(EndOfHeader - $) / 4];
    } else if (BlockAlign > 65536) {
        u24 LzmaBlockSizes[(EndOfHeader - $) / 3];
    } else if (BlockAlign > 256) {
        u16 LzmaBlockSizes[(EndOfHeader - $) / 2];
    } else {
        u8 LzmaBlockSizes[(EndOfHeader - $)];
    }
} [[static]];

be ZArchiveHeader Archive @ 0 [[inline]];
