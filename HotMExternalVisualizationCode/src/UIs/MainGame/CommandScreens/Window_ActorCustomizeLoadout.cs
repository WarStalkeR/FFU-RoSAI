using Arcen.HotM.Core;
using Arcen.Universal;
using System;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_ActorCustomizeLoadout : ToggleableWindowController
    {
        public static ISimMapMobileActor ActorBeingExaminedOrNull = null;
        public static MobileActorTypeDuringGameData DuringGameDataBeingExamined = null;

        private static readonly AbilityType[] AbilitySlots = new AbilityType[AbilitySlotInfo.MAX_SLOT_OVERALL + 1];
        private static readonly bool[] AbilitySlot_HasRegeneratedSinceLoad = new bool[AbilitySlotInfo.MAX_SLOT_OVERALL + 1];
        private bool HasRecalculatedAbilitiesSinceLoad = false;
        public int TimesOpened = 0;

        public static Window_ActorCustomizeLoadout Instance;
        public Window_ActorCustomizeLoadout()
        {
            Instance = this;
            this.PreventsNormalInputHandlers = false;
            this.ShowEvenWhenSomethingElseTryingToMakeAllOtherWindowsNotShow = true;
        }

        public static readonly Dictionary<ActorDataType, int> NetProposedStatChanges = Dictionary<ActorDataType, int>.Create_WillNeverBeGCed( 30, "Window_ActorCustomizeLoadout-NetProposedStatChanges" );
        public static readonly Dictionary<ActorDataType, int> NetPendingStatChanges = Dictionary<ActorDataType, int>.Create_WillNeverBeGCed( 30, "Window_ActorCustomizeLoadout-NetPendingStatChanges" );
        public static readonly List<EquipmentProposal> ProposedEquipmentList = List<EquipmentProposal>.Create_WillNeverBeGCed( 30, "Window_ActorCustomizeLoadout-ProposedReturningEquipmentList" );
        public static int ProposedChangeCount = 0;

        private static readonly Dictionary<ActorFeat, float> workingFeatValues = Dictionary<ActorFeat, float>.Create_WillNeverBeGCed( 20, "Window_ActorCustomizeLoadout-workingFeatValues" );
        private static readonly Dictionary<ActorDataType, int> workingActorDataTypeValues = Dictionary<ActorDataType, int>.Create_WillNeverBeGCed( 20, "Window_ActorCustomizeLoadout-workingActorDataTypeValues" );
        private static readonly Dictionary<ActorPerk, bool> workingBlockedPerks = Dictionary<ActorPerk, bool>.Create_WillNeverBeGCed( 20, "Window_ActorCustomizeLoadout-workingBlockedPerks" );

        public static readonly Dictionary<ActorFeat, float> EffectiveFeats = Dictionary<ActorFeat, float>.Create_WillNeverBeGCed( 20, "Window_ActorCustomizeLoadout-EffectiveFeats" );
        public static readonly Dictionary<ActorBadge, bool> EffectiveBadges = Dictionary<ActorBadge, bool>.Create_WillNeverBeGCed( 20, "Window_ActorCustomizeLoadout-EffectiveBadges" );
        public static readonly Dictionary<ActorPerk, bool> EffectivePerks = Dictionary<ActorPerk, bool>.Create_WillNeverBeGCed( 20, "Window_ActorCustomizeLoadout-EffectivePerks" );
        public static readonly Dictionary<ResourceType, int> EffectiveCostsPerAttack = Dictionary<ResourceType, int>.Create_WillNeverBeGCed( 20, "Window_ActorCustomizeLoadout-EffectiveCostsPerAttack" );

        public static readonly Dictionary<ActorDataType, int> PossibleBestBoostsFromEquipment = Dictionary<ActorDataType, int>.Create_WillNeverBeGCed( 20, "Window_ActorCustomizeLoadout-PossibleBestBoostsFromEquipment" );
        public static readonly Dictionary<ActorFeat, float> PossibleFeats = Dictionary<ActorFeat, float>.Create_WillNeverBeGCed( 20, "Window_ActorCustomizeLoadout-PossibleFeats" );

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( Engine_HotM.IsBigBannerBeingShown )
                return false;
            switch ( Engine_HotM.GameMode )
            {
                case MainGameMode.TheEndOfTime:
                    return false;
            }

            if ( ActorBeingExaminedOrNull != null )
            {
                //seems okay for now
            }
            else if ( DuringGameDataBeingExamined != null )
            {
                if ( !Window_PlayerHardware.Instance.GetShouldDrawThisFrame() )
                    return false;
            }

            return base.GetShouldDrawThisFrame_Subclass();
        }

        #region ClearChanges And OnClose
        public void ClearChanges()
        {
            ActorBeingExaminedOrNull = null;
            DuringGameDataBeingExamined = null;

            for ( int i = 1; i < AbilitySlots.Length; i++ )
            {
                AbilitySlots[i] = null;
                AbilitySlot_HasRegeneratedSinceLoad[i] = false;
            }
            HasRecalculatedAbilitiesSinceLoad = false;

            foreach ( EquipmentSlotType slotType in EquipmentSlotTypeTable.Instance.Rows ) 
            {
                for ( int i = 0; i < slotType.NonSim_WorkingTypes.Length; i++ )
                {
                    slotType.NonSim_WorkingTypes[i] = null;
                    slotType.NonSim_ExistingEntries[i] = null;
                    slotType.NonSim_HasRegeneratedSinceLoad[i] = false;
                }
                slotType.NonSim_SortedAvailableEquipment.Clear();
            }

            NetProposedStatChanges.Clear();
            NetPendingStatChanges.Clear();
            ProposedEquipmentList.Clear();
            ProposedChangeCount = 0;

            workingFeatValues.Clear();
            workingActorDataTypeValues.Clear();
            workingBlockedPerks.Clear();

            EffectiveFeats.Clear();
            EffectiveBadges.Clear();
            EffectivePerks.Clear();
            EffectiveCostsPerAttack.Clear();

            PossibleBestBoostsFromEquipment.Clear();
            PossibleFeats.Clear();
        }

        public override void OnOpen()
        {
            if ( Window_ActorStanceChange.Instance.IsOpen )
                Window_ActorStanceChange.Instance.Close( WindowCloseReason.OtherWindowCausingClose );

            TimesOpened++;
            base.OnOpen();
        }

        public override void OnClose( WindowCloseReason CloseReason )
        {
            //if ( HasRecalculatedAbilitiesSinceLoad )
            //    CommitChangesIfPossible();

            EscapeWindowStackController.SilentlyRemoveFromStackIfOn( this );

            ClearChanges();
            base.OnClose( CloseReason );
        }
        #endregion

        #region LoadFromActor
        private void LoadFromActor( ISimMapMobileActor Actor )
        {
            ActorBeingExaminedOrNull = Actor;
            DuringGameDataBeingExamined = Actor?.GetTypeDuringGameData();
            if ( DuringGameDataBeingExamined == null )
                return;

            LoadFromDuringGameData( DuringGameDataBeingExamined );
        }

        private void LoadFromDuringGameData( MobileActorTypeDuringGameData DuringGameData )
        {
            DuringGameDataBeingExamined = DuringGameData;
            if ( DuringGameDataBeingExamined == null )
                return;

            if ( DuringGameData.IsMachineActor )
            {
                for ( int i = 1; i < AbilitySlots.Length; i++ )
                    AbilitySlots[i] = DuringGameData.GetActorAbilityInSlotIndex( i );
            }
            else
            {
                //for npcs just ignore it
            }

            //int index = 0;
            //foreach ( EquipmentEntry entry in Actor.GetEquipment() )
            //{
            //    ArcenDebugging.LogSingleLine( (index++) + " entry: " + entry.Type, Verbosity.DoNotShow );
            //}

            MobileActorTypeDuringGameData actorDGD = DuringGameDataBeingExamined;
            Dictionary<EquipmentSlotType, RenamedStat> actorSlotCounts = actorDGD.GetEquipmentSlotCounts();
            foreach ( EquipmentSlotType slotType in EquipmentSlotTypeTable.Instance.Rows )
            {
                int slotsHeldByThisActor = actorSlotCounts[slotType]?.Amount ?? 0;
                if ( slotsHeldByThisActor <= 0 )
                    continue; //if this slot type is not relevant to this actor, then skip it!

                int slotIndex = 0;
                foreach ( EquipmentEntry entry in actorDGD.Equipment )
                {
                    if ( entry.SlotType != slotType )
                        continue;
                    if ( slotIndex >= slotType.NonSim_WorkingTypes.Length )
                        break;
                    slotType.NonSim_WorkingTypes[slotIndex] = entry.CurrentType;
                    slotType.NonSim_ExistingEntries[slotIndex] = entry;
                    slotIndex++;
                }

                #region Recalculate NonSim_SortedAvailableEquipment
                slotType.NonSim_SortedAvailableEquipment.Clear();
                foreach ( UnitEquipmentType equipment in slotType.Equipment )
                {
                    if ( !equipment.DuringGame_IsUnlocked() )
                        continue; //if not unlocked, skip it!
                    slotType.NonSim_SortedAvailableEquipment.Add( equipment );
                    equipment.NonSim_IsValidForActor = actorDGD.ValidateEquipmentAgainstActor( equipment );
                }

                if ( slotType.NonSim_SortedAvailableEquipment.Count > 1 )
                {
                    slotType.NonSim_SortedAvailableEquipment.Sort( delegate ( UnitEquipmentType Left, UnitEquipmentType Right )
                    {
                        int val = Right.NonSim_IsValidForActor.CompareTo( Left.NonSim_IsValidForActor ); //desc
                        if ( val != 0 )
                            return val;
                        val = Left.SortOrder.CompareTo( Right.SortOrder );
                        if ( val != 0 )
                            return val;
                        return Left.GetDisplayName().CompareTo( Right.GetDisplayName() );
                    } );
                }
                #endregion
            }
        }
        #endregion

        #region ToggleOpen
        public void ToggleOpen()
        {
            if ( !( Engine_HotM.SelectedActor is ISimMapMobileActor Mobile ) )
                return; //do nothing!

            if ( (Engine_HotM.SelectedActor is ISimNPCUnit npc && !npc.GetIsPlayerControlled()) )
                return;

            if ( ActorBeingExaminedOrNull != Mobile || !this.IsOpen )
            {
                ClearChanges();
                LoadFromActor( Mobile );
                this.Open();
            }
            else
                this.Close( WindowCloseReason.UserDirectRequest );
        }
        #endregion

        #region OpenFromDuringGameData
        public void OpenFromDuringGameData( MobileActorTypeDuringGameData DuringGameData )
        {
            ClearChanges();
            LoadFromDuringGameData( DuringGameData );
            this.Open();
        }
        #endregion

        #region GetMinXForTooltips
        public static float GetMinXForTooltips()
        {
            return SizingRect.GetWorldSpaceTopRightCorner().x;
        }
        #endregion

        private static RectTransform SizingRect = null;

        #region customParent
        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {
            public customParent()
            {
                Window_ActorCustomizeLoadout.CustomParentInstance = this;
            }

            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            private int heightToShow = 0;
            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                this.WindowController.myScale = 1.1f * GameSettings.Current.GetFloat( "Scale_StatsSidebar" );

                this.WindowController.ExtraOffsetX = MathA.Max( Window_ActorTypeSidebarStats.Instance.GetCurrentWidth_Scaled(), Window_ActorSidebarStatsEquipment.Instance.GetCurrentWidth_Scaled() );

                if ( Window_ActorCustomizeLoadout.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        this.Element.Window.MinDeltaTimeBeforeUpdates = 0f; //make it nice and responsive
                        this.Element.Window.MaxDeltaTimeBeforeUpdates = 0f;
                        hasGlobalInitialized = true;
                    }
                    #endregion
                }

                #region Expand or Shrink Size Of This Window
                int newHeight = 75 + MathA.Min( lastYHeightOfInterior, 600 );
                if ( heightToShow != newHeight )
                {
                    heightToShow = newHeight;
                    SizingRect = this.Element.RelevantRect;
                    //appear from the center up and down
                    SizingRect.anchorMin = new Vector2( 0, 0.5f );
                    SizingRect.anchorMax = new Vector2( 0, 0.5f );
                    SizingRect.pivot = new Vector2( 0, 0.5f );
                    SizingRect.UI_SetHeight( heightToShow );
                }
                #endregion

                if ( ActorBeingExaminedOrNull == null && DuringGameDataBeingExamined != null )
                { } //do not auto-close, because we're editing a type rather than an actor
                else if ( ActorBeingExaminedOrNull != Engine_HotM.SelectedActor ) //we're editing a specific actor, so auto-close if it changed
                {
                    CommitChangesIfPossible();
                    if ( !( Engine_HotM.SelectedActor is ISimMapMobileActor Mobile ) )
                        Instance.Close( WindowCloseReason.ShowingRefused ); //close if no actor selected
                    else if ( (Engine_HotM.SelectedActor is ISimNPCUnit npc && !npc.GetIsPlayerControlled()) )
                        Instance.Close( WindowCloseReason.ShowingRefused ); //close if wrong kind selected
                    else
                    {
                        Instance.ClearChanges();
                        Instance.LoadFromActor( Mobile ); //gracefully switch to the other actor if a different actor is selected
                    }
                }
            }
        }
        #endregion

        private readonly ArcenCachedExternalTypeDirect type_tLabelGeneric = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tLabelGeneric ) );
        private readonly ArcenCachedExternalTypeDirect type_tLabelGeneric2 = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tLabelGeneric2 ) );
        private readonly ArcenCachedExternalTypeDirect type_dDropdown = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( dDropdown ) );
        private readonly ArcenCachedExternalTypeDirect type_bButton = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bButton ) );

        private const float NORMAL_ROW_HEIGHT = 18;
        private const float NORMAL_ROW_BUFFER = 2f;
        private const float SECTION_EXTRA_ROW_BUFFER = 9f;
        private const float ABILITY_SECTION_EXTRA_ROW_BUFFER = 21f;
        
        private const float LEFT_COLUMN_WIDTH = 100f;
        private const float COLUMN_SPACER = 2f;
        private const float RIGHT_COLUMN_WIDTH_EQUIPMENT = 440f;
        private const float RIGHT_COLUMN_WIDTH_ABILITIES = 340f;
        private const float COLUMN_EXTRA_SPACER_ABILITIES = 50f;

        private const float BUTTON_ROW_HEIGHT = 22;

        private const float SHORT_BUTTON_ROW_HEIGHT = 14;
        private const float SHORT_BUTTON_WIDTH = 100;
        private const float SHORT_BUTTON_SPACING_X = 10;

        private const float FULL_WIDTH_IGNORESCROLLBAR = 550f;
        private const float FULL_WIDTH_AVOIDSCROLLBAR = 530f;

        private const float HALF_WIDTH_IGNORESCROLLBAR = FULL_WIDTH_IGNORESCROLLBAR / 2f;
        private const float HALF_WIDTH_IGNORESCROLLBAR_MINUS_SPACER = HALF_WIDTH_IGNORESCROLLBAR - (COLUMN_SPACER/2f);

        private static int lastYHeightOfInterior = 0;
        private static int lastNumberOfCompleteClears = 0;

        private static readonly List<KeyValuePair<AbilityType,bool>> SortedVisibleAbilities = List<KeyValuePair<AbilityType, bool>>.Create_WillNeverBeGCed( 50, "Window_ActorCustomizeLoadout-SortedVisibleAbilities" );

        #region RecalculateSortedVisibleAbilitiesIfNeeded
        private void RecalculateSortedVisibleAbilitiesIfNeeded( MobileActorTypeDuringGameData actorDGD )
        {
            if ( HasRecalculatedAbilitiesSinceLoad )
                return;
            HasRecalculatedAbilitiesSinceLoad = true;

            SortedVisibleAbilities.Clear();

            if ( actorDGD.IsMachineActor )
            {
                AbilityGroup abilityGroup = actorDGD.GetAbilityGroup();
                foreach ( AbilityType abilityType in abilityGroup.AbilityTypes )
                {
                    if ( abilityType.BlockedFromBeingAssigned )
                        continue; //can't do that one ever!
                    if ( !abilityType.DuringGame_IsUnlocked() )
                        continue; //can't do this one yet because it's not yet unlocked!
                    if ( abilityType.DuringGame_IsHiddenByFlags() )
                        continue; //can't do this one yet because it's hidden by flags
                    if ( abilityType.RequiredActorCollectionForAbility != null )
                    {
                        if ( actorDGD.ParentUnitTypeOrNull != null )
                        {
                            if ( !actorDGD.ParentUnitTypeOrNull.Collections.ContainsKey( abilityType.RequiredActorCollectionForAbility.ID ) )
                                continue; //skip because this unit is not in the required collection
                        }
                        else if ( actorDGD.ParentVehicleTypeOrNull != null )
                        {
                            if ( !actorDGD.ParentVehicleTypeOrNull.Collections.ContainsKey( abilityType.RequiredActorCollectionForAbility.ID ) )
                                continue; //skip because this vehicle is not in the required collection
                        }
                        else
                            continue; //skip because not a unit or vehicle, so definitely can't use this
                    }

                    bool canEquip = true;
                    if ( abilityType.RequiredActorDataTypeForAbility != null )
                    {
                        if ( abilityType.RequiredActorDataMustBeAtLeast > actorDGD.NonSimActorDataMinMax.GetDisplayDict()[abilityType.RequiredActorDataTypeForAbility].LeftItem ) //compared to the min
                            canEquip = false;
                    }

                    SortedVisibleAbilities.Add( new KeyValuePair<AbilityType, bool>( abilityType, canEquip ) );
                }
            }

            if ( SortedVisibleAbilities.Count > 1  )
            {
                SortedVisibleAbilities.Sort( delegate ( KeyValuePair<AbilityType, bool> Left, KeyValuePair<AbilityType, bool> Right )
                {
                    int val = Right.Value.CompareTo( Left.Value ); //desc, show things you can do first
                    if ( val != 0 )
                        return val;
                    val = Left.Key.SortOrder.CompareTo( Right.Key.SortOrder );
                    if ( val != 0 )
                        return val;
                    return Left.Key.GetDisplayName().CompareTo( Right.Key.GetDisplayName() );
                } );
            }
        }
        #endregion

        #region RecalculateChangeCostsAndSuch
        public static void RecalculateChangeCostsAndSuch( MobileActorTypeDuringGameData actorDGD )
        {
            NetProposedStatChanges.Clear();
            NetPendingStatChanges.Clear();
            ProposedEquipmentList.Clear();
            ProposedChangeCount = 0;

            int baseSquadSize = 1;
            bool isNPC = false;
            NPCUnitType npcUnitType = null;
            if ( actorDGD.IsNPC )
            {
                baseSquadSize = actorDGD.ParentNPCUnitTypeOrNull.BasicSquadSize;
                isNPC = true;
                npcUnitType = actorDGD.ParentNPCUnitTypeOrNull;
            }

            foreach ( EquipmentSlotType slotType in EquipmentSlotTypeTable.Instance.Rows )
            {
                for ( int i = 0; i < slotType.NonSim_WorkingTypes.Length; i++ )
                {
                    EquipmentEntry existingEntry = slotType.NonSim_ExistingEntries[i];
                    if ( existingEntry != null && existingEntry.GetIsAlreadyMatchForProposal( slotType.NonSim_WorkingTypes[i] ) )
                    {
                        EquipmentProposal proposal = existingEntry.GenerateProposalCopyOfSelf();
                        ProposedEquipmentList.Add( proposal );

                        if ( proposal.TurnsOfCooldownBeforeCanSetAgain > 0 )
                        {
                            if ( proposal.PriorType != null )
                            {
                                //these changes have not yet applied, so mark them as pending removal
                                foreach ( KeyValuePair<ActorDataType, int> kv in proposal.PriorType.EquipmentDuringGameData )
                                {
                                    if ( isNPC && !npcUnitType.ActorDataSetUsed.IncludedDict.ContainsKey( kv.Key.ID ) )
                                        continue;
                                    if ( kv.Key.IsEquipmentMultipliedByCurrentSquadMemberCount || kv.Key.IsEquipmentMultipliedByLargestSquadMemberCount )
                                        NetPendingStatChanges[kv.Key] -= (kv.Value * baseSquadSize); //subtract these values
                                    else
                                        NetPendingStatChanges[kv.Key] -= kv.Value; //subtract these values
                                }
                            }

                            if ( proposal.NewType != null )
                            {
                                //these changes have not yet applied, so mark them as pending
                                foreach ( KeyValuePair<ActorDataType, int> kv in proposal.NewType.EquipmentDuringGameData )
                                {
                                    if ( isNPC && !npcUnitType.ActorDataSetUsed.IncludedDict.ContainsKey( kv.Key.ID ) )
                                        continue;
                                    if ( kv.Key.IsEquipmentMultipliedByCurrentSquadMemberCount || kv.Key.IsEquipmentMultipliedByLargestSquadMemberCount )
                                        NetPendingStatChanges[kv.Key] += (kv.Value * baseSquadSize); //add these ones
                                    else
                                        NetPendingStatChanges[kv.Key] += kv.Value; //add these ones
                                }
                            }
                        }

                        continue; //if no change, then ignore it
                    }
                    else if ( existingEntry == null && slotType.NonSim_WorkingTypes[i] == null )
                        continue; //another kind of no-change, so again ignore

                    //if we got here, then there was some mismatch, so the old type is going away
                    ProposedChangeCount++;

                    #region oldType removal
                    {
                        UnitEquipmentType oldType = existingEntry?.CurrentType;
                        //if ( existingEntry?.NewType != null ) //note: this was bad logic
                        //    oldType = existingEntry.NewType;

                        if ( oldType != null )
                        {
                            //swapping from this, so reduce stats accordingly
                            foreach ( KeyValuePair<ActorDataType, int> kv in oldType.EquipmentDuringGameData )
                            {
                                if ( isNPC && !npcUnitType.ActorDataSetUsed.IncludedDict.ContainsKey( kv.Key.ID ) )
                                    continue;
                                if ( kv.Key.IsEquipmentMultipliedByCurrentSquadMemberCount || kv.Key.IsEquipmentMultipliedByLargestSquadMemberCount )
                                    NetProposedStatChanges[kv.Key] -= (kv.Value * baseSquadSize); //subtraction, this time
                                else
                                    NetProposedStatChanges[kv.Key] -= kv.Value; //subtraction, this time
                            }
                        }
                    }
                    #endregion oldType removal

                    #region newType adding
                    {
                        UnitEquipmentType newType = slotType.NonSim_WorkingTypes[i];
                        if ( newType != null )
                        {
                            EquipmentProposal proposal = new EquipmentProposal();
                            proposal.PriorType = existingEntry?.CurrentType; //always count this one; don't worry about a prior NewType in this case
                            proposal.NewType = newType;
                            proposal.TurnsOfCooldownBeforeCanSetAgain = newType.TurnsRequiredToEquip;
                            ProposedEquipmentList.Add( proposal );

                            //swapping to this, so increase stats accordingly
                            foreach ( KeyValuePair<ActorDataType, int> kv in newType.EquipmentDuringGameData )
                            {
                                if ( isNPC && !npcUnitType.ActorDataSetUsed.IncludedDict.ContainsKey( kv.Key.ID ) )
                                    continue;
                                if ( kv.Key.IsEquipmentMultipliedByCurrentSquadMemberCount || kv.Key.IsEquipmentMultipliedByLargestSquadMemberCount )
                                    NetProposedStatChanges[kv.Key] += (kv.Value * baseSquadSize);
                                else
                                    NetProposedStatChanges[kv.Key] += kv.Value;
                            }
                        }
                    }
                    #endregion newType adding
                }
            }

            if ( actorDGD.IsMachineActor )
            {
                bool isVehicle = actorDGD.ParentVehicleTypeOrNull != null;
                int maxSlot = isVehicle ? AbilitySlotInfo.MAX_SLOT_VEHICLES : AbilitySlotInfo.MAX_SLOT_UNITS;

                for ( int slotIndex = 1; slotIndex <= maxSlot; slotIndex++ )
                {
                    if ( actorDGD.GetActorAbilityInSlotIndex( slotIndex ) != AbilitySlots[slotIndex] )
                        ProposedChangeCount++;
                }
            }

            workingFeatValues.Clear();
            workingActorDataTypeValues.Clear();
            workingBlockedPerks.Clear();

            EffectiveFeats.Clear();
            EffectiveBadges.Clear();
            EffectivePerks.Clear();
            EffectiveCostsPerAttack.Clear();

            PossibleBestBoostsFromEquipment.Clear();
            PossibleFeats.Clear();

            BaseMapActorHelper.CalculateCoreActorFeatsAndBoosts( actorDGD, EffectiveFeats, PossibleFeats,
                EffectiveBadges, EffectivePerks, PossibleBestBoostsFromEquipment, EffectiveCostsPerAttack,
                workingFeatValues, workingActorDataTypeValues, workingBlockedPerks, ListView<IEquipmentHolder>.Create( ProposedEquipmentList ) );
        }
        #endregion

        public override void PopulateFreeFormControls( ArcenUI_SetOfCreateElementDirectives Set )
        {
            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            int debugStage = 0;
            float runningY = 3;
            try
            {
                debugStage = 100;
                if ( VisCurrent.IsUIHiddenExceptForSidebar )
                {
                    debugStage = 110;
                    this.Close( WindowCloseReason.ShowingRefused );
                    return;
                }
                debugStage = 200;
                if ( bMainContentParent.ParentT == null )
                    return;
                debugStage = 210;
                this.Window.SetOverridingTransformToWhichToAddChildren( bMainContentParent.ParentT );

                debugStage = 300;
                if ( Engine_HotM.NumberOfCompleteClears != lastNumberOfCompleteClears )
                {
                    debugStage = 310;
                    Set.RefreshAllElements = true;
                    lastNumberOfCompleteClears = Engine_HotM.NumberOfCompleteClears;
                    return;
                }

                debugStage = 400;
                ISimMapMobileActor actorOrNull = ActorBeingExaminedOrNull;
                MobileActorTypeDuringGameData actorDGD = DuringGameDataBeingExamined;
                if ( actorDGD == null )
                    return;

                debugStage = 500;
                RecalculateSortedVisibleAbilitiesIfNeeded( actorDGD );

                debugStage = 600;
                RecalculateChangeCostsAndSuch( actorDGD );

                Rect bounds;

                debugStage = 2100;

                if ( actorOrNull != null && !(actorOrNull is ISimNPCUnit) )
                {
                    #region Rename
                    bRename.Instance.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                ExtraData.Buffer.StartColor( ColorTheme.GetBasicLightTextPurple( element.LastHadMouseWithin ) );
                                ExtraData.Buffer.AddLang( "RenameUnitHeader" );
                                break;
                            case UIAction.HandleMouseover:
                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "ActorLoadout", "RenameUnitHeader" ), element, SideClamp.LeftOrRight, 
                                    TooltipNovelWidth.Smaller, TooltipExtraText.None, TooltipExtraRules.MustBeToRightOfCustomizeLoadout ) )
                                {
                                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                                    novel.TitleUpperLeft.AddLang( "RenameUnitHeader" );
                                    novel.Main.AddLang( "CustomName" );
                                }
                                break;
                            case UIAction.OnClick:
                                if ( actorOrNull is ISimMachineUnit unit )
                                {
                                    Window_ModalTextboxWindow.Instance.Open( Lang.Get( "RenameUnitHeader" ), unit.GetDisplayName(), false, string.Empty, 20,
                                        delegate ( string NewUnitName )
                                        {
                                            unit.SetCustomUnitName( NewUnitName );
                                            return true;
                                        } );
                                }
                                else if ( actorOrNull is ISimMachineVehicle vehicle )
                                {
                                    Window_ModalTextboxWindow.Instance.Open( Lang.Get( "RenameUnitHeader" ), vehicle.GetDisplayName(), false, string.Empty, 20,
                                        delegate ( string NewVehicleName )
                                        {
                                            vehicle.SetCustomVehicleName( NewVehicleName );
                                            return true;
                                        } );
                                }
                                break;
                        }
                    } );
                    #endregion
                }
                else
                    bRename.Instance.Assign( null );

                debugStage = 3100;
                if ( actorOrNull != null )
                {
                    {
                        #region ScrapUnit
                        bScrap.Instance.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    ExtraData.Buffer.StartColor( ColorTheme.GetBasicLightTextPurple( element.LastHadMouseWithin ) );
                                    ExtraData.Buffer.AddLang( "ScrapUnit" );
                                    break;
                                case UIAction.HandleMouseover:
                                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( "ActorLoadout", "Scrap" ), element, SideClamp.LeftOrRight,
                                        TooltipNovelWidth.Smaller, TooltipExtraText.None, TooltipExtraRules.MustBeToRightOfCustomizeLoadout ) )
                                    {
                                        novel.ShadowStyle = TooltipShadowStyle.Standard;
                                        novel.TitleUpperLeft.AddLang( "ScrapUnit" ).AddTooltipHotkeySuffix( "ScrapSelected" );
                                        novel.Main.AddLang( "ScrapUnit_Tooltip1" ).Line();
                                        novel.Main.AddLang( "ScrapUnit_Tooltip2", ColorTheme.PurpleDim ).Line();
                                    }
                                    break;
                                case UIAction.OnClick:
                                    if ( SimCommon.Turn <= 1 )
                                    {
                                        ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                                        return;
                                    }
                                    if ( actorOrNull is ISimMachineActor machineAct && machineAct.GetIsBlockedFromBeingScrappedRightNow() )
                                    {
                                        ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                                        return;
                                    }
                                    {
                                        if ( actorOrNull is ISimMachineActor machineActor )
                                        {
                                            if ( !machineActor.PopupReasonCannotScrapIfCannot( ScrapReason.ReplacementByPlayer ) )
                                            {
                                                ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                                                break;
                                            }
                                        }
                                    }

                                    ModalPopupData.CreateAndLogYesNoStyle( PopupYesNoStyle.Small, delegate
                                    {
                                        if ( actorOrNull is ISimMachineActor machineActor )
                                            machineActor.TryScrapRightNowWithoutWarning_Danger( ScrapReason.ArbitraryPlayer );
                                        else if ( actorOrNull is ISimNPCUnit npcUnit && npcUnit.GetIsPlayerControlled() )
                                            npcUnit.DisbandNPCUnit( NPCDisbandReason.PlayerRequestForOwnUnit );
                                        Engine_HotM.SetSelectedActor( null, false, false, false );
                                    }, null, LocalizedString.AddFormat1_New( "ScrapUnit_Header", actorOrNull.GetDisplayName() ),
                                    LocalizedString.AddFormat1_New( "ScrapUnit_Body", actorOrNull.GetDisplayName() ),
                                    LangCommon.Popup_Common_Yes.LocalizedString, LangCommon.Popup_Common_NoWait.LocalizedString );
                                    break;
                            }
                        } );
                        #endregion
                    }
                }
                else
                    bScrap.Instance.Assign( null );

                debugStage = 4100;

                iPortrait.Instance.SetSpriteIfNeeded( actorDGD.GetTooltipIcon().GetSpriteForUI() );

                debugStage = 5100;

                {
                    if ( actorDGD.IsMachineActor )
                    {
                        bool isVehicle = actorDGD.ParentVehicleTypeOrNull != null;
                        int maxSlot = isVehicle ? AbilitySlotInfo.MAX_SLOT_VEHICLES : AbilitySlotInfo.MAX_SLOT_UNITS;

                        for ( int slotIndexOuter = 1; slotIndexOuter <= maxSlot; slotIndexOuter++ )
                        {
                            debugStage = 5200;

                            int slotIndex = slotIndexOuter;
                            AbilitySlotInfo slotInfo = actorDGD.GetBaseAbilitySlots()[slotIndex];
                            debugStage = 5300;
                            if ( slotInfo?.Ability?.IsSlotSkippedOnEquipmentScreenIfAssigned ?? false )
                                continue;
                            debugStage = 5400;
                            if ( !FlagRefs.HasUnlockedAbilityAdjustments.DuringGameplay_IsTripped )
                                break;

                            debugStage = 5500;

                            #region Ability - LeftColumn
                            bounds = ArcenFloatRectangle.CreateUnityRect( 5, runningY, LEFT_COLUMN_WIDTH, NORMAL_ROW_HEIGHT );
                            UIFlow.AddText( "HoverableHeader", Set, type_tLabelGeneric, "AbilityLabel", slotIndex, SimCommon.Turn + TimesOpened, actorDGD.NonSimUniqueID, "", bounds,
                                delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                                {
                                    if ( actorDGD == null )
                                        return;

                                    switch ( Action )
                                    {
                                        case UIAction.GetTextToShowFromVolatile:
                                            if ( AbilitySlots[slotIndex]?.DuringGame_IsHiddenByFlags() ?? false )
                                                ExtraData.Buffer.StartColor( ColorTheme.GetDisabledPurple( element.LastHadMouseWithin ) );
                                            else if ( AbilitySlots[slotIndex] != actorDGD.GetActorAbilityInSlotIndex( slotIndex ) )
                                                ExtraData.Buffer.StartColor( ColorTheme.Settings_CurrentDiffersFromDefaultYellow );
                                            else
                                                ExtraData.Buffer.StartColor( ColorTheme.GetBasicLightTextPurple( element.LastHadMouseWithin ) );

                                            ExtraData.Buffer.AddLang( "Ability_Prefix" ).Space1x().AddRaw( slotIndex.ToString() );
                                            break;
                                        case UIAction.HandleMouseover:
                                            {
                                                AbilityType selected = AbilitySlots[slotIndex];

                                                if ( selected?.DuringGame_IsHiddenByFlags() ?? false )
                                                {
                                                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( "ActorLoadoutSlot", slotIndex, 0 ), element, SideClamp.LeftOrRight,
                                                        TooltipNovelWidth.Smaller, TooltipExtraText.None, TooltipExtraRules.MustBeToRightOfCustomizeLoadout ) )
                                                    {
                                                        novel.ShadowStyle = TooltipShadowStyle.Standard;
                                                        novel.Icon = IconRefs.AbilityLocked.Icon;
                                                        novel.TitleUpperLeft.AddLang( "Locked" );
                                                        novel.Main.AddLang( "AbilityLocked_Details" ).Line();
                                                    }
                                                    break;
                                                }

                                                if ( selected != null )
                                                {
                                                    ExtraData.Buffer.EnsureResetForNextUpdate();
                                                    AbilityType currentEquipped = actorDGD.GetActorAbilityInSlotIndex( slotIndex );
                                                    if ( AbilitySlots[slotIndex] != currentEquipped )
                                                        ExtraData.Buffer.LineIfLastWrittenWasNotLine().StartColor( ColorTheme.Settings_CurrentDiffersFromDefaultYellow )
                                                        .AddLangAndAfterLineItemHeader( "SwitchingFromCurrentlyEquipped" ).AddRaw( currentEquipped == null ? LangCommon.None.Text : currentEquipped.GetDisplayName() );

                                                    selected.RenderAbilityTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, TooltipExtraRules.MustBeToRightOfCustomizeLoadout,
                                                        AbilityTooltipInstruction.ForEquipmentMenu, slotIndex, null, actorDGD, TooltipExtraText.None, ExtraData.Buffer, null );
                                                }
                                                
                                            }
                                            break;
                                    }
                                }, 9, TextWrapStyle.NoWrap_Ellipsis );
                            #endregion

                            debugStage = 6100;

                            bounds = ArcenFloatRectangle.CreateUnityRect( bounds.xMax + COLUMN_SPACER + COLUMN_EXTRA_SPACER_ABILITIES, runningY, RIGHT_COLUMN_WIDTH_ABILITIES, NORMAL_ROW_HEIGHT );
                            if ( slotInfo != null && (!slotInfo.CanBeReplaced || (AbilitySlots[slotIndex]?.DuringGame_IsHiddenByFlags() ?? false)) )
                            {
                                debugStage = 7100;
                                #region Ability - RightColumn As Label
                                UIFlow.AddText( "HoverableFalseDropdownPurple", Set, type_tLabelGeneric, "AbilityLabelRight", slotIndex, SimCommon.Turn + TimesOpened, actorDGD.NonSimUniqueID, "", bounds,
                                delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                                {
                                    if ( actorDGD == null )
                                        return;

                                    switch ( Action )
                                    {
                                        case UIAction.GetTextToShowFromVolatile:
                                            ExtraData.Buffer.StartSize80();
                                            if ( AbilitySlots[slotIndex]?.DuringGame_IsHiddenByFlags() ?? false )
                                            {
                                                ExtraData.Buffer.AddLang( "Locked", ColorTheme.GetDisabledPurple( element.LastHadMouseWithin ) );
                                                break;
                                            }
                                            ExtraData.Buffer.StartColor( ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                            slotInfo.Ability?.WriteOptionDisplayTextForDropdown( ExtraData.Buffer );
                                            break;
                                        case UIAction.HandleMouseover:
                                            if ( AbilitySlots[slotIndex]?.DuringGame_IsHiddenByFlags() ?? false )
                                            {
                                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "ActorLoadoutSlot", slotIndex, 0 ), element, SideClamp.LeftOrRight,
                                                    TooltipNovelWidth.Smaller, TooltipExtraText.None, TooltipExtraRules.MustBeToRightOfCustomizeLoadout ) )
                                                {
                                                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                                                    novel.Icon = IconRefs.AbilityLocked.Icon;
                                                    novel.TitleUpperLeft.AddLang( "Locked" );
                                                    novel.Main.AddLang( "AbilityLocked_Details" ).Line();
                                                }
                                                break;
                                            }
                                            if ( slotInfo.Ability != null )
                                            {
                                                slotInfo.Ability.RenderAbilityTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, TooltipExtraRules.MustBeToRightOfCustomizeLoadout,
                                                    AbilityTooltipInstruction.ForEquipmentMenu, slotIndex, null, actorDGD, TooltipExtraText.None, null, null );
                                            }
                                            break;
                                    }
                                }, 12, TextWrapStyle.NoWrap_Ellipsis );
                                #endregion
                            }
                            else
                            {
                                debugStage = 8100;
                                #region Ability - RightColumn As Dropdown
                                UIFlow.AddDropdown( "DropdownPurple", Set, type_dDropdown, "AbilityDropdown", slotIndex, SimCommon.Turn + TimesOpened, actorDGD.NonSimUniqueID, bounds,
                                    delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                                    {
                                        if ( actorDGD == null )
                                            return;

                                        switch ( Action )
                                        {
                                            case UIAction.DropdownSelectionChanged:
                                                {
                                                    AbilityType abilityType = ExtraData.DropdownItem as AbilityType;
                                                    //if ( abilityType == null )
                                                    //    return;
                                                    AbilitySlots[slotIndex] = abilityType;
                                                }
                                                break;
                                            case UIAction.HandleMouseover:
                                                {
                                                    ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)element;
                                                    if ( elementAsType == null )
                                                        break;
                                                    AbilityType selected = elementAsType.CurrentlySelectedOption as AbilityType;
                                                    if ( selected == null )
                                                        break;

                                                    ExtraData.Buffer.EnsureResetForNextUpdate();
                                                    AbilityType currentEquipped = actorDGD.GetActorAbilityInSlotIndex( slotIndex );
                                                    if ( AbilitySlots[slotIndex] != currentEquipped )
                                                        ExtraData.Buffer.LineIfLastWrittenWasNotLine().StartColor( ColorTheme.Settings_CurrentDiffersFromDefaultYellow )
                                                        .AddLangAndAfterLineItemHeader( "SwitchingFromCurrentlyEquipped" ).AddRaw( currentEquipped == null ? LangCommon.None.Text : currentEquipped.GetDisplayName() );

                                                    selected?.RenderAbilityTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, TooltipExtraRules.MustBeToRightOfCustomizeLoadout,
                                                        AbilityTooltipInstruction.ForEquipmentMenu, slotIndex, null, actorDGD, TooltipExtraText.None, ExtraData.Buffer, null );                                                    
                                                }
                                                break;
                                            case UIAction.HandleDropdownItemMouseover:
                                                {
                                                    AbilityType abilityType = ExtraData.DropdownItem as AbilityType;
                                                    if ( abilityType == null )
                                                        return;

                                                    ExtraData.Buffer.EnsureResetForNextUpdate();
                                                    AbilityType currentEquipped = actorDGD.GetActorAbilityInSlotIndex( slotIndex );
                                                    if ( AbilitySlots[slotIndex] != currentEquipped && abilityType != currentEquipped )
                                                        ExtraData.Buffer.LineIfLastWrittenWasNotLine().StartColor( ColorTheme.Settings_CurrentDiffersFromDefaultYellow )
                                                        .AddLangAndAfterLineItemHeader( "SwitchingFromCurrentlyEquipped" ).AddRaw( currentEquipped == null ? LangCommon.None.Text : currentEquipped.GetDisplayName() );

                                                    abilityType?.RenderAbilityTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, TooltipExtraRules.MustBeToRightOfCustomizeLoadout,
                                                        AbilityTooltipInstruction.ForEquipmentMenu, slotIndex, null, actorDGD, TooltipExtraText.None, ExtraData.Buffer, null );
                                                }
                                                break;
                                            case UIAction.DropdownRenderSelectedItem:
                                                {
                                                    AbilityType abilityType = ExtraData.DropdownItem as AbilityType;
                                                    if ( abilityType == null )
                                                        return;

                                                    bool canEquip = true;
                                                    if ( abilityType.RequiredActorDataTypeForAbility != null )
                                                    {
                                                        if ( abilityType.RequiredActorDataMustBeAtLeast > actorDGD.NonSimActorDataMinMax.GetDisplayDict()[abilityType.RequiredActorDataTypeForAbility].LeftItem ) //min
                                                            canEquip = false;
                                                    }

                                                    abilityType.WriteOptionDisplayTextForDropdown( ExtraData.Buffer, canEquip ? ColorTheme.GetDataBlue( element.LastHadMouseWithin ) : ColorTheme.RedOrange2 );
                                                    ExtraData.Bool = true; //not sure what this does?
                                                }
                                                break;
                                            case UIAction.DropdownRenderPopupItem:
                                                {
                                                    AbilityType abilityType = ExtraData.DropdownItem as AbilityType;
                                                    if ( abilityType == null )
                                                        return;

                                                    bool canEquip = true;
                                                    if ( abilityType.RequiredActorDataTypeForAbility != null )
                                                    {
                                                        if ( abilityType.RequiredActorDataMustBeAtLeast > actorDGD.NonSimActorDataMinMax.GetDisplayDict()[abilityType.RequiredActorDataTypeForAbility].LeftItem ) //min
                                                            canEquip = false;
                                                    }

                                                    abilityType.WriteOptionDisplayTextForDropdown( ExtraData.Buffer, canEquip ? string.Empty : ColorTheme.RedOrange2 );
                                                    ExtraData.Bool = true; //not sure what this does?
                                                }
                                                break;
                                            case UIAction.DropdownOnUpdate:
                                                {
                                                    ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)element;
                                                    if ( elementAsType == null )
                                                        return;

                                                    int expectedCount = SortedVisibleAbilities.Count;
                                                    bool canBeSetToBlank = false;
                                                    if ( slotInfo == null || slotInfo.Ability == null )
                                                    {
                                                        canBeSetToBlank = true;
                                                        expectedCount++;
                                                    }

                                                    //this ability can be any of the valid sorted abilities
                                                    if ( elementAsType.GetItemCount() != expectedCount || !AbilitySlot_HasRegeneratedSinceLoad[slotIndex] )
                                                    {
                                                        AbilitySlot_HasRegeneratedSinceLoad[slotIndex] = true;
                                                        AbilityType selected = AbilitySlots[slotIndex];
                                                        elementAsType.ClearItems();
                                                        if ( canBeSetToBlank )
                                                            elementAsType.AddItem( EmptyOption, null == selected );
                                                        foreach ( KeyValuePair<AbilityType, bool> kv in SortedVisibleAbilities )
                                                        {
                                                            elementAsType.AddItem( kv.Key, kv.Key == selected );
                                                        }
                                                    }
                                                }
                                                break;
                                        }
                                    } );
                                #endregion
                            }

                            runningY += NORMAL_ROW_HEIGHT + NORMAL_ROW_BUFFER;
                        }
                    }
                }

                runningY += SECTION_EXTRA_ROW_BUFFER;
                runningY += ABILITY_SECTION_EXTRA_ROW_BUFFER;

                debugStage = 12100;

                Dictionary<EquipmentSlotType, RenamedStat> actorSlotCounts = actorDGD.GetEquipmentSlotCounts();
                foreach ( EquipmentSlotType slotType in EquipmentSlotTypeTable.Instance.Rows )
                {
                    debugStage = 12200;
                    int slotsHeldByThisActor = actorSlotCounts[slotType]?.Amount ?? 0;
                    if ( slotsHeldByThisActor <= 0 )
                        continue; //if this slot type is not relevant to this actor, then skip it!

                    for ( int slotIndexOuter = 0; slotIndexOuter < slotsHeldByThisActor; slotIndexOuter++ )
                    {
                        int slotIndex = slotIndexOuter;

                        debugStage = 13100;

                        #region Equipment - LeftColumn
                        bounds = ArcenFloatRectangle.CreateUnityRect( 5, runningY, LEFT_COLUMN_WIDTH, NORMAL_ROW_HEIGHT );
                        UIFlow.AddText( "HoverableHeader", Set, type_tLabelGeneric, slotType.ID, slotIndex, SimCommon.Turn + TimesOpened, actorDGD.NonSimUniqueID, "", bounds,
                            delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                                if ( actorDGD == null || slotType == null )
                                    return;

                                switch ( Action )
                                {
                                    case UIAction.GetTextToShowFromVolatile:
                                        {
                                            string color = string.Empty;
                                            if ( slotType.NonSim_SortedAvailableEquipment.Count <= 0 || actorDGD.AllEquipmentOptionsPerSlotType.Display[slotType] == 0 )
                                                color = ColorTheme.GetDisabledPurple( element.LastHadMouseWithin );
                                            else
                                            {
                                                if ( !(slotType.NonSim_ExistingEntries[slotIndex]?.GetIsAlreadyMatchForProposal( slotType.NonSim_WorkingTypes[slotIndex] ) ?? (slotType.NonSim_WorkingTypes[slotIndex] == null)) )
                                                    color = ColorTheme.Settings_CurrentDiffersFromDefaultYellow;
                                                else
                                                    color = ColorTheme.GetBasicLightTextPurple( element.LastHadMouseWithin );
                                            }
                                            ExtraData.Buffer.StartColor( color );
                                            ExtraData.Buffer.AddSpriteStyled_NoIndent( slotType.Icon, AdjustedSpriteStyle.InlineLarger1_2, color ).Space1x();
                                            ExtraData.Buffer.AddRaw( slotType.EquipmentPrefix.Text );
                                            //if ( slotType.NonSim_SortedAvailableEquipment.Count > 0 )
                                            //{
                                            //    ExtraData.Buffer.Space1x().StartSize70();
                                            //    ExtraData.Buffer.AddFormat1( "Parenthetical", actorDGD.AllEquipmentOptionsPerSlotType.Display[slotType], ColorTheme.Grayer );
                                            //}
                                        }
                                        break;
                                    case UIAction.HandleMouseover:
                                        {
                                            if ( slotType.NonSim_SortedAvailableEquipment.Count <= 0 )
                                            {
                                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "ActorLoadoutSlot", slotIndex, 0 ), element, SideClamp.LeftOrRight,
                                                    TooltipNovelWidth.Smaller, TooltipExtraText.None, TooltipExtraRules.MustBeToRightOfCustomizeLoadout ) )
                                                {
                                                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                                                    novel.Icon = IconRefs.AbilityLocked.Icon;
                                                    novel.TitleUpperLeft.AddLang( "Locked" );
                                                    novel.Main.AddLang( "NoEquipmentOfThisSortInventedYet" ).Line();
                                                }
                                            }
                                            else if ( actorDGD.AllEquipmentOptionsPerSlotType.Display[slotType] == 0 )
                                            {
                                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "ActorLoadoutSlot", slotIndex, 0 ), element, SideClamp.LeftOrRight,
                                                    TooltipNovelWidth.Smaller, TooltipExtraText.None, TooltipExtraRules.MustBeToRightOfCustomizeLoadout ) )
                                                {
                                                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                                                    novel.Icon = IconRefs.AbilityLocked.Icon;
                                                    novel.TitleUpperLeft.AddLang( "Locked" );
                                                    novel.Main.AddLang( "NoCompatibleEquipmentOfThisSortInventedYet" ).Line();
                                                }
                                            }
                                            else
                                            {
                                                UnitEquipmentType selected = slotType.NonSim_WorkingTypes[slotIndex];
                                                if ( selected != null )
                                                {
                                                    ExtraData.Buffer.EnsureResetForNextUpdate();
                                                    if ( !(slotType.NonSim_ExistingEntries[slotIndex]?.GetIsAlreadyMatchForProposal( slotType.NonSim_WorkingTypes[slotIndex] ) ?? (slotType.NonSim_WorkingTypes[slotIndex] == null)) )
                                                    {
                                                        UnitEquipmentType currentEquipped = slotType.NonSim_ExistingEntries[slotIndex]?.CurrentType;
                                                        ExtraData.Buffer.LineIfLastWrittenWasNotLine().StartColor( ColorTheme.Settings_CurrentDiffersFromDefaultYellow )
                                                            .AddLangAndAfterLineItemHeader( "SwitchingFromCurrentlyEquipped" ).AddRaw( currentEquipped == null ? LangCommon.None.Text : currentEquipped.GetDisplayName() );
                                                    }

                                                    selected.RenderEquipmentTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, TooltipInstruction.ForActorCustomization, 
                                                        null, actorDGD, slotType.NonSim_ExistingEntries[slotIndex]?.CurrentType,
                                                        TooltipExtraText.None, ExtraData.Buffer );
                                                }                                                
                                            }
                                        }
                                        break;
                                }
                            }, 9, TextWrapStyle.NoWrap_Ellipsis );
                        #endregion

                        debugStage = 14100;

                        bounds = ArcenFloatRectangle.CreateUnityRect( bounds.xMax + COLUMN_SPACER, runningY, RIGHT_COLUMN_WIDTH_EQUIPMENT, NORMAL_ROW_HEIGHT );
                        int turnsOnCooldown = (slotType.NonSim_ExistingEntries[slotIndex]?.TurnsOfCooldownBeforeCanSetAgain ?? 0);
                        if ( slotType.NonSim_SortedAvailableEquipment.Count <= 0 && slotType.NonSim_ExistingEntries[slotIndex]?.CurrentType == null )
                        {
                            debugStage = 15100;

                            #region Equipment - RightColumn As Label
                            UIFlow.AddText( "HoverableFalseDropdownPurple", Set, type_tLabelGeneric2, slotType.ID, slotIndex, SimCommon.Turn + TimesOpened, actorDGD.NonSimUniqueID, "", bounds,
                            delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                                if ( actorDGD == null || slotType == null )
                                    return;

                                string defaultText = actorSlotCounts[slotType]?.AlternateName?.Text??string.Empty;
                                switch ( Action )
                                {
                                    case UIAction.GetTextToShowFromVolatile:
                                        ExtraData.Buffer.StartSize80().AddRaw( defaultText.IsEmpty() ? "-" : defaultText, ColorTheme.GetDisabledPurple( element.LastHadMouseWithin ) );
                                        break;
                                    case UIAction.HandleMouseover:
                                        if ( defaultText.IsEmpty() )
                                        {
                                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( "ActorLoadoutSlot", slotIndex, 0 ), element, SideClamp.LeftOrRight,
                                                TooltipNovelWidth.Smaller, TooltipExtraText.None, TooltipExtraRules.MustBeToRightOfCustomizeLoadout ) )
                                            {
                                                novel.ShadowStyle = TooltipShadowStyle.Standard;
                                                novel.Icon = IconRefs.AbilityLocked.Icon;
                                                novel.TitleUpperLeft.AddLang( "Locked" );
                                                novel.Main.AddLang( "NoCompatibleEquipmentOfThisSortInventedYet" ).Line();
                                            }
                                        }
                                        break;
                                }
                            }, 12, TextWrapStyle.NoWrap_Ellipsis );
                            #endregion
                        }
                        else if ( turnsOnCooldown > 0 )
                        {
                            #region Equipment - RightColumn As Label
                            UIFlow.AddText( "HoverableFalseDropdownPurple", Set, type_tLabelGeneric2, slotType.ID, slotIndex, SimCommon.Turn + TimesOpened, actorDGD.NonSimUniqueID, "", bounds,
                            delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                                if ( actorDGD == null || slotType == null )
                                    return;

                                switch ( Action )
                                {
                                    case UIAction.GetTextToShowFromVolatile:
                                        ExtraData.Buffer.StartSize60();
                                        ExtraData.Buffer.AddSpriteStyled_NoIndent( IconRefs.Next_NextTurn.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.CategorySelectedBlue );
                                        ExtraData.Buffer.AddRaw( turnsOnCooldown.ToStringThousandsWhole(), ColorTheme.CategorySelectedBlue );
                                        ExtraData.Buffer.EndSize();

                                        ExtraData.Buffer.Position20();

                                        UnitEquipmentType item = slotType.NonSim_ExistingEntries[slotIndex]?.CurrentType;
                                        if ( item != null )
                                            item.WriteEquipmentTextForDropdown( ExtraData.Buffer, -1, ColorTheme.GetDataBlue( element.LastHadMouseWithin ), ColorTheme.GetDataBlue( element.LastHadMouseWithin ),
                                                true, true, null, actorDGD );
                                        break;
                                    case UIAction.HandleMouseover:
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( "ActorLoadoutSlot", slotIndex, 0 ), element, SideClamp.LeftOrRight,
                                            TooltipNovelWidth.Smaller, TooltipExtraText.None, TooltipExtraRules.MustBeToRightOfCustomizeLoadout ) )
                                        {
                                            novel.ShadowStyle = TooltipShadowStyle.Standard;
                                            novel.Icon = IconRefs.AbilityLocked.Icon;
                                            novel.TitleUpperLeft.AddLang( "Locked" );
                                            novel.Main.AddFormat1( "ThisEquipmentSlotOnCooldown", turnsOnCooldown ).Line();
                                        }
                                        break;
                                }
                            }, 12, TextWrapStyle.NoWrap_Ellipsis );
                            #endregion
                        }
                        else
                        {
                            debugStage = 16100;

                            #region Equipment - RightColumn As Dropdown
                            UIFlow.AddDropdown( "DropdownPurple", Set, type_dDropdown, slotType.ID, slotIndex, SimCommon.Turn + TimesOpened, actorDGD.NonSimUniqueID, bounds,
                            delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                                if ( actorDGD == null || slotType == null )
                                    return;

                                switch ( Action )
                                {
                                    case UIAction.DropdownSelectionChanged:
                                        {
                                            UnitEquipmentType equipmentType = ExtraData.DropdownItem as UnitEquipmentType;
                                            if ( equipmentType == null || equipmentType.NonSim_IsValidForActor )
                                                slotType.NonSim_WorkingTypes[slotIndex] = equipmentType;
                                        }
                                        break;
                                    case UIAction.HandleMouseover:
                                        {
                                            ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)element;
                                            if ( elementAsType == null )
                                                break;
                                            UnitEquipmentType selected = elementAsType.CurrentlySelectedOption as UnitEquipmentType;
                                            if ( selected != null )
                                            {
                                                ExtraData.Buffer.EnsureResetForNextUpdate();
                                                if ( !(slotType.NonSim_ExistingEntries[slotIndex]?.GetIsAlreadyMatchForProposal( slotType.NonSim_WorkingTypes[slotIndex] ) ?? (slotType.NonSim_WorkingTypes[slotIndex] == null)) )
                                                {
                                                    UnitEquipmentType currentEquipped = slotType.NonSim_ExistingEntries[slotIndex]?.CurrentType;
                                                    ExtraData.Buffer.LineIfLastWrittenWasNotLine().StartColor( ColorTheme.Settings_CurrentDiffersFromDefaultYellow )
                                                        .AddLangAndAfterLineItemHeader( "SwitchingFromCurrentlyEquipped" ).AddRaw( currentEquipped == null ? LangCommon.None.Text : currentEquipped.GetDisplayName() );
                                                }

                                                selected.RenderEquipmentTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, TooltipInstruction.ForActorCustomization, 
                                                    null, actorDGD, slotType.NonSim_ExistingEntries[slotIndex]?.CurrentType,
                                                    TooltipExtraText.None, ExtraData.Buffer );
                                            }
                                        }
                                        break;
                                    case UIAction.HandleDropdownItemMouseover:
                                        {
                                            UnitEquipmentType hovered = ExtraData.DropdownItem as UnitEquipmentType;
                                            if ( hovered != null )
                                            {
                                                ExtraData.Buffer.EnsureResetForNextUpdate();
                                                if ( !(slotType.NonSim_ExistingEntries[slotIndex]?.GetIsAlreadyMatchForProposal( slotType.NonSim_WorkingTypes[slotIndex] ) ?? (slotType.NonSim_WorkingTypes[slotIndex] == null)) )
                                                {
                                                    UnitEquipmentType currentEquipped = slotType.NonSim_ExistingEntries[slotIndex]?.CurrentType;
                                                    ExtraData.Buffer.LineIfLastWrittenWasNotLine().StartColor( ColorTheme.Settings_CurrentDiffersFromDefaultYellow )
                                                        .AddLangAndAfterLineItemHeader( "SwitchingFromCurrentlyEquipped" ).AddRaw( currentEquipped == null ? LangCommon.None.Text : currentEquipped.GetDisplayName() );
                                                }

                                                hovered.RenderEquipmentTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard,
                                                    TooltipInstruction.ForActorCustomization, null, actorDGD, slotType.NonSim_WorkingTypes[slotIndex], TooltipExtraText.None, ExtraData.Buffer );
                                            }                                            
                                        }
                                        break;
                                    case UIAction.DropdownRenderSelectedItem:
                                        {
                                            UnitEquipmentType item = ExtraData.DropdownItem as UnitEquipmentType;
                                            if ( item == null )
                                            {
                                                string defaultText = actorSlotCounts[slotType]?.AlternateName?.Text ?? string.Empty;
                                                if ( !defaultText.IsEmpty() )
                                                    ExtraData.Buffer.StartSize80().AddRaw( defaultText, ColorTheme.Gray );
                                                return;
                                            }

                                            bool isNewSelected = false;
                                            int effectiveTurns;
                                            if ( !(slotType.NonSim_ExistingEntries[slotIndex]?.GetIsAlreadyMatchForProposal( slotType.NonSim_WorkingTypes[slotIndex] ) ?? (slotType.NonSim_WorkingTypes[slotIndex] == null)) )
                                            {
                                                isNewSelected = true;
                                                effectiveTurns = item.TurnsRequiredToEquip;
                                            }
                                            else
                                            {
                                                //this was a match
                                                effectiveTurns = 0;
                                            }

                                            item.WriteEquipmentTextForDropdown( ExtraData.Buffer, effectiveTurns, isNewSelected ?
                                                ColorTheme.Settings_CurrentDiffersFromDefaultYellow : ColorTheme.GetDataBlue( element.LastHadMouseWithin ),
                                                effectiveTurns <= 0 ? ColorTheme.GetDataBlue( element.LastHadMouseWithin ) : (isNewSelected ? ColorTheme.Settings_CurrentDiffersFromDefaultYellow :
                                                ColorTheme.GetDataBlue( element.LastHadMouseWithin ) ), 
                                                !isNewSelected, false, null, actorDGD );
                                            ExtraData.Bool = true;
                                        }
                                        break;
                                    case UIAction.DropdownRenderPopupItem:
                                        {
                                            UnitEquipmentType item = ExtraData.DropdownItem as UnitEquipmentType;
                                            if ( item == null )
                                            {
                                                string defaultText = actorSlotCounts[slotType]?.AlternateName?.Text ?? string.Empty;
                                                if ( !defaultText.IsEmpty() )
                                                    ExtraData.Buffer.StartSize80().AddNeverTranslated( "<pos=33>", false ).AddRaw( defaultText, ColorTheme.Gray );
                                                return;
                                            }

                                            bool isAlreadySelected = false;
                                            int effectiveTurns;
                                            if ( slotType.NonSim_ExistingEntries[slotIndex]?.GetIsAlreadyMatchForProposal( item ) ?? false )
                                            {
                                                //this was a match
                                                isAlreadySelected = true;
                                                effectiveTurns = 0;
                                            }
                                            else
                                            {
                                                effectiveTurns = item.TurnsRequiredToEquip;
                                            }

                                            item.WriteEquipmentTextForDropdown( ExtraData.Buffer, effectiveTurns, ColorTheme.CategorySelectedBlue, ColorTheme.CategorySelectedBlue,
                                                isAlreadySelected, false, null, actorDGD );
                                            ExtraData.Bool = true;
                                        }
                                        break;
                                    case UIAction.DropdownOnUpdate:
                                        {
                                            ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)element;
                                            if ( elementAsType == null )
                                                return;

                                            int count = slotType.NonSim_SortedAvailableEquipment.Count + 1;

                                            UnitEquipmentType selected = slotType.NonSim_WorkingTypes[slotIndex];

                                            //this equipment can be any of the valid sorted equipments
                                            if ( elementAsType.GetItemCount() != count || elementAsType.CurrentlySelectedOption != selected )
                                            {
                                                elementAsType.ClearItems();
                                                elementAsType.AddItem( EmptyOption, null == selected );
                                                foreach ( UnitEquipmentType item in slotType.NonSim_SortedAvailableEquipment )
                                                {
                                                    elementAsType.AddItem( item, item == selected );
                                                }
                                            }
                                        }
                                        break;
                                }
                            } );
                            #endregion
                        }

                        runningY += NORMAL_ROW_HEIGHT + NORMAL_ROW_BUFFER;
                    }
                }

                debugStage = 40100;

                runningY += SECTION_EXTRA_ROW_BUFFER;

                debugStage = 55100;

                debugStage = 66100;

                debugStage = 77100;

                #region CommitChanges
                bounds = ArcenFloatRectangle.CreateUnityRect( 5 + HALF_WIDTH_IGNORESCROLLBAR_MINUS_SPACER + COLUMN_SPACER, runningY, HALF_WIDTH_IGNORESCROLLBAR_MINUS_SPACER, BUTTON_ROW_HEIGHT );
                runningY += BUTTON_ROW_HEIGHT + NORMAL_ROW_BUFFER;
                bSave.Instance.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    if ( Action == UIAction.OnClick ) //this is just a safety thing if someone were to be an incredibly fast clicker
                        RecalculateChangeCostsAndSuch( actorDGD );

                    int changeCount = ProposedChangeCount;

                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            if ( actorDGD == null )
                            {
                                ExtraData.Buffer.AddLang( "CannotActRightNow", ColorTheme.GetDisabledPurple( element.LastHadMouseWithin ) );
                                bSave.Instance.SetRelatedImage0SpriteIfNeeded( bSave.Instance.Element.RelatedSprites[0] );
                            }
                            else if ( changeCount == 0 )
                            {
                                ExtraData.Buffer.AddLang( "Equipment_NoChangesMade", ColorTheme.GetDisabledPurple( element.LastHadMouseWithin ) );
                                bSave.Instance.SetRelatedImage0SpriteIfNeeded( bSave.Instance.Element.RelatedSprites[0] );
                            }
                            else
                            {
                                ExtraData.Buffer.AddLang( "Equipment_ApplyChanges", ColorTheme.GetBasicLightTextBlue( element.LastHadMouseWithin ) );
                                bSave.Instance.SetRelatedImage0SpriteIfNeeded( bSave.Instance.Element.RelatedSprites[1] );
                            }
                            break;
                        case UIAction.HandleMouseover:
                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( "ActorLoadoutSlot", "CommitChanges" ), element, SideClamp.LeftOrRight,
                                TooltipNovelWidth.Simple, TooltipExtraText.None, TooltipExtraRules.MustBeToRightOfCustomizeLoadout ) )
                            {
                                novel.ShadowStyle = TooltipShadowStyle.Standard;
                                novel.TitleUpperLeft.AddLang( "Equipment_ApplyChanges" );
                                novel.Main.AddLang( "Equipment_Button_Tooltip" ).Line();
                            }
                            break;
                        case UIAction.OnClick:
                            if ( changeCount > 0 && actorDGD != null )
                            {
                                #region Apply The Changes
                                CommitChangesIfPossible();
                                ClearChanges(); //to prevent double-saving

                                //and lastly, close the window
                                Instance.Close( WindowCloseReason.UserDirectRequest );
                                #endregion
                            }
                            else
                                ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                            break;
                    }
                } );
                #endregion

            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "ActorLoadout-PopulateFreeFormControls", debugStage, e, Verbosity.ShowAsError );
            }

            //         bMainContentParent.ParentRT.UI_SetHeight(runningY);
            lastYHeightOfInterior = Mathf.CeilToInt( runningY + NORMAL_ROW_BUFFER );
		}

        #region CommitChangesIfPossible
        private static void CommitChangesIfPossible()
        {
            MobileActorTypeDuringGameData actorDGD = DuringGameDataBeingExamined;
            if ( actorDGD == null )
                return;
            //now apply the equipment list
            MobileActorTypeDuringGameData typeDuringGameData = actorDGD;
            typeDuringGameData.ReplaceEquipmentListContents( ProposedEquipmentList );

            if ( actorDGD.IsMachineActor )
            {
                bool isVehicle = actorDGD.ParentVehicleTypeOrNull != null;
                int maxSlot = isVehicle ? AbilitySlotInfo.MAX_SLOT_VEHICLES : AbilitySlotInfo.MAX_SLOT_UNITS;

                //now apply the abilities
                for ( int slotIndex = 1; slotIndex <= maxSlot; slotIndex++ )
                {
                    if ( !FlagRefs.HasUnlockedAbilityAdjustments.DuringGameplay_IsTripped )
                        break;
                    if ( actorDGD.GetActorAbilityInSlotIndex( slotIndex ) != AbilitySlots[slotIndex] )
                    {
                        typeDuringGameData.SetNewActorAbilityInSlotIndex( slotIndex, AbilitySlots[slotIndex] );
                    }
                }
            }
        }
        #endregion

        #region tHeaderText
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.StartColor( ColorTheme.GetBasicLightTextPurple( false ) );
                Buffer.AddLang( "LoadoutCustomization_Header" );
            }
            public override void OnUpdate() { }
        }
        #endregion

        #region tUnitNameText
        public class tUnitNameText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                ISimMapMobileActor actorOrNull = ActorBeingExaminedOrNull;
                MobileActorTypeDuringGameData actorDGD = DuringGameDataBeingExamined;
                if ( actorDGD == null )
                    return;

                if ( actorOrNull != null )
                    Buffer.AddRaw( actorOrNull.GetDisplayName() );
                else
                    Buffer.AddRaw( actorDGD.GetDisplayName() );
            }
            public override void OnUpdate() { }
        }
        #endregion

        #region tUnitSubText
        public class tUnitSubText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                ISimMapMobileActor actorOrNull = ActorBeingExaminedOrNull;
                MobileActorTypeDuringGameData actorDGD = DuringGameDataBeingExamined;
                if ( actorDGD == null )
                    return;

                if ( actorOrNull != null )
                    Buffer.AddRaw( actorDGD.GetDisplayName() ); //show the type name
                else
                {
                    if ( actorDGD.ParentUnitTypeOrNull != null )
                    {
                        if ( actorDGD.ParentUnitTypeOrNull.IsConsideredMech )
                            Buffer.AddLang( "StandardMech" );
                        else
                            Buffer.AddLang( "StandardAndroid" );
                    }
                    else if ( actorDGD.ParentVehicleTypeOrNull != null)
                        Buffer.AddLang( "StandardVehicle" );
                    else if ( actorDGD.ParentNPCUnitTypeOrNull != null )
                    {
                        if ( actorDGD.ParentNPCUnitTypeOrNull.CostsToCreateIfBulkAndroid.Count > 0 )
                            Buffer.AddLang( "BulkAndroid" );
                        //else if ( npc.GetIsPlayerControlled() && npc.UnitType.CapturedUnitCapacityRequired > 0 )
                        //    Buffer.AddLang( "CapturedUnit" );
                        //else if ( npc.GetIsPartOfPlayerForcesInAnyWay() )
                        //    Buffer.AddLang( "AlliedUnit" );
                        //else
                        //    Buffer.AddLang( "ThirdPartyUnit" );
                    }
                }
            }
            public override void OnUpdate() { }
        }
        #endregion

        #region tCustomizingLabel
        public class tCustomizingLabel : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.StartColor( ColorTheme.GetBasicLightTextPurple( this.Element.LastHadMouseWithin ) );
                Buffer.AddLangAndAfterLineItemHeader( "CustomizingUnitClass" );
            }
            public override void OnUpdate() { }

            public override void HandleMouseover()
            {
                MobileActorTypeDuringGameData actorDGD = DuringGameDataBeingExamined;
                if ( actorDGD == null )
                    return;

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "ActorLoadout", "ClassNotice" ), this.Element, SideClamp.LeftOrRight,
                    TooltipNovelWidth.Simple, TooltipExtraText.None, TooltipExtraRules.MustBeToRightOfCustomizeLoadout ) )
                {
                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                    novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "CustomizingUnitClass" ).AddRaw( actorDGD.GetDisplayName(), ColorTheme.DataBlue );
                    novel.Main.AddLang( "CustomizingUnitClass_Note" ).Line();
                }
            }
        }
        #endregion

        #region tCustomizingData
        public class tCustomizingData : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                MobileActorTypeDuringGameData actorDGD = DuringGameDataBeingExamined;
                if ( actorDGD == null )
                    return;
                Buffer.StartColor( ColorTheme.GetDataBlue( this.Element.LastHadMouseWithin ) );
                Buffer.AddRaw( actorDGD.GetDisplayName() );
            }
            public override void OnUpdate() { }

            public override void HandleMouseover()
            {
                MobileActorTypeDuringGameData actorDGD = DuringGameDataBeingExamined;
                if ( actorDGD == null )
                    return;

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "ActorLoadout", "ClassNotice" ), this.Element, SideClamp.LeftOrRight,
                    TooltipNovelWidth.Simple, TooltipExtraText.None, TooltipExtraRules.MustBeToRightOfCustomizeLoadout ) )
                {
                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                    novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "CustomizingUnitClass" ).AddRaw( actorDGD.GetDisplayName(), ColorTheme.DataBlue );
                    novel.Main.AddLang( "CustomizingUnitClass_Note" ).Line();
                }
            }
        }
        #endregion

        public class bMainContentParent : CustomUIAbstractBase
        {
            public static Transform ParentT;
            public static RectTransform ParentRT;
            public override void OnUpdate()
            {
                if ( ParentT == null )
                {
                    ParentT = this.Element.transform;
                    ParentRT = (RectTransform)ParentT;
                }
            }
        }

        public class tLabelGeneric : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer buffer )
            {
            }
        }

        public class tLabelGeneric2 : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer buffer )
            {
            }
        }

        public class bButtonGeneric : ButtonAbstractBase
        {

        }

        public class dDropdown : DropdownAbstractBase
        {
        }

        public class bButton : ButtonAbstractBase
        {
        }

        private static readonly optionEmpty EmptyOption = new optionEmpty();

        #region optionEmpty
        public class optionEmpty : IArcenDropdownOption
        {
            public bool Equals( IArcenDropdownOption obj )
            {
                if ( obj is optionEmpty )
                    return true;
                return false;
            }

            public object GetItem()
            {
                return this;
            }

            public string GetOptionValueForDropdown()
            {
                return LangCommon.None.Text;
            }

            public string GetStringForDropdownMatch()
            {
                return LangCommon.None.Text;
            }

            public void WriteOptionDisplayTextForDropdown( ArcenDoubleCharacterBuffer Buffer )
            {
                //do nothing, leaving blank on purpose
            }
        }
        #endregion

        #region bExit
        public class bExit : ButtonAbstractBaseWithImage
        {
            public static bExit Original;
            public bExit() { if ( Original == null ) Original = this; }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_ActorCustomizeLoadout.Instance.ClearChanges(); //wipe the changes
                Instance.Close( WindowCloseReason.UserDirectRequest );
                return MouseHandlingResult.None;
            }
        }
        #endregion

        #region bCancel
        public class bCancel : ButtonAbstractBase
        {
            public static bCancel Instance;
            public bCancel() { if ( Instance == null ) Instance = this; }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.StartColor( ColorTheme.GetBasicLightTextPurple( this.Element.LastHadMouseWithin ) );
                Buffer.AddRaw( LangCommon.Popup_Common_Cancel.Text );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Window_ActorCustomizeLoadout.Instance.ClearChanges(); //wipe the changes
                Window_ActorCustomizeLoadout.Instance.Close( WindowCloseReason.UserDirectRequest );
                return MouseHandlingResult.None;
            }
        }
        #endregion

        #region bSave
        public class bSave : ButtonAbstractBase
        {
            public static bSave Instance;
            public bSave() { if ( Instance == null ) Instance = this; }

            public GetOrSetUIData UIDataController;

            public override void OnUpdateSub() { }

            public void Assign( GetOrSetUIData UIDataController )
            {
                this.UIDataController = UIDataController;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = UIDataController;
            }
        }
        #endregion

        #region bScrap
        public class bScrap : ButtonAbstractBase
        {
            public static bScrap Instance;
            public bScrap() { if ( Instance == null ) Instance = this; }

            public GetOrSetUIData UIDataController;

            public override void OnUpdateSub() { }

            public void Assign( GetOrSetUIData UIDataController )
            {
                this.UIDataController = UIDataController;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = UIDataController;
            }

            public override bool GetShouldBeHidden()
            {
                return this.UIDataController == null;
            }
        }
        #endregion

        #region bRename
        public class bRename : ButtonAbstractBase
        {
            public static bRename Instance;
            public bRename() { if ( Instance == null ) Instance = this; }

            public GetOrSetUIData UIDataController;

            public override void OnUpdateSub() { }

            public void Assign( GetOrSetUIData UIDataController )
            {
                this.UIDataController = UIDataController;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = UIDataController;
            }

            public override bool GetShouldBeHidden()
            {
                return this.UIDataController == null;
            }
        }
        #endregion

        #region iPortrait
        public class iPortrait : ImageAbstractBase
        {
            public static iPortrait Instance;
            public iPortrait() { if ( Instance == null ) Instance = this; }

            private Sprite lastSprite = null;
            public void SetSpriteIfNeeded( Sprite sprite )
            {
                if ( this.lastSprite == sprite ) 
                    return;
                this.Element.ReferenceImage.sprite = sprite;
            }
        }
        #endregion
    }
}
