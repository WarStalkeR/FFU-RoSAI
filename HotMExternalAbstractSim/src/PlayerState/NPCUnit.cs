using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.Visualization;

namespace Arcen.HotM.External
{
    internal class NPCUnit : BaseMapActor, ITimeBasedPoolable<NPCUnit>, IProtectedListable, ISimNPCUnit
    {
        //
        //Serialized data
        //-----------------------------------------------------
        public int UnitID = 0;
        public NPCUnitType UnitType { get; private set; }
        public NPCCohort FromCohort { get; private set; }
        #region Stance
        private NPCUnitStance _stance = null;
        private int initialTurnInCurrentStance = -1;

        public NPCUnitStance Stance
        {
            get { return _stance; }
            set
            {
                if ( _stance == value )
                    return;
                _stance = value;
                initialTurnInCurrentStance = SimCommon.Turn;
            }
        }

        public int TurnsInCurrentStance
        {
            get
            {
                if ( initialTurnInCurrentStance < 0 )
                {
                    initialTurnInCurrentStance = SimCommon.Turn;
                    return 0;
                }
                return SimCommon.Turn - initialTurnInCurrentStance;
            }
        }
        #endregion Stance
        #region Objective
        private NPCUnitObjective currentObjective = null;
        public NPCUnitObjective CurrentObjective
        {
            get => this.currentObjective;
            set
            {
                if ( this.currentObjective == value )
                    return;
                this.currentObjective = value;
                this.ObjectiveProgressPoints = 0;
                this.ObjectiveFailureWaitTurns = 0;
                this.NextObjective = null;
            }
        }
        public NPCUnitObjective NextObjective { get; set; }
        public int ObjectiveProgressPoints { get; set; } = 0;
        public int ObjectiveFailureWaitTurns { get; set; } = 0;
        public WrapperedSimBuilding ObjectiveBuilding { get; set; } = new WrapperedSimBuilding( null );
        public ISimMapActor ObjectiveActor { get; set; } = null;
        public bool ObjectiveIsCompleteAndWaiting { get; set; } = false;

        public void WipeAllObjectiveData()
        {
            this.CurrentObjective = null;
            this.NextObjective = null;
            this.ObjectiveActor = null;
            this.ObjectiveBuilding = new WrapperedSimBuilding( null );
            this.ObjectiveProgressPoints = 0;
            this.ObjectiveFailureWaitTurns = 0;
            this.ObjectiveIsCompleteAndWaiting = false;
            //do NOT wipe the attack plan; these are unrelated
        }
        #endregion
        public int TurnsSinceMoved { get; private set; }
        public Vector3 CreationPosition { get; internal set; } = Vector3.zero;
        public string CreationReason { get; private set; } = string.Empty;
        public NPCManager ParentManager { get { return this.IsManagedUnit?.ParentManager; } }
        public CityConflict ParentCityConflict { get { return this.IsCityConflictUnit?.ParentConflict; } }
        public WrapperedSimBuilding ManagerStartLocation { get; set; }
        public int ManagerOriginalMachineActorFocusID { get; set; } = -1;
        public NPCManagedUnit IsManagedUnit { get; set; }
        public CityConflictUnit IsCityConflictUnit { get; set; }
        public Vector3 GroundLocation { get; private set; }
        public WrapperedSimUnitLocation ContainerLocation { get; private set; }
        private Vector3 drawLocation = Vector3.zero;
        public int CurrentSquadSize { get; set; }
        public int LargestSquadSize { get; set; } = 0;
        public int HealthPerUnit = 0;
        public bool IsNPCInFogOfWar { get; set; } = true;
        public NPCActionDesire WantsToPerformAction { get; set; } = NPCActionDesire.None;
        public bool OverridingActionWillWarpOut { get; set; } = false;
        public string LangKeyForDisbandPopup { get; set; } = string.Empty;
        public string SpecialtyActionStringToBeDoneNext { get; set; } = string.Empty;
        public bool NextMoveIsSilent { get; set; }
        public bool DisbandAtTheStartOfNextTurn { get; set; }
        public bool DisbandAsSoonAsNotSelected { get; set; }
        public bool DisbandWhenObjectiveComplete { get; set; }
        public bool HasAttackedYetThisTurn { get; set; } = false;
        public bool IsDowned { get; private set; }
        public bool HasBeenPhysicallyDamagedByPlayer { get; set; }
        public bool HasBeenPhysicallyOrMoraleDamagedByPlayer { get; set; }
        public MapPOI HomePOI { get; set; } = null;
        public MapDistrict HomeDistrict { get; set; } = null;
        public ISimMapActor TargetActor { get; set; }
        public Vector3 TargetLocation { get; set; } = Vector3.negativeInfinity;
        private VisSimpleDrawingObject VisSimpleObject = null;
        private VisLODDrawingObject VisLODObject = null;
        public VisSimpleDrawingObject CustomVisSimpleObject { get; set; } = null;
        public VisLODDrawingObject CustomVisLODObject { get; set; } = null;
        private readonly ThreadsafeTableDictionary32<NPCUnitAccumulator> Accumulators = ThreadsafeTableDictionary32<NPCUnitAccumulator>.Create_WillNeverBeGCed( NPCUnitAccumulatorTable.Instance, "NPCUnit-Accumulators" );
        
        public Vector3 PriorLocation { get; set; } = Vector3.negativeInfinity;
        public Quaternion PriorRotation { get; set; } = Quaternion.identity;

        //
        //NonSerialized data
        //-----------------------------------------------------
        public bool IsFullDead { get; private set; }
        private readonly DoubleBufferedClass<NPCAttackPlan> attackPlan = new DoubleBufferedClass<NPCAttackPlan>( new NPCAttackPlan(), new NPCAttackPlan() );
        public DoubleBufferedClass<NPCAttackPlan> AttackPlan => this.attackPlan;
        public string DebugText { get; set; } = string.Empty;
        public DamageTextPopups.FloatingDamageTextPopup MostRecentDamageText { get; set; } = null;
        private IAutoPooledFloatingLODObject floatingLODObject;
        private IAutoPooledFloatingObject floatingSimpleObject;
        private IAutoPooledFloatingParticleLoop floatingParticleLoop;
        public IAutoPooledFloatingText FloatingText { get; set; } = null;
        private readonly ArcenDoubleCharacterBuffer FloatingTextBuffer = new ArcenDoubleCharacterBuffer( "NPCUnit-FloatingTextBuffer" );
        private Int64 LastFramePrepRendered = -1;
        private Quaternion objectRotation = Quaternion.identity;
        private bool hasEverSetRotation = false;
        private bool hasDoneStompCheckSinceCreationOrLoad = false;

        private bool isMoving = false;
        private float moveProgress = 0;
        private bool isAirDropping = false;
        private float airdropHeight = 0;
        private float airdropSpeed = 0;
        private Vector3 originalSourceLocation = Vector3.zero;
        private Vector3 desiredNewGroundLocation = Vector3.zero;
        private ISimUnitLocation desiredNewContainerLocation = null;
        public MaterializeType Materializing { get; set; } = MaterializeType.None;
        public float MaterializingProgress { get; set; } = 0;


        public NPCRevealer NPCRevealing { get; set; } //this is not really reset or anything later

        private static readonly List<MapItem> staticWorkingBuildings = List<MapItem>.Create_WillNeverBeGCed( 20, "NPCUnit-workingBuildings" );

        public readonly DoubleBufferedList<ISimBuilding> BoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob = DoubleBufferedList<ISimBuilding>.Create_WillNeverBeGCed( 10, "NPCUnit-BoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob", 10 );

        private readonly List<string> turnDumpLines = List<string>.Create_WillNeverBeGCed( 40, "NPCUnit-turnDumpLines" );
        private readonly List<string> targetingDumpLines = List<string>.Create_WillNeverBeGCed( 40, "NPCUnit-targetingDumpLines" );
        public List<string> TurnDumpLines => turnDumpLines;
        public List<string> TargetingDumpLines => targetingDumpLines;

        protected override string DebugStringDescriptor => "npc unit";
        protected override string DebugStringID => this.UnitType?.ID ?? "[null UnitType]";

        protected override void ClearData_TypeSpecific()
        {
            this.UnitID = 0;
            this.UnitType = null;
            this.FromCohort = null;
            this._stance = null;
            this.initialTurnInCurrentStance = -1;
            this.CurrentObjective = null;
            this.NextObjective = null;
            this.ObjectiveProgressPoints = 0;
            this.ObjectiveFailureWaitTurns = 0;
            this.ObjectiveBuilding = new WrapperedSimBuilding( null );
            this.ObjectiveActor = null;
            this.ObjectiveIsCompleteAndWaiting = false;
            this.CreationReason = string.Empty;
            this.TurnsSinceMoved = 0;
            this.CreationPosition = Vector3.zero;
            this.ManagerStartLocation = new WrapperedSimBuilding( null );
            this.ManagerOriginalMachineActorFocusID = -1;
            this.IsManagedUnit = null;
            this.IsCityConflictUnit = null;
            this.GroundLocation = Vector3.zero;
            this.ContainerLocation.Get()?.ClearOccupyingUnitIfThisOne( this );
            this.ContainerLocation = new WrapperedSimUnitLocation( null );
            this.drawLocation = Vector3.zero;
            this.CurrentSquadSize = 0;
            this.LargestSquadSize = 0;
            this.HealthPerUnit = 0;
            this.IsNPCInFogOfWar = true;
            this.WantsToPerformAction = NPCActionDesire.None;
            this.OverridingActionWillWarpOut = false;
            this.LangKeyForDisbandPopup = string.Empty;
            this.SpecialtyActionStringToBeDoneNext = string.Empty;
            this.NextMoveIsSilent = false;
            this.DisbandAtTheStartOfNextTurn = false;
            this.DisbandAsSoonAsNotSelected = false;
            this.DisbandWhenObjectiveComplete = false;
            this.HasAttackedYetThisTurn = false;
            this.IsDowned = false;
            this.HasBeenPhysicallyDamagedByPlayer = false;
            this.HasBeenPhysicallyOrMoraleDamagedByPlayer = false;
            this.HomePOI = null;
            this.HomeDistrict = null;
            this.TargetActor = null;
            this.TargetLocation = Vector3.negativeInfinity;
            this.VisSimpleObject = null;
            this.VisLODObject = null;
            this.CustomVisSimpleObject = null;
            this.CustomVisLODObject = null;
            this.Accumulators.Clear();

            this.PriorLocation = Vector3.negativeInfinity;
            this.PriorRotation = Quaternion.identity;

            this.IsFullDead = false;
            this.attackPlan.ClearAllVersions();
            this.DebugText = string.Empty;
            this.MostRecentDamageText = null;

            if ( this.floatingLODObject != null )
            {
                //this.floatingLODObject.ReturnToPool(); //do not try to do this, it just causes exceptions from background threads
                this.floatingLODObject = null;
            }

            if ( this.floatingSimpleObject != null )
            {
                //this.floatingSimpleObject.ReturnToPool(); //do not try to do this, it just causes exceptions from background threads
                this.floatingSimpleObject = null;
            }

            if ( this.FloatingText != null )
            {
                //this.FloatingText.ReturnToPool(); //do not try to do this, it just causes exceptions from background threads
                this.FloatingText = null;
            }

            if ( this.floatingParticleLoop != null )
            {
                //this.floatingParticleLoop.ReturnToPool(); //do not try to do this, it just causes exceptions from background threads
                this.floatingParticleLoop = null;
            }

            this.LastFramePrepRendered = -1;
            this.objectRotation = Quaternion.identity;
            this.hasEverSetRotation = false;
            this.hasDoneStompCheckSinceCreationOrLoad = false;

            this.isMoving = false;
            this.moveProgress = 0;
            this.isAirDropping = false;
            this.airdropHeight = 0;
            this.airdropSpeed = 0;
            this.originalSourceLocation = Vector3.zero;
            this.desiredNewGroundLocation = Vector3.zero;
            this.desiredNewContainerLocation = null;
            this.Materializing = MaterializeType.None;
            this.MaterializingProgress = 0;

            //this.NPCRevealing; does not need a reset

            BoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob.ClearAllVersions();
            turnDumpLines.Clear();
            targetingDumpLines.Clear();
        }

        #region CreateNew
        public static NPCUnit CreateNew( NPCUnitType UnitType, NPCCohort FromCohort, ISimUnitLocation ContainerLocation,
            int UnitID, float SquadSizeMultiplier, MersenneTwister Rand, bool Materialize, bool IsForDeserialize, string CreationReason )
        {
            NPCUnit unit = NPCUnit.GetFromPoolOrCreate();
            unit.UnitType = UnitType;
            unit.FromCohort = FromCohort;

            if ( UnitID <= 0 )
                UnitID = Interlocked.Increment( ref SimCommon.LastActorID );
            unit.UnitID = UnitID;
            unit.ContainerLocation = new WrapperedSimUnitLocation( ContainerLocation );
            if ( SquadSizeMultiplier > 0 )
                unit.CurrentSquadSize = MathA.Max( 1, Mathf.RoundToInt( UnitType.BasicSquadSize * SquadSizeMultiplier ) );
            else
                unit.CurrentSquadSize = UnitType.BasicSquadSize;

            unit.CreationReason = CreationReason;
            unit.VisLODObject = null;
            unit.VisSimpleObject = null;
            unit.CustomVisSimpleObject = null;
            unit.CustomVisLODObject = null;
            if ( UnitType.IsKeyContact != null )
            {
                UnitType.IsKeyContact.ValidateContact( Rand, true );
                IDrawingObject contactObject = UnitType.IsKeyContact.DuringGame_ChosenDrawingObject;
                if ( contactObject is VisLODDrawingObject lodObj )
                    unit.VisLODObject = lodObj;
                else if ( contactObject is VisSimpleDrawingObject simpleObj )
                    unit.VisSimpleObject = simpleObj;
            }

            if ( unit.VisSimpleObject == null && unit.VisLODObject == null )
            {
                if ( UnitType.DrawingObjectTag.LODObjects.Count > 0 )
                    unit.VisLODObject = UnitType.DrawingObjectTag.LODObjects.GetRandom( Rand );
                else if ( UnitType.DrawingObjectTag.SimpleObjects.Count > 0 )
                    unit.VisSimpleObject = UnitType.DrawingObjectTag.SimpleObjects.GetRandom( Rand );
            }

            if ( unit.VisSimpleObject == null && unit.VisLODObject == null )
                ArcenDebugging.LogSingleLine( "Error A, NPC unit with neither kind of drawing object! UnitType: " + UnitType.ID + "!", Verbosity.ShowAsError );
            
            unit.LargestSquadSize = unit.CurrentSquadSize;

            if ( !IsForDeserialize )
            {
                unit.CurrentTurnSeed = Rand.Next() + 1;
                unit.StatCalculationRandSeed = Rand.Next() + 1;
                unit.InitializeActorData_Randomized( UnitType.ID, UnitType.ActorDataSetUsed, UnitType.ActorData );
            }
            unit.DoPerSecondRecalculations( false ); //make sure any effective maximums are high enough

            unit.HealthPerUnit = MathA.Max( 1, Mathf.FloorToInt( (float)unit.GetActorDataMaximum( ActorRefs.ActorHP, true ) / (float)unit.CurrentSquadSize ) );

            if ( Materialize )
            {
                unit.Materializing = MaterializeType.Appear;
                unit.MaterializingProgress = 0;
            }

            SimCommon.NeedsToAttemptAnotherNPCTargetingPass = true;
            return unit;
        }
        #endregion

        #region Serialize Unit 
        protected override void Serialize_TypeSpecific( ArcenFileSerializer Serializer )
        {
            Serializer.AddInt32( "ID", UnitID );
            Serializer.AddRepeatedlyUsedString_Condensed( "Type", this.UnitType.ID );
            Serializer.AddRepeatedlyUsedString_CondensedIfNotBlank( "Cohort", this.FromCohort?.ID??string.Empty );
            Serializer.AddRepeatedlyUsedString_CondensedIfNotBlank( "Stance", this._stance?.ID??string.Empty );
            Serializer.AddInt32IfGreaterThanZero( "InitialTurnInCurrentStance", initialTurnInCurrentStance );
            Serializer.AddRepeatedlyUsedString_CondensedIfNotBlank( "CurrentObjective", this.currentObjective?.ID??string.Empty );
            Serializer.AddRepeatedlyUsedString_CondensedIfNotBlank( "NextObjective", this.NextObjective?.ID ?? string.Empty );
            Serializer.AddInt32IfGreaterThanZero( "ObjectiveProgressPoints", ObjectiveProgressPoints );
            Serializer.AddInt32IfGreaterThanZero( "ObjectiveFailureWaitTurns", ObjectiveFailureWaitTurns );
            Serializer.AddInt32IfGreaterThanZero( "TurnsSinceMoved", TurnsSinceMoved );
            Serializer.AddBoolIfTrue( "ObjectiveIsCompleteAndWaiting", ObjectiveIsCompleteAndWaiting );

            ISimUnitLocation containerLocation = this.ContainerLocation.Get();
            if ( containerLocation == null )
                Serializer.AddVector3( "GroundLocation", this.GroundLocation );
            else
            {
                if ( containerLocation is MapOutdoorSpot outdoorSpot )
                    Serializer.AddInt32( "ContainerOutdoorSpotID", outdoorSpot.MapOutdoorSpotID );
                else if ( containerLocation is ISimBuilding building )
                    Serializer.AddInt32( "ContainerBuildingID", building.GetBuildingID() );
                else if ( containerLocation is ISimMachineVehicle )
                    ArcenDebugging.LogSingleLine( "NPCUnit: was asked to be in a machine vehicle somehow...!?", Verbosity.ShowAsError );
                else
                    ArcenDebugging.LogSingleLine( "NPCUnit: Unknown ISimUnitLocation container type on save! " + containerLocation.GetType(), Verbosity.ShowAsError );
            }
            Serializer.AddVector3( "drawLocation", this.drawLocation );
            Serializer.AddInt32IfGreaterThanZero( "RotationY", Mathf.RoundToInt( this.objectRotation.eulerAngles.y ) );
            if ( this.CurrentSquadSize != UnitType.BasicSquadSize )
                Serializer.AddInt32( "SquadSizeDiff", this.CurrentSquadSize - UnitType.BasicSquadSize );
            Serializer.AddInt32( "LargestSquadSize", this.LargestSquadSize );
            Serializer.AddInt32( "BasicSquadSize", this.UnitType.BasicSquadSize );
            Serializer.AddInt32( "HealthPerUnit", this.HealthPerUnit );
            Serializer.AddBoolIfTrue( "IsNPCInFogOfWar", this.IsNPCInFogOfWar );
            Serializer.AddInt32IfGreaterThanZero( "WantsToPerformAction", (int)this.WantsToPerformAction );
            Serializer.AddBoolIfTrue( "OverridingActionWillWarpOut", this.OverridingActionWillWarpOut );
            Serializer.AddRepeatedlyUsedString_CondensedIfNotBlank( "LangKeyForDisbandPopup", this.LangKeyForDisbandPopup );
            Serializer.AddRepeatedlyUsedString_CondensedIfNotBlank( "SpecialtyActionStringToBeDoneNext", this.SpecialtyActionStringToBeDoneNext );
            Serializer.AddBoolIfTrue( "NextMoveIsSilent", this.NextMoveIsSilent );
            Serializer.AddBoolIfTrue( "DisbandAtTheStartOfNextTurn", this.DisbandAtTheStartOfNextTurn );
            Serializer.AddBoolIfTrue( "DisbandAsSoonAsNotSelected", this.DisbandAsSoonAsNotSelected );
            Serializer.AddBoolIfTrue( "DisbandWhenObjectiveComplete", this.DisbandWhenObjectiveComplete );
            Serializer.AddBoolIfTrue( "HasAttackedYetThisTurn", this.HasAttackedYetThisTurn );
            Serializer.AddBoolIfTrue( "IsDowned", this.IsDowned );
            Serializer.AddBoolIfTrue( "HasBeenPhysicallyDamagedByPlayer", this.HasBeenPhysicallyDamagedByPlayer );
            Serializer.AddBoolIfTrue( "HasBeenPhysicallyOrMoraleDamagedByPlayer", this.HasBeenPhysicallyOrMoraleDamagedByPlayer );

            if ( this.CustomVisSimpleObject != null )
                Serializer.AddRepeatedlyUsedString_Condensed( "CustomVisSimpleObject", this.CustomVisSimpleObject.ID );
            else if ( this.CustomVisLODObject != null )
                Serializer.AddRepeatedlyUsedString_Condensed( "CustomVisLODObject", this.CustomVisLODObject.ID );
            else if ( this.VisLODObject != null && this.UnitType.DrawingObjectTag.LODObjects.Count > 1 )
                Serializer.AddRepeatedlyUsedString_Condensed( "VisLODObject", this.VisLODObject.ID );
            else if ( this.VisSimpleObject != null && this.UnitType.DrawingObjectTag.SimpleObjects.Count > 1 )
                Serializer.AddRepeatedlyUsedString_Condensed( "VisSimpleObject", this.VisSimpleObject.ID );

            if ( this.TargetActor != null )
                Serializer.AddInt32IfGreaterThanZero( "TargetActor", TargetActor.ActorID );
            if ( this.HomePOI != null )
                Serializer.AddInt16IfGreaterThanZero( "HomePOI", this.HomePOI?.POIID ?? 0 );
            if ( this.HomeDistrict != null )
                Serializer.AddInt32IfGreaterThanZero( "HomeDistrict", this.HomeDistrict?.DistrictID ?? 0 );
            if ( this.TargetLocation.x > float.NegativeInfinity )
                Serializer.AddVector3( "TargetLocation", this.TargetLocation );

            if ( this.ObjectiveBuilding.Get() != null )
                Serializer.AddInt32IfGreaterThanZero( "ObjectiveBuilding", this.ObjectiveBuilding.Get()?.GetBuildingID() ?? 0 );
            if ( this.ObjectiveActor != null )
                Serializer.AddInt32IfGreaterThanZero( "ObjectiveActor", ObjectiveActor.ActorID );
            
            if ( this.ManagerStartLocation.Get() != null )
                Serializer.AddInt32IfGreaterThanZero( "MissionStartLocation", this.ManagerStartLocation.Get()?.GetBuildingID() ?? 0 );
            Serializer.AddInt32IfGreaterThanZero( "ManagerOriginalMachineActorFocusID", this.ManagerOriginalMachineActorFocusID );
            
            Serializer.AddRepeatedlyUsedString_CondensedIfNotBlank( "IsManagedUnit", this.IsManagedUnit?.FullSerializationID??string.Empty );
            Serializer.AddRepeatedlyUsedString_CondensedIfNotBlank( "IsCityConflictUnit", this.IsCityConflictUnit?.FullSerializationID ?? string.Empty );
            Serializer.AddRepeatedlyUsedString_CondensedIfNotBlank( "CreationReason", this.CreationReason );
            Serializer.AddVector3( "CreationPosition", this.CreationPosition );

            if ( this.PriorLocation.x != float.NegativeInfinity )
            {
                Serializer.AddVector3( "PriorLocation", this.PriorLocation );
                Serializer.AddInt32IfGreaterThanZero( "PriorRotation", Mathf.RoundToInt( this.PriorRotation.eulerAngles.y ) );
            }
        }

        private DeserializedObjectLayer dataForLateDeserialize;
        public static NPCUnit Deserialize( DeserializedObjectLayer Data )
        {
            NPCUnitType unitType = Data.GetTableRow( "Type", NPCUnitTypeTable.Instance, false );
            if ( unitType == null )
                return null;

            NPCCohort cohort = Data.GetTableRow( "Cohort", NPCCohortTable.Instance, false );
            if ( cohort == null )
            {
                cohort = Data.GetTableRow( "Team", NPCCohortTable.Instance, false );
                if ( cohort == null )
                    return null; //must have been removed
            }

            NPCUnitStance stance = Data.GetTableRow( "Stance", NPCUnitStanceTable.Instance, false );
            if ( stance == null )
                return null; //must have been removed


            int unitID = Data.GetInt32( "ID", true );
            NPCUnit unit = CreateNew( unitType, cohort, null, unitID, -1f, Engine_Universal.PermanentQualityRandom, false, true, "Deserialize" );
            unit.dataForLateDeserialize = Data;
            unit.AssistInDeserialize_BaseMapActor_Primary( unit.UnitType.ID, unitType.ActorDataSetUsed, Data, unitType.ActorData, null );

            unit._stance = stance;
            unit.initialTurnInCurrentStance = Data.GetInt32( "InitialTurnInCurrentStance", false );

            if ( Data.TryGetTableRow( "CurrentObjective", NPCUnitObjectiveTable.Instance, out NPCUnitObjective currentObjective ) )
            {
                unit.CurrentObjective = currentObjective; //if this is done after NextObjective, it will wipe it out, be careful
                unit.ObjectiveProgressPoints = Data.GetInt32( "ObjectiveProgressPoints", false );
            }
            unit.ObjectiveFailureWaitTurns = Data.GetInt32( "ObjectiveFailureWaitTurns", false );
            unit.ObjectiveIsCompleteAndWaiting = Data.GetBool( "ObjectiveIsCompleteAndWaiting", false );

            if ( Data.TryGetTableRow( "NextObjective", NPCUnitObjectiveTable.Instance, out NPCUnitObjective nextObjective ) )
                unit.NextObjective = nextObjective;

            if ( Data.TryGetUnityVector3( "GroundLocation", out Vector3 groundLocation ) )
                unit.SetActualGroundLocation( groundLocation ); //must be done BEFORE TurnsSinceMoved and HasMovedThisTurn

            if ( Data.TryGetUnityVector3( "drawLocation", out Vector3 drawLocation ) )
                unit.drawLocation = drawLocation;

            if ( Data.TryGetInt32( "ContainerOutdoorSpotID", out int ContainerOutdoorSpotID ) )
            {
                MapOutdoorSpot containerSpot = World_OutdoorSpots.OutdoorSpotsByID[ContainerOutdoorSpotID];
                if ( containerSpot != null )
                    unit.SetActualContainerLocation( containerSpot );
                else
                {
                    unit.SetActualGroundLocation( unit.drawLocation ); //must be done BEFORE TurnsSinceMoved and HasMovedThisTurn

                    if (SimCommon.Turn > 1 )
                        ArcenDebugging.LogSingleLine( "NPCUnit: Asked for MapOutdoorSpot with ID " + ContainerOutdoorSpotID + ", but not found (" +
                            World_OutdoorSpots.OutdoorSpotsByID.Count + " spots).", Verbosity.ShowAsError );
                }
            }

            if ( Data.TryGetInt32( "ContainerBuildingID", out int ContainerBuildingID ) )
            {
                SimBuilding building = World_Buildings.BuildingsByID[ContainerBuildingID];
                if ( building != null )
                    unit.SetActualContainerLocation( building ); //must be done BEFORE TurnsSinceMoved and HasMovedThisTurn
                else
                    ArcenDebugging.LogSingleLine( "NPCUnit: Asked for ISimBuilding with ID " + ContainerBuildingID + ", but not found (" +
                        World_Buildings.BuildingsByID.Count + " buildings).", Verbosity.ShowAsError );
            }

            unit.TurnsSinceMoved = Data.GetInt32( "TurnsSinceMoved", false );

            if ( Data.TryGetInt32( "RotationY", out int rotationY ) )
            {
                unit.objectRotation = Quaternion.Euler( 0, rotationY, 0 );
                unit.hasEverSetRotation = true;
            }

            if ( Data.TryGetInt32( "SquadSizeDiff", out int squadSizeDiff ) )
            {
                unit.CurrentSquadSize = unitType.BasicSquadSize + squadSizeDiff;
                //do not adjust the stats of this unit, since whatever they were saved as should be equivalent
            }

            unit.LargestSquadSize = Data.GetInt32( "LargestSquadSize", false );
            if ( Data.TryGetInt32( "BasicSquadSize", out int basicSquadSize ) )
            {
                int diff = unitType.BasicSquadSize - basicSquadSize;
                unit.LargestSquadSize += diff;
                unit.CurrentSquadSize += diff;
            }
            else
            {
                int diff = unitType.BasicSquadSize - unit.LargestSquadSize;
                if ( diff > 0 ) //this is a legacy case
                {
                    unit.LargestSquadSize += diff;
                    unit.CurrentSquadSize += diff;

                    //ArcenDebugging.LogSingleLine( unitType.ID + " change squad size by " + diff, Verbosity.DoNotShow );
                }
            }

            unit.HealthPerUnit = Data.GetInt32( "HealthPerUnit", false );
            
            unit.IsNPCInFogOfWar = Data.GetBool( "IsNPCInFogOfWar", false );
            unit.WantsToPerformAction = (NPCActionDesire)Data.GetInt32( "WantsToPerformAction", false );
            unit.OverridingActionWillWarpOut = Data.GetBool( "OverridingActionWillWarpOut", false );
            unit.LangKeyForDisbandPopup = Data.GetString( "LangKeyForDisbandPopup", false );
            unit.SpecialtyActionStringToBeDoneNext = Data.GetString( "SpecialtyActionStringToBeDoneNext", false );
            unit.NextMoveIsSilent = Data.GetBool( "NextMoveIsSilent", false );
            unit.DisbandAtTheStartOfNextTurn = Data.GetBool( "DisbandAtTheStartOfNextTurn", false );
            unit.DisbandAsSoonAsNotSelected = Data.GetBool( "DisbandAsSoonAsNotSelected", false );
            unit.DisbandWhenObjectiveComplete = Data.GetBool( "DisbandWhenObjectiveComplete", false );
            unit.HasAttackedYetThisTurn = Data.GetBool( "HasAttackedYetThisTurn", false );
            unit.IsDowned = Data.GetBool( "IsDowned", false );
            unit.HasBeenPhysicallyDamagedByPlayer = Data.GetBool( "HasBeenPhysicallyDamagedByPlayer", false );
            unit.HasBeenPhysicallyOrMoraleDamagedByPlayer = Data.GetBool( "HasBeenPhysicallyOrMoraleDamagedByPlayer", false );

            if ( Data.TryGetUnityVector3( "TargetLocation", out Vector3 targetLocation ) )
                unit.TargetLocation = targetLocation;
            else
                unit.TargetLocation = Vector3.negativeInfinity;

            if ( Data.TryGetTableRow( "CustomVisSimpleObject", VisSimpleDrawingObjectTable.Instance, out VisSimpleDrawingObject customSimpleObj ) )
            {
                unit.VisSimpleObject = customSimpleObj;
                unit.CustomVisSimpleObject = customSimpleObj;
            }
            else if ( Data.TryGetTableRow( "CustomVisLODObject", VisLODDrawingObjectTable.Instance, out VisLODDrawingObject customLODObj ) )
            {
                unit.VisLODObject = customLODObj;
                unit.CustomVisLODObject = customLODObj;
            }
            else if ( unit.UnitType.DrawingObjectTag.LODObjects.Count > 0 )
            {
                if ( unit.UnitType.DrawingObjectTag.LODObjects.Count > 1 )
                {
                    if ( Data.TryGetTableRow( "VisLODObject", VisLODDrawingObjectTable.Instance, out VisLODDrawingObject lodObj ) )
                        unit.VisLODObject = lodObj;
                    else
                        unit.VisLODObject = unit.UnitType.DrawingObjectTag.LODObjects.GetRandom( Engine_Universal.PermanentQualityRandom );
                }
                else
                    unit.VisLODObject = unit.UnitType.DrawingObjectTag.LODObjects[0];
            }
            else if ( unit.UnitType.DrawingObjectTag.SimpleObjects.Count > 0 )
            {
                if ( unit.UnitType.DrawingObjectTag.SimpleObjects.Count > 1 )
                {
                    if ( Data.TryGetTableRow( "VisSimpleObject", VisSimpleDrawingObjectTable.Instance, out VisSimpleDrawingObject simpleObj ) )
                        unit.VisSimpleObject = simpleObj;
                    else
                        unit.VisSimpleObject = unit.UnitType.DrawingObjectTag.SimpleObjects.GetRandom( Engine_Universal.PermanentQualityRandom );
                }
                else
                    unit.VisSimpleObject = unit.UnitType.DrawingObjectTag.SimpleObjects[0];
            }

            if ( unit.VisSimpleObject == null && unit.VisLODObject == null )
                ArcenDebugging.LogSingleLine( "Error B, NPC unit with neither kind of drawing object! UnitType: " + unit.UnitType.ID + "!", Verbosity.ShowAsError );

            if ( Data.TryGetUnityVector3( "PriorLocation", out Vector3 priorLocation ) )
            {
                unit.PriorLocation = priorLocation;
                if ( Data.TryGetInt32( "PriorRotation", out int priorRotationY ) )
                    unit.PriorRotation = Quaternion.Euler( 0, priorRotationY, 0 );
            }

            if ( unit.LargestSquadSize < unit.CurrentSquadSize )
                unit.LargestSquadSize = unit.CurrentSquadSize;
            if ( unit.UnitType.SquadmatesAreNotLostFromDamage )
            {
                if ( unit.CurrentSquadSize < unit.LargestSquadSize )
                    unit.CurrentSquadSize = unit.LargestSquadSize;
            }

            unit.DoPerSecondRecalculations( true ); //this will correct any effective maximums that are too low

            if ( unit.HealthPerUnit <= 0 )
                unit.HealthPerUnit = MathA.Max( 1, Mathf.FloorToInt( (float)unit.GetActorDataMaximum( ActorRefs.ActorHP, true ) / (float)unit.CurrentSquadSize ) );

            if ( Data.TryGetFromIndexedDictionary( "IsManagedUnit", NPCManagedUnit.ManagedUnitsByID, out NPCManagedUnit managedUnit ) )
                unit.IsManagedUnit = managedUnit;

            if ( Data.TryGetFromIndexedDictionary( "IsCityConflictUnit", CityConflictUnit.CityConflictUnitsByID, out CityConflictUnit conflictUnit ) )
                unit.IsCityConflictUnit = conflictUnit;

            unit.CreationReason = Data.GetString( "CreationReason", false );

            if ( Data.TryGetUnityVector3( "CreationPosition", out Vector3 creationPosition ) )
                unit.CreationPosition = creationPosition;
            else
                unit.CreationPosition = unit.drawLocation;

            //TargetActor must be handled in FinishDeserializeLate because it may not be deserialized yet

            if ( Data.TryGetInt16( "HomePOI", out Int16 homePOIID ) )
                unit.HomePOI = CityMap.POIsByID[homePOIID];

            if ( Data.TryGetDictionary( "Accumulators", out Dictionary<string, int> accumulatorDict ) )
            {
                foreach ( KeyValuePair<string, int> kv in accumulatorDict )
                {
                    NPCUnitAccumulator accumulator = NPCUnitAccumulatorTable.Instance.GetRowByIDOrNullIfNotFound( kv.Key );
                    if ( accumulator == null )
                        continue; //this is okay, must be an old type

                    unit.Accumulators.Set_BeVeryCarefulOfUsing( accumulator, kv.Value );
                }
            }

            //FocusBuilding must be handled in FinishDeserializeLate because it is definitely not deserialized yet

            World_Forces.AllNPCUnitsByID[unit.UnitID] = unit;
            SimCommon.AllActorsByID[unit.UnitID] = unit;

            if ( unit.IsManagedUnit != null )
                World_Forces.ManagedNPCUnitsByID[unit.UnitID] = unit;
            if ( unit.IsCityConflictUnit != null )
                World_Forces.CityConflictNPCUnitsByID[unit.UnitID] = unit;

            CityMap.CollidablesCreatedSinceLastVisibilityGranterCalculation.AddEntry(
                RefPair<ICollidable, Int64>.Create( unit, SimCommon.VisibilityGranterCycle ) );

            return unit;
        }

