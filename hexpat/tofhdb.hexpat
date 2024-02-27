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

struct File
{
    u64 size;
    u64 size_compressed;
    u64 offset;
    u32 name_hash;
    char extension[10];
    u8 extension_len;
    padding[1];
};

struct FileHash
{
    u32 hash; // sorting key
    u32 file_index;
};

struct FileHeader
{
    u64 creation_time;
    usize ofs_file_hashes;
    usize num_file_hashes;
    usize ofs_virtual_hashes;
    usize num_virtual_hashes;
    usize ofs_files;
    usize num_files;
    usize ofs_virtual_packs;
    usize num_virtual_packs;
    File files[num_files] @ addressof(ofs_files) + ofs_files;
    FileHash file_hashes[num_file_hashes] @ addressof(ofs_file_hashes) + ofs_file_hashes;
}; 

FileHeader header @ 0;
