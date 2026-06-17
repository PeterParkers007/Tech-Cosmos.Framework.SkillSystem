namespace TechCosmos.SkillSystem.Runtime
{
    /// <summary>
    /// 技能执行的网络角色，用于区分不同端的逻辑分支。
    /// </summary>
    public enum NetworkRole
    {
        /// <summary>本地单机执行。</summary>
        Local,
        /// <summary>服务端权威执行。</summary>
        Server,
        /// <summary>客户端接收执行。</summary>
        Client,
        /// <summary>客户端预测执行，待服务端确认。</summary>
        Predicted
    }
}
