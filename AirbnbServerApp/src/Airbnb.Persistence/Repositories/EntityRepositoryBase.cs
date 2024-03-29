﻿using Airbnb.Domain.Common.Caching;
using Airbnb.Domain.Common.Entities.Interfaces;
using Airbnb.Persistence.Caching.Brokers.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using AirBnB.Domain.Common.Query;
using AirBnB.Persistence.Extensions;

namespace Airbnb.Persistence.Repositories;

public abstract class EntityRepositoryBase<TEntity, TContext>(
    TContext dbContext, 
    ICacheBroker cacheBroker,
    CacheEntryOptions? cacheEntryOptions = default)
    where TEntity : class, IEntity where TContext : DbContext
{
    protected TContext DbContext => dbContext;

    protected IQueryable<TEntity> Get(
        Expression<Func<TEntity, bool>>? predicate = default,
        bool asNoTracking = default
    )
    {
        var initialQuery = DbContext.Set<TEntity>().Where(entity => true);
        if(predicate is not null)
            initialQuery = initialQuery.Where(predicate);

        if (asNoTracking)
            initialQuery = initialQuery.AsNoTracking();

        return initialQuery;
    }

    protected async ValueTask<IList<TEntity>> GetAsync(
        QuerySpecification<TEntity> querySpecification,
        CancellationToken cancellationToken = default
    )
    {
        var foundEntities = new List<TEntity>();
        var cacheKey = querySpecification.CacheKey;

        if(cacheEntryOptions is null || !await cacheBroker.TryGetAsync<List<TEntity>>(cacheKey, out var cacheEntities))
        {
            var initialQuery = DbContext.Set<TEntity>().AsQueryable();

            if (querySpecification.AsNoTracking) initialQuery = initialQuery.AsNoTracking();

            initialQuery = initialQuery.ApplySpecification(querySpecification);

            foundEntities = await initialQuery.ToListAsync(cancellationToken);

            if(cacheEntryOptions is not null)
                await cacheBroker.SetAsync(cacheKey, foundEntities, cacheEntryOptions, cancellationToken);

        }else if(cacheEntities is not null)  
            foundEntities = cacheEntities;

        return foundEntities;
    }

    protected async ValueTask<TEntity?> GetByIdAsync(
        Guid id,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    )
    {
        var foundEntity = default(TEntity?);

        if (cacheEntryOptions is null || !await cacheBroker.TryGetAsync<TEntity>(id.ToString(), out var cachedEntity))
        {
            var initialQuery = DbContext.Set<TEntity>().AsQueryable();

            if (asNoTracking)
                initialQuery = initialQuery.AsNoTracking();

            foundEntity = await initialQuery.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

            if (foundEntity is not null && cacheEntryOptions is not null)
                await cacheBroker.SetAsync(foundEntity.Id.ToString(), foundEntity, cacheEntryOptions, cancellationToken);
        }
        else foundEntity = cachedEntity;
        
        return foundEntity;
    }

    protected async ValueTask<IList<TEntity>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default
    )
    {
        var initialQuery = DbContext.Set<TEntity>().Where(entity => true);

        if(asNoTracking)
            initialQuery = initialQuery.AsNoTracking();

        initialQuery = initialQuery.Where(entity => ids.Contains(entity.Id));

        return await initialQuery.ToListAsync(cancellationToken: cancellationToken);
    }

    protected async ValueTask<TEntity> CreateAsync(
        TEntity entity,
        bool saveChanges = true,
        CancellationToken cancellationToken = default
    )
    {
        entity.Id = Guid.Empty;
        await DbContext.Set<TEntity>().AddAsync(entity, cancellationToken);

        if (cacheEntryOptions is not null)
            await cacheBroker.SetAsync(entity.Id.ToString(), entity, cacheEntryOptions, cancellationToken);
        if (saveChanges)
            await DbContext.SaveChangesAsync(cancellationToken);

        return entity;
    }

    protected async ValueTask<TEntity> UpdateAsync(
        TEntity entity,
        bool saveChanges = true,
        CancellationToken cancellationToken = default
    )
    {
        DbContext.Set<TEntity>().Update(entity);
        if (cacheEntryOptions is not null)
            await cacheBroker.SetAsync(entity.Id.ToString(), entity, cacheEntryOptions, cancellationToken);

        if (saveChanges)
            await DbContext.SaveChangesAsync(cancellationToken: cancellationToken);

        return entity;
    }

    protected async ValueTask<bool> DeleteAsync(
        TEntity entity,
        bool saveChanges = true,
        CancellationToken cancellationToken = default
    )
    {
        DbContext.Set<TEntity>().Remove(entity);

        if (cacheEntryOptions is not null)
            await cacheBroker.DeleteAsync(entity.Id.ToString());
        if (saveChanges)
            await DbContext.SaveChangesAsync(cancellationToken: cancellationToken);

        return true;
    }

    protected async ValueTask<bool> DeleteByIdAsync(
        Guid id,
        bool saveChanges = true,
        CancellationToken cancellationToken = default
    )
    {
        var entity = await DbContext.Set<TEntity>().FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken) ??
                     throw new InvalidOperationException();
        DbContext.Set<TEntity>().Remove(entity);
        if (cacheEntryOptions is not null)
            await cacheBroker.DeleteAsync(entity.Id.ToString());
        if (saveChanges)
            await DbContext.SaveChangesAsync(cancellationToken: cancellationToken);

        return true;
    }
}