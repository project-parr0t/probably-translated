import ctypes
import os
import sys
from pathlib import Path


def main() -> int:
    if len(sys.argv) not in (5, 6):
        raise SystemExit("usage: run.py GAME_ROOT TEST_DLL MOD_DLL RUNTIME_HARMONY_DLL [BETTER_LIQUID_SCANNER_DLL]")
    game, test, mod, harmony, *optional = (Path(arg).resolve(strict=True) for arg in sys.argv[1:])
    managed = game / "Probably Stolen_Data" / "Managed"
    mono = game / "MonoBleedingEdge"
    embed = mono / "EmbedRuntime"
    # Use the game's Mono, not CoreCLR: the BepInEx HarmonyX build expects Mono.
    with os.add_dll_directory(str(embed)):
        runtime = ctypes.CDLL(str(embed / "mono-2.0-bdwgc.dll"))
        runtime.mono_set_dirs.argtypes = [ctypes.c_char_p, ctypes.c_char_p]
        runtime.mono_set_assemblies_path.argtypes = [ctypes.c_char_p]
        runtime.mono_jit_init_version.argtypes = [ctypes.c_char_p, ctypes.c_char_p]
        runtime.mono_jit_init_version.restype = ctypes.c_void_p
        runtime.mono_domain_assembly_open.argtypes = [ctypes.c_void_p, ctypes.c_char_p]
        runtime.mono_domain_assembly_open.restype = ctypes.c_void_p
        runtime.mono_jit_exec.argtypes = [ctypes.c_void_p, ctypes.c_void_p, ctypes.c_int, ctypes.POINTER(ctypes.c_char_p)]
        runtime.mono_jit_exec.restype = ctypes.c_int
        runtime.mono_set_dirs(os.fsencode(managed), os.fsencode(mono / "etc"))
        runtime.mono_set_assemblies_path(os.fsencode(os.pathsep.join(map(str, (harmony.parent, managed)))))
        domain = runtime.mono_jit_init_version(b"HarmonyCompatibility", b"v4.0.30319")
        if not domain:
            raise RuntimeError("Could not initialize Unity Mono")
        assembly = runtime.mono_domain_assembly_open(domain, os.fsencode(test))
        if not assembly:
            raise RuntimeError(f"Could not load {test}")
        paths = (test, mod, managed, harmony, *optional)
        args = (ctypes.c_char_p * len(paths))(*(os.fsencode(path) for path in paths))
        return runtime.mono_jit_exec(domain, assembly, len(args), args)


if __name__ == "__main__":
    raise SystemExit(main())
