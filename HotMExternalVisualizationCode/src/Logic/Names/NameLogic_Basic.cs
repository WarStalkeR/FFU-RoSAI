using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Arcen.HotM.ExternalVis
{
    public class NameLogic_Basic : IVisNameLogicImplementation
    {
        #region GenerateNewRandomUniqueName_Unspecified
        public string GenerateNewRandomUniqueName_Unspecified( VisNameLogic Logic, MersenneTwister Rand )
        {
            switch ( Logic.ID )
            {
                //case "CitizenBarcode":
                //    for ( int attemptCount = 0; attemptCount < 100; attemptCount++ )
                //    {
                //        string name = GenerateCitizenBarcode( Rand );
                //        if ( !CheckIfAlreadyExists_Unspecified( name ) )
                //            return name;
                //    }
                //    return "FailedCitizenBarcodeCount";
                default:
                    ArcenDebugging.LogSingleLine( "GenerateNewRandomUniqueName_Unspecified: No entry found in NameLogic_Basic for '" + Logic.ID + "'", Verbosity.ShowAsError );
                    return "???";
            }
        }

        private bool CheckIfAlreadyExists_Unspecified( string Name )
        {
            return false;// World.NPCs.GetNPCNamesEverUsed()[Name];
        }
        #endregion

        #region GenerateNewRandomUniqueName_ForMachineUnit
        public string GenerateNewRandomUniqueName_ForMachineUnit( VisNameLogic Logic, ISimMachineUnit Unit, MersenneTwister Rand )
        {
            switch ( Logic.ID )
            {
                case "UnitBarcode":
                    for ( int attemptCount = 0; attemptCount < 100; attemptCount++ )
                    {
                        string name = GenerateMachineUnitBarcode( Unit.UnitType.NamePrefix.Text, Rand );
                        if ( !CheckIfAlreadyExists_MachineUnit( name ) )
                            return name;
                    }
                    return "FailedUnitName";
                default:
                    ArcenDebugging.LogSingleLine( "GenerateNewRandomUniqueName_ForMachineUnit: No entry found in NameLogic_Basic for '" + Logic.ID + "'", Verbosity.ShowAsError );
                    return "???";
            }
        }

        private bool CheckIfAlreadyExists_MachineUnit( string Name )
        {
            return World.Forces.GetHasMachineUnitNameEverBeenUsed( Name );
        }
        #endregion

        #region GenerateNewRandomUniqueName_ForMachineVehicle
        public string GenerateNewRandomUniqueName_ForMachineVehicle( VisNameLogic Logic, ISimMachineVehicle Vehicle, MersenneTwister Rand )
        {
            switch ( Logic.ID )
            {
                case "MachineVehicle":
                    {
                        RandomNamePartType p1 = RandomNamePartTypeTable.Instance.GetRowByID( "MachineVehicles_P1" );
                        RandomNamePartType p2 = RandomNamePartTypeTable.Instance.GetRowByID( "MachineVehicles_P2" );

                        for ( int attemptCount = 0; attemptCount < 100; attemptCount++ )
                        {
                            string p2OfName = p2.PartsList.GetRandom( Rand ).GetDisplayName();
                            if ( !CheckIfAlreadyExists_PartMachineVehicleName( p2OfName ) ) //p2 must be unique
                            {
                                string p1OfName = p1.PartsList.GetRandom( Rand ).GetDisplayName(); //p1 is reused a lot
                                return Lang.Format2( "MachineVehicleNameParts", p1OfName, p2OfName );
                            }
                        }
                        return "FailedVehicleName";
                    }
                default:
                    ArcenDebugging.LogSingleLine( "GenerateNewRandomUniqueName_ForMachineVehicle: No entry found in NameLogic_Basic for '" + Logic.ID + "'", Verbosity.ShowAsError );
                    return "???";
            }
        }

        private bool CheckIfAlreadyExists_PartMachineVehicleName( string PartialName )
        {
            return World.Forces.GetHasPartialVehicleNameEverBeenUsed( PartialName );
        }
        #endregion

        #region GenerateMachineUnitBarcode
        private string GenerateMachineUnitBarcode( string UnitTypePrefix, MersenneTwister Rand )
        {
            return GenerateBarcodeSecondHalf( UnitTypePrefix, Rand );
        }
        #endregion

        #region GenerateBarcodeSecondHalf
        private string GenerateBarcodeSecondHalf( string BasePart, MersenneTwister Rand )
        {
            int styleIndex = Rand.Next( 0, 6 );
            switch ( styleIndex )
            {
                case 0:
                default:
                    return BasePart + " " + GetRandomUppercaseLetter( Rand ) + GetRandomNumberString( Rand, 101, 689, "000" );
                case 1:
                    return BasePart + " " + GetRandomUppercaseLetter( Rand ) + GetRandomNumberString( Rand, 2, 87, "00" );
                case 3:
                    return BasePart + " " + GetRandomNumberString( Rand, 2, 87, "00" ) + GetRandomUppercaseLetterPair( Rand, true );
                case 4:
                    return BasePart + " " + GetRandomNumberString( Rand, 101, 889, "000" ) + GetRandomUppercaseLetter( Rand );
                case 5:
                    return BasePart + " " + GetRandomNumberString( Rand, 1, 9, "0" ) + "-" + GetRandomUppercaseLetterPair( Rand, false );
            }
        }
        #endregion

        #region GetRandomUppercaseLetter
        private char GetRandomUppercaseLetter( MersenneTwister Rand )
        {
            return (char)Rand.NextInclus( (int)'A', (int)'Z' );
        }
        #endregion

        #region GetRandomUppercaseLetterPair
        private string GetRandomUppercaseLetterPair( MersenneTwister Rand, bool BothShouldBeSame )
        {
            string letter = GetRandomUppercaseLetter( Rand ).ToString();
            if ( BothShouldBeSame )
                return letter + letter;
            else
                return letter + GetRandomUppercaseLetter( Rand );
        }
        #endregion

        #region GetRandomNumberString
        private string GetRandomNumberString( MersenneTwister Rand, int Lower, int Upper, string DigitString )
        {
            return Rand.NextInclus( Lower, Upper ).ToString( DigitString );
        }
        #endregion
    }
}
