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
using Reko.Core.Expressions;
using Reko.Core.Rtl;
using System.Collections.Generic;
using System.Linq;

namespace Reko.Environments.MacOS.OSX.ArchSpecific
{
    public class X86_64Handler : ArchSpecificHandler
    {
        private readonly IProcessorArchitecture arch;

        public X86_64Handler(IProcessorArchitecture arch)
        {
            this.arch = arch;
        }

        public override CallingConvention? GetCallingConvention(string? ccName)
        {
            return new X86_64CallingConvention(arch);
        }

        public override Expression? GetTrampolineDestination(Address addrInstr, List<RtlInstructionCluster> instrs, IRewriterHost host)
        {
            if (instrs.Count < 1)
                return null;
            if (instrs[^1].Instructions[0] is RtlGoto jmp &&
                jmp.Target is ProcedureConstant con)
            {
                return con;
            }
            return null;
        }

        public override Expression? GetTrampolineDestination(Address addrInstr, IEnumerable<RtlInstruction> instrs, IRewriterHost host)
        {
            var rtl = instrs.Take(1).ToArray();
            if (rtl.Length != 1)
                return null;
            if (rtl[0] is RtlGoto jmp &&
                jmp.Target is ProcedureConstant con)
            {
                return con;
            }
            return null;
        }
    }
}