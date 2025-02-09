﻿// This file is part of Abanu, an Operating System written in C#. Web: https://www.abanu.org
// Licensed under the GNU 2.0 license. See LICENSE.txt file in the project root for full license information.

using System.Runtime.InteropServices;
using Abanu.Kernel.Core;
using Mosa.Runtime.x86;

namespace Abanu.Kernel.Loader
{
    /// <summary>
    /// Static class of helpful memory functions
    /// </summary>
    public static unsafe class Multiboot
    {
        /// <summary>
        /// Magic value that indicates that kernel was loaded by a Multiboot-compliant boot loader
        /// </summary>
        public const uint MultibootMagic = 0x2BADB002;

        public static MultiBootInfo* MultiBootInfo = null;
        private static uint memoryMapCount = 0;

        public static uint MultibootAddress = 0x0;

        public static uint ElfSectionHeaderCount => MultiBootInfo->Syms1;

        public static uint ElfSectionHeaderSize => MultiBootInfo->Syms2;

        public static uint ElfSectionHeaderAddr => MultiBootInfo->Syms3;

        public static uint ElfSectionHeaderShndx => MultiBootInfo->Syms4;

        /// <summary>
        /// Gets the memory map count.
        /// </summary>
        /// <value>The memory map count.</value>
        public static uint MemoryMapCount => memoryMapCount;

        /// <summary>
        /// Gets a value indicating whether this instance is multiboot enabled.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is multiboot enabled; otherwise, <c>false</c>.
        /// </value>
        public static bool IsMultibootEnabled => MultiBootInfo != null;

        /// <summary>
        /// Gets the flags.
        /// </summary>
        /// <value>The flags.</value>
        public static uint Flags => MultiBootInfo->Flags;

        /// <summary>
        /// Gets the memory lower.
        /// </summary>
        /// <value>The lower memory.</value>
        public static uint MemoryLower => MultiBootInfo->MemLower;

        /// <summary>
        /// Gets the memory upper.
        /// </summary>
        /// <value>The memory upper.</value>
        public static uint MemoryUpper => MultiBootInfo->MemUpper;

        /// <summary>
        /// Gets the boot device.
        /// </summary>
        /// <value>The boot device.</value>
        public static uint BootDevice => MultiBootInfo->BootDevice;

        /// <summary>
        /// Gets the CMD line address.
        /// </summary>
        /// <value>The CMD line address.</value>
        public static uint CmdLineAddress => MultiBootInfo->CommandLine;

        /// <summary>
        /// Gets the modules start.
        /// </summary>
        /// <value>The modules start.</value>
        public static uint ModulesStart => MultiBootInfo->ModuleAddress;

        /// <summary>
        /// Gets the modules count.
        /// </summary>
        /// <value>The modules count.</value>
        public static uint ModulesCount => MultiBootInfo->ModuleCount;

        /// <summary>
        /// Gets the length of the memory map.
        /// </summary>
        /// <value>The length of the memory map.</value>
        private static uint MemoryMapIndexLength => MultiBootInfo->MemMapLength;

        /// <summary>
        /// Gets the memory map start.
        /// </summary>
        /// <value>The memory map start.</value>
        private static uint MemoryMapIndexStart => MultiBootInfo->MemMapAddress;

        /// <summary>
        /// Gets the length of the drive.
        /// </summary>
        /// <value>The length of the drive.</value>
        public static uint DriveLength => MultiBootInfo->DrivesLength;

        /// <summary>
        /// Gets the drive start.
        /// </summary>
        /// <value>The drive start.</value>
        public static uint DriveStart => MultiBootInfo->DrivesAddress;

        /// <summary>
        /// Gets the configuration table.
        /// </summary>
        /// <value>The configuration table.</value>
        public static uint ConfigurationTable => MultiBootInfo->ConfigTable;

        /// <summary>
        /// Gets the name of the boot loader.
        /// </summary>
        /// <value>The name of the boot loader.</value>
        public static uint BootLoaderName => MultiBootInfo->BootLoaderName;

        /// <summary>
        /// Gets the APM table.
        /// </summary>
        /// <value>The APM table.</value>
        public static uint APMTable => MultiBootInfo->ApmTable;

        /// <summary>
        /// Gets the VBE control information.
        /// </summary>
        /// <value>The VBE control information.</value>
        public static uint VBEControlInformation => MultiBootInfo->VbeControlInfo;

        /// <summary>
        /// Gets the VBE mode info pointer.
        /// </summary>
        /// <value>The VBE mode info pointer.</value>
        public static uint VBEModeInfo => MultiBootInfo->VbeModeInfo;

        /// <summary>
        /// Gets the VBE mode.
        /// </summary>
        /// <value>The VBE mode.</value>
        public static uint VBEMode => MultiBootInfo->VbeMode;

