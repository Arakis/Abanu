﻿using lonos.Kernel.Core;

namespace lonos.os.Core.x86
{
    public static class Start
    {
        public static void Main()
        {
            Kernel.Core.Start.Main();
            Kernel.Core.x86.DummyClass.DummyCall();
            while (true)
            { }
        }
    }
}
