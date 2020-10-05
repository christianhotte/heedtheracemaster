using System.Collections;
using UnityEngine.Internal;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [System.Serializable]
    public class NodeDefMatrix
    {
        //Class with custom GUI properties for drawing nodespace array
        public int nodespaceX; //How much horizontal space this node takes up
        public int nodespaceY; //How much vertical space this node takes up

        [System.Serializable]
        public struct NDColumns
        {
            public bool[] column;
        }
        public NDColumns[] row;
    }

}

