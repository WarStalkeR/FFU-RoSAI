using System;
using Arcen.Universal;
using Arcen.HotM.Core;

namespace Arcen.HotM.External
{
    internal static class MathTests
    {
        #region DoMatrixTests
        public static void DoMatrixTests()
        {
            UnityEngine.Transform trans = null;
            int nMatrixMultMismatch = 0;
            int nTRSMismatch = 0;
            int nEulerMismatch = 0;
            int total = 0;
            bool printArcenMatrixMathTestOutput = false; //so as to not clutter the log for everyone. Note if errors are hit it will still complain, which is desirable
            bool includeEulerTests = false; //these don't work right now!

            foreach ( KeyValuePair<int, A5ObjectRoot> pair in A5ObjectAggregation.ObjectRootsByID )
            {
                A5ObjectRoot objRoot = pair.Value; //note eventually we'll use some intermediate structure, since we may want to unload this GameObject
                if ( objRoot.InEditorInstances_IncludesDeleted.Count == 0 )
                    continue; //if there are no in-game instances then I don't have any GameObjects to look at

                for ( int i = 0; i < objRoot.InEditorInstances_IncludesDeleted.Count; i++ )
                {
                    UnityEngine.Vector3 unityVec = new UnityEngine.Vector3( 2f, 0.5f, 3f );
                    IA5Placeable place = objRoot.InEditorInstances_IncludesDeleted[i];
                    if ( !place.IsValid || !place.IsObjectActive )
                        continue; //this was deleted
                    trans = place.GetTransform();
                    UnityEngine.Vector3 unityResult = trans.localToWorldMatrix * unityVec;
                    UnityEngine.Vector3 pos = trans.position;
                    //ArcenDebugging.LogSingleLine("Position: of " + obj + ": " + pos , Verbosity.DoNotShow );
                    total++;
                    bool success = ArcenMatrixMath.doMatrixMultiplicationTest( unityVec, trans.localToWorldMatrix );
                    if ( !success )
                        nMatrixMultMismatch++;
                    success = ArcenMatrixMath.doTRSTest( trans );
                    if ( !success )
                        nTRSMismatch++;
                    if ( includeEulerTests )
                    {
                        success = ArcenMatrixMath.doEulerTest( trans );
                        if ( !success )
                            nEulerMismatch++;
                    }

                }
            }
            if ( printArcenMatrixMathTestOutput )
            {
                if ( includeEulerTests )
                    ArcenDebugging.LogSingleLine( "We have " + nMatrixMultMismatch + " vector*matrix mismatches and " + nTRSMismatch + " TRS mismatches and " + nEulerMismatch + " euler mismatches out of " + total + " drawing on unity gameobjects that exist", Verbosity.DoNotShow );
                else
                    ArcenDebugging.LogSingleLine( "We have " + nMatrixMultMismatch + " vector*matrix mismatches and " + nTRSMismatch + " TRS mismatches (euler mismatches were skipped) of " + total + " drawing on unity gameobjects that exist", Verbosity.DoNotShow );
                ArcenDebugging.LogSingleLine( "Doing some additional matrix math tests", Verbosity.DoNotShow );
                int nTests = 100;
                ArcenMatrixMath.AdditionalMatrixTests( nTests );
                ArcenDebugging.LogSingleLine( "Doing some " + nTests + "  TRS tests", Verbosity.DoNotShow );
                int nFails = ArcenMatrixMath.AdditionalTRSTests( nTests );
                ArcenDebugging.LogSingleLine( "We saw " + nFails + " additional TRS failures", Verbosity.DoNotShow );

                if ( includeEulerTests )
                {
                    ArcenDebugging.LogSingleLine( "Doing some " + nTests + " euler tests", Verbosity.DoNotShow );
                    nFails = ArcenMatrixMath.AdditionalEulerTests( nTests );
                    ArcenDebugging.LogSingleLine( "We saw " + nFails + " additional euler failures", Verbosity.DoNotShow );
                }
                ArcenDebugging.LogSingleLine( "Doing some AABB tests", Verbosity.DoNotShow );
                ArcenMatrixMath.AdditionalAABBTests();
            }

        }
        #endregion
    }
}
