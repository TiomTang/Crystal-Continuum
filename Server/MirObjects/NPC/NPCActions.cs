namespace Server.MirObjects
{
    public class NPCActions
    {
        public ScriptActionKind Type;
        public List<string> Params = new List<string>();
        public ScriptSourceInfo Source;

        public NPCActions(ScriptActionKind action, params string[] p)
        {
            Type = action;

            Params.AddRange(p);
        }
    }

}