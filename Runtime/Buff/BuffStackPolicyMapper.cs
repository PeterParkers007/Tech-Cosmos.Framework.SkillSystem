namespace TechCosmos.SkillSystem.Runtime
{
    internal static class BuffStackPolicyMapper
    {
        public static BuffStackPolicy ToGbfPolicy(ModifierStackPolicy policy)
        {
            return policy switch
            {
                ModifierStackPolicy.RefreshDuration => BuffStackPolicy.ExtendDuration,
                ModifierStackPolicy.Stack => BuffStackPolicy.StackAndRefresh,
                ModifierStackPolicy.Replace => BuffStackPolicy.Replace,
                ModifierStackPolicy.Ignore => BuffStackPolicy.ExtendDuration,
                _ => BuffStackPolicy.StackAndRefresh
            };
        }
    }
}
