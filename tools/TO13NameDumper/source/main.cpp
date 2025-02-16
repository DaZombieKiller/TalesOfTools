#include <Windows.h>
#include <detours.h>
#include <set>
#include <string>
#include <iostream>
#include <fstream>
#include <string_view>

#ifdef _WIN64
#define GRACESFR 1
#define BERSERIA 0
#define ZESTIRIA 0 // NONE
#else
#define ZESTIRIA 1
#define GRACESFR 0 // NONE
#define BERSERIA 0 // NONE
#endif

#if GRACESFR
typedef uint64_t hash_t;
#else
typedef uint32_t hash_t;
#endif

#if GRACESFR
decltype(&GetCommandLineA) PEarlyFunction;
#elif BERSERIA
decltype(&GetCurrentDirectoryA) PEarlyFunction;
#else
decltype(&CreateMutexA) PEarlyFunction;
#endif

#if ZESTIRIA
static void(__fastcall *Load)(const char*, const char*, void*);
#elif BERSERIA
static void(*Load)(void*, const char*, const char*);
static void(*Printf)(const char*, ...);
#elif GRACESFR
static uint32_t(*ComputeCheckSum64)(const char*, uint32_t, uint32_t);
static uint64_t(*MakeHashId)(const char*, int, int);
#endif

static CRITICAL_SECTION LoadSection;

static std::set<hash_t> NameHashes;

static std::ofstream NameDB;

#if GRACESFR
static hash_t ComputeHash(std::string_view s, int caseConvert = 0)
{
    uint32_t upper = 0xFFFFFFFF;
    uint32_t lower = 0xFFFFFFFF;

    for (uint8_t b : s)
    {
        if (caseConvert == 1)
            b = tolower(b);
        else if (caseConvert == 2)
            b = toupper(b);

        upper ^= b;
        lower ^= b;

        for (int i = 0; i < 8; i++)
        {
            lower = ((uint32_t)((int32_t)(lower << 31) >> 31) & 0x56811021) ^ (lower >> 1);
            upper = ((uint32_t)((int32_t)(upper << 31) >> 31) & 0x10215681) ^ (upper >> 1);
        }
    }

    upper = ~upper;
    lower = ~lower;
    return ((uint64_t)upper << 32) | (uint64_t)lower;
}
#else
static hash_t ComputeHash(std::string_view s)
{
    unsigned hash = 0;

    for (unsigned char c : s)
        hash ^= toupper(c) + ((hash << 6u) + (hash >> 2u) - 0x61C88647u);

    return hash;
}
#endif

static void AddToNameDB(hash_t hash, std::string_view name)
{
    EnterCriticalSection(&LoadSection);

    if (NameHashes.find(hash) == NameHashes.end())
    {
        NameHashes.insert(hash);
        NameDB << name;
        NameDB << '\n';
        NameDB.flush();
    }

    LeaveCriticalSection(&LoadSection);
}

#if ZESTIRIA || BERSERIA
static hash_t GetNameHash(const char* name, const char* extension)
{
    hash_t hash = 0;
    std::string s = name;
    s += '.';
    s += extension;
    return ComputeHash(s);
}

static void __fastcall AddNameHash(const char* name, const char* extension, void* dummy = nullptr)
{
    hash_t hash = GetNameHash(name, extension);
    AddToNameDB(hash, std::string(name) + std::string(extension));
}

#if ZESTIRIA
__declspec(naked) static void __fastcall LoadDetour(const char* name, const char* extension, void* unknown)
#elif BERSERIA
static void LoadDetour(void* this_, const char* name, const char* extension)
#endif
{
#if ZESTIRIA
    __asm
    {
        pushad
        mov eax, [esp]
        push eax
        call AddNameHash
        popad
        jmp [Load]
    }
#elif BERSERIA
    AddNameHash(name, extension);
    Load(this_, name, extension);
#endif
}
#endif

#if GRACESFR
static uint32_t ComputeCheckSum64Detour(const char* name, uint32_t length, uint32_t mask)
{
    AddToNameDB(ComputeHash(std::string_view(name, length)), std::string_view(name, length));
    return ComputeCheckSum64(name, length, mask);
}

