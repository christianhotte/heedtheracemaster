using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LootSelectorMaster : MonoBehaviour
{
    //Controls loot box selector reticule, and snaps it to designated points

    public float[] xPositions; //The x position of each snap point

    //Target Locations:
        internal float moveTime; //The amount of time it takes selector to move to new position
        internal float xTarget; //The current positional target of selector

    //Smoothdamp Variables:
        private float posRef; //XPosition smoothdamp reference variable

    private void Update()
    {
        //Position Update:
            float newXPosition = Mathf.SmoothDamp(transform.position.x, xTarget, ref posRef, moveTime); //Find new position
            transform.position = new Vector3(newXPosition, transform.position.y, transform.position.z); //Commit new position
    }
}
