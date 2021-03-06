using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyBezierCurves;
using DG.Tweening;

using RhythmHeavenMania.Util;
namespace RhythmHeavenMania.Games.RhythmRally
{
    public class RhythmRally : Minigame
    {
        public enum RallySpeed { Slow, Normal, Fast, SuperFast }

        [Header("Camera")]
        public Transform renderQuadTrans;
        public Transform cameraPos;


        [Header("Ball and curve info")]
        public GameObject ball;
        public GameObject ballShadow;
        public BezierCurve3D serveCurve;
        public BezierCurve3D returnCurve;
        public GameObject ballHitFX;


        [Header("Animators")]
        public Animator playerAnim;
        public Animator opponentAnim;

        [Header("Properties")]
        public RallySpeed rallySpeed = RallySpeed.Normal;
        public bool started;
        public bool missed;
        public bool served;
        public float serveBeat;
        public float targetBeat;
        private bool inPose;

        public Paddlers paddlers;

        public GameEvent bop = new GameEvent();
        
        public static RhythmRally instance;

        private void Awake()
        {
            instance = this;
        }

        
        void Start()
        {
            renderQuadTrans.gameObject.SetActive(true);
            
            var cam = GameCamera.instance.camera;
            var camHeight = 2f * cam.orthographicSize;
            var camWidth = camHeight * cam.aspect;
            renderQuadTrans.localScale = new Vector3(camWidth, camHeight, 1f);

            playerAnim.Play("Idle", 0, 0);
            opponentAnim.Play("Idle", 0, 0);
        }

        const float tableHitTime = 0.58f;
        bool opponentServing = false; // Opponent serving this frame?
        void Update()
        {
            var cond = Conductor.instance;
            var currentBeat = cond.songPositionInBeats;
            
            var hitBeat = serveBeat; // Beat when the last paddler hit the ball
            var beatDur1 = 1f; // From paddle to table
            var beatDur2 = 1f; // From table to other paddle

            var playerState = playerAnim.GetCurrentAnimatorStateInfo(0);
            var opponentState = opponentAnim.GetCurrentAnimatorStateInfo(0);

            bool playerPrepping = false; // Player using prep animation?
            bool opponentPrepping = false; // Opponent using prep animation?

            if (started)
            {
                // Determine hitBeat and beatDurs.
                switch (rallySpeed)
                {
                    case RallySpeed.Normal:
                        if (!served)
                        {
                            hitBeat = serveBeat + 2f;
                        }
                        break;
                    case RallySpeed.Fast:
                        if (!served)
                        {
                            hitBeat = serveBeat + 1f;
                            beatDur1 = 1f;
                            beatDur2 = 2f;
                        }
                        else
                        {
                            beatDur1 = 0.5f;
                            beatDur2 = 0.5f;
                        }
                        break;
                    case RallySpeed.SuperFast:
                        if (!served)
                        {
                            hitBeat = serveBeat + 1f;
                        }

                        beatDur1 = 0.5f;
                        beatDur2 = 0.5f;
                        break;
                    case RallySpeed.Slow:
                        if (!served)
                        {
                            hitBeat = serveBeat + 4f;
                        }

                        beatDur1 = 2f;
                        beatDur2 = 2f;
                        break;
                }


                // Ball position.
                var curveToUse = served ? serveCurve : returnCurve;
                float curvePosition;

                var hitPosition1 = cond.GetPositionFromBeat(hitBeat, beatDur1);
                if (hitPosition1 >= 1f)
                {
                    var hitPosition2 = cond.GetPositionFromBeat(hitBeat + beatDur1, beatDur2);
                    curvePosition = tableHitTime + hitPosition2 * (1f - tableHitTime);
                }
                else
                {
                    curvePosition = hitPosition1 * tableHitTime;
                }

                if (!missed)
                {
                    float curveHeight = 1f;
                    if (rallySpeed == RallySpeed.Fast && served)
                        curveHeight = 0.5f;
                    else if (rallySpeed == RallySpeed.Fast && !served && hitPosition1 >= 1f)
                        curveHeight = 2f;
                    else if (rallySpeed == RallySpeed.Slow)
                        curveHeight = 3f;

                    curveToUse.transform.localScale = new Vector3(1f, curveHeight, 1f);
                    ball.transform.position = curveToUse.GetPoint(Mathf.Clamp(curvePosition, 0, 1));
                }

                // TODO: Make conditional so ball shadow only appears when over table.
                ballShadow.transform.position = new Vector3(ball.transform.position.x, -0.399f, ball.transform.position.z);


                var timeBeforeNextHit = hitBeat + beatDur1 + beatDur2 - currentBeat;

                // Check if the opponent should swing.
                if (!served && timeBeforeNextHit <= 0f)
                {
                    List<Beatmap.Entity> rallies = GameManager.instance.Beatmap.entities.FindAll(c => c.datamodel == "rhythmRally/rally" || c.datamodel == "rhythmRally/slow rally");
                    for (int i = 0; i < rallies.Count; i++)
                    {
                        var rally = rallies[i];
                        if (rally.beat - currentBeat <= 0f && rally.beat + rally.length - currentBeat > 0f)
                        {
                            Serve(hitBeat + beatDur1 + beatDur2, rallySpeed);
                            opponentServing = true;
                            break;
                        }
                    }
                }

                // Check if paddler should do ready animation.
                bool readyToPrep;
                if (rallySpeed == RallySpeed.Slow || (!served && rallySpeed == RallySpeed.Fast))
                    readyToPrep = timeBeforeNextHit <= 2f;
                else
                    readyToPrep = timeBeforeNextHit <= 1f;

                // Paddler ready animation.
                if (readyToPrep && !opponentServing && !inPose)
                {
                    if (served)
                    {
                        playerPrepping = true;
                        if ((playerState.IsName("Swing") && playerAnim.IsAnimationNotPlaying()) || (!playerState.IsName("Swing") && !playerState.IsName("Ready1")))
                            playerAnim.Play("Ready1");
                    }
                    else if (!opponentServing)
                    {
                        opponentPrepping = true;
                        if ((opponentState.IsName("Swing") && opponentAnim.IsAnimationNotPlaying()) || (!opponentState.IsName("Swing") && !opponentState.IsName("Ready1")))
                            opponentAnim.Play("Ready1");
                        
                        // If player never swung and is still in ready state, snap them out of it.
                        if (missed && playerState.IsName("Ready1"))
                            playerAnim.Play("Beat");
                    }
                }
            }


            // Paddler bop animation.
            if (cond.ReportBeat(ref bop.lastReportedBeat, bop.startBeat % 1))
            {
                if (currentBeat >= bop.startBeat && currentBeat < bop.startBeat + bop.length && !inPose)
                {
                    if (!playerPrepping && (playerAnim.IsAnimationNotPlaying() || playerState.IsName("Idle") || playerState.IsName("Beat")))
                        playerAnim.Play("Beat", 0, 0);

                    if (!opponentPrepping && !opponentServing && (opponentAnim.IsAnimationNotPlaying() || opponentState.IsName("Idle") || opponentState.IsName("Beat")))
                        opponentAnim.Play("Beat", 0, 0);
                }
            }

            opponentServing = false;
        }

