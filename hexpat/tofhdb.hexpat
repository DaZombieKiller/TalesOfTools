//
// Tales of Berseria
// TOFHDB Format
//

namespace TL
{
    struct fixed_string<auto size, T>
    {
        T data[size];
        u16 length;
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
        u64 ofs_entries;
        u64 num_entries;
        
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
        u64 unk1;
        u64 unk2;
        u64 ofs_files;
        u64 num_files;
        u64 unk3;
        u64 unk4;
        SFileFinal files[num_files] @ addressof(ofs_files) + ofs_files;
    }; 
}

TL::FileHeader::SFileHeaderFinal header @ 0;
