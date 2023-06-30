﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketCardreaderService
{
    public static class XLExtensions
    {
        public static IEnumerable<string> SplitInGroups(this string original, int size)
        {
            var p = 0;
            var l = original.Length;
            while (l - p > size)
            {
                yield return original.Substring(p, size);
                p += size;
            }
            yield return original.Substring(p);
        }
    }
}
