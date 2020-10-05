using System;
using UnityEngine.Internal;
using UnityEngine.Scripting;

namespace UnityEngine
{
    /// <summary>
    ///     <para>Contains data required to inform part attachment.</para>
    /// </summary>
    [System.Serializable]
    public struct PartSocket
    {
        //PART SOCKET (struct):
        /* 
         *  Created: 11.8.2019 - 05:20 PM - (Christian)
         *  Edited:  11.8.2019 - -------- - (Christian)
         * 
         *  Overall Purpose: Contains all variables required to create a working socket on a vehicle part. A socket allows a part to be affixed to another part at a specified
         *                   position, angle, and scale. PartSocket also allows the user to define what size classes the socket can accept
         */
        

        //VARIABLE DECLARATIONS:
        /// <summary>
        ///     <para>The part currently in this socket.</para>
        /// </summary>
        public GameObject part;
        /// <summary>
        ///     <para>The type of part this socket is compatible with.</para>
        /// </summary>
        public PartMaster.PartType type;
        /// <summary>
        ///     <para>The maximum size of part this socket is compatible with.</para>
        /// </summary>
        public PartMaster.PartSize size;
        /// <summary>
        ///     <para>X position of part in socket, relative to parent.</para>
        /// </summary>
        public float x;
        /// <summary>
        ///     <para>Y position of part in socket, relative to parent.</para>
        /// </summary>
        public float y;
        /// <summary>
        ///     <para>Rotation of part in socket, relative to parent.</para>
        /// </summary>
        public float rotation;
        /// <summary>
        ///     <para>Scale of part in socket.</para>
        /// </summary>
        public Vector2 scale;
    }
}
