namespace MessageTransit.EntityFrameworkCoreIntegration.Audit;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;


public class AuditModelCacheKeyFactory : IModelCacheKeyFactory
{
    public virtual object Create(DbContext context, bool designTime)
    {
        return context is AuditDbContext auditContext
            ? (context.GetType(), auditContext.AuditTableName, auditContext.AuditTableSchema, designTime)
            : new ModelCacheKey(context, designTime);
    }
}
