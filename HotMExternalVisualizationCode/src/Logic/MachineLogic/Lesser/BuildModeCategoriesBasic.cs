using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.HotM.Visualization;

namespace Arcen.HotM.ExternalVis
{
    public class BuildModeCategoriesBasic : IMachineBuildModeCategoryImplementation
    {
        public bool GetShouldBeHiddenForOtherReasons( MachineBuildModeCategory Category )
        {
            return false;
        }

        public void HandleBuildCategoryPerQuarterSecond( MachineBuildModeCategory Category )
        {
            switch ( Category.ID )
            {
                case "Main_Recommended":
                    #region Main_Recommended
                    {
                        if ( SimCommon.TheNetwork?.Tower == null && CommonRefs.ImposingTowerType.DuringGame_IsUnlocked() )
                            Category.AddDuringGame_VisibleMainStructure_Both( CommonRefs.ImposingTowerType );
                        else if ( SimCommon.TheNetwork?.Tower == null && CommonRefs.NetworkTowerStructure.DuringGame_IsUnlocked() )
                            Category.AddDuringGame_VisibleMainStructure_Both( CommonRefs.NetworkTowerStructure );
                        else if ( SimCommon.TheNetwork?.Tower == null && CommonRefs.EmergencyNetworkSourceStructure.DuringGame_IsUnlocked() )
                            Category.AddDuringGame_VisibleMainStructure_Both( CommonRefs.EmergencyNetworkSourceStructure );
                        else
                        {
                            foreach ( SortedMachineJob job in JobCollectionRefs.All.JobList )
                            {
                                if ( job.Type.DuringGame_NumberSuggestedToBuild.Display > 0 )
                                    Category.DuringGame_VisibleMainJobs.AddToConstructionList( job.Type );
                            }

                            if ( SimCommon.VisibleTerritoryControlInvestigations.Count > 0 )
                            {
                                foreach ( Investigation investigation in SimCommon.VisibleTerritoryControlInvestigations )
                                {
                                    if ( investigation?.Type != null )
                                        Category.DuringGame_VisibleInvestigations.AddToConstructionList( investigation );
                                }
                            }
                        }
                    }
                    #endregion
                    break;
                case "Main_Hand":
                    #region Main_Hand
                    {
                        if ( SimCommon.TheNetwork?.Tower == null && CommonRefs.ImposingTowerType.DuringGame_IsUnlocked() )
                            Category.AddDuringGame_VisibleMainStructure_Both( CommonRefs.ImposingTowerType );
                        else if ( SimCommon.TheNetwork?.Tower == null && CommonRefs.NetworkTowerStructure.DuringGame_IsUnlocked() )
                            Category.AddDuringGame_VisibleMainStructure_Both( CommonRefs.NetworkTowerStructure );
                        else if ( SimCommon.TheNetwork?.Tower == null && CommonRefs.EmergencyNetworkSourceStructure.DuringGame_IsUnlocked() )
                            Category.AddDuringGame_VisibleMainStructure_Both( CommonRefs.EmergencyNetworkSourceStructure );
                        else
                        {
                            AddAllFrom_Main( Category, JobCollectionRefs.Hand );
                        }
                    }
                    #endregion
                    break;
                case "Main_Self":
                    #region Main_Self
                    {
                        AddAllFrom_Main( Category, JobCollectionRefs.Self );
                    }
                    #endregion
                    break;
                case "Main_Chain":
                    #region Main_Chain
                    {
                        AddAllFrom_Main( Category, JobCollectionRefs.Chain );

                        if ( SimCommon.VisibleTerritoryControlInvestigations.Count > 0 )
                        {
                            foreach ( Investigation investigation in SimCommon.VisibleTerritoryControlInvestigations )
                            {
                                if ( investigation?.Type != null )
                                    Category.DuringGame_VisibleInvestigations.AddToConstructionList( investigation );
                            }
                        }
                    }
                    #endregion
                    break;
                case "Main_Home":
                    #region Main_Home
                    {
                        AddAllFrom_Main( Category, JobCollectionRefs.Home );
                    }
                    #endregion
                    break;
                case "Main_Work":
                    #region Main_Work
                    {
                        AddAllFrom_Main( Category, JobCollectionRefs.Work );
                    }
                    #endregion
                    break;
                case "Main_New":
                    #region Main_New
                    {
                        AddAllFrom_Main_NewOnly( Category, JobCollectionRefs.All );
                    }
                    #endregion
                    break;
                case "Main_Unbuilt":
                    #region Main_Unbuilt
                    {
                        AddAllFrom_Main_UnbuiltOnly( Category, JobCollectionRefs.All );

                        if ( SimCommon.VisibleTerritoryControlInvestigations.Count > 0 )
                        {
                            foreach ( Investigation investigation in SimCommon.VisibleTerritoryControlInvestigations )
                            {
                                if ( investigation?.Type != null )
                                    Category.DuringGame_VisibleInvestigations.AddToConstructionList( investigation );
                            }
                        }
                    }
                    #endregion
                    break;
                case "Main_All":
                    #region Main_All
                    {
                        AddAllFrom_Main( Category, JobCollectionRefs.All );

                        if ( SimCommon.VisibleTerritoryControlInvestigations.Count > 0 )
                        {
                            foreach ( Investigation investigation in SimCommon.VisibleTerritoryControlInvestigations )
                            {
                                if ( investigation?.Type != null )
                                    Category.DuringGame_VisibleInvestigations.AddToConstructionList( investigation );
                            }
                        }
                    }
                    #endregion
                    break;
                //case "Zodiac_Recommended":
                //    #region Zodiac_Recommended
                //    {
                //        {
                //            foreach ( SortedMachineJob job in JobCollectionRefs.All.JobList )
                //            {
                //                if ( job.Type.RequiredStructureType.IsBuiltInZodiac && job.Type.DuringGame_NumberSuggestedToBuild.Display > 0 )
                //                    Category.DuringGame_VisibleZodiacJobs.AddToConstructionList( job.Type );
                //            }
                //        }
                //    }
                //    #endregion
                //    break;
                //case "Zodiac_Common":
                //    #region Zodiac_Common
                //    {
                //        if ( CommonRefs.DeleteStructureType.DuringGame_IsUnlocked() )
                //            Category.DuringGame_VisibleZodiacStructures.AddToConstructionList( CommonRefs.DeleteStructureType );
                //        if ( CommonRefs.PauseStructureType.DuringGame_IsUnlocked() )
                //            Category.DuringGame_VisibleZodiacStructures.AddToConstructionList( CommonRefs.PauseStructureType );

                //        AddAllFrom_Zodiac( Category, JobCollectionRefs.Common );
                //    }
                //    #endregion
                //    break;
                case "Zodiac_All":
                    #region Zodiac_All
                    {
                        AddAllFrom_Zodiac( Category, JobCollectionRefs.All );
                    }
                    #endregion
                    break;
            }

            if ( SimCommon.TheNetwork != null && 
                ( Category.DuringGame_VisibleMainStructuresCounted.GetDisplayListCount() > 0 ||
                Category.DuringGame_VisibleInvestigations.GetDisplayListCount() > 0 ||
                Category.DuringGame_VisibleMainJobs.GetDisplayListCount() > 0 ) )
            {
                if ( CommonRefs.DeleteStructureType.DuringGame_IsUnlocked() )
                    Category.DuringGame_VisibleMainStructuresAll.AddToConstructionList( CommonRefs.DeleteStructureType ); //never to the counted ones
                if ( CommonRefs.PauseStructureType.DuringGame_IsUnlocked() )
                    Category.DuringGame_VisibleMainStructuresAll.AddToConstructionList( CommonRefs.PauseStructureType ); //never to the counted ones
            }
        }

