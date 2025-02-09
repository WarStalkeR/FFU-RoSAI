using System;
using Arcen.Universal;
using Arcen.HotM.Core;

namespace Arcen.HotM.External
{
    /// <summary>
    /// Purely for example purposes
    /// </summary>
    internal static class ExampleMessageHandler
    {
        /// <summary>
        /// Messages that we have sent, and which we are waiting for full confirmation have been completed.
        /// No external messages deal with this.
        /// </summary>
        internal static readonly List<ExampleMessageVisData> MessagesWeAreTracking = List<ExampleMessageVisData>.Create_WillNeverBeGCed( 10, "ExampleMessageHandler-MessagesWeAreTracking" );

        public static void DoPerFrame()
        {
            if ( MessagesWeAreTracking.Count <= 0 && SimMessaging.ExampleMessageQueue.Count <= 0 )
                return; //if there's nothing to do, don't bother trying

            //running this on a bg thread just to be extra fancy and involve three threads rather than two
            //also this complicates the timing a bit, but on the order of a few ms at most, not seconds at a time.
            ArcenThreading.RunTaskOnBackgroundThread( "_Example.ExampleMessageHandler",
            ( TaskStartData startData ) =>
            {
                if ( SimMessaging.ExampleMessageQueue.Count > 0)
                {
                    while ( SimMessaging.ExampleMessageQueue.TryDequeue( out ExampleMessage message ) )
                    {
                        ExampleMessageVisData visData;
                        visData.Message = message;
                        visData.TimeAtWhichToSendMessage2 = ArcenTime.AnyTimeSinceStartF + message.SecondsToWaitBetweenNotes;
                        MessagesWeAreTracking.Add( visData ); //keep track of it until we finish with it

                        //this is how we choose to interpret what the AbstractSim asked for
                        ArcenNotes.SendNoteToGameOnly( 100, LocalizedString.AddNeverTranslated_New( message.NoteData1ForVis ), NoteStyle.BothGame, 3f );

                        //messaging intermediate progress to ASim.  If ASim doesn't care about this, that's fine, but we told them, either way.
                        visData.Message.IsVisDoneNote1 = true;
                    }    
                }

                for ( int i = MessagesWeAreTracking.Count - 1; i >= 0; i-- )
                {
                    ExampleMessageVisData visData = MessagesWeAreTracking[i];
                    //apparently it's time to send part 2!
                    if ( visData.TimeAtWhichToSendMessage2 < ArcenTime.AnyTimeSinceStartF )
                    {
                        ArcenNotes.SendNoteToGameOnly( 100, LocalizedString.AddNeverTranslated_New( visData.Message.NoteData2ForVis ), NoteStyle.BothGame, 6f );

                        //messaging intermediate progress to ASim.  This is an agreed signal that we will not look at this object anymore.
                        visData.Message.IsVisDoneNote2 = true;

                        //Vis does not own the ExampleMessage data, so we won't clean it up or try to send it back to the pool.  We just forget about it.
                        //if our own ExampleMessageVisData object were a class rather than a struct, then we would put THAT back in a pool, though.
                        //as it is, that's a struct that will just go out of scope and let some stack get cleaned up
                        MessagesWeAreTracking.RemoveAt( i );
                    }
                }

            } );            
        }

        /// <summary>
        /// Generally speaking, the vis layer will have its own object of some sort (usually a class, but can be whatever)
        /// that keeps track of whatever is needed for itself.  That sort of thing doesn't really belong in Common, though it can go there if
        /// it really needs to.  The idea is that we keep Vis data separate from ASim data.
        /// </summary>
        internal struct ExampleMessageVisData
        {
            public ExampleMessage Message;
            public float TimeAtWhichToSendMessage2;
        }
    }
}
