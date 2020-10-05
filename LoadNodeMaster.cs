using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadNodeMaster : MonoBehaviour
{
    //LOADNODE MASTER:
    /*
     *  Created: 11.02.2019 - 9:29 AM - (Christian)
     *  Edited:  11.02.2019 - ------- - (Christian)
     *           11.23.2019 - ------- - (Christian)
     *  
     *  Overall Purpose: Carries positional data relative to other loadNodes in grid, and about track contents
     */

    //Objects and Components:
    private TrackGenMaster trackGenMaster; //The TrackGenerator script which spawned this node
    //private TrackMaster trackMaster; //The TrackMaster script which holds this node after TrackGenerator exports it

    //Spatial Data (assumes 1x1 nodes, expand later):
        //Contains: Public variables with data about where this node is and which nodes are its immediate neighbors
    public Vector3Int nodePosition; //Where this node is in node matrix
    public float nodeRotation; //What direction this node is facing

    public GameObject northNode; //The node directly above this node
    public GameObject eastNode;  //The node to the right of this node
    public GameObject southNode; //The node directly below this node
    public GameObject westNode;  //The node to the left of this node
    public GameObject overNode;  //The node on top of this node
    public GameObject underNode; //The node underneath this node

    //Track Data:
        //Contains: Internal/private variables with data concerning track flow and more complex information about relationships with other nodes in matrix
        [Space()]
        public TrackGenMaster.NodeType nodeType; //This node's type. This governs track flow, and how this node behaves spatially.
        public float nodeOutflowRot; //What direction this node flows into
        public float nodeInflowRot;  //What direction this node can be flowed into
        public GameObject inflowNode;  //The node this node's track flows in from (track input). Can only be null for Blanks and Start. ADD: Expand to array for multiple inflow points
        public GameObject outflowNode; //The node this node's track flows into (track output). Can only be null for Blanks and Finish. ADD: Expand to array for multiple outflow points
        
        //Special Markers:
        [ShowOnly] public bool ignoreNode; //Indicates that this node, for whatever reason, has been marked to be ignored during the Pass3 generation process
        [ShowOnly] public int diagMark; //Indicates that this is the beginning of a wiggle that is [diagMark] segments long
            internal List<GameObject> subDiagonodes; //The other nodes in this diagonode segment (if it is one, left null if not)
        [ShowOnly] public bool isTight180; //Indicates that this node is the first part of a series of two adjacent turns in the same direction

    public void InitializeNode(Vector3Int nodePos, TrackGenMaster tgm)
    {
        //INITIALIZE NODE (Christian):
        /*
         *  Function: Assigns a LoadNode all the necessary variables upon construction
         *  Implemented Functionalities:
         *      -Logs own position and master script
         *      -Calculates gameObject identities of neighbor nodes in all 3 axes
         */

        //Acquire Spatial Data:
        if (tgm != null) { trackGenMaster = tgm; } //Assign master script during generation
        //ADD: Functionality to make this program self-sufficient from trackGenMaster
        nodePosition = nodePos; //Store node location
        if (nodePos.x < tgm.loadNodes.Length && nodePos.y + 1 < tgm.loadNodes[nodePos.x].Length && nodePos.z < tgm.loadNodes[nodePos.x][nodePos.y].Length) //Check for neighbor
            { northNode = tgm.loadNodes[nodePos.x][nodePos.y + 1][nodePos.z]; } //Get neighbor
        if (nodePos.x < tgm.loadNodes.Length && nodePos.y > 0 && nodePos.z < tgm.loadNodes[nodePos.x][nodePos.y].Length) //Check for neighbor
            { southNode = tgm.loadNodes[nodePos.x][nodePos.y - 1][nodePos.z]; } //Get neighbor
        if (nodePos.x + 1 < tgm.loadNodes.Length && nodePos.y < tgm.loadNodes[nodePos.x].Length && nodePos.z < tgm.loadNodes[nodePos.x][nodePos.y].Length) //Check for neighbor
            { eastNode =  tgm.loadNodes[nodePos.x + 1][nodePos.y][nodePos.z]; } //Get neighbor
        if (nodePos.x > 0 && nodePos.y < tgm.loadNodes[nodePos.x].Length && nodePos.z < tgm.loadNodes[nodePos.x][nodePos.y].Length) //Check for neighbor
            { westNode =  tgm.loadNodes[nodePos.x - 1][nodePos.y][nodePos.z]; } //Get neighbor
        if (nodePos.x < tgm.loadNodes.Length && nodePos.y < tgm.loadNodes[nodePos.x].Length && nodePos.z + 1 < tgm.loadNodes[nodePos.x][nodePos.y].Length) //Check for neighbor
            { overNode =  tgm.loadNodes[nodePos.x][nodePos.y][nodePos.z + 1]; } //Get neighbor
        if (nodePos.x < tgm.loadNodes.Length && nodePos.y < tgm.loadNodes[nodePos.x].Length && nodePos.z - 1 >= 0) //Check for neighbor
            { underNode = tgm.loadNodes[nodePos.x][nodePos.y][nodePos.z - 1]; } //Get neighbor
    }
}