        public void FinishDeserializeLate()
        {
            if ( this.dataForLateDeserialize.TryGetInt32( "TargetActor", out int TargetUnitID ) )
                this.TargetActor = SimCommon.AllActorsByID[TargetUnitID];

            if ( this.dataForLateDeserialize.TryGetInt32( "ObjectiveBuilding", out int ObjectiveBuildingID ) )
                this.ObjectiveBuilding = new WrapperedSimBuilding( World_Buildings.BuildingsByID[ObjectiveBuildingID] );

            if ( this.dataForLateDeserialize.TryGetInt32( "MissionStartLocation", out int managerStartLocationID ) )
                this.ManagerStartLocation = new WrapperedSimBuilding( World_Buildings.BuildingsByID[managerStartLocationID] );

            if ( this.dataForLateDeserialize.TryGetInt32( "ManagerOriginalMachineActorFocusID", out int managerOriginalMachineActorFocusID ) )
                this.ManagerOriginalMachineActorFocusID = managerOriginalMachineActorFocusID;
            else
                this.ManagerOriginalMachineActorFocusID = -1;            

            if ( this.dataForLateDeserialize.TryGetInt32( "ObjectiveActor", out int ObjectiveUnitID ) )
                this.ObjectiveActor = SimCommon.AllActorsByID[ObjectiveUnitID];
                        
            if ( this.dataForLateDeserialize.TryGetInt16( "HomeDistrict", out Int16 homeDistrictID ) )
                this.HomeDistrict = CityMap.DistrictsByID[homeDistrictID];

            this.AssistInDeserialize_BaseMapActor_Late( dataForLateDeserialize );
            this.dataForLateDeserialize = null;
        }
        #endregion

        public int ActorID { get { return this.UnitID; } }

        public int GetInt32ValueForSerialization()
        {
            return this.UnitID;
        }

        public override Vector3 GetDrawLocation()
        {
            Vector3 loc = this.drawLocation;
            NPCUnitType unitType = this.UnitType;
            if ( unitType != null )
            {
                bool isMapView = Engine_HotM.GameMode == MainGameMode.CityMap;
                if ( unitType.EntireObjectAlwaysThisHeightAboveGround > 0 )
                    loc = loc.ReplaceY( unitType.EntireObjectAlwaysThisHeightAboveGround ).PlusY( unitType.VisObjectExtraOffset + (isMapView ? unitType.VisObjectExtraOffsetOnCityMap : 0f) );
                else
                    loc = loc.PlusY( unitType.VisObjectExtraOffset + (isMapView ? unitType.VisObjectExtraOffsetOnCityMap : 0f) );
            }

            if ( this.isAirDropping )
                loc.y += this.airdropHeight;

            return loc;
        }

        public Quaternion GetDrawRotation()
        {
            return this.objectRotation;
        }

        public bool GetIsValidForCollisions()
        {
            if ( this.IsFullDead || this.IsDowned )
                return false;
            return true;
        }

        public bool GetIsInFogOfWar()
        {
            return this.IsNPCInFogOfWar;
        }

        public void AlterAccumulatorAmount( NPCUnitAccumulator Accumulator, int ByAmount )
        {
            if ( Accumulator == null )
                return;
            if ( ByAmount == 0 ) 
                return;
            if ( ByAmount < 0 )
            {
                if ( this.Accumulators[Accumulator] <= 0 )
                    return;
            }

            int newVal = this.Accumulators.Add( Accumulator, ByAmount );
            if ( newVal < 0 )
                this.Accumulators.Add( Accumulator , -newVal ); //zero it out, if was negative
        }

        public int GetAccumulatorAmount( NPCUnitAccumulator Accumulator )
        {
            if ( Accumulator == null )
                return 0;

            return this.Accumulators[Accumulator];
        }

        public void DoOnPostHitWithHostileAction( ISimMapActor HostileActionSource, int IntensityOfNegativeAction, MersenneTwister Rand, bool ShouldDoDamageTextPopupsAndLogging )
        {
            //NOTE: make sure everything in here is consistent in DoOnPostTakeDamage

            NPCUnitStance stance = this.Stance;
            if ( stance != null )
            {
                MapPOI poiOfGuard = this.HomePOI;
                if ( stance.WhenDamagedIncreaseAlarmOfPOIForAtLeastXTurns.Min > 0 && poiOfGuard != null )
                {
                    if ( HostileActionSource?.GetIsPlayerControlled()??false )
                        poiOfGuard.IncreasePOIAlarmUntilAtLeastTurn_Player( SimCommon.Turn +
                            stance.WhenDamagedIncreaseAlarmOfPOIForAtLeastXTurns.GetRandom( Rand ), true );
                    else
                        poiOfGuard.IncreasePOIAlarmUntilAtLeastTurn_ThirdParty( SimCommon.Turn +
                            stance.WhenDamagedIncreaseAlarmOfPOIForAtLeastXTurns.GetRandom( Rand ), true );
                }

                stance.Implementation?.HandleLogicForNPCUnitInStance( this, this.Stance,
                    NPCUnitStanceLogic.TookDamage, Rand, null, HostileActionSource, null );

                if ( stance.ImmediatelyDoActionWhenDamagedByAny != null )
                    stance.ImmediatelyDoActionWhenDamagedByAny.Implementation.TryHandleActionLogicForNPCUnit( this, null, 
                        stance.ImmediatelyDoActionWhenDamagedByAny, Rand, NPCUnitActionLogic.ReactingToHostileAction, null );

                if ( stance.ImmediatelyDoActionWhenDamagedByPlayer != null && HostileActionSource.GetIsPartOfPlayerForcesInAnyWay() )
                    stance.ImmediatelyDoActionWhenDamagedByPlayer.Implementation.TryHandleActionLogicForNPCUnit( this, null,
                        stance.ImmediatelyDoActionWhenDamagedByPlayer, Rand, NPCUnitActionLogic.ReactingToHostileAction, null );

                NPCUnitStance switchTo = stance.SwitchToStanceWhenDamaged;
                if ( switchTo != null )
                    this.Stance = switchTo;
            }

            //skip DoDeathCheck

            if ( this.FromCohort != null )
            {
                float aggroMultiplier = 1f;
                if ( HostileActionSource is ISimNPCUnit )
                    aggroMultiplier = 2f; //NPCs naturally pull 200% as much aggro as your units

                //the more intimidation a unit has, the more their damage-based aggro counts
                int intimidationOfAttacker = HostileActionSource.GetActorDataCurrent( ActorRefs.UnitIntimidation, true );
                if ( intimidationOfAttacker > 0 )
                {
                    float intimidationMultiplier = 1f + ((float)intimidationOfAttacker / 100f);
                    aggroMultiplier *= intimidationMultiplier;
                }

                HostileActionSource.AlterAggroOfNPCCohort( this.FromCohort, MathA.Max( 1, Mathf.RoundToInt( IntensityOfNegativeAction * aggroMultiplier ) ) );

                if ( HostileActionSource is ISimMachineUnit HostileActionSourceMachineUnit )
                {
                    if ( HostileActionSourceMachineUnit.ContainerLocation.Get() is ISimMachineVehicle containerVehicle )
                        containerVehicle.AlterAggroOfNPCCohort( this.FromCohort, MathA.Max( 1, Mathf.RoundToInt( IntensityOfNegativeAction * aggroMultiplier ) ) );
                }
            }

            //skip more death stuff

            this.IsManagedUnit?.OnManagedUnitDamaged( this, HostileActionSource, Rand, 0 );
            this.IsCityConflictUnit?.OnUnitDamaged( this, HostileActionSource, Rand, 0, true );

            //do this immediately, so that damage output is lowered if it needs to be
            this.RecalculateActorStats( false );

            if ( HostileActionSource is ISimMachineActor ) //since viable targets are now probably different
                SimCommon.NeedsToAttemptAnotherNPCTargetingPass = true;

            //NOTE: make sure everything in here is consistent in DoOnPostTakeDamage
        }

        public void DoOnPostTakeDamage( ISimMapActor DamageSource, int PhysicalDamageAmount, int MoraleDamageAmount, int SquadMembersLost, MersenneTwister Rand, bool ShouldDoDamageTextPopupsAndLogging )
        {
            //NOTE: make sure everything in here is consistent in DoOnPostHitWithHostileAction
            bool wasAlreadyDead = this.IsFullDead;

            NPCUnitType unitType = this.UnitType;

            NPCUnitStance stance = this.Stance;
            if ( stance != null )
            {
                MapPOI poiOfGuard = this.HomePOI;
                if ( stance.WhenDamagedIncreaseAlarmOfPOIForAtLeastXTurns.Min > 0 && poiOfGuard != null )
                {
                    if ( DamageSource?.GetIsPlayerControlled()??false )
                        poiOfGuard.IncreasePOIAlarmUntilAtLeastTurn_Player( SimCommon.Turn +
                            stance.WhenDamagedIncreaseAlarmOfPOIForAtLeastXTurns.GetRandom( Rand ), true );
                    else
                        poiOfGuard.IncreasePOIAlarmUntilAtLeastTurn_ThirdParty( SimCommon.Turn +
                            stance.WhenDamagedIncreaseAlarmOfPOIForAtLeastXTurns.GetRandom( Rand ), true );
                }

                stance.Implementation?.HandleLogicForNPCUnitInStance( this, this.Stance,
                    NPCUnitStanceLogic.TookDamage, Rand, null, DamageSource, null );

                if ( stance.ImmediatelyDoActionWhenDamagedByAny != null )
                    stance.ImmediatelyDoActionWhenDamagedByAny.Implementation.TryHandleActionLogicForNPCUnit( this, null,
                        stance.ImmediatelyDoActionWhenDamagedByAny, Rand, NPCUnitActionLogic.ReactingToDamage, null );

                if ( stance.ImmediatelyDoActionWhenDamagedByPlayer != null && DamageSource.GetIsPartOfPlayerForcesInAnyWay() )
                    stance.ImmediatelyDoActionWhenDamagedByPlayer.Implementation.TryHandleActionLogicForNPCUnit( this, null,
                        stance.ImmediatelyDoActionWhenDamagedByPlayer, Rand, NPCUnitActionLogic.ReactingToDamage, null );

                NPCUnitStance switchTo = stance.SwitchToStanceWhenDamaged;
                if ( switchTo != null )
                    this.Stance = switchTo;
            }

            if ( unitType != null && DamageSource is ISimMachineUnit )
            {
                if ( unitType.IsVehicle )
                    HandbookRefs.VehicleCrews.DuringGame_UnlockIfNeeded( true );
                else if ( unitType.IsMechStyleMovement )
                {}
                else
                {
                    if ( unitType.SquadmatesAreNotLostFromDamage )
                        HandbookRefs.SquadsOfAndroids.DuringGame_UnlockIfNeeded( true );
                    else
                        HandbookRefs.SquadsOfHumans.DuringGame_UnlockIfNeeded( true );
                }
            }

            if ( SquadMembersLost > 0 && unitType != null )
            {
                if ( unitType.DeathsReduceStatistic != null )
                    unitType.DeathsReduceStatistic.AlterScore_CityOnly( -MathA.Min( (int)unitType.DeathsReduceStatistic.GetScore(), SquadMembersLost ) );

                if ( unitType.DeathsByAnySourceIncreaseStatistic != null )
                    unitType.DeathsByAnySourceIncreaseStatistic.AlterScore_CityOnly( SquadMembersLost );

                if ( DamageSource.GetIsPartOfPlayerForcesInAnyWay() )
                {
                    unitType.KillsByPlayerIncreaseStatistic1?.AlterScore_CityAndMeta( SquadMembersLost );
                    unitType.KillsByPlayerIncreaseStatistic2?.AlterScore_CityAndMeta( SquadMembersLost );
                    unitType.KillsByPlayerIncreaseStatistic3?.AlterScore_CityAndMeta( SquadMembersLost );
                }
                else
                {
                    unitType.KillsByOthersIncreaseStatistic?.AlterScore_CityAndMeta( SquadMembersLost );
                }
            }

            this.IsManagedUnit?.OnManagedUnitDamaged( this, DamageSource, Rand, PhysicalDamageAmount );
            this.IsCityConflictUnit?.OnUnitDamaged( this, DamageSource, Rand, PhysicalDamageAmount, true );

            if ( this.FromCohort != null )
            {
                float aggroMultiplier = 1f;
                if ( DamageSource is ISimNPCUnit )
                    aggroMultiplier = 2f; //NPCs naturally pull 200% as much aggro as your units

                //the more intimidation a unit has, the more their damage-based aggro counts
                int intimidationOfAttacker = DamageSource.GetActorDataCurrent( ActorRefs.UnitIntimidation, true );
                if ( intimidationOfAttacker > 0 )
                {
                    float intimidationMultiplier = 1f + ((float)intimidationOfAttacker / 100f);
                    aggroMultiplier *= intimidationMultiplier;
                }

                DamageSource.AlterAggroOfNPCCohort( this.FromCohort, MathA.Max( 1, Mathf.RoundToInt( (PhysicalDamageAmount + MoraleDamageAmount) * aggroMultiplier ) ) );

                if ( DamageSource is ISimMachineUnit DamageSourceMachineUnit )
                {
                    if ( DamageSourceMachineUnit.ContainerLocation.Get() is ISimMachineVehicle containerVehicle )
                        containerVehicle.AlterAggroOfNPCCohort( this.FromCohort, MathA.Max( 1, Mathf.RoundToInt( (PhysicalDamageAmount + MoraleDamageAmount) * aggroMultiplier ) ) );
                }
            }
            this.DoDeathCheck( Rand, ShouldDoDamageTextPopupsAndLogging );

            if ( this.IsFullDead && !wasAlreadyDead && DamageSource != null )
            {
                if ( DamageSource.GetIsPartOfPlayerForcesInAnyWay() )
                    unitType?.OnUnitKilledByPlayerControlledUnit( DamageSource, Rand );

                if ( unitType != null && ( unitType.IsMechStyleMovement || unitType.IsVehicle ) )
                {
                    if ( DamageSource is ISimNPCUnit npcUnit && ( npcUnit?.UnitType?.ID??string.Empty) == "PlayerParkourBear" )
                        AchievementRefs.BearAttack.TripIfNeeded();
                }
            }

            //do this immediately, so that damage output is lowered if it needs to be
            this.RecalculateActorStats( false );

            if ( DamageSource is ISimMachineActor ) //since health is different now
                SimCommon.NeedsToAttemptAnotherNPCTargetingPass = true;

            //NOTE: make sure everything in here is consistent in DoOnPostHitWithHostileAction
        }

        private static MersenneTwister deathCheckRand = new MersenneTwister( 0 );
        public override void DoDeathCheck( MersenneTwister Rand, bool ShouldDoDamageTextPopupsAndLogging )
        {
            if ( this.IsFullDead )
                return;

            MapActorData moraleData = this.GetActorDataData( ActorRefs.UnitMorale, true );

            bool ranOutOfMorale = moraleData != null && moraleData.Maximum > 0 && moraleData.Current <= 0;
            bool hasAnyHealthLeft = this.GetActorDataCurrent( ActorRefs.ActorHP, false ) > 0;

            if ( hasAnyHealthLeft && !ranOutOfMorale )
                return; //if either HP or morale falls to zero, we dead

            if ( SimCommon.NPCsWaitingToShoot_MainThreadOnly.Count > 0 ) //only true during that firing phase
            {
                NPCAttackPlan attackPlan = this.AttackPlan.Display;
                if ( attackPlan.TargetForStartOfNextTurn != null && attackPlan.LastNPCFiringPassAttempted != SimCommon.CurrentNPCFiringPassAuthorized && !this.HasAttackedYetThisTurn )
                {
                    this.AttackChosenTarget_MainThreadOnly( deathCheckRand, true );
                }
            }

            bool isVisible = !this.IsNPCInFogOfWar;

            //oh no, we died!
            this.IsFullDead = true;
            if ( isVisible )
            {
                int particleCount = (FrameBufferManagerData.ParticleCount.Display + Engine_HotM.NonSim_TotalParticleEffectsPlayingNow);
                bool dropMoreParticles = particleCount > 160 ? Engine_Universal.PermanentQualityRandom.Next( 0, 40000 ) > particleCount * particleCount : //200 squared
                    Engine_Universal.PermanentQualityRandom.Next( 0, 160 ) > particleCount;
                if ( dropMoreParticles )
                {
                    this.UnitType.OnDeath.DuringGame_PlayAtLocation( this.GetEmissionLocation() );
                }
            }

            this.IsManagedUnit?.OnManagedUnitDeath( this, Rand );
            this.IsCityConflictUnit?.OnUnitDeath( this, Rand, !hasAnyHealthLeft );

            if ( ShouldDoDamageTextPopupsAndLogging )
            {
                bool isPlayerRelatedUnit = this.GetIsPartOfPlayerForcesInAnyWay();
                ArcenNotes.SendSimpleNoteToGameOnly( 300, ranOutOfMorale ? NoteRefs.NPCUnitDisbandedFromMoraleLoss : NoteRefs.NPCUnitDied, 
                    isPlayerRelatedUnit ? NoteStyle.BothGame : NoteStyle.StoredGame, 
                    this.UnitType.ID, this.FromCohort.ID, string.Empty, string.Empty, 0, 0, 0,
                    string.Empty, string.Empty, string.Empty,
                    SimCommon.Turn + (isPlayerRelatedUnit ? 20 : 5) );
            }

            if ( isVisible )
                this.SpawnBurningItemAtCurrentLocation();

            if ( Engine_HotM.SelectedActor != this )
                this.ReturnToPool(); //if the player has this selected, then don't do this yet

            SimCommon.NeedsVisibilityGranterRecalculation = true;
            SimCommon.NeedsToAttemptAnotherNPCTargetingPass = true;
        }

        public void DoAlternativeToDeathPositiveItems( ISimMapActor DamageSource, MersenneTwister Rand, bool ShouldDoDamageTextPopupsAndLogging )
        {
            if ( DamageSource != null && DamageSource.GetIsPartOfPlayerForcesInAnyWay() )
                this.UnitType?.OnUnitKilledByPlayerControlledUnit( DamageSource, Rand );

            this.IsManagedUnit?.OnManagedUnitDamaged( this, DamageSource, Rand, 0 );
            this.IsCityConflictUnit?.OnUnitDamaged( this, DamageSource, Rand, 0, true );

            this.IsManagedUnit?.OnManagedUnitDeath( this, Rand );
            this.IsCityConflictUnit?.OnUnitDeath( this, Rand, false );
        }

        public void DisbandNPCUnit( NPCDisbandReason Reason )
        {
            if ( this.IsFullDead )
            {
                if ( Engine_HotM.SelectedActor != this )
                    this.ReturnToPool(); //if the player has this selected, then don't do this yet

                return; //already dead, don't duplicate it
            }

            bool warpOut = false;
            switch ( Reason )
            {
                case NPCDisbandReason.WantedToLeave:
                    warpOut = true;
                    //nothing to log here in the log
                    break;
                case NPCDisbandReason.PlayerRequestForOwnUnit:
                    warpOut = false;
                    //nothing to log here in the log
                    break;
                case NPCDisbandReason.Cheat:
                    ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteRefs.NPCUnitDied, NoteStyle.StoredGame, this.UnitType.ID, this.FromCohort.ID, string.Empty, string.Empty, 0, 0, 0,
                        string.Empty, string.Empty, string.Empty,
                        SimCommon.Turn + 5 );
                    break;
                case NPCDisbandReason.CaughtInExplosion:
                    ArcenNotes.SendSimpleNoteToGameOnly( 300, NoteRefs.NPCUnitDiedInExplosion, this.GetIsPlayerControlled() ? NoteStyle.BothGame : NoteStyle.StoredGame, this.UnitType.ID, this.FromCohort.ID, string.Empty, string.Empty, 0, 0, 0,
                        string.Empty, string.Empty, string.Empty,
                        SimCommon.Turn + 5 );
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "Oops!  There was nothing set up to log the disband-npc-unit logic for reason '" + Reason + "'!", Verbosity.ShowAsError );
                    break;
            }

            this.IsFullDead = true;
            if ( warpOut )
            {
                if ( Engine_HotM.GameMode == MainGameMode.Streets )
                    this.SpawnWarpOutItemAtCurrentLocation();
            }
            else
                this.SpawnBurningItemAtCurrentLocation();

            if ( Engine_HotM.SelectedActor != this )
                this.ReturnToPool(); //if the player has this selected, then don't do this yet

