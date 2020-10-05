using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackPartMaster : MonoBehaviour
{
    //LOADNODE MASTER:
    /*
     *  Created: 11.21.2019 - 3:38 PM - (Christian)
     *  Edited:  11.21.2019 - ------- - (Christian)
     *  
     *  Overall Purpose: Carries inherited information from LoadNode generation process
     */

    //DATA:
        public GameObject[] flowPoints; //Array of flow points in this track part
        internal Vector3 normInFlow; //The relative-corrected normalized inflow position of this track part
        internal Vector3 normOutFlow; //The relative-corrected normalized outflow position of this track part
        //public Vector3 inFlowPos; //The location (in real space, rotation being accounted for) of the inflow edge of this track part
        //public Vector3 outFlowPos; //The location (in real space, rotation being accounted for) of the outflow edge of this track part
        internal float resolution; //The number of pixels across this track part's sprite is
}
