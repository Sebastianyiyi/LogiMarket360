namespace LogiMarket360.Application.Patterns.Interpreter;

public class AndExpression : IExpression
{
    private readonly IExpression _izquierda;
    private readonly IExpression _derecha;

    public AndExpression(IExpression izquierda, IExpression derecha)
    {
        _izquierda = izquierda ?? throw new ArgumentNullException(nameof(izquierda));
        _derecha = derecha ?? throw new ArgumentNullException(nameof(derecha));
    }

    public bool Interpretar(ContextoPedido contexto)
    {
        return _izquierda.Interpretar(contexto) && _derecha.Interpretar(contexto);
    }
}

public class OrExpression : IExpression
{
    private readonly IExpression _izquierda;
    private readonly IExpression _derecha;

    public OrExpression(IExpression izquierda, IExpression derecha)
    {
        _izquierda = izquierda ?? throw new ArgumentNullException(nameof(izquierda));
        _derecha = derecha ?? throw new ArgumentNullException(nameof(derecha));
    }

    public bool Interpretar(ContextoPedido contexto)
    {
        return _izquierda.Interpretar(contexto) || _derecha.Interpretar(contexto);
    }
}
