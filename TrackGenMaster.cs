using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackGenMaster : MonoBehaviour
{
    //TRACK GENERATION MASTER:
    /* 
     *  Created: 11.02.2019 - 12:04 AM - (Christian)
     *  Edited:  11.02.2019 - -------- - (Christian)
     *           11.03.2019 - -------- - (Christian)
     *           11.04.2019 - -------- - (Christian)
     *           11.09.2019 - -------- - (Christian)
     *           11.10.2019 - -------- - (Christian)
     *           11.20.2019 - -------- - (Christian)
     *           11.21.2019 - -------- - (Christian)
     *           11.22.2019 - -------- - (Christian)
     *           11.23.2019 - -------- - (Christian)
     * 
     *  Overall Purpose: Builds randomly-generated racetracks in the background using given track templates, part sets, LoadNode sets, and other more specific variables
     *  Implemented Functionalities:
     *      - Allows user to input basic track generation data (rows, columns, depth, resolution) and generates a grid of blank LoadNodes according to that data
     *      - Allows user to manually assign start and/or finish point when inputting track generation data, then generates start and/or finish at designated location(s)
     *      - Procedurally generates a track which avoids collisions and dead ends, and always finds the finish line
     *      - Contains several methods related to checking LoadNode relationships, verifying LoadNode data and assigning LoadNodes
     *      - Fully compatible with Track Template objects, which can be used to build random generation presets and have more precise track generation options
     *      - Chooses from amongst pool of generated tracks based on desirability rating, which can be tuned via template
     *      - Optimizes tracks after generation for better driving experience
     *      - Spits out a fully-driveable track based on track part prefabs included in template
     */

    //Node Types/Node Classes/Node Structs:
    //NodeType: Used exclusively by nodes to internally determine what kind of track a node is programatically, rather than visually
    public enum NodeType {
                            //STANDARD PASS#1 PARTS:
                                Blank1x1,      //BLANK-----(1x1): Default node containing no track.
                                Start1x1,      //START-----(1x1): Node where track begins generation. Every race begins here.
                                Finish1x1,     //FINISH----(1x1): Track generation destination node. Racers cross this to win.
                                Straight1x1,   //STRAIGHT--(1x1): Basic straightaway node. Nothing interesting.
                                TurnLeft1x1,   //TURNLEFT--(1x1): 90-degree turn left. Hard turn.
                                TurnRight1x1,  //TURNRIGHT-(1x1): 90-degree turn right. Hard turn.
                            //ADVANCED PASS#1 PARTS:
                                TurnLeft2x2,      //TURNLEFT------(2x2): 90-degree turn left. More gradual turn.
                                TurnRight2x2,     //TURNRIGHT-----(2x2): 90-degree turn right. More gradual turn.
                                //STurnLeft2x2,     //STURNLEFT-----(2x2): Smooth skew one node to the left. Navigable.
                                //STurnRight2x2,    //STURNRIGHT----(2x2): Smooth skew one node to the right. Navigable.
                                //STurnLeft3x2,     //STURNLEFT-----(3x2): Smooth skew two nodes to the left. Less navigable.
                                //STurnRight3x2,    //STURNRIGHT----(3x2): Smooth skew two nodes to the right. Less navigable.
                                //AdjLoopLeft3x3,   //ADJLOOPLEFT---(3x3): Full loop which ends next to where it began in opposite direction.
                                //AdjLoopRight3x3,  //ADJLOOPRIGHT--(3x3): Full loop which ends next to where it began in opposite direction.
                                //CornLoopLeft3x3,  //CORNLOOPLEFT--(3x3): Full loop which crosses over itself and ends 90 degrees from where it started.
                                //CornLoopRight3x3, //CORNLOOPRIGHT-(3x3): Full loop which crosses over itself and ends 90 degrees from where it started.
                            //SUPPLEMENTARY PASS#2 PARTS:
                                DiagonalLeft3,    //DIAGONALLEFT--(3): Replaces a left-right-left wiggle with a smooth 45-degree diagonal.
                                DiagonalRight3,   //DIAGONALRIGHT-(3): Replaces a right-left-right wiggle with a smooth 45-degree diagonal.
                                DiagonalLeft5,    //DIAGONALLEFT--(5): Replaces a left-right-left-right-left wiggle with a smooth diagonal.
                                DiagonalRight5,   //DIAGONALRIGHT-(5): Replaces a right-left-right-left-right wiggle with a smooth diagonal.
                                DiagonalLeft7,    //DIAGONALLEFT--(5): Replaces a left-right-left-right-left wiggle with a smooth diagonal.
                                DiagonalRight7,   //DIAGONALRIGHT-(5): Replaces a right-left-right-left-right wiggle with a smooth diagonal.
                         }

    //Objects and Components:
    [Header("Objects and Components:")]
    public GameObject template;    //A template prefab containing all necessary info to guide track generation through every step of the process
    private TrackTemplate tempCont; //Template controller, where all significant information about track template is stored
    public GameObject trackPrefab; //Built to contain all viable track parts, track name, and other variables associated with a fully-assembled track
    public GameObject loadNode;    //Basic placeholder gameObject set used to pre-empt track part placement
    public Sprite[] nodeSprites;   //TEMPORARY: Contains visual references of sprites for loadNodes

    [Header("Track Generation Inputs:")]
        //Contains: Necessary input factors used to build 3D array of LoadNodes in which to generate track
    public float resolution; //How many pixels across is each unit (all units must use the same base resolution) (refactored by program)
    public int columns;      //How many units wide this template is (max)
    public int rows;         //How many units tall this template is (max)
    public int depth;        //How many layers deep this template is (max)
    [Space()]
    public Vector3Int start;       //Describes a 3-dimensional point in LoadNode array where generator will begin generating track
    public Vector3Int finish;      //Describes a 3-dimensional point in LoadNode array where generator will end track generation
    public float startRotation;    //Describes direction to attempt to place start line in. Start may correct itself if this results in outflow (or optionally inflow) being blocked
    public float finishRotation;   //Describes direction to attempt to place finish line in. Finish may correct itself if this results in inflow being blocked

    //Track Flow Variables:
        //Contains: Variables involved with internal track generation and keeping tabs of program counter position in physical space (within generated LoadNode array)
    internal GameObject[][][] loadNodes; //Full array of all loadNodes instantiated by generator
    internal List<GameObject> trackGenerations = new List<GameObject>();  //Array of pass1 tracks completed by generator. If larger than 1, generator chooses one with track count closest to targetTrackNumber
    private List<NodeType> flowTimeline = new List<NodeType>(); //A list describing the order and type of track parts in current track
    private List<GameObject> objectTimeline = new List<GameObject>(); //A list containing all track part objects in current track in order
    private GameObject workingTrack; //Track container gameObject which holds all track parts, eventually being spit out as final product of generator. Has a mastercontroller of its own for storing track stats

    private GameObject startNode;            //Stores start node to be checked for pass completion
    private GameObject finishNode;           //Stores finish node to be checked for pass completion
    private GameObject autoCompleteNode;     //Stores node where, once reached, track will automatically flow into end (generally is just targetNode)
    private GameObject endNode;              //Stores the node this track ends on, considering looping
    [Space()]
    public GameObject targetNode;            //Stores node generator is currently building track towards
    public GameObject currentNode;           //Stores the current node being programmed by track generator
    public GameObject nextNode;              //The next node in sequence, used by track generator to direct flow
    [Space()]
    [Range(2, 100)] public int weightGranularity; //Determines how precise random track determination is when factoring in weight. Generally has very little effect on algorithm when above 10
    [Range(0, 1)] public float straightWeight;    //If STRAIGHT1x1  is among viable track candidates, this decides how likely it is to be picked
    [Range(0, 1)] public float leftWeight;        //If TURNLEFT1x1  is among viable track candidates, this decides how likely it is to be picked
    [Range(0, 1)] public float rightWeight;       //If TURNRIGHT1x1 is among viable track candidates, this decides how likely it is to be picked

    //Internal State Variables:
    //Contains: General private/internal variables which determine the state of the program, and alter decisions it makes after different factors set them appropriately
    [SerializeField] private bool showVisuals; //Activates visual representations of track generation when pulled true
    [SerializeField] private bool inspectTrackSet; //Stops generation as soon as Pass1 is complete, allowing user to check all generation attempts for consistency
    [SerializeField] private bool inspectTrackSet2; //Stops generation as soon as Pass2.25 is complete, allowing user to check Diag Smoothing and other processes for consistency
    [SerializeField] private bool generateImmediately; //Causes generator to immediately generate a track upon scene load, should be checked at all times when TrackGenerator is not being developed
    private bool usingTemplate; //Pulled true if track generator detects a compatible template
    private bool looping; //Pulled true if finish vector is left null.  Causes program to generate track that loops
    private bool passComplete; //Pulled true by pass method once current pass has been completed
    private GameObject forceTrack; //Track to be forced into assignment as a means of completing pass

    //Pass 1 Duration Governors:
    public int generationAttempts;            //What number of track generation tries to make before settling with the one closest to desired properties
    public int failureThreshold;              //What number of unsuccessful track generation attempts to make before returning an error (label seed as bad)
    [ShowOnly] public int genAttemptsMade;   //Tracks how many generation attempts have been made thus far
    [ShowOnly] public int genAttemptsFailed; //Tracks how many generation attempts either returned an error or did not meet criteria upon completion

    //Internal Math Variables:
    //Contains: Basic "setup-and-forget" counters and trackers used in internal equations, meant never to see the light of day. Stuff like smoothDamp refs goes here
    private int partsPlaced; //The number of track parts placed in current track generation attempt
    private float genBeginTime; //The real time last generation began, used to gauge performance
        //Desirability Counters:
        private float targetTrackNumber; //The number of track parts to shoot for, set by template (can be somewhat random)
        private float targetDiagNumber; //The average number of diagonalized segments in track to shoot for, set by template (can be somewhat random)
        private float target180Number; //The number of tight180s to shoot for, set by template (can be somewhat random)

    private void Start()
    {
        if (generateImmediately == true) { GenerateTrack(); } //Generate track on sceneload if instructed to do so
    }

    private void Update()
    {
        if (generateImmediately == false) //Only execute testing functions when not in normal generation mode
        {
            //Testing functions, put here what you need to test:
            if (Input.GetKeyDown(KeyCode.Mouse0) == true) { } //Use this to click onto the play window
            else if (Input.GetKeyDown(KeyCode.Space) == true)
            {
                GenerateTrack();
            }
            else if (Input.GetKeyDown(KeyCode.T) == true) //Use this space to test individual processes and such
            {

            }
            else if (Input.anyKeyDown == true)
            {

            }
        }
        
    }

    public GameObject GenerateTrack()
    {
        //GENERATE TRACK (Christian):
        /*
         *  Function: Generates a track from scratch (given (rows, columns, depth) OR preset template) and returns an array of gameObjects which can be assembled into a functional track
         *  Implemented Functionalities:
         *      -Returns an error if not enough information is given
         *      -Initializes two arrays, one for placeholder LoadNodes and one for final track part output (for efficiency)
         *      -Generates a 3D grid of LoadNodes based on template parameters
         *      -Detects whether or not track is set to loop
         *      -Calls Start and Finish node assignments
         *      -Assigns nodes procedurally (beginning at outflow of start) to node representations of track parts available (randomly)
         *      -Iterates generation multiple times if told, tracking and packaging/exporting successes and logging failures
         *      -Decides on best track based on iterations and desired track properties, then discards the others
         *      -Replaces placeholder loadnodes with Final Track Parts (FTPs) from template reservoir
         */

//PASS#0>>>INITIALIZE GENERATOR-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        //INITIALIZE Template:
        if (template != null) //If PRESET TEMPLATE is provided...
        {
            Debug.Log("TrackGenerator: Using template..."); //Return confirmation message
            usingTemplate = true; //Tell code that a template is being used
            GetTemplateInfo(); //Collect all data from template and link its processes to this track generator
        }
        else if (resolution == 0 || //If INCOMPLETE INFORMATION is provided...
                 columns ==    0 || //"
                 rows ==       0 || //"
                 depth ==      0)   //"
        {
            Debug.Log("TrackGenerator: Incomplete information given," +
                                     " track generation unsuccessful");  //Return error message
            return null; //Break from method with null return
        }
        else //If BASIC PROPERTIES are provided(with no template)...
        {
            Debug.Log("TrackGenerator: Using preset template given..."); //Return confirmation message
            //ADD: Way to read a TrackTemplate depending on input method
        }

        //INITIALIZE Variables:
        float factoredRes = resolution / 100; //Scales down given pixel size to match Unity unit size
        genAttemptsMade = 0; //Initialize generation success counter
        genAttemptsFailed = 0; //Initialize generation failure counter
        trackGenerations = new List<GameObject>(); //Scrub trackGenerations from last generation
        workingTrack = null; //Scrub workingTrack from last generation
        loadNodes =  new GameObject[columns][][];                         //Set number of columns
        for (int x = loadNodes.Length; x > 0; x--)
        {
            loadNodes[x - 1] =  new GameObject[rows][];                   //Set number of rows
            for (int y = loadNodes[x - 1].Length; y > 0; y--)
            {
                loadNodes[x - 1][y - 1] =  new GameObject[depth];         //Set number of layers
                for (int z = loadNodes[x - 1][y - 1].Length; z > 0; z--)
                {
                    loadNodes[x - 1][y - 1][z - 1] = null;                //Initialize each node at null to prevent errors
                }
            }
        }
        if (usingTemplate == true) //Initialize Template Desirability Variables:
        {
            tempCont.currentAWSegment = 0; //Reset current segment tracker

            targetTrackNumber = Random.Range(tempCont.targetTrackMin, tempCont.targetTrackMax); //Optionally make target track number slightly random to increase track variation
            targetDiagNumber =  Random.Range(tempCont.targetDiagMin, tempCont.targetDiagMax); //Optionally make target diag number slightly random to increase track variation
            target180Number = Random.Range(tempCont.target180Min, tempCont.target180Max); //Optionally make target tight180 number slightly random to increase track variation

            Debug.Log("TrackGenerator: Target Track Number = " + targetTrackNumber);
            Debug.Log("TrackGenerator: Target Diag Number = " + targetDiagNumber);
            Debug.Log("TrackGenerator: Target Tight180 Number = " + target180Number);
        }


//PASS#1>>>GENERATE TRACK FLOW--------------------------------------------------------------------------------------------------------------------------------------------------------------------

        Debug.Log("TrackGenerator: Beginning Generation");

        while (genAttemptsMade < generationAttempts && genAttemptsFailed < failureThreshold && workingTrack == null)
        {
            //RE-INITIALIZE Leftover Variables (from last generation):
            genBeginTime = Time.realtimeSinceStartup; //Log start time of generation attempt
            flowTimeline = new List<NodeType>(); //Re-initialize flowTimeline for new generation
            objectTimeline = new List<GameObject>(); //Re-initialize objectTimeLine for new generation

            //INSTANTIATE Grid of LoadNodes:
            Debug.Log("TrackGenerator: Generating LoadNodes...");
            for (int x = loadNodes.Length; x > 0; x--)                          //For each column...
            {
                for (int y = loadNodes[x - 1].Length; y > 0; y--)               //...for each row in that column...
                {
                    for (int z = loadNodes[x - 1][y - 1].Length; z > 0; z--)    //...for each layer at that point...
                    {
                        if (usingTemplate == true) //If a template is being used, check to see if this node actually exists...
                        {
                            if (tempCont.customShape == true && tempCont.loadNodePositions.columns[loadNodes[x - 1].Length - y].row[x - 1] == true) //If node exists on designated node matrix, place it as usual...
                            {
                                loadNodes[x - 1][y - 1][z - 1] = Instantiate(loadNode, transform);                //Instantiate a blank loadNode and child it to TrackGenerator object
                                loadNodes[x - 1][y - 1][z - 1].transform.position = new Vector2((x - 1) * 0.32f,  //Set x coordinates on grid (automatically scale down position)
                                                                                                (y - 1) * 0.32f); //Set y coordinates on grid (automatically scale down position)
                            }
                        }
                        else //Business as usual...
                        {
                            loadNodes[x - 1][y - 1][z - 1] = Instantiate(loadNode, transform);                //Instantiate a blank loadNode and child it to TrackGenerator object
                            loadNodes[x - 1][y - 1][z - 1].transform.position = new Vector2((x - 1) * 0.32f,  //Set x coordinates on grid (automatically scale down position)
                                                                                            (y - 1) * 0.32f); //Set y coordinates on grid (automatically scale down position)
                        }
                    }
                }
            }
            //INITIALIZE Each Instantiated LoadNode:
            Debug.Log("TrackGenerator: Initializing LoadNodes...");
            for (int x = loadNodes.Length; x > 0; x--)                          //For each column...
            {
                for (int y = loadNodes[x - 1].Length; y > 0; y--)               //...for each row in that column...
                {
                    for (int z = loadNodes[x - 1][y - 1].Length; z > 0; z--)    //...for each layer at that point...
                    {
                        if (loadNodes[x - 1][y - 1][z - 1] != null) //For each existing node within given bounds...
                        {
                            loadNodes[x - 1][y - 1][z - 1].GetComponent<LoadNodeMaster>().InitializeNode(new Vector3Int(x - 1, y - 1, z - 1), this); //Give each node necessary data
                        }
                    }
                }
            }
            //DETERMINE End Node Position:
            if (finish.x == 0 && finish.y == 0 && finish.z == 0) //If finish is left null...
            {
                Debug.Log("TrackGenerator: Track set to loop");
                looping = true; //Tell program to generate looping track
            }
            else if (finish.x - 1 > columns || finish.y - 1 > rows || finish.z - 1 > depth) //If finish is outside bounds of track...
            {
                Debug.Log("TrackGenerator: Finish is outside bounds of track," +
                                         " track generation unsuccessful"); //Return error message
                return null; //Break from method with null return
            }
            else if (finish.x == 0 || finish.y == 0 || finish.z == 0) //If finish is left partially null...
            {
                Debug.Log("TrackGenerator: Finish cannot have partially null value," +
                                         " track generation unsuccessful"); //Return error message
                return null; //Break from method with null return
            }
            else //If finish location is valid...
            {
                AssignNode(new Vector3Int(finish.x - 1, finish.y - 1, finish.z - 1), NodeType.Finish1x1, finishRotation); //Assign node as finish
                endNode = finishNode; //Set finishNode as endNode
            }
            //DETERMINE Start Node Position:
            if (start.x - 1 > columns || start.y - 1 > rows || start.z - 1 > depth) //If start is outside bounds of track...
            {
                Debug.Log("TrackGenerator: Start is outside bounds of track," +
                                         " track generation unsuccessful"); //Return error message
                return null; //Break from method with null return
            }
            else if (start.x == 0 || start.y == 0 || start.z == 0) //If start is null...
            {
                Debug.Log("TrackGenerator: Start cannot have null value," +
                                         " track generation unsuccessful"); //Return error message
                return null; //Break from method with null return
            }
            else //If starting location is valid...
            {
                AssignNode(new Vector3Int(start.x - 1, start.y - 1, start.z - 1), NodeType.Start1x1, startRotation); //Assign node as start
                if (endNode == null) { endNode = startNode; }
            }
            //DETERMINE Destinations/Destination Positions:
            if (looping == true) //If track loops and has no waypoints or finish line...
            {
                //Populate AutoCompleteNode Data:
                autoCompleteNode = FlowTarget(startNode, null, 180, false, false); //Set autoComplete node where track path will always lead
                autoCompleteNode.GetComponent<LoadNodeMaster>().outflowNode = startNode; //Link autoCompleteNode's outflow to startNode inflow
                startNode.GetComponent<LoadNodeMaster>().inflowNode = autoCompleteNode; //Link startNode's inflow to autoCompleteNode's outflow
                targetNode = autoCompleteNode;
            }
            else //If track no waypoints and a finish line...
            {
                //Populate AutoCompleteNode Data:
                autoCompleteNode = FlowTarget(finishNode, null, 180, false, false); //Set autoComplete node where track path will always lead
                autoCompleteNode.GetComponent<LoadNodeMaster>().outflowNode = finishNode; //Link autoCompleteNode's outflow to finishNode inflow
                finishNode.GetComponent<LoadNodeMaster>().inflowNode = autoCompleteNode; //Link finishNode's inflow to autoCompleteNode's outflow
                targetNode = autoCompleteNode;
            }

            //GENERATE Track candidate:
            Debug.Log("TrackGenerator: Beginning Pass");
            while (passComplete == false)
            {
                PlaceTrack(); //Complete a single pass (one track part placement) and, if successful, add completed track to list of generations
            }
            passComplete = false; //Prepare for another pass

            //RE-TREAD Track With Additional Data:
            if (usingTemplate == true) //This function is exclusive to template-based track generations
            {
                //DIAGONALS:
                    CheckDiagonodes(trackGenerations[trackGenerations.Count - 1]); //Check the diagonodes of current track
                //TIGHT180s:
                    CheckTight180s(trackGenerations[trackGenerations.Count - 1]); //Check the tight180s of current track
            }
        }

        //LOG/RESET Generation tracker variables:
        Debug.Log("TrackGenerator: Generation Pass #1 Finished! " + genAttemptsMade + " generation attempts made, " + genAttemptsFailed+ " generation attempts failed.");
        genAttemptsMade = 0;   //Reset generation attempt tracker
        genAttemptsFailed = 0; //Reset generation failure tracker

    /*TEMPLATE NEEDED TO PASS>----------*/ if (usingTemplate == false) { return null; } //--------------------------------------------------------------------------------------------------------

        //CALCULATE Desirability Ratings for Each Track Candidate:
        for (int x = 0; x < trackGenerations.Count; x++) //Parse through all track generations...
        {
            TrackMaster currentTrackCont = trackGenerations[x].GetComponent<TrackMaster>();

            //CALCULATE Distances from Targets:
            //Note: Each is a positive, unadjusted number representing how far from target total is
            float trackDFT = Mathf.Abs(currentTrackCont.trackTotal - targetTrackNumber) + 1; //Track distance from target
            float diagDFT = Mathf.Abs(currentTrackCont.diagonalTotal - targetDiagNumber) + 1; //Diag distance from target
            float tightDFT = Mathf.Abs(currentTrackCont.tight180Total - target180Number) + 1; //Tight180 distance from target
            Debug.Log("TrackGenerator: Generation " + x + " distances from targets: Track = " + trackDFT +
                                                                                 ", Diag = " + diagDFT +
                                                                                 ", Tight180 = " + tightDFT);
            //CALCULATE Corrected Desirabilities:
            //Note: Each is a number from 0 to its desirability weight, representative of the track's distance from target (exponentially inversely proportional to distance from target)
            float trackCD = Mathf.Pow(Mathf.Min((targetTrackNumber / trackDFT) / targetTrackNumber, 1), tempCont.trackTargetPenalty) * tempCont.trackWeight; //Track corrected distance
            float diagCD = Mathf.Pow(Mathf.Min((targetDiagNumber / diagDFT) / targetDiagNumber, 1), tempCont.diagTargetPenalty) * tempCont.diagWeight; //Diag corrected distance
            float tightCD = Mathf.Pow(Mathf.Min((target180Number / tightDFT) / target180Number, 1), tempCont.tightTargetPenalty) * tempCont.tight180Weight; //Tight180 corrected distance
            Debug.Log("TrackGenerator: Generation " + x + " corrected distances: Track = " + trackCD +
                                                                              ", Diag = " + diagCD +
                                                                              ", Tight180 = " + tightCD);
            //CALCULATE Final Desirability:
            float desirability = (trackCD +     //TRACKWEIGHT: Calculate desirability contribution
                                  diagCD +      //DIAGWEIGHT: Calculate desirability contribution
                                  tightCD       //TIGHT180WEIGHT: Calculate desirability contribution
                                  ) / (
                                  tempCont.trackWeight +
                                  tempCont.diagWeight + 
                                  tempCont.tight180Weight);        

            currentTrackCont.overallRating = desirability; //Commit desirability data

        }

    /*INSPECTION GATE>------------------*/ if (inspectTrackSet == true) { return null; } //-------------------------------------------------------------------------------------------------------

        //CHOOSE Best track candidate, then delete all others:
        if (workingTrack == null) workingTrack = trackGenerations[0]; //Initialize working track place at first generation to start off comparisons (unless program has already returned a working track)
        for (int x = trackGenerations.Count; x > 0; x--) //Parse through all track generations...
        {
            if (trackGenerations[x - 1].GetComponent<TrackMaster>().overallRating > workingTrack.GetComponent<TrackMaster>().overallRating)
                { workingTrack = trackGenerations[x - 1]; } //If current track is more desirable than working champion, make current track working track
        }

        //DELETE All Unpicked Track Candidates:
        for (int x = trackGenerations.Count; x > 0; x--) { if (trackGenerations[x - 1] != workingTrack) { Destroy(trackGenerations[x - 1]); }} //Destroy all other track generations

//PASS#2>>>OPTIMIZE TRACK LAYOUT-----------------------------------------------------------------------------------------------------------------------------------------------------------------

        //INITIALIZE/RECLAIM Variables:
            List<NodeType> nodeTypesInTrack = new List<NodeType>(); //Initialize list of nodetypes found in this track, to check for with template FTP reservoir

            TrackMaster trackCont = workingTrack.GetComponent<TrackMaster>(); //Get working track's controller
            objectTimeline = trackCont.objectTimeline; //Get working flow timeline (in object form)
            resolution = tempCont.finalResolution; //Update resolution to high-res final track number

        //DIAGONAL SMOOTHING:
            if (tempCont.diagonalSmoothing == true) //Ignore diagonal smoothing is it has not been set by template
            {
                //ASSIGN Diagonals:
                    for (int x = 0; x < trackCont.diagonodes.Count; x++) //Parse through all valid Diagonodes...
                    {
                        AssignDiagonode(trackCont.diagonodes[x]); //Re-Assign Diagonode as proper diagonal
                    }
                //SYNCH Timelines:
                    workingTrack.GetComponent<TrackMaster>().objectTimeline = objectTimeline; //Synch object timeline
                    workingTrack.GetComponent<TrackMaster>().flowTimeline = flowTimeline; //Synch flow timeline
            }

        if (inspectTrackSet2 == true) { return null; } //End function if in designated testing mode


//PASS#3>>>GENERATE FINAL TRACK SEGMENTS----------------------------------------------------------------------------------------------------------------------------------------------------------
    
    //CHECK That all constituent loadnodes have final track analogs in template:
        for (int x = 0; x < objectTimeline.Count; x++) //Parse through loadnode flow timeline...
                { if (nodeTypesInTrack.Contains(objectTimeline[x].GetComponent<LoadNodeMaster>().nodeType) == false) //If the nodeType of current loadnode is not in nodeType list...
                    { nodeTypesInTrack.Add(objectTimeline[x].GetComponent<LoadNodeMaster>().nodeType); } } //Add newly-found nodeType

        for (int x = 0; x < nodeTypesInTrack.Count; x++) //Parse through list of nodeTypes in track...
            { if (GetTrackByName(tempCont.finalTrackPrefabs, nodeTypesInTrack[x]) == null) //If template does not contain an FTP for the given nodeType...
                { Debug.Log("TrackGenerator: Error! Template does not contain FTP of type '" + nodeTypesInTrack[x] + "'"); return null; } } //Post error and return null

        Debug.Log("TrackGenerator: Generation Confirmation! Template contains all FTPs necessary for generation"); //Log success

    //REPLACE LoadNodes with Actual Track Parts:
        for (int x = 0; x < objectTimeline.Count; x++) //Parse through loadnode flow timeline
        {
            //SUB-INITIALIZATIONS:
                LoadNodeMaster nm = objectTimeline[x].GetComponent<LoadNodeMaster>(); //Get current loadNode's controller
                NodeType nt = nm.nodeType; //Get target nodeType
                Vector3Int np = nm.nodePosition; //Get target position (abstract)
                float nr = nm.nodeRotation; //Get target rotation
            //FTP INSTANTIATION:
                GameObject newFTP = Instantiate(GetTrackByName(tempCont.finalTrackPrefabs, nt), workingTrack.transform); //Instantiate new FTP and child it to workingTrack container object
                newFTP.transform.position = new Vector3(np.x * (resolution/100), np.y * (resolution/100), np.z); //Assign Final Track Part's position based on updated resolution
                newFTP.GetComponent<Rigidbody2D>().rotation = nr; //Assign Final Track Part's rotation based on ancestor node
            //INHERIT LOADNODE DATA:
                newFTP.GetComponent<TrackPartMaster>().resolution = resolution; //Send resolution data to new track part
                //SetFlowPoints(objectTimeline[x], newFTP); //Log flow points of track part relative to part in real space
                objectTimeline[x] = newFTP; //Log new FTP in workingTrack's object timeline
        }
        trackCont.objectTimeline = objectTimeline; //Send updated objectTimeline back to trackCont
        
    //DESTROY Used Loadnode Set:
        DeleteNodes(trackCont.loadNodes); //Use pre-build method to systematically erase all LoadNodes in working track

//PASS#4>>>ADD TRACK BACKGROUND---------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    
    //INITIALIZE Variables:
        Vector2Int backgroundTileQuan = new Vector2Int(tempCont.columns + 2, tempCont.columns + 2); //Get width and height of background tiles to generate (with buffer of 1 tile on each side)
        Vector3 currentTilePos = Vector3.zero - new Vector3(resolution / 100, resolution / 100, 0); //Initialize starting placement point (offset by one tile diagonal from starting point)
    
    //PLACE Tiles:
        for (int x = 0; x < backgroundTileQuan.x; x++) //For each column in background...
        {
            for (int y = 0; y < backgroundTileQuan.y; y++) //For each row in background...
            {
                GameObject newFTP = Instantiate(GetTrackByName(tempCont.finalTrackPrefabs, NodeType.Blank1x1), workingTrack.transform); //Instantiate new FTP and child it to workingTrack container object
                newFTP.transform.position = currentTilePos; //Move newly-generated background tile to designated position
                currentTilePos.y += (resolution / 100); //Move current tile marker position vertically by one unit of resolution
                Debug.Log("TrackGenMaster: Placed background tile at coordinates " + newFTP.transform.position.x + "," + newFTP.transform.position.y);
            }
            currentTilePos.x += (resolution / 100); //Move current tile marker position horizontally by one unit of resolution
            currentTilePos.y = -(resolution / 100); //Move current tile marker y-position back to beginning
        }

//PASS#5>>>POLISH TRACK---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        //RE-POSITION Working Track:
        workingTrack.transform.position -= objectTimeline[0].transform.position; //Set track starting line at world center (and orient rest of track to match)
        //for (int x = 0; x < objectTimeline.Count; x++) { SetFlowPoints(objectTimeline[x]); } //Go back through tracks and re-position flow points

        //RETURN Final Track:
        workingTrack.GetComponent<TrackMaster>().generationTime = Time.realtimeSinceStartup - workingTrack.GetComponent<TrackMaster>().genBeginTime; //Re-log workingTrack's total generation time
        SetFlowPoints(workingTrack); //Log track flow points
        return workingTrack;
    }

    private void PlaceTrack()
    {
        //PASS 1 >>> EXTERNAL SUBMETHOD OF GENERATETRACK: Generates a rough contiguous track which connects starting line with designated end

            //INITIALIZE Variables:
            List<NodeType> viableNodes = new List<NodeType>(); //List to store viable node assignments, which can now be picked from at random
            GameObject flowTargetStraight = null; //Predicted flow target for STRAIGHT1x1 track
            GameObject flowTargetLeft =     null; //Predicted flow target for LEFTTURN1x1 track
            GameObject flowTargetRight =    null; //Predicted flow target for RIGHTTURN1x1 track

            //GET All potential flow targets based on track types:
            //Runs full gamut of available validity tests for each track type
            flowTargetStraight = FlowTarget(currentNode, targetNode, 0 + currentNode.GetComponent<LoadNodeMaster>().inflowNode.GetComponent<LoadNodeMaster>().nodeOutflowRot, true, true);
            flowTargetLeft =     FlowTarget(currentNode, targetNode, 90 + currentNode.GetComponent<LoadNodeMaster>().inflowNode.GetComponent<LoadNodeMaster>().nodeOutflowRot, true, true);
            flowTargetRight =    FlowTarget(currentNode, targetNode, -90 + currentNode.GetComponent<LoadNodeMaster>().inflowNode.GetComponent<LoadNodeMaster>().nodeOutflowRot, true, true);
            //CHECK for Pass Completion/Track Forcing
            if (forceTrack != null)
            {
                if (flowTargetStraight != forceTrack) { flowTargetStraight = null; } //Scrub all tracks not being forced
                if (flowTargetLeft != forceTrack)     { flowTargetLeft = null; }     //Scrub all tracks not being forced
                if (flowTargetRight != forceTrack)    { flowTargetRight = null; }    //Scrub all tracks not being forced
                forceTrack = null; //Reset forceTrack
            }

            //CHECK what flow targets are available:
            ConfirmTargets(); //Call for target confirmation, populating list with viable candidates
            void ConfirmTargets()
            {
                //CONFIRM TARGETS: Translates "Target" gameObjects into useable "NodeTypes"

                if (flowTargetStraight != null) //STRAIGHT1x1 TRACK: Test potential flow target viability
                {
                    viableNodes.Add(NodeType.Straight1x1); //Include if viable
                    //Debug.Log("Straight available");
                }
                if (flowTargetLeft != null) //LEFTTURN1x1 TRACK: Test potential flow target viability
                {
                    viableNodes.Add(NodeType.TurnLeft1x1); //Include if viable
                    //Debug.Log("Left available");
                }
                if (flowTargetRight != null) //RIGHTTURN1x1 TRACK: Test potential flow target viability
                {
                    viableNodes.Add(NodeType.TurnRight1x1); //Include if viable
                    //Debug.Log("Right available");
                }
            }

            //CHECK viable track list population:
            if (viableNodes.Count == 0) //If check has not produced any viable nodes...
            {
                //Try check again except without adjacency pulse validation (includes priority check). This issue almost always occurs when track is one node away from autoCompleteNode.
                flowTargetStraight = FlowTarget(currentNode, null, 0 + currentNode.GetComponent<LoadNodeMaster>().inflowNode.GetComponent<LoadNodeMaster>().nodeOutflowRot, true, false);
                flowTargetLeft = FlowTarget(currentNode, null, 90 + currentNode.GetComponent<LoadNodeMaster>().inflowNode.GetComponent<LoadNodeMaster>().nodeOutflowRot, true, false);
                flowTargetRight = FlowTarget(currentNode, null, -90 + currentNode.GetComponent<LoadNodeMaster>().inflowNode.GetComponent<LoadNodeMaster>().nodeOutflowRot, true, false);
                ConfirmTargets(); //Re-confirm targets
                if (viableNodes.Count == 0) //If there are still no viable targets, make last-ditch validation check in case there is only one possible option but it still works
                {
                    //Try check one last time, this time with no pulse or priority check. Solves one very uncommon error where track can box itself into very small corners
                    flowTargetStraight = FlowTarget(currentNode, currentNode, 0 + currentNode.GetComponent<LoadNodeMaster>().inflowNode.GetComponent<LoadNodeMaster>().nodeOutflowRot, true, false);
                    flowTargetLeft = FlowTarget(currentNode, currentNode, 90 + currentNode.GetComponent<LoadNodeMaster>().inflowNode.GetComponent<LoadNodeMaster>().nodeOutflowRot, true, false);
                    flowTargetRight = FlowTarget(currentNode, currentNode, -90 + currentNode.GetComponent<LoadNodeMaster>().inflowNode.GetComponent<LoadNodeMaster>().nodeOutflowRot, true, false);
                    ConfirmTargets(); //Re-confirm targets
                    if (viableNodes.Count == 0)
                    {
                        Debug.Log("TrackGenerator: Pass1: Error, no targets available");
                        genAttemptsFailed++; //Increment failure counter
                        partsPlaced = 0; //Reset part counter for next pass
                        if (usingTemplate == true) { tempCont.currentAWSegment = 0; } //Reset template segment counter if needed
                        passComplete = true; //Tell program to start over again
                        DeleteNodes(loadNodes); //Delete all current nodes in TrackGen and start fresh
                        return; //Break from pass
                    }
                }
                Debug.Log("TrackGenerator: Pass1: Adjacency check ignored");
            }

            //ASSIGN weight to each viable track option:
            if (viableNodes.Count > 1) //Only necessary if there is more than one option
            {
                //Check for template weight
                if (usingTemplate == true) { GetTemplateWeight(); } //Initialize template adaptive weight segment tracker if using template

                float totalWeight = 0; //Initialize total weight variable
                //Populate totalWeight with weights of contained variables
                if (viableNodes.Contains(NodeType.Straight1x1))  { totalWeight += straightWeight; }
                if (viableNodes.Contains(NodeType.TurnLeft1x1))  { totalWeight += leftWeight; }
                if (viableNodes.Contains(NodeType.TurnRight1x1)) { totalWeight += rightWeight; }
                //Populate List based on weight granularity
                if (viableNodes.Contains(NodeType.Straight1x1))
                {
                    Debug.Log("Straight Probability = " + Mathf.RoundToInt((straightWeight / totalWeight) * weightGranularity));
                    viableNodes.Remove(NodeType.Straight1x1); //Remove existing marker node from list
                    for (int x = Mathf.RoundToInt((straightWeight / totalWeight) * weightGranularity) - 1; x >= 0; x--)
                    { viableNodes.Add(NodeType.Straight1x1); } //Add NodeType markers according to calculated weight
                }
                if (viableNodes.Contains(NodeType.TurnLeft1x1))
                {
                    Debug.Log("Left Probability = " + Mathf.RoundToInt((leftWeight / totalWeight) * weightGranularity));
                    viableNodes.Remove(NodeType.TurnLeft1x1); //Remove existing marker node from list
                    for (int x = Mathf.RoundToInt((leftWeight / totalWeight) * weightGranularity) - 1; x >= 0; x--)
                    { viableNodes.Add(NodeType.TurnLeft1x1); } //Add NodeType markers according to calculated weight
                }
                if (viableNodes.Contains(NodeType.TurnRight1x1))
                {
                    Debug.Log("Right Probability = " + Mathf.RoundToInt((rightWeight / totalWeight) * weightGranularity));
                    viableNodes.Remove(NodeType.TurnRight1x1); //Remove existing marker node from list
                    for (int x = Mathf.RoundToInt((rightWeight / totalWeight) * weightGranularity) - 1; x >= 0; x--)
                    { viableNodes.Add(NodeType.TurnRight1x1); } //Add NodeType markers according to calculated weight
                }
            }

            //COMMIT one random track from available flow targets:
            NodeType selectedNode = viableNodes[Random.Range(0, viableNodes.Count)]; //Randomly pick an available node type
            AssignNode(currentNode.GetComponent<LoadNodeMaster>().nodePosition, selectedNode, currentNode.GetComponent<LoadNodeMaster>().nodeRotation); //Assign selected node
            partsPlaced++; //Increment track parts placed counter

            //SHIFT node register to next node:
            currentNode = nextNode;

            //CHECK for pass completion:
            if (passComplete == true) //If pass has been completed, check for failure and export track gameObject if successful
            {
                //GENERATE New Track gameObject and child completed track (with all its nodes) to new container (also populate container with necessary info)
                trackGenerations.Add(PackageNodes()); //Package nodes and add them to generations list
                trackGenerations[genAttemptsMade].transform.position = new Vector3(((resolution / 100) * columns + 0.1f) * genAttemptsMade, 0, 0); //Move to next spot in greater generation grid
                genAttemptsMade++; //Increment generation attempts to be representative of acual attempt number
                //Give container number of parts placed
                Debug.Log("TrackGenerator: Pass1: Completed Pass #" + genAttemptsMade + " with " + partsPlaced + " parts."); //Create data log of generation
                partsPlaced = 0; //Reset part counter for next pass
                if (usingTemplate == true) { tempCont.currentAWSegment = 0; } //Reset template segment counter if needed
            }
    }

    private void AssignNode(Vector3Int nodePos, NodeType nodeType, float rotation)
    {
        //ASSIGN NODE (Christian):
        /*
         *  Function: Assigns an existing node (at nodePos) the requested NodeType (nodeType), and populates any necessary data on node related to doing so.
         *            Rotation rotates node on assignment by designated amount, which is logged as the direction it is facing
         *  NOTE: MUST BE UPDATED every time a new type of track is added, along with any spatial information that track might entail
         *  NOTE: This code is fairly delicate, especially the data that goes into straightaways, leftTurns, and rightTurns. Their template must be followed exactly for generation
         *        algorithm to be successful.
         *  Implemented Functionalities:
         *      -Assigns the given node the given type and attaches the appropriate spriteRenderer
         *      -Intelligently decides finish node rotation based on adjacent nodes when assigning finish node
         *      -Intelligently decides start node rotation based on adjacent nodes when assigning start node
         *      -Automatically populates assigned nodes with necessary positional and relational data
         *      -Automatically generates node data in position relative to inflow rotation, and generates outflow data based on track type
         */

        //INITIALIZE Variables:
        LoadNodeMaster nodeCont = loadNodes[nodePos.x][nodePos.y][nodePos.z].GetComponent<LoadNodeMaster>(); //Get designated LoadNode's controller

        //ASSIGN Selected Node Type
        switch (nodeType)
        {

            //START1x1:
            case NodeType.Start1x1:

                    //Check Validity:
                    //Note: Default Start line orientation is vertical, outflowing northward
                    //Note: This only happens once per track generation
                    if (looping == true) //If track is set to loop...
                    {
                        if (nodeCont.northNode != null && nodeCont.southNode != null) //If node has a valid outflow and inflow...
                        {
                            rotation += 0; //Set rotation
                        }
                        else //Attempt fix with a 90-degree rotation...
                        {
                            if (nodeCont.westNode != null && nodeCont.eastNode != null) //If node has a valid outflow and inflow (after fix)...
                            {
                                rotation += 90; //Set rotation
                            }
                            else //If no rotation fixes outflow/inflow...
                            {
                                Debug.Log("TrackGenerator: Start location not viable");
                                return; //End method
                            }
                        }
                    }
                    else //If track has a separate finish line..
                    {
                        if (nodeCont.northNode != null) //If node has a valid outflow...
                        {
                            rotation += 0; //Set rotation
                        }
                        else //Attempt fix with a 90-degree rotation...
                        {
                            if (nodeCont.westNode != null) //If node has a valid outflow (after fix)...
                            {
                                rotation += 90; //Set rotation
                            }
                            else //Attempt fix with a 180-degree rotation...
                            {
                                if (nodeCont.southNode != null) //If node has a valid outflow (after fix)...
                                {
                                    rotation += 180; //Set rotation
                                }
                                else //Attempt fix with a 270-degree roation
                                {
                                    if (nodeCont.eastNode != null) //If node has a valid outflow (after fix)...
                                    {
                                        rotation += 270; //Set rotation
                                    }
                                    else //If no rotation fixes outflow...
                                    {
                                        Debug.Log("TrackGenerator: Start location not viable");
                                        return; //End method
                                    }
                                }
                            }
                        }
                    }

                    //Commit Assignment:
                        nodeCont.nodeType = NodeType.Start1x1; //Assign node type
                        nodeCont.GetComponent<SpriteRenderer>().sprite = nodeSprites[0]; //Set sprite (temporary)
                        nodeCont.transform.Rotate(Vector3.forward, rotation); //Rotate sprite as needed if fix was applied
                        nodeCont.nodeOutflowRot = 0 + rotation; //Gives the direction node flows out based on base flow rotation value and fixed rotation
                        nodeCont.nodeInflowRot = 0 + rotation;  //Gives the direction track can flow into this node
                        nodeCont.nodeRotation = rotation; //Set node rotation

                        startNode = nodeCont.gameObject; //Remember Start node for later
                        currentNode = FlowTarget(startNode, null, 0, false, false); //Set currentNode tracker to right in front of Start.  CurrentNode is the node to be assigned
                        currentNode.GetComponent<LoadNodeMaster>().inflowNode = startNode; //Tell current node first inflow
                        startNode.GetComponent<LoadNodeMaster>().outflowNode = currentNode; //Set outflow node from start

                        Debug.Log("TrackGenerator: Start location set");
                    break;

            //FINISH1x1:
            case NodeType.Finish1x1:

                    if (looping == false) //Finish is only needed if track does not loop
                    {
                        //Check Validity:
                        //Note: Default Finish line orientation is vertical, inflowing northward
                        //Note: This will only be called if looping == false and a finish line is needed
                        //Note: This only happens once per track generation
                        if (nodeCont.southNode != null) //If node has a valid inflow...
                        {
                            rotation += 0; //Set rotation
                        }
                        else //Attempt fix with a 90-degree rotation...
                        {
                            if (nodeCont.eastNode != null) //If node has a valid inflow (after fix)...
                            {
                                rotation += 90; //Set rotation
                            }
                            else //Attempt fix with a 180-degree rotation...
                            {
                                if (nodeCont.northNode != null) //If node has a valid inflow (after fix)...
                                {
                                    rotation += 180; //Set rotation
                                }
                                else //Attempt fix with a 270-degree roation
                                {
                                    if (nodeCont.westNode != null) //If node has a valid inflow (after fix)...
                                    {
                                        rotation += 270; //Set rotation
                                    }
                                    else //If no rotation fixes outflow...
                                    {
                                        Debug.Log("TrackGenerator: Finish location not viable");
                                        return; //End method
                                    }
                                }
                            }
                        }

                        //Commit Assignment:
                            nodeCont.nodeType = NodeType.Finish1x1; //Assign node type
                            nodeCont.GetComponent<SpriteRenderer>().sprite = nodeSprites[1]; //Set sprite (temporary)
                            nodeCont.transform.Rotate(Vector3.forward, rotation); //Rotate sprite as needed if fix was applied (just replace with nodeFlowRot)
                            nodeCont.nodeOutflowRot = 0 + rotation; //Gives the direction node flows out based on base flow rotation value and fixed rotation
                            nodeCont.nodeInflowRot = 0 + rotation;  //Gives the direction track can flow into this node
                            nodeCont.nodeRotation = rotation; //Set node rotation

                            finishNode = nodeCont.gameObject; //Remember Finish node for later

                            Debug.Log("TrackGenerator: Finish location set");
                    }
                    break;

            //STRAIGHT1x1:
            case NodeType.Straight1x1:

                    //Note: Validity is checked by FlowTarget before track part is placed, in order for predictive track generation to function. Calling this assignment without
                    //      flow-checking validity may result in janky track placement, as this assignment protocol will not correct itself after being called.

                    //Commit Assignment:
                    nodeCont.nodeType = NodeType.Straight1x1; //Assign node type
                    nodeCont.GetComponent<SpriteRenderer>().sprite = nodeSprites[2]; //Set sprite (temporary)
                    nodeCont.transform.Rotate(Vector3.forward, nodeCont.inflowNode.GetComponent<LoadNodeMaster>().nodeOutflowRot); //Rotate sprite to align with origin outflow
                    nodeCont.nodeInflowRot = nodeCont.inflowNode.GetComponent<LoadNodeMaster>().nodeOutflowRot;  //Gives the direction track can flow into this node
                    nodeCont.nodeOutflowRot = 0 + nodeCont.nodeInflowRot; //Gives the direction node flows out based on base flow rotation value and fixed rotation
                    nodeCont.nodeRotation = nodeCont.inflowNode.GetComponent<LoadNodeMaster>().nodeOutflowRot; //Set node program rotation

                    nextNode = FlowTarget(nodeCont.gameObject, null, rotation, false, false); //Get next node
                    nodeCont.outflowNode = nextNode; //Give current node identity of next node
                    nextNode.GetComponent<LoadNodeMaster>().inflowNode = nodeCont.gameObject; //Give next node origin info

                    Debug.Log("TrackGenerator: Straightaway placed");
                    break;

            //LEFT TURN1x1:
            case NodeType.TurnLeft1x1:

                    //Note: Validity is checked by FlowTarget before track part is placed, in order for predictive track generation to function. Calling this assignment without
                    //      flow-checking validity may result in janky track placement, as this assignment protocol will not correct itself after being called.

                    //Commit Assignment:
                    nodeCont.nodeType = NodeType.TurnLeft1x1; //Assign node type
                    nodeCont.GetComponent<SpriteRenderer>().sprite = nodeSprites[3]; //Set sprite (temporary)
                    nodeCont.transform.Rotate(Vector3.forward, nodeCont.inflowNode.GetComponent<LoadNodeMaster>().nodeOutflowRot); //Rotate sprite to align with origin outflow
                    nodeCont.nodeInflowRot = nodeCont.inflowNode.GetComponent<LoadNodeMaster>().nodeOutflowRot;  //Gives the direction track can flow into this node
                    nodeCont.nodeOutflowRot = 90 + nodeCont.nodeInflowRot; //Gives the direction node flows out based on base flow rotation value and fixed rotation
                    nodeCont.nodeRotation = nodeCont.inflowNode.GetComponent<LoadNodeMaster>().nodeOutflowRot; //Set node program rotation

                    nextNode = FlowTarget(nodeCont.gameObject, null, rotation + 90, false, false); //Get next node
                    nodeCont.outflowNode = nextNode; //Give current node identity of next node
                    nextNode.GetComponent<LoadNodeMaster>().inflowNode = nodeCont.gameObject; //Give next node origin info

                    Debug.Log("TrackGenerator: Left turn placed");
                    break;

            //RIGHT TURN1x1:
            case NodeType.TurnRight1x1:

                    //Note: Validity is checked by FlowTarget before track part is placed, in order for predictive track generation to function. Calling this assignment without
                    //      flow-checking validity may result in janky track placement, as this assignment protocol will not correct itself after being called.

                    //Commit Assignment:
                    nodeCont.nodeType = NodeType.TurnRight1x1; //Assign node type
                    nodeCont.GetComponent<SpriteRenderer>().sprite = nodeSprites[4]; //Set sprite (temporary)
                    nodeCont.transform.Rotate(Vector3.forward, nodeCont.inflowNode.GetComponent<LoadNodeMaster>().nodeOutflowRot); //Rotate sprite to align with origin outflow
                    nodeCont.nodeInflowRot = nodeCont.inflowNode.GetComponent<LoadNodeMaster>().nodeOutflowRot;  //Gives the direction track can flow into this node
                    nodeCont.nodeOutflowRot = -90 + nodeCont.nodeInflowRot; //Gives the direction node flows out based on base flow rotation value and fixed rotation
                    nodeCont.nodeRotation = nodeCont.inflowNode.GetComponent<LoadNodeMaster>().nodeOutflowRot; //Set node program rotation

                    nextNode = FlowTarget(nodeCont.gameObject, null, rotation - 90, false, false); //Get next node
                    nodeCont.outflowNode = nextNode; //Give current node identity of next node
                    nextNode.GetComponent<LoadNodeMaster>().inflowNode = nodeCont.gameObject; //Give next node origin info

                    Debug.Log("TrackGenerator: Right turn placed");
                    break;

            
        }

        //LOG Assigned Node on Timeline:
        flowTimeline.Add(nodeType);              //Add assigned node's type to timeline
        objectTimeline.Add(nodeCont.gameObject); //Add assigned node to timeline
    }

    private void CheckDiagonodes(GameObject currentTrack)
    {
        //CHECK DIAGONODES (Christian):
        /*
         *  Function: Parses through current track and fills in Diagonode information for each node
         */

        //INITIALIZE Variables:
        TrackMaster trackCont = currentTrack.GetComponent<TrackMaster>(); //Initialize shorthand for desired track's controller
        List<GameObject> diagonodes = trackCont.diagonodes; //Initialize list of potential diagonal nodes
        int diagTotal = 0; //Initialize variable for totalling all diagChains in a single track

        //FIND Potential Diagonode Start Points:
        for (int x = 0; x < objectTimeline.Count; x++) //Parse through objectTimeline (in direction of flow) and look for strings of alternating turns
        {
            //Initializations:
            LoadNodeMaster nodeCont = objectTimeline[x].GetComponent<LoadNodeMaster>(); //Initialize shorthand for current node's controller
            LoadNodeMaster tempNodeCont = nodeCont; //Used to store data as the program moves up the flow path looking for turn chains
            int diagChain = 0; //Initialize variable for counting how many alternating diagonals there have been in a row during this calculation
            List<GameObject> subDiagonondes = new List<GameObject>(); //Initialize list of constituent nodes in potential diagonal
                                                                      //Detect Diagonal Start:
            if (nodeCont.nodeType == NodeType.TurnLeft1x1 || //If node is a left turn...
                nodeCont.nodeType == NodeType.TurnRight1x1)  //If node is a right turn...
            {
                diagChain++; //Diag Chain is incremented by 1 for every CONSECUTIVE ALTERNATING TURN

                //Detect Additional DiagChain Segments:
                for (int y = x; y < objectTimeline.Count; y++) //Run detection protocol until break (technically auto-breaks when parse exceeds track length)...
                {
                    //Initializations:
                    tempNodeCont = tempNodeCont.outflowNode.GetComponent<LoadNodeMaster>(); //Move program to next node in track flow
                    GameObject diagSpace = null; //Initialize container for checking if potential diagonal would fit in track
                    //CHECK that segment is a TURN:
                    if (tempNodeCont.nodeType != NodeType.TurnLeft1x1 && tempNodeCont.nodeType != NodeType.TurnRight1x1) { break; } //If segment is NOT a turn, break
                    //CHECK that segment ALTERNATES:
                    if (tempNodeCont.nodeType == tempNodeCont.inflowNode.GetComponent<LoadNodeMaster>().nodeType) { break; } //If segment does NOT alternate, break
                    //CHECK that segment FITS:
                        //Note: Because a Diagonode adds nodespace to SOME adjacent areas, program needs to check that these areas are available
                    if (tempNodeCont.nodeType == NodeType.TurnLeft1x1 && tempNodeCont.nodeType == nodeCont.nodeType) //LEFT: If node is turning in same direction of diag...
                    { diagSpace = GetNodeRelative(trackCont.loadNodes, tempNodeCont.gameObject, new Vector3Int(1, 0, 0)); }
                    if (tempNodeCont.nodeType == NodeType.TurnRight1x1 && tempNodeCont.nodeType == nodeCont.nodeType) //RIGHT: If node is turning in same direction of diag...
                    { diagSpace = GetNodeRelative(trackCont.loadNodes, tempNodeCont.gameObject, new Vector3Int(-1, 0, 0)); }
                    if (diagSpace != null) { if (diagSpace.GetComponent<LoadNodeMaster>().nodeType != NodeType.Blank1x1) { break; } } //If segment would not FIT, break
                    //Confirm Continuation of Diagonal:
                    if (diagSpace != null) { subDiagonondes.Add(diagSpace); } //Also add diagSpace if necessary
                    subDiagonondes.Add(tempNodeCont.gameObject); //Add current node to main node's list of subDiagonodes
                    diagChain++; //Increment DiagChain
                }
            }
            //Assign DiagChain:
            if (diagChain > 2 && diagChain % 2 == 0) //DiagMark even diagChains, but use the next node up to actually assign the Diagonode
            {
                diagTotal += 1; //Log as an addition to diagChain next node (for more accurate desirability settings)
            }
            else if (diagChain > 2 && diagChain <= tempCont.maxDiagonalThreshold) //Diagonals only count if they consist of 3 or more track parts (must be odd), and are not disqualifyingly large
            {
                Debug.Log("TrackGenerator: Diagonode location found");
                diagonodes.Add(nodeCont.gameObject); //Move current node to special list of valid potential diagonal targets
                nodeCont.diagMark = diagChain; //Assign diagChain to node at beginning of diagonal (which will eventually be replaced with diagonal node)
                nodeCont.subDiagonodes = subDiagonondes; //Send subNodes to Diagonode (for later destruction)
                x += diagChain - 1; //Skip all nodes already covered by check, so that the only Diag Marks are at the beginning of diagonals
                diagTotal += diagChain; //Add diagChain to total
            }
        }

        //COMMIT DATA:
        trackCont.diagonodes = diagonodes; //Send diagonodes
        trackCont.diagonalTotal = diagTotal; //Send diag total
    }

    private void CheckTight180s(GameObject currentTrack)
    {
        //CHECK TIGHT180s (Christian):
        /*
         *  Function: Parses through current track and fills in Tight180 information for each node
         */

        //INITIALIZE Variables:
        TrackMaster trackCont = currentTrack.GetComponent<TrackMaster>(); //Initialize shorthand for desired track's controller
        int tight180Total = 0; //The total number of tight180s found in current track

        //FIND Tight180s:
        for (int x = 0; x < objectTimeline.Count; x++) //Parse through objectTimeline (in direction of flow) and look for tight180s
        {
            LoadNodeMaster nodeCont =  objectTimeline[x].GetComponent<LoadNodeMaster>(); //Initialize shorthand for loadNode controller
            if (nodeCont.nodeType == NodeType.TurnLeft1x1 || nodeCont.nodeType == NodeType.TurnRight1x1) //Node must be a turn to be a tight180
            {
                if (nodeCont.nodeType == nodeCont.outflowNode.GetComponent<LoadNodeMaster>().nodeType) //If node is a turn and the next node is a turn in the same direction...
                {
                    nodeCont.isTight180 = true; //Tell node that it is a tight180
                    tight180Total++; //Increment total
                }
            }
        }

        //COMMIT DATA:
        trackCont.tight180Total = tight180Total; //Send tight180 total
    }

    private void AssignDiagonode(GameObject originode)
    {
        //ASSIGN DIAGONODE (Christian):
        /*
         *  Function: Re-assigns an existing LoadNode with the designation "Diagonode", which is a special irregularly-sized NodeType used to smooth out annoying wiggles in the track. 
         *            This function also removes all nodes which would be covered by new Diagonode sprite/flow, and stitches flow timeline back together as needed.
         */

        Debug.Log("TrackGenerator: Assigning Diagonode");

        //Initialize Variables:
        LoadNodeMaster nodeCont = originode.GetComponent<LoadNodeMaster>(); //Initialize shorthand for diagonode's controller
        //Label Diagonode:
        if (nodeCont.nodeType == NodeType.TurnLeft1x1) { //Types of LEFT-facing Diagonode:
            switch (nodeCont.diagMark) {
                case 3:
                    //nodeCont.nodeType = NodeType.DiagonalLeft3; //DiagonalLeft3 is deprecated because it is still to short to comfortably be a diagonal, but too long to be left a wiggle
                    nodeCont.nodeType = NodeType.TurnLeft2x2; //Set node type
                    nodeCont.GetComponent<SpriteRenderer>().sprite = nodeSprites[5]; //Set node sprite
                    break;
                case 5:
                    nodeCont.nodeType = NodeType.DiagonalLeft5; //Set node type
                    nodeCont.GetComponent<SpriteRenderer>().sprite = nodeSprites[7]; //Set node sprite
                    break;
                case 7:
                    nodeCont.nodeType = NodeType.DiagonalLeft7; //Set node type
                    nodeCont.GetComponent<SpriteRenderer>().sprite = nodeSprites[9]; //Set node sprite
                    break;
                default:
                    Debug.Log("TrackGenerator: Error! Attempted to process a Diagonode with unsupported length");
                    break;
            }
        }
        else if (nodeCont.nodeType == NodeType.TurnRight1x1) { //Types of RIGHT-facing Diagonode:
            switch (nodeCont.diagMark)
            {
                case 3:
                    //nodeCont.nodeType = NodeType.DiagonalRight3; //DiagonalRight3 is deprecated because it is still to short to comfortably be a diagonal, but too long to be left a wiggle
                    nodeCont.nodeType = NodeType.TurnRight2x2; //Set node type
                    nodeCont.GetComponent<SpriteRenderer>().sprite = nodeSprites[6]; //Set node sprite
                    break;
                case 5:
                    nodeCont.nodeType = NodeType.DiagonalRight5; //Set node type
                    nodeCont.GetComponent<SpriteRenderer>().sprite = nodeSprites[8]; //Set node sprite
                    break;
                case 7:
                    nodeCont.nodeType = NodeType.DiagonalRight7; //Set node type
                    nodeCont.GetComponent<SpriteRenderer>().sprite = nodeSprites[10]; //Set node sprite
                    break;
                default:
                    //Debug.Log("TrackGenerator: Error! Attempted to process a Diagonode with unsupported length");
                    break;
            }
        }
        //Re-Stitch Track Flow:
        nodeCont.outflowNode = nodeCont.subDiagonodes[nodeCont.subDiagonodes.Count - 1].GetComponent<LoadNodeMaster>().outflowNode; //Set diagonode outflow to that of the last node in its subDiagonode chain
        nodeCont.outflowNode.GetComponent<LoadNodeMaster>().inflowNode = nodeCont.gameObject; //Update inflow info on next node in new flow timeline
        //Erase SubDiagonodes:
        for (int x = 0; x < nodeCont.subDiagonodes.Count; x++) //Parse through each subDiagonode of current Diagonode
        {
            //int index = 0; for (int y = 0; y < objectTimeline.Count; y++) { if (objectTimeline[y] == nodeCont.subDiagonodes[x]) { index = y; } } //Get index of subDiagonode
            //if (objectTimeline.Count > index) { objectTimeline.RemoveAt(index); } //Remove subDiagonode from objectTimeLine
            //if (flowTimeline.Count > index) { flowTimeline.RemoveAt(index); } //Remove subDiagonode from flowTimeline
            nodeCont.subDiagonodes[x].GetComponent<LoadNodeMaster>().ignoreNode = true; //Make sure node is not used or rendered in future passes
        }
        //Finalize SubDiagonode Deletion:
        nodeCont.subDiagonodes.Clear(); //Clear last references to subDiagonodes after termination to prevent zombie calls
        RefreshTimelines(); //Re-calculate flow and object timelines
    }

    private GameObject FlowTarget(GameObject currentNode, GameObject targetNode, float testRot, bool validate, bool checkAdjacency)
    {
        //FLOW TARGET (Christian):
        /*
         *  Function: Generates a potential flow target node for a given node and rotation.
         *  Functionality: - Can be used to test if a potential node can flow into another. To get actual flow target of an existing space, leave flowRotTest blank
         *                 - Otherwise, this corrects rotations for specific nodes and uses their neighbor variables to find correct neighbor after factored rotation
         *                 - Automatically underflows rotation variables so you don't have to worry about them
         *                 - Basically tests the viability of a suggested track position
         *                 - If validate is left unchecked, flowTarget will just find exact flow target regardless of normal validation checks
         *  NOTE: This function is VERY delicate, and is designed very specifically to work as a checking system for the Pass1 track generation method. Although
         *        it SHOULD work for other purposes (trackflow validity checks and assignments from other routines), it may not for any number of reasons. This
         *        CANNOT be edited to suit other needs, as it is the foundation of the procedural track generation system
         *  ADD: -Functionality for selecting multiple flow targets or applying rotations to more complex neighbor groups
         */

        //Initialize Variables:
        GameObject newFlowTarget = null; //Initialize variable to return
        LoadNodeMaster nodeCont = currentNode.GetComponent<LoadNodeMaster>(); //Get node controller
        float flowRot = nodeCont.nodeRotation + testRot; //Get flow rotation from node in question
        float endRot = 0; if (looping == true) { endRot = startNode.GetComponent<LoadNodeMaster>().nodeInflowRot; } //Get simplified variable containing the rotation of track end node
                                          else { endRot = finishNode.GetComponent<LoadNodeMaster>().nodeInflowRot; }

        //Overflow/Undeflow Rotation:
        while (flowRot >= 360) { flowRot -= 360; } //Overflow
        while (flowRot < 0)    { flowRot += 360; } //Underflow

        //Check Direction and decide neighbor relevant to flow:
        if (flowRot == 0) //Flow target is northern neighbor
        {
            if (nodeCont.northNode != null) { newFlowTarget = nodeCont.northNode; }
            else { Debug.Log("TrackGenerator: FlowDirectionFinder: Hit obstacle, redirecting..."); return null; }
        }
        else if (flowRot == 90) //Flow target is western neighbor
        {
            if (nodeCont.westNode != null) { newFlowTarget = nodeCont.westNode; }
            else { Debug.Log("TrackGenerator: FlowDirectionFinder: Hit obstacle, redirecting..."); return null; }
        }
        else if (flowRot == 180) //Flow target is southern neighbor
        {
            if (nodeCont.southNode != null) { newFlowTarget = nodeCont.southNode; }
            else { Debug.Log("TrackGenerator: FlowDirectionFinder: Hit obstacle, redirecting..."); return null; }
        }
        else if (flowRot == 270) //Flow target is eastern neighbor
        {
            if (nodeCont.eastNode != null) { newFlowTarget = nodeCont.eastNode; }
            else { Debug.Log("TrackGenerator: FlowDirectionFinder: Hit obstacle, redirecting..."); return null; }
        }
        else //Flow rotation does not have a match in this method
        {
            Debug.Log("TrackGenerator: FlowDirectionFinder: Flow direction is irregular");
            return null; //Return null gameObject
        }

        if (validate == true && forceTrack == null) //Check if target can accept flow from given space (as long as another track hasn't already been forced):
        {
            //Check For Pass Completion:
            if (flowRot == endRot && currentNode == autoCompleteNode) //If a track segment is available which would finish the pass, force it through
            {
                Debug.Log("TrackGenerator: FlowDirectionFinder: Forcing final track");
                forceTrack = newFlowTarget; //Set track to force
                passComplete = true; //Forcing track should cause pass completion
                return newFlowTarget; //Immediately return track-completing flow target
            }

            //Check For Flow Viability:
            if (newFlowTarget == null) { return null; } //If newFlowTarget is outside the bounds of node matrix (redundant)
            if (newFlowTarget.GetComponent<LoadNodeMaster>().nodeType != NodeType.Blank1x1) //If target is not a blank...
            {
                return null; //Return null gameObject
            }
            if (checkAdjacency == true) //If adjacency pulse is being calculated...
            {
                if (PathToTarget(newFlowTarget, targetNode) == false) //If position of newFlowTarget would prevent track from reaching target
                {
                    Debug.Log("TrackGenerator: FlowDirectionFinder: Avoiding dead end, redirecting...");
                    return null; //Return null gameObject
                }
            }
            else if (flowRot != endRot && targetNode == null) //If track generator is ignoring adjacency test to complete track, make sure generator chooses track facing in the correct direction
            {
                //Note: TargetNode loop parameter is arbitrary, used instead of another parameter because this is mutually exclusive with targetNode
                Debug.Log("TrackGenerator: FlowDirectionFinder: Avoiding false positive due to lack of adjacency pulse...");
                return null; //Return null gameObject
            }
        } else if (forceTrack != null) { Debug.Log("TrackGenerator: FlowDirectionFinder: Track ignored"); return null; } //ForceTrack overrides all subsequent validation requests until resolved

        return newFlowTarget; //Return flowTarget
    }

    private bool PathToTarget(GameObject pulseNode, GameObject target)
    {
        //PATH TO FINISH (Christian):
        /*
         *  Function: Sends an adjacency pulse from given blank node, checking if there is any possible path to target node from there. Returns true if so. Designed as
         *            a supplement to the validity check system contained within FlowTarget
         *  Functionality: - Prevents track from looping in on itself or getting stuck on dead ends
         *                 - Populates a list of all blank tangentially adjacent to given node
         *                 - Counts potential node as obstacle when making adjacency calculation
         *                 - Colors all nodes checked with red, to provide a visual aid for user. Changes color back to normal each time it is called
         */
        
        //Initialize Variables:
        LoadNodeMaster nodeCont = pulseNode.GetComponent<LoadNodeMaster>(); //Shorten pulseNode controller address
        bool pathAvailable = false; //Final variable to return. If available path is found, this is pulled true.
        bool pulseFinished = false; //Used to end the while loop which continuously runs adjacency pulse
        List<GameObject> adjacents = new List<GameObject>(); //Initialize list of GameObjects to be populated by adjacency pulse
        List<GameObject> checkedAdjacents = new List<GameObject>(); //Initialize list of GameObjects scanned by adjacency pulse and stored to prevent double-checking

        //RESET SPRITE COLORS:
        if (showVisuals == true)
        {
            for (int x = loadNodes.Length; x > 0; x--)
            {
                for (int y = loadNodes[x - 1].Length; y > 0; y--)
                {
                    for (int z = loadNodes[x - 1][y - 1].Length; z > 0; z--)
                    {
                        loadNodes[x - 1][y - 1][z - 1].GetComponent<SpriteRenderer>().color = Color.white; //Reset tint to white
                    }
                }
            }
        }

        //ADJACENCY PULSE:
        CollectAdjacents(nodeCont); //Initiate pulse with starting adjacents collected from pulseNode neighborhood
        while (pulseFinished == false) //Pulse runs until it either finds path to finish or runs into corner
        {
            if (adjacents.Count == 0) { pathAvailable = false; pulseFinished = true; } //If adjacency pulse has run out of possible nodes before it finds finish, end loop and return false
            else //Parse and grow list of adjacents using neighborhoods
            {
                CollectAdjacents(adjacents[0].GetComponent<LoadNodeMaster>()); //Collect and store adjacents from oldest item on adjacents list
                if (showVisuals == true) adjacents[0].GetComponent<SpriteRenderer>().color = Color.red; //Paint adjacents red for visualization purposes
                checkedAdjacents.Add(adjacents[0]); adjacents.Remove(adjacents[0]); //Once collected from, move gameObject to other list so it won't be used again
            }
        }

        void CollectAdjacents(LoadNodeMaster nCont)
        {
            //Method Function: Used exclusively by PathToFinish to populate adjacency list
            //Functionality: - Collects all non-void, non-track, non-current neighbors and adds them to list

            List<GameObject> neighborHood = new List<GameObject>(); //Initialize empty list to put all potential neighbors in
            if (nCont.northNode != null) { neighborHood.Add(nCont.northNode); } //Add Northern neighbor
            if (nCont.eastNode != null) { neighborHood.Add(nCont.eastNode); } //Add Eastern neighbor
            if (nCont.southNode != null) { neighborHood.Add(nCont.southNode); } //Add Southern neighbor
            if (nCont.westNode != null) { neighborHood.Add(nCont.westNode); } //Add Western neighbor
                //neighborHood.Add(nCont.overNode); //Add Upper neighbor
                //neighborHood.Add(nCont.underNode); //Add Lower neighbor
                                                                    
            for (int x = neighborHood.Count; x > 0; x--) //Parse through list, removing 
            {
                GameObject neighbor = neighborHood[x - 1]; //Initialize shorter variable name for neighbors
                if (neighbor == target) //SUCCESS if target node is tangential neighbor
                {
                    pathAvailable = true; //Tell program a path to finish has been found
                    pulseFinished = true; //Pulse may now end
                    Debug.Log("TrackGenerator: AdjacencyPulse: Target Found!");
                }
                else if (neighbor == currentNode) { neighborHood.Remove(neighbor); } //REMOVE if neighbor is currentNode (simulate node blocking future path)
                else if (neighbor.GetComponent<LoadNodeMaster>().nodeType != NodeType.Blank1x1) { neighborHood.Remove(neighbor); } //REMOVE if neighbor is null or occupied by track part
                else if (adjacents.Contains(neighbor) == true) { neighborHood.Remove(neighbor); } //REMOVE if neighbor has already been added to adjacents list or...
                else if (checkedAdjacents.Contains(neighbor) == true) { neighborHood.Remove(neighbor); } //REMOVE if neighbor has already been checked by adjacency pulse...
                else { adjacents.Add(neighbor); } //KEEP if neighbor is valid and new
            }
        }

        Debug.Log("TrackGenerator: AdjacencyPulse: Adjacents checked = " + checkedAdjacents.Count);
        return pathAvailable; //Return result of pulse
    }

    public GameObject GetNodeRelative(GameObject[][][] nodeMatrix, GameObject relativeNode, Vector3Int relativeCoordinates)
    {
        //Finds the node relative to the given node according to the given coordinates on a theoretical coordinate grid based on the given node's current rotation
        //Used primarily in Pass#2 to confirm the compatibility of advanced node types with the blank spaces they occupy in addition to their track flow

        //Validation Gates:
        if (relativeNode == null)
            { Debug.Log("TrackGenerator: Error! GetNodeRelative request denied. Reason: null node given"); return null; }                    //Return error if null node is given
        if (relativeNode.GetComponent<LoadNodeMaster>() == null)
            { Debug.Log("TrackGenerator: Error! GetNodeRelative request denied. Reason: given object has no node component"); return null; } //Return error if object given is not a node

        //Initialize/Get Variables:
        LoadNodeMaster relNodeCont = relativeNode.GetComponent<LoadNodeMaster>(); //Get given node controller
        Vector3Int actualCoordinates = relNodeCont.nodePosition; //Get actual coordinates of given node
        Vector3Int actualFoundCoordinates = Vector3Int.one; //Initialize container for actual coordinates of found node
        GameObject foundNode = null; //Initialize container for found node

        //Overflow/Undeflow Node Rotation:
        while ( relNodeCont.nodeRotation >= 360) { relNodeCont.nodeRotation -= 360; } //Overflow
        while ( relNodeCont.nodeRotation < 0) { relNodeCont.nodeRotation += 360; } //Underflow

        //Relative Coordinate Calculation:
            //NOTE: When assuming relative coordinate grid, this method places the given node at (0,0) on grid
        if (relNodeCont.nodeRotation == 0)   //If node is facing NORTHWARDS:
            { actualFoundCoordinates = new Vector3Int(actualCoordinates.x + relativeCoordinates.x, actualCoordinates.y + relativeCoordinates.y, actualCoordinates.z); }
        if (relNodeCont.nodeRotation == 90)  //If node is facing EASTWARDS:
            { actualFoundCoordinates = new Vector3Int(actualCoordinates.x - relativeCoordinates.y, actualCoordinates.y + relativeCoordinates.x, actualCoordinates.z); }
        if (relNodeCont.nodeRotation == 180) //If node is facing SOUTHWARDS:
            { actualFoundCoordinates = new Vector3Int(actualCoordinates.x - relativeCoordinates.x, actualCoordinates.y - relativeCoordinates.y, actualCoordinates.z); }
        if (relNodeCont.nodeRotation == 270) //If node is facing WESTWARDS:
            { actualFoundCoordinates = new Vector3Int(actualCoordinates.x + relativeCoordinates.y, actualCoordinates.y - relativeCoordinates.x, actualCoordinates.z); }

        //Return Results:
        Debug.Log("TrackGenerator: Finding point (" + actualFoundCoordinates.x + "," + actualFoundCoordinates.y + "," + actualFoundCoordinates.z + ") on given matrix");
        foundNode = GetNodeFromCoordinates(nodeMatrix, actualFoundCoordinates); //Find actual node at determined coordinates
        return foundNode; //Return found node
    }

    public GameObject GetNodeFromCoordinates(GameObject[][][] nodeMatrix, Vector3Int coordinates)
    {
        //Takes the given coordinates (as long as they correspond to an existing node) and returns the node they represent

        GameObject foundNode = null; //Initialize found node as null
        if (nodeMatrix[coordinates.x][coordinates.y][coordinates.z] != null) //If node at coordinates exists...
            { foundNode = nodeMatrix[coordinates.x][coordinates.y][coordinates.z]; } //Set found node to node at given coordinates
        return foundNode; //Return result
    }

    private GameObject PackageNodes()
    {
        //Creates a track GameObject and places all loadNodes in that package. then sends data to track data script and returns finished package

        //CREATE Package and Send Nodes:
            GameObject newGen = Instantiate(trackPrefab); //Instantiate a new track gameObject and child it to this TrackGenerator
            TrackMaster trackData = newGen.GetComponent<TrackMaster>(); //Initialize temporary storage for track controller
            for (int x = loadNodes.Length; x > 0; x--) //Child each loadNode to new track container
            { for (int y = loadNodes[x - 1].Length; y > 0; y--)
                { for (int z = loadNodes[x - 1][y - 1].Length; z > 0; z--)
                    { if (loadNodes[x - 1][y - 1][z - 1] != null) //Only consider existing nodes
                        {  loadNodes[x - 1][y - 1][z - 1].transform.SetParent(newGen.transform); }}}} //Child each node to new track container
            trackData.SendLoadNodes(loadNodes); //Send full matrix of node positions to track
        //SEND Generation Data to Package:
            newGen.name = "Generation" + genAttemptsMade + "_Parts=" + partsPlaced; //Pick fitting name for generation
            trackData.trackTotal = partsPlaced;                                     //Log parts placed during generation
            trackData.generationTime = (Time.realtimeSinceStartup - genBeginTime);  //Log time taken to generate
            trackData.genBeginTime = genBeginTime;                                  //Log generation start time
            trackData.startNode = startNode;                                        //Log node track starts at
            trackData.flowTimeline = flowTimeline;                                  //Log track flow timeline
            trackData.objectTimeline = objectTimeline;                              //Log track object timeline

        return newGen; //Return newly-generated package
    }

    private void DeleteNodes(GameObject[][][] doomedNodes)
    {
        //Deletes and clears all LoadNodes currently being stored/processed IN TrackGenerator

        GameObject newGen = Instantiate(trackPrefab, transform); //Instantiate a new track gameObject and child it to this TrackGenerator
        for (int x = doomedNodes.Length; x > 0; x--) //Child each loadNode to new track container
        { for (int y = doomedNodes[x - 1].Length; y > 0; y--)
            { for (int z = doomedNodes[x - 1][y - 1].Length; z > 0; z--)
                { if (doomedNodes[x - 1][y - 1][z - 1] != null) //Only consider existing nodes
                    { doomedNodes[x - 1][y - 1][z - 1].transform.SetParent(newGen.transform); }}}} //Child each node to new track container
        Destroy(newGen);
    }

    private void GetTemplateInfo() //Takes data from template and applies it to this track generator's systems
    {
        //General Initializations:
        tempCont = template.GetComponent<TrackTemplate>(); //Get track template controller

        //Assign Basic Track Generation Inputs:
        resolution = tempCont.resolution;         //Get template resolution
        if (tempCont.customShape == true) //If user has indicated that template uses custom shape...
        {
            //Translate LoadNodeMatrix size variables to standard loadNode info for Track Generator:
            columns = tempCont.loadNodePositions.rowSize; //Size of rows = # of columns
            rows = tempCont.loadNodePositions.columnSize; //Size of columns = # of rows
            depth = 1; //TEMP: Default depth
        }
        else //If template is using basic track generation inputs...
        {
            columns = tempCont.columns;               //Get template columns
            rows = tempCont.rows;                     //Get template rows
            depth = tempCont.depth;                   //Get template depth
        }
        start = tempCont.start;                   //Get template start
        finish = tempCont.finish;                 //Get template finish
        startRotation = tempCont.startRotation;   //Get template start rotation
        finishRotation = tempCont.finishRotation; //Get template finish rotation
        //Assign Track Flow Variables (subject to mid-pass change):
        weightGranularity = tempCont.weightGranularity;   //Get weight granularity
        generationAttempts = tempCont.generationAttempts; //Get generation attempts
        failureThreshold = tempCont.failureThreshold;     //Get failure threshold
    }

    private void GetTemplateWeight()
    {
        //Used to retrieve correct/current Pass1 track weight from template, may depend on adaptive weight segments and current pass number

        if (tempCont.adaptiveWeightSegments.Count == 0 || tempCont.adaptiveWeightSegments.Count <= tempCont.currentAWSegment) //If template has no adaptive weight or has finished all segments, cancel this check
        {
            straightWeight = tempCont.straightWeight; //Get straight weight
            leftWeight = tempCont.leftWeight;         //Get left weight
            rightWeight = tempCont.rightWeight;       //Get right weight
            return;
        }

        //CALCULATE real combined duration of current segment and all segments before it
            int realAWSrawDuration = tempCont.adaptiveWeightSegments[tempCont.currentAWSegment].rawDuration; //Initialize variable for storing combined AWS raw duration
            for (int x = 1; x <= tempCont.currentAWSegment; x++) //Parse through segments after first but before current
            {
                realAWSrawDuration += tempCont.adaptiveWeightSegments[x].rawDuration;
            }
        //CHECK for end of segment
            if (partsPlaced >= realAWSrawDuration && //If segment minimum track duration has been exceeded...
                partsPlaced >= (tempCont.adaptiveWeightSegments[tempCont.currentAWSegment].percentDuration * targetTrackNumber)) //...And segment percent generation has been exceeded...
            {
                //End of segment, switch to new segment or default weight values
                tempCont.currentAWSegment++; //Increment current segment tracker
     
                if (tempCont.adaptiveWeightSegments.Count <= tempCont.currentAWSegment) //If segment tracker has reached end of adaptive segment list...
                {
                    //Begin using default weight values:
                    straightWeight = tempCont.straightWeight;       //Get straight weight
                    leftWeight = tempCont.leftWeight;               //Get left weight
                    rightWeight = tempCont.rightWeight;             //Get right weight
                    Debug.Log("TrackGenerator: Finished Adaptive Weight Segment list, switching to default template track weight values...");
                    return; //Break from function
                }
            }

        //GET values from current AWS:
            straightWeight = tempCont.adaptiveWeightSegments[tempCont.currentAWSegment].straightWeight; //Get current segment's straight weight
            leftWeight = tempCont.adaptiveWeightSegments[tempCont.currentAWSegment].leftWeight;         //Get current segment's left weight
            rightWeight = tempCont.adaptiveWeightSegments[tempCont.currentAWSegment].rightWeight;       //Get current segment's right weight
        //LOG template weight use:
            Debug.Log("TrackGenerator: Using AWS #" + tempCont.currentAWSegment + " for track weight values");
    }

    private GameObject GetTrackByName(GameObject[] prefabList, string name)
    {
        //  METHOD: Gets a track from template's list of Final Track Parts, according to the unifying name marker (common thread between LoadNodes and FTPs)
        /*  PARAMS: -PREFABLIST: The list of FTP gameObjects to draw from when checking for track
         *          -NAME: This is the shorthand string name associated with the desired FTP (as a suffix 1-word tag, i.e. "TurnRight1x1")
         */

        for (int x = 0; x < prefabList.Length; x++) //Parse through FTP list and look for name matches
        {
            if (prefabList[x].name.Contains(name)) //If a list item's name contains the desired string...
            {
                Debug.Log("TrackGenerator: Found Final Track Part of type '" + name + "'");
                return prefabList[x]; //Return found FTP
            }
        }
        Debug.Log("TrackGenerator: Could not find Final Track Part of type '" + name + "'");
        return null; //Return null if no matching track could be found
    }

    private GameObject GetTrackByName(GameObject[] prefabList, NodeType name)
    {
        //  METHOD: Overload: Gets a track from template's list of Final Track Parts, according to the unifying name marker. This version is based specifically on a LoadNode identifier
        /*  PARAMS: -PREFABLIST: The list of FTP gameObjects to draw from when checking for track
         *          -NAME: The LoadNode type corresponding to the desired type of FTP
         */

        for (int x = 0; x < prefabList.Length; x++) //Parse through FTP list and look for name matches
        {
            if (prefabList[x].name.Contains(name.ToString())) //If a list item's name contains the desired string (associated directly with name of LoadNode type)...
            {
                Debug.Log("TrackGenerator: Found Final Track Part of type '" + name.ToString() + "'");
                return prefabList[x]; //Return found FTP
            }
        }
        Debug.Log("TrackGenerator: Could not find Final Track Part of type '" + name.ToString() + "'");
        return null; //Return null if no matching track could be found
    }

    private void RefreshTimelines()
    {
        //  METHOD: Parses back through flow and object timeline based on actual node relationships in track (provided Start node still exists).
        /*
         */

        //INITIALIZE Variables:
        List<NodeType> newFlowTimeline = new List<NodeType>(); //Initialize container for new flow timeline
        List<GameObject> newObjectTimeline = new List<GameObject>(); //Initialize container for new object timeline
        LoadNodeMaster currentNodeCont = objectTimeline[0].GetComponent<LoadNodeMaster>(); //Start at the beginning of old object timeline
        bool finished = false; //Initialize variable for breaking out of while loop when done

        //REFRESH Timelines
        while (finished == false)
        {
            if (newObjectTimeline.Contains(currentNodeCont.gameObject)) //End loop once flow has looped around
                { finished = true; }
            else
            {
                newObjectTimeline.Add(currentNodeCont.gameObject); //Add object to objectTimeline
                newFlowTimeline.Add(currentNodeCont.nodeType); //Add NodeType to flow timeline
                currentNodeCont = currentNodeCont.outflowNode.GetComponent<LoadNodeMaster>(); //Move counter to next node in flow
            }
        }

        //CLEAR Junk:
        int oldTimelineCount = objectTimeline.Count; //Initialize constant variable for length of parse
        for (int x = 0; x < oldTimelineCount; x++) //Parse through old object timeline, looking for inconsistencies
        {
            if (newObjectTimeline.Contains(objectTimeline[x]) == false) //If node was ignored in creation of new timeline...
                { Destroy(objectTimeline[x]); }
        }

        //COMMIT Timelines:
        objectTimeline = newObjectTimeline;
        flowTimeline = newFlowTimeline;
    }

    private void SetFlowPoints(GameObject track)
    {
        //Initialize Variables:
        TrackMaster tm = track.GetComponent<TrackMaster>();

        //Get Flow Points:
        for (int x = 0; x < tm.objectTimeline.Count; x++) //Parse through object timeline
        {
            TrackPartMaster pm = objectTimeline[x].GetComponent<TrackPartMaster>();
            for (int y = 1; y < pm.flowPoints.Length; y++) //Parse through flowpoints in track (disregarding final outflow point)
                { tm.flowPoints.Add(pm.flowPoints[y]); } //Add each flow point to timeline
        }
    }
}

