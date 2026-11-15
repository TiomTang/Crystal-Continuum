namespace Server.MirObjects
{
    public class ScriptActions
    {
        public ScriptActionKind Type;
        public List<string> Params = new List<string>();
        public ScriptSourceInfo Source;

        public ScriptActions(ScriptActionKind action, params string[] p)
        {
            Type = action;

            Params.AddRange(p);
        }
    }

}