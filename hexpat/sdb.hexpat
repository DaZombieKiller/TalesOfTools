//
// Tales Of String Dictionary (.SDB)
// This pattern describes the format used in Tales of Zestiria and Tales of Berseria.
// Tales of Xillia and prior games are not compatible with the format described here.
//

import std.ptr;
import std.string;
using NullString = std::string::NullString;
using usize = u64; // u32 for PS3 and Zestiria

struct Entry
{
    NullString *mKey : usize [[pointer_base("std::ptr::relative_to_pointer")]];
    NullString *mValue : usize [[pointer_base("std::ptr::relative_to_pointer")]];
};

struct Bucket
{
    Entry *mEntry : usize [[inline, pointer_base("std::ptr::relative_to_pointer")]];
};

struct Dictionary
{
    Bucket mBuckets[parent.mBucketCount];
    char *mData : usize [[pointer_base("std::ptr::relative_to_pointer")]];
    Entry mEntries[parent.mEntryCount];
};

struct Header
{
    usize mMagic;
    usize mOffset;
    usize mEntryCount;
    usize mBucketCount; // zero or a prime number
    Dictionary mDictionary @ addressof(mOffset) + mOffset;
};

// This is the list of prime numbers used for mBucketCount:
// 17, 29, 37, 53, 67, 79, 97, 131, 193, 257, 389, 521,
// 769, 1031, 1543, 2053, 3079, 6151, 12289, 24593, 49157,
// 98317, 196613, 393241, 786433, 1572869, 3145739, 6291469,
// 12582917, 25165843, 50331653, 100663319, 201326611,
// 402653189, 805306457, 1610612741, 3221225473, 4294967291

// Given a key string, compute its case-sensitive TLHash.
// Begin iterating mEntries starting at mBuckets[hash % mBucketCount].mEntry.
// The search is over when the entry's key matches the input key or you reach
// the end of the mEntries array.

Header mHeader @ 0;
