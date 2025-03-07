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
using Reko.Core.Services;
using Reko.Gui.Services;

namespace Reko.Gui.Design
{
    internal class GlobalVariablesNodeDesigner : TreeNodeDesigner
    {
        private ImageSegment segment;

        public GlobalVariablesNodeDesigner(ImageSegment segment)
        {
            this.segment = segment;
        }

        public override void Initialize(object obj)
        {
            base.Initialize(obj);
            SetTreeNodeProperties(segment);
        }

        private void SetTreeNodeProperties(ImageSegment segment)
        {
            this.TreeNode!.Text = Resources.Node_GlobalVariables;
            this.TreeNode.ImageName = "Data.ico";
        }

        public override void DoDefaultAction()
        {
            var program = Host!.GetAncestorOfType<Program>(this);
            Services!.RequireService<ICodeViewerService>().DisplayGlobals(program, segment);
        }
    }
}
