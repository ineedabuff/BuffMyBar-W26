using System;
using System.Runtime.InteropServices;

namespace BuffBar.Interop;

/// <summary>
/// Chiffrement au repos via DPAPI (Data Protection API) de Windows, lié au compte
/// utilisateur courant. Implémenté en P/Invoke direct sur crypt32.dll afin de rester
/// 100 % natif : aucune dépendance NuGet (le type managé <c>ProtectedData</c> exigerait
/// le paquet System.Security.Cryptography.ProtectedData sur .NET 8).
///
/// Le secret n'est déchiffrable que par le même utilisateur Windows sur la même machine.
/// </summary>
internal static class Dpapi
{
    [StructLayout(LayoutKind.Sequential)]
    private struct DATA_BLOB
    {
        public int cbData;
        public IntPtr pbData;
    }

    // CRYPTPROTECT_UI_FORBIDDEN : pas d'invite interactive (service / arrière-plan).
    private const int CryptProtectUiForbidden = 0x1;

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptProtectData(
        ref DATA_BLOB pDataIn, string? szDataDescr, IntPtr pOptionalEntropy,
        IntPtr pvReserved, IntPtr pPromptStruct, int dwFlags, ref DATA_BLOB pDataOut);

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptUnprotectData(
        ref DATA_BLOB pDataIn, IntPtr ppszDataDescr, IntPtr pOptionalEntropy,
        IntPtr pvReserved, IntPtr pPromptStruct, int dwFlags, ref DATA_BLOB pDataOut);

    /// <summary>Chiffre <paramref name="plain"/> pour l'utilisateur courant. Renvoie null en cas d'échec.</summary>
    public static byte[]? Protect(byte[] plain)
    {
        var inBlob = default(DATA_BLOB);
        var outBlob = default(DATA_BLOB);
        try
        {
            inBlob.pbData = Marshal.AllocHGlobal(plain.Length);
            inBlob.cbData = plain.Length;
            Marshal.Copy(plain, 0, inBlob.pbData, plain.Length);

            if (!CryptProtectData(ref inBlob, "BuffBar", IntPtr.Zero, IntPtr.Zero,
                                  IntPtr.Zero, CryptProtectUiForbidden, ref outBlob))
                return null;

            var result = new byte[outBlob.cbData];
            Marshal.Copy(outBlob.pbData, result, 0, outBlob.cbData);
            return result;
        }
        catch { return null; }
        finally
        {
            if (inBlob.pbData != IntPtr.Zero) Marshal.FreeHGlobal(inBlob.pbData);
            if (outBlob.pbData != IntPtr.Zero) NativeFree(outBlob.pbData);
        }
    }

    /// <summary>Déchiffre des données produites par <see cref="Protect"/>. Renvoie null en cas d'échec.</summary>
    public static byte[]? Unprotect(byte[] encrypted)
    {
        var inBlob = default(DATA_BLOB);
        var outBlob = default(DATA_BLOB);
        try
        {
            inBlob.pbData = Marshal.AllocHGlobal(encrypted.Length);
            inBlob.cbData = encrypted.Length;
            Marshal.Copy(encrypted, 0, inBlob.pbData, encrypted.Length);

            if (!CryptUnprotectData(ref inBlob, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                                    IntPtr.Zero, CryptProtectUiForbidden, ref outBlob))
                return null;

            var result = new byte[outBlob.cbData];
            Marshal.Copy(outBlob.pbData, result, 0, outBlob.cbData);
            return result;
        }
        catch { return null; }
        finally
        {
            if (inBlob.pbData != IntPtr.Zero) Marshal.FreeHGlobal(inBlob.pbData);
            if (outBlob.pbData != IntPtr.Zero) NativeFree(outBlob.pbData);
        }
    }

    // Les blobs de sortie de DPAPI sont alloués par LocalAlloc -> libérés par LocalFree.
    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr hMem);

    private static void NativeFree(IntPtr p) => LocalFree(p);
}
