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
using Reko.Core.Loading;
using Reko.Core.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reko.Loading
{
    /// <summary>
    /// The NullLoader is used when Reko is unable to determine what image
    /// loader to use. It doesn't support disassembly.
    /// </summary>
    public class NullImageLoader : ProgramImageLoader
    {
        private Address baseAddr;
        private readonly byte[] imageBytes;

        public NullImageLoader(IServiceProvider services, ImageLocation imageLocation, byte[] image) : base(services, imageLocation, image)
        {
            this.imageBytes = image;
            this.baseAddr = Address.Ptr32(0);
            this.EntryPoints = new List<ImageSymbol>();
        }

        public IProcessorArchitecture? Architecture { get; set; }
        public List<ImageSymbol> EntryPoints { get; private set; }
        public IPlatform? Platform { get; set; }
        public override Address PreferredBaseAddress
        {
            get { return this.baseAddr; }
            set { this.baseAddr = value; }
        }

        public override Program LoadProgram(Address? addrLoad)
        {
            if (Architecture is null)
                throw new InvalidOperationException("A processor architecture must be specified.");
            if (addrLoad is null)
                addrLoad = PreferredBaseAddress;
            var platform = Platform ?? new DefaultPlatform(Services, Architecture);
            return LoadProgram(addrLoad, Architecture, platform, new());
        }

        public override Program LoadProgram(
            Address addrLoad,
            IProcessorArchitecture arch,
            IPlatform platform,
            List<UserSegment> userSegments)
        {
            var segmentMap = CreatePlatformSegmentMap(platform, addrLoad, userSegments, imageBytes);
            var program = new Program(
                segmentMap,
                arch,
                platform);
            return program;
        }

        public SegmentMap CreatePlatformSegmentMap(
            IPlatform platform,
            Address loadAddr,
            List<UserSegment> userSegments,
            byte[] rawBytes)
        {
            var segmentMap = platform.CreateAbsoluteMemoryMap() ?? new SegmentMap(loadAddr);
            var mem = new ByteMemoryArea(loadAddr, rawBytes);
            if (userSegments.Any(us => us.Address is not null))
            {
                foreach (var useg in userSegments)
                {
                    //$TODO: warning?
                    if (useg.Address is null)
                        continue;
                    var name = useg.Name ?? useg.Address.GenerateName("seg", "");
                    var seg = new ImageSegment(name, useg.Address, mem, useg.AccessMode);
                    seg.Size = useg.Length;
                    segmentMap.AddSegment(seg);
                }
            }
            else
            {
                segmentMap.AddSegment(mem, "code", AccessMode.ReadWriteExecute);
            }
            return segmentMap;
        }
    }
}
