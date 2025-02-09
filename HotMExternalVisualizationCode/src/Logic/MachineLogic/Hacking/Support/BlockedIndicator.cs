using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.HotM.ExternalVis;

namespace Arcen.HotM.ExternalVis.Hacking
{
    using h = Window_Hacking;
    using scene = HackingScene;

    public class BlockedIndicator //let this be GC'd
    {
        public h.hCell LastCell;
        public h.iIndicatorBlood Visuals;

        public void Set( h.hCell BlockingCell )
        {
            if ( LastCell == BlockingCell )
                return;  //nothing to do, as it 
            if ( this.Visuals == null )
                this.Visuals = h.iIndicatorBloodPool.GetOrAddEntry();

            this.Visuals.Trans.localPosition = BlockingCell.CenterPos;

            LastCell = BlockingCell;
        }

        public void ClearExistingData()
        {
            if ( this.Visuals != null )
            {
                this.Visuals.ReturnToParentPool();
                this.Visuals = null;
            }

            this.LastCell = null;
        }
    }
}
