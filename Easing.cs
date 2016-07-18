using UnityEngine;

public static class Easing
{
	public delegate float EasingFunction(float start, float end, float Value);

	public enum EaseType
	{
		easeInQuad,
		easeOutQuad,
		easeInOutQuad,
		easeInCubic,
		easeOutCubic,
		easeInOutCubic,
		easeInQuart,
		easeOutQuart,
		easeInOutQuart,
		easeInQuint,
		easeOutQuint,
		easeInOutQuint,
		easeInSine,
		easeOutSine,
		easeInOutSine,
		easeInExpo,
		easeOutExpo,
		easeInOutExpo,
		easeInCirc,
		easeOutCirc,
		easeInOutCirc,
		linear,
		spring,
		/* GFX47 MOD START */
		//bounce,
		easeInBounce,
		easeOutBounce,
		easeInOutBounce,
		/* GFX47 MOD END */
		easeInBack,
		easeOutBack,
		easeInOutBack,
		/* GFX47 MOD START */
		//elastic,
		easeInElastic,
		easeOutElastic,
		easeInOutElastic,
		/* GFX47 MOD END */
		punch,
		custom
	}

	public static EasingFunction GetEasingFunction(AnimationCurve curve)
	{
		return new EasingFunction( (start, end, value) => Mathf.Lerp(start, end, Mathf.Clamp01(curve.Evaluate(value))));
	}

	public static EasingFunction GetEasingFunction(EaseType easeType)
	{
		EasingFunction ease = null;

		switch (easeType)
		{
			case EaseType.easeInQuad:
				ease = new EasingFunction(easeInQuad);
				break;
			case EaseType.easeOutQuad:
				ease = new EasingFunction(easeOutQuad);
				break;
			case EaseType.easeInOutQuad:
				ease = new EasingFunction(easeInOutQuad);
				break;
			case EaseType.easeInCubic:
				ease = new EasingFunction(easeInCubic);
				break;
			case EaseType.easeOutCubic:
				ease = new EasingFunction(easeOutCubic);
				break;
			case EaseType.easeInOutCubic:
				ease = new EasingFunction(easeInOutCubic);
				break;
			case EaseType.easeInQuart:
				ease = new EasingFunction(easeInQuart);
				break;
			case EaseType.easeOutQuart:
				ease = new EasingFunction(easeOutQuart);
				break;
			case EaseType.easeInOutQuart:
				ease = new EasingFunction(easeInOutQuart);
				break;
			case EaseType.easeInQuint:
				ease = new EasingFunction(easeInQuint);
				break;
			case EaseType.easeOutQuint:
				ease = new EasingFunction(easeOutQuint);
				break;
			case EaseType.easeInOutQuint:
				ease = new EasingFunction(easeInOutQuint);
				break;
			case EaseType.easeInSine:
				ease = new EasingFunction(easeInSine);
				break;
			case EaseType.easeOutSine:
				ease = new EasingFunction(easeOutSine);
				break;
			case EaseType.easeInOutSine:
				ease = new EasingFunction(easeInOutSine);
				break;
			case EaseType.easeInExpo:
				ease = new EasingFunction(easeInExpo);
				break;
			case EaseType.easeOutExpo:
				ease = new EasingFunction(easeOutExpo);
				break;
			case EaseType.easeInOutExpo:
				ease = new EasingFunction(easeInOutExpo);
				break;
			case EaseType.easeInCirc:
				ease = new EasingFunction(easeInCirc);
				break;
			case EaseType.easeOutCirc:
				ease = new EasingFunction(easeOutCirc);
				break;
			case EaseType.easeInOutCirc:
				ease = new EasingFunction(easeInOutCirc);
				break;
			case EaseType.linear:
				ease = new EasingFunction(linear);
				break;
			case EaseType.spring:
				ease = new EasingFunction(spring);
				break;
			/* GFX47 MOD START */
			/*case EaseType.bounce:
				ease = new EasingFunction(bounce);
				break;*/
			case EaseType.easeInBounce:
				ease = new EasingFunction(easeInBounce);
				break;
			case EaseType.easeOutBounce:
				ease = new EasingFunction(easeOutBounce);
				break;
			case EaseType.easeInOutBounce:
				ease = new EasingFunction(easeInOutBounce);
				break;
			/* GFX47 MOD END */
			case EaseType.easeInBack:
				ease = new EasingFunction(easeInBack);
				break;
			case EaseType.easeOutBack:
				ease = new EasingFunction(easeOutBack);
				break;
			case EaseType.easeInOutBack:
				ease = new EasingFunction(easeInOutBack);
				break;
			/* GFX47 MOD START */
			/*case EaseType.elastic:
				ease = new EasingFunction(elastic);
				break;*/
			case EaseType.easeInElastic:
				ease = new EasingFunction(easeInElastic);
				break;
			case EaseType.easeOutElastic:
				ease = new EasingFunction(easeOutElastic);
				break;
			case EaseType.easeInOutElastic:
				ease = new EasingFunction(easeInOutElastic);
				break;
			/* GFX47 MOD END */
		}

		return ease;
	}

