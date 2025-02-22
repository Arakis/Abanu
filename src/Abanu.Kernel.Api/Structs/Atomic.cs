﻿// This file is part of Abanu, an Operating System written in C#. Web: https://www.abanu.org
// Licensed under the GNU 2.0 license. See LICENSE.txt file in the project root for full license information.

namespace Abanu.Kernel.Core
{

    public struct Atomic
    {
        public uint Counter;
    }

    public struct Atomic64
    {
        public ulong Counter;
    }
}
