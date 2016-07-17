using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class TimelineNested : TimelineSpan
{
	public Timeline InnerTimeline { get; internal set; }

	public override bool IsEnded
	{
		get { return InnerTimeline.IsEnded; }
	}

	internal Coroutine Coroutine
	{
		get { return InnerTimeline.Coroutine; }
	}

	internal override void Play()
	{
		InnerTimeline.Play();
	}
}