	#region Easing Curves

	public static float linear(float start, float end, float value)
	{
		return Mathf.Lerp(start, end, value);
	}

	public static float clerp(float start, float end, float value)
	{
		float min = 0.0f;
		float max = 360.0f;
		float half = Mathf.Abs((max - min) * 0.5f);
		float retval = 0.0f;
		float diff = 0.0f;
		if ((end - start) < -half)
		{
			diff = ((max - start) + end) * value;
			retval = start + diff;
		}
		else if ((end - start) > half)
		{
			diff = -((max - end) + start) * value;
			retval = start + diff;
		}
		else retval = start + (end - start) * value;
		return retval;
	}

	public static float spring(float start, float end, float value)
	{
		value = Mathf.Clamp01(value);
		value = (Mathf.Sin(value * Mathf.PI * (0.2f + 2.5f * value * value * value)) * Mathf.Pow(1f - value, 2.2f) + value) * (1f + (1.2f * (1f - value)));
		return start + (end - start) * value;
	}

	public static float easeInQuad(float start, float end, float value)
	{
		end -= start;
		return end * value * value + start;
	}

	public static float easeOutQuad(float start, float end, float value)
	{
		end -= start;
		return -end * value * (value - 2) + start;
	}

	public static float easeInOutQuad(float start, float end, float value)
	{
		value /= .5f;
		end -= start;
		if (value < 1) return end * 0.5f * value * value + start;
		value--;
		return -end * 0.5f * (value * (value - 2) - 1) + start;
	}

	public static float easeInCubic(float start, float end, float value)
	{
		end -= start;
		return end * value * value * value + start;
	}

	public static float easeOutCubic(float start, float end, float value)
	{
		value--;
		end -= start;
		return end * (value * value * value + 1) + start;
	}

	public static float easeInOutCubic(float start, float end, float value)
	{
		value /= .5f;
		end -= start;
		if (value < 1) return end * 0.5f * value * value * value + start;
		value -= 2;
		return end * 0.5f * (value * value * value + 2) + start;
	}

	public static float easeInQuart(float start, float end, float value)
	{
		end -= start;
		return end * value * value * value * value + start;
	}

	public static float easeOutQuart(float start, float end, float value)
	{
		value--;
		end -= start;
		return -end * (value * value * value * value - 1) + start;
	}

	public static float easeInOutQuart(float start, float end, float value)
	{
		value /= .5f;
		end -= start;
		if (value < 1) return end * 0.5f * value * value * value * value + start;
		value -= 2;
		return -end * 0.5f * (value * value * value * value - 2) + start;
	}

	public static float easeInQuint(float start, float end, float value)
	{
		end -= start;
		return end * value * value * value * value * value + start;
	}

	public static float easeOutQuint(float start, float end, float value)
	{
		value--;
		end -= start;
		return end * (value * value * value * value * value + 1) + start;
	}

	public static float easeInOutQuint(float start, float end, float value)
	{
		value /= .5f;
		end -= start;
		if (value < 1) return end * 0.5f * value * value * value * value * value + start;
		value -= 2;
		return end * 0.5f * (value * value * value * value * value + 2) + start;
	}

	public static float easeInSine(float start, float end, float value)
	{
		end -= start;
		return -end * Mathf.Cos(value * (Mathf.PI * 0.5f)) + end + start;
	}

	public static float easeOutSine(float start, float end, float value)
	{
		end -= start;
		return end * Mathf.Sin(value * (Mathf.PI * 0.5f)) + start;
	}

	public static float easeInOutSine(float start, float end, float value)
	{
		end -= start;
		return -end * 0.5f * (Mathf.Cos(Mathf.PI * value) - 1) + start;
	}

	public static float easeInExpo(float start, float end, float value)
	{
		end -= start;
		return end * Mathf.Pow(2, 10 * (value - 1)) + start;
	}

	public static float easeOutExpo(float start, float end, float value)
	{
		end -= start;
		return end * (-Mathf.Pow(2, -10 * value) + 1) + start;
	}

	public static float easeInOutExpo(float start, float end, float value)
	{
		value /= .5f;
		end -= start;
		if (value < 1) return end * 0.5f * Mathf.Pow(2, 10 * (value - 1)) + start;
		value--;
		return end * 0.5f * (-Mathf.Pow(2, -10 * value) + 2) + start;
	}

	public static float easeInCirc(float start, float end, float value)
	{
		end -= start;
		return -end * (Mathf.Sqrt(1 - value * value) - 1) + start;
	}

	public static float easeOutCirc(float start, float end, float value)
	{
		value--;
		end -= start;
		return end * Mathf.Sqrt(1 - value * value) + start;
	}

	public static float easeInOutCirc(float start, float end, float value)
	{
		value /= .5f;
		end -= start;
		if (value < 1) return -end * 0.5f * (Mathf.Sqrt(1 - value * value) - 1) + start;
		value -= 2;
		return end * 0.5f * (Mathf.Sqrt(1 - value * value) + 1) + start;
	}

