namespace Server.MirObjects
{
    public class NPCChecks
    {
        public ScriptCheckKind Type;
        public List<string> Params = new List<string>();
        public ScriptSourceInfo Source;

        public NPCChecks(ScriptCheckKind check, params string[] p)
        {
            Type = check;

            for (int i = 0; i < p.Length; i++)
                Params.Add(p[i]);
        }
    }

}