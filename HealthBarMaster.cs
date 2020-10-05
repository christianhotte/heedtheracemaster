using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthBarMaster : MonoBehaviour
{
    //WEAPON CONTROLLER
    /* 
     *  Created: 12.17.2019 - 06:08 PM - (Christian)
     *  Edited:  12.17.2019 - -------- - (Christian)
     * 
     *  Function: Controls health bar in race scene UI
     *  
     */

    //Objects and Components:
    public Transform indicator; //Red indicator arrow on bar
    public Transform beginning; //Leftmost point on indicator bar
    public Transform end;       //Rightmost point on indicator bar

    void Update()
    {
        //Attempt to Find Player:
        if (GameObject.FindWithTag("Player") == true) //If player can be found in scene
        {
            ChassisMaster chassisCont = null; //Initialize container for player chassis controller
            if (GameObject.FindWithTag("Player").GetComponentInChildren<ChassisMaster>() != null) { chassisCont = GameObject.FindWithTag("Player").GetComponentInChildren<ChassisMaster>(); }
            else if (GameObject.FindWithTag("Player").GetComponentInParent<ChassisMaster>() != null) { chassisCont = GameObject.FindWithTag("Player").GetComponentInParent<ChassisMaster>(); }
            if (chassisCont != null) //If player chassis can be found in scene
            {
                PartMaster pm = chassisCont.GetComponent<PartMaster>(); //Get chassis' partmaster
                float currentHealth = pm.realHealth / pm.maxHealth; //Get player's real health out of a possible 1
                float indicatorXPos = Mathf.Lerp(end.position.x, beginning.position.x, currentHealth); //Lerp indicator position based on player's current relative health
                indicator.position = new Vector3(indicatorXPos, indicator.position.y, indicator.position.z); //Move indicator to designated position
            }
        }
    }
}
