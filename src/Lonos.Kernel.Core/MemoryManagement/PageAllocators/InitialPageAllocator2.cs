﻿// This file is part of Lonos Project, an Operating System written in C#. Web: https://www.lonos.io
// Licensed under the GNU 2.0 license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using Lonos.CTypes;
using Lonos.Kernel.Core.Boot;
using Lonos.Kernel.Core.Devices;
using Lonos.Kernel.Core.Diagnostics;
using Lonos.Kernel.Core.PageManagement;
using Mosa.Runtime;
using Mosa.Runtime.x86;

namespace Lonos.Kernel.Core.MemoryManagement.PageAllocators
{

    /// <summary>
    /// This is a simple, but working Allocator. Its flexible to use.
    /// </summary>
    public abstract unsafe class InitialPageAllocator2 : IPageFrameAllocator
    {

        protected uint _FreePages;

        protected Page* PageArray;
        protected uint _TotalPages;

        private MemoryRegion kmap;

        private uint FistPageNum;

        public string DebugName;
        public int Allocations;

        public PageFrameAllocatorTraceOptions TraceOptions;

        protected abstract MemoryRegion AllocRawMemory(uint size);

        public void Setup(MemoryRegion region, AddressSpaceKind addrKind)
        {
            TraceOptions = new PageFrameAllocatorTraceOptions();
            _AddressSpaceKind = addrKind;
            _Region = region;
            FistPageNum = region.Start / PageSize;
            _TotalPages = region.Size / PageSize;
            kmap = AllocRawMemory(_TotalPages * (uint)sizeof(Page));
            PageArray = (Page*)kmap.Start;

            var firstSelfPageNum = KMath.DivFloor(kmap.Start, 4096);
            var selfPages = KMath.DivFloor(kmap.Size, 4096);

            KernelMessage.WriteLine("Page Frame Array allocated {0} pages, beginning with page {1}", selfPages, firstSelfPageNum);

            PageTableExtensions.SetWritable(PageTable.KernelTable, kmap.Start, kmap.Size);
            MemoryOperation.Clear4(kmap.Start, kmap.Size);

            var addr = FistPageNum * 4096;
            for (uint i = 0; i < _TotalPages; i++)
            {
                PageArray[i].Address = addr;
                //if (i != 0)
                //    PageArray[i - 1].Next = &PageArray[i];
                addr += 4096;
            }

            KernelMessage.WriteLine("Setup free memory");
            SetupFreeMemory();
            KernelMessage.WriteLine("Build linked lists");
            BuildLinkedLists();

            _FreePages = 0;
            for (uint i = 0; i < _TotalPages; i++)
                if (PageArray[i].Status == PageStatus.Free)
                    _FreePages++;

            Assert.True(list_head.list_count(FreeList) == _FreePages, "list_head.list_count(FreeList) == _FreePages");

            KernelMessage.Path(DebugName, "Pages Free: {0}", FreePages);
        }

        protected void BuildLinkedLists()
        {
            FreeList = null;
            for (var i = 0; i < _TotalPages; i++)
            {
                var p = &PageArray[i];
                if (p->Free)
                {
                    if (FreeList == null)
                    {
                        FreeList = (list_head*)p;
                        list_head.INIT_LIST_HEAD(FreeList);
                    }
                    else
                    {
                        list_head.list_add_tail((list_head*)p, FreeList);
                    }
                }
            }
        }

        protected abstract void SetupFreeMemory();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Page* GetPageByAddress(Addr physAddr)
        {
            return GetPageByNum(physAddr / PageSize);
        }

        public Page* GetPageByNum(uint pageNum)
        {
            var pageIdx = pageNum - FistPageNum;
            if (pageIdx > _TotalPages)
                return null;
            return &PageArray[pageIdx];
        }

        public Page* AllocatePage(AllocatePageOptions options = default)
        {
            return AllocatePages(1, options);
        }

        private list_head* FreeList;

        public Page* AllocatePages(uint pages, AllocatePageOptions options = default)
        {
            Page* page = AllocateInternal(pages, options);

            if (page == null)
            {
                //KernelMessage.WriteLine("DebugName: {0}", DebugName);
                KernelMessage.WriteLine("Free pages: {0:X8}, Requested: {1:X8}", FreePages, pages);
                Panic.Error("Out of Memory");
            }

            return page;
        }