        public void Bop(float beat, float length)
        {
            bop.length = length;
            bop.startBeat = beat;
        }

        public void Serve(float beat, RallySpeed speed)
        {
            served = true;
            missed = false;
            started = true;
            opponentServing = true;
            
            serveBeat = beat;
            rallySpeed = speed;

            var bounceBeat = 0f;

            switch (rallySpeed)
            {
                case RallySpeed.Normal:
                    targetBeat = serveBeat + 2f;
                    bounceBeat = serveBeat + 1f;
                    break;
                case RallySpeed.Fast:
                case RallySpeed.SuperFast:
                    targetBeat = serveBeat + 1f;
                    bounceBeat = serveBeat + 0.5f;
                    break;
                case RallySpeed.Slow:
                    targetBeat = serveBeat + 4f;
                    bounceBeat = serveBeat + 2f;
                    break;
            }

            opponentAnim.Play("Swing", 0, 0);
            MultiSound.Play(new MultiSound.Sound[] { new MultiSound.Sound("rhythmRally/Serve", serveBeat), new MultiSound.Sound("rhythmRally/ServeBounce", bounceBeat) });
            paddlers.BounceFX(bounceBeat);

            paddlers.ResetState();
        }

        public void Pose()
        {
            playerAnim.Play("Pose", 0, 0);
            opponentAnim.Play("Pose", 0, 0);
            ball.gameObject.SetActive(false); // temporary solution, should realistically just fall down
            inPose = true;
        }

        public void PrepareFastRally(float beat, RallySpeed speedChange)
        {
            if (speedChange == RallySpeed.Fast)
            {
                BeatAction.New(gameObject, new List<BeatAction.Action>()
                {
                    new BeatAction.Action(beat + 2f, delegate { Serve(beat + 2f, RallySpeed.Fast); })
                });

                MultiSound.Play(new MultiSound.Sound[]
                {
                    new MultiSound.Sound("rhythmRally/Tonk", beat),
                    new MultiSound.Sound("rhythmRally/Tink", beat + 0.5f),
                    new MultiSound.Sound("rhythmRally/Tonk", beat + 1f)
                });
            }
            else if (speedChange == RallySpeed.SuperFast)
            {
                BeatAction.New(gameObject, new List<BeatAction.Action>()
                {
                    new BeatAction.Action(beat + 4f, delegate { Serve(beat + 4f, RallySpeed.SuperFast); }),
                    new BeatAction.Action(beat + 6f, delegate { Serve(beat + 6f, RallySpeed.SuperFast); }),
                    new BeatAction.Action(beat + 8f, delegate { Serve(beat + 8f, RallySpeed.SuperFast); }),
                    new BeatAction.Action(beat + 10f, delegate { Serve(beat + 10f, RallySpeed.SuperFast); })
                });

                MultiSound.Play(new MultiSound.Sound[]
                {
                    new MultiSound.Sound("rhythmRally/Tonk", beat),
                    new MultiSound.Sound("rhythmRally/Tink", beat + 0.5f),
                    new MultiSound.Sound("rhythmRally/Tonk", beat + 1f),
                    new MultiSound.Sound("rhythmRally/Tink", beat + 1.5f),
                    new MultiSound.Sound("rhythmRally/Tonk", beat + 2f),
                    new MultiSound.Sound("rhythmRally/Tink", beat + 2.5f),
                    new MultiSound.Sound("rhythmRally/Tonk", beat + 3f),
                    new MultiSound.Sound("rhythmRally/Tink", beat + 3.5f)
                });
            }
        }
    }
}