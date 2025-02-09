using Arcen.HotM.External;
using Arcen.Universal;
using Arcen.Universal.Deserialization;
using System;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public static class NoteLog
    {
        public static readonly List<IGameNote> LongTermGameLog = List<IGameNote>.Create_WillNeverBeGCed( 100, "NoteLog-LongTermGameLog" );
        public static readonly List<TemporaryNote> TemporaryNotes = List<TemporaryNote>.Create_WillNeverBeGCed( 100, "NoteLog-TemporaryNotes" );

        private static readonly ConcurrentQueue<QueuedNote> QueuedNotes = ConcurrentQueue<QueuedNote>.Create_WillNeverBeGCed( "NoteLog-QueuedNotes" );
        public static int OldestNoPruningNeeded_Game = 0;
        public static int NoteCountNeverPruned = 0;
        private static bool HasDoneValidityChecking = false;

        public static float LastTimeTemporaryNoteWasRevealed = 0;
        public static float LastTimeMouseWasOverTemporaryNote = 0;
        public static float LastTimeAnyTooltipWasShown = 0;

        public static readonly DoubleBufferedList<KeyValuePair<IGameNote, IGameNote>> RecentLog = DoubleBufferedList<KeyValuePair<IGameNote, IGameNote>>.Create_WillNeverBeGCed( 200, "NoteLog-RecentLog", 200 );

        public static readonly DoubleBufferedList<NoteInstructionCollection> FullCollections = DoubleBufferedList<NoteInstructionCollection>.Create_WillNeverBeGCed( 200, "NoteLog-AvailableCollections", 200 );
        public static readonly DoubleBufferedList<NoteInstructionCollection> RecentCollections = DoubleBufferedList<NoteInstructionCollection>.Create_WillNeverBeGCed( 200, "NoteLog-RecentCollections", 200 );

        public static int PriorityLevelToPauseAtAndAbove = 1000;

        #region DoPerFullSecond_BackgroundThread
        public static void DoPerFullSecond_BackgroundThread()
        {
            int debugStage = 0;
            try
            {
                debugStage = 1000;
                foreach ( NoteInstructionCollection coll in NoteInstructionCollectionTable.SortedCollections )
                {
                    coll.Meta_RecentVisible.ClearConstructionValueForStartingConstruction();
                    coll.Meta_TotalVisible.ClearConstructionValueForStartingConstruction();
                }
                debugStage = 2000;
                RecentLog.ClearConstructionListForStartingConstruction();
                FullCollections.ClearConstructionListForStartingConstruction();
                RecentCollections.ClearConstructionListForStartingConstruction();

                debugStage = 3000;
                int indexOfLastToPrune = 999999999;
                int indexOfHeader = 999999999;
                int headersRemaining = 10;
                for ( int i = LongTermGameLog.Count - 1; i >= OldestNoPruningNeeded_Game; i-- )
                {
                    IGameNote gNote = LongTermGameLog[i];
                    if ( gNote == null )
                        continue;
                    if ( gNote.PruneAfter > 0 )
                        indexOfLastToPrune = i;

                    debugStage = 3100;
                    NoteInstruction instruct = gNote.Instruction;
                    if ( instruct != null )
                    {
                        debugStage = 3200;
                        if ( headersRemaining > 0 )
                        {
                            if ( instruct.HeaderType == 2 )
                            {
                                indexOfHeader = i;
                                headersRemaining--;
                            }
                        }
                    }
                }

                if ( indexOfHeader >= 999999999 )
                    indexOfHeader = 0; //start at the start, then.  We must be very early in the game.

                debugStage = 5000;
                int lastIndexOfRecent = MathA.Min( indexOfLastToPrune, indexOfHeader );

                int maxCount = LongTermGameLog.Count;
                IGameNote lastTurnHeader = null;
                for ( int i = 0; i <= maxCount; i++ )
                {
                    IGameNote gNote = LongTermGameLog[i];
                    if ( gNote == null )
                        continue;

                    debugStage = 5100;
                    NoteInstruction instruct = gNote.Instruction;
                    if ( instruct != null )
                    {
                        debugStage = 5200;
                        if ( instruct.HeaderType == 2 )
                            lastTurnHeader = gNote;
                        else //do not add turn headers to these counts or they look very off
                        {
                            if ( i >= lastIndexOfRecent )
                            {
                                foreach ( KeyValuePair<NoteInstructionCollection, int> kv in instruct.Collections )
                                {
                                    if ( kv.Key.ExcludeEphemeral && gNote.PruneAfter > 0 )
                                        continue;
                                    if ( kv.Key.ExcludePermanent && gNote.PruneAfter <= 0 )
                                        continue;
                                    kv.Key.Meta_RecentVisible.Construction++;
                                }
                            }

                            foreach ( KeyValuePair<NoteInstructionCollection, int> kv in instruct.Collections )
                            {
                                if ( kv.Key.ExcludeEphemeral && gNote.PruneAfter > 0 )
                                    continue;
                                if ( kv.Key.ExcludePermanent && gNote.PruneAfter <= 0 )
                                    continue;
                                kv.Key.Meta_TotalVisible.Construction++;
                            }
                        }
                    }

                    if ( i >= lastIndexOfRecent )
                        RecentLog.AddToConstructionList( new KeyValuePair<IGameNote, IGameNote>( gNote, lastTurnHeader ) );
                }

                debugStage = 7000;
                foreach ( NoteInstructionCollection coll in NoteInstructionCollectionTable.SortedCollections )
                {
                    coll.Meta_RecentVisible.SwitchConstructionToDisplay();
                    coll.Meta_TotalVisible.SwitchConstructionToDisplay();

                    if ( coll.Meta_TotalVisible.Display > 0 )
                        FullCollections.AddToConstructionList( coll );
                    if ( coll.Meta_RecentVisible.Display > 0 && !coll.ExcludeEphemeral )
                        RecentCollections.AddToConstructionList( coll );
                }

                RecentLog.SwitchConstructionToDisplay();
                FullCollections.SwitchConstructionToDisplay();
                RecentCollections.SwitchConstructionToDisplay();
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "NoteLogPerSecond", debugStage, e, Verbosity.ShowAsError );
            }
        }
        #endregion

        #region DoPerQuarterSecond_BackgroundThread
        public static void DoPerQuarterSecond_BackgroundThread()
        {
            //nothing to do here for now
        }
        #endregion

        public static void DoPerFrame()
        {
            #region Get PriorityLevelToPauseAtAndAbove
            {
                int index = GameSettings.Current?.GetInt( "BottomRightNotice_PausePriorityLevel" )??0;
                if ( index < 0 )
                    index = 0;
                if ( index >= NotePriorityPauseLevelTable.Instance.Rows.Length )
                    index = 0;
                NotePriorityPauseLevel pauseLevel = NotePriorityPauseLevelTable.Instance.Rows[index];
                PriorityLevelToPauseAtAndAbove = pauseLevel?.Level??1000;
                //ArcenDebugging.LogSingleLine( "index: " + index + " PriorityLevelToPauseAtAndAbove: " + PriorityLevelToPauseAtAndAbove, Verbosity.DoNotShow );
            }
            #endregion

            if ( ArcenTime.AnyTimeSinceStartF - LastTimeMouseWasOverTemporaryNote > 0.2f &&  //hovering the log items
                ArcenTime.AnyTimeSinceStartF - LastTimeAnyTooltipWasShown > 0.2f ) //any tooltip is open
            {
                for ( int i = TemporaryNotes.Count - 1; i >= 0; i-- )
                {
                    TemporaryNote tNote = TemporaryNotes[i];
                    if ( !tNote.HasBeenRevealed )
                        continue;
                    if ( tNote.PriorityLevel >= PriorityLevelToPauseAtAndAbove )
                        continue;

                    tNote.RemainingTime -= ArcenTime.AnyDeltaTime;
                    if ( tNote.RemainingTime <= 0) //expired temporary notes
                        TemporaryNotes.RemoveAt( i );
                }
            }

            #region Get Rid Of Invalid Entries
            if ( !HasDoneValidityChecking  )
            {
                HasDoneValidityChecking = true;
                for ( int i = LongTermGameLog.Count - 1; i >= OldestNoPruningNeeded_Game; i-- )
                {
                    IGameNote gNote = LongTermGameLog[i];
                    if ( gNote.PruneAfter > 0 )
                    {
                        if ( gNote.PruneAfter < SimCommon.Turn )
                        {
                            LongTermGameLog.RemoveAt( i );
                            continue;
                        }
                    }

                    if ( !gNote.HandleNote( GameNoteAction.GetIsStillValid, null, false, null, null, string.Empty, 0, false ) )
                        LongTermGameLog.RemoveAt( i );
                }

                for ( int i = SimMetagame.LongTermMetaLog.Count - 1; i >= 0; i-- )
                {
                    IGameNote gNote = SimMetagame.LongTermMetaLog[i];

                    if ( !gNote.HandleNote( GameNoteAction.GetIsStillValid, null, false, null, null, string.Empty, 0, false ) )
                        SimMetagame.LongTermMetaLog.RemoveAt( i );
                }
            }
            #endregion

            #region Long-Term Log Game
            int noteCountNeverPruned = 0;
            if ( LongTermGameLog.Count > 0 )
            {
                int oldestPruneNeeded = -1;
                int startingIndex = LongTermGameLog.Count - 1;
                int eventuallyWillPrune = 0;
                for ( int i = LongTermGameLog.Count - 1; i >= OldestNoPruningNeeded_Game; i-- )
                {
                    IGameNote gNote = LongTermGameLog[i];
                    if ( gNote.PruneAfter > 0 )
                    {
                        oldestPruneNeeded = i;
                        if ( gNote.PruneAfter < SimCommon.Turn )
                            LongTermGameLog.RemoveAt( i );
                        else
                            eventuallyWillPrune++;
                    }
                }
                noteCountNeverPruned = LongTermGameLog.Count - eventuallyWillPrune;
                if ( oldestPruneNeeded < 0 )
                    OldestNoPruningNeeded_Game = startingIndex; //none of the current batch needed pruning
                else
                {
                    if ( oldestPruneNeeded - 1 > OldestNoPruningNeeded_Game )
                        OldestNoPruningNeeded_Game = oldestPruneNeeded - 1; //go to the one before the last one that needs pruning
                }
            }
            NoteCountNeverPruned = noteCountNeverPruned;
            #endregion

            while ( QueuedNotes.TryDequeue( out QueuedNote qNote ) )
                LogEntry_ToRealLists( qNote.Note, qNote.Style );
        }

        #region LogEntry_ToRealLists
        private static void LogEntry_ToRealLists( IGameNote Note, NoteStyle NoteStyle )
        {
            if ( Note == null )
                return;

            bool logTemporary = false;
            bool logLongTermGame = false;
            bool logLongTermMeta = false;
            switch ( NoteStyle )
            {
                case NoteStyle.BothGame:
                    logTemporary = true;
                    logLongTermGame = true;
                    break;
                case NoteStyle.BothMeta:
                    logTemporary = true;
                    logLongTermMeta = true;
                    break;
                case NoteStyle.AllThree:
                    logTemporary = true;
                    logLongTermGame = true;
                    logLongTermMeta = true;
                    break;
                case NoteStyle.ShowInPassing:
                    logTemporary = true;
                    break;
                case NoteStyle.StoredGame:
                    logLongTermGame = true;
                    break;
                case NoteStyle.StoredMeta:
                    logLongTermMeta = true;
                    break;
            }

            if ( logTemporary && Note != null )
            {
                TemporaryNote tempNote = new TemporaryNote();
                tempNote.Note = Note;
                tempNote.RemainingTime = InputCaching.BottomRightNotice_LingerTime;
                tempNote.HasBeenRevealed = false;
                tempNote.PriorityLevel = Note.PriorityLevel;
                TemporaryNotes.Add( tempNote );
            }

            if ( logLongTermGame )
                LongTermGameLog.Add( Note );

            if ( logLongTermMeta )
                SimMetagame.LongTermMetaLog.Add( Note );
        }
        #endregion

        #region LogEntry
        public static void LogEntry( IGameNote Note, NoteStyle NoteStyle )
        {
            if ( Engine_Universal.CalculateIsCurrentThreadMainThread() )
                LogEntry_ToRealLists( Note, NoteStyle ); //main thread, just put it in directly
            else
            {
                //we are on a background thread, so queue it
                QueuedNote qNote;
                qNote.Note = Note;
                qNote.Style = NoteStyle;
                QueuedNotes.Enqueue( qNote );
            }
        }
        #endregion

        #region Serialization
        public static void SerializeData( ArcenFileSerializer Serializer )
        {
            while ( QueuedNotes.TryDequeue( out QueuedNote qNote ) )
                LogEntry_ToRealLists( qNote.Note, qNote.Style );

            ArcenNotes.SerializeNoteList( Serializer, LongTermGameLog );

            if ( TemporaryNotes.Count > 0 )
            {
                foreach ( TemporaryNote note in TemporaryNotes )
                {
                    if ( note.RemainingTime <= 0f || note.Note == null )
                        continue;
                    Serializer.StartObject( "TemporaryNote" );
                    note.Serialize( Serializer );
                    Serializer.EndObject( "TemporaryNote" );
                }
            }
        }

        public static void DeserializeData( DeserializedObjectLayer Data )
        {
            ArcenNotes.DeserializeNotes( Data, LongTermGameLog );

            TemporaryNotes.Clear();
            if ( Data.ChildLayersByName.TryGetValue( "TemporaryNote", out List<DeserializedObjectLayer> temporaryNoteList ) )
            {
                foreach ( DeserializedObjectLayer dataLayer in temporaryNoteList )
                {
                    TemporaryNote note = new TemporaryNote();
                    note.Deserialize( dataLayer );
                    if ( note.Note == null || note.RemainingTime <= 0f )
                        continue;

                    TemporaryNotes.Add( note );
                }
            }
        }
        #endregion

        #region Cleanup
        public static void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            LastTimeTemporaryNoteWasRevealed = 0;
            LastTimeMouseWasOverTemporaryNote = 0;

            LongTermGameLog.Clear();
            TemporaryNotes.Clear();

            QueuedNotes.Clear();
            OldestNoPruningNeeded_Game = 0;
            HasDoneValidityChecking = false;
        }
        public static void ClearAllObjectsBecauseOfUnload()
        {
            ClearAllMyDataForQuitToMainMenuOrBeforeNewMap();
        }
        #endregion

        public struct NoteDisplayData
        {
            public IGameNote Note;
            public bool IsHeader;
            public IGameNote LastHeader;

            public static NoteDisplayData Create( IGameNote Note, bool IsHeader, IGameNote LastHeader )
            {
                NoteDisplayData data;
                data.Note = Note;
                data.IsHeader = IsHeader;
                data.LastHeader = LastHeader;
                return data;
            }
        }
    }
}