using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public sealed class TimelineSequence : TimelineSpan
{
    public Action<TimelineSequence> UpdateAction;
   
	public string Name { get; internal set; }
	public float Percent { get; private set; }
	internal float DurationInternal { get; set; }

	internal TimelineSequence()
    {
		Percent = 0f;
    }

	public override float Duration
	{
		get { return DurationInternal; }
	}

    public T Var<T>(string name)
    {
        return this.Timeline.Var<T>(name);
    }

	internal override void Update(float timecode)
	{
		this.Percent = DurationInternal <= 0f ?
				1f :
				Mathf.Clamp01((timecode - this.EventTimecode) / Duration);

		if (UpdateAction != null)
			UpdateAction(this);
	}
}
