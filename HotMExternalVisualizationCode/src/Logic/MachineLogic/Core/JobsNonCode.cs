using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class JobsNonCode : IMachineJobImplementation
    {
        public JobResult TryHandleJob( MachineStructure Structure, MachineJob Job, ArcenCharacterBufferBase BufferOrNull, JobLogic Logic, MersenneTwister RandOrNull )
        {
            return JobResult.Success; //truly passive and automated, nothing to do with these
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
                ArcenDebugging.LogDebugStageWithStack( "JobsNonCode HandleJobDeletionLogic error: " + Logic + " " + (Job?.ID ?? "[null]"), debugStage, e, Verbosity.ShowAsError );
                return false;
            }
        }
    }
}
