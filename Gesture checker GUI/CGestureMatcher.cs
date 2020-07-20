using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Leap;
using GlmNet;

namespace Guesture_checker_GUI
{
    class CGestureMatcher
    {
        Vector g_InVector = new Vector(0, 1, 0);
        Vector g_RightVector = new Vector(-1, 0, 0);
        Vector g_UpVector = new Vector(0, 0, -1);
        public enum GestureHand : uint
        {
            GH_LeftHand = 0U,
            GH_RightHand
        };

        public enum GestureType : uint
        {
            // Default
            GT_TriggerFinger = 0U,
            GT_LowerFist,
            GT_Pinch,
            GT_Thumbpress,
            GT_Victory,
            GT_ThumbUp,
            GT_ThumbInward,
            GT_ThumbMiddleTouch,
            GT_ThumbPinkyTouch,

            // Hand orientation
            GT_FlatHandPalmUp,
            GT_FlatHandPalmDown,
            GT_FlatHandPalmAway,
            GT_FlatHandPalmTowards,

            // Two-handed
            GT_Timeout,
            GT_TouchpadAxisX,
            GT_TouchpadAxisY,
            GT_ThumbIndexCrossTouch,

            // VRChat specific
            GT_VRChatPoint,
            GT_VRChatRockOut,
            GT_VRChatSpreadHand,
            GT_VRChatGun,
            GT_VRChatThumbsUp,
            GT_VRChatVictory,

            // Utility
            GT_IndexFingerBend,
            GT_MiddleFingerBend,
            GT_RingFingerBend,
            GT_PinkyFingerBend,

            GT_GesturesCount,
            GT_Invalid = 0xFFU
        };

        public struct FingerData
        {
            public float m_bend;
            public float[] m_bends;
            public Vector m_direction;
            public Vector m_tipPosition;
            public bool m_extended;
        };

