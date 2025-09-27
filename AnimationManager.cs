using UnityEngine;
using System.Collections;

namespace SplitsStats;

public class AnimationManager : MonoBehaviour
{
    private static int timersCurrentlyAnimated = 0;

    /// <summary>
    /// Returns true if any timer is currently being animated.
    /// </summary>
    public static bool AreTimersCurrentlyAnimated()
    { return timersCurrentlyAnimated > 0; }

    /// <summary>
    /// Perform a cubic interpolation between two values given a value of t.
    /// Interpolation is guaranteed to be C1 continuous for any input but this is also Cinf continuous if centralVelocity = 1.5f.
    /// Note: It's expected that t in [0.0, 1.0] but this isn't enforced.
    /// </summary>
    /// <param name="a"> The value at t=0f </param>
    /// <param name="b"> The value at t=1f </param>
    /// <param name="t"> The blending factor of interpolation </param>
    /// <param name="centralVelocity"> The velocity/derivative of the cubic curve at t=0.5f assuming (b - a) = 1f
    /// Note: centralVelocity in [1.5, 3.0] should be true.
    /// If it isn't, the direction of curvature will change more than once within t = (0,1) and 
    /// create an unnatural interpolation curve.</param>
    public static float CubicInterpolation(float a, float b, float t, float centralVelocity = 1.5f)
    {
        float v = (b - a) * centralVelocity;
        float t_3 = t * t * t;
        float t_2 = t * t;
        
        if (t < 0.5f)
        {
            float c_3 = 8*a - 8*b + 4*v;
            float c_2 = -6*a + 6*b - 2*v;
            float c_0 = a;

            return c_3 * t_3 + c_2 * t_2 + c_0;
        }
        else
        {
            float c_3 = 8*a - 8*b + 4*v;
            float c_2 = -18*a + 18*b - 10*v;
            float c_1 = 12*a - 12*b + 8*v;
            float c_0 = -2*a + 3*b - 2*v;

            return c_3 * t_3 + c_2 * t_2 + c_1 * t + c_0;
        }
    }

    // Run the coroutine of the interpolated scaling of the timer fontsize.
    public void LerpTimerFontSize(TimerComponent timer, float newFontSize, float duration)
    {
        StartCoroutine(LerpTimerFontSizeCoroutine(timer, newFontSize, duration));
    }

    public IEnumerator LerpTimerFontSizeCoroutine(TimerComponent timer, float newFontSize, float duration)
    {
        if (timer == null || newFontSize < 0.0f) yield break;

        AnimationManager.timersCurrentlyAnimated ++;
        float initialFontSize = timer.GetHeight();
        float startingTime = Time.time;

        while (Time.time - startingTime < duration)
        {
            float currFontSize = CubicInterpolation(initialFontSize, newFontSize, (Time.time - startingTime) / duration, 2.5f);
            timer.SetHeight(currFontSize);
            yield return null;
        }
        timer.SetHeight(newFontSize);
        AnimationManager.timersCurrentlyAnimated --;
    }
}