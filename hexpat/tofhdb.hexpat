//
// Tales of Berseria
// TOFHDB Format
//

#define BERSERIA
#define PC

#ifdef BERSERIA
using usize = u64;
#endif

#ifndef BERSERIA
// Xillia, Zestiria
using usize = u32;
#endif

#ifdef PC
#pragma endian little
#endif

#ifdef PS4
#pragma endian little
#endif

#ifdef PS3
#pragma endian big
#endif

namespace TL
{
    struct fixed_string<auto size, T>
    {
        T data[size];
        u8 length;
        u8;
    };
    
    struct ss_map_entry<TKey, TValue>
    {
        // name_hash
        TKey key;

        // index into files
        TValue value;
    };
    
    struct ss_map<TKey, TValue>
    {
        usize ofs_entries;
        usize num_entries;
        
        // sorted by key, in order to allow for a binary search to be performed
        ss_map_entry<TKey, TValue> entries[num_entries] @ addressof(ofs_entries) + ofs_entries;
    };
}

namespace TL::FileHeader
{
    struct SFileFinal
    {
        u64 size;
        u64 size_compressed;
        u64 offset;
        u32 name_hash;
        fixed_string<10, char> extension;
    };

    struct SFileHeaderFinal
    {
        u64; // related to FILEDATABASE.TLFDBX
        ss_map<u32, u32> entries;
        usize;
        usize;
        usize ofs_files;
        usize num_files;
        usize;
        usize;
        SFileFinal files[num_files] @ addressof(ofs_files) + ofs_files;
    }; 
}

TL::FileHeader::SFileHeaderFinal header @ 0;
