using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.External
{    
    public static class AttackHelper
    {
        #region DoCannotAffordPopupAtMouseIfNeeded
        private static float lastTimeDidDoNotAffordPopup = 0;
        public static void DoCannotAffordPopupAtMouseIfNeeded( MobileActorTypeDuringGameData DuringGameData, Vector3 location )
        {
            if ( DuringGameData == null || DuringGameData.EffectiveCostsPerAttack.Count == 0 )
                return;
            if ( float.IsInfinity( location.x ) || float.IsNaN( location.x ) )
                return;
            if ( ArcenTime.AnyTimeSinceStartF - lastTimeDidDoNotAffordPopup < 1f )
                return;

            bool couldDoAll = true;
            foreach ( KeyValuePair<ResourceType, int> kv in DuringGameData.EffectiveCostsPerAttack.GetDisplayDict() )
            {
                if ( kv.Value > kv.Key.Current )
                {
                    couldDoAll = false;
                    break;
                }
            }
            if ( couldDoAll )
                return;

            ArcenDoubleCharacterBuffer popupBuffer = DamageTextPopups.GetTextBuffer();
            if ( popupBuffer == null )
                return;

            popupBuffer.Clear();

            bool isFirst = true;
            foreach ( KeyValuePair<ResourceType, int> kv in DuringGameData.EffectiveCostsPerAttack.GetDisplayDict() )
            {
                if ( kv.Value > kv.Key.Current )
                {
                    if ( isFirst )
                        isFirst = false;
                    else
                        popupBuffer.Space3x();

                    popupBuffer.StartNoBr().AddSpriteStyled_NoIndent( kv.Key.Icon, AdjustedSpriteStyle.InlineLarger1_2, kv.Key.IconColorHex );
                    popupBuffer.AddRaw( kv.Value.ToStringThousandsWhole(), ColorTheme.RedOrange2 );
                }
            }

            DamageTextPopups.FloatingDamageTextPopup newDamageText = DamageTextPopups.CreateNewFromTextBuffer( popupBuffer,
                location, location.PlusY( 2f ), 2f, MathA.Max( 2, GameSettings.Current.GetInt( "DamagePopup_LingerTime" ) ) );

            lastTimeDidDoNotAffordPopup = ArcenTime.AnyTimeSinceStartF;
        }
        #endregion

        #region GetTargetVisible
        public static bool GetTargetVisible( ISimMapActor Attacker, ISimMapActor Target )
        {
            if ( Target.IsFullDead || Target.IsCloaked )
                return false;
            if ( Target is ISimNPCUnit )
            {
                //do NOT check GetIsCurrentlyInvisible, it will throw errors
            }
            else if ( Target is ISimMachineUnit machineUnit )
            {
                if ( machineUnit.GetIsCurrentlyInvisible( Attacker is ISimMachineActor ? InvisibilityPurpose.ForPlayerTargeting : InvisibilityPurpose.ForNPCTargeting ) )
                    return false;
            }
            else if ( Target is ISimMachineVehicle machineVehicle )
            {
                if ( machineVehicle.GetIsCurrentlyInvisible( Attacker is ISimMachineActor ? InvisibilityPurpose.ForPlayerTargeting : InvisibilityPurpose.ForNPCTargeting ) )
                    return false;
            }
            return true;
        }
        #endregion

        #region GetTargetVisibleForNPCs
        public static bool GetTargetVisibleForNPCs( ISimMapActor Target )
        {
            if ( Target.IsFullDead || Target.IsCloaked )
                return false;
            if ( Target is ISimNPCUnit )
            {
                //do NOT check GetIsCurrentlyInvisible, it will throw errors
            }
            else if ( Target is ISimMachineUnit machineUnit )
            {
                if ( machineUnit.GetIsCurrentlyInvisible( InvisibilityPurpose.ForNPCTargeting ) )
                    return false;
            }
            else if ( Target is ISimMachineVehicle machineVehicle )
            {
                if ( machineVehicle.GetIsCurrentlyInvisible(  InvisibilityPurpose.ForNPCTargeting ) )
                    return false;
            }
            return true;
        }
        #endregion

        #region GetTargetIsInRange
        public static bool GetTargetIsInRange( ISimMapActor Attacker, ISimMapActor Target )
        {
            if ( !GetTargetVisible( Attacker, Target ) ) 
                return false;

            float attackerRangeSquared = Attacker.GetAttackRangeSquared();
            if ( (Attacker.GetDrawLocation() - Target.GetDrawLocation()).GetSquareGroundMagnitude() > attackerRangeSquared )
                return false;
            return true;
        }
        #endregion

        #region ApplyBasicsOfAttacksToUnit
        public static void ApplyBasicsOfAttacksToUnit( ISimMachineActor Attacker, ISimNPCUnit targetNPC, bool IsMoraleBaseAttack )
        {
            if ( targetNPC.IsCityConflictUnit == null )
            {
                if ( !IsMoraleBaseAttack )
                {
                    if ( targetNPC.UnitType.AppliedOutcastBadgeIfAttackedByMachine != null && Attacker.OutcastLevel < 1 )
                        Attacker.AddOrRemoveBadge( targetNPC.UnitType.AppliedOutcastBadgeIfAttackedByMachine, true );
                }

                if ( Attacker is ISimMachineUnit AttackerMachineUnit )
                {
                    if ( AttackerMachineUnit.ContainerLocation.Get() is ISimMachineVehicle containerVehicle )
                    {
                        if ( !IsMoraleBaseAttack )
                        {
                            if ( targetNPC.UnitType.AppliedOutcastBadgeIfAttackedByMachine != null && containerVehicle.OutcastLevel < 1 )
                                containerVehicle.AddOrRemoveBadge( targetNPC.UnitType.AppliedOutcastBadgeIfAttackedByMachine, true );
                        }

                        containerVehicle.ApplyVisibilityFromAction( ActionVisibility.IsAttack );
                    }
                }
            }

            Attacker.ApplyVisibilityFromAction( ActionVisibility.IsAttack );
        }
        #endregion

        #region DoPopupTextAgainstNPCTarget
        public static void DoPopupTextAgainstNPCTarget( ISimNPCUnit Target, ArcenDoubleCharacterBuffer popupBuffer, float ExtraStartOffset = 0f )
        {
            if ( popupBuffer == null )
                return;

            Vector3 startLocation = Target.GetPositionForCollisions().PlusY ( ExtraStartOffset );
            Vector3 endLocation = startLocation.PlusY( Target.GetHalfHeightForCollisions() + 0.2f );

            DamageTextPopups.FloatingDamageTextPopup newDamageText = DamageTextPopups.CreateNewFromTextBuffer( popupBuffer,
                startLocation, endLocation, 0.7f, MathA.Max( 2, GameSettings.Current.GetInt( "DamagePopup_LingerTime" ) ) );
            newDamageText.MoraleDamageIncluded = 0;
            newDamageText.PhysicalDamageIncluded = 0;
            newDamageText.SquadDeathsIncluded = 0;
            Target.MostRecentDamageText = newDamageText;
        }
        #endregion

        //public static AttackAmounts FullWrapper( ISimMapActor Attacker, ISimMapActor Target, CalculationType CalcType,
        //    MersenneTwister RandIfNotPrediction, float ExtraAttackMultiplier, string ExtraAttackReasonLang, bool CheckCloakedStatus,
        //    int ImagineThisAmountOfAttackerHealthWasLost, ArcenCharacterBufferBase BufferOrNull )
        //{
        //    //this outer method exists just to be in the stack trace
        //    return HandleGeneralAttackLogic_Inner( Attacker, Target, PredictionOnly, RandIfNotPrediction, ExtraAttackMultiplier, ExtraAttackReasonLang, CheckCloakedStatus,
        //        ImagineThisAmountOfAttackerHealthWasLost, BufferOrNull );
        //}

        public static AttackAmounts PredictNPCDamageForTargeting( ISimMapActor Attacker, ISimMapActor Target, bool CheckCloakedStatus, bool CheckTakeCoverStatus, bool ImagineWillBeInCover, CalculationType PredictionType )
        {
            if ( PredictionType == CalculationType.ForExecution )
            {
                ArcenDebugging.LogSingleLine( "Called PredictNPCDamageForTargeting with CalculationType.ForExecution!", Verbosity.ShowAsError );
                return AttackAmounts.Zero();
            }
            //this outer method exists just to be in the stack trace
            return HandleGeneralAttackLogic_Inner( Attacker, Target, PredictionType, null, false, 0f, CheckCloakedStatus, 
                CheckTakeCoverStatus, ImagineWillBeInCover, false, Vector3.zero, 0, false, AttackAmounts.Zero(), false, null, null );
        }

        public static AttackAmounts PredictNPCDamageForImmediateFiringSolution_PrimaryTarget( ISimMapActor Attacker, ISimMapActor Target,
            MersenneTwister RandIfNotPrediction )
        {
            //this outer method exists just to be in the stack trace
            return HandleGeneralAttackLogic_Inner( Attacker, Target, CalculationType.ForExecution,
                RandIfNotPrediction, false, 0f, true, true, false, false, Vector3.zero,
                0, true, AttackAmounts.Zero(), false, null, null );
        }

        public static AttackAmounts PredictNPCDamageForImmediateFiringSolution_SecondaryTarget( ISimMapActor Attacker, ISimMapActor Target,
            MersenneTwister RandIfNotPrediction, float intensityMultiplier )
        {
            //this outer method exists just to be in the stack trace
            return HandleGeneralAttackLogic_Inner( Attacker, Target, CalculationType.ForExecution,
                RandIfNotPrediction, false, intensityMultiplier, true, true, false, false, Vector3.zero,
                0, true, AttackAmounts.Zero(), 
                true, //skip caring about
                null, null );
        }

        public static AttackAmounts DoNPCImmediateAttack_UsePriorCalculation_PrimaryOrSecondary( ISimMapActor Attacker, ISimMapActor Target,
            MersenneTwister RandIfNotPrediction, AttackAmounts DamageAmountWeAlreadyCalculated )
        {
            //this outer method exists just to be in the stack trace
            return HandleGeneralAttackLogic_Inner( Attacker, Target, CalculationType.ForExecution, 
                RandIfNotPrediction, false, 0f, true, true, false, false, Vector3.zero,
                0, false, DamageAmountWeAlreadyCalculated, true, //skip caring about range, as we already calculated it was okay before whatever might have shifted slightly
                null, null );
        }

        public static AttackAmounts DoNPCDelayedAttack_UsePriorCalculation_PrimaryOrSecondary( ISimMapActor Attacker, ISimMapActor Target,
            MersenneTwister RandIfNotPrediction, AttackAmounts DamageAmountWeAlreadyCalculated )
        {
            if ( DamageAmountWeAlreadyCalculated.IsEmpty() )
            {
                ArcenDebugging.LogWithStack( "Passed DamageAmountWeAlreadyCalculated as " + DamageAmountWeAlreadyCalculated + 
                    " to DoNPCDelayedAttack_UsePriorCalculation!", Verbosity.ShowAsError );
                return AttackAmounts.Zero();
            }

            //this outer method exists just to be in the stack trace
            AttackAmounts damageDone = HandleGeneralAttackLogic_Inner( Attacker, Target, CalculationType.ForExecution,
                RandIfNotPrediction, false, 0f, true, true, false, false, Vector3.zero,
                0, false, DamageAmountWeAlreadyCalculated, true, //skip caring about range, as we already calculated it was okay before whatever might have shifted slightly
                null, null );

            //note: the above is handled in a place that is easier to see that, now
            //Target.AlterIncomingDamageActual( -damageDone ); //so we are not overestimating

            return damageDone;
        }

        public static AttackAmounts DoNPCPrecalculationOfAttack( ISimMapActor Attacker, ISimMapActor Target,
            MersenneTwister RandIfNotPrediction )
        {
            //this outer method exists just to be in the stack trace
            return HandleGeneralAttackLogic_Inner( Attacker, Target, CalculationType.ForExecution,
                RandIfNotPrediction, false, 0f, true, true, false, false, Vector3.zero,
                0, true, AttackAmounts.Zero(), false, null, null );
        }

        public static AttackAmounts HandleAttackPredictionDetailsForTooltip( ISimMapActor Attacker, ISimMapActor Target,
            bool IsAndroidLeapingFromBeyondAttackRange, bool CheckCloakedStatus, bool CheckTakeCoverStatus, bool ImagineWillBeInCover, CalculationType PredictionType,
            int ImagineThisAmountOfAttackerHealthWasLost, ArcenCharacterBufferBase BufferOrNull, bool ImagineAttackerWillHaveMoved, Vector3 NewAttackerLocation )
        {
            if ( PredictionType == CalculationType.ForExecution )
            {
                ArcenDebugging.LogSingleLine( "Called HandleAttackPredictionDetailsForTooltip with CalculationType.ForExecution!", Verbosity.ShowAsError );
                return AttackAmounts.Zero();
            }
            //this outer method exists just to be in the stack trace
            return HandleGeneralAttackLogic_Inner( Attacker, Target, PredictionType, null, IsAndroidLeapingFromBeyondAttackRange, 0f,
                CheckCloakedStatus, CheckTakeCoverStatus, ImagineWillBeInCover, ImagineAttackerWillHaveMoved, NewAttackerLocation,
                ImagineThisAmountOfAttackerHealthWasLost, false, AttackAmounts.Zero(), false, BufferOrNull, null );
        }

        public static AttackAmounts HandlePlayerAttackPrediction( ISimMapActor Attacker, ISimMapActor Target,
            bool IsAndroidLeapingFromBeyondAttackRange, CalculationType PredictionType,
            int ImagineThisAmountOfAttackerHealthWasLost, bool ImagineAttackerWillHaveMoved, Vector3 NewAttackerLocation, ArcenCharacterBufferBase SecondaryBufferForKeyNotes )
        {
            if ( PredictionType == CalculationType.ForExecution )
            {
                ArcenDebugging.LogSingleLine( "Called HandlePlayerAttackPrediction with CalculationType.ForExecution!", Verbosity.ShowAsError );
                return AttackAmounts.Zero();
            }
            //this outer method exists just to be in the stack trace
            return HandleGeneralAttackLogic_Inner( Attacker, Target, PredictionType, null, IsAndroidLeapingFromBeyondAttackRange, 0f, true, true, false, ImagineAttackerWillHaveMoved, NewAttackerLocation,
                ImagineThisAmountOfAttackerHealthWasLost, false, AttackAmounts.Zero(), false, null, SecondaryBufferForKeyNotes );
        }

        public static AttackAmounts HandleAttackPredictionAgainstPlayer( ISimMapActor Attacker, ISimMapMobileActor Target,
            bool CheckCloakedStatus, bool CheckTakeCoverStatus, bool ImagineWillBeInCover,
            int ImagineThisAmountOfAttackerHealthWasLost, MersenneTwister Rand )
        {
            //this outer method exists just to be in the stack trace
            return HandleGeneralAttackLogic_Inner( Attacker, Target, CalculationType.ForExecution, Rand, false, 0f, 
                CheckCloakedStatus, CheckTakeCoverStatus, ImagineWillBeInCover, false, Vector3.zero,
                ImagineThisAmountOfAttackerHealthWasLost, true, AttackAmounts.Zero(), true, null, null );
        }

        public static AttackAmounts HandlePlayerMeleeAttackLogic( ISimMapActor Attacker, ISimMapActor Target,
            MersenneTwister RandIfNotPrediction, bool IsAndroidLeapingFromBeyondAttackRange )
        {
            //this outer method exists just to be in the stack trace
            return HandleGeneralAttackLogic_Inner( Attacker, Target, CalculationType.ForExecution, 
                RandIfNotPrediction, IsAndroidLeapingFromBeyondAttackRange, 0f, true, true, false, false, Vector3.zero, //this is for the actual melee logic, so the unit has already moved all it is going to
                0, false, AttackAmounts.Zero(), false, null, null );
        }

        #region HandlePlayerRangedAttackLogic
        public static AttackAmounts HandlePlayerRangedAttackLogic( ISimMapActor Attacker, ISimMapActor Target,
            MersenneTwister RandIfNotPrediction, bool IsAndroidLeapingFromBeyondAttackRange )
        {
            MapTile tile = Target.CalculateMapCell()?.ParentTile; //get this before the target is struck, since we might not be able to afterward
            Vector3 targetLoc = Target.GetDrawLocation();

            AttackAmounts totalDamageDealt = HandleGeneralAttackLogic_Inner( Attacker, Target, CalculationType.ForExecution, 
                RandIfNotPrediction, IsAndroidLeapingFromBeyondAttackRange, 0f, true, true, false, false, Vector3.zero,
                0, false, AttackAmounts.Zero(), false, null, null );

            AttackAmounts originalDamage = totalDamageDealt;

            //now see if there is an area of attack also happening
            float attackAreaSquared = Attacker.GetAreaOfAttackSquared();
            if ( attackAreaSquared > 0 )
            {
                if ( tile != null )
                {
                    float intensityMultiplier = Attacker.GetAreaOfAttackIntensityMultiplier();
                    foreach ( ISimMapActor prospect in tile.ActorsWithinMaxNPCAttackRange.GetDisplayList() )
                    {
                        if ( prospect.IsFullDead || prospect.IsInvalid || prospect == Target || prospect is ISimMachineActor || prospect is MachineStructure )
                            continue; //this prospect is completely invalid, or is the same as the target or the attacker

                        Vector3 pos = prospect.GetDrawLocation();
                        if ( (pos - targetLoc).GetSquareGroundMagnitude() > attackAreaSquared )
                            continue; //this prospective target is out of range, so skip it

                        if ( !Attacker.GetIsValidToCatchInAreaOfEffectExplosion_Current( prospect ) )
                            continue;

                        AttackAmounts newDamage = HandlePlayerRangedAttackLogic_SecondaryTarget( Attacker, prospect, RandIfNotPrediction,
                            IsAndroidLeapingFromBeyondAttackRange, intensityMultiplier );
                        totalDamageDealt.Add( newDamage );
                        //ArcenDebugging.LogSingleLine( "secondary: " + prospect.GetDisplayName() + " mult: " + intensityMultiplier + 
                        //    " ExtraAttackMultiplier: " + ExtraAttackMultiplier + " new: " + newDamage + " vs: " + originalDamage, Verbosity.DoNotShow );
                    }
                }
            }

            return totalDamageDealt;
        }
        #endregion

        #region HandlePlayerRangedAttackLogic_SecondaryTarget
        private static AttackAmounts HandlePlayerRangedAttackLogic_SecondaryTarget( ISimMapActor Attacker, ISimMapActor Target,
            MersenneTwister RandIfNotPrediction, bool IsAndroidLeapingFromBeyondAttackRange, float IntensityFromAOE )
        {
            return HandleGeneralAttackLogic_Inner( Attacker, Target, CalculationType.ForExecution,
                RandIfNotPrediction, IsAndroidLeapingFromBeyondAttackRange, IntensityFromAOE,
                false, //hit even cloaked things
                true, false, false, Vector3.zero,
                0, false, AttackAmounts.Zero(), 
                true, //skip range checks, because these are in the AOE range
                null, null );
        }
        #endregion

        private static ArcenDoubleCharacterBuffer physicalBuffer = new ArcenDoubleCharacterBuffer( "AttackHelper-physicalBuffer" );
        private static ArcenDoubleCharacterBuffer fearBuffer = new ArcenDoubleCharacterBuffer( "AttackHelper-fearBuffer" );
        private static ArcenDoubleCharacterBuffer argumentBuffer = new ArcenDoubleCharacterBuffer( "AttackHelper-argumentBuffer" );

        #region HandleGeneralAttackLogic_Inner
        private static AttackAmounts HandleGeneralAttackLogic_Inner( ISimMapActor Attacker, ISimMapActor Target, CalculationType CalcType, 
            MersenneTwister RandIfNotPrediction, bool IsAndroidLeapingFromBeyondAttackRange, float IntensityFromAOE, bool CheckCloakedStatus, bool CheckTakeCoverStatus, bool ImagineWillBeInCover,
            bool ImagineAttackerWillHaveMoved, Vector3 NewAttackerLocation,
            int ImagineThisAmountOfAttackerHealthWasLost, bool DoFullPrecalculation, AttackAmounts OverridingDamageAmounts, bool SkipCaringAboutRange, 
            ArcenCharacterBufferBase BufferOrNull, ArcenCharacterBufferBase SecondaryBufferOrNull )
        {
            if ( !SkipCaringAboutRange )
            {
                if ( CalcType == CalculationType.ForExecution && !GetTargetIsInRange( Attacker, Target ) )
                {
                    //if ( GetTargetVisible( Attacker, Target ) )
                    //{
                    //    if ( Attacker is ISimNPCUnit ) //only complain if it's an npc doing it.  If it's the player, then it was them clicking many times.
                    //        ArcenDebugging.LogWithStack( "Tried to attack out of range! " + Attacker.GetDisplayName() + " vs " + Target.GetDisplayName(), Verbosity.ShowAsError );
                    //}
                    return AttackAmounts.Zero(); //do not allow accidental shooting from out of range!
                }
            }

            bool attackerIsPlayer = Attacker is ISimMachineActor;

            bool isPrediction = CalcType.IsPrediction();

            if ( BufferOrNull != null )
            {
                physicalBuffer.EnsureResetForNextUpdate();
                fearBuffer.EnsureResetForNextUpdate();
                argumentBuffer.EnsureResetForNextUpdate();
            }

            int physicalDamage = 0;
            int fearDamage = 0;
            int argumentDamage = 0;
            int totalMoraleDamage = 0;

            bool targetCanHaveMoraleDamage = Target.GetCanTakeMoraleDamage();

            bool reducedByArmor = false;
            if ( OverridingDamageAmounts.IsEmpty() )
            {
                int attackerPhysicalPower;
                int attackerFearAttackPower;
                int attackerArgumentAttackPower;

                #region Get Initial Power Levels
                {
                    if ( ImagineThisAmountOfAttackerHealthWasLost > 0 && isPrediction && Attacker is ISimNPCUnit npcAttacker )
                    {
                        attackerPhysicalPower = npcAttacker.CalculateTheoreticalStatStrengthIfWeImagineThisAmountOfLostHealth( ActorRefs.ActorPower, ImagineThisAmountOfAttackerHealthWasLost );
                        if ( targetCanHaveMoraleDamage )
                        {
                            attackerFearAttackPower = npcAttacker.CalculateTheoreticalStatStrengthIfWeImagineThisAmountOfLostHealth( ActorRefs.ActorFearAttackPower, ImagineThisAmountOfAttackerHealthWasLost );
                            attackerArgumentAttackPower = npcAttacker.CalculateTheoreticalStatStrengthIfWeImagineThisAmountOfLostHealth( ActorRefs.ActorArgumentAttackPower, ImagineThisAmountOfAttackerHealthWasLost );
                        }
                        else
                        {
                            attackerFearAttackPower = 0;
                            attackerArgumentAttackPower = 0;
                        }
                    }
                    else
                    {
                        attackerPhysicalPower = Attacker.GetActorDataCurrent( ActorRefs.ActorPower, true );
                        if ( targetCanHaveMoraleDamage )
                        {
                            attackerFearAttackPower = Attacker.GetActorDataCurrent( ActorRefs.ActorFearAttackPower, true );
                            attackerArgumentAttackPower = Attacker.GetActorDataCurrent( ActorRefs.ActorArgumentAttackPower, true );
                        }
                        else
                        {
                            attackerFearAttackPower = 0;
                            attackerArgumentAttackPower = 0;
                        }
                    }
                }
                #endregion

                if ( BufferOrNull != null )
                {
                    if ( attackerPhysicalPower > 0 )
                        physicalBuffer.Line().AddBoldLangAndAfterLineItemHeader( "AttackPowerBase", ColorTheme.DataBlue ).AddRaw( attackerPhysicalPower.ToStringThousandsWhole() ).Line();

                    if ( attackerFearAttackPower > 0 )
                        fearBuffer.Line().AddBoldLangAndAfterLineItemHeader( "FearAttackPowerBase", ColorTheme.DataBlue ).AddRaw( attackerFearAttackPower.ToStringThousandsWhole() ).Line();

                    if ( attackerArgumentAttackPower > 0 )
                        argumentBuffer.Line().AddBoldLangAndAfterLineItemHeader( "ArgumentAttackPowerBase", ColorTheme.DataBlue ).AddRaw( attackerArgumentAttackPower.ToStringThousandsWhole() ).Line();
                }

                foreach ( DataCalculator calculator in DataCalculatorTable.DoDuringAttackPowerCalculation )
                    calculator.Implementation.DoDuringAttackPowerCalculation( calculator,
                        Attacker, Target, CalcType,
                        RandIfNotPrediction, IsAndroidLeapingFromBeyondAttackRange, IntensityFromAOE, CheckCloakedStatus, CheckTakeCoverStatus, ImagineWillBeInCover,
                        ImagineAttackerWillHaveMoved, NewAttackerLocation,
                        ImagineThisAmountOfAttackerHealthWasLost, DoFullPrecalculation, SkipCaringAboutRange,
                        BufferOrNull, SecondaryBufferOrNull,
                        //
                        ref attackerPhysicalPower, ref attackerFearAttackPower, ref attackerArgumentAttackPower );

                if ( attackerIsPlayer && Attacker is ISimMachineActor machineActor )
                {
                    AbilityType abilityMode = machineActor?.IsInAbilityTypeTargetingMode;
                    if ( abilityMode?.AttacksAreFearBased ?? false )
                    {
                        //A portion of this unit's attack power will be converted into fear attack power. The higher this unit's Intimidation, the better the conversion.
                        //The converted attack power will never be less than 10 % and never more than 100 %.It will reach 100 % conversion at 200 Intimidation.

                        int intimidation = Attacker.GetActorDataCurrent( ActorRefs.UnitIntimidation, true );
                        float intimidationPercentage = (float)intimidation / 200f;
                        intimidationPercentage = MathA.ClampFloat( intimidationPercentage, 0.1f, 1f );

                        int addedFearAttack = Mathf.CeilToInt( attackerPhysicalPower * intimidationPercentage );
                        if ( addedFearAttack > 0 )
                        {
                            attackerFearAttackPower += addedFearAttack;

                            if ( BufferOrNull != null )
                                fearBuffer.AddFormat2AndAfterLineItemHeader( "AndroidConvertedPhysicalPowerViaIntimidation", attackerPhysicalPower, intimidation, ColorTheme.DataBlue )
                                    .AddNumberPlusOrMinus( addedFearAttack > 0, addedFearAttack.ToStringThousandsWhole() ).Line();
                        }

                        physicalBuffer.Clear();
                        attackerPhysicalPower = 0;
                    }
                    else if ( abilityMode?.AttacksAreArgumentBased ?? false )
                    {
                        physicalBuffer.Clear();
                        attackerPhysicalPower = 0;

                        //This unit's full Cognition, and half of its Strength, will be converted into argument attack power.

                        int cognition = Attacker.GetActorDataCurrent( ActorRefs.UnitCognition, true );
                        int strength = Attacker.GetActorDataCurrent( ActorRefs.UnitStrength, true );
                        int halfStrength = strength / 2;

                        if ( cognition > 0 )
                        {
                            attackerArgumentAttackPower += cognition;

                            if ( BufferOrNull != null )
                                argumentBuffer.AddFormat1AndAfterLineItemHeader( "FromCognition", cognition, ColorTheme.DataBlue )
                                    .AddNumberPlusOrMinus( cognition > 0, cognition.ToStringThousandsWhole() ).Line();
                        }

                        if ( halfStrength > 0 )
                        {
                            attackerArgumentAttackPower += halfStrength;

                            if ( BufferOrNull != null )
                                argumentBuffer.AddFormat1AndAfterLineItemHeader( "FromStrength", strength, ColorTheme.DataBlue )
                                    .AddNumberPlusOrMinus( halfStrength > 0, halfStrength.ToStringThousandsWhole() ).Line();
                        }
                    }
                }

                #region IsAndroidLeapingFromBeyondAttackRange (without Superspeed)
                if ( IsAndroidLeapingFromBeyondAttackRange && !Attacker.GetHasPerk( CommonRefs.Superspeed ) )
                {
                    if ( attackerPhysicalPower > 0 )
                    {
                        int prior = attackerPhysicalPower;
                        attackerPhysicalPower = Mathf.CeilToInt( attackerPhysicalPower * 0.5f );
                        int change = attackerPhysicalPower - prior;

                        if ( BufferOrNull != null && change != 0 )
                            physicalBuffer.AddLangAndAfterLineItemHeader( "AndroidLeapingFromBeyondAttackRange", ColorTheme.DataBlue ).AddNumberPlusOrMinus( change > 0, change.ToStringThousandsWhole() ).Line();
                    }

                    if ( attackerFearAttackPower > 0 )
                    {
                        int prior = attackerFearAttackPower;
                        attackerFearAttackPower = Mathf.CeilToInt( attackerFearAttackPower * 0.5f );
                        int change = attackerFearAttackPower - prior;

                        if ( BufferOrNull != null && change != 0 )
                            fearBuffer.AddLangAndAfterLineItemHeader( "AndroidLeapingFromBeyondAttackRange", ColorTheme.DataBlue ).AddNumberPlusOrMinus( change > 0, change.ToStringThousandsWhole() ).Line();
                    }

                    if ( attackerArgumentAttackPower > 0 )
                    {
                        int prior = attackerArgumentAttackPower;
                        attackerArgumentAttackPower = Mathf.CeilToInt( attackerArgumentAttackPower * 0.5f );
                        int change = attackerArgumentAttackPower - prior;

                        if ( BufferOrNull != null && change != 0 )
                            argumentBuffer.AddLangAndAfterLineItemHeader( "AndroidLeapingFromBeyondAttackRange", ColorTheme.DataBlue ).AddNumberPlusOrMinus( change > 0, change.ToStringThousandsWhole() ).Line();
                    }
                }
                #endregion

                #region IntensityFromAOE
                if ( IntensityFromAOE > 0f && IntensityFromAOE < 1f )
                {
                    if ( attackerPhysicalPower > 0 )
                    {
                        int prior = attackerPhysicalPower;
                        attackerPhysicalPower = Mathf.CeilToInt( attackerPhysicalPower * IntensityFromAOE );
                        int change = attackerPhysicalPower - prior;

                        if ( BufferOrNull != null && change != 0 )
                            physicalBuffer.AddLangAndAfterLineItemHeader( "CaughtInAreaOfEffect", ColorTheme.DataBlue ).AddNumberPlusOrMinus( change > 0, change.ToStringThousandsWhole() ).Line();
                    }

                    if ( attackerFearAttackPower > 0 )
                    {
                        int prior = attackerFearAttackPower;
                        attackerFearAttackPower = Mathf.CeilToInt( attackerFearAttackPower * IntensityFromAOE );
                        int change = attackerFearAttackPower - prior;

                        if ( BufferOrNull != null && change != 0 )
                            fearBuffer.AddLangAndAfterLineItemHeader( "CaughtInAreaOfEffect", ColorTheme.DataBlue ).AddNumberPlusOrMinus( change > 0, change.ToStringThousandsWhole() ).Line();
                    }

                    if ( attackerArgumentAttackPower > 0 )
                    {
                        int prior = attackerArgumentAttackPower;
                        attackerArgumentAttackPower = Mathf.CeilToInt( attackerArgumentAttackPower * IntensityFromAOE );
                        int change = attackerArgumentAttackPower - prior;

                        if ( BufferOrNull != null && change != 0 )
                            argumentBuffer.AddLangAndAfterLineItemHeader( "CaughtInAreaOfEffect", ColorTheme.DataBlue ).AddNumberPlusOrMinus( change > 0, change.ToStringThousandsWhole() ).Line();
                    }
                }
                #endregion

                bool isAnyKindOfPrediction = DoFullPrecalculation || isPrediction;

                #region Attacker Feats
                FeatSetForType attackerFeats = Attacker.GetFeatSetOrNull();
                if ( attackerFeats != null )
                {
                    if ( attackerFeats.HasCodeWhenDealingDamage.Count > 0 )
                    {
                        foreach ( KeyValuePair<ActorFeat, float> featKV in attackerFeats.HasCodeWhenDealingDamage.GetDisplayDict() )
                        {
                            if ( featKV.Value > 0 )
                            {
                                featKV.Key.Implementation.DoWhenDealingDamage( featKV.Key, featKV.Value, Attacker, Target,
                                    ref attackerPhysicalPower, ref attackerFearAttackPower, ref attackerArgumentAttackPower,
                                    BufferOrNull == null ? null : physicalBuffer, BufferOrNull == null ? null : fearBuffer, BufferOrNull == null ? null : argumentBuffer,
                                    SecondaryBufferOrNull, isAnyKindOfPrediction, out bool StopTheAttack, RandIfNotPrediction );
                                if ( StopTheAttack )
                                    return AttackAmounts.Zero();
                            }
                        }
                    }
                }
                #endregion

                #region Defender Feats
                FeatSetForType defenderFeats = Target.GetFeatSetOrNull();
                if ( defenderFeats != null )
                {
                    if ( defenderFeats.HasCodeWhenTakingDamage.Count > 0 )
                    {
                        foreach ( KeyValuePair<ActorFeat, float> featKV in attackerFeats.HasCodeWhenTakingDamage.GetDisplayDict() )
                        {
                            if ( featKV.Value > 0 )
                            {
                                featKV.Key.Implementation.DoWhenTakingDamage( featKV.Key, featKV.Value, Attacker, Target,
                                    ref attackerPhysicalPower, ref attackerFearAttackPower, ref attackerArgumentAttackPower,
                                    BufferOrNull == null ? null : physicalBuffer, BufferOrNull == null ? null : fearBuffer, BufferOrNull == null ? null : argumentBuffer,
                                    SecondaryBufferOrNull, isAnyKindOfPrediction, out bool StopTheAttack, RandIfNotPrediction );
                                if ( StopTheAttack )
                                    return AttackAmounts.Zero();
                            }
                        }
                    }
                }
                #endregion

                #region obscuredByMicrobuilders
                int obscuredByMicrobuilders = Target.GetStatusIntensity( StatusRefs.ObscuredByMicrobuilderCloud );
                if ( obscuredByMicrobuilders > 0 && attackerPhysicalPower > 0 )
                {
                    if ( obscuredByMicrobuilders > 80 )
                        obscuredByMicrobuilders = 80;

                    obscuredByMicrobuilders = 100 - obscuredByMicrobuilders;

                    float multiplier = (float)obscuredByMicrobuilders / 100f;

                    if ( multiplier < 1f && multiplier > 0f )
                    {
                        int prior = attackerPhysicalPower;
                        attackerPhysicalPower = Mathf.CeilToInt( attackerPhysicalPower * multiplier );
                        int change = attackerPhysicalPower - prior;
                        if ( BufferOrNull != null && change != 0 )
                            physicalBuffer.AddLangAndAfterLineItemHeader( "TargetObscured", ColorTheme.DataBlue ).AddNumberPlusOrMinus( change > 0, change.ToStringThousandsWhole() ).Line();
                    }
                }
                #endregion

                if ( CheckCloakedStatus )
                {
                    if ( Target.IsCloaked ) //helps against physical attacks only
                    {
                        int prior = attackerPhysicalPower;
                        attackerPhysicalPower = 0;
                        int change = attackerPhysicalPower - prior;
                        if ( BufferOrNull != null && change != 0 )
                            physicalBuffer.AddLangAndAfterLineItemHeader( "TargetInFullStealth", ColorTheme.DataBlue ).AddNumberPlusOrMinus( change > 0, change.ToStringThousandsWhole() ).Line();
                    }
                }

                if ( CheckTakeCoverStatus )  //helps against physical attacks only
                {
                    if ( Target.IsTakingCover || ImagineWillBeInCover )
                    {
                        float multiplier = isPrediction ? 0.4f : RandIfNotPrediction.NextFloat( 0.5f, 0.3f );
                        if ( multiplier < 1f && multiplier > 0f )
                        {
                            int prior = attackerPhysicalPower;
                            attackerPhysicalPower = Mathf.CeilToInt( attackerPhysicalPower * multiplier );
                            int change = attackerPhysicalPower - prior;
                            if ( BufferOrNull != null && change != 0 )
                                physicalBuffer.AddLangAndAfterLineItemHeader( "TargetInCover", ColorTheme.DataBlue ).AddNumberPlusOrMinus( change > 0, change.ToStringThousandsWhole() ).Line();
                        }
                    }
                }

                #region Fearless And Set Final Fear Damage
                if ( attackerFearAttackPower > 0 )
                {
                    if ( Target.GetHasPerk( ActorRefs.Fearless ) )
                    {
                        int prior = attackerFearAttackPower;
                        attackerFearAttackPower = 0;
                        int change = attackerFearAttackPower - prior;
                        if ( BufferOrNull != null && change != 0 )
                            fearBuffer.AddLangAndAfterLineItemHeader( "TargetIsFearless", ColorTheme.DataBlue ).AddNumberPlusOrMinus( change > 0, change.ToStringThousandsWhole() ).Line();
                    }

                    //we are done, as far as fear damage goes
                    if ( attackerFearAttackPower > 0 )
                        fearDamage = attackerFearAttackPower;
                }
                #endregion

                #region Stubborn And Set Final Argument Damage
                if ( attackerArgumentAttackPower > 0 )
                {
                    if ( Target.GetHasPerk( ActorRefs.Stubborn ) )
                    {
                        int prior = attackerArgumentAttackPower;
                        attackerArgumentAttackPower = 0;
                        int change = attackerArgumentAttackPower - prior;
                        if ( BufferOrNull != null && change != 0 )
                            argumentBuffer.AddLangAndAfterLineItemHeader( "TargetIsStubborn", ColorTheme.DataBlue ).AddNumberPlusOrMinus( change > 0, change.ToStringThousandsWhole() ).Line();
                    }

                    //we are done, as far as argument damage goes
                    if ( attackerArgumentAttackPower > 0 )
                        argumentDamage = attackerArgumentAttackPower;
                }
                #endregion

                totalMoraleDamage = fearDamage + argumentDamage;

                int minPhysicalDamage = MathA.Max( attackerIsPlayer && attackerPhysicalPower == 0 ? 0 : 1, Mathf.CeilToInt( (float)attackerPhysicalPower / 30f ) );

                if ( attackerPhysicalPower <= 1 && !attackerIsPlayer && ( fearDamage > 0 || argumentDamage > 0 ) )
                {
                    attackerPhysicalPower = 0;
                    minPhysicalDamage = 0;
                }

                physicalDamage = attackerPhysicalPower;
                if ( physicalDamage < minPhysicalDamage )
                    physicalDamage = minPhysicalDamage;

                bool hitsAllSpatialWeaknesses = Attacker.GetHasPerk( CommonRefs.Achilles );

                #region Hit From Above
                //now handle damage multipliers from above or behind, after the min damage has been applied
                int extraVulnerabilityFromAbove = Target.GetActorDataCurrent( ActorRefs.ActorExtraVulnerabilityFromAbove, true );
                if ( extraVulnerabilityFromAbove > 100 )
                {
                    float amountAbove = (ImagineAttackerWillHaveMoved ? Attacker.GetPositionForCollisionsFromTheoretic( NewAttackerLocation ).y : Attacker.GetPositionForCollisions().y)
                        - Target.GetPositionForCollisions().y;

                    //if ( BufferOrNull != null )
                    //    physicalBuffer.AddNeverTranslated( "amountAbove", ColorTheme.DataBlue ).AddRaw( amountAbove.ToStringThousandsDecimal_Optional4() ).Line();
                    //DrawHelper.RenderCube( Attacker.GetPositionForCollisions(), 0.1f, ColorMath.LeafGreen, 1f );
                    //DrawHelper.RenderCube( Target.GetPositionForCollisions(), 0.1f, ColorMath.LeafGreen, 1f );

                    float multiplier = (float)extraVulnerabilityFromAbove / 100f;

                    if ( multiplier > 1f )
                    {
                        bool hitIt = false;
                        if ( amountAbove > 0 || hitsAllSpatialWeaknesses )
                        {
                            hitIt = true;
                            int prior = physicalDamage;
                            physicalDamage = Mathf.CeilToInt( physicalDamage * multiplier );
                            int change = physicalDamage - prior;
                            if ( BufferOrNull != null && change != 0 )
                                physicalBuffer.AddLangAndAfterLineItemHeader( "AboveTargetWithExtraWeaknessToAttacksFromAbove", ColorTheme.DataBlue ).AddNumberPlusOrMinus( change > 0, change.ToStringThousandsWhole() ).Line();

                            if ( SecondaryBufferOrNull != null && !InputCaching.ShouldShowDetailedTooltips )
                                SecondaryBufferOrNull.AddSpriteStyled_NoIndent( ActorRefs.ActorExtraVulnerabilityFromAbove.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.BonusDamageHit ).Space1x()
                                    .StartSize80().AddFormat1( "FromAboveBonusHit", multiplier.ToStringThousandsDecimal_Optional4(), ColorTheme.BonusDamageHit ).EndSize().Line();
                        }
                        else
                        {
                            if ( SecondaryBufferOrNull != null && !InputCaching.ShouldShowDetailedTooltips )
                                SecondaryBufferOrNull.AddSpriteStyled_NoIndent( ActorRefs.ActorExtraVulnerabilityFromAbove.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.BonusDamageMissed ).Space1x()
                                    .StartSize80().AddFormat1( "FromAboveBonusMissed", multiplier.ToStringThousandsDecimal_Optional4(), ColorTheme.BonusDamageMissed ).EndSize().Line();
                        }

                        if ( attackerIsPlayer && !isPrediction )
                            HandbookRefs.SomeEnemiesAreWeakFromAbove.DuringGame_UnlockIfNeeded( !hitIt );
                    }
                }
                #endregion

                #region Hit From Rear
                int extraVulnerabilityFromRear = Target.GetActorDataCurrent( ActorRefs.ActorExtraVulnerabilityFromRear, true );
                if ( extraVulnerabilityFromRear > 100 )
                {
                    float multiplier = (float)extraVulnerabilityFromRear / 100f;
                    if ( multiplier > 1f )
                    {
                        float angleBetweenTargets = MathA.AngleXZInDegrees(
                            ImagineAttackerWillHaveMoved ? Attacker.GetPositionForCollisionsFromTheoretic( NewAttackerLocation ) : Attacker.GetPositionForCollisions(),
                            Target.GetPositionForCollisions() );
                        angleBetweenTargets += 90f; //this is required in order to make it be in the same rotation axis as units on the map

                        while ( angleBetweenTargets > 360 )
                            angleBetweenTargets -= 360;
                        while ( angleBetweenTargets < 0 )
                            angleBetweenTargets += 360;

                        float angleOfTarget = Target.GetRotationYForCollisions();
                        while ( angleOfTarget > 360 )
                            angleOfTarget -= 360;
                        while ( angleOfTarget < 0 )
                            angleOfTarget += 360;

                        float angleDifference1 = angleBetweenTargets - angleOfTarget;
                        if ( angleDifference1 < 0 )
                            angleDifference1 = -angleDifference1;

                        float angleDifference2 = angleBetweenTargets - (angleOfTarget + 360);
                        if ( angleDifference2 < 0 )
                            angleDifference2 = -angleDifference2;

                        float angleDifference3 = (angleBetweenTargets + 360) - angleOfTarget;
                        if ( angleDifference3 < 0 )
                            angleDifference3 = -angleDifference2;

                        float angleDifference = MathA.Min( angleDifference1, angleDifference2, angleDifference3 );

                        //if ( BufferOrNull != null )
                        //{
                        //    physicalBuffer.AddNeverTranslated( "angleBetweenTargets: ", ColorTheme.DataBlue ).AddRaw( angleBetweenTargets.ToStringThousandsDecimal_Optional4() ).Line();
                        //    physicalBuffer.AddNeverTranslated( "angleOfTarget: ", ColorTheme.DataBlue ).AddRaw( angleOfTarget.ToStringThousandsDecimal_Optional4() ).Line();
                        //    physicalBuffer.AddNeverTranslated( "angleDifference: ", ColorTheme.DataBlue ).AddRaw( angleDifference.ToStringThousandsDecimal_Optional4() ).Line();
                        //}

                        bool hitIt = false;
                        if ( angleDifference < 90 || hitsAllSpatialWeaknesses )
                        {
                            hitIt = true;
                            int prior = physicalDamage;
                            physicalDamage = Mathf.CeilToInt( physicalDamage * multiplier );
                            int change = physicalDamage - prior;
                            if ( BufferOrNull != null && change != 0 )
                                physicalBuffer.AddLangAndAfterLineItemHeader( "BehindTargetWithExtraWeaknessToAttacksFromRear", ColorTheme.DataBlue ).AddNumberPlusOrMinus( change > 0, change.ToStringThousandsWhole() ).Line();

                            if ( SecondaryBufferOrNull != null && !InputCaching.ShouldShowDetailedTooltips )
                                SecondaryBufferOrNull.AddSpriteStyled_NoIndent( ActorRefs.ActorExtraVulnerabilityFromRear.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.BonusDamageHit ).Space1x()
                                    .StartSize80().AddFormat1( "FromBehindBonusHit", multiplier.ToStringThousandsDecimal_Optional4(), ColorTheme.BonusDamageHit ).EndSize().Line();
                        }
                        else
                        {
                            if ( SecondaryBufferOrNull != null && !InputCaching.ShouldShowDetailedTooltips )
                                SecondaryBufferOrNull.AddSpriteStyled_NoIndent( ActorRefs.ActorExtraVulnerabilityFromRear.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.BonusDamageMissed ).Space1x()
                                    .StartSize80().AddFormat1( "FromBehindBonusMissed", multiplier.ToStringThousandsDecimal_Optional4(), ColorTheme.BonusDamageMissed ).EndSize().Line();
                        }

                        if ( attackerIsPlayer && !isPrediction )
                            HandbookRefs.SomeEnemiesAreWeakFromBehind.DuringGame_UnlockIfNeeded( !hitIt );
                    }
                }
                #endregion

                //now handle armor, after the bonuses and min damage

                #region Physical Armor
                int armor = Target.GetActorDataCurrent( ActorRefs.ActorArmorPlating, true );
                if ( armor > 0 )
                {
                    int piercing = Attacker.GetActorDataCurrent( ActorRefs.ActorArmorPiercing, true );
                    if ( piercing > 0 )
                        armor -= piercing;

                    if ( attackerIsPlayer && !isPrediction )
                        HandbookRefs.SomeHaveArmorPlating.DuringGame_UnlockIfNeeded( true );

                    if ( armor <= 0 )
                    {
                        if ( BufferOrNull != null )
                            physicalBuffer.AddLang( "TargetArmorNegated", ColorTheme.DataBlue ).Line();
                    }
                    else
                    {
                        reducedByArmor = true;
                        if ( BufferOrNull != null )
                            physicalBuffer.AddLangAndAfterLineItemHeader( "EffectiveArmor", ColorTheme.DataBlue )
                                .AddRaw( armor.ToStringThousandsWhole() ).Line();

                        int intPercentage = MathA.ClampInt( armor, 0, 100 );

                        float percentage = 1f - ((float)intPercentage / 100f);
                        if ( percentage < 0 )
                            percentage = 0;
                        if ( percentage > 1f )
                            percentage = 1f;

                        if ( SecondaryBufferOrNull != null && !InputCaching.ShouldShowDetailedTooltips )
                            SecondaryBufferOrNull.AddSpriteStyled_NoIndent( ActorRefs.ActorArmorPlating.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange2 ).Space1x()
                                .StartSize80().AddFormat1( "PercentOfDamageBlockedByArmorPlating_Brief", intPercentage.ToStringWholeBasic(), ColorTheme.RedOrange2 ).EndSize().Line();

                        int prior = physicalDamage;
                        physicalDamage = Mathf.CeilToInt( physicalDamage * percentage );
                        if ( physicalDamage > prior )
                            physicalDamage = prior;

                        int change = physicalDamage - prior;
                        if ( BufferOrNull != null && change != 0 )
                            physicalBuffer.AddLangAndAfterLineItemHeader( "DamageReductionFromArmor", ColorTheme.DataBlue ).AddNumberPlusOrMinus( change > 0, change.ToStringThousandsWhole() ).Line();
                    }
                }
                #endregion
                
                if ( physicalDamage > 0 )
                {
                    int damageReduction = Target.GetStatusIntensity( StatusRefs.NetworkShield );
                    if ( damageReduction > 0 )
                    {
                        //todo later, the damage prediction details. But this only applies to allies, so probably never seen in the game.  Hence skipping for now.

                        int prior = physicalDamage;
                        physicalDamage -= damageReduction;
                        if ( physicalDamage < 0 )
                            physicalDamage = 0;
                    }
                }
            }
            else
            {
                physicalDamage = OverridingDamageAmounts.Physical;
                totalMoraleDamage = OverridingDamageAmounts.Morale;
            }

            //at this point, physicalDamage and totalMoraleDamage are fully calculated, minus any last randomization
            //************************************************************************

            if ( isPrediction )
            {
                //if ( maxDamageFromResilient > -1 && maxDamageFromResilient < damage )
                //{
                //    int prior = damage;
                //    damage = maxDamageFromResilient;
                //    int change = damage - prior;
                //    if ( BufferOrNull != null && change != 0 )
                //        BufferOrNull.AddLangAndAfterLineItemHeader( "ResilientTarget", ColorTheme.DataBlue ).AddNumberPlusOrMinus( change > 0, change.ToStringThousandsWhole() ).Line();
                //}
            }
            else
            {
                if ( !OverridingDamageAmounts.IsEmpty() )
                {
                    //this seems like a duplicate, but that's fine
                    physicalDamage = OverridingDamageAmounts.Physical;
                    totalMoraleDamage = OverridingDamageAmounts.Morale;
                }
                else
                {
                    #region Extra Final Randomized Bits
                    if ( physicalDamage > 0 )
                    {
                        if ( attackerIsPlayer )
                            physicalDamage = Mathf.CeilToInt( physicalDamage * MathRefs.PlayerAttackDamageRange.Bag.PickRandom( RandIfNotPrediction ) );
                        else
                            physicalDamage = Mathf.CeilToInt( physicalDamage * MathRefs.NPCAttackDamageRange.Bag.PickRandom( RandIfNotPrediction ) );
                    }

                    if ( totalMoraleDamage > 0 )
                    {
                        if ( attackerIsPlayer )
                            totalMoraleDamage = Mathf.CeilToInt( totalMoraleDamage * MathRefs.PlayerAttackDamageRange.Bag.PickRandom( RandIfNotPrediction ) );
                        else
                            totalMoraleDamage = Mathf.CeilToInt( totalMoraleDamage * MathRefs.NPCAttackDamageRange.Bag.PickRandom( RandIfNotPrediction ) );
                    }
                    #endregion
                }

                //if ( maxDamageFromResilient > -1 && maxDamageFromResilient < damage )
                //    damage = maxDamageFromResilient;

                //this is not a prediction, but it IS a precalculation.  Guess we are not writing to the main buffer
                if ( DoFullPrecalculation )
                {
                    AttackAmounts damage;
                    damage.Physical = physicalDamage;
                    damage.Morale = totalMoraleDamage;
                    return damage; //don't actually apply it
                }

                bool shouldDoDamageTextPopupsAndLogging = true;
                {
                    if ( Attacker.GetIsVeryLowPriorityForLog() && Target.GetIsVeryLowPriorityForLog() )
                        shouldDoDamageTextPopupsAndLogging = false;

                    if ( shouldDoDamageTextPopupsAndLogging )
                    {
                        if ( Attacker is ISimNPCUnit npcA && npcA.IsNPCInFogOfWar && Target is ISimNPCUnit npcB && npcB.IsNPCInFogOfWar )
                            shouldDoDamageTextPopupsAndLogging = false;
                    }
                }

                ArcenDoubleCharacterBuffer popupBuffer = shouldDoDamageTextPopupsAndLogging ? DamageTextPopups.GetTextBuffer() : null;
                MapActorData moraleOrNull = totalMoraleDamage > 0 ? Target?.GetActorDataData( ActorRefs.UnitMorale, true ) : null;

                bool willFleeFromMoraleLoss = false;
                if ( totalMoraleDamage > 0 && moraleOrNull != null && totalMoraleDamage > moraleOrNull.Current )
                    willFleeFromMoraleLoss = true;

                int squadMembersLost = 0;

                bool didPopup = false;
                if ( physicalDamage > 0 && !willFleeFromMoraleLoss ) //we only care about physical damage if this unit is not already fleeing
                {
                    didPopup = true;
                    ApplyFinalPhysicalDamageAndStatsAndPopup( Attacker, Target, shouldDoDamageTextPopupsAndLogging, physicalDamage, totalMoraleDamage, reducedByArmor, popupBuffer, ref squadMembersLost );
                }

                bool wasDead = Target.IsFullDead;

                if ( totalMoraleDamage > 0 && moraleOrNull != null )
                {
                    ApplyFinalMoraleDamageAndStatsAndPopup( Attacker, Target, shouldDoDamageTextPopupsAndLogging && !didPopup, shouldDoDamageTextPopupsAndLogging, 
                        totalMoraleDamage, moraleOrNull, popupBuffer );
                }

                if ( physicalDamage > 0 || totalMoraleDamage > 0 )
                {
                    //we also want this to be after the attack notes above, so that if units die, it shows up after they take damage
                    Target.DoOnPostTakeDamage( Attacker, physicalDamage, totalMoraleDamage, squadMembersLost, RandIfNotPrediction, shouldDoDamageTextPopupsAndLogging ); //we want this to be after CurrentSquadSize goes down, just in case

                    if ( !wasDead && Target.IsFullDead )
                    {
                        MapActorData moraleData = Target.GetActorDataData( ActorRefs.UnitMorale, true );
                        bool ranOutOfMorale = moraleData != null && moraleData.Maximum > 0 && moraleData.Current <= 0;

                        #region Attacker Killing Feats
                        FeatSetForType attackerFeats = Attacker.GetFeatSetOrNull();
                        if ( attackerFeats != null )
                        {
                            if ( attackerFeats.HasCodeWhenKilling.Count > 0 )
                            {
                                foreach ( KeyValuePair<ActorFeat, float> featKV in attackerFeats.HasCodeWhenKilling.GetDisplayDict() )
                                {
                                    if ( featKV.Value > 0 )
                                    {
                                        featKV.Key.Implementation.DoWhenKilling( featKV.Key, featKV.Value, Attacker, Target,
                                            physicalDamage, fearDamage, argumentDamage, !ranOutOfMorale, RandIfNotPrediction );
                                    }
                                }
                            }
                        }
                        #endregion

                        #region Defender Dying Feats
                        FeatSetForType defenderFeats = Target.GetFeatSetOrNull();
                        if ( defenderFeats != null )
                        {
                            if ( defenderFeats.HasCodeWhenDying.Count > 0 )
                            {
                                foreach ( KeyValuePair<ActorFeat, float> featKV in attackerFeats.HasCodeWhenDying.GetDisplayDict() )
                                {
                                    if ( featKV.Value > 0 )
                                    {
                                        featKV.Key.Implementation.DoWhenKilling( featKV.Key, featKV.Value, Attacker, Target,
                                            physicalDamage, fearDamage, argumentDamage, !ranOutOfMorale, RandIfNotPrediction );
                                    }
                                }
                            }
                        }
                        #endregion
                    }
                }

            } //endif !isPrediction

            if ( BufferOrNull != null )
            {
                bool didAnyPrior = false;
                if ( physicalDamage > 0 )
                {
                    physicalBuffer.AddBoldLangAndAfterLineItemHeader( "AttackPowerFinal", ColorTheme.DataBlue ).AddRaw( physicalDamage.ToStringThousandsWhole() ).Line();

                    MapActorData targetHealth = Target?.GetActorDataData( ActorRefs.ActorHP, true );
                    if ( targetHealth != null )
                        physicalBuffer.AddBoldLangAndAfterLineItemHeader( "CurrentTargetHP", ColorTheme.HeaderGoldOrangeDark ).AddFormat2( "OutOF", targetHealth.Current.ToStringThousandsWhole(),
                            targetHealth.Maximum.ToStringThousandsWhole() );

                    BufferOrNull.AddRaw( physicalBuffer.GetStringAndResetForNextUpdate() );
                    didAnyPrior = true;
                }

                if ( fearDamage > 0 )
                {
                    fearBuffer.AddBoldLangAndAfterLineItemHeader( "FearAttackPowerFinal", ColorTheme.DataBlue ).AddRaw( fearDamage.ToStringThousandsWhole() ).Line();
                    if ( didAnyPrior )
                        BufferOrNull.Line();
                    BufferOrNull.AddRaw( fearBuffer.GetStringAndResetForNextUpdate() );
                    didAnyPrior = true;
                }

                if ( argumentDamage > 0 )
                {
                    argumentBuffer.AddBoldLangAndAfterLineItemHeader( "ArgumentAttackPowerFinal", ColorTheme.DataBlue ).AddRaw( argumentDamage.ToStringThousandsWhole() ).Line();
                    if ( didAnyPrior )
                        BufferOrNull.Line();
                    BufferOrNull.AddRaw( argumentBuffer.GetStringAndResetForNextUpdate() );
                    didAnyPrior = true;
                }

                if ( totalMoraleDamage > 0 )
                {
                    if ( didAnyPrior )
                        BufferOrNull.Line();

                    BufferOrNull.AddBoldLangAndAfterLineItemHeader( "FinalMoraleDamage", ColorTheme.DataBlue ).AddRaw( totalMoraleDamage.ToStringThousandsWhole() ).Line();

                    MapActorData morale = Target?.GetActorDataData( ActorRefs.UnitMorale, true );
                    if ( morale != null )
                        BufferOrNull.AddBoldLangAndAfterLineItemHeader( "CurrentTargetMorale", ColorTheme.HeaderGoldOrangeDark ).AddFormat2( "OutOF", morale.Current.ToStringThousandsWhole(),
                            morale.Maximum.ToStringThousandsWhole() ).Line();
                }
            }

            {
                AttackAmounts damage;
                damage.Physical = physicalDamage;
                damage.Morale = totalMoraleDamage;
                return damage;
            }
        }
        #endregion

        #region InstantlyKill
        public static void InstantlyKill( ISimMapActor Attacker, ISimMapActor Target, MersenneTwister Rand )
        {
            if ( Attacker == null || Target == null )
                return;

            bool shouldDoDamageTextPopupsAndLogging = true;
            {
                if ( Attacker.GetIsVeryLowPriorityForLog() && Target.GetIsVeryLowPriorityForLog() )
                    shouldDoDamageTextPopupsAndLogging = false;

                if ( shouldDoDamageTextPopupsAndLogging )
                {
                    if ( Attacker is ISimNPCUnit npcA && npcA.IsNPCInFogOfWar && Target is ISimNPCUnit npcB && npcB.IsNPCInFogOfWar )
                        shouldDoDamageTextPopupsAndLogging = false;
                }
            }

            ArcenDoubleCharacterBuffer popupBuffer = shouldDoDamageTextPopupsAndLogging ? DamageTextPopups.GetTextBuffer() : null;

            int physicalDamageToDo = Target.GetActorDataCurrent( ActorRefs.ActorHP, true ) + 1;
            int squadMembersLost = 0;

            ApplyFinalPhysicalDamageAndStatsAndPopup( Attacker, Target, shouldDoDamageTextPopupsAndLogging, physicalDamageToDo, 0, false, popupBuffer, ref squadMembersLost );

            Target.DoDeathCheck( Rand, true );
        }
        #endregion

        #region ApplyFinalPhysicalDamageAndStatsAndPopup
        private static void ApplyFinalPhysicalDamageAndStatsAndPopup( ISimMapActor Attacker, ISimMapActor Target, 
            bool shouldDoDamageTextPopupsAndLogging, int physicalDamage, int moraleDamage, bool reducedByArmor, ArcenDoubleCharacterBuffer popupBuffer, ref int squadMembersLost )
        {
            int debugStage = 0;
            try
            {
                debugStage = 100;

                DamageTextPopups.FloatingDamageTextPopup oldDamageText = null;
                if ( shouldDoDamageTextPopupsAndLogging )
                {
                    debugStage = 500;

                    oldDamageText = Target.MostRecentDamageText;
                    if ( oldDamageText != null && oldDamageText.GetTimeHasExisted() > 1f )
                        oldDamageText = null; //if it's been more than 1 second, don't combine them
                }

                debugStage = 1100;

                squadMembersLost = 0;
                ISimNPCUnit targetNPC = Target as ISimNPCUnit;
                if ( targetNPC != null )
                    squadMembersLost = targetNPC.CalculateSquadSizeLossFromDamage( physicalDamage ); //this must be calculated before damage is dealt, or confusing things happen

                debugStage = 2100;

                Target.AlterActorDataCurrent( ActorRefs.ActorHP, -physicalDamage, false );

                debugStage = 3100;

                Vector3 startLocation = Target.GetPositionForCollisions();
                Vector3 endLocation = startLocation.PlusY( Target.GetHalfHeightForCollisions() + 0.2f );

                int drawnPhysicalDamage = physicalDamage + (oldDamageText?.PhysicalDamageIncluded ?? 0);
                int drawnMoraleDamage = moraleDamage + (oldDamageText?.MoraleDamageIncluded ?? 0);
                int drawnSquadMembersLost = squadMembersLost + (oldDamageText?.SquadDeathsIncluded ?? 0);

                debugStage = 4100;

                if ( shouldDoDamageTextPopupsAndLogging && popupBuffer != null )
                {
                    debugStage = 6100;

                    if ( oldDamageText != null )
                        oldDamageText.MarkMeAsExpiredNow();

                    if ( reducedByArmor )
                        popupBuffer.AddLang( "PopupHitWasReducedByArmor", ColorTheme.RedOrange2 ).Space1x();

                    popupBuffer.AddSpriteStyled_NoIndent( IconRefs.DamageAmount.Icon, AdjustedSpriteStyle.InlineSmaller095,
                        IconRefs.DamageAmount.DefaultColorHexWithHDRHex );
                    popupBuffer.Space1x().AddRaw( (-drawnPhysicalDamage).ToStringThousandsWhole(), IconRefs.DamageAmount.DefaultColorHexWithHDRHex );

                    if ( drawnMoraleDamage > 0 )
                    {
                        popupBuffer.Space1x();
                        popupBuffer.AddSpriteStyled_NoIndent( ActorRefs.UnitMorale.Icon, AdjustedSpriteStyle.InlineSmaller095,
                            IconRefs.DamageAmount.DefaultColorHexWithHDRHex );
                        popupBuffer.Space1x().AddRaw( (-drawnMoraleDamage).ToStringThousandsWhole(), IconRefs.DamageAmount.DefaultColorHexWithHDRHex );
                    }
                }

                debugStage = 12100;

                if ( targetNPC != null )
                {
                    debugStage = 12100;

                    if ( squadMembersLost > 0 )
                    {
                        debugStage = 14100;

                        targetNPC.CurrentSquadSize -= squadMembersLost;

                        if ( shouldDoDamageTextPopupsAndLogging && popupBuffer != null )
                        {
                            popupBuffer.Space3x();

                            popupBuffer.AddSpriteStyled_NoIndent( IconRefs.KillCount.Icon, AdjustedSpriteStyle.InlineSmaller095,
                                IconRefs.KillCount.DefaultColorHexWithHDRHex );
                            popupBuffer.Space1x().AddRaw( (-drawnSquadMembersLost).ToStringThousandsWhole(), IconRefs.KillCount.DefaultColorHexWithHDRHex );
                        }

                        debugStage = 15100;

                        if ( Attacker is ISimMachineUnit AttackerMachineUnit )
                        {
                            debugStage = 16100;

                            ApplyBasicsOfAttacksToUnit( AttackerMachineUnit, targetNPC, false );

                            if ( targetNPC?.UnitType?.DeathsCountAsMurders ?? false )
                            {
                                CityStatisticTable.AlterScore( "Murders", squadMembersLost );
                            }
                            else if ( targetNPC?.UnitType?.DeathsCountAsAttemptedMurders ?? false )
                            {
                                CityStatisticTable.AlterScore( "AttemptedMurders", squadMembersLost );

                                if ( shouldDoDamageTextPopupsAndLogging && popupBuffer != null )
                                {
                                    debugStage = 6100;

                                    if ( oldDamageText != null )
                                        oldDamageText.MarkMeAsExpiredNow();

                                    popupBuffer.Clear();
                                    popupBuffer.AddLang( "EscapedPopup", IconRefs.DamageAmount.DefaultColorHexWithHDRHex );

                                    if ( drawnMoraleDamage > 0 )
                                    {
                                        popupBuffer.Space1x();
                                        popupBuffer.AddSpriteStyled_NoIndent( ActorRefs.UnitMorale.Icon, AdjustedSpriteStyle.InlineSmaller095,
                                            IconRefs.DamageAmount.DefaultColorHexWithHDRHex );
                                    }
                                }
                            }
                            else if ( targetNPC?.UnitType?.IsHuman ?? false )
                            {
                                CityStatisticTable.AlterScore( "CombatKillsHuman", squadMembersLost );
                            }
                            else
                            {
                                CityStatisticTable.AlterScore( "CombatKillsOther", squadMembersLost );
                            }
                        }
                        else if ( Attacker is ISimMachineVehicle AttackerMachineVehicle )
                        {
                            debugStage = 17100;

                            ApplyBasicsOfAttacksToUnit( AttackerMachineVehicle, targetNPC, false );

                            if ( targetNPC?.UnitType?.DeathsCountAsMurders ?? false )
                            {
                                CityStatisticTable.AlterScore( "Murders", squadMembersLost );
                            }
                            else if ( targetNPC?.UnitType?.DeathsCountAsAttemptedMurders ?? false )
                            {
                                CityStatisticTable.AlterScore( "AttemptedMurders", squadMembersLost );

                                if ( shouldDoDamageTextPopupsAndLogging && popupBuffer != null )
                                {
                                    debugStage = 6100;

                                    if ( oldDamageText != null )
                                        oldDamageText.MarkMeAsExpiredNow();

                                    popupBuffer.Clear();
                                    popupBuffer.AddLang( "EscapedPopup", IconRefs.DamageAmount.DefaultColorHexWithHDRHex );

                                    if ( drawnMoraleDamage > 0 )
                                    {
                                        popupBuffer.Space1x();
                                        popupBuffer.AddSpriteStyled_NoIndent( ActorRefs.UnitMorale.Icon, AdjustedSpriteStyle.InlineSmaller095,
                                            IconRefs.DamageAmount.DefaultColorHexWithHDRHex );
                                    }
                                }
                            }
                            else if ( targetNPC?.UnitType?.IsHuman ?? false )
                            {
                                CityStatisticTable.AlterScore( "CombatKillsHuman", squadMembersLost );
                            }
                            else
                            {
                                CityStatisticTable.AlterScore( "CombatKillsOther", squadMembersLost );
                            }
                        }
                        else if ( Attacker is ISimNPCUnit AttackerNPCUnit )
                        {
                            debugStage = 18100;

                            if ( AttackerNPCUnit.GetIsPlayerControlled() )
                            {
                                debugStage = 19100;

                                if ( targetNPC?.UnitType?.DeathsCountAsMurders ?? false )
                                {
                                    CityStatisticTable.AlterScore( "Murders", squadMembersLost );
                                }
                                else if ( targetNPC?.UnitType?.DeathsCountAsAttemptedMurders ?? false )
                                {
                                    CityStatisticTable.AlterScore( "AttemptedMurders", squadMembersLost );

                                    if ( shouldDoDamageTextPopupsAndLogging && popupBuffer != null )
                                    {
                                        debugStage = 6100;

                                        if ( oldDamageText != null )
                                            oldDamageText.MarkMeAsExpiredNow();

                                        popupBuffer.Clear();
                                        popupBuffer.AddLang( "EscapedPopup", IconRefs.DamageAmount.DefaultColorHexWithHDRHex );

                                        if ( drawnMoraleDamage > 0 )
                                        {
                                            popupBuffer.Space1x();
                                            popupBuffer.AddSpriteStyled_NoIndent( ActorRefs.UnitMorale.Icon, AdjustedSpriteStyle.InlineSmaller095,
                                                IconRefs.DamageAmount.DefaultColorHexWithHDRHex );
                                        }
                                    }
                                }
                                else if ( targetNPC?.UnitType?.IsHuman ?? false )
                                {
                                    CityStatisticTable.AlterScore( "CombatKillsHuman", squadMembersLost );
                                }
                                else
                                {
                                    CityStatisticTable.AlterScore( "CombatKillsOther", squadMembersLost );
                                }
                            }
                            else
                            {
                                debugStage = 20100;

                                if ( AttackerNPCUnit?.UnitType?.IsMechStyleMovement ?? false )
                                {
                                    debugStage = 22100;

                                    if ( targetNPC?.UnitType?.DeathsCountAsMurders ?? false )
                                    {
                                        CityStatisticTable.AlterScore( "HumanMechCombatMurders", squadMembersLost );
                                    }
                                    else if ( targetNPC?.UnitType?.DeathsCountAsAttemptedMurders ?? false )
                                    {
                                        //eh?
                                        CityStatisticTable.AlterScore( "AttemptedMurders", squadMembersLost );

                                        if ( shouldDoDamageTextPopupsAndLogging && popupBuffer != null )
                                        {
                                            debugStage = 6100;

                                            if ( oldDamageText != null )
                                                oldDamageText.MarkMeAsExpiredNow();

                                            popupBuffer.Clear();
                                            popupBuffer.AddLang( "EscapedPopup", IconRefs.DamageAmount.DefaultColorHexWithHDRHex );

                                            if ( drawnMoraleDamage > 0 )
                                            {
                                                popupBuffer.Space1x();
                                                popupBuffer.AddSpriteStyled_NoIndent( ActorRefs.UnitMorale.Icon, AdjustedSpriteStyle.InlineSmaller095,
                                                    IconRefs.DamageAmount.DefaultColorHexWithHDRHex );
                                            }
                                        }
                                    }
                                    else if ( targetNPC?.UnitType?.IsHuman ?? false )
                                    {
                                        CityStatisticTable.AlterScore( "HumanMechVHumanCombatKills", squadMembersLost );
                                    }
                                    else if ( targetNPC?.UnitType?.IsMechStyleMovement ?? false )
                                    {
                                        CityStatisticTable.AlterScore( "HumanMechVMechCombatKills", squadMembersLost );
                                    }
                                    else if ( targetNPC?.UnitType?.IsVehicle ?? false )
                                    {
                                        CityStatisticTable.AlterScore( "HumanMechVVehicleCombatKills", squadMembersLost );
                                    }
                                    else
                                    {
                                        CityStatisticTable.AlterScore( "HumanMechVOtherCombatKills", squadMembersLost );
                                    }
                                }
                                else if ( AttackerNPCUnit?.UnitType?.IsVehicle ?? false )
                                {
                                    debugStage = 23100;

                                    if ( targetNPC?.UnitType?.DeathsCountAsMurders ?? false )
                                    {
                                        CityStatisticTable.AlterScore( "HumanVehicleCombatMurders", squadMembersLost );
                                    }
                                    else if ( targetNPC?.UnitType?.DeathsCountAsAttemptedMurders ?? false )
                                    {
                                        //eh?
                                        CityStatisticTable.AlterScore( "AttemptedMurders", squadMembersLost );

                                        if ( shouldDoDamageTextPopupsAndLogging && popupBuffer != null )
                                        {
                                            debugStage = 6100;

                                            if ( oldDamageText != null )
                                                oldDamageText.MarkMeAsExpiredNow();

                                            popupBuffer.Clear();
                                            popupBuffer.AddLang( "EscapedPopup", IconRefs.DamageAmount.DefaultColorHexWithHDRHex );

                                            if ( drawnMoraleDamage > 0 )
                                            {
                                                popupBuffer.Space1x();
                                                popupBuffer.AddSpriteStyled_NoIndent( ActorRefs.UnitMorale.Icon, AdjustedSpriteStyle.InlineSmaller095,
                                                    IconRefs.DamageAmount.DefaultColorHexWithHDRHex );
                                            }
                                        }
                                    }
                                    else if ( targetNPC?.UnitType?.IsHuman ?? false )
                                    {
                                        CityStatisticTable.AlterScore( "HumanVehicleVHumanCombatKills", squadMembersLost );
                                    }
                                    else if ( targetNPC?.UnitType?.IsMechStyleMovement ?? false )
                                    {
                                        CityStatisticTable.AlterScore( "HumanVehicleVMechCombatKills", squadMembersLost );
                                    }
                                    else if ( targetNPC?.UnitType?.IsVehicle ?? false )
                                    {
                                        CityStatisticTable.AlterScore( "HumanVehicleVVehicleCombatKills", squadMembersLost );
                                    }
                                    else
                                    {
                                        CityStatisticTable.AlterScore( "HumanVehicleVOtherCombatKills", squadMembersLost );
                                    }
                                }
                                else
                                {
                                    debugStage = 24100;

                                    if ( targetNPC?.UnitType?.DeathsCountAsMurders ?? false )
                                    {
                                        CityStatisticTable.AlterScore( "HumanCombatMurders", squadMembersLost );
                                    }
                                    else if ( targetNPC?.UnitType?.DeathsCountAsAttemptedMurders ?? false )
                                    {
                                        CityStatisticTable.AlterScore( "HumanCombatAttemptedMurders", squadMembersLost );
                                    }
                                    else if ( targetNPC?.UnitType?.IsHuman ?? false )
                                    {
                                        CityStatisticTable.AlterScore( "HumanVHumanCombatKills", squadMembersLost );
                                    }
                                    else if ( targetNPC?.UnitType?.IsMechStyleMovement ?? false )
                                    {
                                        CityStatisticTable.AlterScore( "HumanVMechCombatKills", squadMembersLost );
                                    }
                                    else if ( targetNPC?.UnitType?.IsVehicle??false )
                                    {
                                        CityStatisticTable.AlterScore( "HumanVVehicleCombatKills", squadMembersLost );
                                    }
                                    else
                                    {
                                        CityStatisticTable.AlterScore( "HumanVOtherCombatKills", squadMembersLost );
                                    }
                                }
                            }
                        }
                    }
                    else //in the event that NO sqaudmembers were lost by this npc!
                    {
                        debugStage = 32100;

                        if ( Attacker is ISimMachineUnit AttackerMachineUnit )
                        {
                            debugStage = 33100;

                            if ( targetNPC.IsCityConflictUnit == null )
                            {
                                if ( targetNPC?.UnitType?.AppliedOutcastBadgeIfAttackedByMachine != null && AttackerMachineUnit.OutcastLevel < 1 )
                                {
                                    AttackerMachineUnit.AddOrRemoveBadge( targetNPC?.UnitType?.AppliedOutcastBadgeIfAttackedByMachine, true );
                                    //AttackerMachineUnit.IsOutcast = true;
                                }
                            }

                            Attacker.ApplyVisibilityFromAction( ActionVisibility.IsAttack );
                        }
                        else if ( Attacker is ISimMachineVehicle AttackerMachineVehicle )
                        {
                            debugStage = 35100;

                            if ( targetNPC.IsCityConflictUnit == null )
                            {
                                if ( targetNPC?.UnitType?.AppliedOutcastBadgeIfAttackedByMachine != null && AttackerMachineVehicle.OutcastLevel < 1 )
                                {
                                    AttackerMachineVehicle.AddOrRemoveBadge( targetNPC?.UnitType?.AppliedOutcastBadgeIfAttackedByMachine, true );
                                    //AttackerMachineVehicle.IsOutcast = true;
                                }
                            }

                            Attacker.ApplyVisibilityFromAction( ActionVisibility.IsAttack );
                        }
                        else if ( Attacker is ISimNPCUnit AttackerNPCUnit )
                        {
                            //nothing to do
                        }
                    }
                }

                debugStage = 41100;

                if ( shouldDoDamageTextPopupsAndLogging && popupBuffer != null )
                {
                    debugStage = 42100;

                    DamageTextPopups.FloatingDamageTextPopup newDamageText = DamageTextPopups.CreateNewFromTextBuffer( popupBuffer,
                        startLocation, endLocation, 0.7f, MathA.Max( 2, GameSettings.Current.GetInt( "DamagePopup_LingerTime" ) ) );
                    if ( newDamageText != null )
                    {
                        newDamageText.PhysicalDamageIncluded = drawnPhysicalDamage;
                        newDamageText.MoraleDamageIncluded = drawnMoraleDamage;
                        newDamageText.SquadDeathsIncluded = drawnSquadMembersLost;
                        Target.MostRecentDamageText = newDamageText;
                    }
                }

                debugStage = 43100;

                if ( shouldDoDamageTextPopupsAndLogging )
                {
                    debugStage = 44100;
                    if ( Attacker is ISimNPCUnit attackerNPCUnit )
                    {
                        debugStage = 45100;

                        if ( Target is ISimNPCUnit targetNPCUnit )
                        {
                            debugStage = 46100;
                            ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "NPCUnitAttackNPCUnit" ),
                                NoteStyle.StoredGame, attackerNPCUnit?.UnitType?.ID, targetNPCUnit?.UnitType?.ID,
                                attackerNPCUnit.FromCohort.ID, targetNPCUnit.FromCohort.ID,
                                attackerNPCUnit.ActorID, 0, physicalDamage,
                                string.Empty, string.Empty, string.Empty,
                                SimCommon.Turn + 10 );
                        }
                        else if ( Target is ISimMachineUnit targetMachineUnit )
                        {
                            debugStage = 47100;
                            ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "NPCUnitAttackMachineUnit" ),
                                NoteStyle.StoredGame, attackerNPCUnit?.UnitType?.ID, targetMachineUnit?.UnitType?.ID, attackerNPCUnit?.FromCohort?.ID, string.Empty,
                                attackerNPCUnit.ActorID, 0, physicalDamage,
                                targetMachineUnit.GetDisplayName(), string.Empty, string.Empty,
                                SimCommon.Turn + 10 );
                        }
                        else if ( Target is ISimMachineVehicle targetMachineVehicle )
                        {
                            debugStage = 48100;
                            ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "NPCUnitAttackMachineVehicle" ),
                                NoteStyle.StoredGame, attackerNPCUnit?.UnitType?.ID, targetMachineVehicle?.VehicleType?.ID, attackerNPCUnit?.FromCohort?.ID, string.Empty,
                                attackerNPCUnit.ActorID, 0, physicalDamage,
                                targetMachineVehicle.GetDisplayName(), string.Empty, string.Empty,
                                SimCommon.Turn + 10 );
                        }
                        else if ( Target is MachineStructure structure )
                        {
                            debugStage = 49100;
                            ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "NPCUnitAttackMachineStructure" ),
                                NoteStyle.StoredGame, attackerNPCUnit?.UnitType?.ID, structure?.Type?.ID, attackerNPCUnit?.FromCohort?.ID, structure?.CurrentJob?.ID ?? string.Empty,
                                attackerNPCUnit.ActorID, 0, physicalDamage,
                                string.Empty, string.Empty, string.Empty,
                                SimCommon.Turn + 10 );
                        }
                        else
                            ArcenDebugging.LogSingleLine( "Unknown type of target attacked by an npc!", Verbosity.ShowAsError );
                    }
                    else if ( Attacker is ISimMachineUnit attackerMachineUnit )
                    {
                        debugStage = 51100;

                        if ( Target is ISimNPCUnit targetNPCUnit )
                        {
                            debugStage = 52100;
                            SimCommon.NeedsToAttemptAnotherNPCTargetingPass = true;
                            ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "MachineUnitAttackNPCUnit" ),
                                NoteStyle.StoredGame, attackerMachineUnit?.UnitType?.ID, targetNPCUnit?.UnitType?.ID, targetNPCUnit?.FromCohort?.ID, string.Empty,
                                attackerMachineUnit.ActorID, 0, physicalDamage,
                                attackerMachineUnit.GetDisplayName(), string.Empty, string.Empty,
                                SimCommon.Turn + 10 );
                        }
                    }
                    else if ( Attacker is ISimMachineVehicle attackerMachineVehicle )
                    {
                        debugStage = 61100;
                        if ( Target is ISimNPCUnit targetNPCUnit )
                        {
                            debugStage = 62100;
                            SimCommon.NeedsToAttemptAnotherNPCTargetingPass = true;
                            ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "MachineVehicleAttackNPCUnit" ),
                                NoteStyle.StoredGame, attackerMachineVehicle?.VehicleType?.ID, targetNPCUnit?.UnitType?.ID, targetNPCUnit?.FromCohort?.ID, string.Empty,
                                attackerMachineVehicle.ActorID, 0, physicalDamage,
                                attackerMachineVehicle.GetDisplayName(), string.Empty, string.Empty,
                                SimCommon.Turn + 10 );
                        }
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "ApplyFinalPhysicalDamageAndStatsAndPopup", debugStage, e, Verbosity.ShowAsError );
            }
        }
        #endregion

        #region ApplyFinalMoraleDamageAndStatsAndPopup
        private static void ApplyFinalMoraleDamageAndStatsAndPopup( ISimMapActor Attacker, ISimMapActor Target,
            bool shouldDoDamageTextPopups, bool shouldDoLogging, int moraleDamage, MapActorData morale, ArcenDoubleCharacterBuffer popupBuffer )
        {
            if ( morale == null || Attacker == null || Target == null )
                return;

            int debugStage = 0;
            try
            {
                debugStage = 100;

                DamageTextPopups.FloatingDamageTextPopup oldDamageText = null;
                if ( shouldDoDamageTextPopups )
                {
                    debugStage = 500;
                    oldDamageText = Target.MostRecentDamageText;
                    if ( oldDamageText != null && oldDamageText.GetTimeHasExisted() > 1f )
                        oldDamageText = null; //if it's been more than 1 second, don't combine them
                }

                debugStage = 1100;

                morale.AlterCurrent( -moraleDamage );

                Vector3 startLocation = Target.GetPositionForCollisions();
                Vector3 endLocation = startLocation.PlusY( Target.GetHalfHeightForCollisions() + 0.2f );

                ISimNPCUnit targetNPC = Target as ISimNPCUnit;

                debugStage = 2100;

                int drawnMoraleDamage = moraleDamage + (oldDamageText?.MoraleDamageIncluded ?? 0);

                if ( shouldDoDamageTextPopups )
                {
                    debugStage = 3100;

                    if ( oldDamageText != null )
                        oldDamageText.MarkMeAsExpiredNow();

                    if ( morale.Current <= 0 )
                        popupBuffer.AddLang( "IncapacitatedPopup", ColorTheme.RedOrange2 ).Space1x();

                    popupBuffer.AddSpriteStyled_NoIndent( ActorRefs.UnitMorale.Icon, AdjustedSpriteStyle.InlineSmaller095,
                        IconRefs.DamageAmount.DefaultColorHexWithHDRHex );
                    popupBuffer.Space1x().AddRaw( (-drawnMoraleDamage).ToStringThousandsWhole(), IconRefs.DamageAmount.DefaultColorHexWithHDRHex );
                }

                debugStage = 7100;

                if ( targetNPC != null )
                {
                    debugStage = 8100;

                    if ( morale.Current <= 0 )
                    {
                        debugStage = 12100;

                        if ( Attacker is ISimMachineUnit AttackerMachineUnit )
                        {
                            debugStage = 13100;
                            ApplyBasicsOfAttacksToUnit( AttackerMachineUnit, targetNPC, true );

                            CityStatisticTable.AlterScore( "NonlethalTakedowns", Mathf.Max( 1, targetNPC.CurrentSquadSize ) );

                            CityStatisticTable.AlterScore( "NonlethalSquadsIncapacitations", 1 );
                        }
                        else if ( Attacker is ISimMachineVehicle AttackerMachineVehicle )
                        {
                            debugStage = 14100;
                            ApplyBasicsOfAttacksToUnit( AttackerMachineVehicle, targetNPC, true );

                            CityStatisticTable.AlterScore( "NonlethalTakedowns", Mathf.Max( 1, targetNPC.CurrentSquadSize ) );

                            CityStatisticTable.AlterScore( "NonlethalSquadsIncapacitations", 1 );
                        }
                        else if ( Attacker is ISimNPCUnit AttackerNPCUnit )
                        {
                            debugStage = 15100;
                            if ( AttackerNPCUnit.GetIsPlayerControlled() )
                            {
                                CityStatisticTable.AlterScore( "NonlethalTakedowns", Mathf.Max( 1, targetNPC.CurrentSquadSize ) );

                                CityStatisticTable.AlterScore( "NonlethalSquadsIncapacitations", 1 );
                            }
                            else
                            {
                                CityStatisticTable.AlterScore( "ThirdPartyNonlethalTakedowns", Mathf.Max( 1, targetNPC.CurrentSquadSize ) );

                                CityStatisticTable.AlterScore( "ThirdPartyNonlethalSquadsIncapacitations", 1 );
                            }
                        }
                    }
                    else //in the event that this wasn't a total kill
                    {
                        debugStage = 22100;

                        if ( Attacker is ISimMachineUnit AttackerMachineUnit )
                        {
                            debugStage = 23100;
                            //no AppliedOutcastBadgeIfAttackedByMachine from morale damage!

                            Attacker.ApplyVisibilityFromAction( ActionVisibility.IsAttack );
                        }
                        else if ( Attacker is ISimMachineVehicle AttackerMachineVehicle )
                        {
                            debugStage = 24100;
                            //no AppliedOutcastBadgeIfAttackedByMachine from morale damage!

                            Attacker.ApplyVisibilityFromAction( ActionVisibility.IsAttack );
                        }
                        else if ( Attacker is ISimNPCUnit AttackerNPCUnit )
                        {
                            //nothing to do
                        }
                    }
                }

                debugStage = 41100;

                if ( shouldDoDamageTextPopups && popupBuffer != null )
                {
                    debugStage = 41700;

                    DamageTextPopups.FloatingDamageTextPopup newDamageText = DamageTextPopups.CreateNewFromTextBuffer( popupBuffer,
                        startLocation, endLocation, 0.7f, MathA.Max( 2, GameSettings.Current.GetInt( "DamagePopup_LingerTime" ) ) );
                    if ( newDamageText != null )
                    {
                        newDamageText.PhysicalDamageIncluded = 0;
                        newDamageText.MoraleDamageIncluded = drawnMoraleDamage;
                        newDamageText.SquadDeathsIncluded = 0;
                        Target.MostRecentDamageText = newDamageText;
                    }
                }

                debugStage = 51100;

                if ( shouldDoLogging )
                {
                    debugStage = 61100;

                    if ( Attacker is ISimNPCUnit attackerNPCUnit )
                    {
                        if ( Target is ISimNPCUnit targetNPCUnit )
                        {
                            debugStage = 71100;

                            ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "NPCUnitAttackMoraleOfNPCUnit" ),
                                NoteStyle.StoredGame, attackerNPCUnit?.UnitType?.ID, targetNPCUnit?.UnitType?.ID,
                                attackerNPCUnit?.FromCohort?.ID, targetNPCUnit?.FromCohort?.ID,
                                attackerNPCUnit.ActorID, morale.Current, moraleDamage,
                                string.Empty, string.Empty, string.Empty,
                                SimCommon.Turn + 10 );
                        }
                        else
                            ArcenDebugging.LogSingleLine( "Unknown type of target morale-attacked by an npc!", Verbosity.ShowAsError );
                    }
                    else if ( Attacker is ISimMachineUnit attackerMachineUnit )
                    {
                        if ( Target is ISimNPCUnit targetNPCUnit )
                        {
                            debugStage = 81100;

                            SimCommon.NeedsToAttemptAnotherNPCTargetingPass = true;
                            ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "MachineUnitAttackMoraleOfNPCUnit" ),
                                NoteStyle.StoredGame, attackerMachineUnit?.UnitType?.ID, targetNPCUnit?.UnitType?.ID, targetNPCUnit?.FromCohort?.ID, string.Empty,
                                attackerMachineUnit.ActorID, morale.Current, moraleDamage,
                                attackerMachineUnit.GetDisplayName(), string.Empty, string.Empty,
                                SimCommon.Turn + 10 );
                        }
                    }
                    else if ( Attacker is ISimMachineVehicle attackerMachineVehicle )
                    {
                        if ( Target is ISimNPCUnit targetNPCUnit )
                        {
                            debugStage = 91100;

                            SimCommon.NeedsToAttemptAnotherNPCTargetingPass = true;
                            ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "MachineVehicleAttackMoraleOfNPCUnit" ),
                                NoteStyle.StoredGame, attackerMachineVehicle?.VehicleType?.ID, targetNPCUnit?.UnitType?.ID, targetNPCUnit?.FromCohort?.ID, string.Empty,
                                attackerMachineVehicle.ActorID, morale.Current, moraleDamage,
                                attackerMachineVehicle.GetDisplayName(), string.Empty, string.Empty,
                                SimCommon.Turn + 10 );
                        }
                    }
                }

                debugStage = 121100;
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "ApplyFinalMoraleDamageAndStatsAndPopup", debugStage, e, Verbosity.ShowAsError );
            }
        }
        #endregion
    }
}
