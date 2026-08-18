using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MyFirstPlugin.Config;

namespace MyFirstPlugin.Events;

public sealed class SpeedDemonEvent : EventBase
{
    private readonly SpeedDemonEventConfig _config;
    private readonly Dictionary<uint, MovementBoostState> _affectedPlayers = new();
    private bool _subscribed;

    public SpeedDemonEvent(SpeedDemonEventConfig? config = null)
    {
        _config = config ?? new SpeedDemonEventConfig();
    }

    public override string Name => "Speed Demon";

    public override string Description => "Everyone moves at extreme speed. Good luck.";

    protected override void OnStart()
    {
        Subscribe();

        foreach (Player player in Player.List)
            ApplyToHuman(player);
    }

    protected override void OnStop()
    {
        Unsubscribe();

        foreach (KeyValuePair<uint, MovementBoostState> affectedPlayer in _affectedPlayers)
        {
            Player? player = null;
            foreach (Player candidate in Player.List)
            {
                if (candidate.NetworkId == affectedPlayer.Key)
                {
                    player = candidate;
                    break;
                }
            }

            if (player == null || player.IsDestroyed)
                continue;

            RestorePlayer(player, affectedPlayer.Value);
        }

        _affectedPlayers.Clear();
    }

    private void Subscribe()
    {
        if (_subscribed)
            return;

        PlayerEvents.Joined += OnPlayerJoined;
        PlayerEvents.Spawned += OnPlayerSpawned;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed)
            return;

        PlayerEvents.Joined -= OnPlayerJoined;
        PlayerEvents.Spawned -= OnPlayerSpawned;
        _subscribed = false;
    }

    private void OnPlayerJoined(PlayerJoinedEventArgs args)
    {
        ApplyToHuman(args.Player);
    }

    private void OnPlayerSpawned(PlayerSpawnedEventArgs args)
    {
        ApplyToHuman(args.Player);
    }

    private void ApplyToHuman(Player? player)
    {
        if (player == null || player.IsDestroyed || !player.IsHuman)
            return;

        if (!_affectedPlayers.ContainsKey(player.NetworkId))
        {
            MovementBoost? existingEffect = player.GetEffect<MovementBoost>();
            _affectedPlayers[player.NetworkId] = new MovementBoostState(
                existingEffect != null && existingEffect.IsEnabled,
                existingEffect?.Intensity ?? 0,
                existingEffect?.TimeLeft ?? 0f
            );
        }

        player.EnableEffect<MovementBoost>(
            _config.Intensity,
            _config.DurationSeconds,
            false
        );
    }

    private static void RestorePlayer(Player player, MovementBoostState state)
    {
        if (state.WasEnabled)
        {
            player.EnableEffect<MovementBoost>(
                state.Intensity,
                state.TimeLeft,
                false
            );
        }
        else
        {
            player.DisableEffect<MovementBoost>();
        }
    }

    private readonly struct MovementBoostState
    {
        public MovementBoostState(bool wasEnabled, byte intensity, float timeLeft)
        {
            WasEnabled = wasEnabled;
            Intensity = intensity;
            TimeLeft = timeLeft;
        }

        public bool WasEnabled { get; }

        public byte Intensity { get; }

        public float TimeLeft { get; }
    }
}
