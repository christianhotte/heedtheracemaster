using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleSpawnMaster : MonoBehaviour
{
    //VEHICLE SPAWN MASTER:
    /* 
     *  Created: 12.16.2019 - 06:49 PM - (Christian)
     *  Edited:  12.16.2019 - -------- - (Christian)
     * 
     *  Function: Generates a random chassis in its place with random parts attached on start of scene (should be childed
     *            to vehicle parent object with no Chassis)
     *  
     */

        public bool buildOnStart; //If checked, this AutoSpawner will immediately generate a vehicle on scene loading

    //Objects and Components:
        private CarMaster carMast;          //This vehicle's Car Master
        private PlayerCarMaster playerMast; //IF APPLICABLE: This vehicle's Player controller
        private BrainMaster brainMast;      //IF APPLICABLE: This vehicle's AI Brain controller
        private WeaponController wepMast;   //This vehicle's weapon controller

        public Transform pivot; //The pivot point to assign to chassis upon instantiation

    //Part Pools:
    [Header("Part Pools:")]
        public GameObject[] chassisPool; //Chassis to choose from
        public GameObject[] bodyPool;    //Bodies to choose from
        public GameObject[] enginePool;  //Engines to choose from
        public GameObject[] exhaustPool; //Exhausts to choose from
        public GameObject[] wheelPool;   //Wheels to choose from
        public GameObject[] weaponGPool; //Gimballed weapons to choose from
        public GameObject[] weaponFPool; //Fixed weapons to choose from
    [Header("Allocation Chances")] //How likely it is that a socket of the given pool will be filled with a random part
        [Range(0 , 1)] public float bodyChance; //Chance for body socket to be filled
        [Range(0 , 1)] public float exhaustChance; //Chance for exhaust socket to be filled
        [Range(0 , 1)] public float weaponGChance; //Chance for gimballed weapon socket to be filled
        [Range(0 , 1)] public float weaponFChance; //Chance for fixed weapon socket to be filled

    //Internal Variables:
        private int partChoice; //Multipurpose variable used to randomly select parts from part pools

    private void Start()
    {
        //Get Objects and Components:
            carMast = GetComponentInParent<CarMaster>(); //Get this vehicle's central controller
            wepMast = GetComponentInParent<WeaponController>(); //Get this vehicle's weapon controller
            if (GetComponentInParent<BrainMaster>() != null && GetComponentInParent<BrainMaster>().enabled == true) { brainMast = GetComponentInParent<BrainMaster>(); } //Get this vehicle's AI controller
            if (GetComponentInParent<PlayerCarMaster>() != null && GetComponentInParent<PlayerCarMaster>().enabled == true) { playerMast = GetComponentInParent<PlayerCarMaster>(); } //Get this vehicle's player controller
        //AutoBuild:
            if (buildOnStart == true) { BuildVehicle(); }
    }

    internal void BuildVehicle() //Builds randomized vehicle onto parent controller object, then deletes itself
    {
        //BUILD RANDOMIZED VEHICLE:
        
        //CHASSIS (mandatory x 1):
            //Generate Chassis & Initialize Variables:
            partChoice = Random.Range(0, chassisPool.Length); //Roll for chassis selection
            GameObject chassis = Instantiate(chassisPool[partChoice], transform.parent); //Generate chosen part and child to vehicle's controller
            PartMaster chassisMast = chassis.GetComponent<PartMaster>(); //Get chassis controller
            //Fill Chassis Contingencies:
            pivot.SetParent(chassis.transform);    //Send pivot over to chassis
            carMast.pivot_Point = pivot;           //Inform carMaster of pivot
            carMast.chassis = chassis;             //Inform carMaster of chassis
            if (brainMast != null) //AI VEHICLE ONLY:
            {
            brainMast.chassis = chassis.transform; //Inform brainMaster of chassis
            }
        //PARTS ON CHASSIS (mandatory x [chassisPartSockets]):
            //Generate Other Parts:
            for (int x = 0; x < chassisMast.partSockets.Length; x++) //Parse through all partSockets on Chassis
            {
                //Initialize Variables:
                    PartSocket currentSocket = chassisMast.partSockets[x];   //Get current partSocket
                    PartMaster.PartType type = currentSocket.type;           //Get current partSocket type
                    GameObject newPart = null;                               //Initialize container variable for new part
                    float useSocket = Random.Range(0f, 1f);                  //Roll for socket useage
                //Generate Part:
                    switch (type) //Part generation process differs by type
                    {
                    //WHEELS (mandatory):
                    case PartMaster.PartType.Wheel:
                        partChoice = Random.Range(0, wheelPool.Length); //Roll for wheel selection
                        newPart = Instantiate(wheelPool[partChoice], chassis.transform); //Generate wheel and child to chassis
                        break;
                    //ENGINE (mandatory):
                    case PartMaster.PartType.Engine:
                        partChoice = Random.Range(0, enginePool.Length); //Roll for engine selection
                        newPart = Instantiate(enginePool[partChoice], chassis.transform); //Generate engine and child to chassis
                        break;
                    //EXHAUSTS (chance):
                    case PartMaster.PartType.Exhaust:
                        if (useSocket > exhaustChance) { break; } //Check for random allocation chance
                        partChoice = Random.Range(0, exhaustPool.Length); //Roll for exhaust selection
                        newPart = Instantiate(exhaustPool[partChoice], chassis.transform); //Generate exhaust and child to chassis
                        break;
                    //BODIES (chance):
                    case PartMaster.PartType.Body:
                        if (useSocket > bodyChance) { break; } //Check for random allocation chance
                        partChoice = Random.Range(0, bodyPool.Length); //Roll for body selection
                        newPart = Instantiate(bodyPool[partChoice], chassis.transform); //Generate body and child to chassis
                        PartMaster bodyMast = newPart.GetComponent<PartMaster>(); //Get body's partMaster

                        //PARTS ON BODY (chance):
                        for (int y = 0; y < bodyMast.partSockets.Length; y++) //Parse through all partSockets on Body
                        {
                            PartSocket currentSocket2 = bodyMast.partSockets[y];   //Get current partSocket
                            PartMaster.PartType type2 = currentSocket2.type;       //Get current partSocket type
                            GameObject newPart2 = null;                            //Initialize container variable for new part
                            float useSocket2 = Random.Range(0f, 1f);               //Roll for socket useage
                            switch (type2)
                            {
                            //GIMBALLED WEAPONS (chance):
                            case PartMaster.PartType.WeaponG:
                                if (useSocket2 > weaponGChance) { break; } //Check for random allocation chance
                                partChoice = Random.Range(0, weaponGPool.Length); //Roll for gimballed weapon selection
                                newPart2 = Instantiate(weaponGPool[partChoice], newPart.transform); //Generate gimballed weapon and child to body
                                wepMast.joystickFunctions.Add(newPart2); //Add weapon to vehicle functions list
                                break;
                            //FIXED WEAPONS (chance):
                            case PartMaster.PartType.WeaponF:
                                if (useSocket2 > weaponFChance) { break; } //Check for random allocation chance
                                partChoice = Random.Range(0, weaponFPool.Length); //Roll for fixed weapon selection
                                newPart2 = Instantiate(weaponFPool[partChoice], newPart.transform); //Generate fixed weapon and child to body
                                break;
                            //UNUSED PARTS (no chance):
                            default:
                                break;
                            }
                            //Cleanup:
                                if (newPart2 != null) { bodyMast.AttachPart(newPart2, -1, true); } //Establish contingencies between body and part
                        }
                        break;
                    //UNUSED PARTS (no chance):
                    default:
                        break;
                    }
                //Cleanup:
                    if (newPart != null) { chassisMast.AttachPart(newPart, -1, true); } //Establish contingencies between chassis and part
            }
        //FINAL CLEANUP:
            //Self-Automated One-Time Tasks:
            chassisMast.GetComponent<ChassisMaster>().vehicleSetUp = true; //Bypass vehicle setup phase on chassis
            chassisMast.GetComponent<ChassisMaster>().FindPivotPoint(); //Do FindPivotPoint calculation on completed chassis
            chassisMast.GetPartGroup(true).SortParts(1).NameParts(NameMaster.GenerateRacerName()); //Final cleanup and organization
            //Final Component Activations:
            carMast.InitializeVehicle(); //Initialize CarMaster
            if (playerMast != null) //If vehicle is player-controlled:
            {
                chassisMast.maxHealth += chassisMast.maxHealth; //Double max health
                playerMast.InitializeVehicle(); //Initialize PlayerCarMaster
            }
            else if (brainMast != null) //If vehicle is AI-controlled:
            {
                brainMast.InitializeVehicle(); //Initialize BrainMaster
            } 
            wepMast.InitializeVehicle(); //Initialize WeaponMaster
            if (GameObject.Find("Intro Track Generator") != null) //EXCEP: Add initialization to IntroTrackGenerator
            {
                GameObject.Find("Intro Track Generator").GetComponent<IntroGenerator>().vehiclesInitialized++; //Add to init count
                carMast.is_Accelerating = true; //Begin vehicle acceleration
            }
            //Self-Destruct
            Destroy(gameObject); //Destroy self once finished with task
    }
}
