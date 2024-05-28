using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using uWindowCapture;

namespace Gather.Interact
{
    public class WindowTexture : UwcWindowTexture
    {
        private void OnDisable()
        {
            material_.mainTexture = null;
        }
    }
}