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

using Reko.Core.Expressions;
using Reko.Core.Machine;
using Reko.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reko.Arch.PaRisc
{
    /// <summary>
    /// Represents a signed left side of a constant.
    /// </summary>
    public class LeftImmediateOperand : AbstractMachineOperand
    {
        public LeftImmediateOperand(int value) : base(PrimitiveType.Word32)
        {
            this.Value = Constant.Int32(value);
        }

        public Constant Value { get; }

        protected override void DoRender(MachineInstructionRenderer renderer, MachineInstructionRendererOptions options)
        {
            var v = Value.ToInt32();
            var fmt = v < 0 ? "L%-{0:X8}" : "L%{0:X8}";
            renderer.WriteFormat(fmt, Math.Abs(v));
        }
    }
}
