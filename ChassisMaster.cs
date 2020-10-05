using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChassisMaster : MonoBehaviour
{
    //VEHICLE CHASSIS MASTER:
    /* 
     *  Created: 11.6.2019 - 02:07 PM - (Christian)
     *  Edited:  11.6.2019 - -------- - (Christian)
     *           11.7.2019 - -------- - (Christian)
     *           11.8.2019 - -------- - (Christian)
     * 
     *  Overall Purpose: Universal controller for vehicle chassis assembly, used to determine/store properties of a vehicle chassis, and keep real-time
     *                   data about chassis status
     */

    //Objects and Components:
        private PartMaster partMaster; //This part's universal master controller

    [Header("Chassis Stats:")] //Stats unique to partType Chassis
        public bool[] turnWheels; //Sets which wheels turn vehicle. True in the socket index of a wheel means it is a turning wheel, false means it is a drive wheel
        public float turnAngle;   //Sets the mechanical turn angle wheels may be rotated at on this vehicle
        [ShowOnly] public float realTurnForce;    //This chassis' current turn force, based off of turn angle and wheel stats
        [ShowOnly] public float realGripStrength; //This chassis' current grip strength, based off of all wheels on chassis
        public Vector2 realPivotPoint; //The point around which this chassis currently turns, based off of all wheels on chassis

    //State Variables:
        internal bool vehicleSetUp; //Performs final cleanup of part flow info after all parts have been loaded in

    private void Awake()
    {
        //Get Objects and Components:
            partMaster = GetComponent<PartMaster>(); //Get PartMaster
    }

    public void Update()
    {
        //Check Vehicle Initialization:
            if (vehicleSetUp == false) //Only trigger initialization when vehicle hasn't been initialized
            {
                partMaster.GetPartGroup(true).SortParts(1).NameParts(NameMaster.GenerateRacerName()); //Organize parts by sorting layer
                vehicleSetUp = true; //Tell program that vehicle has been initialized
            }

    }

    public Vector2 FindPivotPoint() //Checks all wheels attached to this chassis, determines which ones are drive wheels and which ones are turn wheels, then finds pivot to turn around
    {
        Vector2 turnPosTotal = Vector2.zero; //Initialize vector of total turn positions in order to calculate average
        float driveWheels = 0; //Initialize float to store number/weight of drivewheels found, used to calculate average turnPos
        for (int x = turnWheels.Length; x > 0; x--) //Parse through all recognized wheels
        {
            if (turnWheels[x - 1] == true && partMaster.SocketTypeIndex(PartMaster.PartType.Wheel, x - 1).part != null) //If a non-null drivewheel has been found...
            {
                turnPosTotal.x += partMaster.SocketTypeIndex(PartMaster.PartType.Wheel, x - 1).x; //Add to total x value
                turnPosTotal.y += partMaster.SocketTypeIndex(PartMaster.PartType.Wheel, x - 1).y; //Add to total y value
                driveWheels += 1; //Add to average denominator
            }
        }
        if (driveWheels == 0) { } //ADD special exception for tank tracks, which are all turn wheels
        else //Under normal circumstances...
        {
            turnPosTotal.x /= driveWheels; //Divide by number of drive wheels to get pivot center
            turnPosTotal.y /= driveWheels; //Divide by number of drive wheels to get pivot center
        }
        Debug.Log("ChassisMaster: Pivot calculated. X = " + turnPosTotal.x + ", Y = " + turnPosTotal.y);
        realPivotPoint = turnPosTotal; //Update locally automatically
        return turnPosTotal; //Return found pivot
    }

}
