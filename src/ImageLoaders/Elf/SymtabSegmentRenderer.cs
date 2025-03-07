#region License
/* 
 * Copyright (C) 1999-2023 John Källén.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2, or (at your option)
 * any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; see the file COPYING.  If not, write to
 * the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA.
 */
#endregion

using Reko.Core;
using Reko.Core.Output;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reko.ImageLoaders.Elf
{
    public class SymtabSegmentRenderer32 : ImageSegmentRenderer
    {
        private ElfLoader32 loader;
        private ElfSection shdr;

        public SymtabSegmentRenderer32(ElfLoader32 loader, ElfSection shdr)
        {
            this.loader = loader;
            this.shdr = shdr;
        }

        public override void Render(ImageSegment segment, Program program, Formatter formatter)
        {
            var entries = shdr.EntryCount();
            var symtab = shdr.LinkedSection!;
            var rdr = loader.CreateReader(shdr.FileOffset);
            for (int i = 0; i < entries; ++i)
            {
                if (!rdr.TryReadUInt32(out uint iName))
                    return;
                if (!rdr.TryReadUInt32(out uint value))
                    return;
                if (!rdr.TryReadUInt32(out uint size))
                    return;
                if (!rdr.TryReadByte(out byte info))
                    return;
                if (!rdr.TryReadByte(out byte other))
                    return;
                if (!rdr.TryReadUInt16(out ushort shIndex))
                    return;
                string symStr = loader.GetStrPtr(symtab, iName);
                string segName = loader.GetSectionName(shIndex);
                formatter.Write("{0:X4} {1,-40} {2:X8} {3:X8} {4:X2} {5}", i, symStr, value, size, info & 0xFF, segName);
                formatter.WriteLine();
            }
        }

    }

    public class SymtabSegmentRenderer64 : ImageSegmentRenderer
    {
        private ElfLoader64 loader;
        private ElfSection shdr;

        public SymtabSegmentRenderer64(ElfLoader64 loader, ElfSection shdr)
        {
            this.loader = loader;
            this.shdr = shdr;
        }

        public override void Render(ImageSegment segment, Program program, Formatter formatter)
        {
            var entries = shdr.EntryCount();
            var symtab = shdr.LinkedSection!;
            var rdr = loader.CreateReader(shdr.FileOffset);
            for (var i = 0; i < entries; ++i)
            {
                if (!rdr.TryReadUInt32(out uint iName))
                    return;
                if (!rdr.TryReadByte(out byte info))
                    return;
                if (!rdr.TryReadByte(out byte other))
                    return;
                if (!rdr.TryReadUInt16(out ushort shIndex))
                    return;
                if (!rdr.TryReadUInt64(out ulong value))
                    return;
                if (!rdr.TryReadUInt64(out ulong size))
                    return;
                string symStr = loader.GetStrPtr(symtab, iName);
                string segName = loader.GetSectionName(shIndex);
                formatter.Write("{0,4} {1,-40} {2:X8} {3:X8} {4:X2} {5}", i, symStr, value, size, info & 0xFF, segName);
                formatter.WriteLine();
            }
        }
    }
}
