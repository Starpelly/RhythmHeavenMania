using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using RhythmHeavenMania.Util;
namespace RhythmHeavenMania.Games.BuiltToScaleDS
{
    public class BTSPiece : MonoBehaviour
    {
        public Animator anim;

        void LateUpdate()
        {
            if (anim.IsAnimationNotPlaying())
                Destroy(gameObject);
        }
    }
}
