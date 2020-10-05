using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PartMaster : MonoBehaviour
{
    //PART MASTER:
    /* 
     *  Created: 11.7.2019 - 03:24 PM - (Christian)
     *  Edited:  11.8.2019 - -------- - (Christian)
     *           12.9.2019 - -------- - (Christian)
     * 
     *  Overall Purpose: Governs stat values which are universal to all part types, controls and records part attachment
     */
    
    //Public Enums and Lists (pertaining to parts):
    public enum PartType { Null, Chassis, Body, Engine, Bumper, Wheel, Hubcap, Exhaust, WeaponF, WeaponG } //Types of parts known in part system
    public enum PartSize { Standard, Large, Giant, Leviathan } //Size classes of parts known in part system
    public static string[] PartDescriptor = //Part adjectives (purely aesthetic)
                                     { "Fancy", "Nice", "Ruddy", "Filthy", "Strange", "Maimed", "Green", "Moist", "Clean",
                                       "Unfortunate", "Clammy", "Garbage", "Murderous", "Thicc", "Ugly", "Unnerving", "Whack",
                                       "Slippery", "Slick", "Saucey", "Backstabbing", "Primal", "Bloodthirsty", "Herbal",
                                       "Soothing", "Godforsaken", "Lucky", "Worthless", "Naked", "Intimate", "Sexy",
                                       "Repulsive", "Forgettable", "Legendary", "Whelming", "Superb", "Unusual", "Bland",
                                       "Modified", "Slapdash", "Jury-rigged", "Half-assed", "Greasy", "Tacky", "Jazzy",
                                       "Funky" }; 

    //Objects and Components:
        [Header("Relatives:")] //Relatives (parts which are physically adjacent to this part):
        public List<GameObject> children = new List<GameObject>(); //All parts attached to this part (a part may have any set number of children (set by part socketing))
        public GameObject parent; //The part to which this part is attached, if any (a part can only have one parent)
        public GameObject[] subParts; //Array of additional cosmetic sub-parts with adjusted sorting layer data to be taken into consideration
        private SpriteRenderer sr; //This part's spriteRenderer

    //Universal Variable Groups:
        [Header("Identity:")] //Identity (the basic identifiers of this part, usually relative to attachment):
            public PartType partType; //This part's type
            public PartSize partSize; //This part's size class
            public PartSocket[] partSockets; //How many parts this part can have attached, what types of parts they can be, and where/how to attach them

        [Header("Weight:")] //Weight:
            public float weight; //The weight of this part. Set to 0 if part has no weight properties
            [ShowOnly] public float netWeight; //The weight of this part and all of its children

        [Header("Health:")] //Health:
            public float maxHealth; //The maximum health this part can have. Set to 0 if part has no health properties
            [ShowOnly] public float realHealth; //The current health this part has
                      private float prevHealth; //This part's health after last CheckHealth

        [Header("Heat:")] //Heat:
            public float maxHeat; //The maximum heat capacity of this part. Set to 0 if part has no heating properties
            [ShowOnly] public float realHeat; //The current heat this part has
            [Space()] //Diffusion/Infusion:
            [Range(0, 1)] public float heatDiffusionFactor; //The percentage of this part's heat which gets conducted to other parts
            [Range(0, 1)] public float heatInfusionFactor;  //The percentage of diffused heat from other parts that gets conducted into this part
            public float diffusionCheckFrequency; //How often this part updates heat diffusion and infusion variables

            [Space()] //Meltdowns:
            [Range(0, 1)] public float meltThreshold; //The heat threshold at which this part starts doing melting checks (as percentage of total heat capacity)
            public float meltCheckFrequency; //How often this part does melting checks when heat is above melt threshold
            [Range(0, 1)] public float meltCheckChance; //How likely it is that a melt check will result in a meltdown
            public float meltdownDamage; //How much damage this part does to itself when melting down

    //Name Properties:
        public Vector2Ext labelSpritePos; //Positional information for when this part is on a lootLabel
        [ShowOnly] public string ownerName;       //The name of the driver of the vehicle this part comes from
        [ShowOnly] public string partAdjective;   //The random descriptor of this specific part
                   public string partName;        //The technical name of this specific part
                   public string partFaction;     //The faction which produces this part
                   public string partStats;       //The stats inherent to this part
                   public string partDescription; //The technical description of this part's stats and useage

    //Damage and Destruction:
        [Header("Damage and Destruction:")]
            public GameObject destructionEffect; //The effect to spawn upon destruction of this part
            public Color damageFlashColor;  //Color to flash when part takes damage
            public float damageFlashPeriod; //How long each damage flash is
            public int damageFlashNumber;   //How many times to flash part when damaged
            public bool damageFlashAll;     //If checked, all parts on vehicle will flash when this vehicle is damaged

            internal bool flashing;  //When true, this part will flash damage
            internal int flashCount; //Starts at damageFlashNumber, ends flashing once at 0
            private float timeSinceLastFlash; //The time which has passed since last damage flash
            private bool flashColorOn; //If true, spriteRenderer color will be damage flash color

    //Internal State Variables:
        [SerializeField] private bool doPreattachment; //If held true, vehicle will attempt part pre-attachment (for testing purposes)

    void Start()
    {
        //Get Objects and Components:
        sr = GetComponent<SpriteRenderer>(); //Get this part's spriteRenderer

        //Pre-Attach Childed Parts:
        if (doPreattachment == true) //If user has specifically selected this part for pre-attachment
        {
            GameObject[] preAttachments = new GameObject[transform.childCount]; //Initialize array to hold parts for pre-attachment, size equal to number of parts waiting for attachment
            for (int x = transform.childCount; x > 0; x--) //Run attachment protocol for each child
            {
                if (transform.GetChild(x - 1).gameObject.CompareTag("VehiclePart")) //Only attempt to attach vehicle parts
                {
                    AttachPart(transform.GetChild(x - 1).gameObject, -1, true); //Attach parts to open sockets
                }
            }
        }

        //Prep Racetime Variables:
        realHealth = maxHealth; //Initialize real health
        realHeat = maxHeat;     //Initialize real health
    }

    private void Update()
    {
        //DAMAGE FLASH:
        if (flashing == true) //If part is flashing damage...
        {
            if (flashCount <= 0) //End flash if flashCount reaches/exceeds 0
            {
                flashing = false; //Deactivate damage flash mode
                sr.color = Color.white; //Return sprite color to normal
                flashColorOn = false; //Reset marker variable
                timeSinceLastFlash = 0; //Reset marker variable
            }
            else //Otherwise, continue flashing...
            {
                timeSinceLastFlash += Time.deltaTime; //Decrement timeSinceLastFlash
                if (timeSinceLastFlash >= damageFlashPeriod) //If new flash switch is in order...
                {
                    flashCount--; //Decrement flashCount
                    flashColorOn = !flashColorOn; //Toggle flash color
                    if (flashColorOn) { sr.color = damageFlashColor; } //Flash specified color
                    else { sr.color = Color.white; } //Return to normal color
                }
            }
        }

    }

    //---<|PREP AND MAINTAINANCE|>-------------------------------------------------------------------------------------

    public GameObject AttachPart(GameObject part, int socketInput, bool destroyPart)
    {
        //ATTACH PART (Christian):
        /*
         *  Function: Attaches indicated part to indicated (appropriate) socket. Checks to make sure the two are compatible, then establishes necessary connections
         *  Note: If you set socket param negative, method will automatically attempt to assign part to first free socket of correct type
         */

        //INITIALIZE Variables:
        int socket = socketInput; //Initialize parameter as independent variable in case it needs to be changed
        PartType type = part.GetComponent<PartMaster>().partType; //The type of part this method is currently dealing with
        GameObject oldPart = null; //If indicated socket already contains a part, set this to that and use this variable to destroy part after unattachment

        //AUTO-FIND Empty Part Socket (testing feature):
        if (socket < 0) //If socket is set negative...
        { for (int x = partSockets.Length - 1; x >= 0; x--) //Parse through existing sockets for this part
            { if (partSockets[x].part == null && partSockets[x].type == type) { socket = x; break; } } } //If program can find an empty socket of the appropriate part type, use it

        //CHECK Part Compatibility:
        if (part == null) { Debug.Log("PartMaster: Error! AttachPart is being called without a part specified"); return null; }                              //Check 1: Part exists
        if (part.GetComponent<PartMaster>() == null) { Debug.Log("PartMaster: Error! AttachPart is being called on a non-part"); return null; }              //Check 2: Part is part
        if (type != partSockets[socket].type) { Debug.Log("PartMaster: Error! AttachPart is cannot put this type of part in this socket"); return null; }    //Check 3: Part is correct type
        if (partSockets[socket].size < part.GetComponent<PartMaster>().partSize) { Debug.Log("PartMaster: Error! Part is too big to attach"); return null; } //Check 4: Part fits on socket

        //CHECK Socket Availability:
        Debug.Log("PartMaster: Attempting to attach " + part.ToString() + " at index " + socket);
        //Catch nonexistent socket errors:
        if (partSockets.Length < socket || socket < 0)
        { Debug.Log("PartMaster: Error! Attempted to assign " + type.ToString().ToLower() + " to nonexistent socket #" + socket + "/" + partSockets.Length); return null; }
        //Replace part in socket if socket is already occupied:
        if (partSockets[socket].part != null)
        {
            oldPart = partSockets[socket].part; //Get part to be destroyed after assignment
            Debug.Log("PartMaster: Replacing " + oldPart.ToString() + "...");
        }

        //ASSIGN Part to Socket (and update variables):
        //Place in Array:
        partSockets[socket].part = part; //Assign GameObject
        //Set Parentage:
        part.transform.SetParent(transform);                 //Set this part as physical parent
        children.Add(part);                                  //Add part to this part's child list
        children.Remove(oldPart);                            //Remove old part from this part's child list
        part.GetComponent<PartMaster>().parent = gameObject; //Set this part as part's parent
        //Set Position:
        part.transform.localPosition = new Vector3(partSockets[socket].x, partSockets[socket].y, 0);                  //Set position of part
        part.transform.localRotation = new Quaternion(0, 0, partSockets[socket].rotation, part.transform.rotation.w); //Set rotation of part
        part.transform.localScale = new Vector3(partSockets[socket].scale.x, partSockets[socket].scale.y, 1);         //Set scale of part
        //Update Dependent Variables:
        GetNetWeight(); //Get/update net weight of this part
        if (partType == PartType.Chassis && type == PartType.Wheel) { GetComponent<ChassisMaster>().FindPivotPoint(); } //Update pivot point if wheel is being assigned on chassis

        //DESTROY Previous Part:
        if (oldPart != null)
        {
            if (destroyPart) { Destroy(oldPart); } //Destroy replaced part (if replacing a part), after all of its code connections have been severed
            else { return oldPart; } //Return old part for further use
        }

        Debug.Log("PartMaster: " + part.name + " has been successfully attached");
        return null;
    }

    public float GetNetWeight() //Gets this part's net weight, including its own weight and that of all childed parts
    {
        float totalWeight = weight; //Initialize calculated weight variable as the weight of this part
        for (int x = children.Count; x > 0; x--) //Parse through all children of this part...
        { totalWeight += children[x - 1].GetComponent<PartMaster>().GetNetWeight(); } //Get net weight of each child, making each child take the same calculation
        netWeight = totalWeight; //Set netWeight on this part, so that weight officially updates every time this method is called
        return totalWeight; //Return calculated weight
    }

    public List<GameObject> GetPartGroup(bool master) //Returns a list of all parts on the vehicle this part is on, in order of importance
    {
        //Note: Call with master TRUE to get full part tree, leave false to just get parts childed to current part
        //Note: This method has an extension called SortParts which neatly sorts all parts on vehicle into designated layers

        //Variable Initializations:
            List<GameObject> partGroup = new List<GameObject>(); //Create list to fill with constituent parts
            GameObject currentPart = gameObject; //Initialize variable for storing the identity of current part in part tree
            PartMaster cpMaster = this; //Initialize variable for storing the PartMaster of currently-focused part in part tree

        //Identify Chassis:
            if (master == true)
            {
                while (cpMaster.partType != PartType.Chassis) //Search until chassis is found
                {
                if (cpMaster.parent == null) //Check to make sure part tree ends with a chassis
                    { Debug.Log("PartMaster: Error! Attempted GetPartGroup on part with no chassis"); return null; } //Return a soft error if not
                currentPart = cpMaster.parent; //Move to new currentPart
                cpMaster = currentPart.GetComponent<PartMaster>(); //Move to new cpMaster
                }
                partGroup.Add(currentPart); //Add chassis to partGroup
            }
            
        //Populate PartGroup List:
            for (int x = 0; x < children.Count; x++) //Parse through all children part
            {
                partGroup.Add(children[x]); //Add child to partGroup list
                for (int y = 0; y < children[x].GetComponent<PartMaster>().GetPartGroup(false).Count; y++) //Create code asparagus for finding children of children
                {
                    partGroup.Add(children[x].GetComponent<PartMaster>().GetPartGroup(false)[y]); //Add each individual child to PartGroup as well
                }
            }

        //Return Result:
            return partGroup; //Return all parts found on vehicle
    }

//---<|RACETIME CALCULATIONS|>-------------------------------------------------------------------------------------

    public void CheckHealth() //Checks what level part health is at and executes methods depending on current health
    {
        //Note: This should be called on parts when they are damaged, unless their destruction is being postponed

        if (realHealth <= 0) //Vehicle Destruction:
        {
            if (partType == PartType.Chassis) //If CHASSIS has taken critical damage
            {
                Debug.Log("PartMaster: Vehicle " + transform.parent.name + " has been destroyed!"); //Log destruction of vehicle
         
                //Initialize Variables:
                    LootTableKeeper lootTable = GameObject.Find("Global Object").GetComponent<LootTableKeeper>(); //Find loot table controller
                    GameObject effect = null; //Container to store instance of destruction effect (used when applicable)

                //DESTRUCTION EFFECTS:
                    if (destructionEffect != null) { effect = Instantiate(destructionEffect); } //Instantiate destruction effect object
                    if (effect != null) //If destructionEffect was instantiated
                    {
                        effect.transform.position = transform.position;     //Set effect position
                        effect.transform.rotation = transform.rotation;     //Set effect rotation
                        effect.transform.localScale = transform.localScale; //Set effect scale
                    }

                //TRANSFER PARTS TO LOOT TABLE:
                    //CHASSIS (parenting):
                    GameObject controller = transform.parent.gameObject; //Get controller parent
                    transform.SetParent(lootTable.lootBin);              //Move parentage to loot table
                    Destroy(controller);                                 //Destroy previous parent
                    //CHASSIS (lootTable upkeep):
                    GetComponent<SpriteRenderer>().enabled = false; //Deactivate spriterenderer
                    transform.position = Vector3.zero;       //Reset position
                    transform.rotation = new Quaternion();   //Reset rotation
                    transform.localScale = Vector3.zero;     //Reset scale
                    lootTable.tableContents.Add(gameObject); //Add part
                    //OTHER PARTS (lootTable upkeep):
                    for (int x = 0; x < children.Count; x++) //Parse through raw list of all children of chassis
                    {
                        //Initialize Variables:
                        GameObject part = children[x]; //Get current part
                        PartMaster partCont = part.GetComponent<PartMaster>(); //Get current part's controller
                        //Ignore 2/3 Wheels:
                        int chance = 2; //Initialize pick chance
                        if (partCont.partType == PartType.Wheel) { chance = Random.Range(0, 3); } //Get random chance of including wheel
                        if (chance > 1) //Check pick chance
                        {
                        //LootTable Upkeep:
                        part.transform.SetParent(lootTable.lootBin);         //Move parentage to loot table
                        part.GetComponent<SpriteRenderer>().enabled = false; //Deactivate spriterenderer
                        part.transform.position = Vector3.zero;     //Reset position
                        part.transform.rotation = new Quaternion(); //Reset rotation
                        part.transform.localScale = Vector3.zero;   //Reset scale
                        lootTable.tableContents.Add(part);          //Add part
                        }
                        else //If loot was not included in table, destroy it
                        {
                            Destroy(part); //Destroy part
                        }
                        if (partCont.children.Count != 0) //If part has any children...
                        {
                            for (int y = 0; y < partCont.children.Count; y++) //Parse through part's children
                            {
                                partCont.children[y].transform.SetParent(lootTable.lootBin); //Add child to loot bin
                                partCont.children[y].GetComponent<SpriteRenderer>().enabled = false; //Make child invisible
                                partCont.children[y].transform.position = Vector3.zero;        //Reset position
                                partCont.children[y].transform.rotation = new Quaternion();    //Reset rotation
                                partCont.children[y].transform.localScale = Vector3.zero;      //Reset scale
                                lootTable.tableContents.Add(partCont.children[y]);             //Add child
                                partCont.children[y].GetComponent<PartMaster>().parent = null; //Reset parental reference

                                //Weapon Deactivation:
                                if (partCont.children[y].GetComponent<PartMaster>().partType == PartType.WeaponG)
                                {
                                    WeaponMaster_MachineGun wepCont = partCont.children[y].GetComponent<WeaponMaster_MachineGun>(); //Get weapon controller
                                    wepCont.wired = false;
                                    wepCont.controlActive = false;
                                    wepCont.gimballed = false;
                                    wepCont.firing = false;
                                }
                            }
                        }
                        //Reset RaceTime Variable References:
                        partCont.children = new List<GameObject>(); //Reset children reference
                        partCont.parent = null;                     //Reset parent reference
                        partCont.realHealth = maxHealth;            //Reset health
                        partCont.realHeat = maxHeat;                //Reset heat
                    }
                    //CHASSIS (reset raceTime variable references):
                    children = new List<GameObject>(); //Reset children reference
                    realHealth = maxHealth;            //Reset health
                    realHeat = maxHeat;                //Reset heat

            } 
        }
        else if (prevHealth > realHealth) //Vehicle Damage:
        {
            //FLASH DAMAGE:
            flashing = true; //Turn on flashing
            flashCount = damageFlashNumber; //Refresh flash number
            if (damageFlashAll == true) //If damage on this part flashes all other parts...
            {
                List<GameObject> partGroup = GetPartGroup(true); //Get all parts attached to this vehicle
                for (int x = 0; x < partGroup.Count; x++) //Parse through all parts on vehicle...
                {
                    PartMaster partCont = partGroup[x].GetComponent<PartMaster>(); //Get current part's controller
                    partCont.damageFlashColor = damageFlashColor;   //Inherit flash color property
                    partCont.damageFlashPeriod = damageFlashPeriod; //Inherit flash speed property
                    partCont.flashing = true;                       //Begin part flashing
                    partCont.flashCount = damageFlashNumber;        //Reset part flash count
                }
            }
        }

        prevHealth = realHealth; //Log checked health
    }

//---<|GLOBAL DEFINITIONS|>-------------------------------------------------------------------------------------

    public PartSocket SocketTypeIndex(PartType type, int index) //Splits overall PartSocket array into smaller arrays based on type, then applies the given index to the array of that type
    {
        List<PartSocket> typeList = new List<PartSocket>(); //Initialize type-specific list
        for (int x = 0; x < partSockets.Length; x++) //Parse through all sockets on this part
        { if (partSockets[x].type == type) { typeList.Add(partSockets[x]); }} //If socket type matches requested requested type, add it to list
        if (partSockets.Length <= index) { Debug.Log("PartMaster: Error! Requested index is out of range"); return new PartSocket(); } //If index is out of range, throw error and return null socket
        return typeList[index]; //Return the given index of the given type group
    }
}
