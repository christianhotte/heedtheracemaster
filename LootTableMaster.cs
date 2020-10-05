using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Rewired;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LootTableMaster : MonoBehaviour
{
    //LOOT TABLE MASTER
    /* 
     *  Created: 12.15.2019 - 12:33 PM - (Christian)
     *  Edited:  12.15.2019 - -------- - (Christian)
     * 
     *  Function: Controls functionality of loot table, and cutscene flow in loot table scene
     *  
     */

        public List<GameObject> lootTableContents; //The contents of the loot table for this round
        public string[] hints; //Hints to randomly assign to hint text

    //Objects and Components:
        CinemachineBrain camBrain; //Main camera controller
        LootTableKeeper tableKeeper; //Loot table keeper containing loot from last race
        Animator lootTableAnim; //This loot table's animator controller
        Player playerInput; //Rewired player reference
        public GameObject selector; //Loot selector icon
        LootSelectorMaster selMaster; //Loot selector master
        public GameObject[] lootTableLabels; //Loot table label gameObjects
        SpriteRenderer selectorRen; //Loot selector's spriteRenderer
        public GameObject hintText; //Hint text box for hints
    [Header("Cameras:")]
        public GameObject vcam1; //CM vcam 1
        public GameObject vcam2; //CM vcam 2

    [Header("Loot Table Flow:")] //Variables which control loot table cutscene flow
        public float darkWaitTime; //Time to wait in complete darkness before starting to shift table to actual color
        public float panWaitTime; //Time to wait before beginning to pan camera to loot table
        public float darkTransTime; //Time taken to transition from complete darkness to actual color
        public float selWaitTime; //How long after light flicker selector fades in
        public float selFadeTime; //How long it takes selector to fade in/out
        public float selMoveTime; //How long it takes selector to move to its target position
        public float labelMoveTime; //How long it takes a box label to get to/from its visible position
        public float lootSwitchWait; //How long to wait before switching loot boxes when player is holding down switch button
        public int selBlinkNum; //How many times to blink selector upon loot collection
        public float selBlinkInterval; //How long to wait between blinks when blinking loot selector upon loot collection
        public float panOutWaitTime; //How long to wait after loot confirmation to pan camera away

    //Internal Variables:
        private int phase = 1; //Tracks which cutscene phase program is in (always starts at 1)
        private int lootFocus = 3; //Tracks which item of loot is currently under focus
        private float actualPhaseTime; //The actual time since last phase
        private float lastPhaseTime; //The time since last phase, used to allow phases to be edited without having to change whole flow
        private float lastSubPhaseTime; //Secondary variable for tracking phase durations within phases, used where needed
        private float lastSubPhaseTime2; //Secondary variable for tracking phase durations within phases, used where needed
        private float timeSinceLootSwitch = 0; //Time waited between loot switches when player is holding down button
        private int selectorBlinks; //How many times the loot selector has blinked after loot confirmation

        private Color hintColorClear;  //The color of hint text when a = 0
        private Color hintColorOpaque; //The color of hint text when a = 1

    public GameObject garage_Door;

    private void Start()
    {
        //Get Objects and Components:
            camBrain = GameObject.Find("Main Camera").GetComponent<CinemachineBrain>(); //Get main camera controller
            tableKeeper = GameObject.Find("Global Object").GetComponent<LootTableKeeper>(); //Get loot table keeper
            lootTableAnim = GetComponent<Animator>(); //Get loot table animator
            playerInput = ReInput.players.GetPlayer(0); //Set up rewired control
            selectorRen = selector.GetComponent<SpriteRenderer>(); //Get selector's spriteRenderer
            selMaster = selector.GetComponent<LootSelectorMaster>(); //Get selector's controller

        //Initialize Internal Variables:
            lastPhaseTime = Time.realtimeSinceStartup; //Initialize lastPhaseTime as real startup time for program
            lastSubPhaseTime = 0; //Initialize subPhaseTime, activated as needed by lifting from zero
            lastSubPhaseTime2 = 0; //Initialize subPhaseTime, activated as needed by lifting from zero
            lootTableContents = tableKeeper.GetLoot(transform, 5); //Get a piece of loot for each loot box
            tableKeeper.ClearLoot(); //Clear loot table for next race

        //Set Hint Text:
            if (hintText != null && hints.Length != 0) //If hintText has been assigned and there are hints written...
            {
                if (hintText.GetComponent<TextMeshProUGUI>() != null) //If text box exists...
                {
                    hintText.GetComponent<TextMeshProUGUI>().text = hints[Random.Range(0, hints.Length)]; //Assign random hint
                    hintColorClear = hintText.GetComponent<TextMeshProUGUI>().color; //Get hint text current color (clear)
                    hintColorOpaque = new Color(hintColorClear.r, hintColorClear.g, hintColorClear.b, 1); //Create final hint color (opaque)
                }
            }
    }

    private void Update()
    {
        actualPhaseTime = Time.realtimeSinceStartup - lastPhaseTime; //Refresh actual phase time tracker
        int switchVar = phase;
        switch (switchVar)
        {
            case 1: //PHASE #1: Fade In:
                if (darkWaitTime < actualPhaseTime) //TABLE FADEIN:
                {
                    if (lastSubPhaseTime == 0) { lastSubPhaseTime = Time.realtimeSinceStartup; } //Initialize subPhaseTime once
                    float colorTime = (Time.realtimeSinceStartup - lastSubPhaseTime).Remap(0, darkTransTime, 0, 1); //Get time since fadein activation (out of 1)
                    Color newColor = Color.LerpUnclamped(Color.black, Color.white, colorTime); //Lerp to lighter color
                    GetComponent<SpriteRenderer>().color = newColor; //Commit new color
                }
                if (panWaitTime < actualPhaseTime) //CAMERA PAN:
                {
                    if (vcam2.activeSelf != true) //If second camera is not already active
                    { vcam2.SetActive(true); } //Activate second camera
                }
                if (GetComponent<SpriteRenderer>().color.grayscale > 0.95) //NEXTPHASE:
                {
                    GetComponent<SpriteRenderer>().color = Color.white; //Finish off color transition
                    lastSubPhaseTime = 0; //Reset subPhaseTime
                    lastPhaseTime = Time.realtimeSinceStartup; //Refresh lastPhaseTime
                    phase = 2; //Move on to next phase
                }
                break;
            case 2: //PHASE #2: Pull Lightswitch:
                if (playerInput.GetButtonDown("Confirm") == true || Input.GetKeyDown(KeyCode.Return) == true) //PULL SWITCH:
                {
                    lootTableAnim.SetBool("PullingSwitch", true); //Animate switch pull
                }
                if (playerInput.GetButtonUp("Confirm") == true && lootTableAnim.GetBool("PullingSwitch") == true ||
                    Input.GetKeyUp(KeyCode.Return) == true && lootTableAnim.GetBool("PullingSwitch") == true) //RELEASE SWITCH:
                {
                    lootTableAnim.SetBool("PullingSwitch", false); //Animate switch release
                    lastSubPhaseTime = 0; //Reset subPhaseTime
                    lastPhaseTime = Time.realtimeSinceStartup; //Refresh lastPhaseTime
                    phase = 3; //Move on to next phase
                }
                break;
            case 3: //PHASE #3: Activate Loot Selection:
                if (actualPhaseTime > selWaitTime) //ACTIVATE SELECTOR:
                {
                    if (lastSubPhaseTime == 0) { lastSubPhaseTime = Time.realtimeSinceStartup; } //Initialize subPhaseTime once
                    float colorTime = (Time.realtimeSinceStartup - lastSubPhaseTime).Remap(0, selFadeTime, 0, 1); //Get time since fadein activation (out of 1)
                    Color newColor = Color.LerpUnclamped(Color.clear, Color.white, colorTime); //Lerp to higher opacity
                    Color newTextColor = Color.LerpUnclamped(hintColorClear, hintColorOpaque, colorTime); //Lerp to higher opacity
                    selectorRen.color = newColor;                                  //Commit new selector color
                    hintText.GetComponent<TextMeshProUGUI>().color = newTextColor; //Commit new text color
                }
                if (selectorRen.color.a > 0.95f) //NEXTPHASE:
                {
                    //Initialize Label Text:
                    for (int x = 0; x < lootTableLabels.Length; x++) //Parse through loot labels
                    { lootTableLabels[x].GetComponent<LootLabelMaster>().SetContents(lootTableContents[x]); } //Find/Set label contents

                    lootTableLabels[lootFocus - 1].GetComponent<LootLabelMaster>().Activate(labelMoveTime); //Deactivate first label
                    selMaster.moveTime = selMoveTime; //Set selector move time
                    selMaster.xTarget = selMaster.xPositions[lootFocus - 1]; //Commit selector initial position

                    selectorRen.color = Color.white; //Finish off color transition
                    hintText.GetComponent<TextMeshProUGUI>().color = hintColorOpaque; //Finish off color transition
                    lastSubPhaseTime = 0; //Reset subPhaseTime
                    lastPhaseTime = Time.realtimeSinceStartup; //Refresh lastPhaseTime
                    phase = 4; //Move on to next phase
                }
                break;
            case 4: //PHASE #4: Loot Selection:
                if (playerInput.GetAxis("Horizontal") < 0 && playerInput.GetAxisPrev("Horizontal") == 0 || //Player pushes left...
                    playerInput.GetAxis("Horizontal") < 0 && timeSinceLootSwitch == 0 ||                   //Player holds left for long enough...
                    Input.GetKeyDown(KeyCode.LeftArrow) ||                                                 //Player pushes left...
                    Input.GetKey(KeyCode.LeftArrow) && timeSinceLootSwitch == 0)                           //Player holds left for long enough...
                {
                    timeSinceLootSwitch = lootSwitchWait; //Start wait timer for holding down loot switch button
                    lootTableLabels[lootFocus - 1].GetComponent<LootLabelMaster>().Activate(labelMoveTime); //Deactivate previous label
                    lootFocus--; if (lootFocus < 1) { lootFocus = 5; } //Decrement lootFocus and underflow if necessary
                    lootTableAnim.SetInteger("Selection", lootFocus); //Update animator
                    lootTableLabels[lootFocus - 1].GetComponent<LootLabelMaster>().Activate(labelMoveTime); //Activate new label
                    selMaster.moveTime = selMoveTime; //Set selector move time
                    selMaster.xTarget = selMaster.xPositions[lootFocus - 1]; //Commit selector position
                }
                if (playerInput.GetAxis("Horizontal") > 0 && playerInput.GetAxisPrev("Horizontal") == 0 || //Player pushes right...
                    playerInput.GetAxis("Horizontal") > 0 && timeSinceLootSwitch == 0 ||                   //Player holds right for long enough...
                    Input.GetKeyDown(KeyCode.RightArrow) ||                                                //Player pushes right...
                    Input.GetKey(KeyCode.RightArrow) && timeSinceLootSwitch == 0)                          //Player holds right for long enough...
                {
                    timeSinceLootSwitch = lootSwitchWait; //Start wait timer for holding down loot switch button
                    lootTableLabels[lootFocus - 1].GetComponent<LootLabelMaster>().Activate(labelMoveTime); //Deactivate previous label
                    lootFocus++; if (lootFocus > 5) { lootFocus = 1; } //Increment lootFocus and overflow if necessary
                    lootTableAnim.SetInteger("Selection", lootFocus); //Update animator
                    lootTableLabels[lootFocus - 1].GetComponent<LootLabelMaster>().Activate(labelMoveTime); //Activate new label
                    selMaster.moveTime = selMoveTime; //Set selector move time
                    selMaster.xTarget = selMaster.xPositions[lootFocus - 1]; //Commit selector position
                }
                if (timeSinceLootSwitch > 0) //If loot switch timer is ticking down...
                {
                    timeSinceLootSwitch -= Time.deltaTime; //Decrement timeSinceLootSwitch if in use
                    if (timeSinceLootSwitch < 0) { timeSinceLootSwitch = 0; } //Floor timeSinceLootSwitch at 0
                }
                if (playerInput.GetButton("Confirm") || Input.GetKey(KeyCode.Return)) //LOOT CONFIRMATION:
                {
                    //ASSIGN LOOT TO INVENTORY:
                    lootTableLabels[lootFocus - 1].GetComponent<LootLabelMaster>().Activate(labelMoveTime); //Deactivate selected label
                    GameObject.Find("Global Object").GetComponent<InventoryMaster>().inventory.Add(lootTableContents[lootFocus - 1]); //Add selected item to player inventory
                    lootTableContents[lootFocus - 1].transform.SetParent(GameObject.Find("Global Object").GetComponent<InventoryMaster>().inventoryBin); //Send part to player inventory

                    //NEXTPHASE
                    lastSubPhaseTime = 0; //Reset subPhaseTime
                    lastPhaseTime = Time.realtimeSinceStartup; //Refresh lastPhaseTime
                    phase = 5; //Move on to next phase
                }
                break;
            case 5: //PHASE #5: Scene Exit:
                if (lastSubPhaseTime == 0) { lastSubPhaseTime = Time.realtimeSinceStartup; } //Log sub phase time
                if ((Time.realtimeSinceStartup - lastSubPhaseTime) > selBlinkInterval && //Toggle selector each blink interval
                    selBlinkNum >= selectorBlinks) //Stop after specified number of blinks
                {
                    selectorBlinks++; //Increment blink counter
                    lastSubPhaseTime = 0; //Reset sub-Phase tracker

                    if (selectorRen.color != Color.clear) { selectorRen.color = Color.clear; } //Toggle selector clear
                    else { selectorRen.color = Color.white; } //Toggle selector white
                }
                if ((Time.realtimeSinceStartup - lastPhaseTime) > panOutWaitTime) //PAN AWAY:
                {
                    vcam2.SetActive(false); //Deactivate table camera

                    //Fade to Black:
                    if (lastSubPhaseTime2 == 0) { lastSubPhaseTime2 = Time.realtimeSinceStartup; } //Initialize subPhaseTime once
                    float colorTime = (Time.realtimeSinceStartup - lastSubPhaseTime2).Remap(0, darkTransTime, 0, 1); //Get time since fadein activation (out of 1)
                    Color newColor = Color.LerpUnclamped(Color.white, Color.black, colorTime); //Lerp to lighter color
                    GetComponent<SpriteRenderer>().color = newColor; //Commit new color
                    if (GetComponent<SpriteRenderer>().color.grayscale < 0.05) //NEXTPHASE:
                    {
                        GetComponent<SpriteRenderer>().color = Color.black; //Finish off color transition
                        lastSubPhaseTime = 0; //Reset subPhaseTime
                        lastPhaseTime = Time.realtimeSinceStartup; //Refresh lastPhaseTime
                        phase++; //Move on to next phase
                    }
                }
                break;
            case 6: //PHASE #6: Scene Transition:
                StartCoroutine(Enter_Garage());
                break;
        }

    }

    IEnumerator Enter_Garage()
    {
        garage_Door.SetActive(true);
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene("Garage");
    }
}
