using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.External
{
    public static class CombatTextHelper
    {
        public static int NextTurn_EnemySquadsTargeting = 0;
        public static int NextTurn_EnemiesTargeting = 0;
        public static AttackAmounts NextTurn_DamageFromEnemies = AttackAmounts.Zero();

        public static int AttackOfOpportunity_EnemySquadsTargeting = 0;
        public static int AttackOfOpportunity_EnemiesTargeting = 0;
        public static int AttackOfOpportunity_MinDamageFromEnemies = 0;
        public static int AttackOfOpportunity_MaxDamageFromEnemies = 0;

        public static void ClearPredictionStats()
        {
            NextTurn_EnemySquadsTargeting = 0;
            NextTurn_EnemiesTargeting = 0;
            NextTurn_DamageFromEnemies = AttackAmounts.Zero();
            AttackOfOpportunity_EnemySquadsTargeting = 0;
            AttackOfOpportunity_EnemiesTargeting = 0;
            AttackOfOpportunity_MinDamageFromEnemies = 0;
            AttackOfOpportunity_MaxDamageFromEnemies = 0;
        }

        #region GetHasAnyPredictionsToShow
        public static bool GetHasAnyPredictionsToShow( ISimMapMobileActor Actor )
        {
            if ( Actor == null )
                return false;
            if ( NextTurn_DamageFromEnemies.IsEmpty() && AttackOfOpportunity_MaxDamageFromEnemies <= 0 )
                return false;
            return true;
        }
        #endregion

        #region AppendLastPredictedDamageBrief
        public static void AppendLastPredictedDamageBrief( ISimMapMobileActor Actor, ArcenCharacterBufferBase Buffer, TTTextBefore Before, TTTextAfter After )
        {
            if ( Actor == null ) 
                return;
            if ( NextTurn_DamageFromEnemies.IsEmpty() && AttackOfOpportunity_MaxDamageFromEnemies <= 0 )
                return;

            switch ( Before )
            {
                case TTTextBefore.SpacingAlways:
                    Buffer.Space3x();
                    break;
                case TTTextBefore.SpacingIfNotEmpty:
                    Buffer.Space3xIfNotEmpty();
                    break;
                case TTTextBefore.LineAlways:
                    Buffer.Line();
                    break;
            }

            if ( AttackOfOpportunity_MaxDamageFromEnemies > 0 )
            {
                string healthColor = ColorTheme.AttackOfOpportunity;
                int unitHPRemaining = Actor.GetActorDataCurrent( ActorRefs.ActorHP, true );
                bool willDie = unitHPRemaining <= AttackOfOpportunity_MinDamageFromEnemies;
                bool mightDie = !willDie && unitHPRemaining <= AttackOfOpportunity_MaxDamageFromEnemies;

                int percentageMin = MathA.IntPercentage( AttackOfOpportunity_MinDamageFromEnemies, unitHPRemaining );
                int percentageMax = MathA.IntPercentage( AttackOfOpportunity_MaxDamageFromEnemies, unitHPRemaining );

                Buffer.AddSpriteStyled_NoIndent( IconRefs.AttackOfOpportunity.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                    healthColor );
                Buffer.AddFormat2( "MinMaxRangePercent", percentageMin.ToString(), percentageMax.ToString(), healthColor ).Space1x().StartSize50();
                Buffer.AddLang( "Damage_Abbrev", healthColor ).EndSize();
                if ( willDie )
                    Buffer.Space1x().StartSize80().AddLang( "Death_Brief", ColorTheme.RedOrange ).EndSize();
                else if ( mightDie )
                    Buffer.Space1x().StartSize80().AddLang( "MightDie_Brief", ColorTheme.RedOrange ).EndSize();
            }

            if ( NextTurn_DamageFromEnemies.Physical > 0 )
            {
                if ( AttackOfOpportunity_MaxDamageFromEnemies > 0 )
                    Buffer.Space3x();

                string healthColor = ColorTheme.RedOrange2;
                int unitHPRemaining = Actor.GetActorDataCurrent( ActorRefs.ActorHP, true );
                bool willDie = unitHPRemaining <= NextTurn_DamageFromEnemies.Physical;
                if ( willDie )
                    healthColor = ColorTheme.RedOrange;

                int percentage = MathA.IntPercentage( NextTurn_DamageFromEnemies.Physical, unitHPRemaining );

                Buffer.AddSpriteStyled_NoIndent( ActorRefs.ActorHP.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                    healthColor );
                Buffer.AddRaw( percentage.ToStringIntPercent(), healthColor ).Space1x().StartSize50();
                Buffer.AddLang( "Damage_Abbrev", healthColor ).EndSize();
                if ( willDie )
                    Buffer.Space1x().StartSize80().AddLang( "Death_Brief", healthColor ).EndSize();
            }

            if ( After == TTTextAfter.Linebreak )
                Buffer.Line();
        }
        #endregion

        #region AppendLastPredictedDamageLong
        public static void AppendLastPredictedDamageLong( ISimMachineActor Actor, ArcenCharacterBufferBase BufferHeader, ArcenCharacterBufferBase BufferBody, bool DoFullLong, bool IsStayingHere, bool IsMakingAttack )
        {
            if ( Actor == null )
                return;
            if ( NextTurn_DamageFromEnemies.Physical <= 0 && AttackOfOpportunity_MaxDamageFromEnemies <= 0 )
            {
                if ( IsMakingAttack )
                {
                    BufferHeader.AddSpriteStyled_NoIndent( ActorRefs.ActorHP.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange2 )
                        .AddNeverTranslated( DoFullLong ? "<size=100%>" : "<size=80%>", false )
                        .AddLangAndAfterLineItemHeader( DoFullLong ? "IncomingDamageLong" : "IncomingDamageShort", ColorTheme.RedOrange2 ).EndSize();
                    BufferHeader.AddRaw( 0.ToStringIntPercent(), ColorTheme.RedOrange2 ).Space1x();

                    if ( DoFullLong )
                        BufferHeader.StartSize60().AddLang( "Damage", ColorTheme.RedOrange2 ).EndSize();
                    else
                        BufferHeader.StartSize50().AddLang( "Damage_Abbrev", ColorTheme.RedOrange2 ).EndSize();
                    BufferHeader.Line();
                }
                return;
            }

            if ( AttackOfOpportunity_MaxDamageFromEnemies > 0 )
                AppendAttackOfOpportunityDamageOnly( Actor, BufferHeader, BufferBody, DoFullLong );

            if ( NextTurn_DamageFromEnemies.Physical > 0 )
            {
                ArcenCharacterBufferBase effectiveHeader = AttackOfOpportunity_MaxDamageFromEnemies > 0 ? BufferBody : BufferHeader;
                ArcenCharacterBufferBase effectiveBody = BufferBody;
                if ( AttackOfOpportunity_MaxDamageFromEnemies > 0 )
                    effectiveHeader.LineIfLastWrittenWasNotLine();

                string healthColor = ColorTheme.RedOrange2;
                int unitHPRemaining = Actor.GetActorDataCurrent( ActorRefs.ActorHP, true );
                bool willDie = unitHPRemaining <= NextTurn_DamageFromEnemies.Physical;
                if ( willDie )
                    healthColor = ColorTheme.RedOrange;

                effectiveHeader.AddSpriteStyled_NoIndent( ActorRefs.ActorHP.Icon, AdjustedSpriteStyle.InlineLarger1_2, healthColor )
                    .AddNeverTranslated( DoFullLong ? "<size=100%>" : "<size=80%>", false )
                    .AddLangAndAfterLineItemHeader( DoFullLong ? "IncomingDamageLong" : "IncomingDamageShort", healthColor ).EndSize();

                int percentage = MathA.IntPercentage( NextTurn_DamageFromEnemies.Physical, unitHPRemaining );

                effectiveHeader.AddRaw( percentage.ToStringIntPercent() ).Space1x();

                if ( DoFullLong )
                    effectiveHeader.StartSize60().AddLang( "Damage" ).EndSize();
                else
                    effectiveHeader.StartSize50().AddLang( "Damage_Abbrev" ).EndSize();

                if ( willDie )
                    effectiveHeader.Space1x().StartSize80().AddLang( DoFullLong ? "YourUnitDeath_MidLength" : "Death_Brief", healthColor ).EndSize();
                effectiveHeader.Line();

                if ( DoFullLong )
                {
                    if ( willDie )
                    {
                        if ( IsStayingHere )
                            effectiveBody.AddLang( "YourUnitDeath_Longer_StayHere", ColorTheme.RedOrange3 );
                        else
                            effectiveBody.AddLang( "YourUnitDeath_Longer_DoThis", ColorTheme.RedOrange3 );
                        effectiveBody.Line();
                    }

                    if ( NextTurn_EnemiesTargeting > 0 || NextTurn_EnemySquadsTargeting > 0 )
                    {
                        string enemyColor = ColorTheme.PurpleBrighter;
                        effectiveBody.AddSpriteStyled_NoIndent( IconRefs.Next_NextHostileNPCUnit.Icon, AdjustedSpriteStyle.InlineLarger1_2, enemyColor )
                            .AddLangAndAfterLineItemHeader( "EnemiesTargetingYou", enemyColor ).AddRaw( NextTurn_EnemiesTargeting.ToStringThousandsWhole() )
                            .Space1x().AddFormat1( "EnemySquadsParenthetical", NextTurn_EnemySquadsTargeting.ToStringThousandsWhole(), enemyColor )
                            .Line();
                    }
                }
            }
        }
        #endregion

        #region AppendLastPredictedDamageLongSecondary
        public static void AppendLastPredictedDamageLongSecondary( ISimMapMobileActor Actor, bool DoFullLong, bool IsStayingHere, bool IsMakingAttack, bool CanWriteToFrameText )
        {
            if ( Actor == null )
                return;

            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            if ( NextTurn_DamageFromEnemies.Physical <= 0 && AttackOfOpportunity_MaxDamageFromEnemies <= 0 )
            {
                if ( IsMakingAttack )
                {
                    novel.Main.AddSpriteStyled_NoIndent( ActorRefs.ActorHP.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange2 )
                        .AddNeverTranslated( DoFullLong ? "<size=100%>" : "<size=80%>", false )
                        .AddLangAndAfterLineItemHeader( DoFullLong ? "IncomingDamageLong" : "IncomingDamageShort", ColorTheme.RedOrange2 ).EndSize();
                    novel.Main.AddRaw( 0.ToStringIntPercent(), ColorTheme.RedOrange2 ).Space1x();

                    if ( DoFullLong )
                        novel.Main.StartSize60().AddLang( "Damage", ColorTheme.RedOrange2 ).EndSize();
                    else
                        novel.Main.StartSize50().AddLang( "Damage_Abbrev", ColorTheme.RedOrange2 ).EndSize();
                    novel.Main.Line();
                }
                return;
            }

            if ( AttackOfOpportunity_MaxDamageFromEnemies > 0 && Actor is ISimMachineActor machineActor )
            {
                ArcenDoubleCharacterBuffer bufferHeader = CanWriteToFrameText && NextTurn_DamageFromEnemies.Physical > 0 ? novel.FrameTitle : novel.Main;
                ArcenDoubleCharacterBuffer bufferBody = CanWriteToFrameText && NextTurn_DamageFromEnemies.Physical > 0 ? novel.FrameBody : novel.Main;
                AppendAttackOfOpportunityDamageOnly( machineActor, bufferHeader, bufferBody, DoFullLong );
            }

            if ( NextTurn_DamageFromEnemies.Physical > 0 )
            {
                if ( AttackOfOpportunity_MaxDamageFromEnemies > 0 && !CanWriteToFrameText )
                    novel.Main.LineIfLastWrittenWasNotLine();

                string healthColor = ColorTheme.RedOrange2;
                int unitHPRemaining = Actor.GetActorDataCurrent( ActorRefs.ActorHP, true );
                bool willDie = unitHPRemaining <= NextTurn_DamageFromEnemies.Physical;
                if ( willDie )
                    healthColor = ColorTheme.RedOrange;

                novel.Main.AddSpriteStyled_NoIndent( ActorRefs.ActorHP.Icon, AdjustedSpriteStyle.InlineLarger1_2, healthColor )
                    .AddNeverTranslated( DoFullLong ? "<size=100%>" : "<size=80%>", false )
                    .AddLangAndAfterLineItemHeader( "IncomingDamageShort", healthColor ).EndSize();

                novel.Main.AddRaw( NextTurn_DamageFromEnemies.Physical.ToStringThousandsWhole() ).Space1x();
                novel.Main.StartSize50().AddLang( "Damage_Abbrev" ).EndSize();

                if ( willDie )
                    novel.Main.Space1x().StartSize80().AddLang( DoFullLong ? "YourUnitDeath_MidLength" : "Death_Brief", healthColor ).EndSize();
                novel.Main.Line();

                if ( DoFullLong )
                {
                    if ( willDie )
                    {
                        if ( IsStayingHere )
                            novel.Main.AddLang( "YourUnitDeath_Longer_StayHere", ColorTheme.RedOrange3 );
                        else
                            novel.Main.AddLang( "YourUnitDeath_Longer_DoThis", ColorTheme.RedOrange3 );
                        novel.Main.Line();
                    }

                    if ( NextTurn_EnemiesTargeting > 0 || NextTurn_EnemySquadsTargeting > 0 )
                    {
                        string enemyColor = ColorTheme.PurpleBrighter;
                        novel.Main.AddSpriteStyled_NoIndent( IconRefs.Next_NextHostileNPCUnit.Icon, AdjustedSpriteStyle.InlineLarger1_2, enemyColor )
                            .AddLangAndAfterLineItemHeader( "EnemiesTargetingYou", enemyColor ).AddRaw( NextTurn_EnemiesTargeting.ToStringThousandsWhole() )
                            .Space1x().AddFormat1( "EnemySquadsParenthetical", NextTurn_EnemySquadsTargeting.ToStringThousandsWhole(), enemyColor )
                            .Line();
                    }
                }
            }
        }
        #endregion

        #region AppendAttackOfOpportunityDamageOnly
        public static bool AppendAttackOfOpportunityDamageOnly( ISimMachineActor Actor, ArcenCharacterBufferBase BufferHeader, ArcenCharacterBufferBase BufferBody, bool DoFullLong )
        {
            if ( Actor == null )
                return false;
            if ( AttackOfOpportunity_MaxDamageFromEnemies > 0 )
            {
                string healthColor = ColorTheme.AttackOfOpportunity;
                int unitHPRemaining = Actor.GetActorDataCurrent( ActorRefs.ActorHP, true );
                bool willDie = unitHPRemaining <= AttackOfOpportunity_MinDamageFromEnemies;
                bool mightDie = !willDie && unitHPRemaining <= AttackOfOpportunity_MaxDamageFromEnemies;

                int percentageMin = MathA.IntPercentage( AttackOfOpportunity_MinDamageFromEnemies, unitHPRemaining );
                int percentageMax = MathA.IntPercentage( AttackOfOpportunity_MaxDamageFromEnemies, unitHPRemaining );

                BufferHeader.AddSpriteStyled_NoIndent( IconRefs.AttackOfOpportunity.Icon, AdjustedSpriteStyle.InlineLarger1_2, healthColor )
                    .AddNeverTranslated( DoFullLong ? "<size=100%>" : "<size=80%>", false )
                    .AddLangAndAfterLineItemHeader( DoFullLong ? "PredictedAttackOfOpportunityLong" : "PredictedAttackOfOpportunityShort", healthColor ).EndSize();

                BufferHeader.AddFormat2( "MinMaxRangePercent", percentageMin.ToString(), percentageMax.ToString() ).Space1x();

                if ( DoFullLong )
                    BufferHeader.StartSize60().AddLang( "Damage" ).EndSize();
                else
                    BufferHeader.StartSize50().AddLang( "Damage_Abbrev" ).EndSize();

                if ( willDie )
                    BufferHeader.Space1x().StartSize80().AddLang( DoFullLong ? "YourUnitDeath_MidLength" : "Death_Brief", ColorTheme.RedOrange3 ).EndSize();
                else if ( mightDie )
                    BufferHeader.Space1x().StartSize80().AddLang( DoFullLong ? "YourUnitmightDie_MidLength" : "MightDie_Brief", healthColor ).EndSize();
                BufferHeader.Line();

                if ( DoFullLong )
                {
                    if ( willDie )
                    {
                        BufferBody.AddLang( "YourUnitDeath_Longer_AttackOfOpportunity", ColorTheme.RedOrange3 );
                        BufferBody.Line();
                    }
                    else if ( mightDie )
                    {
                        BufferBody.AddLang( "YourUnitMightDie_Longer_AttackOfOpportunity", healthColor );
                        BufferBody.Line();
                    }
                    else
                    {
                        BufferBody.AddLang( "YourUnitWillTakeDamage_Longer_AttackOfOpportunity", healthColor );
                        BufferBody.Line();
                    }

                    if ( AttackOfOpportunity_EnemiesTargeting > 0 || AttackOfOpportunity_EnemySquadsTargeting > 0 )
                    {
                        string enemyColor = ColorTheme.PurpleBrighter;
                        BufferBody.AddSpriteStyled_NoIndent( IconRefs.Next_NextHostileNPCUnit.Icon, AdjustedSpriteStyle.InlineLarger1_2, enemyColor )
                            .AddLangAndAfterLineItemHeader( "EnemiesTargetingYou", enemyColor ).AddRaw( AttackOfOpportunity_EnemiesTargeting.ToStringThousandsWhole() )
                            .Space1x().AddFormat1( "EnemySquadsParenthetical", AttackOfOpportunity_EnemySquadsTargeting.ToStringThousandsWhole(), enemyColor )
                            .Line();
                    }
                }
                return true;
            }
            return false;
        }
        #endregion

        #region AppendRestrictedAreaLong
        public static bool AppendRestrictedAreaLong( ISimMapMobileActor Actor, ArcenCharacterBufferBase BufferHeader, ArcenCharacterBufferBase BufferBody, bool DoFullLong,
            ISimUnitLocation LocationOrNull, Vector3 ActionLocation, bool OnlyCareIfDoesNotPass )
        {
            if ( Actor == null )
                return false;
            if ( LocationOrNull != null )
                return AppendRestrictedAreaLong( Actor, BufferHeader, BufferBody, DoFullLong, LocationOrNull, OnlyCareIfDoesNotPass );
            else
                return AppendRestrictedAreaLong( Actor, BufferHeader, BufferBody, DoFullLong, ActionLocation, OnlyCareIfDoesNotPass );
        }

        public static bool AppendRestrictedAreaLong( ISimMapMobileActor Actor, ArcenCharacterBufferBase BufferHeader, ArcenCharacterBufferBase BufferBody, bool DoFullLong,
            ISimUnitLocation LocationOrNull, bool OnlyCareIfDoesNotPass )
        {
            if ( Actor == null )
                return false;
            if ( LocationOrNull == null )
                return false;
            return AppendRestrictedAreaLong( Actor, BufferHeader, BufferBody, DoFullLong, LocationOrNull.CalculateLocationPOI(), 
                LocationOrNull as ISimBuilding, SecurityClearanceTable.ByLevel[LocationOrNull.CalculateLocationSecurityClearanceInt()], OnlyCareIfDoesNotPass );
        }

        public static bool AppendRestrictedAreaLong( ISimMapMobileActor Actor, ArcenCharacterBufferBase BufferHeader, ArcenCharacterBufferBase BufferBody, bool DoFullLong,
            Vector3 Location, bool OnlyCareIfDoesNotPass )
        {
            if ( Actor == null )
                return false;
            MapCell cell = CityMap.TryGetWorldCellAtCoordinates( Location );
            if ( cell == null )
                return false;

            MapPOI restrictedPOI = cell.CalculateAreCoordinatesInRestrictedPOI( Location );
            if ( restrictedPOI == null )
                return false;

            return AppendRestrictedAreaLong( Actor, BufferHeader, BufferBody, DoFullLong, restrictedPOI, null, restrictedPOI.Type.RequiredClearance, OnlyCareIfDoesNotPass );
        }

        public static bool AppendRestrictedAreaLong( ISimMapMobileActor Actor, ArcenCharacterBufferBase BufferHeader, ArcenCharacterBufferBase BufferBody, bool DoFullLong,
            MapPOI POI, bool OnlyCareIfDoesNotPass )
        {
            if ( Actor == null )
                return false;
            if ( POI == null )
                return false;
            return AppendRestrictedAreaLong( Actor, BufferHeader, BufferBody, DoFullLong, POI, null, POI.Type.RequiredClearance, OnlyCareIfDoesNotPass );
        }

        public static bool AppendRestrictedAreaLong( ISimMapMobileActor Actor, ArcenCharacterBufferBase BufferHeader, ArcenCharacterBufferBase BufferBody, bool DoFullLong, 
            MapPOI POIOrNull, ISimBuilding BuildingOrNull, SecurityClearance RequiredClearance, bool OnlyCareIfDoesNotPass )
        {
            if ( Actor == null )
                return false;
            if ( RequiredClearance == null || RequiredClearance.Level == 0 )
                return false;

            bool isPassingClearance = false;
            string restrictedColor = ColorTheme.RedOrange;
            int clearanceOfActor = Actor.GetEffectiveClearance( BuildingOrNull != null ? ClearanceCheckType.MovingToBuilding : ClearanceCheckType.MovingToNonBuilding );
            if ( clearanceOfActor >= RequiredClearance.Level )
            {
                isPassingClearance = true;
                restrictedColor = ColorTheme.SkillPaleGreen;
                if ( OnlyCareIfDoesNotPass )
                    return false;
            }

            BufferHeader.AddSpriteStyled_NoIndent( IconRefs.Mouse_RestrictedArea.Icon, AdjustedSpriteStyle.InlineLarger1_2, restrictedColor )
                .AddFormat2AndAfterLineItemHeader( DoFullLong ? ( isPassingClearance ? "RestrictedAreaPass" : "RestrictedArea" ) :
                (isPassingClearance ? "RestrictedAreaPass_Brief" : "RestrictedArea_Brief"),
                RequiredClearance.Level.ToString(), RequiredClearance.GetDisplayName(), restrictedColor );
            if ( BuildingOrNull != null && (BuildingOrNull?.GetVariant()?.RequiredClearance?.Level??0) >= RequiredClearance.Level )
                BufferHeader.AddRaw( BuildingOrNull.GetDisplayName() );
            else if ( POIOrNull != null )
                BufferHeader.AddRaw( POIOrNull.GetDisplayName() );
            BufferHeader.Line();

            if ( DoFullLong )
            {
                if ( isPassingClearance )
                    BufferBody.AddLang( "RestrictedAreaPass_Long", ColorTheme.SkillPaleGreen_Lighter ).Line();
                else
                    BufferBody.AddLang( "RestrictedArea_Long", ColorTheme.RedOrange3 ).Line();
            }

            return true;
        }
        #endregion

        #region GetIsMovingToRestrictedArea
        public static bool GetIsMovingToRestrictedArea( ISimMapMobileActor Actor, ISimUnitLocation LocationOrNull, Vector3 ActionLocation, bool OnlyCareIfDoesNotPass )
        {
            if ( Actor == null )
                return false;

            if ( LocationOrNull != null )
                return GetIsMovingToRestrictedArea( Actor, LocationOrNull, OnlyCareIfDoesNotPass );
            else
                return GetIsMovingToRestrictedArea( Actor, ActionLocation, OnlyCareIfDoesNotPass );
        }

        public static bool GetIsMovingToRestrictedArea( ISimMapMobileActor Actor, ISimUnitLocation LocationOrNull, bool OnlyCareIfDoesNotPass )
        {
            if ( Actor == null )
                return false;
            if ( LocationOrNull == null )
                return false;
            return GetIsMovingToRestrictedArea( Actor, LocationOrNull.CalculateLocationPOI(),
                LocationOrNull as ISimBuilding, SecurityClearanceTable.ByLevel[LocationOrNull.CalculateLocationSecurityClearanceInt()], OnlyCareIfDoesNotPass );
        }

        public static bool GetIsMovingToRestrictedArea( ISimMapMobileActor Actor, Vector3 Location, bool OnlyCareIfDoesNotPass )
        {
            if ( Actor == null )
                return false;
            MapCell cell = CityMap.TryGetWorldCellAtCoordinates( Location );
            if ( cell == null )
                return false;

            MapPOI restrictedPOI = cell.CalculateAreCoordinatesInRestrictedPOI( Location );
            if ( restrictedPOI == null )
                return false;

            return GetIsMovingToRestrictedArea( Actor, restrictedPOI, null, restrictedPOI.Type.RequiredClearance, OnlyCareIfDoesNotPass );
        }

        public static bool GetIsMovingToRestrictedArea( ISimMapMobileActor Actor, MapPOI POI, bool OnlyCareIfDoesNotPass )
        {
            if ( Actor == null )
                return false;
            if ( POI == null )
                return false;
            return GetIsMovingToRestrictedArea( Actor,  POI, null, POI.Type.RequiredClearance, OnlyCareIfDoesNotPass );
        }

        public static bool GetIsMovingToRestrictedArea( ISimMapMobileActor Actor,
            MapPOI POIOrNull, ISimBuilding BuildingOrNull, SecurityClearance RequiredClearance, bool OnlyCareIfDoesNotPass )
        {
            if ( Actor == null )
                return false;
            if ( RequiredClearance == null || RequiredClearance.Level <= 0 )
                return false;

            if ( !OnlyCareIfDoesNotPass )
                return true; //we care either way, so time to tell about this

            int clearanceOfActor = Actor.GetEffectiveClearance( BuildingOrNull != null ? ClearanceCheckType.MovingToBuilding : ClearanceCheckType.MovingToNonBuilding );
            return clearanceOfActor < RequiredClearance.Level;
        }
        #endregion

        #region AppendRestrictedAreaShort
        public static bool AppendRestrictedAreaShort( ISimMapMobileActor Actor, ArcenCharacterBufferBase Buffer, bool DoFullLong,
            ISimUnitLocation LocationOrNull, Vector3 ActionLocation, bool OnlyCareIfDoesNotPass )
        {
            if ( Actor == null )
                return false;

            if ( LocationOrNull != null )
                return AppendRestrictedAreaShort( Actor, Buffer, DoFullLong, LocationOrNull, OnlyCareIfDoesNotPass );
            else
                return AppendRestrictedAreaShort( Actor, Buffer, DoFullLong, ActionLocation, OnlyCareIfDoesNotPass );
        }

        public static bool AppendRestrictedAreaShort( ISimMapMobileActor Actor, ArcenCharacterBufferBase Buffer, bool DoFullLong,
            ISimUnitLocation LocationOrNull, bool OnlyCareIfDoesNotPass )
        {
            if ( Actor == null )
                return false;
            if ( LocationOrNull == null )
                return false;
            return AppendRestrictedAreaShort( Actor, Buffer, DoFullLong, LocationOrNull.CalculateLocationPOI(),
                LocationOrNull as ISimBuilding, SecurityClearanceTable.ByLevel[LocationOrNull.CalculateLocationSecurityClearanceInt()], OnlyCareIfDoesNotPass );
        }

        public static bool AppendRestrictedAreaShort( ISimMapMobileActor Actor, ArcenCharacterBufferBase Buffer, bool DoFullLong,
            Vector3 Location, bool OnlyCareIfDoesNotPass )
        {
            if ( Actor == null )
                return false;
            MapCell cell = CityMap.TryGetWorldCellAtCoordinates( Location );
            if ( cell == null )
                return false;

            MapPOI restrictedPOI = cell.CalculateAreCoordinatesInRestrictedPOI( Location );
            if ( restrictedPOI == null )
                return false;

            return AppendRestrictedAreaShort( Actor, Buffer, DoFullLong, restrictedPOI, null, restrictedPOI.Type.RequiredClearance, OnlyCareIfDoesNotPass );
        }

        //public static bool AppendRestrictedAreaShort( ISimMapMobileActor Actor, ArcenCharacterBufferBase Buffer, bool DoFullLong,
        //    MapPOI POI, bool OnlyCareIfDoesNotPass )
        //{
        //    if ( Actor == null )
        //        return false;
        //    if ( POI == null )
        //        return false;
        //    return AppendRestrictedAreaShort( Actor, Buffer, DoFullLong, POI, null, POI.Type.RequiredClearance, OnlyCareIfDoesNotPass );
        //}

        public static bool AppendRestrictedAreaShort( ISimMapMobileActor Actor, ArcenCharacterBufferBase Buffer, bool DoFullLong,
            MapPOI POIOrNull, ISimBuilding BuildingOrNull, SecurityClearance RequiredClearance, bool OnlyCareIfDoesNotPass )
        {
            if ( Actor == null )
                return false;
            if ( RequiredClearance == null )
                return false;

            bool isPassingClearance = false;
            string restrictedColor = ColorTheme.RedOrange;
            int clearanceOfActor = Actor.GetEffectiveClearance( BuildingOrNull != null ? ClearanceCheckType.MovingToBuilding : ClearanceCheckType.MovingToNonBuilding );
            if ( clearanceOfActor >= RequiredClearance.Level )
            {
                isPassingClearance = true;
                restrictedColor = ColorTheme.SkillPaleGreen;

                if ( OnlyCareIfDoesNotPass )
                    return false;
            }

            Buffer.AddSpriteStyled_NoIndent( IconRefs.Mouse_RestrictedArea.Icon, AdjustedSpriteStyle.InlineLarger1_2, restrictedColor )
                    .AddFormat2AndAfterLineItemHeader( DoFullLong ? (isPassingClearance ? "RestrictedAreaPass" : "RestrictedArea") :
                        (isPassingClearance ? "RestrictedAreaPass_Brief" : "RestrictedArea_Brief"),
                    RequiredClearance.Level.ToString(), RequiredClearance.GetDisplayName(), restrictedColor );
            if ( BuildingOrNull != null && (BuildingOrNull?.GetVariant()?.RequiredClearance?.Level ?? 0) >= RequiredClearance.Level )
                Buffer.AddRaw( BuildingOrNull.GetDisplayName() );
            else if ( POIOrNull != null )
                Buffer.AddRaw( POIOrNull.GetDisplayName() );
            Buffer.Line();
            return true;
        }
        #endregion

        #region GetIsDGDBeingDeployedToRestrictedArea
        public static bool GetIsDGDBeingDeployedToRestrictedArea( MobileActorTypeDuringGameData DGD,
            ISimUnitLocation LocationOrNull, Vector3 ActionLocation, bool OnlyCareIfDoesNotPass )
        {
            if ( LocationOrNull != null )
                return GetIsDGDBeingDeployedToRestrictedArea( DGD, LocationOrNull, OnlyCareIfDoesNotPass );
            else
                return GetIsDGDBeingDeployedToRestrictedArea( DGD, ActionLocation, OnlyCareIfDoesNotPass );
        }

        public static bool GetIsDGDBeingDeployedToRestrictedArea( MobileActorTypeDuringGameData DGD,
            ISimUnitLocation LocationOrNull, bool OnlyCareIfDoesNotPass )
        {
            if ( LocationOrNull == null )
                return false;
            return GetIsDGDBeingDeployedToRestrictedArea( DGD, LocationOrNull.CalculateLocationPOI(),
                LocationOrNull as ISimBuilding, SecurityClearanceTable.ByLevel[LocationOrNull.CalculateLocationSecurityClearanceInt()], OnlyCareIfDoesNotPass );
        }

        public static bool GetIsDGDBeingDeployedToRestrictedArea( MobileActorTypeDuringGameData DGD, 
            Vector3 Location, bool OnlyCareIfDoesNotPass )
        {
            MapCell cell = CityMap.TryGetWorldCellAtCoordinates( Location );
            if ( cell == null )
                return false;

            MapPOI restrictedPOI = cell.CalculateAreCoordinatesInRestrictedPOI( Location );
            if ( restrictedPOI == null )
                return false;

            return GetIsDGDBeingDeployedToRestrictedArea( DGD, restrictedPOI, null, restrictedPOI.Type.RequiredClearance, OnlyCareIfDoesNotPass );
        }

        public static bool GetIsDGDBeingDeployedToRestrictedArea( MobileActorTypeDuringGameData DGD,
            MapPOI POI, bool OnlyCareIfDoesNotPass )
        {
            if ( POI == null )
                return false;
            return GetIsDGDBeingDeployedToRestrictedArea( DGD, POI, null, POI.Type.RequiredClearance, OnlyCareIfDoesNotPass );
        }

        public static bool GetIsDGDBeingDeployedToRestrictedArea( MobileActorTypeDuringGameData DGD,
            MapPOI POIOrNull, ISimBuilding BuildingOrNull, SecurityClearance RequiredClearance, bool OnlyCareIfDoesNotPass )
        {
            if ( RequiredClearance == null || RequiredClearance.Level == 0 )
                return false;

            if ( !OnlyCareIfDoesNotPass )
                return true; //we care either way, so time to tell about this

            DGD.CalculateClearanceLevelsForUnitThatDoesNotYetExist( out bool IsCloaked, out int IsUnremarkableAnywhereUpToClearanceInt, out int IsUnremarkableAtBuildingOnlyUpToClearanceInt );

            if ( OnlyCareIfDoesNotPass && IsCloaked )
                return false;

            int clearanceOfActor = (BuildingOrNull != null ? IsUnremarkableAtBuildingOnlyUpToClearanceInt : IsUnremarkableAnywhereUpToClearanceInt);
            return clearanceOfActor < RequiredClearance.Level;
        }
        #endregion

        #region AppendRestrictedDGDAreaLong
        public static bool AppendRestrictedDGDAreaLong( MobileActorTypeDuringGameData DGD, ArcenCharacterBufferBase BufferHeader, ArcenCharacterBufferBase BufferBody, bool DoFullLong,
            ISimUnitLocation LocationOrNull, Vector3 ActionLocation, bool OnlyCareIfDoesNotPass )
        {
            if ( LocationOrNull != null )
                return AppendRestrictedDGDAreaLong( DGD, BufferHeader, BufferBody, DoFullLong, LocationOrNull, OnlyCareIfDoesNotPass );
            else
                return AppendRestrictedDGDAreaLong( DGD, BufferHeader, BufferBody, DoFullLong, ActionLocation, OnlyCareIfDoesNotPass );
        }

        public static bool AppendRestrictedDGDAreaLong( MobileActorTypeDuringGameData DGD, ArcenCharacterBufferBase BufferHeader, ArcenCharacterBufferBase BufferBody, bool DoFullLong,
            ISimUnitLocation LocationOrNull, bool OnlyCareIfDoesNotPass )
        {
            if ( LocationOrNull == null )
                return false;
            return AppendRestrictedDGDAreaLong( DGD, BufferHeader, BufferBody, DoFullLong, LocationOrNull.CalculateLocationPOI(),
                LocationOrNull as ISimBuilding, SecurityClearanceTable.ByLevel[LocationOrNull.CalculateLocationSecurityClearanceInt()], OnlyCareIfDoesNotPass );
        }

        public static bool AppendRestrictedDGDAreaLong( MobileActorTypeDuringGameData DGD, ArcenCharacterBufferBase BufferHeader, ArcenCharacterBufferBase BufferBody, bool DoFullLong,
            Vector3 Location, bool OnlyCareIfDoesNotPass )
        {
            MapCell cell = CityMap.TryGetWorldCellAtCoordinates( Location );
            if ( cell == null )
                return false;

            MapPOI restrictedPOI = cell.CalculateAreCoordinatesInRestrictedPOI( Location );
            if ( restrictedPOI == null )
                return false;

            return AppendRestrictedDGDAreaLong( DGD, BufferHeader, BufferBody, DoFullLong, restrictedPOI, null, restrictedPOI.Type.RequiredClearance, OnlyCareIfDoesNotPass );
        }

        public static bool AppendRestrictedDGDAreaLong( MobileActorTypeDuringGameData DGD, ArcenCharacterBufferBase BufferHeader, ArcenCharacterBufferBase BufferBody, bool DoFullLong,
            MapPOI POI, bool OnlyCareIfDoesNotPass )
        {
            if ( POI == null )
                return false;
            return AppendRestrictedDGDAreaLong( DGD, BufferHeader, BufferBody, DoFullLong, POI, null, POI.Type.RequiredClearance, OnlyCareIfDoesNotPass );
        }

        public static bool AppendRestrictedDGDAreaLong( MobileActorTypeDuringGameData DGD, ArcenCharacterBufferBase BufferHeader, ArcenCharacterBufferBase BufferBody, bool DoFullLong,
            MapPOI POIOrNull, ISimBuilding BuildingOrNull, SecurityClearance RequiredClearance, bool OnlyCareIfDoesNotPass )
        {
            if ( RequiredClearance == null || RequiredClearance.Level == 0 )
                return false;

            DGD.CalculateClearanceLevelsForUnitThatDoesNotYetExist( out bool IsCloaked, out int IsUnremarkableAnywhereUpToClearanceInt, out int IsUnremarkableAtBuildingOnlyUpToClearanceInt );

            if ( OnlyCareIfDoesNotPass && IsCloaked )
                return false;

            bool isPassingClearance = false;
            string restrictedColor = ColorTheme.RedOrange;
            int clearanceOfActor = (BuildingOrNull != null ? IsUnremarkableAtBuildingOnlyUpToClearanceInt : IsUnremarkableAnywhereUpToClearanceInt);
            if ( clearanceOfActor >= RequiredClearance.Level )
            {
                isPassingClearance = true;
                restrictedColor = ColorTheme.SkillPaleGreen;

                if ( OnlyCareIfDoesNotPass )
                    return false;
            }

            BufferHeader.AddSpriteStyled_NoIndent( IconRefs.Mouse_RestrictedArea.Icon, AdjustedSpriteStyle.InlineLarger1_2, restrictedColor )
                .AddFormat2AndAfterLineItemHeader( DoFullLong ? (isPassingClearance ? "RestrictedAreaPass" : "RestrictedArea") :
                    (isPassingClearance ? "RestrictedAreaPass_Brief" : "RestrictedArea_Brief"),
                    RequiredClearance.Level.ToString(), RequiredClearance.GetDisplayName(), restrictedColor );
            if ( BuildingOrNull != null && (BuildingOrNull?.GetVariant()?.RequiredClearance?.Level ?? 0) >= RequiredClearance.Level )
                BufferHeader.AddRaw( BuildingOrNull.GetDisplayName() );
            else if ( POIOrNull != null )
                BufferHeader.AddRaw( POIOrNull.GetDisplayName() );
            BufferHeader.Line();

            if ( DoFullLong )
            {
                if ( isPassingClearance )
                    BufferBody.AddLang( "RestrictedAreaPass_Long", ColorTheme.SkillPaleGreen_Lighter ).Line();
                else
                    BufferBody.AddLang( "RestrictedArea_Long", ColorTheme.RedOrange3 ).Line();
            }

            return true;
        }
        #endregion

        #region AppendRestrictedDGDAreaShort
        public static bool AppendRestrictedDGDAreaShort( MobileActorTypeDuringGameData DGD, ArcenCharacterBufferBase Buffer, bool DoFullLong,
            ISimUnitLocation LocationOrNull, Vector3 ActionLocation, bool OnlyCareIfDoesNotPass )
        {
            if ( LocationOrNull != null )
                return AppendRestrictedDGDAreaShort( DGD, Buffer, DoFullLong, LocationOrNull, OnlyCareIfDoesNotPass );
            else
                return AppendRestrictedDGDAreaShort( DGD, Buffer, DoFullLong, ActionLocation, OnlyCareIfDoesNotPass );
        }

        public static bool AppendRestrictedDGDAreaShort( MobileActorTypeDuringGameData DGD, ArcenCharacterBufferBase Buffer, bool DoFullLong,
            ISimUnitLocation LocationOrNull, bool OnlyCareIfDoesNotPass )
        {
            if ( LocationOrNull == null )
                return false;
            return AppendRestrictedDGDAreaShort( DGD, Buffer, DoFullLong, LocationOrNull.CalculateLocationPOI(),
                LocationOrNull as ISimBuilding, SecurityClearanceTable.ByLevel[LocationOrNull.CalculateLocationSecurityClearanceInt()], OnlyCareIfDoesNotPass );
        }

        public static bool AppendRestrictedDGDAreaShort( MobileActorTypeDuringGameData DGD, ArcenCharacterBufferBase Buffer, bool DoFullLong,
            Vector3 Location, bool OnlyCareIfDoesNotPass )
        {
            MapCell cell = CityMap.TryGetWorldCellAtCoordinates( Location );
            if ( cell == null )
                return false;

            MapPOI restrictedPOI = cell.CalculateAreCoordinatesInRestrictedPOI( Location );
            if ( restrictedPOI == null )
                return false;

            return AppendRestrictedDGDAreaShort( DGD, Buffer, DoFullLong, restrictedPOI, null, restrictedPOI.Type.RequiredClearance, OnlyCareIfDoesNotPass );
        }

        public static bool AppendRestrictedDGDAreaShort( MobileActorTypeDuringGameData DGD, ArcenCharacterBufferBase Buffer, bool DoFullLong,
            MapPOI POI, bool OnlyCareIfDoesNotPass )
        {
            if ( POI == null )
                return false;
            return AppendRestrictedDGDAreaShort( DGD, Buffer, DoFullLong, POI, null, POI.Type.RequiredClearance, OnlyCareIfDoesNotPass );
        }

        public static bool AppendRestrictedDGDAreaShort( MobileActorTypeDuringGameData DGD, ArcenCharacterBufferBase Buffer, bool DoFullLong,
            MapPOI POIOrNull, ISimBuilding BuildingOrNull, SecurityClearance RequiredClearance, bool OnlyCareIfDoesNotPass )
        {
            if ( RequiredClearance == null || RequiredClearance.Level == 0 )
                return false;

            DGD.CalculateClearanceLevelsForUnitThatDoesNotYetExist( out bool IsCloaked, out int IsUnremarkableAnywhereUpToClearanceInt, out int IsUnremarkableAtBuildingOnlyUpToClearanceInt );

            if ( OnlyCareIfDoesNotPass && IsCloaked )
                return false;

            bool isPassingClearance = false;
            string restrictedColor = ColorTheme.RedOrange;
            int clearanceOfActor = (BuildingOrNull != null ? IsUnremarkableAtBuildingOnlyUpToClearanceInt : IsUnremarkableAnywhereUpToClearanceInt);
            if ( clearanceOfActor >= RequiredClearance.Level )
            {
                isPassingClearance = true;
                restrictedColor = ColorTheme.SkillPaleGreen;

                if ( OnlyCareIfDoesNotPass )
                    return false;
            }

            Buffer.AddSpriteStyled_NoIndent( IconRefs.Mouse_RestrictedArea.Icon, AdjustedSpriteStyle.InlineLarger1_2, restrictedColor )
                    .AddFormat2AndAfterLineItemHeader( DoFullLong ? (isPassingClearance ? "RestrictedAreaPass" : "RestrictedArea") :
                        (isPassingClearance ? "RestrictedAreaPass_Brief" : "RestrictedArea_Brief"),
                    RequiredClearance.Level.ToString(), RequiredClearance.GetDisplayName(), restrictedColor );
            if ( BuildingOrNull != null && (BuildingOrNull?.GetVariant()?.RequiredClearance?.Level ?? 0) >= RequiredClearance.Level )
                Buffer.AddRaw( BuildingOrNull.GetDisplayName() );
            else if ( POIOrNull != null )
                Buffer.AddRaw( POIOrNull.GetDisplayName() );
            Buffer.Line();

            return true;
        }
        #endregion
    }

    public enum TTTextBefore
    {
        None,
        SpacingAlways,
        SpacingIfNotEmpty,
        LineAlways
    }

    public enum TTTextAfter
    {
        None,
        Linebreak
    }
}