        /// <summary>
        /// Gets the VBE interface seg.
        /// </summary>
        /// <value>The VBE interface seg.</value>
        public static uint VBEInterfaceSeg => MultiBootInfo->VbeInterfaceSeg;

        /// <summary>
        /// Gets the VBE interface off.
        /// </summary>
        /// <value>The VBE interface off.</value>
        public static uint VBEInterfaceOff => MultiBootInfo->VbeInterfaceOff;

        /// <summary>
        /// Gets the VBE interface len.
        /// </summary>
        /// <value>The VBE interface len.</value>
        public static uint VBEInterfaceLen => MultiBootInfo->VbeInterfaceLength;

        /// <summary>
        /// Gets the presence of VBE.
        /// </summary>
        /// <value>True if VBE is present.</value>
        public static bool VBEPresent
        {
            get { return (Flags & (1 << 11)) == (1 << 11); }
        }

        public static bool FBPresent
        {
            get { return (Flags & (1 << 12)) == (1 << 12); }
        }

        /// <summary>
        /// Gets the VBE mode info structure.
        /// </summary>
        /// <value>The VBE mode info structure.</value>
        public static VBEMode VBEModeInfoStructure
        {
            get
            {
                if (_vbeModeInfoStructure == null && VBEPresent)
                    _vbeModeInfoStructure = new VBEMode((VBEModeInfo*)VBEModeInfo);

                return _vbeModeInfoStructure;
            }
        }

        private static VBEMode _vbeModeInfoStructure = null;

        /// <summary>
        /// Setups this multiboot.
        /// </summary>
        public static unsafe void Setup()
        {
            uint magic = Native.GetMultibootEAX();
            uint address = Native.GetMultibootEBX();

            SetMultibootLocation(address, magic);
        }

        /// <summary>
        /// Sets the multiboot location, if given the proper magic value
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="magic">The magic value.</param>
        public static void SetMultibootLocation(uint address, uint magic)
        {
            if (magic == MultibootMagic)
            {
                SetMultibootLocation(address);
            }
        }

        /// <summary>
        /// Sets the multiboot location.
        /// </summary>
        /// <param name="address">The address.</param>
        public static void SetMultibootLocation(uint address)
        {
            MultibootAddress = address;
            MultiBootInfo = (MultiBootInfo*)address;

            CountMemoryMap();
        }

        /// <summary>
        /// Counts the memory map.
        /// </summary>
        private static void CountMemoryMap()
        {
            memoryMapCount = 0;
            MultiBootMemoryMap* location = (MultiBootMemoryMap*)MemoryMapIndexStart;

            while ((uint)location < (MemoryMapIndexStart + MemoryMapIndexLength))
            {
                memoryMapCount++;

                //location = (MultiBootMemoryMap*)(((uint)location) + location->size + 4);
                location = location->Next;
            }
        }

        /// <summary>
        /// Gets the memory map index location.
        /// </summary>
        /// <param name="index">The index.</param>
        private static MultiBootMemoryMap* GetMemoryMapIndexLocation(uint index)
        {
            MultiBootMemoryMap* location = (MultiBootMemoryMap*)MemoryMapIndexStart;

            for (uint i = 0; i < index; i++)
            {
                location = location->Next;
            }
            return location;
        }

        /// <summary>
        /// Gets the memory map base.
        /// </summary>
        public static uint GetMemoryMapBase(uint index)
        {
            return (uint)GetMemoryMapIndexLocation(index)->BaseAddr;
        }

        /// <summary>
        /// Gets the length of the memory map.
        /// </summary>
        /// <param name="index">The index.</param>
        public static uint GetMemoryMapLength(uint index)
        {
            return (uint)GetMemoryMapIndexLocation(index)->Length;
        }

        /// <summary>
        /// Gets the type of the memory map.
        /// </summary>
        /// <param name="index">The index.</param>
        public static byte GetMemoryMapType(uint index)
        {
            return (byte)GetMemoryMapIndexLocation(index)->Type;
        }
    }

    public unsafe class VBEMode
    {
        private VBEModeInfo* _info;

        public VBEMode(VBEModeInfo* info)
        {
            _info = info;
        }

        /// <summary>
        /// Gets the width of the screen in pixels.
        /// </summary>
        /// <returns>Screen width in pixels.</returns>
        public ushort ScreenWidth => _info->ScreenWidth;

        /// <summary>
        /// Gets the height of the screen in pixels.
        /// </summary>
        /// <returns>Screen height in pixels.</returns>
        public ushort ScreenHeight => _info->ScreenHeight;

        /// <summary>
        /// Gets bits per pixel.
        /// </summary>
        /// <returns>Bits per pixel.</returns>
        public ushort BitsPerPixel => _info->BitsPerPixel;

        /// <summary>
        /// Gets bytes per line.
        /// </summary>
        /// <returns>Bytes per line.</returns>
        public ushort Pitch => _info->Pitch;

