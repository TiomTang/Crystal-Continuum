#include <windows.h>
#include <filesystem>
#include <string>
#include <nethost.h>
#include <hostfxr.h>

namespace fs = std::filesystem;

static volatile struct {
    int64_t offset;
    unsigned char signature[32];
} bundle = {0, {0x8b,0x12,0x02,0xb9,0x6a,0x61,0x20,0x38,0x72,0x7b,0x93,0x02,0x14,0xd7,0xa0,0x32,
                 0x13,0xf5,0xb9,0xe6,0xef,0xae,0x33,0x18,0xee,0x3b,0x2d,0xce,0x24,0xb3,0x6a,0xae}};

static int run(const fs::path& self) {
    wchar_t library[32768];
    size_t length = _countof(library);
    if (get_hostfxr_path(library, &length, nullptr) != 0) return static_cast<int>(0x80008083);
    auto module = LoadLibraryExW(library, nullptr, LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | LOAD_LIBRARY_SEARCH_SYSTEM32);
    auto start = module ? reinterpret_cast<hostfxr_main_bundle_startupinfo_fn>(GetProcAddress(module, "hostfxr_main_bundle_startupinfo")) : nullptr;
    if (!start) return static_cast<int>(0x80008083);
    auto root = fs::path(library).parent_path().parent_path().parent_path().parent_path();
    auto app = self.parent_path() / L"Launcher.dll";
    return start(__argc, const_cast<const wchar_t**>(__wargv), self.c_str(), root.c_str(), app.c_str(), bundle.offset);
}

static DWORD install() {
    auto resource = FindResourceW(nullptr, MAKEINTRESOURCEW(101), RT_RCDATA);
    auto data = LockResource(LoadResource(nullptr, resource));
    auto size = SizeofResource(nullptr, resource);
    GUID guid;
    if (!data || !size || FAILED(CoCreateGuid(&guid))) return 1;
    wchar_t name[40];
    StringFromGUID2(guid, name, _countof(name));
    auto path = fs::temp_directory_path() / (std::wstring(name) + L".exe");
    auto file = CreateFileW(path.c_str(), GENERIC_WRITE, 0, nullptr, CREATE_NEW, FILE_ATTRIBUTE_NORMAL, nullptr);
    if (file == INVALID_HANDLE_VALUE) return 1;
    DWORD written = 0;
    bool saved = WriteFile(file, data, size, &written, nullptr) && written == size;
    CloseHandle(file);
    STARTUPINFOW startup = { sizeof(startup) };
    PROCESS_INFORMATION process = {};
    std::wstring command = L"\"" + path.wstring() + L"\" --ensure --progress";
    DWORD result = 1;
    if (saved && CreateProcessW(path.c_str(), command.data(), nullptr, nullptr, FALSE, CREATE_NO_WINDOW, nullptr, nullptr, &startup, &process)) {
        WaitForSingleObject(process.hProcess, INFINITE);
        GetExitCodeProcess(process.hProcess, &result);
        CloseHandle(process.hThread);
        CloseHandle(process.hProcess);
    }
    DeleteFileW(path.c_str());
    return result;
}

int WINAPI wWinMain(HINSTANCE, HINSTANCE, PWSTR, int) {
    try {
        wchar_t path[32768];
        DWORD length = GetModuleFileNameW(nullptr, path, _countof(path));
        if (!length || length == _countof(path)) return 1;
        int result = run(path);
        if (result != static_cast<int>(0x80008096) && result != static_cast<int>(0x80008083)) return result;
        DWORD status = install();
        if (!status) return run(path);
        auto message = L".NET 8 Desktop Runtime (x64) installation failed. Code: " + std::to_wstring(status);
        MessageBoxW(nullptr, message.c_str(), L"Launcher", MB_ICONERROR);
        return 1;
    } catch (const std::exception& error) {
        MessageBoxA(nullptr, error.what(), "Launcher", MB_ICONERROR);
        return 1;
    }
}
