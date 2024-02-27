#include <Windows.h>
#include <detours.h>
#include <set>
#include <string>
#include <iostream>
#include <fstream>

decltype(&GetCurrentDirectoryA) PGetCurrentDirectoryA;

static void(*Load)(void*, const char*, const char*);

static CRITICAL_SECTION LoadSection;

static std::set<unsigned> NameHashes;

static std::ofstream NameDB;

static unsigned Append(unsigned hash, unsigned char b)
{
    return hash ^ (b + (hash << 6u) + (hash >> 2u) - 0x61C88647u);
}

static unsigned char ToUpper(unsigned char b)
{
    return (unsigned char)(b - 'a') < 0x1Au ? (unsigned char)(b - ' ') : b;
}

static unsigned GetHash(std::string s)
{
    unsigned hash = 0;

    for (unsigned char c : s)
        hash = Append(hash, ToUpper(c));

    return hash;
}

static unsigned GetNameHash(const char* name, const char* extension)
{
    unsigned hash = 0;
    std::string s = name;
    s += '.';
    s += extension;
    return GetHash(s);
}

static void LoadDetour(void* this_, const char* name, const char* extension)
{
    EnterCriticalSection(&LoadSection);
    unsigned hash = GetNameHash(name, extension);

    if (NameHashes.find(hash) == NameHashes.end())
    {
        NameHashes.insert(hash);
        NameDB << std::string(name);
        NameDB << '.';
        NameDB << std::string(extension);
        NameDB << '\n';
        NameDB.flush();
    }

    LeaveCriticalSection(&LoadSection);
    Load(this_, name, extension);
}

static void LoadNames(std::string path)
{
    std::ifstream stream(path);
    
    for (std::string line; std::getline(stream, line);)
    {
        NameHashes.insert(GetHash(line));
    }
}

static void Initialize()
{
    LoadNames("name_db.txt");
    NameDB = std::ofstream("name_db.txt", std::ios_base::app);
    DetourTransactionBegin();
    DetourAttach((void**)&Load, &LoadDetour);
    DetourTransactionCommit();
}

DWORD WINAPI DGetCurrentDirectoryA(DWORD nBufferLength, LPSTR lpBuffer)
{
    Initialize();
    DetourTransactionBegin();
    DetourDetach((void**)&PGetCurrentDirectoryA, &DGetCurrentDirectoryA);
    DetourTransactionCommit();
    return GetCurrentDirectoryA(nBufferLength, lpBuffer);
}

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
    if (fdwReason != DLL_PROCESS_ATTACH)
        return TRUE;

    DisableThreadLibraryCalls(hinstDLL);
    InitializeCriticalSection(&LoadSection);

    // Open a debug console
    AllocConsole();
    (void)freopen("CONOUT$", "w", stdout);
    (void)freopen("CONOUT$", "w", stderr);
    
    // RVAs based on Steam manifest
    // 0336651617463615849: 0x16F3DF0 (latest)
    // 7835388559349787992: 0x16D8560
    *(void**)&Load = (char*)GetModuleHandle(nullptr) + 0x16F3DF0;

    // Hook GetCurrentDirectoryA so we can run before the game, but after Denuvo
    *(void**)&PGetCurrentDirectoryA = GetProcAddress(GetModuleHandleA("KERNEL32"), "GetCurrentDirectoryA");
    DetourTransactionBegin();
    DetourAttach((void**)&PGetCurrentDirectoryA, &DGetCurrentDirectoryA);
    DetourTransactionCommit();
    return TRUE;
}
