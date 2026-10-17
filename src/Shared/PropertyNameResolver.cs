namespace Atc.Azure.Messaging.Internal;

internal static class PropertyNameResolver
{
    /// <summary>
    /// Gets the member name from an expression like <c>m =&gt; m.TenantId</c>.
    /// </summary>
    /// <typeparam name="TMessage">The type of message.</typeparam>
    /// <param name="selector">Selects the value from the message.</param>
    public static string GetName<TMessage>(
        Expression<Func<TMessage, object?>> selector)
        => selector.Body switch
        {
            MemberExpression { Member.Name: var name } => name,
            UnaryExpression { Operand: MemberExpression { Member.Name: var name } } => name,
            _ => throw new ArgumentException(
                "The expression must select a property or field, e.g. m => m.TenantId.",
                nameof(selector)),
        };
}