using UnityEngine;
using UnityEngine.UI;
using ControlFreak2;

/// <summary>
/// TouchJoystickAnimator ke child mein lagao. Joystick ki direction ke hisaab se
/// 4 focus images (left top, right top, right bottom, left bottom) on/off karta hai.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class JoystickDirectionFocusUI : MonoBehaviour
{
    [Header("Joystick")]
    [Tooltip("Joystick reference; khali choro to parent se TouchJoystick dhundh liya jayega.")]
    public TouchJoystick joystick;

    [Header("Focus Images (4 corners)")]
    [Tooltip("Left Top - jab stick UL (up-left) ho.")]
    public GameObject focusLeftTop;

    [Tooltip("Right Top - jab stick UR (up-right) ho.")]
    public GameObject focusRightTop;

    [Tooltip("Right Bottom - jab stick DR (down-right) ho.")]
    public GameObject focusRightBottom;

    [Tooltip("Left Bottom - jab stick DL (down-left) ho.")]
    public GameObject focusLeftBottom;

    [Header("Threshold")]
    [Tooltip("Kitna tilt hone par direction count ho (0-1).")]
    [Range(0.01f, 1f)]
    public float directionThreshold = 0.3f;

    private void Awake()
    {
        if (joystick == null)
        {
            joystick = GetComponentInParent<TouchJoystick>();
        }
    }

    private void Update()
    {
        UpdateFocusVisibility();
    }

    private void UpdateFocusVisibility()
    {
        bool showLT = false;
        bool showRT = false;
        bool showRB = false;
        bool showLB = false;

        if (joystick != null)
        {
            Vector2 vec = joystick.GetVector();
            float mag = vec.magnitude;

            if (mag >= directionThreshold)
            {
                Dir dir = joystick.GetState().GetDir8();

                switch (dir)
                {
                    case Dir.UL:
                        showLT = true;
                        break;
                    case Dir.UR:
                        showRT = true;
                        break;
                    case Dir.DR:
                        showRB = true;
                        break;
                    case Dir.DL:
                        showLB = true;
                        break;
                    case Dir.U:
                        showLT = true;
                        showRT = true;
                        break;
                    case Dir.R:
                        showRT = true;
                        showRB = true;
                        break;
                    case Dir.D:
                        showRB = true;
                        showLB = true;
                        break;
                    case Dir.L:
                        showLB = true;
                        showLT = true;
                        break;
                    default:
                        break;
                }
            }
        }

        if (focusLeftTop != null)
            focusLeftTop.SetActive(showLT);
        if (focusRightTop != null)
            focusRightTop.SetActive(showRT);
        if (focusRightBottom != null)
            focusRightBottom.SetActive(showRB);
        if (focusLeftBottom != null)
            focusLeftBottom.SetActive(showLB);
    }
}
