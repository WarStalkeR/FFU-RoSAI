using System;
using Arcen.Universal;
using Arcen.HotM.Core;

namespace Arcen.HotM.External
{
    /// <summary>
    /// Purely for example purposes
    /// </summary>
    internal static class ExampleMessageSender
    {
        /// <summary>
        /// Messages that we have sent, and which we are waiting for full confirmation have been completed.
        /// No external messages deal with this.
        /// </summary>
        internal static readonly List<ExampleMessageImplementation> SentMessages = List<ExampleMessageImplementation>.Create_WillNeverBeGCed( 10, "ExampleMessageSender-SentMessages" );

        public static bool HasSentAnyMessages = false;

        public static bool DoTheseChecks = false; //make this true in order to test it.

        public static void DoPerTick()
        {
            if ( !DoTheseChecks )
                return;

            if ( !HasSentAnyMessages )
            {
                HasSentAnyMessages = true;
                //dump two messages into the queue.  Part 2 of the second message will arrive before part 2 of the first message, because of the time intervals
                SendAMessage( "Example test part 1A", 3, "Example test part 2A", "Not used, but whatever" );
                SendAMessage( "Example test part 1B", 2, "Example test part 2B", "Not used either, heh." );
            }

            for ( int i = SentMessages.Count - 1; i >= 0; i-- )
            {
                ExampleMessageImplementation message = SentMessages[i];
                if ( message.IsVisDoneNote2 )
                {
                    //this is our agreed-upon signal that Vis has let this go.
                    //nothing enforces this, we just need to trust Vis, and them us, with this part.
                    message.ReturnToPool(); //we can now stick this back in the pool
                    SentMessages.RemoveAt( i );
                }
            }
        }

        #region SendAMessage
        public static void SendAMessage( string MessageTextP1, int TimeToWait, string MessageTextP2, string SecretText )
        {
            ExampleMessageImplementation message = ExampleMessageImplementation.GetFromPoolOrCreate();
            message.NoteData1ForVis = MessageTextP1;
            message.SecondsToWaitBetweenNotes = TimeToWait;
            message.NoteData2ForVis = MessageTextP2;
            message.SecretText = SecretText;

            //track this for our internal purposes inside AbstractSim
            SentMessages.Add( message );

            //send a reference to this message to SimMessaging, for Vis to pick up some time later.
            SimMessaging.ExampleMessageQueue.Enqueue( message );
        }
        #endregion
    }

    public class ExampleMessageImplementation : ExampleMessage, IConcurrentPoolable<ExampleMessageImplementation>, IProtectedListable
    {
        //For some example reason, we decided we don't want or need Vis to see this, but we want it on here.
        public string SecretText;

        public void ClearData()
        {
            this.NoteData1ForVis = string.Empty;
            this.SecondsToWaitBetweenNotes = 0;
            this.NoteData2ForVis = string.Empty;

            this.IsVisDoneNote1 = false;
            this.IsVisDoneNote2 = false;

            this.SecretText = string.Empty;
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private ExampleMessageImplementation()
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "ExampleMessageImplementations" );
            RefTracker.IncrementObjectCount();
        }

        private static readonly ConcurrentPool<ExampleMessageImplementation> Pool = new ConcurrentPool<ExampleMessageImplementation>( "ExampleMessageImplementations",
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOn_EvenPartial_GameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new ExampleMessageImplementation(); } );

        public static ExampleMessageImplementation GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public void DoEarlyCleanupWhenGoingBackIntoPool()
        {
            this.ClearData();
        }

        public void DoAnyBelatedCleanupWhenComingOutOfPool()
        {
        }

        public void DoBeforeRemoveOrClear()
        {
            this.ReturnToPool(); //when I am remove from a list, put me back in the pool
        }

        public void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        private bool isInPool = true;
        public bool GetInPoolStatus()
        {
            return this.isInPool;
        }

        public void SetInPoolStatus( bool IsInPool )
        {
            this.isInPool = IsInPool;
        }
        #endregion

        public void Optional_DoAnyEarlyInits()
        {
        }
    }
}