            SimCommon.NeedsVisibilityGranterRecalculation = true;
            SimCommon.NeedsToAttemptAnotherNPCTargetingPass = true;
        }

        #region SpawnBurningItemAtCurrentLocation
        public void SpawnBurningItemAtCurrentLocation()
        {
            if ( this.floatingLODObject != null )
            {
                if ( this.CustomVisLODObject != null )
                    this.VisLODObject = this.CustomVisLODObject;
                VisLODDrawingObject toDraw = this.VisLODObject;
                if ( toDraw == null )
                    return;

                Vector3 position = this.floatingLODObject.WorldLocation;
                if ( position.x == 0 && position.z == 0 )
                    return;
                if ( float.IsNaN( position.x ) )
                    return;

                MapMaterializingItem materializingItem = MapMaterializingItem.GetFromPoolOrCreate();

                materializingItem.Position = position;
                materializingItem.Rotation = this.floatingLODObject.Rotation;
                materializingItem.Scale = this.floatingLODObject.EffectiveScale;

                materializingItem.RendererGroup = toDraw.LODRenderGroups[toDraw.LODRenderGroups.Count - 1];
                materializingItem.Materializing = this.GetBurnDownType();

                MapEffectCoordinator.AddMaterializingItem( materializingItem );

                //this.floatingLODObject.ReturnToPool(); //do not try to do this, it just causes exceptions from background threads
                this.floatingLODObject = null;
            }
            else if ( this.floatingSimpleObject != null )
            {
                if ( this.CustomVisSimpleObject != null )
                    this.VisSimpleObject = this.CustomVisSimpleObject;
                VisSimpleDrawingObject toDraw = this.VisSimpleObject;
                if ( toDraw == null )
                    return;

                Vector3 position = this.floatingSimpleObject.WorldLocation;
                if ( position.x == 0 && position.z == 0 )
                    return;
                if ( float.IsNaN( position.x ) )
                    return;

                MapMaterializingItem materializingItem = MapMaterializingItem.GetFromPoolOrCreate();

                materializingItem.Position = position;
                materializingItem.Rotation = this.floatingSimpleObject.Rotation;
                materializingItem.Scale = this.floatingSimpleObject.EffectiveScale;

                materializingItem.RendererGroup = toDraw.RendererGroup;
                materializingItem.Materializing = this.GetBurnDownType();

                MapEffectCoordinator.AddMaterializingItem( materializingItem );

                //this.floatingSimpleObject.ReturnToPool(); //do not try to do this, it just causes exceptions from background threads
                this.floatingSimpleObject = null;
            }
        }
        #endregion

        public MaterializeType GetBurnDownType()
        {
            NPCUnitType unitType = this.UnitType;
            if ( this.floatingLODObject != null )
            {
                if ( this.CustomVisLODObject != null )
                    this.VisLODObject = this.CustomVisLODObject;
                VisLODDrawingObject toDraw = this.VisLODObject;
                if ( toDraw == null )
                    return MaterializeType.LowEventCamera_OtherBurnDown;
                if ( unitType == null )
                    return MaterializeType.BurnDownSmall;
                return unitType.IsMechStyleMovement || unitType.IsVehicle ? MaterializeType.BurnDownLarge : MaterializeType.BurnDownSmall;
            }
            else if ( this.floatingSimpleObject != null )
            {
                if ( this.CustomVisSimpleObject != null )
                    this.VisSimpleObject = this.CustomVisSimpleObject;
                VisSimpleDrawingObject toDraw = this.VisSimpleObject;
                if ( toDraw == null )
                    return MaterializeType.LowEventCamera_OtherBurnDown;
                if ( unitType == null )
                    return MaterializeType.BurnDownSmall;
                return toDraw.UseUVFreeBurnDownAnimation ? MaterializeType.LowEventCamera_OtherBurnDown :
                    (unitType.IsMechStyleMovement || unitType.IsVehicle ? MaterializeType.BurnDownLarge : MaterializeType.BurnDownSmall);
            }
            return MaterializeType.LowEventCamera_OtherBurnDown;
        }

        #region SpawnWarpOutItemAtCurrentLocation
        public void SpawnWarpOutItemAtCurrentLocation()
        {
            if ( this.floatingLODObject != null )
            {
                if ( this.CustomVisLODObject != null )
                    this.VisLODObject = this.CustomVisLODObject;
                VisLODDrawingObject toDraw = this.VisLODObject;
                if ( toDraw == null )
                    return;

                Vector3 position = this.floatingLODObject.WorldLocation;
                if ( position.x == 0 && position.z == 0 )
                    return;
                if ( float.IsNaN( position.x ) )
                    return;

                MapMaterializingItem materializingItem = MapMaterializingItem.GetFromPoolOrCreate();

                materializingItem.Position = position;
                materializingItem.Rotation = this.floatingLODObject.Rotation;
                materializingItem.Scale = this.floatingLODObject.EffectiveScale;

                materializingItem.RendererGroup = toDraw.LODRenderGroups[toDraw.LODRenderGroups.Count - 1];
                materializingItem.Materializing = MaterializeType.WarpOut;

                MapEffectCoordinator.AddMaterializingItem( materializingItem );

                //this.floatingLODObject.ReturnToPool(); //do not try to do this, it just causes exceptions from background threads
                this.floatingLODObject = null;
            }
            else if ( this.floatingSimpleObject != null )
            {
                if ( this.CustomVisSimpleObject != null )
                    this.VisSimpleObject = this.CustomVisSimpleObject;
                VisSimpleDrawingObject toDraw = this.VisSimpleObject;
                if ( toDraw == null )
                    return;

                Vector3 position = this.floatingSimpleObject.WorldLocation;
                if ( position.x == 0 && position.z == 0 )
                    return;
                if ( float.IsNaN( position.x ) )
                    return;

                MapMaterializingItem materializingItem = MapMaterializingItem.GetFromPoolOrCreate();

                materializingItem.Position = position;
                materializingItem.Rotation = this.floatingSimpleObject.Rotation;
                materializingItem.Scale = this.floatingSimpleObject.EffectiveScale;

                materializingItem.RendererGroup = toDraw.RendererGroup;
                materializingItem.Materializing = MaterializeType.WarpOut;

                MapEffectCoordinator.AddMaterializingItem( materializingItem );

                //this.floatingSimpleObject.ReturnToPool(); //do not try to do this, it just causes exceptions from background threads
                this.floatingSimpleObject = null;
            }
        }
        #endregion

        #region GetIsBlockedFromActions
        public bool GetIsBlockedFromActions()
        {
            if ( this.GetIsPlayerControlled() )
            {
                if ( (this.UnitType?.CostsToCreateIfBulkAndroid?.Count ?? 0) > 0 )
                {
                    if ( SimCommon.TotalBulkUnitSquadCapacityUsed > MathRefs.BulkUnitCapacity.DuringGameplay_CurrentInt )
                    {
                        this.attackPlan.Display.ClearTargetingBitsOnly();
                        return true; //fizzled, because the player has too many deployed
                    }
                }
                else
                {
                    if ( SimCommon.TotalCapturedUnitSquadCapacityUsed > MathRefs.CapturedUnitCapacity.DuringGameplay_CurrentInt )
                    {
                        this.attackPlan.Display.ClearTargetingBitsOnly();
                        return true; //fizzled, because the player has too many deployed
                    }
                }
            }
            return false;
        }
        #endregion

        #region Location Setting And Related
        public void SetActualContainerLocation( ISimUnitLocation CLoc )
        {
            this.ContainerLocation.Get()?.ClearOccupyingUnitIfThisOne( this );
            if ( CLoc != null )
            {
                this.GroundLocation = CLoc.GetEffectiveWorldLocationForContainedUnit();
                if ( this.UnitType?.IsMechStyleMovement??false )
                {
                    if ( CLoc is MapOutdoorSpot )
                        this.GroundLocation = this.GroundLocation.ReplaceY( 0 );
                }
                this.drawLocation = this.GroundLocation;
            }
            this.TurnsSinceMoved = 0;
            this.ContainerLocation = new WrapperedSimUnitLocation( CLoc );
            CLoc?.SetOccupyingUnitToThisOne( this );
            this.isMoving = false;
            this.desiredNewContainerLocation = null;
            SimCommon.NeedsVisibilityGranterRecalculation = true;
        }

        public void SetActualGroundLocation( Vector3 GLoc )
        {
            if ( this.UnitType?.IsMechStyleMovement ?? false )
                GLoc.y = 0;
            this.GroundLocation = GLoc;
            this.drawLocation = GLoc;

            this.TurnsSinceMoved = 0;
            this.ContainerLocation.Get()?.ClearOccupyingUnitIfThisOne( this );
            this.ContainerLocation = new WrapperedSimUnitLocation( null );
            this.isMoving = false;
            this.desiredNewContainerLocation = null;
            SimCommon.NeedsVisibilityGranterRecalculation = true;
        }

        public void SetDesiredContainerLocation( ISimUnitLocation CLoc )
        {
            if ( CLoc == null )
                return;

            if ( this.GetIsBlockedFromActions() )
                return;

            this.desiredNewContainerLocation = CLoc;
            this.originalSourceLocation = this.ContainerLocation.Get()?.GetEffectiveWorldLocationForContainedUnit() ?? this.GroundLocation;
            this.PriorLocation = this.originalSourceLocation;
            this.PriorRotation = this.objectRotation;
            this.isMoving = true;
            this.moveProgress = 0;

            this.RotateObjectToFace( this.GetDrawLocation(), this.desiredNewContainerLocation.GetEffectiveWorldLocationForContainedUnit(), 1000 );
        }

        public void SetDesiredGroundLocation( Vector3 GLoc )
        {
            if ( this.UnitType?.IsMechStyleMovement??false )
                GLoc.y = 0;

            if ( this.GetIsBlockedFromActions() )
                return;

            this.desiredNewContainerLocation = null;
            this.desiredNewGroundLocation = GLoc;
            this.originalSourceLocation = this.ContainerLocation.Get()?.GetEffectiveWorldLocationForContainedUnit() ?? this.GroundLocation;
            this.PriorLocation = this.originalSourceLocation;
            this.PriorRotation = this.objectRotation;
            this.isMoving = true;
            this.moveProgress = 0;
            this.RotateObjectToFace( this.GetDrawLocation(), this.desiredNewGroundLocation, 1000 );
        }
        #endregion

        public void ChangeCohort( NPCCohort NewCohort )
        {
            if ( NewCohort == null )
                return;
            this.FromCohort = NewCohort;
        }

        public MobileActorTypeDuringGameData GetTypeDuringGameData()
        {
            return this.UnitType?.DuringGameData;
        }

        #region HandleNPCUnitPerTurnFairlyEarly
        internal void HandleNPCUnitPerTurnFairlyEarly( MersenneTwister RandForThisTurn, NPCTimingData TimingDataOrNull )
        {
            bool lackingTimingData = TimingDataOrNull == null;
            long ticksAtStart = lackingTimingData ? 0 : TimingDataOrNull.sw.ElapsedTicks;

            this.CurrentTurnSeed = RandForThisTurn.Next() + 1;

            if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                this.DebugText = string.Empty; //reset at the start of every turn, if this is on

            if ( this.OverridingActionWillWarpOut )
            {
                this.WantsToPerformAction = NPCActionDesire.ShowWarpOut;
                return; //this can happen from time to time, and that's fine
            }

            if ( this.DisbandAtTheStartOfNextTurn )
            {
                this.DisbandNPCUnit( NPCDisbandReason.WantedToLeave );
                return;
            }
            if ( this.DisbandWhenObjectiveComplete && this.currentObjective == null )
            {
                this.DisbandNPCUnit( NPCDisbandReason.WantedToLeave );
                return;
            }

            this.DoManagedDespawnCheck( true, RandForThisTurn );

            switch ( this.WantsToPerformAction )
            {
                case NPCActionDesire.ShowWarpOut:
                    return; //should not happen, but leave them alone just in case
                case NPCActionDesire.ShowAppearance:
                    this.WantsToPerformAction = NPCActionDesire.None; //don't block actions for these npcs
                    break;
            }

            if ( this.isMoving )
            {
                //this happens when the player ends the turn so quickly that the unit has not yet moved
                if ( this.desiredNewContainerLocation != null )
                    this.SetActualContainerLocation( this.desiredNewContainerLocation );
                else if ( this.desiredNewGroundLocation.x != float.NegativeInfinity )
                    this.SetActualGroundLocation( this.desiredNewGroundLocation );
            }

            if ( this.UnitType.SquadmatesAreNotLostFromDamage )
            {
                if ( this.CurrentSquadSize < this.LargestSquadSize )
                    this.CurrentSquadSize = this.LargestSquadSize;
            }

            this.DoContrabandScan_OnTurnThreadOnly();

            NPCUnitStance stance = this.Stance;
            if ( this.GetHasAnyActionItWantsTodo() ) //this should not happen, but if the player never selected a unit near us somehow, then go ahead
            {
                if ( this.ObjectiveIsCompleteAndWaiting )
                {
                    if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                        this.DebugText += "HUPT-ObjectiveIsCompleteAndWaiting\n";
                    this.CurrentObjective.Implementation.DoObjectiveCompleteLogicForNPCUnit( this, this.CurrentObjective, RandForThisTurn, false );
                    this.ObjectiveIsCompleteAndWaiting = false;
                    this.WantsToPerformAction = NPCActionDesire.None;
                    if ( this.DisbandWhenObjectiveComplete )
                    {
                        this.DisbandAtTheStartOfNextTurn = true;
                        return;
                    }
                }
                if ( stance != null && this.GetHasAnyActionItWantsTodo() ) //if still wanting that
                {
                    if ( this.SpecialtyActionStringToBeDoneNext.Length > 0 )
                    {
                        if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                            this.DebugText += "HUPT-SpecialtyActionStringToBeDoneNext: " + this.SpecialtyActionStringToBeDoneNext + "\n";
                        stance.Implementation.HandleLogicForNPCUnitInStance( this, stance, NPCUnitStanceLogic.CheckForSpecialtyNPCAction, RandForThisTurn, null, null, null );
                        if ( this.desiredNewContainerLocation != null )
                            this.SetActualContainerLocation( this.desiredNewContainerLocation );
                        else if ( this.desiredNewGroundLocation.x != float.NegativeInfinity )
                            this.SetActualGroundLocation( this.desiredNewGroundLocation );
                        this.SpecialtyActionStringToBeDoneNext = string.Empty;
                    }
                    else if ( this.TargetLocation.x > float.NegativeInfinity )
                    {
                        if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                            this.DebugText += "HUPT-TargetLocation\n";
                        stance.Implementation.HandleLogicForNPCUnitInStance( this, stance, NPCUnitStanceLogic.MoveLogic, RandForThisTurn, null, null, null );
                        if ( this.desiredNewContainerLocation != null )
                            this.SetActualContainerLocation( this.desiredNewContainerLocation );
                        else if ( this.desiredNewGroundLocation.x != float.NegativeInfinity )
                            this.SetActualGroundLocation( this.desiredNewGroundLocation );
                        this.TargetLocation = Vector3.negativeInfinity;
                    }
                    else
                    {
                        ArcenDebugging.LogWithStack( (this.UnitType?.ID??"null") + ": Wanted to do what would once have been AttackLogicAfterVisuals, but that's not a thing now.", Verbosity.ShowAsError );
                        //if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                        //    this.DebugText += "HUPT-AttackLogicAfterVisuals\n";
                        //stance.Implementation.HandleLogicForNPCUnitInStance( this, stance, NPCUnitStanceLogic.AttackLogicAfterVisuals, RandForThisTurn, null, null, null );
                    }
                }
            }

            this.PerTurnHandleBaseFairlyEarlyActorLogic( RandForThisTurn );

            if ( this.GetActorDataCurrent( ActorRefs.ActorHP, true ) <= 0 )
            {
                this.DoDeathCheck( RandForThisTurn, false );
                return;
            }

            this.PriorLocation = Vector3.negativeInfinity;
            this.WantsToPerformAction = NPCActionDesire.None; //we need to start out false, and if the stance says to do something (belatedly, rather than instant), then that's up to them
            this.SpecialtyActionStringToBeDoneNext = string.Empty;
            this.TargetLocation = Vector3.negativeInfinity;
            this.TargetActor = null;
            this.HasAttackedYetThisTurn = false;

            switch ( this.Materializing )
            {
                case MaterializeType.Appear: //if we would have had an appearance effect, but it's already been more than one turn, then nevermind
                    this.Materializing = MaterializeType.None;
                    this.MaterializingProgress = 0;
                    break;
            }

            #region Discover Groups
            NPCCohort fromCohort = this.FromCohort;
            if ( !this.IsNPCInFogOfWar )
            {
                if ( fromCohort != null )
                    fromCohort.DuringGame_DiscoverIfNeed();
            }
            #endregion

            long ticksAtNext = lackingTimingData ? 0 : TimingDataOrNull.sw.ElapsedTicks;
            if ( !lackingTimingData )
            {
                TimingDataOrNull.EarlyTicks += (ticksAtNext - ticksAtStart);
                ticksAtStart = ticksAtNext;
            }

            if ( stance != null )
            {
                stance.Implementation.HandleLogicForNPCUnitInStance( this, stance, NPCUnitStanceLogic.PerTurnLogic, RandForThisTurn, null, null, TimingDataOrNull );
                stance.Implementation.HandleSecondaryPerTurnLogicForNPCUnitInStance( this, stance, RandForThisTurn );
                if ( !lackingTimingData )
                    TimingDataOrNull.StanceLogicCount++;
            }
            if ( !lackingTimingData )
            {
                ticksAtNext = TimingDataOrNull.sw.ElapsedTicks;
                TimingDataOrNull.StanceTicks += (ticksAtNext - ticksAtStart);
            }

            this.TurnsSinceMoved++;
        }
        #endregion

        #region HandleNPCUnitPerTurnVeryLate
        internal void HandleNPCUnitPerTurnVeryLate( MersenneTwister RandForThisTurn, NPCTimingData TimingDataOrNull )
        {
            if ( this.IsFullDead )
                return;

            this.PerTurnHandleBaseActorVeryLateLogic( RandForThisTurn );
        }
        #endregion

        #region CalculateSquadSizeLossFromDamage
        public int CalculateSquadSizeLossFromDamage( int DamageAmount )
        {
            if ( DamageAmount <= 0 ) 
                return 0;
            if ( !this.actorData.TryGetValue( ActorRefs.ActorHP, out MapActorData Data, false ) )
                return 0;

            int currentHP = Data.Current;
            if ( currentHP <= DamageAmount )
            {
                if ( currentHP <= 0 )
                {
                    //ArcenDebugging.LogSingleLine( this.UnitID + ": " + this.UnitType.ID + " skipped any loss from " + DamageAmount + " dmg to " + currentHP + " of max " + Data.Maximum, Verbosity.DoNotShow );
                    return 0;
                }
                else
                {
                    //ArcenDebugging.LogSingleLine( this.UnitID + ": " + this.UnitType.ID + " total loss " + DamageAmount + " dmg to " + currentHP + " of max " + Data.Maximum, Verbosity.DoNotShow );
                    return this.CurrentSquadSize; //everyone died
                }
            }
            //int lostHP = Data.Maximum - Data.Current;

            if ( this.UnitType.SquadmatesAreNotLostFromDamage )
                return 0;

            //if not everyone, then what portion?
            if ( this.HealthPerUnit > 0 )
            {
                int proposedSquadSize = Mathf.CeilToInt( (float)(currentHP- DamageAmount) / (float)this.HealthPerUnit );
                if ( proposedSquadSize < 1 )
                    proposedSquadSize = 1;
                if ( proposedSquadSize >= this.CurrentSquadSize )
                    return 0;

                int unitsLost = this.CurrentSquadSize - proposedSquadSize;

                //ArcenDebugging.LogSingleLine( this.UnitID + ": " + this.UnitType.ID + " lose " + unitsLost + " from " + DamageAmount + 
                //    " dmg to " + currentHP + " with lostHP: " + lostHP + " with hp-per: " + HealthPerUnit + " proposedSquadSize: " + proposedSquadSize + 
                //    " CurrentSquadSize: " + CurrentSquadSize + " of max " + Data.Maximum, Verbosity.DoNotShow );

                if ( unitsLost > 0 )
                    return unitsLost;
            }
            //no portion, apparently
            return 0;
        }
        #endregion

        #region DoManagedDespawnCheck
        public void DoManagedDespawnCheck( bool IsFromTurnChange, MersenneTwister Rand )
        {
            NPCManagedUnit managedUnit = this.IsManagedUnit;
            if ( managedUnit != null )
            {
                if ( managedUnit.ShouldHardDespawnOnManagerBlocked || managedUnit.ShouldSoftDespawnOnManagerBlocked ||
                    managedUnit.ShouldSoftDespawnOnProjectOrMissionComplete || managedUnit.ShouldHardDespawnOnProjectOrMissionComplete ||
                    managedUnit.ShouldSoftDespawnOnManagerIntervalEnds || managedUnit.ShouldHardDespawnOnManagerIntervalEnds )
                {
                    if ( !(managedUnit.ParentManager?.PeriodicData?.CalculatePeriodicDataMeetsPrerequisites( false,
                        managedUnit.ShouldIgnoreResourcePortionOfManagerBlockedForDespawnLogic, false ) ??true) || !managedUnit.GetShouldSpawn( false ) ||
                        //if player unit targeting a job that is broken or off, then despawn
                        ( this.GetIsPartOfPlayerForcesInAnyWay() && managedUnit.ParentManager?.PeriodicData?.TargetJobTag != null && 
                        (this.ManagerStartLocation.Get()?.MachineStructureInBuilding?.GetIsStructureHavingMajorProblems ()?? true) ) )
                    {
                        if ( managedUnit.ShouldHardDespawnOnManagerBlocked )
                            this.WantsToPerformAction = NPCActionDesire.ShowWarpOut;
                        else //hard despawn
                        {
                            if ( this.CurrentObjective != null )
                                this.DisbandWhenObjectiveComplete = true;
                            else
                                this.WantsToPerformAction = NPCActionDesire.ShowWarpOut;
                        }
                        return;
                    }
                }

                if ( IsFromTurnChange )
                {
                    if ( managedUnit.UnitsWarpOutAfterTurns > 0 && managedUnit.UnitsWarpOutAfterTurns <= this.TurnsHasExisted )
                    {
                        if ( managedUnit.UnitsWarpOutAfterTurnsPercentChance <= 0 || managedUnit.UnitsWarpOutAfterTurnsPercentChance >= 100 ||
                            Rand.Next( 0, 100 ) < managedUnit.UnitsWarpOutAfterTurnsPercentChance )
                        {
                            this.WantsToPerformAction = NPCActionDesire.ShowWarpOut;
                            this.OverridingActionWillWarpOut = true; //this makes it so that if background logic is running at the same time, it won't accidentally stick around.
                            this.LangKeyForDisbandPopup = managedUnit.LangKeyForWarpOutAfterTurns;
                        }
                    }
                }
            }

            CityConflictUnit conflictUnit = this.IsCityConflictUnit;
            if ( conflictUnit != null )
            {
                if ( conflictUnit.ParentConflict.DuringGameplay_State != CityConflictState.Active )
                    this.WantsToPerformAction = NPCActionDesire.ShowWarpOut;
            }
        }
        #endregion

        private static ArcenDoubleCharacterBuffer popupBufferForTurnChangeThread = new ArcenDoubleCharacterBuffer( "NPCUnit-popupBufferForTurnChangeThread" );

        #region DoContrabandScan_OnTurnThreadOnly
        private void DoContrabandScan_OnTurnThreadOnly()
        {
            int contrabandScanner = this.GetActorDataCurrent( ActorRefs.ContrabandScanner, true );
            if ( contrabandScanner <= 1 )
                return; //no scanning abilities, or effectively off, so nevermind
            if ( this.GetIsPartOfPlayerForcesInAnyWay() )
                return;

            MapTile tile = this.CalculateMapCell()?.ParentTile;
            if ( tile == null ) 
                return;

            float attackRangeSquared = this.GetAttackRangeSquared();
            Vector3 myCenter = this.GetDrawLocation();

            foreach ( ISimMapActor actor in tile.ActorsWithinMaxNPCAttackRange.GetDisplayList() )
            {
                if ( actor is MachineStructure structure )
                {
                    MapActorData structureHidden = structure.GetActorDataData( ActorRefs.StructureHidden, true );
                    if ( structureHidden == null || structureHidden.Current <= 0 )
                        continue;

                    float dist = (structure.GetDrawLocation() - myCenter).GetSquareGroundMagnitude();
                    if ( dist > attackRangeSquared )
                        continue; //out of range of our attack, so ignore!

                    int scanResistance = structure.GetActorDataCurrent( ActorRefs.StructureScanResistance, true );
                    if ( scanResistance >= contrabandScanner )
                        continue; //if this structures has more resistance to scanning than our scanner power, then don't do anything to them

                    Vector3 startLocation = structure.GetPositionForCollisions();
                    Vector3 endLocation = startLocation.PlusY( structure.GetHalfHeightForCollisions() + 0.2f );

                    {
                        structureHidden.AlterCurrent( -contrabandScanner );

                        //ArcenDebugging.LogSingleLine( this.GetDisplayName() + " " + this.Stance.ID + " " + structure.GetDisplayName() + " amt: " + contrabandScanner + 
                        //    " dist: " + dist + " attack: " + attackRangeSquared, Verbosity.DoNotShow );

                        DamageTextPopups.FloatingDamageTextPopup oldScannedText = structure.MostRecentScannedText;
                        if ( oldScannedText != null && oldScannedText.GetTimeHasExisted() > 1f )
                            oldScannedText = null; //if it's been more than 1 second, don't combine them

                        int drawnScannedAmount = contrabandScanner + (oldScannedText?.PhysicalDamageIncluded ?? 0);

                        if ( oldScannedText != null )
                            oldScannedText.MarkMeAsExpiredNow();

                        popupBufferForTurnChangeThread.EnsureResetForNextUpdate();
                        popupBufferForTurnChangeThread.AddSpriteStyled_NoIndent( IconRefs.StructureScannedAmount.Icon, AdjustedSpriteStyle.InlineSmaller095,
                            IconRefs.StructureScannedAmount.DefaultColorHexWithHDRHex );
                        popupBufferForTurnChangeThread.Space1x().AddRaw( (-drawnScannedAmount).ToStringThousandsWhole(), IconRefs.StructureScannedAmount.DefaultColorHexWithHDRHex );
                        if ( structureHidden.Current <= 0 )
                        {
                            popupBufferForTurnChangeThread.Space3x().AddLang( "PopupRevealed", IconRefs.StructureRevealedColor.DefaultColorHexWithHDRHex );

                            if ( !FlagRefs.HasBeenContrabandScanned.DuringGameplay_IsTripped )
                                FlagRefs.HasBeenContrabandScanned.TripIfNeeded();
                        }

                        DamageTextPopups.FloatingDamageTextPopup newScannedText = DamageTextPopups.CreateNewFromTextBuffer( popupBufferForTurnChangeThread,
                            startLocation, endLocation, 0.7f, MathA.Max( 2, GameSettings.Current.GetInt( "DamagePopup_LingerTime" ) ) );
                        newScannedText.PhysicalDamageIncluded = drawnScannedAmount;
                        newScannedText.MoraleDamageIncluded = 0;
                        structure.MostRecentScannedText = newScannedText;
                    }
                }
            }
        }
        #endregion

        public void DoPerSecondRecalculations( bool IsFromDeserialization )
        {
            this.DoManagedDespawnCheck( false, Engine_Universal.PermanentQualityRandom );

            NPCUnitStance stance = _stance;
            if ( stance != null )
            {
                MapPOI homePOI = this.HomePOI;
                if ( homePOI != null )
                {
                    bool isAlarmedAgainstPlayer = homePOI.IsPOIAlarmed_AgainstPlayer;
                    bool isAlarmedThirdParty = homePOI.IsPOIAlarmed_ThirdParty;

                    NPCUnitStance switchTo = null;
                    if ( isAlarmedAgainstPlayer )
                        switchTo = stance.SwitchToStanceWhenHomePOIAlarmedAgainstPlayer;
                    else if ( isAlarmedThirdParty )
                    {
                        if ( stance.SwitchToStanceWhenHomePOIAlarmedAgainstThirdParty != null )
                            switchTo = stance.SwitchToStanceWhenHomePOIAlarmedAgainstThirdParty;
                        else
                            switchTo = stance.SwitchToStanceWhenHomePOINotAlarmedAgainstPlayer;
                    }
                    else
                        switchTo = stance.SwitchToStanceWhenHomePOINotAlarmedAgainstAny;

                    if ( switchTo != null )
                        this.Stance = switchTo;
                }

                if ( stance.IsContainedToDistrict && this.HomeDistrict == null )
                    this.HomeDistrict = this.CalculateMapDistrict();
            }

            NPCUnitType convertsTo = null;
            if ( !this.GetIsPlayerControlled() )
            {
                if ( this.UnitType.ConvertsToIfCityFlagTrue1 != null && (this.UnitType.CityFlagThatCausesConversion1?.DuringGameplay_IsTripped ?? false) )
                    convertsTo = this.UnitType.ConvertsToIfCityFlagTrue1;
                if ( this.UnitType.ConvertsToIfCityFlagTrue2 != null && (this.UnitType.CityFlagThatCausesConversion2?.DuringGameplay_IsTripped ?? false) )
                    convertsTo = this.UnitType.ConvertsToIfCityFlagTrue2;
                if ( this.UnitType.ConvertsToIfCityFlagTrue3 != null && (this.UnitType.CityFlagThatCausesConversion3?.DuringGameplay_IsTripped ?? false) )
                    convertsTo = this.UnitType.ConvertsToIfCityFlagTrue3;
            }
            else
            {
                //this IS player controlled
                if ( this.HomePOI != null )
                    this.HomePOI = null;
                if ( this.HomeDistrict != null )
                    this.HomeDistrict = null;
            }

            //do not do this for player units
            if ( convertsTo != null )
            {
                this.UnitType = convertsTo;

                this.VisSimpleObject = null;
                this.VisLODObject = null;
                this.CustomVisSimpleObject = null;
                this.CustomVisLODObject = null;

                if ( UnitType.IsKeyContact != null )
                {
                    UnitType.IsKeyContact.ValidateContact( Engine_Universal.PermanentQualityRandom, true );
                    IDrawingObject contactObject = UnitType.IsKeyContact.DuringGame_ChosenDrawingObject;
                    if ( contactObject is VisLODDrawingObject lodObj )
                        this.VisLODObject = lodObj;
                    else if ( contactObject is VisSimpleDrawingObject simpleObj )
                        this.VisSimpleObject = simpleObj;
                }
                else if ( UnitType.DrawingObjectTag.LODObjects.Count > 0 )
                    this.VisLODObject = UnitType.DrawingObjectTag.LODObjects.GetRandom( Engine_Universal.PermanentQualityRandom );
                else if ( UnitType.DrawingObjectTag.SimpleObjects.Count > 0 )
                    this.VisSimpleObject = UnitType.DrawingObjectTag.SimpleObjects.GetRandom( Engine_Universal.PermanentQualityRandom );

                this.InitializeActorData_Randomized( UnitType.ID, UnitType.ActorDataSetUsed, UnitType.ActorData );
            }

            //NPCCohort cohort = this.FromCohort;
            this.RecalculateActorStats( IsFromDeserialization );

            //pretty much redoing CalculateSquadSizeLossFromDamage for performance reasons, and clarity
            //if ( healthCurrent > 0 && this.HealthPerUnit > 0 )
            //{
            //    int proposedSquadSize = Mathf.CeilToInt( healthCurrent / (float)this.HealthPerUnit );
            //    if ( proposedSquadSize < 1 )
            //        proposedSquadSize = 1;
            //    if ( proposedSquadSize < this.CurrentSquadSize)
            //        this.CurrentSquadSize = proposedSquadSize; //can only go down!
            //}

            this.RecalculatePerksPerSecond( this.UnitType.DuringGameData.Equipment, this.UnitType.DefaultPerks, stance?.PerksGranted, stance?.PerksBlocked );
        }

        protected override void RecalculateActorStats( bool IsFromDeserialization )
        {
            NPCUnitType type = this.UnitType;
            if ( type == null ) 
                return;

            float currentSquadSizeMultiplier_Real = (float)this.CurrentSquadSize / (float)type.BasicSquadSize;
            float currentSquadSizeMultiplier_Hardened = currentSquadSizeMultiplier_Real;
            if ( type.StatsDoNotGoDownWhenSquadmatesLost && currentSquadSizeMultiplier_Hardened < 1 )
                currentSquadSizeMultiplier_Hardened = -1f;

            if ( type.StatsDoNotGoUpWhenSquadmatesAdded && currentSquadSizeMultiplier_Real > 1 )
                currentSquadSizeMultiplier_Real = -1f;
            if ( type.StatsDoNotGoUpWhenSquadmatesAdded && currentSquadSizeMultiplier_Hardened > 1 )
                currentSquadSizeMultiplier_Hardened = -1f;

            float largestSquadSizeMultiplier = (float)this.LargestSquadSize / (float)type.BasicSquadSize;
            if ( type.StatsDoNotGoDownWhenSquadmatesLost && largestSquadSizeMultiplier < 1 )
                largestSquadSizeMultiplier = 1f;
            else if ( type.StatsDoNotGoUpWhenSquadmatesAdded && largestSquadSizeMultiplier > 1 )
                largestSquadSizeMultiplier = 1f;

            Dictionary<ActorDataType, float> stanceMultipliers = this.Stance?.ActorDataMultipliers;
            Dictionary<ActorDataType, int> stanceAdditions = this.Stance?.ActorDataFlatAdded;

            List<ISimBuilding> boostingStructures = this.BoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob.GetDisplayList();
            //float healthCurrent = 0;
            foreach ( KeyValuePair<ActorDataType, MapActorData> kv in this.actorData )
            {
                if ( kv.Value == null )
                    continue;

                {
                    int buffFromCohort = 0;//cohort == null || kv.Key.FlatBuffedFromCohortStat == null ? 0 : cohort.GetDuringGame_Data()[kv.Key.FlatBuffedFromCohortStat];
                    int debuffFromCohort = 0;//cohort == null || kv.Key.FlatDebuffedFromCohortStat == null ? 0 : cohort.GetDuringGame_Data()[kv.Key.FlatDebuffedFromCohortStat];
                    kv.Value.RecalculateEffectiveValues_ForMapActorData( type.DataFlatUpgrades, type.DataMultiplicativeUpgrades, null, null,
                        type.DuringGameData.Equipment, boostingStructures, null, this.DataChangesToMaximumsFromStatuses, this.DataMultipliersToMaximumsFromStatuses,
                        stanceMultipliers, stanceAdditions, type.DuringGameData.DirectStatAdditions, type.DuringGameData.DealStatAdditions.GetDisplayDict(),
                        type.DuringGameData.CollectionsWithUpgrades.GetDisplayList(), this.BadgeDict, null, null, IsFromDeserialization, null,
                        currentSquadSizeMultiplier_Real, currentSquadSizeMultiplier_Hardened, this.CurrentSquadSize, this.LargestSquadSize, largestSquadSizeMultiplier, 
                        buffFromCohort, debuffFromCohort, false, type.ID );
                }

                //if ( kv.Key.ID == "UnitHP" )
                //    healthCurrent = kv.Value.Current;
            }
        }

        #region CalculateTheoreticalStatStrengthIfWeImagineThisAmountOfLostHealth
        public int CalculateTheoreticalStatStrengthIfWeImagineThisAmountOfLostHealth( ActorDataType DataType, int DamageAmount )
        {
            int squadSizeLoss = this.CalculateSquadSizeLossFromDamage( DamageAmount );
            if ( squadSizeLoss <= 0 )
                return this.GetActorDataCurrent( DataType, true ); //just give me the basic item.

            if ( !this.actorData.TryGetValue( DataType, out MapActorData DataData, false ) )
                return 0;

            NPCUnitType type = this.UnitType;
            if ( type == null )
                return 0;

            List<ISimBuilding> boostingStructures = this.BoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob.GetDisplayList();

            float currentSquadSizeMultiplier_Real = (float)(this.CurrentSquadSize - squadSizeLoss) / (float)type.BasicSquadSize;
            float currentSquadSizeMultiplier_Hardened = currentSquadSizeMultiplier_Real;
            if ( type.StatsDoNotGoDownWhenSquadmatesLost && currentSquadSizeMultiplier_Hardened < 1 )
                currentSquadSizeMultiplier_Hardened = -1f;

            if ( type.StatsDoNotGoUpWhenSquadmatesAdded && currentSquadSizeMultiplier_Real > 1 )
                currentSquadSizeMultiplier_Real = -1f;
            if ( type.StatsDoNotGoUpWhenSquadmatesAdded && currentSquadSizeMultiplier_Hardened > 1 )
                currentSquadSizeMultiplier_Hardened = -1f;

            float largestSquadSizeMultiplier = (float)this.LargestSquadSize / (float)type.BasicSquadSize;
            if ( type.StatsDoNotGoDownWhenSquadmatesLost && largestSquadSizeMultiplier < 1 )
                largestSquadSizeMultiplier = 1f;
            else if ( type.StatsDoNotGoUpWhenSquadmatesAdded && largestSquadSizeMultiplier > 1 )
                largestSquadSizeMultiplier = 1f;

            //NPCCohort cohort = this.FromCohort;
            int buffFromCohort = 0;// cohort == null || DataType.FlatBuffedFromCohortStat == null ? 0 : cohort.GetDuringGame_Data()[DataType.FlatBuffedFromCohortStat];
            int debuffFromCohort = 0;//cohort == null || DataType.FlatDebuffedFromCohortStat == null ? 0 : cohort.GetDuringGame_Data()[DataType.FlatDebuffedFromCohortStat];

            Dictionary<ActorDataType, float> stanceMultipliers = this.Stance?.ActorDataMultipliers;
            Dictionary<ActorDataType, int> stanceAdditions = this.Stance?.ActorDataFlatAdded;
            return DataData.RecalculateEffectiveValues_ForMapActorData( type.DataFlatUpgrades, type.DataMultiplicativeUpgrades, null, null,
                type.DuringGameData.Equipment, boostingStructures, null, this.DataChangesToMaximumsFromStatuses, this.DataMultipliersToMaximumsFromStatuses,
                stanceMultipliers, stanceAdditions, type.DuringGameData.DirectStatAdditions, type.DuringGameData.DealStatAdditions.GetDisplayDict(),
                type.DuringGameData.CollectionsWithUpgrades.GetDisplayList(), this.BadgeDict, null, null, false, null,
                currentSquadSizeMultiplier_Real, currentSquadSizeMultiplier_Hardened, ( this.CurrentSquadSize - squadSizeLoss ), this.LargestSquadSize, 
                largestSquadSizeMultiplier, buffFromCohort, debuffFromCohort, true, type.ID );
        }
        #endregion

        public void AppendExtraDetailsForDataType( ArcenDoubleCharacterBuffer Buffer, MapActorData actorData )
        {
            if ( actorData == null || Buffer == null )
                return;

            NPCUnitType type = this.UnitType;
            if ( type == null )
                return;

            float currentSquadSizeMultiplier_Real = (float)this.CurrentSquadSize / (float)type.BasicSquadSize;
            float currentSquadSizeMultiplier_Hardened = currentSquadSizeMultiplier_Real;
            if ( type.StatsDoNotGoDownWhenSquadmatesLost && currentSquadSizeMultiplier_Hardened < 1 )
                currentSquadSizeMultiplier_Hardened = -1f;

            if ( type.StatsDoNotGoUpWhenSquadmatesAdded && currentSquadSizeMultiplier_Real > 1 )
                currentSquadSizeMultiplier_Real = -1f;
            if ( type.StatsDoNotGoUpWhenSquadmatesAdded && currentSquadSizeMultiplier_Hardened > 1 )
                currentSquadSizeMultiplier_Hardened = -1f;

            float largestSquadSizeMultiplier = (float)this.LargestSquadSize / (float)type.BasicSquadSize;
            if ( type.StatsDoNotGoDownWhenSquadmatesLost && largestSquadSizeMultiplier < 1 )
                largestSquadSizeMultiplier = 1f;
            else if ( type.StatsDoNotGoUpWhenSquadmatesAdded && largestSquadSizeMultiplier > 1 )
                largestSquadSizeMultiplier = 1f;

            //NPCCohort cohort = this.FromCohort;
            int buffFromCohort = 0;//cohort == null || actorData.Type.FlatBuffedFromCohortStat == null ? 0 : cohort.GetDuringGame_Data()[actorData.Type.FlatBuffedFromCohortStat];
            int debuffFromCohort = 0;//cohort == null || actorData.Type.FlatDebuffedFromCohortStat == null ? 0 : cohort.GetDuringGame_Data()[actorData.Type.FlatDebuffedFromCohortStat];

            List<ISimBuilding> boostingStructures = this.BoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob.GetDisplayList();
            Dictionary<ActorDataType, float> stanceMultipliers = this.Stance?.ActorDataMultipliers;
            Dictionary<ActorDataType, int> stanceAdditions = this.Stance?.ActorDataFlatAdded;
            actorData.RecalculateEffectiveValues_ForMapActorData( type.DataFlatUpgrades, type.DataMultiplicativeUpgrades, null, null,
                type.DuringGameData.Equipment, boostingStructures, null, this.DataChangesToMaximumsFromStatuses, this.DataMultipliersToMaximumsFromStatuses,
                stanceMultipliers, stanceAdditions, type.DuringGameData.DirectStatAdditions, type.DuringGameData.DealStatAdditions.GetDisplayDict(),
                 type.DuringGameData.CollectionsWithUpgrades.GetDisplayList(), this.BadgeDict, null, null, false, Buffer,
                currentSquadSizeMultiplier_Real, currentSquadSizeMultiplier_Hardened, this.CurrentSquadSize, this.LargestSquadSize, 
                largestSquadSizeMultiplier, buffFromCohort, debuffFromCohort, false, type.ID );
        }

        public override float GetFeatAmount( ActorFeat Feat )
        {
            return this.UnitType?.DefaultFeats?[Feat] ?? -1f;
        }

        public FeatSetForType GetFeatSetOrNull()
        {
            return this.UnitType?.DuringGameData?.FeatSet;
        }

        public bool GetIsInActorCollection( ActorCollection Collection )
        {
            if ( Collection == null )
                return false;
            return this.UnitType?.Collections?.ContainsKey( Collection.ID )??false;
        }

        #region GetEffectiveClearance
        public int GetEffectiveClearance( ClearanceCheckType CheckType )
        {
            int bestClearance = this.IsUnremarkableAnywhereUpToClearanceInt;
            if ( bestClearance < 0 )
                bestClearance = 0; //for these purposes, our floor-value is 0

            SecurityClearance currentClearance = null;
            if ( currentClearance != null && currentClearance.Level > bestClearance )
                bestClearance = currentClearance.Level;

            switch ( CheckType )
            {
                case ClearanceCheckType.StayingThere:
                    {
                        int bestClearanceAtBuilding = this.IsUnremarkableBuildingOnlyUpToClearanceInt;
                        if ( bestClearanceAtBuilding > bestClearance && this.GetIsAtABuilding() )
                            bestClearance = bestClearanceAtBuilding;
                    }
                    break;
                case ClearanceCheckType.MovingToBuilding:
                    {
                        int bestClearanceAtBuilding = this.IsUnremarkableBuildingOnlyUpToClearanceInt;
                        if ( bestClearanceAtBuilding > bestClearance ) //we already know we're moving to a building
                            bestClearance = bestClearanceAtBuilding;
                    }
                    break;
                    //case ClearanceCheckType.MovingToNonBuilding: //nothing special to do here
                    //    break;
            }

            return bestClearance;
        }
        #endregion

        public bool GetIsMovingRightNow()
        {
            return this.isMoving;
        }

        protected bool GetIsAtABuilding()
        {
            return this.ContainerLocation.Get() is ISimBuilding;
        }

        #region GetHasAnyActionItWantsTodo
        public bool GetHasAnyActionItWantsTodo()
        {
            //mirror anything in here to WriteAnyActionItWantsTodo!
            if ( this.OverridingActionWillWarpOut ) //safety measure
                this.WantsToPerformAction = NPCActionDesire.ShowWarpOut;

            return this.WantsToPerformAction != NPCActionDesire.None;
        }
        #endregion

        #region WriteAnyActionItWantsTodo
        public void WriteAnyActionItWantsTodo( ArcenCharacterBufferBase Buffer )
        {
            //mirror anything in here from GetHasAnyActionItWantsTodo!
            Buffer.AddRaw( this.WantsToPerformAction.ToString() );
        }
        #endregion

        #region GetShouldActionsBeWatchedByPlayer
        public bool GetShouldActionsBeWatchedByPlayer()
        {
            NPCUnitStance stance = this.Stance;
            if ( stance == null )
                return false;
            if ( stance.ThisUnitIsNeverEverWatchedByThePlayer )
                return false;

            switch (InputCaching.NPCActionsView )
            {
                case NPCActionViewType.WatchNone:
                    return false;
                case NPCActionViewType.WatchHigh:
                    if ( stance.ThisUnitActsBeforePlayerLooksAtThem )
                        return false;
                    break;
                case NPCActionViewType.WatchAll:
                    break;
            }

            if ( this.IsNPCInFogOfWar && !stance.CausesUnitToAlwaysBeVisibleAsIfOutOfFogOfWar )
                return false;

            return true;
        }
        #endregion

        #region GetIsNPCActionStillValid
        public bool GetIsNPCActionStillValid()
        {
            if ( this.OverridingActionWillWarpOut ) //safety measure
                this.WantsToPerformAction = NPCActionDesire.ShowWarpOut;

            switch ( this.WantsToPerformAction )
            {
                case NPCActionDesire.ShowAppearance:
                case NPCActionDesire.ShowWarpOut:
                    return true;
                case NPCActionDesire.CustomAgainstActor:
                    if ( this.TargetActor != null && this.TargetLocation.x == float.NegativeInfinity && this.SpecialtyActionStringToBeDoneNext.Length == 0 )
                    {
                        //ArcenDebugging.LogSingleLine( "wants to attack D: " + (this.TargetActor?.GetDisplayName()??"null") + " " + this.UnitID, Verbosity.DoNotShow );
                        if ( this.TargetActor.IsFullDead )
                        {
                            this.WantsToPerformAction = NPCActionDesire.None;
                            return false;
                        }
                        return true;
                    }
                    break;
                case NPCActionDesire.None:
                    return false;
            }

            return true; //all the others are general
        }
        #endregion

        #region AttackChosenTarget_MainThreadOnly
        public bool AttackChosenTarget_MainThreadOnly( MersenneTwister RandThatWillBeReset )
        {
            return AttackChosenTarget_MainThreadOnly( RandThatWillBeReset, false );
        }

        private bool AttackChosenTarget_MainThreadOnly( MersenneTwister RandThatWillBeReset, bool ForceNoParticles )
        {
            ISimMapActor target = this.attackPlan.Display.TargetForStartOfNextTurn;
            if ( target == null )
                return false;
            if ( this.HasAttackedYetThisTurn )
                return false;

            this.HasAttackedYetThisTurn = true;

            if ( this.GetIsBlockedFromActions() )
            {
                this.attackPlan.Display.ClearTargetingBitsOnly();
                return false; //fizzled, because the player has too many deployed
            }

            Dictionary<ResourceType, int> costsPerAttack = null;
            MobileActorTypeDuringGameData dgd = this.UnitType?.DuringGameData;
            if ( dgd != null && dgd.EffectiveCostsPerAttack.Count > 0 )
            {
                if ( this.GetIsPlayerControlled() )
                {
                    costsPerAttack = dgd.EffectiveCostsPerAttack.GetDisplayDict();
                    foreach ( KeyValuePair<ResourceType, int> kv in costsPerAttack )
                    {
                        if ( kv.Key.Current < kv.Value )
                        {
                            this.attackPlan.Display.ClearTargetingBitsOnly();
                            return false; //fizzled, because the player cannot afford it
                        }
                    }
                }
            }

            AttackAmounts damage;
            damage.Physical = this.attackPlan.Display.PhysicalDamageIWillDoToThePrimaryTarget;
            damage.Morale = this.attackPlan.Display.MoraleDamageIWillDoToThePrimaryTarget;

            //this is copied to a secondary buffer specifically to avoid issues with if another targeting pass happens in the middle of all this
            this.attackPlan.Display.SecondaryTargetDamageQueue.ClearAndCopyFrom( this.attackPlan.Display.SecondaryTargetsDamaged.GetDisplayDict() );

            RandThatWillBeReset.ReinitializeWithSeed( this.CurrentTurnSeed + this.NonSimUniqueID );

            if ( target.IsFullDead || target.GetIsCurrentlyInvisible( InvisibilityPurpose.ForNPCTargeting ) ||
                !AttackHelper.GetTargetIsInRange( this, target ) )// || target.GetWouldBeDeadFromIncomingDamageActual() ) fire if it would be overkill, that's ok
            {
                //target was invalid, so give up.  Sucks to be an NPC.
                this.attackPlan.Display.ClearTargetingBitsOnly();
                return false;
            }
            else
            {
                //target was still valid!
                if ( damage.IsEmpty() ) //if damage was not valid, then calculate that
                    damage = AttackHelper.PredictNPCDamageForImmediateFiringSolution_PrimaryTarget( this, target, RandThatWillBeReset );
            }

            if ( damage.IsEmpty() )
            {
                this.attackPlan.Display.ClearTargetingBitsOnly();
                return false; //if will not deal any damage, then skip the rest!
            }

            int particleCount = Engine_HotM.NonSim_TravelingParticleEffectsPlayingNow;// ( FrameBufferManagerData.ParticleCount.Display + Engine_HotM.NonSim_TotalParticleEffectsPlayingNow);

            bool dropMoreParticles = !ForceNoParticles && ( particleCount > 160 ? Engine_Universal.PermanentQualityRandom.Next( 0, 40000 ) > particleCount * particleCount : //200 squared
                Engine_Universal.PermanentQualityRandom.Next( 0, 160 ) > particleCount );

            if ( costsPerAttack != null )
            {
                bool isBulk = (this.UnitType?.BulkUnitCapacityRequired??0) > 0;
                foreach ( KeyValuePair<ResourceType, int> kv in costsPerAttack )
                    kv.Key.AlterCurrent_Named( -kv.Value, isBulk ? "Expense_UsedForBulkUnitAttacks" : "Expense_UsedForCapturedUnitAttacks", ResourceAddRule.IgnoreUntilTurnChange );
            }

            if ( dropMoreParticles && ( !target.GetIsInFogOfWar() || !this.GetIsInFogOfWar() ) && !ForceNoParticles )
            {
                //marking damage that is on the way, for real
                if ( damage.Physical > 0 )
                    target.AlterIncomingPhysicalDamageActual( damage.Physical );
                if ( damage.Morale > 0 )
                    target.AlterIncomingMoraleDamageActual( damage.Morale );
                //also for secondaries
                foreach ( KeyValuePair<ISimMapActor, AttackAmounts> kv in this.attackPlan.Display.SecondaryTargetDamageQueue )
                {
                    if ( kv.Value.Physical > 0 )
                        kv.Key.AlterIncomingPhysicalDamageActual( kv.Value.Physical );
                    if ( kv.Value.Morale > 0 )
                        kv.Key.AlterIncomingMoraleDamageActual( kv.Value.Morale );
                }

                //the player could see this, so actually do the sound and firing
                this.FireWeaponsAtTargetPoint( target.GetEmissionLocation(), 
                    Engine_Universal.PermanentQualityRandom, //this is used only for some visual bits, and we are on the main thread, so all good
                    delegate //this will be called-back on the main thread
                    {
                        if ( damage.Physical > 0 )
                            target.AlterIncomingPhysicalDamageActual( -damage.Physical ); //then take that back, so we are not double-counting
                        if ( damage.Morale > 0 )
                            target.AlterIncomingMoraleDamageActual( -damage.Morale ); //then take that back, so we are not double-counting

                        //since this is probably a different thread, but definitely the main thread, use this other random
                        workingRandomMainThreadOnlyShouldBeReset.ReinitializeWithSeed( this.CurrentTurnSeed + this.NonSimUniqueID );
                        AttackHelper.DoNPCDelayedAttack_UsePriorCalculation_PrimaryOrSecondary( this, target, workingRandomMainThreadOnlyShouldBeReset, damage );

                        //now handle any secondaries
                        foreach ( KeyValuePair<ISimMapActor, AttackAmounts> kv in this.attackPlan.Display.SecondaryTargetDamageQueue )
                        {
                            if ( !kv.Value.IsEmpty() )
                            {
                                if ( kv.Value.Physical > 0 )
                                    kv.Key.AlterIncomingPhysicalDamageActual( -kv.Value.Physical );
                                if ( kv.Value.Morale > 0 )
                                    kv.Key.AlterIncomingMoraleDamageActual( -kv.Value.Morale );

                                workingRandomMainThreadOnlyShouldBeReset.ReinitializeWithSeed( this.CurrentTurnSeed + this.NonSimUniqueID );
                                AttackHelper.DoNPCDelayedAttack_UsePriorCalculation_PrimaryOrSecondary( this, kv.Key, workingRandomMainThreadOnlyShouldBeReset, kv.Value );
                            }
                        }
                        this.attackPlan.Display.SecondaryTargetDamageQueue.Clear();

                    } );

                //this clears out prior to the callback above, which is fine because we cached what we needed
                this.attackPlan.Display.ClearTargetingBitsOnly();
            }
            else
            {
                //since the player cannot see this, just do it instantly
                RandThatWillBeReset.ReinitializeWithSeed( this.CurrentTurnSeed + this.NonSimUniqueID );
                AttackHelper.DoNPCImmediateAttack_UsePriorCalculation_PrimaryOrSecondary( this, target, RandThatWillBeReset, damage );

                //now handle any secondaries
                foreach ( KeyValuePair<ISimMapActor, AttackAmounts> kv in this.attackPlan.Display.SecondaryTargetDamageQueue )
                {
                    if ( !kv.Value.IsEmpty() )
                    {
                        RandThatWillBeReset.ReinitializeWithSeed( this.CurrentTurnSeed + this.NonSimUniqueID );
                        AttackHelper.DoNPCImmediateAttack_UsePriorCalculation_PrimaryOrSecondary( this, kv.Key, RandThatWillBeReset, kv.Value );
                    }
                }
                this.attackPlan.Display.SecondaryTargetDamageQueue.Clear();

                //and then clear the attack plan
                this.attackPlan.Display.ClearTargetingBitsOnly();
            }

            return false; //guess we did nothing
        }
        #endregion

        #region StartNPCAction
        public bool StartNPCAction( MersenneTwister RandThatWillBeReset )
        {
            if ( this.OverridingActionWillWarpOut ) //safety measure
                this.WantsToPerformAction = NPCActionDesire.ShowWarpOut;

            if ( this.WantsToPerformAction == NPCActionDesire.None )
                return false;

            switch ( this.WantsToPerformAction )
            {
                case NPCActionDesire.ShowAppearance:
                    this.WantsToPerformAction = NPCActionDesire.None; //this is enough to start it appearing
                    return false; //we did do something, but don't block others from doing things
                case NPCActionDesire.ShowWarpOut:

                    if ( !this.LangKeyForDisbandPopup.IsEmpty() )
                    {
                        #region Popup Notices
                        ArcenDoubleCharacterBuffer buffer = DamageTextPopups.GetTextBufferAppropriateForThreadOrNull();
                        if ( buffer != null )
                        {
                            Vector3 startLocation = this.GetPositionForCollisions().PlusY( this.GetHalfHeightForCollisions() + (Engine_HotM.GameMode == MainGameMode.CityMap ? 3f : 1f) );
                            float popupTextScale = Engine_HotM.GameMode == MainGameMode.CityMap ? 2f : 0.8f;

                            Vector3 endLocation = startLocation.PlusY( this.GetHalfHeightForCollisions() + 0.2f );
                            buffer.AddLang( this.LangKeyForDisbandPopup, ColorRefs.DisbandPopupTextColor.ColorHexWithHDR );
                            DamageTextPopups.FloatingDamageTextPopup newDamageText = DamageTextPopups.CreateNewFromTextBuffer( buffer,
                                startLocation, endLocation, popupTextScale, MathA.Max( 2, GameSettings.Current.GetInt( "DamagePopup_LingerTime" ) ) );
                            newDamageText.PhysicalDamageIncluded = 0;
                            newDamageText.MoraleDamageIncluded = 0;
                            newDamageText.SquadDeathsIncluded = 0;

                            this.LangKeyForDisbandPopup = string.Empty;
                        }
                        #endregion
                    }

                    if ( this.IsManagedUnit != null )
                        HandbookRefs.HumansComeAndGoInWaves.DuringGame_UnlockIfNeeded( false );
                    this.IsManagedUnit?.OnManagedUnitDespawnWillingly( this );
                    this.DisbandNPCUnit( NPCDisbandReason.WantedToLeave );
                    this.WantsToPerformAction = NPCActionDesire.None;
                    return false; //we did do something, but don't block others from doing things
            }

            //for all the others, just let them fall through for now
            this.WantsToPerformAction = NPCActionDesire.None;

            RandThatWillBeReset.ReinitializeWithSeed( this.CurrentTurnSeed + this.NonSimUniqueID );

            if ( this.ObjectiveIsCompleteAndWaiting )
            {
                if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                    this.DebugText += "SNA-ObjectiveIsCompleteAndWaiting\n";
                this.CurrentObjective.Implementation.DoObjectiveCompleteLogicForNPCUnit( this, this.CurrentObjective, RandThatWillBeReset, true );
                this.ObjectiveIsCompleteAndWaiting = false;
                return true;
            }

            if ( this.SpecialtyActionStringToBeDoneNext.Length > 0 )
            {
                if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                    this.DebugText += "SNA-SpecialtyActionStringToBeDoneNext:" + this.SpecialtyActionStringToBeDoneNext + "\n";
                _stance?.Implementation.HandleLogicForNPCUnitInStance( this, _stance, NPCUnitStanceLogic.CheckForSpecialtyNPCAction,
                    RandThatWillBeReset, null, null, null );
                this.SpecialtyActionStringToBeDoneNext = string.Empty;
                return true;
            }
            else if ( this.TargetActor != null && this.TargetLocation.x == float.NegativeInfinity )
            {
                if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                    this.DebugText += "SNA-TargetActorAndLocationBEFOREAttack\n";
                this.TargetActor = null;
                this.TargetLocation = Vector3.negativeInfinity;
                ArcenDebugging.LogWithStack( (this.UnitType?.ID ?? "null") + ": Wanted to do what would once have been a scheduled attack (A), but that's not a thing now.", Verbosity.ShowAsError );
                return false;
            }
            else //when there is no target unit
            {
                NPCUnitStance stance = this.Stance;
                if ( stance != null )
                {
                    RandThatWillBeReset.ReinitializeWithSeed( this.CurrentTurnSeed + this.NonSimUniqueID );
                    if ( this.TargetLocation.x > float.NegativeInfinity )
                    {
                        if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                            this.DebugText += "SNA-TargetLocation\n";
                        stance.Implementation.HandleLogicForNPCUnitInStance( this, stance, NPCUnitStanceLogic.MoveLogic,
                            RandThatWillBeReset, null, null, null );
                        this.TargetLocation = Vector3.negativeInfinity;
                        return true;
                    }
                    else
                    {
                        //we did nothing and that's okay
                        return false;
                    }
                }
            }

            return false; //guess we did nothing
        }
        #endregion

        public void DoNPCPerFrameLogic()
        {
            if ( this.IsFullDead )
            {
                if ( this.IsInvalid )
                    return; //already back in the pool
                if ( Engine_HotM.SelectedActor != this )
                    this.ReturnToPool(); //only return to the pool if not already in the pool and the player does not have this selected
                return;
            }

            if ( this.DisbandAsSoonAsNotSelected )
            {
                ISimMapActor selected = Engine_HotM.SelectedActor;
                if ( selected != this )
                    this.DisbandNPCUnit( NPCDisbandReason.WantedToLeave );
                return;
            }

            this.PerFrameValidateActorData();

            if ( this.isMoving )
            {
                SimCommon.NPCsMoving_MainThreadOnly.AddToConstructionList( this );
                if ( !Engine_HotM.GetIsNPCMovementBlocked() )
                    this.DoPerFrameMovementLogic();
            }

            if ( this.attackPlan.Display.TargetForStartOfNextTurn != null )
                SimCommon.NPCsWithTargets_MainThreadOnly.AddToConstructionList( this );

            #region UnitMorale Handling
            {
                MapActorData morale = this.GetActorDataData( ActorRefs.UnitMorale, true );
                if ( morale != null && morale.Current <= 0 && morale.Maximum > 0 )
                {
                    //we running!

                    Vector3 startLocation = this.GetPositionForCollisions();
                    Vector3 endLocation = startLocation.PlusY( this.GetHalfHeightForCollisions() + 0.2f );
                    ArcenDoubleCharacterBuffer buffer = DamageTextPopups.GetTextBufferAppropriateForThreadOrNull();
                    if ( buffer != null )
                    {
                        buffer.AddLang( "IncapacitatedPopup", IconRefs.HumansRunningAway.DefaultColorHexWithHDRHex );
                        DamageTextPopups.FloatingDamageTextPopup newDamageText = DamageTextPopups.CreateNewFromTextBuffer( buffer,
                            startLocation, endLocation, 0.8f, MathA.Max( 2, GameSettings.Current.GetInt( "DamagePopup_LingerTime" ) ) );
                        newDamageText.PhysicalDamageIncluded = 0;
                        newDamageText.MoraleDamageIncluded = 0;
                        newDamageText.SquadDeathsIncluded = 0;
                    }

                    if ( this.GetIsPartOfPlayerForcesInAnyWay() )
                    {
                        CityStatisticTable.AlterScore( "AlliedHumansWhoLostAllMorale", this.CurrentSquadSize );

                        CityStatisticTable.AlterScore( "AlliedHumanSquadsWhoLostAllMorale", 1 );
                    }
                    else
                    {
                        CityStatisticTable.AlterScore( "HumansWhoLostAllMorale", this.CurrentSquadSize );

                        CityStatisticTable.AlterScore( "HumanSquadsWhoLostAllMorale", 1 );
                    }

                    this.DisbandNPCUnit( NPCDisbandReason.WantedToLeave );
                    return;
                }
            }
            #endregion

            #region Start Action If Moving Into Range Of Selected, Or If In Camera Frustum
            if ( this.GetHasAnyActionItWantsTodo() && !Engine_HotM.GetIsNPCMovementBlocked() )
            {
                ISimMapActor selected = Engine_HotM.SelectedActor;
                if ( selected != null )
                {
                    Vector3 drawLoc = this.GetDrawLocation();
                    float rangeSquared = selected.GetNPCRevealRangeSquared();
                    Vector3 selectedLoc = selected.GetPositionForCollisions();
                    if ( (this.TargetLocation - selectedLoc).GetSquareGroundMagnitude() <= rangeSquared ||
                        (drawLoc - selectedLoc).GetSquareGroundMagnitude() <= rangeSquared ||
                        CameraCurrent.TestFrustumPointFastWithRequiredXZDistanceFromCamera( drawLoc ) ||
                        CameraCurrent.TestFrustumPointFastWithRequiredXZDistanceFromCamera( this.TargetLocation ) )
                    {
                        if ( CameraCurrent.TestFrustumPointInternalFast( this.GetDrawLocation() ) == TestFrustumResults.Inside ) //only do it if in the view of the camera
                            this.StartNPCAction( workingRandomMainThreadOnlyShouldBeReset ); //we are indeed on the main thread
                    }
                }
            }
            #endregion

            NPCUnitType unitType = this.UnitType;

            if ( unitType != null && unitType.DestroyIntersectingBuildingsStrength > 0 && (!this.hasDoneStompCheckSinceCreationOrLoad ||
                (unitType.ShouldDestroyIntersectingBuildingsDuringMovement && isMoving && !this.IsCloaked )) &&
                (unitType.ShouldDestroyIntersectingBuildingsDuringMovement || !isMoving) && !this.isAirDropping && this.Materializing == MaterializeType.None )
            {
                if ( !this.isMoving ) //if we got here by moving, then keep doing it
                    this.hasDoneStompCheckSinceCreationOrLoad = true;

                #region If We Are Here To Destroy Buildings
                {
                    staticWorkingBuildings.Clear();
                    CityMap.FillListOfBuildingsIntersectingCollidable( this, this.drawLocation,
                        this.objectRotation.eulerAngles.y, staticWorkingBuildings, false );

                    if ( staticWorkingBuildings.Count > 0 )
                    {
                        bool isPlayerControlledUnit = this.GetIsPlayerControlled();

                        int particleCount = (FrameBufferManagerData.ParticleCount.Display + Engine_HotM.NonSim_TotalParticleEffectsPlayingNow);
                        foreach ( MapItem bld in staticWorkingBuildings )
                        {
                            ISimBuilding building = bld?.SimBuilding;
                            if ( building == null )
                                continue;
                            if ( building?.GetStatus()?.ShouldBuildingBeNonColliding ?? false )
                                continue; //don't collide with invisible buildings
                            if ( bld.Type.ExtraPlaceableData.ResistanceToDestructionFromCollisions > unitType.DestroyIntersectingBuildingsStrength )
                                continue; //if this blocks being stepped on, then don't stop on it

                            int deaths = building.KillEveryoneHere();
                            if ( deaths > 0 )
                            {
                                if ( isPlayerControlledUnit )
                                    CityStatisticRefs.MurdersByBuildingCollapse.AlterScore_CityAndMeta( deaths );
                                else
                                    CityStatisticRefs.DeathsByBuildingCollapseByThirdParties.AlterScore_CityAndMeta( deaths );
                            }

                            building.SetStatus( CommonRefs.DemolishedBuildingStatus );

                            bool dropMoreParticles = particleCount > 80 ? Engine_Universal.PermanentQualityRandom.Next( 0, 14400 ) > particleCount * particleCount : //120 squared
                                Engine_Universal.PermanentQualityRandom.Next( 0, 120 ) > particleCount;
                            if ( dropMoreParticles )
                            {
                                particleCount++;
                                ParticleSoundRefs.BasicBuildingExplode.DuringGame_PlayAtLocation( bld.OBBCache.BottomCenter,
                                    new Vector3( 0, Engine_Universal.PermanentQualityRandom.Next( 0, 360 ), 0 ) );
                            }
                        }
                        staticWorkingBuildings.Clear();
                    }
                }
                #endregion
            }

            if ( unitType.CapturedByTrapBecomes != null && this.GetStatusIntensity( StatusRefs.CapturedInTrap) > 0 )
            {
                int number = MathA.Max( 1, this.CurrentSquadSize );
                unitType.CapturedByTrapBecomes.AlterCurrent_Named( number, "Increase_CapturedUnits", ResourceAddRule.StoreExcess );
                unitType.CapturedByTrapStatistic?.AlterScore_CityAndMeta( number );
                unitType.CapturedByTrapUnlocks?.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, true, true, false );
                this.DisbandNPCUnit( NPCDisbandReason.WantedToLeave );
            }
            else if ( this.GetStatusIntensity( StatusRefs.HeadedForTormentVessel ) > 0 )
            {
                int number = MathA.Max( 1, this.CurrentSquadSize );
                ResourceRefs.TormentedHumans?.AlterCurrent_Named( number, "Increase_CapturedUnits", ResourceAddRule.StoreExcess );
                CityStatisticTable.Instance.GetRowByID( "CombatantsSentToTormentVessels" )?.AlterScore_CityAndMeta( number );
                this.DisbandNPCUnit( NPCDisbandReason.WantedToLeave );
            }
        }

        #region DoPerFrameMovementLogic
        private MersenneTwister workingRandomMainThreadOnlyShouldBeReset = new MersenneTwister( 0 );
        private void DoPerFrameMovementLogic()
        {
            NPCUnitType unitType = this.UnitType;
            if ( unitType != null && unitType.IsMechStyleMovement )
                this.moveProgress += (ArcenTime.SmoothUnpausedDeltaTime * 4 * InputCaching.MoveSpeed_NPCMech) * Mathf.Max( 0.05f, unitType.MechStyleMovementSpeed ); //the speed of movement
            else if ( unitType != null && unitType.IsVehicle )
                this.moveProgress += (ArcenTime.SmoothUnpausedDeltaTime * 5 * InputCaching.MoveSpeed_NPCVehicle); //the speed of movement
            else
                this.moveProgress += (ArcenTime.SmoothUnpausedDeltaTime * 5 * InputCaching.MoveSpeed_SmallNPC); //the speed of movement

            SimCommon.IsSomeUnitMoving.Construction = true;

            if ( this.moveProgress >= 1 )
            {
                this.isMoving = false;

                Vector3 targetFinalLoc = this.desiredNewContainerLocation != null ? this.desiredNewContainerLocation.GetEffectiveWorldLocationForContainedUnit() : this.desiredNewGroundLocation;
                this.drawLocation = targetFinalLoc;

                ISimUnitLocation reachedContainerLocation = this.desiredNewContainerLocation;
                if ( reachedContainerLocation != null )
                {
                    this.SetActualContainerLocation( reachedContainerLocation );
                }
                else
                {
                    this.SetActualGroundLocation( this.desiredNewGroundLocation );
                }

                if ( !this.NextMoveIsSilent )
                {
                    if ( this.UnitType.OnMovementFinishedWithNoAction != null )
                        this.UnitType.OnMovementFinishedWithNoAction.DuringGame_PlayAtLocation( this.GetDrawLocation(), this.objectRotation.eulerAngles );

                    if ( this.UnitType.ScreenShake_OnRegularMove_Duration > 0 )
                        ScreenShake.StartShake( this.UnitType.ScreenShake_OnRegularMove_Duration,
                            this.UnitType.ScreenShake_OnRegularMove_Intensity,
                            this.UnitType.ScreenShake_OnRegularMove_DecreaseFactor );
                }
                this.NextMoveIsSilent = false;

                NPCUnitStance stance = this.Stance;
                if ( stance != null )
                {
                    if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                        this.DebugText += "MOV-ChecksForNPCAfterMove\n";
                    workingRandomMainThreadOnlyShouldBeReset.ReinitializeWithSeed( this.CurrentTurnSeed + this.NonSimUniqueID );
                    stance.Implementation.HandleLogicForNPCUnitInStance( this, stance, NPCUnitStanceLogic.ChecksForNPCAfterMove,
                        workingRandomMainThreadOnlyShouldBeReset, null, null, null );
                }

                if ( Engine_HotM.SelectedActor == this )
                {
                    //if the player has this selected, move their view here
                    CameraCurrent.HaveFocusedOnNewTargetYet_Streets = false;
                    CameraCurrent.HaveFocusedOnNewTargetYet_CityMap = false;
                }

                #region If We Are Here To Destroy Buildings
                if ( this.UnitType.DestroyIntersectingBuildingsStrength > 0 && !this.isAirDropping )
                {
                    this.hasDoneStompCheckSinceCreationOrLoad = true;
                    staticWorkingBuildings.Clear();
                    CityMap.FillListOfBuildingsIntersectingCollidable( this, this.drawLocation,
                        this.objectRotation.eulerAngles.y, staticWorkingBuildings, false );

                    if ( staticWorkingBuildings.Count > 0 )
                    {
                        int particleCount = (FrameBufferManagerData.ParticleCount.Display + Engine_HotM.NonSim_TotalParticleEffectsPlayingNow);
                        foreach ( MapItem bld in staticWorkingBuildings )
                        {
                            if ( bld.SimBuilding == null )
                                continue;
                            if ( bld.SimBuilding?.GetStatus()?.ShouldBuildingBeNonColliding ?? false )
                                continue; //don't collide with invisible buildings
                            if ( bld.Type.ExtraPlaceableData.ResistanceToDestructionFromCollisions > unitType.DestroyIntersectingBuildingsStrength )
                                continue; //if this blocks being stepped on, then don't stop on it

                            bld.SimBuilding.SetStatus( CommonRefs.DemolishedBuildingStatus );

                            bool dropMoreParticles = particleCount > 80 ? Engine_Universal.PermanentQualityRandom.Next( 0, 14400 ) > particleCount * particleCount : //120 squared
                                Engine_Universal.PermanentQualityRandom.Next( 0, 120 ) > particleCount;
                            if ( dropMoreParticles )
                            {
                                particleCount++;
                                ParticleSoundRefs.BasicBuildingExplode.DuringGame_PlayAtLocation( bld.OBBCache.BottomCenter,
                                    new Vector3( 0, Engine_Universal.PermanentQualityRandom.Next( 0, 360 ), 0 ) );
                            }
                        }
                        staticWorkingBuildings.Clear();
                    }
                }
                #endregion

                SimCommon.NeedsToAttemptAnotherNPCTargetingPass = true;
                return;
            }

            Vector3 targetLoc = this.desiredNewContainerLocation != null ? this.desiredNewContainerLocation.GetEffectiveWorldLocationForContainedUnit() : this.desiredNewGroundLocation;

            this.drawLocation = Vector3.Lerp( this.originalSourceLocation, targetLoc, this.moveProgress );
            if ( unitType != null && unitType.IsMechStyleMovement )
            {
                float lowY = -( this.GetHalfHeightForCollisions() * 1.5f ) * unitType.MechStyleMovementDip;
                if ( this.moveProgress < 0.5f )
                {
                    float yShift = Mathf.Lerp( this.originalSourceLocation.y, lowY, this.moveProgress * 2f );
                    this.drawLocation.y = yShift;
                }
                else if ( this.moveProgress < 1f )
                {
                    float yShift = Mathf.Lerp( lowY, targetLoc.y, ( this.moveProgress - 0.5f ) * 2f );
                    this.drawLocation.y = yShift;
                }
            }
            else if ( unitType == null || !unitType.IsVehicle )
            {
                float highY = (this.GetHalfHeightForCollisions() * 4.5f);
                if ( this.moveProgress < 0.5f )
                {
                    float yShift = Mathf.Lerp( 0, highY, this.moveProgress * 2f );
                    this.drawLocation.y += yShift;
                }
                else if ( this.moveProgress < 1f )
                {
                    float yShift = Mathf.Lerp( highY, 0, (this.moveProgress - 0.5f) * 2f );
                    this.drawLocation.y += yShift;
                }
            }

            this.RotateObjectToFace( this.originalSourceLocation, targetLoc, ArcenTime.UnpausedDeltaTime * SimCommon.CurrentVisualSpeed * 80 ); //rotate over time

            ISimMapActor selectedActor = Engine_HotM.SelectedActor;
            if ( selectedActor != null )
            {
                float selectedRange = selectedActor.GetAttackRangeSquared();
                if ( ( this.drawLocation - selectedActor.GetPositionForCollisions() ).GetSquareGroundMagnitude() <= selectedRange )
                {
                    this.IsNPCInFogOfWar = false; //handle this here so that it appears while moving into range!
                }
            }
        }
        #endregion

        protected override void HandleAnyBitsFromBeingRenderedInAnyFashion()
        {

        }

        #region MarkParticleLoopActiveFromMainFrame
        public void MarkParticleLoopActiveFromMainFrame( ActorParticleLoopReason Reason )
        {
            if ( Reason == null )
                return;

            if ( !this.UnitType.ParticleLoops.TryGetValue( Reason.ID, out SubParticleLoop loop ) )
            {
                ArcenDebugging.LogSingleLine( "NPC unit type '" + this.UnitType.ID + "' is not st up to have ActorParticleLoopReason '" + Reason.ID + "'!", Verbosity.ShowAsError );
                return;
            }

            if ( this.floatingParticleLoop == null || !this.floatingParticleLoop.GetIsValidToUse( this ) ||
                this.floatingParticleLoop.ParticleLoop != loop.Loop )
            {
                this.floatingParticleLoop = loop.Loop.ParticleLoopPool.GetFromPool( this );
                this.floatingParticleLoop.CollisionLayer = CollisionLayers.IconMixedIn;
            }

            this.floatingParticleLoop.LocalScale = loop.Scale;
            this.floatingParticleLoop.Rotation = this.GetDrawRotation();
            this.floatingParticleLoop.WorldLocation = this.GetDrawLocation() + loop.PositionOffset;
            this.floatingParticleLoop.MarkAsStillInUseThisFrame();
        }
        #endregion

        #region DoPerFrameDrawBecauseExistsInOrOutOfCameraView
        private static MersenneTwister perFrameCatchupActionRandThatIsReset = new MersenneTwister( 0 );
        public void DoPerFrameDrawBecauseExistsInOrOutOfCameraView( out bool IsMouseOver, Int64 FramesPrepped, out bool TooltipShouldBeAtCursor, out bool ShouldSkipDrawing, out bool ShouldDrawAsSilhouette )
        {
            TooltipShouldBeAtCursor = false;
            ShouldSkipDrawing = false;
            ShouldDrawAsSilhouette = false;
            IsMouseOver = false;

            if ( this.LastFramePrepRendered >= FramesPrepped || this.IsFullDead )
            {
                ShouldSkipDrawing = true;
                return;
            }
            this.LastFramePrepRendered = FramesPrepped;

            NPCUnitType unitType = this.UnitType;
            if ( unitType == null )
            {
                ShouldSkipDrawing = true;
                return;
            }
            if ( this.DisbandAsSoonAsNotSelected )
            {
                ShouldSkipDrawing = true;
                return;
            }
            
            if ( unitType.MechAirdropsInFromHeight > 0 )
            {
                if ( this.isAirDropping )
                {
                    this.airdropSpeed += ( ArcenTime.UnpausedDeltaTime * unitType.MechAirdropAcceleration );
                    this.airdropHeight -= (ArcenTime.UnpausedDeltaTime * this.airdropSpeed);
                    if ( this.airdropHeight <= 0 )
                    {
                        this.airdropSpeed = 0;
                        this.airdropHeight = 0;
                        this.isAirDropping = false;

                        if ( this.UnitType.ScreenShake_OnMechAirdrops_Duration > 0 )
                            ScreenShake.StartShake( this.UnitType.ScreenShake_OnMechAirdrops_Duration,
                                this.UnitType.ScreenShake_OnMechAirdrops_Intensity,
                                this.UnitType.ScreenShake_OnMechAirdrops_DecreaseFactor );

                        if ( unitType.OnAirdropEnd != null )
                            unitType.OnAirdropEnd.DuringGame_PlayAtLocation( this.GetDrawLocation(), this.objectRotation.eulerAngles );
                    }
                }
                else
                {
                    switch ( this.Materializing )
                    {
                        case MaterializeType.Appear:
                        case MaterializeType.Reveal:
                            {
                                if ( unitType.OnAirdropStart != null )
                                    unitType.OnAirdropStart.DuringGame_PlayAtLocation( this.GetDrawLocation(), this.objectRotation.eulerAngles );

                                this.Materializing = MaterializeType.None;
                                this.airdropHeight = unitType.MechAirdropsInFromHeight;
                                this.airdropSpeed = unitType.MechAirdropStartingSpeed;
                                this.isAirDropping = true;

                                Engine_HotM.SetGameMode( MainGameMode.Streets );
                                VisManagerMidBase.Instance.MainCamera_JumpCameraToPosition( this.GetDrawLocation(), false );
                                VisManagerMidBase.Instance.MainCamera_SetCameraZoom( unitType.MechAirdropZoomDistance, false );
                            }
                            break;
                    }
                }
            }

            bool isMapView = Engine_HotM.GameMode == MainGameMode.CityMap && !unitType.RendersOnTheCityMap;

            if ( this.GetIsCurrentlyInvisible( InvisibilityPurpose.ForPlayerTargeting ) )
            {
                if ( !isMapView && this.GetIsCurrentlyInSilhouetteMode() )
                    ShouldDrawAsSilhouette = true;
                else
                {
                    ShouldSkipDrawing = true;
                    if ( !isMapView )
                    {
                        switch ( this.Materializing )
                        {
                            case MaterializeType.None:
                            case MaterializeType.Appear:
                                if ( !SimCommon.IsFogOfWarDisabled ) //only do the below if we're not in the fog of war
                                {
                                    this.Materializing = MaterializeType.Reveal;
                                    this.MaterializingProgress = 0;
                                }
                                break;
                        }
                    }
                    return;
                }
            }

            if ( isMapView )
            {
                switch ( this.Materializing )
                {
                    case MaterializeType.Appear:
                    case MaterializeType.Reveal:
                    case MaterializeType.WarpOut:
                        this.Materializing = MaterializeType.None;
                        this.MaterializingProgress = 0;
                        break;
                }
            }

            switch ( this.Materializing )
            {
                case MaterializeType.Reveal:
                    if ( SimCommon.IsFogOfWarDisabled ) //if the fog of war is skipped, then skip this also
                    {
                        this.Materializing = MaterializeType.None;
                        this.MaterializingProgress = 0;
                    }
                    break;
            }

            if ( !isMapView && Engine_HotM.SelectedActor != this )
            {
                MapCell cell = this.CalculateMapCell();
                if ( cell != null && !cell.IsConsideredInCameraView )
                {
                    if ( this.UnitType.IsMechStyleMovement || this.UnitType.IsVehicle ) //more checking required
                    {
                        float halfHeight = this.GetHalfHeightForCollisions();
                        Vector3 center = this.GetPositionForCollisions();
                        float radius = this.GetRadiusForCollisions();

                        //out of view, so skip
                        if ( !CameraCurrent.TestFrustumColliderInternalFast( center, halfHeight, radius ) )
                        {
                            ShouldSkipDrawing = true;
                            FrameBufferManagerData.CellFrustumCullCount.Construction++;
                        }
                    }
                    else
                    {
                        ShouldSkipDrawing = true;
                        FrameBufferManagerData.CellFrustumCullCount.Construction++;
                    }
                }
            }

            if ( !ShouldSkipDrawing )
            {
                if ( this.OverridingActionWillWarpOut ) //safety measure
                    this.WantsToPerformAction = NPCActionDesire.ShowWarpOut;

                if ( this.WantsToPerformAction != NPCActionDesire.None && !this.GetShouldActionsBeWatchedByPlayer() ) //and the player doesn't care about it
                {
                    if ( isMapView )
                        this.StartNPCAction( perFrameCatchupActionRandThatIsReset );
                    else
                    {
                        float halfHeight = this.GetHalfHeightForCollisions();
                        Vector3 center = this.GetPositionForCollisions();
                        float radius = this.GetRadiusForCollisions();

                        //out of view, so skip
                        if ( !CameraCurrent.TestFrustumColliderInternalFast( center, halfHeight, radius ) )
                            this.StartNPCAction( perFrameCatchupActionRandThatIsReset );
                    }
                }

                if ( isMapView )
                {
                    switch ( this.Materializing )
                    {
                        case MaterializeType.Appear:
                        case MaterializeType.Reveal:
                        case MaterializeType.WarpOut:
                            this.Materializing = MaterializeType.None;
                            this.MaterializingProgress = 0;
                            break;
                    }
                }

                if ( this.Materializing != MaterializeType.None && ArcenUI.CurrentlyShownWindowsWith_ShouldBlurBackgroundGame.Count == 0 &&
                    !VisCurrent.IsShowingActualEvent && FlagRefs.HasEmergedIntoMap.DuringGameplay_IsTripped &&
                    this.WantsToPerformAction != NPCActionDesire.ShowAppearance ) //if this status is here, then wait
                {
                    if ( this.MaterializingProgress <= 0 && ( ( SimCommon.SecondsSinceLoaded >= 5 || SimCommon.HasDoneSomethingToTriggerNPCArrivalsSinceLoad) && SimCommon.Turn > 1 ) )
                    {
                        VisParticleAndSoundUsage managedAppear = this.IsManagedUnit?.OnAppear;
                        if ( managedAppear == null )
                            managedAppear = this.IsCityConflictUnit?.OnAppear;

                        if ( managedAppear != null )
                        {
                            if ( managedAppear.IsConsideredABigScarySound )
                            {
                                managedAppear.DuringGame_PlaySoundOnlyAtCamera(); //play the sound more loud and center
                                managedAppear.DuringGame_PlayAtLocation( this.GetDrawLocation(), true ); //this plays just the visuals
                            }
                            else
                            {
                                int particleCount = (FrameBufferManagerData.ParticleCount.Display + Engine_HotM.NonSim_TotalParticleEffectsPlayingNow);

                                bool dropMoreParticles = particleCount > 80 ? Engine_Universal.PermanentQualityRandom.Next( 0, 14400 ) > particleCount * particleCount : //120 squared
                                    Engine_Universal.PermanentQualityRandom.Next( 0, 120 ) > particleCount;
                                if ( dropMoreParticles )
                                {
                                    managedAppear.DuringGame_PlayAtLocation( this.GetDrawLocation() );
                                }
                            }
                        }
                        else
                        {
                            int particleCount = (FrameBufferManagerData.ParticleCount.Display + Engine_HotM.NonSim_TotalParticleEffectsPlayingNow);

                            bool dropMoreParticles = particleCount > 80 ? Engine_Universal.PermanentQualityRandom.Next( 0, 14400 ) > particleCount * particleCount : //120 squared
                                Engine_Universal.PermanentQualityRandom.Next( 0, 120 ) > particleCount;
                            if ( dropMoreParticles )
                            {
                                if ( this.TurnsHasExisted < 1 )
                                    this.UnitType.OnAppearAsNewUnit.DuringGame_PlayAtLocation( this.GetDrawLocation() );
                                else
                                    ParticleSoundRefs.NPCAppear_NowVisible.DuringGame_PlayAtLocation( this.GetDrawLocation() );
                            }
                        }
                    }

                    this.MaterializingProgress += ArcenTime.SmoothUnpausedDeltaTime * this.Materializing.GetSpeed();
                    if ( this.MaterializingProgress >= 1.1f ) //go an extra 10%
                        this.Materializing = MaterializeType.None;
                }
            }
            else //YES should skip drawing
            {
                if ( this.WantsToPerformAction != NPCActionDesire.None && !this.GetShouldActionsBeWatchedByPlayer() ) //and the player doesn't care about it
                    this.StartNPCAction( perFrameCatchupActionRandThatIsReset );
            }

            if ( ShouldSkipDrawing )
                return;

            this.HandleAnyBitsFromBeingRenderedInAnyFashion();

            if ( isMapView )
                return; //make sure never to do the below on the map

            float effectiveScale = unitType.ColliderScale;
            if ( InputCaching.NPCExtraHitboxScaleDuringTargeting > 1f && unitType.ColliderScale > 1f && Engine_HotM.SelectedActor is ISimMachineActor && !this.GetIsPartOfPlayerForcesInAnyWay() )
                effectiveScale *= InputCaching.NPCExtraHitboxScaleDuringTargeting;

            bool isMouseoverThis = false;
            if ( this.VisLODObject != null )
            {
                #region Floating LOD Object
                if ( this.CustomVisLODObject != null )
                    this.VisLODObject = this.CustomVisLODObject;
                VisLODDrawingObject toDraw = this.VisLODObject;

                IAutoPooledFloatingLODObject fLODObject = this.floatingLODObject;

                if ( fLODObject == null || !fLODObject.GetIsValidToUse( this ) ||
                    fLODObject.Object.ID != toDraw.ID ) //if we are switching between casual and aggressive stance
                {
                    if ( toDraw.FloatingObjectPool == null )
                    {
                        ArcenDebugging.LogSingleLine( "Null FloatingObjectPool on VisSimpleDrawingObject '" + toDraw.ID +
                            "'! Be sure and set create_auto_pooled_floating_object=\"true\"!", Verbosity.ShowAsError );
                        return;
                    }
                    fLODObject = toDraw.FloatingObjectPool.GetFromPool( this );
                    this.floatingLODObject = fLODObject;
                    fLODObject.CollisionLayer = CollisionLayers.VehicleMixedIn;

                    if ( !this.hasEverSetRotation )
                    {
                        this.hasEverSetRotation = true;
                        float y = Engine_Universal.PermanentQualityRandom.Next( 0, 360 );
                        this.objectRotation = Quaternion.Euler( 0, y, 0 );
                    }
                    //this.RotateObjectToFace( EffectivePosition, Engine_HotM.MouseWorldLocation, 1000 ); //rotate correctly immediately

                }
                else
                {
                    if ( !this.isMoving ) //only do this passive rotation when we are not moving
                    {
                        if ( !this.hasEverSetRotation )
                        {
                            this.hasEverSetRotation = true;
                            float y = Engine_Universal.PermanentQualityRandom.Next( 0, 360 );
                            this.objectRotation = Quaternion.Euler( 0, y, 0 );
                        }

                        //{
                        //     float y = this.objectRotation.y;
                        //     y += (ArcenTime.UnpausedDeltaTime * SimCommon.CurrentVisualSpeed * 20f);
                        //     if ( y >= 360f )
                        //         y -= 360f;
                        //     this.objectRotation = Quaternion.Euler( 0, y, 0 );
                        // }
                    }

                    //this.RotateObjectToFace( EffectivePosition, Engine_HotM.MouseWorldLocation, ArcenTime.UnpausedDeltaTime * SimCommon.CurrentVisualSpeed * 20f ); //rotate over time
                }

                if ( VisCurrent.IsInPhotoMode || effectiveScale <= 1f )
                    fLODObject.ColliderScale = 1f;
                else
                {
                    float roughDistanceFromCamera = (CameraCurrent.CameraBodyPosition - this.drawLocation).GetLargestAbsXZY();
                    if ( roughDistanceFromCamera < 4 )
                        fLODObject.ColliderScale = 1f;
                    else if ( roughDistanceFromCamera > 12 )
                        fLODObject.ColliderScale = effectiveScale;
                    else
                        fLODObject.ColliderScale = 1f + ((effectiveScale - 1f) * ((roughDistanceFromCamera - 4f) / 8f));
                }

                bool isMapViewScaling = isMapView;

                fLODObject.ObjectScale = isMapViewScaling ? unitType.VisObjectScale * MathRefs.UnitExtraScaleOnCityMap.FloatMin : unitType.VisObjectScale;
                Vector3 loc;
                if ( unitType.EntireObjectAlwaysThisHeightAboveGround > 0 )
                    loc = this.drawLocation.ReplaceY( unitType.EntireObjectAlwaysThisHeightAboveGround ).PlusY( unitType.VisObjectExtraOffset + (isMapView ? unitType.VisObjectExtraOffsetOnCityMap : 0f) );
                else
                    loc = this.drawLocation.PlusY( unitType.VisObjectExtraOffset + (isMapView ? unitType.VisObjectExtraOffsetOnCityMap : 0f) );

                if ( this.isAirDropping )
                    loc.y += this.airdropHeight;
                fLODObject.WorldLocation = loc;

                fLODObject.MarkAsStillInUseThisFrame();
                fLODObject.Rotation = this.objectRotation;

                if ( !ShouldDrawAsSilhouette && fLODObject.IsMouseover )
                {
                    IsMouseOver = true;
                    this.RenderTooltip( null, SideClamp.Any, TooltipShadowStyle.None, false, ActorTooltipExtraData.None, TooltipExtraRules.None );
                }
                #endregion
            }
            else
            {
                #region Floating Simple Object
                if ( this.CustomVisSimpleObject != null )
                    this.VisSimpleObject = this.CustomVisSimpleObject;
                VisSimpleDrawingObject toDraw = this.VisSimpleObject;

                IAutoPooledFloatingObject fSimpleObject = this.floatingSimpleObject;

                if ( fSimpleObject == null || !fSimpleObject.GetIsValidToUse( this ) ||
                    fSimpleObject.OriginalObject.ID != toDraw.IDForObjectPoolChecks ) //if we are switching between a couple of different objects
                {
                    if ( toDraw.FloatingObjectPool == null )
                    {
                        ArcenDebugging.LogSingleLine( "Null FloatingObjectPool on VisSimpleDrawingObject '" + toDraw.ID +
                            "'! Be sure and set create_auto_pooled_floating_object=\"true\"!", Verbosity.ShowAsError );
                        return;
                    }
                    fSimpleObject = toDraw.FloatingObjectPool.GetFromPool( this );
                    this.floatingSimpleObject = fSimpleObject;
                    fSimpleObject.CollisionLayer = CollisionLayers.VehicleMixedIn;

                    if ( !this.hasEverSetRotation )
                    {
                        this.hasEverSetRotation = true;
                        float y = Engine_Universal.PermanentQualityRandom.Next( 0, 360 );
                        this.objectRotation = Quaternion.Euler( 0, y, 0 );
                    }
                    //this.RotateObjectToFace( EffectivePosition, Engine_HotM.MouseWorldLocation, 1000 ); //rotate correctly immediately

                }
                else
                {
                    if ( !this.isMoving ) //only do this passive rotation when we are not moving
                    {
                        if ( !this.hasEverSetRotation )
                        {
                            this.hasEverSetRotation = true;
                            float y = Engine_Universal.PermanentQualityRandom.Next( 0, 360 );
                            this.objectRotation = Quaternion.Euler( 0, y, 0 );
                        }

                        //{
                        //     float y = this.objectRotation.y;
                        //     y += (ArcenTime.UnpausedDeltaTime * SimCommon.CurrentVisualSpeed * 20f);
                        //     if ( y >= 360f )
                        //         y -= 360f;
                        //     this.objectRotation = Quaternion.Euler( 0, y, 0 );
                        // }
                    }

                    //this.RotateObjectToFace( EffectivePosition, Engine_HotM.MouseWorldLocation, ArcenTime.UnpausedDeltaTime * SimCommon.CurrentVisualSpeed * 20f ); //rotate over time
                }

                if ( VisCurrent.IsInPhotoMode || effectiveScale <= 1f )
                    fSimpleObject.ColliderScale = 1f;
                else
                {
                    float roughDistanceFromCamera = (CameraCurrent.CameraBodyPosition - this.drawLocation).GetLargestAbsXZY();
                    if ( roughDistanceFromCamera < 4 )
                        fSimpleObject.ColliderScale = 1f;
                    else if ( roughDistanceFromCamera > 12 )
                        fSimpleObject.ColliderScale = effectiveScale;
                    else
                        fSimpleObject.ColliderScale = 1f + ((effectiveScale - 1f) * ((roughDistanceFromCamera - 4f) / 8f));
                }

                bool isMapViewScaling = isMapView;

                fSimpleObject.EffectiveObject = toDraw;
                fSimpleObject.ObjectScale = isMapViewScaling ? unitType.VisObjectScale * MathRefs.UnitExtraScaleOnCityMap.FloatMin : unitType.VisObjectScale;
                Vector3 loc;
                if ( unitType.EntireObjectAlwaysThisHeightAboveGround > 0 )
                    loc = this.drawLocation.ReplaceY( unitType.EntireObjectAlwaysThisHeightAboveGround ).PlusY( unitType.VisObjectExtraOffset + (isMapView ? unitType.VisObjectExtraOffsetOnCityMap : 0f) );
                else
                    loc = this.drawLocation.PlusY( unitType.VisObjectExtraOffset + (isMapView ? unitType.VisObjectExtraOffsetOnCityMap : 0f) );

                if ( this.isAirDropping )
                    loc.y += this.airdropHeight;
                fSimpleObject.WorldLocation = loc;

                fSimpleObject.MarkAsStillInUseThisFrame();
                fSimpleObject.Rotation = this.objectRotation;

                if ( !ShouldDrawAsSilhouette && fSimpleObject.IsMouseover )
                {
                    IsMouseOver = true;
                    this.RenderTooltip( null, SideClamp.Any, TooltipShadowStyle.None, false, ActorTooltipExtraData.None, TooltipExtraRules.None );
                }
                #endregion
            }

            if ( !ShouldDrawAsSilhouette && this.Materializing == MaterializeType.None && !this.isAirDropping )
            {
                if ( InputCaching.IsInInspectMode_FocusOnLens && (SimCommon.CurrentCityLens?.HidesKeyNPCUnitsDuringInspectFocus ?? false) )
                {
                    //do not draw statuses!
                }
                else
                { 
                    int statusesToShow = this.StatusEffectCountToShowByHealthBar;
                    if ( InputCaching.ShowUnitHealthAndAboveHeadIcons || statusesToShow > 0 )
                    {
                        if ( (SimCommon.CurrentCityLens?.SkipPassiveGuardsOnStreets?.Display ?? false) && (this.Stance?.IsConsideredPassiveGuard ?? false) && statusesToShow <= 0 )
                        { }
                        else
                        {
                            this.DrawUnitStanceIcon( this.drawLocation, isMouseoverThis );

                            if ( isMouseoverThis || Engine_HotM.SelectedActor == this || (SimCommon.CurrentCityLens?.ShowHealthBars?.Display ?? false) ||
                                InputCaching.ShouldShowDetailedTooltips || InputCaching.IsInInspectMode_ShowMoreStuff ||
                                (this._stance?.AlwaysShowHealthBar ?? false) ||
                                (this.GetIsPlayerControlled() && (Engine_HotM.SelectedMachineActionMode?.ShowHealthBarsForYourUnits ?? false)) || statusesToShow > 0 )
                            {

                                MapActorData health = this.actorData.GetValueOrInitializeIfMissing( ActorRefs.ActorHP );
                                float scaleMultiplier = this.CalculateEffectiveScaleMultiplierFromCameraPositionAndMode();
                                float scale = scaleMultiplier * 0.4f;

                                Vector3 healthSpot = this.drawLocation;
                                if ( this.UnitType.EntireObjectAlwaysThisHeightAboveGround > 0 )
                                    healthSpot = healthSpot.ReplaceY( this.UnitType.EntireObjectAlwaysThisHeightAboveGround );

                                IconRefs.RenderHealthBar( health.Current, health.Maximum, healthSpot, -0.4f, scaleMultiplier, scale,
                                    InputCaching.ShouldShowDetailedTooltips || InputCaching.IsInInspectMode_ShowMoreStuff, this,
                                    isMouseoverThis || InputCaching.IsInInspectMode_ShowMoreStuff, this );
                            }
                        }
                    }
                }

                this.FromCohort?.DuringGame_DiscoverIfNeed();
            }
        }
        #endregion

        #region DrawUnitStanceIcon
        protected void DrawUnitStanceIcon( Vector3 EffectivePosition, bool isMouseoverThis )
        {
            if ( Engine_HotM.GameMode == MainGameMode.CityMap )
                return; //if map mode, then never draw this at all
            if ( VisCurrent.GetShouldBeBlurred() )
                return; //hide when in a blurred scene

            if ( InputCaching.IsInInspectMode_FocusOnLens )
            {
                if ( this.GetIsPartOfPlayerForcesInAnyWay() )
                    return;
            }

            Vector3 stanceSpot = EffectivePosition;
            if ( this.UnitType.EntireObjectAlwaysThisHeightAboveGround > 0 )
                stanceSpot = stanceSpot.ReplaceY( this.UnitType.EntireObjectAlwaysThisHeightAboveGround );
            stanceSpot = stanceSpot.PlusY( this.UnitType.HeightForCollisions + 0.2f + this.UnitType.StanceIconExtraOffset );

            NPCUnitObjective objective = this.CurrentObjective;
            if ( objective != null && objective.Icon != null ) //if this unit has an objective, then always draw that
            {
                IA5Sprite icon = objective.Icon;

                float scaleMultiplier = this.CalculateEffectiveScaleMultiplierFromCameraPositionAndMode();
                float scale = scaleMultiplier * objective.IconScale * InputCaching.Scale_UnitStanceIcons;
                icon.WriteToDrawBufferForOneFrame( true, stanceSpot, scale, objective.IconColor, false, false, true );

                this.DrawIncomingDamage( stanceSpot, scaleMultiplier );
                return;
            }


            NPCUnitStance stance = this.Stance;
            if ( stance == null )
                return; //don't draw if stance is null

            if ( !stance.IsStanceAlwaysDrawnInStreets )
            {
                if ( Engine_HotM.CurrentCommandModeActionTargeting != null && Engine_HotM.CurrentCommandModeActionTargeting.ShowAllPlayerNPCUnitStances && this.GetIsPlayerControlled() )
                { } //go forth and draw!
                else if ( !(SimCommon.CurrentCityLens?.ShowNPCUnitStances?.Display ?? false) && !(Engine_HotM.SelectedMachineActionMode?.ShowNPCUnitStances ?? false) 
                    && !isMouseoverThis && !InputCaching.IsInInspectMode_ShowMoreStuff )
                    return; //if not a key stance and not mousing over or in a mode that show stances, then skip
            }

            {
                IA5Sprite icon = stance.Icon;
                if ( icon == null )
                    return;

                float scaleMultiplier = this.CalculateEffectiveScaleMultiplierFromCameraPositionAndMode();
                float scale = scaleMultiplier * stance.IconScale * InputCaching.Scale_UnitStanceIcons;
                icon.WriteToDrawBufferForOneFrame( true, stanceSpot, scale, stance.IconColor, false, false, true );

                this.DrawIncomingDamage( stanceSpot, scaleMultiplier );
            }
        }
        #endregion

        private void DrawIncomingDamage( Vector3 EffectivePosition, float scaleMultiplier )
        {
            if ( SimCommon.NPCsWaitingToActOnTheirOwn.Count > 0 || SimCommon.NPCsWaitingToActAfterPlayerLooksAtThem.Count > 0 )
                return;
            if ( !this.GetIsPartOfPlayerForcesInAnyWay() )
                return;

            int incomingPhysicalDamage = this.IncomingDamage.Display.IncomingPhysicalDamageTargeting;
            //int incomingMoraleDamage = this.IncomingDamage.Display.IncomingMoraleDamageTargeting;
            if ( incomingPhysicalDamage > 0 )// || incomingMoraleDamage > 0 )
            {

                int currentHealth = this.GetActorDataCurrent( ActorRefs.ActorHP, true );
                if ( currentHealth > 0 )
                {
                    int progressPercentage = MathA.IntPercentageClamped( incomingPhysicalDamage, currentHealth, 0, 100 );
                    IA5Sprite progressIcon = IconRefs.DamageSprites[progressPercentage];

                    VisIconUsage style = IconRefs.IncomingDamageStyle;

                    progressIcon.WriteToDrawBufferForOneFrame( InputCaching.IsInInspectMode_Any, EffectivePosition + (CameraCurrent.CameraUp * (style.DefaultAddedY * scaleMultiplier)),
                        style.DefaultScale * 1.4f, style.DefaultColorHDR, false, false, true );
                }

                //MapActorData moraleData = this.GetActorDataData( ActorRefs.UnitMorale, true );
                //if ( moraleData != null && moraleData.Maximum > 0 )
                //{

                //}
            }
        }

        public bool GetDataForActualObjectDraw( out IAutoPooledFloatingLODObject fLODObject, out IAutoPooledFloatingObject fSimpleObject, out Color color )
        {
            if ( this.GetIsCurrentlyInvisible( InvisibilityPurpose.ForPlayerTargeting ) && !this.GetIsCurrentlyInSilhouetteMode() )
            {
                fLODObject = null;
                fSimpleObject = null;
                color = ColorMath.White;
                return false;
            }
            if ( this.VisLODObject != null )
            {
                //now actually do the draw, as the above just handles colliders and any particle effects
                fLODObject = this.floatingLODObject;
                fSimpleObject = null;
                color = Color.white;
                //if ( !this.IsFullyFadedIn )
                //    color.a = this.AlphaAmountFromFadeIn;
                return fLODObject != null && fLODObject.GetIsValidToUse( this );
            }
            else
            {
                //now actually do the draw, as the above just handles colliders and any particle effects
                fLODObject = null;
                fSimpleObject = this.floatingSimpleObject;
                color = this.UnitType.SimpleObjectColor;
                //if ( !this.IsFullyFadedIn )
                //    color.a = this.AlphaAmountFromFadeIn;
                return fSimpleObject != null && fSimpleObject.GetIsValidToUse( this );
            }
        }

        private const float ICON_HALF_HEIGHT = 0.9f;
        private const float ICON_MAP_POSITION_Y = 4f;

        #region CalculateEffectiveScaleMultiplierFromCameraPositionAndMode
        public float CalculateEffectiveScaleMultiplierFromCameraPositionAndMode()
        {
            NPCUnitType unitType = this.UnitType;
            if ( unitType == null )
                return 1f;

            bool isMapView = Engine_HotM.GameMode == MainGameMode.CityMap;
            float newScale = isMapView ? 4f : 1f;
            if ( isMapView )
            {
                //if we are very high, then scale the icon up
                if ( Engine_HotM.CameraHeight > 100 )
                {
                    float amountAbove = Engine_HotM.CameraHeight - 100;
                    if ( amountAbove >= 400 )
                        newScale += 8f;
                    else
                        newScale += (amountAbove / 400f) * 8f;
                }
            }
            else
            {
                //if we are a bit high, then scale the icon up
                if ( Engine_HotM.CameraHeight > 10 )
                {
                    float amountAbove = Engine_HotM.CameraHeight - 10;
                    if ( amountAbove >= 40 )
                        newScale += 3f;
                    else
                        newScale += (amountAbove / 40f) * 3f;
                }
            }
            newScale *= unitType.IconScale;
            return newScale;
        }
        #endregion

        #region RotateObjectToFace
        public void RotateObjectToFace( Vector3 effectivePosition, Vector3 otherPosition, float maxDelta )
        {
            if ( otherPosition.x == float.NegativeInfinity || otherPosition.y == float.NegativeInfinity )
                return;

            Vector3 targetLook = otherPosition - effectivePosition; //the sim is ahead of the vis location
            targetLook.y = 0f;

            if ( targetLook.sqrMagnitude <= 0.1f )
                return; //if the range is really short, don't rotate us; it will give us invalid rotations anyway, and cause error cascades

            Quaternion lookRot = Quaternion.LookRotation( targetLook, Vector3.up );

            if ( lookRot.x == float.NaN )
                return; //don't try to rotate to this!

            this.hasEverSetRotation = true;

            if ( maxDelta > 900 ) //if we want to get there so bad, just get there
                this.objectRotation = lookRot;
            else
            {
                /*float angle = Quaternion.Angle(objectRotation, lookRot);
				if (angle < 5f)
	            {
		            maxDelta *= 0.2f;
	            }*/

                Quaternion oldRot = objectRotation;
                this.objectRotation = Quaternion.RotateTowards( this.objectRotation, lookRot, maxDelta );
                if ( this.objectRotation.x == float.NaN ) //undo if NaN result
                    this.objectRotation = oldRot;
            }
        }
        #endregion

        #region ConvertEnemyRobotToPlayerForces
        public void ConvertEnemyRobotToPlayerForces()
        {
            if ( this.UnitType == null )
                return;
            if ( this.GetIsPartOfPlayerForcesInAnyWay() )
                return;

            if ( this.UnitType.UnlockGrantedOnConversionHack != null )
                this.UnitType.UnlockGrantedOnConversionHack.DuringGameplay_ReadyForInventionIfNeedBe( CommonRefs.WorldExperienceInspiration, true );

            if ( this.UnitType.IsMechStyleMovement )
            {
                Vector3 startLocation = this.GetPositionForCollisions();
                Vector3 endLocation = startLocation.PlusY( this.GetHalfHeightForCollisions() + 0.2f );
                ArcenDoubleCharacterBuffer buffer = DamageTextPopups.GetTextBufferAppropriateForThreadOrNull();
                if ( buffer != null )
                {
                    buffer.AddLang( "RobotConverted", IconRefs.RobotConverted.DefaultColorHexWithHDRHex );
                    DamageTextPopups.FloatingDamageTextPopup newDamageText = DamageTextPopups.CreateNewFromTextBuffer( buffer,
                        startLocation, endLocation, 0.8f, MathA.Max( 2, GameSettings.Current.GetInt( "DamagePopup_LingerTime" ) ) );
                    newDamageText.PhysicalDamageIncluded = 0;
                    newDamageText.MoraleDamageIncluded = 0;
                    newDamageText.SquadDeathsIncluded = 0;
                }

                this.Stance = CommonRefs.Player_DeterAndDefend;
                this.FromCohort = CohortRefs.ConvertedTroops;
                this.IsManagedUnit = null;
                this.IsCityConflictUnit = null;
                this.ManagerStartLocation = new WrapperedSimBuilding();
                this.ManagerOriginalMachineActorFocusID = -1;

                CityStatisticTable.AlterScore( "MechsYouConverted", 1 );
            }
            else
            {
                Vector3 startLocation = this.GetPositionForCollisions();
                Vector3 endLocation = startLocation.PlusY( this.GetHalfHeightForCollisions() + 0.2f );
                ArcenDoubleCharacterBuffer buffer = DamageTextPopups.GetTextBufferAppropriateForThreadOrNull();
                if ( buffer != null )
                {
                    buffer.AddLang( "RobotConverted", IconRefs.RobotConverted.DefaultColorHexWithHDRHex );
                    DamageTextPopups.FloatingDamageTextPopup newDamageText = DamageTextPopups.CreateNewFromTextBuffer( buffer,
                        startLocation, endLocation, 0.8f, MathA.Max( 2, GameSettings.Current.GetInt( "DamagePopup_LingerTime" ) ) );
                    newDamageText.PhysicalDamageIncluded = 0;
                    newDamageText.MoraleDamageIncluded = 0;
                    newDamageText.SquadDeathsIncluded = 0;
                }

                this.Stance = CommonRefs.Player_DeterAndDefend;
                this.FromCohort = CohortRefs.ConvertedTroops;
                this.IsManagedUnit = null;
                this.IsCityConflictUnit = null;
                this.ManagerStartLocation = new WrapperedSimBuilding();
                this.ManagerOriginalMachineActorFocusID = -1;

                CityStatisticTable.AlterScore( "RobotsYouConverted", this.CurrentSquadSize );

                CityStatisticTable.AlterScore( "RobotSquadsYouConverted", 1 );
            }
        }
        #endregion

        #region TryCreateNewNPCUnitAsCloseAsPossibleToThisOne
        public ISimNPCUnit TryCreateNewNPCUnitAsCloseAsPossibleToThisOne( NPCUnitType UnitTypeToGrant, NPCCohort FromCohort, NPCUnitStance Stance, 
            float SquadSizeMultiplier, Vector3 RotateToFaceThisPoint, float RotationYIfZeroPlus, bool AllowMechWiggle, int MaxDesiredClearance, 
            CellRange CRange, MersenneTwister Rand, CollisionRule Rule, string CreationReason )
        {
            if ( UnitTypeToGrant == null )
                return null;

            Vector3 unitSpot;
            MapCell cell;
            ISimUnitLocation loc = this.ContainerLocation.Get();
            if ( loc != null )
            {
                cell = loc.GetLocationMapCell();
                unitSpot = loc.GetEffectiveWorldLocationForContainedUnit().ReplaceY( 0 );
            }
            else
            {
                cell = CityMap.TryGetWorldCellAtCoordinates( this.GroundLocation );
                unitSpot = this.GroundLocation.ReplaceY( 0 );
            }
            return World.Forces.TryCreateNewNPCUnitAsCloseAsPossibleToLocation( unitSpot, cell, UnitTypeToGrant, FromCohort,
                Stance, SquadSizeMultiplier, RotateToFaceThisPoint, RotationYIfZeroPlus, AllowMechWiggle, MaxDesiredClearance, CRange, Rand, Rule, CreationReason );
        }
        #endregion

        #region TryCreateNewNPCUnitWithinThisRadius
        public ISimNPCUnit TryCreateNewNPCUnitWithinThisRadius( NPCUnitType UnitTypeToGrant, NPCCohort FromCohort, NPCUnitStance Stance,
            float SquadSizeMultiplier, Vector3 RotateToFaceThisPoint, float RotationYIfZeroPlus, bool AllowMechWiggle, float Radius, 
            int MaxDesiredClearance, CellRange FailoverCRange, MersenneTwister Rand, CollisionRule Rule, string CreationReason )
        {
            if ( UnitTypeToGrant == null )
                return null;

            Vector3 unitSpot;
            MapCell cell;
            ISimUnitLocation loc = this.ContainerLocation.Get();
            if ( loc != null )
            {
                cell = loc.GetLocationMapCell();
                unitSpot = loc.GetEffectiveWorldLocationForContainedUnit().ReplaceY( 0 );
            }
            else
            {
                cell = CityMap.TryGetWorldCellAtCoordinates( this.GroundLocation );
                unitSpot = this.GroundLocation.ReplaceY( 0 );
            }
            return World.Forces.TryCreateNewNPCUnitWithinThisRadius( unitSpot, cell, UnitTypeToGrant, FromCohort,
                Stance, SquadSizeMultiplier, RotateToFaceThisPoint, RotationYIfZeroPlus, AllowMechWiggle, Radius, MaxDesiredClearance, FailoverCRange, Rand, Rule, CreationReason );
        }
        #endregion

        #region TryCreateNewNPCUnitAsCloseAsPossibleToBuilding
        public ISimNPCUnit TryCreateNewNPCUnitAsCloseAsPossibleToBuilding( ISimBuilding BuildingToBeCloseTo, NPCUnitType UnitTypeToGrant, 
            NPCCohort FromCohort, NPCUnitStance Stance, float SquadSizeMultiplier, Vector3 RotateToFaceThisPoint, 
            float RotationYIfZeroPlus, bool AllowMechWiggle, int MaxDesiredClearance, CellRange CRange, MersenneTwister Rand, CollisionRule Rule, string CreationReason )
        {
            if ( UnitTypeToGrant == null )
                return null;
            MapItem buildingItem = BuildingToBeCloseTo?.GetMapItem();
            MapCell cell = buildingItem?.ParentCell;
            if ( buildingItem == null || cell == null )
                return TryCreateNewNPCUnitAsCloseAsPossibleToThisOne( UnitTypeToGrant, FromCohort,
                    Stance, SquadSizeMultiplier, RotateToFaceThisPoint, RotationYIfZeroPlus, AllowMechWiggle, MaxDesiredClearance, CRange, Rand, Rule, CreationReason );

            Vector3 unitSpot = buildingItem.GroundCenterPoint;
            return World.Forces.TryCreateNewNPCUnitAsCloseAsPossibleToLocation( unitSpot, cell, UnitTypeToGrant, FromCohort,
                Stance, SquadSizeMultiplier, RotateToFaceThisPoint, RotationYIfZeroPlus, AllowMechWiggle, MaxDesiredClearance, CRange, Rand, Rule, CreationReason );
        }
        #endregion

        #region Pooling
        private static int LastGlobalUnitIndex = 1;
        public readonly int GlobalUnitIndex;

        private static ReferenceTracker RefTracker;
        private NPCUnit()
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "NPCUnits" );
            RefTracker.IncrementObjectCount();
            GlobalUnitIndex = Interlocked.Increment( ref LastGlobalUnitIndex );
        }

        private static readonly TimeBasedPool<NPCUnit> Pool = TimeBasedPool<NPCUnit>.Create_WillNeverBeGCed( "NPCUnit", 3, 20,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOn_EvenPartial_GameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new NPCUnit(); } );

        public static NPCUnit GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void DoAnyBelatedCleanupWhenComingOutOfPool_ShouldBeVeryLittleNeeded()
        {
        }

        public override void DoMidCleanupWhenLeavingQuarantineBackIntoMainPool_ClearAsMuchAsPossibleIncludingOutgoingReferences()
        {
            this.ClearData(); //this is the main one
        }

        public override void DoEarlyCleanupWhenGoingIntoQuarantine_ClearIncomingPointersButNotOutgoingReferences()
        {
            this.ContainerLocation.Get()?.ClearOccupyingUnitIfThisOne( this );
            this.ContainerLocation = new WrapperedSimUnitLocation( null );

            SimCommon.AllActorsByID.TryRemove( this.UnitID, 5 );
            World_Forces.AllNPCUnitsByID.TryRemove( this.UnitID, 5 );
            World_Forces.ManagedNPCUnitsByID.TryRemove( this.UnitID, 5 );
        }

        public void DoBeforeRemoveOrClear()
        {
            this.ReturnToPool(); //when I am remove from a list, put me back in the pool
        }
        public void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }
        #endregion

        #region EqualsRelated
        public bool EqualsRelated( IAutoRelatedObject Other )
        {
            if ( Other is NPCUnit otherUnit )
                return this.GlobalUnitIndex == otherUnit.GlobalUnitIndex;
            return false;
        }
        #endregion

        public Vector3 GetPositionForCameraFocus()
        {
            Vector3 loc = this.drawLocation;
            if ( this.isMoving )
                loc = this.desiredNewContainerLocation != null ? this.desiredNewContainerLocation.GetEffectiveWorldLocationForContainedUnit() : this.desiredNewGroundLocation;

            if ( (this.UnitType?.EntireObjectAlwaysThisHeightAboveGround??0) > 0 )
                loc = loc.ReplaceY( this.UnitType?.EntireObjectAlwaysThisHeightAboveGround??0 );
            loc = loc.PlusY( (Engine_HotM.SelectedActor == this ? (this.UnitType?.ExtraOffsetForCameraFocusWhenSelected ?? 0) : 0) );

            return loc;
        }

        public override Vector3 GetEmissionLocation()
        {
            Vector3 loc = this.drawLocation;
            if ( (this.UnitType?.EntireObjectAlwaysThisHeightAboveGround ?? 0) > 0 )
                loc = loc.ReplaceY( this.UnitType?.EntireObjectAlwaysThisHeightAboveGround ?? 0 );
            return loc;
        }

        public Vector3 GetActualPositionForMovementOrPlacement()
        {
            return this.ContainerLocation.Get()?.GetEffectiveWorldLocationForContainedUnit() ?? this.GroundLocation;
        }

        public float GetMovementRange()
        {
            float range = this.ActorData[ActorRefs.ActorMoveRange]?.Current ?? 0;

            return range;
        }

        public float GetMovementRangeSquared()
        {
            float range = this.ActorData[ActorRefs.ActorMoveRange]?.Current ?? 0;
            return range * range;
        }

        public float GetNPCRevealRange()
        {
            float range = this.ActorData[ActorRefs.AttackRange]?.Current ?? 0;
            if ( range < 5 )
                range = 5;
            range += range; //for now, npc units just give a flat double of their attack range

            if ( range > SimCommon.MaxNPCAttackRange )
                range = SimCommon.MaxNPCAttackRange;

            return range;
        }

        public float GetNPCRevealRangeSquared()
        {
            float range = this.ActorData[ActorRefs.AttackRange]?.Current ?? 0;
            if ( range < 5 )
                range = 5;
            range += range; //for now, npc units just give a flat double of their attack range

            if ( range > SimCommon.MaxNPCAttackRange )
                return SimCommon.MaxNPCAttackRangeSquared;

            return range * range;
        }

        public ArcenDynamicTableRow GetTypeAsRow()
        {
            return this.UnitType;
        }

        public IA5Sprite GetShapeIcon()
        {
            return this.UnitType?.ShapeIcon;
        }

        public string GetShapeIconColorString()
        {
            return this.UnitType?.ShapeIconColorHex??string.Empty;
        }

        public string GetDisplayName()
        {
            return this.UnitType?.GetDisplayName()??"[null unit type]";
        }

        public string GetTypeName()
        {
            return this.UnitType?.GetDisplayName() ??"[null unit type]";
        }

        public Vector3 GetBottomCenterPosition()
        {
            Vector3 loc = this.drawLocation;
            if ( (this.UnitType?.EntireObjectAlwaysThisHeightAboveGround ?? 0) > 0 )
                loc = loc.ReplaceY( this.UnitType?.EntireObjectAlwaysThisHeightAboveGround ?? 0 );
            return loc;
        }

        public Vector3 GetPositionForCollisions()
        {
            Vector3 loc = this.drawLocation;
            if ( (this.UnitType?.EntireObjectAlwaysThisHeightAboveGround ?? 0) > 0 )
                loc = loc.ReplaceY( this.UnitType?.EntireObjectAlwaysThisHeightAboveGround ?? 0 );
            loc = loc.PlusY( this.UnitType?.HalfHeightForCollisions ?? 0 ).PlusY( this.UnitType?.YOffsetForCollisionBase ?? 0 );
            return loc;
        }

        public Vector3 GetCollisionCenter()
        {
            return this.GetPositionForCollisions();
        }

        public Vector3 GetPositionForCollisionsFromTheoretic( Vector3 TheoreticalPoint )
        {
            Vector3 loc = TheoreticalPoint;
            if ( (this.UnitType?.EntireObjectAlwaysThisHeightAboveGround ?? 0) > 0 )
                loc = loc.ReplaceY( this.UnitType?.EntireObjectAlwaysThisHeightAboveGround ?? 0 );
            loc = loc.PlusY( this.UnitType?.HalfHeightForCollisions ?? 0 ).PlusY( this.UnitType?.YOffsetForCollisionBase ?? 0 );
            return loc;
        }

        public bool GetEquals( ICollidable other )
        {
            if ( other is NPCUnit unit )
                return unit.UnitID == this.UnitID;
            return false;
        }

        public string GetCollidableTypeID()
        {
            return this.UnitType?.ID ?? "[null npc unit]";
        }

        public int GetCollidableSmashesBuildingStrength()
        {
            return this.UnitType?.DestroyIntersectingBuildingsStrength ?? 0;
        }

        public float GetRadiusForCollisions()
        {
            return this.UnitType?.RadiusForCollisions??0;
        }

        public float GetSquaredRadiusForCollisions()
        {
            return this.UnitType?.RadiusSquaredForCollisions??0;
        }

        public float GetHalfHeightForCollisions()
        {
            return this.UnitType?.HalfHeightForCollisions??0;
        }

        public bool GetShouldHideIntersectingDecorations()
        {
            return this.UnitType?.ShouldHideIntersectingDecorations??false;
        }

        public bool GetIsCurrentlyInvisible( InvisibilityPurpose ForPurpose )
        {
            if ( ForPurpose == InvisibilityPurpose.ForNPCTargeting )
                return false; //npc targeting doesn't care about this at all

            //all of the purposes treat this the same, except for the NPC one!

            if ( this.IsNPCInFogOfWar )
            {
                if ( (this.Stance?.CausesUnitToAlwaysBeVisibleAsIfOutOfFogOfWar ?? false) )
                {
                    this.IsNPCInFogOfWar = false;
                    return false;
                }

                NPCCohort group = this.FromCohort;
                if ( group == null || !group.IsPlayerControlled )
                    return true; //if not held by a player-controlled group, and also not in interaction range, then don't show us!
            }
            return false;
        }

        private bool GetIsCurrentlyInSilhouetteMode()
        {
            if ( !(this.UnitType?.IsNotableEnoughToShowAsSilhouetteWhenStanceSuggests??false) )
                return false; //if not notable enough
            return this.Stance?.CausesUnitToShowAsSilhouetteWhenInFogOfWarIfNotableEnough??false;
        }

        public List<SubCollidable> GetSubCollidablesOrNull()
        {
            return this.UnitType.SubCollidables;
        }

        public float GetRotationYForCollisions()
        {
            return this.objectRotation.eulerAngles.y;
        }

        public float GetExtraRadiusBufferWhenTestingForNew()
        {
            return this.UnitType.ExtraRadiusBufferWhenTestingForNew;
        }

        public void SetRotationY( float YRot )
        {
            this.objectRotation = Quaternion.Euler( 0, YRot, 0 );
            this.hasEverSetRotation = true;
        }

        public bool ShouldFreezeCameraAtTheMoment => false;//this.isMoving;

        public void DoOnSelectByPlayer()
        {
            if ( this.GetHasAnyActionItWantsTodo() ) //don't bother checking about blocked movement
                this.StartNPCAction( workingRandomMainThreadOnlyShouldBeReset ); //we are indeed on the main thread
        }

        public void DoOnFocus_Streets()
        {
        }

        public void DoOnFocus_CityMap()
        {
        }

        public int SortPriority
        {
            get
            {
                return 500; //npc units still come after vehicles and machine units
            }
        }
        public int SortID => this.UnitID;

        public override int GetHashCode()
        {
            int hash = this.UnitID.GetHashCode();
            hash = HashCodeHelper.CombineHashCodes( hash, 500.GetHashCode() );
            return hash;
        }

        public override bool Equals( object obj )
        {
            if ( obj is NPCUnit unit )
                return unit.UnitID == this.UnitID;
            else
                return false;
        }

        public ListView<ISimBuilding> GetBoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob()
        {
            return ListView<ISimBuilding>.Create( this.BoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob.GetDisplayList() );
        }

        public IA5Sprite GetTooltipIcon()
        {
            if ( this.VisLODObject != null )
                return this.VisLODObject?.TooltipIcon;
            else if ( this.VisSimpleObject != null )
                return this.VisSimpleObject?.TooltipIcon;
            return null;
        }

        public VisColorUsage GetTooltipIconColorStyle()
        {
            VisColorUsage result = null;
            if ( this.VisLODObject != null )
                result = this.VisLODObject?.GetTooltipIconColorStyle();
            else if ( this.VisSimpleObject != null )
                result = this.VisSimpleObject?.GetTooltipIconColorStyle();

            if ( result == null )
                result = ColorRefs.DefaultTooltipIconColorStyle;

            return result;
        }

        #region RenderTooltip
        //BOOKMARK
        public override void RenderTooltip( IArcenUIElementForSizing DrawNextTo, SideClamp Clamp, TooltipShadowStyle ShadowStyle, bool ShouldBeAtMouseCursor,
            ActorTooltipExtraData ExtraData, TooltipExtraRules ExtraRules )
        {
            NPCUnitType unitType = this.UnitType;
            if ( unitType == null )
                return;
            NPCUnitStance stance = this.Stance;
            if ( stance == null )
                return;

            if ( this.GetIsCurrentlyInvisible( InvisibilityPurpose.ForPlayerTargeting ) ) //this handles cases like them being in silhouette mode
                return;

            UnitStyleNovelTooltipBuffer usn = UnitStyleNovelTooltipBuffer.Instance;

            if ( usn.TryStartUnitStyleTooltip( ShouldBeAtMouseCursor || DrawNextTo != null,
                TooltipID.Create( this ), DrawNextTo, Clamp, ShadowStyle, TooltipExtraText.None, ExtraRules ) )
            {
                bool hasAnythingHyperDetailed = false;
                bool showNormalDetailed = InputCaching.ShouldShowDetailedTooltips;
                bool showHyperDetailed = InputCaching.ShouldShowHyperDetailedTooltips;
                bool showAnyDetailed = showNormalDetailed || showHyperDetailed;
                if ( showHyperDetailed )
                    showNormalDetailed = false;

                unitType.NonSim_TaskHoveredBool = true;

                usn.Title.AddSpriteStyled_NoIndent( unitType.ShapeIcon, AdjustedSpriteStyle.InlineLarger1_2 ).Space1x();
                usn.Title.AddRaw( unitType.GetDisplayName() );
                usn.PortraitIcon = this.GetTooltipIcon();
                usn.PortraitFrameColorHex = this.GetTooltipIconColorStyle()?.RelatedBorderColorHex ?? string.Empty;

                if ( !unitType.GetDescription().IsEmpty() )
                    usn.Main.AddRaw( unitType.GetDescription(), ColorTheme.NarrativeColor ).Line();

                if ( !unitType.LangKeyForStrategyTip.IsEmpty() )
                    usn.Main.AddLang( unitType.LangKeyForStrategyTip, ColorTheme.NarrativeColor ).Line();

                if ( this.DisbandAtTheStartOfNextTurn )
                    usn.Main.AddLang( "UnitWillDisbandBeforeNextTurn", ColorTheme.RedOrange2 ).Line();
                if ( this.DisbandAsSoonAsNotSelected )
                    usn.Main.AddLang( "UnitWillDisbandWhenUnselected", ColorTheme.RedOrange2 ).Line();

                
                bool isPlayerBulk = unitType.CostsToCreateIfBulkAndroid.Count > 0;

                NPCUnitObjective objective = this.CurrentObjective;
                NPCCohort fromCohort = this.FromCohort;
                if ( fromCohort != null )
                {
                    fromCohort.DuringGame_DiscoverIfNeed();

                    if ( fromCohort.IsPlayerControlled )
                    {
                        if ( objective != null )
                        {
                            usn.UpperStats.StartUppercase().AddRaw( objective.GetDisplayName(), ColorTheme.DataBlue ).EndUppercase();
                            usn.UpperStats.Line();
                        }
                        else
                        {
                            usn.UpperStats.StartUppercase().AddRaw( stance.GetDisplayName(), ColorTheme.DataBlue ).EndUppercase();
                            usn.UpperStats.Line();
                        }

                        usn.UpperStats.AddRaw( fromCohort.GetDisplayName(), ColorTheme.Gray ).EndUppercase().Line();
                    }
                    else
                    {
                        usn.UpperStats.StartSize90().StartUppercase().AddRaw( fromCohort.GetDisplayName(), ColorTheme.DataBlue ).EndUppercase().Line();
                        usn.UpperStats.AddRaw( fromCohort.PartOfGroup.GetDisplayName(), ColorTheme.Gray ).Line();
                    }
                }

                usn.LowerStats.AddLangAndAfterLineItemHeader( unitType.IsVehicle || unitType.IsMechStyleMovement ? "CrewOf_Prefix" : "SquadSize_Prefix" );
                if ( isPlayerBulk && unitType.BasicSquadSize > 1 && this.CurrentSquadSize < unitType.BasicSquadSize )
                    usn.LowerStats.AddFormat2( "OutOF", this.CurrentSquadSize, unitType.BasicSquadSize ).Line();
                else
                    usn.LowerStats.AddRaw( this.CurrentSquadSize.ToString(), ColorTheme.DataBlue ).Line();

                if ( !( isPlayerBulk || (fromCohort?.IsPlayerControlled??false)) || showAnyDetailed )
                {
                    if ( objective != null )
                    {
                        ISimBuilding objectiveBuilding = this.ObjectiveBuilding.Get();
                        ISimMapActor objectiveActor = this.ObjectiveActor;

                        usn.FrameAIcon = objective.Icon;

                        usn.FrameATitle.AddRaw( objective.GetDisplayName() );
                        if ( objectiveBuilding != null )
                            usn.FrameABody.AddBoldRawAndAfterLineItemHeader( objectiveBuilding.GetDisplayName() );
                        if ( objectiveActor != null )
                            usn.FrameABody.AddBoldRawAndAfterLineItemHeader( objectiveActor.GetDisplayName() );
                        usn.FrameABody.StartColor( ColorTheme.DataBlue );
                        objective.Implementation.RenderObjectivePercentComplete( this, objective, usn.FrameABody );
                        usn.FrameABody.EndColor().Line();

                        if ( objective.GetDescription().Length > 0 )
                            usn.FrameABody.AddRaw( objective.GetDescription(), ColorTheme.NarrativeColor ).Line();
                        //int lengthBefore = usn.FrameABody.GetLength();
                        objective.Implementation.RenderObjectiveExtraTooltipData( this, objective, usn.FrameABody );
                        //if ( lengthBefore != usn.FrameABody.GetLength() )
                        //    usn.FrameABody.Line();
                    }
                    else
                    {
                        usn.FrameAIcon = stance.Icon;

                        usn.FrameATitle.AddRaw( stance.GetDisplayName() );
                        usn.FrameABody.AddRaw( stance.GetDescription(), ColorTheme.NarrativeColor ).Line();
                        stance.Implementation.HandleLogicForNPCUnitInStance( this, stance, NPCUnitStanceLogic.TooltipAddendum, null, usn.FrameABody, null, null );
                    }
                }

                NPCManagedUnit managedUnit = this.IsManagedUnit;
                NPCManager parentManager = managedUnit?.ParentManager;

                CityConflictUnit conflictUnit = this.IsCityConflictUnit;

                MapPOI homePOI = this.HomePOI;
                MapDistrict homeDistrict = this.HomeDistrict;
                MachineProject relatedToProject = parentManager?.PeriodicData?.GateByCity?.RequiredProjectActive;

                if ( homePOI != null || homeDistrict != null || relatedToProject != null )
                    hasAnythingHyperDetailed = true;
                if ( !showHyperDetailed )
                {
                    homePOI = null;
                    homeDistrict = null;
                    relatedToProject = null;
                }

                NPCDialog dialog = managedUnit?.DialogToShow;
                if ( dialog == null)
                    dialog = conflictUnit?.DialogToShow;

                if ( !showHyperDetailed && dialog != null && dialog.DuringGame_HasHandled )
                    dialog = null;

                bool debug_ShowNPCAggroValues = GameSettings.Current.GetBool( "Debug_ShowNPCAggroValues" );
                bool debug_ShowProjectInnards = GameSettings.Current.GetBool( "Debug_ShowProjectInnards" );
                bool debug_ShowNPCCreationReason = GameSettings.Current.GetBool( "Debug_ShowNPCCreationReason" );

                string debugText = this.DebugText;
                bool hasDebugText = !string.IsNullOrEmpty( debugText );

                int numberStatuses;
                if ( showHyperDetailed )
                {
                    numberStatuses = this.RenderStatusLinesIfAny( usn.Col2Body, true );
                    if ( numberStatuses > 0 )
                        usn.Col2Title.AddLang( "StatusEffects" );
                }
                else
                {
                    usn.FrameCBody.StartStyleLineHeightA();
                    numberStatuses = this.RenderStatusLinesIfAny( usn.FrameCBody, false );
                    //if ( numberStatuses > 0 )
                    //    usn.FrameCTitle.AddLang( "StatusEffects" );
                }

                if ( numberStatuses > 0 )
                    hasAnythingHyperDetailed = true;

                bool hasExtraLoreInfoOnUnit = !unitType.ExtraLoreInfoOptional.Text.IsEmpty();
                bool hasExtraLoreInfoOnStance = !stance.ExtraLoreInfoOptional.Text.IsEmpty();

                if ( hasExtraLoreInfoOnUnit || hasExtraLoreInfoOnStance )
                    hasAnythingHyperDetailed = true;
                if ( !showHyperDetailed )
                {
                    hasExtraLoreInfoOnUnit = false;
                    hasExtraLoreInfoOnStance = false;
                }

                bool hasExtraInfoLangKey = !( (managedUnit?.LangKeyForExtraInfo?.IsEmpty() ?? true) && (conflictUnit?.LangKeyForExtraInfo?.IsEmpty() ?? true) );
                if ( hasExtraInfoLangKey || dialog != null || homePOI != null || homeDistrict != null || relatedToProject != null ||
                    /*debug_ShowNPCAggroValues || debug_ShowProjectInnards ||*/ debug_ShowNPCCreationReason || hasDebugText ||
                    hasExtraLoreInfoOnUnit || hasExtraLoreInfoOnStance )
                {
                    ArcenDoubleCharacterBuffer titleToUse = usn.FrameBTitle;
                    ArcenDoubleCharacterBuffer bodyToUse = usn.FrameBBody;

                    if ( showHyperDetailed )
                    {
                        if ( numberStatuses > 0 )
                        {
                            titleToUse = usn.Col3Title;
                            bodyToUse = usn.Col3Body;
                        }
                        else
                        {
                            titleToUse = usn.Col2Title;
                            bodyToUse = usn.Col2Body;
                        }
                    }

                    if ( showHyperDetailed )
                        titleToUse.AddLang( "ExtraInfo" );
                    bodyToUse.StartColor( ColorTheme.NarrativeColor );
                    if ( hasExtraInfoLangKey )
                    {
                        if ( managedUnit != null && !managedUnit.LangKeyForExtraInfo.IsEmpty() )
                            bodyToUse.AddLang( managedUnit.LangKeyForExtraInfo, ColorTheme.NarrativeColor ).Line();
                        if ( conflictUnit != null && !conflictUnit.LangKeyForExtraInfo.IsEmpty() )
                            bodyToUse.AddLang( conflictUnit.LangKeyForExtraInfo, ColorTheme.NarrativeColor ).Line();
                    }

                    if ( hasExtraLoreInfoOnUnit )
                        bodyToUse.AddSpriteStyled_NoIndent( unitType.ShapeIcon, AdjustedSpriteStyle.InlineLarger1_2 ).Space1x()
                            .AddRaw( unitType.ExtraLoreInfoOptional.Text, ColorTheme.NarrativeColor ).Line();

                    if ( hasExtraLoreInfoOnStance )
                        bodyToUse.AddSpriteStyled_NoIndent( stance.Icon, AdjustedSpriteStyle.InlineLarger1_2 ).Space1x()
                            .AddRaw( stance.ExtraLoreInfoOptional.Text, ColorTheme.NarrativeColor ).Line();

                    if ( dialog != null )
                    {
                        bodyToUse.AddBoldLangAndAfterLineItemHeader( "DiscussionTopic", ColorTheme.DataLabelWhite );
                        if ( !dialog.DuringGame_HasHandled )
                            bodyToUse.AddRaw( dialog.GetDisplayName(), ColorTheme.DataBlue ).Line();
                        else
                            bodyToUse.AddLang( "DiscussionTopic_NothingMore", ColorTheme.Gray ).Line();
                    }

                    if ( homePOI != null )
                    {
                        bodyToUse.AddBoldLangAndAfterLineItemHeader( "HomePOI", ColorTheme.DataLabelWhite );
                        bodyToUse.AddRaw( homePOI.GetDisplayName(), ColorTheme.DataBlue ).Line();
                    }

                    if ( homeDistrict != null )
                    {
                        bodyToUse.AddBoldLangAndAfterLineItemHeader( "HomeDistrict", ColorTheme.DataLabelWhite );
                        bodyToUse.AddRaw( homeDistrict.GetDisplayName(), ColorTheme.DataBlue ).Line();
                    }

                    if ( relatedToProject != null )
                    {
                        bodyToUse.AddBoldLangAndAfterLineItemHeader( "RelatedToProject", ColorTheme.DataLabelWhite );
                        bodyToUse.AddRaw( relatedToProject.GetDisplayName(), ColorTheme.DataBlue ).Line();
                    }

                    #region Debug Section
                    if ( debug_ShowNPCAggroValues && showHyperDetailed )
                    {
                        bodyToUse.StartBold().AddNeverTranslatedAndAfterLineItemHeader( "Aggro", ColorTheme.DataLabelWhite, true ).EndBold().StartSize80();
                        if ( !this.GetHasAggroedAnyNPCCohort() )
                            bodyToUse.AddRaw( LangCommon.None.Text, ColorTheme.HealingGreen );
                        else
                        {
                            bool isFirst = true;
                            foreach ( NPCCohort cohort in NPCCohortTable.Instance.Rows )
                            {
                                int aggroAmount = this.GetAmountHasAggroedNPCCohort( cohort );
                                if ( aggroAmount > 0 )
                                {
                                    if ( isFirst )
                                        isFirst = false;
                                    else
                                        bodyToUse.AddLang( LangCommon.ListSeparator );

                                    bodyToUse.AddRawAndAfterLineItemHeader( cohort.GetDisplayName(), ColorTheme.AngeredGroupRed ).AddRaw( aggroAmount.ToStringWholeBasic() );
                                }
                            }
                            if ( isFirst ) //guess there were none after all
                                bodyToUse.AddRaw( LangCommon.None.Text );
                        }
                        bodyToUse.EndSize().Line();
                    }

                    if ( hasDebugText )
                        bodyToUse.StartSize80().AddNeverTranslatedAndAfterLineItemHeader( "DebugText", ColorTheme.AngRed_Dimmed, true ).AddRaw( debugText ).EndSize().Line();

                    if ( debug_ShowNPCCreationReason )
                        bodyToUse.StartBold().AddNeverTranslatedAndAfterLineItemHeader( "Creation Reason:", ColorTheme.DataLabelWhite, true ).EndBold()
                            .AddRaw( this.CreationReason ).Line();

                    if ( debug_ShowProjectInnards )
                    {
                        bodyToUse.StartBold().AddNeverTranslatedAndAfterLineItemHeader( "Accumulators", ColorTheme.DataLabelWhite, true ).EndBold();
                        bool isFirst = true;
                        foreach ( KeyValuePair<NPCUnitAccumulator, int> kv in this.Accumulators )
                        {
                            if ( kv.Value == 0 )
                                continue;
                            if ( isFirst )
                                isFirst = false;
                            else
                                bodyToUse.AddLang( LangCommon.ListSeparator );
                            bodyToUse.AddRawAndAfterLineItemHeader( kv.Key.ID ).AddNeverTranslated( kv.Value.ToStringThousandsWhole(), true );
                        }
                        bodyToUse.Line();
                    }
                    #endregion debug Section
                }

                this.RenderDataLinesIntoNovelList( null, null, showAnyDetailed );

                int dialogCanBeForcedInto = (managedUnit?.GetDialogCountCanBeForcedInto()??0) + (conflictUnit?.GetDialogCountCanBeForcedInto() ?? 0);
                if ( dialogCanBeForcedInto > 0 )
                {
                    UnitStyleNovelTooltipBuffer novel = UnitStyleNovelTooltipBuffer.Instance;
                    novel.StartFreshStat();
                    novel.StatWorkingRight.AddRaw( dialogCanBeForcedInto.ToString(), IconRefs.DialogCanForceIcon.DefaultColorHex );
                    novel.StatWorkingLeft.AddLang( "ConversationsCanForce", IconRefs.DialogCanForceIcon.DefaultColorHex );
                    
                    novel.FinishStatDataLine( IconRefs.DialogCanForceIcon.Icon, IconRefs.DialogCanForceIcon.DefaultColorHex );
                }

                ArcenDoubleCharacterBuffer lowerEffective = showHyperDetailed ? usn.FrameBBody : usn.LowerNarrative;

                lowerEffective.StartStyleLineHeightA();
                if ( !this.GetIsPartOfPlayerForcesInAnyWay() )
                    unitType.RenderResourcesRecovered( lowerEffective, showAnyDetailed );

                CityConflictUnit isFromConflict = this.IsCityConflictUnit;
                if ( isFromConflict != null && isFromConflict.ParentConflict.DuringGameplay_State == CityConflictState.Active )
                {
                    lowerEffective.AddBoldLangAndAfterLineItemHeader( "CityConflict_ConflictValue", ColorTheme.DataLabelWhite );
                    lowerEffective.AddRaw( isFromConflict.PointsToOpposingSideOnDeath.ToStringThousandsWhole(), ColorTheme.DataBlue );
                    lowerEffective.Line();
                }

                if ( this.IncomingDamage.Display.IncomingPhysicalDamageTargeting > 0 || this.IncomingDamage.Display.IncomingMoraleDamageTargeting > 0 )
                {
                    hasAnythingHyperDetailed = true;
                    this.AppendIncomingDamageLines( lowerEffective, showHyperDetailed );
                }

                switch ( ExtraData )
                {
                    case ActorTooltipExtraData.SelectFocus:
                        lowerEffective.StartStyleLineHeightA().StartSize90();
                        lowerEffective.AddFormat2( "ClickToSelect", Lang.GetLeftClickText(), this.GetDisplayName(), ColorTheme.SoftGold ).Line();
                        lowerEffective.AddFormat2( "ClickToFocusOn", Lang.GetRightClickText(), this.GetDisplayName(), ColorTheme.SoftGold ).Line();
                        break;
                    case ActorTooltipExtraData.FocusDestroy:
                        lowerEffective.StartSize80().StartColor( ColorTheme.SoftGold );
                        lowerEffective.AddFormat2( "ClickToFocusOn", Lang.GetRightClickText(), this.GetDisplayName() ).Line();
                        lowerEffective.AddFormat2( "ClickToDestroyUnit", Lang.GetLeftClickText(), this.GetDisplayName() );
                        break;
                }

                lowerEffective.EndLineHeight();

                if ( hasAnythingHyperDetailed )
                    usn.CanExpand = HyperCanExpandType.Hyper;
                else
                {
                    if ( usn.CanExpand == HyperCanExpandType.No )
                        usn.CanExpand = HyperCanExpandType.NormalOnly;
                }
            }
        }
        #endregion

        #region WriteChangePercentEntry
        private bool WriteChangePercentEntry( bool AddSpacerAtStart, ArcenCharacterBufferBase Buffer, int Change, string ChangeKey )
        {
            if ( Change == 0 )
                return false;

            if ( AddSpacerAtStart )
                Buffer.Space5x().StartNoBr();

            if ( Change >= 0 )
                Buffer.AddFormat1( "PositiveChange", Change.ToStringIntPercent() );
            else
                Buffer.AddRaw( Change.ToStringIntPercent() );

            Buffer.Space1x().AddLang( ChangeKey ).EndNoBr();

            return true;
        }
        #endregion

        #region GetIconInfo
        protected override void GetIconInfo( out IA5Sprite Sprite, out ArcenPoint FrameColRow,
            out bool DrawFrameStyle, out Color Color, out float IconScale, IconLogic Logic )
        {
            if ( Logic == IconLogic.ResourceScavenging)
            {
                NPCUnitType unitType = this.UnitType;
                ResourceType resourceToRender = null;
                if ( unitType.Resource1RecoveredOnExtract != null && unitType.Resource1RecoveredOnExtract.DuringGame_IsUnlocked() )
                    resourceToRender = unitType.Resource1RecoveredOnExtract;
                else if ( unitType.Resource1RecoveredOnDeath != null && unitType.Resource1RecoveredOnDeath.DuringGame_IsUnlocked() )
                    resourceToRender = unitType.Resource1RecoveredOnDeath;
                else if ( unitType.Resource2RecoveredOnExtract != null && unitType.Resource2RecoveredOnExtract.DuringGame_IsUnlocked() )
                    resourceToRender = unitType.Resource2RecoveredOnExtract;
                else if ( unitType.Resource2RecoveredOnDeath != null && unitType.Resource2RecoveredOnDeath.DuringGame_IsUnlocked() )
                    resourceToRender = unitType.Resource2RecoveredOnDeath;
                else if ( unitType.Resource3RecoveredOnExtract != null && unitType.Resource3RecoveredOnExtract.DuringGame_IsUnlocked() )
                    resourceToRender = unitType.Resource3RecoveredOnExtract;
                else if ( unitType.Resource3RecoveredOnDeath != null && unitType.Resource3RecoveredOnDeath.DuringGame_IsUnlocked() )
                    resourceToRender = unitType.Resource3RecoveredOnDeath;

                if ( resourceToRender != null )
                {
                    Sprite = resourceToRender.Icon;
                    FrameColRow = IconRefs.BlankIconBacking;
                    DrawFrameStyle = false;
                    Color = resourceToRender.ScavengeIconUsage.DefaultColorHDR;
                    IconScale = resourceToRender.ScavengeIconUsage.DefaultScale;
                }
                else
                {
                    Sprite = null;
                    FrameColRow = IconRefs.BlankIconBacking;
                    DrawFrameStyle = false;
                    Color = ColorMath.White;
                    IconScale = 1f;
                }
                return;
            }

            NPCUnitObjective objective = this.CurrentObjective;
            if ( objective != null && objective.Icon != null )
            {
                Sprite = objective.Icon;
                FrameColRow = IconRefs.BlankIconBacking;
                DrawFrameStyle = false;
                Color = objective.IconColor;
                IconScale = objective.IconScale;
                return;
            }

            NPCUnitStance stance = this.Stance;
            if ( stance != null && stance.Icon != null )
            {
                Sprite = stance.Icon;
                FrameColRow = IconRefs.BlankIconBacking;
                DrawFrameStyle = false;
                Color = stance.IconColor;
                IconScale = stance.IconScale;
                return;
            }

            Sprite = null;
            FrameColRow = IconRefs.BlankIconBacking;
            DrawFrameStyle = false;
            Color = ColorMath.White;
            IconScale = 1f;
        }
        #endregion

        #region GetCombinedFromChildrenActorDataCurrent
        public int GetCombinedFromChildrenActorDataCurrent( ActorDataType Type )
        {
            return 0; //these never have children!
        }

        public int GetCombinedFromChildrenActorDataCurrent( string TypeName )
        {
            return 0; //these never have children!
        }
        #endregion

        #region CalculateMapCell
        public override MapCell CalculateMapCell()
        {
            MapCell cell;
            ISimUnitLocation loc = this.ContainerLocation.Get();
            if ( loc != null )
                cell = loc.GetLocationMapCell();
            else
                cell = CityMap.TryGetWorldCellAtCoordinates( this.GroundLocation );
            return cell;
        }
        #endregion

        #region CalculateMapDistrict
        public override MapDistrict CalculateMapDistrict()
        {
            MapCell cell = this.CalculateMapCell();
            return cell?.ParentTile?.District;
        }
        #endregion

        #region CalculatePOI
        public override MapPOI CalculatePOI()
        {
            ISimUnitLocation loc = this.ContainerLocation.Get();
            if ( loc != null )
                return loc.CalculateLocationPOI();
            else
            {
                MapCell cell = this.CalculateMapCell();
                return cell?.ParentTile?.GetPOIOfPoint( this.drawLocation );
            }
        }
        #endregion

        public bool GetMustStayOnGround()
        {
            return this.UnitType.IsMechStyleMovement;
        }

        public int GetPercentRobotic()
        {
            return this.UnitType.PercentRobotic;
        }

        public int GetPercentBiological()
        {
            return this.UnitType.PercentBiological;
        }

        public void PlayBulletHitEffectAtPositionForCollisions()
        {
            this.UnitType?.OnBulletHit?.DuringGame_PlayAtLocation( this.GetPositionForCollisions() );
        }

        public bool GetIsHuman()
        {
            return this.UnitType.IsHuman;
        }

        #region ShotEmissionPoint
        public Vector3 GetTransformedShotEmissionPoint( ShotEmissionPoint Point )
        {
            return Point.CalculatePointFrom( this.GetPositionForCollisions(), this.GetRotationYForCollisions() );
        }
        public ProtectedList<ShotEmissionGroup> GetShotEmissionGroups()
        {
            return this.UnitType?.ShotEmissionGroups;
        }
        #endregion

        public bool GetShouldBeTargetForFriendlyAOEAbilities()
        {
            return this.FromCohort?.GetShouldBeTargetForFriendlyAOEAbilities()??false;
        }

        public bool GetWillFireOnMachineUnitsBaseline()
        {
            return this.FromCohort?.GetWillFireOnMachineUnits()??false;
        }

        public bool GetIsConsideredHostileToPlayer()
        {
            if ( this.FromCohort?.GetWillFireOnMachineUnits() ?? false )
            {
                NPCUnitStance stance = this.Stance;
                if ( stance == null || stance.IsConsideredPassiveGuard || stance.IsStillConsideredPassiveGuardToPlayerForces )
                    return false; //passive guards don't show up this way

                if ( stance.WillHoldFireIfAtFullHealth && this.GetActorDataLostFromMax( ActorRefs.ActorHP, true ) <= 0 )
                    return false; //if won't attack while at full health, and is at full health, then not hostile

                NPCManagedUnit managed = this.IsManagedUnit;
                if ( managed != null )
                {
                    if ( managed.WillNotAttackPlayerIfCityFlagFalse != null && !managed.WillNotAttackPlayerIfCityFlagFalse.DuringGameplay_IsTripped )
                        return false; //they are just watching for now

                    if ( managed.WillNotAttackAnyoneIfCityFlagFalse != null && !managed.WillNotAttackAnyoneIfCityFlagFalse.DuringGameplay_IsTripped )
                        return false; //they are just watching for now
                }

                CityConflictUnit conflict = this.IsCityConflictUnit;
                if ( conflict != null )
                {
                    if ( conflict.WillNotAttackPlayerIfCityFlagFalse != null && !conflict.WillNotAttackPlayerIfCityFlagFalse.DuringGameplay_IsTripped )
                        return false; //they are just watching for now

                    if ( conflict.WillNotAttackAnyoneIfCityFlagFalse != null && !conflict.WillNotAttackAnyoneIfCityFlagFalse.DuringGameplay_IsTripped )
                        return false; //they are just watching for now
                }

                return true; //if we got here, they are willing to fire
            }
            else
                return false;
        }

        public override bool GetIsPlayerControlled()
        {
            return this.FromCohort?.IsPlayerControlled??false;
        }

        public override bool GetIsPartOfPlayerForcesInAnyWay()
        {
            if ( this.FromCohort?.IsPlayerControlled??false )
                return true;
            if ( this.FromCohort?.IsConsideredPartOfPlayerForces??false )
                return true;
            return false;
        }

        public override bool GetIsAnAllyFromThePlayerPerspective()
        {
            if ( this.GetIsPartOfPlayerForcesInAnyWay() )
                return true;
            if ( !(this.FromCohort?.GetWillFireOnMachineUnits() ?? false) )
                return true;
            return false;
        }

        public override bool GetIsRelatedToPlayerShellCompany()
        {
            return this.UnitType?.IsTiedToShellCompany ?? false;
        }

        public override bool GetIsRelatedToPlayerShellCompanyOrCompletelyUnrelatedToPlayer()
        {
            if ( this.UnitType?.IsTiedToShellCompany ?? false )
                return true;
            if ( this.GetIsAnAllyFromThePlayerPerspective() )
                return false;
            return true;
        }

        public bool IsInvalid => this.UnitType == null || this.IsInPoolAtAll;// || this.IsFullDead; allow this to be full dead, that's not a problem.  So the player can see something that disappeared

        public bool GetDoesRevealingForPlayer()
        {
            return this.FromCohort?.IsPlayerControlled??false;
        }

        public bool GetAmIAVeryLowPriorityTargetRightNow()
        {
            return this.UnitType?.DeathsCountAsMurders??false; //this means it's a non-combatant.  Only shoot them if there are no actual combatants
        }

        #region GetWillFireAtOtherNPCUnit
        public bool GetWillFireAtOtherNPCUnit( ISimNPCUnit OtherUnit ) //BOOKMARK
        {
            if ( OtherUnit == null || OtherUnit.ActorID == this.UnitID )
                return false; //will not shoot at self, or blank thing!

            NPCCohort myCohort = this.FromCohort;
            NPCCohort otherCohort = OtherUnit.FromCohort;
            if ( myCohort == null || otherCohort == null ) 
                return false; //one of us is already dying, evidently

            bool weArePlayer = this.GetIsPartOfPlayerForcesInAnyWay();
            bool theyArePlayer = OtherUnit.GetIsPartOfPlayerForcesInAnyWay();
            if ( weArePlayer && theyArePlayer )
                return false; //we are friends!  No hurt friends!

            bool otherIsPlayerAlly = otherCohort.GetIsIsInwardLookingPlayerAlly();
            bool myIsPlayerAlly = myCohort.GetIsIsInwardLookingPlayerAlly();

            if ( weArePlayer && otherIsPlayerAlly )
                return false; //no hurt allies!
            if ( theyArePlayer && myIsPlayerAlly )
                return false; //no hurt allies!
            if ( otherIsPlayerAlly && myIsPlayerAlly )
                return false; //no hurt allies!

            if ( weArePlayer && theyArePlayer )
                return false;

            NPCUnitStance myStance = this.Stance;
            NPCUnitStance otherStance = OtherUnit.Stance;
            if ( myStance == null || otherStance == null )
                return true; //one of us has a busted stance, go ahead and fire

            if ( myStance.IsNeverTargetedByNPCs || otherStance.IsNeverTargetedByNPCs )
                return false; //no firing either way!

            if ( !myStance.IsHyperAggressiveAgainstAllButItsOwnCohort && !otherStance.IsHyperAggressiveAgainstAllButItsOwnCohort )
            {
                if ( myStance.IsAfterShellCompany )
                {
                    if ( !OtherUnit.GetIsRelatedToPlayerShellCompanyOrCompletelyUnrelatedToPlayer() )
                        return false;
                }
                else
                {
                    if ( OtherUnit.GetIsRelatedToPlayerShellCompany() )
                        return false;
                }

                if ( otherStance.IsAfterShellCompany )
                {
                    if ( !this.GetIsRelatedToPlayerShellCompanyOrCompletelyUnrelatedToPlayer() )
                        return false;
                }
                else
                {
                    if ( this.GetIsRelatedToPlayerShellCompany() )
                        return false;
                }
            }

            if ( myStance.WillHoldFireAgainstRebelFriendlyMachine && otherStance.IsConsideredRebelFriendlyMachine )
                return false;
            if ( otherStance.WillHoldFireAgainstRebelFriendlyMachine && myStance.IsConsideredRebelFriendlyMachine )
                return false;

            NPCUnitType otherUnitType = OtherUnit.UnitType;
            if ( otherUnitType != null )
            {
                if ( weArePlayer && ( otherUnitType.DeathsCountAsAttemptedMurders || otherUnitType.DeathsCountAsMurders ) )
                    return false; //we don't auto-murder
            }

            if ( weArePlayer && otherStance.ShouldNeverBeTargetedByPlayerForces )
                return false; //even if they would shoot at us, ignore it

            if ( weArePlayer && otherStance.ShouldBeTargetedByAllPlayerForcesThatAreNotShellCompany )
            {
                if ( !this.GetIsRelatedToPlayerShellCompany() )
                    return true;
            }

            NPCManagedUnit myManaged = this.IsManagedUnit;
            NPCManagedUnit otherManaged = OtherUnit.IsManagedUnit;

            {
                if ( myManaged != null && myManaged.WillNotAttackAnyoneIfCityFlagFalse != null && !myManaged.WillNotAttackAnyoneIfCityFlagFalse.DuringGameplay_IsTripped )
                    return false;
                if ( otherManaged != null && otherManaged.WillNotAttackAnyoneIfCityFlagFalse != null && !otherManaged.WillNotAttackAnyoneIfCityFlagFalse.DuringGameplay_IsTripped )
                    return false;

                if ( myManaged != null && myManaged.WillNotAttackPlayerIfCityFlagFalse != null && !myManaged.WillNotAttackPlayerIfCityFlagFalse.DuringGameplay_IsTripped &&
                    (theyArePlayer || otherIsPlayerAlly) )
                    return false;
                if ( otherManaged != null && otherManaged.WillNotAttackPlayerIfCityFlagFalse != null && !otherManaged.WillNotAttackPlayerIfCityFlagFalse.DuringGameplay_IsTripped &&
                    (weArePlayer || myIsPlayerAlly) )
                    return false;
            }

            CityConflictUnit myCityConflict = this.IsCityConflictUnit;
            CityConflictUnit otherCityConflict = this.IsCityConflictUnit;

            {
                if ( myCityConflict != null && myCityConflict.WillNotAttackAnyoneIfCityFlagFalse != null && !myCityConflict.WillNotAttackAnyoneIfCityFlagFalse.DuringGameplay_IsTripped )
                    return false;
                if ( otherCityConflict != null && otherCityConflict.WillNotAttackAnyoneIfCityFlagFalse != null && !otherCityConflict.WillNotAttackAnyoneIfCityFlagFalse.DuringGameplay_IsTripped )
                    return false;

                if ( myCityConflict != null && myCityConflict.WillNotAttackPlayerIfCityFlagFalse != null && !myCityConflict.WillNotAttackPlayerIfCityFlagFalse.DuringGameplay_IsTripped &&
                    (theyArePlayer || otherIsPlayerAlly) )
                    return false;
                if ( otherCityConflict != null && otherCityConflict.WillNotAttackPlayerIfCityFlagFalse != null && !otherCityConflict.WillNotAttackPlayerIfCityFlagFalse.DuringGameplay_IsTripped &&
                    (weArePlayer || myIsPlayerAlly) )
                    return false;
            }

            bool isLikelyFriend = false;

            if ( myCityConflict != null )
            {
                if ( myCityConflict.ParentConflict.Aggressors.ContainsKey( myCohort.ID ) )
                {
                    if ( myCityConflict.ParentConflict.Defenders.ContainsKey( otherCohort.ID ) )
                        return true; //kill them!!!

                    if ( myCityConflict.ParentConflict.Aggressors.ContainsKey( otherCohort.ID ) )
                        isLikelyFriend = true; //no fight friend!  If not enemy elsewhere
                }
                else if ( myCityConflict.ParentConflict.Defenders.ContainsKey( myCohort.ID ) )
                {
                    if ( myCityConflict.ParentConflict.Aggressors.ContainsKey( otherCohort.ID ) )
                        return true; //kill them!!!


                    if ( myCityConflict.ParentConflict.Defenders.ContainsKey( otherCohort.ID ) )
                        isLikelyFriend = true; //no fight friend!  If not enemy elsewhere
                }
            }
            
            //only evaluate this if it's a separate conflict
            if ( otherCityConflict != null && otherCityConflict != myCityConflict )
            {
                if ( otherCityConflict.ParentConflict.Aggressors.ContainsKey( myCohort.ID ) )
                {
                    if ( otherCityConflict.ParentConflict.Defenders.ContainsKey( otherCohort.ID ) )
                        return true; //kill them!!!

                    if ( otherCityConflict.ParentConflict.Aggressors.ContainsKey( otherCohort.ID ) )
                        return false; //no fight friend!
                }
                else if ( otherCityConflict.ParentConflict.Defenders.ContainsKey( myCohort.ID ) )
                {
                    if ( otherCityConflict.ParentConflict.Aggressors.ContainsKey( otherCohort.ID ) )
                        return true; //kill them!!!

                    if ( otherCityConflict.ParentConflict.Defenders.ContainsKey( otherCohort.ID ) )
                        return false; //no fight friend!
                }
            }

            if ( isLikelyFriend )
                return false; //they are friend here, with no overriding enemy status from second conflict. No fight.

            if ( theyArePlayer && this.GetWillFireOnMachineUnitsBaseline() )
            {
                if ( !myStance.WillAttackShellCompanyMachinesEvenIfNotAggroed && (OtherUnit.UnitType?.IsTiedToShellCompany??false) )
                {
                    if ( this.GetAmountHasAggroedNPCCohort( otherCohort ) > 0 || OtherUnit.GetAmountHasAggroedNPCCohort( myCohort ) > 0 )
                        return true; //if one of us aggroed the other, then we are going to fight regardless of other things, EXCEPT what the megacorp told us to do
                    return false; //they're the wrong kind, ignore them
                }
                else if ( myStance.WillAttackShellCompanyMachinesEvenIfNotAggroed && !(OtherUnit.UnitType?.IsTiedToShellCompany ?? false) )
                {
                    if ( this.GetAmountHasAggroedNPCCohort( otherCohort ) > 0 || OtherUnit.GetAmountHasAggroedNPCCohort( myCohort ) > 0 )
                        return true; //if one of us aggroed the other, then we are going to fight regardless of other things, EXCEPT what the megacorp told us to do
                    return false; //they're the wrong kind, ignore them
                }

                return true; //that's a player unit, so get them
            }

            if ( myStance.WillHoldFireAgainstNoncombatants && otherStance.IsConsideredNoncombatant )
                return false; //I don't shoot noncombatants, whatever they are up to

            //if ( OtherUnit.GetIsTrackedByCohort( myCohort ) || this.GetIsTrackedByCohort( otherCohort ) )
            //    return true; //one of us is tracking the other, so we will shoot at each other

            if ( weArePlayer )
            {
                if ( myStance.WillHoldFireAgainstPassiveGuards && otherStance.IsStillConsideredPassiveGuardToPlayerForces  )
                {
                    return false; //these guys are not after us, so don't shoot them, ya idiot
                }

                if ( otherStance.IsConsideredActiveCohortGuard )
                {
                    if ( OtherUnit.HomePOI != null )
                    {
                        MapPOI otherPOI = OtherUnit.CalculatePOI();
                        if ( otherPOI != OtherUnit.HomePOI )
                            return true; //they are out of their home POI, so shoot them
                        else //they are in their home POI
                        {
                            MapPOI myPOI = this.CalculatePOI();
                            if ( myPOI != OtherUnit.HomePOI )
                            {
                                //I am not in there with them
                                float theirSquaredRange = OtherUnit.GetAttackRangeSquared();
                                float distSquared = (this.GetDrawLocation() - OtherUnit.GetDrawLocation()).GetSquareGroundMagnitude();
                                if ( theirSquaredRange < distSquared ) //and I am not in their attack range
                                    return false; //don't shoot them
                                return true; //DO shoot them, as we are in range of them
                            }
                            else
                                return true; //DO shoot them, as we are in their same POI with them
                        }
                    }
                }

                if ( OtherUnit.GetIsValidToAutomaticallyShootAt_Current( this ) )
                {
                    if ( !otherStance.WillAttackShellCompanyMachinesEvenIfNotAggroed && (this.UnitType?.IsTiedToShellCompany ?? false) )
                    {
                        if ( this.GetAmountHasAggroedNPCCohort( otherCohort ) > 0 || OtherUnit.GetAmountHasAggroedNPCCohort( myCohort ) > 0 )
                            return true; //if one of us aggroed the other, then we are going to fight regardless of other things, EXCEPT what the megacorp told us to do
                        return false; //they're the wrong kind, ignore them
                    }
                    else if ( otherStance.WillAttackShellCompanyMachinesEvenIfNotAggroed && !(this.UnitType?.IsTiedToShellCompany ?? false) )
                    {
                        if ( this.GetAmountHasAggroedNPCCohort( otherCohort ) > 0 || OtherUnit.GetAmountHasAggroedNPCCohort( myCohort ) > 0 )
                            return true; //if one of us aggroed the other, then we are going to fight regardless of other things, EXCEPT what the megacorp told us to do
                        return false; //they're the wrong kind, ignore them
                    }

                    return true; //if we are a player unit, and they want to shoot at us, then we're going to shoot at them too
                }
            }

            if ( myStance.WillHoldFireAgainstUnitsThatArePassiveAtFullHealth && otherStance.WillHoldFireIfAtFullHealth && OtherUnit.GetActorDataLostFromMax( ActorRefs.ActorHP, true ) <= 0 )
            {
                return false; //cannot shoot it in an automated way if it's at full health and would ignore us
            }

            if ( myStance.WillHoldFireAgainstPassiveGuards && otherStance.IsConsideredPassiveGuard  )
            {
                if ( OtherUnit.HomePOI != null )
                {
                    if ( this.GetIsPartOfPlayerForcesInAnyWay() || myStance.IsConsideredMachineInTermsOfWhichActiveGuardsToAttack )
                    {
                        if ( !OtherUnit.HomePOI.IsPOIAlarmed_AgainstPlayer )
                            return false; //I cannot shoot them because they are guarding a poi, but they will also not shoot me
                    }
                    else
                    {
                        if ( !OtherUnit.HomePOI.IsPOIAlarmed_ThirdParty )
                            return false; //I cannot shoot them because they are guarding a poi, but they will also not shoot me
                    }
                }
                else
                    return false; //I don't shoot passive guards of this sort
            }

            if ( otherCohort.IsInwardLookingMegacorpAlly && myCohort.IsInwardLookingMegacorpAlly )
                return false; //we serve the masters...

            if ( !this.GetWillFireOnMachineUnitsBaseline() && !OtherUnit.GetWillFireOnMachineUnitsBaseline() )
                return false; //hey, we are both on cohort "do not shoot at machines!"  So we will not shoot at each other, either

            if ( otherManaged?.IsBlockedFromAnyKilling??false )
                return false; //if this unit is not allowed to be killed
            if ( otherCityConflict?.IsBlockedFromAnyKilling ?? false )
                return false; //if this unit is not allowed to be killed



            if ( ( myStance.IsOnOrdersFromTheLocalMegacorp || myStance.IsConsideredCohortGuard || myStance.IsConsideredActiveCohortGuard ) &&
                (otherStance.IsOnOrdersFromTheLocalMegacorp || otherStance.IsConsideredCohortGuard || otherStance.IsConsideredActiveCohortGuard) )
                return false; //we are both on orders from the megacorp, or both guards of some kind, and we're told not to interfere with one another, so no shooting at one another

            if ( myCohort == null || otherCohort == null )
                return true; //one of us has a busted group, go ahead and fire

            if ( myCohort.RowIndexNonSim == otherCohort.RowIndexNonSim )
                return false; //we never shoot at units from the exact same group

            if ( this.GetAmountHasAggroedNPCCohort( otherCohort ) > 0 || OtherUnit.GetAmountHasAggroedNPCCohort( myCohort ) > 0 )
                return true; //if one of us aggroed the other, then we are going to fight regardless of other things, EXCEPT what the megacorp told us to do

            if ( myStance.IsConsideredActiveCohortGuard && otherStance.WillAttackAllActiveCohortGuards )
                return true; //they're after me
            if ( myStance.WillAttackAllActiveCohortGuards && otherStance.IsConsideredActiveCohortGuard )
                return true; //I am after them

            if ( !myStance.IsAllowedToAggroPOIGuards )
            {
                if ( this.GetIsPartOfPlayerForcesInAnyWay() || myStance.IsConsideredMachineInTermsOfWhichActiveGuardsToAttack )
                {
                    if ( OtherUnit.HomePOI != null && !OtherUnit.HomePOI.IsPOIAlarmed_AgainstPlayer )
                        return false; //I cannot shoot them because they are guarding a poi, but they will also not shoot me
                }
                else
                {
                    if ( OtherUnit.HomePOI != null && !OtherUnit.HomePOI.IsPOIAlarmed_ThirdParty )
                        return false; //I cannot shoot them because they are guarding a poi, but they will also not shoot me
                }
            }

            if ( myStance.IsConsideredWealthyCivilian && otherStance.WillHoldFireAgainstWealthyCivilians )
                return false; //they won't shoot me because wealthy civilian
            if ( myStance.WillHoldFireAgainstWealthyCivilians && otherStance.IsConsideredWealthyCivilian )
                return false; //we won't shoot them because wealthy civilian

            if ( myStance.IsPartOfCityEconomy && otherStance.WillHoldFireAgainstCityEconomy )
                return false; //they won't shoot me because part of the city economy
            if ( myStance.WillHoldFireAgainstCityEconomy && otherStance.IsPartOfCityEconomy )
                return false; //we won't shoot them because part of the city economy

            if ( myStance.IsPartOfRegionalEconomy && otherStance.WillHoldFireAgainstRegionalEconomy )
                return false; //they won't shoot me because part of the regional economy
            if ( myStance.WillHoldFireAgainstRegionalEconomy && otherStance.IsPartOfRegionalEconomy )
                return false; //we won't shoot them because part of the regional economy

            if ( myStance.IsPartOfInternationalEconomy && otherStance.WillHoldFireAgainstInternationalEconomy )
                return false; //they won't shoot me because part of the international economy
            if ( myStance.WillHoldFireAgainstInternationalEconomy && otherStance.IsPartOfInternationalEconomy )
                return false; //we won't shoot them because part of the international economy

            if ( myStance.IsConsideredDisruptive || otherStance.IsConsideredDisruptive )
            {
                //one of us is disruptive

                //at this point vastly more likely to shoot
            }
            else 
            {
                //neither of us is disruptive

                if ( ( myStance.IsConsideredCohortGuard || myStance.IsBeneficialToTheCohortGuard ) && otherStance.WillHoldFireAgainstCohortGuard )
                    return false; //they won't shoot me because guard
                if ( myStance.WillHoldFireAgainstCohortGuard && ( otherStance.IsConsideredCohortGuard || otherStance.IsBeneficialToTheCohortGuard) )
                    return false; //we won't shoot them because guard

                if ( myStance.WillHoldFireAgainstNonDisruptive || otherStance.WillHoldFireAgainstNonDisruptive )
                    return false; //we won't shoot each other because one of us will hold fire against non-disruptive, and neither of us is disruptive
            }

            return true; //no other excuse found, so shoot at each other
        }
        #endregion

        public bool GetIsValidToAutomaticallyShootAt_TheoreticalOtherLocation( ISimMapMobileActor Target, MapPOI TheoreticalLocationPOI,
            ISimBuilding TheoreticalBuildingOrNull, Vector3 TheoreticalLocation, MapPOI TheoreticalAggroedPOI, NPCCohort TheoreticalAggroedCohort, bool WillHaveDoneAttack )
        {
            if ( this.DisbandAtTheStartOfNextTurn )
                return false;

            if ( this.Stance?.WillHoldFireIfAtFullHealth ?? false )
            {
                if ( this.GetActorDataLostFromMax( ActorRefs.ActorHP, true ) <= 0 )
                    return false;
            }

            if ( !(this.Stance?.IsHyperAggressiveAgainstAllButItsOwnCohort ?? false) )
            {
                bool isAfterShellCompany = this.Stance?.IsAfterShellCompany ?? false;
                if ( isAfterShellCompany )
                {
                    if ( !Target.GetIsRelatedToPlayerShellCompanyOrCompletelyUnrelatedToPlayer() )
                        return false;
                }
            }

            if ( this.HomePOI != null && !this.HomePOI.IsPOIAlarmed_AgainstPlayer && TheoreticalAggroedPOI != this.HomePOI )
            {
                if ( TheoreticalLocationPOI == this.HomePOI )
                {
                    //they are coming into my house!

                    if ( Target.IsFullDead || Target.GetIsCurrentlyInvisible( InvisibilityPurpose.ForPlayerTargeting ) )
                        return false; //they are invisible or dead, so ignore them

                    if ( Target.IsCloaked && !WillHaveDoneAttack )
                        return false; //they are cloaked and not making an attack, so ignore them

                    NPCManagedUnit myManaged = this.IsManagedUnit;
                    if ( myManaged != null && myManaged.WillNotAttackAnyoneIfCityFlagFalse != null && !myManaged.WillNotAttackAnyoneIfCityFlagFalse.DuringGameplay_IsTripped )
                        return false;
                    if ( myManaged != null && myManaged.WillNotAttackPlayerIfCityFlagFalse != null && !myManaged.WillNotAttackPlayerIfCityFlagFalse.DuringGameplay_IsTripped )
                        return false;

                    CityConflictUnit myConflictUnit = this.IsCityConflictUnit;
                    if ( myConflictUnit != null && myConflictUnit.WillNotAttackAnyoneIfCityFlagFalse != null && !myConflictUnit.WillNotAttackAnyoneIfCityFlagFalse.DuringGameplay_IsTripped )
                        return false;
                    if ( myConflictUnit != null && myConflictUnit.WillNotAttackPlayerIfCityFlagFalse != null && !myConflictUnit.WillNotAttackPlayerIfCityFlagFalse.DuringGameplay_IsTripped )
                        return false;

                    if ( !WillHaveDoneAttack )
                    {
                        if ( Target.GetEffectiveClearance( TheoreticalBuildingOrNull == null ? ClearanceCheckType.MovingToBuilding : ClearanceCheckType.MovingToNonBuilding ) 
                            >= this.HomePOI.Type.RequiredClearance.Level )
                            return false; //they won't have done an attack and they do have the right clearance, so ignore them
                    }

                    if ( Target is ISimMachineActor machineActor )
                    {
                        if ( this.GetIsAnAllyFromThePlayerPerspective() )
                            return false;

                        bool isAggroed = machineActor.GetAmountHasAggroedNPCCohort( this.FromCohort ) > 0 || (TheoreticalAggroedCohort != null && TheoreticalAggroedCohort == this.FromCohort);
                        if ( !isAggroed )
                        {
                            MobileActorTypeDuringGameData dgd = machineActor.GetTypeDuringGameData();
                            if ( dgd?.IsTiedToShellCompany ?? false )
                            {
                                if ( !(this.Stance?.WillAttackShellCompanyMachinesEvenIfNotAggroed ?? false) )
                                    return false;
                            }
                            else
                            {
                                if ( this.Stance?.WillHoldFireAgainstRegularMachinesExceptThoseAggroed ?? false )
                                    return false;
                            }
                        }
                        if ( machineActor.GetIsAbleToAvoidAutoTargetingShotAtAtProposedLocation( this.FromCohort, TheoreticalBuildingOrNull, TheoreticalLocation, WillHaveDoneAttack, isAggroed ) )
                        { } //they are blending in or something;
                        else
                            return true; //KILL
                    }
                    else if ( Target is ISimMachineActor npcUnit && npcUnit.GetIsPartOfPlayerForcesInAnyWay() )
                        return true; //KILL
                }
                else
                {
                    if ( Target.GetIsTrackedByCohort( this.FromCohort ) )
                        return true; //we are tracking them -- get them!
                    if ( Target.OutcastLevel > MathRefs.OutcastLevelAtWhichGuardsFireOutOfPOIs.IntMin )
                    {
                        if ( this.Stance.WillAttackOutcastMachinesAtOrAboveLevel >= Target.OutcastLevel )
                            return true; //if they are moving around outside of our POI, but they are above our outcast level we want to shoot at, then shoot at them
                    }

                    return false; //they are not moving to in the POI we are guarding, or hyper-outcast, so ignore them
                }
            }

            {
                if ( Target is ISimNPCUnit npcUnit )
                {
                    if ( this.GetWillFireAtOtherNPCUnit( npcUnit ) )
                        return true;
                    return false;
                }
                else if ( Target is ISimMachineActor machineActor )
                {
                    if ( Target.IsFullDead || Target.GetIsCurrentlyInvisible( InvisibilityPurpose.ForPlayerTargeting ) )
                        return false; //they are invisible or dead, so ignore them

                    if ( Target.IsCloaked && !WillHaveDoneAttack )
                        return false; //they are cloaked and not making an attack, so ignore them

                    if ( this.GetIsAnAllyFromThePlayerPerspective() )
                        return false; //don't shoot the player, then

                    NPCManagedUnit myManaged = this.IsManagedUnit;
                    if ( myManaged != null && myManaged.WillNotAttackAnyoneIfCityFlagFalse != null && !myManaged.WillNotAttackAnyoneIfCityFlagFalse.DuringGameplay_IsTripped )
                        return false;
                    if ( myManaged != null && myManaged.WillNotAttackPlayerIfCityFlagFalse != null && !myManaged.WillNotAttackPlayerIfCityFlagFalse.DuringGameplay_IsTripped )
                        return false;

                    CityConflictUnit myConflictUnit = this.IsCityConflictUnit;
                    if ( myConflictUnit != null && myConflictUnit.WillNotAttackAnyoneIfCityFlagFalse != null && !myConflictUnit.WillNotAttackAnyoneIfCityFlagFalse.DuringGameplay_IsTripped )
                        return false;
                    if ( myConflictUnit != null && myConflictUnit.WillNotAttackPlayerIfCityFlagFalse != null && !myConflictUnit.WillNotAttackPlayerIfCityFlagFalse.DuringGameplay_IsTripped )
                        return false;

                    if ( machineActor.GetIsTrackedByCohort( this.FromCohort ) )
                        return true; //we are tracking them -- get them!
                    if ( machineActor.OutcastLevel > 0 )
                    {
                        if ( this.Stance.WillAttackOutcastMachinesAtOrAboveLevel >= machineActor.OutcastLevel )
                            return true;
                    }
                    bool isAggroed = machineActor.GetAmountHasAggroedNPCCohort( this.FromCohort ) > 0 || (TheoreticalAggroedCohort != null && TheoreticalAggroedCohort == this.FromCohort);
                    if ( !isAggroed )
                    {
                        MobileActorTypeDuringGameData dgd = machineActor.GetTypeDuringGameData();
                        if ( dgd?.IsTiedToShellCompany ?? false )
                        {
                            if ( !(this.Stance?.WillAttackShellCompanyMachinesEvenIfNotAggroed ?? false) )
                                return false;
                        }
                        else
                        {
                            if ( this.Stance?.WillHoldFireAgainstRegularMachinesExceptThoseAggroed ?? false )
                                return false;
                        }
                    }
                    if ( machineActor.GetIsAbleToAvoidAutoTargetingShotAtAtProposedLocation( this.FromCohort, TheoreticalBuildingOrNull, TheoreticalLocation, WillHaveDoneAttack, isAggroed ) )
                        return false; //they are blending in or something;
                    return true;
                }
                else if ( Target is MachineStructure structure )
                {
                    if ( this.GetIsAnAllyFromThePlayerPerspective() )
                        return false; //don't shoot the player, then

                    if ( structure.IsFullDead || structure.IsCloaked )
                        return false;

                    if ( structure.CurrentJob?.IsRelatedToShellCompany ?? false )
                    {
                        if ( !(this.Stance?.WillAttackShellCompanyStructures ?? false) )
                            return false;
                    }
                    else
                    {
                        if ( this.Stance?.WillHoldFireAgainstRegularMachineStructures ?? false )
                            return false;
                    }

                    if ( structure.GetActorDataCurrent( ActorRefs.NeuralSecretProtection, true ) > 0 )
                        return false;

                    if ( structure.Type?.IsTerritoryControlFlag ?? false )
                        return false;

                    NPCManagedUnit myManaged = this.IsManagedUnit;
                    if ( myManaged != null && myManaged.WillNotAttackAnyoneIfCityFlagFalse != null && !myManaged.WillNotAttackAnyoneIfCityFlagFalse.DuringGameplay_IsTripped )
                        return false;
                    if ( myManaged != null && myManaged.WillNotAttackPlayerIfCityFlagFalse != null && !myManaged.WillNotAttackPlayerIfCityFlagFalse.DuringGameplay_IsTripped )
                        return false;

                    CityConflictUnit myConflictUnit = this.IsCityConflictUnit;
                    if ( myConflictUnit != null && myConflictUnit.WillNotAttackAnyoneIfCityFlagFalse != null && !myConflictUnit.WillNotAttackAnyoneIfCityFlagFalse.DuringGameplay_IsTripped )
                        return false;
                    if ( myConflictUnit != null && myConflictUnit.WillNotAttackPlayerIfCityFlagFalse != null && !myConflictUnit.WillNotAttackPlayerIfCityFlagFalse.DuringGameplay_IsTripped )
                        return false;

                    if ( structure.GetActorDataCurrent( ActorRefs.StructureHidden, true ) > 0 )
                        return false; //if it hasn't been discovered yet!

                    if ( structure.Type?.IsNanotechNetworkTower ?? false )
                    {
                        if ( !FlagRefs.CanAttackNanotechNetworkTowers.DuringGameplay_IsTripped )
                            return false;
                    }

                    return true; //go ahead after the structure!
                }
            }
            return false;
        }

        public bool GetIsValidToCatchInAreaOfEffectExplosion_Current( ISimMapActor Target )
        {
            if ( Target is ISimNPCUnit npcUnit )
                return this.GetWillFireAtOtherNPCUnit( npcUnit );
            else if ( Target is ISimMachineActor machineActor )
            {
                if ( Target.IsFullDead )
                    return false; //if they are dead, stop.  We don't care about cloaking or invisibility, since this is a secondary effect

                if ( machineActor.GetIsTrackedByCohort( this.FromCohort ) )
                    return true; //we are tracking them -- get them!
                if ( machineActor.OutcastLevel > 0 )
                {
                    if ( this.Stance.WillAttackOutcastMachinesAtOrAboveLevel >= machineActor.OutcastLevel )
                        return true;
                }
                if ( this.GetIsAnAllyFromThePlayerPerspective() )
                    return false;

                bool isAggroed = machineActor.GetAmountHasAggroedNPCCohort( this.FromCohort ) > 0;
                if ( !isAggroed )
                {
                    MobileActorTypeDuringGameData dgd = machineActor.GetTypeDuringGameData();
                    if ( dgd?.IsTiedToShellCompany ?? false )
                    {
                        if ( !(this.Stance?.WillAttackShellCompanyMachinesEvenIfNotAggroed ?? false) )
                            return false;
                    }
                    else
                    {
                        if ( this.Stance?.WillHoldFireAgainstRegularMachinesExceptThoseAggroed ?? false )
                            return false;
                    }
                }
                //we do not care about any sort of blending in
                return true;
            }
            else if ( Target is MachineStructure structure )
            {
                if ( this.GetIsAnAllyFromThePlayerPerspective() )
                    return false;

                if ( structure.IsFullDead || structure.IsCloaked )
                    return false;

                if ( structure.CurrentJob?.IsRelatedToShellCompany ?? false )
                {
                    if ( !(this.Stance?.WillAttackShellCompanyStructures ?? false) )
                        return false;
                }
                else
                {
                    if ( this.Stance?.WillHoldFireAgainstRegularMachineStructures ?? false )
                        return false;
                }

                if ( structure.GetActorDataCurrent( ActorRefs.NeuralSecretProtection, true ) > 0 )
                    return false;

                if ( structure.Type?.IsTerritoryControlFlag??false )
                    return false;

                if ( structure.Type?.IsNanotechNetworkTower ?? false )
                {
                    if ( !FlagRefs.CanAttackNanotechNetworkTowers.DuringGameplay_IsTripped )
                        return false;
                }

                //we do not care about the structure being discovered or not!

                return true; //go ahead after the structure!
            }
            return false;
        }

        public bool GetIsValidToAutomaticallyShootAt_Current( ISimMapActor Target )
        {
            return GetIsValidToAutomaticallyShootAt_SameLocation( Target, false, false );
        }

        public bool GetIsValidToAutomaticallyShootAt_SameLocation( ISimMapActor Target, bool WillBeShiftedToTakingCover, bool WillBeShiftedToCloaked )
        {
            if ( Target.IsFullDead )
                return false;

            if ( this.Stance?.WillHoldFireIfAtFullHealth ?? false )
            {
                if ( this.GetActorDataLostFromMax( ActorRefs.ActorHP, true ) <= 0 )
                    return false;
            }

            if ( this.DisbandAtTheStartOfNextTurn )
                return false;

            int armor = Target.GetActorDataCurrent( ActorRefs.ActorArmorPlating, true );
            if ( armor > 0 )
            {
                int piercing = this.GetActorDataCurrent( ActorRefs.ActorArmorPiercing, true );
                int remainder = armor - piercing;
                if ( remainder >= 100 )
                    return false; //don't be an idiot, we can't even scratch them
            }

            if ( this.Stance?.IsHyperAggressiveAgainstAllButItsOwnCohort ?? false )
            {
                if ( Target is ISimNPCUnit npcUnit )
                {
                    NPCCohort myCohort = this.FromCohort;
                    NPCCohort theirCohort = npcUnit.FromCohort;
                    if ( myCohort == theirCohort )
                        return false; //we don't shoot each other
                }
                return true;
            }

            if ( !(this.Stance?.IsHyperAggressiveAgainstAllButItsOwnCohort ?? false) )
            {
                bool isAfterShellCompany = this.Stance?.IsAfterShellCompany ?? false;
                if ( isAfterShellCompany )
                {
                    if ( !Target.GetIsRelatedToPlayerShellCompanyOrCompletelyUnrelatedToPlayer() )
                        return false;
                }
            }

            bool isNonAlarmedPOI = false;
            MapPOI homePOI = this.HomePOI;
            if ( homePOI != null )
            {
                if ( Target.GetIsPartOfPlayerForcesInAnyWay() )
                    isNonAlarmedPOI = !homePOI.IsPOIAlarmed_AgainstPlayer;
                else
                    isNonAlarmedPOI = !homePOI.IsPOIAlarmed_ThirdParty;
            }

            if ( homePOI != null && isNonAlarmedPOI )
            {
                if ( Target.CalculatePOI() == this.HomePOI )
                {
                    //they are in my house!

                    if ( Target.IsFullDead || Target.GetIsCurrentlyInvisible( InvisibilityPurpose.ForPlayerTargeting ) )
                        return false; //they are invisible or dead, so ignore them

                    if ( Target.IsCloaked )
                        return false; //they are cloaked, so ignore them

                    NPCManagedUnit myManaged = this.IsManagedUnit;
                    if ( myManaged != null && myManaged.WillNotAttackAnyoneIfCityFlagFalse != null && !myManaged.WillNotAttackAnyoneIfCityFlagFalse.DuringGameplay_IsTripped )
                        return false;
                    if ( myManaged != null && myManaged.WillNotAttackPlayerIfCityFlagFalse != null && !myManaged.WillNotAttackPlayerIfCityFlagFalse.DuringGameplay_IsTripped )
                        return false;

                    CityConflictUnit myConflictUnit = this.IsCityConflictUnit;
                    if ( myConflictUnit != null && myConflictUnit.WillNotAttackAnyoneIfCityFlagFalse != null && !myConflictUnit.WillNotAttackAnyoneIfCityFlagFalse.DuringGameplay_IsTripped )
                        return false;
                    if ( myConflictUnit != null && myConflictUnit.WillNotAttackPlayerIfCityFlagFalse != null && !myConflictUnit.WillNotAttackPlayerIfCityFlagFalse.DuringGameplay_IsTripped )
                        return false;

                    if ( Target is ISimMapMobileActor mobileTarget )
                    {
                        if ( mobileTarget.GetEffectiveClearance( ClearanceCheckType.StayingThere ) >= this.HomePOI.Type.RequiredClearance.Level )
                            return false; //they do have the right clearance, so ignore them
                    }

                    if ( Target is ISimMachineActor machineActor )
                    {
                        if ( this.GetIsAnAllyFromThePlayerPerspective() )
                            return false;
                        return true; //KILL
                    }
                    else if ( Target is ISimMachineActor npcUnit && npcUnit.GetIsPartOfPlayerForcesInAnyWay() )
                        return true; //KILL
                }
                else
                {
                    if ( Target.GetIsTrackedByCohort( this.FromCohort ) )
                        return true; //we are tracking them -- get them!
                    if ( Target.OutcastLevel > MathRefs.OutcastLevelAtWhichGuardsFireOutOfPOIs.IntMin )
                    {
                        if ( this.Stance.WillAttackOutcastMachinesAtOrAboveLevel >= Target.OutcastLevel )
                            return true; //if they are outside of our POI, but they are above our outcast level we want to shoot at, then shoot at them
                    }

                    return false; //they are not in the POI we are guarding, or hyper-outcast, so ignore them
                }
            }

            {
                if ( Target is ISimNPCUnit npcUnit )
                    return this.GetWillFireAtOtherNPCUnit( npcUnit );
                else if ( Target is ISimMachineActor machineActor )
                {
                    if ( this.GetIsAnAllyFromThePlayerPerspective() )
                        return false;

                    if ( Target.IsFullDead || Target.GetIsCurrentlyInvisible( InvisibilityPurpose.ForPlayerTargeting ) || Target.IsCloaked || WillBeShiftedToCloaked )
                        return false; //they are invisible or dead or cloaked, so ignore them

                    NPCManagedUnit myManaged = this.IsManagedUnit;
                    if ( myManaged != null && myManaged.WillNotAttackAnyoneIfCityFlagFalse != null && !myManaged.WillNotAttackAnyoneIfCityFlagFalse.DuringGameplay_IsTripped )
                        return false;
                    if ( myManaged != null && myManaged.WillNotAttackPlayerIfCityFlagFalse != null && !myManaged.WillNotAttackPlayerIfCityFlagFalse.DuringGameplay_IsTripped )
                        return false;

                    CityConflictUnit myConflictUnit = this.IsCityConflictUnit;
                    if ( myConflictUnit != null && myConflictUnit.WillNotAttackAnyoneIfCityFlagFalse != null && !myConflictUnit.WillNotAttackAnyoneIfCityFlagFalse.DuringGameplay_IsTripped )
                        return false;
                    if ( myConflictUnit != null && myConflictUnit.WillNotAttackPlayerIfCityFlagFalse != null && !myConflictUnit.WillNotAttackPlayerIfCityFlagFalse.DuringGameplay_IsTripped )
                        return false;

                    if ( machineActor.GetIsTrackedByCohort( this.FromCohort ) )
                        return true; //we are tracking them -- get them!
                    if ( machineActor.OutcastLevel > 0 )
                    {
                        if ( this.Stance.WillAttackOutcastMachinesAtOrAboveLevel >= machineActor.OutcastLevel )
                            return true;
                    }
                    bool isAggroed = machineActor.GetAmountHasAggroedNPCCohort( this.FromCohort ) > 0;
                    if ( !isAggroed )
                    {
                        if ( !this.GetWillFireOnMachineUnitsBaseline() ) //we only care about this if we are not specifically aggroed at the target
                            return false;

                        MobileActorTypeDuringGameData dgd = machineActor.GetTypeDuringGameData();
                        if ( dgd?.IsTiedToShellCompany ?? false )
                        {
                            if ( !(this.Stance?.WillAttackShellCompanyMachinesEvenIfNotAggroed ?? false) )
                                return false;
                        }
                        else
                        {
                            if ( this.Stance?.WillHoldFireAgainstRegularMachinesExceptThoseAggroed ?? false )
                                return false;
                        }
                    }
                    if ( machineActor.GetIsCurrentlyAbleToAvoidAutoTargetingShotAt_CurrentLocation( this.FromCohort, isAggroed ) )
                        return false; //they are blending in or something
                    return true;
                }
                else if ( Target is MachineStructure structure )
                {
                    if ( this.GetIsAnAllyFromThePlayerPerspective() )
                        return false;

                    if ( structure.IsFullDead || structure.IsCloaked )
                        return false;

                    if ( structure.CurrentJob?.IsRelatedToShellCompany ?? false )
                    {
                        if ( !(this.Stance?.WillAttackShellCompanyStructures ?? false) )
                            return false;
                    }
                    else
                    {
                        if ( this.Stance?.WillHoldFireAgainstRegularMachineStructures ?? false )
                            return false;
                    }

                    if ( structure.GetActorDataCurrent( ActorRefs.NeuralSecretProtection, true ) > 0 )
                        return false;

                    NPCManagedUnit myManaged = this.IsManagedUnit;
                    if ( myManaged != null && myManaged.WillNotAttackAnyoneIfCityFlagFalse != null && !myManaged.WillNotAttackAnyoneIfCityFlagFalse.DuringGameplay_IsTripped )
                        return false;
                    if ( myManaged != null && myManaged.WillNotAttackPlayerIfCityFlagFalse != null && !myManaged.WillNotAttackPlayerIfCityFlagFalse.DuringGameplay_IsTripped )
                        return false;

                    CityConflictUnit myConflictUnit = this.IsCityConflictUnit;
                    if ( myConflictUnit != null && myConflictUnit.WillNotAttackAnyoneIfCityFlagFalse != null && !myConflictUnit.WillNotAttackAnyoneIfCityFlagFalse.DuringGameplay_IsTripped )
                        return false;
                    if ( myConflictUnit != null && myConflictUnit.WillNotAttackPlayerIfCityFlagFalse != null && !myConflictUnit.WillNotAttackPlayerIfCityFlagFalse.DuringGameplay_IsTripped )
                        return false;

                    if ( structure.GetActorDataCurrent( ActorRefs.StructureHidden, true ) > 0 )
                        return false; //if it hasn't been discovered yet!
                    if ( structure.Type?.IsTerritoryControlFlag ?? false )
                        return false;

                    if ( structure.Type?.IsNanotechNetworkTower ?? false )
                    {
                        if ( !FlagRefs.CanAttackNanotechNetworkTowers.DuringGameplay_IsTripped )
                            return false;
                    }

                    return true; //go ahead after the structure!
                }
            }
            return false;
        }

        public override bool GetIsVeryLowPriorityForLog()
        {
            return this.Stance?.ThisUnitIsOfVeryLowLogImportance ?? false;
        }
    }
}
