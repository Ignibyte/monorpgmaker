namespace MonoRpgMaker.Abstractions;

/// <summary>How an <see cref="IMapEvent"/> is activated.</summary>
public enum EventTrigger
{
    /// <summary>Fires when the player steps onto the event's cell.</summary>
    StepOn,
}
