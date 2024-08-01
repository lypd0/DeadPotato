using System;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Security.Principal;

public class PrivilegeChecker
{
    private const string SE_IMPERSONATE_NAME = "SeImpersonatePrivilege";

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool GetTokenInformation(
        IntPtr TokenHandle,
        TOKEN_INFORMATION_CLASS TokenInformationClass,
        IntPtr TokenInformation,
        uint TokenInformationLength,
        out uint ReturnLength);

    [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
    private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID_AND_ATTRIBUTES
    {
        public LUID Luid;
        public uint Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TOKEN_PRIVILEGES
    {
        public uint PrivilegeCount;
        public LUID_AND_ATTRIBUTES Privileges; // This is a placeholder for the array
    }

    private enum TOKEN_INFORMATION_CLASS
    {
        TokenPrivileges = 3
    }

    private const uint TOKEN_QUERY = 0x0008;
    private const uint SE_PRIVILEGE_ENABLED = 0x00000002;

    public static bool IsSeImpersonatePrivilegeEnabled()
    {
        IntPtr tokenHandle = IntPtr.Zero;
        try
        {
            if (!OpenProcessToken(System.Diagnostics.Process.GetCurrentProcess().Handle, TOKEN_QUERY, out tokenHandle))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            uint tokenInfoLength = 0;
            if (!GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenPrivileges, IntPtr.Zero, 0, out tokenInfoLength))
            {
                int error = Marshal.GetLastWin32Error();
                if (error != 122) // ERROR_INSUFFICIENT_BUFFER
                {
                    throw new Win32Exception(error);
                }
            }

            IntPtr tokenInformation = Marshal.AllocHGlobal((int)tokenInfoLength);
            try
            {
                if (!GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenPrivileges, tokenInformation, tokenInfoLength, out tokenInfoLength))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                TOKEN_PRIVILEGES tokenPrivileges = Marshal.PtrToStructure<TOKEN_PRIVILEGES>(tokenInformation);

                IntPtr luidAndAttributesPtr = IntPtr.Add(tokenInformation, Marshal.OffsetOf<TOKEN_PRIVILEGES>("Privileges").ToInt32());
                LUID_AND_ATTRIBUTES[] privilegesArray = new LUID_AND_ATTRIBUTES[tokenPrivileges.PrivilegeCount];
                for (int i = 0; i < tokenPrivileges.PrivilegeCount; i++)
                {
                    privilegesArray[i] = Marshal.PtrToStructure<LUID_AND_ATTRIBUTES>(luidAndAttributesPtr);
                    luidAndAttributesPtr = IntPtr.Add(luidAndAttributesPtr, Marshal.SizeOf<LUID_AND_ATTRIBUTES>());
                }

                if (!LookupPrivilegeValue(null, SE_IMPERSONATE_NAME, out LUID luid))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                foreach (var privilege in privilegesArray)
                {
                    if (privilege.Luid.Equals(luid) && (privilege.Attributes & SE_PRIVILEGE_ENABLED) == SE_PRIVILEGE_ENABLED)
                    {
                        return true;
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(tokenInformation);
            }
        }
        finally
        {
            if (tokenHandle != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(tokenHandle);
            }
        }

        return false;
    }
}
