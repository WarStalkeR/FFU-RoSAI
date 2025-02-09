using Arcen.Universal;
using Arcen.HotM.Core;
using System;
using Arcen.HotM.Visualization;
using Arcen.HotM.External;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public static class InputWindowCutthrough
    {
        public static bool HandleKey( string InputActionID )
        {
            if ( ArcenInput.IsInputBlockedForAmountOfTime )
                return false;

            switch ( InputActionID )
            {
                case "ShowPerformanceStats":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                    {
                        VisCommands.ShowPerformance();
                        ArcenInput.BlockForAJustPartOfOneSecond();
                        return true;
                    }
                    return false;
                case "ShowSFXTestWindow":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                    {
                        if ( Window_SFXTestWindow.Instance.IsOpen )
                            Window_SFXTestWindow.Instance.Close( WindowCloseReason.UserDirectRequest );
                        else
                            Window_SFXTestWindow.Instance.Open();
                        ArcenInput.BlockForAJustPartOfOneSecond();
                        return true;
                    }
                    return false;
                case "ShowParticleTestWindow":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                    {
                        if ( Window_ParticleTestWindow.Instance.IsOpen )
                            Window_ParticleTestWindow.Instance.Close( WindowCloseReason.UserDirectRequest );
                        else
                            Window_ParticleTestWindow.Instance.Open();
                        ArcenInput.BlockForAJustPartOfOneSecond();
                        return true;
                    }
                    return false;
                case "SkipToNextMusicTrack":
                    if ( ArcenMusicPlayer.Instance != null )
                    {
                        ArcenMusicPlayer.Instance.CurrentOneShotOverridingMusicTrackPlaying = null;
                        ArcenMusicPlayer.Instance.CurrentSecondaryMusicTrackPlaying = null;
                        ArcenMusicPlayer.Instance.CurrentPrimaryMusicTrackPlaying = null;
                        ArcenMusicPlayer.Instance.RemainingTimeAfterCurrentPrimaryTrackPlays = 0f;
                        ArcenInput.BlockForAJustPartOfOneSecond();
                        return true;
                    }
                    return false;
                case "QuitToMainMenu":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor && !VisCurrent.ShouldDrawLoadingMenuBuildings )
                    {
                        VisCommands.HandleQuitToMainMenu();
                        return true;
                    }
                    return false;
                case "ExitToOS":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                    {
                        VisCommands.HandleExitToOS();
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }
    }
}