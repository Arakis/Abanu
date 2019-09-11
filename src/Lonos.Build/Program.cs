﻿// This file is part of Lonos Project, an Operating System written in C#. Web: https://www.lonos.io
// Licensed under the GNU 2.0 license. See LICENSE.txt file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using Mosa.Compiler.MosaTypeSystem;

namespace Lonos.Build
{

    internal static class Program
    {

        private static void Main(string[] args)
        {

            Console.WriteLine("Starting Build...");

            string file;
            if (args[0] == "--image=loader")
            {
                file = BuildUtility.GetEnv("LONOS_BOOTLOADER_EXE");

                var builderBoot = new LonosBuilder_Loader(file);
                builderBoot.Build();
            }
            else if (args[0] == "--image=kernel")
            {
                file = BuildUtility.GetEnv("LONOS_EXE");

                var builder = new LonosBuilder_Kernel(file);
                builder.Build();
            }
            else if (args[0] == "--image=app")
            {
                file = BuildUtility.GetEnv("${LONOS_PROJDIR}/bin/App.HelloKernel.exe");

                var builder = new LonosBuilder_App(file);
                builder.Build();
            }
            else if (args[0] == "--image=app2")
            {
                file = BuildUtility.GetEnv("${LONOS_PROJDIR}/bin/App.HelloService.exe");

                var builder = new LonosBuilder_App(file);
                builder.Build();
            }
            else if (args[0] == "--image=service.basic")
            {
                file = BuildUtility.GetEnv("${LONOS_PROJDIR}/bin/Lonos.Service.Basic.exe");

                var builder = new LonosBuilder_App(file);
                builder.Build();
            }
            else if (args[0] == "--image=app.shell")
            {
                file = BuildUtility.GetEnv("${LONOS_PROJDIR}/bin/App.Shell.exe");

                var builder = new LonosBuilder_App(file);
                builder.Build();
            }
            else if (args[0] == "--image=image")
            {
                LinkImages();
            }
            System.Console.WriteLine("ready");
            //System.Console.ReadLine();
        }

        public static void LinkImages()
        {
            var loaderFile = BuildUtility.GetEnv("${LONOS_OSDIR}/Lonos.OS.Loader.x86.bin");
            var kernelFile = BuildUtility.GetEnv("${LONOS_OSDIR}/Lonos.OS.Core.x86.bin");

            var kernelBytes = File.ReadAllBytes(kernelFile);

            //var ms = new MemoryStream(File.ReadAllBytes(loaderFile));
            var ms = new MemoryStream();
            using (var reader = new BinaryReader(ms))
            {
                using (var writer = new BinaryWriter(ms))
                {
                    writer.Write(File.ReadAllBytes(loaderFile));

                    var alignment = 0x1000;

                    var alignLoaderSectionStart = DivCeil((uint)ms.Length, (uint)alignment) * alignment;
                    var fillBytes = alignLoaderSectionStart - ms.Length;
                    for (var i = 0; i < fillBytes; i++)
                        writer.Write((byte)0);

                    var debugPos = ms.Length;
                    writer.Write(kernelBytes);

                    fillBytes = (DivCeil((uint)kernelBytes.Length, (uint)alignment) * alignment) - kernelBytes.Length;
                    for (var i = 0; i < fillBytes; i++)
                        writer.Write((byte)0);

                    ms.Position = 42;
                    var progHeaderSize = reader.ReadUInt16();
                    ms.Position = 44;
                    var progHeaderNum = reader.ReadUInt16();
                    ms.Position = 28;
                    var pHeaderArrayStart = reader.ReadUInt32();
                    var extraEntry = pHeaderArrayStart + ((progHeaderNum - 1) * progHeaderSize);

                    uint offset = (uint)alignLoaderSectionStart;
                    uint memSize = (uint)kernelBytes.Length;
                    uint fileSize = (uint)kernelBytes.Length;

                    ms.Position = extraEntry + 4;
                    writer.Write(offset);

                    ms.Position = extraEntry + 16;
                    writer.Write(fileSize);
                    writer.Write(memSize);

                    var bytes = ms.ToArray();
                    var outFile = Path.Combine(BuildUtility.GetEnv("LONOS_OSDIR"), "Lonos.OS.image.x86.bin");
                    File.WriteAllBytes(outFile, bytes);
                }
            }
        }

        public static uint DivCeil(uint value, uint dividor)
        {
            return ((value - 1) / dividor) + 1;
        }

    }
}