static uint64_t MakeHashIdDetour(const char* name, int caseConversion, int pathEncoding)
{
    size_t length = strlen(name);
    AddToNameDB(ComputeHash(std::string_view(name, length), caseConversion), std::string_view(name, length));
    return MakeHashId(name, caseConversion, pathEncoding);
}
#endif

static void LoadNames(std::string path)
{
    std::ifstream stream(path);
    
    for (std::string line; std::getline(stream, line);)
    {
        NameHashes.insert(ComputeHash(line));
    }
}

static void Initialize()
{
#if GRACESFR
    HMODULE hGameNative = LoadLibraryA("Tales of Graces f Remastered_Data\\Plugins\\x86_64\\GameNative.dll");

    // RVAs based on Steam manifest
    // 5905203285723701306: 0x85CB0 (latest)
    // 6693400166831520548:
    *(void**)&ComputeCheckSum64 = (char*)hGameNative + 0x85CB0;

    // RVAs based on Steam manifest
    // 5905203285723701306: 0x85D70 (latest)
    // 6693400166831520548:
    *(void**)&MakeHashId = (char*)hGameNative + 0x85D70;
#elif BERSERIA
    // RVAs based on Steam manifest
    // 0336651617463615849: 0x16F3DF0 (latest)
    // 7835388559349787992: 0x16D8560
    * (void**)&Load = (char*)GetModuleHandle(nullptr) + 0x16F3DF0;

    // TL::Printf
    // 0336651617463615849: 0x1392C10 (latest)
    // 7835388559349787992: 0x12FE960
    *(void**)&Printf = (char*)GetModuleHandle(nullptr) + 0x1392C10;
#elif ZESTIRIA
    // RVAs based on Steam manifest
    // 3141087997518986971: 0x551130 (latest)
    * (void**)&Load = (char*)GetModuleHandle(nullptr) + 0x551130;
#endif

    DetourTransactionBegin();
#if BERSERIA || ZESTIRIA
    DetourAttach((void**)&Load, &LoadDetour);
#endif
#if BERSERIA
    DetourAttach((void**)&Printf, std::printf);
#endif
#if GRACESFR
    DetourAttach((void**)&ComputeCheckSum64, ComputeCheckSum64Detour);
    DetourAttach((void**)&MakeHashId, MakeHashIdDetour);
#endif
    DetourTransactionCommit();
    LoadNames("name_db.txt");
    NameDB = std::ofstream("name_db.txt", std::ios_base::app);
}

#if GRACESFR
LPSTR WINAPI DEarlyFunction()
#elif BERSERIA
DWORD WINAPI DEarlyFunction(DWORD nBufferLength, LPSTR lpBuffer)
#else
HANDLE WINAPI DEarlyFunction(LPSECURITY_ATTRIBUTES lpMutexAttributes, BOOL bInitialOwner, LPCSTR lpName)
#endif
{
    DetourTransactionBegin();
    DetourDetach((void**)&PEarlyFunction, &DEarlyFunction);
    DetourTransactionCommit();
    DetourUpdateThread(GetCurrentThread());
    Initialize();
#if GRACESFR
    return GetCommandLineA();
#elif BERSERIA
    return GetCurrentDirectoryA(nBufferLength, lpBuffer);
#else
    return CreateMutexA(lpMutexAttributes, bInitialOwner, lpName);
#endif
}

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
    if (fdwReason != DLL_PROCESS_ATTACH)
        return TRUE;

    //DisableThreadLibraryCalls(hinstDLL);
    InitializeCriticalSection(&LoadSection);

    // Open a debug console
    AllocConsole();
    (void)freopen("CONOUT$", "w", stdout);
    (void)freopen("CONOUT$", "w", stderr);

    // Hook a WinAPI function so we can run before the game, but after Denuvo
    DetourTransactionBegin();
#if GRACESFR
    *(void**)&PEarlyFunction = GetProcAddress(GetModuleHandleA("KERNEL32"), "GetCommandLineA");
#elif BERSERIA
    *(void**)&PEarlyFunction = GetProcAddress(GetModuleHandleA("KERNEL32"), "GetCurrentDirectoryA");
#else
    *(void**)&PEarlyFunction = GetProcAddress(GetModuleHandleA("KERNEL32"), "CreateMutexA");
#endif
    DetourAttach((void**)&PEarlyFunction, &DEarlyFunction);
    DetourTransactionCommit();
    return TRUE;
}
