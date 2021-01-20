﻿using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashSharp
{
    static class Options
    {
        public static readonly Option<string?> WorkingDirectory = new Option<string?>("--cd", "Set working directory");
    }
}
