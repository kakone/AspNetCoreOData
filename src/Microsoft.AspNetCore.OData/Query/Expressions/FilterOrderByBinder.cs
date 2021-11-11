//-----------------------------------------------------------------------------
// <copyright file="FilterBinder.cs" company=".NET Foundation">
//      Copyright (c) .NET Foundation and Contributors. All rights reserved.
//      See License.txt in the project root for license information.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Linq.Expressions;
using Microsoft.OData.UriParser;

namespace Microsoft.AspNetCore.OData.Query.Expressions
{
    /// <summary>
    /// The default implementation to bind an OData $filter represented by <see cref="FilterClause"/> to a <see cref="Expression"/>.
    /// </summary>
    public class FilterBinder2 /*TODO: Remember to change to FilterBinder when finished*/ : QueryBinder, IFilterBinder
    {
        /// <summary>
        /// Translates an OData $filter represented by <see cref="FilterClause"/> to <see cref="Expression"/>.
        /// $filter=Name eq 'Sam'
        ///    |--  $it => $it.Name == "Sam"
        /// </summary>
        /// <param name="filterClause">The filter clause.</param>
        /// <param name="context">The query binder context.</param>
        /// <returns>The filter binder result.</returns>
        public virtual Expression BindFilter(FilterClause filterClause, QueryBinderContext context)
        {
            if (filterClause == null)
            {
                throw Error.ArgumentNull(nameof(filterClause));
            }

            if (context == null)
            {
                throw Error.ArgumentNull(nameof(context));
            }

            // Save the filter binder into the context internally
            // It will be used in sub-filter, for example:  $filter=collectionProp/$count($filter=Name eq 'abc') gt 2
            context.FilterBinder = this;

            Type filterType = context.ElementClrType;

            LambdaExpression filterExpr = BindExpression(filterClause.Expression, filterClause.RangeVariable, context);
            filterExpr = Expression.Lambda(ApplyNullPropagationForFilterBody(filterExpr.Body, context), filterExpr.Parameters);

            Type expectedFilterType = typeof(Func<,>).MakeGenericType(filterType, typeof(bool));
            if (filterExpr.Type != expectedFilterType)
            {
                throw Error.Argument("filterType", SRResources.CannotCastFilter, filterExpr.Type.FullName, expectedFilterType.FullName);
            }

            return filterExpr;
        }
    }
}