        public bool GetGestures(ref Frame f_frame, GestureHand f_which, ref float[] f_result)
        {
            bool l_result = false;

            List<Hand> l_hands = f_frame.Hands;
            foreach (Hand l_hand in l_hands)
            {
                if ((l_hand.IsLeft == (f_which == GestureHand.GH_LeftHand)) || (l_hand.IsRight == (f_which == GestureHand.GH_RightHand)))
                {
                    FingerData[] l_fingerData = new FingerData[5U];
                    for( uint i = 0; i < 5U; ++i ) l_fingerData[i].m_bends = new float[3];

                    List<Finger> l_fingers = l_hand.Fingers;
                    foreach (Finger l_finger in l_fingers)
                    {
                        uint l_index = (uint)(l_finger.Type);

                        l_fingerData[l_index].m_tipPosition = l_finger.TipPosition;
                        l_fingerData[l_index].m_extended = l_finger.IsExtended;

                        Vector l_prevDirection = new Vector();
                        Vector l_direction1 = new Vector();
                        for (int i = 0; i < 4; i++)
                        {
                            Bone l_bone = l_finger.Bone((Leap.Bone.BoneType)(i));
                            l_direction1 = -l_bone.Direction;

                            if (i == (uint)Leap.Bone.BoneType.TYPE_DISTAL)
                                l_fingerData[l_index].m_direction = l_direction1;
                            if (i > 0)
                            {
                                l_fingerData[l_index].m_bends[i - 1] = 57.2957795f * l_direction1.AngleTo(l_prevDirection);
                                l_fingerData[l_index].m_bend += l_fingerData[l_index].m_bends[i - 1];
                            }
                            l_prevDirection = l_direction1;
                        }
                    }

                    // Trigger
                    float l_triggerBend = l_fingerData[(int)Leap.Finger.FingerType.TYPE_INDEX].m_bends[1U] + l_fingerData[(int)Leap.Finger.FingerType.TYPE_INDEX].m_bends[2U];
                    float l_trigger = MapRange(l_triggerBend, 70f, 100f);
                    Merge(ref f_result[(int)GestureType.GT_TriggerFinger], l_trigger);

                    // Lower fist / grip
                    float l_grip = MapRange((l_fingerData[(int)Leap.Finger.FingerType.TYPE_MIDDLE].m_bend + l_fingerData[(int)Leap.Finger.FingerType.TYPE_RING].m_bend + l_fingerData[(int)Leap.Finger.FingerType.TYPE_PINKY].m_bend) / 3f, 90f, 180f);
                    Merge(ref f_result[(int)GestureType.GT_LowerFist], l_grip);

                    // Pinch
                    float l_pinch = MapRange(l_hand.PinchDistance, 40f, 30f);
                    Merge(ref f_result[(int)GestureType.GT_Pinch], l_pinch);

                    // Thumb press
                    Vector l_palmNormal = l_hand.PalmNormal;
                    Vector l_direction = l_hand.Direction;
                    Vector l_pinkySide;
                    if (f_which == GestureHand.GH_RightHand) l_pinkySide = l_palmNormal.Cross(l_direction);
                    else l_pinkySide = l_direction.Cross(l_palmNormal);
                    Merge(ref f_result[(int)GestureType.GT_Thumbpress], 1f - MapRange(l_pinkySide.Dot(l_fingerData[(int)Leap.Finger.FingerType.TYPE_THUMB].m_direction), 0.0f, 0.6f));

                    // Victory
                    Merge(ref f_result[(int)GestureType.GT_Victory], Math.Min(Math.Min(MapRange((l_fingerData[(int)Leap.Finger.FingerType.TYPE_INDEX].m_bend + l_fingerData[(int)Leap.Finger.FingerType.TYPE_MIDDLE].m_bend) / 2f, 50f, 40f),
                    MapRange((l_fingerData[(int)Leap.Finger.FingerType.TYPE_PINKY].m_bend + l_fingerData[(int)Leap.Finger.FingerType.TYPE_RING].m_bend) / 2f, 120f, 150f)),
                    MapRange(57.2957795f * l_fingerData[(int)Leap.Finger.FingerType.TYPE_INDEX].m_direction.AngleTo(l_fingerData[(int)Leap.Finger.FingerType.TYPE_MIDDLE].m_direction), 10f, 20f)));

                    // Flat hand gestures
                    float l_flatHand = MapRange((l_fingerData[(int)Leap.Finger.FingerType.TYPE_THUMB].m_bend + l_fingerData[(int)Leap.Finger.FingerType.TYPE_INDEX].m_bend + l_fingerData[(int)Leap.Finger.FingerType.TYPE_MIDDLE].m_bend + l_fingerData[(int)Leap.Finger.FingerType.TYPE_RING].m_bend + l_fingerData[(int)Leap.Finger.FingerType.TYPE_PINKY].m_bend) / 5f, 50f, 40f);
                    Merge(ref f_result[(int)GestureType.GT_FlatHandPalmUp], Math.Min(l_flatHand, MapRange((g_UpVector).Dot(l_palmNormal), 0.8f, 0.95f)));
                    Merge(ref f_result[(int)GestureType.GT_FlatHandPalmDown], Math.Min(l_flatHand, MapRange((-g_UpVector).Dot(l_palmNormal), 0.8f, 0.95f)));
                    Merge(ref f_result[(int)GestureType.GT_FlatHandPalmAway], Math.Min(l_flatHand, MapRange((g_InVector).Dot(l_palmNormal), 0.8f, 0.95f)));
                    Merge(ref f_result[(int)GestureType.GT_FlatHandPalmTowards], Math.Min(l_flatHand, MapRange((-g_InVector).Dot(l_palmNormal), 0.8f, 0.95f)));

                    // ThumbsUp/Inward gestures (seems broken in new LeapSDK)
                    Vector l_inward = ((f_which == GestureHand.GH_LeftHand) ? g_RightVector : -g_RightVector);
                    float l_fistHand = MapRange((l_fingerData[(int)Leap.Finger.FingerType.TYPE_INDEX].m_bend + l_fingerData[(int)Leap.Finger.FingerType.TYPE_MIDDLE].m_bend + l_fingerData[(int)Leap.Finger.FingerType.TYPE_RING].m_bend + l_fingerData[(int)Leap.Finger.FingerType.TYPE_PINKY].m_bend) / 5f, 120f, 150f);
                    float l_straightThumb = MapRange(l_fingerData[(int)Leap.Finger.FingerType.TYPE_THUMB].m_bend, 50f, 40f);
                    Merge(ref f_result[(int)GestureType.GT_ThumbUp], Math.Min(l_fistHand, Math.Min(l_straightThumb, MapRange((g_UpVector).Dot(l_fingerData[(int)Leap.Finger.FingerType.TYPE_THUMB].m_direction), 0.8f, 0.95f))));
                    Merge(ref f_result[(int)GestureType.GT_ThumbInward], Math.Min(l_fistHand, Math.Min(l_straightThumb, MapRange((l_inward).Dot(l_fingerData[(int)Leap.Finger.FingerType.TYPE_THUMB].m_direction), 0.8f, 0.95f))));

                    // VRChat gestures
                    f_result[(int)GestureType.GT_VRChatPoint] = (!l_fingerData[(int)Leap.Finger.FingerType.TYPE_THUMB].m_extended && l_fingerData[(int)Leap.Finger.FingerType.TYPE_INDEX].m_extended && !l_fingerData[(int)Leap.Finger.FingerType.TYPE_MIDDLE].m_extended && !l_fingerData[(int)Leap.Finger.FingerType.TYPE_RING].m_extended && !l_fingerData[(int)Leap.Finger.FingerType.TYPE_PINKY].m_extended) ? 1f : 0f;
                    f_result[(int)GestureType.GT_VRChatGun] = (l_fingerData[(int)Leap.Finger.FingerType.TYPE_THUMB].m_extended && l_fingerData[(int)Leap.Finger.FingerType.TYPE_INDEX].m_extended && !l_fingerData[(int)Leap.Finger.FingerType.TYPE_MIDDLE].m_extended && !l_fingerData[(int)Leap.Finger.FingerType.TYPE_RING].m_extended && !l_fingerData[(int)Leap.Finger.FingerType.TYPE_PINKY].m_extended) ? 1f : 0f;
                    f_result[(int)GestureType.GT_VRChatSpreadHand] = (l_fingerData[(int)Leap.Finger.FingerType.TYPE_THUMB].m_extended && l_fingerData[(int)Leap.Finger.FingerType.TYPE_INDEX].m_extended && l_fingerData[(int)Leap.Finger.FingerType.TYPE_MIDDLE].m_extended && l_fingerData[(int)Leap.Finger.FingerType.TYPE_RING].m_extended && l_fingerData[(int)Leap.Finger.FingerType.TYPE_PINKY].m_extended && (l_hand.GrabAngle < Math.PI / 6f)) ? 1f : 0f;
                    f_result[(int)GestureType.GT_VRChatThumbsUp] = (l_fingerData[(int)Leap.Finger.FingerType.TYPE_THUMB].m_extended && !l_fingerData[(int)Leap.Finger.FingerType.TYPE_INDEX].m_extended && !l_fingerData[(int)Leap.Finger.FingerType.TYPE_MIDDLE].m_extended && !l_fingerData[(int)Leap.Finger.FingerType.TYPE_RING].m_extended && !l_fingerData[(int)Leap.Finger.FingerType.TYPE_PINKY].m_extended) ? 1f : 0f;
                    f_result[(int)GestureType.GT_VRChatRockOut] = (!l_fingerData[(int)Leap.Finger.FingerType.TYPE_THUMB].m_extended && l_fingerData[(int)Leap.Finger.FingerType.TYPE_INDEX].m_extended && !l_fingerData[(int)Leap.Finger.FingerType.TYPE_MIDDLE].m_extended && !l_fingerData[(int)Leap.Finger.FingerType.TYPE_RING].m_extended && l_fingerData[(int)Leap.Finger.FingerType.TYPE_PINKY].m_extended) ? 1f : 0f;
                    f_result[(int)GestureType.GT_VRChatVictory] = (!l_fingerData[(int)Leap.Finger.FingerType.TYPE_THUMB].m_extended && l_fingerData[(int)Leap.Finger.FingerType.TYPE_INDEX].m_extended && l_fingerData[(int)Leap.Finger.FingerType.TYPE_MIDDLE].m_extended && !l_fingerData[(int)Leap.Finger.FingerType.TYPE_RING].m_extended && !l_fingerData[(int)Leap.Finger.FingerType.TYPE_PINKY].m_extended) ? 1f : 0f;

                    // Finger bends
                    f_result[(int)GestureType.GT_IndexFingerBend] = MapRange(l_fingerData[(int)Leap.Finger.FingerType.TYPE_INDEX].m_bend, 90f, 180f);
                    f_result[(int)GestureType.GT_MiddleFingerBend] = MapRange(l_fingerData[(int)Leap.Finger.FingerType.TYPE_MIDDLE].m_bend, 90f, 180f);
                    f_result[(int)GestureType.GT_RingFingerBend] = MapRange(l_fingerData[(int)Leap.Finger.FingerType.TYPE_RING].m_bend, 90f, 180f);
                    f_result[(int)GestureType.GT_PinkyFingerBend] = MapRange(l_fingerData[(int)Leap.Finger.FingerType.TYPE_PINKY].m_bend, 90f, 180f);

                    float l_length = l_fingerData[(int)Leap.Finger.FingerType.TYPE_THUMB].m_tipPosition.DistanceTo(l_fingerData[(int)Leap.Finger.FingerType.TYPE_MIDDLE].m_tipPosition);
                    f_result[(int)GestureType.GT_ThumbMiddleTouch] = (l_length <= 35f) ? Math.Min((35f - l_length) / 20f, 1f) : 0f;
                    l_length = l_fingerData[(int)Leap.Finger.FingerType.TYPE_THUMB].m_tipPosition.DistanceTo(l_fingerData[(int)Leap.Finger.FingerType.TYPE_PINKY].m_tipPosition);
                    f_result[(int)GestureType.GT_ThumbPinkyTouch] = (l_length <= 35f) ? Math.Min((35f - l_length) / 20f, 1f) : 0f;

                    // Two-handed gestures
                    foreach (Hand l_otherHand in l_hands)
                    {
                        if (((f_which == GestureHand.GH_LeftHand) && l_otherHand.IsRight) || ((f_which == GestureHand.GH_RightHand) && l_otherHand.IsLeft))
                        {
                            Merge(ref f_result[(int)GestureType.GT_Timeout], Math.Min(l_flatHand,  // I reuse the flatHand metric from above
                                Math.Min(MapRange(l_direction.Dot(-l_otherHand.PalmNormal), 0.8f, 0.95f),
                                MapRange(l_fingerData[(int)Leap.Finger.FingerType.TYPE_INDEX].m_tipPosition.DistanceTo(l_otherHand.PalmPosition), 80.0f, 60.0f))
                                ));

                            List<Finger> l_otherFingers = l_otherHand.Fingers;
                            foreach (Finger l_otherFinger in l_otherFingers)
                            {
                                if (l_otherFinger.Type == Leap.Finger.FingerType.TYPE_INDEX)
                                {
                                    if (l_otherFinger.Direction.Dot(l_palmNormal) < 0)
                                    {
                                        Vector l_uVec = l_direction.Cross(l_palmNormal) * (l_hand.PalmWidth / 2f);
                                        Vector l_vVec = l_direction * (l_hand.PalmWidth / 2f);
                                        Vector l_path = l_otherFinger.TipPosition - l_hand.PalmPosition;
                                        Vector l_otherFingerDir = l_otherFinger.Direction;

                                        mat3 l_matrix = new mat3(new vec3(l_uVec.x, l_vVec.x, l_otherFingerDir.x),
                                            new vec3(l_uVec.y, l_vVec.y, l_otherFingerDir.y),
                                            new vec3(l_uVec.z, l_vVec.z, l_otherFingerDir.z));
                                        vec3 l_uv = glm.inverse(l_matrix) * new vec3(l_path.ToFloatArray()[0], l_path.ToFloatArray()[1], l_path.ToFloatArray()[2]);
                                        l_length = (float)Math.Sqrt(l_uv[0] * l_uv[0] + l_uv[1] * l_uv[1]);
                                        if (l_length < 5f)
                                        {
                                            if (l_length > 1f) l_uv /= l_length;
                                            f_result[(int)GestureType.GT_TouchpadAxisX] = l_uv.x;
                                            f_result[(int)GestureType.GT_TouchpadAxisY] = l_uv.y;
                                        }
                                    }

                                    l_length = l_fingerData[(int)Leap.Finger.FingerType.TYPE_THUMB].m_tipPosition.DistanceTo(l_otherFinger.TipPosition);
                                    f_result[(int)GestureType.GT_ThumbIndexCrossTouch] = (l_length <= 35f) ? Math.Max((35f - l_length) / 20f, 1f) : 0f;

                                    break;
                                }
                            }
                            break;
                        }
                    }

                    l_result = true;
                    break;
                }
            }
            return l_result;
        }

