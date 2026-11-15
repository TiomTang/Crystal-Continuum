using System;
using System.Collections.Generic;

namespace Server.MirObjects
{
    /// <summary>Failure and feedback state follows a request, including its delayed pages, not a shared NPC.</summary>
    internal sealed class ScriptInteraction
    {
        [ThreadStatic]
        private static ScriptInteraction current;
        internal static ScriptInteraction Current => current;

        private readonly bool notifyPlayer;
        private readonly Queue<string> path = new Queue<string>();
        private bool notified;
        private readonly HashSet<string> inputs = new HashSet<string>(StringComparer.Ordinal);
        private bool withholdInputContext;
        internal bool Failed { get; private set; }
        internal string FailureId { get; private set; }
        internal string LastCompletedAction;
        internal bool HasSideEffects;
        internal ScriptSourceInfo LastSource;
        internal string CallPath => string.Join(" -> ", path);

        internal void RememberInput(string input)
        {
            if (string.IsNullOrEmpty(input) || inputs.Contains(input)) return;
            if (input.Length > 4096 || inputs.Count >= 16) withholdInputContext = true;
            else inputs.Add(input);
        }

        internal string RedactInput(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            if (withholdInputContext) return "[content withheld: input context limit]";
            foreach (string input in inputs) value = value.Replace(input, "[redacted]", StringComparison.OrdinalIgnoreCase);
            return value;
        }

        internal ScriptExecutionEvidence CreateEvidence(ScriptExecutionEvidence origin = null, string script = null, string page = null)
        {
            var evidence = origin?.Copy() ?? new ScriptExecutionEvidence();
            evidence.Source ??= LastSource;
            evidence.ScriptIdentity = script ?? evidence.ScriptIdentity;
            evidence.PageKey = page ?? evidence.PageKey;
            evidence.Phase = ScriptDiagnosticPhase.Action;
            evidence.LastCompletedAction = LastCompletedAction ?? evidence.LastCompletedAction;
            evidence.HasSideEffects |= HasSideEffects;
            evidence.CallPath = CallPath;
            return evidence;
        }

        static ScriptInteraction()
        {
            ScriptDiagnostics.Reported += diagnostic =>
            {
                if (current == null || !diagnostic.Runtime || diagnostic.IsSummary ||
                    diagnostic.Effect == ScriptDiagnosticEffect.DisplayFallback ||
                    diagnostic.Effect == ScriptDiagnosticEffect.Notice) return;
                current.Failed = true;
                current.FailureId ??= diagnostic.Id;
            };
        }

        internal ScriptInteraction(bool notifyPlayer)
        {
            this.notifyPlayer = notifyPlayer;
        }

        internal static IDisposable Enter(PlayerObject player, ScriptInteraction interaction, string page = null)
        {
            return new Scope(player, interaction, page);
        }

        private sealed class Scope : IDisposable
        {
            private readonly ScriptInteraction previous, interaction;
            private readonly PlayerObject player;
            private bool disposed;

            internal Scope(PlayerObject player, ScriptInteraction interaction, string page)
            {
                this.player = player;
                this.interaction = interaction;
                previous = current;
                current = interaction;
                if (!string.IsNullOrEmpty(page))
                {
                    if (interaction.path.Count == 16) interaction.path.Dequeue();
                    interaction.path.Enqueue(page);
                }
            }

            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                try
                {
                    if (!interaction.Failed) return;
                    if (player != null)
                    {
                        // Panel requests authorize through NPCPage, not only the displayed button results.
                        player.NPCPage = null;
                        player.NPCSuccess?.Clear();
                        player.NPCSpeech?.Clear();
                        player.NPCDialogClose = false;
                        player.ActionList.RemoveAll(action => action.Type == DelayedType.NPC &&
                            ReferenceEquals(action.Interaction, interaction));

                        if (interaction.notifyPlayer && !interaction.notified)
                        {
                            interaction.notified = true;
                            player.ReceiveChat(GameLanguage.Get(
                                "This operation could not be completed. Please contact an administrator. Reference: {0}",
                                interaction.FailureId), ChatType.System);
                        }
                    }
                }
                finally
                {
                    current = previous;
                }
            }
        }
    }
}
