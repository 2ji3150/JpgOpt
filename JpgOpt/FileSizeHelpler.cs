using System;

namespace JpgOpt {
    class FileSizeHelpler
    {
        static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB" };
        public static string SizeSuffix(Int64 value)
        {
            if (value < 0) return $"-{SizeSuffix(-value)}";
            if (value == 0) return "0.0 bytes";
            int mag = (int)Math.Log(value, 1024);
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));
            return $"{adjustedSize:n1} {SizeSuffixes[mag]}";
        }
    }
}