        public float MapRange(float input, float minimum, float maximum)
        {
            float mapped = (input - minimum) / (maximum - minimum);
            return Math.Max(Math.Min(mapped, 1.0f), 0.0f);
        }
        public void Merge(ref float result, float value)
        {
            result = Math.Max(result, value);
        }

        public void GetGestureName(GestureType f_gesture, ref string f_name)
        {
            switch (f_gesture)
            {
                case GestureType.GT_TriggerFinger: f_name = ("TriggerFinger"); break;
                case GestureType.GT_LowerFist: f_name = ("LowerFist"); break;
                case GestureType.GT_Pinch: f_name = ("Pinch"); break;
                case GestureType.GT_Thumbpress: f_name = ("Thumbpress"); break;
                case GestureType.GT_Victory: f_name = ("Victory"); break;
                case GestureType.GT_ThumbUp: f_name = ("ThumbUp"); break;
                case GestureType.GT_ThumbInward: f_name = ("ThumbInward"); break;
                case GestureType.GT_ThumbMiddleTouch: f_name = ("ThumbMiddleTouch"); break;
                case GestureType.GT_ThumbPinkyTouch: f_name = ("ThumbPinkyTouch"); break;
                case GestureType.GT_FlatHandPalmUp: f_name = ("FlatHandPalmUp"); break;
                case GestureType.GT_FlatHandPalmDown: f_name = ("FlatHandPalmDown"); break;
                case GestureType.GT_FlatHandPalmAway: f_name = ("FlatHandPalmAway"); break;
                case GestureType.GT_FlatHandPalmTowards: f_name = ("FlatHandPalmTowards"); break;
                case GestureType.GT_Timeout: f_name = ("Timeout"); break;
                case GestureType.GT_TouchpadAxisX: f_name = ("TouchpadAxisX"); break;
                case GestureType.GT_TouchpadAxisY: f_name = ("TouchpadAxisY"); break;
                case GestureType.GT_ThumbIndexCrossTouch: f_name = ("ThumbIndexCrossTouch"); break;
                case GestureType.GT_VRChatGun: f_name = ("VRChatGun"); break;
                case GestureType.GT_VRChatPoint: f_name = ("VRChatPoint"); break;
                case GestureType.GT_VRChatRockOut: f_name = ("VRChatRockOut"); break;
                case GestureType.GT_VRChatSpreadHand: f_name = ("VRChatSpreadHand"); break;
                case GestureType.GT_VRChatThumbsUp: f_name = ("VRChatThumbsUp"); break;
                case GestureType.GT_VRChatVictory: f_name = ("VRChatVictory"); break;
                case GestureType.GT_IndexFingerBend: f_name = ("IndexFingerBend"); break;
                case GestureType.GT_MiddleFingerBend: f_name = ("MiddleFingerBend"); break;
                case GestureType.GT_RingFingerBend: f_name = ("RingFingerBend"); break;
                case GestureType.GT_PinkyFingerBend: f_name = ("PinkyFingerBend"); break;
            }
        }
    }
}