        /// <summary>
        /// Gets physical location of the framebuffer.
        /// </summary>
        /// <returns>The location of ht framebuffer.</returns>
        public uint MemoryPhysicalLocation => _info->PhysBase;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct MultiBootInfo
    {
        public uint Flags;         //required

        public uint MemLower;      //if bit 0 in flags are set
        public uint MemUpper;      //if bit 0 in flags are set

        public uint BootDevice;    //if bit 1 in flags are set
        public uint CommandLine;   //if bit 2 in flags are set

        public uint ModuleCount;   //if bit 3 in flags are set
        public uint ModuleAddress; //if bit 3 in flags are set

        public uint Syms1; //if bits 4 or 5 in flags are set
        public uint Syms2; //if bits 4 or 5 in flags are set
        public uint Syms3; //if bits 4 or 5 in flags are set
        public uint Syms4; //if bits 4 or 5 in flags are set

        public uint MemMapLength;   //if bit 6 in flags is set
        public uint MemMapAddress;  //if bit 6 in flags is set

        public uint DrivesLength;   //if bit 7 in flags is set
        public uint DrivesAddress;  //if bit 7 in flags is set

        public uint ConfigTable;    //if bit 8 in flags is set
        public uint BootLoaderName; //if bit 9 in flags is set
        public uint ApmTable;       //if bit 10 in flags is set

        public uint VbeControlInfo;     //if bit 11 in flags is set
        public uint VbeModeInfo;        // "
        public ushort VbeMode;            // "
        public ushort VbeInterfaceSeg;    // "
        public ushort VbeInterfaceOff;    // "
        public ushort VbeInterfaceLength; // "

        public ulong FbAddr;   //if bit 12 in flags is set
        public uint FbPitch;   // "
        public uint FbWidth;   // "
        public uint FbHeight;  // "
        public byte FbBpp;     // "
        public byte FbType;    // "
        public uint ColorInfo; // Union

        public MultiBootInfoElfSectionHeader* ElfSectionHeader
        {
            get
            {
                fixed (uint* ptr = &Syms1)
                {
                    return (MultiBootInfoElfSectionHeader*)ptr;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct MultiBootInfoElfSectionHeader
    {
        public uint Count;
        public uint Size;
        public uint Addr;
        public uint Shndx;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MultiBootMemoryMap
    {
        public uint Size;
        public ulong BaseAddr;
        public ulong Length;
        public uint Type;

        public unsafe MultiBootMemoryMap* Next
        {
            get
            {
                fixed (MultiBootMemoryMap* ptr = &this)
                {
                    return (MultiBootMemoryMap*)(((uint)ptr) + Size + 4);
                }
            }
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 256)]
    public unsafe struct VBEModeInfo
    {
        [FieldOffset(0)]
        public ushort Attributes;

        [FieldOffset(2)]
        public byte WindowA;

        [FieldOffset(3)]
        public byte WindowB;

        [FieldOffset(4)]
        public ushort Granularity;

        [FieldOffset(6)]
        public ushort WindowSize;

        [FieldOffset(8)]
        public ushort SegmentA;

        [FieldOffset(10)]
        public ushort SegmentB;

        [FieldOffset(12)]
        public uint WinFuncPtr;

        [FieldOffset(16)]
        public ushort Pitch;

        [FieldOffset(18)]
        public ushort ScreenWidth;

        [FieldOffset(20)]
        public ushort ScreenHeight;

        [FieldOffset(22)]
        public byte WChar;

        [FieldOffset(23)]
        public byte YChar;

        [FieldOffset(24)]
        public byte Planes;

        [FieldOffset(25)]
        public byte BitsPerPixel;

        [FieldOffset(26)]
        public byte Banks;

        [FieldOffset(27)]
        public byte MemoryModel;

        [FieldOffset(28)]
        public byte BankSize;

        [FieldOffset(29)]
        public byte ImagePages;

        [FieldOffset(30)]
        public byte Reserved0;

        [FieldOffset(31)]
        public byte RedMask;

        [FieldOffset(32)]
        public byte RedPosition;

        [FieldOffset(33)]
        public byte GreenMask;

        [FieldOffset(34)]
        public byte GreenPosition;

        [FieldOffset(35)]
        public byte BlueMask;

        [FieldOffset(36)]
        public byte BluePosition;

        [FieldOffset(37)]
        public byte ReservedMask;

        [FieldOffset(38)]
        public byte ReservedPosition;

        [FieldOffset(39)]
        public byte DirectColorAttributes;

        [FieldOffset(40)]
        public uint PhysBase;

        [FieldOffset(44)]
        public uint OffScreenMemoryOff;

        [FieldOffset(48)]
        public ushort OffScreenMemorSize;

        [FieldOffset(50)]
        public fixed byte Reserved1[206];
    }
}
