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
using Reko.Core.Code;
using Reko.Core.Collections;
using Reko.Core.Diagnostics;
using Reko.Core.Expressions;
using Reko.Core.Operators;
using Reko.Core.Services;
using Reko.Core.Types;
using Reko.Evaluation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Reko.Analysis
{
    /// <summary>
    /// Locates instances of add aLo, bLow followed later adc aHi, bHi and
    /// merges them into (add a, b).
    /// </summary>
    /// <remarks>
    /// Limitations: only does this on pairs within the same basic block,
    /// as dominator analysis and SSA analysis haven't been done this early. 
    /// //$TODO: consider doing this _after_ SSA, so that we reap the benefit
    /// of performing this across basic block boundaries. The challenge is
    /// to introduce new variables xx_yy that interfere with existing xx 
    /// and yy references.
    /// This code must be run immediately after SSA translation. In particular
    /// it must happen before value propagation since VP changes 
    /// <code>
    /// adc r1,0,C
    /// </code>
    /// to
    /// <code>
    /// add r1,C
    /// </code>
    /// </remarks>
    public class LongAddRewriter
    {
        private static readonly TraceSwitch trace = new(nameof(LongAddRewriter), "Trace LongAddRewriter operations") { Level = TraceLevel.Verbose };

        private static readonly InstructionMatcher adcPattern;
        private static readonly InstructionMatcher addPattern;
        private static readonly ExpressionMatcher memOffset;
        private static readonly ExpressionMatcher segMemOffset;
        private static readonly InstructionMatcher condm;

        private readonly SsaState ssa;
        private readonly IProcessorArchitecture arch;
        private readonly DecompilerEventListener listener;
        private Expression? dst;

        public LongAddRewriter(SsaState ssa, DecompilerEventListener listener)
        {
            this.ssa = ssa;
            this.arch = ssa.Procedure.Architecture;
            this.listener = listener;
        }

        static LongAddRewriter()
        {
            condm = new InstructionMatcher(
                new Assignment(
                    ExpressionMatcher.AnyId("grf"),
                    new ConditionOf(
                        ExpressionMatcher.AnyExpression("exp"))));

            addPattern = new InstructionMatcher(
                new Assignment(
                    ExpressionMatcher.AnyId("dst"),
                    new BinaryExpression(
                        ExpressionMatcher.AnyOperator("op"),
                        VoidType.Instance,
                        ExpressionMatcher.AnyExpression("left"),
                        ExpressionMatcher.AnyExpression("right"))));

            adcPattern = new InstructionMatcher(
                new Assignment(
                    ExpressionMatcher.AnyId("dst"),
                    new BinaryExpression(
                        ExpressionMatcher.AnyOperator("op1"),
                        VoidType.Instance,
                        new BinaryExpression(
                            ExpressionMatcher.AnyOperator("op2"),
                            VoidType.Instance,
                            ExpressionMatcher.AnyExpression("left"),
                            ExpressionMatcher.AnyExpression("right")),
                        ExpressionMatcher.AnyExpression("cf"))));

            memOffset = ExpressionMatcher.Build(m =>
                new MemoryAccess(
                    new BinaryExpression(
                        ExpressionMatcher.AnyOperator("op"),
                        VoidType.Instance,
                        ExpressionMatcher.AnyExpression("base"),
                        ExpressionMatcher.AnyConstant("Offset")),
                    ExpressionMatcher.AnyDataType("dt")));

            segMemOffset = new ExpressionMatcher(
                new MemoryAccess(
                    MemoryIdentifier.GlobalMemory,
                    new SegmentedPointer(
                        ExpressionMatcher.AnyDataType(null),
                        ExpressionMatcher.AnyId(),
                        new BinaryExpression(
                            ExpressionMatcher.AnyOperator("op"),
                            VoidType.Instance,
                            ExpressionMatcher.AnyExpression("base"),
                            ExpressionMatcher.AnyConstant("Offset"))),
                    ExpressionMatcher.AnyDataType("dt")));
        }

        public void CreateLongInstruction(AddSubCandidate loCandidate, AddSubCandidate hiCandidate)
        {
            var totalSize = PrimitiveType.Create(
                Domain.SignedInt | Domain.UnsignedInt,
                loCandidate.Dst!.DataType.BitSize + hiCandidate.Dst!.DataType.BitSize);
            var left = CreateWideExpression(loCandidate.Left, hiCandidate.Left, totalSize);
            var right = CreateWideExpression(loCandidate.Right, hiCandidate.Right, totalSize);
            this.dst = CreateWideExpression(loCandidate.Dst, hiCandidate.Dst, totalSize);
            var stmts = hiCandidate.Statement!.Block.Statements;
            var addr = hiCandidate.Statement.Address;
            var iStm = FindInsertPosition(loCandidate, hiCandidate, stmts);
            Statement? stmMkLeft = null;
            if (left is Identifier)
            {
                stmMkLeft = stmts.Insert(
                    iStm++,
                    addr,
                    CreateMkSeq(left, hiCandidate.Left, loCandidate.Left));
                left = ReplaceDstWithSsaIdentifier(left, stmMkLeft);
            }

            Statement? stmMkRight = null;
            if (right is Identifier)
            {
                stmMkRight = stmts.Insert(
                    iStm++,
                    addr,
                    CreateMkSeq(right, hiCandidate.Right, loCandidate.Right));
                right = ReplaceDstWithSsaIdentifier(right, stmMkRight);
            }

            var expSum = new BinaryExpression(loCandidate.Op, left.DataType, left, right);
            Instruction instr = Assign(dst, expSum);
            var stmLong = stmts.Insert(iStm++, addr, instr);
            this.dst = ReplaceDstWithSsaIdentifier(this.dst, stmLong);

            var sidDst = GetSsaIdentifierOf(dst);
            //var sidLeft = GetSsaIdentifierOf(left);
            //var sidRight = GetSsaIdentifierOf(right);
            if (stmMkLeft is not null)
                ssa.AddUses(stmMkLeft);
            if (stmMkRight is not null)
                ssa.AddUses(stmMkRight);
            ssa.AddUses(stmLong);

            var sidDstLo = GetSsaIdentifierOf(loCandidate.Dst);
            if (sidDstLo != null)
            {
                var cast = new Slice(loCandidate.Dst.DataType, dst, 0);
                var stmCastLo = stmts.Insert(iStm++, addr, new AliasAssignment(
                    sidDstLo.Identifier, cast));
                var stmDeadLo = sidDstLo.DefStatement;
                sidDstLo.DefStatement = stmCastLo;

                var sidDstHi = GetSsaIdentifierOf(hiCandidate.Dst);
                var slice = new Slice(hiCandidate.Dst.DataType, dst, loCandidate.Dst.DataType.BitSize);
                var stmSliceHi = stmts.Insert(iStm++, addr, new AliasAssignment(
                    sidDstHi!.Identifier, slice));
                var stmDeadHi = sidDstHi.DefStatement;
                sidDstHi.DefStatement = stmSliceHi;

                if (sidDstLo != null)
                {
                    sidDst!.Uses.Add(stmCastLo);
                }
                if (sidDstHi != null)
                {
                    sidDst!.Uses.Add(stmSliceHi);
                }
                ssa.DeleteStatement(stmDeadLo!);
                ssa.DeleteStatement(stmDeadHi!);
            }
        }

        /// <summary>
        /// Find a statement index appropriate for insert the new
        /// long addition statements.
        /// </summary>
        /// <returns></returns>
        private int FindInsertPosition(AddSubCandidate loCandidate, AddSubCandidate hiCandidate, StatementList stmts)
        {
            int iStm = stmts.IndexOf(hiCandidate.Statement!);
            if (loCandidate.Dst is Identifier idLow)
            {
                int iFirstLowUsage = ssa.Identifiers[idLow].Uses
                    .Select(u => stmts.IndexOf(u))
                    .Where(i => i >= 0)
                    .Min();
                iStm = Math.Min(iStm, iFirstLowUsage);
            }
            return iStm;
        }

        private Expression ReplaceDstWithSsaIdentifier(Expression dst, Statement stmLong)
        {
            if (stmLong.Instruction is Assignment ass) {
                var sid = ssa.Identifiers.Add(ass.Dst, stmLong, false);
                ass.Dst = sid.Identifier;
                return ass.Dst;
            }
            return dst;
        }

        private static Instruction CreateMkSeq(Expression dst, Expression hi, Expression lo)
        {
            return Assign(dst, new MkSequence(dst.DataType, hi, lo));
        }

        private static Instruction Assign(Expression dst, Expression src)
        {
            if (dst is Identifier idDst)
            {
                return new Assignment(idDst, src);
            }
            else
            {
                return new Store(dst, src);
            }
        }

        private SsaIdentifier? GetSsaIdentifierOf(Expression dst)
        {
            if (dst is Identifier id)
                return ssa.Identifiers[id];
            else if (dst is MkSequence seq)
            {
                //$TODO what if there are many identifiers?
                foreach (var e in seq.Expressions)
                    if (e is Identifier eId)
                        return ssa.Identifiers[eId];
            }
            return null;
        }

        public void Transform()
        {
            foreach (var block in ssa.Procedure.ControlGraph.Blocks)
            {
                if (listener.IsCanceled())
                    return;
                ReplaceLongAdditions(block);
            }
        }

        /// <summary>
        /// Look for add/adc or sub/sbc pairs that look like long additions
        /// by scanning the statements from the beginning of the block to the end.
        /// </summary>
        public void ReplaceLongAdditions(Block block)
        {
            var stmtsOrig = block.Statements.ToList();
            for (int i = 0; i < block.Statements.Count; ++i)
            {
                if (listener.IsCanceled())
                    return;
                var loInstr = MatchAddSub(block.Statements[i]);
                if (loInstr is null)
                    continue;
                var cond = FindConditionOf(stmtsOrig, i, loInstr.Dst!);
                if (cond is null)
                    continue;

                var hiInstr = FindUsingInstruction(block, cond.FlagGroup, loInstr);
                if (hiInstr is null)
                    continue;
                trace.Verbose("Larw: {0}: found add/sub pair {1} / {2}", block.DisplayName, loInstr.Statement!, hiInstr.Statement!);
                CreateLongInstruction(loInstr, hiInstr);
            }
        }

        /// <summary>
        /// Determines if the carry flag reaches a using instruction, and 
        /// if that using instruction is an ADC or SBC type instruction.
        /// </summary>
        /// <param name="block">The block in which the ADD was found. The ADC
        /// must be in the same block.</param>
        /// <param name="cy">The identifier that tracks the carry flag.</param>
        /// <param name="loInstr">The candidate instruction for the low half
        /// of the addition.</param>
        /// <returns>The MSB part of the ADD/ADC - SUB/SBC pair if successful,
        /// otherwise null.</returns>
        public AddSubCandidate? FindUsingInstruction(
            Block block,
            Identifier cy,
            AddSubCandidate loInstr)
        {
            var queue = new Queue<Statement>(ssa.Identifiers[cy].Uses);
            while (queue.Count > 0)
            {
                var use = queue.Dequeue();
                var asc = MatchAdcSbc(use);
                if (asc != null && asc.Statement != null && asc.Statement.Block == block)
                {
                    if (asc.Op != loInstr.Op)
                        return null;
                    if (asc.Left.GetType() != loInstr.Left.GetType())
                        return null;
                    asc.Statement = use;
                    return asc;
                }
                if (use.Instruction is not Assignment ass)
                    continue;
                if (ass.Src is Slice)
                {
                    queue.EnqueueRange(ssa.Identifiers[ass.Dst].Uses);
                    continue;
                }
                if (IsCarryFlag(ass.Dst))
                    return null;
            }
            return null;
        }

        public bool IsCarryFlag(Expression? exp)
        {
            return 
                exp is Identifier cf &&
                cf.Storage is FlagGroupStorage grf &&
                (arch.CarryFlagMask & grf.FlagGroupBits) != 0;
        }

        /// <summary>
        /// Finds the subsequent statement in this block that defines a condition code based on the
        /// result in expression <paramref name="exp"/>.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="ax"></param>
        /// <returns></returns>
        public CondMatch? FindConditionOf(List<Statement> stms, int iStm, Expression exp)
        {
            if (exp is Identifier idLo)
            {
                foreach (var use in ssa.Identifiers[idLo].Uses)
                {
                    var m = condm.Match(use.Instruction);
                    if (!m.Success)
                        continue;
                    var grf = (Identifier)m.CapturedExpression("grf")!;
                    var condExp = m.CapturedExpression("exp");
                    if (grf.Storage is FlagGroupStorage && exp == condExp)
                    {
                        return new CondMatch(grf, exp, use, 0);
                    }
                }
            }
            for (int i = iStm + 1; i < stms.Count; ++i)
            {
                var m = condm.Match(stms[i].Instruction);
                if (!m.Success)
                    continue;
                var grf = (Identifier)m.CapturedExpression("grf")!;
                var condExp = m.CapturedExpression("exp");
                if (grf.Storage is FlagGroupStorage && exp == condExp)
                {
                    return new CondMatch(grf, exp, null!, i);
                }
            }
            return null;
        }

        private Expression CreateWideExpression(Expression expLo, Expression expHi, DataType totalSize)
        {
            if (expLo is Identifier idLo && expHi is Identifier idHi)
            {
                // If the high part is a zeroed identifier, we are dealing with 
                // a zero-extended value.
                if (ssa.Identifiers[idHi].DefStatement?.Instruction is Assignment ass &&
                    ass.Src.IsZero)
                {
                    var dt = PrimitiveType.Create(Domain.UnsignedInt, totalSize.BitSize);
                    return new Conversion(idLo, idLo.DataType, dt);
                }
                return ssa.Procedure.Frame.EnsureSequence(totalSize, idHi.Storage, idLo.Storage);
            }
            if (expLo is MemoryAccess memDstLo && expHi is MemoryAccess memDstHi && 
                MemoryOperandsAdjacent(ssa.Procedure.Architecture, memDstLo, memDstHi))
            {
                return CreateMemoryAccess(memDstLo, totalSize);
            }
            if (expLo is Constant immLo && expHi is Constant immHi)
            {
                return Constant.Create(totalSize, (immHi.ToUInt64() << expLo.DataType.BitSize) | immLo.ToUInt32());
            }
            return new MkSequence(totalSize, expHi, expLo);
        }

        private static Expression CreateMemoryAccess(MemoryAccess mem, DataType totalSize)
        {
            return new MemoryAccess(mem.MemoryId, mem.EffectiveAddress, totalSize);
        }

        public class CondMatch
        {
            public CondMatch(
                Identifier grf,
                Expression src,
                Statement stm,
                int index)
            {
                this.FlagGroup = grf;
                this.src = src;
                this.Statement = stm;
                this.StatementIndex = index;
            }

            public readonly Identifier FlagGroup;
            public readonly Expression src;
            public readonly Statement Statement;
            public readonly int StatementIndex;
        }

        public static bool MemoryOperandsAdjacent(IProcessorArchitecture arch, MemoryAccess m1, MemoryAccess m2)
        {
            var off1 = GetOffset(m1);
            var off2 = GetOffset(m2);
            if (off1 is null || off2 is null)
                return false;
            return arch.Endianness.OffsetsAdjacent(off1.ToInt64(), off2.ToInt64(), m1.DataType.Size);
        }

        private static Constant? GetOffset(MemoryAccess access)
        {
            var match = memOffset.Match(access);
            if (match.Success)
            {
                return (Constant)match.CapturedExpression("Offset")!;
            }
            match = segMemOffset.Match(access);
            if (match.Success)
            {
                return (Constant)match.CapturedExpression("Offset")!;
            }
            if (access.EffectiveAddress is Constant c)
                return c;
            return null;
        }

        /// <summary>
        /// Matches an "ADC" or "SBB/SBC" pattern.
        /// </summary>
        /// <param name="instr"></param>
        /// <returns>If the match succeeded, returns a partial BinaryExpression
        /// with the left and right side of the ADC/SBC instruction.</returns>
        public AddSubCandidate? MatchAdcSbc(Statement stm)
        {
            var m = adcPattern.Match(stm.Instruction);
            if (m.Success)
            {
                if (!IsCarryFlag(m.CapturedExpression("cf")))
                    return null;
                var op = m.CapturedOperator("op2");
                if (op is null || !op.Type.IsAddOrSub())
                    return null;
                return new AddSubCandidate(
                    op,
                    m.CapturedExpression("left")!,
                    m.CapturedExpression("right")!)
                {
                    Dst = m.CapturedExpression("dst")!,
                    Statement = stm
                };
            }
            m = addPattern.Match(stm.Instruction);
            if (m.Success)
            {
                if (!IsCarryFlag(m.CapturedExpression("right")))
                    return null;
                var op = m.CapturedOperator("op");
                if (op is null || !op.Type.IsAddOrSub())
                    return null;
                var dst = m.CapturedExpression("dst")!;
                var left = m.CapturedExpression("left")!;
                AddSubCandidate? pdp11dst = IsPdp11StyleAdcSbc(op, dst, left);
                if (pdp11dst is not null)
                    return pdp11dst;
                return new AddSubCandidate(
                    op,
                    left,
                    Constant.Zero(left.DataType))
                {
                    Dst = dst,
                    Statement = stm
                };
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Checks to see if this is a PDP-11 style long add.
        /// </summary>
        /// <param name="op">IAdd or ISub</param>
        /// <param name="dst">Possible intermediate step</param>
        /// <param name="left">intermediate addend.</param>
        /// <returns></returns>
        /// <remarks>
        /// The PDP-11 long add sequence looks like this:
        ///     add r1,r2
        ///     adc r3
        ///     add r3,r4
        /// On entry we 
        /// </remarks>
        private AddSubCandidate? IsPdp11StyleAdcSbc(Operator op, Expression dst, Expression left)
        {
            if (dst is not Identifier idIntermediate)
                return null;
            var uses = ssa.Identifiers[idIntermediate].Uses;
            foreach (var use in uses)
            {
                var m = addPattern.Match(use.Instruction);
                if (m.Success)
                {
                    var dstFinal = m.CapturedExpression("dst");
                    var opFinal = m.CapturedOperator("op");
                    var leftFinal = m.CapturedExpression("left");
                    var rightFinal = m.CapturedExpression("right")!;
                    if (leftFinal == idIntermediate && op == opFinal)
                    {
                        return new AddSubCandidate(op, left, rightFinal)
                        {
                            Dst = dstFinal,
                            Statement = use
                        };
                    }
                }
            }
            return null;
        }

        public AddSubCandidate? MatchAddSub(Statement stm)
        {
            var m = addPattern.Match(stm.Instruction);
            if (!m.Success)
                return null;
            var op = m.CapturedOperator("op")!;
            if (!op.Type.IsAddOrSub())
                return null;
            return new AddSubCandidate(
                op,
                m.CapturedExpression("left")!,
                m.CapturedExpression("right")!)
            {
                Dst = m.CapturedExpression("dst")!,
            };
        }
    }

    public class AddSubCandidate
    {
        public AddSubCandidate(Operator op, Expression left, Expression right)
        {
            this.Op = op;
            this.Left = left;
            this.Right = right;
        }

        public int StatementIndex;
        public Statement? Statement;
        public Expression? Dst;
        public readonly Operator Op;
        public readonly Expression Left;
        public readonly Expression Right;
    }
}
