using System;
using Arcen.Universal;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    #region FullscreenResolutionDropdownFiller
    public class FullscreenResolutionDropdownFiller : IDropdownFiller
    {
        public int GetCurrentValue( ArcenSetting setting )
        {
            int searchForWidth = GameSettings.Current.GetInt( ArcenIntSetting_Universal.FullscreenWidth );
            int searchForHeight = GameSettings.Current.GetInt( ArcenIntSetting_Universal.FullscreenHeight );

            int match = -1;
            for ( int i = 0; i < ArcenUI.SupportedResolutions.Count; i++ )
            {
                ArcenPoint resolution = ArcenUI.SupportedResolutions[i];
                if ( resolution.X != searchForWidth )
                    continue;
                if ( resolution.Y != searchForHeight )
                    continue;
                match = i;
                break;
            }
            if ( match < 0 )
            {
                ArcenDebugging.LogSingleLine( "Could not find resolution matching " + searchForWidth + "x" + searchForHeight +
                    " out of " + ArcenUI.SupportedResolutions.Count + " options!", Verbosity.ShowAsError );
                return 0;
            }
            else
                return match;
        }

        public int GetTempValue( ArcenSetting setting )
        {
            return setting.TempValue_Int;
        }

        public void SetExtraValuesFromSpecialLogicIfWeShould( int Value )
        {
            if ( Value >= 0 && Value < ArcenUI.SupportedResolutions.Count )
            {
                ArcenPoint resolution = ArcenUI.SupportedResolutions[Value];
                GameSettings.Current.SetInt( ArcenIntSetting_Universal.FullscreenWidth, resolution.X );
                GameSettings.Current.SetInt( ArcenIntSetting_Universal.FullscreenHeight, resolution.Y );
            }
            else
                ArcenDebugging.LogSingleLine( "Could not find resolution for TempValue_Int " + Value +
                    " out of " + ArcenUI.SupportedResolutions.Count + " options!", Verbosity.ShowAsError );
        }

        private readonly List<IArcenDropdownOption> cachedOptions = List<IArcenDropdownOption>.Create_WillNeverBeGCed( 20, "FullscreenResolutionDropdownFiller-cachedOptions" );
        public List<IArcenDropdownOption> GetListOfDropdownOptions()
        {
            if ( cachedOptions.Count == 0 || cachedOptions.Count != ArcenUI.SupportedResolutions.Count )
            {
                cachedOptions.Clear();
                for ( int i = 0; i < ArcenUI.SupportedResolutions.Count; i++ )
                {
                    ArcenPoint pt = ArcenUI.SupportedResolutions[i];
                    cachedOptions.Add( new ArcenDropdownOption( pt,
                        delegate ( ArcenDoubleCharacterBuffer buffer )
                        {
                            ArcenDropdownOption.GetNameForResolutionBasedDropdown( pt, buffer );
                        } ) );
                }
            }
            return cachedOptions;
        }
    }
    #endregion

    #region QualityFramerateTypeFiller
    public class QualityFramerateTypeFiller : IDropdownFiller
    {
        public int GetCurrentValue( ArcenSetting setting )
        {
            return GameSettings.Current.GetInt( setting );
        }

        public int GetTempValue( ArcenSetting setting )
        {
            return setting.TempValue_Int;
        }

        public void SetExtraValuesFromSpecialLogicIfWeShould( int Value )
        { }

        private readonly List<IArcenDropdownOption> cachedOptions = List<IArcenDropdownOption>.Create_WillNeverBeGCed( 20, "QualityFramerateTypeFiller-cachedOptions" );
        public List<IArcenDropdownOption> GetListOfDropdownOptions()
        {
            if ( cachedOptions.Count == 0 || cachedOptions.Count != QualityFramerateTypeTable.Instance.Rows.Length )
            {
                cachedOptions.Clear();
                for ( int i = 0; i < QualityFramerateTypeTable.Instance.Rows.Length; i++ )
                {
                    QualityFramerateType frameRate = QualityFramerateTypeTable.Instance.Rows[i];
                    cachedOptions.Add( frameRate );
                }
            }
            return cachedOptions;
        }
    }
    #endregion

    #region QualityMSAATypeFiller
    public class QualityMSAATypeFiller : IDropdownFiller
    {
        public int GetCurrentValue( ArcenSetting setting )
        {
            return GameSettings.Current.GetInt( setting );
        }

        public int GetTempValue( ArcenSetting setting )
        {
            return setting.TempValue_Int;
        }

        public void SetExtraValuesFromSpecialLogicIfWeShould( int Value )
        { }

        private readonly List<IArcenDropdownOption> cachedOptions = List<IArcenDropdownOption>.Create_WillNeverBeGCed( 20, "QualityMSAATypeFiller-cachedOptions" );
        public List<IArcenDropdownOption> GetListOfDropdownOptions()
        {
            if ( cachedOptions.Count == 0 || cachedOptions.Count != QualityMSAATypeTable.Instance.Rows.Length )
            {
                cachedOptions.Clear();
                for ( int i = 0; i < QualityMSAATypeTable.Instance.Rows.Length; i++ )
                {
                    QualityMSAAType frameRate = QualityMSAATypeTable.Instance.Rows[i];
                    cachedOptions.Add( frameRate );
                }
            }
            return cachedOptions;
        }
    }
    #endregion

    #region QualityAnisotrophicModeFiller
    public class QualityAnisotrophicModeFiller : IDropdownFiller
    {
        public int GetCurrentValue( ArcenSetting setting )
        {
            return GameSettings.Current.GetInt( setting );
        }

        public int GetTempValue( ArcenSetting setting )
        {
            return setting.TempValue_Int;
        }

        public void SetExtraValuesFromSpecialLogicIfWeShould( int Value )
        { }

        private readonly List<IArcenDropdownOption> cachedOptions = List<IArcenDropdownOption>.Create_WillNeverBeGCed( 20, "QualityAnisotrophicModeFiller-cachedOptions" );
        public List<IArcenDropdownOption> GetListOfDropdownOptions()
        {
            if ( cachedOptions.Count == 0 || cachedOptions.Count != QualityAnisotrophicModeTable.Instance.Rows.Length )
            {
                cachedOptions.Clear();
                for ( int i = 0; i < QualityAnisotrophicModeTable.Instance.Rows.Length; i++ )
                {
                    QualityAnisotrophicMode frameRate = QualityAnisotrophicModeTable.Instance.Rows[i];
                    cachedOptions.Add( frameRate );
                }
            }
            return cachedOptions;
        }
    }
    #endregion

    #region QualityParticleRaycastBudgetFiller
    public class QualityParticleRaycastBudgetFiller : IDropdownFiller
    {
        public int GetCurrentValue( ArcenSetting setting )
        {
            return GameSettings.Current.GetInt( setting );
        }

        public int GetTempValue( ArcenSetting setting )
        {
            return setting.TempValue_Int;
        }

        public void SetExtraValuesFromSpecialLogicIfWeShould( int Value )
        { }

        private readonly List<IArcenDropdownOption> cachedOptions = List<IArcenDropdownOption>.Create_WillNeverBeGCed( 20, "QualityParticleRaycastBudgetFiller-cachedOptions" );
        public List<IArcenDropdownOption> GetListOfDropdownOptions()
        {
            if ( cachedOptions.Count == 0 || cachedOptions.Count != QualityParticleRaycastBudgetTable.Instance.Rows.Length )
            {
                cachedOptions.Clear();
                for ( int i = 0; i < QualityParticleRaycastBudgetTable.Instance.Rows.Length; i++ )
                {
                    QualityParticleRaycastBudget frameRate = QualityParticleRaycastBudgetTable.Instance.Rows[i];
                    cachedOptions.Add( frameRate );
                }
            }
            return cachedOptions;
        }
    }
    #endregion

    #region QualitySecondaryAATypeFiller
    public class QualitySecondaryAATypeFiller : IDropdownFiller
    {
        public int GetCurrentValue( ArcenSetting setting )
        {
            return GameSettings.Current.GetInt( setting );
        }

        public int GetTempValue( ArcenSetting setting )
        {
            return setting.TempValue_Int;
        }

        public void SetExtraValuesFromSpecialLogicIfWeShould( int Value )
        { }

        private readonly List<IArcenDropdownOption> cachedOptions = List<IArcenDropdownOption>.Create_WillNeverBeGCed( 20, "QualitySecondaryAATypeFiller-cachedOptions" );
        public List<IArcenDropdownOption> GetListOfDropdownOptions()
        {
            if ( cachedOptions.Count == 0 || cachedOptions.Count != QualitySecondaryAATypeTable.Instance.Rows.Length )
            {
                cachedOptions.Clear();
                for ( int i = 0; i < QualitySecondaryAATypeTable.Instance.Rows.Length; i++ )
                {
                    QualitySecondaryAAType type = QualitySecondaryAATypeTable.Instance.Rows[i];
                    cachedOptions.Add( type );
                }
            }
            return cachedOptions;
        }
    }
    #endregion

    #region QualityPresetFiller
    public class QualityPresetFiller : IDropdownFiller
    {
        public int GetCurrentValue( ArcenSetting setting )
        {
            return GameSettings.Current.GetInt( setting );
        }

        public int GetTempValue( ArcenSetting setting )
        {
            return setting.TempValue_Int;
        }

        public void SetExtraValuesFromSpecialLogicIfWeShould( int Value )
        { }

        private readonly List<IArcenDropdownOption> cachedOptions = List<IArcenDropdownOption>.Create_WillNeverBeGCed( 20, "QualityPresetFiller-cachedOptions" );
        public List<IArcenDropdownOption> GetListOfDropdownOptions()
        {
            if ( cachedOptions.Count == 0 || cachedOptions.Count != QualityPresetTable.Instance.Rows.Length )
            {
                cachedOptions.Clear();
                for ( int i = 0; i < QualityPresetTable.Instance.Rows.Length; i++ )
                {
                    QualityPreset frameRate = QualityPresetTable.Instance.Rows[i];
                    cachedOptions.Add( frameRate );
                }
            }
            return cachedOptions;
        }
    }
    #endregion

    #region AutosaveIntervalFiller
    public class AutosaveIntervalFiller : IDropdownFiller
    {
        public int GetCurrentValue( ArcenSetting setting )
        {
            return GameSettings.Current.GetInt( setting );
        }

        public int GetTempValue( ArcenSetting setting )
        {
            return setting.TempValue_Int;
        }

        public void SetExtraValuesFromSpecialLogicIfWeShould( int Value )
        { }

        private readonly List<IArcenDropdownOption> cachedOptions = List<IArcenDropdownOption>.Create_WillNeverBeGCed( 20, "AutosaveIntervalFiller-cachedOptions" );
        public List<IArcenDropdownOption> GetListOfDropdownOptions()
        {
            if ( cachedOptions.Count == 0 || cachedOptions.Count != AutosaveIntervalTypeTable.Instance.Rows.Length )
            {
                cachedOptions.Clear();
                for ( int i = 0; i < AutosaveIntervalTypeTable.Instance.Rows.Length; i++ )
                {
                    AutosaveIntervalType type = AutosaveIntervalTypeTable.Instance.Rows[i];
                    cachedOptions.Add( type );
                }
            }
            return cachedOptions;
        }
    }
    #endregion

    #region NPCActionsViewFiller
    public class NPCActionsViewFiller : IDropdownFiller
    {
        public int GetCurrentValue( ArcenSetting setting )
        {
            return GameSettings.Current.GetInt( setting );
        }

        public int GetTempValue( ArcenSetting setting )
        {
            return setting.TempValue_Int;
        }

        public void SetExtraValuesFromSpecialLogicIfWeShould( int Value )
        { }

        private readonly List<IArcenDropdownOption> cachedOptions = List<IArcenDropdownOption>.Create_WillNeverBeGCed( 20, "NPCActionsViewFiller-cachedOptions" );
        public List<IArcenDropdownOption> GetListOfDropdownOptions()
        {
            if ( cachedOptions.Count == 0 )
            {
                cachedOptions.Clear();
                cachedOptions.Add( ArbitraryArcenOption.Create( (int)NPCActionViewType.WatchNone, "NPCActionsView_WatchNone_Name", "NPCActionsView_WatchNone_Desc" ) );
                cachedOptions.Add( ArbitraryArcenOption.Create( (int)NPCActionViewType.WatchHigh, "NPCActionsView_WatchHigh_Name", "NPCActionsView_WatchHigh_Desc" ) );
                cachedOptions.Add( ArbitraryArcenOption.Create( (int)NPCActionViewType.WatchAll, "NPCActionsView_WatchAll_Name", "NPCActionsView_WatchAll_Desc" ) );
            }
            return cachedOptions;
        }
    }
    #endregion

    #region NotePriorityPauseLevelFiller
    public class NotePriorityPauseLevelFiller : IDropdownFiller
    {
        public int GetCurrentValue( ArcenSetting setting )
        {
            return GameSettings.Current.GetInt( setting );
        }

        public int GetTempValue( ArcenSetting setting )
        {
            return setting.TempValue_Int;
        }

        public void SetExtraValuesFromSpecialLogicIfWeShould( int Value )
        { }

        private readonly List<IArcenDropdownOption> cachedOptions = List<IArcenDropdownOption>.Create_WillNeverBeGCed( 20, "NotePriorityPauseLevelFiller-cachedOptions" );
        public List<IArcenDropdownOption> GetListOfDropdownOptions()
        {
            if ( cachedOptions.Count == 0 || cachedOptions.Count != NotePriorityPauseLevelTable.Instance.Rows.Length )
            {
                cachedOptions.Clear();
                for ( int i = 0; i < NotePriorityPauseLevelTable.Instance.Rows.Length; i++ )
                {
                    NotePriorityPauseLevel pauseLevel = NotePriorityPauseLevelTable.Instance.Rows[i];
                    cachedOptions.Add( pauseLevel );
                }
            }
            return cachedOptions;
        }
    }
    #endregion
}