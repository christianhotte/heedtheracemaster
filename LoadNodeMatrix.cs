using System.Collections;
using UnityEngine.Internal;
using UnityEngine.Scripting;

namespace UnityEngine
{
    /// <summary>
    ///     <para>Contains custom temporary part weights for Pass1 of track generation.</para>
    /// </summary>
    [System.Serializable]
    public class LoadNodeMatrix
    {
        //LOAD NODE MATRIX (struct/class):
        /* 
         *  Created: 11.9.2019 - 01:09 PM - (Christian)
         *  Edited:  11.9.2019 - -------- - (Christian)
         * 
         *  Overall Purpose: Allows 3D representation of potential LoadNode matrix to be displayed in inspector as a cube of bools
         */

        //ARRAY CONTROL VARIABLES:
        [Range(1, 50)] public int columnSize;
        [Range(1, 50)] public int rowSize;

        [System.Serializable]
        public struct LNMRows //Set as serializable struct so that array of arrays can be displayed in inspector
        {
             public bool[] row;
        }
        public LNMRows[] columns;
    }
}