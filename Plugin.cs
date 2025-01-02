using System.Collections.Concurrent;
using Exiled.API.Features;
using PlayerEvents = Exiled.Events.Handlers.Player;
using ServerEvents = Exiled.Events.Handlers.Server;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using MEC;
using PlayerRoles;

namespace Queue;

public class Plugin : Plugin<Config> {
  public override string Name => "Queue";
  public override string Author => "furry";
  public override Version Version => new(9, 0, 0);
  public override Version RequiredExiledVersion => new(9, 0, 0);
  
  protected ConcurrentQueue<uint>? Queue;
  protected CoroutineHandle? Coroutine;

  public override void OnEnabled() {
    Log.Info("Subscribing events");
    PlayerEvents.Joined += OnJoined;
    PlayerEvents.Left += OnLeft;
    ServerEvents.RoundEnded += OnRoundEnded;
    ServerEvents.RoundStarted += OnRoundStarted;
    base.OnEnabled();
  }

  public override void OnDisabled() {
    Log.Info("Unsubscribing events");
    PlayerEvents.Joined -= OnJoined;
    PlayerEvents.Left -= OnLeft;
    ServerEvents.RoundEnded -= OnRoundEnded;
    ServerEvents.RoundStarted -= OnRoundStarted;
    base.OnDisabled();
  }

  private IEnumerator<float> ShowQueuePosition() {
    for (;;) {
      if (Queue == null) yield break;
      var queueList = Queue.ToList();
      foreach (var netId in Queue) {
        var p = Player.Get(netId);
        if (p == null) continue;
        p.ShowHint($"You are currently in queue. Your position: {queueList.IndexOf(netId)}");
      }
      yield return Timing.WaitForSeconds(2f);
    }
  }

  private void OnJoined(JoinedEventArgs ev) {
    if (Queue == null || Player.List.Count <= Config.Players) return;
    Queue.Enqueue(ev.Player.NetId);
  }

  private void OnLeft(LeftEventArgs ev) {
    if (Player.List.Count >= Config.Players || Queue == null) return;
    Player? p = null;
    while (p == null && Queue.Count > 0) {
      Queue.TryDequeue(out var netId);
      p = Player.Get(netId);
    }
    if (p == null) return;
    p.Role.Set(RoleTypeId.Spectator);
    p.ShowHint("You have joined the game.");
  }

  private void OnRoundEnded(RoundEndedEventArgs ev) {
    if (Coroutine == null) return;
    Timing.KillCoroutines(Coroutine.Value);
  }

  private void OnRoundStarted() {
    Coroutine = Timing.RunCoroutine(ShowQueuePosition());
    Queue = new ConcurrentQueue<uint>();
  }
}