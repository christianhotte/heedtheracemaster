using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LootLabelMaster : MonoBehaviour
{
    //Controls Loot Labels and sends them to and from designated positions

    //Objects and Components:
        private SpriteRenderer sr; //This label's spriterenderer
        public GameObject loot; //The loot this label is labelling
        public GameObject lootSprite; //The image on this label of this label's loot
        [Space()]

        public GameObject titleTextObj;
        public GameObject factionObj;
        public GameObject factionTextObj;
        public GameObject statsObj;
        public GameObject statsTextObj;
        public GameObject descriptionTextObj;

        private TextMeshProUGUI titleText;       //This label's title text
        private TextMeshProUGUI factionText;     //This label's faction text
        private TextMeshProUGUI statsText;       //This label's stats text
        private TextMeshProUGUI descriptionText; //This label's description text
        private List<TextMeshProUGUI> allText = new List<TextMeshProUGUI>(); //All text to be faded in and out

    //Target Locations:
        private bool activated; //Toggled by activation function

        private Vector3 position1;    //The original position of this label on its box
        private float rotation1;      //The original rotation (z) of this label on its box
        private Vector3 scale1;       //The original scale of this label on its box
        private Transform location2;  //The destination position of this label

        private Vector3 targetPosition; //The current position target of this label
        private float targetRotation;   //The current rotation (z) target of this label
        private Vector3 targetScale;    //The current size target of this label

    //Smoothdamp References:
        private float currentTravelTime; //The most recently-given travel time variable for box label translation
        private bool travelling;       //True while label is in transit between positions
        private float travelStartTime; //Time marker (since startup) since travel began

        private Vector3 posRef;   //Smoothdamp positional velocity reference
        private float rotRef;     //Smoothdamp rotational (z) velocity reference
        private Vector3 scaleRef; //Smoothdamp scale velocity reference

    private void Start()
    {
        //Get Objects and Components:
            sr = GetComponent<SpriteRenderer>(); //Get this label's spriteRenderer

        //Get Positions:
            position1 = transform.position;   //Save current transform position
            rotation1 = transform.rotation.z; //Save current transform rotation
            scale1 = transform.localScale;    //Save current transform scale
            location2 = GameObject.Find("LootLabelPositionReference").transform;

        //Get Text:
            titleText = titleTextObj.GetComponent<TextMeshProUGUI>();             //Get title text
            factionText = factionTextObj.GetComponent<TextMeshProUGUI>();         //Get faction text
            statsText = statsTextObj.GetComponent<TextMeshProUGUI>();             //Get stats text
            descriptionText = descriptionTextObj.GetComponent<TextMeshProUGUI>(); //Get description text
            
            allText.Add(factionObj.GetComponent<TextMeshProUGUI>()); //Add faction text label to list
            allText.Add(statsObj.GetComponent<TextMeshProUGUI>());   //Add stats text label to list
            allText.Add(titleText);       //Add title text to list
            allText.Add(factionText);     //Add faction text to list
            allText.Add(statsText);       //Add stats text to list
            allText.Add(descriptionText); //Add description text to list
    }

    private void Update()
    {
        //Seek target position:
        if (travelling == true)
        {
            //Get new Transform numbers:
                Vector3 newPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref posRef, currentTravelTime*0.3f); //Get new position
                float newRotation = Mathf.SmoothDamp(transform.rotation.z, targetRotation, ref rotRef, currentTravelTime*0.3f);   //Get new rotation
                Vector3 newScale = Vector3.SmoothDamp(transform.localScale, targetScale, ref scaleRef, currentTravelTime*0.3f);   //Get new scale

            //Commit New Transform:
                transform.position = newPosition; //Set new position
                transform.rotation = new Quaternion(0, 0, newRotation, transform.rotation.w); //Set new rotation
                transform.localScale = newScale; //Set new scale

            //Check for Travel end:
                if ((Time.realtimeSinceStartup - travelStartTime) > currentTravelTime) //If time has passed
                {
                    travelling = false; //Turn off travel calculations
                    
                    //Finish Travel:
                        transform.position = targetPosition; //Set final position
                        transform.rotation = new Quaternion(0, 0, targetRotation, transform.rotation.w); //Set final rotation
                        transform.localScale = targetScale; //Set final scale

                    //Deactivation Extra Cleanup:
                        if (activated == false)
                        {
                            sr.color = Color.clear; //Set label invisible
                            for (int x = 0; x < allText.Count; x++) //Parse through all text items...
                            { TextMeshProUGUI t = allText[x]; t.color = new Color(t.color.r, t.color.g, t.color.b, 0); } //Set text invisible
                            lootSprite.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0); //Set sprite invisible
                        }
                }
        }
            
    }

    public void Activate(float travelTime) 
    {
        //Update Data:
            currentTravelTime = travelTime; //Update travel time data
            activated = !activated;         //Toggle activation status

        //Set Target:
            if (activated == true) //Send to target destination...
            {
                targetPosition = location2.position;   //Set target position
                targetRotation = location2.rotation.z; //Set target rotation
                targetScale = location2.localScale;    //Set target scale
                
                sr.color = Color.white; //Set label visible
                for (int x = 0; x < allText.Count; x++) //Parse through all text items...
                { TextMeshProUGUI t = allText[x]; t.color = new Color(t.color.r, t.color.g, t.color.b, 1); } //Set text visible
                lootSprite.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.7f); //Set sprite visible
            }
            else //Send back to default destination
            {
                targetPosition = position1; //Set target position
                targetRotation = rotation1; //Set target rotation
                targetScale = scale1;       //Set target scale
            }

        //Begin Translation:
            travelling = true; //Tell program to begin travel
            travelStartTime = Time.realtimeSinceStartup; //Log travel start time
    }

    public void SetContents(GameObject contents) //Assigns contents of label text
    {
        //Initialize Variables
        loot = contents; //Set loot
        PartMaster pm = contents.GetComponent<PartMaster>(); //Get part's controller

        if (pm.ownerName != "") { titleText.text = pm.ownerName + "'s " + pm.partAdjective + " " + pm.partName; } //Set title
        else { titleText.text = "Someone's " + pm.partAdjective + " " + pm.partName; } //Set default title
        factionText.text = pm.partFaction;         //Set faction
        statsText.text = pm.partStats;             //Set stats
        descriptionText.text = pm.partDescription; //Set description

        lootSprite.GetComponent<SpriteRenderer>().sprite = loot.GetComponent<SpriteRenderer>().sprite; //Set loot sprite
        lootSprite.transform.localPosition = new Vector3(pm.labelSpritePos.x, pm.labelSpritePos.y, 0); //Set loot sprite position
        lootSprite.transform.localRotation = new Quaternion(0, 0, pm.labelSpritePos.r, lootSprite.transform.rotation.w); //Set sprite rotation
        lootSprite.transform.localScale = new Vector3(pm.labelSpritePos.s, pm.labelSpritePos.s, 1); //Set loot sprite scale
    }

}
