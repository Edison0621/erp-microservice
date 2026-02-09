using System.Linq.Expressions;

namespace ErpSystem.BuildingBlocks.Domain.Specifications;

public abstract class Specification<T>(Expression<Func<T, bool>> criteria) : ISpecification<T>
{
    public Expression<Func<T, bool>> Criteria { get; } = criteria;
    public List<Expression<Func<T, object>>> Includes { get; } = [];
    public List<string> IncludeStrings { get; } = [];
    public Expression<Func<T, object>> OrderBy { get; private set; }
    public Expression<Func<T, object>> OrderByDescending { get; private set; } 
    public Expression<Func<T, object>> GroupBy { get; private set; }

    public int Take { get; private set; }
    public int Skip { get; private set; }
    public bool IsPagingEnabled { get; private set; }

    protected virtual void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        this.Includes.Add(includeExpression);
    }

    protected virtual void AddInclude(string includeString)
    {
        this.IncludeStrings.Add(includeString);
    }

    protected virtual void ApplyPaging(int skip, int take)
    {
        this.Skip = skip;
        this.Take = take;
        this.IsPagingEnabled = true;
    }

    protected virtual void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        this.OrderBy = orderByExpression;
    }

    protected virtual void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
    {
        this.OrderByDescending = orderByDescendingExpression;
    }
    
    protected virtual void ApplyGroupBy(Expression<Func<T, object>> groupByExpression)
    {
        this.GroupBy = groupByExpression;
    }
}
