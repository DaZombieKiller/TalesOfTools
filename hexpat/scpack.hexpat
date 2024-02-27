//
// Tales of Berseria
// SCPACK (SCPK) Format
//

#include <std/core.pat>
#include <std/mem.pat>

struct Script
{
    u32; // always 0
    u32; // always 0x10
    u32 ofs_data;
    u32 num_data;
    char name[]; // null terminated
    
    // I don't know if subtracting a constant 0x10 is correct.
    // It seems to be correct for all of the packages I've tried,
    // but the presence of the field above with a value of 0x10
    // makes me wonder if there is any connection.
    char data[num_data - 0x10] @ addressof(this) + ofs_data;
};

struct PackageEntry
{
    u32 name_hash;
    u32 offset;
    Script script @ addressof(this) + offset;
};

struct Package
{
    char magic[4];
    
    if (magic == "SCPK")
        std::core::set_endian(std::mem::Endian::Big); // PS3
    else
        std::core::set_endian(std::mem::Endian::Little);
    
    u32 version; // 100
    u32 ofs_entries;
    u32 num_entries;
    
    // entries should always be sorted by name_hash because the engine
    // performs a binary search on the entries array to find scripts.
    PackageEntry entries[num_entries] @ addressof(this) + ofs_entries;
};

Package package @ 0x00;
