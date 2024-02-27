#include <Windows.h>
#include <detours.h>
#include <set>
#include <string>
#include <iostream>
#include <fstream>

#ifdef _WIN64
#define BERSERIA
#else
#define ZESTIRIA
#endif

#ifdef BERSERIA
decltype(&GetCurrentDirectoryA) PEarlyFunction;
#else
decltype(&CreateMutexA) PEarlyFunction;
#endif

#ifdef ZESTIRIA
static void(__fastcall *Load)(const char*, const char*, void*);
#else
static void(*Load)(void*, const char*, const char*);
#endif

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

static void __fastcall AddNameHash(const char* name, const char* extension, void* dummy = nullptr)
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
}

#ifdef ZESTIRIA
__declspec(naked) static void __fastcall LoadDetour(const char* name, const char* extension, void* unknown)
#else
static void LoadDetour(void* this_, const char* name, const char* extension)
#endif
{
#ifdef ZESTIRIA
    __asm
    {
        pushad
        mov eax, [esp]
        push eax
        call AddNameHash
        popad
        jmp [Load]
    }
#else
    AddNameHash(name, extension);
    Load(this_, name, extension);
#endif
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
    DetourTransactionBegin();
    DetourAttach((void**)&Load, &LoadDetour);
    DetourTransactionCommit();
    LoadNames("name_db.txt");
    NameDB = std::ofstream("name_db.txt", std::ios_base::app);
}

#ifdef BERSERIA
DWORD WINAPI DEarlyFunction(DWORD nBufferLength, LPSTR lpBuffer)
#else
HANDLE WINAPI DEarlyFunction(LPSECURITY_ATTRIBUTES lpMutexAttributes, BOOL bInitialOwner, LPCSTR lpName)
#endif
{
    Initialize();
    DetourTransactionBegin();
    DetourDetach((void**)&PEarlyFunction, &DEarlyFunction);
    DetourTransactionCommit();
    DetourUpdateThread(GetCurrentThread());
#ifdef BERSERIA
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

#ifdef BERSERIA
    // RVAs based on Steam manifest
    // 0336651617463615849: 0x16F3DF0 (latest)
    // 7835388559349787992: 0x16D8560
    *(void**)&Load = (char*)GetModuleHandle(nullptr) + 0x16F3DF0;
#else // ZESTIRIA
    // RVAs based on Steam manifest
    // 3141087997518986971: 0x551130 (latest)
    *(void**)&Load = (char*)GetModuleHandle(nullptr) + 0x551130;
#endif

    // Hook a WinAPI function so we can run before the game, but after Denuvo
    DetourTransactionBegin();
#ifdef BERSERIA
    *(void**)&PEarlyFunction = GetProcAddress(GetModuleHandleA("KERNEL32"), "GetCurrentDirectoryA");
#else
    *(void**)&PEarlyFunction = GetProcAddress(GetModuleHandleA("KERNEL32"), "CreateMutexA");
#endif
    DetourAttach((void**)&PEarlyFunction, &DEarlyFunction);
    DetourTransactionCommit();
    return TRUE;
}
