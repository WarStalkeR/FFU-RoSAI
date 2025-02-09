using System;
using Arcen.Universal;
using Arcen.HotM.Core;

namespace Arcen.HotM.External
{
    /// <summary>
    /// Purely for example purposes
    /// </summary>
    public abstract class ExampleMessage
    {
        //SHARED DATA SECTION
        //--------------------------------------------------------------------

        //We publicly have data in this section that we want Vis to read.
        //ASim writes it before ever sending it to Common.
        //no thread should alter this section at all after it goes to Common, but any thread can read from it.
        //After Vis is done with it, and this is back to just being held by ASim again, then ASim can do what it likes.
        //data in this section can be lists, dictionaries, strings, ints, giant complex data structures with compound complexity.  It doesn't matter.
        //alterations to any of the data in here after it goes to common and before ASim resumes sole ownership means threading errors, potentially very intermittent ones

        public string NoteData1ForVis = string.Empty;
        public int SecondsToWaitBetweenNotes = 3;
        public string NoteData2ForVis = string.Empty;

        //SIGNALING SECTION
        //--------------------------------------------------------------------

        //below here we have fields that are only written from Vis, but read by ASim
        //these need to all be atomic types (int, bool, etc -- not string or decimal or complex objects)
        //ASim needs to understand very clearly when Vis is completely done, but can also have intermittent signals
        //Vis needs to forget its references once it signals that it is done with the last step of this object, or threading issues ensue

        public bool IsVisDoneNote1 = false;

        public bool IsVisDoneNote2 = false;
    }
}