	/* GFX47 MOD START */
	public static float easeInBounce(float start, float end, float value)
	{
		end -= start;
		float d = 1f;
		return end - easeOutBounce(0, end, d - value) + start;
	}
	/* GFX47 MOD END */

	/* GFX47 MOD START */
	//private static float bounce(float start, float end, float value){
	public static float easeOutBounce(float start, float end, float value)
	{
		value /= 1f;
		end -= start;
		if (value < (1 / 2.75f))
		{
			return end * (7.5625f * value * value) + start;
		}
		else if (value < (2 / 2.75f))
		{
			value -= (1.5f / 2.75f);
			return end * (7.5625f * (value) * value + .75f) + start;
		}
		else if (value < (2.5 / 2.75))
		{
			value -= (2.25f / 2.75f);
			return end * (7.5625f * (value) * value + .9375f) + start;
		}
		else
		{
			value -= (2.625f / 2.75f);
			return end * (7.5625f * (value) * value + .984375f) + start;
		}
	}
	/* GFX47 MOD END */

	/* GFX47 MOD START */
	public static float easeInOutBounce(float start, float end, float value)
	{
		end -= start;
		float d = 1f;
		if (value < d * 0.5f) return easeInBounce(0, end, value * 2) * 0.5f + start;
		else return easeOutBounce(0, end, value * 2 - d) * 0.5f + end * 0.5f + start;
	}
	/* GFX47 MOD END */

	public static float easeInBack(float start, float end, float value)
	{
		end -= start;
		value /= 1;
		float s = 1.70158f;
		return end * (value) * value * ((s + 1) * value - s) + start;
	}

	public static float easeOutBack(float start, float end, float value)
	{
		float s = 1.70158f;
		end -= start;
		value = (value) - 1;
		return end * ((value) * value * ((s + 1) * value + s) + 1) + start;
	}

	public static float easeInOutBack(float start, float end, float value)
	{
		float s = 1.70158f;
		end -= start;
		value /= .5f;
		if ((value) < 1)
		{
			s *= (1.525f);
			return end * 0.5f * (value * value * (((s) + 1) * value - s)) + start;
		}
		value -= 2;
		s *= (1.525f);
		return end * 0.5f * ((value) * value * (((s) + 1) * value + s) + 2) + start;
	}

	public static float punch(float amplitude, float value)
	{
		float s = 9;
		if (value == 0)
		{
			return 0;
		}
		else if (value == 1)
		{
			return 0;
		}
		float period = 1 * 0.3f;
		s = period / (2 * Mathf.PI) * Mathf.Asin(0);
		return (amplitude * Mathf.Pow(2, -10 * value) * Mathf.Sin((value * 1 - s) * (2 * Mathf.PI) / period));
	}

	/* GFX47 MOD START */
	public static float easeInElastic(float start, float end, float value)
	{
		end -= start;

		float d = 1f;
		float p = d * .3f;
		float s = 0;
		float a = 0;

		if (value == 0) return start;

		if ((value /= d) == 1) return start + end;

		if (a == 0f || a < Mathf.Abs(end))
		{
			a = end;
			s = p / 4;
		}
		else
		{
			s = p / (2 * Mathf.PI) * Mathf.Asin(end / a);
		}

		return -(a * Mathf.Pow(2, 10 * (value -= 1)) * Mathf.Sin((value * d - s) * (2 * Mathf.PI) / p)) + start;
	}
	/* GFX47 MOD END */

	/* GFX47 MOD START */
	//private static float elastic(float start, float end, float value){
	public static float easeOutElastic(float start, float end, float value)
	{
		/* GFX47 MOD END */
		//Thank you to rafael.marteleto for fixing this as a port over from Pedro's UnityTween
		end -= start;

		float d = 1f;
		float p = d * .3f;
		float s = 0;
		float a = 0;

		if (value == 0) return start;

		if ((value /= d) == 1) return start + end;

		if (a == 0f || a < Mathf.Abs(end))
		{
			a = end;
			s = p * 0.25f;
		}
		else
		{
			s = p / (2 * Mathf.PI) * Mathf.Asin(end / a);
		}

		return (a * Mathf.Pow(2, -10 * value) * Mathf.Sin((value * d - s) * (2 * Mathf.PI) / p) + end + start);
	}

	/* GFX47 MOD START */
	public static float easeInOutElastic(float start, float end, float value)
	{
		end -= start;

		float d = 1f;
		float p = d * .3f;
		float s = 0;
		float a = 0;

		if (value == 0) return start;

		if ((value /= d * 0.5f) == 2) return start + end;

		if (a == 0f || a < Mathf.Abs(end))
		{
			a = end;
			s = p / 4;
		}
		else
		{
			s = p / (2 * Mathf.PI) * Mathf.Asin(end / a);
		}

		if (value < 1) return -0.5f * (a * Mathf.Pow(2, 10 * (value -= 1)) * Mathf.Sin((value * d - s) * (2 * Mathf.PI) / p)) + start;
		return a * Mathf.Pow(2, -10 * (value -= 1)) * Mathf.Sin((value * d - s) * (2 * Mathf.PI) / p) * 0.5f + end + start;
	}
	/* GFX47 MOD END */

	#endregion	
}