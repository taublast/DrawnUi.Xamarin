﻿// Credits to https://github.com/super-ultra

using System.Numerics;

namespace DrawnUi.Maui.Draw;

public struct UnderdampedSpringTimingVectorParameters : IDampingTimingVectorParameters
{
	private readonly Spring spring;
	private readonly Vector2 displacement;
	private readonly Vector2 initialVelocity;
	private readonly float threshold;

	public UnderdampedSpringTimingVectorParameters(Spring spring, Vector2 displacement, Vector2 initialVelocity, float threshold)
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

			return (float)(Math.Log((c1.Length() + c2.Length()) / threshold) / spring.Beta());
		}
	}


	public Vector2 ValueAt(float offsetSecs)
	{
		float t = offsetSecs;
		float wd = spring.DampedNaturalFrequency();
		double expValue = Math.Exp(-spring.Beta() * t);
		float expValueFloat = (float)expValue;
		float cosValue = (float)Math.Cos(wd * t);
		float sinValue = (float)Math.Sin(wd * t);
		return expValueFloat * (c1 * cosValue + c2 * sinValue);
	}

	public Vector2 AmplitudeAt(float offsetSecs)
	{
		var value = ValueAt(offsetSecs);
		return new(Math.Abs(value.X), Math.Abs(value.Y));
	}

	private Vector2 c1 => displacement;
	private Vector2 c2 => (initialVelocity + spring.Beta() * displacement) / spring.DampedNaturalFrequency();
}