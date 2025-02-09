using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class JobsWithSpecialties : IMachineJobImplementation
    {
        public JobResult TryHandleJob( MachineStructure Structure, MachineJob Job, ArcenCharacterBufferBase BufferOrNull, JobLogic Logic, MersenneTwister RandOrNull )
        {
            ISimBuilding building = Structure?.Building;
            if ( building == null || Job == null )
                return JobResult.FailAndDestroyJob;
            int debugStage = 0;
            try
            {

                switch ( Job.ID )
                {
                    default:
                        ArcenDebugging.LogSingleLine( "JobsWithSpecialties: Called TryHandleJob for '" + Job.ID + "', which does not support it!", Verbosity.ShowAsError );
                        return JobResult.FailAndDestroyJob;
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "JobsWithSpecialties HandleJob error: " + Logic + " " + (Job?.ID ?? "[null]"), debugStage, e, Verbosity.ShowAsError );
                return JobResult.Indeterminate;
            }
        }

        public bool HandleJobSpecialty( MachineStructure Structure, MachineJob Job, ArcenCharacterBufferBase BufferOrNull, JobSpecialtyLogic Logic,
            out MinHeight TooltipMinHeight, out TooltipWidth Width )
        {
            TooltipMinHeight = MinHeight.Any;
            Width = TooltipWidth.Wide;

            int debugStage = 0;
            try
            {
                switch ( Job.ID )
                {
                    default:
                        ArcenDebugging.LogSingleLine( "JobsWithSpecialties: Called HandleJobSpecialty for '" + Job.ID + "', which does not support it!", Verbosity.ShowAsError );
                        break;
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "JobsWithSpecialties HandleJobSpecialty error: " + Logic + " " + (Job?.ID ?? "[null]"), debugStage, e, Verbosity.ShowAsError );
            }

            return false;
        }

        public bool HandleJobActivationLogic( MachineStructure Structure, MachineJob Job, ArcenCharacterBufferBase BufferOrNull, JobActivationLogic Logic, 
            out MinHeight TooltipMinHeight, out TooltipWidth Width )
        {
            TooltipMinHeight = MinHeight.Any;
            Width = TooltipWidth.Wide;

            int debugStage = 0;
            try
            {
                switch ( Job.ID )
                {
                    default:
                        ArcenDebugging.LogSingleLine( "JobsWithSpecialties: Called HandleJobActivationLogic for '" + Job.ID + "', which does not support it!", Verbosity.ShowAsError );
                        break;
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "JobsWithSpecialties HandleJobActivationLogic error: " + Logic + " " + (Job?.ID ?? "[null]"), debugStage, e, Verbosity.ShowAsError );
            }

            return false;
        }

        public bool HandleJobDeletionLogic( MachineStructure Structure, MachineJob Job, JobDeletionLogic Logic, Action ActionOrNull, bool IsFromBlowingUp )
        {
            int debugStage = 0;
            try
            {
                switch ( Logic )
                {
                    case JobDeletionLogic.HandleDeletionLogic:
                        JobHelper.HandleLossOfStorageFromJob( Structure, Job, IsFromBlowingUp );
                        break;
                }
                return false; //nothing to do here
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "JobsWithSpecialties HandleJobDeletionLogic error: " + Logic + " " + (Job?.ID ?? "[null]"), debugStage, e, Verbosity.ShowAsError );
                return false;
            }
        }
    }
}
