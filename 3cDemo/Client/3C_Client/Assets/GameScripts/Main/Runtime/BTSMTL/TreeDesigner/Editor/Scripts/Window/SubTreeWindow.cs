using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner.Editor
{
    public class SubTreeWindow : BaseTreeWindow
    {
        public SubTree SubTree => Tree as SubTree;
    }
}
