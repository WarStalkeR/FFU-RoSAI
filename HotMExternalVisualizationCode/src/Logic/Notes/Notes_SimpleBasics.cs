using Arcen.Universal;
using System;
using Arcen.HotM.Core;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public class Notes_SimpleBasics : INoteInstructionHandler
    {
        public bool HandleNote( IGameNote rootNote, GameNoteAction Action, ArcenDoubleCharacterBuffer BufferOrNull, bool WriteShortIfPossible, IArcenUIElementForSizing ElementOrNull,
            IGameNote RelatedHeaderNoteOrNull, string SearchString, int AddedInt, bool IsFromQuickOnScreenLog )
        {
            if ( rootNote?.Instruction == null )
            {
                ArcenDebugging.LogSingleLine( "Note with null instruction passed to Notes_SimpleBasics!", Verbosity.ShowAsError );
                return false;
            }
            if ( !(rootNote is SimpleNote Note ))
            {
                ArcenDebugging.LogSingleLine( "Note with instruction " + (rootNote?.Instruction?.ID??"[null]") + " passed to Notes_SimpleBasics without being of type SimpleNote!", Verbosity.ShowAsError );
                return false;

            }

            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;
            SideClamp clamp = (SideClamp)AddedInt;
            TooltipShadowStyle shadowStyle = IsFromQuickOnScreenLog ? TooltipShadowStyle.None : TooltipShadowStyle.Standard;

            try
            {

                switch ( Note.Instruction.ID )
                {
                    case "StartingTurn":
                        switch ( Action )
                        {
                            case GameNoteAction.WriteText:
                                {
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.AddSpriteStyled_NoIndent( IconRefs.Next_NextTurn.Icon, AdjustedSpriteStyle.InlineLarger1_1, ColorTheme.SoftGold ).Space1x()
                                            .AddFormat1( "TurnNumber", Note.Int1.ToStringWholeBasic(), ColorTheme.SoftGold )
                                            //now show the time
                                            .Space5x().StartSize80().AddRaw( Note.Int2.ToString_TimePossiblyMinutesAndHoursFromSeconds(), ColorTheme.GetBasicLightTextBlue( false ) );
                                    else
                                        BufferOrNull.AddFormat1( "StartingTurn", Note.Int1.ToStringWholeBasic() );
                                }
                                break;
                            case GameNoteAction.WriteTooltip:
                                {
                                }
                                break;
                            case GameNoteAction.WriteUltraBriefAddendumToAnotherAction:
                                BufferOrNull.StartSize60().StartColor( ColorTheme.DataBlue ).AddLang( "Turn" ).Space1x().AddRaw( Note.Int1.ToStringWholeBasic() )
                                    .EndColor().EndSize().Position60();
                                break;
                            case GameNoteAction.GetMatchesSearchString:
                                return Lang.Format1( "StartingTurn", Note.Int1.ToStringWholeBasic() ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                            case GameNoteAction.GetIsStillValid:
                                return true;
                        }
                        break;
                    case "StartedChapter":
                        {
                            MetaChapter chapter = MetaChapterTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    {
                                        BufferOrNull.AddRaw( chapter.GetDisplayName(), ColorTheme.SoftGold );
                                    }
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        //if ( Note.Int1 > 0 )
                                        //{
                                        //    //seconds
                                        //}
                                        //if ( Note.Int2 > 0 )
                                        //{
                                        //    //turn
                                        //}
                                        //if ( Note.Int3 > 0 )
                                        //{
                                        //    //when done
                                        //    DateTime time = new DateTime( Note.Int3 );
                                        //}
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return chapter.GetDisplayName().Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return chapter != null;
                            }
                        }
                        break;
                    case "FinishedWorkCycle":
                        {
                            int workCycle = (int)Note.Int3;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    {
                                        BufferOrNull.AddFormat1( "FinishedWorkCycle", workCycle );
                                    }
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        //if ( Note.Int1 > 0 )
                                        //{
                                        //    //seconds
                                        //}
                                        //if ( Note.Int2 > 0 )
                                        //{
                                        //    //turn
                                        //}
                                        //if ( Note.Int3 > 0 )
                                        //{
                                        //    //when done
                                        //    DateTime time = new DateTime( Note.Int3 );
                                        //}
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return Lang.Format1( "FinishedWorkCycle", workCycle ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return workCycle != 0;
                            }
                        }
                        break;
                    case "ResourceCheat":
                        {
                            ResourceType resourceType = ResourceTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    {
                                        if ( WriteShortIfPossible )
                                            BufferOrNull.StartSize80();
                                        BufferOrNull.AddLangAndAfterLineItemHeader( "CheatPrefix", ColorTheme.DataBlue )
                                            .AddLangAndAfterLineItemHeader( "Granted" ).AddRaw( Note.Int1.ToStringThousandsWhole() ).Space1x();
                                        BufferOrNull.AddSpriteStyled_NoIndent( resourceType.Icon, AdjustedSpriteStyle.InlineLarger1_2 ).AddRaw( resourceType.GetDisplayName() );
                                    }
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "CheatPrefix" );
                                            novel.Main.AddLangAndAfterLineItemHeader( "Granted" ).AddRaw( Note.Int1.ToStringThousandsWhole() ).Space1x()
                                                .AddSpriteStyled_NoIndent( resourceType.Icon, AdjustedSpriteStyle.InlineLarger1_2 ).AddRaw( resourceType.GetDisplayName() ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return resourceType.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "CheatPrefix" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "Granted" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return resourceType != null;
                            }
                        }
                        break;
                    case "GainedResource":
                        {
                            ResourceType resourceType = ResourceTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    {
                                        if ( WriteShortIfPossible )
                                            BufferOrNull.StartSize80();
                                        BufferOrNull.AddFormat1( "PositiveChange", Note.Int1.ToStringThousandsWhole() ).Space1x();
                                        BufferOrNull.AddSpriteStyled_NoIndent( resourceType.Icon, AdjustedSpriteStyle.InlineLarger1_2 ).AddRaw( resourceType.GetDisplayName() );
                                    }
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddFormat1( "PositiveChange", Note.Int1.ToStringThousandsWhole() ).Space1x()
                                                .AddSpriteStyled_NoIndent( resourceType.Icon, AdjustedSpriteStyle.InlineLarger1_2 ).AddRaw( resourceType.GetDisplayName() ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return resourceType.GetMatchesSearchString( SearchString ) ||
                                        Lang.Format1( "PositiveChange", Note.Int1.ToStringThousandsWhole() ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return resourceType != null;
                            }
                        }
                        break;
                    case "MetaResourceCheat":
                        {
                            MetaResourceType resourceType = MetaResourceTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    {
                                        if ( WriteShortIfPossible )
                                            BufferOrNull.StartSize80();
                                        BufferOrNull.AddLangAndAfterLineItemHeader( "CheatPrefix", ColorTheme.DataBlue )
                                            .AddLangAndAfterLineItemHeader( "Granted" ).AddRaw( Note.Int1.ToStringThousandsWhole() ).Space1x();
                                        BufferOrNull.AddSpriteStyled_NoIndent( resourceType.Icon, AdjustedSpriteStyle.InlineLarger1_2 ).AddRaw( resourceType.GetDisplayName() );
                                    }
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "CheatPrefix" );
                                            novel.Main.AddLangAndAfterLineItemHeader( "Granted" ).AddRaw( Note.Int1.ToStringThousandsWhole() ).Space1x()
                                                .AddSpriteStyled_NoIndent( resourceType.Icon, AdjustedSpriteStyle.InlineLarger1_2 ).AddRaw( resourceType.GetDisplayName() ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return resourceType.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "CheatPrefix" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "Granted" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return resourceType != null;
                            }
                        }
                        break;
                    case "InspirationCheat":
                        {
                            ResearchDomain domain = ResearchDomainTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    {
                                        if ( WriteShortIfPossible )
                                            BufferOrNull.StartSize80();
                                        BufferOrNull.AddLangAndAfterLineItemHeader( "CheatPrefix", ColorTheme.DataBlue )
                                            .AddLangAndAfterLineItemHeader( "Granted" ).AddRaw( Note.Int1.ToStringThousandsWhole() ).Space1x();
                                        BufferOrNull.AddSpriteStyled_NoIndent( domain.Icon, AdjustedSpriteStyle.InlineLarger1_2 ).AddRaw( domain.GetDisplayName() );
                                    }
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "CheatPrefix" );
                                            novel.Main.AddLangAndAfterLineItemHeader( "Granted" ).AddRaw( Note.Int1.ToStringThousandsWhole() ).Space1x()
                                                .AddSpriteStyled_NoIndent( domain.Icon, AdjustedSpriteStyle.InlineLarger1_2 ).AddRaw( domain.GetDisplayName() ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return domain.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "CheatPrefix" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "Granted" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return domain != null;
                            }
                        }
                        break;
                    case "MetaChapterCheat":
                        {
                            MetaChapter chapterType = MetaChapterTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    {
                                        if ( WriteShortIfPossible )
                                            BufferOrNull.StartSize80();
                                        BufferOrNull.AddLangAndAfterLineItemHeader( "CheatPrefix", ColorTheme.DataBlue )
                                            .AddLangAndAfterLineItemHeader( "ChangedTo" );
                                        BufferOrNull.AddRaw( chapterType.GetDisplayName() );
                                    }
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "CheatPrefix" );
                                            novel.Main.AddLangAndAfterLineItemHeader( "ChangedTo" )
                                                .AddRaw( chapterType.GetDisplayName() ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return chapterType.GetDisplayName().Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "CheatPrefix" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "ChangedTo" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return chapterType != null;
                            }
                        }
                        break;
                    case "CheatByName":
                        {
                            CheatType cheatType = CheatTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    {
                                        if ( WriteShortIfPossible )
                                            BufferOrNull.StartSize80();
                                        BufferOrNull.AddLangAndAfterLineItemHeader( "CheatPrefix", ColorTheme.DataBlue );
                                        BufferOrNull.AddRaw( cheatType?.GetDisplayName() );
                                    }
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "CheatPrefix" );
                                            novel.Main.AddRaw( cheatType?.GetDisplayName() ).Line();

                                            if ( Note.Int1 > 0 )
                                            {
                                                //seconds
                                            }
                                            if ( Note.Int2 > 0 )
                                            {
                                                //turn
                                            }
                                            if ( Note.Int3 > 0 )
                                            {
                                                //when done
                                                DateTime time = new DateTime( Note.Int3 );
                                            }

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return cheatType?.GetMatchesSearchString( SearchString )??false;
                                case GameNoteAction.GetIsStillValid:
                                    return cheatType != null;
                            }
                        }
                        break;
                    #region CheatMachineUnit
                    case "CheatMachineUnit":
                        {
                            MachineUnitType unitType = MachineUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            string unitName = Note.Name1;
                            ISimMachineUnit unit = SimCommon.AllActorsByID[(int)Note.Int1] as ISimMachineUnit;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "CheatPrefix", ColorTheme.DataBlue );
                                    BufferOrNull.AddRaw( unitName );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddRaw( unitType.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( unit != null )
                                            unit.RenderTooltip( ElementOrNull, clamp, shadowStyle, false, ActorTooltipExtraData.None, TooltipExtraRules.None );
                                        else
                                            unitType.RenderUnitTooltip( ElementOrNull, clamp, shadowStyle, TooltipInstruction.ForExistingObject,
                                                IsFromQuickOnScreenLog ? TooltipExtraText.ClickEitherToClear : TooltipExtraText.None, TooltipExtraRules.None );
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( unit != null )
                                        {
                                            Engine_HotM.SetSelectedActor( unit, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( unit.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unitType.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "CheatPrefix" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        unitName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return unitType != null;
                            }
                        }
                        break;
                    #endregion
                    #region BuiltMachineUnit
                    case "BuiltMachineUnit":
                        {
                            MachineUnitType unitType = MachineUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            string unitName = Note.Name1;
                            ISimMachineUnit unit = SimCommon.AllActorsByID[(int)Note.Int1] as ISimMachineUnit;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( unitType.IsConsideredAndroid ?
                                        "BuiltAndroid" : "BuiltMech", ColorTheme.DataBlue );
                                    BufferOrNull.AddRaw( unitName );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddRaw( unitType.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( unit != null )
                                            unit.RenderTooltip( ElementOrNull, clamp, shadowStyle, false, ActorTooltipExtraData.None, TooltipExtraRules.None );
                                        else
                                            unitType.RenderUnitTooltip( ElementOrNull, clamp, shadowStyle, TooltipInstruction.ForExistingObject,
                                                IsFromQuickOnScreenLog ? TooltipExtraText.ClickEitherToClear : TooltipExtraText.None, TooltipExtraRules.None );
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( unit != null )
                                        {
                                            Engine_HotM.SetSelectedActor( unit, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( unit.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unitType.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( unitType.IsConsideredAndroid ?
                                        "BuiltAndroid" : "BuiltMech" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        unitName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return unitType != null;
                            }
                        }
                        break;
                    #endregion
                    #region ClaimMachineUnit
                    case "ClaimMachineUnit":
                        {
                            MachineUnitType unitType = MachineUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            string unitName = Note.Name1;
                            ISimMachineUnit unit = SimCommon.AllActorsByID[(int)Note.Int1] as ISimMachineUnit;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( unitType.IsConsideredAndroid ?
                                        "CapturedAndroid" : "CapturedMech", ColorTheme.HeaderLighterBlue );
                                    BufferOrNull.AddRaw( unitName );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddRaw( unitType.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( unit != null )
                                            unit.RenderTooltip( ElementOrNull, clamp, shadowStyle, false, ActorTooltipExtraData.None, TooltipExtraRules.None );
                                        else
                                            unitType.RenderUnitTooltip( ElementOrNull, clamp, shadowStyle, TooltipInstruction.ForExistingObject,
                                                IsFromQuickOnScreenLog ? TooltipExtraText.ClickEitherToClear : TooltipExtraText.None, TooltipExtraRules.None );
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( unit != null )
                                        {
                                            Engine_HotM.SetSelectedActor( unit, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( unit.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unitType.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( unitType.IsConsideredAndroid ?
                                        "CapturedAndroid" : "CapturedMech" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        unitName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return unitType != null;
                            }
                        }
                        break;
                    #endregion
                    #region CheatMachineVehicle
                    case "CheatMachineVehicle":
                        {
                            MachineVehicleType vehicleType = MachineVehicleTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            string vehicleName = Note.Name1;
                            ISimMachineVehicle vehicle = World.Forces.GetMachineVehiclesByID()[(int)Note.Int1];

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "CheatPrefix", ColorTheme.DataBlue );
                                    BufferOrNull.AddRaw( vehicleName );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddRaw( vehicleType.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( vehicle != null )
                                            vehicle.RenderTooltip( ElementOrNull, clamp, shadowStyle, false, ActorTooltipExtraData.None, TooltipExtraRules.None );
                                        else
                                            vehicleType.RenderVehicleTooltip( ElementOrNull, clamp, shadowStyle, TooltipInstruction.ForExistingObject,
                                                IsFromQuickOnScreenLog ? TooltipExtraText.ClickEitherToClear : TooltipExtraText.None, TooltipExtraRules.None );
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( vehicle != null )
                                        {
                                            Engine_HotM.SetSelectedActor( vehicle, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( vehicle.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return vehicleType.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "CheatPrefix" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        vehicleName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return vehicleType != null;
                            }
                        }
                        break;
                    #endregion
                    #region BuiltMachineVehicle
                    case "BuiltMachineVehicle":
                        {
                            MachineVehicleType vehicleType = MachineVehicleTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            string vehicleName = Note.Name1;
                            ISimMachineVehicle vehicle = World.Forces.GetMachineVehiclesByID()[(int)Note.Int1];

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "BuiltVehicle", ColorTheme.HeaderLighterBlue );
                                    BufferOrNull.AddRaw( vehicleName );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddRaw( vehicleType.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( vehicle != null )
                                            vehicle.RenderTooltip( ElementOrNull, clamp, shadowStyle, false, ActorTooltipExtraData.None, TooltipExtraRules.None );
                                        else
                                            vehicleType.RenderVehicleTooltip( ElementOrNull, clamp, shadowStyle, TooltipInstruction.ForExistingObject,
                                                IsFromQuickOnScreenLog ? TooltipExtraText.ClickEitherToClear : TooltipExtraText.None, TooltipExtraRules.None );
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( vehicle != null )
                                        {
                                            Engine_HotM.SetSelectedActor( vehicle, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( vehicle.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return vehicleType.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "BuiltVehicle" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        vehicleName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return vehicleType != null;
                            }
                        }
                        break;
                    #endregion
                    #region StoleMachineVehicle
                    case "StoleMachineVehicle":
                        {
                            MachineVehicleType vehicleType = MachineVehicleTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            string vehicleName = Note.Name1;
                            ISimMachineVehicle vehicle = World.Forces.GetMachineVehiclesByID()[(int)Note.Int1];

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "StoleVehicle", ColorTheme.HeaderLighterBlue );
                                    BufferOrNull.AddRaw( vehicleName );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddRaw( vehicleType.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( vehicle != null )
                                            vehicle.RenderTooltip( ElementOrNull, clamp, shadowStyle, false, ActorTooltipExtraData.None, TooltipExtraRules.None );
                                        else
                                            vehicleType.RenderVehicleTooltip( ElementOrNull, clamp, shadowStyle, TooltipInstruction.ForExistingObject,
                                                IsFromQuickOnScreenLog ? TooltipExtraText.ClickEitherToClear : TooltipExtraText.None, TooltipExtraRules.None );
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( vehicle != null )
                                        {
                                            Engine_HotM.SetSelectedActor( vehicle, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( vehicle.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return vehicleType.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "StoleVehicle" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        vehicleName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return vehicleType != null;
                            }
                        }
                        break;
                    #endregion
                    #region CheatNPCUnit
                    case "CheatNPCUnit":
                        {
                            NPCUnitType unitType = NPCUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            NPCCohort cohort = NPCCohortTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );
                            ISimNPCUnit unit = SimCommon.AllActorsByID[(int)Note.Int1] as ISimNPCUnit;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "CheatPrefix", ColorTheme.DataBlue );
                                    BufferOrNull.AddRaw( unitType.GetDisplayName() );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddRaw( cohort.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( unit != null )
                                            unit.RenderTooltip( ElementOrNull, clamp, shadowStyle, false, ActorTooltipExtraData.None, TooltipExtraRules.None );
                                        else
                                            unitType.RenderNPCUnitTooltip( ElementOrNull, clamp, shadowStyle, TooltipInstruction.ForExistingObject,
                                                IsFromQuickOnScreenLog ? TooltipExtraText.ClickEitherToClear : TooltipExtraText.None, TooltipExtraRules.None );
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( unit != null )
                                        {
                                            Engine_HotM.SetSelectedActor( unit, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( unit.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unitType.GetMatchesSearchString( SearchString ) ||
                                        cohort.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "CheatPrefix" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ;
                                case GameNoteAction.GetIsStillValid:
                                    return unitType != null && cohort != null;
                            }
                        }
                        break;
                    #endregion
                    #region CheatRebelWar
                    case "CheatRebelWar":
                        {
                            int rebelMechsSpawned = Convert.ToInt32( Note.ID1 );
                            int corporateMechsSpawned = Convert.ToInt32( Note.ID2 );
                            int rebelsSpawned = (int)Note.Int1;
                            int corporatesSpawned = (int)Note.Int2;
                            int secondsTaken = (int)Note.Int3;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "CheatPrefix", ColorTheme.DataBlue );
                                    BufferOrNull.AddFormatParams( "CheatRebelWarDetails", 
                                        rebelsSpawned.ToStringThousandsWhole(),
                                        corporatesSpawned.ToStringThousandsWhole(),
                                        rebelMechsSpawned.ToStringThousandsWhole(),
                                        corporateMechsSpawned.ToStringThousandsWhole(),
                                        secondsTaken.ToStringThousandsWhole() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "CheatPrefix" );
                                            novel.Main.AddFormatParams( "CheatRebelWarDetails",
                                                rebelsSpawned.ToStringThousandsWhole(),
                                                corporatesSpawned.ToStringThousandsWhole(),
                                                rebelMechsSpawned.ToStringThousandsWhole(),
                                                corporateMechsSpawned.ToStringThousandsWhole(),
                                                secondsTaken.ToStringThousandsWhole() ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return Lang.Get( "CheatRebelWarDetails" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "CheatPrefix" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return true; //always valid
                            }
                        }
                        break;
                    #endregion
                    #region MachineUnitDied
                    case "MachineUnitDied":
                        {
                            MachineUnitType unitType = MachineUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            string unitName = Note.Name1;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "DeathPrefix", ColorTheme.RedOrange3 );
                                    BufferOrNull.AddRaw( unitName );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddRaw( unitType.GetDisplayName()  );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "DeathPrefix" );
                                            novel.Main.AddRaw( unitName ).HyphenSeparator().AddRaw( unitType.GetDisplayName() ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unitType.GetMatchesSearchString( SearchString ) ||
                                        unitName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "DeathPrefix" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return unitType != null;
                            }
                        }
                        break;
                    #endregion
                    #region MachineUnitDiedInCrash
                    case "MachineUnitDiedInCrash":
                        {
                            MachineUnitType unitType = MachineUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            string unitName = Note.Name1;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "CrashDeathPrefix", ColorTheme.RedOrange3 );
                                    BufferOrNull.AddRaw( unitName );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddRaw( unitType.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "CrashDeathPrefix" );
                                            novel.Main.AddRaw( unitName ).HyphenSeparator().AddRaw( unitType.GetDisplayName() )
                                                .Line().AddBoldLangAndAfterLineItemHeader( "VehicleThatCrashed", ColorTheme.DataLabelWhite ).AddRaw( Note.Name2 ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unitType.GetMatchesSearchString( SearchString ) ||
                                        unitName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "CrashDeathPrefix" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "VehicleThatCrashed" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Note.Name2.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return unitType != null;
                            }
                        }
                        break;
                    #endregion
                    #region MachineUnitDiedInExplosion
                    case "MachineUnitDiedInExplosion":
                        {
                            MachineUnitType unitType = MachineUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            string unitName = Note.Name1;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "ExplosionDeathPrefix", ColorTheme.RedOrange3 );
                                    BufferOrNull.AddRaw( unitName );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddRaw( unitType.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "ExplosionDeathPrefix" );
                                            novel.Main.AddRaw( unitName ).HyphenSeparator().AddRaw( unitType.GetDisplayName() ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unitType.GetMatchesSearchString( SearchString ) ||
                                        unitName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "ExplosionDeathPrefix" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ;
                                case GameNoteAction.GetIsStillValid:
                                    return unitType != null;
                            }
                        }
                        break;
                    #endregion
                    #region MachineUnitScrapped
                    case "MachineUnitScrapped":
                        {
                            MachineUnitType unitType = MachineUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            string unitName = Note.Name1;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "ScrappedPrefix", ColorTheme.RedOrange3 );
                                    BufferOrNull.AddRaw( unitName );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddRaw( unitType.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "ScrappedPrefix" );
                                            novel.Main.AddRaw( unitName ).HyphenSeparator().AddRaw( unitType.GetDisplayName() ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unitType.GetMatchesSearchString( SearchString ) ||
                                        unitName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "ScrappedPrefix" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return unitType != null;
                            }
                        }
                        break;
                    #endregion
                    #region MachineUnitReplaced
                    case "MachineUnitReplaced":
                        {
                            MachineUnitType unitType = MachineUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            string unitName = Note.Name1;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "ScrapForReplacementPrefix", ColorTheme.RedOrange3 );
                                    BufferOrNull.AddRaw( unitName );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddRaw( unitType.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "ScrapForReplacementPrefix" );
                                            novel.Main.AddRaw( unitName ).HyphenSeparator().AddRaw( unitType.GetDisplayName() ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unitType.GetMatchesSearchString( SearchString ) ||
                                        unitName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "ScrapForReplacementPrefix" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return unitType != null;
                            }
                        }
                        break;
                    #endregion
                    #region MachineVehicleDied
                    case "MachineVehicleDied":
                        {
                            MachineVehicleType vehicleType = MachineVehicleTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            string unitName = Note.Name1;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "DeathPrefix", ColorTheme.RedOrange3 );
                                    BufferOrNull.AddRaw( unitName );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddRaw( vehicleType.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "DeathPrefix" );
                                            novel.Main.AddRaw( unitName ).HyphenSeparator().AddRaw( vehicleType.GetDisplayName() ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return vehicleType.GetMatchesSearchString( SearchString ) ||
                                        unitName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "DeathPrefix" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return vehicleType != null;
                            }
                        }
                        break;
                    #endregion
                    #region MachineVehicleDiedInExplosion
                    case "MachineVehicleDiedInExplosion":
                        {
                            MachineVehicleType vehicleType = MachineVehicleTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            string unitName = Note.Name1;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "ExplosionDeathPrefix", ColorTheme.RedOrange3 );
                                    BufferOrNull.AddRaw( unitName );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddRaw( vehicleType.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "ExplosionDeathPrefix" );
                                            novel.Main.AddRaw( unitName ).HyphenSeparator().AddRaw( vehicleType.GetDisplayName() ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return vehicleType.GetMatchesSearchString( SearchString ) ||
                                        unitName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "ExplosionDeathPrefix" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return vehicleType != null;
                            }
                        }
                        break;
                    #endregion
                    #region MachineVehicleScrapped
                    case "MachineVehicleScrapped":
                        {
                            MachineVehicleType vehicleType = MachineVehicleTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            string unitName = Note.Name1;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "ScrappedPrefix", ColorTheme.RedOrange3 );
                                    BufferOrNull.AddRaw( unitName );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddRaw( vehicleType.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "ScrappedPrefix" );
                                            novel.Main.AddRaw( unitName ).HyphenSeparator().AddRaw( vehicleType.GetDisplayName() ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return vehicleType.GetMatchesSearchString( SearchString ) ||
                                        unitName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "ScrappedPrefix" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return vehicleType != null;
                            }
                        }
                        break;
                    #endregion
                    #region MachineVehicleReplaced
                    case "MachineVehicleReplaced":
                        {
                            MachineVehicleType vehicleType = MachineVehicleTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            string unitName = Note.Name1;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "ScrapForReplacementPrefix", ColorTheme.RedOrange3 );
                                    BufferOrNull.AddRaw( unitName );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddRaw( vehicleType.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "ScrapForReplacementPrefix" );
                                            novel.Main.AddRaw( unitName ).HyphenSeparator().AddRaw( vehicleType.GetDisplayName() ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return vehicleType.GetMatchesSearchString( SearchString ) ||
                                        unitName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "ScrapForReplacementPrefix" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return vehicleType != null;
                            }
                        }
                        break;
                    #endregion
                    #region NPCUnitDied
                    case "NPCUnitDied":
                        {
                            NPCUnitType unitType = NPCUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            NPCCohort cohort = NPCCohortTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "DeathPrefix", ColorTheme.RedOrange3 );
                                    BufferOrNull.AddRaw( unitType.GetDisplayName() );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddRaw( cohort.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "DeathPrefix" );
                                            novel.Main.AddRaw( unitType.GetDisplayName() ).HyphenSeparator().AddRaw( cohort.GetDisplayName() ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unitType.GetMatchesSearchString( SearchString ) ||
                                        cohort.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "DeathPrefix" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return unitType != null && cohort != null;
                            }
                        }
                        break;
                    #endregion
                    #region NPCUnitDiedInExplosion
                    case "NPCUnitDiedInExplosion":
                        {
                            NPCUnitType unitType = NPCUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            NPCCohort cohort = NPCCohortTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "ExplosionDeathPrefix", ColorTheme.RedOrange3 );
                                    BufferOrNull.AddRaw( unitType.GetDisplayName() );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddRaw( cohort.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "ExplosionDeathPrefix" );
                                            novel.Main.AddRaw( unitType.GetDisplayName() ).HyphenSeparator().AddRaw( cohort.GetDisplayName() ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unitType.GetMatchesSearchString( SearchString ) ||
                                        cohort.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "ExplosionDeathPrefix" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return unitType != null && cohort != null;
                            }
                        }
                        break;
                    #endregion
                    #region NPCUnitDisbandedFromMoraleLoss
                    case "NPCUnitDisbandedFromMoraleLoss":
                        {
                            NPCUnitType unitType = NPCUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            NPCCohort cohort = NPCCohortTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "DisbandedFromMoraleLossPrefix", ColorTheme.RedOrange3 );
                                    BufferOrNull.AddRaw( unitType.GetDisplayName() );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddRaw( cohort.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "DisbandedFromMoraleLossPrefix" );
                                            novel.Main.AddRaw( unitType.GetDisplayName() ).HyphenSeparator().AddRaw( cohort.GetDisplayName() ).Line();
                                            novel.Main.AddLang( "DisbandedFromMoraleLossPrefix_Explanation" ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unitType.GetMatchesSearchString( SearchString ) ||
                                        cohort.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "DisbandedFromMoraleLossPrefix" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "DisbandedFromMoraleLossPrefix_Explanation" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return unitType != null && cohort != null;
                            }
                        }
                        break;
                    #endregion
                    #region NPCUnitFledPhysicalViolence
                    case "NPCUnitFledPhysicalViolence":
                        {
                            NPCUnitType unitType = NPCUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            NPCCohort cohort = NPCCohortTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "FledPhysicalViolencePrefix", ColorTheme.RedOrange3 );
                                    BufferOrNull.AddRaw( unitType.GetDisplayName() );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddRaw( cohort.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "FledPhysicalViolencePrefix" );
                                            novel.Main.AddRaw( unitType.GetDisplayName() ).HyphenSeparator().AddRaw( cohort.GetDisplayName() ).Line();
                                            novel.Main.AddLang( "FledPhysicalViolencePrefix_Explanation" ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unitType.GetMatchesSearchString( SearchString ) ||
                                        cohort.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "FledPhysicalViolencePrefix" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "FledPhysicalViolencePrefix_Explanation" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return unitType != null && cohort != null;
                            }
                        }
                        break;
                    #endregion
                    #region MachineStructureDied
                    case "MachineStructureDied":
                        {
                            MachineStructureType structureType = MachineStructureTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            MachineJob job = MachineJobTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "DeathPrefix", ColorTheme.RedOrange3 );
                                    BufferOrNull.AddRaw( job.GetDisplayName() );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddRaw( structureType.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "DeathPrefix" );
                                            novel.Main.AddRaw( job.GetDisplayName() ).HyphenSeparator().AddRaw( structureType.GetDisplayName() ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return structureType.GetMatchesSearchString( SearchString ) ||
                                        job.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "DeathPrefix" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return structureType != null && job != null;
                            }
                        }
                        break;
                    #endregion
                    #region MachineStructureDiedInExplosion
                    case "MachineStructureDiedInExplosion":
                        {
                            MachineStructureType structureType = MachineStructureTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            MachineJob job = MachineJobTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "ExplosionDeathPrefix", ColorTheme.RedOrange3 );
                                    BufferOrNull.AddRaw( job.GetDisplayName() );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddRaw( structureType.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "ExplosionDeathPrefix" );
                                            novel.Main.AddRaw( job.GetDisplayName() ).HyphenSeparator().AddRaw( structureType.GetDisplayName() ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return structureType.GetMatchesSearchString( SearchString ) ||
                                        job.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "ExplosionDeathPrefix" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return structureType != null && job != null;
                            }
                        }
                        break;
                    #endregion
                    #region MachineStructureScrapped
                    case "MachineStructureScrapped":
                        {
                            MachineStructureType structureType = MachineStructureTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            MachineJob job = MachineJobTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "ScrappedPrefix", ColorTheme.RedOrange3 );
                                    BufferOrNull.AddRaw( job.GetDisplayName() );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddRaw( structureType.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "ScrappedPrefix" );
                                            novel.Main.AddRaw( job.GetDisplayName() ).HyphenSeparator().AddRaw( structureType.GetDisplayName() ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return structureType.GetMatchesSearchString( SearchString ) ||
                                        job.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "ScrappedPrefix" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return structureType != null && job != null;
                            }
                        }
                        break;
                    #endregion
                    #region MachineStructureBuilt
                    case "MachineStructureBuilt":
                        {
                            MachineStructureType structureType = MachineStructureTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            MachineJob jobOrNull = MachineJobTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );

                            string relevantName = jobOrNull != null ? jobOrNull.GetDisplayName() : structureType?.GetDisplayName();

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "BuiltPrefix", ColorTheme.HeaderLighterBlue );
                                    BufferOrNull.AddRaw( relevantName );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "BuiltPrefix" );
                                            novel.Main.AddRaw( relevantName ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return structureType.GetMatchesSearchString( SearchString ) ||
                                        (jobOrNull?.GetMatchesSearchString( SearchString )??false) ||
                                        Lang.Get( "BuiltPrefix" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return structureType != null;
                            }
                        }
                        break;
                    #endregion
                    #region JobFailed
                    case "JobFailed":
                        {
                            MachineJob job = MachineJobTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            BuildingTypeVariant buildingVariant = BuildingTypeVariant.VariantsByID[Note.ID2];
                            ISimBuilding building = World.Buildings.GetAllBuildings()[(int)Note.Int1];

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "JobFailed", ColorTheme.RedOrange3 );
                                    BufferOrNull.AddRaw( job.GetDisplayName() );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddRaw( buildingVariant.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "JobFailed" );
                                            novel.Main.AddRaw( job.GetDisplayName() ).HyphenSeparator().AddRaw( buildingVariant.GetDisplayName() ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( building != null )
                                        {
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( building.GetEffectiveWorldLocationForContainedUnit(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return buildingVariant.GetDisplayName().Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        job.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "JobFailed" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return buildingVariant != null && job != null;
                            }
                        }
                        break;
                    #endregion
                    #region ImmediateInvention
                    case "ImmediateInvention":
                        {
                            Unlock unlock = UnlockTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "NowAvailable", ColorTheme.DataBlue );
                                    IA5Sprite icon = unlock?.GetIcon();
                                    if ( icon != null )
                                        BufferOrNull.AddSpriteStyled_NoIndent( icon, AdjustedSpriteStyle.InlineLarger1_2 ).Space1x();
                                    BufferOrNull.AddRaw( unlock.GetUnlockedDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( unlock != null )
                                        {
                                            unlock.RenderUnlockTooltip( ElementOrNull, clamp, shadowStyle, TooltipInstruction.ForExistingObject,
                                                IsFromQuickOnScreenLog ? TooltipExtraText.ClickEitherToClear : TooltipExtraText.None );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unlock.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "NowAvailable" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return unlock != null;
                            }
                        }
                        break;
                    #endregion
                    #region InventedIdea
                    case "InventedIdea":
                        {
                            Unlock unlock = UnlockTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "Invented", ColorTheme.DataBlue );
                                    IA5Sprite icon = unlock?.GetIcon();
                                    if ( icon != null )
                                        BufferOrNull.AddSpriteStyled_NoIndent( icon, AdjustedSpriteStyle.InlineLarger1_2 ).Space1x();
                                    BufferOrNull.AddRaw( unlock.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( unlock != null )
                                        {
                                            unlock.RenderUnlockTooltip( ElementOrNull, clamp, shadowStyle, TooltipInstruction.ForExistingObject, 
                                                IsFromQuickOnScreenLog ? TooltipExtraText.ClickEitherToClear : TooltipExtraText.None );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unlock.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "Invented" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return unlock != null;
                            }
                        }
                        break;
                    #endregion
                    #region ReadiedIdea
                    case "ReadiedIdea":
                        {
                            Unlock unlock = UnlockTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "ReadiedIdea", ColorTheme.DataBlue );
                                    IA5Sprite icon = unlock?.GetIcon();
                                    if ( icon != null )
                                        BufferOrNull.AddSpriteStyled_NoIndent( icon, AdjustedSpriteStyle.InlineLarger1_2 ).Space1x();
                                    BufferOrNull.AddRaw( unlock.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( unlock != null )
                                        {
                                            unlock.RenderUnlockTooltip( ElementOrNull, clamp, shadowStyle, TooltipInstruction.ForExistingObject,
                                                IsFromQuickOnScreenLog ? TooltipExtraText.ClickEitherToClear : TooltipExtraText.None );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unlock.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "ReadiedIdea" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return unlock != null;
                            }
                        }
                        break;
                    #endregion
                    #region UpgradedInt
                    case "UpgradedInt":
                        {
                            UpgradeInt upgrade = UpgradeIntTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            long timesUpgraded = Note.Int1;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddFormat1AndAfterLineItemHeader( "UpgradeNumberFor", timesUpgraded.ToStringThousandsWhole(), ColorTheme.DataBlue );
                                    IA5Sprite icon = upgrade?.Icon??IconRefs.Upgrade.Icon;
                                    if ( icon != null )
                                        BufferOrNull.AddSpriteStyled_NoIndent( icon, AdjustedSpriteStyle.InlineLarger1_2 ).Space1x();
                                    BufferOrNull.AddRaw( upgrade.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    upgrade?.RenderUpgradeTooltip_ForIncrease( ElementOrNull, clamp, shadowStyle );
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return upgrade.GetMatchesSearchString( SearchString ) ||
                                        Lang.Format1( "UpgradeNumberFor", timesUpgraded.ToStringThousandsWhole() ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return upgrade != null;
                            }
                        }
                        break;
                    #endregion
                    #region UpgradedFloat
                    case "UpgradedFloat":
                        {
                            UpgradeFloat upgrade = UpgradeFloatTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            long timesUpgraded = Note.Int1;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddFormat1AndAfterLineItemHeader( "UpgradeNumberFor", timesUpgraded.ToStringThousandsWhole(), ColorTheme.DataBlue );
                                    IA5Sprite icon = upgrade?.Icon ?? IconRefs.Upgrade.Icon;
                                    if ( icon != null )
                                        BufferOrNull.AddSpriteStyled_NoIndent( icon, AdjustedSpriteStyle.InlineLarger1_2 ).Space1x();
                                    BufferOrNull.AddRaw( upgrade.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    upgrade?.RenderUpgradeTooltip_ForIncrease( ElementOrNull, clamp, shadowStyle );
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return upgrade.GetMatchesSearchString( SearchString ) ||
                                        Lang.Format1( "UpgradeNumberFor", timesUpgraded.ToStringThousandsWhole() ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return upgrade != null;
                            }
                        }
                        break;
                    #endregion
                    #region ResearchDomainInspirationAdded
                    case "ResearchDomainInspirationAdded":
                        {
                            ResearchDomain domain = ResearchDomainTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            int amountAdded = (int)Note.Int1;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddFormat1AndAfterLineItemHeader( "InspirationAdded_ToResearchDomain", amountAdded, ColorTheme.HealingGreen );
                                    IA5Sprite icon = domain?.Icon;
                                    if ( icon != null )
                                        BufferOrNull.AddSpriteStyled_NoIndent( icon, AdjustedSpriteStyle.InlineLarger1_2 ).Space1x();
                                    BufferOrNull.AddRaw( domain.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    domain?.HandleResearchDomainTooltip( ElementOrNull, clamp, shadowStyle, true );
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return domain.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "InspirationAdded_ToResearchDomain" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return domain != null;
                            }
                        }
                        break;
                    #endregion
                    #region ResearchDomainExplored
                    case "ResearchDomainExplored":
                        {
                            ResearchDomain domain = ResearchDomainTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "Explored_ResearchDomain", ColorTheme.DataBlue );
                                    IA5Sprite icon = domain?.Icon;
                                    if ( icon != null )
                                        BufferOrNull.AddSpriteStyled_NoIndent( icon, AdjustedSpriteStyle.InlineLarger1_2 ).Space1x();
                                    BufferOrNull.AddRaw( domain.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    domain?.HandleResearchDomainTooltip( ElementOrNull, clamp, shadowStyle, true );
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return domain.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "Explored_ResearchDomain" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return domain != null;
                            }
                        }
                        break;
                    #endregion
                    #region OtherKeyMessageOpened
                    case "OtherKeyMessageOpened":
                        {
                            OtherKeyMessage keyMessage = OtherKeyMessageTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            OtherKeyMessageOption chosenOption = keyMessage?.OptionsLookup[Note.ID2];

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddRawAndAfterLineItemHeader( keyMessage.ToastLine1.Text, ColorTheme.DataBlue );
                                    IA5Sprite icon = keyMessage?.ToastIcon;
                                    if ( icon != null )
                                        BufferOrNull.AddSpriteStyled_NoIndent( icon, AdjustedSpriteStyle.InlineLarger1_2 ).Space1x();
                                    BufferOrNull.AddRaw( chosenOption.Line1.Text );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    keyMessage?.RenderDetailedLogTooltipText( Note.ID2, ElementOrNull, clamp, shadowStyle );
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return keyMessage.GetMatchesSearchString( SearchString ) ||
                                        chosenOption.GetMatchesSearchString( SearchString );
                                case GameNoteAction.GetIsStillValid:
                                    return keyMessage != null && chosenOption != null;
                            }
                        }
                        break;
                    #endregion
                    #region AchievementEarned
                    case "AchievementEarned":
                        {
                            Achievement achievement = AchievementTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "AchievementEarnedPrefix", ColorTheme.DataBlue );
                                    //IA5Sprite icon = achievement?.Icon_Normal;
                                    //if ( icon != null )
                                    //    BufferOrNull.AddSpriteStyled_NoIndent( icon, AdjustedSpriteStyle.InlineLarger1_2 ).Space1x();
                                    BufferOrNull.AddRaw( achievement?.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    achievement?.RenderAchievementTooltip( Engine_HotM.GameMode == MainGameMode.TheEndOfTime, ElementOrNull, clamp, shadowStyle );
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return achievement.GetMatchesSearchString( SearchString );
                                case GameNoteAction.GetIsStillValid:
                                    return achievement != null;
                            }
                        }
                        break;
                    #endregion
                    #region HandbookEntryAdded
                    case "HandbookEntryAdded":
                        {
                            MachineHandbookEntry handbookEntry = MachineHandbookEntryTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddRaw( handbookEntry.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    handbookEntry?.HandleHandbookTooltip( ElementOrNull, clamp, shadowStyle, false );
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return handbookEntry.GetMatchesSearchString( SearchString );
                                case GameNoteAction.GetIsStillValid:
                                    return handbookEntry != null;
                            }
                        }
                        break;
                    #endregion
                    #region DoomEvent
                    case "DoomEvent":
                        {
                            CityTimelineDoomType doomType = CityTimelineDoomTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            DoomEvent doomEvent = doomType?.DoomMainEventDict[Note.ID2];

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddRaw( doomEvent.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    doomEvent?.WriteTooltip( ElementOrNull, clamp, shadowStyle, false );
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return doomEvent.GetMatchesSearchString( SearchString );
                                case GameNoteAction.GetIsStillValid:
                                    return doomEvent != null;
                            }
                        }
                        break;
                    #endregion
                    #region CityTaskCompleted
                    case "CityTaskCompleted":
                        {
                            CityTask task = CityTaskTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "ChecklistItemComplete", ColorTheme.DataBlue );                                    
                                    BufferOrNull.AddRaw( task.GetDisplayName() );
                                    BufferOrNull.Space3x().AddRaw( task.Line2.Text, ColorTheme.Gray );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    task?.RenderToastTooltipText( ElementOrNull, clamp );
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return task.GetMatchesSearchString( SearchString );
                                case GameNoteAction.GetIsStillValid:
                                    return task != null;
                            }
                        }
                        break;
                    #endregion
                    #region InvestigationStarted
                    case "InvestigationStarted":
                        {
                            InvestigationType investigationType = InvestigationTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            int possibleBuildings = (int)Note.Int1;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "InvestigationStarted", ColorTheme.DataBlue );
                                    BufferOrNull.AddRaw( investigationType.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( investigationType != null )
                                        {
                                            if ( novel.TryStartBasicTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                            {
                                                novel.ShadowStyle = shadowStyle;
                                                novel.Icon = CommonRefs.InvestigationsLens.Icon;
                                                novel.TitleUpperLeft.AddRaw( investigationType.GetDisplayName() );
                                                novel.TitleLowerLeft.AddLang( "InvestigationStarted" );
                                                {
                                                    ArcenDoubleCharacterBuffer buffer = novel.Main;
                                                    buffer.StartColor( ColorTheme.NarrativeColor );

                                                    if ( investigationType.GetDescription().Length > 0 )
                                                        buffer.AddRaw( investigationType.GetDescription() ).Line();
                                                    if ( investigationType.Style.StrategyTip.Text.Length > 0 )
                                                        buffer.AddRaw( investigationType.Style.StrategyTip.Text ).Line();

                                                    buffer.AddBoldLangAndAfterLineItemHeader( "InvestigationInitialPotentialBuildings", ColorTheme.DataLabelWhite )
                                                        .AddRaw( possibleBuildings.ToStringThousandsWhole(), ColorTheme.DataBlue ).Line();
                                                }

                                                if ( !investigationType.Style.MethodDetails.Text.IsEmpty() )
                                                {
                                                    novel.FrameTitle.AddLang( "InvestigationMethodology" );
                                                    novel.FrameBody.AddRaw( investigationType?.Style?.MethodDetails?.Text ?? string.Empty );
                                                }
                                            }
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return investigationType.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "InvestigationStarted").Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return investigationType != null && investigationType.TerritoryControl == null;
                            }
                        }
                        break;
                    #endregion
                    #region InvestigationFinished
                    case "InvestigationFinished":
                        {
                            InvestigationType investigationType = InvestigationTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            int actionsTaken = (int)Note.Int1;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "InvestigationFinished", ColorTheme.DataBlue );
                                    BufferOrNull.AddRaw( investigationType.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( investigationType != null )
                                        {
                                            if ( novel.TryStartBasicTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                            {
                                                novel.Icon = CommonRefs.InvestigationsLens.Icon;
                                                novel.TitleUpperLeft.AddRaw( investigationType.GetDisplayName() );
                                                novel.TitleLowerLeft.AddLang( "InvestigationFinished" );
                                                {
                                                    ArcenDoubleCharacterBuffer buffer = novel.Main;
                                                    buffer.StartColor( ColorTheme.NarrativeColor );

                                                    if ( investigationType.GetDescription().Length > 0 )
                                                        buffer.AddRaw( investigationType.GetDescription() ).Line();
                                                    if ( investigationType.Style.StrategyTip.Text.Length > 0 )
                                                        buffer.AddRaw( investigationType.Style.StrategyTip.Text ).Line();

                                                    buffer.AddBoldLangAndAfterLineItemHeader( "InvestigationActionsTakenToComplete", ColorTheme.DataLabelWhite )
                                                        .AddRaw( actionsTaken.ToStringThousandsWhole(), ColorTheme.DataBlue ).Line();
                                                }

                                                if ( !investigationType.Style.MethodDetails.Text.IsEmpty() )
                                                {
                                                    novel.FrameTitle.AddLang( "InvestigationMethodology" );
                                                    novel.FrameBody.AddRaw( investigationType?.Style?.MethodDetails?.Text ?? string.Empty );
                                                }
                                            }
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return investigationType.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "InvestigationFinished" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return investigationType != null;
                            }
                        }
                        break;
                    #endregion
                    #region TerritoryControlFlagActivated
                    case "TerritoryControlFlagActivated":
                        {
                            TerritoryControlType controlType = TerritoryControlTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();

                                    BufferOrNull.AddLangAndAfterLineItemHeader( "TerritoryControlFlagActivated", ColorTheme.DataBlue );
                                    BufferOrNull.AddRaw( controlType.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( controlType != null )
                                        {
                                            if ( novel.TryStartBasicTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                            {
                                                novel.Icon = controlType.Resource?.Icon;
                                                novel.IconColorHex = controlType.Resource?.IconColorHex ?? string.Empty;
                                                novel.TitleUpperLeft.AddRaw( controlType.GetDisplayName() );
                                                novel.TitleLowerLeft.AddLang( "TerritoryControlFlagActivated" );
                                                novel.Main.StartColor( ColorTheme.NarrativeColor );

                                                if ( controlType.GetDescription().Length > 0 )
                                                    novel.Main.AddRaw( controlType.GetDescription() ).Line();
                                                if ( controlType.StrategyTipOptional.Text.Length > 0 )
                                                    novel.Main.AddRaw( controlType.StrategyTipOptional.Text, ColorTheme.PurpleDim ).Line();
                                            }
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return controlType.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "TerritoryControlFlagActivated" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return controlType != null;
                            }
                        }
                        break;
                    #endregion
                    #region TerritoryControlFlagDisabled
                    case "TerritoryControlFlagDisabled":
                        {
                            TerritoryControlType controlType = TerritoryControlTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();

                                    BufferOrNull.AddLangAndAfterLineItemHeader( "TerritoryControlFlagDisabled", ColorTheme.DataBlue );
                                    BufferOrNull.AddRaw( controlType.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( controlType != null )
                                        {
                                            if ( novel.TryStartBasicTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                            {
                                                novel.Icon = controlType.Resource?.Icon;
                                                novel.IconColorHex = controlType.Resource?.IconColorHex ?? string.Empty;
                                                novel.TitleUpperLeft.AddRaw( controlType.GetDisplayName() );
                                                novel.TitleLowerLeft.AddLang( "TerritoryControlFlagDisabled" );
                                                novel.Main.StartColor( ColorTheme.NarrativeColor );

                                                if ( controlType.GetDescription().Length > 0 )
                                                    novel.Main.AddRaw( controlType.GetDescription() ).Line();
                                                if ( controlType.StrategyTipOptional.Text.Length > 0 )
                                                    novel.Main.AddRaw( controlType.StrategyTipOptional.Text, ColorTheme.PurpleDim ).Line();
                                            }
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return controlType.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "TerritoryControlFlagDisabled" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return controlType != null;
                            }
                        }
                        break;
                    #endregion
                    #region TerritoryControlFlagLost
                    case "TerritoryControlFlagLost":
                        {
                            TerritoryControlType controlType = TerritoryControlTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();

                                    BufferOrNull.AddLangAndAfterLineItemHeader( "TerritoryControlFlagLost", ColorTheme.DataBlue );
                                    BufferOrNull.AddRaw( controlType.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( controlType != null )
                                        {
                                            if ( novel.TryStartBasicTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                            {
                                                novel.Icon = controlType.Resource?.Icon;
                                                novel.IconColorHex = controlType.Resource?.IconColorHex??string.Empty;
                                                novel.TitleUpperLeft.AddRaw( controlType.GetDisplayName() );
                                                novel.TitleLowerLeft.AddLang( "TerritoryControlFlagLost" );
                                                novel.Main.StartColor( ColorTheme.NarrativeColor );

                                                if ( controlType.GetDescription().Length > 0 )
                                                    novel.Main.AddRaw( controlType.GetDescription() ).Line();
                                                if ( controlType.StrategyTipOptional.Text.Length > 0 )
                                                    novel.Main.AddRaw( controlType.StrategyTipOptional.Text, ColorTheme.PurpleDim ).Line();
                                            }
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return controlType.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "TerritoryControlFlagLost" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return controlType != null;
                            }
                        }
                        break;
                    #endregion
                    #region CityConflictStarted
                    case "CityConflictStarted":
                        {
                            CityConflict conflict = CityConflictTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();

                                    BufferOrNull.AddLangAndAfterLineItemHeader( "CityConflictStarted", ColorTheme.DataBlue );
                                    BufferOrNull.AddRaw( conflict.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( conflict != null )
                                            conflict.RenderCityConflictTooltip( ElementOrNull, clamp, false );
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return conflict.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "CityConflictStarted" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return conflict != null;
                            }
                        }
                        break;
                    #endregion
                    #region CityConflictEnded
                    case "CityConflictEnded":
                        {
                            CityConflict conflict = CityConflictTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            int actionsTaken = (int)Note.Int1;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "CityConflictEnded", ColorTheme.DataBlue );
                                    BufferOrNull.AddRaw( conflict.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( conflict != null )
                                            conflict.RenderCityConflictTooltip( ElementOrNull, clamp, false );
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return conflict.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "CityConflictEnded" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return conflict != null;
                            }
                        }
                        break;
                    #endregion
                    #region MurderAndroidForRegistrationStarted
                    case "MurderAndroidForRegistrationStarted":
                        {
                            MachineUnitType unitType = MachineUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            string unitName = Note.Name1;
                            ISimMachineUnit unit = SimCommon.AllActorsByID[(int)Note.Int1] as ISimMachineUnit;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddRawAndAfterLineItemHeader( unitName, ColorTheme.DataBlue );
                                    BufferOrNull.AddLang( "StartingAndroidMurderForRegistration" );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "StartingAndroidMurderForRegistration" );
                                            novel.Main.AddRaw( unitName ).HyphenSeparator()
                                                .AddRaw( (unitType.GetDisplayName()) ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( unit != null )
                                        {
                                            Engine_HotM.SetSelectedActor( unit, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( unit.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unitType.GetMatchesSearchString( SearchString ) ||
                                        unitName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "StartingAndroidMurderForRegistration" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return unitType != null;
                            }
                        }
                        break;
                    #endregion
                    #region MurderedAndroidForRegistration
                    case "MurderedAndroidForRegistration":
                        {
                            MachineUnitType unitType = MachineUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            string unitName = Note.Name1;
                            ISimMachineUnit unit = SimCommon.AllActorsByID[(int)Note.Int1] as ISimMachineUnit;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddRawAndAfterLineItemHeader( unitName, ColorTheme.DataBlue );
                                    BufferOrNull.AddLang( "MurderedAndroidForRegistration" );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddLang( "MurderedAndroidForRegistration_Details", ColorTheme.PurpleDim );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "MurderedAndroidForRegistration" );
                                            novel.Main.AddLang( "MurderedAndroidForRegistration_Details", ColorTheme.NarrativeColor ).Line()
                                                .AddRaw( unitName ).HyphenSeparator().AddRaw( (unitType.GetDisplayName()) ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( unit != null )
                                        {
                                            Engine_HotM.SetSelectedActor( unit, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( unit.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unitType.GetMatchesSearchString( SearchString ) ||
                                        unitName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "MurderedAndroidForRegistration" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "MurderedAndroidForRegistration_Details" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return unitType != null;
                            }
                        }
                        break;
                    #endregion
                    #region WiretapStarted
                    case "WiretapStarted":
                        {
                            MachineUnitType unitType = MachineUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            string unitName = Note.Name1;
                            ISimMachineUnit unit = SimCommon.AllActorsByID[(int)Note.Int1] as ISimMachineUnit;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddRawAndAfterLineItemHeader( unitName, ColorTheme.DataBlue );
                                    BufferOrNull.AddLang( "StartingWiretapToStealWealth" );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "StartingWiretapToStealWealth" );
                                            novel.Main.AddRaw( unitName ).HyphenSeparator()
                                                .AddRaw( (unitType.GetDisplayName()) ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( unit != null )
                                        {
                                            Engine_HotM.SetSelectedActor( unit, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( unit.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unitType.GetMatchesSearchString( SearchString ) ||
                                        unitName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "StartingWiretapToStealWealth" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return unitType != null;
                            }
                        }
                        break;
                    #endregion
                    #region Wiretapped
                    case "Wiretapped":
                        {
                            MachineUnitType unitType = MachineUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            string unitName = Note.Name1;
                            ISimMachineUnit unit = SimCommon.AllActorsByID[(int)Note.Int1] as ISimMachineUnit;
                            int stolenAmount = (int)Note.Int2;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddRawAndAfterLineItemHeader( unitName, ColorTheme.DataBlue );
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "Wiretapped" )
                                        .AddSpriteStyled_NoIndent( ResourceRefs.Wealth.Icon, AdjustedSpriteStyle.InlineLarger1_2).AddRaw( stolenAmount.ToStringThousandsWhole() );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddLang( "Wiretapped_Details", ColorTheme.PurpleDim );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "Wiretapped" );
                                            novel.Main.AddLang( "Wiretapped_Details", ColorTheme.NarrativeColor ).Line()
                                                .AddRaw( unitName ).HyphenSeparator().AddRaw( (unitType.GetDisplayName()) ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( unit != null )
                                        {
                                            Engine_HotM.SetSelectedActor( unit, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( unit.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unitType.GetMatchesSearchString( SearchString ) ||
                                        unitName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "Wiretapped" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "Wiretapped_Details" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return unitType != null;
                            }
                        }
                        break;
                    #endregion
                    #region HideAndSelfRepairStarted
                    case "HideAndSelfRepairStarted":
                        {
                            MachineUnitType unitType = MachineUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            string unitName = Note.Name1;
                            ISimMachineUnit unit = SimCommon.AllActorsByID[(int)Note.Int1] as ISimMachineUnit;
                            int healthAtStart = (int)Note.Int2;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddRawAndAfterLineItemHeader( unitName, ColorTheme.DataBlue );
                                    BufferOrNull.AddLang( "StartingHideAndSelfRepair" ).HyphenSeparator();
                                    BufferOrNull.AddRaw( healthAtStart.ToStringThousandsWhole() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "StartingHideAndSelfRepair" );
                                            novel.Main.AddRaw( unitName ).HyphenSeparator()
                                                .AddRaw( (unitType.GetDisplayName()) ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( unit != null )
                                        {
                                            Engine_HotM.SetSelectedActor( unit, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( unit.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unitType.GetMatchesSearchString( SearchString ) ||
                                        unitName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "StartingHideAndSelfRepair" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return unitType != null;
                            }
                        }
                        break;
                    #endregion
                    #region HideAndSelfRepairComplete
                    case "HideAndSelfRepairComplete":
                        {
                            MachineUnitType unitType = MachineUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            string unitName = Note.Name1;
                            ISimMachineUnit unit = SimCommon.AllActorsByID[(int)Note.Int1] as ISimMachineUnit;
                            int healthAtEnd = (int)Note.Int2;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddRawAndAfterLineItemHeader( unitName, ColorTheme.DataBlue );
                                    BufferOrNull.AddLang( "CompletedHideAndSelfRepair" ).HyphenSeparator();
                                    BufferOrNull.AddRaw( healthAtEnd.ToStringThousandsWhole() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "CompletedHideAndSelfRepair" ).AddRaw( healthAtEnd.ToStringThousandsWhole() );
                                            novel.Main.AddRaw( unitName ).HyphenSeparator().AddRaw( (unitType.GetDisplayName()) ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( unit != null )
                                        {
                                            Engine_HotM.SetSelectedActor( unit, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( unit.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unitType.GetMatchesSearchString( SearchString ) ||
                                        unitName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "CompletedHideAndSelfRepair" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return unitType != null;
                            }
                        }
                        break;
                    #endregion
                    #region GeneralActionOverTimeStarted
                    case "GeneralActionOverTimeStarted":
                        {
                            ActionOverTimeType actionOverTimeType = ActionOverTimeTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            MachineUnitType unitTypeOrNull = MachineUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );
                            MachineVehicleType vehicleTypeOrNull = MachineVehicleTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );
                            ISimMachineActor actor = SimCommon.AllActorsByID[(int)Note.Int1] as ISimMachineActor;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "StartingActionOverTime", ColorTheme.DataBlue );
                                    BufferOrNull.AddRaw( actionOverTimeType?.GetDisplayName()??"???" );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "StartingActionOverTime" ).AddRaw( actionOverTimeType?.GetDisplayName() ?? "???" );

                                            if ( actionOverTimeType != null && !actionOverTimeType.GetDescription().IsEmpty() )
                                                novel.Main.AddRaw( actionOverTimeType.GetDescription(), ColorTheme.NarrativeColor ).Line();

                                            if ( unitTypeOrNull != null)
                                                novel.Main.AddRaw( unitTypeOrNull.GetDisplayName() ).Line();
                                            else if ( vehicleTypeOrNull != null )
                                                novel.Main.AddRaw( vehicleTypeOrNull.GetDisplayName() ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( actor != null )
                                        {
                                            Engine_HotM.SetSelectedActor( actor, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( actor.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return actionOverTimeType.GetDisplayName().Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "StartingActionOverTime" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return actionOverTimeType != null;
                            }
                        }
                        break;
                    #endregion
                    #region GeneralActionOverTimeComplete
                    case "GeneralActionOverTimeComplete":
                        {
                            ActionOverTimeType actionOverTimeType = ActionOverTimeTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            MachineUnitType unitTypeOrNull = MachineUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );
                            MachineVehicleType vehicleTypeOrNull = MachineVehicleTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );
                            ISimMachineActor actor = SimCommon.AllActorsByID[(int)Note.Int1] as ISimMachineActor;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "CompletedActionActionOverTime", ColorTheme.DataBlue );
                                    BufferOrNull.AddRaw( actionOverTimeType?.GetDisplayName() ?? "???" );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "CompletedActionActionOverTime" ).AddRaw( actionOverTimeType?.GetDisplayName() ?? "???" );

                                            if ( actionOverTimeType != null && !actionOverTimeType.GetDescription().IsEmpty() )
                                                novel.Main.AddRaw( actionOverTimeType.GetDescription(), ColorTheme.NarrativeColor ).Line();

                                            if ( unitTypeOrNull != null )
                                                novel.Main.AddRaw( unitTypeOrNull.GetDisplayName() ).Line();
                                            else if ( vehicleTypeOrNull != null )
                                                novel.Main.AddRaw( vehicleTypeOrNull.GetDisplayName() ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( actor != null )
                                        {
                                            Engine_HotM.SetSelectedActor( actor, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( actor.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return actionOverTimeType.GetDisplayName().Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "CompletedActionActionOverTime" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return actionOverTimeType != null;
                            }
                        }
                        break;
                    #endregion
                    #region ExploreSiteStarted
                    case "ExploreSiteStarted":
                        {
                            ExplorationSiteType explorationSiteType = ExplorationSiteTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            string actorName = Note.Name1;
                            ISimMachineActor actor = SimCommon.AllActorsByID[(int)Note.Int1] as ISimMachineActor;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "StartingSiteExploration", ColorTheme.DataBlue );
                                    BufferOrNull.AddSpriteStyled_NoIndent( explorationSiteType?.Icon, AdjustedSpriteStyle.InlineLarger1_2 ).Space1x();
                                    BufferOrNull.AddRaw( explorationSiteType?.GetDisplayName() ?? "???" );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartBasicTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.Icon = explorationSiteType?.Icon;
                                            novel.TitleUpperLeft.AddRaw( explorationSiteType?.GetDisplayName() ?? "???" );
                                            novel.TitleLowerLeft.AddLang( "StartingSiteExploration" );
                                            novel.Main.AddRaw( actorName );

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( actor != null )
                                        {
                                            Engine_HotM.SetSelectedActor( actor, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( actor.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return explorationSiteType.GetDisplayName().Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "StartingSiteExploration" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return explorationSiteType != null;
                            }
                        }
                        break;
                    #endregion
                    #region ExploreSiteCompleted
                    case "ExploreSiteCompleted":
                        {
                            ExplorationSiteType explorationSiteType = ExplorationSiteTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            string actorName = Note.Name1;
                            ISimMachineActor actor = SimCommon.AllActorsByID[(int)Note.Int1] as ISimMachineActor;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "CompletedSiteExploration", ColorTheme.DataBlue );
                                    BufferOrNull.AddSpriteStyled_NoIndent( explorationSiteType?.Icon, AdjustedSpriteStyle.InlineLarger1_2 ).Space1x();
                                    BufferOrNull.AddRaw( explorationSiteType?.GetDisplayName() ?? "???" );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartBasicTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.Icon = explorationSiteType?.Icon;
                                            novel.TitleUpperLeft.AddRaw( explorationSiteType?.GetDisplayName() ?? "???" );
                                            novel.TitleLowerLeft.AddLang( "CompletedSiteExploration" );
                                            novel.Main.AddRaw( actorName );

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( actor != null )
                                        {
                                            Engine_HotM.SetSelectedActor( actor, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( actor.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return explorationSiteType.GetDisplayName().Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "CompletedSiteExploration" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return explorationSiteType != null;
                            }
                        }
                        break;
                    #endregion
                    #region StructuralEngineeringComplete
                    case "StructuralEngineeringComplete":
                        {
                            MachineUnitType unitType = MachineUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            MachineStructureType structureType = MachineStructureTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );
                            MachineJob job = MachineJobTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID3 );
                            string unitName = Note.Name1;
                            ISimMachineUnit unit = SimCommon.AllActorsByID[(int)Note.Int1] as ISimMachineUnit;
                            //int healthAtEnd = (int)Note.Int2;
                            string repairedItem = (job != null ? job.GetDisplayName() : (structureType?.GetDisplayName() ?? "null"));

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddRawAndAfterLineItemHeader( unitName, ColorTheme.DataBlue );
                                    BufferOrNull.AddLang( "CompletedStructuralEngineering" ).HyphenSeparator();
                                    BufferOrNull.AddRaw( repairedItem );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "CompletedStructuralEngineering" ).AddRaw( repairedItem );
                                            novel.Main.AddLang( "MurderedAndroidForRegistration_Details", ColorTheme.NarrativeColor ).Line()
                                                .AddRaw( unitName ).HyphenSeparator().AddRaw( (unitType.GetDisplayName()) ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( unit != null )
                                        {
                                            Engine_HotM.SetSelectedActor( unit, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( unit.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unitType.GetMatchesSearchString( SearchString ) ||
                                        unitName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        structureType.GetMatchesSearchString( SearchString ) ||
                                        (job?.GetMatchesSearchString( SearchString )??false) ||
                                        Lang.Get( "CompletedStructuralEngineering" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return unitType != null && structureType != null && job != null;
                            }
                        }
                        break;
                    #endregion
                    #region WallripStarted
                    case "WallripStarted":
                        {
                            MachineUnitType unitType = MachineUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            ResourceType scavengedResource = ResourceTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );
                            string buildingName = Note.Name1;
                            ISimMachineUnit unit = SimCommon.AllActorsByID[(int)Note.Int1] as ISimMachineUnit;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddRawAndAfterLineItemHeader( unitType?.GetDisplayName(), ColorTheme.DataBlue );
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "StartingWallrip" ).AddRaw( scavengedResource?.GetDisplayName(), ColorTheme.CategorySelectedBlue );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            //unitType?.GetDisplayName()
                                            novel.TitleUpperLeft.AddLang( "StartingWallrip" );
                                            novel.Main.AddRaw( buildingName ).HyphenSeparator()
                                                .AddRaw( scavengedResource?.GetDisplayName() ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( unit != null )
                                        {
                                            Engine_HotM.SetSelectedActor( unit, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( unit.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unitType.GetMatchesSearchString( SearchString ) ||
                                        scavengedResource.GetMatchesSearchString( SearchString ) ||
                                        buildingName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "StartingWallrip" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return unitType != null && scavengedResource != null;
                            }
                        }
                        break;
                    #endregion
                    #region WallripComplete
                    case "WallripComplete":
                        {
                            MachineUnitType unitType = MachineUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            ResourceType scavengedResource = ResourceTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );
                            string buildingName = Note.Name1;
                            ISimMachineUnit unit = SimCommon.AllActorsByID[(int)Note.Int1] as ISimMachineUnit;
                            int totalGathered = (int)Note.Int2;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddRawAndAfterLineItemHeader( unitType?.GetDisplayName(), ColorTheme.DataBlue );
                                    BufferOrNull.AddLang( "CompletedWallrip" ).HyphenSeparator();
                                    BufferOrNull.AddRaw( totalGathered.ToStringThousandsWhole(), ColorTheme.CategorySelectedBlue ).Space1x().AddRaw( scavengedResource?.GetDisplayName(), ColorTheme.CategorySelectedBlue );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            //unitType?.GetDisplayName()
                                            novel.TitleUpperLeft.AddLang( "CompletedWallrip" );
                                            novel.Main.AddRaw( buildingName ).HyphenSeparator()
                                                .AddRaw( totalGathered.ToStringThousandsWhole(), ColorTheme.DataBlue ).Space1x().AddRaw( scavengedResource?.GetDisplayName() ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( unit != null )
                                        {
                                            Engine_HotM.SetSelectedActor( unit, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( unit.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unitType.GetMatchesSearchString( SearchString ) ||
                                        scavengedResource.GetMatchesSearchString( SearchString ) ||
                                        buildingName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "CompletedWallrip" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return unitType != null && scavengedResource != null;
                            }
                        }
                        break;
                    #endregion
                    #region QuietLootingStarted
                    case "QuietLootingStarted":
                        {
                            MachineUnitType unitType = MachineUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            ResourceType scavengedResource = ResourceTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );
                            string buildingName = Note.Name1;
                            ISimMachineUnit unit = SimCommon.AllActorsByID[(int)Note.Int1] as ISimMachineUnit;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddRawAndAfterLineItemHeader( unitType?.GetDisplayName(), ColorTheme.DataBlue );
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "StartingQuietLooting" ).AddRaw( scavengedResource?.GetDisplayName(), ColorTheme.CategorySelectedBlue );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            //unitType?.GetDisplayName()
                                            novel.TitleUpperLeft.AddLang( "StartingQuietLooting" );
                                            novel.Main.AddRaw( buildingName ).HyphenSeparator()
                                                .AddRaw( scavengedResource?.GetDisplayName() ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( unit != null )
                                        {
                                            Engine_HotM.SetSelectedActor( unit, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( unit.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unitType.GetMatchesSearchString( SearchString ) ||
                                        scavengedResource.GetMatchesSearchString( SearchString ) ||
                                        buildingName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "StartingQuietLooting" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return unitType != null && scavengedResource != null;
                            }
                        }
                        break;
                    #endregion
                    #region QuietLootingComplete
                    case "QuietLootingComplete":
                        {
                            MachineUnitType unitType = MachineUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            ResourceType scavengedResource = ResourceTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );
                            string buildingName = Note.Name1;
                            ISimMachineUnit unit = SimCommon.AllActorsByID[(int)Note.Int1] as ISimMachineUnit;
                            int totalGathered = (int)Note.Int2;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddRawAndAfterLineItemHeader( unitType?.GetDisplayName(), ColorTheme.DataBlue );
                                    BufferOrNull.AddLang( "CompletedQuietLooting" ).HyphenSeparator();
                                    BufferOrNull.AddRaw( totalGathered.ToStringThousandsWhole(), ColorTheme.CategorySelectedBlue ).Space1x().AddRaw( scavengedResource?.GetDisplayName(), ColorTheme.CategorySelectedBlue );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            //unitType?.GetDisplayName()
                                            novel.TitleUpperLeft.AddLang( "CompletedQuietLooting" );
                                            novel.Main.AddRaw( buildingName ).HyphenSeparator()
                                                .AddRaw( totalGathered.ToStringThousandsWhole(), ColorTheme.DataBlue ).Space1x().AddRaw( scavengedResource?.GetDisplayName() ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( unit != null )
                                        {
                                            Engine_HotM.SetSelectedActor( unit, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( unit.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unitType.GetMatchesSearchString( SearchString ) ||
                                        scavengedResource.GetMatchesSearchString( SearchString ) ||
                                        buildingName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "CompletedQuietLooting" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return unitType != null && scavengedResource != null;
                            }
                        }
                        break;
                    #endregion
                    #region KeyContactFlagTripped
                    case "KeyContactFlagTripped":
                        {
                            MachineUnitType unitType = MachineUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            KeyContact keyContact = KeyContactTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );
                            KeyContactFlag flag = keyContact?.FlagsDict[Note.ID3];
                            ISimMachineUnit unit = SimCommon.AllActorsByID[(int)Note.Int1] as ISimMachineUnit;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddRawAndAfterLineItemHeader( keyContact?.GetDisplayName(), ColorTheme.CategorySelectedBlue ).AddRaw( flag?.DisplayName?.Text );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddRawAndAfterLineItemHeader( keyContact?.GetDisplayName(), ColorTheme.CategorySelectedBlue )
                                                .AddRaw( flag?.DisplayName?.Text );
                                            novel.Main.AddRaw( (unitType?.GetDisplayName()) ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( unit != null )
                                        {
                                            Engine_HotM.SetSelectedActor( unit, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( unit.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unitType.GetMatchesSearchString( SearchString ) ||
                                        keyContact.GetMatchesSearchString( SearchString ) ||
                                        flag.DisplayName.Text.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return unitType != null && keyContact != null && flag != null;
                            }
                        }
                        break;
                    #endregion
                    #region ColdBlood
                    case "ColdBlood":
                        {
                            MachineUnitType unitType = MachineUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            string unitName = Note.Name1;
                            ISimMachineUnit unit = SimCommon.AllActorsByID[(int)Note.Int1] as ISimMachineUnit;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddRawAndAfterLineItemHeader( unitName, ColorTheme.DataBlue );
                                    BufferOrNull.AddLang( "MurderedRandomHumans" );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddLang( "MurderedRandomHumans_Details", ColorTheme.PurpleDim );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "MurderedRandomHumans" );
                                            novel.Main.AddLang( "MurderedRandomHumans_Details", ColorTheme.NarrativeColor ).Line()
                                                .AddRaw( unitName ).HyphenSeparator().AddRaw( (unitType.GetDisplayName()) ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( unit != null )
                                        {
                                            Engine_HotM.SetSelectedActor( unit, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( unit.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unitType.GetMatchesSearchString( SearchString ) ||
                                        unitName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "MurderedRandomHumans" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "MurderedRandomHumans_Details" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return unitType != null;
                            }
                        }
                        break;
                    #endregion
                    #region WorkByRepairSpiders
                    case "WorkByRepairSpiders":
                        {
                            ISimBuilding building = World.Buildings.GetAllBuildings()[(int)Note.Int3];
                            int totalStructureRepairs = (int)Note.Int1;
                            int totalUnitRepairs = (int)Note.Int2;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddFormat2( "WorkByRepairSpiders_Brief", totalStructureRepairs, totalUnitRepairs );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddFormat2( "WorkByRepairSpiders_Longer", totalStructureRepairs, totalUnitRepairs ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( building != null )
                                        {
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( building.GetEffectiveWorldLocationForContainedUnit(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return building.GetDisplayName().Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "WorkByRepairSpiders_Brief" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "WorkByRepairSpiders_Longer" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return building != null;
                            }
                        }
                        break;
                    #endregion
                    #region WorkByRepairCrabs
                    case "WorkByRepairCrabs":
                        {
                            ISimBuilding building = World.Buildings.GetAllBuildings()[(int)Note.Int3];
                            int totalStructureRepairs = (int)Note.Int1;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddFormat1( "WorkByRepairCrabsA_Brief", totalStructureRepairs );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddFormat1( "WorkByRepairCrabs_Longer", totalStructureRepairs ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( building != null )
                                        {
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( building.GetEffectiveWorldLocationForContainedUnit(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return building.GetDisplayName().Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "WorkByRepairCrabsA_Brief" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "WorkByRepairCrabs_Longer" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return building != null;
                            }
                        }
                        break;
                    #endregion
                    #region UnitFlamethrowerNearby
                    case "UnitFlamethrowerNearby":
                        {
                            int unitCount = (int)Note.Int1;
                            int buildingCount = (int)Note.Int2;
                            int unhomedCount = (int)Note.Int3;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddFormat3( "FlamethrowerNearby_Brief", unitCount, buildingCount, unhomedCount );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddFormat3( "FlamethrowerNearby_Brief", unitCount, buildingCount, unhomedCount );

                                            novel.Main.AddFormat3( "FlamethrowerNearby_Longer", unitCount, buildingCount, unhomedCount ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return Lang.Get( "FlamethrowerNearby_Brief" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "FlamethrowerNearby_Longer" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return true;
                            }
                        }
                        break;
                    #endregion
                    #region UnitRepairNearby
                    case "UnitRepairNearby":
                        {
                            MachineUnitType unitType = MachineUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            string unitName = Note.Name1;
                            int totalStructureRepairs = (int)Note.Int1;
                            int totalUnitRepairs = (int)Note.Int2;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddFormat3( "RepairNearby_Brief", totalStructureRepairs, totalUnitRepairs,
                                        unitName.IsEmpty() ? (unitType?.GetDisplayName()??string.Empty): unitName );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddFormat3( "RepairNearby_Longer", totalStructureRepairs, totalUnitRepairs,
                                                unitName.IsEmpty() ? (unitType?.GetDisplayName() ?? string.Empty) : unitName ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return unitType.GetMatchesSearchString( SearchString ) ||
                                        unitName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "RepairNearby_Brief" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "RepairNearby_Longer" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return unitType != null;
                            }
                        }
                        break;
                    #endregion
                    #region VehicleRepairNearby
                    case "VehicleRepairNearby":
                        {
                            MachineVehicleType vehicleType = MachineVehicleTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            string vehicleName = Note.Name1;
                            int totalStructureRepairs = (int)Note.Int1;
                            int totalUnitRepairs = (int)Note.Int2;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddFormat3( "RepairNearby_Brief", totalStructureRepairs, totalUnitRepairs,
                                        vehicleName.IsEmpty() ? (vehicleType?.GetDisplayName() ?? string.Empty) : vehicleName );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddFormat3( "RepairNearby_Longer", totalStructureRepairs, totalUnitRepairs,
                                            vehicleName.IsEmpty() ? (vehicleType?.GetDisplayName() ?? string.Empty) : vehicleName ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return vehicleType.GetMatchesSearchString( SearchString ) ||
                                        vehicleName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "RepairNearby_Brief" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "RepairNearby_Longer" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return vehicleType != null;
                            }
                        }
                        break;
                    #endregion
                    #region WorkByContrabandJammer
                    case "WorkByContrabandJammer":
                        {
                            ISimBuilding building = World.Buildings.GetAllBuildings()[(int)Note.Int3];
                            int totalStructureHidingDone = (int)Note.Int1;
                            int totalStructuresHiddenHelped = (int)Note.Int2;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddFormat1( "WorkByContrabandJammer_Brief", totalStructureHidingDone.ToStringThousandsWhole() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddFormat2( "WorkByContrabandJammer_Longer", totalStructureHidingDone.ToStringThousandsWhole(), totalStructuresHiddenHelped.ToString() ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( building != null )
                                        {
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( building.GetEffectiveWorldLocationForContainedUnit(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return building.GetDisplayName().Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "WorkByContrabandJammer_Brief" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "WorkByContrabandJammer_Longer" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return building != null;
                            }
                        }
                        break;
                    #endregion
                    #region NPCUnitAttackNPCUnit
                    case "NPCUnitAttackNPCUnit":
                        {
                            NPCUnitType attackerType = NPCUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            NPCUnitType targetType = NPCUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );
                            NPCCohort attackerCohort = NPCCohortTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID3 );
                            NPCCohort targetCohort = NPCCohortTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID4 );
                            ISimNPCUnit attacker = SimCommon.AllActorsByID[(int)Note.Int1] as ISimNPCUnit;
                            int damageDone = (int)Note.Int3;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "AttackBrief", ColorTheme.DataBlue );
                                    BufferOrNull.AddSpriteStyled_NoIndent( ActorRefs.ActorHP.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange2 );
                                    BufferOrNull.AddRaw( damageDone.ToStringThousandsWhole(), ColorTheme.RedOrange2 ).Space3x();

                                    BufferOrNull.AddFormat2( "AttackUnitVsUnit", attackerType.GetDisplayName(), targetType.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddFormat2( "AttackTarget", attackerType.GetDisplayName(), targetType.GetDisplayName() );

                                            novel.Main.AddFormat2( "AttackGroupVsGroup", attackerCohort.GetDisplayName(), targetCohort.GetDisplayName() ).Line()
                                                .AddBoldLangAndAfterLineItemHeader( "DamageDone", ColorTheme.DataLabelWhite )
                                                .AddSpriteStyled_NoIndent( ActorRefs.ActorHP.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange2 )
                                                .AddRaw( damageDone.ToStringThousandsWhole(), ColorTheme.RedOrange2 ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( attacker != null )
                                        {
                                            Engine_HotM.SetSelectedActor( attacker, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( attacker.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return attackerType.GetMatchesSearchString( SearchString ) ||
                                        targetType.GetMatchesSearchString( SearchString ) ||
                                        attackerCohort.GetMatchesSearchString( SearchString ) ||
                                        targetCohort.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "AttackBrief" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Format2( "AttackTarget", attackerType.GetDisplayName(), targetType.GetDisplayName() ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return attackerType != null && targetType != null && attackerCohort != null && targetCohort != null;
                            }
                        }
                        break;
                    #endregion
                    #region NPCUnitAttackMachineUnit
                    case "NPCUnitAttackMachineUnit":
                        {
                            NPCUnitType attackerType = NPCUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            MachineUnitType targetType = MachineUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );
                            NPCCohort attackerCohort = NPCCohortTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID3 );
                            ISimNPCUnit attacker = SimCommon.AllActorsByID[(int)Note.Int1] as ISimNPCUnit;
                            int damageDone = (int)Note.Int3;
                            string targetName = Note.Name1;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "AttackBrief", ColorTheme.DataBlue );
                                    BufferOrNull.AddSpriteStyled_NoIndent( ActorRefs.ActorHP.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange2 );
                                    BufferOrNull.AddRaw( damageDone.ToStringThousandsWhole(), ColorTheme.RedOrange2 ).Space3x();

                                    BufferOrNull.AddFormat2( "AttackUnitVsUnit", attackerType.GetDisplayName(), targetName );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddFormat2( "AttackTarget", attackerType.GetDisplayName(), targetName );

                                            novel.Main.AddFormat2( "AttackGroupVsGroup", attackerCohort.GetDisplayName(), targetType.GetDisplayName() ).Line()
                                                .AddBoldLangAndAfterLineItemHeader( "DamageDone", ColorTheme.DataLabelWhite )
                                                .AddSpriteStyled_NoIndent( ActorRefs.ActorHP.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange2 )
                                                .AddRaw( damageDone.ToStringThousandsWhole(), ColorTheme.RedOrange2 ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( attacker != null )
                                        {
                                            Engine_HotM.SetSelectedActor( attacker, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( attacker.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return attackerType.GetMatchesSearchString( SearchString ) ||
                                        targetType.GetMatchesSearchString( SearchString ) ||
                                        attackerCohort.GetMatchesSearchString( SearchString ) ||
                                        targetName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "AttackBrief" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Format2( "AttackTarget", attackerType.GetDisplayName(), targetName ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return attackerType != null && targetType != null && attackerCohort != null;
                            }
                        }
                        break;
                    #endregion
                    #region NPCUnitAttackMachineVehicle
                    case "NPCUnitAttackMachineVehicle":
                        {
                            NPCUnitType attackerType = NPCUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            MachineVehicleType targetType = MachineVehicleTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );
                            NPCCohort attackerCohort = NPCCohortTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID3 );
                            ISimNPCUnit attacker = SimCommon.AllActorsByID[(int)Note.Int1] as ISimNPCUnit;
                            int damageDone = (int)Note.Int3;
                            string targetName = Note.Name1;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "AttackBrief", ColorTheme.DataBlue );
                                    BufferOrNull.AddSpriteStyled_NoIndent( ActorRefs.ActorHP.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange2 );
                                    BufferOrNull.AddRaw( damageDone.ToStringThousandsWhole(), ColorTheme.RedOrange2 ).Space3x();

                                    BufferOrNull.AddFormat2( "AttackUnitVsUnit", attackerType.GetDisplayName(), targetName );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddFormat2( "AttackTarget", attackerType.GetDisplayName(), targetName );

                                            novel.Main.AddFormat2( "AttackGroupVsGroup", attackerCohort.GetDisplayName(), targetType.GetDisplayName() ).Line()
                                                .AddBoldLangAndAfterLineItemHeader( "DamageDone", ColorTheme.DataLabelWhite )
                                                .AddSpriteStyled_NoIndent( ActorRefs.ActorHP.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange2 )
                                                .AddRaw( damageDone.ToStringThousandsWhole(), ColorTheme.RedOrange2 ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( attacker != null )
                                        {
                                            Engine_HotM.SetSelectedActor( attacker, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( attacker.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return attackerType.GetMatchesSearchString( SearchString ) ||
                                        targetType.GetMatchesSearchString( SearchString ) ||
                                        attackerCohort.GetMatchesSearchString( SearchString ) ||
                                        targetName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "AttackBrief" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Format2( "AttackTarget", attackerType.GetDisplayName(), targetName ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return attackerType != null && targetType != null && attackerCohort != null;
                            }
                        }
                        break;
                    #endregion
                    #region NPCUnitAttackMachineStructure
                    case "NPCUnitAttackMachineStructure":
                        {
                            NPCUnitType attackerType = NPCUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            MachineStructureType targetType = MachineStructureTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );
                            MachineJob targetJob = MachineJobTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID4 );
                            NPCCohort attackerCohort = NPCCohortTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID3 );
                            ISimNPCUnit attacker = SimCommon.AllActorsByID[(int)Note.Int1] as ISimNPCUnit;
                            int damageDone = (int)Note.Int3;

                            string attackedItem = (targetJob != null ? targetJob.GetDisplayName() : (targetType?.GetDisplayName() ?? "null"));

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "AttackBrief", ColorTheme.DataBlue );
                                    BufferOrNull.AddSpriteStyled_NoIndent( ActorRefs.ActorHP.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange2 );
                                    BufferOrNull.AddRaw( damageDone.ToStringThousandsWhole(), ColorTheme.RedOrange2 ).Space3x();

                                    BufferOrNull.AddFormat2( "AttackUnitVsUnit", attackerType.GetDisplayName(), attackedItem );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddFormat2( "AttackTarget", attackerType.GetDisplayName(), attackedItem );

                                            novel.Main.AddFormat2( "AttackGroupVsGroup", attackerCohort.GetDisplayName(), attackedItem ).Line()
                                                .AddBoldLangAndAfterLineItemHeader( "DamageDone", ColorTheme.DataLabelWhite )
                                                .AddSpriteStyled_NoIndent( ActorRefs.ActorHP.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange2 )
                                                .AddRaw( damageDone.ToStringThousandsWhole(), ColorTheme.RedOrange2 ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( attacker != null )
                                        {
                                            Engine_HotM.SetSelectedActor( attacker, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( attacker.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return attackerType.GetMatchesSearchString( SearchString ) ||
                                        targetType.GetMatchesSearchString( SearchString ) ||
                                        attackerCohort.GetMatchesSearchString( SearchString ) ||
                                        (targetJob?.GetMatchesSearchString( SearchString )??false) ||
                                        attackedItem.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "AttackBrief" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Format2( "AttackTarget", attackerType.GetDisplayName(), attackedItem ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return attackerType != null && targetType != null && attackerCohort != null;
                            }
                        }
                        break;
                    #endregion
                    #region MachineUnitAttackNPCUnit
                    case "MachineUnitAttackNPCUnit":
                        {
                            MachineUnitType attackerType = MachineUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            NPCUnitType targetType = NPCUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );
                            NPCCohort targetCohort = NPCCohortTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID3 );
                            ISimMachineUnit attacker = SimCommon.AllActorsByID[(int)Note.Int1] as ISimMachineUnit;
                            int damageDone = (int)Note.Int3;
                            string attackerName = Note.Name1;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "AttackBrief", ColorTheme.DataBlue );
                                    BufferOrNull.AddSpriteStyled_NoIndent( ActorRefs.ActorHP.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange2 );
                                    BufferOrNull.AddRaw( damageDone.ToStringThousandsWhole(), ColorTheme.RedOrange2 ).Space3x();

                                    BufferOrNull.AddFormat2( "AttackUnitVsUnit", attackerName, targetType.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddFormat2( "AttackTarget", attackerName, targetType.GetDisplayName() );

                                            novel.Main.AddFormat2( "AttackGroupVsGroup", attackerType.GetDisplayName(), targetCohort?.GetDisplayName() ?? "???" ).Line()
                                                .AddBoldLangAndAfterLineItemHeader( "DamageDone", ColorTheme.DataLabelWhite )
                                                .AddSpriteStyled_NoIndent( ActorRefs.ActorHP.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange2 )
                                                .AddRaw( damageDone.ToStringThousandsWhole(), ColorTheme.RedOrange2 ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( attacker != null )
                                        {
                                            Engine_HotM.SetSelectedActor( attacker, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( attacker.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return attackerType.GetMatchesSearchString( SearchString ) ||
                                        targetType.GetMatchesSearchString( SearchString ) ||
                                        targetCohort.GetMatchesSearchString( SearchString ) ||
                                        attackerName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "AttackBrief" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Format2( "AttackTarget", attackerName, targetType.GetDisplayName() ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return attackerType != null && targetType != null && targetCohort != null;
                            }
                        }
                        break;
                    #endregion
                    #region MachineVehicleAttackNPCUnit
                    case "MachineVehicleAttackNPCUnit":
                        {
                            MachineVehicleType attackerType = MachineVehicleTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            NPCUnitType targetType = NPCUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );
                            NPCCohort targetCohort = NPCCohortTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID3 );
                            ISimMachineVehicle attacker = World.Forces.GetMachineVehiclesByID()[(int)Note.Int1];
                            int damageDone = (int)Note.Int3;
                            string attackerName = Note.Name1;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "AttackBrief", ColorTheme.DataBlue );
                                    BufferOrNull.AddSpriteStyled_NoIndent( ActorRefs.ActorHP.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange2 );
                                    BufferOrNull.AddRaw( damageDone.ToStringThousandsWhole(), ColorTheme.RedOrange2 ).Space3x();

                                    BufferOrNull.AddFormat2( "AttackUnitVsUnit", attackerName, targetType.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddFormat2( "AttackTarget", attackerName, targetType.GetDisplayName() );

                                            novel.Main.AddFormat2( "AttackGroupVsGroup", attackerType.GetDisplayName(), targetCohort?.GetDisplayName() ?? "???" ).Line()
                                                .AddBoldLangAndAfterLineItemHeader( "DamageDone", ColorTheme.DataLabelWhite )
                                                .AddSpriteStyled_NoIndent( ActorRefs.ActorHP.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange2 )
                                                .AddRaw( damageDone.ToStringThousandsWhole(), ColorTheme.RedOrange2 ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( attacker != null )
                                        {
                                            Engine_HotM.SetSelectedActor( attacker, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( attacker.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return attackerType.GetMatchesSearchString( SearchString ) ||
                                        targetType.GetMatchesSearchString( SearchString ) ||
                                        targetCohort.GetMatchesSearchString( SearchString ) ||
                                        attackerName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "AttackBrief" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Format2( "AttackTarget", attackerName, targetType.GetDisplayName() ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return attackerType != null && targetType != null && targetCohort != null;
                            }
                        }
                        break;
                    #endregion
                    #region NPCUnitAttackMoraleOfNPCUnit
                    case "NPCUnitAttackMoraleOfNPCUnit":
                        {
                            NPCUnitType attackerType = NPCUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            NPCUnitType targetType = NPCUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );
                            NPCCohort attackerCohort = NPCCohortTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID3 );
                            NPCCohort targetCohort = NPCCohortTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID4 );
                            ISimNPCUnit attacker = SimCommon.AllActorsByID[(int)Note.Int1] as ISimNPCUnit;
                            int moraleRemaining = (int)Note.Int2;
                            int moraleDamageDone = (int)Note.Int3;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "AttackMoraleBrief", ColorTheme.DataBlue );
                                    BufferOrNull.AddSpriteStyled_NoIndent( ActorRefs.UnitMorale.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange2 );
                                    BufferOrNull.AddRaw( moraleDamageDone.ToStringThousandsWhole(), ColorTheme.RedOrange2 ).Space3x();

                                    BufferOrNull.AddFormat2( "AttackUnitVsUnit", attackerType.GetDisplayName(), targetType.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddFormat2( "AttackTargetMorale", attackerType.GetDisplayName(), targetType.GetDisplayName() );

                                            novel.Main.AddFormat2( "AttackGroupVsGroup", attackerCohort.GetDisplayName(), targetCohort.GetDisplayName() ).Line()
                                                .AddBoldLangAndAfterLineItemHeader( "MoraleDamageDone", ColorTheme.DataLabelWhite )
                                                .AddSpriteStyled_NoIndent( ActorRefs.UnitMorale.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange2 )
                                                .AddRaw( moraleDamageDone.ToStringThousandsWhole(), ColorTheme.RedOrange2 ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( attacker != null )
                                        {
                                            Engine_HotM.SetSelectedActor( attacker, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( attacker.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return attackerType.GetMatchesSearchString( SearchString ) ||
                                        targetType.GetMatchesSearchString( SearchString ) ||
                                        attackerCohort.GetMatchesSearchString( SearchString ) ||
                                        targetCohort.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "AttackMoraleBrief" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Format2( "AttackTargetMorale", attackerType.GetDisplayName(), targetType.GetDisplayName() ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return attackerType != null && targetType != null && attackerCohort != null && targetCohort != null;
                            }
                        }
                        break;
                    #endregion
                    #region MachineUnitAttackMoraleOfNPCUnit
                    case "MachineUnitAttackMoraleOfNPCUnit":
                        {
                            MachineUnitType attackerType = MachineUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            NPCUnitType targetType = NPCUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );
                            NPCCohort targetCohort = NPCCohortTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID3 );
                            ISimMachineUnit attacker = SimCommon.AllActorsByID[(int)Note.Int1] as ISimMachineUnit;
                            int moraleRemaining = (int)Note.Int2;
                            int moraleDamageDone = (int)Note.Int3;
                            string attackerName = Note.Name1;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "AttackMoraleBrief", ColorTheme.DataBlue );
                                    BufferOrNull.AddSpriteStyled_NoIndent( ActorRefs.UnitMorale.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange2 );
                                    BufferOrNull.AddRaw( moraleDamageDone.ToStringThousandsWhole(), ColorTheme.RedOrange2 ).Space3x();

                                    BufferOrNull.AddFormat2( "AttackUnitVsUnit", attackerName, targetType.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddFormat2( "AttackTargetMorale", attackerName, targetType.GetDisplayName() );

                                            novel.Main.AddFormat2( "AttackGroupVsGroup", attackerType.GetDisplayName(), targetCohort?.GetDisplayName() ?? "???" ).Line()
                                                .AddBoldLangAndAfterLineItemHeader( "MoraleDamageDone", ColorTheme.DataLabelWhite )
                                                .AddSpriteStyled_NoIndent( ActorRefs.UnitMorale.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange2 )
                                                .AddRaw( moraleDamageDone.ToStringThousandsWhole(), ColorTheme.RedOrange2 ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( attacker != null )
                                        {
                                            Engine_HotM.SetSelectedActor( attacker, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( attacker.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return attackerType.GetMatchesSearchString( SearchString ) ||
                                        targetType.GetMatchesSearchString( SearchString ) ||
                                        targetCohort.GetMatchesSearchString( SearchString ) ||
                                        attackerName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "AttackMoraleBrief" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Format2( "AttackTargetMorale", attackerName, targetType.GetDisplayName() ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return attackerType != null && targetType != null && targetCohort != null;
                            }
                        }
                        break;
                    #endregion
                    #region MachineVehicleAttackMoraleOfNPCUnit
                    case "MachineVehicleAttackMoraleOfNPCUnit":
                        {
                            MachineVehicleType attackerType = MachineVehicleTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            NPCUnitType targetType = NPCUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );
                            NPCCohort targetCohort = NPCCohortTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID3 );
                            ISimMachineVehicle attacker = World.Forces.GetMachineVehiclesByID()[(int)Note.Int1];
                            int moraleRemaining = (int)Note.Int2;
                            int moraleDamageDone = (int)Note.Int3;
                            string attackerName = Note.Name1;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "AttackMoraleBrief", ColorTheme.DataBlue );
                                    BufferOrNull.AddSpriteStyled_NoIndent( ActorRefs.UnitMorale.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange2 );
                                    BufferOrNull.AddRaw( moraleDamageDone.ToStringThousandsWhole(), ColorTheme.RedOrange2 ).Space3x();

                                    BufferOrNull.AddFormat2( "AttackUnitVsUnit", attackerName, targetType.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddFormat2( "AttackTargetMorale", attackerName, targetType.GetDisplayName() );

                                            novel.Main.AddFormat2( "AttackGroupVsGroup", attackerType.GetDisplayName(), targetCohort?.GetDisplayName() ?? "???" ).Line()
                                                .AddBoldLangAndAfterLineItemHeader( "MoraleDamageDone", ColorTheme.DataLabelWhite )
                                                .AddSpriteStyled_NoIndent( ActorRefs.UnitMorale.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange2 )
                                                .AddRaw( moraleDamageDone.ToStringThousandsWhole(), ColorTheme.RedOrange2 ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( attacker != null )
                                        {
                                            Engine_HotM.SetSelectedActor( attacker, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( attacker.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return attackerType.GetMatchesSearchString( SearchString ) ||
                                        targetType.GetMatchesSearchString( SearchString ) ||
                                        targetCohort.GetMatchesSearchString( SearchString ) ||
                                        attackerName.Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "AttackMoraleBrief" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Format2( "AttackTargetMorale", attackerName, targetType.GetDisplayName() ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return attackerType != null && targetType != null && targetCohort != null;
                            }
                        }
                        break;
                    #endregion
                    #region NPCObjectiveComplete
                    case "NPCObjectiveComplete":
                        {
                            NPCUnitObjective objective = NPCUnitObjectiveTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            NPCUnitType npcUnitType = NPCUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );
                            NPCCohort npcUnitCohort = NPCCohortTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID3 );
                            ISimNPCUnit npcUnit = SimCommon.AllActorsByID[(int)Note.Int1] as ISimNPCUnit;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "NPCObjective", ColorTheme.DataBlue );
                                    BufferOrNull.AddRaw( objective.GetDisplayName(), objective?.PopupColorHex ?? string.Empty );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "NPCObjectiveComplete" ).AddRaw( objective.GetDisplayName() );
                                            novel.Main.AddRaw( npcUnitType.GetDisplayName() ).HyphenSeparator()
                                                .AddRaw( (npcUnitCohort.GetDisplayName()) ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( npcUnit != null )
                                        {
                                            Engine_HotM.SetSelectedActor( npcUnit, true, false, false );
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( npcUnit.GetDrawLocation(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return objective.GetMatchesSearchString( SearchString ) ||
                                        npcUnitType.GetMatchesSearchString( SearchString ) ||
                                        npcUnitCohort.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "NPCObjective" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase ) ||
                                        Lang.Get( "NPCObjectiveComplete" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return objective != null && npcUnitType != null && npcUnitCohort != null;
                            }
                        }
                        break;
                    #endregion
                    #region ProjectStarted
                    case "ProjectStarted":
                        {
                            MachineProject project = MachineProjectTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "ProjectStarted", ColorTheme.HeaderLighterBlue );
                                    BufferOrNull.AddRaw( project.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    project?.RenderProjectTooltip( ElementOrNull, clamp, shadowStyle, false, false, false );
                                    break;
                                case GameNoteAction.LeftClick:
                                    if ( project != null )
                                    {
                                        if ( project.IsMinorProject )
                                        {
                                            ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                                            return false;
                                        }
                                        SimCommon.RewardProvider = ProjectOutcomeHandler.Start( project );
                                        Window_RewardWindow.Instance.Open();
                                        return true;
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return project.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "ProjectStarted" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return project != null;
                            }
                        }
                        break;
                    #endregion
                    #region ProjectSuccess
                    case "ProjectSuccess":
                        {
                            MachineProject project = MachineProjectTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "ProjectCompleted", ColorTheme.HeaderLighterBlue );
                                    BufferOrNull.AddRaw( project.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    project?.RenderProjectTooltip( ElementOrNull, clamp, shadowStyle, false, false, false );
                                    break;
                                case GameNoteAction.LeftClick:
                                    if ( project != null )
                                    {
                                        if ( project.IsMinorProject )
                                        {
                                            ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                                            return false;
                                        }
                                        SimCommon.RewardProvider = ProjectOutcomeHandler.Start( project );
                                        Window_RewardWindow.Instance.Open();
                                        return true;
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return project.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "ProjectCompleted" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return project != null;
                            }
                        }
                        break;
                    #endregion
                    #region ProjectFailure
                    case "ProjectFailure":
                        {
                            MachineProject project = MachineProjectTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "ProjectFailed", ColorTheme.RedLess );
                                    BufferOrNull.AddRaw( project.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    project?.RenderProjectTooltip( ElementOrNull, clamp, shadowStyle, false, false, false );
                                    break;
                                case GameNoteAction.LeftClick:
                                    if ( project != null )
                                    {
                                        if ( project.IsMinorProject )
                                        {
                                            ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                                            return false;
                                        }
                                        SimCommon.RewardProvider = ProjectOutcomeHandler.Start( project );
                                        Window_RewardWindow.Instance.Open();
                                        return true;
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return project.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "ProjectFailed" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return project != null;
                            }
                        }
                        break;
                    #endregion
                    #region NPCMissionStarted
                    case "NPCMissionStarted":
                        {
                            NPCMission mission = NPCMissionTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            ISimBuilding building = World.Buildings.GetAllBuildings()[(int)Note.Int1];

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "MissionStarted", ColorTheme.HeaderLighterBlue );
                                    BufferOrNull.AddRaw( mission.GetDisplayName() );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddRaw( building.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        mission?.RenderNPCMissionTooltip( ElementOrNull, clamp, shadowStyle );

                                        //if ( building != null )
                                        //{
                                        //    BufferOrNull.Line().AddLangAndAfterLineItemHeader( "Location", ColorTheme.DataBlue );
                                        //    BufferOrNull.AddRaw( building.GetDisplayName() ?? "???" );
                                        //}
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( building != null )
                                        {
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( building.GetEffectiveWorldLocationForContainedUnit(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return mission.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "MissionStarted" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return mission != null;
                            }
                        }
                        break;
                    #endregion
                    #region NPCMissionCompleted
                    case "NPCMissionCompleted":
                        {
                            NPCMission mission = NPCMissionTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            ISimBuilding building = World.Buildings.GetAllBuildings()[(int)Note.Int1];

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "MissionCompleted", ColorTheme.HeaderLighterBlue );
                                    BufferOrNull.AddRaw( mission.GetDisplayName() );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddRaw( building.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        mission?.RenderNPCMissionTooltip( ElementOrNull, clamp, shadowStyle );

                                        //if ( building != null )
                                        //{
                                        //    BufferOrNull.Line().AddLangAndAfterLineItemHeader( "Location", ColorTheme.DataBlue );
                                        //    BufferOrNull.AddRaw( building.GetDisplayName() ?? "???" );
                                        //}
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( building != null )
                                        {
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( building.GetEffectiveWorldLocationForContainedUnit(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return mission.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "MissionCompleted" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return mission != null;
                            }
                        }
                        break;
                    #endregion
                    #region NPCMissionExpired
                    case "NPCMissionExpired":
                        {
                            NPCMission mission = NPCMissionTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            ISimBuilding building = World.Buildings.GetAllBuildings()[(int)Note.Int1];

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "MissionExpired", ColorTheme.HeaderLighterBlue );
                                    BufferOrNull.AddRaw( mission.GetDisplayName() );
                                    if ( !WriteShortIfPossible )
                                        BufferOrNull.HyphenSeparator().AddRaw( building.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        mission?.RenderNPCMissionTooltip( ElementOrNull, clamp, shadowStyle );

                                        //if ( building != null )
                                        //{
                                        //    BufferOrNull.Line().AddLangAndAfterLineItemHeader( "Location", ColorTheme.DataBlue );
                                        //    BufferOrNull.AddRaw( building.GetDisplayName() ?? "???" );
                                        //}
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( building != null )
                                        {
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( building.GetEffectiveWorldLocationForContainedUnit(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return mission.GetMatchesSearchString( SearchString ) ||
                                        Lang.Get( "MissionExpired" ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return mission != null;
                            }
                        }
                        break;
                    #endregion
                    #region EventComplete
                    case "EventComplete":
                        {
                            NPCEvent cEvent = NPCEventTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            string eventCompleteType = Note.ID2;
                            string colorHex = ColorTheme.HeaderLighterBlue;
                            switch ( eventCompleteType )
                            {
                                case "EventPartialSuccess":
                                    colorHex = ColorTheme.Settings_CurrentDiffersFromDefaultYellow;
                                    break;
                                case "EventFailed":
                                    colorHex = ColorTheme.RedOrange3;
                                    break;
                            }
                            EventChoice choice = cEvent?.ChoicesLookup[Note.ID3];
                            EventChoiceResult result = choice?.AllPossibleResults[Note.ID4];

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( eventCompleteType, colorHex );
                                    BufferOrNull.AddRaw( cEvent.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:

                                    if ( novel.TryStartBasicTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.DoubleHeader ) )
                                    {
                                        novel.TitleUpperLeft.AddRaw( cEvent.GetDisplayName() );
                                        novel.TitleLowerLeft.AddLang( eventCompleteType, colorHex );
                                        novel.Icon = cEvent.Icon;

                                        {
                                            ArcenDoubleCharacterBuffer buffer = novel.Main;
                                            if ( cEvent != null )
                                            {
                                                if ( cEvent.GetDescription().Length > 0 )
                                                    buffer.AddRaw( cEvent.GetDescription(), ColorTheme.NarrativeColor ).Line();
                                                if ( cEvent.StrategyTip.Text.Length > 0 )
                                                    buffer.AddRaw( cEvent.StrategyTip.Text, ColorTheme.PurpleDim ).Line();
                                            }
                                        }

                                        if ( choice != null )
                                        {
                                            novel.FrameTitle.AddLangAndAfterLineItemHeader( "Choice", ColorTheme.DataBlue )
                                                .AddRaw( choice.DisplayName.Text );

                                            ArcenDoubleCharacterBuffer buffer = novel.FrameBody;

                                            if ( choice.Description.Text.Length > 0 )
                                                buffer.AddRaw( choice.Description.Text, ColorTheme.NarrativeColor ).Line();
                                            if ( choice.StrategyTip.Text.Length > 0 )
                                                buffer.AddRaw( choice.StrategyTip.Text, ColorTheme.PurpleDim ).Line();

                                            if ( result != null && result.ResultMessage.Text.Length > 0 )
                                            {
                                                if ( !buffer.GetIsEmpty() )
                                                    buffer.Line();
                                                buffer.AddBoldLangAndAfterLineItemHeader( "Choice_Result", ColorTheme.DataLabelWhite );
                                                buffer.AddRaw( result.ResultMessage.Text, ColorTheme.NarrativeColor ).Line();
                                                if ( result.ResultMessageSecondaryNote.Text.Length > 0 )
                                                    buffer.AddRaw( result.ResultMessageSecondaryNote.Text, ColorTheme.NarrativeColor ).Line();
                                            }

                                            if ( buffer.GetIsEmpty() )
                                                buffer.AddRaw( "-", ColorTheme.NarrativeColor );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return cEvent.GetMatchesSearchString( SearchString ) ||
                                        choice.GetMatchesSearchString( SearchString ) ||
                                        result.GetMatchesSearchString( SearchString );
                                case GameNoteAction.GetIsStillValid:
                                    return cEvent != null && choice != null && result != null;
                            }
                        }
                        break;
                    #endregion
                    #region NPCDialogChoice
                    case "NPCDialogChoice":
                        {
                            NPCDialog dialog = NPCDialogTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            NPCDialogChoice choice = dialog?.Choices[Note.ID2];
                            ISimBuilding building = World.Buildings.GetAllBuildings()[(int)Note.Int1];
                            NPCCohort fromCohort = NPCCohortTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID4 );
                            NPCManagedUnit managedUnit = NPCManagedUnit.ManagedUnitsByID[Note.ID3];

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( dialog == null )
                                    {
                                        BufferOrNull.AddNeverTranslated( "[Unknown Dialog]", true );
                                        break;
                                    }
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddRawAndAfterLineItemHeader( choice?.DisplayName?.Text ?? "???", ColorTheme.RustLighter );
                                    BufferOrNull.AddRaw( dialog.GetDisplayName());
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( dialog != null )
                                        {
                                            if ( novel.TryStartBasicTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.DoubleHeader ) )
                                            {
                                                novel.TitleUpperLeft.AddRaw( dialog.GetDisplayName() );
                                                novel.TitleLowerLeft.AddLangAndAfterLineItemHeader( "SpeakingWith" ).AddRaw( managedUnit.ToSpawn.GetDisplayName() )
                                                    .Space2x().AddLangAndAfterLineItemHeader( "FromCohort" ).AddRaw( fromCohort.GetDisplayName() );
                                                novel.Icon = IconRefs.DialogActionIcon.Icon;

                                                {
                                                    ArcenDoubleCharacterBuffer buffer = novel.Main;
                                                    if ( dialog.GetDescription().Length > 0 )
                                                        buffer.AddRaw( dialog.GetDescription(), ColorTheme.NarrativeColor ).Line();
                                                    if ( dialog.StrategyTip.Text.Length > 0 )
                                                        buffer.AddRaw( dialog.StrategyTip.Text, ColorTheme.PurpleDim ).Line();
                                                }

                                                if ( choice != null )
                                                {
                                                    novel.FrameTitle.AddLangAndAfterLineItemHeader( "Choice", ColorTheme.DataBlue )
                                                        .AddRaw( choice.DisplayName.Text );

                                                    ArcenDoubleCharacterBuffer buffer = novel.FrameBody;

                                                    if ( choice.Description.Text.Length > 0 )
                                                        buffer.AddRaw( choice.Description.Text, ColorTheme.NarrativeColor ).Line();
                                                    if ( choice.StrategyTip.Text.Length > 0 )
                                                        buffer.AddRaw( choice.StrategyTip.Text, ColorTheme.PurpleDim ).Line();

                                                    if ( buffer.GetIsEmpty() )
                                                        buffer.AddRaw( "-", ColorTheme.NarrativeColor );
                                                }
                                            }
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( building != null )
                                        {
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( building.GetEffectiveWorldLocationForContainedUnit(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return dialog.GetMatchesSearchString( SearchString ) ||
                                        choice.GetMatchesSearchString( SearchString ) ||
                                        fromCohort.GetMatchesSearchString( SearchString ) ||
                                        managedUnit.ToSpawn.GetMatchesSearchString( SearchString);
                                case GameNoteAction.GetIsStillValid:
                                    return dialog != null && choice != null && fromCohort != null && managedUnit?.ToSpawn != null;
                            }
                        }
                        break;
                    #endregion
                    #region NPCDialogChoiceDebateWon
                    case "NPCDialogChoiceDebateWon":
                        {
                            NPCDialog dialog = NPCDialogTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            NPCDialogChoice choice = dialog?.Choices[Note.ID2];
                            ISimBuilding building = World.Buildings.GetAllBuildings()[(int)Note.Int1];
                            NPCCohort fromCohort = NPCCohortTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID4 );
                            NPCManagedUnit managedUnit = NPCManagedUnit.ManagedUnitsByID[Note.ID3];

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( dialog == null )
                                    {
                                        BufferOrNull.AddNeverTranslated( "[Unknown Dialog]", true );
                                        break;
                                    }
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddRawAndAfterLineItemHeader( choice?.DisplayName?.Text ?? "???", ColorTheme.RustLighter );
                                    BufferOrNull.AddRaw( dialog.GetDisplayName() );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( dialog != null )
                                        {
                                            if ( novel.TryStartBasicTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.DoubleHeader ) )
                                            {
                                                novel.TitleUpperLeft.AddRaw( dialog.GetDisplayName() );
                                                novel.TitleLowerLeft.AddLangAndAfterLineItemHeader( "Debating" ).AddRaw( managedUnit.ToSpawn.GetDisplayName() )
                                                    .Space2x().AddLangAndAfterLineItemHeader( "FromCohort" ).AddRaw( fromCohort.GetDisplayName() );
                                                novel.Icon = IconRefs.DialogActionIcon.Icon;

                                                {
                                                    ArcenDoubleCharacterBuffer buffer = novel.Main;
                                                    if ( dialog.GetDescription().Length > 0 )
                                                        buffer.AddRaw( dialog.GetDescription(), ColorTheme.NarrativeColor ).Line();
                                                    if ( dialog.StrategyTip.Text.Length > 0 )
                                                        buffer.AddRaw( dialog.StrategyTip.Text, ColorTheme.PurpleDim ).Line();
                                                }

                                                if ( choice != null )
                                                {
                                                    novel.FrameTitle.AddLangAndAfterLineItemHeader( "Choice", ColorTheme.DataBlue )
                                                        .AddRaw( choice.DisplayName.Text );

                                                    ArcenDoubleCharacterBuffer buffer = novel.FrameBody;

                                                    if ( choice.Description.Text.Length > 0 )
                                                        buffer.AddRaw( choice.Description.Text, ColorTheme.NarrativeColor ).Line();
                                                    if ( choice.StrategyTip.Text.Length > 0 )
                                                        buffer.AddRaw( choice.StrategyTip.Text, ColorTheme.PurpleDim ).Line();

                                                    buffer.AddLang( "Debate_Win_LogNotice", ColorTheme.HeaderGoldMoreRich ).Line();

                                                    if ( buffer.GetIsEmpty() )
                                                        buffer.AddRaw( "-", ColorTheme.NarrativeColor );
                                                }
                                            }
                                        }
                                    }
                                    break;
                                case GameNoteAction.LeftClick:
                                    {
                                        if ( building != null )
                                        {
                                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( building.GetEffectiveWorldLocationForContainedUnit(), false );
                                            return true;
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return dialog.GetMatchesSearchString( SearchString ) ||
                                        choice.GetMatchesSearchString( SearchString ) ||
                                        fromCohort.GetMatchesSearchString( SearchString ) ||
                                        managedUnit.ToSpawn.GetMatchesSearchString( SearchString );
                                case GameNoteAction.GetIsStillValid:
                                    return dialog != null && choice != null && fromCohort != null && managedUnit?.ToSpawn != null;
                            }
                        }
                        break;
                    #endregion
                    #region CountdownStarted
                    case "CountdownStarted":
                        {
                            OtherCountdownType countdown = OtherCountdownTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    countdown?.WriteTextPart( BufferOrNull, false, false );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    countdown?.RenderCountdownTooltip( ElementOrNull, clamp, shadowStyle, IsFromQuickOnScreenLog ? TooltipExtraText.ClickEitherToClear : TooltipExtraText.None );
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return countdown.GetMatchesSearchString( SearchString );
                                case GameNoteAction.GetIsStillValid:
                                    return countdown != null;
                            }
                        }
                        break;
                    #endregion
                    #region CountdownEnded
                    case "CountdownEnded":
                        {
                            OtherCountdownType countdown = OtherCountdownTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    countdown?.WriteTextPart( BufferOrNull, false, false );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    countdown?.RenderCountdownTooltip( ElementOrNull, clamp, shadowStyle, IsFromQuickOnScreenLog ? TooltipExtraText.ClickEitherToClear : TooltipExtraText.None );
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return countdown.GetMatchesSearchString( SearchString );
                                case GameNoteAction.GetIsStillValid:
                                    return countdown != null;
                            }
                        }
                        break;
                    #endregion
                    #region ExtractedResource
                    case "ExtractedResource":
                        {
                            ResourceType resource = ResourceTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            NPCUnitType unitType = NPCUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );
                            int gained = (int)Note.Int1;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddFormat3( "ResourceExtractedFrom", gained, resource.GetDisplayName(), unitType.GetDisplayName(), ColorTheme.HeaderLighterBlue );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddFormat3( "ResourceExtractedFrom", gained, resource.GetDisplayName(), unitType.GetDisplayName() ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return resource.GetMatchesSearchString( SearchString ) ||
                                        unitType.GetMatchesSearchString( SearchString ) ||
                                        Lang.Format3( "ResourceExtractedFrom", gained, resource.GetDisplayName(), unitType.GetDisplayName() ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return resource != null && unitType != null;
                            }
                        }
                        break;
                    #endregion
                    #region GainedResourceFromKill
                    case "GainedResourceFromKill":
                        {
                            ResourceType resource = ResourceTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID1 );
                            NPCUnitType unitType = NPCUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( Note.ID2 );
                            int gained = (int)Note.Int1;

                            switch ( Action )
                            {
                                case GameNoteAction.WriteText:
                                    if ( WriteShortIfPossible )
                                        BufferOrNull.StartSize80();
                                    BufferOrNull.AddFormat3( "ResourceGainedFromKill", gained, resource.GetDisplayName(), unitType.GetDisplayName(), ColorTheme.HeaderLighterBlue );
                                    break;
                                case GameNoteAction.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Note.Instruction ), ElementOrNull, clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddFormat3( "ResourceGainedFromKill", gained, resource.GetDisplayName(), unitType.GetDisplayName() ).Line();

                                            if ( IsFromQuickOnScreenLog )
                                                TooltipExtraText.ClickEitherToClear.RenderText( novel.Main );
                                        }
                                    }
                                    break;
                                case GameNoteAction.GetMatchesSearchString:
                                    return resource.GetMatchesSearchString( SearchString ) ||
                                        unitType.GetMatchesSearchString( SearchString ) ||
                                        Lang.Format3( "ResourceGainedFromKill", gained, resource.GetDisplayName(), unitType.GetDisplayName() ).Contains( SearchString, StringComparison.InvariantCultureIgnoreCase );
                                case GameNoteAction.GetIsStillValid:
                                    return resource != null && unitType != null;
                            }
                        }
                        break;
                    #endregion
                    default:
                        if ( !Note.Instruction.HasShownHandlerMissingError )
                        {
                            Note.Instruction.HasShownHandlerMissingError = true;
                            ArcenDebugging.LogSingleLine( "Notes_SimpleBasics: No handler is set up for '" + Note.Instruction.ID + "'!", Verbosity.ShowAsError );
                        }
                        break;
                }

                switch ( Action )
                {
                    case GameNoteAction.GetMatchesSearchString:
                        ArcenDebugging.LogSingleLine( "Notes_SimpleBasics: No handler is set up for GetMatchesSearchString for '" + Note.Instruction.ID + "'!", Verbosity.ShowAsError );
                        return false;
                    case GameNoteAction.GetIsStillValid:
                        ArcenDebugging.LogSingleLine( "Notes_SimpleBasics: No handler is set up for GetIsStillValid for '" + Note.Instruction.ID + "'!", Verbosity.ShowAsError );
                        return false;
                }
            }
            catch ( Exception e )
            {
                if ( !Note.Instruction.HasShownCaughtError )
                {
                    Note.Instruction.HasShownCaughtError = true;
                    ArcenDebugging.LogSingleLine( "Notes_SimpleBasics: Error in '" + Note.Instruction.ID + "': " + e, Verbosity.ShowAsError );
                }
            }

            return false;
        }

        public IGameNote DeserializeNote( int PriorityLevel, NoteInstruction Instruction, DeserializedObjectLayer Data )
        {
            SimpleNote simpleNote = SimpleNote.Create( PriorityLevel, Instruction, string.Empty, string.Empty, string.Empty, string.Empty, 0, 0, 0, string.Empty, string.Empty, string.Empty, 0 );
            simpleNote.Deserialize( Data );
            return simpleNote;
        }
    }
}
