using System;
using UnityEngine.Internal;
using UnityEngine.Scripting;

namespace UnityEngine
{
    /// <summary>
    ///     <para>Contains custom temporary part weights for Pass1 of track generation.</para>
    /// </summary>
    [System.Serializable]
    public struct AdaptiveWeightSegmentP1
    {
        //ADAPTIVE WEIGHT SEGMENT (PASS 1) (struct):
        /* 
         *  Created: 11.9.2019 - 12:29 PM - (Christian)
         *  Edited:  11.9.2019 - -------- - (Christian)
         * 
         *  Overall Purpose: Allows user to set track weights for a certain number of tracks during Pass 1 of track generation. Track templates can have any number of
         *                   these custom segments. Keep in mind they will still be overridden if the alternative is a failed track.
         */


        //VARIABLE DECLARATIONS:
        /// <summary>
        ///     <para>The duration of this segment (as a percentage of target track number).</para>
        /// </summary>
        [Range(0, 1)]public float percentDuration;
        /// <summary>
        ///     <para>The duration of this segment (in number of tracks, acts as bare minimum when also using percentDuration).</para>
        /// </summary>
        public int rawDuration;
        [Space()]
        /// <summary>
        ///     <para>Decides how likely Straight1x1 is to be picked from among viable candidates.</para>
        /// </summary>
        [Range(0, 1)] public float straightWeight;
        /// <summary>
        ///     <para>Decides how likely TurnLeft1x1 is to be picked from among viable candidates.</para>
        /// </summary>
        [Range(0, 1)] public float leftWeight;
        /// <summary>
        ///     <para>Decides how likely TurnRight1x1 is to be picked from among viable candidates.</para>
        /// </summary>
        [Range(0, 1)] public float rightWeight;
    }
}