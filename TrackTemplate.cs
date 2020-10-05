using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackTemplate : MonoBehaviour
{
    //TRACKTEMPLATE (Christian):
    /*
     *  Created: 11.02.2019 - 12:20 AM - (Christian)
     *  Edited:  11.04.2019 - -------- - (Christian)
     *           11.22.2019 - -------- - (Christian)
     *  
     *  Overall Purpose: Contains pre-set data which, when fed to TrackGenMaster, produces a randomly generated track with specific properties.
     */

    public LoadNodeMatrix loadNodePositions; //Special struct/class hybrid used exclusively to represent a 2D node matrix in inspector, allows nodes to be set and changed from there
    public GameObject[] finalTrackPrefabs; //Array of objects used in this track prefab, placed during Pass3
    public int finalResolution; //The resolution of the final track prefabs in the array

    [Header("Starting Inputs:")] //Basic inputs already on trackGenerator which template replaces
    //Array Construction:
    public bool customShape; //If checked, sends existing array of loadNodes childed to template GameObject and sets array construction variables to the properties of that array
    public float resolution; //How many pixels across is each unit (all units must use the same base resolution) (refactored by program)
    public int columns;      //How many units wide this template is (max)
    public int rows;         //How many units tall this template is (max)
    public int depth;        //How many layers deep this template is (max)
    //Start/Finish Positions:
    public Vector3Int start;     //Describes a 3-dimensional point in LoadNode array where generator will begin generating track
    public Vector3Int finish;    //Describes a 3-dimensional point in LoadNode array where generator will end track generation
    public float startRotation;  //Describes direction to attempt to place start line in. Start may correct itself if this results in outflow (or optionally inflow) being blocked
    public float finishRotation; //Describes direction to attempt to place finish line in. Finish may correct itself if this results in inflow being blocked

    [Header("Pass 1 Gates:")] //Variables which affect the properties of the track spit out by Pass 1
    //Pass 1 Duration Governors:
    public int generationAttempts;    //What number of track generation tries to make before settling with the one closest to desired properties
    public int failureThreshold;      //What number of unsuccessful track generation attempts to make before returning an error (label seed as bad)
    [Space()]
    //Part Weights:
    public List<AdaptiveWeightSegmentP1> adaptiveWeightSegments = new List<AdaptiveWeightSegmentP1>(); //Allows user to control weight procedurally throughout track generation process
    [Range(2, 100)] public int weightGranularity; //Determines how precise random track determination is when factoring in weight. Generally has very little effect on algorithm when above 10
    [Range(0, 1)] public float straightWeight;    //If STRAIGHT1x1  is among viable track candidates, this decides how likely it is to be picked
    [Range(0, 1)] public float leftWeight;        //If TURNLEFT1x1  is among viable track candidates, this decides how likely it is to be picked
    [Range(0, 1)] public float rightWeight;       //If TURNRIGHT1x1 is among viable track candidates, this decides how likely it is to be picked

    [Header("Desirability Settings:")] //Variables which affect which track generation gets chosen as workingTrack
    //TRACKTOTAL:
        [Range(0, 1)] public float trackWeight; //How important track number is when deciding generation desirability
        [Range(1, 0)] public float trackTargetPenalty; //How much being off target will decrease track desirability score
        public int targetTrackMin; //What number of track parts to shoot for (minimum, set equal to upper for precise target)
        public int targetTrackMax; //What number of track parts to shoot for (maximum, set equal to lower for precise target)
        [Space()]
    //DIAGONALTOTAL:
        [Range(0, 1)] public float diagWeight; //How important track number is when deciding generation desirability
        [Range(1, 0)] public float diagTargetPenalty; //How much being off target will decrease diag desirability score
        public int targetDiagMin; //What number of diagonalized segments to shoot for (minimum, set equal to upper for precise target)
        public int targetDiagMax; //What number of diagonalized segments to shoot for (maximum, set equal to lower for precise target)
        [Space()]
    //TIGHT180TOTAL:
        [Range(0, 1)] public float tight180Weight; //How important tight180 number is when deciding generation desirability
        [Range(1, 0)] public float tightTargetPenalty; //How much being off target will decrease tight180 desirability score
        public int target180Min; //What number of tight180s to shoot for (minimum, set equal to upper for precise target)
        public int target180Max; //What number of tight180s to shoot for (maximum, set equal to lower for precise target)

    [Header("Pass 2 Additions")] //Settings for optimizing track construction, as well as tuning determinants of track desirability
    //DIAGONAL SMOOTHING:
        public bool diagonalSmoothing; //If checked, this will smooth all wiggle patterns into simpler, easier-to-drive diagonals (highly-reccomended for highly-compressed tracks)
        public int maxDiagonalThreshold; //If this number of turns in a potential diagonal is exceeded, count track as failure

    //Internal State Variables:
    internal int currentAWSegment; //Used by trackGenerator to keep track of which adaptive weight segment it is using
}

