#region License
/* 
 * Copyright (C) 1999-2023 John Källén.
 .
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
using Reko.Core.Diagnostics;
using Reko.Core.Expressions;
using Reko.Core.Rtl;
using Reko.Evaluation;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Reko.Scanning
{
    /// <summary>
    /// This class processes a linear sequence of instructions, resulting in
    /// an <see cref="RtlBlock"/>
    /// </summary>
    public class BlockWorker
    {
        private static readonly TraceSwitch log = new TraceSwitch(nameof(BlockWorker), "")
        {
            Level = TraceLevel.Warning
        };

        private readonly AbstractScanner scanner;
        private readonly AbstractProcedureWorker worker;
        private readonly ProcessorState state;
        private readonly ExpressionSimplifier eval;
        private readonly InstrClass rejectMask; // Instruction class bits, which when present cause 
            // an exception to happen.

        public BlockWorker(
            AbstractScanner scanner,
            AbstractProcedureWorker worker,
            Address address,
            IEnumerator<RtlInstructionCluster> trace,
            ProcessorState state,
            InstrClass rejectMask)
        {
            this.scanner = scanner;
            this.worker = worker;
            this.Address = address;
            this.Trace = trace;
            this.state = state;
            this.eval = scanner.CreateEvaluator(state);
            this.rejectMask = rejectMask;
        }

        /// <summary>
        /// Address of the start of the current block being parsed.
        /// </summary>
        public Address Address { get; private set; }

        /// <summary>
        /// Trace of RTL instructions being read from the binary.
        /// </summary>
        public IEnumerator<RtlInstructionCluster> Trace { get; }

        /// <summary>
        /// Performs a linear scan, stopping when a CTI is encountered, or we
        /// run off the end of a trace (denoting invalid code).
        /// </summary>
        /// <returns>
        /// A pair of a completed <see cref="RtlBlock"/> and an updated <see cref="ProcessorState"/>.
        /// If parsing runs off the end of the trace, the returned block will be
        /// marked as invalid.
        /// </returns>
        public (RtlBlock?, ProcessorState) ParseBlock()
        {
            var instrs = new List<RtlInstructionCluster>();
            var trace = this.Trace;
            var state = this.state;
            var addrLast = this.Address;
            log.Inform("ParseBlock({0})", this.Address);
            while (trace.MoveNext())
            {
                var cluster = trace.Current;
                addrLast = cluster.Address;
                if (!worker.TryMarkVisited(addrLast))
                {
                    // Another trace has already visited this address,
                    // or we're out of bounds.
                    // Find the block that other trace resulted in, and 
                    // split it at this address.
                    var blockNew = worker.SplitExistingBlock(addrLast);
                    if (blockNew is null)
                    {
                        // Couldn't split a block; it means we're outside of our 
                        // scanning area, so this whole block is invalid.
                        log.Verbose("    Unable to find instruction at {0}, stopping", addrLast);
                        return (null, state);
                    }
                    log.Verbose("    Fell through to {0}, stopping", cluster.Address);
                    var block = MakeFallthroughBlock(this.Address, addrLast, instrs);
                    return (block, state);
                }
                if ((cluster.Class & this.rejectMask) != 0)
                {
                    instrs.Add(new RtlInstructionCluster(cluster.Address, cluster.Length, new RtlInvalid()));
                    var size = addrLast - this.Address + cluster.Length;
                    return (MakeInvalidBlock(instrs, size), state);
                }
                foreach (var rtl in cluster.Instructions)
                {
                    log.Verbose("      {0}: {1}", cluster.Address, rtl);
                    switch (rtl)
                    {
                    case RtlAssignment ass:
                        EmulateState(ass);
                        continue;
                    case RtlSideEffect side:
                        if (HandleSideEffect(side, state))
                        { 
                            return MakeBlock(instrs, state, side);
                        }
                        continue;
                    case RtlNop _:
                        continue;
                    case RtlInvalid _:
                        instrs.Add(cluster);
                        var size = addrLast - this.Address + cluster.Length;
                        return (MakeInvalidBlock(instrs, size), state);
                    case RtlBranch branch:
                        //Expand sub-instruction statements in a later pass.
                        if (branch.NextStatementRequiresLabel)
                        {
                            continue;
                        }
                        break;
                    case RtlGoto g:
                    case RtlCall call:
                    case RtlReturn ret:
                        break;
                    default:
                        throw new NotImplementedException($"{rtl.GetType()} - not implemented");
                    }
                    Debug.Assert(rtl.Class.HasFlag(InstrClass.Transfer));
                    if (IsPaddingBoundary(cluster, instrs))
                    {
                        var block = MakeFallthroughBlock(this.Address, cluster.Address, instrs);
                        scanner.RegisterEdge(new Edge(this.Address, cluster.Address, EdgeType.Fallthrough));
                        if (!scanner.TryRegisterBlockStart(cluster.Address, this.Address))
                            return (block, state);
                        this.Address = cluster.Address;
                        instrs = new List<RtlInstructionCluster>();
                    }
                    return MakeBlock(instrs, state, rtl);
                }
                if (IsPaddingBoundary(cluster, instrs))
                {
                    var block = MakeFallthroughBlock(this.Address, cluster.Address, instrs);
                    scanner.RegisterEdge(new Edge(this.Address, cluster.Address, EdgeType.Fallthrough));
                    if (!scanner.TryRegisterBlockStart(cluster.Address, this.Address))
                        return (block, state);
                    this.Address = cluster.Address;
                    instrs = new List<RtlInstructionCluster>();
                }
                instrs.Add(cluster);
            }
            // Fell off the end of the trace, mark as bad.
            var length = addrLast - this.Address;
            return (MakeInvalidBlock(instrs, length), state);
        }

        /// <summary>
        /// Returns true if there is a transition between padding instructions
        /// and non-padding instructions between the last RTL cluster in 
        /// <paramref name="instrs"/> and <paramref name="cluster"/> .
        /// </summary>
        private static bool IsPaddingBoundary(RtlInstructionCluster cluster, List<RtlInstructionCluster> instrs)
        {
            if (instrs.Count == 0)
                return false;
            // If the last instruction in instrs was padding or a zero
            // instruction, the next one (in cluster) must also be padding
            // or zero, otherwise we start a new block.
            var change = cluster.Class ^ instrs[^1].Class;
            return (change & (InstrClass.Padding | InstrClass.Zero)) != 0;
        }

        /// <summary>
        /// Return true if the <paramref name="side" /> instruction
        /// diverges.
        /// </summary>
        private bool HandleSideEffect(RtlSideEffect side, ProcessorState state)
        {
            //$TODO: emulate side effect.

            return side.Class.HasFlag(InstrClass.Terminates);
        }

        /// <summary>
        /// Creates an <see cref="RtlBlock"/> that falls through to whatever
        /// instruction is at <paramref name="addrFallthrough"/>.
        /// </summary>
        /// <param name="addrFallthrough">The address at which the block ends.
        /// <param name="instrs"></param>
        /// <returns>A block to fall through to.</returns>
        private RtlBlock MakeFallthroughBlock(Address addrBegin, Address addrFallthrough, List<RtlInstructionCluster> instrs)
        {
            return scanner.RegisterBlock(
                this.state.Architecture,
                addrBegin,
                addrFallthrough - this.Address,
                addrFallthrough,
                instrs);
        }

        private void EmulateState(RtlAssignment ass)
        {
            try
            {
                var value = GetValue(ass.Src);
                switch (ass.Dst)
                {
                case Identifier id:
                    state.SetValue(id, value);
                    return;
                case MemoryAccess mem:
                    if (mem.EffectiveAddress is SegmentedPointer segptr)
                    {
                        state.SetValueEa(segptr.BasePointer, GetValue(segptr.Offset), value);
                    }
                    else
                    {
                        state.SetValueEa(GetValue(mem.EffectiveAddress), value);
                    }
                    return;
                }
            }
            catch
            {
                // Drop all exceptions on the floor.
            }
        }

        private Expression GetValue(Expression e)
        {
            var (value, _) = e.Accept(eval);
            return value;
        }


        /// <summary>
        /// The <see cref="Trace"/> is positioned on a CTI with a delay slot.
        /// We swap the positions of the delay slot instruction and the CTI
        /// so that the CTI appears last in the block.
        /// </summary>
        /// <param name="rtlTransfer">The CTI instruction.</param>
        /// <param name="instrs"></param>
        /// <returns>False if there was no next instruction, or if another CTI
        /// was found in the first CTI delay slot. Reko currently doesn't
        /// handle this rare idiom, although SPARC does allow it.
        /// </returns>
        protected bool TryStealDelaySlot(
            RtlInstructionCluster rtlTransfer,
            List<RtlInstructionCluster> instrs)
        {
            if (!Trace.MoveNext())
            {
                // Fell off the end of memory, CTI is an invalid instruction.
                return false;
            }
            var rtlDelayed = Trace.Current;
            if (rtlDelayed.Class.HasFlag(InstrClass.Transfer))
            {
                // Can't deal with transfer functions in delay slots yet.
                return false;
            }
            // If the delay slot instruction is a nop (padding), we 
            // can ignore it.
            if (!rtlDelayed.Class.HasFlag(InstrClass.Padding))
            {
                // Delay slot instruction does actual work, so we will insert it
                // before the CTI instruction.
                switch (rtlTransfer.Instructions[^1])
                {
                case RtlBranch branch:
                    if (branch.Condition is not Constant)
                    {
                        var (tmp, copy) = worker.MkTmp(rtlTransfer, branch.Condition);
                        instrs.Add(copy);

                        rtlTransfer = new RtlInstructionCluster(
                            rtlTransfer.Address,
                            rtlTransfer.Length,
                            new RtlBranch(tmp, (Address)branch.Target, InstrClass.ConditionalTransfer));
                    }
                    break;
                case RtlGoto g:
                    if (g.Target is not Core.Address)
                    {
                        var (tmp, copy) = worker.MkTmp(rtlTransfer, g.Target);
                        instrs.Add(copy);
                        rtlTransfer = new RtlInstructionCluster(
                            rtlTransfer.Address,
                            rtlTransfer.Length,
                            new RtlGoto(tmp, InstrClass.Transfer));
                    }
                    break;
                case RtlCall call:
                    if (call.Target is not Core.Address)
                    {
                        var (tmp, copy) = worker.MkTmp(rtlTransfer, call.Target);
                        instrs.Add(copy);
                        rtlTransfer = new RtlInstructionCluster(
                            rtlTransfer.Address,
                            rtlTransfer.Length,
                            new RtlCall(tmp, (byte)call.ReturnAddressSize, InstrClass.Transfer | InstrClass.Call));
                    }
                    break;
                case RtlReturn:
                    break;
                default:
                    throw new NotImplementedException($"{rtlTransfer.GetType().Name} - not implemented.");
                }
                // "Steal" the delay slot; we give the delay slot instruction the 
                // same address as the transfer instruction so that the instr addresses
                // in the basic block remain ordered.
                instrs.Add(new RtlInstructionCluster(
                    rtlTransfer.Address,
                    rtlTransfer.Length,
                    rtlDelayed.Instructions));
            }
            instrs.Add(rtlTransfer);
            return true;
        }

        /// <summary>
        /// After reaching a CTI, make an <see cref="RtlBlock"/>.
        /// </summary>
        /// <param name="instrs">The instructions of the resulting <see cref="RtlBlock"/>.</param>
        /// <param name="state">The current <see cref="ProcessorState"/>.</param>
        /// <param name="rtlLast">The instruction that triggered
        /// the creation of the block.</param>
        /// <returns>
        /// A pair of a completed <see cref="Block"/> and an updated <see cref="ProcessorState"/>.
        /// If parsing runs off the end of the trace, the block reference will be null.
        /// </returns>
        private (RtlBlock?, ProcessorState) MakeBlock(
            List<RtlInstructionCluster> instrs,
            ProcessorState state,
            RtlInstruction rtlLast)
        {
            // We are positioned at the CTI.
            var cluster = this.Trace.Current;

            var size = cluster.Address - this.Address + cluster.Length;

            // Make sure we're not heading to hyperspace.
            if (rtlLast is RtlTransfer xfer &&
                xfer.Target is Address addrTarget &&
                !scanner.IsExecutableAddress(addrTarget))
            {
                instrs.Add(new RtlInstructionCluster(cluster.Address, cluster.Length, new RtlInvalid()));
                return (MakeInvalidBlock(instrs, size), state);
            }

            if (rtlLast.Class.HasFlag(InstrClass.Delay))
            {
                if (!TryStealDelaySlot(cluster, instrs))
                    return (MakeInvalidBlock(instrs, size), state);
            }
            else
            {
                instrs.Add(cluster);
            }
            // The trace may have moved if a delay slot was consumed.
            cluster = this.Trace.Current;
            var addrFallthrough = cluster.Address + cluster.Length;
            var block = scanner.RegisterBlock(
                this.state.Architecture,
                this.Address,
                size,
                addrFallthrough,
                instrs);
            return (block, state);
        }

        /// <summary>
        /// Creates an invalid block, but only if the <paramref name="size"/> 
        /// is larger than zero.
        /// </summary>
        /// <returns>Null if the size was zero, otherwise a block ending 
        /// with the invalid instruction.
        /// </returns>
        private RtlBlock? MakeInvalidBlock(
            List<RtlInstructionCluster> instrs,
            long size)
        {
            var arch = this.state.Architecture;
            if (size <= 0 || instrs.Count == 0)
            {
                size = arch.InstructionBitSize / arch.MemoryGranularity;
                instrs.Add(new RtlInstructionCluster(this.Address, (int)size, new RtlInvalid()));
            }
            var block = scanner.RegisterBlock(
                arch,
                this.Address,
                size,
                this.Address + size,
                instrs);
            block.IsValid = false;
            return block;
        }
    }
}
