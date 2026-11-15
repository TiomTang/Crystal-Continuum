using Server.MirEnvir;

namespace Server.MirObjects
{
    public enum DelayedType
    {
        Magic,
        /// <summary>
        /// Param0 MapObject (Target) | Param1 Damage | Param2 Defence | Param3 damageWeapon | Param4 UserMagic | Param5 FinalHit
        /// </summary>
        Damage,
        RangeDamage,        
        Spawn,
        Die,
        Recall,
        MapMovement,
        Mine,
        NPC,
        Poison,
        DamageIndicator,
        Quest,

        // Sanjian
        SpellEffect,
    }

    public class DelayedAction
    {
        protected static Envir Envir
        {
            get { return Envir.Main; }
        }

        public DelayedType Type;
        public long Time;
        public long StartTime;
        public object[] Params;

        public bool FlaggedToRemove;
        internal ScriptInteraction Interaction;

        /// <summary>Script load generation when the step was queued; 0 when it was not queued by a script.</summary>
        internal int ScriptGeneration;
        internal ScriptExecutionEvidence ScriptEvidence;

        public DelayedAction(DelayedType type, long time, params object[] p)
        {
            StartTime = Envir.Time;
            Type = type;
            Time = time;
            Params = p;
            if (type == DelayedType.NPC)
            {
                Interaction = ScriptInteraction.Current;
                ScriptEvidence = ScriptDiagnostics.CaptureExecution();
                // Bind to the script named in the parameters: the queuer may be another script.
                if (p.Length > 1 && p[1] is int scriptId && Envir.Scripts.TryGetValue(scriptId, out NPCScript target))
                    ScriptGeneration = target.LoadGeneration;
            }
        }
    }
}
