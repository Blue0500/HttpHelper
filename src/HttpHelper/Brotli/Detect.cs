using System;
using System.Runtime.InteropServices;

namespace BrotliSharpLib {
    internal static partial class Brotli {
        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_INFO {
            public ushort wProcessorArchitecture;
            public ushort wReserved;
            public uint dwPageSize;
            public IntPtr lpMinimumApplicationAddress;
            public IntPtr lpMaximumApplicationAddress;
            public IntPtr dwActiveProcessorMask;
            public uint dwNumberOfProcessors;
            public uint dwProcessorType;
            public uint dwAllocationGranularity;
            public ushort wProcessorLevel;
            public ushort wProcessorRevision;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct Utsname {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 65)]
            public string sysname;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 65)]
            public string nodename;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 65)]
            public string release;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 65)]
            public string version;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 65)]
            public string machine;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
            private byte[] padding;
        }

        private enum Endianess {
            Little,
            Big,
            Unknown
        }

        /// <summary>
        /// Detects the endianness of the current CPU
        /// </summary>
        private static unsafe Endianess GetEndianess() {
            uint value = 0xaabbccdd;
            byte* b = (byte*)&value;
            if (b[0] == 0xdd) {
                return Endianess.Little;
            }

            if (b[0] == 0xaa) {
                return Endianess.Big;
            }

            return Endianess.Unknown;
        }
    }
}