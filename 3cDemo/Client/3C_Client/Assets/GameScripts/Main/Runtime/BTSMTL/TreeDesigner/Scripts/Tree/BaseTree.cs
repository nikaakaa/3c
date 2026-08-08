namespace TreeDesigner
{
    public static class PipelineBlackboardAuthoringSchema
    {
        public const int CurrentRevision = 1;
    }

    [TreeWindow("OpenBaseTreeWindow")]
    [AcceptableNodePaths("Base")]
    public partial class BaseTree : BaseGraph
    {
        [UnityEngine.SerializeField]
        int m_BlackboardAuthoringSchemaRevision;

        public int BlackboardAuthoringSchemaRevision => m_BlackboardAuthoringSchemaRevision;

#if UNITY_EDITOR
        public void SetBlackboardAuthoringSchemaRevision(int revision)
        {
            if (revision != PipelineBlackboardAuthoringSchema.CurrentRevision)
                throw new System.ArgumentOutOfRangeException(nameof(revision), revision, null);
            m_BlackboardAuthoringSchemaRevision = revision;
        }
#endif
    }
}
