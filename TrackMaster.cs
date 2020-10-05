using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackMaster : MonoBehaviour
{
    //TRACK MASTER:
    /* 
     *  Created: 11.09.2019 - 09:08 PM - (Christian)
     *  Edited:  11.09.2019 - -------- - (Christian)
     *           11.10.2019 - -------- - (Christian)
     * 
     *  Overall Purpose: Contains statistical data about this track, used in candidate selection
     */
    
    //Overall Generation Stats:
    [Header("Generation Stats:")]
        [ShowOnly] public float generationTime; //How long this track took to generate
        [ShowOnly] public float genBeginTime; //Stores what time the generation process of this track began
    [Header("Desirability Stats:")]
        [ShowOnly] public float overallRating; //The overall desirability rating of this track (from 0-1, 1 being the perfect candidate)
        [ShowOnly] public int trackTotal; //How many parts this track has
        [ShowOnly] public int diagonalTotal; //How many segments of this track are involved in potential diagonals
        [ShowOnly] public int tight180Total; //How many instances of a tight 180 turn this track has

    //Flow Timeline:
        internal GameObject[][][] loadNodes; //Full array of all loadNodes contained in this track, detailing where they are
        internal GameObject startNode;          //The node where this map starts
        internal List<TrackGenMaster.NodeType> flowTimeline = new List<TrackGenMaster.NodeType>(); //A list describing the order and type of track parts in this track
        internal List<GameObject> objectTimeline = new List<GameObject>(); //A list containing all track part objects in this track in order
    //Special Data:
        internal List<GameObject> diagonodes = new List<GameObject>(); //List of wonky node clusters marked as potential candidates for DIAGONAL SMOOTHING
        public List<GameObject> flowPoints = new List<GameObject>(); //List of flowpoints in track (not including those which overlap)

    public void SendLoadNodes(GameObject[][][] nodeMatrix)
    {
        //Initialize local loadNode matrix
            loadNodes = new GameObject[nodeMatrix.Length][][]; //Set number of columns
            for (int x = loadNodes.Length; x > 0; x--)
            {
                loadNodes[x - 1] = new GameObject[nodeMatrix[x-1].Length][]; //Set number of rows
                for (int y = loadNodes[x - 1].Length; y > 0; y--)
                {
                    loadNodes[x - 1][y - 1] = new GameObject[nodeMatrix[x-1][y-1].Length]; //Set number of layers
                    for (int z = loadNodes[x - 1][y - 1].Length; z > 0; z--)
                    {
                        loadNodes[x - 1][y - 1][z - 1] = null; //Initialize each node at null to prevent errors
                    }
                }
            }
        //Set loadnodes:
            for (int x = loadNodes.Length; x > 0; x--) //For each column...
            {
                for (int y = loadNodes[x - 1].Length; y > 0; y--) //...for each row in that column...
                {
                    for (int z = loadNodes[x - 1][y - 1].Length; z > 0; z--) //...for each layer at that point...
                    {
                        loadNodes[x - 1][y - 1][z - 1] = nodeMatrix[x - 1][y - 1][z - 1]; //Set each node exactly
                    }
                }
            }
    }
}
