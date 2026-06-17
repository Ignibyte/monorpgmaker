using MonoRpgMaker.Engine.Core;
using MonoRpgMaker.Engine.Sim.Tracer;

namespace MonoRpgMaker.Player;

/// <summary>
/// The shipped game executable's host: boots the engine over the M0 tracer scene.
/// </summary>
public sealed class Game1 : RpgGame
{
    /// <summary>Create the host over the tracer room.</summary>
    public Game1()
        : base(TracerRoom.Build())
    {
    }
}
