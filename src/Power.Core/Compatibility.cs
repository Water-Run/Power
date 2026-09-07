// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

#if NETSTANDARD2_1
namespace System.Runtime.CompilerServices
{
    // Compiler marker only; the Unity build uses ordinary CLR-compatible IL.
    internal static class IsExternalInit { }
}
#endif
