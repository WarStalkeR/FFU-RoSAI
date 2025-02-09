using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public static class ChecklistHelper
    {
        #region RenderObjectiveBox
        public static void RenderObjectiveBox( string ObjectiveBase, string ObjectiveExplanation,
            string StrategyTip, bool IsCompleted )
        {
            if ( IsCompleted || ObjectiveBase.IsEmpty() )
                return;

            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            Window_TaskStack.AddTaskBox( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
            {
                if ( ObjectiveBase.IsEmpty() )
                    return;

                switch ( Action )
                {
                    case UIAction.GetTextToShowFromVolatile:
                        {
                            bool isHovered = element.LastHadMouseWithin;
                            Window_TaskStack.bTaskBox icon = (Window_TaskStack.bTaskBox)element.Controller;
                            icon.SetBoxStyle( AlertColor.Normal, isHovered );

                            ExtraData.Buffer.AddLang( ObjectiveBase );
                        }
                        break;
                    case UIAction.HandleMouseover:
                        {
                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( "ChecklistObjective", ObjectiveBase ), null, SideClamp.LeftOrRight, TooltipNovelWidth.Simple,
                                TooltipExtraText.None, TooltipExtraRules.MustBeToLeftOfTaskStack ) )
                            {
                                novel.TitleUpperLeft.AddLang( ObjectiveBase );
                                novel.Main.AddLang( ObjectiveExplanation, ColorTheme.NarrativeColor );
                                if ( StrategyTip != null && StrategyTip.Length > 0 )
                                    novel.Main.Line().AddLang( StrategyTip, ColorTheme.PurpleDim );
                            }
                        }
                        break;
                    case UIAction.OnClick:
                        {
                        }
                        break;
                }
            } );
        }
        #endregion

        #region RenderObjectiveBoxFormat1
        public static void RenderObjectiveBoxFormat1( string ObjectiveBase, string ObjectiveExplanation,
            string StrategyTip, bool IsCompleted, object Item1 )
        {
            if ( IsCompleted || ObjectiveBase.IsEmpty() )
                return;

            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            Window_TaskStack.AddTaskBox( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
            {
                if ( ObjectiveBase.IsEmpty() )
                    return;

                switch ( Action )
                {
                    case UIAction.GetTextToShowFromVolatile:
                        {
                            bool isHovered = element.LastHadMouseWithin;
                            Window_TaskStack.bTaskBox icon = (Window_TaskStack.bTaskBox)element.Controller;
                            icon.SetBoxStyle( AlertColor.Normal, isHovered );

                            ExtraData.Buffer.AddFormat1( ObjectiveBase, Item1 );
                        }
                        break;
                    case UIAction.HandleMouseover:
                        {
                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( "ChecklistObjective", ObjectiveBase ), null, SideClamp.LeftOrRight, TooltipNovelWidth.Simple,
                                TooltipExtraText.None, TooltipExtraRules.MustBeToLeftOfTaskStack ) )
                            {
                                novel.TitleUpperLeft.AddFormat1( ObjectiveBase, Item1 );
                                novel.Main.AddFormat1( ObjectiveExplanation, Item1, ColorTheme.NarrativeColor );
                                if ( StrategyTip != null && StrategyTip.Length > 0 )
                                    novel.Main.Line().AddFormat1( StrategyTip, Item1, ColorTheme.PurpleDim );
                            }
                        }
                        break;
                    case UIAction.OnClick:
                        {
                        }
                        break;
                }
            } );
        }
        #endregion

        #region RenderObjectiveBoxFormat2
        public static void RenderObjectiveBoxFormat2( string ObjectiveBase, string ObjectiveExplanation,
            string StrategyTip, bool IsCompleted, object Item1, object Item2 )
        {
            if ( IsCompleted || ObjectiveBase.IsEmpty() )
                return;

            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            Window_TaskStack.AddTaskBox( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
            {
                if ( ObjectiveBase.IsEmpty() )
                    return;

                switch ( Action )
                {
                    case UIAction.GetTextToShowFromVolatile:
                        {
                            bool isHovered = element.LastHadMouseWithin;
                            Window_TaskStack.bTaskBox icon = (Window_TaskStack.bTaskBox)element.Controller;
                            icon.SetBoxStyle( AlertColor.Normal, isHovered );

                            ExtraData.Buffer.AddFormat2( ObjectiveBase, Item1, Item2 );
                        }
                        break;
                    case UIAction.HandleMouseover:
                        {
                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( "ChecklistObjective", ObjectiveBase ), null, SideClamp.LeftOrRight, TooltipNovelWidth.Simple,
                                TooltipExtraText.None, TooltipExtraRules.MustBeToLeftOfTaskStack ) )
                            {
                                novel.TitleUpperLeft.AddFormat2( ObjectiveBase, Item1, Item2 );
                                novel.Main.AddFormat2( ObjectiveExplanation, Item1, Item2, ColorTheme.NarrativeColor );
                                if ( StrategyTip != null && StrategyTip.Length > 0 )
                                    novel.Main.Line().AddFormat2( StrategyTip, Item1, Item2, ColorTheme.PurpleDim );
                            }
                        }
                        break;
                    case UIAction.OnClick:
                        {
                        }
                        break;
                }
            } );
        }
        #endregion

        #region RenderObjectiveBoxFormat3
        public static void RenderObjectiveBoxFormat3( string ObjectiveBase, string ObjectiveExplanation,
            string StrategyTip, bool IsCompleted, object Item1, object Item2, object Item3 )
        {
            if ( IsCompleted || ObjectiveBase.IsEmpty() )
                return;

            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            Window_TaskStack.AddTaskBox( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
            {
                if ( ObjectiveBase.IsEmpty() )
                    return;

                switch ( Action )
                {
                    case UIAction.GetTextToShowFromVolatile:
                        {
                            bool isHovered = element.LastHadMouseWithin;
                            Window_TaskStack.bTaskBox icon = (Window_TaskStack.bTaskBox)element.Controller;
                            icon.SetBoxStyle( AlertColor.Normal, isHovered );

                            ExtraData.Buffer.AddFormat3( ObjectiveBase, Item1, Item2, Item3 );
                        }
                        break;
                    case UIAction.HandleMouseover:
                        {
                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( "ChecklistObjective", ObjectiveBase ), null, SideClamp.LeftOrRight, TooltipNovelWidth.Simple,
                                TooltipExtraText.None, TooltipExtraRules.MustBeToLeftOfTaskStack ) )
                            {
                                novel.TitleUpperLeft.AddFormat3( ObjectiveBase, Item1, Item2, Item3 );
                                novel.Main.AddFormat3( ObjectiveExplanation, Item1, Item2, Item3, ColorTheme.NarrativeColor );
                                if ( StrategyTip != null && StrategyTip.Length > 0 )
                                    novel.Main.Line().AddFormat3( StrategyTip, Item1, Item2, Item3, ColorTheme.PurpleDim );
                            }
                        }
                        break;
                    case UIAction.OnClick:
                        {
                        }
                        break;
                }
            } );
        }
        #endregion
    }
}
