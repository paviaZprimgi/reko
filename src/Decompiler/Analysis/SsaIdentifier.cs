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
using Reko.Core.Expressions;
using System;
using System.Collections.Generic;
using System.IO;

namespace Reko.Analysis
{
    /// <summary>
    /// An SSA identifier is actually a node in the SSA graph. The edges of 
    /// the SSA graphs are the uses and definition of a particular SSA
    /// identifier
    /// </summary>
	public class SsaIdentifier
	{
		public SsaIdentifier(Identifier id, Identifier idOrig, Statement? stmDef, bool isSideEffect)
		{
            this.Identifier = id ?? throw new ArgumentNullException(nameof(id));
			this.OriginalIdentifier = idOrig ?? throw new ArgumentNullException(nameof(idOrig));
			this.DefStatement = stmDef!;
            this.IsSideEffect = isSideEffect;
			this.Uses = new List<Statement>();
		}

        /// <summary>
        /// Statement that defines the identifier
        /// </summary>
        public Statement DefStatement { get; set; }        //$TODO: want this to be not-nullable

        /// <summary>
        /// The Identifier itself
        /// </summary>
        public Identifier Identifier { get; private set; }

		public bool IsOriginal
		{
			get { return Identifier == OriginalIdentifier; }
		}

        public bool IsSideEffect { get; private set; }

        /// <summary>
        /// If not null, the induction variable associated with this identifier.
        /// </summary>
        public LinearInductionVariable? InductionVariable { get; set; }

        /// <summary>
        /// The original expression that was rewritten to this identifier.
        /// </summary>
        public Identifier OriginalIdentifier { get; private set; }

		public override string ToString()
		{
			var sb = new StringWriter();
			Write(sb);
			return sb.ToString();
		}

        /// <summary>
        /// _Bag_ of Statements that use the identifier. A bag is needed, 
        /// since the statement a = i * i uses i twice.
        /// </summary>
        public List<Statement> Uses { get; private set; }


        /// <summary>
        /// Find the expression that defines the identifier.
        /// </summary>
        public Expression? GetDefiningExpression()
        {
            switch (this.DefStatement?.Instruction)
            {
            case Assignment ass: return ass.Src;
            case PhiAssignment phi: return phi.Src;
            case CallInstruction call: return call.Callee;
            }
            return null;
        }

        public void Write(TextWriter writer)
		{
			if (IsOriginal)
			{
				writer.Write("{0}:", Identifier);
				OriginalIdentifier.Storage.Write(writer);
			}
			else
			{
				writer.Write("{0}: orig: {1}", Identifier, OriginalIdentifier);
			}
			if (DefStatement != null)
			{
                writer.WriteLine();
				writer.Write("    def:  {0}", DefStatement.Instruction);
			}
			if (Uses.Count > 0)
			{
                bool first = true;
				foreach (Statement u in Uses)
				{
                    writer.WriteLine();
                    if (first)
                    {
                        writer.Write("    uses: ");
                        first = false;
                    }
                    else
                    {
                        writer.Write("          ");
                    }
					writer.Write(u.Instruction);
				}
			}
		}
	}
}
