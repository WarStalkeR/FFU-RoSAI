using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class OuterRadialOptions : IOuterRadialOptionImplementation
    {
        public void TryHandleOuterRadialOption( OuterRadialOption Option, ArcenCharacterBufferBase BufferOrNull, OuterRadialOptionLogic Logic )
        {
            if ( !FlagRefs.HasUnlockedOuterRadialButtons.DuringGameplay_IsTripped )
                return;

            switch ( Option.ID )
            {
                case "GoStraightToNextTurn":
                    {
                        switch ( Logic )
                        {
                            case OuterRadialOptionLogic.HandleLeftClick:
                                VisCommands.HandleGoToNextTurnOrActor( true );
                                break;
                        }
                    }
                    break;
                case "LensFilters":
                    {
                        switch ( Logic )
                        {
                            case OuterRadialOptionLogic.HandleLeftClick:
                                Window_LensFilters.HandleOpenCloseToggle();
                                break;
                        }
                    }
                    break;
                case "TakeAllUnitsOutOfStandby":
                    {
                        switch ( Logic )
                        {
                            case OuterRadialOptionLogic.HandleLeftClick:
                                {
                                    bool didAny = false;
                                    foreach ( ISimMachineActor actor in SimCommon.AllMachineActors.GetDisplayList() )
                                    {
                                        if ( actor.CurrentStandby != StandbyType.None )
                                        {
                                            actor.CurrentStandby = StandbyType.None;
                                            didAny = true;
                                        }
                                    }
                                    if ( !didAny )
                                        ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                                    else
                                        ParticleSoundRefs.StandbyOff.DuringGame_PlaySoundOnlyAtCamera();
                                }
                                break;
                        }
                    }
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "OuterRadialOptions: Called TryHandleOuterRadialOption for '" + Option.ID + "', which does not support it!", Verbosity.ShowAsError );
                    return;
            }
        }
    }
}
