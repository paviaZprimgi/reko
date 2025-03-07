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
using Reko.Gui.ViewModels.Tools;
using Reko.UserInterfaces.AvaloniaUI.ViewModels;
using Reko.UserInterfaces.AvaloniaUI.ViewModels.Tools;
using System;

namespace Reko.UserInterfaces.AvaloniaUI.Services
{
    public class AvaloniaCallGraphNavigatorService : ICallGraphNavigatorService
    {
        private readonly IServiceProvider services;
        private readonly CallGraphNavigatorToolViewModel viewModel;
        private readonly ISelectedAddressService selSvc;
        private Program? program;

        public AvaloniaCallGraphNavigatorService(IServiceProvider services, MainViewModel mainViewModel)
        {
            this.services = services;
            this.viewModel = mainViewModel.CallGraphNavigator;
            this.selSvc = services.RequireService<ISelectedAddressService>();
            this.selSvc.SelectedProcedureChanged += SelSvc_SelectedProcedureChanged;
        }

        public void Show(Program? program, Procedure? proc)
        {
            if (this.program != program)
            {
                this.program = program;
                var callGraph = (program is not null)
                    ? program.CallGraph
                    : new CallGraph();
                this.viewModel.ViewModel = new CallGraphNavigatorViewModel(callGraph, proc);
            }
            else
            {
                this.viewModel.ViewModel.NavigateTo(proc);
            }
        }

        private void SelSvc_SelectedProcedureChanged(object? sender, EventArgs e)
        {
            if (selSvc is null)
                return;
            Show(selSvc.SelectedProgram, selSvc.SelectedProcedure);
        }
    }
}
