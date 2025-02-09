using Arcen.Universal;
using System;

namespace Arcen.HotM.ExternalVis
{
    public class TemporaryNote //just let this get gc'd, that's okay
    {
        public float RemainingTime;
        public IGameNote Note;
        public bool HasBeenRevealed;
        public int PriorityLevel;

        public void Serialize( ArcenFileSerializer Serializer )
        {
            Serializer.AddFloat( "RemainingTime", this.RemainingTime );
            Serializer.AddBoolIfTrue( "HasBeenRevealed", this.HasBeenRevealed );

            if ( this.PriorityLevel > 0 )
                Serializer.AddInt32( "PriorityLevel", this.PriorityLevel );

            if ( this.Note.Instruction != null )
                Serializer.AddRepeatedlyUsedString_Condensed( "Instruction", this.Note.Instruction.ID );

            this.Note.Serialize( Serializer );
        }

        public void Deserialize( DeserializedObjectLayer Data )
        {
            this.RemainingTime = Data.GetFloat( "RemainingTime", false );
            this.HasBeenRevealed = Data.GetBool( "HasBeenRevealed", false );

            if ( Data.TryGetTableRow( "Instruction", NoteInstructionTable.Instance, out NoteInstruction instruction ) )
            {
                int priorityLevel = Data.GetInt32( "PriorityLevel", false );

                this.Note = instruction.Handler.DeserializeNote( priorityLevel, instruction, Data );
            }
            else
            {
                RawNote rawNote = new RawNote();
                if ( rawNote.Deserialize( Data ) )
                    this.Note = rawNote;
            }
        }
    }
}