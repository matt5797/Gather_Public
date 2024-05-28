using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gather.Character
{
    public class PlayerStream : MonoBehaviour
    {
        public enum AudioState
        {
            Disconnect,
            Stereo,
            Mono
        }
        public enum VideoState
        {
            Disconnect,
            Hologram,
            Grid
        }
    }
}