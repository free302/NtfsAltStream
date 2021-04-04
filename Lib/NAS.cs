using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace NasLib
{
    /// <summary>
    /// NTFS Alternate Stream
    /// </summary>
    public class NAS
    {
        public static bool HasStream(string filePath, string streamName)
        {
            try
            {
                using (var sfh = open(filePath, streamName, EFileAccess.GenericRead, ECreationDisposition.OpenExisting))
                    return true;
            }
            catch { return false; }
        }

        public static void WriteStream(string filePath, string streamName, byte[] data)
        {
            using (var sfh = open(filePath, streamName, EFileAccess.GenericWrite, ECreationDisposition.CreateAlways))
            {
                using (var fs = new FileStream(sfh, FileAccess.Write)) fs.Write(data, 0, data.Length);
            }
        }
        public static byte[] ReadStream(string filePath, string streamName)
        {
            using (var sfh = open(filePath, streamName, EFileAccess.GenericRead, ECreationDisposition.OpenExisting))
            using (var fs = new FileStream(sfh, FileAccess.Read))
            {
                var buffer = new byte[fs.Length];
                fs.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }        

        static SafeFileHandle open(string filePath, string streamName, EFileAccess access, ECreationDisposition mode)
        {
            var sfh = CreateFile($"{filePath}:{streamName}", access, EFileShare.Read, IntPtr.Zero,
                mode, EFileAttributes.Normal, IntPtr.Zero);
            if (sfh.IsInvalid) Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            return sfh;
        }

        #region ---- PInvoke ----

        // delete file 필요

        [Flags]
        enum EFileAccess : uint
        {
            GenericRead = 0x80000000, GenericWrite = 0x40000000, GenericExecute = 0x20000000, GenericAll = 0x10000000
        }
        [Flags]
        enum EFileShare : uint
        {
            None = 0x00000000, Read = 0x00000001, Write = 0x00000002, Delete = 0x00000004
        }
        enum ECreationDisposition : uint
        {
            New = 1, CreateAlways = 2, OpenExisting = 3, OpenAlways = 4, TruncateExisting = 5
        }
        [Flags]
        enum EFileAttributes : uint
        {
            Normal = 0x00000080
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern SafeFileHandle CreateFile(
           string lpFileName, EFileAccess dwDesiredAccess, EFileShare dwShareMode, IntPtr lpSecurityAttributes,
           ECreationDisposition dwCreationDisposition, EFileAttributes dwFlagsAndAttributes, IntPtr hTemplateFile);

        #endregion
    
    }
}