        #region AddAllFrom_Main
        private void AddAllFrom_Main( MachineBuildModeCategory Category, MachineJobCollection Collection )
        {
            if ( SimCommon.TheNetwork == null )
                return; //no jobs if the network is not yet in there

            foreach ( SortedMachineJob job in Collection.JobList )
            {
                if ( job.Type.RequiredStructureType.IsBuiltInZodiac )
                    continue;

                if ( job.Type.DuringGame_IsUnlocked() )
                    Category.DuringGame_VisibleMainJobs.AddToConstructionList( job.Type );
            }
        }
        #endregion


        #region AddAllFrom_Main_NewOnly
        private void AddAllFrom_Main_NewOnly( MachineBuildModeCategory Category, MachineJobCollection Collection )
        {
            if ( SimCommon.TheNetwork == null )
                return; //no jobs if the network is not yet in there

            foreach ( SortedMachineJob job in Collection.JobList )
            {
                if ( job.Type.RequiredStructureType.IsBuiltInZodiac )
                    continue;

                if ( job.Type.DuringGame_IsUnlocked() && !job.Type.Meta_HasEverBeenBuilt )
                    Category.DuringGame_VisibleMainJobs.AddToConstructionList( job.Type );
            }
        }
        #endregion

        #region AddAllFrom_Main_UnbuiltOnly
        private void AddAllFrom_Main_UnbuiltOnly( MachineBuildModeCategory Category, MachineJobCollection Collection )
        {
            if ( SimCommon.TheNetwork == null )
                return; //no jobs if the network is not yet in there

            foreach ( SortedMachineJob job in Collection.JobList )
            {
                if ( job.Type.RequiredStructureType.IsBuiltInZodiac )
                    continue;

                if ( job.Type.DuringGame_IsUnlocked() && job.Type.DuringGame_NumberFunctional.Display == 0 && job.Type.DuringGame_NumberInstalling.Display == 0 && job.Type.DuringGame_NumberBroken.Display == 0 )
                    Category.DuringGame_VisibleMainJobs.AddToConstructionList( job.Type );
            }
        }
        #endregion

        #region AddAllFrom_Zodiac
        private void AddAllFrom_Zodiac( MachineBuildModeCategory Category, MachineJobCollection Collection )
        {
            if ( SimCommon.TheNetwork == null )
                return; //no jobs if the network is not yet in there

            foreach ( SortedMachineJob job in Collection.JobList )
            {
                if ( !job.Type.RequiredStructureType.IsBuiltInZodiac )
                    continue;

                if ( job.Type.DuringGame_IsUnlocked() )
                    Category.DuringGame_VisibleZodiacJobs.AddToConstructionList( job.Type );
            }
        }
        #endregion
    }
}
