using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class JobsStorage : IMachineJobImplementation
    {
        public JobResult TryHandleJob( MachineStructure Structure, MachineJob Job, ArcenCharacterBufferBase BufferOrNull, JobLogic Logic, MersenneTwister RandOrNull )
        {
            int debugStage = 0;
            try
            {
                switch ( Job.ID )
                {
                    case "HumanApartments":
                    case "ResidentialMegastructure":
                        JobHelper.HandleFilth( Structure, Job, Logic, RandOrNull, false );
                        JobHelper.HandleVRDayUseSeats( Structure, Job, Logic, RandOrNull );
                        break;
                    case "RefugeeTower":
                        JobHelper.HandleFilth( Structure, Job, Logic, RandOrNull, true );
                        break;
                    case "SmallHumanSafehouse":
                        //no filth or VR, this is not that kind of place.
                        break;
                    case "MolecularGeneticsLab":
                    case "ForensicGeneticsLab":
                    case "ZoologyLab":
                    case "MedicalPractice":
                    case "VeterinaryPractice":
                    case "BotanyLab":
                    case "BionicEngineeringStudio":
                    case "EpidemiologyLab":
                    case "NeuroscienceLab":
                        JobHelper.HandleScientificResearch( Structure, Job, Logic, RandOrNull );
                        break;
                }

                return JobResult.Success; //nothing to do here on most storage jobs
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "JobsStorage HandleJob error: " + Logic + " " + (Job?.ID ?? "[null]"), debugStage, e, Verbosity.ShowAsError );
                return JobResult.Indeterminate;
            }
        }

        public bool HandleJobSpecialty( MachineStructure Structure, MachineJob Job, ArcenCharacterBufferBase BufferOrNull, JobSpecialtyLogic Logic,
            out MinHeight TooltipMinHeight, out TooltipWidth Width )
        {
            TooltipMinHeight = MinHeight.Any;
            Width = TooltipWidth.Wide;
            return false; //nothing to do here
        }

        public bool HandleJobActivationLogic( MachineStructure Structure, MachineJob Job, ArcenCharacterBufferBase BufferOrNull, JobActivationLogic Logic,
            out MinHeight TooltipMinHeight, out TooltipWidth Width )
        {
            TooltipMinHeight = MinHeight.Any;
            Width = TooltipWidth.Wide;
            return false; //nothing to do here
        }

        public bool HandleJobDeletionLogic( MachineStructure Structure, MachineJob Job, JobDeletionLogic Logic, Action ActionOrNull, bool IsFromBlowingUp )
        {
            if ( Structure == null || Job == null )
                return false;

            int debugStage = 0;
            try
            {
                switch ( Job.ID )
                {
                    case "HumanApartments":
                    case "ResidentialMegastructure":
                    case "RefugeeTower":
                        #region Human Residences
                        {
                            switch ( Logic )
                            {
                                case JobDeletionLogic.HandleDeletionLogic:
                                    if ( Structure.ResourceStorageAmountHasBeenProviding1_Effective > 0 )
                                    {
                                        ResourceType resource = Job.CapIncrease1.Resource;
                                        if ( resource == null )
                                            break;
                                        //remove our cap early, so we don't have to wait for the next turn tick
                                        resource.AlterMostSoftCap_ForBetweenTurns_ReallyDangerousSoBeCareful( -Structure.ResourceStorageAmountHasBeenProviding1_Effective );
                                        resource.AlterMidSoftCap_ForBetweenTurns_ReallyDangerousSoBeCareful( -Structure.ResourceStorageAmountHasBeenProviding1_Effective );
                                        resource.AlterHardCap_ForBetweenTurns_ReallyDangerousSoBeCareful( -Structure.ResourceStorageAmountHasBeenProviding1_Effective );
                                        //then if we have an overage, go ahead and handle this instantly also
                                        resource.EnforceCapOverageNow_OnlyOncePerTurn(); //this is an exception, I suppose
                                    }
                                    break;
                                case JobDeletionLogic.IsBlockedFromDeletion:
                                    if ( SimMetagame.CurrentChapterNumber == 1 )
                                    {
                                        ResourceType resource = Job.CapIncrease1.Resource;
                                        if ( resource == null )
                                            return false; //let it happen

                                        if ( Structure.ResourceStorageAmountHasBeenProviding1_Effective <= resource.EffectiveHardCapStorageAvailable )
                                            return false; //they won't become homeless, it's fine
                                        if ( resource == ResourceRefs.ShelteredHumans )
                                        {
                                            if ( Structure.ResourceStorageAmountHasBeenProviding1_Effective <= ResourceRefs.RefugeeHumans.EffectiveHardCapStorageAvailable )
                                                return false; //they won't become homeless, it's fine
                                        }

                                        if ( Job.Nonsim_SkipScrapBlockedNoteUntil < ArcenTime.AnyTimeSinceStartF )
                                        {
                                            Job.Nonsim_SkipScrapBlockedNoteUntil = ArcenTime.AnyTimeSinceStartF + 3f;
                                            ArcenNotes.SendNoteToGameOnly( 50, LocalizedString.StartColor_New( ColorTheme.RedOrange2 )
                                                .AddLangAndAfterLineItemHeader( "ActionBlocked" ).EndColor()
                                                .AddLang( "NotInTheMoodToKickOutHumanTenants" ), NoteStyle.ShowInPassing, 3 );
                                        }
                                        return true; //block it
                                    }
                                    break;
                                case JobDeletionLogic.ShouldHaveDeletionPrompt:
                                    if ( SimMetagame.CurrentChapterNumber != 1 )
                                    {
                                        if ( Structure.ResourceStorageAmountHasBeenProviding1_Effective > 0 &&
                                            Job.CapIncrease1.Resource.Current >= Job.CapIncrease1.Resource.HardCap - Structure.ResourceStorageAmountHasBeenProviding1_Effective )
                                            return true; //if we were providing active housing, and this would make some people lose housing, then make the player get a prompt
                                    }
                                    break;
                                case JobDeletionLogic.HandleDeletionPrompt:
                                    if ( SimMetagame.CurrentChapterNumber != 1 )
                                    {
                                        int peopleThatWouldBeAbandoned = (int)Job.CapIncrease1.Resource.Current - (int)(Job.CapIncrease1.Resource.HardCap -
                                            Structure.ResourceStorageAmountHasBeenProviding1_Effective);

                                        if ( peopleThatWouldBeAbandoned > 0 )
                                        {
                                            ModalPopupData.CreateAndLogYesNoStyle( PopupYesNoStyle.Small, delegate
                                            {
                                                ActionOrNull();
                                            }, null, LocalizedString.AddFormat1_New( "DeleteStructure_Header", Structure.GetDisplayName() ),
                                                LocalizedString.AddFormat1_New( "DeleteStructure_BodyAbandoningHumanTenants", peopleThatWouldBeAbandoned.ToStringThousandsWhole() ),
                                                LangCommon.Popup_Common_Yes.LocalizedString, LangCommon.Popup_Common_NoWait.LocalizedString );
                                        }
                                        else
                                        {
                                            //oh, guess the situation shifted since ShouldHaveDeletionPrompt was true
                                            //so do the regular deleting check
                                            ModalPopupData.CreateAndLogYesNoStyle( PopupYesNoStyle.Small, delegate
                                            {
                                                ActionOrNull();
                                            }, null, LocalizedString.AddFormat1_New( "DeleteStructure_Header", Structure.GetDisplayName() ),
                                                LocalizedString.AddLang_New( "DeleteStructure_BodyRegular" ),
                                                LangCommon.Popup_Common_Yes.LocalizedString, LangCommon.Popup_Common_NoWait.LocalizedString );
                                        }
                                    }
                                    break;
                            }

                            return false;
                        }
                    #endregion
                    case "SmallHumanSafehouse":
                        {
                            switch ( Logic )
                            {
                                case JobDeletionLogic.IsBlockedFromDeletion:
                                    {
                                        ResourceType resource = Job.CapIncrease1.Resource;
                                        if ( resource == null )
                                            return false; //let it happen

                                        if ( Structure.ResourceStorageAmountHasBeenProviding1_Effective <= resource.EffectiveHardCapStorageAvailable )
                                            return false; //they won't become homeless, it's fine

                                        if ( Job.Nonsim_SkipScrapBlockedNoteUntil < ArcenTime.AnyTimeSinceStartF )
                                        {
                                            Job.Nonsim_SkipScrapBlockedNoteUntil = ArcenTime.AnyTimeSinceStartF + 3f;
                                            ArcenNotes.SendNoteToGameOnly( 50, LocalizedString.StartColor_New( ColorTheme.RedOrange2 )
                                                .AddLangAndAfterLineItemHeader( "ActionBlocked" ).EndColor()
                                                .AddLang( "NotInTheMoodToKickOutHumanTenants" ), NoteStyle.ShowInPassing, 3 );
                                        }
                                        return true; //block it
                                    }
                            }
                            return false;
                        }
                    default:
                        #region Basic Storage
                        {
                            switch ( Logic )
                            {
                                case JobDeletionLogic.HandleDeletionLogic:
                                    JobHelper.HandleLossOfStorageFromJob( Structure, Job, IsFromBlowingUp );
                                    break;
                            }
                            return false;
                        }
                        #endregion
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "JobsStorage HandleJobDeletionLogic error: " + Logic + " " + (Job?.ID ?? "[null]"), debugStage, e, Verbosity.ShowAsError );
                return false;
            }
        }
    }
}
