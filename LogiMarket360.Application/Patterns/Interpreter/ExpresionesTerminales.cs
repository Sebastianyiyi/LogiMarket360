namespace LogiMarket360.Application.Patterns.Interpreter;

using System.Globalization;
using LogiMarket360.Domain.Enums;

public class EstadoTerminalExpression : IExpression
{
    private readonly EstadoPedido _estadoEsperado;

    public EstadoTerminalExpression(string estadoTexto)
    {
        if (Enum.TryParse<EstadoPedido>(estadoTexto, ignoreCase: true, out var estado))
        {
            _estadoEsperado = estado;
        }
        else
        {
            throw new FormatException($"El valor '{estadoTexto}' no es un estado válido de pedido.");
        }
    }

    public bool Interpretar(ContextoPedido contexto)
    {
        return contexto.Pedido.EstadoCodigo == _estadoEsperado;
    }
}

public class TotalTerminalExpression : IExpression
{
    private readonly string _operador;
    private readonly decimal _valor;

    public TotalTerminalExpression(string operador, decimal valor)
    {
        _operador = operador;
        _valor = valor;
    }

    public bool Interpretar(ContextoPedido contexto)
    {
        decimal total = contexto.Pedido.Total;

        return _operador switch
        {
            ">=" => total >= _valor,
            "<=" => total <= _valor,
            ">" => total > _valor,
            "<" => total < _valor,
            "=" => total == _valor,
            _ => throw new InvalidOperationException($"Operador de comparación '{_operador}' no soportado.")
        };
    }
}

public class ClienteTerminalExpression : IExpression
{
    private readonly string _textoBusqueda;

    public ClienteTerminalExpression(string textoBusqueda)
    {
        _textoBusqueda = textoBusqueda.Trim().Trim('\'', '"');
    }

    public bool Interpretar(ContextoPedido contexto)
    {
        return contexto.NombreCliente.Contains(_textoBusqueda, StringComparison.OrdinalIgnoreCase);
    }
}