/* -------------------------------------------------------------<<<COLD STORAGE>>>----------------------------------------------------------------------------------------------------------------
 * 
 * private void SetFlowPoints(GameObject node, GameObject FTP)
    {
        //  METHOD: Sets the flow points of a loadNode (a vector3 relative to the node in the center of each side interacting with flow)
        /*  PARAMS: -NODE: The original node to find the flow points of (must have a LoadNodeMaster attached)
         *          -FTP (Final Track Part): The part to set the flow points on (must have a TrackPartMaster attached)
         *
        
        //INITIALIZE Variables:
            LoadNodeMaster nm = node.GetComponent<LoadNodeMaster>(); //Get shorthand for node's master
            TrackPartMaster tm = FTP.GetComponent<TrackPartMaster>(); //Get shorthand for track part's master
            float inFRot = nm.nodeInflowRot; //Get shorthand for node inflow rotation
            float outFRot = nm.nodeOutflowRot; //Get shorthand for node outflow rotation
            Vector3 normInFlowPoint = Vector3.zero; //Initialize position of normalized inflow point
            Vector3 normOutFlowPoint = Vector3.zero; //Initialize position of normalized outflow point
            //Overflow/Undeflow Rotation:
                while (inFRot >= 360) { inFRot -= 360; } //Overflow inflow
                while (inFRot < 0) { inFRot += 360; } //Underflow inflow

                while (outFRot >= 360) { outFRot -= 360; } //Overflow outflow
                while (outFRot < 0) { outFRot += 360; } //Underflow outflow
                //Debug.Log("InFlowRot = " + inFRot);
                //Debug.Log("OutFlowRot = " + outFRot);
        //FIND Normalized Points:
            //INFLOW:
                if (inFRot == 0)   //If inflow is NORTHWARDS:
                    { normInFlowPoint = new Vector3(0, -1, 0); }
                if (inFRot == 90)  //If inflow is EASTWARDS:
                    { normInFlowPoint = new Vector3(1, 0, 0); }
                if (inFRot == 180) //If inflow is SOUTHWARDS:
                    { normInFlowPoint = new Vector3(0, 1, 0); }
                if (inFRot == 270) //If inflow is WESTWARDS:
                    { normInFlowPoint = new Vector3(-1, 0, 0); }
            //OUTFLOW:
                if (outFRot == 0)   //If inflow is NORTHWARDS:
                    { normOutFlowPoint = new Vector3(0, 1, 0); }
                if (outFRot == 90)  //If inflow is EASTWARDS:
                    { normOutFlowPoint = new Vector3(-1, 0, 0); }
                if (outFRot == 180) //If inflow is SOUTHWARDS:
                    { normOutFlowPoint = new Vector3(0, -1, 0); }
                if (outFRot == 270) //If inflow is WESTWARDS:
                    { normOutFlowPoint = new Vector3(1, 0, 0); }
        //FIND Relative Points:
            Vector3 realInFlowPoint = (normInFlowPoint * (tm.resolution / 100) + FTP.transform.position); //Get scaled-up real inflow point in world space
            Vector3 realOutFlowPoint = (normOutFlowPoint * (tm.resolution / 100) + FTP.transform.position); //Get scaled-up real outflow point in world space
        //SET Flow Points:
            tm.normInFlow = normInFlowPoint; //Log normalized inflow point in real space (constant)
            tm.normOutFlow = normOutFlowPoint; //Log normalized outflow point in real space (constant)
            tm.inFlowPos = realInFlowPoint; //Log inflow point in real space (at time of assignment)
            tm.outFlowPos = realOutFlowPoint; //Log inflow point in real space (at time of assignment)
    }

    private void SetFlowPoints(GameObject FTP)
    {
        //  METHOD: (Overload): Sets the flow points of an FTP based on baked-in norm data, used after changing position of a track part to keep flow correct in world space. Note: can only
        /*                      be called after original method has set normalized flow points.
         *  PARAMS: -FTP (Final Track Part): The part to set the flow points on (must have a TrackPartMaster attached)
         *
        
        //INITIALIZE and CALCULATE Flow Points:
        TrackPartMaster tm = FTP.GetComponent<TrackPartMaster>(); //Get shorthand for track part's master
        Vector3 realInFlowPoint = (tm.normInFlow * (tm.resolution / 100) + FTP.transform.position); //Get scaled-up real inflow point in world space
        Vector3 realOutFlowPoint = (tm.normOutFlow * (tm.resolution / 100) + FTP.transform.position); //Get scaled-up real outflow point in world space
        //SET Flow Points:
        tm.inFlowPos = realInFlowPoint; //Log inflow point in real space (at time of assignment)
        tm.outFlowPos = realOutFlowPoint; //Log inflow point in real space (at time of assignment)
    }
 * 
 * 
 * //Build DiagChain:
                        diagChain++; //Increment consecutive turn count
                        while (tempNodeCont.nodeType == NodeType.TurnLeft1x1 || tempNodeCont.nodeType == NodeType.TurnRight1x1) //Run this loop for the entirety of a turn chain...
                        {
                            LoadNodeMaster outfNodeCont = tempNodeCont.outflowNode.GetComponent<LoadNodeMaster>(); //Initialize shorthand for current node's inflow node's controller
                            if (outfNodeCont.nodeType == NodeType.TurnLeft1x1 && outfNodeCont.nodeType != tempNodeCont.nodeType || //If previous node was also a turn and in the alternate direction...
                                outfNodeCont.nodeType == NodeType.TurnRight1x1 && outfNodeCont.nodeType != tempNodeCont.nodeType)  //If previous node was also a turn and in the alternate direction...
                            {
                                //Check If Diagonal Will Fit:
                                if (outfNodeCont.nodeType == nodeCont.nodeType) //If this part of diag matches original turn direction...
                                {
                                    //Initialize Diag Space Container:
                                    GameObject diagSpace = null; //Initialize container
                                    //Get Diag Space (space behind the turn of a diag required to be empty for diagonal to fit):
                                    if (nodeCont.nodeType == NodeType.TurnLeft1x1) { diagSpace = GetNodeRelative(trackCont.loadNodes, outfNodeCont.gameObject, new Vector3Int(-1, 0, 0)); }
                                    else                                           { diagSpace = GetNodeRelative(trackCont.loadNodes, outfNodeCont.gameObject, new Vector3Int(1, 0, 0)); }
                                    //Check if Diag Space is Compatible:
                                    if (diagSpace != null && diagSpace.GetComponent<LoadNodeMaster>().nodeType != NodeType.Blank1x1) //If diagonal placement would obstruct another part of the track...
                                    { break; } //End this diagChain, because continuing it would obstruct another part of the track
                                }
                                //Increment DiagChain:
                                diagChain++; //Increment consecutive turn count
                            }
                            //Move Down DiagChain:
                            tempNodeCont = outfNodeCont; //Move program to next node in sequence
                        }
                        //Assign DiagChain:
                        tempNodeCont.outflowNode.GetComponent<LoadNodeMaster>().diagMark = diagChain; //Set diagMark of last turn in chain
                        x += (diagChain - 1); //Skip already-counted nodes (streamlines check process
 * 
 */
