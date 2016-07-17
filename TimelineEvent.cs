using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public abstract class TimelineEvent
{
	public int EventIndex { get; internal set; }
	public Timeline Timeline { get; internal set; }
	public float TimeCode { get; internal set; }

	internal protected TimelineEvent()
	{

	}

	public override string ToString()
	{
		return string.Format("[{0}] #{1}: {2} @ {3} @ {4}",
			this.Timeline.Script.name,
			this.EventIndex,
			this is TimelineWaitMarker ? "Wait" :
			this is TimelineAction ? "Do" :
			this is TimelineLoopMarker ? ((TimelineLoopMarker)this).MarkerType == TimelineLoopMarkerType.Begin ? "LoopBegin" : "LoopEnd" :
			"Sequence",
			this.TimeCode,
			this.Timeline.PlayTime);
	}
}

public sealed class TimelineWaitMarker: TimelineEvent
{

}

public sealed class TimelineLoopMarker: TimelineEvent
{
	public TimelineLoopMarkerType MarkerType { get; internal set; }
	public int LoopCount { get; internal set; }
}

public enum TimelineLoopMarkerType
{
	Begin,
	End
}

public sealed class TimelineAction : TimelineEvent
{
	public Action<TimelineAction> Action { get; internal set; }

	internal void Invoke()
	{
		this.Action(this);
	}
}

public abstract class TimelineSpan: TimelineEvent
{
	public abstract bool IsEnded { get; }
	internal abstract void Play();
}