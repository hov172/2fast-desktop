#include <mach-o/dyld.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <limits.h>
#include <errno.h>

#if defined(__arm64__)
#define RUNTIME_ARCH "arm64"
#elif defined(__x86_64__)
#define RUNTIME_ARCH "x64"
#else
#error Unsupported macOS architecture
#endif

int main(int argc, char **argv) {
    char executable[PATH_MAX], resolved[PATH_MAX], target[PATH_MAX];
    uint32_t size = sizeof(executable);
    if (_NSGetExecutablePath(executable, &size) != 0 || !realpath(executable, resolved)) {
        fputs("2fast: cannot locate the application bundle.\n", stderr);
        return 1;
    }
    char *separator = strrchr(resolved, '/');
    if (!separator) return 1;
    *separator = '\0';
    int length = snprintf(target, sizeof(target), "%s/../Helpers/%s/2fast.app/Contents/MacOS/Project2FA.Uno", resolved, RUNTIME_ARCH);
    if (length < 0 || length >= (int)sizeof(target) || access(target, X_OK) != 0) {
        fputs("2fast: the bundled runtime is missing. Reinstall the complete app.\n", stderr);
        return 1;
    }
    if (argc == 2 && strcmp(argv[1], "--print-runtime") == 0) {
        puts(RUNTIME_ARCH);
        return 0;
    }
    argv[0] = target;
    execv(target, argv);
    fprintf(stderr, "2fast: could not start the %s runtime (error %d).\n", RUNTIME_ARCH, errno);
    return 1;
}
