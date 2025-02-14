using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class JobsBasic : IMachineJobImplementation
    {
        public JobResult TryHandleJob( MachineStructure Structure, MachineJob Job, ArcenCharacterBufferBase BufferOrNull, JobLogic Logic, MersenneTwister RandOrNull )
        {
            ISimBuilding jobBuilding = Structure?.Building;
            if ( jobBuilding == null || Job == null )
                return JobResult.FailAndDestroyJob;

            int debugStage = 0;
            try
            {

                switch ( Job.ID )
                {
                    case "RepairSpiders":
                        #region RepairSpiders
                        {
                            if ( Logic == JobLogic.PerQuarterSecondAggregation )
                                return JobResult.Indeterminate;

                            switch ( Logic )
                            {
                                case JobLogic.ExecuteLogic:
                                    {
                                        JobMathInt maxTotalRepairsToBuildingsPerTurn_Obj = Job.MathInts["MaxTotalRepairsToBuildingsPerTurn"];
                                        JobMathInt maxHealingPerUnitPerTurn_Obj = Job.MathInts["MaxHealingPerUnitPerTurn"];

                                        int maxTotalRepairsToBuildingsPerTurn = maxTotalRepairsToBuildingsPerTurn_Obj.GetMin( Structure );
                                        int maxHealingPerUnitPerTurn = maxHealingPerUnitPerTurn_Obj.GetMin( Structure );
                                        int maxUnitsHealedPerTurn = Job.GetSingleIntByID( "MaxUnitsHealedPerTurn", Structure );
                                        float slurryRequiredPerHPRestored = Job.GetSingleFloatByID( "SlurryRequiredPerHPRestored", Structure );

                                        List<ISimMapActor> actorsInRange = Structure.ActorsWithinEffectiveRangeOfThisMachineStructure.GetDisplayList();
                                        List<MachineStructure> structuresInRange = Structure.StructuresWithinEffectiveRangeOfThisMachineStructure.GetDisplayList();

                                        int remainingBuildingsToHeal = maxTotalRepairsToBuildingsPerTurn;
                                        //first look at self
                                        JobHelper.DoStructureRepairsIfNeeded_FromPool( Structure, ref remainingBuildingsToHeal,
                                            slurryRequiredPerHPRestored, "Expense_UsedForStructureRepairs" );

                                        //then look for structures that are not functional
                                        if ( remainingBuildingsToHeal > 0 )
                                        {
                                            foreach ( MachineStructure otherStructure in structuresInRange.GetRandomStartEnumerable( RandOrNull ) )
                                            {
                                                if ( otherStructure.Building == null )
                                                    continue;
                                                if ( otherStructure.Building.GetIsDestroyed() || otherStructure.IsBeingRebuilt )
                                                {

                                                }
                                                else
                                                {
                                                    if ( otherStructure.IsFunctionalStructure )
                                                        continue; //skip any that are functional for now
                                                    if ( otherStructure.IsUnderConstruction )
                                                        continue; //don't do it for ones that are under construction
                                                }
                                                if ( otherStructure.IsBlockedFromAutomatedRebuildingUntilAnotherTurn )
                                                    continue; //skip any that are blocked from automated rebuilding for now
                                                if ( otherStructure == Structure )
                                                    continue; //don't do self multiple times

                                                if ( otherStructure.Building.GetIsDestroyed() && !otherStructure.IsBeingRebuilt )
                                                {
                                                    otherStructure.Building.SetStatus( CommonRefs.UnderConstructionBuildingStatus );
                                                    otherStructure.IsFullDead = false;
                                                    otherStructure.GetActorDataData( ActorRefs.ActorHP, true )?.SetCurrentSilently_BeVeryCarefulWithThis( 1 );
                                                    otherStructure.IsBeingRebuilt = true;
                                                }

                                                JobHelper.DoStructureRepairsIfNeeded_FromPool( otherStructure, ref remainingBuildingsToHeal,
                                                    slurryRequiredPerHPRestored, "Expense_UsedForStructureRepairs" );

                                                JobHelper.HandleStructureAfterRepair( otherStructure );

                                                if ( remainingBuildingsToHeal <= 0 )
                                                    break;
                                            }
                                        }

                                        //then look for structures that are functional
                                        if ( remainingBuildingsToHeal > 0 )
                                        {
                                            foreach ( MachineStructure otherStructure in structuresInRange.GetRandomStartEnumerable( RandOrNull ) )
                                            {
                                                if ( !otherStructure.IsFunctionalStructure )
                                                    continue; //skip any that are not functional, we already checked those
                                                if ( otherStructure.IsUnderConstruction )
                                                    continue; //don't do it for ones that are under construction
                                                if ( otherStructure.IsBlockedFromAutomatedRebuildingUntilAnotherTurn )
                                                    continue; //skip any that are blocked from automated rebuilding for now
                                                if ( otherStructure == Structure )
                                                    continue; //don't do self multiple times

                                                JobHelper.DoStructureRepairsIfNeeded_FromPool( otherStructure, ref remainingBuildingsToHeal,
                                                    slurryRequiredPerHPRestored, "Expense_UsedForStructureRepairs" );

                                                if ( remainingBuildingsToHeal <= 0 )
                                                    break;
                                            }
                                        }

                                        int totalStructureHealingDone = maxTotalRepairsToBuildingsPerTurn - remainingBuildingsToHeal;

                                        int remainingUnitsToHeal = maxUnitsHealedPerTurn;
                                        int totalUnitHealingDone = 0;
                                        foreach ( ISimMapActor actor in actorsInRange.GetRandomStartEnumerable( RandOrNull ) )
                                        {
                                            if ( !(actor is ISimMachineUnit) && !(actor is ISimMachineVehicle) )
                                            {
                                                if ( actor is ISimNPCUnit npcUnit && npcUnit.GetIsAnAllyFromThePlayerPerspective() )
                                                { } //allow healing allies
                                                else
                                                    continue;
                                            }

                                            if ( actor is ISimMachineActor machineActor )
                                            {
                                                if ( machineActor?.CurrentActionOverTime?.Type?.IsUnitInvisibleAndAbsentSeemingWhileActing ?? false )
                                                    continue;
                                            }

                                            int amountRepaired = JobHelper.TryDoActorRepairsIfNeeded( actor, maxHealingPerUnitPerTurn,
                                                slurryRequiredPerHPRestored, "Expense_UsedForUnitRepairs" );
                                            if ( amountRepaired > 0 )
                                            {
                                                //we actually did something
                                                totalUnitHealingDone += amountRepaired;
                                                remainingUnitsToHeal--;
                                            }

                                            if ( remainingUnitsToHeal <= 0 )
                                                break;

                                            //note, the collection already includes these
                                            //if ( actor is ISimMachineVehicle machineVehicle )
                                            //{
                                            //    foreach ( ISimMachineUnit unit in machineVehicle.GetStoredUnits() )
                                            //    {
                                            //        amountRepaired = JobHelper.TryDoActorRepairsIfNeeded( unit, maxHealingPerUnitPerTurn,
                                            //            slurryRequiredPerHPRestored, "Expense_UsedForUnitRepairs" );
                                            //        if ( amountRepaired > 0 )
                                            //        {
                                            //            //we actually did something
                                            //            totalUnitHealingDone += amountRepaired;
                                            //            remainingUnitsToHeal--;
                                            //        }

                                            //        if ( remainingUnitsToHeal <= 0 )
                                            //            break;
                                            //    }
                                            //}
                                        }

                                        if ( totalStructureHealingDone > 0 || totalUnitHealingDone > 0 )
                                        {
                                            ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "WorkByRepairSpiders" ),
                                                NoteStyle.StoredGame, totalStructureHealingDone, totalUnitHealingDone, jobBuilding.GetBuildingID(),
                                                SimCommon.Turn + 15 );

                                            {
                                                Vector3 startLocation = jobBuilding.GetMapItem().TopCenterPoint;
                                                Vector3 endLocation = startLocation.PlusY( 0.6f );

                                                ArcenDoubleCharacterBuffer buffer = JobHelper.GetBufferForJobExecutionThreadOnly();
                                                buffer.AddSpriteStyled_NoIndent( maxTotalRepairsToBuildingsPerTurn_Obj.JobIcon, AdjustedSpriteStyle.InlineSmaller095,
                                                    IconRefs.HealingAmount.DefaultColorHexWithHDRHex );
                                                buffer.Space1x().AddRaw( totalStructureHealingDone.ToStringThousandsWhole(), IconRefs.HealingAmount.DefaultColorHexWithHDRHex );

                                                buffer.Space3x();

                                                buffer.AddSpriteStyled_NoIndent( maxHealingPerUnitPerTurn_Obj.JobIcon, AdjustedSpriteStyle.InlineSmaller095,
                                                    IconRefs.AssistTargetCount.DefaultColorHexWithHDRHex );
                                                buffer.Space1x().AddRaw( totalUnitHealingDone.ToStringThousandsWhole(), IconRefs.AssistTargetCount.DefaultColorHexWithHDRHex );

                                                DamageTextPopups.FloatingDamageTextPopup newDamageText = DamageTextPopups.CreateNewFromTextBuffer( buffer,
                                                    startLocation, endLocation, 0.8f, MathA.Max( 2, GameSettings.Current.GetInt( "MajorActionPopup_LingerTime" ) ) );
                                                newDamageText.PhysicalDamageIncluded = 0;
                                                newDamageText.MoraleDamageIncluded = 0;
                                                newDamageText.SquadDeathsIncluded = 0;
                                            }
                                        }
                                    }
                                    break;
                            }
                            return JobResult.Indeterminate;
                        }
                    #endregion
                    case "RepairCrabs":
                        #region RepairCrabs
                        {
                            if ( Logic == JobLogic.PerQuarterSecondAggregation )
                                return JobResult.Indeterminate;

                            switch ( Logic )
                            {
                                case JobLogic.ExecuteLogic:
                                    {
                                        JobMathInt maxTotalRepairsToBuildingsPerTurn_Obj = Job.MathInts["MaxTotalRepairsToBuildingsPerTurn"];

                                        int maxTotalRepairsToBuildingsPerTurn = maxTotalRepairsToBuildingsPerTurn_Obj.GetMin( Structure );
                                        float slurryRequiredPerHPRestored = Job.GetSingleFloatByID( "SlurryRequiredPerHPRestored", Structure );

                                        List<MachineStructure> structuresInRange = Structure.StructuresWithinEffectiveRangeOfThisMachineStructure.GetDisplayList();

                                        int remainingBuildingsToHeal = maxTotalRepairsToBuildingsPerTurn;
                                        //first look at self
                                        JobHelper.DoStructureRepairsIfNeeded_FromPool( Structure, ref remainingBuildingsToHeal,
                                             slurryRequiredPerHPRestored, "Expense_UsedForStructureRepairs" );

                                        //then look for structures that are not functional
                                        if ( remainingBuildingsToHeal > 0 )
                                        {
                                            foreach ( MachineStructure otherStructure in structuresInRange.GetRandomStartEnumerable( RandOrNull ) )
                                            {
                                                if ( otherStructure.Building == null )
                                                    continue;
                                                if ( otherStructure.Building.GetIsDestroyed() || otherStructure.IsBeingRebuilt )
                                                {

                                                }
                                                else
                                                {
                                                    if ( otherStructure.IsFunctionalStructure )
                                                        continue; //skip any that are functional for now
                                                    if ( otherStructure.IsUnderConstruction )
                                                        continue; //don't do it for ones that are under construction
                                                }
                                                if ( otherStructure.IsBlockedFromAutomatedRebuildingUntilAnotherTurn )
                                                    continue; //skip any that are blocked from automated rebuilding for now
                                                if ( otherStructure == Structure )
                                                    continue; //don't do self multiple times

                                                if ( otherStructure.Building.GetIsDestroyed() && !otherStructure.IsBeingRebuilt )
                                                {
                                                    otherStructure.Building.SetStatus( CommonRefs.UnderConstructionBuildingStatus );
                                                    otherStructure.IsFullDead = false;
                                                    otherStructure.GetActorDataData( ActorRefs.ActorHP, true )?.SetCurrentSilently_BeVeryCarefulWithThis( 1 );
                                                    otherStructure.IsBeingRebuilt = true;
                                                }

                                                JobHelper.DoStructureRepairsIfNeeded_FromPool( otherStructure, ref remainingBuildingsToHeal,
                                                    slurryRequiredPerHPRestored, "Expense_UsedForStructureRepairs" );

                                                JobHelper.HandleStructureAfterRepair( otherStructure );

                                                if ( remainingBuildingsToHeal <= 0 )
                                                    break;
                                            }
                                        }

                                        //then look for structures that are functional
                                        if ( remainingBuildingsToHeal > 0 )
                                        {
                                            foreach ( MachineStructure otherStructure in structuresInRange.GetRandomStartEnumerable( RandOrNull ) )
                                            {
                                                if ( !otherStructure.IsFunctionalStructure )
                                                    continue; //skip any that are not functional, we already checked those
                                                if ( otherStructure.IsUnderConstruction )
                                                    continue; //don't do it for ones that are under construction
                                                if ( otherStructure.IsBlockedFromAutomatedRebuildingUntilAnotherTurn )
                                                    continue; //skip any that are blocked from automated rebuilding for now
                                                if ( otherStructure == Structure )
                                                    continue; //don't do self multiple times

                                                JobHelper.DoStructureRepairsIfNeeded_FromPool( otherStructure, ref remainingBuildingsToHeal,
                                                    slurryRequiredPerHPRestored, "Expense_UsedForStructureRepairs" );

                                                if ( remainingBuildingsToHeal <= 0 )
                                                    break;
                                            }
                                        }

                                        int totalStructureHealingDone = maxTotalRepairsToBuildingsPerTurn - remainingBuildingsToHeal;

                                        if ( totalStructureHealingDone > 0 )
                                        {
                                            ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "WorkByRepairCrabs" ),
                                                NoteStyle.StoredGame, totalStructureHealingDone, 0, jobBuilding.GetBuildingID(),
                                                SimCommon.Turn + 15 );

                                            {
                                                Vector3 startLocation = jobBuilding.GetMapItem().TopCenterPoint;
                                                Vector3 endLocation = startLocation.PlusY( 0.6f );

                                                ArcenDoubleCharacterBuffer buffer = JobHelper.GetBufferForJobExecutionThreadOnly();
                                                buffer.AddSpriteStyled_NoIndent( maxTotalRepairsToBuildingsPerTurn_Obj.JobIcon, AdjustedSpriteStyle.InlineSmaller095,
                                                    IconRefs.HealingAmount.DefaultColorHexWithHDRHex );
                                                buffer.Space1x().AddRaw( totalStructureHealingDone.ToStringThousandsWhole(), IconRefs.HealingAmount.DefaultColorHexWithHDRHex );

                                                DamageTextPopups.FloatingDamageTextPopup newDamageText = DamageTextPopups.CreateNewFromTextBuffer( buffer,
                                                    startLocation, endLocation, 0.8f, MathA.Max( 2, GameSettings.Current.GetInt( "MajorActionPopup_LingerTime" ) ) );
                                                newDamageText.PhysicalDamageIncluded = 0;
                                                newDamageText.MoraleDamageIncluded = 0;
                                                newDamageText.SquadDeathsIncluded = 0;
                                            }
                                        }
                                    }
                                    break;
                            }
                            return JobResult.Indeterminate;
                        }
                    #endregion
                    case "ContrabandJammer":
                        #region ContrabandJammer
                        {
                            if ( Logic == JobLogic.PerQuarterSecondAggregation )
                                return JobResult.Indeterminate;

                            switch ( Logic )
                            {
                                case JobLogic.ExecuteLogic:
                                    {
                                        JobMathInt reHidingPercentagePerBuildingPerTurn_Obj = Job.MathInts["ReHidingPercentagePerBuildingPerTurn"];

                                        int reHidingPercentagePerBuildingPerTurn = reHidingPercentagePerBuildingPerTurn_Obj.GetMin( Structure );
                                        float slurryRequiredPerHiddenRestored = Job.GetSingleFloatByID( "SlurryRequiredPerHiddenRestored", Structure );

                                        float reHideMultiplier = (reHidingPercentagePerBuildingPerTurn / 100f);

                                        List<MachineStructure> structuresInRange = Structure.StructuresWithinEffectiveRangeOfThisMachineStructure.GetDisplayList();

                                        int totalStructureHidingDone = 0;
                                        int totalStructuresHiddenHelped = 0;

                                        //first look at self
                                        {
                                            int amountReHidden = JobHelper.DoStructureHiddenFixIfNeeded( Structure, reHideMultiplier,
                                                slurryRequiredPerHiddenRestored, "Expense_UsedForContrabandJamming" );
                                            if ( amountReHidden > 0 )
                                            {
                                                totalStructureHidingDone += amountReHidden;
                                                totalStructuresHiddenHelped++;
                                            }
                                        }

                                        //then look for structures that are functional
                                        {
                                            foreach ( MachineStructure otherStructure in structuresInRange.GetRandomStartEnumerable( RandOrNull ) )
                                            {
                                                if ( !otherStructure.IsFunctionalStructure )
                                                    continue; //skip any that are not functional, we already checked those
                                                if ( otherStructure == Structure )
                                                    continue; //don't do self multiple times

                                                int amountReHidden = JobHelper.DoStructureHiddenFixIfNeeded( otherStructure, reHideMultiplier,
                                                    slurryRequiredPerHiddenRestored, "Expense_UsedForContrabandJamming" );
                                                if ( amountReHidden > 0 )
                                                {
                                                    totalStructureHidingDone += amountReHidden;
                                                    totalStructuresHiddenHelped++;
                                                }
                                            }
                                        }

                                        //then look for structures that are not functional
                                        {
                                            foreach ( MachineStructure otherStructure in structuresInRange.GetRandomStartEnumerable( RandOrNull ) )
                                            {
                                                if ( otherStructure.IsFunctionalStructure )
                                                    continue; //skip any that are functional for now
                                                if ( otherStructure == Structure )
                                                    continue; //don't do self multiple times

                                                int amountReHidden = JobHelper.DoStructureHiddenFixIfNeeded( otherStructure, reHideMultiplier,
                                                    slurryRequiredPerHiddenRestored, "Expense_UsedForContrabandJamming" );
                                                if ( amountReHidden > 0 )
                                                {
                                                    totalStructureHidingDone += amountReHidden;
                                                    totalStructuresHiddenHelped++;
                                                }
                                            }
                                        }

                                        if ( totalStructureHidingDone > 0 )
                                        {
                                            ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "WorkByContrabandJammer" ),
                                                NoteStyle.StoredGame, totalStructureHidingDone, totalStructuresHiddenHelped, jobBuilding.GetBuildingID(),
                                                SimCommon.Turn + 15 );

                                            {
                                                Vector3 startLocation = jobBuilding.GetMapItem().TopCenterPoint;
                                                Vector3 endLocation = startLocation.PlusY( 0.6f );

                                                ArcenDoubleCharacterBuffer buffer = JobHelper.GetBufferForJobExecutionThreadOnly();
                                                buffer.AddSpriteStyled_NoIndent( reHidingPercentagePerBuildingPerTurn_Obj.JobIcon, AdjustedSpriteStyle.InlineSmaller095,
                                                    IconRefs.StructureRevealedColor.DefaultColorHexWithHDRHex );
                                                buffer.Space1x().AddRaw( totalStructureHidingDone.ToStringThousandsWhole(), IconRefs.HealingAmount.DefaultColorHexWithHDRHex );

                                                DamageTextPopups.FloatingDamageTextPopup newDamageText = DamageTextPopups.CreateNewFromTextBuffer( buffer,
                                                    startLocation, endLocation, 0.8f, MathA.Max( 2, GameSettings.Current.GetInt( "MajorActionPopup_LingerTime" ) ) );
                                                newDamageText.PhysicalDamageIncluded = 0;
                                                newDamageText.MoraleDamageIncluded = 0;
                                                newDamageText.SquadDeathsIncluded = 0;
                                            }
                                        }
                                    }
                                    break;
                            }
                            return JobResult.Indeterminate;
                        }
                    #endregion
                    case "Apiary":
                        #region Apiary
                        {
                            if ( Logic == JobLogic.PerQuarterSecondAggregation )
                                return JobResult.Indeterminate;

                            switch ( Logic )
                            {
                                case JobLogic.ExecuteLogic:
                                    {
                                        float newBeesPerBeeHere = Job.GetSingleFloatByID( "NewBeesPerBeeHere", Structure );
                                        float waterPerNewBee = Job.GetSingleFloatByID( "FilteredWaterPerNewBee", Structure );
                                        int capOfNewBeesPerTurn = Job.GetSingleIntByID( "CapOfNewBeesPerTurn", Structure );
                                        
                                        int beesHere = (int)ResourceRefs.PollinatorBees.CalculateCurrentAmountStoredAtBuilding( Structure.ResourceStorageAmountHasBeenProviding1_Effective );
                                        int maxBeesPerTurn = Mathf.CeilToInt( newBeesPerBeeHere * beesHere );
                                        int roomForMore = (int)ResourceRefs.PollinatorBees.EffectiveHardCapStorageAvailable;
                                        if ( roomForMore > 0 )
                                        {
                                            maxBeesPerTurn = Mathf.Min( capOfNewBeesPerTurn, maxBeesPerTurn, roomForMore );

                                            int maxFromWater = Mathf.FloorToInt( (int)ResourceRefs.FilteredWater.Current / waterPerNewBee );

                                            int amountToCreate = MathA.Min( maxFromWater, maxBeesPerTurn );

                                            JobHelper.HandleElectricityAndStorage_Int( Structure, Job, ref amountToCreate, ResourceRefs.PollinatorBees );
                                            JobHelper.HandleResourceConsumed( Structure, Job, ResourceRefs.FilteredWater, waterPerNewBee, amountToCreate, maxBeesPerTurn );
                                            JobHelper.HandleResourceProduced( Structure, Job, ResourceRefs.PollinatorBees, amountToCreate, maxBeesPerTurn, ResourceAddRule.BlockExcess );
                                        }

                                        beesHere = (int)ResourceRefs.PollinatorBees.CalculateCurrentAmountStoredAtBuilding( Structure.ResourceStorageAmountHasBeenProviding1_Effective );
                                        Structure.CurrentStaff = beesHere;

                                        return JobResult.Success;
                                    }
                            }
                            return JobResult.Indeterminate;
                        }
                    #endregion
                    case "CatHouse":
                        #region CatHouse
                        {
                            if ( Logic == JobLogic.PerQuarterSecondAggregation )
                                return JobResult.Indeterminate;

                            switch ( Logic )
                            {
                                case JobLogic.ExecuteLogic:
                                    {
                                        return JobResult.Success;
                                    }
                            }
                            return JobResult.Indeterminate;
                        }
                    #endregion
                    case "AnimalPalace":
                        #region AnimalPalace
                        {
                            if ( Logic == JobLogic.PerQuarterSecondAggregation )
                                return JobResult.Indeterminate;

                            switch ( Logic )
                            {
                                case JobLogic.ExecuteLogic:
                                    {
                                        return JobResult.Success;
                                    }
                            }
                            return JobResult.Indeterminate;
                        }
                    #endregion
                    case "HousingAgency":
                        #region HousingAgency
                        {
                            if ( Logic == JobLogic.PerQuarterSecondAggregation )
                                return JobResult.Indeterminate;

                            switch ( Logic )
                            {
                                case JobLogic.ExecuteLogic:
                                    {
                                        ResourceType shelteredHumans = ResourceRefs.ShelteredHumans;
                                        if ( shelteredHumans.EffectiveHardCapStorageAvailable <= 0 )
                                            break;
                                        {
                                            int originalMaxTentsToHandle = 300;
                                            int maxTentsToHandle = originalMaxTentsToHandle;
                                            int humansToMoveIn = 0;
                                            int homelessTentsReplacedWithTrees = 0;
                                            //homeless people moving into our housing
                                            EconomicClassType homelessClass = EconomicClassTypeTable.Instance.GetRowByID( "Homeless" );
                                            BuildingTag homelessTentTag = CommonRefs.HomelessTent;

                                            List<ISimBuilding>tentList = homelessTentTag.DuringGame_Buildings.GetDisplayList();
                                            if ( tentList.Count > 0 )
                                            {
                                                for ( int i = tentList.Count - 1; i >= 0; i-- )
                                                {
                                                    ISimBuilding building = tentList[i];
                                                    if ( building == null )
                                                        continue;
                                                    MapItem buildingItem = building.GetMapItem();
                                                    if ( buildingItem.IsInPoolAtAll )
                                                        continue;

                                                    MapCell cell = buildingItem.ParentCell;
                                                    if ( cell == null ) 
                                                        continue;

                                                    int residentCount = building.GetResidentAmount( homelessClass );
                                                    if ( residentCount + humansToMoveIn > shelteredHumans.EffectiveHardCapStorageAvailable )
                                                        break; //stop for now, in that case

                                                    maxTentsToHandle--;
                                                    if ( maxTentsToHandle <= 0 )
                                                        break;

                                                    humansToMoveIn += residentCount;
                                                    building.KillEveryoneHere(); //from the normal perspective we are killing, but really they are moving out of the human society
                                                    Vector3 position = buildingItem.GroundCenterPoint;
                                                    Quaternion rotation = buildingItem.Rotation;

                                                    BiomeType biome = buildingItem.ParentTile?.District?.Biome;
                                                    buildingItem.DropBurningEffect();
                                                    building.FullyDeleteBuilding(); //note!  a foreach moving forward would miss items because of this!
                                                    homelessTentsReplacedWithTrees++;

                                                    {
                                                        MapGlowingIndicator indicator = new MapGlowingIndicator();
                                                        indicator.Position = buildingItem.Position;
                                                        indicator.Rotation = buildingItem.Rotation;
                                                        indicator.Scale = buildingItem.Scale;
                                                        indicator.ColorForHighlight = ColorRefs.BuildingRemovedAura.ColorHDR;
                                                        indicator.ItemRoot = buildingItem.Type;
                                                        indicator.RemainingTimeToLive = 4f;
                                                        indicator.ApplyScalesAndOffsets( 0f, 1f, 1f );
                                                        MapEffectCoordinator.AddMapGlowingIndicator( cell, indicator );
                                                    }

                                                    //add an outdoor spot that units can move to like they used to move to the tent
                                                    if ( cell != null )
                                                    {
                                                        MapOutdoorSpot outdoorSpot = MapOutdoorSpot.GetFromPoolOrCreate_NotFromSavegame( cell );
                                                        outdoorSpot.IsOnRoad = false;
                                                        outdoorSpot.Position = position.ReplaceY( MapOutdoorSpot.BASE_PLACEMENT_HEIGHT_OFFROAD );
                                                        cell.AllOutdoorSpots.Add( outdoorSpot );
                                                    }

                                                    //plant a tree
                                                    if ( biome != null )
                                                    {
                                                        A5ObjectRoot treeToPlant = biome.TreeDrawBag.PickRandom( RandOrNull );
                                                        if ( treeToPlant != null && cell != null )
                                                        {
                                                            MapItem item = MapItem.GetFromPoolOrCreate_NotFromSavegame( cell );
                                                            item.Type = treeToPlant;
                                                            item.Position = position;
                                                            item.Rotation = rotation;
                                                            item.Scale = treeToPlant.OriginalScale;
                                                            item.FillOBBCache();
                                                            cell.PlaceMapItemIntoCell( treeToPlant.ExtraPlaceableData.IsMinorDecoration ? TileDest.DecorationMinor : TileDest.DecorationMajor, item, false );
                                                        }
                                                        else if ( treeToPlant == null )
                                                            ArcenDebugging.LogSingleLine( "Null treeToPlant for planting a tree!", Verbosity.ShowAsError );
                                                        else if ( cell == null )
                                                            ArcenDebugging.LogSingleLine( "Null cell for planting a tree!", Verbosity.ShowAsError );
                                                    }
                                                    else
                                                        ArcenDebugging.LogSingleLine( "Null biome for planting a tree!", Verbosity.ShowAsError );
                                                }
                                            }

                                            //move the people into our structures
                                            if ( humansToMoveIn > 0 )
                                            {
                                                shelteredHumans.AlterCurrent_Named( humansToMoveIn, "Increase_HomelessMovingOutOfTents", ResourceAddRule.IgnoreUntilTurnChange );
                                                CityStatisticTable.AlterScore( "HomelessIndividualsHoused", humansToMoveIn );
                                            }
                                            if ( homelessTentsReplacedWithTrees > 0 )
                                            {
                                                CityStatisticTable.AlterScore( "HomelessTentsReplacedWithTrees", homelessTentsReplacedWithTrees );
                                            }

                                            int tentsConverted = originalMaxTentsToHandle - maxTentsToHandle;
                                            if ( tentsConverted > 0 )
                                            {
                                                ArcenDoubleCharacterBuffer popupBuffer = DamageTextPopups.GetTextBufferAppropriateForThreadOrNull();
                                                if ( popupBuffer != null )
                                                {
                                                    popupBuffer.AddFormat1( "PopupConvertedXTents", tentsConverted, IconRefs.NonlethalColor.DefaultColorHexWithHDRHex );

                                                    Vector3 startLocation = jobBuilding.GetMapItem().TopCenterPoint;
                                                    Vector3 endLocation = startLocation.PlusY( 0.6f );

                                                    DamageTextPopups.FloatingDamageTextPopup newDamageText = DamageTextPopups.CreateNewFromTextBuffer( popupBuffer,
                                                        startLocation, endLocation, 0.7f, MathA.Max( 2, GameSettings.Current.GetInt( "DamagePopup_LingerTime" ) ) );
                                                    newDamageText.MoraleDamageIncluded = 0;
                                                    newDamageText.PhysicalDamageIncluded = 0;
                                                    newDamageText.SquadDeathsIncluded = 0;
                                                }
                                            }
                                        }
                                    }
                                    return JobResult.Success;
                            }
                            return JobResult.Indeterminate;
                        }
                        #endregion
                    case "ProteinCannery":
                        #region ProteinCannery
                        {
                            if ( Logic == JobLogic.PerQuarterSecondAggregation )
                                return JobResult.Indeterminate;

                            switch ( Logic )
                            {
                                case JobLogic.ExecuteLogic:
                                    {
                                        int cannedProteinPerTurn = Job.GetSingleIntByID( "CannedProtein", Structure );
                                        int maxCannedPerTurn = cannedProteinPerTurn;
                                        float killedPerCannery = Job.GetSingleFloatByID( "KillsPerCanned", Structure );

                                        int roomForMore = (int)ResourceRefs.CannedProtein.EffectiveHardCapStorageAvailable;
                                        if ( ResourceRefs.CannedProtein.MidSoftCap < 1000000 )
                                            roomForMore = 1000000; //if the player has a very low cap, then complain at them.

                                        if ( roomForMore > 0 )
                                        {
                                            maxCannedPerTurn = MathA.Min( roomForMore, maxCannedPerTurn );
                                            cannedProteinPerTurn = MathA.Min( cannedProteinPerTurn, roomForMore );

                                            JobHelper.HandleElectricityAndStorage_Int( Structure, Job, ref cannedProteinPerTurn, ResourceRefs.CannedProtein );
                                            JobHelper.HandleResourceProduced( Structure, Job, ResourceRefs.CannedProtein, cannedProteinPerTurn, maxCannedPerTurn, ResourceAddRule.IgnoreUntilTurnChange );

                                            int kills = Mathf.RoundToInt( cannedProteinPerTurn * killedPerCannery );
                                            if ( kills > 0 )
                                            {
                                                World.People.KillResidentsCityWide_BiasedLower( kills, RandOrNull );

                                                CityStatisticTable.AlterScore( "HumansMulched", kills );
                                            }
                                        }

                                        return JobResult.Success;
                                    }
                            }
                            return JobResult.Indeterminate;
                        }
                    #endregion
                    case "BiologicalMainframe":
                        #region BiologicalMainframe
                        switch ( Logic )
                        {
                            case JobLogic.ExecuteLogic:
                                {
                                    if ( Structure.TurnsHasExisted >= 4 )
                                    {
                                        Structure.AddOrRemoveBadge( "RottedMinds", true );
                                        OtherKeyMessageTable.Instance.GetRowByID( "BrainsDegrade" ).DuringGameplay_IsReadyToBeViewed = true;
                                    }
                                }
                                break;
                        }
                        return JobHelper.HandleRoutineJobInputOutput( Structure, Job, Logic ); //this is the same from JobsRoutineFlow
                        #endregion
                    case "Codebreaker":
                        #region Codebreaker
                        {
                            if ( Logic == JobLogic.PerQuarterSecondAggregation )
                                return JobResult.Indeterminate;

                            switch ( Logic )
                            {
                                case JobLogic.ExecuteLogic:
                                    {
                                        int computeTime = (int)ResourceRefs.ComputeTime.Current;

                                        int originalDecryptedPerTurn = Job.GetSingleIntByID( "DecryptedPerTurn", Structure );
                                        int computeTimeTotalCost = Job.GetSingleIntByID( "ComputeTime", Structure );
                                        float computeTimePerDecrypted = (float)computeTimeTotalCost / (float)originalDecryptedPerTurn;

                                        int effDecryptedPerTurn = originalDecryptedPerTurn;
                                        if ( computeTime < computeTimeTotalCost )
                                            effDecryptedPerTurn = Mathf.FloorToInt( effDecryptedPerTurn * ((float)computeTime / (float)computeTimeTotalCost ) );

                                        int resourcesToDecrypt = 0;
                                        foreach ( ResourceType resource in ResourceTypeTable.DecryptableResources )
                                        {
                                            if ( resource.Current > 0 )
                                                resourcesToDecrypt++;
                                        }

                                        int totalDecrypted = 0;

                                        if ( resourcesToDecrypt > 0 )
                                        {
                                            int remainingToDecrypt = resourcesToDecrypt;
                                            int decryptedPer = effDecryptedPerTurn / resourcesToDecrypt;

                                            //first try to do things evenly
                                            foreach ( ResourceType resource in ResourceTypeTable.DecryptableResources )
                                            {
                                                if ( resource.Current > 0 )
                                                {
                                                    int toDecrypt = MathA.Min( (int)resource.Current, decryptedPer );
                                                    if ( toDecrypt > 0 )
                                                    {
                                                        totalDecrypted += toDecrypt;
                                                        resource.AlterCurrent_Job( -toDecrypt, Job, ResourceAddRule.IgnoreUntilTurnChange );
                                                        if ( resource.DecryptsIntoResource != null )
                                                            resource.DecryptsIntoResource.AlterCurrent_Job( toDecrypt, Job, ResourceAddRule.IgnoreUntilTurnChange );
                                                        if ( resource.DecryptsIntoStatistic != null )
                                                            resource.DecryptsIntoStatistic.AlterScore_CityOnly( toDecrypt );

                                                        remainingToDecrypt -= toDecrypt;
                                                    }
                                                }
                                            }

                                            if ( remainingToDecrypt > 0 )
                                            {
                                                //secondly, if there was a remainder, just spend it as we can
                                                foreach ( ResourceType resource in ResourceTypeTable.DecryptableResources )
                                                {
                                                    if ( resource.Current > 0 )
                                                    {
                                                        int toDecrypt = MathA.Min( (int)resource.Current, remainingToDecrypt );
                                                        if ( toDecrypt > 0 )
                                                        {
                                                            totalDecrypted += toDecrypt;
                                                            resource.AlterCurrent_Job( -toDecrypt, Job, ResourceAddRule.IgnoreUntilTurnChange );
                                                            if ( resource.DecryptsIntoResource != null )
                                                                resource.DecryptsIntoResource.AlterCurrent_Job( toDecrypt, Job, ResourceAddRule.IgnoreUntilTurnChange );
                                                            if ( resource.DecryptsIntoStatistic != null )
                                                                resource.DecryptsIntoStatistic.AlterScore_CityOnly( toDecrypt );

                                                            remainingToDecrypt -= toDecrypt;
                                                            if ( remainingToDecrypt <= 0 )
                                                                break;
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        if ( totalDecrypted > 0 )
                                        {
                                            int computeCost = Mathf.CeilToInt( totalDecrypted * computeTimePerDecrypted );
                                            ResourceRefs.ComputeTime.AlterCurrent_Job( -computeCost, Job, ResourceAddRule.IgnoreUntilTurnChange );
                                        }

                                        return JobResult.Success;
                                    }
                            }
                            return JobResult.Indeterminate;
                        }
                    #endregion
                    case "MineralScrounger":
                        #region MineralScrounger
                        {
                            if ( Logic == JobLogic.PerQuarterSecondAggregation )
                                return JobResult.Indeterminate;

                            switch ( Logic )
                            {
                                case JobLogic.ExecuteLogic:
                                    {
                                        int yieldPerTurn = Job.GetSingleIntByID( "YieldPerTurn", Structure );
                                        int maxPerTurn = yieldPerTurn;
                                        JobHelper.HandleElectricityAndStorage_Int( Structure, Job, ref yieldPerTurn, null );

                                        if ( yieldPerTurn > 0 )
                                        {
                                            foreach ( KeyValuePair<int, string> kv in MineralScroungerResources.GetRandomStartEnumerable( RandOrNull ) )
                                            {
                                                ResourceType resource = null;
                                                switch ( kv.Value )
                                                {
                                                    case "Alumina":
                                                        resource = ResourceRefs.Alumina;
                                                        break;
                                                    case "Bastnasite":
                                                        resource = ResourceRefs.Bastnasite;
                                                        break;
                                                    case "Scandium":
                                                        resource = ResourceRefs.Scandium;
                                                        break;
                                                    case "Neodymium":
                                                        resource = ResourceRefs.Neodymium;
                                                        break;
                                                }
                                                if ( resource == null || !resource.DuringGame_IsUnlocked() )
                                                    continue;

                                                int availableSpace = 5000 - (int)(resource.Current + resource.ExcessOverage);
                                                if ( availableSpace <= 0 )
                                                    continue; //already have more than that

                                                int amountToGet = MathA.Min( availableSpace, maxPerTurn );
                                                if ( resource.MidSoftCap <= 0 )
                                                {
                                                    resource.AddToExcessOverage( amountToGet );
                                                    return JobResult.Success;
                                                }
                                                else
                                                {
                                                    resource.AlterCurrent_Job( amountToGet, Job, ResourceAddRule.IgnoreUntilTurnChange );
                                                    return JobResult.Success;
                                                }
                                            }

                                            //if we got here, then everything was full -- so microbuilder time!
                                            ResourceRefs.Microbuilders.AlterCurrent_Job( maxPerTurn, Job, ResourceAddRule.IgnoreUntilTurnChange );
                                        }

                                        return JobResult.Success;
                                    }
                            }
                            return JobResult.Indeterminate;
                        }
                    #endregion
                    case "DailyNecessitiesFactory":
                        #region DailyNecessitiesFactory
                        {
                            JobResult result = JobHelper.HandleRoutineJobInputOutput( Structure, Job, Logic ); //this is the same from JobsRoutineFlow
                            if ( Logic == JobLogic.ExecuteLogic && result == JobResult.Success )
                            {
                                AchievementRefs.ApplePieFromScratch.TripIfNeeded();
                            }
                            return result;
                        }
                        #endregion
                    default:
                        if ( Logic == JobLogic.PerQuarterSecondAggregation )
                            return JobResult.Indeterminate;
                        ArcenDebugging.LogSingleLine( "JobsBasic: Called TryHandleJob for '" + Job.ID + "', which does not support it!", Verbosity.ShowAsError );
                        return JobResult.FailAndDestroyJob;
                }

            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "JobsBasic HandleJob error: " + Logic + " " + (Job?.ID ?? "[null]"), debugStage, e, Verbosity.ShowAsError );
                return JobResult.Indeterminate;
            }
        }

        private static readonly string[] MineralScroungerResources = new string[] { "Neodymium", "Scandium", "Alumina", "Bastnasite" };

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
                ArcenDebugging.LogDebugStageWithStack( "JobsBasic HandleJobDeletionLogic error: " + Logic + " " + (Job?.ID ?? "[null]"), debugStage, e, Verbosity.ShowAsError );
                return false;
            }
        }
    }
}
