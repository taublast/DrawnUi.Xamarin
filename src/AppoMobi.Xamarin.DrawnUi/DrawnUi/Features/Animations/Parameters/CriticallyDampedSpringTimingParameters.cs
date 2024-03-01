// Credits to https://github.com/super-ultra

using DrawnUi.Maui.Infrastructure;
using System.Numerics;

namespace DrawnUi.Maui.Draw;

public struct CriticallyDampedSpringTimingParameters : IDampingTimingParameters
{
    private readonly Spring spring;
    private readonly Vector2 displacement;
    private readonly Vector2 initialVelocity;
    private readonly float threshold;

    public CriticallyDampedSpringTimingParameters(Spring spring, Vector2 displacement, Vector2 initialVelocity, float threshold)
    {
        this.spring = spring;
        this.displacement = displacement;
        this.initialVelocity = initialVelocity;
        this.threshold = threshold;
    }

    public float DurationSecs
    {
        get
        {
            if (displacement.Length() == 0 && initialVelocity.Length() == 0)
            {
                return 0;
            }

            float b = spring.Beta();
            float e = (float)Math.Exp(1);

            float t1 = 1 / b * (float)Math.Log(2 * c1.Length() / threshold);
            float t2 = 2 / b * (float)Math.Log(4 * c2.Length() / (e * b * threshold));

            return Math.Max(t1, t2);
        }
    }

    public Vector2 ValueAt(float offsetSecs)
    {
        float t = offsetSecs;
        return (float)Math.Exp(-spring.Beta() * t) * (c1 + c2 * t);
    }

    public Vector2 AmplitudeAt(float offsetSecs)
    {
        var value = ValueAt(offsetSecs);
        return new(Math.Abs(value.X), Math.Abs(value.Y));
    }

    private Vector2 c1 => displacement;
    private Vector2 c2 => initialVelocity + spring.Beta() * displacement;
}