        private Page* AllocateInternal(uint pages, AllocatePageOptions options = default)
        {
            if (KConfig.Trace.PageAllocation && TraceOptions.Enabled && pages >= TraceOptions.MinPages)
                KernelMessage.Path(DebugName, "Requesting Pages: {1}. Available: {2} DebugName={0}", options.DebugName, pages, _FreePages);

            if (pages == 256)
            {
                Debug.Nop();
            }

            lock (this)
            {
                if (pages > 1 && (AddressSpaceKind == AddressSpaceKind.Virtual || options.Continuous))
                {
                    if (!MoveToFreeContinuous(pages))
                    {
                        // Compact
                        KernelMessage.Path(DebugName, "Compacting Linked List");
                        this.Dump();
                        BuildLinkedLists();
                        if (!MoveToFreeContinuous(pages))
                        {
                            this.Dump();
                            KernelMessage.WriteLine("Requesting {0} pages failed", pages);
                            Panic.Error("Requesting pages failed: out of memory");
                        }
                    }
                }

                // ---
                var head = FreeList;
                var headPage = (Page*)head;
                FreeList = head->next;
                list_head.list_del_init(head);
                headPage->Status = PageStatus.Used;
                if (KConfig.Trace.PageAllocation)
                    if (options.DebugName != null)
                        headPage->DebugTag = (uint)Intrinsic.GetObjectAddress(options.DebugName);
                    else
                        headPage->DebugTag = null;
                _FreePages--;
                // ---

                for (var i = 1; i < pages; i++)
                {
                    var tmpNextFree = FreeList->next;
                    list_head.list_move_tail(FreeList, head);
                    var p = (Page*)FreeList;
                    p->Status = PageStatus.Used;
                    FreeList = tmpNextFree;
                    _FreePages--;
                }

                if (KConfig.Trace.PageAllocation && TraceOptions.Enabled && pages >= TraceOptions.MinPages)
                    KernelMessage.Path(DebugName, "Allocation done. Addr: {0:X8} Available: {1}", GetAddress(headPage), _FreePages);

                Allocations++;
                return headPage;
            }

        }

        private bool MoveToFreeContinuous(uint pages)
        {
            Page* tryHead = (Page*)FreeList;
            var loopedPages = 0;

            Page* tmpHead = tryHead;
            var found = false;
            for (int i = 0; i < pages; i++)
            {
                if (loopedPages >= _FreePages)
                    return false;
                loopedPages++;

                var next = (Page*)tmpHead->Lru.next;
                if (GetPageNum(next) - GetPageNum(tmpHead) != 1)
                {
                    tryHead = next;
                    tmpHead = next;
                    i = -1; // Reset loop
                    continue;
                }

                tmpHead = next;

                if (i == pages - 1)
                    found = true;
            }
            FreeList = (list_head*)tryHead;
            return true;
        }

        /// <summary>
        /// Releases a page to the free list
        /// </summary>
        public void Free(Page* page)
        {
            var oldFree = _FreePages;
            string debugName = null;
            if (page->DebugTag != null)
                debugName = (string)Intrinsic.GetObjectFromAddress((Pointer)(uint)page->DebugTag);

            lock (this)
            {
                Page* temp = page;
                uint result = 0;

                do
                {
                    temp = (Page*)temp->Lru.next;
                    _FreePages++;
                    temp->Status = PageStatus.Free;
                }
                while (temp != page);

                list_head.list_headless_splice_tail((list_head*)page, FreeList);
            }
            var freedPages = _FreePages - oldFree;
            if (KConfig.Trace.PageAllocation && TraceOptions.Enabled && freedPages >= TraceOptions.MinPages)
                KernelMessage.Path(DebugName, "Freed Pages: {1}. Addr: {2:X8}. Now available: {3} --> {4}. Allocations={5} DebugName={0}.", debugName, freedPages, GetAddress(page), oldFree, _FreePages, (uint)Allocations);

            Allocations--;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetAddress(Page* page)
        {
            return page->Address;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetPageNum(Page* page)
        {
            return GetAddress(page) / 4096;
        }

        public Page* GetPageByIndex(uint pageIndex)
        {
            if (pageIndex >= _TotalPages)
                return null;
            return &PageArray[pageIndex];
        }

        public Page* NextPage(Page* page)
        {
            var pageIdx = GetPageIndex(page) + 1;
            if (pageIdx >= _TotalPages)
                return null;
            return &PageArray[pageIdx];
        }

        public Page* NextCompoundPage(Page* page)
        {
            if (page == null)
                return null;

            return (Page*)page->Lru.next;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetPageIndex(Page* page)
        {
            return GetPageNum(page) - FistPageNum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetPageIndex(Addr addr)
        {
            return (addr / 4096) - FistPageNum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsPage(Page* page)
        {
            return _Region.Contains(page->Address);
        }

        /// <summary>
        /// Gets the size of a single memory page.
        /// </summary>
        public static uint PageSize => 4096;

        public uint TotalPages => _TotalPages;

        private MemoryRegion _Region;
        public MemoryRegion Region => _Region;

        private AddressSpaceKind _AddressSpaceKind;
        public AddressSpaceKind AddressSpaceKind => _AddressSpaceKind;

        public uint FreePages => _FreePages;

        public void SetTraceOptions(PageFrameAllocatorTraceOptions options)
        {
            TraceOptions = options;
        }

    }

}
