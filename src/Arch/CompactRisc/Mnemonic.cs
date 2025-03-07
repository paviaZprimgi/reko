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

using System;
using System.Collections.Generic;
using System.Text;

namespace Reko.Arch.CompactRisc
{
    public enum Mnemonic
    {
        Invalid,
        Nyi,

        addb,
        addcb,
        addcw,
        addd,
        addub,
        adduw,
        addw,
        andb,
        andd,
        andw,
        ashub,
        ashud,
        ashuw,
        bal,
        beq0b,
        beq0w,
        bne0b,
        bne0w,
        bcc,
        bcs,
        beq,
        bfc,
        bfs,
        bge,
        bgt,
        bhi,
        bhs,
        ble,
        blo,
        bls,
        blt,
        bne,
        br,
        cbitb,
        cbitw,
        cinv,
        cmpb,
        cmpd,
        cmpw,
        di,
        ei,
        eiwait,
        excp,
        jal,
        Jcondb,
        loadb,
        loadd,
        loadm,
        loadmp,
        loadw,
        lpr,
        lprd,
        lshb,
        lshd,
        lshw,
        macqw,
        macsw,
        macuw,
        movb,
        movd,
        movw,
        movxb,
        movxw,
        movzb,
        movzw,
        mulb,
        mulsb,
        mulsw,
        muluw,
        mulw,
        orb,
        ord,
        orw,
        pop,
        popret,
        push,
        res,
        retx,
        sbitb,
        sbitw,
        Scond,
        spr,
        sprd,
        storb,
        stord,
        storm,
        stormp,
        storw,
        subb,
        subcb,
        subcw,
        subd,
        subw,
        tbit,
        tbitb,
        tbitw,
        wait,
        xorb,
        xord,
        xorw,
    }
}
